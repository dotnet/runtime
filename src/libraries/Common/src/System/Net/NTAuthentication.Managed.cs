// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Formats.Asn1;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.Authentication.ExtendedProtection;
using System.Security.Cryptography;
using System.Text;

// NTLM uses all sorts of broken cryptographic primitives (HMAC-MD5, MD4, RC4)
#pragma warning disable CA5351

namespace System.Net
{
    internal sealed partial class NTAuthentication
    {
        // Input parameters
        private readonly bool _isSpNego;
        private readonly NetworkCredential _credential;
        private readonly string? _spn;
        private readonly ChannelBinding? _channelBinding;

        // State parameters
        private byte[]? _spnegoMechList;
        private byte[]? _negotiateMessage;
        private byte[]? _clientSigningKey;
        private byte[]? _serverSigningKey;
        private RC4? _clientSeal;
        private RC4? _serverSeal;
        private uint _clientSequenceNumber;
        private uint _serverSequenceNumber;
        public bool IsCompleted { get; set; }

        // value should match the Windows sspicli NTE_FAIL value
        // defined in winerror.h
        private const int NTE_FAIL = unchecked((int)0x80090020);

        private static ReadOnlySpan<byte> NtlmHeader => "NTLMSSP\0"u8;

        private static ReadOnlySpan<byte> ClientSigningKeyMagic => "session key to client-to-server signing key magic constant\0"u8;
        private static ReadOnlySpan<byte> ServerSigningKeyMagic => "session key to server-to-client signing key magic constant\0"u8;
        private static ReadOnlySpan<byte> ClientSealingKeyMagic => "session key to client-to-server sealing key magic constant\0"u8;
        private static ReadOnlySpan<byte> ServerSealingKeyMagic => "session key to server-to-client sealing key magic constant\0"u8;

        private static readonly byte[] s_workstation = Encoding.Unicode.GetBytes(Environment.MachineName);

        private static SecurityStatusPal SecurityStatusPalOk = new SecurityStatusPal(SecurityStatusPalErrorCode.OK);
        private static SecurityStatusPal SecurityStatusPalContinueNeeded = new SecurityStatusPal(SecurityStatusPalErrorCode.ContinueNeeded);
        private static SecurityStatusPal SecurityStatusPalInvalidToken = new SecurityStatusPal(SecurityStatusPalErrorCode.InvalidToken);
        private static SecurityStatusPal SecurityStatusPalInternalError = new SecurityStatusPal(SecurityStatusPalErrorCode.InternalError);
        private static SecurityStatusPal SecurityStatusPalPackageNotFound = new SecurityStatusPal(SecurityStatusPalErrorCode.PackageNotFound);
        private static SecurityStatusPal SecurityStatusPalMessageAltered = new SecurityStatusPal(SecurityStatusPalErrorCode.MessageAltered);
        private static SecurityStatusPal SecurityStatusPalLogonDenied = new SecurityStatusPal(SecurityStatusPalErrorCode.LogonDenied);

        private const Flags s_requiredFlags =
            Flags.NegotiateNtlm2 | Flags.NegotiateNtlm | Flags.NegotiateUnicode | Flags.TargetName |
            Flags.NegotiateVersion | Flags.NegotiateKeyExchange | Flags.Negotiate128 |
            Flags.NegotiateTargetInfo | Flags.NegotiateAlwaysSign | Flags.NegotiateSign;

        private static readonly Version s_version = new Version { VersionMajor = 6, VersionMinor = 1, ProductBuild = 7600, CurrentRevision = 15 };

        private const int ChallengeResponseLength = 24;

        private const int HeaderLength = 8;

        private const int ChallengeLength = 8;

        private const int DigestLength = 16;

        private const int SessionKeyLength = 16;
        private const int SignatureLength = 16;

        private const string SpnegoOid = "1.3.6.1.5.5.2";

        private const string NtlmOid = "1.3.6.1.4.1.311.2.2.10";

        private enum MessageType : byte
        {
            Negotiate = 1,
            Challenge = 2,
            Authenticate = 3,
        }

        // 2.2.2.5 NEGOTIATE
        [Flags]
        private enum Flags : uint
        {
            NegotiateUnicode = 0x00000001,
            NegotiateOEM = 0x00000002,
            TargetName = 0x00000004,
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

        [StructLayout(LayoutKind.Sequential)]
        private unsafe struct MessageField
        {
            public ushort Length;
            public ushort MaximumLength;
            public int PayloadOffset;
        }

        [StructLayout(LayoutKind.Sequential)]
        private unsafe struct MessageHeader
        {
            public fixed byte Header[HeaderLength];
            public MessageType MessageType;
            private byte _unused1;
            private byte _unused2;
            private byte _unused3;
        }

