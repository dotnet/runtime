// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Collections.Generic;

namespace JIT.HardwareIntrinsics.X86
{
    public static partial class Program
    {
        private const int PASS = 100;
        private const int FAIL = 0;

        private static readonly IDictionary<string, Action> TestList;

        public static int Main(string[] args)
        {
            var isPassing = true;

            foreach (string testToRun in GetTestsToRun(args))
            {
                Console.WriteLine($"Running {testToRun} test...");

                try
                {
                    TestList[testToRun].Invoke();
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error: {e.Message}");
                    isPassing = false;
                }
            }

            return isPassing ? PASS : FAIL;
        }

        private static ICollection<string> GetTestsToRun(string[] args)
        {
            var testsToRun = new HashSet<string>();

            for (var i = 0; i < args.Length; i++)
            {
                var testName = args[i];

                if (testName.Equals("all", StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }

                if (!TestList.Keys.Contains(testName, StringComparer.OrdinalIgnoreCase))
                {
                    PrintUsage();
                }

                testsToRun.Add(testName);
            }

            return (testsToRun.Count == 0) ? TestList.Keys : testsToRun;
        }

        private static void PrintUsage()
        {
            Console.WriteLine($@"Usage:
{Environment.GetCommandLineArgs()[0]} [testName]

  [testName]: The name of the function to test.
              Defaults to 'all'.
              Multiple can be specified.

  Available Test Names:");
            foreach (string testName in TestList.Keys)
            {
                Console.WriteLine($"    {testName}");
            }

            Environment.Exit(FAIL);
        }
    }
}
