// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.ServiceProcess;
using System.Threading.Tasks;
using Microsoft.DotNet.RemoteExecutor;
using Microsoft.Win32.SafeHandles;
using Xunit;

namespace Microsoft.Extensions.Hosting
{
    public class WindowsServiceTester : ServiceController
    {
        private WindowsServiceTester(SafeServiceHandle serviceHandle, RemoteInvokeHandle remoteInvokeHandle, string serviceName) : base(serviceName)
        {
            _serviceHandle = serviceHandle;
            _remoteInvokeHandle = remoteInvokeHandle;
        }

        private SafeServiceHandle _serviceHandle;
        private RemoteInvokeHandle _remoteInvokeHandle;

        public new void Start()
        {
            Start(Array.Empty<string>());
        }

        public new void Start(string[] args)
        {
            base.Start(args);

            // get the process
            _remoteInvokeHandle.Process.Dispose();
            _remoteInvokeHandle.Process = null;

            var statusEx = QueryServiceStatusEx();
            try
            {
                _remoteInvokeHandle.Process = Process.GetProcessById(statusEx.dwProcessId);
                // fetch the process handle so that we can get the exit code later.
                var _ = _remoteInvokeHandle.Process.SafeHandle;
            }
            catch (ArgumentException)
            { }
        }

        public static TimeSpan WaitForStatusTimeout { get; set; } = TimeSpan.FromSeconds(30);

        public new void WaitForStatus(ServiceControllerStatus desiredStatus) =>
            WaitForStatus(desiredStatus, WaitForStatusTimeout);

        public new void WaitForStatus(ServiceControllerStatus desiredStatus, TimeSpan timeout)
        {
            base.WaitForStatus(desiredStatus, timeout);

            Assert.Equal(Status, desiredStatus);
        }

        // the following overloads are necessary to ensure the compiler will produce the correct signature from a lambda.
        public static WindowsServiceTester Create(Func<Task> serviceMain, [CallerMemberName] string serviceName = null) => Create(RemoteExecutor.Invoke(serviceMain, remoteInvokeOptions), serviceName);
        
        public static WindowsServiceTester Create(Func<Task<int>> serviceMain, [CallerMemberName] string serviceName = null) => Create(RemoteExecutor.Invoke(serviceMain, remoteInvokeOptions), serviceName);

        public static WindowsServiceTester Create(Func<int> serviceMain, [CallerMemberName] string serviceName = null) => Create(RemoteExecutor.Invoke(serviceMain, remoteInvokeOptions), serviceName);
        
        public static WindowsServiceTester Create(Action serviceMain, [CallerMemberName] string serviceName = null) => Create(RemoteExecutor.Invoke(serviceMain, remoteInvokeOptions), serviceName);

        private static RemoteInvokeOptions remoteInvokeOptions = new RemoteInvokeOptions() { Start = false };

        private static WindowsServiceTester Create(RemoteInvokeHandle remoteInvokeHandle, string serviceName)
        {
            // create remote executor commandline arguments
            var startInfo = remoteInvokeHandle.Process.StartInfo;
            string commandLine = startInfo.FileName + " " + startInfo.Arguments;

            // install the service
            using (var serviceManagerHandle = new SafeServiceHandle(Interop.Advapi32.OpenSCManager(null, null, Interop.Advapi32.ServiceControllerOptions.SC_MANAGER_ALL)))
            {
                if (serviceManagerHandle.IsInvalid)
                {
                    throw new InvalidOperationException();
                }

                // delete existing service if it exists
                using (var existingServiceHandle = new SafeServiceHandle(Interop.Advapi32.OpenService(serviceManagerHandle, serviceName, Interop.Advapi32.ServiceAccessOptions.ACCESS_TYPE_ALL)))
                {
                    if (!existingServiceHandle.IsInvalid)
                    {
                        Interop.Advapi32.DeleteService(existingServiceHandle);
                    }
                }

                var serviceHandle = new SafeServiceHandle(
                    Interop.Advapi32.CreateService(serviceManagerHandle,
                    serviceName,
                    $"{nameof(WindowsServiceTester)} {serviceName} test service",
                    Interop.Advapi32.ServiceAccessOptions.ACCESS_TYPE_ALL,
                    Interop.Advapi32.ServiceTypeOptions.SERVICE_WIN32_OWN_PROCESS,
                    (int)ServiceStartMode.Manual,
                    Interop.Advapi32.ServiceStartErrorModes.ERROR_CONTROL_NORMAL,
                    commandLine,
                    loadOrderGroup: null,
                    pTagId: IntPtr.Zero,
                    dependencies: null,
                    servicesStartName: null,
                    password: null));

                if (serviceHandle.IsInvalid)
                {
                    throw new Win32Exception();
                }

                return new WindowsServiceTester(serviceHandle, remoteInvokeHandle, serviceName);
            }
        }

        internal unsafe Interop.Advapi32.SERVICE_STATUS QueryServiceStatus()
        {
            Interop.Advapi32.SERVICE_STATUS status = default;
            bool success = Interop.Advapi32.QueryServiceStatus(_serviceHandle, &status);
            if (!success)
            {
                throw new Win32Exception();
            }
            return status;
        }

        internal unsafe Interop.Advapi32.SERVICE_STATUS_PROCESS QueryServiceStatusEx()
        {
            Interop.Advapi32.SERVICE_STATUS_PROCESS status = default;
            bool success = Interop.Advapi32.QueryServiceStatusEx(_serviceHandle, &status);
            if (!success)
            {
                throw new Win32Exception();
            }
            return status;
        }

        protected override void Dispose(bool disposing)
        {
            if (_remoteInvokeHandle != null)
            {
                _remoteInvokeHandle.Dispose();               
            }

            if (!_serviceHandle.IsInvalid)
            {
                // delete the temporary test service
                Interop.Advapi32.DeleteService(_serviceHandle);
                _serviceHandle.Close();
            }
        }
    }
}
