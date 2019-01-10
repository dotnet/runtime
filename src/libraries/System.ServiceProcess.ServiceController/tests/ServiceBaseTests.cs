// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Xunit;

/// <summary>
/// NOTE: All tests checking the output file should always call Stop before checking because Stop will flush the file to disk.
/// </summary>
namespace System.ServiceProcess.Tests
{
    [OuterLoop(/* Modifies machine state */)]
    [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Persistent issues starting test service on NETFX")]
    public class ServiceBaseTests : IDisposable
    {
        private const int connectionTimeout = 30000;
        private readonly TestServiceProvider _testService;

        private static readonly Lazy<bool> s_isElevated = new Lazy<bool>(() => AdminHelpers.IsProcessElevated());
        protected static bool IsProcessElevated => s_isElevated.Value;
        protected static bool IsElevatedAndSupportsEventLogs => IsProcessElevated && PlatformDetection.IsNotWindowsNanoServer;

        private bool _disposed;

        public ServiceBaseTests()
        {
            _testService = new TestServiceProvider();
        }

        private void AssertExpectedProperties(ServiceController testServiceController)
        {
            var comparer = PlatformDetection.IsFullFramework ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal; // Full framework upper cases the name
            Assert.Equal(_testService.TestServiceName, testServiceController.ServiceName, comparer);
            Assert.Equal(_testService.TestServiceDisplayName, testServiceController.DisplayName);
            Assert.Equal(_testService.TestMachineName, testServiceController.MachineName);
            Assert.Equal(ServiceType.Win32OwnProcess, testServiceController.ServiceType);
            Assert.True(testServiceController.CanPauseAndContinue);
            Assert.True(testServiceController.CanStop);
            Assert.True(testServiceController.CanShutdown);
        }

        // [Fact]
        // To cleanup lingering Test Services uncomment the Fact attribute and run the following command
        //   msbuild /t:rebuildandtest /p:XunitMethodName=System.ServiceProcess.Tests.ServiceBaseTests.Cleanup /p:OuterLoop=true
        // Remember to comment out the Fact again before running tests otherwise it will cleanup tests running in parallel
        // and cause them to fail.
        public void Cleanup()
        {
            string currentService = "";
            foreach (ServiceController controller in ServiceController.GetServices())
            {
                try
                {
                    currentService = controller.DisplayName;
                    if (controller.DisplayName.StartsWith("Test Service"))
                    {
                        Console.WriteLine("Trying to clean-up " + currentService);
                        TestServiceInstaller deleteService = new TestServiceInstaller()
                        {
                            ServiceName = controller.ServiceName
                        };
                        deleteService.RemoveService();
                        Console.WriteLine("Cleaned up " + currentService);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Failed " + ex.Message);
                }
            }
        }

        [ConditionalFact(nameof(IsProcessElevated))]
        public void TestOnStartThenStop()
        {
            ServiceController controller = ConnectToServer();

            controller.Stop();
            Assert.Equal((int)PipeMessageByteCode.Stop, _testService.GetByte());
            controller.WaitForStatus(ServiceControllerStatus.Stopped);
        }

        [ConditionalFact(nameof(IsProcessElevated))]
        public void TestOnStartWithArgsThenStop()
        {
            ServiceController controller = ConnectToServer();

            controller.Stop();
            Assert.Equal((int)PipeMessageByteCode.Stop, _testService.GetByte());
            controller.WaitForStatus(ServiceControllerStatus.Stopped);

            controller.Start(new string[] { "StartWithArguments", "a", "b", "c" });
            _testService.Client = null;
            _testService.Client.Connect();

            // There is no definite order between start and connected when tests are running on multiple threads.
            // In this case we dont care much about the order, so we are just checking whether the appropiate bytes have been sent.
            Assert.Equal((int)(PipeMessageByteCode.Connected | PipeMessageByteCode.Start), _testService.GetByte() | _testService.GetByte());
            controller.WaitForStatus(ServiceControllerStatus.Running);

            controller.Stop();
            Assert.Equal((int)PipeMessageByteCode.Stop, _testService.GetByte());
            controller.WaitForStatus(ServiceControllerStatus.Stopped);
        }

        [ConditionalFact(nameof(IsProcessElevated))]
        public void TestOnPauseThenStop()
        {
            ServiceController controller = ConnectToServer();

            controller.Pause();
            Assert.Equal((int)PipeMessageByteCode.Pause, _testService.GetByte());
            controller.WaitForStatus(ServiceControllerStatus.Paused);

            controller.Stop();
            Assert.Equal((int)PipeMessageByteCode.Stop, _testService.GetByte());
            controller.WaitForStatus(ServiceControllerStatus.Stopped);
        }

        [ConditionalFact(nameof(IsProcessElevated))]
        public void TestOnPauseAndContinueThenStop()
        {
            ServiceController controller = ConnectToServer();

            controller.Pause();
            Assert.Equal((int)PipeMessageByteCode.Pause, _testService.GetByte());
            controller.WaitForStatus(ServiceControllerStatus.Paused);

            controller.Continue();
            Assert.Equal((int)PipeMessageByteCode.Continue, _testService.GetByte());
            controller.WaitForStatus(ServiceControllerStatus.Running);

            controller.Stop();
            Assert.Equal((int)PipeMessageByteCode.Stop, _testService.GetByte());
            controller.WaitForStatus(ServiceControllerStatus.Stopped);
        }

        [ConditionalFact(nameof(IsProcessElevated))]
        public void TestOnExecuteCustomCommand()
        {
            ServiceController controller = ConnectToServer();

            controller.ExecuteCommand(128);
            Assert.Equal(128, _testService.GetByte());

            controller.Stop();
            Assert.Equal((int)PipeMessageByteCode.Stop, _testService.GetByte());
            controller.WaitForStatus(ServiceControllerStatus.Stopped);
        }

        [ConditionalFact(nameof(IsProcessElevated))]
        public void TestOnContinueBeforePause()
        {
            ServiceController controller = ConnectToServer();

            controller.Continue();
            controller.WaitForStatus(ServiceControllerStatus.Running);

            controller.Stop();
            Assert.Equal((int)PipeMessageByteCode.Stop, _testService.GetByte());
            controller.WaitForStatus(ServiceControllerStatus.Stopped);
        }

        [ConditionalFact(nameof(IsElevatedAndSupportsEventLogs))]
        public void LogWritten()
        {
            string serviceName = Guid.NewGuid().ToString();
            // The default username for installing the service is NT AUTHORITY\\LocalService which does not have access to EventLog.
            // If the username is null, then the service is created under LocalSystem Account which have access to EventLog.
            var testService = new TestServiceProvider(serviceName, userName: null);
            Assert.True(EventLog.SourceExists(serviceName));
            testService.DeleteTestServices();
        }

        [ConditionalFact(nameof(IsElevatedAndSupportsEventLogs))]
        public void LogWritten_AutoLog_False()
        {
            string serviceName = nameof(LogWritten_AutoLog_False) + Guid.NewGuid().ToString();
            var testService = new TestServiceProvider(serviceName);
            Assert.False(EventLog.SourceExists(serviceName));
            testService.DeleteTestServices();
        }

        [ConditionalFact(nameof(IsProcessElevated))]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Full Framework receives the Connected Byte Code after the Exception Thrown Byte Code")]
        public void PropagateExceptionFromOnStart()
        {
            string serviceName = nameof(PropagateExceptionFromOnStart) + Guid.NewGuid().ToString();
            var testService = new TestServiceProvider(serviceName);
            testService.Client.Connect(connectionTimeout);
            Assert.Equal((int)PipeMessageByteCode.Connected, testService.GetByte());
            Assert.Equal((int)PipeMessageByteCode.ExceptionThrown, testService.GetByte());
            testService.DeleteTestServices();
        }

        private ServiceController ConnectToServer()
        {
            _testService.Client.Connect(connectionTimeout);
            Assert.Equal((int)PipeMessageByteCode.Connected, _testService.GetByte());

            ServiceController controller = new ServiceController(_testService.TestServiceName);
            AssertExpectedProperties(controller);
            return controller;
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
