// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        // From FILE_INFO_BY_HANDLE_CLASS
        // Use for SetFileInformationByHandle
        internal const int FileDispositionInfo = 4;

        internal struct FILE_DISPOSITION_INFO
        {
            internal BOOLEAN DeleteFile;
        }
    }
}
