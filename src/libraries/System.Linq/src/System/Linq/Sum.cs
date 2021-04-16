// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Linq
{
    public static partial class Enumerable
    {
        /// <summary>Computes the sum of a sequence of <see cref="int" /> values.</summary>
        /// <param name="source">A sequence of <see cref="int" /> values to calculate the sum of.</param>
        /// <returns>The sum of the values in the sequence.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <exception cref="System.OverflowException">The sum is larger than <see cref="int.MaxValue" />.</exception>
        /// <remarks>
        /// <para>This method returns zero if <paramref name="source" /> contains no elements.</para>
        /// <para>In Visual Basic query expression syntax, an `Aggregate Into Sum()` clause translates to an invocation of <see cref="O:Enumerable.Sum" />.</para>
        /// </remarks>
        /// <related type="Article" href="/dotnet/visual-basic/language-reference/queries/aggregate-clause">Aggregate Clause (Visual Basic)</related>
        public static int Sum(this IEnumerable<int> source)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            int sum = 0;
            checked
            {
                foreach (int v in source)
                {
                    sum += v;
                }
            }

            return sum;
        }

        /// <summary>Computes the sum of a sequence of nullable <see cref="int" /> values.</summary>
        /// <param name="source">A sequence of nullable <see cref="int" /> values to calculate the sum of.</param>
        /// <returns>The sum of the values in the sequence.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <exception cref="System.OverflowException">The sum is larger than <see cref="int.MaxValue" />.</exception>
        /// <remarks>
        /// <para>This method returns zero if <paramref name="source" /> contains no elements.</para>
        /// <para>The result does not include values that are <see langword="null" />.</para>
        /// <para>In Visual Basic query expression syntax, an `Aggregate Into Sum()` clause translates to an invocation of <see cref="O:Enumerable.Sum" />.</para>
        /// </remarks>
        /// <related type="Article" href="/dotnet/visual-basic/language-reference/queries/aggregate-clause">Aggregate Clause (Visual Basic)</related>
        public static int? Sum(this IEnumerable<int?> source)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            int sum = 0;
            checked
            {
                foreach (int? v in source)
                {
                    if (v != null)
                    {
                        sum += v.GetValueOrDefault();
                    }
                }
            }

            return sum;
        }

        /// <summary>Computes the sum of a sequence of <see cref="long" /> values.</summary>
        /// <param name="source">A sequence of <see cref="long" /> values to calculate the sum of.</param>
        /// <returns>The sum of the values in the sequence.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <exception cref="System.OverflowException">The sum is larger than <see cref="long.MaxValue" />.</exception>
        /// <remarks>
        /// <para>This method returns zero if <paramref name="source" /> contains no elements.</para>
        /// <para>In Visual Basic query expression syntax, an `Aggregate Into Sum()` clause translates to an invocation of <see cref="O:Enumerable.Sum" />.</para>
        /// </remarks>
        /// <related type="Article" href="/dotnet/visual-basic/language-reference/queries/aggregate-clause">Aggregate Clause (Visual Basic)</related>
        public static long Sum(this IEnumerable<long> source)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            long sum = 0;
            checked
            {
                foreach (long v in source)
                {
                    sum += v;
                }
            }

            return sum;
        }

        /// <summary>Computes the sum of a sequence of nullable <see cref="long" /> values.</summary>
        /// <param name="source">A sequence of nullable <see cref="long" /> values to calculate the sum of.</param>
        /// <returns>The sum of the values in the sequence.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <exception cref="System.OverflowException">The sum is larger than <see cref="long.MaxValue" />.</exception>
        /// <remarks>
        /// <para>This method returns zero if <paramref name="source" /> contains no elements.</para>
        /// <para>The result does not include values that are <see langword="null" />.</para>
        /// <para>In Visual Basic query expression syntax, an `Aggregate Into Sum()` clause translates to an invocation of <see cref="O:Enumerable.Sum" />.</para>
        /// </remarks>
        /// <related type="Article" href="/dotnet/visual-basic/language-reference/queries/aggregate-clause">Aggregate Clause (Visual Basic)</related>
        public static long? Sum(this IEnumerable<long?> source)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            long sum = 0;
            checked
            {
                foreach (long? v in source)
                {
                    if (v != null)
                    {
                        sum += v.GetValueOrDefault();
                    }
                }
            }

            return sum;
        }

        /// <summary>Computes the sum of a sequence of <see cref="float" /> values.</summary>
        /// <param name="source">A sequence of <see cref="float" /> values to calculate the sum of.</param>
        /// <returns>The sum of the values in the sequence.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <remarks>
        /// <para>This method returns zero if <paramref name="source" /> contains no elements.</para>
        /// <para>In Visual Basic query expression syntax, an `Aggregate Into Sum()` clause translates to an invocation of <see cref="O:Enumerable.Sum" />.</para>
        /// </remarks>
        /// <example>The following code example demonstrates how to use <see cref="Sum(IEnumerable{float})" /> to sum the values of a sequence.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Enumerable/CS/enumerable.cs" interactive="try-dotnet-method" id="Snippet120":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Enumerable/VB/Enumerable.vb" id="Snippet120":::</example>
        /// <related type="Article" href="/dotnet/visual-basic/language-reference/queries/aggregate-clause">Aggregate Clause (Visual Basic)</related>
        public static float Sum(this IEnumerable<float> source)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            double sum = 0;
            foreach (float v in source)
            {
                sum += v;
            }

            return (float)sum;
        }

        /// <summary>Computes the sum of a sequence of nullable <see cref="float" /> values.</summary>
        /// <param name="source">A sequence of nullable <see cref="float" /> values to calculate the sum of.</param>
        /// <returns>The sum of the values in the sequence.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <remarks>
        /// <para>This method returns zero if <paramref name="source" /> contains no elements.</para>
        /// <para>The result does not include values that are <see langword="null" />.</para>
        /// <para>In Visual Basic query expression syntax, an `Aggregate Into Sum()` clause translates to an invocation of <see cref="O:Enumerable.Sum" />.</para>
        /// </remarks>
        /// <example>The following code example demonstrates how to use <see cref="Sum(IEnumerable{System.Nullable{float}})" /> to sum the values of a sequence.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Enumerable/CS/enumerable.cs" interactive="try-dotnet-method" id="Snippet121":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Enumerable/VB/Enumerable.vb" id="Snippet121":::</example>
        /// <related type="Article" href="/dotnet/visual-basic/language-reference/queries/aggregate-clause">Aggregate Clause (Visual Basic)</related>
        public static float? Sum(this IEnumerable<float?> source)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            double sum = 0;
            foreach (float? v in source)
            {
                if (v != null)
                {
                    sum += v.GetValueOrDefault();
                }
            }

            return (float)sum;
        }

        /// <summary>Computes the sum of a sequence of <see cref="double" /> values.</summary>
        /// <param name="source">A sequence of <see cref="double" /> values to calculate the sum of.</param>
        /// <returns>The sum of the values in the sequence.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <remarks>
        /// <para>This method returns zero if <paramref name="source" /> contains no elements.</para>
        /// <para>In Visual Basic query expression syntax, an `Aggregate Into Sum()` clause translates to an invocation of <see cref="O:Enumerable.Sum" />.</para>
        /// </remarks>
        /// <related type="Article" href="/dotnet/visual-basic/language-reference/queries/aggregate-clause">Aggregate Clause (Visual Basic)</related>
        public static double Sum(this IEnumerable<double> source)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            double sum = 0;
            foreach (double v in source)
            {
                sum += v;
            }

            return sum;
        }

        /// <summary>Computes the sum of a sequence of nullable <see cref="double" /> values.</summary>
        /// <param name="source">A sequence of nullable <see cref="double" /> values to calculate the sum of.</param>
        /// <returns>The sum of the values in the sequence.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <remarks>
        /// <para>This method returns zero if <paramref name="source" /> contains no elements.</para>
        /// <para>The result does not include values that are <see langword="null" />.</para>
        /// <para>In Visual Basic query expression syntax, an `Aggregate Into Sum()` clause translates to an invocation of <see cref="O:Enumerable.Sum" />.</para>
        /// </remarks>
        /// <related type="Article" href="/dotnet/visual-basic/language-reference/queries/aggregate-clause">Aggregate Clause (Visual Basic)</related>
        public static double? Sum(this IEnumerable<double?> source)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            double sum = 0;
            foreach (double? v in source)
            {
                if (v != null)
                {
                    sum += v.GetValueOrDefault();
                }
            }

            return sum;
        }

        /// <summary>Computes the sum of a sequence of <see cref="decimal" /> values.</summary>
        /// <param name="source">A sequence of <see cref="decimal" /> values to calculate the sum of.</param>
        /// <returns>The sum of the values in the sequence.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <exception cref="System.OverflowException">The sum is larger than <see cref="decimal.MaxValue" />.</exception>
        /// <remarks>
        /// <para>The <see cref="Sum(IEnumerable{decimal})" /> method returns zero if <paramref name="source" /> contains no elements.</para>
        /// <para>In Visual Basic query expression syntax, an `Aggregate Into Sum()` clause translates to an invocation of <see cref="O:Enumerable.Sum" />.</para>
        /// </remarks>
        /// <related type="Article" href="/dotnet/visual-basic/language-reference/queries/aggregate-clause">Aggregate Clause (Visual Basic)</related>
        public static decimal Sum(this IEnumerable<decimal> source)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            decimal sum = 0;
            foreach (decimal v in source)
            {
                sum += v;
            }

            return sum;
        }

        /// <summary>Computes the sum of a sequence of nullable <see cref="decimal" /> values.</summary>
        /// <param name="source">A sequence of nullable <see cref="decimal" /> values to calculate the sum of.</param>
        /// <returns>The sum of the values in the sequence.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <exception cref="System.OverflowException">The sum is larger than <see cref="decimal.MaxValue" />.</exception>
        /// <remarks>
        /// <para>This method returns zero if <paramref name="source" /> contains no elements.</para>
        /// <para>The result doesnot include values that are <see langword="null" />.</para>
        /// <para>In Visual Basic query expression syntax, an `Aggregate Into Sum()` clause translates to an invocation of <see cref="O:Enumerable.Sum" />.</para>
        /// </remarks>
        /// <related type="Article" href="/dotnet/visual-basic/language-reference/queries/aggregate-clause">Aggregate Clause (Visual Basic)</related>
        public static decimal? Sum(this IEnumerable<decimal?> source)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            decimal sum = 0;
            foreach (decimal? v in source)
            {
                if (v != null)
                {
                    sum += v.GetValueOrDefault();
                }
            }

            return sum;
        }

        /// <summary>Computes the sum of the sequence of <see cref="int" /> values that are obtained by invoking a transform function on each element of the input sequence.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence of values that are used to calculate a sum.</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <returns>The sum of the projected values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> or <paramref name="selector" /> is <see langword="null" />.</exception>
        /// <exception cref="System.OverflowException">The sum is larger than <see cref="int.MaxValue" />.</exception>
        /// <remarks><format type="text/markdown"><![CDATA[
        /// This method returns zero if `source` contains no elements.
        /// You can apply this method to a sequence of arbitrary values if you provide a function, `selector`, that projects the members of `source` into a numeric type, specifically <xref:System.Int32>.
        /// In Visual Basic query expression syntax, an `Aggregate Into Sum()` clause translates to an invocation of <xref:System.Linq.Enumerable.Sum%2A>.
        /// ## Examples
        /// The following code example demonstrates how to use <xref:System.Linq.Enumerable.Sum%60%601%28System.Collections.Generic.IEnumerable%7B%60%600%7D%2CSystem.Func%7B%60%600%2CSystem.Double%7D%29> to sum the projected values of a sequence.
        /// [!INCLUDE[sqo_diff_overload_example_func](~/includes/sqo-diff-overload-example-func-md.md)]
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Enumerable/CS/enumerable.cs" id="Snippet98":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Enumerable/VB/Enumerable.vb" id="Snippet98":::
        /// ]]></format></remarks>
        /// <related type="Article" href="/dotnet/visual-basic/language-reference/queries/aggregate-clause">Aggregate Clause (Visual Basic)</related>
        public static int Sum<TSource>(this IEnumerable<TSource> source, Func<TSource, int> selector)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (selector == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.selector);
            }

            int sum = 0;
            checked
            {
                foreach (TSource item in source)
                {
                    sum += selector(item);
                }
            }

            return sum;
        }

        /// <summary>Computes the sum of the sequence of nullable <see cref="int" /> values that are obtained by invoking a transform function on each element of the input sequence.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence of values that are used to calculate a sum.</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <returns>The sum of the projected values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> or <paramref name="selector" /> is <see langword="null" />.</exception>
        /// <exception cref="System.OverflowException">The sum is larger than <see cref="int.MaxValue" />.</exception>
        /// <remarks><format type="text/markdown"><![CDATA[
        /// This method returns zero if `source` contains no elements.
        /// The result does not include values that are `null`.
        /// You can apply this method to a sequence of arbitrary values if you provide a function, `selector`, that projects the members of `source` into a numeric type, specifically `Nullable<Int32>` in C# or `Nullable(Of Int32)` in Visual Basic.
        /// In Visual Basic query expression syntax, an `Aggregate Into Sum()` clause translates to an invocation of <xref:System.Linq.Enumerable.Sum%2A>.
        /// ## Examples
        /// The following code example demonstrates how to use <xref:System.Linq.Enumerable.Sum%60%601%28System.Collections.Generic.IEnumerable%7B%60%600%7D%2CSystem.Func%7B%60%600%2CSystem.Double%7D%29> to sum the projected values of a sequence.
        /// [!INCLUDE[sqo_diff_overload_example_func](~/includes/sqo-diff-overload-example-func-md.md)]
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Enumerable/CS/enumerable.cs" id="Snippet98":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Enumerable/VB/Enumerable.vb" id="Snippet98":::
        /// ]]></format></remarks>
        /// <related type="Article" href="/dotnet/visual-basic/language-reference/queries/aggregate-clause">Aggregate Clause (Visual Basic)</related>
        public static int? Sum<TSource>(this IEnumerable<TSource> source, Func<TSource, int?> selector)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (selector == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.selector);
            }

            int sum = 0;
            checked
            {
                foreach (TSource item in source)
                {
                    int? v = selector(item);
                    if (v != null)
                    {
                        sum += v.GetValueOrDefault();
                    }
                }
            }

            return sum;
        }

        /// <summary>Computes the sum of the sequence of <see cref="long" /> values that are obtained by invoking a transform function on each element of the input sequence.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence of values that are used to calculate a sum.</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <returns>The sum of the projected values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> or <paramref name="selector" /> is <see langword="null" />.</exception>
        /// <exception cref="System.OverflowException">The sum is larger than <see cref="long.MaxValue" />.</exception>
        /// <remarks><format type="text/markdown"><![CDATA[
        /// This method returns zero if `source` contains no elements.
        /// You can apply this method to a sequence of arbitrary values if you provide a function, `selector`, that projects the members of `source` into a numeric type, specifically <xref:System.Int64>.
        /// In Visual Basic query expression syntax, an `Aggregate Into Sum()` clause translates to an invocation of <xref:System.Linq.Enumerable.Sum%2A>.
        /// ## Examples
        /// The following code example demonstrates how to use <xref:System.Linq.Enumerable.Sum%60%601%28System.Collections.Generic.IEnumerable%7B%60%600%7D%2CSystem.Func%7B%60%600%2CSystem.Double%7D%29> to sum the projected values of a sequence.
        /// [!INCLUDE[sqo_diff_overload_example_func](~/includes/sqo-diff-overload-example-func-md.md)]
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Enumerable/CS/enumerable.cs" id="Snippet98":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Enumerable/VB/Enumerable.vb" id="Snippet98":::
        /// ]]></format></remarks>
        /// <related type="Article" href="/dotnet/visual-basic/language-reference/queries/aggregate-clause">Aggregate Clause (Visual Basic)</related>
        public static long Sum<TSource>(this IEnumerable<TSource> source, Func<TSource, long> selector)
        {
            if (selector == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.selector);
            }

            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            long sum = 0;
            checked
            {
                foreach (TSource item in source)
                {
                    sum += selector(item);
                }
            }

            return sum;
        }

        /// <summary>Computes the sum of the sequence of nullable <see cref="long" /> values that are obtained by invoking a transform function on each element of the input sequence.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence of values that are used to calculate a sum.</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <returns>The sum of the projected values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> or <paramref name="selector" /> is <see langword="null" />.</exception>
        /// <exception cref="System.OverflowException">The sum is larger than <see cref="long.MaxValue" />.</exception>
        /// <remarks><format type="text/markdown"><![CDATA[
        /// This method returns zero if `source` contains no elements.
        /// The result does not include values that are `null`.
        /// You can apply this method to a sequence of arbitrary values if you provide a function, `selector`, that projects the members of `source` into a numeric type, specifically `Nullable<Int64>` in C# or `Nullable(Of Int64)` in Visual Basic
        /// In Visual Basic query expression syntax, an `Aggregate Into Sum()` clause translates to an invocation of <xref:System.Linq.Enumerable.Sum%2A>.
        /// ## Examples
        /// The following code example demonstrates how to use <xref:System.Linq.Enumerable.Sum%60%601%28System.Collections.Generic.IEnumerable%7B%60%600%7D%2CSystem.Func%7B%60%600%2CSystem.Double%7D%29> to sum the projected values of a sequence.
        /// [!INCLUDE[sqo_diff_overload_example_func](~/includes/sqo-diff-overload-example-func-md.md)]
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Enumerable/CS/enumerable.cs" id="Snippet98":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Enumerable/VB/Enumerable.vb" id="Snippet98":::
        /// ]]></format></remarks>
        /// <related type="Article" href="/dotnet/visual-basic/language-reference/queries/aggregate-clause">Aggregate Clause (Visual Basic)</related>
        public static long? Sum<TSource>(this IEnumerable<TSource> source, Func<TSource, long?> selector)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (selector == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.selector);
            }

            long sum = 0;
            checked
            {
                foreach (TSource item in source)
                {
                    long? v = selector(item);
                    if (v != null)
                    {
                        sum += v.GetValueOrDefault();
                    }
                }
            }

            return sum;
        }

        /// <summary>Computes the sum of the sequence of <see cref="float" /> values that are obtained by invoking a transform function on each element of the input sequence.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence of values that are used to calculate a sum.</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <returns>The sum of the projected values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> or <paramref name="selector" /> is <see langword="null" />.</exception>
        /// <remarks><format type="text/markdown"><![CDATA[
        /// The <xref:System.Linq.Enumerable.Sum%60%601%28System.Collections.Generic.IEnumerable%7B%60%600%7D%2CSystem.Func%7B%60%600%2CSystem.Single%7D%29> method returns zero if `source` contains no elements.
        /// You can apply this method to a sequence of arbitrary values if you provide a function, `selector`, that projects the members of `source` into a numeric type, specifically <xref:System.Single>.
        /// In Visual Basic query expression syntax, an `Aggregate Into Sum()` clause translates to an invocation of <xref:System.Linq.Enumerable.Sum%2A>.
        /// ## Examples
        /// The following code example demonstrates how to use <xref:System.Linq.Enumerable.Sum%60%601%28System.Collections.Generic.IEnumerable%7B%60%600%7D%2CSystem.Func%7B%60%600%2CSystem.Double%7D%29> to sum the projected values of a sequence.
        /// [!INCLUDE[sqo_diff_overload_example_func](~/includes/sqo-diff-overload-example-func-md.md)]
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Enumerable/CS/enumerable.cs" id="Snippet98":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Enumerable/VB/Enumerable.vb" id="Snippet98":::
        /// ]]></format></remarks>
        /// <related type="Article" href="/dotnet/visual-basic/language-reference/queries/aggregate-clause">Aggregate Clause (Visual Basic)</related>
        public static float Sum<TSource>(this IEnumerable<TSource> source, Func<TSource, float> selector)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (selector == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.selector);
            }

            double sum = 0;
            foreach (TSource item in source)
            {
                sum += selector(item);
            }

            return (float)sum;
        }

        /// <summary>Computes the sum of the sequence of nullable <see cref="float" /> values that are obtained by invoking a transform function on each element of the input sequence.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence of values that are used to calculate a sum.</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <returns>The sum of the projected values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> or <paramref name="selector" /> is <see langword="null" />.</exception>
        /// <remarks><format type="text/markdown"><![CDATA[
        /// This method returns zero if `source` contains no elements.
        /// The result does not include values that are `null`.
        /// You can apply this method to a sequence of arbitrary values if you provide a function, `selector`, that projects the members of `source` into a numeric type, specifically `Nullable<Single>` in C# or `Nullable(Of Single)` in Visual Basic.
        /// In Visual Basic query expression syntax, an `Aggregate Into Sum()` clause translates to an invocation of <xref:System.Linq.Enumerable.Sum%2A>.
        /// ## Examples
        /// The following code example demonstrates how to use <xref:System.Linq.Enumerable.Sum%60%601%28System.Collections.Generic.IEnumerable%7B%60%600%7D%2CSystem.Func%7B%60%600%2CSystem.Double%7D%29> to sum the projected values of a sequence.
        /// [!INCLUDE[sqo_diff_overload_example_func](~/includes/sqo-diff-overload-example-func-md.md)]
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Enumerable/CS/enumerable.cs" id="Snippet98":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Enumerable/VB/Enumerable.vb" id="Snippet98":::
        /// ]]></format></remarks>
        /// <related type="Article" href="/dotnet/visual-basic/language-reference/queries/aggregate-clause">Aggregate Clause (Visual Basic)</related>
        public static float? Sum<TSource>(this IEnumerable<TSource> source, Func<TSource, float?> selector)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (selector == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.selector);
            }

            double sum = 0;
            foreach (TSource item in source)
            {
                float? v = selector(item);
                if (v != null)
                {
                    sum += v.GetValueOrDefault();
                }
            }

            return (float)sum;
        }

        /// <summary>Computes the sum of the sequence of <see cref="double" /> values that are obtained by invoking a transform function on each element of the input sequence.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence of values that are used to calculate a sum.</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <returns>The sum of the projected values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> or <paramref name="selector" /> is <see langword="null" />.</exception>
        /// <remarks>
        /// <para>This method returns zero if <paramref name="source" /> contains no elements.</para>
        /// <para>You can apply this method to a sequence of arbitrary values if you provide a function, <paramref name="selector" />, that projects the members of <paramref name="source" /> into a numeric type, specifically <see cref="double" />.</para>
        /// <para>In Visual Basic query expression syntax, an `Aggregate Into Sum()` clause translates to an invocation of <see cref="O:Enumerable.Sum" />.</para>
        /// </remarks>
        /// <example>The following code example demonstrates how to use <see cref="Sum{T}(IEnumerable{T},Func{T,double})" /> to sum the projected values of a sequence.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Enumerable/CS/enumerable.cs" id="Snippet98":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Enumerable/VB/Enumerable.vb" id="Snippet98":::</example>
        /// <related type="Article" href="/dotnet/visual-basic/language-reference/queries/aggregate-clause">Aggregate Clause (Visual Basic)</related>
        public static double Sum<TSource>(this IEnumerable<TSource> source, Func<TSource, double> selector)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (selector == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.selector);
            }

            double sum = 0;
            foreach (TSource item in source)
            {
                sum += selector(item);
            }

            return sum;
        }

        /// <summary>Computes the sum of the sequence of nullable <see cref="double" /> values that are obtained by invoking a transform function on each element of the input sequence.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence of values that are used to calculate a sum.</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <returns>The sum of the projected values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> or <paramref name="selector" /> is <see langword="null" />.</exception>
        /// <remarks><format type="text/markdown"><![CDATA[
        /// This method returns zero if `source` contains no elements.
        /// The result does not include values that are `null`.
        /// You can apply this method to a sequence of arbitrary values if you provide a function, `selector`, that projects the members of `source` into a numeric type, specifically `Nullable<Double>` in C# or `Nullable(Of Double)` in Visual Basic.
        /// In Visual Basic query expression syntax, an `Aggregate Into Sum()` clause translates to an invocation of <xref:System.Linq.Enumerable.Sum%2A>.
        /// ## Examples
        /// The following code example demonstrates how to use <xref:System.Linq.Enumerable.Sum%60%601%28System.Collections.Generic.IEnumerable%7B%60%600%7D%2CSystem.Func%7B%60%600%2CSystem.Double%7D%29> to sum the projected values of a sequence.
        /// [!INCLUDE[sqo_diff_overload_example_func](~/includes/sqo-diff-overload-example-func-md.md)]
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Enumerable/CS/enumerable.cs" id="Snippet98":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Enumerable/VB/Enumerable.vb" id="Snippet98":::
        /// ]]></format></remarks>
        /// <related type="Article" href="/dotnet/visual-basic/language-reference/queries/aggregate-clause">Aggregate Clause (Visual Basic)</related>
        public static double? Sum<TSource>(this IEnumerable<TSource> source, Func<TSource, double?> selector)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (selector == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.selector);
            }

            double sum = 0;
            foreach (TSource item in source)
            {
                double? v = selector(item);
                if (v != null)
                {
                    sum += v.GetValueOrDefault();
                }
            }

            return sum;
        }

        /// <summary>Computes the sum of the sequence of <see cref="decimal" /> values that are obtained by invoking a transform function on each element of the input sequence.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence of values that are used to calculate a sum.</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <returns>The sum of the projected values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> or <paramref name="selector" /> is <see langword="null" />.</exception>
        /// <exception cref="System.OverflowException">The sum is larger than <see cref="decimal.MaxValue" />.</exception>
        /// <remarks><format type="text/markdown"><![CDATA[
        /// This method returns zero if `source` contains no elements.
        /// You can apply this method to a sequence of arbitrary values if you provide a function, `selector`, that projects the members of `source` into a numeric type, specifically <xref:System.Decimal>.
        /// In Visual Basic query expression syntax, an `Aggregate Into Sum()` clause translates to an invocation of <xref:System.Linq.Enumerable.Sum%2A>.
        /// ## Examples
        /// The following code example demonstrates how to use <xref:System.Linq.Enumerable.Sum%60%601%28System.Collections.Generic.IEnumerable%7B%60%600%7D%2CSystem.Func%7B%60%600%2CSystem.Double%7D%29> to sum the projected values of a sequence.
        /// [!INCLUDE[sqo_diff_overload_example_func](~/includes/sqo-diff-overload-example-func-md.md)]
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Enumerable/CS/enumerable.cs" id="Snippet98":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Enumerable/VB/Enumerable.vb" id="Snippet98":::
        /// ]]></format></remarks>
        /// <related type="Article" href="/dotnet/visual-basic/language-reference/queries/aggregate-clause">Aggregate Clause (Visual Basic)</related>
        public static decimal Sum<TSource>(this IEnumerable<TSource> source, Func<TSource, decimal> selector)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (selector == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.selector);
            }

            decimal sum = 0;
            foreach (TSource item in source)
            {
                sum += selector(item);
            }

            return sum;
        }

        /// <summary>Computes the sum of the sequence of nullable <see cref="decimal" /> values that are obtained by invoking a transform function on each element of the input sequence.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence of values that are used to calculate a sum.</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <returns>The sum of the projected values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> or <paramref name="selector" /> is <see langword="null" />.</exception>
        /// <exception cref="System.OverflowException">The sum is larger than <see cref="decimal.MaxValue" />.</exception>
        /// <remarks><format type="text/markdown"><![CDATA[
        /// This method returns zero if `source` contains no elements.
        /// The result does not included values that are `null`.
        /// You can apply this method to a sequence of arbitrary values if you provide a function, `selector`, that projects the members of `source` into a numeric type, specifically `Nullable<Decimal>` in C# or `Nullable(Of Decimal)` in Visual Basic.
        /// In Visual Basic query expression syntax, an `Aggregate Into Sum()` clause translates to an invocation of <xref:System.Linq.Enumerable.Sum%2A>.
        /// ## Examples
        /// The following code example demonstrates how to use <xref:System.Linq.Enumerable.Sum%60%601%28System.Collections.Generic.IEnumerable%7B%60%600%7D%2CSystem.Func%7B%60%600%2CSystem.Double%7D%29> to sum the projected values of a sequence.
        /// [!INCLUDE[sqo_diff_overload_example_func](~/includes/sqo-diff-overload-example-func-md.md)]
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Enumerable/CS/enumerable.cs" id="Snippet98":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Enumerable/VB/Enumerable.vb" id="Snippet98":::
        /// ]]></format></remarks>
        /// <related type="Article" href="/dotnet/visual-basic/language-reference/queries/aggregate-clause">Aggregate Clause (Visual Basic)</related>
        public static decimal? Sum<TSource>(this IEnumerable<TSource> source, Func<TSource, decimal?> selector)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (selector == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.selector);
            }

            decimal sum = 0;
            foreach (TSource item in source)
            {
                decimal? v = selector(item);
                if (v != null)
                {
                    sum += v.GetValueOrDefault();
                }
            }

            return sum;
        }
    }
}
