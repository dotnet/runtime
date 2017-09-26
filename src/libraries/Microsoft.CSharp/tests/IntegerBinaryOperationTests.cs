// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using Xunit;

namespace Microsoft.CSharp.RuntimeBinder.Tests
{
    public class IntegerBinaryOperationTests
    {
        private static CallSite<Func<CallSite, object, object, object>> GetBinaryOperationCallSite(ExpressionType operation, bool checkedContext, bool constantLeftArgument, bool constantRightArgument)
        {
            CSharpArgumentInfo x = CSharpArgumentInfo.Create(constantLeftArgument ? CSharpArgumentInfoFlags.Constant : CSharpArgumentInfoFlags.None, null);
            CSharpArgumentInfo y = CSharpArgumentInfo.Create(constantRightArgument ? CSharpArgumentInfoFlags.Constant : CSharpArgumentInfoFlags.None, null);
            CallSiteBinder binder =
                Binder.BinaryOperation(
                    checkedContext ? CSharpBinderFlags.CheckedContext : CSharpBinderFlags.None, operation,
                    typeof(IntegerBinaryOperationTests), new[] {x, y});
            return CallSite<Func<CallSite, object, object, object>>.Create(binder);
        }

        private static readonly ExpressionType[] DividingOperations = {ExpressionType.Divide, ExpressionType.Modulo};

        public static IEnumerable<object[]> DivisionExtremes
        {
            get
            {
                yield return new object[] { int.MinValue, -1 };
                yield return new object[] { long.MinValue, -1L };
            }
        }

        public static IEnumerable<object[]> SignedMinDivisionByMinusOneData
        {
            get
            {
                foreach (ExpressionType op in DividingOperations)
                {
                    yield return new object[] {op, int.MinValue, -1};
                    yield return new object[] {op, long.MinValue, -1L};
                }
            }
        }

        private static readonly int[] TestInt32Values = {0, 1, -1, 3, int.MinValue, int.MaxValue};

        public static IEnumerable<object[]> Int32DivisionByZero
            => from x in TestInt32Values from o in DividingOperations select new object[] {o, x};

        public static IEnumerable<object[]> Int32TestAdditions
            => from x in TestInt32Values
            from y in TestInt32Values
            select new object[] {x, y, ExpressionType.Add, unchecked(x + y), x == 0 || unchecked(x < 0 ? x + y < y : x + y > y)};

        public static IEnumerable<object[]> Int32TestAnds
            => from x in TestInt32Values
            from y in TestInt32Values
            select new object[] {x, y, ExpressionType.And, x & y, true};

        public static IEnumerable<object[]> Int32TestDivisions
            => from x in TestInt32Values
            from y in TestInt32Values
            where y != 0 && (x != int.MinValue || y != -1)
            select new object[] {x, y, ExpressionType.Divide, x / y, true};

        public static IEnumerable<object[]> Int32TestEquals
            => from x in TestInt32Values
            from y in TestInt32Values
            select new object[] {x, y, ExpressionType.Equal, x == y, true};

        public static IEnumerable<object[]> Int32TestExclusiveOrs
            => from x in TestInt32Values
            from y in TestInt32Values
            select new object[] {x, y, ExpressionType.ExclusiveOr, x ^ y, true};

        public static IEnumerable<object[]> Int32TestGreaterThans
            => from x in TestInt32Values
            from y in TestInt32Values
            select new object[] {x, y, ExpressionType.GreaterThan, x > y, true};

        public static IEnumerable<object[]> Int32TestGreaterThanOrEquals
            => from x in TestInt32Values
            from y in TestInt32Values
            select new object[] {x, y, ExpressionType.GreaterThanOrEqual, x >= y, true};

        public static IEnumerable<object[]> Int32TestLeftShifts
            => from x in TestInt32Values
            from y in TestInt32Values
            select new object[] {x, y, ExpressionType.LeftShift, x << y, true};

        public static IEnumerable<object[]> Int32TestLessThans
            => from x in TestInt32Values
            from y in TestInt32Values
            select new object[] {x, y, ExpressionType.LessThan, x < y, true};

