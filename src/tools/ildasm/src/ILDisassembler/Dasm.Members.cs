// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Text;

namespace ILDisassembler;

// Classes and their members.
internal sealed partial class Dasm
{
    // ---------------------------------------------------------------- custom attributes

    private void DumpCustomAttributes(int tkOwner)
    {
        if (!ShowCustomAttributes)
        {
            return;
        }

        foreach (CustomAttributeHandle h in GetCustomAttributes(tkOwner))
        {
            DumpCustomAttribute(Token(h), withOwner: false);
        }
    }

    private void DumpCustomAttribute(int tkCA, bool withOwner)
    {
        CustomAttribute ca = _md.GetCustomAttribute(MetadataTokens.CustomAttributeHandle(RidFromToken(tkCA)));
        int tkOwner = Token(ca.Parent);
        int tkType = Token(ca.Constructor);
        byte[] blob = Blob(ca.Value);
        if (RidFromToken(tkOwner) == 0)
        {
            return;
        }

        bool found = false;
        foreach (TypeDefDescr td in _typedefs)
        {
            if (TypeFromToken(td.TkTypeSpec) != mdtCustomAttribute || td.Sig is null || td.Sig.Length < 8)
            {
                continue;
            }

            if ((int)ReadUInt32(td.Sig, 0) != tkType || !td.Sig.AsSpan(8).SequenceEqual(blob))
            {
                continue;
            }

            StringBuilder sb = new();
            sb.Append(Indent).Append(JUMPPT(ProperName(td.Name), td.TkSelf));
            if (_options.ShowTokens)
            {
                sb.Append(COMMENT($"/*{tkCA:X8}*/ "));
            }

            PrintLine(sb);
            found = true;
            break;
        }

        if (!found)
        {
            DumpCustomAttributeProps(tkCA, tkType, tkOwner, blob, withOwner);
        }

        _caDumped.Add(RidFromToken(tkCA));
    }

    private void DumpCustomAttributeProps(int tkCA, int tkType, int tkOwner, byte[] blob, bool withOwner)
    {
        bool commentItOut = false;
        if (TypeFromToken(tkType) == mdtMemberRef || TypeFromToken(tkType) == mdtMethodDef)
        {
            int tkParent = TypeFromToken(tkType) == mdtMemberRef ? GetMemberRefProps(tkType).Parent : GetParentOfMethodDef(tkType);
            string? className = null, ns = null;
            if (TypeFromToken(tkParent) == mdtTypeDef && IsValidToken(tkParent))
            {
                (className, ns) = GetNameOfTypeDef(tkParent);
            }
            else if (TypeFromToken(tkParent) == mdtTypeRef && IsValidToken(tkParent))
            {
                (className, ns) = GetNameOfTypeRef(tkParent);
            }

            commentItOut = ns == "System.Diagnostics" && className == "DebuggableAttribute";
        }

        if (commentItOut)
        {
            PrintLine(CommentPrefix);
            PrintLine($"{Indent}// --- The following custom attribute is added automatically, do not uncomment -------");
            PushIndent("//  ");
        }

        StringBuilder sb = new();
        sb.Append(Indent).Append(KEYWORD(".custom")).Append(' ');
        if (withOwner)
        {
            if (_options.ShowTokens)
            {
                sb.Append(COMMENT($"/*{tkCA:X8}*/ "));
            }

            sb.Append('(');
            switch (TypeFromToken(tkOwner))
            {
                case mdtTypeDef:
                case mdtTypeRef:
                case mdtTypeSpec:
                    PrettyPrintToken(sb, tkOwner);
                    break;
                case mdtMemberRef:
                    sb.Append(KEYWORD(IsFieldMemberRef(tkOwner) ? "field " : "method "));
                    PrettyPrintToken(sb, tkOwner);
                    break;
                case mdtMethodDef:
                    sb.Append(KEYWORD("method "));
                    PrettyPrintToken(sb, tkOwner);
                    break;
                default:
                    sb.Append(ERRORMSG("UNKNOWN_OWNER"));
                    break;
            }

            if (_options.ShowTokens)
            {
                sb.Append(COMMENT($"/*{tkOwner:X8}*/ "));
            }

            sb.Append(") ");
        }
        else if (_options.ShowTokens)
        {
            sb.Append(COMMENT($"/*{tkCA:X8}:{tkType:X8}*/ "));
        }

        switch (TypeFromToken(tkType))
        {
            case mdtTypeDef:
            case mdtTypeRef:
            case mdtMemberRef:
            case mdtMethodDef:
                PrettyPrintToken(sb, tkType);
                break;
            default:
                sb.Append(ERRORMSG("UNNAMED_CUSTOM_ATTR"));
                break;
        }

        if (blob.Length != 0)
        {
            if (!_options.CustomAttributesVerbal || !PrettyPrintCustomAttributeBlob(tkType, blob, sb))
            {
                sb.Append(" = ( ");
                DumpByteArray(sb, blob);
            }
        }

        PrintLine(sb);
        if (commentItOut)
        {
            PopIndent(4);
            PrintLine(CommentPostfix);
        }
    }

    // ---------------------------------------------------------------- default values

    private void DumpDefaultValue(int tok, StringBuilder sb)
    {
        ConstantHandle ch = GetConstant(tok);
        if (ch.IsNil)
        {
            sb.Append(" /* NO CORRESPONDING RECORD IN CONSTANTS TABLE */");
            return;
        }

        Constant c = _md.GetConstant(ch);
        byte[] v = Blob(c.Value);
        byte type = (byte)c.TypeCode;
        CultureInfo inv = CultureInfo.InvariantCulture;
        switch (type)
        {
            case ELEMENT_TYPE_I1:
                sb.Append(" = ").Append(KEYWORD("int8")).Append("(0x").Append(ValueOrZero(v, 1).ToString("X2", inv)).Append(')');
                break;
            case ELEMENT_TYPE_U1:
                sb.Append(" = ").Append(KEYWORD("uint8")).Append("(0x").Append(ValueOrZero(v, 1).ToString("X2", inv)).Append(')');
                break;
            case ELEMENT_TYPE_I2:
                sb.Append(" = ").Append(KEYWORD("int16")).Append("(0x").Append(ValueOrZero(v, 2).ToString("X4", inv)).Append(')');
                break;
            case ELEMENT_TYPE_U2:
                sb.Append(" = ").Append(KEYWORD("uint16")).Append("(0x").Append(ValueOrZero(v, 2).ToString("X4", inv)).Append(')');
                break;
            case ELEMENT_TYPE_I4:
                sb.Append(" = ").Append(KEYWORD("int32")).Append("(0x").Append(ValueOrZero(v, 4).ToString("X8", inv)).Append(')');
                break;
            case ELEMENT_TYPE_U4:
                sb.Append(" = ").Append(KEYWORD("uint32")).Append("(0x").Append(ValueOrZero(v, 4).ToString("X8", inv)).Append(')');
                break;
            case ELEMENT_TYPE_CHAR:
                sb.Append(" = ").Append(KEYWORD("char")).Append("(0x").Append(ValueOrZero(v, 2).ToString("X4", inv)).Append(')');
                break;
            case ELEMENT_TYPE_BOOLEAN:
                sb.Append(" = ").Append(KEYWORD("bool")).Append('(').Append(KEYWORD(ValueOrZero(v, 1) != 0 ? "true" : "false")).Append(')');
                break;
            case ELEMENT_TYPE_I8:
                sb.Append(" = ").Append(KEYWORD("int64")).Append("(0x").Append(ValueOrZero(v, 8).ToString("X", inv)).Append(')');
                break;
            case ELEMENT_TYPE_U8:
                sb.Append(" = ").Append(KEYWORD("uint64")).Append("(0x").Append(ValueOrZero(v, 8).ToString("X", inv)).Append(')');
                break;
            case ELEMENT_TYPE_R4:
            {
                uint bits = (uint)ValueOrZero(v, 4);
                float f = BitConverter.UInt32BitsToSingle(bits);
                string szf = FormatG(f, 8, alternate: true);
                float df = (float)Atof(szf);
                if (BitConverter.SingleToUInt32Bits(df) == bits && !IsSpecialNumber(szf))
                {
                    sb.Append(" = ").Append(KEYWORD("float32")).Append('(').Append(szf).Append(')');
                }
                else
                {
                    sb.Append(" = ").Append(KEYWORD("float32")).Append("(0x").Append(bits.ToString("X8", inv)).Append(')');
                }

                break;
            }

            case ELEMENT_TYPE_R8:
            {
                ulong bits = ValueOrZero(v, 8);
                double d = BitConverter.UInt64BitsToDouble(bits);
                string szf = FormatG(d, 17, alternate: true);
                double df = Atof(szf);
                if (BitConverter.DoubleToUInt64Bits(df) == bits && !IsSpecialNumber(szf))
                {
                    sb.Append(" = ").Append(KEYWORD("float64")).Append('(').Append(szf).Append(')');
                }
                else
                {
                    sb.Append(" = ").Append(KEYWORD("float64")).Append("(0x").Append(bits.ToString("X", inv)).Append(") // ").Append(szf);
                }

                break;
            }

            case ELEMENT_TYPE_STRING:
                sb.Append(" = ");
                DumpUnicodeString(sb, v);
                break;

            case ELEMENT_TYPE_CLASS when v.Length == 0 || IsAllZero(v):
                sb.Append(" = ").Append(KEYWORD("nullref"));
                break;

            default:
                sb.Append(ERRORMSG($" /* ILLEGAL CONSTANT type:0x{type:X2}, size:{v.Length} bytes, blob: "));
                if (v.Length != 0)
                {
                    sb.Append('(');
                    DumpByteArray(sb, v);
                }
                else
                {
                    sb.Append("NULL");
                }

                sb.Append(" */");
                break;
        }

        static ulong ValueOrZero(byte[] v, int size)
        {
            ulong r = 0;
            for (int i = 0; i < size && i < v.Length; i++)
            {
                r |= (ulong)v[i] << (8 * i);
            }

            return r;
        }

        static bool IsAllZero(byte[] v)
        {
            foreach (byte b in v)
            {
                if (b != 0)
                {
                    return false;
                }
            }

            return v.Length <= 4;
        }
    }

