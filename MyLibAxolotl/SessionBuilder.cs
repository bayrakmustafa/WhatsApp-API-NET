using Strilanc.Value;
using System;

/**
 * Copyright (C) 2015 smndtrl
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

using Tr.Com.Eimza.LibAxolotl.Ecc;
using Tr.Com.Eimza.LibAxolotl.exceptions;
using Tr.Com.Eimza.LibAxolotl.Protocol;
using Tr.Com.Eimza.LibAxolotl.Ratchet;
using Tr.Com.Eimza.LibAxolotl.State;
using Tr.Com.Eimza.LibAxolotl.Util;

namespace Tr.Com.Eimza.LibAxolotl
{
    /**
 * SessionBuilder is responsible for setting up encrypted sessions.
 * Once a session has been established, {@link org.whispersystems.libaxolotl.SessionCipher}
 * can be used to encrypt/decrypt messages in that session.
 * <p>
 * Sessions are built from one of three different possible vectors:
 * <ol>
 *   <li>A {@link org.whispersystems.libaxolotl.state.PreKeyBundle} retrieved from a server.</li>
 *   <li>A {@link org.whispersystems.libaxolotl.protocol.PreKeyWhisperMessage} received from a client.</li>
 *   <li>A {@link org.whispersystems.libaxolotl.protocol.KeyExchangeMessage} sent to or received from a client.</li>
 * </ol>
 *
 * Sessions are constructed per recipientId + deviceId tuple.  Remote logical users are identified
 * by their recipientId, and each logical recipientId can have multiple physical devices.
 *
 * @author Moxie Marlinspike
 */

    public class SessionBuilder
    {
        private readonly SessionStore sessionStore;
        private readonly PreKeyStore preKeyStore;
        private readonly SignedPreKeyStore signedPreKeyStore;
        private readonly IdentityKeyStore identityKeyStore;
        private readonly AxolotlAddress remoteAddress;

        /**
         * Constructs a SessionBuilder.
         *
         * @param sessionStore The {@link org.whispersystems.libaxolotl.state.SessionStore} to store the constructed session in.
         * @param preKeyStore The {@link  org.whispersystems.libaxolotl.state.PreKeyStore} where the client's local {@link org.whispersystems.libaxolotl.state.PreKeyRecord}s are stored.
         * @param identityKeyStore The {@link org.whispersystems.libaxolotl.state.IdentityKeyStore} containing the client's identity key information.
         * @param remoteAddress The address of the remote user to build a session with.
         */

        public SessionBuilder(SessionStore sessionStore,
                              PreKeyStore preKeyStore,
                              SignedPreKeyStore signedPreKeyStore,
                              IdentityKeyStore identityKeyStore,
                              AxolotlAddress remoteAddress)
        {
            this.sessionStore = sessionStore;
            this.preKeyStore = preKeyStore;
            this.signedPreKeyStore = signedPreKeyStore;
            this.identityKeyStore = identityKeyStore;
            this.remoteAddress = remoteAddress;
        }

        /**
         * Constructs a SessionBuilder
         * @param store The {@link org.whispersystems.libaxolotl.state.AxolotlStore} to store all state information in.
         * @param remoteAddress The address of the remote user to build a session with.
         */

        public SessionBuilder(AxolotlStore store, AxolotlAddress remoteAddress)
            : this(store, store, store, store, remoteAddress)
        {
        }

        /**
         * Build a new session from a received {@link org.whispersystems.libaxolotl.protocol.PreKeyWhisperMessage}.
         *
         * After a session is constructed in this way, the embedded {@link org.whispersystems.libaxolotl.protocol.WhisperMessage}
         * can be decrypted.
         *
         * @param message The received {@link org.whispersystems.libaxolotl.protocol.PreKeyWhisperMessage}.
         * @throws org.whispersystems.libaxolotl.InvalidKeyIdException when there is no local
         *                                                             {@link org.whispersystems.libaxolotl.state.PreKeyRecord}
         *                                                             that corresponds to the PreKey ID in
         *                                                             the message.
         * @throws org.whispersystems.libaxolotl.InvalidKeyException when the message is formatted incorrectly.
         * @throws org.whispersystems.libaxolotl.UntrustedIdentityException when the {@link IdentityKey} of the sender is untrusted.
         */
        /*package*/

        internal May<uint> Process(SessionRecord sessionRecord, PreKeyWhisperMessage message)
        {
            uint messageVersion = message.GetMessageVersion();
            IdentityKey theirIdentityKey = message.GetIdentityKey();

            May<uint> unsignedPreKeyId;

            if (!identityKeyStore.IsTrustedIdentity(remoteAddress.GetName(), theirIdentityKey))
            {
                throw new UntrustedIdentityException(remoteAddress.GetName(), theirIdentityKey);
            }

            switch (messageVersion)
            {
                case 2:
                    unsignedPreKeyId = ProcessV2(sessionRecord, message);
                    break;

                case 3:
                    unsignedPreKeyId = ProcessV3(sessionRecord, message);
                    break;

                default:
                    throw new Exception("Unknown version: " + messageVersion);
            }

            identityKeyStore.SaveIdentity(remoteAddress.GetName(), theirIdentityKey);
            return unsignedPreKeyId;
        }

        private May<uint> ProcessV3(SessionRecord sessionRecord, PreKeyWhisperMessage message)
        {
            if (sessionRecord.HasSessionState(message.GetMessageVersion(), message.GetBaseKey().Serialize()))
            {
                return May<uint>.NoValue;
            }

            SignedPreKeyRecord signedPreKeyRecord = signedPreKeyStore.LoadSignedPreKey(message.GetSignedPreKeyId());
            ECKeyPair ourSignedPreKey = signedPreKeyRecord.GetKeyPair();

            BobAxolotlParameters.Builder parameters = BobAxolotlParameters.NewBuilder();

            parameters.SetTheirBaseKey(message.GetBaseKey())
                      .SetTheirIdentityKey(message.GetIdentityKey())
                      .SetOurIdentityKey(identityKeyStore.GetIdentityKeyPair())
                      .SetOurSignedPreKey(ourSignedPreKey)
                      .SetOurRatchetKey(ourSignedPreKey);

            if (message.GetPreKeyId().HasValue)
            {
                parameters.SetOurOneTimePreKey(new May<ECKeyPair>(preKeyStore.LoadPreKey(message.GetPreKeyId().ForceGetValue()).GetKeyPair()));
            }
            else
            {
                parameters.SetOurOneTimePreKey(May<ECKeyPair>.NoValue);
            }

            if (!sessionRecord.IsFresh())
                sessionRecord.ArchiveCurrentState();

            RatchetingSession.InitializeSession(sessionRecord.GetSessionState(), message.GetMessageVersion(), parameters.Create());

            sessionRecord.GetSessionState().SetLocalRegistrationId(identityKeyStore.GetLocalRegistrationId());
            sessionRecord.GetSessionState().SetRemoteRegistrationId(message.GetRegistrationId());
            sessionRecord.GetSessionState().SetAliceBaseKey(message.GetBaseKey().Serialize());

            if (message.GetPreKeyId().HasValue && message.GetPreKeyId().ForceGetValue() != Medium.MAX_VALUE)
            {
                return message.GetPreKeyId();
            }
            else
            {
                return May<uint>.NoValue;
            }
        }

        private May<uint> ProcessV2(SessionRecord sessionRecord, PreKeyWhisperMessage message)
        {
            if (!message.GetPreKeyId().HasValue)
            {
                throw new InvalidKeyIdException("V2 message requires one time prekey id!");
            }

            if (!preKeyStore.ContainsPreKey(message.GetPreKeyId().ForceGetValue()) &&
                sessionStore.ContainsSession(remoteAddress))
            {
                return May<uint>.NoValue;
            }

            ECKeyPair ourPreKey = preKeyStore.LoadPreKey(message.GetPreKeyId().ForceGetValue()).GetKeyPair();

            BobAxolotlParameters.Builder parameters = BobAxolotlParameters.NewBuilder();

            parameters.SetOurIdentityKey(identityKeyStore.GetIdentityKeyPair())
                          .SetOurSignedPreKey(ourPreKey)
                          .SetOurRatchetKey(ourPreKey)
                          .SetOurOneTimePreKey(May<ECKeyPair>.NoValue) //absent
                          .SetTheirIdentityKey(message.GetIdentityKey())
                          .SetTheirBaseKey(message.GetBaseKey());

            if (!sessionRecord.IsFresh())
                sessionRecord.ArchiveCurrentState();

            RatchetingSession.InitializeSession(sessionRecord.GetSessionState(), message.GetMessageVersion(), parameters.Create());

            sessionRecord.GetSessionState().SetLocalRegistrationId(identityKeyStore.GetLocalRegistrationId());
            sessionRecord.GetSessionState().SetRemoteRegistrationId(message.GetRegistrationId());
            sessionRecord.GetSessionState().SetAliceBaseKey(message.GetBaseKey().Serialize());

            if (message.GetPreKeyId().ForceGetValue() != Medium.MAX_VALUE)
            {
                return message.GetPreKeyId();
            }
            else
            {
                return May<uint>.NoValue; // May.absent();
            }
        }

        /**
         * Build a new session from a {@link org.whispersystems.libaxolotl.state.PreKeyBundle} retrieved from
         * a server.
         *
         * @param preKey A PreKey for the destination recipient, retrieved from a server.
         * @throws InvalidKeyException when the {@link org.whispersystems.libaxolotl.state.PreKeyBundle} is
         *                             badly formatted.
         * @throws org.whispersystems.libaxolotl.UntrustedIdentityException when the sender's
         *                                                                  {@link IdentityKey} is not
         *                                                                  trusted.
         */

        public void Process(PreKeyBundle preKey)
        {
            lock (SessionCipher.SESSION_LOCK)
            {
                if (!identityKeyStore.IsTrustedIdentity(remoteAddress.GetName(), preKey.GetIdentityKey()))
                {
                    throw new UntrustedIdentityException(remoteAddress.GetName(), preKey.GetIdentityKey());
                }

                if (preKey.GetSignedPreKey() != null &&
                    !Curve.VerifySignature(preKey.GetIdentityKey().GetPublicKey(),
                                           preKey.GetSignedPreKey().Serialize(),
                                           preKey.GetSignedPreKeySignature()))
                {
                    throw new InvalidKeyException("Invalid Signature on Device Key!");
                }

                if (preKey.GetSignedPreKey() == null && preKey.GetPreKey() == null)
                {
                    throw new InvalidKeyException("Both Signed and Unsigned Prekeys are Absent!");
                }

                bool supportsV3 = preKey.GetSignedPreKey() != null;
                SessionRecord sessionRecord = sessionStore.LoadSession(remoteAddress);
                ECKeyPair ourBaseKey = Curve.GenerateKeyPair();
                ECPublicKey theirSignedPreKey = supportsV3 ? preKey.GetSignedPreKey() : preKey.GetPreKey();
                ECPublicKey test = preKey.GetPreKey(); // TODO: Cleanup
                May<ECPublicKey> theirOneTimePreKey = (test == null) ? May<ECPublicKey>.NoValue : new May<ECPublicKey>(test);
                May<uint> theirOneTimePreKeyId = theirOneTimePreKey.HasValue ? new May<uint>(preKey.GetPreKeyId()) : May<uint>.NoValue;

                AliceAxolotlParameters.Builder parameters = AliceAxolotlParameters.NewBuilder();

                parameters.SetOurBaseKey(ourBaseKey)
                              .SetOurIdentityKey(identityKeyStore.GetIdentityKeyPair())
                              .SetTheirIdentityKey(preKey.GetIdentityKey())
                              .SetTheirSignedPreKey(theirSignedPreKey)
                              .SetTheirRatchetKey(theirSignedPreKey)
                              .SetTheirOneTimePreKey(supportsV3 ? theirOneTimePreKey : May<ECPublicKey>.NoValue);

                if (!sessionRecord.IsFresh())
                    sessionRecord.ArchiveCurrentState();

                RatchetingSession.InitializeSession(sessionRecord.GetSessionState(),
                                                    supportsV3 ? (uint)3 : 2,
                                                    parameters.Create());

                sessionRecord.GetSessionState().SetUnacknowledgedPreKeyMessage(theirOneTimePreKeyId, preKey.GetSignedPreKeyId(), ourBaseKey.GetPublicKey());
                sessionRecord.GetSessionState().SetLocalRegistrationId(identityKeyStore.GetLocalRegistrationId());
                sessionRecord.GetSessionState().SetRemoteRegistrationId(preKey.GetRegistrationId());
                sessionRecord.GetSessionState().SetAliceBaseKey(ourBaseKey.GetPublicKey().Serialize());

                sessionStore.StoreSession(remoteAddress, sessionRecord);
                identityKeyStore.SaveIdentity(remoteAddress.GetName(), preKey.GetIdentityKey());
            }
        }

        /**
         * Build a new session from a {@link org.whispersystems.libaxolotl.protocol.KeyExchangeMessage}
         * received from a remote client.
         *
         * @param message The received KeyExchangeMessage.
         * @return The KeyExchangeMessage to respond with, or null if no response is necessary.
         * @throws InvalidKeyException if the received KeyExchangeMessage is badly formatted.
         */

        public KeyExchangeMessage Process(KeyExchangeMessage message)

        {
            lock (SessionCipher.SESSION_LOCK)
            {
                if (!identityKeyStore.IsTrustedIdentity(remoteAddress.GetName(), message.GetIdentityKey()))
                {
                    throw new UntrustedIdentityException(remoteAddress.GetName(), message.GetIdentityKey());
                }

                KeyExchangeMessage responseMessage = null;

                if (message.IsInitiate())
                    responseMessage = ProcessInitiate(message);
                else
                    ProcessResponse(message);

                return responseMessage;
            }
        }

        private KeyExchangeMessage ProcessInitiate(KeyExchangeMessage message)
        {
            uint flags = KeyExchangeMessage.RESPONSE_FLAG;
            SessionRecord sessionRecord = sessionStore.LoadSession(remoteAddress);

            if (message.GetVersion() >= 3 &&
                !Curve.VerifySignature(message.GetIdentityKey().GetPublicKey(),
                                       message.GetBaseKey().Serialize(),
                                       message.GetBaseKeySignature()))
            {
                throw new InvalidKeyException("Bad signature!");
            }

            SymmetricAxolotlParameters.Builder builder = SymmetricAxolotlParameters.NewBuilder();

            if (!sessionRecord.GetSessionState().HasPendingKeyExchange())
            {
                builder.SetOurIdentityKey(identityKeyStore.GetIdentityKeyPair())
                       .SetOurBaseKey(Curve.GenerateKeyPair())
                       .SetOurRatchetKey(Curve.GenerateKeyPair());
            }
            else
            {
                builder.SetOurIdentityKey(sessionRecord.GetSessionState().GetPendingKeyExchangeIdentityKey())
                       .SetOurBaseKey(sessionRecord.GetSessionState().GetPendingKeyExchangeBaseKey())
                       .SetOurRatchetKey(sessionRecord.GetSessionState().GetPendingKeyExchangeRatchetKey());
                flags |= KeyExchangeMessage.SIMULTAENOUS_INITIATE_FLAG;
            }

            builder.SetTheirBaseKey(message.GetBaseKey())
                   .SetTheirRatchetKey(message.GetRatchetKey())
                   .SetTheirIdentityKey(message.GetIdentityKey());

            SymmetricAxolotlParameters parameters = builder.Create();

            if (!sessionRecord.IsFresh())
                sessionRecord.ArchiveCurrentState();

            RatchetingSession.InitializeSession(sessionRecord.GetSessionState(),
                                                Math.Min(message.GetMaxVersion(), CipherTextMessage.CURRENT_VERSION),
                                                parameters);

            sessionStore.StoreSession(remoteAddress, sessionRecord);
            identityKeyStore.SaveIdentity(remoteAddress.GetName(), message.GetIdentityKey());

            byte[] baseKeySignature = Curve.CalculateSignature(parameters.GetOurIdentityKey().GetPrivateKey(),
                                                               parameters.GetOurBaseKey().GetPublicKey().Serialize());

            return new KeyExchangeMessage(sessionRecord.GetSessionState().GetSessionVersion(),
                                          message.GetSequence(), flags,
                                          parameters.GetOurBaseKey().GetPublicKey(),
                                          baseKeySignature, parameters.GetOurRatchetKey().GetPublicKey(),
                                          parameters.GetOurIdentityKey().GetPublicKey());
        }

        private void ProcessResponse(KeyExchangeMessage message)
        {
            SessionRecord sessionRecord = sessionStore.LoadSession(remoteAddress);
            SessionState sessionState = sessionRecord.GetSessionState();
            bool hasPendingKeyExchange = sessionState.HasPendingKeyExchange();
            bool isSimultaneousInitiateResponse = message.IsResponseForSimultaneousInitiate();

            if (!hasPendingKeyExchange || sessionState.GetPendingKeyExchangeSequence() != message.GetSequence())
            {
                //Log.w(TAG, "No matching sequence for response. Is simultaneous initiate response: " + isSimultaneousInitiateResponse);
                if (!isSimultaneousInitiateResponse)
                    throw new StaleKeyExchangeException();
                else
                    return;
            }

            SymmetricAxolotlParameters.Builder parameters = SymmetricAxolotlParameters.NewBuilder();

            parameters.SetOurBaseKey(sessionRecord.GetSessionState().GetPendingKeyExchangeBaseKey())
                      .SetOurRatchetKey(sessionRecord.GetSessionState().GetPendingKeyExchangeRatchetKey())
                      .SetOurIdentityKey(sessionRecord.GetSessionState().GetPendingKeyExchangeIdentityKey())
                      .SetTheirBaseKey(message.GetBaseKey())
                      .SetTheirRatchetKey(message.GetRatchetKey())
                      .SetTheirIdentityKey(message.GetIdentityKey());

            if (!sessionRecord.IsFresh())
                sessionRecord.ArchiveCurrentState();

            RatchetingSession.InitializeSession(sessionRecord.GetSessionState(),
                                                Math.Min(message.GetMaxVersion(), CipherTextMessage.CURRENT_VERSION),
                                                parameters.Create());

            if (sessionRecord.GetSessionState().GetSessionVersion() >= 3 &&
                !Curve.VerifySignature(message.GetIdentityKey().GetPublicKey(),
                                       message.GetBaseKey().Serialize(),
                                       message.GetBaseKeySignature()))
            {
                throw new InvalidKeyException("Base key signature doesn't match!");
            }

            sessionStore.StoreSession(remoteAddress, sessionRecord);
            identityKeyStore.SaveIdentity(remoteAddress.GetName(), message.GetIdentityKey());
        }

        /**
         * Initiate a new session by sending an initial KeyExchangeMessage to the recipient.
         *
         * @return the KeyExchangeMessage to deliver.
         */

        public KeyExchangeMessage Process()
        {
            lock (SessionCipher.SESSION_LOCK)
            {
                try
                {
                    uint sequence = KeyHelper.GetRandomSequence(65534) + 1;
                    uint flags = KeyExchangeMessage.INITIATE_FLAG;
                    ECKeyPair baseKey = Curve.GenerateKeyPair();
                    ECKeyPair ratchetKey = Curve.GenerateKeyPair();
                    IdentityKeyPair identityKey = identityKeyStore.GetIdentityKeyPair();
                    byte[] baseKeySignature = Curve.CalculateSignature(identityKey.GetPrivateKey(), baseKey.GetPublicKey().Serialize());
                    SessionRecord sessionRecord = sessionStore.LoadSession(remoteAddress);

                    sessionRecord.GetSessionState().SetPendingKeyExchange(sequence, baseKey, ratchetKey, identityKey);
                    sessionStore.StoreSession(remoteAddress, sessionRecord);

                    return new KeyExchangeMessage(2, sequence, flags, baseKey.GetPublicKey(), baseKeySignature, ratchetKey.GetPublicKey(), identityKey.GetPublicKey());
                }
                catch (InvalidKeyException e)
                {
                    throw new Exception(e.Message);
                }
            }
        }
    }
}