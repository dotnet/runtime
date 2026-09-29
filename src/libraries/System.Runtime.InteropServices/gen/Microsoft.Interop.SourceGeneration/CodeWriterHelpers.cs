// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Globalization;
using System.Text;

namespace Microsoft.Interop;

/// <summary>Formats the names and literals used in generated C# source.</summary>
public static class CodeWriterHelpers
{
    /// <summary>Formats a quoted C# string literal.</summary>
    /// <param name="value">The value of the literal.</param>
    /// <returns>The escaped, quoted literal.</returns>
    public static string StringLiteral(string value)
    {
        if (value is null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        int firstEscape = 0;
        while (firstEscape < value.Length && value[firstEscape] is >= ' ' and <= '~' and not ('"' or '\\'))
        {
            firstEscape++;
        }
        if (firstEscape == value.Length)
        {
            return "\"" + value + "\"";
        }

        var builder = new StringBuilder(checked(value.Length + 2));
        builder.Append('"');
        builder.Append(value, 0, firstEscape);
        for (int i = firstEscape; i < value.Length; i++)
        {
            string? escape = value[i] switch
            {
                '\0' => "\\0",
                '\a' => "\\a",
                '\b' => "\\b",
                '\f' => "\\f",
                '\n' => "\\n",
                '\r' => "\\r",
                '\t' => "\\t",
                '\v' => "\\v",
                '"' => "\\\"",
                '\\' => "\\\\",
                _ => null
            };
            if (escape is not null)
            {
                builder.Append(escape);
                continue;
            }

            int characterLength = char.IsSurrogatePair(value, i) ? 2 : 1;
            // Classify complete surrogate pairs; format and private-use characters remain literal.
            if (CharUnicodeInfo.GetUnicodeCategory(value, i) is UnicodeCategory.Control
                or UnicodeCategory.Surrogate
                or UnicodeCategory.OtherNotAssigned
                or UnicodeCategory.LineSeparator
                or UnicodeCategory.ParagraphSeparator)
            {
                if (characterLength == 2)
                {
                    builder.Append("\\U");
                    builder.Append(char.ConvertToUtf32(value, i).ToString("x8", CultureInfo.InvariantCulture));
                }
                else
                {
                    builder.Append("\\u");
                    builder.Append(((int)value[i]).ToString("x4", CultureInfo.InvariantCulture));
                }
            }
            else
            {
                builder.Append(value, i, characterLength);
            }

            i += characterLength - 1;
        }

        builder.Append('"');
        return builder.ToString();
    }

    /// <summary>Escapes an identifier using the caller's keyword classification.</summary>
    /// <param name="identifier">The identifier to escape.</param>
    /// <param name="isKeyword"><see langword="true"/> to escape the identifier; otherwise, <see langword="false"/>.</param>
    /// <returns>The identifier as C# source text.</returns>
    public static string EscapeIdentifier(string identifier, bool isKeyword)
    {
        return isKeyword ? "@" + identifier : identifier;
    }

    /// <summary>Adds a modifier before any trailing ref and partial keywords.</summary>
    /// <param name="modifiers">The existing modifiers.</param>
    /// <param name="modifier">The modifier to add.</param>
    /// <returns>The updated modifiers.</returns>
    public static ImmutableArray<string> AddModifier(ImmutableArray<string> modifiers, string modifier)
    {
        if (modifiers.Contains(modifier))
        {
            return modifiers;
        }

        int partialIndex = modifiers.IndexOf("partial");
        int refIndex = modifiers.IndexOf("ref");
        int index = (partialIndex, refIndex) switch
        {
            (-1, -1) => modifiers.Length,
            (-1, _) => refIndex,
            (_, -1) => partialIndex,
            _ => Math.Min(partialIndex, refIndex)
        };
        return modifiers.Insert(index, modifier);
    }
}
