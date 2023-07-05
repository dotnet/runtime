// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;

namespace Microsoft.WebAssembly.Diagnostics;

public class InternalErrorException : Exception
{
    public InternalErrorException(string message) : base($"Internal error: {message}")
    {
    }

    public InternalErrorException(string message, Exception? innerException) : base($"Internal error: {message}", innerException)
    {
    }
}
