// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System
{
    public class UnhandledExceptionEventArgs : EventArgs
    {
        private readonly object _exception;
        private readonly bool _isTerminating;

        public UnhandledExceptionEventArgs(object exception, bool isTerminating)
        {
            _exception = exception;
            _isTerminating = isTerminating;
        }

        public object ExceptionObject => _exception;

        public bool IsTerminating => _isTerminating;
    }
}
