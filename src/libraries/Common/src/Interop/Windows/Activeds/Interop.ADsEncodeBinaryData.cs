// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Activeds
    {
        [LibraryImport(Libraries.Activeds)]
        internal static partial int ADsEncodeBinaryData(byte[] data, int length, ref IntPtr result);
    }
}
