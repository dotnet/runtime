// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Diagnostics;
using System.Runtime.InteropServices;


namespace DPStressHarness
{
    internal class Test : TestBase
    {
        private TestAttribute _attr;
        private int _overrideIterations = -1;
        private int _overrideWarmup = -1;

        public Test(TestAttribute attr,
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
                // create an instance of the class that defines the test method.
                object targetInstance = _type.GetConstructor(Type.EmptyTypes).Invoke(null);

                SetVariations(targetInstance);

                ExecuteSetupPhase(targetInstance);

                TestMethodDelegate tmd = CreateTestMethodDelegate();

                ExecuteTest(targetInstance, tmd);

                ExecuteCleanupPhase(targetInstance);

                LogTest();
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

        protected void LogTest()
        {
            Logger logger = new Logger(TestMetrics.RunLabel, TestMetrics.IsOfficial, TestMetrics.Milestone, TestMetrics.Branch);
            logger.AddTest(this.Title);

            LogStandardMetrics(logger);

            logger.AddTestMetric(Constants.TEST_METRIC_ELAPSED_SECONDS, string.Format("{0:F2}", TestMetrics.ElapsedSeconds), "sec", false);

            logger.Save();

            Console.WriteLine("{0}: Elapsed Seconds={1:F2}, Working Set={2}, Peak Working Set={3}, Private Bytes={4}",
                              this.Title,
                              TestMetrics.ElapsedSeconds,
                              TestMetrics.WorkingSet,
                              TestMetrics.PeakWorkingSet,
                              TestMetrics.PrivateBytes);
        }


        private void ExecuteTest(object targetInstance, TestMethodDelegate tmd)
        {
            int warmupIterations = _attr.WarmupIterations;
            int testIterations = _attr.TestIterations;

            if (_overrideIterations >= 0)
            {
                testIterations = _overrideIterations;
            }
            if (_overrideWarmup >= 0)
            {
                warmupIterations = _overrideWarmup;
            }

            /** do some cleanup to make memory tests more accurate **/
            System.GC.Collect();
            System.GC.WaitForPendingFinalizers();
            System.GC.Collect();

            IntPtr h = MemApi.GetCurrentProcess();
            bool fRes = MemApi.SetProcessWorkingSetSize(h, -1, -1);
            /****/

            System.Threading.Thread.Sleep(10000);

            for (int i = 0; i < warmupIterations; i++)
            {
                tmd(targetInstance);
            }

            TestMetrics.StartCollection();
            for (int i = 0; i < testIterations; i++)
            {
                tmd(targetInstance);
            }
            TestMetrics.StopCollection();
        }
    }
}
