// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.Serialization;

namespace System.Collections.Generic
{
    public class KeyNotFoundException : SystemException
    {
        public KeyNotFoundException()
            : base(SR.Arg_KeyNotFound)
        {
            HResult = __HResults.COR_E_KEYNOTFOUND;
        }

        public KeyNotFoundException(String message)
            : base(message)
        {
            HResult = __HResults.COR_E_KEYNOTFOUND;
        }

        public KeyNotFoundException(String message, Exception innerException)
            : base(message, innerException)
        {
            HResult = __HResults.COR_E_KEYNOTFOUND;
        }

        protected KeyNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            throw new PlatformNotSupportedException();
        }
    }
}
