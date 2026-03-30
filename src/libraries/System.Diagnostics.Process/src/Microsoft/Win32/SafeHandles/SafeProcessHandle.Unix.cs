// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security;
using System.Text;
using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace Microsoft.Win32.SafeHandles
{
    public sealed partial class SafeProcessHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        // On Windows, SafeProcessHandle represents the actual OS handle for the process.
        // On Unix, there's no such concept.  Instead, the implementation manufactures
        // a WaitHandle that it manually sets when the process completes; SafeProcessHandle
        // then just wraps that same WaitHandle instance.  This allows consumers that use
        // Process.{Safe}Handle to initialize and use a WaitHandle to successfully use it on
        // Unix as well to wait for the process to complete.

        private readonly SafeWaitHandle? _handle;
        private readonly bool _releaseRef;

        private SafeProcessHandle(int processId, ProcessWaitState.Holder waitStateHolder) : base(ownsHandle: true)
        {
            ProcessId = processId;

            _handle = waitStateHolder._state.EnsureExitedEvent().GetSafeWaitHandle();
            _handle.DangerousAddRef(ref _releaseRef);
            SetHandle(_handle.DangerousGetHandle());
        }

        internal SafeProcessHandle(int processId, SafeWaitHandle handle) :
            this(handle.DangerousGetHandle(), ownsHandle: true)
        {
            ProcessId = processId;
            _handle = handle;
            handle.DangerousAddRef(ref _releaseRef);
        }

        protected override bool ReleaseHandle()
        {
            if (_releaseRef)
            {
                Debug.Assert(_handle != null);
                _handle.DangerousRelease();
            }
            return true;
        }

        // On Unix, we don't use process descriptors yet, so we can't get PID.
        private static int GetProcessIdCore() => throw new PlatformNotSupportedException();

        private static SafeProcessHandle StartCore(ProcessStartInfo startInfo, SafeFileHandle? stdinHandle, SafeFileHandle? stdoutHandle, SafeFileHandle? stderrHandle)
        {
            SafeProcessHandle startedProcess = StartCore(startInfo, stdinHandle, stdoutHandle, stderrHandle, out ProcessWaitState.Holder? waitStateHolder);

            // For standalone SafeProcessHandle.Start, we dispose the wait state holder immediately.
            // The DangerousAddRef on the SafeWaitHandle (Unix) keeps the OS handle alive.
            waitStateHolder?.Dispose();

            return startedProcess;
        }

        internal static SafeProcessHandle StartCore(ProcessStartInfo startInfo, SafeFileHandle? stdinHandle, SafeFileHandle? stdoutHandle, SafeFileHandle? stderrHandle, out ProcessWaitState.Holder? waitStateHolder)
        {
            waitStateHolder = null;

            if (ProcessUtils.PlatformDoesNotSupportProcessStartAndKill)
            {
                throw new PlatformNotSupportedException();
            }

            ProcessUtils.EnsureInitialized();

            string? filename;
            string[] argv;

            IDictionary<string, string?> env = startInfo.Environment;
            string? cwd = !string.IsNullOrWhiteSpace(startInfo.WorkingDirectory) ? startInfo.WorkingDirectory : null;

            bool setCredentials = !string.IsNullOrEmpty(startInfo.UserName);
            uint userId = 0;
            uint groupId = 0;
            uint[]? groups = null;
            if (setCredentials)
            {
                (userId, groupId, groups) = ProcessUtils.GetUserAndGroupIds(startInfo);
            }

            // .NET applications don't echo characters unless there is a Console.Read operation.
            // Unix applications expect the terminal to be in an echoing state by default.
            // To support processes that interact with the terminal (e.g. 'vi'), we need to configure the
            // terminal to echo. We keep this configuration as long as there are children possibly using the terminal.
            // Handle can be null only for UseShellExecute or platforms that don't support Console.Open* methods like Android.
            bool usesTerminal = (stdinHandle is not null && Interop.Sys.IsATty(stdinHandle))
                || (stdoutHandle is not null && Interop.Sys.IsATty(stdoutHandle))
                || (stderrHandle is not null && Interop.Sys.IsATty(stderrHandle));

            if (startInfo.UseShellExecute)
            {
                string verb = startInfo.Verb;
                if (verb != string.Empty &&
                    !string.Equals(verb, "open", StringComparison.OrdinalIgnoreCase))
                {
                    throw new Win32Exception(Interop.Errors.ERROR_NO_ASSOCIATION);
                }

                // On Windows, UseShellExecute of executables and scripts causes those files to be executed.
                // To achieve this on Unix, we check if the file is executable (x-bit).
                // Some files may have the x-bit set even when they are not executable. This happens for example
                // when a Windows filesystem is mounted on Linux. To handle that, treat it as a regular file
                // when exec returns ENOEXEC (file format cannot be executed).
                filename = ProcessUtils.ResolveExecutableForShellExecute(startInfo.FileName, cwd);
                if (filename != null)
                {
                    argv = ProcessUtils.ParseArgv(startInfo);

                    SafeProcessHandle processHandle = ForkAndExecProcess(
                        startInfo, filename, argv, env, cwd,
                        setCredentials, userId, groupId, groups,
                        stdinHandle, stdoutHandle, stderrHandle, usesTerminal,
                        out waitStateHolder,
                        throwOnNoExec: false); // return invalid handle instead of throwing on ENOEXEC

                    if (!processHandle.IsInvalid)
                    {
                        return processHandle;
                    }
                }

                // use default program to open file/url
                filename = Process.GetPathToOpenFile();
                argv = ProcessUtils.ParseArgv(startInfo, filename, ignoreArguments: true);

                return ForkAndExecProcess(
                    startInfo, filename, argv, env, cwd,
                    setCredentials, userId, groupId, groups,
                    stdinHandle, stdoutHandle, stderrHandle, usesTerminal,
                    out waitStateHolder);
            }
            else
            {
                filename = ProcessUtils.ResolvePath(startInfo.FileName);
                argv = ProcessUtils.ParseArgv(startInfo);
                if (Directory.Exists(filename))
                {
                    throw new Win32Exception(SR.DirectoryNotValidAsInput);
                }

                return ForkAndExecProcess(
                    startInfo, filename, argv, env, cwd,
                    setCredentials, userId, groupId, groups,
                    stdinHandle, stdoutHandle, stderrHandle, usesTerminal,
                    out waitStateHolder);
            }
        }

        private static SafeProcessHandle ForkAndExecProcess(
            ProcessStartInfo startInfo, string? resolvedFilename, string[] argv,
            IDictionary<string, string?> env, string? cwd, bool setCredentials, uint userId,
            uint groupId, uint[]? groups,
            SafeFileHandle? stdinHandle, SafeFileHandle? stdoutHandle, SafeFileHandle? stderrHandle,
            bool usesTerminal, out ProcessWaitState.Holder? waitStateHolder, bool throwOnNoExec = true)
        {
            waitStateHolder = null;

            if (string.IsNullOrEmpty(resolvedFilename))
            {
                Interop.ErrorInfo error = Interop.Error.ENOENT.Info();
                throw ProcessUtils.CreateExceptionForErrorStartingProcess(error.GetErrorMessage(), error.RawErrno, startInfo.FileName, cwd);
            }

            int childPid, errno;

            // Lock to avoid races with OnSigChild
            // By using a ReaderWriterLock we allow multiple processes to start concurrently.
            ProcessUtils.s_processStartLock.EnterReadLock();
            try
            {
                if (usesTerminal)
                {
                    ProcessUtils.ConfigureTerminalForChildProcesses(1);
                }

                // Invoke the shim fork/execve routine.  It will fork a child process,
                // map the provided file handles onto the appropriate stdin/stdout/stderr
                // descriptors, and execve to execute the requested process.  The shim implementation
                // is used to fork/execve as executing managed code in a forked process is not safe (only
                // the calling thread will transfer, thread IDs aren't stable across the fork, etc.)
                errno = Interop.Sys.ForkAndExecProcess(
                    resolvedFilename, argv, env, cwd,
                    setCredentials, userId, groupId, groups,
                    out childPid, stdinHandle, stdoutHandle, stderrHandle);

                if (errno == 0)
                {
                    // Create the wait state holder while still holding the read lock.
                    // This ensures the child process is registered in s_childProcessWaitStates
                    // before the lock is released. If SIGCHLD fires after the lock is released,
                    // CheckChildren will find the child in the table and reap it properly.
                    // Without this, there is a race: SIGCHLD could fire after the lock is released
                    // but before the child is registered, causing WaitForExit to hang indefinitely.
                    waitStateHolder = new ProcessWaitState.Holder(childPid, isNewChild: true, usesTerminal);
                }
            }
            finally
            {
                ProcessUtils.s_processStartLock.ExitReadLock();
            }

            if (errno != 0)
            {
                if (usesTerminal)
                {
                    // We failed to launch a child that could use the terminal.
                    ProcessUtils.s_processStartLock.EnterWriteLock();
                    ProcessUtils.ConfigureTerminalForChildProcesses(-1);
                    ProcessUtils.s_processStartLock.ExitWriteLock();
                }

                if (!throwOnNoExec &&
                    new Interop.ErrorInfo(errno).Error == Interop.Error.ENOEXEC)
                {
                    return InvalidHandle;
                }

                throw ProcessUtils.CreateExceptionForErrorStartingProcess(new Interop.ErrorInfo(errno).GetErrorMessage(), errno, resolvedFilename, cwd);
            }

            return new SafeProcessHandle(childPid, waitStateHolder!);
        }
    }
}
