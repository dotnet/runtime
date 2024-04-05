// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.SymbolStore;

namespace System.Reflection.Emit
{
    internal sealed class SymbolDocumentWriter : ISymbolDocumentWriter
    {
        internal readonly Guid _language;
        internal readonly string _url;
        private Guid _hashAlgorithm;
        private byte[]? _hash;
        private byte[]? _source;

        internal string URL => _url;
        internal Guid Language => _language;
        internal Guid HashAlgorithm => _hashAlgorithm;
        internal byte[]? Hash => _hash;
        internal byte[]? Source => _source;

        public SymbolDocumentWriter(string url, Guid language)
        {
            _language = language;
            _url = url;
        }

        public void SetCheckSum(Guid algorithmId, byte[] checkSum)
        {
            _hashAlgorithm = algorithmId;
            _hash = checkSum;
        }

        public void SetSource(byte[] source)
        {
            _source = source;
        }
    }
}
