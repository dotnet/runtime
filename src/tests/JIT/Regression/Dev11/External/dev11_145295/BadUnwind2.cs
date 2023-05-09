// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;


namespace Test
{
    public static class Exceptions
    {
        public class ReachedCallout : Exception
        {
            public int CalloutIndex { get; private set; }
            public ReachedCallout(int calloutIndex) : base("ReachedCallout") { this.CalloutIndex = calloutIndex; }
        }
    }


    public static class Helpers
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Callout0() { Console.WriteLine("    REACHED: Callout0"); }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Callout1() { Console.WriteLine("    REACHED: Callout1"); }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Callout2() { Console.WriteLine("    REACHED: Callout2"); }


        private static Exception s_standardCallout3Exception = new Exceptions.ReachedCallout(3);
        private static Exception s_standardCallout4Exception = new Exceptions.ReachedCallout(4);
        private static Exception s_standardCallout5Exception = new Exceptions.ReachedCallout(5);
        private static Exception s_standardCallout6Exception = new Exceptions.ReachedCallout(6);


        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Callout3() { Console.WriteLine("    REACHED: Callout3"); throw Helpers.s_standardCallout3Exception; }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Callout4() { Console.WriteLine("    REACHED: Callout4"); throw Helpers.s_standardCallout4Exception; }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Callout5() { Console.WriteLine("    REACHED: Callout5"); throw Helpers.s_standardCallout5Exception; }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Callout6() { Console.WriteLine("    REACHED: Callout6"); throw Helpers.s_standardCallout6Exception; }


        private static Exception s_standardException = new Exception("Manual throw.");

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Throw()
        {
            throw Helpers.s_standardException;
        }
    }


    public static class App
    {
        private static int s_numberOfFailures = 0;


        private static void DispatchCallout(string caption, int calloutIndex, Func<int, int> runTarget)
        {
            Console.WriteLine("\r\nRUNNING_SCENARIO: `{0}' ({1})", caption, calloutIndex);

            try
            {
                runTarget(calloutIndex);
                Console.WriteLine("    FAILED: No ReachedCallout exception was thrown.");
                App.s_numberOfFailures += 1;
            }
            catch (Exceptions.ReachedCallout e)
            {
                if (e.CalloutIndex == calloutIndex)
                {
                    Console.WriteLine("    PASSED.");
                }
                else
                {
                    Console.WriteLine("    FAILED: Wrong callout exception (Expected={0}, Actual={1}).", calloutIndex, e.CalloutIndex);
                    App.s_numberOfFailures += 1;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("    FAILED: ({0})", e.GetType().ToString());
                App.s_numberOfFailures += 1;
            }

            return;
        }


        private static void DispatchCalloutSequence(string caption, Func<int, int> runTarget)
        {
            int calloutIndex;

            for (calloutIndex = 3; calloutIndex <= 6; calloutIndex++)
            {
                App.DispatchCallout(caption, calloutIndex, runTarget);
            }

            return;
        }


        [Fact]
        public static int TestEntryPoint()
        {
            App.DispatchCalloutSequence("TopLevel", ILPart.CallThroughFrameWithMultipleEndfinallyOps_TopLevel);
            App.DispatchCalloutSequence("Nested", ILPart.CallThroughFrameWithMultipleEndfinallyOps_Nested);

            if (App.s_numberOfFailures == 0)
            {
                Console.WriteLine("\r\nTest passed.");
                return 100;
            }
            else
            {
                Console.WriteLine("\r\nTest failed.  ErrorCount={0}", App.s_numberOfFailures);
                return 101;
            }
        }
    }
}
