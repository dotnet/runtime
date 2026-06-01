// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using Xunit;

namespace System.Diagnostics.Tests
{
    public partial class ProcessHandlesTests
    {
        [Fact]
        public unsafe void StartWithCallback_PosixSpawn_CanRedirectOutput()
        {
            ProcessStartInfo startInfo = new("/bin/sh")
            {
                ArgumentList = { "-c", "echo hello" },
                RedirectStandardOutput = true,
                UseShellExecute = false
            };

            using Process process = Process.Start(startInfo, (ProcessStartArguments args) =>
            {
                int result;

                // posix_spawn_file_actions_t is a platform-specific struct (80 bytes on glibc x64, different on macOS).
                // We allocate enough space on the stack and pass a pointer.
                byte* fileActionsBuffer = stackalloc byte[PosixSpawnFileActionsSize];
                NativeMemory.Clear(fileActionsBuffer, (nuint)PosixSpawnFileActionsSize);

                result = posix_spawn_file_actions_init(fileActionsBuffer);
                if (result != 0)
                {
                    throw new Win32Exception(result);
                }

                try
                {
                    // Redirect stdin
                    result = posix_spawn_file_actions_adddup2(fileActionsBuffer, (int)args.StandardInput.DangerousGetHandle(), 0);
                    if (result != 0)
                    {
                        throw new Win32Exception(result);
                    }

                    // Redirect stdout to the pipe provided by Process
                    result = posix_spawn_file_actions_adddup2(fileActionsBuffer, (int)args.StandardOutput.DangerousGetHandle(), 1);
                    if (result != 0)
                    {
                        throw new Win32Exception(result);
                    }

                    // Redirect stderr
                    result = posix_spawn_file_actions_adddup2(fileActionsBuffer, (int)args.StandardError.DangerousGetHandle(), 2);
                    if (result != 0)
                    {
                        throw new Win32Exception(result);
                    }

                    int pid;
                    byte[] pathBytes = System.Text.Encoding.UTF8.GetBytes(args.FileName! + '\0');
                    fixed (byte* pathPtr = pathBytes)
                    {
                        result = posix_spawn(&pid, pathPtr, fileActionsBuffer, null, (byte**)args.Arguments, (byte**)args.EnvironmentVariables);
                    }

                    if (result != 0)
                    {
                        throw new Win32Exception(result);
                    }

                    // Get SafeProcessHandle from the pid.
                    // In the future, SafeProcessHandle.Open will be used instead.
                    Process spawned = Process.GetProcessById(pid);
                    return spawned.SafeHandle;
                }
                finally
                {
                    posix_spawn_file_actions_destroy(fileActionsBuffer);
                }
            });

            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit(WaitInMS);

            Assert.Equal("hello\n", output);
            Assert.True(process.HasExited);
            Assert.Equal(0, process.ExitCode);
        }

        // posix_spawn_file_actions_t is 80 bytes on glibc x64, 104 bytes on macOS arm64.
        // Use 128 bytes to be safe across platforms.
        private const int PosixSpawnFileActionsSize = 128;

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
