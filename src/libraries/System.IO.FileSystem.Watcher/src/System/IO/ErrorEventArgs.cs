// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.IO
{
    /// <devdoc>
    ///    Provides data for the <see cref='E:System.IO.FileSystemWatcher.Error'/> event.
    /// </devdoc>
    public class ErrorEventArgs : EventArgs
    {
        private readonly Exception _exception;

        /// <devdoc>
        ///    Initializes a new instance of the class.
        /// </devdoc>
        public ErrorEventArgs(Exception exception)
        {
            _exception = exception;
        }

        /// <devdoc>
        ///    Gets the <see cref='System.Exception'/> that represents the error that occurred.
        /// </devdoc>
        public virtual Exception GetException()
        {
            return _exception;
        }
    }
}
