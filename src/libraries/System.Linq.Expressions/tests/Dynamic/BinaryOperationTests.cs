// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Linq.Expressions.Tests;
using Microsoft.CSharp.RuntimeBinder;
using Xunit;

namespace System.Dynamic.Tests
{
    public class BinaryOperationTests
    {
        private class MinimumOverrideBinaryOperationBinder : BinaryOperationBinder
        {
            public MinimumOverrideBinaryOperationBinder(ExpressionType operation) : base(operation)
            {
            }

            public override DynamicMetaObject FallbackBinaryOperation(
                DynamicMetaObject target, DynamicMetaObject arg, DynamicMetaObject errorSuggestion)
            {
                throw new NotSupportedException();
            }
        }

        private static readonly int[] SomeInt32 = {0, 1, 2, -1, int.MinValue, int.MaxValue, int.MaxValue - 1};

        private static IEnumerable<object[]> CrossJoinInt32()
            => from i in SomeInt32 from j in SomeInt32 select new object[] {i, j};

        private static readonly double[] SomeDouble = {0.0, 1.0, 2.0, -1.0, double.PositiveInfinity, double.NaN};

        private static IEnumerable<object[]> CrossJoinDouble()
            => from i in SomeDouble from j in SomeDouble select new object[] {i, j};

        private static IEnumerable<object[]> BinaryExpressionTypes()
        {
            yield return new object[] {ExpressionType.Add};
            yield return new object[] {ExpressionType.And};
            yield return new object[] {ExpressionType.Divide};
            yield return new object[] {ExpressionType.Equal};
            yield return new object[] {ExpressionType.ExclusiveOr};
            yield return new object[] {ExpressionType.GreaterThan};
            yield return new object[] {ExpressionType.GreaterThanOrEqual};
            yield return new object[] {ExpressionType.LeftShift};
            yield return new object[] {ExpressionType.LessThan};
            yield return new object[] {ExpressionType.LessThanOrEqual};
            yield return new object[] {ExpressionType.Modulo};
            yield return new object[] {ExpressionType.Multiply};
            yield return new object[] {ExpressionType.NotEqual};
            yield return new object[] {ExpressionType.Or};
            yield return new object[] {ExpressionType.Power};
            yield return new object[] {ExpressionType.RightShift};
            yield return new object[] {ExpressionType.Subtract};
            yield return new object[] {ExpressionType.Extension};
            yield return new object[] {ExpressionType.AddAssign};
            yield return new object[] {ExpressionType.AndAssign};
            yield return new object[] {ExpressionType.DivideAssign};
            yield return new object[] {ExpressionType.ExclusiveOrAssign};
            yield return new object[] {ExpressionType.LeftShiftAssign};
            yield return new object[] {ExpressionType.ModuloAssign};
            yield return new object[] {ExpressionType.MultiplyAssign};
            yield return new object[] {ExpressionType.OrAssign};
            yield return new object[] {ExpressionType.PowerAssign};
            yield return new object[] {ExpressionType.RightShiftAssign};
            yield return new object[] {ExpressionType.SubtractAssign};
        }

