// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: DllImportCallback.h
//

//


#ifndef __dllimportcallback_h__
#define __dllimportcallback_h__

#include "object.h"
#include "stublink.h"
#include "ceeload.h"
#include "class.h"
#include "dllimport.h"

class UMThunkMarshInfo;
typedef DPTR(class UMThunkMarshInfo) PTR_UMThunkMarshInfo;

class UMEntryThunk;
typedef DPTR(class UMEntryThunk) PTR_UMEntryThunk;

//----------------------------------------------------------------------
// This structure collects all information needed to marshal an
// unmanaged->managed thunk. The only information missing is the
// managed target and the "this" object (if any.) Those two pieces
// are broken out into a small UMEntryThunk.
//
// The idea is to share UMThunkMarshInfo's between multiple thunks
// that have the same signature while the UMEntryThunk contains the
// minimal info needed to distinguish between actual function pointers.
//----------------------------------------------------------------------

class UMThunkMarshInfo
{
    friend class CheckAsmOffsets;

private:
    enum
    {
        kLoadTimeInited = 0x4c55544d,   //'LUTM'
        kRunTimeInited  = 0x5255544d,   //'RUTM'
    };

public:
    //----------------------------------------------------------
    // This initializer can be called during load time.
    // It does not do any ML stub initialization or sigparsing.
    // The RunTimeInit() must be called subsequently before this
    // can safely be used.
    //----------------------------------------------------------
    VOID LoadTimeInit(MethodDesc* pMD);
    VOID LoadTimeInit(Signature sig, Module * pModule, MethodDesc * pMD = NULL);

    //----------------------------------------------------------
    // This initializer finishes the init started by LoadTimeInit.
    // It does all the ML stub creation, and can throw a CLR
    // exception.
    //
    // It can safely be called multiple times and by concurrent
    // threads.
    //----------------------------------------------------------
    VOID RunTimeInit();

    // Destructor.
    //----------------------------------------------------------
    ~UMThunkMarshInfo();

    //----------------------------------------------------------
    // Accessor functions
    //----------------------------------------------------------
    Signature GetSignature()
    {
        LIMITED_METHOD_CONTRACT;
        return m_sig;
    }

    Module* GetModule()
    {
        LIMITED_METHOD_CONTRACT;
        return m_pModule;
    }

    MethodDesc * GetMethod()
    {
        LIMITED_METHOD_CONTRACT;
        return m_pMD;
    }

    PCODE GetExecStubEntryPoint();

    BOOL IsCompletelyInited()
    {
        LIMITED_METHOD_CONTRACT;
        return (m_pILStub != (PCODE)1);
    }

    static MethodDesc* GetILStubMethodDesc(MethodDesc* pInvokeMD, PInvokeStaticSigInfo* pSigInfo, DWORD dwStubFlags);

    static UINT32 GetOffsetOfStub()
    {
        LIMITED_METHOD_CONTRACT;
        return (UINT32)offsetof(UMThunkMarshInfo, m_pILStub);
    }

private:
    PCODE             m_pILStub;            // IL stub for marshaling
                                            // On non-x86, the managed entrypoint for no-delegate no-marshal signatures
    MethodDesc *      m_pMD;                // maybe null
    Module *          m_pModule;
    Signature         m_sig;
};

typedef DPTR(class UMEntryThunkData) PTR_UMEntryThunkData;

//----------------------------------------------------------------------
// This structure contains the minimal information required to
// distinguish one function pointer from another, with the rest
// being stored in a shared UMThunkMarshInfo.
//
// This structure also contains the actual code bytes that form the
// front end of the thunk. A pointer to the m_code[] byte array is
// what is actually handed to unmanaged client code.
//----------------------------------------------------------------------
class UMEntryThunk : private StubPrecode
{
    friend class UMEntryThunkData;

    static const int Type = PRECODE_UMENTRY_THUNK;

public:
    PTR_UMEntryThunkData GetData() const
    {
        LIMITED_METHOD_CONTRACT;

        return dac_cast<PTR_UMEntryThunkData>(GetSecretParam());
    }
};

class UMEntryThunkData
{
    friend class UMEntryThunkFreeList;
    friend class PInvokeStubLinker;

