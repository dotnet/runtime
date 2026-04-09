// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;

namespace Microsoft.Extensions.Logging.TraceSource
{
    /// <summary>
    /// Provides an IDisposable that represents a logical operation scope based on System.Diagnostics LogicalOperationStack
    /// </summary>
    internal sealed class TraceSourceScope : IDisposable
    {
        // To detect redundant calls
        private bool _isDisposed;

        /// <summary>
        /// Pushes state onto the LogicalOperationStack by calling
        /// <see cref="CorrelationManager.StartLogicalOperation(object)"/>
        /// </summary>
        /// <param name="state">The state.</param>
        public TraceSourceScope(object state)
        {
            Trace.CorrelationManager.StartLogicalOperation(state);
        }

        /// <summary>
        /// Pops a state off the LogicalOperationStack by calling
        /// <see cref="CorrelationManager.StopLogicalOperation()"/>
        /// </summary>
        public void Dispose()
        {
            if (!_isDisposed)
            {
                Trace.CorrelationManager.StopLogicalOperation();
                _isDisposed = true;
            }
        }
    }
}
