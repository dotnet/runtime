// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections;
using System.Linq.Expressions;
using Xunit;

namespace System.Linq.Tests
{
    public class EnumerableQueryTests
    {
        [Fact]
        public void NullExpressionAllowed()
        {
            IQueryable<int> query = new EnumerableQuery<int>((Expression)null);
            Assert.Null(query.Expression);
            Assert.Throws<ArgumentNullException>(() => query.GetEnumerator());
        }

        [Fact]
        public void NullEnumerableConstantNullExpression()
        {
            IQueryable<int> query = new EnumerableQuery<int>((IEnumerable<int>)null);
            var exp = (ConstantExpression)query.Expression;
            Assert.Same(query, exp.Value);
            Assert.Throws<InvalidOperationException>(() => query.GetEnumerator());
        }

        [Fact]
        public void InappropriateExpressionType()
        {
            IQueryable<int> query = new EnumerableQuery<int>(Expression.Constant(Math.PI));
            Assert.NotNull(query.Expression);
            Assert.Throws<ArgumentException>(() => query.GetEnumerator());
        }

        [Fact]
        public void WrapsEnumerableInExpression()
        {
            int[] source = { 1, 2, 3 };
            IQueryable<int> query = (source).AsQueryable();
            var exp = (ConstantExpression)query.Expression;
            Assert.Equal(source, (IEnumerable<int>)exp.Value);
        }

        [Fact]
        public void IsOwnProvider()
        {
            IQueryable<int> query = Enumerable.Empty<int>().AsQueryable();
            Assert.Same(query, query.Provider);
        }

        [Fact]
        public void FromExpressionReturnsSameExpression()
        {
            ConstantExpression exp = Expression.Constant(new[] { 1, 2, 3 });
            IQueryable<int> query = new EnumerableQuery<int>(exp);
            Assert.Same(exp, query.Expression);
        }

        // Guarantees to return the same enumerator every time from the same instance,
        // but not from other instances. As such indicates constancy of enumerable wrapped
        // by EnumerableQuery
        private class ConstantEnumeratorEmptyEnumerable<T> : IEnumerable<T>, IEnumerator<T>
        {
            public IEnumerator<T> GetEnumerator()
            {
                return this;
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return this;
            }

            public bool MoveNext()
            {
                return false;
            }

            public T Current
            {
                get { return default(T); }
            }

            object IEnumerator.Current
            {
                get { return default(T); }
            }

            public void Reset()
            {
            }

            public void Dispose()
            {
            }
        }

        [Fact]
        public void FromEnumerableReturnsSameEnumerable()
        {
            var testEnumerable = new ConstantEnumeratorEmptyEnumerable<int>();
            IQueryable<int> query = testEnumerable.AsQueryable();
            Assert.Same(testEnumerable.GetEnumerator(), query.GetEnumerator());
        }

        [Fact]
        public void ElementType()
        {
            Assert.Equal(typeof(Version), Enumerable.Empty<Version>().AsQueryable().ElementType);
        }

        [Fact]
        public void CreateQuery()
        {
            var exp = Expression.Constant(Enumerable.Range(3, 4).AsQueryable());
            IQueryProvider provider = Enumerable.Empty<string>().AsQueryable().Provider;
            IQueryable<int> query = provider.CreateQuery<int>(exp);
            Assert.Equal(Enumerable.Range(3, 4), query.AsEnumerable());
        }

        [Fact]
        public void CreateQueryNonGeneric()
        {
            var exp = Expression.Constant(Enumerable.Range(3, 4).AsQueryable());
            IQueryProvider provider = Enumerable.Empty<string>().AsQueryable().Provider;
            IQueryable query = provider.CreateQuery(exp);
            Assert.Equal(Enumerable.Range(3, 4), query.Cast<int>());
        }

        [Fact]
        public void CreateQueryNull()
        {
            IQueryProvider provider = Enumerable.Empty<int>().AsQueryable().Provider;
            Assert.Throws<ArgumentNullException>("expression", () => provider.CreateQuery<int>(null));
        }

        [Fact]
        public void CreateQueryNullNonGeneric()
        {
            IQueryProvider provider = Enumerable.Empty<int>().AsQueryable().Provider;
            Assert.Throws<ArgumentNullException>("expression", () => provider.CreateQuery(null));
        }

        [Fact]
        public void CreateQueryInvalidType()
        {
            var exp = Expression.Constant(Math.PI);
            IQueryProvider provider = Enumerable.Empty<string>().AsQueryable().Provider;
            Assert.Throws<ArgumentException>(() => provider.CreateQuery<int>(exp));
        }

        [Fact]
        public void CreateQueryInvalidTypeNonGeneric()
        {
            var exp = Expression.Constant(Math.PI);
            IQueryProvider provider = Enumerable.Empty<string>().AsQueryable().Provider;
            Assert.Throws<ArgumentException>(() => provider.CreateQuery(exp));
        }

        [Fact]
        public void Execute()
        {
            var exp = Expression.Constant(Math.PI);
            IQueryProvider provider = Enumerable.Empty<string>().AsQueryable().Provider;
            Assert.Equal(Math.PI, provider.Execute<double>(exp));
        }

        [Fact]
        public void ExecuteAssignable()
        {
            var exp = Expression.Constant(new int[0]);
            IQueryProvider provider = Enumerable.Empty<string>().AsQueryable().Provider;
            Assert.Empty(provider.Execute<IEnumerable<int>>(exp));
        }

        [Fact]
        public void ExecuteNonGeneric()
        {
            var exp = Expression.Constant(Math.PI);
            IQueryProvider provider = Enumerable.Empty<string>().AsQueryable().Provider;
            Assert.Equal(Math.PI, provider.Execute(exp));
        }

        [Fact]
        public void ExecuteNull()
        {
            IQueryProvider provider = Enumerable.Empty<string>().AsQueryable().Provider;
            Assert.Throws<ArgumentNullException>("expression", () => provider.Execute<int>(null));
        }

        [Fact]
        public void ExecuteNullNonGeneric()
        {
            IQueryProvider provider = Enumerable.Empty<string>().AsQueryable().Provider;
            Assert.Throws<ArgumentNullException>("expression", () => provider.Execute(null));
        }

        [Fact]
        public void ExecuteNotAssignable()
        {
            var exp = Expression.Constant(Math.PI);
            IQueryProvider provider = Enumerable.Empty<string>().AsQueryable().Provider;
            Assert.Throws<ArgumentException>(() => provider.Execute<IEnumerable<int>>(exp));
        }

        [Fact]
        public void ToStringFromEnumerable()
        {
            Assert.Equal(new int[0].ToString(), new int[0].AsQueryable().ToString());
        }

        [Fact]
        public void ToStringFromConstantExpression()
        {
            var exp = Expression.Constant(new int[0]);
            Assert.Equal(exp.ToString(), new EnumerableQuery<int>(exp).ToString());
        }

        [Fact]
        public void ToStringNotConstantExpression()
        {
            var exp = Expression.Constant(new int[0]);
            var block = Expression.Block(Expression.Empty(), exp);
            Assert.Equal(block.ToString(), new EnumerableQuery<int>(block).ToString());
        }

        [Fact]
        public void NullEnumerable()
        {
            Assert.Equal("null", new EnumerableQuery<int>(default(int[])).ToString());
        }
    }
}
