// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
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

// The IL disassembler. Its output can be reassembled by ilasm with the same result.
internal sealed partial class Dasm
{
    private enum DumpMode
    {
        All,
        Class,
        ClassMethod,
        ClassMethodSig,
    }

    private sealed class MethodImplRecord
    {
        public TypeDefinitionHandle Class;
        public EntityHandle Body;
        public EntityHandle Decl;
        public EntityHandle BodyParent;
    }

    private struct VTableRef
    {
        public EntityHandle Method;
        public int Entry;
        public int Slot;
    }

    private struct EATableRef
    {
        public EntityHandle Method;
        public string? Name;
    }

    private readonly PEReader _pe;
    private readonly MetadataReader _md;
    private readonly PEMemoryBlock _metadataBlock;
    private readonly Options _options;
    private readonly TextWriter _out;
    private readonly TextWriter _error;
    private readonly string _inputFileName;
    private readonly string _newLine;
    private readonly string _lineTerminator;
    private readonly IOutputFormatter _formatter;
    private readonly ICustomAttributeDisassembler _customAttributeDisassembler;
    private readonly IPermissionSetDisassembler _permissionSetDisassembler;
    private readonly IReadOnlyList<IMetadataDisassembler> _metadataDisassemblers;
    private readonly ResourceExtractor? _resourceExtractor;

    private readonly bool _rawEHOption;
    private readonly bool _limitedVisibility;
    private readonly bool _hidePub, _hidePriv, _hideFam, _hideAsm, _hideFAA, _hideFOA, _hidePrivScope;

    private DumpMode _mode;
    private readonly string _classToDumpName = "";
    private readonly string _methodToDump = "";
    private readonly string _sigToDump = "";
    private TypeDefinitionHandle _classToDump;
    private EntityHandle _entryPoint;

    // The classes (all but the global <Module> type) and their enclosing classes (nil for top level ones).
    private readonly List<TypeDefinitionHandle> _classList = new();
    private readonly List<TypeDefinitionHandle> _classEnclosing = new();
    private readonly List<MethodImplRecord> _methodImpls = new();
    private readonly HashSet<EntityHandle> _dups = new();
    private readonly HashSet<CustomAttributeHandle> _caDumped = new();
    private readonly Dictionary<FieldDefinitionHandle, uint> _fieldOffsets = new();
    private readonly List<VTableRef> _vtableRefs = new();
    private readonly List<EATableRef> _eatRefs = new();
    private int _eatBase;
    private string?[] _asmRefNames = Array.Empty<string?>();

