// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
using System.Reflection;

namespace RuntimeLibrariesTest.Internal
{
    public static class Runner
    {
        private static int s_numTests = 0;
        private static int s_numFailedTests = 0;
        private static int s_numPassedTests = 0;
        private static bool s_catchAllExceptions = true;
        private static bool s_listOnly = false;

        public static bool RunTests(TestInfo[] allTests, String[] args)
        {
            bool result = true;
            if (processArgs(allTests, args) == false) return false;

            TestInfo[] failedTests = new TestInfo[allTests.Length];
            int numFailed = 0;
            foreach (TestInfo t in allTests)
            {
                if (t.ShouldExecute)
                {
                    bool passed = RunTest(t);
                    result &= passed;
                    if (!passed)
                    {
                        failedTests[numFailed++] = t;
                    }
                }
            }

            if (numFailed != 0)
            {
                Logger.LogInformation(String.Empty);
                Logger.LogInformation("Failed tests:");
                foreach (TestInfo failedTest in failedTests)
                {
                    if (failedTest == null)
                        break;
                    Logger.LogInformation("  {0}", failedTest.Name);
                }
                Logger.LogInformation(String.Empty);
            }

            return result;
        }

        /// <summary>
        /// Runs a given test.
        /// </summary>
        /// <param name="testName">name of test to run.</param>
        /// <param name="test">test action</param>
        /// <returns>true if the test passes and false otherwise.</returns>
        private static bool RunTest(TestInfo t)
        {
            if (s_catchAllExceptions)
            {
                try
                {
                    return RunTestMethod(t);
                }
                catch (Exception ex)
                {
                    Logger.LogInformation("Caught Unexpected exception:" + ex);
                    Logger.LogInformation("---- Test FAILED ---------------");
                    Logger.LogInformation(string.Empty);
                    s_numFailedTests++;
                    return false;
                }
            }
            else
            {
                return RunTestMethod(t);
            }
        }

