// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
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

enum UMThunkStubFlags
{
    umtmlIsStatic           = 0x0001,
    umtmlThisCall           = 0x0002,
    umtmlThisCallHiddenArg  = 0x0004,
    umtmlFpu                = 0x0008,
#ifdef _TARGET_X86_
    // the signature is trivial so stub need not be generated and the target can be called directly
    umtmlSkipStub           = 0x0080,
#endif // _TARGET_X86_
};

#include <pshpack1.h>
//--------------------------------------------------------------------------
// This structure captures basic info needed to build an UMThunk.
//--------------------------------------------------------------------------
struct UMThunkStubInfo
{
    UINT32        m_cbDstStack;         //# of bytes of stack portion of managed args
    UINT16        m_cbSrcStack;         //# of bytes of stack portion of unmanaged args
    UINT16        m_cbRetPop;           //# of bytes to pop on return to unmanaged
    UINT16        m_wFlags;             // UMThunkStubFlags enum
};
#include <poppack.h>

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
    // It does all the ML stub creation, and can throw a COM+
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

#if defined(_TARGET_X86_) && !defined(FEATURE_STUBS_AS_IL)
    PCODE GetExecStubEntryPoint()
    {
        WRAPPER_NO_CONTRACT;
        return GetExecStub()->GetEntryPoint();
    }

    Stub* GetExecStub()
    {
        CONTRACT (Stub*)
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
            PRECONDITION(IsCompletelyInited());
            POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
        }
        CONTRACT_END;

        RETURN m_pExecStub;
    }

    UINT16 GetCbRetPop()
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
            SUPPORTS_DAC;
            PRECONDITION(IsCompletelyInited());
        }
        CONTRACTL_END;

        return m_cbRetPop;
    }

    CorPinvokeMap GetCallingConvention()
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
            SUPPORTS_DAC;
            PRECONDITION(IsCompletelyInited());
        }
        CONTRACTL_END;

        return (CorPinvokeMap)m_callConv;
    }

    VOID SetCallingConvention(const CorPinvokeMap callConv)
    {
        m_callConv = (UINT16)callConv;
    }

#else
    PCODE GetExecStubEntryPoint();
#endif

    UINT32 GetCbActualArgSize()
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
            PRECONDITION(IsCompletelyInited());
        }
        CONTRACTL_END;

        return m_cbActualArgSize;
    }

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

#if defined(_TARGET_X86_) && !defined(FEATURE_STUBS_AS_IL)
    // Compiles an unmanaged to managed thunk for the given signature. The thunk
    // will call the stub or, if fNoStub == TRUE, directly the managed target.
    Stub *CompileNExportThunk(LoaderHeap *pLoaderHeap, PInvokeStaticSigInfo* pSigInfo, MetaSig *pMetaSig, BOOL fNoStub);
#endif // _TARGET_X86_ && !FEATURE_STUBS_AS_IL

#if defined(_TARGET_X86_) && defined(FEATURE_STUBS_AS_IL)
    struct ArgumentRegisters
    {
        UINT32 Ecx;
        UINT32 Edx;
    };

    VOID SetupArguments(char *pSrc, ArgumentRegisters *pArgRegs, char *pDst);
#endif // _TARGET_X86_ && FEATURE_STUBS_AS_IL

private:
    PCODE             m_pILStub;            // IL stub for marshaling 
                                            // On x86, NULL for no-marshal signatures
                                            // On non-x86, the managed entrypoint for no-delegate no-marshal signatures
    UINT32            m_cbActualArgSize;    // caches m_pSig.SizeOfFrameArgumentArray()
                                            // On x86/Linux we have to augment with numRegistersUsed * STACK_ELEM_SIZE
#if defined(_TARGET_X86_)
    UINT16            m_cbRetPop;           // stack bytes popped by callee (for UpdateRegDisplay)
#if defined(FEATURE_STUBS_AS_IL)
    UINT32            m_cbStackArgSize;     // stack bytes pushed for managed code
#else
    Stub*             m_pExecStub;          // UMEntryThunk jumps directly here
    UINT16            m_callConv;           // unmanaged calling convention and flags (CorPinvokeMap)
#endif // FEATURE_STUBS_AS_IL
#endif // _TARGET_X86_

    MethodDesc *      m_pMD;                // maybe null
    Module *          m_pModule;
    Signature         m_sig;
};


