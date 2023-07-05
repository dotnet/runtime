// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Reflection.Tests
{
    public class InvokeInterpretedTests
    {
        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/50957", typeof(PlatformDetection), nameof(PlatformDetection.IsMonoAOT))]
        public static void VerifyInvokeIsUsingEmit_Method()
        {
            MethodInfo method = typeof(TestClassThatThrows).GetMethod(nameof(TestClassThatThrows.Throw))!;
            TargetInvocationException ex = Assert.Throws<TargetInvocationException>(() => method.Invoke(null, null));
            Exception exInner = ex.InnerException;

            Assert.Contains("Here", exInner.ToString());
            Assert.Contains(InterpretedMethodName(), exInner.ToString());
            Assert.DoesNotContain("InvokeStub_TestClassThatThrows", exInner.ToString());

            string InterpretedMethodName() => PlatformDetection.IsMonoRuntime ?
                    "System.Reflection.MethodInvoker.InterpretedInvoke" :
                    "System.RuntimeMethodHandle.InvokeMethod";
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/50957", typeof(PlatformDetection), nameof(PlatformDetection.IsMonoAOT))]
        public static void VerifyInvokeIsUsingEmit_Constructor()
        {
            ConstructorInfo ctor = typeof(TestClassThatThrows).GetConstructor(Type.EmptyTypes)!;
            TargetInvocationException ex = Assert.Throws<TargetInvocationException>(() => ctor.Invoke(null));
            Exception exInner = ex.InnerException;

            Assert.Contains("Here", exInner.ToString());
            Assert.Contains(InterpretedMethodName(), exInner.ToString());
            Assert.DoesNotContain("InvokeStub_TestClassThatThrows", exInner.ToString());

            string InterpretedMethodName() => PlatformDetection.IsMonoRuntime ?
                    "System.Reflection.ConstructorInvoker.InterpretedInvoke" :
                    "System.RuntimeMethodHandle.InvokeMethod";
        }

        private class TestClassThatThrows
        {
            public TestClassThatThrows()
            {
                throw new Exception("Here");
            }

            public static void Throw() => throw new Exception("Here");
        }
    }
}

