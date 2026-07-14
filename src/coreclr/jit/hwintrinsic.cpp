// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "jitpch.h"
#include "hwintrinsic.h"

#ifdef FEATURE_HW_INTRINSICS

static const HWIntrinsicInfo hwIntrinsicInfoArray[] = {
// clang-format off
#define HARDWARE_INTRINSIC(isa, name, simdSize, numArgs, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, intCost, fltCost, category, flag) \
    { \
            /* name */ #name, \
           /* flags */ static_cast<HWIntrinsicFlag>(flag), \
              /* id */ NI_##isa##_##name, \
             /* ins */ {t1, t2, t3, t4, t5, t6, t7, t8, t9, t10}, \
             /* isa */ InstructionSet_##isa, \
        /* simdSize */ simdSize, \
         /* numArgs */ numArgs, \
         /* intCost */ intCost, \
         /* fltCost */ fltCost, \
        /* category */ category \
    },
#include "hwintrinsiclist.h"
    // clang-format on
};

//------------------------------------------------------------------------
// lookup: Gets the HWIntrinsicInfo associated with a given NamedIntrinsic
//
// Arguments:
//    id -- The NamedIntrinsic associated with the HWIntrinsic to lookup
//
// Return Value:
//    The HWIntrinsicInfo associated with id
const HWIntrinsicInfo& HWIntrinsicInfo::lookup(NamedIntrinsic id)
{
    assert(id != NI_Illegal);

    assert(id > NI_HW_INTRINSIC_START);
    assert(id < NI_HW_INTRINSIC_END);

    return hwIntrinsicInfoArray[id - NI_HW_INTRINSIC_START - 1];
}

//------------------------------------------------------------------------
// lookupIns: Gets the instruction associated with a given NamedIntrinsic and base type
//
// Arguments:
//    id   -- The NamedIntrinsic associated for which to lookup its instruction
//    type -- The base type for which to lookup the instruction
//    comp -- The optional compiler instance which is used to special case instruction lookup
//
// Return Value:
//    The instruction for id and type
instruction HWIntrinsicInfo::lookupIns(NamedIntrinsic id, var_types type, Compiler* comp)
{
    if ((type < TYP_BYTE) || (type > TYP_DOUBLE))
    {
        assert(!"Unexpected type");
        return INS_invalid;
    }

    uint16_t    result = lookup(id).ins[type - TYP_BYTE];
    instruction ins    = static_cast<instruction>(result);

#if defined(TARGET_X86)
    if (ins == INS_movd64)
    {
        ins = INS_movd32;
    }
#endif // TARGET_X86

#if defined(TARGET_XARCH)
    if (comp != nullptr)
    {
        instruction evexIns = ins;

        switch (ins)
        {
            case INS_movdqa32:
            {
                if (varTypeIsLong(type))
                {
                    evexIns = INS_vmovdqa64;
                }
                break;
            }

            case INS_movdqu32:
            {
                if (varTypeIsLong(type))
                {
                    evexIns = INS_vmovdqu64;
                }
                break;
            }

            case INS_pandd:
            {
                if (varTypeIsLong(type))
                {
                    evexIns = INS_vpandq;
                }
                break;
            }

            case INS_pandnd:
            {
                if (varTypeIsLong(type))
                {
                    evexIns = INS_vpandnq;
                }
                break;
            }

            case INS_pord:
            {
                if (varTypeIsLong(type))
                {
                    evexIns = INS_vporq;
                }
                break;
            }

            case INS_pxord:
            {
                if (varTypeIsLong(type))
                {
                    evexIns = INS_vpxorq;
                }
                break;
            }

            case INS_vbroadcastf32x4:
            {
                if (type == TYP_DOUBLE)
                {
                    evexIns = INS_vbroadcastf64x2;
                }
                break;
            }

            case INS_vbroadcasti32x4:
            {
                if (varTypeIsLong(type))
                {
                    evexIns = INS_vbroadcasti64x2;
                }
                break;
            }

            case INS_vextractf32x4:
            {
                if (type == TYP_DOUBLE)
                {
                    evexIns = INS_vextractf64x2;
                }
                else if (varTypeIsInt(type))
                {
                    evexIns = INS_vextracti32x4;
                }
                else if (varTypeIsLong(type))
                {
                    evexIns = INS_vextracti64x2;
                }
                break;
            }

            case INS_vextracti32x4:
            {
                if (varTypeIsLong(type))
                {
                    evexIns = INS_vextracti64x2;
                }
                break;
            }

            case INS_vinsertf32x4:
            {
                if (type == TYP_DOUBLE)
                {
                    evexIns = INS_vinsertf64x2;
                }
                else if (varTypeIsInt(type))
                {
                    evexIns = INS_vinserti32x4;
                }
                else if (varTypeIsLong(type))
                {
                    evexIns = INS_vinserti64x2;
                }
                break;
            }

            case INS_vinserti32x4:
            {
                if (varTypeIsLong(type))
                {
                    evexIns = INS_vinserti64x2;
                }
                break;
            }

            default:
            {
                break;
            }
        }

        if ((evexIns != ins) && comp->canUseEvexEncoding())
        {
            ins = evexIns;
        }
    }
#endif // TARGET_XARCH

    return ins;
}

