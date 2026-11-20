// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Text;

namespace ILDisassembler;

// Verbal custom attribute (/CAVERBAL) and permission set blobs.
//
// The blobs are walked with BlobReader instead of being decoded with CustomAttribute.DecodeValue, which cannot
// produce output that reassembles to the same blob:
//  - It reports a boxed (object) argument by the type of its value, so object(int32(5)) and int32(5) look the
//    same, but ilasm encodes them differently. For a named argument, the declared type is not available otherwise.
//  - It needs the underlying type of every enum through the type provider, without access to the blob. The
//    underlying type of an enum from another assembly is not known, and is instead inferred from the size of
//    the remaining value.
//  - It does not report whether the whole blob was consumed, so trailing data would be lost.
// System.Reflection.Metadata has no decoder for the binary permission set format.
// A blob that cannot be shown losslessly is dumped as bytes by the callers.
internal sealed partial class Dasm
{
    // The type of a value in a custom attribute blob.
    private sealed record CAType(SerializationTypeCode Code, CAType? Element = null, int EnumToken = 0, string? EnumName = null);

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
    private int ResolveReflectionNotation(string str)
    {
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

        foreach (AssemblyReferenceHandle h in _md.AssemblyReferences)
        {
            if (Str(_md.GetAssemblyReference(h).Name) == assembly)
            {
                return ResolveTypeRefReflectionNotation(ns, name, Token(h));
            }
        }

        return 0;
    }

    // The underlying type of an enum defined in this module, or null (enums of other assemblies cannot be resolved).
    private PrimitiveTypeCode? UnderlyingTypeOfEnum(int tk)
    {
        if (TypeFromToken(tk) != mdtTypeDef || !IsValidToken(tk))
        {
            return null;
        }

        (_, int parent) = GetTypeDefProps(tk);
        if (RidFromToken(parent) == 0 || !IsValidToken(parent))
        {
            return null;
        }

        (string name, string ns) = TypeFromToken(parent) switch
        {
            mdtTypeDef => GetNameOfTypeDef(parent),
            mdtTypeRef => GetNameOfTypeRef(parent),
            _ => ("", ""),
        };
        if (name != "Enum" || ns != "System")
        {
            return null;
        }

        foreach (FieldDefinitionHandle fh in _md.GetTypeDefinition((TypeDefinitionHandle)Handle(tk)).GetFields())
        {
            FieldDefinition fd = _md.GetFieldDefinition(fh);
            if ((fd.Attributes & FieldAttributes.Static) != 0)
            {
                continue;
            }

            BlobReader sig = _md.GetBlobReader(fd.Signature);
            if (sig.ReadSignatureHeader().Kind != SignatureKind.Field)
            {
                return null;
            }

            SignatureTypeCode code = sig.ReadSignatureTypeCode();
            return code is >= SignatureTypeCode.Boolean and <= SignatureTypeCode.UInt64 ? (PrimitiveTypeCode)code : null;
        }

        return null;
    }

    // ---------------------------------------------------------------- types

    private static bool IsPrimitive(SerializationTypeCode code)
        => code is >= SerializationTypeCode.Boolean and <= SerializationTypeCode.String;

    // A parameter type of the attribute constructor signature.
    private static CAType ReadParameterType(ref BlobReader sig)
    {
        while (true)
        {
            byte b = sig.ReadByte();
            switch ((SignatureTypeCode)b)
            {
                case SignatureTypeCode.RequiredModifier:
                case SignatureTypeCode.OptionalModifier:
                    sig.ReadTypeHandle();
                    continue;
                case SignatureTypeCode.Pinned:
                case SignatureTypeCode.Sentinel:
                    continue;
                case SignatureTypeCode.Object:
                    return new CAType(SerializationTypeCode.TaggedObject);
                case SignatureTypeCode.SZArray:
                    return new CAType(SerializationTypeCode.SZArray, ElementType(ReadParameterType(ref sig)));
                case (SignatureTypeCode)SignatureTypeKind.Class:
                    // Only System.Type is valid here.
                    sig.ReadTypeHandle();
                    return new CAType(SerializationTypeCode.Type);
                case (SignatureTypeCode)SignatureTypeKind.ValueType:
                    // Only an enum is valid here.
                    return new CAType(SerializationTypeCode.Enum, EnumToken: Token(sig.ReadTypeHandle()));
                default:
                    return IsPrimitive((SerializationTypeCode)b) ? new CAType((SerializationTypeCode)b) : throw new BadImageFormatException();
            }
        }
    }

