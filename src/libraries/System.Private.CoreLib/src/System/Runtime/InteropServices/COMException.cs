// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Runtime.Serialization;
using System.Globalization;
using System.Text;

namespace System.Runtime.InteropServices
{
    // Exception for COM Interop errors where we don't recognize the HResult.
    /// <summary>
    /// Exception class for all errors from COM Interop where we don't
    /// recognize the HResult.
    /// </summary>
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class COMException : ExternalException
    {
        public COMException()
            : base(SR.Arg_COMException)
        {
            HResult = HResults.E_FAIL;
        }

        public COMException(string? message)
            : base(message)
        {
            HResult = HResults.E_FAIL;
        }

        public COMException(string? message, Exception? inner)
            : base(message, inner)
        {
            HResult = HResults.E_FAIL;
        }

        public COMException(string? message, int errorCode)
            : base(message)
        {
            HResult = errorCode;
        }

        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        protected COMException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public override string ToString()
        {
            StringBuilder s = new StringBuilder();

            s.Append($"{GetType()} (0x{HResult:X8})");

            string message = Message;
            if (!string.IsNullOrEmpty(message))
            {
                s.Append(": ").Append(message);
            }

            Exception? innerException = InnerException;
            if (innerException != null)
            {
                s.Append(Environment.NewLineConst + InnerExceptionPrefix).Append(innerException.ToString());
            }

            string? stackTrace = StackTrace;
            if (stackTrace != null)
                s.AppendLine().Append(stackTrace);

            return s.ToString();
        }
    }
}
