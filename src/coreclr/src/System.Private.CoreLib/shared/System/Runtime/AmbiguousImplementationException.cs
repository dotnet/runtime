// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System.Runtime.Serialization;

namespace System.Runtime
{
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("System.Runtime, Version=4.2.1.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public sealed class AmbiguousImplementationException : Exception
    {
        public AmbiguousImplementationException()
            : base(SR.AmbiguousImplementationException_NullMessage)
        {
            HResult = HResults.COR_E_AMBIGUOUSIMPLEMENTATION;
        }

        public AmbiguousImplementationException(string? message)
            : base(message)
        {
            HResult = HResults.COR_E_AMBIGUOUSIMPLEMENTATION;
        }

        public AmbiguousImplementationException(string? message, Exception? innerException)
            : base(message, innerException)
        {
            HResult = HResults.COR_E_AMBIGUOUSIMPLEMENTATION;
        }

        private AmbiguousImplementationException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
