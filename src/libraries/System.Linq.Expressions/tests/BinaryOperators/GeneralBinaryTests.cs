// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Linq.Expressions.Tests
{
    public class GeneralBinaryTests
    {
        private static readonly ExpressionType[] BinaryTypes =
        {
            ExpressionType.Add, ExpressionType.AddChecked, ExpressionType.Subtract, ExpressionType.SubtractChecked,
            ExpressionType.Multiply, ExpressionType.MultiplyChecked, ExpressionType.Divide, ExpressionType.Modulo,
            ExpressionType.Power, ExpressionType.And, ExpressionType.AndAlso, ExpressionType.Or, ExpressionType.OrElse,
            ExpressionType.LessThan, ExpressionType.LessThanOrEqual, ExpressionType.GreaterThan, ExpressionType.GreaterThanOrEqual,
            ExpressionType.Equal, ExpressionType.NotEqual, ExpressionType.ExclusiveOr, ExpressionType.Coalesce,
            ExpressionType.ArrayIndex, ExpressionType.RightShift, ExpressionType.LeftShift, ExpressionType.Assign,
            ExpressionType.AddAssign, ExpressionType.AndAssign, ExpressionType.DivideAssign, ExpressionType.ExclusiveOrAssign,
            ExpressionType.LeftShiftAssign, ExpressionType.ModuloAssign, ExpressionType.MultiplyAssign, ExpressionType.OrAssign,
            ExpressionType.PowerAssign, ExpressionType.RightShiftAssign, ExpressionType.SubtractAssign,
            ExpressionType.AddAssignChecked, ExpressionType.SubtractAssignChecked, ExpressionType.MultiplyAssignChecked
        };

        public static IEnumerable<object[]> BinaryTypesData()
        {
            return BinaryTypes.Select(i => new object[] { i });
        }

        private static ExpressionType[] NonBinaryTypes = ((ExpressionType[])Enum.GetValues(typeof(ExpressionType)))
            .Except(BinaryTypes)
            .ToArray();

        public static IEnumerable<ExpressionType> NonBinaryTypesIncludingInvalid = NonBinaryTypes.Concat(Enumerable.Repeat((ExpressionType)(-1), 1));

        public static IEnumerable<object[]> NonBinaryTypesIncludingInvalidData()
        {
            return NonBinaryTypesIncludingInvalid.Select(i => new object[] { i });
        }

        [Theory]
        [MemberData(nameof(NonBinaryTypesIncludingInvalidData))]
        public void MakeBinaryInvalidType(ExpressionType type)
        {
            Assert.Throws<ArgumentException>("binaryType", () => Expression.MakeBinary(type, Expression.Constant(0), Expression.Constant(0)));
            Assert.Throws<ArgumentException>("binaryType", () => Expression.MakeBinary(type, Expression.Constant(0), Expression.Constant(0), false, null));
            Assert.Throws<ArgumentException>("binaryType", () => Expression.MakeBinary(type, Expression.Constant(0), Expression.Constant(0), false, null, null));
        }

        [Theory]
        [MemberData(nameof(BinaryTypesData))]
        public void MakeBinaryLeftNull(ExpressionType type)
        {
            Assert.Throws<ArgumentNullException>(() => Expression.MakeBinary(type, null, Expression.Constant(0)));
            Assert.Throws<ArgumentNullException>(() => Expression.MakeBinary(type, null, Expression.Constant(0), false, null));
            Assert.Throws<ArgumentNullException>(() => Expression.MakeBinary(type, null, Expression.Constant(0), false, null, null));
        }

        [Theory]
        [MemberData(nameof(BinaryTypesData))]
        public void MakeBinaryRightNull(ExpressionType type)
        {
            Assert.Throws<ArgumentNullException>(() => Expression.MakeBinary(type, Expression.Variable(typeof(object)), null));
            Assert.Throws<ArgumentNullException>(() => Expression.MakeBinary(type, Expression.Variable(typeof(object)), null, false, null));
            Assert.Throws<ArgumentNullException>(() => Expression.MakeBinary(type, Expression.Variable(typeof(object)), null, false, null, null));
        }

        public static void CompileBinaryExpression(BinaryExpression expression, bool useInterpreter, bool expected)
        {
            Expression<Func<bool>> e = Expression.Lambda<Func<bool>>(expression, Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile(useInterpreter);

            Assert.Equal(expected, f());
        }

        public static bool CustomEquals(object a, object b)
        {
            // Allow for NaN
            if (a is double && b is double)
            {
                return (double)a == (double)b;
            }
            else if (a is float && b is float)
            {
                return (float)a == (float)b;
            }
            return a == null ? b == null : a.Equals(b);
        }

        public static bool CustomGreaterThan(object a, object b)
        {
            if (a is byte && b is byte)
            {
                return (byte)a > (byte)b;
            }
            else if (a is char && b is char)
            {
                return (char)a > (char)b;
            }
            else if (a is decimal && b is decimal)
            {
                return (decimal)a > (decimal)b;
            }
            else if (a is double && b is double)
            {
                return (double)a > (double)b;
            }
            else if (a is float && b is float)
            {
                return (float)a > (float)b;
            }
            else if (a is int && b is int)
            {
                return (int)a > (int)b;
            }
            else if (a is long && b is long)
            {
                return (long)a > (long)b;
            }
            else if (a is sbyte && b is sbyte)
            {
                return (sbyte)a > (sbyte)b;
            }
            else if (a is short && b is short)
            {
                return (short)a > (short)b;
            }
            else if (a is uint && b is uint)
            {
                return (uint)a > (uint)b;
            }
            else if (a is ulong && b is ulong)
            {
                return (ulong)a > (ulong)b;
            }
            else if (a is ushort && b is ushort)
            {
                return (ushort)a > (ushort)b;
            }
            return false;
        }

        public static bool CustomLessThan(object a, object b) => BothNotNull(a, b) && !IsNaN(a) && !IsNaN(b) && !CustomGreaterThanOrEqual(a, b);

        public static bool CustomGreaterThanOrEqual(object a, object b) => BothNotNull(a, b) && CustomEquals(a, b) || CustomGreaterThan(a, b);
        public static bool CustomLessThanOrEqual(object a, object b) => BothNotNull(a, b) && CustomEquals(a, b) || CustomLessThan(a, b);

        public static bool IsNaN(object obj)
        {
            if (obj is double)
            {
                return double.IsNaN((double)obj);
            }
            else if (obj is float)
            {
                return float.IsNaN((float)obj);
            }
            return false;
        }

        public static bool BothNotNull(object a, object b) => a != null && b != null;
    }
}
