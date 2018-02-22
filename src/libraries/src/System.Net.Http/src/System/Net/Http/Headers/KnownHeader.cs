// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Text;

namespace System.Net.Http.Headers
{
    internal sealed class KnownHeader
    {
        private readonly string _name;
        private readonly HttpHeaderType _headerType;
        private readonly HttpHeaderParser _parser;
        private readonly string[] _knownValues;
        private readonly byte[] _asciiBytesWithColonSpace;

        public KnownHeader(string name) : this(name, HttpHeaderType.Custom, null)
        {
            Debug.Assert(!string.IsNullOrEmpty(name));
            Debug.Assert(HttpRuleParser.GetTokenLength(name, 0) == name.Length);
        }

        public KnownHeader(string name, HttpHeaderType headerType, HttpHeaderParser parser, string[] knownValues = null)
        {
            Debug.Assert(!string.IsNullOrEmpty(name));
            Debug.Assert(HttpRuleParser.GetTokenLength(name, 0) == name.Length);
            Debug.Assert((headerType == HttpHeaderType.Custom) == (parser == null));
            Debug.Assert(knownValues == null || headerType != HttpHeaderType.Custom);

            _name = name;
            _headerType = headerType;
            _parser = parser;
            _knownValues = knownValues;

            _asciiBytesWithColonSpace = new byte[name.Length + 2]; // + 2 for ':' and ' '
            int asciiBytes = Encoding.ASCII.GetBytes(name, _asciiBytesWithColonSpace);
            Debug.Assert(asciiBytes == name.Length);
            _asciiBytesWithColonSpace[_asciiBytesWithColonSpace.Length - 2] = (byte)':';
            _asciiBytesWithColonSpace[_asciiBytesWithColonSpace.Length - 1] = (byte)' ';
        }

        public string Name => _name;
        public HttpHeaderParser Parser => _parser;
        public HttpHeaderType HeaderType => _headerType;
        public string[] KnownValues => _knownValues;
        public byte[] AsciiBytesWithColonSpace => _asciiBytesWithColonSpace;
        public HeaderDescriptor Descriptor => new HeaderDescriptor(this);
    }
}
