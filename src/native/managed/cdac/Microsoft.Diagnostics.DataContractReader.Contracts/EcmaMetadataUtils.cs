// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Text;

namespace Microsoft.Diagnostics.DataContractReader;

public static class EcmaMetadataUtils
{
    internal const int RowIdBitCount = 24;
    internal const uint RIDMask = (1 << RowIdBitCount) - 1;

    public static uint GetRowId(uint token) => token & RIDMask;

    internal static uint MakeToken(uint rid, uint table) => rid | (table << RowIdBitCount);

    // ECMA-335 II.22
    // Metadata table index is the most significant byte of the 4-byte token
    public enum TokenType : uint
    {
        mdtTypeRef = 0x01 << 24,
        mdtTypeDef = 0x02 << 24,
        mdtFieldDef = 0x04 << 24,
        mdtMethodDef = 0x06 << 24,
        mdtSignature = 0x11 << 24,
    }

    public const uint TokenTypeMask = 0xff000000;

    public static uint CreateMethodDef(uint tokenParts)
    {
        Debug.Assert((tokenParts & 0xff000000) == 0, $"Token type should not be set in {nameof(tokenParts)}");
        return (uint)TokenType.mdtMethodDef | tokenParts;
    }

    public static uint CreateFieldDef(uint tokenParts)
    {
        Debug.Assert((tokenParts & 0xff000000) == 0, $"Token type should not be set in {nameof(tokenParts)}");
        return (uint)TokenType.mdtFieldDef | tokenParts;
    }

    // ECMA-335 II.24.2.1 metadata root:
    // Signature(4) | MajorVersion(2) | MinorVersion(2) | Reserved(4) | VersionLength(4) | Version[VersionLength]
    private const ulong MetadataRootVersionLengthOffset = 12;
    private const ulong MetadataRootVersionStringOffset = 16;
    private const uint MaxMetadataVersionLength = 256;

    // Reads the metadata version string from the metadata root (ECMA-335 II.24.2.1) at the given
    // address. Returns an empty string when the address is null or no version string is present.
    public static string ReadMetadataVersion(Target target, TargetPointer metadataRootAddress)
    {
        if (metadataRootAddress == TargetPointer.Null)
        {
            return string.Empty;
        }

        uint versionLength = target.Read<uint>(metadataRootAddress + MetadataRootVersionLengthOffset);
        if (versionLength == 0)
        {
            return string.Empty;
        }

        int length = (int)Math.Min(versionLength, MaxMetadataVersionLength);
        Span<byte> buffer = stackalloc byte[length];
        target.ReadBuffer(metadataRootAddress + MetadataRootVersionStringOffset, buffer);
        int terminator = buffer.IndexOf((byte)0);
        if (terminator >= 0)
        {
            buffer = buffer[..terminator];
        }

        return Encoding.UTF8.GetString(buffer);
    }
}