#if defined(TARGET_XARCH)
const TernaryLogicInfo& TernaryLogicInfo::lookup(uint8_t control)
{
    // This table is 768 bytes and is about as small as we can make it.
    //
    // The way the constants work is we have three keys:
    // * A: 0xF0
    // * B: 0xCC
    // * C: 0xAA
    //
    // To compute the correct control byte, you simply perform the corresponding operation on these keys. So, if you
    // wanted to do (A & B) ^ C, you would compute (0xF0 & 0xCC) ^ 0xAA or 0x6A.
    //
    // This table allows us to compute the inverse information, that is given a control, what are the operations it
    // performs. This allows us to determine things like what operands are actually used (so we can correctly compute
    // isRMW) and what operations are performed and in what order (such that we can do constant folding in the future).
    //
    // The total set of operations supported are:
    // * true:   AllBitsSet
    // * false:  Zero
    // * not:    ~value
    // * and:    left & right
    // * nand:   ~(left & right)
    // * or:     left | right
    // * nor:    ~(left | right)
    // * xor:    left ^ right
    // * xnor:   ~(left ^ right)
    // * cndsel: a ? b : c; aka (B & A) | (C & ~A)
    // * major:  0 if two+ input bits are 0
    // * minor:  1 if two+ input bits are 0

    // clang-format off
    static const TernaryLogicInfo ternaryLogicFlags[256] = {
        /* FALSE */           { TernaryLogicOperKind::False,  TernaryLogicUseFlags::None, TernaryLogicOperKind::None,   TernaryLogicUseFlags::None, TernaryLogicOperKind::None,   TernaryLogicUseFlags::None },
        /* norABC */          { TernaryLogicOperKind::Nor,    TernaryLogicUseFlags::ABC,  TernaryLogicOperKind::None,   TernaryLogicUseFlags::None, TernaryLogicOperKind::None,   TernaryLogicUseFlags::None },
        /* andCnorBA */       { TernaryLogicOperKind::Nor,    TernaryLogicUseFlags::AB,   TernaryLogicOperKind::And,    TernaryLogicUseFlags::C,    TernaryLogicOperKind::None,   TernaryLogicUseFlags::None },
        /* norBA */           { TernaryLogicOperKind::Nor,    TernaryLogicUseFlags::AB,   TernaryLogicOperKind::None,   TernaryLogicUseFlags::None, TernaryLogicOperKind::None,   TernaryLogicUseFlags::None },
        /* andBnorAC */       { TernaryLogicOperKind::Nor,    TernaryLogicUseFlags::AC,   TernaryLogicOperKind::And,    TernaryLogicUseFlags::B,    TernaryLogicOperKind::None,   TernaryLogicUseFlags::None },
        /* norCA */           { TernaryLogicOperKind::Nor,    TernaryLogicUseFlags::AC,   TernaryLogicOperKind::None,   TernaryLogicUseFlags::None, TernaryLogicOperKind::None,   TernaryLogicUseFlags::None },
        /* norAxnorBC */      { TernaryLogicOperKind::Xnor,   TernaryLogicUseFlags::BC,   TernaryLogicOperKind::Nor,    TernaryLogicUseFlags::A,    TernaryLogicOperKind::None,   TernaryLogicUseFlags::None },
        /* norAandBC */       { TernaryLogicOperKind::And,    TernaryLogicUseFlags::BC,   TernaryLogicOperKind::Nor,    TernaryLogicUseFlags::A,    TernaryLogicOperKind::None,   TernaryLogicUseFlags::None },
        /* norAnandBC */      { TernaryLogicOperKind::Nand,   TernaryLogicUseFlags::BC,   TernaryLogicOperKind::Nor,    TernaryLogicUseFlags::A,    TernaryLogicOperKind::None,   TernaryLogicUseFlags::None },
        /* norAxorBC */       { TernaryLogicOperKind::Xor,    TernaryLogicUseFlags::BC,   TernaryLogicOperKind::Nor,    TernaryLogicUseFlags::A,    TernaryLogicOperKind::None,   TernaryLogicUseFlags::None },
        /* andC!A */          { TernaryLogicOperKind::Not,    TernaryLogicUseFlags::A,    TernaryLogicOperKind::And,    TernaryLogicUseFlags::C,    TernaryLogicOperKind::None,   TernaryLogicUseFlags::None },
        /* C?!A:norBA */      { TernaryLogicOperKind::Not,    TernaryLogicUseFlags::A,    TernaryLogicOperKind::Nor,    TernaryLogicUseFlags::AB,   TernaryLogicOperKind::Cond,   TernaryLogicUseFlags::C    },
        /* andB!A */          { TernaryLogicOperKind::Not,    TernaryLogicUseFlags::A,    TernaryLogicOperKind::And,    TernaryLogicUseFlags::B,    TernaryLogicOperKind::None,   TernaryLogicUseFlags::None },
        /* B?!A:norAC */      { TernaryLogicOperKind::Not,    TernaryLogicUseFlags::A,    TernaryLogicOperKind::Nor,    TernaryLogicUseFlags::AC,   TernaryLogicOperKind::Cond,   TernaryLogicUseFlags::B    },
        /* norAnorBC */       { TernaryLogicOperKind::Nor,    TernaryLogicUseFlags::BC,   TernaryLogicOperKind::Nor,    TernaryLogicUseFlags::A,    TernaryLogicOperKind::None,   TernaryLogicUseFlags::None },
        /* !A */              { TernaryLogicOperKind::Not,    TernaryLogicUseFlags::A,    TernaryLogicOperKind::None,   TernaryLogicUseFlags::None, TernaryLogicOperKind::None,   TernaryLogicUseFlags::None },
        /* andAnorBC */       { TernaryLogicOperKind::Nor,    TernaryLogicUseFlags::BC,   TernaryLogicOperKind::And,    TernaryLogicUseFlags::A,    TernaryLogicOperKind::None,   TernaryLogicUseFlags::None },
        /* norCB */           { TernaryLogicOperKind::Nor,    TernaryLogicUseFlags::BC,   TernaryLogicOperKind::None,   TernaryLogicUseFlags::None, TernaryLogicOperKind::None,   TernaryLogicUseFlags::None },
        /* norBxnorAC */      { TernaryLogicOperKind::Xnor,   TernaryLogicUseFlags::AC,   TernaryLogicOperKind::Nor,    TernaryLogicUseFlags::B,    TernaryLogicOperKind::None,   TernaryLogicUseFlags::None },
        /* norBandAC */       { TernaryLogicOperKind::And,    TernaryLogicUseFlags::AC,   TernaryLogicOperKind::Nor,    TernaryLogicUseFlags::B,    TernaryLogicOperKind::None,   TernaryLogicUseFlags::None },
        /* norCxnorBA */      { TernaryLogicOperKind::Xnor,   TernaryLogicUseFlags::AB,   TernaryLogicOperKind::Nor,    TernaryLogicUseFlags::C,    TernaryLogicOperKind::None,   TernaryLogicUseFlags::None },
        /* norCandBA */       { TernaryLogicOperKind::And,    TernaryLogicUseFlags::AB,   TernaryLogicOperKind::Nor,    TernaryLogicUseFlags::C,    TernaryLogicOperKind::None,   TernaryLogicUseFlags::None },
        /* A?norBC:xorBC */   { TernaryLogicOperKind::Nor,    TernaryLogicUseFlags::BC,   TernaryLogicOperKind::Xor,    TernaryLogicUseFlags::BC,   TernaryLogicOperKind::Cond,   TernaryLogicUseFlags::A    },
        /* minorABC */        { TernaryLogicOperKind::Minor,  TernaryLogicUseFlags::ABC,  TernaryLogicOperKind::None,   TernaryLogicUseFlags::None, TernaryLogicOperKind::None,   TernaryLogicUseFlags::None },
        /* A?norBC:andBC */   { TernaryLogicOperKind::Nor,    TernaryLogicUseFlags::BC,   TernaryLogicOperKind::And,    TernaryLogicUseFlags::BC,   TernaryLogicOperKind::Cond,   TernaryLogicUseFlags::A    },
        /* A?norBC:xnorBC */  { TernaryLogicOperKind::Nor,    TernaryLogicUseFlags::BC,   TernaryLogicOperKind::Xnor,   TernaryLogicUseFlags::BC,   TernaryLogicOperKind::Cond,   TernaryLogicUseFlags::A    },
        /* A?norBC:C */       { TernaryLogicOperKind::Nor,    TernaryLogicUseFlags::BC,   TernaryLogicOperKind::Select, TernaryLogicUseFlags::C,    TernaryLogicOperKind::Cond,   TernaryLogicUseFlags::A    },
        /* C?!A:!B */         { TernaryLogicOperKind::Not,    TernaryLogicUseFlags::A,    TernaryLogicOperKind::Not,    TernaryLogicUseFlags::B,    TernaryLogicOperKind::Cond,   TernaryLogicUseFlags::C    },
        /* A?norBC:B */       { TernaryLogicOperKind::Nor,    TernaryLogicUseFlags::BC,   TernaryLogicOperKind::Select, TernaryLogicUseFlags::B,    TernaryLogicOperKind::Cond,   TernaryLogicUseFlags::A    },
        /* B?!A:!C */         { TernaryLogicOperKind::Not,    TernaryLogicUseFlags::A,    TernaryLogicOperKind::Not,    TernaryLogicUseFlags::C,    TernaryLogicOperKind::Cond,   TernaryLogicUseFlags::B    },
        /* xorAorBC */        { TernaryLogicOperKind::Or,     TernaryLogicUseFlags::BC,   TernaryLogicOperKind::Xor,    TernaryLogicUseFlags::A,    TernaryLogicOperKind::None,   TernaryLogicUseFlags::None },
        /* nandAorBC */       { TernaryLogicOperKind::Or,     TernaryLogicUseFlags::BC,   TernaryLogicOperKind::Nand,   TernaryLogicUseFlags::A,    TernaryLogicOperKind::None,   TernaryLogicUseFlags::None },
        /* norBnandAC */      { TernaryLogicOperKind::Nand,   TernaryLogicUseFlags::AC,   TernaryLogicOperKind::Nor,    TernaryLogicUseFlags::B,    TernaryLogicOperKind::None,   TernaryLogicUseFlags::None },
        /* norBxorAC */       { TernaryLogicOperKind::Xor,    TernaryLogicUseFlags::AC,   TernaryLogicOperKind::Nor,    TernaryLogicUseFlags::B,    TernaryLogicOperKind::None,   TernaryLogicUseFlags::None },
        /* andC!B */          { TernaryLogicOperKind::Not,    TernaryLogicUseFlags::B,    TernaryLogicOperKind::And,    TernaryLogicUseFlags::C,    TernaryLogicOperKind::None,   TernaryLogicUseFlags::None },
        /* C?!B:norBA */      { TernaryLogicOperKind::Not,    TernaryLogicUseFlags::B,    TernaryLogicOperKind::Nor,    TernaryLogicUseFlags::AB,   TernaryLogicOperKind::Cond,   TernaryLogicUseFlags::C    },
        /* B?norAC:andAC */   { TernaryLogicOperKind::Nor,    TernaryLogicUseFlags::AC,   TernaryLogicOperKind::And,    TernaryLogicUseFlags::AC,   TernaryLogicOperKind::Cond,   TernaryLogicUseFlags::B    },
        /* B?norAC:xnorAC */  { TernaryLogicOperKind::Nor,    TernaryLogicUseFlags::AC,   TernaryLogicOperKind::Xnor,   TernaryLogicUseFlags::AC,   TernaryLogicOperKind::Cond,   TernaryLogicUseFlags::B    },
        /* B?norAC:C */       { TernaryLogicOperKind::Nor,    TernaryLogicUseFlags::AC,   TernaryLogicOperKind::Select, TernaryLogicUseFlags::C,    TernaryLogicOperKind::Cond,   TernaryLogicUseFlags::B    },
        /* C?!B:!A */         { TernaryLogicOperKind::Not,    TernaryLogicUseFlags::B,    TernaryLogicOperKind::Not,    TernaryLogicUseFlags::A,    TernaryLogicOperKind::Cond,   TernaryLogicUseFlags::C    },
        /* andCxorBA */       { TernaryLogicOperKind::Xor,    TernaryLogicUseFlags::AB,   TernaryLogicOperKind::And,    TernaryLogicUseFlags::C,    TernaryLogicOperKind::None,   TernaryLogicUseFlags::None },
        /* C?xorBA:norBA */   { TernaryLogicOperKind::Xor,    TernaryLogicUseFlags::AB,   TernaryLogicOperKind::Nor,    TernaryLogicUseFlags::AB,   TernaryLogicOperKind::Cond,   TernaryLogicUseFlags::C    },
        /* andCnandBA */      { TernaryLogicOperKind::Nand,   TernaryLogicUseFlags::AB,   TernaryLogicOperKind::And,    TernaryLogicUseFlags::C,    TernaryLogicOperKind::None,   TernaryLogicUseFlags::None },
        /* C?nandBA:norBA */  { TernaryLogicOperKind::Nand,   TernaryLogicUseFlags::AB,   TernaryLogicOperKind::Nor,    TernaryLogicUseFlags::AB,   TernaryLogicOperKind::Cond,   TernaryLogicUseFlags::C    },
        /* B?!A:andAC */      { TernaryLogicOperKind::Not,    TernaryLogicUseFlags::A,    TernaryLogicOperKind::And,    TernaryLogicUseFlags::AC,   TernaryLogicOperKind::Cond,   TernaryLogicUseFlags::B    },
        /* B?!A:xnorAC */     { TernaryLogicOperKind::Not,    TernaryLogicUseFlags::A,    TernaryLogicOperKind::Xnor,   TernaryLogicUseFlags::AC,   TernaryLogicOperKind::Cond,   TernaryLogicUseFlags::B    },
        /* B?!A:C */          { TernaryLogicOperKind::Not,    TernaryLogicUseFlags::A,    TernaryLogicOperKind::Select, TernaryLogicUseFlags::C,    TernaryLogicOperKind::Cond,   TernaryLogicUseFlags::B    },
        /* C?nandBA:!A */     { TernaryLogicOperKind::Nand,   TernaryLogicUseFlags::AB,   TernaryLogicOperKind::Not,    TernaryLogicUseFlags::A,    TernaryLogicOperKind::Cond,   TernaryLogicUseFlags::C    },
        /* andA!B */          { TernaryLogicOperKind::Not,    TernaryLogicUseFlags::B,    TernaryLogicOperKind::And,    TernaryLogicUseFlags::A,    TernaryLogicOperKind::None,   TernaryLogicUseFlags::None },
        /* A?!B:norBC */      { TernaryLogicOperKind::Not,    TernaryLogicUseFlags::B,    TernaryLogicOperKind::Nor,    TernaryLogicUseFlags::BC,   TernaryLogicOperKind::Cond,   TernaryLogicUseFlags::A    },
        /* norBnorAC */       { TernaryLogicOperKind::Nor,    TernaryLogicUseFlags::AC,   TernaryLogicOperKind::Nor,    TernaryLogicUseFlags::B,    TernaryLogicOperKind::None,   TernaryLogicUseFlags::None },
        /* !B */              { TernaryLogicOperKind::Not,    TernaryLogicUseFlags::B,    TernaryLogicOperKind::None,   TernaryLogicUseFlags::None, TernaryLogicOperKind::None,   TernaryLogicUseFlags::None },
        /* B?norAC:A */       { TernaryLogicOperKind::Nor,    TernaryLogicUseFlags::AC,   TernaryLogicOperKind::Select, TernaryLogicUseFlags::A,    TernaryLogicOperKind::Cond,   TernaryLogicUseFlags::B    },
        /* A?!B:!C */         { TernaryLogicOperKind::Not,    TernaryLogicUseFlags::B,    TernaryLogicOperKind::Not,    TernaryLogicUseFlags::C,    TernaryLogicOperKind::Cond,   TernaryLogicUseFlags::A    },
        /* xorBorAC */        { TernaryLogicOperKind::Or,     TernaryLogicUseFlags::AC,   TernaryLogicOperKind::Xor,    TernaryLogicUseFlags::B,    TernaryLogicOperKind::None,   TernaryLogicUseFlags::None },
        /* nandBorAC */       { TernaryLogicOperKind::Or,     TernaryLogicUseFlags::AC,   TernaryLogicOperKind::Nand,   TernaryLogicUseFlags::B,    TernaryLogicOperKind::None,   TernaryLogicUseFlags::None },
        /* A?!B:andBC */      { TernaryLogicOperKind::Not,    TernaryLogicUseFlags::B,    TernaryLogicOperKind::And,    TernaryLogicUseFlags::BC,   TernaryLogicOperKind::Cond,   TernaryLogicUseFlags::A    },
        /* A?!B:xnorBC */     { TernaryLogicOperKind::Not,    TernaryLogicUseFlags::B,    TernaryLogicOperKind::Xnor,   TernaryLogicUseFlags::BC,   TernaryLogicOperKind::Cond,   TernaryLogicUseFlags::A    },
        /* A?!B:C */          { TernaryLogicOperKind::Not,    TernaryLogicUseFlags::B,    TernaryLogicOperKind::Select, TernaryLogicUseFlags::C,    TernaryLogicOperKind::Cond,   TernaryLogicUseFlags::A    },
        /* C?nandBA:!B */     { TernaryLogicOperKind::Nand,   TernaryLogicUseFlags::AB,   TernaryLogicOperKind::Not,    TernaryLogicUseFlags::B,    TernaryLogicOperKind::Cond,   TernaryLogicUseFlags::C    },
        /* xorBA */           { TernaryLogicOperKind::Xor,    TernaryLogicUseFlags::AB,   TernaryLogicOperKind::None,   TernaryLogicUseFlags::None, TernaryLogicOperKind::None,   TernaryLogicUseFlags::None },
        /* C?xorBA:nandBA */  { TernaryLogicOperKind::Xor,    TernaryLogicUseFlags::AB,   TernaryLogicOperKind::Nand,   TernaryLogicUseFlags::AB,   TernaryLogicOperKind::Cond,   TernaryLogicUseFlags::C    },
        /* A?!B:orBC */       { TernaryLogicOperKind::Not,    TernaryLogicUseFlags::B,    TernaryLogicOperKind::Or,     TernaryLogicUseFlags::BC,   TernaryLogicOperKind::Cond,   TernaryLogicUseFlags::A    },
        /* nandBA */          { TernaryLogicOperKind::Nand,   TernaryLogicUseFlags::AB,   TernaryLogicOperKind::None,   TernaryLogicUseFlags::None, TernaryLogicOperKind::None,   TernaryLogicUseFlags::None },
        /* norCnandBA */      { TernaryLogicOperKind::Nand,   TernaryLogicUseFlags::AB,   TernaryLogicOperKind::Nor,    TernaryLogicUseFlags::C,    TernaryLogicOperKind::None,   TernaryLogicUseFlags::None },
        /* norCxorBA */       { TernaryLogicOperKind::Xor,    TernaryLogicUseFlags::AB,   TernaryLogicOperKind::Nor,    TernaryLogicUseFlags::C,    TernaryLogicOperKind::None,   TernaryLogicUseFlags::None },
        /* C?norBA:andBA */   { TernaryLogicOperKind::Nor,    TernaryLogicUseFlags::AB,   TernaryLogicOperKind::And,    TernaryLogicUseFlags::AB,   TernaryLogicOperKind::Cond,   TernaryLogicUseFlags::C    },
        /* C?norBA:xnorBA */  { TernaryLogicOperKind::Nor,    TernaryLogicUseFlags::AB,   TernaryLogicOperKind::Xnor,   TernaryLogicUseFlags::AB,   TernaryLogicOperKind::Cond,   TernaryLogicUseFlags::C    },
        /* andB!C */          { TernaryLogicOperKind::Not,    TernaryLogicUseFlags::C,    TernaryLogicOperKind::And,    TernaryLogicUseFlags::B,    TernaryLogicOperKind::None,   TernaryLogicUseFlags::None },
        /* B?!C:norAC */      { TernaryLogicOperKind::Not,    TernaryLogicUseFlags::C,    TernaryLogicOperKind::Nor,    TernaryLogicUseFlags::AC,   TernaryLogicOperKind::Cond,   TernaryLogicUseFlags::B    },
        /* C?norBA:B */       { TernaryLogicOperKind::Nor,    TernaryLogicUseFlags::AB,   TernaryLogicOperKind::Select, TernaryLogicUseFlags::B,    TernaryLogicOperKind::Cond,   TernaryLogicUseFlags::C    },
        /* B?!C:!A */         { TernaryLogicOperKind::Not,    TernaryLogicUseFlags::C,    TernaryLogicOperKind::Not,    TernaryLogicUseFlags::A,    TernaryLogicOperKind::Cond,   TernaryLogicUseFlags::B    },
        /* andBxorAC */       { TernaryLogicOperKind::Xor,    TernaryLogicUseFlags::AC,   TernaryLogicOperKind::And,    TernaryLogicUseFlags::B,    TernaryLogicOperKind::None,   TernaryLogicUseFlags::None },
        /* B?xorAC:norAC */   { TernaryLogicOperKind::Xor,    TernaryLogicUseFlags::AC,   TernaryLogicOperKind::Nor,    TernaryLogicUseFlags::AC,   TernaryLogicOperKind::Cond,   TernaryLogicUseFlags::B    },
        /* C?!A:andBA */      { TernaryLogicOperKind::Not,    TernaryLogicUseFlags::A,    TernaryLogicOperKind::And,    TernaryLogicUseFlags::AB,   TernaryLogicOperKind::Cond,   TernaryLogicUseFlags::C    },
        /* B?xorAC:!A */      { TernaryLogicOperKind::Xor,    TernaryLogicUseFlags::AC,   TernaryLogicOperKind::Not,    TernaryLogicUseFlags::A,    TernaryLogicOperKind::Cond,   TernaryLogicUseFlags::B    },
        /* andBnandAC */      { TernaryLogicOperKind::Nand,   TernaryLogicUseFlags::AC,   TernaryLogicOperKind::And,    TernaryLogicUseFlags::B,    TernaryLogicOperKind::None,   TernaryLogicUseFlags::None },
        /* B?nandAC:norAC */  { TernaryLogicOperKind::Nand,   TernaryLogicUseFlags::AC,   TernaryLogicOperKind::Nor,    TernaryLogicUseFlags::AC,   TernaryLogicOperKind::Cond,   TernaryLogicUseFlags::B    },
        /* C?!A:B */          { TernaryLogicOperKind::Not,    TernaryLogicUseFlags::A,    TernaryLogicOperKind::Select, TernaryLogicUseFlags::B,    TernaryLogicOperKind::Cond,   TernaryLogicUseFlags::C    },
        /* B?nandAC:!A */     { TernaryLogicOperKind::Nand,   TernaryLogicUseFlags::AC,   TernaryLogicOperKind::Not,    TernaryLogicUseFlags::A,    TernaryLogicOperKind::Cond,   TernaryLogicUseFlags::B    },
        /* andA!C */          { TernaryLogicOperKind::Not,    TernaryLogicUseFlags::C,    TernaryLogicOperKind::And,    TernaryLogicUseFlags::A,    TernaryLogicOperKind::None,   TernaryLogicUseFlags::None },
        /* A?!C:norBC */      { TernaryLogicOperKind::Not,    TernaryLogicUseFlags::C,    TernaryLogicOperKind::Nor,    TernaryLogicUseFlags::BC,   TernaryLogicOperKind::Cond,   TernaryLogicUseFlags::A    },
        /* C?norBA:A */       { TernaryLogicOperKind::Nor,    TernaryLogicUseFlags::AB,   TernaryLogicOperKind::Select, TernaryLogicUseFlags::A,    TernaryLogicOperKind::Cond,   TernaryLogicUseFlags::C    },
        /* A?!C:!B */         { TernaryLogicOperKind::Not,    TernaryLogicUseFlags::C,    TernaryLogicOperKind::Not,    TernaryLogicUseFlags::B,    TernaryLogicOperKind::Cond,   TernaryLogicUseFlags::A    },
        /* norCnorBA */       { TernaryLogicOperKind::Nor,    TernaryLogicUseFlags::AB,   TernaryLogicOperKind::Nor,    TernaryLogicUseFlags::C,    TernaryLogicOperKind::None,   TernaryLogicUseFlags::None },
        /* !C */              { TernaryLogicOperKind::Not,    TernaryLogicUseFlags::C,    TernaryLogicOperKind::None,   TernaryLogicUseFlags::None, TernaryLogicOperKind::None,   TernaryLogicUseFlags::None },
        /* xorCorBA */        { TernaryLogicOperKind::Or,     TernaryLogicUseFlags::AB,   TernaryLogicOperKind::Xor,    TernaryLogicUseFlags::C,    TernaryLogicOperKind::None,   TernaryLogicUseFlags::None },
        /* nandCorBA */       { TernaryLogicOperKind::Or,     TernaryLogicUseFlags::AB,   TernaryLogicOperKind::Nand,   TernaryLogicUseFlags::C,    TernaryLogicOperKind::None,   TernaryLogicUseFlags::None },
        /* A?!C:andBC */      { TernaryLogicOperKind::Not,    TernaryLogicUseFlags::C,    TernaryLogicOperKind::And,    TernaryLogicUseFlags::BC,   TernaryLogicOperKind::Cond,   TernaryLogicUseFlags::A    },
        /* A?!C:xnorBC */     { TernaryLogicOperKind::Not,    TernaryLogicUseFlags::C,    TernaryLogicOperKind::Xnor,   TernaryLogicUseFlags::BC,   TernaryLogicOperKind::Cond,   TernaryLogicUseFlags::A    },
        /* xorCA */           { TernaryLogicOperKind::Xor,    TernaryLogicUseFlags::AC,   TernaryLogicOperKind::None,   TernaryLogicUseFlags::None, TernaryLogicOperKind::None,   TernaryLogicUseFlags::None },
        /* B?xorAC:nandAC */  { TernaryLogicOperKind::Xor,    TernaryLogicUseFlags::AC,   TernaryLogicOperKind::Nand,   TernaryLogicUseFlags::AC,   TernaryLogicOperKind::Cond,   TernaryLogicUseFlags::B    },
        /* A?!C:B */          { TernaryLogicOperKind::Not,    TernaryLogicUseFlags::C,    TernaryLogicOperKind::Select, TernaryLogicUseFlags::B,    TernaryLogicOperKind::Cond,   TernaryLogicUseFlags::A    },
        /* B?nandAC:!C */     { TernaryLogicOperKind::Nand,   TernaryLogicUseFlags::AC,   TernaryLogicOperKind::Not,    TernaryLogicUseFlags::C,    TernaryLogicOperKind::Cond,   TernaryLogicUseFlags::B    },
        /* A?!C:orBC */       { TernaryLogicOperKind::Not,    TernaryLogicUseFlags::C,    TernaryLogicOperKind::Or,     TernaryLogicUseFlags::BC,   TernaryLogicOperKind::Cond,   TernaryLogicUseFlags::A    },
        /* nandCA */          { TernaryLogicOperKind::Nand,   TernaryLogicUseFlags::AC,   TernaryLogicOperKind::None,   TernaryLogicUseFlags::None, TernaryLogicOperKind::None,   TernaryLogicUseFlags::None },
        /* andAxorBC */       { TernaryLogicOperKind::Xor,    TernaryLogicUseFlags::BC,   TernaryLogicOperKind::And,    TernaryLogicUseFlags::A,    TernaryLogicOperKind::None,   TernaryLogicUseFlags::None },
        /* A?xorBC:norBC */   { TernaryLogicOperKind::Xor,    TernaryLogicUseFlags::BC,   TernaryLogicOperKind::Nor,    TernaryLogicUseFlags::BC,   TernaryLogicOperKind::Cond,   TernaryLogicUseFlags::A    },
        /* C?!B:andBA */      { TernaryLogicOperKind::Not,    TernaryLogicUseFlags::B,    TernaryLogicOperKind::And,    TernaryLogicUseFlags::AB,   TernaryLogicOperKind::Cond,   TernaryLogicUseFlags::C    },
        /* A?xorBC:!B */      { TernaryLogicOperKind::Xor,    TernaryLogicUseFlags::BC,   TernaryLogicOperKind::Not,    TernaryLogicUseFlags::B,    TernaryLogicOperKind::Cond,   TernaryLogicUseFlags::A    },
        /* B?!C:andAC */      { TernaryLogicOperKind::Not,    TernaryLogicUseFlags::C,    TernaryLogicOperKind::And,    TernaryLogicUseFlags::AC,   TernaryLogicOperKind::Cond,   TernaryLogicUseFlags::B    },
        /* A?xorBC:!C */      { TernaryLogicOperKind::Xor,    TernaryLogicUseFlags::BC,   TernaryLogicOperKind::Not,    TernaryLogicUseFlags::C,    TernaryLogicOperKind::Cond,   TernaryLogicUseFlags::A    },
        /* xorCB */           { TernaryLogicOperKind::Xor,    TernaryLogicUseFlags::BC,   TernaryLogicOperKind::None,   TernaryLogicUseFlags::None, TernaryLogicOperKind::None,   TernaryLogicUseFlags::None },
        /* A?xorBC:nandBC */  { TernaryLogicOperKind::Xor,    TernaryLogicUseFlags::BC,   TernaryLogicOperKind::Nand,   TernaryLogicUseFlags::BC,   TernaryLogicOperKind::Cond,   TernaryLogicUseFlags::A    },
        /* A?xorBC:andBC */   { TernaryLogicOperKind::Xor,    TernaryLogicUseFlags::BC,   TernaryLogicOperKind::And,    TernaryLogicUseFlags::BC,   TernaryLogicOperKind::Cond,   TernaryLogicUseFlags::A    },
        /* xnorABC */         { TernaryLogicOperKind::Xnor,   TernaryLogicUseFlags::ABC,  TernaryLogicOperKind::None,   TernaryLogicUseFlags::None, TernaryLogicOperKind::None,   TernaryLogicUseFlags::None },
        /* xorCandBA */       { TernaryLogicOperKind::And,    TernaryLogicUseFlags::AB,   TernaryLogicOperKind::Xor,    TernaryLogicUseFlags::C,    TernaryLogicOperKind::None,   TernaryLogicUseFlags::None },
        /* C?nandBA:xnorBA */ { TernaryLogicOperKind::Nand,   TernaryLogicUseFlags::AB,   TernaryLogicOperKind::Xnor,   TernaryLogicUseFlags::AB,   TernaryLogicOperKind::Cond,   TernaryLogicUseFlags::C    },
        /* xorBandAC */       { TernaryLogicOperKind::And,    TernaryLogicUseFlags::AC,   TernaryLogicOperKind::Xor,    TernaryLogicUseFlags::B,    TernaryLogicOperKind::None,   TernaryLogicUseFlags::None },
        /* B?nandAC:xnorAC */ { TernaryLogicOperKind::Nand,   TernaryLogicUseFlags::AC,   TernaryLogicOperKind::Xnor,   TernaryLogicUseFlags::AC,   TernaryLogicOperKind::Cond,   TernaryLogicUseFlags::B    },
        /* B?nandAC:C */      { TernaryLogicOperKind::Nand,   TernaryLogicUseFlags::AC,   TernaryLogicOperKind::Select, TernaryLogicUseFlags::C,    TernaryLogicOperKind::Cond,   TernaryLogicUseFlags::B    },
        /* nandAxnorBC */     { TernaryLogicOperKind::Xnor,   TernaryLogicUseFlags::BC,   TernaryLogicOperKind::Nand,   TernaryLogicUseFlags::A,    TernaryLogicOperKind::None,   TernaryLogicUseFlags::None },
        /* andAnandBC */      { TernaryLogicOperKind::Nand,   TernaryLogicUseFlags::BC,   TernaryLogicOperKind::And,    TernaryLogicUseFlags::A,    TernaryLogicOperKind::None,   TernaryLogicUseFlags::None },
        /* A?nandBC:norBC */  { TernaryLogicOperKind::Nand,   TernaryLogicUseFlags::BC,   TernaryLogicOperKind::Nor,    TernaryLogicUseFlags::BC,   TernaryLogicOperKind::Cond,   TernaryLogicUseFlags::A    },
        /* C?!B:A */          { TernaryLogicOperKind::Not,    TernaryLogicUseFlags::B,    TernaryLogicOperKind::Select, TernaryLogicUseFlags::A,    TernaryLogicOperKind::Cond,   TernaryLogicUseFlags::C    },
        /* A?nandBC:!B */     { TernaryLogicOperKind::Nand,   TernaryLogicUseFlags::BC,   TernaryLogicOperKind::Not,    TernaryLogicUseFlags::B,    TernaryLogicOperKind::Cond,   TernaryLogicUseFlags::A    },
        /* B?!C:A */          { TernaryLogicOperKind::Not,    TernaryLogicUseFlags::C,    TernaryLogicOperKind::Select, TernaryLogicUseFlags::A,    TernaryLogicOperKind::Cond,   TernaryLogicUseFlags::B    },
        /* A?nandBC:!C */     { TernaryLogicOperKind::Nand,   TernaryLogicUseFlags::BC,   TernaryLogicOperKind::Not,    TernaryLogicUseFlags::C,    TernaryLogicOperKind::Cond,   TernaryLogicUseFlags::A    },
        /* B?!C:orAC */       { TernaryLogicOperKind::Not,    TernaryLogicUseFlags::C,    TernaryLogicOperKind::Or,     TernaryLogicUseFlags::AC,   TernaryLogicOperKind::Cond,   TernaryLogicUseFlags::B    },
        /* nandCB */          { TernaryLogicOperKind::Nand,   TernaryLogicUseFlags::BC,   TernaryLogicOperKind::None,   TernaryLogicUseFlags::None, TernaryLogicOperKind::None,   TernaryLogicUseFlags::None },
        /* xorAandBC */       { TernaryLogicOperKind::And,    TernaryLogicUseFlags::BC,   TernaryLogicOperKind::Xor,    TernaryLogicUseFlags::A,    TernaryLogicOperKind::None,   TernaryLogicUseFlags::None },
        /* A?nandBC:xnorBC */ { TernaryLogicOperKind::Nand,   TernaryLogicUseFlags::BC,   TernaryLogicOperKind::Xnor,   TernaryLogicUseFlags::BC,   TernaryLogicOperKind::Cond,   TernaryLogicUseFlags::A    },
        /* A?nandBC:C */      { TernaryLogicOperKind::Nand,   TernaryLogicUseFlags::BC,   TernaryLogicOperKind::Select, TernaryLogicUseFlags::C,    TernaryLogicOperKind::Cond,   TernaryLogicUseFlags::A    },
        /* nandBxnorAC */     { TernaryLogicOperKind::Xnor,   TernaryLogicUseFlags::AC,   TernaryLogicOperKind::Nand,   TernaryLogicUseFlags::B,    TernaryLogicOperKind::None,   TernaryLogicUseFlags::None },
        /* A?nandBC:B */      { TernaryLogicOperKind::Nand,   TernaryLogicUseFlags::BC,   TernaryLogicOperKind::Select, TernaryLogicUseFlags::B,    TernaryLogicOperKind::Cond,   TernaryLogicUseFlags::A    },
        /* nandCxnorBA */     { TernaryLogicOperKind::Xnor,   TernaryLogicUseFlags::AB,   TernaryLogicOperKind::Nand,   TernaryLogicUseFlags::C,    TernaryLogicOperKind::None,   TernaryLogicUseFlags::None },
        /* A?nandBC:orBC */   { TernaryLogicOperKind::Nand,   TernaryLogicUseFlags::BC,   TernaryLogicOperKind::Or,     TernaryLogicUseFlags::BC,   TernaryLogicOperKind::Cond,   TernaryLogicUseFlags::A    },
        /* nandABC */         { TernaryLogicOperKind::Nand,   TernaryLogicUseFlags::ABC,  TernaryLogicOperKind::None,   TernaryLogicUseFlags::None, TernaryLogicOperKind::None,   TernaryLogicUseFlags::None },
        /* andABC */          { TernaryLogicOperKind::And,    TernaryLogicUseFlags::ABC,  TernaryLogicOperKind::None,   TernaryLogicUseFlags::None, TernaryLogicOperKind::None,   TernaryLogicUseFlags::None },
        /* A?andBC:norBC */   { TernaryLogicOperKind::And,    TernaryLogicUseFlags::BC,   TernaryLogicOperKind::Nor,    TernaryLogicUseFlags::BC,   TernaryLogicOperKind::Cond,   TernaryLogicUseFlags::A    },
        /* andCxnorBA */      { TernaryLogicOperKind::Xnor,   TernaryLogicUseFlags::AB,   TernaryLogicOperKind::And,    TernaryLogicUseFlags::C,    TernaryLogicOperKind::None,   TernaryLogicUseFlags::None },
        /* A?andBC:!B */      { TernaryLogicOperKind::And,    TernaryLogicUseFlags::BC,   TernaryLogicOperKind::Not,    TernaryLogicUseFlags::B,    TernaryLogicOperKind::Cond,   TernaryLogicUseFlags::A    },
        /* andBxnorAC */      { TernaryLogicOperKind::Xnor,   TernaryLogicUseFlags::AC,   TernaryLogicOperKind::And,    TernaryLogicUseFlags::B,    TernaryLogicOperKind::None,   TernaryLogicUseFlags::None },
        /* A?andBC:!C */      { TernaryLogicOperKind::And,    TernaryLogicUseFlags::BC,   TernaryLogicOperKind::Not,    TernaryLogicUseFlags::C,    TernaryLogicOperKind::Cond,   TernaryLogicUseFlags::A    },
        /* A?andBC:xorBC */   { TernaryLogicOperKind::And,    TernaryLogicUseFlags::BC,   TernaryLogicOperKind::Xor,    TernaryLogicUseFlags::BC,   TernaryLogicOperKind::Cond,   TernaryLogicUseFlags::A    },
        /* xnorAandBC */      { TernaryLogicOperKind::And,    TernaryLogicUseFlags::BC,   TernaryLogicOperKind::Xnor,   TernaryLogicUseFlags::A,    TernaryLogicOperKind::None,   TernaryLogicUseFlags::None },
        /* andCB */           { TernaryLogicOperKind::And,    TernaryLogicUseFlags::BC,   TernaryLogicOperKind::None,   TernaryLogicUseFlags::None, TernaryLogicOperKind::None,   TernaryLogicUseFlags::None },
        /* B?C:norAC */       { TernaryLogicOperKind::Select, TernaryLogicUseFlags::C,    TernaryLogicOperKind::Nor,    TernaryLogicUseFlags::AC,   TernaryLogicOperKind::Cond,   TernaryLogicUseFlags::B    },
        /* A?andBC:C */       { TernaryLogicOperKind::And,    TernaryLogicUseFlags::BC,   TernaryLogicOperKind::Select, TernaryLogicUseFlags::C,    TernaryLogicOperKind::Cond,   TernaryLogicUseFlags::A    },
        /* B?C:!A */          { TernaryLogicOperKind::Select, TernaryLogicUseFlags::C,    TernaryLogicOperKind::Not,    TernaryLogicUseFlags::A,    TernaryLogicOperKind::Cond,   TernaryLogicUseFlags::B    },
        /* A?andBC:B */       { TernaryLogicOperKind::And,    TernaryLogicUseFlags::BC,   TernaryLogicOperKind::Select, TernaryLogicUseFlags::B,    TernaryLogicOperKind::Cond,   TernaryLogicUseFlags::A    },
        /* C?B:!A */          { TernaryLogicOperKind::Select, TernaryLogicUseFlags::B,    TernaryLogicOperKind::Not,    TernaryLogicUseFlags::A,    TernaryLogicOperKind::Cond,   TernaryLogicUseFlags::C    },
        /* A?andBC:orBC */    { TernaryLogicOperKind::And,    TernaryLogicUseFlags::BC,   TernaryLogicOperKind::Or,     TernaryLogicUseFlags::BC,   TernaryLogicOperKind::Cond,   TernaryLogicUseFlags::A    },
        /* nandAnandBC */     { TernaryLogicOperKind::Nand,   TernaryLogicUseFlags::BC,   TernaryLogicOperKind::Nand,   TernaryLogicUseFlags::A,    TernaryLogicOperKind::None,   TernaryLogicUseFlags::None },
        /* andAxnorBC */      { TernaryLogicOperKind::Xnor,   TernaryLogicUseFlags::BC,   TernaryLogicOperKind::And,    TernaryLogicUseFlags::A,    TernaryLogicOperKind::None,   TernaryLogicUseFlags::None },
        /* B?andAC:!C */      { TernaryLogicOperKind::And,    TernaryLogicUseFlags::AC,   TernaryLogicOperKind::Not,    TernaryLogicUseFlags::C,    TernaryLogicOperKind::Cond,   TernaryLogicUseFlags::B    },
        /* B?andAC:xorAC */   { TernaryLogicOperKind::And,    TernaryLogicUseFlags::AC,   TernaryLogicOperKind::Xor,    TernaryLogicUseFlags::AC,   TernaryLogicOperKind::Cond,   TernaryLogicUseFlags::B    },
        /* xnorBandAC */      { TernaryLogicOperKind::And,    TernaryLogicUseFlags::AC,   TernaryLogicOperKind::Xnor,   TernaryLogicUseFlags::B,    TernaryLogicOperKind::None,   TernaryLogicUseFlags::None },
        /* C?andBA:xorBA */   { TernaryLogicOperKind::And,    TernaryLogicUseFlags::AB,   TernaryLogicOperKind::Xor,    TernaryLogicUseFlags::AB,   TernaryLogicOperKind::Cond,   TernaryLogicUseFlags::C    },
        /* xnorCandBA */      { TernaryLogicOperKind::And,    TernaryLogicUseFlags::AB,   TernaryLogicOperKind::Xnor,   TernaryLogicUseFlags::C,    TernaryLogicOperKind::None,   TernaryLogicUseFlags::None },
        /* xorABC */          { TernaryLogicOperKind::Xor,    TernaryLogicUseFlags::ABC,  TernaryLogicOperKind::None,   TernaryLogicUseFlags::None, TernaryLogicOperKind::None,   TernaryLogicUseFlags::None },
        /* A?xnorBC:nandBC */ { TernaryLogicOperKind::Xnor,   TernaryLogicUseFlags::BC,   TernaryLogicOperKind::Nand,   TernaryLogicUseFlags::BC,   TernaryLogicOperKind::Cond,   TernaryLogicUseFlags::A    },
        /* A?xnorBC:andBC */  { TernaryLogicOperKind::Xnor,   TernaryLogicUseFlags::BC,   TernaryLogicOperKind::And,    TernaryLogicUseFlags::BC,   TernaryLogicOperKind::Cond,   TernaryLogicUseFlags::A    },
        /* xnorCB */          { TernaryLogicOperKind::Xnor,   TernaryLogicUseFlags::BC,   TernaryLogicOperKind::None,   TernaryLogicUseFlags::None, TernaryLogicOperKind::None,   TernaryLogicUseFlags::None },
        /* A?xnorBC:C */      { TernaryLogicOperKind::Xnor,   TernaryLogicUseFlags::BC,   TernaryLogicOperKind::Select, TernaryLogicUseFlags::C,    TernaryLogicOperKind::Cond,   TernaryLogicUseFlags::A    },
        /* B?C:nandAC */      { TernaryLogicOperKind::Select, TernaryLogicUseFlags::C,    TernaryLogicOperKind::Nand,   TernaryLogicUseFlags::AC,   TernaryLogicOperKind::Cond,   TernaryLogicUseFlags::B    },
        /* A?xnorBC:B */      { TernaryLogicOperKind::Xnor,   TernaryLogicUseFlags::BC,   TernaryLogicOperKind::Select, TernaryLogicUseFlags::B,    TernaryLogicOperKind::Cond,   TernaryLogicUseFlags::A    },
        /* C?B:nandBA */      { TernaryLogicOperKind::Select, TernaryLogicUseFlags::B,    TernaryLogicOperKind::Nand,   TernaryLogicUseFlags::AB,   TernaryLogicOperKind::Cond,   TernaryLogicUseFlags::C    },
        /* A?xnorBC:orBC */   { TernaryLogicOperKind::Xnor,   TernaryLogicUseFlags::BC,   TernaryLogicOperKind::Or,     TernaryLogicUseFlags::BC,   TernaryLogicOperKind::Cond,   TernaryLogicUseFlags::A    },
        /* nandAxorBC */      { TernaryLogicOperKind::Xor,    TernaryLogicUseFlags::BC,   TernaryLogicOperKind::Nand,   TernaryLogicUseFlags::A,    TernaryLogicOperKind::None,   TernaryLogicUseFlags::None },
        /* andCA */           { TernaryLogicOperKind::And,    TernaryLogicUseFlags::AC,   TernaryLogicOperKind::None,   TernaryLogicUseFlags::None, TernaryLogicOperKind::None,   TernaryLogicUseFlags::None },
        /* A?C:norBC */       { TernaryLogicOperKind::Select, TernaryLogicUseFlags::C,    TernaryLogicOperKind::Nor,    TernaryLogicUseFlags::BC,   TernaryLogicOperKind::Cond,   TernaryLogicUseFlags::A    },
        /* B?andAC:C */       { TernaryLogicOperKind::And,    TernaryLogicUseFlags::AC,   TernaryLogicOperKind::Select, TernaryLogicUseFlags::C,    TernaryLogicOperKind::Cond,   TernaryLogicUseFlags::B    },
        /* A?C:!B */          { TernaryLogicOperKind::Select, TernaryLogicUseFlags::C,    TernaryLogicOperKind::Not,    TernaryLogicUseFlags::B,    TernaryLogicOperKind::Cond,   TernaryLogicUseFlags::A    },
        /* B?xnorAC:andAC */  { TernaryLogicOperKind::Xnor,   TernaryLogicUseFlags::AC,   TernaryLogicOperKind::And,    TernaryLogicUseFlags::AC,   TernaryLogicOperKind::Cond,   TernaryLogicUseFlags::B    },
        /* xnorCA */          { TernaryLogicOperKind::Xnor,   TernaryLogicUseFlags::AC,   TernaryLogicOperKind::None,   TernaryLogicUseFlags::None, TernaryLogicOperKind::None,   TernaryLogicUseFlags::None },
        /* A?C:xorBC */       { TernaryLogicOperKind::Select, TernaryLogicUseFlags::C,    TernaryLogicOperKind::Xor,    TernaryLogicUseFlags::BC,   TernaryLogicOperKind::Cond,   TernaryLogicUseFlags::A    },
        /* A?C:nandBC */      { TernaryLogicOperKind::Select, TernaryLogicUseFlags::C,    TernaryLogicOperKind::Nand,   TernaryLogicUseFlags::BC,   TernaryLogicOperKind::Cond,   TernaryLogicUseFlags::A    },
        /* andCorAB */        { TernaryLogicOperKind::Or,     TernaryLogicUseFlags::AB,   TernaryLogicOperKind::And,    TernaryLogicUseFlags::C,    TernaryLogicOperKind::None,   TernaryLogicUseFlags::None },
        /* xnorCorBA */       { TernaryLogicOperKind::Or,     TernaryLogicUseFlags::AB,   TernaryLogicOperKind::Xnor,   TernaryLogicUseFlags::C,    TernaryLogicOperKind::None,   TernaryLogicUseFlags::None },
        /* C */               { TernaryLogicOperKind::Select, TernaryLogicUseFlags::C,    TernaryLogicOperKind::None,   TernaryLogicUseFlags::None, TernaryLogicOperKind::None,   TernaryLogicUseFlags::None },
        /* orCnorBA */        { TernaryLogicOperKind::Nor,    TernaryLogicUseFlags::AB,   TernaryLogicOperKind::Or,     TernaryLogicUseFlags::C,    TernaryLogicOperKind::None,   TernaryLogicUseFlags::None },
        /* A?C:B */           { TernaryLogicOperKind::Select, TernaryLogicUseFlags::C,    TernaryLogicOperKind::Select, TernaryLogicUseFlags::B,    TernaryLogicOperKind::Cond,   TernaryLogicUseFlags::A    },
        /* C?orBA:!A */       { TernaryLogicOperKind::Or,     TernaryLogicUseFlags::AB,   TernaryLogicOperKind::Not,    TernaryLogicUseFlags::A,    TernaryLogicOperKind::Cond,   TernaryLogicUseFlags::C    },
        /* A?C:orBC */        { TernaryLogicOperKind::Select, TernaryLogicUseFlags::C,    TernaryLogicOperKind::Or,     TernaryLogicUseFlags::BC,   TernaryLogicOperKind::Cond,   TernaryLogicUseFlags::A    },
        /* orC!A */           { TernaryLogicOperKind::Not,    TernaryLogicUseFlags::A,    TernaryLogicOperKind::Or,     TernaryLogicUseFlags::C,    TernaryLogicOperKind::None,   TernaryLogicUseFlags::None },
        /* B?andAC:A */       { TernaryLogicOperKind::And,    TernaryLogicUseFlags::AC,   TernaryLogicOperKind::Select, TernaryLogicUseFlags::A,    TernaryLogicOperKind::Cond,   TernaryLogicUseFlags::B    },
        /* C?A:!B */          { TernaryLogicOperKind::Select, TernaryLogicUseFlags::A,    TernaryLogicOperKind::Not,    TernaryLogicUseFlags::B,    TernaryLogicOperKind::Cond,   TernaryLogicUseFlags::C    },
        /* B?andAC:orAC */    { TernaryLogicOperKind::And,    TernaryLogicUseFlags::AC,   TernaryLogicOperKind::Or,     TernaryLogicUseFlags::AC,   TernaryLogicOperKind::Cond,   TernaryLogicUseFlags::B    },
        /* nandBnandAC */     { TernaryLogicOperKind::Nand,   TernaryLogicUseFlags::AC,   TernaryLogicOperKind::Nand,   TernaryLogicUseFlags::B,    TernaryLogicOperKind::None,   TernaryLogicUseFlags::None },
        /* B?xnorAC:A */      { TernaryLogicOperKind::Xnor,   TernaryLogicUseFlags::AC,   TernaryLogicOperKind::Select, TernaryLogicUseFlags::A,    TernaryLogicOperKind::Cond,   TernaryLogicUseFlags::B    },
        /* C?A:nandBA */      { TernaryLogicOperKind::Select, TernaryLogicUseFlags::A,    TernaryLogicOperKind::Nand,   TernaryLogicUseFlags::AB,   TernaryLogicOperKind::Cond,   TernaryLogicUseFlags::C    },
        /* B?xnorAC:orAC */   { TernaryLogicOperKind::Xnor,   TernaryLogicUseFlags::AC,   TernaryLogicOperKind::Or,     TernaryLogicUseFlags::AC,   TernaryLogicOperKind::Cond,   TernaryLogicUseFlags::B    },
        /* nandBxorAC */      { TernaryLogicOperKind::Xor,    TernaryLogicUseFlags::AC,   TernaryLogicOperKind::Nand,   TernaryLogicUseFlags::B,    TernaryLogicOperKind::None,   TernaryLogicUseFlags::None },
        /* B?C:A */           { TernaryLogicOperKind::Select, TernaryLogicUseFlags::C,    TernaryLogicOperKind::Select, TernaryLogicUseFlags::A,    TernaryLogicOperKind::Cond,   TernaryLogicUseFlags::B    },
        /* C?orBA:!B */       { TernaryLogicOperKind::Or,     TernaryLogicUseFlags::AB,   TernaryLogicOperKind::Not,    TernaryLogicUseFlags::B,    TernaryLogicOperKind::Cond,   TernaryLogicUseFlags::C    },
        /* B?C:orAC */        { TernaryLogicOperKind::Select, TernaryLogicUseFlags::C,    TernaryLogicOperKind::Or,     TernaryLogicUseFlags::AC,   TernaryLogicOperKind::Cond,   TernaryLogicUseFlags::B    },
        /* orC!B */           { TernaryLogicOperKind::Not,    TernaryLogicUseFlags::B,    TernaryLogicOperKind::Or,     TernaryLogicUseFlags::C,    TernaryLogicOperKind::None,   TernaryLogicUseFlags::None },
        /* C?orBA:xorBA */    { TernaryLogicOperKind::Or,     TernaryLogicUseFlags::AB,   TernaryLogicOperKind::Xor,    TernaryLogicUseFlags::AB,   TernaryLogicOperKind::Cond,   TernaryLogicUseFlags::C    },
        /* C?orBA:nandBA */   { TernaryLogicOperKind::Or,     TernaryLogicUseFlags::AB,   TernaryLogicOperKind::Nand,   TernaryLogicUseFlags::AB,   TernaryLogicOperKind::Cond,   TernaryLogicUseFlags::C    },
        /* orCxorBA */        { TernaryLogicOperKind::Xor,    TernaryLogicUseFlags::AB,   TernaryLogicOperKind::Or,     TernaryLogicUseFlags::C,    TernaryLogicOperKind::None,   TernaryLogicUseFlags::None },
        /* orCnandBA */       { TernaryLogicOperKind::Nand,   TernaryLogicUseFlags::AB,   TernaryLogicOperKind::Or,     TernaryLogicUseFlags::C,    TernaryLogicOperKind::None,   TernaryLogicUseFlags::None },
        /* andBA */           { TernaryLogicOperKind::And,    TernaryLogicUseFlags::AB,   TernaryLogicOperKind::None,   TernaryLogicUseFlags::None, TernaryLogicOperKind::None,   TernaryLogicUseFlags::None },
        /* A?B:norBC */       { TernaryLogicOperKind::Select, TernaryLogicUseFlags::B,    TernaryLogicOperKind::Nor,    TernaryLogicUseFlags::BC,   TernaryLogicOperKind::Cond,   TernaryLogicUseFlags::A    },
        /* C?xnorBA:andBA */  { TernaryLogicOperKind::Xnor,   TernaryLogicUseFlags::AB,   TernaryLogicOperKind::And,    TernaryLogicUseFlags::AB,   TernaryLogicOperKind::Cond,   TernaryLogicUseFlags::C    },
        /* xnorBA */          { TernaryLogicOperKind::Xnor,   TernaryLogicUseFlags::AB,   TernaryLogicOperKind::None,   TernaryLogicUseFlags::None, TernaryLogicOperKind::None,   TernaryLogicUseFlags::None },
        /* C?andBA:B */       { TernaryLogicOperKind::And,    TernaryLogicUseFlags::AB,   TernaryLogicOperKind::Select, TernaryLogicUseFlags::B,    TernaryLogicOperKind::Cond,   TernaryLogicUseFlags::C    },
        /* A?B:!C */          { TernaryLogicOperKind::Select, TernaryLogicUseFlags::B,    TernaryLogicOperKind::Not,    TernaryLogicUseFlags::C,    TernaryLogicOperKind::Cond,   TernaryLogicUseFlags::A    },
        /* A?B:xorBC */       { TernaryLogicOperKind::Select, TernaryLogicUseFlags::B,    TernaryLogicOperKind::Xor,    TernaryLogicUseFlags::BC,   TernaryLogicOperKind::Cond,   TernaryLogicUseFlags::A    },
        /* A?B:nandBC */      { TernaryLogicOperKind::Select, TernaryLogicUseFlags::B,    TernaryLogicOperKind::Nand,   TernaryLogicUseFlags::BC,   TernaryLogicOperKind::Cond,   TernaryLogicUseFlags::A    },
        /* andBorAC */        { TernaryLogicOperKind::Or,     TernaryLogicUseFlags::AC,   TernaryLogicOperKind::And,    TernaryLogicUseFlags::B,    TernaryLogicOperKind::None,   TernaryLogicUseFlags::None },
        /* xnorBorAC */       { TernaryLogicOperKind::Or,     TernaryLogicUseFlags::AC,   TernaryLogicOperKind::Xnor,   TernaryLogicUseFlags::B,    TernaryLogicOperKind::None,   TernaryLogicUseFlags::None },
        /* A?B:C */           { TernaryLogicOperKind::Select, TernaryLogicUseFlags::B,    TernaryLogicOperKind::Select, TernaryLogicUseFlags::C,    TernaryLogicOperKind::Cond,   TernaryLogicUseFlags::A    },
        /* B?orAC:!A */       { TernaryLogicOperKind::Or,     TernaryLogicUseFlags::AC,   TernaryLogicOperKind::Not,    TernaryLogicUseFlags::A,    TernaryLogicOperKind::Cond,   TernaryLogicUseFlags::B    },
        /* B */               { TernaryLogicOperKind::Select, TernaryLogicUseFlags::B,    TernaryLogicOperKind::None,   TernaryLogicUseFlags::None, TernaryLogicOperKind::None,   TernaryLogicUseFlags::None },
        /* orBnorAC */        { TernaryLogicOperKind::Nor,    TernaryLogicUseFlags::AC,   TernaryLogicOperKind::Or,     TernaryLogicUseFlags::B,    TernaryLogicOperKind::None,   TernaryLogicUseFlags::None },
        /* A?B:orBC */        { TernaryLogicOperKind::Select, TernaryLogicUseFlags::B,    TernaryLogicOperKind::Or,     TernaryLogicUseFlags::BC,   TernaryLogicOperKind::Cond,   TernaryLogicUseFlags::A    },
        /* orB!A */           { TernaryLogicOperKind::Not,    TernaryLogicUseFlags::A,    TernaryLogicOperKind::Or,     TernaryLogicUseFlags::B,    TernaryLogicOperKind::None,   TernaryLogicUseFlags::None },
        /* C?andBA:A */       { TernaryLogicOperKind::And,    TernaryLogicUseFlags::AB,   TernaryLogicOperKind::Select, TernaryLogicUseFlags::A,    TernaryLogicOperKind::Cond,   TernaryLogicUseFlags::C    },
        /* B?A:!C */          { TernaryLogicOperKind::Select, TernaryLogicUseFlags::A,    TernaryLogicOperKind::Not,    TernaryLogicUseFlags::C,    TernaryLogicOperKind::Cond,   TernaryLogicUseFlags::B    },
        /* B?A:xorAC */       { TernaryLogicOperKind::Select, TernaryLogicUseFlags::A,    TernaryLogicOperKind::Xor,    TernaryLogicUseFlags::AC,   TernaryLogicOperKind::Cond,   TernaryLogicUseFlags::B    },
        /* B?A:nandAC */      { TernaryLogicOperKind::Select, TernaryLogicUseFlags::A,    TernaryLogicOperKind::Nand,   TernaryLogicUseFlags::AC,   TernaryLogicOperKind::Cond,   TernaryLogicUseFlags::B    },
        /* C?andBA:orBA */    { TernaryLogicOperKind::And,    TernaryLogicUseFlags::AB,   TernaryLogicOperKind::Or,     TernaryLogicUseFlags::AB,   TernaryLogicOperKind::Cond,   TernaryLogicUseFlags::C    },
        /* nandCnandBA */     { TernaryLogicOperKind::Nand,   TernaryLogicUseFlags::AB,   TernaryLogicOperKind::Nand,   TernaryLogicUseFlags::C,    TernaryLogicOperKind::None,   TernaryLogicUseFlags::None },
        /* C?xnorBA:orBA */   { TernaryLogicOperKind::Xnor,   TernaryLogicUseFlags::AB,   TernaryLogicOperKind::Or,     TernaryLogicUseFlags::AB,   TernaryLogicOperKind::Cond,   TernaryLogicUseFlags::C    },
        /* nandCxorBA */      { TernaryLogicOperKind::Xor,    TernaryLogicUseFlags::AB,   TernaryLogicOperKind::Nand,   TernaryLogicUseFlags::C,    TernaryLogicOperKind::None,   TernaryLogicUseFlags::None },
        /* C?B:A */           { TernaryLogicOperKind::Select, TernaryLogicUseFlags::B,    TernaryLogicOperKind::Select, TernaryLogicUseFlags::A,    TernaryLogicOperKind::Cond,   TernaryLogicUseFlags::C    },
        /* B?orAC:!C */       { TernaryLogicOperKind::Or,     TernaryLogicUseFlags::AC,   TernaryLogicOperKind::Not,    TernaryLogicUseFlags::C,    TernaryLogicOperKind::Cond,   TernaryLogicUseFlags::B    },
        /* B?orAC:xorAC */    { TernaryLogicOperKind::Or,     TernaryLogicUseFlags::AC,   TernaryLogicOperKind::Xor,    TernaryLogicUseFlags::AC,   TernaryLogicOperKind::Cond,   TernaryLogicUseFlags::B    },
        /* B?orAC:nandAC */   { TernaryLogicOperKind::Or,     TernaryLogicUseFlags::AC,   TernaryLogicOperKind::Nand,   TernaryLogicUseFlags::AC,   TernaryLogicOperKind::Cond,   TernaryLogicUseFlags::B    },
        /* C?B:orBA */        { TernaryLogicOperKind::Select, TernaryLogicUseFlags::B,    TernaryLogicOperKind::Or,     TernaryLogicUseFlags::AB,   TernaryLogicOperKind::Cond,   TernaryLogicUseFlags::C    },
        /* orB!C */           { TernaryLogicOperKind::Not,    TernaryLogicUseFlags::C,    TernaryLogicOperKind::Or,     TernaryLogicUseFlags::B,    TernaryLogicOperKind::None,   TernaryLogicUseFlags::None },
        /* orBxorAC */        { TernaryLogicOperKind::Xor,    TernaryLogicUseFlags::AC,   TernaryLogicOperKind::Or,     TernaryLogicUseFlags::B,    TernaryLogicOperKind::None,   TernaryLogicUseFlags::None },
        /* orBnandAC */       { TernaryLogicOperKind::Nand,   TernaryLogicUseFlags::AC,   TernaryLogicOperKind::Or,     TernaryLogicUseFlags::B,    TernaryLogicOperKind::None,   TernaryLogicUseFlags::None },
        /* andAorBC */        { TernaryLogicOperKind::Or,     TernaryLogicUseFlags::BC,   TernaryLogicOperKind::And,    TernaryLogicUseFlags::A,    TernaryLogicOperKind::None,   TernaryLogicUseFlags::None },
        /* xnorAorBC */       { TernaryLogicOperKind::Or,     TernaryLogicUseFlags::BC,   TernaryLogicOperKind::Xnor,   TernaryLogicUseFlags::A,    TernaryLogicOperKind::None,   TernaryLogicUseFlags::None },
        /* B?A:C */           { TernaryLogicOperKind::Select, TernaryLogicUseFlags::A,    TernaryLogicOperKind::Select, TernaryLogicUseFlags::C,    TernaryLogicOperKind::Cond,   TernaryLogicUseFlags::B    },
        /* A?orBC:!B */       { TernaryLogicOperKind::Or,     TernaryLogicUseFlags::BC,   TernaryLogicOperKind::Not,    TernaryLogicUseFlags::B,    TernaryLogicOperKind::Cond,   TernaryLogicUseFlags::A    },
        /* C?A:B */           { TernaryLogicOperKind::Select, TernaryLogicUseFlags::A,    TernaryLogicOperKind::Select, TernaryLogicUseFlags::B,    TernaryLogicOperKind::Cond,   TernaryLogicUseFlags::C    },
        /* A?orBC:!C */       { TernaryLogicOperKind::Or,     TernaryLogicUseFlags::BC,   TernaryLogicOperKind::Not,    TernaryLogicUseFlags::C,    TernaryLogicOperKind::Cond,   TernaryLogicUseFlags::A    },
        /* A?orBC:xorBC */    { TernaryLogicOperKind::Or,     TernaryLogicUseFlags::BC,   TernaryLogicOperKind::Xor,    TernaryLogicUseFlags::BC,   TernaryLogicOperKind::Cond,   TernaryLogicUseFlags::A    },
        /* A?orBC:nandBC */   { TernaryLogicOperKind::Or,     TernaryLogicUseFlags::BC,   TernaryLogicOperKind::Nand,   TernaryLogicUseFlags::BC,   TernaryLogicOperKind::Cond,   TernaryLogicUseFlags::A    },
        /* majorABC */        { TernaryLogicOperKind::Major,  TernaryLogicUseFlags::ABC,  TernaryLogicOperKind::None,   TernaryLogicUseFlags::None, TernaryLogicOperKind::None,   TernaryLogicUseFlags::None },
        /* A?orBC:xnorBC */   { TernaryLogicOperKind::Or,     TernaryLogicUseFlags::BC,   TernaryLogicOperKind::Xnor,   TernaryLogicUseFlags::BC,   TernaryLogicOperKind::Cond,   TernaryLogicUseFlags::A    },
        /* orCandBA */        { TernaryLogicOperKind::And,    TernaryLogicUseFlags::AB,   TernaryLogicOperKind::Or,     TernaryLogicUseFlags::C,    TernaryLogicOperKind::None,   TernaryLogicUseFlags::None },
        /* orCxnorBA */       { TernaryLogicOperKind::Xnor,   TernaryLogicUseFlags::AB,   TernaryLogicOperKind::Or,     TernaryLogicUseFlags::C,    TernaryLogicOperKind::None,   TernaryLogicUseFlags::None },
        /* orBandAC */        { TernaryLogicOperKind::And,    TernaryLogicUseFlags::AC,   TernaryLogicOperKind::Or,     TernaryLogicUseFlags::B,    TernaryLogicOperKind::None,   TernaryLogicUseFlags::None },
        /* orBxnorAC */       { TernaryLogicOperKind::Xnor,   TernaryLogicUseFlags::AC,   TernaryLogicOperKind::Or,     TernaryLogicUseFlags::B,    TernaryLogicOperKind::None,   TernaryLogicUseFlags::None },
        /* orCB */            { TernaryLogicOperKind::Or,     TernaryLogicUseFlags::BC,   TernaryLogicOperKind::None,   TernaryLogicUseFlags::None, TernaryLogicOperKind::None,   TernaryLogicUseFlags::None },
        /* nandAnorBC */      { TernaryLogicOperKind::Nor,    TernaryLogicUseFlags::BC,   TernaryLogicOperKind::Nand,   TernaryLogicUseFlags::A,    TernaryLogicOperKind::None,   TernaryLogicUseFlags::None },
        /* A */               { TernaryLogicOperKind::Select, TernaryLogicUseFlags::A,    TernaryLogicOperKind::None,   TernaryLogicUseFlags::None, TernaryLogicOperKind::None,   TernaryLogicUseFlags::None },
        /* orAnorBC */        { TernaryLogicOperKind::Nor,    TernaryLogicUseFlags::BC,   TernaryLogicOperKind::Or,     TernaryLogicUseFlags::A,    TernaryLogicOperKind::None,   TernaryLogicUseFlags::None },
        /* B?A:orAC */        { TernaryLogicOperKind::Select, TernaryLogicUseFlags::A,    TernaryLogicOperKind::Or,     TernaryLogicUseFlags::AC,   TernaryLogicOperKind::Cond,   TernaryLogicUseFlags::B    },
        /* orA!B */           { TernaryLogicOperKind::Not,    TernaryLogicUseFlags::B,    TernaryLogicOperKind::Or,     TernaryLogicUseFlags::A,    TernaryLogicOperKind::None,   TernaryLogicUseFlags::None },
        /* C?A:orBA */        { TernaryLogicOperKind::Select, TernaryLogicUseFlags::A,    TernaryLogicOperKind::Or,     TernaryLogicUseFlags::AB,   TernaryLogicOperKind::Cond,   TernaryLogicUseFlags::C    },
        /* orA!C */           { TernaryLogicOperKind::Not,    TernaryLogicUseFlags::C,    TernaryLogicOperKind::Or,     TernaryLogicUseFlags::A,    TernaryLogicOperKind::None,   TernaryLogicUseFlags::None },
        /* orAxorBC */        { TernaryLogicOperKind::Xor,    TernaryLogicUseFlags::BC,   TernaryLogicOperKind::Or,     TernaryLogicUseFlags::A,    TernaryLogicOperKind::None,   TernaryLogicUseFlags::None },
        /* orAnandBC */       { TernaryLogicOperKind::Nand,   TernaryLogicUseFlags::BC,   TernaryLogicOperKind::Or,     TernaryLogicUseFlags::A,    TernaryLogicOperKind::None,   TernaryLogicUseFlags::None },
        /* orAandBC */        { TernaryLogicOperKind::And,    TernaryLogicUseFlags::BC,   TernaryLogicOperKind::Or,     TernaryLogicUseFlags::A,    TernaryLogicOperKind::None,   TernaryLogicUseFlags::None },
        /* orAxnorBC */       { TernaryLogicOperKind::Xnor,   TernaryLogicUseFlags::BC,   TernaryLogicOperKind::Or,     TernaryLogicUseFlags::A,    TernaryLogicOperKind::None,   TernaryLogicUseFlags::None },
        /* orCA */            { TernaryLogicOperKind::Or,     TernaryLogicUseFlags::AC,   TernaryLogicOperKind::None,   TernaryLogicUseFlags::None, TernaryLogicOperKind::None,   TernaryLogicUseFlags::None },
        /* nandBnorAC */      { TernaryLogicOperKind::Nor,    TernaryLogicUseFlags::AC,   TernaryLogicOperKind::Nand,   TernaryLogicUseFlags::B,    TernaryLogicOperKind::None,   TernaryLogicUseFlags::None },
        /* orBA */            { TernaryLogicOperKind::Or,     TernaryLogicUseFlags::AB,   TernaryLogicOperKind::None,   TernaryLogicUseFlags::None, TernaryLogicOperKind::None,   TernaryLogicUseFlags::None },
        /* nandCnorBA */      { TernaryLogicOperKind::Nor,    TernaryLogicUseFlags::AB,   TernaryLogicOperKind::Nand,   TernaryLogicUseFlags::C,    TernaryLogicOperKind::None,   TernaryLogicUseFlags::None },
        /* orABC */           { TernaryLogicOperKind::Or,     TernaryLogicUseFlags::ABC,  TernaryLogicOperKind::None,   TernaryLogicUseFlags::None, TernaryLogicOperKind::None,   TernaryLogicUseFlags::None },
        /* TRUE */            { TernaryLogicOperKind::True,   TernaryLogicUseFlags::None, TernaryLogicOperKind::None,   TernaryLogicUseFlags::None, TernaryLogicOperKind::None,   TernaryLogicUseFlags::None },
    };
    // clang-format on

    return ternaryLogicFlags[control];
}

