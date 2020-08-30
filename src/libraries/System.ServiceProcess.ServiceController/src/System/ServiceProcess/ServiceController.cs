// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading;

namespace System.ServiceProcess
{
    /// This class represents an NT service. It allows you to connect to a running or stopped service
    /// and manipulate it or get information about it.
    [Designer("System.ServiceProcess.Design.ServiceControllerDesigner, System.Design, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class ServiceController : Component
    {
        private string _machineName; // Never null
        private readonly ManualResetEvent _waitForStatusSignal = new ManualResetEvent(false);
        private const string DefaultMachineName = ".";

        private string? _name;
        private string? _eitherName;
        private string? _displayName;

        private int _commandsAccepted;
        private bool _statusGenerated;
        private bool _startTypeInitialized;
        private int _type;
        private bool _disposed;
        private SafeServiceHandle? _serviceManagerHandle;
        private ServiceControllerStatus _status;
        private ServiceController[]? _dependentServices;
        private ServiceController[]? _servicesDependedOn;
        private ServiceStartMode _startType;

        public ServiceController()
        {
            _machineName = DefaultMachineName;
            _type = Interop.Advapi32.ServiceTypeOptions.SERVICE_TYPE_ALL;
        }

        /// Creates a ServiceController object, based on service name.
        public ServiceController(string name)
            : this(name, DefaultMachineName)
        {
        }

        /// Creates a ServiceController object, based on machine and service name.
        public ServiceController(string name, string machineName)
        {
            if (!CheckMachineName(machineName))
                throw new ArgumentException(SR.Format(SR.BadMachineName, machineName));

            if (string.IsNullOrEmpty(name))
                throw new ArgumentException(SR.Format(SR.InvalidParameter, nameof(name), name));

            _machineName = machineName;
            _eitherName = name;
            _type = Interop.Advapi32.ServiceTypeOptions.SERVICE_TYPE_ALL;
        }


        /// Used by the GetServices and GetDevices methods. Avoids duplicating work by the static
        /// methods and our own GenerateInfo().
        private ServiceController(string machineName, Interop.Advapi32.ENUM_SERVICE_STATUS status)
        {
            if (!CheckMachineName(machineName))
                throw new ArgumentException(SR.Format(SR.BadMachineName, machineName));

            _machineName = machineName;
            _name = status.serviceName;
            _displayName = status.displayName;
            _commandsAccepted = status.controlsAccepted;
            _status = (ServiceControllerStatus)status.currentState;
            _type = status.serviceType;
            _statusGenerated = true;
        }

        /// Used by the GetServicesInGroup method.
        private ServiceController(string machineName, Interop.Advapi32.ENUM_SERVICE_STATUS_PROCESS status)
        {
            if (!CheckMachineName(machineName))
                throw new ArgumentException(SR.Format(SR.BadMachineName, machineName));

            _machineName = machineName;
            _name = status.serviceName;
            _displayName = status.displayName;
            _commandsAccepted = status.controlsAccepted;
            _status = (ServiceControllerStatus)status.currentState;
            _type = status.serviceType;
            _statusGenerated = true;
        }

        /// Tells if the service referenced by this object can be paused.
        public bool CanPauseAndContinue
        {
            get
            {
                GenerateStatus();
                return (_commandsAccepted & Interop.Advapi32.AcceptOptions.ACCEPT_PAUSE_CONTINUE) != 0;
            }
        }


        /// Tells if the service is notified when system shutdown occurs.
        public bool CanShutdown
        {
            get
            {
                GenerateStatus();
                return (_commandsAccepted & Interop.Advapi32.AcceptOptions.ACCEPT_SHUTDOWN) != 0;
            }
        }

        /// Tells if the service referenced by this object can be stopped.
        public bool CanStop
        {
            get
            {
                GenerateStatus();
                return (_commandsAccepted & Interop.Advapi32.AcceptOptions.ACCEPT_STOP) != 0;
            }
        }

        /// The descriptive name shown for this service in the Service applet.
        public string DisplayName
        {
            get
            {
                if (string.IsNullOrEmpty(_displayName))
                    GenerateNames();
                return _displayName;
            }
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                if (string.Equals(value, _displayName, StringComparison.OrdinalIgnoreCase))
                {
                    // they're just changing the casing. No need to close.
                    _displayName = value;
                    return;
                }

                Close();
                _displayName = value;
                _name = "";
            }
        }

        /// The set of services that depend on this service. These are the services that will be stopped if
        /// this service is stopped.
        public ServiceController[] DependentServices
        {
            get
            {
                if (_dependentServices == null)
                {
                    using (var serviceHandle = GetServiceHandle(Interop.Advapi32.ServiceOptions.SERVICE_ENUMERATE_DEPENDENTS))
                    {
                        // figure out how big a buffer we need to get the info
                        int bytesNeeded = 0;
                        int numEnumerated = 0;
                        bool result = Interop.Advapi32.EnumDependentServices(serviceHandle, Interop.Advapi32.ServiceState.SERVICE_STATE_ALL, IntPtr.Zero, 0,
                            ref bytesNeeded, ref numEnumerated);
                        if (result)
                        {
                            _dependentServices = Array.Empty<ServiceController>();
                            return _dependentServices;
                        }

                        int lastError = Marshal.GetLastWin32Error();
                        if (lastError != Interop.Errors.ERROR_MORE_DATA)
                            throw new Win32Exception(lastError);

                        // allocate the buffer
                        IntPtr enumBuffer = Marshal.AllocHGlobal((IntPtr)bytesNeeded);

                        try
                        {
                            // get all the info
                            result = Interop.Advapi32.EnumDependentServices(serviceHandle, Interop.Advapi32.ServiceState.SERVICE_STATE_ALL, enumBuffer, bytesNeeded,
                                ref bytesNeeded, ref numEnumerated);
                            if (!result)
                                throw new Win32Exception();

                            // for each of the entries in the buffer, create a new ServiceController object.
                            _dependentServices = new ServiceController[numEnumerated];
                            for (int i = 0; i < numEnumerated; i++)
                            {
                                Interop.Advapi32.ENUM_SERVICE_STATUS status = new Interop.Advapi32.ENUM_SERVICE_STATUS();
                                IntPtr structPtr = (IntPtr)((long)enumBuffer + (i * Marshal.SizeOf<Interop.Advapi32.ENUM_SERVICE_STATUS>()));
                                Marshal.PtrToStructure(structPtr, status);
                                _dependentServices[i] = new ServiceController(_machineName, status);
                            }
                        }
                        finally
                        {
                            Marshal.FreeHGlobal(enumBuffer);
                        }
                    }
                }

                return _dependentServices;
            }
        }

        /// The name of the machine on which this service resides.
        public string MachineName
        {
            get
            {
                return _machineName;
            }
            set
            {
                if (!CheckMachineName(value))
                    throw new ArgumentException(SR.Format(SR.BadMachineName, value));

                if (string.Equals(_machineName, value, StringComparison.OrdinalIgnoreCase))
                {
                    // no need to close, because the most they're changing is the
                    // casing.
                    _machineName = value;
                    return;
                }

                Close();
                _machineName = value;
            }
        }

        /// Returns the short name of the service referenced by this object.
        public string ServiceName
        {
            get
            {
                if (string.IsNullOrEmpty(_name))
                    GenerateNames();
                return _name;
            }
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                if (string.Equals(value, _name, StringComparison.OrdinalIgnoreCase))
                {
                    // they might be changing the casing, but the service we refer to
                    // is the same. No need to close.
                    _name = value;
                    return;
                }

                if (!ServiceBase.ValidServiceName(value))
                    throw new ArgumentException(SR.Format(SR.ServiceName, value, ServiceBase.MaxNameLength.ToString()));

                Close();
                _name = value;
                _displayName = "";
            }
        }

        public unsafe ServiceController[] ServicesDependedOn
        {
            get
            {
                if (_servicesDependedOn != null)
                    return _servicesDependedOn;

                using (var serviceHandle = GetServiceHandle(Interop.Advapi32.ServiceOptions.SERVICE_QUERY_CONFIG))
                {
                    int bytesNeeded = 0;
                    bool success = Interop.Advapi32.QueryServiceConfig(serviceHandle, IntPtr.Zero, 0, out bytesNeeded);
                    if (success)
                    {
                        _servicesDependedOn = Array.Empty<ServiceController>();
                        return _servicesDependedOn;
                    }

                    int lastError = Marshal.GetLastWin32Error();
                    if (lastError != Interop.Errors.ERROR_INSUFFICIENT_BUFFER)
                        throw new Win32Exception(lastError);

                    // get the info
                    IntPtr bufPtr = Marshal.AllocHGlobal((IntPtr)bytesNeeded);
                    try
                    {
                        success = Interop.Advapi32.QueryServiceConfig(serviceHandle, bufPtr, bytesNeeded, out bytesNeeded);
                        if (!success)
                            throw new Win32Exception(Marshal.GetLastWin32Error());

                        Interop.Advapi32.QUERY_SERVICE_CONFIG config = new Interop.Advapi32.QUERY_SERVICE_CONFIG();
                        Marshal.PtrToStructure(bufPtr, config);
                        Dictionary<string, ServiceController>? dependencyHash = null;

                        char* dependencyChar = config.lpDependencies;
                        if (dependencyChar != null)
                        {
                            // lpDependencies points to the start of multiple null-terminated strings. The list is
                            // double-null terminated.
                            int length = 0;
                            dependencyHash = new Dictionary<string, ServiceController>();
                            while (*(dependencyChar + length) != '\0')
                            {
                                length++;
                                if (*(dependencyChar + length) == '\0')
                                {
                                    string dependencyNameStr = new string(dependencyChar, 0, length);
                                    dependencyChar = dependencyChar + length + 1;
                                    length = 0;
                                    if (dependencyNameStr.StartsWith("+", StringComparison.Ordinal))
                                    {
                                        // this entry is actually a service load group
                                        Interop.Advapi32.ENUM_SERVICE_STATUS_PROCESS[] loadGroup = GetServicesInGroup(_machineName, dependencyNameStr.Substring(1));
                                        foreach (Interop.Advapi32.ENUM_SERVICE_STATUS_PROCESS groupMember in loadGroup)
                                        {
                                            if (!dependencyHash.ContainsKey(groupMember.serviceName!))
                                                dependencyHash.Add(groupMember.serviceName!, new ServiceController(MachineName, groupMember));
                                        }
                                    }
                                    else
                                    {
                                        if (!dependencyHash.ContainsKey(dependencyNameStr))
                                            dependencyHash.Add(dependencyNameStr, new ServiceController(dependencyNameStr, MachineName));
                                    }
                                }
                            }
                        }

                        if (dependencyHash != null)
                        {
                            _servicesDependedOn = new ServiceController[dependencyHash.Count];
                            dependencyHash.Values.CopyTo(_servicesDependedOn, 0);
                        }
                        else
                        {
                            _servicesDependedOn = Array.Empty<ServiceController>();
                        }

                        return _servicesDependedOn;
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(bufPtr);
                    }
                }
            }
        }

        public ServiceStartMode StartType
        {
            get
            {
                if (_startTypeInitialized)
                    return _startType;

                using (var serviceHandle = GetServiceHandle(Interop.Advapi32.ServiceOptions.SERVICE_QUERY_CONFIG))
                {
                    int bytesNeeded = 0;
                    bool success = Interop.Advapi32.QueryServiceConfig(serviceHandle, IntPtr.Zero, 0, out bytesNeeded);

                    int lastError = Marshal.GetLastWin32Error();
                    if (lastError != Interop.Errors.ERROR_INSUFFICIENT_BUFFER)
                        throw new Win32Exception(lastError);

                    // get the info
                    IntPtr bufPtr = Marshal.AllocHGlobal((IntPtr)bytesNeeded);
                    try
                    {
                        success = Interop.Advapi32.QueryServiceConfig(serviceHandle, bufPtr, bytesNeeded, out bytesNeeded);
                        if (!success)
                            throw new Win32Exception(Marshal.GetLastWin32Error());

                        Interop.Advapi32.QUERY_SERVICE_CONFIG config = new Interop.Advapi32.QUERY_SERVICE_CONFIG();
                        Marshal.PtrToStructure(bufPtr, config);

                        _startType = (ServiceStartMode)config.dwStartType;
                        _startTypeInitialized = true;
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(bufPtr);
                    }
                }

                return _startType;
            }
        }

        public SafeHandle ServiceHandle
        {
            get
            {
                return GetServiceHandle(Interop.Advapi32.ServiceOptions.SERVICE_ALL_ACCESS);
            }
        }

        /// Gets the status of the service referenced by this object, e.g., Running, Stopped, etc.
        public ServiceControllerStatus Status
        {
            get
            {
                GenerateStatus();
                return _status;
            }
        }

        /// Gets the type of service that this object references.
        public ServiceType ServiceType
        {
            get
            {
                GenerateStatus();
                return (ServiceType)_type;
            }
        }

        private static bool CheckMachineName(string value)
        {
            // string.Contains(char) is .NetCore2.1+ specific
            return !string.IsNullOrWhiteSpace(value) && value.IndexOf('\\') == -1;
        }

        /// <summary>
        /// Closes the handle to the service manager, but does not
        /// mark the class as disposed.
        /// </summary>
        /// <remarks>
        /// Violates design guidelines by not matching Dispose() -- matches .NET Framework
        /// </remarks>
        public void Close()
        {
            if (_serviceManagerHandle != null)
            {
                _serviceManagerHandle.Dispose();
                _serviceManagerHandle = null;
            }

            _statusGenerated = false;
            _startTypeInitialized = false;
            _type = Interop.Advapi32.ServiceTypeOptions.SERVICE_TYPE_ALL;
        }

        /// <summary>
        /// Closes the handle to the service manager, and disposes.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            Close();
            _disposed = true;
            base.Dispose(disposing);
        }

