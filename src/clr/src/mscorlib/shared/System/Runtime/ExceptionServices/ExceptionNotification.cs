// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Runtime.ExceptionServices
{
    // Definition of the argument-type passed to the FirstChanceException event handler
    public class FirstChanceExceptionEventArgs : EventArgs
    {
        public FirstChanceExceptionEventArgs(Exception exception)
        {
            Exception = exception;
        }

        // Returns the exception object pertaining to the first chance exception
        public Exception Exception { get; }
    }
}
