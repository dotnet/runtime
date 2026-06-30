// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Exercises exception handling that interleaves interpreter and R2R (or JIT) frames and
// funclets. InterpretedFunctionThatThrows is forced to be interpreted (via BypassReadyToRun)
// when the test is R2R compiled, so unwinding crosses the interpreter/compiled boundary
// repeatedly: a throw out of interpreted code propagates through a compiled finally, which
// itself calls interpreted code that throws and is caught inside the funclet, before the
// original exception is finally caught in the outer compiled frame.

using System;
using System.Runtime;
using System.Runtime.CompilerServices;
using Xunit;

namespace Test_interp_r2r_funclets
{
    public static class InterpR2RFunclets
    {
        private const int ExpectedValue = 0x1234567;

        private static int s_staticValue;

        [Fact]
        public static void TestInterleavedInterpreterAndR2RFunclets()
        {
            s_staticValue = 0;
            Assert.Equal(ExpectedValue, R2RFunction());
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int R2RFunction()
        {
            try
            {
                try
                {
                    InterpretedFunctionThatThrows();
                }
                finally
                {
                    try
                    {
                        // Throw and catch entirely within the finally funclet, which itself
                        // runs while the exception from the inner try is in flight.
                        InterpretedFunctionThatThrows();
                    }
                    catch
                    {
                        // Swallowed inside the funclet.
                    }
                }
            }
            catch
            {
                return s_staticValue;
            }

            return -1;
        }

        // Forced to be interpreted when the test is R2R compiled so that the throw originates
        // in interpreted code and must unwind through compiled funclets.
        [BypassReadyToRun]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void InterpretedFunctionThatThrows()
        {
            s_staticValue = ExpectedValue;
            throw new Exception();
        }
    }
}

namespace System.Runtime
{
    // R2R compilation matches this attribute by namespace + name (see
    // CorInfoImpl.ReadyToRun.ShouldCodeNotBeCompiledIntoFinalImage), so a local copy of the
    // CoreLib-internal attribute is sufficient to force the annotated method to be interpreted.
    internal sealed class BypassReadyToRunAttribute : Attribute
    {
    }
}
