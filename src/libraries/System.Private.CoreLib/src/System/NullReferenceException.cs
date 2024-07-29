// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System
{
    /// <summary>
    /// The exception that is thrown when there is an attempt to dereference a <see langword="null"/> object reference.
    /// </summary>
    [Serializable]
    [TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class NullReferenceException : SystemException
    {
        public NullReferenceException()
            : base(SR.Arg_NullReferenceException)
        {
            HResult = HResults.E_POINTER;
        }

        public NullReferenceException(string? message)
            : base(message ?? SR.Arg_NullReferenceException)
        {
            HResult = HResults.E_POINTER;
        }

        public NullReferenceException(string? message, Exception? innerException)
            : base(message ?? SR.Arg_NullReferenceException, innerException)
        {
            HResult = HResults.E_POINTER;
        }

        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        protected NullReferenceException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
