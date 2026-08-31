// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System
{
    /// <summary>
    /// The exception that is thrown when a feature is not supported on the current platform.
    /// </summary>
    [Serializable]
    [TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class PlatformNotSupportedException : NotSupportedException
    {
        public PlatformNotSupportedException()
            : base(SR.Arg_PlatformNotSupported)
        {
            HResult = HResults.COR_E_PLATFORMNOTSUPPORTED;
        }

        public PlatformNotSupportedException(string? message)
            : base(message ?? SR.Arg_PlatformNotSupported)
        {
            HResult = HResults.COR_E_PLATFORMNOTSUPPORTED;
        }

        public PlatformNotSupportedException(string? message, Exception? inner)
            : base(message ?? SR.Arg_PlatformNotSupported, inner)
        {
            HResult = HResults.COR_E_PLATFORMNOTSUPPORTED;
        }

        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        protected PlatformNotSupportedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
