// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;

namespace Internal.Cryptography.Pal.Native
{
    internal static partial class Interop
    {
        private static class Libraries
        {
            public const String Crypt32 = "crypt32.dll";
            public const String Advapi32 = "advapi32.dll";
            public const String Localization = "api-ms-win-core-localization-l1-2-0.dll";
        }
    }
}

