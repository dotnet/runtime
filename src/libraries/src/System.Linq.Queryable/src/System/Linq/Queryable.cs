// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace System.Linq
{
    public static class Queryable
    {
        public static IQueryable<TElement> AsQueryable<TElement>(this IEnumerable<TElement> source)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            return source as IQueryable<TElement> ?? new EnumerableQuery<TElement>(source);
        }

        public static IQueryable AsQueryable(this IEnumerable source)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            IQueryable queryable = source as IQueryable;
            if (queryable != null) return queryable;
            Type enumType = TypeHelper.FindGenericType(typeof(IEnumerable<>), source.GetType());
            if (enumType == null)
                throw Error.ArgumentNotIEnumerableGeneric(nameof(source));
            return EnumerableQuery.Create(enumType.GetTypeInfo().GenericTypeArguments[0], source);
        }

        public static IQueryable<TSource> Where<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (predicate == null)
                throw Error.ArgumentNull(nameof(predicate));
            return source.Provider.CreateQuery<TSource>(
                Expression.Call(
                    null,
                    GetMethodInfoOf(() => Queryable.Where(
                        default(IQueryable<TSource>),
                        default(Expression<Func<TSource, bool>>))),
                    new Expression[] { source.Expression, Expression.Quote(predicate) }
                    ));
        }

        public static IQueryable<TSource> Where<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, int, bool>> predicate)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (predicate == null)
                throw Error.ArgumentNull(nameof(predicate));
            return source.Provider.CreateQuery<TSource>(
                Expression.Call(
                    null,
                    GetMethodInfoOf(() => Queryable.Where(
                        default(IQueryable<TSource>),
                        default(Expression<Func<TSource, int, bool>>))),
                    new Expression[] { source.Expression, Expression.Quote(predicate) }
                    ));
        }

        public static IQueryable<TResult> OfType<TResult>(this IQueryable source)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            return source.Provider.CreateQuery<TResult>(
                Expression.Call(
                    null,
                    GetMethodInfoOf(() => Queryable.OfType<TResult>(
                        default(IQueryable))),
                    new Expression[] { source.Expression }
                    ));
        }

        public static IQueryable<TResult> Cast<TResult>(this IQueryable source)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            return source.Provider.CreateQuery<TResult>(
                Expression.Call(
                    null,
                    GetMethodInfoOf(() => Queryable.Cast<TResult>(
                        default(IQueryable))),
                    new Expression[] { source.Expression }
                    ));
        }

        public static IQueryable<TResult> Select<TSource, TResult>(this IQueryable<TSource> source, Expression<Func<TSource, TResult>> selector)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (selector == null)
                throw Error.ArgumentNull(nameof(selector));
            return source.Provider.CreateQuery<TResult>(
                Expression.Call(
                    null,
                    GetMethodInfoOf(() => Queryable.Select(
                        default(IQueryable<TSource>),
                        default(Expression<Func<TSource, TResult>>))),
                    new Expression[] { source.Expression, Expression.Quote(selector) }
                    ));
        }

        public static IQueryable<TResult> Select<TSource, TResult>(this IQueryable<TSource> source, Expression<Func<TSource, int, TResult>> selector)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (selector == null)
                throw Error.ArgumentNull(nameof(selector));
            return source.Provider.CreateQuery<TResult>(
                Expression.Call(
                    null,
                    GetMethodInfoOf(() => Queryable.Select(
                        default(IQueryable<TSource>),
                        default(Expression<Func<TSource, int, TResult>>))),
                    new Expression[] { source.Expression, Expression.Quote(selector) }
                    ));
        }

        public static IQueryable<TResult> SelectMany<TSource, TResult>(this IQueryable<TSource> source, Expression<Func<TSource, IEnumerable<TResult>>> selector)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (selector == null)
                throw Error.ArgumentNull(nameof(selector));
            return source.Provider.CreateQuery<TResult>(
                Expression.Call(
                    null,
                    GetMethodInfoOf(() => Queryable.SelectMany(
                        default(IQueryable<TSource>),
                        default(Expression<Func<TSource, IEnumerable<TResult>>>))),
                    new Expression[] { source.Expression, Expression.Quote(selector) }
                    ));
        }

        public static IQueryable<TResult> SelectMany<TSource, TResult>(this IQueryable<TSource> source, Expression<Func<TSource, int, IEnumerable<TResult>>> selector)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (selector == null)
                throw Error.ArgumentNull(nameof(selector));
            return source.Provider.CreateQuery<TResult>(
                Expression.Call(
                    null,
                    GetMethodInfoOf(() => Queryable.SelectMany(
                        default(IQueryable<TSource>),
                        default(Expression<Func<TSource, int, IEnumerable<TResult>>>))),
                    new Expression[] { source.Expression, Expression.Quote(selector) }
                    ));
        }

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
                    GetMethodInfoOf(() => Queryable.SelectMany(
                        default(IQueryable<TSource>),
                        default(Expression<Func<TSource, int, IEnumerable<TCollection>>>),
                        default(Expression<Func<TSource, TCollection, TResult>>))),
                    new Expression[] { source.Expression, Expression.Quote(collectionSelector), Expression.Quote(resultSelector) }
                    ));
        }

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
                    GetMethodInfoOf(() => Queryable.SelectMany(
                        default(IQueryable<TSource>),
                        default(Expression<Func<TSource, IEnumerable<TCollection>>>),
                        default(Expression<Func<TSource, TCollection, TResult>>))),
                    new Expression[] { source.Expression, Expression.Quote(collectionSelector), Expression.Quote(resultSelector) }
                    ));
        }

        private static Expression GetSourceExpression<TSource>(IEnumerable<TSource> source)
        {
            IQueryable<TSource> q = source as IQueryable<TSource>;
            if (q != null) return q.Expression;
            return Expression.Constant(source, typeof(IEnumerable<TSource>));
        }

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
                    GetMethodInfoOf(() => Queryable.Join(
                        default(IQueryable<TOuter>),
                        default(IEnumerable<TInner>),
                        default(Expression<Func<TOuter, TKey>>),
                        default(Expression<Func<TInner, TKey>>),
                        default(Expression<Func<TOuter, TInner, TResult>>))),
                    new Expression[] {
                        outer.Expression,
                        GetSourceExpression(inner),
                        Expression.Quote(outerKeySelector),
                        Expression.Quote(innerKeySelector),
                        Expression.Quote(resultSelector)
                        }
                    ));
        }

        public static IQueryable<TResult> Join<TOuter, TInner, TKey, TResult>(this IQueryable<TOuter> outer, IEnumerable<TInner> inner, Expression<Func<TOuter, TKey>> outerKeySelector, Expression<Func<TInner, TKey>> innerKeySelector, Expression<Func<TOuter, TInner, TResult>> resultSelector, IEqualityComparer<TKey> comparer)
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
                    GetMethodInfoOf(() => Queryable.Join(
                        default(IQueryable<TOuter>),
                        default(IEnumerable<TInner>),
                        default(Expression<Func<TOuter, TKey>>),
                        default(Expression<Func<TInner, TKey>>),
                        default(Expression<Func<TOuter, TInner, TResult>>),
                        default(IEqualityComparer<TKey>))),
                    new Expression[] {
                        outer.Expression,
                        GetSourceExpression(inner),
                        Expression.Quote(outerKeySelector),
                        Expression.Quote(innerKeySelector),
                        Expression.Quote(resultSelector),
                        Expression.Constant(comparer, typeof(IEqualityComparer<TKey>))
                        }
                    ));
        }

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
                    GetMethodInfoOf(() => Queryable.GroupJoin(
                        default(IQueryable<TOuter>),
                        default(IEnumerable<TInner>),
                        default(Expression<Func<TOuter, TKey>>),
                        default(Expression<Func<TInner, TKey>>),
                        default(Expression<Func<TOuter, IEnumerable<TInner>, TResult>>))),
                    new Expression[] {
                        outer.Expression,
                        GetSourceExpression(inner),
                        Expression.Quote(outerKeySelector),
                        Expression.Quote(innerKeySelector),
                        Expression.Quote(resultSelector) }
                    ));
        }

        public static IQueryable<TResult> GroupJoin<TOuter, TInner, TKey, TResult>(this IQueryable<TOuter> outer, IEnumerable<TInner> inner, Expression<Func<TOuter, TKey>> outerKeySelector, Expression<Func<TInner, TKey>> innerKeySelector, Expression<Func<TOuter, IEnumerable<TInner>, TResult>> resultSelector, IEqualityComparer<TKey> comparer)
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
                    GetMethodInfoOf(() => Queryable.GroupJoin(
                        default(IQueryable<TOuter>),
                        default(IEnumerable<TInner>),
                        default(Expression<Func<TOuter, TKey>>),
                        default(Expression<Func<TInner, TKey>>),
                        default(Expression<Func<TOuter, IEnumerable<TInner>, TResult>>),
                        default(IEqualityComparer<TKey>))),
                    new Expression[] {
                        outer.Expression,
                        GetSourceExpression(inner),
                        Expression.Quote(outerKeySelector),
                        Expression.Quote(innerKeySelector),
                        Expression.Quote(resultSelector),
                        Expression.Constant(comparer, typeof(IEqualityComparer<TKey>)) }
                    ));
        }

        public static IOrderedQueryable<TSource> OrderBy<TSource, TKey>(this IQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (keySelector == null)
                throw Error.ArgumentNull(nameof(keySelector));
            return (IOrderedQueryable<TSource>)source.Provider.CreateQuery<TSource>(
                Expression.Call(
                    null,
                    GetMethodInfoOf(() => Queryable.OrderBy(
                        default(IQueryable<TSource>),
                        default(Expression<Func<TSource, TKey>>))),
                    new Expression[] { source.Expression, Expression.Quote(keySelector) }
                    ));
        }

        public static IOrderedQueryable<TSource> OrderBy<TSource, TKey>(this IQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector, IComparer<TKey> comparer)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (keySelector == null)
                throw Error.ArgumentNull(nameof(keySelector));
            return (IOrderedQueryable<TSource>)source.Provider.CreateQuery<TSource>(
                Expression.Call(
                    null,
                    GetMethodInfoOf(() => Queryable.OrderBy(
                        default(IQueryable<TSource>),
                        default(Expression<Func<TSource, TKey>>),
                        default(IComparer<TKey>))),
                    new Expression[] { source.Expression, Expression.Quote(keySelector), Expression.Constant(comparer, typeof(IComparer<TKey>)) }
                    ));
        }

        public static IOrderedQueryable<TSource> OrderByDescending<TSource, TKey>(this IQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (keySelector == null)
                throw Error.ArgumentNull(nameof(keySelector));
            return (IOrderedQueryable<TSource>)source.Provider.CreateQuery<TSource>(
                Expression.Call(
                    null,
                    GetMethodInfoOf(() => Queryable.OrderByDescending(
                        default(IQueryable<TSource>),
                        default(Expression<Func<TSource, TKey>>))),
                    new Expression[] { source.Expression, Expression.Quote(keySelector) }
                    ));
        }

        public static IOrderedQueryable<TSource> OrderByDescending<TSource, TKey>(this IQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector, IComparer<TKey> comparer)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (keySelector == null)
                throw Error.ArgumentNull(nameof(keySelector));
            return (IOrderedQueryable<TSource>)source.Provider.CreateQuery<TSource>(
                Expression.Call(
                    null,
                    GetMethodInfoOf(() => Queryable.OrderByDescending(
                        default(IQueryable<TSource>),
                        default(Expression<Func<TSource, TKey>>),
                        default(IComparer<TKey>))),
                    new Expression[] { source.Expression, Expression.Quote(keySelector), Expression.Constant(comparer, typeof(IComparer<TKey>)) }
                    ));
        }

        public static IOrderedQueryable<TSource> ThenBy<TSource, TKey>(this IOrderedQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (keySelector == null)
                throw Error.ArgumentNull(nameof(keySelector));
            return (IOrderedQueryable<TSource>)source.Provider.CreateQuery<TSource>(
                Expression.Call(
                    null,
                    GetMethodInfoOf(() => Queryable.ThenBy(
                        default(IOrderedQueryable<TSource>),
                        default(Expression<Func<TSource, TKey>>))),
                    new Expression[] { source.Expression, Expression.Quote(keySelector) }
                    ));
        }

        public static IOrderedQueryable<TSource> ThenBy<TSource, TKey>(this IOrderedQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector, IComparer<TKey> comparer)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (keySelector == null)
                throw Error.ArgumentNull(nameof(keySelector));
            return (IOrderedQueryable<TSource>)source.Provider.CreateQuery<TSource>(
                Expression.Call(
                    null,
                    GetMethodInfoOf(() => Queryable.ThenBy(
                        default(IOrderedQueryable<TSource>),
                        default(Expression<Func<TSource, TKey>>),
                        default(IComparer<TKey>))),
                    new Expression[] { source.Expression, Expression.Quote(keySelector), Expression.Constant(comparer, typeof(IComparer<TKey>)) }
                    ));
        }

        public static IOrderedQueryable<TSource> ThenByDescending<TSource, TKey>(this IOrderedQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (keySelector == null)
                throw Error.ArgumentNull(nameof(keySelector));
            return (IOrderedQueryable<TSource>)source.Provider.CreateQuery<TSource>(
                Expression.Call(
                    null,
                    GetMethodInfoOf(() => Queryable.ThenByDescending(
                        default(IOrderedQueryable<TSource>),
                        default(Expression<Func<TSource, TKey>>))),
                    new Expression[] { source.Expression, Expression.Quote(keySelector) }
                    ));
        }

        public static IOrderedQueryable<TSource> ThenByDescending<TSource, TKey>(this IOrderedQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector, IComparer<TKey> comparer)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (keySelector == null)
                throw Error.ArgumentNull(nameof(keySelector));
            return (IOrderedQueryable<TSource>)source.Provider.CreateQuery<TSource>(
                Expression.Call(
                    null,
                    GetMethodInfoOf(() => Queryable.ThenByDescending(
                        default(IOrderedQueryable<TSource>),
                        default(Expression<Func<TSource, TKey>>),
                        default(IComparer<TKey>))),
                    new Expression[] { source.Expression, Expression.Quote(keySelector), Expression.Constant(comparer, typeof(IComparer<TKey>)) }
                    ));
        }

        public static IQueryable<TSource> Take<TSource>(this IQueryable<TSource> source, int count)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            return source.Provider.CreateQuery<TSource>(
                Expression.Call(
                    null,
                    GetMethodInfoOf(() => Queryable.Take(
                        default(IQueryable<TSource>),
                        default(int))),
                    new Expression[] { source.Expression, Expression.Constant(count) }
                    ));
        }

        public static IQueryable<TSource> TakeWhile<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (predicate == null)
                throw Error.ArgumentNull(nameof(predicate));
            return source.Provider.CreateQuery<TSource>(
                Expression.Call(
                    null,
                    GetMethodInfoOf(() => Queryable.TakeWhile(
                        default(IQueryable<TSource>),
                        default(Expression<Func<TSource, bool>>))),
                    new Expression[] { source.Expression, Expression.Quote(predicate) }
                    ));
        }

        public static IQueryable<TSource> TakeWhile<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, int, bool>> predicate)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (predicate == null)
                throw Error.ArgumentNull(nameof(predicate));
            return source.Provider.CreateQuery<TSource>(
                Expression.Call(
                    null,
                    GetMethodInfoOf(() => Queryable.TakeWhile(
                        default(IQueryable<TSource>),
                        default(Expression<Func<TSource, int, bool>>))),
                    new Expression[] { source.Expression, Expression.Quote(predicate) }
                    ));
        }

        public static IQueryable<TSource> Skip<TSource>(this IQueryable<TSource> source, int count)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            return source.Provider.CreateQuery<TSource>(
                Expression.Call(
                    null,
                    GetMethodInfoOf(() => Queryable.Skip(
                        default(IQueryable<TSource>),
                        default(int))),
                    new Expression[] { source.Expression, Expression.Constant(count) }
                    ));
        }

        public static IQueryable<TSource> SkipWhile<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (predicate == null)
                throw Error.ArgumentNull(nameof(predicate));
            return source.Provider.CreateQuery<TSource>(
                Expression.Call(
                    null,
                    GetMethodInfoOf(() => Queryable.SkipWhile(
                        default(IQueryable<TSource>),
                        default(Expression<Func<TSource, bool>>))),
                    new Expression[] { source.Expression, Expression.Quote(predicate) }
                    ));
        }

        public static IQueryable<TSource> SkipWhile<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, int, bool>> predicate)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (predicate == null)
                throw Error.ArgumentNull(nameof(predicate));
            return source.Provider.CreateQuery<TSource>(
                Expression.Call(
                    null,
                    GetMethodInfoOf(() => Queryable.SkipWhile(
                        default(IQueryable<TSource>),
                        default(Expression<Func<TSource, int, bool>>))),
                    new Expression[] { source.Expression, Expression.Quote(predicate) }
                    ));
        }

        public static IQueryable<IGrouping<TKey, TSource>> GroupBy<TSource, TKey>(this IQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (keySelector == null)
                throw Error.ArgumentNull(nameof(keySelector));
            return source.Provider.CreateQuery<IGrouping<TKey, TSource>>(
                Expression.Call(
                    null,
                    GetMethodInfoOf(() => Queryable.GroupBy(
                        default(IQueryable<TSource>),
                        default(Expression<Func<TSource, TKey>>))),
                    new Expression[] { source.Expression, Expression.Quote(keySelector) }
                    ));
        }

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
                    GetMethodInfoOf(() => Queryable.GroupBy(
                        default(IQueryable<TSource>),
                        default(Expression<Func<TSource, TKey>>),
                        default(Expression<Func<TSource, TElement>>))),
                    new Expression[] { source.Expression, Expression.Quote(keySelector), Expression.Quote(elementSelector) }
                    ));
        }

        public static IQueryable<IGrouping<TKey, TSource>> GroupBy<TSource, TKey>(this IQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector, IEqualityComparer<TKey> comparer)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (keySelector == null)
                throw Error.ArgumentNull(nameof(keySelector));
            return source.Provider.CreateQuery<IGrouping<TKey, TSource>>(
                Expression.Call(
                    null,
                    GetMethodInfoOf(() => Queryable.GroupBy(
                        default(IQueryable<TSource>),
                        default(Expression<Func<TSource, TKey>>),
                        default(IEqualityComparer<TKey>))),
                    new Expression[] { source.Expression, Expression.Quote(keySelector), Expression.Constant(comparer, typeof(IEqualityComparer<TKey>)) }
                    ));
        }

        public static IQueryable<IGrouping<TKey, TElement>> GroupBy<TSource, TKey, TElement>(this IQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector, Expression<Func<TSource, TElement>> elementSelector, IEqualityComparer<TKey> comparer)
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
                    GetMethodInfoOf(() => Queryable.GroupBy(
                        default(IQueryable<TSource>),
                        default(Expression<Func<TSource, TKey>>),
                        default(Expression<Func<TSource, TElement>>),
                        default(IEqualityComparer<TKey>))),
                    new Expression[] { source.Expression, Expression.Quote(keySelector), Expression.Quote(elementSelector), Expression.Constant(comparer, typeof(IEqualityComparer<TKey>)) }
                    ));
        }

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
                    GetMethodInfoOf(() => Queryable.GroupBy(
                        default(IQueryable<TSource>),
                        default(Expression<Func<TSource, TKey>>),
                        default(Expression<Func<TSource, TElement>>),
                        default(Expression<Func<TKey, IEnumerable<TElement>, TResult>>))),
                    new Expression[] { source.Expression, Expression.Quote(keySelector), Expression.Quote(elementSelector), Expression.Quote(resultSelector) }
                    ));
        }

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
                    GetMethodInfoOf(() => Queryable.GroupBy(
                        default(IQueryable<TSource>),
                        default(Expression<Func<TSource, TKey>>),
                        default(Expression<Func<TKey, IEnumerable<TSource>, TResult>>))),
                    new Expression[] { source.Expression, Expression.Quote(keySelector), Expression.Quote(resultSelector) }
                    ));
        }

        public static IQueryable<TResult> GroupBy<TSource, TKey, TResult>(this IQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector, Expression<Func<TKey, IEnumerable<TSource>, TResult>> resultSelector, IEqualityComparer<TKey> comparer)
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
                    GetMethodInfoOf(() => Queryable.GroupBy(
                        default(IQueryable<TSource>),
                        default(Expression<Func<TSource, TKey>>),
                        default(Expression<Func<TKey, IEnumerable<TSource>, TResult>>),
                        default(IEqualityComparer<TKey>))),
                    new Expression[] { source.Expression, Expression.Quote(keySelector), Expression.Quote(resultSelector), Expression.Constant(comparer, typeof(IEqualityComparer<TKey>)) }
                    ));
        }

        public static IQueryable<TResult> GroupBy<TSource, TKey, TElement, TResult>(this IQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector, Expression<Func<TSource, TElement>> elementSelector, Expression<Func<TKey, IEnumerable<TElement>, TResult>> resultSelector, IEqualityComparer<TKey> comparer)
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
                    GetMethodInfoOf(() => Queryable.GroupBy(
                        default(IQueryable<TSource>),
                        default(Expression<Func<TSource, TKey>>),
                        default(Expression<Func<TSource, TElement>>),
                        default(Expression<Func<TKey, IEnumerable<TElement>, TResult>>),
                        default(IEqualityComparer<TKey>))),
                    new Expression[] { source.Expression, Expression.Quote(keySelector), Expression.Quote(elementSelector), Expression.Quote(resultSelector), Expression.Constant(comparer, typeof(IEqualityComparer<TKey>)) }
                    ));
        }

        public static IQueryable<TSource> Distinct<TSource>(this IQueryable<TSource> source)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            return source.Provider.CreateQuery<TSource>(
                Expression.Call(
                    null,
                    GetMethodInfoOf(() => Queryable.Distinct(
                        default(IQueryable<TSource>))),
                    new Expression[] { source.Expression }
                    ));
        }

        public static IQueryable<TSource> Distinct<TSource>(this IQueryable<TSource> source, IEqualityComparer<TSource> comparer)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            return source.Provider.CreateQuery<TSource>(
                Expression.Call(
                    null,
                    GetMethodInfoOf(() => Queryable.Distinct(
                        default(IQueryable<TSource>),
                        default(IEqualityComparer<TSource>))),
                    new Expression[] { source.Expression, Expression.Constant(comparer, typeof(IEqualityComparer<TSource>)) }
                    ));
        }

        public static IQueryable<TSource> Concat<TSource>(this IQueryable<TSource> source1, IEnumerable<TSource> source2)
        {
            if (source1 == null)
                throw Error.ArgumentNull(nameof(source1));
            if (source2 == null)
                throw Error.ArgumentNull(nameof(source2));
            return source1.Provider.CreateQuery<TSource>(
                Expression.Call(
                    null,
                    GetMethodInfoOf(() => Queryable.Concat(
                        default(IQueryable<TSource>),
                        default(IQueryable<TSource>))),
                    new Expression[] { source1.Expression, GetSourceExpression(source2) }
                    ));
        }

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
                    GetMethodInfoOf(() => Queryable.Zip(
                        default(IQueryable<TFirst>),
                        default(IEnumerable<TSecond>),
                        default(Expression<Func<TFirst, TSecond, TResult>>))),
                    new Expression[] { source1.Expression, GetSourceExpression(source2), Expression.Quote(resultSelector) }
                    ));
        }

        public static IQueryable<TSource> Union<TSource>(this IQueryable<TSource> source1, IEnumerable<TSource> source2)
        {
            if (source1 == null)
                throw Error.ArgumentNull(nameof(source1));
            if (source2 == null)
                throw Error.ArgumentNull(nameof(source2));
            return source1.Provider.CreateQuery<TSource>(
                Expression.Call(
                    null,
                    GetMethodInfoOf(() => Queryable.Union(
                        default(IQueryable<TSource>),
                        default(IQueryable<TSource>))),
                    new Expression[] { source1.Expression, GetSourceExpression(source2) }
                    ));
        }

        public static IQueryable<TSource> Union<TSource>(this IQueryable<TSource> source1, IEnumerable<TSource> source2, IEqualityComparer<TSource> comparer)
        {
            if (source1 == null)
                throw Error.ArgumentNull(nameof(source1));
            if (source2 == null)
                throw Error.ArgumentNull(nameof(source2));
            return source1.Provider.CreateQuery<TSource>(
                Expression.Call(
                    null,
                    GetMethodInfoOf(() => Queryable.Union(
                        default(IQueryable<TSource>),
                        default(IQueryable<TSource>),
                        default(IEqualityComparer<TSource>))),
                    new Expression[] {
                        source1.Expression,
                        GetSourceExpression(source2),
                        Expression.Constant(comparer, typeof(IEqualityComparer<TSource>))
                        }
                    ));
        }

        public static IQueryable<TSource> Intersect<TSource>(this IQueryable<TSource> source1, IEnumerable<TSource> source2)
        {
            if (source1 == null)
                throw Error.ArgumentNull(nameof(source1));
            if (source2 == null)
                throw Error.ArgumentNull(nameof(source2));
            return source1.Provider.CreateQuery<TSource>(
                Expression.Call(
                    null,
                    GetMethodInfoOf(() => Queryable.Intersect(
                        default(IQueryable<TSource>),
                        default(IQueryable<TSource>))),
                    new Expression[] { source1.Expression, GetSourceExpression(source2) }
                    ));
        }

        public static IQueryable<TSource> Intersect<TSource>(this IQueryable<TSource> source1, IEnumerable<TSource> source2, IEqualityComparer<TSource> comparer)
        {
            if (source1 == null)
                throw Error.ArgumentNull(nameof(source1));
            if (source2 == null)
                throw Error.ArgumentNull(nameof(source2));
            return source1.Provider.CreateQuery<TSource>(
                Expression.Call(
                    null,
                    GetMethodInfoOf(() => Queryable.Intersect(
                        default(IQueryable<TSource>),
                        default(IEnumerable<TSource>),
                        default(IEqualityComparer<TSource>))),
                    new Expression[] {
                        source1.Expression,
                        GetSourceExpression(source2),
                        Expression.Constant(comparer, typeof(IEqualityComparer<TSource>))
                        }
                    ));
        }

        public static IQueryable<TSource> Except<TSource>(this IQueryable<TSource> source1, IEnumerable<TSource> source2)
        {
            if (source1 == null)
                throw Error.ArgumentNull(nameof(source1));
            if (source2 == null)
                throw Error.ArgumentNull(nameof(source2));
            return source1.Provider.CreateQuery<TSource>(
                Expression.Call(
                    null,
                    GetMethodInfoOf(() => Queryable.Except(
                        default(IQueryable<TSource>),
                        default(IEnumerable<TSource>))),
                    new Expression[] { source1.Expression, GetSourceExpression(source2) }
                    ));
        }

        public static IQueryable<TSource> Except<TSource>(this IQueryable<TSource> source1, IEnumerable<TSource> source2, IEqualityComparer<TSource> comparer)
        {
            if (source1 == null)
                throw Error.ArgumentNull(nameof(source1));
            if (source2 == null)
                throw Error.ArgumentNull(nameof(source2));
            return source1.Provider.CreateQuery<TSource>(
                Expression.Call(
                    null,
                    GetMethodInfoOf(() => Queryable.Except(
                        default(IQueryable<TSource>),
                        default(IEnumerable<TSource>),
                        default(IEqualityComparer<TSource>))),
                    new Expression[] {
                        source1.Expression,
                        GetSourceExpression(source2),
                        Expression.Constant(comparer, typeof(IEqualityComparer<TSource>))
                        }
                    ));
        }

        public static TSource First<TSource>(this IQueryable<TSource> source)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            return source.Provider.Execute<TSource>(
                Expression.Call(
                    null,
                    GetMethodInfoOf(() => Queryable.First(
                        default(IQueryable<TSource>))),
                    new Expression[] { source.Expression }
                    ));
        }

        public static TSource First<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (predicate == null)
                throw Error.ArgumentNull(nameof(predicate));
            return source.Provider.Execute<TSource>(
                Expression.Call(
                    null,
                    GetMethodInfoOf(() => Queryable.First(
                        default(IQueryable<TSource>),
                        default(Expression<Func<TSource, bool>>))),
                    new Expression[] { source.Expression, Expression.Quote(predicate) }
                    ));
        }

        public static TSource FirstOrDefault<TSource>(this IQueryable<TSource> source)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            return source.Provider.Execute<TSource>(
                Expression.Call(
                    null,
                    GetMethodInfoOf(() => Queryable.FirstOrDefault(
                        default(IQueryable<TSource>))),
                    new Expression[] { source.Expression }
                    ));
        }

        public static TSource FirstOrDefault<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (predicate == null)
                throw Error.ArgumentNull(nameof(predicate));
            return source.Provider.Execute<TSource>(
                Expression.Call(
                    null,
                    GetMethodInfoOf(() => Queryable.FirstOrDefault(
                        default(IQueryable<TSource>),
                        default(Expression<Func<TSource, bool>>))),
                    new Expression[] { source.Expression, Expression.Quote(predicate) }
                    ));
        }

        public static TSource Last<TSource>(this IQueryable<TSource> source)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            return source.Provider.Execute<TSource>(
                Expression.Call(
                    null,
                    GetMethodInfoOf(() => Queryable.Last(
                        default(IQueryable<TSource>))),
                    new Expression[] { source.Expression }
                    ));
        }

        public static TSource Last<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (predicate == null)
                throw Error.ArgumentNull(nameof(predicate));
            return source.Provider.Execute<TSource>(
                Expression.Call(
                    null,
                    GetMethodInfoOf(() => Queryable.Last(
                        default(IQueryable<TSource>),
                        default(Expression<Func<TSource, bool>>))),
                    new Expression[] { source.Expression, Expression.Quote(predicate) }
                    ));
        }

        public static TSource LastOrDefault<TSource>(this IQueryable<TSource> source)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            return source.Provider.Execute<TSource>(
                Expression.Call(
                    null,
                    GetMethodInfoOf(() => Queryable.LastOrDefault(
                        default(IQueryable<TSource>))),
                    new Expression[] { source.Expression }
                    ));
        }

        public static TSource LastOrDefault<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (predicate == null)
                throw Error.ArgumentNull(nameof(predicate));
            return source.Provider.Execute<TSource>(
                Expression.Call(
                    null,
                    GetMethodInfoOf(() => Queryable.LastOrDefault(
                        default(IQueryable<TSource>),
                        default(Expression<Func<TSource, bool>>))),
                    new Expression[] { source.Expression, Expression.Quote(predicate) }
                    ));
        }

        public static TSource Single<TSource>(this IQueryable<TSource> source)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            return source.Provider.Execute<TSource>(
                Expression.Call(
                    null,
                    GetMethodInfoOf(() => Queryable.Single(
                        default(IQueryable<TSource>))),
                    new Expression[] { source.Expression }
                    ));
        }

        public static TSource Single<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (predicate == null)
                throw Error.ArgumentNull(nameof(predicate));
            return source.Provider.Execute<TSource>(
                Expression.Call(
                    null,
                    GetMethodInfoOf(() => Queryable.Single(
                        default(IQueryable<TSource>),
                        default(Expression<Func<TSource, bool>>))),
                    new Expression[] { source.Expression, Expression.Quote(predicate) }
                    ));
        }

        public static TSource SingleOrDefault<TSource>(this IQueryable<TSource> source)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            return source.Provider.Execute<TSource>(
                Expression.Call(
                    null,
                    GetMethodInfoOf(() => Queryable.SingleOrDefault(
                        default(IQueryable<TSource>))),
                    new Expression[] { source.Expression }
                    ));
        }

        public static TSource SingleOrDefault<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (predicate == null)
                throw Error.ArgumentNull(nameof(predicate));
            return source.Provider.Execute<TSource>(
                Expression.Call(
                    null,
                    GetMethodInfoOf(() => Queryable.SingleOrDefault(
                        default(IQueryable<TSource>),
                        default(Expression<Func<TSource, bool>>))),
                    new Expression[] { source.Expression, Expression.Quote(predicate) }
                    ));
        }

        public static TSource ElementAt<TSource>(this IQueryable<TSource> source, int index)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (index < 0)
                throw Error.ArgumentOutOfRange(nameof(index));
            return source.Provider.Execute<TSource>(
                Expression.Call(
                    null,
                    GetMethodInfoOf(() => Queryable.ElementAt(
                        default(IQueryable<TSource>),
                        default(int))),
                    new Expression[] { source.Expression, Expression.Constant(index) }
                    ));
        }

        public static TSource ElementAtOrDefault<TSource>(this IQueryable<TSource> source, int index)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            return source.Provider.Execute<TSource>(
                Expression.Call(
                    null,
                    GetMethodInfoOf(() => Queryable.ElementAtOrDefault(
                        default(IQueryable<TSource>),
                        default(int))),
                    new Expression[] { source.Expression, Expression.Constant(index) }
                    ));
        }

        public static IQueryable<TSource> DefaultIfEmpty<TSource>(this IQueryable<TSource> source)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            return source.Provider.CreateQuery<TSource>(
                Expression.Call(
                    null,
                    GetMethodInfoOf(() => Queryable.DefaultIfEmpty(
                        default(IQueryable<TSource>))),
                    new Expression[] { source.Expression }
                    ));
        }

        public static IQueryable<TSource> DefaultIfEmpty<TSource>(this IQueryable<TSource> source, TSource defaultValue)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            return source.Provider.CreateQuery<TSource>(
                Expression.Call(
                    null,
                    GetMethodInfoOf(() => Queryable.DefaultIfEmpty(
                        default(IQueryable<TSource>),
                        default(TSource))),
                    new Expression[] { source.Expression, Expression.Constant(defaultValue, typeof(TSource)) }
                    ));
        }

        public static bool Contains<TSource>(this IQueryable<TSource> source, TSource item)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            return source.Provider.Execute<bool>(
                Expression.Call(
                    null,
                    GetMethodInfoOf(() => Queryable.Contains(
                        default(IQueryable<TSource>),
                        default(TSource))),
                    new Expression[] { source.Expression, Expression.Constant(item, typeof(TSource)) }
                    ));
        }

        public static bool Contains<TSource>(this IQueryable<TSource> source, TSource item, IEqualityComparer<TSource> comparer)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            return source.Provider.Execute<bool>(
                Expression.Call(
                    null,
                    GetMethodInfoOf(() => Queryable.Contains(
                        default(IQueryable<TSource>),
                        default(TSource),
                        default(IEqualityComparer<TSource>))),
                    new Expression[] { source.Expression, Expression.Constant(item, typeof(TSource)), Expression.Constant(comparer, typeof(IEqualityComparer<TSource>)) }
                    ));
        }

        public static IQueryable<TSource> Reverse<TSource>(this IQueryable<TSource> source)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            return source.Provider.CreateQuery<TSource>(
                Expression.Call(
                    null,
                    GetMethodInfoOf(() => Queryable.Reverse(
                        default(IQueryable<TSource>))),
                    new Expression[] { source.Expression }
                    ));
        }

        public static bool SequenceEqual<TSource>(this IQueryable<TSource> source1, IEnumerable<TSource> source2)
        {
            if (source1 == null)
                throw Error.ArgumentNull(nameof(source1));
            if (source2 == null)
                throw Error.ArgumentNull(nameof(source2));
            return source1.Provider.Execute<bool>(
                Expression.Call(
                    null,
                    GetMethodInfoOf(() => Queryable.SequenceEqual(
                        default(IQueryable<TSource>),
                        default(IQueryable<TSource>))),
                    new Expression[] { source1.Expression, GetSourceExpression(source2) }
                    ));
        }

        public static bool SequenceEqual<TSource>(this IQueryable<TSource> source1, IEnumerable<TSource> source2, IEqualityComparer<TSource> comparer)
        {
            if (source1 == null)
                throw Error.ArgumentNull(nameof(source1));
            if (source2 == null)
                throw Error.ArgumentNull(nameof(source2));
            return source1.Provider.Execute<bool>(
                Expression.Call(
                    null,
                    GetMethodInfoOf(() => Queryable.SequenceEqual(
                        default(IQueryable<TSource>),
                        default(IEnumerable<TSource>),
                        default(IEqualityComparer<TSource>))),
                    new Expression[] {
                        source1.Expression,
                        GetSourceExpression(source2),
                        Expression.Constant(comparer, typeof(IEqualityComparer<TSource>))
                        }
                    ));
        }

        public static bool Any<TSource>(this IQueryable<TSource> source)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            return source.Provider.Execute<bool>(
                Expression.Call(
                    null,
                    GetMethodInfoOf(() => Queryable.Any(
                        default(IQueryable<TSource>))),
                    new Expression[] { source.Expression }
                    ));
        }

        public static bool Any<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (predicate == null)
                throw Error.ArgumentNull(nameof(predicate));
            return source.Provider.Execute<bool>(
                Expression.Call(
                    null,
                    GetMethodInfoOf(() => Queryable.Any(
                        default(IQueryable<TSource>),
                        default(Expression<Func<TSource, bool>>))),
                    new Expression[] { source.Expression, Expression.Quote(predicate) }
                    ));
        }

        public static bool All<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (predicate == null)
                throw Error.ArgumentNull(nameof(predicate));
            return source.Provider.Execute<bool>(
                Expression.Call(
                    null,
                    GetMethodInfoOf(() => Queryable.All(
                        default(IQueryable<TSource>),
                        default(Expression<Func<TSource, bool>>))),
                    new Expression[] { source.Expression, Expression.Quote(predicate) }
                    ));
        }

        public static int Count<TSource>(this IQueryable<TSource> source)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            return source.Provider.Execute<int>(
                Expression.Call(
                    null,
                    GetMethodInfoOf(() => Queryable.Count(
                        default(IQueryable<TSource>))),
                    new Expression[] { source.Expression }
                    ));
        }

        public static int Count<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (predicate == null)
                throw Error.ArgumentNull(nameof(predicate));
            return source.Provider.Execute<int>(
                Expression.Call(
                    null,
                    GetMethodInfoOf(() => Queryable.Count(
                        default(IQueryable<TSource>),
                        default(Expression<Func<TSource, bool>>))),
                    new Expression[] { source.Expression, Expression.Quote(predicate) }
                    ));
        }

        public static long LongCount<TSource>(this IQueryable<TSource> source)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            return source.Provider.Execute<long>(
                Expression.Call(
                    null,
                    GetMethodInfoOf(() => Queryable.LongCount(
                        default(IQueryable<TSource>))),
                    new Expression[] { source.Expression }
                    ));
        }

        public static long LongCount<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (predicate == null)
                throw Error.ArgumentNull(nameof(predicate));
            return source.Provider.Execute<long>(
                Expression.Call(
                    null,
                    GetMethodInfoOf(() => Queryable.LongCount(
                        default(IQueryable<TSource>),
                        default(Expression<Func<TSource, bool>>))),
                    new Expression[] { source.Expression, Expression.Quote(predicate) }
                    ));
        }

        public static TSource Min<TSource>(this IQueryable<TSource> source)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            return source.Provider.Execute<TSource>(
                Expression.Call(
                    null,
                    GetMethodInfoOf(() => Queryable.Min(
                        default(IQueryable<TSource>))),
                    new Expression[] { source.Expression }
                    ));
        }

        public static TResult Min<TSource, TResult>(this IQueryable<TSource> source, Expression<Func<TSource, TResult>> selector)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (selector == null)
                throw Error.ArgumentNull(nameof(selector));
            return source.Provider.Execute<TResult>(
                Expression.Call(
                    null,
                    GetMethodInfoOf(() => Queryable.Min(
                        default(IQueryable<TSource>),
                        default(Expression<Func<TSource, TResult>>))),
                    new Expression[] { source.Expression, Expression.Quote(selector) }
                    ));
        }

        public static TSource Max<TSource>(this IQueryable<TSource> source)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            return source.Provider.Execute<TSource>(
                Expression.Call(
                    null,
                    GetMethodInfoOf(() => Queryable.Max(
                        default(IQueryable<TSource>))),
                    new Expression[] { source.Expression }
                    ));
        }

        public static TResult Max<TSource, TResult>(this IQueryable<TSource> source, Expression<Func<TSource, TResult>> selector)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (selector == null)
                throw Error.ArgumentNull(nameof(selector));
            return source.Provider.Execute<TResult>(
                Expression.Call(
                    null,
                    GetMethodInfoOf(() => Queryable.Max(
                        default(IQueryable<TSource>),
                        default(Expression<Func<TSource, TResult>>))),
                    new Expression[] { source.Expression, Expression.Quote(selector) }
                    ));
        }

        public static int Sum(this IQueryable<int> source)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            return source.Provider.Execute<int>(
                Expression.Call(
                    null,
                    GetMethodInfoOf(() => Queryable.Sum(
                        default(IQueryable<int>))),
                    new Expression[] { source.Expression }
                    ));
        }

        public static int? Sum(this IQueryable<int?> source)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            return source.Provider.Execute<int?>(
                Expression.Call(
                    null,
                    GetMethodInfoOf(() => Queryable.Sum(
                        default(IQueryable<int?>))),
                    new Expression[] { source.Expression }
                    ));
        }

        public static long Sum(this IQueryable<long> source)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            return source.Provider.Execute<long>(
                Expression.Call(
                    null,
                    GetMethodInfoOf(() => Queryable.Sum(
                        default(IQueryable<long>))),
                    new Expression[] { source.Expression }
                    ));
        }

        public static long? Sum(this IQueryable<long?> source)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            return source.Provider.Execute<long?>(
                Expression.Call(
                    null,
                    GetMethodInfoOf(() => Queryable.Sum(
                        default(IQueryable<long?>))),
                    new Expression[] { source.Expression }
                    ));
        }

        public static float Sum(this IQueryable<float> source)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            return source.Provider.Execute<float>(
                Expression.Call(
                    null,
                    GetMethodInfoOf(() => Queryable.Sum(
                        default(IQueryable<float>))),
                    new Expression[] { source.Expression }
                    ));
        }

        public static float? Sum(this IQueryable<float?> source)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            return source.Provider.Execute<float?>(
                Expression.Call(
                    null,
                    GetMethodInfoOf(() => Queryable.Sum(
                        default(IQueryable<float?>))),
                    new Expression[] { source.Expression }
                    ));
        }

        public static double Sum(this IQueryable<double> source)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            return source.Provider.Execute<double>(
                Expression.Call(
                    null,
                    GetMethodInfoOf(() => Queryable.Sum(
                        default(IQueryable<double>))),
                    new Expression[] { source.Expression }
                    ));
        }

        public static double? Sum(this IQueryable<double?> source)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            return source.Provider.Execute<double?>(
                Expression.Call(
                    null,
                    GetMethodInfoOf(() => Queryable.Sum(
                        default(IQueryable<double?>))),
                    new Expression[] { source.Expression }
                    ));
        }

        public static decimal Sum(this IQueryable<decimal> source)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            return source.Provider.Execute<decimal>(
                Expression.Call(
                    null,
                    GetMethodInfoOf(() => Queryable.Sum(
                        default(IQueryable<decimal>))),
                    new Expression[] { source.Expression }
                    ));
        }

        public static decimal? Sum(this IQueryable<decimal?> source)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            return source.Provider.Execute<decimal?>(
                Expression.Call(
                    null,
                    GetMethodInfoOf(() => Queryable.Sum(
                        default(IQueryable<decimal?>))),
                    new Expression[] { source.Expression }
                    ));
        }

        public static int Sum<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, int>> selector)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (selector == null)
                throw Error.ArgumentNull(nameof(selector));
            return source.Provider.Execute<int>(
                Expression.Call(
                    null,
                    GetMethodInfoOf(() => Queryable.Sum(
                        default(IQueryable<TSource>),
                        default(Expression<Func<TSource, int>>))),
                    new Expression[] { source.Expression, Expression.Quote(selector) }
                    ));
        }

        public static int? Sum<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, int?>> selector)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (selector == null)
                throw Error.ArgumentNull(nameof(selector));
            return source.Provider.Execute<int?>(
                Expression.Call(
                    null,
                    GetMethodInfoOf(() => Queryable.Sum(
                        default(IQueryable<TSource>),
                        default(Expression<Func<TSource, int?>>))),
                    new Expression[] { source.Expression, Expression.Quote(selector) }
                    ));
        }

        public static long Sum<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, long>> selector)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (selector == null)
                throw Error.ArgumentNull(nameof(selector));
            return source.Provider.Execute<long>(
                Expression.Call(
                    null,
                    GetMethodInfoOf(() => Queryable.Sum(
                        default(IQueryable<TSource>),
                        default(Expression<Func<TSource, long>>))),
                    new Expression[] { source.Expression, Expression.Quote(selector) }
                    ));
        }

        public static long? Sum<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, long?>> selector)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (selector == null)
                throw Error.ArgumentNull(nameof(selector));
            return source.Provider.Execute<long?>(
                Expression.Call(
                    null,
                    GetMethodInfoOf(() => Queryable.Sum(
                        default(IQueryable<TSource>),
                        default(Expression<Func<TSource, long?>>))),
                    new Expression[] { source.Expression, Expression.Quote(selector) }
                    ));
        }

        public static float Sum<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, float>> selector)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (selector == null)
                throw Error.ArgumentNull(nameof(selector));
            return source.Provider.Execute<float>(
                Expression.Call(
                    null,
                    GetMethodInfoOf(() => Queryable.Sum(
                        default(IQueryable<TSource>),
                        default(Expression<Func<TSource, float>>))),
                    new Expression[] { source.Expression, Expression.Quote(selector) }
                    ));
        }

        public static float? Sum<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, float?>> selector)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (selector == null)
                throw Error.ArgumentNull(nameof(selector));
            return source.Provider.Execute<float?>(
                Expression.Call(
                    null,
                    GetMethodInfoOf(() => Queryable.Sum(
                        default(IQueryable<TSource>),
                        default(Expression<Func<TSource, float?>>))),
                    new Expression[] { source.Expression, Expression.Quote(selector) }
                    ));
        }

        public static double Sum<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, double>> selector)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (selector == null)
                throw Error.ArgumentNull(nameof(selector));
            return source.Provider.Execute<double>(
                Expression.Call(
                    null,
                    GetMethodInfoOf(() => Queryable.Sum(
                        default(IQueryable<TSource>),
                        default(Expression<Func<TSource, double>>))),
                    new Expression[] { source.Expression, Expression.Quote(selector) }
                    ));
        }

        public static double? Sum<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, double?>> selector)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (selector == null)
                throw Error.ArgumentNull(nameof(selector));
            return source.Provider.Execute<double?>(
                Expression.Call(
                    null,
                    GetMethodInfoOf(() => Queryable.Sum(
                        default(IQueryable<TSource>),
                        default(Expression<Func<TSource, double?>>))),
                    new Expression[] { source.Expression, Expression.Quote(selector) }
                    ));
        }

        public static decimal Sum<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, decimal>> selector)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (selector == null)
                throw Error.ArgumentNull(nameof(selector));
            return source.Provider.Execute<decimal>(
                Expression.Call(
                    null,
                    GetMethodInfoOf(() => Queryable.Sum(
                        default(IQueryable<TSource>),
                        default(Expression<Func<TSource, decimal>>))),
                    new Expression[] { source.Expression, Expression.Quote(selector) }
                    ));
        }

        public static decimal? Sum<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, decimal?>> selector)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (selector == null)
                throw Error.ArgumentNull(nameof(selector));
            return source.Provider.Execute<decimal?>(
                Expression.Call(
                    null,
                    GetMethodInfoOf(() => Queryable.Sum(
                        default(IQueryable<TSource>),
                        default(Expression<Func<TSource, decimal?>>))),
                    new Expression[] { source.Expression, Expression.Quote(selector) }
                    ));
        }

        public static double Average(this IQueryable<int> source)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            return source.Provider.Execute<double>(
                Expression.Call(
                    null,
                    GetMethodInfoOf(() => Queryable.Average(
                        default(IQueryable<int>))),
                    new Expression[] { source.Expression }
                    ));
        }

        public static double? Average(this IQueryable<int?> source)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            return source.Provider.Execute<double?>(
                Expression.Call(
                    null,
                    GetMethodInfoOf(() => Queryable.Average(
                        default(IQueryable<int?>))),
                    new Expression[] { source.Expression }
                    ));
        }

        public static double Average(this IQueryable<long> source)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            return source.Provider.Execute<double>(
                Expression.Call(
                    null,
                    GetMethodInfoOf(() => Queryable.Average(
                        default(IQueryable<long>))),
                    new Expression[] { source.Expression }
                    ));
        }

        public static double? Average(this IQueryable<long?> source)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            return source.Provider.Execute<double?>(
                Expression.Call(
                    null,
                    GetMethodInfoOf(() => Queryable.Average(
                        default(IQueryable<long?>))),
                    new Expression[] { source.Expression }
                    ));
        }

        public static float Average(this IQueryable<float> source)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            return source.Provider.Execute<float>(
                Expression.Call(
                    null,
                    GetMethodInfoOf(() => Queryable.Average(
                        default(IQueryable<float>))),
                    new Expression[] { source.Expression }
                    ));
        }

        public static float? Average(this IQueryable<float?> source)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            return source.Provider.Execute<float?>(
                Expression.Call(
                    null,
                    GetMethodInfoOf(() => Queryable.Average(
                        default(IQueryable<float?>))),
                    new Expression[] { source.Expression }
                    ));
        }

        public static double Average(this IQueryable<double> source)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            return source.Provider.Execute<double>(
                Expression.Call(
                    null,
                    GetMethodInfoOf(() => Queryable.Average(
                        default(IQueryable<double>))),
                    new Expression[] { source.Expression }
                    ));
        }

        public static double? Average(this IQueryable<double?> source)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            return source.Provider.Execute<double?>(
                Expression.Call(
                    null,
                    GetMethodInfoOf(() => Queryable.Average(
                        default(IQueryable<double?>))),
                    new Expression[] { source.Expression }
                    ));
        }

        public static decimal Average(this IQueryable<decimal> source)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            return source.Provider.Execute<decimal>(
                Expression.Call(
                    null,
                    GetMethodInfoOf(() => Queryable.Average(
                        default(IQueryable<decimal>))),
                    new Expression[] { source.Expression }
                    ));
        }

        public static decimal? Average(this IQueryable<decimal?> source)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            return source.Provider.Execute<decimal?>(
                Expression.Call(
                    null,
                    GetMethodInfoOf(() => Queryable.Average(
                        default(IQueryable<decimal?>))),
                    new Expression[] { source.Expression }
                    ));
        }

        public static double Average<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, int>> selector)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (selector == null)
                throw Error.ArgumentNull(nameof(selector));
            return source.Provider.Execute<double>(
                Expression.Call(
                    null,
                    GetMethodInfoOf(() => Queryable.Average(
                        default(IQueryable<TSource>),
                        default(Expression<Func<TSource, int>>))),
                    new Expression[] { source.Expression, Expression.Quote(selector) }
                    ));
        }

        public static double? Average<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, int?>> selector)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (selector == null)
                throw Error.ArgumentNull(nameof(selector));
            return source.Provider.Execute<double?>(
                Expression.Call(
                    null,
                    GetMethodInfoOf(() => Queryable.Average(
                        default(IQueryable<TSource>),
                        default(Expression<Func<TSource, int?>>))),
                    new Expression[] { source.Expression, Expression.Quote(selector) }
                    ));
        }

        public static float Average<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, float>> selector)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (selector == null)
                throw Error.ArgumentNull(nameof(selector));
            return source.Provider.Execute<float>(
                Expression.Call(
                    null,
                    GetMethodInfoOf(() => Queryable.Average(
                        default(IQueryable<TSource>),
                        default(Expression<Func<TSource, float>>))),
                    new Expression[] { source.Expression, Expression.Quote(selector) }
                    ));
        }

        public static float? Average<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, float?>> selector)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (selector == null)
                throw Error.ArgumentNull(nameof(selector));
            return source.Provider.Execute<float?>(
                Expression.Call(
                    null,
                    GetMethodInfoOf(() => Queryable.Average(
                        default(IQueryable<TSource>),
                        default(Expression<Func<TSource, float?>>))),
                    new Expression[] { source.Expression, Expression.Quote(selector) }
                    ));
        }

        public static double Average<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, long>> selector)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (selector == null)
                throw Error.ArgumentNull(nameof(selector));
            return source.Provider.Execute<double>(
                Expression.Call(
                    null,
                    GetMethodInfoOf(() => Queryable.Average(
                        default(IQueryable<TSource>),
                        default(Expression<Func<TSource, long>>))),
                    new Expression[] { source.Expression, Expression.Quote(selector) }
                    ));
        }

        public static double? Average<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, long?>> selector)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (selector == null)
                throw Error.ArgumentNull(nameof(selector));
            return source.Provider.Execute<double?>(
                Expression.Call(
                    null,
                    GetMethodInfoOf(() => Queryable.Average(
                        default(IQueryable<TSource>),
                        default(Expression<Func<TSource, long?>>))),
                    new Expression[] { source.Expression, Expression.Quote(selector) }
                    ));
        }

        public static double Average<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, double>> selector)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (selector == null)
                throw Error.ArgumentNull(nameof(selector));
            return source.Provider.Execute<double>(
                Expression.Call(
                    null,
                    GetMethodInfoOf(() => Queryable.Average(
                        default(IQueryable<TSource>),
                        default(Expression<Func<TSource, double>>))),
                    new Expression[] { source.Expression, Expression.Quote(selector) }
                    ));
        }

        public static double? Average<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, double?>> selector)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (selector == null)
                throw Error.ArgumentNull(nameof(selector));
            return source.Provider.Execute<double?>(
                Expression.Call(
                    null,
                    GetMethodInfoOf(() => Queryable.Average(
                        default(IQueryable<TSource>),
                        default(Expression<Func<TSource, double?>>))),
                    new Expression[] { source.Expression, Expression.Quote(selector) }
                    ));
        }

        public static decimal Average<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, decimal>> selector)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (selector == null)
                throw Error.ArgumentNull(nameof(selector));
            return source.Provider.Execute<decimal>(
                Expression.Call(
                    null,
                    GetMethodInfoOf(() => Queryable.Average(
                        default(IQueryable<TSource>),
                        default(Expression<Func<TSource, decimal>>))),
                    new Expression[] { source.Expression, Expression.Quote(selector) }
                    ));
        }

        public static decimal? Average<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, decimal?>> selector)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (selector == null)
                throw Error.ArgumentNull(nameof(selector));
            return source.Provider.Execute<decimal?>(
                Expression.Call(
                    null,
                    GetMethodInfoOf(() => Queryable.Average(
                        default(IQueryable<TSource>),
                        default(Expression<Func<TSource, decimal?>>))),
                    new Expression[] { source.Expression, Expression.Quote(selector) }
                    ));
        }

        public static TSource Aggregate<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, TSource, TSource>> func)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (func == null)
                throw Error.ArgumentNull(nameof(func));
            return source.Provider.Execute<TSource>(
                Expression.Call(
                    null,
                    GetMethodInfoOf(() => Queryable.Aggregate(
                        default(IQueryable<TSource>),
                        default(Expression<Func<TSource, TSource, TSource>>))),
                    new Expression[] { source.Expression, Expression.Quote(func) }
                    ));
        }

        public static TAccumulate Aggregate<TSource, TAccumulate>(this IQueryable<TSource> source, TAccumulate seed, Expression<Func<TAccumulate, TSource, TAccumulate>> func)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (func == null)
                throw Error.ArgumentNull(nameof(func));
            return source.Provider.Execute<TAccumulate>(
                Expression.Call(
                    null,
                    GetMethodInfoOf(() => Queryable.Aggregate(
                        default(IQueryable<TSource>),
                        default(TAccumulate),
                        default(Expression<Func<TAccumulate, TSource, TAccumulate>>))),
                    new Expression[] { source.Expression, Expression.Constant(seed), Expression.Quote(func) }
                    ));
        }

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
                    GetMethodInfoOf(() => Queryable.Aggregate(
                        default(IQueryable<TSource>),
                        default(TAccumulate),
                        default(Expression<Func<TAccumulate, TSource, TAccumulate>>),
                        default(Expression<Func<TAccumulate, TResult>>))),
                    new Expression[] { source.Expression, Expression.Constant(seed), Expression.Quote(func), Expression.Quote(selector) }
                    ));
        }

        private static MethodInfo GetMethodInfoOf<T>(Expression<Func<T>> expression)
        {
            var body = (MethodCallExpression)expression.Body;
            return body.Method;
        }
    }
}
