// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace ILDisassembler;

// Output primitives: the line writer, the code indent, the HTML/RTF decorations, name quoting and
// C runtime style number formatting. These mirror printLine, g_szAsmCodeIndent, KEYWORD/COMMENT/...,
// ProperName and the printf formats used by the native ildasm, whose output is the specification.
internal sealed partial class Dasm
{
    private readonly StringBuilder _indent = new();

    private string Indent => _indent.ToString();

    private void PushIndent(string s = "  ") => _indent.Append(s);

    private void PopIndent(int n = 2) => _indent.Length = Math.Max(0, _indent.Length - n);

    private void PrintLine(string line)
    {
        _out.Write(line);
        _out.Write(_rtf && _newLine == "\n" ? "\\line\n" : _newLine);
    }

    private void PrintLine(StringBuilder line) => PrintLine(line.ToString());

    private void PrintError(string line)
    {
        if (_toFile)
        {
            PrintLine(ERRORMSG(line));
        }

        _error.WriteLine(line);
    }

    // ---------------------------------------------------------------- decorations

    private string KEYWORD(string s) => _html ? "<B><FONT COLOR=NAVY>" + s + "</FONT></B>" : _rtf ? "\\b\\cf1 " + s + "\\cf0\\b0 " : s;

    private string KeywordPrefix => _html ? "<B><FONT COLOR=NAVY>" : _rtf ? "\\b\\cf1 " : "";

    private string KeywordPostfix => _html ? "</FONT></B>" : _rtf ? "\\cf0\\b0 " : "";

    private string COMMENT(string s) => _html ? "<I><FONT COLOR=GREEN>" + s + "</FONT></I>" : _rtf ? "\\cf2\\i " + s + "\\i0\\cf0 " : s;

    private string CommentPrefix => _html ? "<I><FONT COLOR=GREEN>" : _rtf ? "\\cf2\\i " : "";

    private string CommentPostfix => _html ? "</FONT></I>" : _rtf ? "\\i0\\cf0 " : "";

    private string ERRORMSG(string s) => _html ? "<I><B><FONT COLOR=RED>" + s + "</FONT></B></I>" : _rtf ? "\\cf3\\i\\b " + s + "\\cf0\\b0\\i0 " : s;

    private string ANCHORPT(string s, int tk) => _html ? $"<A NAME=A{tk:X8}>{s}</A>" : s;

    private string JUMPPT(string s, int tk) => _html ? $"<A HREF=#A{tk:X8}>{s}</A>" : s;

    private string SCOPE() => _rtf ? "\\{" : "{";

    private string UNSCOPE() => _rtf ? "\\}" : "}";

    private string LTN() => _html ? "&lt;" : "<";

    private string GTN() => _html ? "&gt;" : ">";

    private string AMP() => _html ? "&amp;" : "&";

    // ---------------------------------------------------------------- names

    private static readonly HashSet<string> s_keywords = new(OpcodeTable.Keywords, StringComparer.Ordinal);

    private const string AllowedSymbols = "#_@$.";

    // Names are checked byte by byte in UTF-8 by the native tool, so any non-ASCII character forces quoting.
    private static bool IsDisallowedStarting(char c)
        => !((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || AllowedSymbols.IndexOf(c) >= 0);

    private static bool IsDisallowedCont(char c)
    {
        if (c == '#')
        {
            return true;
        }

        if (c == '?' || c == '`' || (c >= '0' && c <= '9'))
        {
            return false;
        }

        return IsDisallowedStarting(c);
    }

    internal bool IsNameToQuote(string? name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return true;
        }

        if (_options.QuoteAllNames)
        {
            return name != ".ctor" && name != ".cctor";
        }

        bool starting = true;
        for (int i = 0; i < name.Length; i++)
        {
            char c = name[i];
            if (starting ? IsDisallowedStarting(c) : IsDisallowedCont(c))
            {
                return true;
            }

            starting = c == '.';
            if (starting && i + 1 < name.Length && name[i + 1] == '.')
            {
                return true;
            }
        }

        if (s_keywords.Contains(name))
        {
            return name != ".ctor" && name != ".cctor";
        }

        return name[0] == '.' || name[name.Length - 1] == '.';
    }

    private bool IsLocalToQuote(string name)
        => IsNameToQuote(name) || (name.Length > 1 && name.IndexOf('.', 1) >= 0);

