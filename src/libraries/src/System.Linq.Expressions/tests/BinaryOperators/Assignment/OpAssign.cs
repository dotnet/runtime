// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Reflection;
using Xunit;

namespace System.Linq.Expressions.Tests
{
    public class OpAssign
    {
        [Theory]
        [MemberData(nameof(AssignAndEquivalentMethods))]
        public void AssignmentEquivalents(MethodInfo nonAssign, MethodInfo assign, Type type)
        {
            Func<Expression, Expression, Expression> withoutAssignment = (Func<Expression, Expression, Expression>)nonAssign.CreateDelegate(typeof(Func<Expression, Expression, Expression>));
            Func<Expression, Expression, Expression> withAssignment = (Func<Expression, Expression, Expression>)assign.CreateDelegate(typeof(Func<Expression, Expression, Expression>));

            foreach (object x in new[] { 0, -1, 1, 10 }.Select(i => Convert.ChangeType(i, type)))
                foreach (object y in new[] { -1, 1, 10 }.Select(i => Convert.ChangeType(i, type)))
                {
                    ConstantExpression xExp = Expression.Constant(x);
                    ConstantExpression yExp = Expression.Constant(y);
                    Expression woAssign = withoutAssignment(xExp, yExp);
                    ParameterExpression variable = Expression.Variable(type);
                    Expression initAssign = Expression.Assign(variable, xExp);
                    Expression assignment = withAssignment(variable, yExp);
                    Expression wAssign = Expression.Block(
                        new ParameterExpression[] { variable },
                        initAssign,
                        assignment
                        );
                    Assert.True(Expression.Lambda<Func<bool>>(Expression.Equal(woAssign, wAssign)).Compile()());
                    LabelTarget target = Expression.Label(type);
                    Expression wAssignReturningVariable = Expression.Block(
                        new ParameterExpression[] { variable },
                        initAssign,
                        assignment,
                        Expression.Return(target, variable),
                        Expression.Label(target, Expression.Default(type))
                        );
                    Assert.True(Expression.Lambda<Func<bool>>(Expression.Equal(woAssign, wAssignReturningVariable)).Compile()());
                }
        }

        private class Box<T>
        {
            public T Value { get; set; }
            public T this[int index]
            {
                get
                {
                    return Value;
                }
                set
                {
                    Value = value;
                }
            }
            public Box(T value)
            {
                Value = value;
            }
        }

        [Theory]
        [MemberData(nameof(AssignAndEquivalentMethods))]
        public void AssignmentEquivalentsWithMemberAccess(MethodInfo nonAssign, MethodInfo assign, Type type)
        {
            Func<Expression, Expression, Expression> withoutAssignment = (Func<Expression, Expression, Expression>)nonAssign.CreateDelegate(typeof(Func<Expression, Expression, Expression>));
            Func<Expression, Expression, Expression> withAssignment = (Func<Expression, Expression, Expression>)assign.CreateDelegate(typeof(Func<Expression, Expression, Expression>));

            foreach (object x in new[] { 0, -1, 1, 10 }.Select(i => Convert.ChangeType(i, type)))
                foreach (object y in new[] { -1, 1, 10 }.Select(i => Convert.ChangeType(i, type)))
                {
                    ConstantExpression xExp = Expression.Constant(x);
                    ConstantExpression yExp = Expression.Constant(y);
                    Expression woAssign = withoutAssignment(xExp, yExp);
                    Type boxType = typeof(Box<>).MakeGenericType(type);
                    object box = boxType.GetConstructor(new[] { type }).Invoke(new object[] { x });
                    Expression boxExp = Expression.Constant(box);
                    Expression property = Expression.Property(boxExp, boxType.GetProperty("Value"));
                    Expression assignment = withAssignment(property, yExp);
                    Assert.True(Expression.Lambda<Func<bool>>(Expression.Equal(woAssign, assignment)).Compile()());
                    LabelTarget target = Expression.Label(type);
                    box = boxType.GetConstructor(new[] { type }).Invoke(new object[] { x });
                    boxExp = Expression.Constant(box);
                    property = Expression.Property(boxExp, boxType.GetProperty("Value"));
                    assignment = withAssignment(property, yExp);
                    Expression wAssignReturningVariable = Expression.Block(
                        assignment,
                        Expression.Return(target, property),
                        Expression.Label(target, Expression.Default(type))
                        );
                    Assert.True(Expression.Lambda<Func<bool>>(Expression.Equal(woAssign, wAssignReturningVariable)).Compile()());
                }
        }

