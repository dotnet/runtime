// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Xunit;

namespace System.Linq.Expressions.Tests
{
    public static class InvocationTests
    {
        public delegate void X(X a);

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void SelfApplication(bool useInterpreter)
        {
            // Expression<X> f = x => {};
            Expression<X> f = Expression.Lambda<X>(Expression.Empty(), Expression.Parameter(typeof(X)));
            LambdaExpression a = Expression.Lambda(Expression.Invoke(f, f));

            a.Compile(useInterpreter).DynamicInvoke();

            ParameterExpression it = Expression.Parameter(f.Type);
            LambdaExpression b = Expression.Lambda(Expression.Invoke(Expression.Lambda(Expression.Invoke(it, it), it), f));

            b.Compile(useInterpreter).DynamicInvoke();
        }

        [Fact]
        public static void NoWriteBackToInstance()
        {
            new NoThread(false).DoTest();
            new NoThread(true).DoTest(); // This case fails
        }

        public class NoThread
        {
            private readonly bool _preferInterpretation;

            public NoThread(bool preferInterpretation)
            {
                _preferInterpretation = preferInterpretation;
            }

            public Func<NoThread, int> DoItA = (nt) =>
            {
                nt.DoItA = (nt0) => 1;
                return 0;
            };

            public Action Compile()
            {
                ConstantExpression ind0 = Expression.Constant(this);
                MemberExpression fld = Expression.PropertyOrField(ind0, "DoItA");
                BlockExpression block = Expression.Block(typeof(void), Expression.Invoke(fld, ind0));
                return Expression.Lambda<Action>(block).Compile(_preferInterpretation);
            }

            public void DoTest()
            {
                Action act = Compile();
                act();
                Assert.Equal(1, DoItA(this));
            }
        }

        private class FuncHolder
        {
            public Func<int> Function;

            public FuncHolder()
            {
                Function = () =>
                {
                    Function = () => 1;
                    return 0;
                };
            }
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void InvocationDoesNotChangeFunctionInvoked(bool useInterpreter)
        {
            FuncHolder holder = new FuncHolder();
            MemberExpression fld = Expression.Field(Expression.Constant(holder), "Function");
            InvocationExpression inv = Expression.Invoke(fld);
            Func<int> act = (Func<int>)Expression.Lambda(inv).Compile(useInterpreter);
            act();
            Assert.Equal(1, holder.Function());
        }

        [Fact]
        public static void ToStringTest()
        {
            InvocationExpression e1 = Expression.Invoke(Expression.Parameter(typeof(Action), "f"));
            Assert.Equal("Invoke(f)", e1.ToString());

            InvocationExpression e2 = Expression.Invoke(Expression.Parameter(typeof(Action<int>), "f"), Expression.Parameter(typeof(int), "x"));
            Assert.Equal("Invoke(f, x)", e2.ToString());
        }

        [Fact]
        public static void GetArguments()
        {
            VerifyGetArguments(Expression.Invoke(Expression.Default(typeof(Action))));
            VerifyGetArguments(Expression.Invoke(Expression.Default(typeof(Action<int>)), Expression.Constant(0)));
            VerifyGetArguments(
                Expression.Invoke(
                    Expression.Default(typeof(Action<int, int>)),
                    Enumerable.Range(0, 2).Select(i => Expression.Constant(i))));
            VerifyGetArguments(
                Expression.Invoke(
                    Expression.Default(typeof(Action<int, int, int>)),
                    Enumerable.Range(0, 3).Select(i => Expression.Constant(i))));
            VerifyGetArguments(
                Expression.Invoke(
                    Expression.Default(typeof(Action<int, int, int, int>)),
                    Enumerable.Range(0, 4).Select(i => Expression.Constant(i))));
            VerifyGetArguments(
                Expression.Invoke(
                    Expression.Default(typeof(Action<int, int, int, int, int>)),
                    Enumerable.Range(0, 5).Select(i => Expression.Constant(i))));
            VerifyGetArguments(
                Expression.Invoke(
                    Expression.Default(typeof(Action<int, int, int, int, int, int>)),
                    Enumerable.Range(0, 6).Select(i => Expression.Constant(i))));
            VerifyGetArguments(
                Expression.Invoke(
                    Expression.Default(typeof(Action<int, int, int, int, int, int, int>)),
                    Enumerable.Range(0, 7).Select(i => Expression.Constant(i))));
        }

