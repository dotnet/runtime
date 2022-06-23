// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;

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
            {
                return identifier.ToUpperInvariant();
            }
            else if (char.IsLower(identifier[0]))
            {
                return string.Create(identifier.Length, identifier, static (buffer, identifier) =>
                {
                    identifier.CopyTo(buffer);
                    buffer[0] = char.ToUpperInvariant(buffer[0]); // convert only first char to uppercase; leave all else as-is
                });
            }
            else
            {
                return identifier;
            }
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static string MakeCamel(string identifier)
        {
            identifier = MakeValid(identifier);
            if (identifier.Length <= 2)
            {
                return identifier.ToLowerInvariant();
            }
            else if (char.IsUpper(identifier[0]))
            {
                return string.Create(identifier.Length, identifier, static (buffer, identifier) =>
                {
                    identifier.CopyTo(buffer);
                    buffer[0] = char.ToLowerInvariant(buffer[0]); // convert only first char to lowercase; leave all else as-is
                });
            }
            else
            {
                return identifier;
            }
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static string MakeValid(string identifier)
        {
            var builder = new ValueStringBuilder(stackalloc char[MaxIdentifierLength]);
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
            if (!IsValid(c)) throw new ArgumentException(SR.Format(SR.XmlInternalErrorDetails, "Invalid identifier character " + ((short)c).ToString(CultureInfo.InvariantCulture)), nameof(c));
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
                    throw new ArgumentException(SR.Format(SR.XmlInternalErrorDetails, "Unhandled category " + uc), nameof(c));
#else
                return false;
#endif
            }
            return true;
        }

        internal static void CheckValidIdentifier([NotNull] string? ident)
        {
            if (!CSharpHelpers.IsValidLanguageIndependentIdentifier(ident))
                throw new ArgumentException(SR.Format(SR.XmlInvalidIdentifier, ident), nameof(ident));

            Debug.Assert(ident != null);
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
                sb.Append('.');
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
                sb.Append('<');
                int arguments = int.Parse(name.AsSpan(nameEnd + 1), provider: CultureInfo.InvariantCulture) + index;
                for (; index < arguments; index++)
                {
                    sb.Append(GetCSharpName(parameters[index]));
                    if (index < arguments - 1)
                    {
                        sb.Append(',');
                    }
                }
                sb.Append('>');
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
                t = t.GetElementType()!;
                rank++;
            }

            StringBuilder sb = new StringBuilder();
            sb.Append("global::");
            string? ns = t.Namespace;
            if (ns != null && ns.Length > 0)
            {
                string[] parts = ns.Split('.');
                for (int i = 0; i < parts.Length; i++)
                {
                    EscapeKeywords(parts[i], sb);
                    sb.Append('.');
                }
            }

            Type[] arguments = t.IsGenericType || t.ContainsGenericParameters ? t.GetGenericArguments() : Type.EmptyTypes;
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

        private static readonly char[] s_identifierSeparators = new char[] { '.', ',', '<', '>' };

        [return: NotNullIfNotNull("identifier")]
        private static string? EscapeKeywords(string? identifier)
        {
            if (identifier == null || identifier.Length == 0) return identifier;
            string originalIdentifier = identifier;
            string[] names = identifier.Split(s_identifierSeparators);
            StringBuilder sb = new StringBuilder();
            int separator = -1;
            for (int i = 0; i < names.Length; i++)
            {
                if (separator >= 0)
                {
                    sb.Append(originalIdentifier[separator]);
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
