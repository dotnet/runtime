// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================================
**
**
**
** Purpose: The exception class for stack overflow.
**
**
=============================================================================*/

using System.Runtime.Serialization;

namespace System
{
    public sealed class StackOverflowException : SystemException
    {
        public StackOverflowException()
            : base(SR.Arg_StackOverflowException)
        {
            HResult = __HResults.COR_E_STACKOVERFLOW;
        }

        public StackOverflowException(String message)
            : base(message)
        {
            HResult = __HResults.COR_E_STACKOVERFLOW;
        }

        public StackOverflowException(String message, Exception innerException)
            : base(message, innerException)
        {
            HResult = __HResults.COR_E_STACKOVERFLOW;
        }
    }
}
