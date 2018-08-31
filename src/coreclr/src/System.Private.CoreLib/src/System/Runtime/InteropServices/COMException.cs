// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Globalization;

namespace System.Runtime.InteropServices
{
    // Exception for COM Interop errors where we don't recognize the HResult.
    /// <summary>
    /// Exception class for all errors from COM Interop where we don't
    /// recognize the HResult.
    /// </summary>
    [Serializable]
    [TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class COMException : ExternalException
    {
        public COMException()
            : base(SR.Arg_COMException)
        {
            HResult = HResults.E_FAIL;
        }

        public COMException(string message)
            : base(message)
        {
            HResult = HResults.E_FAIL;
        }

        public COMException(string message, Exception inner)
            : base(message, inner)
        {
            HResult = HResults.E_FAIL;
        }

        public COMException(string message, int errorCode)
            : base(message)
        {
            HResult = errorCode;
        }

        protected COMException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public override string ToString()
        {
            string message = Message;
            string s;
            string _className = GetType().ToString();
            s = _className + " (0x" + HResult.ToString("X8", CultureInfo.InvariantCulture) + ")";

            if (!(message == null || message.Length <= 0))
            {
                s = s + ": " + message;
            }

            Exception _innerException = InnerException;

            if (_innerException != null)
            {
                s = s + " ---> " + _innerException.ToString();
            }


            if (StackTrace != null)
                s += Environment.NewLine + StackTrace;

            return s;
        }
    }
}