        [StructLayout(LayoutKind.Sequential)]
        private unsafe struct Version
        {
            public byte VersionMajor;
            public byte VersionMinor;
            public ushort ProductBuild;
            private byte _unused4;
            private byte _unused5;
            private byte _unused6;
            public byte CurrentRevision;
        }

        // Type 1 message
        [StructLayout(LayoutKind.Sequential)]
        private unsafe struct NegotiateMessage
        {
            public MessageHeader Header;
            public Flags Flags;
            public MessageField DomainName;
            public MessageField WorkStation;
            public Version Version;
        }

        // TYPE 2 message
        [StructLayout(LayoutKind.Sequential)]
        private unsafe struct ChallengeMessage
        {
            public MessageHeader Header;
            public MessageField TargetName;
            public Flags Flags;
            public fixed byte ServerChallenge[ChallengeLength];
            private ulong _unused;
            public MessageField TargetInfo;
            public Version Version;
        }

        // TYPE 3 message
        [StructLayout(LayoutKind.Sequential)]
        private unsafe struct AuthenticateMessage
        {
            public MessageHeader Header;
            public MessageField LmChallengeResponse;
            public MessageField NtChallengeResponse;
            public MessageField DomainName;
            public MessageField UserName;
            public MessageField Workstation;
            public MessageField EncryptedRandomSessionKey;
            public Flags Flags;
            public Version Version;
            public fixed byte Mic[16];
        }

        // Set temp to ConcatenationOf(Responserversion, HiResponserversion, Z(6), Time, ClientChallenge, Z(4), ServerName, Z(4))
        [StructLayout(LayoutKind.Sequential)]
        private unsafe struct NtChallengeResponse
        {
            public fixed byte Hmac[DigestLength];
            public byte Responserversion;
            public byte HiResponserversion;
            private byte _reserved1;
            private byte _reserved2;
            private int _reserved3;
            public long Time;
            public fixed byte ClientChallenge[ChallengeLength];
            private int _reserved4;
            public fixed byte ServerInfo[4]; // Has to be non-zero size, so set it to the Z(4) padding
        }

        // rfc4178
        private enum NegotiationToken
        {
            NegTokenInit = 0,
            NegTokenResp = 1
        }

        private enum NegTokenInit
        {
            MechTypes = 0,
            ReqFlags = 1,
            MechToken = 2,
            MechListMIC = 3
        }

        private enum NegTokenResp
        {
            NegState = 0,
            SupportedMech = 1,
            ResponseToken = 2,
            MechListMIC = 3
        }

        private enum NegState
        {
            Unknown = -1,           // Internal. Not in RFC.
            AcceptCompleted = 0,
            AcceptIncomplete = 1,
            Reject = 2,
            RequestMic = 3
        }

        internal NTAuthentication(bool isServer, string package, NetworkCredential credential, string? spn, ContextFlagsPal requestedContextFlags, ChannelBinding? channelBinding)
        {
            if (isServer)
            {
                throw new PlatformNotSupportedException(SR.net_nego_server_not_supported);
            }

            if (package.Equals("NTLM", StringComparison.OrdinalIgnoreCase))
            {
                _isSpNego = false;
            }
            else if (package.Equals("Negotiate", StringComparison.OrdinalIgnoreCase))
            {
                _isSpNego = true;
            }
            else
            {
                throw new PlatformNotSupportedException(SR.net_securitypackagesupport);
            }

            if (string.IsNullOrWhiteSpace(credential.UserName) || string.IsNullOrWhiteSpace(credential.Password))
            {
                // NTLM authentication is not possible with default credentials which are no-op
                throw new PlatformNotSupportedException(SR.net_ntlm_not_possible_default_cred);
            }

            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"package={package}, spn={spn}, requestedContextFlags={requestedContextFlags}");

            // TODO: requestedContextFlags
            _credential = credential;
            _spn = spn;
            _channelBinding = channelBinding;
        }

        internal void CloseContext()
        {
            // Dispose of the state
            _negotiateMessage = null;
            _clientSigningKey = null;
            _serverSigningKey = null;
            _clientSeal?.Dispose();
            _serverSeal?.Dispose();
            _clientSeal = null;
            _serverSeal = null;
            _clientSequenceNumber = 0;
            _serverSequenceNumber = 0;
            IsCompleted = true;
        }

        internal string? GetOutgoingBlob(string? incomingBlob)
        {
            return GetOutgoingBlob(incomingBlob, throwOnError: true, out _);
        }

