// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Xml;

namespace System.Runtime.Serialization.Xml.Canonicalization.Tests
{
    internal interface IAncestralNamespaceContextProvider
    {
        string CurrentNodeName { get; }

        string LookupNamespace(string prefix);
    }

    public static class AncestralNamespaceContextProviderProxy
    {
        public static object CreateContextProvider(XmlReader reader)
        {
            return new ReaderAncestralNamespaceContextProvider(reader);
        }

        private sealed class ReaderAncestralNamespaceContextProvider : IAncestralNamespaceContextProvider
        {
            private XmlReader _reader;

            public ReaderAncestralNamespaceContextProvider(XmlReader reader)
            {
                _reader = reader;
            }

            public string CurrentNodeName
            {
                get { return _reader.Name; }
            }

            public string LookupNamespace(string prefix)
            {
                return _reader.LookupNamespace(prefix);
            }
        }
    }
}
