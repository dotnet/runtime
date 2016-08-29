// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================================
**
**
** Purpose: The exception class for class loading failures.
**
=============================================================================*/

namespace System {
    
    using System;
    using System.Runtime.Remoting;
    using System.Runtime.Serialization;
    using System.Runtime.CompilerServices;
    using System.Globalization;
    [System.Runtime.InteropServices.ComVisible(true)]
    [Serializable]
    public class MissingFieldException : MissingMemberException, ISerializable {
        public MissingFieldException() 
            : base(Environment.GetResourceString("Arg_MissingFieldException")) {
            SetErrorCode(__HResults.COR_E_MISSINGFIELD);
        }
    
        public MissingFieldException(String message) 
            : base(message) {
            SetErrorCode(__HResults.COR_E_MISSINGFIELD);
        }
    
        public MissingFieldException(String message, Exception inner) 
            : base(message, inner) {
            SetErrorCode(__HResults.COR_E_MISSINGFIELD);
        }

        protected MissingFieldException(SerializationInfo info, StreamingContext context) : base(info, context) {
        }
    
        public override String Message
        {
            [System.Security.SecuritySafeCritical]  // auto-generated
            get {
                if (ClassName == null) {
                    return base.Message;
                } else {
                    // do any desired fixups to classname here.
                    return Environment.GetResourceString("MissingField_Name",
                                                                       (Signature != null ? FormatSignature(Signature) + " " : "") +
                                                                       ClassName + "." + MemberName);
                }
            }
        }
    
        // Called from the EE
        private MissingFieldException(String className, String fieldName, byte[] signature)
        {
            ClassName   = className;
            MemberName  = fieldName;
            Signature   = signature;
        }
    
        public MissingFieldException(String className, String fieldName)
        {
            ClassName   = className;
            MemberName  = fieldName;
        }
    
        // If ClassName != null, Message will construct on the fly using it
        // and the other variables. This allows customization of the
        // format depending on the language environment.
    }
}
