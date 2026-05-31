// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;

namespace ILDisassembler;

/// <summary>
/// Helper class for writing IL assembly output with proper formatting.
/// </summary>
internal sealed class ILWriter
{
    private readonly TextWriter _output;
    private int _indentLevel;
    private const string IndentString = "  ";

    public ILWriter(TextWriter output)
    {
        _output = output;
    }

    public void Indent() => _indentLevel++;

    public void Dedent() => _indentLevel = _indentLevel > 0 ? _indentLevel - 1 : 0;

    public void WriteLine()
    {
        _output.WriteLine();
    }

    public void WriteLine(string line)
    {
        WriteIndent();
        _output.WriteLine(line);
    }

    public void Write(string text)
    {
        _output.Write(text);
    }

    public void WriteComment(string comment)
    {
        WriteIndent();
        _output.WriteLine($"// {comment}");
    }

    public void WriteIndent()
    {
        for (int i = 0; i < _indentLevel; i++)
        {
            _output.Write(IndentString);
        }
    }
}
