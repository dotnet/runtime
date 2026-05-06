// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;

namespace Microsoft.WebAssembly.Diagnostics;

public class DebuggerAgentException : Exception
{
    public DebuggerAgentException(string message) : base(message)
    {
    }

    public DebuggerAgentException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}
