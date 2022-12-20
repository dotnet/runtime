// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/* dead code in a switch contained ina a loop */

using System;
using Xunit;

namespace strswitch_loopstrswitchgoto_cs
{
    public class Class1
    {
        private static TestUtil.TestLog s_testLog;

        static Class1()
        {
            System.IO.StringWriter expectedOut = new System.IO.StringWriter();

            expectedOut.WriteLine("s == one");
            expectedOut.WriteLine("s == two");
            expectedOut.WriteLine("s == three");
            expectedOut.WriteLine("s == four");
            expectedOut.WriteLine("s == five");
            expectedOut.WriteLine("Greater than five");

            s_testLog = new TestUtil.TestLog(expectedOut);
        }

        [Fact]
        public static int TestEntryPoint()
        {
            string[] s = { "one", "two", "three", "four", "five", "six" };
            s_testLog.StartRecording();
            for (int i = 0; i < s.Length; i++)
            {
                switch (s[i])
                {
                    case "one":
                        Console.WriteLine("s == one");
                        break;
                    case "two":
                        Console.WriteLine("s == two");
                        break;
                    case "three":
                        try
                        {
                            Console.WriteLine("s == three");
                        }
                        catch (System.Exception e)
                        {
                            Console.WriteLine(e);
                            goto continueloop;
                        }
                        break;
                    case "four":
                        Console.WriteLine("s == four");
                        break;
                    case "five":
                        Console.WriteLine("s == five");
                        break;
                    default:
                        Console.WriteLine("Greater than five");
                        break;
                };
                continue;
            continueloop:
                Console.WriteLine("Continuing");
            };
            s_testLog.StopRecording();

            return s_testLog.VerifyOutput();
        }
    }
}
