// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This header contains the interpreter method / bytecode runtime representation.
// It is shared by the interpreter compiler library, the interpreter executor in
// the main coreclr library, and the JIT's interpreter-IR backend, so the layout
// is defined in exactly one place.

#ifndef _INTERPMETHOD_H_
#define _INTERPMETHOD_H_

#include "intopsshared.h"

#define INTERP_STACK_SLOT_SIZE 8u    // Alignment of each var offset on the interpreter stack
#define INTERP_STACK_ALIGNMENT 16u   // Alignment of interpreter stack at the start of a frame

#ifdef INTERPRETER_COMPILER_INTERNAL
#define COMPILER_SHARED_TYPE(compilerType, vmType, fieldName) compilerType fieldName
#else
#define COMPILER_SHARED_TYPE(compilerType, vmType, fieldName) vmType fieldName
class MethodDesc;
class MethodTable;
#endif

struct CallStubHeader;

struct InterpMethod
{
#if DEBUG
    InterpMethod *self;
#endif
    COMPILER_SHARED_TYPE(CORINFO_METHOD_HANDLE, DPTR(MethodDesc), methodHnd);
    int32_t argsSize;
    int32_t allocaSize;
    void** pDataItems;
    // This stub is used for calling the interpreted method from JITted/AOTed code
    CallStubHeader *pCallStub;
    bool initLocals;
    bool unmanagedCallersOnly;
    bool publishSecretStubParam;
    int32_t codeSize; // size in int32_t slots

    // Maps Continuation.State (suspension-point index) to the byte offset of the matching
    // INTOP_HANDLE_CONTINUATION_RESUME opcode from InterpByteCodeStart.
    int32_t numSuspensionPoints;
    int32_t* suspensionPointIPOffsets;

#ifdef INTERPRETER_COMPILER_INTERNAL
    InterpMethod(
        CORINFO_METHOD_HANDLE methodHnd, int32_t argsSize, int32_t allocaSize,
        void** pDataItems, bool initLocals, bool unmanagedCallersOnly,
        bool publishSecretStubParam, int32_t codeSize
    )
    {
#if DEBUG
        this->self = this;
#endif
        this->methodHnd = methodHnd;
        this->argsSize = argsSize;
        this->allocaSize = allocaSize;
        this->pDataItems = pDataItems;
        this->initLocals = initLocals;
        this->unmanagedCallersOnly = unmanagedCallersOnly;
        this->publishSecretStubParam = publishSecretStubParam;
        this->codeSize = codeSize;
        this->numSuspensionPoints = 0;
        this->suspensionPointIPOffsets = NULL;
        pCallStub = NULL;
    }
#endif

    bool CheckIntegrity()
    {
#if DEBUG
        return this->self == this;
#else
        return true;
#endif
    }
};

struct InterpByteCodeStart
{
#ifndef DPTR
    InterpMethod* const Method; // Pointer to the InterpMethod structure
#else
    DPTR(InterpMethod) const Method; // Pointer to the InterpMethod structure
#endif
    const int32_t* GetByteCodes() const
    {
        return reinterpret_cast<const int32_t*>(this + 1);
    }
};

#endif // _INTERPMETHOD_H_
