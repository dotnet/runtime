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
    }
}
