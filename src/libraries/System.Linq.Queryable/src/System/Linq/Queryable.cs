// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

namespace System.Linq
{
    /// <summary>Provides a set of <see langword="static" /> (<see langword="Shared" /> in Visual Basic) methods for querying data structures that implement <see cref="System.Linq.IQueryable{T}" />.</summary>
    /// <remarks>
    /// <para>The set of methods declared in the <see cref="System.Linq.Queryable" /> class provides an implementation of the standard query operators for querying data sources that implement <see cref="System.Linq.IQueryable{T}" />. The standard query operators are general purpose methods that follow the LINQ pattern and enable you to express traversal, filter, and projection operations over data in any .NET-based programming language.</para>
    /// <para>The majority of the methods in this class are defined as extension methods that extend the <see cref="System.Linq.IQueryable{T}" /> type. This means they can be called like an instance method on any object that implements <see cref="System.Linq.IQueryable{T}" />. These methods that extend <see cref="System.Linq.IQueryable{T}" /> do not perform any querying directly. Instead, their functionality is to build an <see cref="System.Linq.Expressions.Expression" /> object, which is an expression tree that represents the cumulative query. The methods then pass the new expression tree to either the <see cref="System.Linq.IQueryProvider.Execute{T}(System.Linq.Expressions.Expression)" /> method or the <see cref="System.Linq.IQueryProvider.CreateQuery{T}(System.Linq.Expressions.Expression)" /> method of the input <see cref="System.Linq.IQueryable{T}" />. The method that is called depends on whether the <see cref="System.Linq.Queryable" /> method returns a singleton value, in which case <see cref="System.Linq.IQueryProvider.Execute{T}(System.Linq.Expressions.Expression)" /> is called, or has enumerable results, in which case <see cref="System.Linq.IQueryProvider.CreateQuery{T}(System.Linq.Expressions.Expression)" /> is called.</para>
    /// <para>The actual query execution on the target data is performed by a class that implements <see cref="System.Linq.IQueryable{T}" />. The expectation of any <see cref="System.Linq.IQueryable{T}" /> implementation is that the result of executing an expression tree that was constructed by a <see cref="System.Linq.Queryable" /> standard query operator method is equivalent to the result of calling the corresponding method in the <see cref="System.Linq.Enumerable" /> class, if the data source were an <see cref="System.Collections.Generic.IEnumerable{T}" />.</para>
    /// <para>In addition to the standard query operator methods that operate on <see cref="System.Linq.IQueryable{T}" /> objects, this class also contains a method, <see cref="O:System.Linq.Queryable.AsQueryable" />, which types <see cref="System.Collections.IEnumerable" /> objects as <see cref="System.Linq.IQueryable" /> objects.</para>
    /// </remarks>
    /// <related type="Article" href="https://msdn.microsoft.com/library/a73c4aec-5d15-4e98-b962-1274021ea93d">Language-Integrated Query (LINQ)</related>
    /// <related type="Article" href="https://msdn.microsoft.com/library/24cda21e-8af8-4632-b519-c404a839b9b2">Standard Query Operators Overview</related>
    /// <related type="Article" href="https://msdn.microsoft.com/library/fb1d3ed8-d5b0-4211-a71f-dd271529294b">Expression Trees</related>
    /// <related type="Article" href="/dotnet/framework/data/adonet/sql/linq/">LINQ to SQL</related>
    public static class Queryable
    {
        internal const string InMemoryQueryableExtensionMethodsRequiresUnreferencedCode = "Enumerating in-memory collections as IQueryable can require unreferenced code because expressions referencing IQueryable extension methods can get rebound to IEnumerable extension methods. The IEnumerable extension methods could be trimmed causing the application to fail at runtime.";

        /// <summary>Converts a generic <see cref="System.Collections.Generic.IEnumerable{T}" /> to a generic <see cref="System.Linq.IQueryable{T}" />.</summary>
        /// <typeparam name="TElement">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence to convert.</param>
        /// <returns>An <see cref="System.Linq.IQueryable{T}" /> that represents the input sequence.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <remarks>If the type of <paramref name="source" /> implements <see cref="System.Linq.IQueryable{T}" />, <see cref="System.Linq.Queryable.AsQueryable{T}(System.Collections.Generic.IEnumerable{T})" /> returns it directly. Otherwise, it returns an <see cref="System.Linq.IQueryable{T}" /> that executes queries by calling the equivalent query operator methods in <see cref="System.Linq.Enumerable" /> instead of those in <see cref="System.Linq.Queryable" />.</remarks>
        /// <example>The following code example demonstrates how to use <see cref="System.Linq.Queryable.AsQueryable{T}(System.Collections.Generic.IEnumerable{T})" /> to convert an <see cref="System.Collections.Generic.IEnumerable{T}" /> to an <see cref="System.Linq.IQueryable{T}" />.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Queryable/CS/queryable.cs" interactive="try-dotnet-method" id="Snippet125":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Queryable/VB/queryable.vb" id="Snippet125":::</example>
        [RequiresUnreferencedCode(InMemoryQueryableExtensionMethodsRequiresUnreferencedCode)]
        public static IQueryable<TElement> AsQueryable<TElement>(this IEnumerable<TElement> source)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            return source as IQueryable<TElement> ?? new EnumerableQuery<TElement>(source);
        }

        /// <summary>Converts an <see cref="System.Collections.IEnumerable" /> to an <see cref="System.Linq.IQueryable" />.</summary>
        /// <param name="source">A sequence to convert.</param>
        /// <returns>An <see cref="System.Linq.IQueryable" /> that represents the input sequence.</returns>
        /// <exception cref="System.ArgumentException"><paramref name="source" /> does not implement <see cref="System.Collections.Generic.IEnumerable{T}" /> for some <see langword="T" />.</exception>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <remarks>
        /// <para>If the type of <paramref name="source" /> implements <see cref="System.Linq.IQueryable{T}" />, <see cref="System.Linq.Queryable.AsQueryable(System.Collections.IEnumerable)" /> returns it directly. Otherwise, it returns an <see cref="System.Linq.IQueryable{T}" /> that executes queries by calling the equivalent query operator methods in <see cref="System.Linq.Enumerable" /> instead of those in <see cref="System.Linq.Queryable" />.</para>
        /// <para>This method assumes that <paramref name="source" /> implements <see cref="System.Collections.Generic.IEnumerable{T}" /> for some `T`. At runtime, the result is of type <see cref="System.Linq.IQueryable{T}" /> for the same `T`. This method is useful in dynamic scenarios when you do not statically know the type of `T`.</para>
        /// </remarks>
        [RequiresUnreferencedCode(InMemoryQueryableExtensionMethodsRequiresUnreferencedCode)]
        public static IQueryable AsQueryable(this IEnumerable source)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (source is IQueryable queryable) return queryable;
            Type? enumType = TypeHelper.FindGenericType(typeof(IEnumerable<>), source.GetType());
            if (enumType == null)
                throw Error.ArgumentNotIEnumerableGeneric(nameof(source));
            return EnumerableQuery.Create(enumType.GenericTypeArguments[0], source);
        }

        /// <summary>Filters a sequence of values based on a predicate.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">An <see cref="System.Linq.IQueryable{T}" /> to filter.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <returns>An <see cref="System.Linq.IQueryable{T}" /> that contains elements from the input sequence that satisfy the condition specified by <paramref name="predicate" />.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> or <paramref name="predicate" /> is <see langword="null" />.</exception>
        /// <remarks>
        /// <para>This method has at least one parameter of type <see cref="System.Linq.Expressions.Expression{T}" /> whose type argument is one of the <see cref="System.Func{T1,T2}" /> types. For these parameters, you can pass in a lambda expression and it will be compiled to an <see cref="System.Linq.Expressions.Expression{T}" />.</para>
        /// <para>The <see cref="System.Linq.Queryable.Where{T}(System.Linq.IQueryable{T},System.Linq.Expressions.Expression{System.Func{T,bool}})" /> method generates a <see cref="System.Linq.Expressions.MethodCallExpression" /> that represents calling <see cref="System.Linq.Queryable.Where{T}(System.Linq.IQueryable{T},System.Linq.Expressions.Expression{System.Func{T,bool}})" /> itself as a constructed generic method. It then passes the <see cref="System.Linq.Expressions.MethodCallExpression" /> to the <see cref="System.Linq.IQueryProvider.CreateQuery(System.Linq.Expressions.Expression)" /> method of the <see cref="System.Linq.IQueryProvider" /> represented by the <see cref="O:System.Linq.IQueryable.Provider" /> property of the <paramref name="source" /> parameter.</para>
        /// <para>The query behavior that occurs as a result of executing an expression tree that represents calling <see cref="System.Linq.Queryable.Where{T}(System.Linq.IQueryable{T},System.Linq.Expressions.Expression{System.Func{T,bool}})" /> depends on the implementation of the type of the <paramref name="source" /> parameter. The expected behavior is that it returns the elements from <paramref name="source" /> that satisfy the condition specified by <paramref name="predicate" />.</para>
        /// </remarks>
        /// <example>The following code example demonstrates how to use <see cref="System.Linq.Queryable.Where{T}(System.Linq.IQueryable{T},System.Linq.Expressions.Expression{System.Func{T,bool}})" /> to filter a sequence.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Queryable/CS/queryable.cs" interactive="try-dotnet-method" id="Snippet110":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Queryable/VB/queryable.vb" id="Snippet110":::</example>
        [DynamicDependency("Where`1", typeof(Enumerable))]
        public static IQueryable<TSource> Where<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (predicate == null)
                throw Error.ArgumentNull(nameof(predicate));
            return source.Provider.CreateQuery<TSource>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.Where_TSource_2(typeof(TSource)),
                    source.Expression, Expression.Quote(predicate)
                    ));
        }

        /// <summary>Filters a sequence of values based on a predicate. Each element's index is used in the logic of the predicate function.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">An <see cref="System.Linq.IQueryable{T}" /> to filter.</param>
        /// <param name="predicate">A function to test each element for a condition; the second parameter of the function represents the index of the element in the source sequence.</param>
        /// <returns>An <see cref="System.Linq.IQueryable{T}" /> that contains elements from the input sequence that satisfy the condition specified by <paramref name="predicate" />.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> or <paramref name="predicate" /> is <see langword="null" />.</exception>
        /// <remarks>
        /// <para>This method has at least one parameter of type <see cref="System.Linq.Expressions.Expression{T}" /> whose type argument is one of the <see cref="System.Func{T1,T2}" /> types. For these parameters, you can pass in a lambda expression and it will be compiled to an <see cref="System.Linq.Expressions.Expression{T}" />.</para>
        /// <para>The <see cref="System.Linq.Queryable.Where{T}(System.Linq.IQueryable{T},System.Linq.Expressions.Expression{System.Func{T,int,bool}})" /> method generates a <see cref="System.Linq.Expressions.MethodCallExpression" /> that represents calling <see cref="System.Linq.Queryable.Where{T}(System.Linq.IQueryable{T},System.Linq.Expressions.Expression{System.Func{T,int,bool}})" /> itself as a constructed generic method. It then passes the <see cref="System.Linq.Expressions.MethodCallExpression" /> to the <see cref="System.Linq.IQueryProvider.CreateQuery(System.Linq.Expressions.Expression)" /> method of the <see cref="System.Linq.IQueryProvider" /> represented by the <see cref="O:System.Linq.IQueryable.Provider" /> property of the <paramref name="source" /> parameter.</para>
        /// <para>The query behavior that occurs as a result of executing an expression tree that represents calling <see cref="System.Linq.Queryable.Where{T}(System.Linq.IQueryable{T},System.Linq.Expressions.Expression{System.Func{T,int,bool}})" /> depends on the implementation of the type of the <paramref name="source" /> parameter. The expected behavior is that it returns the elements from <paramref name="source" /> that satisfy the condition specified by <paramref name="predicate" />. The index of each source element is provided as the second argument to <paramref name="predicate" />.</para>
        /// </remarks>
        /// <example>The following code example demonstrates how to use <see cref="System.Linq.Queryable.Where{T}(System.Linq.IQueryable{T},System.Linq.Expressions.Expression{System.Func{T,int,bool}})" /> to filter a sequence based on a predicate that incorporates the index of each element.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Queryable/CS/queryable.cs" interactive="try-dotnet-method" id="Snippet111":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Queryable/VB/queryable.vb" id="Snippet111":::</example>
        [DynamicDependency("Where`1", typeof(Enumerable))]
        public static IQueryable<TSource> Where<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, int, bool>> predicate)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (predicate == null)
                throw Error.ArgumentNull(nameof(predicate));
            return source.Provider.CreateQuery<TSource>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.Where_Index_TSource_2(typeof(TSource)),
                    source.Expression, Expression.Quote(predicate)
                    ));
        }

        /// <summary>Filters the elements of an <see cref="System.Linq.IQueryable" /> based on a specified type.</summary>
        /// <typeparam name="TResult">The type to filter the elements of the sequence on.</typeparam>
        /// <param name="source">An <see cref="System.Linq.IQueryable" /> whose elements to filter.</param>
        /// <returns>A collection that contains the elements from <paramref name="source" /> that have type <typeparamref name="TResult" />.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <remarks>
        /// <para>The `OfType` method generates a <see cref="System.Linq.Expressions.MethodCallExpression" /> that represents calling `OfType` itself as a constructed generic method. It then passes the <see cref="System.Linq.Expressions.MethodCallExpression" /> to the <see cref="System.Linq.IQueryProvider.CreateQuery(System.Linq.Expressions.Expression)" /> method of the <see cref="System.Linq.IQueryProvider" /> represented by the <see cref="O:System.Linq.IQueryable.Provider" /> property of the <paramref name="source" /> parameter.</para>
        /// <para>The query behavior that occurs as a result of executing an expression tree that represents calling `OfType` depends on the implementation of the type of the <paramref name="source" /> parameter. The expected behavior is that it filters out any elements in <paramref name="source" /> that are not of type <typeparamref name="TResult" />.</para>
        /// </remarks>
        /// <example>The following code example demonstrates how to use `OfType` to filter out elements that are not of type <see cref="System.Reflection.PropertyInfo" /> from a list of elements of type <see cref="System.Reflection.MemberInfo" />.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Queryable/CS/queryable.cs" id="Snippet69":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Queryable/VB/queryable.vb" id="Snippet69":::</example>
        [DynamicDependency("OfType`1", typeof(Enumerable))]
        public static IQueryable<TResult> OfType<TResult>(this IQueryable source)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            return source.Provider.CreateQuery<TResult>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.OfType_TResult_1(typeof(TResult)), source.Expression));
        }

        /// <summary>Converts the elements of an <see cref="System.Linq.IQueryable" /> to the specified type.</summary>
        /// <typeparam name="TResult">The type to convert the elements of <paramref name="source" /> to.</typeparam>
        /// <param name="source">The <see cref="System.Linq.IQueryable" /> that contains the elements to be converted.</param>
        /// <returns>An <see cref="System.Linq.IQueryable{T}" /> that contains each element of the source sequence converted to the specified type.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <exception cref="System.InvalidCastException">An element in the sequence cannot be cast to type <typeparamref name="TResult" />.</exception>
        /// <remarks>
        /// <para>The <see cref="System.Linq.Queryable.Cast{T}(System.Linq.IQueryable)" /> method generates a <see cref="System.Linq.Expressions.MethodCallExpression" /> that represents calling <see cref="System.Linq.Queryable.Cast{T}(System.Linq.IQueryable)" /> itself as a constructed generic method. It then passes the <see cref="System.Linq.Expressions.MethodCallExpression" /> to the <see cref="System.Linq.IQueryProvider.CreateQuery(System.Linq.Expressions.Expression)" /> method of the <see cref="System.Linq.IQueryProvider" /> represented by the <see cref="O:System.Linq.IQueryable.Provider" /> property of the <paramref name="source" /> parameter.</para>
        /// <para>The query behavior that occurs as a result of executing an expression tree that represents calling <see cref="System.Linq.Queryable.Cast{T}(System.Linq.IQueryable)" /> depends on the implementation of the type of the <paramref name="source" /> parameter. The expected behavior is that it converts the values in <paramref name="source" /> to type <typeparamref name="TResult" />.</para>
        /// </remarks>
        /// <example>The following code example demonstrates how to use <see cref="System.Linq.Queryable.Cast{T}(System.Linq.IQueryable)" /> to convert objects in a sequence to type <see cref="string" />.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Queryable/CS/queryable.cs" interactive="try-dotnet-method" id="Snippet19":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Queryable/VB/queryable.vb" id="Snippet19":::</example>
        [DynamicDependency("Cast`1", typeof(Enumerable))]
        public static IQueryable<TResult> Cast<TResult>(this IQueryable source)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            return source.Provider.CreateQuery<TResult>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.Cast_TResult_1(typeof(TResult)), source.Expression));
        }

        /// <summary>Projects each element of a sequence into a new form.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <typeparam name="TResult">The type of the value returned by the function represented by <paramref name="selector" />.</typeparam>
        /// <param name="source">A sequence of values to project.</param>
        /// <param name="selector">A projection function to apply to each element.</param>
        /// <returns>An <see cref="System.Linq.IQueryable{T}" /> whose elements are the result of invoking a projection function on each element of <paramref name="source" />.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> or <paramref name="selector" /> is <see langword="null" />.</exception>
        /// <remarks>
        /// <para>This method has at least one parameter of type <see cref="System.Linq.Expressions.Expression{T}" /> whose type argument is one of the <see cref="System.Func{T1,T2}" /> types. For these parameters, you can pass in a lambda expression and it will be compiled to an <see cref="System.Linq.Expressions.Expression{T}" />.</para>
        /// <para>The <see cref="System.Linq.Queryable.Select{T1,T2}(System.Linq.IQueryable{T1},System.Linq.Expressions.Expression{System.Func{T1,T2}})" /> method generates a <see cref="System.Linq.Expressions.MethodCallExpression" /> that represents calling <see cref="System.Linq.Queryable.Select{T1,T2}(System.Linq.IQueryable{T1},System.Linq.Expressions.Expression{System.Func{T1,T2}})" /> itself as a constructed generic method. It then passes the <see cref="System.Linq.Expressions.MethodCallExpression" /> to the <see cref="System.Linq.IQueryProvider.CreateQuery(System.Linq.Expressions.Expression)" /> method of the <see cref="System.Linq.IQueryProvider" /> represented by the <see cref="O:System.Linq.IQueryable.Provider" /> property of the <paramref name="source" /> parameter.</para>
        /// <para>The query behavior that occurs as a result of executing an expression tree that represents calling <see cref="System.Linq.Queryable.Select{T1,T2}(System.Linq.IQueryable{T1},System.Linq.Expressions.Expression{System.Func{T1,T2}})" /> depends on the implementation of the type of the <paramref name="source" /> parameter. The expected behavior is that it invokes <paramref name="selector" /> on each element of <paramref name="source" /> to project it into a different form.</para>
        /// </remarks>
        /// <example>The following code example demonstrates how to use <see cref="System.Linq.Queryable.Select{T1,T2}(System.Linq.IQueryable{T1},System.Linq.Expressions.Expression{System.Func{T1,T2}})" /> to project over a sequence of values.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Queryable/CS/queryable.cs" interactive="try-dotnet-method" id="Snippet75":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Queryable/VB/queryable.vb" id="Snippet75":::</example>
        [DynamicDependency("Select`2", typeof(Enumerable))]
        public static IQueryable<TResult> Select<TSource, TResult>(this IQueryable<TSource> source, Expression<Func<TSource, TResult>> selector)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (selector == null)
                throw Error.ArgumentNull(nameof(selector));
            return source.Provider.CreateQuery<TResult>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.Select_TSource_TResult_2(typeof(TSource), typeof(TResult)),
                    source.Expression, Expression.Quote(selector)
                    ));
        }

        /// <summary>Projects each element of a sequence into a new form by incorporating the element's index.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <typeparam name="TResult">The type of the value returned by the function represented by <paramref name="selector" />.</typeparam>
        /// <param name="source">A sequence of values to project.</param>
        /// <param name="selector">A projection function to apply to each element.</param>
        /// <returns>An <see cref="System.Linq.IQueryable{T}" /> whose elements are the result of invoking a projection function on each element of <paramref name="source" />.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> or <paramref name="selector" /> is <see langword="null" />.</exception>
        /// <remarks>
        /// <para>This method has at least one parameter of type <see cref="System.Linq.Expressions.Expression{T}" /> whose type argument is one of the <see cref="System.Func{T1,T2}" /> types. For these parameters, you can pass in a lambda expression and it will be compiled to an <see cref="System.Linq.Expressions.Expression{T}" />.</para>
        /// <para>The <see cref="System.Linq.Queryable.Select{T1,T2}(System.Linq.IQueryable{T1},System.Linq.Expressions.Expression{System.Func{T1,int,T2}})" /> method generates a <see cref="System.Linq.Expressions.MethodCallExpression" /> that represents calling <see cref="System.Linq.Queryable.Select{T1,T2}(System.Linq.IQueryable{T1},System.Linq.Expressions.Expression{System.Func{T1,int,T2}})" /> itself as a constructed generic method. It then passes the <see cref="System.Linq.Expressions.MethodCallExpression" /> to the <see cref="System.Linq.IQueryProvider.CreateQuery(System.Linq.Expressions.Expression)" /> method of the <see cref="System.Linq.IQueryProvider" /> represented by the <see cref="O:System.Linq.IQueryable.Provider" /> property of the <paramref name="source" /> parameter.</para>
        /// <para>The query behavior that occurs as a result of executing an expression tree that represents calling <see cref="System.Linq.Queryable.Select{T1,T2}(System.Linq.IQueryable{T1},System.Linq.Expressions.Expression{System.Func{T1,int,T2}})" /> depend on the implementation of the type of the <paramref name="source" /> parameter. The expected behavior is that it invokes <paramref name="selector" /> on each element of <paramref name="source" /> to project it into a different form.</para>
        /// </remarks>
        /// <example>The following code example demonstrates how to use <see cref="System.Linq.Queryable.Select{T1,T2}(System.Linq.IQueryable{T1},System.Linq.Expressions.Expression{System.Func{T1,int,T2}})" /> to project over a sequence of values and use the index of each element in the projected form.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Queryable/CS/queryable.cs" interactive="try-dotnet-method" id="Snippet76":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Queryable/VB/queryable.vb" id="Snippet76":::</example>
        [DynamicDependency("Select`2", typeof(Enumerable))]
        public static IQueryable<TResult> Select<TSource, TResult>(this IQueryable<TSource> source, Expression<Func<TSource, int, TResult>> selector)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (selector == null)
                throw Error.ArgumentNull(nameof(selector));
            return source.Provider.CreateQuery<TResult>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.Select_Index_TSource_TResult_2(typeof(TSource), typeof(TResult)),
                    source.Expression, Expression.Quote(selector)
                    ));
        }

        /// <summary>Projects each element of a sequence to an <see cref="System.Collections.Generic.IEnumerable{T}" /> and combines the resulting sequences into one sequence.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <typeparam name="TResult">The type of the elements of the sequence returned by the function represented by <paramref name="selector" />.</typeparam>
        /// <param name="source">A sequence of values to project.</param>
        /// <param name="selector">A projection function to apply to each element.</param>
        /// <returns>An <see cref="System.Linq.IQueryable{T}" /> whose elements are the result of invoking a one-to-many projection function on each element of the input sequence.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> or <paramref name="selector" /> is <see langword="null" />.</exception>
        /// <remarks>
        /// <para>This method has at least one parameter of type <see cref="System.Linq.Expressions.Expression{T}" /> whose type argument is one of the <see cref="System.Func{T1,T2}" /> types. For these parameters, you can pass in a lambda expression and it will be compiled to an <see cref="System.Linq.Expressions.Expression{T}" />.</para>
        /// <para>The <see cref="System.Linq.Queryable.SelectMany{T1,T2}(System.Linq.IQueryable{T1},System.Linq.Expressions.Expression{System.Func{T1,System.Collections.Generic.IEnumerable{T2}}})" /> method generates a <see cref="System.Linq.Expressions.MethodCallExpression" /> that represents calling <see cref="System.Linq.Queryable.SelectMany{T1,T2}(System.Linq.IQueryable{T1},System.Linq.Expressions.Expression{System.Func{T1,System.Collections.Generic.IEnumerable{T2}}})" /> itself as a constructed generic method. It then passes the <see cref="System.Linq.Expressions.MethodCallExpression" /> to the <see cref="System.Linq.IQueryProvider.CreateQuery(System.Linq.Expressions.Expression)" /> method of the <see cref="System.Linq.IQueryProvider" /> represented by the <see cref="O:System.Linq.IQueryable.Provider" /> property of the <paramref name="source" /> parameter.</para>
        /// <para>The query behavior that occurs as a result of executing an expression tree that represents calling <see cref="System.Linq.Queryable.SelectMany{T1,T2}(System.Linq.IQueryable{T1},System.Linq.Expressions.Expression{System.Func{T1,System.Collections.Generic.IEnumerable{T2}}})" /> depends on the implementation of the type of the <paramref name="source" /> parameter. The expected behavior is that it invokes <paramref name="selector" /> on each element of <paramref name="source" /> to project it into an enumerable form. It then concatenates the enumerable results into a single, one-dimensional sequence.</para>
        /// </remarks>
        /// <example>The following code example demonstrates how to use <see cref="System.Linq.Queryable.SelectMany{T1,T2}(System.Linq.IQueryable{T1},System.Linq.Expressions.Expression{System.Func{T1,System.Collections.Generic.IEnumerable{T2}}})" /> to perform a one-to-many projection over an array.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Queryable/CS/queryable.cs" id="Snippet77":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Queryable/VB/queryable.vb" id="Snippet77":::</example>
        [DynamicDependency("SelectMany`2", typeof(Enumerable))]
        public static IQueryable<TResult> SelectMany<TSource, TResult>(this IQueryable<TSource> source, Expression<Func<TSource, IEnumerable<TResult>>> selector)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (selector == null)
                throw Error.ArgumentNull(nameof(selector));
            return source.Provider.CreateQuery<TResult>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.SelectMany_TSource_TResult_2(typeof(TSource), typeof(TResult)),
                    source.Expression, Expression.Quote(selector)
                    ));
        }

        /// <summary>Projects each element of a sequence to an <see cref="System.Collections.Generic.IEnumerable{T}" /> and combines the resulting sequences into one sequence. The index of each source element is used in the projected form of that element.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <typeparam name="TResult">The type of the elements of the sequence returned by the function represented by <paramref name="selector" />.</typeparam>
        /// <param name="source">A sequence of values to project.</param>
        /// <param name="selector">A projection function to apply to each element; the second parameter of this function represents the index of the source element.</param>
        /// <returns>An <see cref="System.Linq.IQueryable{T}" /> whose elements are the result of invoking a one-to-many projection function on each element of the input sequence.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> or <paramref name="selector" /> is <see langword="null" />.</exception>
        /// <remarks>
        /// <para>This method has at least one parameter of type <see cref="System.Linq.Expressions.Expression{T}" /> whose type argument is one of the <see cref="System.Func{T1,T2}" /> types. For these parameters, you can pass in a lambda expression and it will be compiled to an <see cref="System.Linq.Expressions.Expression{T}" />.</para>
        /// <para>The <see cref="System.Linq.Queryable.SelectMany{T1,T2}(System.Linq.IQueryable{T1},System.Linq.Expressions.Expression{System.Func{T1,int,System.Collections.Generic.IEnumerable{T2}}})" /> method generates a <see cref="System.Linq.Expressions.MethodCallExpression" /> that represents calling <see cref="System.Linq.Queryable.SelectMany{T1,T2}(System.Linq.IQueryable{T1},System.Linq.Expressions.Expression{System.Func{T1,int,System.Collections.Generic.IEnumerable{T2}}})" /> itself as a constructed generic method. It then passes the <see cref="System.Linq.Expressions.MethodCallExpression" /> to the <see cref="System.Linq.IQueryProvider.CreateQuery(System.Linq.Expressions.Expression)" /> method of the <see cref="System.Linq.IQueryProvider" /> represented by the <see cref="O:System.Linq.IQueryable.Provider" /> property of the <paramref name="source" /> parameter.</para>
        /// <para>The query behavior that occurs as a result of executing an expression tree that represents calling <see cref="System.Linq.Queryable.SelectMany{T1,T2}(System.Linq.IQueryable{T1},System.Linq.Expressions.Expression{System.Func{T1,int,System.Collections.Generic.IEnumerable{T2}}})" /> depends on the implementation of the type of the <paramref name="source" /> parameter. The expected behavior is that it invokes <paramref name="selector" /> on each element of <paramref name="source" /> to project it into an enumerable form. Each enumerable result incorporates the index of the source element. It then concatenates the enumerable results into a single, one-dimensional sequence.</para>
        /// </remarks>
        /// <example>The following code example demonstrates how to use <see cref="System.Linq.Queryable.SelectMany{T1,T2}(System.Linq.IQueryable{T1},System.Linq.Expressions.Expression{System.Func{T1,int,System.Collections.Generic.IEnumerable{T2}}})" /> to perform a one-to-many projection over an array and use the index of each source element.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Queryable/CS/queryable.cs" id="Snippet78":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Queryable/VB/queryable.vb" id="Snippet78":::</example>
        [DynamicDependency("SelectMany`2", typeof(Enumerable))]
        public static IQueryable<TResult> SelectMany<TSource, TResult>(this IQueryable<TSource> source, Expression<Func<TSource, int, IEnumerable<TResult>>> selector)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (selector == null)
                throw Error.ArgumentNull(nameof(selector));
            return source.Provider.CreateQuery<TResult>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.SelectMany_Index_TSource_TResult_2(typeof(TSource), typeof(TResult)),
                    source.Expression, Expression.Quote(selector)
                    ));
        }

        /// <summary>Projects each element of a sequence to an <see cref="System.Collections.Generic.IEnumerable{T}" /> that incorporates the index of the source element that produced it. A result selector function is invoked on each element of each intermediate sequence, and the resulting values are combined into a single, one-dimensional sequence and returned.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <typeparam name="TCollection">The type of the intermediate elements collected by the function represented by <paramref name="collectionSelector" />.</typeparam>
        /// <typeparam name="TResult">The type of the elements of the resulting sequence.</typeparam>
        /// <param name="source">A sequence of values to project.</param>
        /// <param name="collectionSelector">A projection function to apply to each element of the input sequence; the second parameter of this function represents the index of the source element.</param>
        /// <param name="resultSelector">A projection function to apply to each element of each intermediate sequence.</param>
        /// <returns>An <see cref="System.Linq.IQueryable{T}" /> whose elements are the result of invoking the one-to-many projection function <paramref name="collectionSelector" /> on each element of <paramref name="source" /> and then mapping each of those sequence elements and their corresponding <paramref name="source" /> element to a result element.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> or <paramref name="collectionSelector" /> or <paramref name="resultSelector" /> is <see langword="null" />.</exception>
        /// <remarks>
        /// <para>This method has at least one parameter of type <see cref="System.Linq.Expressions.Expression{T}" /> whose type argument is one of the <see cref="System.Func{T1,T2}" /> types. For these parameters, you can pass in a lambda expression and it will be compiled to an <see cref="System.Linq.Expressions.Expression{T}" />.</para>
        /// <para>The <see cref="System.Linq.Queryable.SelectMany{T1,T2,T3}(System.Linq.IQueryable{T1},System.Linq.Expressions.Expression{System.Func{T1,int,System.Collections.Generic.IEnumerable{T2}}},System.Linq.Expressions.Expression{System.Func{T1,T2,T3}})" /> method generates a <see cref="System.Linq.Expressions.MethodCallExpression" /> that represents calling <see cref="System.Linq.Queryable.SelectMany{T1,T2,T3}(System.Linq.IQueryable{T1},System.Linq.Expressions.Expression{System.Func{T1,int,System.Collections.Generic.IEnumerable{T2}}},System.Linq.Expressions.Expression{System.Func{T1,T2,T3}})" /> itself as a constructed generic method. It then passes the <see cref="System.Linq.Expressions.MethodCallExpression" /> to the <see cref="System.Linq.IQueryProvider.CreateQuery(System.Linq.Expressions.Expression)" /> method of the <see cref="System.Linq.IQueryProvider" /> represented by the <see cref="O:System.Linq.IQueryable.Provider" /> property of the <paramref name="source" /> parameter.</para>
        /// <para>The query behavior that occurs as a result of executing an expression tree that represents calling <see cref="System.Linq.Queryable.SelectMany{T1,T2,T3}(System.Linq.IQueryable{T1},System.Linq.Expressions.Expression{System.Func{T1,int,System.Collections.Generic.IEnumerable{T2}}},System.Linq.Expressions.Expression{System.Func{T1,T2,T3}})" /> depends on the implementation of the type of the <paramref name="source" /> parameter. The expected behavior is that it invokes <paramref name="collectionSelector" /> on each element of <paramref name="source" /> to project it into an enumerable form. Each enumerable result incorporates the source element's index. Then the function represented by <paramref name="resultSelector" /> is invoked on each element in each intermediate sequence. The resulting values are concatenated into a single, one-dimensional sequence.</para>
        /// </remarks>
        [DynamicDependency("SelectMany`3", typeof(Enumerable))]
        public static IQueryable<TResult> SelectMany<TSource, TCollection, TResult>(this IQueryable<TSource> source, Expression<Func<TSource, int, IEnumerable<TCollection>>> collectionSelector, Expression<Func<TSource, TCollection, TResult>> resultSelector)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (collectionSelector == null)
                throw Error.ArgumentNull(nameof(collectionSelector));
            if (resultSelector == null)
                throw Error.ArgumentNull(nameof(resultSelector));
            return source.Provider.CreateQuery<TResult>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.SelectMany_Index_TSource_TCollection_TResult_3(typeof(TSource), typeof(TCollection), typeof(TResult)),
                    source.Expression, Expression.Quote(collectionSelector), Expression.Quote(resultSelector)
                    ));
        }

        /// <summary>Projects each element of a sequence to an <see cref="System.Collections.Generic.IEnumerable{T}" /> and invokes a result selector function on each element therein. The resulting values from each intermediate sequence are combined into a single, one-dimensional sequence and returned.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <typeparam name="TCollection">The type of the intermediate elements collected by the function represented by <paramref name="collectionSelector" />.</typeparam>
        /// <typeparam name="TResult">The type of the elements of the resulting sequence.</typeparam>
        /// <param name="source">A sequence of values to project.</param>
        /// <param name="collectionSelector">A projection function to apply to each element of the input sequence.</param>
        /// <param name="resultSelector">A projection function to apply to each element of each intermediate sequence.</param>
        /// <returns>An <see cref="System.Linq.IQueryable{T}" /> whose elements are the result of invoking the one-to-many projection function <paramref name="collectionSelector" /> on each element of <paramref name="source" /> and then mapping each of those sequence elements and their corresponding <paramref name="source" /> element to a result element.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> or <paramref name="collectionSelector" /> or <paramref name="resultSelector" /> is <see langword="null" />.</exception>
        /// <remarks>
        /// <para>This method has at least one parameter of type <see cref="System.Linq.Expressions.Expression{T}" /> whose type argument is one of the <see cref="System.Func{T1,T2}" /> types. For these parameters, you can pass in a lambda expression and it will be compiled to an <see cref="System.Linq.Expressions.Expression{T}" />.</para>
        /// <para>The <see cref="System.Linq.Queryable.SelectMany{T1,T2,T3}(System.Linq.IQueryable{T1},System.Linq.Expressions.Expression{System.Func{T1,System.Collections.Generic.IEnumerable{T2}}},System.Linq.Expressions.Expression{System.Func{T1,T2,T3}})" /> method generates a <see cref="System.Linq.Expressions.MethodCallExpression" /> that represents calling <see cref="System.Linq.Queryable.SelectMany{T1,T2,T3}(System.Linq.IQueryable{T1},System.Linq.Expressions.Expression{System.Func{T1,System.Collections.Generic.IEnumerable{T2}}},System.Linq.Expressions.Expression{System.Func{T1,T2,T3}})" /> itself as a constructed generic method. It then passes the <see cref="System.Linq.Expressions.MethodCallExpression" /> to the <see cref="System.Linq.IQueryProvider.CreateQuery(System.Linq.Expressions.Expression)" /> method of the <see cref="System.Linq.IQueryProvider" /> represented by the <see cref="O:System.Linq.IQueryable.Provider" /> property of the <paramref name="source" /> parameter.</para>
        /// <para>The query behavior that occurs as a result of executing an expression tree that represents calling <see cref="System.Linq.Queryable.SelectMany{T1,T2,T3}(System.Linq.IQueryable{T1},System.Linq.Expressions.Expression{System.Func{T1,System.Collections.Generic.IEnumerable{T2}}},System.Linq.Expressions.Expression{System.Func{T1,T2,T3}})" /> depends on the implementation of the type of the <paramref name="source" /> parameter. The expected behavior is that it invokes <paramref name="collectionSelector" /> on each element of <paramref name="source" /> to project it into an enumerable form. Then the function represented by <paramref name="resultSelector" /> is invoked on each element in each intermediate sequence. The resulting values are concatenated into a single, one-dimensional sequence.</para>
        /// </remarks>
        /// <example>The following code example demonstrates how to use <see cref="System.Linq.Queryable.SelectMany{T1,T2,T3}(System.Linq.IQueryable{T1},System.Linq.Expressions.Expression{System.Func{T1,System.Collections.Generic.IEnumerable{T2}}},System.Linq.Expressions.Expression{System.Func{T1,T2,T3}})" /> to perform a one-to-many projection over an array. This example uses a result selector function to keep the source element that corresponds to each intermediate sequence in scope for the final call to `Select`.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Queryable/CS/queryable.cs" id="Snippet124":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Queryable/VB/queryable.vb" id="Snippet124":::</example>
        [DynamicDependency("SelectMany`3", typeof(Enumerable))]
        public static IQueryable<TResult> SelectMany<TSource, TCollection, TResult>(this IQueryable<TSource> source, Expression<Func<TSource, IEnumerable<TCollection>>> collectionSelector, Expression<Func<TSource, TCollection, TResult>> resultSelector)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (collectionSelector == null)
                throw Error.ArgumentNull(nameof(collectionSelector));
            if (resultSelector == null)
                throw Error.ArgumentNull(nameof(resultSelector));
            return source.Provider.CreateQuery<TResult>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.SelectMany_TSource_TCollection_TResult_3(typeof(TSource), typeof(TCollection), typeof(TResult)),
                    source.Expression, Expression.Quote(collectionSelector), Expression.Quote(resultSelector)
                    ));
        }

        private static Expression GetSourceExpression<TSource>(IEnumerable<TSource> source)
        {
            IQueryable<TSource>? q = source as IQueryable<TSource>;
            return q != null ? q.Expression : Expression.Constant(source, typeof(IEnumerable<TSource>));
        }

        /// <summary>Correlates the elements of two sequences based on matching keys. The default equality comparer is used to compare keys.</summary>
        /// <typeparam name="TOuter">The type of the elements of the first sequence.</typeparam>
        /// <typeparam name="TInner">The type of the elements of the second sequence.</typeparam>
        /// <typeparam name="TKey">The type of the keys returned by the key selector functions.</typeparam>
        /// <typeparam name="TResult">The type of the result elements.</typeparam>
        /// <param name="outer">The first sequence to join.</param>
        /// <param name="inner">The sequence to join to the first sequence.</param>
        /// <param name="outerKeySelector">A function to extract the join key from each element of the first sequence.</param>
        /// <param name="innerKeySelector">A function to extract the join key from each element of the second sequence.</param>
        /// <param name="resultSelector">A function to create a result element from two matching elements.</param>
        /// <returns>An <see cref="System.Linq.IQueryable{T}" /> that has elements of type <typeparamref name="TResult" /> obtained by performing an inner join on two sequences.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="outer" /> or <paramref name="inner" /> or <paramref name="outerKeySelector" /> or <paramref name="innerKeySelector" /> or <paramref name="resultSelector" /> is <see langword="null" />.</exception>
        /// <remarks>
        /// <para>This method has at least one parameter of type <see cref="System.Linq.Expressions.Expression{T}" /> whose type argument is one of the <see cref="System.Func{T1,T2}" /> types. For these parameters, you can pass in a lambda expression and it will be compiled to an <see cref="System.Linq.Expressions.Expression{T}" />.</para>
        /// <para>The <see cref="System.Linq.Queryable.Join{T1,T2,T3,T4}(System.Linq.IQueryable{T1},System.Collections.Generic.IEnumerable{T2},System.Linq.Expressions.Expression{System.Func{T1,T3}},System.Linq.Expressions.Expression{System.Func{T2,T3}},System.Linq.Expressions.Expression{System.Func{T1,T2,T4}})" /> method generates a <see cref="System.Linq.Expressions.MethodCallExpression" /> that represents calling <see cref="System.Linq.Queryable.Join{T1,T2,T3,T4}(System.Linq.IQueryable{T1},System.Collections.Generic.IEnumerable{T2},System.Linq.Expressions.Expression{System.Func{T1,T3}},System.Linq.Expressions.Expression{System.Func{T2,T3}},System.Linq.Expressions.Expression{System.Func{T1,T2,T4}})" /> itself as a constructed generic method. It then passes the <see cref="System.Linq.Expressions.MethodCallExpression" /> to the <see cref="System.Linq.IQueryProvider.CreateQuery{T}(System.Linq.Expressions.Expression)" /> method of the <see cref="System.Linq.IQueryProvider" /> represented by the <see cref="O:System.Linq.IQueryable.Provider" /> property of the <paramref name="outer" /> parameter.</para>
        /// <para>The query behavior that occurs as a result of executing an expression tree that represents calling <see cref="System.Linq.Queryable.Join{T1,T2,T3,T4}(System.Linq.IQueryable{T1},System.Collections.Generic.IEnumerable{T2},System.Linq.Expressions.Expression{System.Func{T1,T3}},System.Linq.Expressions.Expression{System.Func{T2,T3}},System.Linq.Expressions.Expression{System.Func{T1,T2,T4}})" /> depends on the implementation of the type of the <paramref name="outer" /> parameter. The expected behavior is that of an inner join. The <paramref name="outerKeySelector" /> and <paramref name="innerKeySelector" /> functions are used to extract keys from <paramref name="outer" /> and <paramref name="inner" />, respectively. These keys are compared for equality to match elements from each sequence. A pair of elements is stored for each element in <paramref name="inner" /> that matches an element in <paramref name="outer" />. Then the <paramref name="resultSelector" /> function is invoked to project a result object from each pair of matching elements.</para>
        /// </remarks>
        /// <example>The following code example demonstrates how to use <see cref="System.Linq.Queryable.Join{T1,T2,T3,T4}(System.Linq.IQueryable{T1},System.Collections.Generic.IEnumerable{T2},System.Linq.Expressions.Expression{System.Func{T1,T3}},System.Linq.Expressions.Expression{System.Func{T2,T3}},System.Linq.Expressions.Expression{System.Func{T1,T2,T4}})" /> to perform an inner join of two sequences based on a common key.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Queryable/CS/queryable.cs" id="Snippet42":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Queryable/VB/queryable.vb" id="Snippet42":::</example>
        [DynamicDependency("Join`4", typeof(Enumerable))]
        public static IQueryable<TResult> Join<TOuter, TInner, TKey, TResult>(this IQueryable<TOuter> outer, IEnumerable<TInner> inner, Expression<Func<TOuter, TKey>> outerKeySelector, Expression<Func<TInner, TKey>> innerKeySelector, Expression<Func<TOuter, TInner, TResult>> resultSelector)
        {
            if (outer == null)
                throw Error.ArgumentNull(nameof(outer));
            if (inner == null)
                throw Error.ArgumentNull(nameof(inner));
            if (outerKeySelector == null)
                throw Error.ArgumentNull(nameof(outerKeySelector));
            if (innerKeySelector == null)
                throw Error.ArgumentNull(nameof(innerKeySelector));
            if (resultSelector == null)
                throw Error.ArgumentNull(nameof(resultSelector));
            return outer.Provider.CreateQuery<TResult>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.Join_TOuter_TInner_TKey_TResult_5(typeof(TOuter), typeof(TInner), typeof(TKey), typeof(TResult)), outer.Expression, GetSourceExpression(inner), Expression.Quote(outerKeySelector), Expression.Quote(innerKeySelector), Expression.Quote(resultSelector)));
        }

        /// <summary>Correlates the elements of two sequences based on matching keys. A specified <see cref="System.Collections.Generic.IEqualityComparer{T}" /> is used to compare keys.</summary>
        /// <typeparam name="TOuter">The type of the elements of the first sequence.</typeparam>
        /// <typeparam name="TInner">The type of the elements of the second sequence.</typeparam>
        /// <typeparam name="TKey">The type of the keys returned by the key selector functions.</typeparam>
        /// <typeparam name="TResult">The type of the result elements.</typeparam>
        /// <param name="outer">The first sequence to join.</param>
        /// <param name="inner">The sequence to join to the first sequence.</param>
        /// <param name="outerKeySelector">A function to extract the join key from each element of the first sequence.</param>
        /// <param name="innerKeySelector">A function to extract the join key from each element of the second sequence.</param>
        /// <param name="resultSelector">A function to create a result element from two matching elements.</param>
        /// <param name="comparer">An <see cref="System.Collections.Generic.IEqualityComparer{T}" /> to hash and compare keys.</param>
        /// <returns>An <see cref="System.Linq.IQueryable{T}" /> that has elements of type <typeparamref name="TResult" /> obtained by performing an inner join on two sequences.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="outer" /> or <paramref name="inner" /> or <paramref name="outerKeySelector" /> or <paramref name="innerKeySelector" /> or <paramref name="resultSelector" /> is <see langword="null" />.</exception>
        /// <remarks>
        /// <para>This method has at least one parameter of type <see cref="System.Linq.Expressions.Expression{T}" /> whose type argument is one of the <see cref="System.Func{T1,T2}" /> types. For these parameters, you can pass in a lambda expression and it will be compiled to an <see cref="System.Linq.Expressions.Expression{T}" />.</para>
        /// <para>The <see cref="System.Linq.Queryable.Join{T1,T2,T3,T4}(System.Linq.IQueryable{T1},System.Collections.Generic.IEnumerable{T2},System.Linq.Expressions.Expression{System.Func{T1,T3}},System.Linq.Expressions.Expression{System.Func{T2,T3}},System.Linq.Expressions.Expression{System.Func{T1,T2,T4}},System.Collections.Generic.IEqualityComparer{T3})" /> method generates a <see cref="System.Linq.Expressions.MethodCallExpression" /> that represents calling <see cref="System.Linq.Queryable.Join{T1,T2,T3,T4}(System.Linq.IQueryable{T1},System.Collections.Generic.IEnumerable{T2},System.Linq.Expressions.Expression{System.Func{T1,T3}},System.Linq.Expressions.Expression{System.Func{T2,T3}},System.Linq.Expressions.Expression{System.Func{T1,T2,T4}},System.Collections.Generic.IEqualityComparer{T3})" /> itself as a constructed generic method. It then passes the <see cref="System.Linq.Expressions.MethodCallExpression" /> to the <see cref="System.Linq.IQueryProvider.CreateQuery{T}(System.Linq.Expressions.Expression)" /> method of the <see cref="System.Linq.IQueryProvider" /> represented by the <see cref="O:System.Linq.IQueryable.Provider" /> property of the <paramref name="outer" /> parameter.</para>
        /// <para>The query behavior that occurs as a result of executing an expression tree that represents calling <see cref="System.Linq.Queryable.Join{T1,T2,T3,T4}(System.Linq.IQueryable{T1},System.Collections.Generic.IEnumerable{T2},System.Linq.Expressions.Expression{System.Func{T1,T3}},System.Linq.Expressions.Expression{System.Func{T2,T3}},System.Linq.Expressions.Expression{System.Func{T1,T2,T4}},System.Collections.Generic.IEqualityComparer{T3})" /> depends on the implementation of the type of the <paramref name="outer" /> parameter. The expected behavior is that of an inner join. The <paramref name="outerKeySelector" /> and <paramref name="innerKeySelector" /> functions are used to extract keys from <paramref name="outer" /> and <paramref name="inner" />, respectively. These keys are compared for equality by using <paramref name="comparer" />. The outcome of the comparisons is used to create a matching pair for each element in <paramref name="inner" /> that matches an element in <paramref name="outer" />. Then the <paramref name="resultSelector" /> function is invoked to project a result object from each pair of matching elements.</para>
        /// </remarks>
        [DynamicDependency("Join`4", typeof(Enumerable))]
        public static IQueryable<TResult> Join<TOuter, TInner, TKey, TResult>(this IQueryable<TOuter> outer, IEnumerable<TInner> inner, Expression<Func<TOuter, TKey>> outerKeySelector, Expression<Func<TInner, TKey>> innerKeySelector, Expression<Func<TOuter, TInner, TResult>> resultSelector, IEqualityComparer<TKey>? comparer)
        {
            if (outer == null)
                throw Error.ArgumentNull(nameof(outer));
            if (inner == null)
                throw Error.ArgumentNull(nameof(inner));
            if (outerKeySelector == null)
                throw Error.ArgumentNull(nameof(outerKeySelector));
            if (innerKeySelector == null)
                throw Error.ArgumentNull(nameof(innerKeySelector));
            if (resultSelector == null)
                throw Error.ArgumentNull(nameof(resultSelector));
            return outer.Provider.CreateQuery<TResult>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.Join_TOuter_TInner_TKey_TResult_6(typeof(TOuter), typeof(TInner), typeof(TKey), typeof(TResult)), outer.Expression, GetSourceExpression(inner), Expression.Quote(outerKeySelector), Expression.Quote(innerKeySelector), Expression.Quote(resultSelector), Expression.Constant(comparer, typeof(IEqualityComparer<TKey>))));
        }

        /// <summary>Correlates the elements of two sequences based on key equality and groups the results. The default equality comparer is used to compare keys.</summary>
        /// <typeparam name="TOuter">The type of the elements of the first sequence.</typeparam>
        /// <typeparam name="TInner">The type of the elements of the second sequence.</typeparam>
        /// <typeparam name="TKey">The type of the keys returned by the key selector functions.</typeparam>
        /// <typeparam name="TResult">The type of the result elements.</typeparam>
        /// <param name="outer">The first sequence to join.</param>
        /// <param name="inner">The sequence to join to the first sequence.</param>
        /// <param name="outerKeySelector">A function to extract the join key from each element of the first sequence.</param>
        /// <param name="innerKeySelector">A function to extract the join key from each element of the second sequence.</param>
        /// <param name="resultSelector">A function to create a result element from an element from the first sequence and a collection of matching elements from the second sequence.</param>
        /// <returns>An <see cref="System.Linq.IQueryable{T}" /> that contains elements of type <typeparamref name="TResult" /> obtained by performing a grouped join on two sequences.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="outer" /> or <paramref name="inner" /> or <paramref name="outerKeySelector" /> or <paramref name="innerKeySelector" /> or <paramref name="resultSelector" /> is <see langword="null" />.</exception>
        /// <remarks>
        /// <para>This method has at least one parameter of type <see cref="System.Linq.Expressions.Expression{T}" /> whose type argument is one of the <see cref="System.Func{T1,T2}" /> types. For these parameters, you can pass in a lambda expression and it will be compiled to an <see cref="System.Linq.Expressions.Expression{T}" />.</para>
        /// <para>The <see cref="System.Linq.Queryable.GroupJoin{T1,T2,T3,T4}(System.Linq.IQueryable{T1},System.Collections.Generic.IEnumerable{T2},System.Linq.Expressions.Expression{System.Func{T1,T3}},System.Linq.Expressions.Expression{System.Func{T2,T3}},System.Linq.Expressions.Expression{System.Func{T1,System.Collections.Generic.IEnumerable{T2},T4}})" /> method generates a <see cref="System.Linq.Expressions.MethodCallExpression" /> that represents calling <see cref="System.Linq.Queryable.GroupJoin{T1,T2,T3,T4}(System.Linq.IQueryable{T1},System.Collections.Generic.IEnumerable{T2},System.Linq.Expressions.Expression{System.Func{T1,T3}},System.Linq.Expressions.Expression{System.Func{T2,T3}},System.Linq.Expressions.Expression{System.Func{T1,System.Collections.Generic.IEnumerable{T2},T4}})" /> itself as a constructed generic method. It then passes the <see cref="System.Linq.Expressions.MethodCallExpression" /> to the <see cref="System.Linq.IQueryProvider.CreateQuery{T}(System.Linq.Expressions.Expression)" /> method of the <see cref="System.Linq.IQueryProvider" /> represented by the <see cref="O:System.Linq.IQueryable.Provider" /> property of the <paramref name="outer" /> parameter.</para>
        /// <para>The query behavior that occurs as a result of executing an expression tree that represents calling <see cref="System.Linq.Queryable.GroupJoin{T1,T2,T3,T4}(System.Linq.IQueryable{T1},System.Collections.Generic.IEnumerable{T2},System.Linq.Expressions.Expression{System.Func{T1,T3}},System.Linq.Expressions.Expression{System.Func{T2,T3}},System.Linq.Expressions.Expression{System.Func{T1,System.Collections.Generic.IEnumerable{T2},T4}})" /> depends on the implementation of the type of the <paramref name="outer" /> parameter. The expected behavior is that the <paramref name="outerKeySelector" /> and <paramref name="innerKeySelector" /> functions are used to extract keys from <paramref name="outer" /> and <paramref name="inner" />, respectively. These keys are compared for equality to match each element in <paramref name="outer" /> with zero or more elements from <paramref name="inner" />. Then the <paramref name="resultSelector" /> function is invoked to project a result object from each group of correlated elements.</para>
        /// </remarks>
        /// <example>The following code example demonstrates how to use <see cref="System.Linq.Queryable.GroupJoin{T1,T2,T3,T4}(System.Linq.IQueryable{T1},System.Collections.Generic.IEnumerable{T2},System.Linq.Expressions.Expression{System.Func{T1,T3}},System.Linq.Expressions.Expression{System.Func{T2,T3}},System.Linq.Expressions.Expression{System.Func{T1,System.Collections.Generic.IEnumerable{T2},T4}})" /> to perform a grouped join on two sequences.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Queryable/CS/queryable.cs" id="Snippet40":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Queryable/VB/queryable.vb" id="Snippet40":::</example>
        [DynamicDependency("GroupJoin`4", typeof(Enumerable))]
        public static IQueryable<TResult> GroupJoin<TOuter, TInner, TKey, TResult>(this IQueryable<TOuter> outer, IEnumerable<TInner> inner, Expression<Func<TOuter, TKey>> outerKeySelector, Expression<Func<TInner, TKey>> innerKeySelector, Expression<Func<TOuter, IEnumerable<TInner>, TResult>> resultSelector)
        {
            if (outer == null)
                throw Error.ArgumentNull(nameof(outer));
            if (inner == null)
                throw Error.ArgumentNull(nameof(inner));
            if (outerKeySelector == null)
                throw Error.ArgumentNull(nameof(outerKeySelector));
            if (innerKeySelector == null)
                throw Error.ArgumentNull(nameof(innerKeySelector));
            if (resultSelector == null)
                throw Error.ArgumentNull(nameof(resultSelector));
            return outer.Provider.CreateQuery<TResult>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.GroupJoin_TOuter_TInner_TKey_TResult_5(typeof(TOuter), typeof(TInner), typeof(TKey), typeof(TResult)), outer.Expression, GetSourceExpression(inner), Expression.Quote(outerKeySelector), Expression.Quote(innerKeySelector), Expression.Quote(resultSelector)));
        }

        /// <summary>Correlates the elements of two sequences based on key equality and groups the results. A specified <see cref="System.Collections.Generic.IEqualityComparer{T}" /> is used to compare keys.</summary>
        /// <typeparam name="TOuter">The type of the elements of the first sequence.</typeparam>
        /// <typeparam name="TInner">The type of the elements of the second sequence.</typeparam>
        /// <typeparam name="TKey">The type of the keys returned by the key selector functions.</typeparam>
        /// <typeparam name="TResult">The type of the result elements.</typeparam>
        /// <param name="outer">The first sequence to join.</param>
        /// <param name="inner">The sequence to join to the first sequence.</param>
        /// <param name="outerKeySelector">A function to extract the join key from each element of the first sequence.</param>
        /// <param name="innerKeySelector">A function to extract the join key from each element of the second sequence.</param>
        /// <param name="resultSelector">A function to create a result element from an element from the first sequence and a collection of matching elements from the second sequence.</param>
        /// <param name="comparer">A comparer to hash and compare keys.</param>
        /// <returns>An <see cref="System.Linq.IQueryable{T}" /> that contains elements of type <typeparamref name="TResult" /> obtained by performing a grouped join on two sequences.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="outer" /> or <paramref name="inner" /> or <paramref name="outerKeySelector" /> or <paramref name="innerKeySelector" /> or <paramref name="resultSelector" /> is <see langword="null" />.</exception>
        /// <remarks>
        /// <para>This method has at least one parameter of type <see cref="System.Linq.Expressions.Expression{T}" /> whose type argument is one of the <see cref="System.Func{T1,T2}" /> types. For these parameters, you can pass in a lambda expression and it will be compiled to an <see cref="System.Linq.Expressions.Expression{T}" />.</para>
        /// <para>The <see cref="System.Linq.Queryable.GroupJoin{T1,T2,T3,T4}(System.Linq.IQueryable{T1},System.Collections.Generic.IEnumerable{T2},System.Linq.Expressions.Expression{System.Func{T1,T3}},System.Linq.Expressions.Expression{System.Func{T2,T3}},System.Linq.Expressions.Expression{System.Func{T1,System.Collections.Generic.IEnumerable{T2},T4}},System.Collections.Generic.IEqualityComparer{T3})" /> method generates a <see cref="System.Linq.Expressions.MethodCallExpression" /> that represents calling <see cref="System.Linq.Queryable.GroupJoin{T1,T2,T3,T4}(System.Linq.IQueryable{T1},System.Collections.Generic.IEnumerable{T2},System.Linq.Expressions.Expression{System.Func{T1,T3}},System.Linq.Expressions.Expression{System.Func{T2,T3}},System.Linq.Expressions.Expression{System.Func{T1,System.Collections.Generic.IEnumerable{T2},T4}},System.Collections.Generic.IEqualityComparer{T3})" /> itself as a constructed generic method. It then passes the <see cref="System.Linq.Expressions.MethodCallExpression" /> to the <see cref="System.Linq.IQueryProvider.CreateQuery{T}(System.Linq.Expressions.Expression)" /> method of the <see cref="System.Linq.IQueryProvider" /> represented by the <see cref="O:System.Linq.IQueryable.Provider" /> property of the <paramref name="outer" /> parameter.</para>
        /// <para>The query behavior that occurs as a result of executing an expression tree that represents calling <see cref="System.Linq.Queryable.GroupJoin{T1,T2,T3,T4}(System.Linq.IQueryable{T1},System.Collections.Generic.IEnumerable{T2},System.Linq.Expressions.Expression{System.Func{T1,T3}},System.Linq.Expressions.Expression{System.Func{T2,T3}},System.Linq.Expressions.Expression{System.Func{T1,System.Collections.Generic.IEnumerable{T2},T4}},System.Collections.Generic.IEqualityComparer{T3})" /> depends on the implementation of the type of the <paramref name="outer" /> parameter. The expected behavior is that the <paramref name="outerKeySelector" /> and <paramref name="innerKeySelector" /> functions are used to extract keys from <paramref name="outer" /> and <paramref name="inner" />, respectively. These keys are compared for equality by using <paramref name="comparer" />. The outcome of the comparisons is used to match each element in <paramref name="outer" /> with zero or more elements from <paramref name="inner" />. Then the <paramref name="resultSelector" /> function is invoked to project a result object from each group of correlated elements.</para>
        /// </remarks>
        [DynamicDependency("GroupJoin`4", typeof(Enumerable))]
        public static IQueryable<TResult> GroupJoin<TOuter, TInner, TKey, TResult>(this IQueryable<TOuter> outer, IEnumerable<TInner> inner, Expression<Func<TOuter, TKey>> outerKeySelector, Expression<Func<TInner, TKey>> innerKeySelector, Expression<Func<TOuter, IEnumerable<TInner>, TResult>> resultSelector, IEqualityComparer<TKey>? comparer)
        {
            if (outer == null)
                throw Error.ArgumentNull(nameof(outer));
            if (inner == null)
                throw Error.ArgumentNull(nameof(inner));
            if (outerKeySelector == null)
                throw Error.ArgumentNull(nameof(outerKeySelector));
            if (innerKeySelector == null)
                throw Error.ArgumentNull(nameof(innerKeySelector));
            if (resultSelector == null)
                throw Error.ArgumentNull(nameof(resultSelector));
            return outer.Provider.CreateQuery<TResult>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.GroupJoin_TOuter_TInner_TKey_TResult_6(typeof(TOuter), typeof(TInner), typeof(TKey), typeof(TResult)), outer.Expression, GetSourceExpression(inner), Expression.Quote(outerKeySelector), Expression.Quote(innerKeySelector), Expression.Quote(resultSelector), Expression.Constant(comparer, typeof(IEqualityComparer<TKey>))));
        }

        /// <summary>Sorts the elements of a sequence in ascending order according to a key.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by the function that is represented by <paramref name="keySelector" />.</typeparam>
        /// <param name="source">A sequence of values to order.</param>
        /// <param name="keySelector">A function to extract a key from an element.</param>
        /// <returns>An <see cref="System.Linq.IOrderedQueryable{T}" /> whose elements are sorted according to a key.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> or <paramref name="keySelector" /> is <see langword="null" />.</exception>
        /// <remarks>
        /// <para>This method has at least one parameter of type <see cref="System.Linq.Expressions.Expression{T}" /> whose type argument is one of the <see cref="System.Func{T1,T2}" /> types. For these parameters, you can pass in a lambda expression and it will be compiled to an <see cref="System.Linq.Expressions.Expression{T}" />.</para>
        /// <para>The <see cref="System.Linq.Queryable.OrderBy{T1,T2}(System.Linq.IQueryable{T1},System.Linq.Expressions.Expression{System.Func{T1,T2}})" /> method generates a <see cref="System.Linq.Expressions.MethodCallExpression" /> that represents calling <see cref="System.Linq.Queryable.OrderBy{T1,T2}(System.Linq.IQueryable{T1},System.Linq.Expressions.Expression{System.Func{T1,T2}})" /> itself as a constructed generic method. It then passes the <see cref="System.Linq.Expressions.MethodCallExpression" /> to the <see cref="System.Linq.IQueryProvider.CreateQuery{T}(System.Linq.Expressions.Expression)" /> method of the <see cref="System.Linq.IQueryProvider" /> represented by the <see cref="O:System.Linq.IQueryable.Provider" /> property of the <paramref name="source" /> parameter. The result of calling <see cref="System.Linq.IQueryProvider.CreateQuery{T}(System.Linq.Expressions.Expression)" /> is cast to type <see cref="System.Linq.IOrderedQueryable{T}" /> and returned.</para>
        /// <para>The query behavior that occurs as a result of executing an expression tree that represents calling <see cref="System.Linq.Queryable.OrderBy{T1,T2}(System.Linq.IQueryable{T1},System.Linq.Expressions.Expression{System.Func{T1,T2}})" /> depends on the implementation of the type of the <paramref name="source" /> parameter. The expected behavior is that it sorts the elements of <paramref name="source" /> based on the key obtained by invoking <paramref name="keySelector" /> on each element of <paramref name="source" />.</para>
        /// </remarks>
        /// <example>The following code example demonstrates how to use <see cref="System.Linq.Queryable.OrderBy{T1,T2}(System.Linq.IQueryable{T1},System.Linq.Expressions.Expression{System.Func{T1,T2}})" /> to sort the elements of a sequence.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Queryable/CS/queryable.cs" id="Snippet70":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Queryable/VB/queryable.vb" id="Snippet70":::</example>
        [DynamicDependency("OrderBy`2", typeof(Enumerable))]
        public static IOrderedQueryable<TSource> OrderBy<TSource, TKey>(this IQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (keySelector == null)
                throw Error.ArgumentNull(nameof(keySelector));
            return (IOrderedQueryable<TSource>)source.Provider.CreateQuery<TSource>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.OrderBy_TSource_TKey_2(typeof(TSource), typeof(TKey)),
                    source.Expression, Expression.Quote(keySelector)
                    ));
        }

        /// <summary>Sorts the elements of a sequence in ascending order by using a specified comparer.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by the function that is represented by <paramref name="keySelector" />.</typeparam>
        /// <param name="source">A sequence of values to order.</param>
        /// <param name="keySelector">A function to extract a key from an element.</param>
        /// <param name="comparer">An <see cref="System.Collections.Generic.IComparer{T}" /> to compare keys.</param>
        /// <returns>An <see cref="System.Linq.IOrderedQueryable{T}" /> whose elements are sorted according to a key.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> or <paramref name="keySelector" /> or <paramref name="comparer" /> is <see langword="null" />.</exception>
        /// <remarks>
        /// <para>This method has at least one parameter of type <see cref="System.Linq.Expressions.Expression{T}" /> whose type argument is one of the <see cref="System.Func{T1,T2}" /> types. For these parameters, you can pass in a lambda expression and it will be compiled to an <see cref="System.Linq.Expressions.Expression{T}" />.</para>
        /// <para>The <see cref="System.Linq.Queryable.OrderBy{T1,T2}(System.Linq.IQueryable{T1},System.Linq.Expressions.Expression{System.Func{T1,T2}},System.Collections.Generic.IComparer{T2})" /> method generates a <see cref="System.Linq.Expressions.MethodCallExpression" /> that represents calling <see cref="System.Linq.Queryable.OrderBy{T1,T2}(System.Linq.IQueryable{T1},System.Linq.Expressions.Expression{System.Func{T1,T2}},System.Collections.Generic.IComparer{T2})" /> itself as a constructed generic method. It then passes the <see cref="System.Linq.Expressions.MethodCallExpression" /> to the <see cref="System.Linq.IQueryProvider.CreateQuery{T}(System.Linq.Expressions.Expression)" /> method of the <see cref="System.Linq.IQueryProvider" /> represented by the <see cref="O:System.Linq.IQueryable.Provider" /> property of the <paramref name="source" /> parameter. The result of calling <see cref="System.Linq.IQueryProvider.CreateQuery{T}(System.Linq.Expressions.Expression)" /> is cast to type <see cref="System.Linq.IOrderedQueryable{T}" /> and returned.</para>
        /// <para>The query behavior that occurs as a result of executing an expression tree that represents calling <see cref="System.Linq.Queryable.OrderBy{T1,T2}(System.Linq.IQueryable{T1},System.Linq.Expressions.Expression{System.Func{T1,T2}},System.Collections.Generic.IComparer{T2})" /> depends on the implementation of the type of the <paramref name="source" /> parameter. The expected behavior is that it sorts the elements of <paramref name="source" /> based on the key obtained by invoking <paramref name="keySelector" /> on each element of <paramref name="source" />. The <paramref name="comparer" /> parameter is used to compare keys.</para>
        /// </remarks>
        [DynamicDependency("OrderBy`2", typeof(Enumerable))]
        public static IOrderedQueryable<TSource> OrderBy<TSource, TKey>(this IQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector, IComparer<TKey>? comparer)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (keySelector == null)
                throw Error.ArgumentNull(nameof(keySelector));
            return (IOrderedQueryable<TSource>)source.Provider.CreateQuery<TSource>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.OrderBy_TSource_TKey_3(typeof(TSource), typeof(TKey)),
                    source.Expression, Expression.Quote(keySelector), Expression.Constant(comparer, typeof(IComparer<TKey>))
                    ));
        }

        /// <summary>Sorts the elements of a sequence in descending order according to a key.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by the function that is represented by <paramref name="keySelector" />.</typeparam>
        /// <param name="source">A sequence of values to order.</param>
        /// <param name="keySelector">A function to extract a key from an element.</param>
        /// <returns>An <see cref="System.Linq.IOrderedQueryable{T}" /> whose elements are sorted in descending order according to a key.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> or <paramref name="keySelector" /> is <see langword="null" />.</exception>
        /// <remarks>
        /// <para>This method has at least one parameter of type <see cref="System.Linq.Expressions.Expression{T}" /> whose type argument is one of the <see cref="System.Func{T1,T2}" /> types. For these parameters, you can pass in a lambda expression and it will be compiled to an <see cref="System.Linq.Expressions.Expression{T}" />.</para>
        /// <para>The <see cref="System.Linq.Queryable.OrderByDescending{T1,T2}(System.Linq.IQueryable{T1},System.Linq.Expressions.Expression{System.Func{T1,T2}})" /> method generates a <see cref="System.Linq.Expressions.MethodCallExpression" /> that represents calling <see cref="System.Linq.Queryable.OrderByDescending{T1,T2}(System.Linq.IQueryable{T1},System.Linq.Expressions.Expression{System.Func{T1,T2}})" /> itself as a constructed generic method. It then passes the <see cref="System.Linq.Expressions.MethodCallExpression" /> to the <see cref="System.Linq.IQueryProvider.CreateQuery{T}(System.Linq.Expressions.Expression)" /> method of the <see cref="System.Linq.IQueryProvider" /> represented by the <see cref="O:System.Linq.IQueryable.Provider" /> property of the <paramref name="source" /> parameter. The result of calling <see cref="System.Linq.IQueryProvider.CreateQuery{T}(System.Linq.Expressions.Expression)" /> is cast to type <see cref="System.Linq.IOrderedQueryable{T}" /> and returned.</para>
        /// <para>The query behavior that occurs as a result of executing an expression tree that represents calling <see cref="System.Linq.Queryable.OrderByDescending{T1,T2}(System.Linq.IQueryable{T1},System.Linq.Expressions.Expression{System.Func{T1,T2}})" /> depends on the implementation of the type of the <paramref name="source" /> parameter. The expected behavior is that it sorts the elements of <paramref name="source" /> in descending order, based on the key obtained by invoking <paramref name="keySelector" /> on each element of <paramref name="source" />.</para>
        /// </remarks>
        [DynamicDependency("OrderByDescending`2", typeof(Enumerable))]
        public static IOrderedQueryable<TSource> OrderByDescending<TSource, TKey>(this IQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (keySelector == null)
                throw Error.ArgumentNull(nameof(keySelector));
            return (IOrderedQueryable<TSource>)source.Provider.CreateQuery<TSource>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.OrderByDescending_TSource_TKey_2(typeof(TSource), typeof(TKey)),
                    source.Expression, Expression.Quote(keySelector)
                    ));
        }

        /// <summary>Sorts the elements of a sequence in descending order by using a specified comparer.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by the function that is represented by <paramref name="keySelector" />.</typeparam>
        /// <param name="source">A sequence of values to order.</param>
        /// <param name="keySelector">A function to extract a key from an element.</param>
        /// <param name="comparer">An <see cref="System.Collections.Generic.IComparer{T}" /> to compare keys.</param>
        /// <returns>An <see cref="System.Linq.IOrderedQueryable{T}" /> whose elements are sorted in descending order according to a key.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> or <paramref name="keySelector" /> or <paramref name="comparer" /> is <see langword="null" />.</exception>
        /// <remarks>
        /// <para>This method has at least one parameter of type <see cref="System.Linq.Expressions.Expression{T}" /> whose type argument is one of the <see cref="System.Func{T1,T2}" /> types. For these parameters, you can pass in a lambda expression and it will be compiled to an <see cref="System.Linq.Expressions.Expression{T}" />.</para>
        /// <para>The <see cref="System.Linq.Queryable.OrderByDescending{T1,T2}(System.Linq.IQueryable{T1},System.Linq.Expressions.Expression{System.Func{T1,T2}},System.Collections.Generic.IComparer{T2})" /> method generates a <see cref="System.Linq.Expressions.MethodCallExpression" /> that represents calling <see cref="System.Linq.Queryable.OrderByDescending{T1,T2}(System.Linq.IQueryable{T1},System.Linq.Expressions.Expression{System.Func{T1,T2}},System.Collections.Generic.IComparer{T2})" /> itself as a constructed generic method. It then passes the <see cref="System.Linq.Expressions.MethodCallExpression" /> to the <see cref="System.Linq.IQueryProvider.CreateQuery{T}(System.Linq.Expressions.Expression)" /> method of the <see cref="System.Linq.IQueryProvider" /> represented by the <see cref="O:System.Linq.IQueryable.Provider" /> property of the <paramref name="source" /> parameter. The result of calling <see cref="System.Linq.IQueryProvider.CreateQuery{T}(System.Linq.Expressions.Expression)" /> is cast to type <see cref="System.Linq.IOrderedQueryable{T}" /> and returned.</para>
        /// <para>The query behavior that occurs as a result of executing an expression tree that represents calling <see cref="System.Linq.Queryable.OrderByDescending{T1,T2}(System.Linq.IQueryable{T1},System.Linq.Expressions.Expression{System.Func{T1,T2}},System.Collections.Generic.IComparer{T2})" /> depends on the implementation of the type of the <paramref name="source" /> parameter. The expected behavior is that it sorts the elements of <paramref name="source" /> in descending order, based on the key obtained by invoking <paramref name="keySelector" /> on each element of <paramref name="source" />. The <paramref name="comparer" /> parameter is used to compare keys.</para>
        /// </remarks>
        /// <example>The following code example demonstrates how to use <see cref="System.Linq.Queryable.OrderByDescending{T1,T2}(System.Linq.IQueryable{T1},System.Linq.Expressions.Expression{System.Func{T1,T2}},System.Collections.Generic.IComparer{T2})" /> to sort the elements of a sequence in descending order by using a custom comparer.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Queryable/CS/queryable.cs" id="Snippet71":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Queryable/VB/queryable.vb" id="Snippet71":::</example>
        [DynamicDependency("OrderByDescending`2", typeof(Enumerable))]
        public static IOrderedQueryable<TSource> OrderByDescending<TSource, TKey>(this IQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector, IComparer<TKey>? comparer)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (keySelector == null)
                throw Error.ArgumentNull(nameof(keySelector));
            return (IOrderedQueryable<TSource>)source.Provider.CreateQuery<TSource>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.OrderByDescending_TSource_TKey_3(typeof(TSource), typeof(TKey)),
                    source.Expression, Expression.Quote(keySelector), Expression.Constant(comparer, typeof(IComparer<TKey>))
                    ));
        }

        /// <summary>Performs a subsequent ordering of the elements in a sequence in ascending order according to a key.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by the function represented by <paramref name="keySelector" />.</typeparam>
        /// <param name="source">An <see cref="System.Linq.IOrderedQueryable{T}" /> that contains elements to sort.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <returns>An <see cref="System.Linq.IOrderedQueryable{T}" /> whose elements are sorted according to a key.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> or <paramref name="keySelector" /> is <see langword="null" />.</exception>
        /// <remarks>
        /// <para>This method has at least one parameter of type <see cref="System.Linq.Expressions.Expression{T}" /> whose type argument is one of the <see cref="System.Func{T1,T2}" /> types. For these parameters, you can pass in a lambda expression and it will be compiled to an <see cref="System.Linq.Expressions.Expression{T}" />.</para>
        /// <para>The <see cref="System.Linq.Queryable.ThenBy{T1,T2}(System.Linq.IOrderedQueryable{T1},System.Linq.Expressions.Expression{System.Func{T1,T2}})" /> method generates a <see cref="System.Linq.Expressions.MethodCallExpression" /> that represents calling <see cref="System.Linq.Queryable.ThenBy{T1,T2}(System.Linq.IOrderedQueryable{T1},System.Linq.Expressions.Expression{System.Func{T1,T2}})" /> itself as a constructed generic method. It then passes the <see cref="System.Linq.Expressions.MethodCallExpression" /> to the <see cref="System.Linq.IQueryProvider.CreateQuery{T}(System.Linq.Expressions.Expression)" /> method of the <see cref="System.Linq.IQueryProvider" /> represented by the <see cref="O:System.Linq.IQueryable.Provider" /> property of the <paramref name="source" /> parameter. The result of calling <see cref="System.Linq.IQueryProvider.CreateQuery{T}(System.Linq.Expressions.Expression)" /> is cast to type <see cref="System.Linq.IOrderedQueryable{T}" /> and returned.</para>
        /// <para>The query behavior that occurs as a result of executing an expression tree that represents calling <see cref="System.Linq.Queryable.ThenBy{T1,T2}(System.Linq.IOrderedQueryable{T1},System.Linq.Expressions.Expression{System.Func{T1,T2}})" /> depends on the implementation of the type of the <paramref name="source" /> parameter. The expected behavior is that it performs a secondary sort of the elements of <paramref name="source" /> based on the key obtained by invoking <paramref name="keySelector" /> on each element of <paramref name="source" />. All previously established sort orders are preserved.</para>
        /// </remarks>
        /// <example>The following code example demonstrates how to use <see cref="System.Linq.Queryable.ThenBy{T1,T2}(System.Linq.IOrderedQueryable{T1},System.Linq.Expressions.Expression{System.Func{T1,T2}})" /> to perform a secondary ordering of the elements in a sequence.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Queryable/CS/queryable.cs" interactive="try-dotnet-method" id="Snippet102":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Queryable/VB/queryable.vb" id="Snippet102":::</example>
        [DynamicDependency("ThenBy`2", typeof(Enumerable))]
        public static IOrderedQueryable<TSource> ThenBy<TSource, TKey>(this IOrderedQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (keySelector == null)
                throw Error.ArgumentNull(nameof(keySelector));
            return (IOrderedQueryable<TSource>)source.Provider.CreateQuery<TSource>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.ThenBy_TSource_TKey_2(typeof(TSource), typeof(TKey)),
                    source.Expression, Expression.Quote(keySelector)
                    ));
        }

        /// <summary>Performs a subsequent ordering of the elements in a sequence in ascending order by using a specified comparer.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by the function represented by <paramref name="keySelector" />.</typeparam>
        /// <param name="source">An <see cref="System.Linq.IOrderedQueryable{T}" /> that contains elements to sort.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="comparer">An <see cref="System.Collections.Generic.IComparer{T}" /> to compare keys.</param>
        /// <returns>An <see cref="System.Linq.IOrderedQueryable{T}" /> whose elements are sorted according to a key.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> or <paramref name="keySelector" /> or <paramref name="comparer" /> is <see langword="null" />.</exception>
        /// <remarks>
        /// <para>This method has at least one parameter of type <see cref="System.Linq.Expressions.Expression{T}" /> whose type argument is one of the <see cref="System.Func{T1,T2}" /> types. For these parameters, you can pass in a lambda expression and it will be compiled to an <see cref="System.Linq.Expressions.Expression{T}" />.</para>
        /// <para>The <see cref="System.Linq.Queryable.ThenBy{T1,T2}(System.Linq.IOrderedQueryable{T1},System.Linq.Expressions.Expression{System.Func{T1,T2}},System.Collections.Generic.IComparer{T2})" /> method generates a <see cref="System.Linq.Expressions.MethodCallExpression" /> that represents calling <see cref="System.Linq.Queryable.ThenBy{T1,T2}(System.Linq.IOrderedQueryable{T1},System.Linq.Expressions.Expression{System.Func{T1,T2}},System.Collections.Generic.IComparer{T2})" /> itself as a constructed generic method. It then passes the <see cref="System.Linq.Expressions.MethodCallExpression" /> to the <see cref="System.Linq.IQueryProvider.CreateQuery{T}(System.Linq.Expressions.Expression)" /> method of the <see cref="System.Linq.IQueryProvider" /> represented by the <see cref="O:System.Linq.IQueryable.Provider" /> property of the <paramref name="source" /> parameter. The result of calling <see cref="System.Linq.IQueryProvider.CreateQuery{T}(System.Linq.Expressions.Expression)" /> is cast to type <see cref="System.Linq.IOrderedQueryable{T}" /> and returned.</para>
        /// <para>The query behavior that occurs as a result of executing an expression tree that represents calling <see cref="System.Linq.Queryable.ThenBy{T1,T2}(System.Linq.IOrderedQueryable{T1},System.Linq.Expressions.Expression{System.Func{T1,T2}},System.Collections.Generic.IComparer{T2})" /> depends on the implementation of the type of the <paramref name="source" /> parameter. The expected behavior is that it performs a secondary sort of the elements of <paramref name="source" /> based on the key obtained by invoking <paramref name="keySelector" /> on each element of <paramref name="source" />. All previously established sort orders are preserved. The <paramref name="comparer" /> parameter is used to compare key values.</para>
        /// </remarks>
        [DynamicDependency("ThenBy`2", typeof(Enumerable))]
        public static IOrderedQueryable<TSource> ThenBy<TSource, TKey>(this IOrderedQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector, IComparer<TKey>? comparer)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (keySelector == null)
                throw Error.ArgumentNull(nameof(keySelector));
            return (IOrderedQueryable<TSource>)source.Provider.CreateQuery<TSource>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.ThenBy_TSource_TKey_3(typeof(TSource), typeof(TKey)),
                    source.Expression, Expression.Quote(keySelector), Expression.Constant(comparer, typeof(IComparer<TKey>))
                    ));
        }

        /// <summary>Performs a subsequent ordering of the elements in a sequence in descending order, according to a key.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by the function represented by <paramref name="keySelector" />.</typeparam>
        /// <param name="source">An <see cref="System.Linq.IOrderedQueryable{T}" /> that contains elements to sort.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <returns>An <see cref="System.Linq.IOrderedQueryable{T}" /> whose elements are sorted in descending order according to a key.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> or <paramref name="keySelector" /> is <see langword="null" />.</exception>
        /// <remarks>
        /// <para>This method has at least one parameter of type <see cref="System.Linq.Expressions.Expression{T}" /> whose type argument is one of the <see cref="System.Func{T1,T2}" /> types. For these parameters, you can pass in a lambda expression and it will be compiled to an <see cref="System.Linq.Expressions.Expression{T}" />.</para>
        /// <para>The <see cref="System.Linq.Queryable.ThenByDescending{T1,T2}(System.Linq.IOrderedQueryable{T1},System.Linq.Expressions.Expression{System.Func{T1,T2}})" /> method generates a <see cref="System.Linq.Expressions.MethodCallExpression" /> that represents calling <see cref="System.Linq.Queryable.ThenByDescending{T1,T2}(System.Linq.IOrderedQueryable{T1},System.Linq.Expressions.Expression{System.Func{T1,T2}})" /> itself as a constructed generic method. It then passes the <see cref="System.Linq.Expressions.MethodCallExpression" /> to the <see cref="System.Linq.IQueryProvider.CreateQuery{T}(System.Linq.Expressions.Expression)" /> method of the <see cref="System.Linq.IQueryProvider" /> represented by the <see cref="O:System.Linq.IQueryable.Provider" /> property of the <paramref name="source" /> parameter. The result of calling <see cref="System.Linq.IQueryProvider.CreateQuery{T}(System.Linq.Expressions.Expression)" /> is cast to type <see cref="System.Linq.IOrderedQueryable{T}" /> and returned.</para>
        /// <para>The query behavior that occurs as a result of executing an expression tree that represents calling <see cref="System.Linq.Queryable.ThenByDescending{T1,T2}(System.Linq.IOrderedQueryable{T1},System.Linq.Expressions.Expression{System.Func{T1,T2}})" /> depends on the implementation of the type of the <paramref name="source" /> parameter. The expected behavior is that it performs a secondary sort of the elements of <paramref name="source" /> in descending order, based on the key obtained by invoking <paramref name="keySelector" /> on each element of <paramref name="source" />. All previously established sort orders are preserved.</para>
        /// </remarks>
        [DynamicDependency("ThenByDescending`2", typeof(Enumerable))]
        public static IOrderedQueryable<TSource> ThenByDescending<TSource, TKey>(this IOrderedQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (keySelector == null)
                throw Error.ArgumentNull(nameof(keySelector));
            return (IOrderedQueryable<TSource>)source.Provider.CreateQuery<TSource>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.ThenByDescending_TSource_TKey_2(typeof(TSource), typeof(TKey)),
                    source.Expression, Expression.Quote(keySelector)
                    ));
        }

        /// <summary>Performs a subsequent ordering of the elements in a sequence in descending order by using a specified comparer.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <typeparam name="TKey">The type of the key that is returned by the <paramref name="keySelector" /> function.</typeparam>
        /// <param name="source">An <see cref="System.Linq.IOrderedQueryable{T}" /> that contains elements to sort.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="comparer">An <see cref="System.Collections.Generic.IComparer{T}" /> to compare keys.</param>
        /// <returns>A collection whose elements are sorted in descending order according to a key.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> or <paramref name="keySelector" /> or <paramref name="comparer" /> is <see langword="null" />.</exception>
        /// <remarks>
        /// <para>This method has at least one parameter of type <see cref="System.Linq.Expressions.Expression{T}" /> whose type argument is one of the <see cref="System.Func{T1,T2}" /> types. For these parameters, you can pass in a lambda expression and it will be compiled to an <see cref="System.Linq.Expressions.Expression{T}" />.</para>
        /// <para>The <see cref="System.Linq.Queryable.ThenBy{T1,T2}(System.Linq.IOrderedQueryable{T1},System.Linq.Expressions.Expression{System.Func{T1,T2}},System.Collections.Generic.IComparer{T2})" /> method generates a <see cref="System.Linq.Expressions.MethodCallExpression" /> that represents calling <see cref="System.Linq.Queryable.ThenBy{T1,T2}(System.Linq.IOrderedQueryable{T1},System.Linq.Expressions.Expression{System.Func{T1,T2}},System.Collections.Generic.IComparer{T2})" /> itself as a constructed generic method. It then passes the <see cref="System.Linq.Expressions.MethodCallExpression" /> to the <see cref="System.Linq.IQueryProvider.CreateQuery{T}(System.Linq.Expressions.Expression)" /> method of the <see cref="System.Linq.IQueryProvider" /> represented by the <see cref="O:System.Linq.IQueryable.Provider" /> property of the <paramref name="source" /> parameter. The result of calling <see cref="System.Linq.IQueryProvider.CreateQuery{T}(System.Linq.Expressions.Expression)" /> is cast to type <see cref="System.Linq.IOrderedQueryable{T}" /> and returned.</para>
        /// <para>The query behavior that occurs as a result of executing an expression tree that represents calling <see cref="System.Linq.Queryable.ThenBy{T1,T2}(System.Linq.IOrderedQueryable{T1},System.Linq.Expressions.Expression{System.Func{T1,T2}},System.Collections.Generic.IComparer{T2})" /> depends on the implementation of the type of the <paramref name="source" /> parameter. The expected behavior is that it performs a secondary sort of the elements of <paramref name="source" /> in descending order, based on the key obtained by invoking <paramref name="keySelector" /> on each element of <paramref name="source" />. All previously established sort orders are preserved. The <paramref name="comparer" /> parameter is used to compare key values.</para>
        /// </remarks>
        /// <example>The following code example demonstrates how to use <see cref="System.Linq.Queryable.ThenByDescending{T1,T2}(System.Linq.IOrderedQueryable{T1},System.Linq.Expressions.Expression{System.Func{T1,T2}},System.Collections.Generic.IComparer{T2})" /> to perform a secondary ordering of the elements in a sequence in descending order by using a custom comparer.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Queryable/CS/queryable.cs" id="Snippet103":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Queryable/VB/queryable.vb" id="Snippet103":::</example>
        [DynamicDependency("ThenByDescending`2", typeof(Enumerable))]
        public static IOrderedQueryable<TSource> ThenByDescending<TSource, TKey>(this IOrderedQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector, IComparer<TKey>? comparer)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (keySelector == null)
                throw Error.ArgumentNull(nameof(keySelector));
            return (IOrderedQueryable<TSource>)source.Provider.CreateQuery<TSource>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.ThenByDescending_TSource_TKey_3(typeof(TSource), typeof(TKey)),
                    source.Expression, Expression.Quote(keySelector), Expression.Constant(comparer, typeof(IComparer<TKey>))
                    ));
        }

        /// <summary>Returns a specified number of contiguous elements from the start of a sequence.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">The sequence to return elements from.</param>
        /// <param name="count">The number of elements to return.</param>
        /// <returns>An <see cref="System.Linq.IQueryable{T}" /> that contains the specified number of elements from the start of <paramref name="source" />.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <remarks>
        /// <para>The <see cref="System.Linq.Queryable.Take{T}(System.Linq.IQueryable{T},int)" /> method generates a <see cref="System.Linq.Expressions.MethodCallExpression" /> that represents calling <see cref="System.Linq.Queryable.Take{T}(System.Linq.IQueryable{T},int)" /> itself as a constructed generic method. It then passes the <see cref="System.Linq.Expressions.MethodCallExpression" /> to the <see cref="System.Linq.IQueryProvider.CreateQuery(System.Linq.Expressions.Expression)" /> method of the <see cref="System.Linq.IQueryProvider" /> represented by the <see cref="O:System.Linq.IQueryable.Provider" /> property of the <paramref name="source" /> parameter.</para>
        /// <para>The query behavior that occurs as a result of executing an expression tree that represents calling <see cref="System.Linq.Queryable.Take{T}(System.Linq.IQueryable{T},int)" /> depends on the implementation of the type of the <paramref name="source" /> parameter. The expected behavior is that it takes the first <paramref name="count" /> elements from the start of <paramref name="source" />.</para>
        /// </remarks>
        /// <example>The following code example demonstrates how to use <see cref="System.Linq.Queryable.Take{T}(System.Linq.IQueryable{T},int)" /> to return elements from the start of a sequence.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Queryable/CS/queryable.cs" interactive="try-dotnet-method" id="Snippet99":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Queryable/VB/queryable.vb" id="Snippet99":::</example>
        [DynamicDependency("Take`1", typeof(Enumerable))]
        public static IQueryable<TSource> Take<TSource>(this IQueryable<TSource> source, int count)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            return source.Provider.CreateQuery<TSource>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.Take_Int32_TSource_2(typeof(TSource)),
                    source.Expression, Expression.Constant(count)
                    ));
        }

        /// <summary>Returns a specified range of contiguous elements from a sequence.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">The sequence to return elements from.</param>
        /// <param name="range">The range of elements to return, which has start and end indexes either from the start or the end.</param>
        /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <returns>An <see cref="IQueryable{T}" /> that contains the specified <paramref name="range" /> of elements from the <paramref name="source" /> sequence.</returns>
        [DynamicDependency("Take`1", typeof(Enumerable))]
        public static IQueryable<TSource> Take<TSource>(this IQueryable<TSource> source, Range range)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            return source.Provider.CreateQuery<TSource>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.Take_Range_TSource_2(typeof(TSource)),
                    source.Expression, Expression.Constant(range)
                    ));
        }

        /// <summary>Returns elements from a sequence as long as a specified condition is true.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">The sequence to return elements from.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <returns>An <see cref="System.Linq.IQueryable{T}" /> that contains elements from the input sequence occurring before the element at which the test specified by <paramref name="predicate" /> no longer passes.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> or <paramref name="predicate" /> is <see langword="null" />.</exception>
        /// <remarks>
        /// <para>This method has at least one parameter of type <see cref="System.Linq.Expressions.Expression{T}" /> whose type argument is one of the <see cref="System.Func{T1,T2}" /> types. For these parameters, you can pass in a lambda expression and it will be compiled to an <see cref="System.Linq.Expressions.Expression{T}" />.</para>
        /// <para>The <see cref="System.Linq.Queryable.TakeWhile{T}(System.Linq.IQueryable{T},System.Linq.Expressions.Expression{System.Func{T,bool}})" /> method generates a <see cref="System.Linq.Expressions.MethodCallExpression" /> that represents calling <see cref="System.Linq.Queryable.TakeWhile{T}(System.Linq.IQueryable{T},System.Linq.Expressions.Expression{System.Func{T,bool}})" /> itself as a constructed generic method. It then passes the <see cref="System.Linq.Expressions.MethodCallExpression" /> to the <see cref="System.Linq.IQueryProvider.CreateQuery(System.Linq.Expressions.Expression)" /> method of the <see cref="System.Linq.IQueryProvider" /> represented by the <see cref="O:System.Linq.IQueryable.Provider" /> property of the <paramref name="source" /> parameter.</para>
        /// <para>The query behavior that occurs as a result of executing an expression tree that represents calling <see cref="System.Linq.Queryable.TakeWhile{T}(System.Linq.IQueryable{T},System.Linq.Expressions.Expression{System.Func{T,bool}})" /> depends on the implementation of the type of the <paramref name="source" /> parameter. The expected behavior is that it applies <paramref name="predicate" /> to each element in <paramref name="source" /> until it finds an element for which <paramref name="predicate" /> returns <see langword="false" />. It returns all the elements up until that point.</para>
        /// </remarks>
        /// <example>The following code example demonstrates how to use <see cref="System.Linq.Queryable.TakeWhile{T}(System.Linq.IQueryable{T},System.Linq.Expressions.Expression{System.Func{T,bool}})" /> to return elements from the start of a sequence as long as a condition is true.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Queryable/CS/queryable.cs" interactive="try-dotnet-method" id="Snippet100":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Queryable/VB/queryable.vb" id="Snippet100":::</example>
        [DynamicDependency("TakeWhile`1", typeof(Enumerable))]
        public static IQueryable<TSource> TakeWhile<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (predicate == null)
                throw Error.ArgumentNull(nameof(predicate));
            return source.Provider.CreateQuery<TSource>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.TakeWhile_TSource_2(typeof(TSource)),
                    source.Expression, Expression.Quote(predicate)
                    ));
        }

        /// <summary>Returns elements from a sequence as long as a specified condition is true. The element's index is used in the logic of the predicate function.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">The sequence to return elements from.</param>
        /// <param name="predicate">A function to test each element for a condition; the second parameter of the function represents the index of the element in the source sequence.</param>
        /// <returns>An <see cref="System.Linq.IQueryable{T}" /> that contains elements from the input sequence occurring before the element at which the test specified by <paramref name="predicate" /> no longer passes.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> or <paramref name="predicate" /> is <see langword="null" />.</exception>
        /// <remarks>
        /// <para>This method has at least one parameter of type <see cref="System.Linq.Expressions.Expression{T}" /> whose type argument is one of the <see cref="System.Func{T1,T2}" /> types. For these parameters, you can pass in a lambda expression and it will be compiled to an <see cref="System.Linq.Expressions.Expression{T}" />.</para>
        /// <para>The <see cref="System.Linq.Queryable.TakeWhile{T}(System.Linq.IQueryable{T},System.Linq.Expressions.Expression{System.Func{T,int,bool}})" /> method generates a <see cref="System.Linq.Expressions.MethodCallExpression" /> that represents calling <see cref="System.Linq.Queryable.TakeWhile{T}(System.Linq.IQueryable{T},System.Linq.Expressions.Expression{System.Func{T,int,bool}})" /> itself as a constructed generic method. It then passes the <see cref="System.Linq.Expressions.MethodCallExpression" /> to the <see cref="System.Linq.IQueryProvider.CreateQuery(System.Linq.Expressions.Expression)" /> method of the <see cref="System.Linq.IQueryProvider" /> represented by the <see cref="O:System.Linq.IQueryable.Provider" /> property of the <paramref name="source" /> parameter.</para>
        /// <para>The query behavior that occurs as a result of executing an expression tree that represents calling <see cref="System.Linq.Queryable.TakeWhile{T}(System.Linq.IQueryable{T},System.Linq.Expressions.Expression{System.Func{T,int,bool}})" /> depends on the implementation of the type of the <paramref name="source" /> parameter. The expected behavior is that it applies <paramref name="predicate" /> to each element in <paramref name="source" /> until it finds an element for which <paramref name="predicate" /> returns <see langword="false" />. It returns all the elements up until that point. The index of each source element is provided as the second argument to <paramref name="predicate" />.</para>
        /// </remarks>
        /// <example>The following code example demonstrates how to use <see cref="System.Linq.Queryable.TakeWhile{T}(System.Linq.IQueryable{T},System.Linq.Expressions.Expression{System.Func{T,int,bool}})" /> to return elements from the start of a sequence as long as a condition that uses the index of the element is true.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Queryable/CS/queryable.cs" interactive="try-dotnet-method" id="Snippet101":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Queryable/VB/queryable.vb" id="Snippet101":::</example>
        [DynamicDependency("TakeWhile`1", typeof(Enumerable))]
        public static IQueryable<TSource> TakeWhile<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, int, bool>> predicate)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (predicate == null)
                throw Error.ArgumentNull(nameof(predicate));
            return source.Provider.CreateQuery<TSource>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.TakeWhile_Index_TSource_2(typeof(TSource)),
                    source.Expression, Expression.Quote(predicate)
                    ));
        }

        /// <summary>Bypasses a specified number of elements in a sequence and then returns the remaining elements.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">An <see cref="System.Linq.IQueryable{T}" /> to return elements from.</param>
        /// <param name="count">The number of elements to skip before returning the remaining elements.</param>
        /// <returns>An <see cref="System.Linq.IQueryable{T}" /> that contains elements that occur after the specified index in the input sequence.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <remarks>
        /// <para>The <see cref="System.Linq.Queryable.Skip{T}(System.Linq.IQueryable{T},int)" /> method generates a <see cref="System.Linq.Expressions.MethodCallExpression" /> that represents calling <see cref="System.Linq.Queryable.Skip{T}(System.Linq.IQueryable{T},int)" /> itself as a constructed generic method. It then passes the <see cref="System.Linq.Expressions.MethodCallExpression" /> to the <see cref="System.Linq.IQueryProvider.CreateQuery(System.Linq.Expressions.Expression)" /> method of the <see cref="System.Linq.IQueryProvider" /> represented by the <see cref="O:System.Linq.IQueryable.Provider" /> property of the <paramref name="source" /> parameter.</para>
        /// <para>The query behavior that occurs as a result of executing an expression tree that represents calling <see cref="System.Linq.Queryable.Skip{T}(System.Linq.IQueryable{T},int)" /> depends on the implementation of the type of the <paramref name="source" /> parameter. The expected behavior is that it skips the first <paramref name="count" /> elements in <paramref name="source" /> and returns the remaining elements.</para>
        /// </remarks>
        /// <example>The following code example demonstrates how to use <see cref="System.Linq.Queryable.Skip{T}(System.Linq.IQueryable{T},int)" /> to skip a specified number of elements in a sorted array and return the remaining elements.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Queryable/CS/queryable.cs" interactive="try-dotnet-method" id="Snippet87":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Queryable/VB/queryable.vb" id="Snippet87":::</example>
        [DynamicDependency("Skip`1", typeof(Enumerable))]
        public static IQueryable<TSource> Skip<TSource>(this IQueryable<TSource> source, int count)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            return source.Provider.CreateQuery<TSource>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.Skip_TSource_2(typeof(TSource)),
                    source.Expression, Expression.Constant(count)
                    ));
        }

        /// <summary>Bypasses elements in a sequence as long as a specified condition is true and then returns the remaining elements.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">An <see cref="System.Linq.IQueryable{T}" /> to return elements from.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <returns>An <see cref="System.Linq.IQueryable{T}" /> that contains elements from <paramref name="source" /> starting at the first element in the linear series that does not pass the test specified by <paramref name="predicate" />.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> or <paramref name="predicate" /> is <see langword="null" />.</exception>
        /// <remarks>
        /// <para>This method has at least one parameter of type <see cref="System.Linq.Expressions.Expression{T}" /> whose type argument is one of the <see cref="System.Func{T1,T2}" /> types. For these parameters, you can pass in a lambda expression and it will be compiled to an <see cref="System.Linq.Expressions.Expression{T}" />.</para>
        /// <para>The <see cref="System.Linq.Queryable.SkipWhile{T}(System.Linq.IQueryable{T},System.Linq.Expressions.Expression{System.Func{T,bool}})" /> method generates a <see cref="System.Linq.Expressions.MethodCallExpression" /> that represents calling <see cref="System.Linq.Queryable.SkipWhile{T}(System.Linq.IQueryable{T},System.Linq.Expressions.Expression{System.Func{T,bool}})" /> itself as a constructed generic method. It then passes the <see cref="System.Linq.Expressions.MethodCallExpression" /> to the <see cref="System.Linq.IQueryProvider.CreateQuery(System.Linq.Expressions.Expression)" /> method of the <see cref="System.Linq.IQueryProvider" /> represented by the <see cref="O:System.Linq.IQueryable.Provider" /> property of the <paramref name="source" /> parameter.</para>
        /// <para>The query behavior that occurs as a result of executing an expression tree that represents calling <see cref="System.Linq.Queryable.SkipWhile{T}(System.Linq.IQueryable{T},System.Linq.Expressions.Expression{System.Func{T,bool}})" /> depends on the implementation of the type of the <paramref name="source" /> parameter. The expected behavior is that it applies <paramref name="predicate" /> to each element in <paramref name="source" /> until it finds an element for which <paramref name="predicate" /> returns false. That element and all the remaining elements are returned.</para>
        /// </remarks>
        /// <example>The following code example demonstrates how to use <see cref="System.Linq.Queryable.SkipWhile{T}(System.Linq.IQueryable{T},System.Linq.Expressions.Expression{System.Func{T,bool}})" /> to skip elements of an array as long as a condition is true.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Queryable/CS/queryable.cs" interactive="try-dotnet-method" id="Snippet88":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Queryable/VB/queryable.vb" id="Snippet88":::</example>
        [DynamicDependency("SkipWhile`1", typeof(Enumerable))]
        public static IQueryable<TSource> SkipWhile<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (predicate == null)
                throw Error.ArgumentNull(nameof(predicate));
            return source.Provider.CreateQuery<TSource>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.SkipWhile_TSource_2(typeof(TSource)),
                    source.Expression, Expression.Quote(predicate)
                    ));
        }

        /// <summary>Bypasses elements in a sequence as long as a specified condition is true and then returns the remaining elements. The element's index is used in the logic of the predicate function.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">An <see cref="System.Linq.IQueryable{T}" /> to return elements from.</param>
        /// <param name="predicate">A function to test each element for a condition; the second parameter of this function represents the index of the source element.</param>
        /// <returns>An <see cref="System.Linq.IQueryable{T}" /> that contains elements from <paramref name="source" /> starting at the first element in the linear series that does not pass the test specified by <paramref name="predicate" />.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> or <paramref name="predicate" /> is <see langword="null" />.</exception>
        /// <remarks>
        /// <para>This method has at least one parameter of type <see cref="System.Linq.Expressions.Expression{T}" /> whose type argument is one of the <see cref="System.Func{T1,T2}" /> types. For these parameters, you can pass in a lambda expression and it will be compiled to an <see cref="System.Linq.Expressions.Expression{T}" />.</para>
        /// <para>The <see cref="System.Linq.Queryable.SkipWhile{T}(System.Linq.IQueryable{T},System.Linq.Expressions.Expression{System.Func{T,int,bool}})" /> method generates a <see cref="System.Linq.Expressions.MethodCallExpression" /> that represents calling <see cref="System.Linq.Queryable.SkipWhile{T}(System.Linq.IQueryable{T},System.Linq.Expressions.Expression{System.Func{T,int,bool}})" /> itself as a constructed generic method. It then passes the <see cref="System.Linq.Expressions.MethodCallExpression" /> to the <see cref="System.Linq.IQueryProvider.CreateQuery(System.Linq.Expressions.Expression)" /> method of the <see cref="System.Linq.IQueryProvider" /> represented by the <see cref="O:System.Linq.IQueryable.Provider" /> property of the <paramref name="source" /> parameter.</para>
        /// <para>The query behavior that occurs as a result of executing an expression tree that represents calling <see cref="System.Linq.Queryable.SkipWhile{T}(System.Linq.IQueryable{T},System.Linq.Expressions.Expression{System.Func{T,int,bool}})" /> depends on the implementation of the type of the <paramref name="source" /> parameter. The expected behavior is that it applies <paramref name="predicate" /> to each element in <paramref name="source" /> until it finds an element for which <paramref name="predicate" /> returns false. That element and all the remaining elements are returned. The index of each source element is provided as the second argument to <paramref name="predicate" />.</para>
        /// </remarks>
        /// <example>The following code example demonstrates how to use <see cref="System.Linq.Queryable.SkipWhile{T}(System.Linq.IQueryable{T},System.Linq.Expressions.Expression{System.Func{T,int,bool}})" /> to skip elements of an array as long as a condition that depends on the element's index is true.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Queryable/CS/queryable.cs" interactive="try-dotnet-method" id="Snippet89":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Queryable/VB/queryable.vb" id="Snippet89":::</example>
        [DynamicDependency("SkipWhile`1", typeof(Enumerable))]
        public static IQueryable<TSource> SkipWhile<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, int, bool>> predicate)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (predicate == null)
                throw Error.ArgumentNull(nameof(predicate));
            return source.Provider.CreateQuery<TSource>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.SkipWhile_Index_TSource_2(typeof(TSource)),
                    source.Expression, Expression.Quote(predicate)
                    ));
        }

        /// <summary>Groups the elements of a sequence according to a specified key selector function.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by the function represented in <paramref name="keySelector" />.</typeparam>
        /// <param name="source">An <see cref="System.Linq.IQueryable{T}" /> whose elements to group.</param>
        /// <param name="keySelector">A function to extract the key for each element.</param>
        /// <returns>An <c>IQueryable&lt;IGrouping&lt;TKey, TSource&gt;&gt;</c> in C# or <c>IQueryable(Of IGrouping(Of TKey, TSource))</c> in Visual Basic where each <see cref="System.Linq.IGrouping{T1,T2}" /> object contains a sequence of objects and a key.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> or <paramref name="keySelector" /> is <see langword="null" />.</exception>
        /// <remarks>
        /// <para>This method has at least one parameter of type <see cref="System.Linq.Expressions.Expression{T}" /> whose type argument is one of the <see cref="System.Func{T1,T2}" /> types. For these parameters, you can pass in a lambda expression and it will be compiled to an <see cref="System.Linq.Expressions.Expression{T}" />.</para>
        /// <para>The <see cref="System.Linq.Queryable.GroupBy{T1,T2}(System.Linq.IQueryable{T1},System.Linq.Expressions.Expression{System.Func{T1,T2}})" /> method generates a <see cref="System.Linq.Expressions.MethodCallExpression" /> that represents calling <see cref="System.Linq.Queryable.GroupBy{T1,T2}(System.Linq.IQueryable{T1},System.Linq.Expressions.Expression{System.Func{T1,T2}})" /> itself as a constructed generic method. It then passes the <see cref="System.Linq.Expressions.MethodCallExpression" /> to the <see cref="System.Linq.IQueryProvider.CreateQuery{T}(System.Linq.Expressions.Expression)" /> method of the <see cref="System.Linq.IQueryProvider" /> represented by the <see cref="O:System.Linq.IQueryable.Provider" /> property of the <paramref name="source" /> parameter.</para>
        /// <para>The query behavior that occurs as a result of executing an expression tree that represents calling <see cref="System.Linq.Queryable.GroupBy{T1,T2}(System.Linq.IQueryable{T1},System.Linq.Expressions.Expression{System.Func{T1,T2}})" /> depends on the implementation of the type of the <paramref name="source" /> parameter. The expected behavior is that it groups the elements of <paramref name="source" /> by a key value that is obtained by invoking <paramref name="keySelector" /> on each element.</para>
        /// </remarks>
        /// <example>The following code example demonstrates how to use <see cref="System.Linq.Queryable.GroupBy{T1,T2}(System.Linq.IQueryable{T1},System.Linq.Expressions.Expression{System.Func{T1,T2}})" /> to group the elements of a sequence.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Queryable/CS/queryable.cs" id="Snippet14":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Queryable/VB/queryable.vb" id="Snippet14":::</example>
        [DynamicDependency("GroupBy`2", typeof(Enumerable))]
        public static IQueryable<IGrouping<TKey, TSource>> GroupBy<TSource, TKey>(this IQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (keySelector == null)
                throw Error.ArgumentNull(nameof(keySelector));
            return source.Provider.CreateQuery<IGrouping<TKey, TSource>>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.GroupBy_TSource_TKey_2(typeof(TSource), typeof(TKey)),
                    source.Expression, Expression.Quote(keySelector)
                    ));
        }

        /// <summary>Groups the elements of a sequence according to a specified key selector function and projects the elements for each group by using a specified function.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by the function represented in <paramref name="keySelector" />.</typeparam>
        /// <typeparam name="TElement">The type of the elements in each <see cref="System.Linq.IGrouping{T1,T2}" />.</typeparam>
        /// <param name="source">An <see cref="System.Linq.IQueryable{T}" /> whose elements to group.</param>
        /// <param name="keySelector">A function to extract the key for each element.</param>
        /// <param name="elementSelector">A function to map each source element to an element in an <see cref="System.Linq.IGrouping{T1,T2}" />.</param>
        /// <returns>An <c>IQueryable&lt;IGrouping&lt;TKey, TElement&gt;&gt;</c> in C# or <c>IQueryable(Of IGrouping(Of TKey, TElement))</c> in Visual Basic where each <see cref="System.Linq.IGrouping{T1,T2}" /> contains a sequence of objects of type <typeparamref name="TElement" /> and a key.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> or <paramref name="keySelector" /> or <paramref name="elementSelector" /> is <see langword="null" />.</exception>
        /// <remarks>
        /// <para>This method has at least one parameter of type <see cref="System.Linq.Expressions.Expression{T}" /> whose type argument is one of the <see cref="System.Func{T1,T2}" /> types. For these parameters, you can pass in a lambda expression and it will be compiled to an <see cref="System.Linq.Expressions.Expression{T}" />.</para>
        /// <para>The <see cref="System.Linq.Queryable.GroupBy{T1,T2,T3}(System.Linq.IQueryable{T1},System.Linq.Expressions.Expression{System.Func{T1,T2}},System.Linq.Expressions.Expression{System.Func{T1,T3}})" /> method generates a <see cref="System.Linq.Expressions.MethodCallExpression" /> that represents calling <see cref="System.Linq.Queryable.GroupBy{T1,T2,T3}(System.Linq.IQueryable{T1},System.Linq.Expressions.Expression{System.Func{T1,T2}},System.Linq.Expressions.Expression{System.Func{T1,T3}})" /> itself as a constructed generic method. It then passes the <see cref="System.Linq.Expressions.MethodCallExpression" /> to the <see cref="System.Linq.IQueryProvider.CreateQuery{T}(System.Linq.Expressions.Expression)" /> method of the <see cref="System.Linq.IQueryProvider" /> represented by the <see cref="O:System.Linq.IQueryable.Provider" /> property of the <paramref name="source" /> parameter.</para>
        /// <para>The query behavior that occurs as a result of executing an expression tree that represents calling <see cref="System.Linq.Queryable.GroupBy{T1,T2,T3}(System.Linq.IQueryable{T1},System.Linq.Expressions.Expression{System.Func{T1,T2}},System.Linq.Expressions.Expression{System.Func{T1,T3}})" /> depends on the implementation of the type of the <paramref name="source" /> parameter. The expected behavior is that it groups the elements of <paramref name="source" /> by a key value that is obtained by invoking <paramref name="keySelector" /> on each element. It invokes <paramref name="elementSelector" /> on each element to obtain a result element.</para>
        /// </remarks>
        /// <example>The following code example demonstrates how to use <see cref="System.Linq.Queryable.GroupBy{T1,T2,T3}(System.Linq.IQueryable{T1},System.Linq.Expressions.Expression{System.Func{T1,T2}},System.Linq.Expressions.Expression{System.Func{T1,T3}})" /> to group the elements of a sequence.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Queryable/CS/queryable.cs" id="Snippet39":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Queryable/VB/queryable.vb" id="Snippet39":::</example>
        [DynamicDependency("GroupBy`3", typeof(Enumerable))]
        public static IQueryable<IGrouping<TKey, TElement>> GroupBy<TSource, TKey, TElement>(this IQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector, Expression<Func<TSource, TElement>> elementSelector)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (keySelector == null)
                throw Error.ArgumentNull(nameof(keySelector));
            if (elementSelector == null)
                throw Error.ArgumentNull(nameof(elementSelector));
            return source.Provider.CreateQuery<IGrouping<TKey, TElement>>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.GroupBy_TSource_TKey_TElement_3(typeof(TSource), typeof(TKey), typeof(TElement)),
                    source.Expression, Expression.Quote(keySelector), Expression.Quote(elementSelector)
                    ));
        }

        /// <summary>Groups the elements of a sequence according to a specified key selector function and compares the keys by using a specified comparer.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by the function represented in <paramref name="keySelector" />.</typeparam>
        /// <param name="source">An <see cref="System.Linq.IQueryable{T}" /> whose elements to group.</param>
        /// <param name="keySelector">A function to extract the key for each element.</param>
        /// <param name="comparer">An <see cref="System.Collections.Generic.IEqualityComparer{T}" /> to compare keys.</param>
        /// <returns>An <c>IQueryable&lt;IGrouping&lt;TKey, TSource&gt;&gt;</c> in C# or <c>IQueryable(Of IGrouping(Of TKey, TSource))</c> in Visual Basic where each <see cref="System.Linq.IGrouping{T1,T2}" /> contains a sequence of objects and a key.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> or <paramref name="keySelector" /> or <paramref name="comparer" /> is <see langword="null" />.</exception>
        /// <remarks>
        /// <para>This method has at least one parameter of type <see cref="System.Linq.Expressions.Expression{T}" /> whose type argument is one of the <see cref="System.Func{T1,T2}" /> types. For these parameters, you can pass in a lambda expression and it will be compiled to an <see cref="System.Linq.Expressions.Expression{T}" />.</para>
        /// <para>The <see cref="System.Linq.Queryable.GroupBy{T1,T2}(System.Linq.IQueryable{T1},System.Linq.Expressions.Expression{System.Func{T1,T2}},System.Collections.Generic.IEqualityComparer{T2})" /> method generates a <see cref="System.Linq.Expressions.MethodCallExpression" /> that represents calling <see cref="System.Linq.Queryable.GroupBy{T1,T2}(System.Linq.IQueryable{T1},System.Linq.Expressions.Expression{System.Func{T1,T2}},System.Collections.Generic.IEqualityComparer{T2})" /> itself as a constructed generic method. It then passes the <see cref="System.Linq.Expressions.MethodCallExpression" /> to the <see cref="System.Linq.IQueryProvider.CreateQuery{T}(System.Linq.Expressions.Expression)" /> method of the <see cref="System.Linq.IQueryProvider" /> represented by the <see cref="O:System.Linq.IQueryable.Provider" /> property of the <paramref name="source" /> parameter.</para>
        /// <para>The query behavior that occurs as a result of executing an expression tree that represents calling <see cref="System.Linq.Queryable.GroupBy{T1,T2}(System.Linq.IQueryable{T1},System.Linq.Expressions.Expression{System.Func{T1,T2}},System.Collections.Generic.IEqualityComparer{T2})" /> depends on the implementation of the type of the <paramref name="source" /> parameter. The expected behavior is that it groups the elements of <paramref name="source" /> by a key value. The key value is obtained by invoking <paramref name="keySelector" /> on each element, and key values are compared by using <paramref name="comparer" />.</para>
        /// </remarks>
        [DynamicDependency("GroupBy`2", typeof(Enumerable))]
        public static IQueryable<IGrouping<TKey, TSource>> GroupBy<TSource, TKey>(this IQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector, IEqualityComparer<TKey>? comparer)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (keySelector == null)
                throw Error.ArgumentNull(nameof(keySelector));
            return source.Provider.CreateQuery<IGrouping<TKey, TSource>>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.GroupBy_TSource_TKey_3(typeof(TSource), typeof(TKey)),
                    source.Expression, Expression.Quote(keySelector), Expression.Constant(comparer, typeof(IEqualityComparer<TKey>))
                    ));
        }

        /// <summary>Groups the elements of a sequence and projects the elements for each group by using a specified function. Key values are compared by using a specified comparer.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by the function represented in <paramref name="keySelector" />.</typeparam>
        /// <typeparam name="TElement">The type of the elements in each <see cref="System.Linq.IGrouping{T1,T2}" />.</typeparam>
        /// <param name="source">An <see cref="System.Linq.IQueryable{T}" /> whose elements to group.</param>
        /// <param name="keySelector">A function to extract the key for each element.</param>
        /// <param name="elementSelector">A function to map each source element to an element in an <see cref="System.Linq.IGrouping{T1,T2}" />.</param>
        /// <param name="comparer">An <see cref="System.Collections.Generic.IEqualityComparer{T}" /> to compare keys.</param>
        /// <returns>An <c>IQueryable&lt;IGrouping&lt;TKey, TElement&gt;&gt;</c> in C# or <c>IQueryable(Of IGrouping(Of TKey, TElement))</c> in Visual Basic where each <see cref="System.Linq.IGrouping{T1,T2}" /> contains a sequence of objects of type <typeparamref name="TElement" /> and a key.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> or <paramref name="keySelector" /> or <paramref name="elementSelector" /> or <paramref name="comparer" /> is <see langword="null" />.</exception>
        /// <remarks>
        /// <para>This method has at least one parameter of type <see cref="System.Linq.Expressions.Expression{T}" /> whose type argument is one of the <see cref="System.Func{T1,T2}" /> types. For these parameters, you can pass in a lambda expression and it will be compiled to an <see cref="System.Linq.Expressions.Expression{T}" />.</para>
        /// <para>The <see cref="System.Linq.Queryable.GroupBy{T1,T2,T3}(System.Linq.IQueryable{T1},System.Linq.Expressions.Expression{System.Func{T1,T2}},System.Linq.Expressions.Expression{System.Func{T1,T3}},System.Collections.Generic.IEqualityComparer{T2})" /> method generates a <see cref="System.Linq.Expressions.MethodCallExpression" /> that represents calling <see cref="System.Linq.Queryable.GroupBy{T1,T2,T3}(System.Linq.IQueryable{T1},System.Linq.Expressions.Expression{System.Func{T1,T2}},System.Linq.Expressions.Expression{System.Func{T1,T3}},System.Collections.Generic.IEqualityComparer{T2})" /> itself as a constructed generic method. It then passes the <see cref="System.Linq.Expressions.MethodCallExpression" /> to the <see cref="System.Linq.IQueryProvider.CreateQuery{T}(System.Linq.Expressions.Expression)" /> method of the <see cref="System.Linq.IQueryProvider" /> represented by the <see cref="O:System.Linq.IQueryable.Provider" /> property of the <paramref name="source" /> parameter.</para>
        /// <para>The query behavior that occurs as a result of executing an expression tree that represents calling <see cref="System.Linq.Queryable.GroupBy{T1,T2,T3}(System.Linq.IQueryable{T1},System.Linq.Expressions.Expression{System.Func{T1,T2}},System.Linq.Expressions.Expression{System.Func{T1,T3}},System.Collections.Generic.IEqualityComparer{T2})" /> depends on the implementation of the type of the <paramref name="source" /> parameter. The expected behavior is that it groups the elements of <paramref name="source" /> by a key value that is obtained by invoking <paramref name="keySelector" /> on each element. Key values are compared by using <paramref name="comparer" />. The <paramref name="elementSelector" /> parameter is invoked on each element to obtain a result element.</para>
        /// </remarks>
        [DynamicDependency("GroupBy`3", typeof(Enumerable))]
        public static IQueryable<IGrouping<TKey, TElement>> GroupBy<TSource, TKey, TElement>(this IQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector, Expression<Func<TSource, TElement>> elementSelector, IEqualityComparer<TKey>? comparer)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (keySelector == null)
                throw Error.ArgumentNull(nameof(keySelector));
            if (elementSelector == null)
                throw Error.ArgumentNull(nameof(elementSelector));
            return source.Provider.CreateQuery<IGrouping<TKey, TElement>>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.GroupBy_TSource_TKey_TElement_4(typeof(TSource), typeof(TKey), typeof(TElement)), source.Expression, Expression.Quote(keySelector), Expression.Quote(elementSelector), Expression.Constant(comparer, typeof(IEqualityComparer<TKey>))));
        }

        /// <summary>Groups the elements of a sequence according to a specified key selector function and creates a result value from each group and its key. The elements of each group are projected by using a specified function.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by the function represented in <paramref name="keySelector" />.</typeparam>
        /// <typeparam name="TElement">The type of the elements in each <see cref="System.Linq.IGrouping{T1,T2}" />.</typeparam>
        /// <typeparam name="TResult">The type of the result value returned by <paramref name="resultSelector" />.</typeparam>
        /// <param name="source">An <see cref="System.Linq.IQueryable{T}" /> whose elements to group.</param>
        /// <param name="keySelector">A function to extract the key for each element.</param>
        /// <param name="elementSelector">A function to map each source element to an element in an <see cref="System.Linq.IGrouping{T1,T2}" />.</param>
        /// <param name="resultSelector">A function to create a result value from each group.</param>
        /// <returns>An <c>T:System.Linq.IQueryable`1</c> that has a type argument of <typeparamref name="TResult" /> and where each element represents a projection over a group and its key.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> or <paramref name="keySelector" /> or <paramref name="elementSelector" /> or <paramref name="resultSelector" /> is <see langword="null" />.</exception>
        /// <remarks>
        /// <para>This method has at least one parameter of type <see cref="System.Linq.Expressions.Expression{T}" /> whose type argument is one of the <see cref="System.Func{T1,T2}" /> types. For these parameters, you can pass in a lambda expression and it will be compiled to an <see cref="System.Linq.Expressions.Expression{T}" />.</para>
        /// <para>The <see cref="System.Linq.Queryable.GroupBy{T1,T2,T3,T4}(System.Linq.IQueryable{T1},System.Linq.Expressions.Expression{System.Func{T1,T2}},System.Linq.Expressions.Expression{System.Func{T1,T3}},System.Linq.Expressions.Expression{System.Func{T2,System.Collections.Generic.IEnumerable{T3},T4}})" /> method generates a <see cref="System.Linq.Expressions.MethodCallExpression" /> that represents calling <see cref="System.Linq.Queryable.GroupBy{T1,T2,T3,T4}(System.Linq.IQueryable{T1},System.Linq.Expressions.Expression{System.Func{T1,T2}},System.Linq.Expressions.Expression{System.Func{T1,T3}},System.Linq.Expressions.Expression{System.Func{T2,System.Collections.Generic.IEnumerable{T3},T4}})" /> itself as a constructed generic method. It then passes the <see cref="System.Linq.Expressions.MethodCallExpression" /> to the <see cref="System.Linq.IQueryProvider.CreateQuery{T}(System.Linq.Expressions.Expression)" /> method of the <see cref="System.Linq.IQueryProvider" /> represented by the <see cref="O:System.Linq.IQueryable.Provider" /> property of the <paramref name="source" /> parameter.</para>
        /// <para>The query behavior that occurs as a result of executing an expression tree that represents calling <see cref="System.Linq.Queryable.GroupBy{T1,T2,T3,T4}(System.Linq.IQueryable{T1},System.Linq.Expressions.Expression{System.Func{T1,T2}},System.Linq.Expressions.Expression{System.Func{T1,T3}},System.Linq.Expressions.Expression{System.Func{T2,System.Collections.Generic.IEnumerable{T3},T4}})" /> depends on the implementation of the type of the <paramref name="source" /> parameter. The expected behavior is that it groups the elements of <paramref name="source" /> by key values that are obtained by invoking <paramref name="keySelector" /> on each element. The <paramref name="elementSelector" /> parameter is used to project the elements of each group, and the <paramref name="resultSelector" /> parameter is used to obtain a result value from each group and its key.</para>
        /// </remarks>
        /// <example>The following code example demonstrates how to use <see cref="System.Linq.Queryable.GroupBy{T1,T2,T3,T4}(System.Linq.IQueryable{T1},System.Linq.Expressions.Expression{System.Func{T1,T2}},System.Linq.Expressions.Expression{System.Func{T1,T3}},System.Linq.Expressions.Expression{System.Func{T2,System.Collections.Generic.IEnumerable{T3},T4}})" /> to group the elements of a sequence and project a sequence of results of type <typeparamref name="TResult" />.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Queryable/CS/queryable.cs" id="Snippet130":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Queryable/VB/queryable.vb" id="Snippet130":::</example>
        [DynamicDependency("GroupBy`4", typeof(Enumerable))]
        public static IQueryable<TResult> GroupBy<TSource, TKey, TElement, TResult>(this IQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector, Expression<Func<TSource, TElement>> elementSelector, Expression<Func<TKey, IEnumerable<TElement>, TResult>> resultSelector)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (keySelector == null)
                throw Error.ArgumentNull(nameof(keySelector));
            if (elementSelector == null)
                throw Error.ArgumentNull(nameof(elementSelector));
            if (resultSelector == null)
                throw Error.ArgumentNull(nameof(resultSelector));
            return source.Provider.CreateQuery<TResult>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.GroupBy_TSource_TKey_TElement_TResult_4(typeof(TSource), typeof(TKey), typeof(TElement), typeof(TResult)), source.Expression, Expression.Quote(keySelector), Expression.Quote(elementSelector), Expression.Quote(resultSelector)));
        }

        /// <summary>Groups the elements of a sequence according to a specified key selector function and creates a result value from each group and its key.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by the function represented in <paramref name="keySelector" />.</typeparam>
        /// <typeparam name="TResult">The type of the result value returned by <paramref name="resultSelector" />.</typeparam>
        /// <param name="source">An <see cref="System.Linq.IQueryable{T}" /> whose elements to group.</param>
        /// <param name="keySelector">A function to extract the key for each element.</param>
        /// <param name="resultSelector">A function to create a result value from each group.</param>
        /// <returns>An <c>T:System.Linq.IQueryable`1</c> that has a type argument of <typeparamref name="TResult" /> and where each element represents a projection over a group and its key.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> or <paramref name="keySelector" /> or <paramref name="resultSelector" /> is <see langword="null" />.</exception>
        /// <remarks>
        /// <para>This method has at least one parameter of type <see cref="System.Linq.Expressions.Expression{T}" /> whose type argument is one of the <see cref="System.Func{T1,T2}" /> types. For these parameters, you can pass in a lambda expression and it will be compiled to an <see cref="System.Linq.Expressions.Expression{T}" />.</para>
        /// <para>The <see cref="System.Linq.Queryable.GroupBy{T1,T2,T3}(System.Linq.IQueryable{T1},System.Linq.Expressions.Expression{System.Func{T1,T2}},System.Linq.Expressions.Expression{System.Func{T2,System.Collections.Generic.IEnumerable{T1},T3}})" /> method generates a <see cref="System.Linq.Expressions.MethodCallExpression" /> that represents calling <see cref="System.Linq.Queryable.GroupBy{T1,T2,T3}(System.Linq.IQueryable{T1},System.Linq.Expressions.Expression{System.Func{T1,T2}},System.Linq.Expressions.Expression{System.Func{T2,System.Collections.Generic.IEnumerable{T1},T3}})" /> itself as a constructed generic method. It then passes the <see cref="System.Linq.Expressions.MethodCallExpression" /> to the <see cref="System.Linq.IQueryProvider.CreateQuery{T}(System.Linq.Expressions.Expression)" /> method of the <see cref="System.Linq.IQueryProvider" /> represented by the <see cref="O:System.Linq.IQueryable.Provider" /> property of the <paramref name="source" /> parameter.</para>
        /// <para>The query behavior that occurs as a result of executing an expression tree that represents calling <see cref="System.Linq.Queryable.GroupBy{T1,T2,T3}(System.Linq.IQueryable{T1},System.Linq.Expressions.Expression{System.Func{T1,T2}},System.Linq.Expressions.Expression{System.Func{T2,System.Collections.Generic.IEnumerable{T1},T3}})" /> depends on the implementation of the type of the <paramref name="source" /> parameter. The expected behavior is that it groups the elements of <paramref name="source" /> by a key value that is obtained by invoking <paramref name="keySelector" /> on each element. The <paramref name="resultSelector" /> parameter is used to obtain a result value from each group and its key.</para>
        /// </remarks>
        /// <example>The following code example demonstrates how to use <see cref="System.Linq.Queryable.GroupBy{T1,T2,T3}(System.Linq.IQueryable{T1},System.Linq.Expressions.Expression{System.Func{T1,T2}},System.Linq.Expressions.Expression{System.Func{T2,System.Collections.Generic.IEnumerable{T1},T3}})" /> to group the elements of a sequence and project a sequence of results of type <typeparamref name="TResult" />.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Queryable/CS/queryable.cs" id="Snippet15":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Queryable/VB/queryable.vb" id="Snippet15":::</example>
        [DynamicDependency("GroupBy`3", typeof(Enumerable))]
        public static IQueryable<TResult> GroupBy<TSource, TKey, TResult>(this IQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector, Expression<Func<TKey, IEnumerable<TSource>, TResult>> resultSelector)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (keySelector == null)
                throw Error.ArgumentNull(nameof(keySelector));
            if (resultSelector == null)
                throw Error.ArgumentNull(nameof(resultSelector));
            return source.Provider.CreateQuery<TResult>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.GroupBy_TSource_TKey_TResult_3(typeof(TSource), typeof(TKey), typeof(TResult)),
                    source.Expression, Expression.Quote(keySelector), Expression.Quote(resultSelector)
                    ));
        }

        /// <summary>Groups the elements of a sequence according to a specified key selector function and creates a result value from each group and its key. Keys are compared by using a specified comparer.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by the function represented in <paramref name="keySelector" />.</typeparam>
        /// <typeparam name="TResult">The type of the result value returned by <paramref name="resultSelector" />.</typeparam>
        /// <param name="source">An <see cref="System.Linq.IQueryable{T}" /> whose elements to group.</param>
        /// <param name="keySelector">A function to extract the key for each element.</param>
        /// <param name="resultSelector">A function to create a result value from each group.</param>
        /// <param name="comparer">An <see cref="System.Collections.Generic.IEqualityComparer{T}" /> to compare keys.</param>
        /// <returns>An <c>T:System.Linq.IQueryable`1</c> that has a type argument of <typeparamref name="TResult" /> and where each element represents a projection over a group and its key.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> or <paramref name="keySelector" /> or <paramref name="resultSelector" /> or <paramref name="comparer" /> is <see langword="null" />.</exception>
        /// <remarks>
        /// <para>This method has at least one parameter of type <see cref="System.Linq.Expressions.Expression{T}" /> whose type argument is one of the <see cref="System.Func{T1,T2}" /> types. For these parameters, you can pass in a lambda expression and it will be compiled to an <see cref="System.Linq.Expressions.Expression{T}" />.</para>
        /// <para>The <see cref="System.Linq.Queryable.GroupBy{T1,T2,T3}(System.Linq.IQueryable{T1},System.Linq.Expressions.Expression{System.Func{T1,T2}},System.Linq.Expressions.Expression{System.Func{T2,System.Collections.Generic.IEnumerable{T1},T3}},System.Collections.Generic.IEqualityComparer{T2})" /> method generates a <see cref="System.Linq.Expressions.MethodCallExpression" /> that represents calling <see cref="System.Linq.Queryable.GroupBy{T1,T2,T3}(System.Linq.IQueryable{T1},System.Linq.Expressions.Expression{System.Func{T1,T2}},System.Linq.Expressions.Expression{System.Func{T2,System.Collections.Generic.IEnumerable{T1},T3}},System.Collections.Generic.IEqualityComparer{T2})" /> itself as a constructed generic method. It then passes the <see cref="System.Linq.Expressions.MethodCallExpression" /> to the <see cref="System.Linq.IQueryProvider.CreateQuery{T}(System.Linq.Expressions.Expression)" /> method of the <see cref="System.Linq.IQueryProvider" /> represented by the <see cref="O:System.Linq.IQueryable.Provider" /> property of the <paramref name="source" /> parameter.</para>
        /// <para>The query behavior that occurs as a result of executing an expression tree that represents calling <see cref="System.Linq.Queryable.GroupBy{T1,T2,T3}(System.Linq.IQueryable{T1},System.Linq.Expressions.Expression{System.Func{T1,T2}},System.Linq.Expressions.Expression{System.Func{T2,System.Collections.Generic.IEnumerable{T1},T3}},System.Collections.Generic.IEqualityComparer{T2})" /> depends on the implementation of the type of the <paramref name="source" /> parameter. The expected behavior is that it groups the elements of <paramref name="source" /> by key values that are obtained by invoking <paramref name="keySelector" /> on each element. The <paramref name="comparer" /> parameter is used to compare keys and the <paramref name="resultSelector" /> parameter is used to obtain a result value from each group and its key.</para>
        /// </remarks>
        [DynamicDependency("GroupBy`3", typeof(Enumerable))]
        public static IQueryable<TResult> GroupBy<TSource, TKey, TResult>(this IQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector, Expression<Func<TKey, IEnumerable<TSource>, TResult>> resultSelector, IEqualityComparer<TKey>? comparer)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (keySelector == null)
                throw Error.ArgumentNull(nameof(keySelector));
            if (resultSelector == null)
                throw Error.ArgumentNull(nameof(resultSelector));
            return source.Provider.CreateQuery<TResult>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.GroupBy_TSource_TKey_TResult_4(typeof(TSource), typeof(TKey), typeof(TResult)), source.Expression, Expression.Quote(keySelector), Expression.Quote(resultSelector), Expression.Constant(comparer, typeof(IEqualityComparer<TKey>))));
        }

        /// <summary>Groups the elements of a sequence according to a specified key selector function and creates a result value from each group and its key. Keys are compared by using a specified comparer and the elements of each group are projected by using a specified function.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by the function represented in <paramref name="keySelector" />.</typeparam>
        /// <typeparam name="TElement">The type of the elements in each <see cref="System.Linq.IGrouping{T1,T2}" />.</typeparam>
        /// <typeparam name="TResult">The type of the result value returned by <paramref name="resultSelector" />.</typeparam>
        /// <param name="source">An <see cref="System.Linq.IQueryable{T}" /> whose elements to group.</param>
        /// <param name="keySelector">A function to extract the key for each element.</param>
        /// <param name="elementSelector">A function to map each source element to an element in an <see cref="System.Linq.IGrouping{T1,T2}" />.</param>
        /// <param name="resultSelector">A function to create a result value from each group.</param>
        /// <param name="comparer">An <see cref="System.Collections.Generic.IEqualityComparer{T}" /> to compare keys.</param>
        /// <returns>An <c>T:System.Linq.IQueryable`1</c> that has a type argument of <typeparamref name="TResult" /> and where each element represents a projection over a group and its key.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> or <paramref name="keySelector" /> or <paramref name="elementSelector" /> or <paramref name="resultSelector" /> or <paramref name="comparer" /> is <see langword="null" />.</exception>
        /// <remarks>
        /// <para>This method has at least one parameter of type <see cref="System.Linq.Expressions.Expression{T}" /> whose type argument is one of the <see cref="System.Func{T1,T2}" /> types. For these parameters, you can pass in a lambda expression and it will be compiled to an <see cref="System.Linq.Expressions.Expression{T}" />.</para>
        /// <para>The <see cref="System.Linq.Queryable.GroupBy{T1,T2,T3,T4}(System.Linq.IQueryable{T1},System.Linq.Expressions.Expression{System.Func{T1,T2}},System.Linq.Expressions.Expression{System.Func{T1,T3}},System.Linq.Expressions.Expression{System.Func{T2,System.Collections.Generic.IEnumerable{T3},T4}},System.Collections.Generic.IEqualityComparer{T2})" /> method generates a <see cref="System.Linq.Expressions.MethodCallExpression" /> that represents calling <see cref="System.Linq.Queryable.GroupBy{T1,T2,T3,T4}(System.Linq.IQueryable{T1},System.Linq.Expressions.Expression{System.Func{T1,T2}},System.Linq.Expressions.Expression{System.Func{T1,T3}},System.Linq.Expressions.Expression{System.Func{T2,System.Collections.Generic.IEnumerable{T3},T4}},System.Collections.Generic.IEqualityComparer{T2})" /> itself as a constructed generic method. It then passes the <see cref="System.Linq.Expressions.MethodCallExpression" /> to the <see cref="System.Linq.IQueryProvider.CreateQuery{T}(System.Linq.Expressions.Expression)" /> method of the <see cref="System.Linq.IQueryProvider" /> represented by the <see cref="O:System.Linq.IQueryable.Provider" /> property of the <paramref name="source" /> parameter.</para>
        /// <para>The query behavior that occurs as a result of executing an expression tree that represents calling <see cref="System.Linq.Queryable.GroupBy{T1,T2,T3,T4}(System.Linq.IQueryable{T1},System.Linq.Expressions.Expression{System.Func{T1,T2}},System.Linq.Expressions.Expression{System.Func{T1,T3}},System.Linq.Expressions.Expression{System.Func{T2,System.Collections.Generic.IEnumerable{T3},T4}},System.Collections.Generic.IEqualityComparer{T2})" /> depends on the implementation of the type of the <paramref name="source" /> parameter. The expected behavior is that it groups the elements of <paramref name="source" /> by key values that are obtained by invoking <paramref name="keySelector" /> on each element. The <paramref name="comparer" /> parameter is used to compare key values. The <paramref name="elementSelector" /> parameter is used to project the elements of each group, and the <paramref name="resultSelector" /> parameter is used to obtain a result value from each group and its key.</para>
        /// </remarks>
        [DynamicDependency("GroupBy`4", typeof(Enumerable))]
        public static IQueryable<TResult> GroupBy<TSource, TKey, TElement, TResult>(this IQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector, Expression<Func<TSource, TElement>> elementSelector, Expression<Func<TKey, IEnumerable<TElement>, TResult>> resultSelector, IEqualityComparer<TKey>? comparer)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (keySelector == null)
                throw Error.ArgumentNull(nameof(keySelector));
            if (elementSelector == null)
                throw Error.ArgumentNull(nameof(elementSelector));
            if (resultSelector == null)
                throw Error.ArgumentNull(nameof(resultSelector));
            return source.Provider.CreateQuery<TResult>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.GroupBy_TSource_TKey_TElement_TResult_5(typeof(TSource), typeof(TKey), typeof(TElement), typeof(TResult)), source.Expression, Expression.Quote(keySelector), Expression.Quote(elementSelector), Expression.Quote(resultSelector), Expression.Constant(comparer, typeof(IEqualityComparer<TKey>))));
        }

        /// <summary>Returns distinct elements from a sequence by using the default equality comparer to compare values.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">The <see cref="System.Linq.IQueryable{T}" /> to remove duplicates from.</param>
        /// <returns>An <see cref="System.Linq.IQueryable{T}" /> that contains distinct elements from <paramref name="source" />.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <remarks>
        /// <para>The <see cref="System.Linq.Queryable.Distinct{T}(System.Linq.IQueryable{T})" /> method generates a <see cref="System.Linq.Expressions.MethodCallExpression" /> that represents calling <see cref="System.Linq.Queryable.Distinct{T}(System.Linq.IQueryable{T})" /> itself as a constructed generic method. It then passes the <see cref="System.Linq.Expressions.MethodCallExpression" /> to the <see cref="System.Linq.IQueryProvider.CreateQuery{T}(System.Linq.Expressions.Expression)" /> method of the <see cref="System.Linq.IQueryProvider" /> represented by the <see cref="O:System.Linq.IQueryable.Provider" /> property of the <paramref name="source" /> parameter.</para>
        /// <para>The query behavior that occurs as a result of executing an expression tree that represents calling <see cref="System.Linq.Queryable.Distinct{T}(System.Linq.IQueryable{T})" /> depends on the implementation of the type of the <paramref name="source" /> parameter. The expected behavior is that it returns an unordered sequence of the unique items in <paramref name="source" />.</para>
        /// </remarks>
        /// <example>The following code example demonstrates how to use <see cref="System.Linq.Queryable.Distinct{T}(System.Linq.IQueryable{T})" /> to return distinct elements from a sequence.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Queryable/CS/queryable.cs" interactive="try-dotnet-method" id="Snippet27":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Queryable/VB/queryable.vb" id="Snippet27":::</example>
        [DynamicDependency("Distinct`1", typeof(Enumerable))]
        public static IQueryable<TSource> Distinct<TSource>(this IQueryable<TSource> source)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            return source.Provider.CreateQuery<TSource>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.Distinct_TSource_1(typeof(TSource)), source.Expression));
        }

        /// <summary>Returns distinct elements from a sequence by using a specified <see cref="System.Collections.Generic.IEqualityComparer{T}" /> to compare values.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">The <see cref="System.Linq.IQueryable{T}" /> to remove duplicates from.</param>
        /// <param name="comparer">An <see cref="System.Collections.Generic.IEqualityComparer{T}" /> to compare values.</param>
        /// <returns>An <see cref="System.Linq.IQueryable{T}" /> that contains distinct elements from <paramref name="source" />.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> or <paramref name="comparer" /> is <see langword="null" />.</exception>
        /// <remarks>
        /// <para>The <see cref="System.Linq.Queryable.Distinct{T}(System.Linq.IQueryable{T},System.Collections.Generic.IEqualityComparer{T})" /> method generates a <see cref="System.Linq.Expressions.MethodCallExpression" /> that represents calling <see cref="System.Linq.Queryable.Distinct{T}(System.Linq.IQueryable{T},System.Collections.Generic.IEqualityComparer{T})" /> itself as a constructed generic method. It then passes the <see cref="System.Linq.Expressions.MethodCallExpression" /> to the <see cref="System.Linq.IQueryProvider.CreateQuery{T}(System.Linq.Expressions.Expression)" /> method of the <see cref="System.Linq.IQueryProvider" /> represented by the <see cref="O:System.Linq.IQueryable.Provider" /> property of the <paramref name="source" /> parameter.</para>
        /// <para>The query behavior that occurs as a result of executing an expression tree that represents calling <see cref="System.Linq.Queryable.Distinct{T}(System.Linq.IQueryable{T},System.Collections.Generic.IEqualityComparer{T})" /> depends on the implementation of the type of the <paramref name="source" /> parameter. The expected behavior is that it returns an unordered sequence of the unique items in <paramref name="source" /> by using <paramref name="comparer" /> to compare values.</para>
        /// </remarks>
        [DynamicDependency("Distinct`1", typeof(Enumerable))]
        public static IQueryable<TSource> Distinct<TSource>(this IQueryable<TSource> source, IEqualityComparer<TSource>? comparer)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            return source.Provider.CreateQuery<TSource>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.Distinct_TSource_2(typeof(TSource)),
                    source.Expression, Expression.Constant(comparer, typeof(IEqualityComparer<TSource>))
                    ));
        }

        /// <summary>Returns distinct elements from a sequence according to a specified key selector function.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <typeparam name="TKey">The type of key to distinguish elements by.</typeparam>
        /// <param name="source">The sequence to remove duplicate elements from.</param>
        /// <param name="keySelector">A function to extract the key for each element.</param>
        /// <returns>An <see cref="IQueryable{T}" /> that contains distinct elements from the source sequence.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        [DynamicDependency("DistinctBy`2", typeof(Enumerable))]
        public static IQueryable<TSource> DistinctBy<TSource, TKey>(this IQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (keySelector == null)
                throw Error.ArgumentNull(nameof(keySelector));
            return source.Provider.CreateQuery<TSource>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.DistinctBy_TSource_TKey_2(typeof(TSource), typeof(TKey)),
                    source.Expression, Expression.Quote(keySelector)
                    ));
        }

        /// <summary>Returns distinct elements from a sequence according to a specified key selector function.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <typeparam name="TKey">The type of key to distinguish elements by.</typeparam>
        /// <param name="source">The sequence to remove duplicate elements from.</param>
        /// <param name="keySelector">A function to extract the key for each element.</param>
        /// <param name="comparer">An <see cref="IEqualityComparer{TKey}" /> to compare keys.</param>
        /// <returns>An <see cref="IQueryable{T}" /> that contains distinct elements from the source sequence.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        [DynamicDependency("DistinctBy`2", typeof(Enumerable))]
        public static IQueryable<TSource> DistinctBy<TSource, TKey>(this IQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector, IEqualityComparer<TKey>? comparer)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (keySelector == null)
                throw Error.ArgumentNull(nameof(keySelector));
            return source.Provider.CreateQuery<TSource>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.DistinctBy_TSource_TKey_3(typeof(TSource), typeof(TKey)),
                    source.Expression, Expression.Quote(keySelector), Expression.Constant(comparer, typeof(IEqualityComparer<TKey>))
                    ));
        }

        /// <summary>Split the elements of a sequence into chunks of size at most <paramref name="size"/>.</summary>
        /// <param name="source">An <see cref="IEnumerable{T}"/> whose elements to chunk.</param>
        /// <param name="size">Maximum size of each chunk.</param>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <returns>An <see cref="IEnumerable{T}"/> that contains the elements the input sequence split into chunks of size <paramref name="size"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="size"/> is below 1.</exception>
        /// <remarks>
        /// <para>Every chunk except the last will be of size <paramref name="size"/>.</para>
        /// <para>The last chunk will contain the remaining elements and may be of a smaller size.</para>
        /// </remarks>
        [DynamicDependency("Chunk`1", typeof(Enumerable))]
        public static IQueryable<TSource[]> Chunk<TSource>(this IQueryable<TSource> source, int size)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            return source.Provider.CreateQuery<TSource[]>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.Chunk_TSource_1(typeof(TSource)),
                    source.Expression, Expression.Constant(size)
                    ));
        }

        /// <summary>Concatenates two sequences.</summary>
        /// <typeparam name="TSource">The type of the elements of the input sequences.</typeparam>
        /// <param name="source1">The first sequence to concatenate.</param>
        /// <param name="source2">The sequence to concatenate to the first sequence.</param>
        /// <returns>An <see cref="System.Linq.IQueryable{T}" /> that contains the concatenated elements of the two input sequences.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source1" /> or <paramref name="source2" /> is <see langword="null" />.</exception>
        /// <remarks>
        /// <para>The <see cref="System.Linq.Queryable.Concat{T}(System.Linq.IQueryable{T},System.Collections.Generic.IEnumerable{T})" /> method generates a <see cref="System.Linq.Expressions.MethodCallExpression" /> that represents calling <see cref="System.Linq.Queryable.Concat{T}(System.Linq.IQueryable{T},System.Collections.Generic.IEnumerable{T})" /> itself as a constructed generic method. It then passes the <see cref="System.Linq.Expressions.MethodCallExpression" /> to the <see cref="System.Linq.IQueryProvider.CreateQuery{T}(System.Linq.Expressions.Expression)" /> method of the <see cref="System.Linq.IQueryProvider" /> represented by the <see cref="O:System.Linq.IQueryable.Provider" /> property of the <paramref name="source1" /> parameter.</para>
        /// <para>The query behavior that occurs as a result of executing an expression tree that represents calling <see cref="System.Linq.Queryable.Concat{T}(System.Linq.IQueryable{T},System.Collections.Generic.IEnumerable{T})" /> depends on the implementation of the type of the <paramref name="source1" /> parameter. The expected behavior is that the elements in <paramref name="source2" /> are concatenated to those of <paramref name="source1" /> to create a new sequence.</para>
        /// </remarks>
        /// <example>The following code example demonstrates how to use <see cref="System.Linq.Queryable.Concat{T}(System.Linq.IQueryable{T},System.Collections.Generic.IEnumerable{T})" /> to concatenate two sequences.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Queryable/CS/queryable.cs" id="Snippet20":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Queryable/VB/queryable.vb" id="Snippet20":::</example>
        [DynamicDependency("Concat`1", typeof(Enumerable))]
        public static IQueryable<TSource> Concat<TSource>(this IQueryable<TSource> source1, IEnumerable<TSource> source2)
        {
            if (source1 == null)
                throw Error.ArgumentNull(nameof(source1));
            if (source2 == null)
                throw Error.ArgumentNull(nameof(source2));
            return source1.Provider.CreateQuery<TSource>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.Concat_TSource_2(typeof(TSource)),
                    source1.Expression, GetSourceExpression(source2)
                    ));
        }

        /// <summary>Produces a sequence of tuples with elements from the two specified sequences.</summary>
        /// <typeparam name="TFirst">The type of the elements of the first input sequence.</typeparam>
        /// <typeparam name="TSecond">The type of the elements of the second input sequence.</typeparam>
        /// <param name="source1">The first sequence to merge.</param>
        /// <param name="source2">The second sequence to merge.</param>
        /// <returns>A sequence of tuples with elements taken from the first and second sequences, in that order.</returns>
        [DynamicDependency("Zip`2", typeof(Enumerable))]
        public static IQueryable<(TFirst First, TSecond Second)> Zip<TFirst, TSecond>(this IQueryable<TFirst> source1, IEnumerable<TSecond> source2)
        {
            if (source1 == null)
            {
                throw Error.ArgumentNull(nameof(source1));
            }

            if (source2 == null)
            {
                throw Error.ArgumentNull(nameof(source2));
            }

            return source1.Provider.CreateQuery<(TFirst, TSecond)>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.Zip_TFirst_TSecond_2(typeof(TFirst), typeof(TSecond)),
                    source1.Expression, GetSourceExpression(source2)));
        }

        /// <summary>Merges two sequences by using the specified predicate function.</summary>
        /// <typeparam name="TFirst">The type of the elements of the first input sequence.</typeparam>
        /// <typeparam name="TSecond">The type of the elements of the second input sequence.</typeparam>
        /// <typeparam name="TResult">The type of the elements of the result sequence.</typeparam>
        /// <param name="source1">The first sequence to merge.</param>
        /// <param name="source2">The second sequence to merge.</param>
        /// <param name="resultSelector">A function that specifies how to merge the elements from the two sequences.</param>
        /// <returns>An <see cref="System.Linq.IQueryable{T}" /> that contains merged elements of two input sequences.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source1" /> or <paramref name="source2" /> is <see langword="null" />.</exception>
        /// <remarks>
        /// <para>The <see cref="O:System.Linq.Queryable.Zip" /> method generates a <see cref="System.Linq.Expressions.MethodCallExpression" /> that represents calling <see cref="O:System.Linq.Queryable.Zip" /> itself as a constructed generic method. It then passes the <see cref="System.Linq.Expressions.MethodCallExpression" /> to the <see cref="System.Linq.IQueryProvider.CreateQuery{T}(System.Linq.Expressions.Expression)" /> method of the <see cref="System.Linq.IQueryProvider" /> represented by the <see cref="O:System.Linq.IQueryable.Provider" /> property of the <paramref name="source1" /> parameter.</para>
        /// <para>The method merges each element of the first sequence with an element that has the same index in the second sequence. If the sequences do not have the same number of elements, the method merges sequences until it reaches the end of one of them. For example, if one sequence has three elements and the other one has four, the resulting sequence will have only three elements.</para>
        /// </remarks>
        /// <example>The following code example demonstrates how to use the <see cref="O:System.Linq.Queryable.Zip" /> method to merge two sequences.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Queryable/CS/queryable.cs" interactive="try-dotnet-method" id="Snippet200":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Queryable/VB/queryable.vb" id="Snippet200":::</example>
        [DynamicDependency("Zip`3", typeof(Enumerable))]
        public static IQueryable<TResult> Zip<TFirst, TSecond, TResult>(this IQueryable<TFirst> source1, IEnumerable<TSecond> source2, Expression<Func<TFirst, TSecond, TResult>> resultSelector)
        {
            if (source1 == null)
                throw Error.ArgumentNull(nameof(source1));
            if (source2 == null)
                throw Error.ArgumentNull(nameof(source2));
            if (resultSelector == null)
                throw Error.ArgumentNull(nameof(resultSelector));
            return source1.Provider.CreateQuery<TResult>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.Zip_TFirst_TSecond_TResult_3(typeof(TFirst), typeof(TSecond), typeof(TResult)),
                    source1.Expression, GetSourceExpression(source2), Expression.Quote(resultSelector)
                    ));
        }

        /// <summary>
        /// Produces a sequence of tuples with elements from the three specified sequences.
        /// </summary>
        /// <typeparam name="TFirst">The type of the elements of the first input sequence.</typeparam>
        /// <typeparam name="TSecond">The type of the elements of the second input sequence.</typeparam>
        /// <typeparam name="TThird">The type of the elements of the third input sequence.</typeparam>
        /// <param name="source1">The first sequence to merge.</param>
        /// <param name="source2">The second sequence to merge.</param>
        /// <param name="source3">The third sequence to merge.</param>
        /// <returns>A sequence of tuples with elements taken from the first, second and third sequences, in that order.</returns>
        [DynamicDependency("Zip`3", typeof(Enumerable))]
        public static IQueryable<(TFirst First, TSecond Second, TThird Third)> Zip<TFirst, TSecond, TThird>(this IQueryable<TFirst> source1, IEnumerable<TSecond> source2, IEnumerable<TThird> source3)
        {
            if (source1 == null)
                throw Error.ArgumentNull(nameof(source1));
            if (source2 == null)
                throw Error.ArgumentNull(nameof(source2));
            if (source3 == null)
                throw Error.ArgumentNull(nameof(source3));
            return source1.Provider.CreateQuery<(TFirst, TSecond, TThird)>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.Zip_TFirst_TSecond_TThird_3(typeof(TFirst), typeof(TSecond), typeof(TThird)),
                    source1.Expression, GetSourceExpression(source2), GetSourceExpression(source3)
                    ));
        }

        /// <summary>Produces the set union of two sequences by using the default equality comparer.</summary>
        /// <typeparam name="TSource">The type of the elements of the input sequences.</typeparam>
        /// <param name="source1">A sequence whose distinct elements form the first set for the union operation.</param>
        /// <param name="source2">A sequence whose distinct elements form the second set for the union operation.</param>
        /// <returns>An <see cref="System.Linq.IQueryable{T}" /> that contains the elements from both input sequences, excluding duplicates.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source1" /> or <paramref name="source2" /> is <see langword="null" />.</exception>
        /// <remarks>
        /// <para>The <see cref="System.Linq.Queryable.Union{T}(System.Linq.IQueryable{T},System.Collections.Generic.IEnumerable{T})" /> method generates a <see cref="System.Linq.Expressions.MethodCallExpression" /> that represents calling <see cref="System.Linq.Queryable.Union{T}(System.Linq.IQueryable{T},System.Collections.Generic.IEnumerable{T})" /> itself as a constructed generic method. It then passes the <see cref="System.Linq.Expressions.MethodCallExpression" /> to the <see cref="System.Linq.IQueryProvider.CreateQuery{T}(System.Linq.Expressions.Expression)" /> method of the <see cref="System.Linq.IQueryProvider" /> represented by the <see cref="O:System.Linq.IQueryable.Provider" /> property of the <paramref name="source1" /> parameter.</para>
        /// <para>The query behavior that occurs as a result of executing an expression tree that represents calling <see cref="System.Linq.Queryable.Union{T}(System.Linq.IQueryable{T},System.Collections.Generic.IEnumerable{T})" /> depends on the implementation of the type of the <paramref name="source1" /> parameter. The expected behavior is that the set union of the elements in <paramref name="source1" /> and <paramref name="source2" /> is returned.</para>
        /// </remarks>
        /// <example>The following code example demonstrates how to use <see cref="System.Linq.Queryable.Union{T}(System.Linq.IQueryable{T},System.Collections.Generic.IEnumerable{T})" /> to obtain the set union of two sequences.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Queryable/CS/queryable.cs" interactive="try-dotnet-method" id="Snippet109":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Queryable/VB/queryable.vb" id="Snippet109":::</example>
        [DynamicDependency("Union`1", typeof(Enumerable))]
        public static IQueryable<TSource> Union<TSource>(this IQueryable<TSource> source1, IEnumerable<TSource> source2)
        {
            if (source1 == null)
                throw Error.ArgumentNull(nameof(source1));
            if (source2 == null)
                throw Error.ArgumentNull(nameof(source2));
            return source1.Provider.CreateQuery<TSource>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.Union_TSource_2(typeof(TSource)),
                    source1.Expression, GetSourceExpression(source2)
                    ));
        }

        /// <summary>Produces the set union of two sequences by using a specified <see cref="System.Collections.Generic.IEqualityComparer{T}" />.</summary>
        /// <typeparam name="TSource">The type of the elements of the input sequences.</typeparam>
        /// <param name="source1">A sequence whose distinct elements form the first set for the union operation.</param>
        /// <param name="source2">A sequence whose distinct elements form the second set for the union operation.</param>
        /// <param name="comparer">An <see cref="System.Collections.Generic.IEqualityComparer{T}" /> to compare values.</param>
        /// <returns>An <see cref="System.Linq.IQueryable{T}" /> that contains the elements from both input sequences, excluding duplicates.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source1" /> or <paramref name="source2" /> is <see langword="null" />.</exception>
        /// <remarks>
        /// <para>The <see cref="System.Linq.Queryable.Union{T}(System.Linq.IQueryable{T},System.Collections.Generic.IEnumerable{T},System.Collections.Generic.IEqualityComparer{T})" /> method generates a <see cref="System.Linq.Expressions.MethodCallExpression" /> that represents calling <see cref="System.Linq.Queryable.Union{T}(System.Linq.IQueryable{T},System.Collections.Generic.IEnumerable{T},System.Collections.Generic.IEqualityComparer{T})" /> itself as a constructed generic method. It then passes the <see cref="System.Linq.Expressions.MethodCallExpression" /> to the <see cref="System.Linq.IQueryProvider.CreateQuery{T}(System.Linq.Expressions.Expression)" /> method of the <see cref="System.Linq.IQueryProvider" /> represented by the <see cref="O:System.Linq.IQueryable.Provider" /> property of the <paramref name="source1" /> parameter.</para>
        /// <para>The query behavior that occurs as a result of executing an expression tree that represents calling <see cref="System.Linq.Queryable.Union{T}(System.Linq.IQueryable{T},System.Collections.Generic.IEnumerable{T},System.Collections.Generic.IEqualityComparer{T})" /> depends on the implementation of the type of the <paramref name="source1" /> parameter. The expected behavior is that the set union of the elements in <paramref name="source1" /> and <paramref name="source2" /> is returned. The <paramref name="comparer" /> parameter is used to compare values.</para>
        /// </remarks>
        [DynamicDependency("Union`1", typeof(Enumerable))]
        public static IQueryable<TSource> Union<TSource>(this IQueryable<TSource> source1, IEnumerable<TSource> source2, IEqualityComparer<TSource>? comparer)
        {
            if (source1 == null)
                throw Error.ArgumentNull(nameof(source1));
            if (source2 == null)
                throw Error.ArgumentNull(nameof(source2));
            return source1.Provider.CreateQuery<TSource>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.Union_TSource_3(typeof(TSource)),
                    source1.Expression,
                    GetSourceExpression(source2),
                    Expression.Constant(comparer, typeof(IEqualityComparer<TSource>))
                    ));
        }

        /// <summary>Produces the set union of two sequences according to a specified key selector function.</summary>
        /// <typeparam name="TSource">The type of the elements of the input sequences.</typeparam>
        /// <typeparam name="TKey">The type of key to identify elements by.</typeparam>
        /// <param name="source1">An <see cref="IQueryable{T}" /> whose distinct elements form the first set for the union.</param>
        /// <param name="source2">An <see cref="IEnumerable{T}" /> whose distinct elements form the second set for the union.</param>
        /// <param name="keySelector">A function to extract the key for each element.</param>
        /// <returns>An <see cref="IEnumerable{T}" /> that contains the elements from both input sequences, excluding duplicates.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source1" /> or <paramref name="source2" /> is <see langword="null" />.</exception>
        [DynamicDependency("UnionBy`2", typeof(Enumerable))]
        public static IQueryable<TSource> UnionBy<TSource, TKey>(this IQueryable<TSource> source1, IEnumerable<TSource> source2, Expression<Func<TSource, TKey>> keySelector)
        {
            if (source1 == null)
                throw Error.ArgumentNull(nameof(source1));
            if (source2 == null)
                throw Error.ArgumentNull(nameof(source2));
            if (keySelector == null)
                throw Error.ArgumentNull(nameof(keySelector));
            return source1.Provider.CreateQuery<TSource>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.UnionBy_TSource_TKey_3(typeof(TSource), typeof(TKey)),
                    source1.Expression, GetSourceExpression(source2), Expression.Quote(keySelector)
                    ));
        }

        /// <summary>Produces the set union of two sequences according to a specified key selector function.</summary>
        /// <typeparam name="TSource">The type of the elements of the input sequences.</typeparam>
        /// <typeparam name="TKey">The type of key to identify elements by.</typeparam>
        /// <param name="source1">An <see cref="IQueryable{T}" /> whose distinct elements form the first set for the union.</param>
        /// <param name="source2">An <see cref="IEnumerable{T}" /> whose distinct elements form the second set for the union.</param>
        /// <param name="keySelector">A function to extract the key for each element.</param>
        /// <param name="comparer">The <see cref="IEqualityComparer{T}" /> to compare values.</param>
        /// <returns>An <see cref="IEnumerable{T}" /> that contains the elements from both input sequences, excluding duplicates.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source1" /> or <paramref name="source2" /> is <see langword="null" />.</exception>
        [DynamicDependency("UnionBy`2", typeof(Enumerable))]
        public static IQueryable<TSource> UnionBy<TSource, TKey>(this IQueryable<TSource> source1, IEnumerable<TSource> source2, Expression<Func<TSource, TKey>> keySelector, IEqualityComparer<TKey>? comparer)
        {
            if (source1 == null)
                throw Error.ArgumentNull(nameof(source1));
            if (source2 == null)
                throw Error.ArgumentNull(nameof(source2));
            if (keySelector == null)
                throw Error.ArgumentNull(nameof(keySelector));
            return source1.Provider.CreateQuery<TSource>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.UnionBy_TSource_TKey_4(typeof(TSource), typeof(TKey)),
                    source1.Expression,
                    GetSourceExpression(source2),
                    Expression.Quote(keySelector),
                    Expression.Constant(comparer, typeof(IEqualityComparer<TKey>))
                    ));
        }

        /// <summary>Produces the set intersection of two sequences by using the default equality comparer to compare values.</summary>
        /// <typeparam name="TSource">The type of the elements of the input sequences.</typeparam>
        /// <param name="source1">A sequence whose distinct elements that also appear in <paramref name="source2" /> are returned.</param>
        /// <param name="source2">A sequence whose distinct elements that also appear in the first sequence are returned.</param>
        /// <returns>A sequence that contains the set intersection of the two sequences.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source1" /> or <paramref name="source2" /> is <see langword="null" />.</exception>
        /// <remarks>
        /// <para>The <see cref="System.Linq.Queryable.Intersect{T}(System.Linq.IQueryable{T},System.Collections.Generic.IEnumerable{T})" /> method generates a <see cref="System.Linq.Expressions.MethodCallExpression" /> that represents calling <see cref="System.Linq.Queryable.Intersect{T}(System.Linq.IQueryable{T},System.Collections.Generic.IEnumerable{T})" /> itself as a constructed generic method. It then passes the <see cref="System.Linq.Expressions.MethodCallExpression" /> to the <see cref="System.Linq.IQueryProvider.CreateQuery{T}(System.Linq.Expressions.Expression)" /> method of the <see cref="System.Linq.IQueryProvider" /> represented by the <see cref="O:System.Linq.IQueryable.Provider" /> property of the <paramref name="source1" /> parameter.</para>
        /// <para>The query behavior that occurs as a result of executing an expression tree that represents calling <see cref="System.Linq.Queryable.Intersect{T}(System.Linq.IQueryable{T},System.Collections.Generic.IEnumerable{T})" /> depends on the implementation of the type of the <paramref name="source1" /> parameter. The expected behavior is that all the elements in <paramref name="source1" /> that are also in <paramref name="source2" /> are returned.</para>
        /// </remarks>
        /// <example>The following code example demonstrates how to use <see cref="System.Linq.Queryable.Intersect{T}(System.Linq.IQueryable{T},System.Collections.Generic.IEnumerable{T})" /> to return the elements that appear in each of two sequences.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Queryable/CS/queryable.cs" interactive="try-dotnet-method" id="Snippet41":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Queryable/VB/queryable.vb" id="Snippet41":::</example>
        [DynamicDependency("Intersect`1", typeof(Enumerable))]
        public static IQueryable<TSource> Intersect<TSource>(this IQueryable<TSource> source1, IEnumerable<TSource> source2)
        {
            if (source1 == null)
                throw Error.ArgumentNull(nameof(source1));
            if (source2 == null)
                throw Error.ArgumentNull(nameof(source2));
            return source1.Provider.CreateQuery<TSource>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.Intersect_TSource_2(typeof(TSource)),
                    source1.Expression, GetSourceExpression(source2)
                    ));
        }

        /// <summary>Produces the set intersection of two sequences by using the specified <see cref="System.Collections.Generic.IEqualityComparer{T}" /> to compare values.</summary>
        /// <typeparam name="TSource">The type of the elements of the input sequences.</typeparam>
        /// <param name="source1">An <see cref="System.Linq.IQueryable{T}" /> whose distinct elements that also appear in <paramref name="source2" /> are returned.</param>
        /// <param name="source2">An <see cref="System.Collections.Generic.IEnumerable{T}" /> whose distinct elements that also appear in the first sequence are returned.</param>
        /// <param name="comparer">An <see cref="System.Collections.Generic.IEqualityComparer{T}" /> to compare values.</param>
        /// <returns>An <see cref="System.Linq.IQueryable{T}" /> that contains the set intersection of the two sequences.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source1" /> or <paramref name="source2" /> is <see langword="null" />.</exception>
        /// <remarks>
        /// <para>The <see cref="System.Linq.Queryable.Intersect{T}(System.Linq.IQueryable{T},System.Collections.Generic.IEnumerable{T},System.Collections.Generic.IEqualityComparer{T})" /> method generates a <see cref="System.Linq.Expressions.MethodCallExpression" /> that represents calling <see cref="System.Linq.Queryable.Intersect{T}(System.Linq.IQueryable{T},System.Collections.Generic.IEnumerable{T},System.Collections.Generic.IEqualityComparer{T})" /> itself as a constructed generic method. It then passes the <see cref="System.Linq.Expressions.MethodCallExpression" /> to the <see cref="System.Linq.IQueryProvider.CreateQuery{T}(System.Linq.Expressions.Expression)" /> method of the <see cref="System.Linq.IQueryProvider" /> represented by the <see cref="O:System.Linq.IQueryable.Provider" /> property of the <paramref name="source1" /> parameter.</para>
        /// <para>The query behavior that occurs as a result of executing an expression tree that represents calling <see cref="System.Linq.Queryable.Intersect{T}(System.Linq.IQueryable{T},System.Collections.Generic.IEnumerable{T},System.Collections.Generic.IEqualityComparer{T})" /> depends on the implementation of the type of the <paramref name="source1" /> parameter. The expected behavior is that all the elements in <paramref name="source1" /> that are also in <paramref name="source2" /> are returned. The <paramref name="comparer" /> parameter is used to compare elements.</para>
        /// </remarks>
        [DynamicDependency("Intersect`1", typeof(Enumerable))]
        public static IQueryable<TSource> Intersect<TSource>(this IQueryable<TSource> source1, IEnumerable<TSource> source2, IEqualityComparer<TSource>? comparer)
        {
            if (source1 == null)
                throw Error.ArgumentNull(nameof(source1));
            if (source2 == null)
                throw Error.ArgumentNull(nameof(source2));
            return source1.Provider.CreateQuery<TSource>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.Intersect_TSource_3(typeof(TSource)),
                    source1.Expression,
                    GetSourceExpression(source2),
                    Expression.Constant(comparer, typeof(IEqualityComparer<TSource>))
                    ));
        }

        /// <summary>Produces the set intersection of two sequences according to a specified key selector function.</summary>
        /// <typeparam name="TSource">The type of the elements of the input sequences.</typeparam>
        /// <typeparam name="TKey">The type of key to identify elements by.</typeparam>
        /// <param name="source1">An <see cref="IQueryable{T}" /> whose distinct elements that also appear in <paramref name="source2" /> will be returned.</param>
        /// <param name="source2">An <see cref="IEnumerable{T}" /> whose distinct elements that also appear in the first sequence will be returned.</param>
        /// <param name="keySelector">A function to extract the key for each element.</param>
        /// <returns>A sequence that contains the elements that form the set intersection of two sequences.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source1" /> or <paramref name="source2" /> is <see langword="null" />.</exception>
        [DynamicDependency("IntersectBy`2", typeof(Enumerable))]
        public static IQueryable<TSource> IntersectBy<TSource, TKey>(this IQueryable<TSource> source1, IEnumerable<TKey> source2, Expression<Func<TSource, TKey>> keySelector)
        {
            if (source1 == null)
                throw Error.ArgumentNull(nameof(source1));
            if (source2 == null)
                throw Error.ArgumentNull(nameof(source2));
            if (keySelector == null)
                throw Error.ArgumentNull(nameof(keySelector));
            return source1.Provider.CreateQuery<TSource>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.IntersectBy_TSource_TKey_3(typeof(TSource), typeof(TKey)),
                    source1.Expression,
                    GetSourceExpression(source2),
                    Expression.Quote(keySelector)
                    ));
        }

        /// <summary>Produces the set intersection of two sequences according to a specified key selector function.</summary>
        /// <typeparam name="TSource">The type of the elements of the input sequences.</typeparam>
        /// <typeparam name="TKey">The type of key to identify elements by.</typeparam>
        /// <param name="source1">An <see cref="IQueryable{T}" /> whose distinct elements that also appear in <paramref name="source2" /> will be returned.</param>
        /// <param name="source2">An <see cref="IEnumerable{T}" /> whose distinct elements that also appear in the first sequence will be returned.</param>
        /// <param name="keySelector">A function to extract the key for each element.</param>
        /// <param name="comparer">An <see cref="IEqualityComparer{TKey}" /> to compare keys.</param>
        /// <returns>A sequence that contains the elements that form the set intersection of two sequences.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source1" /> or <paramref name="source2" /> is <see langword="null" />.</exception>
        [DynamicDependency("IntersectBy`2", typeof(Enumerable))]
        public static IQueryable<TSource> IntersectBy<TSource, TKey>(this IQueryable<TSource> source1, IEnumerable<TKey> source2, Expression<Func<TSource, TKey>> keySelector, IEqualityComparer<TKey>? comparer)
        {
            if (source1 == null)
                throw Error.ArgumentNull(nameof(source1));
            if (source2 == null)
                throw Error.ArgumentNull(nameof(source2));
            if (keySelector == null)
                throw Error.ArgumentNull(nameof(keySelector));
            return source1.Provider.CreateQuery<TSource>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.IntersectBy_TSource_TKey_4(typeof(TSource), typeof(TKey)),
                    source1.Expression,
                    GetSourceExpression(source2),
                    Expression.Quote(keySelector),
                    Expression.Constant(comparer, typeof(IEqualityComparer<TKey>))
                    ));
        }

        /// <summary>Produces the set difference of two sequences by using the default equality comparer to compare values.</summary>
        /// <typeparam name="TSource">The type of the elements of the input sequences.</typeparam>
        /// <param name="source1">An <see cref="System.Linq.IQueryable{T}" /> whose elements that are not also in <paramref name="source2" /> will be returned.</param>
        /// <param name="source2">An <see cref="System.Collections.Generic.IEnumerable{T}" /> whose elements that also occur in the first sequence will not appear in the returned sequence.</param>
        /// <returns>An <see cref="System.Linq.IQueryable{T}" /> that contains the set difference of the two sequences.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source1" /> or <paramref name="source2" /> is <see langword="null" />.</exception>
        /// <remarks>
        /// <para>The <see cref="System.Linq.Queryable.Except{T}(System.Linq.IQueryable{T},System.Collections.Generic.IEnumerable{T})" /> method generates a <see cref="System.Linq.Expressions.MethodCallExpression" /> that represents calling <see cref="System.Linq.Queryable.Except{T}(System.Linq.IQueryable{T},System.Collections.Generic.IEnumerable{T})" /> itself as a constructed generic method. It then passes the <see cref="System.Linq.Expressions.MethodCallExpression" /> to the <see cref="System.Linq.IQueryProvider.CreateQuery{T}(System.Linq.Expressions.Expression)" /> method of the <see cref="System.Linq.IQueryProvider" /> represented by the <see cref="O:System.Linq.IQueryable.Provider" /> property of the<paramref name="source1" /> parameter.</para>
        /// <para>The query behavior that occurs as a result of executing an expression tree that represents calling <see cref="System.Linq.Queryable.Except{T}(System.Linq.IQueryable{T},System.Collections.Generic.IEnumerable{T})" /> depends on the implementation of the type of  the <paramref name="source1" /> parameter. The expected behavior is that all the elements in <paramref name="source1" /> are returned except for those that are also in <paramref name="source2" />.</para>
        /// </remarks>
        /// <example>The following code example demonstrates how to use <see cref="System.Linq.Queryable.Except{T}(System.Linq.IQueryable{T},System.Collections.Generic.IEnumerable{T})" /> to return those elements that only appear in the first source sequence.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Queryable/CS/queryable.cs" interactive="try-dotnet-method" id="Snippet34":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Queryable/VB/queryable.vb" id="Snippet34":::</example>
        [DynamicDependency("Except`1", typeof(Enumerable))]
        public static IQueryable<TSource> Except<TSource>(this IQueryable<TSource> source1, IEnumerable<TSource> source2)
        {
            if (source1 == null)
                throw Error.ArgumentNull(nameof(source1));
            if (source2 == null)
                throw Error.ArgumentNull(nameof(source2));
            return source1.Provider.CreateQuery<TSource>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.Except_TSource_2(typeof(TSource)),
                    source1.Expression, GetSourceExpression(source2)
                    ));
        }

        /// <summary>Produces the set difference of two sequences by using the specified <see cref="System.Collections.Generic.IEqualityComparer{T}" /> to compare values.</summary>
        /// <typeparam name="TSource">The type of the elements of the input sequences.</typeparam>
        /// <param name="source1">An <see cref="System.Linq.IQueryable{T}" /> whose elements that are not also in <paramref name="source2" /> will be returned.</param>
        /// <param name="source2">An <see cref="System.Collections.Generic.IEnumerable{T}" /> whose elements that also occur in the first sequence will not appear in the returned sequence.</param>
        /// <param name="comparer">An <see cref="System.Collections.Generic.IEqualityComparer{T}" /> to compare values.</param>
        /// <returns>An <see cref="System.Linq.IQueryable{T}" /> that contains the set difference of the two sequences.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source1" /> or <paramref name="source2" /> is <see langword="null" />.</exception>
        /// <remarks>
        /// <para>The <see cref="System.Linq.Queryable.Except{T}(System.Linq.IQueryable{T},System.Collections.Generic.IEnumerable{T},System.Collections.Generic.IEqualityComparer{T})" /> method generates a <see cref="System.Linq.Expressions.MethodCallExpression" /> that represents calling <see cref="System.Linq.Queryable.Except{T}(System.Linq.IQueryable{T},System.Collections.Generic.IEnumerable{T},System.Collections.Generic.IEqualityComparer{T})" /> itself as a constructed generic method. It then passes the <see cref="System.Linq.Expressions.MethodCallExpression" /> to the <see cref="System.Linq.IQueryProvider.CreateQuery{T}(System.Linq.Expressions.Expression)" /> method of the <see cref="System.Linq.IQueryProvider" /> represented by the <see cref="O:System.Linq.IQueryable.Provider" /> property of the<paramref name="source1" /> parameter.</para>
        /// <para>The query behavior that occurs as a result of executing an expression tree that represents calling <see cref="System.Linq.Queryable.Except{T}(System.Linq.IQueryable{T},System.Collections.Generic.IEnumerable{T},System.Collections.Generic.IEqualityComparer{T})" /> depends on the implementation of the type of the <paramref name="source1" /> parameter. The expected behavior is that all the elements in <paramref name="source1" /> are returned except for those that are also in <paramref name="source2" />, and <paramref name="comparer" /> is used to compare values.</para>
        /// </remarks>
        [DynamicDependency("Except`1", typeof(Enumerable))]
        public static IQueryable<TSource> Except<TSource>(this IQueryable<TSource> source1, IEnumerable<TSource> source2, IEqualityComparer<TSource>? comparer)
        {
            if (source1 == null)
                throw Error.ArgumentNull(nameof(source1));
            if (source2 == null)
                throw Error.ArgumentNull(nameof(source2));
            return source1.Provider.CreateQuery<TSource>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.Except_TSource_3(typeof(TSource)),
                    source1.Expression,
                    GetSourceExpression(source2),
                    Expression.Constant(comparer, typeof(IEqualityComparer<TSource>))
                    ));
        }

        [DynamicDependency("ExceptBy`2", typeof(Enumerable))]
        public static IQueryable<TSource> ExceptBy<TSource, TKey>(this IQueryable<TSource> source1, IEnumerable<TKey> source2, Expression<Func<TSource, TKey>> keySelector)
        {
            if (source1 == null)
                throw Error.ArgumentNull(nameof(source1));
            if (source2 == null)
                throw Error.ArgumentNull(nameof(source2));
            if (keySelector == null)
                throw Error.ArgumentNull(nameof(keySelector));
            return source1.Provider.CreateQuery<TSource>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.ExceptBy_TSource_TKey_3(typeof(TSource), typeof(TKey)),
                    source1.Expression,
                    GetSourceExpression(source2),
                    Expression.Quote(keySelector)
                    ));
        }

        [DynamicDependency("ExceptBy`2", typeof(Enumerable))]
        public static IQueryable<TSource> ExceptBy<TSource, TKey>(this IQueryable<TSource> source1, IEnumerable<TKey> source2, Expression<Func<TSource, TKey>> keySelector, IEqualityComparer<TKey>? comparer)
        {
            if (source1 == null)
                throw Error.ArgumentNull(nameof(source1));
            if (source2 == null)
                throw Error.ArgumentNull(nameof(source2));
            if (keySelector == null)
                throw Error.ArgumentNull(nameof(keySelector));
            return source1.Provider.CreateQuery<TSource>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.ExceptBy_TSource_TKey_4(typeof(TSource), typeof(TKey)),
                    source1.Expression,
                    GetSourceExpression(source2),
                    Expression.Quote(keySelector),
                    Expression.Constant(comparer, typeof(IEqualityComparer<TKey>))
                    ));
        }

        /// <summary>Returns the first element of a sequence.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">The <see cref="System.Linq.IQueryable{T}" /> to return the first element of.</param>
        /// <returns>The first element in <paramref name="source" />.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <exception cref="System.InvalidOperationException">The source sequence is empty.</exception>
        /// <remarks>
        /// <para>The <see cref="System.Linq.Queryable.First{T}(System.Linq.IQueryable{T})" /> method generates a <see cref="System.Linq.Expressions.MethodCallExpression" /> that represents calling <see cref="System.Linq.Queryable.First{T}(System.Linq.IQueryable{T})" /> itself as a constructed generic method. It then passes the <see cref="System.Linq.Expressions.MethodCallExpression" /> to the <see cref="System.Linq.IQueryProvider.Execute{T}(System.Linq.Expressions.Expression)" /> method of the <see cref="System.Linq.IQueryProvider" /> represented by the <see cref="O:System.Linq.IQueryable.Provider" /> property of the <paramref name="source" /> parameter.</para>
        /// <para>The query behavior that occurs as a result of executing an expression tree that represents calling <see cref="System.Linq.Queryable.First{T}(System.Linq.IQueryable{T})" /> depends on the implementation of the type of the <paramref name="source" /> parameter. The expected behavior is that it returns the first element in <paramref name="source" />.</para>
        /// </remarks>
        /// <example>The following code example demonstrates how to use <see cref="System.Linq.Queryable.First{T}(System.Linq.IQueryable{T})" /> to return the first element in a sequence.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Queryable/CS/queryable.cs" interactive="try-dotnet-method" id="Snippet35":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Queryable/VB/queryable.vb" id="Snippet35":::</example>
        [DynamicDependency("First`1", typeof(Enumerable))]
        public static TSource First<TSource>(this IQueryable<TSource> source)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            return source.Provider.Execute<TSource>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.First_TSource_1(typeof(TSource)), source.Expression));
        }

        /// <summary>Returns the first element of a sequence that satisfies a specified condition.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">An <see cref="System.Linq.IQueryable{T}" /> to return an element from.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <returns>The first element in <paramref name="source" /> that passes the test in <paramref name="predicate" />.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> or <paramref name="predicate" /> is <see langword="null" />.</exception>
        /// <exception cref="System.InvalidOperationException">No element satisfies the condition in <paramref name="predicate" />.
        /// -or-
        /// The source sequence is empty.</exception>
        /// <remarks>
        /// <para>This method has at least one parameter of type <see cref="System.Linq.Expressions.Expression{T}" /> whose type argument is one of the <see cref="System.Func{T1,T2}" /> types. For these parameters, you can pass in a lambda expression and it will be compiled to an <see cref="System.Linq.Expressions.Expression{T}" />.</para>
        /// <para>The <see cref="System.Linq.Queryable.First{T}(System.Linq.IQueryable{T},System.Linq.Expressions.Expression{System.Func{T,bool}})" /> method generates a <see cref="System.Linq.Expressions.MethodCallExpression" /> that represents calling <see cref="System.Linq.Queryable.First{T}(System.Linq.IQueryable{T},System.Linq.Expressions.Expression{System.Func{T,bool}})" /> itself as a constructed generic method. It then passes the <see cref="System.Linq.Expressions.MethodCallExpression" /> to the <see cref="System.Linq.IQueryProvider.Execute{T}(System.Linq.Expressions.Expression)" /> method of the <see cref="System.Linq.IQueryProvider" /> represented by the <see cref="O:System.Linq.IQueryable.Provider" /> property of the <paramref name="source" /> parameter.</para>
        /// <para>The query behavior that occurs as a result of executing an expression tree that represents calling <see cref="System.Linq.Queryable.First{T}(System.Linq.IQueryable{T},System.Linq.Expressions.Expression{System.Func{T,bool}})" /> depends on the implementation of the type of the <paramref name="source" /> parameter. The expected behavior is that it returns the first element in <paramref name="source" /> that satisfies the condition specified by <paramref name="predicate" />.</para>
        /// </remarks>
        /// <example>The following code example demonstrates how to use <see cref="System.Linq.Queryable.First{T}(System.Linq.IQueryable{T},System.Linq.Expressions.Expression{System.Func{T,bool}})" /> to return the first element of a sequence that satisfies a condition.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Queryable/CS/queryable.cs" interactive="try-dotnet-method" id="Snippet36":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Queryable/VB/queryable.vb" id="Snippet36":::</example>
        [DynamicDependency("First`1", typeof(Enumerable))]
        public static TSource First<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (predicate == null)
                throw Error.ArgumentNull(nameof(predicate));
            return source.Provider.Execute<TSource>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.First_TSource_2(typeof(TSource)),
                    source.Expression, Expression.Quote(predicate)
                    ));
        }

        /// <summary>Returns the first element of a sequence, or a default value if the sequence contains no elements.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">The <see cref="System.Linq.IQueryable{T}" /> to return the first element of.</param>
        /// <returns><c>default</c>(<typeparamref name="TSource" />) if <paramref name="source" /> is empty; otherwise, the first element in <paramref name="source" />.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <remarks>
        /// <para>The <see cref="System.Linq.Queryable.FirstOrDefault{T}(System.Linq.IQueryable{T})" /> method generates a <see cref="System.Linq.Expressions.MethodCallExpression" /> that represents calling <see cref="System.Linq.Queryable.FirstOrDefault{T}(System.Linq.IQueryable{T})" /> itself as a constructed generic method. It then passes the <see cref="System.Linq.Expressions.MethodCallExpression" /> to the <see cref="System.Linq.IQueryProvider.Execute{T}(System.Linq.Expressions.Expression)" /> method of the <see cref="System.Linq.IQueryProvider" /> represented by the <see cref="O:System.Linq.IQueryable.Provider" /> property of the <paramref name="source" /> parameter.</para>
        /// <para>The query behavior that occurs as a result of executing an expression tree that represents calling <see cref="System.Linq.Queryable.FirstOrDefault{T}(System.Linq.IQueryable{T})" /> depends on the implementation of the type of the <paramref name="source" /> parameter. The expected behavior is that it returns the first element in <paramref name="source" />, or a default value if <paramref name="source" /> is empty.</para>
        /// <para>The <see cref="O:System.Linq.Queryable.FirstOrDefault" /> method does not provide a way to specify the default value to return if <paramref name="source" /> is empty. If you want to specify a default value other than `default(TSource)`, use the <see cref="System.Linq.Queryable.DefaultIfEmpty{T}(System.Linq.IQueryable{T},T)" /> method as described in the Example section.</para>
        /// </remarks>
        /// <example>The following code example demonstrates how to use <see cref="System.Linq.Queryable.FirstOrDefault{T}(System.Linq.IQueryable{T})" /> on an empty sequence.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Queryable/CS/queryable.cs" interactive="try-dotnet-method" id="Snippet37":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Queryable/VB/queryable.vb" id="Snippet37":::
        /// Sometimes the value of `default(TSource)` is not the default value that you want to use if the collection contains no elements. Instead of checking the result for the unwanted default value and then changing it if necessary, you can use the <see cref="System.Linq.Queryable.DefaultIfEmpty{T}(System.Linq.IQueryable{T},T)" /> method to specify the default value that you want to use if the collection is empty. Then, call <see cref="System.Linq.Queryable.First{T}(System.Linq.IQueryable{T})" /> to obtain the first element. The following code example uses both techniques to obtain a default value of 1 if a collection of numeric months is empty. Because the default value for an integer is 0, which does not correspond to any month, the default value must be specified as 1 instead. The first result variable is checked for the unwanted default value after the query is completed. The second result variable is obtained by calling <see cref="System.Linq.Queryable.DefaultIfEmpty{T}(System.Linq.IQueryable{T},T)" /> to specify a default value of 1.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Queryable/CS/queryable.cs" interactive="try-dotnet-method" id="Snippet131":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Queryable/VB/queryable.vb" id="Snippet131":::</example>
        [DynamicDependency("FirstOrDefault`1", typeof(Enumerable))]
        public static TSource? FirstOrDefault<TSource>(this IQueryable<TSource> source)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            return source.Provider.Execute<TSource>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.FirstOrDefault_TSource_1(typeof(TSource)), source.Expression));
        }

        /// <summary>Returns the first element of a sequence, or a default value if the sequence contains no elements.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">The <see cref="IEnumerable{T}" /> to return the first element of.</param>
        /// <param name="defaultValue">The default value to return if the sequence is empty.</param>
        /// <returns><paramref name="defaultValue" /> if <paramref name="source" /> is empty; otherwise, the first element in <paramref name="source" />.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        [DynamicDependency("FirstOrDefault`1", typeof(Enumerable))]
        public static TSource FirstOrDefault<TSource>(this IQueryable<TSource> source, TSource defaultValue)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            return source.Provider.Execute<TSource>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.FirstOrDefault_TSource_3(typeof(TSource)),
                    source.Expression, Expression.Constant(defaultValue, typeof(TSource))));
        }

        /// <summary>Returns the first element of a sequence that satisfies a specified condition or a default value if no such element is found.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">An <see cref="System.Linq.IQueryable{T}" /> to return an element from.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <returns><c>default</c>(<typeparamref name="TSource" />) if <paramref name="source" /> is empty or if no element passes the test specified by <paramref name="predicate" />; otherwise, the first element in <paramref name="source" /> that passes the test specified by <paramref name="predicate" />.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> or <paramref name="predicate" /> is <see langword="null" />.</exception>
        /// <remarks>
        /// <para>This method has at least one parameter of type <see cref="System.Linq.Expressions.Expression{T}" /> whose type argument is one of the <see cref="System.Func{T1,T2}" /> types. For these parameters, you can pass in a lambda expression and it will be compiled to an <see cref="System.Linq.Expressions.Expression{T}" />.</para>
        /// <para>The <see cref="System.Linq.Queryable.FirstOrDefault{T}(System.Linq.IQueryable{T},System.Linq.Expressions.Expression{System.Func{T,bool}})" /> method generates a <see cref="System.Linq.Expressions.MethodCallExpression" /> that represents calling <see cref="System.Linq.Queryable.FirstOrDefault{T}(System.Linq.IQueryable{T},System.Linq.Expressions.Expression{System.Func{T,bool}})" /> itself as a constructed generic method. It then passes the <see cref="System.Linq.Expressions.MethodCallExpression" /> to the <see cref="System.Linq.IQueryProvider.Execute{T}(System.Linq.Expressions.Expression)" /> method of the <see cref="System.Linq.IQueryProvider" /> represented by the <see cref="O:System.Linq.IQueryable.Provider" /> property of the <paramref name="source" /> parameter.</para>
        /// <para>The query behavior that occurs as a result of executing an expression tree that represents calling <see cref="System.Linq.Queryable.FirstOrDefault{T}(System.Linq.IQueryable{T},System.Linq.Expressions.Expression{System.Func{T,bool}})" /> depends on the implementation of the type of the <paramref name="source" /> parameter. The expected behavior is that it returns the first element in <paramref name="source" /> that satisfies the condition in <paramref name="predicate" />, or a default value if no element satisfies the condition.</para>
        /// </remarks>
        /// <example>The following code example demonstrates how to use <see cref="System.Linq.Queryable.FirstOrDefault{T}(System.Linq.IQueryable{T},System.Linq.Expressions.Expression{System.Func{T,bool}})" /> by passing in a predicate. In the second query, there is no element in the sequence that satisfies the condition.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Queryable/CS/queryable.cs" interactive="try-dotnet-method" id="Snippet38":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Queryable/VB/queryable.vb" id="Snippet38":::</example>
        [DynamicDependency("FirstOrDefault`1", typeof(Enumerable))]
        public static TSource? FirstOrDefault<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (predicate == null)
                throw Error.ArgumentNull(nameof(predicate));
            return source.Provider.Execute<TSource>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.FirstOrDefault_TSource_2(typeof(TSource)),
                    source.Expression, Expression.Quote(predicate)
                    ));
        }

        /// <summary>Returns the first element of the sequence that satisfies a condition or a default value if no such element is found.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">An <see cref="IEnumerable{T}" /> to return an element from.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <param name="defaultValue">The default value to return if the sequence is empty.</param>
        /// <returns><paramref name="defaultValue" /> if <paramref name="source" /> is empty or if no element passes the test specified by <paramref name="predicate" />; otherwise, the first element in <paramref name="source" /> that passes the test specified by <paramref name="predicate" />.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source" /> or <paramref name="predicate" /> is <see langword="null" />.</exception>
        [DynamicDependency("FirstOrDefault`1", typeof(Enumerable))]
        public static TSource FirstOrDefault<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate, TSource defaultValue)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (predicate == null)
                throw Error.ArgumentNull(nameof(predicate));
            return source.Provider.Execute<TSource>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.FirstOrDefault_TSource_4(typeof(TSource)),
                    source.Expression, Expression.Quote(predicate), Expression.Constant(defaultValue, typeof(TSource))
                ));
        }

        /// <summary>Returns the last element in a sequence.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">An <see cref="System.Linq.IQueryable{T}" /> to return the last element of.</param>
        /// <returns>The value at the last position in <paramref name="source" />.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <exception cref="System.InvalidOperationException">The source sequence is empty.</exception>
        /// <remarks>
        /// <para>The <see cref="System.Linq.Queryable.Last{T}(System.Linq.IQueryable{T})" /> method generates a <see cref="System.Linq.Expressions.MethodCallExpression" /> that represents calling <see cref="System.Linq.Queryable.Last{T}(System.Linq.IQueryable{T})" /> itself as a constructed generic method. It then passes the <see cref="System.Linq.Expressions.MethodCallExpression" /> to the <see cref="System.Linq.IQueryProvider.Execute{T}(System.Linq.Expressions.Expression)" /> method of the <see cref="System.Linq.IQueryProvider" /> represented by the <see cref="O:System.Linq.IQueryable.Provider" /> property of the <paramref name="source" /> parameter.</para>
        /// <para>The query behavior that occurs as a result of executing an expression tree that represents calling <see cref="System.Linq.Queryable.Last{T}(System.Linq.IQueryable{T})" /> depends on the implementation of the type of the <paramref name="source" /> parameter. The expected behavior is that it returns the last element in <paramref name="source" />.</para>
        /// </remarks>
        /// <example>The following code example demonstrates how to use <see cref="System.Linq.Queryable.Last{T}(System.Linq.IQueryable{T})" /> to return the last element of an array.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Queryable/CS/queryable.cs" interactive="try-dotnet-method" id="Snippet43":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Queryable/VB/queryable.vb" id="Snippet43":::</example>
        [DynamicDependency("Last`1", typeof(Enumerable))]
        public static TSource Last<TSource>(this IQueryable<TSource> source)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            return source.Provider.Execute<TSource>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.Last_TSource_1(typeof(TSource)), source.Expression));
        }

        /// <summary>Returns the last element of a sequence that satisfies a specified condition.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">An <see cref="System.Linq.IQueryable{T}" /> to return an element from.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <returns>The last element in <paramref name="source" /> that passes the test specified by <paramref name="predicate" />.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> or <paramref name="predicate" /> is <see langword="null" />.</exception>
        /// <exception cref="System.InvalidOperationException">No element satisfies the condition in <paramref name="predicate" />.
        /// -or-
        /// The source sequence is empty.</exception>
        /// <remarks>
        /// <para>This method has at least one parameter of type <see cref="System.Linq.Expressions.Expression{T}" /> whose type argument is one of the <see cref="System.Func{T1,T2}" /> types. For these parameters, you can pass in a lambda expression and it will be compiled to an <see cref="System.Linq.Expressions.Expression{T}" />.</para>
        /// <para>The <see cref="System.Linq.Queryable.Last{T}(System.Linq.IQueryable{T},System.Linq.Expressions.Expression{System.Func{T,bool}})" /> method generates a <see cref="System.Linq.Expressions.MethodCallExpression" /> that represents calling <see cref="System.Linq.Queryable.Last{T}(System.Linq.IQueryable{T},System.Linq.Expressions.Expression{System.Func{T,bool}})" /> itself as a constructed generic method. It then passes the <see cref="System.Linq.Expressions.MethodCallExpression" /> to the <see cref="System.Linq.IQueryProvider.Execute{T}(System.Linq.Expressions.Expression)" /> method of the <see cref="System.Linq.IQueryProvider" /> represented by the <see cref="O:System.Linq.IQueryable.Provider" /> property of the <paramref name="source" /> parameter.</para>
        /// <para>The query behavior that occurs as a result of executing an expression tree that represents calling <see cref="System.Linq.Queryable.Last{T}(System.Linq.IQueryable{T},System.Linq.Expressions.Expression{System.Func{T,bool}})" /> depends on the implementation of the type of the <paramref name="source" /> parameter. The expected behavior is that it returns the last element in <paramref name="source" /> that satisfies the condition specified by <paramref name="predicate" />.</para>
        /// </remarks>
        /// <example>The following code example demonstrates how to use <see cref="System.Linq.Queryable.Last{T}(System.Linq.IQueryable{T},System.Linq.Expressions.Expression{System.Func{T,bool}})" /> to return the last element of an array that satisfies a condition.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Queryable/CS/queryable.cs" interactive="try-dotnet-method" id="Snippet44":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Queryable/VB/queryable.vb" id="Snippet44":::</example>
        [DynamicDependency("Last`1", typeof(Enumerable))]
        public static TSource Last<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (predicate == null)
                throw Error.ArgumentNull(nameof(predicate));
            return source.Provider.Execute<TSource>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.Last_TSource_2(typeof(TSource)),
                    source.Expression, Expression.Quote(predicate)
                    ));
        }

        /// <summary>Returns the last element in a sequence, or a default value if the sequence contains no elements.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">An <see cref="System.Linq.IQueryable{T}" /> to return the last element of.</param>
        /// <returns><c>default</c>(<typeparamref name="TSource" />) if <paramref name="source" /> is empty; otherwise, the last element in <paramref name="source" />.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <remarks>
        /// <para>The <see cref="System.Linq.Queryable.LastOrDefault{T}(System.Linq.IQueryable{T})" /> method generates a <see cref="System.Linq.Expressions.MethodCallExpression" /> that represents calling <see cref="System.Linq.Queryable.LastOrDefault{T}(System.Linq.IQueryable{T})" /> itself as a constructed generic method. It then passes the <see cref="System.Linq.Expressions.MethodCallExpression" /> to the <see cref="System.Linq.IQueryProvider.Execute{T}(System.Linq.Expressions.Expression)" /> method of the <see cref="System.Linq.IQueryProvider" /> represented by the <see cref="O:System.Linq.IQueryable.Provider" /> property of the <paramref name="source" /> parameter.</para>
        /// <para>The query behavior that occurs as a result of executing an expression tree that represents calling <see cref="System.Linq.Queryable.LastOrDefault{T}(System.Linq.IQueryable{T})" /> depends on the implementation of the type of the <paramref name="source" /> parameter. The expected behavior is that it returns the last element in <paramref name="source" />, or a default value if <paramref name="source" /> is empty.</para>
        /// <para>The <see cref="O:System.Linq.Queryable.LastOrDefault" /> method does not provide a way to specify a default value. If you want to specify a default value other than `default(TSource)`, use the <see cref="System.Linq.Queryable.DefaultIfEmpty{T}(System.Linq.IQueryable{T},T)" /> method as described in the Example section.</para>
        /// </remarks>
        /// <example>The following code example demonstrates how to use <see cref="System.Linq.Queryable.LastOrDefault{T}(System.Linq.IQueryable{T})" /> on an empty array.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Queryable/CS/queryable.cs" interactive="try-dotnet-method" id="Snippet45":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Queryable/VB/queryable.vb" id="Snippet45":::
        /// Sometimes the value of `default(TSource)` is not the default value that you want to use if the collection contains no elements. Instead of checking the result for the unwanted default value and then changing it if necessary, you can use the <see cref="System.Linq.Queryable.DefaultIfEmpty{T}(System.Linq.IQueryable{T},T)" /> method to specify the default value that you want to use if the collection is empty. Then, call <see cref="System.Linq.Queryable.Last{T}(System.Linq.IQueryable{T})" /> to obtain the last element. The following code example uses both techniques to obtain a default value of 1 if a collection of numeric days of the month is empty. Because the default value for an integer is 0, which does not correspond to any day of the month, the default value must be specified as 1 instead. The first result variable is checked for the unwanted default value after the query is completed. The second result variable is obtained by calling <see cref="System.Linq.Queryable.DefaultIfEmpty{T}(System.Linq.IQueryable{T},T)" /> to specify a default value of 1.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Queryable/CS/queryable.cs" interactive="try-dotnet-method" id="Snippet132":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Queryable/VB/queryable.vb" id="Snippet132":::</example>
        [DynamicDependency("LastOrDefault`1", typeof(Enumerable))]
        public static TSource? LastOrDefault<TSource>(this IQueryable<TSource> source)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            return source.Provider.Execute<TSource>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.LastOrDefault_TSource_1(typeof(TSource)), source.Expression));
        }

        /// <summary>Returns the last element of a sequence, or a default value if the sequence contains no elements.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">An <see cref="IEnumerable{T}" /> to return the last element of.</param>
        /// <param name="defaultValue">The default value to return if the sequence is empty.</param>
        /// <returns><paramref name="defaultValue" /> if the source sequence is empty; otherwise, the last element in the <see cref="IEnumerable{T}" />.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        [DynamicDependency("LastOrDefault`1", typeof(Enumerable))]
        public static TSource LastOrDefault<TSource>(this IQueryable<TSource> source, TSource defaultValue)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            return source.Provider.Execute<TSource>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.LastOrDefault_TSource_3(typeof(TSource)),
                    source.Expression, Expression.Constant(defaultValue, typeof(TSource))));
        }

        /// <summary>Returns the last element of a sequence that satisfies a condition or a default value if no such element is found.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">An <see cref="System.Linq.IQueryable{T}" /> to return an element from.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <returns><c>default</c>(<typeparamref name="TSource" />) if <paramref name="source" /> is empty or if no elements pass the test in the predicate function; otherwise, the last element of <paramref name="source" /> that passes the test in the predicate function.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> or <paramref name="predicate" /> is <see langword="null" />.</exception>
        /// <remarks>
        /// <para>This method has at least one parameter of type <see cref="System.Linq.Expressions.Expression{T}" /> whose type argument is one of the <see cref="System.Func{T1,T2}" /> types. For these parameters, you can pass in a lambda expression and it will be compiled to an <see cref="System.Linq.Expressions.Expression{T}" />.</para>
        /// <para>The <see cref="System.Linq.Queryable.LastOrDefault{T}(System.Linq.IQueryable{T})" /> method generates a <see cref="System.Linq.Expressions.MethodCallExpression" /> that represents calling <see cref="System.Linq.Queryable.LastOrDefault{T}(System.Linq.IQueryable{T})" /> itself as a constructed generic method. It then passes the <see cref="System.Linq.Expressions.MethodCallExpression" /> to the <see cref="System.Linq.IQueryProvider.Execute{T}(System.Linq.Expressions.Expression)" /> method of the <see cref="System.Linq.IQueryProvider" /> represented by the <see cref="O:System.Linq.IQueryable.Provider" /> property of the <paramref name="source" /> parameter.</para>
        /// <para>The query behavior that occurs as a result of executing an expression tree that represents calling <see cref="System.Linq.Queryable.LastOrDefault{T}(System.Linq.IQueryable{T})" /> depends on the implementation of the type of the <paramref name="source" /> parameter. The expected behavior is that it returns the last element in <paramref name="source" /> that satisfies the condition specified by <paramref name="predicate" />. It returns a default value if there is no such element in <paramref name="source" />.</para>
        /// </remarks>
        /// <example>The following code example demonstrates how to use <see cref="System.Linq.Queryable.LastOrDefault{T}(System.Linq.IQueryable{T},System.Linq.Expressions.Expression{System.Func{T,bool}})" /> by passing in a predicate. In the second call to the method, there is no element in the sequence that satisfies the condition.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Queryable/CS/queryable.cs" interactive="try-dotnet-method" id="Snippet46":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Queryable/VB/queryable.vb" id="Snippet46":::</example>
        [DynamicDependency("LastOrDefault`1", typeof(Enumerable))]
        public static TSource? LastOrDefault<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (predicate == null)
                throw Error.ArgumentNull(nameof(predicate));
            return source.Provider.Execute<TSource>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.LastOrDefault_TSource_2(typeof(TSource)),
                    source.Expression, Expression.Quote(predicate)
                    ));
        }

        /// <summary>Returns the last element of a sequence that satisfies a condition or a default value if no such element is found.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">An <see cref="IEnumerable{T}" /> to return an element from.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <param name="defaultValue">The default value to return if the sequence is empty.</param>
        /// <returns><paramref name="defaultValue" /> if the sequence is empty or if no elements pass the test in the predicate function; otherwise, the last element that passes the test in the predicate function.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source" /> or <paramref name="predicate" /> is <see langword="null" />.</exception>
        [DynamicDependency("LastOrDefault`1", typeof(Enumerable))]
        public static TSource LastOrDefault<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate, TSource defaultValue)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (predicate == null)
                throw Error.ArgumentNull(nameof(predicate));
            return source.Provider.Execute<TSource>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.LastOrDefault_TSource_4(typeof(TSource)),
                    source.Expression, Expression.Quote(predicate), Expression.Constant(defaultValue, typeof(TSource))
                ));
        }

        /// <summary>Returns the only element of a sequence, and throws an exception if there is not exactly one element in the sequence.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">An <see cref="System.Linq.IQueryable{T}" /> to return the single element of.</param>
        /// <returns>The single element of the input sequence.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <exception cref="System.InvalidOperationException"><paramref name="source" /> has more than one element.
        /// -or-
        /// The source sequence is empty.</exception>
        /// <remarks>
        /// <para>The <see cref="System.Linq.Queryable.Single{T}(System.Linq.IQueryable{T})" /> method generates a <see cref="System.Linq.Expressions.MethodCallExpression" /> that represents calling <see cref="System.Linq.Queryable.Single{T}(System.Linq.IQueryable{T})" /> itself as a constructed generic method. It then passes the <see cref="System.Linq.Expressions.MethodCallExpression" /> to the <see cref="System.Linq.IQueryProvider.Execute{T}(System.Linq.Expressions.Expression)" /> method of the <see cref="System.Linq.IQueryProvider" /> represented by the <see cref="O:System.Linq.IQueryable.Provider" /> property of the <paramref name="source" /> parameter.</para>
        /// <para>The query behavior that occurs as a result of executing an expression tree that represents calling <see cref="System.Linq.Queryable.Single{T}(System.Linq.IQueryable{T})" /> depends on the implementation of the type of the <paramref name="source" /> parameter. The expected behavior is that it returns the only element in <paramref name="source" />.</para>
        /// </remarks>
        /// <example>The following code example demonstrates how to use <see cref="System.Linq.Queryable.Single{T}(System.Linq.IQueryable{T})" /> to select the only element of an array.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Queryable/CS/queryable.cs" interactive="try-dotnet-method" id="Snippet79":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Queryable/VB/queryable.vb" id="Snippet79":::</example>
        [DynamicDependency("Single`1", typeof(Enumerable))]
        public static TSource Single<TSource>(this IQueryable<TSource> source)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            return source.Provider.Execute<TSource>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.Single_TSource_1(typeof(TSource)), source.Expression));
        }

        /// <summary>Returns the only element of a sequence that satisfies a specified condition, and throws an exception if more than one such element exists.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">An <see cref="System.Linq.IQueryable{T}" /> to return a single element from.</param>
        /// <param name="predicate">A function to test an element for a condition.</param>
        /// <returns>The single element of the input sequence that satisfies the condition in <paramref name="predicate" />.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> or <paramref name="predicate" /> is <see langword="null" />.</exception>
        /// <exception cref="System.InvalidOperationException">No element satisfies the condition in <paramref name="predicate" />.
        /// -or-
        /// More than one element satisfies the condition in <paramref name="predicate" />.
        /// -or-
        /// The source sequence is empty.</exception>
        /// <remarks>
        /// <para>This method has at least one parameter of type <see cref="System.Linq.Expressions.Expression{T}" /> whose type argument is one of the <see cref="System.Func{T1,T2}" /> types. For these parameters, you can pass in a lambda expression and it will be compiled to an <see cref="System.Linq.Expressions.Expression{T}" />.</para>
        /// <para>The <see cref="System.Linq.Queryable.Single{T}(System.Linq.IQueryable{T},System.Linq.Expressions.Expression{System.Func{T,bool}})" /> method generates a <see cref="System.Linq.Expressions.MethodCallExpression" /> that represents calling <see cref="System.Linq.Queryable.Single{T}(System.Linq.IQueryable{T},System.Linq.Expressions.Expression{System.Func{T,bool}})" /> itself as a constructed generic method. It then passes the <see cref="System.Linq.Expressions.MethodCallExpression" /> to the <see cref="System.Linq.IQueryProvider.Execute{T}(System.Linq.Expressions.Expression)" /> method of the <see cref="System.Linq.IQueryProvider" /> represented by the <see cref="O:System.Linq.IQueryable.Provider" /> property of the <paramref name="source" /> parameter.</para>
        /// <para>The query behavior that occurs as a result of executing an expression tree that represents calling <see cref="System.Linq.Queryable.Single{T}(System.Linq.IQueryable{T},System.Linq.Expressions.Expression{System.Func{T,bool}})" /> depends on the implementation of the type of the <paramref name="source" /> parameter. The expected behavior is that it returns the only element in <paramref name="source" /> that satisfies the condition specified by <paramref name="predicate" />.</para>
        /// </remarks>
        /// <example>The following code example demonstrates how to use <see cref="System.Linq.Queryable.Single{T}(System.Linq.IQueryable{T},System.Linq.Expressions.Expression{System.Func{T,bool}})" /> to select the only element of an array that satisfies a condition.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Queryable/CS/queryable.cs" interactive="try-dotnet-method" id="Snippet81":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Queryable/VB/queryable.vb" id="Snippet81":::</example>
        [DynamicDependency("Single`1", typeof(Enumerable))]
        public static TSource Single<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (predicate == null)
                throw Error.ArgumentNull(nameof(predicate));
            return source.Provider.Execute<TSource>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.Single_TSource_2(typeof(TSource)),
                    source.Expression, Expression.Quote(predicate)
                    ));
        }

        /// <summary>Returns the only element of a sequence, or a default value if the sequence is empty; this method throws an exception if there is more than one element in the sequence.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">An <see cref="System.Linq.IQueryable{T}" /> to return the single element of.</param>
        /// <returns>The single element of the input sequence, or <c>default</c>(<typeparamref name="TSource" />) if the sequence contains no elements.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <exception cref="System.InvalidOperationException"><paramref name="source" /> has more than one element.</exception>
        /// <remarks>
        /// <para>The <see cref="System.Linq.Queryable.SingleOrDefault{T}(System.Linq.IQueryable{T})" /> method generates a <see cref="System.Linq.Expressions.MethodCallExpression" /> that represents calling <see cref="System.Linq.Queryable.SingleOrDefault{T}(System.Linq.IQueryable{T})" /> itself as a constructed generic method. It then passes the <see cref="System.Linq.Expressions.MethodCallExpression" /> to the <see cref="System.Linq.IQueryProvider.Execute{T}(System.Linq.Expressions.Expression)" /> method of the <see cref="System.Linq.IQueryProvider" /> represented by the <see cref="O:System.Linq.IQueryable.Provider" /> property of the <paramref name="source" /> parameter.</para>
        /// <para>The query behavior that occurs as a result of executing an expression tree that represents calling <see cref="System.Linq.Queryable.SingleOrDefault{T}(System.Linq.IQueryable{T})" /> depends on the implementation of the type of the <paramref name="source" /> parameter. The expected behavior is that it returns the only element in <paramref name="source" />, or a default value if <paramref name="source" /> is empty.</para>
        /// <para>The <see cref="O:System.Linq.Queryable.SingleOrDefault" /> method does not provide a way to specify a default value. If you want to specify a default value other than `default(TSource)`, use the <see cref="System.Linq.Queryable.DefaultIfEmpty{T}(System.Linq.IQueryable{T},T)" /> method as described in the Example section.</para>
        /// </remarks>
        /// <example>The following code example demonstrates how to use <see cref="System.Linq.Queryable.SingleOrDefault{T}(System.Linq.IQueryable{T})" /> to select the only element of an array. The second query demonstrates that <see cref="System.Linq.Queryable.SingleOrDefault{T}(System.Linq.IQueryable{T})" /> returns a default value when the sequence does not contain exactly one element.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Queryable/CS/queryable.cs" interactive="try-dotnet-method" id="Snippet83":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Queryable/VB/queryable.vb" id="Snippet83":::
        /// Sometimes the value of `default(TSource)` is not the default value that you want to use if the collection contains no elements. Instead of checking the result for the unwanted default value and then changing it if necessary, you can use the <see cref="System.Linq.Queryable.DefaultIfEmpty{T}(System.Linq.IQueryable{T},T)" /> method to specify the default value that you want to use if the collection is empty. Then, call <see cref="System.Linq.Queryable.Single{T}(System.Linq.IQueryable{T})" /> to obtain the element. The following code example uses both techniques to obtain a default value of 1 if a collection of page numbers is empty. Because the default value for an integer is 0, which is not usually a valid page number, the default value must be specified as 1 instead. The first result variable is checked for the unwanted default value after the query is completed. The second result variable is obtained by calling <see cref="System.Linq.Queryable.DefaultIfEmpty{T}(System.Linq.IQueryable{T},T)" /> to specify a default value of 1.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Queryable/CS/queryable.cs" interactive="try-dotnet-method" id="Snippet133":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Queryable/VB/queryable.vb" id="Snippet133":::</example>
        [DynamicDependency("SingleOrDefault`1", typeof(Enumerable))]
        public static TSource? SingleOrDefault<TSource>(this IQueryable<TSource> source)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            return source.Provider.Execute<TSource>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.SingleOrDefault_TSource_1(typeof(TSource)), source.Expression));
        }

        /// <summary>Returns the only element of a sequence, or a default value if the sequence is empty; this method throws an exception if there is more than one element in the sequence.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">An <see cref="IEnumerable{T}" /> to return the single element of.</param>
        /// <param name="defaultValue">The default value to return if the sequence is empty.</param>
        /// <returns>The single element of the input sequence, or <paramref name="defaultValue" /> if the sequence contains no elements.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <exception cref="InvalidOperationException">The input sequence contains more than one element.</exception>
        [DynamicDependency("SingleOrDefault`1", typeof(Enumerable))]
        public static TSource SingleOrDefault<TSource>(this IQueryable<TSource> source, TSource defaultValue)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            return source.Provider.Execute<TSource>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.SingleOrDefault_TSource_3(typeof(TSource)),
                    source.Expression, Expression.Constant(defaultValue, typeof(TSource))));

        }

        /// <summary>Returns the only element of a sequence that satisfies a specified condition or a default value if no such element exists; this method throws an exception if more than one element satisfies the condition.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">An <see cref="System.Linq.IQueryable{T}" /> to return a single element from.</param>
        /// <param name="predicate">A function to test an element for a condition.</param>
        /// <returns>The single element of the input sequence that satisfies the condition in <paramref name="predicate" />, or <c>default</c>(<typeparamref name="TSource" />) if no such element is found.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> or <paramref name="predicate" /> is <see langword="null" />.</exception>
        /// <exception cref="System.InvalidOperationException">More than one element satisfies the condition in <paramref name="predicate" />.</exception>
        /// <remarks>
        /// <para>This method has at least one parameter of type <see cref="System.Linq.Expressions.Expression{T}" /> whose type argument is one of the <see cref="System.Func{T1,T2}" /> types. For these parameters, you can pass in a lambda expression and it will be compiled to an <see cref="System.Linq.Expressions.Expression{T}" />.</para>
        /// <para>The <see cref="System.Linq.Queryable.SingleOrDefault{T}(System.Linq.IQueryable{T},System.Linq.Expressions.Expression{System.Func{T,bool}})" /> method generates a <see cref="System.Linq.Expressions.MethodCallExpression" /> that represents calling <see cref="System.Linq.Queryable.SingleOrDefault{T}(System.Linq.IQueryable{T},System.Linq.Expressions.Expression{System.Func{T,bool}})" /> itself as a constructed generic method. It then passes the <see cref="System.Linq.Expressions.MethodCallExpression" /> to the <see cref="System.Linq.IQueryProvider.Execute{T}(System.Linq.Expressions.Expression)" /> method of the <see cref="System.Linq.IQueryProvider" /> represented by the <see cref="O:System.Linq.IQueryable.Provider" /> property of the <paramref name="source" /> parameter.</para>
        /// <para>The query behavior that occurs as a result of executing an expression tree that represents calling <see cref="System.Linq.Queryable.SingleOrDefault{T}(System.Linq.IQueryable{T},System.Linq.Expressions.Expression{System.Func{T,bool}})" /> depends on the implementation of the type of the <paramref name="source" /> parameter. The expected behavior is that it returns the only element in <paramref name="source" /> that satisfies the condition specified by <paramref name="predicate" />, or a default value if no such element exists.</para>
        /// </remarks>
        /// <example>The following code example demonstrates how to use <see cref="System.Linq.Queryable.SingleOrDefault{T}(System.Linq.IQueryable{T},System.Linq.Expressions.Expression{System.Func{T,bool}})" /> to select the only element of an array that satisfies a condition. The second query demonstrates that <see cref="System.Linq.Queryable.SingleOrDefault{T}(System.Linq.IQueryable{T},System.Linq.Expressions.Expression{System.Func{T,bool}})" /> returns a default value when the sequence does not contain exactly one element that satisfies the condition.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Queryable/CS/queryable.cs" interactive="try-dotnet-method" id="Snippet85":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Queryable/VB/queryable.vb" id="Snippet85":::</example>
        [DynamicDependency("SingleOrDefault`1", typeof(Enumerable))]
        public static TSource? SingleOrDefault<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (predicate == null)
                throw Error.ArgumentNull(nameof(predicate));
            return source.Provider.Execute<TSource>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.SingleOrDefault_TSource_2(typeof(TSource)),
                    source.Expression, Expression.Quote(predicate)
                    ));
        }

        /// <summary>Returns the only element of a sequence that satisfies a specified condition or a default value if no such element exists; this method throws an exception if more than one element satisfies the condition.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">An <see cref="IEnumerable{T}" /> to return a single element from.</param>
        /// <param name="predicate">A function to test an element for a condition.</param>
        /// <param name="defaultValue">The default value to return if the sequence is empty.</param>
        /// <returns>The single element of the input sequence that satisfies the condition, or <paramref name="defaultValue" /> if no such element is found.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source" /> or <paramref name="predicate" /> is <see langword="null" />.</exception>
        /// <exception cref="InvalidOperationException">More than one element satisfies the condition in <paramref name="predicate" />.</exception>
        [DynamicDependency("SingleOrDefault`1", typeof(Enumerable))]
        public static TSource SingleOrDefault<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate, TSource defaultValue)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (predicate == null)
                throw Error.ArgumentNull(nameof(predicate));
            return source.Provider.Execute<TSource>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.SingleOrDefault_TSource_4(typeof(TSource)),
                    source.Expression, Expression.Quote(predicate), Expression.Constant(defaultValue, typeof(TSource))
                ));
        }

        /// <summary>Returns the element at a specified index in a sequence.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">An <see cref="System.Linq.IQueryable{T}" /> to return an element from.</param>
        /// <param name="index">The zero-based index of the element to retrieve.</param>
        /// <returns>The element at the specified position in <paramref name="source" />.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="index" /> is less than zero.</exception>
        /// <remarks>
        /// <para>The <see cref="System.Linq.Queryable.ElementAt{T}(System.Linq.IQueryable{T},int)" /> method generates a <see cref="System.Linq.Expressions.MethodCallExpression" /> that represents calling <see cref="System.Linq.Queryable.ElementAt{T}(System.Linq.IQueryable{T},int)" /> itself as a constructed generic method. It then passes the <see cref="System.Linq.Expressions.MethodCallExpression" /> to the <see cref="System.Linq.IQueryProvider.Execute{T}(System.Linq.Expressions.Expression)" /> method of the <see cref="System.Linq.IQueryProvider" /> represented by the <see cref="O:System.Linq.IQueryable.Provider" /> property of the <paramref name="source" /> parameter.</para>
        /// <para>The query behavior that occurs as a result of executing an expression tree that represents calling <see cref="System.Linq.Queryable.ElementAt{T}(System.Linq.IQueryable{T},int)" /> depends on the implementation of the type of the <paramref name="source" /> parameter. The expected behavior is that it returns the item at position <paramref name="index" /> in <paramref name="source" />.</para>
        /// </remarks>
        /// <example>The following code example demonstrates how to use <see cref="System.Linq.Queryable.ElementAt{T}(System.Linq.IQueryable{T},int)" /> to return an element at a specific position in a sequence.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Queryable/CS/queryable.cs" interactive="try-dotnet-method" id="Snippet28":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Queryable/VB/queryable.vb" id="Snippet28":::</example>
        [DynamicDependency("ElementAt`1", typeof(Enumerable))]
        public static TSource ElementAt<TSource>(this IQueryable<TSource> source, int index)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (index < 0)
                throw Error.ArgumentOutOfRange(nameof(index));
            return source.Provider.Execute<TSource>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.ElementAt_Int32_TSource_2(typeof(TSource)),
                    source.Expression, Expression.Constant(index)
                    ));
        }

        /// <summary>Returns the element at a specified index in a sequence.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">An <see cref="IQueryable{T}" /> to return an element from.</param>
        /// <param name="index">The index of the element to retrieve, which is either from the start or the end.</param>
        /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index" /> is outside the bounds of the <paramref name="source" /> sequence.</exception>
        /// <returns>The element at the specified position in the <paramref name="source" /> sequence.</returns>
        [DynamicDependency("ElementAt`1", typeof(Enumerable))]
        public static TSource ElementAt<TSource>(this IQueryable<TSource> source, Index index)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (index.IsFromEnd && index.Value == 0)
                throw Error.ArgumentOutOfRange(nameof(index));
            return source.Provider.Execute<TSource>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.ElementAt_Index_TSource_2(typeof(TSource)),
                    source.Expression, Expression.Constant(index)
                    ));
        }

        /// <summary>Returns the element at a specified index in a sequence or a default value if the index is out of range.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">An <see cref="System.Linq.IQueryable{T}" /> to return an element from.</param>
        /// <param name="index">The zero-based index of the element to retrieve.</param>
        /// <returns><c>default</c>(<typeparamref name="TSource" />) if <paramref name="index" /> is outside the bounds of <paramref name="source" />; otherwise, the element at the specified position in <paramref name="source" />.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <remarks>
        /// <para>The <see cref="System.Linq.Queryable.ElementAtOrDefault{T}(System.Linq.IQueryable{T},int)" /> method generates a <see cref="System.Linq.Expressions.MethodCallExpression" /> that represents calling <see cref="System.Linq.Queryable.ElementAtOrDefault{T}(System.Linq.IQueryable{T},int)" /> itself as a constructed generic method. It then passes the <see cref="System.Linq.Expressions.MethodCallExpression" /> to the <see cref="System.Linq.IQueryProvider.Execute{T}(System.Linq.Expressions.Expression)" /> method of the <see cref="System.Linq.IQueryProvider" /> represented by the <see cref="O:System.Linq.IQueryable.Provider" /> property of the <paramref name="source" /> parameter.</para>
        /// <para>The query behavior that occurs as a result of executing an expression tree that represents calling <see cref="System.Linq.Queryable.ElementAtOrDefault{T}(System.Linq.IQueryable{T},int)" /> depends on the implementation of the type of the <paramref name="source" /> parameter. The expected behavior is that it returns the item at position <paramref name="index" /> in <paramref name="source" />, or `default(TSource)` if <paramref name="index" /> is outside the bounds of <paramref name="source" />.</para>
        /// </remarks>
        /// <example>The following code example demonstrates how to use <see cref="System.Linq.Queryable.ElementAtOrDefault{T}(System.Linq.IQueryable{T},int)" />. This example uses a value for <paramref name="index" /> that is outside the bounds of the source sequence.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Queryable/CS/queryable.cs" interactive="try-dotnet-method" id="Snippet29":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Queryable/VB/queryable.vb" id="Snippet29":::</example>
        [DynamicDependency("ElementAtOrDefault`1", typeof(Enumerable))]
        public static TSource? ElementAtOrDefault<TSource>(this IQueryable<TSource> source, int index)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            return source.Provider.Execute<TSource>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.ElementAtOrDefault_Int32_TSource_2(typeof(TSource)),
                    source.Expression, Expression.Constant(index)
                    ));
        }

        /// <summary>Returns the element at a specified index in a sequence or a default value if the index is out of range.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">An <see cref="IQueryable{T}" /> to return an element from.</param>
        /// <param name="index">The index of the element to retrieve, which is either from the start or the end.</param>
        /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <returns><see langword="default" /> if <paramref name="index" /> is outside the bounds of the <paramref name="source" /> sequence; otherwise, the element at the specified position in the <paramref name="source" /> sequence.</returns>
        [DynamicDependency("ElementAtOrDefault`1", typeof(Enumerable))]
        public static TSource? ElementAtOrDefault<TSource>(this IQueryable<TSource> source, Index index)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            return source.Provider.Execute<TSource>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.ElementAtOrDefault_Index_TSource_2(typeof(TSource)),
                    source.Expression, Expression.Constant(index)
                    ));
        }

        /// <summary>Returns the elements of the specified sequence or the type parameter's default value in a singleton collection if the sequence is empty.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">The <see cref="System.Linq.IQueryable{T}" /> to return a default value for if empty.</param>
        /// <returns>An <see cref="System.Linq.IQueryable{T}" /> that contains <see langword="default" />(<typeparamref name="TSource" />) if <paramref name="source" /> is empty; otherwise, <paramref name="source" />.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <remarks>
        /// <para>The <see cref="System.Linq.Queryable.DefaultIfEmpty{T}(System.Linq.IQueryable{T})" /> method generates a <see cref="System.Linq.Expressions.MethodCallExpression" /> that represents calling <see cref="System.Linq.Queryable.DefaultIfEmpty{T}(System.Linq.IQueryable{T})" /> itself as a constructed generic method. It then passes the <see cref="System.Linq.Expressions.MethodCallExpression" /> to the <see cref="System.Linq.IQueryProvider.CreateQuery{T}(System.Linq.Expressions.Expression)" /> method of the <see cref="System.Linq.IQueryProvider" /> represented by the <see cref="O:System.Linq.IQueryable.Provider" /> property of the <paramref name="source" /> parameter.</para>
        /// <para>The query behavior that occurs as a result of executing an expression tree that represents calling <see cref="System.Linq.Queryable.DefaultIfEmpty{T}(System.Linq.IQueryable{T})" /> depends on the implementation of the type of the <paramref name="source" /> parameter. The expected behavior is that it returns <paramref name="source" /> if it is not empty. Otherwise, it returns an <see cref="System.Linq.IQueryable{T}" /> that contains `default(TSource)`.</para>
        /// </remarks>
        /// <example>The following code examples demonstrate how to use <see cref="System.Linq.Queryable.DefaultIfEmpty{T}(System.Linq.IQueryable{T})" /> to provide a default value in case the source sequence is empty.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Queryable/CS/queryable.cs" id="Snippet24":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Queryable/VB/queryable.vb" id="Snippet24":::</example>
        [DynamicDependency("DefaultIfEmpty`1", typeof(Enumerable))]
        public static IQueryable<TSource> DefaultIfEmpty<TSource>(this IQueryable<TSource> source)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            return source.Provider.CreateQuery<TSource>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.DefaultIfEmpty_TSource_1(typeof(TSource)), source.Expression));
        }

        /// <summary>Returns the elements of the specified sequence or the specified value in a singleton collection if the sequence is empty.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">The <see cref="System.Linq.IQueryable{T}" /> to return the specified value for if empty.</param>
        /// <param name="defaultValue">The value to return if the sequence is empty.</param>
        /// <returns>An <see cref="System.Linq.IQueryable{T}" /> that contains <paramref name="defaultValue" /> if <paramref name="source" /> is empty; otherwise, <paramref name="source" />.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <remarks>
        /// <para>The <see cref="System.Linq.Queryable.DefaultIfEmpty{T}(System.Linq.IQueryable{T},T)" /> method generates a <see cref="System.Linq.Expressions.MethodCallExpression" /> that represents calling <see cref="System.Linq.Queryable.DefaultIfEmpty{T}(System.Linq.IQueryable{T},T)" /> itself as a constructed generic method. It then passes the <see cref="System.Linq.Expressions.MethodCallExpression" /> to the <see cref="System.Linq.IQueryProvider.CreateQuery{T}(System.Linq.Expressions.Expression)" /> method of the <see cref="System.Linq.IQueryProvider" /> represented by the <see cref="O:System.Linq.IQueryable.Provider" /> property of the <paramref name="source" /> parameter.</para>
        /// <para>The query behavior that occurs as a result of executing an expression tree that represents calling <see cref="System.Linq.Queryable.DefaultIfEmpty{T}(System.Linq.IQueryable{T},T)" /> depends on the implementation of the type of the <paramref name="source" /> parameter. The expected behavior is that it returns <paramref name="source" /> if it is not empty. Otherwise, it returns an <see cref="System.Linq.IQueryable{T}" /> that contains <paramref name="defaultValue" />.</para>
        /// </remarks>
        /// <example>The following code example shows a situation in which it is useful to call <see cref="System.Linq.Queryable.DefaultIfEmpty{T}(System.Linq.IQueryable{T},T)" /> in a LINQ query. A default value is passed to <see cref="System.Linq.Queryable.DefaultIfEmpty{T}(System.Linq.IQueryable{T},T)" /> in this example.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Queryable/CS/queryable.cs" id="Snippet25":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Queryable/VB/queryable.vb" id="Snippet25":::</example>
        [DynamicDependency("DefaultIfEmpty`1", typeof(Enumerable))]
        public static IQueryable<TSource> DefaultIfEmpty<TSource>(this IQueryable<TSource> source, TSource defaultValue)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            return source.Provider.CreateQuery<TSource>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.DefaultIfEmpty_TSource_2(typeof(TSource)),
                    source.Expression, Expression.Constant(defaultValue, typeof(TSource))
                    ));
        }

        /// <summary>Determines whether a sequence contains a specified element by using the default equality comparer.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">An <see cref="System.Linq.IQueryable{T}" /> in which to locate <paramref name="item" />.</param>
        /// <param name="item">The object to locate in the sequence.</param>
        /// <returns><see langword="true" /> if the input sequence contains an element that has the specified value; otherwise, <see langword="false" />.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <remarks>
        /// <para>The <see cref="System.Linq.Queryable.Contains{T}(System.Linq.IQueryable{T},T)" /> method generates a <see cref="System.Linq.Expressions.MethodCallExpression" /> that represents calling <see cref="System.Linq.Queryable.Contains{T}(System.Linq.IQueryable{T},T)" /> itself as a constructed generic method. It then passes the <see cref="System.Linq.Expressions.MethodCallExpression" /> to the <see cref="System.Linq.IQueryProvider.Execute{T}(System.Linq.Expressions.Expression)" /> method of the <see cref="System.Linq.IQueryProvider" /> represented by the <see cref="O:System.Linq.IQueryable.Provider" /> property of the <paramref name="source" /> parameter.</para>
        /// <para>The query behavior that occurs as a result of executing an expression tree that represents calling <see cref="System.Linq.Queryable.Contains{T}(System.Linq.IQueryable{T},T)" /> depends on the implementation of the type of the <paramref name="source" /> parameter. The expected behavior is that it determines if <paramref name="source" /> contains <paramref name="item" />.</para>
        /// </remarks>
        /// <example>The following code example demonstrates how to use <see cref="System.Linq.Queryable.Contains{T}(System.Linq.IQueryable{T},T)" /> to determine whether a sequence contains a specific element.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Queryable/CS/queryable.cs" interactive="try-dotnet-method" id="Snippet21":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Queryable/VB/queryable.vb" id="Snippet21":::</example>
        [DynamicDependency("Contains`1", typeof(Enumerable))]
        public static bool Contains<TSource>(this IQueryable<TSource> source, TSource item)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            return source.Provider.Execute<bool>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.Contains_TSource_2(typeof(TSource)),
                    source.Expression, Expression.Constant(item, typeof(TSource))
                    ));
        }

        /// <summary>Determines whether a sequence contains a specified element by using a specified <see cref="System.Collections.Generic.IEqualityComparer{T}" />.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">An <see cref="System.Linq.IQueryable{T}" /> in which to locate <paramref name="item" />.</param>
        /// <param name="item">The object to locate in the sequence.</param>
        /// <param name="comparer">An <see cref="System.Collections.Generic.IEqualityComparer{T}" /> to compare values.</param>
        /// <returns><see langword="true" /> if the input sequence contains an element that has the specified value; otherwise, <see langword="false" />.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <remarks>
        /// <para>The <see cref="System.Linq.Queryable.Contains{T}(System.Linq.IQueryable{T},T,System.Collections.Generic.IEqualityComparer{T})" /> method generates a <see cref="System.Linq.Expressions.MethodCallExpression" /> that represents calling <see cref="System.Linq.Queryable.Contains{T}(System.Linq.IQueryable{T},T,System.Collections.Generic.IEqualityComparer{T})" /> itself as a constructed generic method. It then passes the <see cref="System.Linq.Expressions.MethodCallExpression" /> to the <see cref="System.Linq.IQueryProvider.Execute{T}(System.Linq.Expressions.Expression)" /> method of the <see cref="System.Linq.IQueryProvider" /> represented by the <see cref="O:System.Linq.IQueryable.Provider" /> property of the <paramref name="source" /> parameter.</para>
        /// <para>The query behavior that occurs as a result of executing an expression tree that represents calling <see cref="System.Linq.Queryable.Contains{T}(System.Linq.IQueryable{T},T,System.Collections.Generic.IEqualityComparer{T})" /> depends on the implementation of the type of the <paramref name="source" /> parameter. The expected behavior is that it determines if <paramref name="source" /> contains <paramref name="item" /> by using <paramref name="comparer" /> to compare values.</para>
        /// </remarks>
        [DynamicDependency("Contains`1", typeof(Enumerable))]
        public static bool Contains<TSource>(this IQueryable<TSource> source, TSource item, IEqualityComparer<TSource>? comparer)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            return source.Provider.Execute<bool>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.Contains_TSource_3(typeof(TSource)),
                    source.Expression, Expression.Constant(item, typeof(TSource)), Expression.Constant(comparer, typeof(IEqualityComparer<TSource>))
                    ));
        }

        /// <summary>Inverts the order of the elements in a sequence.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence of values to reverse.</param>
        /// <returns>An <see cref="System.Linq.IQueryable{T}" /> whose elements correspond to those of the input sequence in reverse order.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <remarks>
        /// <para>The <see cref="System.Linq.Queryable.Reverse{T}(System.Linq.IQueryable{T})" /> method generates a <see cref="System.Linq.Expressions.MethodCallExpression" /> that represents calling <see cref="System.Linq.Queryable.Reverse{T}(System.Linq.IQueryable{T})" /> itself as a constructed generic method. It then passes the <see cref="System.Linq.Expressions.MethodCallExpression" /> to the <see cref="System.Linq.IQueryProvider.CreateQuery{T}(System.Linq.Expressions.Expression)" /> method of the <see cref="System.Linq.IQueryProvider" /> represented by the <see cref="O:System.Linq.IQueryable.Provider" /> property of the <paramref name="source" /> parameter.</para>
        /// <para>The query behavior that occurs as a result of executing an expression tree that represents calling <see cref="System.Linq.Queryable.Reverse{T}(System.Linq.IQueryable{T})" /> depends on the implementation of the type of the <paramref name="source" /> parameter. The expected behavior is that it reverses the order of the elements in <paramref name="source" />.</para>
        /// </remarks>
        /// <example>The following code example demonstrates how to use <see cref="System.Linq.Queryable.Reverse{T}(System.Linq.IQueryable{T})" /> to reverse the order of elements in an array.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Queryable/CS/queryable.cs" interactive="try-dotnet-method" id="Snippet74":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Queryable/VB/queryable.vb" id="Snippet74":::</example>
        [DynamicDependency("Reverse`1", typeof(Enumerable))]
        public static IQueryable<TSource> Reverse<TSource>(this IQueryable<TSource> source)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            return source.Provider.CreateQuery<TSource>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.Reverse_TSource_1(typeof(TSource)), source.Expression));
        }

        /// <summary>Determines whether two sequences are equal by using the default equality comparer to compare elements.</summary>
        /// <typeparam name="TSource">The type of the elements of the input sequences.</typeparam>
        /// <param name="source1">An <see cref="System.Linq.IQueryable{T}" /> whose elements to compare to those of <paramref name="source2" />.</param>
        /// <param name="source2">An <see cref="System.Collections.Generic.IEnumerable{T}" /> whose elements to compare to those of the first sequence.</param>
        /// <returns><see langword="true" /> if the two source sequences are of equal length and their corresponding elements compare equal; otherwise, <see langword="false" />.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source1" /> or <paramref name="source2" /> is <see langword="null" />.</exception>
        /// <remarks>
        /// <para>The <see cref="System.Linq.Queryable.SequenceEqual{T}(System.Linq.IQueryable{T},System.Collections.Generic.IEnumerable{T})" /> method generates a <see cref="System.Linq.Expressions.MethodCallExpression" /> that represents calling <see cref="System.Linq.Queryable.SequenceEqual{T}(System.Linq.IQueryable{T},System.Collections.Generic.IEnumerable{T})" /> itself as a constructed generic method. It then passes the <see cref="System.Linq.Expressions.MethodCallExpression" /> to the <see cref="System.Linq.IQueryProvider.Execute{T}(System.Linq.Expressions.Expression)" /> method of the <see cref="System.Linq.IQueryProvider" /> represented by the <see cref="O:System.Linq.IQueryable.Provider" /> property of the <paramref name="source1" /> parameter.</para>
        /// <para>The query behavior that occurs as a result of executing an expression tree that represents calling <see cref="System.Linq.Queryable.SequenceEqual{T}(System.Linq.IQueryable{T},System.Collections.Generic.IEnumerable{T})" /> depends on the implementation of the type of the <paramref name="source1" /> parameter. The expected behavior is that it determines if the two source sequences are equal.</para>
        /// </remarks>
        /// <example>The following code example demonstrates how to use <see cref="System.Linq.Queryable.SequenceEqual{T}(System.Linq.IQueryable{T},System.Collections.Generic.IEnumerable{T})" /> to determine whether two sequences are equal. In this example the sequences are equal.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Queryable/CS/queryable.cs" id="Snippet32":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Queryable/VB/queryable.vb" id="Snippet32":::
        /// The following code example compares two sequences that are not equal.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Queryable/CS/queryable.cs" id="Snippet33":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Queryable/VB/queryable.vb" id="Snippet33":::</example>
        [DynamicDependency("SequenceEqual`1", typeof(Enumerable))]
        public static bool SequenceEqual<TSource>(this IQueryable<TSource> source1, IEnumerable<TSource> source2)
        {
            if (source1 == null)
                throw Error.ArgumentNull(nameof(source1));
            if (source2 == null)
                throw Error.ArgumentNull(nameof(source2));
            return source1.Provider.Execute<bool>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.SequenceEqual_TSource_2(typeof(TSource)),
                    source1.Expression, GetSourceExpression(source2)
                    ));
        }

        /// <summary>Determines whether two sequences are equal by using a specified <see cref="System.Collections.Generic.IEqualityComparer{T}" /> to compare elements.</summary>
        /// <typeparam name="TSource">The type of the elements of the input sequences.</typeparam>
        /// <param name="source1">An <see cref="System.Linq.IQueryable{T}" /> whose elements to compare to those of <paramref name="source2" />.</param>
        /// <param name="source2">An <see cref="System.Collections.Generic.IEnumerable{T}" /> whose elements to compare to those of the first sequence.</param>
        /// <param name="comparer">An <see cref="System.Collections.Generic.IEqualityComparer{T}" /> to use to compare elements.</param>
        /// <returns><see langword="true" /> if the two source sequences are of equal length and their corresponding elements compare equal; otherwise, <see langword="false" />.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source1" /> or <paramref name="source2" /> is <see langword="null" />.</exception>
        /// <remarks>
        /// <para>The <see cref="System.Linq.Queryable.SequenceEqual{T}(System.Linq.IQueryable{T},System.Collections.Generic.IEnumerable{T},System.Collections.Generic.IEqualityComparer{T})" /> method generates a <see cref="System.Linq.Expressions.MethodCallExpression" /> that represents calling <see cref="System.Linq.Queryable.SequenceEqual{T}(System.Linq.IQueryable{T},System.Collections.Generic.IEnumerable{T},System.Collections.Generic.IEqualityComparer{T})" /> itself as a constructed generic method. It then passes the <see cref="System.Linq.Expressions.MethodCallExpression" /> to the <see cref="System.Linq.IQueryProvider.Execute{T}(System.Linq.Expressions.Expression)" /> method of the <see cref="System.Linq.IQueryProvider" /> represented by the <see cref="O:System.Linq.IQueryable.Provider" /> property of the <paramref name="source1" /> parameter.</para>
        /// <para>The query behavior that occurs as a result of executing an expression tree that represents calling <see cref="System.Linq.Queryable.SequenceEqual{T}(System.Linq.IQueryable{T},System.Collections.Generic.IEnumerable{T},System.Collections.Generic.IEqualityComparer{T})" /> depends on the implementation of the type of the <paramref name="source1" /> parameter. The expected behavior is that it determines if the two source sequences are equal by using <paramref name="comparer" /> to compare elements.</para>
        /// </remarks>
        [DynamicDependency("SequenceEqual`1", typeof(Enumerable))]
        public static bool SequenceEqual<TSource>(this IQueryable<TSource> source1, IEnumerable<TSource> source2, IEqualityComparer<TSource>? comparer)
        {
            if (source1 == null)
                throw Error.ArgumentNull(nameof(source1));
            if (source2 == null)
                throw Error.ArgumentNull(nameof(source2));
            return source1.Provider.Execute<bool>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.SequenceEqual_TSource_3(typeof(TSource)),
                    source1.Expression,
                    GetSourceExpression(source2),
                    Expression.Constant(comparer, typeof(IEqualityComparer<TSource>))
                    ));
        }

        /// <summary>Determines whether a sequence contains any elements.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence to check for being empty.</param>
        /// <returns><see langword="true" /> if the source sequence contains any elements; otherwise, <see langword="false" />.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <remarks>
        /// <para>The <see cref="System.Linq.Queryable.Any{T}(System.Linq.IQueryable{T})" /> method generates a <see cref="System.Linq.Expressions.MethodCallExpression" /> that represents calling <see cref="System.Linq.Queryable.Any{T}(System.Linq.IQueryable{T})" /> itself as a constructed generic method. It then passes the <see cref="System.Linq.Expressions.MethodCallExpression" /> to the <see cref="System.Linq.IQueryProvider.Execute{T}(System.Linq.Expressions.Expression)" /> method of the <see cref="System.Linq.IQueryProvider" /> represented by the <see cref="O:System.Linq.IQueryable.Provider" /> property of the <paramref name="source" /> parameter.</para>
        /// <para>The query behavior that occurs as a result of executing an expression tree that represents calling <see cref="System.Linq.Queryable.Any{T}(System.Linq.IQueryable{T})" /> depends on the implementation of the type of the <paramref name="source" /> parameter. The expected behavior is that it determines if <paramref name="source" /> contains any elements.</para>
        /// </remarks>
        /// <example>The following code example demonstrates how to use <see cref="System.Linq.Queryable.Any{T}(System.Linq.IQueryable{T})" /> to determine whether a sequence contains any elements.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Queryable/CS/queryable.cs" interactive="try-dotnet-method" id="Snippet5":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Queryable/VB/queryable.vb" id="Snippet5":::
        /// The Boolean value that the <see cref="System.Linq.Queryable.Any{T}(System.Linq.IQueryable{T})" /> method returns is typically used in the predicate of a `where` clause (`Where` clause in Visual Basic) or a direct call to the <see cref="System.Linq.Queryable.Where{T}(System.Linq.IQueryable{T},System.Linq.Expressions.Expression{System.Func{T,bool}})" /> method. The following example demonstrates this use of the `Any` method.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Queryable/CS/queryable.cs" id="Snippet135":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Queryable/VB/queryable.vb" id="Snippet135":::</example>
        [DynamicDependency("Any`1", typeof(Enumerable))]
        public static bool Any<TSource>(this IQueryable<TSource> source)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            return source.Provider.Execute<bool>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.Any_TSource_1(typeof(TSource)), source.Expression));
        }

        /// <summary>Determines whether any element of a sequence satisfies a condition.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence whose elements to test for a condition.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <returns><see langword="true" /> if any elements in the source sequence pass the test in the specified predicate; otherwise, <see langword="false" />.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> or <paramref name="predicate" /> is <see langword="null" />.</exception>
        /// <remarks>
        /// <para>This method has at least one parameter of type <see cref="System.Linq.Expressions.Expression{T}" /> whose type argument is one of the <see cref="System.Func{T1,T2}" /> types. For these parameters, you can pass in a lambda expression and it will be compiled to an <see cref="System.Linq.Expressions.Expression{T}" />.</para>
        /// <para>The <see cref="System.Linq.Queryable.Any{T}(System.Linq.IQueryable{T},System.Linq.Expressions.Expression{System.Func{T,bool}})" /> method generates a <see cref="System.Linq.Expressions.MethodCallExpression" /> that represents calling <see cref="System.Linq.Queryable.Any{T}(System.Linq.IQueryable{T},System.Linq.Expressions.Expression{System.Func{T,bool}})" /> itself as a constructed generic method. It then passes the <see cref="System.Linq.Expressions.MethodCallExpression" /> to the <see cref="System.Linq.IQueryProvider.Execute{T}(System.Linq.Expressions.Expression)" /> method of the <see cref="System.Linq.IQueryProvider" /> represented by the <see cref="O:System.Linq.IQueryable.Provider" /> property of the <paramref name="source" /> parameter.</para>
        /// <para>The query behavior that occurs as a result of executing an expression tree that represents calling <see cref="System.Linq.Queryable.Any{T}(System.Linq.IQueryable{T},System.Linq.Expressions.Expression{System.Func{T,bool}})" /> depends on the implementation of the type of the <paramref name="source" /> parameter. The expected behavior is that it determines if any of the elements of <paramref name="source" /> satisfy the condition specified by <paramref name="predicate" />.</para>
        /// </remarks>
        /// <example>The following code example demonstrates how to use <see cref="System.Linq.Queryable.Any{T}(System.Linq.IQueryable{T},System.Linq.Expressions.Expression{System.Func{T,bool}})" /> to determine whether any element in a sequence satisfies a condition.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Queryable/CS/queryable.cs" id="Snippet6":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Queryable/VB/queryable.vb" id="Snippet6":::</example>
        [DynamicDependency("Any`1", typeof(Enumerable))]
        public static bool Any<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (predicate == null)
                throw Error.ArgumentNull(nameof(predicate));
            return source.Provider.Execute<bool>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.Any_TSource_2(typeof(TSource)),
                    source.Expression, Expression.Quote(predicate)
                    ));
        }

        /// <summary>Determines whether all the elements of a sequence satisfy a condition.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence whose elements to test for a condition.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <returns><see langword="true" /> if every element of the source sequence passes the test in the specified predicate, or if the sequence is empty; otherwise, <see langword="false" />.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> or <paramref name="predicate" /> is <see langword="null" />.</exception>
        /// <remarks>
        /// <para>This method has at least one parameter of type <see cref="System.Linq.Expressions.Expression{T}" /> whose type argument is one of the <see cref="System.Func{T1,T2}" /> types. For these parameters, you can pass in a lambda expression and it will be compiled to an <see cref="System.Linq.Expressions.Expression{T}" />.</para>
        /// <para>The <see cref="System.Linq.Queryable.All{T}(System.Linq.IQueryable{T},System.Linq.Expressions.Expression{System.Func{T,bool}})" /> method generates a <see cref="System.Linq.Expressions.MethodCallExpression" /> that represents calling <see cref="System.Linq.Queryable.All{T}(System.Linq.IQueryable{T},System.Linq.Expressions.Expression{System.Func{T,bool}})" /> itself as a constructed generic method. It then passes the <see cref="System.Linq.Expressions.MethodCallExpression" /> to the <see cref="System.Linq.IQueryProvider.Execute{T}(System.Linq.Expressions.Expression)" /> method of the <see cref="System.Linq.IQueryProvider" /> represented by the <see cref="O:System.Linq.IQueryable.Provider" /> property of the <paramref name="source" /> parameter.</para>
        /// <para>The query behavior that occurs as a result of executing an expression tree that represents calling <see cref="System.Linq.Queryable.All{T}(System.Linq.IQueryable{T},System.Linq.Expressions.Expression{System.Func{T,bool}})" /> depends on the implementation of the <paramref name="source" /> parameter's type. The expected behavior is that it determines if all the elements in <paramref name="source" /> satisfy the condition in <paramref name="predicate" />.</para>
        /// </remarks>
        /// <example>The following code example demonstrates how to use <see cref="System.Linq.Queryable.All{T}(System.Linq.IQueryable{T},System.Linq.Expressions.Expression{System.Func{T,bool}})" /> to determine whether all the elements in a sequence satisfy a condition.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Queryable/CS/queryable.cs" id="Snippet4":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Queryable/VB/queryable.vb" id="Snippet4":::
        /// The Boolean value that the <see cref="System.Linq.Queryable.All{T}(System.Linq.IQueryable{T},System.Linq.Expressions.Expression{System.Func{T,bool}})" /> method returns is typically used in the predicate of a `where` clause (`Where` clause in Visual Basic) or a direct call to the <see cref="O:System.Linq.Queryable.Where" /> method. The following example demonstrates this use of the `All` method.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Queryable/CS/queryable.cs" id="Snippet134":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Queryable/VB/queryable.vb" id="Snippet134":::</example>
        [DynamicDependency("All`1", typeof(Enumerable))]
        public static bool All<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (predicate == null)
                throw Error.ArgumentNull(nameof(predicate));
            return source.Provider.Execute<bool>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.All_TSource_2(typeof(TSource)),
                    source.Expression, Expression.Quote(predicate)
                    ));
        }

        /// <summary>Returns the number of elements in a sequence.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">The <see cref="System.Linq.IQueryable{T}" /> that contains the elements to be counted.</param>
        /// <returns>The number of elements in the input sequence.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <exception cref="System.OverflowException">The number of elements in <paramref name="source" /> is larger than <see cref="int.MaxValue" />.</exception>
        /// <remarks>
        /// <para>The <see cref="System.Linq.Queryable.Count{T}(System.Linq.IQueryable{T})" /> method generates a <see cref="System.Linq.Expressions.MethodCallExpression" /> that represents calling <see cref="System.Linq.Queryable.Count{T}(System.Linq.IQueryable{T})" /> itself as a constructed generic method. It then passes the <see cref="System.Linq.Expressions.MethodCallExpression" /> to the <see cref="System.Linq.IQueryProvider.Execute{T}(System.Linq.Expressions.Expression)" /> method of the <see cref="System.Linq.IQueryProvider" /> represented by the <see cref="O:System.Linq.IQueryable.Provider" /> property of the <paramref name="source" /> parameter.</para>
        /// <para>The query behavior that occurs as a result of executing an expression tree that represents calling <see cref="System.Linq.Queryable.Count{T}(System.Linq.IQueryable{T})" /> depends on the implementation of the type of the <paramref name="source" /> parameter. The expected behavior is that it counts the number of items in <paramref name="source" />.</para>
        /// </remarks>
        /// <example>The following code example demonstrates how to use <see cref="System.Linq.Queryable.Count{T}(System.Linq.IQueryable{T})" /> to count the elements in a sequence.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Queryable/CS/queryable.cs" interactive="try-dotnet-method" id="Snippet22":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Queryable/VB/queryable.vb" id="Snippet22":::</example>
        [DynamicDependency("Count`1", typeof(Enumerable))]
        public static int Count<TSource>(this IQueryable<TSource> source)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            return source.Provider.Execute<int>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.Count_TSource_1(typeof(TSource)), source.Expression));
        }

        /// <summary>Returns the number of elements in the specified sequence that satisfies a condition.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">An <see cref="System.Linq.IQueryable{T}" /> that contains the elements to be counted.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <returns>The number of elements in the sequence that satisfies the condition in the predicate function.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> or <paramref name="predicate" /> is <see langword="null" />.</exception>
        /// <exception cref="System.OverflowException">The number of elements in <paramref name="source" /> is larger than <see cref="int.MaxValue" />.</exception>
        /// <remarks>
        /// <para>This method has at least one parameter of type <see cref="System.Linq.Expressions.Expression{T}" /> whose type argument is one of the <see cref="System.Func{T1,T2}" /> types. For these parameters, you can pass in a lambda expression and it will be compiled to an <see cref="System.Linq.Expressions.Expression{T}" />.</para>
        /// <para>The <see cref="System.Linq.Queryable.Count{T}(System.Linq.IQueryable{T},System.Linq.Expressions.Expression{System.Func{T,bool}})" /> method generates a <see cref="System.Linq.Expressions.MethodCallExpression" /> that represents calling <see cref="System.Linq.Queryable.Count{T}(System.Linq.IQueryable{T},System.Linq.Expressions.Expression{System.Func{T,bool}})" /> itself as a constructed generic method. It then passes the <see cref="System.Linq.Expressions.MethodCallExpression" /> to the <see cref="System.Linq.IQueryProvider.Execute{T}(System.Linq.Expressions.Expression)" /> method of the <see cref="System.Linq.IQueryProvider" /> represented by the <see cref="O:System.Linq.IQueryable.Provider" /> property of the <paramref name="source" /> parameter.</para>
        /// <para>The query behavior that occurs as a result of executing an expression tree that represents calling <see cref="System.Linq.Queryable.Count{T}(System.Linq.IQueryable{T},System.Linq.Expressions.Expression{System.Func{T,bool}})" /> depends on the implementation of the type of the <paramref name="source" /> parameter. The expected behavior is that it counts the number of items in <paramref name="source" /> that satisfy the condition specified by <paramref name="predicate" />.</para>
        /// </remarks>
        /// <example>The following code example demonstrates how to use <see cref="System.Linq.Queryable.Count{T}(System.Linq.IQueryable{T},System.Linq.Expressions.Expression{System.Func{T,bool}})" /> to count the elements in a sequence that satisfy a condition.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Queryable/CS/queryable.cs" id="Snippet23":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Queryable/VB/queryable.vb" id="Snippet23":::</example>
        [DynamicDependency("Count`1", typeof(Enumerable))]
        public static int Count<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (predicate == null)
                throw Error.ArgumentNull(nameof(predicate));
            return source.Provider.Execute<int>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.Count_TSource_2(typeof(TSource)),
                    source.Expression, Expression.Quote(predicate)
                    ));
        }

        /// <summary>Returns an <see cref="long" /> that represents the total number of elements in a sequence.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">An <see cref="System.Linq.IQueryable{T}" /> that contains the elements to be counted.</param>
        /// <returns>The number of elements in <paramref name="source" />.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <exception cref="System.OverflowException">The number of elements exceeds <see cref="long.MaxValue" />.</exception>
        /// <remarks>
        /// <para>The <see cref="System.Linq.Queryable.LongCount{T}(System.Linq.IQueryable{T})" /> method generates a <see cref="System.Linq.Expressions.MethodCallExpression" /> that represents calling <see cref="System.Linq.Queryable.LongCount{T}(System.Linq.IQueryable{T})" /> itself as a constructed generic method. It then passes the <see cref="System.Linq.Expressions.MethodCallExpression" /> to the <see cref="System.Linq.IQueryProvider.Execute{T}(System.Linq.Expressions.Expression)" /> method of the <see cref="System.Linq.IQueryProvider" /> represented by the <see cref="O:System.Linq.IQueryable.Provider" /> property of the <paramref name="source" /> parameter.</para>
        /// <para>The query behavior that occurs as a result of executing an expression tree that represents calling <see cref="System.Linq.Queryable.LongCount{T}(System.Linq.IQueryable{T})" /> depends on the implementation of the type of the <paramref name="source" /> parameter. The expected behavior is that it counts the number of items in <paramref name="source" /> and returns an <see cref="long" />.</para>
        /// </remarks>
        /// <example>The following code example demonstrates how to use <see cref="System.Linq.Queryable.LongCount{T}(System.Linq.IQueryable{T})" /> to count the elements in an array.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Queryable/CS/queryable.cs" interactive="try-dotnet-method" id="Snippet47":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Queryable/VB/queryable.vb" id="Snippet47":::</example>
        [DynamicDependency("LongCount`1", typeof(Enumerable))]
        public static long LongCount<TSource>(this IQueryable<TSource> source)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            return source.Provider.Execute<long>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.LongCount_TSource_1(typeof(TSource)), source.Expression));
        }

        /// <summary>Returns an <see cref="long" /> that represents the number of elements in a sequence that satisfy a condition.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">An <see cref="System.Linq.IQueryable{T}" /> that contains the elements to be counted.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <returns>The number of elements in <paramref name="source" /> that satisfy the condition in the predicate function.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> or <paramref name="predicate" /> is <see langword="null" />.</exception>
        /// <exception cref="System.OverflowException">The number of matching elements exceeds <see cref="long.MaxValue" />.</exception>
        /// <remarks>
        /// <para>This method has at least one parameter of type <see cref="System.Linq.Expressions.Expression{T}" /> whose type argument is one of the <see cref="System.Func{T1,T2}" /> types. For these parameters, you can pass in a lambda expression and it will be compiled to an <see cref="System.Linq.Expressions.Expression{T}" />.</para>
        /// <para>The <see cref="System.Linq.Queryable.LongCount{T}(System.Linq.IQueryable{T},System.Linq.Expressions.Expression{System.Func{T,bool}})" /> method generates a <see cref="System.Linq.Expressions.MethodCallExpression" /> that represents calling <see cref="System.Linq.Queryable.LongCount{T}(System.Linq.IQueryable{T},System.Linq.Expressions.Expression{System.Func{T,bool}})" /> itself as a constructed generic method. It then passes the <see cref="System.Linq.Expressions.MethodCallExpression" /> to the <see cref="System.Linq.IQueryProvider.Execute{T}(System.Linq.Expressions.Expression)" /> method of the <see cref="System.Linq.IQueryProvider" /> represented by the <see cref="O:System.Linq.IQueryable.Provider" /> property of the <paramref name="source" /> parameter.</para>
        /// <para>The query behavior that occurs as a result of executing an expression tree that represents calling <see cref="System.Linq.Queryable.LongCount{T}(System.Linq.IQueryable{T},System.Linq.Expressions.Expression{System.Func{T,bool}})" /> depends on the implementation of the type of the <paramref name="source" /> parameter. The expected behavior is that it counts the number of items in <paramref name="source" /> that satisfy the condition specified by <paramref name="predicate" /> and returns an <see cref="long" />.</para>
        /// </remarks>
        /// <example>The following code example demonstrates how to use <see cref="System.Linq.Queryable.LongCount{T}(System.Linq.IQueryable{T},System.Linq.Expressions.Expression{System.Func{T,bool}})" /> to count the elements in an array that satisfy a condition.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Queryable/CS/queryable.cs" id="Snippet48":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Queryable/VB/queryable.vb" id="Snippet48":::</example>
        [DynamicDependency("LongCount`1", typeof(Enumerable))]
        public static long LongCount<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (predicate == null)
                throw Error.ArgumentNull(nameof(predicate));
            return source.Provider.Execute<long>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.LongCount_TSource_2(typeof(TSource)),
                    source.Expression, Expression.Quote(predicate)
                    ));
        }

        /// <summary>Returns the minimum value of a generic <see cref="System.Linq.IQueryable{T}" />.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence of values to determine the minimum of.</param>
        /// <returns>The minimum value in the sequence.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <exception cref="System.InvalidOperationException"><paramref name="source" /> contains no elements.</exception>
        /// <remarks>
        /// <para>The <see cref="System.Linq.Queryable.Min{T}(System.Linq.IQueryable{T})" /> method generates a <see cref="System.Linq.Expressions.MethodCallExpression" /> that represents calling <see cref="System.Linq.Queryable.Min{T}(System.Linq.IQueryable{T})" /> itself as a constructed generic method. It then passes the <see cref="System.Linq.Expressions.MethodCallExpression" /> to the <see cref="System.Linq.IQueryProvider.Execute{T}(System.Linq.Expressions.Expression)" /> method of the <see cref="System.Linq.IQueryProvider" /> represented by the <see cref="O:System.Linq.IQueryable.Provider" /> property of the <paramref name="source" /> parameter.</para>
        /// <para>The query behavior that occurs as a result of executing an expression tree that represents calling <see cref="System.Linq.Queryable.Min{T}(System.Linq.IQueryable{T})" /> depends on the implementation of the type of the <paramref name="source" /> parameter. The expected behavior is that it returns the minimum value in <paramref name="source" />.</para>
        /// </remarks>
        /// <example>The following code example demonstrates how to use <see cref="System.Linq.Queryable.Min{T}(System.Linq.IQueryable{T})" /> to determine the minimum value in a sequence.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Queryable/CS/queryable.cs" interactive="try-dotnet-method" id="Snippet60":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Queryable/VB/queryable.vb" id="Snippet60":::</example>
        [DynamicDependency("Min`1", typeof(Enumerable))]
        public static TSource? Min<TSource>(this IQueryable<TSource> source)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            return source.Provider.Execute<TSource>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.Min_TSource_1(typeof(TSource)), source.Expression));
        }

        /// <summary>Returns the minimum value in a generic <see cref="System.Linq.IQueryable{T}" />.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence of values to determine the minimum value of.</param>
        /// <param name="comparer">The <see cref="IComparer{T}" /> to compare values.</param>
        /// <returns>The minimum value in the sequence.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentException">No object in <paramref name="source" /> implements the <see cref="System.IComparable" /> or <see cref="System.IComparable{T}" /> interface.</exception>
        [DynamicDependency("Min`1", typeof(Enumerable))]
        public static TSource? Min<TSource>(this IQueryable<TSource> source, IComparer<TSource>? comparer)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            return source.Provider.Execute<TSource>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.Min_TSource_2(typeof(TSource)),
                    source.Expression,
                    Expression.Constant(comparer, typeof(IComparer<TSource>))
                    ));
        }

        /// <summary>Invokes a projection function on each element of a generic <see cref="System.Linq.IQueryable{T}" /> and returns the minimum resulting value.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <typeparam name="TResult">The type of the value returned by the function represented by <paramref name="selector" />.</typeparam>
        /// <param name="source">A sequence of values to determine the minimum of.</param>
        /// <param name="selector">A projection function to apply to each element.</param>
        /// <returns>The minimum value in the sequence.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> or <paramref name="selector" /> is <see langword="null" />.</exception>
        /// <exception cref="System.InvalidOperationException"><paramref name="source" /> contains no elements.</exception>
        /// <remarks>
        /// <para>This method has at least one parameter of type <see cref="System.Linq.Expressions.Expression{T}" /> whose type argument is one of the <see cref="System.Func{T1,T2}" /> types. For these parameters, you can pass in a lambda expression and it will be compiled to an <see cref="System.Linq.Expressions.Expression{T}" />.</para>
        /// <para>The <see cref="System.Linq.Queryable.Min{T1,T2}(System.Linq.IQueryable{T1},System.Linq.Expressions.Expression{System.Func{T1,T2}})" /> method generates a <see cref="System.Linq.Expressions.MethodCallExpression" /> that represents calling <see cref="System.Linq.Queryable.Min{T1,T2}(System.Linq.IQueryable{T1},System.Linq.Expressions.Expression{System.Func{T1,T2}})" /> itself as a constructed generic method. It then passes the <see cref="System.Linq.Expressions.MethodCallExpression" /> to the <see cref="System.Linq.IQueryProvider.Execute{T}(System.Linq.Expressions.Expression)" /> method of the <see cref="System.Linq.IQueryProvider" /> represented by the <see cref="O:System.Linq.IQueryable.Provider" /> property of the <paramref name="source" /> parameter.</para>
        /// <para>The query behavior that occurs as a result of executing an expression tree that represents calling <see cref="System.Linq.Queryable.Min{T1,T2}(System.Linq.IQueryable{T1},System.Linq.Expressions.Expression{System.Func{T1,T2}})" /> depends on the implementation of the type of the <paramref name="source" /> parameter. The expected behavior is that it invokes <paramref name="selector" /> on each element in <paramref name="source" /> and returns the minimum value.</para>
        /// </remarks>
        /// <example>The following code example demonstrates how to use <see cref="System.Linq.Queryable.Min{T1,T2}(System.Linq.IQueryable{T1},System.Linq.Expressions.Expression{System.Func{T1,T2}})" /> to determine the minimum value in a sequence of projected values.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Queryable/CS/queryable.cs" id="Snippet68":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Queryable/VB/queryable.vb" id="Snippet68":::</example>
        [DynamicDependency("Min`2", typeof(Enumerable))]
        public static TResult? Min<TSource, TResult>(this IQueryable<TSource> source, Expression<Func<TSource, TResult>> selector)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (selector == null)
                throw Error.ArgumentNull(nameof(selector));
            return source.Provider.Execute<TResult>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.Min_TSource_TResult_2(typeof(TSource), typeof(TResult)),
                    source.Expression, Expression.Quote(selector)
                    ));
        }

        /// <summary>Returns the minimum value in a generic <see cref="IQueryable{T}"/> according to a specified key selector function.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <typeparam name="TKey">The type of key to compare elements by.</typeparam>
        /// <param name="source">A sequence of values to determine the minimum value of.</param>
        /// <param name="keySelector">A function to extract the key for each element.</param>
        /// <returns>The value with the minimum key in the sequence.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentException">No key extracted from <paramref name="source" /> implements the <see cref="IComparable" /> or <see cref="IComparable{TKey}" /> interface.</exception>
        [DynamicDependency("MinBy`2", typeof(Enumerable))]
        public static TSource? MinBy<TSource, TKey>(this IQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (keySelector == null)
                throw Error.ArgumentNull(nameof(keySelector));
            return source.Provider.Execute<TSource>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.MinBy_TSource_TKey_2(typeof(TSource), typeof(TKey)),
                    source.Expression,
                    Expression.Quote(keySelector)
                    ));
        }

        /// <summary>Returns the minimum value in a generic <see cref="IQueryable{T}"/> according to a specified key selector function.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <typeparam name="TKey">The type of key to compare elements by.</typeparam>
        /// <param name="source">A sequence of values to determine the minimum value of.</param>
        /// <param name="keySelector">A function to extract the key for each element.</param>
        /// <param name="comparer">The <see cref="IComparer{TKey}" /> to compare keys.</param>
        /// <returns>The value with the minimum key in the sequence.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentException">No key extracted from <paramref name="source" /> implements the <see cref="IComparable" /> or <see cref="IComparable{TKey}" /> interface.</exception>
        [DynamicDependency("MinBy`2", typeof(Enumerable))]
        public static TSource? MinBy<TSource, TKey>(this IQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector, IComparer<TSource>? comparer)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (keySelector == null)
                throw Error.ArgumentNull(nameof(keySelector));
            return source.Provider.Execute<TSource>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.MinBy_TSource_TKey_3(typeof(TSource), typeof(TKey)),
                    source.Expression,
                    Expression.Quote(keySelector),
                    Expression.Constant(comparer, typeof(IComparer<TSource>))
                    ));
        }

        /// <summary>Returns the maximum value in a generic <see cref="System.Linq.IQueryable{T}" />.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence of values to determine the maximum of.</param>
        /// <returns>The maximum value in the sequence.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <exception cref="System.InvalidOperationException"><paramref name="source" /> contains no elements.</exception>
        /// <remarks>
        /// <para>The <see cref="System.Linq.Queryable.Max{T}(System.Linq.IQueryable{T})" /> method generates a <see cref="System.Linq.Expressions.MethodCallExpression" /> that represents calling <see cref="System.Linq.Queryable.Max{T}(System.Linq.IQueryable{T})" /> itself as a constructed generic method. It then passes the <see cref="System.Linq.Expressions.MethodCallExpression" /> to the <see cref="System.Linq.IQueryProvider.Execute{T}(System.Linq.Expressions.Expression)" /> method of the <see cref="System.Linq.IQueryProvider" /> represented by the <see cref="O:System.Linq.IQueryable.Provider" /> property of the <paramref name="source" /> parameter.</para>
        /// <para>The query behavior that occurs as a result of executing an expression tree that represents calling <see cref="System.Linq.Queryable.Max{T}(System.Linq.IQueryable{T})" /> depends on the implementation of the type of the <paramref name="source" /> parameter. The expected behavior is that it returns the maximum value in <paramref name="source" />.</para>
        /// </remarks>
        /// <example>The following code example demonstrates how to use <see cref="System.Linq.Queryable.Max{T}(System.Linq.IQueryable{T})" /> to determine the maximum value in a sequence.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Queryable/CS/queryable.cs" interactive="try-dotnet-method" id="Snippet52":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Queryable/VB/queryable.vb" id="Snippet52":::</example>
        [DynamicDependency("Max`1", typeof(Enumerable))]
        public static TSource? Max<TSource>(this IQueryable<TSource> source)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            return source.Provider.Execute<TSource>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.Max_TSource_1(typeof(TSource)), source.Expression));
        }
        /// <summary>Returns the maximum value in a generic <see cref="System.Linq.IQueryable{T}" />.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence of values to determine the maximum value of.</param>
        /// <param name="comparer">The <see cref="IComparer{T}" /> to compare values.</param>
        /// <returns>The maximum value in the sequence.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        [DynamicDependency("Max`1", typeof(Enumerable))]
        public static TSource? Max<TSource>(this IQueryable<TSource> source, IComparer<TSource>? comparer)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            return source.Provider.Execute<TSource>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.Max_TSource_2(typeof(TSource)),
                    source.Expression,
                    Expression.Constant(comparer, typeof(IComparer<TSource>))
                    ));
        }

        /// <summary>Invokes a projection function on each element of a generic <see cref="System.Linq.IQueryable{T}" /> and returns the maximum resulting value.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <typeparam name="TResult">The type of the value returned by the function represented by <paramref name="selector" />.</typeparam>
        /// <param name="source">A sequence of values to determine the maximum of.</param>
        /// <param name="selector">A projection function to apply to each element.</param>
        /// <returns>The maximum value in the sequence.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> or <paramref name="selector" /> is <see langword="null" />.</exception>
        /// <exception cref="System.InvalidOperationException"><paramref name="source" /> contains no elements.</exception>
        /// <remarks>
        /// <para>This method has at least one parameter of type <see cref="System.Linq.Expressions.Expression{T}" /> whose type argument is one of the <see cref="System.Func{T1,T2}" /> types. For these parameters, you can pass in a lambda expression and it will be compiled to an <see cref="System.Linq.Expressions.Expression{T}" />.</para>
        /// <para>The <see cref="System.Linq.Queryable.Max{T1,T2}(System.Linq.IQueryable{T1},System.Linq.Expressions.Expression{System.Func{T1,T2}})" /> method generates a <see cref="System.Linq.Expressions.MethodCallExpression" /> that represents calling <see cref="System.Linq.Queryable.Max{T1,T2}(System.Linq.IQueryable{T1},System.Linq.Expressions.Expression{System.Func{T1,T2}})" /> itself as a constructed generic method. It then passes the <see cref="System.Linq.Expressions.MethodCallExpression" /> to the <see cref="System.Linq.IQueryProvider.Execute{T}(System.Linq.Expressions.Expression)" /> method of the <see cref="System.Linq.IQueryProvider" /> represented by the <see cref="O:System.Linq.IQueryable.Provider" /> property of the <paramref name="source" /> parameter.</para>
        /// <para>The query behavior that occurs as a result of executing an expression tree that represents calling <see cref="System.Linq.Queryable.Max{T1,T2}(System.Linq.IQueryable{T1},System.Linq.Expressions.Expression{System.Func{T1,T2}})" /> depends on the implementation of the type of the <paramref name="source" /> parameter. The expected behavior is that it invokes <paramref name="selector" /> on each element in <paramref name="source" /> and returns the maximum value.</para>
        /// </remarks>
        /// <example>The following code example demonstrates how to use <see cref="System.Linq.Queryable.Max{T1,T2}(System.Linq.IQueryable{T1},System.Linq.Expressions.Expression{System.Func{T1,T2}})" /> to determine the maximum value in a sequence of projected values.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Queryable/CS/queryable.cs" id="Snippet58":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Queryable/VB/queryable.vb" id="Snippet58":::</example>
        [DynamicDependency("Max`2", typeof(Enumerable))]
        public static TResult? Max<TSource, TResult>(this IQueryable<TSource> source, Expression<Func<TSource, TResult>> selector)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (selector == null)
                throw Error.ArgumentNull(nameof(selector));
            return source.Provider.Execute<TResult>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.Max_TSource_TResult_2(typeof(TSource), typeof(TResult)),
                    source.Expression, Expression.Quote(selector)
                    ));
        }

        /// <summary>Returns the maximum value in a generic <see cref="IQueryable{T}"/> according to a specified key selector function.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <typeparam name="TKey">The type of key to compare elements by.</typeparam>
        /// <param name="source">A sequence of values to determine the maximum value of.</param>
        /// <param name="keySelector">A function to extract the key for each element.</param>
        /// <returns>The value with the maximum key in the sequence.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentException">No key extracted from <paramref name="source" /> implements the <see cref="IComparable" /> or <see cref="IComparable{TKey}" /> interface.</exception>
        [DynamicDependency("MaxBy`2", typeof(Enumerable))]
        public static TSource? MaxBy<TSource, TKey>(this IQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (keySelector == null)
                throw Error.ArgumentNull(nameof(keySelector));
            return source.Provider.Execute<TSource>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.MaxBy_TSource_TKey_2(typeof(TSource), typeof(TKey)),
                    source.Expression,
                    Expression.Quote(keySelector)
                    ));
        }

        /// <summary>Returns the maximum value in a generic <see cref="IQueryable{T}"/> according to a specified key selector function.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <typeparam name="TKey">The type of key to compare elements by.</typeparam>
        /// <param name="source">A sequence of values to determine the maximum value of.</param>
        /// <param name="keySelector">A function to extract the key for each element.</param>
        /// <param name="comparer">The <see cref="IComparer{TKey}" /> to compare keys.</param>
        /// <returns>The value with the maximum key in the sequence.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentException">No key extracted from <paramref name="source" /> implements the <see cref="IComparable" /> or <see cref="IComparable{TKey}" /> interface.</exception>
        [DynamicDependency("MaxBy`2", typeof(Enumerable))]
        public static TSource? MaxBy<TSource, TKey>(this IQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector, IComparer<TSource>? comparer)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (keySelector == null)
                throw Error.ArgumentNull(nameof(keySelector));
            return source.Provider.Execute<TSource>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.MaxBy_TSource_TKey_3(typeof(TSource), typeof(TKey)),
                    source.Expression,
                    Expression.Quote(keySelector),
                    Expression.Constant(comparer, typeof(IComparer<TSource>))
                    ));
        }

        /// <summary>Computes the sum of a sequence of <see cref="int" /> values.</summary>
        /// <param name="source">A sequence of <see cref="int" /> values to calculate the sum of.</param>
        /// <returns>The sum of the values in the sequence.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <exception cref="System.OverflowException">The sum is larger than <see cref="int.MaxValue" />.</exception>
        /// <remarks>
        /// <para>The <see cref="System.Linq.Queryable.Sum(System.Linq.IQueryable{int})" /> method generates a <see cref="System.Linq.Expressions.MethodCallExpression" /> that represents calling <see cref="System.Linq.Queryable.Sum(System.Linq.IQueryable{int})" /> itself as a constructed generic method. It then passes the <see cref="System.Linq.Expressions.MethodCallExpression" /> to the <see cref="System.Linq.IQueryProvider.Execute{T}(System.Linq.Expressions.Expression)" /> method of the <see cref="System.Linq.IQueryProvider" /> represented by the <see cref="O:System.Linq.IQueryable.Provider" /> property of the <paramref name="source" /> parameter.</para>
        /// <para>The query behavior that occurs as a result of executing an expression tree that represents calling <see cref="System.Linq.Queryable.Sum(System.Linq.IQueryable{int})" /> depends on the implementation of the type of the <paramref name="source" /> parameter. The expected behavior is that it returns the sum of the values in <paramref name="source" />.</para>
        /// </remarks>
        [DynamicDependency("Sum", typeof(Enumerable))]
        public static int Sum(this IQueryable<int> source)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            return source.Provider.Execute<int>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.Sum_Int32_1, source.Expression));
        }

        /// <summary>Computes the sum of a sequence of nullable <see cref="int" /> values.</summary>
        /// <param name="source">A sequence of nullable <see cref="int" /> values to calculate the sum of.</param>
        /// <returns>The sum of the values in the sequence.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <exception cref="System.OverflowException">The sum is larger than <see cref="int.MaxValue" />.</exception>
        /// <remarks>
        /// <para>The <see cref="System.Linq.Queryable.Sum(System.Linq.IQueryable{System.Nullable{int}})" /> method generates a <see cref="System.Linq.Expressions.MethodCallExpression" /> that represents calling <see cref="System.Linq.Queryable.Sum(System.Linq.IQueryable{System.Nullable{int}})" /> itself as a constructed generic method. It then passes the <see cref="System.Linq.Expressions.MethodCallExpression" /> to the <see cref="System.Linq.IQueryProvider.Execute{T}(System.Linq.Expressions.Expression)" /> method of the <see cref="System.Linq.IQueryProvider" /> represented by the <see cref="O:System.Linq.IQueryable.Provider" /> property of the <paramref name="source" /> parameter.</para>
        /// <para>The query behavior that occurs as a result of executing an expression tree that represents calling <see cref="System.Linq.Queryable.Sum(System.Linq.IQueryable{System.Nullable{int}})" /> depends on the implementation of the type of the <paramref name="source" /> parameter. The expected behavior is that it returns the sum of the values in <paramref name="source" />.</para>
        /// </remarks>
        [DynamicDependency("Sum", typeof(Enumerable))]
        public static int? Sum(this IQueryable<int?> source)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            return source.Provider.Execute<int?>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.Sum_NullableInt32_1, source.Expression));
        }

        /// <summary>Computes the sum of a sequence of <see cref="long" /> values.</summary>
        /// <param name="source">A sequence of <see cref="long" /> values to calculate the sum of.</param>
        /// <returns>The sum of the values in the sequence.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <exception cref="System.OverflowException">The sum is larger than <see cref="long.MaxValue" />.</exception>
        /// <remarks>
        /// <para>The <see cref="System.Linq.Queryable.Sum(System.Linq.IQueryable{long})" /> method generates a <see cref="System.Linq.Expressions.MethodCallExpression" /> that represents calling <see cref="System.Linq.Queryable.Sum(System.Linq.IQueryable{long})" /> itself as a constructed generic method. It then passes the <see cref="System.Linq.Expressions.MethodCallExpression" /> to the <see cref="System.Linq.IQueryProvider.Execute{T}(System.Linq.Expressions.Expression)" /> method of the <see cref="System.Linq.IQueryProvider" /> represented by the <see cref="O:System.Linq.IQueryable.Provider" /> property of the <paramref name="source" /> parameter.</para>
        /// <para>The query behavior that occurs as a result of executing an expression tree that represents calling <see cref="System.Linq.Queryable.Sum(System.Linq.IQueryable{long})" /> depends on the implementation of the type of the <paramref name="source" /> parameter. The expected behavior is that it returns the sum of the values in <paramref name="source" />.</para>
        /// </remarks>
        [DynamicDependency("Sum", typeof(Enumerable))]
        public static long Sum(this IQueryable<long> source)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            return source.Provider.Execute<long>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.Sum_Int64_1, source.Expression));
        }

        /// <summary>Computes the sum of a sequence of nullable <see cref="long" /> values.</summary>
        /// <param name="source">A sequence of nullable <see cref="long" /> values to calculate the sum of.</param>
        /// <returns>The sum of the values in the sequence.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <exception cref="System.OverflowException">The sum is larger than <see cref="long.MaxValue" />.</exception>
        /// <remarks>
        /// <para>The <see cref="System.Linq.Queryable.Sum(System.Linq.IQueryable{System.Nullable{long}})" /> method generates a <see cref="System.Linq.Expressions.MethodCallExpression" /> that represents calling <see cref="System.Linq.Queryable.Sum(System.Linq.IQueryable{System.Nullable{long}})" /> itself as a constructed generic method. It then passes the <see cref="System.Linq.Expressions.MethodCallExpression" /> to the <see cref="System.Linq.IQueryProvider.Execute{T}(System.Linq.Expressions.Expression)" /> method of the <see cref="System.Linq.IQueryProvider" /> represented by the <see cref="O:System.Linq.IQueryable.Provider" /> property of the <paramref name="source" /> parameter.</para>
        /// <para>The query behavior that occurs as a result of executing an expression tree that represents calling <see cref="System.Linq.Queryable.Sum(System.Linq.IQueryable{System.Nullable{long}})" /> depends on the implementation of the type of the <paramref name="source" /> parameter. The expected behavior is that it returns the sum of the values in <paramref name="source" />.</para>
        /// </remarks>
        [DynamicDependency("Sum", typeof(Enumerable))]
        public static long? Sum(this IQueryable<long?> source)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            return source.Provider.Execute<long?>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.Sum_NullableInt64_1, source.Expression));
        }

        /// <summary>Computes the sum of a sequence of <see cref="float" /> values.</summary>
        /// <param name="source">A sequence of <see cref="float" /> values to calculate the sum of.</param>
        /// <returns>The sum of the values in the sequence.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <remarks>
        /// <para>The <see cref="System.Linq.Queryable.Sum(System.Linq.IQueryable{float})" /> method generates a <see cref="System.Linq.Expressions.MethodCallExpression" /> that represents calling <see cref="System.Linq.Queryable.Sum(System.Linq.IQueryable{float})" /> itself as a constructed generic method. It then passes the <see cref="System.Linq.Expressions.MethodCallExpression" /> to the <see cref="System.Linq.IQueryProvider.Execute{T}(System.Linq.Expressions.Expression)" /> method of the <see cref="System.Linq.IQueryProvider" /> represented by the <see cref="O:System.Linq.IQueryable.Provider" /> property of the <paramref name="source" /> parameter.</para>
        /// <para>The query behavior that occurs as a result of executing an expression tree that represents calling <see cref="System.Linq.Queryable.Sum(System.Linq.IQueryable{float})" /> depends on the implementation of the type of the <paramref name="source" /> parameter. The expected behavior is that it returns the sum of the values in <paramref name="source" />.</para>
        /// </remarks>
        /// <example>The following code example demonstrates how to use <see cref="System.Linq.Queryable.Sum(System.Linq.IQueryable{float})" /> to sum the values of a sequence.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Queryable/CS/queryable.cs" interactive="try-dotnet-method" id="Snippet120":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Queryable/VB/queryable.vb" id="Snippet120":::</example>
        [DynamicDependency("Sum", typeof(Enumerable))]
        public static float Sum(this IQueryable<float> source)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            return source.Provider.Execute<float>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.Sum_Single_1, source.Expression));
        }

        /// <summary>Computes the sum of a sequence of nullable <see cref="float" /> values.</summary>
        /// <param name="source">A sequence of nullable <see cref="float" /> values to calculate the sum of.</param>
        /// <returns>The sum of the values in the sequence.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <remarks>
        /// <para>The <see cref="System.Linq.Queryable.Sum(System.Linq.IQueryable{System.Nullable{float}})" /> method generates a <see cref="System.Linq.Expressions.MethodCallExpression" /> that represents calling <see cref="System.Linq.Queryable.Sum(System.Linq.IQueryable{System.Nullable{float}})" /> itself as a constructed generic method. It then passes the <see cref="System.Linq.Expressions.MethodCallExpression" /> to the <see cref="System.Linq.IQueryProvider.Execute{T}(System.Linq.Expressions.Expression)" /> method of the <see cref="System.Linq.IQueryProvider" /> represented by the <see cref="O:System.Linq.IQueryable.Provider" /> property of the <paramref name="source" /> parameter.</para>
        /// <para>The query behavior that occurs as a result of executing an expression tree that represents calling <see cref="System.Linq.Queryable.Sum(System.Linq.IQueryable{System.Nullable{float}})" /> depends on the implementation of the type of the <paramref name="source" /> parameter. The expected behavior is that it returns the sum of the values in <paramref name="source" />.</para>
        /// </remarks>
        /// <example>The following code example demonstrates how to use <see cref="System.Linq.Queryable.Sum(System.Linq.IQueryable{System.Nullable{float}})" /> to sum the values of a sequence.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Queryable/CS/queryable.cs" interactive="try-dotnet-method" id="Snippet121":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Queryable/VB/queryable.vb" id="Snippet121":::</example>
        [DynamicDependency("Sum", typeof(Enumerable))]
        public static float? Sum(this IQueryable<float?> source)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            return source.Provider.Execute<float?>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.Sum_NullableSingle_1, source.Expression));
        }

        /// <summary>Computes the sum of a sequence of <see cref="double" /> values.</summary>
        /// <param name="source">A sequence of <see cref="double" /> values to calculate the sum of.</param>
        /// <returns>The sum of the values in the sequence.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <remarks>
        /// <para>The <see cref="System.Linq.Queryable.Sum(System.Linq.IQueryable{double})" /> method generates a <see cref="System.Linq.Expressions.MethodCallExpression" /> that represents calling <see cref="System.Linq.Queryable.Sum(System.Linq.IQueryable{double})" /> itself as a constructed generic method. It then passes the <see cref="System.Linq.Expressions.MethodCallExpression" /> to the <see cref="System.Linq.IQueryProvider.Execute{T}(System.Linq.Expressions.Expression)" /> method of the <see cref="System.Linq.IQueryProvider" /> represented by the <see cref="O:System.Linq.IQueryable.Provider" /> property of the <paramref name="source" /> parameter.</para>
        /// <para>The query behavior that occurs as a result of executing an expression tree that represents calling <see cref="System.Linq.Queryable.Sum(System.Linq.IQueryable{double})" /> depends on the implementation of the type of the <paramref name="source" /> parameter. The expected behavior is that it returns the sum of the values in <paramref name="source" />.</para>
        /// </remarks>
        [DynamicDependency("Sum", typeof(Enumerable))]
        public static double Sum(this IQueryable<double> source)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            return source.Provider.Execute<double>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.Sum_Double_1, source.Expression));
        }

        /// <summary>Computes the sum of a sequence of nullable <see cref="double" /> values.</summary>
        /// <param name="source">A sequence of nullable <see cref="double" /> values to calculate the sum of.</param>
        /// <returns>The sum of the values in the sequence.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <remarks>
        /// <para>The <see cref="System.Linq.Queryable.Sum(System.Linq.IQueryable{System.Nullable{double}})" /> method generates a <see cref="System.Linq.Expressions.MethodCallExpression" /> that represents calling <see cref="System.Linq.Queryable.Sum(System.Linq.IQueryable{System.Nullable{double}})" /> itself as a constructed generic method. It then passes the <see cref="System.Linq.Expressions.MethodCallExpression" /> to the <see cref="System.Linq.IQueryProvider.Execute{T}(System.Linq.Expressions.Expression)" /> method of the <see cref="System.Linq.IQueryProvider" /> represented by the <see cref="O:System.Linq.IQueryable.Provider" /> property of the <paramref name="source" /> parameter.</para>
        /// <para>The query behavior that occurs as a result of executing an expression tree that represents calling <see cref="System.Linq.Queryable.Sum(System.Linq.IQueryable{System.Nullable{double}})" /> depends on the implementation of the type of the <paramref name="source" /> parameter. The expected behavior is that it returns the sum of the values in <paramref name="source" />.</para>
        /// </remarks>
        [DynamicDependency("Sum", typeof(Enumerable))]
        public static double? Sum(this IQueryable<double?> source)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            return source.Provider.Execute<double?>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.Sum_NullableDouble_1, source.Expression));
        }

        /// <summary>Computes the sum of a sequence of <see cref="decimal" /> values.</summary>
        /// <param name="source">A sequence of <see cref="decimal" /> values to calculate the sum of.</param>
        /// <returns>The sum of the values in the sequence.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <exception cref="System.OverflowException">The sum is larger than <see cref="decimal.MaxValue" />.</exception>
        /// <remarks>
        /// <para>The <see cref="System.Linq.Queryable.Sum(System.Linq.IQueryable{decimal})" /> method generates a <see cref="System.Linq.Expressions.MethodCallExpression" /> that represents calling <see cref="System.Linq.Queryable.Sum(System.Linq.IQueryable{decimal})" /> itself as a constructed generic method. It then passes the <see cref="System.Linq.Expressions.MethodCallExpression" /> to the <see cref="System.Linq.IQueryProvider.Execute{T}(System.Linq.Expressions.Expression)" /> method of the <see cref="System.Linq.IQueryProvider" /> represented by the <see cref="O:System.Linq.IQueryable.Provider" /> property of the <paramref name="source" /> parameter.</para>
        /// <para>The query behavior that occurs as a result of executing an expression tree that represents calling <see cref="System.Linq.Queryable.Sum(System.Linq.IQueryable{decimal})" /> depends on the implementation of the type of the <paramref name="source" /> parameter. The expected behavior is that it returns the sum of the values in <paramref name="source" />.</para>
        /// </remarks>
        [DynamicDependency("Sum", typeof(Enumerable))]
        public static decimal Sum(this IQueryable<decimal> source)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            return source.Provider.Execute<decimal>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.Sum_Decimal_1, source.Expression));
        }

        /// <summary>Computes the sum of a sequence of nullable <see cref="decimal" /> values.</summary>
        /// <param name="source">A sequence of nullable <see cref="decimal" /> values to calculate the sum of.</param>
        /// <returns>The sum of the values in the sequence.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <exception cref="System.OverflowException">The sum is larger than <see cref="decimal.MaxValue" />.</exception>
        /// <remarks>
        /// <para>The <see cref="System.Linq.Queryable.Sum(System.Linq.IQueryable{System.Nullable{decimal}})" /> method generates a <see cref="System.Linq.Expressions.MethodCallExpression" /> that represents calling <see cref="System.Linq.Queryable.Sum(System.Linq.IQueryable{System.Nullable{decimal}})" /> itself as a constructed generic method. It then passes the <see cref="System.Linq.Expressions.MethodCallExpression" /> to the <see cref="System.Linq.IQueryProvider.Execute{T}(System.Linq.Expressions.Expression)" /> method of the <see cref="System.Linq.IQueryProvider" /> represented by the <see cref="O:System.Linq.IQueryable.Provider" /> property of the <paramref name="source" /> parameter.</para>
        /// <para>The query behavior that occurs as a result of executing an expression tree that represents calling <see cref="System.Linq.Queryable.Sum(System.Linq.IQueryable{System.Nullable{decimal}})" /> depends on the implementation of the type of the <paramref name="source" /> parameter. The expected behavior is that it returns the sum of the values in <paramref name="source" />.</para>
        /// </remarks>
        [DynamicDependency("Sum", typeof(Enumerable))]
        public static decimal? Sum(this IQueryable<decimal?> source)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            return source.Provider.Execute<decimal?>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.Sum_NullableDecimal_1, source.Expression));
        }

        /// <summary>Computes the sum of the sequence of <see cref="int" /> values that is obtained by invoking a projection function on each element of the input sequence.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence of values of type <typeparamref name="TSource" />.</param>
        /// <param name="selector">A projection function to apply to each element.</param>
        /// <returns>The sum of the projected values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> or <paramref name="selector" /> is <see langword="null" />.</exception>
        /// <exception cref="System.OverflowException">The sum is larger than <see cref="int.MaxValue" />.</exception>
        /// <remarks><format type="text/markdown"><![CDATA[
        /// This method has at least one parameter of type <xref:System.Linq.Expressions.Expression%601> whose type argument is one of the <xref:System.Func%602> types. For these parameters, you can pass in a lambda expression and it will be compiled to an <xref:System.Linq.Expressions.Expression%601>.
        /// The <xref:System.Linq.Queryable.Sum%60%601%28System.Linq.IQueryable%7B%60%600%7D%2CSystem.Linq.Expressions.Expression%7BSystem.Func%7B%60%600%2CSystem.Int32%7D%7D%29> method generates a <xref:System.Linq.Expressions.MethodCallExpression> that represents calling <xref:System.Linq.Queryable.Sum%60%601%28System.Linq.IQueryable%7B%60%600%7D%2CSystem.Linq.Expressions.Expression%7BSystem.Func%7B%60%600%2CSystem.Int32%7D%7D%29> itself as a constructed generic method. It then passes the <xref:System.Linq.Expressions.MethodCallExpression> to the <xref:System.Linq.IQueryProvider.Execute%60%601%28System.Linq.Expressions.Expression%29> method of the <xref:System.Linq.IQueryProvider> represented by the <xref:System.Linq.IQueryable.Provider%2A> property of the `source` parameter.
        /// The query behavior that occurs as a result of executing an expression tree that represents calling <xref:System.Linq.Queryable.Sum%60%601%28System.Linq.IQueryable%7B%60%600%7D%2CSystem.Linq.Expressions.Expression%7BSystem.Func%7B%60%600%2CSystem.Int32%7D%7D%29> depends on the implementation of the type of the `source` parameter. The expected behavior is that it invokes `selector` on each element of `source` and returns the sum of the resulting values.
        /// ## Examples
        /// The following code example demonstrates how to use <xref:System.Linq.Queryable.Sum%60%601%28System.Linq.IQueryable%7B%60%600%7D%2CSystem.Linq.Expressions.Expression%7BSystem.Func%7B%60%600%2CSystem.Double%7D%7D%29> to sum the projected values of a sequence.
        /// [!INCLUDE[sqo_diff_overload_example_func](~/includes/sqo-diff-overload-example-func-md.md)]
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Queryable/CS/queryable.cs" id="Snippet98":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Queryable/VB/queryable.vb" id="Snippet98":::
        /// ]]></format></remarks>
        [DynamicDependency("Sum`1", typeof(Enumerable))]
        public static int Sum<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, int>> selector)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (selector == null)
                throw Error.ArgumentNull(nameof(selector));
            return source.Provider.Execute<int>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.Sum_Int32_TSource_2(typeof(TSource)),
                    source.Expression, Expression.Quote(selector)
                    ));
        }

        /// <summary>Computes the sum of the sequence of nullable <see cref="int" /> values that is obtained by invoking a projection function on each element of the input sequence.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence of values of type <typeparamref name="TSource" />.</param>
        /// <param name="selector">A projection function to apply to each element.</param>
        /// <returns>The sum of the projected values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> or <paramref name="selector" /> is <see langword="null" />.</exception>
        /// <exception cref="System.OverflowException">The sum is larger than <see cref="int.MaxValue" />.</exception>
        /// <remarks><format type="text/markdown"><![CDATA[
        /// This method has at least one parameter of type <xref:System.Linq.Expressions.Expression%601> whose type argument is one of the <xref:System.Func%602> types. For these parameters, you can pass in a lambda expression and it will be compiled to an <xref:System.Linq.Expressions.Expression%601>.
        /// The <xref:System.Linq.Queryable.Sum%60%601%28System.Linq.IQueryable%7B%60%600%7D%2CSystem.Linq.Expressions.Expression%7BSystem.Func%7B%60%600%2CSystem.Nullable%7BSystem.Int32%7D%7D%7D%29> method generates a <xref:System.Linq.Expressions.MethodCallExpression> that represents calling <xref:System.Linq.Queryable.Sum%60%601%28System.Linq.IQueryable%7B%60%600%7D%2CSystem.Linq.Expressions.Expression%7BSystem.Func%7B%60%600%2CSystem.Nullable%7BSystem.Int32%7D%7D%7D%29> itself as a constructed generic method. It then passes the <xref:System.Linq.Expressions.MethodCallExpression> to the <xref:System.Linq.IQueryProvider.Execute%60%601%28System.Linq.Expressions.Expression%29> method of the <xref:System.Linq.IQueryProvider> represented by the <xref:System.Linq.IQueryable.Provider%2A> property of the `source` parameter.
        /// The query behavior that occurs as a result of executing an expression tree that represents calling <xref:System.Linq.Queryable.Sum%60%601%28System.Linq.IQueryable%7B%60%600%7D%2CSystem.Linq.Expressions.Expression%7BSystem.Func%7B%60%600%2CSystem.Nullable%7BSystem.Int32%7D%7D%7D%29> depends on the implementation of the type of the `source` parameter. The expected behavior is that it invokes `selector` on each element of `source` and returns the sum of the resulting values.
        /// ## Examples
        /// The following code example demonstrates how to use <xref:System.Linq.Queryable.Sum%60%601%28System.Linq.IQueryable%7B%60%600%7D%2CSystem.Linq.Expressions.Expression%7BSystem.Func%7B%60%600%2CSystem.Double%7D%7D%29> to sum the projected values of a sequence.
        /// [!INCLUDE[sqo_diff_overload_example_func](~/includes/sqo-diff-overload-example-func-md.md)]
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Queryable/CS/queryable.cs" id="Snippet98":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Queryable/VB/queryable.vb" id="Snippet98":::
        /// ]]></format></remarks>
        [DynamicDependency("Sum`1", typeof(Enumerable))]
        public static int? Sum<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, int?>> selector)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (selector == null)
                throw Error.ArgumentNull(nameof(selector));
            return source.Provider.Execute<int?>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.Sum_NullableInt32_TSource_2(typeof(TSource)),
                    source.Expression, Expression.Quote(selector)
                    ));
        }

        /// <summary>Computes the sum of the sequence of <see cref="long" /> values that is obtained by invoking a projection function on each element of the input sequence.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence of values of type <typeparamref name="TSource" />.</param>
        /// <param name="selector">A projection function to apply to each element.</param>
        /// <returns>The sum of the projected values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> or <paramref name="selector" /> is <see langword="null" />.</exception>
        /// <exception cref="System.OverflowException">The sum is larger than <see cref="long.MaxValue" />.</exception>
        /// <remarks><format type="text/markdown"><![CDATA[
        /// This method has at least one parameter of type <xref:System.Linq.Expressions.Expression%601> whose type argument is one of the <xref:System.Func%602> types. For these parameters, you can pass in a lambda expression and it will be compiled to an <xref:System.Linq.Expressions.Expression%601>.
        /// The <xref:System.Linq.Queryable.Sum%60%601%28System.Linq.IQueryable%7B%60%600%7D%2CSystem.Linq.Expressions.Expression%7BSystem.Func%7B%60%600%2CSystem.Int64%7D%7D%29> method generates a <xref:System.Linq.Expressions.MethodCallExpression> that represents calling <xref:System.Linq.Queryable.Sum%60%601%28System.Linq.IQueryable%7B%60%600%7D%2CSystem.Linq.Expressions.Expression%7BSystem.Func%7B%60%600%2CSystem.Int64%7D%7D%29> itself as a constructed generic method. It then passes the <xref:System.Linq.Expressions.MethodCallExpression> to the <xref:System.Linq.IQueryProvider.Execute%60%601%28System.Linq.Expressions.Expression%29> method of the <xref:System.Linq.IQueryProvider> represented by the <xref:System.Linq.IQueryable.Provider%2A> property of the `source` parameter.
        /// The query behavior that occurs as a result of executing an expression tree that represents calling <xref:System.Linq.Queryable.Sum%60%601%28System.Linq.IQueryable%7B%60%600%7D%2CSystem.Linq.Expressions.Expression%7BSystem.Func%7B%60%600%2CSystem.Int64%7D%7D%29> depends on the implementation of the type of the `source` parameter. The expected behavior is that it invokes `selector` on each element of `source` and returns the sum of the resulting values.
        /// ## Examples
        /// The following code example demonstrates how to use <xref:System.Linq.Queryable.Sum%60%601%28System.Linq.IQueryable%7B%60%600%7D%2CSystem.Linq.Expressions.Expression%7BSystem.Func%7B%60%600%2CSystem.Double%7D%7D%29> to sum the projected values of a sequence.
        /// [!INCLUDE[sqo_diff_overload_example_func](~/includes/sqo-diff-overload-example-func-md.md)]
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Queryable/CS/queryable.cs" id="Snippet98":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Queryable/VB/queryable.vb" id="Snippet98":::
        /// ]]></format></remarks>
        [DynamicDependency("Sum`1", typeof(Enumerable))]
        public static long Sum<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, long>> selector)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (selector == null)
                throw Error.ArgumentNull(nameof(selector));
            return source.Provider.Execute<long>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.Sum_Int64_TSource_2(typeof(TSource)),
                    source.Expression, Expression.Quote(selector)
                    ));
        }

        /// <summary>Computes the sum of the sequence of nullable <see cref="long" /> values that is obtained by invoking a projection function on each element of the input sequence.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence of values of type <typeparamref name="TSource" />.</param>
        /// <param name="selector">A projection function to apply to each element.</param>
        /// <returns>The sum of the projected values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> or <paramref name="selector" /> is <see langword="null" />.</exception>
        /// <exception cref="System.OverflowException">The sum is larger than <see cref="long.MaxValue" />.</exception>
        /// <remarks><format type="text/markdown"><![CDATA[
        /// This method has at least one parameter of type <xref:System.Linq.Expressions.Expression%601> whose type argument is one of the <xref:System.Func%602> types. For these parameters, you can pass in a lambda expression and it will be compiled to an <xref:System.Linq.Expressions.Expression%601>.
        /// The <xref:System.Linq.Queryable.Sum%60%601%28System.Linq.IQueryable%7B%60%600%7D%2CSystem.Linq.Expressions.Expression%7BSystem.Func%7B%60%600%2CSystem.Nullable%7BSystem.Int64%7D%7D%7D%29> method generates a <xref:System.Linq.Expressions.MethodCallExpression> that represents calling <xref:System.Linq.Queryable.Sum%60%601%28System.Linq.IQueryable%7B%60%600%7D%2CSystem.Linq.Expressions.Expression%7BSystem.Func%7B%60%600%2CSystem.Nullable%7BSystem.Int64%7D%7D%7D%29> itself as a constructed generic method. It then passes the <xref:System.Linq.Expressions.MethodCallExpression> to the <xref:System.Linq.IQueryProvider.Execute%60%601%28System.Linq.Expressions.Expression%29> method of the <xref:System.Linq.IQueryProvider> represented by the <xref:System.Linq.IQueryable.Provider%2A> property of the `source` parameter.
        /// The query behavior that occurs as a result of executing an expression tree that represents calling <xref:System.Linq.Queryable.Sum%60%601%28System.Linq.IQueryable%7B%60%600%7D%2CSystem.Linq.Expressions.Expression%7BSystem.Func%7B%60%600%2CSystem.Nullable%7BSystem.Int64%7D%7D%7D%29> depends on the implementation of the type of the `source` parameter. The expected behavior is that it invokes `selector` on each element of `source` and returns the sum of the resulting values.
        /// ## Examples
        /// The following code example demonstrates how to use <xref:System.Linq.Queryable.Sum%60%601%28System.Linq.IQueryable%7B%60%600%7D%2CSystem.Linq.Expressions.Expression%7BSystem.Func%7B%60%600%2CSystem.Double%7D%7D%29> to sum the projected values of a sequence.
        /// [!INCLUDE[sqo_diff_overload_example_func](~/includes/sqo-diff-overload-example-func-md.md)]
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Queryable/CS/queryable.cs" id="Snippet98":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Queryable/VB/queryable.vb" id="Snippet98":::
        /// ]]></format></remarks>
        [DynamicDependency("Sum`1", typeof(Enumerable))]
        public static long? Sum<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, long?>> selector)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (selector == null)
                throw Error.ArgumentNull(nameof(selector));
            return source.Provider.Execute<long?>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.Sum_NullableInt64_TSource_2(typeof(TSource)),
                    source.Expression, Expression.Quote(selector)
                    ));
        }

        /// <summary>Computes the sum of the sequence of <see cref="float" /> values that is obtained by invoking a projection function on each element of the input sequence.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence of values of type <typeparamref name="TSource" />.</param>
        /// <param name="selector">A projection function to apply to each element.</param>
        /// <returns>The sum of the projected values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> or <paramref name="selector" /> is <see langword="null" />.</exception>
        /// <remarks><format type="text/markdown"><![CDATA[
        /// This method has at least one parameter of type <xref:System.Linq.Expressions.Expression%601> whose type argument is one of the <xref:System.Func%602> types. For these parameters, you can pass in a lambda expression and it will be compiled to an <xref:System.Linq.Expressions.Expression%601>.
        /// The <xref:System.Linq.Queryable.Sum%60%601%28System.Linq.IQueryable%7B%60%600%7D%2CSystem.Linq.Expressions.Expression%7BSystem.Func%7B%60%600%2CSystem.Single%7D%7D%29> method generates a <xref:System.Linq.Expressions.MethodCallExpression> that represents calling <xref:System.Linq.Queryable.Sum%60%601%28System.Linq.IQueryable%7B%60%600%7D%2CSystem.Linq.Expressions.Expression%7BSystem.Func%7B%60%600%2CSystem.Single%7D%7D%29> itself as a constructed generic method. It then passes the <xref:System.Linq.Expressions.MethodCallExpression> to the <xref:System.Linq.IQueryProvider.Execute%60%601%28System.Linq.Expressions.Expression%29> method of the <xref:System.Linq.IQueryProvider> represented by the <xref:System.Linq.IQueryable.Provider%2A> property of the `source` parameter.
        /// The query behavior that occurs as a result of executing an expression tree that represents calling <xref:System.Linq.Queryable.Sum%60%601%28System.Linq.IQueryable%7B%60%600%7D%2CSystem.Linq.Expressions.Expression%7BSystem.Func%7B%60%600%2CSystem.Single%7D%7D%29> depends on the implementation of the type of the `source` parameter. The expected behavior is that it invokes `selector` on each element of `source` and returns the sum of the resulting values.
        /// ## Examples
        /// The following code example demonstrates how to use <xref:System.Linq.Queryable.Sum%60%601%28System.Linq.IQueryable%7B%60%600%7D%2CSystem.Linq.Expressions.Expression%7BSystem.Func%7B%60%600%2CSystem.Double%7D%7D%29> to sum the projected values of a sequence.
        /// [!INCLUDE[sqo_diff_overload_example_func](~/includes/sqo-diff-overload-example-func-md.md)]
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Queryable/CS/queryable.cs" id="Snippet98":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Queryable/VB/queryable.vb" id="Snippet98":::
        /// ]]></format></remarks>
        [DynamicDependency("Sum`1", typeof(Enumerable))]
        public static float Sum<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, float>> selector)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (selector == null)
                throw Error.ArgumentNull(nameof(selector));
            return source.Provider.Execute<float>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.Sum_Single_TSource_2(typeof(TSource)),
                    source.Expression, Expression.Quote(selector)
                    ));
        }

        /// <summary>Computes the sum of the sequence of nullable <see cref="float" /> values that is obtained by invoking a projection function on each element of the input sequence.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence of values of type <typeparamref name="TSource" />.</param>
        /// <param name="selector">A projection function to apply to each element.</param>
        /// <returns>The sum of the projected values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> or <paramref name="selector" /> is <see langword="null" />.</exception>
        /// <remarks><format type="text/markdown"><![CDATA[
        /// This method has at least one parameter of type <xref:System.Linq.Expressions.Expression%601> whose type argument is one of the <xref:System.Func%602> types. For these parameters, you can pass in a lambda expression and it will be compiled to an <xref:System.Linq.Expressions.Expression%601>.
        /// The <xref:System.Linq.Queryable.Sum%60%601%28System.Linq.IQueryable%7B%60%600%7D%2CSystem.Linq.Expressions.Expression%7BSystem.Func%7B%60%600%2CSystem.Nullable%7BSystem.Single%7D%7D%7D%29> method generates a <xref:System.Linq.Expressions.MethodCallExpression> that represents calling <xref:System.Linq.Queryable.Sum%60%601%28System.Linq.IQueryable%7B%60%600%7D%2CSystem.Linq.Expressions.Expression%7BSystem.Func%7B%60%600%2CSystem.Nullable%7BSystem.Single%7D%7D%7D%29> itself as a constructed generic method. It then passes the <xref:System.Linq.Expressions.MethodCallExpression> to the <xref:System.Linq.IQueryProvider.Execute%60%601%28System.Linq.Expressions.Expression%29> method of the <xref:System.Linq.IQueryProvider> represented by the <xref:System.Linq.IQueryable.Provider%2A> property of the `source` parameter.
        /// The query behavior that occurs as a result of executing an expression tree that represents calling <xref:System.Linq.Queryable.Sum%60%601%28System.Linq.IQueryable%7B%60%600%7D%2CSystem.Linq.Expressions.Expression%7BSystem.Func%7B%60%600%2CSystem.Nullable%7BSystem.Single%7D%7D%7D%29> depends on the implementation of the type of the `source` parameter. The expected behavior is that it invokes `selector` on each element of `source` and returns the sum of the resulting values.
        /// ## Examples
        /// The following code example demonstrates how to use <xref:System.Linq.Queryable.Sum%60%601%28System.Linq.IQueryable%7B%60%600%7D%2CSystem.Linq.Expressions.Expression%7BSystem.Func%7B%60%600%2CSystem.Double%7D%7D%29> to sum the projected values of a sequence.
        /// [!INCLUDE[sqo_diff_overload_example_func](~/includes/sqo-diff-overload-example-func-md.md)]
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Queryable/CS/queryable.cs" id="Snippet98":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Queryable/VB/queryable.vb" id="Snippet98":::
        /// ]]></format></remarks>
        [DynamicDependency("Sum`1", typeof(Enumerable))]
        public static float? Sum<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, float?>> selector)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (selector == null)
                throw Error.ArgumentNull(nameof(selector));
            return source.Provider.Execute<float?>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.Sum_NullableSingle_TSource_2(typeof(TSource)),
                    source.Expression, Expression.Quote(selector)
                    ));
        }

        /// <summary>Computes the sum of the sequence of <see cref="double" /> values that is obtained by invoking a projection function on each element of the input sequence.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence of values of type <typeparamref name="TSource" />.</param>
        /// <param name="selector">A projection function to apply to each element.</param>
        /// <returns>The sum of the projected values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> or <paramref name="selector" /> is <see langword="null" />.</exception>
        /// <remarks><format type="text/markdown"><![CDATA[
        /// This method has at least one parameter of type <xref:System.Linq.Expressions.Expression%601> whose type argument is one of the <xref:System.Func%602> types. For these parameters, you can pass in a lambda expression and it will be compiled to an <xref:System.Linq.Expressions.Expression%601>.
        /// The <xref:System.Linq.Queryable.Sum%60%601%28System.Linq.IQueryable%7B%60%600%7D%2CSystem.Linq.Expressions.Expression%7BSystem.Func%7B%60%600%2CSystem.Double%7D%7D%29> method generates a <xref:System.Linq.Expressions.MethodCallExpression> that represents calling <xref:System.Linq.Queryable.Sum%60%601%28System.Linq.IQueryable%7B%60%600%7D%2CSystem.Linq.Expressions.Expression%7BSystem.Func%7B%60%600%2CSystem.Double%7D%7D%29> itself as a constructed generic method. It then passes the <xref:System.Linq.Expressions.MethodCallExpression> to the <xref:System.Linq.IQueryProvider.Execute%60%601%28System.Linq.Expressions.Expression%29> method of the <xref:System.Linq.IQueryProvider> represented by the <xref:System.Linq.IQueryable.Provider%2A> property of the `source` parameter.
        /// The query behavior that occurs as a result of executing an expression tree that represents calling <xref:System.Linq.Queryable.Sum%60%601%28System.Linq.IQueryable%7B%60%600%7D%2CSystem.Linq.Expressions.Expression%7BSystem.Func%7B%60%600%2CSystem.Double%7D%7D%29> depends on the implementation of the type of he `source` parameter. The expected behavior is that it invokes `selector` on each element of `source` and returns the sum of the resulting values.
        /// ## Examples
        /// The following code example demonstrates how to use <xref:System.Linq.Queryable.Sum%60%601%28System.Linq.IQueryable%7B%60%600%7D%2CSystem.Linq.Expressions.Expression%7BSystem.Func%7B%60%600%2CSystem.Double%7D%7D%29> to sum the projected values of a sequence.
        /// [!INCLUDE[sqo_diff_overload_example_func](~/includes/sqo-diff-overload-example-func-md.md)]
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Queryable/CS/queryable.cs" id="Snippet98":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Queryable/VB/queryable.vb" id="Snippet98":::
        /// ]]></format></remarks>
        [DynamicDependency("Sum`1", typeof(Enumerable))]
        public static double Sum<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, double>> selector)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (selector == null)
                throw Error.ArgumentNull(nameof(selector));
            return source.Provider.Execute<double>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.Sum_Double_TSource_2(typeof(TSource)),
                    source.Expression, Expression.Quote(selector)
                    ));
        }

        /// <summary>Computes the sum of the sequence of nullable <see cref="double" /> values that is obtained by invoking a projection function on each element of the input sequence.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence of values of type <typeparamref name="TSource" />.</param>
        /// <param name="selector">A projection function to apply to each element.</param>
        /// <returns>The sum of the projected values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> or <paramref name="selector" /> is <see langword="null" />.</exception>
        /// <remarks><format type="text/markdown"><![CDATA[
        /// This method has at least one parameter of type <xref:System.Linq.Expressions.Expression%601> whose type argument is one of the <xref:System.Func%602> types. For these parameters, you can pass in a lambda expression and it will be compiled to an <xref:System.Linq.Expressions.Expression%601>.
        /// The <xref:System.Linq.Queryable.Sum%60%601%28System.Linq.IQueryable%7B%60%600%7D%2CSystem.Linq.Expressions.Expression%7BSystem.Func%7B%60%600%2CSystem.Nullable%7BSystem.Double%7D%7D%7D%29> method generates a <xref:System.Linq.Expressions.MethodCallExpression> that represents calling <xref:System.Linq.Queryable.Sum%60%601%28System.Linq.IQueryable%7B%60%600%7D%2CSystem.Linq.Expressions.Expression%7BSystem.Func%7B%60%600%2CSystem.Nullable%7BSystem.Double%7D%7D%7D%29> itself as a constructed generic method. It then passes the <xref:System.Linq.Expressions.MethodCallExpression> to the <xref:System.Linq.IQueryProvider.Execute%60%601%28System.Linq.Expressions.Expression%29> method of the <xref:System.Linq.IQueryProvider> represented by the <xref:System.Linq.IQueryable.Provider%2A> property of the `source` parameter.
        /// The query behavior that occurs as a result of executing an expression tree that represents calling <xref:System.Linq.Queryable.Sum%60%601%28System.Linq.IQueryable%7B%60%600%7D%2CSystem.Linq.Expressions.Expression%7BSystem.Func%7B%60%600%2CSystem.Nullable%7BSystem.Double%7D%7D%7D%29> depends on the implementation of the type of the `source` parameter. The expected behavior is that it invokes `selector` on each element of `source` and returns the sum of the resulting values.
        /// ## Examples
        /// The following code example demonstrates how to use <xref:System.Linq.Queryable.Sum%60%601%28System.Linq.IQueryable%7B%60%600%7D%2CSystem.Linq.Expressions.Expression%7BSystem.Func%7B%60%600%2CSystem.Double%7D%7D%29> to sum the projected values of a sequence.
        /// [!INCLUDE[sqo_diff_overload_example_func](~/includes/sqo-diff-overload-example-func-md.md)]
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Queryable/CS/queryable.cs" id="Snippet98":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Queryable/VB/queryable.vb" id="Snippet98":::
        /// ]]></format></remarks>
        [DynamicDependency("Sum`1", typeof(Enumerable))]
        public static double? Sum<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, double?>> selector)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (selector == null)
                throw Error.ArgumentNull(nameof(selector));
            return source.Provider.Execute<double?>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.Sum_NullableDouble_TSource_2(typeof(TSource)),
                    source.Expression, Expression.Quote(selector)
                    ));
        }

        /// <summary>Computes the sum of the sequence of <see cref="decimal" /> values that is obtained by invoking a projection function on each element of the input sequence.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence of values of type <typeparamref name="TSource" />.</param>
        /// <param name="selector">A projection function to apply to each element.</param>
        /// <returns>The sum of the projected values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> or <paramref name="selector" /> is <see langword="null" />.</exception>
        /// <exception cref="System.OverflowException">The sum is larger than <see cref="decimal.MaxValue" />.</exception>
        /// <remarks><format type="text/markdown"><![CDATA[
        /// This method has at least one parameter of type <xref:System.Linq.Expressions.Expression%601> whose type argument is one of the <xref:System.Func%602> types. For these parameters, you can pass in a lambda expression and it will be compiled to an <xref:System.Linq.Expressions.Expression%601>.
        /// The <xref:System.Linq.Queryable.Sum%60%601%28System.Linq.IQueryable%7B%60%600%7D%2CSystem.Linq.Expressions.Expression%7BSystem.Func%7B%60%600%2CSystem.Decimal%7D%7D%29> method generates a <xref:System.Linq.Expressions.MethodCallExpression> that represents calling <xref:System.Linq.Queryable.Sum%60%601%28System.Linq.IQueryable%7B%60%600%7D%2CSystem.Linq.Expressions.Expression%7BSystem.Func%7B%60%600%2CSystem.Decimal%7D%7D%29> itself as a constructed generic method. It then passes the <xref:System.Linq.Expressions.MethodCallExpression> to the <xref:System.Linq.IQueryProvider.Execute%60%601%28System.Linq.Expressions.Expression%29> method of the <xref:System.Linq.IQueryProvider> represented by the <xref:System.Linq.IQueryable.Provider%2A> property of the `source` parameter.
        /// The query behavior that occurs as a result of executing an expression tree that represents calling <xref:System.Linq.Queryable.Sum%60%601%28System.Linq.IQueryable%7B%60%600%7D%2CSystem.Linq.Expressions.Expression%7BSystem.Func%7B%60%600%2CSystem.Decimal%7D%7D%29> depends on the implementation of the type of the `source` parameter. The expected behavior is that it invokes `selector` on each element of `source` and returns the sum of the resulting values.
        /// ## Examples
        /// The following code example demonstrates how to use <xref:System.Linq.Queryable.Sum%60%601%28System.Linq.IQueryable%7B%60%600%7D%2CSystem.Linq.Expressions.Expression%7BSystem.Func%7B%60%600%2CSystem.Double%7D%7D%29> to sum the projected values of a sequence.
        /// [!INCLUDE[sqo_diff_overload_example_func](~/includes/sqo-diff-overload-example-func-md.md)]
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Queryable/CS/queryable.cs" id="Snippet98":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Queryable/VB/queryable.vb" id="Snippet98":::
        /// ]]></format></remarks>
        [DynamicDependency("Sum`1", typeof(Enumerable))]
        public static decimal Sum<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, decimal>> selector)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (selector == null)
                throw Error.ArgumentNull(nameof(selector));
            return source.Provider.Execute<decimal>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.Sum_Decimal_TSource_2(typeof(TSource)),
                    source.Expression, Expression.Quote(selector)
                    ));
        }

        /// <summary>Computes the sum of the sequence of nullable <see cref="decimal" /> values that is obtained by invoking a projection function on each element of the input sequence.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence of values of type <typeparamref name="TSource" />.</param>
        /// <param name="selector">A projection function to apply to each element.</param>
        /// <returns>The sum of the projected values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> or <paramref name="selector" /> is <see langword="null" />.</exception>
        /// <exception cref="System.OverflowException">The sum is larger than <see cref="decimal.MaxValue" />.</exception>
        /// <remarks><format type="text/markdown"><![CDATA[
        /// This method has at least one parameter of type <xref:System.Linq.Expressions.Expression%601> whose type argument is one of the <xref:System.Func%602> types. For these parameters, you can pass in a lambda expression and it will be compiled to an <xref:System.Linq.Expressions.Expression%601>.
        /// The <xref:System.Linq.Queryable.Sum%60%601%28System.Linq.IQueryable%7B%60%600%7D%2CSystem.Linq.Expressions.Expression%7BSystem.Func%7B%60%600%2CSystem.Nullable%7BSystem.Decimal%7D%7D%7D%29> method generates a <xref:System.Linq.Expressions.MethodCallExpression> that represents calling <xref:System.Linq.Queryable.Sum%60%601%28System.Linq.IQueryable%7B%60%600%7D%2CSystem.Linq.Expressions.Expression%7BSystem.Func%7B%60%600%2CSystem.Nullable%7BSystem.Decimal%7D%7D%7D%29> itself as a constructed generic method. It then passes the <xref:System.Linq.Expressions.MethodCallExpression> to the <xref:System.Linq.IQueryProvider.Execute%60%601%28System.Linq.Expressions.Expression%29> method of the <xref:System.Linq.IQueryProvider> represented by the <xref:System.Linq.IQueryable.Provider%2A> property of the `source` parameter.
        /// The query behavior that occurs as a result of executing an expression tree that represents calling <xref:System.Linq.Queryable.Sum%60%601%28System.Linq.IQueryable%7B%60%600%7D%2CSystem.Linq.Expressions.Expression%7BSystem.Func%7B%60%600%2CSystem.Nullable%7BSystem.Decimal%7D%7D%7D%29> depends on the implementation of the type of the `source` parameter. The expected behavior is that it invokes `selector` on each element of `source` and returns the sum of the resulting values.
        /// ## Examples
        /// The following code example demonstrates how to use <xref:System.Linq.Queryable.Sum%60%601%28System.Linq.IQueryable%7B%60%600%7D%2CSystem.Linq.Expressions.Expression%7BSystem.Func%7B%60%600%2CSystem.Double%7D%7D%29> to sum the projected values of a sequence.
        /// [!INCLUDE[sqo_diff_overload_example_func](~/includes/sqo-diff-overload-example-func-md.md)]
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Queryable/CS/queryable.cs" id="Snippet98":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Queryable/VB/queryable.vb" id="Snippet98":::
        /// ]]></format></remarks>
        [DynamicDependency("Sum`1", typeof(Enumerable))]
        public static decimal? Sum<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, decimal?>> selector)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (selector == null)
                throw Error.ArgumentNull(nameof(selector));
            return source.Provider.Execute<decimal?>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.Sum_NullableDecimal_TSource_2(typeof(TSource)),
                    source.Expression, Expression.Quote(selector)
                    ));
        }

        /// <summary>Computes the average of a sequence of <see cref="int" /> values.</summary>
        /// <param name="source">A sequence of <see cref="int" /> values to calculate the average of.</param>
        /// <returns>The average of the sequence of values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <exception cref="System.InvalidOperationException"><paramref name="source" /> contains no elements.</exception>
        /// <remarks>
        /// <para>The <see cref="System.Linq.Queryable.Average(System.Linq.IQueryable{int})" /> method generates a <see cref="System.Linq.Expressions.MethodCallExpression" /> that represents calling <see cref="System.Linq.Queryable.Average(System.Linq.IQueryable{int})" /> itself. It then passes the <see cref="System.Linq.Expressions.MethodCallExpression" /> to the <see cref="System.Linq.IQueryProvider.Execute{T}(System.Linq.Expressions.Expression)" /> method of the <see cref="System.Linq.IQueryProvider" /> represented by the <see cref="O:System.Linq.IQueryable.Provider" /> property of the <paramref name="source" /> parameter.</para>
        /// <para>The query behavior that occurs as a result of executing an expression tree that represents calling <see cref="System.Linq.Queryable.Average(System.Linq.IQueryable{int})" /> depends on the implementation of the type of the <paramref name="source" /> parameter. The expected behavior is that it calculates the average of the values in <paramref name="source" />.</para>
        /// </remarks>
        /// <example>The following code example demonstrates how to use <see cref="System.Linq.Queryable.Average(System.Linq.IQueryable{int})" /> to calculate the average of a sequence of values.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Queryable/CS/queryable.cs" interactive="try-dotnet-method" id="Snippet8":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Queryable/VB/queryable.vb" id="Snippet8":::</example>
        [DynamicDependency("Average", typeof(Enumerable))]
        public static double Average(this IQueryable<int> source)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            return source.Provider.Execute<double>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.Average_Int32_1, source.Expression));
        }

        /// <summary>Computes the average of a sequence of nullable <see cref="int" /> values.</summary>
        /// <param name="source">A sequence of nullable <see cref="int" /> values to calculate the average of.</param>
        /// <returns>The average of the sequence of values, or <see langword="null" /> if the source sequence is empty or contains only <see langword="null" /> values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <remarks><format type="text/markdown"><![CDATA[
        /// The <xref:System.Linq.Queryable.Average%28System.Linq.IQueryable%7BSystem.Nullable%7BSystem.Int32%7D%7D%29> method generates a <xref:System.Linq.Expressions.MethodCallExpression> that represents calling <xref:System.Linq.Queryable.Average%28System.Linq.IQueryable%7BSystem.Nullable%7BSystem.Int32%7D%7D%29> itself. It then passes the <xref:System.Linq.Expressions.MethodCallExpression> to the <xref:System.Linq.IQueryProvider.Execute%60%601%28System.Linq.Expressions.Expression%29> method of the <xref:System.Linq.IQueryProvider> represented by the <xref:System.Linq.IQueryable.Provider%2A> property of the `source` parameter.
        /// The query behavior that occurs as a result of executing an expression tree that represents calling <xref:System.Linq.Queryable.Average%28System.Linq.IQueryable%7BSystem.Nullable%7BSystem.Int32%7D%7D%29> depends on the implementation of the type of the `source` parameter. The expected behavior is that it calculates the average of the values in `source`.
        /// ## Examples
        /// The following code example demonstrates how to use <xref:System.Linq.Queryable.Average%28System.Linq.IQueryable%7BSystem.Nullable%7BSystem.Int64%7D%7D%29> to calculate the average of a sequence of values.
        /// [!INCLUDE[sqo_diff_overload_example_elementtype](~/includes/sqo-diff-overload-example-elementtype-md.md)]
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Queryable/CS/queryable.cs" interactive="try-dotnet-method" id="Snippet12":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Queryable/VB/queryable.vb" id="Snippet12":::
        /// ]]></format></remarks>
        [DynamicDependency("Average", typeof(Enumerable))]
        public static double? Average(this IQueryable<int?> source)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            return source.Provider.Execute<double?>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.Average_NullableInt32_1, source.Expression));
        }

        /// <summary>Computes the average of a sequence of <see cref="long" /> values.</summary>
        /// <param name="source">A sequence of <see cref="long" /> values to calculate the average of.</param>
        /// <returns>The average of the sequence of values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <exception cref="System.InvalidOperationException"><paramref name="source" /> contains no elements.</exception>
        /// <remarks><format type="text/markdown"><![CDATA[
        /// The <xref:System.Linq.Queryable.Average%28System.Linq.IQueryable%7BSystem.Int64%7D%29> method generates a <xref:System.Linq.Expressions.MethodCallExpression> that represents calling <xref:System.Linq.Queryable.Average%28System.Linq.IQueryable%7BSystem.Int64%7D%29> itself. It then passes the <xref:System.Linq.Expressions.MethodCallExpression> to the <xref:System.Linq.IQueryProvider.Execute%60%601%28System.Linq.Expressions.Expression%29> method of the <xref:System.Linq.IQueryProvider> represented by the <xref:System.Linq.IQueryable.Provider%2A> property of the `source` parameter.
        /// The query behavior that occurs as a result of executing an expression tree that represents calling <xref:System.Linq.Queryable.Average%28System.Linq.IQueryable%7BSystem.Int64%7D%29> depends on the implementation of the type of the `source` parameter. The expected behavior is that it calculates the average of the values in `source`.
        /// ## Examples
        /// The following code example demonstrates how to use <xref:System.Linq.Queryable.Average%28System.Linq.IQueryable%7BSystem.Int32%7D%29> to calculate the average of a sequence of values.
        /// [!INCLUDE[sqo_diff_overload_example_elementtype](~/includes/sqo-diff-overload-example-elementtype-md.md)]
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Queryable/CS/queryable.cs" interactive="try-dotnet-method" id="Snippet8":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Queryable/VB/queryable.vb" id="Snippet8":::
        /// ]]></format></remarks>
        [DynamicDependency("Average", typeof(Enumerable))]
        public static double Average(this IQueryable<long> source)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            return source.Provider.Execute<double>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.Average_Int64_1, source.Expression));
        }

        /// <summary>Computes the average of a sequence of nullable <see cref="long" /> values.</summary>
        /// <param name="source">A sequence of nullable <see cref="long" /> values to calculate the average of.</param>
        /// <returns>The average of the sequence of values, or <see langword="null" /> if the source sequence is empty or contains only <see langword="null" /> values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <remarks>
        /// <para>The <see cref="System.Linq.Queryable.Average(System.Linq.IQueryable{System.Nullable{long}})" /> method generates a <see cref="System.Linq.Expressions.MethodCallExpression" /> that represents calling <see cref="System.Linq.Queryable.Average(System.Linq.IQueryable{System.Nullable{long}})" /> itself. It then passes the <see cref="System.Linq.Expressions.MethodCallExpression" /> to the <see cref="System.Linq.IQueryProvider.Execute{T}(System.Linq.Expressions.Expression)" /> method of the <see cref="System.Linq.IQueryProvider" /> represented by the <see cref="O:System.Linq.IQueryable.Provider" /> property of the <paramref name="source" /> parameter.</para>
        /// <para>The query behavior that occurs as a result of executing an expression tree that represents calling <see cref="System.Linq.Queryable.Average(System.Linq.IQueryable{System.Nullable{long}})" /> depends on the implementation of the type of the <paramref name="source" /> parameter. The expected behavior is that it calculates the average of the values in <paramref name="source" />.</para>
        /// </remarks>
        /// <example>The following code example demonstrates how to use <see cref="System.Linq.Queryable.Average(System.Linq.IQueryable{System.Nullable{long}})" /> to calculate the average of a sequence of values.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Queryable/CS/queryable.cs" interactive="try-dotnet-method" id="Snippet12":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Queryable/VB/queryable.vb" id="Snippet12":::</example>
        [DynamicDependency("Average", typeof(Enumerable))]
        public static double? Average(this IQueryable<long?> source)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            return source.Provider.Execute<double?>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.Average_NullableInt64_1, source.Expression));
        }

        /// <summary>Computes the average of a sequence of <see cref="float" /> values.</summary>
        /// <param name="source">A sequence of <see cref="float" /> values to calculate the average of.</param>
        /// <returns>The average of the sequence of values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <exception cref="System.InvalidOperationException"><paramref name="source" /> contains no elements.</exception>
        /// <remarks><format type="text/markdown"><![CDATA[
        /// The <xref:System.Linq.Queryable.Average%28System.Linq.IQueryable%7BSystem.Single%7D%29> method generates a <xref:System.Linq.Expressions.MethodCallExpression> that represents calling <xref:System.Linq.Queryable.Average%28System.Linq.IQueryable%7BSystem.Single%7D%29> itself. It then passes the <xref:System.Linq.Expressions.MethodCallExpression> to the <xref:System.Linq.IQueryProvider.Execute%60%601%28System.Linq.Expressions.Expression%29> method of the <xref:System.Linq.IQueryProvider> represented by the <xref:System.Linq.IQueryable.Provider%2A> property of the `source` parameter.
        /// The query behavior that occurs as a result of executing an expression tree that represents calling <xref:System.Linq.Queryable.Average%28System.Linq.IQueryable%7BSystem.Single%7D%29> depends on the implementation of the type of the `source` parameter. The expected behavior is that it calculates the average of the values in `source`.
        /// ## Examples
        /// The following code example demonstrates how to use <xref:System.Linq.Queryable.Average%28System.Linq.IQueryable%7BSystem.Int32%7D%29> to calculate the average of a sequence of values.
        /// [!INCLUDE[sqo_diff_overload_example_elementtype](~/includes/sqo-diff-overload-example-elementtype-md.md)]
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Queryable/CS/queryable.cs" interactive="try-dotnet-method" id="Snippet8":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Queryable/VB/queryable.vb" id="Snippet8":::
        /// ]]></format></remarks>
        [DynamicDependency("Average", typeof(Enumerable))]
        public static float Average(this IQueryable<float> source)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            return source.Provider.Execute<float>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.Average_Single_1, source.Expression));
        }

        /// <summary>Computes the average of a sequence of nullable <see cref="float" /> values.</summary>
        /// <param name="source">A sequence of nullable <see cref="float" /> values to calculate the average of.</param>
        /// <returns>The average of the sequence of values, or <see langword="null" /> if the source sequence is empty or contains only <see langword="null" /> values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <remarks><format type="text/markdown"><![CDATA[
        /// The <xref:System.Linq.Queryable.Average%28System.Linq.IQueryable%7BSystem.Nullable%7BSystem.Single%7D%7D%29> method generates a <xref:System.Linq.Expressions.MethodCallExpression> that represents calling <xref:System.Linq.Queryable.Average%28System.Linq.IQueryable%7BSystem.Nullable%7BSystem.Single%7D%7D%29> itself. It then passes the <xref:System.Linq.Expressions.MethodCallExpression> to the <xref:System.Linq.IQueryProvider.Execute%60%601%28System.Linq.Expressions.Expression%29> method of the <xref:System.Linq.IQueryProvider> represented by the <xref:System.Linq.IQueryable.Provider%2A> property of the `source` parameter.
        /// The query behavior that occurs as a result of executing an expression tree that represents calling <xref:System.Linq.Queryable.Average%28System.Linq.IQueryable%7BSystem.Nullable%7BSystem.Single%7D%7D%29> depends on the implementation of the type of the `source` parameter. The expected behavior is that it calculates the average of the values in `source`.
        /// ## Examples
        /// The following code example demonstrates how to use <xref:System.Linq.Queryable.Average%28System.Linq.IQueryable%7BSystem.Nullable%7BSystem.Int64%7D%7D%29> to calculate the average of a sequence of values.
        /// [!INCLUDE[sqo_diff_overload_example_elementtype](~/includes/sqo-diff-overload-example-elementtype-md.md)]
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Queryable/CS/queryable.cs" interactive="try-dotnet-method" id="Snippet12":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Queryable/VB/queryable.vb" id="Snippet12":::
        /// ]]></format></remarks>
        [DynamicDependency("Average", typeof(Enumerable))]
        public static float? Average(this IQueryable<float?> source)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            return source.Provider.Execute<float?>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.Average_NullableSingle_1, source.Expression));
        }

        /// <summary>Computes the average of a sequence of <see cref="double" /> values.</summary>
        /// <param name="source">A sequence of <see cref="double" /> values to calculate the average of.</param>
        /// <returns>The average of the sequence of values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <exception cref="System.InvalidOperationException"><paramref name="source" /> contains no elements.</exception>
        /// <remarks><format type="text/markdown"><![CDATA[
        /// The <xref:System.Linq.Queryable.Average%28System.Linq.IQueryable%7BSystem.Double%7D%29> method generates a <xref:System.Linq.Expressions.MethodCallExpression> that represents calling <xref:System.Linq.Queryable.Average%28System.Linq.IQueryable%7BSystem.Double%7D%29> itself. It then passes the <xref:System.Linq.Expressions.MethodCallExpression> to the <xref:System.Linq.IQueryProvider.Execute%60%601%28System.Linq.Expressions.Expression%29> method of the <xref:System.Linq.IQueryProvider> represented by the <xref:System.Linq.IQueryable.Provider%2A> property of the `source` parameter.
        /// The query behavior that occurs as a result of executing an expression tree that represents calling <xref:System.Linq.Queryable.Average%28System.Linq.IQueryable%7BSystem.Double%7D%29> depends on the implementation of the type of the `source` parameter. The expected behavior is that it calculates the average of the values in `source`.
        /// ## Examples
        /// The following code example demonstrates how to use <xref:System.Linq.Queryable.Average%28System.Linq.IQueryable%7BSystem.Int32%7D%29> to calculate the average of a sequence of values.
        /// [!INCLUDE[sqo_diff_overload_example_elementtype](~/includes/sqo-diff-overload-example-elementtype-md.md)]
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Queryable/CS/queryable.cs" interactive="try-dotnet-method" id="Snippet8":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Queryable/VB/queryable.vb" id="Snippet8":::
        /// ]]></format></remarks>
        [DynamicDependency("Average", typeof(Enumerable))]
        public static double Average(this IQueryable<double> source)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            return source.Provider.Execute<double>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.Average_Double_1, source.Expression));
        }

        /// <summary>Computes the average of a sequence of nullable <see cref="double" /> values.</summary>
        /// <param name="source">A sequence of nullable <see cref="double" /> values to calculate the average of.</param>
        /// <returns>The average of the sequence of values, or <see langword="null" /> if the source sequence is empty or contains only <see langword="null" /> values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <remarks><format type="text/markdown"><![CDATA[
        /// The <xref:System.Linq.Queryable.Average%28System.Linq.IQueryable%7BSystem.Nullable%7BSystem.Double%7D%7D%29> method generates a <xref:System.Linq.Expressions.MethodCallExpression> that represents calling <xref:System.Linq.Queryable.Average%28System.Linq.IQueryable%7BSystem.Nullable%7BSystem.Double%7D%7D%29> itself. It then passes the <xref:System.Linq.Expressions.MethodCallExpression> to the <xref:System.Linq.IQueryProvider.Execute%60%601%28System.Linq.Expressions.Expression%29> method of the <xref:System.Linq.IQueryProvider> represented by the <xref:System.Linq.IQueryable.Provider%2A> property of the `source` parameter.
        /// The query behavior that occurs as a result of executing an expression tree that represents calling <xref:System.Linq.Queryable.Average%28System.Linq.IQueryable%7BSystem.Nullable%7BSystem.Double%7D%7D%29> depends on the implementation of the type of the `source` parameter. The expected behavior is that it calculates the average of the values in `source`.
        /// ## Examples
        /// The following code example demonstrates how to use <xref:System.Linq.Queryable.Average%28System.Linq.IQueryable%7BSystem.Nullable%7BSystem.Int64%7D%7D%29> to calculate the average of a sequence of values.
        /// [!INCLUDE[sqo_diff_overload_example_elementtype](~/includes/sqo-diff-overload-example-elementtype-md.md)]
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Queryable/CS/queryable.cs" interactive="try-dotnet-method" id="Snippet12":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Queryable/VB/queryable.vb" id="Snippet12":::
        /// ]]></format></remarks>
        [DynamicDependency("Average", typeof(Enumerable))]
        public static double? Average(this IQueryable<double?> source)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            return source.Provider.Execute<double?>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.Average_NullableDouble_1, source.Expression));
        }

        /// <summary>Computes the average of a sequence of <see cref="decimal" /> values.</summary>
        /// <param name="source">A sequence of <see cref="decimal" /> values to calculate the average of.</param>
        /// <returns>The average of the sequence of values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <exception cref="System.InvalidOperationException"><paramref name="source" /> contains no elements.</exception>
        /// <remarks><format type="text/markdown"><![CDATA[
        /// The <xref:System.Linq.Queryable.Average%28System.Linq.IQueryable%7BSystem.Decimal%7D%29> method generates a <xref:System.Linq.Expressions.MethodCallExpression> that represents calling <xref:System.Linq.Queryable.Average%28System.Linq.IQueryable%7BSystem.Decimal%7D%29> itself. It then passes the <xref:System.Linq.Expressions.MethodCallExpression> to the <xref:System.Linq.IQueryProvider.Execute%60%601%28System.Linq.Expressions.Expression%29> method of the <xref:System.Linq.IQueryProvider> represented by the <xref:System.Linq.IQueryable.Provider%2A> property of the `source` parameter.
        /// The query behavior that occurs as a result of executing an expression tree that represents calling <xref:System.Linq.Queryable.Average%28System.Linq.IQueryable%7BSystem.Decimal%7D%29> depends on the implementation of the type of the `source` parameter. The expected behavior is that it calculates the average of the values in `source`.
        /// ## Examples
        /// The following code example demonstrates how to use <xref:System.Linq.Queryable.Average%28System.Linq.IQueryable%7BSystem.Int32%7D%29> to calculate the average of a sequence of values.
        /// [!INCLUDE[sqo_diff_overload_example_elementtype](~/includes/sqo-diff-overload-example-elementtype-md.md)]
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Queryable/CS/queryable.cs" interactive="try-dotnet-method" id="Snippet8":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Queryable/VB/queryable.vb" id="Snippet8":::
        /// ]]></format></remarks>
        [DynamicDependency("Average", typeof(Enumerable))]
        public static decimal Average(this IQueryable<decimal> source)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            return source.Provider.Execute<decimal>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.Average_Decimal_1, source.Expression));
        }

        /// <summary>Computes the average of a sequence of nullable <see cref="decimal" /> values.</summary>
        /// <param name="source">A sequence of nullable <see cref="decimal" /> values to calculate the average of.</param>
        /// <returns>The average of the sequence of values, or <see langword="null" /> if the source sequence is empty or contains only <see langword="null" /> values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <remarks><format type="text/markdown"><![CDATA[
        /// The <xref:System.Linq.Queryable.Average%28System.Linq.IQueryable%7BSystem.Nullable%7BSystem.Decimal%7D%7D%29> method generates a <xref:System.Linq.Expressions.MethodCallExpression> that represents calling <xref:System.Linq.Queryable.Average%28System.Linq.IQueryable%7BSystem.Nullable%7BSystem.Decimal%7D%7D%29> itself. It then passes the <xref:System.Linq.Expressions.MethodCallExpression> to the <xref:System.Linq.IQueryProvider.Execute%60%601%28System.Linq.Expressions.Expression%29> method of the <xref:System.Linq.IQueryProvider> represented by the <xref:System.Linq.IQueryable.Provider%2A> property of the `source` parameter.
        /// The query behavior that occurs as a result of executing an expression tree that represents calling <xref:System.Linq.Queryable.Average%28System.Linq.IQueryable%7BSystem.Nullable%7BSystem.Decimal%7D%7D%29> depends on the implementation of the type of the `source` parameter. The expected behavior is that it calculates the average of the values in `source`.
        /// ## Examples
        /// The following code example demonstrates how to use <xref:System.Linq.Queryable.Average%28System.Linq.IQueryable%7BSystem.Nullable%7BSystem.Int64%7D%7D%29> to calculate the average of a sequence of values.
        /// [!INCLUDE[sqo_diff_overload_example_elementtype](~/includes/sqo-diff-overload-example-elementtype-md.md)]
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Queryable/CS/queryable.cs" interactive="try-dotnet-method" id="Snippet12":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Queryable/VB/queryable.vb" id="Snippet12":::
        /// ]]></format></remarks>
        [DynamicDependency("Average", typeof(Enumerable))]
        public static decimal? Average(this IQueryable<decimal?> source)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            return source.Provider.Execute<decimal?>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.Average_NullableDecimal_1, source.Expression));
        }

        /// <summary>Computes the average of a sequence of <see cref="int" /> values that is obtained by invoking a projection function on each element of the input sequence.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence of values to calculate the average of.</param>
        /// <param name="selector">A projection function to apply to each element.</param>
        /// <returns>The average of the sequence of values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> or <paramref name="selector" /> is <see langword="null" />.</exception>
        /// <exception cref="System.InvalidOperationException"><paramref name="source" /> contains no elements.</exception>
        /// <remarks>
        /// <para>This method has at least one parameter of type <see cref="System.Linq.Expressions.Expression{T}" /> whose type argument is one of the <see cref="System.Func{T1,T2}" /> types. For these parameters, you can pass in a lambda expression and it will be compiled to an <see cref="System.Linq.Expressions.Expression{T}" />.</para>
        /// <para>The <see cref="System.Linq.Queryable.Average{T}(System.Linq.IQueryable{T},System.Linq.Expressions.Expression{System.Func{T,int}})" /> method generates a <see cref="System.Linq.Expressions.MethodCallExpression" /> that represents calling <see cref="System.Linq.Queryable.Average{T}(System.Linq.IQueryable{T},System.Linq.Expressions.Expression{System.Func{T,int}})" /> itself as a constructed generic method. It then passes the <see cref="System.Linq.Expressions.MethodCallExpression" /> to the <see cref="System.Linq.IQueryProvider.Execute{T}(System.Linq.Expressions.Expression)" /> method of the <see cref="System.Linq.IQueryProvider" /> represented by the <see cref="O:System.Linq.IQueryable.Provider" /> property of the <paramref name="source" /> parameter.</para>
        /// <para>The query behavior that occurs as a result of executing an expression tree that represents calling <see cref="System.Linq.Queryable.Average{T}(System.Linq.IQueryable{T},System.Linq.Expressions.Expression{System.Func{T,int}})" /> depends on the implementation of the type of the <paramref name="source" /> parameter. The expected behavior is that it calculates the average of the values in <paramref name="source" /> after invoking <paramref name="selector" /> on each value.</para>
        /// </remarks>
        /// <example>The following code example demonstrates how to use <see cref="System.Linq.Queryable.Average{T}(System.Linq.IQueryable{T},System.Linq.Expressions.Expression{System.Func{T,int}})" /> to calculate the average <see cref="string" /> length in a sequence of values of type <see cref="string" />.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Queryable/CS/queryable.cs" interactive="try-dotnet-method" id="Snippet18":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Queryable/VB/queryable.vb" id="Snippet18":::</example>
        [DynamicDependency("Average`1", typeof(Enumerable))]
        public static double Average<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, int>> selector)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (selector == null)
                throw Error.ArgumentNull(nameof(selector));
            return source.Provider.Execute<double>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.Average_Int32_TSource_2(typeof(TSource)),
                    source.Expression, Expression.Quote(selector)
                    ));
        }

        /// <summary>Computes the average of a sequence of nullable <see cref="int" /> values that is obtained by invoking a projection function on each element of the input sequence.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence of values to calculate the average of.</param>
        /// <param name="selector">A projection function to apply to each element.</param>
        /// <returns>The average of the sequence of values, or <see langword="null" /> if the <paramref name="source" /> sequence is empty or contains only <see langword="null" /> values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> or <paramref name="selector" /> is <see langword="null" />.</exception>
        /// <remarks><format type="text/markdown"><![CDATA[
        /// This method has at least one parameter of type <xref:System.Linq.Expressions.Expression%601> whose type argument is one of the <xref:System.Func%602> types. For these parameters, you can pass in a lambda expression and it will be compiled to an <xref:System.Linq.Expressions.Expression%601>.
        /// The <xref:System.Linq.Queryable.Average%60%601%28System.Linq.IQueryable%7B%60%600%7D%2CSystem.Linq.Expressions.Expression%7BSystem.Func%7B%60%600%2CSystem.Nullable%7BSystem.Int32%7D%7D%7D%29> method generates a <xref:System.Linq.Expressions.MethodCallExpression> that represents calling <xref:System.Linq.Queryable.Average%60%601%28System.Linq.IQueryable%7B%60%600%7D%2CSystem.Linq.Expressions.Expression%7BSystem.Func%7B%60%600%2CSystem.Nullable%7BSystem.Int32%7D%7D%7D%29> itself as a constructed generic method. It then passes the <xref:System.Linq.Expressions.MethodCallExpression> to the <xref:System.Linq.IQueryProvider.Execute%60%601%28System.Linq.Expressions.Expression%29> method of the <xref:System.Linq.IQueryProvider> represented by <xref:System.Linq.IQueryable.Provider%2A> property of the `source` parameter.
        /// The query behavior that occurs as a result of executing an expression tree that represents calling <xref:System.Linq.Queryable.Average%60%601%28System.Linq.IQueryable%7B%60%600%7D%2CSystem.Linq.Expressions.Expression%7BSystem.Func%7B%60%600%2CSystem.Nullable%7BSystem.Int32%7D%7D%7D%29> depends on the implementation of the type of the `source` parameter. The expected behavior is that it calculates the average of the values in `source` after invoking `selector` on each value.
        /// ## Examples
        /// The following code example demonstrates how to use <xref:System.Linq.Queryable.Average%60%601%28System.Linq.IQueryable%7B%60%600%7D%2CSystem.Linq.Expressions.Expression%7BSystem.Func%7B%60%600%2CSystem.Int32%7D%7D%29> to calculate the average <xref:System.String> length in a sequence of values of type <xref:System.String>.
        /// [!INCLUDE[sqo_diff_overload_example_func](~/includes/sqo-diff-overload-example-func-md.md)]
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Queryable/CS/queryable.cs" interactive="try-dotnet-method" id="Snippet18":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Queryable/VB/queryable.vb" id="Snippet18":::
        /// ]]></format></remarks>
        [DynamicDependency("Average`1", typeof(Enumerable))]
        public static double? Average<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, int?>> selector)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (selector == null)
                throw Error.ArgumentNull(nameof(selector));
            return source.Provider.Execute<double?>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.Average_NullableInt32_TSource_2(typeof(TSource)),
                    source.Expression, Expression.Quote(selector)
                    ));
        }

        /// <summary>Computes the average of a sequence of <see cref="float" /> values that is obtained by invoking a projection function on each element of the input sequence.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence of values to calculate the average of.</param>
        /// <param name="selector">A projection function to apply to each element.</param>
        /// <returns>The average of the sequence of values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> or <paramref name="selector" /> is <see langword="null" />.</exception>
        /// <exception cref="System.InvalidOperationException"><paramref name="source" /> contains no elements.</exception>
        /// <remarks><format type="text/markdown"><![CDATA[
        /// This method has at least one parameter of type <xref:System.Linq.Expressions.Expression%601> whose type argument is one of the <xref:System.Func%602> types. For these parameters, you can pass in a lambda expression and it will be compiled to an <xref:System.Linq.Expressions.Expression%601>.
        /// The <xref:System.Linq.Queryable.Average%60%601%28System.Linq.IQueryable%7B%60%600%7D%2CSystem.Linq.Expressions.Expression%7BSystem.Func%7B%60%600%2CSystem.Single%7D%7D%29> method generates a <xref:System.Linq.Expressions.MethodCallExpression> that represents calling <xref:System.Linq.Queryable.Average%60%601%28System.Linq.IQueryable%7B%60%600%7D%2CSystem.Linq.Expressions.Expression%7BSystem.Func%7B%60%600%2CSystem.Single%7D%7D%29> itself as a constructed generic method. It then passes the <xref:System.Linq.Expressions.MethodCallExpression> to the <xref:System.Linq.IQueryProvider.Execute%60%601%28System.Linq.Expressions.Expression%29> method of the <xref:System.Linq.IQueryProvider> represented by the <xref:System.Linq.IQueryable.Provider%2A> property of the `source` parameter.
        /// The query behavior that occurs as a result of executing an expression tree that represents calling <xref:System.Linq.Queryable.Average%60%601%28System.Linq.IQueryable%7B%60%600%7D%2CSystem.Linq.Expressions.Expression%7BSystem.Func%7B%60%600%2CSystem.Single%7D%7D%29> depends on the implementation of the type of the `source` parameter. The expected behavior is that it calculates the average of the values in `source` after invoking `selector` on each value.
        /// ## Examples
        /// The following code example demonstrates how to use <xref:System.Linq.Queryable.Average%60%601%28System.Linq.IQueryable%7B%60%600%7D%2CSystem.Linq.Expressions.Expression%7BSystem.Func%7B%60%600%2CSystem.Int32%7D%7D%29> to calculate the average <xref:System.String> length in a sequence of values of type <xref:System.String>.
        /// [!INCLUDE[sqo_diff_overload_example_func](~/includes/sqo-diff-overload-example-func-md.md)]
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Queryable/CS/queryable.cs" interactive="try-dotnet-method" id="Snippet18":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Queryable/VB/queryable.vb" id="Snippet18":::
        /// ]]></format></remarks>
        [DynamicDependency("Average`1", typeof(Enumerable))]
        public static float Average<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, float>> selector)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (selector == null)
                throw Error.ArgumentNull(nameof(selector));
            return source.Provider.Execute<float>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.Average_Single_TSource_2(typeof(TSource)),
                    source.Expression, Expression.Quote(selector)
                    ));
        }

        /// <summary>Computes the average of a sequence of nullable <see cref="float" /> values that is obtained by invoking a projection function on each element of the input sequence.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence of values to calculate the average of.</param>
        /// <param name="selector">A projection function to apply to each element.</param>
        /// <returns>The average of the sequence of values, or <see langword="null" /> if the <paramref name="source" /> sequence is empty or contains only <see langword="null" /> values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> or <paramref name="selector" /> is <see langword="null" />.</exception>
        /// <remarks><format type="text/markdown"><![CDATA[
        /// This method has at least one parameter of type <xref:System.Linq.Expressions.Expression%601> whose type argument is one of the <xref:System.Func%602> types. For these parameters, you can pass in a lambda expression and it will be compiled to an <xref:System.Linq.Expressions.Expression%601>.
        /// The <xref:System.Linq.Queryable.Average%60%601%28System.Linq.IQueryable%7B%60%600%7D%2CSystem.Linq.Expressions.Expression%7BSystem.Func%7B%60%600%2CSystem.Nullable%7BSystem.Single%7D%7D%7D%29> method generates a <xref:System.Linq.Expressions.MethodCallExpression> that represents calling <xref:System.Linq.Queryable.Average%60%601%28System.Linq.IQueryable%7B%60%600%7D%2CSystem.Linq.Expressions.Expression%7BSystem.Func%7B%60%600%2CSystem.Nullable%7BSystem.Single%7D%7D%7D%29> itself as a constructed generic method. It then passes the <xref:System.Linq.Expressions.MethodCallExpression> to the <xref:System.Linq.IQueryProvider.Execute%60%601%28System.Linq.Expressions.Expression%29> method of the <xref:System.Linq.IQueryProvider> represented by <xref:System.Linq.IQueryable.Provider%2A> property of the `source` parameter.
        /// The query behavior that occurs as a result of executing an expression tree that represents calling <xref:System.Linq.Queryable.Average%60%601%28System.Linq.IQueryable%7B%60%600%7D%2CSystem.Linq.Expressions.Expression%7BSystem.Func%7B%60%600%2CSystem.Nullable%7BSystem.Single%7D%7D%7D%29> depends on the implementation of the type of the `source` parameter. The expected behavior is that it calculates the average of the values in `source` after invoking `selector` on each value.
        /// ## Examples
        /// The following code example demonstrates how to use <xref:System.Linq.Queryable.Average%60%601%28System.Linq.IQueryable%7B%60%600%7D%2CSystem.Linq.Expressions.Expression%7BSystem.Func%7B%60%600%2CSystem.Int32%7D%7D%29> to calculate the average <xref:System.String> length in a sequence of values of type <xref:System.String>.
        /// [!INCLUDE[sqo_diff_overload_example_func](~/includes/sqo-diff-overload-example-func-md.md)]
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Queryable/CS/queryable.cs" interactive="try-dotnet-method" id="Snippet18":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Queryable/VB/queryable.vb" id="Snippet18":::
        /// ]]></format></remarks>
        [DynamicDependency("Average`1", typeof(Enumerable))]
        public static float? Average<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, float?>> selector)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (selector == null)
                throw Error.ArgumentNull(nameof(selector));
            return source.Provider.Execute<float?>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.Average_NullableSingle_TSource_2(typeof(TSource)),
                    source.Expression, Expression.Quote(selector)
                    ));
        }

        /// <summary>Computes the average of a sequence of <see cref="long" /> values that is obtained by invoking a projection function on each element of the input sequence.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence of values to calculate the average of.</param>
        /// <param name="selector">A projection function to apply to each element.</param>
        /// <returns>The average of the sequence of values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> or <paramref name="selector" /> is <see langword="null" />.</exception>
        /// <exception cref="System.InvalidOperationException"><paramref name="source" /> contains no elements.</exception>
        /// <remarks><format type="text/markdown"><![CDATA[
        /// This method has at least one parameter of type <xref:System.Linq.Expressions.Expression%601> whose type argument is one of the <xref:System.Func%602> types. For these parameters, you can pass in a lambda expression and it will be compiled to an <xref:System.Linq.Expressions.Expression%601>.
        /// The <xref:System.Linq.Queryable.Average%60%601%28System.Linq.IQueryable%7B%60%600%7D%2CSystem.Linq.Expressions.Expression%7BSystem.Func%7B%60%600%2CSystem.Int64%7D%7D%29> method generates a <xref:System.Linq.Expressions.MethodCallExpression> that represents calling <xref:System.Linq.Queryable.Average%60%601%28System.Linq.IQueryable%7B%60%600%7D%2CSystem.Linq.Expressions.Expression%7BSystem.Func%7B%60%600%2CSystem.Int64%7D%7D%29> itself as a constructed generic method. It then passes the <xref:System.Linq.Expressions.MethodCallExpression> to the <xref:System.Linq.IQueryProvider.Execute%60%601%28System.Linq.Expressions.Expression%29> method of the <xref:System.Linq.IQueryProvider> represented by the <xref:System.Linq.IQueryable.Provider%2A> property of the `source` parameter.
        /// The query behavior that occurs as a result of executing an expression tree that represents calling <xref:System.Linq.Queryable.Average%60%601%28System.Linq.IQueryable%7B%60%600%7D%2CSystem.Linq.Expressions.Expression%7BSystem.Func%7B%60%600%2CSystem.Int64%7D%7D%29> depends on the implementation of the type of the `source` parameter. The expected behavior is that it calculates the average of the values in `source` after invoking `selector` on each value.
        /// ## Examples
        /// The following code example demonstrates how to use <xref:System.Linq.Queryable.Average%60%601%28System.Linq.IQueryable%7B%60%600%7D%2CSystem.Linq.Expressions.Expression%7BSystem.Func%7B%60%600%2CSystem.Int32%7D%7D%29> to calculate the average <xref:System.String> length in a sequence of values of type <xref:System.String>.
        /// [!INCLUDE[sqo_diff_overload_example_func](~/includes/sqo-diff-overload-example-func-md.md)]
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Queryable/CS/queryable.cs" interactive="try-dotnet-method" id="Snippet18":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Queryable/VB/queryable.vb" id="Snippet18":::
        /// ]]></format></remarks>
        [DynamicDependency("Average`1", typeof(Enumerable))]
        public static double Average<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, long>> selector)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (selector == null)
                throw Error.ArgumentNull(nameof(selector));
            return source.Provider.Execute<double>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.Average_Int64_TSource_2(typeof(TSource)),
                    source.Expression, Expression.Quote(selector)
                    ));
        }

        /// <summary>Computes the average of a sequence of nullable <see cref="long" /> values that is obtained by invoking a projection function on each element of the input sequence.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence of values to calculate the average of.</param>
        /// <param name="selector">A projection function to apply to each element.</param>
        /// <returns>The average of the sequence of values, or <see langword="null" /> if the <paramref name="source" /> sequence is empty or contains only <see langword="null" /> values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> or <paramref name="selector" /> is <see langword="null" />.</exception>
        /// <remarks><format type="text/markdown"><![CDATA[
        /// This method has at least one parameter of type <xref:System.Linq.Expressions.Expression%601> whose type argument is one of the <xref:System.Func%602> types. For these parameters, you can pass in a lambda expression and it will be compiled to an <xref:System.Linq.Expressions.Expression%601>.
        /// The <xref:System.Linq.Queryable.Average%60%601%28System.Linq.IQueryable%7B%60%600%7D%2CSystem.Linq.Expressions.Expression%7BSystem.Func%7B%60%600%2CSystem.Nullable%7BSystem.Int64%7D%7D%7D%29> method generates a <xref:System.Linq.Expressions.MethodCallExpression> that represents calling <xref:System.Linq.Queryable.Average%60%601%28System.Linq.IQueryable%7B%60%600%7D%2CSystem.Linq.Expressions.Expression%7BSystem.Func%7B%60%600%2CSystem.Nullable%7BSystem.Int64%7D%7D%7D%29> itself as a constructed generic method. It then passes the <xref:System.Linq.Expressions.MethodCallExpression> to the <xref:System.Linq.IQueryProvider.Execute%60%601%28System.Linq.Expressions.Expression%29> method of the <xref:System.Linq.IQueryProvider> represented by the <xref:System.Linq.IQueryable.Provider%2A> property of the `source` parameter.
        /// The query behavior that occurs as a result of executing an expression tree that represents calling <xref:System.Linq.Queryable.Average%60%601%28System.Linq.IQueryable%7B%60%600%7D%2CSystem.Linq.Expressions.Expression%7BSystem.Func%7B%60%600%2CSystem.Nullable%7BSystem.Int64%7D%7D%7D%29> depends on the implementation of the type of the `source` parameter. The expected behavior is that it calculates the average of the values in `source` after invoking `selector` on each value.
        /// ## Examples
        /// The following code example demonstrates how to use <xref:System.Linq.Queryable.Average%60%601%28System.Linq.IQueryable%7B%60%600%7D%2CSystem.Linq.Expressions.Expression%7BSystem.Func%7B%60%600%2CSystem.Int32%7D%7D%29> to calculate the average <xref:System.String> length in a sequence of values of type <xref:System.String>.
        /// [!INCLUDE[sqo_diff_overload_example_func](~/includes/sqo-diff-overload-example-func-md.md)]
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Queryable/CS/queryable.cs" interactive="try-dotnet-method" id="Snippet18":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Queryable/VB/queryable.vb" id="Snippet18":::
        /// ]]></format></remarks>
        [DynamicDependency("Average`1", typeof(Enumerable))]
        public static double? Average<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, long?>> selector)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (selector == null)
                throw Error.ArgumentNull(nameof(selector));
            return source.Provider.Execute<double?>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.Average_NullableInt64_TSource_2(typeof(TSource)),
                    source.Expression, Expression.Quote(selector)
                    ));
        }

        /// <summary>Computes the average of a sequence of <see cref="double" /> values that is obtained by invoking a projection function on each element of the input sequence.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence of values to calculate the average of.</param>
        /// <param name="selector">A projection function to apply to each element.</param>
        /// <returns>The average of the sequence of values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> or <paramref name="selector" /> is <see langword="null" />.</exception>
        /// <exception cref="System.InvalidOperationException"><paramref name="source" /> contains no elements.</exception>
        /// <remarks><format type="text/markdown"><![CDATA[
        /// This method has at least one parameter of type <xref:System.Linq.Expressions.Expression%601> whose type argument is one of the <xref:System.Func%602> types. For these parameters, you can pass in a lambda expression and it will be compiled to an <xref:System.Linq.Expressions.Expression%601>.
        /// The <xref:System.Linq.Queryable.Average%60%601%28System.Linq.IQueryable%7B%60%600%7D%2CSystem.Linq.Expressions.Expression%7BSystem.Func%7B%60%600%2CSystem.Double%7D%7D%29> method generates a <xref:System.Linq.Expressions.MethodCallExpression> that represents calling <xref:System.Linq.Queryable.Average%60%601%28System.Linq.IQueryable%7B%60%600%7D%2CSystem.Linq.Expressions.Expression%7BSystem.Func%7B%60%600%2CSystem.Double%7D%7D%29> itself as a constructed generic method. It then passes the <xref:System.Linq.Expressions.MethodCallExpression> to the <xref:System.Linq.IQueryProvider.Execute%60%601%28System.Linq.Expressions.Expression%29> method of the <xref:System.Linq.IQueryProvider> represented by the <xref:System.Linq.IQueryable.Provider%2A> property of the `source` parameter.
        /// The query behavior that occurs as a result of executing an expression tree that represents calling <xref:System.Linq.Queryable.Average%60%601%28System.Linq.IQueryable%7B%60%600%7D%2CSystem.Linq.Expressions.Expression%7BSystem.Func%7B%60%600%2CSystem.Double%7D%7D%29> depends on the implementation of the type of the `source` parameter. The expected behavior is that it calculates the average of the values in `source` after invoking `selector` on each value.
        /// ## Examples
        /// The following code example demonstrates how to use <xref:System.Linq.Queryable.Average%60%601%28System.Linq.IQueryable%7B%60%600%7D%2CSystem.Linq.Expressions.Expression%7BSystem.Func%7B%60%600%2CSystem.Int32%7D%7D%29> to calculate the average <xref:System.String> length in a sequence of values of type <xref:System.String>.
        /// [!INCLUDE[sqo_diff_overload_example_func](~/includes/sqo-diff-overload-example-func-md.md)]
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Queryable/CS/queryable.cs" interactive="try-dotnet-method" id="Snippet18":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Queryable/VB/queryable.vb" id="Snippet18":::
        /// ]]></format></remarks>
        [DynamicDependency("Average`1", typeof(Enumerable))]
        public static double Average<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, double>> selector)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (selector == null)
                throw Error.ArgumentNull(nameof(selector));
            return source.Provider.Execute<double>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.Average_Double_TSource_2(typeof(TSource)),
                    source.Expression, Expression.Quote(selector)
                    ));
        }

        /// <summary>Computes the average of a sequence of nullable <see cref="double" /> values that is obtained by invoking a projection function on each element of the input sequence.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence of values to calculate the average of.</param>
        /// <param name="selector">A projection function to apply to each element.</param>
        /// <returns>The average of the sequence of values, or <see langword="null" /> if the <paramref name="source" /> sequence is empty or contains only <see langword="null" /> values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> or <paramref name="selector" /> is <see langword="null" />.</exception>
        /// <remarks><format type="text/markdown"><![CDATA[
        /// This method has at least one parameter of type <xref:System.Linq.Expressions.Expression%601> whose type argument is one of the <xref:System.Func%602> types. For these parameters, you can pass in a lambda expression and it will be compiled to an <xref:System.Linq.Expressions.Expression%601>.
        /// The <xref:System.Linq.Queryable.Average%60%601%28System.Linq.IQueryable%7B%60%600%7D%2CSystem.Linq.Expressions.Expression%7BSystem.Func%7B%60%600%2CSystem.Nullable%7BSystem.Double%7D%7D%7D%29> method generates a <xref:System.Linq.Expressions.MethodCallExpression> that represents calling <xref:System.Linq.Queryable.Average%60%601%28System.Linq.IQueryable%7B%60%600%7D%2CSystem.Linq.Expressions.Expression%7BSystem.Func%7B%60%600%2CSystem.Nullable%7BSystem.Double%7D%7D%7D%29> itself as a constructed generic method. It then passes the <xref:System.Linq.Expressions.MethodCallExpression> to the <xref:System.Linq.IQueryProvider.Execute%60%601%28System.Linq.Expressions.Expression%29> method of the <xref:System.Linq.IQueryProvider> represented by the <xref:System.Linq.IQueryable.Provider%2A> property of the `source` parameter.
        /// The query behavior that occurs as a result of executing an expression tree that represents calling <xref:System.Linq.Queryable.Average%60%601%28System.Linq.IQueryable%7B%60%600%7D%2CSystem.Linq.Expressions.Expression%7BSystem.Func%7B%60%600%2CSystem.Nullable%7BSystem.Double%7D%7D%7D%29> depends on the implementation of the type of the `source` parameter. The expected behavior is that it calculates the average of the values in `source` after invoking `selector` on each value.
        /// ## Examples
        /// The following code example demonstrates how to use <xref:System.Linq.Queryable.Average%60%601%28System.Linq.IQueryable%7B%60%600%7D%2CSystem.Linq.Expressions.Expression%7BSystem.Func%7B%60%600%2CSystem.Int32%7D%7D%29> to calculate the average <xref:System.String> length in a sequence of values of type <xref:System.String>.
        /// [!INCLUDE[sqo_diff_overload_example_func](~/includes/sqo-diff-overload-example-func-md.md)]
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Queryable/CS/queryable.cs" interactive="try-dotnet-method" id="Snippet18":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Queryable/VB/queryable.vb" id="Snippet18":::
        /// ]]></format></remarks>
        [DynamicDependency("Average`1", typeof(Enumerable))]
        public static double? Average<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, double?>> selector)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (selector == null)
                throw Error.ArgumentNull(nameof(selector));
            return source.Provider.Execute<double?>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.Average_NullableDouble_TSource_2(typeof(TSource)),
                    source.Expression, Expression.Quote(selector)
                    ));
        }

        /// <summary>Computes the average of a sequence of <see cref="decimal" /> values that is obtained by invoking a projection function on each element of the input sequence.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence of values that are used to calculate an average.</param>
        /// <param name="selector">A projection function to apply to each element.</param>
        /// <returns>The average of the sequence of values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> or <paramref name="selector" /> is <see langword="null" />.</exception>
        /// <exception cref="System.InvalidOperationException"><paramref name="source" /> contains no elements.</exception>
        /// <remarks><format type="text/markdown"><![CDATA[
        /// This method has at least one parameter of type <xref:System.Linq.Expressions.Expression%601> whose type argument is one of the <xref:System.Func%602> types. For these parameters, you can pass in a lambda expression and it will be compiled to an <xref:System.Linq.Expressions.Expression%601>.
        /// The <xref:System.Linq.Queryable.Average%60%601%28System.Linq.IQueryable%7B%60%600%7D%2CSystem.Linq.Expressions.Expression%7BSystem.Func%7B%60%600%2CSystem.Decimal%7D%7D%29> method generates a <xref:System.Linq.Expressions.MethodCallExpression> that represents calling <xref:System.Linq.Queryable.Average%60%601%28System.Linq.IQueryable%7B%60%600%7D%2CSystem.Linq.Expressions.Expression%7BSystem.Func%7B%60%600%2CSystem.Decimal%7D%7D%29> itself as a constructed generic method. It then passes the <xref:System.Linq.Expressions.MethodCallExpression> to the <xref:System.Linq.IQueryProvider.Execute%60%601%28System.Linq.Expressions.Expression%29> method of the <xref:System.Linq.IQueryProvider> represented by the <xref:System.Linq.IQueryable.Provider%2A> property of the `source` parameter.
        /// The query behavior that occurs as a result of executing an expression tree that represents calling <xref:System.Linq.Queryable.Average%60%601%28System.Linq.IQueryable%7B%60%600%7D%2CSystem.Linq.Expressions.Expression%7BSystem.Func%7B%60%600%2CSystem.Decimal%7D%7D%29> depends on the implementation of the type of the `source` parameter. The expected behavior is that it calculates the average of the values in `source` after invoking `selector` on each value.
        /// ## Examples
        /// The following code example demonstrates how to use <xref:System.Linq.Queryable.Average%60%601%28System.Linq.IQueryable%7B%60%600%7D%2CSystem.Linq.Expressions.Expression%7BSystem.Func%7B%60%600%2CSystem.Int32%7D%7D%29> to calculate the average <xref:System.String> length in a sequence of values of type <xref:System.String>.
        /// [!INCLUDE[sqo_diff_overload_example_func](~/includes/sqo-diff-overload-example-func-md.md)]
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Queryable/CS/queryable.cs" interactive="try-dotnet-method" id="Snippet18":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Queryable/VB/queryable.vb" id="Snippet18":::
        /// ]]></format></remarks>
        [DynamicDependency("Average`1", typeof(Enumerable))]
        public static decimal Average<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, decimal>> selector)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (selector == null)
                throw Error.ArgumentNull(nameof(selector));
            return source.Provider.Execute<decimal>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.Average_Decimal_TSource_2(typeof(TSource)),
                    source.Expression, Expression.Quote(selector)
                    ));
        }

        /// <summary>Computes the average of a sequence of nullable <see cref="decimal" /> values that is obtained by invoking a projection function on each element of the input sequence.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence of values to calculate the average of.</param>
        /// <param name="selector">A projection function to apply to each element.</param>
        /// <returns>The average of the sequence of values, or <see langword="null" /> if the <paramref name="source" /> sequence is empty or contains only <see langword="null" /> values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> or <paramref name="selector" /> is <see langword="null" />.</exception>
        /// <remarks><format type="text/markdown"><![CDATA[
        /// This method has at least one parameter of type <xref:System.Linq.Expressions.Expression%601> whose type argument is one of the <xref:System.Func%602> types. For these parameters, you can pass in a lambda expression and it will be compiled to an <xref:System.Linq.Expressions.Expression%601>.
        /// The <xref:System.Linq.Queryable.Average%60%601%28System.Linq.IQueryable%7B%60%600%7D%2CSystem.Linq.Expressions.Expression%7BSystem.Func%7B%60%600%2CSystem.Nullable%7BSystem.Decimal%7D%7D%7D%29> method generates a <xref:System.Linq.Expressions.MethodCallExpression> that represents calling <xref:System.Linq.Queryable.Average%60%601%28System.Linq.IQueryable%7B%60%600%7D%2CSystem.Linq.Expressions.Expression%7BSystem.Func%7B%60%600%2CSystem.Nullable%7BSystem.Decimal%7D%7D%7D%29> itself as a constructed generic method. It then passes the <xref:System.Linq.Expressions.MethodCallExpression> to the <xref:System.Linq.IQueryProvider.Execute%60%601%28System.Linq.Expressions.Expression%29> method of the <xref:System.Linq.IQueryProvider> represented by the <xref:System.Linq.IQueryable.Provider%2A> property of the `source` parameter.
        /// The query behavior that occurs as a result of executing an expression tree that represents calling <xref:System.Linq.Queryable.Average%60%601%28System.Linq.IQueryable%7B%60%600%7D%2CSystem.Linq.Expressions.Expression%7BSystem.Func%7B%60%600%2CSystem.Nullable%7BSystem.Decimal%7D%7D%7D%29> depends on the implementation of the type of the `source` parameter. The expected behavior is that it calculates the average of the values in `source` after invoking `selector` on each value.
        /// ## Examples
        /// The following code example demonstrates how to use <xref:System.Linq.Queryable.Average%60%601%28System.Linq.IQueryable%7B%60%600%7D%2CSystem.Linq.Expressions.Expression%7BSystem.Func%7B%60%600%2CSystem.Int32%7D%7D%29> to calculate the average <xref:System.String> length in a sequence of values of type <xref:System.String>.
        /// [!INCLUDE[sqo_diff_overload_example_func](~/includes/sqo-diff-overload-example-func-md.md)]
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Queryable/CS/queryable.cs" interactive="try-dotnet-method" id="Snippet18":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Queryable/VB/queryable.vb" id="Snippet18":::
        /// ]]></format></remarks>
        [DynamicDependency("Average`1", typeof(Enumerable))]
        public static decimal? Average<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, decimal?>> selector)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (selector == null)
                throw Error.ArgumentNull(nameof(selector));
            return source.Provider.Execute<decimal?>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.Average_NullableDecimal_TSource_2(typeof(TSource)),
                    source.Expression, Expression.Quote(selector)
                    ));
        }

        /// <summary>Applies an accumulator function over a sequence.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence to aggregate over.</param>
        /// <param name="func">An accumulator function to apply to each element.</param>
        /// <returns>The final accumulator value.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> or <paramref name="func" /> is <see langword="null" />.</exception>
        /// <exception cref="System.InvalidOperationException"><paramref name="source" /> contains no elements.</exception>
        /// <remarks>
        /// <para>This method has at least one parameter of type <see cref="System.Linq.Expressions.Expression{T}" /> whose type argument is one of the <see cref="System.Func{T1,T2}" /> types. For these parameters, you can pass in a lambda expression and it will be compiled to an <see cref="System.Linq.Expressions.Expression{T}" />.</para>
        /// <para>The <see cref="System.Linq.Queryable.Aggregate{T}(System.Linq.IQueryable{T},System.Linq.Expressions.Expression{System.Func{T,T,T}})" /> method generates a <see cref="System.Linq.Expressions.MethodCallExpression" /> that represents calling <see cref="System.Linq.Queryable.Aggregate{T}(System.Linq.IQueryable{T},System.Linq.Expressions.Expression{System.Func{T,T,T}})" /> itself as a constructed generic method. It then passes the <see cref="System.Linq.Expressions.MethodCallExpression" /> to the <see cref="System.Linq.IQueryProvider.Execute{T}(System.Linq.Expressions.Expression)" /> method of the <see cref="System.Linq.IQueryProvider" /> represented by the <see cref="O:System.Linq.IQueryable.Provider" /> property of the <paramref name="source" /> parameter.</para>
        /// <para>The query behavior that occurs as a result of executing an expression tree that represents calling <see cref="System.Linq.Queryable.Aggregate{T}(System.Linq.IQueryable{T},System.Linq.Expressions.Expression{System.Func{T,T,T}})" /> depends on the implementation of the type of the <paramref name="source" /> parameter. The expected behavior is that the specified function, <paramref name="func" />, is applied to each value in the source sequence and the accumulated value is returned. The first value in <paramref name="source" /> is used as the seed value for the accumulated value, which corresponds to the first parameter in <paramref name="func" />.</para>
        /// <para>To simplify common aggregation operations, the set of standard query operators also includes two counting methods, <see cref="O:System.Linq.Queryable.Count" /> and <see cref="O:System.Linq.Queryable.LongCount" />, and four numeric aggregation methods, namely <see cref="O:System.Linq.Queryable.Max" />, <see cref="O:System.Linq.Queryable.Min" />, <see cref="O:System.Linq.Queryable.Sum" />, and <see cref="O:System.Linq.Queryable.Average" />.</para>
        /// </remarks>
        /// <example>The following code example demonstrates how to use <see cref="System.Linq.Queryable.Aggregate{T}(System.Linq.IQueryable{T},System.Linq.Expressions.Expression{System.Func{T,T,T}})" /> to build a sentence from an array of strings.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Queryable/CS/queryable.cs" interactive="try-dotnet-method" id="Snippet1":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Queryable/VB/queryable.vb" id="Snippet1":::</example>
        [DynamicDependency("Aggregate`1", typeof(Enumerable))]
        public static TSource Aggregate<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, TSource, TSource>> func)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (func == null)
                throw Error.ArgumentNull(nameof(func));
            return source.Provider.Execute<TSource>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.Aggregate_TSource_2(typeof(TSource)),
                    source.Expression, Expression.Quote(func)
                    ));
        }

        /// <summary>Applies an accumulator function over a sequence. The specified seed value is used as the initial accumulator value.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <typeparam name="TAccumulate">The type of the accumulator value.</typeparam>
        /// <param name="source">A sequence to aggregate over.</param>
        /// <param name="seed">The initial accumulator value.</param>
        /// <param name="func">An accumulator function to invoke on each element.</param>
        /// <returns>The final accumulator value.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> or <paramref name="func" /> is <see langword="null" />.</exception>
        /// <remarks>
        /// <para>This method has at least one parameter of type <see cref="System.Linq.Expressions.Expression{T}" /> whose type argument is one of the <see cref="System.Func{T1,T2}" /> types. For these parameters, you can pass in a lambda expression and it will be compiled to an <see cref="System.Linq.Expressions.Expression{T}" />.</para>
        /// <para>The <see cref="System.Linq.Queryable.Aggregate{T1,T2}(System.Linq.IQueryable{T1},T2,System.Linq.Expressions.Expression{System.Func{T2,T1,T2}})" /> method generates a <see cref="System.Linq.Expressions.MethodCallExpression" /> that represents calling <see cref="System.Linq.Queryable.Aggregate{T1,T2}(System.Linq.IQueryable{T1},T2,System.Linq.Expressions.Expression{System.Func{T2,T1,T2}})" /> itself as a constructed generic method. It then passes the <see cref="System.Linq.Expressions.MethodCallExpression" /> to the <see cref="System.Linq.IQueryProvider.Execute{T}(System.Linq.Expressions.Expression)" /> method of the <see cref="System.Linq.IQueryProvider" /> represented by the <see cref="O:System.Linq.IQueryable.Provider" /> property of the <paramref name="source" /> parameter.</para>
        /// <para>The query behavior that occurs as a result of executing an expression tree that represents calling <see cref="System.Linq.Queryable.Aggregate{T1,T2}(System.Linq.IQueryable{T1},T2,System.Linq.Expressions.Expression{System.Func{T2,T1,T2}})" /> depends on the implementation of the type of the <paramref name="source" /> parameter. The expected behavior is that the specified function, <paramref name="func" />, is applied to each value in the source sequence and the accumulated value is returned. The <paramref name="seed" /> parameter is used as the seed value for the accumulated value, which corresponds to the first parameter in <paramref name="func" />.</para>
        /// <para>To simplify common aggregation operations, the set of standard query operators also includes two counting methods, <see cref="O:System.Linq.Queryable.Count" /> and <see cref="O:System.Linq.Queryable.LongCount" />, and four numeric aggregation methods, namely <see cref="O:System.Linq.Queryable.Max" />, <see cref="O:System.Linq.Queryable.Min" />, <see cref="O:System.Linq.Queryable.Sum" />, and <see cref="O:System.Linq.Queryable.Average" />.</para>
        /// </remarks>
        /// <example>The following code example demonstrates how to use <see cref="System.Linq.Queryable.Aggregate{T1,T2}(System.Linq.IQueryable{T1},T2,System.Linq.Expressions.Expression{System.Func{T2,T1,T2}})" /> to apply an accumulator function when a seed value is provided to the function.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Queryable/CS/queryable.cs" interactive="try-dotnet-method" id="Snippet2":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Queryable/VB/queryable.vb" id="Snippet2":::</example>
        [DynamicDependency("Aggregate`2", typeof(Enumerable))]
        public static TAccumulate Aggregate<TSource, TAccumulate>(this IQueryable<TSource> source, TAccumulate seed, Expression<Func<TAccumulate, TSource, TAccumulate>> func)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (func == null)
                throw Error.ArgumentNull(nameof(func));
            return source.Provider.Execute<TAccumulate>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.Aggregate_TSource_TAccumulate_3(typeof(TSource), typeof(TAccumulate)),
                    source.Expression, Expression.Constant(seed), Expression.Quote(func)
                    ));
        }

        /// <summary>Applies an accumulator function over a sequence. The specified seed value is used as the initial accumulator value, and the specified function is used to select the result value.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <typeparam name="TAccumulate">The type of the accumulator value.</typeparam>
        /// <typeparam name="TResult">The type of the resulting value.</typeparam>
        /// <param name="source">A sequence to aggregate over.</param>
        /// <param name="seed">The initial accumulator value.</param>
        /// <param name="func">An accumulator function to invoke on each element.</param>
        /// <param name="selector">A function to transform the final accumulator value into the result value.</param>
        /// <returns>The transformed final accumulator value.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> or <paramref name="func" /> or <paramref name="selector" /> is <see langword="null" />.</exception>
        /// <remarks>
        /// <para>This method has at least one parameter of type <see cref="System.Linq.Expressions.Expression{T}" /> whose type argument is one of the <see cref="System.Func{T1,T2}" /> types. For these parameters, you can pass in a lambda expression and it will be compiled to an <see cref="System.Linq.Expressions.Expression{T}" />.</para>
        /// <para>The <see cref="System.Linq.Queryable.Aggregate{T1,T2,T3}(System.Linq.IQueryable{T1},T2,System.Linq.Expressions.Expression{System.Func{T2,T1,T2}},System.Linq.Expressions.Expression{System.Func{T2,T3}})" /> method generates a <see cref="System.Linq.Expressions.MethodCallExpression" /> that represents calling <see cref="System.Linq.Queryable.Aggregate{T1,T2,T3}(System.Linq.IQueryable{T1},T2,System.Linq.Expressions.Expression{System.Func{T2,T1,T2}},System.Linq.Expressions.Expression{System.Func{T2,T3}})" /> itself as a constructed generic method. It then passes the <see cref="System.Linq.Expressions.MethodCallExpression" /> to the <see cref="System.Linq.IQueryProvider.Execute{T}(System.Linq.Expressions.Expression)" /> method of the <see cref="System.Linq.IQueryProvider" /> represented by the <see cref="O:System.Linq.IQueryable.Provider" /> property of the <paramref name="source" /> parameter.</para>
        /// <para>The query behavior that occurs as a result of executing an expression tree that represents calling <see cref="System.Linq.Queryable.Aggregate{T1,T2,T3}(System.Linq.IQueryable{T1},T2,System.Linq.Expressions.Expression{System.Func{T2,T1,T2}},System.Linq.Expressions.Expression{System.Func{T2,T3}})" /> depends on the implementation of the type of the <paramref name="source" /> parameter. The expected behavior is that the specified function, <paramref name="func" />, is applied to each value in the source sequence and the accumulated value is returned. The <paramref name="seed" /> parameter is used as the seed value for the accumulated value, which corresponds to the first parameter in <paramref name="func" />. The final accumulated value is passed to <paramref name="selector" /> to obtain the result value.</para>
        /// <para>To simplify common aggregation operations, the set of standard query operators also includes two counting methods, <see cref="O:System.Linq.Queryable.Count" /> and <see cref="O:System.Linq.Queryable.LongCount" />, and four numeric aggregation methods, namely <see cref="O:System.Linq.Queryable.Max" />, <see cref="O:System.Linq.Queryable.Min" />, <see cref="O:System.Linq.Queryable.Sum" />, and <see cref="O:System.Linq.Queryable.Average" />.</para>
        /// </remarks>
        /// <example>The following code example demonstrates how to use <see cref="System.Linq.Queryable.Aggregate{T1,T2,T3}(System.Linq.IQueryable{T1},T2,System.Linq.Expressions.Expression{System.Func{T2,T1,T2}},System.Linq.Expressions.Expression{System.Func{T2,T3}})" /> to apply an accumulator function and a result selector.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Queryable/CS/queryable.cs" interactive="try-dotnet-method" id="Snippet3":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Queryable/VB/queryable.vb" id="Snippet3":::</example>
        [DynamicDependency("Aggregate`3", typeof(Enumerable))]
        public static TResult Aggregate<TSource, TAccumulate, TResult>(this IQueryable<TSource> source, TAccumulate seed, Expression<Func<TAccumulate, TSource, TAccumulate>> func, Expression<Func<TAccumulate, TResult>> selector)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (func == null)
                throw Error.ArgumentNull(nameof(func));
            if (selector == null)
                throw Error.ArgumentNull(nameof(selector));
            return source.Provider.Execute<TResult>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.Aggregate_TSource_TAccumulate_TResult_4(typeof(TSource), typeof(TAccumulate), typeof(TResult)), source.Expression, Expression.Constant(seed), Expression.Quote(func), Expression.Quote(selector)));
        }

        /// <summary>Returns a new queryable sequence that contains the elements from <paramref name="source" /> with the last <paramref name="count" /> elements of the source queryable sequence omitted.</summary>
        /// <typeparam name="TSource">The type of the elements in the queryable sequence.</typeparam>
        /// <param name="source">A queryable sequence.</param>
        /// <param name="count">The number of elements to omit from the end of the queryable sequence.</param>
        /// <returns>A new queryable sequence that contains the elements from <paramref name="source" /> minus <paramref name="count" /> elements from the end of the queryable sequence.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <remarks>If <paramref name="count" /> is not a positive number, this method returns an identical copy of the <paramref name="source" /> queryable sequence.</remarks>
        [DynamicDependency("SkipLast`1", typeof(Enumerable))]
        public static IQueryable<TSource> SkipLast<TSource>(this IQueryable<TSource> source, int count)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            return source.Provider.CreateQuery<TSource>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.SkipLast_TSource_2(typeof(TSource)),
                    source.Expression, Expression.Constant(count)
                    ));
        }

        /// <summary>Returns a new queryable sequence that contains the last <paramref name="count" /> elements from <paramref name="source" />.</summary>
        /// <typeparam name="TSource">The type of the elements in the queryable sequence.</typeparam>
        /// <param name="source">A queryable sequence instance.</param>
        /// <param name="count">The number of elements to take from the end of the queryable sequence.</param>
        /// <returns>A new queryable sequence that contains the last <paramref name="count" /> elements from <paramref name="source" />.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <remarks>If <paramref name="count" /> is not a positive number, this method returns an empty queryable sequence.</remarks>
        [DynamicDependency("TakeLast`1", typeof(Enumerable))]
        public static IQueryable<TSource> TakeLast<TSource>(this IQueryable<TSource> source, int count)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            return source.Provider.CreateQuery<TSource>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.TakeLast_TSource_2(typeof(TSource)),
                    source.Expression, Expression.Constant(count)
                    ));
        }

        /// <summary>Returns a new queryable sequence that contains the elements from <paramref name="source" /> plus the specified <paramref name="element" /> appended at the end.</summary>
        /// <typeparam name="TSource">The type of the elements in the queryable sequence.</typeparam>
        /// <param name="source">A queryable sequence.</param>
        /// <param name="element">An element of type <typeparamref name="TSource" /> to append to <paramref name="source" />.</param>
        /// <returns>A new queryable sequence that contains the elements from <paramref name="source" /> plus the specified <paramref name="element" /> appended at the end.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        [DynamicDependency("Append`1", typeof(Enumerable))]
        public static IQueryable<TSource> Append<TSource>(this IQueryable<TSource> source, TSource element)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            return source.Provider.CreateQuery<TSource>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.Append_TSource_2(typeof(TSource)),
                    source.Expression, Expression.Constant(element)
                    ));
        }

        /// <summary>Returns a new queryable sequence that contains the elements from <paramref name="source" /> plus the specified <paramref name="element" /> prepended at the beginning.</summary>
        /// <typeparam name="TSource">The type of the elements in the queryable sequence.</typeparam>
        /// <param name="source">A queryable sequence.</param>
        /// <param name="element">An element of type <typeparamref name="TSource" /> to prepend to <paramref name="source" />.</param>
        /// <returns>A new queryable sequence that contains the elements from <paramref name="source" /> plus the specified <paramref name="element" /> prepended at the beginning.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        [DynamicDependency("Prepend`1", typeof(Enumerable))]
        public static IQueryable<TSource> Prepend<TSource>(this IQueryable<TSource> source, TSource element)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            return source.Provider.CreateQuery<TSource>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.Prepend_TSource_2(typeof(TSource)),
                    source.Expression, Expression.Constant(element)
                    ));
        }
    }
}
