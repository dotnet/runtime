// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Reflection;


namespace DPStressHarness
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Init(args);
            Run();
        }
        public enum RunMode
        {
            RunAll,
            RunVerify,
            Help,
            ExitWithError
        };

        private static RunMode s_mode = RunMode.RunAll;
        private static IEnumerable<TestBase> s_tests;
        private static StressEngine s_eng;
        private static string s_error;


        public static void Init(string[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "-a":
                        string assemblyName = args[++i];
                        TestFinder.AssemblyName = new AssemblyName(assemblyName);
                        break;

                    case "-all":
                        s_mode = RunMode.RunAll;
                        break;

                    case "-override":
                        TestMetrics.Overrides.Add(args[++i], args[++i]);
                        break;

                    case "-variation":
                        TestMetrics.Variations.Add(args[++i]);
                        break;

                    case "-test":
                        TestMetrics.SelectedTests.Add(args[++i]);
                        break;

                    case "-duration":
                        TestMetrics.StressDuration = Int32.Parse(args[++i]);
                        break;

                    case "-threads":
                        TestMetrics.StressThreads = Int32.Parse(args[++i]);
                        break;

                    case "-verify":
                        s_mode = RunMode.RunVerify;
                        break;

                    case "-debug":
                        if (System.Diagnostics.Debugger.IsAttached)
                        {
                            System.Diagnostics.Debugger.Break();
                        }
                        else
                        {
                            Console.WriteLine("Current PID: {0}, attach the debugger and press Enter to continue the execution...", System.Diagnostics.Process.GetCurrentProcess().Id);
                            Console.ReadLine();
                        }
                        break;

                    case "-exceptionThreshold":
                        TestMetrics.ExceptionThreshold = Int32.Parse(args[++i]);
                        break;

                    case "-monitorenabled":
                        TestMetrics.MonitorEnabled = bool.Parse(args[++i]);
                        break;

                    case "-randomSeed":
                        TestMetrics.RandomSeed = Int32.Parse(args[++i]);
                        break;

                    case "-filter":
                        TestMetrics.Filter = args[++i];
                        break;

                    case "-printMethodName":
                        TestMetrics.PrintMethodName = true;
                        break;

                    case "-deadlockdetection":
                        if (bool.Parse(args[++i]))
                        {
                            DeadlockDetection.Enable();
                        }
                        break;

                    default:
                        s_mode = RunMode.Help;
                        break;
                }
            }

            if (TestFinder.AssemblyName != null)
            {
                Console.WriteLine("Assembly Found for the Assembly Name " + TestFinder.AssemblyName);
                
                if (TestFinder.AssemblyName != null)
                {
                    // get and load all the tests
                    s_tests = TestFinder.GetTests(Assembly.Load(TestFinder.AssemblyName));

                    // instantiate the stress engine
                    s_eng = new StressEngine(TestMetrics.StressThreads, TestMetrics.StressDuration, s_tests, TestMetrics.RandomSeed);
                }
                else
                {
                    Program.s_error = string.Format("Assembly {0} cannot be found.", TestFinder.AssemblyName);
                    s_mode = RunMode.ExitWithError;
                }
            }
        }

        public static void TestCase(Assembly assembly,
                                    RunMode mode,
                                    int duration,
                                    int threads,
                                    int? exceptionThreshold = null,
                                    bool printMethodName = false,
                                    bool deadlockDetection = false,
                                    int randomSeed = 0,
                                    string[] selectedTests = null,
                                    string[] overrides = null,
                                    string[] variations = null,
                                    string filter = null,
                                    bool monitorEnabled = false,
                                    string monitorMachineName = "localhost")
        {
            TestMetrics.Reset();
            TestFinder.AssemblyName = assembly.GetName();
            mode = RunMode.RunAll;

            for (int i = 0; overrides != null && i < overrides.Length; i++)
            {
                TestMetrics.Overrides.Add(overrides[i], overrides[++i]);
            }

            for (int i = 0; variations != null && i < variations.Length; i++)
            {
                TestMetrics.Variations.Add(variations[i]);
            }

            for (int i = 0; selectedTests != null && i < selectedTests.Length; i++)
            {
                TestMetrics.SelectedTests.Add(selectedTests[i]);
            }

            TestMetrics.StressDuration = duration;
            TestMetrics.StressThreads = threads;
            TestMetrics.ExceptionThreshold = exceptionThreshold;
            TestMetrics.MonitorEnabled = monitorEnabled;
            TestMetrics.MonitorMachineName = monitorMachineName;
            TestMetrics.RandomSeed = randomSeed;
            TestMetrics.Filter = filter;
            TestMetrics.PrintMethodName = printMethodName;

            if (deadlockDetection)
            {
                DeadlockDetection.Enable();
            }

            // get and load all the tests
            s_tests = TestFinder.GetTests(assembly);

            // instantiate the stress engine
            s_eng = new StressEngine(TestMetrics.StressThreads, TestMetrics.StressDuration, s_tests, TestMetrics.RandomSeed);
        }

        public static void Run()
        {
            if (TestFinder.AssemblyName == null)
            {
                s_mode = RunMode.Help;
            }
            switch (s_mode)
            {
                case RunMode.RunAll:
                    RunStress();
                    break;

                case RunMode.RunVerify:
                    RunVerify();
                    break;

                case RunMode.ExitWithError:
                    ExitWithError();
                    break;

                case RunMode.Help:
                    PrintHelp();
                    break;
            }
        }

        private static void PrintHelp()
        {
            Console.WriteLine("stresstest.exe [-a <module name>] <arguments>");
            Console.WriteLine();
            Console.WriteLine("   -a <module name> should specify path to the assembly containing the tests.");
            Console.WriteLine();
            Console.WriteLine("Supported options are:");
            Console.WriteLine();
            Console.WriteLine("   -all                        Run all tests - best for debugging, not perf measurements.");
            Console.WriteLine();
            Console.WriteLine("   -verify                     Run in functional verification mode.");
            Console.WriteLine();
            Console.WriteLine("   -duration <n>               Duration of the test in seconds.");
            Console.WriteLine();
            Console.WriteLine("   -threads <n>                Number of threads to use.");
            Console.WriteLine();
            Console.WriteLine("   -override <name> <value>    Override the value of a test property.");
            Console.WriteLine();
            Console.WriteLine("   -test <name>                Run specific test(s) using their name.");
            Console.WriteLine();
            Console.WriteLine("   -debug                      Print process ID in the beginning and wait for Enter (to give your time to attach the debugger).");
            Console.WriteLine();
            Console.WriteLine("   -exceptionThreshold <n>     An optional limit on exceptions which will be caught. When reached, test will halt.");
            Console.WriteLine();
            Console.WriteLine("   -monitorenabled             True or False to enable monitoring. Default is false");
            Console.WriteLine();
            Console.WriteLine("   -randomSeed                 Enables setting of the random number generator used internally.  This serves both the purpose");
            Console.WriteLine("                               of helping to improve reproducibility and making it deterministic from Chess's perspective");
            Console.WriteLine("                               for a given schedule. Default is " + TestMetrics.RandomSeed + ".");
            Console.WriteLine();
            Console.WriteLine("   -filter                     Run tests whose stress test attributes match the given filter. Filter is not applied if attribute");
            Console.WriteLine("                               does not implement ITestAttributeFilter. Example: -filter TestType=Query,Update;IsServerTest=True ");
            Console.WriteLine();
            Console.WriteLine("   -printMethodName            Print tests' title in console window");
            Console.WriteLine();
            Console.WriteLine("   -deadlockdetection          True or False to enable deadlock detection. Default is false");
            Console.WriteLine();
        }

        private static int ExitWithError()
        {
            Environment.FailFast("Exit with error(s).");
            return 1;
        }

        private static int RunVerify()
        {
            throw new NotImplementedException();
        }

        private static int RunStress()
        {
            return s_eng.Run();
        }
    }

}

