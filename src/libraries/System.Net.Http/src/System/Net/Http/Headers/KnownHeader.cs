// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text;

namespace System.Net.Http.Headers
{
    internal sealed partial class KnownHeader
    {
        public KnownHeader(string name, int? http2StaticTableIndex = null, int? http3StaticTableIndex = null) :
            this(name, HttpHeaderType.Custom, parser: null, knownValues: null, http2StaticTableIndex, http3StaticTableIndex)
        {
            Debug.Assert(!string.IsNullOrEmpty(name));
            Debug.Assert(name[0] == ':' || HttpRuleParser.GetTokenLength(name, 0) == name.Length);
        }

        public KnownHeader(string name, HttpHeaderType headerType, HttpHeaderParser? parser, string[]? knownValues = null, int? http2StaticTableIndex = null, int? http3StaticTableIndex = null)
        {
            Debug.Assert(!string.IsNullOrEmpty(name));
            Debug.Assert(name[0] == ':' || HttpRuleParser.GetTokenLength(name, 0) == name.Length);

            Name = name;
            HeaderType = headerType;
            Parser = parser;
            KnownValues = knownValues;

            Initialize(http2StaticTableIndex, http3StaticTableIndex);

            var asciiBytesWithColonSpace = new byte[name.Length + 2]; // + 2 for ':' and ' '
            int asciiBytes = Encoding.ASCII.GetBytes(name, asciiBytesWithColonSpace);
            Debug.Assert(asciiBytes == name.Length);
            asciiBytesWithColonSpace[asciiBytesWithColonSpace.Length - 2] = (byte)':';
            asciiBytesWithColonSpace[asciiBytesWithColonSpace.Length - 1] = (byte)' ';
            AsciiBytesWithColonSpace = asciiBytesWithColonSpace;
        }

        partial void Initialize(int? http2StaticTableIndex, int? http3StaticTableIndex);

        public string Name { get; }
        public HttpHeaderParser? Parser { get; }
        public HttpHeaderType HeaderType { get; }

        /// <summary>
        /// If a raw string is a known value, this instance will be returned rather than allocating a new string.
        /// </summary>
        public string[]? KnownValues { get; }
        public byte[] AsciiBytesWithColonSpace { get; }
        public HeaderDescriptor Descriptor => new HeaderDescriptor(this);
    }
}
