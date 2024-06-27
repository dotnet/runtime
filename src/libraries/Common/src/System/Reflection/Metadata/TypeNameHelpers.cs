// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.Metadata;
using System.Text;

namespace System.Reflection.Metadata
{
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

        internal static (string typeNamespace, string name) Split(string typeName)
        {
            string typeNamespace, name;

            // Matches algorithm from ns::FindSep in src\coreclr\utilcode\namespaceutil.cpp
            // This could result in the type name beginning with a '.' character.
            int separator = typeName.LastIndexOf('.');
            if (separator <= 0)
            {
                typeNamespace = "";
                name = typeName;
            }
            else
            {
                if (typeName[separator - 1] == '.')
                    separator--;
                typeNamespace = typeName.Substring(0, separator);
                name = typeName.Substring(separator + 1);
            }

            return (typeNamespace, name);
        }
    }
}
