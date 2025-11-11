// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.StubHelpers
{
    internal static partial class StubHelpers
    {
        internal static object? AsyncCallContinuation() => throw new Exception(); // Unconditionally expanded intrinsic
    }
}
