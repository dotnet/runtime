// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*****************************************************************************/
#ifndef SIMD_INTRINSIC
#error Define SIMD_INTRINSIC before including this file
#endif
/*****************************************************************************/

// clang-format off
#ifdef FEATURE_SIMD

    /*
         Notes:
            a) TYP_UNKNOWN means 'baseType' of SIMD vector which is not known apriori
            b) Each method maps to a unique intrinsic Id
            c) To facilitate argument types to be used as an array initializer, args are listed within "{}" braces.
            d) Since comma is used as actual param seperator in a macro, TYP_UNDEF entries are added to keep param count constant.
            e) TODO-Cleanup: when we plumb TYP_SIMD through front-end, replace TYP_STRUCT with TYP_SIMD.
     */

#if defined(_TARGET_XARCH_) || defined(_TARGET_ARM64_)

// Max number of parameters that we model in the table for SIMD intrinsic methods.
#define SIMD_INTRINSIC_MAX_MODELED_PARAM_COUNT       3

// Actual maximum number of parameters for any SIMD intrinsic method.
// Constructors that take either N values, or a smaller Vector plus additional element values,
// actually have more arguments than the "modeled" count.
#define SIMD_INTRINSIC_MAX_PARAM_COUNT               5

// Max number of base types supported by an intrinsic
#define SIMD_INTRINSIC_MAX_BASETYPE_COUNT    10

/***************************************************************************************************************************************************************************************************************************
              Method Name,              Is Instance    Intrinsic Id,             Display Name,             return type,   Arg count,    Individual argument types           Supported base types
                                           Method                                                                                      (including implicit "this")
 ***************************************************************************************************************************************************************************************************************************/
SIMD_INTRINSIC(nullptr,                     false,       None,                     "None",                   TYP_UNDEF,      0,      {TYP_UNDEF, TYP_UNDEF, TYP_UNDEF},     {TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF})

SIMD_INTRINSIC("get_Count",                 false,       GetCount,                 "count",                  TYP_INT,        0,      {TYP_VOID, TYP_UNDEF, TYP_UNDEF},      {TYP_INT, TYP_FLOAT, TYP_DOUBLE, TYP_LONG, TYP_USHORT, TYP_UBYTE, TYP_BYTE, TYP_SHORT, TYP_UINT, TYP_ULONG})
SIMD_INTRINSIC("get_One",                   false,       GetOne,                   "one",                    TYP_STRUCT,     0,      {TYP_VOID, TYP_UNDEF, TYP_UNDEF},      {TYP_INT, TYP_FLOAT, TYP_DOUBLE, TYP_LONG, TYP_USHORT, TYP_UBYTE, TYP_BYTE, TYP_SHORT, TYP_UINT, TYP_ULONG})
SIMD_INTRINSIC("get_Zero",                  false,       GetZero,                  "zero",                   TYP_STRUCT,     0,      {TYP_VOID, TYP_UNDEF, TYP_UNDEF},      {TYP_INT, TYP_FLOAT, TYP_DOUBLE, TYP_LONG, TYP_USHORT, TYP_UBYTE, TYP_BYTE, TYP_SHORT, TYP_UINT, TYP_ULONG})
SIMD_INTRINSIC("get_AllOnes",               false,       GetAllOnes,               "allOnes",                TYP_STRUCT,     0,      {TYP_VOID, TYP_UNDEF, TYP_UNDEF},      {TYP_INT, TYP_FLOAT, TYP_DOUBLE, TYP_LONG, TYP_USHORT, TYP_UBYTE, TYP_BYTE, TYP_SHORT, TYP_UINT, TYP_ULONG})

