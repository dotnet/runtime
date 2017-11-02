// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================================
**
**
**
** Purpose: The exception class for programs with invalid IL or bad metadata.
**
**
=============================================================================*/

using System.Runtime.Serialization;

namespace System
{
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public sealed class InvalidProgramException : SystemException
    {
        public InvalidProgramException()
            : base(SR.InvalidProgram_Default)
        {
            HResult = HResults.COR_E_INVALIDPROGRAM;
        }

        public InvalidProgramException(String message)
            : base(message)
        {
            HResult = HResults.COR_E_INVALIDPROGRAM;
        }

        public InvalidProgramException(String message, Exception inner)
            : base(message, inner)
        {
            HResult = HResults.COR_E_INVALIDPROGRAM;
        }

        internal InvalidProgramException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
