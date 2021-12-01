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

/*  tn  - TYP_name
    nm  - name string
    jitType - The jit compresses types that are 'equivalent', this is the jit type genActualType()
    verType - Used for type checking
    sz  - size in bytes (genTypeSize(t))
    sze - size in bytes for the emitter (GC types are encoded) (emitTypeSize(t))
    asze- size in bytes for the emitter (GC types are encoded) (emitActualTypeSize(t))
    st  - stack slots (slots are sizeof(int) bytes) (genTypeStSzs())
    al  - alignment
    tf  - flags
    howUsed - If a variable is used (referenced) as the type

DEF_TP(tn      ,nm        , jitType,     verType, sz,sze,asze, st,al, tf,            howUsed     )
*/

// clang-format off
DEF_TP(UNDEF   ,"<UNDEF>" , TYP_UNDEF,   TI_ERROR, 0,  0,  0,   0, 0, VTF_ANY,        0           )
DEF_TP(VOID    ,"void"    , TYP_VOID,    TI_ERROR, 0,  0,  0,   0, 0, VTF_ANY,        0           )

DEF_TP(BOOL    ,"bool"    , TYP_INT,     TI_BYTE,  1,  1,  4,   1, 1, VTF_INT|VTF_UNS,TYPE_REF_INT)
DEF_TP(BYTE    ,"byte"    , TYP_INT,     TI_BYTE,  1,  1,  4,   1, 1, VTF_INT,        TYPE_REF_INT)
DEF_TP(UBYTE   ,"ubyte"   , TYP_INT,     TI_BYTE,  1,  1,  4,   1, 1, VTF_INT|VTF_UNS,TYPE_REF_INT)

DEF_TP(SHORT   ,"short"   , TYP_INT,     TI_SHORT, 2,  2,  4,   1, 2, VTF_INT,        TYPE_REF_INT)
DEF_TP(USHORT  ,"ushort"  , TYP_INT,     TI_SHORT, 2,  2,  4,   1, 2, VTF_INT|VTF_UNS,TYPE_REF_INT)

DEF_TP(INT     ,"int"     , TYP_INT,     TI_INT,   4,  4,  4,   1, 4, VTF_INT|VTF_I32,        TYPE_REF_INT)
DEF_TP(UINT    ,"uint"    , TYP_INT,     TI_INT,   4,  4,  4,   1, 4, VTF_INT|VTF_UNS|VTF_I32,TYPE_REF_INT) // Only used in GT_CAST nodes

DEF_TP(LONG    ,"long"    , TYP_LONG,    TI_LONG,  8,EPS,EPS,   2, 8, VTF_INT|VTF_I64,        TYPE_REF_LNG)
DEF_TP(ULONG   ,"ulong"   , TYP_LONG,    TI_LONG,  8,EPS,EPS,   2, 8, VTF_INT|VTF_UNS|VTF_I64,TYPE_REF_LNG)       // Only used in GT_CAST nodes

DEF_TP(FLOAT   ,"float"   , TYP_FLOAT,   TI_FLOAT, 4,  4,  4,   1, 4, VTF_FLT,        TYPE_REF_FLT)
DEF_TP(DOUBLE  ,"double"  , TYP_DOUBLE,  TI_DOUBLE,8,  8,  8,   2, 8, VTF_FLT,        TYPE_REF_DBL)

DEF_TP(REF     ,"ref"     , TYP_REF,     TI_REF,  PS,GCS,GCS, PST,PS, VTF_ANY|VTF_GCR|VTF_I,TYPE_REF_PTR)
DEF_TP(BYREF   ,"byref"   , TYP_BYREF,   TI_ERROR,PS,BRS,BRS, PST,PS, VTF_ANY|VTF_BYR|VTF_I,TYPE_REF_BYR)
DEF_TP(STRUCT  ,"struct"  , TYP_STRUCT,  TI_STRUCT,0,  0,  0,   1, 4, VTF_S,          TYPE_REF_STC)

DEF_TP(BLK     ,"blk"     , TYP_BLK,     TI_ERROR, 0,  0,  0,   1, 4, VTF_ANY,        0           ) // blob of memory
DEF_TP(LCLBLK  ,"lclBlk"  , TYP_LCLBLK,  TI_ERROR, 0,  0,  0,   1, 4, VTF_ANY,        0           ) // preallocated memory for locspace

#ifdef FEATURE_SIMD
// Amd64: The size and alignment of SIMD vector varies at JIT time based on whether target arch supports AVX or SSE2.
DEF_TP(SIMD8    ,"simd8"  , TYP_SIMD8,   TI_STRUCT, 8, 8,  8,   2, 8, VTF_S,          TYPE_REF_STC)
DEF_TP(SIMD12   ,"simd12" , TYP_SIMD12,  TI_STRUCT,12,16, 16,   4,16, VTF_S,          TYPE_REF_STC)
DEF_TP(SIMD16   ,"simd16" , TYP_SIMD16,  TI_STRUCT,16,16, 16,   4,16, VTF_S,          TYPE_REF_STC)
DEF_TP(SIMD32   ,"simd32" , TYP_SIMD32,  TI_STRUCT,32,32, 32,   8,16, VTF_S,          TYPE_REF_STC)
#endif // FEATURE_SIMD

DEF_TP(UNKNOWN ,"unknown" ,TYP_UNKNOWN,  TI_ERROR, 0,  0,  0,   0, 0, VTF_ANY,        0           )
// clang-format on

#undef GCS
#undef BRS
#undef EPS
#undef PS
#undef PST
#undef VTF_I32
#undef VTF_I64
