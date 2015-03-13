// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.CSharp.RuntimeBinder
{
    /// <summary>
    /// Represents an error that occurs while processing a dynamic bind in the C# runtime binder. Exceptions of this type differ from <see cref="RuntimeBinderInternalCompilerException"/> in that
    /// <see cref="RuntimeBinderException"/> represents a failure to bind in the sense of a usual compiler error, whereas <see cref="RuntimeBinderInternalCompilerException"/>
    /// represents a malfunctioning of the runtime binder itself.
    /// </summary>
    public class RuntimeBinderException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RuntimeBinderException"/> class. 
        /// </summary>
        public RuntimeBinderException()
            : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RuntimeBinderException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        public RuntimeBinderException(string message)
            : base(message)
        {
        }

        /// <summary>
        ///  Initializes a new instance of the <see cref="RuntimeBinderException"/> class with a specified error message
        ///  and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference if no inner exception is specified.</param>
        public RuntimeBinderException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}