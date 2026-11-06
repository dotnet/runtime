// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System;

internal static class MathCompatibilityPolyfills
{
    extension(Math)
    {
        public static int Clamp(int value, int min, int max)
        {
            if (min > max)
            {
                throw new ArgumentException($"{min} cannot be greater than {max}.");
            }

            return value < min ? min : value > max ? max : value;
        }
    }
}
