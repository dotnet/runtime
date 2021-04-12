// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

namespace System.Linq
{
    /// <summary>Represents an <see cref="System.Collections.IEnumerable" /> as an <see cref="System.Linq.EnumerableQuery" /> data source.</summary>
    public abstract class EnumerableQuery
    {
        internal abstract Expression Expression { get; }
        internal abstract IEnumerable? Enumerable { get; }

        /// <summary>Initializes a new instance of the <see cref="System.Linq.EnumerableQuery" /> class.</summary>
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

    /// <summary>Represents an <see cref="System.Collections.Generic.IEnumerable{T}" /> collection as an <see cref="System.Linq.IQueryable{T}" /> data source.</summary>
    /// <typeparam name="T">The type of the data in the collection.</typeparam>
    public class EnumerableQuery<T> : EnumerableQuery, IOrderedQueryable<T>, IQueryProvider
    {
        private readonly Expression _expression;
        private IEnumerable<T>? _enumerable;

        /// <summary>Gets the query provider that is associated with this instance.</summary>
        /// <value>The query provider that is associated with this instance.</value>
        /// <remarks>This member is an explicit interface member implementation. It can be used only when the <see cref="System.Linq.EnumerableQuery{T}" /> instance is cast to an <see cref="System.Linq.IQueryable" /> interface.</remarks>
        IQueryProvider IQueryable.Provider => this;

        /// <summary>Initializes a new instance of the <see cref="System.Linq.EnumerableQuery{T}" /> class and associates it with an <see cref="System.Collections.Generic.IEnumerable{T}" /> collection.</summary>
        /// <param name="enumerable">A collection to associate with the new instance.</param>
        [RequiresUnreferencedCode(Queryable.InMemoryQueryableExtensionMethodsRequiresUnreferencedCode)]
        public EnumerableQuery(IEnumerable<T> enumerable)
        {
            _enumerable = enumerable;
            _expression = Expression.Constant(this);
        }

        /// <summary>Initializes a new instance of the <see cref="System.Linq.EnumerableQuery{T}" /> class and associates the instance with an expression tree.</summary>
        /// <param name="expression">An expression tree to associate with the new instance.</param>
        [RequiresUnreferencedCode(Queryable.InMemoryQueryableExtensionMethodsRequiresUnreferencedCode)]
        public EnumerableQuery(Expression expression)
        {
            _expression = expression;
        }

        internal override Expression Expression => _expression;

        internal override IEnumerable? Enumerable => _enumerable;

        /// <summary>Gets the expression tree that is associated with or that represents this instance.</summary>
        /// <value>The expression tree that is associated with or that represents this instance.</value>
        /// <remarks>This member is an explicit interface member implementation. It can be used only when the <see cref="System.Linq.EnumerableQuery{T}" /> instance is cast to an <see cref="System.Linq.IQueryable" /> interface.</remarks>
        Expression IQueryable.Expression => _expression;

        /// <summary>Gets the type of the data in the collection that this instance represents.</summary>
        /// <value>The type of the data in the collection that this instance represents.</value>
        /// <remarks>This member is an explicit interface member implementation. It can be used only when the <see cref="System.Linq.EnumerableQuery{T}" /> instance is cast to an <see cref="System.Linq.IQueryable" /> interface.</remarks>
        Type IQueryable.ElementType => typeof(T);

        /// <summary>Constructs a new <see cref="System.Linq.EnumerableQuery{T}" /> object and associates it with a specified expression tree that represents an <see cref="System.Linq.IQueryable" /> collection of data.</summary>
        /// <param name="expression">An expression tree that represents an <see cref="System.Linq.IQueryable" /> collection of data.</param>
        /// <returns>An <see cref="System.Linq.EnumerableQuery{T}" /> object that is associated with <paramref name="expression" />.</returns>
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

        /// <summary>Constructs a new <see cref="System.Linq.EnumerableQuery{T}" /> object and associates it with a specified expression tree that represents an <see cref="System.Linq.IQueryable{T}" /> collection of data.</summary>
        /// <typeparam name="TElement">The type of the data in the collection that <paramref name="expression" /> represents.</typeparam>
        /// <param name="expression">An expression tree to execute.</param>
        /// <returns>An EnumerableQuery object that is associated with <paramref name="expression" />.</returns>
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

        /// <summary>Executes an expression after rewriting it to call <see cref="System.Linq.Enumerable" /> methods instead of <see cref="System.Linq.Queryable" /> methods on any enumerable data sources that cannot be queried by <see cref="System.Linq.Queryable" /> methods.</summary>
        /// <param name="expression">An expression tree to execute.</param>
        /// <returns>The value that results from executing <paramref name="expression" />.</returns>
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "This class's ctor is annotated as RequiresUnreferencedCode.")]
        object? IQueryProvider.Execute(Expression expression)
        {
            if (expression == null)
                throw Error.ArgumentNull(nameof(expression));
            return EnumerableExecutor.Create(expression).ExecuteBoxed();
        }

        /// <summary>Executes an expression after rewriting it to call <see cref="System.Linq.Enumerable" /> methods instead of <see cref="System.Linq.Queryable" /> methods on any enumerable data sources that cannot be queried by <see cref="System.Linq.Queryable" /> methods.</summary>
        /// <typeparam name="TElement">The type of the data in the collection that <paramref name="expression" /> represents.</typeparam>
        /// <param name="expression">An expression tree to execute.</param>
        /// <returns>The value that results from executing <paramref name="expression" />.</returns>
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

        /// <summary>Returns an enumerator that can iterate through the associated <see cref="System.Collections.Generic.IEnumerable{T}" /> collection, or, if it is null, through the collection that results from rewriting the associated expression tree as a query on an <see cref="System.Collections.Generic.IEnumerable{T}" /> data source and executing it.</summary>
        /// <returns>An enumerator that can be used to iterate through the associated data source.</returns>
        /// <remarks>This member is an explicit interface member implementation. It can be used only when the <see cref="System.Linq.EnumerableQuery{T}" /> instance is cast to an <see cref="System.Collections.IEnumerable" /> interface.</remarks>
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

        /// <summary>Returns a textual representation of the enumerable collection or, if it is null, of the expression tree that is associated with this instance.</summary>
        /// <returns>A textual representation of the enumerable collection or, if it is null, of the expression tree that is associated with this instance.</returns>
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
