// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#define USE_INSTRUMENTATION
using System;
using System.Collections;
using System.Threading;
using System.Reflection;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Collections.Specialized;
using System.Net;
//using System.Web.Mail;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

using System.Runtime.Loader;

delegate void TestPreLoaderDelegate(ReliabilityTest test, string[] paths);
delegate void AssemblyLoadContextUnloadDelegate();

internal class CustomAssemblyResolver : AssemblyLoadContext
{
    private string _frameworkPath;
    private string _testsPath;

    public CustomAssemblyResolver()
    {
        _frameworkPath = Environment.GetEnvironmentVariable("BVT_ROOT");
        if (_frameworkPath == null)
        {
            _frameworkPath = Environment.GetEnvironmentVariable("CORE_ROOT");
        }

        if (_frameworkPath == null)
        {
            _frameworkPath = Directory.GetCurrentDirectory();
        }

        string stressFrameworkDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        _testsPath = Path.Combine(stressFrameworkDir, "Tests");
    }

    protected override Assembly Load(AssemblyName assemblyName)
    {
        string strPath;
        if (assemblyName.Name.StartsWith("System."))
        {
            strPath = Path.Combine(_frameworkPath, assemblyName.Name + ".dll");
        }
        else
        {
            strPath = Path.Combine(_testsPath, assemblyName.Name + ".dll");
        }
        Assembly asmLoaded = LoadFromAssemblyPath(strPath);
        return asmLoaded;
    }
}

public interface IMultipleReliabilityTest
{
    bool Register();
    bool Unregister();
    bool Run(int testNumber);
    int TestCount { get; }
}

public interface ISingleReliabilityTest
{
    bool Register();
    bool Unregister();
    bool Run();			// returns true on success, false on failure.
}

public class ReliabilityFramework
{
    // instance members
    private int _testsRunningCount = 0, _testsRanCount = 0, _failCount = 0;
    private ReliabilityConfig _reliabilityConfig;
    private ReliabilityTestSet _curTestSet;
    private DateTime _startTime;
    private bool _totalSuccess;
    private Guid _resultGroupGuid = Guid.Empty;
    private AutoResetEvent _testDone = new AutoResetEvent(false);
    TestAssemblyLoadContext[] _testALCs = null;
    private DetourHelpers _detourHelpers;
    private Hashtable _foundTests;
    public int LoadingCount = 0;
    private int _reportedFailCnt = 0;
    private RFLogging _logger = new RFLogging();
    private DateTime _lastLogTime = DateTime.Now;

    // static members
    private static int s_seed = (int)System.DateTime.Now.Ticks;
    private static Random s_randNum = new Random(s_seed);
    private static string timeValue = null;
    private static bool s_fNoExit = false;
    // constants
    private const string waitingText = "Waiting for all tests to finish loading, Remaining Tests: ";

    // support for running in automation
    internal static bool IsRunningAsUnitTest = false;
    internal static bool IsRunningLongGCTests = false;

    internal MethodInfo _privateCollectionCountMethod;

    private int PrivateCollectionCount(int generation)
    {
        if (_privateCollectionCountMethod == null)
        {
            _privateCollectionCountMethod = typeof(GC).GetMethod("_CollectionCount", BindingFlags.NonPublic | BindingFlags.Static);
        }
        object result = _privateCollectionCountMethod.Invoke(null, new object[] { generation, 1 });
        int value = (int)result;
        return value;
    }

    /// <summary>
    /// Our main execution routine for the reliability framework.  Here we create an instance of the framework & run the reliability tests
    /// in it.  All code in here will execute in our starting app domain.
    /// </summary>
    /// <param name="args">command line arguments.  First argument should be the config file we use for this run</param>
    /// <returns>Returns 100 on success, something else on failure</returns>
    public static int Main(string[] args)
    {
        string configFile = null;
        bool okToContinue = true, doReplay = false;
        string sTests = "tests", sSeed = "seed", exectime = "maximumExecutionTime";

        ReliabilityFramework rf = new ReliabilityFramework();
        rf._logger.WriteToInstrumentationLog(null, LoggingLevels.StartupShutdown, "Started");
        foreach (string arg in args)
        {
            rf._logger.WriteToInstrumentationLog(null, LoggingLevels.StartupShutdown, String.Format("Argument: {0}", arg));
            if (arg[0] == '-')
            {
                if (String.Compare(arg.Substring(1), "replay", true) == 0)
                {
                    doReplay = true;
                }
                else if (String.Compare(arg.Substring(1), "unittest", true) == 0)
                {
                    IsRunningAsUnitTest = true;
                }
                else if (String.Compare(arg.Substring(1, arg.IndexOf(':') - 1), sTests, true) == 0)
                {
                    String testlist = arg.Substring(sTests.Length + 2);
                    String[] tests;
                    tests = testlist.Split(',');
                }
                else if (String.Compare(arg.Substring(1, arg.IndexOf(':') - 1), sSeed, true) == 0)
                {
                    s_seed = Convert.ToInt32(arg.Substring(sSeed.Length + 2));
                    s_randNum = new Random(s_seed);
                }
                else if (String.Compare(arg.Substring(1, arg.IndexOf(':') - 1), exectime, true) == 0)
                {
                    timeValue = arg.Substring(exectime.Length + 2);
                }

                else
                {
                    Console.WriteLine("Unknown option: {0}", arg);
                    okToContinue = false;
                }
            }
            else if (configFile == null)
            {
                configFile = arg;
            }
        }

        IsRunningLongGCTests = System.Environment.GetEnvironmentVariable("RunningLongGCTests") == "1";

        // if no config file specified, check for [something]_gc.config in the current folder.
        if (configFile == null)
        {
            var config = IsRunningAsUnitTest ?
                "*_gc_ci.config" :
                "*_gc.config";

            configFile = Directory.GetFiles(Environment.CurrentDirectory, config).SingleOrDefault();
        }

        if (configFile == null)
        {
            okToContinue = false;
            Console.WriteLine("You must specify a config file!");
            rf._logger.WriteToInstrumentationLog(null, LoggingLevels.StartupShutdown, "No configuration file specified.");
        }

        System.Console.WriteLine("Using config file: " + configFile);

        if (!okToContinue)
        {
            Console.WriteLine("\r\nHost Interface Reliability Harness\r\n");
            Console.WriteLine("Usage: ReliabiltityFramework [options] <test config file>");
            Console.WriteLine("");
            Console.WriteLine("Available options: ");
            Console.WriteLine("");
            Console.WriteLine(" -replay     -   Replay from log file");
            Console.WriteLine(" -{0}:<tests>	-	Comma delimited list of tests to run (no spaces)", sTests);
            Console.WriteLine(" -{0}:<seed>	-	Random Number seed for replays", sSeed);
            Console.WriteLine(" -unittest   -   Set when run via unit test harness");
            rf._logger.WriteToInstrumentationLog(null, LoggingLevels.StartupShutdown, "Not ok to continue.");

            return 0;
        }

        int retVal = -1;
        try
        {
            try
            {
                rf._logger.WriteToInstrumentationLog(null, LoggingLevels.Tests, "Running tests...");
                retVal = rf.RunReliabilityTests(configFile, doReplay);
                rf._logger.WriteToInstrumentationLog(null, LoggingLevels.Tests, String.Format("Successfully executed tests, return val: {0}", retVal));
            }
            catch (OutOfMemoryException e)
            {
                rf.HandleOom(e, "Running tests");
            }
            catch (Exception e)
            {
                Exception eTemp = e.InnerException;
                while (eTemp != null)
                {
                    if (eTemp is OutOfMemoryException)
                    {
                        rf.HandleOom(e, "Running tests (inner)");
                        break;
                    }

                    eTemp = e.InnerException;
                }

                string err = String.Format("Exception while running tests: {0}", e);

                if (eTemp == null)
                {
                    rf._logger.WriteToInstrumentationLog(null, LoggingLevels.Tests, err);
                    Console.WriteLine("There was an exception while attempting to run the tests: See Instrumentation Log for details. (Exception: {0})", e);
                }

                // crash on exceptions when running as a unit test.
                if (IsRunningAsUnitTest)
                    Environment.FailFast(err, e);
            }
        }
        finally
        {
            rf._logger.WriteToInstrumentationLog(null, LoggingLevels.StartupShutdown, "Reliability framework is shutting down...");
        }

        NoExitPoll();

        rf._logger.WriteToInstrumentationLog(null, LoggingLevels.StartupShutdown, String.Format("Shutdown w/ ret val of  {0}", retVal));


        GC.Collect(2);
        GC.WaitForPendingFinalizers();
        return (retVal);
    }

