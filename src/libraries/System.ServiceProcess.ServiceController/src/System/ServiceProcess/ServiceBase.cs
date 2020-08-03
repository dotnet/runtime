// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading;

using static Interop.Advapi32;

namespace System.ServiceProcess
{
    /// <devdoc>
    /// <para>Provides a base class for a service that will exist as part of a service application. <see cref='System.ServiceProcess.ServiceBase'/>
    /// must be derived when creating a new service class.</para>
    /// </devdoc>
    public class ServiceBase : Component
    {
        private SERVICE_STATUS _status;
        private IntPtr _statusHandle;
        private ServiceControlCallbackEx? _commandCallbackEx;
        private ServiceMainCallback? _mainCallback;
        private ManualResetEvent? _startCompletedSignal;
        private ExceptionDispatchInfo? _startFailedException;
        private int _acceptedCommands;
        private string _serviceName;
        private bool _nameFrozen;          // set to true once we've started running and ServiceName can't be changed any more.
        private bool _commandPropsFrozen;  // set to true once we've use the Can... properties.
        private bool _disposed;
        private bool _initialized;
        private EventLog? _eventLog;

        /// <devdoc>
        ///    <para>
        ///       Indicates the maximum size for a service name.
        ///    </para>
        /// </devdoc>
        public const int MaxNameLength = 80;

        /// <devdoc>
        /// <para>Creates a new instance of the <see cref='System.ServiceProcess.ServiceBase()'/> class.</para>
        /// </devdoc>
        public ServiceBase()
        {
            _acceptedCommands = AcceptOptions.ACCEPT_STOP;
            ServiceName = "";
            AutoLog = true;
        }

        /// <devdoc>
        /// When this method is called from OnStart, OnStop, OnPause or OnContinue,
        /// the specified wait hint is passed to the
        /// Service Control Manager to avoid having the service marked as not responding.
        /// </devdoc>
        public unsafe void RequestAdditionalTime(int milliseconds)
        {
            fixed (SERVICE_STATUS* pStatus = &_status)
            {
                if (_status.currentState != ServiceControlStatus.STATE_CONTINUE_PENDING &&
                    _status.currentState != ServiceControlStatus.STATE_START_PENDING &&
                    _status.currentState != ServiceControlStatus.STATE_STOP_PENDING &&
                    _status.currentState != ServiceControlStatus.STATE_PAUSE_PENDING)
                {
                    throw new InvalidOperationException(SR.NotInPendingState);
                }

                _status.waitHint = milliseconds;
                _status.checkPoint++;
                SetServiceStatus(_statusHandle, pStatus);
            }
        }

        /// <devdoc>
        /// Indicates whether to report Start, Stop, Pause, and Continue commands in the event
        /// </devdoc>
        [DefaultValue(true)]
        public bool AutoLog { get; set; }

        /// <devdoc>
        /// The termination code for the service.  Set this to a non-zero value before
        /// stopping to indicate an error to the Service Control Manager.
        /// </devdoc>
        public int ExitCode
        {
            get
            {
                return _status.win32ExitCode;
            }
            set
            {
                _status.win32ExitCode = value;
            }
        }

        /// <devdoc>
        ///  Indicates whether the service can be handle notifications on
        ///  computer power status changes.
        /// </devdoc>
        [DefaultValue(false)]
        public bool CanHandlePowerEvent
        {
            get
            {
                return (_acceptedCommands & AcceptOptions.ACCEPT_POWEREVENT) != 0;
            }
            set
            {
                if (_commandPropsFrozen)
                    throw new InvalidOperationException(SR.CannotChangeProperties);

                if (value)
                {
                    _acceptedCommands |= AcceptOptions.ACCEPT_POWEREVENT;
                }
                else
                {
                    _acceptedCommands &= ~AcceptOptions.ACCEPT_POWEREVENT;
                }
            }
        }

        /// <devdoc>
        /// Indicates whether the service can handle Terminal Server session change events.
        /// </devdoc>
        [DefaultValue(false)]
        public bool CanHandleSessionChangeEvent
        {
            get
            {
                return (_acceptedCommands & AcceptOptions.ACCEPT_SESSIONCHANGE) != 0;
            }
            set
            {
                if (_commandPropsFrozen)
                    throw new InvalidOperationException(SR.CannotChangeProperties);

                if (value)
                {
                    _acceptedCommands |= AcceptOptions.ACCEPT_SESSIONCHANGE;
                }
                else
                {
                    _acceptedCommands &= ~AcceptOptions.ACCEPT_SESSIONCHANGE;
                }
            }
        }