        private unsafe void GenerateStatus()
        {
            if (!_statusGenerated)
            {
                using (var serviceHandle = GetServiceHandle(Interop.Advapi32.ServiceOptions.SERVICE_QUERY_STATUS))
                {
                    Interop.Advapi32.SERVICE_STATUS svcStatus = default;
                    bool success = Interop.Advapi32.QueryServiceStatus(serviceHandle, &svcStatus);
                    if (!success)
                        throw new Win32Exception(Marshal.GetLastWin32Error());

                    _commandsAccepted = svcStatus.controlsAccepted;
                    _status = (ServiceControllerStatus)svcStatus.currentState;
                    _type = svcStatus.serviceType;
                    _statusGenerated = true;
                }
            }
        }

        [MemberNotNull(nameof(_name))]
        [MemberNotNull(nameof(_displayName))]
        private unsafe void GenerateNames()
        {
            GetDataBaseHandleWithConnectAccess();

            if (string.IsNullOrEmpty(_name))
            {
                // Figure out the _name based on the information we have.
                // We must either have _displayName or the constructor parameter _eitherName.
                string? userGivenName = string.IsNullOrEmpty(_eitherName) ? _displayName : _eitherName;

                if (string.IsNullOrEmpty(userGivenName))
                    throw new InvalidOperationException(SR.Format(SR.ServiceName, userGivenName, ServiceBase.MaxNameLength.ToString()));

                // Try it as a display name
                string? result = GetServiceKeyName(_serviceManagerHandle, userGivenName);

                if (result != null)
                {
                    // Now we have both
                    _name = result;
                    _displayName = userGivenName;
                    _eitherName = null;
                    return;
                }

                // Try it as a service name
                result = GetServiceDisplayName(_serviceManagerHandle, userGivenName);

                if (result == null)
                {
                    throw new InvalidOperationException(SR.Format(SR.NoService, userGivenName, _machineName), new Win32Exception(Interop.Errors.ERROR_SERVICE_DOES_NOT_EXIST));
                }

                _name = userGivenName;
                _displayName = result;
                _eitherName = null;
            }
            else if (string.IsNullOrEmpty(_displayName))
            {
                // We must have _name
                string? result = GetServiceDisplayName(_serviceManagerHandle, _name);

                if (result == null)
                {
                    throw new InvalidOperationException(SR.Format(SR.NoService, _name, _machineName), new Win32Exception(Interop.Errors.ERROR_SERVICE_DOES_NOT_EXIST));
                }

                _displayName = result;
                _eitherName = null;
            }
        }

