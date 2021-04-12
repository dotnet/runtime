// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

namespace System.Linq
{
    /// <summary>Represents an expression tree and provides functionality to execute the expression tree after rewriting it.</summary>
    public abstract class EnumerableExecutor
    {
        [RequiresUnreferencedCode(Queryable.InMemoryQueryableExtensionMethodsRequiresUnreferencedCode)]
        internal abstract object? ExecuteBoxed();

        /// <summary>Initializes a new instance of the <see cref="System.Linq.EnumerableExecutor" /> class.</summary>
        internal EnumerableExecutor() { }

        internal static EnumerableExecutor Create(Expression expression)
        {
            Type execType = typeof(EnumerableExecutor<>).MakeGenericType(expression.Type);
            return (EnumerableExecutor)Activator.CreateInstance(execType, expression)!;
        }
    }

    /// <summary>Represents an expression tree and provides functionality to execute the expression tree after rewriting it.</summary>
    /// <typeparam name="T">The data type of the value that results from executing the expression tree.</typeparam>
    public class EnumerableExecutor<T> : EnumerableExecutor
    {
        private readonly Expression _expression;

        /// <summary>Initializes a new instance of the <see cref="System.Linq.EnumerableExecutor{T}" /> class.</summary>
        /// <param name="expression">An expression tree to associate with the new instance.</param>
        public EnumerableExecutor(Expression expression)
        {
            _expression = expression;
        }

        [RequiresUnreferencedCode(Queryable.InMemoryQueryableExtensionMethodsRequiresUnreferencedCode)]
        internal override object? ExecuteBoxed() => Execute();

        [RequiresUnreferencedCode(Queryable.InMemoryQueryableExtensionMethodsRequiresUnreferencedCode)]
        internal T Execute()
        {
            EnumerableRewriter rewriter = new EnumerableRewriter();
            Expression body = rewriter.Visit(_expression);
            Expression<Func<T>> f = Expression.Lambda<Func<T>>(body, (IEnumerable<ParameterExpression>?)null);
            Func<T> func = f.Compile();
            return func();
        }
    }
}
