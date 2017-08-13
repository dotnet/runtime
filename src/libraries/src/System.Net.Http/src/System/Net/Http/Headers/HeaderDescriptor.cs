// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Net.Http.Headers
{
    // This struct represents a particular named header --
    // if the header is one of our known headers, then it contains a reference to the KnownHeader object;
    // otherwise, for custom headers, it just contains a string for the header name.
    // Use HeaderDescriptor.Get to resolve an arbitrary header name to a HeaderDescriptor.
    internal struct HeaderDescriptor : IEquatable<HeaderDescriptor>
    {
        private readonly string _headerName;
        private readonly KnownHeader _knownHeader;

        public HeaderDescriptor(KnownHeader knownHeader)
        {
            _knownHeader = knownHeader;
            _headerName = knownHeader.Name;
        }

        // This should not be used directly; use static Get below
        private HeaderDescriptor(string headerName)
        {
            _headerName = headerName;
            _knownHeader = null;
        }

        public string Name => _headerName;
        public HttpHeaderParser Parser => _knownHeader?.Parser;
        public HttpHeaderType HeaderType => _knownHeader == null ? HttpHeaderType.Custom : _knownHeader.HeaderType;

        public bool Equals(HeaderDescriptor other) =>
            _knownHeader == null ?
                string.Equals(_headerName, other._headerName, StringComparison.OrdinalIgnoreCase) :
                _knownHeader == other._knownHeader;
        public override int GetHashCode() => _knownHeader?.GetHashCode() ?? StringComparer.OrdinalIgnoreCase.GetHashCode(_headerName);
        public override bool Equals(object obj) => throw new InvalidOperationException();   // Ensure this is never called, to avoid boxing
        public static bool operator ==(HeaderDescriptor left, HeaderDescriptor right) => left.Equals(right);
        public static bool operator !=(HeaderDescriptor left, HeaderDescriptor right) => !left.Equals(right);

        public static bool TryGet(string headerName, out HeaderDescriptor descriptor)
        {
            Debug.Assert(!string.IsNullOrEmpty(headerName));

            KnownHeader knownHeader = KnownHeaders.TryGetKnownHeader(headerName);
            if (knownHeader != null)
            {
                descriptor = new HeaderDescriptor(knownHeader);
                return true;
            }

            if (HttpRuleParser.GetTokenLength(headerName, 0) != headerName.Length)
            {
                descriptor = default(HeaderDescriptor);
                return false;
            }

            descriptor = new HeaderDescriptor(headerName);
            return true;
        }

        public HeaderDescriptor AsCustomHeader()
        {
            Debug.Assert(_knownHeader != null);
            Debug.Assert(_knownHeader.HeaderType != HttpHeaderType.Custom);
            return new HeaderDescriptor(_knownHeader.Name);
        }
    }
}