// .ctor call or newobj - there are four forms.
// This form takes the object plus a value of the base (element) type:
SIMD_INTRINSIC(".ctor",                     true,        Init,                     "init",                   TYP_VOID,       2,      {TYP_BYREF, TYP_UNKNOWN, TYP_UNDEF},   {TYP_INT, TYP_FLOAT, TYP_DOUBLE, TYP_LONG, TYP_USHORT, TYP_UBYTE, TYP_BYTE, TYP_SHORT, TYP_UINT, TYP_ULONG})
// This form takes the object plus an array of the base (element) type:
SIMD_INTRINSIC(".ctor",                     true,        InitArray,                "initArray",              TYP_VOID,       2,      {TYP_BYREF, TYP_REF,     TYP_UNDEF},   {TYP_INT, TYP_FLOAT, TYP_DOUBLE, TYP_LONG, TYP_USHORT, TYP_UBYTE, TYP_BYTE, TYP_SHORT, TYP_UINT, TYP_ULONG})
// This form takes the object, an array of the base (element) type, and an index into the array:
SIMD_INTRINSIC(".ctor",                     true,        InitArrayX,               "initArray",              TYP_VOID,       3,      {TYP_BYREF, TYP_REF,     TYP_INT  },   {TYP_INT, TYP_FLOAT, TYP_DOUBLE, TYP_LONG, TYP_USHORT, TYP_UBYTE, TYP_BYTE, TYP_SHORT, TYP_UINT, TYP_ULONG})
// This form takes the object, and N values of the base (element) type.  The actual number of arguments depends upon the Vector size, which must be a fixed type such as Vector2f/3f/4f
// Right now this intrinsic is supported only on fixed float vectors and hence the supported base types lists only TYP_FLOAT.
// This is currently the intrinsic that has the largest maximum number of operands - if we add new fixed vector types
// with more than 4 elements, the above SIMD_INTRINSIC_MAX_PARAM_COUNT will have to change.
SIMD_INTRINSIC(".ctor",                     true,        InitN,                    "initN",                  TYP_VOID,       2,      {TYP_BYREF, TYP_UNKNOWN, TYP_UNKNOWN}, {TYP_FLOAT, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF})
// This form takes the object, a smaller fixed vector, and one or two additional arguments of the base type, e.g. Vector3 V = new Vector3(V2, x); where V2 is a Vector2, and x is a float.
SIMD_INTRINSIC(".ctor",                     true,        InitFixed,                "initFixed",              TYP_VOID,       3,      {TYP_BYREF, TYP_STRUCT,  TYP_UNKNOWN}, {TYP_FLOAT, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF})

// Copy vector to an array
SIMD_INTRINSIC("CopyTo",                    true,        CopyToArray,               "CopyToArray",           TYP_VOID,       2,      {TYP_BYREF, TYP_REF,     TYP_UNDEF},   {TYP_INT, TYP_FLOAT, TYP_DOUBLE, TYP_LONG, TYP_USHORT, TYP_UBYTE, TYP_BYTE, TYP_SHORT, TYP_UINT, TYP_ULONG})
SIMD_INTRINSIC("CopyTo",                    true,        CopyToArrayX,              "CopyToArray",           TYP_VOID,       3,      {TYP_BYREF, TYP_REF,     TYP_INT  },   {TYP_INT, TYP_FLOAT, TYP_DOUBLE, TYP_LONG, TYP_USHORT, TYP_UBYTE, TYP_BYTE, TYP_SHORT, TYP_UINT, TYP_ULONG})

// Get operations
SIMD_INTRINSIC("get_Item",                  true,        GetItem,                  "get[i]",                 TYP_UNKNOWN,    2,      {TYP_BYREF, TYP_INT,     TYP_UNDEF},   {TYP_INT, TYP_FLOAT, TYP_DOUBLE, TYP_LONG, TYP_USHORT, TYP_UBYTE, TYP_BYTE, TYP_SHORT, TYP_UINT, TYP_ULONG})
SIMD_INTRINSIC("get_X",                     true,        GetX,                     "getX",                   TYP_UNKNOWN,    1,      {TYP_BYREF, TYP_UNDEF,   TYP_UNDEF},   {TYP_FLOAT, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF})
SIMD_INTRINSIC("get_Y",                     true,        GetY,                     "getY",                   TYP_UNKNOWN,    1,      {TYP_BYREF, TYP_UNDEF,   TYP_UNDEF},   {TYP_FLOAT, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF})
SIMD_INTRINSIC("get_Z",                     true,        GetZ,                     "getZ",                   TYP_UNKNOWN,    1,      {TYP_BYREF, TYP_UNDEF,   TYP_UNDEF},   {TYP_FLOAT, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF})
SIMD_INTRINSIC("get_W",                     true,        GetW,                     "getW",                   TYP_UNKNOWN,    1,      {TYP_BYREF, TYP_UNDEF,   TYP_UNDEF},   {TYP_FLOAT, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF})

