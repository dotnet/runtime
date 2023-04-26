// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
**
**
**
**
** Purpose: Exception for a missing satellite assembly needed
**          for ultimate resource fallback.  This usually
**          indicates a setup and/or deployment problem.
**
**
===========================================================*/

using System.ComponentModel;
using System.Runtime.Serialization;

namespace System.Resources
{
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class MissingSatelliteAssemblyException : SystemException
    {
        private readonly string? _cultureName;

        public MissingSatelliteAssemblyException()
            : base(SR.MissingSatelliteAssembly_Default)
        {
            HResult = System.HResults.COR_E_MISSINGSATELLITEASSEMBLY;
        }

        public MissingSatelliteAssemblyException(string? message)
            : base(message)
        {
            HResult = System.HResults.COR_E_MISSINGSATELLITEASSEMBLY;
        }

        public MissingSatelliteAssemblyException(string? message, string? cultureName)
            : base(message)
        {
            HResult = System.HResults.COR_E_MISSINGSATELLITEASSEMBLY;
            _cultureName = cultureName;
        }

        public MissingSatelliteAssemblyException(string? message, Exception? inner)
            : base(message, inner)
        {
            HResult = System.HResults.COR_E_MISSINGSATELLITEASSEMBLY;
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
