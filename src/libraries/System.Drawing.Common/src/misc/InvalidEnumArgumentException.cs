// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Runtime.Serialization;

namespace System.ComponentModel
{
    /// <summary>
    /// The exception that is thrown when using invalid arguments that are enumerators.
    /// </summary>
    internal class InvalidEnumArgumentException : ArgumentException
    {
        /// <summary>
        /// <para>Initializes a new instance of the <see cref='InvalidEnumArgumentException'/> class without a message.</para>
        /// </summary>
        public InvalidEnumArgumentException() : this(null)
        {
        }

        /// <summary>
        /// <para>Initializes a new instance of the <see cref='InvalidEnumArgumentException'/> class with 
        ///    the specified message.</para>
        /// </summary>
        public InvalidEnumArgumentException(string message)
            : base(message)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the Exception class with a specified error message and a 
        ///     reference to the inner exception that is the cause of this exception.
        ///     FxCop CA1032: Multiple constructors are required to correctly implement a custom exception.
        /// </summary>
        public InvalidEnumArgumentException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// <para>Initializes a new instance of the <see cref='InvalidEnumArgumentException'/> class with a 
        ///    message generated from the argument, invalid value, and enumeration
        ///    class.</para>
        /// </summary>
        public InvalidEnumArgumentException(string argumentName, int invalidValue, Type enumClass)
            : base(SR.Format(SR.InvalidEnumArgument,
                                argumentName,
                                invalidValue.ToString(CultureInfo.CurrentCulture),
                                enumClass.Name), argumentName)
        {
        }

        /// <summary>
        ///     Need this constructor since Exception implements ISerializable. We don't have any fields,
        ///     so just forward this to base.
        /// </summary>
        protected InvalidEnumArgumentException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            throw new PlatformNotSupportedException();
        }
    }
}