// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/*=============================================================================
**
** 
**
** Purpose: Exception class for attempt to access an unloaded AppDomain
**
**
=============================================================================*/

namespace System {

    using System.Runtime.Serialization;

[System.Runtime.InteropServices.ComVisible(true)]
    [Serializable]
    public class AppDomainUnloadedException : SystemException {
        public AppDomainUnloadedException() 
            : base(Environment.GetResourceString("Arg_AppDomainUnloadedException")) {
            SetErrorCode(__HResults.COR_E_APPDOMAINUNLOADED);
        }
    
        public AppDomainUnloadedException(String message) 
            : base(message) {
            SetErrorCode(__HResults.COR_E_APPDOMAINUNLOADED);
        }
    
        public AppDomainUnloadedException(String message, Exception innerException) 
            : base(message, innerException) {
            SetErrorCode(__HResults.COR_E_APPDOMAINUNLOADED);
        }

        //
        //This constructor is required for serialization.
        //
        protected AppDomainUnloadedException(SerializationInfo info, StreamingContext context) : base(info, context) {
        }
    }
}