        [Theory]
        [MemberData(nameof(AssignAndEquivalentMethods))]
        public void AssignmentEquivalentsWithIndexAccess(MethodInfo nonAssign, MethodInfo assign, Type type)
        {
            Func<Expression, Expression, Expression> withoutAssignment = (Func<Expression, Expression, Expression>)nonAssign.CreateDelegate(typeof(Func<Expression, Expression, Expression>));
            Func<Expression, Expression, Expression> withAssignment = (Func<Expression, Expression, Expression>)assign.CreateDelegate(typeof(Func<Expression, Expression, Expression>));

            foreach (object x in new[] { 0, -1, 1, 10 }.Select(i => Convert.ChangeType(i, type)))
                foreach (object y in new[] { -1, 1, 10 }.Select(i => Convert.ChangeType(i, type)))
                {
                    ConstantExpression xExp = Expression.Constant(x);
                    ConstantExpression yExp = Expression.Constant(y);
                    Expression woAssign = withoutAssignment(xExp, yExp);
                    Type boxType = typeof(Box<>).MakeGenericType(type);
                    object box = boxType.GetConstructor(new[] { type }).Invoke(new object[] { x });
                    Expression boxExp = Expression.Constant(box);
                    Expression property = Expression.Property(boxExp, boxType.GetProperty("Item"), Expression.Constant(0));
                    Expression assignment = withAssignment(property, yExp);
                    Assert.True(Expression.Lambda<Func<bool>>(Expression.Equal(woAssign, assignment)).Compile()());
                    LabelTarget target = Expression.Label(type);
                    box = boxType.GetConstructor(new[] { type }).Invoke(new object[] { x });
                    boxExp = Expression.Constant(box);
                    property = Expression.Property(boxExp, boxType.GetProperty("Item"), Expression.Constant(0));
                    assignment = withAssignment(property, yExp);
                    Expression wAssignReturningVariable = Expression.Block(
                        assignment,
                        Expression.Return(target, property),
                        Expression.Label(target, Expression.Default(type))
                        );
                    Assert.True(Expression.Lambda<Func<bool>>(Expression.Equal(woAssign, wAssignReturningVariable)).Compile()());
                }
        }

        [Theory]
        [MemberData(nameof(AssignmentMethods))]
        public void AssignmentReducable(MethodInfo assign, Type type)
        {
            Func<Expression, Expression, Expression> withAssignment = (Func<Expression, Expression, Expression>)assign.CreateDelegate(typeof(Func<Expression, Expression, Expression>));

            ParameterExpression variable = Expression.Variable(type);
            Expression assignment = withAssignment(variable, Expression.Default(type));
            Assert.True(assignment.CanReduce);
            Assert.NotSame(assignment, assignment.ReduceAndCheck());
        }

        [Theory]
        [MemberData(nameof(AssignmentMethods))]
        public void CannotAssignToNonWritable(MethodInfo assign, Type type)
        {
            Func<Expression, Expression, Expression> withAssignment = (Func<Expression, Expression, Expression>)assign.CreateDelegate(typeof(Func<Expression, Expression, Expression>));

            Assert.Throws<ArgumentException>(() => withAssignment(Expression.Default(type), Expression.Default(type)));
        }

        [Theory]
        [MemberData(nameof(AssignmentMethods))]
        public void AssignmentWithMemberAccessReducable(MethodInfo assign, Type type)
        {
            Func<Expression, Expression, Expression> withAssignment = (Func<Expression, Expression, Expression>)assign.CreateDelegate(typeof(Func<Expression, Expression, Expression>));

            Type boxType = typeof(Box<>).MakeGenericType(type);
            object box = boxType.GetConstructor(new[] { type }).Invoke(new object[] { Convert.ChangeType(0, type) });
            Expression boxExp = Expression.Constant(box);
            Expression property = Expression.Property(boxExp, boxType.GetProperty("Value"));
            Expression assignment = withAssignment(property, Expression.Default(type));
            Assert.True(assignment.CanReduce);
            Assert.NotSame(assignment, assignment.ReduceAndCheck());
        }

        [Theory]
        [MemberData(nameof(AssignmentMethods))]
        public void AssignmentWithIndexAccessReducable(MethodInfo assign, Type type)
        {
            Func<Expression, Expression, Expression> withAssignment = (Func<Expression, Expression, Expression>)assign.CreateDelegate(typeof(Func<Expression, Expression, Expression>));

            Type boxType = typeof(Box<>).MakeGenericType(type);
            object box = boxType.GetConstructor(new[] { type }).Invoke(new object[] { Convert.ChangeType(0, type) });
            Expression boxExp = Expression.Constant(box);
            Expression property = Expression.Property(boxExp, boxType.GetProperty("Item"), Expression.Constant(0));
            Expression assignment = withAssignment(property, Expression.Default(type));
            Assert.True(assignment.CanReduce);
            Assert.NotSame(assignment, assignment.ReduceAndCheck());
        }


        private static class Unreadable<T>
        {
            public static T WriteOnly
            {
                set { }
            }
        }

