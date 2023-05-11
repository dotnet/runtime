// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace System.ComponentModel.Composition
{
    /// <summary>
    ///     The exception that is thrown when the cardinality of a <see cref="Primitives.ImportDefinition"/>
    ///     does not match the cardinality of the <see cref="Primitives.Export"/> objects available in an
    ///     <see cref="Hosting.ExportProvider"/>.
    /// </summary>
    [DebuggerTypeProxy(typeof(ImportCardinalityMismatchExceptionDebuggerProxy))]
    [DebuggerDisplay("{Message}")]
    [Serializable]
    public class ImportCardinalityMismatchException : Exception
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="ImportCardinalityMismatchException"/> class.
        /// </summary>
        public ImportCardinalityMismatchException()
            : this((string?)null, (Exception?)null)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="ImportCardinalityMismatchException"/> class
        ///     with the specified error message.
        /// </summary>
        /// <param name="message">
        ///     A <see cref="string"/> containing a message that describes the
        ///     <see cref="ImportCardinalityMismatchException"/>; or <see langword="null"/> to set
        ///     the <see cref="Exception.Message"/> property to its default value.
        /// </param>
        public ImportCardinalityMismatchException(string? message)
            : this(message, (Exception?)null)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="ImportCardinalityMismatchException"/> class
        ///     with the specified error message and exception that is the cause of the
        ///     exception.
        /// </summary>
        /// <param name="message">
        ///     A <see cref="string"/> containing a message that describes the
        ///     <see cref="ImportCardinalityMismatchException"/>; or <see langword="null"/> to set
        ///     the <see cref="Exception.Message"/> property to its default value.
        /// </param>
        /// <param name="innerException">
        ///     The <see cref="Exception"/> that is the underlying cause of the
        ///     <see cref="ImportCardinalityMismatchException"/>; or <see langword="null"/> to set
        ///     the <see cref="Exception.InnerException"/> property to <see langword="null"/>.
        /// </param>
        public ImportCardinalityMismatchException(string? message, Exception? innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="ImportCardinalityMismatchException"/> class
        ///     with the specified serialization data.
        /// </summary>
        /// <param name="info">
        ///     The <see cref="SerializationInfo"/> that holds the serialized object data about the
        ///     <see cref="ImportCardinalityMismatchException"/>.
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
        protected ImportCardinalityMismatchException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
