// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime;
using System.Globalization;

namespace System.Runtime.Serialization
{
    [Serializable]
    public sealed class AmbiguousImplementationException : Exception
    {
        public AmbiguousImplementationException()
            : base(SR.AmbiguousImplementationException_NullMessage)
        {
        }

        public AmbiguousImplementationException(string message)
            : base(message)
        {
        }

        public AmbiguousImplementationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        private AmbiguousImplementationException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
