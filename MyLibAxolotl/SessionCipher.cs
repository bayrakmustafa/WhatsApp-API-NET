using Strilanc.Value;
using System;
using System.Collections.Generic;

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
using Tr.Com.Eimza.LibAxolotl.Protocol;
using Tr.Com.Eimza.LibAxolotl.Ratchet;
using Tr.Com.Eimza.LibAxolotl.State;
using Tr.Com.Eimza.LibAxolotl.Util;

namespace Tr.Com.Eimza.LibAxolotl
{
    /**
     * The main entry point for Axolotl encrypt/decrypt operations.
     *
     * Once a session has been established with {@link SessionBuilder},
     * this class can be used for all encrypt/decrypt operations within
     * that session.
     *
     * @author Moxie Marlinspike
     */

    public class SessionCipher
    {
        public static readonly Object SESSION_LOCK = new Object();

        private readonly SessionStore sessionStore;
        private readonly SessionBuilder sessionBuilder;
        private readonly PreKeyStore preKeyStore;
        private readonly AxolotlAddress remoteAddress;

        /**
         * Construct a SessionCipher for encrypt/decrypt operations on a session.
         * In order to use SessionCipher, a session must have already been created
         * and stored using {@link SessionBuilder}.
         *
         * @param  sessionStore The {@link SessionStore} that contains a session for this recipient.
         * @param  remoteAddress  The remote address that messages will be encrypted to or decrypted from.
         */

        public SessionCipher(SessionStore sessionStore, PreKeyStore preKeyStore,
                             SignedPreKeyStore signedPreKeyStore, IdentityKeyStore identityKeyStore,
                             AxolotlAddress remoteAddress)
        {
            this.sessionStore = sessionStore;
            this.preKeyStore = preKeyStore;
            this.remoteAddress = remoteAddress;
            this.sessionBuilder = new SessionBuilder(sessionStore, preKeyStore, signedPreKeyStore,
                                                     identityKeyStore, remoteAddress);
        }

        public SessionCipher(AxolotlStore store, AxolotlAddress remoteAddress)
            : this(store, store, store, store, remoteAddress)
        {
        }

        /**
         * Encrypt a message.
         *
         * @param  paddedMessage The plaintext message bytes, optionally padded to a constant multiple.
         * @return A ciphertext message encrypted to the recipient+device tuple.
         */

        public CiphertextMessage Encrypt(byte[] paddedMessage)
        {
            lock (SESSION_LOCK)
            {
                SessionRecord sessionRecord = sessionStore.LoadSession(remoteAddress);
                SessionState sessionState = sessionRecord.GetSessionState();
                ChainKey chainKey = sessionState.GetSenderChainKey();
                MessageKeys messageKeys = chainKey.GetMessageKeys();
                ECPublicKey senderEphemeral = sessionState.GetSenderRatchetKey();
                uint previousCounter = sessionState.GetPreviousCounter();
                uint sessionVersion = sessionState.GetSessionVersion();

                byte[] ciphertextBody = GetCiphertext(sessionVersion, messageKeys, paddedMessage);
                CiphertextMessage ciphertextMessage = new WhisperMessage(sessionVersion, messageKeys.GetMacKey(),
                                                                         senderEphemeral, chainKey.GetIndex(),
                                                                         previousCounter, ciphertextBody,
                                                                         sessionState.GetLocalIdentityKey(),
                                                                         sessionState.GetRemoteIdentityKey());

                if (sessionState.HasUnacknowledgedPreKeyMessage())
                {
                    SessionState.UnacknowledgedPreKeyMessageItems items = sessionState.GetUnacknowledgedPreKeyMessageItems();
                    uint localRegistrationId = sessionState.GetLocalRegistrationId();

                    ciphertextMessage = new PreKeyWhisperMessage(sessionVersion, localRegistrationId, items.GetPreKeyId(),
                                                                 items.GetSignedPreKeyId(), items.GetBaseKey(),
                                                                 sessionState.GetLocalIdentityKey(),
                                                                 (WhisperMessage)ciphertextMessage);
                }

                sessionState.SetSenderChainKey(chainKey.GetNextChainKey());
                sessionStore.StoreSession(remoteAddress, sessionRecord);
                return ciphertextMessage;
            }
        }

