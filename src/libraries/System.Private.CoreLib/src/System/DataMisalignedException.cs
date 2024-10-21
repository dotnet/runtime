// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System
{
    /// <summary>
    /// The exception that is thrown when a unit of data is read from or written to an address that is not a multiple of the data size.
    /// </summary>
    [Serializable]
    [TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public sealed class DataMisalignedException : SystemException
    {
        public DataMisalignedException()
            : base(SR.Arg_DataMisalignedException)
        {
            HResult = HResults.COR_E_DATAMISALIGNED;
        }

        public DataMisalignedException(string? message)
            : base(message ?? SR.Arg_DataMisalignedException)
        {
            HResult = HResults.COR_E_DATAMISALIGNED;
        }

        public DataMisalignedException(string? message, Exception? innerException)
            : base(message ?? SR.Arg_DataMisalignedException, innerException)
        {
            HResult = HResults.COR_E_DATAMISALIGNED;
        }

        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        private DataMisalignedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
