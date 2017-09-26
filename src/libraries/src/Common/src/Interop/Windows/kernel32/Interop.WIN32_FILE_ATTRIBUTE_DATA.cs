// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

internal partial class Interop
{
    internal partial class Kernel32
    {
        internal struct WIN32_FILE_ATTRIBUTE_DATA
        {
            internal int fileAttributes;
            internal uint ftCreationTimeLow;
            internal uint ftCreationTimeHigh;
            internal uint ftLastAccessTimeLow;
            internal uint ftLastAccessTimeHigh;
            internal uint ftLastWriteTimeLow;
            internal uint ftLastWriteTimeHigh;
            internal uint fileSizeHigh;
            internal uint fileSizeLow;

            internal void PopulateFrom(ref WIN32_FIND_DATA findData)
            {
                // Copy the information to data
                fileAttributes = (int)findData.dwFileAttributes;
                ftCreationTimeLow = findData.ftCreationTime.dwLowDateTime;
                ftCreationTimeHigh = findData.ftCreationTime.dwHighDateTime;
                ftLastAccessTimeLow = findData.ftLastAccessTime.dwLowDateTime;
                ftLastAccessTimeHigh = findData.ftLastAccessTime.dwHighDateTime;
                ftLastWriteTimeLow = findData.ftLastWriteTime.dwLowDateTime;
                ftLastWriteTimeHigh = findData.ftLastWriteTime.dwHighDateTime;
                fileSizeHigh = findData.nFileSizeHigh;
                fileSizeLow = findData.nFileSizeLow;
            }
        }
    }
}