//------------------------------------------------------------------------
// GetTernaryControlByte: Get the control byte for a TernaryLogic operation
//   given the oper and two existing control bytes
//
// Arguments:
//    oper -- the operation being performed
//    op1  -- the control byte for op1
//    op2  -- the control byte for op2
//
// Return Value:
//    The new control byte evaluated from performing oper on op1 and op2
//
uint8_t TernaryLogicInfo::GetTernaryControlByte(genTreeOps oper, uint8_t op1, uint8_t op2)
{
    switch (oper)
    {
        case GT_AND:
        {
            return static_cast<uint8_t>(op1 & op2);
        }

        case GT_AND_NOT:
        {
            return static_cast<uint8_t>(~op1 & op2);
        }

        case GT_OR:
        {
            return static_cast<uint8_t>(op1 | op2);
        }

        case GT_XOR:
        {
            return static_cast<uint8_t>(op1 ^ op2);
        }

        default:
        {
            unreached();
        }
    }
}

//------------------------------------------------------------------------
// GetTernaryControlByte: Get the control byte for a TernaryLogic operation
//   given a ternary logic oper and two inputs
//
// Arguments:
//    oper -- the operation being performed
//    op1  -- the control byte for op1, this is ignored for unary oper
//    op2  -- the control byte for op2
//
// Return Value:
//    The new control byte evaluated from performing oper on op1 and op2
//
uint8_t TernaryLogicInfo::GetTernaryControlByte(TernaryLogicOperKind oper, uint8_t op1, uint8_t op2)
{
    switch (oper)
    {
        case TernaryLogicOperKind::Select:
        {
            return op2;
        }

        case TernaryLogicOperKind::Not:
        {
            return ~op2;
        }

        case TernaryLogicOperKind::And:
        {
            return op1 & op2;
        }

        case TernaryLogicOperKind::Nand:
        {
            return ~(op1 & op2);
        }

        case TernaryLogicOperKind::Or:
        {
            return op1 | op2;
        }

        case TernaryLogicOperKind::Nor:
        {
            return ~(op1 | op2);
        }

        case TernaryLogicOperKind::Xor:
        {
            return op1 ^ op2;
        }

        case TernaryLogicOperKind::Xnor:
        {
            return ~(op1 ^ op2);
        }

        default:
        {
            unreached();
        }
    }
}

//------------------------------------------------------------------------
// GetTernaryControlByte: Get the control byte for a TernaryLogic operation
//   given an existing info and three control bytes
//
// Arguments:
//    info -- the info describing the operation being performed
//    op1  -- the control byte for op1
//    op2  -- the control byte for op2
//    op3  -- the control byte for op3
//
// Return Value:
//    The new control byte evaluated from performing info on op1, op2, and op3
//
uint8_t TernaryLogicInfo::GetTernaryControlByte(const TernaryLogicInfo& info, uint8_t op1, uint8_t op2, uint8_t op3)
{
    uint8_t oper1Result;

    switch (info.oper1Use)
    {
        case TernaryLogicUseFlags::None:
        {
            assert(info.oper2 == TernaryLogicOperKind::None);
            assert(info.oper2Use == TernaryLogicUseFlags::None);

            assert(info.oper3 == TernaryLogicOperKind::None);
            assert(info.oper3Use == TernaryLogicUseFlags::None);

            switch (info.oper1)
            {
                case TernaryLogicOperKind::False:
                {
                    oper1Result = 0x00;
                    break;
                }

                case TernaryLogicOperKind::True:
                {
                    oper1Result = 0xFF;
                    break;
                }

                default:
                {
                    unreached();
                }
            }
            break;
        }

        case TernaryLogicUseFlags::A:
        {
            oper1Result = GetTernaryControlByte(info.oper1, 0x00, op1);
            break;
        }

        case TernaryLogicUseFlags::B:
        {
            oper1Result = GetTernaryControlByte(info.oper1, 0x00, op2);
            break;
        }

        case TernaryLogicUseFlags::C:
        {
            oper1Result = GetTernaryControlByte(info.oper1, 0x00, op3);
            break;
        }

        case TernaryLogicUseFlags::AB:
        {
            oper1Result = GetTernaryControlByte(info.oper1, op1, op2);
            break;
        }

        case TernaryLogicUseFlags::AC:
        {
            oper1Result = GetTernaryControlByte(info.oper1, op1, op3);
            break;
        }

        case TernaryLogicUseFlags::BC:
        {
            oper1Result = GetTernaryControlByte(info.oper1, op2, op3);
            break;
        }

        case TernaryLogicUseFlags::ABC:
        {
            assert(info.oper2 == TernaryLogicOperKind::None);
            assert(info.oper2Use == TernaryLogicUseFlags::None);

            assert(info.oper3 == TernaryLogicOperKind::None);
            assert(info.oper3Use == TernaryLogicUseFlags::None);

            switch (info.oper1)
            {
                case TernaryLogicOperKind::Nor:
                {
                    oper1Result = ~(op1 | op2 | op3);
                    break;
                }

                case TernaryLogicOperKind::Minor:
                {
                    oper1Result = 0x17;
                    break;
                }

                case TernaryLogicOperKind::Xnor:
                {
                    oper1Result = ~(op1 ^ op2 ^ op3);
                    break;
                }

                case TernaryLogicOperKind::Nand:
                {
                    oper1Result = ~(op1 & op2 & op3);
                    break;
                }

                case TernaryLogicOperKind::And:
                {
                    oper1Result = op1 & op2 & op3;
                    break;
                }

                case TernaryLogicOperKind::Xor:
                {
                    oper1Result = op1 ^ op2 ^ op3;
                    break;
                }

                case TernaryLogicOperKind::Major:
                {
                    oper1Result = 0xE8;
                    break;
                }

                case TernaryLogicOperKind::Or:
                {
                    oper1Result = op1 | op2 | op3;
                    break;
                }

                default:
                {
                    unreached();
                }
            }
            break;
        }

        default:
        {
            unreached();
        }
    }

    uint8_t oper2Result;

    switch (info.oper2Use)
    {
        case TernaryLogicUseFlags::None:
        {
            assert(info.oper3 == TernaryLogicOperKind::None);
            assert(info.oper3Use == TernaryLogicUseFlags::None);

            oper2Result = oper1Result;
            break;
        }

        case TernaryLogicUseFlags::A:
        {
            oper2Result = GetTernaryControlByte(info.oper2, oper1Result, op1);
            break;
        }

        case TernaryLogicUseFlags::B:
        {
            oper2Result = GetTernaryControlByte(info.oper2, oper1Result, op2);
            break;
        }

        case TernaryLogicUseFlags::C:
        {
            oper2Result = GetTernaryControlByte(info.oper2, oper1Result, op3);
            break;
        }

        case TernaryLogicUseFlags::AB:
        {
            oper2Result = GetTernaryControlByte(info.oper2, op1, op2);
            break;
        }

        case TernaryLogicUseFlags::AC:
        {
            oper2Result = GetTernaryControlByte(info.oper2, op1, op3);
            break;
        }

        case TernaryLogicUseFlags::BC:
        {
            oper2Result = GetTernaryControlByte(info.oper2, op2, op3);
            break;
        }

        default:
        {
            unreached();
        }
    }

    uint8_t oper3Result;

    switch (info.oper3Use)
    {
        case TernaryLogicUseFlags::None:
        {
            assert(info.oper3 == TernaryLogicOperKind::None);
            oper3Result = oper2Result;
            break;
        }

        case TernaryLogicUseFlags::A:
        {
            assert(info.oper3 == TernaryLogicOperKind::Cond);
            oper3Result = (oper1Result & op1) | (oper2Result & ~op1);
            break;
        }

        case TernaryLogicUseFlags::B:
        {
            assert(info.oper3 == TernaryLogicOperKind::Cond);
            oper3Result = (oper1Result & op2) | (oper2Result & ~op2);
            break;
        }

        case TernaryLogicUseFlags::C:
        {
            assert(info.oper3 == TernaryLogicOperKind::Cond);
            oper3Result = (oper1Result & op3) | (oper2Result & ~op3);
            break;
        }

        default:
        {
            unreached();
        }
    }

    return oper3Result;
}
#endif // TARGET_XARCH

//------------------------------------------------------------------------
// getBaseJitTypeFromArgIfNeeded: Get simdBaseJitType of intrinsic from 1st or 2nd argument depending on the flag
//
// Arguments:
//    intrinsic       -- id of the intrinsic function.
//    method          -- method handle of the intrinsic function.
//    sig             -- signature of the intrinsic call.
//    simdBaseJitType -- Predetermined simdBaseJitType, could be CORINFO_TYPE_UNDEF
//
// Return Value:
//    The basetype of intrinsic of it can be fetched from 1st or 2nd argument, else return baseType unmodified.
//
var_types Compiler::getBaseTypeFromArgIfNeeded(NamedIntrinsic intrinsic, CORINFO_SIG_INFO* sig, var_types simdBaseType)
{
    if (HWIntrinsicInfo::BaseTypeFromSecondArg(intrinsic) || HWIntrinsicInfo::BaseTypeFromFirstArg(intrinsic))
    {
        CORINFO_ARG_LIST_HANDLE arg = sig->args;

        if (HWIntrinsicInfo::BaseTypeFromSecondArg(intrinsic))
        {
            arg = info.compCompHnd->getArgNext(arg);
        }

        CORINFO_CLASS_HANDLE argClass = info.compCompHnd->getArgClass(sig, arg);
        simdBaseType                  = getBaseTypeAndSizeOfSIMDType(argClass);

        if (simdBaseType == TYP_UNDEF) // the argument is not a vector
        {
            CORINFO_CLASS_HANDLE tmpClass;
            CorInfoType          simdBaseJitType = strip(info.compCompHnd->getArgType(sig, arg, &tmpClass));

            if (simdBaseJitType == CORINFO_TYPE_PTR)
            {
                simdBaseJitType = info.compCompHnd->getChildType(argClass, &tmpClass);
            }

            simdBaseType = JitType2PreciseVarType(simdBaseJitType);
        }
        assert(simdBaseType != TYP_UNDEF);
    }

    return simdBaseType;
}

struct HWIntrinsicIsaRange
{
    NamedIntrinsic FirstId;
    NamedIntrinsic LastId;
};

static const HWIntrinsicIsaRange hwintrinsicIsaRangeArray[] = {
// clang-format off
#if defined(TARGET_XARCH)
    { FIRST_NI_X86Base, LAST_NI_X86Base },                      // X86Base
    { FIRST_NI_AVX, LAST_NI_AVX },                              // AVX
    { FIRST_NI_AVX2, LAST_NI_AVX2 },                            // AVX2
    { FIRST_NI_AVX512, LAST_NI_AVX512 },                        // AVX512
    { FIRST_NI_AVX512v2, LAST_NI_AVX512v2 },                    // AVX512v2
    { FIRST_NI_AVX512v3, LAST_NI_AVX512v3 },                    // AVX512v3
    { NI_Illegal, NI_Illegal },                                 //      AVX10v1
    { FIRST_NI_AVX10v2, LAST_NI_AVX10v2 },                      // AVX10v2
    { NI_Illegal, NI_Illegal },                                 //      APX
    { FIRST_NI_AES, LAST_NI_AES },                              // AES
    { FIRST_NI_AES_V256, LAST_NI_AES_V256 },                    // AES_V256
    { FIRST_NI_AES_V512, LAST_NI_AES_V512 },                    // AES_V512
    { NI_Illegal, NI_Illegal },                                 //      AVX512VP2INTERSECT
    { NI_Illegal, NI_Illegal },                                 //      AVXIFMA
    { FIRST_NI_AVXVNNI, LAST_NI_AVXVNNI },                      // AVXVNNI
    { FIRST_NI_AVX512BMM, LAST_NI_AVX512BMM },                  // AVX512BMM
    { FIRST_NI_GFNI, LAST_NI_GFNI },                            // GFNI
    { FIRST_NI_GFNI_V256, LAST_NI_GFNI_V256 },                  // GFNI_V256
    { FIRST_NI_GFNI_V512, LAST_NI_GFNI_V512 },                  // GFNI_V512
    { NI_Illegal, NI_Illegal },                                 //      SHA
    { NI_Illegal, NI_Illegal },                                 //      WAITPKG
    { FIRST_NI_X86Serialize, LAST_NI_X86Serialize },            // X86Serialize
    { FIRST_NI_Vector, LAST_NI_Vector },                        // Vector128
    { FIRST_NI_Vector, LAST_NI_Vector },                        // Vector256
    { FIRST_NI_Vector, LAST_NI_Vector },                        // Vector512
    { NI_Illegal, NI_Illegal },                                 //      VectorT128
    { NI_Illegal, NI_Illegal },                                 //      VectorT256
    { NI_Illegal, NI_Illegal },                                 //      VectorT512
    { FIRST_NI_AVXVNNIINT, LAST_NI_AVXVNNIINT },                // AVXVNNIINT
    { FIRST_NI_AVXVNNIINT_V512, LAST_NI_AVXVNNIINT_V512 },      // AVXVNNIINT_V512

    { FIRST_NI_X86Base_X64, LAST_NI_X86Base_X64 },              // X86Base_X64
    { NI_Illegal, NI_Illegal },                                 //      AVX_X64
    { FIRST_NI_AVX2_X64, LAST_NI_AVX2_X64 },                    // AVX2_X64
    { FIRST_NI_AVX512_X64, LAST_NI_AVX512_X64 },                // AVX512_X64
    { NI_Illegal, NI_Illegal },                                 //      AVX512v2_X64
    { NI_Illegal, NI_Illegal },                                 //      AVX512v3_X64
    { NI_Illegal, NI_Illegal },                                 //      AVX10v1_X64
    { FIRST_NI_AVX10v2_X64, LAST_NI_AVX10v2_X64 },              // AVX10v2_X64
    { NI_Illegal, NI_Illegal },                                 //      AES_X64
    { NI_Illegal, NI_Illegal },                                 //      AVX512VP2INTERSECT_X64
    { NI_Illegal, NI_Illegal },                                 //      AVXIFMA_X64
    { NI_Illegal, NI_Illegal },                                 //      AVXVNNI_X64
    { NI_Illegal, NI_Illegal },                                 //      GFNI_X64
    { NI_Illegal, NI_Illegal },                                 //      SHA_X64
    { NI_Illegal, NI_Illegal },                                 //      WAITPKG_X64
    { NI_Illegal, NI_Illegal },                                 //      X86Serialize_X64
#elif defined (TARGET_ARM64)
    { FIRST_NI_ArmBase, LAST_NI_ArmBase },                      // ArmBase
    { FIRST_NI_AdvSimd, LAST_NI_AdvSimd },                      // AdvSimd
    { FIRST_NI_Aes, LAST_NI_Aes },                              // Aes
    { FIRST_NI_Crc32, LAST_NI_Crc32 },                          // Crc32
    { FIRST_NI_Dp, LAST_NI_Dp },                                // Dp
    { FIRST_NI_Rdm, LAST_NI_Rdm },                              // Rdm
    { FIRST_NI_Sha1, LAST_NI_Sha1 },                            // Sha1
    { FIRST_NI_Sha256, LAST_NI_Sha256 },                        // Sha256
    { NI_Illegal, NI_Illegal },                                 //      Atomics
    { FIRST_NI_Vector, LAST_NI_Vector },                        // Vector64
    { FIRST_NI_Vector, LAST_NI_Vector },                        // Vector128
    { NI_Illegal, NI_Illegal },                                 //      VectorT
    { NI_Illegal, NI_Illegal },                                 //      Dczva
    { NI_Illegal, NI_Illegal },                                 //      Rcpc
    { NI_Illegal, NI_Illegal },                                 //      VectorT128
    { NI_Illegal, NI_Illegal },                                 //      Rcpc2
    { FIRST_NI_Sve, LAST_NI_Sve },                              // Sve
    { FIRST_NI_Sve2, LAST_NI_Sve2 },                            // Sve2
    { FIRST_NI_Sha3, LAST_NI_Sha3 },                            // Sha3
    { NI_Illegal, NI_Illegal },                                 //      Sm4
    { NI_Illegal, NI_Illegal },                                 //      SveAes
    { FIRST_NI_SveSha3, LAST_NI_SveSha3 },                      // SveSha3
    { NI_Illegal, NI_Illegal },                                 //      SveSm4
    { NI_Illegal, NI_Illegal },                                 //      Cssc
    { FIRST_NI_ArmBase_Arm64, LAST_NI_ArmBase_Arm64 },          // ArmBase_Arm64
    { FIRST_NI_AdvSimd_Arm64, LAST_NI_AdvSimd_Arm64 },          // AdvSimd_Arm64
    { NI_Illegal, NI_Illegal },                                 //      Aes_Arm64
    { FIRST_NI_Crc32_Arm64, LAST_NI_Crc32_Arm64 },              // Crc32_Arm64
    { NI_Illegal, NI_Illegal },                                 //      Dp_Arm64
    { FIRST_NI_Rdm_Arm64, LAST_NI_Rdm_Arm64 },                  // Rdm_Arm64
    { NI_Illegal, NI_Illegal },                                 //      Sha1_Arm64
    { NI_Illegal, NI_Illegal },                                 //      Sha256_Arm64
    { NI_Illegal, NI_Illegal },                                 //      Sve_Arm64
    { NI_Illegal, NI_Illegal },                                 //      Sve2_Arm64
    { NI_Illegal, NI_Illegal },                                 //      Sha3_Arm64
    { NI_Illegal, NI_Illegal },                                 //      Sm4_Arm64
    { NI_Illegal, NI_Illegal },                                 //      SveAes_Arm64
    { NI_Illegal, NI_Illegal },                                 //      SveSha3_Arm64
    { NI_Illegal, NI_Illegal },                                 //      SveSm4_Arm64
#elif defined(TARGET_WASM)
    { NI_Illegal, NI_Illegal },                                 //      WasmBase
    { FIRST_NI_PackedSimd, LAST_NI_PackedSimd },                // PackedSimd
    { FIRST_NI_Vector, LAST_NI_Vector },                        // Vector128
#else
#error Unsupported platform
#endif
    // clang-format on
};

#if defined(DEBUG)
static void ValidateHWIntrinsicInfo(CORINFO_InstructionSet isa, NamedIntrinsic ni, const HWIntrinsicInfo& info)
{
    // We should have found the entry we expected to find here
    assert(info.id == ni);

    // It should belong to the expected ISA
    assert(info.isa == isa);

    if ((info.simdSize != -1) && (info.simdSize != 0))
    {
        // We should only have known SIMD sizes
#if defined(TARGET_ARM64)
        assert((info.simdSize == 8) || (info.simdSize == 16));
#elif defined(TARGET_XARCH)
        assert((info.simdSize == 16) || (info.simdSize == 32) || (info.simdSize == 64));
#elif defined(TARGET_WASM)
        assert(info.simdSize == 16);
#else
        unreached();
#endif
    }

    if (info.numArgs != -1)
    {
        // We should only have an expected number of arguments
#if defined(TARGET_ARM64) || defined(TARGET_XARCH) || defined(TARGET_WASM)
        assert((info.numArgs >= 0) && (info.numArgs <= 5));
#else
        unreached();
#endif
    }

    // TODO: There's more we could validate here in terms of flags, instructions used, etc.
    // Some of this is already done ad-hoc elsewhere throughout the JIT
}

static void ValidateHWIntrinsicIsaRange(CORINFO_InstructionSet isa, const HWIntrinsicIsaRange& isaRange)
{
    // Wasm: keep validation enabled once ISA ranges and lists are properly defined/sorted.
    // Both entries should be illegal if either is
    if (isaRange.FirstId == NI_Illegal)
    {
        assert(isaRange.LastId == NI_Illegal);
        return;
    }
    assert(isaRange.LastId != NI_Illegal);

#if defined(TARGET_XARCH)
    if ((isa == InstructionSet_Vector128) || (isa == InstructionSet_Vector256) || (isa == InstructionSet_Vector512))
    {
        isa = InstructionSet_Vector;
    }
#elif defined(TARGET_ARM64)
    if ((isa == InstructionSet_Vector64) || (isa == InstructionSet_Vector128) || (isa == InstructionSet_VectorT))
    {
        isa = InstructionSet_Vector;
    }
#elif defined(TARGET_WASM)
    if (isa == InstructionSet_Vector128)
    {
        isa = InstructionSet_Vector;
    }
#else
#error Unsupported platform
#endif

    // Both entries should belong to the expected ISA
    assert(HWIntrinsicInfo::lookupIsa(isaRange.FirstId) == isa);
    assert(HWIntrinsicInfo::lookupIsa(isaRange.LastId) == isa);

    // The last ID should be the same as or after the first ID
    assert(isaRange.FirstId <= isaRange.LastId);

    // The ID before the range should not be part of the expected ISA
    NamedIntrinsic prevId = static_cast<NamedIntrinsic>(isaRange.FirstId - 1);
    assert((prevId == NI_HW_INTRINSIC_START) || (HWIntrinsicInfo::lookupIsa(prevId) != isa));

    // The ID after the range should not be part of the expected ISA
    NamedIntrinsic nextId = static_cast<NamedIntrinsic>(isaRange.LastId + 1);
#if defined(TARGET_ARM64)
    assert((nextId == NI_HW_INTRINSIC_END) || (HWIntrinsicInfo::lookupIsa(nextId) != isa) ||
           (nextId == SPECIAL_NI_Sve));
#else
    assert((nextId == NI_HW_INTRINSIC_END) || (HWIntrinsicInfo::lookupIsa(nextId) != isa));
#endif

    NamedIntrinsic         ni       = static_cast<NamedIntrinsic>(isaRange.FirstId);
    const HWIntrinsicInfo* prevInfo = &HWIntrinsicInfo::lookup(ni);
    ValidateHWIntrinsicInfo(isa, ni, *prevInfo);

    size_t count = (isaRange.LastId - isaRange.FirstId) + 1;

    for (size_t i = 1; i < count; i++)
    {
        ni                          = static_cast<NamedIntrinsic>(isaRange.FirstId + i);
        const HWIntrinsicInfo* info = &HWIntrinsicInfo::lookup(ni);
        ValidateHWIntrinsicInfo(isa, ni, *info);

        // The current name should be sorted after the previous
        assert(strcmp(info->name, prevInfo->name) > 0);

        prevInfo = info;
    }
}

