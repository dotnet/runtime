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
using Antlr4.Runtime.Tree;

namespace ILAssembler
{
#pragma warning disable CA1822 // Mark members as static
    internal sealed partial class GrammarActions : ICILVisitor<GrammarResult>
    {
        GrammarResult ICILVisitor<GrammarResult>.VisitCompQstring(CILParser.CompQstringContext context)
        {
            return VisitCompQstring(context);
        }

        private static GrammarResult.String VisitCompQstring(CILParser.CompQstringContext context)
        {
            StringBuilder builder = new();
            foreach (var item in context.QSTRING())
            {
                builder.Append(StringHelpers.ParseQuotedString(item.Symbol.Text));
            }
            return new(builder.ToString());
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitDottedName(CILParser.DottedNameContext context)
        {
            return VisitDottedName(context);
        }

        public static GrammarResult.String VisitDottedName(CILParser.DottedNameContext context)
        {
            if (context.DOTTEDNAME() is not null)
            {
                return new(context.GetText());
            }

            return new(string.Join(
                ".",
                context.dottedNamePart().Select(part =>
                    part.SQSTRING() is null
                        ? part.GetText()
                        : StringHelpers.ParseQuotedString(part.GetText()))));
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitDottedNamePart(CILParser.DottedNamePartContext context) => throw new UnreachableException();

        GrammarResult ICILVisitor<GrammarResult>.VisitFloat64(CILParser.Float64Context context) => VisitFloat64(context);
        public GrammarResult.Literal<double> VisitFloat64(CILParser.Float64Context context)
        {
            if (context.FLOAT64() is ITerminalNode float64)
            {
                string text = float64.Symbol.Text;
                bool neg = text.StartsWith('-');
                if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double result))
                {
                    result = neg ? double.MaxValue : double.MinValue;
                }
                return new(result);
            }
            else if (context.int32() is CILParser.Int32Context int32)
            {
                IToken node = int32.INT32().Symbol;
                if (!ParseIntegerValue(node.Text.AsSpan(), out long intValue))
                {
                    _diagnostics.Add(new Diagnostic(
                        DiagnosticIds.LiteralOutOfRange,
                        DiagnosticSeverity.Error,
                        string.Format(DiagnosticMessageTemplates.LiteralOutOfRange, node.Text),
                        Location.From(node, _documents)));
                    intValue = 0;
                }

                if (context.FLOAT32() is not null)
                {
                    // FLOAT32 '(' int32 ')' — hex bits reinterpreted as float32
                    return new(BitConverter.Int32BitsToSingle((int)intValue));
                }
                // int32 or int32 '.' — plain integer or trailing-dot float
                return new((double)intValue);
            }
            else if (context.int64() is CILParser.Int64Context int64)
            {
                // FLOAT64_ '(' int64 ')' — hex bits reinterpreted as float64
                long value = VisitInt64(int64).Value;
                return new(BitConverter.Int64BitsToDouble(value));
            }
            throw new UnreachableException();
        }
        GrammarResult ICILVisitor<GrammarResult>.VisitId(CILParser.IdContext context) => VisitId(context);
        public static GrammarResult.String VisitId(CILParser.IdContext context)
        {
            string text = context.GetText();
            if (context.SQSTRING() is not null && text.Length >= 2 && text[0] == '\'')
            {
                text = text.Substring(1, text.Length - 2);
            }
            return new GrammarResult.String(text);
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
                // Try parsing as unsigned — handles values like:
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

        GrammarResult ICILVisitor<GrammarResult>.VisitInt32(CILParser.Int32Context context)
        {
            return VisitInt32(context);
        }

        public GrammarResult.Literal<int> VisitInt32(CILParser.Int32Context context)
        {
            IToken node = context.INT32().Symbol;

            ReadOnlySpan<char> value = node.Text.AsSpan();

            if (!ParseIntegerValue(value, out long num))
            {
                _diagnostics.Add(new Diagnostic(
                    DiagnosticIds.LiteralOutOfRange,
                    DiagnosticSeverity.Error,
                    string.Format(DiagnosticMessageTemplates.LiteralOutOfRange, node.Text),
                    Location.From(node, _documents)));
                return new GrammarResult.Literal<int>(0);
            }

            return new GrammarResult.Literal<int>((int)num);
        }


        GrammarResult ICILVisitor<GrammarResult>.VisitInt64(CILParser.Int64Context context)
        {
            return VisitInt64(context);
        }

        public GrammarResult.Literal<long> VisitInt64(CILParser.Int64Context context)
        {
            IToken node = context.GetChild<ITerminalNode>(0).Symbol;

            ReadOnlySpan<char> value = node.Text.AsSpan();

            if (!ParseIntegerValue(value, out long num))
            {
                _diagnostics.Add(new Diagnostic(
                    DiagnosticIds.LiteralOutOfRange,
                    DiagnosticSeverity.Error,
                    string.Format(DiagnosticMessageTemplates.LiteralOutOfRange, node.Text),
                    Location.From(node, _documents)));
                return new GrammarResult.Literal<long>(0);
            }

            return new GrammarResult.Literal<long>(num);
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitIntOrWildcard(CILParser.IntOrWildcardContext context) => VisitIntOrWildcard(context);
        public GrammarResult.Literal<int?> VisitIntOrWildcard(CILParser.IntOrWildcardContext context) => context.int32() is {} int32 ? new(VisitInt32(int32).Value) : new(null);

        GrammarResult ICILVisitor<GrammarResult>.VisitSlashedName(CILParser.SlashedNameContext context)
        {
            return VisitSlashedName(context);
        }

        public static GrammarResult.Literal<TypeName> VisitSlashedName(CILParser.SlashedNameContext context)
        {
            TypeName? currentTypeName = null;
            foreach (var item in context.dottedName())
            {
                currentTypeName = new TypeName(currentTypeName, VisitDottedName(item).Value);
            }
            // We'll always have at least one dottedName, so the value here will be non-null
            return new(currentTypeName!);
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitTruefalse(CILParser.TruefalseContext context) => VisitTruefalse(context);

        public static GrammarResult.Literal<bool> VisitTruefalse(CILParser.TruefalseContext context)
        {
            return new(bool.Parse(context.GetText()));
        }

    }
}
