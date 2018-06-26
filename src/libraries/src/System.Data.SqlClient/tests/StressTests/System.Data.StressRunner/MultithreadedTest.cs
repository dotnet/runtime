// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Diagnostics;
using System.Threading;

namespace DPStressHarness
{
    internal class MultiThreadedTest : TestBase
    {
        private MultiThreadedTestAttribute _attr;
        public static bool _continue;
        public static int _threadsRunning;
        public static int _rps;
        public static Exception _firstException = null;

        private struct TestInfo
        {
            public object _instance;
            public TestMethodDelegate _delegateTest;
        }

        public MultiThreadedTest(MultiThreadedTestAttribute attr,
                                 MethodInfo testMethodInfo,
                                 Type type,
                                 List<MethodInfo> setupMethods,
                                 List<MethodInfo> cleanupMethods)
            : base(attr, testMethodInfo, type, setupMethods, cleanupMethods)

        {
            _attr = attr;
        }

        public override void Run()
        {
            try
            {
                Stopwatch timer = new Stopwatch();
                long warmupDuration = (long)_attr.WarmupDuration * Stopwatch.Frequency;
                long testDuration = (long)_attr.TestDuration * Stopwatch.Frequency;
                int threads = _attr.Threads;

                TestInfo[] info = new TestInfo[threads];
                ConstructorInfo targetConstructor = _type.GetConstructor(Type.EmptyTypes);

                for (int i = 0; i < threads; i++)
                {
                    info[i] = new TestInfo();
                    info[i]._instance = targetConstructor.Invoke(null);
                    info[i]._delegateTest = CreateTestMethodDelegate();

                    SetVariations(info[i]._instance);
                    ExecuteSetupPhase(info[i]._instance);
                }

                _firstException = null;
                _continue = true;
                _rps = 0;

                for (int i = 0; i < threads; i++)
                {
                    Interlocked.Increment(ref _threadsRunning);
                    Thread t = new Thread(new ParameterizedThreadStart(MultiThreadedTest.RunThread));
                    t.Start(info[i]);
                }

                timer.Reset();
                timer.Start();

                while (timer.ElapsedTicks < warmupDuration)
                {
                    Thread.Sleep(1000);
                }

                int warmupRequests = Interlocked.Exchange(ref _rps, 0);
                timer.Reset();
                timer.Start();
                TestMetrics.StartCollection();

                while (timer.ElapsedTicks < testDuration)
                {
                    Thread.Sleep(1000);
                }

                int requests = Interlocked.Exchange(ref _rps, 0);
                double elapsedSeconds = timer.ElapsedTicks / Stopwatch.Frequency;
                TestMetrics.StopCollection();
                _continue = false;

                while (_threadsRunning > 0)
                {
                    Thread.Sleep(1000);
                }

                for (int i = 0; i < threads; i++)
                {
                    ExecuteCleanupPhase(info[i]._instance);
                }

                double rps = (double)requests / elapsedSeconds;

                if (_firstException == null)
                {
                    LogTest(rps);
                }
                else
                {
                    LogTestFailure(_firstException.ToString());
                }
            }
            catch (TargetInvocationException e)
            {
                LogTestFailure(e.InnerException.ToString());
            }
            catch (Exception e)
            {
                LogTestFailure(e.ToString());
            }
        }


        public static void RunThread(object state)
        {
            try
            {
                while (_continue)
                {
                    TestInfo info = (TestInfo)state;
                    info._delegateTest(info._instance);
                    Interlocked.Increment(ref _rps);
                }
            }
            catch (Exception e)
            {
                if (_firstException == null)
                {
                    _firstException = e;
                }
                _continue = false;
            }
            finally
            {
                Interlocked.Decrement(ref _threadsRunning);
            }
        }

        protected void LogTest(double rps)
        {
            Logger logger = new Logger(TestMetrics.RunLabel, TestMetrics.IsOfficial, TestMetrics.Milestone, TestMetrics.Branch);
            logger.AddTest(this.Title);

            LogStandardMetrics(logger);

            logger.AddTestMetric(Constants.TEST_METRIC_RPS, string.Format("{0:F2}", rps), "rps", true);

            logger.Save();

            Console.WriteLine("{0}: Requests per Second={1:F2}, Working Set={2}, Peak Working Set={3}, Private Bytes={4}",
                              this.Title,
                              rps,
                              TestMetrics.WorkingSet,
                              TestMetrics.PeakWorkingSet,
                              TestMetrics.PrivateBytes);
        }
    }
}