// Set operations
SIMD_INTRINSIC("set_X",                     true,        SetX,                     "setX",                   TYP_VOID,       2,      {TYP_BYREF, TYP_UNKNOWN,   TYP_UNDEF},   {TYP_FLOAT, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF})
SIMD_INTRINSIC("set_Y",                     true,        SetY,                     "setY",                   TYP_VOID,       2,      {TYP_BYREF, TYP_UNKNOWN,   TYP_UNDEF},   {TYP_FLOAT, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF})
SIMD_INTRINSIC("set_Z",                     true,        SetZ,                     "setZ",                   TYP_VOID,       2,      {TYP_BYREF, TYP_UNKNOWN,   TYP_UNDEF},   {TYP_FLOAT, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF})
SIMD_INTRINSIC("set_W",                     true,        SetW,                     "setW",                   TYP_VOID,       2,      {TYP_BYREF, TYP_UNKNOWN,   TYP_UNDEF},   {TYP_FLOAT, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF})

// Object.Equals()
SIMD_INTRINSIC("Equals",                    true,        InstEquals,               "equals",                 TYP_BOOL,       2,      {TYP_BYREF, TYP_STRUCT, TYP_UNDEF},    {TYP_INT, TYP_FLOAT, TYP_DOUBLE, TYP_LONG, TYP_USHORT, TYP_UBYTE, TYP_BYTE, TYP_SHORT, TYP_UINT, TYP_ULONG})

// Operator == and !=
SIMD_INTRINSIC("op_Equality",               false,       OpEquality,               "==",                     TYP_BOOL,       2,      {TYP_STRUCT, TYP_STRUCT, TYP_UNDEF},   {TYP_INT, TYP_FLOAT, TYP_DOUBLE, TYP_LONG, TYP_USHORT, TYP_UBYTE, TYP_BYTE, TYP_SHORT, TYP_UINT, TYP_ULONG})
SIMD_INTRINSIC("op_Inequality",             false,       OpInEquality,             "!=",                     TYP_BOOL,       2,      {TYP_STRUCT, TYP_STRUCT, TYP_UNDEF},   {TYP_INT, TYP_FLOAT, TYP_DOUBLE, TYP_LONG, TYP_USHORT, TYP_UBYTE, TYP_BYTE, TYP_SHORT, TYP_UINT, TYP_ULONG})

// Arithmetic Operations
SIMD_INTRINSIC("op_Addition",               false,       Add,                      "+",                      TYP_STRUCT,     2,      {TYP_STRUCT, TYP_STRUCT, TYP_UNDEF},   {TYP_INT, TYP_FLOAT, TYP_DOUBLE, TYP_LONG, TYP_USHORT, TYP_UBYTE, TYP_BYTE, TYP_SHORT, TYP_UINT, TYP_ULONG})
SIMD_INTRINSIC("op_Subtraction",            false,       Sub,                      "-",                      TYP_STRUCT,     2,      {TYP_STRUCT, TYP_STRUCT, TYP_UNDEF},   {TYP_INT, TYP_FLOAT, TYP_DOUBLE, TYP_LONG, TYP_USHORT, TYP_UBYTE, TYP_BYTE, TYP_SHORT, TYP_UINT, TYP_ULONG})

#if defined(_TARGET_XARCH_)
SIMD_INTRINSIC("op_Multiply",               false,       Mul,                      "*",                      TYP_STRUCT,     2,      {TYP_STRUCT, TYP_STRUCT, TYP_UNDEF},   {TYP_INT, TYP_FLOAT, TYP_DOUBLE, TYP_SHORT,TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF})
#elif defined(_TARGET_ARM64_)
// TODO-ARM64-CQ Investigate code sequence to accelerate LONG/ULONG vector multiply
SIMD_INTRINSIC("op_Multiply",               false,       Mul,                      "*",                      TYP_STRUCT,     2,      {TYP_STRUCT, TYP_STRUCT, TYP_UNDEF},   {TYP_INT, TYP_FLOAT, TYP_DOUBLE, TYP_USHORT, TYP_UBYTE, TYP_BYTE, TYP_SHORT, TYP_UINT, TYP_UNDEF, TYP_UNDEF})
#endif

SIMD_INTRINSIC("op_Division",               false,       Div,                      "/",                      TYP_STRUCT,     2,      {TYP_STRUCT, TYP_STRUCT, TYP_UNDEF},   {TYP_FLOAT, TYP_DOUBLE, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF})

