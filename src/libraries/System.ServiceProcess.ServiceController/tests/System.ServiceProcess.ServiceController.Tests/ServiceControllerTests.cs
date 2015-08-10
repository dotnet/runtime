// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.IO;
using System.ServiceProcess;
using System.Threading;
using Xunit;
using Microsoft.Win32;

namespace System.ServiceProcessServiceController.Tests
{
    internal sealed class ServiceProvider
    {
        public readonly string TestMachineName;
        public readonly TimeSpan ControlTimeout;
        public readonly string TestServiceName;
        public readonly string TestServiceDisplayName;
        public readonly string DependentTestServiceNamePrefix;
        public readonly string DependentTestServiceDisplayNamePrefix;
        public readonly string TestServiceRegistryKey;

        public ServiceProvider()
        {
            TestMachineName = ".";
            ControlTimeout = TimeSpan.FromSeconds(3);
            TestServiceName = Guid.NewGuid().ToString();
            TestServiceDisplayName = "Test Service " + TestServiceName;
            DependentTestServiceNamePrefix = TestServiceName + ".Dependent";
            DependentTestServiceDisplayNamePrefix = TestServiceDisplayName + ".Dependent";
            TestServiceRegistryKey = @"HKEY_USERS\.DEFAULT\dotnetTests\ServiceController\" + TestServiceName;

            // Create the service
            CreateTestServices();
        }

        private void CreateTestServices()
        {
            // Create the test service and its dependent services. Then, start the test service.
            // All control tests assume that the test service is running when they are executed.
            // So all tests should make sure to restart the service if they stop, pause, or shut
            // it down.
            RunServiceExecutable("create");
        }

        public void DeleteTestServices()
        {
            RunServiceExecutable("delete");
            RegistryKey users = Registry.Users;
            if (users.OpenSubKey(".DEFAULT\\dotnetTests") != null)
                users.DeleteSubKeyTree(".DEFAULT\\dotnetTests");
        }

        private void RunServiceExecutable(string action)
        {
            const string serviceExecutable = "System.ServiceProcess.ServiceController.TestNativeService.exe";
            var process = new Process();
            process.StartInfo.FileName = serviceExecutable;
            process.StartInfo.Arguments = string.Format("\"{0}\" \"{1}\" {2}", TestServiceName, TestServiceDisplayName, action);
            process.Start();
            process.WaitForExit();
            
            if (process.ExitCode != 0)
            {
                throw new Exception("error: " + serviceExecutable + " failed with exit code " + process.ExitCode.ToString());
            }
        }
    }

    public class ServiceControllerTests : IDisposable
    {
        private const int ExpectedDependentServiceCount = 3;

        ServiceProvider _testService; 

        public ServiceControllerTests()
        {
            _testService = new ServiceProvider();
        }

        [Fact]
        public void ConstructWithServiceName()
        {
            var controller = new ServiceController(_testService.TestServiceName);
            Assert.Equal(_testService.TestServiceName, controller.ServiceName);
            Assert.Equal(_testService.TestServiceDisplayName, controller.DisplayName);
            Assert.Equal(_testService.TestMachineName, controller.MachineName);
            Assert.Equal(ServiceType.Win32OwnProcess, controller.ServiceType);
        }

        [Fact]
        public void ConstructWithDisplayName()
        {
            var controller = new ServiceController(_testService.TestServiceDisplayName);
            Assert.Equal(_testService.TestServiceName, controller.ServiceName);
            Assert.Equal(_testService.TestServiceDisplayName, controller.DisplayName);
            Assert.Equal(_testService.TestMachineName, controller.MachineName);
            Assert.Equal(ServiceType.Win32OwnProcess, controller.ServiceType);
        }

        [Fact]
        public void ConstructWithMachineName()
        {
            var controller = new ServiceController(_testService.TestServiceName, _testService.TestMachineName);
            Assert.Equal(_testService.TestServiceName, controller.ServiceName);
            Assert.Equal(_testService.TestServiceDisplayName, controller.DisplayName);
            Assert.Equal(_testService.TestMachineName, controller.MachineName);
            Assert.Equal(ServiceType.Win32OwnProcess, controller.ServiceType);

            Assert.Throws<ArgumentException>(() => { var c = new ServiceController(_testService.TestServiceName, ""); });
        }

        [Fact]
        public void ControlCapabilities()
        {
            var controller = new ServiceController(_testService.TestServiceName);
            Assert.True(controller.CanStop);
            Assert.True(controller.CanPauseAndContinue);
            Assert.False(controller.CanShutdown);
        }

        [Fact]
        public void StartWithArguments()
        {
            var controller = new ServiceController(_testService.TestServiceName);
            Assert.Equal(ServiceControllerStatus.Running, controller.Status);

            controller.Stop();
            controller.WaitForStatus(ServiceControllerStatus.Stopped, _testService.ControlTimeout);
            Assert.Equal(ServiceControllerStatus.Stopped, controller.Status);

            var args = new[] { "a", "b", "c", "d", "e" };
            controller.Start(args);
            controller.WaitForStatus(ServiceControllerStatus.Running, _testService.ControlTimeout);
            Assert.Equal(ServiceControllerStatus.Running, controller.Status);

            // The test service writes the arguments that it was started with to the _testService.TestServiceRegistryKey.
            // Read this key to verify that the arguments were properly passed to the service.
            string argsString = Registry.GetValue(_testService.TestServiceRegistryKey, "ServiceArguments", null) as string;
            Assert.Equal(string.Join(",", args), argsString);
        }

