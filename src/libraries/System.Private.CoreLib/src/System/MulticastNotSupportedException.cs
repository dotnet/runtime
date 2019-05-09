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
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public sealed class MulticastNotSupportedException : SystemException
    {
        public MulticastNotSupportedException()
            : base(SR.Arg_MulticastNotSupportedException)
        {
            HResult = HResults.COR_E_MULTICASTNOTSUPPORTED;
        }

        public MulticastNotSupportedException(string? message)
            : base(message)
        {
            HResult = HResults.COR_E_MULTICASTNOTSUPPORTED;
        }

        public MulticastNotSupportedException(string? message, Exception? inner)
            : base(message, inner)
        {
            HResult = HResults.COR_E_MULTICASTNOTSUPPORTED;
        }

        internal MulticastNotSupportedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
