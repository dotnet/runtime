// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.XUnitExtensions;
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
            var comparer = PlatformDetection.IsNetFramework ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal; // .NET Framework upper cases the name
            Assert.Equal(_testService.TestServiceName, testServiceController.ServiceName, comparer);
            Assert.Equal(_testService.TestServiceDisplayName, testServiceController.DisplayName);
            Assert.Equal(_testService.TestMachineName, testServiceController.MachineName);
            Assert.Equal(ServiceType.Win32OwnProcess, testServiceController.ServiceType);
            Assert.True(testServiceController.CanPauseAndContinue);
            Assert.True(testServiceController.CanStop);
            Assert.True(testServiceController.CanShutdown);
        }

        // [Fact]
        // To cleanup lingering Test Services uncomment the Fact attribute, make it public and run the following command
        // dotnet build /t:test /p:XunitMethodName=System.ServiceProcess.Tests.ServiceBaseTests.Cleanup /p:OuterLoop=true
        // Remember to comment out the Fact again before running tests otherwise it will cleanup tests running in parallel
        // and cause them to fail.
        private void Cleanup()
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

            // Start created a new TestService; dispose of our client stream and reconnect to it
            _testService.Client = null;
            _testService.Client.Connect();

            // Test service does not mutually synchronize Connected and Start messages
            var bytes = new byte[] { _testService.GetByte(), _testService.GetByte() };
            Assert.Contains((byte)PipeMessageByteCode.Connected, bytes);
            Assert.Contains((byte)PipeMessageByteCode.Start, bytes);

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
            if (PlatformDetection.IsWindowsServerCore)
            {
                throw new SkipTestException("Skip on Windows Server Core"); // https://github.com/dotnet/runtime/issues/43207
            }

            ServiceController controller = ConnectToServer();

            controller.ExecuteCommand(128);
            // Response from test service:
            //  128 => Environment.UserInteractive == false
            //  129 => Environment.UserInteractive == true
            //
            // On Windows Nano and other SKU that do not expose Window Stations, Environment.UserInteractive
            // will always return true, even within a service process.
            // Otherwise, we expect it to be false.
            // (This is the only place we verify Environment.UserInteractive can return false)
            byte expected = PlatformDetection.HasWindowsShell ? (byte)128 : (byte)129;
            Assert.Equal(expected, _testService.GetByte());

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
            // If the username is null, then the service is created under LocalSystem Account which have access to EventLog.
            var testService = new TestServiceProvider(serviceName);
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
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, ".NET Framework receives the Connected Byte Code after the Exception Thrown Byte Code")]
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
            TestServiceProvider.DebugTrace("ServiceBaseTests.ConnectToServer: connecting");
            _testService.Client.Connect(connectionTimeout);
            Assert.Equal((int)PipeMessageByteCode.Connected, _testService.GetByte());
            TestServiceProvider.DebugTrace("ServiceBaseTests.ConnectToServer: received connect byte");

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