    public void HandleOom(Exception e, string message)
    {
        try
        {
            _logger.WriteToInstrumentationLog(_curTestSet, LoggingLevels.Tests, String.Format("Exception while running tests: {0}", e));
            if (_curTestSet.DebugBreakOnOutOfMemory)
            {
                OomExceptionCausedDebugBreak();
            }
        }
        catch (OutOfMemoryException)
        {
            // hang and let someone debug if we can't even break in...
            Thread.CurrentThread.Join();
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void OomExceptionCausedDebugBreak()
    {
        MyDebugBreak("Harness");
    }

    /// <summary>
    /// Runs the reliability tests.  Called from Main with the name of the configuration file we should be using.
    /// All code in here runs in our starting app domain.
    /// </summary>
    /// <param name="testConfig">configuration file to use</param>
    /// <returns>100 on success, another number on failure.</returns>
    public int RunReliabilityTests(string testConfig, bool doReplay)
    {
        _totalSuccess = true;

        try
        {
            _logger.WriteToInstrumentationLog(_curTestSet, LoggingLevels.Tests, "Getting configuration...");
            _reliabilityConfig = new ReliabilityConfig(testConfig);
        }
        catch (ArgumentException e)
        {
            _logger.WriteToInstrumentationLog(_curTestSet, LoggingLevels.Tests, String.Format("Error while getting configuration: {0}", e));
            return (-1);
        }
        catch (FileNotFoundException fe)
        {
            _logger.WriteToInstrumentationLog(_curTestSet, LoggingLevels.Tests, String.Format("Couldn't find configuration file: {0}", fe));
            return (-1);
        }

        // save the current directory
        string curDir = Directory.GetCurrentDirectory();

        // Enumerator through all the test sets...
        foreach (ReliabilityTestSet testSet in _reliabilityConfig)
        {
            if (testSet.InstallDetours)
            {
                _detourHelpers = new DetourHelpers();
                _detourHelpers.Initialize(testSet);
            }
            else
            {
                if (_detourHelpers != null)
                {
                    _detourHelpers.Uninitialize();
                }
                _detourHelpers = null;
            }

            // restore the current directory incase a test changed it
            Directory.SetCurrentDirectory(curDir);

            _logger.WriteToInstrumentationLog(testSet, LoggingLevels.Tests, String.Format("Executing test set: {0}", testSet.FriendlyName));
            _testsRunningCount = 0;
            _testsRanCount = 0;
            _curTestSet = testSet;
            if (timeValue != null)
                _curTestSet.MaximumTime = ReliabilityConfig.ConvertTimeValueToTestRunTime(timeValue);

            _logger.ReportResults = _curTestSet.ReportResults;

            if (_curTestSet.AssemblyLoadContextLoaderMode == AssemblyLoadContextLoaderMode.RoundRobin)
            {
                // full isoloation & normal are handled by the way we setup
                // tests in ReliabilityConfiguration.  Round robin needs extra
                // logic when we create app domains.
                _testALCs = new TestAssemblyLoadContext[_curTestSet.NumAssemblyLoadContexts];
                for (int alc = 0; alc < _curTestSet.NumAssemblyLoadContexts; alc++)
                {
                    _testALCs[alc] = new TestAssemblyLoadContext("RoundRobinContext" + alc.ToString());
                }
            }

            if (_curTestSet.ReportResults)
            {
                _logger.WriteToInstrumentationLog(_curTestSet, LoggingLevels.SmartDotNet, "Reporting results...");
                try
                {
                }
                catch (Exception e)
                {
                    _logger.WriteToInstrumentationLog(_curTestSet, LoggingLevels.SmartDotNet, String.Format("Exception while communicating w/ smart.net server: {0}", e));
                    AddFailure("Failed to initialize result reporting", null, -1);
                }
            }

            // we don't log while we're replaying a log file.
            if (!doReplay)
            {
                _logger.WriteToInstrumentationLog(_curTestSet, LoggingLevels.Logging, "Opening log file...");
                if (!_curTestSet.DisableLogging)
                {
                    _logger.OpenLog(_curTestSet.FriendlyName);
                }
                _logger.WriteStartupInfo(s_seed);
            }

            if (testSet.Tests == null)
            {
                _logger.WriteToInstrumentationLog(_curTestSet, LoggingLevels.Tests, "No tests to run in test set");
                Console.WriteLine("No tests to run, skipping..\r\n");
                // no tests in this test set, skip it.
                continue;
            }

            // step 1: preload all the tests, this does NOT start them.
            _logger.WriteToInstrumentationLog(_curTestSet, LoggingLevels.Tests, "Preloading tests...");
            Console.Write("Loading all tests: ");
            bool haveAtLeastOneTest = false;
            for (int i = 0; i < testSet.Tests.Length; i++)
            {
                ReliabilityTest test = testSet.Tests[i];

                switch (test.TestStartMode)
                {
                    case TestStartModeEnum.ProcessLoader:
                        Interlocked.Increment(ref LoadingCount);

                        //for the process loader we just need
                        //to fill in some details (such as the full path).
                        TestPreLoader(test, testSet.DiscoveryPaths);
                        if (test.TestObject == null)
                        {
                            Console.WriteLine("Test does not exist: {0}", test);
                            AddFailure("Test does not exist - disabling.", test, -1);
                        }
                        else
                        {
                            haveAtLeastOneTest = true;
                        }

                        break;
                    case TestStartModeEnum.AssemblyLoadContextLoader:
                        // for the AssemblyLoadContext loader we create the
                        // AssemblyLoadContexts here
                        try
                        {
                            if (_curTestSet.AssemblyLoadContextLoaderMode != AssemblyLoadContextLoaderMode.Lazy)
                            {
                                Interlocked.Increment(ref LoadingCount);

                                test.AssemblyLoadContextIndex = i % _curTestSet.NumAssemblyLoadContexts;    // only used for roudn robin scheduling.
                                Task.Factory.StartNew(() =>
                                {
                                    TestPreLoader(test, testSet.DiscoveryPaths);
                                });
                                //                                TestPreLoaderDelegate loadTestDelegate = new TestPreLoaderDelegate(this.TestPreLoader);
                                //                                loadTestDelegate.BeginInvoke(test, testSet.DiscoveryPaths, null, null);
                            }
                            haveAtLeastOneTest = true;
                        }
                        catch { }
                        break;
                    case TestStartModeEnum.AppDomainLoader:
                        Console.WriteLine("Appdomain mode is NOT supported for ProjectK");
                        break;
                }
                Console.Write(".");
            }

            _logger.WriteToInstrumentationLog(_curTestSet, LoggingLevels.Tests, "Finished Preloading tests...");
            if (!haveAtLeastOneTest)
            {
                _logger.WriteToInstrumentationLog(_curTestSet, LoggingLevels.Tests, "No tests to execute");
                AddFailure("No tests exist!", null, -1);
                Console.WriteLine("I have no tests to run!");
                continue;
            }

            while (LoadingCount != 0)
            {
                int tmp = LoadingCount;
                Console.Write("{0,4}\b\b\b\b", tmp);
                Thread.Sleep(1000);
            }

            _logger.WriteToInstrumentationLog(_curTestSet, LoggingLevels.Tests, "All tests loaded...");
            Console.WriteLine("");

            // update the startTime
            _startTime = DateTime.Now;

            // step 2: start all the tests & run them until we're done.
            //          if we're in replay mode we'll replay the start order from the log.

            if (doReplay)
            {
                Console.WriteLine("Replaying from log file {0}.log", _curTestSet.FriendlyName);
                ExecuteFromLog("Logs\\" + _curTestSet.FriendlyName + ".log");
            }
            else
            {
                _logger.WriteToInstrumentationLog(_curTestSet, LoggingLevels.Tests, "Beginning test run...");
                TestStarter();
                _logger.CloseLog();
            }

            if ((testSet.PercentPassIsPass != -1 && _failCount > 0 && ((_failCount * 100) / _testsRanCount) < (100 - testSet.PercentPassIsPass)))
            {
                Console.WriteLine("Some tests failed, but below the fail percent ({0} ran, {1} failed, perecent={2})", _testsRanCount, _failCount, testSet.PercentPassIsPass);
                _totalSuccess = true;
            }
        }

        if (_detourHelpers != null)
        {
            _detourHelpers.Uninitialize();
        }

        if (_totalSuccess)
        {
            Console.WriteLine("All tests passed");
            return (100);
        }
        return (99);
    }

    [DllImport("kernel32.dll")]
    private extern static void DebugBreak();

    [DllImport("kernel32.dll")]
    private extern static bool IsDebuggerPresent();

    [DllImport("kernel32.dll")]
    private extern static void OutputDebugString(string debugStr);

    /// <summary>
    /// Checks to see if we should block all execution due to a fatal error
    /// (when DebugBreak is not available on win9x or running outside the debugger).
    /// </summary>
    private static void NoExitPoll()
    {
        if (s_fNoExit)
        {
            try
            {
                Console.WriteLine("A fatal error has occurred, will not continue starting tests...");
            }
            finally
            {
                Thread.CurrentThread.Join();
            }
        }
    }
    internal static void MyDebugBreak(string extraData)
    {
        if (IsDebuggerPresent())
        {
            Console.WriteLine(string.Format("DebugBreak: {0}", extraData));
            DebugBreak();
        }
        {
            // We need to stop the process now,
            // but all the threads are still running
            try
            {
                Console.WriteLine("MyDebugBreak called, stopping process... {0}", extraData);
            }
            finally
            {
                s_fNoExit = true;
                Thread.CurrentThread.Join();
            }
        }
    }

    /// <summary>
    /// Calculates the total number of tests to be run based upon the maximum
    /// number of loops & number of tests in the current test set.
    /// </summary>
    /// <returns></returns>
    private int CalculateTestsToRun()
    {
        int totalTestsToRun = 0;
        foreach (ReliabilityTest test in _curTestSet.Tests)
        {
            for (int i = 0; i < test.ConcurrentCopies; i++)
            {
                totalTestsToRun++;
            }
        }

        if (_curTestSet.MaximumLoops != -1)
        {
            totalTestsToRun *= _curTestSet.MaximumLoops;
        }
        else
        {
            totalTestsToRun = Int32.MaxValue;
        }
        return (totalTestsToRun);
    }

    /// <summary>
    /// TestStarter monitors the current situation and starts tests as appropriate.
    /// </summary>
    ///
    private void TestStarter()
    {
        int totalTestsToRun = CalculateTestsToRun();
        int lastTestStarted = 0;			// this is our index into the array of tests, this ensures fair distribution over all the tests.
        DateTime lastStart = DateTime.Now;	// keeps track of when we last started a test
        TimeSpan minTimeToStartTest = new TimeSpan(0, 5, 0);	// after 5 minutes if we haven't started a test we're having problems...
        int cpuAdjust = 0, memAdjust = 0;	// if we discover that we're not starting new tests quick enough we adjust the CPU/Mem percentages
        // so we start new tests sooner (so they start BEFORE we drop below our minimum CPU)

        //Console.WriteLine("RF - TestStarter found {0} tests to run", totalTestsToRun);

        /************************************************************************
         * loop until we've run out of time or have executed all of the tests.
         */
        do
        {
            NoExitPoll();

            // if we're just waiting for tests to exit we don't need to look for new tests to
            // run or worry about bumping up the CPU or memory usage (because there's no more
            // tests available

            float memVal = 0;
            float cpuVal = 0;
            float pagesVal = 0;
            float pageFaultsVal = 0;
            float ourPageFaultsVal = 0;

            if ((_testsRanCount + _testsRunningCount) < totalTestsToRun)
            {
                // alright, so we have the potential of having available tests.  So now we need to figure
                // out whether or not it's actually appropriate for us to start a test.  We have a lot of
                // different data points to be considered when starting a tests.  Some of these make
                // tests run, while others stop tests from running.
                //		Maximum # of tests running at once			(stops tests from running)
                //		Minimum number of tests running at once		(makes tests run)
                //		Machine usage characteristics:
                //			CPU Usage								(makes tests run)
                //			Memory Usage							(makes tests run)
                //			Paging & Page Faults					(stops tests from running)


                bool startTest = false;				// do we need to start a test?
                TimeSpan timeRunning = DateTime.Now.Subtract(_startTime);

                // if the test didn't exist our test object is null
                // and the test can't be ran.
                if (_curTestSet.MaxTestsRunning == -1 || (_testsRunningCount < _curTestSet.MaxTestsRunning))    // don't start if we have a maximum # of tests and we've reached it.
                {
                    // check test running count.
                    if (_testsRunningCount < _curTestSet.MinTestsRunning || _testsRunningCount == 0)
                    {
                        startTest = true;
                        // check memory usage
                    }
                    else if (_curTestSet.EnablePerfCounters)
                    {
                        if (memVal < (_curTestSet.MinPercentMem + memAdjust))
                        {
                            startTest = true;
                            // the more we adjust the adjuster the harder we make to adjust it in the future.  We have to fall out
                            // of the range of 1/4 of the adjuster value to increment it again.  (so, if mem %==50, and memAdjust==8,
                            // we need to fall below 48 before we'll adjust it again)
                            if (memVal < (_curTestSet.MinPercentMem - (memAdjust >> 2)) && memAdjust < 25)
                            {
                                memAdjust++;
                            }
                        }
                        // check CPU usage
                        else if ((cpuVal < (_curTestSet.GetCurrentMinPercentCPU(timeRunning) + cpuAdjust)))
                        {
                            startTest = true;
                            // the more we adjust the adjuster the harder we make to adjust it in the future.  We have to fall out
                            // of the range of 1/4 of the adjuster value to increment it again.  (so, if cpu %==50, and cpuAdjust==8,
                            // we need to fall below 48 before we'll adjust it again)
                            if (cpuVal < (_curTestSet.GetCurrentMinPercentCPU(timeRunning) - (cpuAdjust >> 2)) && cpuAdjust < 25)
                            {
                                cpuAdjust++;
                            }
                        }

                        if (!startTest)
                        {
                            _logger.WriteToInstrumentationLog(_curTestSet, LoggingLevels.TestStarter, String.Format("Cannot start test (perf): TestsRunning: {0} Mem: {1} Cpu: {2} MemAdj: {3} CpuAdj: {4}", _testsRunningCount, memVal, cpuVal, memAdjust, cpuAdjust));
                        }

                        // We disable tests if we're paging too much.  TODO: Tune these numbers to be good.
                        if (startTest && (pagesVal > 75) && (pageFaultsVal > 200) && (ourPageFaultsVal > 150))
                        {
                            _logger.WritePerfStats(pagesVal, pageFaultsVal, ourPageFaultsVal, cpuVal, memVal, true);
                            startTest = false;
                        }
                    }
                }
                else if (_curTestSet.MaxTestsRunning != -1)
                {
                    _logger.WriteToInstrumentationLog(_curTestSet, LoggingLevels.TestStarter, String.Format("Blocking until test is finished"));
                    _testDone.WaitOne();
                    _logger.WriteToInstrumentationLog(_curTestSet, LoggingLevels.TestStarter, String.Format("Test has finished, stopping blocking"));
                }

                if (startTest)
                {
                    _logger.WriteToInstrumentationLog(_curTestSet, LoggingLevels.TestStarter, String.Format("Looking for test to start..."));
                    while (true)
                    {
                        // we haven't found a test to run yet, let's look for another one.
                        int startingTest = lastTestStarted++;
                        if (startingTest == _curTestSet.Tests.Length)
                        {
                            // alright, we looped, we don't want to get stuck here forever (when all tests have executed their maximum amount of times)
                            // so we'll break out, check on the time limit / test run limit, and come back to run tests in a bit...
                            lastTestStarted = 0;
                            _logger.WriteToInstrumentationLog(_curTestSet, LoggingLevels.TestStarter, String.Format("Wrapped on test list..."));
                            break;
                        }

                        // we can start this test if it hasn't exceeded the maximum loops and we aren't running too many concurrent copies.
                        ReliabilityTest curTest = _curTestSet.Tests[startingTest];
                        //Console.WriteLine("current test: {0}: {1}", curTest.TestObject, (curTest.TestLoadFailed ? "failed" : "succeeded"));
                        if (!curTest.TestLoadFailed && ((curTest.TestObject != null) ||
                            (_curTestSet.AppDomainLoaderMode == AppDomainLoaderMode.Lazy) ||
                            (_curTestSet.AssemblyLoadContextLoaderMode == AssemblyLoadContextLoaderMode.Lazy)))
                        {
                            bool fLogEntered = false;

                            // test the lock and see if we can enter.
                            Monitor.TryEnter(curTest, 5000, ref fLogEntered);

                            if (!fLogEntered)
                            {
                                fLogEntered = false;
                                _logger.WriteToInstrumentationLog(_curTestSet, LoggingLevels.Tests, String.Format("Test is locked, cannot start {0}", curTest.RefOrID));
                            }

                            lock (curTest)
                            {
                                if (fLogEntered)
                                {
                                    Monitor.Exit(curTest);
                                }

                                // check and make sure it's ok to run the test.

                                bool reachedMaximumRuns = curTest.RunCount >= (curTest.ConcurrentCopies * _curTestSet.MaximumLoops);
                                bool maximumCopiesRunning = curTest.RunningCount >= curTest.ConcurrentCopies;
                                bool testTooLong = false;
                                bool otherGroupTestRunning = false;

                                if (_curTestSet.MaximumLoops == -1)
                                {
                                    reachedMaximumRuns = false;
                                }

                                if (_curTestSet.MaximumTime != 0)
                                {
                                    testTooLong = curTest.ExpectedDuration >= (_curTestSet.MaximumTime - (DateTime.Now.Subtract(_startTime).Ticks / TimeSpan.TicksPerMinute));
                                }

                                if (curTest.Group != null)
                                {
                                    for (int i = 1; i < curTest.Group.Count; i++)
                                    {
                                        ReliabilityTest groupeTest = curTest.Group[i];
                                        if (groupeTest != curTest && groupeTest.RunningCount > 0)
                                        {
                                            otherGroupTestRunning = true;
                                            break;
                                        }
                                    }
                                }

                                if (!reachedMaximumRuns && !maximumCopiesRunning && !testTooLong && !otherGroupTestRunning)
                                {
                                    _logger.WriteTestStart(curTest);
                                    _logger.WriteToInstrumentationLog(_curTestSet, LoggingLevels.TestStarter, String.Format("RUN {0} Test Started: {1}{2} {3}", DateTime.Now, curTest.RefOrID, Environment.NewLine, curTest.Index));
                                    StartTest(_curTestSet.Tests[startingTest]);
                                    lastStart = DateTime.Now;
                                    break;
                                }
                                else
                                {
                                    _logger.WriteToInstrumentationLog(_curTestSet, LoggingLevels.TestStarter, String.Format("Cannot start test {0} Maxruns:{1} MaxCopies:{2} TestTooLong:{3} OtherGroup:{4}{5}", curTest.RefOrID, reachedMaximumRuns, maximumCopiesRunning, testTooLong, otherGroupTestRunning, Environment.NewLine));
                                }
                            }
                        }
                        else
                        {
                            _logger.WriteToInstrumentationLog(_curTestSet, LoggingLevels.TestStarter, String.Format("No test object to start test for index {0}", startingTest));
                        }
                    }
                }
                else
                {
                    Thread.Sleep(250);	// give the CPU a bit of a rest if we don't need to start a new test.
                    if (_curTestSet.DebugBreakOnMissingTest && DateTime.Now.Subtract(_startTime) > minTimeToStartTest)
                    {
                        NewTestsNotStartingDebugBreak();
                    }
                }
            }
            else
            {
                Thread.Sleep(1000);
                _logger.WriteToInstrumentationLog(_curTestSet, LoggingLevels.TestStarter, String.Format("Ran all tests"));
            }
        } while ((_curTestSet.MaximumTime == 0 || // no time limit
            (DateTime.Now.Subtract(_startTime).Ticks / TimeSpan.TicksPerMinute) < _curTestSet.MaximumTime) &&		// or time limit reached
            _testsRanCount < totalTestsToRun);												// maximum loop / test run limit

        /************************************************************************
         * test set is finished...
         */

        TestSetShutdown(totalTestsToRun);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void NewTestsNotStartingDebugBreak()
    {
        MyDebugBreak("Tests haven't been started in a long time!");
    }

    /// <summary>
    /// Shuts down the current test set, waiting for tests to finish, etc...
    /// </summary>
    /// <param name="totalTestsToRun"></param>
    private void TestSetShutdown(int totalTestsToRun)
    {
        // output why we're exiting...
        if (_curTestSet.MaximumTime != 0 && (DateTime.Now.Subtract(_startTime).Ticks / TimeSpan.TicksPerMinute) >= _curTestSet.MaximumTime)
        {
            string msg = String.Format("Reached time limit, exiting: ran {0} tests out of {1}", _testsRanCount, totalTestsToRun);
            _logger.WriteToInstrumentationLog(_curTestSet, LoggingLevels.StartupShutdown, msg);
            Console.WriteLine(msg);
        }
        else if (_testsRanCount >= totalTestsToRun)
        {
            string msg = String.Format("Ran all tests, exiting...");
            _logger.WriteToInstrumentationLog(_curTestSet, LoggingLevels.StartupShutdown, msg);
            Console.WriteLine(msg);
        }

        if (_testsRunningCount > 0)
        {
            string msg = String.Format("Waiting for tests to finish running: {0,4}", _testsRunningCount);
            _logger.WriteToInstrumentationLog(_curTestSet, LoggingLevels.StartupShutdown, msg);
            Console.WriteLine(msg);

            int secondsIter = 5;
            int waitCnt = 0;
            int waitCntTotal = _curTestSet.MaximumWaitTime * 60 / secondsIter;
            msg = String.Format("START WAITING for {0}s", _curTestSet.MaximumWaitTime * 60);
            _logger.WriteToInstrumentationLog(_curTestSet, LoggingLevels.StartupShutdown, msg);
            long lastAllocatedBytes = GC.GetTotalAllocatedBytes(false);
            while (_testsRunningCount > 0 && waitCnt < waitCntTotal)
            {
                Thread.Sleep(secondsIter * 1000);
                long currentAllocatedBytes = GC.GetTotalAllocatedBytes(false);
                msg = String.Format("============current number of tests running {0,4}, allocated {1:n0} so far, {2:n0} since last; (GC {3}:{4}:{5}/{6}:{7}:{8}), waited {9}s",
                    _testsRunningCount, currentAllocatedBytes, (currentAllocatedBytes - lastAllocatedBytes),
                    GC.CollectionCount(0),
                    GC.CollectionCount(1),
                    GC.CollectionCount(2),
                    PrivateCollectionCount(0),
                    PrivateCollectionCount(1),
                    PrivateCollectionCount(2),
                    (waitCnt * secondsIter));
                lastAllocatedBytes = currentAllocatedBytes;
                _logger.WriteToInstrumentationLog(_curTestSet, LoggingLevels.StartupShutdown, msg);

                for (int i = 0; i < _curTestSet.Tests.Length; i++)
                {
                    if (_curTestSet.Tests[i].RunningCount != 0)
                    {
                        msg = String.Format("Still running: {0}", _curTestSet.Tests[i].RefOrID);
                        _logger.WriteToInstrumentationLog(_curTestSet, LoggingLevels.StartupShutdown, msg);
                    }
                }
                waitCnt++;
            }
        }

        // let the user know what tests haven't finished...
        if (_testsRunningCount != 0)
        {
            _logger.WriteToInstrumentationLog(_curTestSet, LoggingLevels.StartupShutdown, "************Timeout reached************");
            for (int i = 0; i < _curTestSet.Tests.Length; i++)
            {
                if (_curTestSet.Tests[i].RunningCount != 0)
                {
                    string msg = String.Format("Still running: {0}", _curTestSet.Tests[i].RefOrID);
                    _logger.WriteToInstrumentationLog(_curTestSet, LoggingLevels.StartupShutdown, msg);
                    Console.WriteLine(msg);
                    AddFailure("Test Hang", _curTestSet.Tests[i], -1);
                }
            }

            if (_curTestSet.DebugBreakOnTestHang)
            {
                TestIsHungDebugBreak();
            }
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void TestIsHungDebugBreak()
    {
        string msg = String.Format("break");
        _logger.WriteToInstrumentationLog(_curTestSet, LoggingLevels.StartupShutdown, msg);

        MyDebugBreak("TestHang");
    }

    /// <summary>
    /// Starts the test passed.  The test should already be loaded into an app domain.
    /// </summary>
    /// <param name="test">The test to run.</param>
    private void StartTest(ReliabilityTest test)
    {
        try
        {
            if (_curTestSet.AssemblyLoadContextLoaderMode == AssemblyLoadContextLoaderMode.Lazy)
            {
                Console.WriteLine("Test Loading: {0} run #{1}", test.RefOrID, test.RunCount);
                TestPreLoader(test, _curTestSet.DiscoveryPaths);
            }

            // Any test failed to load during the preload step should be removed. Need to take a closer look.
            // This is to fix Dev10 Bug 552621.
            if (test.TestLoadFailed)
            {
                _logger.WriteToInstrumentationLog(_curTestSet, LoggingLevels.Tests, "Test failed to load.");
                return;
            }

            Thread newThread = new Thread(new ParameterizedThreadStart(this.StartTestWorker));
            newThread.IsBackground = true;

            // check the thread requirements and set appropriately.
            switch ((test.TestAttrs & TestAttributes.RequiresThread))
            {
                case TestAttributes.RequiresSTAThread:
                    newThread.SetApartmentState(ApartmentState.STA);
                    break;
                case TestAttributes.RequiresMTAThread:
                    newThread.SetApartmentState(ApartmentState.MTA);
                    break;
                case TestAttributes.RequiresUnknownThread:
                    // no attribute specified... ignore.
                    break;
            }

            newThread.Name = test.RefOrID;

            Interlocked.Increment(ref _testsRunningCount);
            test.TestStarted();
            _logger.WriteToInstrumentationLog(_curTestSet, LoggingLevels.TestStarter, String.Format("RF.StartTest, RTs({0}) - Instances of this test: {1} - New Test:{2}, {3} threads",
                _testsRunningCount, test.RunningCount, test.RefOrID, Process.GetCurrentProcess().Threads.Count));

            newThread.Start(test);
        }
        catch (OutOfMemoryException e)
        {
            HandleOom(e, "StartTest");
        }
    }

    /// <summary>
    /// StartTestWorker does the actual work of starting a test.  It should already be running on it's own thread by the time
    /// we call here (because StartTest creates the new thread).
    /// </summary>
    private void StartTestWorker(object test)
    {
        try
        {
            // Update the running time for the stress run
            if (((TimeSpan)DateTime.Now.Subtract(_lastLogTime)).TotalMinutes >= 30)
            {
                _logger.RecordTimeStamp();
                _lastLogTime = DateTime.Now;
            }

            ReliabilityTest daTest = test as ReliabilityTest;

            if (_detourHelpers != null)
            {
                _detourHelpers.SetTestIdName(daTest.Index + 1, daTest.RefOrID);
                _detourHelpers.SetThreadTestId(daTest.Index + 1);
            }

            Debug.Assert(daTest != null);		// if we didn't find the test then there's something horribly wrong!

            daTest.StartTime = DateTime.Now;
            switch (daTest.TestStartMode)
            {
                case TestStartModeEnum.ProcessLoader:
                    Task.Factory.StartNew(() =>
                    {
                        string msg = String.Format("==============================[tid: {0, 4}, running test: {1} STATUS: START, {2} tests running {3} threads ==============================",
                                    Thread.CurrentThread.ManagedThreadId, daTest.Assembly, _testsRunningCount,
                                    Process.GetCurrentProcess().Threads.Count);
                        _logger.WriteToInstrumentationLog(_curTestSet, LoggingLevels.StartupShutdown, msg);

                        try
                        {
                            daTest.EntryPointMethod.Invoke(null, new object[] { (daTest.Arguments == null) ? new string[0] : daTest.GetSplitArguments() });
                        }
                        catch (Exception e)
                        {
                            // crash on exceptions when running as a unit test.
                            if (IsRunningAsUnitTest)
                                Environment.FailFast("Test failed", e);

                            Console.WriteLine(e);
                        }
                        msg = String.Format("==============================[tid: {0, 4}, running test: {1} STATUS: DONE, {2} tests running {3} threads ==============================",
                                    Thread.CurrentThread.ManagedThreadId, daTest.Assembly, _testsRunningCount,
                                    Process.GetCurrentProcess().Threads.Count);
                        _logger.WriteToInstrumentationLog(_curTestSet, LoggingLevels.StartupShutdown, msg);
                        Interlocked.Increment(ref _testsRanCount);
                        SignalTestFinished(daTest);
                    });
                    break;
                case TestStartModeEnum.AppDomainLoader:
                    Console.WriteLine("Appdomain mode is NOT supported for ProjectK");
                    break;
                case TestStartModeEnum.AssemblyLoadContextLoader:
                    try
                    {
                        if (daTest.TestObject is string)
                        {
                            try
                            {
                                int exitCode = daTest.ExecuteInAssemblyLoadContext();

                                // HACKHACK: VSWhidbey bug #113535: Breaking change.  Tests that return a value via Environment.ExitCode
                                // will not have their value propagated back properly via AppDomain.ExecuteAssembly.   These tests will
                                // typically have a return value of 0 (because they have a void entry point).  We will check
                                // Environment.ExitCode and if it's not zero, we'll treat that as our return value (then reset
                                // Env.ExitCode back to 0).

                                if (exitCode == 0 && Environment.ExitCode != 0)
                                {
                                    _logger.WriteTestRace(daTest);
                                    exitCode = Environment.ExitCode;
                                    Environment.ExitCode = 0;
                                }

                                if (exitCode != daTest.SuccessCode)
                                {
                                    AddFailure(String.Format("Test Result ({0}) != Success ({1})", exitCode, daTest.SuccessCode), daTest, exitCode);
                                }
                                else
                                {
                                    AddSuccess("", daTest, exitCode);
                                }
                                _logger.WriteToInstrumentationLog(_curTestSet, LoggingLevels.Tests, String.Format("Test {0} has exited with result {1}", daTest.RefOrID, exitCode));
                            }
                            catch (PathTooLongException)
                            {
                                if (_curTestSet.DebugBreakOnPathTooLong)
                                {
                                    MyDebugBreak("Path too long");
                                }
                            }
                            catch (OutOfMemoryException e)
                            {
                                HandleOom(e, "ExecuteAssembly");
                            }
                            catch (Exception e)
                            {
                                Exception eTemp = e.InnerException;

                                while (eTemp != null)
                                {
                                    if (eTemp is OutOfMemoryException)
                                    {
                                        HandleOom(e, "ExecuteAssembly (inner)");
                                        break;
                                    }

                                    eTemp = eTemp.InnerException;
                                }

                                string err = String.Format("Error in executing test {0}: {1}", daTest.RefOrID, e);

                                if (eTemp == null)
                                {
                                    _logger.WriteToInstrumentationLog(_curTestSet, LoggingLevels.Tests, err);
                                    AddFailure("Failed to ExecuteAssembly (" + e.ToString() + ")", daTest, -1);
                                }

                                // crash on exceptions when running as a unit test.
                                if (IsRunningAsUnitTest)
                                    Environment.FailFast(err, e);
                            }
                        }
                        else if (daTest.TestObject is ISingleReliabilityTest)
                        {
                            try
                            {
                                if (((ISingleReliabilityTest)daTest.TestObject).Run() != true)
                                {
                                    AddFailure("SingleReliabilityTest returned false", daTest, 0);
                                }
                                else
                                {
                                    AddSuccess("", daTest, 1);
                                }
                            }
                            catch (Exception e)
                            {
                                string err = $"Error in executing ISingleReliabilityTest: {e}";

                                Console.WriteLine(err);
                                AddFailure("ISingleReliabilityTest threw exception!", daTest, -1);

                                // crash on exceptions when running as a unit test.
                                if (IsRunningAsUnitTest)
                                    Environment.FailFast(err, e);
                            }
                        }
                        else if (daTest.TestObject is IMultipleReliabilityTest)
                        {
                            try
                            {
                                if (((IMultipleReliabilityTest)daTest.TestObject).Run(0) != true)
                                {
                                    AddFailure("MultipleReliabilityTest returned false", daTest, 0);
                                }
                                else
                                {
                                    AddSuccess("", daTest, 1);
                                }
                            }
                            catch (Exception ex)
                            {
                                string err = $"Error in executing IMultipleReliabilityTest: {ex}";

                                Console.WriteLine(err);
                                AddFailure("IMultipleReliabilityTest threw exception!", daTest, -1);

                                // crash on exceptions when running as a unit test.
                                if (IsRunningAsUnitTest)
                                    Environment.FailFast(err, ex);
                            }
                        }

                        /* Test is finished executing, we need to clean up now... */

                        Interlocked.Increment(ref _testsRanCount);

                        if (_curTestSet.AssemblyLoadContextLoaderMode == AssemblyLoadContextLoaderMode.FullIsolation || _curTestSet.AssemblyLoadContextLoaderMode == AssemblyLoadContextLoaderMode.Lazy)
                        {
                            // we're in full isolation & have test runs left.  we need to
                            // recreate the AssemblyLoadContext so that we don't die on statics.
                            lock (daTest)
                            {
                                if (daTest.HasAssemblyLoadContext)
                                {
                                    _logger.WriteToInstrumentationLog(_curTestSet, LoggingLevels.AssemblyLoadContext, String.Format("Unloading AssemblyLoadContext (locked): {0}", daTest.AssemblyLoadContextName));
                                    daTest.MyLoader = null;
                                    daTest.TestObject = null;
                                    UnloadAssemblyLoadContext(daTest);
                                }
                                if (_curTestSet.MaximumLoops != 1 && _curTestSet.AssemblyLoadContextLoaderMode != AssemblyLoadContextLoaderMode.Lazy)
                                {
                                    TestPreLoader(daTest, _curTestSet.DiscoveryPaths);  // need to reload assembly & AssemblyLoadContext
                                }
                                _logger.WriteToInstrumentationLog(_curTestSet, LoggingLevels.AssemblyLoadContext, String.Format("Unloading complete (freeing lock): {0}", daTest.RefOrID));
                            }
                        }
                        else if ((daTest.RunCount >= (_curTestSet.MaximumLoops * daTest.ConcurrentCopies) ||
                            ((DateTime.Now.Subtract(_startTime).Ticks / TimeSpan.TicksPerMinute > _curTestSet.MaximumTime))) &&
                            (_curTestSet.AssemblyLoadContextLoaderMode != AssemblyLoadContextLoaderMode.RoundRobin))    // don't want to unload domains in round robin mode, we don't know how
                                                                                                                        // many tests are left.
                        {
                            lock (daTest)
                            {   // make sure no one accesses the assembly load context at the same time (between here & RunReliabilityTests)
                                if (daTest.RunningCount == 1 && daTest.HasAssemblyLoadContext)
                                {   // only unload when the last test finishes.
                                    daTest.MyLoader = null;
                                    daTest.TestObject = null;
                                    UnloadAssemblyLoadContext(daTest);
                                    TestPreLoader(daTest, _curTestSet.DiscoveryPaths);  // need to reload assembly & AssemblyLoadContext
                                    RunCommands(daTest.PostCommands, "post", daTest);
                                }
                            }
                        }
                    }
                    finally
                    {
                        SignalTestFinished(daTest);
                    }
                    break;
            }
        }
        catch (Exception e)
        {
            string err = String.Format("Unexpected exception on StartTestWorker: {0}", e);
            _logger.WriteToInstrumentationLog(_curTestSet, LoggingLevels.Tests, err);

            // crash on exceptions when running as a unit test.
            if (IsRunningAsUnitTest)
                Environment.FailFast(err, e);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void UnexpectedThreadAbortDebugBreak()
    {
        MyDebugBreak("Unexpected Thread Abort");
    }

    /// <summary>
    /// Called after a test has finished executing.
    /// </summary>
    /// <param name="test"></param>
    public void SignalTestFinished(ReliabilityTest test)
    {
        Interlocked.Decrement(ref _testsRunningCount);
        _testDone.Set();	// we signal the event before we do the lock() below because the lock could throw due to OOM.

        test.TestStopped();
    }

    /// <summary>
    /// Runs a list of commands stored in an array list.  Logs any failures.
    /// </summary>
    /// <param name="commands">the array list of commands</param>
    /// <param name="commandType">the type of commands (used only for logging)</param>
    private void RunCommands(List<string> commands, string commandType, ReliabilityTest test)
    {
        if (commands != null)
        {
            for (int i = 0; i < commands.Count; i++)
            {
                ProcessStartInfo pi = null;
                Process p = null;
                try
                {
                    Debug.Assert(commands[i] is string, "Non-string in command list!");

                    string torun = ((string)commands[i]).Replace("__ASSEMBLY__", test.Assembly);
                    string args = null;
                    if (torun.IndexOf(' ') != -1)
                    {
                        args = torun.Substring(torun.IndexOf(' ') + 1);
                        torun = torun.Substring(0, torun.IndexOf(' '));
                    }

                    pi = new ProcessStartInfo(torun);
                    if (test.BasePath != String.Empty)
                    {
                        pi.WorkingDirectory = test.BasePath;
                    }
                    if (args != null)
                    {
                        pi.Arguments = args;
                    }
                    pi.UseShellExecute = true;
                    p = Process.Start(pi);
                    p.WaitForExit();
                }
                catch
                {
                    _logger.WritePreCommandFailure(test, (string)commands[i], commandType);
                }
                finally
                {
                    if (null != p)
                    {
                        p.Dispose();
                    }
                }
            }
        }
    }
    /// <summary>
    /// Loads the specified test into a new app domain.  Returns true on success and false on failure.
    /// </summary>
    /// <param name="test">the test to execute</param>
    /// <param name="paths">paths where to search for the assembly if it's not found.</param>
    /// <returns>true if the test was successfully loaded & executed, false otherwise.</returns>
    private void TestPreLoader(ReliabilityTest test, string[] paths)
    {
        try
        {
            RunCommands(test.PreCommands, "pre", test);

            List<string> newPaths = new List<string>(paths);
            string bvtRoot = Environment.GetEnvironmentVariable("BVT_ROOT");
            if (bvtRoot != null)
            {
                newPaths.Add(bvtRoot);
            }

            string coreRoot = Environment.GetEnvironmentVariable("CORE_ROOT");
            if (coreRoot != null)
            {
                newPaths.Add(coreRoot);
            }

            string thisRoot = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Tests");
            newPaths.Add(thisRoot);

            switch (test.TestStartMode)
            {
                case TestStartModeEnum.ProcessLoader:
                    TestPreLoader_Process(test, newPaths.ToArray());
                    break;
                case TestStartModeEnum.AssemblyLoadContextLoader:
                    TestPreLoader_AssemblyLoadContext(test, newPaths.ToArray());
                    break;
                case TestStartModeEnum.AppDomainLoader:
                    Console.WriteLine("Appdomain mode is NOT supported for ProjectK");
                    break;
            }
        }
        catch (Exception e)
        {
            string msg = String.Format("\r\nBad test ({1} - {3}): {0}\r\n{2}\r\n{4}", test.RefOrID, e.GetType(), waitingText, e.Message, e.StackTrace);
            _logger.WriteToInstrumentationLog(_curTestSet, LoggingLevels.Tests, msg);
            test.ConcurrentCopies = 0;
            test.TestLoadFailed = true;
            if (_curTestSet.DebugBreakOnBadTest)
            {
                BadTestDebugBreak(msg);
            }

            // crash on exceptions when running as a unit test.
            if (IsRunningAsUnitTest)
                Environment.FailFast(msg, e);
        }
        Interlocked.Decrement(ref LoadingCount);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void BadTestDebugBreak(string msg)
    {
        MyDebugBreak(msg);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    WeakReference UnloadAssemblyLoadContextInner(ReliabilityTest test)
    {
        WeakReference alcRef = new WeakReference(test.AssemblyLoadContext);
        test.AssemblyLoadContext.Unload();
        test.AssemblyLoadContext = null;

        return alcRef;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    void UnloadAssemblyLoadContext(ReliabilityTest test)
    {
        WeakReference alcWeakRef = UnloadAssemblyLoadContextInner(test);
        for (int i = 0; (i < 8) && alcWeakRef.IsAlive; i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        if (alcWeakRef.IsAlive)
        {
            TestAssemblyLoadContext alc = (TestAssemblyLoadContext)alcWeakRef.Target;
            if (alc != null)
            {
                _logger.WriteToInstrumentationLog(_curTestSet, LoggingLevels.AssemblyLoadContext, "FAILED unloading AssemblyLoadContext: " + alc.FriendlyName + " for " + test.Index.ToString());
                return;
            }
        }
        _logger.WriteToInstrumentationLog(_curTestSet, LoggingLevels.AssemblyLoadContext, "SUCCEED unloading AssemblyLoadContext for " + test.Index.ToString());
    }

    /// <summary>
    /// Pre-loads a test into the correct AssemblyLoadContext for the current loader mode.
    /// This method behaves in the same way as the TestPreLoader_AppDomain, the difference
    /// (besides the AssemblyLoadContext vs AppDomain creation differences) is that it uses
    /// reflection to get and invoke the methods on the LoaderClass loaded into
    /// the AssemblyLoadContext.
    /// </summary>
    /// <param name="test"></param>
    /// <param name="paths"></param>
    void TestPreLoader_AssemblyLoadContext(ReliabilityTest test, string[] paths)
    {
        TestAssemblyLoadContext alc = null;

        try
        {
            if (_curTestSet.AssemblyLoadContextLoaderMode != AssemblyLoadContextLoaderMode.RoundRobin || test.CustomAction == CustomActionType.LegacySecurityPolicy)
            {
                // TODO: can there be a parent ALC whose name we would like to prepend?
                string assemblyLoadContextName = "TestContext_" + test.Assembly + "_" + Guid.NewGuid().ToString();
                _logger.WriteToInstrumentationLog(_curTestSet, LoggingLevels.AssemblyLoadContext, "Creating AssemblyLoadContext: " + assemblyLoadContextName + " for " + test.Index.ToString());

                alc = new TestAssemblyLoadContext(assemblyLoadContextName, test.BasePath, paths);
            }
            else
            {
                alc = _testALCs[test.AssemblyLoadContextIndex];
                alc.SetPaths(test.BasePath, paths);
            }

            AssemblyName an = new AssemblyName();
            Object ourObj = null;

            test.AssemblyLoadContext = alc;
            Assembly testAssembly = alc.LoadFromAssemblyPath(Assembly.GetExecutingAssembly().Location);
            object obj = testAssembly.CreateInstance(typeof(LoaderClass).FullName);
            Type loaderClassType = testAssembly.GetType(typeof(LoaderClass).FullName);

            MethodInfo[] methods = loaderClassType.GetMethods();

            MethodInfo suppressConsoleMethod = loaderClassType.GetMethod("SuppressConsole");
            MethodInfo loadMethod = loaderClassType.GetMethod("Load");
            MethodInfo loadFromMethod = loaderClassType.GetMethod("LoadFrom");
            MethodInfo checkMainForThreadTypeMethod = loaderClassType.GetMethod("CheckMainForThreadType");
            MethodInfo getTestMethod = loaderClassType.GetMethod("GetTest");

            if (test.SuppressConsoleOutput)
                suppressConsoleMethod.Invoke(obj, new object[0]);


            if (test.Assembly.ToLower().IndexOf(".exe") == -1 && test.Assembly.ToLower().IndexOf(".dll") == -1)	// must be a simple name or fullname...
            {
                loadMethod.Invoke(obj, new object[] { test.Assembly, paths });
            }
            else			// has an executable extension, must be in local directory.
            {
                loadFromMethod.Invoke(obj, new object[] { Path.Combine(test.BasePath, test.Assembly), paths });
            }

            // check and see if this test is marked as requiring STA.  We only do
            // the check once, and then we set the STA/MTA/Unknown bit on the test attributes
            // to avoid doing reflection every time we start the test.
            if ((test.TestAttrs & TestAttributes.RequiresThread) == TestAttributes.None)
            {
                ApartmentState state = (ApartmentState)(int)checkMainForThreadTypeMethod.Invoke(obj, new object[0]);
                switch (state)
                {
                    case ApartmentState.STA:
                        test.TestAttrs |= TestAttributes.RequiresSTAThread;
                        break;
                    case ApartmentState.MTA:
                        test.TestAttrs |= TestAttributes.RequiresMTAThread;
                        break;
                    case ApartmentState.Unknown:
                        test.TestAttrs |= TestAttributes.RequiresUnknownThread;
                        break;

                }
            }

            ourObj = getTestMethod.Invoke(obj, new object[0]);
            IEnumerable<Type> interfaces = ourObj.GetType().GetTypeInfo().ImplementedInterfaces;

            Type iSingleReliabilityTestType = testAssembly.GetType(typeof(ISingleReliabilityTest).FullName);
            Type iMultipleReliabilityTestType = testAssembly.GetType(typeof(IMultipleReliabilityTest).FullName);

            if (interfaces.Contains(iSingleReliabilityTestType))
            {
                iSingleReliabilityTestType.InvokeMember("Register", BindingFlags.InvokeMethod | BindingFlags.Public, null, ourObj, new object[0]);
            }
            else if (interfaces.Contains(iMultipleReliabilityTestType))
            {
                iMultipleReliabilityTestType.InvokeMember("Register", BindingFlags.InvokeMethod | BindingFlags.Public, null, ourObj, new object[0]);
            }
            else if (!(ourObj is string))	// we were unable to find a test here - a string is an executable filename.
            {
                Interlocked.Decrement(ref LoadingCount);
                return;
            }

            test.TestObject = ourObj;
            test.MyLoader = obj;
        }
        catch (Exception)
        {
            // if we took an exception while loading the test, but we still have an app domain
            // we don't want to leak the app domain.
            if (alc != null)
            {
                alc = null;
                UnloadAssemblyLoadContext(test);
            }
            throw;
        }
    }

    /// <summary>
    /// Preloads a test for a process, this just sets the test object to the appropriate path.
    /// </summary>
    /// <param name="test"></param>
    /// <param name="paths"></param>
    private void TestPreLoader_Process(ReliabilityTest test, string[] paths)
    {
        string realpath = ReliabilityConfig.ConvertPotentiallyRelativeFilenameToFullPath(test.BasePath, test.Assembly);
        Debug.Assert(test.TestObject == null);
        if (File.Exists(realpath))
        {
            test.TestObject = realpath;
        }
        else if (File.Exists((string)test.Assembly))
        {
            test.TestObject = test.Assembly;
        }
        else
        {
            foreach (string path in paths)
            {
                string fullPath = ReliabilityConfig.ConvertPotentiallyRelativeFilenameToFullPath(path, (string)test.Assembly);
                if (File.Exists(fullPath))
                {
                    test.TestObject = fullPath;
                    break;
                }
            }
        }

        if (test.TestObject == null)
        {
            Console.WriteLine("Couldn't find path for {0}", test.Assembly);
        }

        if (test.EntryPointMethod == null)
        {
            CustomAssemblyResolver resolver = new CustomAssemblyResolver();
            // test.Assembly is with the extension. LoadFromAssemblyName needs it without.
            string strAssemblyNameWithoutExt = Path.ChangeExtension(test.Assembly, null);
            Assembly testAssembly = resolver.LoadFromAssemblyName(new AssemblyName(strAssemblyNameWithoutExt));
            Type[] testTypes = AssemblyExtensions.GetTypes(testAssembly);
            MethodInfo methodInfo = null;

            if (test.SuppressConsoleOutput)
            {
                // Console.SetOut(System.IO.TextWriter.Null);
                Assembly consoleAssembly = resolver.LoadFromAssemblyPath(typeof(Console).Assembly.Location);
                Type consoleType = consoleAssembly.GetType("System.Console");
                MethodInfo setOutMethod = consoleType.GetMethod("SetOut", BindingFlags.Public | BindingFlags.Static);
                Type textWriterType = setOutMethod.GetParameters()[0].ParameterType;
                FieldInfo nullField = textWriterType.GetField("Null", BindingFlags.Public | BindingFlags.Static);
                object nullInstance = nullField.GetValue(null);
                setOutMethod.Invoke(null, new object[] { nullInstance });
            }

            if (testTypes != null)
            {
                foreach (Type t in testTypes)
                {
                    methodInfo = t.GetMethod("Main");
                    if (methodInfo != null)
                    {
                        //Console.WriteLine(t.FullName + " contains the entrypoint");
                        break;
                    }
                }
            }

            test.EntryPointMethod = methodInfo;
        }
    }
    /// <summary>
    /// This method will send a failure message to the test owner that their test has failed.
    /// </summary>
    /// <param name="testCase">the test case which failed</param>
    /// <param name="returnCode">return code of the test, -1 for none provided</param>
    private void SendFailMail(ReliabilityTest testCase, string message)
    {
        //SendFailMail(testCase, message, null, null, null);
    }

    /*
    /// <summary>
    /// This method will send a failure message to the test owner that their test has failed.
    /// </summary>
    /// <param name="testCase">the test case which failed</param>
    /// <param name="returnCode">return code of the test, -1 for none provided</param>
    void SendFailMail(ReliabilityTest testCase, string message, string subject, string to, string body)
    {
        // we only want to send fail mails once / test / run, so we mark a failed test
        // and won't send any additional e-mails once it's failed.
        if (testCase == null || !testCase.HasFailed)
        {
            if (testCase != null)
            {
                testCase.HasFailed = true;
            }
            try
            {
#pragma warning disable 618
                MailMessage mail = new MailMessage();
#pragma warning restore
                if (subject != null)
                {
                    mail.Subject = subject;
                }
                else
                {
                    mail.Subject = "ACTION REQUIRED::Follow up on stress failures";
                }
                mail.From = "corbvt@microsoft.com";

                if (to == null)
                {
                    if (testCase != null && testCase.TestOwner != null)
                    {
                        string[] failMailReceivers = testCase.TestOwner.Split(new char[] { ';' });

                        //convert aliases into full e-mail addresses
                        foreach (string failReceiver in failMailReceivers)
                        {
                            if (failReceiver.IndexOf("@") != -1)
                            {
                                mail.To = failReceiver;
                            }
                            else
                            {
                                mail.To = String.Format("{0}@microsoft.com", failReceiver);
                            }
                        }
                    }
                    else
                    {
                        mail.To = "dinov@microsoft.com";
                    }
                }
                else
                {
                    mail.To = to;
                }

                if (_curTestSet.CCFailMail != null)
                {
                    mail.Cc = _curTestSet.CCFailMail;
                }

#pragma warning disable 0618
                mail.BodyFormat = MailFormat.Html;
                mail.Priority = MailPriority.High;
#pragma warning restore
                if (body == null)
                {
                    mail.Body = String.Format(@"
<HTML><BODY><H2>Please investigate your test failures ASAP</H2>

<table border=1 cellspacing=0>
<tr><td bgcolor=#cccccc>Computer Name:</td><td> {0}</td></tr>
<tr><td bgcolor=#cccccc>Test         :</td><td> {1} {2}</td></tr>
<tr><td bgcolor=#cccccc>Comments	 :</td><td> {3}</td></tr>
</table>

<P>If you are listed on the To: line, you have test failures to investigate.

<p>For all failures please find the machine listed above on the <a href=""http://urtframeworks/stress/stressdetails.aspx?team=CLR"">CLR Stress Details Web Page</a> and open a tracking bug if one has not already been created for this stress run.

<p>If this is a product failure please e-mail the
<a href=""mailto:corqrd"">CLR Quick Response Dev Team</a> with the failure information and tracking bug number.  The QRT will then open a product bug if appropriate and resolve the tracking bug as a duplicate.

<p>If this is a test failure please open a tracking bug via the CLR Stress Details web page and assign if to yourself.  Resolve the bug once you have fixed the test issue.

<p>If this is a stress harness issue please contact <a href=""mailto:timme;dinov"">the stress developers</a>.

Thanks for contributing to CLR Stress!
	</P></BODY></HTML>", Environment.MachineName, testCase == null ? "None" : testCase.Assembly, testCase == null ? "None" : testCase.Arguments, message);
                }
                else
                {
                    mail.Body = body;
                }
#pragma warning disable 0618
                SmtpMail.SmtpServer = "smarthost";
                SmtpMail.Send(mail);
#pragma warning restore
            }
            catch (Exception e)
            {
                Console.WriteLine("Error sending fail mail: {0}", e);
            }
        }
    }
    */
    /// <summary>
    /// Add a failure to the failure log.
    /// </summary>
    /// <param name="failMsg">the message to add (the reason the failure happened)</param>
    /// <param name="test">the test that failed</param>
    private void AddFailure(string failMsg, ReliabilityTest test, int returnCode)
    {
        // bump the fail count
        Interlocked.Increment(ref _failCount);

        // log the failure
        _logger.WriteTestFail(test, failMsg);

        // report results back to harnesses / urt frameworks.
        _totalSuccess = false;

        try
        {
            if (_curTestSet.ReportResults && File.Exists(Environment.ExpandEnvironmentVariables("%SCRIPTSDIR%\\record.js")))
            {
                string arguments;
                if (test == null)
                {
                    arguments = String.Format("//b //nologo %SCRIPTSDIR%\\record.js -i %STRESSID% -a ADD_CUSTOM -s RUNNING -k FAIL{0:000} -v \"(non test failure)\"", _reportedFailCnt++);
                }
                else
                {
                    arguments = String.Format("//b //nologo %SCRIPTSDIR%\\record.js -i %STRESSID% -a ADD_CUSTOM -s RUNNING -k FAIL{0:000} -v \"{1} ({2} ReturnCode={3})\"", _reportedFailCnt++, test.RefOrID, test.TestOwner, returnCode);
                }
                ProcessStartInfo psi = new ProcessStartInfo("cscript.exe", Environment.ExpandEnvironmentVariables(arguments));
                psi.UseShellExecute = false;
                psi.RedirectStandardOutput = true;

                Process p = Process.Start(psi);
                p.StandardOutput.ReadToEnd();
                p.WaitForExit();
                if (p.ExitCode != 0)
                {
                    Console.WriteLine("cscript.exe " + Environment.ExpandEnvironmentVariables("//b //nologo %SCRIPTSDIR%\\record.js -i %STRESSID% -a UPDATE_RECORD -s RUNNING"));
                    Console.WriteLine("WARNING: Status update did not return success! {0}", p.ExitCode);
                }
                p.Dispose();
            }
            else
            {
                _logger.LogNoResultReporter(_curTestSet.ReportResults);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("WARNING: Status update did not return success (exception thrown {0})!", e);
        }

        if (test == null)
        {
            //SendFailMail(test,failMsg);
        }
    }

    /// <summary>
    /// Report that a test has successfully passed
    /// </summary>
    /// <param name="passMsg">an additional message about the test passing</param>
    /// <param name="testRefOrID">the test which passed</param>
    private void AddSuccess(string passMsg, ReliabilityTest test, int returnCode)
    {
        // log the failure
        _logger.WriteTestPass(test, passMsg);
    }



    /// <summary>
    /// This method will find the test with the given refOrID and return it's index into the current test set.
    /// </summary>
    /// <param name="id">refOrId of test</param>
    /// <returns>the index into the test set, or -1 if the test cannot be found.</returns>
    private int FindTestByID(string id)
    {
        // found tests must be initialized by ExecuteFromLog.  This means that if we were to do multiple runs in one process
        // foundTests must be re-initialized.
        if (_foundTests == null)
        {
            return (-1);
        }

        if (_foundTests[id] != null)
        {
            return ((int)_foundTests[id]);
        }

        for (int i = 0; i < _curTestSet.Tests.Length; i++)
        {
            if (_curTestSet.Tests[i].RefOrID == id)
            {
                //found the test, store it in our hashtable
                // for quick access, and return the test ID.
                _foundTests[id] = i;
                return (i);
            }
        }

        // couldn't find the test. stop us from doing a search in
        // the future, and return -1
        _foundTests[id] = -1;
        return (-1);
    }

    private string ExtractAttribute(string attribute, string from)
    {
        int attrStart = from.IndexOf(attribute);
        string value = from.Substring(attrStart + attribute.Length + 2);			// +2 is for = and "
        return (value.Substring(0, value.IndexOf('"')));
    }

    /// <summary>
    /// This method will do a reliability run from a pre-generated log file.  We will execute all the tests with the same amount of time
    /// between starts as the previous run.  We will also execute all the same tests.  ExecuteFromLog requires that the same test set
    /// is already loaded into curTestSet.  If the test entries in the test set have been modified (eg, entries removed, renamed, or had
    /// their parameters altered) you will get different results.  Changing the order will not effect the re-run.
    /// </summary>
    /// <param name="logFile"></param>
    private void ExecuteFromLog(string logFile)
    {
        FileStream inputFile;
        StreamReader inputReader;
        string input;

        try
        {
            inputFile = File.OpenRead(logFile);
            inputReader = new StreamReader(inputFile);
        }
        catch (Exception e)
        {
            Console.WriteLine("Unable to open input file: {0}", logFile);
            Console.WriteLine("Exception thrown: {0} {1}", e.GetType(), e.Message);
            return;
        }

        _foundTests = new Hashtable();
        DateTime baseTime = DateTime.MinValue;
        DateTime startTime = DateTime.MaxValue;

        const string randSeedText = "<RandomSeed>";
        const string testRunText = "<TestRun>";
        const string startupInfoText = "<StartupInfo>";
        const string startupInfoCloseText = "</StartupInfo>";
        const string testStartText = "<TestStart ";
        const string testPassText = "<TestPass ";
        const string testFailText = "<TestFail ";
        const string testRaceText = "<TestRace ";
        const string dateTimeText = "DateTime";
        const string testIdText = "TestId";

        while ((input = inputReader.ReadLine()) != null)
        {
            string inputLine = input.Trim();

            if (String.Compare(inputLine, testRunText) == 0)
            {
                continue;
            }
            else if (String.Compare(inputLine, startupInfoText) == 0)
            {
                while ((input = inputReader.ReadLine()) != null)
                {
                    inputLine = input.Trim();
                    if (String.Compare(inputLine, startupInfoCloseText) == 0)
                    {
                        break;
                    }
                    else if (String.Compare(inputLine, 0, randSeedText, 0, randSeedText.Length) == 0)
                    {
                        int seed = Convert.ToInt32(inputLine.Substring(randSeedText.Length, inputLine.IndexOf('<', randSeedText.Length) - randSeedText.Length));
                        s_randNum = new Random(seed);
                    }
                    else
                    {
                        Console.WriteLine("Ignoring unrecognized input: {0}", inputLine);
                    }
                }
                continue;
            }

            else if (String.Compare(inputLine, 0, testStartText, 0, testStartText.Length) == 0)
            {
                // start a test.
                string dateTime = ExtractAttribute(dateTimeText, input);
                string id = ExtractAttribute(testIdText, input);

                DateTime thisTime = DateTime.Parse(dateTime);
                int curTest = FindTestByID(id);

                if (curTest != -1)
                {
                    // wait until the time is appropriate.
                    if (baseTime == DateTime.MinValue)
                    {
                        baseTime = thisTime;    // this is the 1st run command, this is our base time.
                        startTime = DateTime.Now;
                    }
                    else
                    {
                        if ((thisTime.Subtract(baseTime)) > (DateTime.Now.Subtract(startTime)))
                        {
                            // sleep for (thisTime - baseTime) - (DateTime.Now - startTime)
                            Thread.Sleep((int)(thisTime.Subtract(baseTime).Subtract(DateTime.Now.Subtract(startTime)).Ticks / TimeSpan.TicksPerMillisecond));
                        }
                    }

                    StartTest(_curTestSet.Tests[curTest]);
                }
            }
            else if (String.Compare(inputLine, 0, testPassText, 0, testPassText.Length) == 0 ||
                String.Compare(inputLine, 0, testFailText, 0, testFailText.Length) == 0 ||
                String.Compare(inputLine, 0, testRaceText, 0, testRaceText.Length) == 0)
            {
                // opening <TestRun> tag.
                continue;
            }
        }

        while (_testsRunningCount != 0)	// let the user know what tests haven't finished...
        {
            Console.WriteLine(".");
            Thread.Sleep(2000);
        }
    }
}
