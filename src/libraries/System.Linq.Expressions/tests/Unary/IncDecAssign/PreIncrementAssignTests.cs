// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using Xunit;

namespace System.Linq.Expressions.Tests
{
    public class PreIncrementAssignTests : IncDecAssignTests
    {
        [Theory]
        [MemberData("Int16sAndIncrements")]
        [MemberData("NullableInt16sAndIncrements")]
        [MemberData("UInt16sAndIncrements")]
        [MemberData("NullableUInt16sAndIncrements")]
        [MemberData("Int32sAndIncrements")]
        [MemberData("NullableInt32sAndIncrements")]
        [MemberData("UInt32sAndIncrements")]
        [MemberData("NullableUInt32sAndIncrements")]
        [MemberData("Int64sAndIncrements")]
        [MemberData("NullableInt64sAndIncrements")]
        [MemberData("UInt64sAndIncrements")]
        [MemberData("NullableUInt64sAndIncrements")]
        [MemberData("DecimalsAndIncrements")]
        [MemberData("NullableDecimalsAndIncrements")]
        [MemberData("SinglesAndIncrements")]
        [MemberData("NullableSinglesAndIncrements")]
        [MemberData("DoublesAndIncrements")]
        [MemberData("NullableDoublesAndIncrements")]
        public void ReturnsCorrectValues(Type type, object value, object result)
        {
            ParameterExpression variable = Expression.Variable(type);
            BlockExpression block = Expression.Block(
                new[] { variable },
                Expression.Assign(variable, Expression.Constant(value, type)),
                Expression.PreIncrementAssign(variable)
                );
            Assert.True(Expression.Lambda<Func<bool>>(Expression.Equal(Expression.Constant(result, type), block)).Compile()());
        }

        [Theory]
        [MemberData("Int16sAndIncrements")]
        [MemberData("NullableInt16sAndIncrements")]
        [MemberData("UInt16sAndIncrements")]
        [MemberData("NullableUInt16sAndIncrements")]
        [MemberData("Int32sAndIncrements")]
        [MemberData("NullableInt32sAndIncrements")]
        [MemberData("UInt32sAndIncrements")]
        [MemberData("NullableUInt32sAndIncrements")]
        [MemberData("Int64sAndIncrements")]
        [MemberData("NullableInt64sAndIncrements")]
        [MemberData("UInt64sAndIncrements")]
        [MemberData("NullableUInt64sAndIncrements")]
        [MemberData("DecimalsAndIncrements")]
        [MemberData("NullableDecimalsAndIncrements")]
        [MemberData("SinglesAndIncrements")]
        [MemberData("NullableSinglesAndIncrements")]
        [MemberData("DoublesAndIncrements")]
        [MemberData("NullableDoublesAndIncrements")]
        public void AssignsCorrectValues(Type type, object value, object result)
        {
            ParameterExpression variable = Expression.Variable(type);
            LabelTarget target = Expression.Label(type);
            BlockExpression block = Expression.Block(
                new[] { variable },
                Expression.Assign(variable, Expression.Constant(value, type)),
                Expression.PreIncrementAssign(variable),
                Expression.Return(target, variable),
                Expression.Label(target, Expression.Default(type))
                );
            Assert.True(Expression.Lambda<Func<bool>>(Expression.Equal(Expression.Constant(result, type), block)).Compile()());
        }

        [Fact]
        public void SingleNanToNan()
        {
            TestPropertyClass<float> instance = new TestPropertyClass<float>();
            instance.TestInstance = float.NaN;
            Assert.True(float.IsNaN(
                Expression.Lambda<Func<float>>(
                    Expression.PreIncrementAssign(
                        Expression.Property(
                            Expression.Constant(instance),
                            typeof(TestPropertyClass<float>),
                            "TestInstance"
                            )
                        )
                    ).Compile()()
                ));
            Assert.True(float.IsNaN(instance.TestInstance));
        }

        [Fact]
        public void DoubleNanToNan()
        {
            TestPropertyClass<double> instance = new TestPropertyClass<double>();
            instance.TestInstance = double.NaN;
            Assert.True(double.IsNaN(
                Expression.Lambda<Func<double>>(
                    Expression.PreIncrementAssign(
                        Expression.Property(
                            Expression.Constant(instance),
                            typeof(TestPropertyClass<double>),
                            "TestInstance"
                            )
                        )
                    ).Compile()()
                ));
            Assert.True(double.IsNaN(instance.TestInstance));
        }

        [Theory]
        [MemberData("IncrementOverflowingValues")]
        public void OverflowingValuesThrow(object value)
        {
            ParameterExpression variable = Expression.Variable(value.GetType());
            Action overflow = Expression.Lambda<Action>(
                Expression.Block(
                    typeof(void),
                    new[] { variable },
                    Expression.Assign(variable, Expression.Constant(value)),
                    Expression.PreIncrementAssign(variable)
                    )
                ).Compile();
            Assert.Throws<OverflowException>(overflow);
        }

        [Theory]
        [MemberData("UnincrementableAndUndecrementableTypes")]
        public void InvalidOperandType(Type type)
        {
            ParameterExpression variable = Expression.Variable(type);
            Assert.Throws<InvalidOperationException>(() => Expression.PreIncrementAssign(variable));
        }

