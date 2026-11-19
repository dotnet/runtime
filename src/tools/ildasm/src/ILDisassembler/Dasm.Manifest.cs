// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Text;

namespace ILDisassembler;

// Assembly and manifest disassembly (the native dman.cpp).
internal sealed partial class Dasm
{
    private readonly List<(int Tok, string Name)> _files = new();
    private readonly List<(int Tok, string Name)> _asmRefs = new();

    private sealed class ExportedTypeDescr
    {
        public int Tok;
        public int TypeDef;
        public int Implementation;
        public string Name = "";
        public int Flags;
    }

    private readonly List<ExportedTypeDescr> _exportedTypes = new();

    private void DumpManifest()
    {
        PrintLine("// Metadata version: " + _md.MetadataVersion);
        DumpModuleRefs();
        if (_md.IsAssembly || RowCount(TableIndex.AssemblyRef) != 0)
        {
            DumpAssemblyRefs();
            DumpAssembly();
            DumpFiles();
            DumpExportedTypes();
            DumpManifestResources();
        }
        else
        {
            PrintLine(COMMENT("// Module contains no Manifest data"));
        }

        DumpScope();
        DumpVtable();
    }

    private void DumpModuleRefs()
    {
        int n = RowCount(TableIndex.ModuleRef);
        for (int i = 1; i <= n; i++)
        {
            int tk = mdtModuleRef | i;
            string name = Str(_md.GetModuleReference(MetadataTokens.ModuleReferenceHandle(i)).Name);
            if (name.Length == 0)
            {
                continue; // the no-name ModuleRef is an IJW artifact
            }

            StringBuilder sb = new();
            sb.Append(Indent).Append(KEYWORD(".module extern")).Append(' ').Append(ANCHORPT(ProperName(name), tk));
            if (_options.ShowTokens)
            {
                sb.Append(COMMENT($" /*{tk:X8}*/"));
            }

            PrintLine(sb);
            DumpCustomAttributes(tk);
            DumpPermissions(tk);
        }
    }

    private static string AssemblyFlagsText(AssemblyFlags flags, bool isDef)
    {
        int f = (int)flags;
        StringBuilder sb = new();
        if ((f & 0x0100) != 0) sb.Append("retargetable ");
        if ((f & 0x0E00) == 0x0200) sb.Append("windowsruntime ");
        if (isDef && (f & 0x00F0) == 0x0070) sb.Append("noplatform ");
        switch (f & 0x0070)
        {
            case 0x0010: sb.Append("cil "); break;
            case 0x0020: sb.Append("x86 "); break;
            case 0x0040: sb.Append("amd64 "); break;
            case 0x0050: sb.Append("arm "); break;
            case 0x0060: sb.Append("arm64 "); break;
        }

        return sb.ToString();
    }

    private void DumpAssemblyMetaData(Version v, string culture)
    {
        PrintLine($"{Indent}{KEYWORD(".ver")} {v.Major}:{v.Minor}:{v.Build}:{v.Revision}");
        if (culture.Length != 0)
        {
            StringBuilder sb = new();
            sb.Append(Indent).Append(KEYWORD(".locale")).Append(" = (");
            // The native tool includes the terminating NUL character.
            byte[] bytes = new byte[(culture.Length + 1) * 2];
            Encoding.Unicode.GetBytes(culture, 0, culture.Length, bytes, 0);
            DumpByteArray(sb, bytes);
            PrintLine(sb);
        }
    }

