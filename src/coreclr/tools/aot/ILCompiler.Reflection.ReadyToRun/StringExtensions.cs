// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Text;

namespace ILCompiler.Reflection.ReadyToRun
{
    public static class StringBuilderExtensions
    {
        /// <summary>
        /// Appends a C# string literal with the given value to the string builder.
        /// </summary>
        /// <remarks>
        /// This method closely follows the logic in <see cref="Microsoft.CodeAnalysis.CSharp.ObjectDisplay.FormatLiteral(string, ObjectDisplayOptions)"/>
        /// method in Roslyn .NET compiler; see its
        /// <a href="https://github.com/dotnet/roslyn/blob/master/src/Compilers/CSharp/Portable/SymbolDisplay/ObjectDisplay.cs">sources</a> for reference.
        /// </remarks>
        public static StringBuilder AppendEscapedString(this StringBuilder builder, string value, bool placeQuotes = true)
        {
            if (placeQuotes)
            {
                builder.Append('"');
            }

            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                UnicodeCategory category;

                // Fast check for printable ASCII characters
                if ((c <= 0x7e) && (c >= 0x20) || !NeedsEscaping(category = CharUnicodeInfo.GetUnicodeCategory(c)))
                {
                    if ((c == '"') || (c == '\\'))
                    {
                        builder.Append(@"\");
                    }
                    builder.Append(c);
                }
                else if (category == UnicodeCategory.Surrogate)
                {
                    // Check for a valid surrogate pair
                    category = CharUnicodeInfo.GetUnicodeCategory(value, i);
                    if (category == UnicodeCategory.Surrogate)
                    {
                        // Escape an unpaired surrogate
                        builder.Append(@"\u" + ((int)c).ToString("x4"));
                    }
                    else if (NeedsEscaping(category))
                    {
                        // A surrogate pair that needs to be escaped
                        int codePoint = char.ConvertToUtf32(value, i);
                        builder.Append(@"\U" + codePoint.ToString("x8"));
                        i++; // Skip the already-encoded second surrogate of the pair
                    }
                    else
                    {
                        // Copy a printable surrogate pair
                        builder.Append(c);
                        builder.Append(value[++i]);
                    }
                }
                else
                {
                    string escaped = null;
                    switch(c)
                    {
                        case '\0': escaped = @"\0"; break;
                        case '\a': escaped = @"\a"; break;
                        case '\b': escaped = @"\b"; break;
                        case '\f': escaped = @"\f"; break;
                        case '\n': escaped = @"\n"; break;
                        case '\r': escaped = @"\r"; break;
                        case '\t': escaped = @"\t"; break;
                        case '\v': escaped = @"\v"; break;
                        default :
                            escaped = @"\u" + ((int)c).ToString("x4"); break;
                    };
                    builder.Append(escaped);
                }
            }
            if (placeQuotes)
            {
                builder.Append('"');
            }
            return builder;
        }

        /// <summary>
        /// Determines whether characters of the given <see cref="UnicodeCategory"/> will be represented with escape sequences.
        /// </summary>
        private static bool NeedsEscaping(UnicodeCategory category)
        {
            switch (category)
            {
                case UnicodeCategory.LineSeparator:
                case UnicodeCategory.ParagraphSeparator:
                case UnicodeCategory.Control:
                case UnicodeCategory.Surrogate:
                case UnicodeCategory.OtherNotAssigned:
                    return true;
                default:
                    return false;
            }
        }
    }

    public static class StringExtensions
    {
        /// <summary>
        /// Returns a C# string literal with the given value.
        /// </summary>
        public static string ToEscapedString(this string value, bool placeQuotes = true)
        {
            return new StringBuilder(value.Length + 16).AppendEscapedString(value, placeQuotes).ToString();
        }
    }
}