    // A type encoded in the blob: the type of a named argument or of a boxed value.
    private static CAType ReadEncodedType(ref BlobReader r)
    {
        SerializationTypeCode code = r.ReadSerializationTypeCode();
        return code switch
        {
            SerializationTypeCode.Type or SerializationTypeCode.TaggedObject => new CAType(code),
            SerializationTypeCode.SZArray => new CAType(code, ElementType(ReadEncodedType(ref r))),
            SerializationTypeCode.Enum => new CAType(code, EnumName: ReadString(ref r) ?? throw new BadImageFormatException()),
            _ => IsPrimitive(code) ? new CAType(code) : throw new BadImageFormatException(),
        };
    }

    // Arrays of arrays are not valid in custom attributes.
    private static CAType ElementType(CAType t) => t.Code != SerializationTypeCode.SZArray ? t : throw new BadImageFormatException();

    // A SerString that can be written losslessly: one with embedded NULs or invalid UTF-8 (decoded as U+FFFD) cannot.
    private static string? ReadString(ref BlobReader r)
    {
        string? s = r.ReadSerializedString();
        if (s is not null && (s.Contains('\0') || s.Contains('\uFFFD')))
        {
            throw new BadImageFormatException();
        }

        return s;
    }

    private static string Keyword(SerializationTypeCode code) => code switch
    {
        SerializationTypeCode.Boolean => "bool",
        SerializationTypeCode.Char => "char",
        SerializationTypeCode.SByte => "int8",
        SerializationTypeCode.Byte => "uint8",
        SerializationTypeCode.Int16 => "int16",
        SerializationTypeCode.UInt16 => "uint16",
        SerializationTypeCode.Int32 => "int32",
        SerializationTypeCode.UInt32 => "uint32",
        SerializationTypeCode.Int64 => "int64",
        SerializationTypeCode.UInt64 => "uint64",
        SerializationTypeCode.Single => "float32",
        SerializationTypeCode.Double => "float64",
        SerializationTypeCode.String => "string",
        SerializationTypeCode.Type => "type",
        SerializationTypeCode.TaggedObject => "object",
        _ => throw new BadImageFormatException(),
    };

    private void AppendResolvedType(StringBuilder out_, string name)
    {
        int tk = ResolveReflectionNotation(name);
        if (IsNilToken(tk))
        {
            out_.Append(KEYWORD("class ")).Append('\'').Append(UnquotedProperName(name)).Append('\'');
        }
        else
        {
            PrettyPrintClass(out_, tk);
        }
    }

    // The type of a named argument: "int32", "string[]", "type", "object[]", "enum class 'N.E'"...
    private void AppendEncodedType(StringBuilder out_, CAType t)
    {
        switch (t.Code)
        {
            case SerializationTypeCode.SZArray:
                AppendEncodedType(out_, t.Element!);
                out_.Append("[]");
                break;
            case SerializationTypeCode.Enum:
                out_.Append(KEYWORD("enum "));
                AppendResolvedType(out_, t.EnumName!);
                break;
            default:
                out_.Append(KEYWORD(Keyword(t.Code)));
                break;
        }
    }

    // ---------------------------------------------------------------- values

    // Appends a value of type t, such as "int32(5)", "string[2]('a' nullref)" or "object(type(class C))",
    // read from r, which must not go past end.
    private void AppendValue(ref BlobReader r, int end, CAType t, StringBuilder out_)
    {
        if (r.Offset >= end)
        {
            throw new BadImageFormatException();
        }

        string appendix = "";
        int count = 1;
        bool isArray = t.Code == SerializationTypeCode.SZArray;
        if (isArray)
        {
            int n = r.ReadInt32();
            if (n < -1)
            {
                throw new BadImageFormatException();
            }

            appendix = "[" + n.ToString(CultureInfo.InvariantCulture) + "]";
            count = n == -1 ? 0 : n; // -1 is a null array
            t = t.Element!;
        }

        // An enum value is shown as its underlying type.
        SerializationTypeCode code = t.Code == SerializationTypeCode.Enum ? UnderlyingTypeOfEnumValue(t, end - r.Offset, isArray) : t.Code;
        out_.Append(KEYWORD(Keyword(code))).Append(appendix).Append('(');
        for (int i = 0; i < count; i++)
        {
            if (i != 0)
            {
                out_.Append(' ');
            }

            AppendElement(ref r, end, code, out_);
            if (r.Offset > end)
            {
                throw new BadImageFormatException();
            }
        }

        // An empty array gets "( )": managed ilasm reads "()" as a single token.
        out_.Append(count == 0 && isArray ? " )" : ")");
    }