    private void DumpAssemblyRefs()
    {
        _asmRefs.Clear();
        int n = RowCount(TableIndex.AssemblyRef);
        _asmRefNames = new string?[n];
        for (int ix = 0; ix < n; ix++)
        {
            int tk = mdtAssemblyRef | (ix + 1);
            AssemblyReference ar = _md.GetAssemblyReference(MetadataTokens.AssemblyReferenceHandle(ix + 1));
            string name = Str(ar.Name);
            _asmRefs.Add((tk, name));

            StringBuilder sb = new();
            sb.Append(Indent).Append(KEYWORD(".assembly extern")).Append(' ');
            if (_options.ShowTokens)
            {
                sb.Append(COMMENT($"/*{tk:X8}*/ "));
            }

            foreach (string f in AssemblyFlagsText(ar.Flags, isDef: false).Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                sb.Append(KEYWORD(f + " "));
            }

            bool dup = false;
            for (int ixx = 0; ixx < ix; ixx++)
            {
                if (_asmRefs[ixx].Name == name)
                {
                    dup = true;
                    break;
                }
            }

            string alias = name;
            if (dup)
            {
                alias = name + "_" + ix.ToString(CultureInfo.InvariantCulture);
                sb.Append(ProperName(name)).Append(' ').Append(KEYWORD("as")).Append(' ').Append(ANCHORPT(ProperName(alias), tk));
            }
            else
            {
                sb.Append(ANCHORPT(ProperName(name), tk));
            }

            _asmRefNames[ix] = alias;
            PrintLine(sb);
            PrintLine(Indent + SCOPE());
            PushIndent();
            DumpCustomAttributes(tk);
            byte[] key = Blob(ar.PublicKeyOrToken);
            if (key.Length != 0)
            {
                sb.Clear();
                sb.Append(Indent).Append(KEYWORD(((int)ar.Flags & 0x1) != 0 ? ".publickey" : ".publickeytoken")).Append(" = (");
                DumpByteArray(sb, key);
                PrintLine(sb);
            }

            byte[] hash = Blob(ar.HashValue);
            if (hash.Length != 0)
            {
                sb.Clear();
                sb.Append(Indent).Append(KEYWORD(".hash")).Append(" = (");
                DumpByteArray(sb, hash);
                PrintLine(sb);
            }

            DumpAssemblyMetaData(ar.Version, Str(ar.Culture));
            PopIndent();
            PrintLine(Indent + UNSCOPE());
        }
    }

    private void DumpAssembly()
    {
        if (!_md.IsAssembly)
        {
            return;
        }

        int tk = mdtAssembly | 1;
        AssemblyDefinition ad = _md.GetAssemblyDefinition();
        StringBuilder sb = new();
        sb.Append(Indent).Append(KEYWORD(".assembly")).Append(' ');
        if (_options.ShowTokens)
        {
            sb.Append(COMMENT($"/*{tk:X8}*/ "));
        }

        string flags = AssemblyFlagsText(ad.Flags, isDef: true);
        if (flags.Length != 0)
        {
            foreach (string f in flags.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                sb.Append(KEYWORD(f + " "));
            }
        }

        sb.Append(ANCHORPT(ProperName(Str(ad.Name)), tk));
        PrintLine(sb);
        PrintLine(Indent + SCOPE());
        PushIndent();
        DumpCustomAttributes(tk);
        DumpPermissions(tk);
        byte[] key = Blob(ad.PublicKey);
        if (key.Length != 0)
        {
            sb.Clear();
            sb.Append(Indent).Append(KEYWORD(".publickey")).Append(" = (");
            DumpByteArray(sb, key);
            PrintLine(sb);
        }

        if (ad.HashAlgorithm != 0)
        {
            PrintLine($"{Indent}{KEYWORD(".hash algorithm")} 0x{(uint)ad.HashAlgorithm:X8}");
        }

        DumpAssemblyMetaData(ad.Version, Str(ad.Culture));
        PopIndent();
        PrintLine(Indent + UNSCOPE());
    }

    private void DumpFiles()
    {
        _files.Clear();
        int n = RowCount(TableIndex.File);
        int entryPoint = _pe.PEHeaders.CorHeader!.EntryPointTokenOrRelativeVirtualAddress;
        for (int i = 1; i <= n; i++)
        {
            int tk = mdtFile | i;
            AssemblyFile f = _md.GetAssemblyFile(MetadataTokens.AssemblyFileHandle(i));
            string name = Str(f.Name);
            _files.Add((tk, name));
            StringBuilder sb = new();
            sb.Append(Indent).Append(KEYWORD(".file")).Append(' ');
            if (_options.ShowTokens)
            {
                sb.Append(COMMENT($"/*{tk:X8}*/ "));
            }

            if (!f.ContainsMetadata)
            {
                sb.Append(KEYWORD("nometadata "));
            }

            sb.Append(ANCHORPT(ProperName(name), tk));
            PrintLine(sb);
            if (entryPoint == tk)
            {
                PrintLine(KEYWORD("    .entrypoint"));
            }

            byte[] hash = Blob(f.HashValue);
            if (hash.Length != 0)
            {
                sb.Clear();
                sb.Append("    ").Append(KEYWORD(".hash")).Append(" = (");
                DumpByteArray(sb, hash);
                PrintLine(sb);
            }

            DumpCustomAttributes(tk);
        }
    }

