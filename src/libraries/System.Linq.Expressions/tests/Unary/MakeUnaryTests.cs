// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Reflection;
using Xunit;

namespace System.Linq.Expressions.Tests
{
    public class MakeUnaryTests
    {
        public static readonly object[][] NonUnaryExpressionTypes =
        {
            new object[] {ExpressionType.Add},
            new object[] {ExpressionType.AddChecked},
            new object[] {ExpressionType.And},
            new object[] {ExpressionType.AndAlso},
            new object[] {ExpressionType.ArrayIndex},
            new object[] {ExpressionType.Call},
            new object[] {ExpressionType.Coalesce},
            new object[] {ExpressionType.Conditional},
            new object[] {ExpressionType.Constant},
            new object[] {ExpressionType.Divide},
            new object[] {ExpressionType.Equal},
            new object[] {ExpressionType.ExclusiveOr},
            new object[] {ExpressionType.GreaterThan},
            new object[] {ExpressionType.GreaterThanOrEqual},
            new object[] {ExpressionType.Invoke},
            new object[] {ExpressionType.Lambda},
            new object[] {ExpressionType.LeftShift},
            new object[] {ExpressionType.LessThan},
            new object[] {ExpressionType.LessThanOrEqual},
            new object[] {ExpressionType.ListInit},
            new object[] {ExpressionType.MemberAccess},
            new object[] {ExpressionType.MemberInit},
            new object[] {ExpressionType.Modulo},
            new object[] {ExpressionType.Multiply},
            new object[] {ExpressionType.MultiplyChecked},
            new object[] {ExpressionType.New},
            new object[] {ExpressionType.NewArrayInit},
            new object[] {ExpressionType.NewArrayBounds},
            new object[] {ExpressionType.NotEqual},
            new object[] {ExpressionType.Or},
            new object[] {ExpressionType.OrElse},
            new object[] {ExpressionType.Parameter},
            new object[] {ExpressionType.Power},
            new object[] {ExpressionType.RightShift},
            new object[] {ExpressionType.Subtract},
            new object[] {ExpressionType.SubtractChecked},
            new object[] {ExpressionType.TypeIs},
            new object[] {ExpressionType.Assign},
            new object[] {ExpressionType.Block},
            new object[] {ExpressionType.DebugInfo},
            new object[] {ExpressionType.Dynamic},
            new object[] {ExpressionType.Default},
            new object[] {ExpressionType.Extension},
            new object[] {ExpressionType.Goto},
            new object[] {ExpressionType.Index},
            new object[] {ExpressionType.Label},
            new object[] {ExpressionType.RuntimeVariables},
            new object[] {ExpressionType.Loop},
            new object[] {ExpressionType.Switch},
            new object[] {ExpressionType.Try},
            new object[] {ExpressionType.AddAssign},
            new object[] {ExpressionType.AndAssign},
            new object[] {ExpressionType.DivideAssign},
            new object[] {ExpressionType.ExclusiveOrAssign},
            new object[] {ExpressionType.LeftShiftAssign},
            new object[] {ExpressionType.ModuloAssign},
            new object[] {ExpressionType.MultiplyAssign},
            new object[] {ExpressionType.OrAssign},
            new object[] {ExpressionType.PowerAssign},
            new object[] {ExpressionType.RightShiftAssign},
            new object[] {ExpressionType.SubtractAssign},
            new object[] {ExpressionType.AddAssignChecked},
            new object[] {ExpressionType.MultiplyAssignChecked},
            new object[] {ExpressionType.SubtractAssignChecked},
            new object[] {ExpressionType.TypeEqual},
            new object[] {(ExpressionType)(-1)},
            new object[] {(ExpressionType)int.MaxValue}
        };

        public static IEnumerable<object[]> NumericMethodAllowedUnaryTypes
        {
            get
            {
                yield return new object[] {ExpressionType.Negate};
                yield return new object[] {ExpressionType.NegateChecked};
                yield return new object[] {ExpressionType.OnesComplement};
                yield return new object[] {ExpressionType.UnaryPlus};
                yield return new object[] {ExpressionType.Increment};
                yield return new object[] {ExpressionType.Decrement};
                yield return new object[] {ExpressionType.PreIncrementAssign};
                yield return new object[] {ExpressionType.PostIncrementAssign};
                yield return new object[] {ExpressionType.PreDecrementAssign};
                yield return new object[] {ExpressionType.PostDecrementAssign};
            }
        }

