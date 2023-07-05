// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using static System.Linq.Utilities;

namespace System.Linq
{
    public static partial class Enumerable
    {
        public static IEnumerable<TResult> Select<TSource, TResult>(
            this IEnumerable<TSource> source, Func<TSource, TResult> selector)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (selector == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.selector);
            }

            if (source is Iterator<TSource> iterator)
            {
                return iterator.Select(selector);
            }

            if (source is IList<TSource> ilist)
            {
                if (source is TSource[] array)
                {
                    return array.Length == 0 ?
                        Empty<TResult>() :
                        new SelectArrayIterator<TSource, TResult>(array, selector);
                }

                if (source is List<TSource> list)
                {
                    return new SelectListIterator<TSource, TResult>(list, selector);
                }

                return new SelectIListIterator<TSource, TResult>(ilist, selector);
            }

            if (source is IPartition<TSource> partition)
            {
                IEnumerable<TResult>? result = null;
                CreateSelectIPartitionIterator(selector, partition, ref result);
                if (result != null)
                {
                    return result;
                }
            }

            return new SelectEnumerableIterator<TSource, TResult>(source, selector);
        }

#pragma warning disable IDE0060 // https://github.com/dotnet/roslyn-analyzers/issues/6177
        static partial void CreateSelectIPartitionIterator<TResult, TSource>(
            Func<TSource, TResult> selector, IPartition<TSource> partition, [NotNull] ref IEnumerable<TResult>? result);
