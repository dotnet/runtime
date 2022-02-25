// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Version
    {
        [GeneratedDllImport(Libraries.Version, EntryPoint = "VerQueryValueW", StringMarshalling = StringMarshalling.Utf16)]
        internal static partial bool VerQueryValue(IntPtr pBlock, string lpSubBlock, out IntPtr lplpBuffer, out uint puLen);
    }
}
