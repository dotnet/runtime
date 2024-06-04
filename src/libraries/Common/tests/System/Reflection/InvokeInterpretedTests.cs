// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Reflection.Tests
{
    public class InvokeInterpretedTests
    {
        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/50957", typeof(PlatformDetection), nameof(PlatformDetection.IsMonoAOT))]
        public static void VerifyInvokeIsUsingInterpreter_Method()
        {
            MethodInfo method = typeof(TestClassThatThrows).GetMethod(nameof(TestClassThatThrows.Throw))!;
            TargetInvocationException ex = Assert.Throws<TargetInvocationException>(() => method.Invoke(null, null));
            Exception exInner = ex.InnerException;

            Assert.Contains("Here", exInner.ToString());
            Assert.Contains(InterpretedMethodName, exInner.ToString());
            Assert.DoesNotContain("InvokeStub_TestClassThatThrows", exInner.ToString());
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/50957", typeof(PlatformDetection), nameof(PlatformDetection.IsMonoAOT))]
        public static void VerifyInvokeIsUsingInterpreter_Constructor()
        {
            ConstructorInfo ctor = typeof(TestClassThatThrows).GetConstructor(Type.EmptyTypes)!;
            TargetInvocationException ex = Assert.Throws<TargetInvocationException>(() => ctor.Invoke(null));
            Exception exInner = ex.InnerException;

            Assert.Contains("Here", exInner.ToString());
            Assert.Contains(InterpretedConstructorName, exInner.ToString());
            Assert.DoesNotContain("InvokeStub_TestClassThatThrows", exInner.ToString());
        }

        private static string InterpretedConstructorName => PlatformDetection.IsMonoRuntime ?
            "InterpretedInvoke_Constructor" :
            "System.RuntimeMethodHandle.InvokeMethod";

        private static string InterpretedMethodName => PlatformDetection.IsMonoRuntime ?
            "InterpretedInvoke_Method" :
            "System.RuntimeMethodHandle.InvokeMethod";

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

