// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Linq
{
    public static partial class Enumerable
    {
        /// <summary>Returns the elements of the specified sequence or the type parameter's default value in a singleton collection if the sequence is empty.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">The sequence to return a default value for if it is empty.</param>
        /// <returns>An <see cref="IEnumerable{T}" /> object that contains the default value for the <typeparamref name="TSource" /> type if <paramref name="source" /> is empty; otherwise, <paramref name="source" />.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <remarks>
        /// <para>This method is implemented by using deferred execution. The immediate return value is an object that stores all the information that is required to perform the action. The query represented by this method is not executed until the object is enumerated either by calling its `GetEnumerator` method directly or by using `foreach` in Visual C# or `For Each` in Visual Basic.</para>
        /// <para>The default value for reference and nullable types is <see langword="null" />.</para>
        /// <para>This method can be used to produce a left outer join when it is combined with the <see cref="O:Enumerable.GroupJoin" />) method.</para>
        /// </remarks>
        /// <example>The following code examples demonstrate how to use <see cref="DefaultIfEmpty{T}(IEnumerable{T})" /> to provide a default value in case the source sequence is empty.
        /// This example uses a non-empty sequence.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Enumerable/CS/enumerable.cs" id="Snippet24":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Enumerable/VB/Enumerable.vb" id="Snippet24":::
        /// This example uses an empty sequence.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Enumerable/CS/enumerable.cs" interactive="try-dotnet-method" id="Snippet25":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Enumerable/VB/Enumerable.vb" id="Snippet25":::</example>
        /// <related type="Article" href="https://msdn.microsoft.com/library/442d176d-028c-4beb-8d22-407d4ef89107">Joining</related>
        public static IEnumerable<TSource?> DefaultIfEmpty<TSource>(this IEnumerable<TSource> source) =>
            DefaultIfEmpty(source, default);

        /// <summary>Returns the elements of the specified sequence or the specified value in a singleton collection if the sequence is empty.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">The sequence to return the specified value for if it is empty.</param>
        /// <param name="defaultValue">The value to return if the sequence is empty.</param>
        /// <returns>An <see cref="IEnumerable{T}" /> that contains <paramref name="defaultValue" /> if <paramref name="source" /> is empty; otherwise, <paramref name="source" />.</returns>
        /// <remarks>
        /// <para>This method is implemented by using deferred execution. The immediate return value is an object that stores all the information that is required to perform the action. The query represented by this method is not executed until the object is enumerated either by calling its `GetEnumerator` method directly or by using `foreach` in Visual C# or `For Each` in Visual Basic.</para>
        /// <para>This method can be used to produce a left outer join when it is combined with the <see cref="O:Enumerable.GroupJoin" />) method.</para>
        /// </remarks>
        /// <example>The following code example demonstrates how to use the <see cref="DefaultIfEmpty{T}(IEnumerable{T},T)" /> method and specify a default value. The first sequence is not empty and the second sequence is empty.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Enumerable/CS/enumerable.cs" id="Snippet26":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Enumerable/VB/Enumerable.vb" id="Snippet26":::</example>
        /// <related type="Article" href="https://msdn.microsoft.com/library/442d176d-028c-4beb-8d22-407d4ef89107">Joining</related>
        public static IEnumerable<TSource> DefaultIfEmpty<TSource>(this IEnumerable<TSource> source, TSource defaultValue)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            return new DefaultIfEmptyIterator<TSource>(source, defaultValue);
        }

        private sealed partial class DefaultIfEmptyIterator<TSource> : Iterator<TSource>
        {
            private readonly IEnumerable<TSource> _source;
            private readonly TSource _default;
            private IEnumerator<TSource>? _enumerator;

            public DefaultIfEmptyIterator(IEnumerable<TSource> source, TSource defaultValue)
            {
                Debug.Assert(source != null);
                _source = source;
                _default = defaultValue;
            }

            public override Iterator<TSource> Clone() => new DefaultIfEmptyIterator<TSource>(_source, _default);

            public override bool MoveNext()
            {
                switch (_state)
                {
                    case 1:
                        _enumerator = _source.GetEnumerator();
                        if (_enumerator.MoveNext())
                        {
                            _current = _enumerator.Current;
                            _state = 2;
                        }
                        else
                        {
                            _current = _default;
                            _state = -1;
                        }

                        return true;
                    case 2:
                        Debug.Assert(_enumerator != null);
                        if (_enumerator.MoveNext())
                        {
                            _current = _enumerator.Current;
                            return true;
                        }

                        break;
                }

                Dispose();
                return false;
            }

            public override void Dispose()
            {
                if (_enumerator != null)
                {
                    _enumerator.Dispose();
                    _enumerator = null;
                }

                base.Dispose();
            }
        }
    }
}
