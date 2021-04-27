// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

namespace System.Linq
{
    public abstract class EnumerableQuery
    {
        internal abstract Expression Expression { get; }
        internal abstract IEnumerable? Enumerable { get; }

        internal EnumerableQuery() { }

        [RequiresUnreferencedCode(Queryable.InMemoryQueryableExtensionMethodsRequiresUnreferencedCode)]
        internal static IQueryable Create(Type elementType, IEnumerable sequence)
        {
            Type seqType = typeof(EnumerableQuery<>).MakeGenericType(elementType);
            return (IQueryable)Activator.CreateInstance(seqType, sequence)!;
        }

        [RequiresUnreferencedCode(Queryable.InMemoryQueryableExtensionMethodsRequiresUnreferencedCode)]
        internal static IQueryable Create(Type elementType, Expression expression)
        {
            Type seqType = typeof(EnumerableQuery<>).MakeGenericType(elementType);
            return (IQueryable)Activator.CreateInstance(seqType, expression)!;
        }
    }

    public class EnumerableQuery<T> : EnumerableQuery, IOrderedQueryable<T>, IQueryProvider
    {
        private readonly Expression _expression;
        private IEnumerable<T>? _enumerable;

        IQueryProvider IQueryable.Provider => this;

        [RequiresUnreferencedCode(Queryable.InMemoryQueryableExtensionMethodsRequiresUnreferencedCode)]
        public EnumerableQuery(IEnumerable<T> enumerable)
        {
            _enumerable = enumerable;
            _expression = Expression.Constant(this);
        }

        [RequiresUnreferencedCode(Queryable.InMemoryQueryableExtensionMethodsRequiresUnreferencedCode)]
        public EnumerableQuery(Expression expression)
        {
            _expression = expression;
        }

        internal override Expression Expression => _expression;

        internal override IEnumerable? Enumerable => _enumerable;

        Expression IQueryable.Expression => _expression;

        Type IQueryable.ElementType => typeof(T);

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "This class's ctor is annotated as RequiresUnreferencedCode.")]
        IQueryable IQueryProvider.CreateQuery(Expression expression)
        {
            if (expression == null)
                throw Error.ArgumentNull(nameof(expression));
            Type? iqType = TypeHelper.FindGenericType(typeof(IQueryable<>), expression.Type);
            if (iqType == null)
                throw Error.ArgumentNotValid(nameof(expression));
            return Create(iqType.GetGenericArguments()[0], expression);
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "This class's ctor is annotated as RequiresUnreferencedCode.")]
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

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "This class's ctor is annotated as RequiresUnreferencedCode.")]
        object? IQueryProvider.Execute(Expression expression)
        {
            if (expression == null)
                throw Error.ArgumentNull(nameof(expression));
            return EnumerableExecutor.Create(expression).ExecuteBoxed();
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "This class's ctor is annotated as RequiresUnreferencedCode.")]
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

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "This class's ctor is annotated as RequiresUnreferencedCode.")]
        private IEnumerator<T> GetEnumerator()
        {
            if (_enumerable == null)
            {
                EnumerableRewriter rewriter = new EnumerableRewriter();
                Expression body = rewriter.Visit(_expression);
                Expression<Func<IEnumerable<T>>> f = Expression.Lambda<Func<IEnumerable<T>>>(body, (IEnumerable<ParameterExpression>?)null);
                IEnumerable<T> enumerable = f.Compile()();
                if (enumerable == this)
                    throw Error.EnumeratingNullEnumerableExpression();
                _enumerable = enumerable;
            }
            return _enumerable.GetEnumerator();
        }

        public override string? ToString()
        {
            if (_expression is ConstantExpression c && c.Value == this)
            {
                if (_enumerable != null)
                    return _enumerable.ToString();
                return "null";
            }
            return _expression.ToString();
        }
    }
}
