// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography.Xml
{
    public sealed class DataReference : EncryptedReference
    {
        public DataReference() : base()
        {
            ReferenceType = "DataReference";
        }

        public DataReference(string uri) : base(uri)
        {
            ReferenceType = "DataReference";
        }

        public DataReference(string uri, TransformChain transformChain) : base(uri, transformChain)
        {
            ReferenceType = "DataReference";
        }
    }
}
