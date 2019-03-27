// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;

namespace Microsoft.Extensions.Logging.TraceSource
{
    /// <summary>
    /// Provides an IDisposable that represents a logical operation scope based on System.Diagnostics LogicalOperationStack
    /// </summary>
    [Obsolete("This type is obsolete and will be removed in a future version. This type is part of TraceSource logger implementation and shouldn't be used directly")]
    public class TraceSourceScope : IDisposable
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
