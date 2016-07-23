// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================================
**
**
**
** Purpose: The exception class for class loading failures.
**
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
    public class MissingMethodException : MissingMemberException, ISerializable {
        public MissingMethodException() 
            : base(Environment.GetResourceString("Arg_MissingMethodException")) {
            SetErrorCode(__HResults.COR_E_MISSINGMETHOD);
        }
    
        public MissingMethodException(String message) 
            : base(message) {
            SetErrorCode(__HResults.COR_E_MISSINGMETHOD);
        }
    
        public MissingMethodException(String message, Exception inner) 
            : base(message, inner) {
            SetErrorCode(__HResults.COR_E_MISSINGMETHOD);
        }

        protected MissingMethodException(SerializationInfo info, StreamingContext context) : base(info, context) {
        }
    
        public override String Message
        {
            [System.Security.SecuritySafeCritical]  // auto-generated
            get {
                if (ClassName == null) {
                    return base.Message;
                } else {
                    // do any desired fixups to classname here.
                    return Environment.GetResourceString("MissingMethod_Name",
                                                                       ClassName + "." + MemberName +
                                                                       (Signature != null ? " " + FormatSignature(Signature) : ""));
                }
            }
        }
    
        // Called from the EE
        private MissingMethodException(String className, String methodName, byte[] signature)
        {
            ClassName   = className;
            MemberName  = methodName;
            Signature   = signature;
        }
    
        public MissingMethodException(String className, String methodName)
        {
            ClassName   = className;
            MemberName  = methodName;
        }
    
        // If ClassName != null, Message will construct on the fly using it
        // and the other variables. This allows customization of the
        // format depending on the language environment.
    }
}
