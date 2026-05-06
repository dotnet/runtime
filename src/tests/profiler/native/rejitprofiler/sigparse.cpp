// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "sigparse.h"

bool SigParser::Parse(sig_byte *pb, sig_count cbBuffer)
{
    pbBase = pb;
    pbCur = pb;
    pbEnd = pbBase + cbBuffer;

    sig_elem_type elem_type;

    if (!ParseByte(&elem_type))
        return false;

    switch (elem_type & 0xf)
    {
    case SIG_METHOD_DEFAULT:  // default calling convention
    case SIG_METHOD_C:        // C calling convention
    case SIG_METHOD_STDCALL:  // Stdcall calling convention
    case SIG_METHOD_THISCALL: // thiscall  calling convention
    case SIG_METHOD_FASTCALL: // fastcall calling convention
    case SIG_METHOD_VARARG:   // vararg calling convention
            return ParseMethod(elem_type);
            break;

    case SIG_FIELD:           // encodes a field
            return ParseField(elem_type);
            break;

    case SIG_LOCAL_SIG:       // used for the .locals directive
            return ParseLocals(elem_type);
            break;

    case SIG_PROPERTY:        // used to encode a property
            return ParseProperty(elem_type);
            break;

    default:
            // unknown signature
            break;
    }

    return false;
}


bool SigParser::ParseByte(sig_byte *pbOut)
{
    if (pbCur < pbEnd)
    {
        *pbOut = *pbCur;
        pbCur++;
        return true;
    }

    return false;
}


bool SigParser::ParseMethod(sig_elem_type elem_type)
{
    // MethodDefSig ::= [[HASTHIS] [EXPLICITTHIS]] (DEFAULT|VARARG|GENERIC GenParamCount)
    //                    ParamCount RetType Param* [SENTINEL Param+]

    NotifyBeginMethod(elem_type);

    sig_count gen_param_count;
    sig_count param_count;

    if (elem_type & (SIG_HASTHIS | SIG_EXPLICITTHIS))
    {
        NotifyHasThis();
    }

    if (elem_type & SIG_GENERIC)
    {
        if (!ParseNumber(&gen_param_count))
                return false;

        NotifyGenericParamCount(gen_param_count);
    }

    if (!ParseNumber(&param_count))
        return false;

    NotifyParamCount(param_count);

    if (!ParseRetType())
        return false;

    bool fEncounteredSentinel = false;

    for (sig_count i = 0; i < param_count; i++)
    {
        if (pbCur >= pbEnd)
            return false;

        if (*pbCur == ELEMENT_TYPE_SENTINEL)
        {
                if (fEncounteredSentinel)
                    return false;

                fEncounteredSentinel = true;
                NotifySentinel();
                pbCur++;
        }

        if (!ParseParam())
            return false;
    }

    NotifyEndMethod();

    return true;
}


bool SigParser::ParseField(sig_elem_type elem_type)
{
    // FieldSig ::= FIELD CustomMod* Type

    NotifyBeginField(elem_type);

    if (!ParseOptionalCustomMods())
        return false;

    if (!ParseType())
        return false;

    NotifyEndField();

    return true;
}


bool SigParser::ParseProperty(sig_elem_type elem_type)
{
    // PropertySig ::= PROPERTY [HASTHIS] ParamCount CustomMod* Type Param*

    NotifyBeginProperty(elem_type);

    sig_count param_count;

    if (!ParseNumber(&param_count))
        return false;

    NotifyParamCount(param_count);

    if (!ParseOptionalCustomMods())
        return false;

    for (sig_count i = 0; i < param_count; i++)
    {
        if (!ParseParam())
            return false;
    }

    NotifyEndProperty();

    return true;
}


bool SigParser::ParseLocals(sig_elem_type elem_type)
{
    //   LocalVarSig ::= LOCAL_SIG Count (TYPEDBYREF | ([CustomMod] [Constraint])* [BYREF] Type)+

    NotifyBeginLocals(elem_type);

    sig_count local_count;

    if (!ParseNumber(&local_count))
        return false;

    NotifyLocalsCount(local_count);

    for (sig_count i = 0; i < local_count; i++)
    {
        if (!ParseLocal())
            return false;
    }

    NotifyEndLocals();

    return true;
}


bool SigParser::ParseLocal()
{
    //TYPEDBYREF | ([CustomMod] [Constraint])* [BYREF] Type
    NotifyBeginLocal();

    if (pbCur >= pbEnd)
        return false;

    if (*pbCur == ELEMENT_TYPE_TYPEDBYREF)
    {
        NotifyTypedByref();
        pbCur++;
        goto Success;
    }

    if (!ParseOptionalCustomModsOrConstraint())
        return false;

    if (pbCur >= pbEnd)
        return false;

    if (*pbCur == ELEMENT_TYPE_BYREF)
    {
        NotifyByref();
        pbCur++;
    }

    if (!ParseType())
        return false;

Success:
    NotifyEndLocal();
    return true;
}


