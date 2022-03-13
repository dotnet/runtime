// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Runtime.InteropServices;

namespace System.ServiceProcess.Tests
{
    public class TestServiceInstaller
    {
        public TestServiceInstaller()
        {
        }

        public string DisplayName { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string[] ServicesDependedOn { get; set; } = Array.Empty<string>();

        public string ServiceName { get; set; } = string.Empty;

        public ServiceStartMode StartType { get; set; } = ServiceStartMode.Manual;

        public string Username { get; set; }

        public string Password { get; set; }

        public string ServiceCommandLine { get; set; }

        // Install and start the test service, after starting any prerequisite services it depends on
        public unsafe void Install()
        {
            string username = Username;
            string password = Password;

            if (ServiceCommandLine == null)
            {
                string processName = Process.GetCurrentProcess().MainModule.FileName;
                string entryPointName = System.Reflection.Assembly.GetEntryAssembly().Location;
                string arguments = ServiceName;

                // if process and entry point aren't the same then we are running hosted so pass
                // in the entrypoint as the first argument
                if (!string.Equals(processName, entryPointName, StringComparison.OrdinalIgnoreCase))
                {
                    arguments = $"\"{entryPointName}\" {arguments}";
                }

                ServiceCommandLine = $"\"{processName}\" {arguments}";
            }

            // Build servicesDependedOn string
            // These are prerequisite services that must be started before this service
            string servicesDependedOn = null;
            if (ServicesDependedOn.Length > 0)
            {
                StringBuilder buff = new StringBuilder();
                for (int i = 0; i < ServicesDependedOn.Length; ++i)
                {
                    //The servicesDependedOn need to be separated by a null
                    buff.Append(ServicesDependedOn[i]);
                    buff.Append('\0');
                }
                // an extra null at the end indicates end of list.
                buff.Append('\0');

                servicesDependedOn = buff.ToString();
            }

            // Open the service manager
            using (var serviceManagerHandle = new SafeServiceHandle(Interop.Advapi32.OpenSCManager(null, null, Interop.Advapi32.ServiceControllerOptions.SC_MANAGER_ALL)))
            {
                if (serviceManagerHandle.IsInvalid)
                    throw new InvalidOperationException("Cannot open Service Control Manager");

                TestService.DebugTrace($"TestServiceInstaller: creating service {ServiceName} with prerequisite services {servicesDependedOn}");

                // Install the service
                using (var serviceHandle = new SafeServiceHandle(Interop.Advapi32.CreateService(serviceManagerHandle, ServiceName,
                    DisplayName, Interop.Advapi32.ServiceAccessOptions.ACCESS_TYPE_ALL, Interop.Advapi32.ServiceTypeOptions.SERVICE_TYPE_WIN32_OWN_PROCESS,
                    (int)StartType, Interop.Advapi32.ServiceStartErrorModes.ERROR_CONTROL_NORMAL,
                    ServiceCommandLine, null, IntPtr.Zero, servicesDependedOn, username, password)))
                {
                    if (serviceHandle.IsInvalid)
                        throw new Win32Exception("Cannot create service");

                    // A local variable in an unsafe method is already fixed -- so we don't need a "fixed { }" blocks to protect
                    // across the p/invoke calls below.

                    if (Description.Length != 0)
                    {
                        Interop.Advapi32.SERVICE_DESCRIPTION serviceDesc = new Interop.Advapi32.SERVICE_DESCRIPTION();
                        serviceDesc.description = Marshal.StringToHGlobalUni(Description);
                        bool success = Interop.Advapi32.ChangeServiceConfig2(serviceHandle, Interop.Advapi32.ServiceConfigOptions.SERVICE_CONFIG_DESCRIPTION, ref serviceDesc);
                        Marshal.FreeHGlobal(serviceDesc.description);
                        if (!success)
                            throw new Win32Exception("Cannot set description");
                    }

                    // Start the service after creating it
                    using (ServiceController svc = new ServiceController(ServiceName))
                    {
                        if (svc.Status != ServiceControllerStatus.Running)
                        {
                            TestService.DebugTrace($"TestServiceInstaller: instructing ServiceController to start service {ServiceName}");
                            svc.Start();
                            if (!ServiceName.StartsWith("PropagateExceptionFromOnStart"))
                                svc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(120));
                        }
                        else
                        {
                            TestService.DebugTrace("TestServiceInstaller: service {ServiceName} already running");
                        }
                    }
                }
            }
        }

        public void RemoveService()
        {
            try
            {
                StopService();
            }
            finally
            {
                // If the service didn't stop promptly, we will get a TimeoutException.
                // This means the test service has gotten "jammed".
                // Meantime we still want this service to get deleted, so we'll go ahead and call
                // DeleteService, which will schedule it to get deleted on reboot.
                // We won't catch the exception: we do want the test to fail.
                DeleteService();

                ServiceName = null;
            }
        }

        private void StopService()
        {
            using (ServiceController svc = new ServiceController(ServiceName))
            {
                // The Service exists at this point, but OpenService is failing, possibly because its being invoked concurrently for another service.
                // https://github.com/dotnet/runtime/issues/23247
                if (svc.Status != ServiceControllerStatus.Stopped)
                {
                    try
                    {
                        TestService.DebugTrace("TestServiceInstaller: instructing ServiceController to Stop service " + ServiceName);
                        svc.Stop();
                    }
                    catch (InvalidOperationException)
                    {
                        // Already stopped
                        return;
                    }

                    // var sw = Stopwatch.StartNew();
                    svc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(120));
                    // sw.Stop();
                    // if (sw.Elapsed > TimeSpan.FromSeconds(30))
                    // {
                    //    Console.WriteLine($"Took unexpectedly long to stop a service: {sw.Elapsed.TotalSeconds}");
                    // }
                }
            }
        }

        private void DeleteService()
        {
            using (var serviceManagerHandle = new SafeServiceHandle(Interop.Advapi32.OpenSCManager(null, null, Interop.Advapi32.ServiceControllerOptions.SC_MANAGER_ALL)))
            {
                if (serviceManagerHandle.IsInvalid)
                    throw new Win32Exception("Could not open SCM");

                using (var serviceHandle = new SafeServiceHandle(Interop.Advapi32.OpenService(serviceManagerHandle, ServiceName, Interop.Advapi32.ServiceOptions.STANDARD_RIGHTS_DELETE)))
                {
                    if (serviceHandle.IsInvalid)
                        throw new Win32Exception($"Could not find service '{ServiceName}'");

                    TestService.DebugTrace("TestServiceInstaller: instructing ServiceController to Delete service " + ServiceName);
                    if (!Interop.Advapi32.DeleteService(serviceHandle))
                    {
                        throw new Win32Exception($"Could not delete service '{ServiceName}'");
                    }
                }
            }
        }
    }
}
