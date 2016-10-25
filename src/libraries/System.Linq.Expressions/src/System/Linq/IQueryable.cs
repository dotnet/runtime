// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace System.Linq
{
    /// <summary>
    /// Provides functionality to evaluate queries against a specific data source wherein the type of the data is not specified.
    /// </summary>
    public interface IQueryable : IEnumerable
    {
        /// <summary>
        /// Gets the expression tree that is associated with the instance of <see cref="IQueryable"/>.
        /// </summary>
        Expression Expression { get; }

        /// <summary>
        /// Gets the type of the element(s) that are returned when the expression tree associated with this instance of <see cref="IQueryable"/> is executed.
        /// </summary>
        Type ElementType { get; }

        /// <summary>
        /// Gets the query provider that is associated with this data source.
        /// </summary>
        IQueryProvider Provider { get; }
    }

    /// <summary>
    /// Provides functionality to evaluate queries against a specific data source wherein the type of the data is known.
    /// </summary>
    /// <typeparam name="T">The type of the data in the data source.</typeparam>
    public interface IQueryable<out T> : IEnumerable<T>, IQueryable
    {
    }

    /// <summary>
    /// Defines methods to create and execute queries that are described by an <see cref="IQueryable"/> object.
    /// </summary>
    /// <remarks>
    /// The <see cref="IQueryProvider"/> interface is intended for implementation by query providers.
    /// </remarks>
    public interface IQueryProvider
    {
        /// <summary>
        /// Constructs an <see cref="IQueryable"/> object that can evaluate the query represented by a specified expression tree.
        /// </summary>
        /// <param name="expression">An expression tree that represents a LINQ query.</param>
        /// <returns>An <see cref="IQueryable"/> that can evaluate the query represented by the specified expression tree.</returns>
        /// <remarks>
        /// The CreateQuery method is used to create new <see cref="IQueryable"/> objects, given an expression tree. The query that is represented by the returned object is associated with a specific LINQ provider.
        /// Several of the standard query operator methods defined in Queryable, such as OfType{TResult} and Cast{TResult}, call this method. They pass it a <see cref="MethodCallExpression"/> that represents a LINQ query.
        /// </remarks>
        IQueryable CreateQuery(Expression expression);

        /// <summary>
        /// Constructs an <see cref="IQueryable{T}"/> object that can evaluate the query represented by a specified expression tree.
        /// </summary>
        /// <param name="expression">An expression tree that represents a LINQ query.</param>
        /// <returns>An <see cref="IQueryable{T}"/> that can evaluate the query represented by the specified expression tree.</returns>
        /// <remarks>
        /// The <see cref="CreateQuery{TElement}"/> method is used to create new <see cref="IQueryable{T}"/> objects, given an expression tree. The query that is represented by the returned object is associated with a specific LINQ provider.
        /// Most of the Queryable standard query operator methods that return enumerable results call this method.They pass it a <see cref="MethodCallExpression"/> that represents a LINQ query.
        /// </remarks>
        IQueryable<TElement> CreateQuery<TElement>(Expression expression);

        /// <summary>
        /// Executes the query represented by a specified expression tree.
        /// </summary>
        /// <param name="expression">An expression tree that represents a LINQ query.</param>
        /// <returns>The value that results from executing the specified query.</returns>
        /// <remarks>
        /// The <see cref="Execute"/> method executes queries that return a single value (instead of an enumerable sequence of values). Expression trees that represent queries that return enumerable results are executed when their associated <see cref="IQueryable"/> object is enumerated.
        /// </remarks>
        object Execute(Expression expression);

        /// <summary>
        /// Executes the strongly-typed query represented by a specified expression tree.
        /// </summary>
        /// <typeparam name="TResult">The type of the value that results from executing the query.</typeparam>
        /// <param name="expression">An expression tree that represents a LINQ query.</param>
        /// <returns>The value that results from executing the specified query.</returns>
        /// <remarks>
        /// The <see cref="Execute{TElement}"/> method executes queries that return a single value (instead of an enumerable sequence of values). Expression trees that represent queries that return enumerable results are executed when the <see cref="IQueryable{T}"/> object that contains the expression tree is enumerated.
        /// The Queryable standard query operator methods that return singleton results call <see cref="Execute{TElement}"/>. They pass it a <see cref="MethodCallExpression"/> that represents a LINQ query.
        /// </remarks>
        TResult Execute<TResult>(Expression expression);
    }

    /// <summary>
    /// Represents the result of a sorting operation.
    /// </summary>
    public interface IOrderedQueryable : IQueryable
    {
    }

    /// <summary>
    /// Represents the result of a sorting operation.
    /// </summary>
    /// <typeparam name="T">The type of the content of the data source.</typeparam>
    public interface IOrderedQueryable<out T> : IQueryable<T>, IOrderedQueryable
    {
    }
}
