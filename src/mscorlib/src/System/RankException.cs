// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/*=============================================================================
**
**
**
** Purpose: For methods that are passed arrays with the wrong number of
**          dimensions.
**
**
=============================================================================*/

namespace System {
    
    using System;
    using System.Runtime.Serialization;
[System.Runtime.InteropServices.ComVisible(true)]
    [Serializable]
    public class RankException : SystemException
    {
        public RankException() 
            : base(Environment.GetResourceString("Arg_RankException")) {
            SetErrorCode(__HResults.COR_E_RANK);
        }
    
        public RankException(String message) 
            : base(message) {
            SetErrorCode(__HResults.COR_E_RANK);
        }
        
        public RankException(String message, Exception innerException) 
            : base(message, innerException) {
            SetErrorCode(__HResults.COR_E_RANK);
        }

        protected RankException(SerializationInfo info, StreamingContext context) : base(info, context) {
        }

    }
}
