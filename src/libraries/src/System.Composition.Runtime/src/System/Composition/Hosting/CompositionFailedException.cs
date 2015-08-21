// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

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
            : base(Properties.Resources.CompositionFailedDefaultExceptionMessage)
        { }

        /// <summary>
        /// Construct a <see cref="CompositionFailedException"/>.
        /// </summary>
        /// <param name="message">The exception message.</param>
        public CompositionFailedException(string message)
            : base(message)
        { }

        /// <summary>
        /// Construct a <see cref="CompositionFailedException"/>.
        /// </summary>
        /// <param name="message">The exception message.</param>
        /// <param name="innerException">The inner exception.</param>
        public CompositionFailedException(string message, Exception innerException)
            : base(message, innerException)
        { }
    }
}
