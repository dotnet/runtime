// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;

namespace System.ComponentModel.Composition
{
    /// <summary>
    ///     The exception that is thrown when the underlying exported value or metadata of an 
    ///     <see cref="Lazy{T}"/> or <see cref="Lazy{T, TMetadataView}"/> object cannot be 
    ///     cast to <c>T</c> or <c>TMetadataView</c>, respectively.
    /// </summary>
    [Serializable]
    public class CompositionContractMismatchException : Exception
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="CompositionContractMismatchException"/> class.
        /// </summary>
        public CompositionContractMismatchException()
            : this((string)null, (Exception)null)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="CompositionContractMismatchException"/> class 
        ///     with the specified error message.
        /// </summary>
        /// <param name="message">
        ///     A <see cref="String"/> containing a message that describes the 
        ///     <see cref="CompositionContractMismatchException"/>; or <see langword="null"/> to set
        ///     the <see cref="Exception.Message"/> property to its default value.
        /// </param>
        public CompositionContractMismatchException(string message)
            : this(message, (Exception)null)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="CompositionContractMismatchException"/> class 
        ///     with the specified error message and exception that is the cause of the  
        ///     exception.
        /// </summary>
        /// <param name="message">
        ///     A <see cref="String"/> containing a message that describes the 
        ///     <see cref="CompositionContractMismatchException"/>; or <see langword="null"/> to set
        ///     the <see cref="Exception.Message"/> property to its default value.
        /// </param>
        /// <param name="innerException">
        ///     The <see cref="Exception"/> that is the underlying cause of the 
        ///     <see cref="CompositionContractMismatchException"/>; or <see langword="null"/> to set
        ///     the <see cref="Exception.InnerException"/> property to <see langword="null"/>.
        /// </param>
        public CompositionContractMismatchException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
        
        /// <summary>
        ///     Initializes a new instance of the <see cref="CompositionContractMismatchException"/> class 
        ///     with the specified serialization data.
        /// </summary>
        /// <param name="info">
        ///     The <see cref="SerializationInfo"/> that holds the serialized object data about the 
        ///     <see cref="CompositionContractMismatchException"/>.
        /// </param>
        /// <param name="context">
        ///     The <see cref="StreamingContext"/> that contains contextual information about the 
        ///     source or destination.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="info"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="SerializationException">
        ///     <paramref name="info"/> is missing a required value.
        /// </exception>
        /// <exception cref="InvalidCastException">
        ///     <paramref name="info"/> contains a value that cannot be cast to the correct type.
        /// </exception>
        protected CompositionContractMismatchException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
