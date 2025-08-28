// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#ifndef __PRECODE_PORTABLE_H__
#define __PRECODE_PORTABLE_H__

#ifndef FEATURE_PORTABLE_ENTRYPOINTS
#error Requires FEATURE_PORTABLE_ENTRYPOINTS to be set
#endif // !FEATURE_PORTABLE_ENTRYPOINTS

class PortableEntryPoint final
{
public: // static
    static bool IsNativeEntryPoint(void* addr);
    static MethodDesc* GetMethodDesc(void* addr);
    static void* GetInterpreterData(void* addr);
    static void SetInterpreterData(void* addr, PCODE interpreterData);

private: // static
    static PortableEntryPoint* ToPortableEntryPoint(void* addr);

private:
    INDEBUG(size_t _canary);
    void* _pActualCode;
    MethodDesc* _pMD;
    void* _pInterpreterData;

public:
    void Init(MethodDesc* pMD);
};

extern InterleavedLoaderHeapConfig s_stubPrecodeHeapConfig;
#ifdef HAS_FIXUP_PRECODE
extern InterleavedLoaderHeapConfig s_fixupStubPrecodeHeapConfig;
#endif

enum PrecodeType
{
    PRECODE_INVALID = -100,
    PRECODE_STUB,
    PRECODE_UMENTRY_THUNK,
    PRECODE_FIXUP,
    PRECODE_PINVOKE_IMPORT,
    PRECODE_THISPTR_RETBUF,
};

class StubPrecode
{
public: // static
    static const BYTE Type = PRECODE_STUB;

    static void StaticInitialize() { }

public:
    void Init(StubPrecode* pPrecodeRX, TADDR secretParam, LoaderAllocator *pLoaderAllocator = NULL, TADDR type = StubPrecode::Type, TADDR target = 0) { }

    void SetTargetUnconditional(TADDR target) { }

    TADDR GetSecretParam() const { return (TADDR)NULL; }

    MethodDesc* GetMethodDesc() { return NULL; }
};

class FixupPrecode final
{
public: // static
    static const int FixupCodeOffset = 0;

    static void StaticInitialize() { }

public:
    PCODE* GetTargetSlot() { return NULL; }

    MethodDesc* GetMethodDesc() { return NULL; }
};

class PInvokeImportPrecode final : StubPrecode
{
public:
    LPVOID GetEntrypoint() { return NULL; }
};

class UMEntryThunk;

class Precode
{
public: // static
    static Precode* Allocate(PrecodeType t, MethodDesc* pMD,
        LoaderAllocator *pLoaderAllocator, AllocMemTracker *pamTracker)
    {
        return NULL;
    }

    static Precode* GetPrecodeFromEntryPoint(PCODE addr, BOOL fSpeculative = FALSE)
    {
        return NULL;
    }

public:
    PrecodeType GetType() { return (PrecodeType)0; }

    UMEntryThunk* AsUMEntryThunk() { return NULL; }

    PInvokeImportPrecode* AsPInvokeImportPrecode() { return NULL; }

    MethodDesc* GetMethodDesc(BOOL fSpeculative = FALSE) { return NULL; }

    PCODE GetEntryPoint() { return (PCODE)NULL; }

    BOOL IsPointingToNativeCode(PCODE pNativeCode) { return FALSE; }

    void Reset() { }

    PCODE GetTarget() { return (PCODE)NULL; }

    void ResetTargetInterlocked() { }

    BOOL SetTargetInterlocked(PCODE target, BOOL fOnlyRedirectFromPrestub = TRUE) { return FALSE; }

    BOOL IsPointingToPrestub() { return FALSE; }

    BOOL IsPointingToPrestub(PCODE target) { return FALSE; }
};

void FlushCacheForDynamicMappedStub(void* code, SIZE_T size);
BOOL DoesSlotCallPrestub(PCODE pCode);

struct PrecodeMachineDescriptor
{
    static void Init(PrecodeMachineDescriptor* dest) { }
};

#endif // __PRECODE_PORTABLE_H__
