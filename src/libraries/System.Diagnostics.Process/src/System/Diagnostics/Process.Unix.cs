// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security;
using System.Text;
using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace System.Diagnostics
{
    public partial class Process : IDisposable
    {
        /// <summary>
        /// Puts a Process component in state to interact with operating system processes that run in a
        /// special mode by enabling the native property SeDebugPrivilege on the current thread.
        /// </summary>
        public static void EnterDebugMode()
        {
            // Nop.
        }

        /// <summary>
        /// Takes a Process component out of the state that lets it interact with operating system processes
        /// that run in a special mode.
        /// </summary>
        public static void LeaveDebugMode()
        {
            // Nop.
        }

        [CLSCompliant(false)]
        [SupportedOSPlatform("windows")]
        public static Process Start(string fileName, string userName, SecureString password, string domain)
        {
            throw new PlatformNotSupportedException(SR.ProcessStartWithPasswordAndDomainNotSupported);
        }

        [CLSCompliant(false)]
        [SupportedOSPlatform("windows")]
        public static Process Start(string fileName, string arguments, string userName, SecureString password, string domain)
        {
            throw new PlatformNotSupportedException(SR.ProcessStartWithPasswordAndDomainNotSupported);
        }

        /// <summary>Terminates the associated process immediately.</summary>
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [SupportedOSPlatform("maccatalyst")]
        public void Kill()
        {
            if (!ProcessUtils.PlatformSupportsProcessStartAndKill)
            {
                throw new PlatformNotSupportedException();
            }

            EnsureState(State.HaveId);

            // Check if we know the process has exited. This avoids us targeting another
            // process that has a recycled PID. This only checks our internal state, the Kill call below
            // actively checks if the process is still alive.
            if (GetHasExited(refresh: false))
            {
                return;
            }

            int killResult = Interop.Sys.Kill(_processId, Interop.Sys.GetPlatformSignalNumber(PosixSignal.SIGKILL));
            if (killResult != 0)
            {
                Interop.Error error = Interop.Sys.GetLastError();

                // Don't throw if the process has exited.
                if (error == Interop.Error.ESRCH)
                {
                    return;
                }

                throw new Win32Exception(); // same exception as on Windows
            }
        }

        private bool GetHasExited(bool refresh)
            => GetWaitState().GetExited(out _, refresh);

        private List<Exception>? KillTree()
        {
            List<Exception>? exceptions = null;
            KillTree(ref exceptions);
            return exceptions;
        }

        private void KillTree(ref List<Exception>? exceptions)
        {
            // If the process has exited, we can no longer determine its children.
            // If we know the process has exited, stop already.
            if (GetHasExited(refresh: false))
            {
                return;
            }

            // Stop the process, so it won't start additional children.
            // This is best effort: kill can return before the process is stopped.
            int stopResult = Interop.Sys.Kill(_processId, Interop.Sys.GetPlatformSIGSTOP());
            if (stopResult != 0)
            {
                Interop.Error error = Interop.Sys.GetLastError();
                // Ignore 'process no longer exists' error.
                if (error != Interop.Error.ESRCH)
                {
                    (exceptions ??= new List<Exception>()).Add(new Win32Exception());
                }
                return;
            }

            List<Process> children = GetChildProcesses();

            int killResult = Interop.Sys.Kill(_processId, Interop.Sys.GetPlatformSignalNumber(PosixSignal.SIGKILL));
            if (killResult != 0)
            {
                Interop.Error error = Interop.Sys.GetLastError();
                // Ignore 'process no longer exists' error.
                if (error != Interop.Error.ESRCH)
                {
                    (exceptions ??= new List<Exception>()).Add(new Win32Exception());
                }
            }

            foreach (Process childProcess in children)
            {
                childProcess.KillTree(ref exceptions);
                childProcess.Dispose();
            }
        }

        /// <summary>Discards any information about the associated process.</summary>
        partial void RefreshCore();

        /// <summary>Additional logic invoked when the Process is closed.</summary>
        private void CloseCore()
        {
            if (_waitStateHolder != null)
            {
                _waitStateHolder.Dispose();
                _waitStateHolder = null;
            }
        }

        /// <summary>Additional configuration when a process ID is set.</summary>
        partial void ConfigureAfterProcessIdSet()
        {
            // Make sure that we configure the wait state holder for this process object, which we can only do once we have a process ID.
            Debug.Assert(_haveProcessId, $"{nameof(ConfigureAfterProcessIdSet)} should only be called once a process ID is set");
            // Initialize WaitStateHolder for non-child processes
            GetWaitState();
        }

        /// <devdoc>
        ///     Make sure we are watching for a process exit.
        /// </devdoc>
        /// <internalonly/>
        private void EnsureWatchingForExit()
        {
            if (!_watchingForExit)
            {
                lock (this)
                {
                    if (!_watchingForExit)
                    {
                        Debug.Assert(_waitHandle == null);
                        Debug.Assert(_registeredWaitHandle == null);
                        Debug.Assert(Associated, "Process.EnsureWatchingForExit called with no associated process");
                        _watchingForExit = true;
                        try
                        {
                            _waitHandle = new ProcessWaitHandle(GetWaitState());
                            _registeredWaitHandle = ThreadPool.RegisterWaitForSingleObject(_waitHandle,
                                new WaitOrTimerCallback(CompletionCallback), _waitHandle, -1, true);
                        }
                        catch
                        {
                            _waitHandle?.Dispose();
                            _waitHandle = null;
                            _watchingForExit = false;
                            throw;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Instructs the Process component to wait the specified number of milliseconds for the associated process to exit.
        /// </summary>
        private bool WaitForExitCore(int milliseconds)
        {
            bool exited = GetWaitState().WaitForExit(milliseconds);
            Debug.Assert(exited || milliseconds != Timeout.Infinite);

            if (exited && milliseconds == Timeout.Infinite) // if we have a hard timeout, we cannot wait for the streams
            {
                _output?.EOF.GetAwaiter().GetResult();
                _error?.EOF.GetAwaiter().GetResult();
            }

            return exited;
        }

        /// <summary>Gets the main module for the associated process.</summary>
        public ProcessModule? MainModule
        {
            get
            {
                ProcessModuleCollection pmc = Modules;
                return pmc.Count > 0 ? pmc[0] : null;
            }
        }

        /// <summary>Checks whether the process has exited and updates state accordingly.</summary>
        private void UpdateHasExited()
        {
            int? exitCode;
            _exited = GetWaitState().GetExited(out exitCode, refresh: true);
            if (_exited && exitCode != null)
            {
                _exitCode = exitCode.Value;
            }
        }

        /// <summary>Gets the time that the associated process exited.</summary>
        private DateTime ExitTimeCore
        {
            get { return GetWaitState().ExitTime; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the associated process priority
        /// should be temporarily boosted by the operating system when the main window
        /// has focus.
        /// </summary>
        private static bool PriorityBoostEnabledCore
        {
            get { return false; } //Nop
            set { } // Nop
        }

        /// <summary>
        /// Gets or sets the overall priority category for the associated process.
        /// </summary>
        private ProcessPriorityClass PriorityClassCore
        {
            // This mapping is relatively arbitrary.  0 is normal based on the man page,
            // and the other values above and below are simply distributed evenly.
            get
            {
                EnsureState(State.HaveNonExitedId);

                int errno = Interop.Sys.GetPriority(Interop.Sys.PriorityWhich.PRIO_PROCESS, _processId, out int pri);
                if (errno != 0) // Interop.Sys.GetPriority returns GetLastWin32Error()
                {
                    throw new Win32Exception(errno); // match Windows exception
                }

                Debug.Assert(pri >= -20 && pri <= 20);
                return
                    pri < -15 ? ProcessPriorityClass.RealTime :
                    pri < -10 ? ProcessPriorityClass.High :
                    pri < -5 ? ProcessPriorityClass.AboveNormal :
                    pri == 0 ? ProcessPriorityClass.Normal :
                    pri <= 10 ? ProcessPriorityClass.BelowNormal :
                    ProcessPriorityClass.Idle;
            }
            set
            {
                EnsureState(State.HaveNonExitedId);

                int pri = 0; // Normal
                switch (value)
                {
                    case ProcessPriorityClass.RealTime: pri = -19; break;
                    case ProcessPriorityClass.High: pri = -11; break;
                    case ProcessPriorityClass.AboveNormal: pri = -6; break;
                    case ProcessPriorityClass.BelowNormal: pri = 10; break;
                    case ProcessPriorityClass.Idle: pri = 19; break;
                    default:
                        Debug.Assert(value == ProcessPriorityClass.Normal, "Input should have been validated by caller");
                        break;
                }

                int result = Interop.Sys.SetPriority(Interop.Sys.PriorityWhich.PRIO_PROCESS, _processId, pri);
                if (result == -1)
                {
                    throw new Win32Exception(); // match Windows exception
                }
            }
        }

        /// <summary>Checks whether the argument is a direct child of this process.</summary>
        private bool IsParentOf(Process possibleChildProcess)
        {
            try
            {
                return Id == possibleChildProcess.ParentProcessId;
            }
            catch (Exception e) when (IsProcessInvalidException(e))
            {
                return false;
            }
        }

        private bool Equals(Process process)
        {
            try
            {
                return Id == process.Id;
            }
            catch (Exception e) when (IsProcessInvalidException(e))
            {
                return false;
            }
        }

        partial void ThrowIfExited(bool refresh)
        {
            // Don't allocate a ProcessWaitState.Holder unless we're refreshing.
            if (_waitStateHolder == null && !refresh)
            {
                return;
            }

            if (GetHasExited(refresh))
            {
                throw new InvalidOperationException(SR.Format(SR.ProcessHasExited, _processId.ToString()));
            }
        }

        /// <summary>
        /// Gets a short-term handle to the process, with the given access.  If a handle exists,
        /// then it is reused.  If the process has exited, it throws an exception.
        /// </summary>
        private SafeProcessHandle GetProcessHandle()
        {
            if (_haveProcessHandle)
            {
                ThrowIfExited(refresh: true);

                return _processHandle!;
            }

            EnsureState(State.HaveNonExitedId | State.IsLocal);
            return new SafeProcessHandle(_processId, GetSafeWaitHandle());
        }

        private bool StartCore(ProcessStartInfo startInfo, SafeFileHandle? stdinHandle, SafeFileHandle? stdoutHandle, SafeFileHandle? stderrHandle)
        {
            SafeProcessHandle startedProcess = SafeProcessHandle.StartCore(startInfo, stdinHandle, stdoutHandle, stderrHandle, out ProcessWaitState.Holder? waitStateHolder);
            Debug.Assert(!startedProcess.IsInvalid);

            _waitStateHolder = waitStateHolder;
            SetProcessHandle(startedProcess);
            SetProcessId(startedProcess.ProcessId);
            return true;
        }


        /// <summary>Finalizable holder for the underlying shared wait state object.</summary>
        private ProcessWaitState.Holder? _waitStateHolder;

        private static long s_ticksPerSecond;

        /// <summary>Convert a number of "jiffies", or ticks, to a TimeSpan.</summary>
        /// <param name="ticks">The number of ticks.</param>
        /// <returns>The equivalent TimeSpan.</returns>
        internal static TimeSpan TicksToTimeSpan(double ticks)
        {
            long ticksPerSecond = Volatile.Read(ref s_ticksPerSecond);
            if (ticksPerSecond == 0)
            {
                // Look up the number of ticks per second in the system's configuration,
                // then use that to convert to a TimeSpan
                ticksPerSecond = Interop.Sys.SysConf(Interop.Sys.SysConfName._SC_CLK_TCK);
                if (ticksPerSecond <= 0)
                {
                    throw new Win32Exception();
                }

                Volatile.Write(ref s_ticksPerSecond, ticksPerSecond);
            }

            return TimeSpan.FromSeconds(ticks / (double)ticksPerSecond);
        }

        private static AnonymousPipeClientStream OpenStream(SafeFileHandle handle, FileAccess access)
        {
            PipeDirection direction = access == FileAccess.Write ? PipeDirection.Out : PipeDirection.In;

            // Transfer the ownership to SafePipeHandle, so that it can be properly released when the AnonymousPipeClientStream is disposed.
            SafePipeHandle safePipeHandle = new(handle.DangerousGetHandle(), ownsHandle: true);
            handle.SetHandleAsInvalid();

            // Use AnonymousPipeClientStream for async, cancellable read/write support.
            return new AnonymousPipeClientStream(direction, safePipeHandle);
        }

        private static Encoding GetStandardInputEncoding() => Encoding.Default;

        private static Encoding GetStandardOutputEncoding() => Encoding.Default;

        /// <summary>Gets the wait state for this Process object.</summary>
        private ProcessWaitState GetWaitState()
        {
            if (_waitStateHolder == null)
            {
                EnsureState(State.HaveId);
                _waitStateHolder = new ProcessWaitState.Holder(_processId);
            }
            return _waitStateHolder._state;
        }

        private SafeWaitHandle GetSafeWaitHandle()
            => GetWaitState().EnsureExitedEvent().GetSafeWaitHandle();

        public IntPtr MainWindowHandle => IntPtr.Zero;

        private static bool CloseMainWindowCore() => false;

        public string MainWindowTitle => string.Empty;

        public bool Responding => true;

        private static bool WaitForInputIdleCore(int _ /*milliseconds*/) => throw new InvalidOperationException(SR.InputIdleUnknownError);

    }
}
