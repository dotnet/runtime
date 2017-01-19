// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.CSharp.RuntimeBinder;
using Xunit;

namespace System.Dynamic.Tests
{
    public class UnaryOperationTests
    {
        private class MinimumOverrideUnaryOperationBinder : UnaryOperationBinder
        {
            public MinimumOverrideUnaryOperationBinder(ExpressionType operation)
                : base(operation)
            {
            }

            public override DynamicMetaObject FallbackUnaryOperation(DynamicMetaObject target, DynamicMetaObject errorSuggestion)
            {
                throw new NotSupportedException();
            }
        }

        private static readonly int[] SomeInt32 = { 0, 1, 2, -1, int.MinValue, int.MaxValue, int.MaxValue - 1 };

        private static IEnumerable<object[]> Int32Args() => SomeInt32.Select(i => new object[] {i});

        private static IEnumerable<object[]> BooleanArgs()
        {
            yield return new object[] {false};
            yield return new object[] {true};
        }

        private static IEnumerable<object[]> UnaryExpressionTypes()
        {
            yield return new object[] {ExpressionType.Decrement};
            yield return new object[] {ExpressionType.Extension};
            yield return new object[] {ExpressionType.Increment};
            yield return new object[] {ExpressionType.IsFalse};
            yield return new object[] {ExpressionType.IsTrue};
            yield return new object[] {ExpressionType.Negate};
            yield return new object[] {ExpressionType.Not};
            yield return new object[] {ExpressionType.OnesComplement};
            yield return new object[] {ExpressionType.UnaryPlus};
        }

        private static IEnumerable<object[]> NonUnaryExpressionTypes()
        {
            yield return new object[] {ExpressionType.Add};
            yield return new object[] {ExpressionType.AddAssign};
            yield return new object[] {ExpressionType.AddAssignChecked};
            yield return new object[] {ExpressionType.AddChecked};
            yield return new object[] {ExpressionType.And};
            yield return new object[] {ExpressionType.AndAlso};
            yield return new object[] {ExpressionType.AndAssign};
            yield return new object[] {ExpressionType.ArrayIndex};
            yield return new object[] {ExpressionType.ArrayLength};
            yield return new object[] {ExpressionType.Assign};
            yield return new object[] {ExpressionType.Block};
            yield return new object[] {ExpressionType.Call};
            yield return new object[] {ExpressionType.Coalesce};
            yield return new object[] {ExpressionType.Conditional};
            yield return new object[] {ExpressionType.Constant};
            yield return new object[] {ExpressionType.Convert};
            yield return new object[] {ExpressionType.ConvertChecked};
            yield return new object[] {ExpressionType.DebugInfo};
            yield return new object[] {ExpressionType.Default};
            yield return new object[] {ExpressionType.Divide};
            yield return new object[] {ExpressionType.DivideAssign};
            yield return new object[] {ExpressionType.Dynamic};
            yield return new object[] {ExpressionType.Equal};
            yield return new object[] {ExpressionType.ExclusiveOr};
            yield return new object[] {ExpressionType.ExclusiveOrAssign};
            yield return new object[] {ExpressionType.Goto};
            yield return new object[] {ExpressionType.GreaterThan};
            yield return new object[] {ExpressionType.GreaterThanOrEqual};
            yield return new object[] {ExpressionType.Index};
            yield return new object[] {ExpressionType.Invoke};
            yield return new object[] {ExpressionType.Label};
            yield return new object[] {ExpressionType.Lambda};
            yield return new object[] {ExpressionType.LeftShift};
            yield return new object[] {ExpressionType.LeftShiftAssign};
            yield return new object[] {ExpressionType.LessThan};
            yield return new object[] {ExpressionType.LessThanOrEqual};
            yield return new object[] {ExpressionType.ListInit};
            yield return new object[] {ExpressionType.Loop};
            yield return new object[] {ExpressionType.MemberAccess};
            yield return new object[] {ExpressionType.MemberInit};
            yield return new object[] {ExpressionType.Modulo};
            yield return new object[] {ExpressionType.ModuloAssign};
            yield return new object[] {ExpressionType.Multiply};
            yield return new object[] {ExpressionType.MultiplyAssign};
            yield return new object[] {ExpressionType.MultiplyAssignChecked};
            yield return new object[] {ExpressionType.MultiplyChecked};
            yield return new object[] {ExpressionType.NegateChecked};
            yield return new object[] {ExpressionType.New};
            yield return new object[] {ExpressionType.NewArrayBounds};
            yield return new object[] {ExpressionType.NewArrayInit};
            yield return new object[] {ExpressionType.NotEqual};
            yield return new object[] {ExpressionType.Or};
            yield return new object[] {ExpressionType.OrAssign};
            yield return new object[] {ExpressionType.OrElse};
            yield return new object[] {ExpressionType.Parameter};
            yield return new object[] {ExpressionType.PostDecrementAssign};
            yield return new object[] {ExpressionType.PostIncrementAssign};
            yield return new object[] {ExpressionType.Power};
            yield return new object[] {ExpressionType.PowerAssign};
            yield return new object[] {ExpressionType.PreDecrementAssign};
            yield return new object[] {ExpressionType.PreIncrementAssign};
            yield return new object[] {ExpressionType.Quote};
            yield return new object[] {ExpressionType.RightShift};
            yield return new object[] {ExpressionType.RightShiftAssign};
            yield return new object[] {ExpressionType.RuntimeVariables};
            yield return new object[] {ExpressionType.Subtract};
            yield return new object[] {ExpressionType.SubtractAssign};
            yield return new object[] {ExpressionType.SubtractAssignChecked};
            yield return new object[] {ExpressionType.SubtractChecked};
            yield return new object[] {ExpressionType.Switch};
            yield return new object[] {ExpressionType.Throw};
            yield return new object[] {ExpressionType.Try};
            yield return new object[] {ExpressionType.TypeAs};
            yield return new object[] {ExpressionType.TypeEqual};
            yield return new object[] {ExpressionType.TypeIs};
            yield return new object[] {ExpressionType.Unbox};
        }

