// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Runtime.Serialization;

namespace System
{
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class TypeUnloadedException : SystemException
    {
        public TypeUnloadedException()
            : base(SR.Arg_TypeUnloadedException)
        {
            HResult = HResults.COR_E_TYPEUNLOADED;
        }

        public TypeUnloadedException(string? message)
            : base(message)
        {
            HResult = HResults.COR_E_TYPEUNLOADED;
        }

        public TypeUnloadedException(string? message, Exception? innerException)
            : base(message, innerException)
        {
            HResult = HResults.COR_E_TYPEUNLOADED;
        }

        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        protected TypeUnloadedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
