// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.Collections.Generic;

/// <summary>Provides downlevel polyfills for instance methods on <see cref="Stack{T}"/>.</summary>
internal static class StackPolyfills
{
    public static bool TryPeek<T>(this Stack<T> stack, [MaybeNullWhen(false)] out T result)
    {
        if (stack.Count > 0)
        {
            result = stack.Peek();
            return true;
        }

        result = default;
        return false;
    }

    public static bool TryPop<T>(this Stack<T> stack, [MaybeNullWhen(false)] out T result)
    {
        if (stack.Count > 0)
        {
            result = stack.Pop();
            return true;
        }

        result = default;
        return false;
    }
}
