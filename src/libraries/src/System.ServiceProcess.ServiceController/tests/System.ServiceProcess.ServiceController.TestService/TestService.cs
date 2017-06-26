// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.Diagnostics;
using System;
using System.Collections;
using System.IO;
using System.Threading;
using System.Text;
using System.Runtime.InteropServices;
using System.Globalization;

namespace System.ServiceProcess
{
    public class TestServiceInstaller
    {
        public const string NetworkServiceName = "NT AUTHORITY\\NetworkService";
        public const string LocalServiceName = "NT AUTHORITY\\LocalService";

        public TestServiceInstaller()
        {
        }

        public string DisplayName { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string[] ServicesDependedOn { get; set; } = Array.Empty<string>();

        public string ServiceName { get; set; } = string.Empty;

        public ServiceStartMode StartType { get; set; } = ServiceStartMode.Manual;

        public bool DelayedAutoStart { get; set; } = false;

        public string Username { get; set; }

        public string Password { get; set; }

        public unsafe void Install()
        {
            string username = Username;
            string password = Password;

            if (string.IsNullOrEmpty(username))
            {
                username = LocalServiceName;
            }

            string moduleFileName = System.Reflection.Assembly.GetEntryAssembly().Location;

            // Put quotas around module file name. Otherwise a service might fail to start if there is space in the path.
            // Note: Though CreateService accepts a binaryPath allowing
            // arguments for automatic services, in /assemblypath=foo,
            // foo is simply the path to the executable.
            // Therefore, it is best to quote if there are no quotes,
            // and best to not quote if there are quotes.
            if (moduleFileName.IndexOf('\"') == -1)
                moduleFileName = "\"" + moduleFileName + "\"";

            //Build servicesDependedOn string
            string servicesDependedOn = null;
            if (ServicesDependedOn.Length > 0)
            {
                StringBuilder buff = new StringBuilder();
                for (int i = 0; i < ServicesDependedOn.Length; ++i)
                {
                    // we have to build a list of the services' short names. But the user
                    // might have used long names in the ServicesDependedOn property. Try
                    // to use ServiceController's logic to get the short name.
                    string tempServiceName = ServicesDependedOn[i];
                    try
                    {
                        ServiceController svc = new ServiceController(tempServiceName, ".");
                        tempServiceName = svc.ServiceName;
                    }
                    catch
                    {
                    }
                    //The servicesDependedOn need to be separated by a null
                    buff.Append(tempServiceName);
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
                    moduleFileName, null, IntPtr.Zero, servicesDependedOn, username, password);

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

                if (Environment.OSVersion.Version.Major > 5)
                {
                    if (StartType == ServiceStartMode.Automatic)
                    {
                        Interop.Advapi32.SERVICE_DELAYED_AUTOSTART_INFO serviceDelayedInfo = new Interop.Advapi32.SERVICE_DELAYED_AUTOSTART_INFO();
                        serviceDelayedInfo.fDelayedAutostart = DelayedAutoStart;
                        bool success = Interop.Advapi32.ChangeServiceConfig2(serviceHandle, Interop.Advapi32.ServiceConfigOptions.SERVICE_CONFIG_DELAYED_AUTO_START_INFO, ref serviceDelayedInfo);
                        if (!success)
                            throw new Win32Exception();
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

        private void RemoveService()
        {
            //
            // SCUM deletes a service when the Service is stopped and there is no open handle to the Service.
            // Service will be deleted asynchrously, so it takes a while for the deletion to be complete.
            // The recoommended way to delete a Service is:
            // (a)  DeleteService/closehandle,
            // (b) Stop service & wait until it is stopped & close handle
            // (c)  Wait for 5-10 secs for the async deletion to go through.
            //
            //Context.LogMessage(Res.GetString(Res.ServiceRemoving, ServiceName));
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

                Interop.Advapi32.DeleteService(serviceHandle);
            }
            finally
            {
                if (serviceHandle != IntPtr.Zero)
                    Interop.Advapi32.CloseServiceHandle(serviceHandle);

                Interop.Advapi32.CloseServiceHandle(serviceManagerHandle);
            }
            //Context.LogMessage(Res.GetString(Res.ServiceRemoved, ServiceName));

            // Stop the service
            try
            {
                using (ServiceController svc = new ServiceController(ServiceName))
                {
                    if (svc.Status != ServiceControllerStatus.Stopped)
                    {
                        //Context.LogMessage(Res.GetString(Res.TryToStop, ServiceName));
                        svc.Stop();
                        int timeout = 10;
                        svc.Refresh();
                        while (svc.Status != ServiceControllerStatus.Stopped && timeout > 0)
                        {
                            Thread.Sleep(1000);
                            svc.Refresh();
                            timeout--;
                        }
                    }
                }
            }
            catch
            {
            }

            Thread.Sleep(5000);
        }
    }
}