        public static IEnumerable<object[]> Int32TestLessThanOrEquals
            => from x in TestInt32Values
            from y in TestInt32Values
            select new object[] {x, y, ExpressionType.LessThanOrEqual, x <= y, true};

        public static IEnumerable<object[]> Int32TestModulos
            => from x in TestInt32Values
            from y in TestInt32Values
            where y != 0 && (x != int.MinValue || y != -1)
            select new object[] {x, y, ExpressionType.Modulo, x % y, true};

        public static IEnumerable<object[]> Int32TestMultiplications
            => from x in TestInt32Values
            from y in TestInt32Values
            select
                new object[]
                {
                    x, y, ExpressionType.Multiply, unchecked(x * y),
                    y == 0 || !(x == -1 && y == int.MinValue) && !(x == int.MinValue && y == -1) && unchecked(x * y / y) == x
                };

        public static IEnumerable<object[]> Int32TestNotEquals
            => from x in TestInt32Values
            from y in TestInt32Values
            select new object[] {x, y, ExpressionType.NotEqual, x != y, true};


        public static IEnumerable<object[]> Int32TestRightShifts
            => from x in TestInt32Values
            from y in TestInt32Values
            select new object[] {x, y, ExpressionType.RightShift, x >> y, true};


        public static IEnumerable<object[]> Int32TestSubtractions
            => from x in TestInt32Values
            from y in TestInt32Values
            select
                new object[]
                {
                    x, y, ExpressionType.Subtract, unchecked(x - y),
                    (x == 0 && y != int.MinValue) || y == 0 || unchecked(y > 0 ? x - y < x : x - y > x)
                };

        private static readonly uint[] TestUInt32Values = {0, 1, 3, int.MaxValue, uint.MaxValue};

        public static IEnumerable<object[]> UInt32DivisionByZero
            => from x in TestUInt32Values from o in DividingOperations select new object[] {o, x};

        public static IEnumerable<object[]> UInt32TestAdditions
            => from x in TestUInt32Values
            from y in TestUInt32Values
            select new object[] {x, y, ExpressionType.Add, unchecked(x + y), x == 0 || unchecked(x + y) > y};

        public static IEnumerable<object[]> UInt32TestAnds
            => from x in TestUInt32Values
            from y in TestUInt32Values
            select new object[] {x, y, ExpressionType.And, x & y, true};

        public static IEnumerable<object[]> UInt32TestDivisions
            => from x in TestUInt32Values
            from y in TestUInt32Values
            where y != 0
            select new object[] {x, y, ExpressionType.Divide, x / y, true};

        public static IEnumerable<object[]> UInt32TestEquals
            => from x in TestUInt32Values
            from y in TestUInt32Values
            select new object[] {x, y, ExpressionType.Equal, x == y, true};

        public static IEnumerable<object[]> UInt32TestExclusiveOrs
            => from x in TestUInt32Values
            from y in TestUInt32Values
            select new object[] {x, y, ExpressionType.ExclusiveOr, x ^ y, true};

        public static IEnumerable<object[]> UInt32TestGreaterThans
            => from x in TestUInt32Values
            from y in TestUInt32Values
            select new object[] {x, y, ExpressionType.GreaterThan, x > y, true};

        public static IEnumerable<object[]> UInt32TestGreaterThanOrEquals
            => from x in TestUInt32Values
            from y in TestUInt32Values
            select new object[] {x, y, ExpressionType.GreaterThanOrEqual, x >= y, true};

        public static IEnumerable<object[]> UInt32TestLessThans
            => from x in TestUInt32Values
            from y in TestUInt32Values
            select new object[] {x, y, ExpressionType.LessThan, x < y, true};

        public static IEnumerable<object[]> UInt32TestLessThanOrEquals
            => from x in TestUInt32Values
            from y in TestUInt32Values
            select new object[] {x, y, ExpressionType.LessThanOrEqual, x <= y, true};

        public static IEnumerable<object[]> UInt32TestModulos
            => from x in TestUInt32Values
            from y in TestUInt32Values
            where y != 0
            select new object[] {x, y, ExpressionType.Modulo, x % y, true};

        public static IEnumerable<object[]> UInt32TestMultiplications
            => from c in new[] {true, false}
            from x in TestUInt32Values
            from y in TestUInt32Values
            select new object[] {x, y, ExpressionType.Multiply, unchecked(x * y), y == 0 || unchecked(x * y / y) == x};

