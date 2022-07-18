// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

// IVT to "Microsoft.CSharp.RuntimeBinder.Binder", just to use IVT in a test (see: InternalsVisibleToTest below)
[assembly: InternalsVisibleTo("Microsoft.CSharp, PublicKey = 002400000480000094000000060200000024000052534131000400000100010007D1FA57C4AED9F0A32E84AA0FAEFD0DE9E8FD6AEC8F87FB03766C834C99921EB23BE79AD9D5DCC1DD9AD236132102900B723CF980957FC4E177108FC607774F29E8320E92EA05ECE4E821C0A5EFE8F1645C4C0C93C1AB99285D622CAA652C1DFAD63D745D6F2DE5F17E5EAF0FC4963D261C8A12436518206DC093344D5AD293")]

namespace Microsoft.CSharp.RuntimeBinder.Tests
{
    [ActiveIssue("https://github.com/dotnet/runtime/issues/26798", TargetFrameworkMonikers.NetFramework)]
    public class RuntimeBinderTests
    {
        [Fact]
        public void MultipleUseOfSameLocalInSameScope()
        {
            dynamic d0 = 23;
            dynamic d1 = 14;
            if (d0 == 23)
            {
                dynamic d2 = 19;
                d0 = d0 - d1 + d2;
                Assert.Equal(28, new string(' ', d0).Length);
            }
            dynamic dr = d0 * d1 + d0 + d0 + d0 / d1 - Math.Pow(d1, 2);
            Assert.Equal(254, dr);
        }

        private class Value<T>
        {
            public T Quantity { get; set; }
        }

        private class Holder
        {
            private object _value;

            public void Assign<T>(T value) => _value = value;

            public T Value<T>() => (T)_value;
        }

        [Fact]
        public void GenericNameMatchesPredefined()
        {
            dynamic d = 3;
            dynamic v = new Value<int> { Quantity = d };
            dynamic r = v.Quantity;
            Assert.Equal(3, r);
            dynamic h = new Holder();
            h.Assign<int>(1);
            Assert.Equal(1, h.Value<int>());
            h.Assign(2);
            Assert.Equal(2, h.Value<int>());
        }

        private static class MySite
        {
            public static CallSite<Action<CallSite, object>> mySite;
        }

        public class Class1
        {
            public static string Result = null;

            internal void Foo()
            {
                Result += "CALLED";
            }
        }

        // https://github.com/dotnet/runtime/issues/6625
        [Fact]
        public void InternalsVisibleToTest()
        {
            Class1 typed = new Class1();

            // make a callsite as if it is contained inside "Microsoft.CSharp.RuntimeBinder.RuntimeBinderException"
            MySite.mySite = CallSite<Action<CallSite, object>>.Create(Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(
                CSharpBinderFlags.ResultDiscarded,
                "Foo",
                null,
                typeof(Microsoft.CSharp.RuntimeBinder.RuntimeBinderException),
                new CSharpArgumentInfo[]
                {
                    CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
                }));

            MySite.mySite.Target(MySite.mySite, typed);

            // call should suceed because of the IVT to Microsoft.CSharp
            Assert.Equal("CALLED", Class1.Result);

            // make a callsite as if it is contained inside "System.Exception"
            MySite.mySite = CallSite<Action<CallSite, object>>.Create(Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(
                CSharpBinderFlags.ResultDiscarded,
                "Foo",
                null,
                typeof(System.Exception),
                new CSharpArgumentInfo[]
                {
                    CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
                }));

            // call should fail because "Foo" is internal to the calling context.
            Assert.Throws<Microsoft.CSharp.RuntimeBinder.RuntimeBinderException>(
                                                            () => MySite.mySite.Target(MySite.mySite, typed)
                                                         );
        }

        public class OuterType<T>
        {
            public class MyEntity
            {
                public int Id { get; set; }

                public string Name { get; set; }
            }
        }

        [Fact]
        public void AccessMemberOfNonGenericNestedInGeneric()
        {
            Func<dynamic, int> dynamicDelegate = e => e.Id;
            var dto = new OuterType<int>.MyEntity { Id = 1, Name = "Foo" };
            Assert.Equal(1, dynamicDelegate(dto));
        }

        interface ITestInterface
        {
            int this[int index] { get; }