    // ---------------------------------------------------------------- params

    private void DumpParams(ParamDescriptor[]? pd, int ulParams)
    {
        if (pd is null)
        {
            return;
        }

        for (int i = ulParams; i < 2 * ulParams + 1; i++)
        {
            int j = i % (ulParams + 1);
            if (RidFromToken(pd[j].Tok) == 0)
            {
                continue;
            }

            ImmutableArray<CustomAttributeHandle> cas = ShowCustomAttributes ? GetCustomAttributes(pd[j].Tok) : ImmutableArray<CustomAttributeHandle>.Empty;
            int ulCAs = ShowCustomAttributes ? cas.Length : 0;
            bool hasDefault = (pd[j].Attr & 0x1000) != 0;
            if (ulCAs != 0 || hasDefault)
            {
                StringBuilder sb = new();
                sb.Append(Indent).Append(KEYWORD(".param")).Append(" [").Append((i - ulParams).ToString(CultureInfo.InvariantCulture)).Append(']');
                if (_options.ShowTokens)
                {
                    sb.Append(COMMENT($"/*{pd[j].Tok:X8}*/ "));
                }

                if (hasDefault)
                {
                    DumpDefaultValue(pd[j].Tok, sb);
                }

                PrintLine(sb);
                if (ulCAs != 0)
                {
                    foreach (CustomAttributeHandle h in cas)
                    {
                        DumpCustomAttribute(Token(h), withOwner: false);
                    }
                }
            }
        }
    }

    // ---------------------------------------------------------------- permissions

    private void DumpPermissions(int tkOwner)
    {
        foreach (DeclarativeSecurityAttributeHandle h in GetDeclSecurity(tkOwner))
        {
            DeclarativeSecurityAttribute ds = _md.GetDeclarativeSecurityAttribute(h);
            byte[] blob = Blob(ds.PermissionSet);
            StringBuilder sb = new();
            sb.Append(Indent).Append(KEYWORD(".permissionset")).Append(' ');
            string action = (ushort)ds.Action switch
            {
                0 => "",
                1 => KEYWORD("request"),
                2 => KEYWORD("demand"),
                3 => KEYWORD("assert"),
                4 => KEYWORD("deny"),
                5 => KEYWORD("permitonly"),
                6 => KEYWORD("linkcheck"),
                7 => KEYWORD("inheritcheck"),
                8 => KEYWORD("reqmin"),
                9 => KEYWORD("reqopt"),
                10 => KEYWORD("reqrefuse"),
                11 => KEYWORD("prejitgrant"),
                12 => KEYWORD("prejitdeny"),
                13 => KEYWORD("noncasdemand"),
                14 => KEYWORD("noncaslinkdemand"),
                15 => KEYWORD("noncasinheritance"),
                _ => ERRORMSG("<UNKNOWN_ACTION>"),
            };
            sb.Append(action);
            if (blob.Length != 0)
            {
                PrintLine(sb);
                sb.Clear();
                sb.Append(Indent).Append("          ");
                if (!DumpPermissionSetBlob(sb, ds.PermissionSet))
                {
                    sb.Append(KEYWORD("bytearray")).Append(" (");
                    DumpByteArray(sb, blob);
                }

                PrintLine(sb);
            }
            else
            {
                sb.Append(" = ()");
                PrintLine(sb);
            }

            DumpCustomAttributes(Token(h));
        }
    }

    // ---------------------------------------------------------------- generic parameters

    private string DumpGenericPars(int tok, string prefix, bool split)
    {
        StringBuilder sb = new(prefix);
        ImmutableArray<GenericParameterHandle> gps = GetGenericParams(tok);
        if (gps.Length == 0)
        {
            return sb.ToString();
        }

        sb.Append(LTN());
        int begin = sb.Length;
        int i = 1;
        int n = 0;
        foreach (GenericParameterHandle gph in gps)
        {
            GenericParameter gp = _md.GetGenericParameter(gph);
            int attr = (int)gp.Attributes;
            switch (attr & 0x3)
            {
                case 1: sb.Append("+ "); break;
                case 2: sb.Append("- "); break;
            }

            if ((attr & 0x4) != 0)
            {
                sb.Append("class ");
            }

            if ((attr & 0x8) != 0)
            {
                sb.Append("valuetype ");
            }

            if ((attr & 0x20) != 0)
            {
                sb.Append("byreflike ");
            }

            if ((attr & 0x10) != 0)
            {
                sb.Append(".ctor ");
            }

            int unknown = attr & ~(0x3C | 0x3);
            if (unknown != 0)
            {
                sb.Append("flags(0x").Append(unknown.ToString("x", CultureInfo.InvariantCulture)).Append(") ");
            }

            GenericParameterConstraintHandleCollection constraints = gp.GetConstraints();
            if (constraints.Count != 0)
            {
                sb.Append('(');
                int ix = 0;
                foreach (GenericParameterConstraintHandle c in constraints)
                {
                    if (ix++ != 0)
                    {
                        sb.Append(", ");
                    }

                    sb.Append(PrettyPrintClass(Token(_md.GetGenericParameterConstraint(c).Type)));
                }

                sb.Append(") ");
            }

            string name = Str(gp.Name);
            if (name.Length != 0)
            {
                sb.Append(ProperName(name));
            }

            n++;
            if (n < gps.Length)
            {
                sb.Append(',');
                if (split && i == 4)
                {
                    PrintLine(sb);
                    i = 0;
                    int pad = Utf8Length(sb.Remove(begin, sb.Length - begin)) ;
                    sb.Clear();
                    sb.Append(' ', pad);
                }
            }

            i++;
        }

        sb.Append(GTN());
        return sb.ToString();
    }

