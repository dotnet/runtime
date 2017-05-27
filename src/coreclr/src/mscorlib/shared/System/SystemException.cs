// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;

namespace System
{
    public class SystemException : Exception
    {
        public SystemException()
            : base(SR.Arg_SystemException)
        {
            HResult = __HResults.COR_E_SYSTEM;
        }

        public SystemException(String message)
            : base(message)
        {
            HResult = __HResults.COR_E_SYSTEM;
        }

        public SystemException(String message, Exception innerException)
            : base(message, innerException)
        {
            HResult = __HResults.COR_E_SYSTEM;
        }

        protected SystemException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            throw new PlatformNotSupportedException();
        }
    }
}