        public static IEnumerable<object[]> UInt32TestNotEquals
            => from x in TestUInt32Values
            from y in TestUInt32Values
            select new object[] {x, y, ExpressionType.NotEqual, x != y, true};

        public static IEnumerable<object[]> UInt32TestSubtractions
            => from x in TestUInt32Values
            from y in TestUInt32Values
            select new object[] {x, y, ExpressionType.Subtract, unchecked(x - y), y == 0 || unchecked(y > 0 ? x - y < x : x - y > x)};


        private static readonly long[] TestInt64Values = {0, 1, -1, 3, long.MinValue, long.MaxValue};

        public static IEnumerable<object[]> Int64DivisionByZero
            => from x in TestInt64Values from o in DividingOperations select new object[] {o, x};


        public static IEnumerable<object[]> Int64TestAdditions
            => from x in TestInt64Values
            from y in TestInt64Values
            select new object[] {x, y, ExpressionType.Add, unchecked(x + y), x == 0 || unchecked(x < 0 ? x + y < y : x + y > y)};

        public static IEnumerable<object[]> Int64TestAnds
            => from x in TestInt64Values
            from y in TestInt64Values
            select new object[] {x, y, ExpressionType.And, x & y, true};

        public static IEnumerable<object[]> Int64TestDivisions
            => from x in TestInt64Values
            from y in TestInt64Values
            where y != 0 && (x != long.MinValue || y != -1)
            select new object[] {x, y, ExpressionType.Divide, x / y, true};

        public static IEnumerable<object[]> Int64TestEquals
            => from x in TestInt64Values
            from y in TestInt64Values
            select new object[] {x, y, ExpressionType.Equal, x == y, true};

        public static IEnumerable<object[]> Int64TestExclusiveOrs
            => from x in TestInt64Values
            from y in TestInt64Values
            select new object[] {x, y, ExpressionType.ExclusiveOr, x ^ y, true};

        public static IEnumerable<object[]> Int64TestGreaterThans
            => from x in TestInt64Values
            from y in TestInt64Values
            select new object[] {x, y, ExpressionType.GreaterThan, x > y, true};

        public static IEnumerable<object[]> Int64TestGreaterThanOrEquals
            => from x in TestInt64Values
            from y in TestInt64Values
            select new object[] {x, y, ExpressionType.GreaterThanOrEqual, x >= y, true};

        public static IEnumerable<object[]> Int64TestLessThans
            => from x in TestInt64Values
            from y in TestInt64Values
            select new object[] {x, y, ExpressionType.LessThan, x < y, true};

        public static IEnumerable<object[]> Int64TestLessThanOrEquals
            => from x in TestInt64Values
            from y in TestInt64Values
            select new object[] {x, y, ExpressionType.LessThanOrEqual, x <= y, true};

        public static IEnumerable<object[]> Int64TestModulos
            => from x in TestInt64Values
            from y in TestInt64Values
            where y != 0 && (x != long.MinValue || y != -1)
            select new object[] {x, y, ExpressionType.Modulo, x % y, true};

        public static IEnumerable<object[]> Int64TestMultiplications
            => from x in TestInt64Values
            from y in TestInt64Values
            select
                new object[]
                {
                    x, y, ExpressionType.Multiply, unchecked(x * y),
                    y == 0 || !(x == -1 && y == long.MinValue) && !(x == long.MinValue && y == -1) && unchecked(x * y / y) == x
                };

        public static IEnumerable<object[]> Int64TestNotEquals
            => from x in TestInt64Values
            from y in TestInt64Values
            select new object[] {x, y, ExpressionType.NotEqual, x != y, true};

        public static IEnumerable<object[]> Int64TestSubtractions
            => from x in TestInt64Values
            from y in TestInt64Values
            select
                new object[]
                {
                    x, y, ExpressionType.Subtract, unchecked(x - y),
                    (x == 0 && y != long.MinValue) || y == 0 || unchecked(y > 0 ? x - y < x : x - y > x)
                };

        private static readonly ulong[] TestUInt64Values = {0, 1, 3, long.MaxValue, ulong.MaxValue};