        /// <devdoc>
        ///    <para> Indicates whether the service can be paused
        ///       and resumed.</para>
        /// </devdoc>
        [DefaultValue(false)]
        public bool CanPauseAndContinue
        {
            get
            {
                return (_acceptedCommands & AcceptOptions.ACCEPT_PAUSE_CONTINUE) != 0;
            }
            set
            {
                if (_commandPropsFrozen)
                    throw new InvalidOperationException(SR.CannotChangeProperties);

                if (value)
                {
                    _acceptedCommands |= AcceptOptions.ACCEPT_PAUSE_CONTINUE;
                }
                else
                {
                    _acceptedCommands &= ~AcceptOptions.ACCEPT_PAUSE_CONTINUE;
                }
            }
        }

        /// <devdoc>
        ///    <para> Indicates whether the service should be notified when
        ///       the system is shutting down.</para>
        /// </devdoc>
        [DefaultValue(false)]
        public bool CanShutdown
        {
            get
            {
                return (_acceptedCommands & AcceptOptions.ACCEPT_SHUTDOWN) != 0;
            }
            set
            {
                if (_commandPropsFrozen)
                    throw new InvalidOperationException(SR.CannotChangeProperties);

                if (value)
                {
                    _acceptedCommands |= AcceptOptions.ACCEPT_SHUTDOWN;
                }
                else
                {
                    _acceptedCommands &= ~AcceptOptions.ACCEPT_SHUTDOWN;
                }
            }
        }

        /// <devdoc>
        ///    <para> Indicates whether the service can be
        ///       stopped once it has started.</para>
        /// </devdoc>
        [DefaultValue(true)]
        public bool CanStop
        {
            get
            {
                return (_acceptedCommands & AcceptOptions.ACCEPT_STOP) != 0;
            }
            set
            {
                if (_commandPropsFrozen)
                    throw new InvalidOperationException(SR.CannotChangeProperties);

                if (value)
                {
                    _acceptedCommands |= AcceptOptions.ACCEPT_STOP;
                }
                else
                {
                    _acceptedCommands &= ~AcceptOptions.ACCEPT_STOP;
                }
            }
        }

        /// <devdoc>
        /// can be used to write notification of service command calls, such as Start and Stop, to the Application event log. This property is read-only.
        /// </devdoc>
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public virtual EventLog EventLog
        {
            get
            {
                if (_eventLog == null)
                {
                    _eventLog = new EventLog("Application");
                    _eventLog.Source = ServiceName;
                }

                return _eventLog;
            }
        }

        [EditorBrowsable(EditorBrowsableState.Advanced)]
        protected IntPtr ServiceHandle
        {
            get
            {
                return _statusHandle;
            }
        }

        /// <devdoc>
        ///    <para> Indicates the short name used to identify the service to the system.</para>
        /// </devdoc>
        public string ServiceName
        {
            get
            {
                return _serviceName;
            }
            [MemberNotNull(nameof(_serviceName))]
            set
            {
                if (_nameFrozen)
                    throw new InvalidOperationException(SR.CannotChangeName);

                // For component properties, "" is a special case.
                if (value != "" && !ValidServiceName(value))
                    throw new ArgumentException(SR.Format(SR.ServiceName, value, ServiceBase.MaxNameLength.ToString(CultureInfo.CurrentCulture)));

                _serviceName = value;
            }
        }

        internal static bool ValidServiceName(string serviceName)
        {
            if (serviceName == null)
                return false;

            // not too long and check for empty name as well.
            if (serviceName.Length > ServiceBase.MaxNameLength || serviceName.Length == 0)
                return false;

            // no slashes or backslash allowed
            foreach (char c in serviceName)
            {
                if ((c == '\\') || (c == '/'))
                    return false;
            }

            return true;
        }

        /// <devdoc>
        ///    <para>Disposes of the resources (other than memory ) used by
        ///       the <see cref='System.ServiceProcess.ServiceBase'/>.</para>
        ///    This is called from <see cref="Run(ServiceBase[])"/> when all
        ///    services in the process have entered the SERVICE_STOPPED state.
        /// </devdoc>
        protected override void Dispose(bool disposing)
        {
            _nameFrozen = false;
            _commandPropsFrozen = false;
            _disposed = true;
            base.Dispose(disposing);
        }

