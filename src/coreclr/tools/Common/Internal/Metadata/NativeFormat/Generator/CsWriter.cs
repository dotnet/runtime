// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;

class CsWriter : IDisposable
{
    TextWriter _writer;

    int _indent;
    bool _emptyLineAssumed;

    public CsWriter(string filename)
    {
        _writer = new StreamWriter(filename);

        _writer.WriteLine("// Licensed to the .NET Foundation under one or more agreements.");
        _writer.WriteLine("// The .NET Foundation licenses this file to you under the MIT license.");
        _writer.WriteLine();
        _writer.WriteLine("// NOTE: This is a generated file - do not manually edit!");
        _writer.WriteLine();
    }

    public void Dispose()
    {
        _writer.Dispose();
    }

    public void Indent()
    {
        _indent++;
    }

    public void Exdent()
    {
        _indent--;
    }

    public void WriteLine(string s)
    {
        for (int i = 0; i < 4 * _indent; i++)
            _writer.Write(' ');
        _writer.WriteLine(s);

        _emptyLineAssumed = false;
    }

    public void WriteLine()
    {
        _writer.WriteLine();

        _emptyLineAssumed = true;
    }

    public void WriteLineIfNeeded()
    {
        if (!_emptyLineAssumed)
            WriteLine();
    }

    public void OpenScope(string s)
    {
        WriteLineIfNeeded();

        WriteLine(s);
        WriteLine("{");
        Indent();

        _emptyLineAssumed = true;
    }

    public void CloseScope(string s = null)
    {
        Exdent();
        WriteLine((s != null) ? ("} // " + s) : "}");
    }

    public void WriteScopeAttribute(string s)
    {
        WriteLineIfNeeded();
        WriteLine(s);

        _emptyLineAssumed = true;
    }

    public void WriteDocComment(string s)
    {
        WriteLine("/// " + s);
    }

    public void WriteTypeAttributesForCoreLib()
    {
        WriteLineIfNeeded();

        _writer.WriteLine("#if SYSTEM_PRIVATE_CORELIB");
        WriteScopeAttribute("[CLSCompliant(false)]");
        WriteScopeAttribute("[ReflectionBlocked]");
        _writer.WriteLine("#endif");
    }
}
