// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#define TEST_DEFINITION

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Threading;
using Xunit;

namespace Microsoft.CSharp.RuntimeBinder.Tests
{
    public class BindingErrors
    {
        private class TypeWithConditional
        {
            [Conditional("TEST_DEFINITION")]
            public virtual void DoNothing()
            {
            }
        }

        private class DerivedTypeWithConditional : TypeWithConditional
        {
            public override void DoNothing()
            {
            }
        }

        private class TypeWithEvent
        {
            public event Action<object, EventArgs> Event;

            protected virtual void OnTrigger(EventArgs e)
            {
                Event?.Invoke(this, e);
            }
        }

        private class TypeWithOverloads
        {
            public void DoNothing(int x, long y)
            {
            }

            public void DoNothing(long x, int y)
            {
            }
        }

        // So the static binder can't decide some cases are/are not static at compilation stage
        class StaticAndInstanceSameName
        {
            public void DoSomething(double d)
            {
            }

            public static void DoSomething(int i)
            {
            }
        }

        private class AmbiguousNumClass
        {
            private readonly int _num;

            public AmbiguousNumClass(int num) => _num = num;

            public static implicit operator double(AmbiguousNumClass nc) => nc._num;

            public static implicit operator decimal(AmbiguousNumClass nc) => nc._num;
        }

        public static T ReturnRef<T>(T item) where T : class
        {
            return item;
        }

        public static T ReturnVal<T>(T item) where T : struct
        {
            return item;
        }

        private class NonGeneric
        {
            public int Prop => 1;

            public int Meth() => 1;
        }

        private class Constraints
        {
            public void MustBeConvertible<T>(T arg) where T:IConvertible
            {
            }

            public void MustBeStruct<T>(T arg) where T : struct
            {
            }

            public void MustBeDerived<TDerived, TBase>(TDerived d, TBase b) where TDerived : TBase { }
        }

        [Fact]
        public void CannotBindToConditional()
        {
            var obj = new TypeWithConditional();
            obj.DoNothing(); // Confirm can bind statically.
            dynamic d = obj;
            Assert.Throws<RuntimeBinderException>(() => d.DoNothing());
        }

        [Fact]
        public void CannotBindToOverriddenConditional()
        {
            var obj = new DerivedTypeWithConditional();
            obj.DoNothing(); // Confirm can bind statically.
            dynamic d = obj;
            Assert.Throws<RuntimeBinderException>(() => d.DoNothing());
        }

        [Fact]
        public void CannotBindToEventAsProperty()
        {
            dynamic d = new TypeWithEvent();
            Assert.Throws<RuntimeBinderException>(() => d.Event = 3);
            Assert.Throws<RuntimeBinderException>(() =>
            {
                int x = d.Event;
            });
        }

        [Fact]
        public void CannotBindToEventAsInvokable()
        {
            dynamic d = new TypeWithEvent();
            Assert.Throws<RuntimeBinderException>(() => d.Event(new EventArgs()));
        }

        [Fact]
        public void MethodOnNullReference()
        {
            dynamic d = null;
            Assert.Throws<RuntimeBinderException>(() => d.DoStuff());
        }

        [Fact]
        public void PropertyOrFieldOnNullReference()
        {
            dynamic d = null;
            Assert.Throws<RuntimeBinderException>(() => d.Value = 3);
            Assert.Throws<RuntimeBinderException>(() =>
            {
                int x = d.Value;
            });
        }

        [Fact]
        public void CannotConvertVoid()
        {
            dynamic d = new List<int>();
            Assert.Throws<RuntimeBinderException>(() => d.Add(1).ToString());
            Assert.Throws<RuntimeBinderException>(() =>
            {
                int i = d.Add(1);
            });
        }

        [Fact]
        public void MethodGroupLikeProperty()
        {
            dynamic d = new List<int>();
            Assert.Throws<RuntimeBinderException>(() => d.Add = 42);
            Assert.Throws<RuntimeBinderException>(() =>
            {
                int i = d.Add;
            });
        }

        [Fact]
        public void PropertyLikeMethod()
        {
            dynamic d = new List<int>();
            Assert.Throws<RuntimeBinderException>(() => d.Add = 42);
            Assert.Throws<RuntimeBinderException>(() =>
            {
                int i = d.Add;
            });
        }

        [Fact]
        public void AmbiguousOverloadAsTarget()
        {
            dynamic d = new TypeWithOverloads();
            Assert.Throws<RuntimeBinderException>(() => d.DoNothing(1, 2));
        }

