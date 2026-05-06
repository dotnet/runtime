// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        // From FILE_INFO_BY_HANDLE_CLASS
        // Use for SetFileInformationByHandle
        internal const int FileEndOfFileInfo = 6;

        internal struct FILE_END_OF_FILE_INFO
        {
            internal long EndOfFile;
        }
    }
}