//----------------------------------------------------------------------
// This structure contains the minimal information required to
// distinguish one function pointer from another, with the rest
// being stored in a shared UMThunkMarshInfo.
//
// This structure also contains the actual code bytes that form the
// front end of the thunk. A pointer to the m_code[] byte array is
// what is actually handed to unmanaged client code.
//----------------------------------------------------------------------
class UMEntryThunk
{
    friend class CheckAsmOffsets;
    friend class NDirectStubLinker;
    friend class UMEntryThunkFreeList;

private:
#ifdef _DEBUG
    enum
    {
        kLoadTimeInited = 0x4c554554,   //'LUET'
        kRunTimeInited  = 0x52554554,   //'RUET'
    };
#endif

public:
    static UMEntryThunk* CreateUMEntryThunk();
    static VOID FreeUMEntryThunk(UMEntryThunk* p);

#if defined(_TARGET_X86_) && !defined(FEATURE_STUBS_AS_IL)
    // Compiles an unmanaged to managed thunk with the given calling convention adaptation.
    // - psrcofsregs are stack offsets that should be loaded to argument registers (ECX, EDX)
    // - psrcofs are stack offsets that should be repushed for the managed target
    // - retbufofs is the offset of the hidden byref structure argument when returning large
    //   structures; -1 means there is none
    // Special values recognized by psrcofsregs and psrcofs are -1 which means not present
    // and 1 which means that this register/stack slot should get the UMEntryThunk pointer.
    // This method is used for all reverse P/Invoke calls on x86 (the umtmlSkipStub
    // flag determines whether the managed target is stub or the actual target method).
    static VOID CompileUMThunkWorker(UMThunkStubInfo *pInfo,
                                     CPUSTUBLINKER *pcpusl,
                                     UINT *psrcofsregs,
                                     UINT *psrcofs,
                                     UINT retbufofs);
#endif // _TARGET_X86_ && !FEATURE_STUBS_AS_IL

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

        m_pMD = pMD;    // For debugging and profiling, so they can identify the target

        m_code.Encode((BYTE*)TheUMThunkPreStub(), this);

#ifdef _DEBUG
        m_state = kLoadTimeInited;
#endif
    }

    ~UMEntryThunk()
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
        }
        CONTRACTL_END;

        if (GetObjectHandle())
        {
            DestroyLongWeakHandle(GetObjectHandle());
        }
    }

    void Terminate();

    VOID RunTimeInit()
    {
        STANDARD_VM_CONTRACT;

        // Ensure method's module is activate in app domain
        m_pMD->EnsureActive();

        m_pUMThunkMarshInfo->RunTimeInit();

        // Ensure that we have either the managed target or the delegate.
        if (m_pObjectHandle == NULL && m_pManagedTarget == NULL)
            m_pManagedTarget = m_pMD->GetMultiCallableAddrOfCode();

        m_code.Encode((BYTE*)m_pUMThunkMarshInfo->GetExecStubEntryPoint(), this);

#ifdef _DEBUG
        m_state = kRunTimeInited;
#endif
    }

    // asm entrypoint
    static VOID STDCALL DoRunTimeInit(UMEntryThunk* pThis);

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
            if (m_pManagedTarget != NULL)
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
    

    const BYTE* GetCode() const
    {
        CONTRACT (const BYTE*)
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
            PRECONDITION(m_state == kRunTimeInited || m_state == kLoadTimeInited);
            POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
        }
        CONTRACT_END;

        RETURN m_code.GetEntryPoint();
    }

    static UMEntryThunk* RecoverUMEntryThunk(const VOID* pCode)
    {
        LIMITED_METHOD_CONTRACT;
        return (UMEntryThunk*)( ((LPBYTE)pCode) - offsetof(UMEntryThunk, m_code) );
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

    static DWORD GetOffsetOfMethodDesc()
    {
        LIMITED_METHOD_CONTRACT;
        return offsetof(class UMEntryThunk, m_pMD);
    }

    static DWORD GetCodeOffset()
    {
        LIMITED_METHOD_CONTRACT;
        return offsetof(UMEntryThunk, m_code);
    }

    static UMEntryThunk* Decode(LPVOID pCallback);

    static VOID __fastcall ReportViolation(UMEntryThunk* p);

private:
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

#ifdef _DEBUG
    DWORD                   m_state;        // the initialization state
#endif

    UMEntryThunkCode        m_code;
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

#if defined(_TARGET_X86_) && !defined(FEATURE_STUBS_AS_IL)
//-------------------------------------------------------------------------
// One-time creation of special prestub to initialize UMEntryThunks.
//-------------------------------------------------------------------------
Stub *GenerateUMThunkPrestub();
#endif // _TARGET_X86_ && !FEATURE_STUBS_AS_IL

//-------------------------------------------------------------------------
// NExport stub
//-------------------------------------------------------------------------
#if  !defined(_WIN64) && !defined(DACCESS_COMPILE) && !defined(CROSS_COMPILE)
EXCEPTION_HANDLER_DECL(FastNExportExceptHandler);
EXCEPTION_HANDLER_DECL(UMThunkPrestubHandler);
#endif // _WIN64

extern "C" void TheUMEntryPrestub(void);
extern "C" PCODE TheUMEntryPrestubWorker(UMEntryThunk * pUMEntryThunk);

EXTERN_C void UMThunkStub(void);

#ifdef _DEBUG
void STDCALL LogUMTransition(UMEntryThunk* thunk);
#endif

#endif //__dllimportcallback_h__