        [Fact]
        public void AmbiguousOverloadAsArgument()
        {
            TypeWithOverloads target = new TypeWithOverloads();
            dynamic d = 2;
            Assert.Throws<RuntimeBinderException>(() => target.DoNothing(1, d));
        }

        [Fact]
        public void RefConstraintMethod()
        {
            dynamic d = 3;
            Assert.Throws<RuntimeBinderException>(() => ReturnRef(d));
        }

        [Fact]
        public void ValConstraintMethod()
        {
            dynamic d = "abc";
            dynamic dThis = this; // Or else the fact that d is really object means this can't compile
            Assert.Throws<RuntimeBinderException>(() => dThis.ReturnVal(d));
        }

        [Fact]
        public void ExplicitlyCallOperator()
        {
            dynamic d = 23m;
            Assert.Throws<RuntimeBinderException>(() => d.op_Increment());
        }

        [Fact]
        public void ExplicitlyCallPropertyAccessor()
        {
            dynamic d = "abc";
            Assert.Throws<RuntimeBinderException>(() => d.get_Length());
        }

        [Fact]
        public void CastToStatic()
        {
            CallSite<Func<CallSite, object, object>> site = CallSite<Func<CallSite, object, object>>.Create(
                Binder.Convert(CSharpBinderFlags.ConvertExplicit, typeof(Binder), GetType()));
            Func<CallSite, object, object> targ = site.Target;
            Assert.Throws<RuntimeBinderException>(() => targ(site, "abc"));
        }

        [Fact]
        public void AmbiguousUnaryOp()
        {
            dynamic d = new AmbiguousNumClass(7);
            Assert.Throws<RuntimeBinderException>(() => -d);
        }

        [Fact]
        public void StaticCallOnInstance()
        {
            dynamic d = new StaticAndInstanceSameName();
            d.DoSomething(2.0); // No exception
            Assert.Throws<RuntimeBinderException>(() => d.DoSomething(2));
            d = 2.0;
            new StaticAndInstanceSameName().DoSomething(d); // No exception
            d = 2;
            Assert.Throws<RuntimeBinderException>(() => new StaticAndInstanceSameName().DoSomething(d));
        }

        [Fact]
        public void InstanceCallOnType()
        {
            dynamic d = 2;
            StaticAndInstanceSameName.DoSomething(d); // No exception
            d = 2.0;
            Assert.Throws<RuntimeBinderException>(() => StaticAndInstanceSameName.DoSomething(d));
        }

        [Fact]
        public void CtorCallOnNoCtorType()
        {
            CallSite<Func<CallSite, Type, double>> callSite = CallSite<Func<CallSite, Type, double>>.Create(
                Binder.InvokeConstructor(
                    CSharpBinderFlags.InvokeSpecialName, GetType(),
                    new[]
                    {
                        CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
                    }));
            Func<CallSite, Type, double> target = callSite.Target;
            Assert.Throws<RuntimeBinderException>(() => target(callSite, typeof(double)));
        }

        [Fact]
        public void NullaryCtorCallOnNoNullaryCtor()
        {
            CallSite<Func<CallSite, Type, string>> callSite = CallSite<Func<CallSite, Type, string>>.Create(
                Binder.InvokeConstructor(
                    CSharpBinderFlags.InvokeSpecialName, GetType(),
                    new[]
                    {
                        CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
                    }));
            Func<CallSite, Type, string> target = callSite.Target;
            RuntimeBinderException rbe = Assert.Throws<RuntimeBinderException>(() => target(callSite, typeof(string)));
            Assert.Contains("0", rbe.Message);
        }