            int Add(int arg);
        }

        interface ITestDerived : ITestInterface
        {

        }

        class TestImpl : ITestDerived
        {
            public int this[int index] => index * 2;

            public int Add(int arg) => arg + 2;
        }

        [Fact]
        public void InheritedInterfaceMethod()
        {
            ITestDerived itd = new TestImpl();
            dynamic d = 3;
            dynamic res = itd.Add(d);
            Assert.Equal(5, res);
        }


        [Fact]
        public void InheritedInterfaceIndexer()
        {
            ITestDerived itd = new TestImpl();
            dynamic d = 3;
            dynamic res = itd[d];
            Assert.Equal(6, res);
        }

        [Fact]
        public void InterfaceMethodInheritedFromObject()
        {
            ITestDerived itd = new TestImpl();
            dynamic d = itd;
            dynamic res = itd.Equals(d);
            Assert.True(res);
        }

        [Fact]
        public void CyclicTypeDefinition()
        {
            dynamic x = new Third<int>();
            Assert.Equal(0, x.Zero());
        }

        class First<T> where T : First<T>
        {
            public int Zero() => 0;
        }

        class Second<T> : First<T> where T : First<T>
        {
        }

        class Third<T> : Second<Third<T>>
        {
        }

        public class Castable
        {
            public static implicit operator int(Castable _) => 2;

            public static implicit operator string(Castable _) => "abc";
        }

        [Fact]
        public void ImplicitOperatorForPlus()
        {
            dynamic d = new Castable();
            dynamic result = d + 1;
            Assert.Equal(3, result);
            result = 5 + d;
            Assert.Equal(7, result);
            result = d + "def";
            Assert.Equal("abcdef", result);
            result = "xyz" + d;
            Assert.Equal("xyzabc", result);
        }

        [Fact]
        public void DynamicArgumentToCyclicTypeDefinition()
        {
            dynamic arg = 5;
            new Builder<object>().SomeMethod(arg);
        }

        public class Builder<TItem> : BuilderBaseEx<Builder<TItem>>
        {
            public Builder<TItem> SomeMethod(object arg)
            {
                return this;
            }
        }
        public class BuilderBaseEx<T> : BuilderBase<T> where T : BuilderBaseEx<T> { }
        public class BuilderBase<T> where T : BuilderBase<T> { }

        [Fact]
        public void CircularOnOwnNested()
        {
            dynamic d = new Generic<string>.Inner
            {
                Foo = "expected"
            };

            Assert.Equal("expected", d.Foo);
        }


        [Fact]
        public void CircularOnOwnNestedAbsentMember()
        {
            dynamic d = new Generic<string>.Inner
            {
                Foo = "expected"
            };

            Assert.Throws<RuntimeBinderException>(() => d.Bar);
        }

        class Generic<T> : BindingList<Generic<T>.Inner>
        {
            public class Inner
            {
                public object Foo { get; set; }
            }
        }

        public class SomeType
        {
            public string SomeMethod(int i) => "ABC " + i;
        }

        private class SomePrivateType
        {
            public string SomeMethod(int i) => "ABC " + i;
        }

        internal class SomeInternalType
        {
            public string SomeMethod(int i) => "ABC " + i;
        }

        protected class SomeProtectedType
        {
            public string SomeMethod(int i) => "ABC " + i;
        }

        [Fact]
        public void MethodCallWithNullContext()
        {
            CallSiteBinder binder = Binder.InvokeMember(
                CSharpBinderFlags.None, nameof(SomeType.SomeMethod), Type.EmptyTypes, null,
                new[]
                {
                    CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null),
                    CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
                });
            CallSite<Func<CallSite, object, object, object>> site =
                CallSite<Func<CallSite, object, object, object>>.Create(binder);
            Func<CallSite, object, object, object> targ = site.Target;
            object res = targ(site, new SomeType(), 9);
            Assert.Equal("ABC 9", res);
        }

