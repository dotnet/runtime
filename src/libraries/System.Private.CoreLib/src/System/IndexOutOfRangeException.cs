// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System
{
    /// <summary>
    /// The exception that is thrown when an attempt is made to access an element of an array or collection with an index that is outside its bounds.
    /// </summary>
    [Serializable]
    [TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public sealed class IndexOutOfRangeException : SystemException
    {
        public IndexOutOfRangeException()
            : base(SR.Arg_IndexOutOfRangeException)
        {
            HResult = HResults.COR_E_INDEXOUTOFRANGE;
        }

        public IndexOutOfRangeException(string? message)
            : base(message ?? SR.Arg_IndexOutOfRangeException)
        {
            HResult = HResults.COR_E_INDEXOUTOFRANGE;
        }

        public IndexOutOfRangeException(string? message, Exception? innerException)
            : base(message ?? SR.Arg_IndexOutOfRangeException, innerException)
        {
            HResult = HResults.COR_E_INDEXOUTOFRANGE;
        }

        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        private IndexOutOfRangeException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
