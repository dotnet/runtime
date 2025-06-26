// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.Metadata;
using System.Text;

namespace System.Reflection.Metadata
{
    // Once Roslyn references System.Reflection.Metadata 10.0.0 or later,
    // we can replace this with TypeName.Unescape.
    internal static class TypeNameHelpers
    {
        private const char EscapeCharacter = '\\';

        /// <summary>
        /// Removes escape characters from the string (if there were any found).
        /// </summary>
        internal static string Unescape(string name)
        {
            int indexOfEscapeCharacter = name.IndexOf(EscapeCharacter);
            if (indexOfEscapeCharacter < 0)
            {
                return name;
            }

            return Unescape(name, indexOfEscapeCharacter);

            static string Unescape(string name, int indexOfEscapeCharacter)
            {
                // this code path is executed very rarely (IL Emit or pure IL with chars not allowed in C# or F#)
                var sb = new ValueStringBuilder(stackalloc char[64]);
                sb.EnsureCapacity(name.Length);
                sb.Append(name.AsSpan(0, indexOfEscapeCharacter));

                for (int i = indexOfEscapeCharacter; i < name.Length;)
                {
                    char c = name[i++];

                    if (c != EscapeCharacter)
                    {
                        sb.Append(c);
                    }
                    else if (i < name.Length && name[i] == EscapeCharacter) // escaped escape character ;)
                    {
                        sb.Append(c);
                        // Consume the escaped escape character, it's important for edge cases
                        // like escaped escape character followed by another escaped char (example: "\\\\\\+")
                        i++;
                    }
                }

                return sb.ToString();
            }
        }
    }
}
