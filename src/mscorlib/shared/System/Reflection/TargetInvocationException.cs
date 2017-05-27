// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;

namespace System.Reflection
{
    public sealed class TargetInvocationException : ApplicationException
    {
        public TargetInvocationException(Exception inner)
            : base(SR.Arg_TargetInvocationException, inner)
        {
            HResult = __HResults.COR_E_TARGETINVOCATION;
        }

        public TargetInvocationException(string message, Exception inner)
            : base(message, inner)
        {
            HResult = __HResults.COR_E_TARGETINVOCATION;
        }
    }
}
