// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.Serialization;

namespace Claunia.PropertyList
{
    /// <summary>The exception that is thrown when an property list file could not be processed correctly.</summary>
    [Serializable]
    public class PropertyListException : Exception
    {
        /// <summary>Initializes a new instance of the <see cref="PropertyListException" /> class.</summary>
        public PropertyListException() {}

        /// <summary>Initializes a new instance of the <see cref="PropertyListException" /> class.</summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        public PropertyListException(string message) : base(message) {}

        /// <summary>Initializes a new instance of the <see cref="PropertyListException" /> class.</summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="inner">
        ///     The exception that is the cause of the current exception, or <see langword="null" /> if no inner
        ///     exception is specified.
        /// </param>
        public PropertyListException(string message, Exception inner) : base(message, inner) {}

        [Obsolete]
        protected PropertyListException(SerializationInfo info, StreamingContext context) : base(info, context) {}
    }
}
