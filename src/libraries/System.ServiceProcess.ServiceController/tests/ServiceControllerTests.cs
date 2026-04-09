// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.ServiceProcess.Tests
{
    [OuterLoop(/* Modifies machine state */)]
    [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Persistent issues starting test service on NETFX")]
    public partial class ServiceControllerTests : IDisposable
    {
        private const int connectionTimeout = 30000;
        private readonly TestServiceProvider _testService;

        private bool _disposed;

        public ServiceControllerTests()
        {
            _testService = new TestServiceProvider();
        }

        private void AssertExpectedProperties(ServiceController testServiceController)
        {
            Assert.Equal(_testService.TestServiceName, testServiceController.ServiceName, StringComparer.OrdinalIgnoreCase);
            Assert.Equal(_testService.TestServiceDisplayName, testServiceController.DisplayName);
            Assert.Equal(_testService.TestMachineName, testServiceController.MachineName);
            Assert.Equal(ServiceType.Win32OwnProcess, testServiceController.ServiceType);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsPrivilegedProcess))]
        public void ConstructWithServiceName()
        {
            var controller = new ServiceController(_testService.TestServiceName);
            AssertExpectedProperties(controller);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsPrivilegedProcess))]
        public void ConstructWithServiceName_ToUpper()
        {
            var controller = new ServiceController(_testService.TestServiceName.ToUpperInvariant());
            AssertExpectedProperties(controller);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsPrivilegedProcess))]
        public void ConstructWithDisplayName()
        {
            var controller = new ServiceController(_testService.TestServiceDisplayName);
            AssertExpectedProperties(controller);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsPrivilegedProcess))]
        public void ConstructWithMachineName()
        {
            var controller = new ServiceController(_testService.TestServiceName, _testService.TestMachineName);
            AssertExpectedProperties(controller);

            AssertExtensions.Throws<ArgumentException>(null, () => { new ServiceController(_testService.TestServiceName, ""); });
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsPrivilegedProcess))]
        public void ControlCapabilities()
        {
            var controller = new ServiceController(_testService.TestServiceName);
            controller.WaitForStatus(ServiceControllerStatus.Running, _testService.ControlTimeout);

            Assert.True(controller.CanStop);
            Assert.True(controller.CanPauseAndContinue);
            Assert.True(controller.CanShutdown);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsPrivilegedProcess))]
        public void Start_NullArg_ThrowsArgumentNullException()
        {
            var controller = new ServiceController(_testService.TestServiceName);
            Assert.Throws<ArgumentNullException>(() => controller.Start(new string[] { null } ));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsPrivilegedProcess))]
        public void StopAndStart()
        {
            var controller = new ServiceController(_testService.TestServiceName);
            controller.WaitForStatus(ServiceControllerStatus.Running, _testService.ControlTimeout);
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

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsPrivilegedProcess))]
        public void PauseAndContinue()
        {
            string serviceName = _testService.TestServiceName;
            var controller = new ServiceController(serviceName);

            controller.WaitForStatus(ServiceControllerStatus.Running, _testService.ControlTimeout);
            Assert.Equal(ServiceControllerStatus.Running, controller.Status);

            _testService.Client.Connect(connectionTimeout);
            Assert.Equal((int)PipeMessageByteCode.Connected, _testService.GetByte());

            for (int i = 0; i < 2; i++)
            {
                controller.Pause();
                Assert.Equal((int)PipeMessageByteCode.Pause, _testService.GetByte());
                controller.WaitForStatus(ServiceControllerStatus.Paused, _testService.ControlTimeout);
                Assert.Equal(ServiceControllerStatus.Paused, controller.Status);

                controller.Continue();
                Assert.Equal((int)PipeMessageByteCode.Continue, _testService.GetByte());
                controller.WaitForStatus(ServiceControllerStatus.Running, _testService.ControlTimeout);
                Assert.Equal(ServiceControllerStatus.Running, controller.Status);
            }

            controller.Stop();
            Assert.Equal((int)PipeMessageByteCode.Stop, _testService.GetByte());
            controller.WaitForStatus(ServiceControllerStatus.Stopped, _testService.ControlTimeout);
            Assert.Equal(ServiceControllerStatus.Stopped, controller.Status);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsPrivilegedProcess))]
        public void GetServices_FindSelf()
        {
            bool foundTestService = false;

            foreach (var service in ServiceController.GetServices())
            {
                if (service.ServiceName == _testService.TestServiceName)
                {
                    foundTestService = true;
                    AssertExpectedProperties(service);
                }
            }

            Assert.True(foundTestService, "Test service was not enumerated with all services");
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsPrivilegedProcess))]
        public void Dependencies()
        {
            var controller = new ServiceController(_testService.TestServiceName);
            Assert.Equal(0, controller.DependentServices.Length);
            Assert.Equal(1, controller.ServicesDependedOn.Length);

            var prerequisiteServiceController = new ServiceController(_testService.TestServiceName + ".Prerequisite");
            Assert.Equal(1, prerequisiteServiceController.DependentServices.Length);
            Assert.Equal(0, prerequisiteServiceController.ServicesDependedOn.Length);

            Assert.Equal(controller.ServicesDependedOn[0].ServiceName, prerequisiteServiceController.ServiceName);
            Assert.Equal(prerequisiteServiceController.DependentServices[0].ServiceName, controller.ServiceName);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsPrivilegedProcess))]
        public void ServicesStartMode()
        {
            var controller = new ServiceController(_testService.TestServiceName);
            Assert.Equal(ServiceStartMode.Manual, controller.StartType);

            // Check for the startType of the services that depend on the test service
            for (int i = 0; i < controller.DependentServices.Length; i++)
            {
                Assert.Equal(ServiceStartMode.Disabled, controller.DependentServices[i].StartType);
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _testService.DeleteTestServices();
                _disposed = true;
            }
        }
    }
}
