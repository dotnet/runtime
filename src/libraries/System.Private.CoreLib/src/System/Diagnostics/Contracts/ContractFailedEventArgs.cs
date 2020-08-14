// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Diagnostics.Contracts
{
    public sealed class ContractFailedEventArgs : EventArgs
    {
        private readonly ContractFailureKind _failureKind;
        private readonly string? _message;
        private readonly string? _condition;
        private readonly Exception? _originalException;
        private bool _handled;
        private bool _unwind;

        internal Exception? thrownDuringHandler;

        public ContractFailedEventArgs(ContractFailureKind failureKind, string? message, string? condition, Exception? originalException)
        {
            Debug.Assert(originalException == null || failureKind == ContractFailureKind.PostconditionOnException);
            _failureKind = failureKind;
            _message = message;
            _condition = condition;
            _originalException = originalException;
        }

        public string? Message => _message;
        public string? Condition => _condition;
        public ContractFailureKind FailureKind => _failureKind;
        public Exception? OriginalException => _originalException;

        // Whether the event handler "handles" this contract failure, or to fail via escalation policy.
        public bool Handled => _handled;

        public void SetHandled()
        {
            _handled = true;
        }

        public bool Unwind => _unwind;

        public void SetUnwind()
        {
            _unwind = true;
        }
    }
}
