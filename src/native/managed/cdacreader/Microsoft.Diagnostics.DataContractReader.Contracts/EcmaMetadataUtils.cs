// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Microsoft.Diagnostics.DataContractReader;

internal static class EcmaMetadataUtils
{
    internal const int RowIdBitCount = 24;
    internal const uint RIDMask = (1 << RowIdBitCount) - 1;

    internal static uint GetRowId(uint token) => token & RIDMask;

    internal static uint MakeToken(uint rid, uint table) => rid | (table << RowIdBitCount);

    // ECMA-335 II.22
    // Metadata table index is the most significant byte of the 4-byte token
    public enum TokenType : uint
    {
        mdtMethodDef = 0x06 << 24
    }

    public static uint CreateMethodDef(uint tokenParts)
    {
        Debug.Assert((tokenParts & 0xff000000) == 0, $"Token type should not be set in {nameof(tokenParts)}");
        return (uint)TokenType.mdtMethodDef | tokenParts;
    }
}
