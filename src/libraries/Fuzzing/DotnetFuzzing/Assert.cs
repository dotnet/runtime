// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace DotnetFuzzing;

internal static class Assert
{
    // Feel free to add any other helpers you need.

    public static void Equal<T>(T expected, T actual)
        where T : IEquatable<T>
    {
        if (!expected.Equals(actual))
        {
            Throw(expected, actual);
        }

        static void Throw(T expected, T actual) =>
            throw new AssertException($"Expected={expected} Actual={actual}");
    }

    private sealed class AssertException : Exception
    {
        public AssertException(string message) : base(message) { }
    }
}
