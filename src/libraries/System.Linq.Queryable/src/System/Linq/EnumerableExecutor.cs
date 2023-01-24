// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

namespace System.Linq
{
    public abstract class EnumerableExecutor
    {
        [RequiresUnreferencedCode(Queryable.InMemoryQueryableExtensionMethodsRequiresUnreferencedCode)]
        [RequiresDynamicCode(Queryable.InMemoryQueryableExtensionMethodsRequiresDynamicCode)]
        internal abstract object? ExecuteBoxed();

        internal EnumerableExecutor() { }

        [RequiresDynamicCode(Queryable.InMemoryQueryableExtensionMethodsRequiresDynamicCode)]
        internal static EnumerableExecutor Create(Expression expression)
        {
            Type execType = typeof(EnumerableExecutor<>).MakeGenericType(expression.Type);
            return (EnumerableExecutor)Activator.CreateInstance(execType, expression)!;
        }
    }

    public class EnumerableExecutor<T> : EnumerableExecutor
    {
        private readonly Expression _expression;

        public EnumerableExecutor(Expression expression)
        {
            _expression = expression;
        }

        [RequiresUnreferencedCode(Queryable.InMemoryQueryableExtensionMethodsRequiresUnreferencedCode)]
        [RequiresDynamicCode(Queryable.InMemoryQueryableExtensionMethodsRequiresDynamicCode)]
        internal override object? ExecuteBoxed() => Execute();

        [RequiresUnreferencedCode(Queryable.InMemoryQueryableExtensionMethodsRequiresUnreferencedCode)]
        [RequiresDynamicCode(Queryable.InMemoryQueryableExtensionMethodsRequiresDynamicCode)]
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
