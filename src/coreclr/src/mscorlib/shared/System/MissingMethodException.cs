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

using System.Runtime.Serialization;

namespace System
{
    public class MissingMethodException : MissingMemberException
    {
        public MissingMethodException()
            : base(SR.Arg_MissingMethodException)
        {
            HResult = __HResults.COR_E_MISSINGMETHOD;
        }

        public MissingMethodException(string message)
            : base(message)
        {
            HResult = __HResults.COR_E_MISSINGMETHOD;
        }

        public MissingMethodException(string message, Exception inner)
            : base(message, inner)
        {
            HResult = __HResults.COR_E_MISSINGMETHOD;
        }

        public MissingMethodException(string className, string methodName)
        {
            ClassName = className;
            MemberName = methodName;
        }

        protected MissingMethodException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            throw new PlatformNotSupportedException();
        }

        public override string Message
        {
            get
            {
                return ClassName == null ? base.Message : 
                    SR.Format(SR.MissingMethod_Name, ClassName + "." + MemberName + 
                        (Signature != null ? " " + FormatSignature(Signature) : string.Empty));
            }
        }
    }
}
