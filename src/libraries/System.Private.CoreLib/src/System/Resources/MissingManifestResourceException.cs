// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System.Resources
{
    [Serializable]
    [TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class MissingManifestResourceException : SystemException
    {
        public MissingManifestResourceException()
            : base(SR.Arg_MissingManifestResourceException)
        {
            HResult = HResults.COR_E_MISSINGMANIFESTRESOURCE;
        }

        public MissingManifestResourceException(string? message)
            : base(message ?? SR.Arg_MissingManifestResourceException)
        {
            HResult = HResults.COR_E_MISSINGMANIFESTRESOURCE;
        }

        public MissingManifestResourceException(string? message, Exception? inner)
            : base(message ?? SR.Arg_MissingManifestResourceException, inner)
        {
            HResult = HResults.COR_E_MISSINGMANIFESTRESOURCE;
        }

        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        protected MissingManifestResourceException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
