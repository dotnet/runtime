using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.DotNet.XHarness.TestRunners.Common;
using XUnitWrapperLibrary;

namespace XHarnessRunnerLibrary;

public static class RunnerEntryPoint
{
    public static async Task<int> RunTests(
        Func<TestFilter?,
        TestSummary> runTestsCallback,
        string assemblyName,
        string? filter)
    {
        ApplicationEntryPoint? entryPoint = null;

        if (OperatingSystem.IsAndroid())
        {
            entryPoint = new AndroidEntryPoint(new SimpleDevice(assemblyName), runTestsCallback, assemblyName, filter);
        }
        if (OperatingSystem.IsMacCatalyst() || OperatingSystem.IsIOS() || OperatingSystem.IsTvOS())
        {
            entryPoint = new AppleEntryPoint(new SimpleDevice(assemblyName), runTestsCallback, assemblyName, filter);
        }
        if (OperatingSystem.IsBrowser() || OperatingSystem.IsWasi() )
        {
            entryPoint = new WasmEntryPoint(runTestsCallback, assemblyName, filter);
        }
        if (entryPoint is null)
        {
            throw new InvalidOperationException("The XHarness runner library test runner is only supported on mobile+wasm platforms. Use the XUnitWrapperLibrary-based runner on desktop platforms.");
        }

        bool anyFailedTests = false;
        entryPoint.TestsCompleted += (o, e) => anyFailedTests = e.FailedTests > 0;
        await entryPoint.RunAsync();

        if (OperatingSystem.IsBrowser() || OperatingSystem.IsWasi() )
        {
            // Browser expects all xharness processes to exit with 0, even in case of failure
            return 0;
        }
        return anyFailedTests ? 1 : 0;
    }

    sealed class AppleEntryPoint : iOSApplicationEntryPointBase
    {
        private readonly Func<TestFilter?, TestSummary> _runTestsCallback;
        private readonly string _assemblyName;
        private readonly string? _methodNameToRun;

        public AppleEntryPoint(
            IDevice device,
            Func<TestFilter?, TestSummary> runTestsCallback,
            string assemblyName,
            string? methodNameToRun)
        {
            Device = device;
            _runTestsCallback = runTestsCallback;
            _assemblyName = assemblyName;
            _methodNameToRun = methodNameToRun;
        }

        protected override IDevice? Device { get; }
        protected override int? MaxParallelThreads => 1;
        protected override bool IsXunit => true;
        protected override TestRunner GetTestRunner(LogWriter logWriter)
        {
            var runner = new GeneratedTestRunner(logWriter, _runTestsCallback, _assemblyName, writeBase64TestResults: false);
            if (_methodNameToRun is not null)
            {
                runner.SkipMethod(_methodNameToRun, isExcluded: false);
            }
            return runner;
        }

        protected override IEnumerable<TestAssemblyInfo> GetTestAssemblies() => Array.Empty<TestAssemblyInfo>();
        protected override void TerminateWithSuccess() => Console.WriteLine("[TerminateWithSuccess]");
    }

    sealed class AndroidEntryPoint : AndroidApplicationEntryPointBase
    {
        private readonly Func<TestFilter?, TestSummary> _runTestsCallback;
        private readonly string _assemblyName;
        private readonly string? _methodNameToRun;

        public AndroidEntryPoint(
            IDevice device,
            Func<TestFilter?, TestSummary> runTestsCallback,
            string assemblyName,
            string? methodNameToRun)
        {
            Device = device;
            _runTestsCallback = runTestsCallback;
            _assemblyName = assemblyName;
            _methodNameToRun = methodNameToRun;
        }

        protected override IDevice? Device { get; }
        protected override int? MaxParallelThreads => 1;
        protected override bool IsXunit => true;
        protected override TestRunner GetTestRunner(LogWriter logWriter)
        {
            var runner = new GeneratedTestRunner(logWriter, _runTestsCallback, _assemblyName, writeBase64TestResults: false);
            if (_methodNameToRun is not null)
            {
                runner.SkipMethod(_methodNameToRun, isExcluded: false);
            }
            return runner;
        }

        public override string TestsResultsFinalPath
        {
            get
            {
                string? testResultsDir = Environment.GetEnvironmentVariable("TEST_RESULTS_DIR");
                if (string.IsNullOrEmpty(testResultsDir))
                    throw new ArgumentException("TEST_RESULTS_DIR should not be empty");

                return Path.Combine(testResultsDir, "testResults.xml");
            }
        }
        protected override IEnumerable<TestAssemblyInfo> GetTestAssemblies() => Array.Empty<TestAssemblyInfo>();

        protected override void TerminateWithSuccess() {}

        public override TextWriter? Logger => null;
    }

    sealed class WasmEntryPoint : WasmApplicationEntryPointBase
    {
        private readonly Func<TestFilter?, TestSummary> _runTestsCallback;
        private readonly string _assemblyName;
        private readonly string? _methodNameToRun;

        public WasmEntryPoint(
            Func<TestFilter?, TestSummary> runTestsCallback,
            string assemblyName,
            string? methodNameToRun)
        {
            _runTestsCallback = runTestsCallback;
            _assemblyName = assemblyName;
            _methodNameToRun = methodNameToRun;
        }
        protected override int? MaxParallelThreads => 1;
        protected override bool IsXunit => true;
        protected override TestRunner GetTestRunner(LogWriter logWriter)
        {
            var runner = new GeneratedTestRunner(logWriter, _runTestsCallback, _assemblyName, writeBase64TestResults: true);
            if (_methodNameToRun is not null)
            {
                runner.SkipMethod(_methodNameToRun, isExcluded: false);
            }
            return runner;
        }

        protected override IEnumerable<TestAssemblyInfo> GetTestAssemblies() => Array.Empty<TestAssemblyInfo>();
    }

    class SimpleDevice : IDevice
    {
        public SimpleDevice(string assemblyName)
        {
            BundleIdentifier = "net.dot." + assemblyName;
        }

        public string BundleIdentifier { get; }

        public string? UniqueIdentifier { get; }

        public string? Name { get; }

        public string? Model { get; }

        public string? SystemName { get; }

        public string? SystemVersion { get; }

        public string? Locale { get; }
    }
}
