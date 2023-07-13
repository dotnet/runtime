// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Runtime.Serialization;

namespace System.Configuration
{
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class SettingsPropertyIsReadOnlyException : Exception
    {
        public SettingsPropertyIsReadOnlyException(string message)
             : base(message)
        {
        }

        public SettingsPropertyIsReadOnlyException(string message, Exception innerException)
             : base(message, innerException)
        {
        }

#if NET8_0_OR_GREATER
        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
#endif
        protected SettingsPropertyIsReadOnlyException(SerializationInfo info, StreamingContext context)
             : base(info, context)
        {
        }

        public SettingsPropertyIsReadOnlyException()
        {
        }
    }
}
