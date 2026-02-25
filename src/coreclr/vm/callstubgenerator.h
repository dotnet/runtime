// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef CALLSTUBGENERATOR_H
#define CALLSTUBGENERATOR_H

#include "callingconvention.h"

class MethodDesc;
class AllocMemTracker;

// This is a header for a call stub that translates arguments from the interpreter stack to the CPU registers and native
// stack, invokes the target method, and translates the return value back to the interpreter stack.
struct CallStubHeader
{
    typedef void (*InvokeFunctionPtr)(PCODE *routines, int8_t*pArgs, int8_t*pRet, int totalStackSize, PTR_PTR_Object pContinuationRet);

    // Number of routines in the Routines array.
    int NumRoutines;
    // Index of the target method slot within the Routines array.
    // For normal calls, this is NumRoutines - 1.
    // For Swift lowered returns, store routines follow the target slot.
    int TargetSlotIndex;
    // Total stack size used for the arguments.
    int TotalStackSize;
    bool HasContinuationRet; // Indicates whether the stub supports returning a continuation
    bool HasSwiftError; // Indicates whether the stub has a Swift error parameter
    // This is a pointer to a helper function that invokes the target method. There are several
    // versions of this function, depending on the return type of the target method.
    InvokeFunctionPtr Invoke;
    // This is an array of routines that translate the arguments from the interpreter stack to the CPU registers and native stack.
    PCODE Routines[0];

    CallStubHeader(int numRoutines, int targetSlotIndex, PCODE *pRoutines, int totalStackSize, bool hasContinuationRet, bool hasSwiftError, InvokeFunctionPtr pInvokeFunction)
    {
        LIMITED_METHOD_CONTRACT;

        NumRoutines = numRoutines;
        TargetSlotIndex = targetSlotIndex;
        TotalStackSize = totalStackSize;
        Invoke = pInvokeFunction;
        HasContinuationRet = hasContinuationRet;
        HasSwiftError = hasSwiftError;

        memcpy(Routines, pRoutines, NumRoutines * sizeof(PCODE));
    }

    // Set the address of the target method to call.
    void SetTarget(PCODE target)
    {
        LIMITED_METHOD_CONTRACT;

        VolatileStore(&Routines[TargetSlotIndex], target);
    }

    PCODE GetTarget()
    {
        LIMITED_METHOD_CONTRACT;

        return VolatileLoadWithoutBarrier(&Routines[TargetSlotIndex]);
    }

    size_t GetSize()
    {
        LIMITED_METHOD_CONTRACT;

        // The size of the CallStubHeader is the size of the header plus the size of the routines array.
        return sizeof(CallStubHeader) + (NumRoutines * sizeof(PCODE));
    }
};

// This class generates the call stub for a given method. It uses the calling convention of the target CPU to determine
// how to translate the arguments from the interpreter stack to the CPU registers and native stack.
class CallStubGenerator
{
    enum ReturnType
    {
        ReturnTypeVoid,
        ReturnTypeI1,
        ReturnTypeU1,
        ReturnTypeI2,
        ReturnTypeU2,
        ReturnTypeI8,
        ReturnTypeDouble,
#ifdef TARGET_32BIT
        ReturnTypeI4,
        ReturnTypeFloat,
#endif // TARGET_32BIT
#if defined(TARGET_AMD64) || defined(TARGET_ARM)
        ReturnTypeBuffArg1,
        ReturnTypeBuffArg2,
#else
#if defined(TARGET_ARM64) && defined(TARGET_WINDOWS)
        ReturnTypeBuffArg2,
#endif
        ReturnTypeBuff,
#endif
#ifdef UNIX_AMD64_ABI
        ReturnTypeI8I8,
        ReturnTypeDoubleDouble,
        ReturnTypeI8Double,
        ReturnTypeDoubleI8,
#endif // UNIX_AMD64_ABI
#ifdef TARGET_ARM64
        ReturnType2I8,
        ReturnType2Double,
        ReturnType3Double,
        ReturnType4Double,
        ReturnTypeFloat,
        ReturnType2Float,
        ReturnType3Float,
        ReturnType4Float,
        ReturnTypeVector64,
        ReturnType2Vector64,
        ReturnType3Vector64,
        ReturnType4Vector64,
        ReturnTypeVector128,
        ReturnType2Vector128,
        ReturnType3Vector128,
        ReturnType4Vector128,
#if defined(TARGET_APPLE)
        ReturnTypeSwiftLowered,
#endif // TARGET_APPLE
#endif // TARGET_ARM64
#if defined(TARGET_RISCV64)
        ReturnType2I8,
        ReturnType2Double,
        ReturnTypeFloatInt,
        ReturnTypeIntFloat,
#endif // TARGET_RISCV64
    };

    enum class RoutineType
    {
        None,
        GPReg,
        FPReg,
#ifdef TARGET_ARM64
        FPReg32,
        FPReg128,
#if defined(TARGET_APPLE)
        SwiftSelf,
        SwiftSelfByRef,
        SwiftError,
        SwiftIndirectResult,
#endif // TARGET_APPLE
#endif // TARGET_ARM64
        Stack
    };

    // When the m_r1, m_x1 or m_s1 are set to NoRange, it means that there is no active range of registers or stack arguments.
    static const int NoRange = -1;

    RoutineType m_currentRoutineType = RoutineType::None;

    // Current sequential range of general purpose registers used to pass arguments.
    int m_r1 = NoRange;
    int m_r2 = NoRange;
    // Current sequential range of floating point registers used to pass arguments.
    int m_x1 = NoRange;
    int m_x2 = NoRange;
    // Current sequential range of offsets of stack arguments used to pass arguments.
    int m_s1 = NoRange;
    int m_s2 = NoRange;
    // The index of the next routine to store in the Routines array.
    int m_routineIndex = 0;
    // The index of the target method slot in the Routines array.
    int m_targetSlotIndex = -1;
    // The total stack size used for the arguments.
    int m_totalStackSize = 0;

