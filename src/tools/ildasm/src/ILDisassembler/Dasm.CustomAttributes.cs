// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text;

namespace ILDisassembler;

// Verbal custom attribute and permission set blobs (/CAVERBAL and .permissionset), as in the native dasm.cpp.
internal sealed partial class Dasm
{
    private const byte SERIALIZATION_TYPE_TYPE = 0x50;
    private const byte SERIALIZATION_TYPE_TAGGED_OBJECT = 0x51;
    private const byte SERIALIZATION_TYPE_FIELD = 0x53;
    private const byte SERIALIZATION_TYPE_PROPERTY = 0x54;
    private const byte SERIALIZATION_TYPE_ENUM = 0x55;

    private sealed class CABlobException : Exception
    {
    }

    // ---------------------------------------------------------------- reflection notation

    private int FindTypeDef(string ns, string name, int tkEncloser)
    {
        foreach (TypeDefinitionHandle h in _md.TypeDefinitions)
        {
            TypeDefinition td = _md.GetTypeDefinition(h);
            TypeDefinitionHandle decl = td.GetDeclaringType();
            int enc = decl.IsNil ? mdTypeDefNil : Token(decl);
            if (enc == tkEncloser && Str(td.Name) == name && Str(td.Namespace) == ns)
            {
                return Token(h);
            }
        }

        return 0;
    }

    private int FindTypeRefByName(string ns, string name, int tkResScope)
    {
        foreach (TypeReferenceHandle h in _md.TypeReferences)
        {
            TypeReference tr = _md.GetTypeReference(h);
            if (Token(tr.ResolutionScope) == tkResScope && Str(tr.Name) == name && Str(tr.Namespace) == ns)
            {
                return Token(h);
            }
        }

        return 0;
    }

    private int ResolveTypeDefReflectionNotation(string ns, string name, int tkEncloser)
    {
        int plus = name.LastIndexOf('+');
        if (plus >= 0)
        {
            tkEncloser = ResolveTypeDefReflectionNotation(ns, name.Substring(0, plus), tkEncloser);
            ns = "";
            name = name.Substring(plus + 1);
        }

        return FindTypeDef(ns, name, tkEncloser);
    }

    private int ResolveTypeRefReflectionNotation(string ns, string name, int tkResScope)
    {
        int plus = name.LastIndexOf('+');
        if (plus >= 0)
        {
            tkResScope = ResolveTypeRefReflectionNotation(ns, name.Substring(0, plus), tkResScope);
            ns = "";
            name = name.Substring(plus + 1);
        }

        return FindTypeRefByName(ns, name, tkResScope);
    }

    // "Namespace.Name, Assembly, ..." -> a TypeDef or TypeRef token, or 0.
    private int ResolveReflectionNotation(byte[] data, int start, int len)
    {
        string str = Encoding.UTF8.GetString(data, start, len);
        int z = str.IndexOf('\0');
        if (z >= 0)
        {
            str = str.Substring(0, z);
        }

        string? assembly = null;
        int comma = str.IndexOf(',');
        if (comma >= 0)
        {
            string rest = str.Substring(comma + 1).TrimStart(' ');
            int comma2 = rest.IndexOf(',');
            assembly = comma2 >= 0 ? rest.Substring(0, comma2) : rest;
            str = str.Substring(0, comma);
        }

        string ns = "";
        string name = str;
        int dot = str.LastIndexOf('.');
        if (dot >= 0)
        {
            ns = str.Substring(0, dot);
            name = str.Substring(dot + 1);
        }

        if (assembly is null)
        {
            int tk = ResolveTypeDefReflectionNotation(ns, name, mdTypeDefNil);
            if (tk != 0)
            {
                return tk;
            }

            assembly = "mscorlib";
        }

        int n = RowCount(System.Reflection.Metadata.Ecma335.TableIndex.AssemblyRef);
        for (int i = 1; i <= n; i++)
        {
            AssemblyReference ar = _md.GetAssemblyReference(System.Reflection.Metadata.Ecma335.MetadataTokens.AssemblyReferenceHandle(i));
            if (Str(ar.Name) == assembly)
            {
                return ResolveTypeRefReflectionNotation(ns, name, mdtAssemblyRef | i);
            }
        }

        return 0;
    }