        [Fact]
        public void MethodCorrectResult()
        {
            ParameterExpression variable = Expression.Variable(typeof(string));
            BlockExpression block = Expression.Block(
                new[] { variable },
                Expression.Assign(variable, Expression.Constant("hello")),
                Expression.PreIncrementAssign(variable, typeof(IncDecAssignTests).GetTypeInfo().GetDeclaredMethod("SillyMethod"))
                );
            Assert.Equal("Eggplant", Expression.Lambda<Func<string>>(block).Compile()());
        }

        [Fact]
        public void MethodCorrectAssign()
        {
            ParameterExpression variable = Expression.Variable(typeof(string));
            LabelTarget target = Expression.Label(typeof(string));
            BlockExpression block = Expression.Block(
                new[] { variable },
                Expression.Assign(variable, Expression.Constant("hello")),
                Expression.PreIncrementAssign(variable, typeof(IncDecAssignTests).GetTypeInfo().GetDeclaredMethod("SillyMethod")),
                Expression.Return(target, variable),
                Expression.Label(target, Expression.Default(typeof(string)))
                );
            Assert.Equal("Eggplant", Expression.Lambda<Func<string>>(block).Compile()());
        }

        [Fact]
        public void IncorrectMethodType()
        {
            Expression variable = Expression.Variable(typeof(int));
            MethodInfo method = typeof(IncDecAssignTests).GetTypeInfo().GetDeclaredMethod("SillyMethod");
            Assert.Throws<InvalidOperationException>(() => Expression.PreIncrementAssign(variable, method));
        }

        [Fact]
        public void IncorrectMethodParameterCount()
        {
            Expression variable = Expression.Variable(typeof(string));
            MethodInfo method = typeof(object).GetTypeInfo().GetDeclaredMethod("ReferenceEquals");
            Assert.Throws<ArgumentException>(() => Expression.PreIncrementAssign(variable, method));
        }

        [Fact]
        public void IncorrectMethodReturnType()
        {
            Expression variable = Expression.Variable(typeof(int));
            MethodInfo method = typeof(IncDecAssignTests).GetTypeInfo().GetDeclaredMethod("GetString");
            Assert.Throws<ArgumentException>(() => Expression.PreIncrementAssign(variable, method));
        }

        [Fact]
        public void StaticMemberAccessCorrect()
        {
            TestPropertyClass<uint>.TestStatic = 2U;
            Assert.Equal(
                3U,
                Expression.Lambda<Func<uint>>(
                    Expression.PreIncrementAssign(
                        Expression.Property(null, typeof(TestPropertyClass<uint>), "TestStatic")
                        )
                    ).Compile()()
                );
            Assert.Equal(3U, TestPropertyClass<uint>.TestStatic);
        }

        [Fact]
        public void InstanceMemberAccessCorrect()
        {
            TestPropertyClass<int> instance = new TestPropertyClass<int>();
            instance.TestInstance = 2;
            Assert.Equal(
                3,
                Expression.Lambda<Func<int>>(
                    Expression.PreIncrementAssign(
                        Expression.Property(
                            Expression.Constant(instance),
                            typeof(TestPropertyClass<int>),
                            "TestInstance"
                            )
                        )
                    ).Compile()()
                );
            Assert.Equal(3, instance.TestInstance);
        }

        [Fact]
        public void ArrayAccessCorrect()
        {
            int[] array = new int[1];
            array[0] = 2;
            Assert.Equal(
                3,
                Expression.Lambda<Func<int>>(
                    Expression.PreIncrementAssign(
                        Expression.ArrayAccess(Expression.Constant(array), Expression.Constant(0))
                        )
                    ).Compile()()
                );
            Assert.Equal(3, array[0]);
        }

        [Fact]
        public void CanReduce()
        {
            ParameterExpression variable = Expression.Variable(typeof(int));
            UnaryExpression op = Expression.PreIncrementAssign(variable);
            Assert.True(op.CanReduce);
            Assert.NotSame(op, op.ReduceAndCheck());
        }

        [Fact]
        public void NullOperand()
        {
            Assert.Throws<ArgumentNullException>("expression", () => Expression.PreIncrementAssign(null));
        }

        [Fact]
        public void UnwritableOperand()
        {
            Assert.Throws<ArgumentException>("expression", () => Expression.PreIncrementAssign(Expression.Constant(1)));
        }

        [Fact]
        public void UnreadableOperand()
        {
            Expression value = Expression.Property(null, typeof(Unreadable<int>), "WriteOnly");
            Assert.Throws<ArgumentException>("expression", () => Expression.PreIncrementAssign(value));
        }

        [Fact]
        public void UpdateSameOperandSameNode()
        {
            UnaryExpression op = Expression.PreIncrementAssign(Expression.Variable(typeof(int)));
            Assert.Same(op, op.Update(op.Operand));
        }

        [Fact]
        public void UpdateDiffOperandDiffNode()
        {
            UnaryExpression op = Expression.PreIncrementAssign(Expression.Variable(typeof(int)));
            Assert.NotSame(op, op.Update(Expression.Variable(typeof(int))));
        }
    }
}