        internal unsafe string? GetOutgoingBlob(string? incomingBlob, bool throwOnError, out SecurityStatusPal statusCode)
        {
            Debug.Assert(!IsCompleted);

            byte[]? decodedIncomingBlob = null;
            if (incomingBlob != null && incomingBlob.Length > 0)
            {
                decodedIncomingBlob = Convert.FromBase64String(incomingBlob);
            }
            byte[]? decodedOutgoingBlob;

            // TODO: Logging, validation
            if (_negotiateMessage == null)
            {
                Debug.Assert(decodedIncomingBlob == null);

                _negotiateMessage = new byte[sizeof(NegotiateMessage)];
                CreateNtlmNegotiateMessage(_negotiateMessage);

                decodedOutgoingBlob = _isSpNego ? CreateSpNegoNegotiateMessage(_negotiateMessage) : _negotiateMessage;
                statusCode = SecurityStatusPalContinueNeeded;
            }
            else
            {
                Debug.Assert(decodedIncomingBlob != null);

                if (!_isSpNego)
                {
                    IsCompleted = true;
                    decodedOutgoingBlob = ProcessChallenge(decodedIncomingBlob, out statusCode);
                }
                else
                {
                    decodedOutgoingBlob = ProcessSpNegoChallenge(decodedIncomingBlob, out statusCode);
                }
            }

            string? outgoingBlob = null;
            if (decodedOutgoingBlob != null && decodedOutgoingBlob.Length > 0)
            {
                outgoingBlob = Convert.ToBase64String(decodedOutgoingBlob);
            }

            if (IsCompleted)
            {
                CloseContext();
            }

            return outgoingBlob;
        }

        private static unsafe void CreateNtlmNegotiateMessage(Span<byte> asBytes)
        {
            Debug.Assert(HeaderLength == NtlmHeader.Length);
            Debug.Assert(asBytes.Length == sizeof(NegotiateMessage));

            ref NegotiateMessage message = ref MemoryMarshal.AsRef<NegotiateMessage>(asBytes);

            asBytes.Clear();
            NtlmHeader.CopyTo(asBytes);
            message.Header.MessageType = MessageType.Negotiate;
            message.Flags = s_requiredFlags;
            message.Version = s_version;
        }

        private static unsafe int GetFieldLength(MessageField field)
        {
            ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(&field, sizeof(MessageField));
            return BinaryPrimitives.ReadInt16LittleEndian(span);
        }

        private static unsafe int GetFieldOffset(MessageField field)
        {
            ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(&field, sizeof(MessageField));
            return BinaryPrimitives.ReadInt16LittleEndian(span.Slice(4));
        }

        private static ReadOnlySpan<byte> GetField(MessageField field, ReadOnlySpan<byte> payload)
        {
            int offset = GetFieldOffset(field);
            int length = GetFieldLength(field);

            if (length == 0 || offset + length > payload.Length)
            {
                return ReadOnlySpan<byte>.Empty;
            }

            return payload.Slice(GetFieldOffset(field), GetFieldLength(field));
        }

        private static unsafe void SetField(ref MessageField field, int length, int offset)
        {
            if (length > short.MaxValue)
            {
                throw new Win32Exception(NTE_FAIL);
            }

            fixed (void* ptr = &field)
            {
                Span<byte> span = new Span<byte>(ptr, sizeof(MessageField));
                BinaryPrimitives.WriteInt16LittleEndian(span, (short)length);
                BinaryPrimitives.WriteInt16LittleEndian(span.Slice(2), (short)length);
                BinaryPrimitives.WriteInt32LittleEndian(span.Slice(4), offset);
            }
        }

        private static void AddToPayload(ref MessageField field, ReadOnlySpan<byte> data, Span<byte> payload, ref int offset)
        {
            SetField(ref field, data.Length, offset);
            data.CopyTo(payload.Slice(offset));
            offset += data.Length;
        }

        private static void AddToPayload(ref MessageField field, ReadOnlySpan<char> data, Span<byte> payload, ref int offset)
        {
            int dataLength = Encoding.Unicode.GetBytes(data, payload.Slice(offset));
            SetField(ref field, dataLength, offset);
            offset += dataLength;
        }

        // Section 3.3.2
        // Define NTOWFv2(Passwd, User, UserDom) as HMAC_MD5(MD4(UNICODE(Passwd)), UNICODE(ConcatenationOf(Uppercase(User),
        // UserDom ) ) )
        // EndDefine
        private static void makeNtlm2Hash(string domain, string userName, ReadOnlySpan<char> password, Span<byte> hash)
        {
            // Maximum password length for Windows authentication is 128 characters, we enforce
            // the limit early to prevent allocating large buffers on stack.
            if (password.Length > 128)
            {
                throw new Win32Exception(NTE_FAIL);
            }

            Span<byte> pwHash = stackalloc byte[DigestLength];
            Span<byte> pwBytes = stackalloc byte[Encoding.Unicode.GetByteCount(password)];

            try
            {
                Encoding.Unicode.GetBytes(password, pwBytes);
                MD4.HashData(pwBytes, pwHash);

                using (var hmac = IncrementalHash.CreateHMAC(HashAlgorithmName.MD5, pwHash))
                {
                    // strangely, user is upper case, domain is not.
                    byte[] blob = Encoding.Unicode.GetBytes(string.Concat(userName.ToUpperInvariant(), domain));
                    hmac.AppendData(blob);
                    hmac.GetHashAndReset(hash);
                }
            }
            finally
            {
                CryptographicOperations.ZeroMemory(pwBytes);
                CryptographicOperations.ZeroMemory(pwHash);
            }
        }

