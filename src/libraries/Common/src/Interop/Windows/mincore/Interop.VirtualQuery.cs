// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.InteropServices;

internal partial class Interop
{
    internal partial class mincore
    {
        [DllImport(Libraries.Memory, EntryPoint = "VirtualQuery", CharSet = CharSet.Unicode, SetLastError = true)]
        internal extern static UIntPtr VirtualQuery(SafeMemoryMappedViewHandle lpAddress, ref MEMORY_BASIC_INFORMATION lpBuffer, UIntPtr dwLength);
    }
}
