// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class libcrypto
    {
        [DllImport(Libraries.LibCrypto)]
        internal static extern unsafe int RAND_pseudo_bytes(byte* buf, int num);
    }
}