    private void DumpGenericParsCA(int tok)
    {
        if (!ShowCustomAttributes)
        {
            return;
        }

        int i = 0;
        foreach (GenericParameterHandle gph in GetGenericParams(tok))
        {
            GenericParameter gp = _md.GetGenericParameter(gph);
            int tkTyPar = Token(gph);
            CustomAttributeHandleCollection cas = _md.GetCustomAttributes(gph);
            if (cas.Count != 0)
            {
                StringBuilder sb = new();
                sb.Append(Indent).Append(KEYWORD(".param type")).Append(' ');
                string name = Str(gp.Name);
                if (name.Length != 0)
                {
                    sb.Append(ProperName(name)).Append(' ');
                }
                else
                {
                    sb.Append('[').Append((i + 1).ToString(CultureInfo.InvariantCulture)).Append("] ");
                }

                if (_options.ShowTokens)
                {
                    sb.Append(COMMENT($"/*{tkTyPar:X8}*/ "));
                }

                PrintLine(sb);
                PushIndent();
                foreach (CustomAttributeHandle h in cas)
                {
                    DumpCustomAttribute(Token(h), withOwner: false);
                }

                PopIndent();
            }

            foreach (GenericParameterConstraintHandle ch in gp.GetConstraints())
            {
                GenericParameterConstraint c = _md.GetGenericParameterConstraint(ch);
                CustomAttributeHandleCollection ccas = _md.GetCustomAttributes(ch);
                if (ccas.Count == 0)
                {
                    continue;
                }

                StringBuilder sb = new();
                sb.Append(Indent).Append(KEYWORD(".param constraint")).Append(' ');
                string name = Str(gp.Name);
                if (name.Length != 0)
                {
                    sb.Append("  ").Append(ProperName(name));
                }
                else
                {
                    sb.Append("  [").Append((gp.Index + 1).ToString(CultureInfo.InvariantCulture)).Append(']');
                }

                if (_options.ShowTokens)
                {
                    sb.Append(COMMENT($"/*{tkTyPar:X8}*/ "));
                }

                sb.Append(", ").Append(PrettyPrintClass(Token(c.Type)));
                PrintLine(sb);
                PushIndent();
                foreach (CustomAttributeHandle h in ccas)
                {
                    DumpCustomAttribute(Token(h), withOwner: false);
                }

                PopIndent();
            }

            i++;
        }
    }

    // ---------------------------------------------------------------- method signature line splitting

    private string PrettyPrintMethodSig(StringBuilder line, byte[] sig, string buff, SigLabel? label)
    {
        int uMaxWidth = _html || _rtf ? 240 : 40;
        if (buff.Length != 0 && Utf8Length(line) > uMaxWidth)
        {
            PrintLine(line);
            line.Clear();
            line.Append(Indent).Append("        ");
        }

        string tail = PrettyPrintSig(sig, buff, line.ToString(), label);
        if (buff.Length == 0)
        {
            return tail;
        }

        string newbuff = " " + buff + "(";
        int offset = tail.IndexOf(newbuff, StringComparison.Ordinal);
        if (offset < 0)
        {
            return tail;
        }

        if (Encoding.UTF8.GetByteCount(tail.AsSpan(0, offset)) > uMaxWidth)
        {
            int offset2 = tail.IndexOf(" marshal(", StringComparison.Ordinal);
            if (offset2 >= 0 && offset2 < offset)
            {
                PrintLine(tail.Substring(0, offset2));
                tail = Indent + "        " + tail.Substring(offset2 + 1);
                offset = tail.IndexOf(newbuff, StringComparison.Ordinal);
            }

            PrintLine(tail.Substring(0, offset));
            tail = Indent + "        " + tail.Substring(offset + 1);
            offset = tail.IndexOf(newbuff, StringComparison.Ordinal);
        }

        int indent = Encoding.UTF8.GetByteCount(tail.AsSpan(0, offset)) + Encoding.UTF8.GetByteCount(buff) + 2;
        string ltn = LTN();
        string gtn = GTN();
        int pComma = tail.IndexOf(',', Math.Min(tail.Length, ByteOffsetToCharIndex(tail, Encoding.UTF8.GetByteCount(buff))));
        while (pComma >= 0)
        {
            int i = 0, j = 0, k = 0, l = 0;
            for (int pch = 0; pch < pComma; pch++)
            {
                char c = tail[pch];
                if (c == '\\')
                {
                    pch++;
                }
                else if (c == '\'')
                {
                    j = 1 - j;
                }
                else if (c == '"')
                {
                    k = 1 - k;
                }
                else if (j == 0)
                {
                    if (c == '[')
                    {
                        i++;
                    }
                    else if (c == ']')
                    {
                        i--;
                    }
                    else if (string.CompareOrdinal(tail, pch, ltn, 0, ltn.Length) == 0)
                    {
                        l++;
                    }
                    else if (string.CompareOrdinal(tail, pch, gtn, 0, gtn.Length) == 0)
                    {
                        l--;
                    }
                }
            }

            pComma++;
            if (i == 0 && j == 0 && k == 0 && l == 0)
            {
                PrintLine(tail.Substring(0, pComma));
                tail = new string(' ', indent) + tail.Substring(pComma);
                pComma = tail.IndexOf(',');
            }
            else
            {
                pComma = tail.IndexOf(',', pComma);
            }
        }

        return tail;
    }

    // ---------------------------------------------------------------- overrides

    private string PrettyPrintOverrideDecl(MethodImplRecord mi, int tkOverrider, out bool overridingTypeSpec)
    {
        StringBuilder sb = new();
        sb.Append(Indent).Append(KEYWORD(".override")).Append(' ');
        int tkDecl = mi.TkDecl;
        int tkDeclParent = 0;
        string tailSig = "";
        string memberName;
        overridingTypeSpec = false;

        if (IsValidToken(tkDecl))
        {
            bool needsFullTokenPrint = false;
            bool hasTkDeclParent = false;
            tkDeclParent = GetParentToken(tkDecl);
            if (IsValidToken(tkDeclParent) || TypeFromToken(tkDeclParent) == mdtModule)
            {
                if (TypeFromToken(tkDeclParent) == mdtMethodDef)
                {
                    tkDeclParent = GetParentOfMethodDef(tkDeclParent);
                }

                if (RidFromToken(tkDeclParent) != 0)
                {
                    if (TypeFromToken(tkDeclParent) == mdtTypeSpec)
                    {
                        needsFullTokenPrint = true;
                    }

                    hasTkDeclParent = true;
                }
            }
            else
            {
                sb.Append(ERRORMSG("INVALID OVERRIDDEN METHOD'S PARENT TOKEN"));
            }

            if (TypeFromToken(tkOverrider) == mdtMethodDef && !needsFullTokenPrint)
            {
                int declSigTok = tkDecl;
                if (TypeFromToken(declSigTok) == mdtMethodSpec)
                {
                    declSigTok = GetMethodSpecProps(declSigTok).Method;
                }

                byte[]? declSig = TypeFromToken(declSigTok) switch
                {
                    mdtMethodDef => GetSigOfMethodDef(declSigTok),
                    mdtMemberRef => GetMemberRefProps(declSigTok).Sig,
                    _ => null,
                };
                byte[] bodySig = GetSigOfMethodDef(tkOverrider);
                if (declSig is not null)
                {
                    if (!SigEquals(declSig, bodySig))
                    {
                        needsFullTokenPrint = true;
                    }
                }
                else
                {
                    sb.Append(ERRORMSG("INVALID BODY OR DECL SIG"));
                }
            }

            if (needsFullTokenPrint)
            {
                sb.Append(' ').Append(KEYWORD("method")).Append(' ');
                PrettyPrintToken(sb, tkDecl);
                overridingTypeSpec = true;
                return sb.ToString();
            }

            if (hasTkDeclParent)
            {
                PrettyPrintToken(sb, tkDeclParent);
                sb.Append("::");
            }

            if (TypeFromToken(tkDecl) == mdtMethodSpec)
            {
                (int meth, byte[] inst) = GetMethodSpecProps(tkDecl);
                if (inst.Length != 0)
                {
                    tailSig = PrettyPrintSig(inst, "");
                }

                tkDecl = meth;
            }

            if (TypeFromToken(tkDecl) == mdtMethodDef)
            {
                memberName = GetNameOfMethodDef(tkDecl);
            }
            else if (TypeFromToken(tkDecl) == mdtMemberRef)
            {
                memberName = GetMemberRefProps(tkDecl).Name;
            }
            else
            {
                memberName = ERRORMSG($"INVALID TOKEN: 0x{tkDecl:X8}");
            }

            memberName = MakeNameIfNone(memberName, tkDecl);
        }
        else
        {
            memberName = ERRORMSG($"INVALID TOKEN: 0x{tkDecl:X8}");
        }

        sb.Append(ProperName(memberName)).Append(tailSig);
        if (_options.ShowTokens)
        {
            sb.Append(COMMENT($" /*{tkDeclParent:X8}::{mi.TkDecl:X8}*/ "));
        }

        return sb.ToString();
    }

