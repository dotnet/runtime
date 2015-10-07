// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Tracing;
using System.Runtime.CompilerServices;

namespace System.Diagnostics
{
    /// <summary>
    /// Current this EventSource is only here so that the Debugger can inject code using Function evaluation
    /// We may add actual logging requests as well at some point.   This can be removed if the debugger  no 
    /// longer needs it.   
    /// </summary>
    [EventSource(Name = "Microsoft-Diagnostics-DiagnosticSource")]
    internal class DiagnosticSourceEventSource : EventSource
    {
        public static DiagnosticSourceEventSource Logger = new DiagnosticSourceEventSource();

        /// <summary>
        /// On every command (which the debugger can force by turning on this EventSource with ETW)
        /// call a function that the debugger can hook to do an arbitrary func evaluation.  
        /// </summary>
        /// <param name="args"></param>
        protected override void OnEventCommand(EventCommandEventArgs args)
        {
            BreakPointWithDebuggerFuncEval();
        }

        #region private 
        private volatile bool _false;       // A value that is always false but the compiler does not know this. 

        /// <summary>
        /// A function which is fully interruptible even in release code so we can stop here and 
        /// do function evaluation in the debugger.   Thus this is just a place that is useful
        /// for the debugger to place a breakpoint where it can inject code with function evaluation
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private void BreakPointWithDebuggerFuncEval()
        {
            new object();   // This is only here because it helps old desktop runtimes emit a GC safe point at the start of the method
            while (_false)
            {
                _false = false;
            }
        }
        #endregion 
    }
}
