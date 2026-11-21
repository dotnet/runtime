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
        public int TkClass;
        public int TkBody;
        public int TkDecl;
        public int TkBodyParent;
    }

    private struct VTableRef
    {
        public int Tok;
        public int Entry;
        public int Slot;
    }

    private struct EATableRef
    {
        public int Tok;
        public string? Name;
    }

    private readonly PEReader _pe;
    private readonly MetadataReader _md;
    private readonly PEMemoryBlock _metadataBlock;
    private readonly Options _options;
    private readonly TextWriter _out;
    private readonly TextWriter _error;
    private readonly bool _toFile;
    private readonly string _outputFileName;
    private readonly bool _html;
    private readonly bool _rtf;
    private readonly string _newLine;

    private readonly bool _rawEHOption;
    private readonly bool _limitedVisibility;
    private readonly bool _hidePub, _hidePriv, _hideFam, _hideAsm, _hideFAA, _hideFOA, _hidePrivScope;

    private DumpMode _mode;
    private readonly string _classToDumpName = "";
    private readonly string _methodToDump = "";
    private readonly string _sigToDump = "";
    private int _classToDump;

    private readonly List<int> _classList = new();
    private readonly List<int> _classEnclosing = new();
    private readonly List<MethodImplRecord> _methodImpls = new();
    private readonly HashSet<int> _dups = new();
    private readonly List<TypeDefDescr> _typedefs = new();
    private readonly HashSet<int> _caDumped = new();
    private readonly Dictionary<int, uint> _fieldOffsets = new();
    private readonly List<VTableRef> _vtableRefs = new();
    private readonly List<EATableRef> _eatRefs = new();
    private int _eatBase;
    private string?[] _asmRefNames = Array.Empty<string?>();

    // Data referenced through field RVAs and vtable fixups, dumped as .data at the end.
    private readonly List<(uint Rva, uint Size)> _ptrTags = new();

    public Dasm(PEReader pe, Options options, TextWriter output, TextWriter error, bool toFile, string outputFileName, string newLine = "\n")
    {
        _newLine = newLine;
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
        _toFile = toFile;
        _rawEHOption = options.RawExceptionHandling;
        _outputFileName = outputFileName;
        _html = toFile && options.Html && !options.Rtf;
        _rtf = toFile && options.Rtf;

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

    // ---------------------------------------------------------------- PE access

    private byte[]? GetBytesAtRva(uint rva, int length)
    {
        if (rva == 0)
        {
            return null;
        }

        PEMemoryBlock block;
        try
        {
            block = _pe.GetSectionData((int)rva);
        }
        catch (Exception e) when (e is ArgumentOutOfRangeException or BadImageFormatException)
        {
            return null;
        }

        if (block.Length == 0)
        {
            return null;
        }

        ImmutableArray<byte> content = block.GetContent();
        int n = length < 0 ? content.Length : Math.Min(length, content.Length);
        byte[] b = new byte[n];
        content.CopyTo(0, b, 0, n);
        return b;
    }

    // The bytes from rva to the end of its section, without copying them; empty when rva is not in a section.
    private unsafe ReadOnlySpan<byte> SpanAtRva(uint rva)
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

    // Section names are compared as C strings: ilasm pads them with garbage after the terminating NUL.
    private static string CSectionName(SectionHeader sh)
    {
        int z = sh.Name.IndexOf('\0');
        return z < 0 ? sh.Name : sh.Name.Substring(0, z);
    }

    private static string DataLabelPrefix(SectionHeader sh) => CSectionName(sh) switch
    {
        ".tls" => "T_",
        ".text" => "I_",
        _ => "D_",
    };

    // DumpDataPtr: the label of data at rva, registering it for the .data dump at the end.
    private void DumpDataPtr(StringBuilder sb, uint ptr, uint size)
    {
        SectionHeader? sh = SectionOf(ptr);
        if (sh is not null)
        {
            // The computed size can over-estimate (auto layout); the data cannot extend past its section.
            ulong sectionEnd = (ulong)(uint)sh.Value.VirtualAddress + (uint)sh.Value.VirtualSize;
            if (size != UnknownSize && ptr + (ulong)size > sectionEnd)
            {
                size = (uint)(sectionEnd - ptr);
            }

            int j = _ptrTags.FindIndex(t => t.Rva == ptr);
            if (j < 0)
            {
                _ptrTags.Add((ptr, size));
            }
            else if (_ptrTags[j].Size < size)
            {
                _ptrTags[j] = (ptr, size);
            }

            sb.Append(DataLabelPrefix(sh.Value)).Append(ptr.ToString("X8", CultureInfo.InvariantCulture));
        }
        else
        {
            sb.Append(ERRORMSG($"0x{ptr:X8} /* WARNING: rogue pointer! (size 0x{size:X8}) */"));
        }
    }

    // ---------------------------------------------------------------- field sizes

    // The size of the data of an RVA field, computed from the layout like the runtime does for sequential and
    // explicit layout (an upper bound for auto layout, which only ever packs tighter). 0xFFFFFFFF means unknown: the data then extends to the next label.
    private const uint UnknownSize = 0xFFFFFFFF;

    private readonly Dictionary<int, (uint Size, uint Align)> _valueTypeLayouts = new();

    private uint SizeOfField(int tk)
    {
        if (TypeFromToken(tk) != mdtFieldDef || RidFromToken(tk) == 0)
        {
            return UnknownSize;
        }

        byte[] sig = GetSigOfFieldDef(tk);
        try
        {
            int p = 0;
            if (sig.Length == 0 || !IsCallConv(UncompressData(sig, ref p), IMAGE_CEE_CS_CALLCONV_FIELD))
            {
                return UnknownSize;
            }

            return LayoutOfType(sig, ref p, 0).Size;
        }
        catch (IndexOutOfRangeException)
        {
            return UnknownSize;
        }
    }

    private (uint Size, uint Align) LayoutOfType(byte[] sig, ref int p, int depth)
    {
        uint ptrSize = IsPE32 ? 4u : 8u;
        while (true)
        {
            byte typ = sig[p++];
            switch (typ)
            {
                case ELEMENT_TYPE_BOOLEAN:
                case ELEMENT_TYPE_I1:
                case ELEMENT_TYPE_U1:
                    return (1, 1);
                case ELEMENT_TYPE_CHAR:
                case ELEMENT_TYPE_I2:
                case ELEMENT_TYPE_U2:
                    return (2, 2);
                case ELEMENT_TYPE_I4:
                case ELEMENT_TYPE_U4:
                case ELEMENT_TYPE_R4:
                    return (4, 4);
                case ELEMENT_TYPE_I8:
                case ELEMENT_TYPE_U8:
                case ELEMENT_TYPE_R8:
                    return (8, 8);
                case ELEMENT_TYPE_OBJECT:
                case ELEMENT_TYPE_STRING:
                case ELEMENT_TYPE_CLASS:
                case ELEMENT_TYPE_PTR:
                case ELEMENT_TYPE_BYREF:
                case ELEMENT_TYPE_FNPTR:
                case ELEMENT_TYPE_I:
                case ELEMENT_TYPE_U:
                case ELEMENT_TYPE_SZARRAY:
                case ELEMENT_TYPE_ARRAY:
                    return (ptrSize, ptrSize);
                case ELEMENT_TYPE_TYPEDBYREF:
                    return (ptrSize * 2, ptrSize);
                case ELEMENT_TYPE_VALUETYPE:
                    return depth > 64 ? (UnknownSize, 1) : LayoutOfValueType(UncompressToken(sig, ref p), depth + 1);
                case ELEMENT_TYPE_CMOD_OPT:
                case ELEMENT_TYPE_CMOD_REQD:
                    UncompressToken(sig, ref p);
                    continue;
                case ELEMENT_TYPE_PINNED:
                    continue;
                default:
                    // void, generic parameters and instantiations, and anything unexpected.
                    return (UnknownSize, 1);
            }
        }
    }

    private (uint Size, uint Align) LayoutOfValueType(int tk, int depth)
    {
        if (TypeFromToken(tk) != mdtTypeDef || !IsValidToken(tk))
        {
            return (UnknownSize, 1);
        }

        if (_valueTypeLayouts.TryGetValue(tk, out (uint, uint) known))
        {
            return known;
        }

        // Guards against cycles through invalid metadata.
        _valueTypeLayouts[tk] = (UnknownSize, 1);
        TypeDefinition td = _md.GetTypeDefinition((TypeDefinitionHandle)Handle(tk));
        int a = (int)td.Attributes;
        bool hasLayout = HasClassLayoutRow(tk);
        TypeLayout layout = td.GetLayout();
        uint pack = hasLayout && layout.PackingSize != 0 ? (uint)layout.PackingSize : 8;
        uint classSize = hasLayout ? (uint)layout.Size : 0;
        bool isExplicit = (a & 0x18) == 0x10;
        uint size = 0;
        uint align = 1;
        foreach (FieldDefinitionHandle fh in td.GetFields())
        {
            FieldDefinition fd = _md.GetFieldDefinition(fh);
            if ((fd.Attributes & (FieldAttributes.Static | FieldAttributes.Literal)) != 0)
            {
                continue;
            }

            byte[] fsig = Blob(fd.Signature);
            int p = 1;
            (uint fieldSize, uint fieldAlign) = LayoutOfType(fsig, ref p, depth);
            if (fieldSize == UnknownSize)
            {
                return _valueTypeLayouts[tk] = (UnknownSize, 1);
            }

            fieldAlign = Math.Min(fieldAlign, pack);
            align = Math.Max(align, fieldAlign);
            if (isExplicit)
            {
                int offset = fd.GetOffset();
                size = Math.Max(size, (offset < 0 ? 0 : (uint)offset) + fieldSize);
            }
            else
            {
                size = (size + fieldAlign - 1) / fieldAlign * fieldAlign + fieldSize;
            }
        }

        size = (size + align - 1) / align * align;
        size = Math.Max(Math.Max(size, classSize), 1);
        return _valueTypeLayouts[tk] = (size, align);
    }

    // ---------------------------------------------------------------- enumeration

    private void EnumClasses()
    {
        ImmutableArray<TypeDefinitionHandle> all = Arr(_md.TypeDefinitions);
        // IMDInternalImport::EnumTypeDefInit skips the global <Module> type (the first row).
        for (int i = 1; i < all.Length; i++)
        {
            _classList.Add(Token(all[i]));
        }

        foreach (int cl in _classList)
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

            int enclosing = GetEnclosingClass(cl);
            _classEnclosing.Add(RidFromToken(enclosing) != 0 ? enclosing : mdTypeDefNil);

            TypeDefinition td = _md.GetTypeDefinition((TypeDefinitionHandle)Handle(cl));
            foreach (MethodImplementationHandle mih in td.GetMethodImplementations())
            {
                MethodImplementation mi = _md.GetMethodImplementation(mih);
                int body = Token(mi.MethodBody);
                int decl = Token(mi.MethodDeclaration);
                int bodyParent = GetParentToken(body);
                _methodImpls.Add(new MethodImplRecord { TkClass = cl, TkBody = body, TkDecl = decl, TkBodyParent = bodyParent });
            }
        }

        // nesting consistency
        for (int i = 0; i < _classList.Count; i++)
        {
            int tkThis = _classList[i];
            int tkEncloser = _classEnclosing[i];
            int tkPrevLevel = tkThis;
            while (tkEncloser != mdTypeDefNil)
            {
                if (tkThis == tkEncloser)
                {
                    PrintError($"error : class {tkThis:X8} is nested in itself");
                    _classEnclosing[i] = mdTypeDefNil;
                    break;
                }

                int j = _classList.IndexOf(tkEncloser);
                if (j < 0)
                {
                    PrintError($"error : class {tkPrevLevel:X8} is nested in missing class {tkEncloser:x8}");
                    _classEnclosing[i] = mdTypeDefNil;
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
        if (_classEnclosing[0] == mdTypeDefNil && n0 == "<Module>" && ns0.Length == 0)
        {
            _dups.Add(_classList[0]);
        }

        Dictionary<(int, string, string), int> seen = new();
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
            TypeDefinition td = i == 0 ? GlobalType() : _md.GetTypeDefinition((TypeDefinitionHandle)Handle(_classList[i - 1]));
            if (i == 0 && RowCount(TableIndex.TypeDef) == 0)
            {
                continue;
            }

            HashSet<(string, ByteArrayKey)> methods = new();
            foreach (MethodDefinitionHandle h in td.GetMethods())
            {
                MethodDefinition m = _md.GetMethodDefinition(h);
                if (!methods.Add((Str(m.Name), new ByteArrayKey(Blob(m.Signature)))))
                {
                    _dups.Add(Token(h));
                }
            }

            HashSet<(string, ByteArrayKey)> fields = new();
            foreach (FieldDefinitionHandle h in td.GetFields())
            {
                FieldDefinition f = _md.GetFieldDefinition(h);
                if (!fields.Add((Str(f.Name), new ByteArrayKey(Blob(f.Signature)))))
                {
                    _dups.Add(Token(h));
                }
            }
        }
    }

    private readonly struct ByteArrayKey : IEquatable<ByteArrayKey>
    {
        private readonly byte[] _b;

        public ByteArrayKey(byte[] b) => _b = b;

        public bool Equals(ByteArrayKey other) => _b.AsSpan().SequenceEqual(other._b);

        public override bool Equals(object? obj) => obj is ByteArrayKey k && Equals(k);

        public override int GetHashCode()
        {
            HashCode h = default;
            h.AddBytes(_b);
            return h.ToHashCode();
        }
    }

    private void EnumTypedefs()
    {
        foreach (TypeSpecificationHandle h in AllTypeSpecs())
        {
            byte[] sig = Blob(_md.GetTypeSpecification(h).Signature);
            if (sig.Length == 0 || sig[0] != ELEMENT_TYPE_TYPEDEF)
            {
                continue;
            }

            int z = Array.IndexOf(sig, (byte)0, 1);
            if (z < 0)
            {
                continue;
            }

            TypeDefDescr td = new()
            {
                Name = Encoding.UTF8.GetString(sig, 1, z - 1),
                TkSelf = Token(h),
            };
            int l = z + 1;
            td.TkTypeSpec = l + 4 <= sig.Length ? (int)ReadUInt32(sig, l) : 0;
            if (TypeFromToken(td.TkTypeSpec) == mdtTypeSpec && IsValidToken(td.TkTypeSpec))
            {
                td.Sig = GetTypeSpecSig(td.TkTypeSpec);
            }
            else if (TypeFromToken(td.TkTypeSpec) == mdtCustomAttribute)
            {
                l += 4;
                td.Sig = sig.AsSpan(Math.Min(l, sig.Length)).ToArray();
            }

            _typedefs.Add(td);
        }
    }

    private IEnumerable<TypeSpecificationHandle> AllTypeSpecs()
    {
        int n = RowCount(TableIndex.TypeSpec);
        for (int i = 1; i <= n; i++)
        {
            yield return MetadataTokens.TypeSpecificationHandle(i);
        }
    }

    private void DumpTypedefs()
    {
        PrintLine("");
        foreach (TypeDefDescr td in _typedefs.ToArray())
        {
            StringBuilder sb = new();
            sb.Append(Indent).Append(ANCHORPT(KEYWORD(".typedef"), td.TkSelf)).Append(' ');
            if (_options.ShowTokens)
            {
                sb.Append(COMMENT($"/*{td.TkSelf:X8}*/ "));
            }

            int tk = td.TkTypeSpec;
            switch (TypeFromToken(tk))
            {
                case mdtCustomAttribute:
                {
                    PrintLine(sb);
                    PushIndent("        ");
                    byte[] s = td.Sig ?? Array.Empty<byte>();
                    int tkType = s.Length >= 4 ? (int)ReadUInt32(s, 0) : 0;
                    int tkOwner = s.Length >= 8 ? (int)ReadUInt32(s, 4) : 0;
                    byte[] blob = s.Length > 8 ? s.AsSpan(8).ToArray() : Array.Empty<byte>();
                    DumpCustomAttributeProps(0, tkType, tkOwner, blob, RidFromToken(tkOwner) != 0);
                    PrintLine($"{Indent} {KEYWORD("as")} {ProperName(td.Name)}");
                    PopIndent(8);
                    continue;
                }

                case mdtMethodDef:
                    sb.Append(KEYWORD("method "));
                    break;
                case mdtFieldDef:
                    sb.Append(KEYWORD("field "));
                    break;
                case mdtMemberRef:
                    sb.Append(KEYWORD(IsValidToken(tk) && IsFieldMemberRef(tk) ? "field " : "method "));
                    break;
            }

            List<TypeDefDescr> saved = new(_typedefs);
            _typedefs.Clear();
            try
            {
                PrettyPrintToken(sb, tk);
            }
            finally
            {
                _typedefs.AddRange(saved);
            }

            sb.Append(' ').Append(KEYWORD("as")).Append(' ').Append(ProperName(td.Name));
            PrintLine(sb);
        }
    }

    private void DumpMscorlib()
    {
        if (FindTypeDefByName("System", "Object", out int tk))
        {
            (TypeAttributes attrs, int extends) = GetTypeDefProps(tk);
            bool isClass = (attrs & TypeAttributes.ClassSemanticsMask) == TypeAttributes.Class;
            if (isClass && !IsValidToken(extends))
            {
                PrintLine("");
                PrintLine($"{Indent}{KEYWORD(".mscorlib")} ");
                PrintLine("");
            }
        }
    }

    private void DumpTypelist()
    {
        if (_classList.Count > 1)
        {
            PrintLine("");
            PrintLine($"{Indent}{KEYWORD(".typelist")} ");
            PrintLine(Indent + SCOPE());
            PushIndent();
            foreach (int cl in _classList)
            {
                PrintLine(Indent + PrettyPrintClass(cl));
            }

            PopIndent();
            PrintLine(Indent + UNSCOPE());
            PrintLine("");
        }
    }

    // ---------------------------------------------------------------- the whole file

    public bool DumpFile(string inputFileName)
    {
        try
        {
            return DumpFileCore(inputFileName);
        }
        finally
        {
            _pdbProvider?.Dispose();
            _pdbProvider = null;
            _pdb = null;
        }
    }

    private bool DumpFileCore(string inputFileName)
    {
        if (_html)
        {
            PrintLine("<HTML>");
            PrintLine("<HEAD>");
            PrintLine($"<TITLE> {inputFileName} - IL DASM</TITLE>");
            PrintLine("</HEAD>");
            PrintLine("<BODY>");
            PrintLine("<FONT SIZE=3 FACE=\"Arial\">");
            PrintLine("<PRE>");
        }
        else if (_rtf)
        {
            DumpRTFPrefix();
        }

        DumpPreamble();

        CorHeader? cor = _pe.PEHeaders.CorHeader;
        if (cor is null)
        {
            PrintError($"error : '{inputFileName}' has no valid CLR header and cannot be disassembled");
            if (_options.Headers)
            {
                DumpHeader();
            }

            return false;
        }

        if (cor.MajorRuntimeVersion == 1 || cor.MajorRuntimeVersion > 2)
        {
            PrintError($"CORHeader->MajorRuntimeVersion = {cor.MajorRuntimeVersion}");
            PrintError("error : Bad CLR header, or the version of the file format is not supported");
            return false;
        }

        int entryPoint = cor.EntryPointTokenOrRelativeVirtualAddress;
        OpenPdb(inputFileName);

        EnumClasses();
        EnumTypedefs();

        // Header, statistics and metadata info go inside a "multiline comment" block (empty without HTML/RTF).
        PrintLine(CommentPrefix);
        if (_options.Headers)
        {
            DumpHeader();
            DumpHeaderDetails();
        }
        else
        {
            DumpVTables();
        }

        if (_options.Stats)
        {
            DumpStatistics();
        }

        if (_options.ClassList)
        {
            PrintClassList();
        }

        if (_options.Metadata)
        {
            DumpMetaInfo();
        }

        PrintLine(CommentPostfix);

        if (!_options.NoIL)
        {
            if (_classToDump != 0)
            {
                DumpClass(TopEncloser(_classToDump), entryPoint, 7);
                PrintLine(COMMENT("// *********** DISASSEMBLY COMPLETE ***********************"));
                return FinishFile();
            }

            DumpMscorlib();
            if (_options.TypeList)
            {
                DumpTypelist();
            }

            DumpManifest();
            DumpTypedefs();

            if (_options.ForwardDeclarations && _classList.Count != 0)
            {
                PrintLine(COMMENT("//"));
                PrintLine(COMMENT("// ============== CLASS STRUCTURE DECLARATION =================="));
                PrintLine(COMMENT("//"));
                for (int i = 0; i < _classList.Count; i++)
                {
                    if (_classEnclosing[i] == mdTypeDefNil)
                    {
                        DumpClass(_classList[i], entryPoint, 2);
                    }
                }

                PrintLine("");
                PrintLine(COMMENT("// ============================================================="));
                PrintLine("");
            }

            DumpGlobalFields();
            DumpGlobalMethods(entryPoint);

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
                    if (_classEnclosing[i] == mdTypeDefNil)
                    {
                        DumpClass(_classList[i], entryPoint, 7);
                    }
                }

                PrintLine("");
                PrintLine(COMMENT("// ============================================================="));
                PrintLine("");
            }

            if (ShowCustomAttributes)
            {
                int n = RowCount(TableIndex.CustomAttribute);
                for (int i = 1; i <= n; i++)
                {
                    if (!_caDumped.Contains(i))
                    {
                        DumpCustomAttribute(mdtCustomAttribute | i, withOwner: true);
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
        if (_toFile)
        {
            DumpWin32Resources();
        }

        if (_html)
        {
            PrintLine("</PRE>");
            PrintLine("</BODY>");
            PrintLine("</HTML>");
        }
        else if (_rtf)
        {
            DumpRTFPostfix();
        }

        return true;
    }

    private void DumpRTFPrefix()
    {
        _out.Write("{\\rtf1\\ansi\n");
        _out.Write("{\\fonttbl{\\f0\\fmodern\\fprq1\\fcharset1 Courier New;}{\\f1\\fswiss\\fcharset1 Arial;}}\n");
        _out.Write("{\\colortbl ;\\red0\\green0\\blue128;\\red0\\green128\\blue0;\\red255\\green0\\blue0;}\n");
        _out.Write("\\viewkind4\\uc1\\pard\\f0\\fs20\n");
    }

    private void DumpRTFPostfix()
    {
        _out.Write("}\n");
    }

    private void DumpPreamble()
    {
        PrintLine("");
        if (_html)
        {
            PrintLine("<FONT SIZE=4><B>");
        }

        PrintLine(COMMENT("//  .NET IL Disassembler.  Version " + ProductVersion));
        if (_html)
        {
            PrintLine("</B></FONT>");
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
            string? v = typeof(Dasm).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            if (v is null)
            {
                return "0.0.0";
            }

            int plus = v.IndexOf('+');
            return plus >= 0 ? v.Substring(0, plus) : v;
        }
    }

    // .data sections for everything referenced via DumpDataPtr.
    private void DumpDataSections()
    {
        if (_ptrTags.Count == 0)
        {
            return;
        }

        List<(uint Rva, uint Size)> tags = new(_ptrTags);
        // Sorted by RVA with a stable sort.
        tags.Sort((x, y) => x.Rva.CompareTo(y.Rva));
        for (int j = 0; j < tags.Count; j++)
        {
            uint fromPtr = tags[j].Rva;
            SectionHeader? shn = SectionOf(fromPtr);
            if (shn is null)
            {
                PrintLine($"ERROR: One of used pointers references undefined data (D_{fromPtr:X8})");
                break;
            }

            SectionHeader sh = shn.Value;
            uint secEnd = (uint)sh.VirtualAddress + (uint)sh.VirtualSize;
            string prefix = DataLabelPrefix(sh);
            uint toPtr = j == tags.Count - 1 ? secEnd : Math.Min(tags[j + 1].Rva, secEnd);
            if (toPtr - fromPtr > tags[j].Size)
            {
                toPtr = fromPtr + tags[j].Size;
            }

            uint limPtr = Math.Min(toPtr, (uint)sh.VirtualAddress + (uint)sh.SizeOfRawData);
            while (true)
            {
                StringBuilder sb = new();
                sb.Append(Indent).Append(KEYWORD(".data")).Append(' ');
                if (prefix == "T_")
                {
                    sb.Append(KEYWORD("tls "));
                }
                else if (prefix == "I_")
                {
                    sb.Append(KEYWORD("cil "));
                }

                if (fromPtr >= limPtr)
                {
                    sb.Append(prefix).Append(fromPtr.ToString("X8", CultureInfo.InvariantCulture)).Append(" = ").Append(KEYWORD("int8")).Append('[').Append(((int)(toPtr - fromPtr)).ToString(CultureInfo.InvariantCulture)).Append(']');
                    PrintLine(sb);
                }
                else
                {
                    sb.Append(prefix).Append(fromPtr.ToString("X8", CultureInfo.InvariantCulture)).Append(" = ").Append(KEYWORD("bytearray")).Append(" (");
                    PrintLine(sb);
                    byte[] data = GetRawSectionBytes(sh, fromPtr, limPtr - fromPtr);
                    DumpHexbytes(data, fromPtr, toPtr, limPtr);
                }

                if (limPtr == toPtr && j < tags.Count - 1 && toPtr < tags[j + 1].Rva)
                {
                    uint stptr = tags[j + 1].Rva;
                    uint align = 1;
                    while ((align & stptr) == 0)
                    {
                        align <<= 1;
                    }

                    align -= 1;
                    if ((toPtr & align) != 0)
                    {
                        fromPtr = toPtr;
                        toPtr = (toPtr + align) & ~align;
                        continue;
                    }
                }

                break;
            }
        }
    }

    private byte[] GetRawSectionBytes(SectionHeader sh, uint rva, uint count)
    {
        byte[] result = new byte[count];
        long fileOffset = sh.PointerToRawData + (rva - (uint)sh.VirtualAddress);
        ImmutableArray<byte> content = Image;
        for (int i = 0; i < count && fileOffset + i < content.Length; i++)
        {
            result[i] = content[(int)(fileOffset + i)];
        }

        return result;
    }

    private void DumpHexbytes(byte[] data, uint fromPtr, uint toPtr, uint limPtr)
    {
        StringBuilder sb = new();
        sb.Append(Indent).Append("                ");
        StringBuilder sz = new();
        int k = 0;
        bool printsz = false;
        for (uint curPtr = fromPtr; curPtr < toPtr; curPtr++, k++)
        {
            if (k == 16)
            {
                if (printsz)
                {
                    sb.Append(COMMENT("  // " + sz));
                }

                PrintLine(sb);
                sb.Clear();
                sb.Append(Indent).Append("                ");
                k = 0;
                sz.Clear();
                printsz = false;
            }

            byte b = curPtr >= limPtr ? (byte)0 : data[curPtr - fromPtr];
            sb.Append(' ').Append(b.ToString("X2", CultureInfo.InvariantCulture));
            if (IsPrint(b))
            {
                if (_rtf)
                {
                    if (b == '\\' || b == '{' || b == '}')
                    {
                        sz.Append('\\');
                    }

                    sz.Append((char)b);
                }
                else if (_html)
                {
                    // Other characters are dropped in HTML mode.
                    if (b == '<')
                    {
                        sz.Append(LTN());
                    }
                    else if (b == '>')
                    {
                        sz.Append(GTN());
                    }
                }
                else
                {
                    sz.Append((char)b);
                }

                printsz = true;
            }
            else
            {
                sz.Append('.');
            }
        }

        sb.Append(") ");
        if (printsz)
        {
            for (int i = k; i < 16; i++)
            {
                sb.Append("   ");
            }

            sb.Append(COMMENT("// " + sz));
        }

        PrintLine(sb);
    }

    // ---------------------------------------------------------------- vtable fixups and exports

    private void DumpVTables()
    {
        CorHeader cor = _pe.PEHeaders.CorHeader!;
        DirectoryEntry dir = cor.VtableFixupsDirectory;
        if (dir.RelativeVirtualAddress == 0)
        {
            return;
        }

        PrintLine("// VTableFixup Directory:");
        int count = dir.Size / 8;
        byte[]? fixups = GetBytesAtRva((uint)dir.RelativeVirtualAddress, dir.Size);
        if (fixups is null || fixups.Length < dir.Size)
        {
            PrintLine("// Failed to get vtable fix-up table.");
            PrintLine("");
            return;
        }

        for (int i = 0; i < count; i++)
        {
            uint rva = ReadUInt32(fixups, i * 8);
            ushort n = (ushort)(fixups[i * 8 + 4] | fixups[i * 8 + 5] << 8);
            ushort type = (ushort)(fixups[i * 8 + 6] | fixups[i * 8 + 7] << 8);
            PrintLine($"//   IMAGE_COR_VTABLEFIXUP[{i}]:");
            PrintLine($"//       RVA:               0x{rva:x8}");
            PrintLine($"//       Count:             0x{n:x4}");
            PrintLine($"//       Type:              0x{type:x4}");
            byte[]? slots = GetBytesAtRva(rva, -1);
            if (slots is null)
            {
                PrintLine("// Bogus RVA for entry.");
                continue;
            }

            int p = 0;
            for (int s = 0; s < n; s++)
            {
                if ((type & 0x01) != 0)
                {
                    uint tok = ReadUInt32(slots, p);
                    PrintLine($"//         [0x{s:x4}]            (0x{tok:x8})");
                    p += 4;
                    if (!IsValidToken((int)tok))
                    {
                        PrintError($"error :  invalid token ({tok:x8})");
                    }
                    else if (TypeFromToken((int)tok) != mdtMethodDef)
                    {
                        PrintError($"error : unexpected token type, expected {mdtMethodDef >> 24:x2}, got {TypeFromToken((int)tok) >> 24:x2}");
                    }
                }
                else
                {
                    ulong v = BitConverter.ToUInt64(slots, p);
                    PrintLine($"//         [0x{s:x4}]            (0x{v,16:x})");
                    uint tok = (uint)v;
                    p += 8;
                    if (!IsValidToken((int)tok))
                    {
                        PrintError($"error :  invalid token ({tok:x8})");
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

        int subsystem = (int)pe.Subsystem;
        string[] names =
        [
            "// UNKNOWN", "// NATIVE", "// WINDOWS_GUI", "// WINDOWS_CUI", "// <illegal value>", "// OS2_CUI", "// <illegal value>",
            "// POSIX_CUI", "// NATIVE_WINDOWS", "// WINDOWS_CE_GUI", "// EFI_APPLICATION", "// EFI_BOOT_SERVICE_DRIVER",
            "// EFI_RUNTIME_DRIVER", "// EFI_ROM", "// XBOX",
        ];
        PrintLine($"{Indent}{KEYWORD(".subsystem")} 0x{subsystem:x4}       {COMMENT(names[subsystem > 14 ? 4 : subsystem])}");

        uint flags = (uint)cor.Flags;
        StringBuilder sb = new();
        sb.Append(Indent).Append(KEYWORD(".corflags")).Append(" 0x").Append(flags.ToString("x8", CultureInfo.InvariantCulture));
        if (flags != 0)
        {
            StringBuilder c = new("    // ");
            if ((flags & 0x1) != 0) c.Append(" ILONLY");
            if ((flags & 0x00000002) != 0 && (flags & 0x00020000) == 0) c.Append(" 32BITREQUIRED");
            if ((flags & 0x00000002) != 0 && (flags & 0x00020000) != 0) c.Append(" 32BITPREFERRED");
            if ((flags & 0x4) != 0) c.Append(" IL_LIBRARY");
            if ((flags & 0x10000) != 0) c.Append(" TRACKDEBUGDATA");
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

            byte[] entries = GetRawSectionBytes(shn.Value, (uint)vt.RelativeVirtualAddress, (uint)vt.Size);
            int k = 0;
            for (int j = vt.Size; j > 0; j -= 8, k++)
            {
                int off = k * 8;
                if (off + 8 > entries.Length)
                {
                    break;
                }

                uint addr = ReadUInt32(entries, off);
                ushort count = (ushort)(entries[off + 4] | entries[off + 5] << 8);
                ushort type = (ushort)(entries[off + 6] | entries[off + 7] << 8);
                sb.Clear();
                sb.Append(Indent).Append(KEYWORD(".vtfixup")).Append(" [").Append(count.ToString(CultureInfo.InvariantCulture)).Append("] ");
                uint size = (uint)count * 4;
                if ((type & 0x01) != 0)
                {
                    sb.Append(KEYWORD("int32 "));
                }
                else if ((type & 0x02) != 0)
                {
                    sb.Append(KEYWORD("int64 "));
                    size <<= 1;
                }

                if ((type & 0x04) != 0) sb.Append(KEYWORD("fromunmanaged "));
                if ((type & 0x10) != 0) sb.Append(KEYWORD("callmostderived "));
                if ((type & 0x08) != 0) sb.Append(KEYWORD("retainappdomain "));
                sb.Append(KEYWORD("at "));
                DumpDataPtr(sb, addr, size);
                byte[]? slots = GetBytesAtRva(addr, -1);
                if (slots is not null)
                {
                    StringBuilder c = new(" //");
                    int p = 0;
                    for (int s = 0; s < count; s++)
                    {
                        int tok = (int)ReadUInt32(slots, p);
                        if ((type & 0x01) != 0)
                        {
                            c.Append(' ').Append(((uint)tok).ToString("X8", CultureInfo.InvariantCulture));
                            p += 4;
                        }
                        else
                        {
                            c.Append(' ').Append(BitConverter.ToUInt64(slots, p).ToString("X16", CultureInfo.InvariantCulture));
                            p += 8;
                        }

                        _vtableRefs.Add(new VTableRef { Tok = tok, Entry = k, Slot = s });
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

            byte[]? table = GetBytesAtRva((uint)exportDir.RelativeVirtualAddress, 40);
            if (table is null || table.Length < 40)
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
                byte[]? addrs = GetBytesAtRva(addrTable, (int)numAT * 4);
                _eatRefs.Clear();
                for (int j = 0; j < numAT; j++)
                {
                    EATableRef r = new();
                    uint entryRva = ReadUInt32(addrs!, j * 4);
                    byte[]? cont = GetBytesAtRva(entryRva, 10);
                    if (cont is not null)
                    {
                        uint tokRva = IsPE32
                            ? ReadUInt32(cont, 2) - (uint)pe.ImageBase
                            : (uint)(BitConverter.ToUInt64(cont, 2) - (uint)pe.ImageBase);
                        byte[]? tok = GetBytesAtRva(tokRva, 4);
                        if (tok is not null)
                        {
                            r.Tok = (int)ReadUInt32(tok, 0);
                        }
                    }

                    _eatRefs.Add(r);
                }
            }

            if (numNames != 0 && namePtr != 0 && ordTable != 0)
            {
                byte[]? names = GetBytesAtRva(namePtr, (int)numAT * 4);
                byte[]? ords = GetBytesAtRva(ordTable, (int)numAT * 2);
                for (int j = 0; j < numAT; j++)
                {
                    uint nameRva = ReadUInt32(names!, j * 4);
                    int ord = ords![j * 2] | ords[j * 2 + 1] << 8;
                    byte[]? nameBytes = GetBytesAtRva(nameRva, -1);
                    string? name = null;
                    if (nameBytes is not null)
                    {
                        int z = Array.IndexOf(nameBytes, (byte)0);
                        name = Encoding.UTF8.GetString(nameBytes, 0, z < 0 ? nameBytes.Length : z);
                    }

                    if (ord < _eatRefs.Count)
                    {
                        _eatRefs[ord] = new EATableRef { Tok = _eatRefs[ord].Tok, Name = name };
                    }
                }
            }

            _eatBase = (int)ordinalBase;
        }
        catch (Exception e) when (e is IndexOutOfRangeException or ArgumentException or NullReferenceException)
        {
            PrintError("// ERROR READING EXPORT ADDRESS TABLE");
            _eatRefs.Clear();
        }
    }
}