    public Dasm(PEReader pe, Options options, TextWriter output, TextWriter error)
    {
        _pe = pe;
        // A PE file without metadata is only accepted for /HEADERS; DumpFile stops after the headers without using metadata.
        if (options.R2RNativeMetadata)
        {
            _md = GetR2RManifestReader(pe);
            _metadataBlock = _r2rBlock;
        }
        else if (pe.HasMetadata)
        {
            _md = pe.GetMetadataReader(MetadataReaderOptions.None);
            _metadataBlock = pe.GetMetadata();
        }
        else
        {
            _md = null!;
        }
        _options = options;
        _out = output;
        _error = error;
        _inputFileName = options.InputFileName ?? "<stream>";
        _newLine = options.NewLine;
        _formatter = options.Formatter;
        _lineTerminator = _formatter.LineTerminator(_newLine);
        _customAttributeDisassembler = options.CustomAttributeDisassembler;
        _permissionSetDisassembler = options.PermissionSetDisassembler;
        _metadataDisassemblers = options.MetadataDisassemblers;
        // Resource files are saved by the extractor as the manifest lists them.
        foreach (IMetadataDisassembler d in _metadataDisassemblers)
        {
            _resourceExtractor ??= d as ResourceExtractor;
        }

        _rawEHOption = options.RawExceptionHandling;

        string? vis = options.Visibility;
        if (options.PublicOnly || !string.IsNullOrEmpty(vis))
        {
            _hidePub = _hidePriv = _hideFam = _hideAsm = _hideFAA = _hideFOA = _hidePrivScope = true;
            if (options.PublicOnly)
            {
                _hidePub = false;
            }

            if (!string.IsNullOrEmpty(vis))
            {
                foreach (string part in vis.Split('+'))
                {
                    switch (part.Trim('"').ToLowerInvariant())
                    {
                        case "pub": _hidePub = false; break;
                        case "pri": _hidePriv = false; break;
                        case "fam": _hideFam = false; break;
                        case "asm": _hideAsm = false; break;
                        case "faa": _hideFAA = false; break;
                        case "foa": _hideFOA = false; break;
                        case "psc": _hidePrivScope = false; break;
                    }
                }
            }

            _limitedVisibility = _hidePub || _hidePriv || _hideFam || _hideAsm || _hideFAA || _hideFOA || _hidePrivScope;
        }

        if (!string.IsNullOrEmpty(options.Item))
        {
            string item = options.Item;
            int p = item.IndexOf(':');
            if (p < 0)
            {
                _mode = DumpMode.Class;
                _classToDumpName = item;
            }
            else
            {
                if (p + 1 >= item.Length || item[p + 1] != ':')
                {
                    throw new ArgumentException("Invalid item specification: " + item);
                }

                _classToDumpName = item.Substring(0, p);
                string rest = item.Substring(p + 2);
                int q = rest.IndexOf('(');
                if (q < 0)
                {
                    _mode = DumpMode.ClassMethod;
                    _methodToDump = rest;
                }
                else
                {
                    _mode = DumpMode.ClassMethodSig;
                    _methodToDump = rest.Substring(0, q);
                    _sigToDump = rest.Substring(q + 1);
                }
            }
        }
    }

    private bool ShowCustomAttributes => !_options.NoCustomAttributes;

    internal Options Options => _options;

    internal PEReader PE => _pe;

    internal MetadataReader Metadata => _md;

    // The classes, all but the global <Module> type.
    internal IReadOnlyList<TypeDefinitionHandle> Classes => _classList;

    // Runs the metadata disassemblers of a phase; false if there are none.
    private bool RunMetadataDisassemblers(DisassemblyPhase phase)
    {
        bool any = false;
        foreach (IMetadataDisassembler d in _metadataDisassemblers)
        {
            if (d.Phase == phase)
            {
                d.Disassemble(this);
                any = true;
            }
        }

        return any;
    }

    // ---------------------------------------------------------------- PE access

    // The bytes from rva to the end of its section (at most length of them), without copying them; false when rva is not
    // in a section.
    internal bool TryGetBytesAtRva(uint rva, int length, out ReadOnlySpan<byte> bytes)
    {
        bytes = SpanAtRva(rva);
        if (bytes.IsEmpty)
        {
            return false;
        }

        // A negative length is the whole section.
        if ((uint)length < (uint)bytes.Length)
        {
            bytes = bytes.Slice(0, length);
        }

        return true;
    }

    // The bytes from rva to the end of its section, without copying them; empty when rva is not in a section.
    internal unsafe ReadOnlySpan<byte> SpanAtRva(uint rva)
    {
        if (rva == 0)
        {
            return default;
        }

        try
        {
            PEMemoryBlock block = _pe.GetSectionData((int)rva);
            return block.Length == 0 ? default : new ReadOnlySpan<byte>(block.Pointer, block.Length);
        }
        catch (Exception e) when (e is ArgumentOutOfRangeException or BadImageFormatException)
        {
            return default;
        }
    }

    private bool IsPE32 => _pe.PEHeaders.PEHeader!.Magic == PEMagic.PE32;

    private SectionHeader? SectionOf(uint rva)
    {
        foreach (SectionHeader sh in _pe.PEHeaders.SectionHeaders)
        {
            if (rva >= (uint)sh.VirtualAddress && rva < (uint)sh.VirtualAddress + (uint)sh.VirtualSize)
            {
                return sh;
            }
        }

        return null;
    }

