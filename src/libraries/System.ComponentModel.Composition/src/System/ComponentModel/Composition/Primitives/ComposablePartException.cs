// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace System.ComponentModel.Composition.Primitives
{
    /// <summary>
    ///     The exception that is thrown when an error occurs when calling methods on a
    ///     <see cref="ComposablePart"/>.
    /// </summary>
    [DebuggerTypeProxy(typeof(ComposablePartExceptionDebuggerProxy))]
    [DebuggerDisplay("{Message}")]
    [Serializable]
    public class ComposablePartException : Exception
    {
        private readonly ICompositionElement? _element;

        /// <summary>
        ///     Initializes a new instance of the <see cref="ComposablePartException"/> class.
        /// </summary>
        public ComposablePartException()
            : this((string?)null, (ICompositionElement?)null, (Exception?)null)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="ComposablePartException"/> class
        ///     with the specified serialization data.
        /// </summary>
        /// <param name="info">
        ///     The <see cref="SerializationInfo"/> that holds the serialized object data about the
        ///     <see cref="ComposablePartException"/>.
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
#if NET8_0_OR_GREATER
        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
#endif
        protected ComposablePartException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            _element = (ICompositionElement?)info.GetValue("Element", typeof(ICompositionElement));
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="ComposablePartException"/> class
        ///     with the specified error message.
        /// </summary>
        /// <param name="message">
        ///     A <see cref="string"/> containing a message that describes the
        ///     <see cref="ComposablePartException"/>; or <see langword="null"/> to set
        ///     the <see cref="Exception.Message"/> property to its default value.
        /// </param>
        public ComposablePartException(string? message)
            : this(message, (ICompositionElement?)null, (Exception?)null)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="ComposablePartException"/> class
        ///     with the specified error message and composition element that is the cause of
        ///     the exception.
        /// </summary>
        /// <param name="message">
        ///     A <see cref="string"/> containing a message that describes the
        ///     <see cref="ComposablePartException"/>; or <see langword="null"/> to set
        ///     the <see cref="Exception.Message"/> property to its default value.
        /// </param>
        /// <param name="element">
        ///     The <see cref="ICompositionElement"/> that is the cause of the
        ///     <see cref="ComposablePartException"/>; or <see langword="null"/> to set
        ///     the <see cref="ComposablePartException.Element"/> property to
        ///     <see langword="null"/>.
        /// </param>
        public ComposablePartException(string? message, ICompositionElement? element)
            : this(message, element, (Exception?)null)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="ComposablePartException"/> class
        ///     with the specified error message and exception that is the cause of the
        ///     exception.
        /// </summary>
        /// <param name="message">
        ///     A <see cref="string"/> containing a message that describes the
        ///     <see cref="ComposablePartException"/>; or <see langword="null"/> to set
        ///     the <see cref="Exception.Message"/> property to its default value.
        /// </param>
        /// <param name="innerException">
        ///     The <see cref="Exception"/> that is the underlying cause of the
        ///     <see cref="ComposablePartException"/>; or <see langword="null"/> to set
        ///     the <see cref="Exception.InnerException"/> property to <see langword="null"/>.
        /// </param>
        public ComposablePartException(string? message, Exception? innerException)
            : this(message, (ICompositionElement?)null, innerException)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="ComposablePartException"/> class
        ///     with the specified error message, and composition element and exception that
        ///     are the cause of the exception.
        /// </summary>
        /// <param name="message">
        ///     A <see cref="string"/> containing a message that describes the
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
        public ComposablePartException(string? message, ICompositionElement? element, Exception? innerException)
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
        public ICompositionElement? Element
        {
            get { return _element; }
        }

        /// <summary>
        ///     Gets the serialization data of the exception.
        /// </summary>
        /// <param name="info">
        ///     The <see cref="SerializationInfo"/> that holds the serialized object data about the
        ///     <see cref="ComposablePartException"/>.
        /// </param>
        /// <param name="context">
        ///     The <see cref="StreamingContext"/> that contains contextual information about the
        ///     source or destination.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="info"/> is <see langword="null"/>.
        /// </exception>
#if NET8_0_OR_GREATER
        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
#endif
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("Element", _element.ToSerializableElement());
        }
    }
}
