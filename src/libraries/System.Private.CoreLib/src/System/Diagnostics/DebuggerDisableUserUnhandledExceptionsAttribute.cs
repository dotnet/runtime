// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Diagnostics
{
    /// <summary>
    /// If a .NET Debugger is attached which supports the Debugger.BreakForUserUnhandledException(Exception) API,
    /// this attribute will prevent the debugger from breaking on user-unhandled exceptions when the
    /// exception is caught by a method with this attribute, unless BreakForUserUnhandledException is called.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class DebuggerDisableUserUnhandledExceptionsAttribute : Attribute
    {
    }
}