    // The underlying type of an enum defined in this module; enums referenced from other assemblies cannot be
    // resolved (the native tool cannot open them either) and give 0.
    private uint UnderlyingTypeOfEnum(int tk)
    {
        if (TypeFromToken(tk) != mdtTypeDef || !IsValidToken(tk))
        {
            return 0;
        }

        (_, int parent) = GetTypeDefProps(tk);
        if (RidFromToken(parent) == 0 || !IsValidToken(parent))
        {
            return 0;
        }

        (string name, string ns) = TypeFromToken(parent) switch
        {
            mdtTypeDef => GetNameOfTypeDef(parent),
            mdtTypeRef => GetNameOfTypeRef(parent),
            _ => ("", ""),
        };
        if (name != "Enum" || ns != "System")
        {
            return 0;
        }

        foreach (FieldDefinitionHandle fh in _md.GetTypeDefinition((TypeDefinitionHandle)Handle(tk)).GetFields())
        {
            FieldDefinition fd = _md.GetFieldDefinition(fh);
            if ((fd.Attributes & FieldAttributes.Static) != 0)
            {
                continue;
            }

            byte[] sig = Blob(fd.Signature);
            return sig.Length > 1 ? sig[1] : 0u;
        }

        return 0;
    }

    // ---------------------------------------------------------------- blob values

    private static int SkipType(byte[] s, int p)
    {
        while (true)
        {
            byte b = s[p++];
            switch (b)
            {
                case SERIALIZATION_TYPE_ENUM:
                {
                    uint l = UncompressData(s, ref p);
                    return p + (int)l;
                }

                case ELEMENT_TYPE_VALUETYPE:
                case ELEMENT_TYPE_CLASS:
                    UncompressToken(s, ref p);
                    return p;
                case ELEMENT_TYPE_CMOD_REQD:
                case ELEMENT_TYPE_CMOD_OPT:
                    UncompressToken(s, ref p);
                    continue;
                case ELEMENT_TYPE_ARRAY:
                {
                    p = SkipType(s, p);
                    uint rank = UncompressData(s, ref p);
                    if (rank != 0)
                    {
                        uint numSizes = UncompressData(s, ref p);
                        while (numSizes-- > 0)
                        {
                            UncompressData(s, ref p);
                        }

                        uint numLowBounds = UncompressData(s, ref p);
                        while (numLowBounds-- > 0)
                        {
                            UncompressData(s, ref p);
                        }
                    }

                    return p;
                }

                case ELEMENT_TYPE_PINNED:
                case ELEMENT_TYPE_PTR:
                case ELEMENT_TYPE_BYREF:
                case ELEMENT_TYPE_SZARRAY:
                    continue;
                case ELEMENT_TYPE_VAR:
                case ELEMENT_TYPE_MVAR:
                    UncompressData(s, ref p);
                    return p;
                case ELEMENT_TYPE_FNPTR:
                {
                    UncompressData(s, ref p);
                    uint argCnt = UncompressData(s, ref p);
                    p = SkipType(s, p);
                    while (argCnt-- > 0)
                    {
                        p = SkipType(s, p);
                    }

                    return p;
                }

                case ELEMENT_TYPE_GENERICINST:
                {
                    p = SkipType(s, p);
                    uint argCnt = UncompressData(s, ref p);
                    while (argCnt-- > 0)
                    {
                        p = SkipType(s, p);
                    }

                    return p;
                }

                default:
                    return p;
            }
        }
    }

