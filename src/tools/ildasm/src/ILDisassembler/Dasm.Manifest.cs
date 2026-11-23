// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Text;

namespace ILDisassembler;

// Assembly and manifest disassembly.
internal sealed partial class Dasm
{
    private readonly List<(AssemblyFileHandle Handle, string Name)> _files = new();
    private readonly List<(AssemblyReferenceHandle Handle, string Name)> _asmRefs = new();

    private sealed class ExportedTypeDescr
    {
        public ExportedTypeHandle Handle;
        // The TypeDef id hint, printed as is.
        public int TypeDef;
        public EntityHandle Implementation;
        public string Name = "";
        public TypeAttributes Flags;
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
        // SRM has no ModuleReference collection.
        int n = RowCount(TableIndex.ModuleRef);
        for (int i = 1; i <= n; i++)
        {
            ModuleReferenceHandle h = MetadataTokens.ModuleReferenceHandle(i);
            string name = Str(_md.GetModuleReference(h).Name);
            if (name.Length == 0)
            {
                continue; // the no-name ModuleRef is an IJW artifact
            }

            StringBuilder sb = new();
            sb.Append(Indent).Append(KEYWORD(".module extern")).Append(' ').Append(ANCHORPT(ProperName(name), h));
            if (_options.ShowTokens)
            {
                sb.Append(COMMENT($" /*{Token(h):X8}*/"));
            }

            PrintLine(sb);
            DumpCustomAttributes(h);
            DumpPermissions(h);
        }
    }

    // The processor architecture of an assembly (afPA_*), bits 4-7 of the flags, which AssemblyFlags does not define.
    private enum ProcessorArchitecture
    {
        MSIL = 0x0010,
        X86 = 0x0020,
        AMD64 = 0x0040,
        ARM = 0x0050,
        ARM64 = 0x0060,
        NoPlatform = 0x0070,
        Mask = 0x0070,
        FullMask = 0x00F0,
    }

    private static string AssemblyFlagsText(AssemblyFlags flags, bool isDef)
    {
        StringBuilder sb = new();
        if ((flags & AssemblyFlags.Retargetable) != 0) sb.Append("retargetable ");
        if ((flags & AssemblyFlags.ContentTypeMask) == AssemblyFlags.WindowsRuntime) sb.Append("windowsruntime ");
        ProcessorArchitecture pa = (ProcessorArchitecture)flags;
        if (isDef && (pa & ProcessorArchitecture.FullMask) == ProcessorArchitecture.NoPlatform) sb.Append("noplatform ");
        switch (pa & ProcessorArchitecture.Mask)
        {
            case ProcessorArchitecture.MSIL: sb.Append("cil "); break;
            case ProcessorArchitecture.X86: sb.Append("x86 "); break;
            case ProcessorArchitecture.AMD64: sb.Append("amd64 "); break;
            case ProcessorArchitecture.ARM: sb.Append("arm "); break;
            case ProcessorArchitecture.ARM64: sb.Append("arm64 "); break;
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
            // The terminating NUL character is included.
            byte[] bytes = new byte[(culture.Length + 1) * 2];
            Encoding.Unicode.GetBytes(culture, 0, culture.Length, bytes, 0);
            DumpByteArray(sb, bytes);
            PrintLine(sb);
        }
    }

