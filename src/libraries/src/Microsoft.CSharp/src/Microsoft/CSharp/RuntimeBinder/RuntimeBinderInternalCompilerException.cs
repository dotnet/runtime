// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.Serialization;

namespace Microsoft.CSharp.RuntimeBinder
{
    /// <summary>
    /// Represents an error that occurs while processing a dynamic bind in the C# runtime binder. Exceptions of this type differ from <see cref="RuntimeBinderException"/> in that
    /// <see cref="RuntimeBinderException"/> represents a failure to bind in the sense of a usual compiler error, whereas <see cref="RuntimeBinderInternalCompilerException"/>
    /// represents a malfunctioning of the runtime binder itself.
    /// </summary>
    [Serializable]
    public class RuntimeBinderInternalCompilerException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RuntimeBinderInternalCompilerException"/> class.
        /// </summary>
        public RuntimeBinderInternalCompilerException()
            : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RuntimeBinderInternalCompilerException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        public RuntimeBinderInternalCompilerException(string message)
            : base(message)
        {
        }

        /// <summary>
        ///  Initializes a new instance of the <see cref="RuntimeBinderInternalCompilerException"/> class with a specified error message
        ///  and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference if no inner exception is specified.</param>
        public RuntimeBinderInternalCompilerException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RuntimeBinderInternalCompilerException"/> class with serialized data.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="StreamingContext"/>  that contains contextual information about the source or destination.</param>
        protected RuntimeBinderInternalCompilerException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
