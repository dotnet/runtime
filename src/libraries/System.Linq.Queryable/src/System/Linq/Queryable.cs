// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

namespace System.Linq
{
    public static class Queryable
    {
        internal const string InMemoryQueryableExtensionMethodsRequiresUnreferencedCode = "Enumerating in-memory collections as IQueryable can require unreferenced code because expressions referencing IQueryable extension methods can get rebound to IEnumerable extension methods. The IEnumerable extension methods could be trimmed causing the application to fail at runtime.";
        internal const string InMemoryQueryableExtensionMethodsRequiresDynamicCode = "Enumerating in-memory collections as IQueryable can require creating new generic types or methods, which requires creating code at runtime. This may not work when AOT compiling.";

        [RequiresUnreferencedCode(InMemoryQueryableExtensionMethodsRequiresUnreferencedCode)]
        [RequiresDynamicCode(InMemoryQueryableExtensionMethodsRequiresDynamicCode)]
        public static IQueryable<TElement> AsQueryable<TElement>(this IEnumerable<TElement> source)
        {
            ArgumentNullException.ThrowIfNull(source);

            return source as IQueryable<TElement> ?? new EnumerableQuery<TElement>(source);
        }

        [RequiresUnreferencedCode(InMemoryQueryableExtensionMethodsRequiresUnreferencedCode)]
        [RequiresDynamicCode(InMemoryQueryableExtensionMethodsRequiresDynamicCode)]
        public static IQueryable AsQueryable(this IEnumerable source)
        {
            ArgumentNullException.ThrowIfNull(source);

            if (source is IQueryable queryable)
            {
                return queryable;
            }

            Type? enumType = TypeHelper.FindGenericType(typeof(IEnumerable<>), source.GetType());
            if (enumType == null)
            {
                throw Error.ArgumentNotIEnumerableGeneric(nameof(source));
            }

            return EnumerableQuery.Create(enumType.GenericTypeArguments[0], source);
        }

        [DynamicDependency("Where`1", typeof(Enumerable))]
        public static IQueryable<TSource> Where<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(predicate);

            return source.Provider.CreateQuery<TSource>(
                Expression.Call(
                    null,
                    new Func<IQueryable<TSource>, Expression<Func<TSource, bool>>, IQueryable<TSource>>(Where).Method,
                    source.Expression, Expression.Quote(predicate)));
        }

