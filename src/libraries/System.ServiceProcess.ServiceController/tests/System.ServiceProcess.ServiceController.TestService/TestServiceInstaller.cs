// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Text;
using System.Runtime.InteropServices;

namespace System.ServiceProcess.Tests
{
    public class TestServiceInstaller
    {
        public const string LocalServiceName = "NT AUTHORITY\\LocalService";

        private string _removalStack;

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

        public unsafe void Install()
        {
            string username = Username;
            string password = Password;

            if (string.IsNullOrEmpty(username))
            {
                username = LocalServiceName;
            }

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

            //Build servicesDependedOn string
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
            IntPtr serviceManagerHandle = Interop.Advapi32.OpenSCManager(null, null, Interop.Advapi32.ServiceControllerOptions.SC_MANAGER_ALL);
            IntPtr serviceHandle = IntPtr.Zero;
            if (serviceManagerHandle == IntPtr.Zero)
                throw new InvalidOperationException("Cannot open Service Control Manager");

            try
            {
                // Install the service
                serviceHandle = Interop.Advapi32.CreateService(serviceManagerHandle, ServiceName,
                    DisplayName, Interop.Advapi32.ServiceAccessOptions.ACCESS_TYPE_ALL, Interop.Advapi32.ServiceTypeOptions.SERVICE_TYPE_WIN32_OWN_PROCESS,
                    (int)StartType, Interop.Advapi32.ServiceStartErrorModes.ERROR_CONTROL_NORMAL,
                    ServiceCommandLine, null, IntPtr.Zero, servicesDependedOn, username, password);

                if (serviceHandle == IntPtr.Zero)
                    throw new Win32Exception();

                // A local variable in an unsafe method is already fixed -- so we don't need a "fixed { }" blocks to protect
                // across the p/invoke calls below.

                if (Description.Length != 0)
                {
                    Interop.Advapi32.SERVICE_DESCRIPTION serviceDesc = new Interop.Advapi32.SERVICE_DESCRIPTION();
                    serviceDesc.description = Marshal.StringToHGlobalUni(Description);
                    bool success = Interop.Advapi32.ChangeServiceConfig2(serviceHandle, Interop.Advapi32.ServiceConfigOptions.SERVICE_CONFIG_DESCRIPTION, ref serviceDesc);
                    Marshal.FreeHGlobal(serviceDesc.description);
                    if (!success)
                        throw new Win32Exception();
                }

                // Start the service after creating it
                using (ServiceController svc = new ServiceController(ServiceName))
                {
                    if (svc.Status != ServiceControllerStatus.Running)
                    {
                        svc.Start();
                        svc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(30));
                    }
                }
            }
            finally
            {
                if (serviceHandle != IntPtr.Zero)
                    Interop.Advapi32.CloseServiceHandle(serviceHandle);

                Interop.Advapi32.CloseServiceHandle(serviceManagerHandle);
            }
        }

        public void RemoveService()
        {
            if (ServiceName == null)
                throw new InvalidOperationException($"Already removed service at stack ${_removalStack}");

            // Store the stack for logging in case we're called twice
            try
            {
                throw new Exception();
            }
            catch (Exception e)
            {
                _removalStack = e.StackTrace;
            }

            // Stop the service
            using (ServiceController svc = new ServiceController(ServiceName))
            {
                // The Service exists at this point, but OpenService is failing, possibly because its being invoked concurrently for another service.
                // https://github.com/dotnet/corefx/issues/23388 
                if (svc.Status != ServiceControllerStatus.Stopped)
                {
                    try
                    {
                        svc.Stop();
                    }
                    catch (InvalidOperationException)
                    {
                        ServiceName = null;
                        return;
                    }

                    svc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30));
                }
            }

            IntPtr serviceManagerHandle = Interop.Advapi32.OpenSCManager(null, null, Interop.Advapi32.ServiceControllerOptions.SC_MANAGER_ALL);
            if (serviceManagerHandle == IntPtr.Zero)
                throw new Win32Exception();

            IntPtr serviceHandle = IntPtr.Zero;
            try
            {
                serviceHandle = Interop.Advapi32.OpenService(serviceManagerHandle,
                    ServiceName, Interop.Advapi32.ServiceOptions.STANDARD_RIGHTS_DELETE);

                if (serviceHandle == IntPtr.Zero)
                    throw new Win32Exception();

                if (!Interop.Advapi32.DeleteService(serviceHandle))
                    throw new Win32Exception();
            }
            finally
            {
                if (serviceHandle != IntPtr.Zero)
                    Interop.Advapi32.CloseServiceHandle(serviceHandle);

                Interop.Advapi32.CloseServiceHandle(serviceManagerHandle);
            }

            ServiceName = null;
        }
    }
}