        public static IEnumerable<object[]> UInt64DivisionByZero
            => from x in TestUInt64Values from o in DividingOperations select new object[] {o, x};


        public static IEnumerable<object[]> UInt64TestAdditions
            => from x in TestUInt64Values
            from y in TestUInt64Values
            select new object[] {x, y, ExpressionType.Add, unchecked(x + y), x == 0 || unchecked(x + y) > y};

        public static IEnumerable<object[]> UInt64TestAnds
            => from x in TestUInt64Values
            from y in TestUInt64Values
            select new object[] {x, y, ExpressionType.And, x & y, true};

        public static IEnumerable<object[]> UInt64TestDivisions
            => from x in TestUInt64Values
            from y in TestUInt64Values
            where y != 0
            select new object[] {x, y, ExpressionType.Divide, x / y, true};

        public static IEnumerable<object[]> UInt64TestEquals
            => from x in TestUInt64Values
            from y in TestUInt64Values
            select new object[] {x, y, ExpressionType.Equal, x == y, true};

        public static IEnumerable<object[]> UInt64TestExclusiveOrs
            => from x in TestUInt64Values
            from y in TestUInt64Values
            select new object[] {x, y, ExpressionType.ExclusiveOr, x ^ y, true};

        public static IEnumerable<object[]> UInt64TestGreaterThans
            => from x in TestUInt64Values
            from y in TestUInt64Values
            select new object[] {x, y, ExpressionType.GreaterThan, x > y, true};

        public static IEnumerable<object[]> UInt64TestGreaterThanOrEquals
            => from x in TestUInt64Values
            from y in TestUInt64Values
            select new object[] {x, y, ExpressionType.GreaterThanOrEqual, x >= y, true};

        public static IEnumerable<object[]> UInt64TestLessThans
            => from x in TestUInt64Values
            from y in TestUInt64Values
            select new object[] {x, y, ExpressionType.LessThan, x < y, true};

        public static IEnumerable<object[]> UInt64TestLessThanOrEquals
            => from x in TestUInt64Values
            from y in TestUInt64Values
            select new object[] {x, y, ExpressionType.LessThanOrEqual, x <= y, true};

        public static IEnumerable<object[]> UInt64TestModulos
            => from x in TestUInt64Values
            from y in TestUInt64Values
            where y != 0
            select new object[] {x, y, ExpressionType.Modulo, x % y, true};

        public static IEnumerable<object[]> UInt64TestMultiplications
            => from c in new[] {true, false}
            from x in TestUInt64Values
            from y in TestUInt64Values
            select new object[] {x, y, ExpressionType.Multiply, unchecked(x * y), y == 0 || unchecked(x * y / y) == x};

        public static IEnumerable<object[]> UInt64TestNotEquals
            => from x in TestUInt64Values
            from y in TestUInt64Values
            select new object[] {x, y, ExpressionType.NotEqual, x != y, true};

        public static IEnumerable<object[]> UInt64TestSubtractions
            => from x in TestUInt64Values
            from y in TestUInt64Values
            select new object[] {x, y, ExpressionType.Subtract, unchecked(x - y), y == 0 || unchecked(y > 0 ? x - y < x : x - y > x)};

