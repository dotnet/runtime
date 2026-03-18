// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Collections.Immutable.Tests
{
    /// <summary>
    /// Defines a proxy class for accessing non-public binary tree implementation details.
    /// </summary>
    public sealed class BinaryTreeProxy(object underlyingValue, Type underlyingType)
    {
        public int Height => (int)GetProperty(nameof(Height));
        public bool IsEmpty => (bool)GetProperty(nameof(IsEmpty));
        public int Count => (int)GetProperty(nameof(Count));
        public BinaryTreeProxy? Left => GetProperty(nameof(Left)) is { } leftValue
            ? new(leftValue, underlyingType)
            : null;

        public BinaryTreeProxy? Right => GetProperty(nameof(Right)) is { } rightValue
            ? new(rightValue, underlyingType)
            : null;

        private object? GetProperty(string propertyName)
            => underlyingType.GetProperty(propertyName)!.GetValue(underlyingValue);
    }
}