        /// <devdoc>
        ///    <para> When implemented in a
        ///       derived class,
        ///       executes when a Continue command is sent to the service
        ///       by the
        ///       Service Control Manager. Specifies the actions to take when a
        ///       service resumes normal functioning after being paused.</para>
        /// </devdoc>
        protected virtual void OnContinue()
        {
        }

        /// <devdoc>
        ///    <para> When implemented in a
        ///       derived class, executes when a Pause command is sent
        ///       to
        ///       the service by the Service Control Manager. Specifies the
        ///       actions to take when a service pauses.</para>
        /// </devdoc>
        protected virtual void OnPause()
        {
        }

        /// <devdoc>
        ///    <para>
        ///         When implemented in a derived class, executes when the computer's
        ///         power status has changed.
        ///    </para>
        /// </devdoc>
        protected virtual bool OnPowerEvent(PowerBroadcastStatus powerStatus)
        {
            return true;
        }

        /// <devdoc>
        ///    <para>When implemented in a derived class,
        ///       executes when a Terminal Server session change event is received.</para>
        /// </devdoc>
        protected virtual void OnSessionChange(SessionChangeDescription changeDescription)
        {
        }

        /// <devdoc>
        ///    <para>When implemented in a derived class,
        ///       executes when the system is shutting down.
        ///       Specifies what should
        ///       happen just prior
        ///       to the system shutting down.</para>
        /// </devdoc>
        protected virtual void OnShutdown()
        {
        }

        /// <devdoc>
        ///    <para> When implemented in a
        ///       derived class, executes when a Start command is sent
        ///       to the service by the Service
        ///       Control Manager. Specifies the actions to take when the service starts.</para>
        ///    <note type="rnotes">
        ///       Tech review note:
        ///       except that the SCM does not allow passing arguments, so this overload will
        ///       never be called by the SCM in the current version. Question: Is this true even
        ///       when the string array is empty? What should we say, then. Can
        ///       a ServiceBase derived class only be called programmatically? Will
        ///       OnStart never be called if you use the SCM to start the service? What about
        ///       services that start automatically at boot-up?
        ///    </note>
        /// </devdoc>
        protected virtual void OnStart(string[] args)
        {
        }

        /// <devdoc>
        ///    <para> When implemented in a
        ///       derived class, executes when a Stop command is sent to the
        ///       service by the Service Control Manager. Specifies the actions to take when a
        ///       service stops
        ///       running.</para>
        /// </devdoc>
        protected virtual void OnStop()
        {
        }

        private unsafe void DeferredContinue()
        {
            fixed (SERVICE_STATUS* pStatus = &_status)
            {
                try
                {
                    OnContinue();
                    WriteLogEntry(SR.ContinueSuccessful);
                    _status.currentState = ServiceControlStatus.STATE_RUNNING;
                }
                catch (Exception e)
                {
                    _status.currentState = ServiceControlStatus.STATE_PAUSED;
                    WriteLogEntry(SR.Format(SR.ContinueFailed, e), EventLogEntryType.Error);

                    // We re-throw the exception so that the advapi32 code can report
                    // ERROR_EXCEPTION_IN_SERVICE as it would for native services.
                    throw;
                }
                finally
                {
                    SetServiceStatus(_statusHandle, pStatus);
                }
            }
        }

        private void DeferredCustomCommand(int command)
        {
            try
            {
                OnCustomCommand(command);
                WriteLogEntry(SR.CommandSuccessful);
            }
            catch (Exception e)
            {
                WriteLogEntry(SR.Format(SR.CommandFailed, e), EventLogEntryType.Error);

                // We should re-throw the exception so that the advapi32 code can report
                // ERROR_EXCEPTION_IN_SERVICE as it would for native services.
                throw;
            }
        }

        private unsafe void DeferredPause()
        {
            fixed (SERVICE_STATUS* pStatus = &_status)
            {
                try
                {
                    OnPause();
                    WriteLogEntry(SR.PauseSuccessful);
                    _status.currentState = ServiceControlStatus.STATE_PAUSED;
                }
                catch (Exception e)
                {
                    _status.currentState = ServiceControlStatus.STATE_RUNNING;
                    WriteLogEntry(SR.Format(SR.PauseFailed, e), EventLogEntryType.Error);

                    // We re-throw the exception so that the advapi32 code can report
                    // ERROR_EXCEPTION_IN_SERVICE as it would for native services.
                    throw;
                }
                finally
                {
                    SetServiceStatus(_statusHandle, pStatus);
                }
            }
        }

