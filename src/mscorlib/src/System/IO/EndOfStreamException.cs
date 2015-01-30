// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/*============================================================
**
** 
** 
**
**
** Purpose: Exception to be thrown when reading past end-of-file.
**
**
===========================================================*/

using System;
using System.Runtime.Serialization;

namespace System.IO {
    [Serializable]
[System.Runtime.InteropServices.ComVisible(true)]
    public class EndOfStreamException : IOException
    {
        public EndOfStreamException() 
            : base(Environment.GetResourceString("Arg_EndOfStreamException")) {
            SetErrorCode(__HResults.COR_E_ENDOFSTREAM);
        }
        
        public EndOfStreamException(String message) 
            : base(message) {
            SetErrorCode(__HResults.COR_E_ENDOFSTREAM);
        }
        
        public EndOfStreamException(String message, Exception innerException) 
            : base(message, innerException) {
            SetErrorCode(__HResults.COR_E_ENDOFSTREAM);
        }

        protected EndOfStreamException(SerializationInfo info, StreamingContext context) : base (info, context) {
        }
    }

}
