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

struct InterpHelperData {
    uint32_t addressDataItemIndex : 29;
    uint32_t accessType : 3;
};

struct CallStubHeader;

struct InterpMethod
{
#if DEBUG
    InterpMethod *self;
#endif
    CORINFO_METHOD_HANDLE methodHnd;
    int32_t argsSize;
    int32_t allocaSize;
    void** pDataItems;
    // This stub is used for calling the interpreted method from JITted/AOTed code
    CallStubHeader *pCallStub;
    bool initLocals;
    bool unmanagedCallersOnly;

    InterpMethod(
        CORINFO_METHOD_HANDLE methodHnd, int32_t argsSize, int32_t allocaSize,
        void** pDataItems, bool initLocals, bool unmanagedCallersOnly
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

enum class InterpGenericLookupType : uint32_t
{
    This,
    Method,
    Class
};

const int InterpGenericLookup_MaxIndirections = 4;

const uint16_t InterpGenericLookup_UseHelper = InterpGenericLookup_MaxIndirections + 1;

struct InterpGenericLookup
{
    // This is signature you must pass back to the runtime lookup helper
    void*                   signature;

    // Here is the helper you must call. It is one of CORINFO_HELP_RUNTIMEHANDLE_* helpers.

    InterpGenericLookupType lookupType;

    // Number of indirections to get there
    // InterpGenericLookup_UseHelper = don't know how to get it, so use helper function at run-time instead
    // 0 = use the this pointer itself (e.g. token is C<!0> inside code in sealed class C)
    //     or method desc itself (e.g. token is method void M::mymeth<!!0>() inside code in M::mymeth)
    // Otherwise, follow each byte-offset stored in the "offsets[]" array (may be negative)
    uint16_t                indirections;

    // If this is not CORINFO_NO_SIZE_CHECK, then the last indirection used needs to be checked
    // against the size stored at this offset from the previous indirection pointer. The logic
    // here is to allow for the generic dictionary to change in size without requiring any locks.
    uint16_t                sizeOffset;
    uint16_t                offsets[InterpGenericLookup_MaxIndirections];
};

enum class PInvokeCallFlags : int32_t
{
    None = 0,
    Indirect = 1 << 0, // The call target address is indirect
    SuppressGCTransition = 1 << 1, // The pinvoke is marked by the SuppressGCTransition attribute
};

enum class CalliFlags : int32_t
{
    None = 0,
    SuppressGCTransition = 1 << 1, // The call is marked by the SuppressGCTransition attribute
    PInvoke = 1 << 2, // The call is a PInvoke call
};

#define FUNCLET_STACK_ADJUSTMENT_OFFSET 8

#endif
