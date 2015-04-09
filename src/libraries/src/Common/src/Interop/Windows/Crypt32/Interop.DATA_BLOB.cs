// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

internal partial class Interop
{
    internal partial class Crypt32
    {
        [StructLayout(LayoutKind.Sequential)]
        internal struct DATA_BLOB
        {
            internal uint cbData;
            internal IntPtr pbData;

            internal DATA_BLOB(IntPtr handle, uint size)
            {
                cbData = size;
                pbData = handle;
            }
        }
    }
}