        private void DeferredPowerEvent(int eventType, IntPtr eventData)
        {
            // Note: The eventData pointer might point to an invalid location
            // This might happen because, between the time the eventData ptr was
            // captured and the time this deferred code runs, the ptr might have
            // already been freed.
            try
            {
                OnPowerEvent((PowerBroadcastStatus)eventType);

                WriteLogEntry(SR.PowerEventOK);
            }
            catch (Exception e)
            {
                WriteLogEntry(SR.Format(SR.PowerEventFailed, e), EventLogEntryType.Error);

                // We rethrow the exception so that advapi32 code can report
                // ERROR_EXCEPTION_IN_SERVICE as it would for native services.
                throw;
            }
        }

        private void DeferredSessionChange(int eventType, int sessionId)
        {
            try
            {
                OnSessionChange(new SessionChangeDescription((SessionChangeReason)eventType, sessionId));
            }
            catch (Exception e)
            {
                WriteLogEntry(SR.Format(SR.SessionChangeFailed, e), EventLogEntryType.Error);

                // We rethrow the exception so that advapi32 code can report
                // ERROR_EXCEPTION_IN_SERVICE as it would for native services.
                throw;
            }
        }

        // We mustn't call OnStop directly from the command callback, as this will
        // tie up the command thread for the duration of the OnStop, which can be lengthy.
        // This is a problem when multiple services are hosted in a single process.
        private unsafe void DeferredStop()
        {
            fixed (SERVICE_STATUS* pStatus = &_status)
            {
                int previousState = _status.currentState;

                _status.checkPoint = 0;
                _status.waitHint = 0;
                _status.currentState = ServiceControlStatus.STATE_STOP_PENDING;
                SetServiceStatus(_statusHandle, pStatus);
                try
                {
                    OnStop();
                    WriteLogEntry(SR.StopSuccessful);
                    _status.currentState = ServiceControlStatus.STATE_STOPPED;
                    SetServiceStatus(_statusHandle, pStatus);
                }
                catch (Exception e)
                {
                    _status.currentState = previousState;
                    SetServiceStatus(_statusHandle, pStatus);
                    WriteLogEntry(SR.Format(SR.StopFailed, e), EventLogEntryType.Error);
                    throw;
                }
            }
        }

        private unsafe void DeferredShutdown()
        {
            try
            {
                OnShutdown();
                WriteLogEntry(SR.ShutdownOK);

                if (_status.currentState == ServiceControlStatus.STATE_PAUSED || _status.currentState == ServiceControlStatus.STATE_RUNNING)
                {
                    fixed (SERVICE_STATUS* pStatus = &_status)
                    {
                        _status.checkPoint = 0;
                        _status.waitHint = 0;
                        _status.currentState = ServiceControlStatus.STATE_STOPPED;
                        SetServiceStatus(_statusHandle, pStatus);
                    }
                }
            }
            catch (Exception e)
            {
                WriteLogEntry(SR.Format(SR.ShutdownFailed, e), EventLogEntryType.Error);
                throw;
            }
        }

        /// <devdoc>
        /// <para>When implemented in a derived class, <see cref='System.ServiceProcess.ServiceBase.OnCustomCommand'/>
        /// executes when a custom command is passed to
        /// the service. Specifies the actions to take when
        /// a command with the specified parameter value occurs.</para>
        /// <note type="rnotes">
        ///    Previously had "Passed to the
        ///    service by
        ///    the SCM", but the SCM doesn't pass custom commands. Do we want to indicate an
        ///    agent here? Would it be the ServiceController, or is there another way to pass
        ///    the int into the service? I thought that the SCM did pass it in, but
        ///    otherwise ignored it since it was an int it doesn't recognize. I was under the
        ///    impression that the difference was that the SCM didn't have default processing, so
        ///    it transmitted it without examining it or trying to performs its own
        ///    default behavior on it. Please correct where my understanding is wrong in the
        ///    second paragraph below--what, if any, contact does the SCM have with a
        ///    custom command?
        /// </note>
        /// </devdoc>
        protected virtual void OnCustomCommand(int command)
        {
        }

