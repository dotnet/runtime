// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace System.Diagnostics
{
    /// <devdoc>
    ///    <para>
    ///       Provides access to local and remote
    ///       processes. Enables you to start and stop system processes.
    ///    </para>
    /// </devdoc>
    public class Process : IDisposable
    {
        //
        // FIELDS
        //

        private bool _haveProcessId;
        private int _processId;
        private bool _haveProcessHandle;
        private SafeProcessHandle _m_processHandle;
        private bool _isRemoteMachine;
        private string _machineName;
        private ProcessInfo _processInfo;
        private Int32 _m_processAccess;

        private ProcessThreadCollection _threads;
        private ProcessModuleCollection _modules;

        private bool _haveWorkingSetLimits;
        private IntPtr _minWorkingSet;
        private IntPtr _maxWorkingSet;

        private bool _haveProcessorAffinity;
        private IntPtr _processorAffinity;

        private bool _havePriorityClass;
        private ProcessPriorityClass _priorityClass;

        private ProcessStartInfo _startInfo;

        private bool _watchForExit;
        private bool _watchingForExit;
        private EventHandler _onExited;
        private bool _exited;
        private int _exitCode;
        private bool _signaled;

        private DateTime _exitTime;
        private bool _haveExitTime;

        private bool _priorityBoostEnabled;
        private bool _havePriorityBoostEnabled;

        private bool _raisedOnExited;
        private RegisteredWaitHandle _registeredWaitHandle;
        private WaitHandle _waitHandle;
        private StreamReader _standardOutput;
        private StreamWriter _standardInput;
        private StreamReader _standardError;
        private bool _disposed;

        private static object s_CreateProcessLock = new object();

        // This enum defines the operation mode for redirected process stream.
        // We don't support switching between synchronous mode and asynchronous mode.
        private enum StreamReadMode
        {
            undefined,
            syncMode,
            asyncMode
        }

        private StreamReadMode _outputStreamReadMode;
        private StreamReadMode _errorStreamReadMode;

        // Support for asynchrously reading streams
        public event DataReceivedEventHandler OutputDataReceived;
        public event DataReceivedEventHandler ErrorDataReceived;
        // Abstract the stream details
        internal AsyncStreamReader output;
        internal AsyncStreamReader error;
        internal bool pendingOutputRead;
        internal bool pendingErrorRead;
        private static SafeFileHandle s_InvalidPipeHandle = new SafeFileHandle(IntPtr.Zero, false);
#if FEATURE_TRACESWITCH
#if DEBUG
        internal static TraceSwitch processTracing = new TraceSwitch("processTracing", "Controls debug output from Process component");
#else
        internal static TraceSwitch processTracing = null;
#endif
#endif

        //
        // CONSTRUCTORS
        //

        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.Diagnostics.Process'/> class.
        ///    </para>
        /// </devdoc>
        public Process()
        {
            _machineName = ".";
            _outputStreamReadMode = StreamReadMode.undefined;
            _errorStreamReadMode = StreamReadMode.undefined;
            _m_processAccess = Interop.PROCESS_ALL_ACCESS;
        }

        Process(string machineName, bool isRemoteMachine, int processId, ProcessInfo processInfo)
            : base()
        {
            _processInfo = processInfo;
            _machineName = machineName;
            _isRemoteMachine = isRemoteMachine;
            _processId = processId;
            _haveProcessId = true;
            _outputStreamReadMode = StreamReadMode.undefined;
            _errorStreamReadMode = StreamReadMode.undefined;
            _m_processAccess = Interop.PROCESS_ALL_ACCESS;
        }

        //
        // PROPERTIES
        //

        /// <devdoc>
        ///     Returns whether this process component is associated with a real process.
        /// </devdoc>
        /// <internalonly/>
        bool Associated
        {
            get
            {
                return _haveProcessId || _haveProcessHandle;
            }
        }

        /// <devdoc>
        ///    <para>
        ///       Gets the base priority of
        ///       the associated process.
        ///    </para>
        /// </devdoc>
        public int BasePriority
        {
            get
            {
                EnsureState(State.HaveProcessInfo);
                return _processInfo.basePriority;
            }
        }

        /// <devdoc>
        ///    <para>
        ///       Gets
        ///       the
        ///       value that was specified by the associated process when it was terminated.
        ///    </para>
        /// </devdoc>
        public int ExitCode
        {
            get
            {
                EnsureState(State.Exited);
                return _exitCode;
            }
        }

        /// <devdoc>
        ///    <para>
        ///       Gets a
        ///       value indicating whether the associated process has been terminated.
        ///    </para>
        /// </devdoc>
        public bool HasExited
        {
            get
            {
                if (!_exited)
                {
                    EnsureState(State.Associated);
                    SafeProcessHandle handle = null;
                    try
                    {
                        handle = GetProcessHandle(Interop.PROCESS_QUERY_INFORMATION | Interop.SYNCHRONIZE, false);
                        if (handle.IsInvalid)
                        {
                            _exited = true;
                        }
                        else
                        {
                            int exitCode;

                            // Although this is the wrong way to check whether the process has exited,
                            // it was historically the way we checked for it, and a lot of code then took a dependency on
                            // the fact that this would always be set before the pipes were closed, so they would read
                            // the exit code out after calling ReadToEnd() or standard output or standard error. In order
                            // to allow 259 to function as a valid exit code and to break as few people as possible that
                            // took the ReadToEnd dependency, we check for an exit code before doing the more correct
                            // check to see if we have been signalled.
                            if (Interop.mincore.GetExitCodeProcess(handle, out exitCode) && exitCode != Interop.STILL_ACTIVE)
                            {
                                _exited = true;
                                _exitCode = exitCode;
                            }
                            else
                            {
                                // The best check for exit is that the kernel process object handle is invalid, 
                                // or that it is valid and signaled.  Checking if the exit code != STILL_ACTIVE 
                                // does not guarantee the process is closed,
                                // since some process could return an actual STILL_ACTIVE exit code (259).
                                if (!_signaled) // if we just came from WaitForExit, don't repeat
                                {
                                    ProcessWaitHandle wh = null;
                                    try
                                    {
                                        wh = new ProcessWaitHandle(handle);
                                        _signaled = wh.WaitOne(0);
                                    }
                                    finally
                                    {
                                        if (wh != null)
                                            wh.Dispose();
                                    }
                                }
                                if (_signaled)
                                {
                                    if (!Interop.mincore.GetExitCodeProcess(handle, out exitCode))
                                        throw new Win32Exception();

                                    _exited = true;
                                    _exitCode = exitCode;
                                }
                            }
                        }
                    }
                    finally
                    {
                        ReleaseProcessHandle(handle);
                    }

                    if (_exited)
                    {
                        RaiseOnExited();
                    }
                }
                return _exited;
            }
        }

        private ProcessThreadTimes GetProcessTimes()
        {
            ProcessThreadTimes processTimes = new ProcessThreadTimes();
            SafeProcessHandle handle = null;
            try
            {
                int access = Interop.PROCESS_QUERY_INFORMATION;

                access = Interop.PROCESS_QUERY_LIMITED_INFORMATION;

                handle = GetProcessHandle(access, false);
                if (handle.IsInvalid)
                {
                    // On OS older than XP, we will not be able to get the handle for a process
                    // after it terminates. 
                    // On Windows XP and newer OS, the information about a process will stay longer. 
                    throw new InvalidOperationException(SR.Format(SR.ProcessHasExited, _processId.ToString(CultureInfo.CurrentCulture)));
                }

                if (!Interop.mincore.GetProcessTimes(handle,
                                                   out processTimes.create,
                                                   out processTimes.exit,
                                                   out processTimes.kernel,
                                                   out processTimes.user))
                {
                    throw new Win32Exception();
                }
            }
            finally
            {
                ReleaseProcessHandle(handle);
            }
            return processTimes;
        }

        /// <devdoc>
        ///    <para>
        ///       Gets the time that the associated process exited.
        ///    </para>
        /// </devdoc>
        public DateTime ExitTime
        {
            get
            {
                if (!_haveExitTime)
                {
                    EnsureState(State.IsNt | State.Exited);
                    _exitTime = GetProcessTimes().ExitTime;
                    _haveExitTime = true;
                }
                return _exitTime;
            }
        }

        public SafeProcessHandle SafeHandle
        {
            get
            {
                EnsureState(State.Associated);
                return OpenProcessHandle(_m_processAccess);
            }
        }

        /// <devdoc>
        ///    <para>
        ///       Gets the number of handles that are associated
        ///       with the process.
        ///    </para>
        /// </devdoc>
        public int HandleCount
        {
            get
            {
                EnsureState(State.HaveProcessInfo);
                return _processInfo.handleCount;
            }
        }

        /// <devdoc>
        ///    <para>
        ///       Gets
        ///       the unique identifier for the associated process.
        ///    </para>
        /// </devdoc>
        public int Id
        {
            get
            {
                EnsureState(State.HaveId);
                return _processId;
            }
        }

        /// <devdoc>
        ///    <para>
        ///       Gets
        ///       the name of the computer on which the associated process is running.
        ///    </para>
        /// </devdoc>
        public string MachineName
        {
            get
            {
                EnsureState(State.Associated);
                return _machineName;
            }
        }

        /// <devdoc>
        ///    <para>
        ///       Gets
        ///       the main module for the associated process.
        ///    </para>
        /// </devdoc>
        public ProcessModule MainModule
        {
            get
            {
                // We only return null if we couldn't find a main module.
                // This could be because
                //      1. The process hasn't finished loading the main module (most likely)
                //      2. There are no modules loaded (possible for certain OS processes)
                //      3. Possibly other?
                EnsureState(State.HaveId | State.IsLocal);
                // on NT the first module is the main module                    
                ModuleInfo module = NtProcessManager.GetFirstModuleInfo(_processId);
                return new ProcessModule(module);
            }
        }


        /// <devdoc>
        ///    <para>
        ///       Gets or sets the maximum allowable working set for the associated
        ///       process.
        ///    </para>
        /// </devdoc>
        public IntPtr MaxWorkingSet
        {
            get
            {
                EnsureWorkingSetLimits();
                return _maxWorkingSet;
            }
            set
            {
                SetWorkingSetLimits(null, value);
            }
        }

        /// <devdoc>
        ///    <para>
        ///       Gets or sets the minimum allowable working set for the associated
        ///       process.
        ///    </para>
        /// </devdoc>
        public IntPtr MinWorkingSet
        {
            get
            {
                EnsureWorkingSetLimits();
                return _minWorkingSet;
            }
            set
            {
                SetWorkingSetLimits(value, null);
            }
        }

        public ProcessModuleCollection Modules
        {
            get
            {
                if (_modules == null)
                {
                    EnsureState(State.HaveId | State.IsLocal);
                    ModuleInfo[] moduleInfos = ProcessManager.GetModuleInfos(_processId);
                    ProcessModule[] newModulesArray = new ProcessModule[moduleInfos.Length];
                    for (int i = 0; i < moduleInfos.Length; i++)
                    {
                        newModulesArray[i] = new ProcessModule(moduleInfos[i]);
                    }
                    ProcessModuleCollection newModules = new ProcessModuleCollection(newModulesArray);
                    _modules = newModules;
                }
                return _modules;
            }
        }

        [System.Runtime.InteropServices.ComVisible(false)]
        public long NonpagedSystemMemorySize64
        {
            get
            {
                EnsureState(State.HaveNtProcessInfo);
                return _processInfo.poolNonpagedBytes;
            }
        }

        [System.Runtime.InteropServices.ComVisible(false)]
        public long PagedMemorySize64
        {
            get
            {
                EnsureState(State.HaveNtProcessInfo);
                return _processInfo.pageFileBytes;
            }
        }

        [System.Runtime.InteropServices.ComVisible(false)]
        public long PagedSystemMemorySize64
        {
            get
            {
                EnsureState(State.HaveNtProcessInfo);
                return _processInfo.poolPagedBytes;
            }
        }

        [System.Runtime.InteropServices.ComVisible(false)]
        public long PeakPagedMemorySize64
        {
            get
            {
                EnsureState(State.HaveNtProcessInfo);
                return _processInfo.pageFileBytesPeak;
            }
        }

        [System.Runtime.InteropServices.ComVisible(false)]
        public long PeakWorkingSet64
        {
            get
            {
                EnsureState(State.HaveNtProcessInfo);
                return _processInfo.workingSetPeak;
            }
        }

        [System.Runtime.InteropServices.ComVisible(false)]
        public long PeakVirtualMemorySize64
        {
            get
            {
                EnsureState(State.HaveNtProcessInfo);
                return _processInfo.virtualBytesPeak;
            }
        }

        /// <devdoc>
        ///    <para>
        ///       Gets or sets a value indicating whether the associated process priority
        ///       should be temporarily boosted by the operating system when the main window
        ///       has focus.
        ///    </para>
        /// </devdoc>
        public bool PriorityBoostEnabled
        {
            get
            {
                EnsureState(State.IsNt);
                if (!_havePriorityBoostEnabled)
                {
                    SafeProcessHandle handle = null;
                    try
                    {
                        handle = GetProcessHandle(Interop.PROCESS_QUERY_INFORMATION);
                        bool disabled = false;
                        if (!Interop.mincore.GetProcessPriorityBoost(handle, out disabled))
                        {
                            throw new Win32Exception();
                        }
                        _priorityBoostEnabled = !disabled;
                        _havePriorityBoostEnabled = true;
                    }
                    finally
                    {
                        ReleaseProcessHandle(handle);
                    }
                }
                return _priorityBoostEnabled;
            }
            set
            {
                EnsureState(State.IsNt);
                SafeProcessHandle handle = null;
                try
                {
                    handle = GetProcessHandle(Interop.PROCESS_SET_INFORMATION);
                    if (!Interop.mincore.SetProcessPriorityBoost(handle, !value))
                        throw new Win32Exception();
                    _priorityBoostEnabled = value;
                    _havePriorityBoostEnabled = true;
                }
                finally
                {
                    ReleaseProcessHandle(handle);
                }
            }
        }

        /// <devdoc>
        ///    <para>
        ///       Gets or sets the overall priority category for the
        ///       associated process.
        ///    </para>
        /// </devdoc>
        public ProcessPriorityClass PriorityClass
        {
            get
            {
                if (!_havePriorityClass)
                {
                    SafeProcessHandle handle = null;
                    try
                    {
                        handle = GetProcessHandle(Interop.PROCESS_QUERY_INFORMATION);
                        int value = Interop.mincore.GetPriorityClass(handle);
                        if (value == 0)
                        {
                            throw new Win32Exception();
                        }
                        _priorityClass = (ProcessPriorityClass)value;
                        _havePriorityClass = true;
                    }
                    finally
                    {
                        ReleaseProcessHandle(handle);
                    }
                }
                return _priorityClass;
            }
            set
            {
                if (!Enum.IsDefined(typeof(ProcessPriorityClass), value))
                {
                    throw new ArgumentException(SR.Format(SR.InvalidEnumArgument, "value", (int)value, typeof(ProcessPriorityClass)));
                }

                SafeProcessHandle handle = null;

                try
                {
                    handle = GetProcessHandle(Interop.PROCESS_SET_INFORMATION);
                    if (!Interop.mincore.SetPriorityClass(handle, (int)value))
                    {
                        throw new Win32Exception();
                    }
                    _priorityClass = value;
                    _havePriorityClass = true;
                }
                finally
                {
                    ReleaseProcessHandle(handle);
                }
            }
        }

        [System.Runtime.InteropServices.ComVisible(false)]
        public long PrivateMemorySize64
        {
            get
            {
                EnsureState(State.HaveNtProcessInfo);
                return _processInfo.privateBytes;
            }
        }

        /// <devdoc>
        ///     Returns the amount of time the process has spent running code inside the operating
        ///     system core.
        /// </devdoc>
        public TimeSpan PrivilegedProcessorTime
        {
            get
            {
                EnsureState(State.IsNt);
                return GetProcessTimes().PrivilegedProcessorTime;
            }
        }

        /// <devdoc>
        ///    <para>
        ///       Gets
        ///       the friendly name of the process.
        ///    </para>
        /// </devdoc>
        public string ProcessName
        {
            get
            {
                EnsureState(State.HaveProcessInfo);
                String processName = _processInfo.processName;

                return _processInfo.processName;
            }
        }

        /// <devdoc>
        ///    <para>
        ///       Gets
        ///       or sets which processors the threads in this process can be scheduled to run on.
        ///    </para>
        /// </devdoc>
        public IntPtr ProcessorAffinity
        {
            get
            {
                if (!_haveProcessorAffinity)
                {
                    SafeProcessHandle handle = null;
                    try
                    {
                        handle = GetProcessHandle(Interop.PROCESS_QUERY_INFORMATION);
                        IntPtr processAffinity;
                        IntPtr systemAffinity;
                        if (!Interop.mincore.GetProcessAffinityMask(handle, out processAffinity, out systemAffinity))
                            throw new Win32Exception();
                        _processorAffinity = processAffinity;
                    }
                    finally
                    {
                        ReleaseProcessHandle(handle);
                    }
                    _haveProcessorAffinity = true;
                }
                return _processorAffinity;
            }
            set
            {
                SafeProcessHandle handle = null;
                try
                {
                    handle = GetProcessHandle(Interop.PROCESS_SET_INFORMATION);
                    if (!Interop.mincore.SetProcessAffinityMask(handle, value))
                        throw new Win32Exception();

                    _processorAffinity = value;
                    _haveProcessorAffinity = true;
                }
                finally
                {
                    ReleaseProcessHandle(handle);
                }
            }
        }

        public int SessionId
        {
            get
            {
                EnsureState(State.HaveNtProcessInfo);
                return _processInfo.sessionId;
            }
        }

        /// <devdoc>
        ///    <para>
        ///       Gets or sets the properties to pass into the <see cref='System.Diagnostics.Process.Start'/> method for the <see cref='System.Diagnostics.Process'/>
        ///       .
        ///    </para>
        /// </devdoc>
        public ProcessStartInfo StartInfo
        {
            get
            {
                if (_startInfo == null)
                {
                    _startInfo = new ProcessStartInfo(this);
                }
                return _startInfo;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }
                _startInfo = value;
            }
        }

        /// <devdoc>
        ///     Returns the time the associated process was started.
        /// </devdoc>
        public DateTime StartTime
        {
            get
            {
                EnsureState(State.IsNt);
                return GetProcessTimes().StartTime;
            }
        }

        /// <devdoc>
        ///    <para>
        ///       Gets the set of threads that are running in the associated
        ///       process.
        ///    </para>
        /// </devdoc>
        public ProcessThreadCollection Threads
        {
            get
            {
                if (_threads == null)
                {
                    EnsureState(State.HaveProcessInfo);
                    int count = _processInfo.threadInfoList.Count;
                    ProcessThread[] newThreadsArray = new ProcessThread[count];
                    for (int i = 0; i < count; i++)
                    {
                        newThreadsArray[i] = new ProcessThread(_isRemoteMachine, (ThreadInfo)_processInfo.threadInfoList[i]);
                    }
                    ProcessThreadCollection newThreads = new ProcessThreadCollection(newThreadsArray);
                    _threads = newThreads;
                }
                return _threads;
            }
        }

        /// <devdoc>
        ///     Returns the amount of time the associated process has spent utilizing the CPU.
        ///     It is the sum of the <see cref='System.Diagnostics.Process.UserProcessorTime'/> and
        ///     <see cref='System.Diagnostics.Process.PrivilegedProcessorTime'/>.
        /// </devdoc>
        public TimeSpan TotalProcessorTime
        {
            get
            {
                EnsureState(State.IsNt);
                return GetProcessTimes().TotalProcessorTime;
            }
        }

        /// <devdoc>
        ///     Returns the amount of time the associated process has spent running code
        ///     inside the application portion of the process (not the operating system core).
        /// </devdoc>
        public TimeSpan UserProcessorTime
        {
            get
            {
                EnsureState(State.IsNt);
                return GetProcessTimes().UserProcessorTime;
            }
        }

        [System.Runtime.InteropServices.ComVisible(false)]
        public long VirtualMemorySize64
        {
            get
            {
                EnsureState(State.HaveNtProcessInfo);
                return _processInfo.virtualBytes;
            }
        }

        /// <devdoc>
        ///    <para>
        ///       Gets or sets whether the <see cref='System.Diagnostics.Process.Exited'/>
        ///       event is fired
        ///       when the process terminates.
        ///    </para>
        /// </devdoc>
        public bool EnableRaisingEvents
        {
            get
            {
                return _watchForExit;
            }
            set
            {
                if (value != _watchForExit)
                {
                    if (Associated)
                    {
                        if (value)
                        {
                            OpenProcessHandle();
                            EnsureWatchingForExit();
                        }
                        else
                        {
                            StopWatchingForExit();
                        }
                    }
                    _watchForExit = value;
                }
            }
        }


        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public StreamWriter StandardInput
        {
            get
            {
                if (_standardInput == null)
                {
                    throw new InvalidOperationException(SR.CantGetStandardIn);
                }

                return _standardInput;
            }
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public StreamReader StandardOutput
        {
            get
            {
                if (_standardOutput == null)
                {
                    throw new InvalidOperationException(SR.CantGetStandardOut);
                }

                if (_outputStreamReadMode == StreamReadMode.undefined)
                {
                    _outputStreamReadMode = StreamReadMode.syncMode;
                }
                else if (_outputStreamReadMode != StreamReadMode.syncMode)
                {
                    throw new InvalidOperationException(SR.CantMixSyncAsyncOperation);
                }

                return _standardOutput;
            }
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public StreamReader StandardError
        {
            get
            {
                if (_standardError == null)
                {
                    throw new InvalidOperationException(SR.CantGetStandardError);
                }

                if (_errorStreamReadMode == StreamReadMode.undefined)
                {
                    _errorStreamReadMode = StreamReadMode.syncMode;
                }
                else if (_errorStreamReadMode != StreamReadMode.syncMode)
                {
                    throw new InvalidOperationException(SR.CantMixSyncAsyncOperation);
                }

                return _standardError;
            }
        }

        [System.Runtime.InteropServices.ComVisible(false)]
        public long WorkingSet64
        {
            get
            {
                EnsureState(State.HaveNtProcessInfo);
                return _processInfo.workingSet;
            }
        }

        public event EventHandler Exited
        {
            add
            {
                _onExited += value;
            }
            remove
            {
                _onExited -= value;
            }
        }

        /// <devdoc>
        ///     Release the temporary handle we used to get process information.
        ///     If we used the process handle stored in the process object (we have all access to the handle,) don't release it.
        /// </devdoc>
        /// <internalonly/>
        void ReleaseProcessHandle(SafeProcessHandle handle)
        {
            if (handle == null)
            {
                return;
            }

            if (_haveProcessHandle && handle == _m_processHandle)
            {
                return;
            }
#if FEATURE_TRACESWITCH
            Debug.WriteLineIf(processTracing.TraceVerbose, "Process - CloseHandle(process)");
#endif
            handle.Dispose();
        }

        /// <devdoc>
        ///     This is called from the threadpool when a proces exits.
        /// </devdoc>
        /// <internalonly/>
        private void CompletionCallback(object context, bool wasSignaled)
        {
            StopWatchingForExit();
            RaiseOnExited();
        }

        /// <internalonly/>
        /// <devdoc>
        ///    <para>
        ///       Free any resources associated with this component.
        ///    </para>
        /// </devdoc>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    //Dispose managed and unmanaged resources
                    Close();
                }
                _disposed = true;
            }
        }

        /// <devdoc>
        ///    <para>
        ///       Frees any resources associated with this component.
        ///    </para>
        /// </devdoc>
        internal void Close()
        {
            if (Associated)
            {
                if (_haveProcessHandle)
                {
                    StopWatchingForExit();
#if FEATURE_TRACESWITCH
                    Debug.WriteLineIf(processTracing.TraceVerbose, "Process - CloseHandle(process) in Close()");
#endif
                    _m_processHandle.Dispose();
                    _m_processHandle = null;
                    _haveProcessHandle = false;
                }
                _haveProcessId = false;
                _isRemoteMachine = false;
                _machineName = ".";
                _raisedOnExited = false;

                //Don't call close on the Readers and writers
                //since they might be referenced by somebody else while the 
                //process is still alive but this method called.
                _standardOutput = null;
                _standardInput = null;
                _standardError = null;

                output = null;
                error = null;

                Refresh();
            }
        }

        /// <devdoc>
        ///     Helper method for checking preconditions when accessing properties.
        /// </devdoc>
        /// <internalonly/>
        void EnsureState(State state)
        {
            if ((state & State.Associated) != (State)0)
                if (!Associated)
                    throw new InvalidOperationException(SR.NoAssociatedProcess);

            if ((state & State.HaveId) != (State)0)
            {
                if (!_haveProcessId)
                {
                    if (_haveProcessHandle)
                    {
                        SetProcessId(ProcessManager.GetProcessIdFromHandle(_m_processHandle));
                    }
                    else
                    {
                        EnsureState(State.Associated);
                        throw new InvalidOperationException(SR.ProcessIdRequired);
                    }
                }
            }

            if ((state & State.IsLocal) != (State)0 && _isRemoteMachine)
            {
                throw new NotSupportedException(SR.NotSupportedRemote);
            }

            if ((state & State.HaveProcessInfo) != (State)0)
            {
                if (_processInfo == null)
                {
                    if ((state & State.HaveId) == (State)0) EnsureState(State.HaveId);
                    ProcessInfo[] processInfos = ProcessManager.GetProcessInfos(_machineName);
                    for (int i = 0; i < processInfos.Length; i++)
                    {
                        if (processInfos[i].processId == _processId)
                        {
                            _processInfo = processInfos[i];
                            break;
                        }
                    }
                    if (_processInfo == null)
                    {
                        throw new InvalidOperationException(SR.NoProcessInfo);
                    }
                }
            }

            if ((state & State.Exited) != (State)0)
            {
                if (!HasExited)
                {
                    throw new InvalidOperationException(SR.WaitTillExit);
                }

                if (!_haveProcessHandle)
                {
                    throw new InvalidOperationException(SR.NoProcessHandle);
                }
            }
        }

        /// <devdoc>
        ///     Make sure we are watching for a process exit.
        /// </devdoc>
        /// <internalonly/>
        void EnsureWatchingForExit()
        {
            if (!_watchingForExit)
            {
                lock (this)
                {
                    if (!_watchingForExit)
                    {
                        Debug.Assert(_haveProcessHandle, "Process.EnsureWatchingForExit called with no process handle");
                        Debug.Assert(Associated, "Process.EnsureWatchingForExit called with no associated process");
                        _watchingForExit = true;
                        try
                        {
                            _waitHandle = new ProcessWaitHandle(_m_processHandle);
                            _registeredWaitHandle = ThreadPool.RegisterWaitForSingleObject(_waitHandle,
                                new WaitOrTimerCallback(CompletionCallback), null, -1, true);
                        }
                        catch
                        {
                            _watchingForExit = false;
                            throw;
                        }
                    }
                }
            }
        }

        /// <devdoc>
        ///     Make sure we have obtained the min and max working set limits.
        /// </devdoc>
        /// <internalonly/>
        void EnsureWorkingSetLimits()
        {
            EnsureState(State.IsNt);
            if (!_haveWorkingSetLimits)
            {
                SafeProcessHandle handle = null;
                try
                {
                    handle = GetProcessHandle(Interop.PROCESS_QUERY_INFORMATION);
                    IntPtr min;
                    IntPtr max;

                    // GetProcessWorkingSetSize is not exposed in coresys we use 
                    // GetProcessWorkingSetSizeEx instead which also returns FLAGS which give some flexibility
                    // in terms of Min and Max values which we neglect.
                    int ignoredFlags;
                    if (!Interop.mincore.GetProcessWorkingSetSizeEx(handle, out min, out max, out ignoredFlags))
                    {
                        throw new Win32Exception();
                    }
                    _minWorkingSet = min;
                    _maxWorkingSet = max;
                    _haveWorkingSetLimits = true;
                }
                finally
                {
                    ReleaseProcessHandle(handle);
                }
            }
        }

        public static void EnterDebugMode()
        {
            SetPrivilege("SeDebugPrivilege", (int)Interop.SE_PRIVILEGE_ENABLED);
        }

        private static void SetPrivilege(string privilegeName, int attrib)
        {
            SafeTokenHandle hToken = null;
            Interop.LUID debugValue = new Interop.LUID();

            // this is only a "pseudo handle" to the current process - no need to close it later
            SafeProcessHandle processHandle = Interop.mincore.GetCurrentProcess();

            // get the process token so we can adjust the privilege on it.  We DO need to
            // close the token when we're done with it.
            if (!Interop.mincore.OpenProcessToken(processHandle, Interop.TOKEN_ADJUST_PRIVILEGES, out hToken))
            {
                throw new Win32Exception();
            }

            try
            {
                if (!Interop.mincore.LookupPrivilegeValue(null, privilegeName, out debugValue))
                {
                    throw new Win32Exception();
                }

                Interop.TokenPrivileges tkp = new Interop.TokenPrivileges();
                tkp.Luid = debugValue;
                tkp.Attributes = attrib;

                Interop.mincore.AdjustTokenPrivileges(hToken, false, tkp, 0, IntPtr.Zero, IntPtr.Zero);

                // AdjustTokenPrivileges can return true even if it failed to
                // set the privilege, so we need to use GetLastError
                if (Marshal.GetLastWin32Error() != Interop.ERROR_SUCCESS)
                {
                    throw new Win32Exception();
                }
            }
            finally
            {
#if FEATURE_TRACESWITCH
                Debug.WriteLineIf(processTracing.TraceVerbose, "Process - CloseHandle(processToken)");
#endif
                if (hToken != null)
                {
                    hToken.Dispose();
                }
            }
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static void LeaveDebugMode()
        {
            SetPrivilege("SeDebugPrivilege", 0);
        }

        /// <devdoc>
        ///    <para>
        ///       Returns a new <see cref='System.Diagnostics.Process'/> component given a process identifier and
        ///       the name of a computer in the network.
        ///    </para>
        /// </devdoc>
        public static Process GetProcessById(int processId, string machineName)
        {
            if (!ProcessManager.IsProcessRunning(processId, machineName))
            {
                throw new ArgumentException(SR.Format(SR.MissingProccess, processId.ToString(CultureInfo.CurrentCulture)));
            }

            return new Process(machineName, ProcessManager.IsRemoteMachine(machineName), processId, null);
        }

        /// <devdoc>
        ///    <para>
        ///       Returns a new <see cref='System.Diagnostics.Process'/> component given the
        ///       identifier of a process on the local computer.
        ///    </para>
        /// </devdoc>
        public static Process GetProcessById(int processId)
        {
            return GetProcessById(processId, ".");
        }

        /// <devdoc>
        ///    <para>
        ///       Creates an array of <see cref='System.Diagnostics.Process'/> components that are
        ///       associated
        ///       with process resources on the
        ///       local computer. These process resources share the specified process name.
        ///    </para>
        /// </devdoc>
        public static Process[] GetProcessesByName(string processName)
        {
            return GetProcessesByName(processName, ".");
        }

        /// <devdoc>
        ///    <para>
        ///       Creates an array of <see cref='System.Diagnostics.Process'/> components that are associated with process resources on a
        ///       remote computer. These process resources share the specified process name.
        ///    </para>
        /// </devdoc>
        public static Process[] GetProcessesByName(string processName, string machineName)
        {
            if (processName == null) processName = String.Empty;
            Process[] procs = GetProcesses(machineName);
            List<Process> list = new List<Process>();

            for (int i = 0; i < procs.Length; i++)
            {
                if (String.Equals(processName, procs[i].ProcessName, StringComparison.OrdinalIgnoreCase))
                {
                    list.Add(procs[i]);
                }
                else
                {
                    procs[i].Dispose();
                }
            }

            Process[] temp = new Process[list.Count];
            list.CopyTo(temp, 0);
            return temp;
        }

        /// <devdoc>
        ///    <para>
        ///       Creates a new <see cref='System.Diagnostics.Process'/>
        ///       component for each process resource on the local computer.
        ///    </para>
        /// </devdoc>
        public static Process[] GetProcesses()
        {
            return GetProcesses(".");
        }

        /// <devdoc>
        ///    <para>
        ///       Creates a new <see cref='System.Diagnostics.Process'/>
        ///       component for each
        ///       process resource on the specified computer.
        ///    </para>
        /// </devdoc>
        public static Process[] GetProcesses(string machineName)
        {
            bool isRemoteMachine = ProcessManager.IsRemoteMachine(machineName);
            ProcessInfo[] processInfos = ProcessManager.GetProcessInfos(machineName);
            Process[] processes = new Process[processInfos.Length];
            for (int i = 0; i < processInfos.Length; i++)
            {
                ProcessInfo processInfo = processInfos[i];
                processes[i] = new Process(machineName, isRemoteMachine, processInfo.processId, processInfo);
            }
#if FEATURE_TRACESWITCH
            Debug.WriteLineIf(processTracing.TraceVerbose, "Process.GetProcesses(" + machineName + ")");
#if DEBUG
            if (processTracing.TraceVerbose) {
                Debug.Indent();
                for (int i = 0; i < processInfos.Length; i++) {
                    Debug.WriteLine(processes[i].Id + ": " + processes[i].ProcessName);
                }
                Debug.Unindent();
            }
#endif
#endif
            return processes;
        }

        /// <devdoc>
        ///    <para>
        ///       Returns a new <see cref='System.Diagnostics.Process'/>
        ///       component and associates it with the current active process.
        ///    </para>
        /// </devdoc>
        public static Process GetCurrentProcess()
        {
            return new Process(".", false, unchecked((int)Interop.mincore.GetCurrentProcessId()), null);
        }

        /// <devdoc>
        ///    <para>
        ///       Raises the <see cref='System.Diagnostics.Process.Exited'/> event.
        ///    </para>
        /// </devdoc>
        protected void OnExited()
        {
            EventHandler exited = _onExited;
            if (exited != null)
            {
                exited(this, EventArgs.Empty);
            }
        }

        /// <devdoc>
        ///     Gets a short-term handle to the process, with the given access.  
        ///     If a handle is stored in current process object, then use it.
        ///     Note that the handle we stored in current process object will have all access we need.
        /// </devdoc>
        /// <internalonly/>
        SafeProcessHandle GetProcessHandle(int access, bool throwIfExited)
        {
#if FEATURE_TRACESWITCH
            Debug.WriteLineIf(processTracing.TraceVerbose, "GetProcessHandle(access = 0x" + access.ToString("X8", CultureInfo.InvariantCulture) + ", throwIfExited = " + throwIfExited + ")");
#if DEBUG
            if (processTracing.TraceVerbose) {
                StackFrame calledFrom = new StackTrace(true).GetFrame(0);
                Debug.WriteLine("   called from " + calledFrom.GetFileName() + ", line " + calledFrom.GetFileLineNumber());
            }
#endif
#endif
            if (_haveProcessHandle)
            {
                if (throwIfExited)
                {
                    // Since haveProcessHandle is true, we know we have the process handle
                    // open with at least SYNCHRONIZE access, so we can wait on it with 
                    // zero timeout to see if the process has exited.
                    ProcessWaitHandle waitHandle = null;
                    try
                    {
                        waitHandle = new ProcessWaitHandle(_m_processHandle);
                        if (waitHandle.WaitOne(0))
                        {
                            if (_haveProcessId)
                                throw new InvalidOperationException(SR.Format(SR.ProcessHasExited, _processId.ToString(CultureInfo.CurrentCulture)));
                            else
                                throw new InvalidOperationException(SR.ProcessHasExitedNoId);
                        }
                    }
                    finally
                    {
                        if (waitHandle != null)
                        {
                            waitHandle.Dispose();
                        }
                    }
                }
                return _m_processHandle;
            }
            else
            {
                EnsureState(State.HaveId | State.IsLocal);
                SafeProcessHandle handle = SafeProcessHandle.InvalidHandle;
                handle = ProcessManager.OpenProcess(_processId, access, throwIfExited);
                if (throwIfExited && (access & Interop.PROCESS_QUERY_INFORMATION) != 0)
                {
                    if (Interop.mincore.GetExitCodeProcess(handle, out _exitCode) && _exitCode != Interop.STILL_ACTIVE)
                    {
                        throw new InvalidOperationException(SR.Format(SR.ProcessHasExited, _processId.ToString(CultureInfo.CurrentCulture)));
                    }
                }
                return handle;
            }
        }

        /// <devdoc>
        ///     Gets a short-term handle to the process, with the given access.  If a handle exists,
        ///     then it is reused.  If the process has exited, it throws an exception.
        /// </devdoc>
        /// <internalonly/>
        SafeProcessHandle GetProcessHandle(int access)
        {
            return GetProcessHandle(access, true);
        }

        /// <devdoc>
        ///     Opens a long-term handle to the process, with all access.  If a handle exists,
        ///     then it is reused.  If the process has exited, it throws an exception.
        /// </devdoc>
        /// <internalonly/>
        SafeProcessHandle OpenProcessHandle()
        {
            return OpenProcessHandle(Interop.PROCESS_ALL_ACCESS);
        }

        SafeProcessHandle OpenProcessHandle(Int32 access)
        {
            if (!_haveProcessHandle)
            {
                //Cannot open a new process handle if the object has been disposed, since finalization has been suppressed.            
                if (_disposed)
                {
                    throw new ObjectDisposedException(GetType().Name);
                }

                SetProcessHandle(GetProcessHandle(access));
            }
            return _m_processHandle;
        }

        /// <devdoc>
        ///     Raise the Exited event, but make sure we don't do it more than once.
        /// </devdoc>
        /// <internalonly/>
        void RaiseOnExited()
        {
            if (!_raisedOnExited)
            {
                lock (this)
                {
                    if (!_raisedOnExited)
                    {
                        _raisedOnExited = true;
                        OnExited();
                    }
                }
            }
        }

        /// <devdoc>
        ///    <para>
        ///       Discards any information about the associated process
        ///       that has been cached inside the process component. After <see cref='System.Diagnostics.Process.Refresh'/> is called, the
        ///       first request for information for each property causes the process component
        ///       to obtain a new value from the associated process.
        ///    </para>
        /// </devdoc>
        public void Refresh()
        {
            _processInfo = null;
            _threads = null;
            _modules = null;
            _exited = false;
            _signaled = false;
            _haveWorkingSetLimits = false;
            _haveProcessorAffinity = false;
            _havePriorityClass = false;
            _haveExitTime = false;
            _havePriorityBoostEnabled = false;
        }

        /// <devdoc>
        ///     Helper to associate a process handle with this component.
        /// </devdoc>
        /// <internalonly/>
        void SetProcessHandle(SafeProcessHandle processHandle)
        {
            _m_processHandle = processHandle;
            _haveProcessHandle = true;
            if (_watchForExit)
            {
                EnsureWatchingForExit();
            }
        }

        /// <devdoc>
        ///     Helper to associate a process id with this component.
        /// </devdoc>
        /// <internalonly/>
        void SetProcessId(int processId)
        {
            _processId = processId;
            _haveProcessId = true;
        }

        /// <devdoc>
        ///     Helper to set minimum or maximum working set limits.
        /// </devdoc>
        /// <internalonly/>
        void SetWorkingSetLimits(object newMin, object newMax)
        {
            EnsureState(State.IsNt);

            SafeProcessHandle handle = null;
            try
            {
                handle = GetProcessHandle(Interop.PROCESS_QUERY_INFORMATION | Interop.PROCESS_SET_QUOTA);
                IntPtr min;
                IntPtr max;
                int ignoredFlags;
                if (!Interop.mincore.GetProcessWorkingSetSizeEx(handle, out min, out max, out ignoredFlags))
                {
                    throw new Win32Exception();
                }

                if (newMin != null)
                {
                    min = (IntPtr)newMin;
                }

                if (newMax != null)
                {
                    max = (IntPtr)newMax;
                }

                if ((long)min > (long)max)
                {
                    if (newMin != null)
                    {
                        throw new ArgumentException(SR.BadMinWorkset);
                    }
                    else
                    {
                        throw new ArgumentException(SR.BadMaxWorkset);
                    }
                }

                // We use SetProcessWorkingSetSizeEx which gives an option to follow
                // the max and min value even in low-memory and abundant-memory situtions.
                // However, we do not use these flags to emulate the existing behavior
                if (!Interop.mincore.SetProcessWorkingSetSizeEx(handle, min, max, 0))
                {
                    throw new Win32Exception();
                }

                // The value may be rounded/changed by the OS, so go get it
                if (!Interop.mincore.GetProcessWorkingSetSizeEx(handle, out min, out max, out ignoredFlags))
                {
                    throw new Win32Exception();
                }
                _minWorkingSet = min;
                _maxWorkingSet = max;
                _haveWorkingSetLimits = true;
            }
            finally
            {
                ReleaseProcessHandle(handle);
            }
        }

        /// <devdoc>
        ///    <para>
        ///       Starts a process specified by the <see cref='System.Diagnostics.Process.StartInfo'/> property of this <see cref='System.Diagnostics.Process'/>
        ///       component and associates it with the
        ///    <see cref='System.Diagnostics.Process'/> . If a process resource is reused 
        ///       rather than started, the reused process is associated with this <see cref='System.Diagnostics.Process'/>
        ///       component.
        ///    </para>
        /// </devdoc>
        public bool Start()
        {
            Close();
            ProcessStartInfo startInfo = StartInfo;
            if (startInfo.FileName.Length == 0)
                throw new InvalidOperationException(SR.FileNameMissing);

            return StartWithCreateProcess(startInfo);
        }

        private static void CreatePipeWithSecurityAttributes(out SafeFileHandle hReadPipe, out SafeFileHandle hWritePipe, Interop.SECURITY_ATTRIBUTES lpPipeAttributes, int nSize)
        {
            bool ret = Interop.mincore.CreatePipe(out hReadPipe, out hWritePipe, lpPipeAttributes, nSize);
            if (!ret || hReadPipe.IsInvalid || hWritePipe.IsInvalid)
            {
                throw new Win32Exception();
            }
        }

        // Using synchronous Anonymous pipes for process input/output redirection means we would end up 
        // wasting a worker threadpool thread per pipe instance. Overlapped pipe IO is desirable, since 
        // it will take advantage of the NT IO completion port infrastructure. But we can't really use 
        // Overlapped I/O for process input/output as it would break Console apps (managed Console class 
        // methods such as WriteLine as well as native CRT functions like printf) which are making an
        // assumption that the console standard handles (obtained via GetStdHandle()) are opened
        // for synchronous I/O and hence they can work fine with ReadFile/WriteFile synchrnously!
        private void CreatePipe(out SafeFileHandle parentHandle, out SafeFileHandle childHandle, bool parentInputs)
        {
            Interop.SECURITY_ATTRIBUTES securityAttributesParent = new Interop.SECURITY_ATTRIBUTES();
            securityAttributesParent.bInheritHandle = true;

            SafeFileHandle hTmp = null;
            try
            {
                if (parentInputs)
                {
                    CreatePipeWithSecurityAttributes(out childHandle, out hTmp, securityAttributesParent, 0);
                }
                else
                {
                    CreatePipeWithSecurityAttributes(out hTmp,
                                                          out childHandle,
                                                          securityAttributesParent,
                                                          0);
                }
                // Duplicate the parent handle to be non-inheritable so that the child process 
                // doesn't have access. This is done for correctness sake, exact reason is unclear.
                // One potential theory is that child process can do something brain dead like 
                // closing the parent end of the pipe and there by getting into a blocking situation
                // as parent will not be draining the pipe at the other end anymore. 
                SafeProcessHandle currentProcHandle = Interop.mincore.GetCurrentProcess();
                if (!Interop.mincore.DuplicateHandle(currentProcHandle,
                                                     hTmp,
                                                     currentProcHandle,
                                                     out parentHandle,
                                                     0,
                                                     false,
                                                     Interop.DUPLICATE_SAME_ACCESS))
                {
                    throw new Win32Exception();
                }
            }
            finally
            {
                if (hTmp != null && !hTmp.IsInvalid)
                {
                    hTmp.Dispose();
                }
            }
        }

        private static StringBuilder BuildCommandLine(string executableFileName, string arguments)
        {
            // Construct a StringBuilder with the appropriate command line
            // to pass to CreateProcess.  If the filename isn't already 
            // in quotes, we quote it here.  This prevents some security
            // problems (it specifies exactly which part of the string
            // is the file to execute).
            StringBuilder commandLine = new StringBuilder();
            string fileName = executableFileName.Trim();
            bool fileNameIsQuoted = (fileName.StartsWith("\"", StringComparison.Ordinal) && fileName.EndsWith("\"", StringComparison.Ordinal));
            if (!fileNameIsQuoted)
            {
                commandLine.Append("\"");
            }

            commandLine.Append(fileName);

            if (!fileNameIsQuoted)
            {
                commandLine.Append("\"");
            }

            if (!String.IsNullOrEmpty(arguments))
            {
                commandLine.Append(" ");
                commandLine.Append(arguments);
            }

            return commandLine;
        }

        private bool StartWithCreateProcess(ProcessStartInfo startInfo)
        {
            if (startInfo.StandardOutputEncoding != null && !startInfo.RedirectStandardOutput)
            {
                throw new InvalidOperationException(SR.StandardOutputEncodingNotAllowed);
            }

            if (startInfo.StandardErrorEncoding != null && !startInfo.RedirectStandardError)
            {
                throw new InvalidOperationException(SR.StandardErrorEncodingNotAllowed);
            }

            // See knowledge base article Q190351 for an explanation of the following code.  Noteworthy tricky points:
            //    * The handles are duplicated as non-inheritable before they are passed to CreateProcess so
            //      that the child process can not close them
            //    * CreateProcess allows you to redirect all or none of the standard IO handles, so we use
            //      GetStdHandle for the handles that are not being redirected

            //Cannot start a new process and store its handle if the object has been disposed, since finalization has been suppressed.            
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }

            StringBuilder commandLine = BuildCommandLine(startInfo.FileName, startInfo.Arguments);

            Interop.STARTUPINFO startupInfo = new Interop.STARTUPINFO();
            Interop.PROCESS_INFORMATION processInfo = new Interop.PROCESS_INFORMATION();
            Interop.SECURITY_ATTRIBUTES unused_SecAttrs = new Interop.SECURITY_ATTRIBUTES();
            SafeProcessHandle procSH = new SafeProcessHandle();
            SafeThreadHandle threadSH = new SafeThreadHandle();
            bool retVal;
            int errorCode = 0;
            // handles used in parent process
            SafeFileHandle standardInputWritePipeHandle = null;
            SafeFileHandle standardOutputReadPipeHandle = null;
            SafeFileHandle standardErrorReadPipeHandle = null;
            GCHandle environmentHandle = new GCHandle();
            lock (s_CreateProcessLock)
            {
                try
                {
                    // set up the streams
                    if (startInfo.RedirectStandardInput || startInfo.RedirectStandardOutput || startInfo.RedirectStandardError)
                    {
                        if (startInfo.RedirectStandardInput)
                        {
                            CreatePipe(out standardInputWritePipeHandle, out startupInfo.hStdInput, true);
                        }
                        else
                        {
                            startupInfo.hStdInput = new SafeFileHandle(Interop.mincore.GetStdHandle(Interop.STD_INPUT_HANDLE), false);
                        }

                        if (startInfo.RedirectStandardOutput)
                        {
                            CreatePipe(out standardOutputReadPipeHandle, out startupInfo.hStdOutput, false);
                        }
                        else
                        {
                            startupInfo.hStdOutput = new SafeFileHandle(Interop.mincore.GetStdHandle(Interop.STD_OUTPUT_HANDLE), false);
                        }

                        if (startInfo.RedirectStandardError)
                        {
                            CreatePipe(out standardErrorReadPipeHandle, out startupInfo.hStdError, false);
                        }
                        else
                        {
                            startupInfo.hStdError = new SafeFileHandle(Interop.mincore.GetStdHandle(Interop.STD_ERROR_HANDLE), false);
                        }

                        startupInfo.dwFlags = Interop.STARTF_USESTDHANDLES;
                    }

                    // set up the creation flags paramater
                    int creationFlags = 0;
                    if (startInfo.CreateNoWindow) creationFlags |= Interop.CREATE_NO_WINDOW;

                    // set up the environment block parameter
                    IntPtr environmentPtr = (IntPtr)0;
                    if (startInfo.environmentVariables != null)
                    {
                        creationFlags |= Interop.CREATE_UNICODE_ENVIRONMENT;
                        byte[] environmentBytes = EnvironmentBlock.ToByteArray(startInfo.environmentVariables);
                        environmentHandle = GCHandle.Alloc(environmentBytes, GCHandleType.Pinned);
                        environmentPtr = environmentHandle.AddrOfPinnedObject();
                    }
                    string workingDirectory = startInfo.WorkingDirectory;
                    if (workingDirectory == string.Empty)
                        workingDirectory = Directory.GetCurrentDirectory();

                    try { }
                    finally
                    {
                        retVal = Interop.mincore.CreateProcess(
                                null,                // we don't need this since all the info is in commandLine
                                commandLine,         // pointer to the command line string
                                unused_SecAttrs, // address to process security attributes, we don't need to inheriat the handle
                                unused_SecAttrs, // address to thread security attributes.
                                true,                // handle inheritance flag
                                creationFlags,       // creation flags
                                environmentPtr,      // pointer to new environment block
                                workingDirectory,    // pointer to current directory name
                                startupInfo,         // pointer to STARTUPINFO
                                processInfo      // pointer to PROCESS_INFORMATION
                            );
                        if (!retVal)
                            errorCode = Marshal.GetLastWin32Error();
                        if (processInfo.hProcess != (IntPtr)0 && processInfo.hProcess != (IntPtr)Interop.INVALID_HANDLE_VALUE)
                            procSH.InitialSetHandle(processInfo.hProcess);
                        if (processInfo.hThread != (IntPtr)0 && processInfo.hThread != (IntPtr)Interop.INVALID_HANDLE_VALUE)
                            threadSH.InitialSetHandle(processInfo.hThread);
                    }
                    if (!retVal)
                    {
                        if (errorCode == Interop.ERROR_BAD_EXE_FORMAT || errorCode == Interop.ERROR_EXE_MACHINE_TYPE_MISMATCH)
                        {
                            throw new Win32Exception(errorCode, SR.InvalidApplication);
                        }
                        throw new Win32Exception(errorCode);
                    }
                }
                finally
                {
                    // free environment block
                    if (environmentHandle.IsAllocated)
                    {
                        environmentHandle.Free();
                    }

                    startupInfo.Dispose();
                }
            }

            if (startInfo.RedirectStandardInput)
            {
                Encoding enc = GetEncoding((int)Interop.mincore.GetConsoleCP());
                _standardInput = new StreamWriter(new FileStream(standardInputWritePipeHandle, FileAccess.Write, 4096, false), enc, 4096);
                _standardInput.AutoFlush = true;
            }
            if (startInfo.RedirectStandardOutput)
            {
                Encoding enc = (startInfo.StandardOutputEncoding != null) ? startInfo.StandardOutputEncoding : GetEncoding((int)Interop.mincore.GetConsoleOutputCP());
                _standardOutput = new StreamReader(new FileStream(standardOutputReadPipeHandle, FileAccess.Read, 4096, false), enc, true, 4096);
            }
            if (startInfo.RedirectStandardError)
            {
                Encoding enc = (startInfo.StandardErrorEncoding != null) ? startInfo.StandardErrorEncoding : GetEncoding((int)Interop.mincore.GetConsoleOutputCP());
                _standardError = new StreamReader(new FileStream(standardErrorReadPipeHandle, FileAccess.Read, 4096, false), enc, true, 4096);
            }

            bool ret = false;
            if (!procSH.IsInvalid)
            {
                SetProcessHandle(procSH);
                SetProcessId((int)processInfo.dwProcessId);
                threadSH.Dispose();
                ret = true;
            }

            return ret;
        }

        // In most scenario 437 is the codepage used for Console encoding. However this encoding is not available by default and so we use the try{} catch{} pattern and use UTF8 in case of failure.
        // This ensures that if the user uses Encoding.RegisterProvider to register the encoding the Process class can automatically get the codepage as well.
        private static Encoding GetEncoding(int codePage)
        {
            Encoding enc = null;
            try
            {
                enc = Encoding.GetEncoding(codePage);
            }
            catch (NotSupportedException)
            {
                // There is no data available for the above codePage so we will use UTF8 instead with emitPrefix set to false.
                enc = new UTF8Encoding(false);
            }
            return enc;
        }

        /// <devdoc>
        ///    <para>
        ///       Starts a process resource by specifying the name of a
        ///       document or application file. Associates the process resource with a new <see cref='System.Diagnostics.Process'/>
        ///       component.
        ///    </para>
        /// </devdoc>
        public static Process Start(string fileName)
        {
            return Start(new ProcessStartInfo(fileName));
        }

        /// <devdoc>
        ///    <para>
        ///       Starts a process resource by specifying the name of an
        ///       application and a set of command line arguments. Associates the process resource
        ///       with a new <see cref='System.Diagnostics.Process'/>
        ///       component.
        ///    </para>
        /// </devdoc>
        public static Process Start(string fileName, string arguments)
        {
            return Start(new ProcessStartInfo(fileName, arguments));
        }

        /// <devdoc>
        ///    <para>
        ///       Starts a process resource specified by the process start
        ///       information passed in, for example the file name of the process to start.
        ///       Associates the process resource with a new <see cref='System.Diagnostics.Process'/>
        ///       component.
        ///    </para>
        /// </devdoc>
        public static Process Start(ProcessStartInfo startInfo)
        {
            Process process = new Process();
            if (startInfo == null) throw new ArgumentNullException("startInfo");
            process.StartInfo = startInfo;
            if (process.Start())
            {
                return process;
            }
            return null;
        }

        /// <devdoc>
        ///    <para>
        ///       Stops the
        ///       associated process immediately.
        ///    </para>
        /// </devdoc>
        public void Kill()
        {
            SafeProcessHandle handle = null;
            try
            {
                handle = GetProcessHandle(Interop.PROCESS_TERMINATE);
                if (!Interop.mincore.TerminateProcess(handle, -1))
                    throw new Win32Exception();
            }
            finally
            {
                ReleaseProcessHandle(handle);
            }
        }

        /// <devdoc>
        ///     Make sure we are not watching for process exit.
        /// </devdoc>
        /// <internalonly/>
        void StopWatchingForExit()
        {
            if (_watchingForExit)
            {
                lock (this)
                {
                    if (_watchingForExit)
                    {
                        _watchingForExit = false;
                        _registeredWaitHandle.Unregister(null);
                        _waitHandle.Dispose();
                        _waitHandle = null;
                        _registeredWaitHandle = null;
                    }
                }
            }
        }

        public override string ToString()
        {
            if (Associated)
            {
                string processName = ProcessName;
                if (processName.Length != 0)
                {
                    return String.Format(CultureInfo.CurrentCulture, "{0} ({1})", base.ToString(), processName);
                }
            }
            return base.ToString();
        }

        /// <devdoc>
        ///    <para>
        ///       Instructs the <see cref='System.Diagnostics.Process'/> component to wait the specified number of milliseconds for the associated process to exit.
        ///    </para>
        /// </devdoc>
        public bool WaitForExit(int milliseconds)
        {
            SafeProcessHandle handle = null;
            bool exited;
            ProcessWaitHandle processWaitHandle = null;
            try
            {
                handle = GetProcessHandle(Interop.SYNCHRONIZE, false);
                if (handle.IsInvalid)
                {
                    exited = true;
                }
                else
                {
                    processWaitHandle = new ProcessWaitHandle(handle);
                    if (processWaitHandle.WaitOne(milliseconds))
                    {
                        exited = true;
                        _signaled = true;
                    }
                    else
                    {
                        exited = false;
                        _signaled = false;
                    }
                }
            }
            finally
            {
                if (processWaitHandle != null)
                {
                    processWaitHandle.Dispose();
                }

                // If we have a hard timeout, we cannot wait for the streams
                if (output != null && milliseconds == -1)
                {
                    output.WaitUtilEOF();
                }

                if (error != null && milliseconds == -1)
                {
                    error.WaitUtilEOF();
                }

                ReleaseProcessHandle(handle);
            }

            if (exited && _watchForExit)
            {
                RaiseOnExited();
            }

            return exited;
        }

        /// <devdoc>
        ///    <para>
        ///       Instructs the <see cref='System.Diagnostics.Process'/> component to wait
        ///       indefinitely for the associated process to exit.
        ///    </para>
        /// </devdoc>
        public void WaitForExit()
        {
            WaitForExit(-1);
        }

        // Support for working asynchronously with streams
        /// <devdoc>
        /// <para>
        /// Instructs the <see cref='System.Diagnostics.Process'/> component to start
        /// reading the StandardOutput stream asynchronously. The user can register a callback
        /// that will be called when a line of data terminated by \n,\r or \r\n is reached, or the end of stream is reached
        /// then the remaining information is returned. The user can add an event handler to OutputDataReceived.
        /// </para>
        /// </devdoc>
        [System.Runtime.InteropServices.ComVisible(false)]
        public void BeginOutputReadLine()
        {
            if (_outputStreamReadMode == StreamReadMode.undefined)
            {
                _outputStreamReadMode = StreamReadMode.asyncMode;
            }
            else if (_outputStreamReadMode != StreamReadMode.asyncMode)
            {
                throw new InvalidOperationException(SR.CantMixSyncAsyncOperation);
            }

            if (pendingOutputRead)
                throw new InvalidOperationException(SR.PendingAsyncOperation);

            pendingOutputRead = true;
            // We can't detect if there's a pending sychronous read, stream also doesn't.
            if (output == null)
            {
                if (_standardOutput == null)
                {
                    throw new InvalidOperationException(SR.CantGetStandardOut);
                }

                Stream s = _standardOutput.BaseStream;
                output = new AsyncStreamReader(this, s, new UserCallBack(OutputReadNotifyUser), _standardOutput.CurrentEncoding);
            }
            output.BeginReadLine();
        }


        /// <devdoc>
        /// <para>
        /// Instructs the <see cref='System.Diagnostics.Process'/> component to start
        /// reading the StandardError stream asynchronously. The user can register a callback
        /// that will be called when a line of data terminated by \n,\r or \r\n is reached, or the end of stream is reached
        /// then the remaining information is returned. The user can add an event handler to ErrorDataReceived.
        /// </para>
        /// </devdoc>
        [System.Runtime.InteropServices.ComVisible(false)]
        public void BeginErrorReadLine()
        {
            if (_errorStreamReadMode == StreamReadMode.undefined)
            {
                _errorStreamReadMode = StreamReadMode.asyncMode;
            }
            else if (_errorStreamReadMode != StreamReadMode.asyncMode)
            {
                throw new InvalidOperationException(SR.CantMixSyncAsyncOperation);
            }

            if (pendingErrorRead)
            {
                throw new InvalidOperationException(SR.PendingAsyncOperation);
            }

            pendingErrorRead = true;
            // We can't detect if there's a pending sychronous read, stream also doesn't.
            if (error == null)
            {
                if (_standardError == null)
                {
                    throw new InvalidOperationException(SR.CantGetStandardError);
                }

                Stream s = _standardError.BaseStream;
                error = new AsyncStreamReader(this, s, new UserCallBack(ErrorReadNotifyUser), _standardError.CurrentEncoding);
            }
            error.BeginReadLine();
        }

        /// <devdoc>
        /// <para>
        /// Instructs the <see cref='System.Diagnostics.Process'/> component to cancel the asynchronous operation
        /// specified by BeginOutputReadLine().
        /// </para>
        /// </devdoc>
        [System.Runtime.InteropServices.ComVisible(false)]
        public void CancelOutputRead()
        {
            if (output != null)
            {
                output.CancelOperation();
            }
            else
            {
                throw new InvalidOperationException(SR.NoAsyncOperation);
            }

            pendingOutputRead = false;
        }

        /// <devdoc>
        /// <para>
        /// Instructs the <see cref='System.Diagnostics.Process'/> component to cancel the asynchronous operation
        /// specified by BeginErrorReadLine().
        /// </para>
        /// </devdoc>
        [System.Runtime.InteropServices.ComVisible(false)]
        public void CancelErrorRead()
        {
            if (error != null)
            {
                error.CancelOperation();
            }
            else
            {
                throw new InvalidOperationException(SR.NoAsyncOperation);
            }

            pendingErrorRead = false;
        }

        internal void OutputReadNotifyUser(String data)
        {
            // To avoid race between remove handler and raising the event
            DataReceivedEventHandler outputDataReceived = OutputDataReceived;
            if (outputDataReceived != null)
            {
                DataReceivedEventArgs e = new DataReceivedEventArgs(data);
                outputDataReceived(this, e);  // Call back to user informing data is available
            }
        }

        internal void ErrorReadNotifyUser(String data)
        {
            // To avoid race between remove handler and raising the event
            DataReceivedEventHandler errorDataReceived = ErrorDataReceived;
            if (errorDataReceived != null)
            {
                DataReceivedEventArgs e = new DataReceivedEventArgs(data);
                errorDataReceived(this, e); // Call back to user informing data is available.
            }
        }

        /// <summary>
        ///     A desired internal state.
        /// </summary>
        /// <internalonly/>
        enum State
        {
            HaveId = 0x1,
            IsLocal = 0x2,
            IsNt = 0x4,
            HaveProcessInfo = 0x8,
            Exited = 0x10,
            Associated = 0x20,
            IsWin2k = 0x40,
            HaveNtProcessInfo = HaveProcessInfo | IsNt
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }

    /// <devdoc>
    ///     This data structure contains information about a process that is collected
    ///     in bulk by querying the operating system.  The reason to make this a separate
    ///     structure from the process component is so that we can throw it away all at once
    ///     when Refresh is called on the component.
    /// </devdoc>
    /// <internalonly/>
    internal class ProcessInfo
    {
        public List<ThreadInfo> threadInfoList = new List<ThreadInfo>();
        public int basePriority;
        public string processName;
        public int processId;
        public int handleCount;
        public long poolPagedBytes;
        public long poolNonpagedBytes;
        public long virtualBytes;
        public long virtualBytesPeak;
        public long workingSetPeak;
        public long workingSet;
        public long pageFileBytesPeak;
        public long pageFileBytes;
        public long privateBytes;
        public int sessionId;
    }

    /// <devdoc>
    ///     This data structure contains information about a thread in a process that
    ///     is collected in bulk by querying the operating system.  The reason to
    ///     make this a separate structure from the ProcessThread component is so that we
    ///     can throw it away all at once when Refresh is called on the component.
    /// </devdoc>
    /// <internalonly/>
    internal class ThreadInfo
    {
        public int threadId;
        public int processId;
        public int basePriority;
        public int currentPriority;
        public IntPtr startAddress;
        public ThreadState threadState;
        public ThreadWaitReason threadWaitReason;
    }

    /// <devdoc>
    ///     This data structure contains information about a module in a process that
    ///     is collected in bulk by querying the operating system.  The reason to
    ///     make this a separate structure from the ProcessModule component is so that we
    ///     can throw it away all at once when Refresh is called on the component.
    /// </devdoc>
    /// <internalonly/>
    internal class ModuleInfo
    {
        public string baseName;
        public string fileName;
        public IntPtr baseOfDll;
        public IntPtr entryPoint;
        public int sizeOfImage;
    }

    internal static class EnvironmentBlock
    {
        public static byte[] ToByteArray(Dictionary<string, string> sd)
        {
            // get the keys
            string[] keys = new string[sd.Count];
            byte[] envBlock = null;
            sd.Keys.CopyTo(keys, 0);

            // sort both by the keys
            // Windows 2000 requires the environment block to be sorted by the key
            // It will first converting the case the strings and do ordinal comparison.

            // We do not use Array.Sort(keys, values, IComparer) since it is only supported
            // in System.Runtime contract from 4.20.0.0 and Test.Net depends on System.Runtime 4.0.10.0
            // we workaround this by sorting only the keys and then lookup the values form the keys.
            Array.Sort(keys, OrdinalCaseInsensitiveComparer.Default);

            // create a list of null terminated "key=val" strings
            StringBuilder stringBuff = new StringBuilder();
            for (int i = 0; i < sd.Count; ++i)
            {
                stringBuff.Append(keys[i]);
                stringBuff.Append('=');
                stringBuff.Append(sd[keys[i]]);
                stringBuff.Append('\0');
            }
            // an extra null at the end indicates end of list.
            stringBuff.Append('\0');
            envBlock = Encoding.Unicode.GetBytes(stringBuff.ToString());
            return envBlock;
        }
    }

    internal class OrdinalCaseInsensitiveComparer : IComparer
    {
        internal static readonly OrdinalCaseInsensitiveComparer Default = new OrdinalCaseInsensitiveComparer();

        public int Compare(Object a, Object b)
        {
            String sa = a as String;
            String sb = b as String;
            if (sa != null && sb != null)
            {
                return String.Compare(sa, sb, StringComparison.OrdinalIgnoreCase);
            }
            return Comparer<object>.Default.Compare(a, b);
        }
    }

    internal class ProcessThreadTimes
    {
        internal long create;
        internal long exit;
        internal long kernel;
        internal long user;

        public DateTime StartTime
        {
            get
            {
                return DateTime.FromFileTime(create);
            }
        }

        public DateTime ExitTime
        {
            get
            {
                return DateTime.FromFileTime(exit);
            }
        }

        public TimeSpan PrivilegedProcessorTime
        {
            get
            {
                return new TimeSpan(kernel);
            }
        }

        public TimeSpan UserProcessorTime
        {
            get
            {
                return new TimeSpan(user);
            }
        }

        public TimeSpan TotalProcessorTime
        {
            get
            {
                return new TimeSpan(user + kernel);
            }
        }
    }
}
