// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Security.Cryptography;

namespace Internal.Cryptography
{
    internal readonly ref struct PemEnumerator
    {
        private readonly ReadOnlySpan<char> _contents;

        public PemEnumerator(ReadOnlySpan<char> contents)
        {
            _contents = contents;
        }

        public Enumerator GetEnumerator() => new Enumerator(_contents);

        internal ref struct Enumerator
        {
            private ReadOnlySpan<char> _contents;
            private PemFields _pemFields;

            public Enumerator(ReadOnlySpan<char> contents)
            {
                _contents = contents;
                _pemFields = default;
            }

            public PemFieldItem Current => new PemFieldItem(_contents, _pemFields);

            public bool MoveNext()
            {
                _contents = _contents[_pemFields.Location.End..];
                return PemEncoding.TryFind(_contents, out _pemFields);
            }

            internal readonly ref struct PemFieldItem
            {
                private readonly ReadOnlySpan<char> _contents;
                private readonly PemFields _pemFields;

                public PemFieldItem(ReadOnlySpan<char> contents, PemFields pemFields)
                {
                    _contents = contents;
                    _pemFields = pemFields;
                }

                public void Deconstruct(out ReadOnlySpan<char> contents, out PemFields pemFields)
                {
                    contents = _contents;
                    pemFields = _pemFields;
                }
            }
        }
    }
}