        /// <devdoc>
        ///    <para>Provides the main entry point for an executable that
        ///       contains multiple associated services. Loads the specified services into memory so they can be
        ///       started.</para>
        /// </devdoc>
        public static unsafe void Run(ServiceBase[] services)
        {
            if (services == null || services.Length == 0)
                throw new ArgumentException(SR.NoServices);

            IntPtr entriesPointer = Marshal.AllocHGlobal(checked((services.Length + 1) * sizeof(SERVICE_TABLE_ENTRY)));
            Span<SERVICE_TABLE_ENTRY> entries = new Span<SERVICE_TABLE_ENTRY>((void*)entriesPointer, services.Length + 1);
            entries.Clear();
            try
            {
                bool multipleServices = services.Length > 1;

                // The members of the last entry in the table must have NULL values to designate the end of the table.
                // Leave the last element in the entries span to be zeroed out.
                for (int index = 0; index < services.Length; ++index)
                {
                    ServiceBase service = services[index];
                    service.Initialize(multipleServices);
                    // This method allocates on unmanaged heap; Make sure that the contents are freed after use.
                    entries[index] = service.GetEntry();
                }

                // While the service is running, this function will never return. It will return when the service
                // is stopped.
                // After it returns, SCM might terminate the process at any time
                // (so subsequent code is not guaranteed to run).
                bool res = StartServiceCtrlDispatcher(entriesPointer);

                foreach (ServiceBase service in services)
                {
                    if (service._startFailedException != null)
                    {
                        // Propagate exceptions throw during OnStart.
                        // Note that this same exception is also thrown from ServiceMainCallback
                        // (so SCM can see it as well).
                        service._startFailedException.Throw();
                    }
                }

                string errorMessage = "";

                if (!res)
                {
                    errorMessage = new Win32Exception().Message;
                    Console.WriteLine(SR.CantStartFromCommandLine);
                }

                foreach (ServiceBase service in services)
                {
                    service.Dispose();
                    if (!res)
                    {
                        service.WriteLogEntry(SR.Format(SR.StartFailed, errorMessage), EventLogEntryType.Error);
                    }
                }
            }
            finally
            {
                // Free the pointer to the name of the service on the unmanaged heap.
                for (int i = 0; i < entries.Length; i++)
                {
                    Marshal.FreeHGlobal(entries[i].name);
                }

                // Free the unmanaged array containing the entries.
                Marshal.FreeHGlobal(entriesPointer);
            }
        }

        /// <devdoc>
        ///    <para>Provides the main
        ///       entry point for an executable that contains a single
        ///       service. Loads the service into memory so it can be
        ///       started.</para>
        /// </devdoc>
        public static void Run(ServiceBase service)
        {
            if (service == null)
                throw new ArgumentException(SR.NoServices);

            Run(new ServiceBase[] { service });
        }

        public void Stop()
        {
            DeferredStop();
        }

        private void Initialize(bool multipleServices)
        {
            if (!_initialized)
            {
                //Cannot register the service with NT service manatger if the object has been disposed, since finalization has been suppressed.
                if (_disposed)
                    throw new ObjectDisposedException(GetType().Name);

                if (!multipleServices)
                {
                    _status.serviceType = ServiceTypeOptions.SERVICE_TYPE_WIN32_OWN_PROCESS;
                }
                else
                {
                    _status.serviceType = ServiceTypeOptions.SERVICE_TYPE_WIN32_SHARE_PROCESS;
                }

                _status.currentState = ServiceControlStatus.STATE_START_PENDING;
                _status.controlsAccepted = 0;
                _status.win32ExitCode = 0;
                _status.serviceSpecificExitCode = 0;
                _status.checkPoint = 0;
                _status.waitHint = 0;

                _mainCallback = new ServiceMainCallback(this.ServiceMainCallback);
                _commandCallbackEx = new ServiceControlCallbackEx(this.ServiceCommandCallbackEx);

                _initialized = true;
            }
        }

        // Make sure that the name field is freed after use. We allocate a new string to avoid holding one central handle,
        // which may lead to dangling pointer if Dispose is called in other thread.
        private SERVICE_TABLE_ENTRY GetEntry()
        {
            _nameFrozen = true;
            return new SERVICE_TABLE_ENTRY()
            {
                callback = Marshal.GetFunctionPointerForDelegate(_mainCallback!),
                name = Marshal.StringToHGlobalUni(_serviceName)
            };
        }

