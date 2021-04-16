// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Linq
{
    public static partial class Enumerable
    {
        /// <summary>Applies an accumulator function over a sequence.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">An <see cref="IEnumerable{T}" /> to aggregate over.</param>
        /// <param name="func">An accumulator function to be invoked on each element.</param>
        /// <returns>The final accumulator value.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source" /> or <paramref name="func" /> is <see langword="null" />.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="source" /> contains no elements.</exception>
        /// <remarks>
        /// <para>
        /// The <see cref="Aggregate{T}(IEnumerable{T},Func{T,T,T})" /> method makes it simple to perform a calculation over a sequence of values.
        /// This method works by calling <paramref name="func" /> one time for each element in <paramref name="source" /> except the first one.
        /// Each time <paramref name="func" /> is called, <see cref="Aggregate{T}(IEnumerable{T},Func{T,T,T})" /> passes both the element from
        /// the sequence and an aggregated value (as the first argument to <paramref name="func" />). The first element of <paramref name="source" />
        /// is used as the initial aggregate value. The result of <paramref name="func" /> replaces the previous aggregated value.
        /// <see cref="Aggregate{T}(IEnumerable{T},Func{T,T,T})" /> returns the final result of <paramref name="func" />.
        /// </para>
        /// <para>
        /// This overload of the <see cref="O:Enumerable.Aggregate" /> method isn't suitable for all cases because it uses the first element of
        /// <paramref name="source" /> as the initial aggregate value. You should choose another overload if the return value should include only the
        /// elements of <paramref name="source" /> that meet a certain condition. For example, this overload isn't reliable if you want to calculate
        /// the sum of the even numbers in <paramref name="source" />. The result will be incorrect if the first element is odd instead of even.
        /// </para>
        /// <para>
        /// To simplify common aggregation operations, the standard query operators also include a general purpose count method, <see cref="O:Enumerable.Count" />,
        /// and four numeric aggregation methods, namely <see cref="O:Enumerable.Min" />, <see cref="O:Enumerable.Max" />, <see cref="O:Enumerable.Sum" />,
        /// and <see cref="O:Enumerable.Average" />
        /// </para>.
        /// </remarks>
        /// <example>The following code example demonstrates how to reverse the order of words in a string by using <see cref="O:Enumerable.Aggregate" />.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Enumerable/CS/enumerable.cs" interactive="try-dotnet-method" id="Snippet1":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Enumerable/VB/Enumerable.vb" id="Snippet1":::</example>
        public static TSource Aggregate<TSource>(this IEnumerable<TSource> source, Func<TSource, TSource, TSource> func)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (func == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.func);
            }

            using (IEnumerator<TSource> e = source.GetEnumerator())
            {
                if (!e.MoveNext())
                {
                    ThrowHelper.ThrowNoElementsException();
                }

                TSource result = e.Current;
                while (e.MoveNext())
                {
                    result = func(result, e.Current);
                }

                return result;
            }
        }

        /// <summary>Applies an accumulator function over a sequence. The specified seed value is used as the initial accumulator value.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <typeparam name="TAccumulate">The type of the accumulator value.</typeparam>
        /// <param name="source">An <see cref="IEnumerable{T}" /> to aggregate over.</param>
        /// <param name="seed">The initial accumulator value.</param>
        /// <param name="func">An accumulator function to be invoked on each element.</param>
        /// <returns>The final accumulator value.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> or <paramref name="func" /> is <see langword="null" />.</exception>
        /// <remarks>
        /// <para>
        /// The <see cref="Aggregate{T1,T2}(IEnumerable{T1},T2,Func{T2,T1,T2})" /> method makes it simple to perform a calculation over a sequence of values.
        /// This method works by calling <paramref name="func" /> one time for each element in <paramref name="source" />. Each time <paramref name="func" />
        /// is called, <see cref="Aggregate{T1,T2}(IEnumerable{T1},T2,Func{T2,T1,T2})" /> passes both the element from the sequence and an aggregated value
        /// (as the first argument to <paramref name="func" />). The value of the <paramref name="seed" /> parameter is used as the initial aggregate value.
        /// The result of <paramref name="func" /> replaces the previous aggregated value. <see cref="Aggregate{T1,T2}(IEnumerable{T1},T2,Func{T2,T1,T2})" />
        /// returns the final result of <paramref name="func" />.
        /// </para>
        /// <para>
        /// To simplify common aggregation operations, the standard query operators also include a general purpose count method, <see cref="O:Enumerable.Count" />,
        /// and four numeric aggregation methods, namely <see cref="O:Enumerable.Min" />, <see cref="O:Enumerable.Max" />, <see cref="O:Enumerable.Sum" />,
        /// and <see cref="O:Enumerable.Average" />.
        /// </para>
        /// </remarks>
        /// <example>The following code example demonstrates how to use <see cref="O:Enumerable.Aggregate" /> to apply an accumulator function and use a seed value.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Enumerable/CS/enumerable.cs" interactive="try-dotnet-method" id="Snippet2":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Enumerable/VB/Enumerable.vb" id="Snippet2":::</example>
        public static TAccumulate Aggregate<TSource, TAccumulate>(this IEnumerable<TSource> source, TAccumulate seed, Func<TAccumulate, TSource, TAccumulate> func)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (func == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.func);
            }

            TAccumulate result = seed;
            foreach (TSource element in source)
            {
                result = func(result, element);
            }

            return result;
        }

        /// <summary>
        /// Applies an accumulator function over a sequence. The specified seed value is used as the initial accumulator value,
        /// and the specified function is used to select the result value.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <typeparam name="TAccumulate">The type of the accumulator value.</typeparam>
        /// <typeparam name="TResult">The type of the resulting value.</typeparam>
        /// <param name="source">An <see cref="IEnumerable{T}" /> to aggregate over.</param>
        /// <param name="seed">The initial accumulator value.</param>
        /// <param name="func">An accumulator function to be invoked on each element.</param>
        /// <param name="resultSelector">A function to transform the final accumulator value into the result value.</param>
        /// <returns>The transformed final accumulator value.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> or <paramref name="func" /> or <paramref name="resultSelector" /> is <see langword="null" />.</exception>
        /// <remarks>
        /// <para>
        /// The <see cref="Aggregate{T1,T2,T3}(IEnumerable{T1},T2,Func{T2,T1,T2},Func{T2,T3})" /> method makes it simple to perform a calculation over a sequence of values.
        /// This method works by calling <paramref name="func" /> one time for each element in <paramref name="source" />. Each time <paramref name="func" /> is called,
        /// <see cref="Aggregate{T1,T2,T3}(IEnumerable{T1},T2,Func{T2,T1,T2},Func{T2,T3})" /> passes both the element from the sequence and an aggregated value
        /// (as the first argument to <paramref name="func" />). The value of the <paramref name="seed" /> parameter is used as the initial aggregate value.
        /// The result of <paramref name="func" /> replaces the previous aggregated value. The final result of <paramref name="func" /> is passed to <paramref name="resultSelector" />
        /// to obtain the final result of <see cref="Aggregate{T1,T2,T3}(IEnumerable{T1},T2,Func{T2,T1,T2},Func{T2,T3})" />.
        /// </para>
        /// <para>
        /// To simplify common aggregation operations, the standard query operators also include a general purpose count method, <see cref="O:Enumerable.Count" />,
        /// and four numeric aggregation methods, namely <see cref="O:Enumerable.Min" />, <see cref="O:Enumerable.Max" />, <see cref="O:Enumerable.Sum" />,
        /// and <see cref="O:Enumerable.Average" />.
        /// </para>
        /// </remarks>
        /// <example>The following code example demonstrates how to use <see cref="O:Enumerable.Aggregate" /> to apply an accumulator function and a result selector.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Enumerable/CS/enumerable.cs" interactive="try-dotnet-method" id="Snippet3":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Enumerable/VB/Enumerable.vb" id="Snippet3":::</example>
        public static TResult Aggregate<TSource, TAccumulate, TResult>(this IEnumerable<TSource> source, TAccumulate seed, Func<TAccumulate, TSource, TAccumulate> func, Func<TAccumulate, TResult> resultSelector)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (func == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.func);
            }

            if (resultSelector == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.resultSelector);
            }

            TAccumulate result = seed;
            foreach (TSource element in source)
            {
                result = func(result, element);
            }

            return resultSelector(result);
        }
    }
}
