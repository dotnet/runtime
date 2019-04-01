// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================================
**
**
** Purpose: The exception class for class loading failures.
**
=============================================================================*/

#nullable enable
using System.Runtime.Serialization;

namespace System
{
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class FieldAccessException : MemberAccessException
    {
        public FieldAccessException()
            : base(SR.Arg_FieldAccessException)
        {
            HResult = HResults.COR_E_FIELDACCESS;
        }

        public FieldAccessException(string? message)
            : base(message)
        {
            HResult = HResults.COR_E_FIELDACCESS;
        }

        public FieldAccessException(string? message, Exception? inner)
            : base(message, inner)
        {
            HResult = HResults.COR_E_FIELDACCESS;
        }

        protected FieldAccessException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