        private int ServiceCommandCallbackEx(int command, int eventType, IntPtr eventData, IntPtr eventContext)
        {
            switch (command)
            {
                case ControlOptions.CONTROL_POWEREVENT:
                    {
                        ThreadPool.QueueUserWorkItem(_ => DeferredPowerEvent(eventType, eventData));
                        break;
                    }

                case ControlOptions.CONTROL_SESSIONCHANGE:
                    {
                        // The eventData pointer can be released between now and when the DeferredDelegate gets called.
                        // So we capture the session id at this point
                        WTSSESSION_NOTIFICATION sessionNotification = new WTSSESSION_NOTIFICATION();
                        Marshal.PtrToStructure(eventData, sessionNotification);
                        ThreadPool.QueueUserWorkItem(_ => DeferredSessionChange(eventType, sessionNotification.sessionId));
                        break;
                    }

                default:
                    {
                        ServiceCommandCallback(command);
                        break;
                    }
            }

            return 0;
        }

        /// <devdoc>
        ///     Command Handler callback is called by NT .
        ///     Need to take specific action in response to each
        ///     command message. There is usually no need to override this method.
        ///     Instead, override OnStart, OnStop, OnCustomCommand, etc.
        /// </devdoc>
        /// <internalonly/>
        private unsafe void ServiceCommandCallback(int command)
        {
            fixed (SERVICE_STATUS* pStatus = &_status)
            {
                if (command == ControlOptions.CONTROL_INTERROGATE)
                    SetServiceStatus(_statusHandle, pStatus);
                else if (_status.currentState != ServiceControlStatus.STATE_CONTINUE_PENDING &&
                    _status.currentState != ServiceControlStatus.STATE_START_PENDING &&
                    _status.currentState != ServiceControlStatus.STATE_STOP_PENDING &&
                    _status.currentState != ServiceControlStatus.STATE_PAUSE_PENDING)
                {
                    switch (command)
                    {
                        case ControlOptions.CONTROL_CONTINUE:
                            if (_status.currentState == ServiceControlStatus.STATE_PAUSED)
                            {
                                _status.currentState = ServiceControlStatus.STATE_CONTINUE_PENDING;
                                SetServiceStatus(_statusHandle, pStatus);

                                ThreadPool.QueueUserWorkItem(_ => DeferredContinue());
                            }

                            break;

                        case ControlOptions.CONTROL_PAUSE:
                            if (_status.currentState == ServiceControlStatus.STATE_RUNNING)
                            {
                                _status.currentState = ServiceControlStatus.STATE_PAUSE_PENDING;
                                SetServiceStatus(_statusHandle, pStatus);

                                ThreadPool.QueueUserWorkItem(_ => DeferredPause());
                            }

                            break;

                        case ControlOptions.CONTROL_STOP:
                            int previousState = _status.currentState;
                            //
                            // Can't perform all of the service shutdown logic from within the command callback.
                            // This is because there is a single ScDispatcherLoop for the entire process.  Instead, we queue up an
                            // asynchronous call to "DeferredStop", and return immediately.  This is crucial for the multiple service
                            // per process scenario, such as the new managed service host model.
                            //
                            if (_status.currentState == ServiceControlStatus.STATE_PAUSED || _status.currentState == ServiceControlStatus.STATE_RUNNING)
                            {
                                _status.currentState = ServiceControlStatus.STATE_STOP_PENDING;
                                SetServiceStatus(_statusHandle, pStatus);
                                // Set our copy of the state back to the previous so that the deferred stop routine
                                // can also save the previous state.
                                _status.currentState = previousState;

                                ThreadPool.QueueUserWorkItem(_ => DeferredStop());
                            }

                            break;

                        case ControlOptions.CONTROL_SHUTDOWN:
                            //
                            // Same goes for shutdown -- this needs to be very responsive, so we can't have one service tying up the
                            // dispatcher loop.
                            //
                            ThreadPool.QueueUserWorkItem(_ => DeferredShutdown());
                            break;

                        default:
                            ThreadPool.QueueUserWorkItem(_ => DeferredCustomCommand(command));
                            break;
                    }
                }
            }
        }

