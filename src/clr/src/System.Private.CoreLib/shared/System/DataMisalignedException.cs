// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================================
**
**
** Purpose: The exception class for a misaligned access exception
**
=============================================================================*/

#nullable enable
using System.Runtime.Serialization;

namespace System
{
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public sealed class DataMisalignedException : SystemException
    {
        public DataMisalignedException()
            : base(SR.Arg_DataMisalignedException)
        {
            HResult = HResults.COR_E_DATAMISALIGNED;
        }

        public DataMisalignedException(string? message)
            : base(message)
        {
            HResult = HResults.COR_E_DATAMISALIGNED;
        }

        public DataMisalignedException(string? message, Exception? innerException)
            : base(message, innerException)
        {
            HResult = HResults.COR_E_DATAMISALIGNED;
        }

        internal DataMisalignedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
