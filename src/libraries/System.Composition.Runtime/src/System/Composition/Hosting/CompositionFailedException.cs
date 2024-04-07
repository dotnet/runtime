// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Composition.Hosting
{
    /// <summary>
    /// The exception type thrown when composition problems occur.
    /// Exception should be assumed to be fatal for the entire composition/container unless
    /// otherwise documented - no production code should throw this exception.
    /// </summary>
    public class CompositionFailedException : Exception
    {
        /// <summary>
        /// Construct a <see cref="CompositionFailedException"/> with the default message.
        /// </summary>
        public CompositionFailedException()
            : base(SR.CompositionFailedDefaultExceptionMessage)
        { }

        /// <summary>
        /// Construct a <see cref="CompositionFailedException"/>.
        /// </summary>
        /// <param name="message">The exception message.</param>
        public CompositionFailedException(string message)
            : base(message ?? SR.CompositionFailedDefaultExceptionMessage)
        { }

        /// <summary>
        /// Construct a <see cref="CompositionFailedException"/>.
        /// </summary>
        /// <param name="message">The exception message.</param>
        /// <param name="innerException">The inner exception.</param>
        public CompositionFailedException(string message, Exception innerException)
            : base(message ?? SR.CompositionFailedDefaultExceptionMessage, innerException)
        { }
    }
}
