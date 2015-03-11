// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
