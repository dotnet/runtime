// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
internal partial class Interop
{
    internal partial class Kernel32
    {
        internal partial class FileAttributes
        {
            internal const int FILE_ATTRIBUTE_NORMAL = 0x00000080;
            internal const int FILE_ATTRIBUTE_READONLY = 0x00000001;
            internal const int FILE_ATTRIBUTE_DIRECTORY = 0x00000010;
            internal const int FILE_ATTRIBUTE_REPARSE_POINT = 0x00000400;
        }
    }
}