    // The raw bytes of a section, read from the file: what is past the end of the file reads as zeros.
    private ReadOnlySpan<byte> RawSectionBytes(SectionHeader sh, uint rva, uint count)
    {
        long fileOffset = sh.PointerToRawData + (rva - (uint)sh.VirtualAddress);
        ReadOnlySpan<byte> image = Image.AsSpan();
        if (fileOffset + count <= image.Length)
        {
            return image.Slice((int)fileOffset, (int)count);
        }

        byte[] result = new byte[count];
        if (fileOffset < image.Length)
        {
            image.Slice((int)fileOffset).CopyTo(result);
        }

        return result;
    }

    // Section names are compared as C strings: ilasm pads them with garbage after the terminating NUL.
    internal static string CSectionName(SectionHeader sh)
    {
        int z = sh.Name.IndexOf('\0');
        return z < 0 ? sh.Name : sh.Name.Substring(0, z);
    }

    // ---------------------------------------------------------------- enumeration

    private void EnumClasses()
    {
        bool first = true;
        foreach (TypeDefinitionHandle h in _md.TypeDefinitions)
        {
            // COMPAT: skip the global <Module> type (the first row).
            if (!first)
            {
                _classList.Add(h);
            }

            first = false;
        }

        foreach (TypeDefinitionHandle cl in _classList)
        {
            if (_mode is DumpMode.Class or DumpMode.ClassMethod or DumpMode.ClassMethodSig)
            {
                bool tokens = _options.ShowTokens;
                _options.ShowTokens = false;
                try
                {
                    if (PrettyPrintClass(cl) == _classToDumpName)
                    {
                        _classToDump = cl;
                    }
                }
                finally
                {
                    _options.ShowTokens = tokens;
                }
            }

            TypeDefinition td = _md.GetTypeDefinition(cl);
            _classEnclosing.Add(td.GetDeclaringType());

            foreach (MethodImplementationHandle mih in td.GetMethodImplementations())
            {
                MethodImplementation mi = _md.GetMethodImplementation(mih);
                _methodImpls.Add(new MethodImplRecord
                {
                    Class = cl,
                    Body = mi.MethodBody,
                    Decl = mi.MethodDeclaration,
                    BodyParent = GetParent(mi.MethodBody),
                });
            }
        }

        // nesting consistency
        for (int i = 0; i < _classList.Count; i++)
        {
            TypeDefinitionHandle tkThis = _classList[i];
            TypeDefinitionHandle tkEncloser = _classEnclosing[i];
            TypeDefinitionHandle tkPrevLevel = tkThis;
            while (!tkEncloser.IsNil)
            {
                if (tkThis == tkEncloser)
                {
                    PrintError($"error : class {Token(tkThis):X8} is nested in itself");
                    _classEnclosing[i] = default;
                    break;
                }

                int j = _classList.IndexOf(tkEncloser);
                if (j < 0)
                {
                    PrintError($"error : class {Token(tkPrevLevel):X8} is nested in missing class {Token(tkEncloser):x8}");
                    _classEnclosing[i] = default;
                    break;
                }

                tkPrevLevel = tkEncloser;
                tkEncloser = _classEnclosing[j];
            }
        }

        if (_classList.Count == 0)
        {
            return;
        }

        // class dups
        (string n0, string ns0) = GetNameOfTypeDef(_classList[0]);
        if (_classEnclosing[0].IsNil && n0 == "<Module>" && ns0.Length == 0)
        {
            _dups.Add(_classList[0]);
        }

        Dictionary<(TypeDefinitionHandle, string, string), int> seen = new();
        seen[(_classEnclosing[0], n0, ns0)] = 0;
        for (int i = 1; i < _classList.Count; i++)
        {
            (string n, string ns) = GetNameOfTypeDef(_classList[i]);
            if (!seen.TryAdd((_classEnclosing[i], n, ns), i))
            {
                _dups.Add(_classList[i]);
            }
        }

        // field and method dups, including the global ones
        for (int i = 0; i <= _classList.Count; i++)
        {
            TypeDefinition td = i == 0 ? GlobalType() : _md.GetTypeDefinition(_classList[i - 1]);
            if (i == 0 && RowCount(TableIndex.TypeDef) == 0)
            {
                continue;
            }

            HashSet<(string, ImmutableArray<byte>)> methods = new(s_nameAndSignatureComparer);
            foreach (MethodDefinitionHandle h in td.GetMethods())
            {
                MethodDefinition m = _md.GetMethodDefinition(h);
                if (!methods.Add((Str(m.Name), _md.GetBlobContent(m.Signature))))
                {
                    _dups.Add(h);
                }
            }

            HashSet<(string, ImmutableArray<byte>)> fields = new(s_nameAndSignatureComparer);
            foreach (FieldDefinitionHandle h in td.GetFields())
            {
                FieldDefinition f = _md.GetFieldDefinition(h);
                if (!fields.Add((Str(f.Name), _md.GetBlobContent(f.Signature))))
                {
                    _dups.Add(h);
                }
            }
        }
    }

