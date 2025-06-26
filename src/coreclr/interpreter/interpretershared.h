// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This header contains definitions needed by this compiler library and also by
// the interpreter executor in the main coreclr library
#ifndef _INTERPRETERSHARED_H_
#define _INTERPRETERSHARED_H_

#include "intopsshared.h"

#ifdef _MSC_VER
#define INTERP_API
#else
#define INTERP_API __attribute__ ((visibility ("default")))
#endif // _MSC_VER

#define INTERP_STACK_SLOT_SIZE 8    // Alignment of each var offset on the interpreter stack
#define INTERP_STACK_ALIGNMENT 16   // Alignment of interpreter stack at the start of a frame

#define INTERP_INDIRECT_HELPER_TAG 1 // When a helper ftn's address is indirect we tag it with this tag bit

struct CallStubHeader;

struct InterpMethod
{
#if DEBUG
    InterpMethod *self;
#endif
    CORINFO_METHOD_HANDLE methodHnd;
    int32_t allocaSize;
    void** pDataItems;
    // This stub is used for calling the interpreted method from JITted/AOTed code
    CallStubHeader *pCallStub;
    bool initLocals;

    InterpMethod(CORINFO_METHOD_HANDLE methodHnd, int32_t allocaSize, void** pDataItems, bool initLocals)
    {
#if DEBUG
        this->self = this;
#endif
        this->methodHnd = methodHnd;
        this->allocaSize = allocaSize;
        this->pDataItems = pDataItems;
        this->initLocals = initLocals;
        pCallStub = NULL;
    }

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

typedef class ICorJitInfo* COMP_HANDLE;

class MethodSet
{
private:
    struct MethodName
    {
        MethodName* m_next;
        const char* m_patternStart;
        const char* m_patternEnd;
        bool        m_containsClassName;
        bool        m_classNameContainsInstantiation;
        bool        m_methodNameContainsInstantiation;
        bool        m_containsSignature;
        bool        m_containsAssemblyName;
    };

    const char* m_listFromConfig = nullptr;
    MethodName* m_names = nullptr;

    MethodSet(const MethodSet& other)            = delete;
    MethodSet& operator=(const MethodSet& other) = delete;

public:
    MethodSet()
    {
    }

    ~MethodSet()
    {
        destroy();
    }

    const char* list() const
    {
        return m_listFromConfig;
    }

    void initialize(const char* listFromConfig);
    void destroy();

    inline bool isEmpty() const
    {
        return m_names == nullptr;
    }
    bool contains(COMP_HANDLE comp, CORINFO_METHOD_HANDLE methodHnd, CORINFO_CLASS_HANDLE classHnd, CORINFO_SIG_INFO* sigInfo) const;
};

const CORINFO_CLASS_HANDLE  NO_CLASS_HANDLE  = nullptr;
const CORINFO_FIELD_HANDLE  NO_FIELD_HANDLE  = nullptr;
const CORINFO_METHOD_HANDLE NO_METHOD_HANDLE = nullptr;

#endif
