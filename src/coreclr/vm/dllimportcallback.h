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

struct UMEntryThunkData
{
    friend class NDirectStubLinker;

    // The start of the managed code.
    // if m_pObjectHandle is non-NULL, this field is still set to help with diagnostic of call on collected delegate crashes
    // but it may not have the correct value.
    PCODE                   m_pManagedTarget;

    // This is used for profiling.
    PTR_MethodDesc          m_pMD;

    // Object handle holding "this" reference. May be a strong or weak handle.
    // Field is NULL for a static method.
    OBJECTHANDLE            m_pObjectHandle;

    union
    {
        // Pointer to the shared structure containing everything else
        PTR_UMThunkMarshInfo    m_pUMThunkMarshInfo;
        // Pointer to the next UMEntryThunk in the free list. Used when it is freed.
        UMEntryThunk *m_pNextFreeThunk;
    };

    PTR_UMEntryThunk m_pUMEntryThunk;

#ifdef _DEBUG
    DWORD                   m_state;        // the initialization state
#endif
};

typedef DPTR(struct UMEntryThunkData) PTR_UMEntryThunkData;

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
    friend class UMEntryThunkFreeList;

private:
#ifdef _DEBUG
    enum
    {
        kLoadTimeInited = 0x4c554554,   //'LUET'
        kRunTimeInited  = 0x52554554,   //'RUET'
    };
#endif

    PTR_UMEntryThunkData GetData() const
    {
        LIMITED_METHOD_CONTRACT;
        
        return dac_cast<PTR_UMEntryThunkData>(GetSecretParam());
    }

public:

    static const int Type = PRECODE_UMENTRY_THUNK;

    static UMEntryThunk* CreateUMEntryThunk();
    static VOID FreeUMEntryThunk(UMEntryThunk* p);

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

        PTR_UMEntryThunkData data = GetData();
        data->m_pManagedTarget = pManagedTarget;
        data->m_pObjectHandle     = pObjectHandle;
        data->m_pUMThunkMarshInfo = pUMThunkMarshInfo;

        data->m_pMD = pMD;    // For debugging and profiling, so they can identify the target

        SetTargetUnconditional(TheUMThunkPreStub());

#ifdef _DEBUG
        data->m_state = kLoadTimeInited;
#endif
    }

    void Terminate();

    VOID RunTimeInit()
    {
        STANDARD_VM_CONTRACT;

        PTR_UMEntryThunkData data = GetData();
        // Ensure method's module is activate in app domain
        data->m_pMD->EnsureActive();

        data->m_pUMThunkMarshInfo->RunTimeInit();

        // Ensure that we have either the managed target or the delegate.
        if (data->m_pObjectHandle == NULL && data->m_pManagedTarget == (TADDR)0)
            data->m_pManagedTarget = data->m_pMD->GetMultiCallableAddrOfCode();

        SetTargetUnconditional(data->m_pUMThunkMarshInfo->GetExecStubEntryPoint());

#ifdef _DEBUG
        data->m_state = kRunTimeInited;
#endif // _DEBUG
    }

    PCODE GetManagedTarget() const
    {
        CONTRACT (PCODE)
        {
            THROWS;
            GC_TRIGGERS;
            MODE_ANY;
            PRECONDITION(GetData()->m_state == kRunTimeInited || GetData()->m_state == kLoadTimeInited);
            POSTCONDITION(RETVAL != NULL);
        }
        CONTRACT_END;

        OBJECTHANDLE hndDelegate = GetObjectHandle();
        if (hndDelegate != NULL)
        {
            GCX_COOP();

            DELEGATEREF orDelegate = (DELEGATEREF)ObjectFromHandle(hndDelegate);
            _ASSERTE(orDelegate != NULL);
            _ASSERTE(GetData()->m_pMD->IsEEImpl());

            // We have optimizations that skip the Invoke method and call directly the
            // delegate's target method. We need to return the target in that case,
            // otherwise debugger would fail to step in.
            RETURN orDelegate->GetMethodPtr();
        }
        else
        {
            PTR_UMEntryThunkData data = GetData();
            if (data->m_pManagedTarget != (TADDR)0)
            {
                RETURN data->m_pManagedTarget;
            }
            else
            {
                RETURN data->m_pMD->GetMultiCallableAddrOfCode();
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
            PRECONDITION(GetData()->m_state == kRunTimeInited  ||
                         GetData()->m_state == kLoadTimeInited ||
                         GetData()->m_pObjectHandle == NULL);
        }
        CONTRACT_END;

        RETURN GetData()->m_pObjectHandle;
    }

    UMThunkMarshInfo* GetUMThunkMarshInfo() const
    {
        CONTRACT (UMThunkMarshInfo*)
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
            SUPPORTS_DAC;
            PRECONDITION(GetData()->m_state == kRunTimeInited || GetData()->m_state == kLoadTimeInited);
            POSTCONDITION(CheckPointer(RETVAL));
        }
        CONTRACT_END;

        RETURN GetData()->m_pUMThunkMarshInfo;
    }


    PCODE GetCode() const
    {
        CONTRACT (PCODE)
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
            PRECONDITION(GetData()->m_state == kRunTimeInited || GetData()->m_state == kLoadTimeInited);
            POSTCONDITION(CheckPointer(dac_cast<BYTE*>(RETVAL), NULL_OK));
        }
        CONTRACT_END;

        RETURN PINSTRToPCODE(dac_cast<TADDR>(this));
    }

    MethodDesc* GetMethod() const
    {
        CONTRACT (MethodDesc*)
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
            PRECONDITION(GetData()->m_state == kRunTimeInited || GetData()->m_state == kLoadTimeInited);
            POSTCONDITION(CheckPointer(RETVAL,NULL_OK));
        }
        CONTRACT_END;

        RETURN GetData()->m_pMD;
    }

    static VOID __fastcall ReportViolation(UMEntryThunkData* pEntryThunkData);
};

// Cache to hold UMEntryThunk/UMThunkMarshInfo instances associated with MethodDescs.
// All UMEntryThunk/UMThunkMarshInfo instances are destroyed when the cache goes away.
class UMEntryThunkCache
{
public:
    UMEntryThunkCache(AppDomain *pDomain);
    ~UMEntryThunkCache();

    UMEntryThunk *GetUMEntryThunk(MethodDesc *pMD);

private:
    struct CacheElement
    {
        CacheElement()
        {
            LIMITED_METHOD_CONTRACT;
            m_pMD = NULL;
            m_pThunk = NULL;
        }

        MethodDesc   *m_pMD;
        UMEntryThunk *m_pThunk;
    };

    class ThunkSHashTraits : public NoRemoveSHashTraits< DefaultSHashTraits<CacheElement> >
    {
    public:
        typedef MethodDesc *key_t;
        static key_t GetKey(element_t e)       { LIMITED_METHOD_CONTRACT; return e.m_pMD; }
        static BOOL Equals(key_t k1, key_t k2) { LIMITED_METHOD_CONTRACT; return (k1 == k2); }
        static count_t Hash(key_t k)           { LIMITED_METHOD_CONTRACT; return (count_t)(size_t)k; }
        static const element_t Null()          { LIMITED_METHOD_CONTRACT; return CacheElement(); }
        static bool IsNull(const element_t &e) { LIMITED_METHOD_CONTRACT; return (e.m_pMD == NULL); }
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

#ifdef _DEBUG
void STDCALL LogUMTransition(UMEntryThunk* thunk);
#endif

#endif //__dllimportcallback_h__
