// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using Xunit;

namespace System.ServiceProcess.Tests
{
    public static class SafeServiceControllerTests
    {
        private const string KeyIsoSvcName = "KEYISO";

        [ActiveIssue("https://github.com/dotnet/runtime/issues/21463")]
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))] // https://github.com/dotnet/runtime/issues/21463
        public static void GetServices()
        {
            bool foundKeyIsoSvc = false;
            bool foundSamSvc = false;
            bool foundOtherSvc = false;

            foreach (var service in ServiceController.GetServices())
            {
                // The CNG Key Isolation service (KeyIso) and Security Accounts Manager (SAM) service (SamSs)
                // are both system-critical, so they can be counted on to be registered and running.
                // This gives us a view into whether GetServices is returning expected data.
                switch (service.ServiceName.ToUpperInvariant())
                {
                    case KeyIsoSvcName:
                        foundKeyIsoSvc = true;
                        Assert.False(string.IsNullOrEmpty(service.DisplayName), "string.IsNullOrEmpty(KeyIso.DisplayName)");
                        Assert.Equal(PlatformDetection.IsNotWindowsNanoServer, service.CanStop);
                        Assert.False(service.CanPauseAndContinue, "KeyIso.CanPauseAndContinue");
                        Assert.False(service.CanShutdown, "KeyIso.CanShutdown");
                        Assert.Equal(ServiceType.Win32ShareProcess, service.ServiceType);
                        Assert.Equal(ServiceStartMode.Manual, service.StartType);
                        break;
                    case "SAMSS":
                        foundSamSvc = true;
                        Assert.False(string.IsNullOrEmpty(service.DisplayName), "string.IsNullOrEmpty(SamSs.DisplayName)");
                        Assert.False(service.CanStop, "SamSs.CanStop");
                        Assert.False(service.CanPauseAndContinue, "SamSs.CanPauseAndContinue");
                        Assert.False(service.CanShutdown, "SamSs.CanShutdown");
                        Assert.Equal(ServiceType.Win32ShareProcess, service.ServiceType);
                        Assert.Equal(ServiceStartMode.Automatic, service.StartType);
                        break;
                    case "EVENTLOG":
                        Assert.False(string.IsNullOrEmpty(service.DisplayName), "string.IsNullOrEmpty(EventLog.DisplayName)");
                        Assert.True(service.CanStop, "EventLog.CanStop");
                        Assert.True(service.CanShutdown, "EventLog.CanShutdown");
                        break;
                    default:
                        foundOtherSvc = true;
                        break;
                }
            }

            Assert.True(foundKeyIsoSvc, "foundKeyIsoSvc");
            Assert.True(foundSamSvc, "foundSamSvc");
            Assert.True(foundOtherSvc, "foundOtherSvc");
        }

        [Fact]
        public static void ConstructWithBadServiceName()
        {
            Assert.ThrowsAny<ArgumentException>(() => new ServiceController(null));
            Assert.Throws<ArgumentException>(() => new ServiceController(""));
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, ".NET Framework does not throw")]
        public static void Initialize_GetNames()
        {
            Assert.Throws<InvalidOperationException>(() => new ServiceController().ServiceName);
            Assert.Throws<InvalidOperationException>(() => new ServiceController().DisplayName);
        }

        [Fact]
        public static void GetDevices()
        {
            ServiceController[] devices = ServiceController.GetDevices();
            Assert.True(devices.Length != 0);

            const ServiceType SERVICE_DRIVER =
                ServiceType.FileSystemDriver |
                ServiceType.KernelDriver |
                ServiceType.RecognizerDriver;

            Assert.All(devices, device => Assert.NotEqual(0, (int)(device.ServiceType & SERVICE_DRIVER)));
        }

        [Fact]
        public static void EnumerateDeviceService()
        {
            ServiceController[] devices = ServiceController.GetDevices();
            Assert.True(devices.Length != 0);

            ServiceController actual = new ServiceController(devices[0].ServiceName, ".");
            Assert.Equal(devices[0].Status, actual.Status);
            Assert.Equal(devices[0].ServiceName, actual.ServiceName);
            Assert.Equal(devices[0].DisplayName, actual.DisplayName);
            Assert.Equal(devices[0].ServiceType, actual.ServiceType);
            Assert.Equal(devices[0].MachineName, actual.MachineName);
        }

        [Fact]
        public static void NonExistentService_GetStatus()
        {
            var controller = new ServiceController(Guid.NewGuid().ToString("N"));
            Exception exception = Assert.Throws<InvalidOperationException>(() => controller.Status);
            Assert.IsType<Win32Exception>(exception.InnerException);
        }

        [Fact]
        public static void NonExistentService_GetDisplayName()
        {
            var controller = new ServiceController(Guid.NewGuid().ToString("N"));
            Exception exception = Assert.Throws<InvalidOperationException>(() => controller.DisplayName);
            Assert.IsType<Win32Exception>(exception.InnerException);
        }

        [Fact]
        public static void SetNameToNonexistentService_GetStatus()
        {
            var controller = new ServiceController();
            controller.ServiceName = Guid.NewGuid().ToString("N");
            Exception exception = Assert.Throws<InvalidOperationException>(() => controller.Status);
            Assert.IsType<Win32Exception>(exception.InnerException);
        }

        [Fact]
        public static void SetNameToNonexistentService_GetDisplayName()
        {
            var controller = new ServiceController();
            controller.ServiceName = Guid.NewGuid().ToString("N");
            Exception exception = Assert.Throws<InvalidOperationException>(() => controller.DisplayName);
            Assert.IsType<Win32Exception>(exception.InnerException);
        }

        [Fact]
        public static void SetDisplayNameToNonexistentService_GetStatus()
        {
            var controller = new ServiceController();
            controller.DisplayName = Guid.NewGuid().ToString("N");
            Exception exception = Assert.Throws<InvalidOperationException>(() => controller.Status);
            Assert.IsType<Win32Exception>(exception.InnerException);
        }

        [Fact]
        public static void SetDisplayNameToNonexistentService_GetServiceName()
        {
            var controller = new ServiceController();
            controller.DisplayName = Guid.NewGuid().ToString("N");
            Exception exception = Assert.Throws<InvalidOperationException>(() => controller.ServiceName);
            Assert.IsType<Win32Exception>(exception.InnerException);
        }

        [Fact]
        public static void SetServiceName_GetDisplayName()
        {
            var keyIsoDisplayName = new ServiceController(KeyIsoSvcName).DisplayName;

            var controller = new ServiceController();
            controller.ServiceName = KeyIsoSvcName;

            // On Nano Server >=1809 the casing is sometimes changed during normalization: Expected: KEYISO, Actual: KeyIso
            Assert.Equal(keyIsoDisplayName, controller.DisplayName, ignoreCase: PlatformDetection.IsWindowsNanoServer);
        }

        [Fact]
        [OuterLoop] // The test may fail on individual machines with more than one UI language.
        public static void SetDisplayName_GetServiceName()
        {
            var keyIsoDisplayName = new ServiceController(KeyIsoSvcName).DisplayName;

            var controller = new ServiceController();
            controller.DisplayName = keyIsoDisplayName;
            Assert.Equal(KeyIsoSvcName.ToLowerInvariant(), controller.ServiceName.ToLowerInvariant());
        }

        [Fact]
        public static void InitializeServiceName_GetDisplayName()
        {
            var controller = new ServiceController(KeyIsoSvcName);
            Assert.Equal(KeyIsoSvcName, controller.ServiceName);
            Assert.NotEmpty(controller.DisplayName);
        }

        [Fact]
        [OuterLoop] // The test may fail on individual machines with more than one UI language.
        public static void InitializeDisplayName_GetServiceName()
        {
            var keyIsoDisplayName = new ServiceController(KeyIsoSvcName).DisplayName;

            var controller = new ServiceController(keyIsoDisplayName);
            Assert.Equal(KeyIsoSvcName.ToLowerInvariant(), controller.ServiceName.ToLowerInvariant());
            Assert.Equal(keyIsoDisplayName, controller.DisplayName);
        }

        [Fact]
        public static void WaitForStatusTimeout()
        {
            var controller = new ServiceController(KeyIsoSvcName);
            Assert.Throws<System.ServiceProcess.TimeoutException>(() => controller.WaitForStatus(ServiceControllerStatus.Paused, TimeSpan.Zero));
        }
    }
}
