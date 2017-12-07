// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Reflection.Emit;
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
            ParameterExpression paramI = Expression.Parameter(typeof(int), "i");
            ParameterExpression paramJ = Expression.Parameter(typeof(double), "j");
            ParameterExpression paramK = Expression.Parameter(typeof(decimal), "k");
            ParameterExpression paramL = Expression.Parameter(typeof(short), "l");

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
            ParameterExpression[] paramList = Enumerable.Range(0, 20).Select(_ => Expression.Variable(typeof(int))).ToArray();
            Expression<IcosanaryInt32Func> exp = Expression.Lambda<IcosanaryInt32Func>(
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
            AssertExtensions.Throws<ArgumentException>("TDelegate", () => Expression.Lambda<object>(Expression.Constant(0)));
            AssertExtensions.Throws<ArgumentException>("TDelegate", () => Expression.Lambda<int>(Expression.Constant(0)));
            AssertExtensions.Throws<ArgumentException>("TDelegate", () => Expression.Lambda<object>(Expression.Constant(0), true));
            AssertExtensions.Throws<ArgumentException>("TDelegate", () => Expression.Lambda<object>(Expression.Constant(0), true, Enumerable.Empty<ParameterExpression>()));
            AssertExtensions.Throws<ArgumentException>("TDelegate", () => Expression.Lambda<object>(Expression.Constant(0), "foo", Enumerable.Empty<ParameterExpression>()));
            AssertExtensions.Throws<ArgumentException>("delegateType", () => Expression.Lambda(typeof(object), Expression.Constant(0)));
            AssertExtensions.Throws<ArgumentException>("delegateType", () => Expression.Lambda(typeof(int), Expression.Constant(0)));
            AssertExtensions.Throws<ArgumentException>("delegateType", () => Expression.Lambda(typeof(object), Expression.Constant(0), true));
            AssertExtensions.Throws<ArgumentException>("delegateType", () => Expression.Lambda(typeof(object), Expression.Constant(0), true, Enumerable.Empty<ParameterExpression>()));
            AssertExtensions.Throws<ArgumentException>("delegateType", () => Expression.Lambda(typeof(object), Expression.Constant(0), "foo", Enumerable.Empty<ParameterExpression>()));

            // Note, be derived from MulticastDelegate, not merely actually MulticastDelegate or Delegate.
            AssertExtensions.Throws<ArgumentException>("TDelegate", () => Expression.Lambda<Delegate>(Expression.Constant(0), true, Enumerable.Empty<ParameterExpression>()));
            AssertExtensions.Throws<ArgumentException>("TDelegate", () => Expression.Lambda<Delegate>(Expression.Constant(0), "foo", Enumerable.Empty<ParameterExpression>()));
            AssertExtensions.Throws<ArgumentException>("delegateType", () => Expression.Lambda(typeof(Delegate), Expression.Constant(0)));
            AssertExtensions.Throws<ArgumentException>("delegateType", () => Expression.Lambda(typeof(Delegate), Expression.Constant(0), true));

            AssertExtensions.Throws<ArgumentException>("TDelegate", () => Expression.Lambda<MulticastDelegate>(Expression.Constant(0), true, Enumerable.Empty<ParameterExpression>()));
            AssertExtensions.Throws<ArgumentException>("TDelegate", () => Expression.Lambda<MulticastDelegate>(Expression.Constant(0), "foo", Enumerable.Empty<ParameterExpression>()));
            AssertExtensions.Throws<ArgumentException>("delegateType", () => Expression.Lambda(typeof(MulticastDelegate), Expression.Constant(0)));
            AssertExtensions.Throws<ArgumentException>("delegateType", () => Expression.Lambda(typeof(MulticastDelegate), Expression.Constant(0), true));
        }

        [Fact]
        public void NullLambdaBody()
        {
            AssertExtensions.Throws<ArgumentNullException>("body", () => Expression.Lambda<Func<int, int>>(null));
            AssertExtensions.Throws<ArgumentNullException>("body", () => Expression.Lambda<Func<int, int>>(null, true));
            AssertExtensions.Throws<ArgumentNullException>("body", () => Expression.Lambda<Func<int, int>>(null, true, Enumerable.Empty<ParameterExpression>()));
            AssertExtensions.Throws<ArgumentNullException>("body", () => Expression.Lambda<Func<int, int>>(null, "foo", Enumerable.Empty<ParameterExpression>()));
            AssertExtensions.Throws<ArgumentNullException>("body", () => Expression.Lambda(typeof(Func<int, int>), null));
            AssertExtensions.Throws<ArgumentNullException>("body", () => Expression.Lambda(typeof(Func<int, int>), null, true));
            AssertExtensions.Throws<ArgumentNullException>("body", () => Expression.Lambda(typeof(Func<int, int>), null, true, Enumerable.Empty<ParameterExpression>()));
            AssertExtensions.Throws<ArgumentNullException>("body", () => Expression.Lambda(typeof(Func<int, int>), null, "foo", Enumerable.Empty<ParameterExpression>()));
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
            AssertExtensions.Throws<ArgumentNullException>("parameters[0]", () => Expression.Lambda<Func<int, int>>(Expression.Constant(0), default(ParameterExpression)));
            AssertExtensions.Throws<ArgumentNullException>("parameters[0]", () => Expression.Lambda<Func<int, int>>(Expression.Constant(0), true, default(ParameterExpression)));
            AssertExtensions.Throws<ArgumentNullException>("parameters[0]", () => Expression.Lambda<Func<int, int>>(Expression.Constant(0), true, Enumerable.Repeat(default(ParameterExpression), 1)));
            AssertExtensions.Throws<ArgumentNullException>("parameters[0]", () => Expression.Lambda<Func<int, int>>(Expression.Constant(0), "foo", Enumerable.Repeat(default(ParameterExpression), 1)));
            AssertExtensions.Throws<ArgumentNullException>("parameters[0]", () => Expression.Lambda(typeof(Func<int, int>), Expression.Constant(0), default(ParameterExpression)));
            AssertExtensions.Throws<ArgumentNullException>("parameters[0]", () => Expression.Lambda(typeof(Func<int, int>), Expression.Constant(0), true, default(ParameterExpression)));
            AssertExtensions.Throws<ArgumentNullException>("parameters[0]", () => Expression.Lambda(typeof(Func<int, int>), Expression.Constant(0), true, Enumerable.Repeat(default(ParameterExpression), 1)));
            AssertExtensions.Throws<ArgumentNullException>("parameters[0]", () => Expression.Lambda(typeof(Func<int, int>), Expression.Constant(0), "foo", Enumerable.Repeat(default(ParameterExpression), 1)));
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
            LambdaExpression exp = Expression.Lambda(
                Expression.Empty()
                );
            Assert.IsAssignableFrom<Expression<Action>>(exp);
            Assert.Equal(typeof(Action), exp.Type);

            exp = Expression.Lambda(
                Expression.Constant(3)
                );
            Assert.IsAssignableFrom<Expression<Func<int>>>(exp);
            Assert.Equal(typeof(Func<int>), exp.Type);

            exp = Expression.Lambda(
                Expression.Empty(),
                Enumerable.Range(0, 16).Select(_ => Expression.Variable(typeof(int)))
                );
            Assert.IsAssignableFrom<Expression<Action<
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
            Assert.IsAssignableFrom<Expression<Func<
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

#if FEATURE_COMPILE
            // From this point on, the tests require FEATURE_COMPILE (RefEmit) support as SLE needs to create delegate types on the fly. 
            // You can't instantiate Func<> over 20 arguments or over byrefs.
            ParameterExpression[] paramList = Enumerable.Range(0, 20).Select(_ => Expression.Variable(typeof(int))).ToArray();
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
#endif //FEATURE_COMPILE
        }

        [Fact]
        public void NoPreferenceCompile()
        {
            // The two compilation options are given plenty of exercise between here and elsewhere
            // Make sure the no-preference approach keeps working.

            ParameterExpression param = Expression.Parameter(typeof(int));
            Expression<Func<int, int>> typedExp = Expression.Lambda<Func<int, int>>(
                Expression.Add(param, Expression.Constant(2)),
                param
                );
            Assert.Equal(5, typedExp.Compile()(3));

            LambdaExpression exp = Expression.Lambda(
                Expression.Add(param, Expression.Constant(7)),
                param
                );
            Assert.Equal(19, exp.Compile().DynamicInvoke(12));
        }

        [Fact]
        public void DuplicateParameters()
        {
            ParameterExpression param = Expression.Parameter(typeof(int));
            AssertExtensions.Throws<ArgumentException>("parameters[1]", () => Expression.Lambda(Expression.Empty(), false, param, param));
            AssertExtensions.Throws<ArgumentException>("parameters[1]",
                () => Expression.Lambda<Func<int, int, int>>(Expression.Constant(0), false, param, param));
        }

        [Fact]
        public void IncorrectArgumentCount()
        {
            AssertExtensions.Throws<ArgumentException>(null,
                () => Expression.Lambda<Action>(Expression.Empty(), Expression.Parameter(typeof(int))));
            AssertExtensions.Throws<ArgumentException>(null,
                () => Expression.Lambda<Action<int, int>>(Expression.Empty(), "nullary or binary?", Enumerable.Empty<ParameterExpression>()));
            AssertExtensions.Throws<ArgumentException>(null,
                () => Expression.Lambda<Func<int>>(Expression.Constant(1), Expression.Parameter(typeof(int))));
            AssertExtensions.Throws<ArgumentException>(null,
                () => Expression.Lambda<Func<int, int, int>>(Expression.Constant(1), "nullary or binary?", Enumerable.Empty<ParameterExpression>()));
            AssertExtensions.Throws<ArgumentException>(null,
                () => Expression.Lambda(typeof(Action), Expression.Empty(), Expression.Parameter(typeof(int))));
            AssertExtensions.Throws<ArgumentException>(null,
                () => Expression.Lambda(typeof(Func<int, int, int>), Expression.Constant(1), "nullary or binary?", Enumerable.Empty<ParameterExpression>()));
        }

        [Fact]
        public void ByRefParameterForValueDelegateParameter()
        {
            AssertExtensions.Throws<ArgumentException>(null,
                () => Expression.Lambda<Action<int>>(Expression.Empty(), Expression.Parameter(typeof(int).MakeByRefType())));
            AssertExtensions.Throws<ArgumentException>(null,
                () => Expression.Lambda<Func<int, bool, int, string>>(
                    Expression.Constant(""),
                    Expression.Parameter(typeof(int)),
                    Expression.Parameter(typeof(bool).MakeByRefType()),
                    Expression.Parameter(typeof(int))));
            AssertExtensions.Throws<ArgumentException>(null,
                () => Expression.Lambda(typeof(Action<int>), Expression.Empty(), Expression.Parameter(typeof(int).MakeByRefType())));
            AssertExtensions.Throws<ArgumentException>(null,
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
            AssertExtensions.Throws<ArgumentException>(null, () => Expression.Lambda<Action<int>>(Expression.Empty(), Expression.Parameter(typeof(long))));
            AssertExtensions.Throws<ArgumentException>(null, () => Expression.Lambda(typeof(Action<int>), Expression.Empty(), Expression.Parameter(typeof(long))));
            AssertExtensions.Throws<ArgumentException>(null, () => Expression.Lambda<Func<Uri, int>>(Expression.Constant(1), Expression.Parameter(typeof(string))));
            AssertExtensions.Throws<ArgumentException>(null, () => Expression.Lambda(typeof(Func<Uri, int>), Expression.Constant(1), Expression.Parameter(typeof(string))));
        }

        [Fact]
        public void IncorrectReturnTypes()
        {
            AssertExtensions.Throws<ArgumentException>(null, () => Expression.Lambda<Func<int>>(Expression.Constant(typeof(long))));
            AssertExtensions.Throws<ArgumentException>(null, () => Expression.Lambda(typeof(Func<int>), Expression.Constant(typeof(long))));
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public void AutoQuote(bool useInterpreter)
        {
            ParameterExpression param = Expression.Parameter(typeof(int));
            Expression<Func<int, int>> inner = Expression.Lambda<Func<int, int>>(
                Expression.Multiply(param, Expression.Constant(2)),
                param
                );
            Expression<Func<Expression<Func<int, int>>>> outer = Expression.Lambda<Func<Expression<Func<int, int>>>>(inner);
            Assert.IsType<UnaryExpression>(outer.Body);
            Assert.Equal(ExpressionType.Quote, outer.Body.NodeType);
            Func<Expression<Func<int, int>>> outerDel = outer.Compile(useInterpreter);
            Func<int, int> innerDel = outerDel().Compile(useInterpreter);
            Assert.Equal(16, innerDel(8));
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public void NestedCompile(bool useInterpreter)
        {
            ParameterExpression param = Expression.Parameter(typeof(int));
            Expression<Func<int, int>> inner = Expression.Lambda<Func<int, int>>(
                Expression.Multiply(param, Expression.Constant(2)),
                param
                );
            Expression<Func<Func<int, int>>> outer = Expression.Lambda<Func<Func<int, int>>>(inner);
            Assert.Same(inner, outer.Body);
            Func<Func<int, int>> outerDel = outer.Compile(useInterpreter);
            Func<int, int> innerDel = outerDel();
            Assert.Equal(16, innerDel(8));
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public void AnyTypeCanBeReturnedVoid(bool useInterpreter)
        {
            Action act = Expression.Lambda<Action>(Expression.Constant("foo")).Compile(useInterpreter);
            act();
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public void NameNeedNotBeValidCSharpLabel(bool useInterpreter)
        {
            string name = "1, 2, 3, 4. This is not a valid C\u266F label!\"'<>.\uffff";
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
            Expression body = Expression.Empty();
            ParameterExpression[] pars = Array.Empty<ParameterExpression>();
            Expression<Action> lambda0 = Expression.Lambda<Action>(body, pars);
            Assert.Same(lambda0, lambda0.Update(body, null));
            Assert.Same(lambda0, lambda0.Update(body, pars));
            pars = pars.Append(Expression.Parameter(typeof(int))).ToArray();
            Expression<Action<int>> lambda1 = Expression.Lambda<Action<int>>(body, pars);
            Assert.Same(lambda1, lambda1.Update(body, pars));
            pars = pars.Append(Expression.Parameter(typeof(int))).ToArray();
            Expression<Action<int, int>> lambda2 = Expression.Lambda<Action<int, int>>(body, pars);
            Assert.Same(lambda2, lambda2.Update(body, pars));
            pars = pars.Append(Expression.Parameter(typeof(int))).ToArray();
            Expression<Action<int, int, int>> lambda3 = Expression.Lambda<Action<int, int, int>>(body, pars);
            Assert.Same(lambda3, lambda3.Update(body, pars));
            pars = pars.Append(Expression.Parameter(typeof(int))).ToArray();
            Expression<Action<int, int, int, int>> lambda4 = Expression.Lambda<Action<int, int, int, int>>(body, pars);
            Assert.Same(lambda4, lambda4.Update(body, pars));
        }

        [Fact]
        public void UpdateDoesntRepeatEnumeration()
        {
            Expression body = Expression.Empty();
            ParameterExpression[] pars = Array.Empty<ParameterExpression>();
            Expression<Action> lambda0 = Expression.Lambda<Action>(body, pars);
            Assert.Same(lambda0, lambda0.Update(body, new RunOnceEnumerable<ParameterExpression>(pars)));
            pars = pars.Append(Expression.Parameter(typeof(int))).ToArray();
            Expression<Action<int>> lambda1 = Expression.Lambda<Action<int>>(body, pars);
            Assert.Same(lambda1, lambda1.Update(body, new RunOnceEnumerable<ParameterExpression>(pars)));
            pars = pars.Append(Expression.Parameter(typeof(int))).ToArray();
            Expression<Action<int, int>> lambda2 = Expression.Lambda<Action<int, int>>(body, pars);
            Assert.Same(lambda2, lambda2.Update(body, new RunOnceEnumerable<ParameterExpression>(pars)));
            pars = pars.Append(Expression.Parameter(typeof(int))).ToArray();
            Expression<Action<int, int, int>> lambda3 = Expression.Lambda<Action<int, int, int>>(body, pars);
            Assert.Same(lambda3, lambda3.Update(body, new RunOnceEnumerable<ParameterExpression>(pars)));
            pars = pars.Append(Expression.Parameter(typeof(int))).ToArray();
            Expression<Action<int, int, int, int>> lambda4 = Expression.Lambda<Action<int, int, int, int>>(body, pars);
            Assert.Same(lambda4, lambda4.Update(body, new RunOnceEnumerable<ParameterExpression>(pars)));
        }

        [Fact]
        public void UpdateDifferentBodyReturnsDifferent()
        {
            Expression body = Expression.Empty();
            Expression newBody = Expression.Empty();
            ParameterExpression[] pars = Array.Empty<ParameterExpression>();
            Expression<Action> lambda0 = Expression.Lambda<Action>(body, pars);
            Assert.NotSame(lambda0, lambda0.Update(newBody, pars));
            pars = pars.Append(Expression.Parameter(typeof(int))).ToArray();
            Expression<Action<int>> lambda1 = Expression.Lambda<Action<int>>(body, pars);
            Assert.NotSame(lambda1, lambda1.Update(newBody, pars));
            pars = pars.Append(Expression.Parameter(typeof(int))).ToArray();
            Expression<Action<int, int>> lambda2 = Expression.Lambda<Action<int, int>>(body, pars);
            Assert.NotSame(lambda2, lambda2.Update(newBody, pars));
            pars = pars.Append(Expression.Parameter(typeof(int))).ToArray();
            Expression<Action<int, int, int>> lambda3 = Expression.Lambda<Action<int, int, int>>(body, pars);
            Assert.NotSame(lambda3, lambda3.Update(newBody, pars));
            pars = pars.Append(Expression.Parameter(typeof(int))).ToArray();
            Expression<Action<int, int, int, int>> lambda4 = Expression.Lambda<Action<int, int, int, int>>(body, pars);
            Assert.NotSame(lambda4, lambda4.Update(newBody, pars));
        }

        [Fact]
        public void UpdateDifferentParamsReturnsDifferent()
        {
            Expression body = Expression.Empty();
            ParameterExpression[] pars = Array.Empty<ParameterExpression>();
            Expression<Action> lambda0 = Expression.Lambda<Action>(body, pars);
            VerifyUpdateDifferentParamsReturnsDifferent(lambda0, pars);
            pars = pars.Append(Expression.Parameter(typeof(int))).ToArray();
            Expression<Action<int>> lambda1 = Expression.Lambda<Action<int>>(body, pars);
            VerifyUpdateDifferentParamsReturnsDifferent(lambda1, pars);
            pars = pars.Append(Expression.Parameter(typeof(int))).ToArray();
            Expression<Action<int, int>> lambda2 = Expression.Lambda<Action<int, int>>(body, pars);
            VerifyUpdateDifferentParamsReturnsDifferent(lambda2, pars);
            pars = pars.Append(Expression.Parameter(typeof(int))).ToArray();
            Expression<Action<int, int, int>> lambda3 = Expression.Lambda<Action<int, int, int>>(body, pars);
            VerifyUpdateDifferentParamsReturnsDifferent(lambda3, pars);
            pars = pars.Append(Expression.Parameter(typeof(int))).ToArray();
            Expression<Action<int, int, int, int>> lambda4 = Expression.Lambda<Action<int, int, int, int>>(body, pars);
            VerifyUpdateDifferentParamsReturnsDifferent(lambda4, pars);
        }

        private static void VerifyUpdateDifferentParamsReturnsDifferent<TDelegate>(Expression<TDelegate> lamda, ParameterExpression[] pars)
        {
            // Should try to create new lambda, but should fail as should have wrong number of arguments.
            AssertExtensions.Throws<ArgumentException>(null, () => lamda.Update(lamda.Body, pars.Append(Expression.Parameter(typeof(int)))));

            if (pars.Length != 0)
            {
                AssertExtensions.Throws<ArgumentException>(null, () => lamda.Update(lamda.Body, null));
                for (int i = 0; i != pars.Length; ++i)
                {
                    ParameterExpression[] newPars = new ParameterExpression[pars.Length];
                    pars.CopyTo(newPars, 0);
                    newPars[i] = Expression.Parameter(typeof(int));
                    Assert.NotSame(lamda, lamda.Update(lamda.Body, newPars));
                }

                IEnumerable<ParameterExpression> diffPars = new RunOnceEnumerable<ParameterExpression>(
                    Enumerable.Range(0, lamda.Parameters.Count) // Trigger Parameters collection build.
                    .Select(_ => Expression.Parameter(typeof(int))));

                Assert.NotSame(lamda, lamda.Update(lamda.Body, diffPars));
            }
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

        [Theory, ClassData(typeof(CompilationTypes))]
        public void CurriedFunctions(bool useInterpreter)
        {
            Expression<Func<int, Func<int>>> f1 = x => () => x;
            Func<int, Func<int>> d1 = f1.Compile(useInterpreter);
            Func<int> c1 = d1(42);
            Assert.Equal(42, c1());
            Assert.Equal(42, c1());

            Expression<Func<int, Func<int, int>>> f2 = x => y => x - y;
            Func<int, Func<int, int>> d2 = f2.Compile(useInterpreter);
            Func<int, int> c2 = d2(42);
            Assert.Equal(41, c2(1));
            Assert.Equal(40, c2(2));

            Expression<Func<int, Func<int, Func<int, int>>>> f3 = x => y => z => x * y - z;
            Func<int, Func<int, Func<int, int>>> d3 = f3.Compile(useInterpreter);
            Func<int, Func<int, int>> c3 = d3(2);
            Func<int, int> c31 = c3(21);
            Assert.Equal(41, c31(1));
            Assert.Equal(40, c31(2));
            Func<int, int> c32 = c3(22);
            Assert.Equal(41, c32(3));
            Assert.Equal(40, c32(4));
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public void CurriedFunctionsReadWrite(bool useInterpreter)
        {
            // Generate code like:
            //
            // val => () =>
            // {
            //     sum = 0;
            //
            //     val = 1;
            //     sum += val;
            //     ...
            //     val = n;
            //     sum += val;
            //
            //     return sum;
            // }
            //
            // This introduces repeated reads and writes for a hoisted local, which may be subject
            // to optimizations for closure storage access.
            //
            for (var i = 0; i < 10; i++)
            {
                ParameterExpression val = Expression.Parameter(typeof(int));
                ParameterExpression sum = Expression.Parameter(typeof(int));

                var addExprs = new List<Expression>();

                for (var j = 1; j <= i; j++)
                {
                    addExprs.Add(Expression.Assign(val, Expression.Constant(j)));
                    addExprs.Add(Expression.AddAssign(sum, val));
                }

                BlockExpression adds = Expression.Block(addExprs);
                BlockExpression body = Expression.Block(new[] { sum }, Expression.Assign(sum, Expression.Constant(0)), adds, sum);

                Expression<Func<int, Func<int>>> e = Expression.Lambda<Func<int, Func<int>>>(Expression.Lambda<Func<int>>(body), val);
                Func<int, Func<int>> f = e.Compile(useInterpreter);

                Assert.Equal(i * (i + 1) / 2, f(i)());
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public void CurriedFunctionsUsingRef(bool useInterpreter)
        {
            Expression<Func<int, Func<int>>> f1 = x => () => x * Add(ref x, 1);
            Func<int, Func<int>> d1 = f1.Compile(useInterpreter);
            Assert.Equal(3 * 4, d1(3)());

            Expression<Func<int, Func<int>>> f2 = x => () => x * Add(ref x, 1) * Add(ref x, 2);
            Func<int, Func<int>> d2 = f2.Compile(useInterpreter);
            Assert.Equal(3 * 4 * 6, d2(3)());

            Expression<Func<int, Func<int>>> f3 = x => () => x * Add(ref x, 1) * Add(ref x, 2) * x;
            Func<int, Func<int>> d3 = f3.Compile(useInterpreter);
            Assert.Equal(3 * 4 * 6 * 6, d3(3)());
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public void CurriedFunctionsReadWriteThroughRef(bool useInterpreter)
        {
            // Generate code like:
            //
            // val => () =>
            // {
            //     sum = 0;
            //
            //     val = 1;
            //     Add(ref sum, val);
            //     ...
            //     val = n;
            //     Add(ref sum, val);
            //
            //     return sum;
            // }
            //
            // This introduces repeated reads and writes for a hoisted local, which may be subject
            // to optimizations for closure storage access.
            //

            MethodInfo add = typeof(LambdaTests).GetMethod(nameof(LambdaTests.Add), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

            for (var i = 0; i < 10; i++)
            {
                ParameterExpression val = Expression.Parameter(typeof(int));
                ParameterExpression sum = Expression.Parameter(typeof(int));

                var addExprs = new List<Expression>();

                for (var j = 1; j <= i; j++)
                {
                    addExprs.Add(Expression.Assign(val, Expression.Constant(j)));
                    addExprs.Add(Expression.Call(add, sum, val));
                }

                BlockExpression adds = Expression.Block(addExprs);
                BlockExpression body = Expression.Block(new[] { sum }, Expression.Assign(sum, Expression.Constant(0)), adds, sum);

                Expression<Func<int, Func<int>>> e = Expression.Lambda<Func<int, Func<int>>>(Expression.Lambda<Func<int>>(body), val);
                Func<int, Func<int>> f = e.Compile(useInterpreter);

                Assert.Equal(i * (i + 1) / 2, f(i)());
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public void CurriedFunctionsVariableCaptureSemantics(bool useInterpreter)
        {
            ParameterExpression x = Expression.Parameter(typeof(int));
            ParameterExpression f = Expression.Parameter(typeof(Func<int>));

            Expression<Func<Func<int>>> e =
                Expression.Lambda<Func<Func<int>>>(
                    Expression.Block(
                        new[] { f, x },
                        Expression.Assign(x, Expression.Constant(-1)),
                        Expression.Assign(
                            f,
                            Expression.Lambda<Func<int>>(
                                Expression.MultiplyAssign(x, Expression.Constant(2))
                            )
                        ),
                        Expression.Assign(x, Expression.Constant(20)),
                        Expression.AddAssign(x, Expression.Constant(1)),
                        f
                    )
                );

            Func<Func<int>> d = e.Compile(useInterpreter);

            Func<int> i = d();

            Assert.Equal(42, i());
            Assert.Equal(84, i());
        }

        private static IEnumerable<object[]> LambdaTypes() =>
            from parCount in Enumerable.Range(0, 6)
            from name in new[] {null, "Lambda"}
            from tailCall in new[] {false, true}
            select new object[] {parCount, name, tailCall};

        [Theory, MemberData(nameof(LambdaTypes))]
        public void ParameterListBehavior(int parCount, string name, bool tailCall)
        {
            // This method contains a lot of assertions, which amount to one large assertion that
            // the result of the Parameters property behaves correctly.
            ParameterExpression[] pars =
                Enumerable.Range(0, parCount).Select(_ => Expression.Parameter(typeof(int))).ToArray();
            LambdaExpression lamda = Expression.Lambda(Expression.Empty(), name, tailCall, pars);
            ReadOnlyCollection<ParameterExpression> parameters = lamda.Parameters;
            Assert.Equal(parCount, parameters.Count);
            using (var en = parameters.GetEnumerator())
            {
                IEnumerator nonGenEn = ((IEnumerable)parameters).GetEnumerator();
                for (int i = 0; i != parCount; ++i)
                {
                    Assert.True(en.MoveNext());
                    Assert.True(nonGenEn.MoveNext());
                    Assert.Same(pars[i], parameters[i]);
                    Assert.Same(pars[i], en.Current);
                    Assert.Same(pars[i], nonGenEn.Current);
                    Assert.Equal(i, parameters.IndexOf(pars[i]));
                    Assert.True(parameters.Contains(pars[i]));
                }

                Assert.False(en.MoveNext());
                Assert.False(nonGenEn.MoveNext());
                (nonGenEn as IDisposable)?.Dispose();
            }

            ParameterExpression[] copyToTest = new ParameterExpression[parCount + 1];
            Assert.Throws<ArgumentNullException>(() => parameters.CopyTo(null, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => parameters.CopyTo(copyToTest, -1));
            Assert.All(copyToTest, Assert.Null); // assert partial copy didn't happen before exception
            AssertExtensions.Throws<ArgumentException>(parCount >= 1 && parCount <= 3 && name == null && !tailCall ? null : "destinationArray", () => parameters.CopyTo(copyToTest, 2));
            Assert.All(copyToTest, Assert.Null);
            parameters.CopyTo(copyToTest, 1);
            Assert.Equal(copyToTest, pars.Prepend(null));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => parameters[-1]);
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => parameters[parCount]);
            Assert.Equal(-1, parameters.IndexOf(Expression.Parameter(typeof(int))));
            Assert.False(parameters.Contains(Expression.Parameter(typeof(int))));
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public void AboveByteMaxArityArg(bool useInterpreter)
        {
            ParameterExpression[] pars = Enumerable.Range(0, 300).Select(_ => Expression.Parameter(typeof(int))).ToArray();
            LambdaExpression lambda = Expression.Lambda(pars.Last(), pars);
            Delegate del = lambda.Compile(useInterpreter);
            object[] args = Enumerable.Repeat<object>(0, 299).Append(23).ToArray();
            object result = del.DynamicInvoke(args);
            Assert.Equal(23, result);
        }

#if FEATURE_COMPILE
        [Theory, ClassData(typeof(CompilationTypes))]
        public void AboveByteMaxArityArgIL(bool useInterpreter)
        {
            ParameterExpression[] pars = Enumerable.Range(0, 300)
                .Select(_ => Expression.Parameter(typeof(int)))
                .ToArray();
            LambdaExpression lambda = Expression.Lambda(pars.Last(), pars);
            lambda.VerifyIL(
                @".method int32 ::lambda_method(class [System.Linq.Expressions]System.Runtime.CompilerServices.Closure,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32)
{
  .maxstack 1

  IL_0000: ldarg      V_300
  IL_0004: ret        
}
");
        }
#endif

        private struct Mutable
        {
            public bool Mutated;

            public Mutable Mutate()
            {
                Mutated = true;
                return this;
            }
        }

        private delegate Mutable TricentaryIntAndMutableFunc(
            int arg0, int arg1, int arg2, int arg3, int arg4, int arg5, int arg6, int arg7, int arg8, int arg9,
            int arg10, int arg11, int arg12, int arg13, int arg14, int arg15, int arg16, int arg17, int arg18,
            int arg19, int arg20, int arg21, int arg22, int arg23, int arg24, int arg25, int arg26, int arg27,
            int arg28, int arg29, int arg30, int arg31, int arg32, int arg33, int arg34, int arg35, int arg36,
            int arg37, int arg38, int arg39, int arg40, int arg41, int arg42, int arg43, int arg44, int arg45,
            int arg46, int arg47, int arg48, int arg49, int arg50, int arg51, int arg52, int arg53, int arg54,
            int arg55, int arg56, int arg57, int arg58, int arg59, int arg60, int arg61, int arg62, int arg63,
            int arg64, int arg65, int arg66, int arg67, int arg68, int arg69, int arg70, int arg71, int arg72,
            int arg73, int arg74, int arg75, int arg76, int arg77, int arg78, int arg79, int arg80, int arg81,
            int arg82, int arg83, int arg84, int arg85, int arg86, int arg87, int arg88, int arg89, int arg90,
            int arg91, int arg92, int arg93, int arg94, int arg95, int arg96, int arg97, int arg98, int arg99,
            int arg100, int arg101, int arg102, int arg103, int arg104, int arg105, int arg106, int arg107, int arg108,
            int arg109, int arg110, int arg111, int arg112, int arg113, int arg114, int arg115, int arg116, int arg117,
            int arg118, int arg119, int arg120, int arg121, int arg122, int arg123, int arg124, int arg125, int arg126,
            int arg127, int arg128, int arg129, int arg130, int arg131, int arg132, int arg133, int arg134, int arg135,
            int arg136, int arg137, int arg138, int arg139, int arg140, int arg141, int arg142, int arg143, int arg144,
            int arg145, int arg146, int arg147, int arg148, int arg149, int arg150, int arg151, int arg152, int arg153,
            int arg154, int arg155, int arg156, int arg157, int arg158, int arg159, int arg160, int arg161, int arg162,
            int arg163, int arg164, int arg165, int arg166, int arg167, int arg168, int arg169, int arg170, int arg171,
            int arg172, int arg173, int arg174, int arg175, int arg176, int arg177, int arg178, int arg179, int arg180,
            int arg181, int arg182, int arg183, int arg184, int arg185, int arg186, int arg187, int arg188, int arg189,
            int arg190, int arg191, int arg192, int arg193, int arg194, int arg195, int arg196, int arg197, int arg198,
            int arg199, int arg200, int arg201, int arg202, int arg203, int arg204, int arg205, int arg206, int arg207,
            int arg208, int arg209, int arg210, int arg211, int arg212, int arg213, int arg214, int arg215, int arg216,
            int arg217, int arg218, int arg219, int arg220, int arg221, int arg222, int arg223, int arg224, int arg225,
            int arg226, int arg227, int arg228, int arg229, int arg230, int arg231, int arg232, int arg233, int arg234,
            int arg235, int arg236, int arg237, int arg238, int arg239, int arg240, int arg241, int arg242, int arg243,
            int arg244, int arg245, int arg246, int arg247, int arg248, int arg249, int arg250, int arg251, int arg252,
            int arg253, int arg254, int arg255, int arg256, int arg257, int arg258, int arg259, int arg260, int arg261,
            int arg262, int arg263, int arg264, int arg265, int arg266, int arg267, int arg268, int arg269, int arg270,
            int arg271, int arg272, int arg273, int arg274, int arg275, int arg276, int arg277, int arg278, int arg279,
            int arg280, int arg281, int arg282, int arg283, int arg284, int arg285, int arg286, int arg287, int arg288,
            int arg289, int arg290, int arg291, int arg292, int arg293, int arg294, int arg295, int arg296, int arg297,
            int arg298, Mutable arg299);

        [Theory, ClassData(typeof(CompilationTypes))]
        public void AboveByteMaxArityArgAddress(bool useInterpreter)
        {
            ParameterExpression parToMutate = Expression.Parameter(typeof(Mutable));
            ParameterExpression[] pars = Enumerable.Range(0, 299)
                .Select(_ => Expression.Parameter(typeof(int)))
                .Append(parToMutate)
                .ToArray();
            Expression<TricentaryIntAndMutableFunc> lambda = Expression.Lambda<TricentaryIntAndMutableFunc>(
                Expression.Call(
                    parToMutate, nameof(Mutable.Mutate), Type.EmptyTypes, Array.Empty<ParameterExpression>()), pars);
            TricentaryIntAndMutableFunc del = lambda.Compile(useInterpreter);
            Mutable result = del(
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, default);
            Assert.True(result.Mutated);
        }

#if FEATURE_COMPILE
        [Fact]
        public void AboveByteMaxArityArgAddressIL()
        {
            ParameterExpression parToMutate = Expression.Parameter(typeof(Mutable));
            ParameterExpression[] pars = Enumerable.Range(0, 299)
                .Select(_ => Expression.Parameter(typeof(int)))
                .Append(parToMutate)
                .ToArray();
            Expression.Lambda<TricentaryIntAndMutableFunc>(
                Expression.Call(
                    parToMutate, nameof(Mutable.Mutate), Type.EmptyTypes, Array.Empty<ParameterExpression>()), pars).VerifyIL(@".method valuetype [System.Linq.Expressions.Tests]System.Linq.Expressions.Tests.LambdaTests+Mutable ::lambda_method(class [System.Linq.Expressions]System.Runtime.CompilerServices.Closure,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,valuetype [System.Linq.Expressions.Tests]System.Linq.Expressions.Tests.LambdaTests+Mutable)
{
  .maxstack 1

  IL_0000: ldarga     V_300
  IL_0004: call       instance valuetype [System.Linq.Expressions.Tests]System.Linq.Expressions.Tests.LambdaTests+Mutable valuetype [System.Linq.Expressions.Tests]System.Linq.Expressions.Tests.LambdaTests+Mutable::Mutate()
  IL_0009: ret        
}
");
        }
#endif

        [Theory, ClassData(typeof(CompilationTypes))]
        public void ExcessiveArity(bool useInterpreter)
        {
            ParameterExpression[] pars = Enumerable.Range(0, ushort.MaxValue).Select(_ => Expression.Parameter(typeof(int))).ToArray();
            LambdaExpression lambda = Expression.Lambda(pars.Last(), pars);
            Assert.Throws<InvalidProgramException>(() => lambda.Compile(useInterpreter));
        }

        private static int Add(ref int var, int val)
        {
            return var += val;
        }

        [Fact]
        public void OpenGenericDelegate()
        {
            AssertExtensions.Throws<ArgumentException>("delegateType", () => Expression.Lambda(typeof(Action<>), Expression.Empty()));
            AssertExtensions.Throws<ArgumentException>("delegateType", () => Expression.Lambda(typeof(Action<>), Expression.Empty(), Enumerable.Empty<ParameterExpression>()));
            AssertExtensions.Throws<ArgumentException>("delegateType", () => Expression.Lambda(typeof(Action<>), Expression.Empty(), false));
            AssertExtensions.Throws<ArgumentException>("delegateType", () => Expression.Lambda(typeof(Action<>), Expression.Empty(), false, Enumerable.Empty<ParameterExpression>()));
        }

#if FEATURE_COMPILE // When we don't have FEATURE_COMPILE we don't have the Reflection.Emit used in the tests.

        [Theory, ClassData(typeof(CompilationTypes))]
        public void PrivateDelegate(bool useInterpreter)
        {
            AssemblyBuilder assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Name"), AssemblyBuilderAccess.RunAndCollect);
            ModuleBuilder module = assembly.DefineDynamicModule("Name");
            TypeBuilder builder = module.DefineType("Type", TypeAttributes.Class | TypeAttributes.NotPublic | TypeAttributes.Sealed | TypeAttributes.AnsiClass | TypeAttributes.AutoClass, typeof(MulticastDelegate));
            builder.DefineConstructor(MethodAttributes.RTSpecialName | MethodAttributes.HideBySig | MethodAttributes.Public, CallingConventions.Standard, new[] { typeof(object), typeof(IntPtr) }).SetImplementationFlags(MethodImplAttributes.Runtime | MethodImplAttributes.Managed);
            builder.DefineMethod("Invoke", MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual, typeof(int), Type.EmptyTypes).SetImplementationFlags(MethodImplAttributes.Runtime | MethodImplAttributes.Managed);
            Type delType = builder.CreateTypeInfo();
            LambdaExpression lambda = Expression.Lambda(delType, Expression.Constant(42));
            Delegate del = lambda.Compile(useInterpreter);
            Assert.IsType(delType, del);
            Assert.Equal(42, del.DynamicInvoke());
        }

#endif

    }
}