        [Fact]
        public void MethodCallWithNullContextCannotSeeNonPublic()
        {
            CallSiteBinder binder = Binder.InvokeMember(
                CSharpBinderFlags.None, nameof(SomePrivateType.SomeMethod), Type.EmptyTypes, null,
                new[]
                {
                    CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null),
                    CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
                });
            CallSite<Func<CallSite, object, object, object>> site =
                CallSite<Func<CallSite, object, object, object>>.Create(binder);
            Func<CallSite, object, object, object> targ = site.Target;
            Assert.Throws<RuntimeBinderException>(() => targ(site, new SomePrivateType(), 9));
            Assert.Throws<RuntimeBinderException>(() => targ(site, new SomeInternalType(), 9));
            Assert.Throws<RuntimeBinderException>(() => targ(site, new SomeProtectedType(), 9));
        }

        interface ICounter1
        {
            int Count(ICollection x);
            double ExplicitCount { get; set; }
        }

        interface ICounter2
        {
            int Count(ICollection x);
            int ExplicitCount { get; set; }
        }

        interface ICounterBoth : ICounter1, ICounter2 { }

        [Fact]
        public void AmbiguousInterfaceInheritedMethodError()
        {
            ICounterBoth icb = null; // Error on ambiguity should happen before error on null.
            string message = Assert.Throws<RuntimeBinderException>(() => icb.Count((dynamic)new int[3])).Message;
            // The call is ambiguous between the following methods or properties:
            // 'Microsoft.CSharp.RuntimeBinder.Tests.RuntimeBinderTests.ICounter1.Count(System.Collections.ICollection)'
            // and 'Microsoft.CSharp.RuntimeBinder.Tests.RuntimeBinderTests.ICounter2.Count(System.Collections.ICollection)'
            Assert.Contains("'Microsoft.CSharp.RuntimeBinder.Tests.RuntimeBinderTests.ICounter1.Count(System.Collections.ICollection)'", message);
            Assert.Contains("'Microsoft.CSharp.RuntimeBinder.Tests.RuntimeBinderTests.ICounter2.Count(System.Collections.ICollection)'", message);
        }

        [Fact]
        public void AmbiguousMemberError()
        {
            CallSite<Func<CallSite, ICounterBoth, int, object>> compileTimeTypeValueSetter =
                CallSite<Func<CallSite, ICounterBoth, int, object>>.Create(
                    Binder.SetMember(
                        CSharpBinderFlags.None, "ExplicitCount", GetType(),
                        new[]
                        {
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.UseCompileTimeType, null),
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.UseCompileTimeType, null)
                        }));
            Func<CallSite, ICounterBoth, int, object> target0 = compileTimeTypeValueSetter.Target;
            string message = Assert.Throws<RuntimeBinderException>(() => target0(compileTimeTypeValueSetter, null, 2)).Message;
            // Ambiguity between 'Microsoft.CSharp.RuntimeBinder.Tests.RuntimeBinderTests.ICounter1.ExplicitCount'
            // and 'Microsoft.CSharp.RuntimeBinder.Tests.RuntimeBinderTests.ICounter2.ExplicitCount'
            Assert.Contains("'Microsoft.CSharp.RuntimeBinder.Tests.RuntimeBinderTests.ICounter1.ExplicitCount'", message);
            Assert.Contains("'Microsoft.CSharp.RuntimeBinder.Tests.RuntimeBinderTests.ICounter2.ExplicitCount'", message);

            CallSite<Func<CallSite, ICounterBoth, object, object>> runTimeTypeValueSetter =
                CallSite<Func<CallSite, ICounterBoth, object, object>>.Create(
                    Binder.SetMember(
                        CSharpBinderFlags.None, "ExplicitCount", GetType(),
                        new[]
                        {
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.UseCompileTimeType, null),
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
                        }));
            var target1 = runTimeTypeValueSetter.Target;
            Assert.Equal(message, Assert.Throws<RuntimeBinderException>(() => target1(runTimeTypeValueSetter, null, 2)).Message);

            CallSite<Func<CallSite, ICounterBoth, object>> getter =
                CallSite<Func<CallSite, ICounterBoth, object>>.Create(
                    Binder.GetMember(
                        CSharpBinderFlags.None, "ExplicitCount", GetType(),
                        new[]
                        {
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.UseCompileTimeType, null)
                        }));

            Func<CallSite, ICounterBoth, object> target2 = getter.Target;
            Assert.Equal(message, Assert.Throws<RuntimeBinderException>(() => target2(getter, null)).Message);
        }
    }
}