        // Need to execute the start method on a thread pool thread.
        // Most applications will start asynchronous operations in the
        // OnStart method. If such a method is executed in MainCallback
        // thread, the async operations might get canceled immediately.
        private void ServiceQueuedMainCallback(object state)
        {
            string[] args = (string[])state;

            try
            {
                OnStart(args);
                WriteLogEntry(SR.StartSuccessful);
                _status.checkPoint = 0;
                _status.waitHint = 0;
                _status.currentState = ServiceControlStatus.STATE_RUNNING;
            }
            catch (Exception e)
            {
                WriteLogEntry(SR.Format(SR.StartFailed, e), EventLogEntryType.Error);
                _status.currentState = ServiceControlStatus.STATE_STOPPED;

                // We capture the exception so that it can be propagated
                // from ServiceBase.Run.
                // We also use the presence of this exception to inform SCM
                // that the service failed to start successfully.
                _startFailedException = ExceptionDispatchInfo.Capture(e);
            }
            _startCompletedSignal!.Set();
        }

        /// <devdoc>
        ///     ServiceMain callback is called by NT .
        ///     It is expected that we register the command handler,
        ///     and start the service at this point.
        /// </devdoc>
        /// <internalonly/>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public unsafe void ServiceMainCallback(int argCount, IntPtr argPointer)
        {
            fixed (SERVICE_STATUS* pStatus = &_status)
            {
                string[]? args = null;

                if (argCount > 0)
                {
                    char** argsAsPtr = (char**)argPointer.ToPointer();

                    //Lets read the arguments
                    // the first arg is always the service name. We don't want to pass that in.
                    args = new string[argCount - 1];

                    for (int index = 0; index < args.Length; ++index)
                    {
                        // we increment the pointer first so we skip over the first argument.
                        argsAsPtr++;
                        args[index] = Marshal.PtrToStringUni((IntPtr)(*argsAsPtr))!;
                    }
                }

                // If we are being hosted, then Run will not have been called, since the EXE's Main entrypoint is not called.
                if (!_initialized)
                {
                    Initialize(true);
                }

                _statusHandle = RegisterServiceCtrlHandlerEx(ServiceName, _commandCallbackEx, (IntPtr)0);

                _nameFrozen = true;
                if (_statusHandle == (IntPtr)0)
                {
                    string errorMessage = new Win32Exception().Message;
                    WriteLogEntry(SR.Format(SR.StartFailed, errorMessage), EventLogEntryType.Error);
                }

                _status.controlsAccepted = _acceptedCommands;
                _commandPropsFrozen = true;
                if ((_status.controlsAccepted & AcceptOptions.ACCEPT_STOP) != 0)
                {
                    _status.controlsAccepted = _status.controlsAccepted | AcceptOptions.ACCEPT_SHUTDOWN;
                }

                _status.currentState = ServiceControlStatus.STATE_START_PENDING;

                bool statusOK = SetServiceStatus(_statusHandle, pStatus);

                if (!statusOK)
                {
                    return;
                }

                // Need to execute the start method on a thread pool thread.
                // Most applications will start asynchronous operations in the
                // OnStart method. If such a method is executed in the current
                // thread, the async operations might get canceled immediately
                // since NT will terminate this thread right after this function
                // finishes.
                _startCompletedSignal = new ManualResetEvent(false);
                _startFailedException = null;
                ThreadPool.QueueUserWorkItem(new WaitCallback(this.ServiceQueuedMainCallback!), args);
                _startCompletedSignal.WaitOne();

                if (_startFailedException != null)
                {
                    // Inform SCM that the service could not be started successfully.
                    // (Unless the service has already provided another failure exit code)
                    if (_status.win32ExitCode == 0)
                    {
                        _status.win32ExitCode = ServiceControlStatus.ERROR_EXCEPTION_IN_SERVICE;
                    }
                }

                statusOK = SetServiceStatus(_statusHandle, pStatus);
                if (!statusOK)
                {
                    WriteLogEntry(SR.Format(SR.StartFailed, new Win32Exception().Message), EventLogEntryType.Error);
                    _status.currentState = ServiceControlStatus.STATE_STOPPED;
                    SetServiceStatus(_statusHandle, pStatus);
                }
            }
        }

        private void WriteLogEntry(string message, EventLogEntryType type = EventLogEntryType.Information)
        {
            // EventLog failures shouldn't affect the service operation
            try
            {
                if (AutoLog)
                {
                    EventLog.WriteEntry(message, type);
                }
            }
            catch
            {
                // Do nothing.  Not having the event log is bad, but not starting the service as a result is worse.
            }
        }
    }
}