        [Theory]
        [MemberData(nameof(AssignmentMethods))]
        public static void ThrowsOnLeftUnreadable(MethodInfo assign, Type type)
        {
            Func<Expression, Expression, Expression> withAssignment = (Func<Expression, Expression, Expression>)assign.CreateDelegate(typeof(Func<Expression, Expression, Expression>));

            Type unreadableType = typeof(Unreadable<>).MakeGenericType(type);
            Expression property = Expression.Property(null, unreadableType.GetProperty("WriteOnly"));
            Assert.Throws<ArgumentException>(() => withAssignment(property, Expression.Default(type)));
        }

        [Theory]
        [MemberData(nameof(AssignmentMethods))]
        public static void ThrowsOnRightUnreadable(MethodInfo assign, Type type)
        {
            Func<Expression, Expression, Expression> withAssignment = (Func<Expression, Expression, Expression>)assign.CreateDelegate(typeof(Func<Expression, Expression, Expression>));

            Type unreadableType = typeof(Unreadable<>).MakeGenericType(type);
            Expression property = Expression.Property(null, unreadableType.GetProperty("WriteOnly"));
            Expression variable = Expression.Variable(type);
            Assert.Throws<ArgumentException>(() => withAssignment(variable, property));
        }

        [Theory]
        [MemberData(nameof(AssignmentMethodsWithoutTypes))]
        public void ThrowIfNoSuchBinaryOperation(MethodInfo assign)
        {
            Func<Expression, Expression, Expression> withAssignment = (Func<Expression, Expression, Expression>)assign.CreateDelegate(typeof(Func<Expression, Expression, Expression>));

            ParameterExpression variable = Expression.Variable(typeof(string));
            Expression value = Expression.Default(typeof(string));
            Assert.Throws<InvalidOperationException>(() => withAssignment(variable, value));
        }

        private static IEnumerable<object[]> AssignmentMethods()
        {
            MethodInfo[] expressionMethods = typeof(Expression).GetMethods().Where(mi => mi.GetParameters().Length == 2).ToArray();
            foreach (Tuple<string, string> names in AssignAndEquivalentMethodNames(true))
                yield return new object[] { expressionMethods.First(mi => mi.Name == names.Item2), typeof(int) };
            foreach (Tuple<string, string> names in AssignAndEquivalentMethodNames(false))
                yield return new object[] { expressionMethods.First(mi => mi.Name == names.Item2), typeof(double) };
        }

        private static IEnumerable<object[]> AssignmentMethodsWithoutTypes()
        {
            MethodInfo[] expressionMethods = typeof(Expression).GetMethods().Where(mi => mi.GetParameters().Length == 2).ToArray();
            return AssignAndEquivalentMethodNames(true).Concat(AssignAndEquivalentMethodNames(false))
                .Select(i => i.Item2)
                .Distinct()
                .Select(i => new object[] { expressionMethods.First(mi => mi.Name == i) });
        }

        private static IEnumerable<object[]> AssignAndEquivalentMethods()
        {
            MethodInfo[] expressionMethods = typeof(Expression).GetMethods().Where(mi => mi.GetParameters().Length == 2).ToArray();
            foreach (Tuple<string, string> names in AssignAndEquivalentMethodNames(true))
                yield return new object[] {
                    expressionMethods.First(mi => mi.Name == names.Item1),
                    expressionMethods.First(mi => mi.Name == names.Item2),
                    typeof(int)
                };
            foreach (Tuple<string, string> names in AssignAndEquivalentMethodNames(false))
                yield return new object[] {
                    expressionMethods.First(mi => mi.Name == names.Item1),
                    expressionMethods.First(mi => mi.Name == names.Item2),
                    typeof(double)
                };
        }

        private static IEnumerable<Tuple<string, string>> AssignAndEquivalentMethodNames(bool integral)
        {
            yield return Tuple.Create("Add", "AddAssign");
            yield return Tuple.Create("AddChecked", "AddAssignChecked");
            yield return Tuple.Create("Divide", "DivideAssign");
            yield return Tuple.Create("Modulo", "ModuloAssign");
            yield return Tuple.Create("Multiply", "MultiplyAssign");
            yield return Tuple.Create("MultiplyChecked", "MultiplyAssignChecked");
            yield return Tuple.Create("Subtract", "SubtractAssign");
            yield return Tuple.Create("SubtractChecked", "SubtractAssignChecked");
            if (integral)
            {
                yield return Tuple.Create("And", "AndAssign");
                yield return Tuple.Create("ExclusiveOr", "ExclusiveOrAssign");
                yield return Tuple.Create("LeftShift", "LeftShiftAssign");
                yield return Tuple.Create("Or", "OrAssign");
                yield return Tuple.Create("RightShift", "RightShiftAssign");
            }
            else
                yield return Tuple.Create("Power", "PowerAssign");
        }
    }
}
