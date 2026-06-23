// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.DotNet.RemoteExecutor;
using Microsoft.DotNet.XUnitExtensions;
using Microsoft.Win32.SafeHandles;
using Xunit;

namespace System.Diagnostics.Tests
{
    public partial class ProcessHandlesTests
    {
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public unsafe void StartWithCallback_PosixSpawn_CanRedirectOutput()
        {
            ProcessStartInfo startInfo = new("/bin/sh")
            {
                ArgumentList = { "-c", "echo hello && echo error >&2" },
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using Process process = UnixProcessStartArguments.Start(startInfo, (UnixProcessStartArguments args) =>
            {
                int result;

                // posix_spawn_file_actions_t is a platform-specific struct (80 bytes on glibc x64, 104 bytes on macOS arm64).
                // Use 128 bytes to be safe across platforms; NativeMemory.Alloc provides sufficient native alignment.
                const int PosixSpawnFileActionsSize = 128;
                void* fileActionsBuffer = NativeMemory.Alloc(PosixSpawnFileActionsSize);
                if (fileActionsBuffer is null)
                {
                    throw new OutOfMemoryException();
                }

                try
                {
                    result = posix_spawn_file_actions_init(fileActionsBuffer);
                    if (result != 0)
                    {
                        throw new Win32Exception(result);
                    }

                    try
                    {
                        Redirect(fileActionsBuffer, args.StandardInput, 0);
                        Redirect(fileActionsBuffer, args.StandardOutput, 1);
                        Redirect(fileActionsBuffer, args.StandardError, 2);

                        int pid;
                        result = posix_spawn(&pid, args.ResolvedPath, fileActionsBuffer, null, args.Arguments, args.EnvironmentVariables);

                        if (result != 0)
                        {
                            throw new Win32Exception(result);
                        }

                        return pid;
                    }
                    finally
                    {
                        posix_spawn_file_actions_destroy(fileActionsBuffer);
                    }
                }
                finally
                {
                    NativeMemory.Free(fileActionsBuffer);
                }
            });

            (string output, string error) = process.ReadAllText();
            process.WaitForExit(WaitInMS);

            Assert.Equal("hello\n", output);
            Assert.Equal("error\n", error);
            Assert.True(process.HasExited);
            Assert.Equal(0, process.ExitCode);
        }

        private static unsafe void Redirect(void* fileActionsBuffer, nint handle, int fd)
        {
            int result = posix_spawn_file_actions_adddup2(fileActionsBuffer, (int)handle, fd);
            if (result != 0)
            {
                throw new Win32Exception(result);
            }
        }

        [DllImport("libc", SetLastError = false)]
        private static extern unsafe int posix_spawn(int* pid, byte* path, void* file_actions, void* attrp, byte** argv, byte** envp);

        [DllImport("libc", SetLastError = false)]
        private static extern unsafe int posix_spawn_file_actions_init(void* file_actions);

        [DllImport("libc", SetLastError = false)]
        private static extern unsafe int posix_spawn_file_actions_destroy(void* file_actions);

        [DllImport("libc", SetLastError = false)]
        private static extern unsafe int posix_spawn_file_actions_adddup2(void* file_actions, int fd, int newfd);

        private static string GetSafeFileHandleId(SafeFileHandle handle)
        {
            if (Interop.Sys.FStat(handle, out Interop.Sys.FileStatus status) != 0)
            {
                throw new Win32Exception();
            }
            return FormattableString.Invariant($"{status.Dev}:{status.Ino}");
        }
    }
}
