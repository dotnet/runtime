// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdint.h>

typedef int (*BinaryOperation)(int, int);

#ifdef LLVM_LTO_POINTER_GETTER
extern int ManagedAdd(int, int);
BinaryOperation NativeGetPointer(void) { return &ManagedAdd; }
#else
#ifdef _WIN32
#include <windows.h>
#else
#include <pthread.h>
#endif

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
WRAP1(uint8_t, Narrow, int)
WRAP1(int16_t, Enum, int16_t)
WRAP2(intptr_t, NativeInt, intptr_t, int)
WRAP1(int, SafeDivide, int)
WRAP1(unsigned long long, SafeRemainder, unsigned long long)
WRAP2(int, LocalAddress, int, int)

extern int16_t ManagedSmallArguments(int8_t, uint16_t, uint16_t, uint8_t);
int16_t NativeSmallArguments(int8_t x, uint16_t y, uint16_t z, uint8_t flag) { return ManagedSmallArguments(x, y, z, flag); }

// Floating-point operations.
WRAP2(float, FloatArithmetic, float, float)
WRAP2(double, DoubleArithmetic, double, double)
WRAP2(int, FloatCompare, double, double)
WRAP1(double, UIntToDouble, unsigned long long)
WRAP1(float, UIntToFloat, unsigned long long)
WRAP1(float, UIntToFloatViaDouble, unsigned long long)
WRAP1(int, DoubleToInt, double)
WRAP1(unsigned int, DoubleToUInt, double)
WRAP1(long long, DoubleToLong, double)
WRAP1(unsigned long long, DoubleToULong, double)
WRAP1(uint8_t, DoubleToByte, double)
WRAP1(int8_t, DoubleToSByte, double)
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
extern int ManagedCallback(int);
int NativeCallback(int x) { return ManagedCallback(x); }
int NativeCallbackPointer(UnaryOperation callback, int x) { return callback(x); }

extern int RejectedFunctionPointerCall(BinaryOperation, int, int);
int NativeRejectedIndirect(BinaryOperation callback, int x, int y) { return RejectedFunctionPointerCall(callback, x, y); }
extern int RejectedClassConstructor(int);
int NativeClassConstructor(int x) { return RejectedClassConstructor(x); }

extern BinaryOperation ManagedQuotedPointerGetter(void);
int NativeQuotedPointer(int x) { return ManagedQuotedPointerGetter()(x, 1); }

int NativeTransitionStress(int count)
{
    uint32_t sum = 0;
    for (int i = 0; i < count; i++)
        sum += (uint32_t)ManagedAdd(i, 1);
    return (int)sum;
}

typedef struct { int x, y, result; } ThreadArguments;

#ifdef _WIN32
static DWORD WINAPI NativeThreadEntry(void* state)
#else
static void* NativeThreadEntry(void* state)
#endif
{
    ThreadArguments* arguments = (ThreadArguments*)state;
    arguments->result = ManagedAdd(arguments->x, arguments->y);
    return 0;
}

int NativeForeignThread(int x, int y)
{
    ThreadArguments arguments = { x, y, -1 };
#ifdef _WIN32
    HANDLE thread = CreateThread(NULL, 0, NativeThreadEntry, &arguments, 0, NULL);
    if (thread == NULL)
        return -1;
    DWORD result = WaitForSingleObject(thread, INFINITE);
    CloseHandle(thread);
    if (result != WAIT_OBJECT_0)
        return -1;
#else
    pthread_t thread;
    if (pthread_create(&thread, NULL, NativeThreadEntry, &arguments) != 0)
        return -1;
    if (pthread_join(thread, NULL) != 0)
        return -1;
#endif
    return arguments.result;
}
#endif
