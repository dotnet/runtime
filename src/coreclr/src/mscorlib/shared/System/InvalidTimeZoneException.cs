// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;

namespace System
{
    public class InvalidTimeZoneException : Exception
    {
        public InvalidTimeZoneException()
        {
        }

        public InvalidTimeZoneException(String message)
            : base(message)
        {
        }

        public InvalidTimeZoneException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected InvalidTimeZoneException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            throw new PlatformNotSupportedException();
        }
    }
}
