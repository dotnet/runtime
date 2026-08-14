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
        private CILParser.CompQstringContext? _composedStringOwner;
        private StringBuilder? _composedStringAccumulator;
        private readonly Stack<DottedNameFrame> _dottedNameFrames = new();

        private sealed class DottedNameFrame
        {
            public DottedNameFrame(CILParser.DottedNameContext owner)
            {
                Owner = owner;
            }

            public CILParser.DottedNameContext Owner { get; }

            public string? FirstPart { get; set; }

            public StringBuilder? Builder { get; set; }
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitCompQstring(CILParser.CompQstringContext context)
        {
            return VisitCompQstring(context);
        }

        internal void BeginComposedString(CILParser.CompQstringContext context)
        {
            Debug.Assert(_composedStringOwner is null);
            Debug.Assert(_composedStringAccumulator is null);
            _composedStringOwner = context;
            _composedStringAccumulator = new StringBuilder();
        }

        internal void AddComposedStringPart(CILParser.CompQstringContext context, IToken token)
        {
            if (ReferenceEquals(_composedStringOwner, context) &&
                _composedStringAccumulator is { } accumulator)
            {
                accumulator.Append(StringHelpers.ParseQuotedString(token.Text));
            }
        }

        internal string EndComposedString(CILParser.CompQstringContext context)
        {
            if (!ReferenceEquals(_composedStringOwner, context))
            {
                return string.Empty;
            }

            string value = _composedStringAccumulator?.ToString() ?? string.Empty;
            _composedStringOwner = null;
            _composedStringAccumulator = null;

            return value;
        }

        private static GrammarResult.String VisitCompQstring(CILParser.CompQstringContext context)
            => new(context.Value ?? string.Empty);

        GrammarResult ICILVisitor<GrammarResult>.VisitDottedName(CILParser.DottedNameContext context)
        {
            return VisitDottedName(context);
        }

        internal void BeginDottedName(CILParser.DottedNameContext context)
            => _dottedNameFrames.Push(new(context));

        internal void AddDottedNamePart(CILParser.DottedNameContext context, string value)
        {
            Debug.Assert(_dottedNameFrames.Count > 0);
            DottedNameFrame frame = _dottedNameFrames.Peek();
            Debug.Assert(ReferenceEquals(frame.Owner, context));
            if (!ReferenceEquals(frame.Owner, context))
            {
                return;
            }

            if (frame.FirstPart is null)
            {
                frame.FirstPart = value;
                return;
            }

            frame.Builder ??= new StringBuilder(frame.FirstPart);
            frame.Builder.Append('.');
            frame.Builder.Append(value);
        }

        internal void AddDottedNameToken(CILParser.DottedNameContext context, IToken token)
            => AddDottedNamePart(context, ParseIdentifier(token));

        internal string EndDottedName(CILParser.DottedNameContext context)
        {
            Debug.Assert(_dottedNameFrames.Count > 0);
            DottedNameFrame frame = _dottedNameFrames.Pop();
            Debug.Assert(ReferenceEquals(frame.Owner, context));
            return ReferenceEquals(frame.Owner, context)
                ? frame.Builder?.ToString() ?? frame.FirstPart ?? string.Empty
                : string.Empty;
        }

        public static GrammarResult.String VisitDottedName(CILParser.DottedNameContext context)
            => new(context.Value ?? string.Empty);

        GrammarResult ICILVisitor<GrammarResult>.VisitDottedNamePart(CILParser.DottedNamePartContext context)
            => new GrammarResult.String(context.Value ?? string.Empty);

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

        GrammarResult ICILVisitor<GrammarResult>.VisitFloat64(CILParser.Float64Context context) => VisitFloat64(context);

        public static GrammarResult.Literal<double> VisitFloat64(CILParser.Float64Context context)
            => new(context.Value);

        GrammarResult ICILVisitor<GrammarResult>.VisitId(CILParser.IdContext context) => VisitId(context);
        public static GrammarResult.String VisitId(CILParser.IdContext context)
        {
            return new GrammarResult.String(ParseIdentifier(context.Start));
        }

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
            return new(ParseInt32(context.INT32().Symbol));
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


        GrammarResult ICILVisitor<GrammarResult>.VisitInt64(CILParser.Int64Context context)
        {
            return VisitInt64(context);
        }

        public GrammarResult.Literal<long> VisitInt64(CILParser.Int64Context context)
        {
            return new(ParseInt64(context.Start));
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

        GrammarResult ICILVisitor<GrammarResult>.VisitIntOrWildcard(CILParser.IntOrWildcardContext context) => VisitIntOrWildcard(context);
        public static GrammarResult.Literal<int?> VisitIntOrWildcard(CILParser.IntOrWildcardContext context) => new(context.Value);

        GrammarResult ICILVisitor<GrammarResult>.VisitSlashedName(CILParser.SlashedNameContext context)
        {
            return VisitSlashedName(context);
        }

        public static GrammarResult.Literal<TypeName> VisitSlashedName(CILParser.SlashedNameContext context)
            => new(GetSlashedNameValue(context));

        GrammarResult ICILVisitor<GrammarResult>.VisitTruefalse(CILParser.TruefalseContext context) => VisitTruefalse(context);

        internal bool ParseBoolean(IToken token) => bool.Parse(token.Text);

        public static GrammarResult.Literal<bool> VisitTruefalse(CILParser.TruefalseContext context)
            => new(context.Value);

    }
}