    // Members are duplicates when they have the same name and signature bytes.
    private static readonly IEqualityComparer<(string Name, ImmutableArray<byte> Signature)> s_nameAndSignatureComparer =
        EqualityComparer<(string Name, ImmutableArray<byte> Signature)>.Create(
            (a, b) => a.Name == b.Name && a.Signature.AsSpan().SequenceEqual(b.Signature.AsSpan()),
            x => HashCode.Combine(x.Name, x.Signature.Length));

    private void DumpMscorlib()
    {
        if (FindTypeDefByName("System", "Object", out TypeDefinitionHandle obj))
        {
            TypeDefinition td = _md.GetTypeDefinition(obj);
            bool isClass = (td.Attributes & TypeAttributes.ClassSemanticsMask) == TypeAttributes.Class;
            if (isClass && !IsValid(td.BaseType))
            {
                PrintLine("");
                PrintLine($"{Indent}{KEYWORD(".mscorlib")} ");
                PrintLine("");
            }
        }
    }

    // ---------------------------------------------------------------- the whole file

    public bool DumpFile()
    {
        try
        {
            return DumpFileCore();
        }
        finally
        {
            _pdbProvider?.Dispose();
            _pdbProvider = null;
            _pdb = null;
        }
    }

    private bool DumpFileCore()
    {
        _out.Write(_formatter.Prolog(_inputFileName, _newLine));
        DumpPreamble();

        CorHeader? cor = _pe.PEHeaders.CorHeader;
        if (cor is null)
        {
            PrintError($"error : '{_inputFileName}' has no valid CLR header and cannot be disassembled");
            RunMetadataDisassemblers(DisassemblyPhase.Headers);
            return false;
        }

        if (cor.MajorRuntimeVersion == 1 || cor.MajorRuntimeVersion > 2)
        {
            PrintError($"CORHeader->MajorRuntimeVersion = {cor.MajorRuntimeVersion}");
            PrintError("error : Bad CLR header, or the version of the file format is not supported");
            return false;
        }

        _entryPoint = TryGetHandle(cor.EntryPointTokenOrRelativeVirtualAddress, out EntityHandle entryPoint) ? entryPoint : default;
        OpenPdb();

        EnumClasses();

        // The headers, statistics and metadata info go inside a "multiline comment" block (empty in plain text).
        PrintLine(CommentPrefix);
        if (!RunMetadataDisassemblers(DisassemblyPhase.Headers))
        {
            DumpVTables();
        }

        RunMetadataDisassemblers(DisassemblyPhase.Summary);
        PrintLine(CommentPostfix);

        if (!_options.NoIL)
        {
            if (!_classToDump.IsNil)
            {
                DumpClass(TopEncloser(_classToDump), ClassParts.All);
                PrintLine(COMMENT("// *********** DISASSEMBLY COMPLETE ***********************"));
                return FinishFile();
            }

            DumpMscorlib();
            RunMetadataDisassemblers(DisassemblyPhase.BeforeManifest);

            DumpManifest();
            PrintLine("");

            if (_options.ForwardDeclarations && _classList.Count != 0)
            {
                PrintLine(COMMENT("//"));
                PrintLine(COMMENT("// ============== CLASS STRUCTURE DECLARATION =================="));
                PrintLine(COMMENT("//"));
                for (int i = 0; i < _classList.Count; i++)
                {
                    if (_classEnclosing[i].IsNil)
                    {
                        DumpClass(_classList[i], ClassParts.NestedClasses);
                    }
                }

                PrintLine("");
                PrintLine(COMMENT("// ============================================================="));
                PrintLine("");
            }

            DumpGlobalFields();
            DumpGlobalMethods();

            if (_classList.Count != 0)
            {
                PrintLine("");
                PrintLine(COMMENT("// =============== CLASS MEMBERS DECLARATION ==================="));
                if (_options.ForwardDeclarations)
                {
                    PrintLine(COMMENT("//   note that class flags, 'extends' and 'implements' clauses"));
                    PrintLine(COMMENT("//          are provided here for information only"));
                }

                PrintLine("");
                for (int i = 0; i < _classList.Count; i++)
                {
                    if (_classEnclosing[i].IsNil)
                    {
                        DumpClass(_classList[i], ClassParts.All);
                    }
                }

                PrintLine("");
                PrintLine(COMMENT("// ============================================================="));
                PrintLine("");
            }

            if (ShowCustomAttributes)
            {
                foreach (CustomAttributeHandle h in _md.CustomAttributes)
                {
                    if (!_caDumped.Contains(h))
                    {
                        DumpCustomAttribute(h, withOwner: true);
                    }
                }
            }

            DumpDataSections();
            PrintLine(COMMENT("// *********** DISASSEMBLY COMPLETE ***********************"));
        }

        return FinishFile();
    }

