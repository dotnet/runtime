// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace System.Linq
{
    // Must remain public for Silverlight
    public abstract class EnumerableQuery
    {
        internal abstract Expression Expression { get; }
        internal abstract IEnumerable Enumerable { get; }
        internal static IQueryable Create(Type elementType, IEnumerable sequence)
        {
            Type seqType = typeof(EnumerableQuery<>).MakeGenericType(elementType);
            return (IQueryable)Activator.CreateInstance(seqType, sequence);
        }

        internal static IQueryable Create(Type elementType, Expression expression)
        {
            Type seqType = typeof(EnumerableQuery<>).MakeGenericType(elementType);
            return (IQueryable)Activator.CreateInstance(seqType, expression);
        }
    }

    // Must remain public for Silverlight
    public class EnumerableQuery<T> : EnumerableQuery, IOrderedQueryable<T>, IQueryProvider
    {
        private readonly Expression _expression;
        private IEnumerable<T> _enumerable;

        IQueryProvider IQueryable.Provider => this;

        // Must remain public for Silverlight
        public EnumerableQuery(IEnumerable<T> enumerable)
        {
            _enumerable = enumerable;
            _expression = Expression.Constant(this);
        }

        // Must remain public for Silverlight
        public EnumerableQuery(Expression expression)
        {
            _expression = expression;
        }

        internal override Expression Expression => _expression;

        internal override IEnumerable Enumerable => _enumerable;

        Expression IQueryable.Expression => _expression;

        Type IQueryable.ElementType => typeof(T);

        IQueryable IQueryProvider.CreateQuery(Expression expression)
        {
            if (expression == null)
                throw Error.ArgumentNull(nameof(expression));
            Type iqType = TypeHelper.FindGenericType(typeof(IQueryable<>), expression.Type);
            if (iqType == null)
                throw Error.ArgumentNotValid(nameof(expression));
            return Create(iqType.GetGenericArguments()[0], expression);
        }

        IQueryable<TElement> IQueryProvider.CreateQuery<TElement>(Expression expression)
        {
            if (expression == null)
                throw Error.ArgumentNull(nameof(expression));
            if (!typeof(IQueryable<TElement>).IsAssignableFrom(expression.Type))
            {
                throw Error.ArgumentNotValid(nameof(expression));
            }
            return new EnumerableQuery<TElement>(expression);
        }

        // Baselining as Safe for Mix demo so that interface can be transparent. Marking this
        // critical (which was the original annotation when porting to silverlight) would violate
        // fxcop security rules if the interface isn't also critical. However, transparent code
        // can't access this anyway for Mix since we're not exposing AsQueryable().
        //
        // Note, the above assertion no longer holds. Now making AsQueryable() public again
        // the security fallout of which will need to be re-examined.
        object IQueryProvider.Execute(Expression expression)
        {
            if (expression == null)
                throw Error.ArgumentNull(nameof(expression));
            return EnumerableExecutor.Create(expression).ExecuteBoxed();
        }

        // see above
        TElement IQueryProvider.Execute<TElement>(Expression expression)
        {
            if (expression == null)
                throw Error.ArgumentNull(nameof(expression));
            if (!typeof(TElement).IsAssignableFrom(expression.Type))
                throw Error.ArgumentNotValid(nameof(expression));
            return new EnumerableExecutor<TElement>(expression).Execute();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

        private IEnumerator<T> GetEnumerator()
        {
            if (_enumerable == null)
            {
                EnumerableRewriter rewriter = new EnumerableRewriter();
                Expression body = rewriter.Visit(_expression);
                Expression<Func<IEnumerable<T>>> f = Expression.Lambda<Func<IEnumerable<T>>>(body, (IEnumerable<ParameterExpression>)null);
                IEnumerable<T> enumerable = f.Compile()();
                if (enumerable == this)
                    throw Error.EnumeratingNullEnumerableExpression();
                _enumerable = enumerable;
            }
            return _enumerable.GetEnumerator();
        }

        public override string ToString()
        {
            ConstantExpression c = _expression as ConstantExpression;
            if (c != null && c.Value == this)
            {
                if (_enumerable != null)
                    return _enumerable.ToString();
                return "null";
            }
            return _expression.ToString();
        }
    }
}
