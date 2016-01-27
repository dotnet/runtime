// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
** 
** 
**
**
** Purpose: Exception for a missing assembly-level resource 
**
**
===========================================================*/

using System;
using System.Runtime.Serialization;

namespace System.Resources {
    [Serializable]
[System.Runtime.InteropServices.ComVisible(true)]
    public class MissingManifestResourceException : SystemException
    {
        public MissingManifestResourceException() 
            : base(Environment.GetResourceString("Arg_MissingManifestResourceException")) {
            SetErrorCode(System.__HResults.COR_E_MISSINGMANIFESTRESOURCE);
        }
        
        public MissingManifestResourceException(String message) 
            : base(message) {
            SetErrorCode(System.__HResults.COR_E_MISSINGMANIFESTRESOURCE);
        }
        
        public MissingManifestResourceException(String message, Exception inner) 
            : base(message, inner) {
            SetErrorCode(System.__HResults.COR_E_MISSINGMANIFESTRESOURCE);
        }

#if FEATURE_SERIALIZATION
        protected MissingManifestResourceException(SerializationInfo info, StreamingContext context) : base (info, context) {
        }
#endif // FEATURE_SERIALIZATION
    }
}
