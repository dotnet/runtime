// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        // From FILE_INFO_BY_HANDLE_CLASS
        // Use for GetFileInformationByHandleEx/SetFileInformationByHandle
        internal const int FileBasicInfo = 0;

        internal struct FILE_BASIC_INFO
        {
            internal long CreationTime;
            internal long LastAccessTime;
            internal long LastWriteTime;
            internal long ChangeTime;
            internal uint FileAttributes;
        }
    }
}
