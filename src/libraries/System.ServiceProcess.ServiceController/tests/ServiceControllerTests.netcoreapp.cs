// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.ServiceProcess.Tests
{
    [OuterLoop(/* Modifies machine state */)]
    public partial class ServiceControllerTests : IDisposable
    {
        [ConditionalFact(nameof(IsProcessElevated))]
        public void Stop_FalseArg_WithDependentServices_ThrowsInvalidOperationException()
        {
            var controller = new ServiceController(_testService.TestServiceName);
            Assert.Equal(0, controller.DependentServices.Length);
            Assert.Equal(1, controller.ServicesDependedOn.Length);

            var prerequisiteServiceController = new ServiceController(_testService.TestServiceName + ".Prerequisite");
            Assert.Equal(1, prerequisiteServiceController.DependentServices.Length);
            Assert.Equal(0, prerequisiteServiceController.ServicesDependedOn.Length);

            prerequisiteServiceController.WaitForStatus(ServiceControllerStatus.Running, _testService.ControlTimeout);
            Assert.Throws<InvalidOperationException>(() => prerequisiteServiceController.Stop(stopDependentServices: false));
        }

        [ConditionalFact(nameof(IsProcessElevated))]
        public void Stop_TrueArg_WithDependentServices_StopsTheServiceAndItsDependents()
        {
            var controller = new ServiceController(_testService.TestServiceName);
            controller.WaitForStatus(ServiceControllerStatus.Running, _testService.ControlTimeout);

            controller.Stop(stopDependentServices: true);
            controller.WaitForStatus(ServiceControllerStatus.Stopped, _testService.ControlTimeout);

            Assert.Equal(ServiceControllerStatus.Stopped, controller.Status);
            Assert.All(controller.DependentServices, service => Assert.Equal(ServiceControllerStatus.Stopped, service.Status));
        }

        [ConditionalFact(nameof(IsProcessElevated))]
        public void StopTheServiceAndItsDependentsManually()
        {
            var controller = new ServiceController(_testService.TestServiceName);
            controller.WaitForStatus(ServiceControllerStatus.Running, _testService.ControlTimeout);

            // stop the services that depend on this service
            foreach (var dependentService in controller.DependentServices)
            {
                dependentService.Stop(stopDependentServices: false);
                dependentService.WaitForStatus(ServiceControllerStatus.Stopped, _testService.ControlTimeout);
            }
            controller.Stop(stopDependentServices: false);
            controller.WaitForStatus(ServiceControllerStatus.Stopped, _testService.ControlTimeout);

            Assert.Equal(ServiceControllerStatus.Stopped, controller.Status);
            Assert.All(controller.DependentServices, service => Assert.Equal(ServiceControllerStatus.Stopped, service.Status));
        }
    }
}
