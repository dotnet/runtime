// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Runtime.Serialization;

namespace System
{
    /// <summary>
    /// An exception class used when an invalid Uniform Resource Identifier is detected.
    /// </summary>
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class UriFormatException : FormatException, ISerializable
    {
        public UriFormatException() : base()
        {
        }

        public UriFormatException(string? textString) : base(textString)
        {
        }

        public UriFormatException(string? textString, Exception? e) : base(textString, e)
        {
        }

        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        protected UriFormatException(SerializationInfo serializationInfo, StreamingContext streamingContext) : base(serializationInfo, streamingContext)
        {
        }

        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        void ISerializable.GetObjectData(SerializationInfo serializationInfo, StreamingContext streamingContext)
        {
            base.GetObjectData(serializationInfo, streamingContext);
        }
    }
}
