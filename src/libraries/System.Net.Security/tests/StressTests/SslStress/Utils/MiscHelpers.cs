// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace SslStress.Utils
{
    public static class MiscHelpers
    {
        // help transform `(foo != null) ? Bar(foo) : null` expressions into `foo?.Select(Bar)`
        public static S Pipe<T, S>(this T value, Func<T, S> mapper) => mapper(value);
        public static void Pipe<T>(this T value, Action<T> body) => body(value);
    }
}
