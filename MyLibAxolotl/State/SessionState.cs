/**
 * Copyright (C) 2015 smndtrl, langboost
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using Google.ProtocolBuffers;
using Strilanc.Value;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Tr.Com.Eimza.LibAxolotl.Ecc;
using Tr.Com.Eimza.LibAxolotl.Kdf;
using Tr.Com.Eimza.LibAxolotl.Ratchet;
using Tr.Com.Eimza.LibAxolotl.Util;
using static Tr.Com.Eimza.LibAxolotl.State.StorageProtos;
using static Tr.Com.Eimza.LibAxolotl.State.StorageProtos.SessionStructure.Types;

namespace Tr.Com.Eimza.LibAxolotl.State
{
    public class SessionState
    {
        private static readonly int MAX_MESSAGE_KEYS = 2000;

        private SessionStructure sessionStructure;

        public SessionState()
        {
            this.sessionStructure = SessionStructure.CreateBuilder().Build();
        }

        public SessionState(SessionStructure sessionStructure)
        {
            this.sessionStructure = sessionStructure;
        }

        public SessionState(SessionState copy)
        {
            this.sessionStructure = copy.sessionStructure.ToBuilder().Build();
        }

        public SessionStructure GetStructure()
        {
            return sessionStructure;
        }

        public byte[] GetAliceBaseKey()
        {
            return this.sessionStructure.AliceBaseKey.ToByteArray();
        }

        public void SetAliceBaseKey(byte[] aliceBaseKey)
        {
            this.sessionStructure = this.sessionStructure.ToBuilder()
                                                         .SetAliceBaseKey(ByteString.CopyFrom(aliceBaseKey))
                                                         .Build();
        }

        public void SetSessionVersion(uint version)
        {
            this.sessionStructure = this.sessionStructure.ToBuilder()
                                                         .SetSessionVersion(version)
                                                         .Build();
        }

        public uint GetSessionVersion()
        {
            uint sessionVersion = this.sessionStructure.SessionVersion;

            if (sessionVersion == 0)
                return 2;
            else
                return sessionVersion;
        }

        public void SetRemoteIdentityKey(IdentityKey identityKey)
        {
            this.sessionStructure = this.sessionStructure.ToBuilder()
                                                         .SetRemoteIdentityPublic(ByteString.CopyFrom(identityKey.Serialize()))
                                                         .Build();
        }

        public void SetLocalIdentityKey(IdentityKey identityKey)
        {
            this.sessionStructure = this.sessionStructure.ToBuilder()
                                                         .SetLocalIdentityPublic(ByteString.CopyFrom(identityKey.Serialize()))
                                                         .Build();
        }

        public IdentityKey GetRemoteIdentityKey()
        {
            try
            {
                if (!this.sessionStructure.HasRemoteIdentityPublic)
                {
                    return null;
                }

                return new IdentityKey(this.sessionStructure.RemoteIdentityPublic.ToByteArray(), 0);
            }
            catch (InvalidKeyException e)
            {
                Debug.WriteLine(e.ToString(), "SessionRecordV2");
                return null;
            }
        }

        public IdentityKey GetLocalIdentityKey()
        {
            try
            {
                return new IdentityKey(this.sessionStructure.LocalIdentityPublic.ToByteArray(), 0);
            }
            catch (InvalidKeyException e)
            {
                throw new Exception(e.Message);
            }
        }

        public uint GetPreviousCounter()
        {
            return sessionStructure.PreviousCounter;
        }

        public void SetPreviousCounter(uint previousCounter)
        {
            this.sessionStructure = this.sessionStructure.ToBuilder()
                                                         .SetPreviousCounter(previousCounter)
                                                         .Build();
        }

        public RootKey GetRootKey()
        {
            return new RootKey(HKDF.CreateFor(GetSessionVersion()),
                               this.sessionStructure.RootKey.ToByteArray());
        }

        public void SetRootKey(RootKey rootKey)
        {
            this.sessionStructure = this.sessionStructure.ToBuilder()
                                                         .SetRootKey(ByteString.CopyFrom(rootKey.GetKeyBytes()))
                                                         .Build();
        }

        public ECPublicKey GetSenderRatchetKey()
        {
            try
            {
                return Curve.DecodePoint(sessionStructure.SenderChain.SenderRatchetKey.ToByteArray(), 0);
            }
            catch (InvalidKeyException e)
            {
                throw new Exception(e.Message);
            }
        }

        public ECKeyPair GetSenderRatchetKeyPair()
        {
            ECPublicKey publicKey = GetSenderRatchetKey();
            ECPrivateKey privateKey = Curve.DecodePrivatePoint(sessionStructure.SenderChain
                                                                               .SenderRatchetKeyPrivate
                                                                               .ToByteArray());

            return new ECKeyPair(publicKey, privateKey);
        }

        public bool HasReceiverChain(ECPublicKey senderEphemeral)
        {
            return GetReceiverChain(senderEphemeral) != null;
        }

        public bool HasSenderChain()
        {
            return sessionStructure.HasSenderChain;
        }

        private Pair<Chain, uint> GetReceiverChain(ECPublicKey senderEphemeral)
        {
            IList<Chain> receiverChains = sessionStructure.ReceiverChainsList;
            uint index = 0;

            foreach (Chain receiverChain in receiverChains)
            {
                try
                {
                    ECPublicKey chainSenderRatchetKey = Curve.DecodePoint(receiverChain.SenderRatchetKey.ToByteArray(), 0);

                    if (chainSenderRatchetKey.Equals(senderEphemeral))
                    {
                        return new Pair<Chain, uint>(receiverChain, index);
                    }
                }
                catch (InvalidKeyException e)
                {
                    Debug.WriteLine(e.ToString(), "SessionRecordV2");
                }

                index++;
            }

            return null;
        }

        public ChainKey GetReceiverChainKey(ECPublicKey senderEphemeral)
        {
            Pair<Chain, uint> receiverChainAndIndex = GetReceiverChain(senderEphemeral);
            Chain receiverChain = receiverChainAndIndex.First();

            if (receiverChain == null)
            {
                return null;
            }
            else
            {
                return new ChainKey(HKDF.CreateFor(GetSessionVersion()),
                                    receiverChain.ChainKey.Key.ToByteArray(),
                                    receiverChain.ChainKey.Index);
            }
        }

        public void AddReceiverChain(ECPublicKey senderRatchetKey, ChainKey chainKey)
        {
            Chain.Types.ChainKey chainKeyStructure = Chain.Types.ChainKey.CreateBuilder()
                                                             .SetKey(ByteString.CopyFrom(chainKey.GetKey()))
                                                             .SetIndex(chainKey.GetIndex())
                                                             .Build();

            Chain chain = Chain.CreateBuilder()
                               .SetChainKey(chainKeyStructure)
                               .SetSenderRatchetKey(ByteString.CopyFrom(senderRatchetKey.Serialize()))
                               .Build();

            this.sessionStructure = this.sessionStructure.ToBuilder().AddReceiverChains(chain).Build();

            if (this.sessionStructure.ReceiverChainsList.Count > 5)
            {
                this.sessionStructure = this.sessionStructure.ToBuilder()/*.ClearReceiverChains()*/.Build(); //RemoveReceiverChains(0) TODO: why does it work without
            }
        }

        public void SetSenderChain(ECKeyPair senderRatchetKeyPair, ChainKey chainKey)
        {
            Chain.Types.ChainKey chainKeyStructure = Chain.Types.ChainKey.CreateBuilder()
                                                             .SetKey(ByteString.CopyFrom(chainKey.GetKey()))
                                                             .SetIndex(chainKey.GetIndex())
                                                             .Build();

            Chain senderChain = Chain.CreateBuilder()
                                     .SetSenderRatchetKey(ByteString.CopyFrom(senderRatchetKeyPair.GetPublicKey().Serialize()))
                                     .SetSenderRatchetKeyPrivate(ByteString.CopyFrom(senderRatchetKeyPair.GetPrivateKey().Serialize()))
                                     .SetChainKey(chainKeyStructure)
                                     .Build();

            this.sessionStructure = this.sessionStructure.ToBuilder().SetSenderChain(senderChain).Build();
        }

        public ChainKey GetSenderChainKey()
        {
            Chain.Types.ChainKey chainKeyStructure = sessionStructure.SenderChain.ChainKey;
            return new ChainKey(HKDF.CreateFor(GetSessionVersion()),
                                chainKeyStructure.Key.ToByteArray(), chainKeyStructure.Index);
        }

        public void SetSenderChainKey(ChainKey nextChainKey)
        {
            Chain.Types.ChainKey chainKey = Chain.Types.ChainKey.CreateBuilder()
                                                    .SetKey(ByteString.CopyFrom(nextChainKey.GetKey()))
                                                    .SetIndex(nextChainKey.GetIndex())
                                                    .Build();

            Chain chain = sessionStructure.SenderChain.ToBuilder()
                                          .SetChainKey(chainKey).Build();

            this.sessionStructure = this.sessionStructure.ToBuilder().SetSenderChain(chain).Build();
        }

        public bool HasMessageKeys(ECPublicKey senderEphemeral, uint counter)
        {
            Pair<Chain, uint> chainAndIndex = GetReceiverChain(senderEphemeral);
            Chain chain = chainAndIndex.First();

            if (chain == null)
            {
                return false;
            }

            IList<Chain.Types.MessageKey> messageKeyList = chain.MessageKeysList;

            foreach (Chain.Types.MessageKey messageKey in messageKeyList)
            {
                if (messageKey.Index == counter)
                {
                    return true;
                }
            }

            return false;
        }

        public MessageKeys RemoveMessageKeys(ECPublicKey senderEphemeral, uint counter)
        {
            Pair<Chain, uint> chainAndIndex = GetReceiverChain(senderEphemeral);
            Chain chain = chainAndIndex.First();

            if (chain == null)
            {
                return null;
            }

            List<Chain.Types.MessageKey> messageKeyList = new List<Chain.Types.MessageKey>(chain.MessageKeysList);
            IEnumerator<Chain.Types.MessageKey> messageKeyIterator = messageKeyList.GetEnumerator();
            MessageKeys result = null;

            while (messageKeyIterator.MoveNext()) //hasNext()
            {
                Chain.Types.MessageKey messageKey = messageKeyIterator.Current; // next()

                if (messageKey.Index == counter)
                {
                    result = new MessageKeys(messageKey.CipherKey.ToByteArray(),
                                            messageKey.MacKey.ToByteArray(),
                                             messageKey.Iv.ToByteArray(),
                                             messageKey.Index);

                    messageKeyList.Remove(messageKey); //messageKeyIterator.remove();
                    break;
                }
            }

            Chain updatedChain = chain.ToBuilder().ClearMessageKeys()
                                      .AddRangeMessageKeys(messageKeyList) // AddAllMessageKeys
                                      .Build();

            this.sessionStructure = this.sessionStructure.ToBuilder()
                                                         .SetReceiverChains((int)chainAndIndex.Second(), updatedChain) // TODO: conv
                                                         .Build();

            return result;
        }

        public void SetMessageKeys(ECPublicKey senderEphemeral, MessageKeys messageKeys)
        {
            Pair<Chain, uint> chainAndIndex = GetReceiverChain(senderEphemeral);
            Chain chain = chainAndIndex.First();
            Chain.Types.MessageKey messageKeyStructure = Chain.Types.MessageKey.CreateBuilder()
                                                                      .SetCipherKey(ByteString.CopyFrom(messageKeys.GetCipherKey()/*.getEncoded()*/))
                                                                      .SetMacKey(ByteString.CopyFrom(messageKeys.GetMacKey()/*.getEncoded()*/))
                                                                      .SetIndex(messageKeys.GetCounter())
                                                                      .SetIv(ByteString.CopyFrom(messageKeys.GetIv()/*.getIV()*/))
                                                                      .Build();

            Chain.Builder updatedChain = chain.ToBuilder().AddMessageKeys(messageKeyStructure);
            if (updatedChain.MessageKeysList.Count > MAX_MESSAGE_KEYS)
            {
                updatedChain.MessageKeysList.RemoveAt(0);
            }

            this.sessionStructure = this.sessionStructure.ToBuilder()
                                                         .SetReceiverChains((int)chainAndIndex.Second(), updatedChain.Build()) // TODO: conv
                                                         .Build();
        }

        public void SetReceiverChainKey(ECPublicKey senderEphemeral, ChainKey chainKey)
        {
            Pair<Chain, uint> chainAndIndex = GetReceiverChain(senderEphemeral);
            Chain chain = chainAndIndex.First();

            Chain.Types.ChainKey chainKeyStructure = Chain.Types.ChainKey.CreateBuilder()
                                                             .SetKey(ByteString.CopyFrom(chainKey.GetKey()))
                                                             .SetIndex(chainKey.GetIndex())
                                                             .Build();

            Chain updatedChain = chain.ToBuilder().SetChainKey(chainKeyStructure).Build();

            this.sessionStructure = this.sessionStructure.ToBuilder()
                                                         .SetReceiverChains((int)chainAndIndex.Second(), updatedChain) // TODO: conv
                                                         .Build();
        }

        public void SetPendingKeyExchange(uint sequence,
                                          ECKeyPair ourBaseKey,
                                          ECKeyPair ourRatchetKey,
                                          IdentityKeyPair ourIdentityKey)
        {
            PendingKeyExchange structure =
                PendingKeyExchange.CreateBuilder()
                                  .SetSequence(sequence)
                                  .SetLocalBaseKey(ByteString.CopyFrom(ourBaseKey.GetPublicKey().Serialize()))
                                  .SetLocalBaseKeyPrivate(ByteString.CopyFrom(ourBaseKey.GetPrivateKey().Serialize()))
                                  .SetLocalRatchetKey(ByteString.CopyFrom(ourRatchetKey.GetPublicKey().Serialize()))
                                  .SetLocalRatchetKeyPrivate(ByteString.CopyFrom(ourRatchetKey.GetPrivateKey().Serialize()))
                                  .SetLocalIdentityKey(ByteString.CopyFrom(ourIdentityKey.GetPublicKey().Serialize()))
                                  .SetLocalIdentityKeyPrivate(ByteString.CopyFrom(ourIdentityKey.GetPrivateKey().Serialize()))
                                  .Build();

            this.sessionStructure = this.sessionStructure.ToBuilder()
                                                         .SetPendingKeyExchange(structure)
                                                         .Build();
        }

        public uint GetPendingKeyExchangeSequence()
        {
            return sessionStructure.PendingKeyExchange.Sequence;
        }

        public ECKeyPair GetPendingKeyExchangeBaseKey()
        {
            ECPublicKey publicKey = Curve.DecodePoint(sessionStructure.PendingKeyExchange
                                                                .LocalBaseKey.ToByteArray(), 0);

            ECPrivateKey privateKey = Curve.DecodePrivatePoint(sessionStructure.PendingKeyExchange
                                                                       .LocalBaseKeyPrivate
                                                                       .ToByteArray());

            return new ECKeyPair(publicKey, privateKey);
        }

        public ECKeyPair GetPendingKeyExchangeRatchetKey()
        {
            ECPublicKey publicKey = Curve.DecodePoint(sessionStructure.PendingKeyExchange
                                                                .LocalRatchetKey.ToByteArray(), 0);

            ECPrivateKey privateKey = Curve.DecodePrivatePoint(sessionStructure.PendingKeyExchange
                                                                       .LocalRatchetKeyPrivate
                                                                       .ToByteArray());

            return new ECKeyPair(publicKey, privateKey);
        }

        public IdentityKeyPair GetPendingKeyExchangeIdentityKey()
        {
            IdentityKey publicKey = new IdentityKey(sessionStructure.PendingKeyExchange
                                                            .LocalIdentityKey.ToByteArray(), 0);

            ECPrivateKey privateKey = Curve.DecodePrivatePoint(sessionStructure.PendingKeyExchange
                                                                       .LocalIdentityKeyPrivate
                                                                       .ToByteArray());

            return new IdentityKeyPair(publicKey, privateKey);
        }

        public bool HasPendingKeyExchange()
        {
            return sessionStructure.HasPendingKeyExchange;
        }

        public void SetUnacknowledgedPreKeyMessage(May<uint> preKeyId, uint signedPreKeyId, ECPublicKey baseKey)
        {
            PendingPreKey.Builder pending = PendingPreKey.CreateBuilder()
                                                         .SetSignedPreKeyId(signedPreKeyId)
                                                         .SetBaseKey(ByteString.CopyFrom(baseKey.Serialize()));

            if (preKeyId.HasValue)
            {
                pending.SetPreKeyId(preKeyId.ForceGetValue());
            }

            this.sessionStructure = this.sessionStructure.ToBuilder()
                                                         .SetPendingPreKey(pending.Build())
                                                         .Build();
        }

        public bool HasUnacknowledgedPreKeyMessage()
        {
            return this.sessionStructure.HasPendingPreKey;
        }

        public UnacknowledgedPreKeyMessageItems GetUnacknowledgedPreKeyMessageItems()
        {
            try
            {
                May<uint> preKeyId;

                if (sessionStructure.PendingPreKey.HasPreKeyId)
                {
                    preKeyId = new May<uint>(sessionStructure.PendingPreKey.PreKeyId);
                }
                else
                {
                    preKeyId = May<uint>.NoValue;
                }

                return
                    new UnacknowledgedPreKeyMessageItems(preKeyId,
                                                         sessionStructure.PendingPreKey.SignedPreKeyId,
                                                         Curve.DecodePoint(sessionStructure.PendingPreKey
                                                                                           .BaseKey
                                                                                           .ToByteArray(), 0));
            }
            catch (InvalidKeyException e)
            {
                throw new Exception(e.Message);
            }
        }

        public void ClearUnacknowledgedPreKeyMessage()
        {
            this.sessionStructure = this.sessionStructure.ToBuilder()
                                                         .ClearPendingPreKey()
                                                         .Build();
        }

        public void SetRemoteRegistrationId(uint registrationId)
        {
            this.sessionStructure = this.sessionStructure.ToBuilder()
                                                         .SetRemoteRegistrationId(registrationId)
                                                         .Build();
        }

        public uint GetRemoteRegistrationId()
        {
            return this.sessionStructure.RemoteRegistrationId;
        }

        public void SetLocalRegistrationId(uint registrationId)
        {
            this.sessionStructure = this.sessionStructure.ToBuilder()
                                                         .SetLocalRegistrationId(registrationId)
                                                         .Build();
        }

        public uint GetLocalRegistrationId()
        {
            return this.sessionStructure.LocalRegistrationId;
        }

        public byte[] Serialize()
        {
            return sessionStructure.ToByteArray();
        }

        public class UnacknowledgedPreKeyMessageItems
        {
            private readonly May<uint> preKeyId;
            private readonly uint signedPreKeyId;
            private readonly ECPublicKey baseKey;

            public UnacknowledgedPreKeyMessageItems(May<uint> preKeyId,
                                                    uint signedPreKeyId,
                                                    ECPublicKey baseKey)
            {
                this.preKeyId = preKeyId;
                this.signedPreKeyId = signedPreKeyId;
                this.baseKey = baseKey;
            }

            public May<uint> GetPreKeyId()
            {
                return preKeyId;
            }

            public uint GetSignedPreKeyId()
            {
                return signedPreKeyId;
            }

            public ECPublicKey GetBaseKey()
            {
                return baseKey;
            }
        }
    }
}