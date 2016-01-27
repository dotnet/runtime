// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//

namespace System.Threading 
{
    using System;
    using System.Runtime.Serialization;
    using System.Runtime.InteropServices;

    [Serializable]
    public sealed class ThreadStartException : SystemException 
    {
        private ThreadStartException() 
            : base(Environment.GetResourceString("Arg_ThreadStartException")) 
        {
            SetErrorCode(__HResults.COR_E_THREADSTART);
        }

        private ThreadStartException(Exception reason)
            : base(Environment.GetResourceString("Arg_ThreadStartException"), reason)
        {
            SetErrorCode(__HResults.COR_E_THREADSTART);
        }

        //required for serialization
        internal ThreadStartException(SerializationInfo info, StreamingContext context) 
            : base(info, context) 
        {
        }
   
    }
}


