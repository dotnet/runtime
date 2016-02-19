// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Reflection;
using Xunit;

namespace System.Linq.Expressions.Tests
{
    public class PostIncrementAssignTests : IncDecAssignTests
    {
        [Theory]
        [MemberData(nameof(Int16sAndIncrements))]
        [MemberData(nameof(NullableInt16sAndIncrements))]
        [MemberData(nameof(UInt16sAndIncrements))]
        [MemberData(nameof(NullableUInt16sAndIncrements))]
        [MemberData(nameof(Int32sAndIncrements))]
        [MemberData(nameof(NullableInt32sAndIncrements))]
        [MemberData(nameof(UInt32sAndIncrements))]
        [MemberData(nameof(NullableUInt32sAndIncrements))]
        [MemberData(nameof(Int64sAndIncrements))]
        [MemberData(nameof(NullableInt64sAndIncrements))]
        [MemberData(nameof(UInt64sAndIncrements))]
        [MemberData(nameof(NullableUInt64sAndIncrements))]
        [MemberData(nameof(DecimalsAndIncrements))]
        [MemberData(nameof(NullableDecimalsAndIncrements))]
        [MemberData(nameof(SinglesAndIncrements))]
        [MemberData(nameof(NullableSinglesAndIncrements))]
        [MemberData(nameof(DoublesAndIncrements))]
        [MemberData(nameof(NullableDoublesAndIncrements))]
        public void ReturnsCorrectValues(Type type, object value, object _)
        {
            ParameterExpression variable = Expression.Variable(type);
            BlockExpression block = Expression.Block(
                new[] { variable },
                Expression.Assign(variable, Expression.Constant(value, type)),
                Expression.PostIncrementAssign(variable)
                );
            Assert.True(Expression.Lambda<Func<bool>>(Expression.Equal(Expression.Constant(value, type), block)).Compile()());
        }

