// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace System.Net.Security
{
    /// <summary>
    /// Decodes the KERB_VALIDATION_INFO structure carried in the "urn:mspac:logon-info" buffer
    /// of a Kerberos Privilege Attribute Certificate (PAC).
    /// </summary>
    /// <remarks>
    /// The structure is serialized using NDR type serialization version 1 as described in
    /// [MS-RPCE] 2.2.6, wrapping the KERB_VALIDATION_INFO defined in [MS-PAC] 2.5.
    ///
    /// Only the fields needed to describe the peer identity are retained. The remaining fields
    /// are still parsed, because NDR is a positional format and skipping a field would desync
    /// the reader from the stream.
    ///
    /// Every read is bounds checked and any malformed input results in a null result rather
    /// than an exception.
    /// </remarks>
    internal sealed class KerberosPacLogonInfo
    {
        // A PAC describing a user with more groups than this is treated as malformed. The value
        // is far above what Active Directory can issue, since the resulting ticket would exceed
        // the maximum Kerberos token size long before this limit is reached.
        private const int MaxGroupCount = 8192;

        private const byte NdrLittleEndian = 0x10;
        private const byte NdrVersion = 1;
        private const int NdrCommonHeaderLength = 8;

        public string? EffectiveName { get; private set; }
        public string? LogonDomainName { get; private set; }
        public string? UserSid { get; private set; }
        public string? PrimaryGroupSid { get; private set; }
        public List<string> GroupSids { get; } = new List<string>();

        /// <summary>
        /// Decodes the logon information, returning null if <paramref name="buffer"/> is not a
        /// well formed KERB_VALIDATION_INFO.
        /// </summary>
        public static KerberosPacLogonInfo? Decode(ReadOnlySpan<byte> buffer)
        {
            NdrReader reader = new NdrReader(buffer);

            if (!TryReadTypeSerializationHeader(ref reader))
            {
                return null;
            }

            // The root of the serialized type is a unique pointer. A null root carries no
            // information and is treated as malformed.
            if (!reader.TryReadUInt32(out uint rootReferent) || rootReferent == 0)
            {
                return null;
            }

            KerberosPacLogonInfo logonInfo = new KerberosPacLogonInfo();
            return logonInfo.TryReadValidationInfo(ref reader) ? logonInfo : null;
        }

        private static bool TryReadTypeSerializationHeader(ref NdrReader reader)
        {
            // Common type header, [MS-RPCE] 2.2.6.1.
            if (!reader.TryReadByte(out byte version) || version != NdrVersion ||
                !reader.TryReadByte(out byte endianness) ||
                !reader.TryReadUInt16(out ushort commonHeaderLength) || commonHeaderLength != NdrCommonHeaderLength ||
                !reader.TrySkip(4))
            {
                return false;
            }

            // Only little-endian integers with an ASCII character set are produced in practice,
            // and byte swapping for the alternative has no way of being tested.
            if ((endianness & 0xF0) != NdrLittleEndian)
            {
                return false;
            }

            // Private header, [MS-RPCE] 2.2.6.2. The declared length is not trusted for bounds
            // checking; the reader validates every access against the real buffer instead.
            return reader.TryReadUInt32(out _) && reader.TrySkip(4);
        }

        private bool TryReadValidationInfo(ref NdrReader reader)
        {
            // Fixed part of KERB_VALIDATION_INFO, [MS-PAC] 2.5. Pointer fields carry only a
            // referent id here; the data they point at is deferred to after the fixed part and
            // appears in field order.
            if (!reader.TrySkip(6 * 8))                                  // LogonTime .. PasswordMustChange
            {
                return false;
            }

            Span<uint> nameReferents = stackalloc uint[6];
            for (int i = 0; i < nameReferents.Length; i++)
            {
                // EffectiveName, FullName, LogonScript, ProfilePath, HomeDirectory, HomeDirectoryDrive
                if (!TryReadUnicodeStringHeader(ref reader, out nameReferents[i]))
                {
                    return false;
                }
            }

            if (!reader.TryReadUInt16(out _) ||                          // LogonCount
                !reader.TryReadUInt16(out _) ||                          // BadPasswordCount
                !reader.TryReadUInt32(out uint userId) ||
                !reader.TryReadUInt32(out uint primaryGroupId) ||
                !reader.TryReadUInt32(out uint groupCount) ||
                !reader.TryReadUInt32(out uint groupIdsReferent) ||
                !reader.TryReadUInt32(out _) ||                          // UserFlags
                !reader.TrySkip(16))                                     // UserSessionKey
            {
                return false;
            }

            if (!TryReadUnicodeStringHeader(ref reader, out uint logonServerReferent) ||
                !TryReadUnicodeStringHeader(ref reader, out uint logonDomainNameReferent) ||
                !reader.TryReadUInt32(out uint logonDomainIdReferent) ||
                !reader.TrySkip(8) ||                                    // Reserved1[2]
                !reader.TryReadUInt32(out _) ||                          // UserAccountControl
                !reader.TryReadUInt32(out _) ||                          // SubAuthStatus
                !reader.TrySkip(8) ||                                    // LastSuccessfulILogon
                !reader.TrySkip(8) ||                                    // LastFailedILogon
                !reader.TryReadUInt32(out _) ||                          // FailedILogonCount
                !reader.TryReadUInt32(out _) ||                          // Reserved3
                !reader.TryReadUInt32(out uint sidCount) ||
                !reader.TryReadUInt32(out uint extraSidsReferent) ||
                !reader.TryReadUInt32(out uint resourceDomainIdReferent) ||
                !reader.TryReadUInt32(out uint resourceGroupCount) ||
                !reader.TryReadUInt32(out uint resourceGroupIdsReferent))
            {
                return false;
            }

            // Deferred pointer data, in the order the pointers appear above.
            string?[] names = new string?[nameReferents.Length];
            for (int i = 0; i < nameReferents.Length; i++)
            {
                if (!TryReadUnicodeStringData(ref reader, nameReferents[i], out names[i]))
                {
                    return false;
                }
            }

            if (!TryReadGroupMemberships(ref reader, groupIdsReferent, groupCount, out uint[]? groupRids) ||
                !TryReadUnicodeStringData(ref reader, logonServerReferent, out _) ||
                !TryReadUnicodeStringData(ref reader, logonDomainNameReferent, out string? logonDomainName) ||
                !TryReadSid(ref reader, logonDomainIdReferent, out string? logonDomainSid) ||
                !TryReadExtraSids(ref reader, extraSidsReferent, sidCount, out List<string>? extraSids) ||
                !TryReadSid(ref reader, resourceDomainIdReferent, out string? resourceDomainSid) ||
                !TryReadGroupMemberships(ref reader, resourceGroupIdsReferent, resourceGroupCount, out uint[]? resourceGroupRids))
            {
                return false;
            }

            EffectiveName = names[0];
            LogonDomainName = logonDomainName;

            // Group membership in the logon domain is expressed as relative identifiers that are
            // only meaningful when combined with the domain SID. Without it there is nothing to
            // report, but the PAC is still well formed.
            if (logonDomainSid is not null)
            {
                UserSid = FormatRid(logonDomainSid, userId);
                PrimaryGroupSid = FormatRid(logonDomainSid, primaryGroupId);

                if (groupRids is not null)
                {
                    foreach (uint rid in groupRids)
                    {
                        GroupSids.Add(FormatRid(logonDomainSid, rid));
                    }
                }
            }

            if (extraSids is not null)
            {
                GroupSids.AddRange(extraSids);
            }

            if (resourceDomainSid is not null && resourceGroupRids is not null)
            {
                foreach (uint rid in resourceGroupRids)
                {
                    GroupSids.Add(FormatRid(resourceDomainSid, rid));
                }
            }

            return true;
        }

        private static string FormatRid(string domainSid, uint relativeId) =>
            string.Create(CultureInfo.InvariantCulture, $"{domainSid}-{relativeId}");

        private static bool TryReadUnicodeStringHeader(ref NdrReader reader, out uint referent)
        {
            // RPC_UNICODE_STRING: the lengths are byte counts and are redundant with the counts
            // carried by the deferred conformant varying array, so only the referent is kept.
            referent = 0;
            return reader.TryReadUInt16(out _) &&
                   reader.TryReadUInt16(out _) &&
                   reader.TryReadUInt32(out referent);
        }

        private static bool TryReadUnicodeStringData(ref NdrReader reader, uint referent, out string? value)
        {
            value = null;
            if (referent == 0)
            {
                return true;
            }

            // Conformant varying array of wchar.
            if (!reader.TryReadUInt32(out uint maxCount) ||
                !reader.TryReadUInt32(out uint offset) ||
                !reader.TryReadUInt32(out uint actualCount) ||
                offset != 0 ||
                actualCount > maxCount)
            {
                return false;
            }

            if (actualCount > int.MaxValue / 2 ||
                !reader.TryReadBytes((int)actualCount * 2, out ReadOnlySpan<byte> chars))
            {
                return false;
            }

            // Some producers include the terminating null in the character count.
            value = Encoding.Unicode.GetString(chars).TrimEnd('\0');
            return true;
        }

        private static bool TryReadGroupMemberships(ref NdrReader reader, uint referent, uint count, out uint[]? relativeIds)
        {
            relativeIds = null;
            if (referent == 0)
            {
                return true;
            }

            // Conformant array of GROUP_MEMBERSHIP.
            if (!reader.TryReadUInt32(out uint maxCount) || maxCount != count || count > MaxGroupCount)
            {
                return false;
            }

            uint[] result = new uint[count];
            for (int i = 0; i < result.Length; i++)
            {
                if (!reader.TryReadUInt32(out result[i]) ||
                    !reader.TryReadUInt32(out _))                        // Attributes
                {
                    return false;
                }
            }

            relativeIds = result;
            return true;
        }

        private static bool TryReadExtraSids(ref NdrReader reader, uint referent, uint count, out List<string>? sids)
        {
            sids = null;
            if (referent == 0)
            {
                return true;
            }

            // Conformant array of KERB_SID_AND_ATTRIBUTES. The SID pointers within the array are
            // themselves deferred, so the array is read in two passes.
            if (!reader.TryReadUInt32(out uint maxCount) || maxCount != count || count > MaxGroupCount)
            {
                return false;
            }

            uint[] referents = new uint[count];
            for (int i = 0; i < referents.Length; i++)
            {
                if (!reader.TryReadUInt32(out referents[i]) ||
                    !reader.TryReadUInt32(out _))                        // Attributes
                {
                    return false;
                }
            }

            List<string> result = new List<string>();
            foreach (uint sidReferent in referents)
            {
                if (!TryReadSid(ref reader, sidReferent, out string? sid))
                {
                    return false;
                }

                if (sid is not null)
                {
                    result.Add(sid);
                }
            }

            sids = result;
            return true;
        }

        private static bool TryReadSid(ref NdrReader reader, uint referent, out string? sid)
        {
            sid = null;
            if (referent == 0)
            {
                return true;
            }

            // RPC_SID is a conformant structure whose maximum count carries the sub authority
            // count, duplicating the field inside the structure. Both must agree.
            if (!reader.TryReadUInt32(out uint maxCount) ||
                !reader.TryReadByte(out byte revision) ||
                !reader.TryReadByte(out byte subAuthorityCount) ||
                !reader.TryReadBytes(6, out ReadOnlySpan<byte> identifierAuthority) ||
                maxCount != subAuthorityCount)
            {
                return false;
            }

            ulong authority = 0;
            foreach (byte b in identifierAuthority)
            {
                authority = (authority << 8) | b;
            }

            StringBuilder builder = new StringBuilder();
            builder.Append("S-").Append(revision).Append('-');

            // Authorities that do not fit in 32 bits are written in hexadecimal, matching the
            // SDDL string format produced by Windows.
            if (authority <= uint.MaxValue)
            {
                builder.Append(authority.ToString(CultureInfo.InvariantCulture));
            }
            else
            {
                builder.Append("0x").Append(authority.ToString("x12", CultureInfo.InvariantCulture));
            }

            for (int i = 0; i < subAuthorityCount; i++)
            {
                if (!reader.TryReadUInt32(out uint subAuthority))
                {
                    return false;
                }

                builder.Append('-').Append(subAuthority.ToString(CultureInfo.InvariantCulture));
            }

            sid = builder.ToString();
            return true;
        }

        /// <summary>
        /// A forward-only reader over an NDR octet stream that fails rather than throwing when
        /// the stream is truncated.
        /// </summary>
        private ref struct NdrReader
        {
            private readonly ReadOnlySpan<byte> _buffer;
            private int _position;

            public NdrReader(ReadOnlySpan<byte> buffer)
            {
                _buffer = buffer;
                _position = 0;
            }

            // NDR aligns each primitive to its own size relative to the start of the octet
            // stream. The type serialization headers that precede the data are 16 bytes, a
            // multiple of the largest alignment used here, so offsets within this buffer and
            // offsets within the NDR stream agree.
            private bool TryAlign(int alignment)
            {
                int padding = (alignment - (_position % alignment)) % alignment;
                return TrySkip(padding);
            }

            public bool TrySkip(int count)
            {
                if (count < 0 || _buffer.Length - _position < count)
                {
                    return false;
                }

                _position += count;
                return true;
            }

            public bool TryReadByte(out byte value)
            {
                if (_position >= _buffer.Length)
                {
                    value = 0;
                    return false;
                }

                value = _buffer[_position++];
                return true;
            }

            public bool TryReadUInt16(out ushort value)
            {
                value = 0;
                if (!TryAlign(sizeof(ushort)) || !TryReadBytes(sizeof(ushort), out ReadOnlySpan<byte> bytes))
                {
                    return false;
                }

                value = BinaryPrimitives.ReadUInt16LittleEndian(bytes);
                return true;
            }

            public bool TryReadUInt32(out uint value)
            {
                value = 0;
                if (!TryAlign(sizeof(uint)) || !TryReadBytes(sizeof(uint), out ReadOnlySpan<byte> bytes))
                {
                    return false;
                }

                value = BinaryPrimitives.ReadUInt32LittleEndian(bytes);
                return true;
            }

            public bool TryReadBytes(int count, out ReadOnlySpan<byte> value)
            {
                if (count < 0 || _buffer.Length - _position < count)
                {
                    value = default;
                    return false;
                }

                value = _buffer.Slice(_position, count);
                _position += count;
                return true;
            }
        }
    }
}
