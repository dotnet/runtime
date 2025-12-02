// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System
{
    /// <summary>
    /// The exception that is thrown when a DLL specified in a DLL import cannot be found.
    /// </summary>
    [Serializable]
    [TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class DllNotFoundException : TypeLoadException
    {
        public DllNotFoundException()
            : base(SR.Arg_DllNotFoundException)
        {
            HResult = HResults.COR_E_DLLNOTFOUND;
        }

        public DllNotFoundException(string? message)
            : base(message ?? SR.Arg_DllNotFoundException)
        {
            HResult = HResults.COR_E_DLLNOTFOUND;
        }

        public DllNotFoundException(string? message, Exception? inner)
            : base(message ?? SR.Arg_DllNotFoundException, inner)
        {
            HResult = HResults.COR_E_DLLNOTFOUND;
        }

        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        protected DllNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