        [Theory]
        [MemberData(nameof(Int32TestAdditions))]
        [MemberData(nameof(Int32TestAnds))]
        [MemberData(nameof(Int32TestDivisions))]
        [MemberData(nameof(Int32TestEquals))]
        [MemberData(nameof(Int32TestExclusiveOrs))]
        [MemberData(nameof(Int32TestGreaterThans))]
        [MemberData(nameof(Int32TestGreaterThanOrEquals))]
        [MemberData(nameof(Int32TestLeftShifts))]
        [MemberData(nameof(Int32TestLessThans))]
        [MemberData(nameof(Int32TestLessThanOrEquals))]
        [MemberData(nameof(Int32TestModulos))]
        [MemberData(nameof(Int32TestMultiplications))]
        [MemberData(nameof(Int32TestNotEquals))]
        [MemberData(nameof(Int32TestRightShifts))]
        [MemberData(nameof(Int32TestSubtractions))]
        [MemberData(nameof(UInt32TestAdditions))]
        [MemberData(nameof(UInt32TestAnds))]
        [MemberData(nameof(UInt32TestDivisions))]
        [MemberData(nameof(UInt32TestEquals))]
        [MemberData(nameof(UInt32TestExclusiveOrs))]
        [MemberData(nameof(UInt32TestGreaterThans))]
        [MemberData(nameof(UInt32TestGreaterThanOrEquals))]
        [MemberData(nameof(UInt32TestLessThans))]
        [MemberData(nameof(UInt32TestLessThanOrEquals))]
        [MemberData(nameof(UInt32TestModulos))]
        [MemberData(nameof(UInt32TestMultiplications))]
        [MemberData(nameof(UInt32TestNotEquals))]
        [MemberData(nameof(UInt32TestSubtractions))]
        [MemberData(nameof(Int64TestAdditions))]
        [MemberData(nameof(Int64TestAnds))]
        [MemberData(nameof(Int64TestDivisions))]
        [MemberData(nameof(Int64TestEquals))]
        [MemberData(nameof(Int64TestExclusiveOrs))]
        [MemberData(nameof(Int64TestGreaterThans))]
        [MemberData(nameof(Int64TestGreaterThanOrEquals))]
        [MemberData(nameof(Int64TestLessThans))]
        [MemberData(nameof(Int64TestLessThanOrEquals))]
        [MemberData(nameof(Int64TestModulos))]
        [MemberData(nameof(Int64TestMultiplications))]
        [MemberData(nameof(Int64TestNotEquals))]
        [MemberData(nameof(Int64TestSubtractions))]
        [MemberData(nameof(UInt64TestAdditions))]
        [MemberData(nameof(UInt64TestAnds))]
        [MemberData(nameof(UInt64TestDivisions))]
        [MemberData(nameof(UInt64TestEquals))]
        [MemberData(nameof(UInt64TestExclusiveOrs))]
        [MemberData(nameof(UInt64TestGreaterThans))]
        [MemberData(nameof(UInt64TestGreaterThanOrEquals))]
        [MemberData(nameof(UInt64TestLessThans))]
        [MemberData(nameof(UInt64TestLessThanOrEquals))]
        [MemberData(nameof(UInt64TestModulos))]
        [MemberData(nameof(UInt64TestMultiplications))]
        [MemberData(nameof(UInt64TestNotEquals))]
        [MemberData(nameof(UInt64TestSubtractions))]
        public void RuntimeExpression(object x, object y, ExpressionType type, object result, bool shouldSucceedChecked)
        {
            var callsite = GetBinaryOperationCallSite(type, false, false, false);
            Assert.Equal(result, callsite.Target(callsite, x, y));
            callsite = GetBinaryOperationCallSite(type, true, false, false);
            if (shouldSucceedChecked)
            {
                Assert.Equal(result, callsite.Target(callsite, x, y));
            }
            else
            {
                Assert.Throws<OverflowException>(() => callsite.Target(callsite, x, y));
            }
        }