        /**
         * Decrypt a message.
         *
         * @param  ciphertext The {@link PreKeyWhisperMessage} to decrypt.
         *
         * @return The plaintext.
         * @throws InvalidMessageException if the input is not valid ciphertext.
         * @throws DuplicateMessageException if the input is a message that has already been received.
         * @throws LegacyMessageException if the input is a message formatted by a protocol version that
         *                                is no longer supported.
         * @throws InvalidKeyIdException when there is no local {@link org.whispersystems.libaxolotl.state.PreKeyRecord}
         *                               that corresponds to the PreKey ID in the message.
         * @throws InvalidKeyException when the message is formatted incorrectly.
         * @throws UntrustedIdentityException when the {@link IdentityKey} of the sender is untrusted.
         */

        public byte[] Decrypt(PreKeyWhisperMessage ciphertext)

        {
            return Decrypt(ciphertext, new NullDecryptionCallback());
        }

        /**
         * Decrypt a message.
         *
         * @param  ciphertext The {@link PreKeyWhisperMessage} to decrypt.
         * @param  callback   A callback that is triggered after decryption is complete,
         *                    but before the updated session state has been committed to the session
         *                    DB.  This allows some implementations to store the committed plaintext
         *                    to a DB first, in case they are concerned with a crash happening between
         *                    the time the session state is updated but before they're able to store
         *                    the plaintext to disk.
         *
         * @return The plaintext.
         * @throws InvalidMessageException if the input is not valid ciphertext.
         * @throws DuplicateMessageException if the input is a message that has already been received.
         * @throws LegacyMessageException if the input is a message formatted by a protocol version that
         *                                is no longer supported.
         * @throws InvalidKeyIdException when there is no local {@link org.whispersystems.libaxolotl.state.PreKeyRecord}
         *                               that corresponds to the PreKey ID in the message.
         * @throws InvalidKeyException when the message is formatted incorrectly.
         * @throws UntrustedIdentityException when the {@link IdentityKey} of the sender is untrusted.
         */

        public byte[] Decrypt(PreKeyWhisperMessage ciphertext, DecryptionCallback callback)

        {
            lock (SESSION_LOCK)
            {
                SessionRecord sessionRecord = sessionStore.LoadSession(remoteAddress);
                May<uint> unsignedPreKeyId = sessionBuilder.Process(sessionRecord, ciphertext);
                byte[] plaintext = Decrypt(sessionRecord, ciphertext.GetWhisperMessage());

                callback.HandlePlaintext(plaintext);

                sessionStore.StoreSession(remoteAddress, sessionRecord);

                if (unsignedPreKeyId.HasValue)
                {
                    preKeyStore.RemovePreKey(unsignedPreKeyId.ForceGetValue());
                }

                return plaintext;
            }
        }

        /**
         * Decrypt a message.
         *
         * @param  ciphertext The {@link WhisperMessage} to decrypt.
         *
         * @return The plaintext.
         * @throws InvalidMessageException if the input is not valid ciphertext.
         * @throws DuplicateMessageException if the input is a message that has already been received.
         * @throws LegacyMessageException if the input is a message formatted by a protocol version that
         *                                is no longer supported.
         * @throws NoSessionException if there is no established session for this contact.
         */

        public byte[] Decrypt(WhisperMessage ciphertext)

        {
            return Decrypt(ciphertext, new NullDecryptionCallback());
        }

        /**
         * Decrypt a message.
         *
         * @param  ciphertext The {@link WhisperMessage} to decrypt.
         * @param  callback   A callback that is triggered after decryption is complete,
         *                    but before the updated session state has been committed to the session
         *                    DB.  This allows some implementations to store the committed plaintext
         *                    to a DB first, in case they are concerned with a crash happening between
         *                    the time the session state is updated but before they're able to store
         *                    the plaintext to disk.
         *
         * @return The plaintext.
         * @throws InvalidMessageException if the input is not valid ciphertext.
         * @throws DuplicateMessageException if the input is a message that has already been received.
         * @throws LegacyMessageException if the input is a message formatted by a protocol version that
         *                                is no longer supported.
         * @throws NoSessionException if there is no established session for this contact.
         */

        public byte[] Decrypt(WhisperMessage ciphertext, DecryptionCallback callback)

