// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Reflection;
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

            public BaseClass BaseClassProperty { get; set; }

            public int Int32Property { get; set; }
            public int Int32Field;
            public readonly int ReadonlyInt32Field;
            public int ReadonlyInt32Property { get { return 42; } }
            public static int StaticInt32Property1 { get; set; }
            public static int StaticInt32Field;
            public static readonly int ReadonlyStaticInt32Field;
            public static int ReadonlyStaticInt32Property { get { return 321; } }
            public const int ConstantInt32 = 12;
            public int WriteOnlyInt32 { set { } }
            public static int WriteOnlyStaticInt32 { set { } }

            private int _underlyingIndexerField1;
            public int this[int i]
            {
                get { return _underlyingIndexerField1; }
                set
                {
                    Assert.Equal(1, i);
                    _underlyingIndexerField1 = value;
                }
            }

            private int _underlyingIndexerField2;
            public int this[int i, int j]
            {
                get { return _underlyingIndexerField2; }
                set
                {
                    Assert.Equal(1, i);
                    Assert.Equal(2, j);
                    _underlyingIndexerField2 = value;
                }
            }

            public static int StaticInt32Property2 { get; set; }

#pragma warning restore 649
        }

        private class ReadOnlyIndexer
        {
            public int this[int i] { get { return 0; } }
        }

        private class WriteOnlyIndexer
        {
            public int this[int i] { set { } }
        }

        private struct StructWithPropertiesAndFields
        {
            private int _underlyingIndexerField;
            public int this[int i]
            {
                get { return _underlyingIndexerField; }
                set
                {
                    Assert.Equal(1, i);
                    _underlyingIndexerField = value;
                }
            }
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
            yield return new object[] { Expression.Constant(1) };
            yield return new object[] { Expression.Property(Expression.Constant(new ReadOnlyIndexer()), "Item", new Expression[] { Expression.Constant(1) }) };
        }

        private static IEnumerable<object> WriteOnlyExpressions()
        {
            Expression obj = Expression.Constant(new PropertyAndFields());
            yield return new object[] { Expression.Property(obj, typeof(PropertyAndFields), nameof(PropertyAndFields.WriteOnlyInt32)) };
            yield return new object[] { Expression.Property(obj, typeof(PropertyAndFields), nameof(PropertyAndFields.WriteOnlyString)) };
            yield return new object[] { Expression.Property(null, typeof(PropertyAndFields), nameof(PropertyAndFields.WriteOnlyStaticInt32)) };
            yield return new object[] { Expression.Property(null, typeof(PropertyAndFields), nameof(PropertyAndFields.WriteOnlyStaticString)) };
            yield return new object[] { Expression.Property(Expression.Constant(new WriteOnlyIndexer()), "Item", new Expression[] { Expression.Constant(1) }) };
        }

        private static IEnumerable<object> MemberAssignments()
        {
            Expression obj = Expression.Constant(new PropertyAndFields());
            yield return new object[] { Expression.Field(null, typeof(PropertyAndFields), nameof(PropertyAndFields.StaticInt32Field)), 1 };
            yield return new object[] { Expression.Property(null, typeof(PropertyAndFields), nameof(PropertyAndFields.StaticInt32Property1)), 2 };
            yield return new object[] { Expression.Field(obj, typeof(PropertyAndFields), nameof(PropertyAndFields.Int32Field)), 3 };
            yield return new object[] { Expression.Property(obj, typeof(PropertyAndFields), nameof(PropertyAndFields.Int32Property)), 4 };
            yield return new object[] { Expression.Field(null, typeof(PropertyAndFields), nameof(PropertyAndFields.StaticStringField)), "a" };
            yield return new object[] { Expression.Property(null, typeof(PropertyAndFields), nameof(PropertyAndFields.StaticStringProperty)), "b" };
            yield return new object[] { Expression.Field(obj, typeof(PropertyAndFields), nameof(PropertyAndFields.StringField)), "c" };
            yield return new object[] { Expression.Property(obj, typeof(PropertyAndFields), nameof(PropertyAndFields.StringProperty)), "d" };

            yield return new object[] { Expression.Property(obj, typeof(PropertyAndFields), nameof(PropertyAndFields.BaseClassProperty)), new BaseClass() };
            yield return new object[] { Expression.Property(obj, typeof(PropertyAndFields), nameof(PropertyAndFields.BaseClassProperty)), new SubClass() };

            // IndexExpression for indexed property
            PropertyInfo simpleIndexer = typeof(PropertyAndFields).GetProperties().First(prop => prop.GetIndexParameters().Length == 1);
            yield return new object[] { Expression.Property(obj, simpleIndexer, new Expression[] { Expression.Constant(1) }), 5 };

            PropertyInfo advancedIndexer = typeof(PropertyAndFields).GetProperties().First(prop => prop.GetIndexParameters().Length == 2);
            yield return new object[] { Expression.Property(obj, advancedIndexer, new Expression[] { Expression.Constant(1), Expression.Constant(2) }), 5 };

            // IndexExpression for non-indexed property
            yield return new object[] { Expression.Property(null, typeof(PropertyAndFields).GetProperty(nameof(PropertyAndFields.StaticInt32Property2)), new Expression[0]), 6 };
            yield return new object[] { Expression.Property(obj, nameof(PropertyAndFields.Int32Property), new Expression[0]), 7 };
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
        public void LeftNull_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>("left", () => Expression.Assign(null, Expression.Constant("")));
        }

        [Fact]
        public void RightNull_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>("right", () => Expression.Assign(Expression.Variable(typeof(int)), null));
        }

        [Theory]
        [InlineData(typeof(int), "Hello", typeof(string))]
        [InlineData(typeof(long), 1, typeof(int))]
        [InlineData(typeof(int?), 1, typeof(int))]
        [InlineData(typeof(SubClass), null, typeof(BaseClass))]
        [InlineData(typeof(int), null, typeof(BaseClass))]
        [InlineData(typeof(BaseClass), 1, typeof(int))]
        public void MismatchTypes(Type variableType, object constantValue, Type constantType)
        {
            Assert.Throws<ArgumentException>(null, () => Expression.Assign(Expression.Variable(variableType), Expression.Constant(constantValue, constantType)));
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

        [Theory]
        [MemberData(nameof(ReadOnlyExpressions))]
        public void LeftReadOnly_ThrowsArgumentException(Expression readonlyExp)
        {
            Assert.Throws<ArgumentException>("left", () => Expression.Assign(readonlyExp, Expression.Default(readonlyExp.Type)));
        }

        [Theory]
        [MemberData(nameof(WriteOnlyExpressions))]
        public static void Right_WriteOnly_ThrowsArgumentException(Expression writeOnlyExp)
        {
            ParameterExpression variable = Expression.Variable(writeOnlyExp.Type);
            Assert.Throws<ArgumentException>("right", () => Expression.Assign(variable, writeOnlyExp));
        }

        [Theory]
        [ClassData(typeof(InvalidTypesData))]
        public static void Left_InvalidType_ThrowsArgumentException(Type type)
        {
            Expression left = new FakeExpression(ExpressionType.Parameter, type);
            Assert.Throws<ArgumentException>("left", () => Expression.Assign(left, Expression.Parameter(typeof(int))));
        }

        [Theory]
        [ClassData(typeof(InvalidTypesData))]
        public static void Right_InvalidType_ThrowsArgumentException(Type type)
        {
            Expression right = new FakeExpression(ExpressionType.Parameter, type);
            Assert.Throws<ArgumentException>("right", () => Expression.Assign(Expression.Variable(typeof(object)), right));
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void Left_ValueTypeContainsChildTryExpression_ThrowsNotSupportedExceptionOnCompilation(bool useInterpreter)
        {
            Expression tryExpression = Expression.TryFinally(
                Expression.Constant(1),
                Expression.Empty()
            );
            Expression index = Expression.Property(Expression.Constant(new StructWithPropertiesAndFields()), typeof(StructWithPropertiesAndFields).GetProperty("Item"), new Expression[] { tryExpression });

            Expression<Func<bool>> func = Expression.Lambda<Func<bool>>(
                Expression.Block(
                    Expression.Assign(index, Expression.Constant(123)),
                    Expression.Equal(index, Expression.Constant(123))
                    )
                );

            if (useInterpreter)
            {
                Assert.True(func.Compile(useInterpreter)());
            }
            else
            {
                Assert.Throws<NotSupportedException>(() => func.Compile(useInterpreter));
            }
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void Left_ReferenceTypeContainsChildTryExpression_Compiles(bool useInterpreter)
        {
            Expression tryExpression = Expression.TryFinally(
                Expression.Constant(1),
                Expression.Empty()
            );
            PropertyInfo simpleIndexer = typeof(PropertyAndFields).GetProperties().First(prop => prop.GetIndexParameters().Length == 1);
            Expression index = Expression.Property(Expression.Constant(new PropertyAndFields()), simpleIndexer, new Expression[] { tryExpression });

            Func<bool> func = Expression.Lambda<Func<bool>>(
                Expression.Block(
                    Expression.Assign(index, Expression.Constant(123)),
                    Expression.Equal(index, Expression.Constant(123))
                    )
                ).Compile(useInterpreter);
            Assert.True(func());
        }

        [Fact]
        public static void ToStringTest()
        {
            var e = Expression.Assign(Expression.Parameter(typeof(int), "a"), Expression.Parameter(typeof(int), "b"));
            Assert.Equal("(a = b)", e.ToString());
        }

        class BaseClass { }
        class SubClass : BaseClass { }
    }
}
