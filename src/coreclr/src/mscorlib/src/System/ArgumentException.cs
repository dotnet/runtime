// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================================
**
**
**
** Purpose: Exception class for invalid arguments to a method.
**
**
=============================================================================*/

namespace System {
    
    using System;
    using System.Runtime.Remoting;
    using System.Runtime.Serialization;
    using System.Globalization;
    using System.Security.Permissions;
    using System.Diagnostics.Contracts;
    // The ArgumentException is thrown when an argument does not meet 
    // the contract of the method.  Ideally it should give a meaningful error
    // message describing what was wrong and which parameter is incorrect.
    // 
    [System.Runtime.InteropServices.ComVisible(true)]
    [Serializable]
    public class ArgumentException : SystemException, ISerializable {
        private String m_paramName;
        
        // Creates a new ArgumentException with its message 
        // string set to the empty string. 
        public ArgumentException() 
            : base(Environment.GetResourceString("Arg_ArgumentException")) {
            SetErrorCode(__HResults.COR_E_ARGUMENT);
        }
        
        // Creates a new ArgumentException with its message 
        // string set to message. 
        // 
        public ArgumentException(String message) 
            : base(message) {
            SetErrorCode(__HResults.COR_E_ARGUMENT);
        }
        
        public ArgumentException(String message, Exception innerException) 
            : base(message, innerException) {
            SetErrorCode(__HResults.COR_E_ARGUMENT);
        }

        public ArgumentException(String message, String paramName, Exception innerException) 
            : base(message, innerException) {
            m_paramName = paramName;
            SetErrorCode(__HResults.COR_E_ARGUMENT);
        }
        
        public ArgumentException (String message, String paramName)
        
            : base (message) {
            m_paramName = paramName;
            SetErrorCode(__HResults.COR_E_ARGUMENT);
        }

        protected ArgumentException(SerializationInfo info, StreamingContext context) : base(info, context) {
            m_paramName = info.GetString("ParamName");
        }
        
        public override String Message
        {
            get {
                String s = base.Message;
                if (!String.IsNullOrEmpty(m_paramName)) {
                    String resourceString = Environment.GetResourceString("Arg_ParamName_Name", m_paramName);
                    return s + Environment.NewLine + resourceString;
                }
                else
                    return s;
            }
        }
                
        public virtual String ParamName {
            get { return m_paramName; }
        }
    
        [System.Security.SecurityCritical]  // auto-generated_required
        public override void GetObjectData(SerializationInfo info, StreamingContext context) {
            if (info==null) {
                throw new ArgumentNullException("info");
            }
            Contract.EndContractBlock();
            base.GetObjectData(info, context);
            info.AddValue("ParamName", m_paramName, typeof(String));
        }
    }
}