    // ---------------------------------------------------------------- visibility filtering

    private bool IsHiddenMethod(MethodAttributes a)
    {
        if (!_limitedVisibility)
        {
            return false;
        }

        return (a & MethodAttributes.MemberAccessMask) switch
        {
            MethodAttributes.Public => _hidePub,
            MethodAttributes.Private => _hidePriv,
            MethodAttributes.Family => _hideFam,
            MethodAttributes.Assembly => _hideAsm,
            MethodAttributes.FamORAssem => _hideFOA,
            MethodAttributes.FamANDAssem => _hideFAA,
            _ => _hidePrivScope,
        };
    }

    private bool IsHiddenField(FieldAttributes a) => IsHiddenMethod((MethodAttributes)(int)(a & FieldAttributes.FieldAccessMask));

    // ---------------------------------------------------------------- methods

    private bool DumpMethod(int funcToken, string? className, int entryPointToken, bool dumpBody)
    {
        MethodDefinition md = _md.GetMethodDefinition((MethodDefinitionHandle)Handle(funcToken));
        MethodAttributes attrs = md.Attributes;
        if (IsHiddenMethod(attrs))
        {
            return false;
        }

        MethodImplAttributes implAttrs = md.ImplAttributes;
        uint rva = (uint)md.RelativeVirtualAddress;
        string memberName = MakeNameIfNone(Str(md.Name), funcToken);
        byte[] sig = Blob(md.Signature);
        if (sig.Length == 0)
        {
            PrintError(ERRORMSG($"{Indent}ERROR: method '{memberName}' has no signature"));
            return false;
        }

        int savedMVarOwner = _tkMVarOwner;
        _tkMVarOwner = funcToken;
        string plainSig = PrettyPrintSig(sig, DumpGenericPars(funcToken, "", false));

        if (_mode is DumpMode.ClassMethod or DumpMode.ClassMethodSig)
        {
            if (memberName != _methodToDump)
            {
                _tkMVarOwner = savedMVarOwner;
                return false;
            }

            if (_mode == DumpMode.ClassMethodSig)
            {
                bool tokens = _options.ShowTokens;
                _options.ShowTokens = false;
                string s = PrettyPrintSig(sig, "");
                _options.ShowTokens = tokens;
                if (s != _sigToDump)
                {
                    _tkMVarOwner = savedMVarOwner;
                    return false;
                }
            }
        }

        if (!dumpBody)
        {
            PrintLine(plainSig);
            _tkMVarOwner = savedMVarOwner;
            return true;
        }

        StringBuilder sb = new();
        sb.Append(Indent).Append(ANCHORPT(KEYWORD(".method"), funcToken)).Append(' ');
        if (_options.ShowTokens)
        {
            sb.Append(COMMENT($"/*{funcToken:X8}*/ "));
        }

        sb.Append(KeywordPrefix);
        switch (attrs & MethodAttributes.MemberAccessMask)
        {
            case MethodAttributes.Public: sb.Append("public "); break;
            case MethodAttributes.Private: sb.Append("private "); break;
            case MethodAttributes.Family: sb.Append("family "); break;
            case MethodAttributes.Assembly: sb.Append("assembly "); break;
            case MethodAttributes.FamANDAssem: sb.Append("famandassem "); break;
            case MethodAttributes.FamORAssem: sb.Append("famorassem "); break;
            case MethodAttributes.PrivateScope: sb.Append("privatescope "); break;
        }

        int a = (int)attrs;
        if ((a & 0x0080) != 0) sb.Append("hidebysig ");
        if ((a & 0x0100) != 0) sb.Append("newslot ");
        if ((a & 0x0800) != 0) sb.Append("specialname ");
        if ((a & 0x1000) != 0) sb.Append("rtspecialname ");
        if ((a & 0x0010) != 0) sb.Append("static ");
        if ((a & 0x0400) != 0) sb.Append("abstract ");
        if ((a & 0x0200) != 0) sb.Append("strict ");
        if ((a & 0x0040) != 0) sb.Append("virtual ");
        if ((a & 0x0020) != 0) sb.Append("final ");
        if ((a & 0x0008) != 0) sb.Append("unmanagedexp ");
        if ((a & 0x8000) != 0) sb.Append("reqsecobj ");
        sb.Append(KeywordPostfix);

        if ((a & 0x2000) != 0)
        {
            sb.Append(KEYWORD("pinvokeimpl")).Append('(');
            var map = GetPinvokeMap(funcToken);
            if (map is null)
            {
                sb.Append(COMMENT("/* No map */"));
            }
            else
            {
                DumpPinvokeMap(sb, map.Value.Flags, map.Value.Name != memberName ? map.Value.Name : null, map.Value.ModuleRef);
            }

            sb.Append(") ");
        }

        _thisIsInstanceMethod = (a & 0x0010) == 0;
        string buff = IsMdPrivateScope(attrs) ? memberName + "$PST" + X8(funcToken) : memberName;
        buff = DumpGenericPars(funcToken, ProperName(buff), false);

        // Argument names and attributes: names[n] for parameter n+1, names[numArgs] for the return value.
        int numArgs;
        {
            int p = 0;
            uint callConv = UncompressData(sig, ref p);
            if ((callConv & IMAGE_CEE_CS_CALLCONV_GENERIC) != 0)
            {
                UncompressData(sig, ref p);
            }

            numArgs = (int)UncompressData(sig, ref p) + 1;
        }

        ParameterHandleCollection paramHandles = md.GetParameters();
        int ulArgs = Math.Max(paramHandles.Count, numArgs);
        int retParamIx = numArgs - 1;
        ParamDescriptor[]? names = null;
        SigLabel? label = null;
        if (ulArgs != 0)
        {
            names = new ParamDescriptor[ulArgs + 2];
            int j = 0;
            foreach (ParameterHandle ph in paramHandles)
            {
                Parameter prm = _md.GetParameter(ph);
                int seq = prm.SequenceNumber;
                if (seq > ulArgs + 1)
                {
                    PrintError($"// error : parameter {j} has sequence number {seq} (0x{seq:X}), parameter info skipped");
                }
                else
                {
                    int ix = retParamIx;
                    if (seq != 0)
                    {
                        ix = seq - 1;
                        string pname = Str(prm.Name);
                        if (pname.Length != 0)
                        {
                            names[ix].Name = pname;
                        }
                    }

                    names[ix].Attr = (uint)prm.Attributes;
                    names[ix].Tok = Token(ph);
                }

                j++;
            }

            for (j = 0; j < numArgs; j++)
            {
                if (string.IsNullOrEmpty(names[j].Name))
                {
                    names[j].Name = "A_" + (_thisIsInstanceMethod ? j + 1 : j).ToString(CultureInfo.InvariantCulture);
                }
            }

            label = new SigLabel { Names = names, StartIndex = 0 };
        }
        else
        {
            label = new SigLabel { Prefix = "A", StartIndex = _thisIsInstanceMethod ? 1 : 0 };
        }

        string line = PrettyPrintMethodSig(sb, sig, buff, label);
        sb.Clear();
        sb.Append(line).Append(KeywordPrefix);
        int ia = (int)implAttrs;
        switch (ia & 0x3)
        {
            case 1: sb.Append(" native"); break;
            case 0: sb.Append(" cil"); break;
            case 2: sb.Append(" optil"); break;
            case 3: sb.Append(" runtime"); break;
        }

        sb.Append((ia & 0x4) != 0 ? " unmanaged" : " managed");
        if ((ia & 0x0080) != 0) sb.Append(" preservesig");
        if ((ia & 0x0010) != 0) sb.Append(" forwardref");
        if ((ia & 0x1000) != 0) sb.Append(" internalcall");
        if ((ia & 0x0020) != 0) sb.Append(" synchronized");
        if ((ia & 0x0008) != 0) sb.Append(" noinlining");
        if ((ia & 0x0100) != 0) sb.Append(" aggressiveinlining");
        if ((ia & 0x0040) != 0) sb.Append(" nooptimization");
        if ((ia & 0x0200) != 0) sb.Append(" aggressiveoptimization");
        if ((ia & 0x2000) != 0) sb.Append(" async");
        sb.Append(KeywordPostfix);
        PrintLine(sb);

        if (_options.ShowBytes)
        {
            string szt = "SIG:";
            for (int i = 0; i < sig.Length;)
            {
                StringBuilder hex = new();
                hex.Append(Indent).Append("// ").Append(szt);
                while (i < sig.Length)
                {
                    hex.Append(' ').Append(sig[i].ToString("X2", CultureInfo.InvariantCulture));
                    i++;
                    if ((i & 0x1F) == 0)
                    {
                        break;
                    }
                }

                PrintLine(COMMENT(hex.ToString()));
                szt = "    ";
            }
        }

        PrintLine(Indent + SCOPE());
        PushIndent();

        if (funcToken == entryPointToken)
        {
            PrintLine(Indent + KEYWORD(".entrypoint"));
        }

        DumpCustomAttributes(funcToken);
        DumpGenericParsCA(funcToken);
        DumpParams(names, retParamIx);
        DumpPermissions(funcToken);

        foreach (VTableRef vt in _vtableRefs)
        {
            if (vt.Tok == funcToken)
            {
                PrintLine($"{Indent}{KEYWORD(".vtentry")} {vt.Entry + 1} : {vt.Slot + 1}");
                break;
            }
        }

        for (int j = 0; j < _eatRefs.Count; j++)
        {
            if (_eatRefs[j].Tok == funcToken)
            {
                PrintLine($"{Indent}{KEYWORD(".export")} [{j + _eatBase}] {KEYWORD("as")} {ProperName(_eatRefs[j].Name ?? "")}");
                break;
            }
        }

        foreach (MethodImplRecord mi in _methodImpls)
        {
            if (mi.TkBody == funcToken)
            {
                PrintLine(PrettyPrintOverrideDecl(mi, funcToken, out _));
            }
        }

        bool done = false;
        if ((a & 0x2000) != 0)
        {
            if (rva != 0)
            {
                PrintLine(COMMENT($"{Indent}// Embedded native code"));
                DumpNativeMethodComment(rva);
            }
            else
            {
                PopIndent();
                PrintLine(Indent + UNSCOPE());
                _tkMVarOwner = savedMVarOwner;
                return true;
            }

            done = true;
        }

        if (!done)
        {
            if ((ia & 0x4) == 0)
            {
                if ((ia & 0x3) == 0 || (ia & 0x3) == 2)
                {
                    if (_options.ShowBytes)
                    {
                        PrintLine(COMMENT($"{Indent}// Method begins at RVA 0x{rva:x}"));
                    }

                    if (rva != 0)
                    {
                        DisassembleWrapper(rva, funcToken, names, ulArgs);
                    }
                }
                else if ((ia & 0x3) == 1)
                {
                    DumpNativeMethodComment(rva);
                }
            }
            else if ((ia & 0x3) == 1)
            {
                PrintLine(COMMENT($"{Indent}//  Unmanaged TargetRVA = 0x{rva:X8}"));
            }
            else if ((ia & 0x3) == 3)
            {
                PrintLine(COMMENT($"{Indent}// Method provided by Runtime"));
            }
        }

        PopIndent();
        sb.Clear();
        sb.Append(Indent).Append(UNSCOPE()).Append(' ');
        if (className is not null)
        {
            sb.Append(COMMENT("// end of method " + ProperName(className) + "::"));
            sb.Append(COMMENT(ProperName(memberName)));
        }
        else
        {
            sb.Append(COMMENT("// end of global method " + ProperName(memberName)));
        }

        PrintLine(sb);
        PrintLine("");
        _tkMVarOwner = savedMVarOwner;
        return true;
    }

