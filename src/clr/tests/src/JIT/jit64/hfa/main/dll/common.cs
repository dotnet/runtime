// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace HFATest
{
    public class Common
    {
        public const int SUCC_RET_CODE = 100;
        public const int FAIL_RET_CODE = 1;

        public const float tolerance = (float)1.0E-15;

        public static bool CheckResult(string testName, float actual, float expected)
        {
            bool check = Math.Abs(expected - actual) <= tolerance;
            DisplayResult(testName, actual, expected, check);
            return check;
        }

        public static bool CheckResult(string testName, double actual, double expected)
        {
            bool check = Math.Abs(expected - actual) <= tolerance;
            DisplayResult(testName, actual, expected, check);
            return check;
        }

        private static void DisplayResult(string testName, double actual, double expected, bool result)
        {
            System.Console.Write("[" + testName + "]\t");
            if (result)
            {
                System.Console.WriteLine("PASSED");
            }
            else
            {
                System.Console.WriteLine("FAILED => expected = {0}, actual = {1}", expected, actual);
            }
        }
    }
}
