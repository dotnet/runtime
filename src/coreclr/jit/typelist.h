// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#define GCS EA_GCREF
#define BRS EA_BYREF
#define EPS EA_PTRSIZE
#define PS TARGET_POINTER_SIZE
#define PST (TARGET_POINTER_SIZE / sizeof(int))

#ifdef TARGET_64BIT
#define VTF_I32 0
#define VTF_I64 VTF_I
#else
#define VTF_I32 VTF_I
#define VTF_I64 0
#endif

/*  tn      - TYP_name
    nm      - name string
    jitType - The jit compresses types that are 'equivalent', this is the jit type genActualType()
    verType - Used for type checking
    sz      - size in bytes (genTypeSize(t))
    sze     - size in bytes for the emitter (GC types are encoded) (emitTypeSize(t))
    asze    - size in bytes for the emitter (GC types are encoded) (emitActualTypeSize(t))
    st      - stack slots (slots are sizeof(int) bytes) (genTypeStSzs())
    al      - alignment
    regTyp  - LSRA: type of register to use
    regFld  - LSRA: field to use to track available registers
    tf      - flags

DEF_TP(tn      ,nm        , jitType,     verType, sz,sze,asze, st,al, regTyp,    regFld,              tf     )
*/

// clang-format off
DEF_TP(UNDEF   ,"<UNDEF>" , TYP_UNDEF,   TI_ERROR, 0,  0,  0,   0, 0, VTR_INT,   availableIntRegs,    VTF_ANY)
DEF_TP(VOID    ,"void"    , TYP_VOID,    TI_ERROR, 0,  0,  0,   0, 0, VTR_INT,   availableIntRegs,    VTF_ANY)

DEF_TP(BOOL    ,"bool"    , TYP_INT,     TI_BYTE,  1,  1,  4,   1, 1, VTR_INT,   availableIntRegs,    VTF_INT|VTF_UNS)
DEF_TP(BYTE    ,"byte"    , TYP_INT,     TI_BYTE,  1,  1,  4,   1, 1, VTR_INT,   availableIntRegs,    VTF_INT)
DEF_TP(UBYTE   ,"ubyte"   , TYP_INT,     TI_BYTE,  1,  1,  4,   1, 1, VTR_INT,   availableIntRegs,    VTF_INT|VTF_UNS)

DEF_TP(SHORT   ,"short"   , TYP_INT,     TI_SHORT, 2,  2,  4,   1, 2, VTR_INT,   availableIntRegs,    VTF_INT)
DEF_TP(USHORT  ,"ushort"  , TYP_INT,     TI_SHORT, 2,  2,  4,   1, 2, VTR_INT,   availableIntRegs,    VTF_INT|VTF_UNS)

DEF_TP(INT     ,"int"     , TYP_INT,     TI_INT,   4,  4,  4,   1, 4, VTR_INT,   availableIntRegs,    VTF_INT|VTF_I32)
DEF_TP(UINT    ,"uint"    , TYP_INT,     TI_INT,   4,  4,  4,   1, 4, VTR_INT,   availableIntRegs,    VTF_INT|VTF_UNS|VTF_I32) // Only used in GT_CAST nodes

DEF_TP(LONG    ,"long"    , TYP_LONG,    TI_LONG,  8,EPS,EPS,   2, 8, VTR_INT,   availableIntRegs,    VTF_INT|VTF_I64)
DEF_TP(ULONG   ,"ulong"   , TYP_LONG,    TI_LONG,  8,EPS,EPS,   2, 8, VTR_INT,   availableIntRegs,    VTF_INT|VTF_UNS|VTF_I64) // Only used in GT_CAST nodes

DEF_TP(FLOAT   ,"float"   , TYP_FLOAT,   TI_FLOAT, 4,  4,  4,   1, 4, VTR_FLOAT, availableFloatRegs,  VTF_FLT)
DEF_TP(DOUBLE  ,"double"  , TYP_DOUBLE,  TI_DOUBLE,8,  8,  8,   2, 8, VTR_FLOAT, availableDoubleRegs, VTF_FLT)

DEF_TP(REF     ,"ref"     , TYP_REF,     TI_REF,  PS,GCS,GCS, PST,PS, VTR_INT,   availableIntRegs,    VTF_ANY|VTF_GCR|VTF_I)
DEF_TP(BYREF   ,"byref"   , TYP_BYREF,   TI_ERROR,PS,BRS,BRS, PST,PS, VTR_INT,   availableIntRegs,    VTF_ANY|VTF_BYR|VTF_I)
DEF_TP(STRUCT  ,"struct"  , TYP_STRUCT,  TI_STRUCT,0,  0,  0,   1, 4, VTR_INT,   availableIntRegs,    VTF_S)

#ifdef FEATURE_SIMD
DEF_TP(SIMD8    ,"simd8"  , TYP_SIMD8,   TI_STRUCT, 8, 8,  8,   2, 8, VTR_FLOAT, availableDoubleRegs, VTF_S|VTF_VEC)
DEF_TP(SIMD12   ,"simd12" , TYP_SIMD12,  TI_STRUCT,12,16, 16,   4,16, VTR_FLOAT, availableDoubleRegs, VTF_S|VTF_VEC)
DEF_TP(SIMD16   ,"simd16" , TYP_SIMD16,  TI_STRUCT,16,16, 16,   4,16, VTR_FLOAT, availableDoubleRegs, VTF_S|VTF_VEC)
#if defined(TARGET_XARCH)
DEF_TP(SIMD32   ,"simd32" , TYP_SIMD32,  TI_STRUCT,32,32, 32,   8,16, VTR_FLOAT, availableDoubleRegs, VTF_S|VTF_VEC)
DEF_TP(SIMD64   ,"simd64" , TYP_SIMD64,  TI_STRUCT,64,64, 64,  16,16, VTR_FLOAT, availableDoubleRegs, VTF_S|VTF_VEC)
DEF_TP(MASK     ,"mask"   , TYP_MASK,    TI_STRUCT, 8, 8,  8,   2, 8, VTR_MASK,  availableMaskRegs,   VTF_ANY)
#endif // TARGET_XARCH
#endif // FEATURE_SIMD

DEF_TP(UNKNOWN ,"unknown" ,TYP_UNKNOWN,  TI_ERROR, 0,  0,  0,   0, 0, VTR_INT,   availableIntRegs,    VTF_ANY)
// clang-format on

#undef GCS
#undef BRS
#undef EPS
#undef PS
#undef PST
#undef VTF_I32
#undef VTF_I64