        // Section 3.3.2
        //
        // Set temp to ConcatenationOf(Responserversion, HiResponserversion, Z(6), Time, ClientChallenge, Z(4), ServerName, Z(4))
        // Set NTProofStr to HMAC_MD5(ResponseKeyNT, ConcatenationOf(CHALLENGE_MESSAGE.ServerChallenge, temp))
        // Set NtChallengeResponse to ConcatenationOf(NTProofStr, temp)
        private unsafe void makeNtlm2ChallengeResponse(DateTime time, ReadOnlySpan<byte> ntlm2hash, ReadOnlySpan<byte> serverChallenge, Span<byte> clientChallenge, ReadOnlySpan<byte> serverInfo, ref MessageField field, Span<byte> payload, ref int payloadOffset)
        {
            Debug.Assert(serverChallenge.Length == ChallengeLength);
            Debug.Assert(clientChallenge.Length == ChallengeLength);
            Debug.Assert(ntlm2hash.Length == DigestLength);

            Span<byte> blob = payload.Slice(payloadOffset, sizeof(NtChallengeResponse) + serverInfo.Length);
            ref NtChallengeResponse temp = ref MemoryMarshal.AsRef<NtChallengeResponse>(blob.Slice(0, sizeof(NtChallengeResponse)));

            temp.HiResponserversion = 1;
            temp.Responserversion = 1;
            temp.Time = time.ToFileTimeUtc();

            clientChallenge.CopyTo(MemoryMarshal.CreateSpan(ref temp.ClientChallenge[0], ChallengeLength));
            serverInfo.CopyTo(MemoryMarshal.CreateSpan(ref temp.ServerInfo[0], serverInfo.Length));

            // Calculate NTProofStr
            // we created temp part in place where it needs to be.
            // now we need to prepend it with calculated hmac.
            using (var hmac = IncrementalHash.CreateHMAC(HashAlgorithmName.MD5, ntlm2hash))
            {
                hmac.AppendData(serverChallenge);
                hmac.AppendData(blob.Slice(DigestLength));
                hmac.GetHashAndReset(blob);
            }

            SetField(ref field, blob.Length, payloadOffset);

            payloadOffset += blob.Length;
        }

        private unsafe void WriteChannelBindingHash(Span<byte> hashBuffer)
        {
            if (_channelBinding != null)
            {
                IntPtr cbtData = _channelBinding.DangerousGetHandle();
                int cbtDataSize = _channelBinding.Size;

                using (var md5 = IncrementalHash.CreateHash(HashAlgorithmName.MD5))
                {
                    md5.AppendData(new Span<byte>((void *)cbtData, cbtDataSize));
                    md5.GetHashAndReset(hashBuffer);
                }
            }
            else
            {
                hashBuffer.Fill(0);
            }
        }

