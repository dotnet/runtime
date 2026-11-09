// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Text;
using Internal.JitInterface;

namespace SuperPmi.Replay;

// A buffered output file, written as is (without newline translation).
internal sealed class FileWriter : IDisposable
{
    private readonly StreamWriter _writer;

    private FileWriter(StreamWriter writer) => _writer = writer;

    public static FileWriter CreateNew(string path)
    {
        try
        {
            return new FileWriter(new StreamWriter(new FileStream(path, FileMode.Create, FileAccess.Write), new UTF8Encoding(false), 1 << 16));
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    public void Print(string value) => _writer.Write(value);

    public void PrintQuotedCsvField(string value) => _writer.Write($"\"{value.Replace("\"", "\"\"")}\"");

    public void Dispose() => _writer.Dispose();
}

// Emits method statistics as CSV, to <input>.stats.
internal sealed unsafe class MethodStatsEmitter : IDisposable
{
    private readonly FileWriter _statsFile;
    private string _statsTypes;

    public MethodStatsEmitter(string nameOfInput)
    {
        string fileName = nameOfInput + ".stats";
        _statsFile = FileWriter.CreateNew(fileName);
        if (_statsFile == null)
            Logger.LogError($"Failed to open output file '{fileName}'.");
    }

    private bool Has(char type) => _statsTypes.Contains('*') || _statsTypes.Contains(type) || _statsTypes.Contains(char.ToUpperInvariant(type));

    // The CSV header row.
    public void SetStatsTypes(string types)
    {
        _statsTypes = types;
        if (_statsFile == null)
            return;

        var header = new StringBuilder();
        if (Has('h'))
            header.Append("HASH,");
        if (Has('n'))
            header.Append("METHOD_NUMBER,");
        if (Has('i'))
            header.Append("IL_CODE_SIZE,");
        if (Has('a'))
            header.Append("ASM_CODE_SIZE,");
        if (Has('t'))
            header.Append("Time1,Time2,");
        WriteRow(header);
    }

    public void Emit(int methodNumber, MethodContext mc, ulong firstTime, ulong secondTime)
    {
        if (_statsFile == null)
            return;

        var row = new StringBuilder();
        if (Has('h'))
            row.Append(mc.dumpMethodHashToBuffer()).Append(',');
        if (Has('n'))
            row.Append(methodNumber).Append(',');
        if (Has('i'))
            row.Append(mc.repCompileMethod(out _, out _).ILCodeSize).Append(',');
        if (Has('a'))
        {
            // A compile result without code is likely a thin method context.
            uint codeSize = 0;
            if (mc.cr.CompileMethod != null)
                mc.cr.repCompileMethod(out _, out codeSize, out _);
            row.Append(codeSize).Append(',');
        }
        if (Has('t'))
            row.Append(firstTime).Append(',').Append(secondTime).Append(',');
        WriteRow(row);
    }

    // Replaces the final ',' with a newline.
    private void WriteRow(StringBuilder row)
    {
        if (row.Length == 0)
            return;
        row[row.Length - 1] = '\n';
        _statsFile.Print(row.ToString());
    }

    public void Dispose() => _statsFile?.Dispose();
}
