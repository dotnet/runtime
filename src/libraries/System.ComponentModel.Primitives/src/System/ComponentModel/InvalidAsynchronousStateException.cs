// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System.ComponentModel
{
    /// <summary>
    /// The exception that is thrown when a thread that an operation should execute on no
    /// longer exists or is not pumping messages
    /// </summary>
    [Serializable]
    [TypeForwardedFrom("System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class InvalidAsynchronousStateException : ArgumentException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref='System.ComponentModel.InvalidAsynchronousStateException'/>
        /// class without a message.
        /// </summary>
        public InvalidAsynchronousStateException() : this(null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref='System.ComponentModel.InvalidAsynchronousStateException'/>
        /// class with the specified message.
        /// </summary>
        public InvalidAsynchronousStateException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the Exception class with a specified error message
        /// and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        public InvalidAsynchronousStateException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected InvalidAsynchronousStateException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
