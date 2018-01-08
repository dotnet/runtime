// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text;
using System.Collections;
using System.IO;
using System.Globalization;
using System.Diagnostics;
using System.Reflection;

namespace System.Xml.Serialization
{
    ///<internalonly/>
    /// <devdoc>
    ///    <para>[To be supplied.]</para>
    /// </devdoc>
    public class CodeIdentifier
    {
        internal const int MaxIdentifierLength = 511;

        [Obsolete("This class should never get constructed as it contains only static methods.")]
        public CodeIdentifier()
        {
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static string MakePascal(string identifier)
        {
            identifier = MakeValid(identifier);
            if (identifier.Length <= 2)
                return CultureInfo.InvariantCulture.TextInfo.ToUpper(identifier);
            else if (char.IsLower(identifier[0]))
                return char.ToUpperInvariant(identifier[0]) + identifier.Substring(1);
            else
                return identifier;
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static string MakeCamel(string identifier)
        {
            identifier = MakeValid(identifier);
            if (identifier.Length <= 2)
                return CultureInfo.InvariantCulture.TextInfo.ToLower(identifier);
            else if (char.IsUpper(identifier[0]))
                return char.ToLower(identifier[0]) + identifier.Substring(1);
            else
                return identifier;
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static string MakeValid(string identifier)
        {
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < identifier.Length && builder.Length < MaxIdentifierLength; i++)
            {
                char c = identifier[i];
                if (IsValid(c))
                {
                    if (builder.Length == 0 && !IsValidStart(c))
                    {
                        builder.Append("Item");
                    }
                    builder.Append(c);
                }
            }
            if (builder.Length == 0) return "Item";
            return builder.ToString();
        }

        internal static string MakeValidInternal(string identifier)
        {
            if (identifier.Length > 30)
            {
                return "Item";
            }
            return MakeValid(identifier);
        }

        private static bool IsValidStart(char c)
        {
            // the given char is already a valid name character
#if DEBUG
            // use exception in the place of Debug.Assert to avoid throwing asserts from a server process such as aspnet_ewp.exe
            if (!IsValid(c)) throw new ArgumentException(SR.Format(SR.XmlInternalErrorDetails, "Invalid identifier character " + ((Int16)c).ToString(CultureInfo.InvariantCulture)), "c");
#endif

            // First char cannot be a number
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.DecimalDigitNumber)
                return false;
            return true;
        }

        private static bool IsValid(char c)
        {
            UnicodeCategory uc = CharUnicodeInfo.GetUnicodeCategory(c);
            // each char must be Lu, Ll, Lt, Lm, Lo, Nd, Mn, Mc, Pc
            // 
            switch (uc)
            {
                case UnicodeCategory.UppercaseLetter:        // Lu
                case UnicodeCategory.LowercaseLetter:        // Ll
                case UnicodeCategory.TitlecaseLetter:        // Lt
                case UnicodeCategory.ModifierLetter:         // Lm
                case UnicodeCategory.OtherLetter:            // Lo
                case UnicodeCategory.DecimalDigitNumber:     // Nd
                case UnicodeCategory.NonSpacingMark:         // Mn
                case UnicodeCategory.SpacingCombiningMark:   // Mc
                case UnicodeCategory.ConnectorPunctuation:   // Pc
                    break;
                case UnicodeCategory.LetterNumber:
                case UnicodeCategory.OtherNumber:
                case UnicodeCategory.EnclosingMark:
                case UnicodeCategory.SpaceSeparator:
                case UnicodeCategory.LineSeparator:
                case UnicodeCategory.ParagraphSeparator:
                case UnicodeCategory.Control:
                case UnicodeCategory.Format:
                case UnicodeCategory.Surrogate:
                case UnicodeCategory.PrivateUse:
                case UnicodeCategory.DashPunctuation:
                case UnicodeCategory.OpenPunctuation:
                case UnicodeCategory.ClosePunctuation:
                case UnicodeCategory.InitialQuotePunctuation:
                case UnicodeCategory.FinalQuotePunctuation:
                case UnicodeCategory.OtherPunctuation:
                case UnicodeCategory.MathSymbol:
                case UnicodeCategory.CurrencySymbol:
                case UnicodeCategory.ModifierSymbol:
                case UnicodeCategory.OtherSymbol:
                case UnicodeCategory.OtherNotAssigned:
                    return false;
                default:
#if DEBUG
                    // use exception in the place of Debug.Assert to avoid throwing asserts from a server process such as aspnet_ewp.exe
                    throw new ArgumentException(SR.Format(SR.XmlInternalErrorDetails, "Unhandled category " + uc), "c");
#else
                return false;
#endif
            }
            return true;
        }

        internal static void CheckValidIdentifier(string ident)
        {
            if (!CSharpHelpers.IsValidLanguageIndependentIdentifier(ident))
                throw new ArgumentException(SR.Format(SR.XmlInvalidIdentifier, ident), nameof(ident));
        }

        internal static string GetCSharpName(string name)
        {
            return EscapeKeywords(name.Replace('+', '.'));
        }

        private static int GetCSharpName(Type t, Type[] parameters, int index, StringBuilder sb)
        {
            if (t.DeclaringType != null && t.DeclaringType != t)
            {
                index = GetCSharpName(t.DeclaringType, parameters, index, sb);
                sb.Append(".");
            }
            string name = t.Name;
            int nameEnd = name.IndexOf('`');
            if (nameEnd < 0)
            {
                nameEnd = name.IndexOf('!');
            }
            if (nameEnd > 0)
            {
                EscapeKeywords(name.Substring(0, nameEnd), sb);
                sb.Append("<");
                int arguments = Int32.Parse(name.Substring(nameEnd + 1), CultureInfo.InvariantCulture) + index;
                for (; index < arguments; index++)
                {
                    sb.Append(GetCSharpName(parameters[index]));
                    if (index < arguments - 1)
                    {
                        sb.Append(",");
                    }
                }
                sb.Append(">");
            }
            else
            {
                EscapeKeywords(name, sb);
            }
            return index;
        }

        internal static string GetCSharpName(Type t)
        {
            int rank = 0;
            while (t.IsArray)
            {
                t = t.GetElementType();
                rank++;
            }
            StringBuilder sb = new StringBuilder();
            sb.Append("global::");
            string ns = t.Namespace;
            if (ns != null && ns.Length > 0)
            {
                string[] parts = ns.Split(new char[] { '.' });
                for (int i = 0; i < parts.Length; i++)
                {
                    EscapeKeywords(parts[i], sb);
                    sb.Append(".");
                }
            }

            Type[] arguments = t.IsGenericType || t.ContainsGenericParameters ? t.GetGenericArguments() : Array.Empty<Type>();
            GetCSharpName(t, arguments, 0, sb);
            for (int i = 0; i < rank; i++)
            {
                sb.Append("[]");
            }
            return sb.ToString();
        }

        /*
        internal static string GetTypeName(string name, CodeDomProvider codeProvider) {
            return codeProvider.GetTypeOutput(new CodeTypeReference(name));
        }
        */

        private static void EscapeKeywords(string identifier, StringBuilder sb)
        {
            if (identifier == null || identifier.Length == 0)
                return;
            int arrayCount = 0;
            while (identifier.EndsWith("[]", StringComparison.Ordinal))
            {
                arrayCount++;
                identifier = identifier.Substring(0, identifier.Length - 2);
            }
            if (identifier.Length > 0)
            {
                CheckValidIdentifier(identifier);
                identifier = CSharpHelpers.CreateEscapedIdentifier(identifier);
                sb.Append(identifier);
            }
            for (int i = 0; i < arrayCount; i++)
            {
                sb.Append("[]");
            }
        }

        private static string EscapeKeywords(string identifier)
        {
            if (identifier == null || identifier.Length == 0) return identifier;
            string originalIdentifier = identifier;
            string[] names = identifier.Split(new char[] { '.', ',', '<', '>' });
            StringBuilder sb = new StringBuilder();
            int separator = -1;
            for (int i = 0; i < names.Length; i++)
            {
                if (separator >= 0)
                {
                    sb.Append(originalIdentifier.Substring(separator, 1));
                }
                separator++;
                separator += names[i].Length;
                string escapedName = names[i].Trim();
                EscapeKeywords(escapedName, sb);
            }
            if (sb.Length != originalIdentifier.Length)
                return sb.ToString();
            return originalIdentifier;
        }
    }
}
