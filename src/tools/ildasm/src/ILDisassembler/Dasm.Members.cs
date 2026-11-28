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

    private void DumpCustomAttributes(EntityHandle owner)
    {
        if (!ShowCustomAttributes)
        {
            return;
        }

        foreach (CustomAttributeHandle h in GetCustomAttributes(owner))
        {
            DumpCustomAttribute(h, withOwner: false);
        }
    }

    private void DumpCustomAttribute(CustomAttributeHandle h, bool withOwner)
    {
        CustomAttribute ca = _md.GetCustomAttribute(h);
        if (ca.Parent.IsNil)
        {
            return;
        }

        DumpCustomAttributeProps(h, ca.Constructor, ca.Parent, Blob(ca.Value), withOwner);
        _caDumped.Add(h);
    }

    private void DumpCustomAttributeProps(CustomAttributeHandle ca, EntityHandle ctor, EntityHandle owner, byte[] blob, bool withOwner)
    {
        bool commentItOut = false;
        if (ctor.Kind is HandleKind.MemberReference or HandleKind.MethodDefinition)
        {
            EntityHandle parent = GetParent(ctor);
            if (parent.Kind is HandleKind.TypeDefinition or HandleKind.TypeReference && IsValid(parent))
            {
                (string className, string ns) = GetNameOfType(parent);
                commentItOut = ns == "System.Diagnostics" && className == "DebuggableAttribute";
            }
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
                sb.Append(COMMENT($"/*{Token(ca):X8}*/ "));
            }

            sb.Append('(');
            switch (owner.Kind)
            {
                case HandleKind.TypeDefinition:
                case HandleKind.TypeReference:
                case HandleKind.TypeSpecification:
                    PrettyPrintToken(sb, owner);
                    break;
                case HandleKind.MemberReference:
                    sb.Append(KEYWORD(IsFieldMemberRef((MemberReferenceHandle)owner) ? "field " : "method "));
                    PrettyPrintToken(sb, owner);
                    break;
                case HandleKind.MethodDefinition:
                    sb.Append(KEYWORD("method "));
                    PrettyPrintToken(sb, owner);
                    break;
                default:
                    sb.Append(ERRORMSG("UNKNOWN_OWNER"));
                    break;
            }

            if (_options.ShowTokens)
            {
                sb.Append(COMMENT($"/*{Token(owner):X8}*/ "));
            }

            sb.Append(") ");
        }
        else if (_options.ShowTokens)
        {
            sb.Append(COMMENT($"/*{Token(ca):X8}:{Token(ctor):X8}*/ "));
        }

        switch (ctor.Kind)
        {
            case HandleKind.TypeDefinition:
            case HandleKind.TypeReference:
            case HandleKind.MemberReference:
            case HandleKind.MethodDefinition:
                PrettyPrintToken(sb, ctor);
                break;
            default:
                sb.Append(ERRORMSG("UNNAMED_CUSTOM_ATTR"));
                break;
        }

        if (blob.Length != 0)
        {
            _customAttributeDisassembler.AppendValue(this, sb, ctor, blob);
        }

        PrintLine(sb);
        if (commentItOut)
        {
            PopIndent(4);
            PrintLine(CommentPostfix);
        }
    }

    // ---------------------------------------------------------------- default values

    private void DumpDefaultValue(EntityHandle parent, StringBuilder sb)
    {
        ConstantHandle ch = GetConstant(parent);
        if (ch.IsNil)
        {
            sb.Append(" /* NO CORRESPONDING RECORD IN CONSTANTS TABLE */");
            return;
        }

        Constant c = _md.GetConstant(ch);
        byte[] v = Blob(c.Value);
        CultureInfo inv = CultureInfo.InvariantCulture;
        switch (c.TypeCode)
        {
            case ConstantTypeCode.SByte:
                sb.Append(" = ").Append(KEYWORD("int8")).Append("(0x").Append(ValueOrZero(v, 1).ToString("X2", inv)).Append(')');
                break;
            case ConstantTypeCode.Byte:
                sb.Append(" = ").Append(KEYWORD("uint8")).Append("(0x").Append(ValueOrZero(v, 1).ToString("X2", inv)).Append(')');
                break;
            case ConstantTypeCode.Int16:
                sb.Append(" = ").Append(KEYWORD("int16")).Append("(0x").Append(ValueOrZero(v, 2).ToString("X4", inv)).Append(')');
                break;
            case ConstantTypeCode.UInt16:
                sb.Append(" = ").Append(KEYWORD("uint16")).Append("(0x").Append(ValueOrZero(v, 2).ToString("X4", inv)).Append(')');
                break;
            case ConstantTypeCode.Int32:
                sb.Append(" = ").Append(KEYWORD("int32")).Append("(0x").Append(ValueOrZero(v, 4).ToString("X8", inv)).Append(')');
                break;
            case ConstantTypeCode.UInt32:
                sb.Append(" = ").Append(KEYWORD("uint32")).Append("(0x").Append(ValueOrZero(v, 4).ToString("X8", inv)).Append(')');
                break;
            case ConstantTypeCode.Char:
                sb.Append(" = ").Append(KEYWORD("char")).Append("(0x").Append(ValueOrZero(v, 2).ToString("X4", inv)).Append(')');
                break;
            case ConstantTypeCode.Boolean:
                sb.Append(" = ").Append(KEYWORD("bool")).Append('(').Append(KEYWORD(ValueOrZero(v, 1) != 0 ? "true" : "false")).Append(')');
                break;
            case ConstantTypeCode.Int64:
                sb.Append(" = ").Append(KEYWORD("int64")).Append("(0x").Append(ValueOrZero(v, 8).ToString("X", inv)).Append(')');
                break;
            case ConstantTypeCode.UInt64:
                sb.Append(" = ").Append(KEYWORD("uint64")).Append("(0x").Append(ValueOrZero(v, 8).ToString("X", inv)).Append(')');
                break;
            case ConstantTypeCode.Single:
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

            case ConstantTypeCode.Double:
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

            case ConstantTypeCode.String:
                sb.Append(" = ");
                DumpUnicodeString(sb, v);
                break;

            case ConstantTypeCode.NullReference when v.Length == 0 || IsAllZero(v):
                sb.Append(" = ").Append(KEYWORD("nullref"));
                break;

            default:
                sb.Append(ERRORMSG($" /* ILLEGAL CONSTANT type:0x{(byte)c.TypeCode:X2}, size:{v.Length} bytes, blob: "));
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
            ParameterHandle param = pd[j].Param;
            if (param.IsNil)
            {
                continue;
            }

            ImmutableArray<CustomAttributeHandle> cas = ShowCustomAttributes ? GetCustomAttributes(param) : ImmutableArray<CustomAttributeHandle>.Empty;
            int ulCAs = ShowCustomAttributes ? cas.Length : 0;
            bool hasDefault = (pd[j].Attr & ParameterAttributes.HasDefault) != 0;
            if (ulCAs != 0 || hasDefault)
            {
                StringBuilder sb = new();
                sb.Append(Indent).Append(KEYWORD(".param")).Append(" [").Append((i - ulParams).ToString(CultureInfo.InvariantCulture)).Append(']');
                if (_options.ShowTokens)
                {
                    sb.Append(COMMENT($"/*{Token(param):X8}*/ "));
                }

                if (hasDefault)
                {
                    DumpDefaultValue(param, sb);
                }

                PrintLine(sb);
                if (ulCAs != 0)
                {
                    foreach (CustomAttributeHandle h in cas)
                    {
                        DumpCustomAttribute(h, withOwner: false);
                    }
                }
            }
        }
    }

    // ---------------------------------------------------------------- permissions

    private void DumpPermissions(EntityHandle owner)
    {
        foreach (DeclarativeSecurityAttributeHandle h in GetDeclSecurity(owner))
        {
            DeclarativeSecurityAttribute ds = _md.GetDeclarativeSecurityAttribute(h);
            byte[] blob = Blob(ds.PermissionSet);
            StringBuilder sb = new();
            sb.Append(Indent).Append(KEYWORD(".permissionset")).Append(' ');
            string action = ds.Action switch
            {
                DeclarativeSecurityAction.None => "",
                SecurityActionRequest => KEYWORD("request"),
                DeclarativeSecurityAction.Demand => KEYWORD("demand"),
                DeclarativeSecurityAction.Assert => KEYWORD("assert"),
                DeclarativeSecurityAction.Deny => KEYWORD("deny"),
                DeclarativeSecurityAction.PermitOnly => KEYWORD("permitonly"),
                DeclarativeSecurityAction.LinkDemand => KEYWORD("linkcheck"),
                DeclarativeSecurityAction.InheritanceDemand => KEYWORD("inheritcheck"),
                DeclarativeSecurityAction.RequestMinimum => KEYWORD("reqmin"),
                DeclarativeSecurityAction.RequestOptional => KEYWORD("reqopt"),
                DeclarativeSecurityAction.RequestRefuse => KEYWORD("reqrefuse"),
                SecurityActionPrejitGrant => KEYWORD("prejitgrant"),
                SecurityActionPrejitDeny => KEYWORD("prejitdeny"),
                SecurityActionNonCasDemand => KEYWORD("noncasdemand"),
                SecurityActionNonCasLinkDemand => KEYWORD("noncaslinkdemand"),
                SecurityActionNonCasInheritance => KEYWORD("noncasinheritance"),
                _ => ERRORMSG("<UNKNOWN_ACTION>"),
            };
            sb.Append(action);
            if (blob.Length != 0)
            {
                PrintLine(sb);
                sb.Clear();
                sb.Append(Indent).Append("          ");
                _permissionSetDisassembler.AppendPermissionSet(this, sb, ds.PermissionSet);

                PrintLine(sb);
            }
            else
            {
                sb.Append(" = ()");
                PrintLine(sb);
            }

            DumpCustomAttributes(h);
        }
    }

    // The CorDeclSecurity actions that DeclarativeSecurityAction does not define.
    private const DeclarativeSecurityAction SecurityActionRequest = (DeclarativeSecurityAction)1;
    private const DeclarativeSecurityAction SecurityActionPrejitGrant = (DeclarativeSecurityAction)11;
    private const DeclarativeSecurityAction SecurityActionPrejitDeny = (DeclarativeSecurityAction)12;
    private const DeclarativeSecurityAction SecurityActionNonCasDemand = (DeclarativeSecurityAction)13;
    private const DeclarativeSecurityAction SecurityActionNonCasLinkDemand = (DeclarativeSecurityAction)14;
    private const DeclarativeSecurityAction SecurityActionNonCasInheritance = (DeclarativeSecurityAction)15;

    // ---------------------------------------------------------------- generic parameters

    private string DumpGenericPars(EntityHandle owner, string prefix, bool split)
    {
        StringBuilder sb = new(prefix);
        ImmutableArray<GenericParameterHandle> gps = GetGenericParams(owner);
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
            GenericParameterAttributes attr = gp.Attributes;
            switch (attr & GenericParameterAttributes.VarianceMask)
            {
                case GenericParameterAttributes.Covariant: sb.Append("+ "); break;
                case GenericParameterAttributes.Contravariant: sb.Append("- "); break;
            }

            if ((attr & GenericParameterAttributes.ReferenceTypeConstraint) != 0)
            {
                sb.Append("class ");
            }

            if ((attr & GenericParameterAttributes.NotNullableValueTypeConstraint) != 0)
            {
                sb.Append("valuetype ");
            }

            if ((attr & GenericParameterAttributes.AllowByRefLike) != 0)
            {
                sb.Append("byreflike ");
            }

            if ((attr & GenericParameterAttributes.DefaultConstructorConstraint) != 0)
            {
                sb.Append(".ctor ");
            }

            int unknown = (int)(attr & ~(GenericParameterAttributes.VarianceMask | GenericParameterAttributes.SpecialConstraintMask | GenericParameterAttributes.AllowByRefLike));
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

                    sb.Append(PrettyPrintClass(NilAsDefault(_md.GetGenericParameterConstraint(c).Type)));
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

    private void DumpGenericParsCA(EntityHandle owner)
    {
        if (!ShowCustomAttributes)
        {
            return;
        }

        int i = 0;
        foreach (GenericParameterHandle gph in GetGenericParams(owner))
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
                    DumpCustomAttribute(h, withOwner: false);
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

                sb.Append(", ").Append(PrettyPrintClass(NilAsDefault(c.Type)));
                PrintLine(sb);
                PushIndent();
                foreach (CustomAttributeHandle h in ccas)
                {
                    DumpCustomAttribute(h, withOwner: false);
                }

                PopIndent();
            }

            i++;
        }
    }

    // ---------------------------------------------------------------- method signature line splitting

    private string PrettyPrintMethodSig(StringBuilder line, byte[] sig, string buff, SigLabel? label)
    {
        int uMaxWidth = _formatter.SignatureWrapWidth;
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

    private string PrettyPrintOverrideDecl(MethodImplRecord mi, EntityHandle overrider, out bool overridingTypeSpec)
    {
        StringBuilder sb = new();
        sb.Append(Indent).Append(KEYWORD(".override")).Append(' ');
        EntityHandle decl = mi.Decl;
        EntityHandle declParent = default;
        string tailSig = "";
        string memberName;
        overridingTypeSpec = false;

        if (IsValid(decl))
        {
            bool needsFullTokenPrint = false;
            bool hasDeclParent = false;
            declParent = GetParent(decl);
            if (IsValid(declParent) || declParent.Kind == HandleKind.ModuleDefinition)
            {
                if (declParent.Kind == HandleKind.MethodDefinition)
                {
                    declParent = GetParent(declParent);
                }

                if (!declParent.IsNil)
                {
                    if (declParent.Kind == HandleKind.TypeSpecification)
                    {
                        needsFullTokenPrint = true;
                    }

                    hasDeclParent = true;
                }
            }
            else
            {
                sb.Append(ERRORMSG("INVALID OVERRIDDEN METHOD'S PARENT TOKEN"));
            }

            if (overrider.Kind == HandleKind.MethodDefinition && !needsFullTokenPrint)
            {
                EntityHandle declMethod = decl.Kind == HandleKind.MethodSpecification
                    ? NilAsDefault(_md.GetMethodSpecification((MethodSpecificationHandle)decl).Method)
                    : decl;
                byte[]? declSig = GetMethodSig(declMethod);
                byte[] bodySig = Blob(_md.GetMethodDefinition((MethodDefinitionHandle)overrider).Signature);
                if (declSig is not null)
                {
                    if (!declSig.AsSpan().SequenceEqual(bodySig))
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
                PrettyPrintToken(sb, decl);
                overridingTypeSpec = true;
                return sb.ToString();
            }

            if (hasDeclParent)
            {
                PrettyPrintToken(sb, declParent);
                sb.Append("::");
            }

            if (decl.Kind == HandleKind.MethodSpecification)
            {
                MethodSpecification ms = _md.GetMethodSpecification((MethodSpecificationHandle)decl);
                byte[] inst = Blob(ms.Signature);
                if (inst.Length != 0)
                {
                    tailSig = PrettyPrintSig(inst, "");
                }

                decl = NilAsDefault(ms.Method);
            }

            memberName = decl.Kind switch
            {
                HandleKind.MethodDefinition => Str(_md.GetMethodDefinition((MethodDefinitionHandle)decl).Name),
                HandleKind.MemberReference => Str(_md.GetMemberReference((MemberReferenceHandle)decl).Name),
                _ => ERRORMSG($"INVALID TOKEN: 0x{Token(decl):X8}"),
            };
            memberName = MakeNameIfNone(memberName, decl);
        }
        else
        {
            memberName = ERRORMSG($"INVALID TOKEN: 0x{Token(decl):X8}");
        }

        sb.Append(ProperName(memberName)).Append(tailSig);
        if (_options.ShowTokens)
        {
            // A nil MemberRef parent keeps its table.
            sb.Append(COMMENT($" /*{MetadataTokens.GetToken(declParent):X8}::{Token(mi.Decl):X8}*/ "));
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

    private bool DumpMethod(MethodDefinitionHandle method, string? className, bool dumpBody)
    {
        MethodDefinition md = _md.GetMethodDefinition(method);
        MethodAttributes attrs = md.Attributes;
        if (IsHiddenMethod(attrs))
        {
            return false;
        }

        MethodImplAttributes implAttrs = md.ImplAttributes;
        uint rva = (uint)md.RelativeVirtualAddress;
        string memberName = MakeNameIfNone(Str(md.Name), method);
        byte[] sig = Blob(md.Signature);
        if (sig.Length == 0)
        {
            PrintError(ERRORMSG($"{Indent}ERROR: method '{memberName}' has no signature"));
            return false;
        }

        EntityHandle savedMVarOwner = _mvarOwner;
        _mvarOwner = method;
        string plainSig = PrettyPrintSig(sig, DumpGenericPars(method, "", false));

        if (_mode is DumpMode.ClassMethod or DumpMode.ClassMethodSig)
        {
            if (memberName != _methodToDump)
            {
                _mvarOwner = savedMVarOwner;
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
                    _mvarOwner = savedMVarOwner;
                    return false;
                }
            }
        }

        if (!dumpBody)
        {
            PrintLine(plainSig);
            _mvarOwner = savedMVarOwner;
            return true;
        }

        StringBuilder sb = new();
        sb.Append(Indent).Append(ANCHORPT(KEYWORD(".method"), method)).Append(' ');
        if (_options.ShowTokens)
        {
            sb.Append(COMMENT($"/*{Token(method):X8}*/ "));
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

        if ((attrs & MethodAttributes.HideBySig) != 0) sb.Append("hidebysig ");
        if ((attrs & MethodAttributes.NewSlot) != 0) sb.Append("newslot ");
        if ((attrs & MethodAttributes.SpecialName) != 0) sb.Append("specialname ");
        if ((attrs & MethodAttributes.RTSpecialName) != 0) sb.Append("rtspecialname ");
        if ((attrs & MethodAttributes.Static) != 0) sb.Append("static ");
        if ((attrs & MethodAttributes.Abstract) != 0) sb.Append("abstract ");
        if ((attrs & MethodAttributes.CheckAccessOnOverride) != 0) sb.Append("strict ");
        if ((attrs & MethodAttributes.Virtual) != 0) sb.Append("virtual ");
        if ((attrs & MethodAttributes.Final) != 0) sb.Append("final ");
        if ((attrs & MethodAttributes.UnmanagedExport) != 0) sb.Append("unmanagedexp ");
        if ((attrs & MethodAttributes.RequireSecObject) != 0) sb.Append("reqsecobj ");
        sb.Append(KeywordPostfix);

        if ((attrs & MethodAttributes.PinvokeImpl) != 0)
        {
            sb.Append(KEYWORD("pinvokeimpl")).Append('(');
            var map = GetPinvokeMap(method);
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

        _thisIsInstanceMethod = (attrs & MethodAttributes.Static) == 0;
        string buff = IsMdPrivateScope(attrs) ? memberName + "$PST" + X8(Token(method)) : memberName;
        buff = DumpGenericPars(method, ProperName(buff), false);

        // Argument names and attributes: names[n] for parameter n+1, names[numArgs] for the return value.
        int numArgs;
        {
            int p = 0;
            uint callConv = UncompressData(sig, ref p);
            if ((callConv & (uint)SignatureAttributes.Generic) != 0)
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

                    names[ix].Attr = prm.Attributes;
                    names[ix].Param = ph;
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
        MethodImplAttributes codeType = implAttrs & MethodImplAttributes.CodeTypeMask;
        switch (codeType)
        {
            case MethodImplAttributes.Native: sb.Append(" native"); break;
            case MethodImplAttributes.IL: sb.Append(" cil"); break;
            case MethodImplAttributes.OPTIL: sb.Append(" optil"); break;
            case MethodImplAttributes.Runtime: sb.Append(" runtime"); break;
        }

        bool unmanaged = (implAttrs & MethodImplAttributes.Unmanaged) != 0;
        sb.Append(unmanaged ? " unmanaged" : " managed");
        if ((implAttrs & MethodImplAttributes.PreserveSig) != 0) sb.Append(" preservesig");
        if ((implAttrs & MethodImplAttributes.ForwardRef) != 0) sb.Append(" forwardref");
        if ((implAttrs & MethodImplAttributes.InternalCall) != 0) sb.Append(" internalcall");
        if ((implAttrs & MethodImplAttributes.Synchronized) != 0) sb.Append(" synchronized");
        if ((implAttrs & MethodImplAttributes.NoInlining) != 0) sb.Append(" noinlining");
        if ((implAttrs & MethodImplAttributes.AggressiveInlining) != 0) sb.Append(" aggressiveinlining");
        if ((implAttrs & MethodImplAttributes.NoOptimization) != 0) sb.Append(" nooptimization");
        if ((implAttrs & MethodImplAttributes.AggressiveOptimization) != 0) sb.Append(" aggressiveoptimization");
        if ((implAttrs & MethodImplAttributes.Async) != 0) sb.Append(" async");
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

        if (method == _entryPoint)
        {
            PrintLine(Indent + KEYWORD(".entrypoint"));
        }

        DumpCustomAttributes(method);
        DumpGenericParsCA(method);
        DumpParams(names, retParamIx);
        DumpPermissions(method);

        foreach (VTableRef vt in _vtableRefs)
        {
            if (vt.Method == method)
            {
                PrintLine($"{Indent}{KEYWORD(".vtentry")} {vt.Entry + 1} : {vt.Slot + 1}");
                break;
            }
        }

        for (int j = 0; j < _eatRefs.Count; j++)
        {
            if (_eatRefs[j].Method == method)
            {
                PrintLine($"{Indent}{KEYWORD(".export")} [{j + _eatBase}] {KEYWORD("as")} {ProperName(_eatRefs[j].Name ?? "")}");
                break;
            }
        }

        foreach (MethodImplRecord mi in _methodImpls)
        {
            if (mi.Body == method)
            {
                PrintLine(PrettyPrintOverrideDecl(mi, method, out _));
            }
        }

        bool done = false;
        if ((attrs & MethodAttributes.PinvokeImpl) != 0)
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
                _mvarOwner = savedMVarOwner;
                return true;
            }

            done = true;
        }

        if (!done)
        {
            if (!unmanaged)
            {
                if (codeType is MethodImplAttributes.IL or MethodImplAttributes.OPTIL)
                {
                    if (_options.ShowBytes)
                    {
                        PrintLine(COMMENT($"{Indent}// Method begins at RVA 0x{rva:x}"));
                    }

                    if (rva != 0)
                    {
                        DisassembleWrapper(rva, method, names, ulArgs);
                    }
                }
                else if (codeType == MethodImplAttributes.Native)
                {
                    DumpNativeMethodComment(rva);
                }
            }
            else if (codeType == MethodImplAttributes.Native)
            {
                PrintLine(COMMENT($"{Indent}//  Unmanaged TargetRVA = 0x{rva:X8}"));
            }
            else if (codeType == MethodImplAttributes.Runtime)
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
        _mvarOwner = savedMVarOwner;
        return true;
    }

    private void DumpNativeMethodComment(uint rva)
    {
        PrintLine(COMMENT($"{Indent}// Disassembly of native methods is not supported."));
        PrintLine(COMMENT($"{Indent}//  Managed TargetRVA = 0x{rva:X8}"));
    }

    private void DisassembleWrapper(uint rva, MethodDefinitionHandle method, ParamDescriptor[]? names, int ulArgs)
    {
        MethodBody? body = DecodeMethodBody(rva);
        if (body is null)
        {
            PrintError($"INVALID METHOD ADDRESS: 0x{0:X8} (RVA: 0x{rva:X8})");
            return;
        }

        try
        {
            Disassemble(body, method, names, ulArgs);
        }
        catch (Exception e) when (e is IndexOutOfRangeException or ArgumentOutOfRangeException or ArgumentException or BadImageFormatException)
        {
            PrintLine($"{Indent} ********* ERROR DISASSEMBLING THE METHOD IL CODE ***********");
        }
    }

    private void DumpPinvokeMap(StringBuilder sb, MethodImportAttributes flags, string? importName, ModuleReferenceHandle moduleRef)
    {
        string dll = IsValid(moduleRef) ? Str(_md.GetModuleReference(moduleRef).Name) : "";
        if (dll.Length != 0)
        {
            DumpQString(sb, dll, Indent, 80);
        }

        if (!string.IsNullOrEmpty(importName))
        {
            sb.Append(KEYWORD(" as "));
            DumpQString(sb, importName, Indent, 80);
        }

        sb.Append(KeywordPrefix);
        if ((flags & MethodImportAttributes.ExactSpelling) != 0) sb.Append(" nomangle");
        switch (flags & MethodImportAttributes.CharSetMask)
        {
            case MethodImportAttributes.CharSetAnsi: sb.Append(" ansi"); break;
            case MethodImportAttributes.CharSetUnicode: sb.Append(" unicode"); break;
            case MethodImportAttributes.CharSetAuto: sb.Append(" autochar"); break;
        }

        if ((flags & MethodImportAttributes.SetLastError) != 0) sb.Append(" lasterr");
        switch (flags & MethodImportAttributes.CallingConventionMask)
        {
            case MethodImportAttributes.CallingConventionWinApi: sb.Append(" winapi"); break;
            case MethodImportAttributes.CallingConventionCDecl: sb.Append(" cdecl"); break;
            case MethodImportAttributes.CallingConventionThisCall: sb.Append(" thiscall"); break;
            case MethodImportAttributes.CallingConventionFastCall: sb.Append(" fastcall"); break;
            case MethodImportAttributes.CallingConventionStdCall: sb.Append(" stdcall"); break;
        }

        switch (flags & MethodImportAttributes.BestFitMappingMask)
        {
            case MethodImportAttributes.BestFitMappingEnable: sb.Append(" bestfit:on"); break;
            case MethodImportAttributes.BestFitMappingDisable: sb.Append(" bestfit:off"); break;
        }

        switch (flags & MethodImportAttributes.ThrowOnUnmappableCharMask)
        {
            case MethodImportAttributes.ThrowOnUnmappableCharEnable: sb.Append(" charmaperror:on"); break;
            case MethodImportAttributes.ThrowOnUnmappableCharDisable: sb.Append(" charmaperror:off"); break;
        }

        sb.Append(KeywordPostfix);
    }

    // ---------------------------------------------------------------- fields

    private bool DumpField(FieldDefinitionHandle field, string? className, bool dumpBody)
    {
        FieldDefinition fd = _md.GetFieldDefinition(field);
        string psz = MakeNameIfNone(Str(fd.Name), field);
        FieldAttributes attrs = fd.Attributes;
        if (IsHiddenField(attrs))
        {
            return false;
        }

        string memberName = ProperName(IsFdPrivateScope(attrs) ? psz + "$PST" + X8(Token(field)) : psz);
        byte[] sig = Blob(fd.Signature);
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
            sb.Append(Indent).Append(ANCHORPT(KEYWORD(".field"), field)).Append(' ');
            if (_options.ShowTokens)
            {
                sb.Append(COMMENT($"/*{Token(field):X8}*/ "));
            }
        }

        if (_fieldOffsets.TryGetValue(field, out uint offset) && offset != 0xFFFFFFFF)
        {
            sb.Append('[').Append(((int)offset).ToString(CultureInfo.InvariantCulture)).Append("] ");
        }

        sb.Append(KeywordPrefix);
        switch (attrs & FieldAttributes.FieldAccessMask)
        {
            case FieldAttributes.Public: sb.Append("public "); break;
            case FieldAttributes.Private: sb.Append("private "); break;
        }

        if ((attrs & FieldAttributes.Static) != 0) sb.Append("static ");
        switch (attrs & FieldAttributes.FieldAccessMask)
        {
            case FieldAttributes.Family: sb.Append("family "); break;
            case FieldAttributes.Assembly: sb.Append("assembly "); break;
            case FieldAttributes.FamANDAssem: sb.Append("famandassem "); break;
            case FieldAttributes.FamORAssem: sb.Append("famorassem "); break;
            case FieldAttributes.PrivateScope: sb.Append("privatescope "); break;
        }

        if ((attrs & FieldAttributes.InitOnly) != 0) sb.Append("initonly ");
        if ((attrs & FieldAttributes.Literal) != 0) sb.Append("literal ");
#pragma warning disable SYSLIB0050 // Obsolete for formatter-based serialization, still a metadata flag.
        if ((attrs & FieldAttributes.NotSerialized) != 0) sb.Append("notserialized ");
#pragma warning restore SYSLIB0050
        if ((attrs & FieldAttributes.SpecialName) != 0) sb.Append("specialname ");
        if ((attrs & FieldAttributes.RTSpecialName) != 0) sb.Append("rtspecialname ");
        sb.Append(KeywordPostfix);

        if ((attrs & FieldAttributes.PinvokeImpl) != 0)
        {
            sb.Append(KEYWORD("pinvokeimpl")).Append('(');
            var map = GetPinvokeMap(field);
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

        sb.Append(DumpMarshaling(field));
        sb.Append(szStr);

        if ((attrs & FieldAttributes.HasFieldRVA) != 0)
        {
            sb.Append(KEYWORD(" at "));
            DumpDataPtr(sb, (uint)fd.GetRelativeVirtualAddress(), SizeOfField(field));
        }

        if ((attrs & FieldAttributes.HasDefault) != 0 && dumpBody)
        {
            DumpDefaultValue(field, sb);
        }

        PrintLine(sb);
        if (dumpBody)
        {
            DumpCustomAttributes(field);
            DumpPermissions(field);
        }

        return true;
    }

    // ---------------------------------------------------------------- events and properties

    // The accessors of an event or property in table order. EventDefinition/PropertyDefinition.GetAccessors() group them
    // by kind, keep only the last getter, setter, adder, remover or raiser, drop other semantics, and need a sorted
    // table, while the output has every MethodSemantics row in table order: the rows are read from the table.
    private List<(MethodDefinitionHandle Method, MethodSemanticsAttributes Semantics)> GetAssociates(EntityHandle association)
    {
        List<(MethodDefinitionHandle, MethodSemanticsAttributes)> list = new();
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
        // HasSemantics: the tag is 0 for an event, 1 for a property.
        uint target = (uint)(MetadataTokens.GetRowNumber(association) << 1 | (association.Kind == HandleKind.PropertyDefinition ? 1 : 0));
        BlobReader r = _metadataBlock.GetReader();
        for (int i = 0; i < rows; i++)
        {
            r.Offset = baseOffset + i * rowSize;
            ushort sem = r.ReadUInt16();
            uint method = methodSize == 2 ? r.ReadUInt16() : r.ReadUInt32();
            uint assoc = assocSize == 2 ? r.ReadUInt16() : r.ReadUInt32();
            if (assoc == target)
            {
                list.Add((MetadataTokens.MethodDefinitionHandle((int)method), (MethodSemanticsAttributes)sem));
            }
        }

        return list;
    }

    private bool AllAssociatesHidden(List<(MethodDefinitionHandle Method, MethodSemanticsAttributes Semantics)> assoc)
    {
        if (!_limitedVisibility || assoc.Count == 0)
        {
            return false;
        }

        foreach ((MethodDefinitionHandle m, _) in assoc)
        {
            if (IsValid(m) && !IsHiddenMethod(_md.GetMethodDefinition(m).Attributes))
            {
                return false;
            }
        }

        return true;
    }

    private void AppendAccessor(StringBuilder sb, MethodDefinitionHandle m)
    {
        if (IsValid(m))
        {
            PrettyPrintToken(sb, m);
        }
        else
        {
            sb.Append(ERRORMSG($"INVALID TOKEN 0x{MetadataTokens.GetToken(m):X8}"));
        }
    }

    private bool DumpEvent(EventDefinitionHandle evh, string className, bool dumpBody)
    {
        EventDefinition ev = _md.GetEventDefinition(evh);
        string psz = MakeNameIfNone(Str(ev.Name), evh);
        EventAttributes attrs = ev.Attributes;
        EntityHandle eventType = ev.Type;
        if (_mode is DumpMode.ClassMethod or DumpMode.ClassMethodSig && psz != _methodToDump)
        {
            return false;
        }

        List<(MethodDefinitionHandle Method, MethodSemanticsAttributes Semantics)> assoc = GetAssociates(evh);
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
                sb.Append(COMMENT($"/*{Token(evh):X8}*/ "));
            }
        }
        else
        {
            sb.Append(ProperName(psz)).Append(" : ");
        }

        if ((attrs & EventAttributes.SpecialName) != 0)
        {
            sb.Append(KEYWORD("specialname "));
        }

        if ((attrs & EventAttributes.RTSpecialName) != 0)
        {
            sb.Append(KEYWORD("rtspecialname "));
        }

        if (IsValid(eventType) && eventType.Kind is HandleKind.TypeReference or HandleKind.TypeDefinition or HandleKind.TypeSpecification)
        {
            PrettyPrintToken(sb, eventType);
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
        DumpCustomAttributes(evh);
        DumpPermissions(evh);
        foreach ((MethodDefinitionHandle m, MethodSemanticsAttributes sem) in assoc)
        {
            sb.Clear();
            if ((sem & MethodSemanticsAttributes.Adder) != 0) sb.Append(Indent).Append(KEYWORD(".addon")).Append(' ');
            else if ((sem & MethodSemanticsAttributes.Remover) != 0) sb.Append(Indent).Append(KEYWORD(".removeon")).Append(' ');
            else if ((sem & MethodSemanticsAttributes.Raiser) != 0) sb.Append(Indent).Append(KEYWORD(".fire")).Append(' ');
            else if ((sem & MethodSemanticsAttributes.Other) != 0) sb.Append(Indent).Append(KEYWORD(".other")).Append(' ');
            else sb.Append(ERRORMSG($"UNKNOWN SEMANTICS: 0x{(int)sem:X} "));

            AppendAccessor(sb, m);

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

    private bool DumpProp(PropertyDefinitionHandle prh, string className, bool dumpBody)
    {
        PropertyDefinition pr = _md.GetPropertyDefinition(prh);
        string psz = MakeNameIfNone(Str(pr.Name), prh);
        PropertyAttributes attrs = pr.Attributes;
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

        List<(MethodDefinitionHandle Method, MethodSemanticsAttributes Semantics)> assoc = GetAssociates(prh);
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
                sb.Append(COMMENT($"/*{Token(prh):X8}*/ "));
            }
        }
        else
        {
            sb.Append(ProperName(psz)).Append(" : ");
        }

        if ((attrs & PropertyAttributes.SpecialName) != 0)
        {
            sb.Append(KEYWORD("specialname "));
        }

        if ((attrs & PropertyAttributes.RTSpecialName) != 0)
        {
            sb.Append(KEYWORD("rtspecialname "));
        }

        string line = PrettyPrintMethodSig(sb, sig, dumpBody ? ProperName(psz) : "", null);
        sb.Clear();
        sb.Append(line);
        if ((attrs & PropertyAttributes.HasDefault) != 0 && dumpBody)
        {
            DumpDefaultValue(prh, sb);
        }

        PrintLine(sb);
        if (dumpBody)
        {
            PrintLine(Indent + SCOPE());
            PushIndent();
            DumpCustomAttributes(prh);
            DumpPermissions(prh);
            foreach ((MethodDefinitionHandle m, MethodSemanticsAttributes sem) in assoc)
            {
                sb.Clear();
                if ((sem & MethodSemanticsAttributes.Setter) != 0) sb.Append(Indent).Append(KEYWORD(".set")).Append(' ');
                else if ((sem & MethodSemanticsAttributes.Getter) != 0) sb.Append(Indent).Append(KEYWORD(".get")).Append(' ');
                else if ((sem & MethodSemanticsAttributes.Other) != 0) sb.Append(Indent).Append(KEYWORD(".other")).Append(' ');
                else sb.Append(ERRORMSG($"UNKNOWN SEMANTICS: 0x{(int)sem:X} "));

                AppendAccessor(sb, m);

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

    private void DumpMembers(TypeDefinitionHandle cl, string className)
    {
        TypeDefinition td = _md.GetTypeDefinition(cl);
        List<EntityHandle> members = new();
        foreach (FieldDefinitionHandle h in td.GetFields())
        {
            members.Add(h);
        }

        foreach (MethodDefinitionHandle h in td.GetMethods())
        {
            members.Add(h);
        }

        foreach (EventDefinitionHandle h in td.GetEvents())
        {
            members.Add(h);
        }

        foreach (PropertyDefinitionHandle h in td.GetProperties())
        {
            members.Add(h);
        }

        foreach (EntityHandle h in members)
        {
            bool ret;
            if (IsValid(h))
            {
                ret = h.Kind switch
                {
                    HandleKind.FieldDefinition => DumpField((FieldDefinitionHandle)h, className, true),
                    HandleKind.MethodDefinition => DumpMethod((MethodDefinitionHandle)h, className, true),
                    HandleKind.EventDefinition => DumpEvent((EventDefinitionHandle)h, className, true),
                    HandleKind.PropertyDefinition => DumpProp((PropertyDefinitionHandle)h, className, true),
                    _ => false,
                };
            }
            else
            {
                PrintLine(ERRORMSG($"INVALID MEMBER TOKEN: 0x{Token(h):X8}"));
                ret = false;
            }

            if (ret && _mode == DumpMode.ClassMethodSig)
            {
                break;
            }
        }
    }

    private bool GetClassLayout(TypeDefinitionHandle cl, out uint packSize, out uint classSize)
    {
        _fieldOffsets.Clear();
        packSize = 0xFFFFFFFF;
        classSize = 0xFFFFFFFF;
        if (cl.IsNil)
        {
            return true;
        }

        bool ret = false;
        TypeDefinition td = _md.GetTypeDefinition(cl);
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
                _fieldOffsets[fh] = (uint)off;
                ret = true;
            }
        }

        return ret;
    }

    // Whether the type has a ClassLayout row: TypeDefinition.GetLayout() returns the same (default) layout for a row
    // with zero packing and size and for no row, which print differently (.pack 0 .size 0, or nothing).
    private bool HasClassLayoutRow(TypeDefinitionHandle cl)
    {
        int rows = RowCount(TableIndex.ClassLayout);
        if (rows == 0)
        {
            return false;
        }

        // ClassLayout: PackingSize (2), ClassSize (4), Parent (TypeDef index)
        int rowSize = _md.GetTableRowSize(TableIndex.ClassLayout);
        int typeDefSize = RowCount(TableIndex.TypeDef) > ushort.MaxValue ? 4 : 2;
        int baseOffset = _md.GetTableMetadataOffset(TableIndex.ClassLayout);
        BlobReader r = _metadataBlock.GetReader();
        for (int i = 0; i < rows; i++)
        {
            r.Offset = baseOffset + i * rowSize + 6;
            uint parent = typeDefSize == 2 ? r.ReadUInt16() : r.ReadUInt32();
            if (parent == MetadataTokens.GetRowNumber(cl))
            {
                return true;
            }
        }

        return false;
    }

    private bool IsANestedInB(TypeDefinitionHandle a, TypeDefinitionHandle b)
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

            if (a.IsNil)
            {
                return false;
            }
        }
    }

    private TypeDefinitionHandle TopEncloser(TypeDefinitionHandle a)
    {
        while (true)
        {
            int ix = _classList.IndexOf(a);
            if (ix < 0 || _classEnclosing[ix].IsNil)
            {
                return a;
            }

            a = _classEnclosing[ix];
        }
    }

    // The parts of a class to dump (DumpClass's whatToDump).
    [Flags]
    private enum ClassParts
    {
        // The title, flags, extends and implements.
        Header = 0,
        // The pack, size, custom attributes and the rest of the class declaration.
        Declaration = 1,
        NestedClasses = 2,
        Members = 4,
        All = Declaration | NestedClasses | Members,
    }

    private bool DumpClass(TypeDefinitionHandle cl, ClassParts whatToDump)
    {
        EntityHandle savedVarOwner = _varOwner;
        ClassParts whatToDumpOrig = whatToDump;
        (string pc1, string pc2) = GetNameOfTypeDef(cl);
        pc1 = MakeNameIfNone(pc1, cl);

        if (_mode is DumpMode.Class or DumpMode.ClassMethod or DumpMode.ClassMethodSig)
        {
            if (cl != _classToDump)
            {
                if (IsANestedInB(_classToDump, cl))
                {
                    whatToDump = ClassParts.NestedClasses;
                }
                else
                {
                    return true;
                }
            }
        }

        TypeDefinition td = _md.GetTypeDefinition(cl);
        TypeAttributes attrs = td.Attributes;
        EntityHandle extends = td.BaseType;
        TypeAttributes vis = attrs & TypeAttributes.VisibilityMask;
        if (_limitedVisibility)
        {
            if (_hidePub && vis is TypeAttributes.Public or TypeAttributes.NestedPublic) return false;
            if (_hidePriv && vis is TypeAttributes.NotPublic or TypeAttributes.NestedPrivate) return false;
            if (_hideFam && vis == TypeAttributes.NestedFamily) return false;
            if (_hideAsm && vis == TypeAttributes.NestedAssembly) return false;
            if (_hideFOA && vis == TypeAttributes.NestedFamORAssem) return false;
            if (_hideFAA && vis == TypeAttributes.NestedFamANDAssem) return false;
        }

        _varOwner = cl;
        string className = pc1;
        string ns = pc2;

        StringBuilder sb = new();
        sb.Append(Indent).Append(KEYWORD(".class")).Append(' ');
        if (_options.ShowTokens)
        {
            sb.Append(COMMENT($"/*{Token(cl):X8}*/ "));
        }

        sb.Append(KeywordPrefix);
        if ((attrs & TypeAttributes.Interface) != 0) sb.Append("interface ");
        switch (vis)
        {
            case TypeAttributes.Public: sb.Append("public "); break;
            case TypeAttributes.NotPublic: sb.Append("private "); break;
        }

        if ((attrs & TypeAttributes.Abstract) != 0) sb.Append("abstract ");
        switch (attrs & TypeAttributes.LayoutMask)
        {
            case TypeAttributes.AutoLayout: sb.Append("auto "); break;
            case TypeAttributes.SequentialLayout: sb.Append("sequential "); break;
            case TypeAttributes.ExplicitLayout: sb.Append("explicit "); break;
            case TypeAttributes.ExtendedLayout: sb.Append("extended "); break;
        }

        switch (attrs & TypeAttributes.StringFormatMask)
        {
            case TypeAttributes.AnsiClass: sb.Append("ansi "); break;
            case TypeAttributes.UnicodeClass: sb.Append("unicode "); break;
            case TypeAttributes.AutoClass: sb.Append("autochar "); break;
        }

        if ((attrs & TypeAttributes.Import) != 0) sb.Append("import ");
        if ((attrs & TypeAttributes.WindowsRuntime) != 0) sb.Append("windowsruntime ");
#pragma warning disable SYSLIB0050 // Obsolete for formatter-based serialization, still a metadata flag.
        if ((attrs & TypeAttributes.Serializable) != 0) sb.Append("serializable ");
#pragma warning restore SYSLIB0050
        if ((attrs & TypeAttributes.Sealed) != 0) sb.Append("sealed ");
        switch (vis)
        {
            case TypeAttributes.NestedPublic: sb.Append("nested public "); break;
            case TypeAttributes.NestedPrivate: sb.Append("nested private "); break;
            case TypeAttributes.NestedFamily: sb.Append("nested family "); break;
            case TypeAttributes.NestedAssembly: sb.Append("nested assembly "); break;
            case TypeAttributes.NestedFamANDAssem: sb.Append("nested famandassem "); break;
            case TypeAttributes.NestedFamORAssem: sb.Append("nested famorassem "); break;
        }

        if ((attrs & TypeAttributes.BeforeFieldInit) != 0) sb.Append("beforefieldinit ");
        if ((attrs & TypeAttributes.SpecialName) != 0) sb.Append("specialname ");
        if ((attrs & TypeAttributes.RTSpecialName) != 0) sb.Append("rtspecialname ");
        sb.Append(KeywordPostfix);
        if (ns.Length != 0)
        {
            sb.Append(ProperName(ns)).Append('.');
        }

        sb.Append(whatToDump > ClassParts.NestedClasses ? ANCHORPT(ProperName(className), cl) : JUMPPT(ProperName(className), cl));
        string header = DumpGenericPars(cl, sb.ToString(), split: true);
        PrintLine(header);

        if (!extends.IsNil)
        {
            sb.Clear();
            sb.Append(Indent).Append("       ").Append(KEYWORD("extends")).Append(' ');
            if (IsValid(extends))
            {
                PrettyPrintToken(sb, extends);
            }
            else
            {
                sb.Append(ERRORMSG($"INVALID TOKEN: 0x{Token(extends):X8}"));
            }

            PrintLine(sb);
        }

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

            AppendInterface(sb, iih);
            if (i < numInterfaces - 1)
            {
                sb.Append(',');
            }

            PrintLine(sb);
            i++;
        }

        if (whatToDump == ClassParts.Header)
        {
            PrintLine($"{Indent}{SCOPE()} {UNSCOPE()}");
            _varOwner = savedVarOwner;
            return true;
        }

        PrintLine(Indent + SCOPE());
        PushIndent();

        if ((whatToDump & ClassParts.Declaration) != 0)
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
                            AppendInterface(sb, iih);
                            PrintLine(sb);
                            first = false;
                        }

                        DumpCustomAttribute(cah, withOwner: false);
                    }
                }
            }

            DumpGenericParsCA(cl);
            DumpPermissions(cl);

            foreach (MethodImplRecord mi in _methodImpls)
            {
                if (mi.Class == cl && mi.BodyParent != cl)
                {
                    sb.Clear();
                    sb.Append(PrettyPrintOverrideDecl(mi, cl, out bool overridingTypeSpec));
                    sb.Append(KEYWORD(" with "));
                    if (overridingTypeSpec)
                    {
                        sb.Append(KEYWORD("method "));
                    }

                    PrettyPrintToken(sb, mi.Body);
                    PrintLine(sb);
                }
            }
        }
        else
        {
            _fieldOffsets.Clear();
        }

        if ((whatToDump & ClassParts.NestedClasses) != 0)
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
                    DumpClass(_classList[k], whatToDumpOrig);
                    regetLayout = true;
                }
            }

            if (regetLayout)
            {
                GetClassLayout(cl, out _, out _);
            }

            _mode = mode;
        }

        if ((whatToDump & ClassParts.Members) != 0)
        {
            DumpMembers(cl, className);
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
        _varOwner = savedVarOwner;
        return true;
    }

    private void DumpGlobalMethods()
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
            if (DumpMethod(h, null, true) && _mode is DumpMode.ClassMethod or DumpMode.ClassMethodSig)
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
            if (DumpField(h, null, true) && _mode is DumpMode.ClassMethod or DumpMode.ClassMethodSig)
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

    // The interface of an InterfaceImpl row.
    private void AppendInterface(StringBuilder sb, InterfaceImplementationHandle h)
    {
        EntityHandle iface = _md.GetInterfaceImplementation(h).Interface;
        if (IsValid(iface))
        {
            PrettyPrintToken(sb, iface);
        }
        else
        {
            sb.Append(ERRORMSG($"INVALID TOKEN: 0x{Token(iface):X8}"));
        }
    }

    private TypeDefinition GlobalType()
        => RowCount(TableIndex.TypeDef) > 0 ? _md.GetTypeDefinition(s_moduleType) : default;
}