    // PrettyPrintCABlobValue: formats one value of type ts[tp] read from data[dp]; throws CABlobException on failure.
    private void PrettyPrintCABlobValue(byte[] ts, ref int tp, byte[] data, ref int dp, int dataEnd, StringBuilder out_)
    {
        string appendix = "";
        uint numElements = 1;
        bool reiterate;
        bool closeParenthesis;
        CultureInfo inv = CultureInfo.InvariantCulture;
        do
        {
            if (dp >= dataEnd)
            {
                throw new CABlobException();
            }

            reiterate = false;
            closeParenthesis = true;
            byte typ = ts[tp++];

            void Open(string keyword)
            {
                out_.Append(KEYWORD(keyword)).Append(appendix).Append('(');
            }

            switch (typ)
            {
                case ELEMENT_TYPE_BOOLEAN:
                    Open("bool");
                    for (uint n = 0; n < numElements; n++)
                    {
                        if (n != 0) out_.Append(' ');
                        out_.Append(data[dp] != 0 ? KEYWORD("true") : KEYWORD("false"));
                        dp++;
                    }

                    break;
                case ELEMENT_TYPE_CHAR:
                    Open("char");
                    for (uint n = 0; n < numElements; n++)
                    {
                        if (n != 0) out_.Append(' ');
                        out_.Append("0x").Append(BitConverter.ToUInt16(data, dp).ToString("X4", inv));
                        dp += 2;
                    }

                    break;
                case ELEMENT_TYPE_I1:
                    Open("int8");
                    for (uint n = 0; n < numElements; n++)
                    {
                        if (n != 0) out_.Append(' ');
                        out_.Append(((sbyte)data[dp]).ToString(inv));
                        dp++;
                    }

                    break;
                case ELEMENT_TYPE_U1:
                    Open("uint8");
                    for (uint n = 0; n < numElements; n++)
                    {
                        if (n != 0) out_.Append(' ');
                        out_.Append(data[dp].ToString(inv));
                        dp++;
                    }

                    break;
                case ELEMENT_TYPE_I2:
                    Open("int16");
                    for (uint n = 0; n < numElements; n++)
                    {
                        if (n != 0) out_.Append(' ');
                        out_.Append(BitConverter.ToInt16(data, dp).ToString(inv));
                        dp += 2;
                    }

                    break;
                case ELEMENT_TYPE_U2:
                    Open("uint16");
                    for (uint n = 0; n < numElements; n++)
                    {
                        if (n != 0) out_.Append(' ');
                        out_.Append(BitConverter.ToUInt16(data, dp).ToString(inv));
                        dp += 2;
                    }

                    break;
                case ELEMENT_TYPE_I4:
                case ELEMENT_TYPE_U4:
                    // The native tool prints both with %d.
                    Open(typ == ELEMENT_TYPE_I4 ? "int32" : "uint32");
                    for (uint n = 0; n < numElements; n++)
                    {
                        if (n != 0) out_.Append(' ');
                        out_.Append(BitConverter.ToInt32(data, dp).ToString(inv));
                        dp += 4;
                    }

                    break;
                case ELEMENT_TYPE_I8:
                case ELEMENT_TYPE_U8:
                    // The native tool prints both with %lld.
                    Open(typ == ELEMENT_TYPE_I8 ? "int64" : "uint64");
                    for (uint n = 0; n < numElements; n++)
                    {
                        if (n != 0) out_.Append(' ');
                        out_.Append(BitConverter.ToInt64(data, dp).ToString(inv));
                        dp += 8;
                    }

                    break;
                case ELEMENT_TYPE_R4:
                    Open("float32");
                    for (uint n = 0; n < numElements; n++)
                    {
                        if (n != 0) out_.Append(' ');
                        uint bits = BitConverter.ToUInt32(data, dp);
                        string s = FormatG(BitConverter.UInt32BitsToSingle(bits), 8, alternate: true);
                        float df = (float)Atof(s);
                        if (BitConverter.SingleToUInt32Bits(df) != bits || IsSpecialNumber(s))
                        {
                            s = "0x" + bits.ToString("X8", inv);
                        }

                        out_.Append(s);
                        dp += 4;
                    }

                    break;
                case ELEMENT_TYPE_R8:
                    Open("float64");
                    for (uint n = 0; n < numElements; n++)
                    {
                        if (n != 0) out_.Append(' ');
                        ulong bits = BitConverter.ToUInt64(data, dp);
                        string s = FormatG(BitConverter.UInt64BitsToDouble(bits), 17, alternate: true);
                        double df = Atof(s);
                        if (BitConverter.DoubleToUInt64Bits(df) != bits || IsSpecialNumber(s))
                        {
                            s = "0x" + bits.ToString("X", inv);
                        }

                        out_.Append(s);
                        dp += 8;
                    }

                    break;
                case ELEMENT_TYPE_OBJECT:
                case SERIALIZATION_TYPE_TAGGED_OBJECT:
                    Open("object");
                    for (uint n = 0; n < numElements; n++)
                    {
                        int valuePtr = SkipType(data, dp);
                        if (n != 0) out_.Append(' ');
                        int typePtr = dp;
                        PrettyPrintCABlobValue(data, ref typePtr, data, ref valuePtr, dataEnd, out_);
                        dp = valuePtr;
                    }

                    break;
                case ELEMENT_TYPE_STRING:
                    Open("string");
                    for (uint n = 0; n < numElements; n++)
                    {
                        if (n != 0) out_.Append(' ');
                        if (data[dp] == 0xFF)
                        {
                            out_.Append(KEYWORD("nullref"));
                            dp++;
                        }
                        else
                        {
                            out_.Append('\'');
                            int l = (int)UncompressData(data, ref dp);
                            if (dp + l > dataEnd)
                            {
                                throw new CABlobException();
                            }

                            out_.Append(UnquotedProperName(Utf8Z(data, dp, l))).Append('\'');
                            dp += l;
                        }
                    }

                    break;
                case ELEMENT_TYPE_CLASS:
                case SERIALIZATION_TYPE_TYPE:
                    if (typ == ELEMENT_TYPE_CLASS)
                    {
                        UncompressToken(ts, ref tp);
                    }

                    Open("type");
                    for (uint n = 0; n < numElements; n++)
                    {
                        if (n != 0) out_.Append(' ');
                        if (data[dp] == 0xFF)
                        {
                            out_.Append(KEYWORD("nullref"));
                            dp++;
                        }
                        else
                        {
                            int l = (int)UncompressData(data, ref dp);
                            if (dp + l > dataEnd)
                            {
                                throw new CABlobException();
                            }

                            AppendResolvedType(out_, data, dp, l);
                            dp += l;
                        }
                    }

                    break;
                case ELEMENT_TYPE_VALUETYPE:
                case SERIALIZATION_TYPE_ENUM:
                {
                    int tk;
                    if (typ == ELEMENT_TYPE_VALUETYPE)
                    {
                        tk = UncompressToken(ts, ref tp);
                    }
                    else
                    {
                        int l = (int)UncompressData(ts, ref tp);
                        tk = ResolveReflectionNotation(ts, tp, l);
                        tp += l;
                    }

                    uint underType = UnderlyingTypeOfEnum(tk);
                    if (underType == 0)
                    {
                        underType = (dataEnd - dp) switch
                        {
                            1 => ELEMENT_TYPE_BOOLEAN,
                            2 => ELEMENT_TYPE_I2,
                            4 => ELEMENT_TYPE_I4,
                            8 => ELEMENT_TYPE_I8,
                            _ => throw new CABlobException(),
                        };
                    }

                    byte[] ps = [(byte)underType];
                    int pp = 0;
                    PrettyPrintCABlobValue(ps, ref pp, data, ref dp, dataEnd, out_);
                    closeParenthesis = false;
                    break;
                }

                case ELEMENT_TYPE_SZARRAY:
                    numElements *= BitConverter.ToUInt32(data, dp);
                    reiterate = true;
                    appendix = "[" + ((int)numElements).ToString(inv) + "]";
                    if (numElements == 0xFFFFFFFF)
                    {
                        numElements = 0;
                    }

                    dp += 4;
                    break;
                case ELEMENT_TYPE_CMOD_OPT:
                case ELEMENT_TYPE_CMOD_REQD:
                case ELEMENT_TYPE_PINNED:
                    reiterate = true;
                    break;
                default:
                    throw new CABlobException();
            }
        }
        while (reiterate);

        if (closeParenthesis)
        {
            out_.Append(')');
        }
    }

