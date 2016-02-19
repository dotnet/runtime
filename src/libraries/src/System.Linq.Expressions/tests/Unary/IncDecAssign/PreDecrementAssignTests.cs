// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Reflection;
using Xunit;

namespace System.Linq.Expressions.Tests
{
    public class PreDecrementAssignTests : IncDecAssignTests
    {
        [Theory]
        [MemberData(nameof(Int16sAndDecrements))]
        [MemberData(nameof(NullableInt16sAndDecrements))]
        [MemberData(nameof(UInt16sAndDecrements))]
        [MemberData(nameof(NullableUInt16sAndDecrements))]
        [MemberData(nameof(Int32sAndDecrements))]
        [MemberData(nameof(NullableInt32sAndDecrements))]
        [MemberData(nameof(UInt32sAndDecrements))]
        [MemberData(nameof(NullableUInt32sAndDecrements))]
        [MemberData(nameof(Int64sAndDecrements))]
        [MemberData(nameof(NullableInt64sAndDecrements))]
        [MemberData(nameof(UInt64sAndDecrements))]
        [MemberData(nameof(NullableUInt64sAndDecrements))]
        [MemberData(nameof(DecimalsAndDecrements))]
        [MemberData(nameof(NullableDecimalsAndDecrements))]
        [MemberData(nameof(SinglesAndDecrements))]
        [MemberData(nameof(NullableSinglesAndDecrements))]
        [MemberData(nameof(DoublesAndDecrements))]
        [MemberData(nameof(NullableDoublesAndDecrements))]
        public void ReturnsCorrectValues(Type type, object value, object result)
        {
            ParameterExpression variable = Expression.Variable(type);
            BlockExpression block = Expression.Block(
                new[] { variable },
                Expression.Assign(variable, Expression.Constant(value, type)),
                Expression.PreDecrementAssign(variable)
                );
            Assert.True(Expression.Lambda<Func<bool>>(Expression.Equal(Expression.Constant(result, type), block)).Compile()());
        }

