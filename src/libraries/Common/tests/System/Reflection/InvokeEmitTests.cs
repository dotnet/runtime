// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Xunit;

namespace System.Reflection.Tests
{
    public class InvokeEmitTests
    {
        [ConditionalFact(typeof(InvokeEmitTests), nameof(InvokeEmitTests.IsEmitInvokeSupported))]
        public static void VerifyInvokeIsUsingEmit_Method()
        {
            MethodInfo method = typeof(TestClassThatThrows).GetMethod(nameof(TestClassThatThrows.Throw))!;
            TargetInvocationException ex = Assert.Throws<TargetInvocationException>(() => method.Invoke(null, null));
            Exception exInner = ex.InnerException;

            Assert.Contains("Here", exInner.ToString());
            Assert.Contains("InvokeStub_TestClassThatThrows", exInner.ToString());
            Assert.DoesNotContain(InterpretedMethodName, exInner.ToString());
        }

        [ConditionalFact(typeof(InvokeEmitTests), nameof(InvokeEmitTests.IsEmitInvokeSupported))]
        public static void VerifyInvokeIsUsingEmit_Constructor()
        {
            ConstructorInfo ctor = typeof(TestClassThatThrows).GetConstructor(Type.EmptyTypes)!;
            TargetInvocationException ex = Assert.Throws<TargetInvocationException>(() => ctor.Invoke(null));
            Exception exInner = ex.InnerException;

            Assert.Contains("Here", exInner.ToString());
            Assert.Contains("InvokeStub_TestClassThatThrows", exInner.ToString());
            Assert.DoesNotContain(InterpretedMethodName, exInner.ToString());
        }

        private static bool IsEmitInvokeSupported()
        {
            // Emit is only used for Invoke when RuntimeFeature.IsDynamicCodeCompiled is true.
            return RuntimeFeature.IsDynamicCodeCompiled
                && !PlatformDetection.IsMonoRuntime; // Temporary until Mono is updated.
        }

        private static string InterpretedMethodName => PlatformDetection.IsMonoRuntime ?
                "System.Reflection.MethodInvoker.InterpretedInvoke" :
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
