// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using Xunit;

namespace System.Linq.Tests
{
    public class QueryFromExpressionTests
    {
        private class SimplePair : IEnumerable<int>
        {
            public int First { get; set; }

            public int Second { get; set; }

            public IEnumerator<int> GetEnumerator()
            {
                yield return First;
                yield return Second;
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        private static IQueryProvider _prov = Enumerable.Empty<int>().AsQueryable().Provider;

        [Fact]
        public void ExpressionToQueryFromProvider()
        {
            Expression exp = Expression.Constant(Enumerable.Range(0, 2).AsQueryable());
            IQueryable<int> q = _prov.CreateQuery<int>(exp);
            Assert.Equal(Enumerable.Range(0, 2), q);
        }

        [Fact]
        public void ExpressionToQueryByConstructor()
        {
            Expression exp = Expression.Constant(Enumerable.Range(0, 2).AsQueryable());
            IQueryable<int> q = new EnumerableQuery<int>(exp);
            Assert.Equal(Enumerable.Range(0, 2), q);
        }

        [Fact]
        public void ConditionalEqualModuloConstantConstant()
        {
            Expression cond = Expression.Condition(
                Expression.Equal(
                    Expression.Modulo(Expression.Constant(1), Expression.Constant(2)),
                    Expression.Constant(0)
                   ),
                Expression.Constant(Enumerable.Range(0, 2).AsQueryable()),
                Expression.Constant(Enumerable.Range(3, 2).AsQueryable())
            );
            IQueryable<int> q = _prov.CreateQuery<int>(cond);
            Assert.Equal(Enumerable.Range(3, 2), q);
        }

        private static IQueryable<char> AsciiControlCharacters
        {
            get
            {
                return Enumerable.Range(0, 128).AsQueryable()
                    .Select(i => (char)i)
                    .Where(c => char.IsControl(c));
            }
        }

        [Fact]
        public void PropertyAccess()
        {
            Expression access = Expression.Property(null, typeof(QueryFromExpressionTests), "AsciiControlCharacters");
            IQueryable<char> q = _prov.CreateQuery<char>(access);
            Assert.Equal(Enumerable.Range(0, 128).Select(i => (char)i).Where(c => char.IsControl(c)), q);
        }

        [Fact]
        public void ArrayIndex()
        {
            Expression array = Expression.Constant("abcd".ToArray());
            Expression call = Expression.Call(
                typeof(Queryable),
                "AsQueryable",
                new[] { typeof(char) },
                Expression.NewArrayInit(
                    typeof(char),
                    Expression.ArrayIndex(array, Expression.Constant(0)),
                    Expression.ArrayIndex(array, Expression.Constant(2)))
                );
            Assert.Equal(new[] { 'a', 'c' }, _prov.CreateQuery<char>(call));
        }

        [Fact]
        public void ConditionalNotNotEqualAddPlusConstantNegateConstant()
        {
            Expression cond = Expression.Condition(
                Expression.Not(
                    Expression.NotEqual(
                        Expression.Add(Expression.UnaryPlus(Expression.Constant(1)), Expression.Negate(Expression.Constant(2))),
                        Expression.Constant(-1)
                        )
                        ),
                Expression.Constant(Enumerable.Range(0, 2).AsQueryable()),
                Expression.Constant(Enumerable.Range(3, 2).AsQueryable())
            );
            IQueryable<int> q = _prov.CreateQuery<int>(cond);
            Assert.Equal(Enumerable.Range(0, 2), q);
        }

        [Fact]
        public void ConditionalNotNotEqualAddCheckedPlusConstantNegateCheckedConstant()
        {
            Expression cond = Expression.Condition(
                Expression.Not(
                    Expression.NotEqual(
                        Expression.AddChecked(Expression.UnaryPlus(Expression.Constant(1)), Expression.NegateChecked(Expression.Constant(2))),
                        Expression.Constant(-1)
                        )
                        ),
                Expression.Constant(Enumerable.Range(0, 2).AsQueryable()),
                Expression.Constant(Enumerable.Range(3, 2).AsQueryable())
            );
            IQueryable<int> q = _prov.CreateQuery<int>(cond);
            Assert.Equal(Enumerable.Range(0, 2), q);
        }

        [Fact]
        public void ConditionalLogicalOperators()
        {
            Expression logic = Expression.OrElse(
                Expression.AndAlso(
                    Expression.LessThanOrEqual(Expression.Constant(3), Expression.Constant(4)), Expression.LessThan(Expression.Constant(2), Expression.Constant(1))
                ),
                Expression.Or(
                    Expression.And(
                        Expression.GreaterThan(Expression.Constant(2), Expression.Constant(1)),
                        Expression.GreaterThanOrEqual(
                            Expression.Constant(8),
                            Expression.ExclusiveOr(Expression.Constant(3), Expression.Constant(5))
                        )
                    ),
                    Expression.Constant(true)
                )
            );
            Expression cond = Expression.Condition(
                logic,
                Expression.Constant(Enumerable.Range(0, 2).AsQueryable()),
                Expression.Constant(Enumerable.Range(3, 2).AsQueryable())
            );
            IQueryable<int> q = _prov.CreateQuery<int>(cond);
            Assert.Equal(Enumerable.Range(0, 2), q);
        }

        [Fact]
        public void SubtractionAndCalls()
        {
            Expression rangeCall = Expression.Call(
                typeof(Enumerable),
                "Range",
                new Type[0],
                Expression.Subtract(Expression.Constant(6), Expression.Constant(2)),
                Expression.SubtractChecked(Expression.Constant(12), Expression.Constant(3))
                );
            Expression call = Expression.Call(
                typeof(Queryable),
                "AsQueryable",
                new[] { typeof(int) },
                rangeCall
                );
            IQueryable<int> q = _prov.CreateQuery<int>(call);
            Assert.Equal(Enumerable.Range(4, 9), q);
        }

        [Fact]
        public void MultiplicationAndCalls()
        {
            Expression rangeCall = Expression.Call(
                typeof(Enumerable),
                "Range",
                new Type[0],
                Expression.Multiply(Expression.Constant(4), Expression.Constant(5)),
                Expression.MultiplyChecked(Expression.Constant(3), Expression.Constant(2))
                );
            Expression call = Expression.Call(
                typeof(Queryable),
                "AsQueryable",
                new[] { typeof(int) },
                rangeCall
                );
            IQueryable<int> q = _prov.CreateQuery<int>(call);
            Assert.Equal(Enumerable.Range(20, 6), q);
        }

        [Fact]
        public void DivisionAndPower()
        {
            Expression rangeCall = Expression.Call(
                typeof(Enumerable),
                "Range",
                new Type[0],
                Expression.Convert(Expression.Power(Expression.Constant(4.0), Expression.Constant(5.0)), typeof(int)),
                Expression.Divide(Expression.Constant(20), Expression.Constant(10))
                );
            Expression call = Expression.Call(
                typeof(Queryable),
                "AsQueryable",
                new[] { typeof(int) },
                rangeCall
                );
            IQueryable<int> q = _prov.CreateQuery<int>(call);
            Assert.Equal(Enumerable.Range(1024, 2), q);
        }

        [Fact]
        public void ConvertsNewArrayAndArrayLength()
        {
            Expression cond = Expression.Condition(
                Expression.Equal(
                    Expression.AddChecked(
                        Expression.Convert(
                            Expression.ArrayLength(Expression.NewArrayInit(typeof(int), Enumerable.Range(0, 3).Select(i => Expression.Constant(i)))),
                            typeof(long)),
                        Expression.ConvertChecked(
                            Expression.ArrayLength(Expression.NewArrayBounds(typeof(bool), Expression.Constant(2))),
                            typeof(long)
                            )
                            ),
                    Expression.Constant(5L)
                    ),
                Expression.Constant(Enumerable.Range(0, 2).AsQueryable()),
                Expression.Constant(Enumerable.Range(3, 2).AsQueryable())
            );
            IQueryable<int> q = _prov.CreateQuery<int>(cond);
            Assert.Equal(Enumerable.Range(0, 2), q);
        }

        [Fact]
        public void CoalesceShifts()
        {
            Expression list = Expression.ListInit(
                Expression.New(typeof(List<int>)),
                Expression.LeftShift(Expression.Constant(5), Expression.Constant(2)),
                Expression.RightShift(Expression.Constant(31), Expression.Constant(1))
                );
            Expression call = Expression.Call(
                typeof(Queryable),
                "AsQueryable",
                new[] { typeof(int) },
                list
                );
            Expression coal = Expression.Coalesce(Expression.Constant(null, typeof(IQueryable<int>)), call);
            IQueryable<int> q = _prov.CreateQuery<int>(coal);
            Assert.Equal(new[] { 20, 15 }, q);
        }

        [Fact]
        public void TypeAs()
        {
            Expression cond = Expression.Condition(
                Expression.Equal(
                    Expression.Constant(null),
                    Expression.TypeAs(Expression.Constant("", typeof(object)), typeof(string))
                    ),
                Expression.Constant(Enumerable.Range(0, 2).AsQueryable()),
                Expression.Constant(Enumerable.Range(3, 2).AsQueryable())
            );
            IQueryable<int> q = _prov.CreateQuery<int>(cond);
            Assert.Equal(Enumerable.Range(3, 2), q);
            cond = Expression.Condition(
                Expression.Equal(
                    Expression.Constant(null),
                    Expression.TypeAs(Expression.Constant("", typeof(object)), typeof(Uri))
                    ),
                Expression.Constant(Enumerable.Range(0, 2).AsQueryable()),
                Expression.Constant(Enumerable.Range(3, 2).AsQueryable())
            );
            q = _prov.CreateQuery<int>(cond);
            Assert.Equal(Enumerable.Range(0, 2), q);
        }

        [Fact]
        public void TypeIs()
        {
            Expression cond = Expression.Condition(
                Expression.TypeIs(Expression.Constant("", typeof(object)), typeof(string)),
                Expression.Constant(Enumerable.Range(0, 2).AsQueryable()),
                Expression.Constant(Enumerable.Range(3, 2).AsQueryable())
            );
            IQueryable<int> q = _prov.CreateQuery<int>(cond);
            Assert.Equal(Enumerable.Range(0, 2), q);
            cond = Expression.Condition(
                Expression.TypeIs(Expression.New(typeof(object)), typeof(string)),
                Expression.Constant(Enumerable.Range(0, 2).AsQueryable()),
                Expression.Constant(Enumerable.Range(3, 2).AsQueryable())
            );
            q = _prov.CreateQuery<int>(cond);
            Assert.Equal(Enumerable.Range(3, 2), q);
        }

        [Fact]
        public void MemberInit()
        {
            Expression init = Expression.MemberInit(
                    Expression.New(typeof(SimplePair)),
                    Expression.Bind(typeof(SimplePair).GetMember("First")[0], Expression.Constant(8)),
                    Expression.Bind(typeof(SimplePair).GetMember("Second")[0], Expression.Constant(13))
                );
            Expression call = Expression.Call(
                typeof(Queryable),
                "AsQueryable",
                new[] { typeof(int) },
                init
                );
            IQueryable<int> q = _prov.CreateQuery<int>(call);
            Assert.Equal(new[] { 8, 13 }, q);
        }

        [Fact]
        public void InvokeAndMemberAccess()
        {
            Expression<Func<int, IQueryable<char>>> lambda = start => "acbdefghijklmnop".AsQueryable().Skip(start);
            Expression invoke = Expression.Invoke(lambda, Expression.Constant(2));
            IQueryable<char> q = _prov.CreateQuery<char>(invoke);
            Assert.Equal("bdefghijklmnop".ToCharArray(), q.ToArray());
        }

        [Fact]
        public void QueryWrappedAsConstant()
        {
            Expression cond = Expression.Condition(
                Expression.Equal(
                    Expression.Modulo(Expression.Constant(1), Expression.Constant(2)),
                    Expression.Constant(0)
                   ),
                Expression.Constant(Enumerable.Range(0, 2).AsQueryable()),
                Expression.Constant(Enumerable.Range(3, 2).AsQueryable())
            );
            IQueryable<int> q = _prov.CreateQuery<int>(Expression.Constant(_prov.CreateQuery<int>(cond)));
            Assert.Equal(Enumerable.Range(3, 2), q);
        }

        private sealed class BogusExpression : Expression
        {
            public override ExpressionType NodeType
            {
                get { return (ExpressionType)(-1); }
            }

            public override Type Type
            {
                get { return typeof(IQueryable<bool>); }
            }
        }

        [Fact]
        public void UnknownExpressionType()
        {
            IQueryable<bool> q = _prov.CreateQuery<bool>(new BogusExpression());
            Assert.Throws<ArgumentException>(() => q.GetEnumerator());
        }

        private IQueryable<string> SimpleMethod()
        {
            return new[] { "a", "b", "c" }.AsQueryable();
        }

        [Fact]
        public void SimpleMethodCall()
        {
            Expression call = Expression.Call(Expression.Constant(this), "SimpleMethod", new Type[0]);
            IQueryable<string> q = _prov.CreateQuery<string>(call);
            Assert.Equal(new[] { "a", "b", "c" }, q);
        }

        private static IEnumerable<char> IncrementCharacters(char start, char end)
        {
            for (; start != end; ++start)
            {
                yield return start;
            }
        }

        private IQueryable<char> ParameterMethod(char start, char end)
        {
            return IncrementCharacters(start, end).AsQueryable();
        }

        [Fact]
        public void ParameterMethodCallViaLambda()
        {
            ParameterExpression start = Expression.Parameter(typeof(char));
            ParameterExpression end = Expression.Parameter(typeof(char));
            Expression call = Expression.Call(Expression.Constant(this), "ParameterMethod", new Type[0], start, end);
            Expression lambda = Expression.Lambda<Func<char, char, IQueryable<char>>>(call, start, end);
            Expression invoke = Expression.Invoke(lambda, Expression.Constant('b'), Expression.Constant('g'));
            Assert.Equal("bcdef".ToCharArray(), _prov.CreateQuery<char>(invoke));
        }

        private static class TestLinqExtensions
        {
            public static IEnumerable<int> RunningTotals(IEnumerable<int> source)
            {
                if (source == null)
                    throw new ArgumentNullException(nameof(source));
                return RunningTotalsIterator(source);
            }

            public static IEnumerable<int> RunningTotalsIterator(IEnumerable<int> source)
            {
                using (var en = source.GetEnumerator())
                {
                    if (en.MoveNext())
                    {
                        int current = en.Current;
                        yield return current;
                        while (en.MoveNext())
                            yield return current += en.Current;
                    }
                }
            }

            public static IQueryable<int> RunningTotals(IQueryable<int> source)
            {
                // A real class would only overload for IQueryable separately if there
                // was a reason for doing so, but this suffices to test.
                return RunningTotals(source.AsEnumerable()).AsQueryable();
            }

            public static IQueryable<int> RunningTotalsNoMatch(IQueryable<int> source)
            {
                return RunningTotals(source);
            }

            public static IQueryable<int> RunningTotals(IQueryable<int> source, int initialTally)
            {
                return RunningTotals(Enumerable.Repeat(initialTally, 1).AsQueryable().Concat(source));
            }
        }

        private class TestLinqInstanceNoMatch
        {
            public IQueryable<int> RunningTotals(IQueryable<int> source)
            {
                return TestLinqExtensions.RunningTotals(source);
            }
        }

        [Fact]
        public void EnumerableQueryableAsInternalArgumentToMethod()
        {
            Expression call = Expression.Call(
                typeof(TestLinqExtensions)
                    .GetMethods()
                    .First(mi => mi.Name == "RunningTotals" && mi.GetParameters().Length == 1 && mi.GetParameters()[0].ParameterType == typeof(IQueryable<int>)),
                Expression.Constant(Enumerable.Range(1, 3).AsQueryable())
                );
            IQueryable<int> q = _prov.CreateQuery<int>(call);
            Assert.Equal(new[] { 1, 3, 6 }, q);
        }

        [Fact]
        public void EnumerableQueryableAsInternalArgumentToMethodNoMatch()
        {
            Expression call = Expression.Call(
                typeof(TestLinqExtensions)
                    .GetMethods()
                    .First(mi => mi.Name == "RunningTotalsNoMatch" && mi.GetParameters()[0].ParameterType == typeof(IQueryable<int>)),
                Expression.Constant(Enumerable.Range(1, 3).AsQueryable())
                );
            IQueryable<int> q = _prov.CreateQuery<int>(call);
            Assert.Throws<InvalidOperationException>(() => q.GetEnumerator());
        }

        [Fact]
        public void EnumerableQueryableAsInternalArgumentToMethodNoArgumentMatch()
        {
            Expression call = Expression.Call(
                typeof(TestLinqExtensions)
                    .GetMethods()
                    .First(mi => mi.Name == "RunningTotals" && mi.GetParameters().Length == 2),
                Expression.Constant(Enumerable.Range(1, 3).AsQueryable()),
                Expression.Constant(3)
                );
            IQueryable<int> q = _prov.CreateQuery<int>(call);
            Assert.Throws<InvalidOperationException>(() => q.GetEnumerator());
        }

        [Fact]
        public void EnumerableQueryableAsInternalArgumentToInstanceMethodNoMatch()
        {
            Expression call = Expression.Call(
                Expression.Constant(new TestLinqInstanceNoMatch()),
                typeof(TestLinqInstanceNoMatch)
                    .GetMethods()
                    .First(mi => mi.Name == "RunningTotals"),
                Expression.Constant(Enumerable.Range(1, 3).AsQueryable())
                );
            IQueryable<int> q = _prov.CreateQuery<int>(call);
            Assert.Throws<InvalidOperationException>(() => q.GetEnumerator());
        }

        [Fact]
        public void EnumerableQueryAsInternalArgumentToQueryableMethod()
        {
            Expression call = Expression.Call(typeof(Queryable), "AsQueryable", new[] { typeof(int) }, Expression.Constant(Enumerable.Range(1, 3).AsQueryable(), typeof(IQueryable<int>)));
            IQueryable<int> q = _prov.CreateQuery<int>(call);
            Assert.Equal(new[] { 1, 2, 3 }, q);
        }

        [Fact]
        public void NonGeneric()
        {
            Expression call = Expression.Call(typeof(Queryable), "AsQueryable", new[] { typeof(int) }, Expression.Constant(Enumerable.Range(1, 3).AsQueryable(), typeof(IQueryable<int>)));
            IQueryable q = _prov.CreateQuery(call);
            Assert.Equal(new[] { 1, 2, 3 }, q.Cast<int>());
        }

        [Fact]
        public void ExplicitlyTypedConditional()
        {
            Expression call = Expression.Call(
                typeof(Queryable),
                "OfType",
                new[] { typeof(long) },
                Expression.Condition(
                    Expression.Constant(true),
                    Expression.Constant(new long?[] { 2, 3, null, 1 }.AsQueryable()),
                    Expression.Constant(Enumerable.Range(0, 3).AsQueryable().Select(i => (long)i)),
                    typeof(IQueryable)
                    )
                );
            IQueryable<long> q = _prov.CreateQuery<long>(call);
            Assert.Equal(new long[] { 2, 3, 1 }, q);
        }

        [Fact]
        public void Block()
        {
            Expression block = Expression.Block(
                Expression.Empty(),
                Expression.Constant(Enumerable.Range(0, 2).AsQueryable())
                );
            IQueryable<int> q = _prov.CreateQuery<int>(block);
            Assert.Equal(Enumerable.Range(0, 2), q);
        }

        [Fact]
        public void ExplicitlyTypedBlock()
        {
            Expression block = Expression.Block(
                typeof(IQueryable<int>),
                Expression.Empty(),
                Expression.Constant(Enumerable.Range(0, 2).AsQueryable())
                );
            IQueryable<int> q = _prov.CreateQuery<int>(block);
            Assert.Equal(Enumerable.Range(0, 2), q);
        }

        [Fact]
        public void Return()
        {
            LabelTarget target = Expression.Label(typeof(IQueryable<int>));
            Expression block = Expression.Block(
                    Expression.Return(target, Expression.Constant(Enumerable.Range(0, 2).AsQueryable())),
                    Expression.Label(target, Expression.Default(typeof(IQueryable<int>)))
                );
            IQueryable<int> q = _prov.CreateQuery<int>(block);
            Assert.Equal(Enumerable.Range(0, 2), q);
        }

        [Fact]
        public void ReturnArray()
        {
            LabelTarget target = Expression.Label(typeof(IQueryable<int>));
            Expression block = Expression.Block(
                    Expression.Return(target, Expression.Constant(new[] { 1, 1, 2, 3, 5, 8 }.AsQueryable())),
                    Expression.Label(target, Expression.Default(typeof(IQueryable<int>)))
                );
            IQueryable<int> q = _prov.CreateQuery<int>(block);
            Assert.Equal(new[] { 1, 1, 2, 3, 5, 8 }, q);
        }

        [Fact]
        public void ReturnOrdered()
        {
            LabelTarget target = Expression.Label(typeof(IQueryable<int>));
            Expression block = Expression.Block(
                    Expression.Return(target, Expression.Constant(Enumerable.Range(0, 3).OrderByDescending(i => i).AsQueryable())),
                    Expression.Label(target, Expression.Default(typeof(IOrderedQueryable<int>)))
                );
            IQueryable<int> q = _prov.CreateQuery<int>(block);
            Assert.Equal(new[] { 2, 1, 0 }, q);
        }

        [Fact]
        public void ReturnOrderedAfterAsQueryable()
        {
            LabelTarget target = Expression.Label(typeof(IQueryable<int>));
            Expression block = Expression.Block(
                    Expression.Return(target, Expression.Constant(Enumerable.Range(0, 3).AsQueryable().OrderByDescending(i => i))),
                    Expression.Label(target, Expression.Default(typeof(IOrderedQueryable<int>)))
                );
            IQueryable<int> q = _prov.CreateQuery<int>(block);
            Assert.Equal(new[] { 2, 1, 0 }, q);
        }

        [Fact]
        public void ReturnNonQueryable()
        {
            LabelTarget target = Expression.Label(typeof(int));
            Expression block = Expression.Block(
                Expression.Condition(
                    Expression.Constant(true),
                    Expression.Return(target, Expression.Constant(3)),
                    Expression.Return(target, Expression.Constant(1))
                    ),
                Expression.Return(target, Expression.Constant(2)),
                Expression.Label(target, Expression.Default(typeof(int)))
                );
            Assert.Equal(3, _prov.Execute<int>(block));
        }

        [Fact]
        public void ReturnNonQueryableUntyped()
        {
            LabelTarget target = Expression.Label(typeof(int));
            Expression block = Expression.Block(
                Expression.Condition(
                    Expression.Constant(true),
                    Expression.Return(target, Expression.Constant(3)),
                    Expression.Return(target, Expression.Constant(1))
                    ),
                Expression.Return(target, Expression.Constant(2)),
                Expression.Label(target, Expression.Default(typeof(int)))
                );
            Assert.Equal(3, _prov.Execute(block));
        }
    }
}
