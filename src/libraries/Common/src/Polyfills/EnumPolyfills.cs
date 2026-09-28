// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System;

/// <summary>Provides downlevel polyfills for static methods on <see cref="Enum"/>.</summary>
internal static class EnumPolyfills
{
    extension(Enum)
    {
        public static TEnum Parse<TEnum>(string value, bool ignoreCase) where TEnum : struct, Enum =>
            (TEnum)Enum.Parse(typeof(TEnum), value, ignoreCase);
    }
}
