// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
