// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

internal partial class Interop
{
    internal partial class mincore
    {
        [DllImport(Libraries.ProcessEnviornment, SetLastError = true)]
        internal static extern IntPtr GetStdHandle(int nStdHandle);  // param is NOT a handle, but it returns one!
    }
}
