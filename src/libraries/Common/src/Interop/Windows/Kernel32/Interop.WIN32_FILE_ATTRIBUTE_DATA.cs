// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        internal struct WIN32_FILE_ATTRIBUTE_DATA
        {
            internal int dwFileAttributes;
            internal FILE_TIME ftCreationTime;
            internal FILE_TIME ftLastAccessTime;
            internal FILE_TIME ftLastWriteTime;
            internal uint nFileSizeHigh;
            internal uint nFileSizeLow;

            internal void PopulateFrom(ref WIN32_FIND_DATA findData)
            {
                dwFileAttributes = (int)findData.dwFileAttributes;
                ftCreationTime = findData.ftCreationTime;
                ftLastAccessTime = findData.ftLastAccessTime;
                ftLastWriteTime = findData.ftLastWriteTime;
                nFileSizeHigh = findData.nFileSizeHigh;
                nFileSizeLow = findData.nFileSizeLow;
            }
        }
    }
}
