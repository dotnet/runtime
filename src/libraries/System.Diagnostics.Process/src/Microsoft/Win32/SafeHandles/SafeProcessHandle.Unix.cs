// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
** Class:  SafeProcessHandle
**
** A wrapper for a process handle
**
**
===========================================================*/

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.Versioning;

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

        /// <summary>Finalizable holder for the underlying shared wait state object.</summary>
        internal System.Diagnostics.ProcessWaitState.Holder? _waitStateHolder;

        internal SafeProcessHandle(int processId, SafeWaitHandle handle) :
            this(handle.DangerousGetHandle(), ownsHandle: true)
        {
            ProcessId = processId;
            _handle = handle;
            handle.DangerousAddRef(ref _releaseRef);
        }

        internal int ProcessId { get; }

        protected override bool ReleaseHandle()
        {
            _waitStateHolder?.Dispose();
            _waitStateHolder = null;
            if (_releaseRef)
            {
                Debug.Assert(_handle != null);
                _handle.DangerousRelease();
            }
            return true;
        }

        private static SafeProcessHandle StartCore(System.Diagnostics.ProcessStartInfo startInfo, SafeFileHandle? stdinHandle, SafeFileHandle? stdoutHandle, SafeFileHandle? stderrHandle)
        {
            if (System.Diagnostics.Process.PlatformDoesNotSupportProcessStartAndKill)
            {
                throw new PlatformNotSupportedException();
            }

            System.Diagnostics.Process.EnsureInitialized();

            string? filename;
            string[] argv;

            string[] envp = System.Diagnostics.ProcessUtils.CreateEnvp(startInfo);
            string? cwd = !string.IsNullOrWhiteSpace(startInfo.WorkingDirectory) ? startInfo.WorkingDirectory : null;

            bool setCredentials = !string.IsNullOrEmpty(startInfo.UserName);
            uint userId = 0;
            uint groupId = 0;
            uint[]? groups = null;
            if (setCredentials)
            {
                (userId, groupId, groups) = System.Diagnostics.ProcessUtils.GetUserAndGroupIds(startInfo);
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
                    throw new System.ComponentModel.Win32Exception(Interop.Errors.ERROR_NO_ASSOCIATION);
                }

                // On Windows, UseShellExecute of executables and scripts causes those files to be executed.
                // To achieve this on Unix, we check if the file is executable (x-bit).
                // Some files may have the x-bit set even when they are not executable. This happens for example
                // when a Windows filesystem is mounted on Linux. To handle that, treat it as a regular file
                // when exec returns ENOEXEC (file format cannot be executed).
                bool isExecuting = false;
                filename = System.Diagnostics.ProcessUtils.ResolveExecutableForShellExecute(startInfo.FileName, cwd);
                if (filename != null)
                {
                    argv = System.Diagnostics.ProcessUtils.ParseArgv(startInfo);

                    SafeProcessHandle? handle = ForkAndExecProcess(
                        startInfo, filename, argv, envp, cwd,
                        setCredentials, userId, groupId, groups,
                        stdinHandle, stdoutHandle, stderrHandle, usesTerminal,
                        throwOnNoExec: false); // return null instead of throwing on ENOEXEC
                    isExecuting = handle is not null;
                    if (isExecuting)
                    {
                        return handle!;
                    }
                }

                // use default program to open file/url
                if (!isExecuting)
                {
                    filename = System.Diagnostics.ProcessUtils.GetPathToOpenFile();
                    argv = System.Diagnostics.ProcessUtils.ParseArgv(startInfo, filename, ignoreArguments: true);

                    return ForkAndExecProcess(
                        startInfo, filename, argv, envp, cwd,
                        setCredentials, userId, groupId, groups,
                        stdinHandle, stdoutHandle, stderrHandle, usesTerminal)!;
                }
            }
            else
            {
                filename = System.Diagnostics.ProcessUtils.ResolvePath(startInfo.FileName);
                argv = System.Diagnostics.ProcessUtils.ParseArgv(startInfo);
                if (Directory.Exists(filename))
                {
                    throw new System.ComponentModel.Win32Exception(SR.DirectoryNotValidAsInput);
                }

                return ForkAndExecProcess(
                    startInfo, filename, argv, envp, cwd,
                    setCredentials, userId, groupId, groups,
                    stdinHandle, stdoutHandle, stderrHandle, usesTerminal)!;
            }

            // This should not be reached.
            return new SafeProcessHandle();
        }

        private static SafeProcessHandle? ForkAndExecProcess(
            System.Diagnostics.ProcessStartInfo startInfo, string? resolvedFilename, string[] argv,
            string[] envp, string? cwd, bool setCredentials, uint userId,
            uint groupId, uint[]? groups,
            SafeFileHandle? stdinHandle, SafeFileHandle? stdoutHandle, SafeFileHandle? stderrHandle,
            bool usesTerminal, bool throwOnNoExec = true)
        {
            if (string.IsNullOrEmpty(resolvedFilename))
            {
                Interop.ErrorInfo errno = Interop.Error.ENOENT.Info();
                throw System.Diagnostics.ProcessUtils.CreateExceptionForErrorStartingProcess(errno.GetErrorMessage(), errno.RawErrno, startInfo.FileName, cwd);
            }

            // Lock to avoid races with OnSigChild
            // By using a ReaderWriterLock we allow multiple processes to start concurrently.
            System.Diagnostics.ProcessUtils.s_processStartLock.EnterReadLock();
            System.Diagnostics.ProcessWaitState.Holder? waitStateHolder = null;
            try
            {
                if (usesTerminal)
                {
                    System.Diagnostics.Process.ConfigureTerminalForChildProcesses(1);
                }

                int childPid;

                // Invoke the shim fork/execve routine.  It will fork a child process,
                // map the provided file handles onto the appropriate stdin/stdout/stderr
                // descriptors, and execve to execute the requested process.  The shim implementation
                // is used to fork/execve as executing managed code in a forked process is not safe (only
                // the calling thread will transfer, thread IDs aren't stable across the fork, etc.)
                int errno = Interop.Sys.ForkAndExecProcess(
                    resolvedFilename, argv, envp, cwd,
                    setCredentials, userId, groupId, groups,
                    out childPid, stdinHandle, stdoutHandle, stderrHandle);

                if (errno == 0)
                {
                    // Ensure we'll reap this process.
                    // note: SetProcessId will set this if we don't set it first.
                    waitStateHolder = new System.Diagnostics.ProcessWaitState.Holder(childPid, isNewChild: true, usesTerminal);

                    // Store the child's information into this Process object.
                    Debug.Assert(childPid >= 0);
                    SafeProcessHandle processHandle = new SafeProcessHandle(childPid, waitStateHolder._state.EnsureExitedEvent().GetSafeWaitHandle());
                    processHandle._waitStateHolder = waitStateHolder;

                    return processHandle;
                }
                else
                {
                    if (!throwOnNoExec &&
                        new Interop.ErrorInfo(errno).Error == Interop.Error.ENOEXEC)
                    {
                        return null;
                    }

                    throw System.Diagnostics.ProcessUtils.CreateExceptionForErrorStartingProcess(new Interop.ErrorInfo(errno).GetErrorMessage(), errno, resolvedFilename, cwd);
                }
            }
            finally
            {
                System.Diagnostics.ProcessUtils.s_processStartLock.ExitReadLock();

                if (waitStateHolder == null && usesTerminal)
                {
                    // We failed to launch a child that could use the terminal.
                    System.Diagnostics.ProcessUtils.s_processStartLock.EnterWriteLock();
                    System.Diagnostics.Process.ConfigureTerminalForChildProcesses(-1);
                    System.Diagnostics.ProcessUtils.s_processStartLock.ExitWriteLock();
                }
            }
        }
    }
}
