// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Internal;

namespace System.ComponentModel.Composition.Primitives
{
    internal sealed class ComposablePartExceptionDebuggerProxy
    {
        private readonly ComposablePartException _exception;

        public ComposablePartExceptionDebuggerProxy(ComposablePartException exception)
        {
            Requires.NotNull(exception, nameof(exception));

            _exception = exception;
        }

        public ICompositionElement? Element
        {
            get { return _exception.Element; }
        }

        public Exception? InnerException
        {
            get { return _exception.InnerException; }
        }

        public string Message
        {
            get { return _exception.Message; }
        }
    }
}