    private bool FinishFile()
    {
        RunMetadataDisassemblers(DisassemblyPhase.End);
        _out.Write(_formatter.Epilog(_newLine));
        return true;
    }

    private void DumpPreamble()
    {
        PrintLine("");
        if (_formatter.TitlePrefix is string titlePrefix)
        {
            PrintLine(titlePrefix);
        }

        PrintLine(COMMENT("//  .NET IL Disassembler.  Version " + ProductVersion));
        if (_formatter.TitlePostfix is string titlePostfix)
        {
            PrintLine(titlePostfix);
        }

        PrintLine("");
        if (_limitedVisibility || !ShowCustomAttributes || _options.NoIL || _mode != DumpMode.All)
        {
            PrintLine("");
            PrintLine(COMMENT("// warning : THIS IS A PARTIAL DISASSEMBLY, NOT SUITABLE FOR RE-ASSEMBLING"));
            PrintLine("");
        }

        if (_limitedVisibility)
        {
            StringBuilder sb = new("// Only shown items having accessibility:");
            if (!_hidePub) sb.Append(" Public");
            if (!_hidePriv) sb.Append(" Private");
            if (!_hideFam) sb.Append(" Family");
            if (!_hideAsm) sb.Append(" Assembly");
            if (!_hideFAA) sb.Append(" FamilyANDAssembly");
            if (!_hidePrivScope) sb.Append(" PrivateScope");
            PrintLine(COMMENT(sb.ToString()));
        }
    }

    internal static string ProductVersion
    {
        get
        {
            // The SDK always generates the attribute.
            string v = typeof(Dasm).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion;
            int plus = v.IndexOf('+');
            return plus >= 0 ? v.Substring(0, plus) : v;
        }
    }

    // ---------------------------------------------------------------- vtable fixups and exports

    // The COR_VTABLE_* flags of a vtable fixup (IMAGE_COR_VTABLEFIXUP.Type); SRM does not define them.
    [Flags]
    private enum VTableFixupFlags : ushort
    {
        Int32 = 0x01,
        Int64 = 0x02,
        FromUnmanaged = 0x04,
        FromUnmanagedRetainAppDomain = 0x08,
        CallMostDerived = 0x10,
    }

    // IMAGE_COR_VTABLEFIXUP: RVA (4), Count (2), Type (2).
    private const int VTableFixupSize = 8;