        [Theory]
        [MemberData(nameof(Int32TestAdditions))]
        [MemberData(nameof(Int32TestAnds))]
        [MemberData(nameof(Int32TestDivisions))]
        [MemberData(nameof(Int32TestEquals))]
        [MemberData(nameof(Int32TestExclusiveOrs))]
        [MemberData(nameof(Int32TestGreaterThans))]
        [MemberData(nameof(Int32TestGreaterThanOrEquals))]
        [MemberData(nameof(Int32TestLeftShifts))]
        [MemberData(nameof(Int32TestLessThans))]
        [MemberData(nameof(Int32TestLessThanOrEquals))]
        [MemberData(nameof(Int32TestModulos))]
        [MemberData(nameof(Int32TestMultiplications))]
        [MemberData(nameof(Int32TestNotEquals))]
        [MemberData(nameof(Int32TestRightShifts))]
        [MemberData(nameof(Int32TestSubtractions))]
        [MemberData(nameof(UInt32TestAdditions))]
        [MemberData(nameof(UInt32TestAnds))]
        [MemberData(nameof(UInt32TestDivisions))]
        [MemberData(nameof(UInt32TestEquals))]
        [MemberData(nameof(UInt32TestExclusiveOrs))]
        [MemberData(nameof(UInt32TestGreaterThans))]
        [MemberData(nameof(UInt32TestGreaterThanOrEquals))]
        [MemberData(nameof(UInt32TestLessThans))]
        [MemberData(nameof(UInt32TestLessThanOrEquals))]
        [MemberData(nameof(UInt32TestModulos))]
        [MemberData(nameof(UInt32TestMultiplications))]
        [MemberData(nameof(UInt32TestNotEquals))]
        [MemberData(nameof(UInt32TestSubtractions))]
        [MemberData(nameof(Int64TestAdditions))]
        [MemberData(nameof(Int64TestAnds))]
        [MemberData(nameof(Int64TestDivisions))]
        [MemberData(nameof(Int64TestEquals))]
        [MemberData(nameof(Int64TestExclusiveOrs))]
        [MemberData(nameof(Int64TestGreaterThans))]
        [MemberData(nameof(Int64TestGreaterThanOrEquals))]
        [MemberData(nameof(Int64TestLessThans))]
        [MemberData(nameof(Int64TestLessThanOrEquals))]
        [MemberData(nameof(Int64TestModulos))]
        [MemberData(nameof(Int64TestMultiplications))]
        [MemberData(nameof(Int64TestNotEquals))]
        [MemberData(nameof(Int64TestSubtractions))]
        [MemberData(nameof(UInt64TestAdditions))]
        [MemberData(nameof(UInt64TestAnds))]
        [MemberData(nameof(UInt64TestDivisions))]
        [MemberData(nameof(UInt64TestEquals))]
        [MemberData(nameof(UInt64TestExclusiveOrs))]
        [MemberData(nameof(UInt64TestGreaterThans))]
        [MemberData(nameof(UInt64TestGreaterThanOrEquals))]
        [MemberData(nameof(UInt64TestLessThans))]
        [MemberData(nameof(UInt64TestLessThanOrEquals))]
        [MemberData(nameof(UInt64TestModulos))]
        [MemberData(nameof(UInt64TestMultiplications))]
        [MemberData(nameof(UInt64TestNotEquals))]
        [MemberData(nameof(UInt64TestSubtractions))]
        public void ConstantExpressions(object x, object y, ExpressionType type, object result, bool shouldSucceedChecked)
        {
            var callsite = GetBinaryOperationCallSite(type, false, true, true);
            Assert.Equal(result, callsite.Target(callsite, x, y));
            callsite = GetBinaryOperationCallSite(type, true, true, true);
            if (shouldSucceedChecked)
            {
                Assert.Equal(result, callsite.Target(callsite, x, y));
            }
            else
            {
                Assert.Throws<OverflowException>(() => callsite.Target(callsite, x, y));
            }
        }

        [Theory]
        [MemberData(nameof(Int32DivisionByZero))]
        [MemberData(nameof(UInt32DivisionByZero))]
        [MemberData(nameof(Int64DivisionByZero))]
        [MemberData(nameof(UInt64DivisionByZero))]
        public void RuntimeDivideByZero(ExpressionType type, object x)
        {
            var callsite = GetBinaryOperationCallSite(type, false, false, false);
            object zero = Convert.ChangeType(0, x.GetType());
            Assert.Throws<DivideByZeroException>(() => callsite.Target(callsite, x, zero));
            callsite = GetBinaryOperationCallSite(type, true, false, false);
            Assert.Throws<DivideByZeroException>(() => callsite.Target(callsite, x, zero));
        }

        [Theory]
        [MemberData(nameof(Int32DivisionByZero))]
        [MemberData(nameof(UInt32DivisionByZero))]
        [MemberData(nameof(Int64DivisionByZero))]
        [MemberData(nameof(UInt64DivisionByZero))]
        public void ConstantDivideByZero(ExpressionType type, object x)
        {
            var callsite = GetBinaryOperationCallSite(type, false, true, true);
            object zero = Convert.ChangeType(0, x.GetType());
            Assert.Throws<DivideByZeroException>(() => callsite.Target(callsite, x, zero));
            callsite = GetBinaryOperationCallSite(type, true, true, true);
            Assert.Throws<DivideByZeroException>(() => callsite.Target(callsite, x, zero));
        }

