// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System.Reflection
{
    [Serializable]
    [TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class CustomAttributeFormatException : FormatException
    {
        public CustomAttributeFormatException()
            : this(SR.Arg_CustomAttributeFormatException)
        {
        }

        public CustomAttributeFormatException(string? message)
            : this(message, null)
        {
        }

        public CustomAttributeFormatException(string? message, Exception? inner)
            : base(message ?? SR.Arg_CustomAttributeFormatException, inner)
        {
            HResult = HResults.COR_E_CUSTOMATTRIBUTEFORMAT;
        }

        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        protected CustomAttributeFormatException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
