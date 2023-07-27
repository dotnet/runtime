// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Collections.Generic;

namespace System.Text.Json.SourceGeneration
{
    internal static class SourceGeneratorHelpers
    {
        private static readonly char[] s_enumSeparator = new char[] { ',' };

        public static string FormatEnumLiteral<TEnum>(string fullyQualifiedName, TEnum value) where TEnum : struct, Enum
        {
            IEnumerable<string> values = value.ToString().Split(s_enumSeparator, StringSplitOptions.RemoveEmptyEntries)
                .Select(name => name.Trim())
                .Select(name =>
                    int.TryParse(name, out _)
                    ? $"({fullyQualifiedName})({name})"
                    : $"{fullyQualifiedName}.{name}");

            return string.Join(" | ", values);
        }
    }
}
