// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Reflection;
using Xunit;

namespace System.Linq.Expressions.Tests
{
    public class OpAssign
    {
        [Theory]
        [PerCompilationType(nameof(AssignAndEquivalentMethods))]
        public void AssignmentEquivalents(MethodInfo nonAssign, MethodInfo assign, Type type, bool useInterpreter)
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
                    Assert.True(Expression.Lambda<Func<bool>>(Expression.Equal(woAssign, wAssign)).Compile(useInterpreter)());
                    LabelTarget target = Expression.Label(type);
                    Expression wAssignReturningVariable = Expression.Block(
                        new ParameterExpression[] { variable },
                        initAssign,
                        assignment,
                        Expression.Return(target, variable),
                        Expression.Label(target, Expression.Default(type))
                        );
                    Assert.True(Expression.Lambda<Func<bool>>(Expression.Equal(woAssign, wAssignReturningVariable)).Compile(useInterpreter)());
                }
        }

        private class Box<T>
        {
            public static T StaticValue { get; set; }
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
        [PerCompilationType(nameof(AssignAndEquivalentMethods))]
        public void AssignmentEquivalentsWithMemberAccess(MethodInfo nonAssign, MethodInfo assign, Type type, bool useInterpreter)
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
                    Assert.True(Expression.Lambda<Func<bool>>(Expression.Equal(woAssign, assignment)).Compile(useInterpreter)());
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
                    Assert.True(Expression.Lambda<Func<bool>>(Expression.Equal(woAssign, wAssignReturningVariable)).Compile(useInterpreter)());
                }
        }

        [Theory, PerCompilationType(nameof(AssignAndEquivalentMethods))]
        public void AssignmentEquivalentsWithStaticMemberAccess(MethodInfo nonAssign, MethodInfo assign, Type type, bool useInterpreter)
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
                    PropertyInfo prop = boxType.GetProperty("StaticValue");
                    prop.SetValue(null, x);
                    Expression property = Expression.Property(null, prop);
                    Expression assignment = withAssignment(property, yExp);
                    Assert.True(Expression.Lambda<Func<bool>>(Expression.Equal(woAssign, assignment)).Compile(useInterpreter)());
                    prop.SetValue(null, x);
                    Expression wAssignReturningVariable = Expression.Block(
                        assignment,
                        property
                        );
                    Assert.True(Expression.Lambda<Func<bool>>(Expression.Equal(woAssign, wAssignReturningVariable)).Compile(useInterpreter)());
                }
        }
        [Theory]
        [PerCompilationType(nameof(AssignAndEquivalentMethods))]
        public void AssignmentEquivalentsWithIndexAccess(MethodInfo nonAssign, MethodInfo assign, Type type, bool useInterpreter)
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
                    Assert.True(Expression.Lambda<Func<bool>>(Expression.Equal(woAssign, assignment)).Compile(useInterpreter)());
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
                    Assert.True(Expression.Lambda<Func<bool>>(Expression.Equal(woAssign, wAssignReturningVariable)).Compile(useInterpreter)());
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

            AssertExtensions.Throws<ArgumentException>("left", () => withAssignment(Expression.Default(type), Expression.Default(type)));
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
            AssertExtensions.Throws<ArgumentException>("left", () => withAssignment(property, Expression.Default(type)));
        }

        [Theory]
        [MemberData(nameof(AssignmentMethods))]
        public static void ThrowsOnRightUnreadable(MethodInfo assign, Type type)
        {
            Func<Expression, Expression, Expression> withAssignment = (Func<Expression, Expression, Expression>)assign.CreateDelegate(typeof(Func<Expression, Expression, Expression>));

            Type unreadableType = typeof(Unreadable<>).MakeGenericType(type);
            Expression property = Expression.Property(null, unreadableType.GetProperty("WriteOnly"));
            Expression variable = Expression.Variable(type);
            AssertExtensions.Throws<ArgumentException>("right", () => withAssignment(variable, property));
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

        [Theory]
        [MemberData(nameof(ToStringData))]
        public static void ToStringTest(ExpressionType kind, string symbol, Type type)
        {
            BinaryExpression e = Expression.MakeBinary(kind, Expression.Parameter(type, "a"), Expression.Parameter(type, "b"));
            Assert.Equal($"(a {symbol} b)", e.ToString());
        }

        private static IEnumerable<object[]> ToStringData()
        {
            return ToStringDataImpl().Select(t => new object[] { t.Item1, t.Item2, t.Item3 });
        }

        private static IEnumerable<Tuple<ExpressionType, string, Type>> ToStringDataImpl()
        {
            yield return Tuple.Create(ExpressionType.AddAssign, "+=", typeof(int));
            yield return Tuple.Create(ExpressionType.AddAssignChecked, "+=", typeof(int));
            yield return Tuple.Create(ExpressionType.SubtractAssign, "-=", typeof(int));
            yield return Tuple.Create(ExpressionType.SubtractAssignChecked, "-=", typeof(int));
            yield return Tuple.Create(ExpressionType.MultiplyAssign, "*=", typeof(int));
            yield return Tuple.Create(ExpressionType.MultiplyAssignChecked, "*=", typeof(int));
            yield return Tuple.Create(ExpressionType.DivideAssign, "/=", typeof(int));
            yield return Tuple.Create(ExpressionType.ModuloAssign, "%=", typeof(int));
            yield return Tuple.Create(ExpressionType.PowerAssign, "**=", typeof(double));
            yield return Tuple.Create(ExpressionType.LeftShiftAssign, "<<=", typeof(int));
            yield return Tuple.Create(ExpressionType.RightShiftAssign, ">>=", typeof(int));
            yield return Tuple.Create(ExpressionType.AndAssign, "&=", typeof(int));
            yield return Tuple.Create(ExpressionType.AndAssign, "&&=", typeof(bool));
            yield return Tuple.Create(ExpressionType.OrAssign, "|=", typeof(int));
            yield return Tuple.Create(ExpressionType.OrAssign, "||=", typeof(bool));
            yield return Tuple.Create(ExpressionType.ExclusiveOrAssign, "^=", typeof(int));
            yield return Tuple.Create(ExpressionType.ExclusiveOrAssign, "^=", typeof(bool));
        }

        private static IEnumerable<ExpressionType> AssignExpressionTypes
        {
            get
            {
                yield return ExpressionType.AddAssign;
                yield return ExpressionType.SubtractAssign;
                yield return ExpressionType.MultiplyAssign;
                yield return ExpressionType.AddAssignChecked;
                yield return ExpressionType.SubtractAssignChecked;
                yield return ExpressionType.MultiplyAssignChecked;
                yield return ExpressionType.DivideAssign;
                yield return ExpressionType.ModuloAssign;
                yield return ExpressionType.PowerAssign;
                yield return ExpressionType.AndAssign;
                yield return ExpressionType.OrAssign;
                yield return ExpressionType.RightShiftAssign;
                yield return ExpressionType.LeftShiftAssign;
                yield return ExpressionType.ExclusiveOrAssign;
            }
        }

        private static IEnumerable<Func<Expression, Expression, MethodInfo, BinaryExpression>> AssignExpressionMethodInfoUsingFactories
        {
            get
            {
                yield return Expression.AddAssign;
                yield return Expression.SubtractAssign;
                yield return Expression.MultiplyAssign;
                yield return Expression.AddAssignChecked;
                yield return Expression.SubtractAssignChecked;
                yield return Expression.MultiplyAssignChecked;
                yield return Expression.DivideAssign;
                yield return Expression.ModuloAssign;
                yield return Expression.PowerAssign;
                yield return Expression.AndAssign;
                yield return Expression.OrAssign;
                yield return Expression.RightShiftAssign;
                yield return Expression.LeftShiftAssign;
                yield return Expression.ExclusiveOrAssign;
            }
        }

        public static IEnumerable<object[]> AssignExpressionTypesArguments
            => AssignExpressionTypes.Select(t => new object[] {t});

        public static IEnumerable<object[]> AssignExpressionMethodInfoUsingFactoriesArguments =
            AssignExpressionMethodInfoUsingFactories.Select(f => new object[] {f});

        private static IEnumerable<LambdaExpression> NonUnaryLambdas
        {
            get
            {
                yield return Expression.Lambda<Action>(Expression.Empty());
                Expression<Func<int, int, int>> exp = (x, y) => x + y;
                yield return exp;
            }
        }

        public static IEnumerable<object[]> AssignExpressionTypesAndNonUnaryLambdas =>
            AssignExpressionTypes.SelectMany(t => NonUnaryLambdas, (t, l) => new object[] {t, l});

        private static IEnumerable<LambdaExpression> NonIntegerReturnUnaryIntegerLambdas
        {
            get
            {
                ParameterExpression param = Expression.Parameter(typeof(int));
                yield return Expression.Lambda<Action<int>>(Expression.Empty(), param);
                Expression<Func<int, long>> convL = x => x;
                yield return convL;
                Expression<Func<int, string>> toString = x => x.ToString();
                yield return toString;
            }
        }

        public static IEnumerable<object[]> AssignExpressionTypesAndNonIntegerReturnUnaryIntegerLambdas
            => AssignExpressionTypes.SelectMany(t => NonIntegerReturnUnaryIntegerLambdas, (t, l) => new object[] {t, l});

        private static IEnumerable<LambdaExpression> NonIntegerTakingUnaryIntegerReturningLambda
        {
            get
            {
                Expression<Func<long, int>> fromL = x => (int)x;
                yield return fromL;
                Expression<Func<string, int>> fromS = x => x.Length;
                yield return fromS;
            }
        }

        public static IEnumerable<object[]> AssignExpressionTypesAndNonIntegerTakingUnaryIntegerReturningLambda
            =>
                AssignExpressionTypes.SelectMany(
                    t => NonIntegerTakingUnaryIntegerReturningLambda, (t, l) => new object[] {t, l});

        [Theory, MemberData(nameof(AssignExpressionTypesArguments))]
        public void CannotHaveConversionOnAssignWithoutMethod(ExpressionType type)
        {
            var lhs = Expression.Variable(typeof(int));
            var rhs = Expression.Constant(0);
            Expression<Func<int, int>> identity = x => x;
            Assert.Throws<InvalidOperationException>(() => Expression.MakeBinary(type, lhs, rhs, false, null, identity));
            Assert.Throws<InvalidOperationException>(() => Expression.MakeBinary(type, lhs, rhs, true, null, identity));
        }

        public static int FiftyNinthBear(int x, int y)
        {
            // Ensure numbers add up to 40. Then ignore that and return 59.
            if (x + y != 40) throw new ArgumentException();
            return 59;
        }

        [Theory, PerCompilationType(nameof(AssignExpressionTypesArguments))]
        public void ConvertAssignment(ExpressionType type, bool useInterpreter)
        {
            var lhs = Expression.Parameter(typeof(int));
            var rhs = Expression.Constant(25);
            Expression<Func<int, int>> doubleIt = x => 2 * x;
            var lambda = Expression.Lambda<Func<int, int>>(
                Expression.MakeBinary(type, lhs, rhs, false, GetType().GetMethod(nameof(FiftyNinthBear)), doubleIt),
                lhs
                );
            var func = lambda.Compile(useInterpreter);
            Assert.Equal(118, func(15));
        }

        [Theory, MemberData(nameof(AssignExpressionTypesAndNonUnaryLambdas))]
        public void ConversionMustBeUnary(ExpressionType type, LambdaExpression conversion)
        {
            var lhs = Expression.Parameter(typeof(int));
            var rhs = Expression.Constant(25);
            MethodInfo meth = GetType().GetMethod(nameof(FiftyNinthBear));
            AssertExtensions.Throws<ArgumentException>(
                "conversion", () => Expression.MakeBinary(type, lhs, rhs, false, meth, conversion));
        }

        [Theory, MemberData(nameof(AssignExpressionTypesAndNonIntegerReturnUnaryIntegerLambdas))]
        public void ConversionMustConvertToLHSType(ExpressionType type, LambdaExpression conversion)
        {
            var lhs = Expression.Parameter(typeof(int));
            var rhs = Expression.Constant(25);
            MethodInfo meth = GetType().GetMethod(nameof(FiftyNinthBear));
            Assert.Throws<InvalidOperationException>(() => Expression.MakeBinary(type, lhs, rhs, false, meth, conversion));
        }

        [Theory, MemberData(nameof(AssignExpressionTypesAndNonIntegerTakingUnaryIntegerReturningLambda))]
        public void ConversionMustConvertFromRHSType(ExpressionType type, LambdaExpression conversion)
        {
            var lhs = Expression.Parameter(typeof(int));
            var rhs = Expression.Constant(25);
            MethodInfo meth = GetType().GetMethod(nameof(FiftyNinthBear));
            Assert.Throws<InvalidOperationException>(() => Expression.MakeBinary(type, lhs, rhs, false, meth, conversion));
        }

        private class AddsToSomethingElse : IEquatable<AddsToSomethingElse>
        {
            public int Value { get; }

            public AddsToSomethingElse(int value)
            {
                Value = value;
            }

            public static int operator +(AddsToSomethingElse x, AddsToSomethingElse y) => x.Value + y.Value;

            public bool Equals(AddsToSomethingElse other) => Value == other?.Value;

            public override bool Equals(object obj) => Equals(obj as AddsToSomethingElse);

            public override int GetHashCode() => Value;
        }

        private static string StringAddition(int x, int y) => (x + y).ToString();

        [Fact]
        public void CannotAssignOpIfOpReturnNotAssignable()
        {
            var lhs = Expression.Parameter(typeof(AddsToSomethingElse));
            var rhs = Expression.Constant(new AddsToSomethingElse(3));
            AssertExtensions.Throws<ArgumentException>(null, () => Expression.AddAssign(lhs, rhs));
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public void CanAssignOpIfOpReturnNotAssignableButConversionFixes(bool useInterpreter)
        {
            var lhs = Expression.Parameter(typeof(AddsToSomethingElse));
            var rhs = Expression.Constant(new AddsToSomethingElse(3));
            Expression<Func<int, AddsToSomethingElse>> conversion = x => new AddsToSomethingElse(x);
            var exp = Expression.Lambda<Func<AddsToSomethingElse, AddsToSomethingElse>>(
                Expression.AddAssign(lhs, rhs, null, conversion),
                lhs
                );
            var func = exp.Compile(useInterpreter);
            Assert.Equal(new AddsToSomethingElse(10), func(new AddsToSomethingElse(7)));
        }

        [Theory, PerCompilationType(nameof(AssignExpressionTypesArguments))]
        public void ConvertOpAssignToMember(ExpressionType type, bool useInterpreter)
        {
            Box<int> box = new Box<int>(25);
            Expression<Func<int, int>> doubleIt = x => x * 2;
            var exp = Expression.Lambda<Func<int>>(
                Expression.MakeBinary(
                    type,
                    Expression.Property(Expression.Constant(box), "Value"),
                    Expression.Constant(15),
                    false,
                    GetType().GetMethod(nameof(FiftyNinthBear)),
                    doubleIt
                    )
                );
            var act = exp.Compile(useInterpreter);
            Assert.Equal(118, act());
            Assert.Equal(118, box.Value);
        }

        [Theory, PerCompilationType(nameof(AssignExpressionTypesArguments))]
        public void ConvertOpAssignToArrayIndex(ExpressionType type, bool useInterpreter)
        {
            int[] array = {0, 0, 25, 0};
            Expression<Func<int, int>> doubleIt = x => x * 2;
            var exp = Expression.Lambda<Func<int>>(
                Expression.MakeBinary(
                    type,
                    Expression.ArrayAccess(Expression.Constant(array), Expression.Constant(2)),
                    Expression.Constant(15),
                    false,
                    GetType().GetMethod(nameof(FiftyNinthBear)),
                    doubleIt
                    )
                );
            var act = exp.Compile(useInterpreter);
            Assert.Equal(118, act());
            Assert.Equal(118, array[2]);
        }

        private delegate int ByRefInts(ref int x, int y);

        private delegate int BothByRefInts(ref int x, ref int y);

        [Theory, PerCompilationType(nameof(AssignExpressionMethodInfoUsingFactoriesArguments))]
        public void MethodNoConvertOpWriteByRefParameter(Func<Expression, Expression, MethodInfo, BinaryExpression> factory, bool useInterpreter)
        {
            var pX = Expression.Parameter(typeof(int).MakeByRefType());
            var pY = Expression.Parameter(typeof(int));
            var exp = Expression.Lambda<ByRefInts>(factory(pX, pY, GetType().GetMethod(nameof(FiftyNinthBear))), pX, pY);
            var del = exp.Compile(useInterpreter);
            int arg = 5;
            Assert.Equal(59, del(ref arg, 35));
            Assert.Equal(59, arg);
        }

        private delegate AddsToSomethingElse ByRefSomeElse(ref AddsToSomethingElse x, AddsToSomethingElse y);

        [Theory, ClassData(typeof(CompilationTypes))]
        public void ConvertOpWriteByRefParameterOverloadedOperator(bool useInterpreter)
        {
            var pX = Expression.Parameter(typeof(AddsToSomethingElse).MakeByRefType());
            var pY = Expression.Parameter(typeof(AddsToSomethingElse));
            Expression<Func<int, AddsToSomethingElse>> conv = x => new AddsToSomethingElse(x);
            var exp = Expression.Lambda<ByRefSomeElse>(Expression.AddAssign(pX, pY, null, conv), pX, pY);
            var del = exp.Compile(useInterpreter);
            AddsToSomethingElse arg = new AddsToSomethingElse(5);
            AddsToSomethingElse result = del(ref arg, new AddsToSomethingElse(35));
            Assert.Equal(result, arg);
        }
    }
}
