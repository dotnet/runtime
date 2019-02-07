// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Runtime.Serialization;

namespace System.Runtime
{
    [Serializable]
    public sealed class AmbiguousImplementationException : Exception
    {
        public AmbiguousImplementationException()
            : base(SR.AmbiguousImplementationException_NullMessage)
        {
            HResult = HResults.COR_E_AMBIGUOUSIMPLEMENTATION;
        }

        public AmbiguousImplementationException(string message)
            : base(message)
        {
            HResult = HResults.COR_E_AMBIGUOUSIMPLEMENTATION;
        }

        public AmbiguousImplementationException(string message, Exception innerException)
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
