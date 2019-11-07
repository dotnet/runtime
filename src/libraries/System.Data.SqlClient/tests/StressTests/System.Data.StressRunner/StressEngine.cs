// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;
using Monitoring;

namespace DPStressHarness
{
    public class StressEngine
    {
        private Random _rnd;
        private int _threads;
        private int _duration;
        private int _threadsRunning;
        private bool _continue;
        private List<StressTest> _allTests;
        private RecordedExceptions _exceptions = new RecordedExceptions();
        private PerfCounters _perfcounters = null;
        private static long s_globalRequestsCounter = 0;

        public StressEngine(int threads, int duration, IEnumerable<TestBase> allTests, int seed)
        {
            if (seed != 0)
            {
                _rnd = new Random(seed);
            }
            else
            {
                Random rndBootstrap = new Random();

                seed = rndBootstrap.Next();

                _rnd = new Random(seed);
            }

            Console.WriteLine("Seeding stress engine random number generator with {0}\n", seed);


            _threads = threads;
            _duration = duration;
            _allTests = new List<StressTest>();

            List<StressTest> tmpWeightedLookup = new List<StressTest>();

            foreach (TestBase t in allTests)
            {
                if (t is StressTest)
                {
                    _allTests.Add(t as StressTest);
                }
            }

            try
            {
                _perfcounters = new PerfCounters();
            }
            catch (Exception e)
            {
                Console.WriteLine("Warning: An error occurred initializing performance counters. Performance counters can only be initialized when running with Administrator privileges. Error Message: " + e.Message);
            }
        }

        public int Run()
        {
            TraceListener listener = new TextWriterTraceListener(Console.Out);
            Trace.Listeners.Add(listener);
            Trace.UseGlobalLock = true;

            _threadsRunning = 0;
            _continue = true;

            if (_allTests.Count == 0)
            {
                throw new ArgumentException("The specified assembly doesn't contain any tests to run. Test methods must be decorated with a Test, StressTest, MultiThreadedTest, or ThreadPoolTest attribute.");
            }

            // Run any global setup
            StressTest firstStressTest = _allTests.Find(t => t is StressTest);
            if (null != firstStressTest)
            {
                firstStressTest.RunGlobalSetup();
            }

            //Monitoring Start
            IMonitorLoader _monitorloader = null;
            if (TestMetrics.MonitorEnabled)
            {
                _monitorloader = MonitorLoader.LoadMonitorLoaderAssembly();
                if (_monitorloader != null)
                {
                    _monitorloader.Enabled = TestMetrics.MonitorEnabled;
                    _monitorloader.HostMachine = TestMetrics.MonitorMachineName;
                    _monitorloader.TestName = firstStressTest.Title;
                    _monitorloader.Action(MonitorLoaderUtils.MonitorAction.Start);
                }
            }

            for (int i = 0; i < _threads; i++)
            {
                Interlocked.Increment(ref _threadsRunning);
                Thread t = new Thread(new ThreadStart(this.RunStressThread));
                t.Start();
            }

            while (_threadsRunning > 0)
            {
                Thread.Sleep(1000);
            }

            //Monitoring Stop
            if (TestMetrics.MonitorEnabled)
            {
                if (_monitorloader != null)
                    _monitorloader.Action(MonitorLoaderUtils.MonitorAction.Stop);
            }

            // Run any global cleanup
            if (null != firstStressTest)
            {
                firstStressTest.RunGlobalCleanup();
            }

            // Write out all exceptions
            _exceptions.TraceAllExceptions();
            return _exceptions.GetExceptionsCount();
        }


        public void RunStressThread()
        {
            try
            {
                StressTest[] tests = new StressTest[_allTests.Count];
                List<int> tmpWeightedLookup = new List<int>();

                for (int i = 0; i < tests.Length; i++)
                {
                    tests[i] = _allTests[i].Clone();
                    tests[i].RunSetup();

                    for (int j = 0; j < tests[i].Weight; j++)
                    {
                        tmpWeightedLookup.Add(i);
                    }
                }

                int[] weightedLookup = tmpWeightedLookup.ToArray();

                Stopwatch timer = new Stopwatch();
                long testDuration = (long)_duration * Stopwatch.Frequency;

                timer.Reset();
                timer.Start();

                while (_continue && timer.ElapsedTicks < testDuration)
                {
                    int n = _rnd.Next(0, weightedLookup.Length);
                    StressTest t = tests[weightedLookup[n]];

                    if (TestMetrics.PrintMethodName)
                    {
                        FakeConsole.WriteLine("{0}: {1}", ++s_globalRequestsCounter, t.Title);
                    }

                    try
                    {
                        DeadlockDetection.AddTestThread();
                        t.Run();
                        if (_perfcounters != null)
                            _perfcounters.IncrementRequestsCounter();
                    }
                    catch (Exception e)
                    {
                        if (_perfcounters != null)
                            _perfcounters.IncrementExceptionsCounter();

                        t.HandleException(e);

                        bool thresholdExceeded = _exceptions.Record(t.Title, e);
                        if (thresholdExceeded)
                        {
                            FakeConsole.WriteLine("Exception Threshold of {0} has been exceeded on {1} - Halting!\n",
                                TestMetrics.ExceptionThreshold, t.Title);
                            break;
                        }
                    }
                    finally
                    {
                        DeadlockDetection.RemoveThread();
                    }
                }

                foreach (StressTest t in tests)
                {
                    t.RunCleanup();
                }
            }
            finally
            {
                _continue = false;
                Interlocked.Decrement(ref _threadsRunning);
            }
        }
    }
}
