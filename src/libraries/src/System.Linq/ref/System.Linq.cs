// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


namespace System.Linq
{
    public static partial class Enumerable
    {
        public static TSource Aggregate<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, TSource, TSource> func) { return default(TSource); }
        public static TAccumulate Aggregate<TSource, TAccumulate>(this System.Collections.Generic.IEnumerable<TSource> source, TAccumulate seed, System.Func<TAccumulate, TSource, TAccumulate> func) { return default(TAccumulate); }
        public static TResult Aggregate<TSource, TAccumulate, TResult>(this System.Collections.Generic.IEnumerable<TSource> source, TAccumulate seed, System.Func<TAccumulate, TSource, TAccumulate> func, System.Func<TAccumulate, TResult> resultSelector) { return default(TResult); }
        public static bool All<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, bool> predicate) { return default(bool); }
        public static bool Any<TSource>(this System.Collections.Generic.IEnumerable<TSource> source) { return default(bool); }
        public static bool Any<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, bool> predicate) { return default(bool); }
        public static System.Collections.Generic.IEnumerable<TSource> AsEnumerable<TSource>(this System.Collections.Generic.IEnumerable<TSource> source) { return default(System.Collections.Generic.IEnumerable<TSource>); }
        public static decimal Average(this System.Collections.Generic.IEnumerable<decimal> source) { return default(decimal); }
        public static double Average(this System.Collections.Generic.IEnumerable<double> source) { return default(double); }
        public static double Average(this System.Collections.Generic.IEnumerable<int> source) { return default(double); }
        public static double Average(this System.Collections.Generic.IEnumerable<long> source) { return default(double); }
        public static System.Nullable<decimal> Average(this System.Collections.Generic.IEnumerable<System.Nullable<decimal>> source) { return default(System.Nullable<decimal>); }
        public static System.Nullable<double> Average(this System.Collections.Generic.IEnumerable<System.Nullable<double>> source) { return default(System.Nullable<double>); }
        public static System.Nullable<double> Average(this System.Collections.Generic.IEnumerable<System.Nullable<int>> source) { return default(System.Nullable<double>); }
        public static System.Nullable<double> Average(this System.Collections.Generic.IEnumerable<System.Nullable<long>> source) { return default(System.Nullable<double>); }
        public static System.Nullable<float> Average(this System.Collections.Generic.IEnumerable<System.Nullable<float>> source) { return default(System.Nullable<float>); }
        public static float Average(this System.Collections.Generic.IEnumerable<float> source) { return default(float); }
        public static decimal Average<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, decimal> selector) { return default(decimal); }
        public static double Average<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, double> selector) { return default(double); }
        public static double Average<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, int> selector) { return default(double); }
        public static double Average<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, long> selector) { return default(double); }
        public static System.Nullable<decimal> Average<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, System.Nullable<decimal>> selector) { return default(System.Nullable<decimal>); }
        public static System.Nullable<double> Average<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, System.Nullable<double>> selector) { return default(System.Nullable<double>); }
        public static System.Nullable<double> Average<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, System.Nullable<int>> selector) { return default(System.Nullable<double>); }
        public static System.Nullable<double> Average<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, System.Nullable<long>> selector) { return default(System.Nullable<double>); }
        public static System.Nullable<float> Average<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, System.Nullable<float>> selector) { return default(System.Nullable<float>); }
        public static float Average<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, float> selector) { return default(float); }
        public static System.Collections.Generic.IEnumerable<TResult> Cast<TResult>(this System.Collections.IEnumerable source) { return default(System.Collections.Generic.IEnumerable<TResult>); }
        public static System.Collections.Generic.IEnumerable<TSource> Concat<TSource>(this System.Collections.Generic.IEnumerable<TSource> first, System.Collections.Generic.IEnumerable<TSource> second) { return default(System.Collections.Generic.IEnumerable<TSource>); }
        public static System.Collections.Generic.IEnumerable<TSource> Append<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, TSource element) { return default(System.Collections.Generic.IEnumerable<TSource>); }
        public static System.Collections.Generic.IEnumerable<TSource> Prepend<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, TSource element) { return default(System.Collections.Generic.IEnumerable<TSource>); }
        public static bool Contains<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, TSource value) { return default(bool); }
        public static bool Contains<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, TSource value, System.Collections.Generic.IEqualityComparer<TSource> comparer) { return default(bool); }
        public static int Count<TSource>(this System.Collections.Generic.IEnumerable<TSource> source) { return default(int); }
        public static int Count<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, bool> predicate) { return default(int); }
        public static System.Collections.Generic.IEnumerable<TSource> DefaultIfEmpty<TSource>(this System.Collections.Generic.IEnumerable<TSource> source) { return default(System.Collections.Generic.IEnumerable<TSource>); }
        public static System.Collections.Generic.IEnumerable<TSource> DefaultIfEmpty<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, TSource defaultValue) { return default(System.Collections.Generic.IEnumerable<TSource>); }
        public static System.Collections.Generic.IEnumerable<TSource> Distinct<TSource>(this System.Collections.Generic.IEnumerable<TSource> source) { return default(System.Collections.Generic.IEnumerable<TSource>); }
        public static System.Collections.Generic.IEnumerable<TSource> Distinct<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Collections.Generic.IEqualityComparer<TSource> comparer) { return default(System.Collections.Generic.IEnumerable<TSource>); }
        public static TSource ElementAt<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, int index) { return default(TSource); }
        public static TSource ElementAtOrDefault<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, int index) { return default(TSource); }
        public static System.Collections.Generic.IEnumerable<TResult> Empty<TResult>() { return default(System.Collections.Generic.IEnumerable<TResult>); }
        public static System.Collections.Generic.IEnumerable<TSource> Except<TSource>(this System.Collections.Generic.IEnumerable<TSource> first, System.Collections.Generic.IEnumerable<TSource> second) { return default(System.Collections.Generic.IEnumerable<TSource>); }
        public static System.Collections.Generic.IEnumerable<TSource> Except<TSource>(this System.Collections.Generic.IEnumerable<TSource> first, System.Collections.Generic.IEnumerable<TSource> second, System.Collections.Generic.IEqualityComparer<TSource> comparer) { return default(System.Collections.Generic.IEnumerable<TSource>); }
        public static TSource First<TSource>(this System.Collections.Generic.IEnumerable<TSource> source) { return default(TSource); }
        public static TSource First<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, bool> predicate) { return default(TSource); }
        public static TSource FirstOrDefault<TSource>(this System.Collections.Generic.IEnumerable<TSource> source) { return default(TSource); }
        public static TSource FirstOrDefault<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, bool> predicate) { return default(TSource); }
        public static System.Collections.Generic.IEnumerable<System.Linq.IGrouping<TKey, TSource>> GroupBy<TSource, TKey>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, TKey> keySelector) { return default(System.Collections.Generic.IEnumerable<System.Linq.IGrouping<TKey, TSource>>); }
        public static System.Collections.Generic.IEnumerable<System.Linq.IGrouping<TKey, TSource>> GroupBy<TSource, TKey>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, TKey> keySelector, System.Collections.Generic.IEqualityComparer<TKey> comparer) { return default(System.Collections.Generic.IEnumerable<System.Linq.IGrouping<TKey, TSource>>); }
        public static System.Collections.Generic.IEnumerable<System.Linq.IGrouping<TKey, TElement>> GroupBy<TSource, TKey, TElement>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, TKey> keySelector, System.Func<TSource, TElement> elementSelector) { return default(System.Collections.Generic.IEnumerable<System.Linq.IGrouping<TKey, TElement>>); }
        public static System.Collections.Generic.IEnumerable<System.Linq.IGrouping<TKey, TElement>> GroupBy<TSource, TKey, TElement>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, TKey> keySelector, System.Func<TSource, TElement> elementSelector, System.Collections.Generic.IEqualityComparer<TKey> comparer) { return default(System.Collections.Generic.IEnumerable<System.Linq.IGrouping<TKey, TElement>>); }
        public static System.Collections.Generic.IEnumerable<TResult> GroupBy<TSource, TKey, TResult>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, TKey> keySelector, System.Func<TKey, System.Collections.Generic.IEnumerable<TSource>, TResult> resultSelector) { return default(System.Collections.Generic.IEnumerable<TResult>); }
        public static System.Collections.Generic.IEnumerable<TResult> GroupBy<TSource, TKey, TResult>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, TKey> keySelector, System.Func<TKey, System.Collections.Generic.IEnumerable<TSource>, TResult> resultSelector, System.Collections.Generic.IEqualityComparer<TKey> comparer) { return default(System.Collections.Generic.IEnumerable<TResult>); }
        public static System.Collections.Generic.IEnumerable<TResult> GroupBy<TSource, TKey, TElement, TResult>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, TKey> keySelector, System.Func<TSource, TElement> elementSelector, System.Func<TKey, System.Collections.Generic.IEnumerable<TElement>, TResult> resultSelector) { return default(System.Collections.Generic.IEnumerable<TResult>); }
        public static System.Collections.Generic.IEnumerable<TResult> GroupBy<TSource, TKey, TElement, TResult>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, TKey> keySelector, System.Func<TSource, TElement> elementSelector, System.Func<TKey, System.Collections.Generic.IEnumerable<TElement>, TResult> resultSelector, System.Collections.Generic.IEqualityComparer<TKey> comparer) { return default(System.Collections.Generic.IEnumerable<TResult>); }
        public static System.Collections.Generic.IEnumerable<TResult> GroupJoin<TOuter, TInner, TKey, TResult>(this System.Collections.Generic.IEnumerable<TOuter> outer, System.Collections.Generic.IEnumerable<TInner> inner, System.Func<TOuter, TKey> outerKeySelector, System.Func<TInner, TKey> innerKeySelector, System.Func<TOuter, System.Collections.Generic.IEnumerable<TInner>, TResult> resultSelector) { return default(System.Collections.Generic.IEnumerable<TResult>); }
        public static System.Collections.Generic.IEnumerable<TResult> GroupJoin<TOuter, TInner, TKey, TResult>(this System.Collections.Generic.IEnumerable<TOuter> outer, System.Collections.Generic.IEnumerable<TInner> inner, System.Func<TOuter, TKey> outerKeySelector, System.Func<TInner, TKey> innerKeySelector, System.Func<TOuter, System.Collections.Generic.IEnumerable<TInner>, TResult> resultSelector, System.Collections.Generic.IEqualityComparer<TKey> comparer) { return default(System.Collections.Generic.IEnumerable<TResult>); }
        public static System.Collections.Generic.IEnumerable<TSource> Intersect<TSource>(this System.Collections.Generic.IEnumerable<TSource> first, System.Collections.Generic.IEnumerable<TSource> second) { return default(System.Collections.Generic.IEnumerable<TSource>); }
        public static System.Collections.Generic.IEnumerable<TSource> Intersect<TSource>(this System.Collections.Generic.IEnumerable<TSource> first, System.Collections.Generic.IEnumerable<TSource> second, System.Collections.Generic.IEqualityComparer<TSource> comparer) { return default(System.Collections.Generic.IEnumerable<TSource>); }
        public static System.Collections.Generic.IEnumerable<TResult> Join<TOuter, TInner, TKey, TResult>(this System.Collections.Generic.IEnumerable<TOuter> outer, System.Collections.Generic.IEnumerable<TInner> inner, System.Func<TOuter, TKey> outerKeySelector, System.Func<TInner, TKey> innerKeySelector, System.Func<TOuter, TInner, TResult> resultSelector) { return default(System.Collections.Generic.IEnumerable<TResult>); }
        public static System.Collections.Generic.IEnumerable<TResult> Join<TOuter, TInner, TKey, TResult>(this System.Collections.Generic.IEnumerable<TOuter> outer, System.Collections.Generic.IEnumerable<TInner> inner, System.Func<TOuter, TKey> outerKeySelector, System.Func<TInner, TKey> innerKeySelector, System.Func<TOuter, TInner, TResult> resultSelector, System.Collections.Generic.IEqualityComparer<TKey> comparer) { return default(System.Collections.Generic.IEnumerable<TResult>); }
        public static TSource Last<TSource>(this System.Collections.Generic.IEnumerable<TSource> source) { return default(TSource); }
        public static TSource Last<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, bool> predicate) { return default(TSource); }
        public static TSource LastOrDefault<TSource>(this System.Collections.Generic.IEnumerable<TSource> source) { return default(TSource); }
        public static TSource LastOrDefault<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, bool> predicate) { return default(TSource); }
        public static long LongCount<TSource>(this System.Collections.Generic.IEnumerable<TSource> source) { return default(long); }
        public static long LongCount<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, bool> predicate) { return default(long); }
        public static decimal Max(this System.Collections.Generic.IEnumerable<decimal> source) { return default(decimal); }
        public static double Max(this System.Collections.Generic.IEnumerable<double> source) { return default(double); }
        public static int Max(this System.Collections.Generic.IEnumerable<int> source) { return default(int); }
        public static long Max(this System.Collections.Generic.IEnumerable<long> source) { return default(long); }
        public static System.Nullable<decimal> Max(this System.Collections.Generic.IEnumerable<System.Nullable<decimal>> source) { return default(System.Nullable<decimal>); }
        public static System.Nullable<double> Max(this System.Collections.Generic.IEnumerable<System.Nullable<double>> source) { return default(System.Nullable<double>); }
        public static System.Nullable<int> Max(this System.Collections.Generic.IEnumerable<System.Nullable<int>> source) { return default(System.Nullable<int>); }
        public static System.Nullable<long> Max(this System.Collections.Generic.IEnumerable<System.Nullable<long>> source) { return default(System.Nullable<long>); }
        public static System.Nullable<float> Max(this System.Collections.Generic.IEnumerable<System.Nullable<float>> source) { return default(System.Nullable<float>); }
        public static float Max(this System.Collections.Generic.IEnumerable<float> source) { return default(float); }
        public static TSource Max<TSource>(this System.Collections.Generic.IEnumerable<TSource> source) { return default(TSource); }
        public static decimal Max<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, decimal> selector) { return default(decimal); }
        public static double Max<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, double> selector) { return default(double); }
        public static int Max<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, int> selector) { return default(int); }
        public static long Max<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, long> selector) { return default(long); }
        public static System.Nullable<decimal> Max<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, System.Nullable<decimal>> selector) { return default(System.Nullable<decimal>); }
        public static System.Nullable<double> Max<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, System.Nullable<double>> selector) { return default(System.Nullable<double>); }
        public static System.Nullable<int> Max<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, System.Nullable<int>> selector) { return default(System.Nullable<int>); }
        public static System.Nullable<long> Max<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, System.Nullable<long>> selector) { return default(System.Nullable<long>); }
        public static System.Nullable<float> Max<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, System.Nullable<float>> selector) { return default(System.Nullable<float>); }
        public static float Max<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, float> selector) { return default(float); }
        public static TResult Max<TSource, TResult>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, TResult> selector) { return default(TResult); }
        public static decimal Min(this System.Collections.Generic.IEnumerable<decimal> source) { return default(decimal); }
        public static double Min(this System.Collections.Generic.IEnumerable<double> source) { return default(double); }
        public static int Min(this System.Collections.Generic.IEnumerable<int> source) { return default(int); }
        public static long Min(this System.Collections.Generic.IEnumerable<long> source) { return default(long); }
        public static System.Nullable<decimal> Min(this System.Collections.Generic.IEnumerable<System.Nullable<decimal>> source) { return default(System.Nullable<decimal>); }
        public static System.Nullable<double> Min(this System.Collections.Generic.IEnumerable<System.Nullable<double>> source) { return default(System.Nullable<double>); }
        public static System.Nullable<int> Min(this System.Collections.Generic.IEnumerable<System.Nullable<int>> source) { return default(System.Nullable<int>); }
        public static System.Nullable<long> Min(this System.Collections.Generic.IEnumerable<System.Nullable<long>> source) { return default(System.Nullable<long>); }
        public static System.Nullable<float> Min(this System.Collections.Generic.IEnumerable<System.Nullable<float>> source) { return default(System.Nullable<float>); }
        public static float Min(this System.Collections.Generic.IEnumerable<float> source) { return default(float); }
        public static TSource Min<TSource>(this System.Collections.Generic.IEnumerable<TSource> source) { return default(TSource); }
        public static decimal Min<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, decimal> selector) { return default(decimal); }
        public static double Min<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, double> selector) { return default(double); }
        public static int Min<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, int> selector) { return default(int); }
        public static long Min<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, long> selector) { return default(long); }
        public static System.Nullable<decimal> Min<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, System.Nullable<decimal>> selector) { return default(System.Nullable<decimal>); }
        public static System.Nullable<double> Min<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, System.Nullable<double>> selector) { return default(System.Nullable<double>); }
        public static System.Nullable<int> Min<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, System.Nullable<int>> selector) { return default(System.Nullable<int>); }
        public static System.Nullable<long> Min<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, System.Nullable<long>> selector) { return default(System.Nullable<long>); }
        public static System.Nullable<float> Min<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, System.Nullable<float>> selector) { return default(System.Nullable<float>); }
        public static float Min<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, float> selector) { return default(float); }
        public static TResult Min<TSource, TResult>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, TResult> selector) { return default(TResult); }
        public static System.Collections.Generic.IEnumerable<TResult> OfType<TResult>(this System.Collections.IEnumerable source) { return default(System.Collections.Generic.IEnumerable<TResult>); }
        public static System.Linq.IOrderedEnumerable<TSource> OrderBy<TSource, TKey>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, TKey> keySelector) { return default(System.Linq.IOrderedEnumerable<TSource>); }
        public static System.Linq.IOrderedEnumerable<TSource> OrderBy<TSource, TKey>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, TKey> keySelector, System.Collections.Generic.IComparer<TKey> comparer) { return default(System.Linq.IOrderedEnumerable<TSource>); }
        public static System.Linq.IOrderedEnumerable<TSource> OrderByDescending<TSource, TKey>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, TKey> keySelector) { return default(System.Linq.IOrderedEnumerable<TSource>); }
        public static System.Linq.IOrderedEnumerable<TSource> OrderByDescending<TSource, TKey>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, TKey> keySelector, System.Collections.Generic.IComparer<TKey> comparer) { return default(System.Linq.IOrderedEnumerable<TSource>); }
        public static System.Collections.Generic.IEnumerable<int> Range(int start, int count) { return default(System.Collections.Generic.IEnumerable<int>); }
        public static System.Collections.Generic.IEnumerable<TResult> Repeat<TResult>(TResult element, int count) { return default(System.Collections.Generic.IEnumerable<TResult>); }
        public static System.Collections.Generic.IEnumerable<TSource> Reverse<TSource>(this System.Collections.Generic.IEnumerable<TSource> source) { return default(System.Collections.Generic.IEnumerable<TSource>); }
        public static System.Collections.Generic.IEnumerable<TResult> Select<TSource, TResult>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, TResult> selector) { return default(System.Collections.Generic.IEnumerable<TResult>); }
        public static System.Collections.Generic.IEnumerable<TResult> Select<TSource, TResult>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, int, TResult> selector) { return default(System.Collections.Generic.IEnumerable<TResult>); }
        public static System.Collections.Generic.IEnumerable<TResult> SelectMany<TSource, TResult>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, System.Collections.Generic.IEnumerable<TResult>> selector) { return default(System.Collections.Generic.IEnumerable<TResult>); }
        public static System.Collections.Generic.IEnumerable<TResult> SelectMany<TSource, TResult>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, int, System.Collections.Generic.IEnumerable<TResult>> selector) { return default(System.Collections.Generic.IEnumerable<TResult>); }
        public static System.Collections.Generic.IEnumerable<TResult> SelectMany<TSource, TCollection, TResult>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, System.Collections.Generic.IEnumerable<TCollection>> collectionSelector, System.Func<TSource, TCollection, TResult> resultSelector) { return default(System.Collections.Generic.IEnumerable<TResult>); }
        public static System.Collections.Generic.IEnumerable<TResult> SelectMany<TSource, TCollection, TResult>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, int, System.Collections.Generic.IEnumerable<TCollection>> collectionSelector, System.Func<TSource, TCollection, TResult> resultSelector) { return default(System.Collections.Generic.IEnumerable<TResult>); }
        public static bool SequenceEqual<TSource>(this System.Collections.Generic.IEnumerable<TSource> first, System.Collections.Generic.IEnumerable<TSource> second) { return default(bool); }
        public static bool SequenceEqual<TSource>(this System.Collections.Generic.IEnumerable<TSource> first, System.Collections.Generic.IEnumerable<TSource> second, System.Collections.Generic.IEqualityComparer<TSource> comparer) { return default(bool); }
        public static TSource Single<TSource>(this System.Collections.Generic.IEnumerable<TSource> source) { return default(TSource); }
        public static TSource Single<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, bool> predicate) { return default(TSource); }
        public static TSource SingleOrDefault<TSource>(this System.Collections.Generic.IEnumerable<TSource> source) { return default(TSource); }
        public static TSource SingleOrDefault<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, bool> predicate) { return default(TSource); }
        public static System.Collections.Generic.IEnumerable<TSource> Skip<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, int count) { return default(System.Collections.Generic.IEnumerable<TSource>); }
        public static System.Collections.Generic.IEnumerable<TSource> SkipWhile<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, bool> predicate) { return default(System.Collections.Generic.IEnumerable<TSource>); }
        public static System.Collections.Generic.IEnumerable<TSource> SkipWhile<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, int, bool> predicate) { return default(System.Collections.Generic.IEnumerable<TSource>); }
        public static decimal Sum(this System.Collections.Generic.IEnumerable<decimal> source) { return default(decimal); }
        public static double Sum(this System.Collections.Generic.IEnumerable<double> source) { return default(double); }
        public static int Sum(this System.Collections.Generic.IEnumerable<int> source) { return default(int); }
        public static long Sum(this System.Collections.Generic.IEnumerable<long> source) { return default(long); }
        public static System.Nullable<decimal> Sum(this System.Collections.Generic.IEnumerable<System.Nullable<decimal>> source) { return default(System.Nullable<decimal>); }
        public static System.Nullable<double> Sum(this System.Collections.Generic.IEnumerable<System.Nullable<double>> source) { return default(System.Nullable<double>); }
        public static System.Nullable<int> Sum(this System.Collections.Generic.IEnumerable<System.Nullable<int>> source) { return default(System.Nullable<int>); }
        public static System.Nullable<long> Sum(this System.Collections.Generic.IEnumerable<System.Nullable<long>> source) { return default(System.Nullable<long>); }
        public static System.Nullable<float> Sum(this System.Collections.Generic.IEnumerable<System.Nullable<float>> source) { return default(System.Nullable<float>); }
        public static float Sum(this System.Collections.Generic.IEnumerable<float> source) { return default(float); }
        public static decimal Sum<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, decimal> selector) { return default(decimal); }
        public static double Sum<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, double> selector) { return default(double); }
        public static int Sum<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, int> selector) { return default(int); }
        public static long Sum<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, long> selector) { return default(long); }
        public static System.Nullable<decimal> Sum<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, System.Nullable<decimal>> selector) { return default(System.Nullable<decimal>); }
        public static System.Nullable<double> Sum<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, System.Nullable<double>> selector) { return default(System.Nullable<double>); }
        public static System.Nullable<int> Sum<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, System.Nullable<int>> selector) { return default(System.Nullable<int>); }
        public static System.Nullable<long> Sum<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, System.Nullable<long>> selector) { return default(System.Nullable<long>); }
        public static System.Nullable<float> Sum<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, System.Nullable<float>> selector) { return default(System.Nullable<float>); }
        public static float Sum<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, float> selector) { return default(float); }
        public static System.Collections.Generic.IEnumerable<TSource> Take<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, int count) { return default(System.Collections.Generic.IEnumerable<TSource>); }
        public static System.Collections.Generic.IEnumerable<TSource> TakeWhile<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, bool> predicate) { return default(System.Collections.Generic.IEnumerable<TSource>); }
        public static System.Collections.Generic.IEnumerable<TSource> TakeWhile<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, int, bool> predicate) { return default(System.Collections.Generic.IEnumerable<TSource>); }
        public static System.Linq.IOrderedEnumerable<TSource> ThenBy<TSource, TKey>(this System.Linq.IOrderedEnumerable<TSource> source, System.Func<TSource, TKey> keySelector) { return default(System.Linq.IOrderedEnumerable<TSource>); }
        public static System.Linq.IOrderedEnumerable<TSource> ThenBy<TSource, TKey>(this System.Linq.IOrderedEnumerable<TSource> source, System.Func<TSource, TKey> keySelector, System.Collections.Generic.IComparer<TKey> comparer) { return default(System.Linq.IOrderedEnumerable<TSource>); }
        public static System.Linq.IOrderedEnumerable<TSource> ThenByDescending<TSource, TKey>(this System.Linq.IOrderedEnumerable<TSource> source, System.Func<TSource, TKey> keySelector) { return default(System.Linq.IOrderedEnumerable<TSource>); }
        public static System.Linq.IOrderedEnumerable<TSource> ThenByDescending<TSource, TKey>(this System.Linq.IOrderedEnumerable<TSource> source, System.Func<TSource, TKey> keySelector, System.Collections.Generic.IComparer<TKey> comparer) { return default(System.Linq.IOrderedEnumerable<TSource>); }
        public static TSource[] ToArray<TSource>(this System.Collections.Generic.IEnumerable<TSource> source) { return default(TSource[]); }
        public static System.Collections.Generic.Dictionary<TKey, TSource> ToDictionary<TSource, TKey>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, TKey> keySelector) { return default(System.Collections.Generic.Dictionary<TKey, TSource>); }
        public static System.Collections.Generic.Dictionary<TKey, TSource> ToDictionary<TSource, TKey>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, TKey> keySelector, System.Collections.Generic.IEqualityComparer<TKey> comparer) { return default(System.Collections.Generic.Dictionary<TKey, TSource>); }
        public static System.Collections.Generic.Dictionary<TKey, TElement> ToDictionary<TSource, TKey, TElement>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, TKey> keySelector, System.Func<TSource, TElement> elementSelector) { return default(System.Collections.Generic.Dictionary<TKey, TElement>); }
        public static System.Collections.Generic.Dictionary<TKey, TElement> ToDictionary<TSource, TKey, TElement>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, TKey> keySelector, System.Func<TSource, TElement> elementSelector, System.Collections.Generic.IEqualityComparer<TKey> comparer) { return default(System.Collections.Generic.Dictionary<TKey, TElement>); }
        public static System.Collections.Generic.List<TSource> ToList<TSource>(this System.Collections.Generic.IEnumerable<TSource> source) { return default(System.Collections.Generic.List<TSource>); }
        public static System.Linq.ILookup<TKey, TSource> ToLookup<TSource, TKey>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, TKey> keySelector) { return default(System.Linq.ILookup<TKey, TSource>); }
        public static System.Linq.ILookup<TKey, TSource> ToLookup<TSource, TKey>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, TKey> keySelector, System.Collections.Generic.IEqualityComparer<TKey> comparer) { return default(System.Linq.ILookup<TKey, TSource>); }
        public static System.Linq.ILookup<TKey, TElement> ToLookup<TSource, TKey, TElement>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, TKey> keySelector, System.Func<TSource, TElement> elementSelector) { return default(System.Linq.ILookup<TKey, TElement>); }
        public static System.Linq.ILookup<TKey, TElement> ToLookup<TSource, TKey, TElement>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, TKey> keySelector, System.Func<TSource, TElement> elementSelector, System.Collections.Generic.IEqualityComparer<TKey> comparer) { return default(System.Linq.ILookup<TKey, TElement>); }
        public static System.Collections.Generic.IEnumerable<TSource> Union<TSource>(this System.Collections.Generic.IEnumerable<TSource> first, System.Collections.Generic.IEnumerable<TSource> second) { return default(System.Collections.Generic.IEnumerable<TSource>); }
        public static System.Collections.Generic.IEnumerable<TSource> Union<TSource>(this System.Collections.Generic.IEnumerable<TSource> first, System.Collections.Generic.IEnumerable<TSource> second, System.Collections.Generic.IEqualityComparer<TSource> comparer) { return default(System.Collections.Generic.IEnumerable<TSource>); }
        public static System.Collections.Generic.IEnumerable<TSource> Where<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, bool> predicate) { return default(System.Collections.Generic.IEnumerable<TSource>); }
        public static System.Collections.Generic.IEnumerable<TSource> Where<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, int, bool> predicate) { return default(System.Collections.Generic.IEnumerable<TSource>); }
        public static System.Collections.Generic.IEnumerable<TResult> Zip<TFirst, TSecond, TResult>(this System.Collections.Generic.IEnumerable<TFirst> first, System.Collections.Generic.IEnumerable<TSecond> second, System.Func<TFirst, TSecond, TResult> resultSelector) { return default(System.Collections.Generic.IEnumerable<TResult>); }
    }
    public partial interface IGrouping<out TKey, out TElement> : System.Collections.Generic.IEnumerable<TElement>, System.Collections.IEnumerable
    {
        TKey Key { get; }
    }
    public partial interface ILookup<TKey, TElement> : System.Collections.Generic.IEnumerable<System.Linq.IGrouping<TKey, TElement>>, System.Collections.IEnumerable
    {
        int Count { get; }
        System.Collections.Generic.IEnumerable<TElement> this[TKey key] { get; }
        bool Contains(TKey key);
    }
    public partial interface IOrderedEnumerable<TElement> : System.Collections.Generic.IEnumerable<TElement>, System.Collections.IEnumerable
    {
        System.Linq.IOrderedEnumerable<TElement> CreateOrderedEnumerable<TKey>(System.Func<TElement, TKey> keySelector, System.Collections.Generic.IComparer<TKey> comparer, bool descending);
    }
    public partial class Lookup<TKey, TElement> : System.Collections.Generic.IEnumerable<System.Linq.IGrouping<TKey, TElement>>, System.Collections.IEnumerable, System.Linq.ILookup<TKey, TElement>
    {
        internal Lookup() { }
        public int Count { get { return default(int); } }
        public System.Collections.Generic.IEnumerable<TElement> this[TKey key] { get { return default(System.Collections.Generic.IEnumerable<TElement>); } }
        public System.Collections.Generic.IEnumerable<TResult> ApplyResultSelector<TResult>(System.Func<TKey, System.Collections.Generic.IEnumerable<TElement>, TResult> resultSelector) { return default(System.Collections.Generic.IEnumerable<TResult>); }
        public bool Contains(TKey key) { return default(bool); }
        public System.Collections.Generic.IEnumerator<System.Linq.IGrouping<TKey, TElement>> GetEnumerator() { return default(System.Collections.Generic.IEnumerator<System.Linq.IGrouping<TKey, TElement>>); }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return default(System.Collections.IEnumerator); }
    }
}