    private void DumpNativeMethodComment(uint rva)
    {
        PrintLine(COMMENT($"{Indent}// Disassembly of native methods is not supported."));
        PrintLine(COMMENT($"{Indent}//  Managed TargetRVA = 0x{rva:X8}"));
    }

    private void DisassembleWrapper(uint rva, int funcToken, ParamDescriptor[]? names, int ulArgs)
    {
        MethodBody? body = DecodeMethodBody(rva);
        if (body is null)
        {
            PrintError($"INVALID METHOD ADDRESS: 0x{0:X8} (RVA: 0x{rva:X8})");
            return;
        }

        try
        {
            Disassemble(body, funcToken, names, ulArgs);
        }
        catch (Exception e) when (e is IndexOutOfRangeException or ArgumentOutOfRangeException or ArgumentException or BadImageFormatException)
        {
            PrintLine($"{Indent} ********* ERROR DISASSEMBLING THE METHOD IL CODE ***********");
        }
    }

    private void DumpPinvokeMap(StringBuilder sb, MethodImportAttributes flags, string? importName, int moduleRef)
    {
        string dll = RidFromToken(moduleRef) != 0 && IsValidToken(moduleRef)
            ? Str(_md.GetModuleReference((ModuleReferenceHandle)Handle(moduleRef)).Name)
            : "";
        if (dll.Length != 0)
        {
            DumpQString(sb, dll, Indent, 80);
        }

        if (!string.IsNullOrEmpty(importName))
        {
            sb.Append(KEYWORD(" as "));
            DumpQString(sb, importName, Indent, 80);
        }

        int f = (int)flags;
        sb.Append(KeywordPrefix);
        if ((f & 0x0001) != 0) sb.Append(" nomangle");
        switch (f & 0x0006)
        {
            case 0x2: sb.Append(" ansi"); break;
            case 0x4: sb.Append(" unicode"); break;
            case 0x6: sb.Append(" autochar"); break;
        }

        if ((f & 0x0040) != 0) sb.Append(" lasterr");
        switch (f & 0x0700)
        {
            case 0x100: sb.Append(" winapi"); break;
            case 0x200: sb.Append(" cdecl"); break;
            case 0x400: sb.Append(" thiscall"); break;
            case 0x500: sb.Append(" fastcall"); break;
            case 0x300: sb.Append(" stdcall"); break;
        }

        switch (f & 0x0030)
        {
            case 0x10: sb.Append(" bestfit:on"); break;
            case 0x20: sb.Append(" bestfit:off"); break;
        }

        switch (f & 0x3000)
        {
            case 0x1000: sb.Append(" charmaperror:on"); break;
            case 0x2000: sb.Append(" charmaperror:off"); break;
        }

        sb.Append(KeywordPostfix);
    }

    // ---------------------------------------------------------------- fields