        private static void VerifyGetArguments(InvocationExpression invoke)
        {
            var args = invoke.Arguments;
            Assert.Equal(args.Count, invoke.ArgumentCount);
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => invoke.GetArgument(-1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => invoke.GetArgument(args.Count));
            for (int i = 0; i != args.Count; ++i)
            {
                Assert.Same(args[i], invoke.GetArgument(i));
                Assert.Equal(i, ((ConstantExpression)invoke.GetArgument(i)).Value);
            }
        }

        [Fact]
        public static void ArgumentCountMismatchLambda()
        {
            Expression<Func<int, int, int>> adder = (x, y) => x + y;
            Assert.Throws<InvalidOperationException>(() => Expression.Invoke(adder, Expression.Constant(1)));
            Assert.Throws<InvalidOperationException>(() => Expression.Invoke(adder, Expression.Constant(1), Expression.Constant(1), Expression.Constant(1)));
        }

        [Fact]
        public static void ArgumentTypeMismatchLambda()
        {
            Expression<Func<int, int, int>> adder = (x, y) => x + y;
            AssertExtensions.Throws<ArgumentException>("arg1", () => Expression.Invoke(adder, Expression.Constant(1), Expression.Constant(1L)));
            AssertExtensions.Throws<ArgumentException>("arg0", () => Expression.Invoke(adder, Expression.Constant(1L), Expression.Constant(1)));
        }

        [Fact]
        public static void ArgumentCountMismatchDelegate()
        {
            Func<int, int, int> adder = (x, y) => x + y;
            Assert.Throws<InvalidOperationException>(() => Expression.Invoke(Expression.Constant(adder), Expression.Constant(1)));
            Assert.Throws<InvalidOperationException>(() => Expression.Invoke(Expression.Constant(adder), Expression.Constant(1), Expression.Constant(1), Expression.Constant(1)));
        }

        [Fact]
        public static void ArgumentTypeMismatchDelegate()
        {
            Func<int, int, int> adder = (x, y) => x + y;
            AssertExtensions.Throws<ArgumentException>("arg1", () => Expression.Invoke(Expression.Constant(adder), Expression.Constant(1), Expression.Constant(1L)));
            AssertExtensions.Throws<ArgumentException>("arg0", () => Expression.Invoke(Expression.Constant(adder), Expression.Constant(1L), Expression.Constant(1)));
        }

        [Fact]
        public static void UpdateSameReturnsSame()
        {
            Expression<Func<int, int, int>> adder = (x, y) => x + y;
            Expression lhs = Expression.Constant(1);
            Expression rhs = Expression.Constant(2);
            var invoke = Expression.Invoke(adder, lhs, rhs);
            Assert.Same(invoke, invoke.Update(adder, new[] {lhs, rhs}));
        }

        [Fact]
        public static void UpdateDifferentLambdaReturnsDifferent()
        {
            Expression<Func<int, int, int>> adder = (x, y) => x + y;
            Expression lhs = Expression.Constant(1);
            Expression rhs = Expression.Constant(2);
            var invoke = Expression.Invoke(adder, lhs, rhs);
            adder = (x, y) => y + x;
            Assert.NotSame(invoke, invoke.Update(adder, new[] { lhs, rhs }));
        }

        [Fact]
        public static void UpdateDifferentArgReturnsDifferent()
        {
            Expression<Func<int, int, int>> adder = (x, y) => x + y;
            Expression lhs = Expression.Constant(1);
            Expression rhs = Expression.Constant(2);
            var invoke = Expression.Invoke(adder, lhs, rhs);
            Assert.NotSame(invoke, invoke.Update(adder, new[] { lhs, Expression.Constant(2) }));
        }

        private static IEnumerable<object[]> InvocationExpressions()
        {
            for (int i = 0; i <= 6; ++i)
            {
                Type[] genArgs = Enumerable.Repeat(typeof(int), i + 1).ToArray();
                Type delegateType = Expression.GetFuncType(genArgs);

                ParameterExpression instance = Expression.Parameter(delegateType);

                yield return new object[] {Expression.Invoke(instance, Enumerable.Repeat(Expression.Constant(0), i))};
            }
        }

        private class ParameterChangingVisitor : ExpressionVisitor
        {
            protected override Expression VisitParameter(ParameterExpression node) => Expression.Parameter(node.Type);
        }

        private class ParameterAndConstantChangingVisitor : ParameterChangingVisitor
        {
            protected override Expression VisitConstant(ConstantExpression node) => Expression.Constant(node.Value, node.Type);
        }

