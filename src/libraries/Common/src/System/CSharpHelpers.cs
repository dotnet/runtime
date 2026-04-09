// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;

namespace System
{
    /// <devdoc>
    ///    <para>Provides a base class for code generators.</para>
    /// </devdoc>
    internal abstract class CSharpHelpers
    {
        private static readonly HashSet<string> s_fixedStringLookup = new HashSet<string>()
        {
            "as",
            "do",
            "if",
            "in",
            "is",
            "for",
            "int",
            "new",
            "out",
            "ref",
            "try",
            "base",
            "bool",
            "byte",
            "case",
            "char",
            "else",
            "enum",
            "goto",
            "lock",
            "long",
            "null",
            "this",
            "true",
            "uint",
            "void",
            "break",
            "catch",
            "class",
            "const",
            "event",
            "false",
            "fixed",
            "float",
            "sbyte",
            "short",
            "throw",
            "ulong",
            "using",
            "where",
            "while",
            "yield",
            "double",
            "extern",
            "object",
            "params",
            "public",
            "return",
            "sealed",
            "sizeof",
            "static",
            "string",
            "struct",
            "switch",
            "typeof",
            "unsafe",
            "ushort",
            "checked",
            "decimal",
            "default",
            "finally",
            "foreach",
            "partial",
            "private",
            "virtual",
            "abstract",
            "continue",
            "delegate",
            "explicit",
            "implicit",
            "internal",
            "operator",
            "override",
            "readonly",
            "volatile",
            "__arglist",
            "__makeref",
            "__reftype",
            "interface",
            "namespace",
            "protected",
            "unchecked",
            "__refvalue",
            "stackalloc",
        };

        public static string CreateEscapedIdentifier(string name)
        {
            // Any identifier started with two consecutive underscores are
            // reserved by CSharp.
            if (IsKeyword(name) || IsPrefixTwoUnderscore(name))
            {
                return "@" + name;
            }
            return name;
        }

        /// <devdoc>
        ///    <para>
        ///       Gets a value indicating whether the specified value is a valid language
        ///       independent identifier.
        ///    </para>
        /// </devdoc>
        public static bool IsValidLanguageIndependentIdentifier(string? value)
        {
            return IsValidTypeNameOrIdentifier(value, false);
        }

        internal static bool IsKeyword(string value)
        {
            return s_fixedStringLookup.Contains(value);
        }

        internal static bool IsPrefixTwoUnderscore(string value)
        {
            if (value.Length < 3)
            {
                return false;
            }
            else
            {
                return ((value[0] == '_') && (value[1] == '_') && (value[2] != '_'));
            }
        }

        internal static bool IsValidTypeNameOrIdentifier(string? value, bool isTypeName)
        {
            bool nextMustBeStartChar = true;

            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            // each char must be Lu, Ll, Lt, Lm, Lo, Nd, Mn, Mc, Pc
            //
            for (int i = 0; i < value.Length; i++)
            {
                char ch = value[i];
                UnicodeCategory uc = CharUnicodeInfo.GetUnicodeCategory(ch);
                switch (uc)
                {
                    case UnicodeCategory.UppercaseLetter:        // Lu
                    case UnicodeCategory.LowercaseLetter:        // Ll
                    case UnicodeCategory.TitlecaseLetter:        // Lt
                    case UnicodeCategory.ModifierLetter:         // Lm
                    case UnicodeCategory.LetterNumber:           // Lm
                    case UnicodeCategory.OtherLetter:            // Lo
                        nextMustBeStartChar = false;
                        break;

                    case UnicodeCategory.NonSpacingMark:         // Mn
                    case UnicodeCategory.SpacingCombiningMark:   // Mc
                    case UnicodeCategory.ConnectorPunctuation:   // Pc
                    case UnicodeCategory.DecimalDigitNumber:     // Nd
                        // Underscore is a valid starting character, even though it is a ConnectorPunctuation.
                        if (nextMustBeStartChar && ch != '_')
                            return false;

                        nextMustBeStartChar = false;
                        break;
                    default:
                        // We only check the special Type chars for type names.
                        if (isTypeName && IsSpecialTypeChar(ch, ref nextMustBeStartChar))
                        {
                            break;
                        }

                        return false;
                }
            }

            return true;
        }

        // This can be a special character like a separator that shows up in a type name
        // This is an odd set of characters.  Some come from characters that are allowed by C++, like < and >.
        // Others are characters that are specified in the type and assembly name grammar.
        internal static bool IsSpecialTypeChar(char ch, ref bool nextMustBeStartChar)
        {
            switch (ch)
            {
                case ':':
                case '.':
                case '$':
                case '+':
                case '<':
                case '>':
                case '-':
                case '[':
                case ']':
                case ',':
                case '&':
                case '*':
                    nextMustBeStartChar = true;
                    return true;

                case '`':
                    return true;
            }
            return false;
        }
    }
}