        /// <summary>
        /// Runs a given test.
        /// </summary>
        /// <param name="testName">name of test to run.</param>
        /// <param name="test">test action</param>
        /// <returns>true if the test passes and false otherwise.</returns>
        private static bool RunTestMethod(TestInfo t)
        {
            if (s_listOnly)
            {
                Logger.LogInformation("{0}", t.Name);
                return true;
            }

            s_numTests++;
            bool passed = true;
            try
            {
                Logger.LogInformation("--------------------------------");
                if (t.InstanceClass != null && t.InstanceClass.TestInitializer != null)
                {
                    Logger.LogInformation("Running TestInitializer for {0}", t.Name);
                    t.InstanceClass.TestInitializer();
                }

                try
                {
                    Logger.LogInformation("Running Test: {0}", t.Name);
                    if (t.IsTask)
                    {
                        var rv = Task.Run(async () => await asyncRunner(t.AsFunc())).Result;
                    }
                    else
                    {
                        t.AsAction();
                    }

                    if (t.ExpectsException != null)
                    {
                        Assert.Fail(
                            "Test did not throw expected exception: {0}. {1}",
                            t.ExpectsException.ExceptionType.ToString(),
                            t.ExpectsException.Description != null ? t.ExpectsException.Description : "");
                    }
                }
                catch (Exception ex)
                {
                    if (t.ExpectsException == null)
                    {
                        throw;
                    }
                    else
                    {
                        if (!t.ExpectsException.ExceptionType.GetTypeInfo().IsAssignableFrom(ex.GetType().GetTypeInfo()))
                        {
                            throw;
                        }
                        else
                        {
                            passed = true;
                        }
                    }
                }

                s_numPassedTests++;
                if (t.InstanceClass != null && t.InstanceClass.TestCleanup != null)
                {
                    Logger.LogInformation("Running TestCleanup for {0}", t.Name);
                    t.InstanceClass.TestCleanup();
                }
            }
            catch (AssertTestException ex)
            {
                Logger.LogInformation(ex.ToString());
                passed = false;
                s_numFailedTests++;
            }
            catch (AggregateException ae)
            {
                // Only handles AssertTestExceptions and lets any other exceptions stop the program.
                ae.Handle(exception =>
                {
                    if (exception is AssertTestException)
                    {
                        Logger.LogInformation(exception.ToString());
                        passed = false;
                        s_numFailedTests++;
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                });
            }


            // Does not catch Exception because we want to fail fast and generate a dump 
            // file when an unexpected exception is encountered. (Except for ExpectedException cases.)

            Logger.LogInformation("---- Test {0} ---------------", passed ? "PASSED" : "FAILED");
            Logger.LogInformation(string.Empty);
            return passed;
        }

        private static async Task<int> asyncRunner(Task t)
        {
            await t;
            return 100;
        }

        public static void PrintResults()
        {
            Logger.LogInformation("Finished running {0} tests. PASSED: {1}.  FAILED: {2}", NumTests, NumPassedTests, NumFailedTests);
        }

        public static int NumFailedTests
        {
            get { return s_numFailedTests; }
        }

        public static int NumTests
        {
            get { return s_numTests; }
        }

        public static int NumPassedTests
        {
            get { return s_numPassedTests; }
        }

        private static bool processArgs(TestInfo[] tests, String[] args)
        {
            bool exactMatch = false;
            bool executeAll = false;
            bool executeNonIgnored = true;
            foreach (var a in args)
            {
                if (a.StartsWith("/") || a.StartsWith("-"))
                {
                    if (a.Length == 1) throw new ArgumentException("Invalid Parameter:" + a);
                    String val = a.Substring(1);
                    switch (val.ToLower())
                    {
                        case "?":
                        case "help":
                            printHelp(tests);
                            return false;
                        case "a":
                        case "all":
                            executeAll = true;
                            break;
                        case "t":
                        case "throw":
                            s_catchAllExceptions = false;
                            break;
                        case "l":
                        case "list":
                            s_listOnly = true;
                            break;
                        case "exact":
                            exactMatch = true;
                            break;
                        default:
                            Logger.LogInformation("Invalid parameter:" + a);
                            break;
                    }
                }
                else if (a.StartsWith("!"))
                {
                    // Must process these after we process the executeAll tests.
                }
                else if (a.Length != 0 && (Char.IsLetterOrDigit(a[0]) || a[0] == '.' || a[0] == ' '))
                {
                    executeAll = false;
                    executeNonIgnored = false;
                    foreach (var t in tests)
                    {
                        if (exactMatch == false)
                        {
                            if (t.Name.IndexOf(a, StringComparison.OrdinalIgnoreCase) != -1)
                            {
                                t.ShouldExecute = true;
                            }
                        }
                        else
                        {
                            // If the name doesn't match exactly, check to see if it ends with the name
                            if (String.Equals(t.Name, a, StringComparison.OrdinalIgnoreCase) || t.Name.EndsWith("." + a, StringComparison.OrdinalIgnoreCase))
                            {
                                t.ShouldExecute = true;
                            }
                        }
                    }
                }
                else
                {
                    throw new ArgumentException("Invalid Argument:" + a);
                }
            }

            if (executeAll == true)
            {
                for (int i = 0; i < tests.Length; i++)
                {
                    tests[i].ShouldExecute = true;
                }
            }

            if (executeNonIgnored == true)
            {
                for (int i = 0; i < tests.Length; i++)
                {
                    if (tests[i].IsIgnored == false) tests[i].ShouldExecute = true;
                }
            }

            foreach (String a in args)
            {
                if (a.StartsWith("!"))
                {
                    String pattern = a.Substring(1);
                    foreach (TestInfo t in tests)
                    {
                        if (t.Name.IndexOf(pattern, StringComparison.OrdinalIgnoreCase) != -1)
                        {
                            t.ShouldExecute = false;
                        }
                    }
                }
            }

            return true;
        }

        private static void printHelp(TestInfo[] tests)
        {
            Logger.LogInformation("Syntax:  EXEname [testname1 testname2 ...]");
            Logger.LogInformation("If no args are given, will run all tests (EXCLUDING the ignored tests. Otherwise,"
                + " will only run tests with the input test names. Comparison of test names is case-insensitive.");
            Logger.LogInformation("Switches: ");
            Logger.LogInformation("/?, /help                Display help message.");
            Logger.LogInformation("/a, /all                 Run all tests, including ignored tests.");
            Logger.LogInformation("/l, /list                Display the test names but don't run them.");
            Logger.LogInformation("");
            Logger.LogInformation("Test Methods");
            foreach (var test in tests)
            {
                if (test.IsIgnored == false)
                {
                    Logger.LogInformation("   - " + test.Name);
                }
            }
            Logger.LogInformation("Ignored Test Methods: (To run, use /a or /all or specify test name)");
            foreach (var test in tests)
            {
                if (test.IsIgnored == true)
                {
                    Logger.LogInformation("   - " + test.Name);
                }
            }
        }
    }
}
