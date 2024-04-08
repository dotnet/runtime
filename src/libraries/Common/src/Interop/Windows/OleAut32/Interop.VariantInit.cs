// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class OleAut32
    {
        [LibraryImport(Libraries.OleAut32)]
        internal static partial void VariantInit(IntPtr pObject);
    }
}
