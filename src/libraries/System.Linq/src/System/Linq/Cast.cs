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

            if (default(TResult) is not null && source is IEnumerable<TResult> typedSource)
            {
                // The source was already an IEnumerable<TResult> and TResult can't be null. As
                // such, all values the original input can yield are valid, and we can just return
                // the strongly-typed input directly as if this were Cast rather than OfType.
                return typedSource;
            }

            return new OfTypeIterator<TResult>(source);
        }

        private sealed partial class OfTypeIterator<TResult>(IEnumerable source) : Iterator<TResult>
        {
            private readonly IEnumerable _source = source;
            private IEnumerator? _enumerator;

            public override Iterator<TResult> Clone() => new OfTypeIterator<TResult>(_source);

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
                        while (_enumerator.MoveNext())
                        {
                            if (_enumerator.Current is TResult result)
                            {
                                _current = result;
                                return true;
                            }
                        }

                        Dispose();
                        break;
                }

                return false;
            }

            public override void Dispose()
            {
                (_enumerator as IDisposable)?.Dispose();
                _enumerator = null;

                base.Dispose();
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

            if (source is ICollection collection)
            {
                return new CastICollectionIterator<TResult>(collection);
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
        private sealed partial class CastICollectionIterator<TResult>(ICollection source) : Iterator<TResult>
        {
            private readonly ICollection _source = source;
            private IEnumerator? _enumerator;

            public override Iterator<TResult> Clone() => new CastICollectionIterator<TResult>(_source);

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
                (_enumerator as IDisposable)?.Dispose();
                _enumerator = null;

                base.Dispose();
            }
        }
    }
}
