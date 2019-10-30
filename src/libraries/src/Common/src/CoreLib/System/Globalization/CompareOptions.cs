// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Globalization
{
    [Flags]
    public enum CompareOptions
    {
        None = 0x00000000,
        IgnoreCase = 0x00000001,
        IgnoreNonSpace = 0x00000002,
        IgnoreSymbols = 0x00000004,
        IgnoreKanaType = 0x00000008,
        IgnoreWidth = 0x00000010,
        OrdinalIgnoreCase = 0x10000000, // This flag can not be used with other flags.
        StringSort = 0x20000000,
        Ordinal = 0x40000000, // This flag can not be used with other flags.
    }
}
