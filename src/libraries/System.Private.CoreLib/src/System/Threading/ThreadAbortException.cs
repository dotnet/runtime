// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System.Threading
{
    /// <summary>
    /// The exception that is thrown when a call is made to the <see cref="Thread.Abort" /> method.
    /// </summary>
    [Serializable]
    [TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public sealed class ThreadAbortException : SystemException
    {
        internal ThreadAbortException()
        {
            HResult = HResults.COR_E_THREADABORTED;
        }

        public object? ExceptionState => null;

        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        private ThreadAbortException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