// SquareRoot is recognized as an intrinsic only for float or double vectors
SIMD_INTRINSIC("SquareRoot",                false,       Sqrt,                     "sqrt",                   TYP_STRUCT,     1,      {TYP_STRUCT, TYP_UNDEF, TYP_UNDEF},    {TYP_FLOAT, TYP_DOUBLE, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF})

SIMD_INTRINSIC("Min",                       false,       Min,                      "min",                    TYP_STRUCT,     2,      {TYP_STRUCT, TYP_STRUCT, TYP_UNDEF},   {TYP_INT, TYP_FLOAT, TYP_DOUBLE, TYP_LONG, TYP_USHORT, TYP_UBYTE, TYP_BYTE, TYP_SHORT, TYP_UINT, TYP_ULONG})
SIMD_INTRINSIC("Max",                       false,       Max,                      "max",                    TYP_STRUCT,     2,      {TYP_STRUCT, TYP_STRUCT, TYP_UNDEF},   {TYP_INT, TYP_FLOAT, TYP_DOUBLE, TYP_LONG, TYP_USHORT, TYP_UBYTE, TYP_BYTE, TYP_SHORT, TYP_UINT, TYP_ULONG})
SIMD_INTRINSIC("Abs",                       false,       Abs,                      "abs",                    TYP_STRUCT,     1,      {TYP_STRUCT, TYP_UNDEF, TYP_UNDEF },   {TYP_INT, TYP_FLOAT, TYP_DOUBLE, TYP_LONG, TYP_USHORT, TYP_UBYTE, TYP_BYTE, TYP_SHORT, TYP_UINT, TYP_ULONG})

// Vector Relational operators
SIMD_INTRINSIC("Equals",                    false,       Equal,                    "eq",                     TYP_STRUCT,     2,      {TYP_STRUCT, TYP_STRUCT, TYP_UNDEF},   {TYP_INT, TYP_FLOAT, TYP_DOUBLE, TYP_LONG, TYP_USHORT, TYP_UBYTE, TYP_BYTE, TYP_SHORT, TYP_UINT, TYP_ULONG})
SIMD_INTRINSIC("LessThan",                  false,       LessThan,                 "lt",                     TYP_STRUCT,     2,      {TYP_STRUCT, TYP_STRUCT, TYP_UNDEF},   {TYP_INT, TYP_FLOAT, TYP_DOUBLE, TYP_LONG, TYP_USHORT, TYP_UBYTE, TYP_BYTE, TYP_SHORT, TYP_UINT, TYP_ULONG})
SIMD_INTRINSIC("LessThanOrEqual",           false,       LessThanOrEqual,          "le",                     TYP_STRUCT,     2,      {TYP_STRUCT, TYP_STRUCT, TYP_UNDEF},   {TYP_INT, TYP_FLOAT, TYP_DOUBLE, TYP_LONG, TYP_USHORT, TYP_UBYTE, TYP_BYTE, TYP_SHORT, TYP_UINT, TYP_ULONG})
SIMD_INTRINSIC("GreaterThan",               false,       GreaterThan,              "gt",                     TYP_STRUCT,     2,      {TYP_STRUCT, TYP_STRUCT, TYP_UNDEF},   {TYP_INT, TYP_FLOAT, TYP_DOUBLE, TYP_LONG, TYP_USHORT, TYP_UBYTE, TYP_BYTE, TYP_SHORT, TYP_UINT, TYP_ULONG})
SIMD_INTRINSIC("GreaterThanOrEqual",        false,       GreaterThanOrEqual,       "ge",                     TYP_STRUCT,     2,      {TYP_STRUCT, TYP_STRUCT, TYP_UNDEF},   {TYP_INT, TYP_FLOAT, TYP_DOUBLE, TYP_LONG, TYP_USHORT, TYP_UBYTE, TYP_BYTE, TYP_SHORT, TYP_UINT, TYP_ULONG})

