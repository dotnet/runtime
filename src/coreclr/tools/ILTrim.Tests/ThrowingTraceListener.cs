// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace ILTrim.Tests
{
    /// <summary>
    /// Replaces the default trace listener so that Debug.Assert failures throw
    /// an exception instead of calling Environment.FailFast.  This allows the
    /// expected-failure mechanism in TestSuites to catch and skip known ILTrim
    /// limitations instead of crashing the entire test runner process.
    /// </summary>
    internal sealed class ThrowingTraceListener : TraceListener
    {
        public override void Fail(string? message, string? detailMessage)
        {
            throw new DebugAssertException(
                message + (detailMessage is null ? "" : Environment.NewLine + detailMessage));
        }

        public override void Write(string? message) { }
        public override void WriteLine(string? message) { }
    }

    internal sealed class DebugAssertException : Exception
    {
        public DebugAssertException(string? message) : base(message) { }
    }

    internal static class TraceListenerSetup
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            Trace.Listeners.Clear();
            Trace.Listeners.Add(new ThrowingTraceListener());
        }
    }
}
