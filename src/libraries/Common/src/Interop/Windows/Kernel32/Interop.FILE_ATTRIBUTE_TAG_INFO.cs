// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        // From FILE_INFO_BY_HANDLE_CLASS
        // Use for GetFileInformationByHandleEx
        internal const int FileAttributeTagInfo = 9;

        [StructLayout(LayoutKind.Sequential)]
        internal struct FILE_ATTRIBUTE_TAG_INFO
        {
            internal uint FileAttributes;
            internal uint ReparseTag;
        }
    }
}
