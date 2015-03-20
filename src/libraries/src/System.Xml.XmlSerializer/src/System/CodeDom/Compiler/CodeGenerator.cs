// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//------------------------------------------------------------------------------
// </copyright>                                                                
//------------------------------------------------------------------------------

using System.Diagnostics;
using System;
using System.IO;
using System.Collections;
using System.Reflection;
using System.Globalization;
using System.CodeDom;
using System.Text;


namespace System.CodeDom.Compiler
{
    /// <devdoc>
    ///    <para>Provides a base class for code generators.</para>
    /// </devdoc>
    internal abstract class CodeGenerator
    {
        /// <devdoc>
        ///    <para>
        ///       Gets a value indicating whether the specified value is a valid language
        ///       independent identifier.
        ///    </para>
        /// </devdoc>
        public static bool IsValidLanguageIndependentIdentifier(string value)
        {
            return IsValidTypeNameOrIdentifier(value, false);
        }


        private static bool IsValidTypeNameOrIdentifier(string value, bool isTypeName)
        {
            bool nextMustBeStartChar = true;

            if (value.Length == 0)
                return false;

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
        // Others are characters that are specified in the type and assembly name grammer. 
        private static bool IsSpecialTypeChar(char ch, ref bool nextMustBeStartChar)
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