    private static string Utf8Z(byte[] b, int start, int len)
    {
        int end = start + len;
        int z = Array.IndexOf(b, (byte)0, start, len);
        return Encoding.UTF8.GetString(b, start, (z < 0 ? end : z) - start);
    }

    private void AppendResolvedType(StringBuilder out_, byte[] data, int start, int len)
    {
        int tk = ResolveReflectionNotation(data, start, len);
        if (IsNilToken(tk))
        {
            out_.Append(KEYWORD("class ")).Append('\'').Append(UnquotedProperName(Utf8Z(data, start, len))).Append('\'');
        }
        else
        {
            PrettyPrintClass(out_, tk);
        }
    }

    private void PrettyPrintCustomAttributeNVPairs(uint nPairs, byte[] data, ref int dp, int dataEnd, StringBuilder out_)
    {
        while (dp < dataEnd)
        {
            switch (data[dp])
            {
                case SERIALIZATION_TYPE_FIELD:
                    out_.Append(KEYWORD("field "));
                    break;
                case SERIALIZATION_TYPE_PROPERTY:
                    out_.Append(KEYWORD("property "));
                    break;
                default:
                    throw new CABlobException();
            }

            dp++;
            if (dp >= dataEnd)
            {
                throw new CABlobException();
            }

            int dataTypePtr = dp;
            string append = "";
            if (data[dp] == ELEMENT_TYPE_SZARRAY)
            {
                append = "[]";
                dp++;
            }

            if (data[dp] == SERIALIZATION_TYPE_TYPE)
            {
                out_.Append(KEYWORD("type"));
                dp++;
            }
            else if (data[dp] == SERIALIZATION_TYPE_TAGGED_OBJECT)
            {
                out_.Append(KEYWORD("object"));
                dp++;
            }
            else if (data[dp] == SERIALIZATION_TYPE_ENUM)
            {
                out_.Append(KEYWORD("enum "));
                dp++;
                int l = (int)UncompressData(data, ref dp);
                if (dp + l > dataEnd)
                {
                    throw new CABlobException();
                }

                AppendResolvedType(out_, data, dp, l);
                dp += l;
            }
            else
            {
                append = "";
                dp = dataTypePtr;
                PrettyPrintType(data, ref dp, out_);
            }

            out_.Append(append);
            if (dp >= dataEnd)
            {
                throw new CABlobException();
            }

            int nameLen = (int)UncompressData(data, ref dp);
            if (dp + nameLen > dataEnd)
            {
                throw new CABlobException();
            }

            out_.Append(" '").Append(UnquotedProperName(Utf8Z(data, dp, nameLen))).Append("' = ");
            dp += nameLen;
            if (dp >= dataEnd)
            {
                throw new CABlobException();
            }

            PrettyPrintCABlobValue(data, ref dataTypePtr, data, ref dp, dataEnd, out_);
            out_.Append('\n');
            nPairs--;
        }
    }

