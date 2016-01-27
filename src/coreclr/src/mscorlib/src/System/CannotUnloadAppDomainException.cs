// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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







