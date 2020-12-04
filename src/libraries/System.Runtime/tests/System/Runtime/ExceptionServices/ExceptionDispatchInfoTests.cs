// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Xunit;

namespace System.Runtime.ExceptionServices.Tests
{
    public class ExceptionDispatchInfoTests
    {
        [Fact]
        public static void StaticThrow_NullArgument_ThrowArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ExceptionDispatchInfo.Throw(null));
        }

        [Fact]
        public static void StaticThrow_UpdatesStackTraceAppropriately()
        {
            const string RethrowMessageSubstring = "End of stack trace";
            var e = new FormatException();
            for (int i = 0; i < 3; i++)
            {
                Assert.Same(e, Assert.Throws<FormatException>(() => ExceptionDispatchInfo.Throw(e)));
                Assert.Equal(i, Regex.Matches(e.StackTrace, RethrowMessageSubstring).Count);
            }
        }

        [Fact]
        public static void SetCurrentStackTrace_Invalid_Throws()
        {
            Exception e;

            // Null argument
            e = null;
            AssertExtensions.Throws<ArgumentNullException>("source", () => ExceptionDispatchInfo.SetCurrentStackTrace(e));

            // Previously set current stack
            e = new Exception();
            ExceptionDispatchInfo.SetCurrentStackTrace(e);
            Assert.Throws<InvalidOperationException>(() => ExceptionDispatchInfo.SetCurrentStackTrace(e));

            // Previously thrown
            e = new Exception();
            try { throw e; } catch { }
            Assert.Throws<InvalidOperationException>(() => ExceptionDispatchInfo.SetCurrentStackTrace(e));
        }

        [Fact]
        public static void SetCurrentStackTrace_IncludedInExceptionStackTrace()
        {
            Exception e;

            e = new Exception();
            ABCDEFGHIJKLMNOPQRSTUVWXYZ(e);
            Assert.Contains(nameof(ABCDEFGHIJKLMNOPQRSTUVWXYZ), e.StackTrace);

            e = new Exception();
            ABCDEFGHIJKLMNOPQRSTUVWXYZ(e);
            try { throw e; } catch { }
            Assert.Contains(nameof(ABCDEFGHIJKLMNOPQRSTUVWXYZ), e.StackTrace);
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private static void ABCDEFGHIJKLMNOPQRSTUVWXYZ(Exception e)
        {
            Assert.Same(e, ExceptionDispatchInfo.SetCurrentStackTrace(e));
        }
    }
}
