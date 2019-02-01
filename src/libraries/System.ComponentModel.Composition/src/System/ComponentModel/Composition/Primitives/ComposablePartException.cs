// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.ComponentModel.Composition.Primitives
{
    /// <summary>
    ///     The exception that is thrown when an error occurs when calling methods on a
    ///     <see cref="ComposablePart"/>.
    /// </summary>
    [DebuggerTypeProxy(typeof(ComposablePartExceptionDebuggerProxy))]
    [DebuggerDisplay("{Message}")]
    public class ComposablePartException : Exception
    {
        private readonly ICompositionElement _element;

        /// <summary>
        ///     Initializes a new instance of the <see cref="ComposablePartException"/> class.
        /// </summary>
        public ComposablePartException()
            : this((string)null, (ICompositionElement)null, (Exception)null)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="ComposablePartException"/> class 
        ///     with the specified error message.
        /// </summary>
        /// <param name="message">
        ///     A <see cref="String"/> containing a message that describes the 
        ///     <see cref="ComposablePartException"/>; or <see langword="null"/> to set
        ///     the <see cref="Exception.Message"/> property to its default value.
        /// </param>
        public ComposablePartException(string message)
            : this(message, (ICompositionElement)null, (Exception)null)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="ComposablePartException"/> class 
        ///     with the specified error message and composition element that is the cause of
        ///     the exception.
        /// </summary>
        /// <param name="message">
        ///     A <see cref="String"/> containing a message that describes the 
        ///     <see cref="ComposablePartException"/>; or <see langword="null"/> to set
        ///     the <see cref="Exception.Message"/> property to its default value.
        /// </param>
        /// <param name="element">
        ///     The <see cref="ICompositionElement"/> that is the cause of the
        ///     <see cref="ComposablePartException"/>; or <see langword="null"/> to set
        ///     the <see cref="ComposablePartException.Element"/> property to 
        ///     <see langword="null"/>.
        /// </param>
        public ComposablePartException(string message, ICompositionElement element)
            : this(message, element, (Exception)null)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="ComposablePartException"/> class 
        ///     with the specified error message and exception that is the cause of the  
        ///     exception.
        /// </summary>
        /// <param name="message">
        ///     A <see cref="String"/> containing a message that describes the 
        ///     <see cref="ComposablePartException"/>; or <see langword="null"/> to set
        ///     the <see cref="Exception.Message"/> property to its default value.
        /// </param>
        /// <param name="innerException">
        ///     The <see cref="Exception"/> that is the underlying cause of the 
        ///     <see cref="ComposablePartException"/>; or <see langword="null"/> to set
        ///     the <see cref="Exception.InnerException"/> property to <see langword="null"/>.
        /// </param>
        public ComposablePartException(string message, Exception innerException)
            : this(message, (ICompositionElement)null, innerException)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="ComposablePartException"/> class 
        ///     with the specified error message, and composition element and exception that 
        ///     are the cause of the exception.
        /// </summary>
        /// <param name="message">
        ///     A <see cref="String"/> containing a message that describes the 
        ///     <see cref="ComposablePartException"/>; or <see langword="null"/> to set
        ///     the <see cref="Exception.Message"/> property to its default value.
        /// </param>
        /// <param name="element">
        ///     The <see cref="ICompositionElement"/> that is the cause of the
        ///     <see cref="ComposablePartException"/>; or <see langword="null"/> to set
        ///     the <see cref="ComposablePartException.Element"/> property to 
        ///     <see langword="null"/>.
        /// </param>
        /// <param name="innerException">
        ///     The <see cref="Exception"/> that is the underlying cause of the 
        ///     <see cref="ComposablePartException"/>; or <see langword="null"/> to set
        ///     the <see cref="Exception.InnerException"/> property to <see langword="null"/>.
        /// </param>
        public ComposablePartException(string message, ICompositionElement element, Exception innerException)
            : base(message, innerException)
        {
            _element = element;
        }

        /// <summary>
        ///     Gets the composition element that is the cause of the exception.
        /// </summary>
        /// <value>
        ///     The <see cref="ICompositionElement"/> that is the cause of the
        ///     <see cref="ComposablePartException"/>. The default is <see langword="null"/>.
        /// </value>
        public ICompositionElement Element
        {
            get { return _element; }
        }
    }
}
