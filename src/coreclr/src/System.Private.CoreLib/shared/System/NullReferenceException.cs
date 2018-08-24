// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================================
**
**
**
** Purpose: Exception class for dereferencing a null reference.
**
**
=============================================================================*/

using System.Runtime.Serialization;

namespace System
{
    [Serializable]
    public class NullReferenceException : SystemException
    {
        public NullReferenceException()
            : base(SR.Arg_NullReferenceException)
        {
            HResult = HResults.COR_E_NULLREFERENCE;
        }

        public NullReferenceException(string message)
            : base(message)
        {
            HResult = HResults.COR_E_NULLREFERENCE;
        }

        public NullReferenceException(string message, Exception innerException)
            : base(message, innerException)
        {
            HResult = HResults.COR_E_NULLREFERENCE;
        }

        protected NullReferenceException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