    private void DumpExportedTypeFQN(ExportedTypeDescr e, StringBuilder sb)
    {
        if (TypeFromToken(e.Implementation) == mdtExportedType)
        {
            ExportedTypeDescr? outer = _exportedTypes.Find(x => x.Tok == e.Implementation);
            if (outer is not null)
            {
                DumpExportedTypeFQN(outer, sb);
                sb.Append('/');
            }
        }

        sb.Append(JUMPPT(ProperName(e.Name), e.Tok));
    }

    private void DumpImplementation(int tkImpl, uint offset)
    {
        if (RidFromToken(tkImpl) == 0)
        {
            return;
        }

        StringBuilder sb = new();
        switch (TypeFromToken(tkImpl))
        {
            case mdtFile:
            {
                int i = _files.FindIndex(f => f.Tok == tkImpl);
                if (i >= 0)
                {
                    sb.Append(Indent).Append(KEYWORD(".file")).Append(' ').Append(JUMPPT(ProperName(_files[i].Name), tkImpl));
                    if (_options.ShowTokens)
                    {
                        sb.Append(COMMENT($"/*{tkImpl:X8}*/ "));
                    }

                    if (offset != 0xFFFFFFFF)
                    {
                        sb.Append(' ').Append(KEYWORD("at")).Append(" 0x").Append(offset.ToString("X8", CultureInfo.InvariantCulture));
                    }

                    PrintLine(sb);
                }

                break;
            }

            case mdtAssemblyRef:
            {
                int i = _asmRefs.FindIndex(a => a.Tok == tkImpl);
                if (i >= 0)
                {
                    sb.Append(Indent).Append(KEYWORD(".assembly extern")).Append(' ').Append(JUMPPT(ProperName(_asmRefNames[i] ?? _asmRefs[i].Name), tkImpl));
                    if (_options.ShowTokens)
                    {
                        sb.Append(COMMENT($" /*{tkImpl:X8}*/ "));
                    }

                    PrintLine(sb);
                }

                break;
            }

            case mdtExportedType:
            {
                ExportedTypeDescr? e = _exportedTypes.Find(x => x.Tok == tkImpl);
                if (e is not null)
                {
                    sb.Append(Indent).Append(KEYWORD(".class extern")).Append(' ');
                    DumpExportedTypeFQN(e, sb);
                    if (_options.ShowTokens)
                    {
                        sb.Append(COMMENT($" /*{tkImpl:X8}*/ "));
                    }

                    PrintLine(sb);
                }

                break;
            }
        }
    }

    private void DumpExportedType(ExportedTypeDescr e, StringBuilder sb)
    {
        if (_options.ShowTokens)
        {
            sb.Append(COMMENT($"/*{e.Tok:X8}*/ "));
        }

        int f = e.Flags;
        if ((f & 0x7) == 1) sb.Append(KEYWORD("public "));
        if ((f & 0x00200000) != 0) sb.Append(KEYWORD("forwarder "));
        switch (f & 0x7)
        {
            case 2: sb.Append(KEYWORD("nested public ")); break;
            case 3: sb.Append(KEYWORD("nested private ")); break;
            case 4: sb.Append(KEYWORD("nested family ")); break;
            case 5: sb.Append(KEYWORD("nested assembly ")); break;
            case 6: sb.Append(KEYWORD("nested famandassem ")); break;
            case 7: sb.Append(KEYWORD("nested famorassem ")); break;
        }

        sb.Append(ANCHORPT(ProperName(e.Name), e.Tok));
        PrintLine(sb);
        PrintLine(Indent + SCOPE());
        PushIndent();
        DumpCustomAttributes(e.Tok);
        DumpImplementation(e.Implementation, 0xFFFFFFFF);
        if (RidFromToken(e.TypeDef) != 0)
        {
            PrintLine($"{Indent}{KEYWORD(".class")} 0x{e.TypeDef:X8}");
        }

        PopIndent();
        PrintLine(Indent + UNSCOPE());
    }

