// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;

namespace System
{
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("System.Core, Version=3.5.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class InvalidTimeZoneException : Exception
    {
        public InvalidTimeZoneException()
        {
        }

        public InvalidTimeZoneException(string? message)
            : base(message)
        {
        }

        public InvalidTimeZoneException(string? message, Exception? innerException)
            : base(message, innerException)
        {
        }

        protected InvalidTimeZoneException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