        [Fact]
        public void QuinaryCtorCallOnNoQuinaryCtor()
        {
            CallSite<Func<CallSite, Type, object, object, object, object, object, object>> callSite = CallSite<Func<CallSite, Type, object, object, object, object, object, object>>.Create(
                Binder.InvokeConstructor(CSharpBinderFlags.InvokeSpecialName, GetType(),
                    new[]{
                        CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null),
                        CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null),
                        CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null),
                        CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null),
                        CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null),
                        CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)}));
            Func<CallSite, Type, object, object, object, object, object, object> target = callSite.Target;
            RuntimeBinderException rbe = Assert.Throws<RuntimeBinderException>(() => target.Invoke(callSite, typeof(string), null, null, null, null, null));
            Assert.Contains("5", rbe.Message);
        }

        [Fact]
        public void NoSuchMember()
        {
            dynamic d = 3;
            Assert.Throws<RuntimeBinderException>(() => d.Test());
        }

        [Fact]
        public void NoTypeVarsOnProperty()
        {
            dynamic d = new NonGeneric();
            Assert.Throws<RuntimeBinderException>(() => d.Prop<int>());
        }

        [Fact]
        public void NoTypeVarsOnNonGenericMethod()
        {
            dynamic d = new NonGeneric();
            Assert.Throws<RuntimeBinderException>(() => d.Meth<int>());
        }

        [Fact]
        public void NullableDoesNotSatisfyInterfaceConstraints()
        {
            dynamic d = new Constraints();
            int? i = 3;
            Assert.Throws<RuntimeBinderException>(() => d.MustBeConvertible(i));
        }

        [Fact]
        public void NullableDoesNotSatisfyStructConstraints()
        {
            dynamic d = new Constraints();
            int? i = 3;
            Assert.Throws<RuntimeBinderException>(() => d.MustBeStruct(i));
        }

        [Fact]
        public void NullableDoesNotSatisfyConstraints()
        {
            dynamic d = new Constraints();
            int? n = 3;
            int i = 3;
            Assert.Throws<RuntimeBinderException>(() => d.MustBeDerived(n, i));
        }

        [Fact]
        public void NullableDoesNotSatisfyConstraintsEnum()
        {
            dynamic d = new Constraints();
            StringComparison? n = StringComparison.CurrentCulture;
            Enum e = StringComparison.CurrentCulture;
            Assert.Throws<RuntimeBinderException>(() => d.MustBeDerived(n, e));
        }

        [Fact]
        public void DuplicateNamedArgument()
        {
            CallSite<Func<CallSite, object, object, object, object>> site =
                CallSite<Func<CallSite, object, object, object, object>>.Create(
                    Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(
                        CSharpBinderFlags.None, "Equals", null, GetType(),
                        new[]
                        {
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null),
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.NamedArgument, "x"),
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.NamedArgument, "x")
                        }));
            Func<CallSite, object, object, object, object> target = site.Target;
            Assert.Throws<RuntimeBinderException>(() => target.Invoke(site, EqualityComparer<int>.Default, 2, 2));
        }

        public static IEnumerable<object[]> WrongArgumentCounts(int correct) =>
            Enumerable.Range(0, 5).Where(i => i != correct).Select(i => new object[] {i});

        [Theory, MemberData(nameof(WrongArgumentCounts), 2)]
        public void BinaryOperatorWrongNumberArguments(int argumentCount)
        {
            CSharpArgumentInfo x = CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null);
            CSharpArgumentInfo y = CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null);
            CallSiteBinder binder =
                Binder.BinaryOperation(
                    CSharpBinderFlags.None, ExpressionType.Add,
                    GetType(), new[] { x, y });
            LabelTarget target = Expression.Label();
            object[] args = Enumerable.Range(0, argumentCount).Select(i => (object)i).ToArray();
            ReadOnlyCollection<ParameterExpression> parameters = Enumerable.Range(0, argumentCount)
                .Select(_ => Expression.Parameter(typeof(int)))
                .ToList()
                .AsReadOnly();
            // Throws ArgumentOutOfRangeException for zero arguments, ArgumentException for 1 or 3 or more.
            Assert.ThrowsAny<ArgumentException>(() => binder.Bind(args, parameters, target));
        }

        private static void DoStuff<T>(IEnumerable<T> x)
        {
            // Don't actually do stuff!
        }

        [Fact]
        public void CannotInferTypeArgument()
        {
            dynamic d = new object();
            Assert.Throws<RuntimeBinderException>(() => DoStuff(d));
        }

        public class Outer
        {
            public class Inner
            {
                public void DoNothing()
                {
                }
            }
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/26798", TargetFrameworkMonikers.NetFramework)]
        public void TryInvokeOrAccessNestedClassAsMember()
        {
            dynamic dFirst = new Outer.Inner();
            dFirst.DoNothing();
            dynamic d = new Outer();
            Assert.Throws<RuntimeBinderException>(() => d.Inner<int>());
            Assert.Throws<RuntimeBinderException>(() => d.Inner());
            Assert.Throws<RuntimeBinderException>(() => d.Inner = 2);
            Assert.Throws<RuntimeBinderException>(
                () =>
                {
                    int i = d.Inner<int>();
                });
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/26798", TargetFrameworkMonikers.NetFramework)]
        public void TryInvokeTypeParameterAsMember()
        {
            dynamic d = new List<int>();
            Assert.Throws<RuntimeBinderException>(() => d.T);
            Assert.Throws<RuntimeBinderException>(() => d.T());
            Assert.Throws<RuntimeBinderException>(() => d.T<int>());
            Assert.Throws<RuntimeBinderException>(() =>
            {
                int i = d.T;
            });
        }

        public class BaseForOuterWithMethod
        {
            public int Inner() => 42;
        }

        public class DerivedOuterHidingMethod : BaseForOuterWithMethod
        {
            public new class Inner
            {
                public void DoNothing()
                {
                }
            }
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/26798", TargetFrameworkMonikers.NetFramework)]
        public void AccessMethodHiddenByNested()
        {
            dynamic dFirst = new DerivedOuterHidingMethod.Inner();
            dFirst.DoNothing();
            dynamic d = new DerivedOuterHidingMethod();
            Assert.Equal(42, d.Inner());
        }

        public class BaseForOuterWithNested
        {
            public class Inner
            {
                public void DoNothing()
                {
                }
            }
        }

        public class DerivedOuterHidingNested : BaseForOuterWithNested
        {
            public new int Inner() => 42;
        }

        [Fact]
        public void AccessMethodHidingNested()
        {
            dynamic dFirst = new BaseForOuterWithNested.Inner();
            dFirst.DoNothing();
            dynamic d = new DerivedOuterHidingNested();
            Assert.Equal(42, d.Inner());
        }

        [Fact]
        public void CannotCallOperatorDirectly()
        {
            CultureInfo prev = CultureInfo.CurrentCulture;
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            dynamic d = "";
            RuntimeBinderException e = Assert.Throws<RuntimeBinderException>(() => d.op_Equality("", ""));
            Assert.Equal("'string.operator ==(string, string)': cannot explicitly call operator or accessor", e.Message);
            Thread.CurrentThread.CurrentCulture = prev;
        }

        [Fact]
        public void CannotCallAccessorDirectly()
        {
            CultureInfo prev = CultureInfo.CurrentCulture;
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            dynamic d = "";
            RuntimeBinderException e = Assert.Throws<RuntimeBinderException>(() => d.get_Length());
            Assert.Equal("'string.Length.get': cannot explicitly call operator or accessor", e.Message);
            Thread.CurrentThread.CurrentCulture = prev;
        }

        [Fact]
        public void AllowIndexerAccess()
        {
            // Indexers' accessors can be accessed directly. This is against the C# rules, which only allow
            // direct access of indexer accessors when they are not the default member as C# has no other
            // way to express such access, but being stricter would be a breaking change.
            List<int> list = new List<int> { 1, 2, 3 };
            dynamic d = list;
            d.set_Item(2, 4);
            dynamic e = d.get_Item(2);
            Assert.Equal(4, e);
            Assert.Equal(4, list[2]);
        }

        [Fact]
        public void AllowStringIndexerAccess()
        {
            dynamic d = "abcd";
            char c = d.get_Chars(2);
            Assert.Equal('c', c);
        }

        private sealed class InitOnlyProperty
        {
            public InitOnlyProperty() { }
            public InitOnlyProperty(int value)
            {
                ((dynamic)this).P = value;
            }
            public int P { get; init; }
            public int Q
            {
                get { return P; }
                init { ((dynamic)this).P = value; }
            }
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
        public void InitOnlyProperty_Set()
        {
            dynamic d = new InitOnlyProperty();
            Assert.Throws<RuntimeBinderException>(() => d.P = 1);
        }

        [Fact]
        public void InitOnlyProperty_SetAccessor()
        {
            dynamic d = new InitOnlyProperty();
            Assert.Throws<RuntimeBinderException>(() => d.set_P(1));
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
        public void InitOnlyProperty_SetInConstructor()
        {
            Assert.Throws<RuntimeBinderException>(() => new InitOnlyProperty(1));
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
        public void InitOnlyProperty_SetInInitAccessor()
        {
            Assert.Throws<RuntimeBinderException>(() => new InitOnlyProperty() { Q = 1 });
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
        public void InitOnlyProperty_Increment()
        {
            dynamic d = new InitOnlyProperty();
            Assert.Throws<RuntimeBinderException>(() => d.P++);
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
        public void InitOnlyProperty_CompoundAssignment()
        {
            dynamic d = new InitOnlyProperty();
            Assert.Throws<RuntimeBinderException>(() => d.P += 1);
        }
    }
}
