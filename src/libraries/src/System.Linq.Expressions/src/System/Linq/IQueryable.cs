// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace System.Linq
{
    public interface IQueryable : IEnumerable
    {
        Expression Expression { get; }
        Type ElementType { get; }

        // the provider that created this query
        IQueryProvider Provider { get; }
    }

    public interface IQueryable<out T> : IEnumerable<T>, IQueryable
    {
    }

    public interface IQueryProvider
    {
        IQueryable CreateQuery(Expression expression);

        IQueryable<TElement> CreateQuery<TElement>(Expression expression);

        object Execute(Expression expression);

        TResult Execute<TResult>(Expression expression);
    }

    public interface IOrderedQueryable : IQueryable
    {
    }

    public interface IOrderedQueryable<out T> : IQueryable<T>, IOrderedQueryable
    {
    }
}
