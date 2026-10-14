// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.DotNet.RemoteExecutor;
using Microsoft.DotNet.XUnitExtensions;
using Microsoft.Win32.SafeHandles;
using Xunit;

namespace System.Diagnostics.Tests
{
    public partial class ProcessHandlesTests
    {
        // RLIMIT_NOFILE's numeric value is Linux-specific (7); other Unix platforms use different values.
        private const int RLIMIT_NOFILE = 7;
        private static readonly nuint RLIM_INFINITY = nuint.MaxValue;

        // struct rlimit uses native rlim_t fields, which are pointer-sized (32-bit on 32-bit Linux).
        [StructLayout(LayoutKind.Sequential)]
        private struct RLimit
        {
            public nuint rlim_cur;
            public nuint rlim_max;
        }

        [DllImport("libc", SetLastError = true)]
        private static extern int dup2(int oldfd, int newfd);

        [DllImport("libc", SetLastError = true)]
        private static extern int close(int fd);

        [DllImport("libc", SetLastError = true)]
        private static extern int getrlimit(int resource, out RLimit rlim);

        [DllImport("libc", SetLastError = true)]
        private static extern int setrlimit(int resource, ref RLimit rlim);

        // Regression test for a bug where SetCloexecForAllFdsFallback() (the fallback used when
        // close_range()/fdwalk() are unavailable, in pal_process.c) bounded its cleanup loop by the
        // *current* RLIMIT_NOFILE soft limit. If a high-numbered fd was opened and RLIMIT_NOFILE was
        // subsequently lowered below it (which does not close the already-open fd, only future opens),
        // the loop would stop short of that fd, leaving FD_CLOEXEC unset on it and leaking it into
        // child processes even though it wasn't in the InheritedHandles allow-list.
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)]
        public void HighNumberedFd_NotLeakedAfterLoweringRLimit()
        {
            // Everything below -- including the RLIMIT_NOFILE change -- runs inside a RemoteExecutor-invoked
            // process, so that lowering the fd limit does not affect other tests running concurrently
            // in this test process.
            using RemoteInvokeHandle remoteHandle = RemoteExecutor.Invoke(static () =>
            {
                Assert.Equal(0, getrlimit(RLIMIT_NOFILE, out RLimit limit));

                // Need enough headroom to safely carve out a high fd and then lower the limit below it,
                // while leaving plenty of room for fds this process (and Process.Start machinery) still need.
                const nuint MinRequiredLimit = 512;
                if (limit.rlim_cur == RLIM_INFINITY || limit.rlim_cur < MinRequiredLimit)
                {
                    return; // Cannot safely construct the scenario on this machine; skip.
                }

                string path = Path.GetTempFileName();
                try
                {
                    using SafeFileHandle fileHandle = File.OpenHandle(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None, FileOptions.None);
                    string id = GetSafeFileHandleId(fileHandle);

                    // Duplicate the handle to a high fd number, just below the current soft limit.
                    // dup2() always clears FD_CLOEXEC on the new fd, so it starts out inheritable,
                    // exactly like a raw fd a user opened and wants to keep alive across the limit change.
                    int highFd = (int)(limit.rlim_cur - 10);
                    if (dup2((int)fileHandle.DangerousGetHandle(), highFd) < 0)
                    {
                        throw new Win32Exception(Marshal.GetLastWin32Error());
                    }

                    try
                    {
                        // Lower RLIMIT_NOFILE below highFd. This does not close the already-open highFd,
                        // it only restricts future opens above the new limit.
                        RLimit newLimit = limit;
                        newLimit.rlim_cur = (nuint)highFd - 5;
                        Assert.Equal(0, setrlimit(RLIMIT_NOFILE, ref newLimit));

                        RemoteInvokeOptions options = new() { CheckExitCode = true };
                        options.StartInfo.InheritedHandles = []; // restrict all handle inheritance

                        using RemoteInvokeHandle grandchildHandle = RemoteExecutor.Invoke(
                            static (string fdStr, string fileId) =>
                            {
                                int fd = int.Parse(fdStr);
                                using SafeFileHandle handle = new SafeFileHandle((nint)fd, ownsHandle: false);

                                string? childId;
                                try
                                {
                                    childId = GetSafeFileHandleId(handle);
                                }
                                catch (Win32Exception)
                                {
                                    // Not a valid fd in the grandchild process: not leaked, as expected.
                                    return RemoteExecutor.SuccessExitCode;
                                }

                                Assert.NotEqual(fileId, childId);
                                return RemoteExecutor.SuccessExitCode;
                            },
                            highFd.ToString(),
                            id,
                            options);
                    }
                    finally
                    {
                        close(highFd);
                    }
                }
                finally
                {
                    File.Delete(path);
                }
            });
        }

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
                // Use 256 bytes as a conservative safe buffer across platforms; NativeMemory.Alloc provides sufficient native alignment.
                const int PosixSpawnFileActionsSize = 256;
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