static void ValidateHWIntrinsicIsaRangeArray()
{
    for (size_t i = 0; i < ARRAY_SIZE(hwintrinsicIsaRangeArray); i++)
    {
        CORINFO_InstructionSet isa = static_cast<CORINFO_InstructionSet>(i + 1);
        ValidateHWIntrinsicIsaRange(isa, hwintrinsicIsaRangeArray[i]);
    }
}
#endif

//------------------------------------------------------------------------
// binarySearchId: Does a binary search through a given ISA for the NamedIntrinsic matching a given name
//
// Arguments:
//    isa        -- The instruction set to search
//    sig        -- The signature of the intrinsic
//    methodName -- The name of the method associated with the HWIntrinsic to lookup
//
// Return Value:
//    The NamedIntrinsic associated with methodName and isa
static NamedIntrinsic binarySearchId(CORINFO_InstructionSet isa, CORINFO_SIG_INFO* sig, const char* methodName)
{
    size_t isaIndex = static_cast<size_t>(isa) - 1;
    assert(isaIndex < ARRAY_SIZE(hwintrinsicIsaRangeArray));

    const HWIntrinsicIsaRange& isaRange = hwintrinsicIsaRangeArray[isaIndex];

    if (isaRange.FirstId == NI_Illegal)
    {
        return NI_Illegal;
    }

    size_t rangeLower = isaRange.FirstId;
    size_t rangeUpper = isaRange.LastId;

    while (rangeLower <= rangeUpper)
    {
        // This is safe since rangeLower and rangeUpper will never be negative
        size_t rangeIndex = (rangeUpper + rangeLower) / 2;

        NamedIntrinsic         ni            = static_cast<NamedIntrinsic>(rangeIndex);
        const HWIntrinsicInfo& intrinsicInfo = HWIntrinsicInfo::lookup(ni);

        int sortOrder = strcmp(methodName, intrinsicInfo.name);

        if (sortOrder < 0)
        {
            rangeUpper = rangeIndex - 1;
        }
        else if (sortOrder > 0)
        {
            rangeLower = rangeIndex + 1;
        }
        else
        {
            assert(sortOrder == 0);
            assert((intrinsicInfo.numArgs == -1) || (sig->numArgs == static_cast<uint8_t>(intrinsicInfo.numArgs)));
            return ni;
        }
    }

    // There are several helper intrinsics that are implemented in managed code
    // Those intrinsics will hit this code path and need to return NI_Illegal
    return NI_Illegal;
}

//------------------------------------------------------------------------
// lookupId: Gets the NamedIntrinsic for a given method name and InstructionSet
//
// Arguments:
//    comp                    -- The compiler
//    sig                     -- The signature of the intrinsic
//    className               -- The name of the class associated with the HWIntrinsic to lookup
//    methodName              -- The name of the method associated with the HWIntrinsic to lookup
//    innerEnclosingClassName -- The name of the inner enclosing class of nested 64-bit classes
//    outerEnclosingClassName -- The name of the outer enclosing class of nested 64-bit classes
//    isXplatIntrinsic        -- True if the intrinsic lives directly under the cross-platform
//                               System.Runtime.Intrinsics (or System.Numerics) namespace and
//                               therefore has a managed fallback when the underlying ISA isn't
//                               available; false if it is platform-specific (under
//                               System.Runtime.Intrinsics.<Arch>) and must throw a
//                               PlatformNotSupportedException when the ISA isn't available.
//
// Return Value:
//    The NamedIntrinsic associated with methodName and isa
NamedIntrinsic HWIntrinsicInfo::lookupId(Compiler*         comp,
                                         CORINFO_SIG_INFO* sig,
                                         const char*       className,
                                         const char*       methodName,
                                         const char*       innerEnclosingClassName,
                                         const char*       outerEnclosingClassName,
                                         bool              isXplatIntrinsic)
{
#if defined(DEBUG)
    static bool validationCompleted = false;

    if (!validationCompleted)
    {
        ValidateHWIntrinsicIsaRangeArray();
        validationCompleted = true;
    }
#endif // DEBUG

    // Signatures that have a 'this' parameter are illegal intrinsics.
    if (sig->hasThis())
    {
        return NI_Illegal;
    }

    CORINFO_InstructionSet isa = comp->lookupIsa(className, innerEnclosingClassName, outerEnclosingClassName);

    if (isa == InstructionSet_ILLEGAL)
    {
        return NI_Illegal;
    }

    bool     isHWIntrinsicEnabled      = (JitConfig.EnableHWIntrinsic() != 0);
    bool     isIsaSupported            = isHWIntrinsicEnabled && comp->compSupportsHWIntrinsic(isa);
    bool     isHardwareAcceleratedProp = false;
    bool     isSupportedProp           = false;
    uint32_t vectorByteLength          = 0;

    if (strncmp(methodName, "get_Is", 6) == 0)
    {
        if (strcmp(methodName + 6, "HardwareAccelerated") == 0)
        {
            isHardwareAcceleratedProp = true;
        }
        else if (strcmp(methodName + 6, "Supported") == 0)
        {
            isSupportedProp = true;
        }
    }

#ifdef TARGET_XARCH
    if (isHardwareAcceleratedProp)
    {
        // Special case: Some of Vector128/256 APIs are hardware accelerated with Sse1 and Avx1,
        // but we want IsHardwareAccelerated to return true only when all of them are (there are
        // still can be cases where e.g. Sse41 might give an additional boost for Vector128, but it's
        // not important enough to bump the minimal Sse version here)

        if (isa == InstructionSet_Vector128)
        {
            isa              = InstructionSet_X86Base;
            vectorByteLength = 16;
        }
        else if (isa == InstructionSet_Vector256)
        {
            isa              = InstructionSet_AVX2;
            vectorByteLength = 32;
        }
        else if (isa == InstructionSet_Vector512)
        {
            isa              = InstructionSet_AVX512;
            vectorByteLength = 64;
        }
        else
        {
            assert((strcmp(className, "Vector128") != 0) && (strcmp(className, "Vector256") != 0) &&
                   (strcmp(className, "Vector512") != 0));
        }
    }
#endif

    if (isSupportedProp && (strncmp(className, "Vector", 6) == 0))
    {
        // The Vector*<T>.IsSupported props report if T is supported & is specially handled in lookupNamedIntrinsic
        return NI_Illegal;
    }

    if (isSupportedProp || isHardwareAcceleratedProp)
    {
        // The `compSupportsHWIntrinsic` above validates `compSupportsIsa` indicating
        // that the compiler can emit instructions for the ISA but not whether the
        // hardware supports them.
        //
        // The `compExactlyDependsOn` on call then validates that the target hardware
        // supports the instruction. Normally this is the same ISA as we just checked
        // but for Vector128/256 on xarch this can be a different ISA since we accelerate
        // some APIs even when we can't accelerate all APIs.
        //
        // When the target hardware does support the instruction set, we can return a
        // constant true. When it doesn't then we want to report the check as dynamically
        // supported instead if the opportunistic support does exist. This allows some targets,
        // such as AOT, to emit a check against a cached CPU query so lightup can still happen
        // (such as for SSE4.1 when the target hardware is SSE2).
        //
        // When the compiler doesn't support ISA or when it does but the target hardware does
        // not and we aren't in a scenario with support for a dynamic check, we want to return false.

        if (isIsaSupported && comp->compSupportsHWIntrinsic(isa) &&
            (vectorByteLength <= comp->getPreferredVectorByteLength()))
        {
            if (!comp->IsTargetAbi(CORINFO_NATIVEAOT_ABI) || comp->compExactlyDependsOn(isa))
            {
                return NI_IsSupported_True;
            }
            else if (isSupportedProp)
            {
                assert(comp->IsTargetAbi(CORINFO_NATIVEAOT_ABI));
                return NI_IsSupported_Dynamic;
            }
        }

        return NI_IsSupported_False;
    }
    else if (!isIsaSupported)
    {
        // We only want to surface a hard PlatformNotSupportedException when the ISA is
        // unsupported AND there is no viable managed fallback. That is only the case for
        // the platform-specific APIs under System.Runtime.Intrinsics.<Arch> (e.g.,
        // Avx512.LoadVector512). The cross-platform APIs in System.Runtime.Intrinsics
        // (e.g., Vector512.Load, Vector128<T>.AsByte) and System.Numerics (e.g.,
        // Vector<T>) all have a managed fallback that should be used when the underlying
        // ISA isn't available. For those, we fall through to the per-ISA handling below
        // which returns NI_Illegal, allowing the call to be treated as a normal managed
        // method (so the body can be inlined / executed normally).

        if (!isXplatIntrinsic)
        {
            return NI_Throw_PlatformNotSupportedException;
        }
        else
        {
            return NI_Illegal;
        }
    }

    // Special case: For Vector64/128/256 we currently don't accelerate any of the methods when
    // IsHardwareAccelerated reports false. For Vector64 and Vector128 this is when the baseline
    // ISA is unsupported. For Vector256 this is when AVX2 is unsupported since integer types
    // can't get properly accelerated.

    if (isa == InstructionSet_Vector128)
    {
        if (!isHWIntrinsicEnabled)
        {
            return NI_Illegal;
        }
    }
#if defined(TARGET_XARCH)
    else if (isa == InstructionSet_Vector256)
    {
        if (!comp->compOpportunisticallyDependsOn(InstructionSet_AVX))
        {
            return NI_Illegal;
        }
    }
    else if (isa == InstructionSet_Vector512)
    {
        if (!comp->compOpportunisticallyDependsOn(InstructionSet_AVX512))
        {
            return NI_Illegal;
        }
    }
#elif defined(TARGET_ARM64)
    else if (isa == InstructionSet_Vector64)
    {
        if (!isHWIntrinsicEnabled)
        {
            return NI_Illegal;
        }
    }
    else if (isa == InstructionSet_VectorT)
    {
        // This instruction set should only be set when SVE is enabled.
        // Baseline Vector<T> will use InstructionSet_VectorT128.
        if (!comp->compOpportunisticallyDependsOn(InstructionSet_Sve))
        {
            return NI_Illegal;
        }
    }
#endif

#if defined(TARGET_XARCH)
    // AVX10v1 is a strict superset of all AVX512 ISAs
    //
    // The original design was that it exposed the AVX512VL instructions without requiring V512 support
    // however, later iterations changed this and it is now just a unifying ISA instead

    if (isa == InstructionSet_AVX10v1)
    {
        NamedIntrinsic ni = binarySearchId(InstructionSet_AVX512, sig, methodName);

        if (ni != NI_Illegal)
        {
            return ni;
        }

        ni = binarySearchId(InstructionSet_AVX512v2, sig, methodName);

        if (ni != NI_Illegal)
        {
            return ni;
        }

        return binarySearchId(InstructionSet_AVX512v3, sig, methodName);
    }
    else if (isa == InstructionSet_AVX10v1_X64)
    {
        return binarySearchId(InstructionSet_AVX512_X64, sig, methodName);
    }
#endif // TARGET_XARCH

    return binarySearchId(isa, sig, methodName);
}

//------------------------------------------------------------------------
// lookupSimdSize: Gets the SimdSize for a given HWIntrinsic and signature
//
// Arguments:
//    id -- The ID associated with the HWIntrinsic to lookup
//   sig -- The signature of the HWIntrinsic to lookup
//
// Return Value:
//    The SIMD size for the HWIntrinsic associated with id and sig
//
// Remarks:
//    This function is only used by the importer. After importation, we can
//    get the SIMD size from the GenTreeHWIntrinsic node.
unsigned HWIntrinsicInfo::lookupSimdSize(Compiler* comp, NamedIntrinsic id, CORINFO_SIG_INFO* sig)
{
    unsigned simdSize = 0;

    if (tryLookupSimdSize(id, &simdSize))
    {
        return simdSize;
    }

    CORINFO_CLASS_HANDLE typeHnd = nullptr;

    if (HWIntrinsicInfo::BaseTypeFromFirstArg(id))
    {
        typeHnd = comp->info.compCompHnd->getArgClass(sig, sig->args);
    }
    else if (HWIntrinsicInfo::BaseTypeFromSecondArg(id))
    {
        CORINFO_ARG_LIST_HANDLE secondArg = comp->info.compCompHnd->getArgNext(sig->args);
        typeHnd                           = comp->info.compCompHnd->getArgClass(sig, secondArg);
    }
    else
    {
        assert(JITtype2varType(sig->retType) == TYP_STRUCT);
        typeHnd = sig->retTypeSigClass;
    }

    var_types simdBaseType = comp->getBaseTypeAndSizeOfSIMDType(typeHnd, &simdSize);
    assert((simdSize > 0) && (simdBaseType != TYP_UNDEF));
    return simdSize;
}

//------------------------------------------------------------------------
// isImmOp: Checks whether the HWIntrinsic node has an imm operand
//
// Arguments:
//    id -- The NamedIntrinsic associated with the HWIntrinsic to lookup
//    op -- The operand to check
//
// Return Value:
//     true if the node has an imm operand; otherwise, false
bool HWIntrinsicInfo::isImmOp(NamedIntrinsic id, const GenTree* op)
{
#ifdef TARGET_XARCH
    if (HWIntrinsicInfo::lookupCategory(id) != HW_Category_IMM)
    {
        return false;
    }

    if (!HWIntrinsicInfo::MaybeImm(id))
    {
        return true;
    }
#elif defined(TARGET_ARM64)
    if (!HWIntrinsicInfo::HasImmediateOperand(id))
    {
        return false;
    }
#elif defined(TARGET_WASM)
    if (!HWIntrinsicInfo::HasImmediateOperand(id))
    {
        return false;
    }
#else
#error Unsupported platform
#endif

    if (genActualType(op->TypeGet()) != TYP_INT)
    {
        return false;
    }

    return true;
}

//------------------------------------------------------------------------
// getArgForHWIntrinsic: pop an argument from the stack and validate its type
//
// Arguments:
//    argType    -- the required type of argument
//    argClass   -- the class handle of argType
//
// Return Value:
//     the validated argument
//
GenTree* Compiler::getArgForHWIntrinsic(var_types argType, CORINFO_CLASS_HANDLE argClass)
{
    GenTree* arg = nullptr;

    if (varTypeIsStruct(argType))
    {
        if (!varTypeIsSIMD(argType))
        {
            unsigned int argSizeBytes;
            (void)getBaseTypeAndSizeOfSIMDType(argClass, &argSizeBytes);
            argType = getSIMDTypeForSize(argSizeBytes);
        }
        assert(varTypeIsSIMD(argType));

        arg = impSIMDPopStack();
        assert(varTypeIsSIMDOrMask(arg));
    }
    else
    {
        assert(varTypeIsArithmetic(argType) || (argType == TYP_BYREF));

        arg = impPopStack().val;
        assert(varTypeIsArithmetic(arg->TypeGet()) || ((argType == TYP_BYREF) && arg->TypeIs(TYP_BYREF)));

        if (!impCheckImplicitArgumentCoercion(argType, arg->gtType))
        {
            BADCODE("the hwintrinsic argument has a type that can't be implicitly converted to the signature type");
        }
    }

    return arg;
}

//------------------------------------------------------------------------
// impSimdCreate: Common importer logic for Vector{64,128,256,512}.Create intrinsics.
//
// Handles the shared pattern used across xarch, arm64, and wasm:
//   * sig->numArgs == 1                    -> emit a broadcast/splat node
//   * sig->numArgs == simdLength, all const-> fold into a single GenTreeVecCon
//   * otherwise                            -> pack operands into a GT_HWINTRINSIC
//                                             via IntrinsicNodeBuilder
//
// Arguments:
//    intrinsic    -- the NI_Vector*_Create NamedIntrinsic being imported
//    sig          -- the call signature (used for numArgs)
//    simdBaseType -- the base element type of the SIMD vector
//    retType      -- the SIMD return type
//    simdSize     -- size in bytes of the SIMD vector
//
// Return Value:
//    The imported GenTree* representing the Create call. Operands are popped
//    off the importer stack as part of this call.
//
GenTree* Compiler::impSimdCreate(
    NamedIntrinsic intrinsic, CORINFO_SIG_INFO* sig, var_types simdBaseType, var_types retType, unsigned simdSize)
{
    if (sig->numArgs == 1)
    {
        GenTree* op1 = impStackTop().val;

#ifdef TARGET_WASM
        if (!(op1->IsIntegralConst() || op1->IsCnsFltOrDbl()))
        {
            // Vector*.Create(T) with a non-constant operand is not yet supported on Wasm.
            // Returning nullptr lets the importer fall back to the managed implementation.
            return nullptr;
        }
#endif

        op1 = impPopStack().val;
        return gtNewSimdCreateBroadcastNode(retType, op1, simdBaseType, simdSize);
    }

    uint32_t simdLength = getSIMDVectorLength(simdSize, simdBaseType);
    assert(sig->numArgs == simdLength);

    bool isConstant = true;

    if (varTypeIsFloating(simdBaseType))
    {
        for (uint32_t index = 0; index < sig->numArgs; index++)
        {
            if (!impStackTop(index).val->IsCnsFltOrDbl())
            {
                isConstant = false;
                break;
            }
        }
    }
    else
    {
        assert(varTypeIsIntegral(simdBaseType));

        for (uint32_t index = 0; index < sig->numArgs; index++)
        {
            if (!impStackTop(index).val->IsIntegralConst())
            {
                isConstant = false;
                break;
            }
        }
    }

    if (isConstant)
    {
        GenTreeVecCon* vecCon = gtNewVconNode(retType);

        switch (simdBaseType)
        {
            case TYP_BYTE:
            case TYP_UBYTE:
            {
                for (uint32_t index = 0; index < sig->numArgs; index++)
                {
                    uint8_t cnsVal = static_cast<uint8_t>(impPopStack().val->AsIntConCommon()->IntegralValue());
                    vecCon->gtSimdVal.u8[simdLength - 1 - index] = cnsVal;
                }
                break;
            }

            case TYP_SHORT:
            case TYP_USHORT:
            {
                for (uint32_t index = 0; index < sig->numArgs; index++)
                {
                    uint16_t cnsVal = static_cast<uint16_t>(impPopStack().val->AsIntConCommon()->IntegralValue());
                    vecCon->gtSimdVal.u16[simdLength - 1 - index] = cnsVal;
                }
                break;
            }

            case TYP_INT:
            case TYP_UINT:
            {
                for (uint32_t index = 0; index < sig->numArgs; index++)
                {
                    uint32_t cnsVal = static_cast<uint32_t>(impPopStack().val->AsIntConCommon()->IntegralValue());
                    vecCon->gtSimdVal.u32[simdLength - 1 - index] = cnsVal;
                }
                break;
            }

            case TYP_LONG:
            case TYP_ULONG:
            {
                for (uint32_t index = 0; index < sig->numArgs; index++)
                {
                    uint64_t cnsVal = static_cast<uint64_t>(impPopStack().val->AsIntConCommon()->IntegralValue());
                    vecCon->gtSimdVal.u64[simdLength - 1 - index] = cnsVal;
                }
                break;
            }

            case TYP_FLOAT:
            {
                for (uint32_t index = 0; index < sig->numArgs; index++)
                {
                    float cnsVal = static_cast<float>(impPopStack().val->AsDblCon()->DconValue());
                    vecCon->gtSimdVal.f32[simdLength - 1 - index] = cnsVal;
                }
                break;
            }

            case TYP_DOUBLE:
            {
                for (uint32_t index = 0; index < sig->numArgs; index++)
                {
                    double cnsVal = static_cast<double>(impPopStack().val->AsDblCon()->DconValue());
                    vecCon->gtSimdVal.f64[simdLength - 1 - index] = cnsVal;
                }
                break;
            }

            default:
            {
                unreached();
            }
        }

        return vecCon;
    }
#ifdef TARGET_WASM
    else
    {
        // non const Vector128.Create not yet implemented on Wasm
        return nullptr;
    }
#endif

    IntrinsicNodeBuilder nodeBuilder(getAllocator(CMK_ASTNode), sig->numArgs);

    for (int i = sig->numArgs - 1; i >= 0; i--)
    {
        GenTree* arg = impPopStack().val;
        nodeBuilder.AddOperand(i, arg);
    }

    return gtNewSimdHWIntrinsicNode(retType, std::move(nodeBuilder), intrinsic, simdBaseType, simdSize);
}

//------------------------------------------------------------------------
// addRangeCheckIfNeeded: add a GT_BOUNDS_CHECK node for non-full-range imm-intrinsic
//
// Arguments:
//    intrinsic     -- intrinsic ID
//    immOp         -- the immediate operand of the intrinsic
//    immLowerBound -- lower incl. bound for a value of the immediate operand (for a non-full-range imm-intrinsic)
//    immUpperBound -- upper incl. bound for a value of the immediate operand (for a non-full-range imm-intrinsic)
//
// Return Value:
//     add a GT_BOUNDS_CHECK node for non-full-range imm-intrinsic, which would throw ArgumentOutOfRangeException
//     when the imm-argument is not in the valid range
//
GenTree* Compiler::addRangeCheckIfNeeded(NamedIntrinsic intrinsic, GenTree* immOp, int immLowerBound, int immUpperBound)
{
    assert(immOp != nullptr);
    // Full-range imm-intrinsics do not need the range-check
    // because the imm-parameter of the intrinsic method is a byte.
    // AVX2 Gather intrinsics no not need the range-check
    // because their imm-parameter have discrete valid values that are handle by managed code
    if (!immOp->IsCnsIntOrI() && HWIntrinsicInfo::isImmOp(intrinsic, immOp)
#ifdef TARGET_XARCH
        && !HWIntrinsicInfo::isAVX2GatherIntrinsic(intrinsic) && !HWIntrinsicInfo::HasFullRangeImm(intrinsic)
#endif
    )
    {
        assert(varTypeIsIntegral(immOp));

        return addRangeCheckForHWIntrinsic(immOp, immLowerBound, immUpperBound);
    }
    else
    {
        return immOp;
    }
}

//------------------------------------------------------------------------
// addRangeCheckForHWIntrinsic: add a GT_BOUNDS_CHECK node for an intrinsic
//
// Arguments:
//    immOp         -- the immediate operand of the intrinsic
//    immLowerBound -- lower incl. bound for a value of the immediate operand (for a non-full-range imm-intrinsic)
//    immUpperBound -- upper incl. bound for a value of the immediate operand (for a non-full-range imm-intrinsic)
//
// Return Value:
//     add a GT_BOUNDS_CHECK node for non-full-range imm-intrinsic, which would throw ArgumentOutOfRangeException
//     when the imm-argument is not in the valid range
//
GenTree* Compiler::addRangeCheckForHWIntrinsic(GenTree* immOp, int immLowerBound, int immUpperBound)
{
    // Bounds check for value of an immediate operand
    //   (immLowerBound <= immOp) && (immOp <= immUpperBound)
    //
    // implemented as a single comparison in the form of
    //
    // if ((immOp - immLowerBound) >= (immUpperBound - immLowerBound + 1))
    // {
    //     throw new ArgumentOutOfRangeException();
    // }
    //
    // The value of (immUpperBound - immLowerBound + 1) is denoted as adjustedUpperBound.

    const ssize_t adjustedUpperBound     = (ssize_t)immUpperBound - immLowerBound + 1;
    GenTree*      adjustedUpperBoundNode = gtNewIconNode(adjustedUpperBound, TYP_INT);

    GenTree* immOpDup = nullptr;

    immOp = impCloneExpr(immOp, &immOpDup, CHECK_SPILL_ALL,
                         nullptr DEBUGARG("Clone an immediate operand for immediate value bounds check"));

    if (immLowerBound != 0)
    {
        immOpDup = gtNewOperNode(GT_SUB, TYP_INT, immOpDup, gtNewIconNode(immLowerBound, TYP_INT));
    }

    GenTreeBoundsChk* hwIntrinsicChk =
        new (this, GT_BOUNDS_CHECK) GenTreeBoundsChk(immOpDup, adjustedUpperBoundNode, SCK_ARG_RNG_EXCPN);

    return gtNewOperNode(GT_COMMA, immOp->TypeGet(), hwIntrinsicChk, immOp);
}

//------------------------------------------------------------------------
// compSupportsHWIntrinsic: check whether a given instruction is enabled via configuration
//
// Arguments:
//    isa - Instruction set
//
// Return Value:
//    true iff the given instruction set is enabled via configuration (environment variables, etc.).
bool Compiler::compSupportsHWIntrinsic(CORINFO_InstructionSet isa)
{
    return compHWIntrinsicDependsOn(isa);
}

//------------------------------------------------------------------------
// impIsTableDrivenHWIntrinsic:
//
// Arguments:
//    intrinsicId - HW intrinsic id
//    category - category of a HW intrinsic
//
// Return Value:
//    returns true if this category can be table-driven in the importer
//
static bool impIsTableDrivenHWIntrinsic(NamedIntrinsic intrinsicId, HWIntrinsicCategory category)
{
    return (category != HW_Category_Special) && !HWIntrinsicInfo::HasSpecialImport(intrinsicId);
}

static bool isSupportedBaseType(NamedIntrinsic intrinsic, var_types baseType)
{
    if (baseType == TYP_UNDEF)
    {
        return false;
    }

    // We don't actually check the intrinsic outside of the false case as we expect
    // the exposed managed signatures are either generic and support all types
    // or they are explicit and support the type indicated.

    if (varTypeIsArithmetic(baseType))
    {
        return true;
    }

    assert(HWIntrinsicInfo::lookupIsa(intrinsic) == InstructionSet_Vector);
    return false;
}

// HWIntrinsicSignatureReader: a helper class that "reads" a list of hardware intrinsic arguments and stores
// the corresponding argument type descriptors as the fields of the class instance.
//
struct HWIntrinsicSignatureReader final
{
    // Read: enumerates the list of arguments of a hardware intrinsic and stores the CORINFO_CLASS_HANDLE
    // and var_types values of each operand into the corresponding fields of the class instance.
    //
    // Arguments:
    //    compHnd -- an instance of COMP_HANDLE class.
    //    sig     -- a hardware intrinsic signature.
    //
    void Read(COMP_HANDLE compHnd, CORINFO_SIG_INFO* sig)
    {
        CORINFO_ARG_LIST_HANDLE args = sig->args;

        if (sig->numArgs > 0)
        {
            op1JitType = strip(compHnd->getArgType(sig, args, &op1ClsHnd));

            if (sig->numArgs > 1)
            {
                args       = compHnd->getArgNext(args);
                op2JitType = strip(compHnd->getArgType(sig, args, &op2ClsHnd));
            }

            if (sig->numArgs > 2)
            {
                args       = compHnd->getArgNext(args);
                op3JitType = strip(compHnd->getArgType(sig, args, &op3ClsHnd));
            }

            if (sig->numArgs > 3)
            {
                args       = compHnd->getArgNext(args);
                op4JitType = strip(compHnd->getArgType(sig, args, &op4ClsHnd));
            }
        }
    }

    CORINFO_CLASS_HANDLE op1ClsHnd;
    CORINFO_CLASS_HANDLE op2ClsHnd;
    CORINFO_CLASS_HANDLE op3ClsHnd;
    CORINFO_CLASS_HANDLE op4ClsHnd;
    CorInfoType          op1JitType;
    CorInfoType          op2JitType;
    CorInfoType          op3JitType;
    CorInfoType          op4JitType;

    var_types GetOp1Type() const
    {
        return JITtype2varType(op1JitType);
    }

    var_types GetOp2Type() const
    {
        return JITtype2varType(op2JitType);
    }

    var_types GetOp3Type() const
    {
        return JITtype2varType(op3JitType);
    }

    var_types GetOp4Type() const
    {
        return JITtype2varType(op4JitType);
    }

    var_types GetOp1TypeAsPrecise() const
    {
        return JitType2PreciseVarType(op1JitType);
    }

    var_types GetOp2TypeAsPrecise() const
    {
        return JitType2PreciseVarType(op2JitType);
    }

    var_types GetOp3TypeAsPrecise() const
    {
        return JitType2PreciseVarType(op3JitType);
    }

    var_types GetOp4TypeAsPrecise() const
    {
        return JitType2PreciseVarType(op4JitType);
    }
};

//------------------------------------------------------------------------
// CheckHWIntrinsicImmRange: Check if an immediate is within the valid range
//
// Arguments:
//    intrinsicId       -- HW intrinsic id
//    simdBaseJitType   -- The base JIT type of SIMD type of the intrinsic
//    immOp             -- Immediate to check is within range
//    mustExpand        -- true if the intrinsic must expand to a GenTree*; otherwise, false
//    immLowerBound     -- the lower valid bound of the immediate
//    immLowerBound     -- the upper valid bound of the immediate
//    hasFullRangeImm   -- the range has all valid values. The immediate is always within range.
//    useFallback [OUT] -- Only set if false is returned. A fallback can be used instead.
//
// Return Value:
//    returns true if immOp is within range. Otherwise false.
//
bool Compiler::CheckHWIntrinsicImmRange(NamedIntrinsic intrinsic,
                                        var_types      simdBaseType,
                                        GenTree*       immOp,
                                        bool           mustExpand,
                                        int            immLowerBound,
                                        int            immUpperBound,
                                        bool           hasFullRangeImm,
                                        bool*          useFallback)
{
    *useFallback = false;

    if (!hasFullRangeImm && immOp->IsCnsIntOrI())
    {
        const int ival = (int)immOp->AsIntCon()->IconValue();
        bool      immOutOfRange;
#ifdef TARGET_XARCH
        if (HWIntrinsicInfo::isAVX2GatherIntrinsic(intrinsic))
        {
            immOutOfRange = (ival != 1) && (ival != 2) && (ival != 4) && (ival != 8);
        }
        else
#endif
        {
            immOutOfRange = (ival < immLowerBound) || (ival > immUpperBound);
        }

        if (immOutOfRange)
        {
            // The imm-HWintrinsics that do not accept all imm8 values may throw
            // ArgumentOutOfRangeException when the imm argument is not in the valid range,
            // unless the intrinsic can be transformed into one that does accept all imm8 values

#ifdef TARGET_ARM64
            switch (intrinsic)
            {
                case NI_AdvSimd_ShiftLeftLogical:
                case NI_AdvSimd_ShiftLeftLogicalScalar:
                case NI_AdvSimd_ShiftRightLogical:
                case NI_AdvSimd_ShiftRightLogicalScalar:
                case NI_AdvSimd_ShiftRightArithmetic:
                case NI_AdvSimd_ShiftRightArithmeticScalar:
                    *useFallback = true;
                    break;

                default:
                    assert(*useFallback == false);
                    break;
            }
#endif // TARGET_ARM64

            return false;
        }
    }
    else if (!immOp->IsCnsIntOrI())
    {
        if (HWIntrinsicInfo::NoJmpTableImm(intrinsic))
        {
            *useFallback = true;
            return false;
        }
#if defined(TARGET_XARCH)
        else if (HWIntrinsicInfo::MaybeNoJmpTableImm(intrinsic))
        {
#if defined(TARGET_X86)

            if (varTypeIsLong(simdBaseType))
            {
                if (!mustExpand)
                {
                    return false;
                }
            }
            else
#endif // TARGET_X86
            {
                *useFallback = true;
                return false;
            }
        }
#endif // TARGET_XARCH
        else if (!mustExpand)
        {
            // When the imm-argument is not a constant and we are not being forced to expand, we need to
            // return false so a GT_CALL to the intrinsic method is emitted instead. The
            // intrinsic method is recursive and will be forced to expand, at which point
            // we emit some less efficient fallback code.
            return false;
        }
    }

    return true;
}

//------------------------------------------------------------------------
// impHWIntrinsic: Import a hardware intrinsic as a GT_HWINTRINSIC node if possible
//
// Arguments:
//    intrinsic  -- id of the intrinsic function.
//    clsHnd     -- class handle containing the intrinsic function.
//    method     -- method handle of the intrinsic function.
//    sig        -- signature of the intrinsic call
//    entryPoint -- The entry point information required for R2R scenarios
//    mustExpand -- true if the intrinsic must return a GenTree*; otherwise, false

