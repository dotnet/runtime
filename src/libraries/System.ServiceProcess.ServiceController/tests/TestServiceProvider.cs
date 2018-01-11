// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Security.Principal;
using Xunit;
using System.IO;
using System.Threading;

namespace System.ServiceProcess.Tests
{
    internal sealed class TestServiceProvider
    {
        private static readonly Lazy<bool> s_runningWithElevatedPrivileges = new Lazy<bool>(
            () => new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator));

        public static bool RunningWithElevatedPrivileges
        {
            get { return s_runningWithElevatedPrivileges.Value; }
        }

        public readonly string TestServiceAssembly = typeof(TestService).Assembly.Location;
        public readonly string TestMachineName;
        public readonly TimeSpan ControlTimeout;
        public readonly string TestServiceName;
        public readonly string TestServiceDisplayName;

        private readonly TestServiceProvider _dependentServices;
        public TestServiceProvider()
        {
            TestMachineName = ".";
            ControlTimeout = TimeSpan.FromSeconds(120);
            TestServiceName = Guid.NewGuid().ToString();
            TestServiceDisplayName = "Test Service " + TestServiceName;

            _dependentServices = new TestServiceProvider(TestServiceName + ".Dependent");

            // Create the service
            CreateTestServices();
        }

        public TestServiceProvider(string serviceName)
        {
            TestMachineName = ".";
            ControlTimeout = TimeSpan.FromSeconds(120);
            TestServiceName = serviceName;
            TestServiceDisplayName = "Test Service " + TestServiceName;

            // Create the service
            CreateTestServices();
        }

        private void CreateTestServices()
        {
            TestServiceInstaller testServiceInstaller = new TestServiceInstaller();

            testServiceInstaller.ServiceName = TestServiceName;
            testServiceInstaller.DisplayName = TestServiceDisplayName;

            if (_dependentServices != null)
            {
                testServiceInstaller.ServicesDependedOn = new string[] { _dependentServices.TestServiceName };
            }

            var comparer = PlatformDetection.IsFullFramework ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal; // Full framework upper cases the name
            string processName = Process.GetCurrentProcess().MainModule.FileName;
            string entryPointName = typeof(TestService).Assembly.Location;
            string arguments = TestServiceName;

            // if process and entry point aren't the same then we are running hosted so pass
            // in the entrypoint as the first argument
            if (!PlatformDetection.IsFullFramework)
            {
                arguments = $"\"{entryPointName}\" {arguments}";
            }
            else
            {
                processName = entryPointName;
            }

            testServiceInstaller.ServiceCommandLine = $"\"{processName}\" {arguments}";

            testServiceInstaller.Install();
        }

        public void DeleteTestServices()
        {
            try
            {
                TestServiceInstaller testServiceInstaller = new TestServiceInstaller();
                testServiceInstaller.ServiceName = TestServiceName;
                testServiceInstaller.RemoveService();

                if (File.Exists(LogPath))
                {
                    try
                    {
                        File.Delete(LogPath);
                    }
                    catch (IOException)
                    {
                        // Don't fail simply because the service was not fully cleaned up
                        // and is still holding a handle to the log file
                    }
                }
            }
            finally
            {
                // Lets be sure to try and clean up dependenct services even if something goes
                // wrong with the full removal of the other service.
                if (_dependentServices != null)
                {
                    _dependentServices.DeleteTestServices();
                }
            }
        }

        private string LogPath => TestService.GetLogPath(TestServiceName);

        public string GetServiceOutput()
        {
            // Need to open with FileShare.ReadWrite because we expect the service still has it open for write
            using (StreamReader reader = new StreamReader(File.Open(LogPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
            {
                return reader.ReadToEnd();
            }
        }
    }
}
