// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
            if (source is IQueryable queryable) return queryable;
            Type? enumType = TypeHelper.FindGenericType(typeof(IEnumerable<>), source.GetType());
            if (enumType == null)
                throw Error.ArgumentNotIEnumerableGeneric(nameof(source));
            return EnumerableQuery.Create(enumType.GenericTypeArguments[0], source);
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
                    CachedReflectionInfo.Where_TSource_2(typeof(TSource)),
                    source.Expression, Expression.Quote(predicate)
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
                    CachedReflectionInfo.Where_Index_TSource_2(typeof(TSource)),
                    source.Expression, Expression.Quote(predicate)
                    ));
        }

        public static IQueryable<TResult> OfType<TResult>(this IQueryable source)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            return source.Provider.CreateQuery<TResult>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.OfType_TResult_1(typeof(TResult)), source.Expression));
        }

        public static IQueryable<TResult> Cast<TResult>(this IQueryable source)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            return source.Provider.CreateQuery<TResult>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.Cast_TResult_1(typeof(TResult)), source.Expression));
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
                    CachedReflectionInfo.Select_TSource_TResult_2(typeof(TSource), typeof(TResult)),
                    source.Expression, Expression.Quote(selector)
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
                    CachedReflectionInfo.Select_Index_TSource_TResult_2(typeof(TSource), typeof(TResult)),
                    source.Expression, Expression.Quote(selector)
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
                    CachedReflectionInfo.SelectMany_TSource_TResult_2(typeof(TSource), typeof(TResult)),
                    source.Expression, Expression.Quote(selector)
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
                    CachedReflectionInfo.SelectMany_Index_TSource_TResult_2(typeof(TSource), typeof(TResult)),
                    source.Expression, Expression.Quote(selector)
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
                    CachedReflectionInfo.SelectMany_Index_TSource_TCollection_TResult_3(typeof(TSource), typeof(TCollection), typeof(TResult)),
                    source.Expression, Expression.Quote(collectionSelector), Expression.Quote(resultSelector)
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
                    CachedReflectionInfo.SelectMany_TSource_TCollection_TResult_3(typeof(TSource), typeof(TCollection), typeof(TResult)),
                    source.Expression, Expression.Quote(collectionSelector), Expression.Quote(resultSelector)
                    ));
        }

        private static Expression GetSourceExpression<TSource>(IEnumerable<TSource> source)
        {
            IQueryable<TSource>? q = source as IQueryable<TSource>;
            return q != null ? q.Expression : Expression.Constant(source, typeof(IEnumerable<TSource>));
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
                    CachedReflectionInfo.Join_TOuter_TInner_TKey_TResult_5(typeof(TOuter), typeof(TInner), typeof(TKey), typeof(TResult)), outer.Expression, GetSourceExpression(inner), Expression.Quote(outerKeySelector), Expression.Quote(innerKeySelector), Expression.Quote(resultSelector)));
        }

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

        public static IQueryable<TSource> Take<TSource>(this IQueryable<TSource> source, int count)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            return source.Provider.CreateQuery<TSource>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.Take_TSource_2(typeof(TSource)),
                    source.Expression, Expression.Constant(count)
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
                    CachedReflectionInfo.TakeWhile_TSource_2(typeof(TSource)),
                    source.Expression, Expression.Quote(predicate)
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
                    CachedReflectionInfo.TakeWhile_Index_TSource_2(typeof(TSource)),
                    source.Expression, Expression.Quote(predicate)
                    ));
        }

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

        public static IQueryable<TSource> Distinct<TSource>(this IQueryable<TSource> source)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            return source.Provider.CreateQuery<TSource>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.Distinct_TSource_1(typeof(TSource)), source.Expression));
        }

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

        public static TSource First<TSource>(this IQueryable<TSource> source)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            return source.Provider.Execute<TSource>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.First_TSource_1(typeof(TSource)), source.Expression));
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
                    CachedReflectionInfo.First_TSource_2(typeof(TSource)),
                    source.Expression, Expression.Quote(predicate)
                    ));
        }

        public static TSource? FirstOrDefault<TSource>(this IQueryable<TSource> source)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            return source.Provider.Execute<TSource>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.FirstOrDefault_TSource_1(typeof(TSource)), source.Expression));
        }

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

        public static TSource Last<TSource>(this IQueryable<TSource> source)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            return source.Provider.Execute<TSource>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.Last_TSource_1(typeof(TSource)), source.Expression));
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
                    CachedReflectionInfo.Last_TSource_2(typeof(TSource)),
                    source.Expression, Expression.Quote(predicate)
                    ));
        }

        public static TSource? LastOrDefault<TSource>(this IQueryable<TSource> source)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            return source.Provider.Execute<TSource>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.LastOrDefault_TSource_1(typeof(TSource)), source.Expression));
        }

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

        public static TSource Single<TSource>(this IQueryable<TSource> source)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            return source.Provider.Execute<TSource>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.Single_TSource_1(typeof(TSource)), source.Expression));
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
                    CachedReflectionInfo.Single_TSource_2(typeof(TSource)),
                    source.Expression, Expression.Quote(predicate)
                    ));
        }

        public static TSource? SingleOrDefault<TSource>(this IQueryable<TSource> source)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            return source.Provider.Execute<TSource>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.SingleOrDefault_TSource_1(typeof(TSource)), source.Expression));
        }

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

        public static TSource ElementAt<TSource>(this IQueryable<TSource> source, int index)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (index < 0)
                throw Error.ArgumentOutOfRange(nameof(index));
            return source.Provider.Execute<TSource>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.ElementAt_TSource_2(typeof(TSource)),
                    source.Expression, Expression.Constant(index)
                    ));
        }

        public static TSource? ElementAtOrDefault<TSource>(this IQueryable<TSource> source, int index)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            return source.Provider.Execute<TSource>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.ElementAtOrDefault_TSource_2(typeof(TSource)),
                    source.Expression, Expression.Constant(index)
                    ));
        }

        public static IQueryable<TSource> DefaultIfEmpty<TSource>(this IQueryable<TSource> source)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            return source.Provider.CreateQuery<TSource>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.DefaultIfEmpty_TSource_1(typeof(TSource)), source.Expression));
        }

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

        public static IQueryable<TSource> Reverse<TSource>(this IQueryable<TSource> source)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            return source.Provider.CreateQuery<TSource>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.Reverse_TSource_1(typeof(TSource)), source.Expression));
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
                    CachedReflectionInfo.SequenceEqual_TSource_2(typeof(TSource)),
                    source1.Expression, GetSourceExpression(source2)
                    ));
        }

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

        public static bool Any<TSource>(this IQueryable<TSource> source)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            return source.Provider.Execute<bool>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.Any_TSource_1(typeof(TSource)), source.Expression));
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
                    CachedReflectionInfo.Any_TSource_2(typeof(TSource)),
                    source.Expression, Expression.Quote(predicate)
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
                    CachedReflectionInfo.All_TSource_2(typeof(TSource)),
                    source.Expression, Expression.Quote(predicate)
                    ));
        }

        public static int Count<TSource>(this IQueryable<TSource> source)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            return source.Provider.Execute<int>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.Count_TSource_1(typeof(TSource)), source.Expression));
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
                    CachedReflectionInfo.Count_TSource_2(typeof(TSource)),
                    source.Expression, Expression.Quote(predicate)
                    ));
        }

        public static long LongCount<TSource>(this IQueryable<TSource> source)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            return source.Provider.Execute<long>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.LongCount_TSource_1(typeof(TSource)), source.Expression));
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
                    CachedReflectionInfo.LongCount_TSource_2(typeof(TSource)),
                    source.Expression, Expression.Quote(predicate)
                    ));
        }

        public static TSource? Min<TSource>(this IQueryable<TSource> source)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            return source.Provider.Execute<TSource>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.Min_TSource_1(typeof(TSource)), source.Expression));
        }

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

        public static TSource? Max<TSource>(this IQueryable<TSource> source)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            return source.Provider.Execute<TSource>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.Max_TSource_1(typeof(TSource)), source.Expression));
        }

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

        public static int Sum(this IQueryable<int> source)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            return source.Provider.Execute<int>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.Sum_Int32_1, source.Expression));
        }

        public static int? Sum(this IQueryable<int?> source)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            return source.Provider.Execute<int?>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.Sum_NullableInt32_1, source.Expression));
        }

        public static long Sum(this IQueryable<long> source)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            return source.Provider.Execute<long>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.Sum_Int64_1, source.Expression));
        }

        public static long? Sum(this IQueryable<long?> source)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            return source.Provider.Execute<long?>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.Sum_NullableInt64_1, source.Expression));
        }

        public static float Sum(this IQueryable<float> source)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            return source.Provider.Execute<float>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.Sum_Single_1, source.Expression));
        }

        public static float? Sum(this IQueryable<float?> source)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            return source.Provider.Execute<float?>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.Sum_NullableSingle_1, source.Expression));
        }

        public static double Sum(this IQueryable<double> source)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            return source.Provider.Execute<double>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.Sum_Double_1, source.Expression));
        }

        public static double? Sum(this IQueryable<double?> source)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            return source.Provider.Execute<double?>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.Sum_NullableDouble_1, source.Expression));
        }

        public static decimal Sum(this IQueryable<decimal> source)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            return source.Provider.Execute<decimal>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.Sum_Decimal_1, source.Expression));
        }

        public static decimal? Sum(this IQueryable<decimal?> source)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            return source.Provider.Execute<decimal?>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.Sum_NullableDecimal_1, source.Expression));
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
                    CachedReflectionInfo.Sum_Int32_TSource_2(typeof(TSource)),
                    source.Expression, Expression.Quote(selector)
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
                    CachedReflectionInfo.Sum_NullableInt32_TSource_2(typeof(TSource)),
                    source.Expression, Expression.Quote(selector)
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
                    CachedReflectionInfo.Sum_Int64_TSource_2(typeof(TSource)),
                    source.Expression, Expression.Quote(selector)
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
                    CachedReflectionInfo.Sum_NullableInt64_TSource_2(typeof(TSource)),
                    source.Expression, Expression.Quote(selector)
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
                    CachedReflectionInfo.Sum_Single_TSource_2(typeof(TSource)),
                    source.Expression, Expression.Quote(selector)
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
                    CachedReflectionInfo.Sum_NullableSingle_TSource_2(typeof(TSource)),
                    source.Expression, Expression.Quote(selector)
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
                    CachedReflectionInfo.Sum_Double_TSource_2(typeof(TSource)),
                    source.Expression, Expression.Quote(selector)
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
                    CachedReflectionInfo.Sum_NullableDouble_TSource_2(typeof(TSource)),
                    source.Expression, Expression.Quote(selector)
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
                    CachedReflectionInfo.Sum_Decimal_TSource_2(typeof(TSource)),
                    source.Expression, Expression.Quote(selector)
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
                    CachedReflectionInfo.Sum_NullableDecimal_TSource_2(typeof(TSource)),
                    source.Expression, Expression.Quote(selector)
                    ));
        }

        public static double Average(this IQueryable<int> source)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            return source.Provider.Execute<double>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.Average_Int32_1, source.Expression));
        }

        public static double? Average(this IQueryable<int?> source)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            return source.Provider.Execute<double?>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.Average_NullableInt32_1, source.Expression));
        }

        public static double Average(this IQueryable<long> source)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            return source.Provider.Execute<double>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.Average_Int64_1, source.Expression));
        }

        public static double? Average(this IQueryable<long?> source)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            return source.Provider.Execute<double?>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.Average_NullableInt64_1, source.Expression));
        }

        public static float Average(this IQueryable<float> source)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            return source.Provider.Execute<float>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.Average_Single_1, source.Expression));
        }

        public static float? Average(this IQueryable<float?> source)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            return source.Provider.Execute<float?>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.Average_NullableSingle_1, source.Expression));
        }

        public static double Average(this IQueryable<double> source)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            return source.Provider.Execute<double>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.Average_Double_1, source.Expression));
        }

        public static double? Average(this IQueryable<double?> source)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            return source.Provider.Execute<double?>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.Average_NullableDouble_1, source.Expression));
        }

        public static decimal Average(this IQueryable<decimal> source)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            return source.Provider.Execute<decimal>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.Average_Decimal_1, source.Expression));
        }

        public static decimal? Average(this IQueryable<decimal?> source)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            return source.Provider.Execute<decimal?>(
                Expression.Call(
                    null,
                    CachedReflectionInfo.Average_NullableDecimal_1, source.Expression));
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
                    CachedReflectionInfo.Average_Int32_TSource_2(typeof(TSource)),
                    source.Expression, Expression.Quote(selector)
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
                    CachedReflectionInfo.Average_NullableInt32_TSource_2(typeof(TSource)),
                    source.Expression, Expression.Quote(selector)
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
                    CachedReflectionInfo.Average_Single_TSource_2(typeof(TSource)),
                    source.Expression, Expression.Quote(selector)
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
                    CachedReflectionInfo.Average_NullableSingle_TSource_2(typeof(TSource)),
                    source.Expression, Expression.Quote(selector)
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
                    CachedReflectionInfo.Average_Int64_TSource_2(typeof(TSource)),
                    source.Expression, Expression.Quote(selector)
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
                    CachedReflectionInfo.Average_NullableInt64_TSource_2(typeof(TSource)),
                    source.Expression, Expression.Quote(selector)
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
                    CachedReflectionInfo.Average_Double_TSource_2(typeof(TSource)),
                    source.Expression, Expression.Quote(selector)
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
                    CachedReflectionInfo.Average_NullableDouble_TSource_2(typeof(TSource)),
                    source.Expression, Expression.Quote(selector)
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
                    CachedReflectionInfo.Average_Decimal_TSource_2(typeof(TSource)),
                    source.Expression, Expression.Quote(selector)
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
                    CachedReflectionInfo.Average_NullableDecimal_TSource_2(typeof(TSource)),
                    source.Expression, Expression.Quote(selector)
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
                    CachedReflectionInfo.Aggregate_TSource_2(typeof(TSource)),
                    source.Expression, Expression.Quote(func)
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
                    CachedReflectionInfo.Aggregate_TSource_TAccumulate_3(typeof(TSource), typeof(TAccumulate)),
                    source.Expression, Expression.Constant(seed), Expression.Quote(func)
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
                    CachedReflectionInfo.Aggregate_TSource_TAccumulate_TResult_4(typeof(TSource), typeof(TAccumulate), typeof(TResult)), source.Expression, Expression.Constant(seed), Expression.Quote(func), Expression.Quote(selector)));
        }

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
