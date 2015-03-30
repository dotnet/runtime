// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;

using pid_t = System.Int32;

internal static partial class Interop
{
    internal static partial class libc
    {
        [DllImport(Libraries.Libc, SetLastError = true)]
        internal static extern pid_t waitpid(pid_t pid, out int status, WaitPidOptions options);

        internal enum WaitPidOptions
        {
            None = 0,
            WNOHANG = 1,
            WUNTRACED = 2
        }

        internal static int WEXITSTATUS(int status)
        {
            return (status & 0xFF00) >> 8;
        }

        internal static bool WIFEXITED(int status)
        {
            return WTERMSIG(status) == 0;
        }

        internal static bool WIFSIGNALED(int status)
        {
            return ((sbyte)(((status) & 0x7f) + 1) >> 1) > 0;
        }

        internal static int WTERMSIG(int status)
        {
            return status & 0x7F;
        }
    }
}
