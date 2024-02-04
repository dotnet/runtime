// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using Xunit;

namespace System.Tests
{
    [ActiveIssue("https://github.com/dotnet/runtime/issues/50957", typeof(PlatformDetection), nameof(PlatformDetection.IsBrowser), nameof(PlatformDetection.IsMonoAOT))]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/95981", typeof(PlatformDetection), nameof(PlatformDetection.IsHybridGlobalizationOnBrowser))]
    public class StackTraceHiddenAttributeTests
    {
        [Fact]
        public void Ctor()
        {
            new StackTraceHiddenAttribute();
        }


        [Fact]
        public void MethodHidden_ExceptionStackTrace()
        {
            string stacktrace = null;
            try
            {
                ThrowStackTraceMethodA();
            }
            catch (Exception e)
            {
                stacktrace = e.StackTrace;
            }

            Assert.NotNull(stacktrace);

            Assert.Contains(nameof(ThrowStackTraceMethodA), stacktrace);
            Assert.DoesNotContain(nameof(ThrowStackTraceMethodB), stacktrace);
            Assert.DoesNotContain(nameof(ThrowStackTraceMethodC), stacktrace);
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private static string ThrowStackTraceMethodA() => ThrowStackTraceMethodB();

        [StackTraceHidden]
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private static string ThrowStackTraceMethodB() => ThrowStackTraceMethodC();

        [StackTraceHidden]
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private static string ThrowStackTraceMethodC() => throw new Exception();


        [Fact]
        public void MethodHidden_EnvironmentStackTrace()
        {
            string stacktrace = GetStackTraceMethodA();
            Assert.Contains(nameof(GetStackTraceMethodA), stacktrace);
            Assert.DoesNotContain(nameof(GetStackTraceMethodB), stacktrace);
            Assert.Contains(nameof(GetStackTraceMethodC), stacktrace);
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private static string GetStackTraceMethodA() => GetStackTraceMethodB();

        [StackTraceHidden]
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private static string GetStackTraceMethodB() => GetStackTraceMethodC();

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private static string GetStackTraceMethodC() => Environment.StackTrace;


        [Fact]
        public void ConstructorHidden_EnvironmentStackTrace()
        {
            Assert.Contains(nameof(NotHiddenConstructor), new NotHiddenConstructor().StackTrace);
            Assert.DoesNotContain(nameof(HiddenConstructor), new HiddenConstructor().StackTrace);
        }

        private class NotHiddenConstructor
        {
            public readonly string StackTrace;

            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
            public NotHiddenConstructor() => StackTrace = Environment.StackTrace;
        }

        private class HiddenConstructor
        {
            public readonly string StackTrace;

            [StackTraceHidden]
            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
            public HiddenConstructor() => StackTrace = Environment.StackTrace;
        }


        [Fact]
        public void ClassHidden_EnvironmentStackTrace()
        {
            string stacktrace = HiddenClass.GetStackTraceMethodA();
            Assert.DoesNotContain(nameof(HiddenClass.GetStackTraceMethodA), stacktrace);
            Assert.DoesNotContain(nameof(HiddenClass.GetStackTraceMethodB), stacktrace);
            Assert.DoesNotContain(nameof(HiddenClass.GetStackTraceMethodC), stacktrace);
        }

        [StackTraceHidden]
        internal class HiddenClass
        {
            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
            internal static string GetStackTraceMethodA() => GetStackTraceMethodB();

            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
            internal static string GetStackTraceMethodB() => GetStackTraceMethodC();

            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
            internal static string GetStackTraceMethodC() => Environment.StackTrace;
        }


        [Fact]
        public void StructHidden_EnvironmentStackTrace()
        {
            string stacktrace = new HiddenStruct().GetStackTraceMethodA();
            Assert.DoesNotContain(nameof(HiddenStruct.GetStackTraceMethodA), stacktrace);
            Assert.DoesNotContain(nameof(HiddenStruct.GetStackTraceMethodB), stacktrace);
            Assert.DoesNotContain(nameof(HiddenStruct.GetStackTraceMethodC), stacktrace);
        }

        [StackTraceHidden]
        internal struct HiddenStruct
        {
            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
            internal string GetStackTraceMethodA() => GetStackTraceMethodB();

            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
            internal string GetStackTraceMethodB() => GetStackTraceMethodC();

            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
            internal string GetStackTraceMethodC() => Environment.StackTrace;
        }
    }
}