    private bool DumpField(int tk, string? className, bool dumpBody)
    {
        string psz = MakeNameIfNone(GetNameOfFieldDef(tk), tk);
        FieldAttributes attrs = GetFieldDefAttrs(tk);
        if (IsHiddenField(attrs))
        {
            return false;
        }

        string memberName = ProperName(IsFdPrivateScope(attrs) ? psz + "$PST" + X8(tk) : psz);
        byte[] sig = GetSigOfFieldDef(tk);
        if (sig.Length == 0)
        {
            PrintError($"{Indent}ERROR: field '{memberName}' has no signature");
            return false;
        }

        string szStr = PrettyPrintSig(sig, dumpBody ? memberName : "");

        if (_mode is DumpMode.ClassMethod or DumpMode.ClassMethodSig)
        {
            if (memberName != _methodToDump)
            {
                return false;
            }

            if (_mode == DumpMode.ClassMethodSig)
            {
                bool tokens = _options.ShowTokens;
                _options.ShowTokens = false;
                string s = PrettyPrintSig(sig, "");
                _options.ShowTokens = tokens;
                if (s != _sigToDump)
                {
                    return false;
                }
            }
        }

        StringBuilder sb = new();
        if (dumpBody)
        {
            sb.Append(Indent).Append(ANCHORPT(KEYWORD(".field"), tk)).Append(' ');
            if (_options.ShowTokens)
            {
                sb.Append(COMMENT($"/*{tk:X8}*/ "));
            }
        }

        if (_fieldOffsets.TryGetValue(tk, out uint offset) && offset != 0xFFFFFFFF)
        {
            sb.Append('[').Append(((int)offset).ToString(CultureInfo.InvariantCulture)).Append("] ");
        }

        int a = (int)attrs;
        sb.Append(KeywordPrefix);
        switch (attrs & FieldAttributes.FieldAccessMask)
        {
            case FieldAttributes.Public: sb.Append("public "); break;
            case FieldAttributes.Private: sb.Append("private "); break;
        }

        if ((a & 0x0010) != 0) sb.Append("static ");
        switch (attrs & FieldAttributes.FieldAccessMask)
        {
            case FieldAttributes.Family: sb.Append("family "); break;
            case FieldAttributes.Assembly: sb.Append("assembly "); break;
            case FieldAttributes.FamANDAssem: sb.Append("famandassem "); break;
            case FieldAttributes.FamORAssem: sb.Append("famorassem "); break;
            case FieldAttributes.PrivateScope: sb.Append("privatescope "); break;
        }

        if ((a & 0x0020) != 0) sb.Append("initonly ");
        if ((a & 0x0040) != 0) sb.Append("literal ");
        if ((a & 0x0080) != 0) sb.Append("notserialized ");
        if ((a & 0x0200) != 0) sb.Append("specialname ");
        if ((a & 0x0400) != 0) sb.Append("rtspecialname ");
        sb.Append(KeywordPostfix);

        if ((a & 0x2000) != 0)
        {
            sb.Append(KEYWORD("pinvokeimpl")).Append('(');
            var map = GetPinvokeMap(tk);
            if (map is null)
            {
                sb.Append(COMMENT("/* No map */"));
            }
            else
            {
                DumpPinvokeMap(sb, map.Value.Flags, map.Value.Name != psz ? map.Value.Name : null, map.Value.ModuleRef);
            }

            sb.Append(") ");
        }

        sb.Append(DumpMarshaling(tk));
        sb.Append(szStr);

        if ((a & 0x0100) != 0)
        {
            sb.Append(KEYWORD(" at "));
            uint fieldRva = GetFieldRva(tk);
            DumpDataPtr(sb, fieldRva, SizeOfField(tk));
        }

        if ((a & 0x8000) != 0 && dumpBody)
        {
            DumpDefaultValue(tk, sb);
        }

        PrintLine(sb);
        if (dumpBody)
        {
            DumpCustomAttributes(tk);
            DumpPermissions(tk);
        }

        return true;
    }

    // ---------------------------------------------------------------- events and properties

    private List<(int Method, uint Semantics)> GetAssociates(int tk)
    {
        List<(int, uint)> list = new();
        int rows = RowCount(TableIndex.MethodSemantics);
        if (rows == 0)
        {
            return list;
        }

        // MethodSemantics: Semantics (2), Method (MethodDef index), Association (HasSemantics coded index)
        int rowSize = _md.GetTableRowSize(TableIndex.MethodSemantics);
        int methodSize = RowCount(TableIndex.MethodDef) > ushort.MaxValue ? 4 : 2;
        int assocSize = CodedIndexSize(1, TableIndex.Event, TableIndex.Property);
        int baseOffset = _md.GetTableMetadataOffset(TableIndex.MethodSemantics);
        uint target = (uint)(RidFromToken(tk) << 1 | (TypeFromToken(tk) == mdtProperty ? 1 : 0));
        BlobReader r = _metadataBlock.GetReader();
        for (int i = 0; i < rows; i++)
        {
            r.Offset = baseOffset + i * rowSize;
            ushort sem = r.ReadUInt16();
            uint method = methodSize == 2 ? r.ReadUInt16() : r.ReadUInt32();
            uint assoc = assocSize == 2 ? r.ReadUInt16() : r.ReadUInt32();
            if (assoc == target)
            {
                list.Add((mdtMethodDef | (int)method, sem));
            }
        }

        return list;
    }

    private bool AllAssociatesHidden(List<(int Method, uint Semantics)> assoc)
    {
        if (!_limitedVisibility || assoc.Count == 0)
        {
            return false;
        }

        foreach ((int m, _) in assoc)
        {
            if (TypeFromToken(m) == mdtMethodDef && IsValidToken(m) && !IsHiddenMethod(GetMethodDefAttrs(m)))
            {
                return false;
            }
        }

        return true;
    }

    private bool DumpEvent(int tk, string className, bool dumpBody)
    {
        EventDefinition ev = _md.GetEventDefinition((EventDefinitionHandle)Handle(tk));
        string psz = MakeNameIfNone(Str(ev.Name), tk);
        int attrs = (int)ev.Attributes;
        int tkEventType = Token(ev.Type);
        if (_mode is DumpMode.ClassMethod or DumpMode.ClassMethodSig && psz != _methodToDump)
        {
            return false;
        }

        List<(int Method, uint Semantics)> assoc = GetAssociates(tk);
        if (AllAssociatesHidden(assoc))
        {
            return false;
        }

        StringBuilder sb = new();
        if (dumpBody)
        {
            sb.Append(Indent).Append(KEYWORD(".event")).Append(' ');
            if (_options.ShowTokens)
            {
                sb.Append(COMMENT($"/*{tk:X8}*/ "));
            }
        }
        else
        {
            sb.Append(ProperName(psz)).Append(" : ");
        }

        if ((attrs & 0x0200) != 0)
        {
            sb.Append(KEYWORD("specialname "));
        }

        if ((attrs & 0x0400) != 0)
        {
            sb.Append(KEYWORD("rtspecialname "));
        }

        if (RidFromToken(tkEventType) != 0 && IsValidToken(tkEventType))
        {
            switch (TypeFromToken(tkEventType))
            {
                case mdtTypeRef:
                case mdtTypeDef:
                case mdtTypeSpec:
                    PrettyPrintToken(sb, tkEventType);
                    break;
            }
        }

        if (!dumpBody)
        {
            PrintLine(sb);
            return true;
        }

        sb.Append(' ').Append(ProperName(psz));
        PrintLine(sb);
        PrintLine(Indent + SCOPE());
        PushIndent();
        DumpCustomAttributes(tk);
        DumpPermissions(tk);
        foreach ((int m, uint sem) in assoc)
        {
            sb.Clear();
            if ((sem & 0x0008) != 0) sb.Append(Indent).Append(KEYWORD(".addon")).Append(' ');
            else if ((sem & 0x0010) != 0) sb.Append(Indent).Append(KEYWORD(".removeon")).Append(' ');
            else if ((sem & 0x0020) != 0) sb.Append(Indent).Append(KEYWORD(".fire")).Append(' ');
            else if ((sem & 0x0004) != 0) sb.Append(Indent).Append(KEYWORD(".other")).Append(' ');
            else sb.Append(ERRORMSG($"UNKNOWN SEMANTICS: 0x{sem:X} "));

            if (IsValidToken(m))
            {
                PrettyPrintToken(sb, m);
            }
            else
            {
                sb.Append(ERRORMSG($"INVALID TOKEN 0x{m:X8}"));
            }

            PrintLine(sb);
        }

        PopIndent();
        sb.Clear();
        sb.Append(Indent).Append(UNSCOPE()).Append(' ');
        sb.Append(COMMENT("// end of event " + ProperName(className) + "::"));
        sb.Append(COMMENT(ProperName(psz)));
        PrintLine(sb);
        return true;
    }

