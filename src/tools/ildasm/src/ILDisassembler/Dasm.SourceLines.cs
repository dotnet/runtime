// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Text;

namespace ILDisassembler;

// /LINENUM: .line directives from the portable PDB (embedded, or found through the CodeView entry or next to the
// image). The native ildasm on .NET has no symbol reader and ignores the option (dotnet/runtime#53822); the
// directive format is the one it used with a symbol reader.
internal sealed partial class Dasm
{
    private MetadataReader? _pdb;
    private MetadataReaderProvider? _pdbProvider;
    private string _wasFileName = "";

    private void OpenPdb(string inputFileName)
    {
        if (!_options.ShowLineNumbers || inputFileName == "<stream>")
        {
            return;
        }

        try
        {
            if (_pe.TryOpenAssociatedPortablePdb(inputFileName, path => File.Exists(path) ? File.OpenRead(path) : null, out _pdbProvider, out _))
            {
                _pdb = _pdbProvider!.GetMetadataReader();
            }
        }
        catch (Exception e) when (e is BadImageFormatException or IOException or UnauthorizedAccessException)
        {
            _pdb = null;
        }
    }

    private List<SequencePoint>? GetSequencePoints(int methodToken)
    {
        if (_pdb is null)
        {
            return null;
        }

        MethodDebugInformationHandle h = MetadataTokens.MethodDebugInformationHandle(RidFromToken(methodToken));
        if (RidFromToken(methodToken) > _pdb.MethodDebugInformation.Count)
        {
            return null;
        }

        List<SequencePoint> points = new();
        try
        {
            foreach (SequencePoint sp in _pdb.GetMethodDebugInformation(h).GetSequencePoints())
            {
                points.Add(sp);
            }
        }
        catch (BadImageFormatException)
        {
            return null;
        }

        // Ordered like the native tool: by offset, then line, then column.
        points.Sort((a, b) =>
        {
            int c = a.Offset.CompareTo(b.Offset);
            if (c == 0)
            {
                c = a.StartLine.CompareTo(b.StartLine);
            }

            return c != 0 ? c : a.StartColumn.CompareTo(b.StartColumn);
        });
        return points.Count == 0 ? null : points;
    }

    private void DumpSourceLine(SequencePoint sp)
    {
        string fileName = sp.Document.IsNil ? "" : _pdb!.GetString(_pdb.GetDocument(sp.Document).Name);
        bool newFile = fileName != _wasFileName;
        StringBuilder sb = new();
        sb.Append(Indent).Append(KEYWORD(".line")).Append(' ')
            .Append(sp.StartLine).Append(',').Append(sp.EndLine).Append(" : ")
            .Append(sp.StartColumn).Append(',').Append(sp.EndColumn).Append(' ');
        if (newFile)
        {
            _wasFileName = fileName;
            string name = ProperName(fileName);
            sb.Append(name.StartsWith('\'') ? name : "'" + name + "'");
        }
        else
        {
            sb.Append("''");
        }

        PrintLine(sb);
    }
}
