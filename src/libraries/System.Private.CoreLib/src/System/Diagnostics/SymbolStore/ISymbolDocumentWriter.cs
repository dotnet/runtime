// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Diagnostics.SymbolStore
{
    public interface ISymbolDocumentWriter
    {
        void SetCheckSum(Guid algorithmId, byte[] checkSum);
        void SetSource(byte[] source);
    }
}
