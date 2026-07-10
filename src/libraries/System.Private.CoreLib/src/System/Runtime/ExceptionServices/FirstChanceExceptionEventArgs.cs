// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.ExceptionServices
{
    // Definition of the argument-type passed to the FirstChanceException event handler
    public class FirstChanceExceptionEventArgs : EventArgs
    {
        // The CoreCLR runtime allocates this object and sets _exception directly
        // during first-chance exception dispatch, so this field must not be renamed
        // without updating the runtime (see corelib.h FIELD__FIRSTCHANCE_EVENTARGS__EXCEPTION).
        private readonly Exception _exception;

        public FirstChanceExceptionEventArgs(Exception exception)
        {
            _exception = exception;
        }

        // Returns the exception object pertaining to the first chance exception
        public Exception Exception => _exception;
    }
}
