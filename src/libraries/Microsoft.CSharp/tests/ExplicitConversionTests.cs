// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Xunit;

namespace Microsoft.CSharp.RuntimeBinder.Tests
{
    public class ExplicitConversionTests
    {
        private enum ExpectedConversionResult
        {
            Succeed,
            CompileError,
            RuntimeError
        }
        private static void AssertExplicitConvert<TSource, TTarget>(TSource argument, TTarget target, ExpectedConversionResult expected)
        {
            CallSiteBinder binder = Binder.Convert(CSharpBinderFlags.ConvertExplicit, typeof(TTarget), typeof(ExplicitConversionTests));
            CallSite<Func<CallSite, TSource, TTarget>> callSite =
                CallSite<Func<CallSite, TSource, TTarget>>.Create(binder);
            Func<CallSite, TSource, TTarget> func = callSite.Target;
            switch (expected)
            {
                case ExpectedConversionResult.CompileError:
                    Assert.Throws<RuntimeBinderException>(() => func(callSite, argument));
                    break;

                case ExpectedConversionResult.RuntimeError:
                    Assert.Throws<InvalidCastException>(() => func(callSite, argument));
                    break;

                default:
                    TTarget result = func(callSite, argument);
                    Assert.Equal(target, result);
                    break;
            }

            if (typeof(TSource) != typeof(object))
            {
                AssertExplicitConvert<object, TTarget>(argument, target, expected);
            }
        }


        private interface IInterface
        {
        }

        private class UnsealedClass
        {
        }

        private sealed class SealedClass
        {
        }

        private class ImplementingClass : IInterface
        {
        }

        private class ImplementingSealedClass : IInterface
        {
        }

        private class UnrelatedNonInterface
        {
        }

        private struct Struct
        {
        }

        private struct ImplementingStruct : IInterface
        {
        }

        [Fact]
        public void ClassInterfaceExplicitConversion()
        {
            AssertExplicitConvert(new SealedClass(), default(IInterface), ExpectedConversionResult.CompileError);
            AssertExplicitConvert(new UnsealedClass(), default(IInterface), ExpectedConversionResult.RuntimeError);
            ImplementingClass ic = new ImplementingClass();
            AssertExplicitConvert(ic, (IInterface)ic, ExpectedConversionResult.Succeed);
            ImplementingSealedClass isc = new ImplementingSealedClass();
            AssertExplicitConvert(isc, (IInterface)isc, ExpectedConversionResult.Succeed);
            AssertExplicitConvert(new Struct(), default(IInterface), ExpectedConversionResult.CompileError);
            AssertExplicitConvert(new ImplementingStruct(), new ImplementingStruct(), ExpectedConversionResult.Succeed);
            AssertExplicitConvert(new SealedClass(), default(UnrelatedNonInterface), ExpectedConversionResult.CompileError);
            AssertExplicitConvert(new UnsealedClass(), default(UnrelatedNonInterface), ExpectedConversionResult.CompileError);
            AssertExplicitConvert(new Struct(), default(UnrelatedNonInterface), ExpectedConversionResult.CompileError);
        }

        [Fact]
        public void InterfaceClassExplicitConversion()
        {
            IInterface iif = new ImplementingClass();
            AssertExplicitConvert(iif, default(SealedClass), ExpectedConversionResult.CompileError);
            AssertExplicitConvert(iif, default(UnsealedClass), ExpectedConversionResult.CompileError);
            ImplementingClass ic = new ImplementingClass();
            AssertExplicitConvert((IInterface)ic, ic, ExpectedConversionResult.Succeed);
            ImplementingSealedClass isc = new ImplementingSealedClass();
            AssertExplicitConvert((IInterface)isc, isc, ExpectedConversionResult.Succeed);
            AssertExplicitConvert(iif, default(Struct), ExpectedConversionResult.CompileError);
            AssertExplicitConvert((IInterface)new ImplementingStruct(), new ImplementingStruct(), ExpectedConversionResult.Succeed);
        }

        [Fact]
        public void ClassInterfaceArrayElementExplicitConversions()
        {
            AssertExplicitConvert(new SealedClass[0], default(IInterface[]), ExpectedConversionResult.CompileError);
            var ic = new ImplementingClass[0];
            AssertExplicitConvert(ic, (IInterface[])ic, ExpectedConversionResult.Succeed);
            var isc = new ImplementingSealedClass[0];
            AssertExplicitConvert(isc, (IInterface[])isc, ExpectedConversionResult.Succeed);
            AssertExplicitConvert(new Struct[0], default(IInterface[]), ExpectedConversionResult.CompileError);
            AssertExplicitConvert(new SealedClass[0], default(UnrelatedNonInterface[]), ExpectedConversionResult.CompileError);
            AssertExplicitConvert(new UnsealedClass[0], default(UnrelatedNonInterface[]), ExpectedConversionResult.CompileError);
            AssertExplicitConvert(new Struct[0], default(UnrelatedNonInterface[]), ExpectedConversionResult.CompileError);
        }

        [Fact, SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "25754 is not fixed in NetFX")]
        public void ClassInterfaceArrayElementExplicitConversionsCoreFX()
        {
            AssertExplicitConvert(new UnsealedClass[0], default(IInterface[]), ExpectedConversionResult.RuntimeError);
        }

        [Fact]
        public void ClassInterfaceArrayIListElementExplicitConversions()
        {
            AssertExplicitConvert(new SealedClass[0], default(IList<IInterface>), ExpectedConversionResult.CompileError);
            AssertExplicitConvert(new UnsealedClass[0], default(IList<IInterface>), ExpectedConversionResult.RuntimeError);
            var ic = new ImplementingClass[0];
            AssertExplicitConvert(ic, (IList<IInterface>)ic, ExpectedConversionResult.Succeed);
            var isc = new ImplementingSealedClass[0];
            AssertExplicitConvert(isc, (IList<IInterface>)isc, ExpectedConversionResult.Succeed);
            AssertExplicitConvert(new Struct[0], default(IList<IInterface>), ExpectedConversionResult.CompileError);
            AssertExplicitConvert(new SealedClass[0], default(IList<UnrelatedNonInterface>), ExpectedConversionResult.CompileError);
            AssertExplicitConvert(new UnsealedClass[0], default(IList<UnrelatedNonInterface>), ExpectedConversionResult.CompileError);
            AssertExplicitConvert(new Struct[0], default(IList<UnrelatedNonInterface>), ExpectedConversionResult.CompileError);
        }
    }
}
