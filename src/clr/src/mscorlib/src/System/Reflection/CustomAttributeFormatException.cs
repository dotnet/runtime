// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////
//
// CustomAttributeFormatException is thrown when the binary format of a 
// 
//    custom attribute is invalid.
//
//
namespace System.Reflection {
    using System;
    using ApplicationException = System.ApplicationException;
    using System.Runtime.Serialization;
    [Serializable]
[System.Runtime.InteropServices.ComVisible(true)]
    public class CustomAttributeFormatException  : FormatException {
    
        public CustomAttributeFormatException()
            : base(Environment.GetResourceString("Arg_CustomAttributeFormatException")) {
            SetErrorCode(__HResults.COR_E_CUSTOMATTRIBUTEFORMAT);
        }
    
        public CustomAttributeFormatException(String message) : base(message) {
            SetErrorCode(__HResults.COR_E_CUSTOMATTRIBUTEFORMAT);
        }
        
        public CustomAttributeFormatException(String message, Exception inner) : base(message, inner) {
            SetErrorCode(__HResults.COR_E_CUSTOMATTRIBUTEFORMAT);
        }

        protected CustomAttributeFormatException(SerializationInfo info, StreamingContext context) : base(info, context) {
        }

    }
}