        {
            lock (SESSION_LOCK)
            {
                if (!sessionStore.ContainsSession(remoteAddress))
                {
                    throw new NoSessionException($"No session for: {remoteAddress}");
                }

                SessionRecord sessionRecord = sessionStore.LoadSession(remoteAddress);
                byte[] plaintext = Decrypt(sessionRecord, ciphertext);

                callback.HandlePlaintext(plaintext);

                sessionStore.StoreSession(remoteAddress, sessionRecord);

                return plaintext;
            }
        }

        private byte[] Decrypt(SessionRecord sessionRecord, WhisperMessage ciphertext)
        {
            lock (SESSION_LOCK)
            {
                IEnumerator<SessionState> previousStates = sessionRecord.GetPreviousSessionStates().GetEnumerator(); //iterator
                LinkedList<Exception> exceptions = new LinkedList<Exception>();

                try
                {
                    SessionState sessionState = new SessionState(sessionRecord.GetSessionState());
                    byte[] plaintext = Decrypt(sessionState, ciphertext);

                    sessionRecord.SetState(sessionState);
                    return plaintext;
                }
                catch (InvalidMessageException e)
                {
                    exceptions.AddLast(e); // add (java default behavioir addlast)
                }

                while (previousStates.MoveNext()) //hasNext();
                {
                    try
                    {
                        SessionState promotedState = new SessionState(previousStates.Current); //.next()
                        byte[] plaintext = Decrypt(promotedState, ciphertext);

                        sessionRecord.GetPreviousSessionStates().Remove(previousStates.Current); // previousStates.remove()
                        sessionRecord.PromoteState(promotedState);

                        return plaintext;
                    }
                    catch (InvalidMessageException e)
                    {
                        exceptions.AddLast(e);
                    }
                }

                throw new InvalidMessageException("No valid sessions.", exceptions);
            }
        }

        private byte[] Decrypt(SessionState sessionState, WhisperMessage ciphertextMessage)
        {
            if (!sessionState.HasSenderChain())
            {
                throw new InvalidMessageException("Uninitialized session!");
            }

            if (ciphertextMessage.GetMessageVersion() != sessionState.GetSessionVersion())
            {
                throw new InvalidMessageException($"Message version {ciphertextMessage.GetMessageVersion()}, but session version {sessionState.GetSessionVersion()}");
            }

            uint messageVersion = ciphertextMessage.GetMessageVersion();
            ECPublicKey theirEphemeral = ciphertextMessage.GetSenderRatchetKey();
            uint counter = ciphertextMessage.GetCounter();
            ChainKey chainKey = GetOrCreateChainKey(sessionState, theirEphemeral);
            MessageKeys messageKeys = GetOrCreateMessageKeys(sessionState, theirEphemeral,
                                                                      chainKey, counter);

            ciphertextMessage.VerifyMac(messageVersion,
                                            sessionState.GetRemoteIdentityKey(),
                                            sessionState.GetLocalIdentityKey(),
                                            messageKeys.GetMacKey());

            byte[] plaintext = GetPlaintext(messageVersion, messageKeys, ciphertextMessage.GetBody());

            sessionState.ClearUnacknowledgedPreKeyMessage();

            return plaintext;
        }

        public uint GetRemoteRegistrationId()
        {
            lock (SESSION_LOCK)
            {
                SessionRecord record = sessionStore.LoadSession(remoteAddress);
                return record.GetSessionState().GetRemoteRegistrationId();
            }
        }

        public uint GetSessionVersion()
        {
            lock (SESSION_LOCK)
            {
                if (!sessionStore.ContainsSession(remoteAddress))
                {
                    throw new Exception($"No session for {remoteAddress}!"); // IllegalState
                }

                SessionRecord record = sessionStore.LoadSession(remoteAddress);
                return record.GetSessionState().GetSessionVersion();
            }
        }

        private ChainKey GetOrCreateChainKey(SessionState sessionState, ECPublicKey theirEphemeral)