        private byte[] ProcessTargetInfo(ReadOnlySpan<byte> targetInfo, out DateTime time, out bool hasNbNames)
        {
            int spnSize = _spn != null ? Encoding.Unicode.GetByteCount(_spn) : 0;

            if (spnSize > short.MaxValue)
            {
                throw new Win32Exception(NTE_FAIL);
            }

            bool hasNbComputerName = false, hasNbDomainName = false;
            byte[] targetInfoBuffer = new byte[targetInfo.Length + 20 /* channel binding */ + 4 + spnSize /* SPN */ + 8 /* flags */];
            int targetInfoOffset = 0;

            time = DateTime.UtcNow;

            if (targetInfo.Length > 0)
            {
                ReadOnlySpan<byte> info = targetInfo;
                while (info.Length >= 4)
                {
                    AvId ID = (AvId)info[0];
                    byte l1 = info[2];
                    byte l2 = info[3];
                    int length = (l2 << 8) + l1;

                    if (ID == AvId.EOL)
                    {
                        break;
                    }

                    if (ID == AvId.Timestamp)
                    {
                        time = DateTime.FromFileTimeUtc(BitConverter.ToInt64(info.Slice(4, 8)));
                    }
                    else if (ID == AvId.TargetName || ID == AvId.ChannelBindings)
                    {
                        // Skip these, we insert them ourselves
                        info = info.Slice(length + 4);
                        continue;
                    }
                    else if (ID == AvId.NbComputerName)
                    {
                        hasNbComputerName = true;
                    }
                    else if (ID == AvId.NbDomainName)
                    {
                        hasNbDomainName = true;
                    }

                    // Copy attribute-value pair into destination target info
                    info.Slice(0, length + 4).CopyTo(targetInfoBuffer.AsSpan(targetInfoOffset, length + 4));
                    targetInfoOffset += length + 4;

                    info = info.Slice(length + 4);
                }
            }

            hasNbNames = hasNbComputerName && hasNbDomainName;

            // Add new entries into destination target info

            // Target name (eg. HTTP/example.org)
            targetInfoBuffer[targetInfoOffset] = (byte)AvId.TargetName;
            BinaryPrimitives.WriteUInt16LittleEndian(targetInfoBuffer.AsSpan(2 + targetInfoOffset), (ushort)spnSize);
            if (_spn != null)
            {
                int bytesWritten = Encoding.Unicode.GetBytes(_spn, targetInfoBuffer.AsSpan(4 + targetInfoOffset));
                Debug.Assert(bytesWritten == spnSize);
            }
            targetInfoOffset += spnSize + 4;

            // Channel binding
            targetInfoBuffer[targetInfoOffset] = (byte)AvId.ChannelBindings;
            targetInfoBuffer[targetInfoOffset + 2] = 16;
            WriteChannelBindingHash(targetInfoBuffer.AsSpan(targetInfoOffset + 4, 16));
            targetInfoOffset += 16 + 4;

            // Flags
            targetInfoBuffer[targetInfoOffset] = (byte)AvId.Flags;
            targetInfoBuffer[targetInfoOffset + 2] = 4; // Length of flags
            targetInfoBuffer[targetInfoOffset + 4] = 2; // MIC flag
            targetInfoOffset += 8;

            // EOL
            targetInfoOffset += 4;

            if (targetInfoOffset == targetInfoBuffer.Length)
            {
                return targetInfoBuffer;
            }

            return targetInfoBuffer.AsSpan(targetInfoOffset).ToArray();
        }

        // Section 3.4.5.2 SIGNKEY, 3.4.5.3 SEALKEY
        private static byte[] DeriveKey(ReadOnlySpan<byte> exportedSessionKey, ReadOnlySpan<byte> magic)
        {
            using (var md5 = IncrementalHash.CreateHash(HashAlgorithmName.MD5))
            {
                md5.AppendData(exportedSessionKey);
                md5.AppendData(magic);
                return md5.GetHashAndReset();
            }
        }

