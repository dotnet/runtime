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

using System.Globalization;
using System.Runtime.Serialization;

namespace System
{
    // The ArgumentException is thrown when an argument does not meet 
    // the contract of the method.  Ideally it should give a meaningful error
    // message describing what was wrong and which parameter is incorrect.
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class ArgumentException : SystemException
    {
        private string? _paramName;

        // Creates a new ArgumentException with its message 
        // string set to the empty string. 
        public ArgumentException()
            : base(SR.Arg_ArgumentException)
        {
            HResult = HResults.COR_E_ARGUMENT;
        }

        // Creates a new ArgumentException with its message 
        // string set to message. 
        // 
        public ArgumentException(string? message)
            : base(message)
        {
            HResult = HResults.COR_E_ARGUMENT;
        }

        public ArgumentException(string? message, Exception? innerException)
            : base(message, innerException)
        {
            HResult = HResults.COR_E_ARGUMENT;
        }

        public ArgumentException(string? message, string? paramName, Exception? innerException)
            : base(message, innerException)
        {
            _paramName = paramName;
            HResult = HResults.COR_E_ARGUMENT;
        }

        public ArgumentException(string? message, string? paramName)
            : base(message)
        {
            _paramName = paramName;
            HResult = HResults.COR_E_ARGUMENT;
        }

        protected ArgumentException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            _paramName = info.GetString("ParamName");
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("ParamName", _paramName, typeof(string));
        }

        public override string Message
        {
            get
            {
                SetMessageField();
                
                string s = base.Message;
                if (!string.IsNullOrEmpty(_paramName))
                {
                    s += " " + SR.Format(SR.Arg_ParamName_Name, _paramName);
                }

                return s;
            }
        }

        private void SetMessageField()
        {
            if (_message == null && HResult == System.HResults.COR_E_ARGUMENT) 
            {
                _message = SR.Arg_ArgumentException;
            }
        }
        
        public virtual string? ParamName
        {
            get { return _paramName; }
        }
    }
}