    private SerializationTypeCode UnderlyingTypeOfEnumValue(CAType t, int remaining, bool isArray)
    {
        PrimitiveTypeCode? underlying = UnderlyingTypeOfEnum(t.EnumToken != 0 ? t.EnumToken : ResolveReflectionNotation(t.EnumName!));
        if (underlying is not null)
        {
            return (SerializationTypeCode)underlying.Value;
        }

        // Unknown enum: infer the size from the rest of the value, which only works for the last one.
        return isArray ? throw new BadImageFormatException() : remaining switch
        {
            1 => SerializationTypeCode.Byte,
            2 => SerializationTypeCode.Int16,
            4 => SerializationTypeCode.Int32,
            8 => SerializationTypeCode.Int64,
            _ => throw new BadImageFormatException(),
        };
    }

    private void AppendElement(ref BlobReader r, int end, SerializationTypeCode code, StringBuilder out_)
    {
        CultureInfo inv = CultureInfo.InvariantCulture;
        switch (code)
        {
            case SerializationTypeCode.Boolean:
                out_.Append(KEYWORD(r.ReadByte() switch
                {
                    0 => "false",
                    1 => "true",
                    _ => throw new BadImageFormatException(), // not representable as bool(...)
                }));
                break;
            case SerializationTypeCode.Char:
                out_.Append("0x").Append(r.ReadUInt16().ToString("X4", inv));
                break;
            case SerializationTypeCode.SByte:
                out_.Append(r.ReadSByte().ToString(inv));
                break;
            case SerializationTypeCode.Byte:
                out_.Append(r.ReadByte().ToString(inv));
                break;
            case SerializationTypeCode.Int16:
                out_.Append(r.ReadInt16().ToString(inv));
                break;
            case SerializationTypeCode.UInt16:
                out_.Append(r.ReadUInt16().ToString(inv));
                break;
            case SerializationTypeCode.Int32:
            case SerializationTypeCode.UInt32:
                // Both are shown as signed.
                out_.Append(r.ReadInt32().ToString(inv));
                break;
            case SerializationTypeCode.Int64:
            case SerializationTypeCode.UInt64:
                out_.Append(r.ReadInt64().ToString(inv));
                break;
            case SerializationTypeCode.Single:
            {
                uint bits = r.ReadUInt32();
                string s = FormatG(BitConverter.UInt32BitsToSingle(bits), 8, alternate: true);
                if (BitConverter.SingleToUInt32Bits((float)Atof(s)) != bits || IsSpecialNumber(s))
                {
                    s = "0x" + bits.ToString("X8", inv);
                }

                out_.Append(s);
                break;
            }

            case SerializationTypeCode.Double:
            {
                ulong bits = r.ReadUInt64();
                string s = FormatG(BitConverter.UInt64BitsToDouble(bits), 17, alternate: true);
                if (BitConverter.DoubleToUInt64Bits(Atof(s)) != bits || IsSpecialNumber(s))
                {
                    s = "0x" + bits.ToString("X", inv);
                }

                out_.Append(s);
                break;
            }

            case SerializationTypeCode.String:
            {
                string? s = ReadString(ref r);
                if (s is null)
                {
                    out_.Append(KEYWORD("nullref"));
                }
                else
                {
                    out_.Append('\'').Append(UnquotedProperName(s)).Append('\'');
                }

                break;
            }

            case SerializationTypeCode.Type:
            {
                string? s = ReadString(ref r);
                if (s is null)
                {
                    out_.Append(KEYWORD("nullref"));
                }
                else
                {
                    AppendResolvedType(out_, s);
                }

                break;
            }

            case SerializationTypeCode.TaggedObject:
                AppendValue(ref r, end, ReadEncodedType(ref r), out_);
                break;
            default:
                throw new BadImageFormatException();
        }
    }

