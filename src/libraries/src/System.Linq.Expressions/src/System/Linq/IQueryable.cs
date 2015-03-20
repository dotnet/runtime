// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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