    internal void DumpVTables()
    {
        CorHeader cor = _pe.PEHeaders.CorHeader!;
        DirectoryEntry dir = cor.VtableFixupsDirectory;
        if (dir.RelativeVirtualAddress == 0)
        {
            return;
        }

        PrintLine("// VTableFixup Directory:");
        int count = dir.Size / VTableFixupSize;
        if (!TryGetBytesAtRva((uint)dir.RelativeVirtualAddress, dir.Size, out ReadOnlySpan<byte> fixups) || fixups.Length < dir.Size)
        {
            PrintLine("// Failed to get vtable fix-up table.");
            PrintLine("");
            return;
        }

        for (int i = 0; i < count; i++)
        {
            ReadOnlySpan<byte> fixup = fixups.Slice(i * VTableFixupSize);
            uint rva = BinaryPrimitives.ReadUInt32LittleEndian(fixup);
            ushort n = BinaryPrimitives.ReadUInt16LittleEndian(fixup.Slice(4));
            VTableFixupFlags type = (VTableFixupFlags)BinaryPrimitives.ReadUInt16LittleEndian(fixup.Slice(6));
            PrintLine($"//   IMAGE_COR_VTABLEFIXUP[{i}]:");
            PrintLine($"//       RVA:               0x{rva:x8}");
            PrintLine($"//       Count:             0x{n:x4}");
            PrintLine($"//       Type:              0x{(ushort)type:x4}");
            ReadOnlySpan<byte> slots = SpanAtRva(rva);
            if (slots.IsEmpty)
            {
                PrintLine("// Bogus RVA for entry.");
                continue;
            }

            int p = 0;
            for (int s = 0; s < n; s++)
            {
                if ((type & VTableFixupFlags.Int32) != 0)
                {
                    uint tok = ReadUInt32(slots, p);
                    PrintLine($"//         [0x{s:x4}]            (0x{tok:x8})");
                    p += 4;
                    if (!TryGetHandle((int)tok, out Handle h))
                    {
                        PrintError($"error :  invalid token ({tok:x8})");
                    }
                    else if (h.Kind != HandleKind.MethodDefinition)
                    {
                        PrintError($"error : unexpected token type, expected {(int)HandleKind.MethodDefinition:x2}, got {(int)h.Kind:x2}");
                    }
                }
                else
                {
                    ulong v = BinaryPrimitives.ReadUInt64LittleEndian(slots.Slice(p));
                    PrintLine($"//         [0x{s:x4}]            (0x{v,16:x})");
                    p += 8;
                    if (!TryGetHandle((int)(uint)v, out Handle _))
                    {
                        PrintError($"error :  invalid token ({(uint)v:x8})");
                    }
                }
            }
        }

        PrintLine("");
    }

