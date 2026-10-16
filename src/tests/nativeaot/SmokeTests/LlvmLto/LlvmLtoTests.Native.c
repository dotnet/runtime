// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdint.h>

typedef int (*BinaryOperation)(int, int);

#ifdef LLVM_LTO_POINTER_GETTER
extern int ManagedAdd(int, int);
BinaryOperation NativeGetPointer(void) { return &ManagedAdd; }
#else
#define WRAP1(ret, name, type) extern ret Managed##name(type x); ret Native##name(type x) { return Managed##name(x); }
#define WRAP2(ret, name, first, second) extern ret Managed##name(first x, second y); ret Native##name(first x, second y) { return Managed##name(x, y); }

// Integer operations.
WRAP2(long long, Wide, long long, long long)
WRAP2(long long, Shift, long long, int)
WRAP1(long long, Convert, int)
WRAP2(int, Compare, unsigned long long, unsigned long long)
WRAP2(int, Locals, int, int)
WRAP2(int, Phi, int, int)
WRAP1(int, Switch, int)
// RuntimeImport/RuntimeExport use the managed helper ABI, including normalized
// 32-bit integer returns. These are intentionally not public C ABI signatures.
WRAP1(unsigned int, Narrow, int)
WRAP1(int, Enum, int)
WRAP2(intptr_t, NativeInt, intptr_t, int)
WRAP1(int, SafeDivide, int)
WRAP1(unsigned long long, SafeRemainder, unsigned long long)
WRAP2(int, LocalAddress, int, int)

extern int ManagedSmallArguments(int, unsigned int, unsigned int, unsigned int);
int NativeSmallArguments(int x, unsigned int y, unsigned int z, unsigned int flag) { return ManagedSmallArguments(x, y, z, flag); }

// Floating-point operations.
WRAP2(float, FloatArithmetic, float, float)
WRAP2(double, DoubleArithmetic, double, double)
WRAP2(int, FloatCompare, double, double)
WRAP1(double, UIntToDouble, unsigned long long)
WRAP1(int, DoubleToInt, double)
WRAP1(unsigned int, DoubleToUInt, double)
WRAP1(long long, DoubleToLong, double)
WRAP1(unsigned long long, DoubleToULong, double)
WRAP1(unsigned int, DoubleToByte, double)
WRAP1(int, DoubleToSByte, double)
WRAP1(int, LocalBitcast, float)

typedef enum { MathOperation_Abs, MathOperation_Sqrt, MathOperation_Floor, MathOperation_Ceiling,
    MathOperation_Truncate, MathOperation_CopySign, MathOperation_Identity } MathOperation;
extern double ManagedMath(double, double, MathOperation);
double NativeMath(double x, double y, MathOperation operation) { return ManagedMath(x, y, operation); }
extern float ManagedMathF(float, float, MathOperation);
float NativeMathF(float x, float y, MathOperation operation) { return ManagedMathF(x, y, operation); }

// Calls and function pointers.
extern int ManagedAdd(int, int);
extern int ManagedSubtract(int, int);
int NativeFold(int x) { return ManagedAdd(x, 1) ^ 0x1234; }
WRAP1(int, Compose, int)
WRAP1(int, KnownCalli, int)

extern void ManagedVoid(void);
int NativeVoid(void) { ManagedVoid(); return 37; }

extern int RejectedNoInline(int);
int NativeNoInline(int x) { return RejectedNoInline(x); }

extern BinaryOperation NativeGetPointer(void);
// The getter is compiled separately from this file: LTO can recover its target.
int NativeKnownPointer(int x) { return NativeGetPointer()(x, 1) ^ 0x1234; }

// This target is supplied by the opaque RyuJIT caller and must remain indirect.
int NativeRuntimePointer(BinaryOperation callback, int x, int y) { return callback(x, y); }

extern BinaryOperation ManagedPointerGetter(void);
extern BinaryOperation ManagedPointerSelect(BinaryOperation, BinaryOperation, int);
int NativeThroughManagedPointer(int x) { return ManagedPointerGetter()(x, 1); }
int NativePointerSelect(int x) { return ManagedPointerSelect(&ManagedAdd, &ManagedSubtract, x & 1)(x, 1); }

typedef int (*UnaryOperation)(int);
extern int RejectedUnmanagedEntry(int);
int NativeCallback(int x) { return RejectedUnmanagedEntry(x); }
int NativeCallbackPointer(UnaryOperation callback, int x) { return callback(x); }
#endif
