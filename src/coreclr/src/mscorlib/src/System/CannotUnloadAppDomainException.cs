// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/*=============================================================================
**
** 
**
** Purpose: Exception class for failed attempt to unload an AppDomain.
**
**
=============================================================================*/

namespace System {

    using System.Runtime.Serialization;

[System.Runtime.InteropServices.ComVisible(true)]
    [Serializable]
    public class CannotUnloadAppDomainException : SystemException {
        public CannotUnloadAppDomainException() 
            : base(Environment.GetResourceString("Arg_CannotUnloadAppDomainException")) {
            SetErrorCode(__HResults.COR_E_CANNOTUNLOADAPPDOMAIN);
        }
    
        public CannotUnloadAppDomainException(String message) 
            : base(message) {
            SetErrorCode(__HResults.COR_E_CANNOTUNLOADAPPDOMAIN);
        }
    
        public CannotUnloadAppDomainException(String message, Exception innerException) 
            : base(message, innerException) {
            SetErrorCode(__HResults.COR_E_CANNOTUNLOADAPPDOMAIN);
        }

        //
        //This constructor is required for serialization.
        //
        protected CannotUnloadAppDomainException(SerializationInfo info, StreamingContext context) : base(info, context) {
        }
    }
}







