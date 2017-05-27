// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;

namespace System.Threading
{
    public sealed class ThreadStartException : SystemException
    {
        internal ThreadStartException()
            : base(SR.Arg_ThreadStartException)
        {
            HResult = __HResults.COR_E_THREADSTART;
        }

        internal ThreadStartException(Exception reason)
            : base(SR.Arg_ThreadStartException, reason)
        {
            HResult = __HResults.COR_E_THREADSTART;
        }
    }
}
