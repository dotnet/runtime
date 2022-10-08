// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;

namespace Microsoft.WebAssembly.Diagnostics;

public struct ValueOrError<TValue>
{
    public TValue? Value { get; init; }
    public Result? Error { get; init; }

    public bool IsError => Error != null;

    private ValueOrError(TValue? value = default, Result? error = default)
    {
        if (value != null && error != null)
            throw new ArgumentException($"Both {nameof(value)}, and {nameof(error)} cannot be non-null");

        if (value == null && error == null)
            throw new ArgumentException($"Both {nameof(value)}, and {nameof(error)} cannot be null");

        Value = value;
        Error = error;
    }

    public static ValueOrError<TValue> WithValue(TValue value) => new ValueOrError<TValue>(value: value);
    public static ValueOrError<TValue> WithError(Result err) => new ValueOrError<TValue>(error: err);
    public static ValueOrError<TValue> WithError(string msg) => new ValueOrError<TValue>(error: Result.Err(msg));
}
