// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System.ComponentModel
{
    /// <summary>
    /// The exception that is thrown when using invalid arguments that are enumerators.
    /// </summary>
    [Serializable]
    [TypeForwardedFrom("System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class InvalidEnumArgumentException : ArgumentException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref='InvalidEnumArgumentException'/>
        /// class without a message.
        /// </summary>
        public InvalidEnumArgumentException() : this(null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref='InvalidEnumArgumentException'/>
        /// class with the specified message.
        /// </summary>
        public InvalidEnumArgumentException(string? message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the Exception class with a specified error message
        /// and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        public InvalidEnumArgumentException(string? message, Exception? innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref='InvalidEnumArgumentException'/>
        /// class with a message generated from the argument, invalid value, and
        /// enumeration class.
        /// </summary>
        public InvalidEnumArgumentException(string? argumentName, int invalidValue, Type enumClass)
            : base(SR.Format(SR.InvalidEnumArgument,
                                argumentName,
                                invalidValue,
                                enumClass?.Name), argumentName)
        {
            ArgumentNullException.ThrowIfNull(enumClass);
        }

        /// <summary>
        /// Need this constructor since Exception implements ISerializable.
        /// We don't have any fields, so just forward this to base.
        /// </summary>
        protected InvalidEnumArgumentException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