    // Appends " = {...}" to line (possibly emitting preceding lines); returns false if the blob cannot be shown verbally.
    private bool PrettyPrintCustomAttributeBlob(int tkType, byte[] blob, StringBuilder line)
    {
        byte[] sig;
        if (TypeFromToken(tkType) == mdtMemberRef)
        {
            sig = GetMemberRefProps(tkType).Sig;
        }
        else if (TypeFromToken(tkType) == mdtMethodDef)
        {
            sig = GetSigOfMethodDef(tkType);
        }
        else
        {
            return false;
        }

        StringBuilder out_ = new();
        uint numElements;
        try
        {
            int tp = 0;
            uint callConv = UncompressData(sig, ref tp);
            if ((callConv & IMAGE_CEE_CS_CALLCONV_GENERIC) != 0)
            {
                return false;
            }

            uint numArgs = UncompressData(sig, ref tp);
            numElements = numArgs;
            if (!IsCallConv(callConv, IMAGE_CEE_CS_CALLCONV_GENERICINST))
            {
                PrettyPrintType(sig, ref tp, new StringBuilder());
            }

            out_.Append(" = {");
            int dp = 2;
            while (tp < sig.Length)
            {
                if (sig[tp] == ELEMENT_TYPE_SENTINEL)
                {
                    tp++;
                }
                else
                {
                    if (numArgs == 0)
                    {
                        break;
                    }

                    PrettyPrintCABlobValue(sig, ref tp, blob, ref dp, blob.Length - 2, out_);
                    out_.Append('\n');
                    --numArgs;
                }
            }

            ushort numPairs = BitConverter.ToUInt16(blob, dp);
            dp += 2;
            numElements += numPairs;
            PrettyPrintCustomAttributeNVPairs(numPairs, blob, ref dp, blob.Length, out_);
        }
        catch (Exception e) when (e is CABlobException or IndexOutOfRangeException or ArgumentException)
        {
            return false;
        }

        string[] segments = out_.ToString().Split('\n');
        int init = line.Length;
        if (Utf8Length(line) > 80)
        {
            PrintLine(line);
            line.Clear();
            line.Append(Indent).Append("        ");
            init = line.Length;
        }

        line.Append(segments[0]);
        init += 4;
        int bl = Indent.Length;
        for (int n = 1; n < numElements; n++)
        {
            PrintLine(line);
            line.Length = init;
            for (int i = bl; i < init; i++)
            {
                line[i] = ' ';
            }

            line.Append(n < segments.Length ? segments[n] : "");
        }

        line.Append('}');
        if (_options.ShowBytes)
        {
            PrintLine(line);
            PushIndent("  //    ");
            line.Clear();
            line.Append(Indent).Append(" = ( ");
            DumpByteArray(line, blob);
            PopIndent(8);
        }

        return true;
    }

