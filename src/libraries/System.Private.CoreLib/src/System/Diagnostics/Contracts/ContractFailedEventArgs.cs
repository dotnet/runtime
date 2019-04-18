// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace System.Diagnostics.Contracts
{
    public sealed class ContractFailedEventArgs : EventArgs
    {
        private ContractFailureKind _failureKind;
        private string? _message;
        private string? _condition;
        private Exception? _originalException;
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

        public string? Message { get { return _message; } }
        public string? Condition { get { return _condition; } }
        public ContractFailureKind FailureKind { get { return _failureKind; } }
        public Exception? OriginalException { get { return _originalException; } }

        // Whether the event handler "handles" this contract failure, or to fail via escalation policy.
        public bool Handled
        {
            get { return _handled; }
        }

        public void SetHandled()
        {
            _handled = true;
        }

        public bool Unwind
        {
            get { return _unwind; }
        }

        public void SetUnwind()
        {
            _unwind = true;
        }
    }
}
