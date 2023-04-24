// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Linq
{
    internal static class Error
    {
        internal static Exception ArgumentNotIEnumerableGeneric(string message) =>
            new ArgumentException(Strings.ArgumentNotIEnumerableGeneric(message));

        internal static Exception ArgumentNotValid(string message) =>
            new ArgumentException(Strings.ArgumentNotValid(message));

        internal static Exception ArgumentOutOfRange(string message) =>
            new ArgumentOutOfRangeException(message);

        internal static Exception NoMethodOnType(string name, object type) =>
            new InvalidOperationException(Strings.NoMethodOnType(name, type));

        internal static Exception NoMethodOnTypeMatchingArguments(string name, object type) =>
            new InvalidOperationException(Strings.NoMethodOnTypeMatchingArguments(name, type));

        internal static Exception EnumeratingNullEnumerableExpression() =>
            new InvalidOperationException(Strings.EnumeratingNullEnumerableExpression());
    }
}