    // The start of the managed code.
    // if m_pObjectHandle is non-NULL, this field is still set to help with diagnostic of call on collected delegate crashes
    // but it may not have the correct value.
    PCODE                   m_pManagedTarget;

#ifdef FEATURE_INTERPRETER
    // InterpreterPrecode to tailcall if the target is interpreted. This allows TheUMEntryPrestubWorker
    // stash the hidden argument in a thread static and avoid collision with the hidden argument
    // used by InterpreterPrecode.
    Volatile<PCODE>         m_pInterpretedTarget;
#endif

    // This is used for debugging and profiling.
    PTR_MethodDesc          m_pMD;

    // Object handle holding "this" reference. May be a strong or weak handle.
    // Field is NULL for a static method.
    // Field is (OBJECHANDLE)-1 for collected delegates
    OBJECTHANDLE            m_pObjectHandle;

    union
    {
        // Pointer to the shared structure containing everything else
        PTR_UMThunkMarshInfo    m_pUMThunkMarshInfo;
        // Pointer to the next UMEntryThunk in the free list. Used when it is freed.
        UMEntryThunkData *m_pNextFreeThunk;
    };

    PTR_UMEntryThunk m_pUMEntryThunk;

#ifdef _DEBUG
    enum
    {
        kLoadTimeInited = 0x4c554554,   //'LUET'
        kRunTimeInited  = 0x52554554,   //'RUET'
    };

    DWORD                   m_state;        // the initialization state
#endif

public:
    static UMEntryThunkData* CreateUMEntryThunk();
    static VOID FreeUMEntryThunk(UMEntryThunkData* p);

#ifndef DACCESS_COMPILE
    VOID LoadTimeInit(PCODE                   pManagedTarget,
                      OBJECTHANDLE            pObjectHandle,
                      UMThunkMarshInfo       *pUMThunkMarshInfo,
                      MethodDesc             *pMD)
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
            PRECONDITION(CheckPointer(pUMThunkMarshInfo));
            PRECONDITION(pMD != NULL);
        }
        CONTRACTL_END;

        m_pManagedTarget = pManagedTarget;
        m_pObjectHandle     = pObjectHandle;
        m_pUMThunkMarshInfo = pUMThunkMarshInfo;
        m_pInterpretedTarget = (PCODE)0;

        m_pMD = pMD;

        m_pUMEntryThunk->SetTargetUnconditional(TheUMThunkPreStub());

#ifdef _DEBUG
        m_state = kLoadTimeInited;
#endif

        FlushCacheForDynamicMappedStub(m_pUMEntryThunk, sizeof(UMEntryThunk));
    }

    void Terminate();

#ifdef FEATURE_INTERPRETER
    PCODE GetInterpreterTarget()
    {
        STANDARD_VM_CONTRACT;
        return m_pInterpretedTarget;
    }
#endif

    void RunTimeInit()
    {
        STANDARD_VM_CONTRACT;

        // Ensure method's module is activate in app domain
        m_pMD->EnsureActive();

        m_pUMThunkMarshInfo->RunTimeInit();

        // Ensure that we have either the managed target or the delegate.
        if (m_pObjectHandle == NULL && m_pManagedTarget == (TADDR)0)
            m_pManagedTarget = m_pMD->GetMultiCallableAddrOfCode();

        PCODE entryPoint = m_pUMThunkMarshInfo->GetExecStubEntryPoint();

#ifdef FEATURE_INTERPRETER
        // For interpreted stubs we need to ensure that TheUMEntryPrestubWorker runs for every
        // unmanaged-to-managed invocation in order to populate the TLS variable every time.
        auto stubKind = RangeSectionStubManager::GetStubKind(entryPoint);
        if (stubKind == STUB_CODE_BLOCK_STUBPRECODE)
        {
            StubPrecode* pPrecode = Precode::GetPrecodeFromEntryPoint(entryPoint)->AsStubPrecode();
            if (pPrecode->GetType() == PRECODE_INTERPRETER)
            {
                m_pInterpretedTarget = entryPoint;
                entryPoint = NULL;
            }
        }

        if (entryPoint != NULL)
#endif // FEATURE_INTERPRETER
        {
            m_pUMEntryThunk->SetTargetUnconditional(entryPoint);
        }

#ifdef _DEBUG
        m_state = kRunTimeInited;
#endif // _DEBUG
    }

    PCODE GetManagedTarget() const
    {
        CONTRACT (PCODE)
        {
            THROWS;
            GC_TRIGGERS;
            MODE_ANY;
            PRECONDITION(m_state == kRunTimeInited || m_state == kLoadTimeInited);
            POSTCONDITION(RETVAL != NULL);
        }
        CONTRACT_END;

        OBJECTHANDLE hndDelegate = GetObjectHandle();
        if (hndDelegate != NULL)
        {
            GCX_COOP();

            DELEGATEREF orDelegate = (DELEGATEREF)ObjectFromHandle(hndDelegate);
            _ASSERTE(orDelegate != NULL);
            _ASSERTE(m_pMD->IsEEImpl());

            // We have optimizations that skip the Invoke method and call directly the
            // delegate's target method. We need to return the target in that case,
            // otherwise debugger would fail to step in.
            RETURN orDelegate->GetMethodPtr();
        }
        else
        {
            if (m_pManagedTarget != (TADDR)0)
            {
                RETURN m_pManagedTarget;
            }
            else
            {
                RETURN m_pMD->GetMultiCallableAddrOfCode();
            }
        }
    }
