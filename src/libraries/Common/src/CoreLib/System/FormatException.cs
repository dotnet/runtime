// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
**
**
** Purpose: Exception to designate an illegal argument to FormatMessage.
**
** 
===========================================================*/

#nullable enable
using System.Runtime.Serialization;

namespace System
{
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class FormatException : SystemException
    {
        public FormatException()
            : base(SR.Arg_FormatException)
        {
            HResult = HResults.COR_E_FORMAT;
        }

        public FormatException(string? message)
            : base(message)
        {
            HResult = HResults.COR_E_FORMAT;
        }

        public FormatException(string? message, Exception? innerException)
            : base(message, innerException)
        {
            HResult = HResults.COR_E_FORMAT;
        }

        protected FormatException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
