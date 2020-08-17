// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Intrinsics.Arm;

namespace JIT.HardwareIntrinsics.Arm
{
    public static partial class Program
    {
        private const int PASS = 100;
        private const int FAIL = 0;

        private static readonly IDictionary<string, Action> TestList;

        public static int Main(string[] args)
        {
            var isPassing = true;

            PrintSupportedIsa();

            foreach (string testToRun in GetTestsToRun(args))
            {
                TestLibrary.TestFramework.BeginTestCase(testToRun);

                try
                {
                    TestList[testToRun].Invoke();
                }
                catch (Exception e)
                {
                    TestLibrary.TestFramework.LogError(e.GetType().ToString(), e.Message);
                    TestLibrary.TestFramework.LogVerbose(e.StackTrace);
                    isPassing = false;
                }

                TestLibrary.TestFramework.EndTestCase();
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

        private static void PrintSupportedIsa()
        {
            TestLibrary.TestFramework.LogInformation("Supported ISAs:");
            TestLibrary.TestFramework.LogInformation($"  AdvSimd:   {AdvSimd.IsSupported}");
            TestLibrary.TestFramework.LogInformation($"  Aes:       {Aes.IsSupported}");
            TestLibrary.TestFramework.LogInformation($"  ArmBase:   {ArmBase.IsSupported}");
            TestLibrary.TestFramework.LogInformation($"  Crc32:     {Crc32.IsSupported}");
            TestLibrary.TestFramework.LogInformation($"  Dp:        {Dp.IsSupported}");
            TestLibrary.TestFramework.LogInformation($"  Rdm:       {Rdm.IsSupported}");
            TestLibrary.TestFramework.LogInformation($"  Sha1:      {Sha1.IsSupported}");
            TestLibrary.TestFramework.LogInformation($"  Sha256:    {Sha256.IsSupported}");
            TestLibrary.TestFramework.LogInformation(string.Empty);
        }

        private static void PrintUsage()
        {
            TestLibrary.TestFramework.LogInformation($@"Usage:
{Environment.GetCommandLineArgs()[0]} [testName]

  [testName]: The name of the function to test.
              Defaults to 'all'.
              Multiple can be specified.

  Available Test Names:");
            foreach (string testName in TestList.Keys)
            {
                TestLibrary.TestFramework.LogInformation($"    {testName}");
            }

            Environment.Exit(FAIL);
        }
    }
}
