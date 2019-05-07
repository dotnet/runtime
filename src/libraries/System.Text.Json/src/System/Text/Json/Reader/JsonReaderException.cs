// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;

namespace System.Text.Json
{
    // This class exists because the serializer needs to catch reader-originated exceptions in order to throw JsonException which has Path information.
    [Serializable]
    internal sealed class JsonReaderException : JsonException
    {
        public JsonReaderException(string message, long lineNumber, long bytePositionInLine) : base(message, path : null, lineNumber, bytePositionInLine)
        {
        }

        private JsonReaderException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