    private void DumpExportedTypes()
    {
        _exportedTypes.Clear();
        int n = RowCount(TableIndex.ExportedType);
        for (int i = 1; i <= n; i++)
        {
            ExportedType et = _md.GetExportedType(MetadataTokens.ExportedTypeHandle(i));
            string ns = Str(et.Namespace);
            string name = Str(et.Name);
            _exportedTypes.Add(new ExportedTypeDescr
            {
                Tok = mdtExportedType | i,
                TypeDef = et.GetTypeDefinitionId(),
                Implementation = TokenOrNil(et.Implementation, mdtFile),
                Name = ns.Length != 0 ? ns + "." + name : name,
                Flags = (int)et.Attributes,
            });
        }

        foreach (ExportedTypeDescr e in _exportedTypes)
        {
            int impl = e.Implementation;
            // The ComType of a nested class has its nester's ComType as implementation.
            while (TypeFromToken(impl) == mdtExportedType)
            {
                ExportedTypeDescr? outer = _exportedTypes.Find(x => x.Tok == impl);
                if (outer is null)
                {
                    break;
                }

                impl = outer.Implementation;
            }

            if (RidFromToken(impl) != 0)
            {
                StringBuilder sb = new();
                sb.Append(Indent).Append(KEYWORD(".class extern")).Append(' ');
                DumpExportedType(e, sb);
                e.TypeDef = 0;
            }
        }
    }

    // ConvertToLegalFileNameInPlace: neutralizes reserved names and replaces characters invalid in file names.
    private static bool ConvertToLegalFileName(ref string name)
    {
        bool alias = false;
        char[] c = name.ToCharArray();
        string[] reserved = ["COM", "LPT", "CON", "PRN", "AUX", "NUL"];
        for (int i = 0; i < reserved.Length; i++)
        {
            if (c.Length >= 3 && string.Compare(name, 0, reserved[i], 0, 3, StringComparison.OrdinalIgnoreCase) == 0)
            {
                int p = 3;
                if (i <= 1)
                {
                    if (p < c.Length && c[p] >= '1' && c[p] <= '9')
                    {
                        p++;
                    }
                    else
                    {
                        continue;
                    }
                }

                if (p >= c.Length || c[p] == '.')
                {
                    c[0] = '_';
                    alias = true;
                    break;
                }
            }
        }

        for (int i = 0; i < c.Length; i++)
        {
            char ch = c[i];
            if (ch == 0)
            {
                Array.Resize(ref c, i);
                break;
            }

            if (ch < 32)
            {
                c[i] = '~';
                alias = true;
                continue;
            }

            char r = ch switch
            {
                ':' => '!',
                '\\' => '$',
                ',' => '&',
                ';' => '@',
                '<' => '(',
                '>' => ')',
                '"' => '`',
                '/' => '_',
                '|' => '-',
                '*' => '+',
                '?' => '=',
                _ => ch,
            };
            if (r != ch)
            {
                c[i] = r;
                alias = true;
            }
        }

        name = new string(c);
        return alias;
    }

