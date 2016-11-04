// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if netstandard10
using System.Runtime.Serialization;
#endif //netstandard10

namespace System
{
    /// <summary>
    /// An exception class used when an invalid Uniform Resource Identifier is detected.
    /// </summary>
#if netstandard10
    [Serializable]
#endif //netstandard10
    public class UriFormatException : FormatException
#if netstandard10
    , ISerializable
#endif //netstandard10
    {
        public UriFormatException() : base()
        {
        }

        public UriFormatException(string textString) : base(textString)
        {
        }

        public UriFormatException(string textString, Exception e) : base(textString, e)
        {
        }
#if netstandard10
        protected UriFormatException(SerializationInfo serializationInfo, StreamingContext streamingContext) : base(serializationInfo, streamingContext)
        {
        }

        void ISerializable.GetObjectData(SerializationInfo serializationInfo, StreamingContext streamingContext) {
            base.GetObjectData(serializationInfo, streamingContext);
        }
#endif //netstandard10
    }; // class UriFormatException
} // namespace System
