// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.ServiceProcess.Tests
{
    [OuterLoop(/* Modifies machine state */)]
    [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Persistent issues starting test service on NETFX")]
    public class ServiceControllerTests : IDisposable
    {
        private const int connectionTimeout = 30000;
        private readonly TestServiceProvider _testService;

        private static readonly Lazy<bool> s_isElevated = new Lazy<bool>(() => AdminHelpers.IsProcessElevated());
        protected static bool IsProcessElevated => s_isElevated.Value;

        private const int ExpectedDependentServiceCount = 3;
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

        [ConditionalFact(nameof(IsProcessElevated))]
        public void ConstructWithServiceName()
        {
            var controller = new ServiceController(_testService.TestServiceName);
            AssertExpectedProperties(controller);
        }

        [ConditionalFact(nameof(IsProcessElevated))]
        public void ConstructWithServiceName_ToUpper()
        {
            var controller = new ServiceController(_testService.TestServiceName.ToUpperInvariant());
            AssertExpectedProperties(controller);
        }

        [ConditionalFact(nameof(IsProcessElevated))]
        public void ConstructWithDisplayName()
        {
            var controller = new ServiceController(_testService.TestServiceDisplayName);
            AssertExpectedProperties(controller);
        }

        [ConditionalFact(nameof(IsProcessElevated))]
        public void ConstructWithMachineName()
        {
            var controller = new ServiceController(_testService.TestServiceName, _testService.TestMachineName);
            AssertExpectedProperties(controller);

            AssertExtensions.Throws<ArgumentException>(null, () => { var c = new ServiceController(_testService.TestServiceName, ""); });
        }

        [ConditionalFact(nameof(IsProcessElevated))]
        public void ControlCapabilities()
        {
            var controller = new ServiceController(_testService.TestServiceName);
            controller.WaitForStatus(ServiceControllerStatus.Running, _testService.ControlTimeout);

            Assert.True(controller.CanStop);
            Assert.True(controller.CanPauseAndContinue);
            Assert.True(controller.CanShutdown);
        }

        [ConditionalFact(nameof(IsProcessElevated))]
        public void Start_NullArg_ThrowsArgumentNullException()
        {
            var controller = new ServiceController(_testService.TestServiceName);
            Assert.Throws<ArgumentNullException>(() => controller.Start(new string[] { null } ));
        }

        [ConditionalFact(nameof(IsProcessElevated))]
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

        [ConditionalFact(nameof(IsProcessElevated))]
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

        [ConditionalFact(nameof(IsProcessElevated))]
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

        [ConditionalFact(nameof(IsProcessElevated))]
        public void Dependencies()
        {
            // The test service creates a number of dependent services, each of which is depended on
            // by all the services created after it.
            var controller = new ServiceController(_testService.TestServiceName);
            Assert.Equal(0, controller.DependentServices.Length);
            Assert.Equal(1, controller.ServicesDependedOn.Length);

            var dependentController = new ServiceController(_testService.TestServiceName + ".Dependent");
            Assert.Equal(1, dependentController.DependentServices.Length);
            Assert.Equal(0, dependentController.ServicesDependedOn.Length);

            Assert.Equal(controller.ServicesDependedOn[0].ServiceName, dependentController.ServiceName);
            Assert.Equal(dependentController.DependentServices[0].ServiceName, controller.ServiceName);
        }

        [ConditionalFact(nameof(IsProcessElevated))]
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
            if (!_disposed)
            {
                _testService.DeleteTestServices();
                _disposed = true;
            }
        }

        private static ServiceController AssertHasDependent(ServiceController controller, string serviceName, string displayName)
        {
            var dependent = FindService(controller.DependentServices, serviceName, displayName);
            Assert.NotNull(dependent);

            return dependent;
        }

        private static ServiceController AssertDependsOn(ServiceController controller, string serviceName, string displayName)
        {
            var dependency = FindService(controller.ServicesDependedOn, serviceName, displayName);
            Assert.NotNull(dependency);

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