    private bool DumpProp(int tk, string className, bool dumpBody)
    {
        PropertyDefinition pr = _md.GetPropertyDefinition((PropertyDefinitionHandle)Handle(tk));
        string psz = MakeNameIfNone(Str(pr.Name), tk);
        int attrs = (int)pr.Attributes;
        byte[] sig = Blob(pr.Signature);
        if (sig.Length == 0)
        {
            PrintError($"{Indent}ERROR: property '{psz}' has no signature");
            return false;
        }

        if (_mode is DumpMode.ClassMethod or DumpMode.ClassMethodSig && psz != _methodToDump)
        {
            return false;
        }

        List<(int Method, uint Semantics)> assoc = GetAssociates(tk);
        if (AllAssociatesHidden(assoc))
        {
            return false;
        }

        StringBuilder sb = new();
        if (dumpBody)
        {
            sb.Append(Indent).Append(KEYWORD(".property")).Append(' ');
            if (_options.ShowTokens)
            {
                sb.Append(COMMENT($"/*{tk:X8}*/ "));
            }
        }
        else
        {
            sb.Append(ProperName(psz)).Append(" : ");
        }

        if ((attrs & 0x0200) != 0)
        {
            sb.Append(KEYWORD("specialname "));
        }

        if ((attrs & 0x0400) != 0)
        {
            sb.Append(KEYWORD("rtspecialname "));
        }

        string line = PrettyPrintMethodSig(sb, sig, dumpBody ? ProperName(psz) : "", null);
        sb.Clear();
        sb.Append(line);
        if ((attrs & 0x1000) != 0 && dumpBody)
        {
            DumpDefaultValue(tk, sb);
        }

        PrintLine(sb);
        if (dumpBody)
        {
            PrintLine(Indent + SCOPE());
            PushIndent();
            DumpCustomAttributes(tk);
            DumpPermissions(tk);
            foreach ((int m, uint sem) in assoc)
            {
                sb.Clear();
                if ((sem & 0x0001) != 0) sb.Append(Indent).Append(KEYWORD(".set")).Append(' ');
                else if ((sem & 0x0002) != 0) sb.Append(Indent).Append(KEYWORD(".get")).Append(' ');
                else if ((sem & 0x0004) != 0) sb.Append(Indent).Append(KEYWORD(".other")).Append(' ');
                else sb.Append(ERRORMSG($"UNKNOWN SEMANTICS: 0x{sem:X} "));

                if (IsValidToken(m))
                {
                    PrettyPrintToken(sb, m);
                }
                else
                {
                    sb.Append(ERRORMSG($"INVALID TOKEN 0x{m:X8}"));
                }

                PrintLine(sb);
            }

            PopIndent();
            sb.Clear();
            sb.Append(Indent).Append(UNSCOPE()).Append(' ');
            sb.Append(COMMENT("// end of property " + ProperName(className) + "::"));
            sb.Append(COMMENT(ProperName(psz)));
            PrintLine(sb);
        }

        return true;
    }

    // ---------------------------------------------------------------- classes

    private void DumpMembers(int cl, string className, int entryPointToken)
    {
        TypeDefinition td = _md.GetTypeDefinition((TypeDefinitionHandle)Handle(cl));
        List<int> members = new();
        foreach (FieldDefinitionHandle h in td.GetFields())
        {
            members.Add(Token(h));
        }

        foreach (MethodDefinitionHandle h in td.GetMethods())
        {
            members.Add(Token(h));
        }

        foreach (EventDefinitionHandle h in td.GetEvents())
        {
            members.Add(Token(h));
        }

        foreach (PropertyDefinitionHandle h in td.GetProperties())
        {
            members.Add(Token(h));
        }

        foreach (int tk in members)
        {
            bool ret;
            if (IsValidToken(tk))
            {
                ret = TypeFromToken(tk) switch
                {
                    mdtFieldDef => DumpField(tk, className, true),
                    mdtMethodDef => DumpMethod(tk, className, entryPointToken, true),
                    mdtEvent => DumpEvent(tk, className, true),
                    mdtProperty => DumpProp(tk, className, true),
                    _ => false,
                };
            }
            else
            {
                PrintLine(ERRORMSG($"INVALID MEMBER TOKEN: 0x{tk:X8}"));
                ret = false;
            }

            if (ret && _mode == DumpMode.ClassMethodSig)
            {
                break;
            }
        }
    }

    private bool GetClassLayout(int cl, out uint packSize, out uint classSize)
    {
        _fieldOffsets.Clear();
        packSize = 0xFFFFFFFF;
        classSize = 0xFFFFFFFF;
        if (RidFromToken(cl) == 0)
        {
            return true;
        }

        bool ret = false;
        TypeDefinition td = _md.GetTypeDefinition((TypeDefinitionHandle)Handle(cl));
        TypeLayout layout = td.GetLayout();
        if (HasClassLayoutRow(cl))
        {
            packSize = (uint)layout.PackingSize;
            classSize = (uint)layout.Size;
            ret = true;
        }

        foreach (FieldDefinitionHandle fh in td.GetFields())
        {
            int off = _md.GetFieldDefinition(fh).GetOffset();
            if (off != -1)
            {
                _fieldOffsets[Token(fh)] = (uint)off;
                ret = true;
            }
        }

        return ret;
    }

    private bool HasClassLayoutRow(int cl)
    {
        int rows = RowCount(TableIndex.ClassLayout);
        if (rows == 0)
        {
            return false;
        }

        int rowSize = _md.GetTableRowSize(TableIndex.ClassLayout);
        int typeDefSize = RowCount(TableIndex.TypeDef) > ushort.MaxValue ? 4 : 2;
        int baseOffset = _md.GetTableMetadataOffset(TableIndex.ClassLayout);
        BlobReader r = _metadataBlock.GetReader();
        for (int i = 0; i < rows; i++)
        {
            r.Offset = baseOffset + i * rowSize + 6;
            uint parent = typeDefSize == 2 ? r.ReadUInt16() : r.ReadUInt32();
            if (parent == RidFromToken(cl))
            {
                return true;
            }
        }

        return false;
    }

    private bool IsANestedInB(int a, int b)
    {
        while (true)
        {
            int ix = _classList.IndexOf(a);
            if (ix < 0)
            {
                return false;
            }

            a = _classEnclosing[ix];
            if (a == b)
            {
                return true;
            }

            if (a == mdTypeDefNil)
            {
                return false;
            }
        }
    }

    private int TopEncloser(int a)
    {
        while (true)
        {
            int ix = _classList.IndexOf(a);
            if (ix < 0 || _classEnclosing[ix] == mdTypeDefNil)
            {
                return a;
            }

            a = _classEnclosing[ix];
        }
    }

