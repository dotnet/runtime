// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System;

/// <summary>Provides downlevel polyfills for static methods on <see cref="Enum"/>.</summary>
internal static class EnumPolyfills
{
    extension(Enum)
    {
        public static string[] GetNames<TEnum>() where TEnum : struct, Enum =>
            Enum.GetNames(typeof(TEnum));

        public static TEnum[] GetValues<TEnum>() where TEnum : struct, Enum =>
            (TEnum[])Enum.GetValues(typeof(TEnum));

        public static TEnum Parse<TEnum>(string value, bool ignoreCase) where TEnum : struct, Enum =>
            (TEnum)Enum.Parse(typeof(TEnum), value, ignoreCase);
    }
}