    private void DumpVtable()
    {
        PEHeader pe = _pe.PEHeaders.PEHeader!;
        CorHeader cor = _pe.PEHeaders.CorHeader!;
        if (IsPE32)
        {
            PrintLine($"{Indent}{KEYWORD(".imagebase")} 0x{pe.ImageBase:x8}");
            PrintLine($"{Indent}{KEYWORD(".file alignment")} 0x{pe.FileAlignment:x8}");
            PrintLine($"{Indent}{KEYWORD(".stackreserve")} 0x{pe.SizeOfStackReserve:x8}");
        }
        else
        {
            PrintLine($"{Indent}{KEYWORD(".imagebase")} 0x{pe.ImageBase:x16}");
            PrintLine($"{Indent}{KEYWORD(".file alignment")} 0x{pe.FileAlignment:x8}");
            PrintLine($"{Indent}{KEYWORD(".stackreserve")} 0x{pe.SizeOfStackReserve:x16}");
        }

        string subsystem = pe.Subsystem switch
        {
            Subsystem.Unknown => "// UNKNOWN",
            Subsystem.Native => "// NATIVE",
            Subsystem.WindowsGui => "// WINDOWS_GUI",
            Subsystem.WindowsCui => "// WINDOWS_CUI",
            Subsystem.OS2Cui => "// OS2_CUI",
            Subsystem.PosixCui => "// POSIX_CUI",
            Subsystem.NativeWindows => "// NATIVE_WINDOWS",
            Subsystem.WindowsCEGui => "// WINDOWS_CE_GUI",
            Subsystem.EfiApplication => "// EFI_APPLICATION",
            Subsystem.EfiBootServiceDriver => "// EFI_BOOT_SERVICE_DRIVER",
            Subsystem.EfiRuntimeDriver => "// EFI_RUNTIME_DRIVER",
            Subsystem.EfiRom => "// EFI_ROM",
            Subsystem.Xbox => "// XBOX",
            _ => "// <illegal value>",
        };
        PrintLine($"{Indent}{KEYWORD(".subsystem")} 0x{(int)pe.Subsystem:x4}       {COMMENT(subsystem)}");

        CorFlags flags = cor.Flags;
        StringBuilder sb = new();
        sb.Append(Indent).Append(KEYWORD(".corflags")).Append(" 0x").Append(((uint)flags).ToString("x8", CultureInfo.InvariantCulture));
        if (flags != 0)
        {
            StringBuilder c = new("    // ");
            if ((flags & CorFlags.ILOnly) != 0) c.Append(" ILONLY");
            if ((flags & CorFlags.Requires32Bit) != 0 && (flags & CorFlags.Prefers32Bit) == 0) c.Append(" 32BITREQUIRED");
            if ((flags & CorFlags.Requires32Bit) != 0 && (flags & CorFlags.Prefers32Bit) != 0) c.Append(" 32BITPREFERRED");
            if ((flags & CorFlags.ILLibrary) != 0) c.Append(" IL_LIBRARY");
            if ((flags & CorFlags.TrackDebugData) != 0) c.Append(" TRACKDEBUGDATA");
            sb.Append(COMMENT(c.ToString()));
        }

        PrintLine(sb);

        DumpEATEntries();

        _vtableRefs.Clear();
        DirectoryEntry vt = cor.VtableFixupsDirectory;
        if (vt.Size != 0)
        {
            SectionHeader? shn = SectionOf((uint)vt.RelativeVirtualAddress);
            if (shn is null)
            {
                return;
            }

            ReadOnlySpan<byte> entries = RawSectionBytes(shn.Value, (uint)vt.RelativeVirtualAddress, (uint)vt.Size);
            int k = 0;
            for (int j = vt.Size; j > 0; j -= VTableFixupSize, k++)
            {
                int off = k * VTableFixupSize;
                if (off + VTableFixupSize > entries.Length)
                {
                    break;
                }

                uint addr = BinaryPrimitives.ReadUInt32LittleEndian(entries.Slice(off));
                ushort count = BinaryPrimitives.ReadUInt16LittleEndian(entries.Slice(off + 4));
                VTableFixupFlags type = (VTableFixupFlags)BinaryPrimitives.ReadUInt16LittleEndian(entries.Slice(off + 6));
                sb.Clear();
                sb.Append(Indent).Append(KEYWORD(".vtfixup")).Append(" [").Append(count.ToString(CultureInfo.InvariantCulture)).Append("] ");
                uint size = (uint)count * 4;
                if ((type & VTableFixupFlags.Int32) != 0)
                {
                    sb.Append(KEYWORD("int32 "));
                }
                else if ((type & VTableFixupFlags.Int64) != 0)
                {
                    sb.Append(KEYWORD("int64 "));
                    size <<= 1;
                }

                if ((type & VTableFixupFlags.FromUnmanaged) != 0) sb.Append(KEYWORD("fromunmanaged "));
                if ((type & VTableFixupFlags.CallMostDerived) != 0) sb.Append(KEYWORD("callmostderived "));
                if ((type & VTableFixupFlags.FromUnmanagedRetainAppDomain) != 0) sb.Append(KEYWORD("retainappdomain "));
                sb.Append(KEYWORD("at "));
                DumpDataPtr(sb, addr, size);
                ReadOnlySpan<byte> slots = SpanAtRva(addr);
                if (!slots.IsEmpty)
                {
                    StringBuilder c = new(" //");
                    int p = 0;
                    for (int s = 0; s < count; s++)
                    {
                        uint tok = ReadUInt32(slots, p);
                        if ((type & VTableFixupFlags.Int32) != 0)
                        {
                            c.Append(' ').Append(tok.ToString("X8", CultureInfo.InvariantCulture));
                            p += 4;
                        }
                        else
                        {
                            c.Append(' ').Append(BinaryPrimitives.ReadUInt64LittleEndian(slots.Slice(p)).ToString("X16", CultureInfo.InvariantCulture));
                            p += 8;
                        }

                        _vtableRefs.Add(new VTableRef { Method = TryGetHandle((int)tok, out EntityHandle m) ? m : default, Entry = k, Slot = s });
                    }

                    sb.Append(COMMENT(c.ToString()));
                }
                else
                {
                    sb.Append(' ').Append(ERRORMSG("// Bogus RVA for entry."));
                }

                PrintLine(sb);
            }
        }
    }