        [Fact]
        public void StopAndStart()
        {
            var controller = new ServiceController(_testService.TestServiceName);
            Assert.Equal(ServiceControllerStatus.Running, controller.Status);

            for (int i = 0; i < 2; i++)
            {
                controller.Stop();
                controller.WaitForStatus(ServiceControllerStatus.Stopped, _testService.ControlTimeout);
                Assert.Equal(ServiceControllerStatus.Stopped, controller.Status);

                controller.Start();
                controller.WaitForStatus(ServiceControllerStatus.Running, _testService.ControlTimeout);
                Assert.Equal(ServiceControllerStatus.Running, controller.Status);
            }
        }

        [Fact]
        public void PauseAndContinue()
        {
            var controller = new ServiceController(_testService.TestServiceName);
            Assert.Equal(ServiceControllerStatus.Running, controller.Status);

            for (int i = 0; i < 2; i++)
            {
                controller.Pause();
                controller.WaitForStatus(ServiceControllerStatus.Paused, _testService.ControlTimeout);
                Assert.Equal(ServiceControllerStatus.Paused, controller.Status);

                controller.Continue();
                controller.WaitForStatus(ServiceControllerStatus.Running, _testService.ControlTimeout);
                Assert.Equal(ServiceControllerStatus.Running, controller.Status);
            }
        }

        [Fact]
        public void WaitForStatusTimeout()
        {
            var controller = new ServiceController(_testService.TestServiceName);
            Assert.Throws<System.ServiceProcess.TimeoutException>(() => controller.WaitForStatus(ServiceControllerStatus.Paused, TimeSpan.Zero));
        }

        [Fact]
        public void GetServices()
        {
            bool foundTestService = false;

            foreach (var service in ServiceController.GetServices())
            {
                if (service.ServiceName == _testService.TestServiceName)
                {
                    foundTestService = true;
                }
            }

            Assert.True(foundTestService, "Test service was not enumerated with all services");
        }

        [Fact]
        public void GetDevices()
        {
            var devices = ServiceController.GetDevices();
            Assert.True(devices.Length != 0);

            const ServiceType SERVICE_TYPE_DRIVER =
                ServiceType.FileSystemDriver |
                ServiceType.KernelDriver |
                ServiceType.RecognizerDriver;

            foreach (var device in devices)
            {
                if ((int)(device.ServiceType & SERVICE_TYPE_DRIVER) == 0)
                {
                    Assert.True(false, string.Format("Service '{0}' is of type '{1}' and is not a device driver.", device.ServiceName, device.ServiceType));
                }
            }
        }

        [Fact]
        public void Dependencies()
        {
            // The test service creates a number of dependent services, each of which is depended on
            // by all the services created after it.
            var controller = new ServiceController(_testService.TestServiceName);
            Assert.Equal(ExpectedDependentServiceCount, controller.DependentServices.Length);

            for (int i = 0; i < controller.DependentServices.Length; i++)
            {
                var dependent = AssertHasDependent(controller, _testService.DependentTestServiceNamePrefix + i, _testService.DependentTestServiceDisplayNamePrefix + i);
                Assert.Equal(ServiceType.Win32OwnProcess, dependent.ServiceType);

                // Assert that this dependent service is depended on by all the test services created after it
                Assert.Equal(ExpectedDependentServiceCount - i - 1, dependent.DependentServices.Length);

                for (int j = i + 1; j < ExpectedDependentServiceCount; j++)
                {
                    AssertHasDependent(dependent, _testService.DependentTestServiceNamePrefix + j, _testService.DependentTestServiceDisplayNamePrefix + j);
                }

                // Assert that the dependent service depends on the main test service
                AssertDependsOn(dependent, _testService.TestServiceName, _testService.TestServiceDisplayName);

                // Assert that this dependent service depends on all the test services created before it
                Assert.Equal(i + 1, dependent.ServicesDependedOn.Length);

                for (int j = i - 1; j >= 0; j--)
                {
                    AssertDependsOn(dependent, _testService.DependentTestServiceNamePrefix + j, _testService.DependentTestServiceDisplayNamePrefix + j);
                }
            }
        }

        [Fact]
        public void ServicesStartMode()
        {
            var controller = new ServiceController(_testService.TestServiceName);
            Assert.Equal(ServiceStartMode.Manual, controller.StartType);

            // Check for the startType of the dependent services.
            for (int i = 0; i < controller.DependentServices.Length; i++)
            {
                Assert.Equal(ServiceStartMode.Disabled, controller.DependentServices[i].StartType);
            }
        }

        public void Dispose()
        {
            _testService.DeleteTestServices();
        }

        private static ServiceController AssertHasDependent(ServiceController controller, string serviceName, string displayName)
        {
            var dependent = FindService(controller.DependentServices, serviceName, displayName);
            if (dependent == null)
            {
                Assert.True(false, string.Format("Expected service {0} to have dependent service {1}", controller.ServiceName, serviceName));
            }

            return dependent;
        }

        private static ServiceController AssertDependsOn(ServiceController controller, string serviceName, string displayName)
        {
            var dependency = FindService(controller.ServicesDependedOn, serviceName, displayName);
            if (dependency == null)
            {
                Assert.True(false, string.Format("Expected service {0} to depend on service {1}", controller.ServiceName, serviceName));
            }

            return dependency;
        }

        private static ServiceController FindService(ServiceController[] services, string serviceName, string displayName)
        {
            foreach (ServiceController service in services)
            {
                if (service.ServiceName == serviceName && service.DisplayName == displayName)
                {
                    return service;
                }
            }

            return null;
        }
    }
}