    // whatToDump: 0 - title, flags, extends, implements; +1 - pack, size and custom attributes; +2 - nested classes; +4 - members
    private bool DumpClass(int cl, int entryPointToken, int whatToDump)
    {
        int savedVarOwner = _tkVarOwner;
        int whatToDumpOrig = whatToDump;
        (string pc1, string pc2) = GetNameOfTypeDef(cl);
        pc1 = MakeNameIfNone(pc1, cl);

        if (_mode is DumpMode.Class or DumpMode.ClassMethod or DumpMode.ClassMethodSig)
        {
            if (cl != _classToDump)
            {
                if (IsANestedInB(_classToDump, cl))
                {
                    whatToDump = 2;
                }
                else
                {
                    return true;
                }
            }
        }

        (TypeAttributes attrs, int crExtends) = GetTypeDefProps(cl);
        int a = (int)attrs;
        if (_limitedVisibility)
        {
            int vis = a & 0x7;
            if (_hidePub && (vis == 1 || vis == 2)) return false;
            if (_hidePriv && (vis == 0 || vis == 3)) return false;
            if (_hideFam && vis == 4) return false;
            if (_hideAsm && vis == 5) return false;
            if (_hideFOA && vis == 7) return false;
            if (_hideFAA && vis == 6) return false;
        }

        _tkVarOwner = cl;
        string className = pc1;
        string ns = pc2;

        StringBuilder sb = new();
        sb.Append(Indent).Append(KEYWORD(".class")).Append(' ');
        if (_options.ShowTokens)
        {
            sb.Append(COMMENT($"/*{cl:X8}*/ "));
        }

        sb.Append(KeywordPrefix);
        if ((a & 0x20) != 0) sb.Append("interface ");
        switch (a & 0x7)
        {
            case 1: sb.Append("public "); break;
            case 0: sb.Append("private "); break;
        }

        if ((a & 0x80) != 0) sb.Append("abstract ");
        switch (a & 0x18)
        {
            case 0x00: sb.Append("auto "); break;
            case 0x08: sb.Append("sequential "); break;
            case 0x10: sb.Append("explicit "); break;
            case 0x18: sb.Append("extended "); break;
        }

        switch (a & 0x30000)
        {
            case 0x00000: sb.Append("ansi "); break;
            case 0x10000: sb.Append("unicode "); break;
            case 0x20000: sb.Append("autochar "); break;
        }

        if ((a & 0x1000) != 0) sb.Append("import ");
        if ((a & 0x4000) != 0) sb.Append("windowsruntime ");
        if ((a & 0x2000) != 0) sb.Append("serializable ");
        if ((a & 0x100) != 0) sb.Append("sealed ");
        switch (a & 0x7)
        {
            case 2: sb.Append("nested public "); break;
            case 3: sb.Append("nested private "); break;
            case 4: sb.Append("nested family "); break;
            case 5: sb.Append("nested assembly "); break;
            case 6: sb.Append("nested famandassem "); break;
            case 7: sb.Append("nested famorassem "); break;
        }

        if ((a & 0x100000) != 0) sb.Append("beforefieldinit ");
        if ((a & 0x400) != 0) sb.Append("specialname ");
        if ((a & 0x800) != 0) sb.Append("rtspecialname ");
        sb.Append(KeywordPostfix);
        if (ns.Length != 0)
        {
            sb.Append(ProperName(ns)).Append('.');
        }

        sb.Append(whatToDump > 2 ? ANCHORPT(ProperName(className), cl) : JUMPPT(ProperName(className), cl));
        string header = DumpGenericPars(cl, sb.ToString(), split: true);
        PrintLine(header);

        if (!IsNilToken(crExtends))
        {
            sb.Clear();
            sb.Append(Indent).Append("       ").Append(KEYWORD("extends")).Append(' ');
            if (IsValidToken(crExtends))
            {
                PrettyPrintToken(sb, crExtends);
            }
            else
            {
                sb.Append(ERRORMSG($"INVALID TOKEN: 0x{crExtends:X8}"));
            }

            PrintLine(sb);
        }

        TypeDefinition td = _md.GetTypeDefinition((TypeDefinitionHandle)Handle(cl));
        InterfaceImplementationHandleCollection iis = td.GetInterfaceImplementations();
        int numInterfaces = iis.Count;
        int i = 0;
        foreach (InterfaceImplementationHandle iih in iis)
        {
            sb.Clear();
            if (i != 0)
            {
                sb.Append(Indent).Append("                  ");
            }
            else
            {
                sb.Append(Indent).Append("       ").Append(KEYWORD("implements")).Append(' ');
            }

            int crInterface = Token(_md.GetInterfaceImplementation(iih).Interface);
            if (IsValidToken(crInterface))
            {
                PrettyPrintToken(sb, crInterface);
            }
            else
            {
                sb.Append(ERRORMSG($"INVALID TOKEN: 0x{crInterface:X8}"));
            }

            if (i < numInterfaces - 1)
            {
                sb.Append(',');
            }

            PrintLine(sb);
            i++;
        }

        if (whatToDump == 0)
        {
            PrintLine($"{Indent}{SCOPE()} {UNSCOPE()}");
            _tkVarOwner = savedVarOwner;
            return true;
        }

        PrintLine(Indent + SCOPE());
        PushIndent();

        if ((whatToDump & 1) != 0)
        {
            if (GetClassLayout(cl, out uint packSize, out uint classSize))
            {
                if (packSize != 0xFFFFFFFF)
                {
                    PrintLine($"{Indent}{KEYWORD(".pack")} {(int)packSize}");
                }

                if (classSize != 0xFFFFFFFF)
                {
                    PrintLine($"{Indent}{KEYWORD(".size")} {(int)classSize}");
                }
            }

            DumpCustomAttributes(cl);
            if (numInterfaces > 0 && ShowCustomAttributes)
            {
                foreach (InterfaceImplementationHandle iih in iis)
                {
                    bool first = true;
                    foreach (CustomAttributeHandle cah in _md.GetCustomAttributes(iih))
                    {
                        if (first)
                        {
                            sb.Clear();
                            sb.Append(Indent).Append('.').Append(KEYWORD("interfaceimpl type")).Append(' ');
                            int crInterface = Token(_md.GetInterfaceImplementation(iih).Interface);
                            if (IsValidToken(crInterface))
                            {
                                PrettyPrintToken(sb, crInterface);
                            }
                            else
                            {
                                sb.Append(ERRORMSG($"INVALID TOKEN: 0x{crInterface:X8}"));
                            }

                            PrintLine(sb);
                            first = false;
                        }

                        DumpCustomAttribute(Token(cah), withOwner: false);
                    }
                }
            }

            DumpGenericParsCA(cl);
            DumpPermissions(cl);

            foreach (MethodImplRecord mi in _methodImpls)
            {
                if (mi.TkClass == cl && mi.TkBodyParent != cl)
                {
                    sb.Clear();
                    sb.Append(PrettyPrintOverrideDecl(mi, cl, out bool overridingTypeSpec));
                    sb.Append(KEYWORD(" with "));
                    if (overridingTypeSpec)
                    {
                        sb.Append(KEYWORD("method "));
                    }

                    PrettyPrintToken(sb, mi.TkBody);
                    PrintLine(sb);
                }
            }
        }
        else
        {
            _fieldOffsets.Clear();
        }

        if ((whatToDump & 2) != 0)
        {
            bool regetLayout = false;
            DumpMode mode = _mode;
            if (_mode == DumpMode.Class)
            {
                _mode = DumpMode.All;
            }

            for (int k = 0; k < _classList.Count; k++)
            {
                if (_classEnclosing[k] == cl)
                {
                    DumpClass(_classList[k], entryPointToken, whatToDumpOrig);
                    regetLayout = true;
                }
            }

            if (regetLayout)
            {
                GetClassLayout(cl, out _, out _);
            }

            _mode = mode;
        }

        if ((whatToDump & 4) != 0)
        {
            DumpMembers(cl, className, entryPointToken);
        }

        PopIndent();
        sb.Clear();
        sb.Append(Indent).Append(UNSCOPE()).Append(' ').Append(CommentPrefix).Append("// end of class ");
        if (ns.Length != 0)
        {
            sb.Append(ProperName(ns)).Append('.');
        }

        sb.Append(ProperName(className)).Append(CommentPostfix);
        PrintLine(sb);
        PrintLine("");
        _tkVarOwner = savedVarOwner;
        return true;
    }

    private void DumpGlobalMethods(int entryPointToken)
    {
        int i = 0;
        foreach (MethodDefinitionHandle h in GlobalType().GetMethods())
        {
            if (i == 0)
            {
                PrintLine("");
                PrintLine(COMMENT("// ================== GLOBAL METHODS ========================="));
                PrintLine("");
            }

            i++;
            if (DumpMethod(Token(h), null, entryPointToken, true) && _mode is DumpMode.ClassMethod or DumpMode.ClassMethodSig)
            {
                break;
            }
        }

        if (i != 0)
        {
            PrintLine("");
            PrintLine(COMMENT("// ============================================================="));
            PrintLine("");
        }
    }

    private void DumpGlobalFields()
    {
        int i = 0;
        foreach (FieldDefinitionHandle h in GlobalType().GetFields())
        {
            if (i == 0)
            {
                PrintLine("");
                PrintLine(COMMENT("// ================== GLOBAL FIELDS =========================="));
                PrintLine("");
            }

            i++;
            if (DumpField(Token(h), null, true) && _mode is DumpMode.ClassMethod or DumpMode.ClassMethodSig)
            {
                break;
            }
        }

        if (i != 0)
        {
            PrintLine("");
            PrintLine(COMMENT("// ============================================================="));
            PrintLine("");
        }
    }

    private TypeDefinition GlobalType()
        => RowCount(TableIndex.TypeDef) > 0 ? _md.GetTypeDefinition(MetadataTokens.TypeDefinitionHandle(1)) : default;
}