    // Appends count named arguments, one per line, which must end exactly at end.
    private void AppendNamedArguments(ref BlobReader r, int count, int end, StringBuilder out_)
    {
        for (int i = 0; i < count; i++)
        {
            if (r.Offset >= end)
            {
                throw new BadImageFormatException();
            }

            out_.Append(KEYWORD((CustomAttributeNamedArgumentKind)r.ReadByte() switch
            {
                CustomAttributeNamedArgumentKind.Field => "field ",
                CustomAttributeNamedArgumentKind.Property => "property ",
                _ => throw new BadImageFormatException(),
            }));
            CAType t = ReadEncodedType(ref r);
            AppendEncodedType(out_, t);
            string name = ReadString(ref r) ?? throw new BadImageFormatException();
            out_.Append(" '").Append(UnquotedProperName(name)).Append("' = ");
            AppendValue(ref r, end, t, out_);
            out_.Append('\n');
        }

        if (r.Offset != end)
        {
            throw new BadImageFormatException();
        }
    }

    // ---------------------------------------------------------------- custom attributes

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
        int numElements;
        try
        {
            numElements = AppendCustomAttributeValue(sig, blob, out_);
        }
        catch (BadImageFormatException)
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

    // Appends the constructor arguments and named arguments, one per line; returns their number.
    private unsafe int AppendCustomAttributeValue(byte[] signature, byte[] blob, StringBuilder out_)
    {
        fixed (byte* pSig = signature, pBlob = blob)
        {
            BlobReader sig = new(pSig, signature.Length);
            BlobReader r = new(pBlob, blob.Length);
            SignatureHeader header = sig.ReadSignatureHeader();
            if (header.Kind != SignatureKind.Method || header.IsGeneric)
            {
                throw new BadImageFormatException();
            }

            int numArgs = sig.ReadCompressedInteger();
            SignatureTypeCode returnType;
            while ((returnType = sig.ReadSignatureTypeCode()) is SignatureTypeCode.RequiredModifier or SignatureTypeCode.OptionalModifier)
            {
                sig.ReadTypeHandle();
            }

            if (returnType != SignatureTypeCode.Void || r.ReadUInt16() != 1) // the prolog
            {
                throw new BadImageFormatException();
            }

            out_.Append(" = {");
            int argsEnd = blob.Length - 2; // the number of named arguments follows
            for (int i = 0; i < numArgs; i++)
            {
                AppendValue(ref r, argsEnd, ReadParameterType(ref sig), out_);
                out_.Append('\n');
            }

            int numPairs = r.ReadUInt16();
            AppendNamedArguments(ref r, numPairs, blob.Length, out_);
            return numArgs + numPairs;
        }
    }

    // ---------------------------------------------------------------- permission sets

    // DumpPermissionSetBlob: the binary (".") permission set format, shown as attribute-like name/value pairs.
    private bool DumpPermissionSetBlob(StringBuilder line, BlobHandle permissionSet)
    {
        StringBuilder out_ = new();
        try
        {
            BlobReader r = _md.GetBlobReader(permissionSet);
            if (r.Length == 0 || r.ReadByte() != '.')
            {
                return false;
            }

            out_.Append(" = {");
            int nAttrs = r.ReadCompressedInteger();
            for (int iAttr = 0; iAttr < nAttrs; iAttr++)
            {
                AppendResolvedType(out_, ReadString(ref r) ?? throw new BadImageFormatException());
                out_.Append(" = {");
                int l = r.ReadCompressedInteger();
                if (l > 0)
                {
                    int end = r.Offset + l;
                    int props = r.ReadCompressedInteger();
                    AppendNamedArguments(ref r, props, end, out_);
                    if (props > 0)
                    {
                        out_.Length--; // the last newline
                    }
                }

                out_.Append(iAttr == nAttrs - 1 ? "}" : "}, ");
            }

            if (r.RemainingBytes != 0)
            {
                return false;
            }

            out_.Append('}');
        }
        catch (BadImageFormatException)
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
