// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;

namespace Microsoft.WebAssembly.AppHost;

public class CommandLineException : Exception
{
    public CommandLineException() { }
    public CommandLineException(string message) : base(message) { }
    public CommandLineException(string message, Exception inner) : base(message, inner) { }
}
