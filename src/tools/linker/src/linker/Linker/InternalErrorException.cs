// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Mono.Linker
{
    public class InternalErrorException : Exception
    {
        /// <summary>
        /// Represents an internal fatal error. Should be used in places that are not expected to be hit by the linker.
        /// </summary>
        /// <param name="message">Message description of what went wrong and why</param>
        public InternalErrorException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Represents an internal fatal error. Should be used in places that are not expected to be hit by the linker.
        /// </summary>
        /// <param name="message">Message description of what went wrong and why</param>
        /// <param name="innerException"></param>
        public InternalErrorException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
