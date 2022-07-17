// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;

namespace Microsoft.WebAssembly.Diagnostics;

public class ExpressionEvaluationFailedException : Exception
{
    public ExpressionEvaluationFailedException(string message) : base(message)
    {
    }

    public ExpressionEvaluationFailedException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}
