// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
