// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
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
            Assert.Throws<ArgumentException>(() => Expression.MakeBinary(type, Expression.Constant(0), Expression.Constant(0)));
            Assert.Throws<ArgumentException>(() => Expression.MakeBinary(type, Expression.Constant(0), Expression.Constant(0), false, null));
            Assert.Throws<ArgumentException>(() => Expression.MakeBinary(type, Expression.Constant(0), Expression.Constant(0), false, null, null));
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
    }
}