        private static IEnumerable<object[]> NonBinaryExpressionTypes()
        {
            yield return new object[] {ExpressionType.AddChecked};
            yield return new object[] {ExpressionType.AndAlso};
            yield return new object[] {ExpressionType.ArrayLength};
            yield return new object[] {ExpressionType.ArrayIndex};
            yield return new object[] {ExpressionType.Call};
            yield return new object[] {ExpressionType.Coalesce};
            yield return new object[] {ExpressionType.Conditional};
            yield return new object[] {ExpressionType.Constant};
            yield return new object[] {ExpressionType.Convert};
            yield return new object[] {ExpressionType.ConvertChecked};
            yield return new object[] {ExpressionType.Invoke};
            yield return new object[] {ExpressionType.Lambda};
            yield return new object[] {ExpressionType.ListInit};
            yield return new object[] {ExpressionType.MemberAccess};
            yield return new object[] {ExpressionType.MemberInit};
            yield return new object[] {ExpressionType.MultiplyChecked};
            yield return new object[] {ExpressionType.Negate};
            yield return new object[] {ExpressionType.UnaryPlus};
            yield return new object[] {ExpressionType.NegateChecked};
            yield return new object[] {ExpressionType.New};
            yield return new object[] {ExpressionType.NewArrayInit};
            yield return new object[] {ExpressionType.NewArrayBounds};
            yield return new object[] {ExpressionType.Not};
            yield return new object[] {ExpressionType.OrElse};
            yield return new object[] {ExpressionType.Parameter};
            yield return new object[] {ExpressionType.Quote};
            yield return new object[] {ExpressionType.SubtractChecked};
            yield return new object[] {ExpressionType.TypeAs};
            yield return new object[] {ExpressionType.TypeIs};
            yield return new object[] {ExpressionType.Assign};
            yield return new object[] {ExpressionType.Block};
            yield return new object[] {ExpressionType.DebugInfo};
            yield return new object[] {ExpressionType.Decrement};
            yield return new object[] {ExpressionType.Dynamic};
            yield return new object[] {ExpressionType.Default};
            yield return new object[] {ExpressionType.Goto};
            yield return new object[] {ExpressionType.Increment};
            yield return new object[] {ExpressionType.Index};
            yield return new object[] {ExpressionType.Label};
            yield return new object[] {ExpressionType.RuntimeVariables};
            yield return new object[] {ExpressionType.Loop};
            yield return new object[] {ExpressionType.Switch};
            yield return new object[] {ExpressionType.Throw};
            yield return new object[] {ExpressionType.Try};
            yield return new object[] {ExpressionType.Unbox};
            yield return new object[] {ExpressionType.AddAssignChecked};
            yield return new object[] {ExpressionType.MultiplyAssignChecked};
            yield return new object[] {ExpressionType.SubtractAssignChecked};
            yield return new object[] {ExpressionType.PreIncrementAssign};
            yield return new object[] {ExpressionType.PreDecrementAssign};
            yield return new object[] {ExpressionType.PostIncrementAssign};
            yield return new object[] {ExpressionType.PostDecrementAssign};
            yield return new object[] {ExpressionType.TypeEqual};
            yield return new object[] {ExpressionType.OnesComplement};
            yield return new object[] {ExpressionType.IsTrue};
            yield return new object[] {ExpressionType.IsFalse};
        }

        [Theory, MemberData(nameof(CrossJoinInt32))]
        public void AddInt32(int x, int y)
        {
            dynamic dX = x;
            dynamic dY = y;
            Assert.Equal(unchecked(x + y), unchecked(dX + dY));
        }

        [Theory, MemberData(nameof(CrossJoinInt32))]
        public void AddOvfInt32(int x, int y)
        {
            dynamic dX = x;
            dynamic dY = y;
            int result;
            try
            {
                result = checked(x + y);
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => checked(dX + dY));
                return;
            }

            Assert.Equal(result, checked(dX + dY));
        }

        [Theory, MemberData(nameof(CrossJoinInt32))]
        public void AndInt32(int x, int y)
        {
            dynamic dX = x;
            dynamic dY = y;
            Assert.Equal(x & y, dX & dY);
        }

        [Theory, MemberData(nameof(CrossJoinInt32))]
        public void DivideInt32(int x, int y)
        {
            dynamic dX = x;
            dynamic dY = y;
            if (y == 0)
                Assert.Throws<DivideByZeroException>(() => dX / dY);
            else if (y == -1 && x == int.MinValue)
                Assert.Throws<OverflowException>(() => dX / dY);
            else
                Assert.Equal(x / y, dX / dY);
        }

        [Theory, MemberData(nameof(CrossJoinInt32))]
        public void EqualInt32(int x, int y)
        {
            dynamic dX = x;
            dynamic dY = y;
            Assert.Equal(x == y, dX == dY);
        }

        [Theory, MemberData(nameof(CrossJoinInt32))]
        public void ExclusiveOrInt32(int x, int y)
        {
            dynamic dX = x;
            dynamic dY = y;
            Assert.Equal(x ^ y, dX ^ dY);
        }

        [Theory, MemberData(nameof(CrossJoinInt32))]
        public void GreaterThanInt32(int x, int y)
        {
            dynamic dX = x;
            dynamic dY = y;
            Assert.Equal(x > y, dX > dY);
        }

