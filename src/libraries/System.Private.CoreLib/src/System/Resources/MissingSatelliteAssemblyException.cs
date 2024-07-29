// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System.Resources
{
    /// <summary>
    /// The exception that is thrown when the satellite assembly for the resources of the default culture is missing.
    /// </summary>
    [Serializable]
    [TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class MissingSatelliteAssemblyException : SystemException
    {
        private readonly string? _cultureName;

        public MissingSatelliteAssemblyException()
            : base(SR.MissingSatelliteAssembly_Default)
        {
            HResult = HResults.COR_E_MISSINGSATELLITEASSEMBLY;
        }

        public MissingSatelliteAssemblyException(string? message)
            : base(message ?? SR.MissingSatelliteAssembly_Default)
        {
            HResult = HResults.COR_E_MISSINGSATELLITEASSEMBLY;
        }

        public MissingSatelliteAssemblyException(string? message, string? cultureName)
            : base(message ?? SR.MissingSatelliteAssembly_Default)
        {
            HResult = HResults.COR_E_MISSINGSATELLITEASSEMBLY;
            _cultureName = cultureName;
        }

        public MissingSatelliteAssemblyException(string? message, Exception? inner)
            : base(message ?? SR.MissingSatelliteAssembly_Default, inner)
        {
            HResult = HResults.COR_E_MISSINGSATELLITEASSEMBLY;
        }

        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        protected MissingSatelliteAssemblyException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public string? CultureName => _cultureName;
    }
}