        [DynamicDependency("Where`1", typeof(Enumerable))]
        public static IQueryable<TSource> Where<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, int, bool>> predicate)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(predicate);

            return source.Provider.CreateQuery<TSource>(
                Expression.Call(
                    null,
                    new Func<IQueryable<TSource>, Expression<Func<TSource, int, bool>>, IQueryable<TSource>>(Where).Method,
                    source.Expression, Expression.Quote(predicate)));
        }

        [DynamicDependency("OfType`1", typeof(Enumerable))]
        public static IQueryable<TResult> OfType<TResult>(this IQueryable source)
        {
            ArgumentNullException.ThrowIfNull(source);

            return source.Provider.CreateQuery<TResult>(
                Expression.Call(
                    null,
                    new Func<IQueryable, IQueryable<TResult>>(OfType<TResult>).Method,
                    source.Expression));
        }

        [DynamicDependency("Cast`1", typeof(Enumerable))]
        public static IQueryable<TResult> Cast<TResult>(this IQueryable source)
        {
            ArgumentNullException.ThrowIfNull(source);

            return source.Provider.CreateQuery<TResult>(
                Expression.Call(
                    null,
                    new Func<IQueryable, IQueryable<TResult>>(Cast<TResult>).Method,
                    source.Expression));
        }

        [DynamicDependency("Select`2", typeof(Enumerable))]
        public static IQueryable<TResult> Select<TSource, TResult>(this IQueryable<TSource> source, Expression<Func<TSource, TResult>> selector)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(selector);

            return source.Provider.CreateQuery<TResult>(
                Expression.Call(
                    null,
                    new Func<IQueryable<TSource>, Expression<Func<TSource, TResult>>, IQueryable<TResult>>(Select).Method,
                    source.Expression, Expression.Quote(selector)));
        }

        [DynamicDependency("Select`2", typeof(Enumerable))]
        public static IQueryable<TResult> Select<TSource, TResult>(this IQueryable<TSource> source, Expression<Func<TSource, int, TResult>> selector)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(selector);

            return source.Provider.CreateQuery<TResult>(
                Expression.Call(
                    null,
                    new Func<IQueryable<TSource>, Expression<Func<TSource, int, TResult>>, IQueryable<TResult>>(Select).Method,
                    source.Expression, Expression.Quote(selector)));
        }

        [DynamicDependency("SelectMany`2", typeof(Enumerable))]
        public static IQueryable<TResult> SelectMany<TSource, TResult>(this IQueryable<TSource> source, Expression<Func<TSource, IEnumerable<TResult>>> selector)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(selector);

            return source.Provider.CreateQuery<TResult>(
                Expression.Call(
                    null,
                    new Func<IQueryable<TSource>, Expression<Func<TSource, IEnumerable<TResult>>>, IQueryable<TResult>>(SelectMany).Method,
                    source.Expression, Expression.Quote(selector)));
        }

        [DynamicDependency("SelectMany`2", typeof(Enumerable))]
        public static IQueryable<TResult> SelectMany<TSource, TResult>(this IQueryable<TSource> source, Expression<Func<TSource, int, IEnumerable<TResult>>> selector)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(selector);

            return source.Provider.CreateQuery<TResult>(
                Expression.Call(
                    null,
                    new Func<IQueryable<TSource>, Expression<Func<TSource, int, IEnumerable<TResult>>>, IQueryable<TResult>>(SelectMany).Method,
                    source.Expression, Expression.Quote(selector)));
        }

        [DynamicDependency("SelectMany`3", typeof(Enumerable))]
        public static IQueryable<TResult> SelectMany<TSource, TCollection, TResult>(this IQueryable<TSource> source, Expression<Func<TSource, int, IEnumerable<TCollection>>> collectionSelector, Expression<Func<TSource, TCollection, TResult>> resultSelector)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(collectionSelector);
            ArgumentNullException.ThrowIfNull(resultSelector);

            return source.Provider.CreateQuery<TResult>(
                Expression.Call(
                    null,
                    new Func<IQueryable<TSource>, Expression<Func<TSource, int, IEnumerable<TCollection>>>, Expression<Func<TSource, TCollection, TResult>>, IQueryable<TResult>>(SelectMany).Method,
                    source.Expression, Expression.Quote(collectionSelector), Expression.Quote(resultSelector)));
        }

        [DynamicDependency("SelectMany`3", typeof(Enumerable))]
        public static IQueryable<TResult> SelectMany<TSource, TCollection, TResult>(this IQueryable<TSource> source, Expression<Func<TSource, IEnumerable<TCollection>>> collectionSelector, Expression<Func<TSource, TCollection, TResult>> resultSelector)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(collectionSelector);
            ArgumentNullException.ThrowIfNull(resultSelector);

            return source.Provider.CreateQuery<TResult>(
                Expression.Call(
                    null,
                    new Func<IQueryable<TSource>, Expression<Func<TSource, IEnumerable<TCollection>>>, Expression<Func<TSource, TCollection, TResult>>, IQueryable<TResult>>(SelectMany).Method,
                    source.Expression, Expression.Quote(collectionSelector), Expression.Quote(resultSelector)));
        }

        private static Expression GetSourceExpression<TSource>(IEnumerable<TSource> source)
        {
            IQueryable<TSource>? q = source as IQueryable<TSource>;
            return q != null ? q.Expression : Expression.Constant(source, typeof(IEnumerable<TSource>));
        }

        [DynamicDependency("Join`4", typeof(Enumerable))]
        public static IQueryable<TResult> Join<TOuter, TInner, TKey, TResult>(this IQueryable<TOuter> outer, IEnumerable<TInner> inner, Expression<Func<TOuter, TKey>> outerKeySelector, Expression<Func<TInner, TKey>> innerKeySelector, Expression<Func<TOuter, TInner, TResult>> resultSelector)
        {
            ArgumentNullException.ThrowIfNull(outer);
            ArgumentNullException.ThrowIfNull(inner);
            ArgumentNullException.ThrowIfNull(outerKeySelector);
            ArgumentNullException.ThrowIfNull(innerKeySelector);
            ArgumentNullException.ThrowIfNull(resultSelector);

            return outer.Provider.CreateQuery<TResult>(
                Expression.Call(
                    null,
                    new Func<IQueryable<TOuter>, IEnumerable<TInner>, Expression<Func<TOuter, TKey>>, Expression<Func<TInner, TKey>>, Expression<Func<TOuter, TInner, TResult>>, IQueryable<TResult>>(Join).Method,
                    outer.Expression, GetSourceExpression(inner), Expression.Quote(outerKeySelector), Expression.Quote(innerKeySelector), Expression.Quote(resultSelector)));
        }

        [DynamicDependency("Join`4", typeof(Enumerable))]
        public static IQueryable<TResult> Join<TOuter, TInner, TKey, TResult>(this IQueryable<TOuter> outer, IEnumerable<TInner> inner, Expression<Func<TOuter, TKey>> outerKeySelector, Expression<Func<TInner, TKey>> innerKeySelector, Expression<Func<TOuter, TInner, TResult>> resultSelector, IEqualityComparer<TKey>? comparer)
        {
            ArgumentNullException.ThrowIfNull(outer);
            ArgumentNullException.ThrowIfNull(inner);
            ArgumentNullException.ThrowIfNull(outerKeySelector);
            ArgumentNullException.ThrowIfNull(innerKeySelector);
            ArgumentNullException.ThrowIfNull(resultSelector);

            return outer.Provider.CreateQuery<TResult>(
                Expression.Call(
                    null,
                    new Func<IQueryable<TOuter>, IEnumerable<TInner>, Expression<Func<TOuter, TKey>>, Expression<Func<TInner, TKey>>, Expression<Func<TOuter, TInner, TResult>>, IEqualityComparer<TKey>, IQueryable<TResult>>(Join).Method,
                    outer.Expression, GetSourceExpression(inner), Expression.Quote(outerKeySelector), Expression.Quote(innerKeySelector), Expression.Quote(resultSelector), Expression.Constant(comparer, typeof(IEqualityComparer<TKey>))));
        }

        [DynamicDependency("GroupJoin`4", typeof(Enumerable))]
        public static IQueryable<TResult> GroupJoin<TOuter, TInner, TKey, TResult>(this IQueryable<TOuter> outer, IEnumerable<TInner> inner, Expression<Func<TOuter, TKey>> outerKeySelector, Expression<Func<TInner, TKey>> innerKeySelector, Expression<Func<TOuter, IEnumerable<TInner>, TResult>> resultSelector)
        {
            ArgumentNullException.ThrowIfNull(outer);
            ArgumentNullException.ThrowIfNull(inner);
            ArgumentNullException.ThrowIfNull(outerKeySelector);
            ArgumentNullException.ThrowIfNull(innerKeySelector);
            ArgumentNullException.ThrowIfNull(resultSelector);

            return outer.Provider.CreateQuery<TResult>(
                Expression.Call(
                    null,
                    new Func<IQueryable<TOuter>, IEnumerable<TInner>, Expression<Func<TOuter, TKey>>, Expression<Func<TInner, TKey>>, Expression<Func<TOuter, IEnumerable<TInner>, TResult>>, IQueryable<TResult>>(GroupJoin).Method,
                    outer.Expression, GetSourceExpression(inner), Expression.Quote(outerKeySelector), Expression.Quote(innerKeySelector), Expression.Quote(resultSelector)));
        }

        [DynamicDependency("GroupJoin`4", typeof(Enumerable))]
        public static IQueryable<TResult> GroupJoin<TOuter, TInner, TKey, TResult>(this IQueryable<TOuter> outer, IEnumerable<TInner> inner, Expression<Func<TOuter, TKey>> outerKeySelector, Expression<Func<TInner, TKey>> innerKeySelector, Expression<Func<TOuter, IEnumerable<TInner>, TResult>> resultSelector, IEqualityComparer<TKey>? comparer)
        {
            ArgumentNullException.ThrowIfNull(outer);
            ArgumentNullException.ThrowIfNull(inner);
            ArgumentNullException.ThrowIfNull(outerKeySelector);
            ArgumentNullException.ThrowIfNull(innerKeySelector);
            ArgumentNullException.ThrowIfNull(resultSelector);

            return outer.Provider.CreateQuery<TResult>(
                Expression.Call(
                    null,
                    new Func<IQueryable<TOuter>, IEnumerable<TInner>, Expression<Func<TOuter, TKey>>, Expression<Func<TInner, TKey>>, Expression<Func<TOuter, IEnumerable<TInner>, TResult>>, IEqualityComparer<TKey>, IQueryable<TResult>>(GroupJoin).Method,
                    outer.Expression, GetSourceExpression(inner), Expression.Quote(outerKeySelector), Expression.Quote(innerKeySelector), Expression.Quote(resultSelector), Expression.Constant(comparer, typeof(IEqualityComparer<TKey>))));
        }

        /// <summary>
        /// Sorts the elements of a sequence in ascending order.
        /// </summary>
        /// <typeparam name="T">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">A sequence of values to order.</param>
        /// <returns>An <see cref="IOrderedEnumerable{TElement}"/> whose elements are sorted.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
        /// <remarks>
        /// This method has at least one parameter of type <see cref="Expression{TDelegate}"/> whose type argument is one
        /// of the <see cref="Func{T,TResult}"/> types.
        /// For these parameters, you can pass in a lambda expression and it will be compiled to an <see cref="Expression{TDelegate}"/>.
        ///
        /// The <see cref="Order{T}(IQueryable{T})"/> method generates a <see cref="MethodCallExpression"/> that represents
        /// calling <see cref="Enumerable.Order{T}(IEnumerable{T})"/> itself as a constructed generic method.
        /// It then passes the <see cref="MethodCallExpression"/> to the <see cref="IQueryProvider.CreateQuery{TElement}(Expression)"/> method
        /// of the <see cref="IQueryProvider"/> represented by the <see cref="IQueryable.Provider"/> property of the <paramref name="source"/>
        /// parameter. The result of calling <see cref="IQueryProvider.CreateQuery{TElement}(Expression)"/> is cast to
        /// type <see cref="IOrderedQueryable{T}"/> and returned.
        ///
        /// The query behavior that occurs as a result of executing an expression tree
        /// that represents calling <see cref="Enumerable.Order{T}(IEnumerable{T})"/>
        /// depends on the implementation of the <paramref name="source"/> parameter.
        /// The expected behavior is that it sorts the elements of <paramref name="source"/> by itself.
        /// </remarks>
        [DynamicDependency("Order`1", typeof(Enumerable))]
        public static IOrderedQueryable<T> Order<T>(this IQueryable<T> source)
        {
            ArgumentNullException.ThrowIfNull(source);

            return (IOrderedQueryable<T>)source.Provider.CreateQuery<T>(
                Expression.Call(
                    null,
                    new Func<IQueryable<T>, IOrderedQueryable<T>>(Order).Method,
                    source.Expression));
        }

        /// <summary>
        /// Sorts the elements of a sequence in ascending order.
        /// </summary>
        /// <typeparam name="T">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">A sequence of values to order.</param>
        /// <param name="comparer">An <see cref="IComparer{T}"/> to compare elements.</param>
        /// <returns>An <see cref="IOrderedEnumerable{TElement}"/> whose elements are sorted.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
        /// <remarks>
        /// This method has at least one parameter of type <see cref="Expression{TDelegate}"/> whose type argument is one
        /// of the <see cref="Func{T,TResult}"/> types.
        /// For these parameters, you can pass in a lambda expression and it will be compiled to an <see cref="Expression{TDelegate}"/>.
        ///
        /// The <see cref="Order{T}(IQueryable{T})"/> method generates a <see cref="MethodCallExpression"/> that represents
        /// calling <see cref="Enumerable.Order{T}(IEnumerable{T})"/> itself as a constructed generic method.
        /// It then passes the <see cref="MethodCallExpression"/> to the <see cref="IQueryProvider.CreateQuery{TElement}(Expression)"/> method
        /// of the <see cref="IQueryProvider"/> represented by the <see cref="IQueryable.Provider"/> property of the <paramref name="source"/>
        /// parameter. The result of calling <see cref="IQueryProvider.CreateQuery{TElement}(Expression)"/> is cast to
        /// type <see cref="IOrderedQueryable{T}"/> and returned.
        ///
        /// The query behavior that occurs as a result of executing an expression tree
        /// that represents calling <see cref="Enumerable.Order{T}(IEnumerable{T})"/>
        /// depends on the implementation of the <paramref name="source"/> parameter.
        /// The expected behavior is that it sorts the elements of <paramref name="source"/> by itself.
        /// </remarks>
        [DynamicDependency("Order`1", typeof(Enumerable))]
        public static IOrderedQueryable<T> Order<T>(this IQueryable<T> source, IComparer<T> comparer)
        {
            ArgumentNullException.ThrowIfNull(source);

            return (IOrderedQueryable<T>)source.Provider.CreateQuery<T>(
                Expression.Call(
                    null,
                    new Func<IQueryable<T>, IComparer<T>, IOrderedQueryable<T>>(Order).Method,
                    source.Expression, Expression.Constant(comparer, typeof(IComparer<T>))));
        }

        [DynamicDependency("OrderBy`2", typeof(Enumerable))]
        public static IOrderedQueryable<TSource> OrderBy<TSource, TKey>(this IQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(keySelector);

            return (IOrderedQueryable<TSource>)source.Provider.CreateQuery<TSource>(
                Expression.Call(
                    null,
                    new Func<IQueryable<TSource>, Expression<Func<TSource, TKey>>, IOrderedQueryable<TSource>>(OrderBy).Method,
                    source.Expression, Expression.Quote(keySelector)));
        }

        [DynamicDependency("OrderBy`2", typeof(Enumerable))]
        public static IOrderedQueryable<TSource> OrderBy<TSource, TKey>(this IQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector, IComparer<TKey>? comparer)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(keySelector);

            return (IOrderedQueryable<TSource>)source.Provider.CreateQuery<TSource>(
                Expression.Call(
                    null,
                    new Func<IQueryable<TSource>, Expression<Func<TSource, TKey>>, IComparer<TKey>, IOrderedQueryable<TSource>>(OrderBy).Method,
                    source.Expression, Expression.Quote(keySelector), Expression.Constant(comparer, typeof(IComparer<TKey>))));
        }

        /// <summary>
        /// Sorts the elements of a sequence in descending order.
        /// </summary>
        /// <typeparam name="T">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">A sequence of values to order.</param>
        /// <returns>An <see cref="IOrderedEnumerable{TElement}"/> whose elements are sorted.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
        /// <remarks>
        /// This method has at least one parameter of type <see cref="Expression{TDelegate}"/> whose type argument is one
        /// of the <see cref="Func{T,TResult}"/> types.
        /// For these parameters, you can pass in a lambda expression and it will be compiled to an <see cref="Expression{TDelegate}"/>.
        ///
        /// The <see cref="Order{T}(IQueryable{T})"/> method generates a <see cref="MethodCallExpression"/> that represents
        /// calling <see cref="Enumerable.Order{T}(IEnumerable{T})"/> itself as a constructed generic method.
        /// It then passes the <see cref="MethodCallExpression"/> to the <see cref="IQueryProvider.CreateQuery{TElement}(Expression)"/> method
        /// of the <see cref="IQueryProvider"/> represented by the <see cref="IQueryable.Provider"/> property of the <paramref name="source"/>
        /// parameter. The result of calling <see cref="IQueryProvider.CreateQuery{TElement}(Expression)"/> is cast to
        /// type <see cref="IOrderedQueryable{T}"/> and returned.
        ///
        /// The query behavior that occurs as a result of executing an expression tree
        /// that represents calling <see cref="Enumerable.Order{T}(IEnumerable{T})"/>
        /// depends on the implementation of the <paramref name="source"/> parameter.
        /// The expected behavior is that it sorts the elements of <paramref name="source"/> by itself.
        /// </remarks>
        [DynamicDependency("OrderDescending`1", typeof(Enumerable))]
        public static IOrderedQueryable<T> OrderDescending<T>(this IQueryable<T> source)
        {
            ArgumentNullException.ThrowIfNull(source);

            return (IOrderedQueryable<T>)source.Provider.CreateQuery<T>(
                Expression.Call(
                    null,
                    new Func<IQueryable<T>, IOrderedQueryable<T>>(OrderDescending).Method,
                    source.Expression));
        }

        /// <summary>
        /// Sorts the elements of a sequence in descending order.
        /// </summary>
        /// <typeparam name="T">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">A sequence of values to order.</param>
        /// <param name="comparer">An <see cref="IComparer{T}"/> to compare elements.</param>
        /// <returns>An <see cref="IOrderedEnumerable{TElement}"/> whose elements are sorted.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
        /// <remarks>
        /// This method has at least one parameter of type <see cref="Expression{TDelegate}"/> whose type argument is one
        /// of the <see cref="Func{T,TResult}"/> types.
        /// For these parameters, you can pass in a lambda expression and it will be compiled to an <see cref="Expression{TDelegate}"/>.
        ///
        /// The <see cref="Order{T}(IQueryable{T})"/> method generates a <see cref="MethodCallExpression"/> that represents
        /// calling <see cref="Enumerable.Order{T}(IEnumerable{T})"/> itself as a constructed generic method.
        /// It then passes the <see cref="MethodCallExpression"/> to the <see cref="IQueryProvider.CreateQuery{TElement}(Expression)"/> method
        /// of the <see cref="IQueryProvider"/> represented by the <see cref="IQueryable.Provider"/> property of the <paramref name="source"/>
        /// parameter. The result of calling <see cref="IQueryProvider.CreateQuery{TElement}(Expression)"/> is cast to
        /// type <see cref="IOrderedQueryable{T}"/> and returned.
        ///
        /// The query behavior that occurs as a result of executing an expression tree
        /// that represents calling <see cref="Enumerable.Order{T}(IEnumerable{T})"/>
        /// depends on the implementation of the <paramref name="source"/> parameter.
        /// The expected behavior is that it sorts the elements of <paramref name="source"/> by itself.
        /// </remarks>
        [DynamicDependency("OrderDescending`1", typeof(Enumerable))]
        public static IOrderedQueryable<T> OrderDescending<T>(this IQueryable<T> source, IComparer<T> comparer)
        {
            ArgumentNullException.ThrowIfNull(source);

            return (IOrderedQueryable<T>)source.Provider.CreateQuery<T>(
                Expression.Call(
                    null,
                    new Func<IQueryable<T>, IComparer<T>, IOrderedQueryable<T>>(OrderDescending).Method,
                    source.Expression, Expression.Constant(comparer, typeof(IComparer<T>))));
        }

        [DynamicDependency("OrderByDescending`2", typeof(Enumerable))]
        public static IOrderedQueryable<TSource> OrderByDescending<TSource, TKey>(this IQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(keySelector);

            return (IOrderedQueryable<TSource>)source.Provider.CreateQuery<TSource>(
                Expression.Call(
                    null,
                    new Func<IQueryable<TSource>, Expression<Func<TSource, TKey>>, IOrderedQueryable<TSource>>(OrderByDescending).Method,
                    source.Expression, Expression.Quote(keySelector)));
        }

        [DynamicDependency("OrderByDescending`2", typeof(Enumerable))]
        public static IOrderedQueryable<TSource> OrderByDescending<TSource, TKey>(this IQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector, IComparer<TKey>? comparer)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(keySelector);

            return (IOrderedQueryable<TSource>)source.Provider.CreateQuery<TSource>(
                Expression.Call(
                    null,
                    new Func<IQueryable<TSource>, Expression<Func<TSource, TKey>>, IComparer<TKey>, IOrderedQueryable<TSource>>(OrderByDescending).Method,
                    source.Expression, Expression.Quote(keySelector), Expression.Constant(comparer, typeof(IComparer<TKey>))));
        }

        [DynamicDependency("ThenBy`2", typeof(Enumerable))]
        public static IOrderedQueryable<TSource> ThenBy<TSource, TKey>(this IOrderedQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(keySelector);

            return (IOrderedQueryable<TSource>)source.Provider.CreateQuery<TSource>(
                Expression.Call(
                    null,
                    new Func<IOrderedQueryable<TSource>, Expression<Func<TSource, TKey>>, IOrderedQueryable<TSource>>(ThenBy).Method,
                    source.Expression, Expression.Quote(keySelector)));
        }

        [DynamicDependency("ThenBy`2", typeof(Enumerable))]
        public static IOrderedQueryable<TSource> ThenBy<TSource, TKey>(this IOrderedQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector, IComparer<TKey>? comparer)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(keySelector);

            return (IOrderedQueryable<TSource>)source.Provider.CreateQuery<TSource>(
                Expression.Call(
                    null,
                    new Func<IOrderedQueryable<TSource>, Expression<Func<TSource, TKey>>, IComparer<TKey>, IOrderedQueryable<TSource>>(ThenBy).Method,
                    source.Expression, Expression.Quote(keySelector), Expression.Constant(comparer, typeof(IComparer<TKey>))));
        }

        [DynamicDependency("ThenByDescending`2", typeof(Enumerable))]
        public static IOrderedQueryable<TSource> ThenByDescending<TSource, TKey>(this IOrderedQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(keySelector);

            return (IOrderedQueryable<TSource>)source.Provider.CreateQuery<TSource>(
                Expression.Call(
                    null,
                    new Func<IOrderedQueryable<TSource>, Expression<Func<TSource, TKey>>, IOrderedQueryable<TSource>>(ThenByDescending).Method,
                    source.Expression, Expression.Quote(keySelector)));
        }

        [DynamicDependency("ThenByDescending`2", typeof(Enumerable))]
        public static IOrderedQueryable<TSource> ThenByDescending<TSource, TKey>(this IOrderedQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector, IComparer<TKey>? comparer)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(keySelector);

            return (IOrderedQueryable<TSource>)source.Provider.CreateQuery<TSource>(
                Expression.Call(
                    null,
                    new Func<IOrderedQueryable<TSource>, Expression<Func<TSource, TKey>>, IComparer<TKey>, IOrderedQueryable<TSource>>(ThenByDescending).Method,
                    source.Expression, Expression.Quote(keySelector), Expression.Constant(comparer, typeof(IComparer<TKey>))));
        }

        [DynamicDependency("Take`1", typeof(Enumerable))]
        public static IQueryable<TSource> Take<TSource>(this IQueryable<TSource> source, int count)
        {
            ArgumentNullException.ThrowIfNull(source);

            return source.Provider.CreateQuery<TSource>(
                Expression.Call(
                    null,
                    new Func<IQueryable<TSource>, int, IQueryable<TSource>>(Take).Method,
                    source.Expression, Expression.Constant(count)));
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
            ArgumentNullException.ThrowIfNull(source);

            return source.Provider.CreateQuery<TSource>(
                Expression.Call(
                    null,
                    new Func<IQueryable<TSource>, Range, IQueryable<TSource>>(Take).Method,
                    source.Expression, Expression.Constant(range)));
        }

        [DynamicDependency("TakeWhile`1", typeof(Enumerable))]
        public static IQueryable<TSource> TakeWhile<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(predicate);

            return source.Provider.CreateQuery<TSource>(
                Expression.Call(
                    null,
                    new Func<IQueryable<TSource>, Expression<Func<TSource, bool>>, IQueryable<TSource>>(TakeWhile).Method,
                    source.Expression, Expression.Quote(predicate)));
        }

        [DynamicDependency("TakeWhile`1", typeof(Enumerable))]
        public static IQueryable<TSource> TakeWhile<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, int, bool>> predicate)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(predicate);

            return source.Provider.CreateQuery<TSource>(
                Expression.Call(
                    null,
                    new Func<IQueryable<TSource>, Expression<Func<TSource, int, bool>>, IQueryable<TSource>>(TakeWhile).Method,
                    source.Expression, Expression.Quote(predicate)));
        }

        [DynamicDependency("Skip`1", typeof(Enumerable))]
        public static IQueryable<TSource> Skip<TSource>(this IQueryable<TSource> source, int count)
        {
            ArgumentNullException.ThrowIfNull(source);

            return source.Provider.CreateQuery<TSource>(
                Expression.Call(
                    null,
                    new Func<IQueryable<TSource>, int, IQueryable<TSource>>(Skip).Method,
                    source.Expression, Expression.Constant(count)));
        }

        [DynamicDependency("SkipWhile`1", typeof(Enumerable))]
        public static IQueryable<TSource> SkipWhile<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(predicate);

            return source.Provider.CreateQuery<TSource>(
                Expression.Call(
                    null,
                    new Func<IQueryable<TSource>, Expression<Func<TSource, bool>>, IQueryable<TSource>>(SkipWhile).Method,
                    source.Expression, Expression.Quote(predicate)));
        }

        [DynamicDependency("SkipWhile`1", typeof(Enumerable))]
        public static IQueryable<TSource> SkipWhile<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, int, bool>> predicate)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(predicate);

            return source.Provider.CreateQuery<TSource>(
                Expression.Call(
                    null,
                    new Func<IQueryable<TSource>, Expression<Func<TSource, int, bool>>, IQueryable<TSource>>(SkipWhile).Method,
                    source.Expression, Expression.Quote(predicate)));
        }

        [DynamicDependency("GroupBy`2", typeof(Enumerable))]
        public static IQueryable<IGrouping<TKey, TSource>> GroupBy<TSource, TKey>(this IQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(keySelector);

            return source.Provider.CreateQuery<IGrouping<TKey, TSource>>(
                Expression.Call(
                    null,
                    new Func<IQueryable<TSource>, Expression<Func<TSource, TKey>>, IQueryable<IGrouping<TKey, TSource>>>(GroupBy).Method,
                    source.Expression, Expression.Quote(keySelector)));
        }

        [DynamicDependency("GroupBy`3", typeof(Enumerable))]
        public static IQueryable<IGrouping<TKey, TElement>> GroupBy<TSource, TKey, TElement>(this IQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector, Expression<Func<TSource, TElement>> elementSelector)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(keySelector);
            ArgumentNullException.ThrowIfNull(elementSelector);

            return source.Provider.CreateQuery<IGrouping<TKey, TElement>>(
                Expression.Call(
                    null,
                    new Func<IQueryable<TSource>, Expression<Func<TSource, TKey>>, Expression<Func<TSource, TElement>>, IQueryable<IGrouping<TKey, TElement>>>(GroupBy).Method,
                    source.Expression, Expression.Quote(keySelector), Expression.Quote(elementSelector)));
        }

        [DynamicDependency("GroupBy`2", typeof(Enumerable))]
        public static IQueryable<IGrouping<TKey, TSource>> GroupBy<TSource, TKey>(this IQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector, IEqualityComparer<TKey>? comparer)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(keySelector);

            return source.Provider.CreateQuery<IGrouping<TKey, TSource>>(
                Expression.Call(
                    null,
                    new Func<IQueryable<TSource>, Expression<Func<TSource, TKey>>, IEqualityComparer<TKey>, IQueryable<IGrouping<TKey, TSource>>>(GroupBy).Method,
                    source.Expression, Expression.Quote(keySelector), Expression.Constant(comparer, typeof(IEqualityComparer<TKey>))));
        }

        [DynamicDependency("GroupBy`3", typeof(Enumerable))]
        public static IQueryable<IGrouping<TKey, TElement>> GroupBy<TSource, TKey, TElement>(this IQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector, Expression<Func<TSource, TElement>> elementSelector, IEqualityComparer<TKey>? comparer)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(keySelector);
            ArgumentNullException.ThrowIfNull(elementSelector);

            return source.Provider.CreateQuery<IGrouping<TKey, TElement>>(
                Expression.Call(
                    null,
                    new Func<IQueryable<TSource>, Expression<Func<TSource, TKey>>, Expression<Func<TSource, TElement>>, IEqualityComparer<TKey>, IQueryable<IGrouping<TKey, TElement>>>(GroupBy).Method,
                    source.Expression, Expression.Quote(keySelector), Expression.Quote(elementSelector), Expression.Constant(comparer, typeof(IEqualityComparer<TKey>))));
        }

        [DynamicDependency("GroupBy`4", typeof(Enumerable))]
        public static IQueryable<TResult> GroupBy<TSource, TKey, TElement, TResult>(this IQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector, Expression<Func<TSource, TElement>> elementSelector, Expression<Func<TKey, IEnumerable<TElement>, TResult>> resultSelector)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(keySelector);
            ArgumentNullException.ThrowIfNull(elementSelector);
            ArgumentNullException.ThrowIfNull(resultSelector);

            return source.Provider.CreateQuery<TResult>(
                Expression.Call(
                    null,
                    new Func<IQueryable<TSource>, Expression<Func<TSource, TKey>>, Expression<Func<TSource, TElement>>, Expression<Func<TKey, IEnumerable<TElement>, TResult>>, IQueryable<TResult>>(GroupBy).Method,
                    source.Expression, Expression.Quote(keySelector), Expression.Quote(elementSelector), Expression.Quote(resultSelector)));
        }

        [DynamicDependency("GroupBy`3", typeof(Enumerable))]
        public static IQueryable<TResult> GroupBy<TSource, TKey, TResult>(this IQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector, Expression<Func<TKey, IEnumerable<TSource>, TResult>> resultSelector)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(keySelector);
            ArgumentNullException.ThrowIfNull(resultSelector);

            return source.Provider.CreateQuery<TResult>(
                Expression.Call(
                    null,
                    new Func<IQueryable<TSource>, Expression<Func<TSource, TKey>>, Expression<Func<TKey, IEnumerable<TSource>, TResult>>, IQueryable<TResult>>(GroupBy).Method,
                    source.Expression, Expression.Quote(keySelector), Expression.Quote(resultSelector)));
        }

        [DynamicDependency("GroupBy`3", typeof(Enumerable))]
        public static IQueryable<TResult> GroupBy<TSource, TKey, TResult>(this IQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector, Expression<Func<TKey, IEnumerable<TSource>, TResult>> resultSelector, IEqualityComparer<TKey>? comparer)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(keySelector);
            ArgumentNullException.ThrowIfNull(resultSelector);

            return source.Provider.CreateQuery<TResult>(
                Expression.Call(
                    null,
                    new Func<IQueryable<TSource>, Expression<Func<TSource, TKey>>, Expression<Func<TKey, IEnumerable<TSource>, TResult>>, IEqualityComparer<TKey>, IQueryable<TResult>>(GroupBy).Method,
                    source.Expression, Expression.Quote(keySelector), Expression.Quote(resultSelector), Expression.Constant(comparer, typeof(IEqualityComparer<TKey>))));
        }

        [DynamicDependency("GroupBy`4", typeof(Enumerable))]
        public static IQueryable<TResult> GroupBy<TSource, TKey, TElement, TResult>(this IQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector, Expression<Func<TSource, TElement>> elementSelector, Expression<Func<TKey, IEnumerable<TElement>, TResult>> resultSelector, IEqualityComparer<TKey>? comparer)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(keySelector);
            ArgumentNullException.ThrowIfNull(elementSelector);
            ArgumentNullException.ThrowIfNull(resultSelector);

            return source.Provider.CreateQuery<TResult>(
                Expression.Call(
                    null,
                    new Func<IQueryable<TSource>, Expression<Func<TSource, TKey>>, Expression<Func<TSource, TElement>>, Expression<Func<TKey, IEnumerable<TElement>, TResult>>, IEqualityComparer<TKey>, IQueryable<TResult>>(GroupBy).Method,
                    source.Expression, Expression.Quote(keySelector), Expression.Quote(elementSelector), Expression.Quote(resultSelector), Expression.Constant(comparer, typeof(IEqualityComparer<TKey>))));
        }

        [DynamicDependency("Distinct`1", typeof(Enumerable))]
        public static IQueryable<TSource> Distinct<TSource>(this IQueryable<TSource> source)
        {
            ArgumentNullException.ThrowIfNull(source);

            return source.Provider.CreateQuery<TSource>(
                Expression.Call(
                    null,
                    new Func<IQueryable<TSource>, IQueryable<TSource>>(Distinct).Method,
                    source.Expression));
        }

        [DynamicDependency("Distinct`1", typeof(Enumerable))]
        public static IQueryable<TSource> Distinct<TSource>(this IQueryable<TSource> source, IEqualityComparer<TSource>? comparer)
        {
            ArgumentNullException.ThrowIfNull(source);

            return source.Provider.CreateQuery<TSource>(
                Expression.Call(
                    null,
                    new Func<IQueryable<TSource>, IEqualityComparer<TSource>, IQueryable<TSource>>(Distinct).Method,
                    source.Expression, Expression.Constant(comparer, typeof(IEqualityComparer<TSource>))));
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
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(keySelector);

            return source.Provider.CreateQuery<TSource>(
                Expression.Call(
                    null,
                    new Func<IQueryable<TSource>, Expression<Func<TSource, TKey>>, IQueryable<TSource>>(DistinctBy).Method,
                    source.Expression, Expression.Quote(keySelector)));
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
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(keySelector);

            return source.Provider.CreateQuery<TSource>(
                Expression.Call(
                    null,
                    new Func<IQueryable<TSource>, Expression<Func<TSource, TKey>>, IEqualityComparer<TKey>, IQueryable<TSource>>(DistinctBy).Method,
                    source.Expression, Expression.Quote(keySelector), Expression.Constant(comparer, typeof(IEqualityComparer<TKey>))));
        }

        /// <summary>Split the elements of a sequence into chunks of size at most <paramref name="size"/>.</summary>
        /// <param name="source">An <see cref="IEnumerable{T}"/> whose elements to chunk.</param>
        /// <param name="size">Maximum size of each chunk.</param>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <returns>An <see cref="IQueryable{T}"/> that contains the elements the input sequence split into chunks of size <paramref name="size"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="size"/> is below 1.</exception>
        /// <remarks>
        /// <para>Every chunk except the last will be of size <paramref name="size"/>.</para>
        /// <para>The last chunk will contain the remaining elements and may be of a smaller size.</para>
        /// </remarks>
        [DynamicDependency("Chunk`1", typeof(Enumerable))]
        public static IQueryable<TSource[]> Chunk<TSource>(this IQueryable<TSource> source, int size)
        {
            ArgumentNullException.ThrowIfNull(source);

            return source.Provider.CreateQuery<TSource[]>(
                Expression.Call(
                    null,
                    new Func<IQueryable<TSource>, int, IQueryable<TSource[]>>(Chunk).Method,
                    source.Expression, Expression.Constant(size)));
        }

        [DynamicDependency("Concat`1", typeof(Enumerable))]
        public static IQueryable<TSource> Concat<TSource>(this IQueryable<TSource> source1, IEnumerable<TSource> source2)
        {
            ArgumentNullException.ThrowIfNull(source1);
            ArgumentNullException.ThrowIfNull(source2);

            return source1.Provider.CreateQuery<TSource>(
                Expression.Call(
                    null,
                    new Func<IQueryable<TSource>, IEnumerable<TSource>, IQueryable<TSource>>(Concat).Method,
                    source1.Expression, GetSourceExpression(source2)));
        }

        [DynamicDependency("Zip`2", typeof(Enumerable))]
        public static IQueryable<(TFirst First, TSecond Second)> Zip<TFirst, TSecond>(this IQueryable<TFirst> source1, IEnumerable<TSecond> source2)
        {
            ArgumentNullException.ThrowIfNull(source1);
            ArgumentNullException.ThrowIfNull(source2);

            return source1.Provider.CreateQuery<(TFirst, TSecond)>(
                Expression.Call(
                    null,
                    new Func<IQueryable<TFirst>, IEnumerable<TSecond>, IQueryable<(TFirst, TSecond)>>(Zip).Method,
                    source1.Expression, GetSourceExpression(source2)));
        }

        [DynamicDependency("Zip`3", typeof(Enumerable))]
        public static IQueryable<TResult> Zip<TFirst, TSecond, TResult>(this IQueryable<TFirst> source1, IEnumerable<TSecond> source2, Expression<Func<TFirst, TSecond, TResult>> resultSelector)
        {
            ArgumentNullException.ThrowIfNull(source1);
            ArgumentNullException.ThrowIfNull(source2);
            ArgumentNullException.ThrowIfNull(resultSelector);

            return source1.Provider.CreateQuery<TResult>(
                Expression.Call(
                    null,
                    new Func<IQueryable<TFirst>, IEnumerable<TSecond>, Expression<Func<TFirst, TSecond, TResult>>, IQueryable<TResult>>(Zip).Method,
                    source1.Expression, GetSourceExpression(source2), Expression.Quote(resultSelector)));
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
            ArgumentNullException.ThrowIfNull(source1);
            ArgumentNullException.ThrowIfNull(source2);
            ArgumentNullException.ThrowIfNull(source3);

            return source1.Provider.CreateQuery<(TFirst, TSecond, TThird)>(
                Expression.Call(
                    null,
                    new Func<IQueryable<TFirst>, IEnumerable<TSecond>, IEnumerable<TThird>, IQueryable<(TFirst, TSecond, TThird)>>(Zip).Method,
                    source1.Expression, GetSourceExpression(source2), GetSourceExpression(source3)));
        }

        [DynamicDependency("Union`1", typeof(Enumerable))]
        public static IQueryable<TSource> Union<TSource>(this IQueryable<TSource> source1, IEnumerable<TSource> source2)
        {
            ArgumentNullException.ThrowIfNull(source1);
            ArgumentNullException.ThrowIfNull(source2);

            return source1.Provider.CreateQuery<TSource>(
                Expression.Call(
                    null,
                    new Func<IQueryable<TSource>, IEnumerable<TSource>, IQueryable<TSource>>(Union).Method,
                    source1.Expression, GetSourceExpression(source2)));
        }

        [DynamicDependency("Union`1", typeof(Enumerable))]
        public static IQueryable<TSource> Union<TSource>(this IQueryable<TSource> source1, IEnumerable<TSource> source2, IEqualityComparer<TSource>? comparer)
        {
            ArgumentNullException.ThrowIfNull(source1);
            ArgumentNullException.ThrowIfNull(source2);

            return source1.Provider.CreateQuery<TSource>(
                Expression.Call(
                    null,
                    new Func<IQueryable<TSource>, IEnumerable<TSource>, IEqualityComparer<TSource>, IQueryable<TSource>>(Union).Method,
                    source1.Expression,
                    GetSourceExpression(source2),
                    Expression.Constant(comparer, typeof(IEqualityComparer<TSource>))));
        }

        /// <summary>Produces the set union of two sequences according to a specified key selector function.</summary>
        /// <typeparam name="TSource">The type of the elements of the input sequences.</typeparam>
        /// <typeparam name="TKey">The type of key to identify elements by.</typeparam>
        /// <param name="source1">An <see cref="IQueryable{T}" /> whose distinct elements form the first set for the union.</param>
        /// <param name="source2">An <see cref="IEnumerable{T}" /> whose distinct elements form the second set for the union.</param>
        /// <param name="keySelector">A function to extract the key for each element.</param>
        /// <returns>An <see cref="IQueryable{T}" /> that contains the elements from both input sequences, excluding duplicates.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source1" /> or <paramref name="source2" /> is <see langword="null" />.</exception>
        [DynamicDependency("UnionBy`2", typeof(Enumerable))]
        public static IQueryable<TSource> UnionBy<TSource, TKey>(this IQueryable<TSource> source1, IEnumerable<TSource> source2, Expression<Func<TSource, TKey>> keySelector)
        {
            ArgumentNullException.ThrowIfNull(source1);
            ArgumentNullException.ThrowIfNull(source2);
            ArgumentNullException.ThrowIfNull(keySelector);

            return source1.Provider.CreateQuery<TSource>(
                Expression.Call(
                    null,
                    new Func<IQueryable<TSource>, IEnumerable<TSource>, Expression<Func<TSource, TKey>>, IQueryable<TSource>>(UnionBy).Method,
                    source1.Expression, GetSourceExpression(source2), Expression.Quote(keySelector)));
        }

        /// <summary>Produces the set union of two sequences according to a specified key selector function.</summary>
        /// <typeparam name="TSource">The type of the elements of the input sequences.</typeparam>
        /// <typeparam name="TKey">The type of key to identify elements by.</typeparam>
        /// <param name="source1">An <see cref="IQueryable{T}" /> whose distinct elements form the first set for the union.</param>
        /// <param name="source2">An <see cref="IEnumerable{T}" /> whose distinct elements form the second set for the union.</param>
        /// <param name="keySelector">A function to extract the key for each element.</param>
        /// <param name="comparer">The <see cref="IEqualityComparer{T}" /> to compare values.</param>
        /// <returns>An <see cref="IQueryable{T}" /> that contains the elements from both input sequences, excluding duplicates.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source1" /> or <paramref name="source2" /> is <see langword="null" />.</exception>
        [DynamicDependency("UnionBy`2", typeof(Enumerable))]
        public static IQueryable<TSource> UnionBy<TSource, TKey>(this IQueryable<TSource> source1, IEnumerable<TSource> source2, Expression<Func<TSource, TKey>> keySelector, IEqualityComparer<TKey>? comparer)
        {
            ArgumentNullException.ThrowIfNull(source1);
            ArgumentNullException.ThrowIfNull(source2);
            ArgumentNullException.ThrowIfNull(keySelector);

            return source1.Provider.CreateQuery<TSource>(
                Expression.Call(
                    null,
                    new Func<IQueryable<TSource>, IEnumerable<TSource>, Expression<Func<TSource, TKey>>, IEqualityComparer<TKey>, IQueryable<TSource>>(UnionBy).Method,
                    source1.Expression,
                    GetSourceExpression(source2),
                    Expression.Quote(keySelector),
                    Expression.Constant(comparer, typeof(IEqualityComparer<TKey>))));
        }

        /// <summary>Return index and the associated item.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">An <see cref="IQueryable{T}" /> to return an element from.</param>
        /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        [DynamicDependency("Index`1", typeof(Enumerable))]
        public static IQueryable<(int Index, TSource Item)> Index<TSource>(this IQueryable<TSource> source)
        {
            ArgumentNullException.ThrowIfNull(source);

            return source.Provider.CreateQuery<(int Index, TSource Item)>(
                Expression.Call(
                    null,
                    new Func<IQueryable<TSource>, IQueryable<(int Index, TSource Item)>>(Index).Method,
                    source.Expression));
        }

        [DynamicDependency("Intersect`1", typeof(Enumerable))]
        public static IQueryable<TSource> Intersect<TSource>(this IQueryable<TSource> source1, IEnumerable<TSource> source2)
        {
            ArgumentNullException.ThrowIfNull(source1);
            ArgumentNullException.ThrowIfNull(source2);

            return source1.Provider.CreateQuery<TSource>(
                Expression.Call(
                    null,
                    new Func<IQueryable<TSource>, IEnumerable<TSource>, IQueryable<TSource>>(Intersect).Method,
                    source1.Expression, GetSourceExpression(source2)));
        }

        [DynamicDependency("Intersect`1", typeof(Enumerable))]
        public static IQueryable<TSource> Intersect<TSource>(this IQueryable<TSource> source1, IEnumerable<TSource> source2, IEqualityComparer<TSource>? comparer)
        {
            ArgumentNullException.ThrowIfNull(source1);
            ArgumentNullException.ThrowIfNull(source2);

            return source1.Provider.CreateQuery<TSource>(
                Expression.Call(
                    null,
                    new Func<IQueryable<TSource>, IEnumerable<TSource>, IEqualityComparer<TSource>, IQueryable<TSource>>(Intersect).Method,
                    source1.Expression,
                    GetSourceExpression(source2),
                    Expression.Constant(comparer, typeof(IEqualityComparer<TSource>))));
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
            ArgumentNullException.ThrowIfNull(source1);
            ArgumentNullException.ThrowIfNull(source2);
            ArgumentNullException.ThrowIfNull(keySelector);

            return source1.Provider.CreateQuery<TSource>(
                Expression.Call(
                    null,
                    new Func<IQueryable<TSource>, IEnumerable<TKey>, Expression<Func<TSource, TKey>>, IQueryable<TSource>>(IntersectBy).Method,
                    source1.Expression,
                    GetSourceExpression(source2),
                    Expression.Quote(keySelector)));
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
            ArgumentNullException.ThrowIfNull(source1);
            ArgumentNullException.ThrowIfNull(source2);
            ArgumentNullException.ThrowIfNull(keySelector);

            return source1.Provider.CreateQuery<TSource>(
                Expression.Call(
                    null,
                    new Func<IQueryable<TSource>, IEnumerable<TKey>, Expression<Func<TSource, TKey>>, IEqualityComparer<TKey>, IQueryable<TSource>>(IntersectBy).Method,
                    source1.Expression,
                    GetSourceExpression(source2),
                    Expression.Quote(keySelector),
                    Expression.Constant(comparer, typeof(IEqualityComparer<TKey>))));
        }

        [DynamicDependency("Except`1", typeof(Enumerable))]
        public static IQueryable<TSource> Except<TSource>(this IQueryable<TSource> source1, IEnumerable<TSource> source2)
        {
            ArgumentNullException.ThrowIfNull(source1);
            ArgumentNullException.ThrowIfNull(source2);

            return source1.Provider.CreateQuery<TSource>(
                Expression.Call(
                    null,
                    new Func<IQueryable<TSource>, IEnumerable<TSource>, IQueryable<TSource>>(Except).Method,
                    source1.Expression, GetSourceExpression(source2)));
        }

        [DynamicDependency("Except`1", typeof(Enumerable))]
        public static IQueryable<TSource> Except<TSource>(this IQueryable<TSource> source1, IEnumerable<TSource> source2, IEqualityComparer<TSource>? comparer)
        {
            ArgumentNullException.ThrowIfNull(source1);
            ArgumentNullException.ThrowIfNull(source2);

            return source1.Provider.CreateQuery<TSource>(
                Expression.Call(
                    null,
                    new Func<IQueryable<TSource>, IEnumerable<TSource>, IEqualityComparer<TSource>, IQueryable<TSource>>(Except).Method,
                    source1.Expression,
                    GetSourceExpression(source2),
                    Expression.Constant(comparer, typeof(IEqualityComparer<TSource>))));
        }

        /// <summary>
        /// Produces the set difference of two sequences according to a specified key selector function.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of the input sequence.</typeparam>
        /// <typeparam name="TKey">The type of key to identify elements by.</typeparam>
        /// <param name="source1">An <see cref="IQueryable{TSource}" /> whose keys that are not also in <paramref name="source2"/> will be returned.</param>
        /// <param name="source2">An <see cref="IEnumerable{TKey}" /> whose keys that also occur in the first sequence will cause those elements to be removed from the returned sequence.</param>
        /// <param name="keySelector">A function to extract the key for each element.</param>
        /// <returns>A <see cref="IQueryable{TSource}" /> that contains the set difference of the elements of two sequences.</returns>
        [DynamicDependency("ExceptBy`2", typeof(Enumerable))]
        public static IQueryable<TSource> ExceptBy<TSource, TKey>(this IQueryable<TSource> source1, IEnumerable<TKey> source2, Expression<Func<TSource, TKey>> keySelector)
        {
            ArgumentNullException.ThrowIfNull(source1);
            ArgumentNullException.ThrowIfNull(source2);
            ArgumentNullException.ThrowIfNull(keySelector);

            return source1.Provider.CreateQuery<TSource>(
                Expression.Call(
                    null,
                    new Func<IQueryable<TSource>, IEnumerable<TKey>, Expression<Func<TSource, TKey>>, IQueryable<TSource>>(ExceptBy).Method,
                    source1.Expression,
                    GetSourceExpression(source2),
                    Expression.Quote(keySelector)));
        }

        /// <summary>
        /// Produces the set difference of two sequences according to a specified key selector function.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of the input sequence.</typeparam>
        /// <typeparam name="TKey">The type of key to identify elements by.</typeparam>
        /// <param name="source1">An <see cref="IQueryable{TSource}" /> whose keys that are not also in <paramref name="source2"/> will be returned.</param>
        /// <param name="source2">An <see cref="IEnumerable{TKey}" /> whose keys that also occur in the first sequence will cause those elements to be removed from the returned sequence.</param>
        /// <param name="keySelector">A function to extract the key for each element.</param>
        /// <param name="comparer">An <see cref="IEqualityComparer{TKey}" /> to compare keys.</param>
        /// <returns>A <see cref="IQueryable{TSource}" /> that contains the set difference of the elements of two sequences.</returns>
        [DynamicDependency("ExceptBy`2", typeof(Enumerable))]
        public static IQueryable<TSource> ExceptBy<TSource, TKey>(this IQueryable<TSource> source1, IEnumerable<TKey> source2, Expression<Func<TSource, TKey>> keySelector, IEqualityComparer<TKey>? comparer)
        {
            ArgumentNullException.ThrowIfNull(source1);
            ArgumentNullException.ThrowIfNull(source2);
            ArgumentNullException.ThrowIfNull(keySelector);

            return source1.Provider.CreateQuery<TSource>(
                Expression.Call(
                    null,
                    new Func<IQueryable<TSource>, IEnumerable<TKey>, Expression<Func<TSource, TKey>>, IEqualityComparer<TKey>, IQueryable<TSource>>(ExceptBy).Method,
                    source1.Expression,
                    GetSourceExpression(source2),
                    Expression.Quote(keySelector),
                    Expression.Constant(comparer, typeof(IEqualityComparer<TKey>))));
        }

        [DynamicDependency("First`1", typeof(Enumerable))]
        public static TSource First<TSource>(this IQueryable<TSource> source)
        {
            ArgumentNullException.ThrowIfNull(source);

            return source.Provider.Execute<TSource>(
                Expression.Call(
                    null,
                    new Func<IQueryable<TSource>, TSource>(First).Method,
                    source.Expression));
        }

        [DynamicDependency("First`1", typeof(Enumerable))]
        public static TSource First<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(predicate);

            return source.Provider.Execute<TSource>(
                Expression.Call(
                    null,
                    new Func<IQueryable<TSource>, Expression<Func<TSource, bool>>, TSource>(First).Method,
                    source.Expression, Expression.Quote(predicate)));
        }

        [DynamicDependency("FirstOrDefault`1", typeof(Enumerable))]
        public static TSource? FirstOrDefault<TSource>(this IQueryable<TSource> source)
        {
            ArgumentNullException.ThrowIfNull(source);

            return source.Provider.Execute<TSource>(
                Expression.Call(
                    null,
                    new Func<IQueryable<TSource>, TSource?>(FirstOrDefault).Method,
                    source.Expression));
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
            ArgumentNullException.ThrowIfNull(source);

            return source.Provider.Execute<TSource>(
                Expression.Call(
                    null,
                    new Func<IQueryable<TSource>, TSource, TSource>(FirstOrDefault).Method,
                    source.Expression, Expression.Constant(defaultValue, typeof(TSource))));
        }

        [DynamicDependency("FirstOrDefault`1", typeof(Enumerable))]
        public static TSource? FirstOrDefault<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(predicate);

            return source.Provider.Execute<TSource>(
                Expression.Call(
                    null,
                    new Func<IQueryable<TSource>, Expression<Func<TSource, bool>>, TSource?>(FirstOrDefault).Method,
                    source.Expression, Expression.Quote(predicate)));
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
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(predicate);

            return source.Provider.Execute<TSource>(
                Expression.Call(
                    null,
                    new Func<IQueryable<TSource>, Expression<Func<TSource, bool>>, TSource, TSource>(FirstOrDefault).Method,
                    source.Expression, Expression.Quote(predicate), Expression.Constant(defaultValue, typeof(TSource))));
        }

        [DynamicDependency("Last`1", typeof(Enumerable))]
        public static TSource Last<TSource>(this IQueryable<TSource> source)
        {
            ArgumentNullException.ThrowIfNull(source);

            return source.Provider.Execute<TSource>(
                Expression.Call(
                    null,
                    new Func<IQueryable<TSource>, TSource>(Last).Method,
                    source.Expression));
        }

        [DynamicDependency("Last`1", typeof(Enumerable))]
        public static TSource Last<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(predicate);

            return source.Provider.Execute<TSource>(
                Expression.Call(
                    null,
                    new Func<IQueryable<TSource>, Expression<Func<TSource, bool>>, TSource>(Last).Method,
                    source.Expression, Expression.Quote(predicate)));
        }

        [DynamicDependency("LastOrDefault`1", typeof(Enumerable))]
        public static TSource? LastOrDefault<TSource>(this IQueryable<TSource> source)
        {
            ArgumentNullException.ThrowIfNull(source);

            return source.Provider.Execute<TSource>(
                Expression.Call(
                    null,
                    new Func<IQueryable<TSource>, TSource?>(LastOrDefault).Method,
                    source.Expression));
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
            ArgumentNullException.ThrowIfNull(source);

            return source.Provider.Execute<TSource>(
                Expression.Call(
                    null,
                    new Func<IQueryable<TSource>, TSource, TSource>(LastOrDefault).Method,
                    source.Expression, Expression.Constant(defaultValue, typeof(TSource))));
        }

        [DynamicDependency("LastOrDefault`1", typeof(Enumerable))]
        public static TSource? LastOrDefault<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(predicate);

            return source.Provider.Execute<TSource>(
                Expression.Call(
                    null,
                    new Func<IQueryable<TSource>, Expression<Func<TSource, bool>>, TSource?>(LastOrDefault).Method,
                    source.Expression, Expression.Quote(predicate)));
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
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(predicate);

            return source.Provider.Execute<TSource>(
                Expression.Call(
                    null,
                    new Func<IQueryable<TSource>, Expression<Func<TSource, bool>>, TSource, TSource>(LastOrDefault).Method,
                    source.Expression, Expression.Quote(predicate), Expression.Constant(defaultValue, typeof(TSource))
                ));
        }

        [DynamicDependency("Single`1", typeof(Enumerable))]
        public static TSource Single<TSource>(this IQueryable<TSource> source)
        {
            ArgumentNullException.ThrowIfNull(source);

            return source.Provider.Execute<TSource>(
                Expression.Call(
                    null,
                    new Func<IQueryable<TSource>, TSource>(Single).Method,
                    source.Expression));
        }

        [DynamicDependency("Single`1", typeof(Enumerable))]
        public static TSource Single<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(predicate);

            return source.Provider.Execute<TSource>(
                Expression.Call(
                    null,
                    new Func<IQueryable<TSource>, Expression<Func<TSource, bool>>, TSource>(Single).Method,
                    source.Expression, Expression.Quote(predicate)));
        }

        [DynamicDependency("SingleOrDefault`1", typeof(Enumerable))]
        public static TSource? SingleOrDefault<TSource>(this IQueryable<TSource> source)
        {
            ArgumentNullException.ThrowIfNull(source);

            return source.Provider.Execute<TSource>(
                Expression.Call(
                    null,
                    new Func<IQueryable<TSource>, TSource?>(SingleOrDefault).Method,
                    source.Expression));
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
            ArgumentNullException.ThrowIfNull(source);

            return source.Provider.Execute<TSource>(
                Expression.Call(
                    null,
                    new Func<IQueryable<TSource>, TSource, TSource>(SingleOrDefault).Method,
                    source.Expression, Expression.Constant(defaultValue, typeof(TSource))));
        }

        [DynamicDependency("SingleOrDefault`1", typeof(Enumerable))]
        public static TSource? SingleOrDefault<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(predicate);

            return source.Provider.Execute<TSource>(
                Expression.Call(
                    null,
                    new Func<IQueryable<TSource>, Expression<Func<TSource, bool>>, TSource?>(SingleOrDefault).Method,
                    source.Expression, Expression.Quote(predicate)));
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
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(predicate);

            return source.Provider.Execute<TSource>(
                Expression.Call(
                    null,
                    new Func<IQueryable<TSource>, Expression<Func<TSource, bool>>, TSource, TSource>(SingleOrDefault).Method,
                    source.Expression, Expression.Quote(predicate), Expression.Constant(defaultValue, typeof(TSource))));
        }

        [DynamicDependency("ElementAt`1", typeof(Enumerable))]
        public static TSource ElementAt<TSource>(this IQueryable<TSource> source, int index)
        {
            ArgumentNullException.ThrowIfNull(source);

            if (index < 0)
                throw Error.ArgumentOutOfRange(nameof(index));

            return source.Provider.Execute<TSource>(
                Expression.Call(
                    null,
                    new Func<IQueryable<TSource>, int, TSource>(ElementAt).Method,
                    source.Expression, Expression.Constant(index)));
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
            ArgumentNullException.ThrowIfNull(source);

            if (index.IsFromEnd && index.Value == 0)
                throw Error.ArgumentOutOfRange(nameof(index));

            return source.Provider.Execute<TSource>(
                Expression.Call(
                    null,
                    new Func<IQueryable<TSource>, Index, TSource>(ElementAt).Method,
                    source.Expression, Expression.Constant(index)));
        }

        [DynamicDependency("ElementAtOrDefault`1", typeof(Enumerable))]
        public static TSource? ElementAtOrDefault<TSource>(this IQueryable<TSource> source, int index)
        {
            ArgumentNullException.ThrowIfNull(source);

            return source.Provider.Execute<TSource>(
                Expression.Call(
                    null,
                    new Func<IQueryable<TSource>, int, TSource?>(ElementAtOrDefault).Method,
                    source.Expression, Expression.Constant(index)));
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
            ArgumentNullException.ThrowIfNull(source);

            return source.Provider.Execute<TSource>(
                Expression.Call(
                    null,
                    new Func<IQueryable<TSource>, Index, TSource?>(ElementAtOrDefault).Method,
                    source.Expression, Expression.Constant(index)));
        }

        [DynamicDependency("DefaultIfEmpty`1", typeof(Enumerable))]
        public static IQueryable<TSource?> DefaultIfEmpty<TSource>(this IQueryable<TSource> source)
        {
            ArgumentNullException.ThrowIfNull(source);

            return source.Provider.CreateQuery<TSource>(
                Expression.Call(
                    null,
                    new Func<IQueryable<TSource>, IQueryable<TSource?>>(DefaultIfEmpty).Method,
                    source.Expression));
        }

        [DynamicDependency("DefaultIfEmpty`1", typeof(Enumerable))]
        public static IQueryable<TSource> DefaultIfEmpty<TSource>(this IQueryable<TSource> source, TSource defaultValue)
        {
            ArgumentNullException.ThrowIfNull(source);

            return source.Provider.CreateQuery<TSource>(
                Expression.Call(
                    null,
                    new Func<IQueryable<TSource>, TSource, IQueryable<TSource?>>(DefaultIfEmpty).Method,
                    source.Expression, Expression.Constant(defaultValue, typeof(TSource))));
        }

        [DynamicDependency("Contains`1", typeof(Enumerable))]
        public static bool Contains<TSource>(this IQueryable<TSource> source, TSource item)
        {
            ArgumentNullException.ThrowIfNull(source);

            return source.Provider.Execute<bool>(
                Expression.Call(
                    null,
                    new Func<IQueryable<TSource>, TSource, bool>(Contains).Method,
                    source.Expression, Expression.Constant(item, typeof(TSource))));
        }

        [DynamicDependency("Contains`1", typeof(Enumerable))]
        public static bool Contains<TSource>(this IQueryable<TSource> source, TSource item, IEqualityComparer<TSource>? comparer)
        {
            ArgumentNullException.ThrowIfNull(source);

            return source.Provider.Execute<bool>(
                Expression.Call(
                    null,
                    new Func<IQueryable<TSource>, TSource, IEqualityComparer<TSource>, bool>(Contains).Method,
                    source.Expression, Expression.Constant(item, typeof(TSource)), Expression.Constant(comparer, typeof(IEqualityComparer<TSource>))));
        }

        [DynamicDependency("Reverse`1", typeof(Enumerable))]
        public static IQueryable<TSource> Reverse<TSource>(this IQueryable<TSource> source)
        {
            ArgumentNullException.ThrowIfNull(source);

            return source.Provider.CreateQuery<TSource>(
                Expression.Call(
                    null,
                    new Func<IQueryable<TSource>, IQueryable<TSource>>(Reverse).Method,
                    source.Expression));
        }

        [DynamicDependency("SequenceEqual`1", typeof(Enumerable))]
        public static bool SequenceEqual<TSource>(this IQueryable<TSource> source1, IEnumerable<TSource> source2)
        {
            ArgumentNullException.ThrowIfNull(source1);
            ArgumentNullException.ThrowIfNull(source2);

            return source1.Provider.Execute<bool>(
                Expression.Call(
                    null,
                    new Func<IQueryable<TSource>, IEnumerable<TSource>, bool>(SequenceEqual).Method,
                    source1.Expression, GetSourceExpression(source2)));
        }

        [DynamicDependency("SequenceEqual`1", typeof(Enumerable))]
        public static bool SequenceEqual<TSource>(this IQueryable<TSource> source1, IEnumerable<TSource> source2, IEqualityComparer<TSource>? comparer)
        {
            ArgumentNullException.ThrowIfNull(source1);
            ArgumentNullException.ThrowIfNull(source2);

            return source1.Provider.Execute<bool>(
                Expression.Call(
                    null,
                    new Func<IQueryable<TSource>, IEnumerable<TSource>, IEqualityComparer<TSource>, bool>(SequenceEqual).Method,
                    source1.Expression,
                    GetSourceExpression(source2),
                    Expression.Constant(comparer, typeof(IEqualityComparer<TSource>))));
        }

        [DynamicDependency("Any`1", typeof(Enumerable))]
        public static bool Any<TSource>(this IQueryable<TSource> source)
        {
            ArgumentNullException.ThrowIfNull(source);

            return source.Provider.Execute<bool>(
                Expression.Call(
                    null,
                    new Func<IQueryable<TSource>, bool>(Any).Method,
                    source.Expression));
        }

        [DynamicDependency("Any`1", typeof(Enumerable))]
        public static bool Any<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(predicate);

            return source.Provider.Execute<bool>(
                Expression.Call(
                    null,
                    new Func<IQueryable<TSource>, Expression<Func<TSource, bool>>, bool>(Any).Method,
                    source.Expression, Expression.Quote(predicate)));
        }

        [DynamicDependency("All`1", typeof(Enumerable))]
        public static bool All<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(predicate);

            return source.Provider.Execute<bool>(
                Expression.Call(
                    null,
                    new Func<IQueryable<TSource>, Expression<Func<TSource, bool>>, bool>(All).Method,
                    source.Expression, Expression.Quote(predicate)));
        }

        [DynamicDependency("Count`1", typeof(Enumerable))]
        public static int Count<TSource>(this IQueryable<TSource> source)
        {
            ArgumentNullException.ThrowIfNull(source);

            return source.Provider.Execute<int>(
                Expression.Call(
                    null,
                    new Func<IQueryable<TSource>, int>(Count).Method,
                    source.Expression));
        }

        [DynamicDependency("Count`1", typeof(Enumerable))]
        public static int Count<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(predicate);

            return source.Provider.Execute<int>(
                Expression.Call(
                    null,
                    new Func<IQueryable<TSource>, Expression<Func<TSource, bool>>, int>(Count).Method,
                    source.Expression, Expression.Quote(predicate)));
        }

        /// <summary>Returns the count of each element from a sequence according to a specified key selector function.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <typeparam name="TKey">The type of key to distinguish elements by.</typeparam>
        /// <param name="source">The sequence to count elements from.</param>
        /// <param name="keySelector">A function to extract the key for each element.</param>
        /// <param name="comparer">An <see cref="IEqualityComparer{TKey}" /> to compare keys.</param>
        /// <returns>An <see cref="IQueryable{T}" /> that contains count for each distinct elements from the source sequence as a <see cref="KeyValuePair{TKey, TValue}"/> object.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        [DynamicDependency("CountBy`2", typeof(Enumerable))]
        public static IQueryable<KeyValuePair<TKey, int>> CountBy<TSource, TKey>(this IQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector, IEqualityComparer<TKey>? comparer = null) where TKey : notnull
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(keySelector);

            return source.Provider.CreateQuery<KeyValuePair<TKey, int>>(
                Expression.Call(
                    null,
                    new Func<IQueryable<TSource>, Expression<Func<TSource, TKey>>, IEqualityComparer<TKey>, IQueryable<KeyValuePair<TKey, int>>>(CountBy).Method,
                    source.Expression, Expression.Quote(keySelector), Expression.Constant(comparer, typeof(IEqualityComparer<TKey>))));
        }

        [DynamicDependency("LongCount`1", typeof(Enumerable))]
        public static long LongCount<TSource>(this IQueryable<TSource> source)
        {
            ArgumentNullException.ThrowIfNull(source);

            return source.Provider.Execute<long>(
                Expression.Call(
                    null,
                    new Func<IQueryable<TSource>, long>(LongCount).Method,
                    source.Expression));
        }

        [DynamicDependency("LongCount`1", typeof(Enumerable))]
        public static long LongCount<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(predicate);

            return source.Provider.Execute<long>(
                Expression.Call(
                    null,
                    new Func<IQueryable<TSource>, Expression<Func<TSource, bool>>, long>(LongCount).Method,
                    source.Expression, Expression.Quote(predicate)));
        }

        [DynamicDependency("Min`1", typeof(Enumerable))]
        public static TSource? Min<TSource>(this IQueryable<TSource> source)
        {
            ArgumentNullException.ThrowIfNull(source);

            return source.Provider.Execute<TSource>(
                Expression.Call(
                    null,
                    new Func<IQueryable<TSource>, TSource?>(Min).Method,
                    source.Expression));
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
            ArgumentNullException.ThrowIfNull(source);

            return source.Provider.Execute<TSource>(
                Expression.Call(
                    null,
                    new Func<IQueryable<TSource>, IComparer<TSource>, TSource?>(Min).Method,
                    source.Expression,
                    Expression.Constant(comparer, typeof(IComparer<TSource>))));
        }

        [DynamicDependency("Min`2", typeof(Enumerable))]
        public static TResult? Min<TSource, TResult>(this IQueryable<TSource> source, Expression<Func<TSource, TResult>> selector)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(selector);

            return source.Provider.Execute<TResult>(
                Expression.Call(
                    null,
                    new Func<IQueryable<TSource>, Expression<Func<TSource, TResult>>, TResult?>(Min).Method,
                    source.Expression, Expression.Quote(selector)));
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
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(keySelector);

            return source.Provider.Execute<TSource>(
                Expression.Call(
                    null,
                    new Func<IQueryable<TSource>, Expression<Func<TSource, TKey>>, TSource?>(MinBy).Method,
                    source.Expression,
                    Expression.Quote(keySelector)));
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
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(keySelector);

            return source.Provider.Execute<TSource>(
                Expression.Call(
                    null,
                    new Func<IQueryable<TSource>, Expression<Func<TSource, TKey>>, IComparer<TSource>, TSource?>(MinBy).Method,
                    source.Expression,
                    Expression.Quote(keySelector),
                    Expression.Constant(comparer, typeof(IComparer<TSource>))));
        }

        [DynamicDependency("Max`1", typeof(Enumerable))]
        public static TSource? Max<TSource>(this IQueryable<TSource> source)
        {
            ArgumentNullException.ThrowIfNull(source);

            return source.Provider.Execute<TSource>(
                Expression.Call(
                    null,
                    new Func<IQueryable<TSource>, TSource?>(Max).Method,
                    source.Expression));
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
            ArgumentNullException.ThrowIfNull(source);

            return source.Provider.Execute<TSource>(
                Expression.Call(
                    null,
                    new Func<IQueryable<TSource>, IComparer<TSource>, TSource?>(Max).Method,
                    source.Expression,
                    Expression.Constant(comparer, typeof(IComparer<TSource>))));
        }

        [DynamicDependency("Max`2", typeof(Enumerable))]
        public static TResult? Max<TSource, TResult>(this IQueryable<TSource> source, Expression<Func<TSource, TResult>> selector)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(selector);

            return source.Provider.Execute<TResult>(
                Expression.Call(
                    null,
                    new Func<IQueryable<TSource>, Expression<Func<TSource, TResult>>, TResult?>(Max).Method,
                    source.Expression, Expression.Quote(selector)));
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
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(keySelector);

            return source.Provider.Execute<TSource>(
                Expression.Call(
                    null,
                    new Func<IQueryable<TSource>, Expression<Func<TSource, TKey>>, TSource?>(MaxBy).Method,
                    source.Expression,
                    Expression.Quote(keySelector)));
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
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(keySelector);

            return source.Provider.Execute<TSource>(
                Expression.Call(
                    null,
                    new Func<IQueryable<TSource>, Expression<Func<TSource, TKey>>, IComparer<TSource>, TSource?>(MaxBy).Method,
                    source.Expression,
                    Expression.Quote(keySelector),
                    Expression.Constant(comparer, typeof(IComparer<TSource>))));
        }

        [DynamicDependency("Sum", typeof(Enumerable))]
        public static int Sum(this IQueryable<int> source)
        {
            ArgumentNullException.ThrowIfNull(source);

            return source.Provider.Execute<int>(
                Expression.Call(
                    null,
                    new Func<IQueryable<int>, int>(Sum).Method,
                    source.Expression));
        }

        [DynamicDependency("Sum", typeof(Enumerable))]
        public static int? Sum(this IQueryable<int?> source)
        {
            ArgumentNullException.ThrowIfNull(source);

            return source.Provider.Execute<int?>(
                Expression.Call(
                    null,
                    new Func<IQueryable<int?>, int?>(Sum).Method,
                    source.Expression));
        }

        [DynamicDependency("Sum", typeof(Enumerable))]
        public static long Sum(this IQueryable<long> source)
        {
            ArgumentNullException.ThrowIfNull(source);

            return source.Provider.Execute<long>(
                Expression.Call(
                    null,
                    new Func<IQueryable<long>, long>(Sum).Method,
                    source.Expression));
        }

        [DynamicDependency("Sum", typeof(Enumerable))]
        public static long? Sum(this IQueryable<long?> source)
        {
            ArgumentNullException.ThrowIfNull(source);

            return source.Provider.Execute<long?>(
                Expression.Call(
                    null,
                    new Func<IQueryable<long?>, long?>(Sum).Method,
                    source.Expression));
        }

        [DynamicDependency("Sum", typeof(Enumerable))]
        public static float Sum(this IQueryable<float> source)
        {
            ArgumentNullException.ThrowIfNull(source);

            return source.Provider.Execute<float>(
                Expression.Call(
                    null,
                    new Func<IQueryable<float>, float>(Sum).Method,
                    source.Expression));
        }

        [DynamicDependency("Sum", typeof(Enumerable))]
        public static float? Sum(this IQueryable<float?> source)
        {
            ArgumentNullException.ThrowIfNull(source);

            return source.Provider.Execute<float?>(
                Expression.Call(
                    null,
                    new Func<IQueryable<float?>, float?>(Sum).Method,
                    source.Expression));
        }

        [DynamicDependency("Sum", typeof(Enumerable))]
        public static double Sum(this IQueryable<double> source)
        {
            ArgumentNullException.ThrowIfNull(source);

            return source.Provider.Execute<double>(
                Expression.Call(
                    null,
                    new Func<IQueryable<double>, double>(Sum).Method,
                    source.Expression));
        }

        [DynamicDependency("Sum", typeof(Enumerable))]
        public static double? Sum(this IQueryable<double?> source)
        {
            ArgumentNullException.ThrowIfNull(source);

            return source.Provider.Execute<double?>(
                Expression.Call(
                    null,
                    new Func<IQueryable<double?>, double?>(Sum).Method,
                    source.Expression));
        }

        [DynamicDependency("Sum", typeof(Enumerable))]
        public static decimal Sum(this IQueryable<decimal> source)
        {
            ArgumentNullException.ThrowIfNull(source);

            return source.Provider.Execute<decimal>(
                Expression.Call(
                    null,
                    new Func<IQueryable<decimal>, decimal>(Sum).Method,
                    source.Expression));
        }

        [DynamicDependency("Sum", typeof(Enumerable))]
        public static decimal? Sum(this IQueryable<decimal?> source)
        {
            ArgumentNullException.ThrowIfNull(source);

            return source.Provider.Execute<decimal?>(
                Expression.Call(
                    null,
                    new Func<IQueryable<decimal?>, decimal?>(Sum).Method,
                    source.Expression));
        }

        [DynamicDependency("Sum`1", typeof(Enumerable))]
        public static int Sum<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, int>> selector)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(selector);

            return source.Provider.Execute<int>(
                Expression.Call(
                    null,
                    new Func<IQueryable<TSource>, Expression<Func<TSource, int>>, int>(Sum).Method,
                    source.Expression, Expression.Quote(selector)));
        }

        [DynamicDependency("Sum`1", typeof(Enumerable))]
        public static int? Sum<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, int?>> selector)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(selector);

            return source.Provider.Execute<int?>(
                Expression.Call(
                    null,
                    new Func<IQueryable<TSource>, Expression<Func<TSource, int?>>, int?>(Sum).Method,
                    source.Expression, Expression.Quote(selector)));
        }

        [DynamicDependency("Sum`1", typeof(Enumerable))]
        public static long Sum<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, long>> selector)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(selector);

            return source.Provider.Execute<long>(
                Expression.Call(
                    null,
                    new Func<IQueryable<TSource>, Expression<Func<TSource, long>>, long>(Sum).Method,
                    source.Expression, Expression.Quote(selector)));
        }

        [DynamicDependency("Sum`1", typeof(Enumerable))]
        public static long? Sum<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, long?>> selector)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(selector);

            return source.Provider.Execute<long?>(
                Expression.Call(
                    null,
                    new Func<IQueryable<TSource>, Expression<Func<TSource, long?>>, long?>(Sum).Method,
                    source.Expression, Expression.Quote(selector)));
        }

        [DynamicDependency("Sum`1", typeof(Enumerable))]
        public static float Sum<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, float>> selector)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(selector);

            return source.Provider.Execute<float>(
                Expression.Call(
                    null,
                    new Func<IQueryable<TSource>, Expression<Func<TSource, float>>, float>(Sum).Method,
                    source.Expression, Expression.Quote(selector)));
        }

        [DynamicDependency("Sum`1", typeof(Enumerable))]
        public static float? Sum<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, float?>> selector)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(selector);

            return source.Provider.Execute<float?>(
                Expression.Call(
                    null,
                    new Func<IQueryable<TSource>, Expression<Func<TSource, float?>>, float?>(Sum).Method,
                    source.Expression, Expression.Quote(selector)));
        }

        [DynamicDependency("Sum`1", typeof(Enumerable))]
        public static double Sum<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, double>> selector)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(selector);

            return source.Provider.Execute<double>(
                Expression.Call(
                    null,
                    new Func<IQueryable<TSource>, Expression<Func<TSource, double>>, double>(Sum).Method,
                    source.Expression, Expression.Quote(selector)));
        }

        [DynamicDependency("Sum`1", typeof(Enumerable))]
        public static double? Sum<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, double?>> selector)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(selector);

            return source.Provider.Execute<double?>(
                Expression.Call(
                    null,
                    new Func<IQueryable<TSource>, Expression<Func<TSource, double?>>, double?>(Sum).Method,
                    source.Expression, Expression.Quote(selector)));
        }

        [DynamicDependency("Sum`1", typeof(Enumerable))]
        public static decimal Sum<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, decimal>> selector)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(selector);

            return source.Provider.Execute<decimal>(
                Expression.Call(
                    null,
                    new Func<IQueryable<TSource>, Expression<Func<TSource, decimal>>, decimal>(Sum).Method,
                    source.Expression, Expression.Quote(selector)));
        }

        [DynamicDependency("Sum`1", typeof(Enumerable))]
        public static decimal? Sum<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, decimal?>> selector)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(selector);

            return source.Provider.Execute<decimal?>(
                Expression.Call(
                    null,
                    new Func<IQueryable<TSource>, Expression<Func<TSource, decimal?>>, decimal?>(Sum).Method,
                    source.Expression, Expression.Quote(selector)));
        }

        [DynamicDependency("Average", typeof(Enumerable))]
        public static double Average(this IQueryable<int> source)
        {
            ArgumentNullException.ThrowIfNull(source);

            return source.Provider.Execute<double>(
                Expression.Call(
                    null,
                    new Func<IQueryable<int>, double>(Average).Method,
                    source.Expression));
        }

        [DynamicDependency("Average", typeof(Enumerable))]
        public static double? Average(this IQueryable<int?> source)
        {
            ArgumentNullException.ThrowIfNull(source);

            return source.Provider.Execute<double?>(
                Expression.Call(
                    null,
                    new Func<IQueryable<int?>, double?>(Average).Method,
                    source.Expression));
        }

        [DynamicDependency("Average", typeof(Enumerable))]
        public static double Average(this IQueryable<long> source)
        {
            ArgumentNullException.ThrowIfNull(source);

            return source.Provider.Execute<double>(
                Expression.Call(
                    null,
                    new Func<IQueryable<long>, double>(Average).Method,
                    source.Expression));
        }

        [DynamicDependency("Average", typeof(Enumerable))]
        public static double? Average(this IQueryable<long?> source)
        {
            ArgumentNullException.ThrowIfNull(source);

            return source.Provider.Execute<double?>(
                Expression.Call(
                    null,
                    new Func<IQueryable<long?>, double?>(Average).Method,
                    source.Expression));
        }

        [DynamicDependency("Average", typeof(Enumerable))]
        public static float Average(this IQueryable<float> source)
        {
            ArgumentNullException.ThrowIfNull(source);

            return source.Provider.Execute<float>(
                Expression.Call(
                    null,
                    new Func<IQueryable<float>, float>(Average).Method,
                    source.Expression));
        }

        [DynamicDependency("Average", typeof(Enumerable))]
        public static float? Average(this IQueryable<float?> source)
        {
            ArgumentNullException.ThrowIfNull(source);

            return source.Provider.Execute<float?>(
                Expression.Call(
                    null,
                    new Func<IQueryable<float?>, float?>(Average).Method,
                    source.Expression));
        }

        [DynamicDependency("Average", typeof(Enumerable))]
        public static double Average(this IQueryable<double> source)
        {
            ArgumentNullException.ThrowIfNull(source);

            return source.Provider.Execute<double>(
                Expression.Call(
                    null,
                    new Func<IQueryable<double>, double>(Average).Method,
                    source.Expression));
        }

        [DynamicDependency("Average", typeof(Enumerable))]
        public static double? Average(this IQueryable<double?> source)
        {
            ArgumentNullException.ThrowIfNull(source);

            return source.Provider.Execute<double?>(
                Expression.Call(
                    null,
                    new Func<IQueryable<double?>, double?>(Average).Method,
                    source.Expression));
        }

        [DynamicDependency("Average", typeof(Enumerable))]
        public static decimal Average(this IQueryable<decimal> source)
        {
            ArgumentNullException.ThrowIfNull(source);

            return source.Provider.Execute<decimal>(
                Expression.Call(
                    null,
                    new Func<IQueryable<decimal>, decimal>(Average).Method,
                    source.Expression));
        }

        [DynamicDependency("Average", typeof(Enumerable))]
        public static decimal? Average(this IQueryable<decimal?> source)
        {
            ArgumentNullException.ThrowIfNull(source);

            return source.Provider.Execute<decimal?>(
                Expression.Call(
                    null,
                    new Func<IQueryable<decimal?>, decimal?>(Average).Method,
                    source.Expression));
        }

        [DynamicDependency("Average`1", typeof(Enumerable))]
        public static double Average<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, int>> selector)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(selector);

            return source.Provider.Execute<double>(
                Expression.Call(
                    null,
                    new Func<IQueryable<TSource>, Expression<Func<TSource, int>>, double>(Average).Method,
                    source.Expression, Expression.Quote(selector)));
        }

        [DynamicDependency("Average`1", typeof(Enumerable))]
        public static double? Average<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, int?>> selector)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(selector);

            return source.Provider.Execute<double?>(
                Expression.Call(
                    null,
                    new Func<IQueryable<TSource>, Expression<Func<TSource, int?>>, double?>(Average).Method,
                    source.Expression, Expression.Quote(selector)));
        }

        [DynamicDependency("Average`1", typeof(Enumerable))]
        public static float Average<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, float>> selector)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(selector);

            return source.Provider.Execute<float>(
                Expression.Call(
                    null,
                    new Func<IQueryable<TSource>, Expression<Func<TSource, float>>, float>(Average).Method,
                    source.Expression, Expression.Quote(selector)));
        }

        [DynamicDependency("Average`1", typeof(Enumerable))]
        public static float? Average<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, float?>> selector)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(selector);

            return source.Provider.Execute<float?>(
                Expression.Call(
                    null,
                    new Func<IQueryable<TSource>, Expression<Func<TSource, float?>>, float?>(Average).Method,
                    source.Expression, Expression.Quote(selector)));
        }

        [DynamicDependency("Average`1", typeof(Enumerable))]
        public static double Average<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, long>> selector)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(selector);

            return source.Provider.Execute<double>(
                Expression.Call(
                    null,
                    new Func<IQueryable<TSource>, Expression<Func<TSource, long>>, double>(Average).Method,
                    source.Expression, Expression.Quote(selector)));
        }

        [DynamicDependency("Average`1", typeof(Enumerable))]
        public static double? Average<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, long?>> selector)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(selector);

            return source.Provider.Execute<double?>(
                Expression.Call(
                    null,
                    new Func<IQueryable<TSource>, Expression<Func<TSource, long?>>, double?>(Average).Method,
                    source.Expression, Expression.Quote(selector)));
        }

        [DynamicDependency("Average`1", typeof(Enumerable))]
        public static double Average<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, double>> selector)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(selector);

            return source.Provider.Execute<double>(
                Expression.Call(
                    null,
                    new Func<IQueryable<TSource>, Expression<Func<TSource, double>>, double>(Average).Method,
                    source.Expression, Expression.Quote(selector)));
        }

        [DynamicDependency("Average`1", typeof(Enumerable))]
        public static double? Average<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, double?>> selector)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(selector);

            return source.Provider.Execute<double?>(
                Expression.Call(
                    null,
                    new Func<IQueryable<TSource>, Expression<Func<TSource, double?>>, double?>(Average).Method,
                    source.Expression, Expression.Quote(selector)));
        }

        [DynamicDependency("Average`1", typeof(Enumerable))]
        public static decimal Average<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, decimal>> selector)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(selector);

            return source.Provider.Execute<decimal>(
                Expression.Call(
                    null,
                    new Func<IQueryable<TSource>, Expression<Func<TSource, decimal>>, decimal>(Average).Method,
                    source.Expression, Expression.Quote(selector)));
        }

        [DynamicDependency("Average`1", typeof(Enumerable))]
        public static decimal? Average<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, decimal?>> selector)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(selector);

            return source.Provider.Execute<decimal?>(
                Expression.Call(
                    null,
                    new Func<IQueryable<TSource>, Expression<Func<TSource, decimal?>>, decimal?>(Average).Method,
                    source.Expression, Expression.Quote(selector)));
        }

        [DynamicDependency("Aggregate`1", typeof(Enumerable))]
        public static TSource Aggregate<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, TSource, TSource>> func)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(func);

            return source.Provider.Execute<TSource>(
                Expression.Call(
                    null,
                    new Func<IQueryable<TSource>, Expression<Func<TSource, TSource, TSource>>, TSource>(Aggregate).Method,
                    source.Expression, Expression.Quote(func)));
        }

        [DynamicDependency("Aggregate`2", typeof(Enumerable))]
        public static TAccumulate Aggregate<TSource, TAccumulate>(this IQueryable<TSource> source, TAccumulate seed, Expression<Func<TAccumulate, TSource, TAccumulate>> func)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(func);

            return source.Provider.Execute<TAccumulate>(
                Expression.Call(
                    null,
                    new Func<IQueryable<TSource>, TAccumulate, Expression<Func<TAccumulate, TSource, TAccumulate>>, TAccumulate>(Aggregate).Method,
                    source.Expression, Expression.Constant(seed), Expression.Quote(func)));
        }

        [DynamicDependency("Aggregate`3", typeof(Enumerable))]
        public static TResult Aggregate<TSource, TAccumulate, TResult>(this IQueryable<TSource> source, TAccumulate seed, Expression<Func<TAccumulate, TSource, TAccumulate>> func, Expression<Func<TAccumulate, TResult>> selector)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(func);
            ArgumentNullException.ThrowIfNull(selector);

            return source.Provider.Execute<TResult>(
                Expression.Call(
                    null,
                    new Func<IQueryable<TSource>, TAccumulate, Expression<Func<TAccumulate, TSource, TAccumulate>>, Expression<Func<TAccumulate, TResult>>, TResult>(Aggregate).Method,
                    source.Expression, Expression.Constant(seed), Expression.Quote(func), Expression.Quote(selector)));
        }

        [DynamicDependency("AggregateBy`3", typeof(Enumerable))]
        public static IQueryable<KeyValuePair<TKey, TAccumulate>> AggregateBy<TSource, TKey, TAccumulate>(this IQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector, TAccumulate seed, Expression<Func<TAccumulate, TSource, TAccumulate>> func, IEqualityComparer<TKey>? keyComparer = null) where TKey : notnull
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(keySelector);
            ArgumentNullException.ThrowIfNull(func);

            return source.Provider.CreateQuery<KeyValuePair<TKey, TAccumulate>>(
                Expression.Call(
                    null,
                    new Func<IQueryable<TSource>, Expression<Func<TSource, TKey>>, TAccumulate, Expression<Func<TAccumulate, TSource, TAccumulate>>, IEqualityComparer<TKey>, IQueryable<KeyValuePair<TKey, TAccumulate>>>(AggregateBy).Method,
                    source.Expression, Expression.Quote(keySelector), Expression.Constant(seed), Expression.Quote(func), Expression.Constant(keyComparer, typeof(IEqualityComparer<TKey>))));
        }

        [DynamicDependency("AggregateBy`3", typeof(Enumerable))]
        public static IQueryable<KeyValuePair<TKey, TAccumulate>> AggregateBy<TSource, TKey, TAccumulate>(this IQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector, Expression<Func<TKey, TAccumulate>> seedSelector, Expression<Func<TAccumulate, TSource, TAccumulate>> func, IEqualityComparer<TKey>? keyComparer = null) where TKey : notnull
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(keySelector);
            ArgumentNullException.ThrowIfNull(seedSelector);
            ArgumentNullException.ThrowIfNull(func);

            return source.Provider.CreateQuery<KeyValuePair<TKey, TAccumulate>>(
                Expression.Call(
                    null,
                    new Func<IQueryable<TSource>, Expression<Func<TSource, TKey>>, Expression<Func<TKey, TAccumulate>>, Expression<Func<TAccumulate, TSource, TAccumulate>>, IEqualityComparer<TKey>, IQueryable<KeyValuePair<TKey, TAccumulate>>>(AggregateBy).Method,
                    source.Expression, Expression.Quote(keySelector), Expression.Quote(seedSelector), Expression.Quote(func), Expression.Constant(keyComparer, typeof(IEqualityComparer<TKey>))));
        }

        [DynamicDependency("SkipLast`1", typeof(Enumerable))]
        public static IQueryable<TSource> SkipLast<TSource>(this IQueryable<TSource> source, int count)
        {
            ArgumentNullException.ThrowIfNull(source);

            return source.Provider.CreateQuery<TSource>(
                Expression.Call(
                    null,
                    new Func<IQueryable<TSource>, int, IQueryable<TSource>>(SkipLast).Method,
                    source.Expression, Expression.Constant(count)
                    ));
        }

        [DynamicDependency("TakeLast`1", typeof(Enumerable))]
        public static IQueryable<TSource> TakeLast<TSource>(this IQueryable<TSource> source, int count)
        {
            ArgumentNullException.ThrowIfNull(source);

            return source.Provider.CreateQuery<TSource>(
                Expression.Call(
                    null,
                    new Func<IQueryable<TSource>, int, IQueryable<TSource>>(TakeLast).Method,
                    source.Expression, Expression.Constant(count)));
        }

        [DynamicDependency("Append`1", typeof(Enumerable))]
        public static IQueryable<TSource> Append<TSource>(this IQueryable<TSource> source, TSource element)
        {
            ArgumentNullException.ThrowIfNull(source);

            return source.Provider.CreateQuery<TSource>(
                Expression.Call(
                    null,
                    new Func<IQueryable<TSource>, TSource, IQueryable<TSource>>(Append).Method,
                    source.Expression, Expression.Constant(element)));
        }

        [DynamicDependency("Prepend`1", typeof(Enumerable))]
        public static IQueryable<TSource> Prepend<TSource>(this IQueryable<TSource> source, TSource element)
        {
            ArgumentNullException.ThrowIfNull(source);

            return source.Provider.CreateQuery<TSource>(
                Expression.Call(
                    null,
                    new Func<IQueryable<TSource>, TSource, IQueryable<TSource>>(Prepend).Method,
                    source.Expression, Expression.Constant(element)));
        }
    }
}