#endif // !DACCESS_COMPILE

    OBJECTHANDLE GetObjectHandle() const
    {
        CONTRACT (OBJECTHANDLE)
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
            // If we OOM after we create the holder but
            // before we set the m_state we can have
            // m_state == 0 and m_pObjectHandle == NULL
            PRECONDITION(m_state == kRunTimeInited  ||
                         m_state == kLoadTimeInited ||
                         m_pObjectHandle == NULL);
        }
        CONTRACT_END;

        RETURN m_pObjectHandle;
    }

    bool IsCollectedDelegate() const
    {
        LIMITED_METHOD_CONTRACT;

        return m_pObjectHandle == (OBJECTHANDLE)-1;
    }

    UMThunkMarshInfo* GetUMThunkMarshInfo() const
    {
        CONTRACT (UMThunkMarshInfo*)
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
            SUPPORTS_DAC;
            PRECONDITION(m_state == kRunTimeInited || m_state == kLoadTimeInited);
            POSTCONDITION(CheckPointer(RETVAL));
        }
        CONTRACT_END;

        RETURN m_pUMThunkMarshInfo;
    }

    PCODE GetCode() const
    {
        CONTRACT (PCODE)
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
            PRECONDITION(m_state == kRunTimeInited || m_state == kLoadTimeInited);
            POSTCONDITION(CheckPointer(dac_cast<BYTE*>(RETVAL), NULL_OK));
        }
        CONTRACT_END;

        RETURN PINSTRToPCODE(dac_cast<TADDR>(m_pUMEntryThunk));
    }

    MethodDesc* GetMethod() const
    {
        CONTRACT (MethodDesc*)
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
            PRECONDITION(m_state == kRunTimeInited || m_state == kLoadTimeInited);
            POSTCONDITION(CheckPointer(RETVAL,NULL_OK));
        }
        CONTRACT_END;

        RETURN m_pMD;
    }
};

// Cache to hold UMEntryThunk/UMThunkMarshInfo instances associated with MethodDescs.
// All UMEntryThunk/UMThunkMarshInfo instances are destroyed when the cache goes away.
class UMEntryThunkCache
{
public:
    UMEntryThunkCache(AppDomain *pDomain);
    ~UMEntryThunkCache();

    UMEntryThunkData *GetUMEntryThunk(MethodDesc *pMD);

private:
    class ThunkSHashTraits : public NoRemoveSHashTraits< DefaultSHashTraits<UMEntryThunkData *> >
    {
    public:
        typedef MethodDesc *key_t;
        static key_t GetKey(element_t e)       { LIMITED_METHOD_CONTRACT; return e->GetMethod(); }
        static BOOL Equals(key_t k1, key_t k2) { LIMITED_METHOD_CONTRACT; return (k1 == k2); }
        static count_t Hash(key_t k)           { LIMITED_METHOD_CONTRACT; return (count_t)(size_t)k; }
    };

    static void DestroyMarshInfo(UMThunkMarshInfo *pMarshInfo)
    {
        WRAPPER_NO_CONTRACT;
        pMarshInfo->~UMThunkMarshInfo();
    }

    SHash<ThunkSHashTraits> m_hash;
    Crst       m_crst;
    AppDomain *m_pDomain;
};

#ifndef FEATURE_EH_FUNCLETS
EXCEPTION_HANDLER_DECL(FastNExportExceptHandler);
#endif // FEATURE_EH_FUNCLETS

extern "C" void TheUMEntryPrestub(void);
extern "C" PCODE TheUMEntryPrestubWorker(UMEntryThunkData * pUMEntryThunk);

#endif //__dllimportcallback_h__
