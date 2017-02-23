// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================================
**
**
**
** Purpose: The exception class for some failed P/Invoke calls.
**
**
=============================================================================*/


using System;
using System.Runtime.Serialization;

namespace System
{
    [Serializable]
    public class DllNotFoundException : TypeLoadException
    {
        public DllNotFoundException()
            : base(Environment.GetResourceString("Arg_DllNotFoundException"))
        {
            SetErrorCode(__HResults.COR_E_DLLNOTFOUND);
        }

        public DllNotFoundException(String message)
            : base(message)
        {
            SetErrorCode(__HResults.COR_E_DLLNOTFOUND);
        }

        public DllNotFoundException(String message, Exception inner)
            : base(message, inner)
        {
            SetErrorCode(__HResults.COR_E_DLLNOTFOUND);
        }

        protected DllNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
