// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

////////////////////////////////////////////////////////////////////////////////
// MulticastNotSupportedException
// This is thrown when you add multiple callbacks to a non-multicast delegate.
////////////////////////////////////////////////////////////////////////////////

using System.Runtime.Serialization;

namespace System
{
    public sealed class MulticastNotSupportedException : SystemException
    {
        public MulticastNotSupportedException()
            : base(SR.Arg_MulticastNotSupportedException)
        {
            HResult = __HResults.COR_E_MULTICASTNOTSUPPORTED;
        }

        public MulticastNotSupportedException(String message)
            : base(message)
        {
            HResult = __HResults.COR_E_MULTICASTNOTSUPPORTED;
        }

        public MulticastNotSupportedException(String message, Exception inner)
            : base(message, inner)
        {
            HResult = __HResults.COR_E_MULTICASTNOTSUPPORTED;
        }
    }
}
