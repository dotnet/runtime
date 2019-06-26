// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0.
// See THIRD-PARTY-NOTICES.TXT in the project root for license information.

using System.Runtime.Serialization;

namespace System.Net.Http.HPack
{
    // TODO: Should this be public?
    [Serializable]
    internal class HuffmanDecodingException : Exception
    {
        public HuffmanDecodingException()
        {
        }

        public HuffmanDecodingException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
