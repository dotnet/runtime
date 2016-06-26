// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Reflection;
using Xunit;

namespace System.Linq.Expressions.Tests
{
    [TestCaseOrderer("System.Linq.Expressions.Tests.TestOrderer", "System.Linq.Expressions.Tests")]
    public class LambdaTests
    {
        private delegate int IcosanaryInt32Func(
            int arg1,
            int arg2,
            int arg3,
            int arg4,
            int arg5,
            int arg6,
            int arg7,
            int arg8,
            int arg9,
            int arg10,
            int arg11,
            int arg12,
            int arg13,
            int arg14,
            int arg15,
            int arg16,
            int arg17,
            int arg18,
            int arg19,
            int arg20);

        [Theory, ClassData(typeof(CompilationTypes))]
        public void Lambda(bool useInterpreter)
        {
            var paramI = Expression.Parameter(typeof(int), "i");
            var paramJ = Expression.Parameter(typeof(double), "j");
            var paramK = Expression.Parameter(typeof(decimal), "k");
            var paramL = Expression.Parameter(typeof(short), "l");

            Expression lambda = (Expression<Func<int, double, decimal, int>>)((i, j, k) => i);

            Assert.Equal(typeof(Func<int, double, decimal, Func<int, double, decimal, int>>), Expression.Lambda(lambda, paramI, paramJ, paramK).Type);

            lambda = (Expression<Func<int, double, decimal, short, int>>)((i, j, k, l) => i);

            Assert.IsType<Func<int, double, decimal, short, Func<int, double, decimal, short, int>>>(Expression.Lambda(lambda, paramI, paramJ, paramK, paramL).Compile(useInterpreter));

            Assert.Equal("(i, j, k, l) => i", lambda.ToString());
        }

        // Possible issue with AOT? See https://github.com/dotnet/corefx/pull/8116/files#r61346743
        [Theory(Skip = "870811"), ClassData(typeof(CompilationTypes))]
        public void InvokeComputedLambda(bool useInterpreter)
        {
            ParameterExpression x = Expression.Parameter(typeof(int), "x");
            ParameterExpression y = Expression.Parameter(typeof(int), "y");
            Expression call = Expression.Call(null, GetType().GetMethod(nameof(ComputeLambda), BindingFlags.Static | BindingFlags.NonPublic), y);
            InvocationExpression ie = Expression.Invoke(call, x);
            Expression<Func<int, int, int>> lambda = Expression.Lambda<Func<int, int, int>>(ie, x, y);

            Func<int, int, int> d = lambda.Compile(useInterpreter);
            Assert.Equal(14, d(5, 9));
            Assert.Equal(40, d(5, 8));
        }