// Return Value:
//    The GT_HWINTRINSIC node, or nullptr if not a supported intrinsic
//
GenTree* Compiler::impHWIntrinsic(NamedIntrinsic        intrinsic,
                                  CORINFO_CLASS_HANDLE  clsHnd,
                                  CORINFO_METHOD_HANDLE method,
                                  CORINFO_SIG_INFO* sig R2RARG(CORINFO_CONST_LOOKUP* entryPoint),
                                  bool                  mustExpand)
{
    // NextCallRetAddr requires a CALL, so return nullptr.
    if (!mustExpand && info.compHasNextCallRetAddr)
    {
        return nullptr;
    }

    HWIntrinsicCategory    category     = HWIntrinsicInfo::lookupCategory(intrinsic);
    CORINFO_InstructionSet isa          = HWIntrinsicInfo::lookupIsa(intrinsic);
    int                    numArgs      = sig->numArgs;
    var_types              retType      = genActualType(JITtype2varType(sig->retType));
    var_types              simdBaseType = TYP_UNDEF;
    GenTree*               retNode      = nullptr;

    if (retType == TYP_STRUCT)
    {
        unsigned int sizeBytes;
        simdBaseType = getBaseTypeAndSizeOfSIMDType(sig->retTypeSigClass, &sizeBytes);

        if (HWIntrinsicInfo::IsMultiReg(intrinsic))
        {
            assert(sizeBytes == 0);
        }

#ifdef TARGET_ARM64
        else if ((intrinsic == NI_AdvSimd_LoadAndInsertScalar) || (intrinsic == NI_AdvSimd_Arm64_LoadAndInsertScalar))
        {
            var_types retFieldBaseType = TYP_UNDEF;

            var_types retFieldType = impNormStructType(sig->retTypeSigClass, &retFieldBaseType);

            if (retFieldType == TYP_STRUCT)
            {
                CORINFO_CLASS_HANDLE structType;
                unsigned int         sizeBytes = 0;

                // LoadAndInsertScalar that returns 2,3 or 4 vectors
                assert(retFieldBaseType == TYP_UNDEF);
                unsigned fieldCount = info.compCompHnd->getClassNumInstanceFields(sig->retTypeSigClass);
                assert(fieldCount > 1);
                CORINFO_FIELD_HANDLE fieldHandle = info.compCompHnd->getFieldInClass(sig->retTypeClass, 0);
                CorInfoType          fieldType   = info.compCompHnd->getFieldType(fieldHandle, &structType);
                simdBaseType                     = getBaseTypeAndSizeOfSIMDType(structType, &sizeBytes);
                switch (fieldCount)
                {
                    case 2:
                        intrinsic = sizeBytes == 8 ? NI_AdvSimd_LoadAndInsertScalarVector64x2
                                                   : NI_AdvSimd_Arm64_LoadAndInsertScalarVector128x2;
                        break;
                    case 3:
                        intrinsic = sizeBytes == 8 ? NI_AdvSimd_LoadAndInsertScalarVector64x3
                                                   : NI_AdvSimd_Arm64_LoadAndInsertScalarVector128x3;
                        break;
                    case 4:
                        intrinsic = sizeBytes == 8 ? NI_AdvSimd_LoadAndInsertScalarVector64x4
                                                   : NI_AdvSimd_Arm64_LoadAndInsertScalarVector128x4;
                        break;
                    default:
                        assert(!"unsupported");
                }
            }
            else
            {
                assert((retFieldType == TYP_SIMD8) || (retFieldType == TYP_SIMD16));
                assert(isSupportedBaseType(intrinsic, simdBaseType));
                retType = getSIMDTypeForSize(sizeBytes);
            }
        }
#endif
        else
        {
            // We want to return early here for cases where retType was TYP_STRUCT as per method signature and
            // rather than deferring the decision after getting the simdBaseJitType of arg.
            if (!isSupportedBaseType(intrinsic, simdBaseType))
            {
                return nullptr;
            }

            assert(sizeBytes != 0);
            retType = getSIMDTypeForSize(sizeBytes);
        }
    }

    simdBaseType      = getBaseTypeFromArgIfNeeded(intrinsic, sig, simdBaseType);
    unsigned simdSize = 0;

    if (simdBaseType == TYP_UNDEF)
    {
        if ((category == HW_Category_Scalar) || (category == HW_Category_Special))
        {
            simdBaseType = JitType2PreciseVarType(sig->retType);

            if (simdBaseType == TYP_VOID)
            {
                simdBaseType = TYP_UNDEF;
            }
        }
        else
        {
            unsigned int sizeBytes;

            simdBaseType = getBaseTypeAndSizeOfSIMDType(clsHnd, &sizeBytes);

#ifdef TARGET_ARM64
            if (simdBaseType == TYP_UNDEF && HWIntrinsicInfo::HasScalarInputVariant(intrinsic))
            {
                // Did not find a valid vector type. The intrinsic has alternate scalar version. Switch to that.

                assert(sizeBytes == 0);
                intrinsic = HWIntrinsicInfo::GetScalarInputVariant(intrinsic);
                category  = HWIntrinsicInfo::lookupCategory(intrinsic);
                isa       = HWIntrinsicInfo::lookupIsa(intrinsic);

                simdBaseType = JitType2PreciseVarType(sig->retType);
                assert(simdBaseType != TYP_VOID);
                assert(simdBaseType != TYP_UNDEF);
                assert(simdBaseType != TYP_STRUCT);
            }
            else
#endif // TARGET_ARM64
            {
                assert((category == HW_Category_Special) || (category == HW_Category_Helper) || (sizeBytes != 0));
            }
        }
    }
#ifdef TARGET_ARM64
    else if ((simdBaseType == TYP_STRUCT) && (HWIntrinsicInfo::BaseTypeFromValueTupleArg(intrinsic)))
    {
        // If HW_Flag_BaseTypeFromValueTupleArg is set, one of the base type position flags must be set.
        assert(HWIntrinsicInfo::BaseTypeFromFirstArg(intrinsic) || HWIntrinsicInfo::BaseTypeFromSecondArg(intrinsic));

        CORINFO_ARG_LIST_HANDLE arg = sig->args;

        if (HWIntrinsicInfo::BaseTypeFromSecondArg(intrinsic))
        {
            arg = info.compCompHnd->getArgNext(arg);
        }

        CORINFO_CLASS_HANDLE argClass = info.compCompHnd->getArgClass(sig, arg);
        INDEBUG(unsigned fieldCount = info.compCompHnd->getClassNumInstanceFields(argClass));
        assert(fieldCount > 1);

        CORINFO_CLASS_HANDLE classHnd;
        CORINFO_FIELD_HANDLE fieldHandle = info.compCompHnd->getFieldInClass(argClass, 0);
        CorInfoType          fieldType   = info.compCompHnd->getFieldType(fieldHandle, &classHnd);
        assert(isIntrinsicType(classHnd));

        simdBaseType = getBaseTypeAndSizeOfSIMDType(classHnd, &simdSize);
        assert(simdSize > 0);
    }
#endif // TARGET_ARM64

    // Immediately return if the category is other than scalar/special and this is not a supported base type.
    if ((category != HW_Category_Special) && (category != HW_Category_Scalar) &&
        !isSupportedBaseType(intrinsic, simdBaseType))
    {
        return nullptr;
    }

    if (simdBaseType != TYP_UNDEF)
    {
#ifdef TARGET_XARCH
        if (HWIntrinsicInfo::NeedsNormalizeSmallTypeToInt(intrinsic) && varTypeIsSmall(simdBaseType))
        {
            simdBaseType = varTypeIsUnsigned(simdBaseType) ? TYP_UINT : TYP_INT;
        }
#endif // TARGET_XARCH
    }

    // We may have already determined simdSize for intrinsics that require special handling.
    // If so, skip the lookup.
    simdSize = (simdSize == 0) ? HWIntrinsicInfo::lookupSimdSize(this, intrinsic, sig) : simdSize;

    GenTree* immOp1          = nullptr;
    GenTree* immOp2          = nullptr;
    int      immLowerBound   = 0;
    int      immUpperBound   = 0;
    bool     hasFullRangeImm = false;
    bool     useFallback     = false;
    bool     setMethodHandle = false;

    getHWIntrinsicImmOps(intrinsic, sig, &immOp1, &immOp2);

    // Validate the second immediate
#ifdef TARGET_ARM64
    if (immOp2 != nullptr)
    {
        unsigned  immSimdSize     = simdSize;
        var_types immSimdBaseType = simdBaseType;
        getHWIntrinsicImmTypes(intrinsic, sig, 2, &immSimdSize, &immSimdBaseType);
        HWIntrinsicInfo::lookupImmBounds(intrinsic, immSimdSize, immSimdBaseType, 2, &immLowerBound, &immUpperBound);

        if (!CheckHWIntrinsicImmRange(intrinsic, simdBaseType, immOp2, mustExpand, immLowerBound, immUpperBound, false,
                                      &useFallback))
        {
            if (useFallback)
            {
                return impNonConstFallback(intrinsic, retType, simdBaseType);
            }
            else if (immOp2->IsCnsIntOrI())
            {
                // If we know the immediate is out-of-range,
                // convert the intrinsic into a user call (or throw if we must expand)
                return impUnsupportedNamedIntrinsic(CORINFO_HELP_THROW_ARGUMENTOUTOFRANGEEXCEPTION, method, sig,
                                                    mustExpand);
            }
            else
            {
                // The immediate is unknown, and we aren't using a fallback intrinsic.
                // In this case, CheckHWIntrinsicImmRange should not return false for intrinsics that must expand.
                assert(!mustExpand);

                if (opts.OptimizationEnabled())
                {
                    // Only enable late stage rewriting if optimizations are enabled
                    // as we won't otherwise encounter a constant at the later point
                    setMethodHandle = true;
                }
                else
                {
                    // Just convert to a user call
                    return nullptr;
                }
            }
        }
    }
#else
    assert(immOp2 == nullptr);
#endif

    // Validate the first immediate
    if (immOp1 != nullptr)
    {
#ifdef TARGET_ARM64
        unsigned  immSimdSize     = simdSize;
        var_types immSimdBaseType = simdBaseType;
        getHWIntrinsicImmTypes(intrinsic, sig, 1, &immSimdSize, &immSimdBaseType);
        HWIntrinsicInfo::lookupImmBounds(intrinsic, immSimdSize, immSimdBaseType, 1, &immLowerBound, &immUpperBound);
#elif defined(TARGET_XARCH)
        immUpperBound   = HWIntrinsicInfo::lookupImmUpperBound(intrinsic);
        hasFullRangeImm = HWIntrinsicInfo::HasFullRangeImm(intrinsic);
#elif defined(TARGET_WASM)
        immUpperBound = HWIntrinsicInfo::lookupImmUpperBound(intrinsic, simdSize, simdBaseType);
#endif

        if (!CheckHWIntrinsicImmRange(intrinsic, simdBaseType, immOp1, mustExpand, immLowerBound, immUpperBound,
                                      hasFullRangeImm, &useFallback))
        {
            if (useFallback)
            {
                return impNonConstFallback(intrinsic, retType, simdBaseType);
            }
            else if (immOp1->IsCnsIntOrI())
            {
                // If we know the immediate is out-of-range,
                // convert the intrinsic into a user call (or throw if we must expand)
                return impUnsupportedNamedIntrinsic(CORINFO_HELP_THROW_ARGUMENTOUTOFRANGEEXCEPTION, method, sig,
                                                    mustExpand);
            }
            else
            {
                // The immediate is unknown, and we aren't using a fallback intrinsic.
                // In this case, CheckHWIntrinsicImmRange should not return false for intrinsics that must expand.
                assert(!mustExpand);

                if (opts.OptimizationEnabled())
                {
                    // Only enable late stage rewriting if optimizations are enabled
                    // as we won't otherwise encounter a constant at the later point
                    setMethodHandle = true;
                }
                else
                {
                    // Just convert to a user call
                    return nullptr;
                }
            }
        }
    }

    if (HWIntrinsicInfo::IsFloatingPointUsed(intrinsic))
    {
        // Set `compFloatingPointUsed` to cover the scenario where an intrinsic is operating on SIMD fields, but
        // where no SIMD local vars are in use. This is the same logic as is used for FEATURE_SIMD.
        compFloatingPointUsed = true;
    }

    var_types nodeRetType = retType;
#if defined(FEATURE_MASKED_HW_INTRINSICS) && defined(TARGET_ARM64)
    if (HWIntrinsicInfo::ReturnsPerElementMask(intrinsic))
    {
        // Ensure the result is generated to a mask.
        nodeRetType = TYP_MASK;
    }
#endif // FEATURE_MASKED_HW_INTRINSICS && TARGET_ARM64

    // table-driven importer of simple intrinsics
    if (impIsTableDrivenHWIntrinsic(intrinsic, category))
    {
        const bool isScalar = (category == HW_Category_Scalar);

        assert(numArgs >= 0);

        if (!isScalar)
        {
            if (HWIntrinsicInfo::lookupIns(intrinsic, simdBaseType, this) == INS_invalid)
            {
                assert(!"Unexpected HW intrinsic");
                return nullptr;
            }

#if defined(TARGET_ARM64)
            if ((simdSize != 8) && (simdSize != 16) && (simdSize != SIZE_UNKNOWN))
#elif defined(TARGET_XARCH)
            if ((simdSize != 16) && (simdSize != 32) && (simdSize != 64))
#elif defined(TARGET_WASM)
            if (simdSize != 16)
#endif // TARGET_*
            {
                assert(!"Unexpected SIMD size");
                return nullptr;
            }
        }

        GenTree*                   op1 = nullptr;
        GenTree*                   op2 = nullptr;
        GenTree*                   op3 = nullptr;
        GenTree*                   op4 = nullptr;
        HWIntrinsicSignatureReader sigReader;
        sigReader.Read(info.compCompHnd, sig);

        switch (numArgs)
        {
            case 4:
                op4 = getArgForHWIntrinsic(sigReader.GetOp4Type(), sigReader.op4ClsHnd);
                op4 = addRangeCheckIfNeeded(intrinsic, op4, immLowerBound, immUpperBound);
                op3 = getArgForHWIntrinsic(sigReader.GetOp3Type(), sigReader.op3ClsHnd);
                op2 = getArgForHWIntrinsic(sigReader.GetOp2Type(), sigReader.op2ClsHnd);
                op1 = getArgForHWIntrinsic(sigReader.GetOp1Type(), sigReader.op1ClsHnd);
                break;

            case 3:
                op3 = getArgForHWIntrinsic(sigReader.GetOp3Type(), sigReader.op3ClsHnd);
                op2 = getArgForHWIntrinsic(sigReader.GetOp2Type(), sigReader.op2ClsHnd);
                op1 = getArgForHWIntrinsic(sigReader.GetOp1Type(), sigReader.op1ClsHnd);
                break;

            case 2:
                op2 = getArgForHWIntrinsic(sigReader.GetOp2Type(), sigReader.op2ClsHnd);
                op2 = addRangeCheckIfNeeded(intrinsic, op2, immLowerBound, immUpperBound);
                op1 = getArgForHWIntrinsic(sigReader.GetOp1Type(), sigReader.op1ClsHnd);
                break;

            case 1:
                op1 = getArgForHWIntrinsic(sigReader.GetOp1Type(), sigReader.op1ClsHnd);
                break;

            default:
                break;
        }

        switch (numArgs)
        {
            case 0:
            {
                assert(!isScalar);
                retNode = gtNewSimdHWIntrinsicNode(nodeRetType, intrinsic, simdBaseType, simdSize);
                break;
            }

            case 1:
            {
                if ((category == HW_Category_MemoryLoad) && op1->OperIs(GT_CAST))
                {
                    // Although the API specifies a pointer, if what we have is a BYREF, that's what
                    // we really want, so throw away the cast.
                    if (op1->gtGetOp1()->TypeIs(TYP_BYREF))
                    {
                        op1 = op1->gtGetOp1();
                    }
                }

                retNode = isScalar ? gtNewScalarHWIntrinsicNode(nodeRetType, op1, intrinsic)
                                   : gtNewSimdHWIntrinsicNode(nodeRetType, op1, intrinsic, simdBaseType, simdSize);

#if defined(TARGET_XARCH)
                switch (intrinsic)
                {
                    case NI_X86Base_ConvertToVector128Int16:
                    case NI_X86Base_ConvertToVector128Int32:
                    case NI_X86Base_ConvertToVector128Int64:
                    case NI_AVX2_BroadcastScalarToVector128:
                    case NI_AVX2_BroadcastScalarToVector256:
                    case NI_AVX2_ConvertToVector256Int16:
                    case NI_AVX2_ConvertToVector256Int32:
                    case NI_AVX2_ConvertToVector256Int64:
                    {
                        // These intrinsics have both pointer and vector overloads
                        // We want to be able to differentiate between them so lets
                        // just track the aux type as a ptr or undefined, depending

                        var_types auxiliaryType = TYP_UNKNOWN;

                        if (!varTypeIsSIMD(op1))
                        {
                            auxiliaryType = TYP_U_IMPL;
                            retNode->gtFlags |= (GTF_EXCEPT | GTF_GLOB_REF);
                        }

                        retNode->AsHWIntrinsic()->SetAuxiliaryType(auxiliaryType);
                        break;
                    }

                    default:
                    {
                        break;
                    }
                }
#elif defined(TARGET_ARM64)
                switch (intrinsic)
                {
                    case NI_Sve_ConvertToDouble:
                    case NI_Sve_ConvertToInt32:
                    case NI_Sve_ConvertToInt64:
                    case NI_Sve_ConvertToSingle:
                    case NI_Sve_ConvertToUInt32:
                    case NI_Sve_ConvertToUInt64:
                        // Save the base type of return SIMD. It is used to contain this intrinsic inside
                        // ConditionalSelect.
                        retNode->AsHWIntrinsic()->SetAuxiliaryType(getBaseTypeOfSIMDType(sig->retTypeSigClass));
                        break;
                    default:
                        break;
                }
#endif // TARGET_XARCH

                break;
            }

            case 2:
            {
                retNode = isScalar ? gtNewScalarHWIntrinsicNode(nodeRetType, op1, op2, intrinsic)
                                   : gtNewSimdHWIntrinsicNode(nodeRetType, op1, op2, intrinsic, simdBaseType, simdSize);

#ifdef TARGET_XARCH
                if ((intrinsic == NI_X86Base_Crc32) || (intrinsic == NI_X86Base_X64_Crc32))
                {
                    // TODO-XArch-Cleanup: currently we use the simdBaseJitType to bring the type of the second argument
                    // to the code generator. May encode the overload info in other way.
                    retNode->AsHWIntrinsic()->SetSimdBaseType(sigReader.GetOp2TypeAsPrecise());
                }
#elif defined(TARGET_ARM64)
                switch (intrinsic)
                {
                    case NI_Crc32_ComputeCrc32:
                    case NI_Crc32_ComputeCrc32C:
                    case NI_Crc32_Arm64_ComputeCrc32:
                    case NI_Crc32_Arm64_ComputeCrc32C:
                        retNode->AsHWIntrinsic()->SetSimdBaseType(sigReader.GetOp2TypeAsPrecise());
                        break;

                    case NI_AdvSimd_AddWideningUpper:
                    case NI_AdvSimd_SubtractWideningUpper:
                        assert(varTypeIsSIMD(op1->TypeGet()));
                        retNode->AsHWIntrinsic()->SetAuxiliaryType(getBaseTypeOfSIMDType(sigReader.op1ClsHnd));
                        break;

                    case NI_AdvSimd_Arm64_AddSaturateScalar:
                        assert(varTypeIsSIMD(op2->TypeGet()));
                        retNode->AsHWIntrinsic()->SetAuxiliaryType(getBaseTypeOfSIMDType(sigReader.op2ClsHnd));
                        break;

                    case NI_ArmBase_Arm64_MultiplyHigh:
                        if (sig->retType == CORINFO_TYPE_ULONG)
                        {
                            retNode->AsHWIntrinsic()->SetSimdBaseType(TYP_ULONG);
                        }
                        else
                        {
                            assert(sig->retType == CORINFO_TYPE_LONG);
                            retNode->AsHWIntrinsic()->SetSimdBaseType(TYP_LONG);
                        }
                        break;

                    case NI_Sve_CreateWhileLessThanMaskByte:
                    case NI_Sve_CreateWhileLessThanMaskDouble:
                    case NI_Sve_CreateWhileLessThanMaskInt16:
                    case NI_Sve_CreateWhileLessThanMaskInt32:
                    case NI_Sve_CreateWhileLessThanMaskInt64:
                    case NI_Sve_CreateWhileLessThanMaskSByte:
                    case NI_Sve_CreateWhileLessThanMaskSingle:
                    case NI_Sve_CreateWhileLessThanMaskUInt16:
                    case NI_Sve_CreateWhileLessThanMaskUInt32:
                    case NI_Sve_CreateWhileLessThanMaskUInt64:
                    case NI_Sve_CreateWhileLessThanOrEqualMaskByte:
                    case NI_Sve_CreateWhileLessThanOrEqualMaskDouble:
                    case NI_Sve_CreateWhileLessThanOrEqualMaskInt16:
                    case NI_Sve_CreateWhileLessThanOrEqualMaskInt32:
                    case NI_Sve_CreateWhileLessThanOrEqualMaskInt64:
                    case NI_Sve_CreateWhileLessThanOrEqualMaskSByte:
                    case NI_Sve_CreateWhileLessThanOrEqualMaskSingle:
                    case NI_Sve_CreateWhileLessThanOrEqualMaskUInt16:
                    case NI_Sve_CreateWhileLessThanOrEqualMaskUInt32:
                    case NI_Sve_CreateWhileLessThanOrEqualMaskUInt64:
                    case NI_Sve2_CreateWhileGreaterThanMaskByte:
                    case NI_Sve2_CreateWhileGreaterThanMaskDouble:
                    case NI_Sve2_CreateWhileGreaterThanMaskInt16:
                    case NI_Sve2_CreateWhileGreaterThanMaskInt32:
                    case NI_Sve2_CreateWhileGreaterThanMaskInt64:
                    case NI_Sve2_CreateWhileGreaterThanMaskSByte:
                    case NI_Sve2_CreateWhileGreaterThanMaskSingle:
                    case NI_Sve2_CreateWhileGreaterThanMaskUInt16:
                    case NI_Sve2_CreateWhileGreaterThanMaskUInt32:
                    case NI_Sve2_CreateWhileGreaterThanMaskUInt64:
                    case NI_Sve2_CreateWhileGreaterThanOrEqualMaskByte:
                    case NI_Sve2_CreateWhileGreaterThanOrEqualMaskDouble:
                    case NI_Sve2_CreateWhileGreaterThanOrEqualMaskInt16:
                    case NI_Sve2_CreateWhileGreaterThanOrEqualMaskInt32:
                    case NI_Sve2_CreateWhileGreaterThanOrEqualMaskInt64:
                    case NI_Sve2_CreateWhileGreaterThanOrEqualMaskSByte:
                    case NI_Sve2_CreateWhileGreaterThanOrEqualMaskSingle:
                    case NI_Sve2_CreateWhileGreaterThanOrEqualMaskUInt16:
                    case NI_Sve2_CreateWhileGreaterThanOrEqualMaskUInt32:
                    case NI_Sve2_CreateWhileGreaterThanOrEqualMaskUInt64:
                        retNode->AsHWIntrinsic()->SetAuxiliaryType(JitType2PreciseVarType(sigReader.op1JitType));
                        break;

                    case NI_Sve_ShiftLeftLogical:
                    case NI_Sve_ShiftRightArithmetic:
                    case NI_Sve_ShiftRightLogical:
                        retNode->AsHWIntrinsic()->SetAuxiliaryType(getBaseTypeOfSIMDType(sigReader.op2ClsHnd));
                        break;

                    default:
                        break;
                }
#endif
                break;
            }

            case 3:
            {
#ifdef TARGET_ARM64
                if (intrinsic == NI_AdvSimd_LoadAndInsertScalar)
                {
                    op2 = addRangeCheckIfNeeded(intrinsic, op2, immLowerBound, immUpperBound);

                    if (op1->OperIs(GT_CAST))
                    {
                        // Although the API specifies a pointer, if what we have is a BYREF, that's what
                        // we really want, so throw away the cast.
                        if (op1->gtGetOp1()->TypeIs(TYP_BYREF))
                        {
                            op1 = op1->gtGetOp1();
                        }
                    }
                }
                else if ((intrinsic == NI_AdvSimd_Insert) || (intrinsic == NI_AdvSimd_InsertScalar))
                {
                    op2 = addRangeCheckIfNeeded(intrinsic, op2, immLowerBound, immUpperBound);
                }
                else
#elif defined(TARGET_WASM)
                // On WASM, PackedSimd.ReplaceScalar takes the lane immediate as the middle
                // (op2) operand: ReplaceScalar(Vector128<T> vector, byte imm, T value). Other
                // 3-arg PackedSimd immediate intrinsics (LoadScalarAndInsert, StoreSelectedScalar)
                // put the immediate at op3 and fall through to the default handling below.
                if (intrinsic == NI_PackedSimd_ReplaceScalar)
                {
                    op2 = addRangeCheckIfNeeded(intrinsic, op2, immLowerBound, immUpperBound);
                }
                else
#endif
                {
                    op3 = addRangeCheckIfNeeded(intrinsic, op3, immLowerBound, immUpperBound);
                }

                retNode = isScalar
                              ? gtNewScalarHWIntrinsicNode(nodeRetType, op1, op2, op3, intrinsic)
                              : gtNewSimdHWIntrinsicNode(nodeRetType, op1, op2, op3, intrinsic, simdBaseType, simdSize);

                switch (intrinsic)
                {
#if defined(TARGET_XARCH)
                    case NI_AVX2_GatherVector128:
                    case NI_AVX2_GatherVector256:
                        assert(varTypeIsSIMD(op2->TypeGet()));
                        retNode->AsHWIntrinsic()->SetAuxiliaryType(getBaseTypeOfSIMDType(sigReader.op2ClsHnd));
                        break;

#elif defined(TARGET_ARM64)
                    case NI_Sve_GatherVector:
                    case NI_Sve_GatherVectorByteZeroExtend:
                    case NI_Sve_GatherVectorByteZeroExtendFirstFaulting:
                    case NI_Sve_GatherVectorFirstFaulting:
                    case NI_Sve_GatherVectorInt16SignExtend:
                    case NI_Sve_GatherVectorInt16SignExtendFirstFaulting:
                    case NI_Sve_GatherVectorInt16WithByteOffsetsSignExtend:
                    case NI_Sve_GatherVectorInt16WithByteOffsetsSignExtendFirstFaulting:
                    case NI_Sve_GatherVectorInt32SignExtend:
                    case NI_Sve_GatherVectorInt32SignExtendFirstFaulting:
                    case NI_Sve_GatherVectorInt32WithByteOffsetsSignExtend:
                    case NI_Sve_GatherVectorInt32WithByteOffsetsSignExtendFirstFaulting:
                    case NI_Sve_GatherVectorSByteSignExtend:
                    case NI_Sve_GatherVectorSByteSignExtendFirstFaulting:
                    case NI_Sve_GatherVectorUInt16WithByteOffsetsZeroExtend:
                    case NI_Sve_GatherVectorUInt16WithByteOffsetsZeroExtendFirstFaulting:
                    case NI_Sve_GatherVectorUInt16ZeroExtend:
                    case NI_Sve_GatherVectorUInt16ZeroExtendFirstFaulting:
                    case NI_Sve_GatherVectorUInt32WithByteOffsetsZeroExtend:
                    case NI_Sve_GatherVectorUInt32WithByteOffsetsZeroExtendFirstFaulting:
                    case NI_Sve_GatherVectorUInt32ZeroExtend:
                    case NI_Sve_GatherVectorUInt32ZeroExtendFirstFaulting:
                    case NI_Sve_GatherVectorWithByteOffsets:
                    case NI_Sve_GatherVectorWithByteOffsetFirstFaulting:
                    case NI_Sve2_GatherVectorByteZeroExtendNonTemporal:
                    case NI_Sve2_GatherVectorInt16SignExtendNonTemporal:
                    case NI_Sve2_GatherVectorInt16WithByteOffsetsSignExtendNonTemporal:
                    case NI_Sve2_GatherVectorInt32SignExtendNonTemporal:
                    case NI_Sve2_GatherVectorInt32WithByteOffsetsSignExtendNonTemporal:
                    case NI_Sve2_GatherVectorNonTemporal:
                    case NI_Sve2_GatherVectorSByteSignExtendNonTemporal:
                    case NI_Sve2_GatherVectorUInt16WithByteOffsetsZeroExtendNonTemporal:
                    case NI_Sve2_GatherVectorUInt16ZeroExtendNonTemporal:
                    case NI_Sve2_GatherVectorUInt32WithByteOffsetsZeroExtendNonTemporal:
                    case NI_Sve2_GatherVectorUInt32ZeroExtendNonTemporal:
                    case NI_Sve2_GatherVectorWithByteOffsetsNonTemporal:
                        assert(varTypeIsSIMD(op3->TypeGet()));
                        if (numArgs == 3)
                        {
                            retNode->AsHWIntrinsic()->SetAuxiliaryType(getBaseTypeOfSIMDType(sigReader.op3ClsHnd));
                        }
                        break;
#endif

                    default:
                        break;
                }

                break;
            }

            case 4:
            {
                assert(!isScalar);
                retNode = gtNewSimdHWIntrinsicNode(nodeRetType, op1, op2, op3, op4, intrinsic, simdBaseType, simdSize);

                switch (intrinsic)
                {
#if defined(TARGET_ARM64)
                    case NI_Sve_Scatter:
                        assert(varTypeIsSIMD(op3->TypeGet()));
                        if (numArgs == 4)
                        {
                            retNode->AsHWIntrinsic()->SetAuxiliaryType(getBaseTypeOfSIMDType(sigReader.op3ClsHnd));
                        }
                        break;
#endif

                    default:
                        break;
                }
                break;
            }

            default:
                break;
        }
    }
    else
    {
        retNode = impSpecialIntrinsic(intrinsic, clsHnd, method, sig R2RARG(entryPoint), simdBaseType, nodeRetType,
                                      simdSize, mustExpand);

#if defined(FEATURE_MASKED_HW_INTRINSICS) && defined(TARGET_ARM64)
        if (retNode != nullptr)
        {
            // The special import may have switched the type of the node.
            nodeRetType = retNode->gtType;
        }
#endif
    }

    if (setMethodHandle && (retNode != nullptr))
    {
        retNode->AsHWIntrinsic()->SetMethodHandle(this, method R2RARG(*entryPoint));
    }

#if defined(FEATURE_MASKED_HW_INTRINSICS) && defined(TARGET_ARM64)
    if (HWIntrinsicInfo::IsExplicitMaskedOperation(intrinsic))
    {
        assert(numArgs > 0);

        switch (intrinsic)
        {
            case NI_Sve_CreateBreakAfterPropagateMask:
            case NI_Sve_CreateBreakBeforePropagateMask:
            {
                // HWInstrinsic requires a mask for op3
                GenTree*& op = retNode->AsHWIntrinsic()->Op(3);
                op           = gtNewSimdCvtVectorToMaskNode(TYP_MASK, op, simdBaseType, simdSize);
                FALLTHROUGH;
            }
            case NI_Sve_CreateBreakAfterMask:
            case NI_Sve_CreateBreakBeforeMask:
            case NI_Sve_CreateMaskForFirstActiveElement:
            case NI_Sve_CreateMaskForNextActiveElement:
            case NI_Sve_GetActiveElementCount:
            case NI_Sve_TestAnyTrue:
            case NI_Sve_TestFirstTrue:
            case NI_Sve_TestLastTrue:
            {
                // HWInstrinsic requires a mask for op2
                GenTree*& op = retNode->AsHWIntrinsic()->Op(2);
                op           = gtNewSimdCvtVectorToMaskNode(TYP_MASK, op, simdBaseType, simdSize);
                FALLTHROUGH;
            }
            default:
            {
                // HWInstrinsic requires a mask for op1
                GenTree*& op = retNode->AsHWIntrinsic()->Op(1);
                op           = gtNewSimdCvtVectorToMaskNode(TYP_MASK, op, simdBaseType, simdSize);
                break;
            }
        }

        if (HWIntrinsicInfo::IsMultiReg(intrinsic))
        {
            assert(HWIntrinsicInfo::IsExplicitMaskedOperation(retNode->AsHWIntrinsic()->GetHWIntrinsicId()));
            assert(HWIntrinsicInfo::IsMultiReg(retNode->AsHWIntrinsic()->GetHWIntrinsicId()));
            retNode =
                impStoreMultiRegValueToVar(retNode, sig->retTypeSigClass DEBUGARG(CorInfoCallConvExtension::Managed));
        }
    }

    if (HWIntrinsicInfo::IsEmbeddedMaskedOperation(intrinsic))
    {
        switch (intrinsic)
        {
            case NI_Sve_CreateBreakPropagateMask:
            {
                GenTree*& op1 = retNode->AsHWIntrinsic()->Op(1);
                GenTree*& op2 = retNode->AsHWIntrinsic()->Op(2);
                op1           = gtNewSimdCvtVectorToMaskNode(TYP_MASK, op1, simdBaseType, simdSize);
                op2           = gtNewSimdCvtVectorToMaskNode(TYP_MASK, op2, simdBaseType, simdSize);
                break;
            }

            default:
                break;
        }
    }

    if (nodeRetType == TYP_MASK)
    {
        // HWInstrinsic returns a mask, but all returns must be vectors, so convert mask to vector.
        retNode = gtNewSimdCvtMaskToVectorNode(retType, retNode, simdBaseType, simdSize);
    }
#endif // FEATURE_MASKED_HW_INTRINSICS && TARGET_ARM64

    if ((retNode != nullptr) && retNode->OperIs(GT_HWINTRINSIC))
    {
        assert(!retNode->OperMayThrow(this) || ((retNode->gtFlags & GTF_EXCEPT) != 0));
        assert(!retNode->OperRequiresAsgFlag() || ((retNode->gtFlags & GTF_ASG) != 0));
        assert(!retNode->OperIsImplicitIndir() || ((retNode->gtFlags & GTF_GLOB_REF) != 0));
    }

    return retNode;
}