        [Theory, MemberData(nameof(Int32Args))]
        public void DecrementPrefixInt32(int x)
        {
            dynamic d = x;
            Assert.Equal(x - 1, --d);
            Assert.Equal(x - 1, d);
        }

        [Theory, MemberData(nameof(Int32Args))]
        public void DecrementPostfixInt32(int x)
        {
            dynamic d = x;
            Assert.Equal(x, d--);
            Assert.Equal(x - 1, d);
        }

        [Theory, MemberData(nameof(Int32Args))]
        public void DecrementPrefixOvfInt32(int x)
        {
            dynamic d = x;
            if (x == int.MinValue)
            {
                Assert.Throws<OverflowException>(() => checked(--d));
            }
            else
            {
                checked
                {
                    Assert.Equal(x - 1, --d);
                    Assert.Equal(x - 1, d);
                }
            }
        }

        [Theory, MemberData(nameof(Int32Args))]
        public void DecrementPostfixOvfInt32(int x)
        {
            dynamic d = x;
            if (x == int.MinValue)
            {
                Assert.Throws<OverflowException>(() => checked(d--));
            }
            else
            {
                checked
                {
                    Assert.Equal(x, d--);
                    Assert.Equal(x - 1, d);
                }
            }
        }

        [Theory, MemberData(nameof(Int32Args))]
        public void IncrementPrefixInt32(int x)
        {
            dynamic d = x;
            Assert.Equal(x + 1, ++d);
            Assert.Equal(x + 1, d);
        }

        [Theory, MemberData(nameof(Int32Args))]
        public void IncrementPostfixInt32(int x)
        {
            dynamic d = x;
            Assert.Equal(x, d++);
            Assert.Equal(x + 1, d);
        }

