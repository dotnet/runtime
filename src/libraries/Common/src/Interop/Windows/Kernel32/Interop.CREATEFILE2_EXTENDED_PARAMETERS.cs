// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        internal unsafe struct CREATEFILE2_EXTENDED_PARAMETERS
        {
            internal uint dwSize;
            internal uint dwFileAttributes;
            internal uint dwFileFlags;
            internal uint dwSecurityQosFlags;
            internal Interop.Kernel32.SECURITY_ATTRIBUTES* lpSecurityAttributes;
            internal IntPtr hTemplateFile;
        }
    }
}
