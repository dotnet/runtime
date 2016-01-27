// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

using System;
using System.Runtime.Serialization;

namespace System.Resources {
    [Serializable]
[System.Runtime.InteropServices.ComVisible(true)]
    public class MissingSatelliteAssemblyException : SystemException
    {
        private String _cultureName;

        public MissingSatelliteAssemblyException() 
            : base(Environment.GetResourceString("MissingSatelliteAssembly_Default")) {
            SetErrorCode(System.__HResults.COR_E_MISSINGSATELLITEASSEMBLY);
        }
        
        public MissingSatelliteAssemblyException(String message) 
            : base(message) {
            SetErrorCode(System.__HResults.COR_E_MISSINGSATELLITEASSEMBLY);
        }
        
        public MissingSatelliteAssemblyException(String message, String cultureName) 
            : base(message) {
            SetErrorCode(System.__HResults.COR_E_MISSINGSATELLITEASSEMBLY);
            _cultureName = cultureName;
        }

        public MissingSatelliteAssemblyException(String message, Exception inner) 
            : base(message, inner) {
            SetErrorCode(System.__HResults.COR_E_MISSINGSATELLITEASSEMBLY);
        }

#if FEATURE_SERIALIZATION
        protected MissingSatelliteAssemblyException(SerializationInfo info, StreamingContext context) : base (info, context) {
        }
#endif // FEATURE_SERIALIZATION

        public String CultureName {
            get { return _cultureName; }
        }
    }
}