    // DumpPermissionSetBlob: the XML-less (".") permission set format, shown as attribute-like name/value pairs.
    private bool DumpPermissionSetBlob(StringBuilder line, byte[] perm)
    {
        if (perm.Length == 0 || perm[0] != '.')
        {
            return false;
        }

        StringBuilder out_ = new();
        try
        {
            int p = 1;
            out_.Append(" = {");
            uint nAttrs = UncompressData(perm, ref p);
            for (uint iAttr = 0; iAttr < nAttrs; iAttr++)
            {
                int l = (int)UncompressData(perm, ref p);
                AppendResolvedType(out_, perm, p, l);
                p += l;
                out_.Append(" = {");
                l = (int)UncompressData(perm, ref p);
                if (l > 0)
                {
                    int end = p + l;
                    uint props = UncompressData(perm, ref p);
                    if (props > 0)
                    {
                        PrettyPrintCustomAttributeNVPairs(props, perm, ref p, end, out_);
                        out_.Length--;
                    }

                    p = end;
                }

                out_.Append(iAttr == nAttrs - 1 ? "}" : "}, ");
            }

            out_.Append('}');
        }
        catch (Exception e) when (e is CABlobException or IndexOutOfRangeException or ArgumentException)
        {
            return false;
        }

        string sz = out_.ToString();
        int prefixLen = line.Length;
        int szptr = prefixLen;
        int pc;
        while ((pc = sz.IndexOf("}, ", StringComparison.Ordinal)) >= 0)
        {
            line.Length = szptr;
            line.Append(sz, 0, pc + 2);
            PrintLine(line);
            sz = sz.Substring(pc + 3);
            if (szptr == prefixLen)
            {
                szptr += 4;
            }

            line.Clear();
            line.Append(' ', szptr);
        }

        line.Length = szptr;
        line.Append(sz);
        return true;
    }
}