    CallStubHeader::InvokeFunctionPtr m_pInvokeFunction = NULL;
    bool m_interpreterToNative = false;
    bool m_hasSwiftError = false;
    bool m_isSwiftCallConv = false;

#if !defined(UNIX_AMD64_ABI) && defined(ENREGISTERED_PARAMTYPE_MAXSIZE)
    PCODE GetGPRegRefRoutine(int r);
    PCODE GetStackRefRoutine();
#endif // !UNIX_AMD64_ABI && ENREGISTERED_PARAMTYPE_MAXSIZE
    PCODE GetStackRoutine();
#if defined(TARGET_APPLE) && defined(TARGET_ARM64)
    PCODE GetStackRoutine_1B();
    PCODE GetStackRoutine_2B();
    PCODE GetStackRoutine_4B();
#endif // TARGET_APPLE && TARGET_ARM64
    PCODE GetFPRegRangeRoutine(int x1, int x2);
#ifdef TARGET_ARM64
    PCODE GetFPReg128RangeRoutine(int x1, int x2);
    PCODE GetFPReg32RangeRoutine(int x1, int x2);
#endif // TARGET_ARM64
#if defined(TARGET_APPLE) && defined(TARGET_ARM64)
    // Swift calling convention state
    int m_swiftSelfByRefSize = 0;
    CORINFO_SWIFT_LOWERING m_swiftReturnLowering = {};
    bool m_hasSwiftReturnLowering = false;

    // Swift routine helpers
    PCODE GetSwiftSelfRoutine();
    PCODE GetSwiftSelfByRefRoutine();
    PCODE GetSwiftErrorRoutine();
    PCODE GetSwiftIndirectResultRoutine();
    PCODE GetSwiftLoadGPAtOffsetRoutine(int regIndex);
    PCODE GetSwiftLoadFPAtOffsetRoutine(int regIndex);
    PCODE GetSwiftStoreGPAtOffsetRoutine(int regIndex);
    PCODE GetSwiftStoreFPAtOffsetRoutine(int regIndex);

    // Swift lowering info for expanded struct elements
    struct SwiftLoweringElement {
        uint16_t offset;        // Offset within struct
        uint16_t structSize;    // If non-zero, this is the last element, advance x9 by this amount
        bool isFloat;           // True if this element goes in FP register
        bool isLowered;         // True if this is part of a lowered struct
    };

    void RewriteSignatureForSwiftLowering(MetaSig &sig, SigBuilder &swiftSigBuilder, CQuickArray<SwiftLoweringElement> &swiftLoweringInfo, int &swiftIndirectResultCount);
    bool ProcessSwiftSpecialArgument(MethodTable* pArgMT, int interpStackSlotSize, int32_t &interpreterStackOffset, PCODE *pRoutines);
    void EmitSwiftLoweredElementRoutine(SwiftLoweringElement &elem, ArgLocDesc &argLocDesc, PCODE *pRoutines);
    void EmitSwiftReturnLoweringRoutines(PCODE *pRoutines);
#endif // TARGET_APPLE && TARGET_ARM64
    PCODE GetGPRegRangeRoutine(int r1, int r2);
    template<typename ArgIteratorType>
    ReturnType GetReturnType(ArgIteratorType *pArgIt);
    CallStubHeader::InvokeFunctionPtr GetInvokeFunctionPtr(ReturnType returnType);
    PCODE GetInterpreterReturnTypeHandler(ReturnType returnType);
#ifdef TARGET_ARM
    PCODE GetRegRoutine_4B(int r1, int r2);
    PCODE GetStackRoutine_4B();
#endif // TARGET_ARM

    // Process the argument described by argLocDesc. This function is called for each argument in the method signature.
    template<typename ArgIteratorType>
    void ProcessArgument(ArgIteratorType *pArgIt, ArgLocDesc& argLocDesc, PCODE *pRoutines);
public:
    // Generate the call stub for the given method.
    CallStubHeader *GenerateCallStub(MethodDesc *pMD, AllocMemTracker *pamTracker, bool interpreterToNative);
    CallStubHeader *GenerateCallStubForSig(MetaSig &sig);

private:
    static size_t ComputeTempStorageSize(const MetaSig& sig)
    {
        int numArgs = sig.NumFixedArgs() + (sig.HasThis() ? 1 : 0);

        // The size of the temporary storage is the size of the CallStubHeader plus the size of the routines array.
        // The size of the routines array is three times the number of arguments plus one slot for the target method pointer.
        size_t baseSize = sizeof(CallStubHeader) + ((numArgs + 1) * 3 + 1) * sizeof(PCODE);
#if defined(TARGET_APPLE) && defined(TARGET_ARM64)
        // Add extra space for Swift return lowering (up to 4 elements * 2 slots + terminator = 9 slots).
        baseSize += 9 * sizeof(PCODE);
#endif
        return baseSize;
    }
    void ComputeCallStub(MetaSig &sig, PCODE *pRoutines, MethodDesc *pMD);
    template<typename ArgIteratorType>
    void ComputeCallStubWorker(bool hasUnmanagedCallConv, CorInfoCallConvExtension unmanagedCallConv, MetaSig &sig, PCODE *pRoutines, MethodDesc *pMD);

    void TerminateCurrentRoutineIfNotOfNewType(RoutineType type, PCODE *pRoutines);
};

void InitCallStubGenerator();

#endif // CALLSTUBGENERATOR_H
