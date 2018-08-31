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
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class MissingMethodException : MissingMemberException
    {
        public MissingMethodException()
            : base(SR.Arg_MissingMethodException)
        {
            HResult = HResults.COR_E_MISSINGMETHOD;
        }

        public MissingMethodException(string message)
            : base(message)
        {
            HResult = HResults.COR_E_MISSINGMETHOD;
        }

        public MissingMethodException(string message, Exception inner)
            : base(message, inner)
        {
            HResult = HResults.COR_E_MISSINGMETHOD;
        }

        public MissingMethodException(string className, string methodName)
        {
            ClassName = className;
            MemberName = methodName;
        }

        protected MissingMethodException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
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
