// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ComponentModel
{
    /// <devdoc>
    ///    <para>The exception that is thrown when a thread that an operation should execute on no longer exists or is not pumping messages</para>
    /// </devdoc>
    public class InvalidAsynchronousStateException : ArgumentException
    {
        /// <devdoc>
        /// <para>Initializes a new instance of the <see cref='System.ComponentModel.InvalidAsynchronousStateException'/> class without a message.</para>
        /// </devdoc>
        public InvalidAsynchronousStateException() : this(null)
        {
        }

        /// <devdoc>
        /// <para>Initializes a new instance of the <see cref='System.ComponentModel.InvalidAsynchronousStateException'/> class with 
        ///    the specified message.</para>
        /// </devdoc>
        public InvalidAsynchronousStateException(string message)
            : base(message)
        {
        }

        /// <devdoc>
        ///     Initializes a new instance of the Exception class with a specified error message and a 
        ///     reference to the inner exception that is the cause of this exception.
        /// </devdoc>
        public InvalidAsynchronousStateException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