        [Theory, MemberData(nameof(Int32Args))]
        public void IncrementPrefixOvfInt32(int x)
        {
            dynamic d = x;
            if (x == int.MaxValue)
            {
                Assert.Throws<OverflowException>(() => checked(++d));
            }
            else
            {
                checked
                {
                    Assert.Equal(x + 1, ++d);
                    Assert.Equal(x + 1, d);
                }
            }
        }

        [Theory, MemberData(nameof(Int32Args))]
        public void IncrementPostfixOvfInt32(int x)
        {
            dynamic d = x;
            if (x == int.MaxValue)
            {
                Assert.Throws<OverflowException>(() => checked(d++));
            }
            else
            {
                checked
                {
                    Assert.Equal(x, d++);
                    Assert.Equal(x + 1, d);
                }
            }
        }

        [Theory, MemberData(nameof(Int32Args))]
        public void NegateInt32(int x)
        {
            dynamic d = x;
            Assert.Equal(-x, -d);
        }

        [Theory, MemberData(nameof(Int32Args))]
        public void NegateOvfInt32(int x)
        {
            dynamic d = x;
            if (x == int.MinValue)
            {
                Assert.Throws<OverflowException>(() => checked(-d));
            }
            else
            {
                Assert.Equal(-x, -d);
            }
        }

        [Theory, MemberData(nameof(Int32Args))]
        public void UnaryPlusInt32(int x)
        {
            dynamic d = x;
            Assert.Equal(x, +d);
        }

        [Theory, MemberData(nameof(Int32Args))]
        public void OnesComplementInt32(int x)
        {
            dynamic d = x;
            Assert.Equal(~x, ~d);
        }

        [Theory, MemberData(nameof(BooleanArgs))]
        public void NotBoolean(bool x)
        {
            dynamic d = x;
            Assert.Equal(!x, !d);
        }

        [Theory, MemberData(nameof(BooleanArgs))]
        public void IsTrueBoolean(bool x)
        {
            dynamic d = x;
            Assert.Equal(x ? 1 : 2, d ? 1 : 2);
        }

        [Theory, MemberData(nameof(BooleanArgs))]
        public void IsFalse(bool x)
        {
            dynamic d = x;
            Assert.Equal(x, d && true);
        }

        [Theory, MemberData(nameof(NonUnaryExpressionTypes))]
        public void NonUnaryOperation(ExpressionType type)
        {
            Assert.Throws<ArgumentException>("operation", () => new MinimumOverrideUnaryOperationBinder(type));
        }

        [Theory, MemberData(nameof(UnaryExpressionTypes))]
        public void ReturnType(ExpressionType type)
        {
            Assert.Equal(
                type >= ExpressionType.IsTrue ? typeof(bool) : typeof(object),
                new MinimumOverrideUnaryOperationBinder(type).ReturnType);
        }

        [Theory, MemberData(nameof(UnaryExpressionTypes))]
        public void ExpressionTypeMatches(ExpressionType type)
        {
            Assert.Equal(type, new MinimumOverrideUnaryOperationBinder(type).Operation);
        }

        [Fact]
        public void NullTarget()
        {
            var binder = new MinimumOverrideUnaryOperationBinder(ExpressionType.Negate);
            Assert.Throws<ArgumentNullException>("target", () => binder.Bind(null, null));
        }

        [Fact]
        public void ArgumentPassed()
        {
            var target = new DynamicMetaObject(Expression.Parameter(typeof(object), null), BindingRestrictions.Empty);
            var arg = new DynamicMetaObject(Expression.Parameter(typeof(object), null), BindingRestrictions.Empty);
            var binder = new MinimumOverrideUnaryOperationBinder(ExpressionType.Negate);
            Assert.Throws<ArgumentException>("args", () => binder.Bind(target, new[] {arg}));
        }

        [Fact]
        public void InvalidOperationForType()
        {
            dynamic d = "23";
            Assert.Throws<RuntimeBinderException>(() => -d);
            d = 23;
            Assert.Throws<RuntimeBinderException>(() => !d);
        }
    }
}
