// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Linq.Expressions.Tests
{
    public class Assign
    {
        private class PropertyAndFields
        {
#pragma warning disable 649 // Assigned through expressions.
            public string StringProperty { get; set; }
            public string StringField;
            public readonly string ReadonlyStringField;
            public string ReadonlyStringProperty { get { return ""; } }
            public static string StaticStringProperty { get; set; }
            public static string StaticStringField;
            public static readonly string ReadonlyStaticStringField;
            public static string ReadonlyStaticStringProperty { get { return ""; } }
            public const string ConstantString = "Constant";
            public string WriteOnlyString { set { } }
            public static string WriteOnlyStaticString { set { } }

            public int Int32Property { get; set; }
            public int Int32Field;
            public readonly int ReadonlyInt32Field;
            public int ReadonlyInt32Property { get { return 42; } }
            public static int StaticInt32Property { get; set; }
            public static int StaticInt32Field;
            public static readonly int ReadonlyStaticInt32Field;
            public static int ReadonlyStaticInt32Property { get { return 321; } }
            public const int ConstantInt32 = 12;
            public int WriteOnlyInt32 { set { } }
            public static int WriteOnlyStaticInt32 { set { } }
#pragma warning restore 649
        }

        private static IEnumerable<object[]> ReadOnlyExpressions()
        {
            Expression obj = Expression.Constant(new PropertyAndFields());
            yield return new object[] { Expression.Field(obj, typeof(PropertyAndFields), nameof(PropertyAndFields.ReadonlyInt32Field)) };
            yield return new object[] { Expression.Property(obj, typeof(PropertyAndFields), nameof(PropertyAndFields.ReadonlyInt32Property)) };
            yield return new object[] { Expression.Field(obj, typeof(PropertyAndFields), nameof(PropertyAndFields.ReadonlyStringField)) };
            yield return new object[] { Expression.Property(obj, typeof(PropertyAndFields), nameof(PropertyAndFields.ReadonlyStringProperty)) };
            yield return new object[] { Expression.Field(null, typeof(PropertyAndFields), nameof(PropertyAndFields.ConstantInt32)) };
            yield return new object[] { Expression.Field(null, typeof(PropertyAndFields), nameof(PropertyAndFields.ConstantString)) };
            yield return new object[] { Expression.Field(null, typeof(PropertyAndFields), nameof(PropertyAndFields.ReadonlyStaticInt32Field)) };
            yield return new object[] { Expression.Property(null, typeof(PropertyAndFields), nameof(PropertyAndFields.ReadonlyStaticInt32Property)) };
            yield return new object[] { Expression.Field(null, typeof(PropertyAndFields), nameof(PropertyAndFields.ReadonlyStaticStringField)) };
            yield return new object[] { Expression.Property(null, typeof(PropertyAndFields), nameof(PropertyAndFields.ReadonlyStaticStringProperty)) };
            yield return new object[] { Expression.Default(typeof(int)) };
            yield return new object[] { Expression.Default(typeof(string)) };
            yield return new object[] { Expression.Default(typeof(DateTime)) };
        }

        private static IEnumerable<object> WriteOnlyExpressions()
        {
            Expression obj = Expression.Constant(new PropertyAndFields());
            yield return new object[] { Expression.Property(obj, typeof(PropertyAndFields), nameof(PropertyAndFields.WriteOnlyInt32)) };
            yield return new object[] { Expression.Property(obj, typeof(PropertyAndFields), nameof(PropertyAndFields.WriteOnlyString)) };
            yield return new object[] { Expression.Property(null, typeof(PropertyAndFields), nameof(PropertyAndFields.WriteOnlyStaticInt32)) };
            yield return new object[] { Expression.Property(null, typeof(PropertyAndFields), nameof(PropertyAndFields.WriteOnlyStaticString)) };
        }

        private static IEnumerable<object> MemberAssignments()
        {
            Expression obj = Expression.Constant(new PropertyAndFields());
            yield return new object[] { Expression.Field(null, typeof(PropertyAndFields), nameof(PropertyAndFields.StaticInt32Field)), 1 };
            yield return new object[] { Expression.Property(null, typeof(PropertyAndFields), nameof(PropertyAndFields.StaticInt32Property)), 2 };
            yield return new object[] { Expression.Field(obj, typeof(PropertyAndFields), nameof(PropertyAndFields.Int32Field)), 3 };
            yield return new object[] { Expression.Property(obj, typeof(PropertyAndFields), nameof(PropertyAndFields.Int32Property)), 4 };
            yield return new object[] { Expression.Field(null, typeof(PropertyAndFields), nameof(PropertyAndFields.StaticStringField)), "a" };
            yield return new object[] { Expression.Property(null, typeof(PropertyAndFields), nameof(PropertyAndFields.StaticStringProperty)), "b" };
            yield return new object[] { Expression.Field(obj, typeof(PropertyAndFields), nameof(PropertyAndFields.StringField)), "c" };
            yield return new object[] { Expression.Property(obj, typeof(PropertyAndFields), nameof(PropertyAndFields.StringProperty)), "d" };
        }

        [Fact]
        public static void BasicAssignmentExpressionTest()
        {
            var left = Expression.Parameter(typeof(int));
            var right = Expression.Parameter(typeof(int));

            BinaryExpression actual = Expression.Assign(left, right);

            Assert.Same(left, actual.Left);
            Assert.Same(right, actual.Right);
            Assert.Null(actual.Conversion);

            Assert.False(actual.IsLifted);
            Assert.False(actual.IsLiftedToNull);

            Assert.Equal(typeof(int), actual.Type);
            Assert.Equal(ExpressionType.Assign, actual.NodeType);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public void SimpleAssignment(bool useInterpreter)
        {
            ParameterExpression variable = Expression.Variable(typeof(int));
            LabelTarget target = Expression.Label(typeof(int));
            Expression exp = Expression.Block(
                new ParameterExpression[] { variable },
                Expression.Assign(variable, Expression.Constant(42)),
                Expression.Return(target, variable),
                Expression.Label(target, Expression.Default(typeof(int)))
                );
            Assert.Equal(42, Expression.Lambda<Func<int>>(exp).Compile(useInterpreter)());
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public void AssignmentHasValueItself(bool useInterpreter)
        {
            ParameterExpression variable = Expression.Variable(typeof(int));
            Expression exp = Expression.Block(
                new ParameterExpression[] { variable },
                Expression.Assign(variable, Expression.Constant(42))
                );
            Assert.Equal(42, Expression.Lambda<Func<int>>(exp).Compile(useInterpreter)());
        }

        [Theory, PerCompilationType(nameof(MemberAssignments))]
        public void AssignToMember(Expression memberExp, object value, bool useInterpreter)
        {
            Func<bool> func = Expression.Lambda<Func<bool>>(
                Expression.Block(
                    Expression.Assign(memberExp, Expression.Constant(value)),
                    Expression.Equal(memberExp, Expression.Constant(value))
                    )
                ).Compile(useInterpreter);
            Assert.True(func());
        }

        [Fact]
        public void CannotReduce()
        {
            Expression exp = Expression.Assign(Expression.Variable(typeof(int)), Expression.Constant(0));
            Assert.False(exp.CanReduce);
            Assert.Same(exp, exp.Reduce());
            Assert.Throws<ArgumentException>(null, () => exp.ReduceAndCheck());
        }

        [Fact]
        public void ThrowsOnLeftNull()
        {
            Assert.Throws<ArgumentNullException>("left", () => Expression.Assign(null, Expression.Constant("")));
        }

        [Fact]
        public void ThrowsOnRightNull()
        {
            Assert.Throws<ArgumentNullException>("right", () => Expression.Assign(Expression.Variable(typeof(int)), null));
        }

        [Fact]
        public void LeftMustBeWritable()
        {
            Assert.Throws<ArgumentException>("left", () => Expression.Assign(Expression.Constant(0), Expression.Constant(1)));
        }

        [Fact]
        public void MismatchTypes()
        {
            Assert.Throws<ArgumentException>(null, () => Expression.Assign(Expression.Variable(typeof(int)), Expression.Constant("Hello")));
        }

        [Fact]
        public void AssignableButOnlyWithConversion()
        {
            Assert.Throws<ArgumentException>(null, () => Expression.Assign(Expression.Variable(typeof(long)), Expression.Constant(1)));
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public void ReferenceAssignable(bool useInterpreter)
        {
            ParameterExpression variable = Expression.Variable(typeof(object));
            LabelTarget target = Expression.Label(typeof(object));
            Expression exp = Expression.Block(
                new ParameterExpression[] { variable },
                Expression.Assign(variable, Expression.Constant("Hello")),
                Expression.Return(target, variable),
                Expression.Label(target, Expression.Default(typeof(object)))
                );
            Assert.Equal("Hello", Expression.Lambda<Func<object>>(exp).Compile(useInterpreter)());
        }

        [Theory, MemberData(nameof(ReadOnlyExpressions))]
        public void AttemptToAssignToNonWritable(Expression readonlyExp)
        {
            Assert.Throws<ArgumentException>("left", () => Expression.Assign(readonlyExp, Expression.Default(readonlyExp.Type)));
        }

        [Theory, MemberData(nameof(WriteOnlyExpressions))]
        public static void AttemptToAssignFromNonReadable(Expression writeOnlyExp)
        {
            ParameterExpression variable = Expression.Variable(writeOnlyExp.Type);
            Assert.Throws<ArgumentException>("right", () => Expression.Assign(variable, writeOnlyExp));
        }

        [Fact]
        public static void ToStringTest()
        {
            var e = Expression.Assign(Expression.Parameter(typeof(int), "a"), Expression.Parameter(typeof(int), "b"));
            Assert.Equal("(a = b)", e.ToString());
        }
    }
}
