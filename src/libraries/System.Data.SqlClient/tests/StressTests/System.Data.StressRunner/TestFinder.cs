// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.IO;

namespace DPStressHarness
{
    internal class TestFinder
    {
        private static AssemblyName s_assemblyName;

        public static AssemblyName AssemblyName
        {
            get { return s_assemblyName; }
            set { s_assemblyName = value; }
        }

        public static IEnumerable<TestBase> GetTests(Assembly assembly)
        {
            List<TestBase> tests = new List<TestBase>();


            Type[] typesInModule = null;
            try
            {
                typesInModule = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                Console.WriteLine("ReflectionTypeLoadException Errors");
                foreach (Exception loadEx in ex.LoaderExceptions)
                {
                    Console.WriteLine("\t" + loadEx.Message);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error." + ex.Message);
            }

            foreach (Type t in typesInModule)
            {
                MethodInfo[] methods = t.GetMethods(BindingFlags.Instance | BindingFlags.Public);
                List<MethodInfo> setupMethods = new List<MethodInfo>();
                List<MethodInfo> cleanupMethods = new List<MethodInfo>();

                MethodInfo globalSetupMethod = null;
                MethodInfo globalCleanupMethod = null;
                MethodInfo globalExceptionHandlerMethod = null;

                foreach (MethodInfo m in methods)
                {
                    GlobalTestSetupAttribute[] globalSetupAttributes = (GlobalTestSetupAttribute[])m.GetCustomAttributes(typeof(GlobalTestSetupAttribute), true);
                    if (globalSetupAttributes.Length > 0)
                    {
                        if (null == globalSetupMethod)
                        {
                            globalSetupMethod = m;
                        }
                        else
                        {
                            throw new NotSupportedException("Only one GlobalTestSetup method may be specified per type.");
                        }
                    }

                    GlobalTestCleanupAttribute[] globalCleanupAttributes = (GlobalTestCleanupAttribute[])m.GetCustomAttributes(typeof(GlobalTestCleanupAttribute), true);
                    if (globalCleanupAttributes.Length > 0)
                    {
                        if (null == globalCleanupMethod)
                        {
                            globalCleanupMethod = m;
                        }
                        else
                        {
                            throw new NotSupportedException("Only one GlobalTestCleanup method may be specified per type.");
                        }
                    }

                    GlobalExceptionHandlerAttribute[] globalExceptionHandlerAttributes = (GlobalExceptionHandlerAttribute[])m.GetCustomAttributes(typeof(GlobalExceptionHandlerAttribute), true);
                    if (globalExceptionHandlerAttributes.Length > 0)
                    {
                        if (null == globalExceptionHandlerMethod)
                        {
                            globalExceptionHandlerMethod = m;
                        }
                        else
                        {
                            throw new NotSupportedException("Only one GlobalExceptionHandler method may be specified.");
                        }
                    }

                    TestSetupAttribute[] testSetupAttrs = (TestSetupAttribute[])m.GetCustomAttributes(typeof(TestSetupAttribute), true);
                    if (testSetupAttrs.Length > 0)
                    {
                        setupMethods.Add(m); ;
                    }

                    TestCleanupAttribute[] testCleanupAttrs = (TestCleanupAttribute[])m.GetCustomAttributes(typeof(TestCleanupAttribute), true);
                    if (testCleanupAttrs.Length > 0)
                    {
                        cleanupMethods.Add(m); ;
                    }
                }

                foreach (MethodInfo m in methods)
                {
                    // add single-threaded tests to the list
                    TestAttribute[] testAttrs = (TestAttribute[])m.GetCustomAttributes(typeof(TestAttribute), true);
                    foreach (TestAttribute attr in testAttrs)
                    {
                        tests.Add(new Test(attr, m, t, setupMethods, cleanupMethods));
                    }

                    // add any declared stress tests.
                    StressTestAttribute[] stressTestAttrs = (StressTestAttribute[])m.GetCustomAttributes(typeof(StressTestAttribute), true);
                    foreach (StressTestAttribute attr in stressTestAttrs)
                    {
                        if (TestMetrics.IncludeTest(attr) && MatchFilter(attr))
                            tests.Add(new StressTest(attr, m, globalSetupMethod, globalCleanupMethod, t, setupMethods, cleanupMethods, globalExceptionHandlerMethod));
                    }

                    // add multi-threaded (non thread pool) tests to the list
                    MultiThreadedTestAttribute[] multiThreadedTestAttrs = (MultiThreadedTestAttribute[])m.GetCustomAttributes(typeof(MultiThreadedTestAttribute), true);
                    foreach (MultiThreadedTestAttribute attr in multiThreadedTestAttrs)
                    {
                        if (TestMetrics.IncludeTest(attr))
                            tests.Add(new MultiThreadedTest(attr, m, t, setupMethods, cleanupMethods));
                    }

                    // add multi-threaded (with thread pool) tests to the list
                    ThreadPoolTestAttribute[] threadPoolTestAttrs = (ThreadPoolTestAttribute[])m.GetCustomAttributes(typeof(ThreadPoolTestAttribute), true);
                    foreach (ThreadPoolTestAttribute attr in threadPoolTestAttrs)
                    {
                        if (TestMetrics.IncludeTest(attr))
                            tests.Add(new ThreadPoolTest(attr, m, t, setupMethods, cleanupMethods));
                    }
                }
            }

            return tests;
        }

        private static bool MatchFilter(StressTestAttribute attr)
        {
            // This change should not have impacts on any existing tests. 
            //    1. If filter is not provided in command line, we do not apply filter and select all the tests.
            //    2. If current test attribute (such as StressTestAttribute) does not implement ITestAttriuteFilter, it is not affected and still selected.

            if (string.IsNullOrEmpty(TestMetrics.Filter))
            {
                return true;
            }

            var filter = attr as ITestAttributeFilter;
            if (filter == null)
            {
                return true;
            }

            return filter.MatchFilter(TestMetrics.Filter);
        }
    }
}
