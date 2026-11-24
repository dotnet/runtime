// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;

namespace System;

internal static partial class StringPolyfills
{
    extension(string)
    {
        public static string Create<TState>(int length, TState state, SpanAction<char, TState> action)
        {
            if (action is null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            if (length < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(length));
            }

            if (length == 0)
            {
                return string.Empty;
            }

            char[] chars = new char[length];
            action(chars, state);
            return new string(chars);
        }
    }
}
