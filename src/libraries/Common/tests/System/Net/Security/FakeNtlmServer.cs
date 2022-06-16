// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Buffers.Binary;
using System.Text;
using System.Net;
using System.Security.Cryptography;
using System.Net.Security;
using Xunit;

namespace System.Net.Security
{
    // Implementation of subset of the NTLM specification
    // https://docs.microsoft.com/en-us/openspecs/windows_protocols/ms-nlmp/b38c36ed-2804-4868-a9ff-8dd3182128e4
    //
    // Server-side implementation of the NTLMv2 exchange is implemented with
    // basic verification of the messages passed by the client against a
    // specified set of authentication credentials.
    //
    // This is not indended as a production-quality code for implementing the
    // NTLM authentication. It's merely to serve as a validation of challenges
    // and responses for unit test purposes. The validation checks the
    // structure of the messages, their integrity and use of specified
    // features (eg. MIC).
    internal class FakeNtlmServer
    {
        public FakeNtlmServer(NetworkCredential expectedCredential)
        {
            _expectedCredential = expectedCredential;
        }

        // Behavior modifiers
        public bool SendTimestamp { get; set; } = true;
        public byte[] Version { get; set; } = new byte[] { 0x06, 0x00, 0x70, 0x17, 0x00, 0x00, 0x00, 0x0f }; // 6.0.6000 / 15
        public bool TargetIsServer { get; set; } = false;
        public bool PreferUnicode { get; set; } = true;
        public bool ForceNegotiateVersion { get; set; } = true;

        // Negotiation results
        public bool IsAuthenticated { get; private set; }
        public bool IsMICPresent { get; private set; }
        public string? ClientSpecifiedSpn { get; private set; }

        private NetworkCredential _expectedCredential;

        // Saved Negotiate and Challenge messages for MIC calculation
        private byte[]? _negotiateMessage;
        private byte[]? _challengeMessage;

        // Established signing and sealing keys
        private byte[]? _clientSigningKey;
        private byte[]? _serverSigningKey;
        internal RC4? _clientSeal;
        internal RC4? _serverSeal;
        private Flags _negotiatedFlags;

        private MessageType _expectedMessageType = MessageType.Negotiate;

        // Minimal set of required negotiation flags
        private const Flags _requiredFlags =
            Flags.NegotiateNtlm2 | Flags.NegotiateNtlm | Flags.NegotiateAlwaysSign;

        // Fixed server challenge (same value as in Protocol Examples section of the specification)
        private byte[] _serverChallenge = new byte[] { 0x01, 0x23, 0x45, 0x67, 0x89, 0xab, 0xcd, 0xef };

        private static ReadOnlySpan<byte> NtlmHeader => "NTLMSSP\0"u8;
        private static ReadOnlySpan<byte> ClientSigningKeyMagic => "session key to client-to-server signing key magic constant\0"u8;
        private static ReadOnlySpan<byte> ServerSigningKeyMagic => "session key to server-to-client signing key magic constant\0"u8;
        private static ReadOnlySpan<byte> ClientSealingKeyMagic => "session key to client-to-server sealing key magic constant\0"u8;
        private static ReadOnlySpan<byte> ServerSealingKeyMagic => "session key to server-to-client sealing key magic constant\0"u8;

        private enum MessageType : uint
        {
            Negotiate = 1,
            Challenge = 2,
            Authenticate = 3,
        }

        [Flags]
        private enum Flags : uint
        {
            NegotiateUnicode = 0x00000001,
            NegotiateOEM = 0x00000002,
            RequestTargetName = 0x00000004,
            NegotiateSign = 0x00000010,
            NegotiateSeal = 0x00000020,
            NegotiateDatagram = 0x00000040,
            NegotiateLMKey = 0x00000080,
            NegotiateNtlm = 0x00000200,
            NegotiateAnonymous = 0x00000800,
            NegotiateDomainSupplied = 0x00001000,
            NegotiateWorkstationSupplied = 0x00002000,
            NegotiateAlwaysSign = 0x00008000,
            TargetTypeDomain = 0x00010000,
            TargetTypeServer = 0x00020000,
            NegotiateNtlm2 = 0x00080000,
            RequestIdenityToken = 0x00100000,
            RequestNonNtSessionKey = 0x00400000,
            NegotiateTargetInfo = 0x00800000,
            NegotiateVersion = 0x02000000,
            Negotiate128 = 0x20000000,
            NegotiateKeyExchange = 0x40000000,
            Negotiate56 = 0x80000000,