// Bitwise operations
SIMD_INTRINSIC("op_BitwiseAnd",             false,       BitwiseAnd,               "&",                      TYP_STRUCT,     2,      {TYP_STRUCT, TYP_STRUCT, TYP_UNDEF},   {TYP_INT, TYP_FLOAT, TYP_DOUBLE, TYP_LONG, TYP_USHORT, TYP_UBYTE, TYP_BYTE, TYP_SHORT, TYP_UINT, TYP_ULONG})
SIMD_INTRINSIC("AndNot",                    false,       BitwiseAndNot,            "&~",                     TYP_STRUCT,     2,      {TYP_STRUCT, TYP_STRUCT, TYP_UNDEF},   {TYP_INT, TYP_FLOAT, TYP_DOUBLE, TYP_LONG, TYP_USHORT, TYP_UBYTE, TYP_BYTE, TYP_SHORT, TYP_UINT, TYP_ULONG})
SIMD_INTRINSIC("op_BitwiseOr",              false,       BitwiseOr,                "|",                      TYP_STRUCT,     2,      {TYP_STRUCT, TYP_STRUCT, TYP_UNDEF},   {TYP_INT, TYP_FLOAT, TYP_DOUBLE, TYP_LONG, TYP_USHORT, TYP_UBYTE, TYP_BYTE, TYP_SHORT, TYP_UINT, TYP_ULONG})
SIMD_INTRINSIC("op_ExclusiveOr",            false,       BitwiseXor,               "^",                      TYP_STRUCT,     2,      {TYP_STRUCT, TYP_STRUCT, TYP_UNDEF},   {TYP_INT, TYP_FLOAT, TYP_DOUBLE, TYP_LONG, TYP_USHORT, TYP_UBYTE, TYP_BYTE, TYP_SHORT, TYP_UINT, TYP_ULONG})

// Dot Product
#if defined(_TARGET_XARCH_)
// Is supported only on Vector<int> on AVX.
SIMD_INTRINSIC("Dot",                       false,       DotProduct,               "Dot",                    TYP_UNKNOWN,    2,      {TYP_STRUCT, TYP_STRUCT, TYP_UNDEF},   {TYP_INT, TYP_FLOAT, TYP_DOUBLE, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF})
#elif defined(_TARGET_ARM64_)
// Dot Product does not support LONG/ULONG due to lack of multiply support (see TODO-ARM64-CQ above)
SIMD_INTRINSIC("Dot",                       false,       DotProduct,               "Dot",                    TYP_UNKNOWN,    2,      {TYP_STRUCT, TYP_STRUCT, TYP_UNDEF},   {TYP_INT, TYP_FLOAT, TYP_DOUBLE, TYP_USHORT, TYP_UBYTE, TYP_BYTE, TYP_SHORT, TYP_UINT, TYP_UNDEF, TYP_UNDEF})
#endif

// Select
SIMD_INTRINSIC("ConditionalSelect",         false,       Select,                   "Select",                 TYP_STRUCT,     3,      {TYP_STRUCT, TYP_STRUCT, TYP_STRUCT},  {TYP_INT, TYP_FLOAT, TYP_DOUBLE, TYP_LONG, TYP_USHORT, TYP_UBYTE, TYP_BYTE, TYP_SHORT, TYP_UINT, TYP_ULONG})

// Cast
SIMD_INTRINSIC("op_Explicit",               false,       Cast,                     "Cast",                   TYP_STRUCT,     1,      {TYP_STRUCT, TYP_UNDEF,  TYP_UNDEF},   {TYP_INT, TYP_FLOAT, TYP_DOUBLE, TYP_LONG, TYP_USHORT, TYP_UBYTE, TYP_BYTE, TYP_SHORT, TYP_UINT, TYP_ULONG})

// Convert int/uint to single
SIMD_INTRINSIC("ConvertToSingle",           false,       ConvertToSingle,          "ConvertToSingle",        TYP_STRUCT,     1,      {TYP_STRUCT, TYP_UNDEF,  TYP_UNDEF},   {TYP_INT, TYP_UINT, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF})
// Convert long/ulong to double
SIMD_INTRINSIC("ConvertToDouble",           false,       ConvertToDouble,          "ConvertToDouble",        TYP_STRUCT,     1,      {TYP_STRUCT, TYP_UNDEF,  TYP_UNDEF},   {TYP_LONG, TYP_ULONG, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF})
// Convert single to int
SIMD_INTRINSIC("ConvertToInt32",            false,       ConvertToInt32,           "ConvertToInt32",         TYP_STRUCT,     1,      {TYP_STRUCT, TYP_UNDEF,  TYP_UNDEF},   {TYP_FLOAT, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF})
// Convert double to long
SIMD_INTRINSIC("ConvertToInt64",            false,       ConvertToInt64,           "ConvertToInt64",         TYP_STRUCT,     1,      {TYP_STRUCT, TYP_UNDEF,  TYP_UNDEF},   {TYP_DOUBLE, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF})
// Narrow two input Vector<T>s to a single Vector<T>. The return value's lower elements are the elements from src1, and the upper elements are from src2.
SIMD_INTRINSIC("Narrow",                    false,       Narrow,                   "Narrow",                 TYP_STRUCT,     2,      {TYP_STRUCT, TYP_STRUCT, TYP_UNDEF},   {TYP_INT, TYP_DOUBLE, TYP_LONG, TYP_USHORT, TYP_SHORT, TYP_UINT, TYP_ULONG, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF})
// Widen one input Vector<T> to two Vector<T>s: dest1 contains the lower half of elements in src, and dest2 contains the upper half of elements in src.
SIMD_INTRINSIC("Widen",                     false,       Widen,                    "Widen",                  TYP_VOID,       3,      {TYP_STRUCT, TYP_BYREF,  TYP_BYREF},   {TYP_INT, TYP_FLOAT, TYP_USHORT, TYP_UBYTE, TYP_BYTE, TYP_SHORT, TYP_UINT, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF})

