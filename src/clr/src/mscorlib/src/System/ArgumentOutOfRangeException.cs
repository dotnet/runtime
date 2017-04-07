// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================================
**
**
**
** Purpose: Exception class for method arguments outside of the legal range.
**
**
=============================================================================*/


using System;
using System.Runtime.Remoting;
using System.Runtime.Serialization;
using System.Globalization;
using System.Diagnostics.Contracts;

namespace System
{
    // The ArgumentOutOfRangeException is thrown when an argument 
    // is outside the legal range for that argument.  
    [Serializable]
    public class ArgumentOutOfRangeException : ArgumentException, ISerializable
    {
        private static volatile String _rangeMessage;
        private Object m_actualValue;

        private static String RangeMessage
        {
            get
            {
                if (_rangeMessage == null)
                    _rangeMessage = SR.Arg_ArgumentOutOfRangeException;
                return _rangeMessage;
            }
        }

        // Creates a new ArgumentOutOfRangeException with its message 
        // string set to a default message explaining an argument was out of range.
        public ArgumentOutOfRangeException()
            : base(RangeMessage)
        {
            HResult = __HResults.COR_E_ARGUMENTOUTOFRANGE;
        }

        public ArgumentOutOfRangeException(String paramName)
            : base(RangeMessage, paramName)
        {
            HResult = __HResults.COR_E_ARGUMENTOUTOFRANGE;
        }

        public ArgumentOutOfRangeException(String paramName, String message)
            : base(message, paramName)
        {
            HResult = __HResults.COR_E_ARGUMENTOUTOFRANGE;
        }

        public ArgumentOutOfRangeException(String message, Exception innerException)
            : base(message, innerException)
        {
            HResult = __HResults.COR_E_ARGUMENTOUTOFRANGE;
        }

        // We will not use this in the classlibs, but we'll provide it for
        // anyone that's really interested so they don't have to stick a bunch
        // of printf's in their code.
        public ArgumentOutOfRangeException(String paramName, Object actualValue, String message)
            : base(message, paramName)
        {
            m_actualValue = actualValue;
            HResult = __HResults.COR_E_ARGUMENTOUTOFRANGE;
        }

        public override String Message
        {
            get
            {
                String s = base.Message;
                if (m_actualValue != null)
                {
                    String valueMessage = SR.Format(SR.ArgumentOutOfRange_ActualValue, m_actualValue.ToString());
                    if (s == null)
                        return valueMessage;
                    return s + Environment.NewLine + valueMessage;
                }
                return s;
            }
        }

        // Gets the value of the argument that caused the exception.
        // Note - we don't set this anywhere in the class libraries in 
        // version 1, but it might come in handy for other developers who
        // want to avoid sticking printf's in their code.
        public virtual Object ActualValue
        {
            get { return m_actualValue; }
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException(nameof(info));
            }
            Contract.EndContractBlock();
            base.GetObjectData(info, context);
            info.AddValue("ActualValue", m_actualValue, typeof(Object));
        }

        protected ArgumentOutOfRangeException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            m_actualValue = info.GetValue("ActualValue", typeof(Object));
        }
    }
}