        [Theory]
        [MemberData(nameof(Int16sAndIncrements))]
        [MemberData(nameof(NullableInt16sAndIncrements))]
        [MemberData(nameof(UInt16sAndIncrements))]
        [MemberData(nameof(NullableUInt16sAndIncrements))]
        [MemberData(nameof(Int32sAndIncrements))]
        [MemberData(nameof(NullableInt32sAndIncrements))]
        [MemberData(nameof(UInt32sAndIncrements))]
        [MemberData(nameof(NullableUInt32sAndIncrements))]
        [MemberData(nameof(Int64sAndIncrements))]
        [MemberData(nameof(NullableInt64sAndIncrements))]
        [MemberData(nameof(UInt64sAndIncrements))]
        [MemberData(nameof(NullableUInt64sAndIncrements))]
        [MemberData(nameof(DecimalsAndIncrements))]
        [MemberData(nameof(NullableDecimalsAndIncrements))]
        [MemberData(nameof(SinglesAndIncrements))]
        [MemberData(nameof(NullableSinglesAndIncrements))]
        [MemberData(nameof(DoublesAndIncrements))]
        [MemberData(nameof(NullableDoublesAndIncrements))]
        public void AssignsCorrectValues(Type type, object value, object result)
        {
            ParameterExpression variable = Expression.Variable(type);
            LabelTarget target = Expression.Label(type);
            BlockExpression block = Expression.Block(
                new[] { variable },
                Expression.Assign(variable, Expression.Constant(value, type)),
                Expression.PostIncrementAssign(variable),
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
                    Expression.PostIncrementAssign(
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
                    Expression.PostIncrementAssign(
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
        [MemberData(nameof(IncrementOverflowingValues))]
        public void OverflowingValuesThrow(object value)
        {
            ParameterExpression variable = Expression.Variable(value.GetType());
            Action overflow = Expression.Lambda<Action>(
                Expression.Block(
                    typeof(void),
                    new[] { variable },
                    Expression.Assign(variable, Expression.Constant(value)),
                    Expression.PostIncrementAssign(variable)
                    )
                ).Compile();
            Assert.Throws<OverflowException>(overflow);
        }

        [Theory]
        [MemberData(nameof(UnincrementableAndUndecrementableTypes))]
        public void InvalidOperandType(Type type)
        {
            ParameterExpression variable = Expression.Variable(type);
            Assert.Throws<InvalidOperationException>(() => Expression.PostIncrementAssign(variable));
        }

        [Fact]
        public void MethodCorrectResult()
        {
            ParameterExpression variable = Expression.Variable(typeof(string));
            BlockExpression block = Expression.Block(
                new[] { variable },
                Expression.Assign(variable, Expression.Constant("hello")),
                Expression.PostIncrementAssign(variable, typeof(IncDecAssignTests).GetTypeInfo().GetDeclaredMethod("SillyMethod"))
                );
            Assert.Equal("hello", Expression.Lambda<Func<string>>(block).Compile()());
        }

        [Fact]
        public void MethodCorrectAssign()
        {
            ParameterExpression variable = Expression.Variable(typeof(string));
            LabelTarget target = Expression.Label(typeof(string));
            BlockExpression block = Expression.Block(
                new[] { variable },
                Expression.Assign(variable, Expression.Constant("hello")),
                Expression.PostIncrementAssign(variable, typeof(IncDecAssignTests).GetTypeInfo().GetDeclaredMethod("SillyMethod")),
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
            Assert.Throws<InvalidOperationException>(() => Expression.PostIncrementAssign(variable, method));
        }

        [Fact]
        public void IncorrectMethodParameterCount()
        {
            Expression variable = Expression.Variable(typeof(string));
            MethodInfo method = typeof(object).GetTypeInfo().GetDeclaredMethod("ReferenceEquals");
            Assert.Throws<ArgumentException>(() => Expression.PostIncrementAssign(variable, method));
        }

        [Fact]
        public void IncorrectMethodReturnType()
        {
            Expression variable = Expression.Variable(typeof(int));
            MethodInfo method = typeof(IncDecAssignTests).GetTypeInfo().GetDeclaredMethod("GetString");
            Assert.Throws<ArgumentException>(() => Expression.PostIncrementAssign(variable, method));
        }

        [Fact]
        public void StaticMemberAccessCorrect()
        {
            TestPropertyClass<long>.TestStatic = 2L;
            Assert.Equal(
                2L,
                Expression.Lambda<Func<long>>(
                    Expression.PostIncrementAssign(
                        Expression.Property(null, typeof(TestPropertyClass<long>), "TestStatic")
                        )
                    ).Compile()()
                );
            Assert.Equal(3L, TestPropertyClass<long>.TestStatic);
        }

        [Fact]
        public void InstanceMemberAccessCorrect()
        {
            TestPropertyClass<int> instance = new TestPropertyClass<int>();
            instance.TestInstance = 2;
            Assert.Equal(
                2,
                Expression.Lambda<Func<int>>(
                    Expression.PostIncrementAssign(
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
                2,
                Expression.Lambda<Func<int>>(
                    Expression.PostIncrementAssign(
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
            UnaryExpression op = Expression.PostIncrementAssign(variable);
            Assert.True(op.CanReduce);
            Assert.NotSame(op, op.ReduceAndCheck());
        }

        [Fact]
        public void NullOperand()
        {
            Assert.Throws<ArgumentNullException>("expression", () => Expression.PostIncrementAssign(null));
        }

        [Fact]
        public void UnwritableOperand()
        {
            Assert.Throws<ArgumentException>("expression", () => Expression.PostIncrementAssign(Expression.Constant(1)));
        }

        [Fact]
        public void UnreadableOperand()
        {
            Expression value = Expression.Property(null, typeof(Unreadable<int>), "WriteOnly");
            Assert.Throws<ArgumentException>("expression", () => Expression.PostIncrementAssign(value));
        }

        [Fact]
        public void UpdateSameOperandSameNode()
        {
            UnaryExpression op = Expression.PostIncrementAssign(Expression.Variable(typeof(int)));
            Assert.Same(op, op.Update(op.Operand));
        }

        [Fact]
        public void UpdateDiffOperandDiffNode()
        {
            UnaryExpression op = Expression.PostIncrementAssign(Expression.Variable(typeof(int)));
            Assert.NotSame(op, op.Update(Expression.Variable(typeof(int))));
        }
    }
}