        [Theory]
        [MemberData(nameof(Int16sAndDecrements))]
        [MemberData(nameof(NullableInt16sAndDecrements))]
        [MemberData(nameof(UInt16sAndDecrements))]
        [MemberData(nameof(NullableUInt16sAndDecrements))]
        [MemberData(nameof(Int32sAndDecrements))]
        [MemberData(nameof(NullableInt32sAndDecrements))]
        [MemberData(nameof(UInt32sAndDecrements))]
        [MemberData(nameof(NullableUInt32sAndDecrements))]
        [MemberData(nameof(Int64sAndDecrements))]
        [MemberData(nameof(NullableInt64sAndDecrements))]
        [MemberData(nameof(UInt64sAndDecrements))]
        [MemberData(nameof(NullableUInt64sAndDecrements))]
        [MemberData(nameof(DecimalsAndDecrements))]
        [MemberData(nameof(NullableDecimalsAndDecrements))]
        [MemberData(nameof(SinglesAndDecrements))]
        [MemberData(nameof(NullableSinglesAndDecrements))]
        [MemberData(nameof(DoublesAndDecrements))]
        [MemberData(nameof(NullableDoublesAndDecrements))]
        public void AssignsCorrectValues(Type type, object value, object result)
        {
            ParameterExpression variable = Expression.Variable(type);
            LabelTarget target = Expression.Label(type);
            BlockExpression block = Expression.Block(
                new[] { variable },
                Expression.Assign(variable, Expression.Constant(value, type)),
                Expression.PreDecrementAssign(variable),
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
                    Expression.PreDecrementAssign(
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
                    Expression.PreDecrementAssign(
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
        [MemberData(nameof(DecrementOverflowingValues))]
        public void OverflowingValuesThrow(object value)
        {
            ParameterExpression variable = Expression.Variable(value.GetType());
            Action overflow = Expression.Lambda<Action>(
                Expression.Block(
                    typeof(void),
                    new[] { variable },
                    Expression.Assign(variable, Expression.Constant(value)),
                    Expression.PreDecrementAssign(variable)
                    )
                ).Compile();
            Assert.Throws<OverflowException>(overflow);
        }

        [Theory]
        [MemberData(nameof(UnincrementableAndUndecrementableTypes))]
        public void InvalidOperandType(Type type)
        {
            ParameterExpression variable = Expression.Variable(type);
            Assert.Throws<InvalidOperationException>(() => Expression.PreDecrementAssign(variable));
        }

        [Fact]
        public void MethodCorrectResult()
        {
            ParameterExpression variable = Expression.Variable(typeof(string));
            BlockExpression block = Expression.Block(
                new[] { variable },
                Expression.Assign(variable, Expression.Constant("hello")),
                Expression.PreDecrementAssign(variable, typeof(IncDecAssignTests).GetTypeInfo().GetDeclaredMethod("SillyMethod"))
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
                Expression.PreDecrementAssign(variable, typeof(IncDecAssignTests).GetTypeInfo().GetDeclaredMethod("SillyMethod")),
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
            Assert.Throws<InvalidOperationException>(() => Expression.PreDecrementAssign(variable, method));
        }

        [Fact]
        public void IncorrectMethodParameterCount()
        {
            Expression variable = Expression.Variable(typeof(string));
            MethodInfo method = typeof(object).GetTypeInfo().GetDeclaredMethod("ReferenceEquals");
            Assert.Throws<ArgumentException>(() => Expression.PreDecrementAssign(variable, method));
        }

        [Fact]
        public void IncorrectMethodReturnType()
        {
            Expression variable = Expression.Variable(typeof(int));
            MethodInfo method = typeof(IncDecAssignTests).GetTypeInfo().GetDeclaredMethod("GetString");
            Assert.Throws<ArgumentException>(() => Expression.PreDecrementAssign(variable, method));
        }

        [Fact]
        public void StaticMemberAccessCorrect()
        {
            TestPropertyClass<int>.TestStatic = 2;
            Assert.Equal(
                1,
                Expression.Lambda<Func<int>>(
                    Expression.PreDecrementAssign(
                        Expression.Property(null, typeof(TestPropertyClass<int>), "TestStatic")
                        )
                    ).Compile()()
                );
            Assert.Equal(1, TestPropertyClass<int>.TestStatic);
        }

        [Fact]
        public void InstanceMemberAccessCorrect()
        {
            TestPropertyClass<int> instance = new TestPropertyClass<int>();
            instance.TestInstance = 2;
            Assert.Equal(
                1,
                Expression.Lambda<Func<int>>(
                    Expression.PreDecrementAssign(
                        Expression.Property(
                            Expression.Constant(instance),
                            typeof(TestPropertyClass<int>),
                            "TestInstance"
                            )
                        )
                    ).Compile()()
                );
            Assert.Equal(1, instance.TestInstance);
        }

        [Fact]
        public void ArrayAccessCorrect()
        {
            int[] array = new int[1];
            array[0] = 2;
            Assert.Equal(
                1,
                Expression.Lambda<Func<int>>(
                    Expression.PreDecrementAssign(
                        Expression.ArrayAccess(Expression.Constant(array), Expression.Constant(0))
                        )
                    ).Compile()()
                );
            Assert.Equal(1, array[0]);
        }

        [Fact]
        public void CanReduce()
        {
            ParameterExpression variable = Expression.Variable(typeof(int));
            UnaryExpression op = Expression.PreDecrementAssign(variable);
            Assert.True(op.CanReduce);
            Assert.NotSame(op, op.ReduceAndCheck());
        }

        [Fact]
        public void NullOperand()
        {
            Assert.Throws<ArgumentNullException>("expression", () => Expression.PreDecrementAssign(null));
        }

        [Fact]
        public void UnwritableOperand()
        {
            Assert.Throws<ArgumentException>("expression", () => Expression.PreDecrementAssign(Expression.Constant(1)));
        }

        [Fact]
        public void UnreadableOperand()
        {
            Expression value = Expression.Property(null, typeof(Unreadable<int>), "WriteOnly");
            Assert.Throws<ArgumentException>("expression", () => Expression.PreDecrementAssign(value));
        }

        [Fact]
        public void UpdateSameOperandSameNode()
        {
            UnaryExpression op = Expression.PreDecrementAssign(Expression.Variable(typeof(int)));
            Assert.Same(op, op.Update(op.Operand));
        }

        [Fact]
        public void UpdateDiffOperandDiffNode()
        {
            UnaryExpression op = Expression.PreDecrementAssign(Expression.Variable(typeof(int)));
            Assert.NotSame(op, op.Update(Expression.Variable(typeof(int))));
        }
    }
}
