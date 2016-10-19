// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;

namespace System.IO
{
    [Serializable]
    public sealed class InvalidDataException : SystemException
    {
        public InvalidDataException()
            : base(SR.GenericInvalidData)
        {
        }

        public InvalidDataException(string message)
            : base(message)
        {
        }

        public InvalidDataException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        internal InvalidDataException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