    private void DumpAssemblyRefs()
    {
        _asmRefs.Clear();
        _asmRefNames = new string?[_md.AssemblyReferences.Count];
        int ix = 0;
        foreach (AssemblyReferenceHandle h in _md.AssemblyReferences)
        {
            AssemblyReference ar = _md.GetAssemblyReference(h);
            string name = Str(ar.Name);
            _asmRefs.Add((h, name));

            StringBuilder sb = new();
            sb.Append(Indent).Append(KEYWORD(".assembly extern")).Append(' ');
            if (_options.ShowTokens)
            {
                sb.Append(COMMENT($"/*{Token(h):X8}*/ "));
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
                sb.Append(ProperName(name)).Append(' ').Append(KEYWORD("as")).Append(' ').Append(ANCHORPT(ProperName(alias), h));
            }
            else
            {
                sb.Append(ANCHORPT(ProperName(name), h));
            }

            _asmRefNames[ix] = alias;
            PrintLine(sb);
            PrintLine(Indent + SCOPE());
            PushIndent();
            DumpCustomAttributes(h);
            byte[] key = Blob(ar.PublicKeyOrToken);
            if (key.Length != 0)
            {
                sb.Clear();
                sb.Append(Indent).Append(KEYWORD((ar.Flags & AssemblyFlags.PublicKey) != 0 ? ".publickey" : ".publickeytoken")).Append(" = (");
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
            ix++;
        }
    }

    private void DumpAssembly()
    {
        if (!_md.IsAssembly)
        {
            return;
        }

        EntityHandle h = EntityHandle.AssemblyDefinition;
        AssemblyDefinition ad = _md.GetAssemblyDefinition();
        StringBuilder sb = new();
        sb.Append(Indent).Append(KEYWORD(".assembly")).Append(' ');
        if (_options.ShowTokens)
        {
            sb.Append(COMMENT($"/*{Token(h):X8}*/ "));
        }

        string flags = AssemblyFlagsText(ad.Flags, isDef: true);
        if (flags.Length != 0)
        {
            foreach (string f in flags.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                sb.Append(KEYWORD(f + " "));
            }
        }

        sb.Append(ANCHORPT(ProperName(Str(ad.Name)), h));
        PrintLine(sb);
        PrintLine(Indent + SCOPE());
        PushIndent();
        DumpCustomAttributes(h);
        DumpPermissions(h);
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
        foreach (AssemblyFileHandle h in _md.AssemblyFiles)
        {
            AssemblyFile f = _md.GetAssemblyFile(h);
            string name = Str(f.Name);
            _files.Add((h, name));
            StringBuilder sb = new();
            sb.Append(Indent).Append(KEYWORD(".file")).Append(' ');
            if (_options.ShowTokens)
            {
                sb.Append(COMMENT($"/*{Token(h):X8}*/ "));
            }

            if (!f.ContainsMetadata)
            {
                sb.Append(KEYWORD("nometadata "));
            }

            sb.Append(ANCHORPT(ProperName(name), h));
            PrintLine(sb);
            if (_entryPoint == h)
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

            DumpCustomAttributes(h);
        }
    }

    private void DumpExportedTypeFQN(ExportedTypeDescr e, StringBuilder sb)
    {
        if (e.Implementation.Kind == HandleKind.ExportedType)
        {
            ExportedTypeDescr? outer = _exportedTypes.Find(x => x.Handle == e.Implementation);
            if (outer is not null)
            {
                DumpExportedTypeFQN(outer, sb);
                sb.Append('/');
            }
        }

        sb.Append(JUMPPT(ProperName(e.Name), e.Handle));
    }

    private void DumpImplementation(EntityHandle impl, uint offset)
    {
        if (impl.IsNil)
        {
            return;
        }

        StringBuilder sb = new();
        switch (impl.Kind)
        {
            case HandleKind.AssemblyFile:
            {
                int i = _files.FindIndex(f => f.Handle == impl);
                if (i >= 0)
                {
                    sb.Append(Indent).Append(KEYWORD(".file")).Append(' ').Append(JUMPPT(ProperName(_files[i].Name), impl));
                    if (_options.ShowTokens)
                    {
                        sb.Append(COMMENT($"/*{Token(impl):X8}*/ "));
                    }

                    if (offset != 0xFFFFFFFF)
                    {
                        sb.Append(' ').Append(KEYWORD("at")).Append(" 0x").Append(offset.ToString("X8", CultureInfo.InvariantCulture));
                    }

                    PrintLine(sb);
                }

                break;
            }

            case HandleKind.AssemblyReference:
            {
                int i = _asmRefs.FindIndex(a => a.Handle == impl);
                if (i >= 0)
                {
                    sb.Append(Indent).Append(KEYWORD(".assembly extern")).Append(' ').Append(JUMPPT(ProperName(_asmRefNames[i] ?? _asmRefs[i].Name), impl));
                    if (_options.ShowTokens)
                    {
                        sb.Append(COMMENT($" /*{Token(impl):X8}*/ "));
                    }

                    PrintLine(sb);
                }

                break;
            }

            case HandleKind.ExportedType:
            {
                ExportedTypeDescr? e = _exportedTypes.Find(x => x.Handle == impl);
                if (e is not null)
                {
                    sb.Append(Indent).Append(KEYWORD(".class extern")).Append(' ');
                    DumpExportedTypeFQN(e, sb);
                    if (_options.ShowTokens)
                    {
                        sb.Append(COMMENT($" /*{Token(impl):X8}*/ "));
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
            sb.Append(COMMENT($"/*{Token(e.Handle):X8}*/ "));
        }

        TypeAttributes vis = e.Flags & TypeAttributes.VisibilityMask;
        if (vis == TypeAttributes.Public) sb.Append(KEYWORD("public "));
        if ((e.Flags & TypeAttributeForwarder) != 0) sb.Append(KEYWORD("forwarder "));
        switch (vis)
        {
            case TypeAttributes.NestedPublic: sb.Append(KEYWORD("nested public ")); break;
            case TypeAttributes.NestedPrivate: sb.Append(KEYWORD("nested private ")); break;
            case TypeAttributes.NestedFamily: sb.Append(KEYWORD("nested family ")); break;
            case TypeAttributes.NestedAssembly: sb.Append(KEYWORD("nested assembly ")); break;
            case TypeAttributes.NestedFamANDAssem: sb.Append(KEYWORD("nested famandassem ")); break;
            case TypeAttributes.NestedFamORAssem: sb.Append(KEYWORD("nested famorassem ")); break;
        }

        sb.Append(ANCHORPT(ProperName(e.Name), e.Handle));
        PrintLine(sb);
        PrintLine(Indent + SCOPE());
        PushIndent();
        DumpCustomAttributes(e.Handle);
        DumpImplementation(e.Implementation, 0xFFFFFFFF);
        if ((e.TypeDef & 0xFFFFFF) != 0)
        {
            PrintLine($"{Indent}{KEYWORD(".class")} 0x{e.TypeDef:X8}");
        }

        PopIndent();
        PrintLine(Indent + UNSCOPE());
    }

    // tdForwarder: the exported type is a type forwarder.
    private const TypeAttributes TypeAttributeForwarder = (TypeAttributes)0x00200000;

    private void DumpExportedTypes()
    {
        _exportedTypes.Clear();
        foreach (ExportedTypeHandle h in _md.ExportedTypes)
        {
            ExportedType et = _md.GetExportedType(h);
            string ns = Str(et.Namespace);
            string name = Str(et.Name);
            _exportedTypes.Add(new ExportedTypeDescr
            {
                Handle = h,
                TypeDef = et.GetTypeDefinitionId(),
                Implementation = et.Implementation,
                Name = ns.Length != 0 ? ns + "." + name : name,
                Flags = et.Attributes,
            });
        }

        foreach (ExportedTypeDescr e in _exportedTypes)
        {
            EntityHandle impl = e.Implementation;
            // The ComType of a nested class has its nester's ComType as implementation.
            while (impl.Kind == HandleKind.ExportedType)
            {
                ExportedTypeDescr? outer = _exportedTypes.Find(x => x.Handle == impl);
                if (outer is null)
                {
                    break;
                }

                impl = outer.Implementation;
            }

            if (!impl.IsNil)
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
        List<string> names = new();
        bool first = true;
        bool resourcesRead = false;
        ReadOnlySpan<byte> resources = default;
        foreach (ManifestResourceHandle h in _md.ManifestResources)
        {
            if (first)
            {
                // The names of the resource files cannot conflict with the output and the Win32 resource file.
                string outName = _resourceExtractor?.OutputFileName ?? "";

                names.Add(outName);
                int dot = outName.LastIndexOf('.');
                names.Add((dot < 0 ? outName : outName.Substring(0, dot)) + ".res");
                first = false;
            }

            ManifestResource mr = _md.GetManifestResource(h);
            string name = Str(mr.Name);
            StringBuilder sb = new();
            sb.Append(Indent).Append(KEYWORD(".mresource")).Append(' ');
            if (_options.ShowTokens)
            {
                sb.Append(COMMENT($"/*{Token(h):X8}*/ "));
            }

            ManifestResourceAttributes vis = mr.Attributes & ManifestResourceAttributes.VisibilityMask;
            if (vis == ManifestResourceAttributes.Public) sb.Append(KEYWORD("public "));
            if (vis == ManifestResourceAttributes.Private) sb.Append(KEYWORD("private "));
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
            DumpCustomAttributes(h);
            EntityHandle impl = mr.Implementation;
            uint offset = (uint)mr.Offset;
            if (impl.IsNil)
            {
                if (!resourcesRead)
                {
                    DirectoryEntry resDir = _pe.PEHeaders.CorHeader!.ResourcesDirectory;
                    resources = resDir.RelativeVirtualAddress != 0 ? SpanAtRva((uint)resDir.RelativeVirtualAddress) : default;
                    if (resources.IsEmpty)
                    {
                        PrintError("Failed to read import data.");
                    }

                    resources = resources.Slice(0, Math.Min(resDir.Size, resources.Length));

                    resourcesRead = true;
                }

                if (!resources.IsEmpty)
                {
                    DumpResourceFile(resources, offset, name, fileName);
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

    private void DumpResourceFile(ReadOnlySpan<byte> res, uint offset, string name, string fileName)
    {
        if (offset + 4 > res.Length)
        {
            PrintError($"ERROR retrieving/saving embedded managed resource '{name}' at offset 0x{offset:X8}");
            return;
        }

        uint len = ReadUInt32(res, (int)offset);
        PrintLine(COMMENT($"{Indent}// Offset: 0x{offset:X8} Length: 0x{len:X8}"));
        if (_resourceExtractor is null)
        {
            return;
        }

        try
        {
            int start = (int)offset + 4;
            int count = (int)Math.Min(len, (uint)Math.Max(0, res.Length - start));
            _resourceExtractor.SaveManagedResource(fileName, res.Slice(start, count));
            if (count < len)
            {
                throw new IOException();
            }

            PrintLine(COMMENT($"{Indent}// WARNING: managed resource file {ProperName(fileName)} created"));
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            PrintError($"{Indent}// ERROR: reading managed resource {ProperName(fileName)} at offset 0x{offset:X}");
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
        DumpCustomAttributes(EntityHandle.ModuleDefinition);
        DumpPermissions(EntityHandle.ModuleDefinition);
    }
}