        // This gets decoded byte blob and returns response in binary form.
        private unsafe byte[]? ProcessChallenge(byte[] blob, out SecurityStatusPal statusCode)
        {
            // TODO: Validate size and offsets

            ReadOnlySpan<byte> asBytes = new ReadOnlySpan<byte>(blob);
            ref readonly ChallengeMessage challengeMessage = ref MemoryMarshal.AsRef<ChallengeMessage>(asBytes.Slice(0, sizeof(ChallengeMessage)));

            // Verify message type and signature
            if (challengeMessage.Header.MessageType != MessageType.Challenge ||
                !NtlmHeader.SequenceEqual(asBytes.Slice(0, NtlmHeader.Length)))
            {
                statusCode = SecurityStatusPalInvalidToken;
                return null;
            }

            Flags flags = BitConverter.IsLittleEndian ? challengeMessage.Flags : (Flags)BinaryPrimitives.ReverseEndianness((uint)challengeMessage.Flags);
            ReadOnlySpan<byte> targetName = GetField(challengeMessage.TargetName, asBytes);

            // Only NTLMv2 with MIC is supported
            //
            // NegotiateSign and NegotiateKeyExchange are necessary to calculate the key
            // that is used for MIC.
            if ((flags & s_requiredFlags) != s_requiredFlags)
            {
                statusCode = SecurityStatusPalInvalidToken;
                return null;
            }

            ReadOnlySpan<byte> targetInfo = GetField(challengeMessage.TargetInfo, asBytes);
            byte[] targetInfoBuffer = ProcessTargetInfo(targetInfo, out DateTime time, out bool hasNbNames);

            // If NTLM v2 authentication is used and the CHALLENGE_MESSAGE does not contain both
            // MsvAvNbComputerName and MsvAvNbDomainName AVPairs and either Integrity is TRUE or
            // Confidentiality is TRUE, then return STATUS_LOGON_FAILURE ([MS-ERREF] section 2.3.1).
            if (!hasNbNames && (flags & (Flags.NegotiateSign | Flags.NegotiateSeal)) != 0)
            {
                statusCode = SecurityStatusPalInvalidToken;
                return null;
            }

            int responseLength =
                sizeof(AuthenticateMessage) +
                ChallengeResponseLength +
                sizeof(NtChallengeResponse) +
                targetInfoBuffer.Length +
                Encoding.Unicode.GetByteCount(_credential.UserName) +
                Encoding.Unicode.GetByteCount(_credential.Domain) +
                s_workstation.Length +
                SessionKeyLength;

            byte[] responseBytes = new byte[responseLength];
            Span<byte> responseAsSpan = new Span<byte>(responseBytes);
            ref AuthenticateMessage response = ref MemoryMarshal.AsRef<AuthenticateMessage>(responseAsSpan.Slice(0, sizeof(AuthenticateMessage)));

            // variable fields
            Span<byte> payload = responseAsSpan;
            int payloadOffset = sizeof(AuthenticateMessage);

            responseAsSpan.Clear();
            NtlmHeader.CopyTo(responseAsSpan);

            response.Header.MessageType = MessageType.Authenticate;
            response.Flags = s_requiredFlags;
            response.Version = s_version;

            // Calculate hash for hmac - same for lm2 and ntlm2
            Span<byte> ntlm2hash = stackalloc byte[DigestLength];
            makeNtlm2Hash(_credential.Domain, _credential.UserName, _credential.Password, ntlm2hash);

            // Get random bytes for client challenge
            byte[] clientChallenge = new byte[ChallengeLength];
            RandomNumberGenerator.Fill(clientChallenge);

            // Create empty LM2 response.
            SetField(ref response.LmChallengeResponse, ChallengeResponseLength, payloadOffset);
            payload.Slice(payloadOffset, ChallengeResponseLength).Fill(0);
            payloadOffset += ChallengeResponseLength;

            // Create NTLM2 response
            ReadOnlySpan<byte> serverChallenge = asBytes.Slice(24, 8);
            makeNtlm2ChallengeResponse(time, ntlm2hash, serverChallenge, clientChallenge, targetInfoBuffer, ref response.NtChallengeResponse, payload, ref payloadOffset);
            Debug.Assert(payloadOffset == sizeof(AuthenticateMessage) + ChallengeResponseLength + sizeof(NtChallengeResponse) + targetInfoBuffer.Length);

            AddToPayload(ref response.UserName, _credential.UserName, payload, ref payloadOffset);
            AddToPayload(ref response.DomainName, _credential.Domain, payload, ref payloadOffset);
            AddToPayload(ref response.Workstation, s_workstation, payload, ref payloadOffset);

            // Generate random session key that will be used for signing the messages
            Span<byte> exportedSessionKey = stackalloc byte[16];
            RandomNumberGenerator.Fill(exportedSessionKey);

            // Both flags are necessary to exchange keys needed for MIC (!)
            Debug.Assert(flags.HasFlag(Flags.NegotiateSign) && flags.HasFlag(Flags.NegotiateKeyExchange));

            // Derive session base key
            Span<byte> sessionBaseKey = stackalloc byte[16];
            using (var hmacSessionKey = IncrementalHash.CreateHMAC(HashAlgorithmName.MD5, ntlm2hash))
            {
                hmacSessionKey.AppendData(responseAsSpan.Slice(response.NtChallengeResponse.PayloadOffset, 16));
                hmacSessionKey.GetHashAndReset(sessionBaseKey);
            }

            // Encrypt exportedSessionKey with sessionBaseKey
            using (RC4 rc4 = new RC4(sessionBaseKey))
            {
                Span<byte> encryptedRandomSessionKey = payload.Slice(payloadOffset, 16);
                rc4.Transform(exportedSessionKey, encryptedRandomSessionKey);
                SetField(ref response.EncryptedRandomSessionKey, 16, payloadOffset);
                payloadOffset += 16;
            }

            // Calculate MIC
            Debug.Assert(_negotiateMessage != null);
            using (var hmacMic = IncrementalHash.CreateHMAC(HashAlgorithmName.MD5, exportedSessionKey))
            {
                hmacMic.AppendData(_negotiateMessage);
                hmacMic.AppendData(blob);
                hmacMic.AppendData(responseBytes.AsSpan(0, payloadOffset));
                hmacMic.GetHashAndReset(MemoryMarshal.CreateSpan(ref response.Mic[0], hmacMic.HashLengthInBytes));
            }

            // Derive signing keys
            _clientSigningKey = DeriveKey(exportedSessionKey, ClientSigningKeyMagic);
            _serverSigningKey = DeriveKey(exportedSessionKey, ServerSigningKeyMagic);
            _clientSeal = new RC4(DeriveKey(exportedSessionKey, ClientSealingKeyMagic));
            _serverSeal = new RC4(DeriveKey(exportedSessionKey, ServerSealingKeyMagic));
            _clientSequenceNumber = 0;
            _serverSequenceNumber = 0;
            CryptographicOperations.ZeroMemory(exportedSessionKey);

            Debug.Assert(payloadOffset == responseBytes.Length);

            statusCode = SecurityStatusPalOk;
            return responseBytes;
        }

