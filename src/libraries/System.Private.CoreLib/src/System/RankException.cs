// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System
{
    /// <summary>
    /// The exception that is thrown when an array with the wrong number of dimensions is passed to a method.
    /// </summary>
    [Serializable]
    [TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class RankException : SystemException
    {
        public RankException()
            : base(SR.Arg_RankException)
        {
            HResult = HResults.COR_E_RANK;
        }

        public RankException(string? message)
            : base(message ?? SR.Arg_RankException)
        {
            HResult = HResults.COR_E_RANK;
        }

        public RankException(string? message, Exception? innerException)
            : base(message ?? SR.Arg_RankException, innerException)
        {
            HResult = HResults.COR_E_RANK;
        }

        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        protected RankException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
