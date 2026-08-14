// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using Antlr4.Runtime;
using Antlr4.Runtime.Misc;

namespace ILAssembler
{
#pragma warning disable CA1822 // Mark members as static
    internal sealed partial class GrammarActions
    {
        internal void AddComposedStringPart(StringBuilder builder, IToken token)
            => builder.Append(StringHelpers.ParseQuotedString(token.Text));

        internal string EndComposedString(StringBuilder builder)
            => builder.ToString();

        internal void AddDottedNamePart(CILParser.DottedNameBuilder builder, string value)
        {
            if (builder.HasPart)
            {
                builder.Value.Append('.');
            }

            builder.Value.Append(value);
            builder.HasPart = true;
        }

        internal void AddDottedNameToken(CILParser.DottedNameBuilder builder, IToken token)
            => AddDottedNamePart(builder, ParseIdentifier(token));

        internal string EndDottedName(CILParser.DottedNameBuilder builder)
            => builder.Value.ToString();

        internal string ParseDottedNamePart(IToken token)
            => token.Text.Length >= 2 && token.Text[0] == '\''
                ? StringHelpers.ParseQuotedString(token.Text)
                : token.Text;

        internal double ParseFloatingLiteral(IToken token)
        {
            string text = token.Text;
            bool neg = text.StartsWith('-');
            if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double result))
            {
                result = neg ? double.MaxValue : double.MinValue;
            }

            return result;
        }

        internal double ParseFloatingInteger(IToken token)
        {
            if (!ParseIntegerValue(token.Text.AsSpan(), out long value))
            {
                ReportLiteralOutOfRange(token);
                value = 0;
            }

            return value;
        }

        internal double ParseFloat32Bits(IToken token)
            => BitConverter.Int32BitsToSingle(ParseInt32(token));

        internal double ParseFloat64Bits(IToken token)
            => BitConverter.Int64BitsToDouble(ParseInt64(token));

        internal static string GetIdentifier(CILParser.IdContext context)
            => ParseIdentifier(context.Start);

        private static string ParseIdentifier(IToken token)
        {
            string text = token.Text;
            return text.Length >= 2 && text[0] == '\''
                ? text.Substring(1, text.Length - 2)
                : text;
        }

        private static bool ParseIntegerValue(ReadOnlySpan<char> value, out long result)
        {
            NumberStyles parseStyle = NumberStyles.None;
            bool negate = false;
            if (value.StartsWith("-".AsSpan()))
            {
                negate = true;
                value = value.Slice(1);
            }

            if (value.StartsWith("0x".AsSpan()))
            {
                parseStyle = NumberStyles.AllowHexSpecifier;
                value = value.Slice(2);
            }
            else if (value.StartsWith("0".AsSpan()))
            {
                // Octal support isn't built-in, so we'll do it manually.
                result = 0;
                for (int i = 0; i < value.Length; i++, result *= 8)
                {
                    int digitValue = value[i] - '0';
                    if (digitValue < 0 || digitValue > 7)
                    {
                        // COMPAT: native ilasm skips invalid digits silently
                        continue;
                    }
                    result += digitValue;
                }
                if (negate) result = -result;
                return true;
            }

            bool success = long.TryParse(value.ToString(), parseStyle, CultureInfo.InvariantCulture, out result);
            if (!success)
            {
                // Try parsing as unsigned to handle values like:
                // - Decimal overflow with negation: 9223372036854775808 (= -Int64.MinValue)
                // - Large unsigned decimal: 18444492274432737280
                if (ulong.TryParse(value.ToString(), parseStyle, CultureInfo.InvariantCulture, out ulong uresult))
                {
                    result = unchecked((long)uresult);
                    if (negate) result = unchecked(-result);
                    return true;
                }
                // Handle oversized hex values (>64 bits) by truncating to low 64 bits,
                // matching native ilasm behavior for values like 0x94188556b24089e8b90c9c61f9f3088
                if (parseStyle == NumberStyles.AllowHexSpecifier && value.Length > 16)
                {
                    var truncated = value.Slice(value.Length - 16);
                    if (ulong.TryParse(truncated.ToString(), parseStyle, CultureInfo.InvariantCulture, out uresult))
                    {
                        result = unchecked((long)uresult);
                        if (negate) result = unchecked(-result);
                        return true;
                    }
                }
                return false;
            }

            if (negate) result = -result;
            return true;
        }

        internal int ParseInt32(IToken token)
        {
            ReadOnlySpan<char> value = token.Text.AsSpan();
            if (!ParseIntegerValue(value, out long num))
            {
                ReportLiteralOutOfRange(token);
                return 0;
            }

            return (int)num;
        }


        private long ParseInt64(IToken token)
        {
            ReadOnlySpan<char> value = token.Text.AsSpan();
            if (!ParseIntegerValue(value, out long num))
            {
                ReportLiteralOutOfRange(token);
                return 0;
            }

            return num;
        }

        private void ReportLiteralOutOfRange(IToken token)
        {
            _diagnostics.Add(new Diagnostic(
                DiagnosticIds.LiteralOutOfRange,
                DiagnosticSeverity.Error,
                string.Format(DiagnosticMessageTemplates.LiteralOutOfRange, token.Text),
                Location.From(token, _documents)));
        }

        internal bool ParseBoolean(IToken token) => bool.Parse(token.Text);

    }
}
