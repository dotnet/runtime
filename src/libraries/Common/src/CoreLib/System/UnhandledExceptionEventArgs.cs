// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System
{
    public class UnhandledExceptionEventArgs : EventArgs
    {
        private Object _exception;
        private bool _isTerminating;

        public UnhandledExceptionEventArgs(Object exception, bool isTerminating)
        {
            _exception = exception;
            _isTerminating = isTerminating;
        }

        public Object ExceptionObject
        {
            get { return _exception; }
        }

        public bool IsTerminating
        {
            get { return _isTerminating; }
        }
    }
}