        [Theory, MemberData(nameof(CrossJoinInt32))]
        public void GreaterThanOrEqualInt32(int x, int y)
        {
            dynamic dX = x;
            dynamic dY = y;
            Assert.Equal(x >= y, dX >= dY);
        }

        [Theory, MemberData(nameof(CrossJoinInt32))]
        public void LeftShiftInt32(int x, int y)
        {
            dynamic dX = x;
            dynamic dY = y;
            Assert.Equal(x << y, dX << dY);
        }

        [Theory, MemberData(nameof(CrossJoinInt32))]
        public void LessThanInt32(int x, int y)
        {
            dynamic dX = x;
            dynamic dY = y;
            Assert.Equal(x < y, dX < dY);
        }

        [Theory, MemberData(nameof(CrossJoinInt32))]
        public void LessThanOrEqualInt32(int x, int y)
        {
            dynamic dX = x;
            dynamic dY = y;
            Assert.Equal(x <= y, dX <= dY);
        }

        [Theory, MemberData(nameof(CrossJoinInt32))]
        public void ModuloInt32(int x, int y)
        {
            dynamic dX = x;
            dynamic dY = y;
            if (y == 0)
                Assert.Throws<DivideByZeroException>(() => dX % dY);
            else if (y == -1 && x == int.MinValue)
                Assert.Throws<OverflowException>(() => dX % dY);
            else
                Assert.Equal(x % y, dX % dY);
        }

        [Theory, MemberData(nameof(CrossJoinInt32))]
        public void MultiplyInt32(int x, int y)
        {
            dynamic dX = x;
            dynamic dY = y;
            Assert.Equal(unchecked(x * y), unchecked(dX * dY));
        }

        [Theory, MemberData(nameof(CrossJoinInt32))]
        public void MultiplyOvfInt32(int x, int y)
        {
            dynamic dX = x;
            dynamic dY = y;
            int result;
            try
            {
                result = checked(x * y);
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => checked(dX * dY));
                return;
            }