    private void DumpManifestResources()
    {
        int n = RowCount(TableIndex.ManifestResource);
        if (n == 0)
        {
            return;
        }

        string outDir = "";
        string outName = "";
        if (_outputFileName.Length != 0)
        {
            outDir = Path.GetDirectoryName(_outputFileName) ?? "";
            outName = Path.GetFileName(_outputFileName);
        }

        List<string> names = new() { outName };
        int dot = outName.LastIndexOf('.');
        names.Add((dot < 0 ? outName : outName.Substring(0, dot)) + ".res");

        ImmutableArray<byte> resources = default;
        DirectoryEntry resDir = _pe.PEHeaders.CorHeader!.ResourcesDirectory;
        for (int ix = 1; ix <= n; ix++)
        {
            int tk = mdtManifestResource | ix;
            ManifestResource mr = _md.GetManifestResource(MetadataTokens.ManifestResourceHandle(ix));
            string name = Str(mr.Name);
            StringBuilder sb = new();
            sb.Append(Indent).Append(KEYWORD(".mresource")).Append(' ');
            if (_options.ShowTokens)
            {
                sb.Append(COMMENT($"/*{tk:X8}*/ "));
            }

            int vis = (int)mr.Attributes & 0x7;
            if (vis == 1) sb.Append(KEYWORD("public "));
            if (vis == 2) sb.Append(KEYWORD("private "));
            sb.Append(ProperName(name));

            string fileName = name;
            bool alias = ConvertToLegalFileName(ref fileName);
            string baseName = fileName;
            for (int index = 1; ; index++)
            {
                bool conflict = fileName.Length == 0;
                if (!conflict)
                {
                    foreach (string prev in names)
                    {
                        if (string.Equals(prev, fileName, StringComparison.OrdinalIgnoreCase))
                        {
                            conflict = true;
                            break;
                        }
                    }
                }

                if (!conflict)
                {
                    break;
                }

                fileName = baseName + index.ToString(CultureInfo.InvariantCulture);
                alias = true;
            }

            names.Add(fileName);
            if (alias)
            {
                sb.Append(' ').Append(KEYWORD("as")).Append(' ').Append(ProperName(fileName));
            }

            PrintLine(sb);
            PrintLine(Indent + SCOPE());
            PushIndent();
            DumpCustomAttributes(tk);
            int impl = TokenOrNil(mr.Implementation, mdtFile);
            uint offset = (uint)mr.Offset;
            if (RidFromToken(impl) == 0)
            {
                if (resources.IsDefault)
                {
                    byte[]? r = resDir.RelativeVirtualAddress != 0 ? GetBytesAtRva((uint)resDir.RelativeVirtualAddress, resDir.Size) : null;
                    if (r is null)
                    {
                        PrintError("Failed to read import data.");
                        resources = ImmutableArray<byte>.Empty;
                    }
                    else
                    {
                        resources = ImmutableArray.Create(r);
                    }
                }

                if (!resources.IsEmpty)
                {
                    DumpResourceFile(resources, offset, name, Path.Combine(outDir, fileName), fileName);
                }
            }
            else
            {
                DumpImplementation(impl, offset);
            }

            PopIndent();
            PrintLine(Indent + UNSCOPE());
        }
    }

    private void DumpResourceFile(ImmutableArray<byte> res, uint offset, string name, string path, string displayName)
    {
        if (offset + 4 > res.Length)
        {
            PrintError($"ERROR retrieving/saving embedded managed resource '{name}' at offset 0x{offset:X8}");
            return;
        }

        uint len = (uint)(res[(int)offset] | res[(int)offset + 1] << 8 | res[(int)offset + 2] << 16 | res[(int)offset + 3] << 24);
        PrintLine(COMMENT($"{Indent}// Offset: 0x{offset:X8} Length: 0x{len:X8}"));
        if (!_toFile)
        {
            return;
        }

        try
        {
            using FileStream fs = new(path, FileMode.Create, FileAccess.Write);
            int start = (int)offset + 4;
            int count = (int)Math.Min(len, (uint)Math.Max(0, res.Length - start));
            fs.Write(res.AsSpan().Slice(start, count));
            if (count < len)
            {
                throw new IOException();
            }

            PrintLine(COMMENT($"{Indent}// WARNING: managed resource file {ProperName(displayName)} created"));
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            PrintError($"{Indent}// ERROR: reading managed resource {ProperName(displayName)} at offset 0x{offset:X}");
        }
    }

    private void DumpScope()
    {
        ModuleDefinition mod = _md.GetModuleDefinition();
        string name = Str(mod.Name);
        if (name.Length == 0)
        {
            return;
        }

        PrintLine($"{Indent}{KEYWORD(".module")} {ProperName(name)}");
        Guid mvid = _md.GetGuid(mod.Mvid);
        PrintLine(COMMENT($"{Indent}// MVID: {{{mvid.ToString("D").ToLowerInvariant()}}}"));
        DumpCustomAttributes(mdtModule | 1);
        DumpPermissions(mdtModule | 1);
    }
}
