// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace System.Linq
{
    /// <summary>Provides functionality to evaluate queries against a specific data source wherein the type of the data is not specified.</summary>
    /// <remarks>The <see cref="System.Linq.IQueryable" /> interface is intended for implementation by query providers. It is only supposed to be implemented by providers that also implement <see cref="System.Linq.IQueryable{T}" />. If the provider does not also implement <see cref="System.Linq.IQueryable{T}" />, the standard query operators cannot be used on the provider's data source.
    /// The <see cref="System.Linq.IQueryable" /> interface inherits the <see cref="System.Collections.IEnumerable" /> interface so that if it represents a query, the results of that query can be enumerated. Enumeration causes the expression tree associated with an <see cref="System.Linq.IQueryable" /> object to be executed. The definition of "executing an expression tree" is specific to a query provider. For example, it may involve translating the expression tree to an appropriate query language for the underlying data source. Queries that do not return enumerable results are executed when the <see cref="O:System.Linq.IQueryProvider.Execute" /> method is called.
    /// For more information about how to create your own LINQ provider, see <a href="https://docs.microsoft.com/archive/blogs/mattwar/linq-building-an-iqueryable-provider-part-i">LINQ: Building an IQueryable Provider</a>.</remarks>
    /// <altmember langword="System.Linq.Queryable"/>
    /// <altmember cref="System.Linq.IQueryable{T}"/>
    public interface IQueryable : IEnumerable
    {
        /// <summary>Gets the expression tree that is associated with the instance of <see cref="System.Linq.IQueryable" />.</summary>
        /// <value>The <see cref="System.Linq.Expressions.Expression" /> that is associated with this instance of <see cref="System.Linq.IQueryable" />.</value>
        /// <remarks>If an instance of <see cref="System.Linq.IQueryable" /> represents a LINQ query against a data source, the associated expression tree represents that query.</remarks>
        Expression Expression { get; }

        /// <summary>Gets the type of the element(s) that are returned when the expression tree associated with this instance of <see cref="System.Linq.IQueryable" /> is executed.</summary>
        /// <value>A <see cref="System.Type" /> that represents the type of the element(s) that are returned when the expression tree associated with this object is executed.</value>
        /// <remarks>The <see cref="O:System.Linq.IQueryable.ElementType" /> property represents the "T" in `IQueryable&lt;T&gt;` or `IQueryable(Of T)`.</remarks>
        Type ElementType { get; }

        /// <summary>Gets the query provider that is associated with this data source.</summary>
        /// <value>The <see cref="System.Linq.IQueryProvider" /> that is associated with this data source.</value>
        /// <remarks>If an instance of <see cref="System.Linq.IQueryable" /> represents a LINQ query against a data source, the associated query provider is the provider that created the <see cref="System.Linq.IQueryable" /> instance.</remarks>
        IQueryProvider Provider { get; }
    }

    /// <summary>Provides functionality to evaluate queries against a specific data source wherein the type of the data is known.</summary>
    /// <typeparam name="T">The type of the data in the data source.</typeparam>
    /// <remarks>The <see cref="System.Linq.IQueryable{T}" /> interface is intended for implementation by query providers.
    /// This interface inherits the <see cref="System.Collections.Generic.IEnumerable{T}" /> interface so that if it represents a query, the results of that query can be enumerated. Enumeration forces the expression tree associated with an <see cref="System.Linq.IQueryable{T}" /> object to be executed. Queries that do not return enumerable results are executed when the <see cref="System.Linq.IQueryProvider.Execute{T}(System.Linq.Expressions.Expression)" /> method is called.
    /// The definition of "executing an expression tree" is specific to a query provider. For example, it may involve translating the expression tree to a query language appropriate for an underlying data source.
    /// The <see cref="System.Linq.IQueryable{T}" /> interface enables queries to be polymorphic. That is, because a query against an `IQueryable` data source is represented as an expression tree, it can be executed against different types of data sources.
    /// The <see langword="static" /> (`Shared` in Visual Basic) methods defined in the class <see langword="System.Linq.Queryable" /> (except for <see cref="O:System.Linq.Queryable.AsQueryable" />, <see cref="O:System.Linq.Queryable.ThenBy" />, and <see cref="O:System.Linq.Queryable.ThenByDescending" />) extend objects of types that implement the <see cref="System.Linq.IQueryable{T}" /> interface.
    /// For more information about how to create your own LINQ provider, see <a href="https://docs.microsoft.com/archive/blogs/mattwar/linq-building-an-iqueryable-provider-part-i">LINQ: Building an IQueryable Provider</a>.</remarks>
    /// <altmember langword="System.Linq.Queryable"/>
    public interface IQueryable<out T> : IEnumerable<T>, IQueryable
    {
    }

    /// <summary>Defines methods to create and execute queries that are described by an <see cref="System.Linq.IQueryable" /> object.</summary>
    /// <remarks>The <see cref="System.Linq.IQueryProvider" /> interface is intended for implementation by query providers.
    /// For more information about how to create your own LINQ provider, see <a href="https://docs.microsoft.com/archive/blogs/mattwar/linq-building-an-iqueryable-provider-part-i">LINQ: Building an IQueryable Provider</a>.</remarks>
    /// <altmember langword="System.Linq.Queryable"/>
    /// <altmember cref="System.Linq.IQueryable{T}"/>
    public interface IQueryProvider
    {
        /// <summary>Constructs an <see cref="System.Linq.IQueryable" /> object that can evaluate the query represented by a specified expression tree.</summary>
        /// <param name="expression">An expression tree that represents a LINQ query.</param>
        /// <returns>An <see cref="System.Linq.IQueryable" /> that can evaluate the query represented by the specified expression tree.</returns>
        /// <remarks><format type="text/markdown"><![CDATA[
        /// > [!NOTE]
        /// >  The <xref:System.Linq.IQueryable.Expression%2A> property of the returned <xref:System.Linq.IQueryable> object is equal to `expression`.
        /// ]]></format>
        /// The <see cref="O:System.Linq.IQueryProvider.CreateQuery" /> method is used to create new <see cref="System.Linq.IQueryable" /> objects, given an expression tree. The query that is represented by the returned object is associated with a specific LINQ provider.
        /// Several of the standard query operator methods defined in <see langword="System.Linq.Queryable" />, such as <see cref="O:System.Linq.Queryable.OfType" /> and <see cref="O:System.Linq.Queryable.Cast" />, call this method. They pass it a <see cref="System.Linq.Expressions.MethodCallExpression" /> that represents a LINQ query.</remarks>
        IQueryable CreateQuery(Expression expression);

        /// <summary>Constructs an <see cref="System.Linq.IQueryable{T}" /> object that can evaluate the query represented by a specified expression tree.</summary>
        /// <typeparam name="TElement">The type of the elements of the <see cref="System.Linq.IQueryable{T}" /> that is returned.</typeparam>
        /// <param name="expression">An expression tree that represents a LINQ query.</param>
        /// <returns>An <see cref="System.Linq.IQueryable{T}" /> that can evaluate the query represented by the specified expression tree.</returns>
        /// <remarks><format type="text/markdown"><![CDATA[
        /// > [!NOTE]
        /// >  The <xref:System.Linq.IQueryable.Expression%2A> property of the returned <xref:System.Linq.IQueryable%601> object is equal to `expression`.
        /// ]]></format>
        /// The <see cref="O:System.Linq.IQueryProvider.CreateQuery" /> method is used to create new <see cref="System.Linq.IQueryable{T}" /> objects, given an expression tree. The query that is represented by the returned object is associated with a specific LINQ provider.
        /// Most of the <see langword="System.Linq.Queryable" /> standard query operator methods that return enumerable results call this method. They pass it a <see cref="System.Linq.Expressions.MethodCallExpression" /> that represents a LINQ query.</remarks>
        IQueryable<TElement> CreateQuery<TElement>(Expression expression);

        /// <summary>Executes the query represented by a specified expression tree.</summary>
        /// <param name="expression">An expression tree that represents a LINQ query.</param>
        /// <returns>The value that results from executing the specified query.</returns>
        /// <remarks>The <see cref="O:System.Linq.IQueryProvider.Execute" /> method executes queries that return a single value (instead of an enumerable sequence of values). Expression trees that represent queries that return enumerable results are executed when their associated <see cref="System.Linq.IQueryable" /> object is enumerated.</remarks>
        object? Execute(Expression expression);

        /// <summary>Executes the strongly-typed query represented by a specified expression tree.</summary>
        /// <typeparam name="TResult">The type of the value that results from executing the query.</typeparam>
        /// <param name="expression">An expression tree that represents a LINQ query.</param>
        /// <returns>The value that results from executing the specified query.</returns>
        /// <remarks>The <see cref="O:System.Linq.IQueryProvider.Execute" /> method executes queries that return a single value (instead of an enumerable sequence of values). Expression trees that represent queries that return enumerable results are executed when the <see cref="System.Linq.IQueryable{T}" /> object that contains the expression tree is enumerated.
        /// The <see langword="System.Linq.Queryable" /> standard query operator methods that return singleton results call <see cref="O:System.Linq.IQueryProvider.Execute" />. They pass it a <see cref="System.Linq.Expressions.MethodCallExpression" /> that represents a LINQ query.</remarks>
        TResult Execute<TResult>(Expression expression);
    }

    /// <summary>Represents the result of a sorting operation.</summary>
    /// <remarks>The <see cref="System.Linq.IOrderedQueryable" /> interface is intended for implementation by query providers.
    /// This interface represents the result of a sorting query that calls the method(s) <see cref="O:System.Linq.Queryable.OrderBy" />, <see cref="O:System.Linq.Queryable.OrderByDescending" />, <see cref="O:System.Linq.Queryable.ThenBy" /> or <see cref="O:System.Linq.Queryable.ThenByDescending" />. When <see cref="O:System.Linq.IQueryProvider.CreateQuery" /> is called and passed an expression tree that represents a sorting query, the resulting <see cref="System.Linq.IQueryable" /> object must be of a type that implements <see cref="System.Linq.IOrderedQueryable" />.
    /// For more information about how to create your own LINQ provider, see <a href="https://docs.microsoft.com/archive/blogs/mattwar/linq-building-an-iqueryable-provider-part-i">LINQ: Building an IQueryable Provider</a>.</remarks>
    /// <altmember langword="System.Linq.Queryable"/>
    /// <altmember cref="System.Linq.IQueryable{T}"/>
    public interface IOrderedQueryable : IQueryable
    {
    }

    /// <summary>Represents the result of a sorting operation.</summary>
    /// <typeparam name="T">The type of the content of the data source.</typeparam>
    /// <remarks>The <see cref="System.Linq.IOrderedQueryable{T}" /> interface is intended for implementation by query providers.
    /// This interface represents the result of a sorting query that calls the method(s) <see cref="O:System.Linq.Queryable.OrderBy" />, <see cref="O:System.Linq.Queryable.OrderByDescending" />, <see cref="O:System.Linq.Queryable.ThenBy" /> or <see cref="O:System.Linq.Queryable.ThenByDescending" />. When <see cref="System.Linq.IQueryProvider.CreateQuery{T}(System.Linq.Expressions.Expression)" /> is called and passed an expression tree that represents a sorting query, the resulting <see cref="System.Linq.IQueryable{T}" /> object must be of a type that implements <see cref="System.Linq.IOrderedQueryable{T}" />.
    /// For more information about how to create your own LINQ provider, see <a href="https://docs.microsoft.com/archive/blogs/mattwar/linq-building-an-iqueryable-provider-part-i">LINQ: Building an IQueryable Provider</a>.</remarks>
    /// <altmember langword="System.Linq.Queryable"/>
    /// <altmember cref="System.Linq.IQueryable{T}"/>
    public interface IOrderedQueryable<out T> : IQueryable<T>, IOrderedQueryable
    {
    }
}
