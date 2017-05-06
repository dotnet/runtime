// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================================
**
**
** Purpose: The exception class for class loading failures.
**
=============================================================================*/


using System;
using System.Runtime.Serialization;
using System.Runtime.CompilerServices;
using System.Globalization;

namespace System
{
    [Serializable]
    public class MissingFieldException : MissingMemberException, ISerializable
    {
        public MissingFieldException()
            : base(SR.Arg_MissingFieldException)
        {
            HResult = __HResults.COR_E_MISSINGFIELD;
        }

        public MissingFieldException(String message)
            : base(message)
        {
            HResult = __HResults.COR_E_MISSINGFIELD;
        }

        public MissingFieldException(String message, Exception inner)
            : base(message, inner)
        {
            HResult = __HResults.COR_E_MISSINGFIELD;
        }

        protected MissingFieldException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public override String Message
        {
            get
            {
                if (ClassName == null)
                {
                    return base.Message;
                }
                else
                {
                    // do any desired fixups to classname here.
                    return SR.Format(SR.MissingField_Name, (Signature != null ? FormatSignature(Signature) + " " : "") + ClassName + "." + MemberName);
                }
            }
        }

        public MissingFieldException(String className, String fieldName)
        {
            ClassName = className;
            MemberName = fieldName;
        }

        // If ClassName != null, Message will construct on the fly using it
        // and the other variables. This allows customization of the
        // format depending on the language environment.
    }
}
