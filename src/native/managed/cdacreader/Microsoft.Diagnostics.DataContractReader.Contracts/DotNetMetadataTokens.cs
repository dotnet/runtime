// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Microsoft.Diagnostics.DataContractReader;

internal static class DotNetMetadataTokens
{
    public enum TokenType : uint
    {
        mdtMethodDef = 0x06000000
    }

    public static uint CreateMethodDef(uint tokenParts)
    {
        Debug.Assert((tokenParts & 0xff000000) == 0, $"Token type should not be set in {nameof(tokenParts)}");
        return (uint)TokenType.mdtMethodDef | tokenParts;
    }
}