        /// <summary>
        /// Gets service name (key name) from service display name.
        /// Returns null if service is not found.
        /// </summary>
        private unsafe string? GetServiceKeyName(SafeServiceHandle? SCMHandle, string serviceDisplayName)
        {
            var builder = new ValueStringBuilder(stackalloc char[256]);
            int bufLen;
            while (true)
            {
                bufLen = builder.Capacity;
                fixed (char* c = builder)
                {
                    if (Interop.Advapi32.GetServiceKeyName(SCMHandle, serviceDisplayName, c, ref bufLen))
                        break;
                }

                int lastError = Marshal.GetLastWin32Error();
                if (lastError == Interop.Errors.ERROR_SERVICE_DOES_NOT_EXIST)
                {
                    return null;
                }
                else if (lastError != Interop.Errors.ERROR_INSUFFICIENT_BUFFER)
                {
                    throw new InvalidOperationException(SR.Format(SR.NoService, serviceDisplayName, _machineName), new Win32Exception(lastError));
                }

                builder.EnsureCapacity(bufLen + 1); // Does not include null
            }

            builder.Length = bufLen;
            return builder.ToString();
        }

        private unsafe string? GetServiceDisplayName(SafeServiceHandle? SCMHandle, string serviceName)
        {
            var builder = new ValueStringBuilder(4096);
            int bufLen;
            while (true)
            {
                bufLen = builder.Capacity;
                fixed (char* c = builder)
                {
                    if (Interop.Advapi32.GetServiceDisplayName(SCMHandle, serviceName, c, ref bufLen))
                        break;
                }

                int lastError = Marshal.GetLastWin32Error();
                if (lastError == Interop.Errors.ERROR_SERVICE_DOES_NOT_EXIST)
                {
                    return null;
                }
                else if (lastError != Interop.Errors.ERROR_INSUFFICIENT_BUFFER)
                {
                    throw new InvalidOperationException(SR.Format(SR.NoService, serviceName, _machineName), new Win32Exception(lastError));
                }

                builder.EnsureCapacity(bufLen + 1); // Does not include null
            }

            builder.Length = bufLen;
            return builder.ToString();
        }

