// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Reflection;
using Xunit;

namespace System.Linq.Expressions.Tests
{
    public class SwitchTests
    {
        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public void IntSwitch1(bool useInterpreter)
        {
            ParameterExpression p = Expression.Parameter(typeof(int));
            ParameterExpression p1 = Expression.Parameter(typeof(string));
            SwitchExpression s = Expression.Switch(p,
                Expression.Assign(p1, Expression.Constant("default")),
                Expression.SwitchCase(Expression.Assign(p1, Expression.Constant("hello")), Expression.Constant(1)),
                Expression.SwitchCase(Expression.Assign(p1, Expression.Constant("two")), Expression.Constant(2)),
                Expression.SwitchCase(Expression.Assign(p1, Expression.Constant("lala")), Expression.Constant(1)));

            BlockExpression block = Expression.Block(new ParameterExpression[] { p1 }, s, p1);

            Func<int, string> f = Expression.Lambda<Func<int, string>>(block, p).Compile(useInterpreter);

            Assert.Equal("hello", f(1));
            Assert.Equal("two", f(2));
            Assert.Equal("default", f(3));
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public void NullableIntSwitch1(bool useInterpreter)
        {
            ParameterExpression p = Expression.Parameter(typeof(int?));
            ParameterExpression p1 = Expression.Parameter(typeof(string));
            SwitchExpression s = Expression.Switch(p,
                Expression.Assign(p1, Expression.Constant("default")),
                Expression.SwitchCase(Expression.Assign(p1, Expression.Constant("hello")), Expression.Constant((int?)1, typeof(int?))),
                Expression.SwitchCase(Expression.Assign(p1, Expression.Constant("two")), Expression.Constant((int?)2, typeof(int?))),
                Expression.SwitchCase(Expression.Assign(p1, Expression.Constant("lala")), Expression.Constant((int?)1, typeof(int?))));

            BlockExpression block = Expression.Block(new ParameterExpression[] { p1 }, s, p1);

            Func<int?, string> f = Expression.Lambda<Func<int?, string>>(block, p).Compile(useInterpreter);

            Assert.Equal("hello", f(1));
            Assert.Equal("two", f(2));
            Assert.Equal("default", f(null));
            Assert.Equal("default", f(3));
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public void NullableIntSwitch2(bool useInterpreter)
        {
            ParameterExpression p = Expression.Parameter(typeof(int?));
            ParameterExpression p1 = Expression.Parameter(typeof(string));
            SwitchExpression s = Expression.Switch(p,
                Expression.Assign(p1, Expression.Constant("default")),
                Expression.SwitchCase(Expression.Assign(p1, Expression.Constant("hello")), Expression.Constant((int?)1, typeof(int?))),
                Expression.SwitchCase(Expression.Assign(p1, Expression.Constant("two")), Expression.Constant((int?)2, typeof(int?))),
                Expression.SwitchCase(Expression.Assign(p1, Expression.Constant("null")), Expression.Constant((int?)null, typeof(int?))),
                Expression.SwitchCase(Expression.Assign(p1, Expression.Constant("lala")), Expression.Constant((int?)1, typeof(int?))));

            BlockExpression block = Expression.Block(new ParameterExpression[] { p1 }, s, p1);

            Func<int?, string> f = Expression.Lambda<Func<int?, string>>(block, p).Compile(useInterpreter);

            Assert.Equal("hello", f(1));
            Assert.Equal("two", f(2));
            Assert.Equal("null", f(null));
            Assert.Equal("default", f(3));
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public void IntSwitch2(bool useInterpreter)
        {
            ParameterExpression p = Expression.Parameter(typeof(byte));
            ParameterExpression p1 = Expression.Parameter(typeof(string));
            SwitchExpression s = Expression.Switch(p,
                Expression.Assign(p1, Expression.Constant("default")),
                Expression.SwitchCase(Expression.Assign(p1, Expression.Constant("hello")), Expression.Constant((byte)1)),
                Expression.SwitchCase(Expression.Assign(p1, Expression.Constant("two")), Expression.Constant((byte)2)),
                Expression.SwitchCase(Expression.Assign(p1, Expression.Constant("lala")), Expression.Constant((byte)1)));

            BlockExpression block = Expression.Block(new ParameterExpression[] { p1 }, s, p1);

            Func<byte, string> f = Expression.Lambda<Func<byte, string>>(block, p).Compile(useInterpreter);

            Assert.Equal("hello", f(1));
            Assert.Equal("two", f(2));
            Assert.Equal("default", f(3));
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public void IntSwitch3(bool useInterpreter)
        {
            ParameterExpression p = Expression.Parameter(typeof(uint));
            ParameterExpression p1 = Expression.Parameter(typeof(string));
            SwitchExpression s = Expression.Switch(p,
                Expression.Assign(p1, Expression.Constant("default")),
                Expression.SwitchCase(Expression.Assign(p1, Expression.Constant("hello")), Expression.Constant((uint)1)),
                Expression.SwitchCase(Expression.Assign(p1, Expression.Constant("two")), Expression.Constant((uint)2)),
                Expression.SwitchCase(Expression.Assign(p1, Expression.Constant("lala")), Expression.Constant((uint)1)),
                Expression.SwitchCase(Expression.Assign(p1, Expression.Constant("wow")), Expression.Constant(uint.MaxValue)));

            BlockExpression block = Expression.Block(new ParameterExpression[] { p1 }, s, p1);

            Func<uint, string> f = Expression.Lambda<Func<uint, string>>(block, p).Compile(useInterpreter);

            Assert.Equal("hello", f(1));
            Assert.Equal("wow", f(uint.MaxValue));
            Assert.Equal("two", f(2));
            Assert.Equal("default", f(3));
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public void StringSwitch(bool useInterpreter)
        {
            ParameterExpression p = Expression.Parameter(typeof(string));
            SwitchExpression s = Expression.Switch(p,
                Expression.Constant("default"),
                Expression.SwitchCase(Expression.Constant("hello"), Expression.Constant("hi")),
                Expression.SwitchCase(Expression.Constant("lala"), Expression.Constant("bye")));

            Func<string, string> f = Expression.Lambda<Func<string, string>>(s, p).Compile(useInterpreter);

            Assert.Equal("hello", f("hi"));
            Assert.Equal("lala", f("bye"));
            Assert.Equal("default", f("hi2"));
            Assert.Equal("default", f(null));
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public void StringSwitch1(bool useInterpreter)
        {
            ParameterExpression p = Expression.Parameter(typeof(string));
            ParameterExpression p1 = Expression.Parameter(typeof(string));
            SwitchExpression s = Expression.Switch(p,
                Expression.Assign(p1, Expression.Constant("default")),
                Expression.SwitchCase(Expression.Assign(p1, Expression.Constant("hello")), Expression.Constant("hi")),
                Expression.SwitchCase(Expression.Assign(p1, Expression.Constant("null")), Expression.Constant(null, typeof(string))),
                Expression.SwitchCase(Expression.Assign(p1, Expression.Constant("lala")), Expression.Constant("bye")));

            BlockExpression block = Expression.Block(new ParameterExpression[] { p1 }, s, p1);

            Func<string, string> f = Expression.Lambda<Func<string, string>>(block, p).Compile(useInterpreter);

            Assert.Equal("hello", f("hi"));
            Assert.Equal("lala", f("bye"));
            Assert.Equal("default", f("hi2"));
            Assert.Equal("null", f(null));
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public void StringSwitchNotConstant(bool useInterpreter)
        {
            Expression<Func<string>> expr1 = () => new string('a', 5);
            Expression<Func<string>> expr2 = () => new string('q', 5);

            ParameterExpression p = Expression.Parameter(typeof(string));
            SwitchExpression s = Expression.Switch(p,
                Expression.Constant("default"),
                Expression.SwitchCase(Expression.Invoke(expr1), Expression.Invoke(expr2)),
                Expression.SwitchCase(Expression.Constant("lala"), Expression.Constant("bye")));

            Func<string, string> f = Expression.Lambda<Func<string, string>>(s, p).Compile(useInterpreter);

            Assert.Equal("aaaaa", f("qqqqq"));
            Assert.Equal("lala", f("bye"));
            Assert.Equal("default", f("hi2"));
            Assert.Equal("default", f(null));
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public void ObjectSwitch1(bool useInterpreter)
        {
            ParameterExpression p = Expression.Parameter(typeof(object));
            ParameterExpression p1 = Expression.Parameter(typeof(string));
            SwitchExpression s = Expression.Switch(p,
                Expression.Assign(p1, Expression.Constant("default")),
                Expression.SwitchCase(Expression.Assign(p1, Expression.Constant("hello")), Expression.Constant("hi")),
                Expression.SwitchCase(Expression.Assign(p1, Expression.Constant("null")), Expression.Constant(null, typeof(string))),
                Expression.SwitchCase(Expression.Assign(p1, Expression.Constant("lala")), Expression.Constant("bye")),
                Expression.SwitchCase(Expression.Assign(p1, Expression.Constant("lalala")), Expression.Constant("hi")));

            BlockExpression block = Expression.Block(new ParameterExpression[] { p1 }, s, p1);

            Func<object, string> f = Expression.Lambda<Func<object, string>>(block, p).Compile(useInterpreter);

            Assert.Equal("hello", f("hi"));
            Assert.Equal("lala", f("bye"));
            Assert.Equal("default", f("hi2"));
            Assert.Equal("default", f("HI"));
            Assert.Equal("null", f(null));
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public void DefaultOnlySwitch(bool useInterpreter)
        {
            ParameterExpression p = Expression.Parameter(typeof(int));
            SwitchExpression s = Expression.Switch(p, Expression.Constant(42));

            Func<int, int> fInt32Int32 = Expression.Lambda<Func<int, int>>(s, p).Compile(useInterpreter);

            Assert.Equal(42, fInt32Int32(0));
            Assert.Equal(42, fInt32Int32(1));
            Assert.Equal(42, fInt32Int32(-1));

            s = Expression.Switch(typeof(object), p, Expression.Constant("A test string"), null);

            Func<int, object> fInt32Object = Expression.Lambda<Func<int, object>>(s, p).Compile(useInterpreter);

            Assert.Equal("A test string", fInt32Object(0));
            Assert.Equal("A test string", fInt32Object(1));
            Assert.Equal("A test string", fInt32Object(-1));

            p = Expression.Parameter(typeof(string));
            s = Expression.Switch(p, Expression.Constant("foo"));

            Func<string, string> fStringString = Expression.Lambda<Func<string, string>>(s, p).Compile(useInterpreter);

            Assert.Equal("foo", fStringString("bar"));
            Assert.Equal("foo", fStringString(null));
            Assert.Equal("foo", fStringString("foo"));
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public void NoDefaultOrCasesSwitch(bool useInterpreter)
        {
            ParameterExpression p = Expression.Parameter(typeof(int));
            SwitchExpression s = Expression.Switch(p);

            Action<int> f = Expression.Lambda<Action<int>>(s, p).Compile(useInterpreter);

            f(0);

            Assert.Equal(s.Type, typeof(void));
        }

        [Fact]
        public void TypedNoDefaultOrCasesSwitch()
        {
            ParameterExpression p = Expression.Parameter(typeof(int));
            // A SwitchExpression with neither a defaultBody nor any cases can not be any type except void.
            Assert.Throws<ArgumentException>("defaultBody", () => Expression.Switch(typeof(int), p, null, null));
        }

        private delegate int RefSettingDelegate(ref bool changed);

        private delegate void JustRefSettingDelegate(ref bool changed);

        public static int QuestionMeaning(ref bool changed)
        {
            changed = true;
            return 42;
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public void DefaultOnlySwitchWithSideEffect(bool useInterpreter)
        {
            bool changed = false;
            ParameterExpression pOut = Expression.Parameter(typeof(bool).MakeByRefType(), "changed");
            MethodCallExpression switchValue = Expression.Call(typeof(SwitchTests).GetMethod(nameof(QuestionMeaning)), pOut);
            SwitchExpression s = Expression.Switch(switchValue, Expression.Constant(42));

            RefSettingDelegate fInt32Int32 = Expression.Lambda<RefSettingDelegate>(s, pOut).Compile(useInterpreter);

            Assert.False(changed);
            Assert.Equal(42, fInt32Int32(ref changed));
            Assert.True(changed);
            changed = false;
            Assert.Equal(42, fInt32Int32(ref changed));
            Assert.True(changed);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public void NoDefaultOrCasesSwitchWithSideEffect(bool useInterpreter)
        {
            bool changed = false;
            ParameterExpression pOut = Expression.Parameter(typeof(bool).MakeByRefType(), "changed");
            MethodCallExpression switchValue = Expression.Call(typeof(SwitchTests).GetMethod(nameof(QuestionMeaning)), pOut);
            SwitchExpression s = Expression.Switch(switchValue, (Expression)null);

            JustRefSettingDelegate f = Expression.Lambda<JustRefSettingDelegate>(s, pOut).Compile(useInterpreter);

            Assert.False(changed);
            f(ref changed);
            Assert.True(changed);
        }

        public class TestComparers
        {
            public static bool CaseInsensitiveStringCompare(string s1, string s2)
            {
                return StringComparer.OrdinalIgnoreCase.Equals(s1, s2);
            }
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public void SwitchWithComparison(bool useInterpreter)
        {
            ParameterExpression p = Expression.Parameter(typeof(string));
            ParameterExpression p1 = Expression.Parameter(typeof(string));
            SwitchExpression s = Expression.Switch(p,
                Expression.Assign(p1, Expression.Constant("default")),
                typeof(TestComparers).GetMethod(nameof(TestComparers.CaseInsensitiveStringCompare)),
                Expression.SwitchCase(Expression.Assign(p1, Expression.Constant("hello")), Expression.Constant("hi")),
                Expression.SwitchCase(Expression.Assign(p1, Expression.Constant("null")), Expression.Constant(null, typeof(string))),
                Expression.SwitchCase(Expression.Assign(p1, Expression.Constant("lala")), Expression.Constant("bye")),
                Expression.SwitchCase(Expression.Assign(p1, Expression.Constant("lalala")), Expression.Constant("hi")));

            BlockExpression block = Expression.Block(new ParameterExpression[] { p1 }, s, p1);

            Func<string, string> f = Expression.Lambda<Func<string, string>>(block, p).Compile(useInterpreter);

            Assert.Equal("hello", f("hi"));
            Assert.Equal("lala", f("bYe"));
            Assert.Equal("default", f("hi2"));
            Assert.Equal("hello", f("HI"));
            Assert.Equal("null", f(null));
        }

        [Fact]
        public void NullSwitchValue()
        {
            Assert.Throws<ArgumentNullException>("switchValue", () => Expression.Switch(null));
            Assert.Throws<ArgumentNullException>("switchValue", () => Expression.Switch(null, Expression.Empty()));
            Assert.Throws<ArgumentNullException>("switchValue", () => Expression.Switch(null, Expression.Empty(), default(MethodInfo)));
            Assert.Throws<ArgumentNullException>("switchValue", () => Expression.Switch(null, Expression.Empty(), default(MethodInfo), Enumerable.Empty<SwitchCase>()));
            Assert.Throws<ArgumentNullException>("switchValue", () => Expression.Switch(typeof(int), null, Expression.Constant(1), default(MethodInfo)));
            Assert.Throws<ArgumentNullException>("switchValue", () => Expression.Switch(typeof(int), null, Expression.Constant(1), default(MethodInfo), Enumerable.Empty<SwitchCase>()));
        }

        [Fact]
        public void VoidSwitchValue()
        {
            Assert.Throws<ArgumentException>("switchValue", () => Expression.Switch(Expression.Empty()));
            Assert.Throws<ArgumentException>("switchValue", () => Expression.Switch(Expression.Empty(), Expression.Empty()));
            Assert.Throws<ArgumentException>("switchValue", () => Expression.Switch(Expression.Empty(), Expression.Empty(), default(MethodInfo)));
            Assert.Throws<ArgumentException>("switchValue", () => Expression.Switch(Expression.Empty(), Expression.Empty(), default(MethodInfo), Enumerable.Empty<SwitchCase>()));
            Assert.Throws<ArgumentException>("switchValue", () => Expression.Switch(typeof(int), Expression.Empty(), Expression.Constant(1), default(MethodInfo)));
            Assert.Throws<ArgumentException>("switchValue", () => Expression.Switch(typeof(int), Expression.Empty(), Expression.Constant(1), default(MethodInfo), Enumerable.Empty<SwitchCase>()));
        }

        private static IEnumerable<object[]> ComparisonsWithInvalidParmeterCounts()
        {
            Func<bool> nullary = () => true;
            yield return new object[] { nullary.GetMethodInfo() };
            Func<int, bool> unary = x => x % 2 == 0;
            yield return new object[] { unary.GetMethodInfo() };
            Func<int, int, int, bool> ternary = (x, y, z) => (x == y) == (y == z);
            yield return new object[] { ternary.GetMethodInfo() };
            Func<int, int, int, int, bool> quaternary = (a, b, c, d) => (a == b) == (c == d);
            yield return new object[] { quaternary.GetMethodInfo() };
        }

        [Theory, MemberData(nameof(ComparisonsWithInvalidParmeterCounts))]
        public void InvalidComparisonMethodParameterCount(MethodInfo comparison)
        {
            Assert.Throws<ArgumentException>("comparison", () => Expression.Switch(Expression.Constant(0), Expression.Empty(), comparison));
            Assert.Throws<ArgumentException>("comparison", () => Expression.Switch(Expression.Constant(0), Expression.Empty(), comparison, Enumerable.Empty<SwitchCase>()));
            Assert.Throws<ArgumentException>("comparison", () => Expression.Switch(typeof(int), Expression.Constant(0), Expression.Constant(1), comparison));
            Assert.Throws<ArgumentException>("comparison", () => Expression.Switch(typeof(int), Expression.Constant(0), Expression.Constant(1), comparison, Enumerable.Empty<SwitchCase>()));
        }

        [Fact]
        public void ComparisonLeftParameterIncorrect()
        {
            Func<string, int, bool> isLength = (x, y) => (x?.Length).GetValueOrDefault() == y;
            MethodInfo comparer = isLength.GetMethodInfo();
            Assert.Throws<ArgumentException>(null, () => Expression.Switch(Expression.Constant(0), Expression.Empty(), comparer));
            Assert.Throws<ArgumentException>(null, () => Expression.Switch(Expression.Constant(0), Expression.Empty(), comparer, Enumerable.Empty<SwitchCase>()));
            Assert.Throws<ArgumentException>(null, () => Expression.Switch(typeof(int), Expression.Constant(0), Expression.Constant(1), comparer));
            Assert.Throws<ArgumentException>(null, () => Expression.Switch(typeof(int), Expression.Constant(0), Expression.Constant(1), comparer, Enumerable.Empty<SwitchCase>()));
        }

        [Fact]
        public void ComparisonRightParameterIncorrect()
        {
            Func<int, string, bool> isLength = (x, y) => (y?.Length).GetValueOrDefault() == x;
            MethodInfo comparer = isLength.GetMethodInfo();
            Assert.Throws<ArgumentException>(null, () => Expression.Switch(Expression.Constant(0), Expression.Empty(), comparer, Expression.SwitchCase(Expression.Empty(), Expression.Constant(0))));
            Assert.Throws<ArgumentException>(null, () => Expression.Switch(Expression.Constant(0), Expression.Empty(), comparer, Enumerable.Repeat(Expression.SwitchCase(Expression.Empty(), Expression.Constant(0)), 1)));
            Assert.Throws<ArgumentException>("cases", () => Expression.Switch(typeof(int), Expression.Constant(0), Expression.Constant(1), comparer, Expression.SwitchCase(Expression.Empty(), Expression.Constant(0))));
            Assert.Throws<ArgumentException>("cases", () => Expression.Switch(typeof(int), Expression.Constant(0), Expression.Constant(1), comparer, Enumerable.Repeat(Expression.SwitchCase(Expression.Empty(), Expression.Constant(0)), 1)));
        }

        static bool WithinTen(int x, int y) => Math.Abs(x - y) < 10;

        [Theory, ClassData(typeof(CompilationTypes))]
        public void LiftedCall(bool useInterpreter)
        {
            Func<int> f = Expression.Lambda<Func<int>>(
                Expression.Switch(
                    Expression.Constant(30, typeof(int?)),
                    Expression.Constant(0),
                    typeof(SwitchTests).GetMethod(nameof(WithinTen), BindingFlags.Static | BindingFlags.NonPublic),
                    Expression.SwitchCase(Expression.Constant(1), Expression.Constant(2, typeof(int?))),
                    Expression.SwitchCase(Expression.Constant(2), Expression.Constant(9, typeof(int?)), Expression.Constant(28, typeof(int?))),
                    Expression.SwitchCase(Expression.Constant(3), Expression.Constant(49, typeof(int?)))
                    )
                ).Compile(useInterpreter);

            Assert.Equal(2, f());
        }

        [Fact]
        public void LeftLiftedCall()
        {
            Assert.Throws<ArgumentException>(null, () =>
                Expression.Switch(
                    Expression.Constant(30, typeof(int?)),
                    Expression.Constant(0),
                    typeof(SwitchTests).GetMethod(nameof(WithinTen), BindingFlags.Static | BindingFlags.NonPublic),
                    Expression.SwitchCase(Expression.Constant(1), Expression.Constant(2))
                    )
                );
        }

        [Fact]
        public void CaseTypeMisMatch()
        {
            Assert.Throws<ArgumentException>("cases", () =>
                Expression.Switch(
                    Expression.Constant(30),
                    Expression.SwitchCase(Expression.Constant(1), Expression.Constant(0)),
                    Expression.SwitchCase(Expression.Constant(2), Expression.Constant("Foo"))
                    )
                );
            Assert.Throws<ArgumentException>("cases", () =>
                Expression.Switch(
                    Expression.Constant(30),
                    Expression.SwitchCase(Expression.Constant(1), Expression.Constant(0), Expression.Constant("Foo"))
                    )
                );
        }

        static int NonBooleanMethod(int x, int y) => x + y;

        [Fact]
        public void NonBooleanComparer()
        {
            MethodInfo comparer = typeof(SwitchTests).GetMethod(nameof(NonBooleanMethod), BindingFlags.Static | BindingFlags.NonPublic);
            Assert.Throws<ArgumentException>("comparison", () => Expression.Switch(Expression.Constant(0), Expression.Empty(), comparer, Expression.SwitchCase(Expression.Empty(), Expression.Constant(0))));
            Assert.Throws<ArgumentException>("comparison", () => Expression.Switch(Expression.Constant(0), Expression.Empty(), comparer, Enumerable.Repeat(Expression.SwitchCase(Expression.Empty(), Expression.Constant(0)), 1)));
            Assert.Throws<ArgumentException>("cases", () => Expression.Switch(typeof(int), Expression.Constant(0), Expression.Constant(1), comparer, Expression.SwitchCase(Expression.Empty(), Expression.Constant(0))));
            Assert.Throws<ArgumentException>("cases", () => Expression.Switch(typeof(int), Expression.Constant(0), Expression.Constant(1), comparer, Enumerable.Repeat(Expression.SwitchCase(Expression.Empty(), Expression.Constant(0)), 1)));
        }

        [Fact]
        public void MismatchingCasesAndType()
        {
            Assert.Throws<ArgumentException>("cases", () => Expression.Switch(Expression.Constant(2), Expression.SwitchCase(Expression.Constant("Foo"), Expression.Constant(0)), Expression.SwitchCase(Expression.Constant(3), Expression.Constant(9))));
        }

        [Fact]
        public void MismatchingCasesAndExpclitType()
        {
            Assert.Throws<ArgumentException>("cases", () => Expression.Switch(typeof(int), Expression.Constant(0), null, null, Expression.SwitchCase(Expression.Constant("Foo"), Expression.Constant(0))));
        }

        [Fact]
        public void MismatchingDefaultAndExpclitType()
        {
            Assert.Throws<ArgumentException>("defaultBody", () => Expression.Switch(typeof(int), Expression.Constant(0), Expression.Constant("Foo"), null));
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public void MismatchingAllowedIfExplicitlyVoidIntgralValueType(bool useInterpreter)
        {
            Expression.Lambda<Action>(
                Expression.Switch(
                    typeof(void),
                    Expression.Constant(0),
                    Expression.Constant(1),
                    null,
                    Expression.SwitchCase(Expression.Constant("Foo"), Expression.Constant(2)),
                    Expression.SwitchCase(Expression.Constant(DateTime.MinValue), Expression.Constant(3))
                    )
                ).Compile(useInterpreter)();
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public void MismatchingAllowedIfExplicitlyVoidStringValueType(bool useInterpreter)
        {
            Expression.Lambda<Action>(
                Expression.Switch(
                    typeof(void),
                    Expression.Constant("Foo"),
                    Expression.Constant(1),
                    null,
                    Expression.SwitchCase(Expression.Constant("Foo"), Expression.Constant("Bar")),
                    Expression.SwitchCase(Expression.Constant(DateTime.MinValue), Expression.Constant("Foo"))
                    )
                ).Compile(useInterpreter)();
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public void MismatchingAllowedIfExplicitlyVoidDateTimeValueType(bool useInterpreter)
        {
            Expression.Lambda<Action>(
                Expression.Switch(
                    typeof(void),
                    Expression.Constant(DateTime.MinValue),
                    Expression.Constant(1),
                    null,
                    Expression.SwitchCase(Expression.Constant("Foo"), Expression.Constant(DateTime.MinValue)),
                    Expression.SwitchCase(Expression.Constant(DateTime.MinValue), Expression.Constant(DateTime.MaxValue))
                    )
                ).Compile(useInterpreter)();
        }

        [Fact]
        public void SwitchCaseUpdateSameToSame()
        {
            Expression[] tests = {Expression.Constant(0), Expression.Constant(2)};
            SwitchCase sc = Expression.SwitchCase(Expression.Constant(1), tests);
            Assert.Same(sc, sc.Update(tests, sc.Body));
            Assert.Same(sc, sc.Update(sc.TestValues, sc.Body));
        }

        [Fact]
        public void SwitchCaseUpdateNullTestsToSame()
        {
            SwitchCase sc = Expression.SwitchCase(Expression.Constant(0), Expression.Constant(1));
            Assert.Throws<ArgumentException>("testValues", () => sc.Update(null, sc.Body));
            Assert.Throws<ArgumentNullException>("body", () => sc.Update(sc.TestValues, null));
        }

        [Fact]
        public void SwitchCaseDifferentBodyToDifferent()
        {
            SwitchCase sc = Expression.SwitchCase(Expression.Constant(1), Expression.Constant(0), Expression.Constant(2));
            Assert.NotSame(sc, sc.Update(sc.TestValues, Expression.Constant(1)));
        }

        [Fact]
        public void SwitchCaseDifferentTestsToDifferent()
        {
            SwitchCase sc = Expression.SwitchCase(Expression.Constant(1), Expression.Constant(0), Expression.Constant(2));
            Assert.NotSame(sc, sc.Update(new[] { Expression.Constant(0), Expression.Constant(2) }, sc.Body));
        }

        [Fact]
        public void SwitchCaseUpdateDoesntRepeatEnumeration()
        {
            SwitchCase sc = Expression.SwitchCase(Expression.Constant(1), Expression.Constant(0), Expression.Constant(2));
            Assert.NotSame(sc, sc.Update(new RunOnceEnumerable<Expression>(new[] { Expression.Constant(0), Expression.Constant(2) }), sc.Body));
        }

        [Fact]
        public void SwitchUpdateSameToSame()
        {
            SwitchCase[] cases =
            {
                Expression.SwitchCase(Expression.Constant(1), Expression.Constant(1)),
                Expression.SwitchCase(Expression.Constant(2), Expression.Constant(2))
            };

            SwitchExpression sw = Expression.Switch(
                Expression.Constant(0),
                Expression.Constant(0),
                cases
                );
            Assert.Same(sw, sw.Update(sw.SwitchValue, cases.Skip(0), sw.DefaultBody));
            Assert.Same(sw, sw.Update(sw.SwitchValue, cases, sw.DefaultBody));
            Assert.Same(sw, NoOpVisitor.Instance.Visit(sw));
        }

        [Fact]
        public void SwitchUpdateDifferentDefaultToDifferent()
        {
            SwitchExpression sw = Expression.Switch(
                Expression.Constant(0),
                Expression.Constant(0),
                Expression.SwitchCase(Expression.Constant(1), Expression.Constant(1)),
                Expression.SwitchCase(Expression.Constant(2), Expression.Constant(2))
                );
            Assert.NotSame(sw, sw.Update(sw.SwitchValue, sw.Cases, Expression.Constant(0)));
        }

        [Fact]
        public void SwitchUpdateDifferentValueToDifferent()
        {
            SwitchExpression sw = Expression.Switch(
                Expression.Constant(0),
                Expression.Constant(0),
                Expression.SwitchCase(Expression.Constant(1), Expression.Constant(1)),
                Expression.SwitchCase(Expression.Constant(2), Expression.Constant(2))
                );
            Assert.NotSame(sw, sw.Update(Expression.Constant(0), sw.Cases, sw.DefaultBody));
        }

        [Fact]
        public void SwitchUpdateDifferentCasesToDifferent()
        {
            SwitchExpression sw = Expression.Switch(
                Expression.Constant(0),
                Expression.Constant(0),
                Expression.SwitchCase(Expression.Constant(1), Expression.Constant(1)),
                Expression.SwitchCase(Expression.Constant(2), Expression.Constant(2))
                );

            SwitchCase[] newCases = new[]
            {
                Expression.SwitchCase(Expression.Constant(1), Expression.Constant(1)),
                Expression.SwitchCase(Expression.Constant(2), Expression.Constant(2))
            };

            Assert.NotSame(sw, sw.Update(sw.SwitchValue, newCases, sw.DefaultBody));
        }

        [Fact]
        public void SwitchUpdateDoesntRepeatEnumeration()
        {
            SwitchExpression sw = Expression.Switch(
                Expression.Constant(0),
                Expression.Constant(0),
                Expression.SwitchCase(Expression.Constant(1), Expression.Constant(1)),
                Expression.SwitchCase(Expression.Constant(2), Expression.Constant(2))
                );

            IEnumerable<SwitchCase> newCases =
                new RunOnceEnumerable<SwitchCase>(
                    new SwitchCase[]
                    {
                        Expression.SwitchCase(Expression.Constant(1), Expression.Constant(1)),
                        Expression.SwitchCase(Expression.Constant(2), Expression.Constant(2))
                    });

            Assert.NotSame(sw, sw.Update(sw.SwitchValue, newCases, sw.DefaultBody));
        }

        [Fact]
        public void SingleTestCaseToString()
        {
            SwitchCase sc = Expression.SwitchCase(Expression.Constant(1), Expression.Constant(0));
            Assert.Equal("case (0): ...", sc.ToString());
        }

        [Fact]
        public void MultipleTestCaseToString()
        {
            SwitchCase sc = Expression.SwitchCase(Expression.Constant(1), Expression.Constant(0), Expression.Constant("A"));
            Assert.Equal("case (0, \"A\"): ...", sc.ToString());
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public void SwitchOnString(bool useInterpreter)
        {
            var values = new string[] { "foobar", "foo", "bar", "baz", "qux", "quux", "corge", "grault", "garply", "waldo", "fred", "plugh", "xyzzy", "thud" };

            for (var i = 1; i <= values.Length; i++)
            {
                SwitchCase[] cases = values.Take(i).Select((s, j) => Expression.SwitchCase(Expression.Constant(j), Expression.Constant(values[j]))).ToArray();
                ParameterExpression value = Expression.Parameter(typeof(string));
                Expression<Func<string, int>> e = Expression.Lambda<Func<string, int>>(Expression.Switch(value, Expression.Constant(-1), cases), value);
                Func<string, int> f = e.Compile(useInterpreter);

                int k = 0;
                foreach (var str in values.Take(i))
                {
                    Assert.Equal(k, f(str));
                    k++;
                }

                foreach (var str in values.Skip(i).Concat(new[] { default(string), "whatever", "FOO" }))
                {
                    Assert.Equal(-1, f(str));
                    k++;
                }
            }
        }

        [Fact]
        public void ToStringTest()
        {
            SwitchExpression e1 = Expression.Switch(Expression.Parameter(typeof(int), "x"), Expression.SwitchCase(Expression.Empty(), Expression.Constant(1)));
            Assert.Equal("switch (x) { ... }", e1.ToString());

            SwitchCase e2 = Expression.SwitchCase(Expression.Parameter(typeof(int), "x"), Expression.Constant(1));
            Assert.Equal("case (1): ...", e2.ToString());

            SwitchCase e3 = Expression.SwitchCase(Expression.Parameter(typeof(int), "x"), Expression.Constant(1), Expression.Constant(2));
            Assert.Equal("case (1, 2): ...", e3.ToString());
        }
    }
}
