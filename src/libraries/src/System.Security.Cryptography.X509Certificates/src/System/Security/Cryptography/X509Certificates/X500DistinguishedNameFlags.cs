// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;

namespace System.Security.Cryptography.X509Certificates
{
    [Flags]
    public enum X500DistinguishedNameFlags
    {
        None = 0x0000,
        Reversed = 0x0001,

        UseSemicolons = 0x0010,
        DoNotUsePlusSign = 0x0020,
        DoNotUseQuotes = 0x0040,
        UseCommas = 0x0080,
        UseNewLines = 0x0100,

        UseUTF8Encoding = 0x1000,
        UseT61Encoding = 0x2000,
        ForceUTF8Encoding = 0x4000,
    }
}