        private void CalculateSignature(
            ReadOnlySpan<byte> message,
            uint sequenceNumber,
            ReadOnlySpan<byte> signingKey,
            RC4 seal,
            Span<byte> signature)
        {
            BinaryPrimitives.WriteInt32LittleEndian(signature, 1);
            BinaryPrimitives.WriteUInt32LittleEndian(signature.Slice(12), _serverSequenceNumber);
            using (var hmac = IncrementalHash.CreateHMAC(HashAlgorithmName.MD5, signingKey))
            {
                hmac.AppendData(signature.Slice(12, 4));
                hmac.AppendData(message);
                Span<byte> hmacResult = stackalloc byte[hmac.HashLengthInBytes];
                hmac.GetHashAndReset(hmacResult);
                seal.Transform(hmacResult.Slice(0, 8), signature.Slice(4, 8));
            }
        }

        private bool VerifyMIC(ReadOnlySpan<byte> message, ReadOnlySpan<byte> signature)
        {
            // Check length and version
            if (signature.Length != SignatureLength ||
                BinaryPrimitives.ReadInt32LittleEndian(signature) != 1 ||
                _serverSeal == null ||
                _serverSigningKey == null)
            {
                return false;
            }

            Span<byte> expectedSignature = stackalloc byte[SignatureLength];
            CalculateSignature(message, _serverSequenceNumber, _serverSigningKey, _serverSeal, expectedSignature);

            _serverSequenceNumber++;

            return signature.SequenceEqual(expectedSignature);
        }

        private byte[] GetMIC(ReadOnlySpan<byte> message)
        {
            Debug.Assert(_clientSeal != null);
            Debug.Assert(_clientSigningKey != null);

            byte[] signature = new byte[SignatureLength];
            CalculateSignature(message, _clientSequenceNumber, _clientSigningKey, _clientSeal, signature);
            _clientSequenceNumber++;
            return signature;
        }

        private unsafe byte[] CreateSpNegoNegotiateMessage(ReadOnlySpan<byte> ntlmNegotiateMessage)
        {
            AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);
            using (writer.PushSequence(new Asn1Tag(TagClass.Application, 0)))
            {
                writer.WriteObjectIdentifier(SpnegoOid);

                // NegTokenInit::= SEQUENCE {
                //    mechTypes[0] MechTypeList,
                //    reqFlags[1] ContextFlags OPTIONAL,
                //       --inherited from RFC 2478 for backward compatibility,
                //      --RECOMMENDED to be left out
                //    mechToken[2] OCTET STRING  OPTIONAL,
                //    mechListMIC[3] OCTET STRING  OPTIONAL,
                //    ...
                // }
                using (writer.PushSequence(new Asn1Tag(TagClass.ContextSpecific, (int)NegotiationToken.NegTokenInit)))
                {
                    using (writer.PushSequence())
                    {
                        // MechType::= OBJECT IDENTIFIER
                        //    -- OID represents each security mechanism as suggested by
                        //   --[RFC2743]
                        //
                        // MechTypeList::= SEQUENCE OF MechType
                        using (writer.PushSequence(new Asn1Tag(TagClass.ContextSpecific, (int)NegTokenInit.MechTypes)))
                        {
                            AsnWriter mechListWriter = new AsnWriter(AsnEncodingRules.DER);

                            using (mechListWriter.PushSequence())
                            {
                                mechListWriter.WriteObjectIdentifier(NtlmOid);
                            }

                            _spnegoMechList = mechListWriter.Encode();
                            mechListWriter.CopyTo(writer);
                        }

                        using (writer.PushSequence(new Asn1Tag(TagClass.ContextSpecific, (int)NegTokenInit.MechToken)))
                        {
                            writer.WriteOctetString(ntlmNegotiateMessage);
                        }
                    }
                }
            }

            return writer.Encode();
        }