bool SigParser::ParseOptionalCustomModsOrConstraint()
{
    for (;;)
    {
        if (pbCur >= pbEnd)
            return true;

        switch (*pbCur)
        {
        case ELEMENT_TYPE_CMOD_OPT:
        case ELEMENT_TYPE_CMOD_REQD:
            if (!ParseCustomMod())
                return false;
            break;

        case ELEMENT_TYPE_PINNED:
            NotifyConstraint(*pbCur);
            pbCur++;
            break;

        default:
            return true;
        }
    }

    return false;
}


bool SigParser::ParseOptionalCustomMods()
{
    for (;;)
    {
        if (pbCur >= pbEnd)
            return true;

        switch (*pbCur)
        {
        case ELEMENT_TYPE_CMOD_OPT:
        case ELEMENT_TYPE_CMOD_REQD:
            if (!ParseCustomMod())
                return false;
            break;

        default:
            return true;
        }
    }

    return false;
}



bool SigParser::ParseCustomMod()
{
    sig_elem_type cmod = 0;
    sig_index index;
    sig_index_type indexType;

    if (!ParseByte(&cmod))
        return false;

    if (cmod == ELEMENT_TYPE_CMOD_OPT || cmod == ELEMENT_TYPE_CMOD_REQD)
    {
        if (!ParseTypeDefOrRefEncoded(&indexType, &index))
            return false;

        NotifyCustomMod(cmod, indexType, index);
        return true;
    }

    return false;
}


bool SigParser::ParseParam()
{
    // Param ::= CustomMod* ( TYPEDBYREF | [BYREF] Type )

    NotifyBeginParam();

    if (!ParseOptionalCustomMods())
        return false;

    if (pbCur >= pbEnd)
        return false;

    if (*pbCur == ELEMENT_TYPE_TYPEDBYREF)
    {
        NotifyTypedByref();
        pbCur++;
        goto Success;
    }

    if (*pbCur == ELEMENT_TYPE_BYREF)
    {
        NotifyByref();
        pbCur++;
    }

    if (!ParseType())
        return false;

Success:
    NotifyEndParam();
    return true;
}


bool SigParser::ParseRetType()
{
    // RetType ::= CustomMod* ( VOID | TYPEDBYREF | [BYREF] Type )

    NotifyBeginRetType();

    if (!ParseOptionalCustomMods())
        return false;

    if (pbCur >= pbEnd)
        return false;

    if (*pbCur == ELEMENT_TYPE_TYPEDBYREF)
    {
        NotifyTypedByref();
        pbCur++;
        goto Success;
    }

    if (*pbCur == ELEMENT_TYPE_VOID)
    {
        NotifyVoid();
        pbCur++;
        goto Success;
    }

    if (*pbCur == ELEMENT_TYPE_BYREF)
    {
        NotifyByref();
        pbCur++;
    }

    if (!ParseType())
        return false;

Success:
    NotifyEndRetType();
    return true;
}

bool SigParser::ParseArrayShape()
{
    sig_count rank;
    sig_count numsizes;
    sig_count size;

    // ArrayShape ::= Rank NumSizes Size* NumLoBounds LoBound*
    NotifyBeginArrayShape();
    if (!ParseNumber(&rank))
        return false;

    NotifyRank(rank);

    if (!ParseNumber(&numsizes))
        return false;

    NotifyNumSizes(numsizes);

    for (sig_count i = 0; i < numsizes; i++)
    {
        if (!ParseNumber(&size))
            return false;

        NotifySize(size);
    }

    if (!ParseNumber(&numsizes))
        return false;

    NotifyNumLoBounds(numsizes);

    for (sig_count i = 0; i < numsizes; i++)
    {
        if (!ParseNumber(&size))
            return false;

        NotifyLoBound(size);
    }

    NotifyEndArrayShape();
    return true;
}