        private static Expression<Func<int, int>> ComputeLambda(int y)
        {
            if ((y & 1) != 0)
            {
                return x => x + y;
            }

            return x => x * y;
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public void HighArityDelegate(bool useInterpreter)
        {
            var paramList = Enumerable.Range(0, 20).Select(_ => Expression.Variable(typeof(int))).ToArray();
            var exp = Expression.Lambda<IcosanaryInt32Func>(
                Expression.Add(
                    paramList[0],
                    Expression.Add(
                        paramList[1],
                        Expression.Add(
                            paramList[2],
                            Expression.Add(
                                paramList[3],
                                Expression.Add(
                                    paramList[4],
                                    Expression.Add(
                                        paramList[5],
                                        Expression.Add(
                                            paramList[6],
                                            Expression.Add(
                                                paramList[7],
                                                Expression.Add(
                                                    paramList[8],
                                                    Expression.Add(
                                                        paramList[9],
                                                        Expression.Add(
                                                            paramList[10],
                                                            Expression.Add(
                                                                paramList[11],
                                                                Expression.Add(
                                                                    paramList[12],
                                                                    Expression.Add(
                                                                        paramList[13],
                                                                        Expression.Add(
                                                                            paramList[14],
                                                                            Expression.Add(
                                                                                paramList[15],
                                                                                Expression.Add(
                                                                                    paramList[16],
                                                                                    Expression.Add(
                                                                                        paramList[17],
                                                                                        Expression.Add(
                                                                                            paramList[18],
                                                                                            paramList[19]
                                                                                            )
                                                                                        )
                                                                                    )
                                                                                )
                                                                            )
                                                                        )
                                                                    )
                                                                )
                                                            )
                                                        )
                                                    )
                                                )
                                            )
                                        )
                                    )
                                )
                            )
                        )
                    ),
                paramList
                );
            IcosanaryInt32Func f = exp.Compile(useInterpreter);
            Assert.Equal(210, f(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20));
        }

        [Fact]
        public void LambdaTypeMustBeDelegate()
        {
            Assert.Throws<ArgumentException>("TDelegate", () => Expression.Lambda<object>(Expression.Constant(0)));
            Assert.Throws<ArgumentException>("TDelegate", () => Expression.Lambda<int>(Expression.Constant(0)));
            Assert.Throws<ArgumentException>("TDelegate", () => Expression.Lambda<object>(Expression.Constant(0), true));
            Assert.Throws<ArgumentException>("TDelegate", () => Expression.Lambda<object>(Expression.Constant(0), true, Enumerable.Empty<ParameterExpression>()));
            Assert.Throws<ArgumentException>("TDelegate", () => Expression.Lambda<object>(Expression.Constant(0), "foo", Enumerable.Empty<ParameterExpression>()));
            Assert.Throws<ArgumentException>("delegateType", () => Expression.Lambda(typeof(object), Expression.Constant(0)));
            Assert.Throws<ArgumentException>("delegateType", () => Expression.Lambda(typeof(int), Expression.Constant(0)));
            Assert.Throws<ArgumentException>("delegateType", () => Expression.Lambda(typeof(object), Expression.Constant(0), true));
            Assert.Throws<ArgumentException>("delegateType", () => Expression.Lambda(typeof(object), Expression.Constant(0), true, Enumerable.Empty<ParameterExpression>()));
            Assert.Throws<ArgumentException>("delegateType", () => Expression.Lambda(typeof(object), Expression.Constant(0), "foo", Enumerable.Empty<ParameterExpression>()));

            // Note, be derived from MulticastDelegate, not merely actually MulticastDelegate or Delegate.
            Assert.Throws<ArgumentException>("TDelegate", () => Expression.Lambda<Delegate>(Expression.Constant(0), true, Enumerable.Empty<ParameterExpression>()));
            Assert.Throws<ArgumentException>("TDelegate", () => Expression.Lambda<Delegate>(Expression.Constant(0), "foo", Enumerable.Empty<ParameterExpression>()));
            Assert.Throws<ArgumentException>("delegateType", () => Expression.Lambda(typeof(Delegate), Expression.Constant(0)));
            Assert.Throws<ArgumentException>("delegateType", () => Expression.Lambda(typeof(Delegate), Expression.Constant(0), true));

            Assert.Throws<ArgumentException>("TDelegate", () => Expression.Lambda<MulticastDelegate>(Expression.Constant(0), true, Enumerable.Empty<ParameterExpression>()));
            Assert.Throws<ArgumentException>("TDelegate", () => Expression.Lambda<MulticastDelegate>(Expression.Constant(0), "foo", Enumerable.Empty<ParameterExpression>()));
            Assert.Throws<ArgumentException>("delegateType", () => Expression.Lambda(typeof(MulticastDelegate), Expression.Constant(0)));
            Assert.Throws<ArgumentException>("delegateType", () => Expression.Lambda(typeof(MulticastDelegate), Expression.Constant(0), true));
        }

        [Fact]
        public void NullLambdaBody()
        {
            Assert.Throws<ArgumentNullException>("body", () => Expression.Lambda<Func<int, int>>(null));
            Assert.Throws<ArgumentNullException>("body", () => Expression.Lambda<Func<int, int>>(null, true));
            Assert.Throws<ArgumentNullException>("body", () => Expression.Lambda<Func<int, int>>(null, true, Enumerable.Empty<ParameterExpression>()));
            Assert.Throws<ArgumentNullException>("body", () => Expression.Lambda<Func<int, int>>(null, "foo", Enumerable.Empty<ParameterExpression>()));
            Assert.Throws<ArgumentNullException>("body", () => Expression.Lambda(typeof(Func<int, int>), null));
            Assert.Throws<ArgumentNullException>("body", () => Expression.Lambda(typeof(Func<int, int>), null, true));
            Assert.Throws<ArgumentNullException>("body", () => Expression.Lambda(typeof(Func<int, int>), null, true, Enumerable.Empty<ParameterExpression>()));
            Assert.Throws<ArgumentNullException>("body", () => Expression.Lambda(typeof(Func<int, int>), null, "foo", Enumerable.Empty<ParameterExpression>()));
        }

        [Fact]
        public void NullParameters()
        {
            Assert.Empty(Expression.Lambda<Func<int>>(Expression.Constant(0), default(ParameterExpression[])).Parameters);
            Assert.Empty(Expression.Lambda<Func<int>>(Expression.Constant(0), true, default(ParameterExpression[])).Parameters);
            Assert.Empty(Expression.Lambda<Func<int>>(Expression.Constant(0), true, default(IEnumerable<ParameterExpression>)).Parameters);
            Assert.Empty(Expression.Lambda<Func<int>>(Expression.Constant(0), "foo", default(IEnumerable<ParameterExpression>)).Parameters);
            Assert.Empty(Expression.Lambda(typeof(Func<int>), Expression.Constant(0), default(ParameterExpression[])).Parameters);
            Assert.Empty(Expression.Lambda(typeof(Func<int>), Expression.Constant(0), true, default(ParameterExpression[])).Parameters);
            Assert.Empty(Expression.Lambda(typeof(Func<int>), Expression.Constant(0), true, default(IEnumerable<ParameterExpression>)).Parameters);
            Assert.Empty(Expression.Lambda(typeof(Func<int>), Expression.Constant(0), "foo", default(IEnumerable<ParameterExpression>)).Parameters);
        }

        [Fact]
        public void NullParameter()
        {
            Assert.Throws<ArgumentNullException>("parameters", () => Expression.Lambda<Func<int, int>>(Expression.Constant(0), default(ParameterExpression)));
            Assert.Throws<ArgumentNullException>("parameters", () => Expression.Lambda<Func<int, int>>(Expression.Constant(0), true, default(ParameterExpression)));
            Assert.Throws<ArgumentNullException>("parameters", () => Expression.Lambda<Func<int, int>>(Expression.Constant(0), true, Enumerable.Repeat(default(ParameterExpression), 1)));
            Assert.Throws<ArgumentNullException>("parameters", () => Expression.Lambda<Func<int, int>>(Expression.Constant(0), "foo", Enumerable.Repeat(default(ParameterExpression), 1)));
            Assert.Throws<ArgumentNullException>("parameters", () => Expression.Lambda(typeof(Func<int, int>), Expression.Constant(0), default(ParameterExpression)));
            Assert.Throws<ArgumentNullException>("parameters", () => Expression.Lambda(typeof(Func<int, int>), Expression.Constant(0), true, default(ParameterExpression)));
            Assert.Throws<ArgumentNullException>("parameters", () => Expression.Lambda(typeof(Func<int, int>), Expression.Constant(0), true, Enumerable.Repeat(default(ParameterExpression), 1)));
            Assert.Throws<ArgumentNullException>("parameters", () => Expression.Lambda(typeof(Func<int, int>), Expression.Constant(0), "foo", Enumerable.Repeat(default(ParameterExpression), 1)));
        }

        [Fact]
        public void ExplicitlyNullNameAllowed()
        {
            Assert.Null(Expression.Lambda<Func<int>>(Expression.Constant(0), null, Enumerable.Empty<ParameterExpression>()).Name);
            Assert.Null(Expression.Lambda(Expression.Constant(0), null, false, Enumerable.Empty<ParameterExpression>()).Name);
        }

        // Order this last to give IcosanaryInt32Func a chance to have been used by other tests.
        [Fact, TestOrder(1)]
        public void ImplicitlyTyped()
        {
            var exp = Expression.Lambda(
                Expression.Empty()
                );
            Assert.IsType<Expression<Action>>(exp);
            Assert.Equal(typeof(Action), exp.Type);

            exp = Expression.Lambda(
                Expression.Constant(3)
                );
            Assert.IsType<Expression<Func<int>>>(exp);
            Assert.Equal(typeof(Func<int>), exp.Type);

            exp = Expression.Lambda(
                Expression.Empty(),
                Enumerable.Range(0, 16).Select(_ => Expression.Variable(typeof(int)))
                );
            Assert.IsType<Expression<Action<
                int, int, int, int,
                int, int, int, int,
                int, int, int, int,
                int, int, int, int>>>(exp);
            Assert.Equal(typeof(Action<
                int, int, int, int,
                int, int, int, int,
                int, int, int, int,
                int, int, int, int>), exp.Type);

            exp = Expression.Lambda(
                Expression.Constant(false),
                Enumerable.Range(0, 16).Select(_ => Expression.Variable(typeof(double)))
                );
            Assert.IsType<Expression<Func<
                double, double, double, double,
                double, double, double, double,
                double, double, double, double,
                double, double, double, double,
                bool>>>(exp);
            Assert.Equal(typeof(Func<
                double, double, double, double,
                double, double, double, double,
                double, double, double, double,
                double, double, double, double,
                bool>), exp.Type);

            var paramList = Enumerable.Range(0, 20).Select(_ => Expression.Variable(typeof(int))).ToArray();
            exp = Expression.Lambda(
                Expression.Constant(0),
                paramList
                );

            Assert.IsNotType<Expression<IcosanaryInt32Func>>(exp);
            Type delType = exp.Type;
            Assert.Equal(new[] { delType }, exp.GetType().GetGenericArguments());
            MethodInfo delMethod = delType.GetMethod("Invoke");
            Assert.Equal(delMethod.ReturnType, typeof(int));
            Assert.Equal(20, delMethod.GetParameters().Length);
            Assert.True(delMethod.GetParameters().All(p => p.ParameterType == typeof(int)));
            Assert.Same(delType, Expression.Lambda(Expression.Constant(9), paramList).Type);
            string name = delType.Name;
            int graveIndex = name.IndexOf('`');
            if (graveIndex != -1)
                name = name.Substring(0, graveIndex);
            Assert.NotEqual("Func", name);

            exp = Expression.Lambda(
                Expression.Constant(3L),
                Expression.Parameter(typeof(int).MakeByRefType())
                );
            delType = exp.Type;
            Assert.Equal(new[] { delType }, exp.GetType().GetGenericArguments());
            delMethod = delType.GetMethod("Invoke");
            Assert.Equal(delMethod.ReturnType, typeof(long));
            Assert.Equal(1, delMethod.GetParameters().Length);
            Assert.Equal(typeof(int).MakeByRefType(), delMethod.GetParameters()[0].ParameterType);
            Assert.Same(delType, Expression.Lambda(Expression.Constant(3L), Expression.Parameter(typeof(int).MakeByRefType())).Type);
        }

        [Fact]
        public void NoPreferenceCompile()
        {
            // The two compilation options are given plenty of exercise between here and elsewhere
            // Make sure the no-preference approach keeps working.

            var param = Expression.Parameter(typeof(int));
            var typedExp = Expression.Lambda<Func<int, int>>(
                Expression.Add(param, Expression.Constant(2)),
                param
                );
            Assert.Equal(5, typedExp.Compile()(3));

            var exp = Expression.Lambda(
                Expression.Add(param, Expression.Constant(7)),
                param
                );
            Assert.Equal(19, exp.Compile().DynamicInvoke(12));
        }

        [Fact]
        public void DuplicateParameters()
        {
            var param = Expression.Parameter(typeof(int));
            Assert.Throws<ArgumentException>("parameters[1]", () => Expression.Lambda(Expression.Empty(), false, param, param));
            Assert.Throws<ArgumentException>("parameters[1]",
                () => Expression.Lambda<Func<int, int, int>>(Expression.Constant(0), false, param, param));
        }

        [Fact]
        public void IncorrectArgumentCount()
        {
            Assert.Throws<ArgumentException>(null,
                () => Expression.Lambda<Action>(Expression.Empty(), Expression.Parameter(typeof(int))));
            Assert.Throws<ArgumentException>(null,
                () => Expression.Lambda<Action<int, int>>(Expression.Empty(), "nullary or binary?", Enumerable.Empty<ParameterExpression>()));
            Assert.Throws<ArgumentException>(null,
                () => Expression.Lambda<Func<int>>(Expression.Constant(1), Expression.Parameter(typeof(int))));
            Assert.Throws<ArgumentException>(null,
                () => Expression.Lambda<Func<int, int, int>>(Expression.Constant(1), "nullary or binary?", Enumerable.Empty<ParameterExpression>()));
            Assert.Throws<ArgumentException>(null,
                () => Expression.Lambda(typeof(Action), Expression.Empty(), Expression.Parameter(typeof(int))));
            Assert.Throws<ArgumentException>(null,
                () => Expression.Lambda(typeof(Func<int, int, int>), Expression.Constant(1), "nullary or binary?", Enumerable.Empty<ParameterExpression>()));
        }

        [Fact]
        public void ByRefParameterForValueDelegateParameter()
        {
            Assert.Throws<ArgumentException>(null,
                () => Expression.Lambda<Action<int>>(Expression.Empty(), Expression.Parameter(typeof(int).MakeByRefType())));
            Assert.Throws<ArgumentException>(null,
                () => Expression.Lambda<Func<int, bool, int, string>>(
                    Expression.Constant(""),
                    Expression.Parameter(typeof(int)),
                    Expression.Parameter(typeof(bool).MakeByRefType()),
                    Expression.Parameter(typeof(int))));
            Assert.Throws<ArgumentException>(null,
                () => Expression.Lambda(typeof(Action<int>), Expression.Empty(), Expression.Parameter(typeof(int).MakeByRefType())));
            Assert.Throws<ArgumentException>(null,
                () => Expression.Lambda(
                    typeof(Func<int, bool, int, string>),
                    Expression.Constant(""),
                    Expression.Parameter(typeof(int)),
                    Expression.Parameter(typeof(bool).MakeByRefType()),
                    Expression.Parameter(typeof(int))));
        }

        [Fact]
        public void IncorrectParameterTypes()
        {
            Assert.Throws<ArgumentException>(
                () => Expression.Lambda<Action<int>>(Expression.Empty(), Expression.Parameter(typeof(long))));
            Assert.Throws<ArgumentException>(
                () => Expression.Lambda(typeof(Action<int>), Expression.Empty(), Expression.Parameter(typeof(long))));
            Assert.Throws<ArgumentException>(
                () => Expression.Lambda<Func<Uri, int>>(Expression.Constant(1), Expression.Parameter(typeof(string)))
                );
            Assert.Throws<ArgumentException>(
                () => Expression.Lambda(typeof(Func<Uri, int>), Expression.Constant(1), Expression.Parameter(typeof(string)))
                );
        }

        [Fact]
        public void IncorrectReturnTypes()
        {
            Assert.Throws<ArgumentException>(null, () => Expression.Lambda<Func<int>>(Expression.Constant(typeof(long))));
            Assert.Throws<ArgumentException>(null, () => Expression.Lambda(typeof(Func<int>), Expression.Constant(typeof(long))));
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public void AutoQuote(bool useInterpreter)
        {
            var param = Expression.Parameter(typeof(int));
            var inner = Expression.Lambda<Func<int, int>>(
                Expression.Multiply(param, Expression.Constant(2)),
                param
                );
            var outer = Expression.Lambda<Func<Expression<Func<int, int>>>>(inner);
            Assert.IsType<UnaryExpression>(outer.Body);
            Assert.Equal(ExpressionType.Quote, outer.Body.NodeType);
            var outerDel = outer.Compile(useInterpreter);
            var innerDel = outerDel().Compile(useInterpreter);
            Assert.Equal(16, innerDel(8));
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public void NestedCompile(bool useInterpreter)
        {
            var param = Expression.Parameter(typeof(int));
            var inner = Expression.Lambda<Func<int, int>>(
                Expression.Multiply(param, Expression.Constant(2)),
                param
                );
            var outer = Expression.Lambda<Func<Func<int, int>>>(inner);
            Assert.Same(inner, outer.Body);
            var outerDel = outer.Compile(useInterpreter);
            var innerDel = outerDel();
            Assert.Equal(16, innerDel(8));
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public void AnyTypeCanBeReturnedVoid(bool useInterpreter)
        {
            var act = Expression.Lambda<Action>(Expression.Constant("foo")).Compile(useInterpreter);
            act();
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public void NameNeedNotBeValidCSharpLabel(bool useInterpreter)
        {
            string name = "1, 2, 3, 4. This is not a valid C♯ label!\"'<>.\uffff";
            var exp = (Expression<Func<int>>)Expression.Lambda(Expression.Constant(21), name, Array.Empty<ParameterExpression>());
            Assert.Equal(name, exp.Name);
            Assert.Equal(21, exp.Compile(useInterpreter)());

            exp = (Expression<Func<int>>)Expression.Lambda(typeof(Func<int>), Expression.Constant(22), name, Array.Empty<ParameterExpression>());
            Assert.Equal(name, exp.Name);
            Assert.Equal(22, exp.Compile(useInterpreter)());

            exp = Expression.Lambda<Func<int>>(Expression.Constant(23), name, Array.Empty<ParameterExpression>());
            Assert.Equal(name, exp.Name);
            Assert.Equal(23, exp.Compile(useInterpreter)());
        }

        [Fact]
        public void UpdateSameReturnsSame()
        {
            var param = Expression.Parameter(typeof(int));
            var identExp = Expression.Lambda<Func<int, int>>(param, param);
            Assert.Same(identExp, identExp.Update(param, identExp.Parameters));
        }

        [Fact]
        public void UpdateDifferentBodyReturnsDifferent()
        {
            var param = Expression.Parameter(typeof(int));
            var identExp = Expression.Lambda<Func<int, int>>(param, param);
            Assert.NotSame(identExp, identExp.Update(Expression.UnaryPlus(param), identExp.Parameters));
        }

        [Fact]
        public void UpdateDifferentParamsReturnsDifferent()
        {
            var param0 = Expression.Parameter(typeof(int));
            var param1 = Expression.Parameter(typeof(int));
            var add = Expression.Lambda<Func<int, int, int>>(
                Expression.Add(param0, param1),
                param0,
                param1
                );
            Assert.NotSame(add, add.Update(add.Body, new[] {param1, param0}));
        }

        [Fact]
        public void UpdateLeavesTailCallAsIs()
        {
            var lambda = (Expression<Func<int>>)Expression.Lambda(Expression.Constant(1), true);
            lambda = lambda.Update(Expression.Constant(2), lambda.Parameters);
            Assert.True(lambda.TailCall);

            lambda = (Expression<Func<int>>)Expression.Lambda(typeof(Func<int>), Expression.Constant(3), Enumerable.Empty<ParameterExpression>());
            lambda = lambda.Update(Expression.Constant(4), lambda.Parameters);
            Assert.False(lambda.TailCall);
        }
    }
}