    // The export directory (IMAGE_EXPORT_DIRECTORY), which SRM does not read: exported methods are reached through
    // stubs that hold the address of their vtable fixup slot.
    private void DumpEATEntries()
    {
        try
        {
            PEHeader pe = _pe.PEHeaders.PEHeader!;
            DirectoryEntry exportDir = pe.ExportTableDirectory;
            if (pe.NumberOfRvaAndSizes == 0 || exportDir.Size == 0)
            {
                return;
            }

            if (!TryGetBytesAtRva((uint)exportDir.RelativeVirtualAddress, 40, out ReadOnlySpan<byte> table) || table.Length < 40)
            {
                return;
            }

            uint ordinalBase = ReadUInt32(table, 16);
            uint numAT = ReadUInt32(table, 20);
            uint numNames = ReadUInt32(table, 24);
            uint addrTable = ReadUInt32(table, 28);
            uint namePtr = ReadUInt32(table, 32);
            uint ordTable = ReadUInt32(table, 36);
            if (numAT != 0 && addrTable != 0)
            {
                TryGetBytesAtRva(addrTable, (int)numAT * 4, out ReadOnlySpan<byte> addrs);
                _eatRefs.Clear();
                for (int j = 0; j < numAT; j++)
                {
                    EATableRef r = new();
                    uint entryRva = ReadUInt32(addrs, j * 4);
                    if (TryGetBytesAtRva(entryRva, 10, out ReadOnlySpan<byte> cont))
                    {
                        uint tokRva = IsPE32
                            ? ReadUInt32(cont, 2) - (uint)pe.ImageBase
                            : (uint)(BinaryPrimitives.ReadUInt64LittleEndian(cont.Slice(2)) - (uint)pe.ImageBase);
                        if (TryGetBytesAtRva(tokRva, 4, out ReadOnlySpan<byte> tok) && TryGetHandle((int)ReadUInt32(tok, 0), out EntityHandle m))
                        {
                            r.Method = m;
                        }
                    }

                    _eatRefs.Add(r);
                }
            }

            if (numNames != 0 && namePtr != 0 && ordTable != 0)
            {
                TryGetBytesAtRva(namePtr, (int)numAT * 4, out ReadOnlySpan<byte> names);
                TryGetBytesAtRva(ordTable, (int)numAT * 2, out ReadOnlySpan<byte> ords);
                for (int j = 0; j < numAT; j++)
                {
                    uint nameRva = ReadUInt32(names, j * 4);
                    int ord = BinaryPrimitives.ReadUInt16LittleEndian(ords.Slice(j * 2));
                    ReadOnlySpan<byte> nameBytes = SpanAtRva(nameRva);
                    string? name = null;
                    if (!nameBytes.IsEmpty)
                    {
                        int z = nameBytes.IndexOf((byte)0);
                        name = Encoding.UTF8.GetString(z < 0 ? nameBytes : nameBytes.Slice(0, z));
                    }

                    if (ord < _eatRefs.Count)
                    {
                        _eatRefs[ord] = new EATableRef { Method = _eatRefs[ord].Method, Name = name };
                    }
                }
            }

            _eatBase = (int)ordinalBase;
        }
        catch (Exception e) when (e is IndexOutOfRangeException or ArgumentException)
        {
            PrintError("// ERROR READING EXPORT ADDRESS TABLE");
            _eatRefs.Clear();
        }
    }
}