        private static SafeServiceHandle GetDataBaseHandleWithAccess(string machineName, int serviceControlManagerAccess)
        {
            SafeServiceHandle? databaseHandle = null;
            if (machineName.Equals(DefaultMachineName) || machineName.Length == 0)
            {
                databaseHandle = new SafeServiceHandle(Interop.Advapi32.OpenSCManager(null, null, serviceControlManagerAccess));
            }
            else
            {
                databaseHandle = new SafeServiceHandle(Interop.Advapi32.OpenSCManager(machineName, null, serviceControlManagerAccess));
            }

            if (databaseHandle.IsInvalid)
            {
                Exception inner = new Win32Exception(Marshal.GetLastWin32Error());
                throw new InvalidOperationException(SR.Format(SR.OpenSC, machineName), inner);
            }

            return databaseHandle;
        }

        private void GetDataBaseHandleWithConnectAccess()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }

            // get a handle to SCM with connect access and store it in serviceManagerHandle field.
            if (_serviceManagerHandle == null)
            {
                _serviceManagerHandle = GetDataBaseHandleWithAccess(_machineName, Interop.Advapi32.ServiceControllerOptions.SC_MANAGER_CONNECT);
            }
        }

        /// Gets all the device-driver services on the local machine.
        public static ServiceController[] GetDevices()
        {
            return GetDevices(DefaultMachineName);
        }

        /// Gets all the device-driver services in the machine specified.
        public static ServiceController[] GetDevices(string machineName)
        {
            return GetServicesOfType(machineName, Interop.Advapi32.ServiceTypeOptions.SERVICE_TYPE_DRIVER);
        }

        /// Opens a handle for the current service. The handle must be Dispose()'d.
        private SafeServiceHandle GetServiceHandle(int desiredAccess)
        {
            GetDataBaseHandleWithConnectAccess();

            var serviceHandle = new SafeServiceHandle(Interop.Advapi32.OpenService(_serviceManagerHandle, ServiceName, desiredAccess));
            if (serviceHandle.IsInvalid)
            {
                Exception inner = new Win32Exception(Marshal.GetLastWin32Error());
                throw new InvalidOperationException(SR.Format(SR.OpenService, ServiceName, _machineName), inner);
            }

            return serviceHandle;
        }

        /// Gets the services (not including device-driver services) on the local machine.
        public static ServiceController[] GetServices()
        {
            return GetServices(DefaultMachineName);
        }

        /// Gets the services (not including device-driver services) on the machine specified.
        public static ServiceController[] GetServices(string machineName)
        {
            return GetServicesOfType(machineName, Interop.Advapi32.ServiceTypeOptions.SERVICE_TYPE_WIN32);
        }

        /// Helper function for ServicesDependedOn.
        private static Interop.Advapi32.ENUM_SERVICE_STATUS_PROCESS[] GetServicesInGroup(string machineName, string group)
        {
            return GetServices<Interop.Advapi32.ENUM_SERVICE_STATUS_PROCESS>(machineName, Interop.Advapi32.ServiceTypeOptions.SERVICE_TYPE_WIN32, group, status => { return status; });
        }

        /// Helper function for GetDevices and GetServices.
        private static ServiceController[] GetServicesOfType(string machineName, int serviceType)
        {
            if (!CheckMachineName(machineName))
                throw new ArgumentException(SR.Format(SR.BadMachineName, machineName));

            return GetServices<ServiceController>(machineName, serviceType, null, status => { return new ServiceController(machineName, status); });
        }

        /// Helper for GetDevices, GetServices, and ServicesDependedOn
        private static T[] GetServices<T>(string machineName, int serviceType, string? group, Func<Interop.Advapi32.ENUM_SERVICE_STATUS_PROCESS, T> selector)
        {
            int bytesNeeded;
            int servicesReturned;
            int resumeHandle = 0;

            T[] services;

            using (SafeServiceHandle databaseHandle = GetDataBaseHandleWithAccess(machineName, Interop.Advapi32.ServiceControllerOptions.SC_MANAGER_ENUMERATE_SERVICE))
            {
                Interop.Advapi32.EnumServicesStatusEx(
                    databaseHandle,
                    Interop.Advapi32.ServiceControllerOptions.SC_ENUM_PROCESS_INFO,
                    serviceType,
                    Interop.Advapi32.StatusOptions.STATUS_ALL,
                    IntPtr.Zero,
                    0,
                    out bytesNeeded,
                    out servicesReturned,
                    ref resumeHandle,
                    group);

                IntPtr memory = Marshal.AllocHGlobal((IntPtr)bytesNeeded);
                try
                {
                    //
                    // Get the set of services
                    //
                    Interop.Advapi32.EnumServicesStatusEx(
                        databaseHandle,
                        Interop.Advapi32.ServiceControllerOptions.SC_ENUM_PROCESS_INFO,
                        serviceType,
                        Interop.Advapi32.StatusOptions.STATUS_ALL,
                        memory,
                        bytesNeeded,
                        out bytesNeeded,
                        out servicesReturned,
                        ref resumeHandle,
                        group);

                    //
                    // Go through the block of memory it returned to us and select the results
                    //
                    services = new T[servicesReturned];
                    for (int i = 0; i < servicesReturned; i++)
                    {
                        IntPtr structPtr = (IntPtr)((long)memory + (i * Marshal.SizeOf<Interop.Advapi32.ENUM_SERVICE_STATUS_PROCESS>()));
                        Interop.Advapi32.ENUM_SERVICE_STATUS_PROCESS status = new Interop.Advapi32.ENUM_SERVICE_STATUS_PROCESS();
                        Marshal.PtrToStructure(structPtr, status);
                        services[i] = selector(status);
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(memory);
                }
            }

            return services;
        }

        /// Suspends a service's operation.
        public unsafe void Pause()
        {
            using (var serviceHandle = GetServiceHandle(Interop.Advapi32.ServiceOptions.SERVICE_PAUSE_CONTINUE))
            {
                Interop.Advapi32.SERVICE_STATUS status = default;
                bool result = Interop.Advapi32.ControlService(serviceHandle, Interop.Advapi32.ControlOptions.CONTROL_PAUSE, &status);
                if (!result)
                {
                    Exception inner = new Win32Exception(Marshal.GetLastWin32Error());
                    throw new InvalidOperationException(SR.Format(SR.PauseService, ServiceName, _machineName), inner);
                }
            }
        }

        /// Continues a service after it has been paused.
        public unsafe void Continue()
        {
            using (var serviceHandle = GetServiceHandle(Interop.Advapi32.ServiceOptions.SERVICE_PAUSE_CONTINUE))
            {
                Interop.Advapi32.SERVICE_STATUS status = default;
                bool result = Interop.Advapi32.ControlService(serviceHandle, Interop.Advapi32.ControlOptions.CONTROL_CONTINUE, &status);
                if (!result)
                {
                    Exception inner = new Win32Exception(Marshal.GetLastWin32Error());
                    throw new InvalidOperationException(SR.Format(SR.ResumeService, ServiceName, _machineName), inner);
                }
            }
        }

        public unsafe void ExecuteCommand(int command)
        {
            using (var serviceHandle = GetServiceHandle(Interop.Advapi32.ServiceOptions.SERVICE_USER_DEFINED_CONTROL))
            {
                Interop.Advapi32.SERVICE_STATUS status = default;
                bool result = Interop.Advapi32.ControlService(serviceHandle, command, &status);
                if (!result)
                {
                    Exception inner = new Win32Exception(Marshal.GetLastWin32Error());
                    throw new InvalidOperationException(SR.Format(SR.ControlService, ServiceName, MachineName), inner);
                }
            }
        }

        /// Refreshes all property values.
        public void Refresh()
        {
            _statusGenerated = false;
            _startTypeInitialized = false;
            _dependentServices = null;
            _servicesDependedOn = null;
        }

        /// Starts the service.
        public void Start()
        {
            Start(Array.Empty<string>());
        }

        /// Starts a service in the machine specified.
        public void Start(string[] args)
        {
            if (args == null)
                throw new ArgumentNullException(nameof(args));

            using (SafeServiceHandle serviceHandle = GetServiceHandle(Interop.Advapi32.ServiceOptions.SERVICE_START))
            {
                IntPtr[] argPtrs = new IntPtr[args.Length];
                int i = 0;
                try
                {
                    for (i = 0; i < args.Length; i++)
                    {
                        if (args[i] == null)
                            throw new ArgumentNullException($"{nameof(args)}[{i}]", SR.ArgsCantBeNull);

                        argPtrs[i] = Marshal.StringToHGlobalUni(args[i]);
                    }
                }
                catch
                {
                    for (int j = 0; j < i; j++)
                        Marshal.FreeHGlobal(argPtrs[i]);
                    throw;
                }

                GCHandle argPtrsHandle = default;
                try
                {
                    argPtrsHandle = GCHandle.Alloc(argPtrs, GCHandleType.Pinned);
                    bool result = Interop.Advapi32.StartService(serviceHandle, args.Length, (IntPtr)argPtrsHandle.AddrOfPinnedObject());
                    if (!result)
                    {
                        Exception inner = new Win32Exception(Marshal.GetLastWin32Error());
                        throw new InvalidOperationException(SR.Format(SR.CannotStart, ServiceName, _machineName), inner);
                    }
                }
                finally
                {
                    for (i = 0; i < args.Length; i++)
                        Marshal.FreeHGlobal(argPtrs[i]);
                    if (argPtrsHandle.IsAllocated)
                        argPtrsHandle.Free();
                }
            }
        }

        /// Stops the service. If any other services depend on this one for operation,
        /// they will be stopped first. The DependentServices property lists this set
        /// of services.
        public unsafe void Stop()
        {
            using (SafeServiceHandle serviceHandle = GetServiceHandle(Interop.Advapi32.ServiceOptions.SERVICE_STOP))
            {
                // Before stopping this service, stop all the dependent services that are running.
                // (It's OK not to cache the result of getting the DependentServices property because it caches on its own.)
                for (int i = 0; i < DependentServices.Length; i++)
                {
                    ServiceController currentDependent = DependentServices[i];
                    currentDependent.Refresh();
                    if (currentDependent.Status != ServiceControllerStatus.Stopped)
                    {
                        currentDependent.Stop();
                        currentDependent.WaitForStatus(ServiceControllerStatus.Stopped, new TimeSpan(0, 0, 30));
                    }
                }

                Interop.Advapi32.SERVICE_STATUS status = default;
                bool result = Interop.Advapi32.ControlService(serviceHandle, Interop.Advapi32.ControlOptions.CONTROL_STOP, &status);
                if (!result)
                {
                    Exception inner = new Win32Exception(Marshal.GetLastWin32Error());
                    throw new InvalidOperationException(SR.Format(SR.StopService, ServiceName, _machineName), inner);
                }
            }
        }

        /// Waits infinitely until the service has reached the given status.
        public void WaitForStatus(ServiceControllerStatus desiredStatus)
        {
            WaitForStatus(desiredStatus, TimeSpan.MaxValue);
        }

        /// Waits until the service has reached the given status or until the specified time
        /// has expired
        public void WaitForStatus(ServiceControllerStatus desiredStatus, TimeSpan timeout)
        {
            if (!Enum.IsDefined(typeof(ServiceControllerStatus), desiredStatus))
                throw new ArgumentException(SR.Format(SR.InvalidEnumArgument, nameof(desiredStatus), (int)desiredStatus, typeof(ServiceControllerStatus)));

            DateTime start = DateTime.UtcNow;
            Refresh();
            while (Status != desiredStatus)
            {
                if (DateTime.UtcNow - start > timeout)
                    throw new System.ServiceProcess.TimeoutException(SR.Format(SR.Timeout, ServiceName));

                _waitForStatusSignal.WaitOne(250);
                Refresh();
            }
        }
    }
}
