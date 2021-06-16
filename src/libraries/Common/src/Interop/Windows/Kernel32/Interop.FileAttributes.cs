// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        internal static partial class FileAttributes
        {
            internal const int FILE_ATTRIBUTE_NORMAL = 0x00000080;
            internal const int FILE_ATTRIBUTE_READONLY = 0x00000001;
            internal const int FILE_ATTRIBUTE_DIRECTORY = 0x00000010;
            internal const int FILE_ATTRIBUTE_REPARSE_POINT = 0x00000400;
        }
    }
}
