// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
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
        public static void SetCurrentOrRemoteStackTrace_Invalid_Throws()
        {
            Exception e;

            // Null argument
            e = null;
            AssertExtensions.Throws<ArgumentNullException>("source", () => ExceptionDispatchInfo.SetCurrentStackTrace(e));
            AssertExtensions.Throws<ArgumentNullException>("source", () => ExceptionDispatchInfo.SetRemoteStackTrace(e, "Hello"));
            AssertExtensions.Throws<ArgumentNullException>("stackTrace", () => ExceptionDispatchInfo.SetRemoteStackTrace(new Exception(), stackTrace: null));

            // Previously set current stack
            e = new Exception();
            ExceptionDispatchInfo.SetCurrentStackTrace(e);
            Assert.Throws<InvalidOperationException>(() => ExceptionDispatchInfo.SetCurrentStackTrace(e));
            Assert.Throws<InvalidOperationException>(() => ExceptionDispatchInfo.SetRemoteStackTrace(e, "Hello"));

            // Previously thrown
            e = new Exception();
            try { throw e; } catch { }
            Assert.Throws<InvalidOperationException>(() => ExceptionDispatchInfo.SetCurrentStackTrace(e));
            Assert.Throws<InvalidOperationException>(() => ExceptionDispatchInfo.SetRemoteStackTrace(e, "Hello"));
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/50957", typeof(PlatformDetection), nameof(PlatformDetection.IsBrowser), nameof(PlatformDetection.IsMonoAOT))]
        public static void SetCurrentStackTrace_IncludedInExceptionStackTrace()
        {
            Exception e;

            e = new Exception();
            ABCDEFGHIJKLMNOPQRSTUVWXYZ(e);
            Assert.Contains(nameof(ABCDEFGHIJKLMNOPQRSTUVWXYZ), e.StackTrace, StringComparison.Ordinal);

            e = new Exception();
            ABCDEFGHIJKLMNOPQRSTUVWXYZ(e);
            try { throw e; } catch { }
            Assert.Contains(nameof(ABCDEFGHIJKLMNOPQRSTUVWXYZ), e.StackTrace, StringComparison.Ordinal);
        }

        [Fact]
        public static void SetRemoteStackTrace_IncludedInExceptionStackTrace()
        {
            Exception e;

            e = new Exception();
            Assert.Same(e, ExceptionDispatchInfo.SetRemoteStackTrace(e, "pumpkin-anaconda-maritime")); // 3 randomly selected words
            Assert.Contains("pumpkin-anaconda-maritime", e.StackTrace, StringComparison.Ordinal);
            Assert.DoesNotContain("pumpkin-anaconda-maritime", new StackTrace(e).ToString(), StringComparison.Ordinal); // we shouldn't attempt to parse it in a StackTrace object

            e = new Exception();
            Assert.Same(e, ExceptionDispatchInfo.SetRemoteStackTrace(e, "pumpkin-anaconda-maritime"));
            try { throw e; } catch { }
            Assert.Contains("pumpkin-anaconda-maritime", e.StackTrace, StringComparison.Ordinal);
            Assert.DoesNotContain("pumpkin-anaconda-maritime", new StackTrace(e).ToString(), StringComparison.Ordinal);
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private static void ABCDEFGHIJKLMNOPQRSTUVWXYZ(Exception e)
        {
            Assert.Same(e, ExceptionDispatchInfo.SetCurrentStackTrace(e));
        }
    }
}