        [Theory, MemberData(nameof(InvocationExpressions))]
        public static void LambdaChangeVisit(InvocationExpression invoke)
        {
            Assert.NotSame(invoke, new ParameterChangingVisitor().Visit(invoke));
        }

        [Theory, MemberData(nameof(InvocationExpressions))]
        public static void LambdaAndArgChangeVisit(InvocationExpression invoke)
        {
            Assert.NotSame(invoke, new ParameterAndConstantChangingVisitor().Visit(invoke));
        }

#if FEATURE_COMPILE // When we don't have FEATURE_COMPILE we don't have the Reflection.Emit used in the tests.

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void InvokePrivateDelegate(bool useInterpreter)
        {
            AssemblyBuilder assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Name"), AssemblyBuilderAccess.RunAndCollect);
            ModuleBuilder module = assembly.DefineDynamicModule("Name");
            TypeBuilder builder = module.DefineType("Type", TypeAttributes.Class | TypeAttributes.NotPublic | TypeAttributes.Sealed | TypeAttributes.AnsiClass | TypeAttributes.AutoClass, typeof(MulticastDelegate));
            builder.DefineConstructor(MethodAttributes.RTSpecialName | MethodAttributes.HideBySig | MethodAttributes.Public, CallingConventions.Standard, new[] { typeof(object), typeof(IntPtr) }).SetImplementationFlags(MethodImplAttributes.Runtime | MethodImplAttributes.Managed);
            builder.DefineMethod("Invoke", MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual, typeof(int), Type.EmptyTypes).SetImplementationFlags(MethodImplAttributes.Runtime | MethodImplAttributes.Managed);
            Type delType = builder.CreateTypeInfo();
            LambdaExpression lambda = Expression.Lambda(delType, Expression.Constant(42));
            Delegate del = lambda.Compile(useInterpreter);
            var invoke = Expression.Invoke(Expression.Constant(del));
            var invLambda = Expression.Lambda<Func<int>>(invoke);
            var invFunc = invLambda.Compile(useInterpreter);
            Assert.Equal(42, invFunc());
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void InvokePrivateDelegateTypeLambda(bool useInterpreter)
        {
            AssemblyBuilder assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Name"), AssemblyBuilderAccess.RunAndCollect);
            ModuleBuilder module = assembly.DefineDynamicModule("Name");
            TypeBuilder builder = module.DefineType("Type", TypeAttributes.Class | TypeAttributes.NotPublic | TypeAttributes.Sealed | TypeAttributes.AnsiClass | TypeAttributes.AutoClass, typeof(MulticastDelegate));
            builder.DefineConstructor(MethodAttributes.RTSpecialName | MethodAttributes.HideBySig | MethodAttributes.Public, CallingConventions.Standard, new[] { typeof(object), typeof(IntPtr) }).SetImplementationFlags(MethodImplAttributes.Runtime | MethodImplAttributes.Managed);
            builder.DefineMethod("Invoke", MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual, typeof(int), Type.EmptyTypes).SetImplementationFlags(MethodImplAttributes.Runtime | MethodImplAttributes.Managed);
            Type delType = builder.CreateTypeInfo();
            LambdaExpression lambda = Expression.Lambda(delType, Expression.Constant(42));
            var invoke = Expression.Invoke(lambda);
            var invLambda = Expression.Lambda<Func<int>>(invoke);
            var invFunc = invLambda.Compile(useInterpreter);
            Assert.Equal(42, invFunc());
        }
#endif

        private delegate void RefIntAction(ref int x);

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void InvokeByRefLambda(bool useInterpreter)
        {
            ParameterExpression refParam = Expression.Parameter(typeof(int).MakeByRefType());
            ParameterExpression param = Expression.Parameter(typeof(List<int>));
            Func<List<int>, List<int>> func = Expression.Lambda<Func<List<int>, List<int>>>(
                    Expression.Block(
                        Expression.Invoke(
                            Expression.Lambda<RefIntAction>(
                                Expression.AddAssign(refParam, Expression.Constant(2)), refParam),
                            Expression.MakeIndex(
                                param, typeof(List<int>).GetProperty("Item"), new[] {Expression.Constant(0)})), param),
                    param)
                .Compile(useInterpreter);
            List<int> list = new List<int> { 9 };
            Assert.Equal(11, func(list)[0]);
            Assert.Equal(11, list[0]);
        }
    }
}