#pragma warning restore IDE0060

        public static IEnumerable<TResult> Select<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, int, TResult> selector)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (selector == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.selector);
            }

            return SelectIterator(source, selector);
        }

        private static IEnumerable<TResult> SelectIterator<TSource, TResult>(IEnumerable<TSource> source, Func<TSource, int, TResult> selector)
        {
            int index = -1;
            foreach (TSource element in source)
            {
                checked
                {
                    index++;
                }

                yield return selector(element, index);
            }
        }

        /// <summary>
        /// An iterator that maps each item of an <see cref="IEnumerable{TSource}"/>.
        /// </summary>
        /// <typeparam name="TSource">The type of the source enumerable.</typeparam>
        /// <typeparam name="TResult">The type of the mapped items.</typeparam>
        private sealed partial class SelectEnumerableIterator<TSource, TResult> : Iterator<TResult>
        {
            private readonly IEnumerable<TSource> _source;
            private readonly Func<TSource, TResult> _selector;
            private IEnumerator<TSource>? _enumerator;

            public SelectEnumerableIterator(IEnumerable<TSource> source, Func<TSource, TResult> selector)
            {
                Debug.Assert(source != null);
                Debug.Assert(selector != null);
                _source = source;
                _selector = selector;
            }

            public override Iterator<TResult> Clone() =>
                new SelectEnumerableIterator<TSource, TResult>(_source, _selector);

            public override void Dispose()
            {
                if (_enumerator != null)
                {
                    _enumerator.Dispose();
                    _enumerator = null;
                }

                base.Dispose();
            }

            public override bool MoveNext()
            {
                switch (_state)
                {
                    case 1:
                        _enumerator = _source.GetEnumerator();
                        _state = 2;
                        goto case 2;
                    case 2:
                        Debug.Assert(_enumerator != null);
                        if (_enumerator.MoveNext())
                        {
                            _current = _selector(_enumerator.Current);
                            return true;
                        }

                        Dispose();
                        break;
                }

                return false;
            }

            public override IEnumerable<TResult2> Select<TResult2>(Func<TResult, TResult2> selector) =>
                new SelectEnumerableIterator<TSource, TResult2>(_source, CombineSelectors(_selector, selector));
        }

        /// <summary>
        /// An iterator that maps each item of an array.
        /// </summary>
        /// <typeparam name="TSource">The type of the source array.</typeparam>
        /// <typeparam name="TResult">The type of the mapped items.</typeparam>
        [DebuggerDisplay("Count = {CountForDebugger}")]
        private sealed partial class SelectArrayIterator<TSource, TResult> : Iterator<TResult>
        {
            private readonly TSource[] _source;
            private readonly Func<TSource, TResult> _selector;

            public SelectArrayIterator(TSource[] source, Func<TSource, TResult> selector)
            {
                Debug.Assert(source != null);
                Debug.Assert(selector != null);
                Debug.Assert(source.Length > 0); // Caller should check this beforehand and return a cached result
                _source = source;
                _selector = selector;
            }

            private int CountForDebugger => _source.Length;

            public override Iterator<TResult> Clone() => new SelectArrayIterator<TSource, TResult>(_source, _selector);

            public override bool MoveNext()
            {
                TSource[] source = _source;
                int index = _state - 1;
                if ((uint)index < (uint)source.Length)
                {
                    _state++;
                    _current = _selector(source[index]);
                    return true;
                }

                Dispose();
                return false;
            }

            public override IEnumerable<TResult2> Select<TResult2>(Func<TResult, TResult2> selector) =>
                new SelectArrayIterator<TSource, TResult2>(_source, CombineSelectors(_selector, selector));
        }

        /// <summary>
        /// An iterator that maps each item of a <see cref="List{TSource}"/>.
        /// </summary>
        /// <typeparam name="TSource">The type of the source list.</typeparam>
        /// <typeparam name="TResult">The type of the mapped items.</typeparam>
        [DebuggerDisplay("Count = {CountForDebugger}")]
        private sealed partial class SelectListIterator<TSource, TResult> : Iterator<TResult>
        {
            private readonly List<TSource> _source;
            private readonly Func<TSource, TResult> _selector;
            private List<TSource>.Enumerator _enumerator;

            public SelectListIterator(List<TSource> source, Func<TSource, TResult> selector)
            {
                Debug.Assert(source != null);
                Debug.Assert(selector != null);
                _source = source;
                _selector = selector;
            }

            private int CountForDebugger => _source.Count;

            public override Iterator<TResult> Clone() => new SelectListIterator<TSource, TResult>(_source, _selector);

            public override bool MoveNext()
            {
                switch (_state)
                {
                    case 1:
                        _enumerator = _source.GetEnumerator();
                        _state = 2;
                        goto case 2;
                    case 2:
                        if (_enumerator.MoveNext())
                        {
                            _current = _selector(_enumerator.Current);
                            return true;
                        }

                        Dispose();
                        break;
                }

                return false;
            }

            public override IEnumerable<TResult2> Select<TResult2>(Func<TResult, TResult2> selector) =>
                new SelectListIterator<TSource, TResult2>(_source, CombineSelectors(_selector, selector));
        }

        /// <summary>
        /// An iterator that maps each item of an <see cref="IList{TSource}"/>.
        /// </summary>
        /// <typeparam name="TSource">The type of the source list.</typeparam>
        /// <typeparam name="TResult">The type of the mapped items.</typeparam>
        [DebuggerDisplay("Count = {CountForDebugger}")]
        private sealed partial class SelectIListIterator<TSource, TResult> : Iterator<TResult>
        {
            private readonly IList<TSource> _source;
            private readonly Func<TSource, TResult> _selector;
            private IEnumerator<TSource>? _enumerator;

            public SelectIListIterator(IList<TSource> source, Func<TSource, TResult> selector)
            {
                Debug.Assert(source != null);
                Debug.Assert(selector != null);
                _source = source;
                _selector = selector;
            }

            private int CountForDebugger => _source.Count;

            public override Iterator<TResult> Clone() => new SelectIListIterator<TSource, TResult>(_source, _selector);

            public override bool MoveNext()
            {
                switch (_state)
                {
                    case 1:
                        _enumerator = _source.GetEnumerator();
                        _state = 2;
                        goto case 2;
                    case 2:
                        Debug.Assert(_enumerator != null);
                        if (_enumerator.MoveNext())
                        {
                            _current = _selector(_enumerator.Current);
                            return true;
                        }

                        Dispose();
                        break;
                }

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

            public override IEnumerable<TResult2> Select<TResult2>(Func<TResult, TResult2> selector) =>
                new SelectIListIterator<TSource, TResult2>(_source, CombineSelectors(_selector, selector));
        }
    }
}
