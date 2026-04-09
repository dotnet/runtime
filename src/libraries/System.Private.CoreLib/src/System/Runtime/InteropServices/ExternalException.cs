// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System.Runtime.InteropServices
{
    /// <summary>
    /// The base exception type for all COM interop exceptions and structured exception handling (SEH) exceptions.
    /// </summary>
    [Serializable]
    [TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class ExternalException : SystemException
    {
        public ExternalException()
            : base(SR.Arg_ExternalException)
        {
            HResult = HResults.E_FAIL;
        }

        public ExternalException(string? message)
            : base(message ?? SR.Arg_ExternalException)
        {
            HResult = HResults.E_FAIL;
        }

        public ExternalException(string? message, Exception? inner)
            : base(message ?? SR.Arg_ExternalException, inner)
        {
            HResult = HResults.E_FAIL;
        }

        public ExternalException(string? message, int errorCode)
            : base(message ?? SR.Arg_ExternalException)
        {
            HResult = errorCode;
        }

        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        protected ExternalException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public virtual int ErrorCode => HResult;

        public override string ToString()
        {
            string message = Message;

            string s = $"{GetType()} (0x{HResult:X8})";

            if (!string.IsNullOrEmpty(message))
            {
                s += ": " + message;
            }

            Exception? innerException = InnerException;
            if (innerException != null)
            {
                s += Environment.NewLineConst + InnerExceptionPrefix + innerException.ToString();
            }

            if (StackTrace != null)
                s += Environment.NewLineConst + StackTrace;

            return s;
        }
    }
}