//------------------------------------------------------------------------
// impXplatIntrinsic: dispatch xplat intrinsics to their own implementation
//
// Arguments:
//    intrinsic       -- id of the intrinsic function.
//    clsHnd          -- class handle containing the intrinsic function.
//    method          -- method handle of the intrinsic function.
//    sig             -- signature of the intrinsic call.
//    entryPoint      -- The entry point information required for R2R scenarios
//    simdBaseJitType -- generic argument of the intrinsic.
//    retType         -- return type of the intrinsic.
//    mustExpand      -- true if the intrinsic must return a GenTree*; otherwise, false
//
// Return Value:
//    the expanded intrinsic.
//
// Assumptions:
//    This method is only caled for Vector### methods
//
//    Xarch - baseline ISA requirements have been met, as follows:
//      Vector128: SSE2
//      Vector256: AVX (note that AVX2 cannot be assumed)
//      Vector512: AVX-512F+CD+DQ+BW+VL
//
GenTree* Compiler::impXplatIntrinsic(NamedIntrinsic        intrinsic,
                                     CORINFO_CLASS_HANDLE  clsHnd,
                                     CORINFO_METHOD_HANDLE method,
                                     CORINFO_SIG_INFO* sig R2RARG(CORINFO_CONST_LOOKUP* entryPoint),
                                     var_types             simdBaseType,
                                     var_types             retType,
                                     unsigned              simdSize,
                                     bool                  mustExpand)
{
    assert(HWIntrinsicInfo::lookupIsa(intrinsic) == InstructionSet_Vector);

#if defined(TARGET_XARCH)
    if (simdSize == 32)
    {
        bool potentiallyNotSupported = true;

        if (HWIntrinsicInfo::AvxOnlyCompatible(intrinsic))
        {
            switch (intrinsic)
            {
                case NI_Vector_Abs:
                case NI_Vector_IsNegative:
                case NI_Vector_IsPositive:
                {
                    potentiallyNotSupported = varTypeIsSigned(simdBaseType);
                    break;
                }

                case NI_Vector_ExtractMostSignificantBits:
                {
                    potentiallyNotSupported = varTypeIsSmall(simdBaseType);
                    break;
                }

                case NI_Vector_AddSaturate:
                case NI_Vector_Dot:
                case NI_Vector_Equals:
                case NI_Vector_GreaterThan:
                case NI_Vector_GreaterThanOrEqual:
                case NI_Vector_IsZero:
                case NI_Vector_LessThan:
                case NI_Vector_LessThanOrEqual:
                case NI_Vector_MaxNative:
                case NI_Vector_MinNative:
                case NI_Vector_MultiplyAddEstimate:
                case NI_Vector_Narrow:
                case NI_Vector_NarrowWithSaturation:
                case NI_Vector_SubtractSaturate:
                case NI_Vector_Sum:
                case NI_Vector_WidenLower:
                case NI_Vector_WidenUpper:
                case NI_Vector_op_Addition:
                case NI_Vector_op_Division:
                case NI_Vector_op_Equality:
                case NI_Vector_op_Inequality:
                case NI_Vector_op_Multiply:
                case NI_Vector_op_Subtraction:
                case NI_Vector_op_UnaryNegation:
                {
                    potentiallyNotSupported = varTypeIsIntegral(simdBaseType);
                    break;
                }

                case NI_Vector_IsFinite:
                case NI_Vector_IsInfinity:
                case NI_Vector_IsInteger:
                case NI_Vector_IsNegativeInfinity:
                case NI_Vector_IsPositiveInfinity:
                case NI_Vector_IsSubnormal:
                {
                    potentiallyNotSupported = varTypeIsFloating(simdBaseType);
                    break;
                }

                case NI_Vector_UnzipEven:
                case NI_Vector_UnzipOdd:
                {
                    potentiallyNotSupported = genTypeSize(simdBaseType) != 4;
                    break;
                }

                case NI_Vector_CreateAlternatingSequence:
                {
                    GenTree* op1 = impStackTop(1).val;
                    GenTree* op2 = impStackTop(0).val;

                    potentiallyNotSupported = !op1->OperIsConst() || !op2->OperIsConst();
                    break;
                }

                case NI_Vector_CreateGeometricSequence:
                case NI_Vector_CreateSequence:
                {
                    GenTree* op1 = impStackTop(1).val;
                    GenTree* op2 = impStackTop(0).val;

                    potentiallyNotSupported =
                        varTypeIsIntegral(simdBaseType) && (!op1->OperIsConst() || !op2->OperIsConst());
                    break;
                }

                default:
                {
                    potentiallyNotSupported = false;
                    break;
                }
            }
        }

        if (potentiallyNotSupported && !compOpportunisticallyDependsOn(InstructionSet_AVX2))
        {
            return nullptr;
        }
    }
#endif

    if (simdSize != 0)
    {
        assert(varTypeIsArithmetic(simdBaseType));
    }

    GenTree* retNode = nullptr;
    GenTree* op1     = nullptr;
    GenTree* op2     = nullptr;
    GenTree* op3     = nullptr;
    GenTree* op4     = nullptr;

    bool isMinMaxIntrinsic = false;
    bool isMax             = false;
    bool isMagnitude       = false;
    bool isNative          = false;
    bool isNumber          = false;

    bool isConcatIntrinsic = false;
    bool leftUpper         = false;
    bool rightUpper        = false;

    switch (intrinsic)
    {
        case NI_Vector_Abs:
        {
            assert(sig->numArgs == 1);
            op1     = impSIMDPopStack();
            retNode = gtNewSimdAbsNode(retType, op1, simdBaseType, simdSize);
            break;
        }

        case NI_Vector_AddSaturate:
        {
            assert(sig->numArgs == 2);

            op2 = impSIMDPopStack();
            op1 = impSIMDPopStack();

            if (varTypeIsFloating(simdBaseType))
            {
                retNode = gtNewSimdBinOpNode(GT_ADD, retType, op1, op2, simdBaseType, simdSize);
            }
            else
            {
#if defined(TARGET_ARM64)
                intrinsic = NI_AdvSimd_AddSaturate;

                if ((simdSize == 8) && varTypeIsLong(simdBaseType))
                {
                    intrinsic = NI_AdvSimd_AddSaturateScalar;
                }

                retNode = gtNewSimdHWIntrinsicNode(retType, op1, op2, intrinsic, simdBaseType, simdSize);
#elif defined(TARGET_XARCH) || defined(TARGET_WASM)
                if (varTypeIsSmall(simdBaseType))
                {
#if defined(TARGET_XARCH)
                    if (simdSize == 64)
                    {
                        intrinsic = NI_AVX512_AddSaturate;
                    }
                    else if (simdSize == 32)
                    {
                        intrinsic = NI_AVX2_AddSaturate;
                    }
                    else
                    {
                        assert(simdSize == 16);
                        intrinsic = NI_X86Base_AddSaturate;
                    }
#elif defined(TARGET_WASM)
                    intrinsic = NI_PackedSimd_AddSaturate;
#else
#error Unsupported platform
#endif
                    retNode = gtNewSimdHWIntrinsicNode(retType, op1, op2, intrinsic, simdBaseType, simdSize);
                }
                else if (varTypeIsUnsigned(simdBaseType))
                {
                    // For unsigned we simply have to detect `(x + y) < x`
                    // and in that scenario return MaxValue (AllBitsSet)

                    GenTree* cns     = gtNewAllBitsSetConNode(retType);
                    GenTree* op1Dup1 = fgMakeMultiUse(&op1);

                    GenTree* tmp     = gtNewSimdBinOpNode(GT_ADD, retType, op1, op2, simdBaseType, simdSize);
                    GenTree* tmpDup1 = fgMakeMultiUse(&tmp);
                    GenTree* msk     = gtNewSimdCmpOpNode(GT_LT, retType, tmp, op1Dup1, simdBaseType, simdSize);

                    retNode = gtNewSimdCndSelNode(retType, msk, cns, tmpDup1, simdBaseType, simdSize);
                }
                else
                {
                    // For signed the logic is a bit more complex, but is
                    // explained on the managed side as part of Scalar<T>.AddSaturate

                    GenTreeVecCon* minCns = gtNewVconNode(retType);
                    GenTreeVecCon* maxCns = gtNewVconNode(retType);

                    switch (simdBaseType)
                    {
                        case TYP_INT:
                        {
                            minCns->EvaluateBroadcastInPlace<int32_t>(INT32_MIN);
                            maxCns->EvaluateBroadcastInPlace<int32_t>(INT32_MAX);
                            break;
                        }

                        case TYP_LONG:
                        {
                            minCns->EvaluateBroadcastInPlace<int64_t>(INT64_MIN);
                            maxCns->EvaluateBroadcastInPlace<int64_t>(INT64_MAX);
                            break;
                        }

                        default:
                        {
                            unreached();
                        }
                    }

                    GenTree* op1Dup1 = fgMakeMultiUse(&op1);
                    GenTree* op2Dup1 = fgMakeMultiUse(&op2);

                    GenTree* tmp = gtNewSimdBinOpNode(GT_ADD, retType, op1, op2, simdBaseType, simdSize);

                    GenTree* tmpDup1 = fgMakeMultiUse(&tmp);
                    GenTree* tmpDup2 = gtCloneExpr(tmpDup1);

                    GenTree* msk = gtNewSimdIsNegativeNode(retType, tmpDup1, simdBaseType, simdSize);
                    GenTree* ovf = gtNewSimdCndSelNode(retType, msk, maxCns, minCns, simdBaseType, simdSize);

                    // The mask we need is ((a ^ b) & ~(b ^ c)) < 0

#if defined(TARGET_XARCH)
                    if (compOpportunisticallyDependsOn(InstructionSet_AVX512))
                    {
                        // tmpDup1 = a: 0xF0
                        // op1Dup1 = b: 0xCC
                        // op2Dup2 = c: 0xAA
                        //
                        // 0x18    = A ? norBC : andBC
                        //           a ? ~(b | c) : (b & c)
                        msk = gtNewSimdTernaryLogicNode(retType, tmp, op1Dup1, op2Dup1, gtNewIconNode(0x18),
                                                        simdBaseType, simdSize);
                    }
                    else
#endif
                    {
                        GenTree* op1Dup2 = gtCloneExpr(op1Dup1);

                        GenTree* msk2 = gtNewSimdBinOpNode(GT_XOR, retType, tmp, op1Dup1, simdBaseType, simdSize);
                        GenTree* msk3 = gtNewSimdBinOpNode(GT_XOR, retType, op1Dup2, op2Dup1, simdBaseType, simdSize);

                        msk = gtNewSimdBinOpNode(GT_AND_NOT, retType, msk2, msk3, simdBaseType, simdSize);
                    }

                    msk     = gtNewSimdIsNegativeNode(retType, msk, simdBaseType, simdSize);
                    retNode = gtNewSimdCndSelNode(retType, msk, ovf, tmpDup2, simdBaseType, simdSize);
                }
#endif
            }
            break;
        }

        case NI_Vector_AndNot:
        {
            assert(sig->numArgs == 2);

            // We don't want to support creating AND_NOT nodes prior to LIR
            // as it can break important optimizations. We'll produces this
            // in lowering instead so decompose into the individual operations
            // on import

            op2 = impSIMDPopStack();
            op1 = impSIMDPopStack();

            op2     = gtFoldExpr(gtNewSimdUnOpNode(GT_NOT, retType, op2, simdBaseType, simdSize));
            retNode = gtNewSimdBinOpNode(GT_AND, retType, op1, op2, simdBaseType, simdSize);
            break;
        }

        case NI_Vector_As:
        case NI_Vector_AsByte:
        case NI_Vector_AsDouble:
        case NI_Vector_AsInt16:
        case NI_Vector_AsInt32:
        case NI_Vector_AsInt64:
        case NI_Vector_AsNInt:
        case NI_Vector_AsNUInt:
        case NI_Vector_AsSByte:
        case NI_Vector_AsSingle:
        case NI_Vector_AsUInt16:
        case NI_Vector_AsUInt32:
        case NI_Vector_AsUInt64:
        case NI_Vector_AsVector4:
#if defined(TARGET_ARM64) || defined(TARGET_WASM)
        case NI_Vector_AsVector:
#endif
        {
            // We fold away the cast here, as it only exists to satisfy
            // the type system. It is safe to do this here since the retNode type
            // and the signature return type are both the same TYP_SIMD.

            assert(sig->numArgs == 1);
            retNode = impSIMDPopStack();

            assert(retNode->gtType == getSIMDTypeForSize(getSIMDTypeSizeInBytes(sig->retTypeSigClass)));
            break;
        }

#if defined(TARGET_XARCH)
        case NI_Vector_AsVector:
        case NI_Vector_AsVector256:
        case NI_Vector_AsVector512:
        {
            assert(sig->numArgs == 1);
            uint32_t vectorTByteLength = getVectorTByteLength();

            if (vectorTByteLength == 0)
            {
                // VectorT ISA was not present. Fall back to managed.
                break;
            }

            if (vectorTByteLength == simdSize)
            {
                // We fold away the cast here, as it only exists to satisfy
                // the type system. It is safe to do this here since the retNode type
                // and the signature return type are both the same TYP_SIMD.

                retNode = impSIMDPopStack();
                assert(retNode->gtType == getSIMDTypeForSize(getSIMDTypeSizeInBytes(sig->retTypeSigClass)));

                break;
            }

            // Vector<T> is a different size than the source/target SIMD type, so we should
            // treat this as a call to the appropriate narrowing or widening intrinsic.

            NamedIntrinsic convertIntrinsic = NI_Illegal;
            unsigned       convertSize      = 0;

            switch (vectorTByteLength)
            {
                case XMM_REGSIZE_BYTES:
                {
                    if (intrinsic == NI_Vector_AsVector)
                    {
                        if (simdSize == 64)
                        {
                            convertIntrinsic = NI_Vector_GetLower128;
                            convertSize      = 64;
                        }
                        else
                        {
                            assert(simdSize == 32);
                            convertIntrinsic = NI_Vector_GetLower;
                            convertSize      = 32;
                        }
                    }
                    else if (intrinsic == NI_Vector_AsVector512)
                    {
                        assert(simdSize == 64);
                        convertIntrinsic = NI_Vector_ToVector512;
                        convertSize      = 16;
                    }
                    else
                    {
                        assert(intrinsic == NI_Vector_AsVector256);
                        assert(simdSize == 32);

                        convertIntrinsic = NI_Vector_ToVector256;
                        convertSize      = 16;
                    }
                    break;
                }

                case YMM_REGSIZE_BYTES:
                {
                    if (intrinsic == NI_Vector_AsVector)
                    {
                        if (simdSize == 64)
                        {
                            convertIntrinsic = NI_Vector_GetLower;
                            convertSize      = 64;
                        }
                        else
                        {
                            assert(simdSize == 16);
                            convertIntrinsic = NI_Vector_ToVector256;
                            convertSize      = 16;
                        }
                    }
                    else
                    {
                        assert(intrinsic == NI_Vector_AsVector512);
                        assert(simdSize == 64);

                        convertIntrinsic = NI_Vector_ToVector512;
                        convertSize      = 32;
                    }
                    break;
                }

                case ZMM_REGSIZE_BYTES:
                {
                    if (intrinsic == NI_Vector_AsVector)
                    {
                        assert((simdSize == 16) || (simdSize == 32));
                        convertIntrinsic = NI_Vector_ToVector512;
                        convertSize      = simdSize;
                    }
                    else
                    {
                        assert(intrinsic == NI_Vector_AsVector256);
                        assert(simdSize == 32);

                        convertIntrinsic = NI_Vector_GetLower;
                        convertSize      = 64;
                    }
                    break;
                }

                default:
                {
                    unreached();
                }
            }

            assert(convertIntrinsic != NI_Illegal);
            assert(convertSize != 0);

            op1     = impSIMDPopStack();
            retNode = gtNewSimdHWIntrinsicNode(retType, op1, convertIntrinsic, simdBaseType, convertSize);

            break;
        }
#endif

        case NI_Vector_AsVector128:
        {
            assert(sig->numArgs == 1);
            assert(retType == TYP_SIMD16);

            switch (simdSize)
            {
                case 8:
                {
                    assert(simdBaseType == TYP_FLOAT);

#if defined(TARGET_WASM)
                    // TODO-WASM-SIMD: Implement NI_Vector_AsVector128(Vector2) - Need WithElement
                    return nullptr;
#endif

                    op1 = impSIMDPopStack();

                    if (op1->IsCnsVec())
                    {
                        GenTreeVecCon* vecCon = op1->AsVecCon();
                        vecCon->gtType        = TYP_SIMD16;

                        vecCon->gtSimdVal.f32[2] = 0.0f;
                        vecCon->gtSimdVal.f32[3] = 0.0f;

                        return vecCon;
                    }

#if defined(TARGET_ARM64)
                    op1 = gtNewSimdHWIntrinsicNode(retType, op1, NI_Vector_ToVector128Unsafe, simdBaseType, 8);
#else
                    op1 = gtNewSimdHWIntrinsicNode(retType, op1, NI_Vector_AsVector128Unsafe, simdBaseType, 8);
#endif

                    GenTree* idx  = gtNewIconNode(2, TYP_INT);
                    GenTree* zero = gtNewZeroConNode(TYP_FLOAT);
                    op1           = gtNewSimdWithElementNode(retType, op1, idx, zero, simdBaseType, 16);

                    idx     = gtNewIconNode(3, TYP_INT);
                    zero    = gtNewZeroConNode(TYP_FLOAT);
                    retNode = gtNewSimdWithElementNode(retType, op1, idx, zero, simdBaseType, 16);

                    break;
                }

                case 12:
                {
                    assert(simdBaseType == TYP_FLOAT);

#if defined(TARGET_WASM)
                    // TODO-WASM-SIMD: Implement NI_Vector_AsVector128(Vector3) - Need WithElement
                    return nullptr;
#endif

                    op1 = impSIMDPopStack();

                    if (op1->IsCnsVec())
                    {
                        GenTreeVecCon* vecCon = op1->AsVecCon();
                        vecCon->gtType        = TYP_SIMD16;

                        vecCon->gtSimdVal.f32[3] = 0.0f;
                        return vecCon;
                    }

                    op1 = gtNewSimdHWIntrinsicNode(retType, op1, NI_Vector_AsVector128Unsafe, simdBaseType, 12);

                    GenTree* idx  = gtNewIconNode(3, TYP_INT);
                    GenTree* zero = gtNewZeroConNode(TYP_FLOAT);
                    retNode       = gtNewSimdWithElementNode(retType, op1, idx, zero, simdBaseType, 16);
                    break;
                }

                case 16:
                {
                    // We fold away the cast here, as it only exists to satisfy
                    // the type system. It is safe to do this here since the retNode type
                    // and the signature return type are both the same TYP_SIMD.

                    retNode = impSIMDPopStack();
                    assert(retNode->gtType == getSIMDTypeForSize(getSIMDTypeSizeInBytes(sig->retTypeSigClass)));

                    break;
                }

#if defined(TARGET_XARCH)
                case 32:
                case 64:
                {
                    // Vector<T> is larger, so we should treat this as a call to the appropriate narrowing intrinsic
                    intrinsic = simdSize == YMM_REGSIZE_BYTES ? NI_Vector_GetLower : NI_Vector_GetLower128;

                    op1     = impSIMDPopStack();
                    retNode = gtNewSimdHWIntrinsicNode(retType, op1, intrinsic, simdBaseType, simdSize);
                    break;
                }
#endif

                default:
                {
                    unreached();
                }
            }

            break;
        }

        case NI_Vector_AsVector128Unsafe:
        {
            assert(sig->numArgs == 1);
            assert(retType == TYP_SIMD16);
            assert(simdBaseType == TYP_FLOAT);
            assert((simdSize == 8) || (simdSize == 12));

#if defined(TARGET_WASM)
            // TODO-WASM-SIMD: Implement NI_Vector_AsVector128Unsafe
            return nullptr;
#endif

            op1     = impSIMDPopStack();
            retNode = gtNewSimdHWIntrinsicNode(retType, op1, NI_Vector_AsVector128Unsafe, simdBaseType, simdSize);
            break;
        }

        case NI_Vector_AsVector2:
        case NI_Vector_AsVector3:
        {
            assert((simdSize == 16) && (simdBaseType == TYP_FLOAT));
            assert((retType == TYP_SIMD8) || (retType == TYP_SIMD12));

#if defined(TARGET_WASM)
            // TODO-WASM-SIMD: Implement NI_Vector_AsVector2/3
            return nullptr;
#endif

            assert(sig->numArgs == 1);
            op1 = impSIMDPopStack();

#if defined(TARGET_ARM64)
            if (retType == TYP_SIMD8)
            {
                retNode = gtNewSimdGetLowerNode(TYP_SIMD8, op1, simdBaseType, simdSize);
                break;
            }
#endif

            retNode = gtNewSimdHWIntrinsicNode(retType, op1, intrinsic, simdBaseType, simdSize);
            break;
        }

        case NI_Vector_Ceiling:
        {
            assert(sig->numArgs == 1);

            if (!varTypeIsFloating(simdBaseType))
            {
                retNode = impSIMDPopStack();
                break;
            }

            op1     = impSIMDPopStack();
            retNode = gtNewSimdCeilNode(retType, op1, simdBaseType, simdSize);
            break;
        }

        case NI_Vector_ConcatLowerLower:
        {
            isConcatIntrinsic = true;
            break;
        }

        case NI_Vector_ConcatLowerUpper:
        {
            isConcatIntrinsic = true;
            rightUpper        = true;
            break;
        }

        case NI_Vector_ConcatUpperLower:
        {
            isConcatIntrinsic = true;
            leftUpper         = true;
            break;
        }

        case NI_Vector_ConcatUpperUpper:
        {
            isConcatIntrinsic = true;
            leftUpper         = true;
            rightUpper        = true;
            break;
        }

        case NI_Vector_ConditionalSelect:
        {
            assert(sig->numArgs == 3);

            op3 = impSIMDPopStack();
            op2 = impSIMDPopStack();
            op1 = impSIMDPopStack();

            retNode = gtNewSimdCndSelNode(retType, op1, op2, op3, simdBaseType, simdSize);
            break;
        }

        case NI_Vector_ConvertToDouble:
        {
            assert(sig->numArgs == 1);
            assert(varTypeIsLong(simdBaseType));

#if defined(TARGET_XARCH)
            if (!compOpportunisticallyDependsOn(InstructionSet_AVX512))
            {
                break;
            }

            if (simdSize == 64)
            {
                intrinsic = NI_AVX512_ConvertToVector512Double;
            }
            else if (simdSize == 32)
            {
                intrinsic = NI_AVX512_ConvertToVector256Double;
            }
            else
            {
                assert(simdSize == 16);
                intrinsic = NI_AVX512_ConvertToVector128Double;
            }
#elif defined(TARGET_ARM64)
            if (simdSize == 16)
            {
                intrinsic = NI_AdvSimd_Arm64_ConvertToDouble;
            }
            else
            {
                assert(simdSize == 8);
                intrinsic = NI_AdvSimd_Arm64_ConvertToDoubleScalar;
            }
#elif defined(TARGET_WASM)
            // TODO-WASM-SIMD: Implement NI_Vector_ConvertToDouble
            return nullptr;
#else
            unreached();
#endif

            op1     = impSIMDPopStack();
            retNode = gtNewSimdHWIntrinsicNode(retType, op1, intrinsic, simdBaseType, simdSize);
            break;
        }

        case NI_Vector_ConvertToInt32:
        {
            assert(sig->numArgs == 1);
            assert(simdBaseType == TYP_FLOAT);

            op1     = impSIMDPopStack();
            retNode = gtNewSimdCvtNode(retType, op1, TYP_INT, simdBaseType, simdSize);
            break;
        }

        case NI_Vector_ConvertToInt32Native:
        {
            assert(sig->numArgs == 1);
            assert(simdBaseType == TYP_FLOAT);

            if (BlockNonDeterministicIntrinsics(mustExpand))
            {
                break;
            }

            op1     = impSIMDPopStack();
            retNode = gtNewSimdCvtNativeNode(retType, op1, TYP_INT, simdBaseType, simdSize);
            break;
        }

        case NI_Vector_ConvertToInt64:
        {
            assert(sig->numArgs == 1);
            assert(simdBaseType == TYP_DOUBLE);

#if defined(TARGET_XARCH)
            if (!compOpportunisticallyDependsOn(InstructionSet_AVX512))
            {
                break;
            }
#endif

#if defined(TARGET_WASM)
            // TODO-WASM-SIMD: Implement NI_Vector_ConvertToInt64
            return nullptr;
#endif

            op1     = impSIMDPopStack();
            retNode = gtNewSimdCvtNode(retType, op1, TYP_LONG, simdBaseType, simdSize);
            break;
        }

        case NI_Vector_ConvertToInt64Native:
        {
            assert(sig->numArgs == 1);
            assert(simdBaseType == TYP_DOUBLE);

            if (BlockNonDeterministicIntrinsics(mustExpand))
            {
                break;
            }

#if defined(TARGET_XARCH)
            if (!compOpportunisticallyDependsOn(InstructionSet_AVX512))
            {
                break;
            }
#endif

#if defined(TARGET_WASM)
            // TODO-WASM-SIMD: Implement NI_Vector_ConvertToInt64Native
            return nullptr;
#endif

            op1     = impSIMDPopStack();
            retNode = gtNewSimdCvtNativeNode(retType, op1, TYP_LONG, simdBaseType, simdSize);
            break;
        }

        case NI_Vector_ConvertToSingle:
        {
            assert(sig->numArgs == 1);
            assert(varTypeIsInt(simdBaseType));

#if defined(TARGET_XARCH)
            if (simdBaseType == TYP_INT)
            {
                if (simdSize == 64)
                {
                    intrinsic = NI_AVX512_ConvertToVector512Single;
                }
                else if (simdSize == 32)
                {
                    intrinsic = NI_AVX_ConvertToVector256Single;
                }
                else
                {
                    assert(simdSize == 16);
                    intrinsic = NI_X86Base_ConvertToVector128Single;
                }
            }
            else if (compOpportunisticallyDependsOn(InstructionSet_AVX512))
            {
                if (simdSize == 64)
                {
                    intrinsic = NI_AVX512_ConvertToVector512Single;
                }
                else if (simdSize == 32)
                {
                    intrinsic = NI_AVX512_ConvertToVector256Single;
                }
                else
                {
                    assert(simdSize == 16);
                    intrinsic = NI_AVX512_ConvertToVector128Single;
                }
            }
            else
            {
                break;
            }
#elif defined(TARGET_ARM64)
            intrinsic = NI_AdvSimd_ConvertToSingle;
#elif defined(TARGET_WASM)
            intrinsic = NI_PackedSimd_ConvertToSingle;
#else
            unreached();
#endif

            op1     = impSIMDPopStack();
            retNode = gtNewSimdHWIntrinsicNode(retType, op1, intrinsic, simdBaseType, simdSize);
            break;
        }

        case NI_Vector_ConvertToUInt32:
        {
            assert(sig->numArgs == 1);
            assert(simdBaseType == TYP_FLOAT);

#if defined(TARGET_XARCH)
            if (!compOpportunisticallyDependsOn(InstructionSet_AVX512))
            {
                break;
            }
#endif

            op1     = impSIMDPopStack();
            retNode = gtNewSimdCvtNode(retType, op1, TYP_UINT, simdBaseType, simdSize);
            break;
        }

        case NI_Vector_ConvertToUInt32Native:
        {
            assert(sig->numArgs == 1);
            assert(simdBaseType == TYP_FLOAT);

            if (BlockNonDeterministicIntrinsics(mustExpand))
            {
                break;
            }

#if defined(TARGET_XARCH)
            if (!compOpportunisticallyDependsOn(InstructionSet_AVX512))
            {
                break;
            }
#endif

            op1     = impSIMDPopStack();
            retNode = gtNewSimdCvtNativeNode(retType, op1, TYP_UINT, simdBaseType, simdSize);
            break;
        }

        case NI_Vector_ConvertToUInt64:
        {
            assert(sig->numArgs == 1);
            assert(simdBaseType == TYP_DOUBLE);

#if defined(TARGET_XARCH)
            if (!compOpportunisticallyDependsOn(InstructionSet_AVX512))
            {
                break;
            }
#endif

#if defined(TARGET_WASM)
            // TODO-WASM-SIMD: Implement NI_Vector_ConvertToUInt64
            return nullptr;
#endif

            op1     = impSIMDPopStack();
            retNode = gtNewSimdCvtNode(retType, op1, TYP_ULONG, simdBaseType, simdSize);
            break;
        }

        case NI_Vector_ConvertToUInt64Native:
        {
            assert(sig->numArgs == 1);
            assert(simdBaseType == TYP_DOUBLE);

            if (BlockNonDeterministicIntrinsics(mustExpand))
            {
                break;
            }

#if defined(TARGET_XARCH)
            if (!compOpportunisticallyDependsOn(InstructionSet_AVX512))
            {
                break;
            }
#endif

#if defined(TARGET_WASM)
            // TODO-WASM-SIMD: Implement NI_Vector_ConvertToUInt64Native
            return nullptr;
#endif

            op1     = impSIMDPopStack();
            retNode = gtNewSimdCvtNativeNode(retType, op1, TYP_ULONG, simdBaseType, simdSize);
            break;
        }

        case NI_Vector_Create:
        {
            retNode = impSimdCreate(intrinsic, sig, simdBaseType, retType, simdSize);
            break;
        }

        case NI_Vector_CreateAlternatingSequence:
        {
            assert(sig->numArgs == 2);

            impSpillSideEffect(true, stackState.esStackDepth -
                                         2 DEBUGARG("Spilling op1 side effects for vector CreateAlternatingSequence"));

#if defined(TARGET_WASM)
            if (!impStackTop(0).val->OperIsConst() || !impStackTop(1).val->OperIsConst())
            {
                // TODO-WASM-SIMD: Implement NI_Vector_CreateAlternatingSequence - Need Shuffle
                return nullptr;
            }
#endif

            op2 = impPopStack().val;
            op1 = impPopStack().val;

            retNode = gtNewSimdCreateAlternatingSequenceNode(retType, op1, op2, simdBaseType, simdSize);
            break;
        }

        case NI_Vector_CreateGeometricSequence:
        {
            assert(sig->numArgs == 2);

            bool multiplierIsConst = impStackTop(0).val->OperIsConst();
            bool initialIsConst    = impStackTop(1).val->OperIsConst();
            bool canGenerate       = multiplierIsConst;

#if !defined(TARGET_XARCH)
            if (canGenerate && !initialIsConst)
            {
#if defined(TARGET_ARM64)
                canGenerate = !varTypeIsLong(simdBaseType) || (simdSize == 8);
#else
                canGenerate = false;
#endif
            }
#endif

            if (!canGenerate)
            {
                if (opts.OptimizationEnabled())
                {
                    op2 = impPopStack().val;
                    op1 = impPopStack().val;

                    retNode = gtNewSimdHWIntrinsicNode(retType, op1, op2, intrinsic, simdBaseType, simdSize);
                    retNode->AsHWIntrinsic()->SetMethodHandle(this, method R2RARG(*entryPoint));
                }
                break;
            }

            impSpillSideEffect(true, stackState.esStackDepth -
                                         2 DEBUGARG("Spilling op1 side effects for vector CreateGeometricSequence"));

            op2 = impPopStack().val;
            op1 = impPopStack().val;

            retNode = gtNewSimdCreateGeometricSequenceNode(retType, op1, op2, simdBaseType, simdSize);
            break;
        }

        case NI_Vector_CreateScalar:
        {
            assert(sig->numArgs == 1);

#if defined(TARGET_WASM)
            if (!impStackTop(0).val->OperIsConst())
            {
                // TODO-WASM-SIMD: Implement NI_Vector_CreateScalar
                break;
            }
#endif

            op1     = impPopStack().val;
            retNode = gtNewSimdCreateScalarNode(retType, op1, simdBaseType, simdSize);
            break;
        }

        case NI_Vector_CreateScalarUnsafe:
        {
            assert(sig->numArgs == 1);

#if defined(TARGET_WASM)
            if (!impStackTop(0).val->OperIsConst())
            {
                // TODO-WASM-SIMD: Implement NI_Vector_CreateScalarUnsafe
                break;
            }
#endif

            op1     = impPopStack().val;
            retNode = gtNewSimdCreateScalarUnsafeNode(retType, op1, simdBaseType, simdSize);
            break;
        }

        case NI_Vector_CreateSequence:
        {
            assert(sig->numArgs == 2);

#if defined(TARGET_ARM64)
            if (varTypeIsLong(simdBaseType) && !impStackTop(0).val->OperIsConst())
            {
                // TODO-ARM64-CQ: We should support long/ulong multiplication.
                break;
            }
#endif

            impSpillSideEffect(true, stackState.esStackDepth -
                                         2 DEBUGARG("Spilling op1 side effects for vector CreateSequence"));

            op2 = impPopStack().val;
            op1 = impPopStack().val;

            retNode = gtNewSimdCreateSequenceNode(retType, op1, op2, simdBaseType, simdSize);
            break;
        }

        case NI_Vector_Dot:
        {
            assert(sig->numArgs == 2);

#if defined(TARGET_WASM)
            // TODO-WASM-SIMD: Implement NI_Vector_Dot - Need Shuffle
            return nullptr;
#endif

#if defined(TARGET_ARM64)
            if (varTypeIsLong(simdBaseType))
            {
                break;
            }
#endif

            var_types simdType = getSIMDTypeForSize(simdSize);

            op2 = impSIMDPopStack();
            op1 = impSIMDPopStack();

#if defined(TARGET_XARCH)
            if ((simdSize == 64) || varTypeIsByte(simdBaseType) || varTypeIsLong(simdBaseType))
            {
                // The lowering for Dot doesn't handle these cases, so import as Sum(left * right)
                retNode = gtNewSimdBinOpNode(GT_MUL, simdType, op1, op2, simdBaseType, simdSize);
                retNode = gtNewSimdSumNode(retType, retNode, simdBaseType, simdSize);
                break;
            }
#endif

            retNode = gtNewSimdDotProdNode(simdType, op1, op2, simdBaseType, simdSize);
            retNode = gtNewSimdToScalarNode(retType, retNode, simdBaseType, simdSize);
            break;
        }

        case NI_Vector_Equals:
        {
            assert(sig->numArgs == 2);

            op2 = impSIMDPopStack();
            op1 = impSIMDPopStack();

            retNode = gtNewSimdCmpOpNode(GT_EQ, retType, op1, op2, simdBaseType, simdSize);
            break;
        }

        case NI_Vector_EqualsAny:
        {
            assert(sig->numArgs == 2);

            op2 = impSIMDPopStack();
            op1 = impSIMDPopStack();

            retNode = gtNewSimdCmpOpAnyNode(GT_EQ, retType, op1, op2, simdBaseType, simdSize);
            break;
        }

        case NI_Vector_ExtractMostSignificantBits:
        {
            assert(sig->numArgs == 1);
            op1 = impSIMDPopStack();

#if defined(TARGET_XARCH)
            if ((simdSize == 64) || canUseEvexEncoding())
            {
                op1     = gtFoldExpr(gtNewSimdCvtVectorToMaskNode(TYP_MASK, op1, simdBaseType, simdSize));
                retNode = gtNewSimdHWIntrinsicNode(retType, op1, NI_AVX512_MoveMask, simdBaseType, simdSize);
                break;
            }

            switch (simdBaseType)
            {
                case TYP_BYTE:
                case TYP_UBYTE:
                {
                    intrinsic = (simdSize == 32) ? NI_AVX2_MoveMask : NI_X86Base_MoveMask;
                    break;
                }

                case TYP_SHORT:
                case TYP_USHORT:
                {
                    break;
                }

                case TYP_INT:
                case TYP_UINT:
                case TYP_FLOAT:
                {
                    simdBaseType = TYP_FLOAT;
                    intrinsic    = (simdSize == 32) ? NI_AVX_MoveMask : NI_X86Base_MoveMask;
                    break;
                }

                case TYP_LONG:
                case TYP_ULONG:
                case TYP_DOUBLE:
                {
                    simdBaseType = TYP_DOUBLE;
                    intrinsic    = (simdSize == 32) ? NI_AVX_MoveMask : NI_X86Base_MoveMask;
                    break;
                }

                default:
                {
                    unreached();
                }
            }
#elif defined(TARGET_WASM)
            if (simdBaseType == TYP_FLOAT)
            {
                simdBaseType = TYP_INT;
            }
            else if (simdBaseType == TYP_DOUBLE)
            {
                simdBaseType = TYP_LONG;
            }
            else
            {
                assert(varTypeIsIntegral(simdBaseType));
            }

            intrinsic = NI_PackedSimd_Bitmask;
#endif

            retNode = gtNewSimdHWIntrinsicNode(retType, op1, intrinsic, simdBaseType, simdSize);
            break;
        }

        case NI_Vector_Floor:
        {
            assert(sig->numArgs == 1);

            if (!varTypeIsFloating(simdBaseType))
            {
                retNode = impSIMDPopStack();
                break;
            }

            op1     = impSIMDPopStack();
            retNode = gtNewSimdFloorNode(retType, op1, simdBaseType, simdSize);
            break;
        }

        case NI_Vector_FusedMultiplyAdd:
        {
            assert(sig->numArgs == 3);
            assert(varTypeIsFloating(simdBaseType));

#if defined(TARGET_XARCH)
            if (!compOpportunisticallyDependsOn(InstructionSet_AVX2))
            {
                break;
            }
#elif defined(TARGET_ARM64)
            impSpillSideEffect(true,
                               stackState.esStackDepth - 3 DEBUGARG("Spilling op1 side effects for FusedMultiplyAdd"));

            impSpillSideEffect(true,
                               stackState.esStackDepth - 2 DEBUGARG("Spilling op2 side effects for FusedMultiplyAdd"));
#elif defined(TARGET_WASM)
            // TODO-WASM-SIMD: Implement NI_Vector_FusedMultiplyAdd
            return nullptr;
#endif

            op3 = impSIMDPopStack();
            op2 = impSIMDPopStack();
            op1 = impSIMDPopStack();

            retNode = gtNewSimdFmaNode(retType, op1, op2, op3, simdBaseType, simdSize);
            break;
        }

        case NI_Vector_GetElement:
        {
            assert(sig->numArgs == 2);

#if defined(TARGET_WASM)
            // TODO-WASM-SIMD: Implement NI_Vector_GetElement
            return nullptr;
#endif

            op2 = impPopStack().val;
            op1 = impSIMDPopStack();

            retNode = gtNewSimdGetElementNode(retType, op1, op2, simdBaseType, simdSize);
            break;
        }

#if defined(TARGET_XARCH) || defined(TARGET_ARM64)
        case NI_Vector_GetLower:
        {
            assert(sig->numArgs == 1);

#if defined(TARGET_XARCH)
            if (simdSize == 8)
            {
                break;
            }
#endif

            op1     = impSIMDPopStack();
            retNode = gtNewSimdGetLowerNode(retType, op1, simdBaseType, simdSize);
            break;
        }

        case NI_Vector_GetUpper:
        {
            assert(sig->numArgs == 1);

#if defined(TARGET_XARCH)
            if (simdSize == 8)
            {
                break;
            }
#endif

            op1     = impSIMDPopStack();
            retNode = gtNewSimdGetUpperNode(retType, op1, simdBaseType, simdSize);
            break;
        }
#endif // !TARGET_XARCH && !TARGET_ARM64

        case NI_Vector_GreaterThan:
        {
            assert(sig->numArgs == 2);

            op2 = impSIMDPopStack();
            op1 = impSIMDPopStack();

            retNode = gtNewSimdCmpOpNode(GT_GT, retType, op1, op2, simdBaseType, simdSize);
            break;
        }

        case NI_Vector_GreaterThanAll:
        {
            assert(sig->numArgs == 2);

            op2 = impSIMDPopStack();
            op1 = impSIMDPopStack();

            retNode = gtNewSimdCmpOpAllNode(GT_GT, retType, op1, op2, simdBaseType, simdSize);
            break;
        }

        case NI_Vector_GreaterThanAny:
        {
            assert(sig->numArgs == 2);

            op2 = impSIMDPopStack();
            op1 = impSIMDPopStack();

            retNode = gtNewSimdCmpOpAnyNode(GT_GT, retType, op1, op2, simdBaseType, simdSize);
            break;
        }

        case NI_Vector_GreaterThanOrEqual:
        {
            assert(sig->numArgs == 2);

            op2 = impSIMDPopStack();
            op1 = impSIMDPopStack();

            retNode = gtNewSimdCmpOpNode(GT_GE, retType, op1, op2, simdBaseType, simdSize);
            break;
        }

        case NI_Vector_GreaterThanOrEqualAll:
        {
            assert(sig->numArgs == 2);

            op2 = impSIMDPopStack();
            op1 = impSIMDPopStack();

            retNode = gtNewSimdCmpOpAllNode(GT_GE, retType, op1, op2, simdBaseType, simdSize);
            break;
        }

        case NI_Vector_GreaterThanOrEqualAny:
        {
            assert(sig->numArgs == 2);

            op2 = impSIMDPopStack();
            op1 = impSIMDPopStack();

            retNode = gtNewSimdCmpOpAnyNode(GT_GE, retType, op1, op2, simdBaseType, simdSize);
            break;
        }

        case NI_Vector_IsEvenInteger:
        {
            assert(sig->numArgs == 1);

            if (varTypeIsFloating(simdBaseType))
            {
                // The code for handling floating-point is decently complex but also expected
                // to be rare, so we fallback to the managed implementation, which is accelerated
                break;
            }

            op1     = impSIMDPopStack();
            retNode = gtNewSimdIsEvenIntegerNode(retType, op1, simdBaseType, simdSize);
            break;
        }

        case NI_Vector_IsFinite:
        {
            assert(sig->numArgs == 1);
            op1     = impSIMDPopStack();
            retNode = gtNewSimdIsFiniteNode(retType, op1, simdBaseType, simdSize);
            break;
        }

        case NI_Vector_IsInfinity:
        {
            assert(sig->numArgs == 1);
            op1     = impSIMDPopStack();
            retNode = gtNewSimdIsInfinityNode(retType, op1, simdBaseType, simdSize);
            break;
        }

        case NI_Vector_IsInteger:
        {
            assert(sig->numArgs == 1);
            op1     = impSIMDPopStack();
            retNode = gtNewSimdIsIntegerNode(retType, op1, simdBaseType, simdSize);
            break;
        }

        case NI_Vector_IsNaN:
        {
            assert(sig->numArgs == 1);
            op1     = impSIMDPopStack();
            retNode = gtNewSimdIsNaNNode(retType, op1, simdBaseType, simdSize);
            break;
        }

        case NI_Vector_IsNegative:
        {
            assert(sig->numArgs == 1);
            op1     = impSIMDPopStack();
            retNode = gtNewSimdIsNegativeNode(retType, op1, simdBaseType, simdSize);
            break;
        }

        case NI_Vector_IsNegativeInfinity:
        {
            assert(sig->numArgs == 1);
            op1     = impSIMDPopStack();
            retNode = gtNewSimdIsNegativeInfinityNode(retType, op1, simdBaseType, simdSize);
            break;
        }

        case NI_Vector_IsNormal:
        {
            assert(sig->numArgs == 1);
            op1     = impSIMDPopStack();
            retNode = gtNewSimdIsNormalNode(retType, op1, simdBaseType, simdSize);
            break;
        }

        case NI_Vector_IsOddInteger:
        {
            assert(sig->numArgs == 1);

            if (varTypeIsFloating(simdBaseType))
            {
                // The code for handling floating-point is decently complex but also expected
                // to be rare, so we fallback to the managed implementation, which is accelerated
                break;
            }

            op1     = impSIMDPopStack();
            retNode = gtNewSimdIsOddIntegerNode(retType, op1, simdBaseType, simdSize);
            break;
        }

        case NI_Vector_IsPositive:
        {
            assert(sig->numArgs == 1);
            op1     = impSIMDPopStack();
            retNode = gtNewSimdIsPositiveNode(retType, op1, simdBaseType, simdSize);
            break;
        }

        case NI_Vector_IsPositiveInfinity:
        {
            assert(sig->numArgs == 1);
            op1     = impSIMDPopStack();
            retNode = gtNewSimdIsPositiveInfinityNode(retType, op1, simdBaseType, simdSize);
            break;
        }

        case NI_Vector_IsSubnormal:
        {
            assert(sig->numArgs == 1);
            op1     = impSIMDPopStack();
            retNode = gtNewSimdIsSubnormalNode(retType, op1, simdBaseType, simdSize);
            break;
        }

        case NI_Vector_IsZero:
        {
            assert(sig->numArgs == 1);
            op1     = impSIMDPopStack();
            retNode = gtNewSimdIsZeroNode(retType, op1, simdBaseType, simdSize);
            break;
        }

        case NI_Vector_LessThan:
        {
            assert(sig->numArgs == 2);

            op2 = impSIMDPopStack();
            op1 = impSIMDPopStack();

            retNode = gtNewSimdCmpOpNode(GT_LT, retType, op1, op2, simdBaseType, simdSize);
            break;
        }

        case NI_Vector_LessThanAll:
        {
            assert(sig->numArgs == 2);

            op2 = impSIMDPopStack();
            op1 = impSIMDPopStack();

            retNode = gtNewSimdCmpOpAllNode(GT_LT, retType, op1, op2, simdBaseType, simdSize);
            break;
        }

        case NI_Vector_LessThanAny:
        {
            assert(sig->numArgs == 2);

            op2 = impSIMDPopStack();
            op1 = impSIMDPopStack();

            retNode = gtNewSimdCmpOpAnyNode(GT_LT, retType, op1, op2, simdBaseType, simdSize);
            break;
        }

        case NI_Vector_LessThanOrEqual:
        {
            assert(sig->numArgs == 2);

            op2 = impSIMDPopStack();
            op1 = impSIMDPopStack();

            retNode = gtNewSimdCmpOpNode(GT_LE, retType, op1, op2, simdBaseType, simdSize);
            break;
        }

        case NI_Vector_LessThanOrEqualAll:
        {
            assert(sig->numArgs == 2);

            op2 = impSIMDPopStack();
            op1 = impSIMDPopStack();

            retNode = gtNewSimdCmpOpAllNode(GT_LE, retType, op1, op2, simdBaseType, simdSize);
            break;
        }

        case NI_Vector_LessThanOrEqualAny:
        {
            assert(sig->numArgs == 2);

            op2 = impSIMDPopStack();
            op1 = impSIMDPopStack();

            retNode = gtNewSimdCmpOpAnyNode(GT_LE, retType, op1, op2, simdBaseType, simdSize);
            break;
        }

        case NI_Vector_LoadAligned:
        {
            assert(sig->numArgs == 1);

#if defined(TARGET_ARM64) || defined(TARGET_WASM)
            if (opts.OptimizationDisabled())
            {
                // ARM64/WASM doesn't have aligned loads, but aligned loads are only validated to be
                // aligned when optimizations are disable, so only skip the intrinsic handling
                // if optimizations are enabled
                break;
            }
#endif

            op1 = impPopStack().val;

            if (op1->OperIs(GT_CAST) && op1->gtGetOp1()->TypeIs(TYP_BYREF))
            {
                // If what we have is a BYREF, that's what we really want, so throw away the cast.
                op1 = op1->gtGetOp1();
            }

            retNode = gtNewSimdLoadAlignedNode(retType, op1, simdBaseType, simdSize);
            break;
        }

        case NI_Vector_LoadAlignedNonTemporal:
        {
            assert(sig->numArgs == 1);

#if defined(TARGET_ARM64) || defined(TARGET_WASM)
            if (opts.OptimizationDisabled())
            {
                // ARM64/WASM doesn't have aligned loads, but aligned loads are only validated to be
                // aligned when optimizations are disable, so only skip the intrinsic handling
                // if optimizations are enabled
                break;
            }
#endif

            op1 = impPopStack().val;

            if (op1->OperIs(GT_CAST) && op1->gtGetOp1()->TypeIs(TYP_BYREF))
            {
                // If what we have is a BYREF, that's what we really want, so throw away the cast.
                op1 = op1->gtGetOp1();
            }

            retNode = gtNewSimdLoadNonTemporalNode(retType, op1, simdBaseType, simdSize);
            break;
        }

        case NI_Vector_LoadUnsafe:
        {
            if (sig->numArgs == 2)
            {
                op2 = impPopStack().val;
            }
            else
            {
                assert(sig->numArgs == 1);
            }

            op1 = impPopStack().val;

            if (op1->OperIs(GT_CAST) && op1->gtGetOp1()->TypeIs(TYP_BYREF))
            {
                // If what we have is a BYREF, that's what we really want, so throw away the cast.
                op1 = op1->gtGetOp1();
            }

            if (sig->numArgs == 2)
            {
                op3 = gtNewIconNode(genTypeSize(simdBaseType), op2->TypeGet());
                op2 = gtNewOperNode(GT_MUL, op2->TypeGet(), op2, op3);
                op1 = gtNewOperNode(GT_ADD, op1->TypeGet(), op1, op2);
            }

            retNode = gtNewSimdLoadNode(retType, op1, simdBaseType, simdSize);
            break;
        }

        case NI_Vector_Max:
        {
            isMinMaxIntrinsic = true;
            isMax             = true;
            break;
        }

        case NI_Vector_MaxMagnitude:
        {
            isMinMaxIntrinsic = true;
            isMax             = true;
            isMagnitude       = true;
            break;
        }

        case NI_Vector_MaxMagnitudeNumber:
        {
            isMinMaxIntrinsic = true;
            isMax             = true;
            isMagnitude       = true;
            isNumber          = true;
            break;
        }

        case NI_Vector_MaxNative:
        {
            isMinMaxIntrinsic = true;
            isMax             = true;
            isNative          = true;
            break;
        }

        case NI_Vector_MaxNumber:
        {
            isMinMaxIntrinsic = true;
            isMax             = true;
            isNumber          = true;
            break;
        }

        case NI_Vector_Min:
        {
            isMinMaxIntrinsic = true;
            break;
        }

        case NI_Vector_MinMagnitude:
        {
            isMinMaxIntrinsic = true;
            isMagnitude       = true;
            break;
        }

        case NI_Vector_MinMagnitudeNumber:
        {
            isMinMaxIntrinsic = true;
            isMagnitude       = true;
            isNumber          = true;
            break;
        }

        case NI_Vector_MinNative:
        {
            isMinMaxIntrinsic = true;
            isNative          = true;
            break;
        }

        case NI_Vector_MinNumber:
        {
            isMinMaxIntrinsic = true;
            isNumber          = true;
            break;
        }

        case NI_Vector_MultiplyAddEstimate:
        {
            assert(sig->numArgs == 3);

            if (BlockNonDeterministicIntrinsics(mustExpand))
            {
                break;
            }

#if defined(TARGET_ARM64)
            if (varTypeIsFloating(simdBaseType))
            {
                impSpillSideEffect(true, stackState.esStackDepth -
                                             3 DEBUGARG("Spilling op1 side effects for MultiplyAddEstimate"));

                impSpillSideEffect(true, stackState.esStackDepth -
                                             2 DEBUGARG("Spilling op2 side effects for MultiplyAddEstimate"));
            }
#endif

            op3 = impSIMDPopStack();
            op2 = impSIMDPopStack();
            op1 = impSIMDPopStack();

            bool isFmaSupported = varTypeIsFloating(simdBaseType);

#if defined(TARGET_XARCH)
            if (isFmaSupported)
            {
                isFmaSupported = compExactlyDependsOn(InstructionSet_AVX2);
            }
#elif defined(TARGET_WASM)
            isFmaSupported = false;
#endif

            if (isFmaSupported)
            {
                retNode = gtNewSimdFmaNode(retType, op1, op2, op3, simdBaseType, simdSize);
            }
            else
            {
                GenTree* mulNode = gtNewSimdBinOpNode(GT_MUL, retType, op1, op2, simdBaseType, simdSize);
                retNode          = gtNewSimdBinOpNode(GT_ADD, retType, mulNode, op3, simdBaseType, simdSize);
            }
            break;
        }

        case NI_Vector_Narrow:
        {
            assert(sig->numArgs == 2);

#if defined(TARGET_WASM)
            // TODO-WASM-SIMD: Implement NI_Vector_Narrow
            return nullptr;
#endif

            op2 = impSIMDPopStack();
            op1 = impSIMDPopStack();

            retNode = gtNewSimdNarrowNode(retType, op1, op2, simdBaseType, simdSize);
            break;
        }

        case NI_Vector_NarrowWithSaturation:
        {
            assert(sig->numArgs == 2);

#if defined(TARGET_XARCH)
            op2 = impSIMDPopStack();
            op1 = impSIMDPopStack();

            if (simdBaseType == TYP_DOUBLE)
            {
                // gtNewSimdNarrowNode uses the base type of the return for the simdBaseType
                retNode = gtNewSimdNarrowNode(retType, op1, op2, TYP_FLOAT, simdSize);
            }
            else if ((simdSize == 16) && ((simdBaseType == TYP_SHORT) || (simdBaseType == TYP_INT)))
            {
                // PackSignedSaturate uses the base type of the return for the simdBaseType
                simdBaseType = (simdBaseType == TYP_SHORT) ? TYP_BYTE : TYP_SHORT;

                intrinsic = NI_X86Base_PackSignedSaturate;
                retNode   = gtNewSimdHWIntrinsicNode(retType, op1, op2, intrinsic, simdBaseType, simdSize);
            }
            else if (compOpportunisticallyDependsOn(InstructionSet_AVX512))
            {
                if ((simdSize == 32) || (simdSize == 64))
                {
                    if (simdSize == 32)
                    {
                        intrinsic = NI_Vector_ToVector512Unsafe;

                        op1 = gtNewSimdHWIntrinsicNode(TYP_SIMD64, op1, intrinsic, simdBaseType, simdSize);
                        op1 = gtNewSimdWithUpperNode(TYP_SIMD64, op1, op2, simdBaseType, simdSize * 2);
                    }

                    switch (simdBaseType)
                    {
                        case TYP_SHORT:
                        {
                            intrinsic = NI_AVX512_ConvertToVector256SByteWithSaturation;
                            break;
                        }

                        case TYP_USHORT:
                        {
                            intrinsic = NI_AVX512_ConvertToVector256ByteWithSaturation;
                            break;
                        }

                        case TYP_INT:
                        {
                            intrinsic = NI_AVX512_ConvertToVector256Int16WithSaturation;
                            break;
                        }

                        case TYP_UINT:
                        {
                            intrinsic = NI_AVX512_ConvertToVector256UInt16WithSaturation;
                            break;
                        }

                        case TYP_LONG:
                        {
                            intrinsic = NI_AVX512_ConvertToVector256Int32WithSaturation;
                            break;
                        }

                        case TYP_ULONG:
                        {
                            intrinsic = NI_AVX512_ConvertToVector256UInt32WithSaturation;
                            break;
                        }

                        default:
                        {
                            unreached();
                        }
                    }
                }
                else
                {
                    assert(simdSize == 16);
                    intrinsic = NI_Vector_ToVector256Unsafe;

                    op1 = gtNewSimdHWIntrinsicNode(TYP_SIMD32, op1, intrinsic, simdBaseType, simdSize);
                    op1 = gtNewSimdWithUpperNode(TYP_SIMD32, op1, op2, simdBaseType, simdSize * 2);

                    switch (simdBaseType)
                    {
                        case TYP_USHORT:
                        {
                            intrinsic = NI_AVX512_ConvertToVector128ByteWithSaturation;
                            break;
                        }

                        case TYP_UINT:
                        {
                            intrinsic = NI_AVX512_ConvertToVector128UInt16WithSaturation;
                            break;
                        }

                        case TYP_LONG:
                        {
                            intrinsic = NI_AVX512_ConvertToVector128Int32WithSaturation;
                            break;
                        }

                        case TYP_ULONG:
                        {
                            intrinsic = NI_AVX512_ConvertToVector128UInt32WithSaturation;
                            break;
                        }

                        default:
                        {
                            unreached();
                        }
                    }
                }

                if (simdSize == 64)
                {
                    op1 = gtNewSimdHWIntrinsicNode(TYP_SIMD32, op1, intrinsic, simdBaseType, simdSize);
                    op2 = gtNewSimdHWIntrinsicNode(TYP_SIMD32, op2, intrinsic, simdBaseType, simdSize);

                    retNode = gtNewSimdWithUpperNode(retType, op1, op2, simdBaseType, simdSize);
                }
                else
                {
                    retNode = gtNewSimdHWIntrinsicNode(retType, op1, intrinsic, simdBaseType, simdSize * 2);
                }
            }
            else
            {
                // gtNewSimdNarrowNode uses the base type of the return for the simdBaseType
                var_types narrowSimdBaseType;

                GenTreeVecCon* minCns = varTypeIsSigned(simdBaseType) ? gtNewVconNode(retType) : nullptr;
                GenTreeVecCon* maxCns = gtNewVconNode(retType);

                switch (simdBaseType)
                {
                    case TYP_SHORT:
                    {
                        minCns->EvaluateBroadcastInPlace<int16_t>(INT8_MIN);
                        maxCns->EvaluateBroadcastInPlace<int16_t>(INT8_MAX);

                        narrowSimdBaseType = TYP_BYTE;
                        break;
                    }

                    case TYP_USHORT:
                    {
                        maxCns->EvaluateBroadcastInPlace<uint16_t>(UINT8_MAX);
                        narrowSimdBaseType = TYP_UBYTE;
                        break;
                    }

                    case TYP_INT:
                    {
                        minCns->EvaluateBroadcastInPlace<int32_t>(INT16_MIN);
                        maxCns->EvaluateBroadcastInPlace<int32_t>(INT16_MAX);

                        narrowSimdBaseType = TYP_SHORT;
                        break;
                    }

                    case TYP_UINT:
                    {
                        maxCns->EvaluateBroadcastInPlace<uint32_t>(UINT16_MAX);
                        narrowSimdBaseType = TYP_USHORT;
                        break;
                    }

                    case TYP_LONG:
                    {
                        minCns->EvaluateBroadcastInPlace<int64_t>(INT32_MIN);
                        maxCns->EvaluateBroadcastInPlace<int64_t>(INT32_MAX);

                        narrowSimdBaseType = TYP_INT;
                        break;
                    }

                    case TYP_ULONG:
                    {
                        maxCns->EvaluateBroadcastInPlace<uint64_t>(UINT32_MAX);
                        narrowSimdBaseType = TYP_UINT;
                        break;
                    }

                    default:
                    {
                        unreached();
                    }
                }

                // This does a clamp which is defined as: Min(Max(value, min), max)
                // which means that we do a max computation if a minimum constant is specified
                // There will be none specified for unsigned to unsigned narrowing since
                // they share a lower bound (0) and will already be correct.

                if (minCns != nullptr)
                {
                    op1 = gtNewSimdMinMaxNode(retType, op1, minCns, simdBaseType, simdSize, /* isMax */ true,
                                              /* isMagnitude */ false, /* isNumber */ false);
                    op2 = gtNewSimdMinMaxNode(retType, op2, gtCloneExpr(minCns), simdBaseType, simdSize,
                                              /* isMax */ true, /* isMagnitude */ false, /* isNumber */ false);
                }

                op1 = gtNewSimdMinMaxNode(retType, op1, maxCns, simdBaseType, simdSize, /* isMax */ false,
                                          /* isMagnitude */ false, /* isNumber */ false);
                op2 = gtNewSimdMinMaxNode(retType, op2, gtCloneExpr(maxCns), simdBaseType, simdSize,
                                          /* isMax */ false, /* isMagnitude */ false, /* isNumber */ false);

                retNode = gtNewSimdNarrowNode(retType, op1, op2, narrowSimdBaseType, simdSize);
            }
#elif defined(TARGET_ARM64)
            op2 = impSIMDPopStack();
            op1 = impSIMDPopStack();

            if (varTypeIsFloating(simdBaseType))
            {
                retNode = gtNewSimdNarrowNode(retType, op1, op2, simdBaseType, simdSize);
            }
            else if (simdSize == 16)
            {
                intrinsic = NI_AdvSimd_ExtractNarrowingSaturateLower;
                op1       = gtNewSimdHWIntrinsicNode(TYP_SIMD8, op1, intrinsic, simdBaseType, 8);

                intrinsic = NI_AdvSimd_ExtractNarrowingSaturateUpper;
                retNode   = gtNewSimdHWIntrinsicNode(retType, op1, op2, intrinsic, simdBaseType, simdSize);
            }
            else
            {
                intrinsic = NI_Vector_ToVector128Unsafe;
                op1       = gtNewSimdHWIntrinsicNode(TYP_SIMD16, op1, intrinsic, simdBaseType, simdSize);

                op1 = gtNewSimdWithUpperNode(TYP_SIMD16, op1, op2, simdBaseType, 16);

                intrinsic = NI_AdvSimd_ExtractNarrowingSaturateLower;
                retNode   = gtNewSimdHWIntrinsicNode(retType, op1, intrinsic, simdBaseType, simdSize);
            }
#elif defined(TARGET_WASM)
            // TODO-WASM-SIMD: Implement NI_Vector_NarrowWithSaturation
            return nullptr;
#else
            unreached();
#endif
            break;
        }

        case NI_Vector_Reverse:
        {
            assert(sig->numArgs == 1);

#if defined(TARGET_WASM)
            // TODO-WASM-SIMD: Implement NI_Vector_Reverse - Need Shuffle
            return nullptr;
#endif

#if defined(TARGET_XARCH)
            if ((simdSize == 64) && varTypeIsByte(simdBaseType))
            {
                if (!compOpportunisticallyDependsOn(InstructionSet_AVX512v2))
                {
                    break;
                }
            }
#endif

            op1     = impSIMDPopStack();
            retNode = gtNewSimdReverseNode(retType, op1, simdBaseType, simdSize);
            break;
        }

        case NI_Vector_Round:
        {
            if (sig->numArgs != 1)
            {
                break;
            }

            if (!varTypeIsFloating(simdBaseType))
            {
                retNode = impSIMDPopStack();
                break;
            }

            op1     = impSIMDPopStack();
            retNode = gtNewSimdRoundNode(retType, op1, simdBaseType, simdSize);
            break;
        }

        case NI_Vector_ShiftLeft:
        {
            assert(sig->numArgs == 2);

            if (!varTypeIsSIMD(impStackTop(0).val))
            {
                // We just want the inlining profitability boost for the helper intrinsics/
                // that have operator alternatives like `simd << int`
                break;
            }

#if defined(TARGET_WASM)
            // TODO-WASM-SIMD: Implement NI_Vector_ShiftLeft
            return nullptr;
#endif

#if defined(TARGET_XARCH)
            if ((simdSize == 16) && !compOpportunisticallyDependsOn(InstructionSet_AVX2))
            {
                break;
            }
#endif

            op2 = impSIMDPopStack();
            op1 = impSIMDPopStack();

#if defined(TARGET_XARCH)
            if (simdSize == 64)
            {
                intrinsic = NI_AVX512_ShiftLeftLogicalVariable;
            }
            else
            {
                assert((simdSize == 16) || (simdSize == 32));
                intrinsic = NI_AVX2_ShiftLeftLogicalVariable;
            }
#elif defined(TARGET_ARM64)
            if (simdSize == 16)
            {
                intrinsic = NI_AdvSimd_ShiftLogical;
            }
            else
            {
                assert(simdSize == 8);
                intrinsic = varTypeIsLong(simdBaseType) ? NI_AdvSimd_ShiftLogicalScalar : NI_AdvSimd_ShiftLogical;
            }
#elif defined(TARGET_WASM)
            // TODO-WASM-SIMD: Implement NI_Vector_ShiftLeft
            return nullptr;
#else
            unreached();
#endif

            retNode = gtNewSimdHWIntrinsicNode(retType, op1, op2, intrinsic, simdBaseType, simdSize);
            break;
        }

        case NI_Vector_Shuffle:
        case NI_Vector_ShuffleNative:
        case NI_Vector_ShuffleNativeFallback:
        {
            assert((sig->numArgs == 2) || (sig->numArgs == 3));

#if defined(TARGET_WASM)
            // TODO-WASM-SIMD: Implement NI_Vector_Shuffle
            return nullptr;
#endif

            bool isShuffleNative    = (intrinsic != NI_Vector_Shuffle);
            bool isNonDeterministic = isShuffleNative;

#if defined(TARGET_ARM64)
            if (isNonDeterministic)
            {
                isNonDeterministic = genTypeSize(simdBaseType) > 1;
            }
#endif

            if (isNonDeterministic && BlockNonDeterministicIntrinsics(mustExpand))
            {
                break;
            }

            GenTree* indices = impStackTop(0).val;

            // Check if the required intrinsics are available to emit now (validForShuffle). If we have variable
            // indices that might become possible to emit later (due to them becoming constant), this will be
            // indicated in canBecomeValidForShuffle; otherwise, it's just the same as validForShuffle.
            bool canBecomeValidForShuffle = false;
            bool validForShuffle =
                IsValidForShuffle(indices, simdSize, simdBaseType, &canBecomeValidForShuffle, isShuffleNative);

            // If it isn't valid for shuffle (and can't become valid later), then give up now.
            if (!canBecomeValidForShuffle)
            {
                return nullptr;
            }

            // If the indices might become constant later, then we don't emit for now, delay until later.
            if ((!validForShuffle) || (!indices->IsCnsVec()))
            {
                assert(sig->numArgs == 2);

                if (opts.OptimizationEnabled())
                {
                    // Only enable late stage rewriting if optimizations are enabled
                    // as we won't otherwise encounter a constant at the later point
                    op2 = impSIMDPopStack();
                    op1 = impSIMDPopStack();

                    retNode = gtNewSimdHWIntrinsicNode(retType, op1, op2, intrinsic, simdBaseType, simdSize);

                    retNode->AsHWIntrinsic()->SetMethodHandle(this, method R2RARG(*entryPoint));
                    break;
                }

                // If we're not doing late stage rewriting, just return null now as it won't become valid.
                if (!validForShuffle)
                {
                    return nullptr;
                }
            }

            if (sig->numArgs == 2)
            {
                op2     = impSIMDPopStack();
                op1     = impSIMDPopStack();
                retNode = gtNewSimdShuffleNode(retType, op1, op2, simdBaseType, simdSize, isShuffleNative);
            }
            break;
        }

        case NI_Vector_Sqrt:
        {
            assert(sig->numArgs == 1);

            if (varTypeIsFloating(simdBaseType))
            {
                op1     = impSIMDPopStack();
                retNode = gtNewSimdSqrtNode(retType, op1, simdBaseType, simdSize);
            }
            break;
        }

        case NI_Vector_StoreAligned:
        {
            assert(sig->numArgs == 2);
            assert(retType == TYP_VOID);

#if defined(TARGET_ARM64) || defined(TARGET_WASM)
            if (opts.OptimizationDisabled())
            {
                // ARM64/WASM doesn't have aligned stores, but aligned stores are only validated to be
                // aligned when optimizations are disable, so only skip the intrinsic handling
                // if optimizations are enabled
                break;
            }
#endif

            impSpillSideEffect(true, stackState.esStackDepth - 2 DEBUGARG("Spilling op1 side effects for HWIntrinsic"));

            op2 = impPopStack().val;

            if (op2->OperIs(GT_CAST) && op2->gtGetOp1()->TypeIs(TYP_BYREF))
            {
                // If what we have is a BYREF, that's what we really want, so throw away the cast.
                op2 = op2->gtGetOp1();
            }

            op1 = impSIMDPopStack();

            retNode = gtNewSimdStoreAlignedNode(op2, op1, simdBaseType, simdSize);
            break;
        }

        case NI_Vector_StoreAlignedNonTemporal:
        {
            assert(sig->numArgs == 2);
            assert(retType == TYP_VOID);

#if defined(TARGET_ARM64) || defined(TARGET_WASM)
            if (opts.OptimizationDisabled())
            {
                // ARM64/WASM doesn't have aligned stores, but aligned stores are only validated to be
                // aligned when optimizations are disable, so only skip the intrinsic handling
                // if optimizations are enabled
                break;
            }
#endif

            impSpillSideEffect(true, stackState.esStackDepth - 2 DEBUGARG("Spilling op1 side effects for HWIntrinsic"));

            op2 = impPopStack().val;

            if (op2->OperIs(GT_CAST) && op2->gtGetOp1()->TypeIs(TYP_BYREF))
            {
                // If what we have is a BYREF, that's what we really want, so throw away the cast.
                op2 = op2->gtGetOp1();
            }

            op1 = impSIMDPopStack();

            retNode = gtNewSimdStoreNonTemporalNode(op2, op1, simdBaseType, simdSize);
            break;
        }

        case NI_Vector_StoreUnsafe:
        {
            assert(retType == TYP_VOID);

            if (sig->numArgs == 3)
            {
                impSpillSideEffect(true,
                                   stackState.esStackDepth - 3 DEBUGARG("Spilling op1 side effects for HWIntrinsic"));

                op3 = impPopStack().val;
            }
            else
            {
                assert(sig->numArgs == 2);

                impSpillSideEffect(true,
                                   stackState.esStackDepth - 2 DEBUGARG("Spilling op1 side effects for HWIntrinsic"));
            }

            op2 = impPopStack().val;

            if (op2->OperIs(GT_CAST) && op2->gtGetOp1()->TypeIs(TYP_BYREF))
            {
                // If what we have is a BYREF, that's what we really want, so throw away the cast.
                op2 = op2->gtGetOp1();
            }

            if (sig->numArgs == 3)
            {
                op4 = gtNewIconNode(genTypeSize(simdBaseType), op3->TypeGet());
                op3 = gtNewOperNode(GT_MUL, op3->TypeGet(), op3, op4);
                op2 = gtNewOperNode(GT_ADD, op2->TypeGet(), op2, op3);
            }

            op1 = impSIMDPopStack();

            retNode = gtNewSimdStoreNode(op2, op1, simdBaseType, simdSize);
            break;
        }

        case NI_Vector_SubtractSaturate:
        {
            assert(sig->numArgs == 2);

            op2 = impSIMDPopStack();
            op1 = impSIMDPopStack();

            if (varTypeIsFloating(simdBaseType))
            {
                retNode = gtNewSimdBinOpNode(GT_SUB, retType, op1, op2, simdBaseType, simdSize);
            }
            else
            {
#if defined(TARGET_ARM64)
                intrinsic = NI_AdvSimd_SubtractSaturate;

                if ((simdSize == 8) && varTypeIsLong(simdBaseType))
                {
                    intrinsic = NI_AdvSimd_SubtractSaturateScalar;
                }

                retNode = gtNewSimdHWIntrinsicNode(retType, op1, op2, intrinsic, simdBaseType, simdSize);
#elif defined(TARGET_XARCH) || defined(TARGET_WASM)
                if (varTypeIsSmall(simdBaseType))
                {
#if defined(TARGET_XARCH)
                    if (simdSize == 64)
                    {
                        intrinsic = NI_AVX512_SubtractSaturate;
                    }
                    else if (simdSize == 32)
                    {
                        intrinsic = NI_AVX2_SubtractSaturate;
                    }
                    else
                    {
                        assert(simdSize == 16);
                        intrinsic = NI_X86Base_SubtractSaturate;
                    }
#elif defined(TARGET_WASM)
                    intrinsic = NI_PackedSimd_SubtractSaturate;
#else
#error Unsupported platform
#endif

                    retNode = gtNewSimdHWIntrinsicNode(retType, op1, op2, intrinsic, simdBaseType, simdSize);
                }
                else if (varTypeIsUnsigned(simdBaseType))
                {
                    // For unsigned we simply have to detect `(x - y) > x`
                    // and in that scenario return MinValue (Zero)

                    GenTree* cns     = gtNewZeroConNode(retType);
                    GenTree* op1Dup1 = fgMakeMultiUse(&op1);

                    GenTree* tmp     = gtNewSimdBinOpNode(GT_SUB, retType, op1, op2, simdBaseType, simdSize);
                    GenTree* tmpDup1 = fgMakeMultiUse(&tmp);
                    GenTree* msk     = gtNewSimdCmpOpNode(GT_GT, retType, tmp, op1Dup1, simdBaseType, simdSize);

                    retNode = gtNewSimdCndSelNode(retType, msk, cns, tmpDup1, simdBaseType, simdSize);
                }
                else
                {
                    // For signed the logic is a bit more complex, but is
                    // explained on the managed side as part of Scalar<T>.SubtractSaturate

                    GenTreeVecCon* minCns = gtNewVconNode(retType);
                    GenTreeVecCon* maxCns = gtNewVconNode(retType);

                    switch (simdBaseType)
                    {
                        case TYP_INT:
                        {
                            minCns->EvaluateBroadcastInPlace<int32_t>(INT32_MIN);
                            maxCns->EvaluateBroadcastInPlace<int32_t>(INT32_MAX);
                            break;
                        }

                        case TYP_LONG:
                        {
                            minCns->EvaluateBroadcastInPlace<int64_t>(INT64_MIN);
                            maxCns->EvaluateBroadcastInPlace<int64_t>(INT64_MAX);
                            break;
                        }

                        default:
                        {
                            unreached();
                        }
                    }

                    GenTree* op1Dup1 = fgMakeMultiUse(&op1);
                    GenTree* op2Dup1 = fgMakeMultiUse(&op2);

                    GenTree* tmp = gtNewSimdBinOpNode(GT_SUB, retType, op1, op2, simdBaseType, simdSize);

                    GenTree* tmpDup1 = fgMakeMultiUse(&tmp);
                    GenTree* tmpDup2 = gtCloneExpr(tmpDup1);

                    GenTree* msk = gtNewSimdIsNegativeNode(retType, tmpDup1, simdBaseType, simdSize);
                    GenTree* ovf = gtNewSimdCndSelNode(retType, msk, maxCns, minCns, simdBaseType, simdSize);

                    // The mask we need is ((a ^ b) & (b ^ c)) < 0

#if defined(TARGET_XARCH)
                    if (compOpportunisticallyDependsOn(InstructionSet_AVX512))
                    {
                        // tmpDup1 = a: 0xF0
                        // op1Dup1 = b: 0xCC
                        // op2Dup2 = c: 0xAA
                        //
                        // 0x18    = B ? norAC : andAC
                        //           b ? ~(a | c) : (a & c)
                        msk = gtNewSimdTernaryLogicNode(retType, tmp, op1Dup1, op2Dup1, gtNewIconNode(0x24),
                                                        simdBaseType, simdSize);
                    }
                    else
#endif
                    {
                        GenTree* op1Dup2 = gtCloneExpr(op1Dup1);

                        GenTree* msk2 = gtNewSimdBinOpNode(GT_XOR, retType, tmp, op1Dup1, simdBaseType, simdSize);
                        GenTree* msk3 = gtNewSimdBinOpNode(GT_XOR, retType, op1Dup2, op2Dup1, simdBaseType, simdSize);

                        msk = gtNewSimdBinOpNode(GT_AND, retType, msk2, msk3, simdBaseType, simdSize);
                    }

                    msk     = gtNewSimdIsNegativeNode(retType, msk, simdBaseType, simdSize);
                    retNode = gtNewSimdCndSelNode(retType, msk, ovf, tmpDup2, simdBaseType, simdSize);
                }
#endif
            }
            break;
        }

        case NI_Vector_Sum:
        {
            assert(sig->numArgs == 1);

#if defined(TARGET_WASM)
            // TODO-WASM-SIMD: Implement NI_Vector_Sum - Need Shuffle
            return nullptr;
#endif

            op1     = impSIMDPopStack();
            retNode = gtNewSimdSumNode(retType, op1, simdBaseType, simdSize);
            break;
        }

        case NI_Vector_ToScalar:
        {
            assert(sig->numArgs == 1);

#if defined(TARGET_WASM)
            // TODO-WASM-SIMD: Implement NI_Vector_ToScalar - Need GetElement
            return nullptr;
#endif

            op1     = impSIMDPopStack();
            retNode = gtNewSimdToScalarNode(retType, op1, simdBaseType, simdSize);
            break;
        }

        case NI_Vector_Truncate:
        {
            assert(sig->numArgs == 1);

            if (!varTypeIsFloating(simdBaseType))
            {
                retNode = impSIMDPopStack();
                break;
            }

            op1     = impSIMDPopStack();
            retNode = gtNewSimdTruncNode(retType, op1, simdBaseType, simdSize);
            break;
        }

        case NI_Vector_UnzipEven:
        case NI_Vector_UnzipOdd:
        {
            assert(sig->numArgs == 2);

#if defined(TARGET_WASM)
            // TODO-WASM-SIMD: Implement NI_Vector_Unzip - Need Shuffle
            return nullptr;
#endif

#if defined(TARGET_XARCH)
            if (simdSize == 16)
            {
                bool supportsX86BaseShuffle = (genTypeSize(simdBaseType) == 4);

                if (!supportsX86BaseShuffle && !compOpportunisticallyDependsOn(InstructionSet_AVX2))
                {
                    break;
                }
            }
#endif

            op2 = impSIMDPopStack();
            op1 = impSIMDPopStack();

            bool odd = (intrinsic == NI_Vector_UnzipOdd);
            retNode  = gtNewSimdUnzipNode(retType, op1, op2, simdBaseType, simdSize, odd);
            break;
        }

        case NI_Vector_WidenLower:
        {
            assert(sig->numArgs == 1);
            op1     = impSIMDPopStack();
            retNode = gtNewSimdWidenLowerNode(retType, op1, simdBaseType, simdSize);
            break;
        }

        case NI_Vector_WidenUpper:
        {
            assert(sig->numArgs == 1);

#if defined(TARGET_WASM)
            if (simdBaseType == TYP_FLOAT)
            {
                // TODO-WASM-SIMD - Implement NI_Vector_WidenUpper(float) - Need Shuffle
                return nullptr;
            }
#endif

            op1     = impSIMDPopStack();
            retNode = gtNewSimdWidenUpperNode(retType, op1, simdBaseType, simdSize);
            break;
        }

        case NI_Vector_WithElement:
        {
            assert(sig->numArgs == 3);

#if defined(TARGET_WASM)
            // TODO-WASM-SIMD: Implement NI_Vector_WithElement
            return nullptr;
#endif

#if defined(TARGET_X86)
            if (varTypeIsLong(simdBaseType))
            {
                return nullptr;
            }
#elif defined(TARGET_ARM64)
            bool     isIndexConst = true;
            GenTree* indexOp      = impStackTop(1).val;

            if (!indexOp->OperIsConst())
            {
                if (!opts.OptimizationEnabled())
                {
                    // Only enable late stage rewriting if optimizations are enabled
                    // as we won't otherwise encounter a constant at the later point
                    return nullptr;
                }
                isIndexConst = false;
            }
            else
            {
                ssize_t imm8  = indexOp->AsIntCon()->IconValue();
                ssize_t count = simdSize / genTypeSize(simdBaseType);

                if ((imm8 >= count) || (imm8 < 0))
                {
                    // Using software fallback if index is out of range (throw exception)
                    return nullptr;
                }
            }
#endif

            op3 = impPopStack().val;
            op2 = impPopStack().val;
            op1 = impSIMDPopStack();

#if defined(TARGET_ARM64)
            if (!isIndexConst)
            {
                retNode = gtNewSimdHWIntrinsicNode(retType, op1, op2, op3, intrinsic, simdBaseType, simdSize);
                retNode->AsHWIntrinsic()->SetMethodHandle(this, method R2RARG(*entryPoint));
                break;
            }
#endif

            retNode = gtNewSimdWithElementNode(retType, op1, op2, op3, simdBaseType, simdSize);
            break;
        }

#if defined(TARGET_XARCH) || defined(TARGET_ARM64)
        case NI_Vector_WithLower:
        {
            assert(sig->numArgs == 2);

#if defined(TARGET_XARCH)
            if (simdSize == 16)
            {
                break;
            }
#endif

            op2 = impSIMDPopStack();
            op1 = impSIMDPopStack();

            retNode = gtNewSimdWithLowerNode(retType, op1, op2, simdBaseType, simdSize);
            break;
        }

        case NI_Vector_WithUpper:
        {
            assert(sig->numArgs == 2);

#if defined(TARGET_XARCH)
            if (simdSize == 16)
            {
                break;
            }
#endif

            op2 = impSIMDPopStack();
            op1 = impSIMDPopStack();

            retNode = gtNewSimdWithUpperNode(retType, op1, op2, simdBaseType, simdSize);
            break;
        }
#endif // !TARGET_XARCH && !TARGET_ARM64

        case NI_Vector_ZipLower:
        case NI_Vector_ZipUpper:
        {
            assert(sig->numArgs == 2);

#if defined(TARGET_WASM)
            // TODO-WASM-SIMD: Implement NI_Vector_Zip - Need Shuffle
            return nullptr;
#endif

            op2 = impSIMDPopStack();
            op1 = impSIMDPopStack();

            bool upper = (intrinsic == NI_Vector_ZipUpper);
            retNode    = gtNewSimdZipNode(retType, op1, op2, simdBaseType, simdSize, upper);
            break;
        }

        case NI_Vector_get_AllBitsSet:
        {
            assert(sig->numArgs == 0);
            retNode = gtNewAllBitsSetConNode(retType);
            break;
        }

        case NI_Vector_get_E:
        {
            assert(sig->numArgs == 0);

            if (varTypeIsFloating(simdBaseType))
            {
                GenTreeVecCon* vecCns = gtNewVconNode(retType);
                vecCns->EvaluateBroadcastInPlace(simdBaseType, 2.718281828459045);
                retNode = vecCns;
            }
            break;
        }

        case NI_Vector_get_Epsilon:
        {
            assert(sig->numArgs == 0);

            if (simdBaseType == TYP_FLOAT)
            {
                GenTreeVecCon* vecCns = gtNewVconNode(retType);
                vecCns->EvaluateBroadcastInPlace(TYP_INT, static_cast<int64_t>(0x00000001));
                retNode = vecCns;
            }
            else if (simdBaseType == TYP_DOUBLE)
            {
                GenTreeVecCon* vecCns = gtNewVconNode(retType);
                vecCns->EvaluateBroadcastInPlace(TYP_LONG, static_cast<int64_t>(0x0000000000000001));
                retNode = vecCns;
            }
            break;
        }

        case NI_Vector_get_Indices:
        {
            assert(sig->numArgs == 0);
            retNode = gtNewSimdGetIndicesNode(retType, simdBaseType, simdSize);
            break;
        }

        case NI_Vector_get_NaN:
        {
            assert(sig->numArgs == 0);

            if (simdBaseType == TYP_FLOAT)
            {
                GenTreeVecCon* vecCns = gtNewVconNode(retType);
                vecCns->EvaluateBroadcastInPlace(TYP_INT, static_cast<int64_t>(0xFFC00000));
                retNode = vecCns;
            }
            else if (simdBaseType == TYP_DOUBLE)
            {
                GenTreeVecCon* vecCns = gtNewVconNode(retType);
                vecCns->EvaluateBroadcastInPlace(TYP_LONG, static_cast<int64_t>(0xFFF8000000000000));
                retNode = vecCns;
            }
            break;
        }

        case NI_Vector_get_NegativeInfinity:
        {
            assert(sig->numArgs == 0);

            if (simdBaseType == TYP_FLOAT)
            {
                GenTreeVecCon* vecCns = gtNewVconNode(retType);
                vecCns->EvaluateBroadcastInPlace(TYP_INT, static_cast<int64_t>(0xFF800000));
                retNode = vecCns;
            }
            else if (simdBaseType == TYP_DOUBLE)
            {
                GenTreeVecCon* vecCns = gtNewVconNode(retType);
                vecCns->EvaluateBroadcastInPlace(TYP_LONG, static_cast<int64_t>(0xFFF0000000000000));
                retNode = vecCns;
            }
            break;
        }

        case NI_Vector_get_NegativeOne:
        {
            assert(sig->numArgs == 0);

            if (varTypeIsFloating(simdBaseType))
            {
                GenTreeVecCon* vecCns = gtNewVconNode(retType);
                vecCns->EvaluateBroadcastInPlace(simdBaseType, -1.0);
                retNode = vecCns;
            }
            else if (varTypeIsSigned(simdBaseType))
            {
                GenTreeVecCon* vecCns = gtNewVconNode(retType);
                vecCns->EvaluateBroadcastInPlace(simdBaseType, static_cast<int64_t>(-1));
                retNode = vecCns;
            }
            break;
        }

        case NI_Vector_get_NegativeZero:
        {
            assert(sig->numArgs == 0);

            if (varTypeIsFloating(simdBaseType))
            {
                GenTreeVecCon* vecCns = gtNewVconNode(retType);
                vecCns->EvaluateBroadcastInPlace(simdBaseType, -0.0);
                retNode = vecCns;
            }
            break;
        }

        case NI_Vector_get_One:
        {
            assert(sig->numArgs == 0);
            retNode = gtNewOneConNode(retType, simdBaseType);
            break;
        }

        case NI_Vector_get_Pi:
        {
            assert(sig->numArgs == 0);

            if (varTypeIsFloating(simdBaseType))
            {
                GenTreeVecCon* vecCns = gtNewVconNode(retType);
                vecCns->EvaluateBroadcastInPlace(simdBaseType, 3.141592653589793);
                retNode = vecCns;
            }
            break;
        }

        case NI_Vector_get_PositiveInfinity:
        {
            assert(sig->numArgs == 0);

            if (simdBaseType == TYP_FLOAT)
            {
                GenTreeVecCon* vecCns = gtNewVconNode(retType);
                vecCns->EvaluateBroadcastInPlace(TYP_INT, static_cast<int64_t>(0x7F800000));
                retNode = vecCns;
            }
            else if (simdBaseType == TYP_DOUBLE)
            {
                GenTreeVecCon* vecCns = gtNewVconNode(retType);
                vecCns->EvaluateBroadcastInPlace(TYP_LONG, static_cast<int64_t>(0x7FF0000000000000));
                retNode = vecCns;
            }
            break;
        }

        case NI_Vector_get_SignSequence:
        {
            assert(sig->numArgs == 0);

            var_types scalarType  = genActualType(simdBaseType);
            GenTree*  one         = gtNewOneConNode(scalarType);
            GenTree*  negativeOne = varTypeIsFloating(simdBaseType) ? gtNewDconNode(-1.0, simdBaseType)
                                                                    : gtNewAllBitsSetConNode(scalarType);

            retNode = gtNewSimdCreateAlternatingSequenceNode(retType, one, negativeOne, simdBaseType, simdSize);
            break;
        }

        case NI_Vector_get_Tau:
        {
            assert(sig->numArgs == 0);

            if (varTypeIsFloating(simdBaseType))
            {
                GenTreeVecCon* vecCns = gtNewVconNode(retType);
                vecCns->EvaluateBroadcastInPlace(simdBaseType, 6.283185307179586);
                retNode = vecCns;
            }
            break;
        }

        case NI_Vector_get_Zero:
        {
            assert(sig->numArgs == 0);
            retNode = gtNewZeroConNode(retType);
            break;
        }

        case NI_Vector_op_Addition:
        {
            assert(sig->numArgs == 2);

            op2 = impSIMDPopStack();
            op1 = impSIMDPopStack();

            retNode = gtNewSimdBinOpNode(GT_ADD, retType, op1, op2, simdBaseType, simdSize);
            break;
        }

        case NI_Vector_op_BitwiseAnd:
        {
            assert(sig->numArgs == 2);

            op2 = impSIMDPopStack();
            op1 = impSIMDPopStack();

            retNode = gtNewSimdBinOpNode(GT_AND, retType, op1, op2, simdBaseType, simdSize);
            break;
        }

        case NI_Vector_op_BitwiseOr:
        {
            assert(sig->numArgs == 2);

            op2 = impSIMDPopStack();
            op1 = impSIMDPopStack();

            retNode = gtNewSimdBinOpNode(GT_OR, retType, op1, op2, simdBaseType, simdSize);
            break;
        }

        case NI_Vector_op_Division:
        {
            assert(sig->numArgs == 2);

            if (!varTypeIsFloating(simdBaseType))
            {
#if defined(TARGET_XARCH)
                // Check to see if it is possible to emulate the integer division
                if (varTypeIsLong(simdBaseType))
                {
                    break;
                }
                impSpillSideEffect(true, stackState.esStackDepth -
                                             2 DEBUGARG("Spilling op1 side effects for vector integer division"));
#else
                // We can't trivially handle division for integral types using SIMD
                break;
#endif
            }

            CORINFO_ARG_LIST_HANDLE arg1     = sig->args;
            CORINFO_ARG_LIST_HANDLE arg2     = info.compCompHnd->getArgNext(arg1);
            var_types               argType  = TYP_UNKNOWN;
            CORINFO_CLASS_HANDLE    argClass = NO_CLASS_HANDLE;

            argType = JITtype2varType(strip(info.compCompHnd->getArgType(sig, arg2, &argClass)));
            op2     = getArgForHWIntrinsic(argType, argClass);

            argType = JITtype2varType(strip(info.compCompHnd->getArgType(sig, arg1, &argClass)));
            op1     = getArgForHWIntrinsic(argType, argClass);

            retNode = gtNewSimdBinOpNode(GT_DIV, retType, op1, op2, simdBaseType, simdSize);
            break;
        }

        case NI_Vector_op_Equality:
        {
            assert(sig->numArgs == 2);

            op2 = impSIMDPopStack();
            op1 = impSIMDPopStack();

            retNode = gtNewSimdCmpOpAllNode(GT_EQ, retType, op1, op2, simdBaseType, simdSize);
            break;
        }

        case NI_Vector_op_ExclusiveOr:
        {
            assert(sig->numArgs == 2);

            op2 = impSIMDPopStack();
            op1 = impSIMDPopStack();

            retNode = gtNewSimdBinOpNode(GT_XOR, retType, op1, op2, simdBaseType, simdSize);
            break;
        }

        case NI_Vector_op_Inequality:
        {
            assert(sig->numArgs == 2);

            op2 = impSIMDPopStack();
            op1 = impSIMDPopStack();

            retNode = gtNewSimdCmpOpAnyNode(GT_NE, retType, op1, op2, simdBaseType, simdSize);
            break;
        }

        case NI_Vector_op_LeftShift:
        {
            assert(sig->numArgs == 2);

#if defined(TARGET_WASM)
            // TODO-WASM-SIMD: Implement NI_Vector_op_LeftShift
            return nullptr;
#endif

            op2 = impPopStack().val;
            op1 = impSIMDPopStack();

            retNode = gtNewSimdBinOpNode(GT_LSH, retType, op1, op2, simdBaseType, simdSize);
            break;
        }

        case NI_Vector_op_Multiply:
        {
            assert(sig->numArgs == 2);

            CORINFO_ARG_LIST_HANDLE arg1     = sig->args;
            CORINFO_ARG_LIST_HANDLE arg2     = info.compCompHnd->getArgNext(arg1);
            var_types               argType  = TYP_UNKNOWN;
            CORINFO_CLASS_HANDLE    argClass = NO_CLASS_HANDLE;

            argType = JITtype2varType(strip(info.compCompHnd->getArgType(sig, arg2, &argClass)));
            op2     = getArgForHWIntrinsic(argType, argClass);

            argType = JITtype2varType(strip(info.compCompHnd->getArgType(sig, arg1, &argClass)));
            op1     = getArgForHWIntrinsic(argType, argClass);

            retNode = gtNewSimdBinOpNode(GT_MUL, retType, op1, op2, simdBaseType, simdSize);
            break;
        }

        case NI_Vector_op_OnesComplement:
        {
            assert(sig->numArgs == 1);
            op1     = impSIMDPopStack();
            retNode = gtNewSimdUnOpNode(GT_NOT, retType, op1, simdBaseType, simdSize);
            break;
        }

        case NI_Vector_op_RightShift:
        {
            assert(sig->numArgs == 2);

#if defined(TARGET_WASM)
            // TODO-WASM-SIMD: Implement NI_Vector_op_RightShift
            return nullptr;
#endif

            genTreeOps op = varTypeIsUnsigned(simdBaseType) ? GT_RSZ : GT_RSH;

            op2 = impPopStack().val;
            op1 = impSIMDPopStack();

            retNode = gtNewSimdBinOpNode(op, retType, op1, op2, simdBaseType, simdSize);
            break;
        }

        case NI_Vector_op_Subtraction:
        {
            assert(sig->numArgs == 2);

            op2 = impSIMDPopStack();
            op1 = impSIMDPopStack();

            retNode = gtNewSimdBinOpNode(GT_SUB, retType, op1, op2, simdBaseType, simdSize);
            break;
        }

        case NI_Vector_op_UnaryNegation:
        {
            assert(sig->numArgs == 1);
            op1     = impSIMDPopStack();
            retNode = gtNewSimdUnOpNode(GT_NEG, retType, op1, simdBaseType, simdSize);
            break;
        }

        case NI_Vector_op_UnaryPlus:
        {
            assert(sig->numArgs == 1);
            retNode = impSIMDPopStack();
            break;
        }

        case NI_Vector_op_UnsignedRightShift:
        {
            assert(sig->numArgs == 2);

#if defined(TARGET_WASM)
            // TODO-WASM-SIMD: Implement NI_Vector_op_UnsignedRightShift
            return nullptr;
#endif

            op2 = impPopStack().val;
            op1 = impSIMDPopStack();

            retNode = gtNewSimdBinOpNode(GT_RSZ, retType, op1, op2, simdBaseType, simdSize);
            break;
        }

        default:
        {
            unreached();
            break;
        }
    }

    if (isMinMaxIntrinsic)
    {
        assert(sig->numArgs == 2);
        assert(retNode == nullptr);

        if (isNative && BlockNonDeterministicIntrinsics(mustExpand))
        {
            return nullptr;
        }

        op2 = impSIMDPopStack();
        op1 = impSIMDPopStack();

        if (isNative)
        {
            assert(!isMagnitude && !isNumber);
            retNode = gtNewSimdMinMaxNativeNode(retType, op1, op2, simdBaseType, simdSize, isMax);
        }
        else
        {
            retNode = gtNewSimdMinMaxNode(retType, op1, op2, simdBaseType, simdSize, isMax, isMagnitude, isNumber);
        }
    }
    else if (isConcatIntrinsic)
    {
        assert(sig->numArgs == 2);
        assert(retNode == nullptr);

#if defined(TARGET_WASM)
        // TODO-WASM-SIMD: Implement NI_Vector_Concat - Need Shuffle
        return nullptr;
#endif

        op2 = impSIMDPopStack();
        op1 = impSIMDPopStack();

        retNode = gtNewSimdConcatNode(retType, op1, op2, simdBaseType, simdSize, leftUpper, rightUpper);
    }
#if defined(TARGET_XARCH)
    else if (retType == TYP_MASK)
    {
        retType = getSIMDTypeForSize(simdSize);
        assert(retType == getSIMDTypeForSize(getSIMDTypeSizeInBytes(sig->retTypeSigClass)));
        retNode = gtNewSimdCvtMaskToVectorNode(retType, gtFoldExpr(retNode), simdBaseType, simdSize);
    }
#endif

    return retNode;
}
#endif // FEATURE_HW_INTRINSICS
