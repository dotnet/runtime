// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================================
**
**
** Purpose: The exception class for a misaligned access exception
**
=============================================================================*/

namespace System 
{
    using System;
    using System.Runtime.Serialization;

    [Serializable]
[System.Runtime.InteropServices.ComVisible(true)]
    public sealed class DataMisalignedException : SystemException 
    {
        public DataMisalignedException() 
            : base(Environment.GetResourceString("Arg_DataMisalignedException")) 
        {
            SetErrorCode(__HResults.COR_E_DATAMISALIGNED);
        }
    
        public DataMisalignedException(String message) 
            : base(message) 
        {
            SetErrorCode(__HResults.COR_E_DATAMISALIGNED);
        }

        public DataMisalignedException(String message, Exception innerException) 
            : base(message, innerException) 
        {
            SetErrorCode(__HResults.COR_E_DATAMISALIGNED);
        }

        internal DataMisalignedException(SerializationInfo info, StreamingContext context) 
            : base (info, context) 
        {
        }
    }

}