        {
            try
            {
                if (sessionState.HasReceiverChain(theirEphemeral))
                {
                    return sessionState.GetReceiverChainKey(theirEphemeral);
                }
                else
                {
                    RootKey rootKey = sessionState.GetRootKey();
                    ECKeyPair ourEphemeral = sessionState.GetSenderRatchetKeyPair();
                    Pair<RootKey, ChainKey> receiverChain = rootKey.CreateChain(theirEphemeral, ourEphemeral);
                    ECKeyPair ourNewEphemeral = Curve.GenerateKeyPair();
                    Pair<RootKey, ChainKey> senderChain = receiverChain.First().CreateChain(theirEphemeral, ourNewEphemeral);

                    sessionState.SetRootKey(senderChain.First());
                    sessionState.AddReceiverChain(theirEphemeral, receiverChain.Second());
                    sessionState.SetPreviousCounter(Math.Max(sessionState.GetSenderChainKey().GetIndex() - 1, 0));
                    sessionState.SetSenderChain(ourNewEphemeral, senderChain.Second());

                    return receiverChain.Second();
                }
            }
            catch (InvalidKeyException e)
            {
                throw new InvalidMessageException(e);
            }
        }

        private MessageKeys GetOrCreateMessageKeys(SessionState sessionState,
                                                   ECPublicKey theirEphemeral,
                                                   ChainKey chainKey, uint counter)

        {
            if (chainKey.GetIndex() > counter)
            {
                if (sessionState.HasMessageKeys(theirEphemeral, counter))
                {
                    return sessionState.RemoveMessageKeys(theirEphemeral, counter);
                }
                else
                {
                    throw new DuplicateMessageException($"Received message with old counter: {chainKey.GetIndex()}  , {counter}");
                }
            }

            //Avoiding a uint overflow
            uint chainKeyIndex = chainKey.GetIndex();
            if ((counter > chainKeyIndex) && (counter - chainKeyIndex > 2000))
            {
                throw new InvalidMessageException("Over 2000 messages into the future!");
            }

            while (chainKey.GetIndex() < counter)
            {
                MessageKeys messageKeys = chainKey.GetMessageKeys();
                sessionState.SetMessageKeys(theirEphemeral, messageKeys);
                chainKey = chainKey.GetNextChainKey();
            }

            sessionState.SetReceiverChainKey(theirEphemeral, chainKey.GetNextChainKey());
            return chainKey.GetMessageKeys();
        }

        private byte[] GetCiphertext(uint version, MessageKeys messageKeys, byte[] plaintext)
        {
            try
            {
                if (version >= 3)
                {
                    //cipher = getCipher(Cipher.ENCRYPT_MODE, messageKeys.getCipherKey(), messageKeys.getIv());
                    return Tr.Com.Eimza.LibAxolotl.Util.Encrypt.AesCbcPkcs5(plaintext, messageKeys.GetCipherKey(), messageKeys.GetIv());
                }
                else
                {
                    //cipher = getCipher(Cipher.ENCRYPT_MODE, messageKeys.getCipherKey(), messageKeys.getCounter());
                    return Tr.Com.Eimza.LibAxolotl.Util.Encrypt.AesCtr(plaintext, messageKeys.GetCipherKey(), messageKeys.GetCounter());
                }
            }
            catch (/*IllegalBlockSizeException | BadPadding*/Exception e)
            {
                throw new Exception(e.Message);
            }
        }

        private byte[] GetPlaintext(uint version, MessageKeys messageKeys, byte[] cipherText)
        {
            try
            {
                //Cipher cipher;

                if (version >= 3)
                {
                    //cipher = getCipher(Cipher.DECRYPT_MODE, messageKeys.getCipherKey(), messageKeys.getIv());
                    return Tr.Com.Eimza.LibAxolotl.Util.Decrypt.AesCbcPkcs5(cipherText, messageKeys.GetCipherKey(), messageKeys.GetIv());
                }
                else
                {
                    //cipher = getCipher(Cipher.DECRYPT_MODE, messageKeys.getCipherKey(), messageKeys.getCounter())
                    return Tr.Com.Eimza.LibAxolotl.Util.Decrypt.AesCtr(cipherText, messageKeys.GetCipherKey(), messageKeys.GetCounter());
                }
            }
            catch (/*IllegalBlockSizeException | BadPadding*/Exception e)
            {
                throw new InvalidMessageException(e);
            }
        }

        private class NullDecryptionCallback : DecryptionCallback
        {
            public void HandlePlaintext(byte[] plaintext)
            {
            }
        }
    }
}