// Miscellaneous
SIMD_INTRINSIC("get_IsHardwareAccelerated", false,       HWAccel,                  "HWAccel",                TYP_BOOL,       0,      {TYP_UNDEF,  TYP_UNDEF,  TYP_UNDEF},   {TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF})

#ifdef _TARGET_XARCH_
// Shuffle and Shift operations - these are internal intrinsics as there is no corresponding managed method.
// To prevent this being accidentally recognized as an intrinsic, all of the arg types and supported base types is made TYP_UNDEF
SIMD_INTRINSIC("ShuffleSSE2",               false,       ShuffleSSE2,              "ShuffleSSE2",            TYP_STRUCT,     2,      {TYP_UNDEF, TYP_UNDEF, TYP_UNDEF},     {TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF})

// Internal, logical shift operations that shift the entire vector register instead of individual elements of the vector.
SIMD_INTRINSIC("ShiftLeftInternal",         false,       ShiftLeftInternal,        "<< Internal",            TYP_STRUCT,     2,      {TYP_UNDEF, TYP_UNDEF, TYP_UNDEF},     {TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF})
SIMD_INTRINSIC("ShiftRightInternal",        false,       ShiftRightInternal,       ">> Internal",            TYP_STRUCT,     2,      {TYP_UNDEF, TYP_UNDEF, TYP_UNDEF},     {TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF})
#endif // _TARGET_XARCH_

// Internal intrinsics for saving & restoring the upper half of a vector register
SIMD_INTRINSIC("UpperSave",                 false,       UpperSave,                "UpperSave Internal",     TYP_STRUCT,     2,      {TYP_UNDEF, TYP_UNDEF, TYP_UNDEF},     {TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF})
SIMD_INTRINSIC("UpperRestore",              false,       UpperRestore,             "UpperRestore Internal",  TYP_STRUCT,     2,      {TYP_UNDEF, TYP_UNDEF, TYP_UNDEF},     {TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF})

// Internal intrinsics for Widen
SIMD_INTRINSIC("WidenHi",                   false,       WidenHi,                   "WidenHi",               TYP_VOID,       2,      {TYP_UNDEF, TYP_UNDEF,  TYP_UNDEF},    {TYP_INT, TYP_FLOAT, TYP_USHORT, TYP_UBYTE, TYP_BYTE, TYP_SHORT, TYP_UINT, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF})
SIMD_INTRINSIC("WidenLo",                   false,       WidenLo,                   "WidenLo",               TYP_VOID,       2,      {TYP_UNDEF, TYP_UNDEF,  TYP_UNDEF},    {TYP_INT, TYP_FLOAT, TYP_USHORT, TYP_UBYTE, TYP_BYTE, TYP_SHORT, TYP_UINT, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF})

SIMD_INTRINSIC(nullptr,                     false,       Invalid,                   "Invalid",               TYP_UNDEF,      0,      {TYP_UNDEF,  TYP_UNDEF,  TYP_UNDEF},   {TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_UNDEF})
#undef SIMD_INTRINSIC
#else // !defined(_TARGET_XARCH_) && !defined(_TARGET_ARM64_)
#error SIMD intrinsics not defined for target arch
#endif // !defined(_TARGET_XARCH_) && !defined(_TARGET_ARM64_)


#endif //FEATURE_SIMD
// clang-format on
