// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

internal static partial class Interop
{
    internal static partial class libc
    {
        internal static class FileDescriptors
        {
            internal const int STDIN_FILENO = 0;
            internal const int STDOUT_FILENO = 1;
            internal const int STDERR_FILENO = 2;
        }
    }
}
