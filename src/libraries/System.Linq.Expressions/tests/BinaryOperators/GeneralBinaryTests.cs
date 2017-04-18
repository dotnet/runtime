// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Reflection;
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

        public static IEnumerable<object[]> NonConversionBinaryTypesAndValues
        {
            get
            {
                yield return new object[] {ExpressionType.Add, 0, 0};
                yield return new object[] {ExpressionType.AddChecked, 0, 0};
                yield return new object[] {ExpressionType.Subtract, 0, 0};
                yield return new object[] {ExpressionType.SubtractChecked, 0, 0};
                yield return new object[] {ExpressionType.Multiply, 0, 0};
                yield return new object[] {ExpressionType.MultiplyChecked, 0, 0};
                yield return new object[] {ExpressionType.Divide, 0, 1};
                yield return new object[] {ExpressionType.Modulo, 0, 2};
                yield return new object[] {ExpressionType.Power, 1.0, 1.0};
                yield return new object[] {ExpressionType.And, 0, 0};
                yield return new object[] {ExpressionType.AndAlso, false, false};
                yield return new object[] {ExpressionType.Or, 0, 0};
                yield return new object[] {ExpressionType.OrElse, false, false};
                yield return new object[] {ExpressionType.LessThan, 0, 0};
                yield return new object[] {ExpressionType.LessThanOrEqual, 0, 0};
                yield return new object[] {ExpressionType.GreaterThan, 0, 0};
                yield return new object[] {ExpressionType.GreaterThanOrEqual, 0, 0};
                yield return new object[] {ExpressionType.Equal, 0, 0};
                yield return new object[] {ExpressionType.NotEqual, 0, 0};
                yield return new object[] {ExpressionType.ExclusiveOr, false, false};
                yield return new object[] {ExpressionType.ArrayIndex, new int[1], 0};
                yield return new object[] {ExpressionType.RightShift, 0, 0};
                yield return new object[] {ExpressionType.LeftShift, 0, 0};
            }
        }

        public static IEnumerable<object[]> NumericMethodAllowedBinaryTypesAndValues
        {
            get
            {
                yield return new object[] {ExpressionType.Add};
                yield return new object[] {ExpressionType.AddChecked};
                yield return new object[] {ExpressionType.AddAssign};
                yield return new object[] {ExpressionType.AddAssignChecked};
                yield return new object[] {ExpressionType.Subtract};
                yield return new object[] {ExpressionType.SubtractChecked};
                yield return new object[] {ExpressionType.SubtractAssign};
                yield return new object[] {ExpressionType.SubtractAssignChecked};
                yield return new object[] {ExpressionType.Multiply};
                yield return new object[] {ExpressionType.MultiplyChecked};
                yield return new object[] {ExpressionType.MultiplyAssign};
                yield return new object[] {ExpressionType.MultiplyAssignChecked};
                yield return new object[] {ExpressionType.Divide};
                yield return new object[] {ExpressionType.DivideAssign};
                yield return new object[] {ExpressionType.Modulo};
                yield return new object[] {ExpressionType.ModuloAssign};
                yield return new object[] {ExpressionType.Power};
                yield return new object[] {ExpressionType.PowerAssign};
                yield return new object[] {ExpressionType.And};
                yield return new object[] {ExpressionType.AndAssign};
                yield return new object[] {ExpressionType.Or};
                yield return new object[] {ExpressionType.OrAssign};
                yield return new object[] {ExpressionType.LessThan};
                yield return new object[] {ExpressionType.LessThanOrEqual};
                yield return new object[] {ExpressionType.GreaterThan};
                yield return new object[] {ExpressionType.GreaterThanOrEqual};
                yield return new object[] {ExpressionType.Equal};
                yield return new object[] {ExpressionType.NotEqual};
                yield return new object[] {ExpressionType.ExclusiveOr};
                yield return new object[] {ExpressionType.ExclusiveOrAssign};
                yield return new object[] {ExpressionType.RightShift};
                yield return new object[] {ExpressionType.RightShiftAssign};
                yield return new object[] {ExpressionType.LeftShift};
                yield return new object[] {ExpressionType.LeftShiftAssign};
            }
        }

        public static IEnumerable<object[]> BooleanMethodAllowedBinaryTypesAndValues
        {
            get
            {
                yield return new object[] {ExpressionType.AndAlso};
                yield return new object[] {ExpressionType.OrElse};
            }
        }

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
            AssertExtensions.Throws<ArgumentException>("binaryType", () => Expression.MakeBinary(type, Expression.Constant(0), Expression.Constant(0)));
            AssertExtensions.Throws<ArgumentException>("binaryType", () => Expression.MakeBinary(type, Expression.Constant(0), Expression.Constant(0), false, null));
            AssertExtensions.Throws<ArgumentException>("binaryType", () => Expression.MakeBinary(type, Expression.Constant(0), Expression.Constant(0), false, null, null));
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

        [Theory, PerCompilationType(nameof(NonConversionBinaryTypesAndValues))]
        public static void ConversionIgnoredWhenIrrelevant(
            ExpressionType type, object lhs, object rhs, bool useInterpreter)
        {
            // The types of binary expression that can't have a converter just ignore any lambda
            // passed in. This probably shouldn't be the case (an ArgumentException would be
            // appropriate, but it would be a breaking change to stop this now.

            Expression<Action> sillyLambda =
                Expression.Lambda<Action>(Expression.Throw(Expression.Constant(new Exception())));
            BinaryExpression op = Expression.MakeBinary(
                type, Expression.Constant(lhs), Expression.Constant(rhs), false, null, sillyLambda);
            Expression.Lambda(op).Compile(useInterpreter).DynamicInvoke();
        }

        private class GenericClassWithNonGenericMethod<TClassType>
        {
            public static int DoIntStuff(int x, int y) => unchecked(x + y);

            public static GenericClassWithNonGenericMethod<TClassType> DoBooleanStuff(GenericClassWithNonGenericMethod<TClassType> x, GenericClassWithNonGenericMethod<TClassType> y) => x;

            public static bool operator true(GenericClassWithNonGenericMethod<TClassType> obj) => true;

            public static bool operator false(GenericClassWithNonGenericMethod<TClassType> obj) => false;
        }

        [Theory, PerCompilationType(nameof(NumericMethodAllowedBinaryTypesAndValues))]
        public static void MethodOfOpenGeneric(ExpressionType type, bool useInterpreter)
        {
            ParameterExpression left = Expression.Parameter(typeof(int));
            ConstantExpression right = Expression.Constant(2);
            var genType = typeof(GenericClassWithNonGenericMethod<>);
            MethodInfo method = genType.GetMethod(nameof(GenericClassWithNonGenericMethod<int>.DoIntStuff));
            AssertExtensions.Throws<ArgumentException>("method", () => Expression.MakeBinary(type, left, right, false, method));
            method = genType.MakeGenericType(genType).GetMethod(nameof(GenericClassWithNonGenericMethod<int>.DoIntStuff));
            AssertExtensions.Throws<ArgumentException>("method", () => Expression.MakeBinary(type, left, right, false, method));

            // Confirm does work when closed.
            var validType = typeof(GenericClassWithNonGenericMethod<int>);
            method = validType.GetMethod(nameof(GenericClassWithNonGenericMethod<int>.DoIntStuff));
            Expression exp = Expression.MakeBinary(type, left, right, false, method);
            Func<int, int> f = Expression.Lambda<Func<int, int>>(exp, left).Compile(useInterpreter);
            Assert.Equal(5, f(3));
        }

        [Theory, PerCompilationType(nameof(BooleanMethodAllowedBinaryTypesAndValues))]
        public static void MethodOfOpenGenericBoolean(ExpressionType type, bool useInterpreter)
        {
            GenericClassWithNonGenericMethod<bool> value = new GenericClassWithNonGenericMethod<bool>();
            ConstantExpression left = Expression.Constant(value);
            ConstantExpression right = Expression.Constant(new GenericClassWithNonGenericMethod<bool>());
            var genType = typeof(GenericClassWithNonGenericMethod<>);
            MethodInfo method = genType.GetMethod(nameof(GenericClassWithNonGenericMethod<bool>.DoBooleanStuff));
            AssertExtensions.Throws<ArgumentException>("method", () => Expression.MakeBinary(type, left, right, false, method));
            method = genType.MakeGenericType(genType).GetMethod(nameof(GenericClassWithNonGenericMethod<int>.DoIntStuff));
            AssertExtensions.Throws<ArgumentException>("method", () => Expression.MakeBinary(type, left, right, false, method));

            // Confirm does work when closed.
            var validType = typeof(GenericClassWithNonGenericMethod<bool>);
            method = validType.GetMethod(nameof(GenericClassWithNonGenericMethod<bool>.DoBooleanStuff));
            Expression exp = Expression.MakeBinary(type, left, right, false, method);
            Func<GenericClassWithNonGenericMethod<bool>> f = Expression.Lambda<Func<GenericClassWithNonGenericMethod<bool>>>(exp).Compile(useInterpreter);
            Assert.Same(value, f());
        }
    }
}
