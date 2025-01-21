﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Xunit;

namespace System.Reflection.Tests
{
    public class InvokeEmitTests
    {
        [ConditionalFact(typeof(InvokeEmitTests), nameof(IsEmitInvokeSupported))]
        public static void VerifyInvokeIsUsingEmit_Method()
        {
            MethodInfo method = typeof(TestClassThatThrows).GetMethod(nameof(TestClassThatThrows.Throw))!;
            TargetInvocationException ex = Assert.Throws<TargetInvocationException>(() => method.Invoke(null, new object[] { "" }));
            Exception exInner = ex.InnerException;
            Assert.Contains("Method Here", ex.ToString());
            Assert.Contains("InvokeStub_<Object, Void>", exInner.ToString());
        }

        [ConditionalFact(typeof(InvokeEmitTests), nameof(IsEmitInvokeSupported))]
        public static void VerifyInvokeIsUsingEmit_Constructor()
        {
            ConstructorInfo ctor = typeof(TestClassThatThrows).GetConstructor(new Type[] {typeof(string)})!;
            TargetInvocationException ex = Assert.Throws<TargetInvocationException>(() => ctor.Invoke(new object[] { "" }));
            Exception exInner = ex.InnerException;
            Assert.Contains("Ctor Here", exInner.ToString());
            Assert.Contains("InvokeStub_<Object, Void>", exInner.ToString());
        }

        private static bool IsEmitInvokeSupported()
        {
            // Emit is only used for Invoke when RuntimeFeature.IsDynamicCodeSupported is true.
            return RuntimeFeature.IsDynamicCodeSupported;
        }

        private class TestClassThatThrows
        {
            public TestClassThatThrows(string _)
            {
                throw new Exception("Ctor Here");
            }

            public static void Throw(string _) => throw new Exception("Method Here");
        }
    }
}