            AllSupported =
                NegotiateUnicode | NegotiateOEM | RequestTargetName |
                NegotiateSign | NegotiateSeal | NegotiateDatagram |
                /* NegotiateLMKey | */ NegotiateNtlm | /* NegotiateAnonymous | */
                /* NegotiateDomainSupplied | NegotiateWorkstationSupplied | */
                NegotiateAlwaysSign | TargetTypeDomain | TargetTypeServer |
                NegotiateNtlm2 | /* RequestIdenityToken | RequestNonNtSessionKey | */
                NegotiateTargetInfo | NegotiateVersion | Negotiate128 |
                NegotiateKeyExchange | Negotiate56,
        }

        private enum AvId
        {
            EOL,
            NbComputerName,
            NbDomainName,
            DnsComputerName,
            DnsDomainName,
            DnsTreeName,
            Flags,
            Timestamp,
            SingleHost,
            TargetName,
            ChannelBindings,
        }

        [Flags]
        private enum AvFlags : uint
        {
            ConstrainedAuthentication = 1,
            MICPresent = 2,
            UntrustedSPN = 4,
        }

        private static ReadOnlySpan<byte> GetField(ReadOnlySpan<byte> payload, int fieldOffset)
        {
            uint offset = BinaryPrimitives.ReadUInt32LittleEndian(payload.Slice(fieldOffset + 4));
            ushort length = BinaryPrimitives.ReadUInt16LittleEndian(payload.Slice(fieldOffset));

            if (length == 0 || offset + length > payload.Length)
            {
                return ReadOnlySpan<byte>.Empty;
            }

            return payload.Slice((int)offset, length);
        }

        public byte[]? GetOutgoingBlob(byte[]? incomingBlob)
        {
            // Ensure the message is long enough
            Assert.True(incomingBlob.Length >= 12);
            Assert.Equal(NtlmHeader.ToArray(), incomingBlob.AsSpan(0, 8).ToArray());

            var messageType = (MessageType)BinaryPrimitives.ReadUInt32LittleEndian(incomingBlob.AsSpan(8, 4));
            Assert.Equal(_expectedMessageType, messageType);

            switch (messageType)
            {
                case MessageType.Negotiate:
                    // We don't negotiate, we just verify
                    Assert.True(incomingBlob.Length >= 32);
                    Flags flags = (Flags)BinaryPrimitives.ReadUInt32LittleEndian(incomingBlob.AsSpan(12, 4));
                    Assert.Equal(_requiredFlags, (flags & _requiredFlags));
                    Assert.True((flags & (Flags.NegotiateOEM | Flags.NegotiateUnicode)) != 0);
                    if (flags.HasFlag(Flags.NegotiateDomainSupplied))
                    {
                        string domain = Encoding.ASCII.GetString(GetField(incomingBlob, 16));
                        Assert.Equal(_expectedCredential.Domain, domain);
                    }
                    _expectedMessageType = MessageType.Authenticate;
                    _negotiateMessage = incomingBlob;
                    return _challengeMessage = GenerateChallenge(flags);

                case MessageType.Authenticate:
                    // Validate the authentication!
                    ValidateAuthentication(incomingBlob);
                    _expectedMessageType = 0;
                    return null;

                default:
                    Assert.Fail($"Incorrect message type {messageType}");
                    return null;
            }
        }

        private static int WriteAvIdString(Span<byte> buffer, AvId avId, string value)
        {
            int size = Encoding.Unicode.GetByteCount(value);
            BinaryPrimitives.WriteUInt16LittleEndian(buffer, (ushort)avId);
            BinaryPrimitives.WriteUInt16LittleEndian(buffer.Slice(2), (ushort)size);
            Encoding.Unicode.GetBytes(value, buffer.Slice(4));
            return size + 4;
        }

