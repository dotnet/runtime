// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System
{
    /// <summary>
    /// Serves as the base class for application-defined exceptions.
    /// </summary>
    /// <remarks>
    /// You should derive custom exceptions from the <see cref="Exception" /> class rather than the <see cref="ApplicationException" /> class.
    /// You should not throw an <see cref="ApplicationException" /> in your code, and you should not catch an <see cref="ApplicationException" />
    /// unless you intend to re-throw the original exception.
    /// </remarks>
    [Serializable]
    [TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class ApplicationException : Exception
    {
        // Creates a new ApplicationException with its message string set to
        // the empty string, its HRESULT set to COR_E_APPLICATION,
        // and its ExceptionInfo reference set to null.
        public ApplicationException()
            : base(SR.Arg_ApplicationException)
        {
            HResult = HResults.COR_E_APPLICATION;
        }

        // Creates a new ApplicationException with its message string set to
        // message, its HRESULT set to COR_E_APPLICATION,
        // and its ExceptionInfo reference set to null.
        //
        public ApplicationException(string? message)
            : base(message ?? SR.Arg_ApplicationException)
        {
            HResult = HResults.COR_E_APPLICATION;
        }

        public ApplicationException(string? message, Exception? innerException)
            : base(message ?? SR.Arg_ApplicationException, innerException)
        {
            HResult = HResults.COR_E_APPLICATION;
        }

        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        protected ApplicationException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
