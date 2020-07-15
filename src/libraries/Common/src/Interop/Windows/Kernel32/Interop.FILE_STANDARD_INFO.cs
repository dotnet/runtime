// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        // From FILE_INFO_BY_HANDLE_CLASS
        // Use for GetFileInformationByHandleEx
        internal const int FileStandardInfo = 1;

        internal struct FILE_STANDARD_INFO
        {
            internal long AllocationSize;
            internal long EndOfFile;
            internal uint NumberOfLinks;
            internal BOOL DeletePending;
            internal BOOL Directory;
        }
    }
}
