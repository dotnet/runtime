// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================================
**
**
** Purpose: The exception class for a misaligned access exception
**
=============================================================================*/

using System.Runtime.Serialization;

namespace System
{
    public sealed class DataMisalignedException : SystemException
    {
        public DataMisalignedException()
            : base(SR.Arg_DataMisalignedException)
        {
            HResult = __HResults.COR_E_DATAMISALIGNED;
        }

        public DataMisalignedException(String message)
            : base(message)
        {
            HResult = __HResults.COR_E_DATAMISALIGNED;
        }

        public DataMisalignedException(String message, Exception innerException)
            : base(message, innerException)
        {
            HResult = __HResults.COR_E_DATAMISALIGNED;
        }
    }
}
