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
    static bool IsNativeEntryPoint(TADDR addr);
    static TADDR MarkNativeEntryPoint(TADDR entryPoint);

    static void* GetActualCode(TADDR addr);
    static void SetActualCode(TADDR addr, void* actualCode);
    static MethodDesc* GetMethodDesc(TADDR addr);
    static void* GetInterpreterData(TADDR addr);
    static void SetInterpreterData(TADDR addr, PCODE interpreterData);

private: // static
    static PortableEntryPoint* ToPortableEntryPoint(TADDR addr);

private:
    VolatilePtr<BYTE> _pActualCode;
    MethodDesc* _pMD;
    void* _pInterpreterData;

    // We keep the canary value last to ensure a stable ABI across build flavors
    INDEBUG(size_t _canary);

public:
    void Init(MethodDesc* pMD);

    // Query methods for entry point state.
    bool IsValid() const;

    bool IsByteCodeCompiled() const
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(IsValid());
        return _pInterpreterData != nullptr;
    }

    bool HasNativeCode() const
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(IsValid());
        return _pActualCode != nullptr;
    }

    bool IsReadyForR2R() const
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(IsValid());
        // State when interpreted method was prepared to be called from R2R compiled code.
        // pActualCode is a managed calling convention -> interpreter executor call stub in this case.
        return _pInterpreterData != nullptr && _pActualCode != nullptr;
    }
};

extern InterleavedLoaderHeapConfig s_stubPrecodeHeapConfig;

enum PrecodeType
{
    PRECODE_INVALID = -100,
    PRECODE_STUB,
    PRECODE_UMENTRY_THUNK,
    PRECODE_FIXUP,
    PRECODE_PINVOKE_IMPORT,
    PRECODE_INTERPRETER,
};

class StubPrecode
{
public: // static
    static const BYTE Type = PRECODE_STUB;

public:
    void Init(StubPrecode* pPrecodeRX, TADDR secretParam, LoaderAllocator *pLoaderAllocator = NULL, TADDR type = StubPrecode::Type, TADDR target = 0);

    BYTE GetType();

    void SetTargetUnconditional(TADDR target);

    TADDR GetSecretParam() const;

    MethodDesc* GetMethodDesc();
};

typedef DPTR(StubPrecode) PTR_StubPrecode;

class FixupPrecode final
{
public: // static
    static const int FixupCodeOffset = 0;

public:
    PCODE* GetTargetSlot();

    MethodDesc* GetMethodDesc();
};

class UMEntryThunk;

class Precode
{
public: // static
    static Precode* Allocate(PrecodeType t, MethodDesc* pMD,
        LoaderAllocator *pLoaderAllocator, AllocMemTracker *pamTracker);

    static Precode* GetPrecodeFromEntryPoint(PCODE addr, BOOL fSpeculative = FALSE);

public:
    PrecodeType GetType();

    UMEntryThunk* AsUMEntryThunk();

    StubPrecode* AsStubPrecode();

    MethodDesc* GetMethodDesc(BOOL fSpeculative = FALSE);

    PCODE GetEntryPoint();

    BOOL IsPointingToNativeCode(PCODE pNativeCode);

    void Reset();

    PCODE GetTarget();

    void ResetTargetInterlocked();

    BOOL SetTargetInterlocked(PCODE target, BOOL fOnlyRedirectFromPrestub = TRUE);

    BOOL IsPointingToPrestub();

    BOOL IsPointingToPrestub(PCODE target);
};

void FlushCacheForDynamicMappedStub(void* code, SIZE_T size);
BOOL DoesSlotCallPrestub(PCODE pCode);

class PrecodeMachineDescriptor { };

#endif // __PRECODE_PORTABLE_H__
