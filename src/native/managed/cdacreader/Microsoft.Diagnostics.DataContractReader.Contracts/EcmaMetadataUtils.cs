// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader;

internal static class EcmaMetadataUtils
{
    internal const int RowIdBitCount = 24;
    internal const uint RIDMask = (1 << RowIdBitCount) - 1;

    internal static uint GetRowId(uint token) => token & RIDMask;

    internal static uint MakeToken(uint rid, uint table) => rid | (table << RowIdBitCount);

}
