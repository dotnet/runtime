// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================================
**
**
**
** Purpose: For methods that should be implemented on subclasses.
**
**
=============================================================================*/

using System.Runtime.Serialization;

namespace System
{
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class NotSupportedException : SystemException
    {
        public NotSupportedException()
            : base(SR.Arg_NotSupportedException)
        {
            HResult = HResults.COR_E_NOTSUPPORTED;
        }

        public NotSupportedException(string? message)
            : base(message)
        {
            HResult = HResults.COR_E_NOTSUPPORTED;
        }

        public NotSupportedException(string? message, Exception? innerException)
            : base(message, innerException)
        {
            HResult = HResults.COR_E_NOTSUPPORTED;
        }

        protected NotSupportedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
