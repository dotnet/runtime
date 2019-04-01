// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System.Runtime.Serialization;

namespace System
{
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("System.Core, Version=3.5.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class TimeZoneNotFoundException : Exception
    {
        public TimeZoneNotFoundException()
        {
        }

        public TimeZoneNotFoundException(string? message)
            : base(message)
        {
        }

        public TimeZoneNotFoundException(string? message, Exception? innerException)
            : base(message, innerException)
        {
        }

        protected TimeZoneNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
