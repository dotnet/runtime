// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Internal;

namespace System.ComponentModel.Composition
{
    internal class ImportCardinalityMismatchExceptionDebuggerProxy
    {
        private readonly ImportCardinalityMismatchException _exception;

        public ImportCardinalityMismatchExceptionDebuggerProxy(ImportCardinalityMismatchException exception)
        {
            Requires.NotNull(exception, nameof(exception));

            _exception = exception;
        }

        public Exception InnerException 
        { 
            get { return _exception.InnerException; }
        }

        public string Message
        {
            get { return _exception.Message; }
        }
    }
}
