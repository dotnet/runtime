// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Net.Http.Headers
{
    internal sealed class KnownHeader
    {
        private string _name;
        private HttpHeaderType _headerType;
        private HttpHeaderParser _parser;

        public KnownHeader(string name, HttpHeaderType headerType, HttpHeaderParser parser)
        {
            Debug.Assert(!string.IsNullOrEmpty(name));
            Debug.Assert(HttpRuleParser.GetTokenLength(name, 0) == name.Length);
            Debug.Assert(headerType != HttpHeaderType.Custom);
            Debug.Assert(parser != null);

            _name = name;
            _headerType = headerType;
            _parser = parser;
        }

        public KnownHeader(string name)
        {
            Debug.Assert(!string.IsNullOrEmpty(name));
            Debug.Assert(HttpRuleParser.GetTokenLength(name, 0) == name.Length);

            _name = name;
            _headerType = HttpHeaderType.Custom;
            _parser = null;
        }

        public string Name => _name;
        public HttpHeaderParser Parser => _parser;
        public HttpHeaderType HeaderType => _headerType;
        public HeaderDescriptor Descriptor => new HeaderDescriptor(this);
    }
}