            Assert.Equal(result, dX * dY);
        }

        [Theory, MemberData(nameof(CrossJoinInt32))]
        public void NotEqualInt32(int x, int y)
        {
            dynamic dX = x;
            dynamic dY = y;
            Assert.Equal(x != y, dX != dY);
        }

        [Theory, MemberData(nameof(CrossJoinInt32))]
        public void OrInt32(int x, int y)
        {
            dynamic dX = x;
            dynamic dY = y;
            Assert.Equal(x | y, dX | dY);
        }

        [Theory, MemberData(nameof(CrossJoinInt32))]
        public void RightShiftInt32(int x, int y)
        {
            dynamic dX = x;
            dynamic dY = y;
            Assert.Equal(x >> y, dX >> dY);
        }

        [Theory, MemberData(nameof(CrossJoinInt32))]
        public void SubtractInt32(int x, int y)
        {
            dynamic dX = x;
            dynamic dY = y;
            Assert.Equal(unchecked(x - y), unchecked(dX - dY));
        }

        [Theory, MemberData(nameof(CrossJoinInt32))]
        public void SubtractOvfInt32(int x, int y)
        {
            dynamic dX = x;
            dynamic dY = y;
            int result;
            try
            {
                result = checked(x - y);
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => checked(dX - dY));
                return;
            }

            Assert.Equal(result, checked(dX - dY));
        }

        [Theory, MemberData(nameof(CrossJoinInt32))]
        public void AddInt32Assign(int x, int y)
        {
            dynamic dX = x;
            dynamic dY = y;

            unchecked
            {
                dX += dY;
                Assert.Equal(x + y, dX);
            }
        }

        [Theory, MemberData(nameof(CrossJoinInt32))]
        public void AddOvfInt32Assign(int x, int y)
        {
            dynamic dX = x;
            dynamic dY = y;
            int result;
            try
            {
                result = checked(x + y);
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => checked(dX += dY));
                return;
            }

            checked
            {
                dX += dY;
            }
            Assert.Equal(result, dX);
        }

        [Theory, MemberData(nameof(CrossJoinInt32))]
        public void AndInt32Assign(int x, int y)
        {
            dynamic dX = x;
            dynamic dY = y;
            dX &= dY;
            Assert.Equal(x & y, dX);
        }

        [Theory, MemberData(nameof(CrossJoinInt32))]
        public void DivideInt32Assign(int x, int y)
        {
            dynamic dX = x;
            dynamic dY = y;
            if (y == 0)
                Assert.Throws<DivideByZeroException>(() => dX /= dY);
            else if (y == -1 && x == int.MinValue)
                Assert.Throws<OverflowException>(() => dX /= dY);
            else
            {
                dX /= dY;
                Assert.Equal(x / y, dX);
            }
        }

        [Theory, MemberData(nameof(CrossJoinInt32))]
        public void ExclusiveOrInt32Assign(int x, int y)
        {
            dynamic dX = x;
            dynamic dY = y;
            dX ^= dY;
            Assert.Equal(x ^ y, dX);
        }

        [Theory, MemberData(nameof(CrossJoinInt32))]
        public void LeftShiftInt32Assign(int x, int y)
        {
            dynamic dX = x;
            dynamic dY = y;
            dX <<= dY;
            Assert.Equal(x << y, dX);
        }

        [Theory, MemberData(nameof(CrossJoinInt32))]
        public void ModuloInt32Assign(int x, int y)
        {
            dynamic dX = x;
            dynamic dY = y;
            if (y == 0)
                Assert.Throws<DivideByZeroException>(() => dX %= dY);
            else if (y == -1 && x == int.MinValue)
                Assert.Throws<OverflowException>(() => dX %= dY);
            else
            {
                dX %= dY;
                Assert.Equal(x % y, dX);
            }
        }

        [Theory, MemberData(nameof(CrossJoinInt32))]
        public void MultiplyInt32Assign(int x, int y)
        {
            dynamic dX = x;
            dynamic dY = y;

            unchecked
            {
                dX *= dY;
                Assert.Equal(x * y, dX);
            }
        }

        [Theory, MemberData(nameof(CrossJoinInt32))]
        public void MultiplyOvfInt32Assign(int x, int y)
        {
            dynamic dX = x;
            dynamic dY = y;
            int result;
            try
            {
                result = checked(x * y);
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => checked(dX *= dY));
                return;
            }

            dX *= dY;
            Assert.Equal(result, dX);
        }


        [Theory, MemberData(nameof(CrossJoinInt32))]
        public void OrInt32Assign(int x, int y)
        {
            dynamic dX = x;
            dynamic dY = y;
            dX |= dY;
            Assert.Equal(x | y, dX);
        }

        [Theory, MemberData(nameof(CrossJoinInt32))]
        public void RightShiftInt32Assign(int x, int y)
        {
            dynamic dX = x;
            dynamic dY = y;
            dX >>= dY;
            Assert.Equal(x >> y, dX);
        }

        [Theory, MemberData(nameof(CrossJoinInt32))]
        public void SubtractInt32Assign(int x, int y)
        {
            dynamic dX = x;
            dynamic dY = y;

            unchecked
            {
                dX -= dY;
                Assert.Equal(x - y, dX);
            }
        }

        [Theory, MemberData(nameof(CrossJoinInt32))]
        public void SubtractOvfInt32Assign(int x, int y)
        {
            dynamic dX = x;
            dynamic dY = y;
            int result;
            try
            {
                result = checked(x - y);
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => checked(dX -= dY));
                return;
            }

            checked
            {
                dX -= dY;
            }
            Assert.Equal(result, dX);
        }

        [Theory, MemberData(nameof(CrossJoinDouble))]
        public void AddDouble(double x, double y)
        {
            dynamic dX = x;
            dynamic dY = y;
            Assert.Equal(x + y, dX + dY);
        }

        [Theory, MemberData(nameof(CrossJoinDouble))]
        public void DivideDouble(double x, double y)
        {
            dynamic dX = x;
            dynamic dY = y;
            Assert.Equal(x / y, dX / dY);
        }

        [Theory, MemberData(nameof(CrossJoinDouble))]
        public void EqualDouble(double x, double y)
        {
            dynamic dX = x;
            dynamic dY = y;
            Assert.Equal(x == y, dX == dY);
        }

        [Theory, MemberData(nameof(CrossJoinDouble))]
        public void GreaterThanDouble(double x, double y)
        {
            dynamic dX = x;
            dynamic dY = y;
            Assert.Equal(x > y, dX > dY);
        }

        [Theory, MemberData(nameof(CrossJoinDouble))]
        public void GreaterThanOrEqualDouble(double x, double y)
        {
            dynamic dX = x;
            dynamic dY = y;
            Assert.Equal(x >= y, dX >= dY);
        }

        [Theory, MemberData(nameof(CrossJoinDouble))]
        public void LessThanDouble(double x, double y)
        {
            dynamic dX = x;
            dynamic dY = y;
            Assert.Equal(x < y, dX < dY);
        }

        [Theory, MemberData(nameof(CrossJoinDouble))]
        public void LessThanOrEqualDouble(double x, double y)
        {
            dynamic dX = x;
            dynamic dY = y;
            Assert.Equal(x <= y, dX <= dY);
        }

        [Theory, MemberData(nameof(CrossJoinDouble))]
        public void ModuloDouble(double x, double y)
        {
            dynamic dX = x;
            dynamic dY = y;
            Assert.Equal(x % y, dX % dY);
        }

        [Theory, MemberData(nameof(CrossJoinDouble))]
        public void MultiplyDouble(double x, double y)
        {
            dynamic dX = x;
            dynamic dY = y;
            Assert.Equal(x * y, dX * dY);
        }


        [Theory, MemberData(nameof(CrossJoinDouble))]
        public void NotEqualDouble(double x, double y)
        {
            dynamic dX = x;
            dynamic dY = y;
            Assert.Equal(x != y, dX != dY);
        }

        [Theory, MemberData(nameof(CrossJoinDouble))]
        public void SubtractDouble(double x, double y)
        {
            dynamic dX = x;
            dynamic dY = y;
            Assert.Equal(x - y, dX - dY);
        }

        [Theory, MemberData(nameof(CrossJoinDouble))]
        public void AddDoubleAssign(double x, double y)
        {
            dynamic dX = x;
            dynamic dY = y;
            dX += dY;
            Assert.Equal(x + y, dX);
        }

        [Theory, MemberData(nameof(CrossJoinDouble))]
        public void DivideDoubleAssign(double x, double y)
        {
            dynamic dX = x;
            dynamic dY = y;
            dX /= dY;
            Assert.Equal(x / y, dX);
        }

        [Theory, MemberData(nameof(CrossJoinDouble))]
        public void ModuloDoubleAssign(double x, double y)
        {
            dynamic dX = x;
            dynamic dY = y;
            dX %= dY;
            Assert.Equal(x % y, dX);
        }

        [Theory, MemberData(nameof(CrossJoinDouble))]
        public void MultiplyDoubleAssign(double x, double y)
        {
            dynamic dX = x;
            dynamic dY = y;
            dX *= dY;
            Assert.Equal(x * y, dX);
        }

        [Theory, MemberData(nameof(CrossJoinDouble))]
        public void SubtractDoubleAssign(double x, double y)
        {
            dynamic dX = x;
            dynamic dY = y;
            dX -= dY;
            Assert.Equal(x - y, dX);
        }

        [Theory, MemberData(nameof(NonBinaryExpressionTypes))]
        public void NonBinaryOperations(ExpressionType type)
        {
            AssertExtensions.Throws<ArgumentException>("operation", () => new MinimumOverrideBinaryOperationBinder(type));
        }

        [Theory, MemberData(nameof(BinaryExpressionTypes))]
        public void ReturnType(ExpressionType type)
        {
            Assert.Equal(typeof(object), new MinimumOverrideBinaryOperationBinder(type).ReturnType);
        }

        [Theory, MemberData(nameof(BinaryExpressionTypes))]
        public void ExpressionTypeMatches(ExpressionType type)
        {
            Assert.Equal(type, new MinimumOverrideBinaryOperationBinder(type).Operation);
        }

        [Fact]
        public void NullTarget()
        {
            var binder = new MinimumOverrideBinaryOperationBinder(ExpressionType.Add);
            var arg = new DynamicMetaObject(Expression.Parameter(typeof(object), null), BindingRestrictions.Empty);
            AssertExtensions.Throws<ArgumentNullException>("target", () => binder.Bind(null, new[] {arg}));
        }

        [Fact]
        public void NullArgumentArrayPassed()
        {
            var target = new DynamicMetaObject(Expression.Parameter(typeof(object), null), BindingRestrictions.Empty);
            var binder = new MinimumOverrideBinaryOperationBinder(ExpressionType.Add);
            AssertExtensions.Throws<ArgumentNullException>("args", () => binder.Bind(target, null));
        }

        [Fact]
        public void NoArgumentsPassed()
        {
            var target = new DynamicMetaObject(Expression.Parameter(typeof(object), null), BindingRestrictions.Empty);
            var binder = new MinimumOverrideBinaryOperationBinder(ExpressionType.Add);
            AssertExtensions.Throws<ArgumentException>("args", () => binder.Bind(target, Array.Empty<DynamicMetaObject>()));
        }

        [Fact]
        public void TooManyArgumentArrayPassed()
        {
            var target = new DynamicMetaObject(Expression.Parameter(typeof(object), null), BindingRestrictions.Empty);
            var binder = new MinimumOverrideBinaryOperationBinder(ExpressionType.Add);
            var arg0 = new DynamicMetaObject(Expression.Parameter(typeof(object), null), BindingRestrictions.Empty);
            var arg1 = new DynamicMetaObject(Expression.Parameter(typeof(object), null), BindingRestrictions.Empty);
            AssertExtensions.Throws<ArgumentException>("args", () => binder.Bind(target, new[] {arg0, arg1}));
        }

        [Fact]
        public void SingleNullArgumentPassed()
        {
            var target = new DynamicMetaObject(Expression.Parameter(typeof(object), null), BindingRestrictions.Empty);
            var binder = new MinimumOverrideBinaryOperationBinder(ExpressionType.Add);
            AssertExtensions.Throws<ArgumentNullException>("args", () => binder.Bind(target, new DynamicMetaObject[1]));
        }

        [Fact]
        public void InvalidOperationForType()
        {
            dynamic dX = "23";
            dynamic dY = "49";
            Assert.Throws<RuntimeBinderException>(() => dX * dY);
            dX = 23;
            dY = 49;
            Assert.Throws<RuntimeBinderException>(() => dX && dY);
        }

        [Fact]
        public void LiteralDoubleNaN()
        {
            dynamic d = double.NaN;
            Assert.False(d == double.NaN);
            Assert.True(d != double.NaN);
            d = 3.0;
            Assert.True(double.IsNaN(d + double.NaN));
        }

        [Fact]
        public void LiteralSingleNaN()
        {
            dynamic d = float.NaN;
            Assert.False(d == float.NaN);
            Assert.True(d != float.NaN);
            d = 3.0F;
            Assert.True(float.IsNaN(d + float.NaN));
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public void BinaryCallSiteBinder_DynamicExpression(bool useInterpreter)
        {
            DynamicExpression expression = DynamicExpression.Dynamic(
                new BinaryCallSiteBinder(),
                typeof(object),
                Expression.Constant(40, typeof(object)),
                Expression.Constant(2, typeof(object)));
            Func<object> func = Expression.Lambda<Func<object>>(expression).Compile(useInterpreter);
            Assert.Equal("42", func().ToString());
        }

        private class BinaryCallSiteBinder : BinaryOperationBinder
        {
            public BinaryCallSiteBinder() : base(ExpressionType.Add) {}

            public override DynamicMetaObject FallbackBinaryOperation(DynamicMetaObject target, DynamicMetaObject arg, DynamicMetaObject errorSuggestion)
            {
                return new DynamicMetaObject(
                    Expression.Convert(
                    Expression.Add(
                        Expression.Convert(target.Expression, typeof(int)),
                        Expression.Convert(arg.Expression, typeof(int))
                    ), typeof(object)),

                    BindingRestrictions.GetTypeRestriction(target.Expression, typeof(int)).Merge(
                        BindingRestrictions.GetTypeRestriction(arg.Expression, typeof(int))
                    ));
            }
        }
    }
}
