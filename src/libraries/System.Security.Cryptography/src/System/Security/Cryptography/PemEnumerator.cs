// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography
{
    internal static class PemEnumerator
    {
        internal static PemEnumerator<char> Utf16(ReadOnlySpan<char> pemData)
        {
            return new PemEnumerator<char>(pemData, PemEncoding.TryFind);
        }

        internal static PemEnumerator<byte> Utf8(ReadOnlySpan<byte> pemData)
        {
            return new PemEnumerator<byte>(pemData, PemEncoding.TryFindUtf8);
        }
    }

    internal readonly ref struct PemEnumerator<TChar>
    {
        internal delegate bool TryFindFunc(ReadOnlySpan<TChar> pemData, out PemFields fields);

        private readonly ReadOnlySpan<TChar> _contents;
        private readonly TryFindFunc _tryFindFunc;

        internal PemEnumerator(ReadOnlySpan<TChar> contents, TryFindFunc findFunc)
        {
            _contents = contents;
            _tryFindFunc = findFunc;
        }

        public Enumerator GetEnumerator() => new Enumerator(_contents, _tryFindFunc);

        internal ref struct Enumerator
        {
            private ReadOnlySpan<TChar> _contents;
            private PemFields _pemFields;
            private readonly TryFindFunc _tryFindFunc;

            internal Enumerator(ReadOnlySpan<TChar> contents, TryFindFunc tryFindFunc)
            {
                _contents = contents;
                _pemFields = default;
                _tryFindFunc = tryFindFunc;
            }

            public readonly PemFieldItem Current => new PemFieldItem(_contents, _pemFields);

            public bool MoveNext()
            {
                _contents = _contents[_pemFields.Location.End..];
                return _tryFindFunc(_contents, out _pemFields);
            }

            internal readonly ref struct PemFieldItem(ReadOnlySpan<TChar> contents, PemFields pemFields)
            {
                private readonly ReadOnlySpan<TChar> _contents = contents;
                private readonly PemFields _pemFields = pemFields;

                public void Deconstruct(out ReadOnlySpan<TChar> contents, out PemFields pemFields)
                {
                    contents = _contents;
                    pemFields = _pemFields;
                }
            }
        }
    }
}
