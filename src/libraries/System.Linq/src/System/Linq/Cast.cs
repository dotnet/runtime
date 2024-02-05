// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace System.Linq
{
    public static partial class Enumerable
    {
        public static IEnumerable<TResult> OfType<TResult>(this IEnumerable source)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            return OfTypeIterator<TResult>(source);
        }

        private static IEnumerable<TResult> OfTypeIterator<TResult>(IEnumerable source)
        {
            foreach (object? obj in source)
            {
                if (obj is TResult result)
                {
                    yield return result;
                }
            }
        }

        public static IEnumerable<
#nullable disable // there's no way to annotate the connection of the nullability of TResult to that of the source
                TResult
#nullable restore
                > Cast<TResult>(this IEnumerable source)
        {
            if (source is IEnumerable<TResult> typedSource)
            {
                return typedSource;
            }

            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (source is IList list)
            {
                return new CastIListIterator<TResult>(list);
            }

            return CastIterator<TResult>(source);
        }

        private static IEnumerable<TResult> CastIterator<TResult>(IEnumerable source)
        {
            foreach (object obj in source)
            {
                yield return (TResult)obj;
            }
        }

        [DebuggerDisplay("Count = {Count}")]
        private sealed partial class CastIListIterator<TResult>(IList source) : Iterator<TResult>, IList<TResult>, IReadOnlyList<TResult>
        {
            private readonly IList _source = source;
            private IEnumerator? _enumerator;

            public override Iterator<TResult> Clone() => new CastIListIterator<TResult>(_source);

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
                            _current = (TResult)_enumerator.Current;
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
                    (_enumerator as IDisposable)?.Dispose();
                    _enumerator = null;
                }

                base.Dispose();
            }

            public int Count => _source.Count;

            public bool IsReadOnly => true;

            public int IndexOf(TResult item) => _source.IndexOf(item);

            public bool Contains(TResult item) => _source.Contains(item);

            public void CopyTo(TResult[] array, int arrayIndex)
            {
                ArgumentNullException.ThrowIfNull(array);

                IList source = _source;
                int count = source.Count;

                if (arrayIndex < 0 || arrayIndex > array.Length - count)
                {
                    throw new ArgumentOutOfRangeException(nameof(arrayIndex));
                }

                for (int i = 0; i < count; i++)
                {
                    array[arrayIndex + i] = (TResult)source[i]!;
                }
            }

            public TResult this[int index]
            {
                get => (TResult)_source[index]!;
                set => throw new NotSupportedException();
            }

            public void Add(TResult item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public void Insert(int index, TResult item) => throw new NotSupportedException();
            public bool Remove(TResult item) => throw new NotSupportedException();
            public void RemoveAt(int index) => throw new NotSupportedException();

            public override IEnumerable<TResult2> Select<TResult2>(Func<TResult, TResult2> selector) =>
                new SelectIListIterator<TResult, TResult2>(this, selector);
        }
    }
}
