// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Runtime.Serialization;

namespace System
{
    /// <summary>
    /// The exception class for OOM.
    /// </summary>
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class OutOfMemoryException : SystemException
    {
        public OutOfMemoryException() : base(
#if CORECLR
            GetMessageFromNativeResources(ExceptionMessageKind.OutOfMemory)
#else
            SR.Arg_OutOfMemoryException
#endif
            )
        {
            HResult = HResults.COR_E_OUTOFMEMORY;
        }

        public OutOfMemoryException(string? message)
            : base(message)
        {
            HResult = HResults.COR_E_OUTOFMEMORY;
        }

        public OutOfMemoryException(string? message, Exception? innerException)
            : base(message, innerException)
        {
            HResult = HResults.COR_E_OUTOFMEMORY;
        }

        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        protected OutOfMemoryException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
