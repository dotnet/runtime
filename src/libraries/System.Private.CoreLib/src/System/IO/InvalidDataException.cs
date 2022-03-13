// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Serialization;

namespace System.IO
{
    /// <summary>The exception that is thrown when a data stream is in an invalid format.</summary>
    /// <remarks>An <see cref="System.IO.InvalidDataException" /> is thrown when invalid data is detected in the data stream.</remarks>
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public sealed class InvalidDataException : SystemException
    {
        /// <summary>Initializes a new instance of the <see cref="System.IO.InvalidDataException" /> class.</summary>
        /// <remarks>This constructor initializes the <see cref="System.Exception.Message" /> property of the new instance to a system-supplied message that describes the error, such as "An invalid argument was specified." This message is localized based on the current system culture.</remarks>
        public InvalidDataException()
            : base(SR.GenericInvalidData)
        {
        }

        /// <summary>Initializes a new instance of the <see cref="System.IO.InvalidDataException" /> class with a specified error message.</summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <remarks>This constructor initializes the <see cref="System.Exception.Message" /> property of the new instance to a system-supplied message that describes the error, such as "An invalid argument was specified." This message is localized based on the current system culture.</remarks>
        public InvalidDataException(string? message)
            : base(message)
        {
        }

        /// <summary>Initializes a new instance of the <see cref="System.IO.InvalidDataException" /> class with a reference to the inner exception that is the cause of this exception.</summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception. If the <paramref name="innerException" /> parameter is not <see langword="null" />, the current exception is raised in a <see langword="catch" /> block that handles the inner exception.</param>
        /// <remarks>This constructor initializes the <see cref="System.Exception.Message" /> property of the new instance using the value of the <paramref name="message" /> parameter. The content of the <paramref name="message" /> parameter is intended to be understood by humans. The caller of this constructor is required to ensure that this string has been localized for the current system culture.
        /// An exception that is thrown as a direct result of a previous exception should include a reference to the previous exception in the <see cref="System.Exception.InnerException" /> property. The <see cref="System.Exception.InnerException" /> property returns the same value that is passed into the constructor, or <see langword="null" /> if the <see cref="System.Exception.InnerException" /> property does not supply the inner exception value to the constructor.</remarks>
        public InvalidDataException(string? message, Exception? innerException)
            : base(message, innerException)
        {
        }

        private InvalidDataException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