        [Theory]
        [MemberData(nameof(Int32DivisionByZero))]
        [MemberData(nameof(UInt32DivisionByZero))]
        [MemberData(nameof(Int64DivisionByZero))]
        [MemberData(nameof(UInt64DivisionByZero))]
        public void DivideByConstantZero(ExpressionType type, object x)
        {
            var callsite = GetBinaryOperationCallSite(type, false, false, true);
            object zero = Convert.ChangeType(0, x.GetType());
            Assert.Throws<DivideByZeroException>(() => callsite.Target(callsite, x, zero));
            callsite = GetBinaryOperationCallSite(type, true, false, true);
            Assert.Throws<DivideByZeroException>(() => callsite.Target(callsite, x, zero));
        }

        [Fact]
        public void IntegerDivideByLiteralZero()
        {
            dynamic d = 3;
            Assert.Throws<DivideByZeroException>(() => d / 0);
        }

        [Fact]
        public void IntegeryDivideByConstZero()
        {
            dynamic d = 3;
            const int zero = 0;
            Assert.Throws<DivideByZeroException>(() => d / zero);
        }

        [Fact]
        public void IntegeryDivideByLocalZero()
        {
            dynamic d = 3;
            int zero = 0;
            Assert.Throws<DivideByZeroException>(() => d / zero);
        }

        [Theory, MemberData(nameof(SignedMinDivisionByMinusOneData))]
        public void RuntimeDivideSignedMinimumByMinusOne(ExpressionType type, object dividend, object divisor)
        {
            var callsite = GetBinaryOperationCallSite(type, false, false, false);
            Assert.Throws<OverflowException>(() => callsite.Target(callsite, dividend, divisor));
            callsite = GetBinaryOperationCallSite(type, true, false, false);
            Assert.Throws<OverflowException>(() => callsite.Target(callsite, dividend, divisor));
        }

        [Theory, MemberData(nameof(DivisionExtremes))]
        public void ConstantDivideSignedMinimumByMinusOne(object dividend, object divisor)
        {
            var callsite = GetBinaryOperationCallSite(ExpressionType.Divide, false, true, true);
            Assert.Throws<OverflowException>(() => callsite.Target(callsite, dividend, divisor));
            callsite = GetBinaryOperationCallSite(ExpressionType.Divide, true, true, true);
            Assert.Throws<OverflowException>(() => callsite.Target(callsite, dividend, divisor));
        }

        [Theory, MemberData(nameof(DivisionExtremes))]
        public void ConstantModuloSignedMinimumByMinusOne(object dividend, object divisor)
        {
            var callsite = GetBinaryOperationCallSite(ExpressionType.Modulo, false, true, true);
            object zero = Convert.ChangeType(0, dividend.GetType());
            Assert.Throws<OverflowException>(() => callsite.Target(callsite, dividend, divisor));
            callsite = GetBinaryOperationCallSite(ExpressionType.Modulo, true, true, true);
            Assert.Throws<OverflowException>(() => callsite.Target(callsite, dividend, divisor));
        }

        [Fact]
        public void NullDMO()
        {
            BinaryOperationBinder binder = Binder.BinaryOperation(
                CSharpBinderFlags.None,
                ExpressionType.Add,
                GetType(),
                new[] { CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null), CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null) }
            ) as BinaryOperationBinder;
            DynamicMetaObject dmo2 = new DynamicMetaObject(Expression.Parameter(typeof(object)), BindingRestrictions.Empty, 2);
            DynamicMetaObject dmoNoVal = new DynamicMetaObject(Expression.Parameter(typeof(object)), BindingRestrictions.Empty);
            AssertExtensions.Throws<ArgumentNullException>("target", () => binder.FallbackBinaryOperation(null, null));
            AssertExtensions.Throws<ArgumentNullException>("arg", () => binder.FallbackBinaryOperation(dmo2, null));
            AssertExtensions.Throws<ArgumentException>("target", () => binder.FallbackBinaryOperation(dmoNoVal, null));
            AssertExtensions.Throws<ArgumentException>("arg", () => binder.FallbackBinaryOperation(dmo2, dmoNoVal));
        }
    }
}
