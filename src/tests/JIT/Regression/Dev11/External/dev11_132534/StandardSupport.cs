// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace Test
{
    public struct BasicStruct
    {
        public int Field1;
        public int Field2;
        public int Field3;
        public int Field4;

        public BasicStruct(int field1, int field2, int field3, int field4)
        {
            this.Field1 = field1;
            this.Field2 = field2;
            this.Field3 = field3;
            this.Field4 = field4;
            return;
        }

        public override string ToString()
        {
            return
                String.Format(
                    "{0}, {1}, {2}, {3}",
                    this.Field1,
                    this.Field2,
                    this.Field3,
                    this.Field4
                );
        }

        public static bool AreEqual(BasicStruct s1, BasicStruct s2)
        {
            if ((s1.Field1 == s2.Field1) &&
                (s1.Field2 == s2.Field2) &&
                (s1.Field3 == s2.Field3) &&
                (s1.Field4 == s2.Field4))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }


    public static class Support
    {
        private static Exception SignalVerificationFailure(string kind, string value, string expected)
        {
            throw
                new Exception(
                    String.Format(
                        "FAILED: {0} verification failed.\r\n" +
                        "    Observed value: {1}\r\n" +
                        "    Expected value: {2}\r\n",

                        kind,
                        value,
                        expected
                    )
                );
        }

        public static void VerifyInt(int value, int expected)
        {
            if (value == expected) { return; }
            throw Support.SignalVerificationFailure("Int", value.ToString(), expected.ToString());
        }

        public static void VerifyLong(long value, long expected)
        {
            if (value == expected) { return; }
            throw Support.SignalVerificationFailure("Long", value.ToString(), expected.ToString());
        }

        public static void VerifyFloat(float value, float expected)
        {
            if (value == expected) { return; }
            throw Support.SignalVerificationFailure("Float", value.ToString(), expected.ToString());
        }

        public static void VerifyDouble(double value, double expected)
        {
            if (value == expected) { return; }
            throw Support.SignalVerificationFailure("Double", value.ToString(), expected.ToString());
        }

        public static void VerifyString(string value, string expected)
        {
            if (value == expected) { return; }
            throw Support.SignalVerificationFailure("String", value.ToString(), expected.ToString());
        }

        public static void VerifyStruct(BasicStruct value, BasicStruct expected)
        {
            if (BasicStruct.AreEqual(value, expected)) { return; }
            throw Support.SignalVerificationFailure("Struct", value.ToString(), expected.ToString());
        }
    }


    public static partial class CallerSide
    {
        private static string s_lastExecutedCaller;

        public static void PrepareForWrapperCall()
        {
            CallerSide.s_lastExecutedCaller = null;
            return;
        }

        public static void RecordExecutedCaller(string tag)
        {
            if (CallerSide.s_lastExecutedCaller != null)
            {
                throw new Exception("Tried to record multiple callers at once.");
            }

            CallerSide.s_lastExecutedCaller = tag;
            return;
        }

        public static void VerifyExecutedCaller(string expectedTag)
        {
            if (CallerSide.s_lastExecutedCaller != expectedTag)
            {
                throw new Exception("The exected caller was not recorded during the last operation.");
            }

            return;
        }

        public static bool MakeWrapperCall(string functionTag, Action runSpecificWrapper)
        {
            Console.WriteLine("    Executing JMP wrapper for: \"{0}\"", functionTag);

            try
            {
                runSpecificWrapper();
            }
            catch (Exception e)
            {
                Console.WriteLine("        FAILED: ({0})", e.GetType().ToString());
                return false;
            }

            Console.WriteLine("        PASSED");
            return true;
        }
    }


    public static class App
    {
        [Fact]
        public static int TestEntryPoint()
        {
            int iret = 100;
            Console.WriteLine("Starting JMP tests...\r\n");
            if (!CallerSide.MakeAllWrapperCalls())
            {
                iret = 1;
            }
            Console.WriteLine("\r\nJMP tests are complete.\r\n");
            return iret;
        }
    }
}

