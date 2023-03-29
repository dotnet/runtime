// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel;
using System.ServiceProcess;
using Microsoft.DotNet.RemoteExecutor;
using Microsoft.Win32.SafeHandles;

namespace Microsoft.Extensions.Hosting
{
    public class WindowsServiceTester : ServiceController
    {
        private WindowsServiceTester(SafeServiceHandle handle, string serviceName) : base(serviceName)
        {
            _handle = handle;
        }

        private SafeServiceHandle _handle;

        public static WindowsServiceTester Create(string serviceName, Action serviceMain)
        {
            // create remote executor commandline arguments
            string commandLine;
            using (var remoteExecutorHandle = RemoteExecutor.Invoke(serviceMain, new RemoteInvokeOptions() { Start = false }))
            {
                var startInfo = remoteExecutorHandle.Process.StartInfo;
                remoteExecutorHandle.Process.Dispose();
                remoteExecutorHandle.Process = null;
                commandLine = startInfo.FileName + " " + startInfo.Arguments;
            }

            // install the service
            using (var serviceManagerHandle = new SafeServiceHandle(Interop.Advapi32.OpenSCManager(null, null, Interop.Advapi32.ServiceControllerOptions.SC_MANAGER_ALL)))
            {
                if (serviceManagerHandle.IsInvalid)
                {
                    throw new InvalidOperationException("Cannot open Service Control Manager");
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
                    $"{nameof(WindowsServiceTester)} test service",
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
                    throw new Win32Exception("Could not create service");
                }

                return new WindowsServiceTester(serviceHandle, serviceName);
            }
        }

        internal unsafe Interop.Advapi32.SERVICE_STATUS QueryServiceStatus()
        {
            Interop.Advapi32.SERVICE_STATUS status = default;
            bool success = Interop.Advapi32.QueryServiceStatus(_handle, &status);
            if (!success)
            {
                throw new Win32Exception();
            }
            return status;
        }
        internal unsafe Interop.Advapi32.SERVICE_STATUS_PROCESS QueryServiceStatusEx()
        {
            Interop.Advapi32.SERVICE_STATUS_PROCESS status = default;
            bool success = Interop.Advapi32.QueryServiceStatusEx(_handle, &status);
            if (!success)
            {
                throw new Win32Exception();
            }
            return status;
        }

        protected override void Dispose(bool disposing)
        {
            if (!_handle.IsInvalid)
            {
                // delete the temporary test service
                Interop.Advapi32.DeleteService(_handle);
                _handle.Close();
                _handle = null;
            }
        }

    }
}