bool SigParser::ParseType()
{
    /*
    Type ::= ( BOOLEAN | CHAR | I1 | U1 | U2 | U2 | I4 | U4 | I8 | U8 | R4 | R8 | I | U |
                    | VALUETYPE TypeDefOrRefEncoded
                    | CLASS TypeDefOrRefEncoded
                    | STRING
                    | OBJECT
                    | PTR CustomMod* VOID
                    | PTR CustomMod* Type
                    | FNPTR MethodDefSig
                    | FNPTR MethodRefSig
                    | ARRAY Type ArrayShape
                    | SZARRAY CustomMod* Type
                    | GENERICINST (CLASS | VALUETYPE) TypeDefOrRefEncoded GenArgCount Type *
                    | VAR Number
                    | MVAR Number

    */

    NotifyBeginType();

    sig_elem_type elem_type;
    sig_index index;
    sig_mem_number number;
    sig_index_type indexType;

    if (!ParseByte(&elem_type))
        return false;

    switch (elem_type)
    {
    case  ELEMENT_TYPE_BOOLEAN:
    case  ELEMENT_TYPE_CHAR:
    case  ELEMENT_TYPE_I1:
    case  ELEMENT_TYPE_U1:
    case  ELEMENT_TYPE_U2:
    case  ELEMENT_TYPE_I2:
    case  ELEMENT_TYPE_I4:
    case  ELEMENT_TYPE_U4:
    case  ELEMENT_TYPE_I8:
    case  ELEMENT_TYPE_U8:
    case  ELEMENT_TYPE_R4:
    case  ELEMENT_TYPE_R8:
    case  ELEMENT_TYPE_I:
    case  ELEMENT_TYPE_U:
    case  ELEMENT_TYPE_STRING:
    case  ELEMENT_TYPE_OBJECT:
        // simple types
        NotifyTypeSimple(elem_type);
        break;

    case  ELEMENT_TYPE_PTR:
        // PTR CustomMod* VOID
        // PTR CustomMod* Type

        NotifyTypePointer();

        if (!ParseOptionalCustomMods())
            return false;

        if (pbCur >= pbEnd)
            return false;

    if (*pbCur == ELEMENT_TYPE_VOID)
        {
            pbCur++;
            NotifyVoid();
            break;
        }

        if (!ParseType())
            return false;

        break;

    case  ELEMENT_TYPE_CLASS:
        // CLASS TypeDefOrRefEncoded
        NotifyTypeClass();

        if (!ParseTypeDefOrRefEncoded(&indexType, &index))
            return false;

        NotifyTypeDefOrRef(indexType, index);
        break;

    case  ELEMENT_TYPE_VALUETYPE:
        //VALUETYPE TypeDefOrRefEncoded
        NotifyTypeValueType();

        if (!ParseTypeDefOrRefEncoded(&indexType, &index))
            return false;

        NotifyTypeDefOrRef(indexType, index);
        break;

    case  ELEMENT_TYPE_FNPTR:
        // FNPTR MethodDefSig
        // FNPTR MethodRefSig
        NotifyTypeFunctionPointer();

        if (!ParseByte(&elem_type))
            return false;

        if (!ParseMethod(elem_type))
            return false;

        break;

    case  ELEMENT_TYPE_ARRAY:
        // ARRAY Type ArrayShape
        NotifyTypeArray();

        if (!ParseType())
            return false;

        if (!ParseArrayShape())
            return false;
        break;

    case  ELEMENT_TYPE_SZARRAY:
        // SZARRAY CustomMod* Type

        NotifyTypeSzArray();

        if (!ParseOptionalCustomMods())
            return false;

        if (!ParseType())
            return false;

        break;

    case  ELEMENT_TYPE_GENERICINST:
        // GENERICINST (CLASS | VALUETYPE) TypeDefOrRefEncoded GenArgCount Type *

        if (!ParseByte(&elem_type))
            return false;

        if (elem_type != ELEMENT_TYPE_CLASS && elem_type != ELEMENT_TYPE_VALUETYPE)
            return false;

        if (!ParseTypeDefOrRefEncoded(&indexType, &index))
            return false;

        if (!ParseNumber(&number))
            return false;

        NotifyTypeGenericInst(elem_type, indexType, index, number);

        {
            for (sig_mem_number i=0; i < number; i++)
            {
                if (!ParseType())
                    return false;
            }
        }

        break;

    case  ELEMENT_TYPE_VAR:
        // VAR Number
        if (!ParseNumber(&number))
            return false;
        NotifyTypeGenericTypeVariable(number);
        break;

    case  ELEMENT_TYPE_MVAR:
        // MVAR Number
        if (!ParseNumber(&number))
            return false;
        NotifyTypeGenericMemberVariable(number);
        break;
    }

    NotifyEndType();

    return true;
}

bool SigParser::ParseTypeDefOrRefEncoded(sig_index_type *pIndexTypeOut, sig_index *pIndexOut)
{
    // parse an encoded typedef or typeref

    sig_count encoded  = 0;

    if (!ParseNumber(&encoded))
        return false;

    *pIndexTypeOut = (sig_index_type) (encoded & 0x3);
    *pIndexOut = (encoded >> 2);
    return true;
}

bool SigParser::ParseNumber(sig_count *pOut)
{
    // parse the variable length number format (0-4 bytes)

    sig_byte b1 = 0, b2 = 0, b3 = 0, b4 = 0;

    // at least one byte in the encoding, read that

    if (!ParseByte(&b1))
        return false;

    if (b1 == 0xff)
    {
        // special encoding of 'NULL'
        // not sure what this means as a number, don't expect to see it except for string lengths
        // which we don't encounter anyway so calling it an error
        return false;
    }

    // early out on 1 byte encoding
    if ( (b1 & 0x80) == 0)
    {
        *pOut = (int)b1;
        return true;
    }

    // now at least 2 bytes in the encoding, read 2nd byte
    if (!ParseByte(&b2))
        return false;

    // early out on 2 byte encoding
    if ( (b1 & 0x40) == 0)
    {
        *pOut = (((b1 & 0x3f) << 8) | b2);
        return true;
    }

    // must be a 4 byte encoding

    if ( (b1 & 0x20) != 0)
    {
        // 4 byte encoding has this bit clear -- error if not
        return false;
    }

    if (!ParseByte(&b3))
        return false;

    if (!ParseByte(&b4))
        return false;

    *pOut = ((b1 & 0x1f)<<24) | (b2<<16) | (b3<<8) | b4;
    return true;
}
