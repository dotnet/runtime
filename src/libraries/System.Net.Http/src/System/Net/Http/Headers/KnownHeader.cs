// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Net.Http.HPack;
using System.Text;

namespace System.Net.Http.Headers
{
    internal sealed class KnownHeader
    {
        public KnownHeader(string name, int? http2StaticTableIndex = null) :
            this(name, HttpHeaderType.Custom, parser: null, knownValues: null, http2StaticTableIndex)
        {
            Debug.Assert(!string.IsNullOrEmpty(name));
            Debug.Assert(HttpRuleParser.GetTokenLength(name, 0) == name.Length);
        }

        public KnownHeader(string name, HttpHeaderType headerType, HttpHeaderParser parser, string[] knownValues = null, int? http2StaticTableIndex = null)
        {
            Debug.Assert(!string.IsNullOrEmpty(name));
            Debug.Assert(HttpRuleParser.GetTokenLength(name, 0) == name.Length);
            Debug.Assert((headerType == HttpHeaderType.Custom) == (parser == null));
            Debug.Assert(knownValues == null || headerType != HttpHeaderType.Custom);

            Name = name;
            HeaderType = headerType;
            Parser = parser;
            KnownValues = knownValues;

            Http2EncodedName = http2StaticTableIndex.HasValue ?
                HPackEncoder.EncodeLiteralHeaderFieldWithoutIndexingToAllocatedArray(http2StaticTableIndex.GetValueOrDefault()) :
                HPackEncoder.EncodeLiteralHeaderFieldWithoutIndexingNewNameToAllocatedArray(name);

            var asciiBytesWithColonSpace = new byte[name.Length + 2]; // + 2 for ':' and ' '
            int asciiBytes = Encoding.ASCII.GetBytes(name, asciiBytesWithColonSpace);
            Debug.Assert(asciiBytes == name.Length);
            asciiBytesWithColonSpace[asciiBytesWithColonSpace.Length - 2] = (byte)':';
            asciiBytesWithColonSpace[asciiBytesWithColonSpace.Length - 1] = (byte)' ';
            AsciiBytesWithColonSpace = asciiBytesWithColonSpace;
        }

        public string Name { get; }
        public HttpHeaderParser Parser { get; }
        public HttpHeaderType HeaderType { get; }
        public string[] KnownValues { get; }
        public byte[] AsciiBytesWithColonSpace { get; }
        public HeaderDescriptor Descriptor => new HeaderDescriptor(this);
        public byte[] Http2EncodedName { get; }
    }
}