        private byte[] GenerateChallenge(Flags flags)
        {
            byte[] buffer = new byte[1000];
            byte[] targetName = Encoding.Unicode.GetBytes(TargetIsServer ? "Server" : _expectedCredential.Domain);
            int payloadOffset = 56;

            // Loosely follow the flag manipulation in
            // 3.2.5.1.1 Server Receives a NEGOTIATE_MESSAGE from the Client
            flags &= ~(Flags.NegotiateLMKey | Flags.TargetTypeServer | Flags.TargetTypeDomain);
            flags |= Flags.NegotiateNtlm | Flags.NegotiateAlwaysSign | Flags.NegotiateTargetInfo;
            // Specification says to set Flags.RequestTargetName but it's valid only in NEGOTIATE_MESSAGE?!
            flags |= TargetIsServer ? Flags.TargetTypeServer : Flags.TargetTypeDomain;
            if (PreferUnicode && flags.HasFlag(Flags.NegotiateUnicode))
            {
                flags &= ~Flags.NegotiateOEM;
            }
            if (ForceNegotiateVersion)
            {
                flags |= Flags.NegotiateVersion;
            }
            // Remove any unsupported flags here
            flags &= Flags.AllSupported;

            NtlmHeader.CopyTo(buffer.AsSpan(0, 8));
            BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(8), (uint)MessageType.Challenge);
            BinaryPrimitives.WriteUInt16LittleEndian(buffer.AsSpan(12), (ushort)targetName.Length);
            BinaryPrimitives.WriteUInt16LittleEndian(buffer.AsSpan(14), (ushort)targetName.Length);
            BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(16), (ushort)payloadOffset);
            targetName.CopyTo(buffer.AsSpan(payloadOffset, targetName.Length));
            payloadOffset += targetName.Length;
            BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(20), (uint)flags);
            _serverChallenge.CopyTo(buffer.AsSpan(24, 8));
            // 8 bytes reserved
            // 8 bytes of TargetInfoFields (written below)
            Version.CopyTo(buffer.AsSpan(48, 8));

            int targetInfoOffset = payloadOffset;
            int targetInfoCurrentOffset = targetInfoOffset;
            targetInfoCurrentOffset += WriteAvIdString(buffer.AsSpan(targetInfoCurrentOffset), AvId.NbDomainName, _expectedCredential.Domain);
            targetInfoCurrentOffset += WriteAvIdString(buffer.AsSpan(targetInfoCurrentOffset), AvId.NbComputerName, "Server");

            if (SendTimestamp)
            {
                BinaryPrimitives.WriteUInt16LittleEndian(buffer.AsSpan(targetInfoCurrentOffset), (ushort)AvId.Timestamp);
                BinaryPrimitives.WriteUInt16LittleEndian(buffer.AsSpan(targetInfoCurrentOffset + 2), (ushort)8);
                BinaryPrimitives.WriteInt64LittleEndian(buffer.AsSpan(targetInfoCurrentOffset + 4), DateTime.UtcNow.ToFileTimeUtc());
                targetInfoCurrentOffset += 12;
            }

            // TODO: DNS machine, domain, forest?
            // EOL
            targetInfoCurrentOffset += 4;
            int targetInfoSize = targetInfoCurrentOffset - targetInfoOffset;

            BinaryPrimitives.WriteUInt16LittleEndian(buffer.AsSpan(40), (ushort)targetInfoSize);
            BinaryPrimitives.WriteUInt16LittleEndian(buffer.AsSpan(42), (ushort)targetInfoSize);
            BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(44), (uint)targetInfoOffset);

            return buffer.AsSpan(0, targetInfoCurrentOffset).ToArray();
        }

        private byte[] MakeNtlm2Hash()
        {
            byte[] pwHash = new byte[16];
            byte[] pwBytes = Encoding.Unicode.GetBytes(_expectedCredential.Password);
            MD4.HashData(pwBytes, pwHash);
            using (IncrementalHash hmac = IncrementalHash.CreateHMAC(HashAlgorithmName.MD5, pwHash))
            {
                hmac.AppendData(Encoding.Unicode.GetBytes(_expectedCredential.UserName.ToUpper() + _expectedCredential.Domain));
                return hmac.GetHashAndReset();
            }
        }

        // Section 3.4.5.2 SIGNKEY, 3.4.5.3 SEALKEY
        private byte[] DeriveKey(ReadOnlySpan<byte> exportedSessionKey, ReadOnlySpan<byte> magic)
        {
            using (var md5 = IncrementalHash.CreateHash(HashAlgorithmName.MD5))
            {
                md5.AppendData(exportedSessionKey);
                md5.AppendData(magic);
                return md5.GetHashAndReset();
            }
        }

        private void ValidateAuthentication(byte[] incomingBlob)
        {
            ReadOnlySpan<byte> lmChallengeResponse = GetField(incomingBlob, 12);
            ReadOnlySpan<byte> ntChallengeResponse = GetField(incomingBlob, 20);
            ReadOnlySpan<byte> encryptedRandomSessionKey = GetField(incomingBlob, 52);
            ReadOnlySpan<byte> mic = incomingBlob.AsSpan(72, 16);

            Flags flags = (Flags)BinaryPrimitives.ReadUInt32LittleEndian(incomingBlob.AsSpan(60));
            Assert.Equal(_requiredFlags, (flags & _requiredFlags));

            // Only one encoding can be selected by the client
            Assert.True((flags & (Flags.NegotiateOEM | Flags.NegotiateUnicode)) != 0);
            Assert.True((flags & (Flags.NegotiateOEM | Flags.NegotiateUnicode)) != (Flags.NegotiateOEM | Flags.NegotiateUnicode));
            Encoding encoding = flags.HasFlag(Flags.NegotiateUnicode) ? Encoding.Unicode : Encoding.ASCII;

            string domainName = encoding.GetString(GetField(incomingBlob, 28));
            string userName = encoding.GetString(GetField(incomingBlob, 36));
            string workstation = encoding.GetString(GetField(incomingBlob, 44));
            Assert.Equal(_expectedCredential.UserName, userName);
            Assert.Equal(_expectedCredential.Domain, domainName);

            byte[] ntlm2hash = MakeNtlm2Hash();
            Span<byte> sessionBaseKey = stackalloc byte[16];
            using (IncrementalHash hmac = IncrementalHash.CreateHMAC(HashAlgorithmName.MD5, ntlm2hash))
            {
                hmac.AppendData(_serverChallenge);
                hmac.AppendData(ntChallengeResponse.Slice(16));
                // If this matches then the password matched
                IsAuthenticated = hmac.GetHashAndReset().AsSpan().SequenceEqual(ntChallengeResponse.Slice(0, 16));

                if (!IsAuthenticated)
                {
                    // Bail out
                    return;
                }

                // Compute sessionBaseKey
                hmac.AppendData(ntChallengeResponse.Slice(0, 16));
                hmac.GetHashAndReset(sessionBaseKey);
            }

            ReadOnlySpan<byte> avPairs = ntChallengeResponse.Slice(16 + 28);
            AvFlags avFlags = 0;
            while (avPairs[0] != (byte)AvId.EOL)
            {
                AvId id = (AvId)avPairs[0];
                Assert.Equal(0, avPairs[1]);
                ushort length = BinaryPrimitives.ReadUInt16LittleEndian(avPairs.Slice(2, 2));

                if (id == AvId.Flags)
                {
                    Assert.Equal(4, length);
                    avFlags = (AvFlags)BinaryPrimitives.ReadUInt32LittleEndian(avPairs.Slice(4, 4));
                }
                else if (id == AvId.TargetName)
                {
                    ClientSpecifiedSpn = Encoding.Unicode.GetString(avPairs.Slice(4, length));
                }

                avPairs = avPairs.Slice(length + 4);
            }

            // Decrypt exportedSessionKey with sessionBaseKey
            Span<byte> exportedSessionKey = stackalloc byte[16];
            if (flags.HasFlag(Flags.NegotiateKeyExchange) &&
                (flags.HasFlag(Flags.NegotiateSeal) || flags.HasFlag(Flags.NegotiateSign)))
            {
                using (RC4 rc4 = new RC4(sessionBaseKey))
                {
                    rc4.Transform(encryptedRandomSessionKey, exportedSessionKey);
                }
            }
            else
            {
                sessionBaseKey.CopyTo(exportedSessionKey);
            }

            // Calculate and verify message integrity if enabled
            if (avFlags.HasFlag(AvFlags.MICPresent))
            {
                IsMICPresent = true;

                Assert.NotNull(_negotiateMessage);
                Assert.NotNull(_challengeMessage);
                byte[] calculatedMic = new byte[16];
                using (var hmacMic = IncrementalHash.CreateHMAC(HashAlgorithmName.MD5, exportedSessionKey))
                {
                    hmacMic.AppendData(_negotiateMessage);
                    hmacMic.AppendData(_challengeMessage);
                    // Authenticate message with the MIC erased
                    hmacMic.AppendData(incomingBlob.AsSpan(0, 72));
                    hmacMic.AppendData(new byte[16]);
                    hmacMic.AppendData(incomingBlob.AsSpan(72 + 16));
                    hmacMic.GetHashAndReset(calculatedMic);
                }
                Assert.Equal(mic.ToArray(), calculatedMic);
            }

            // Derive signing keys
            _clientSigningKey = DeriveKey(exportedSessionKey, ClientSigningKeyMagic);
            _serverSigningKey = DeriveKey(exportedSessionKey, ServerSigningKeyMagic);
            _clientSeal = new RC4(DeriveKey(exportedSessionKey, ClientSealingKeyMagic));
            _serverSeal = new RC4(DeriveKey(exportedSessionKey, ServerSealingKeyMagic));
            _negotiatedFlags = flags;
            CryptographicOperations.ZeroMemory(exportedSessionKey);
        }

        private void CalculateSignature(
            ReadOnlySpan<byte> message,
            uint sequenceNumber,
            ReadOnlySpan<byte> signingKey,
            RC4 seal,
            Span<byte> signature)
        {
            BinaryPrimitives.WriteInt32LittleEndian(signature, 1);
            BinaryPrimitives.WriteUInt32LittleEndian(signature.Slice(12), sequenceNumber);
            using (var hmac = IncrementalHash.CreateHMAC(HashAlgorithmName.MD5, signingKey))
            {
                hmac.AppendData(signature.Slice(12, 4));
                hmac.AppendData(message);
                Span<byte> hmacResult = stackalloc byte[hmac.HashLengthInBytes];
                hmac.GetHashAndReset(hmacResult);
                if (_negotiatedFlags.HasFlag(Flags.NegotiateKeyExchange))
                {
                    seal.Transform(hmacResult.Slice(0, 8), signature.Slice(4, 8));
                }
                else
                {
                    hmacResult.Slice(0, 8).CopyTo(signature.Slice(4, 8));
                }
            }
        }

        public void VerifyMIC(ReadOnlySpan<byte> message, ReadOnlySpan<byte> signature, uint sequenceNumber)
        {
            Assert.Equal(16, signature.Length);
            // Check version
            Assert.Equal(1, BinaryPrimitives.ReadInt32LittleEndian(signature));
            // Make sure the authentication finished
            Assert.NotNull(_clientSeal);
            Assert.NotNull(_clientSigningKey);

            Span<byte> expectedSignature = stackalloc byte[16];
            CalculateSignature(message, sequenceNumber, _clientSigningKey, _clientSeal, expectedSignature);
            Assert.True(signature.SequenceEqual(expectedSignature));
        }

        public void GetMIC(ReadOnlySpan<byte> message, Span<byte> signature, uint sequenceNumber)
        {
            // Make sure the authentication finished
            Assert.NotNull(_serverSeal);
            Assert.NotNull(_serverSigningKey);

            CalculateSignature(message, sequenceNumber, _serverSigningKey, _serverSeal, signature);
        }

        public void Unseal(ReadOnlySpan<byte> sealedMessage, Span<byte> message)
        {
            _clientSeal.Transform(sealedMessage, message);
        }

        public void Seal(ReadOnlySpan<byte> message, Span<byte> sealedMessage)
        {
            _serverSeal.Transform(message, sealedMessage);
        }
    }
}