    internal string UnquotedProperName(string name)
    {
        if (name.Length == 0)
        {
            return "";
        }

        StringBuilder sb = new(name.Length + 8);
        foreach (char c in name)
        {
            switch (c)
            {
                case '\t': sb.Append("\\t"); break;
                case '\n': sb.Append("\\n"); break;
                case '\b': sb.Append("\\b"); break;
                case '\r': sb.Append("\\r"); break;
                case '\f': sb.Append("\\f"); break;
                case '\v': sb.Append("\\v"); break;
                case '\a': sb.Append("\\a"); break;
                case '\\': sb.Append("\\\\"); break;
                case '\'': sb.Append("\\'"); break;
                case '"': sb.Append("\\\""); break;
                case '{': sb.Append(SCOPE()); break;
                case '}': sb.Append(UNSCOPE()); break;
                case '<': sb.Append(LTN()); break;
                case '>': sb.Append(GTN()); break;
                case '&': sb.Append(AMP()); break;
                default: sb.Append(c); break;
            }
        }

        return sb.ToString();
    }

    internal string ProperName(string name, bool isLocalName = false)
    {
        if (name.Length == 0)
        {
            return "";
        }

        bool quoted = isLocalName ? IsLocalToQuote(name) : IsNameToQuote(name);
        string s = UnquotedProperName(name);
        return quoted ? "'" + s + "'" : s;
    }

    private string ProperLocalName(string name) => ProperName(name, isLocalName: true);

    // ---------------------------------------------------------------- strings

    private static bool IsPrint(int c) => c >= 0x20 && c <= 0x7E;

    // DumpQString: appends a quoted string to sb, splitting it into "..." + "..." lines when it is longer than maxLen.
    // Like the native tool, lengths are measured in UTF-8 bytes.
    private void DumpQString(StringBuilder sb, string toDump, string prefix, int maxLen)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(toDump);
        int lwt = Utf8Length(sb);
        int p = 0;
        int remaining = bytes.Length;
        int tally = 0;
        int uMaxLen = maxLen;
        List<byte> pending = new();
        sb.Append('"');
        do
        {
            int l = remaining;
            if (l > uMaxLen + 2)
            {
                l = uMaxLen;
                while (l > 0 && bytes[p + l - 1] == '\\')
                {
                    l--;
                }

                if (l == 0)
                {
                    l = (uMaxLen + 1) & ~1;
                }
            }

            if (tally != 0)
            {
                PrintLine(sb);
                sb.Clear();
                sb.Append(prefix).Append("+ \"");
            }
            else
            {
                uMaxLen = uMaxLen - Encoding.UTF8.GetByteCount(prefix) + lwt;
            }

            tally++;
            for (int i = 0; i < l; i++, p++)
            {
                byte b = bytes[p];
                string? esc = (char)b switch
                {
                    '\n' => "\\n",
                    '\r' => "\\r",
                    '\t' => "\\t",
                    '\b' => "\\b",
                    '\f' => "\\f",
                    '\v' => "\\v",
                    '\a' => "\\a",
                    '?' => "\\?",
                    '\\' => "\\\\",
                    '"' => "\\\"",
                    '{' => SCOPE(),
                    '}' => UNSCOPE(),
                    '<' => LTN(),
                    '>' => GTN(),
                    _ => null,
                };
                if (esc is null)
                {
                    pending.Add(b);
                }
                else
                {
                    FlushPending(sb, pending);
                    sb.Append(esc);
                }
            }

            FlushPending(sb, pending);
            sb.Append('"');
            remaining -= l;
        }
        while (remaining != 0);

