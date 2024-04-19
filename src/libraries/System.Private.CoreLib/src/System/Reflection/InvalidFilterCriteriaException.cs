// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System.Reflection
{
    [Serializable]
    [TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class InvalidFilterCriteriaException : ApplicationException
    {
        public InvalidFilterCriteriaException()
            : this(SR.Arg_InvalidFilterCriteriaException)
        {
        }

        public InvalidFilterCriteriaException(string? message)
            : this(message, null)
        {
        }

        public InvalidFilterCriteriaException(string? message, Exception? inner)
            : base(message ?? SR.Arg_InvalidFilterCriteriaException, inner)
        {
            HResult = HResults.COR_E_INVALIDFILTERCRITERIA;
        }

        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        protected InvalidFilterCriteriaException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
