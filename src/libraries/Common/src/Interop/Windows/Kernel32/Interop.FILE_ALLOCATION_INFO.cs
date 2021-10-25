// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        // Value taken from https://docs.microsoft.com/en-us/windows/win32/api/fileapi/nf-fileapi-setfileinformationbyhandle#remarks:
        internal const int FileAllocationInfo = 5;

        internal struct FILE_ALLOCATION_INFO
        {
            internal long AllocationSize;
        }
    }
}
