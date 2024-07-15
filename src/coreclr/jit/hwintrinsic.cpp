// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "jitpch.h"
#include "hwintrinsic.h"

#ifdef FEATURE_HW_INTRINSICS

static const HWIntrinsicInfo hwIntrinsicInfoArray[] = {
// clang-format off
#if defined(TARGET_XARCH)
#define HARDWARE_INTRINSIC(isa, name, size, numarg, extra, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, category, flag) \
    { \
            /* name */ #name, \
           /* flags */ static_cast<HWIntrinsicFlag>(flag), \
              /* id */ NI_##isa##_##name, \
             /* ins */ t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, \
             /* isa */ InstructionSet_##isa, \
        /* simdSize */ size, \
         /* numArgs */ numarg, \
        /* category */ category \
    },
#include "hwintrinsiclistxarch.h"
#elif defined (TARGET_ARM64)
#define HARDWARE_INTRINSIC(isa, name, size, numarg, extra, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, category, flag) \
    { \
            /* name */ #name, \
           /* flags */ static_cast<HWIntrinsicFlag>(flag), \
              /* id */ NI_##isa##_##name, \
             /* ins */ t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, \
             /* isa */ InstructionSet_##isa, \
        /* simdSize */ size, \
         /* numArgs */ numarg, \
        /* category */ category \
    },
#include "hwintrinsiclistarm64.h"
#else
#error Unsupported platform
#endif
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
#endif // TARGET_XARCH

//------------------------------------------------------------------------
// getBaseJitTypeFromArgIfNeeded: Get simdBaseJitType of intrinsic from 1st or 2nd argument depending on the flag
//
// Arguments:
//    intrinsic       -- id of the intrinsic function.
//    clsHnd          -- class handle containing the intrinsic function.
//    method          -- method handle of the intrinsic function.
//    sig             -- signature of the intrinsic call.
//    simdBaseJitType -- Predetermined simdBaseJitType, could be CORINFO_TYPE_UNDEF
//
// Return Value:
//    The basetype of intrinsic of it can be fetched from 1st or 2nd argument, else return baseType unmodified.
//
CorInfoType Compiler::getBaseJitTypeFromArgIfNeeded(NamedIntrinsic       intrinsic,
                                                    CORINFO_CLASS_HANDLE clsHnd,
                                                    CORINFO_SIG_INFO*    sig,
                                                    CorInfoType          simdBaseJitType)
{
    if (HWIntrinsicInfo::BaseTypeFromSecondArg(intrinsic) || HWIntrinsicInfo::BaseTypeFromFirstArg(intrinsic))
    {
        CORINFO_ARG_LIST_HANDLE arg = sig->args;

        if (HWIntrinsicInfo::BaseTypeFromSecondArg(intrinsic))
        {
            arg = info.compCompHnd->getArgNext(arg);
        }

        CORINFO_CLASS_HANDLE argClass = info.compCompHnd->getArgClass(sig, arg);
        simdBaseJitType               = getBaseJitTypeAndSizeOfSIMDType(argClass);

        if (simdBaseJitType == CORINFO_TYPE_UNDEF) // the argument is not a vector
        {
            CORINFO_CLASS_HANDLE tmpClass;
            simdBaseJitType = strip(info.compCompHnd->getArgType(sig, arg, &tmpClass));

            if (simdBaseJitType == CORINFO_TYPE_PTR)
            {
                simdBaseJitType = info.compCompHnd->getChildType(argClass, &tmpClass);
            }
        }
        assert(simdBaseJitType != CORINFO_TYPE_UNDEF);
    }

    return simdBaseJitType;
}

//------------------------------------------------------------------------
// vnEncodesResultTypeForHWIntrinsic(NamedIntrinsic hwIntrinsicID):
//
// Arguments:
//    hwIntrinsicID -- The id for the HW intrinsic
//
// Return Value:
//   Returns true if this intrinsic requires value numbering to add an
//   extra SimdType argument that encodes the resulting type.
//   If we don't do this overloaded versions can return the same VN
//   leading to incorrect CSE substitutions.
//
/* static */ bool Compiler::vnEncodesResultTypeForHWIntrinsic(NamedIntrinsic hwIntrinsicID)
{
    // No extra type information is needed for scalar/special HW Intrinsic.
    //
    unsigned simdSize = 0;
    if (HWIntrinsicInfo::tryLookupSimdSize(hwIntrinsicID, &simdSize) && (simdSize == 0))
    {
        return false;
    }

    int numArgs = HWIntrinsicInfo::lookupNumArgs(hwIntrinsicID);

    // HW Intrinsic's with -1 for numArgs have a varying number of args, so we currently
    // give them a unique value number, and don't add an extra argument.
    //
    if (numArgs == -1)
    {
        return false;
    }

    // We iterate over all of the different baseType's for this intrinsic in the HWIntrinsicInfo table
    // We set  diffInsCount to the number of instructions that can execute differently.
    //
    unsigned diffInsCount = 0;
#ifdef TARGET_XARCH
    instruction lastIns = INS_invalid;
#endif
    for (var_types baseType = TYP_BYTE; (baseType <= TYP_DOUBLE); baseType = (var_types)(baseType + 1))
    {
        instruction curIns = HWIntrinsicInfo::lookupIns(hwIntrinsicID, baseType);
        if (curIns != INS_invalid)
        {
#ifdef TARGET_XARCH
            if (curIns != lastIns)
            {
                diffInsCount++;
                // remember the last valid instruction that we saw
                lastIns = curIns;
            }
#elif defined(TARGET_ARM64)
            // On ARM64 we use the same instruction and specify an insOpt arrangement
            // so we always consider the instruction operation to be different
            //
            diffInsCount++;
#endif // TARGET
            if (diffInsCount >= 2)
            {
                // We can  early exit the loop now
                break;
            }
        }
    }

    // If we see two (or more) different instructions we need the extra VNF_SimdType arg
    return (diffInsCount >= 2);
}

struct HWIntrinsicIsaRange
{
    NamedIntrinsic FirstId;
    NamedIntrinsic LastId;
};

static const HWIntrinsicIsaRange hwintrinsicIsaRangeArray[] = {
// clang-format off
#if defined(TARGET_XARCH)
    { FIRST_NI_X86Base, LAST_NI_X86Base },
    { FIRST_NI_SSE, LAST_NI_SSE },
    { FIRST_NI_SSE2, LAST_NI_SSE2 },
    { FIRST_NI_SSE3, LAST_NI_SSE3 },
    { FIRST_NI_SSSE3, LAST_NI_SSSE3 },
    { FIRST_NI_SSE41, LAST_NI_SSE41 },
    { FIRST_NI_SSE42, LAST_NI_SSE42 },
    { FIRST_NI_AVX, LAST_NI_AVX },
    { FIRST_NI_AVX2, LAST_NI_AVX2 },
    { FIRST_NI_AES, LAST_NI_AES },
    { FIRST_NI_BMI1, LAST_NI_BMI1 },
    { FIRST_NI_BMI2, LAST_NI_BMI2 },
    { FIRST_NI_FMA, LAST_NI_FMA },
    { FIRST_NI_LZCNT, LAST_NI_LZCNT },
    { FIRST_NI_PCLMULQDQ, LAST_NI_PCLMULQDQ },
    { FIRST_NI_POPCNT, LAST_NI_POPCNT },
    { FIRST_NI_Vector128, LAST_NI_Vector128 },
    { FIRST_NI_Vector256, LAST_NI_Vector256 },
    { FIRST_NI_Vector512, LAST_NI_Vector512 },
    { FIRST_NI_AVXVNNI, LAST_NI_AVXVNNI },
    { NI_Illegal, NI_Illegal },                                 // MOVBE
    { FIRST_NI_X86Serialize, LAST_NI_X86Serialize },
    { NI_Illegal, NI_Illegal },                                 // EVEX
    { FIRST_NI_AVX512F, LAST_NI_AVX512F },
    { FIRST_NI_AVX512F_VL, LAST_NI_AVX512F_VL },
    { FIRST_NI_AVX512BW, LAST_NI_AVX512BW },
    { FIRST_NI_AVX512BW_VL, LAST_NI_AVX512BW_VL },
    { FIRST_NI_AVX512CD, LAST_NI_AVX512CD },
    { FIRST_NI_AVX512CD_VL, LAST_NI_AVX512CD_VL },
    { FIRST_NI_AVX512DQ, LAST_NI_AVX512DQ },
    { FIRST_NI_AVX512DQ_VL, LAST_NI_AVX512DQ_VL },
    { FIRST_NI_AVX512VBMI, LAST_NI_AVX512VBMI },
    { FIRST_NI_AVX512VBMI_VL, LAST_NI_AVX512VBMI_VL },
    { FIRST_NI_AVX10v1, LAST_NI_AVX10v1 },
    { FIRST_NI_AVX10v1_V512, LAST_NI_AVX10v1_V512 },
    { NI_Illegal, NI_Illegal },                                 // VectorT128
    { NI_Illegal, NI_Illegal },                                 // VectorT256
    { NI_Illegal, NI_Illegal },                                 // VectorT512
    { FIRST_NI_X86Base_X64, LAST_NI_X86Base_X64 },
    { FIRST_NI_SSE_X64, LAST_NI_SSE_X64 },
    { FIRST_NI_SSE2_X64, LAST_NI_SSE2_X64 },
    { NI_Illegal, NI_Illegal },                                 // SSE3_X64
    { NI_Illegal, NI_Illegal },                                 // SSSE3_X64
    { FIRST_NI_SSE41_X64, LAST_NI_SSE41_X64 },
    { FIRST_NI_SSE42_X64, LAST_NI_SSE42_X64 },
    { NI_Illegal, NI_Illegal },                                 // AVX_X64
    { NI_Illegal, NI_Illegal },                                 // AVX2_X64
    { NI_Illegal, NI_Illegal },                                 // AES_X64
    { FIRST_NI_BMI1_X64, LAST_NI_BMI1_X64 },
    { FIRST_NI_BMI2_X64, LAST_NI_BMI2_X64 },
    { NI_Illegal, NI_Illegal },                                 // FMA_X64
    { FIRST_NI_LZCNT_X64, LAST_NI_LZCNT_X64 },
    { NI_Illegal, NI_Illegal },                                 // PCLMULQDQ_X64
    { FIRST_NI_POPCNT_X64, LAST_NI_POPCNT_X64 },
    { NI_Illegal, NI_Illegal },                                 // AVXVNNI_X64
    { NI_Illegal, NI_Illegal },                                 // MOVBE_X64
    { NI_Illegal, NI_Illegal },                                 // X86Serialize_X64
    { NI_Illegal, NI_Illegal },                                 // EVEX_X64
    { FIRST_NI_AVX512F_X64, LAST_NI_AVX512F_X64 },
    { NI_Illegal, NI_Illegal },                                 // AVX512F_VL_X64
    { NI_Illegal, NI_Illegal },                                 // AVX512BW_X64
    { NI_Illegal, NI_Illegal },                                 // AVX512BW_VL_X64
    { NI_Illegal, NI_Illegal },                                 // AVX512CD_X64
    { NI_Illegal, NI_Illegal },                                 // AVX512CD_VL_X64
    { NI_Illegal, NI_Illegal },                                 // AVX512DQ_X64
    { NI_Illegal, NI_Illegal },                                 // AVX512DQ_VL_X64
    { NI_Illegal, NI_Illegal },                                 // AVX512VBMI_X64
    { NI_Illegal, NI_Illegal },                                 // AVX512VBMI_VL_X64
    { FIRST_NI_AVX10v1_X64, LAST_NI_AVX10v1_X64 },
    { NI_Illegal, NI_Illegal },                                 // AVX10v1_V512_X64
#elif defined (TARGET_ARM64)
    { FIRST_NI_ArmBase, LAST_NI_ArmBase },
    { FIRST_NI_AdvSimd, LAST_NI_AdvSimd },
    { FIRST_NI_Aes, LAST_NI_Aes },
    { FIRST_NI_Crc32, LAST_NI_Crc32 },
    { FIRST_NI_Dp, LAST_NI_Dp },
    { FIRST_NI_Rdm, LAST_NI_Rdm },
    { FIRST_NI_Sha1, LAST_NI_Sha1 },
    { FIRST_NI_Sha256, LAST_NI_Sha256 },
    { NI_Illegal, NI_Illegal },                         // Atomics
    { FIRST_NI_Vector64, LAST_NI_Vector64 },
    { FIRST_NI_Vector128, LAST_NI_Vector128 },
    { NI_Illegal, NI_Illegal },                         // Dczva
    { NI_Illegal, NI_Illegal },                         // Rcpc
    { NI_Illegal, NI_Illegal },                         // VectorT128
    { NI_Illegal, NI_Illegal },                         // Rcpc2
    { FIRST_NI_Sve, LAST_NI_Sve },
    { FIRST_NI_ArmBase_Arm64, LAST_NI_ArmBase_Arm64 },
    { FIRST_NI_AdvSimd_Arm64, LAST_NI_AdvSimd_Arm64 },
    { NI_Illegal, NI_Illegal },                         // Aes_Arm64
    { FIRST_NI_Crc32_Arm64, LAST_NI_Crc32_Arm64 },
    { NI_Illegal, NI_Illegal },                         // Dp_Arm64
    { FIRST_NI_Rdm_Arm64, LAST_NI_Rdm_Arm64 },
    { NI_Illegal, NI_Illegal },                         // Sha1_Arm64
    { NI_Illegal, NI_Illegal },                         // Sha256_Arm64
    { NI_Illegal, NI_Illegal },                         // Sve_Arm64
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
#else
        unreached();
#endif
    }

    if (info.numArgs != -1)
    {
        // We should only have an expected number of arguments
#if defined(TARGET_ARM64)
        assert((info.numArgs >= 0) && (info.numArgs <= 4));
#elif defined(TARGET_XARCH)
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
    // Both entries should be illegal if either is
    if (isaRange.FirstId == NI_Illegal)
    {
        assert(isaRange.LastId == NI_Illegal);
        return;
    }
    assert(isaRange.LastId != NI_Illegal);

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
// lookupId: Gets the NamedIntrinsic for a given method name and InstructionSet
//
// Arguments:
//    comp       -- The compiler
//    sig        -- The signature of the intrinsic
//    className  -- The name of the class associated with the HWIntrinsic to lookup
//    methodName -- The name of the method associated with the HWIntrinsic to lookup
//    enclosingClassName -- The name of the enclosing class of X64 classes
//
// Return Value:
//    The NamedIntrinsic associated with methodName and isa
NamedIntrinsic HWIntrinsicInfo::lookupId(Compiler*         comp,
                                         CORINFO_SIG_INFO* sig,
                                         const char*       className,
                                         const char*       methodName,
                                         const char*       enclosingClassName)
{
#if defined(DEBUG)
    static bool validationCompleted = false;

    if (!validationCompleted)
    {
        ValidateHWIntrinsicIsaRangeArray();
        validationCompleted = true;
    }
#endif // DEBUG

    CORINFO_InstructionSet isa = lookupIsa(className, enclosingClassName);

    if (isa == InstructionSet_ILLEGAL)
    {
        return NI_Illegal;
    }

    bool     isIsaSupported            = comp->compSupportsHWIntrinsic(isa);
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
            isa              = InstructionSet_SSE2;
            vectorByteLength = 16;
        }
        else if (isa == InstructionSet_Vector256)
        {
            isa              = InstructionSet_AVX2;
            vectorByteLength = 32;
        }
        else if (isa == InstructionSet_Vector512)
        {
            isa              = InstructionSet_AVX512F;
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
            else
            {
                assert(comp->IsTargetAbi(CORINFO_NATIVEAOT_ABI));
                return NI_IsSupported_Dynamic;
            }
        }

        return NI_IsSupported_False;
    }
    else if (!isIsaSupported)
    {
        return NI_Throw_PlatformNotSupportedException;
    }

    // Special case: For Vector64/128/256 we currently don't accelerate any of the methods when
    // IsHardwareAccelerated reports false. For Vector64 and Vector128 this is when the baseline
    // ISA is unsupported. For Vector256 this is when AVX2 is unsupported since integer types
    // can't get properly accelerated.

    // We support some Vector256 intrinsics on AVX-only CPUs
    bool isLimitedVector256Isa = false;

    if (isa == InstructionSet_Vector128)
    {
        if (!comp->IsBaselineSimdIsaSupported())
        {
            return NI_Illegal;
        }
    }
#if defined(TARGET_XARCH)
    else if (isa == InstructionSet_Vector256)
    {
        if (!comp->compOpportunisticallyDependsOn(InstructionSet_AVX2))
        {
            if (comp->compOpportunisticallyDependsOn(InstructionSet_AVX))
            {
                isLimitedVector256Isa = true;
            }
            else
            {
                return NI_Illegal;
            }
        }
    }
    else if (isa == InstructionSet_Vector512)
    {
        if (!comp->IsBaselineVector512IsaSupportedOpportunistically())
        {
            return NI_Illegal;
        }
    }
#elif defined(TARGET_ARM64)
    else if (isa == InstructionSet_Vector64)
    {
        if (!comp->IsBaselineSimdIsaSupported())
        {
            return NI_Illegal;
        }
    }
#endif

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

#if defined(TARGET_XARCH)
            // on AVX1-only CPUs we only support a subset of intrinsics in Vector256
            if (isLimitedVector256Isa && !AvxOnlyCompatible(ni))
            {
                return NI_Illegal;
            }
#endif // TARGET_XARCH

            return ni;
        }
    }

    // There are several helper intrinsics that are implemented in managed code
    // Those intrinsics will hit this code path and need to return NI_Illegal
    return NI_Illegal;
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

    CorInfoType simdBaseJitType = comp->getBaseJitTypeAndSizeOfSIMDType(typeHnd, &simdSize);
    assert((simdSize > 0) && (simdBaseJitType != CORINFO_TYPE_UNDEF));
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
            (void)getBaseJitTypeAndSizeOfSIMDType(argClass, &argSizeBytes);
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
// addRangeCheckIfNeeded: add a GT_BOUNDS_CHECK node for non-full-range imm-intrinsic
//
// Arguments:
//    intrinsic     -- intrinsic ID
//    immOp         -- the immediate operand of the intrinsic
//    mustExpand    -- true if the compiler is compiling the fallback(GT_CALL) of this intrinsics
//    immLowerBound -- lower incl. bound for a value of the immediate operand (for a non-full-range imm-intrinsic)
//    immUpperBound -- upper incl. bound for a value of the immediate operand (for a non-full-range imm-intrinsic)
//
// Return Value:
//     add a GT_BOUNDS_CHECK node for non-full-range imm-intrinsic, which would throw ArgumentOutOfRangeException
//     when the imm-argument is not in the valid range
//
GenTree* Compiler::addRangeCheckIfNeeded(
    NamedIntrinsic intrinsic, GenTree* immOp, bool mustExpand, int immLowerBound, int immUpperBound)
{
    assert(immOp != nullptr);
    // Full-range imm-intrinsics do not need the range-check
    // because the imm-parameter of the intrinsic method is a byte.
    // AVX2 Gather intrinsics no not need the range-check
    // because their imm-parameter have discrete valid values that are handle by managed code
    if (mustExpand && HWIntrinsicInfo::isImmOp(intrinsic, immOp)
#ifdef TARGET_XARCH
        && !HWIntrinsicInfo::isAVX2GatherIntrinsic(intrinsic) && !HWIntrinsicInfo::HasFullRangeImm(intrinsic)
#endif
    )
    {
        assert(!immOp->IsCnsIntOrI());
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
    return compHWIntrinsicDependsOn(isa) && (
#ifdef DEBUG
                                                JitConfig.EnableIncompleteISAClass() ||
#endif
                                                HWIntrinsicInfo::isFullyImplementedIsa(isa));
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

//------------------------------------------------------------------------
// isSupportedBaseType
//
// Arguments:
//    intrinsicId - HW intrinsic id
//    baseJitType - Base JIT type of the intrinsic.
//
// Return Value:
//    returns true if the baseType is supported for given intrinsic.
//
static bool isSupportedBaseType(NamedIntrinsic intrinsic, CorInfoType baseJitType)
{
    if (baseJitType == CORINFO_TYPE_UNDEF)
    {
        return false;
    }

    var_types baseType = JitType2PreciseVarType(baseJitType);

    // We don't actually check the intrinsic outside of the false case as we expect
    // the exposed managed signatures are either generic and support all types
    // or they are explicit and support the type indicated.

    if (varTypeIsArithmetic(baseType))
    {
        return true;
    }

#ifdef DEBUG
    CORINFO_InstructionSet isa = HWIntrinsicInfo::lookupIsa(intrinsic);
#ifdef TARGET_XARCH
    assert((isa == InstructionSet_Vector512) || (isa == InstructionSet_Vector256) || (isa == InstructionSet_Vector128));
#endif // TARGET_XARCH
#ifdef TARGET_ARM64
    assert((isa == InstructionSet_Vector64) || (isa == InstructionSet_Vector128));
#endif // TARGET_ARM64
#endif // DEBUG
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
                                        CorInfoType    simdBaseJitType,
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
            assert(!mustExpand);
            // The imm-HWintrinsics that do not accept all imm8 values may throw
            // ArgumentOutOfRangeException when the imm argument is not in the valid range
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
            var_types simdBaseType = JitType2PreciseVarType(simdBaseJitType);

            if (varTypeIsLong(simdBaseType))
            {
                if (!mustExpand)
                {
                    return false;
                }
            }
            else
#endif // TARGET_XARCH
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

    HWIntrinsicCategory    category        = HWIntrinsicInfo::lookupCategory(intrinsic);
    CORINFO_InstructionSet isa             = HWIntrinsicInfo::lookupIsa(intrinsic);
    int                    numArgs         = sig->numArgs;
    var_types              retType         = genActualType(JITtype2varType(sig->retType));
    CorInfoType            simdBaseJitType = CORINFO_TYPE_UNDEF;
    GenTree*               retNode         = nullptr;

    if (retType == TYP_STRUCT)
    {
        unsigned int sizeBytes;
        simdBaseJitType = getBaseJitTypeAndSizeOfSIMDType(sig->retTypeSigClass, &sizeBytes);

        if (HWIntrinsicInfo::IsMultiReg(intrinsic))
        {
            assert(sizeBytes == 0);
        }

#ifdef TARGET_ARM64
        else if ((intrinsic == NI_AdvSimd_LoadAndInsertScalar) || (intrinsic == NI_AdvSimd_Arm64_LoadAndInsertScalar))
        {
            CorInfoType pSimdBaseJitType = CORINFO_TYPE_UNDEF;
            var_types   retFieldType     = impNormStructType(sig->retTypeSigClass, &pSimdBaseJitType);

            if (retFieldType == TYP_STRUCT)
            {
                CORINFO_CLASS_HANDLE structType;
                unsigned int         sizeBytes = 0;

                // LoadAndInsertScalar that returns 2,3 or 4 vectors
                assert(pSimdBaseJitType == CORINFO_TYPE_UNDEF);
                unsigned fieldCount = info.compCompHnd->getClassNumInstanceFields(sig->retTypeSigClass);
                assert(fieldCount > 1);
                CORINFO_FIELD_HANDLE fieldHandle = info.compCompHnd->getFieldInClass(sig->retTypeClass, 0);
                CorInfoType          fieldType   = info.compCompHnd->getFieldType(fieldHandle, &structType);
                simdBaseJitType                  = getBaseJitTypeAndSizeOfSIMDType(structType, &sizeBytes);
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
                        assert("unsupported");
                }
            }
            else
            {
                assert((retFieldType == TYP_SIMD8) || (retFieldType == TYP_SIMD16));
                assert(isSupportedBaseType(intrinsic, simdBaseJitType));
                retType = getSIMDTypeForSize(sizeBytes);
            }
        }
#endif
        else
        {
            // We want to return early here for cases where retType was TYP_STRUCT as per method signature and
            // rather than deferring the decision after getting the simdBaseJitType of arg.
            if (!isSupportedBaseType(intrinsic, simdBaseJitType))
            {
                return nullptr;
            }

            assert(sizeBytes != 0);
            retType = getSIMDTypeForSize(sizeBytes);
        }
    }

    simdBaseJitType = getBaseJitTypeFromArgIfNeeded(intrinsic, clsHnd, sig, simdBaseJitType);

    if (simdBaseJitType == CORINFO_TYPE_UNDEF)
    {
        if ((category == HW_Category_Scalar) || HWIntrinsicInfo::isScalarIsa(isa))
        {
            simdBaseJitType = sig->retType;

            if (simdBaseJitType == CORINFO_TYPE_VOID)
            {
                simdBaseJitType = CORINFO_TYPE_UNDEF;
            }
        }
        else
        {
            unsigned int sizeBytes;

            simdBaseJitType = getBaseJitTypeAndSizeOfSIMDType(clsHnd, &sizeBytes);

#if defined(TARGET_ARM64)
            if (simdBaseJitType == CORINFO_TYPE_UNDEF && HWIntrinsicInfo::HasScalarInputVariant(intrinsic))
            {
                // Did not find a valid vector type. The intrinsic has alternate scalar version. Switch to that.

                assert(sizeBytes == 0);
                intrinsic = HWIntrinsicInfo::GetScalarInputVariant(intrinsic);
                category  = HWIntrinsicInfo::lookupCategory(intrinsic);
                isa       = HWIntrinsicInfo::lookupIsa(intrinsic);

                simdBaseJitType = sig->retType;
                assert(simdBaseJitType != CORINFO_TYPE_VOID);
                assert(simdBaseJitType != CORINFO_TYPE_UNDEF);
                assert(simdBaseJitType != CORINFO_TYPE_VALUECLASS);
            }
            else
#endif
            {
                assert((category == HW_Category_Special) || (category == HW_Category_Helper) || (sizeBytes != 0));
            }
        }
    }

    // Immediately return if the category is other than scalar/special and this is not a supported base type.
    if ((category != HW_Category_Special) && (category != HW_Category_Scalar) && !HWIntrinsicInfo::isScalarIsa(isa) &&
        !isSupportedBaseType(intrinsic, simdBaseJitType))
    {
        return nullptr;
    }

    var_types simdBaseType = TYP_UNKNOWN;

    if (simdBaseJitType != CORINFO_TYPE_UNDEF)
    {
        simdBaseType = JitType2PreciseVarType(simdBaseJitType);
    }

    const unsigned simdSize = HWIntrinsicInfo::lookupSimdSize(this, intrinsic, sig);

    HWIntrinsicSignatureReader sigReader;
    sigReader.Read(info.compCompHnd, sig);

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
        getHWIntrinsicImmTypes(intrinsic, sig, 2, simdBaseType, simdBaseJitType, sigReader.op1ClsHnd,
                               sigReader.op2ClsHnd, sigReader.op3ClsHnd, &immSimdSize, &immSimdBaseType);
        HWIntrinsicInfo::lookupImmBounds(intrinsic, immSimdSize, immSimdBaseType, 2, &immLowerBound, &immUpperBound);

        if (!CheckHWIntrinsicImmRange(intrinsic, simdBaseJitType, immOp2, mustExpand, immLowerBound, immUpperBound,
                                      false, &useFallback))
        {
            if (useFallback)
            {
                return impNonConstFallback(intrinsic, retType, simdBaseJitType);
            }
            else if (!opts.OptimizationEnabled())
            {
                // Only enable late stage rewriting if optimizations are enabled
                // as we won't otherwise encounter a constant at the later point
                return nullptr;
            }
            else
            {
                setMethodHandle = true;
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
        getHWIntrinsicImmTypes(intrinsic, sig, 1, simdBaseType, simdBaseJitType, sigReader.op1ClsHnd,
                               sigReader.op2ClsHnd, sigReader.op3ClsHnd, &immSimdSize, &immSimdBaseType);
        HWIntrinsicInfo::lookupImmBounds(intrinsic, immSimdSize, immSimdBaseType, 1, &immLowerBound, &immUpperBound);
#else
        immUpperBound   = HWIntrinsicInfo::lookupImmUpperBound(intrinsic);
        hasFullRangeImm = HWIntrinsicInfo::HasFullRangeImm(intrinsic);
#endif

        if (!CheckHWIntrinsicImmRange(intrinsic, simdBaseJitType, immOp1, mustExpand, immLowerBound, immUpperBound,
                                      hasFullRangeImm, &useFallback))
        {
            if (useFallback)
            {
                return impNonConstFallback(intrinsic, retType, simdBaseJitType);
            }
            else if (!opts.OptimizationEnabled())
            {
                // Only enable late stage rewriting if optimizations are enabled
                // as we won't otherwise encounter a constant at the later point
                return nullptr;
            }
            else
            {
                setMethodHandle = true;
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
#if defined(TARGET_ARM64)
    if (HWIntrinsicInfo::ReturnsPerElementMask(intrinsic))
    {
        // Ensure the result is generated to a mask.
        nodeRetType = TYP_MASK;
    }
#endif // defined(TARGET_ARM64)

    // table-driven importer of simple intrinsics
    if (impIsTableDrivenHWIntrinsic(intrinsic, category))
    {
        const bool isScalar = (category == HW_Category_Scalar);

        assert(numArgs >= 0);

        if (!isScalar)
        {
            if (HWIntrinsicInfo::lookupIns(intrinsic, simdBaseType) == INS_invalid)
            {
                assert(!"Unexpected HW intrinsic");
                return nullptr;
            }

#if defined(TARGET_ARM64)
            if ((simdSize != 8) && (simdSize != 16))
#elif defined(TARGET_XARCH)
            if ((simdSize != 16) && (simdSize != 32) && (simdSize != 64))
#endif // TARGET_*
            {
                assert(!"Unexpected SIMD size");
                return nullptr;
            }
        }

        GenTree* op1 = nullptr;
        GenTree* op2 = nullptr;
        GenTree* op3 = nullptr;
        GenTree* op4 = nullptr;

        switch (numArgs)
        {
            case 4:
                op4 = getArgForHWIntrinsic(sigReader.GetOp4Type(), sigReader.op4ClsHnd);
                op4 = addRangeCheckIfNeeded(intrinsic, op4, mustExpand, immLowerBound, immUpperBound);
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
                op2 = addRangeCheckIfNeeded(intrinsic, op2, mustExpand, immLowerBound, immUpperBound);
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
                retNode = gtNewSimdHWIntrinsicNode(nodeRetType, intrinsic, simdBaseJitType, simdSize);
                break;
            }

            case 1:
            {
                if ((category == HW_Category_MemoryLoad) && op1->OperIs(GT_CAST))
                {
                    // Although the API specifies a pointer, if what we have is a BYREF, that's what
                    // we really want, so throw away the cast.
                    if (op1->gtGetOp1()->TypeGet() == TYP_BYREF)
                    {
                        op1 = op1->gtGetOp1();
                    }
                }

                retNode = isScalar ? gtNewScalarHWIntrinsicNode(nodeRetType, op1, intrinsic)
                                   : gtNewSimdHWIntrinsicNode(nodeRetType, op1, intrinsic, simdBaseJitType, simdSize);

#if defined(TARGET_XARCH)
                switch (intrinsic)
                {
                    case NI_SSE41_ConvertToVector128Int16:
                    case NI_SSE41_ConvertToVector128Int32:
                    case NI_SSE41_ConvertToVector128Int64:
                    case NI_AVX2_BroadcastScalarToVector128:
                    case NI_AVX2_BroadcastScalarToVector256:
                    case NI_AVX512F_BroadcastScalarToVector512:
                    case NI_AVX512BW_BroadcastScalarToVector512:
                    case NI_AVX2_ConvertToVector256Int16:
                    case NI_AVX2_ConvertToVector256Int32:
                    case NI_AVX2_ConvertToVector256Int64:
                    case NI_AVX2_BroadcastVector128ToVector256:
                    case NI_AVX512F_BroadcastVector128ToVector512:
                    case NI_AVX512F_BroadcastVector256ToVector512:
                    {
                        // These intrinsics have both pointer and vector overloads
                        // We want to be able to differentiate between them so lets
                        // just track the aux type as a ptr or undefined, depending

                        CorInfoType auxiliaryType = CORINFO_TYPE_UNDEF;

                        if (!varTypeIsSIMD(op1))
                        {
                            auxiliaryType = CORINFO_TYPE_PTR;
                            retNode->gtFlags |= (GTF_EXCEPT | GTF_GLOB_REF);
                        }

                        retNode->AsHWIntrinsic()->SetAuxiliaryJitType(auxiliaryType);
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
                    case NI_Sve_ConvertToInt32:
                    case NI_Sve_ConvertToUInt32:
                    case NI_Sve_ConvertToInt64:
                    case NI_Sve_ConvertToUInt64:
                        // Save the base type of return SIMD. It is used to contain this intrinsic inside
                        // ConditionalSelect.
                        retNode->AsHWIntrinsic()->SetAuxiliaryJitType(getBaseJitTypeOfSIMDType(sig->retTypeSigClass));
                        break;
                    default:
                        break;
                }
#endif // TARGET_XARCH

                break;
            }

            case 2:
            {
                retNode = isScalar
                              ? gtNewScalarHWIntrinsicNode(nodeRetType, op1, op2, intrinsic)
                              : gtNewSimdHWIntrinsicNode(nodeRetType, op1, op2, intrinsic, simdBaseJitType, simdSize);

#ifdef TARGET_XARCH
                if ((intrinsic == NI_SSE42_Crc32) || (intrinsic == NI_SSE42_X64_Crc32))
                {
                    // TODO-XArch-Cleanup: currently we use the simdBaseJitType to bring the type of the second argument
                    // to the code generator. May encode the overload info in other way.
                    retNode->AsHWIntrinsic()->SetSimdBaseJitType(sigReader.op2JitType);
                }
#elif defined(TARGET_ARM64)
                switch (intrinsic)
                {
                    case NI_Crc32_ComputeCrc32:
                    case NI_Crc32_ComputeCrc32C:
                    case NI_Crc32_Arm64_ComputeCrc32:
                    case NI_Crc32_Arm64_ComputeCrc32C:
                        retNode->AsHWIntrinsic()->SetSimdBaseJitType(sigReader.op2JitType);
                        break;

                    case NI_AdvSimd_AddWideningUpper:
                    case NI_AdvSimd_SubtractWideningUpper:
                        assert(varTypeIsSIMD(op1->TypeGet()));
                        retNode->AsHWIntrinsic()->SetAuxiliaryJitType(getBaseJitTypeOfSIMDType(sigReader.op1ClsHnd));
                        break;

                    case NI_AdvSimd_Arm64_AddSaturateScalar:
                        assert(varTypeIsSIMD(op2->TypeGet()));
                        retNode->AsHWIntrinsic()->SetAuxiliaryJitType(getBaseJitTypeOfSIMDType(sigReader.op2ClsHnd));
                        break;

                    case NI_ArmBase_Arm64_MultiplyHigh:
                        if (sig->retType == CORINFO_TYPE_ULONG)
                        {
                            retNode->AsHWIntrinsic()->SetSimdBaseJitType(CORINFO_TYPE_ULONG);
                        }
                        else
                        {
                            assert(sig->retType == CORINFO_TYPE_LONG);
                            retNode->AsHWIntrinsic()->SetSimdBaseJitType(CORINFO_TYPE_LONG);
                        }
                        break;

                    case NI_Sve_CreateWhileLessThanMask8Bit:
                    case NI_Sve_CreateWhileLessThanOrEqualMask8Bit:
                    case NI_Sve_CreateWhileLessThanMask16Bit:
                    case NI_Sve_CreateWhileLessThanOrEqualMask16Bit:
                    case NI_Sve_CreateWhileLessThanMask32Bit:
                    case NI_Sve_CreateWhileLessThanOrEqualMask32Bit:
                    case NI_Sve_CreateWhileLessThanMask64Bit:
                    case NI_Sve_CreateWhileLessThanOrEqualMask64Bit:
                        retNode->AsHWIntrinsic()->SetAuxiliaryJitType(sigReader.op1JitType);
                        break;

                    case NI_Sve_ShiftLeftLogical:
                    case NI_Sve_ShiftRightArithmetic:
                    case NI_Sve_ShiftRightLogical:
                        retNode->AsHWIntrinsic()->SetAuxiliaryJitType(getBaseJitTypeOfSIMDType(sigReader.op2ClsHnd));
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
                    op2 = addRangeCheckIfNeeded(intrinsic, op2, mustExpand, immLowerBound, immUpperBound);

                    if (op1->OperIs(GT_CAST))
                    {
                        // Although the API specifies a pointer, if what we have is a BYREF, that's what
                        // we really want, so throw away the cast.
                        if (op1->gtGetOp1()->TypeGet() == TYP_BYREF)
                        {
                            op1 = op1->gtGetOp1();
                        }
                    }
                }
                else if ((intrinsic == NI_AdvSimd_Insert) || (intrinsic == NI_AdvSimd_InsertScalar))
                {
                    op2 = addRangeCheckIfNeeded(intrinsic, op2, mustExpand, immLowerBound, immUpperBound);
                }
                else
#endif
                {
                    op3 = addRangeCheckIfNeeded(intrinsic, op3, mustExpand, immLowerBound, immUpperBound);
                }

                retNode = isScalar ? gtNewScalarHWIntrinsicNode(nodeRetType, op1, op2, op3, intrinsic)
                                   : gtNewSimdHWIntrinsicNode(nodeRetType, op1, op2, op3, intrinsic, simdBaseJitType,
                                                              simdSize);

                switch (intrinsic)
                {
#if defined(TARGET_XARCH)
                    case NI_AVX2_GatherVector128:
                    case NI_AVX2_GatherVector256:
                        assert(varTypeIsSIMD(op2->TypeGet()));
                        retNode->AsHWIntrinsic()->SetAuxiliaryJitType(getBaseJitTypeOfSIMDType(sigReader.op2ClsHnd));
                        break;

#elif defined(TARGET_ARM64)
                    case NI_Sve_GatherVector:
                    case NI_Sve_GatherVectorByteZeroExtend:
                    case NI_Sve_GatherVectorFirstFaulting:
                    case NI_Sve_GatherVectorInt16SignExtend:
                    case NI_Sve_GatherVectorInt16WithByteOffsetsSignExtend:
                    case NI_Sve_GatherVectorInt32SignExtend:
                    case NI_Sve_GatherVectorInt32WithByteOffsetsSignExtend:
                    case NI_Sve_GatherVectorSByteSignExtend:
                    case NI_Sve_GatherVectorUInt16WithByteOffsetsZeroExtend:
                    case NI_Sve_GatherVectorUInt16WithByteOffsetsZeroExtendFirstFaulting:
                    case NI_Sve_GatherVectorUInt16ZeroExtend:
                    case NI_Sve_GatherVectorUInt16ZeroExtendFirstFaulting:
                    case NI_Sve_GatherVectorUInt32WithByteOffsetsZeroExtend:
                    case NI_Sve_GatherVectorUInt32WithByteOffsetsZeroExtendFirstFaulting:
                    case NI_Sve_GatherVectorUInt32ZeroExtend:
                    case NI_Sve_GatherVectorWithByteOffsets:
                    case NI_Sve_GatherVectorUInt32ZeroExtendFirstFaulting:
                        assert(varTypeIsSIMD(op3->TypeGet()));
                        if (numArgs == 3)
                        {
                            retNode->AsHWIntrinsic()->SetAuxiliaryJitType(
                                getBaseJitTypeOfSIMDType(sigReader.op3ClsHnd));
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
                retNode =
                    gtNewSimdHWIntrinsicNode(nodeRetType, op1, op2, op3, op4, intrinsic, simdBaseJitType, simdSize);

                switch (intrinsic)
                {
#if defined(TARGET_ARM64)
                    case NI_Sve_Scatter:
                        assert(varTypeIsSIMD(op3->TypeGet()));
                        if (numArgs == 4)
                        {
                            retNode->AsHWIntrinsic()->SetAuxiliaryJitType(
                                getBaseJitTypeOfSIMDType(sigReader.op3ClsHnd));
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
        retNode = impSpecialIntrinsic(intrinsic, clsHnd, method, sig R2RARG(entryPoint), simdBaseJitType, nodeRetType,
                                      simdSize, mustExpand);
    }

    if (setMethodHandle && (retNode != nullptr))
    {
        retNode->AsHWIntrinsic()->SetMethodHandle(this, method R2RARG(*entryPoint));
    }

#if defined(TARGET_ARM64)
    if (HWIntrinsicInfo::IsExplicitMaskedOperation(intrinsic))
    {
        assert(numArgs > 0);
        GenTree* op1 = retNode->AsHWIntrinsic()->Op(1);

        switch (intrinsic)
        {
            case NI_Sve_CreateBreakAfterMask:
            case NI_Sve_CreateBreakAfterPropagateMask:
            case NI_Sve_CreateBreakBeforeMask:
            case NI_Sve_CreateBreakBeforePropagateMask:
            case NI_Sve_CreateMaskForFirstActiveElement:
            case NI_Sve_CreateMaskForNextActiveElement:
            case NI_Sve_GetActiveElementCount:
            case NI_Sve_TestAnyTrue:
            case NI_Sve_TestFirstTrue:
            case NI_Sve_TestLastTrue:
            {
                GenTree* op2 = retNode->AsHWIntrinsic()->Op(2);

                // HWInstrinsic requires a mask for op2
                if (!varTypeIsMask(op2))
                {
                    retNode->AsHWIntrinsic()->Op(2) =
                        gtNewSimdCvtVectorToMaskNode(TYP_MASK, op2, simdBaseJitType, simdSize);
                }
                break;
            }

            default:
                break;
        }

        switch (intrinsic)
        {
            case NI_Sve_CreateBreakAfterPropagateMask:
            case NI_Sve_CreateBreakBeforePropagateMask:
            {
                GenTree* op3 = retNode->AsHWIntrinsic()->Op(3);

                // HWInstrinsic requires a mask for op3
                if (!varTypeIsMask(op3))
                {
                    retNode->AsHWIntrinsic()->Op(3) =
                        gtNewSimdCvtVectorToMaskNode(TYP_MASK, op3, simdBaseJitType, simdSize);
                }
                break;
            }

            default:
                break;
        }

        if (!varTypeIsMask(op1))
        {
            // Op1 input is a vector. HWInstrinsic requires a mask.
            retNode->AsHWIntrinsic()->Op(1) = gtNewSimdCvtVectorToMaskNode(TYP_MASK, op1, simdBaseJitType, simdSize);
        }

        if (HWIntrinsicInfo::IsMultiReg(intrinsic))
        {
            assert(HWIntrinsicInfo::IsExplicitMaskedOperation(retNode->AsHWIntrinsic()->GetHWIntrinsicId()));
            assert(HWIntrinsicInfo::IsMultiReg(retNode->AsHWIntrinsic()->GetHWIntrinsicId()));
            retNode =
                impStoreMultiRegValueToVar(retNode, sig->retTypeSigClass DEBUGARG(CorInfoCallConvExtension::Managed));
        }
    }

    if (retType != nodeRetType)
    {
        // HWInstrinsic returns a mask, but all returns must be vectors, so convert mask to vector.
        assert(HWIntrinsicInfo::ReturnsPerElementMask(intrinsic));
        assert(nodeRetType == TYP_MASK);

        GenTreeHWIntrinsic* op = retNode->AsHWIntrinsic();

        CorInfoType simdBaseJitType = op->GetSimdBaseJitType();
        unsigned    simdSize        = op->GetSimdSize();

        retNode = gtNewSimdCvtMaskToVectorNode(retType, op, simdBaseJitType, simdSize);
    }
#endif // defined(TARGET_ARM64)

    if ((retNode != nullptr) && retNode->OperIs(GT_HWINTRINSIC))
    {
        assert(!retNode->OperMayThrow(this) || ((retNode->gtFlags & GTF_EXCEPT) != 0));
        assert(!retNode->OperRequiresAsgFlag() || ((retNode->gtFlags & GTF_ASG) != 0));
        assert(!retNode->OperIsImplicitIndir() || ((retNode->gtFlags & GTF_GLOB_REF) != 0));
    }

    return retNode;
}

#endif // FEATURE_HW_INTRINSICS
