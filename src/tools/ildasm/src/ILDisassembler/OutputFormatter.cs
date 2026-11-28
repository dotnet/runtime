// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

namespace ILDisassembler;

/// <summary>
/// Decorates the disassembly for the output format: plain text, HTML or RTF.
/// </summary>
internal interface IOutputFormatter
{
    string KeywordPrefix { get; }

    string KeywordPostfix { get; }

    string CommentPrefix { get; }

    string CommentPostfix { get; }

    string Keyword(string s);

    string Comment(string s);

    string Error(string s);

    // A definition that references to it can jump to, and such a reference; the token identifies the definition.
    string Anchor(string s, int token);

    string Jump(string s, int token);

    // Characters that the format itself uses.
    string OpenScope { get; }

    string CloseScope { get; }

    string LessThan { get; }

    string GreaterThan { get; }

    string Ampersand { get; }

    // The width past which a method signature is continued on the next line.
    int SignatureWrapWidth { get; }

    // What ends a line of output that uses the given line terminator.
    string LineTerminator(string newLine);

    // The text that starts and ends the output.
    string Prolog(string inputFileName, string newLine);

    string Epilog(string newLine);

    // The lines around the title comment of the output; empty when there are none.
    string TitlePrefix { get; }

    string TitlePostfix { get; }

    // Appends a printable character to the comment that shows the text of a byte array.
    void AppendCommentChar(StringBuilder sb, char c);

    // Appends a printable character to the comment that shows the text of a .data block.
    void AppendDataCommentChar(StringBuilder sb, char c);
}

internal sealed class PlainTextFormatter : IOutputFormatter
{
    public static PlainTextFormatter Instance { get; } = new();

    public string KeywordPrefix => "";

    public string KeywordPostfix => "";

    public string CommentPrefix => "";

    public string CommentPostfix => "";

    public string Keyword(string s) => s;

    public string Comment(string s) => s;

    public string Error(string s) => s;

    public string Anchor(string s, int token) => s;

    public string Jump(string s, int token) => s;

    public string OpenScope => "{";

    public string CloseScope => "}";

    public string LessThan => "<";

    public string GreaterThan => ">";

    public string Ampersand => "&";

    public int SignatureWrapWidth => 40;

    public string LineTerminator(string newLine) => newLine;

    public string Prolog(string inputFileName, string newLine) => "";

    public string Epilog(string newLine) => "";

    public string TitlePrefix => "";

    public string TitlePostfix => "";

    public void AppendCommentChar(StringBuilder sb, char c) => sb.Append(c);

    public void AppendDataCommentChar(StringBuilder sb, char c) => sb.Append(c);
}

internal sealed class HtmlFormatter : IOutputFormatter
{
    public static HtmlFormatter Instance { get; } = new();

    public string KeywordPrefix => "<B><FONT COLOR=NAVY>";

    public string KeywordPostfix => "</FONT></B>";

    public string CommentPrefix => "<I><FONT COLOR=GREEN>";

    public string CommentPostfix => "</FONT></I>";

    public string Keyword(string s) => KeywordPrefix + s + KeywordPostfix;

    public string Comment(string s) => CommentPrefix + s + CommentPostfix;

    public string Error(string s) => "<I><B><FONT COLOR=RED>" + s + "</FONT></B></I>";

    public string Anchor(string s, int token) => $"<A NAME=A{token:X8}>{s}</A>";

    public string Jump(string s, int token) => $"<A HREF=#A{token:X8}>{s}</A>";

    public string OpenScope => "{";

    public string CloseScope => "}";

    public string LessThan => "&lt;";

    public string GreaterThan => "&gt;";

    public string Ampersand => "&amp;";

    public int SignatureWrapWidth => 240;

    public string LineTerminator(string newLine) => newLine;

    public string Prolog(string inputFileName, string newLine)
        => string.Join(newLine, "<HTML>", "<HEAD>", $"<TITLE> {inputFileName} - IL DASM</TITLE>", "</HEAD>", "<BODY>",
            "<FONT SIZE=3 FACE=\"Arial\">", "<PRE>", "");

    public string Epilog(string newLine) => string.Join(newLine, "</PRE>", "</BODY>", "</HTML>", "");

    public string TitlePrefix => "<FONT SIZE=4><B>";

    public string TitlePostfix => "</B></FONT>";

    public void AppendCommentChar(StringBuilder sb, char c)
    {
        switch (c)
        {
            case '<': sb.Append(LessThan); break;
            case '>': sb.Append(GreaterThan); break;
            default: sb.Append(c); break;
        }
    }

    // COMPAT: only '<' and '>' of the text of a .data block are shown.
    public void AppendDataCommentChar(StringBuilder sb, char c)
    {
        switch (c)
        {
            case '<': sb.Append(LessThan); break;
            case '>': sb.Append(GreaterThan); break;
        }
    }
}

internal sealed class RtfFormatter : IOutputFormatter
{
    public static RtfFormatter Instance { get; } = new();

    public string KeywordPrefix => "\\b\\cf1 ";

    public string KeywordPostfix => "\\cf0\\b0 ";

    public string CommentPrefix => "\\cf2\\i ";

    public string CommentPostfix => "\\i0\\cf0 ";

    public string Keyword(string s) => KeywordPrefix + s + KeywordPostfix;

    public string Comment(string s) => CommentPrefix + s + CommentPostfix;

    public string Error(string s) => "\\cf3\\i\\b " + s + "\\cf0\\b0\\i0 ";

    public string Anchor(string s, int token) => s;

    public string Jump(string s, int token) => s;

    public string OpenScope => "\\{";

    public string CloseScope => "\\}";

    public string LessThan => "<";

    public string GreaterThan => ">";

    public string Ampersand => "&";

    public int SignatureWrapWidth => 240;

    // COMPAT: lines that end with "\r\n" are not marked as RTF line breaks.
    public string LineTerminator(string newLine) => newLine == "\n" ? "\\line\n" : newLine;

    public string Prolog(string inputFileName, string newLine)
        => "{\\rtf1\\ansi\n"
            + "{\\fonttbl{\\f0\\fmodern\\fprq1\\fcharset1 Courier New;}{\\f1\\fswiss\\fcharset1 Arial;}}\n"
            + "{\\colortbl ;\\red0\\green0\\blue128;\\red0\\green128\\blue0;\\red255\\green0\\blue0;}\n"
            + "\\viewkind4\\uc1\\pard\\f0\\fs20\n";

    public string Epilog(string newLine) => "}\n";

    public string TitlePrefix => "";

    public string TitlePostfix => "";

    public void AppendCommentChar(StringBuilder sb, char c)
    {
        if (c is '\\' or '{' or '}')
        {
            sb.Append('\\');
        }

        sb.Append(c);
    }

    public void AppendDataCommentChar(StringBuilder sb, char c) => AppendCommentChar(sb, c);
}
