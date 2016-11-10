// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace System.Linq
{
    internal static class CachedReflectionInfo
    {
        private static MethodInfo s_Aggregate_TSource_2;

        public static MethodInfo Aggregate_TSource_2(Type TSource) =>
             (s_Aggregate_TSource_2 ??
             (s_Aggregate_TSource_2 = GetMethodInfoOf<IQueryable<object>, Expression<Func<object, object, object>>, object>(Queryable.Aggregate<object>).GetGenericMethodDefinition()))
              .MakeGenericMethod(TSource);

        private static MethodInfo s_Aggregate_TSource_TAccumulate_3;

        public static MethodInfo Aggregate_TSource_TAccumulate_3(Type TSource, Type TAccumulate) =>
             (s_Aggregate_TSource_TAccumulate_3 ??
             (s_Aggregate_TSource_TAccumulate_3 = GetMethodInfoOf<IQueryable<object>, object, Expression<Func<object, object, object>>, object>(Queryable.Aggregate<object, object>).GetGenericMethodDefinition()))
              .MakeGenericMethod(TSource, TAccumulate);

        private static MethodInfo s_Aggregate_TSource_TAccumulate_TResult_4;

        public static MethodInfo Aggregate_TSource_TAccumulate_TResult_4(Type TSource, Type TAccumulate, Type TResult) =>
             (s_Aggregate_TSource_TAccumulate_TResult_4 ??
             (s_Aggregate_TSource_TAccumulate_TResult_4 = GetMethodInfoOf<IQueryable<object>, object, Expression<Func<object, object, object>>, Expression<Func<object, object>>, object>(Queryable.Aggregate<object, object, object>).GetGenericMethodDefinition()))
              .MakeGenericMethod(TSource, TAccumulate, TResult);

        private static MethodInfo s_All_TSource_2;

        public static MethodInfo All_TSource_2(Type TSource) =>
             (s_All_TSource_2 ??
             (s_All_TSource_2 = GetMethodInfoOf<IQueryable<object>, Expression<Func<object, bool>>, bool>(Queryable.All<object>).GetGenericMethodDefinition()))
              .MakeGenericMethod(TSource);

        private static MethodInfo s_Any_TSource_1;

        public static MethodInfo Any_TSource_1(Type TSource) =>
             (s_Any_TSource_1 ??
             (s_Any_TSource_1 = GetMethodInfoOf<IQueryable<object>, bool>(Queryable.Any<object>).GetGenericMethodDefinition()))
              .MakeGenericMethod(TSource);

        private static MethodInfo s_Any_TSource_2;

        public static MethodInfo Any_TSource_2(Type TSource) =>
             (s_Any_TSource_2 ??
             (s_Any_TSource_2 = GetMethodInfoOf<IQueryable<object>, Expression<Func<object, bool>>, bool>(Queryable.Any<object>).GetGenericMethodDefinition()))
              .MakeGenericMethod(TSource);

        private static MethodInfo s_AsQueryable_1;

        public static MethodInfo AsQueryable_1 =>
             s_AsQueryable_1 ??
            (s_AsQueryable_1 = GetMethodInfoOf<IEnumerable, IQueryable>(Queryable.AsQueryable));

        private static MethodInfo s_AsQueryable_TElement_1;

        public static MethodInfo AsQueryable_TElement_1(Type TElement) =>
             (s_AsQueryable_TElement_1 ??
             (s_AsQueryable_TElement_1 = GetMethodInfoOf<IEnumerable<object>, IQueryable<object>>(Queryable.AsQueryable<object>).GetGenericMethodDefinition()))
              .MakeGenericMethod(TElement);

        private static MethodInfo s_Average_Int32_1;

        public static MethodInfo Average_Int32_1 =>
             s_Average_Int32_1 ??
            (s_Average_Int32_1 = GetMethodInfoOf<IQueryable<int>, double>(Queryable.Average));

        private static MethodInfo s_Average_NullableInt32_1;

        public static MethodInfo Average_NullableInt32_1 =>
             s_Average_NullableInt32_1 ??
            (s_Average_NullableInt32_1 = GetMethodInfoOf<IQueryable<int?>, double?>(Queryable.Average));

        private static MethodInfo s_Average_Int64_1;

        public static MethodInfo Average_Int64_1 =>
             s_Average_Int64_1 ??
            (s_Average_Int64_1 = GetMethodInfoOf<IQueryable<long>, double>(Queryable.Average));

        private static MethodInfo s_Average_NullableInt64_1;

        public static MethodInfo Average_NullableInt64_1 =>
             s_Average_NullableInt64_1 ??
            (s_Average_NullableInt64_1 = GetMethodInfoOf<IQueryable<long?>, double?>(Queryable.Average));

        private static MethodInfo s_Average_Single_1;

        public static MethodInfo Average_Single_1 =>
             s_Average_Single_1 ??
            (s_Average_Single_1 = GetMethodInfoOf<IQueryable<float>, float>(Queryable.Average));

        private static MethodInfo s_Average_NullableSingle_1;

        public static MethodInfo Average_NullableSingle_1 =>
             s_Average_NullableSingle_1 ??
            (s_Average_NullableSingle_1 = GetMethodInfoOf<IQueryable<float?>, float?>(Queryable.Average));

        private static MethodInfo s_Average_Double_1;

        public static MethodInfo Average_Double_1 =>
             s_Average_Double_1 ??
            (s_Average_Double_1 = GetMethodInfoOf<IQueryable<double>, double>(Queryable.Average));

        private static MethodInfo s_Average_NullableDouble_1;

        public static MethodInfo Average_NullableDouble_1 =>
             s_Average_NullableDouble_1 ??
            (s_Average_NullableDouble_1 = GetMethodInfoOf<IQueryable<double?>, double?>(Queryable.Average));

        private static MethodInfo s_Average_Decimal_1;

        public static MethodInfo Average_Decimal_1 =>
             s_Average_Decimal_1 ??
            (s_Average_Decimal_1 = GetMethodInfoOf<IQueryable<decimal>, decimal>(Queryable.Average));

        private static MethodInfo s_Average_NullableDecimal_1;

        public static MethodInfo Average_NullableDecimal_1 =>
             s_Average_NullableDecimal_1 ??
            (s_Average_NullableDecimal_1 = GetMethodInfoOf<IQueryable<decimal?>, decimal?>(Queryable.Average));

        private static MethodInfo s_Average_Int32_TSource_2;

        public static MethodInfo Average_Int32_TSource_2(Type TSource) =>
             (s_Average_Int32_TSource_2 ??
             (s_Average_Int32_TSource_2 = GetMethodInfoOf<IQueryable<object>, Expression<Func<object, int>>, double>(Queryable.Average<object>).GetGenericMethodDefinition()))
              .MakeGenericMethod(TSource);

        private static MethodInfo s_Average_NullableInt32_TSource_2;

        public static MethodInfo Average_NullableInt32_TSource_2(Type TSource) =>
             (s_Average_NullableInt32_TSource_2 ??
             (s_Average_NullableInt32_TSource_2 = GetMethodInfoOf<IQueryable<object>, Expression<Func<object, int?>>, double?>(Queryable.Average<object>).GetGenericMethodDefinition()))
              .MakeGenericMethod(TSource);

        private static MethodInfo s_Average_Single_TSource_2;

        public static MethodInfo Average_Single_TSource_2(Type TSource) =>
             (s_Average_Single_TSource_2 ??
             (s_Average_Single_TSource_2 = GetMethodInfoOf<IQueryable<object>, Expression<Func<object, float>>, float>(Queryable.Average<object>).GetGenericMethodDefinition()))
              .MakeGenericMethod(TSource);

        private static MethodInfo s_Average_NullableSingle_TSource_2;

        public static MethodInfo Average_NullableSingle_TSource_2(Type TSource) =>
             (s_Average_NullableSingle_TSource_2 ??
             (s_Average_NullableSingle_TSource_2 = GetMethodInfoOf<IQueryable<object>, Expression<Func<object, float?>>, float?>(Queryable.Average<object>).GetGenericMethodDefinition()))
              .MakeGenericMethod(TSource);

        private static MethodInfo s_Average_Int64_TSource_2;

        public static MethodInfo Average_Int64_TSource_2(Type TSource) =>
             (s_Average_Int64_TSource_2 ??
             (s_Average_Int64_TSource_2 = GetMethodInfoOf<IQueryable<object>, Expression<Func<object, long>>, double>(Queryable.Average<object>).GetGenericMethodDefinition()))
              .MakeGenericMethod(TSource);

        private static MethodInfo s_Average_NullableInt64_TSource_2;

        public static MethodInfo Average_NullableInt64_TSource_2(Type TSource) =>
             (s_Average_NullableInt64_TSource_2 ??
             (s_Average_NullableInt64_TSource_2 = GetMethodInfoOf<IQueryable<object>, Expression<Func<object, long?>>, double?>(Queryable.Average<object>).GetGenericMethodDefinition()))
              .MakeGenericMethod(TSource);

        private static MethodInfo s_Average_Double_TSource_2;

        public static MethodInfo Average_Double_TSource_2(Type TSource) =>
             (s_Average_Double_TSource_2 ??
             (s_Average_Double_TSource_2 = GetMethodInfoOf<IQueryable<object>, Expression<Func<object, double>>, double>(Queryable.Average<object>).GetGenericMethodDefinition()))
              .MakeGenericMethod(TSource);

        private static MethodInfo s_Average_NullableDouble_TSource_2;

        public static MethodInfo Average_NullableDouble_TSource_2(Type TSource) =>
             (s_Average_NullableDouble_TSource_2 ??
             (s_Average_NullableDouble_TSource_2 = GetMethodInfoOf<IQueryable<object>, Expression<Func<object, double?>>, double?>(Queryable.Average<object>).GetGenericMethodDefinition()))
              .MakeGenericMethod(TSource);

        private static MethodInfo s_Average_Decimal_TSource_2;

        public static MethodInfo Average_Decimal_TSource_2(Type TSource) =>
             (s_Average_Decimal_TSource_2 ??
             (s_Average_Decimal_TSource_2 = GetMethodInfoOf<IQueryable<object>, Expression<Func<object, decimal>>, decimal>(Queryable.Average<object>).GetGenericMethodDefinition()))
              .MakeGenericMethod(TSource);

        private static MethodInfo s_Average_NullableDecimal_TSource_2;

        public static MethodInfo Average_NullableDecimal_TSource_2(Type TSource) =>
             (s_Average_NullableDecimal_TSource_2 ??
             (s_Average_NullableDecimal_TSource_2 = GetMethodInfoOf<IQueryable<object>, Expression<Func<object, decimal?>>, decimal?>(Queryable.Average<object>).GetGenericMethodDefinition()))
              .MakeGenericMethod(TSource);

        private static MethodInfo s_Cast_TResult_1;

        public static MethodInfo Cast_TResult_1(Type TResult) =>
             (s_Cast_TResult_1 ??
             (s_Cast_TResult_1 = GetMethodInfoOf<IQueryable, IQueryable<object>>(Queryable.Cast<object>).GetGenericMethodDefinition()))
              .MakeGenericMethod(TResult);

        private static MethodInfo s_Concat_TSource_2;

        public static MethodInfo Concat_TSource_2(Type TSource) =>
             (s_Concat_TSource_2 ??
             (s_Concat_TSource_2 = GetMethodInfoOf<IQueryable<object>, IEnumerable<object>, IQueryable<object>>(Queryable.Concat<object>).GetGenericMethodDefinition()))
              .MakeGenericMethod(TSource);

        private static MethodInfo s_Contains_TSource_2;

        public static MethodInfo Contains_TSource_2(Type TSource) =>
             (s_Contains_TSource_2 ??
             (s_Contains_TSource_2 = GetMethodInfoOf<IQueryable<object>, object, bool>(Queryable.Contains<object>).GetGenericMethodDefinition()))
              .MakeGenericMethod(TSource);

        private static MethodInfo s_Contains_TSource_3;

        public static MethodInfo Contains_TSource_3(Type TSource) =>
             (s_Contains_TSource_3 ??
             (s_Contains_TSource_3 = GetMethodInfoOf<IQueryable<object>, object, IEqualityComparer<object>, bool>(Queryable.Contains<object>).GetGenericMethodDefinition()))
              .MakeGenericMethod(TSource);

        private static MethodInfo s_Count_TSource_1;

        public static MethodInfo Count_TSource_1(Type TSource) =>
             (s_Count_TSource_1 ??
             (s_Count_TSource_1 = GetMethodInfoOf<IQueryable<object>, int>(Queryable.Count<object>).GetGenericMethodDefinition()))
              .MakeGenericMethod(TSource);

        private static MethodInfo s_Count_TSource_2;

        public static MethodInfo Count_TSource_2(Type TSource) =>
             (s_Count_TSource_2 ??
             (s_Count_TSource_2 = GetMethodInfoOf<IQueryable<object>, Expression<Func<object, bool>>, int>(Queryable.Count<object>).GetGenericMethodDefinition()))
              .MakeGenericMethod(TSource);

        private static MethodInfo s_DefaultIfEmpty_TSource_1;

        public static MethodInfo DefaultIfEmpty_TSource_1(Type TSource) =>
             (s_DefaultIfEmpty_TSource_1 ??
             (s_DefaultIfEmpty_TSource_1 = GetMethodInfoOf<IQueryable<object>, IQueryable<object>>(Queryable.DefaultIfEmpty<object>).GetGenericMethodDefinition()))
              .MakeGenericMethod(TSource);

        private static MethodInfo s_DefaultIfEmpty_TSource_2;

        public static MethodInfo DefaultIfEmpty_TSource_2(Type TSource) =>
             (s_DefaultIfEmpty_TSource_2 ??
             (s_DefaultIfEmpty_TSource_2 = GetMethodInfoOf<IQueryable<object>, object, IQueryable<object>>(Queryable.DefaultIfEmpty<object>).GetGenericMethodDefinition()))
              .MakeGenericMethod(TSource);

        private static MethodInfo s_Distinct_TSource_1;

        public static MethodInfo Distinct_TSource_1(Type TSource) =>
             (s_Distinct_TSource_1 ??
             (s_Distinct_TSource_1 = GetMethodInfoOf<IQueryable<object>, IQueryable<object>>(Queryable.Distinct<object>).GetGenericMethodDefinition()))
              .MakeGenericMethod(TSource);

        private static MethodInfo s_Distinct_TSource_2;

        public static MethodInfo Distinct_TSource_2(Type TSource) =>
             (s_Distinct_TSource_2 ??
             (s_Distinct_TSource_2 = GetMethodInfoOf<IQueryable<object>, IEqualityComparer<object>, IQueryable<object>>(Queryable.Distinct<object>).GetGenericMethodDefinition()))
              .MakeGenericMethod(TSource);

        private static MethodInfo s_ElementAt_TSource_2;

        public static MethodInfo ElementAt_TSource_2(Type TSource) =>
             (s_ElementAt_TSource_2 ??
             (s_ElementAt_TSource_2 = GetMethodInfoOf<IQueryable<object>, int, object>(Queryable.ElementAt<object>).GetGenericMethodDefinition()))
              .MakeGenericMethod(TSource);

        private static MethodInfo s_ElementAtOrDefault_TSource_2;

        public static MethodInfo ElementAtOrDefault_TSource_2(Type TSource) =>
             (s_ElementAtOrDefault_TSource_2 ??
             (s_ElementAtOrDefault_TSource_2 = GetMethodInfoOf<IQueryable<object>, int, object>(Queryable.ElementAtOrDefault<object>).GetGenericMethodDefinition()))
              .MakeGenericMethod(TSource);

        private static MethodInfo s_Except_TSource_2;

        public static MethodInfo Except_TSource_2(Type TSource) =>
             (s_Except_TSource_2 ??
             (s_Except_TSource_2 = GetMethodInfoOf<IQueryable<object>, IEnumerable<object>, IQueryable<object>>(Queryable.Except<object>).GetGenericMethodDefinition()))
              .MakeGenericMethod(TSource);

        private static MethodInfo s_Except_TSource_3;

        public static MethodInfo Except_TSource_3(Type TSource) =>
             (s_Except_TSource_3 ??
             (s_Except_TSource_3 = GetMethodInfoOf<IQueryable<object>, IEnumerable<object>, IEqualityComparer<object>, IQueryable<object>>(Queryable.Except<object>).GetGenericMethodDefinition()))
              .MakeGenericMethod(TSource);

        private static MethodInfo s_First_TSource_1;

        public static MethodInfo First_TSource_1(Type TSource) =>
             (s_First_TSource_1 ??
             (s_First_TSource_1 = GetMethodInfoOf<IQueryable<object>, object>(Queryable.First<object>).GetGenericMethodDefinition()))
              .MakeGenericMethod(TSource);

        private static MethodInfo s_First_TSource_2;

        public static MethodInfo First_TSource_2(Type TSource) =>
             (s_First_TSource_2 ??
             (s_First_TSource_2 = GetMethodInfoOf<IQueryable<object>, Expression<Func<object, bool>>, object>(Queryable.First<object>).GetGenericMethodDefinition()))
              .MakeGenericMethod(TSource);

        private static MethodInfo s_FirstOrDefault_TSource_1;

        public static MethodInfo FirstOrDefault_TSource_1(Type TSource) =>
             (s_FirstOrDefault_TSource_1 ??
             (s_FirstOrDefault_TSource_1 = GetMethodInfoOf<IQueryable<object>, object>(Queryable.FirstOrDefault<object>).GetGenericMethodDefinition()))
              .MakeGenericMethod(TSource);

        private static MethodInfo s_FirstOrDefault_TSource_2;

        public static MethodInfo FirstOrDefault_TSource_2(Type TSource) =>
             (s_FirstOrDefault_TSource_2 ??
             (s_FirstOrDefault_TSource_2 = GetMethodInfoOf<IQueryable<object>, Expression<Func<object, bool>>, object>(Queryable.FirstOrDefault<object>).GetGenericMethodDefinition()))
              .MakeGenericMethod(TSource);

        private static MethodInfo s_GroupBy_TSource_TKey_2;

        public static MethodInfo GroupBy_TSource_TKey_2(Type TSource, Type TKey) =>
             (s_GroupBy_TSource_TKey_2 ??
             (s_GroupBy_TSource_TKey_2 = GetMethodInfoOf<IQueryable<object>, Expression<Func<object, object>>, IQueryable<IGrouping<object, object>>>(Queryable.GroupBy<object, object>).GetGenericMethodDefinition()))
              .MakeGenericMethod(TSource, TKey);

        private static MethodInfo s_GroupBy_TSource_TKey_3;

        public static MethodInfo GroupBy_TSource_TKey_3(Type TSource, Type TKey) =>
             (s_GroupBy_TSource_TKey_3 ??
             (s_GroupBy_TSource_TKey_3 = GetMethodInfoOf<IQueryable<object>, Expression<Func<object, object>>, IEqualityComparer<object>, IQueryable<IGrouping<object, object>>>(Queryable.GroupBy<object, object>).GetGenericMethodDefinition()))
              .MakeGenericMethod(TSource, TKey);

        private static MethodInfo s_GroupBy_TSource_TKey_TElement_3;

        public static MethodInfo GroupBy_TSource_TKey_TElement_3(Type TSource, Type TKey, Type TElement) =>
             (s_GroupBy_TSource_TKey_TElement_3 ??
             (s_GroupBy_TSource_TKey_TElement_3 = GetMethodInfoOf<IQueryable<object>, Expression<Func<object, object>>, Expression<Func<object, object>>, IQueryable<IGrouping<object, object>>>(Queryable.GroupBy<object, object, object>).GetGenericMethodDefinition()))
              .MakeGenericMethod(TSource, TKey, TElement);

        private static MethodInfo s_GroupBy_TSource_TKey_TElement_4;

        public static MethodInfo GroupBy_TSource_TKey_TElement_4(Type TSource, Type TKey, Type TElement) =>
             (s_GroupBy_TSource_TKey_TElement_4 ??
             (s_GroupBy_TSource_TKey_TElement_4 = GetMethodInfoOf<IQueryable<object>, Expression<Func<object, object>>, Expression<Func<object, object>>, IEqualityComparer<object>, IQueryable<IGrouping<object, object>>>(Queryable.GroupBy<object, object, object>).GetGenericMethodDefinition()))
              .MakeGenericMethod(TSource, TKey, TElement);

        private static MethodInfo s_GroupBy_TSource_TKey_TResult_3;

        public static MethodInfo GroupBy_TSource_TKey_TResult_3(Type TSource, Type TKey, Type TResult) =>
             (s_GroupBy_TSource_TKey_TResult_3 ??
             (s_GroupBy_TSource_TKey_TResult_3 = GetMethodInfoOf<IQueryable<object>, Expression<Func<object, object>>, Expression<Func<object, IEnumerable<object>, object>>, IQueryable<object>>(Queryable.GroupBy<object, object, object>).GetGenericMethodDefinition()))
              .MakeGenericMethod(TSource, TKey, TResult);

        private static MethodInfo s_GroupBy_TSource_TKey_TResult_4;

        public static MethodInfo GroupBy_TSource_TKey_TResult_4(Type TSource, Type TKey, Type TResult) =>
             (s_GroupBy_TSource_TKey_TResult_4 ??
             (s_GroupBy_TSource_TKey_TResult_4 = GetMethodInfoOf<IQueryable<object>, Expression<Func<object, object>>, Expression<Func<object, IEnumerable<object>, object>>, IEqualityComparer<object>, IQueryable<object>>(Queryable.GroupBy<object, object, object>).GetGenericMethodDefinition()))
              .MakeGenericMethod(TSource, TKey, TResult);

        private static MethodInfo s_GroupBy_TSource_TKey_TElement_TResult_4;

        public static MethodInfo GroupBy_TSource_TKey_TElement_TResult_4(Type TSource, Type TKey, Type TElement, Type TResult) =>
             (s_GroupBy_TSource_TKey_TElement_TResult_4 ??
             (s_GroupBy_TSource_TKey_TElement_TResult_4 = GetMethodInfoOf<IQueryable<object>, Expression<Func<object, object>>, Expression<Func<object, object>>, Expression<Func<object, IEnumerable<object>, object>>, IQueryable<object>>(Queryable.GroupBy<object, object, object, object>).GetGenericMethodDefinition()))
              .MakeGenericMethod(TSource, TKey, TElement, TResult);

        private static MethodInfo s_GroupBy_TSource_TKey_TElement_TResult_5;

        public static MethodInfo GroupBy_TSource_TKey_TElement_TResult_5(Type TSource, Type TKey, Type TElement, Type TResult) =>
             (s_GroupBy_TSource_TKey_TElement_TResult_5 ??
             (s_GroupBy_TSource_TKey_TElement_TResult_5 = GetMethodInfoOf<IQueryable<object>, Expression<Func<object, object>>, Expression<Func<object, object>>, Expression<Func<object, IEnumerable<object>, object>>, IEqualityComparer<object>, IQueryable<object>>(Queryable.GroupBy<object, object, object, object>).GetGenericMethodDefinition()))
              .MakeGenericMethod(TSource, TKey, TElement, TResult);

        private static MethodInfo s_GroupJoin_TOuter_TInner_TKey_TResult_5;

        public static MethodInfo GroupJoin_TOuter_TInner_TKey_TResult_5(Type TOuter, Type TInner, Type TKey, Type TResult) =>
             (s_GroupJoin_TOuter_TInner_TKey_TResult_5 ??
             (s_GroupJoin_TOuter_TInner_TKey_TResult_5 = GetMethodInfoOf<IQueryable<object>, IEnumerable<object>, Expression<Func<object, object>>, Expression<Func<object, object>>, Expression<Func<object, IEnumerable<object>, object>>, IQueryable<object>>(Queryable.GroupJoin<object, object, object, object>).GetGenericMethodDefinition()))
              .MakeGenericMethod(TOuter, TInner, TKey, TResult);

        private static MethodInfo s_GroupJoin_TOuter_TInner_TKey_TResult_6;

        public static MethodInfo GroupJoin_TOuter_TInner_TKey_TResult_6(Type TOuter, Type TInner, Type TKey, Type TResult) =>
             (s_GroupJoin_TOuter_TInner_TKey_TResult_6 ??
             (s_GroupJoin_TOuter_TInner_TKey_TResult_6 = GetMethodInfoOf<IQueryable<object>, IEnumerable<object>, Expression<Func<object, object>>, Expression<Func<object, object>>, Expression<Func<object, IEnumerable<object>, object>>, IEqualityComparer<object>, IQueryable<object>>(Queryable.GroupJoin<object, object, object, object>).GetGenericMethodDefinition()))
              .MakeGenericMethod(TOuter, TInner, TKey, TResult);

        private static MethodInfo s_Intersect_TSource_2;

        public static MethodInfo Intersect_TSource_2(Type TSource) =>
             (s_Intersect_TSource_2 ??
             (s_Intersect_TSource_2 = GetMethodInfoOf<IQueryable<object>, IEnumerable<object>, IQueryable<object>>(Queryable.Intersect<object>).GetGenericMethodDefinition()))
              .MakeGenericMethod(TSource);

        private static MethodInfo s_Intersect_TSource_3;

        public static MethodInfo Intersect_TSource_3(Type TSource) =>
             (s_Intersect_TSource_3 ??
             (s_Intersect_TSource_3 = GetMethodInfoOf<IQueryable<object>, IEnumerable<object>, IEqualityComparer<object>, IQueryable<object>>(Queryable.Intersect<object>).GetGenericMethodDefinition()))
              .MakeGenericMethod(TSource);

        private static MethodInfo s_Join_TOuter_TInner_TKey_TResult_5;

        public static MethodInfo Join_TOuter_TInner_TKey_TResult_5(Type TOuter, Type TInner, Type TKey, Type TResult) =>
             (s_Join_TOuter_TInner_TKey_TResult_5 ??
             (s_Join_TOuter_TInner_TKey_TResult_5 = GetMethodInfoOf<IQueryable<object>, IEnumerable<object>, Expression<Func<object, object>>, Expression<Func<object, object>>, Expression<Func<object, object, object>>, IQueryable<object>>(Queryable.Join<object, object, object, object>).GetGenericMethodDefinition()))
              .MakeGenericMethod(TOuter, TInner, TKey, TResult);

        private static MethodInfo s_Join_TOuter_TInner_TKey_TResult_6;

        public static MethodInfo Join_TOuter_TInner_TKey_TResult_6(Type TOuter, Type TInner, Type TKey, Type TResult) =>
             (s_Join_TOuter_TInner_TKey_TResult_6 ??
             (s_Join_TOuter_TInner_TKey_TResult_6 = GetMethodInfoOf<IQueryable<object>, IEnumerable<object>, Expression<Func<object, object>>, Expression<Func<object, object>>, Expression<Func<object, object, object>>, IEqualityComparer<object>, IQueryable<object>>(Queryable.Join<object, object, object, object>).GetGenericMethodDefinition()))
              .MakeGenericMethod(TOuter, TInner, TKey, TResult);

        private static MethodInfo s_Last_TSource_1;

        public static MethodInfo Last_TSource_1(Type TSource) =>
             (s_Last_TSource_1 ??
             (s_Last_TSource_1 = GetMethodInfoOf<IQueryable<object>, object>(Queryable.Last<object>).GetGenericMethodDefinition()))
              .MakeGenericMethod(TSource);

        private static MethodInfo s_Last_TSource_2;

        public static MethodInfo Last_TSource_2(Type TSource) =>
             (s_Last_TSource_2 ??
             (s_Last_TSource_2 = GetMethodInfoOf<IQueryable<object>, Expression<Func<object, bool>>, object>(Queryable.Last<object>).GetGenericMethodDefinition()))
              .MakeGenericMethod(TSource);

        private static MethodInfo s_LastOrDefault_TSource_1;

        public static MethodInfo LastOrDefault_TSource_1(Type TSource) =>
             (s_LastOrDefault_TSource_1 ??
             (s_LastOrDefault_TSource_1 = GetMethodInfoOf<IQueryable<object>, object>(Queryable.LastOrDefault<object>).GetGenericMethodDefinition()))
              .MakeGenericMethod(TSource);

        private static MethodInfo s_LastOrDefault_TSource_2;

        public static MethodInfo LastOrDefault_TSource_2(Type TSource) =>
             (s_LastOrDefault_TSource_2 ??
             (s_LastOrDefault_TSource_2 = GetMethodInfoOf<IQueryable<object>, Expression<Func<object, bool>>, object>(Queryable.LastOrDefault<object>).GetGenericMethodDefinition()))
              .MakeGenericMethod(TSource);

        private static MethodInfo s_LongCount_TSource_1;

        public static MethodInfo LongCount_TSource_1(Type TSource) =>
             (s_LongCount_TSource_1 ??
             (s_LongCount_TSource_1 = GetMethodInfoOf<IQueryable<object>, long>(Queryable.LongCount<object>).GetGenericMethodDefinition()))
              .MakeGenericMethod(TSource);

        private static MethodInfo s_LongCount_TSource_2;

        public static MethodInfo LongCount_TSource_2(Type TSource) =>
             (s_LongCount_TSource_2 ??
             (s_LongCount_TSource_2 = GetMethodInfoOf<IQueryable<object>, Expression<Func<object, bool>>, long>(Queryable.LongCount<object>).GetGenericMethodDefinition()))
              .MakeGenericMethod(TSource);

        private static MethodInfo s_Max_TSource_1;

        public static MethodInfo Max_TSource_1(Type TSource) =>
             (s_Max_TSource_1 ??
             (s_Max_TSource_1 = GetMethodInfoOf<IQueryable<object>, object>(Queryable.Max<object>).GetGenericMethodDefinition()))
              .MakeGenericMethod(TSource);

        private static MethodInfo s_Max_TSource_TResult_2;

        public static MethodInfo Max_TSource_TResult_2(Type TSource, Type TResult) =>
             (s_Max_TSource_TResult_2 ??
             (s_Max_TSource_TResult_2 = GetMethodInfoOf<IQueryable<object>, Expression<Func<object, object>>, object>(Queryable.Max<object, object>).GetGenericMethodDefinition()))
              .MakeGenericMethod(TSource, TResult);

        private static MethodInfo s_Min_TSource_1;

        public static MethodInfo Min_TSource_1(Type TSource) =>
             (s_Min_TSource_1 ??
             (s_Min_TSource_1 = GetMethodInfoOf<IQueryable<object>, object>(Queryable.Min<object>).GetGenericMethodDefinition()))
              .MakeGenericMethod(TSource);

        private static MethodInfo s_Min_TSource_TResult_2;

        public static MethodInfo Min_TSource_TResult_2(Type TSource, Type TResult) =>
             (s_Min_TSource_TResult_2 ??
             (s_Min_TSource_TResult_2 = GetMethodInfoOf<IQueryable<object>, Expression<Func<object, object>>, object>(Queryable.Min<object, object>).GetGenericMethodDefinition()))
              .MakeGenericMethod(TSource, TResult);

        private static MethodInfo s_OfType_TResult_1;

        public static MethodInfo OfType_TResult_1(Type TResult) =>
             (s_OfType_TResult_1 ??
             (s_OfType_TResult_1 = GetMethodInfoOf<IQueryable, IQueryable<object>>(Queryable.OfType<object>).GetGenericMethodDefinition()))
              .MakeGenericMethod(TResult);

        private static MethodInfo s_OrderBy_TSource_TKey_2;

        public static MethodInfo OrderBy_TSource_TKey_2(Type TSource, Type TKey) =>
             (s_OrderBy_TSource_TKey_2 ??
             (s_OrderBy_TSource_TKey_2 = GetMethodInfoOf<IQueryable<object>, Expression<Func<object, object>>, IOrderedQueryable<object>>(Queryable.OrderBy<object, object>).GetGenericMethodDefinition()))
              .MakeGenericMethod(TSource, TKey);

        private static MethodInfo s_OrderBy_TSource_TKey_3;

        public static MethodInfo OrderBy_TSource_TKey_3(Type TSource, Type TKey) =>
             (s_OrderBy_TSource_TKey_3 ??
             (s_OrderBy_TSource_TKey_3 = GetMethodInfoOf<IQueryable<object>, Expression<Func<object, object>>, IComparer<object>, IOrderedQueryable<object>>(Queryable.OrderBy<object, object>).GetGenericMethodDefinition()))
              .MakeGenericMethod(TSource, TKey);

        private static MethodInfo s_OrderByDescending_TSource_TKey_2;

        public static MethodInfo OrderByDescending_TSource_TKey_2(Type TSource, Type TKey) =>
             (s_OrderByDescending_TSource_TKey_2 ??
             (s_OrderByDescending_TSource_TKey_2 = GetMethodInfoOf<IQueryable<object>, Expression<Func<object, object>>, IOrderedQueryable<object>>(Queryable.OrderByDescending<object, object>).GetGenericMethodDefinition()))
              .MakeGenericMethod(TSource, TKey);

        private static MethodInfo s_OrderByDescending_TSource_TKey_3;

        public static MethodInfo OrderByDescending_TSource_TKey_3(Type TSource, Type TKey) =>
             (s_OrderByDescending_TSource_TKey_3 ??
             (s_OrderByDescending_TSource_TKey_3 = GetMethodInfoOf<IQueryable<object>, Expression<Func<object, object>>, IComparer<object>, IOrderedQueryable<object>>(Queryable.OrderByDescending<object, object>).GetGenericMethodDefinition()))
              .MakeGenericMethod(TSource, TKey);

        private static MethodInfo s_Reverse_TSource_1;

        public static MethodInfo Reverse_TSource_1(Type TSource) =>
             (s_Reverse_TSource_1 ??
             (s_Reverse_TSource_1 = GetMethodInfoOf<IQueryable<object>, IQueryable<object>>(Queryable.Reverse<object>).GetGenericMethodDefinition()))
              .MakeGenericMethod(TSource);

        private static MethodInfo s_Select_TSource_TResult_2;

        public static MethodInfo Select_TSource_TResult_2(Type TSource, Type TResult) =>
             (s_Select_TSource_TResult_2 ??
             (s_Select_TSource_TResult_2 = GetMethodInfoOf<IQueryable<object>, Expression<Func<object, object>>, IQueryable<object>>(Queryable.Select<object, object>).GetGenericMethodDefinition()))
              .MakeGenericMethod(TSource, TResult);

        private static MethodInfo s_Select_Index_TSource_TResult_2;

        public static MethodInfo Select_Index_TSource_TResult_2(Type TSource, Type TResult) =>
             (s_Select_Index_TSource_TResult_2 ??
             (s_Select_Index_TSource_TResult_2 = GetMethodInfoOf<IQueryable<object>, Expression<Func<object, int, object>>, IQueryable<object>>(Queryable.Select<object, object>).GetGenericMethodDefinition()))
              .MakeGenericMethod(TSource, TResult);

        private static MethodInfo s_SelectMany_TSource_TResult_2;

        public static MethodInfo SelectMany_TSource_TResult_2(Type TSource, Type TResult) =>
             (s_SelectMany_TSource_TResult_2 ??
             (s_SelectMany_TSource_TResult_2 = GetMethodInfoOf<IQueryable<object>, Expression<Func<object, IEnumerable<object>>>, IQueryable<object>>(Queryable.SelectMany<object, object>).GetGenericMethodDefinition()))
              .MakeGenericMethod(TSource, TResult);

        private static MethodInfo s_SelectMany_Index_TSource_TResult_2;

        public static MethodInfo SelectMany_Index_TSource_TResult_2(Type TSource, Type TResult) =>
             (s_SelectMany_Index_TSource_TResult_2 ??
             (s_SelectMany_Index_TSource_TResult_2 = GetMethodInfoOf<IQueryable<object>, Expression<Func<object, int, IEnumerable<object>>>, IQueryable<object>>(Queryable.SelectMany<object, object>).GetGenericMethodDefinition()))
              .MakeGenericMethod(TSource, TResult);

        private static MethodInfo s_SelectMany_Index_TSource_TCollection_TResult_3;

        public static MethodInfo SelectMany_Index_TSource_TCollection_TResult_3(Type TSource, Type TCollection, Type TResult) =>
             (s_SelectMany_Index_TSource_TCollection_TResult_3 ??
             (s_SelectMany_Index_TSource_TCollection_TResult_3 = GetMethodInfoOf<IQueryable<object>, Expression<Func<object, int, IEnumerable<object>>>, Expression<Func<object, object, object>>, IQueryable<object>>(Queryable.SelectMany<object, object, object>).GetGenericMethodDefinition()))
              .MakeGenericMethod(TSource, TCollection, TResult);

        private static MethodInfo s_SelectMany_TSource_TCollection_TResult_3;

        public static MethodInfo SelectMany_TSource_TCollection_TResult_3(Type TSource, Type TCollection, Type TResult) =>
             (s_SelectMany_TSource_TCollection_TResult_3 ??
             (s_SelectMany_TSource_TCollection_TResult_3 = GetMethodInfoOf<IQueryable<object>, Expression<Func<object, IEnumerable<object>>>, Expression<Func<object, object, object>>, IQueryable<object>>(Queryable.SelectMany<object, object, object>).GetGenericMethodDefinition()))
              .MakeGenericMethod(TSource, TCollection, TResult);

        private static MethodInfo s_SequenceEqual_TSource_2;

        public static MethodInfo SequenceEqual_TSource_2(Type TSource) =>
             (s_SequenceEqual_TSource_2 ??
             (s_SequenceEqual_TSource_2 = GetMethodInfoOf<IQueryable<object>, IEnumerable<object>, bool>(Queryable.SequenceEqual<object>).GetGenericMethodDefinition()))
              .MakeGenericMethod(TSource);

        private static MethodInfo s_SequenceEqual_TSource_3;

        public static MethodInfo SequenceEqual_TSource_3(Type TSource) =>
             (s_SequenceEqual_TSource_3 ??
             (s_SequenceEqual_TSource_3 = GetMethodInfoOf<IQueryable<object>, IEnumerable<object>, IEqualityComparer<object>, bool>(Queryable.SequenceEqual<object>).GetGenericMethodDefinition()))
              .MakeGenericMethod(TSource);

        private static MethodInfo s_Single_TSource_1;

        public static MethodInfo Single_TSource_1(Type TSource) =>
             (s_Single_TSource_1 ??
             (s_Single_TSource_1 = GetMethodInfoOf<IQueryable<object>, object>(Queryable.Single<object>).GetGenericMethodDefinition()))
              .MakeGenericMethod(TSource);

        private static MethodInfo s_Single_TSource_2;

        public static MethodInfo Single_TSource_2(Type TSource) =>
             (s_Single_TSource_2 ??
             (s_Single_TSource_2 = GetMethodInfoOf<IQueryable<object>, Expression<Func<object, bool>>, object>(Queryable.Single<object>).GetGenericMethodDefinition()))
              .MakeGenericMethod(TSource);

        private static MethodInfo s_SingleOrDefault_TSource_1;

        public static MethodInfo SingleOrDefault_TSource_1(Type TSource) =>
             (s_SingleOrDefault_TSource_1 ??
             (s_SingleOrDefault_TSource_1 = GetMethodInfoOf<IQueryable<object>, object>(Queryable.SingleOrDefault<object>).GetGenericMethodDefinition()))
              .MakeGenericMethod(TSource);

        private static MethodInfo s_SingleOrDefault_TSource_2;

        public static MethodInfo SingleOrDefault_TSource_2(Type TSource) =>
             (s_SingleOrDefault_TSource_2 ??
             (s_SingleOrDefault_TSource_2 = GetMethodInfoOf<IQueryable<object>, Expression<Func<object, bool>>, object>(Queryable.SingleOrDefault<object>).GetGenericMethodDefinition()))
              .MakeGenericMethod(TSource);

        private static MethodInfo s_Skip_TSource_2;

        public static MethodInfo Skip_TSource_2(Type TSource) =>
             (s_Skip_TSource_2 ??
             (s_Skip_TSource_2 = GetMethodInfoOf<IQueryable<object>, int, IQueryable<object>>(Queryable.Skip<object>).GetGenericMethodDefinition()))
              .MakeGenericMethod(TSource);

        private static MethodInfo s_SkipWhile_TSource_2;

        public static MethodInfo SkipWhile_TSource_2(Type TSource) =>
             (s_SkipWhile_TSource_2 ??
             (s_SkipWhile_TSource_2 = GetMethodInfoOf<IQueryable<object>, Expression<Func<object, bool>>, IQueryable<object>>(Queryable.SkipWhile<object>).GetGenericMethodDefinition()))
              .MakeGenericMethod(TSource);

        private static MethodInfo s_SkipWhile_Index_TSource_2;

        public static MethodInfo SkipWhile_Index_TSource_2(Type TSource) =>
             (s_SkipWhile_Index_TSource_2 ??
             (s_SkipWhile_Index_TSource_2 = GetMethodInfoOf<IQueryable<object>, Expression<Func<object, int, bool>>, IQueryable<object>>(Queryable.SkipWhile<object>).GetGenericMethodDefinition()))
              .MakeGenericMethod(TSource);

        private static MethodInfo s_Sum_Int32_1;

        public static MethodInfo Sum_Int32_1 =>
             s_Sum_Int32_1 ??
            (s_Sum_Int32_1 = GetMethodInfoOf<IQueryable<int>, int>(Queryable.Sum));

        private static MethodInfo s_Sum_NullableInt32_1;

        public static MethodInfo Sum_NullableInt32_1 =>
             s_Sum_NullableInt32_1 ??
            (s_Sum_NullableInt32_1 = GetMethodInfoOf<IQueryable<int?>, int?>(Queryable.Sum));

        private static MethodInfo s_Sum_Int64_1;

        public static MethodInfo Sum_Int64_1 =>
             s_Sum_Int64_1 ??
            (s_Sum_Int64_1 = GetMethodInfoOf<IQueryable<long>, long>(Queryable.Sum));

        private static MethodInfo s_Sum_NullableInt64_1;

        public static MethodInfo Sum_NullableInt64_1 =>
             s_Sum_NullableInt64_1 ??
            (s_Sum_NullableInt64_1 = GetMethodInfoOf<IQueryable<long?>, long?>(Queryable.Sum));

        private static MethodInfo s_Sum_Single_1;

        public static MethodInfo Sum_Single_1 =>
             s_Sum_Single_1 ??
            (s_Sum_Single_1 = GetMethodInfoOf<IQueryable<float>, float>(Queryable.Sum));

        private static MethodInfo s_Sum_NullableSingle_1;

        public static MethodInfo Sum_NullableSingle_1 =>
             s_Sum_NullableSingle_1 ??
            (s_Sum_NullableSingle_1 = GetMethodInfoOf<IQueryable<float?>, float?>(Queryable.Sum));

        private static MethodInfo s_Sum_Double_1;

        public static MethodInfo Sum_Double_1 =>
             s_Sum_Double_1 ??
            (s_Sum_Double_1 = GetMethodInfoOf<IQueryable<double>, double>(Queryable.Sum));

        private static MethodInfo s_Sum_NullableDouble_1;

        public static MethodInfo Sum_NullableDouble_1 =>
             s_Sum_NullableDouble_1 ??
            (s_Sum_NullableDouble_1 = GetMethodInfoOf<IQueryable<double?>, double?>(Queryable.Sum));

        private static MethodInfo s_Sum_Decimal_1;

        public static MethodInfo Sum_Decimal_1 =>
             s_Sum_Decimal_1 ??
            (s_Sum_Decimal_1 = GetMethodInfoOf<IQueryable<decimal>, decimal>(Queryable.Sum));

        private static MethodInfo s_Sum_NullableDecimal_1;

        public static MethodInfo Sum_NullableDecimal_1 =>
             s_Sum_NullableDecimal_1 ??
            (s_Sum_NullableDecimal_1 = GetMethodInfoOf<IQueryable<decimal?>, decimal?>(Queryable.Sum));

        private static MethodInfo s_Sum_NullableDecimal_TSource_2;

        public static MethodInfo Sum_NullableDecimal_TSource_2(Type TSource) =>
             (s_Sum_NullableDecimal_TSource_2 ??
             (s_Sum_NullableDecimal_TSource_2 = GetMethodInfoOf<IQueryable<object>, Expression<Func<object, decimal?>>, decimal?>(Queryable.Sum<object>).GetGenericMethodDefinition()))
              .MakeGenericMethod(TSource);

        private static MethodInfo s_Sum_Int32_TSource_2;

        public static MethodInfo Sum_Int32_TSource_2(Type TSource) =>
             (s_Sum_Int32_TSource_2 ??
             (s_Sum_Int32_TSource_2 = GetMethodInfoOf<IQueryable<object>, Expression<Func<object, int>>, int>(Queryable.Sum<object>).GetGenericMethodDefinition()))
              .MakeGenericMethod(TSource);

        private static MethodInfo s_Sum_NullableInt32_TSource_2;

        public static MethodInfo Sum_NullableInt32_TSource_2(Type TSource) =>
             (s_Sum_NullableInt32_TSource_2 ??
             (s_Sum_NullableInt32_TSource_2 = GetMethodInfoOf<IQueryable<object>, Expression<Func<object, int?>>, int?>(Queryable.Sum<object>).GetGenericMethodDefinition()))
              .MakeGenericMethod(TSource);

        private static MethodInfo s_Sum_Int64_TSource_2;

        public static MethodInfo Sum_Int64_TSource_2(Type TSource) =>
             (s_Sum_Int64_TSource_2 ??
             (s_Sum_Int64_TSource_2 = GetMethodInfoOf<IQueryable<object>, Expression<Func<object, long>>, long>(Queryable.Sum<object>).GetGenericMethodDefinition()))
              .MakeGenericMethod(TSource);

        private static MethodInfo s_Sum_NullableInt64_TSource_2;

        public static MethodInfo Sum_NullableInt64_TSource_2(Type TSource) =>
             (s_Sum_NullableInt64_TSource_2 ??
             (s_Sum_NullableInt64_TSource_2 = GetMethodInfoOf<IQueryable<object>, Expression<Func<object, long?>>, long?>(Queryable.Sum<object>).GetGenericMethodDefinition()))
              .MakeGenericMethod(TSource);

        private static MethodInfo s_Sum_Single_TSource_2;

        public static MethodInfo Sum_Single_TSource_2(Type TSource) =>
             (s_Sum_Single_TSource_2 ??
             (s_Sum_Single_TSource_2 = GetMethodInfoOf<IQueryable<object>, Expression<Func<object, float>>, float>(Queryable.Sum<object>).GetGenericMethodDefinition()))
              .MakeGenericMethod(TSource);

        private static MethodInfo s_Sum_NullableSingle_TSource_2;

        public static MethodInfo Sum_NullableSingle_TSource_2(Type TSource) =>
             (s_Sum_NullableSingle_TSource_2 ??
             (s_Sum_NullableSingle_TSource_2 = GetMethodInfoOf<IQueryable<object>, Expression<Func<object, float?>>, float?>(Queryable.Sum<object>).GetGenericMethodDefinition()))
              .MakeGenericMethod(TSource);

        private static MethodInfo s_Sum_Double_TSource_2;

        public static MethodInfo Sum_Double_TSource_2(Type TSource) =>
             (s_Sum_Double_TSource_2 ??
             (s_Sum_Double_TSource_2 = GetMethodInfoOf<IQueryable<object>, Expression<Func<object, double>>, double>(Queryable.Sum<object>).GetGenericMethodDefinition()))
              .MakeGenericMethod(TSource);

        private static MethodInfo s_Sum_NullableDouble_TSource_2;

        public static MethodInfo Sum_NullableDouble_TSource_2(Type TSource) =>
             (s_Sum_NullableDouble_TSource_2 ??
             (s_Sum_NullableDouble_TSource_2 = GetMethodInfoOf<IQueryable<object>, Expression<Func<object, double?>>, double?>(Queryable.Sum<object>).GetGenericMethodDefinition()))
              .MakeGenericMethod(TSource);

        private static MethodInfo s_Sum_Decimal_TSource_2;

        public static MethodInfo Sum_Decimal_TSource_2(Type TSource) =>
             (s_Sum_Decimal_TSource_2 ??
             (s_Sum_Decimal_TSource_2 = GetMethodInfoOf<IQueryable<object>, Expression<Func<object, decimal>>, decimal>(Queryable.Sum<object>).GetGenericMethodDefinition()))
              .MakeGenericMethod(TSource);

        private static MethodInfo s_Take_TSource_2;

        public static MethodInfo Take_TSource_2(Type TSource) =>
             (s_Take_TSource_2 ??
             (s_Take_TSource_2 = GetMethodInfoOf<IQueryable<object>, int, IQueryable<object>>(Queryable.Take<object>).GetGenericMethodDefinition()))
              .MakeGenericMethod(TSource);

        private static MethodInfo s_TakeWhile_TSource_2;

        public static MethodInfo TakeWhile_TSource_2(Type TSource) =>
             (s_TakeWhile_TSource_2 ??
             (s_TakeWhile_TSource_2 = GetMethodInfoOf<IQueryable<object>, Expression<Func<object, bool>>, IQueryable<object>>(Queryable.TakeWhile<object>).GetGenericMethodDefinition()))
              .MakeGenericMethod(TSource);

        private static MethodInfo s_TakeWhile_Index_TSource_2;

        public static MethodInfo TakeWhile_Index_TSource_2(Type TSource) =>
             (s_TakeWhile_Index_TSource_2 ??
             (s_TakeWhile_Index_TSource_2 = GetMethodInfoOf<IQueryable<object>, Expression<Func<object, int, bool>>, IQueryable<object>>(Queryable.TakeWhile<object>).GetGenericMethodDefinition()))
              .MakeGenericMethod(TSource);

        private static MethodInfo s_ThenBy_TSource_TKey_2;

        public static MethodInfo ThenBy_TSource_TKey_2(Type TSource, Type TKey) =>
             (s_ThenBy_TSource_TKey_2 ??
             (s_ThenBy_TSource_TKey_2 = GetMethodInfoOf<IOrderedQueryable<object>, Expression<Func<object, object>>, IOrderedQueryable<object>>(Queryable.ThenBy<object, object>).GetGenericMethodDefinition()))
              .MakeGenericMethod(TSource, TKey);

        private static MethodInfo s_ThenBy_TSource_TKey_3;

        public static MethodInfo ThenBy_TSource_TKey_3(Type TSource, Type TKey) =>
             (s_ThenBy_TSource_TKey_3 ??
             (s_ThenBy_TSource_TKey_3 = GetMethodInfoOf<IOrderedQueryable<object>, Expression<Func<object, object>>, IComparer<object>, IOrderedQueryable<object>>(Queryable.ThenBy<object, object>).GetGenericMethodDefinition()))
              .MakeGenericMethod(TSource, TKey);

        private static MethodInfo s_ThenByDescending_TSource_TKey_2;

        public static MethodInfo ThenByDescending_TSource_TKey_2(Type TSource, Type TKey) =>
             (s_ThenByDescending_TSource_TKey_2 ??
             (s_ThenByDescending_TSource_TKey_2 = GetMethodInfoOf<IOrderedQueryable<object>, Expression<Func<object, object>>, IOrderedQueryable<object>>(Queryable.ThenByDescending<object, object>).GetGenericMethodDefinition()))
              .MakeGenericMethod(TSource, TKey);

        private static MethodInfo s_ThenByDescending_TSource_TKey_3;

        public static MethodInfo ThenByDescending_TSource_TKey_3(Type TSource, Type TKey) =>
             (s_ThenByDescending_TSource_TKey_3 ??
             (s_ThenByDescending_TSource_TKey_3 = GetMethodInfoOf<IOrderedQueryable<object>, Expression<Func<object, object>>, IComparer<object>, IOrderedQueryable<object>>(Queryable.ThenByDescending<object, object>).GetGenericMethodDefinition()))
              .MakeGenericMethod(TSource, TKey);

        private static MethodInfo s_Union_TSource_2;

        public static MethodInfo Union_TSource_2(Type TSource) =>
             (s_Union_TSource_2 ??
             (s_Union_TSource_2 = GetMethodInfoOf<IQueryable<object>, IEnumerable<object>, IQueryable<object>>(Queryable.Union<object>).GetGenericMethodDefinition()))
              .MakeGenericMethod(TSource);

        private static MethodInfo s_Union_TSource_3;

        public static MethodInfo Union_TSource_3(Type TSource) =>
             (s_Union_TSource_3 ??
             (s_Union_TSource_3 = GetMethodInfoOf<IQueryable<object>, IEnumerable<object>, IEqualityComparer<object>, IQueryable<object>>(Queryable.Union<object>).GetGenericMethodDefinition()))
              .MakeGenericMethod(TSource);

        private static MethodInfo s_Where_TSource_2;

        public static MethodInfo Where_TSource_2(Type TSource) =>
             (s_Where_TSource_2 ??
             (s_Where_TSource_2 = GetMethodInfoOf<IQueryable<object>, Expression<Func<object, bool>>, IQueryable<object>>(Queryable.Where<object>).GetGenericMethodDefinition()))
              .MakeGenericMethod(TSource);

        private static MethodInfo s_Where_Index_TSource_2;

        public static MethodInfo Where_Index_TSource_2(Type TSource) =>
             (s_Where_Index_TSource_2 ??
             (s_Where_Index_TSource_2 = GetMethodInfoOf<IQueryable<object>, Expression<Func<object, int, bool>>, IQueryable<object>>(Queryable.Where<object>).GetGenericMethodDefinition()))
              .MakeGenericMethod(TSource);

        private static MethodInfo s_Zip_TFirst_TSecond_TResult_3;

        public static MethodInfo Zip_TFirst_TSecond_TResult_3(Type TFirst, Type TSecond, Type TResult) =>
             (s_Zip_TFirst_TSecond_TResult_3 ??
             (s_Zip_TFirst_TSecond_TResult_3 = GetMethodInfoOf<IQueryable<object>, IEnumerable<object>, Expression<Func<object, object, object>>, IQueryable<object>>(Queryable.Zip<object, object, object>).GetGenericMethodDefinition()))
              .MakeGenericMethod(TFirst, TSecond, TResult);

        private static MethodInfo GetMethodInfoOf<T1, R>(Func<T1, R> f) => GetMethodInfoOf((Delegate)f);
        private static MethodInfo GetMethodInfoOf<T1, T2, R>(Func<T1, T2, R> f) => GetMethodInfoOf((Delegate)f);
        private static MethodInfo GetMethodInfoOf<T1, T2, T3, R>(Func<T1, T2, T3, R> f) => GetMethodInfoOf((Delegate)f);
        private static MethodInfo GetMethodInfoOf<T1, T2, T3, T4, R>(Func<T1, T2, T3, T4, R> f) => GetMethodInfoOf((Delegate)f);
        private static MethodInfo GetMethodInfoOf<T1, T2, T3, T4, T5, R>(Func<T1, T2, T3, T4, T5, R> f) => GetMethodInfoOf((Delegate)f);
        private static MethodInfo GetMethodInfoOf<T1, T2, T3, T4, T5, T6, R>(Func<T1, T2, T3, T4, T5, T6, R> f) => GetMethodInfoOf((Delegate)f);
        private static MethodInfo GetMethodInfoOf(Delegate f) => f.GetMethodInfo();
    }
}