        private unsafe byte[]? ProcessSpNegoChallenge(byte[] challenge, out SecurityStatusPal statusCode)
        {
            NegState state = NegState.Unknown;
            string? mech = null;
            byte[]? blob = null;
            byte[]? mechListMIC = null;

            try
            {
                AsnReader reader = new AsnReader(challenge, AsnEncodingRules.DER);
                AsnReader challengeReader = reader.ReadSequence(new Asn1Tag(TagClass.ContextSpecific, (int)NegotiationToken.NegTokenResp));
                reader.ThrowIfNotEmpty();

                // NegTokenResp ::= SEQUENCE {
                //    negState[0] ENUMERATED {
                //        accept - completed(0),
                //        accept - incomplete(1),
                //        reject(2),
                //        request - mic(3)
                //    } OPTIONAL,
                // --REQUIRED in the first reply from the target
                //    supportedMech[1] MechType OPTIONAL,
                // --present only in the first reply from the target
                // responseToken[2] OCTET STRING  OPTIONAL,
                // mechListMIC[3] OCTET STRING  OPTIONAL,
                // ...
                // }

                challengeReader = challengeReader.ReadSequence();

                if (challengeReader.HasData && challengeReader.PeekTag().HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, (int)NegTokenResp.NegState)))
                {
                    AsnReader valueReader = challengeReader.ReadSequence(new Asn1Tag(TagClass.ContextSpecific, (int)NegTokenResp.NegState));
                    state = valueReader.ReadEnumeratedValue<NegState>();
                    valueReader.ThrowIfNotEmpty();
                }

                if (challengeReader.HasData && challengeReader.PeekTag().HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, (int)NegTokenResp.SupportedMech)))
                {
                    AsnReader valueReader = challengeReader.ReadSequence(new Asn1Tag(TagClass.ContextSpecific, (int)NegTokenResp.SupportedMech));
                    mech = valueReader.ReadObjectIdentifier();
                    valueReader.ThrowIfNotEmpty();
                }

                if (challengeReader.HasData && challengeReader.PeekTag().HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, (int)NegTokenResp.ResponseToken)))
                {
                    AsnReader valueReader = challengeReader.ReadSequence(new Asn1Tag(TagClass.ContextSpecific, (int)NegTokenResp.ResponseToken));
                    blob = valueReader.ReadOctetString();
                    valueReader.ThrowIfNotEmpty();
                }

                if (challengeReader.HasData && challengeReader.PeekTag().HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, (int)NegTokenResp.MechListMIC)))
                {
                    AsnReader valueReader = challengeReader.ReadSequence(new Asn1Tag(TagClass.ContextSpecific, (int)NegTokenResp.MechListMIC));
                    mechListMIC = valueReader.ReadOctetString();
                    valueReader.ThrowIfNotEmpty();
                }

                challengeReader.ThrowIfNotEmpty();
            }
            catch (AsnContentException)
            {
                statusCode = SecurityStatusPalInvalidToken;
                return null;
            }

            if (blob?.Length > 0)
            {
                // Mechanism should be set on first message. In case of NTLM that's the only
                // message with the challenge blob.
                if (!NtlmOid.Equals(mech))
                {
                    if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"Server requested unknown mechanism {mech}");
                    statusCode = SecurityStatusPalPackageNotFound;
                    return null;
                }

                // Process decoded NTLM blob.
                byte[]? response = ProcessChallenge(blob, out statusCode);

                if (statusCode.ErrorCode != SecurityStatusPalErrorCode.OK)
                {
                    return null;
                }

                if (response?.Length > 0)
                {
                    AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);

                    using (writer.PushSequence(new Asn1Tag(TagClass.ContextSpecific, (int)NegotiationToken.NegTokenResp)))
                    {
                        using (writer.PushSequence())
                        {
                            using (writer.PushSequence(new Asn1Tag(TagClass.ContextSpecific, (int)NegTokenResp.ResponseToken)))
                            {
                                writer.WriteOctetString(response);
                            }

                            using (writer.PushSequence(new Asn1Tag(TagClass.ContextSpecific, (int)NegTokenResp.MechListMIC)))
                            {
                                writer.WriteOctetString(GetMIC(_spnegoMechList));
                            }
                        }
                    }

                    statusCode = state == NegState.RequestMic ? SecurityStatusPalContinueNeeded : SecurityStatusPalOk;
                    return writer.Encode();
                }
            }

            if (mechListMIC != null)
            {
                if (_spnegoMechList == null || state != NegState.AcceptCompleted)
                {
                    statusCode = SecurityStatusPalInternalError;
                    return null;
                }

                if (!VerifyMIC(_spnegoMechList, mechListMIC))
                {
                    statusCode = SecurityStatusPalMessageAltered;
                    return null;
                }
            }

            IsCompleted = state == NegState.AcceptCompleted || state == NegState.Reject;
            statusCode = state switch {
                NegState.AcceptCompleted => SecurityStatusPalOk,
                NegState.AcceptIncomplete => SecurityStatusPalContinueNeeded,
                NegState.Reject => SecurityStatusPalLogonDenied,
                _ => SecurityStatusPalInternalError
            };

            return null;
        }
    }
}