        public static IEnumerable<object[]> BooleanMethodAllowedUnaryTypes
        {
            get
            {
                yield return new object[] {ExpressionType.Not};
                yield return new object[] {ExpressionType.IsFalse};
                yield return new object[] {ExpressionType.IsTrue};
            }
        }

        public static IEnumerable<object[]> ConversionUnaryTypes
        {
            get
            {
                yield return new object[] {ExpressionType.Convert};
                yield return new object[] {ExpressionType.ConvertChecked};
            }
        }

        private class GenericClassWithNonGenericMethod<TClassType>
        {
            public static int DoIntStuff(int x) => x;

            public static bool DoBooleanStuff(bool b) => b;

            public static int CastLongToInt(long l) => unchecked((int)l);
        }

        [Theory, MemberData(nameof(NonUnaryExpressionTypes))]
        public void MakeUnaryExpressionNonUnary(ExpressionType type)
        {
            AssertExtensions.Throws<ArgumentException>("unaryType", () => Expression.MakeUnary(type, null, null));
        }

        [Theory, MemberData(nameof(NumericMethodAllowedUnaryTypes))]
        public void MethodOfOpenGenericOnNumeric(ExpressionType type)
        {
            ParameterExpression variable = Expression.Variable(typeof(int));
            Type genType = typeof(GenericClassWithNonGenericMethod<>);
            MethodInfo method = genType.GetMethod(nameof(GenericClassWithNonGenericMethod<int>.DoIntStuff));
            AssertExtensions.Throws<ArgumentException>("method", () => Expression.MakeUnary(type, variable, typeof(int), method));
            method = genType.MakeGenericType(genType).GetMethod(nameof(GenericClassWithNonGenericMethod<int>.DoIntStuff));
            AssertExtensions.Throws<ArgumentException>("method", () => Expression.MakeUnary(type, variable, typeof(int), method));
            // Demonstrate does work when closed.
            method = genType.MakeGenericType(typeof(int)).GetMethod(nameof(GenericClassWithNonGenericMethod<int>.DoIntStuff));
            Expression.MakeUnary(type, variable, typeof(int), method);
        }

        [Theory, MemberData(nameof(BooleanMethodAllowedUnaryTypes))]
        public void MethodOfOpenGenericOnBoolean(ExpressionType type)
        {
            ParameterExpression variable = Expression.Variable(typeof(bool));
            Type genType = typeof(GenericClassWithNonGenericMethod<>);
            MethodInfo method = genType.GetMethod(nameof(GenericClassWithNonGenericMethod<int>.DoBooleanStuff));
            AssertExtensions.Throws<ArgumentException>("method", () => Expression.MakeUnary(type, variable, typeof(bool), method));
            method = genType.MakeGenericType(genType).GetMethod(nameof(GenericClassWithNonGenericMethod<int>.DoBooleanStuff));
            AssertExtensions.Throws<ArgumentException>("method", () => Expression.MakeUnary(type, variable, typeof(bool), method));
            // Demonstrate does work when closed.
            method = genType.MakeGenericType(typeof(int)).GetMethod(nameof(GenericClassWithNonGenericMethod<int>.DoBooleanStuff));
            Expression.MakeUnary(type, variable, typeof(bool), method);
        }

        [Theory, MemberData(nameof(BooleanMethodAllowedUnaryTypes))]
        public void MethodOfOpenGenericOnConversion(ExpressionType type)
        {
            ParameterExpression variable = Expression.Variable(typeof(long));
            Type genType = typeof(GenericClassWithNonGenericMethod<>);
            MethodInfo method = genType.GetMethod(nameof(GenericClassWithNonGenericMethod<int>.CastLongToInt));
            AssertExtensions.Throws<ArgumentException>("method", () => Expression.MakeUnary(type, variable, typeof(int), method));
            method = genType.MakeGenericType(genType).GetMethod(nameof(GenericClassWithNonGenericMethod<int>.CastLongToInt));
            AssertExtensions.Throws<ArgumentException>("method", () => Expression.MakeUnary(type, variable, typeof(int), method));
            // Demonstrate does work when closed.
            method = genType.MakeGenericType(typeof(int)).GetMethod(nameof(GenericClassWithNonGenericMethod<int>.CastLongToInt));
            Expression.MakeUnary(type, variable, typeof(int), method);
        }
    }
}
