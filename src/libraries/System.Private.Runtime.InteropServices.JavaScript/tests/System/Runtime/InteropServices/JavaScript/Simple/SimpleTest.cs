// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading.Tasks;

namespace System.Runtime.InteropServices.JavaScript.Tests
{
    public static class SimpleTest
    {
        public static async Task<int> Test()
        {
            var tests = new List<Func<Task>>();
            tests.Add(TimerTests.T0_NoTimer);
            tests.Add(TimerTests.T1_OneTimer);
            tests.Add(TimerTests.T2_SecondTimerEarlier);
            tests.Add(TimerTests.T3_SecondTimerLater);
            tests.Add(TimerTests.T5_FiveTimers);

            try
            {
                Console.WriteLine("SimpleMain start test!");
                var failures = 0;
                var failureNames = new List<string>();
                foreach (var test in tests)
                {
                    var failed = await RunTest(test);
                    if (failed != null)
                    {
                        failureNames.Add(failed);
                        failures++;
                    }
                }

                foreach (var failure in failureNames)
                {
                    Console.WriteLine(failure);
                }
                Console.WriteLine($"{Environment.NewLine}=== TEST EXECUTION SUMMARY ==={Environment.NewLine}Total: {tests.Count}, Failed: {failures}");
                return failures;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return -1;
            }
        }

        private static async Task<string> RunTest(Func<Task> action)
        {
            try
            {
                Console.WriteLine("[STRT] " + action.Method.Name);
                await action();
                Console.WriteLine("[DONE] " + action.Method.Name);
                return null;
            }
            catch (Exception ex)
            {
                var message="[FAIL] "+action.Method.Name + " " + ex.Message;
                Console.WriteLine(message);
                return message;
            }
        }
    }
}