        static void FlushPending(StringBuilder sb, List<byte> pending)
        {
            if (pending.Count > 0)
            {
                sb.Append(Encoding.UTF8.GetString(pending.ToArray()));
                pending.Clear();
            }
        }
    }

    private static int Utf8Length(StringBuilder sb)
    {
        int n = 0;
        foreach (ReadOnlyMemory<char> chunk in sb.GetChunks())
        {
            n += Encoding.UTF8.GetByteCount(chunk.Span);
        }

        return n;
    }

    // DumpByteArray: appends "XX XX ... ) // ascii", 16 bytes per line, continuation lines aligned to the current column.
    private void DumpByteArray(StringBuilder sb, ReadOnlySpan<byte> blob)
    {
        int ulStrOffset = sb.Length;
        int k = 0;
        bool printsz = false;
        StringBuilder sz = new();
        for (int j = 0; j < blob.Length; j++, k++)
        {
            if (k == 16)
            {
                if (printsz)
                {
                    sb.Append(COMMENT("  // " + sz));
                }

                PrintLine(sb);
                sb.Clear();
                sb.Append(Indent);
                while (sb.Length < ulStrOffset)
                {
                    sb.Append(' ');
                }

                k = 0;
                sz.Clear();
                printsz = false;
            }

            byte byt = blob[j];
            sb.Append(byt.ToString("X2", CultureInfo.InvariantCulture)).Append(' ');
            AppendPrintable(sz, byt, ref printsz);
        }

        sb.Append(") ");
        if (printsz)
        {
            for (int j = k; j < 16; j++)
            {
                sb.Append("   ");
            }

            sb.Append(COMMENT("// " + sz));
        }
    }

    private void AppendPrintable(StringBuilder sz, byte byt, ref bool printsz)
    {
        if (!IsPrint(byt))
        {
            sz.Append('.');
            return;
        }

        if (_rtf)
        {
            if (byt == '\\' || byt == '{' || byt == '}')
            {
                sz.Append('\\');
            }

            sz.Append((char)byt);
        }
        else if (_html)
        {
            sz.Append(byt == '<' ? LTN() : byt == '>' ? GTN() : ((char)byt).ToString());
        }
        else
        {
            sz.Append((char)byt);
        }

        printsz = true;
    }

    // DumpUnicodeString: appends a quoted string, or a bytearray if the string is not printable ASCII.
    private void DumpUnicodeString(StringBuilder sb, ReadOnlySpan<byte> utf16Bytes)
    {
        int cch = utf16Bytes.Length / 2;
        bool asBytes = false;
        char[] chars = new char[cch];
        for (int i = 0; i < cch; i++)
        {
            chars[i] = (char)(utf16Bytes[2 * i] | (utf16Bytes[2 * i + 1] << 8));
            if (chars[i] == 0)
            {
                asBytes = true;
            }
        }

        string s = new(chars);
        if (!asBytes)
        {
            foreach (byte b in Encoding.UTF8.GetBytes(s))
            {
                if (!IsPrint(b) && b != '\t' && b != '\n' && b != '\r')
                {
                    asBytes = true;
                    break;
                }
            }
        }

        if (!asBytes)
        {
            DumpQString(sb, s, Indent, 50);
        }
        else
        {
            sb.Append(KEYWORD("bytearray")).Append(" (");
            DumpByteArray(sb, utf16Bytes.Slice(0, cch * 2));
        }
    }

    // ---------------------------------------------------------------- C runtime formatting

    // Emulates printf("%.*g") and printf("%#.*g").
    internal static string FormatG(double v, int precision, bool alternate = false)
    {
        if (double.IsNaN(v))
        {
            return BitConverter.DoubleToInt64Bits(v) < 0 ? "-nan" : "nan";
        }

        if (double.IsInfinity(v))
        {
            return v < 0 ? "-inf" : "inf";
        }

        int p = precision == 0 ? 1 : precision;
        if (v == 0)
        {
            string zero = BitConverter.DoubleToInt64Bits(v) < 0 ? "-0" : "0";
            return alternate ? zero + "." + new string('0', p - 1) : zero;
        }

        string e = v.ToString("E" + (p - 1).ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
        int ePos = e.IndexOf('E');
        int x = int.Parse(e.AsSpan(ePos + 1), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture);
        if (p > x && x >= -4)
        {
            string f = v.ToString("F" + (p - 1 - x).ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
            if (alternate)
            {
                return f.Contains('.') ? f : f + ".";
            }

            return TrimZeros(f);
        }

        string mantissa = e.Substring(0, ePos);
        if (alternate)
        {
            if (!mantissa.Contains('.'))
            {
                mantissa += ".";
            }
        }
        else
        {
            mantissa = TrimZeros(mantissa);
        }

        int absX = Math.Abs(x);
        return mantissa + "e" + (x < 0 ? "-" : "+") + (absX < 10 ? "0" : "") + absX.ToString(CultureInfo.InvariantCulture);

        static string TrimZeros(string s)
        {
            if (!s.Contains('.'))
            {
                return s;
            }

            s = s.TrimEnd('0');
            return s.EndsWith('.') ? s.Substring(0, s.Length - 1) : s;
        }
    }

    private static bool IsSpecialNumber(string s)
        => s.Contains('#') || s.Contains("nan", StringComparison.Ordinal) || s.Contains("NAN", StringComparison.Ordinal)
            || s.Contains("inf", StringComparison.Ordinal) || s.Contains("INF", StringComparison.Ordinal);

    private static double Atof(string s)
        => double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out double d) ? d : 0;

    // printf("%-10s", s)
    private static string Pad10(string s) => s.Length >= 10 ? s : s.PadRight(10);

    private static string X8(int v) => v.ToString("X8", CultureInfo.InvariantCulture);

    private static string X8(uint v) => v.ToString("X8", CultureInfo.InvariantCulture);

    private static string x8(uint v) => v.ToString("x8", CultureInfo.InvariantCulture);
}
