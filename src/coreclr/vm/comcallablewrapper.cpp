// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//--------------------------------------------------------------------------
// ComCallablewrapper.cpp
//
// Implementation for various Wrapper classes
//
//  COMWrapper      : COM callable wrappers for CLR interfaces
//

//--------------------------------------------------------------------------


#include "common.h"
#include "clrtypes.h"

#include "comcallablewrapper.h"

#include "object.h"
#include "field.h"
#include "method.hpp"
#include "class.h"
#include "runtimecallablewrapper.h"
#include "olevariant.h"
#include "cachelinealloc.h"
#include "threads.h"
#include "ceemain.h"
#include "excep.h"
#include "stublink.h"
#include "cgensys.h"
#include "comtoclrcall.h"
#include "clrtocomcall.h"
#include "comutilnative.h"
#include "eeconfig.h"
#include "interoputil.h"
#include "dispex.h"
#include "guidfromname.h"
#include "comconnectionpoints.h"
#include <objsafe.h>    // IID_IObjctSafe
#include "virtualcallstub.h"
#include "contractimpl.h"
#include "caparser.h"
#include "appdomain.inl"
#include "typestring.h"

// The enum that describes the value of the IDispatchImplAttribute custom attribute.
enum IDispatchImplType
{
    SystemDefinedImpl   = 0,
    InternalImpl        = 1,
    CompatibleImpl      = 2
};

// The enum that describe the value of System.Runtime.InteropServices.CustomQueryInterfaceResult
// It is the return value of the method System.Runtime.InteropServices.ICustomQueryInterface.GetInterface
enum CustomQueryInterfaceResult
{
    Handled          = 0,
    NotHandled       = 1,
    Failed           = 2
};

typedef CQuickArray<MethodTable*> CQuickEEClassPtrs;

// Startup and shutdown lock
CrstStatic g_CreateWrapperTemplateCrst;


// This is the prestub that is used for Com calls entering COM+
extern "C" VOID ComCallPreStub();

class NewCCWHolderBase : public HolderBase<ComCallWrapper *>
{

protected:
    NewCCWHolderBase(ComCallWrapper *pValue)
        : HolderBase<ComCallWrapper *>(pValue)
    {
    }

    // BaseHolder only initialize BASE with one parameter, so I had to
    // use a separate function to set the cache which will be used in the release
    void SetCache(ComCallWrapperCache *pCache)
    {
        m_pCache = pCache;
    }

    void DoAcquire()
    {
        // Do nothing
    }

    void DoRelease()
    {
        this->m_value->FreeWrapper(m_pCache);
    }


private :
    ComCallWrapperCache *m_pCache;
};

typedef ComCallWrapper *ComCallWrapperPtr;

// This is used to hold a newly created CCW. It will release the CCW (and linked wrappers)
// upon exit, if SuppressRelease() is not called. It doesn't try to release the SimpleComCallWrapper
// or destroy the handle
// I need to use BaseHolder instead of BaseWrapper because BaseHolder allows me to use a class as BASE
//
class NewCCWHolder : public BaseHolder<ComCallWrapperPtr, NewCCWHolderBase>
{
public :
    NewCCWHolder(ComCallWrapperCache *pCache)
    {
        SetCache(pCache);
    }

    ComCallWrapperPtr& operator=(ComCallWrapperPtr p)
    {
        Assign(p);
        return m_value;
    }

    FORCEINLINE const ComCallWrapperPtr &operator->()
    {
        return this->m_value;
    }

    operator ComCallWrapperPtr()
    {
        return m_value;
    }
};

// Calls Destruct on ComCallMethodDesc's in an array - used as backout code when laying out ComMethodTable.
void DestructComCallMethodDescs(ArrayList *pDescArray)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
    }
    CONTRACTL_END;

    ArrayList::Iterator i = pDescArray->Iterate();
    while (i.Next())
    {
        ComCallMethodDesc *pCMD = (ComCallMethodDesc *)i.GetElement();
        pCMD->Destruct();
    }
}

typedef Wrapper<ArrayList *, DoNothing<ArrayList *>, DestructComCallMethodDescs> ComCallMethodDescArrayHolder;

// Forward declarations
static bool GetComIPFromCCW_HandleCustomQI(ComCallWrapper *pWrap, REFIID riid, MethodTable * pIntfMT, IUnknown **ppUnkOut);

//--------------------------------------------------------------------------
//  IsDuplicateClassItfMD(MethodDesc *pMD, unsigned int ix)
//  Determines if the specified method desc is a duplicate.
//  Note that this method should only be called to determine duplicates on
//  the class interface.
//--------------------------------------------------------------------------
bool IsDuplicateClassItfMD(MethodDesc *pMD, unsigned int ix)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(CheckPointer(pMD));
    }
    CONTRACTL_END;

    if (!pMD->IsDuplicate())
        return false;
    if (pMD->GetSlot() == ix)
        return false;

    return true;
}

//--------------------------------------------------------------------------
//  IsDuplicateClassItfMD(MethodDesc *pMD, unsigned int ix)
//  Determines if the specified method desc is a duplicate.
//  Note that this method should only be called to determine duplicates on
//  the class interface.
//--------------------------------------------------------------------------
bool IsDuplicateClassItfMD(InteropMethodTableSlotData *pInteropMD, unsigned int ix)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(CheckPointer(pInteropMD));
    }
    CONTRACTL_END;

    if (!pInteropMD->IsDuplicate())
        return false;
    if (pInteropMD->GetSlot() == ix)
        return false;

    return true;
}

bool IsOverloadedComVisibleMember(MethodDesc *pMD, MethodDesc *pParentMD)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(CheckPointer(pMD));
        PRECONDITION(CheckPointer(pParentMD));
    }
    CONTRACTL_END;

    // Array methods should never be exposed to COM.
    if (pMD->IsArray())
        return FALSE;

    // If this is the same MethodDesc, then it is not an overload at all
    if (pMD == pParentMD)
        return FALSE;

    // If the new member is not visible from COM then it isn't an overloaded public member.
    if (!IsMethodVisibleFromCom(pMD))
        return FALSE;

    // If the old member is visible from COM then the new one is not a public overload.
    if (IsMethodVisibleFromCom(pParentMD))
        return FALSE;

    // The new member is a COM visible overload of a non COM visible member.
    return TRUE;
}

bool IsNewComVisibleMember(MethodDesc *pMD)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(CheckPointer(pMD));
    }
    CONTRACTL_END;

    // Array methods should never be exposed to COM.
    if (pMD->IsArray())
        return FALSE;

    // Check to see if the member is visible from COM.
    return IsMethodVisibleFromCom(pMD) ? true : false;
}

bool IsStrictlyUnboxed(MethodDesc *pMD)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pMD));
    }
    CONTRACTL_END;

    MethodTable *pMT = pMD->GetMethodTable();

    MethodTable::MethodIterator it(pMT);
    for (; it.IsValid(); it.Next()) {
        MethodDesc *pCurrMD = it.GetMethodDesc();
        if (pCurrMD->GetMemberDef() == pMD->GetMemberDef())
            return false;
    }

    return true;
}

void FillInComVtableSlot(SLOT* pComVtable,          // must point to the first slot after the "extra slots" (e.g. IUnknown/IDispatch slots)
                         UINT  uComSlot,            // must be relative to pComVtable
                         ComCallMethodDesc* pMD)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(CheckPointer(pComVtable));
        PRECONDITION(CheckPointer(pMD));
    }
    CONTRACTL_END;

    pComVtable[uComSlot] = (SLOT)(((BYTE*)pMD - COMMETHOD_CALL_PRESTUB_SIZE)ARM_ONLY(+THUMB_CODE));
}



ComCallMethodDesc* ComMethodTable::ComCallMethodDescFromSlot(unsigned i)
{
    CONTRACT(ComCallMethodDesc*)
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACT_END;

    ComCallMethodDesc* pCMD = NULL;

    SLOT* rgVtable = (SLOT*)((ComMethodTable *)this+1);

// NOTE: make sure to keep this in sync with FillInComVtableSlot
    pCMD = (ComCallMethodDesc*)(((BYTE *)rgVtable[i]) + COMMETHOD_CALL_PRESTUB_SIZE ARM_ONLY(-THUMB_CODE));

    RETURN pCMD;
}

//--------------------------------------------------------------------------
// Determines if the Compatible IDispatch implementation is required for
// the specified class.
//--------------------------------------------------------------------------
bool IsOleAutDispImplRequiredForClass(MethodTable *pClass)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(CheckPointer(pClass));
    }
    CONTRACTL_END;

    HRESULT             hr;
    const BYTE *        pVal;
    ULONG               cbVal;
    Assembly *          pAssembly = pClass->GetAssembly();
    IDispatchImplType   DispImplType = SystemDefinedImpl;

    // First check for the IDispatchImplType custom attribute first.
    hr = pClass->GetCustomAttribute(WellKnownAttribute::IDispatchImpl, (const void**)&pVal, &cbVal);
    if (hr == S_OK)
    {
        CustomAttributeParser cap(pVal, cbVal);
        IfFailThrow(cap.SkipProlog());
        UINT8 u1;
        IfFailThrow(cap.GetU1(&u1));

        DispImplType = (IDispatchImplType)u1;
        if ((DispImplType > 2) || (DispImplType < 0))
            DispImplType = SystemDefinedImpl;
    }

    // If the custom attribute was set to something other than system defined then we will use that.
    if (DispImplType != SystemDefinedImpl)
        return (bool) (DispImplType == CompatibleImpl);

    // Check to see if the assembly has the IDispatchImplType attribute set.
    hr = pAssembly->GetCustomAttribute(pAssembly->GetManifestToken(), WellKnownAttribute::IDispatchImpl, (const void**)&pVal, &cbVal);
    if (hr == S_OK)
    {
        CustomAttributeParser cap(pVal, cbVal);
        IfFailThrow(cap.SkipProlog());
        UINT8 u1;
        IfFailThrow(cap.GetU1(&u1));

        DispImplType = (IDispatchImplType)u1;
        if ((DispImplType > 2) || (DispImplType < 0))
            DispImplType = SystemDefinedImpl;
    }

    // If the custom attribute was set to something other than system defined then we will use that.
    if (DispImplType != SystemDefinedImpl)
        return (bool) (DispImplType == CompatibleImpl);

    // Removed registry key check per reg cleanup bug 45978
    // Effect: Will return false so code cleanup
    return false;
}

//--------------------------------------------------------------------------
// This routine is called anytime a com method is invoked for the first time.
// It is responsible for generating the real stub.
//
// This function's only caller is the ComPreStub.
//
// For the duration of the prestub, the current Frame on the stack
// will be a PrestubMethodFrame (which derives from FramedMethodFrame.)
// Hence, things such as exceptions and gc will work normally.
//
// On rare occasions, the ComPrestub may get called twice because two
// threads try to call the same method simultaneously.
//--------------------------------------------------------------------------
extern "C" PCODE ComPreStubWorker(ComPrestubMethodFrame *pPFrame, UINT64 *pErrorReturn)
{
    CONTRACT (PCODE)
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        ENTRY_POINT;
        PRECONDITION(CheckPointer(pPFrame));
        PRECONDITION(CheckPointer(pErrorReturn));
    }
    CONTRACT_END;

    HRESULT hr = S_OK;
    PCODE retAddr = NULL;

    BEGIN_ENTRYPOINT_VOIDRET;

    PCODE pStub = NULL;
    BOOL fNonTransientExceptionThrown = FALSE;

    ComCallMethodDesc *pCMD = pPFrame->GetComCallMethodDesc();
    IUnknown          *pUnk = *(IUnknown **)pPFrame->GetPointerToArguments();

    OBJECTREF          pThrowable = NULL;

    Thread* pThread = SetupThreadNoThrow();
    if (pThread == NULL)
    {
        hr = E_OUTOFMEMORY;
    }
    else
    {
        // Transition to cooperative GC mode before we start setting up the stub.
        GCX_COOP();

        // The PreStub allocates memory for the frame, but doesn't link it
        // into the chain or fully initialize it. Do so now.
        pPFrame->Init();
        pPFrame->Push();

        ComCallWrapper    *pWrap =  NULL;

        GCPROTECT_BEGIN(pThrowable)
        {
            // We need a try/catch around the code to enter the domain since entering
            // an AppDomain can throw an exception.
            EX_TRY
            {
                // check for invalid wrappers in the debug build
                // in the retail all bets are off
                pWrap = ComCallWrapper::GetWrapperFromIP(pUnk);
                _ASSERTE(pWrap->IsWrapperActive() || pWrap->IsAggregated());

                // Make sure we're not trying to call on the class interface of a class with ComVisible(false) members
                //  in its hierarchy.
                if ((pCMD->IsFieldCall()) || (NULL == pCMD->GetInterfaceMethodDesc() && !pCMD->GetMethodDesc()->IsInterface()))
                {
                    // If we have a fieldcall or a null interface MD, we could be dealing with the IClassX interface.
                    ComMethodTable* pComMT = ComMethodTable::ComMethodTableFromIP(pUnk);
                    pComMT->CheckParentComVisibility(FALSE);
                }

                {
                    OBJECTREF pADThrowable = NULL;

                    BOOL fExceptionThrown = FALSE;

                    GCPROTECT_BEGIN(pADThrowable);
                    {
                        if (pCMD->IsMethodCall())
                        {
                            // We need to ensure all valuetypes are loaded in
                            //  the target domain so that GC can happen later

                            EX_TRY
                            {
                                MethodDesc* pTargetMD = pCMD->GetMethodDesc();
                                MetaSig::EnsureSigValueTypesLoaded(pTargetMD);
                            }
                            EX_CATCH
                            {
                                pADThrowable = GET_THROWABLE();
                            }
                            EX_END_CATCH(RethrowTerminalExceptions);
                        }

                        if (pADThrowable != NULL)
                        {
                            // Transform the exception into an HRESULT. This also sets up
                            // an IErrorInfo on the current thread for the exception.
                            hr = SetupErrorInfo(pADThrowable);
                            pADThrowable = NULL;
                            fExceptionThrown = TRUE;
                        }
                    }
                    GCPROTECT_END();

                    if(!fExceptionThrown)
                    {
                        GCPROTECT_BEGIN(pADThrowable);
                        {
                            // We need a try/catch around the call to the worker since we need
                            // to transform any exceptions into HRESULTs. We want to do this
                            // inside the AppDomain of the CCW.
                            EX_TRY
                            {
                                GCX_PREEMP();
                                pStub = ComCall::GetComCallMethodStub(pCMD);
                            }
                            EX_CATCH
                            {
                                fNonTransientExceptionThrown = !GET_EXCEPTION()->IsTransient();
                                pADThrowable = GET_THROWABLE();
                            }
                            EX_END_CATCH(RethrowTerminalExceptions);

                            if (pADThrowable != NULL)
                            {
                                // Transform the exception into an HRESULT. This also sets up
                                // an IErrorInfo on the current thread for the exception.
                                hr = SetupErrorInfo(pADThrowable);
                                pADThrowable = NULL;
                            }
                        }
                        GCPROTECT_END();
                    }
                }
            }
            EX_CATCH
            {
                pThrowable = GET_THROWABLE();

                // If an exception was thrown while transitionning back to the original
                // AppDomain then can't use the stub and must report an error.
                pStub = NULL;
            }
            EX_END_CATCH(SwallowAllExceptions);

            if (pThrowable != NULL)
            {
                // Transform the exception into an HRESULT. This also sets up
                // an IErrorInfo on the current thread for the exception.
                hr = SetupErrorInfo(pThrowable);
                pThrowable = NULL;
            }
        }
        GCPROTECT_END();

        // Unlink the PrestubMethodFrame.
        pPFrame->Pop();

        if (pStub)
        {
            // Now, replace the prestub with the new stub.
            static_assert((COMMETHOD_CALL_PRESTUB_SIZE - COMMETHOD_CALL_PRESTUB_ADDRESS_OFFSET) % DATA_ALIGNMENT == 0,
                "The call target in COM prestub must be aligned so we can guarantee atomicity of updates");

            UINT_PTR* ppofs = (UINT_PTR*)  (((BYTE*)pCMD) - COMMETHOD_CALL_PRESTUB_SIZE + COMMETHOD_CALL_PRESTUB_ADDRESS_OFFSET);

            ExecutableWriterHolder<UINT_PTR> ppofsWriterHolder(ppofs, sizeof(UINT_PTR));
#ifdef TARGET_X86
            *ppofsWriterHolder.GetRW() = ((UINT_PTR)pStub - (size_t)pCMD);
#else
            *ppofsWriterHolder.GetRW() = ((UINT_PTR)pStub);
#endif

            // Return the address of the prepad. The prepad will regenerate the hidden parameter and due
            // to the update above will execute the new stub code the second time around.
            retAddr = (PCODE)(((BYTE*)pCMD - COMMETHOD_CALL_PRESTUB_SIZE)ARM_ONLY(+THUMB_CODE));

            goto Exit;
        }
    }

    // We failed to set up the stub so we need to report an error to the caller.
    //
    // IMPORTANT: No floating point operations can occur after this point!
    //
    *pErrorReturn = 0;
    if (pCMD->IsNativeHResultRetVal())
        *pErrorReturn = hr;
    else if (pCMD->IsNativeBoolRetVal())
        *pErrorReturn = 0;
    else if (pCMD->IsNativeR4RetVal())
        setFPReturn(4, CLR_NAN_32);
    else if (pCMD->IsNativeR8RetVal())
        setFPReturn(8, CLR_NAN_64);
    else
        _ASSERTE(pCMD->IsNativeVoidRetVal());

#ifdef TARGET_X86
    // Number of bytes to pop is upper half of the return value on x86
    *(((INT32 *)pErrorReturn) + 1) = pCMD->GetNumStackBytes();
#endif

    retAddr = NULL;

Exit:

    END_ENTRYPOINT_VOIDRET;

    RETURN retAddr;
}

FORCEINLINE void CPListRelease(CQuickArray<ConnectionPoint*>* value)
{
    WRAPPER_NO_CONTRACT;

    if (value)
    {
        // Delete all the connection points.
        for (UINT i = 0; i < value->Size(); i++)
            delete (*value)[i];

        // Delete the list itself.
        delete value;
    }
}

typedef CQuickArray<ConnectionPoint*> CPArray;

FORCEINLINE void CPListDoNothing(CPArray*)
{
    LIMITED_METHOD_CONTRACT;
}

class CPListHolder : public Wrapper<CPArray*, CPListDoNothing, CPListRelease, NULL>
{
public:
    CPListHolder(CPArray* p = NULL)
        : Wrapper<CPArray*, CPListDoNothing, CPListRelease, NULL>(p)
    {
        WRAPPER_NO_CONTRACT;
    }

    FORCEINLINE void operator=(CPArray* p)
    {
        WRAPPER_NO_CONTRACT;
        Wrapper<CPArray*, CPListDoNothing, CPListRelease, NULL>::operator=(p);
    }
};

NOINLINE void LogCCWRefCountChange_BREAKPOINT(ComCallWrapper *pCCW)
{
    LIMITED_METHOD_CONTRACT;
    // Empty function to put breakpoint on when debugging CCW ref-counting issues.
    // At this point *(pCCW->m_ppThis) is the managed object wrapped by the CCW.

    // Bogus code so the function is not optimized away.
    if (pCCW == NULL)
        DebugBreak();
}

void SimpleComCallWrapper::BuildRefCountLogMessage(LPCWSTR wszOperation, StackSString &ssMessage, ULONG dwEstimatedRefCount)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    // Don't worry about domain unloading in CoreCLR
    LPCUTF8 pszClassName;
    LPCUTF8 pszNamespace;
    if (SUCCEEDED(m_pMT->GetMDImport()->GetNameOfTypeDef(m_pMT->GetCl(), &pszClassName, &pszNamespace)))
    {
        OBJECTHANDLE handle = GetMainWrapper()->GetObjectHandle();
        _UNCHECKED_OBJECTREF obj = NULL;

        // Force retriving the handle without using OBJECTREF and under cooperative mode
        // We only need the value in ETW events and it doesn't matter if it is super accurate
        if (handle != NULL)
            obj = *((_UNCHECKED_OBJECTREF *)(handle));

        if (ETW_EVENT_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PRIVATE_PROVIDER_DOTNET_Context, CCWRefCountChange))
        {
            EX_TRY
            {
                SString className;
                className.SetUTF8(pszClassName);
                SString nameSpace;
                nameSpace.SetUTF8(pszNamespace);

                FireEtwCCWRefCountChange(
                    handle,
                    (Object *)obj,
                    this,
                    dwEstimatedRefCount,
                    NULL,                   // domain value is not interesting in CoreCLR
                    className.GetUnicode(), nameSpace.GetUnicode(), wszOperation, GetClrInstanceId());
            }
            EX_CATCH
            { }
            EX_END_CATCH(SwallowAllExceptions);
        }

        if (g_pConfig->ShouldLogCCWRefCountChange(pszClassName, pszNamespace))
        {
            EX_TRY
            {
                StackSString ssClassName;
                TypeString::AppendType(ssClassName, TypeHandle(m_pMT));

                ssMessage.Printf(W("LogCCWRefCountChange[%s]: '%s', Object=poi(%p)"),
                    wszOperation,                                          // %s operation
                    ssClassName.GetUnicode(),                              // %s type name
                    handle);               // %p Object
            }
            EX_CATCH
            { }
            EX_END_CATCH(SwallowAllExceptions);
        }
    }
}

// static
void SimpleComCallWrapper::LogRefCount(ComCallWrapper *pWrap, StackSString &ssMessage, ULONG dwRefCountToLog)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (!ssMessage.IsEmpty())
    {
        EX_TRY
        {
            ssMessage.AppendPrintf(W(", RefCount=%u\n"), dwRefCountToLog);
            WszOutputDebugString(ssMessage.GetUnicode());
        }
        EX_CATCH
        { }
        EX_END_CATCH(SwallowAllExceptions);

        LogCCWRefCountChange_BREAKPOINT(pWrap);
    }
}

LONGLONG SimpleComCallWrapper::ReleaseImplWithLogging(LONGLONG * pRefCount)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    LONGLONG newRefCount;

    StackSString ssMessage;
    ComCallWrapper *pWrap = GetMainWrapper();
    BuildRefCountLogMessage(W("Release"), ssMessage, GET_EXT_COM_REF(READ_REF(*pRefCount)-1));

    // Decrement the ref count
    newRefCount = ::InterlockedDecrement64(pRefCount);

    LogRefCount(pWrap, ssMessage, GET_EXT_COM_REF(newRefCount));

    return newRefCount;
}


//--------------------------------------------------------------------------
// simple ComCallWrapper for all simple std interfaces, that are not used very often
// like IProvideClassInfo, ISupportsErrorInfo etc.
//--------------------------------------------------------------------------
SimpleComCallWrapper::SimpleComCallWrapper()
{
    WRAPPER_NO_CONTRACT;

    memset(this, 0, sizeof(SimpleComCallWrapper));
}

//--------------------------------------------------------------------------
// VOID SimpleComCallWrapper::Cleanup()
//--------------------------------------------------------------------------
VOID SimpleComCallWrapper::Cleanup()
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    // in case the caller stills holds on to the IP
    for (int i = 0; i < enum_LastStdVtable; i++)
    {
        m_rgpVtable[i] = 0;
    }

    m_pWrap = NULL;
    m_pMT = NULL;

    if (m_pCPList)
    {
        for (UINT i = 0; i < m_pCPList->Size(); i++)
            delete (*m_pCPList)[i];

        delete m_pCPList;
        m_pCPList = NULL;
    }

    if (m_pTemplate)
    {
        m_pTemplate->Release();
        m_pTemplate = NULL;
    }

    if (m_pAuxData)
    {
        delete m_pAuxData;
        m_pAuxData = NULL;
    }
}


VOID SimpleComCallWrapper::Neuter()
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(m_pSyncBlock));
        PRECONDITION(!IsNeutered());
    }
    CONTRACTL_END;

    STRESS_LOG1 (LF_INTEROP, LL_INFO100, "Neutering CCW 0x%p\n", this->GetMainWrapper());

    // Disconnect the object from the CCW
    //  Starting now, if this object gets passed out
    //  to unmanaged code, it will create a new CCW tied
    //  to the domain it was passed out from.
    InteropSyncBlockInfo* pInteropInfo = m_pSyncBlock->GetInteropInfoNoCreate();
    if (pInteropInfo)
        pInteropInfo->SetCCW(NULL);

    // NULL the syncblock entry - we can't hang onto this anymore as the syncblock will be killed asynchronously to us.
    ResetSyncBlock();

    // Disconnect the CCW from the object
    //  Calls made on this CCW will no longer succeed.
    //  The CCW has been neutered.
    //   do this for each of the CCWs
    m_pWrap->Neuter();

    StackSString ssMessage;
    ComCallWrapper *pWrap = m_pWrap;
    if (g_pConfig->LogCCWRefCountChangeEnabled())
    {
        BuildRefCountLogMessage(W("Neuter"), ssMessage, GET_EXT_COM_REF(READ_REF(m_llRefCount) | CLEANUP_SENTINEL));
    }

    // Set the neutered bit on the ref-count.
    LONGLONG *pRefCount = &m_llRefCount;
    LONGLONG oldRefCount = *pRefCount;
    LONGLONG newRefCount = oldRefCount | CLEANUP_SENTINEL;
    while (InterlockedCompareExchange64((LONGLONG*)pRefCount, newRefCount, oldRefCount) != oldRefCount)
    {
        oldRefCount = *pRefCount;
        newRefCount = oldRefCount | CLEANUP_SENTINEL;
    }

    // IMPORTANT: Do not touch instance fields or any other data associated with the CCW beyond this
    // point unless newRefCount equals CLEANUP_SENTINEL (it's the only case when we know that Release
    // could not swoop in and destroy our data structures).

    if (g_pConfig->LogCCWRefCountChangeEnabled())
    {
        LogRefCount(pWrap, ssMessage, GET_EXT_COM_REF(newRefCount));
    }

    // If we hit the sentinel value, it's our responsibility to clean up.
    if (newRefCount == CLEANUP_SENTINEL)
        m_pWrap->Cleanup();
}

//--------------------------------------------------------------------------
//destructor
//--------------------------------------------------------------------------
SimpleComCallWrapper::~SimpleComCallWrapper()
{
    WRAPPER_NO_CONTRACT;

    Cleanup();
}

//--------------------------------------------------------------------------
// Init, with the MethodTable, pointer to the vtable of the interface
// and the main ComCallWrapper if the interface needs it
//--------------------------------------------------------------------------
void SimpleComCallWrapper::InitNew(OBJECTREF oref, ComCallWrapperCache *pWrapperCache, ComCallWrapper* pWrap,
                                ComCallWrapper *pClassWrap, SyncBlock *pSyncBlock,
                                ComCallWrapperTemplate* pTemplate)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
        PRECONDITION(oref != NULL);
        PRECONDITION(CheckPointer(pWrap));
        PRECONDITION(CheckPointer(pWrapperCache, NULL_OK));
        PRECONDITION(CheckPointer(pSyncBlock, NULL_OK));
        PRECONDITION(CheckPointer(pTemplate));
        PRECONDITION(m_pSyncBlock == NULL);
        PRECONDITION(CheckPointer(g_pExceptionClass));
    }
    CONTRACTL_END;

    MethodTable* pMT = pTemplate->GetClassType().GetMethodTable();
    PREFIX_ASSUME(pMT != NULL);


    m_pMT = pMT;
    m_pWrap = pWrap;
    m_pClassWrap = pClassWrap;
    m_pWrapperCache = pWrapperCache;
    m_pTemplate = pTemplate;
    m_pTemplate->AddRef();

    m_pOuter = NULL;

    m_pSyncBlock = pSyncBlock;

    if (pMT->IsComObjectType())
        m_flags |= enum_IsExtendsCom;

#ifdef _DEBUG
    for (int i = 0; i < enum_LastStdVtable; i++)
        _ASSERTE(GetStdInterfaceKind((IUnknown*)(&g_rgStdVtables[i])) == i);
#endif // _DEBUG

    for (int i = 0; i < enum_LastStdVtable; i++)
        m_rgpVtable[i] = g_rgStdVtables[i];

    // If the managed object extends a COM base class then we need to set IProvideClassInfo
    // to NULL until we determine if we need to use the IProvideClassInfo of the base class
    // or the one of the managed class.
    if (IsExtendsCOMObject())
        m_rgpVtable[enum_IProvideClassInfo] = NULL;

    // IErrorInfo is valid only for exception classes
    m_rgpVtable[enum_IErrorInfo] = NULL;

    // IDispatchEx is valid only for classes that have IExpando capabilities - which is no longer supported.
    m_rgpVtable[enum_IDispatchEx] = NULL;
}

//--------------------------------------------------------------------------
// ReInit,with the new sync block and the urt context
//--------------------------------------------------------------------------
void SimpleComCallWrapper::ReInit(SyncBlock* pSyncBlock)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(CheckPointer(pSyncBlock));
    }
    CONTRACTL_END;

    m_pSyncBlock = pSyncBlock;
}

//--------------------------------------------------------------------------
// Returns TRUE if the ICustomQI implementation returns Handled or Failed for IID_IMarshal.
//--------------------------------------------------------------------------
BOOL SimpleComCallWrapper::CustomQIRespondsToIMarshal()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(GetComCallWrapperTemplate()->SupportsICustomQueryInterface());
    }
    CONTRACTL_END;

    if ((m_flags & enum_CustomQIRespondsToIMarshal_Inited) == 0)
    {
        DWORD newFlags = enum_CustomQIRespondsToIMarshal_Inited;

        SafeComHolder<IUnknown> pUnk;
        if (GetComIPFromCCW_HandleCustomQI(GetMainWrapper(), IID_IMarshal, NULL, &pUnk))
        {
            newFlags |= enum_CustomQIRespondsToIMarshal;
        }
        FastInterlockOr((ULONG *)&m_flags, newFlags);
    }

    return (m_flags & enum_CustomQIRespondsToIMarshal);
}

//--------------------------------------------------------------------------
// Initializes the information used for exposing exceptions to COM.
//--------------------------------------------------------------------------
void SimpleComCallWrapper::InitExceptionInfo()
{
    LIMITED_METHOD_CONTRACT;
    m_rgpVtable[enum_IErrorInfo] = g_rgStdVtables[enum_IErrorInfo];
}

//--------------------------------------------------------------------------
// Initializes the IDispatchEx information.
//--------------------------------------------------------------------------
void SimpleComCallWrapper::InitDispatchExInfo()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM());

        // Make sure the class supports at least IReflect..
        PRECONDITION(SupportsIReflect(m_pMT));
    }
    CONTRACTL_END;

    SimpleCCWAuxData *pAuxData = GetOrCreateAuxData();
    if (pAuxData->m_pDispatchExInfo)
        return;

    // Create the DispatchExInfo object.
    NewHolder<DispatchExInfo> pDispExInfo = new DispatchExInfo(this, m_pMT);

    // Synchronize the DispatchExInfo with the actual object.
    pDispExInfo->SynchWithManagedView();

    // Swap the lock into the class member in a thread safe manner.
    if (NULL == FastInterlockCompareExchangePointer(&pAuxData->m_pDispatchExInfo, pDispExInfo.GetValue(), NULL))
        pDispExInfo.SuppressRelease();

    // Set the vtable entry to ensure that the next QI call will return immediately.
    m_rgpVtable[enum_IDispatchEx] = g_rgStdVtables[enum_IDispatchEx];
}

void SimpleComCallWrapper::SetUpCPList()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    CQuickArray<MethodTable *> SrcItfList;

    // If the list has already been set up, then return.
    if (m_pCPList)
        return;

    // Retrieve the list of COM source interfaces for the managed class.
    GetComSourceInterfacesForClass(m_pMT, SrcItfList);

    // Call the helper to do the rest of the set up.
    SetUpCPListHelper(SrcItfList.Ptr(), (int)SrcItfList.Size());
}


void SimpleComCallWrapper::SetUpCPListHelper(MethodTable **apSrcItfMTs, int cSrcItfs)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(apSrcItfMTs));
    }
    CONTRACTL_END;

    CPListHolder pCPList = NULL;
    ComCallWrapper *pWrap = GetMainWrapper();
    int NumCPs = 0;

    // Allocate the list of connection points.
    pCPList = CreateCPArray();
    pCPList->AllocThrows(cSrcItfs);

    for (int i = 0; i < cSrcItfs; i++)
    {
        // Create a CP helper thru which CP operations will be done.
        // Should we throw here instead of ignoring creation errors?
        ConnectionPoint *pCP = TryCreateConnectionPoint(pWrap, apSrcItfMTs[i]);
        if (pCP != NULL)
        {
            // Add the connection point to the list.
            (*pCPList)[NumCPs++] = pCP;
        }

    }

    // Now that we now the actual number of connection points we were
    // able to hook up, resize the array.
    pCPList->Shrink(NumCPs);

    // Finally, we set the connection point list in the simple wrapper. If
    // no other thread already set it, we set pCPList to NULL to indicate
    // that ownership has been transfered to the simple wrapper.
    if (InterlockedCompareExchangeT(&m_pCPList, pCPList.GetValue(), NULL) == NULL)
        pCPList.SuppressRelease();
}

ConnectionPoint *SimpleComCallWrapper::TryCreateConnectionPoint(ComCallWrapper *pWrap, MethodTable *pEventMT)
{
    CONTRACT (ConnectionPoint*)
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pWrap));
        PRECONDITION(CheckPointer(pEventMT));
        POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
    }
    CONTRACT_END;

    ConnectionPoint *pCP = NULL;

    EX_TRY
    {
        pCP = CreateConnectionPoint(pWrap, pEventMT);
    }
    EX_CATCH
    {
        pCP = NULL;
    }
    EX_END_CATCH(RethrowTerminalExceptions)

    RETURN pCP;
}

ConnectionPoint *SimpleComCallWrapper::CreateConnectionPoint(ComCallWrapper *pWrap, MethodTable *pEventMT)
{
    CONTRACT (ConnectionPoint*)
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM());
        PRECONDITION(CheckPointer(pWrap));
        PRECONDITION(CheckPointer(pEventMT));
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END;

    RETURN (new ConnectionPoint(pWrap, pEventMT));
}

CQuickArray<ConnectionPoint*> *SimpleComCallWrapper::CreateCPArray()
{
    CONTRACT (CQuickArray<ConnectionPoint*>*)
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM());
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END;

    RETURN (new CQuickArray<ConnectionPoint*>());
}

//--------------------------------------------------------------------------
// Returns TRUE if the simple wrapper represents a COM+ exception object.
//--------------------------------------------------------------------------
BOOL SimpleComCallWrapper::SupportsExceptions(MethodTable *pClass)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pClass, NULL_OK));
    }
    CONTRACTL_END;

    while (pClass != NULL)
    {
        if (pClass == g_pExceptionClass)
            return TRUE;

        pClass = pClass->GetComPlusParentMethodTable();
    }
    return FALSE;
}

//--------------------------------------------------------------------------
// Returns TRUE if the COM+ object that this wrapper represents implements
// IReflect.
//--------------------------------------------------------------------------
BOOL SimpleComCallWrapper::SupportsIReflect(MethodTable *pClass)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pClass));
    }
    CONTRACTL_END;

    // We want to disallow passing out IDispatchEx for Type inheritors to close a security hole.
    if (pClass == g_pRuntimeTypeClass)
        return FALSE;

    if (CoreLibBinder::IsClass(pClass, CLASS__TYPE_BUILDER))
        return FALSE;

    if (CoreLibBinder::IsClass(pClass, CLASS__TYPE))
        return FALSE;

    if (CoreLibBinder::IsClass(pClass, CLASS__ENUM_BUILDER))
        return FALSE;

    // Check to see if the MethodTable associated with the wrapper implements IReflect.
    return pClass->ImplementsInterface(CoreLibBinder::GetClass(CLASS__IREFLECT));
}

// NOINLINE to prevent RCWHolder from forcing caller to push/pop an FS:0 handler
NOINLINE BOOL SimpleComCallWrapper::ShouldUseManagedIProvideClassInfo()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    BOOL bUseManagedIProvideClassInfo = TRUE;

    // Retrieve the MethodTable of the wrapper.
    ComCallWrapper *pMainWrap = GetMainWrapper();

    // Only extensible RCW's should go down this code path.
    _ASSERTE(pMainWrap->IsExtendsCOMObject());

    MethodTable * pObjectMT = pMainWrap->GetSimpleWrapper()->GetMethodTable();
    MethodTable * pMT = pObjectMT;

    // Find the first COM visible IClassX starting at the bottom of the hierarchy and
    // going up the inheritance chain.
    while (pMT != NULL)
    {
        if (IsTypeVisibleFromCom(TypeHandle(pMT)))
            break;
        pMT = pMT->GetComPlusParentMethodTable();
    }

    // Since this is an extensible RCW if the CLR classes that derive from the COM component
    // are not visible then we will give out the COM component's IProvideClassInfo.
    if (pMT == NULL || pMT == g_pObjectClass)
    {
        SyncBlock* pSyncBlock = GetSyncBlock();
        _ASSERTE(pSyncBlock);

        RCWHolder pRCW(GetThread());
        RCWPROTECT_BEGIN(pRCW, pSyncBlock);

        bUseManagedIProvideClassInfo = !pRCW->SupportsIProvideClassInfo();

        RCWPROTECT_END(pRCW);
    }

    // Object should always be visible if we return TRUE
    _ASSERTE(!bUseManagedIProvideClassInfo || pMT != NULL);

    return bUseManagedIProvideClassInfo;
}


// QI for well known interfaces from within the runtime direct fetch, instead of guid comparisons.
// The returned interface is AddRef'd.
IUnknown* SimpleComCallWrapper::QIStandardInterface(Enum_StdInterfaces index)
{
    CONTRACT (IUnknown*)
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;

        // assert for valid index
        PRECONDITION(index < enum_LastStdVtable);
        INSTANCE_CHECK;
        POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
    }
    CONTRACT_END;

    IUnknown* pIntf = NULL;

    if (m_rgpVtable[index] != NULL)
    {
        pIntf = (IUnknown*)&m_rgpVtable[index];
    }
    else if (index == enum_IProvideClassInfo)
    {
        // If we either have a visible managed part to the class or if the base class
        // does not implement IProvideClassInfo then use the one on the managed class.
        if (ShouldUseManagedIProvideClassInfo())
        {
            // Set up the vtable pointer so that next time we don't have to determine
            // that the IProvideClassInfo is provided by the managed class.
            m_rgpVtable[enum_IProvideClassInfo] = g_rgStdVtables[enum_IProvideClassInfo];

            // Return the interface pointer to the standard IProvideClassInfo interface.
            pIntf = (IUnknown*)&m_rgpVtable[enum_IProvideClassInfo];
        }
    }
    else if (index == enum_IErrorInfo)
    {
        if (SupportsExceptions(m_pMT))
        {
            // Initialize the exception info before we return the interface.
            InitExceptionInfo();
            pIntf = (IUnknown*)&m_rgpVtable[enum_IErrorInfo];
        }
    }
    else if (index == enum_IDispatchEx)
    {
        if (SupportsIReflect(m_pMT))
        {
            // Initialize the DispatchExInfo before we return the interface.
            InitDispatchExInfo();
            pIntf = (IUnknown*)&m_rgpVtable[enum_IDispatchEx];
        }
    }

    // If we found what we were looking for, then AddRef the wrapper.
    // Note that we don't do SafeAddRef(pIntf) because it's overkill to
    // go via IUnknown when we already have the wrapper in-hand.
    if (pIntf)
    {
        if (index == enum_InnerUnknown)
            this->AddRef();
        else
            this->AddRefWithAggregationCheck();
    }

    RETURN pIntf;
}

#include <optsmallperfcritical.h>   // improves CCW QI perf by ~10%

#define IS_EQUAL_GUID(refguid,data1,data2,data3, data4,data5,data6,data7,data8,data9,data10,data11) \
    ((((DWORD*)&refguid)[0] == (data1)) &&                                             \
     (((DWORD*)&refguid)[1] == (((data3)<<16)|(data2))) &&                             \
     (((DWORD*)&refguid)[2] == (((data7)<<24)|((data6)<<16)|((data5)<<8)|(data4))) &&  \
     (((DWORD*)&refguid)[3] == (((data11)<<24)|((data10)<<16)|((data9)<<8)|(data8))))  \

#define IS_EQUAL_GUID_LOW_12_BYTES(refguid,data1,data2,data3, data4,data5,data6,data7,data8,data9,data10,data11) \
    ((((DWORD*)&refguid)[1] == (((data3)<<16)|(data2))) &&                             \
     (((DWORD*)&refguid)[2] == (((data7)<<24)|((data6)<<16)|((data5)<<8)|(data4))) &&  \
     (((DWORD*)&refguid)[3] == (((data11)<<24)|((data10)<<16)|((data9)<<8)|(data8))))  \

#define HANDLE_IID_INLINE(itfEnum,data1,data2,data3, data4,data5,data6,data7,data8,data9,data10,data11)     \
    CASE_IID_INLINE(itfEnum,data1,data2,data3, data4,data5,data6,data7,data8,data9,data10,data11)           \
    {                                                                                                       \
        RETURN QIStandardInterface(itfEnum);                                                                \
    }                                                                                                       \
    break;                                                                                                  \

#define CASE_IID_INLINE(itfEnum,data1,data2,data3, data4,data5,data6,data7,data8,data9,data10,data11)               \
    case data1:                                                                                                     \
        if (IS_EQUAL_GUID_LOW_12_BYTES(riid,data1,data2,data3, data4,data5,data6,data7,data8,data9,data10,data11))  \

#define IS_KNOWN_INTERFACE_CONTRACT(iid) \
    CONTRACT(bool)                                          \
    {                                                       \
        MODE_ANY;                                           \
        NOTHROW;                                            \
        GC_NOTRIGGER;                                       \
        POSTCONDITION(RETVAL == !!IsEqualGUID(iid, riid));  \
    }                                                       \
    CONTRACT_END;                                           \

inline bool IsIUnknown(REFIID riid)
{
    IS_KNOWN_INTERFACE_CONTRACT(IID_IUnknown);
    RETURN IS_EQUAL_GUID(riid, 0x00000000,0x0000,0x0000,0xC0,0x00,0x00,0x00,0x00,0x00,0x00,0x46);
}
inline bool IsIDispatch(REFIID riid)
{
    IS_KNOWN_INTERFACE_CONTRACT(IID_IDispatch);
    RETURN IS_EQUAL_GUID(riid, 0x00020400,0x0000,0x0000,0xC0,0x00,0x00,0x00,0x00,0x00,0x00,0x46);
}
inline bool IsGUID_NULL(REFIID riid)
{
    IS_KNOWN_INTERFACE_CONTRACT(GUID_NULL);
    RETURN IS_EQUAL_GUID(riid, 0x00000000,0x0000,0x0000,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00);
}
inline bool IsIErrorInfo(REFIID riid)
{
    IS_KNOWN_INTERFACE_CONTRACT(IID_IErrorInfo);
    RETURN IS_EQUAL_GUID(riid, 0x1CF2B120,0x547D,0x101B,0x8E,0x65,0x08,0x00,0x2B,0x2B,0xD1,0x19);
}

// QI for well known interfaces from within the runtime based on an IID.
IUnknown* SimpleComCallWrapper::QIStandardInterface(REFIID riid)
{
    CONTRACT (IUnknown*)
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INSTANCE_CHECK;
        POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
    }
    CONTRACT_END;

    // IID_IMarshal                    00000003-0000-0000-C000-000000000046
    // IID_IErrorInfo                  1CF2B120-547D-101B-8E65-08002B2BD119
    // IID_IDispatchEx                 A6EF9860-C720-11d0-9337-00A0C90DCAA9
    // IID_IProvideClassInfo           B196B283-BAB4-101A-B69C-00AA00341D07
    // IID_IConnectionPointContainer   B196B284-BAB4-101A-B69C-00AA00341D07
    // IID_IObjectSafety               CB5BDC81-93C1-11cf-8F20-00805F2CD064
    // IID_ISupportErrorInfo           DF0B3D60-548F-101B-8E65-08002B2BD119
    // IID_IAgileObject                94ea2b94-e9cc-49e0-c0ff-ee64ca8f5b90

    // Switch on the first DWORD since they're all (currently) unique.
    switch (riid.Data1)
    {
    HANDLE_IID_INLINE(enum_IMarshal                 ,0x00000003,0x0000,0x0000,0xC0,0x00,0x00,0x00,0x00,0x00,0x00,0x46);
    HANDLE_IID_INLINE(enum_IErrorInfo               ,0x1CF2B120,0x547D,0x101B,0x8E,0x65,0x08,0x00,0x2B,0x2B,0xD1,0x19);
    HANDLE_IID_INLINE(enum_IDispatchEx              ,0xA6EF9860,0xC720,0x11d0,0x93,0x37,0x00,0xA0,0xC9,0x0D,0xCA,0xA9); // hit3, !=
    HANDLE_IID_INLINE(enum_ISupportsErrorInfo       ,0xDF0B3D60,0x548F,0x101B,0x8E,0x65,0x08,0x00,0x2B,0x2B,0xD1,0x19);
    HANDLE_IID_INLINE(enum_IProvideClassInfo        ,0xB196B283,0xBAB4,0x101A,0xB6,0x9C,0x00,0xAA,0x00,0x34,0x1D,0x07);  // hit4, !=
    HANDLE_IID_INLINE(enum_IConnectionPointContainer,0xB196B284,0xBAB4,0x101A,0xB6,0x9C,0x00,0xAA,0x00,0x34,0x1D,0x07);  // b196b284 101abab4 aa009cb6 071d3400
    CASE_IID_INLINE(  enum_IAgileObject            ,0x94ea2b94,0xe9cc,0x49e0,0xc0,0xff,0xee,0x64,0xca,0x8f,0x5b,0x90)
        {
            // Don't implement IAgileObject if we are aggregated, if the object explicitly implements IMarshal, or if its ICustomQI returns
            // Failed or Handled for IID_IMarshal (compat).
            if (!IsAggregated())
            {
                ComCallWrapperTemplate *pTemplate = GetComCallWrapperTemplate();
                if (!pTemplate->ImplementsIMarshal())
                {
                    if (!pTemplate->SupportsICustomQueryInterface() || !CustomQIRespondsToIMarshal())
                    {
                        RETURN QIStandardInterface(enum_IAgileObject);
                    }
                }
            }
        }
        break;
    }

    RETURN NULL;
}
#include <optdefault.h>

//--------------------------------------------------------------------------
// Init Outer unknown, cache a GIT cookie
//--------------------------------------------------------------------------
void SimpleComCallWrapper::InitOuter(IUnknown* pOuter)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(CheckPointer(pOuter, NULL_OK));
    }
    CONTRACTL_END;

    if (pOuter != NULL)
        m_pOuter = pOuter;

    MarkAggregated();
}

//--------------------------------------------------------------------------
// Init Outer unknown, cache a GIT cookie
//--------------------------------------------------------------------------
void SimpleComCallWrapper::ResetOuter()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    m_pOuter = NULL;

    if (IsAggregated())
        UnMarkAggregated();
}


//--------------------------------------------------------------------------
// Get Outer Unknown on the correct thread
//--------------------------------------------------------------------------
IUnknown* SimpleComCallWrapper::GetOuter()
{
    CONTRACT (IUnknown*)
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
    }
    CONTRACT_END;

    if (m_pClassWrap)
    {
        // Forward to the real wrapper if this CCW represents a variant interface
        RETURN m_pClassWrap->GetSimpleWrapper()->GetOuter();
    }

    RETURN m_pOuter;
}

BOOL SimpleComCallWrapper::FindConnectionPoint(REFIID riid, IConnectionPoint **ppCP)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(ppCP));
    }
    CONTRACTL_END;

    // If the connection point list hasn't been set up yet, then set it up now.
    if (!m_pCPList)
        SetUpCPList();

    // Search through the list for a connection point for the requested IID.

    // Go to preemp mode early to prevent multiple GC mode switches.
    GCX_PREEMP();

    for (UINT i = 0; i < m_pCPList->Size(); i++)
    {
        ConnectionPoint *pCP = (*m_pCPList)[i];
        if (pCP->GetIID() == riid)
        {
            // We found a connection point for the requested IID.
            HRESULT hr = SafeQueryInterfacePreemp(pCP, IID_IConnectionPoint, (IUnknown**)ppCP);
            _ASSERTE(hr == S_OK);
            return TRUE;
        }
    }

    return FALSE;
}

void SimpleComCallWrapper::EnumConnectionPoints(IEnumConnectionPoints **ppEnumCP)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM());
        PRECONDITION(CheckPointer(ppEnumCP));
    }
    CONTRACTL_END;

    // If the connection point list hasn't been set up yet, then set it up now.
    if (!m_pCPList)
        SetUpCPList();

    // Create a new connection point enum.
    ComCallWrapper *pWrap = GetMainWrapper();
    NewHolder<ConnectionPointEnum>pCPEnum = new ConnectionPointEnum(pWrap, m_pCPList);

    // Retrieve the IEnumConnectionPoints interface. This cannot fail.
    HRESULT hr = SafeQueryInterface((IUnknown*)pCPEnum, IID_IEnumConnectionPoints, (IUnknown**)ppEnumCP);
    _ASSERTE(hr == S_OK);

    pCPEnum.SuppressRelease();
}

//--------------------------------------------------------------------------
// COM called wrappers on COM+ objects
//  Purpose: Expose COM+ objects as COM classic Interfaces
//  Reqmts:  Wrapper has to have the same layout as the COM2 interface
//
//  The wrapper objects are aligned at 16 bytes, and the original this
//  pointer is replicated every 16 bytes, so for any COM2 interface
//  within the wrapper, the original 'this' can be obtained by masking
//  low 4 bits of COM2 IP.
//
//           16 byte aligned                            COM2 Vtable
//           +-----------+
//           | Org. this |
//           +-----------+                              +-----+
// COM2 IP-->| VTable ptr|----------------------------->|slot1|
//           +-----------+           +-----+            +-----+
// COM2 IP-->| VTable ptr|---------->|slot1|            |slot2|
//           +-----------+           +-----+            +     +
//           | VTable ptr|           | ....|            | ... |
//           +-----------+           +     +            +     +
//           | Org. this |           |slotN|            |slotN|
//           +           +           +-----+            +-----+
//           |  ....     |
//           +           +
//           |  |
//           +-----------+
//
//
//  VTable and Stubs: can share stub code, we need to have different vtables
//                    for different interfaces, so the stub can jump to different
//                    marshalling code.
//  Stubs : adjust this pointer and jump to the appropriate address,
//  Marshalling params and results, based on the method signature the stub jumps to
//  appropriate code to handle marshalling and unmarshalling.
//
//--------------------------------------------------------------------------

//--------------------------------------------------------------------------
// Check if the wrapper has been deactivated
//--------------------------------------------------------------------------
BOOL ComCallWrapper::IsHandleWeak()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    SimpleComCallWrapper* simpleWrap = GetSimpleWrapper();
    _ASSERTE(simpleWrap);
    return simpleWrap->IsHandleWeak();
}

//--------------------------------------------------------------------------
// Mark the wrapper as holding a weak handle to the object
//--------------------------------------------------------------------------
void ComCallWrapper::MarkHandleWeak()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    SimpleComCallWrapper* simpleWrap = GetSimpleWrapper();
    _ASSERTE(simpleWrap);
    simpleWrap->MarkHandleWeak();
}

//--------------------------------------------------------------------------
// Mark the wrapper as not having a weak handle
//--------------------------------------------------------------------------
void ComCallWrapper::ResetHandleStrength()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    SimpleComCallWrapper* simpleWrap = GetSimpleWrapper();
    _ASSERTE(simpleWrap);
    simpleWrap->ResetHandleStrength();
}

//--------------------------------------------------------------------------
// Check if the wrapper was activated via COM
//--------------------------------------------------------------------------
BOOL ComCallWrapper::IsComActivated()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    SimpleComCallWrapper* simpleWrap = GetSimpleWrapper();
    _ASSERTE(simpleWrap);
    return simpleWrap->IsComActivated();
}

//--------------------------------------------------------------------------
// Mark the wrapper as being created via COM activation
//--------------------------------------------------------------------------
VOID ComCallWrapper::MarkComActivated()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    SimpleComCallWrapper* simpleWrap = GetSimpleWrapper();
    _ASSERTE(simpleWrap);
    simpleWrap->MarkComActivated();
}

//--------------------------------------------------------------------------
// void ComCallWrapper::InitializeOuter(IUnknown* pOuter)
// init outer unknown, aggregation support
//--------------------------------------------------------------------------
void ComCallWrapper::InitializeOuter(IUnknown* pOuter)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(CheckPointer(pOuter, NULL_OK));
    }
    CONTRACTL_END;

    GetSimpleWrapper()->InitOuter(pOuter);
}


//--------------------------------------------------------------------------
// BOOL ComCallWrapper::IsAggregated()
// check if the wrapper is aggregated
//--------------------------------------------------------------------------
BOOL ComCallWrapper::IsAggregated()
{
    WRAPPER_NO_CONTRACT;

    return GetSimpleWrapper()->IsAggregated();
}


//--------------------------------------------------------------------------
// BOOL ComCallWrapper::IsExtendsCOMObject(()
// check if the wrapper is to a managed object that extends a com object
//--------------------------------------------------------------------------
BOOL ComCallWrapper::IsExtendsCOMObject()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    return GetSimpleWrapper()->IsExtendsCOMObject();
}

//--------------------------------------------------------------------------
// HRESULT ComCallWrapper::GetInnerUnknown(void** ppv)
// aggregation support, get inner unknown
//--------------------------------------------------------------------------
HRESULT ComCallWrapper::GetInnerUnknown(void **ppv)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(ppv));
        PRECONDITION(GetSimpleWrapper()->GetOuter() != NULL);
    }
    CONTRACTL_END;

    return GetSimpleWrapper()->GetInnerUnknown(ppv);
}

//--------------------------------------------------------------------------
// Get Outer Unknown on the correct thread
//--------------------------------------------------------------------------
IUnknown* ComCallWrapper::GetOuter()
{
    CONTRACT (IUnknown*)
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
    }
    CONTRACT_END;

    RETURN GetSimpleWrapper()->GetOuter();
}

//--------------------------------------------------------------------------
// SyncBlock* ComCallWrapper::GetSyncBlock()
//--------------------------------------------------------------------------
SyncBlock* ComCallWrapper::GetSyncBlock()
{
    CONTRACT (SyncBlock*)
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END;

    RETURN GetSimpleWrapper()->GetSyncBlock();
}

//--------------------------------------------------------------------------
//ComCallWrapper* ComCallWrapper::CopyFromTemplate(ComCallWrapperTemplate* pTemplate,
//                                                 OBJECTREF* pRef)
//  create a wrapper and initialize it from the template
//--------------------------------------------------------------------------
ComCallWrapper* ComCallWrapper::CopyFromTemplate(ComCallWrapperTemplate* pTemplate,
                                                 ComCallWrapperCache *pWrapperCache,
                                                 OBJECTHANDLE oh)
{
    CONTRACT (ComCallWrapper*)
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM());
        PRECONDITION(CheckPointer(pTemplate));
        PRECONDITION(CheckPointer(pWrapperCache));
        PRECONDITION(oh != NULL);
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END;

    // num interfaces on the object
    size_t numInterfaces = pTemplate->GetNumInterfaces();

    // we have a template, create a wrapper and initialize from the template
    // alloc wrapper, aligned to cache line
    NewCCWHolder pStartWrapper(pWrapperCache);
    pStartWrapper = (ComCallWrapper*)pWrapperCache->GetCacheLineAllocator()->
#ifdef HOST_64BIT
                                    GetCacheLine64();
    _ASSERTE(sizeof(ComCallWrapper) <= 64);
#else
                                    GetCacheLine32();
    _ASSERTE(sizeof(ComCallWrapper) <= 32);
#endif

    if (pStartWrapper == NULL)
        COMPlusThrowOM();

    LOG((LF_INTEROP, LL_INFO100, "ComCallWrapper::CopyFromTemplate on Object %8.8x, Wrapper %8.8x\n", oh, static_cast<ComCallWrapperPtr>(pStartWrapper)));

    // addref commgr
    pWrapperCache->AddRef();

    // store the object handle
    pStartWrapper->m_ppThis = oh;

    // The first slot will hold the Basic interface.
    // The second slot will hold the IClassX interface which will be generated on the fly.
    unsigned blockIndex = 0;
    if (pTemplate->RepresentsVariantInterface())
    {
        // interface CCW doesn't need the basic ComMT, it will fall back to its class CCW
        // for anything but the one variant interface it represents
        pStartWrapper->m_rgpIPtr[blockIndex++] = NULL;
    }
    else
    {
        pStartWrapper->m_rgpIPtr[blockIndex++] = (SLOT *)(pTemplate->GetBasicComMT() + 1);
    }
    pStartWrapper->m_rgpIPtr[blockIndex++] = NULL;

    ComCallWrapper* pWrapper = pStartWrapper;
    for (unsigned i =0; i< numInterfaces; i++)
    {
        if (blockIndex >= NumVtablePtrs)
        {
            // alloc wrapper, aligned 32 bytes
            ComCallWrapper* pNewWrapper = (ComCallWrapper*)pWrapperCache->GetCacheLineAllocator()->
#ifdef HOST_64BIT
                                          GetCacheLine64();
            _ASSERTE(sizeof(ComCallWrapper) <= 64);
#else
                                          GetCacheLine32();
            _ASSERTE(sizeof(ComCallWrapper) <= 32);
#endif

            _ASSERTE(0 == (((DWORD_PTR)pNewWrapper) & ~enum_ThisMask));

            // Link the wrapper
            SetNext(pWrapper, pNewWrapper);

            blockIndex = 0; // reset block index
            if (pNewWrapper == NULL)
            {
                RETURN NULL;
            }

            pWrapper = pNewWrapper;

            // initialize the object reference
            pWrapper->m_ppThis = oh;
        }

        pWrapper->m_rgpIPtr[blockIndex++] = pTemplate->GetVTableSlot(i);
    }

    // If the wrapper is part of a chain, then set the terminator.
    if (pWrapper != pStartWrapper)
        SetNext(pWrapper, LinkedWrapperTerminator);

    pStartWrapper.SuppressRelease();

    RETURN pStartWrapper;
}

//--------------------------------------------------------------------------
//  identify the location within the wrapper where the vtable for this index will
//  be stored
//--------------------------------------------------------------------------
SLOT** ComCallWrapper::GetComIPLocInWrapper(ComCallWrapper* pWrap, unsigned int iIndex)
{
    CONTRACT (SLOT**)
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(CheckPointer(pWrap));
        PRECONDITION(iIndex > 1);  // We should never attempt to get the basic or IClassX interface here.
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END;

    SLOT** pTearOff = NULL;
    while (iIndex >= NumVtablePtrs)
    {
        //@todo delayed creation support
        _ASSERTE(pWrap->IsLinked() != 0);
        pWrap = GetNext(pWrap);
        iIndex-= NumVtablePtrs;
    }
    _ASSERTE(pWrap != NULL);
    pTearOff = (SLOT **)&pWrap->m_rgpIPtr[iIndex];

    RETURN pTearOff;
}

//--------------------------------------------------------------------------
// void ComCallWrapper::Cleanup(ComCallWrapper* pWrap)
// clean up , release gc registered reference and free wrapper
//--------------------------------------------------------------------------
void ComCallWrapper::Cleanup()
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        INSTANCE_CHECK;
    }
    CONTRACTL_END;

    _ASSERTE(m_pSimpleWrapper);

    // Save it into a variable to observe a consistent state
    ULONG refCount = m_pSimpleWrapper->GetRefCount();

    LOG((LF_INTEROP, LL_INFO100,
        "Calling ComCallWrapper::Cleanup on CCW 0x%p. cbRef = 0x%x\n",
        this, refCount));

    if (refCount != 0)
    {
        // _ASSERTE(g_fEEShutDown == TRUE);
        // could be either in shutdown or forced GC in appdomain unload
        // there are external COM references to this wrapper
        // so let us just forget about cleaning now
        // when the ref-count reaches 0, we will
        // do the cleanup anyway
        return;
    }

    STRESS_LOG1 (LF_INTEROP, LL_INFO100, "Cleaning up CCW 0x%p\n", this);

    // Retrieve the COM call wrapper cache before we clear anything
    ComCallWrapperCache *pWrapperCache = m_pSimpleWrapper->GetWrapperCache();

    BOOL fOwnsHandle = FALSE;
    SyncBlock* pSyncBlock = m_pSimpleWrapper->GetSyncBlock();

    // only the "root" CCW owns the handle
    // Even though we don't use this for native deriving from managed scenarios,
    // we still use multiple CCWs from variance
    fOwnsHandle = !(GetComCallWrapperTemplate()->RepresentsVariantInterface());

    //  This CCW may have belonged to an object that was killed when its AD was unloaded, but the CCW has a positive RefCount.
    //  In this case, the SyncBlock and/or InteropSyncBlockInfo will be null.
    if (pSyncBlock)
    {
        InteropSyncBlockInfo* pInteropInfo = pSyncBlock->GetInteropInfoNoCreate();

        if (pInteropInfo)
        {
            // Disconnect the object from the CCW
            //  Starting now, if this object gets passed out
            //  to unmanaged code, it will create a new CCW tied
            //  to the domain it was passed out from.
            pInteropInfo->SetCCW(NULL);

            // NULL the syncblock entry - we can't hang onto this anymore as the syncblock will be killed asynchronously to us.
            m_pSimpleWrapper->ResetSyncBlock();

            // Check for an associated RCW
            RCWHolder pRCW(GetThread());
            pRCW.InitNoCheck(pSyncBlock);
            NewRCWHolder pNewRCW = pRCW.GetRawRCWUnsafe();

            if (!pRCW.IsNull())
            {
                // Remove the RCW from the cache
                RCWCache* pCache = RCWCache::GetRCWCacheNoCreate();
                _ASSERTE(pCache);

                {
                    // Switch to cooperative mode for RCWCache::LockHolder::LockHolder (COOPERATIVE)
                    GCX_COOP();

                    RCWCache::LockHolder lh(pCache);
                    pCache->RemoveWrapper(&pRCW);
                }
            }
        }
    }

    if (m_pSimpleWrapper)
    {
        m_pSimpleWrapper->Cleanup();
    }

    if (g_fEEStarted || m_pSimpleWrapper->GetOuter() == NULL)
    {
        delete m_pSimpleWrapper;
        ClearSimpleWrapper(this);
    }

    if (fOwnsHandle && m_ppThis)
    {
        LOG((LF_INTEROP, LL_INFO100, "ComCallWrapper::Cleanup on Object %8.8x\n", m_ppThis));
        ClearHandle();
    }

    m_ppThis = NULL;
    FreeWrapper(pWrapperCache);
}

//--------------------------------------------------------------------------
// void ComCallWrapper::Neuter()
// walk the CCW list and clear all handles to the object
//--------------------------------------------------------------------------
void ComCallWrapper::Neuter()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    ClearHandle();

    ComCallWrapper* pWrap = this;
    while (pWrap != NULL)
    {
        ComCallWrapper* pTempWrap = ComCallWrapper::GetNext(pWrap);
        pWrap->m_ppThis = NULL;
        pWrap = pTempWrap;
    }
}

//--------------------------------------------------------------------------
// void ComCallWrapper::ClearHandle()
// clear the ref-counted handle
//--------------------------------------------------------------------------
void ComCallWrapper::ClearHandle()
{
    WRAPPER_NO_CONTRACT;

    OBJECTHANDLE pThis = m_ppThis;
    if (FastInterlockCompareExchangePointer(&m_ppThis, NULL, pThis) == pThis)
    {
        DestroyRefcountedHandle(pThis);
    }
}

SLOT** ComCallWrapper::GetFirstInterfaceSlot()
{
    CONTRACT(SLOT**)
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END;

    SLOT** firstInterface = GetComIPLocInWrapper(this, Slot_FirstInterface);
    RETURN firstInterface;
}

//--------------------------------------------------------------------------
// void ComCallWrapper::FreeWrapper(ComCallWrapper* pWrap)
// walk the list and free all wrappers
//--------------------------------------------------------------------------
void ComCallWrapper::FreeWrapper(ComCallWrapperCache *pWrapperCache)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        INSTANCE_CHECK;
        PRECONDITION(CheckPointer(pWrapperCache));
    }
    CONTRACTL_END;

    {
        ComCallWrapperCache::LockHolder lh(pWrapperCache);

        ComCallWrapper* pWrap2 = IsLinked() ? GetNext(this) : NULL;

        while (pWrap2 != NULL)
        {
            ComCallWrapper* pTempWrap = GetNext(pWrap2);
    #ifdef HOST_64BIT
            pWrapperCache->GetCacheLineAllocator()->FreeCacheLine64(pWrap2);
    #else //HOST_64BIT
            pWrapperCache->GetCacheLineAllocator()->FreeCacheLine32(pWrap2);
    #endif //HOST_64BIT
            pWrap2 = pTempWrap;
        }
    #ifdef HOST_64BIT
        pWrapperCache->GetCacheLineAllocator()->FreeCacheLine64(this);
    #else //HOST_64BIT
        pWrapperCache->GetCacheLineAllocator()->FreeCacheLine32(this);
    #endif //HOST_64BIT
    }

    // release ccw mgr
    pWrapperCache->Release();
}

//--------------------------------------------------------------------------
//ComCallWrapper* ComCallWrapper::CreateWrapper(OBJECTREF* ppObj, ComCallWrapperTemplate *pTemplate, ComCallWrapper *pClassCCW)
// this function should be called only with pre-emptive GC disabled
// GCProtect the object ref being passed in, as this code could enable gc
//--------------------------------------------------------------------------
ComCallWrapper* ComCallWrapper::CreateWrapper(OBJECTREF* ppObj, ComCallWrapperTemplate *pTemplate, ComCallWrapper *pClassCCW)
{
    CONTRACT(ComCallWrapper *)
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(ppObj != NULL);
        PRECONDITION(CheckPointer(pTemplate, NULL_OK));
        PRECONDITION(CheckPointer(pClassCCW, NULL_OK));
        PRECONDITION(pTemplate == NULL || !pTemplate->RepresentsVariantInterface() || pClassCCW != NULL);
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END;

    ComCallWrapper* pStartWrapper = NULL;
    OBJECTREF pServer = NULL;

    GCPROTECT_BEGIN(pServer);

    pServer = *ppObj;

    // grab the sync block from the server
    SyncBlock* pSyncBlock = pServer->GetSyncBlock();

    pSyncBlock->SetPrecious();

    // if the object belongs to a domain neutral class, need to allocate the wrapper in the default domain.
    // The object is potentially agile so if allocate out of the current domain and then hand out to
    // multiple domains we might never release the wrapper for that object and hence never unload the CCWC.
    ComCallWrapperCache *pWrapperCache = NULL;
    TypeHandle thClass = pServer->GetTypeHandle();

    pWrapperCache = thClass.GetMethodTable()->GetLoaderAllocator()->GetComCallWrapperCache();

    {
        // check if somebody beat us to it
        pStartWrapper = GetWrapperForObject(pServer, pTemplate);

        if (pStartWrapper == NULL)
        {
            if (pTemplate == NULL)
            {
                // get the wrapper template from object's type if it was not passed explicitly
                pTemplate = ComCallWrapperTemplate::GetTemplate(thClass);
            }

            // Make sure the CCW will be destroyed when exception happens
            // Also keep pWrapperCache alive within this scope
            // It needs to be destroyed after ComCallWrapperCache::LockHolder otherwise there would be a lock violation
            NewCCWHolder pNewCCW(pWrapperCache);

            // Now we'll take the lock in a place where we won't be calling managed code and check again.
            {
                ComCallWrapperCache::LockHolder lh(pWrapperCache);

                pStartWrapper = GetWrapperForObject(pServer, pTemplate);
                if (pStartWrapper == NULL)
                {
                    Wrapper<OBJECTHANDLE, DoNothing, DestroyRefcountedHandle> oh;

                    ComCallWrapper *pRootWrapper = GetWrapperForObject(pServer, NULL);
                    if (pRootWrapper == NULL)
                    {
                        // create handle for the object. This creates a handle in the current domain. We can't tell
                        // if the object is agile in non-checked, so we trust that our checking works and when we
                        // attempt to hand this out to another domain then we will assume that the object is truly
                        // agile and will convert the handle to a global handle.
                        oh = GetAppDomain()->CreateRefcountedHandle(NULL);
                        _ASSERTE(oh);
                    }
                    else
                    {
                        // if the object already has a CCW, we reuse the handle
                        oh = pRootWrapper->GetObjectHandle();
                        oh.SuppressRelease();
                    }

                    // copy from template
                    pNewCCW = CopyFromTemplate(pTemplate, pWrapperCache, oh);

                    NewHolder<SimpleComCallWrapper> pSimpleWrap = new SimpleComCallWrapper();

                    pSimpleWrap->InitNew(pServer, pWrapperCache, pNewCCW, pClassCCW, pSyncBlock, pTemplate);

                    InitSimpleWrapper(pNewCCW, pSimpleWrap);

                    if (pRootWrapper == NULL)
                    {
                        // store the object in the handle - this must happen before we publish the CCW
                        // in the sync block, so that other threads don't see a CCW pointing to nothing
                        StoreObjectInHandle( oh, pServer );

                        // finally, store the wrapper for the object in the sync block
                        pSyncBlock->GetInteropInfo()->SetCCW(pNewCCW);
                    }
                    else
                    {
                        // link the wrapper to the existing chain of CCWs
                        while (ComCallWrapper::GetNext(pRootWrapper) != NULL)
                        {
                            pRootWrapper = ComCallWrapper::GetNext(pRootWrapper);
                        }
                        ComCallWrapper::SetNext(pRootWrapper, pNewCCW);
                    }

                    oh.SuppressRelease();
                    pNewCCW.SuppressRelease();
                    pSimpleWrap.SuppressRelease();

                    pStartWrapper = pNewCCW;
                }
            }
        }
    }
    GCPROTECT_END();

    RETURN pStartWrapper;
}

//--------------------------------------------------------------------------
// Get IClassX interface pointer from the wrapper. This method will also
// lay out the IClassX COM method table if it has not yet been laid out.
// The returned interface is AddRef'd.
//--------------------------------------------------------------------------
IUnknown* ComCallWrapper::GetIClassXIP(bool inspectionOnly)
{
    CONTRACT (IUnknown*)
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
    }
    CONTRACT_END;

    ComCallWrapper *pWrap = this;
    IUnknown *pIntf = NULL;
    ComMethodTable* pIClassXComMT = NULL;

    // The IClassX VTable pointer is in the start wrapper.
    if (pWrap->IsLinked())
        pWrap = ComCallWrapper::GetStartWrapper(pWrap);

    SLOT* slot = pWrap->m_rgpIPtr[Slot_IClassX];
    if (NULL == slot)
    {
        if (inspectionOnly)
            RETURN NULL;

        // Get the IClassX ComMethodTable (create if it doesn't exist),
        //  and set it into the vtable map.
        pIClassXComMT = m_pSimpleWrapper->m_pTemplate->GetClassComMT();
        pWrap->m_rgpIPtr[Slot_IClassX] = (SLOT *)(pIClassXComMT + 1);
    }
    else
    {
        pIClassXComMT = (ComMethodTable*)slot - 1;
    }

    // Lay out of the IClassX COM method table if it has not yet been laid out.
    if (!pIClassXComMT->IsLayoutComplete())
    {
        // We won't attempt to lay out the class if we are only trying to
        // passively inspect the interface.
        if (inspectionOnly)
            RETURN NULL;
        else
            pIClassXComMT->LayOutClassMethodTable();
    }

    // Return the IClassX vtable pointer.
    pIntf = (IUnknown*)&pWrap->m_rgpIPtr[Slot_IClassX];

    // If we are only inspecting, don't addref.
    if (inspectionOnly)
        RETURN pIntf;

    // AddRef the wrapper.
    // Note that we don't do SafeAddRef(pIntf) because it's overkill to
    // go via IUnknown when we already have the wrapper in-hand.
    ULONG cbRef = pWrap->AddRefWithAggregationCheck();

    // 0xbadF00d implies the AddRef didn't go through
    RETURN ((cbRef != 0xbadf00d) ? pIntf : NULL);
}

IUnknown* ComCallWrapper::GetBasicIP(bool inspectionOnly)
{
    CONTRACT (IUnknown*)
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
    }
    CONTRACT_END;

    // If the legacy switch is set, we'll always return the IClassX IP
    //  when QIing for IUnknown or IDispatch.
    // Whidbey Tactics has decided to make this opt-in rather than
    // opt-out for now.  Remove the check for the legacy switch.
    if (GetComCallWrapperTemplate()->SupportsIClassX())
        RETURN GetIClassXIP(inspectionOnly);

    ComCallWrapper *pWrap = this;
    IUnknown *pIntf = NULL;

    // The IClassX VTable pointer is in the start wrapper.
    if (pWrap->IsLinked())
        pWrap = ComCallWrapper::GetStartWrapper(pWrap);

    ComMethodTable* pIBasicComMT = (ComMethodTable*)pWrap->m_rgpIPtr[Slot_Basic] - 1;
    _ASSERTE(pIBasicComMT);

    // Lay out the basic COM method table if it has not yet been laid out.
    if (!pIBasicComMT->IsLayoutComplete())
    {
        if (inspectionOnly)
            RETURN NULL;
        else
            pIBasicComMT->LayOutBasicMethodTable();
    }

    // Return the basic vtable pointer.
    pIntf = (IUnknown*)&pWrap->m_rgpIPtr[Slot_Basic];

    // If we are not addref'ing the IUnknown (for passive inspection like ETW), return it now.
    if (inspectionOnly)
        RETURN pIntf;

    // AddRef the wrapper.
    // Note that we don't do SafeAddRef(pIntf) because it's overkill to
    // go via IUnknown when we already have the wrapper in-hand.
    ULONG cbRef = pWrap->AddRefWithAggregationCheck();

    // 0xbadF00d implies the AddRef didn't go through
    RETURN ((cbRef != 0xbadf00d) ? pIntf : NULL);
}

struct InvokeICustomQueryInterfaceGetInterfaceArgs
{
    ComCallWrapper *pWrap;
    GUID *pGuid;
    IUnknown **ppUnk;
    CustomQueryInterfaceResult *pRetVal;
};

VOID __stdcall InvokeICustomQueryInterfaceGetInterface_CallBack(LPVOID ptr)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(ptr));
    }
    CONTRACTL_END;
    InvokeICustomQueryInterfaceGetInterfaceArgs *pArgs = (InvokeICustomQueryInterfaceGetInterfaceArgs*)ptr;

    {
        GCX_COOP();
        OBJECTREF pObj = pArgs->pWrap->GetObjectRef();

        GCPROTECT_BEGIN(pObj);

        // 1. Get MD
        MethodDesc *pMD = pArgs->pWrap->GetSimpleWrapper()->GetComCallWrapperTemplate()->GetICustomQueryInterfaceGetInterfaceMD();

        // 2. Get Object Handle
        OBJECTHANDLE hndCustomQueryInterface = pArgs->pWrap->GetObjectHandle();

        // 3 construct the MethodDescCallSite
        MethodDescCallSite GetInterface(pMD, hndCustomQueryInterface);

        ARG_SLOT Args[] = {
            ObjToArgSlot(pObj),
            PtrToArgSlot(pArgs->pGuid),
            PtrToArgSlot(pArgs->ppUnk),
            };

        *(pArgs->pRetVal) = (CustomQueryInterfaceResult)GetInterface.Call_RetArgSlot(Args);
        GCPROTECT_END();
    }
}

//--------------------------------------------------------------------------
//  check if the interface is supported, return a index into the IMap
//  returns -1, if pIntfMT is not supported
//--------------------------------------------------------------------------
static int GetIndexForIntfMT(ComCallWrapperTemplate *pTemplate, MethodTable *pIntfMT)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pTemplate));
        PRECONDITION(CheckPointer(pIntfMT));
    }
    CONTRACTL_END;

    for (ULONG j = 0; j < pTemplate->GetNumInterfaces(); j++)
    {
        ComMethodTable *pItfComMT = (ComMethodTable *)pTemplate->GetVTableSlot(j) - 1;
        if (pItfComMT->GetMethodTable()->IsEquivalentTo(pIntfMT))
            return j;
    }

    return -1;
}

static IUnknown *GetComIPFromCCW_VisibilityCheck(
    IUnknown *pIntf,
    MethodTable *pIntfMT,
    ComMethodTable *pIntfComMT,
    GetComIPFromCCW::flags flags)
{
    CONTRACT(IUnknown*)
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pIntf));
        PRECONDITION(CheckPointer(pIntfComMT));
    }
    CONTRACT_END;

    if (// Do a visibility check if needed.
        ((flags & GetComIPFromCCW::CheckVisibility) && (!pIntfComMT->IsComVisible())))
    {
        //  If not, fail to return the interface.
        SafeRelease(pIntf);
        RETURN NULL;
    }
    RETURN pIntf;
}

static IUnknown * GetComIPFromCCW_HandleExtendsCOMObject(
    ComCallWrapper * pWrap,
    REFIID riid,
    MethodTable * pIntfMT,
    ComCallWrapperTemplate * pTemplate,
    int imapIndex,
    unsigned int intfIndex)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    // If we don't implement the interface, we delegate to base
    BOOL bDelegateToBase = TRUE;
    if (imapIndex != -1)
    {
        MethodTable * pMT = pWrap->GetMethodTableOfObjectRef();
        MethodTable::InterfaceMapIterator intIt = pMT->IterateInterfaceMapFrom(intfIndex);

        // If the number of slots is 0, then no need to proceed
        MethodTable* pItf = intIt.GetInterfaceApprox();
        if (pItf->GetNumVirtuals() != 0)
        {
            MethodDesc *pClsMD = NULL;
            _ASSERTE(!pItf->HasInstantiation());

            // Find the implementation for the first slot of the interface
            DispatchSlot impl(pMT->FindDispatchSlot(pItf->GetTypeID(), 0, FALSE /* throwOnConflict */));
            CONSISTENCY_CHECK(!impl.IsNull());

            // Get the MethodDesc for this slot in the class
            pClsMD = impl.GetMethodDesc();

            MethodTable * pClsMT = pClsMD->GetMethodTable();
            bDelegateToBase = (pClsMT->IsInterface() || pClsMT->IsComImport()) ? TRUE : FALSE;
        }
        else
        {
            // The interface has no methods so we cannot override it. Because of this
            // it makes sense to delegate to the base COM component.
            bDelegateToBase = TRUE;
        }
    }

    if (bDelegateToBase)
    {
        // This is an interface of the base COM object so delegate the call to it
        SyncBlock* pBlock = pWrap->GetSyncBlock();
        _ASSERTE(pBlock);

        SafeComHolder<IUnknown> pUnk;

        RCWHolder pRCW(GetThread());
        RCWPROTECT_BEGIN(pRCW, pBlock);

        pUnk = (pIntfMT != NULL) ? pRCW->GetComIPFromRCW(pIntfMT)
                                 : pRCW->GetComIPFromRCW(riid);

        RCWPROTECT_END(pRCW);
        return pUnk.Extract();
    }

    return NULL;
}

static IUnknown * GetComIPFromCCW_ForIID_Worker(
    ComCallWrapper *pWrap,
    REFIID riid,
    MethodTable *pIntfMT,
    GetComIPFromCCW::flags flags,
    ComCallWrapperTemplate * pTemplate)
{
    CONTRACT(IUnknown*)
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pWrap));
        POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
    }
    CONTRACT_END;

    ComMethodTable * pIntfComMT = NULL;
    MethodTable * pMT = pWrap->GetMethodTableOfObjectRef();

    // At this point, it must be that the IID is one of IClassX IIDs or
    //  it isn't implemented on this class.  We'll have to search through and set
    //  up the entire hierarchy to determine which it is.
    if (IsIClassX(pMT, riid, &pIntfComMT))
    {
        // If the class that this IClassX's was generated for is marked
        // as ClassInterfaceType.AutoDual or AutoDisp,
        // then give out the IClassX IP.
        if (pIntfComMT->GetClassInterfaceType() == clsIfAutoDual || pIntfComMT->GetClassInterfaceType() == clsIfAutoDisp)
        {
            // Make sure the all the base classes of the class this IClassX corresponds to
            // are visible to COM.
            pIntfComMT->CheckParentComVisibility(FALSE);

            // Giveout IClassX of this class because the IID matches one of the IClassX in the hierarchy
            // This assumes any IClassX implementation must be derived from base class IClassX's implementation
            IUnknown * pIntf = pWrap->GetIClassXIP();
            RETURN GetComIPFromCCW_VisibilityCheck(pIntf, pIntfMT, pIntfComMT, flags);
        }
    }

    RETURN NULL;
}

static IUnknown *GetComIPFromCCW_ForIntfMT_Worker(ComCallWrapper *pWrap, MethodTable *pIntfMT, GetComIPFromCCW::flags flags)
{
    CONTRACT(IUnknown*)
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pWrap));
        POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
    }
    CONTRACT_END;

    MethodTable * pMT = pWrap->GetMethodTableOfObjectRef();

    // class method table
    if (pMT->CanCastToClass(pIntfMT))
    {
        // Make sure we're not trying to pass out a generic-based class interface
        if (pMT->HasInstantiation())
        {
            COMPlusThrow(kInvalidOperationException, IDS_EE_ATTEMPT_TO_CREATE_GENERIC_CCW);
        }

        // Retrieve the COM method table for the requested interface.
        ComCallWrapperTemplate *pIntfCCWTemplate = ComCallWrapperTemplate::GetTemplate(TypeHandle(pIntfMT));
        if (pIntfCCWTemplate->SupportsIClassX())
        {
            ComMethodTable * pIntfComMT = pIntfCCWTemplate->GetClassComMT();

            // If the class that this IClassX's was generated for is marked
            // as ClassInterfaceType.AutoDual or AutoDisp,
            // then give out the IClassX IP.
            if (pIntfComMT->GetClassInterfaceType() == clsIfAutoDual || pIntfComMT->GetClassInterfaceType() == clsIfAutoDisp)
            {
                // Make sure the all the base classes of the class this IClassX corresponds to
                // are visible to COM.
                pIntfComMT->CheckParentComVisibility(FALSE);

                // Giveout IClassX
                IUnknown * pIntf = pWrap->GetIClassXIP();
                RETURN GetComIPFromCCW_VisibilityCheck(pIntf, pIntfMT, pIntfComMT, flags);
            }
        }
    }
    RETURN NULL;
}

static bool GetComIPFromCCW_HandleCustomQI(
                ComCallWrapper * pWrap, REFIID riid, MethodTable * pIntfMT, IUnknown ** ppUnkOut)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pWrap));
    }
    CONTRACTL_END;

    // Customize QI: We call the method System.Runtime.InteropServices.ICustomQueryInterface
    //               GetInterface implemented by user to do the customized QI work.
    CustomQueryInterfaceResult retVal = Handled;

    // prepare the GUID
    GUID guid;
    if (IsEqualGUID(riid, GUID_NULL) && pIntfMT != NULL)
    {
        // riid is null, we retrieve the guid from the methodtable
        pIntfMT->GetGuid(&guid, true);
    }
    else
    {
        // copy riid to avoid user modify it
        guid = riid;
    }

    InvokeICustomQueryInterfaceGetInterfaceArgs args = {pWrap, &guid, ppUnkOut, &retVal};

    InvokeICustomQueryInterfaceGetInterface_CallBack(&args);

    // return if user already handle the QI
    if (retVal == Handled)
        return true;
    // return NULL if user wants to fail the QI
    if (retVal == Failed)
    {
        *ppUnkOut = NULL;
        return true;
    }
    // assure that user returns the known return value
    _ASSERTE(retVal == NotHandled);
    return false;
}

// A MODE_ANY helper to get the MethodTable of the 'this' object.  This helper keeps
// the GCX_COOP transition out of the caller (it implies a holder which implies an
// FS:0 handler on x86).
MethodTable * ComCallWrapper::GetMethodTableOfObjectRef()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    GCX_COOP();
    return GetObjectRef()->GetMethodTable();
}

//--------------------------------------------------------------------------
// IUnknown* ComCallWrapper::GetComIPfromCCW(ComCallWrapper *pWrap, REFIID riid, MethodTable* pIntfMT, BOOL bCheckVisibility)
// Get an interface from wrapper, based on riid or pIntfMT. The returned interface is AddRef'd.
//--------------------------------------------------------------------------
// static
IUnknown* ComCallWrapper::GetComIPFromCCW(ComCallWrapper *pWrap, REFIID riid, MethodTable* pIntfMT,
                                          GetComIPFromCCW::flags flags)
{
    CONTRACT(IUnknown*)
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pWrap));
        POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
    }
    CONTRACT_END;

    // scan the wrapper
    if (pWrap->IsLinked())
        pWrap = ComCallWrapper::GetStartWrapper(pWrap);

    ComCallWrapperTemplate *pTemplate = pWrap->GetSimpleWrapper()->GetComCallWrapperTemplate();

    // We should not be getting a CCW that represents a CCW interface as GetWrapperFromIP will always
    // convert a IP to main CCW
    _ASSERTE(!pTemplate->RepresentsVariantInterface());

    if (IsIUnknown(riid))
    {
        // We don't do visibility checks on IUnknown.
        RETURN pWrap->GetBasicIP();
    }

    if (!(flags & GetComIPFromCCW::SuppressCustomizedQueryInterface)
        && pTemplate->SupportsICustomQueryInterface())
    {
        // Customize QI: We call the method System.Runtime.InteropServices.ICustomQueryInterface
        //               GetInterface implemented by user to do the customized QI work.
        IUnknown * pUnkCustomQIResult = NULL;
        if (GetComIPFromCCW_HandleCustomQI(pWrap, riid, pIntfMT, &pUnkCustomQIResult))
            RETURN pUnkCustomQIResult;
    }

    if (IsIDispatch(riid))
    {
        // We don't do visibility checks on IUnknown.
        RETURN pWrap->GetIDispatchIP();
    }

    signed imapIndex = -1;
    if (pIntfMT == NULL)
    {
        if (IsGUID_NULL(riid))  // there's no interface with GUID_NULL IID so we can bail out right away
            RETURN NULL;

        // Go through all the implemented methods except the COM imported class interfaces
        // and compare the IID's to find the requested one.
        for (unsigned j = 0; j < pTemplate->GetNumInterfaces(); j++)
        {
            ComMethodTable *pItfComMT = (ComMethodTable *)pTemplate->GetVTableSlot(j) - 1;
            if (pItfComMT && !pItfComMT->IsComClassItf())
            {
                if (InlineIsEqualGUID(pItfComMT->GetIID(), riid))
                {
                    pIntfMT = pItfComMT->GetMethodTable();
                    imapIndex = j;
                    break;
                }
            }
        }

        if (imapIndex == -1)
        {
            // Check for the standard interfaces.
            SimpleComCallWrapper* pSimpleWrap = pWrap->GetSimpleWrapper();
            IUnknown * pIntf = pSimpleWrap->QIStandardInterface(riid);
            if (pIntf)
                RETURN pIntf;

            pIntf = GetComIPFromCCW_ForIID_Worker(pWrap, riid, pIntfMT, flags, pTemplate);
            if (pIntf)
                RETURN pIntf;
        }
    }
    else
    {
        imapIndex = GetIndexForIntfMT(pTemplate, pIntfMT);

        if (!pIntfMT->IsInterface())
        {
            IUnknown * pIntf = GetComIPFromCCW_ForIntfMT_Worker(pWrap, pIntfMT, flags);
            if (pIntf)
                RETURN pIntf;
        }
    }

    // At this point, all of the 'fast' special cases have already returned and we're
    // left with either no interface found (imapIndex == -1) or a user-code-implemented
    // interface was found ((imapIndex != -1) && (pIntfMT != NULL)).

    unsigned intfIndex = imapIndex;
    if (imapIndex != -1)
    {
        // We don't support QI calls for interfaces that have generic arguments.
        _ASSERTE(pIntfMT != NULL);
        if (pIntfMT->HasInstantiation())
        {
            COMPlusThrow(kInvalidOperationException, IDS_EE_ATTEMPT_TO_CREATE_GENERIC_CCW);
        }

        if (pIntfMT->IsInterface() && !pIntfMT->HasOnlyAbstractMethods())
        {
            COMPlusThrow(kInvalidOperationException, IDS_EE_ATTEMPT_TO_CREATE_NON_ABSTRACT_CCW);
        }

        // The first block has one slot for the IClassX vtable pointer
        //  and one slot for the basic vtable pointer.
        imapIndex += Slot_FirstInterface;
    }

    // COM plus objects that extend from COM objects are special
    if (pWrap->IsExtendsCOMObject())
    {
        IUnknown * pIntf = GetComIPFromCCW_HandleExtendsCOMObject(pWrap, riid, pIntfMT,
                                pTemplate, imapIndex, intfIndex);
        if (pIntf)
            RETURN pIntf;
    }

    // check if interface is supported
    if (imapIndex == -1)
        RETURN NULL;

    // interface method table != NULL
    _ASSERTE(pIntfMT != NULL);

    // IUnknown* loc within the wrapper
    SLOT** ppVtable = GetComIPLocInWrapper(pWrap, imapIndex);
    _ASSERTE(*ppVtable != NULL); // this should point to COM Vtable or interface vtable

    // Finish laying out the interface COM method table if it has not been done yet.
    ComMethodTable *pItfComMT = ComMethodTable::ComMethodTableFromIP((IUnknown*)ppVtable);
    if (!pItfComMT->IsLayoutComplete())
    {
        MethodTable *pClassMT = pTemplate->GetClassType().GetMethodTable();
        if (!pItfComMT->LayOutInterfaceMethodTable(pClassMT))
            RETURN NULL;
    }

    // AddRef the wrapper.
    // Note that we don't do SafeAddRef(pIntf) because it's overkill to
    // go via IUnknown when we already have the wrapper in-hand.
    ULONG cbRef = pWrap->AddRefWithAggregationCheck();

    // 0xbadF00d implies the AddRef didn't go through
    if (cbRef == 0xbadf00d)
        RETURN NULL;

    // The interface pointer is the pointer to the vtable.
    IUnknown * pIntf = (IUnknown*)ppVtable;
    // Retrieve the COM method table from the interface.
    ComMethodTable * pIntfComMT = ComMethodTable::ComMethodTableFromIP(pIntf);

    // Manual inlining of GetComIPFromCCW_VisibilityCheck() for common case.
    if (// Do a visibility check if needed.
        ((flags & GetComIPFromCCW::CheckVisibility) && (!pIntfComMT->IsComVisible())))
    {
        //  If not, fail to return the interface.
        SafeRelease(pIntf);
        pIntf = NULL;
    }
    RETURN pIntf;
}

//--------------------------------------------------------------------------
// Get the IDispatch interface pointer for the wrapper.
// The returned interface is AddRef'd.
//--------------------------------------------------------------------------
IDispatch* ComCallWrapper::GetIDispatchIP()
{
    CONTRACT (IDispatch*)
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
    }
    CONTRACT_END;

    SimpleComCallWrapper* pSimpleWrap = GetSimpleWrapper();
    MethodTable*          pMT         = pSimpleWrap->GetMethodTable();

    // Retrieve the type of the default interface for the class.
    TypeHandle hndDefItfClass;
    DefaultInterfaceType DefItfType = GetDefaultInterfaceForClassWrapper(TypeHandle(pMT), &hndDefItfClass);

    if ((DefItfType == DefaultInterfaceType_AutoDual) || (DefItfType == DefaultInterfaceType_AutoDispatch))
    {
        // Make sure we release the BasicIP we're about to get.
        SafeComHolder<IUnknown> pBasic = GetBasicIP();
        ComMethodTable* pCMT = ComMethodTable::ComMethodTableFromIP(pBasic);
        pCMT->CheckParentComVisibility(TRUE);
    }

    // If the class implements IReflect then use the IDispatchEx implementation.
    if (SimpleComCallWrapper::SupportsIReflect(pMT))
    {
        // The class implements IReflect so lets let it handle IDispatch calls.
        // We will do this by exposing the IDispatchEx implementation of IDispatch.
        RETURN (IDispatch *)pSimpleWrap->QIStandardInterface(IID_IDispatchEx);
    }

    // Get the correct default interface
    switch (DefItfType)
    {
        case DefaultInterfaceType_Explicit:
        {
            _ASSERTE(!hndDefItfClass.IsNull());
            _ASSERTE(hndDefItfClass.IsInterface());

            CorIfaceAttr ifaceType = hndDefItfClass.GetMethodTable()->GetComInterfaceType();
            if (IsDispatchBasedItf(ifaceType))
            {
                RETURN (IDispatch*)GetComIPFromCCW(this, GUID_NULL, hndDefItfClass.GetMethodTable());
            }
            else
            {
                RETURN NULL;
            }
        }

        case DefaultInterfaceType_IUnknown:
        {
            RETURN NULL;
        }

        case DefaultInterfaceType_AutoDual:
        case DefaultInterfaceType_AutoDispatch:
        {
            RETURN (IDispatch*)GetBasicIP();
        }

        case DefaultInterfaceType_BaseComClass:
        {
            SyncBlock* pBlock = GetSyncBlock();
            _ASSERTE(pBlock);

            SafeComHolder<IDispatch> pDisp;

            RCWHolder pRCW(GetThread());
            RCWPROTECT_BEGIN(pRCW, pBlock);

            pDisp = pRCW->GetIDispatch();

            RCWPROTECT_END(pRCW);
            RETURN pDisp.Extract();
        }

        default:
        {
            _ASSERTE(!"Invalid default interface type!");
            RETURN NULL;
        }
    }
}

//--------------------------------------------------------------------------
// ComCallable wrapper manager
// constructor
//--------------------------------------------------------------------------
ComCallWrapperCache::ComCallWrapperCache() :
    m_cbRef(0),
    m_pCacheLineAllocator(NULL),
    m_pLoaderAllocator(NULL),
    m_lock(CrstCOMWrapperCache)
{
    WRAPPER_NO_CONTRACT;

}

//-------------------------------------------------------------------
// ComCallable wrapper manager
// destructor
//-------------------------------------------------------------------
ComCallWrapperCache::~ComCallWrapperCache()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    LOG((LF_INTEROP, LL_INFO100, "ComCallWrapperCache::~ComCallWrapperCache %8.8x in loader allocator [%d] %8.8x\n",
            this, GetLoaderAllocator() ? GetLoaderAllocator()->GetCreationNumber() : 0, GetLoaderAllocator()));

    if (m_pCacheLineAllocator)
    {
        delete m_pCacheLineAllocator;
        m_pCacheLineAllocator = NULL;
    }

    LoaderAllocator *pLoaderAllocator = GetLoaderAllocator();   // don't use member directly, need to mask off flags
    if (pLoaderAllocator)
    {
        // clear hook in LoaderAllocator as we're going away
        pLoaderAllocator->ResetComCallWrapperCache();
    }
}


//-------------------------------------------------------------------
// ComCallable wrapper manager
// Create/Init method
//-------------------------------------------------------------------
ComCallWrapperCache *ComCallWrapperCache::Create(LoaderAllocator *pLoaderAllocator)
{
    CONTRACT (ComCallWrapperCache*)
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM());
        PRECONDITION(CheckPointer(pLoaderAllocator));
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END;

    NewHolder<ComCallWrapperCache> pWrapperCache = new ComCallWrapperCache();

    LOG((LF_INTEROP, LL_INFO100, "ComCallWrapperCache::Create %8.8x in loader allocator [%d] %8.8x\n",
        (ComCallWrapperCache *)pWrapperCache, pLoaderAllocator ? pLoaderAllocator->GetCreationNumber() : 0, pLoaderAllocator));

    NewHolder<CCacheLineAllocator> line = new CCacheLineAllocator;

    pWrapperCache->m_pLoaderAllocator = pLoaderAllocator;
    pWrapperCache->m_pCacheLineAllocator = line;

    pWrapperCache->AddRef();

    line.SuppressRelease();
    pWrapperCache.SuppressRelease();
    RETURN pWrapperCache;
}

//-------------------------------------------------------------------
// ComCallable wrapper manager
// LONG AddRef()
//-------------------------------------------------------------------
LONG ComCallWrapperCache::AddRef()
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    LONG i = FastInterlockIncrement(&m_cbRef);
    LOG((LF_INTEROP, LL_INFO100, "ComCallWrapperCache::Addref %8.8x with %d in loader allocator [%d] %8.8x\n",
        this, i, GetLoaderAllocator()?GetLoaderAllocator()->GetCreationNumber() : 0, GetLoaderAllocator()));

    return i;
}

//-------------------------------------------------------------------
// ComCallable wrapper manager
// LONG Release()
//-------------------------------------------------------------------
LONG ComCallWrapperCache::Release()
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    LONG i = FastInterlockDecrement(&m_cbRef);
    _ASSERTE(i >= 0);

    LOG((LF_INTEROP, LL_INFO100, "ComCallWrapperCache::Release %8.8x with %d in loader allocator [%d] %8.8x\n",
        this, i, GetLoaderAllocator() ? GetLoaderAllocator()->GetCreationNumber() : 0, GetLoaderAllocator()));
    if ( i == 0)
        delete this;

    return i;
}






//--------------------------------------------------------------------------
// void ComMethodTable::Cleanup()
// free the stubs and the vtable
//--------------------------------------------------------------------------
void ComMethodTable::Cleanup()
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    unsigned cbExtraSlots = GetNumExtraSlots(GetInterfaceType());
    unsigned cbSlots = m_cbSlots;

    SLOT* pComVtable = (SLOT *)(this + 1);

    // If we have created and laid out the method desc then we need to delete them.
    if (IsLayoutComplete())
    {
#ifdef PROFILING_SUPPORTED
        // We used to issue the COMClassicVTableDestroyed callback from here.
        // However, that causes an AV.  At this point the MethodTable is gone
        // (as the AppDomain containing it has been unloaded), but the ComMethodTable
        // still points to it.  The code here used to wrap a TypeHandle around the
        // MethodTable pointer, cast to a ClassID, and then call COMClassicVTableDestroyed.
        // But the act of casting to a TypeHandle invokes debug-code to verify the
        // MethodTable, which causes an AV.
        //
        // For now, we're not issuing the COMClassicVTableDestroyed callback anymore.
        // <REVISIT_TODO>Reexamine the profiling API around
        // CCWs and move the callback elsewhere and / or rethink the current
        // set of CCW callbacks to mirror reality more accurately.</REVISIT_TODO>
#endif // PROFILING_SUPPORTED

        for (unsigned i = cbExtraSlots; i < cbSlots+cbExtraSlots; i++)
        {
            // Don't bother grabbing the ComCallMethodDesc if the method represented by the
            // current vtable slot doesn't belong to the current ComMethodTable.
            if (!OwnedbyThisMT(i))
            {
                continue;
            }

            // ComCallMethodDescFromSlot returns NULL when the
            // ComCallMethodDesc has already been cleaned up.
            ComCallMethodDesc* pCMD = ComCallMethodDescFromSlot(i);
            if ( (pComVtable[i] == (SLOT)-1 ) ||
                 (pCMD == NULL)
               )
            {
                continue;
            }

            // All the stubs that are in a COM->COM+ VTable are to the generic
            // helpers (g_pGenericComCallStubFields, etc.).  So all we do is
            // discard the resources held by the ComMethodDesc.
            pCMD->Destruct();
        }
    }

    if (m_pDispatchInfo)
        delete m_pDispatchInfo;
    if (m_pMDescr)
        DeleteExecutable(m_pMDescr);
    if (m_pITypeInfo && !g_fProcessDetach)
        SafeRelease(m_pITypeInfo);

    DeleteExecutable(this);
}


//--------------------------------------------------------------------------
// Lay's out the members of a ComMethodTable that represents an IClassX.
//--------------------------------------------------------------------------
void ComMethodTable::LayOutClassMethodTable()
{
    CONTRACTL
    {
        PRECONDITION(m_pMT != NULL);
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM());
    }
    CONTRACTL_END;

    GCX_PREEMP();

    unsigned i;
    IDispatchVtable* pDispVtable;
    SLOT *pComVtable;
    unsigned cbPrevSlots = 0;
    unsigned cbAlloc = 0;
    NewExecutableHolder<BYTE>  pMDMemoryPtr = NULL;
    BYTE*  pMethodDescMemory = NULL;
    size_t writeableOffset = 0;
    unsigned cbNumParentVirtualMethods = 0;
    unsigned cbTotalParentFields = 0;
    unsigned cbParentComMTSlots = 0;
    MethodTable* pComPlusParentClass = m_pMT->GetComPlusParentMethodTable();
    MethodTable* pParentClass = m_pMT->GetParentMethodTable();
    MethodTable* pCurrParentClass = pParentClass;
    MethodTable* pCurrMT = m_pMT;
    InteropMethodTableData *pCurrParentInteropMT = NULL;
    InteropMethodTableData *pCurrInteropMT = NULL;
    ComMethodTable* pParentComMT = NULL;
    const unsigned cbExtraSlots = GetNumExtraSlots(ifDual);
    CQuickEEClassPtrs apClassesToProcess;
    int cClassesToProcess = 0;

    //
    // If we have a parent ensure its IClassX COM method table is laid out.
    //

    if (pComPlusParentClass)
    {
        pParentComMT = ComCallWrapperTemplate::SetupComMethodTableForClass(pComPlusParentClass, TRUE);
        cbParentComMTSlots = pParentComMT->m_cbSlots;
    }

    LOG((LF_INTEROP, LL_INFO1000, "LayOutClassMethodTable: %s, parent: %s, this: %p\n", m_pMT->GetDebugClassName(), pParentClass ? pParentClass->GetDebugClassName() : 0, this));

    //
    // Allocate a temporary space to generate the vtable into.
    //

    S_UINT32 cbTempVtable = (S_UINT32(m_cbSlots) + S_UINT32(cbExtraSlots)) * S_UINT32(sizeof(SLOT));

    if (cbTempVtable.IsOverflow())
        ThrowHR(COR_E_OVERFLOW);

    NewArrayHolder<BYTE> pTempVtable = new BYTE[cbTempVtable.Value()];
    pDispVtable = (IDispatchVtable *)pTempVtable.GetValue();

    //
    // Set up the IUnknown and IDispatch methods.
    //

    // Setup IUnknown vtable
    pDispVtable->m_qi      = (SLOT)Unknown_QueryInterface;
    pDispVtable->m_addref  = (SLOT)Unknown_AddRef;
    pDispVtable->m_release = (SLOT)Unknown_Release;


    // Set up the common portion of the IDispatch vtable.
    pDispVtable->m_GetTypeInfoCount = (SLOT)Dispatch_GetTypeInfoCount_Wrapper;
    pDispVtable->m_GetTypeInfo      = (SLOT)Dispatch_GetTypeInfo_Wrapper;

    // If the class interface is a pure disp interface then we need to use the
    // internal implementation of IDispatch for GetIdsOfNames and Invoke.
    if (GetClassInterfaceType() == clsIfAutoDisp)
    {
        // Use the internal implementation.
        pDispVtable->m_GetIDsOfNames = (SLOT)InternalDispatchImpl_GetIDsOfNames_Wrapper;
        pDispVtable->m_Invoke = (SLOT)InternalDispatchImpl_Invoke_Wrapper;
    }
    else
    {
        // We need to set the entry points to the Dispatch versions which determine
        // which implementation to use at runtime based on the class that implements
        // the interface.
        pDispVtable->m_GetIDsOfNames = (SLOT)Dispatch_GetIDsOfNames_Wrapper;
        pDispVtable->m_Invoke = (SLOT)Dispatch_Invoke_Wrapper;
    }


    //
    // Lay out the portion of the vtable containing the methods of the class.
    //
    // Note that we only do this if the class doesn't have any generic instantiations
    // in it's hierarchy.
    //
    ArrayList NewCOMMethodDescs;
    ComCallMethodDescArrayHolder NewCOMMethodDescsHolder(&NewCOMMethodDescs);

    unsigned cbNewSlots = 0;

    //
    // Copy the members down from our parent's template
    // We guarantee to have at least all the slots from parent's template
    //

    pComVtable = ((SLOT*)pDispVtable) + cbExtraSlots;
    if (pParentComMT)
    {
        SLOT *pPrevComVtable = ((SLOT *)(pParentComMT + 1)) + cbExtraSlots;
        CopyMemory(pComVtable, pPrevComVtable, sizeof(SLOT) * cbParentComMTSlots);
        cbPrevSlots = cbParentComMTSlots;
    }

    if (!m_pMT->HasGenericClassInstantiationInHierarchy())
    {
        ExecutableWriterHolder<BYTE> methodDescMemoryWriteableHolder;
        //
        // Allocate method desc's for the rest of the slots.
        //
        unsigned cbMethodDescs = (COMMETHOD_PREPAD + sizeof(ComCallMethodDesc)) * (m_cbSlots - cbParentComMTSlots);
        cbAlloc = cbMethodDescs;
        if (cbAlloc > 0)
        {
            pMDMemoryPtr = (BYTE*) new (executable) BYTE[cbAlloc + sizeof(UINT_PTR)];
            pMethodDescMemory = pMDMemoryPtr;

            methodDescMemoryWriteableHolder = ExecutableWriterHolder<BYTE>(pMethodDescMemory, cbAlloc + sizeof(UINT_PTR));
            writeableOffset = methodDescMemoryWriteableHolder.GetRW() - pMethodDescMemory;

            // initialize the method desc memory to zero
            FillMemory(pMethodDescMemory + writeableOffset, cbAlloc, 0x0);

            *(UINT_PTR *)(pMethodDescMemory + writeableOffset) = cbMethodDescs; // fill in the size of the method desc's

            // move past the size
            pMethodDescMemory += sizeof(UINT_PTR);
        }

        _ASSERTE(0 == (((DWORD_PTR)pMethodDescMemory) & (sizeof(void*)-1)));

        //
        // Create an array of all the classes that need to be laid out.
        //

        do
        {
            apClassesToProcess.ReSizeThrows(cClassesToProcess + 2);
            apClassesToProcess[cClassesToProcess++] = pCurrMT;
            pCurrMT = pCurrMT->GetParentMethodTable();
        }
        while (pCurrMT != pComPlusParentClass);
        apClassesToProcess[cClassesToProcess++] = pCurrMT;

        //
        // Set up the COM call method desc's for all the methods and fields that were introduced
        // between the current class and its parent COM+ class. This includes any methods on
        // COM classes.
        //
        for (cClassesToProcess -= 2; cClassesToProcess >= 0; cClassesToProcess--)
        {
            //
            // Retrieve the current class and the current parent class.
            //

            pCurrMT = apClassesToProcess[cClassesToProcess];
            pCurrInteropMT = pCurrMT->GetComInteropData();
            _ASSERTE(pCurrInteropMT);

            pCurrParentClass = apClassesToProcess[cClassesToProcess + 1];


            //
            // Retrieve the number of fields and vtable methods on the parent class.
            //

            if (pCurrParentClass)
            {
                cbTotalParentFields = pCurrParentClass->GetNumInstanceFields();
                pCurrParentInteropMT = pCurrParentClass->GetComInteropData();
                _ASSERTE(pCurrParentInteropMT);
                cbNumParentVirtualMethods = pCurrParentInteropMT->cVTable;
            }


            //
            // Set up the COM call method desc's for methods that were not public in the parent class
            // but were made public in the current class.
            //

            for (i = 0; i < cbNumParentVirtualMethods; i++)
            {
                MethodDesc* pMD = NULL;
                InteropMethodTableSlotData *pCurrInteropMD = NULL;
                pCurrInteropMD = &pCurrInteropMT->pVTable[i];
                pMD = pCurrInteropMD->pMD;
                MethodDesc* pParentMD = NULL;
                InteropMethodTableSlotData *pCurrParentInteropMD = NULL;
                pCurrParentInteropMD = &pCurrParentInteropMT->pVTable[i];
                pParentMD = pCurrParentInteropMD->pMD;

                if (pMD &&
                        !(pCurrInteropMD ? IsDuplicateClassItfMD(pCurrInteropMD, i) : IsDuplicateClassItfMD(pMD, i)) &&
                    IsOverloadedComVisibleMember(pMD, pParentMD))
                {
                    // some bytes are reserved for CALL xxx before the method desc
                    ComCallMethodDesc* pNewMD = (ComCallMethodDesc *) (pMethodDescMemory + COMMETHOD_PREPAD);
                    ComCallMethodDesc* pNewMDRW = (ComCallMethodDesc *) (pMethodDescMemory + writeableOffset + COMMETHOD_PREPAD);
                    NewCOMMethodDescs.Append(pNewMD);

                    pNewMDRW->InitMethod(pMD, NULL);

                    emitCOMStubCall(pNewMD, pNewMDRW, GetEEFuncEntryPoint(ComCallPreStub));

                    FillInComVtableSlot(pComVtable, cbPrevSlots++, pNewMD);

                    pMethodDescMemory += (COMMETHOD_PREPAD + sizeof(ComCallMethodDesc));
                }
            }


            //
            // Set up the COM call method desc's for all newly introduced public methods.
            //

            unsigned cbNumVirtualMethods = 0;
            cbNumVirtualMethods = pCurrInteropMT->cVTable;
            for (i = cbNumParentVirtualMethods; i < cbNumVirtualMethods; i++)
            {
                MethodDesc* pMD = NULL;
                InteropMethodTableSlotData *pCurrInteropMD = NULL;
                pCurrInteropMD = &pCurrInteropMT->pVTable[i];
                pMD = pCurrInteropMD->pMD;

                if (pMD &&
                        !(pCurrInteropMD ? IsDuplicateClassItfMD(pCurrInteropMD, i) : IsDuplicateClassItfMD(pMD, i)) &&
                    IsNewComVisibleMember(pMD))
                {
                    // some bytes are reserved for CALL xxx before the method desc
                    ComCallMethodDesc* pNewMD = (ComCallMethodDesc *) (pMethodDescMemory + COMMETHOD_PREPAD);
                    ComCallMethodDesc* pNewMDRW = (ComCallMethodDesc *) (pMethodDescMemory + writeableOffset + COMMETHOD_PREPAD);
                    NewCOMMethodDescs.Append(pNewMD);

                    pNewMDRW->InitMethod(pMD, NULL);

                    emitCOMStubCall(pNewMD, pNewMDRW, GetEEFuncEntryPoint(ComCallPreStub));

                    FillInComVtableSlot(pComVtable, cbPrevSlots++, pNewMD);

                    pMethodDescMemory += (COMMETHOD_PREPAD + sizeof(ComCallMethodDesc));
                }
            }


            //
            // Add the non virtual methods introduced on the current class.
            //

            MethodTable::MethodIterator it(pCurrMT);
            for (; it.IsValid(); it.Next())
            {
                if (!it.IsVirtual()) {
                    MethodDesc* pMD = it.GetMethodDesc();

                    if (pMD != NULL && !IsDuplicateClassItfMD(pMD, it.GetSlotNumber()) &&
                        IsNewComVisibleMember(pMD) && !pMD->IsStatic() && !pMD->IsCtor()
                        && (!pCurrMT->IsValueType() || (GetClassInterfaceType() != clsIfAutoDual && IsStrictlyUnboxed(pMD))))
                    {
                        // some bytes are reserved for CALL xxx before the method desc
                        ComCallMethodDesc* pNewMD = (ComCallMethodDesc *) (pMethodDescMemory + COMMETHOD_PREPAD);
                        ComCallMethodDesc* pNewMDRW = (ComCallMethodDesc *) (pMethodDescMemory + writeableOffset + COMMETHOD_PREPAD);

                        NewCOMMethodDescs.Append(pNewMD);

                        pNewMDRW->InitMethod(pMD, NULL);

                        emitCOMStubCall(pNewMD, pNewMDRW, GetEEFuncEntryPoint(ComCallPreStub));

                        FillInComVtableSlot(pComVtable, cbPrevSlots++, pNewMD);

                        pMethodDescMemory += (COMMETHOD_PREPAD + sizeof(ComCallMethodDesc));
                    }
                }
            }


            //
            // Set up the COM call method desc's for the public fields defined in the current class.
            //

            // <TODO>check this approximation - we may be losing exact type information </TODO>
            ApproxFieldDescIterator fdIterator(pCurrMT, ApproxFieldDescIterator::INSTANCE_FIELDS);
            FieldDesc* pFD = NULL;
            while ((pFD = fdIterator.Next()) != NULL)
            {
                if (IsMemberVisibleFromCom(pCurrMT, pFD->GetMemberDef(), mdTokenNil)) // if it is a public field grab it
                {
                    // set up a getter method
                    // some bytes are reserved for CALL xxx before the method desc
                    ComCallMethodDesc* pNewMD = (ComCallMethodDesc *) (pMethodDescMemory + COMMETHOD_PREPAD);
                    ComCallMethodDesc* pNewMDRW = (ComCallMethodDesc *) (pMethodDescMemory + writeableOffset + COMMETHOD_PREPAD);
                    NewCOMMethodDescs.Append(pNewMD);

                    pNewMDRW->InitField(pFD, TRUE);

                    emitCOMStubCall(pNewMD, pNewMDRW, GetEEFuncEntryPoint(ComCallPreStub));

                    FillInComVtableSlot(pComVtable, cbPrevSlots++, pNewMD);

                    pMethodDescMemory+= (COMMETHOD_PREPAD + sizeof(ComCallMethodDesc));

                    // setup a setter method
                    // some bytes are reserved for CALL xxx before the method desc
                    pNewMD = (ComCallMethodDesc *) (pMethodDescMemory + COMMETHOD_PREPAD);
                    pNewMDRW = (ComCallMethodDesc *) (pMethodDescMemory + writeableOffset + COMMETHOD_PREPAD);
                    NewCOMMethodDescs.Append(pNewMD);

                    pNewMDRW->InitField(pFD, FALSE);

                    emitCOMStubCall(pNewMD, pNewMDRW, GetEEFuncEntryPoint(ComCallPreStub));

                    FillInComVtableSlot(pComVtable, cbPrevSlots++, pNewMD);

                    pMethodDescMemory+= (COMMETHOD_PREPAD + sizeof(ComCallMethodDesc));
                }
            }
        }
    }
    _ASSERTE(m_cbSlots == cbPrevSlots);

    {
        // Take the lock and copy data from the temporary vtable to this instance
        CrstHolder ch(&g_CreateWrapperTemplateCrst);

        if (IsLayoutComplete())
            return;

        ExecutableWriterHolder<ComMethodTable> comMTWriterHolder(this, sizeof(ComMethodTable) + cbTempVtable.Value());

        // IDispatch vtable follows the header
        CopyMemory(comMTWriterHolder.GetRW() + 1, pDispVtable, cbTempVtable.Value());

        // Set the layout complete flag and release the lock.
        comMTWriterHolder.GetRW()->m_Flags |= enum_LayoutComplete;

        // We've successfully laid out the class method table so we need to suppress the release of the
        // memory for the ComCallMethodDescs and store it inside the ComMethodTable so we can
        // release it when we clean up the ComMethodTable.
        comMTWriterHolder.GetRW()->m_pMDescr = (BYTE*)pMDMemoryPtr;
        pMDMemoryPtr.SuppressRelease();
        NewCOMMethodDescsHolder.SuppressRelease();
    }

    LOG((LF_INTEROP, LL_INFO1000, "LayOutClassMethodTable: %s, parent: %s, this: %p  [DONE]\n", m_pMT->GetDebugClassName(), pParentClass ? pParentClass->GetDebugClassName() : 0, this));
}

//--------------------------------------------------------------------------
// Lay out the members of a ComMethodTable that represents an interface.
//--------------------------------------------------------------------------
BOOL ComMethodTable::LayOutInterfaceMethodTable(MethodTable* pClsMT)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pClsMT, NULL_OK));
        PRECONDITION(pClsMT == NULL || !pClsMT->IsInterface());
    }
    CONTRACTL_END;

    GCX_PREEMP();

    MethodTable *pItfClass = m_pMT;
    CorIfaceAttr ItfType = m_pMT->GetComInterfaceType();
    ULONG cbExtraSlots = GetNumExtraSlots(ItfType);

    BYTE *pMethodDescMemory = NULL;
    IUnkVtable* pUnkVtable;
    SLOT *pComVtable;
    unsigned i;


    LOG((LF_INTEROP, LL_INFO1000, "LayOutInterfaceMethodTable: %s, this: %p\n", pItfClass->GetDebugClassName(), this));

    unsigned cbSlots = pItfClass->GetNumVirtuals();

    //
    // Allocate a temporary space to generate the vtable into.
    //
    S_UINT32 cbTempVtable = (S_UINT32(m_cbSlots) + S_UINT32(cbExtraSlots)) * S_UINT32(sizeof(SLOT));
    cbTempVtable += S_UINT32(cbSlots) * S_UINT32((COMMETHOD_PREPAD + sizeof(ComCallMethodDesc)));

    if (cbTempVtable.IsOverflow())
        ThrowHR(COR_E_OVERFLOW);

    NewArrayHolder<BYTE> pTempVtable = new BYTE[cbTempVtable.Value()];

    pUnkVtable = (IUnkVtable *)pTempVtable.GetValue();
    pComVtable = ((SLOT*)pUnkVtable) + cbExtraSlots;

    // Set all vtable slots to -1 for sparse vtables. That way we catch attempts
    // to access empty slots quickly and, during cleanup, we can tell empty
    // slots from full ones.
    if (m_pMT->IsSparseForCOMInterop())
        memset(pUnkVtable + cbExtraSlots, -1, m_cbSlots * sizeof(SLOT));

    // Method descs are at the end of the vtable
    // m_cbSlots interfaces methods + IUnk methods
    pMethodDescMemory = (BYTE *)&pComVtable[m_cbSlots];

    // Setup IUnk vtable
    pUnkVtable->m_qi        = (SLOT)Unknown_QueryInterface;
    pUnkVtable->m_addref    = (SLOT)Unknown_AddRef;
    pUnkVtable->m_release   = (SLOT)Unknown_Release;

    if (IsDispatchBasedItf(ItfType))
    {
        // Setup the IDispatch vtable.
        IDispatchVtable* pDispVtable = (IDispatchVtable*)pUnkVtable;

        // Set up the common portion of the IDispatch vtable.
        pDispVtable->m_GetTypeInfoCount     = (SLOT)Dispatch_GetTypeInfoCount_Wrapper;
        pDispVtable->m_GetTypeInfo          = (SLOT)Dispatch_GetTypeInfo_Wrapper;

        // If the interface is a pure disp interface then we need to use the internal
        // implementation since OleAut does not support invoking on pure disp interfaces.
        if (ItfType == ifDispatch)
        {
            // Use the internal implementation.
            pDispVtable->m_GetIDsOfNames    = (SLOT)InternalDispatchImpl_GetIDsOfNames_Wrapper;
            pDispVtable->m_Invoke           = (SLOT)InternalDispatchImpl_Invoke_Wrapper;
        }
        else
        {
            // We need to set the entry points to the Dispatch versions which determine
            // which implmentation to use at runtime based on the class that implements
            // the interface.
            pDispVtable->m_GetIDsOfNames    = (SLOT)Dispatch_GetIDsOfNames_Wrapper;
            pDispVtable->m_Invoke           = (SLOT)Dispatch_Invoke_Wrapper;
        }
    }

    ArrayList NewCOMMethodDescs;
    ComCallMethodDescArrayHolder NewCOMMethodDescsHolder(&NewCOMMethodDescs);

    for (i = 0; i < cbSlots; i++)
    {
        // Some space for a CALL xx xx xx xx stub is reserved before the beginning of the MethodDesc
        ComCallMethodDesc* pNewMD = (ComCallMethodDesc *) (pMethodDescMemory + COMMETHOD_PREPAD);
        NewCOMMethodDescs.Append(pNewMD);

        MethodDesc* pIntfMD = m_pMT->GetMethodDescForSlot(i);

        if (m_pMT->HasInstantiation())
        {
            pIntfMD = MethodDesc::FindOrCreateAssociatedMethodDesc(
                pIntfMD,
                m_pMT,
                FALSE,           // forceBoxedEntryPoint
                Instantiation(), // methodInst
                FALSE,           // allowInstParam
                TRUE);           // forceRemotableMethod
        }

        MethodDesc *pClassMD = NULL;
        if (pClsMT != NULL)
        {
            DispatchSlot impl(pClsMT->FindDispatchSlotForInterfaceMD(pIntfMD, FALSE /* throwOnConflict */));
            pClassMD = impl.GetMethodDesc();
        }

        if (pClassMD != NULL)
        {
            pNewMD->InitMethod(pClassMD, pIntfMD);
        }
        else
        {
            // we will perform interface dispatch at run-time
            pNewMD->InitMethod(pIntfMD, NULL);
        }

        pMethodDescMemory += (COMMETHOD_PREPAD + sizeof(ComCallMethodDesc));
    }

    {
        // Take the lock and copy data from the temporary vtable to this instance
        CrstHolder ch(&g_CreateWrapperTemplateCrst);

        if (IsLayoutComplete())
            return TRUE;

        ExecutableWriterHolder<ComMethodTable> comMTWriterHolder(this, sizeof(ComMethodTable) + cbTempVtable.Value());
        size_t writeableOffset = (BYTE*)comMTWriterHolder.GetRW() - (BYTE*)this;

        // IUnk vtable follows the header
        CopyMemory(comMTWriterHolder.GetRW() + 1, pUnkVtable, cbTempVtable.Value());

        // Finish by emitting stubs and initializing the slots
        pUnkVtable = (IUnkVtable *)(this + 1);
        pComVtable = ((SLOT*)pUnkVtable) + cbExtraSlots;
        SLOT *pComVtableRW = (SLOT*)((BYTE*)pComVtable + writeableOffset);

        // Method descs are at the end of the vtable
        // m_cbSlots interfaces methods + IUnk methods
        pMethodDescMemory = (BYTE *)&pComVtable[m_cbSlots];

        for (i = 0; i < cbSlots; i++)
        {
            ComCallMethodDesc* pNewMD = (ComCallMethodDesc *) (pMethodDescMemory + COMMETHOD_PREPAD);
            ComCallMethodDesc* pNewMDRW = (ComCallMethodDesc *) (pMethodDescMemory + writeableOffset + COMMETHOD_PREPAD);

            MethodDesc* pIntfMD  = m_pMT->GetMethodDescForSlot(i);

            emitCOMStubCall(pNewMD, pNewMDRW, GetEEFuncEntryPoint(ComCallPreStub));

            UINT slotIndex = (pIntfMD->GetComSlot() - cbExtraSlots);
            FillInComVtableSlot(pComVtableRW, slotIndex, pNewMD);

            pMethodDescMemory += (COMMETHOD_PREPAD + sizeof(ComCallMethodDesc));
        }

        // Set the layout complete flag and release the lock.
        comMTWriterHolder.GetRW()->m_Flags |= enum_LayoutComplete;
        NewCOMMethodDescsHolder.SuppressRelease();
    }

#ifdef PROFILING_SUPPORTED
    // Notify profiler of the CCW, so it can avoid double-counting.
    {
        BEGIN_PROFILER_CALLBACK(CORProfilerTrackCCW());
#if defined(_DEBUG)
        WCHAR rIID[40]; // {00000000-0000-0000-0000-000000000000}
        GuidToLPWSTR(m_IID, rIID, lengthof(rIID));
        LOG((LF_CORPROF, LL_INFO100, "COMClassicVTableCreated Class:%hs, IID:%ls, vTbl:%#08x\n",
             pItfClass->GetDebugClassName(), rIID, pUnkVtable));
#else
        LOG((LF_CORPROF, LL_INFO100, "COMClassicVTableCreated Class:%#x, IID:{%08x-...}, vTbl:%#08x\n",
             pItfClass, m_IID.Data1, pUnkVtable));
#endif
        (&g_profControlBlock)->COMClassicVTableCreated((ClassID) TypeHandle(pItfClass).AsPtr(),
                                                                   m_IID,
                                                                   pUnkVtable,
                                                                   m_cbSlots+cbExtraSlots);
        END_PROFILER_CALLBACK();
   }
#endif // PROFILING_SUPPORTED

    LOG((LF_INTEROP, LL_INFO1000, "LayOutInterfaceMethodTable: %s, this: %p [DONE]\n", pItfClass->GetDebugClassName(), this));

    return TRUE;
}

void ComMethodTable::LayOutBasicMethodTable()
{
    CONTRACTL
    {
        PRECONDITION(m_pMT != NULL);
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM());
    }
    CONTRACTL_END;


    IDispatchVtable* pDispVtable;

    LOG((LF_INTEROP, LL_INFO1000, "LayOutBasicMethodTable: %s, this: %p\n", m_pMT->GetDebugClassName(), this));

    //
    // Set up the IUnknown and IDispatch methods. Each thread will write exactly the same values to the
    // slots so we let it run concurrently and execute a memory barrier by doing InterlockOr at the end.
    //

    // IDispatch vtable follows the header
    pDispVtable = (IDispatchVtable*)(this + 1);

    // Setup IUnknown vtable
    pDispVtable->m_qi      = (SLOT)Unknown_QueryInterface;
    pDispVtable->m_addref  = (SLOT)Unknown_AddRef;
    pDispVtable->m_release = (SLOT)Unknown_Release;


    // Set up the common portion of the IDispatch vtable.
    pDispVtable->m_GetTypeInfoCount = (SLOT)Dispatch_GetTypeInfoCount_Wrapper;
    pDispVtable->m_GetTypeInfo      = (SLOT)Dispatch_GetTypeInfo_Wrapper;

    // If the class interface is a pure disp interface then we need to use the
    // internal implementation of IDispatch for GetIdsOfNames and Invoke.
    if (GetClassInterfaceType() == clsIfAutoDisp)
    {
        // Use the internal implementation.
        pDispVtable->m_GetIDsOfNames = (SLOT)InternalDispatchImpl_GetIDsOfNames_Wrapper;
        pDispVtable->m_Invoke = (SLOT)InternalDispatchImpl_Invoke_Wrapper;
    }
    else
    {
        // We need to set the entry points to the Dispatch versions which determine
        // which implementation to use at runtime based on the class that implements
        // the interface.
        pDispVtable->m_GetIDsOfNames = (SLOT)Dispatch_GetIDsOfNames_Wrapper;
        pDispVtable->m_Invoke = (SLOT)Dispatch_Invoke_Wrapper;
    }

    //
    // Set the layout complete flag.
    //
    FastInterlockOr((DWORD *)&m_Flags, enum_LayoutComplete);

    LOG((LF_INTEROP, LL_INFO1000, "LayOutClassMethodTable: %s, this: %p  [DONE]\n", m_pMT->GetDebugClassName(), this));
}

//--------------------------------------------------------------------------
// Retrieves the DispatchInfo associated with the COM method table. If
// the DispatchInfo has not been initialized yet then it is initilized.
//--------------------------------------------------------------------------
DispatchInfo *ComMethodTable::GetDispatchInfo()
{
    CONTRACT (DispatchInfo*)
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM());
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END;

    if (!m_pDispatchInfo)
    {
        // Create the DispatchInfo object.
        NewHolder<DispatchInfo> pDispInfo = new DispatchInfo(m_pMT);

        // Synchronize the DispatchInfo with the actual object.
        pDispInfo->SynchWithManagedView();

        ExecutableWriterHolder<ComMethodTable> comMTWriterHolder(this, sizeof(ComMethodTable));
        // Swap the lock into the class member in a thread safe manner.
        if (NULL == FastInterlockCompareExchangePointer(&comMTWriterHolder.GetRW()->m_pDispatchInfo, pDispInfo.GetValue(), NULL))
            pDispInfo.SuppressRelease();

    }

    RETURN m_pDispatchInfo;
}

//--------------------------------------------------------------------------
// Set an ITypeInfo pointer for the method table.
//--------------------------------------------------------------------------
void ComMethodTable::SetITypeInfo(ITypeInfo *pNew)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pNew));
    }
    CONTRACTL_END;

    if (InterlockedCompareExchangeT(&m_pITypeInfo, pNew, NULL) == NULL)
    {
        SafeAddRef(pNew);
    }
}

//--------------------------------------------------------------------------
// Return the parent ComMethodTable.
//--------------------------------------------------------------------------
ComMethodTable *ComMethodTable::GetParentClassComMT()
{
    CONTRACT (ComMethodTable*)
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(IsIClassX());
        POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
    }
    CONTRACT_END;

    MethodTable *pParentComPlusMT = m_pMT->GetComPlusParentMethodTable();
    if (!pParentComPlusMT)
        RETURN NULL;

    ComCallWrapperTemplate *pTemplate = pParentComPlusMT->GetComCallWrapperTemplate();
    if (!pTemplate)
        RETURN NULL;

    RETURN pTemplate->GetClassComMT();
}

//---------------------------------------------------------
// ComCallWrapperTemplate::IIDToInterfaceTemplateCache
//---------------------------------------------------------

// Perf critical cache lookup code, in particular we want InlineIsEqualGUID to be inlined.
#include <optsmallperfcritical.h>

// Looks up an interface template in the cache.
bool ComCallWrapperTemplate::IIDToInterfaceTemplateCache::LookupInterfaceTemplate(REFIID riid, ComCallWrapperTemplate **ppTemplate)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    SpinLock::Holder lock(&m_lock);

    for (SIZE_T i = 0; i < CACHE_SIZE; i++)
    {
        // is the item in use?
        if (!m_items[i].IsFree())
        {
            // does the IID match?
            if (InlineIsEqualGUID(m_items[i].m_iid, riid))
            {
                // mark the item as hot to help avoid eviction
                m_items[i].MarkHot();
                *ppTemplate = m_items[i].GetTemplate();
                return true;
            }
        }
    }

    *ppTemplate = NULL;
    return false;
}

#include <optdefault.h>

// Inserts an interface template in the cache. If the cache is full and an item needs to be evicted,
// it tries to find one that hasn't been recently used.
void ComCallWrapperTemplate::IIDToInterfaceTemplateCache::InsertInterfaceTemplate(REFIID riid, ComCallWrapperTemplate *pTemplate)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    SpinLock::Holder lock(&m_lock);

    for (SIZE_T i = 0; i < CACHE_SIZE; i++)
    {
        // is the item free?
        if (m_items[i].IsFree())
        {
            m_items[i].m_iid = riid;
            m_items[i].SetTemplate(pTemplate);
            return;
        }
    }

    // the cache is full - find an item to evict and reset all items to "cold"
    SIZE_T index_to_evict = 0;
    for (SIZE_T i = 0; i < CACHE_SIZE; i++)
    {
        // is the item cold?
        if (!m_items[i].IsHot())
        {
            index_to_evict = i;
        }
        m_items[i].MarkCold();
    }

    m_items[index_to_evict].m_iid = riid;
    m_items[index_to_evict].SetTemplate(pTemplate);
}

//---------------------------------------------------------
// ComCallWrapperTemplate::CCWInterfaceMapIterator
//---------------------------------------------------------
ComCallWrapperTemplate::CCWInterfaceMapIterator::CCWInterfaceMapIterator(TypeHandle thClass)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        CAN_TAKE_LOCK;
    }
    CONTRACTL_END;

    MethodTable *pMT = thClass.GetMethodTable();

    // iterate interface map of the type
    MethodTable::InterfaceMapIterator it = pMT->IterateInterfaceMap();
    while (it.Next())
    {
        MethodTable *pItfMT = it.GetInterface(pMT);
        AppendInterface(pItfMT);
    }

    Reset();
}

// Append a new interface to the m_Interfaces array.
ComCallWrapperTemplate::CCWInterfaceMapIterator::InterfaceProps &ComCallWrapperTemplate::CCWInterfaceMapIterator::AppendInterface(MethodTable *pItfMT)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    InterfaceProps &props = *m_Interfaces.Append();

    props.m_pItfMT = pItfMT;

    return props;
}

//---------------------------------------------------------
// One-time init
//---------------------------------------------------------
/*static*/
void ComCallWrapperTemplate::Init()
{
    WRAPPER_NO_CONTRACT;

    g_CreateWrapperTemplateCrst.Init(CrstWrapperTemplate, (CrstFlags)(CRST_REENTRANCY | CRST_HOST_BREAKABLE));
}

ComCallWrapperTemplate::ComCallWrapperTemplate()
{
    LIMITED_METHOD_CONTRACT;
}

//--------------------------------------------------------------------------
// static void ComCallWrapperTemplate::Cleanup(ComCallWrapperTemplate* pTemplate)
//  Cleanup the template
//--------------------------------------------------------------------------
void ComCallWrapperTemplate::Cleanup()
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        INSTANCE_CHECK;
        PRECONDITION(!m_thClass.IsNull());
    }
    CONTRACTL_END;

    for (unsigned j = 0; j < m_cbInterfaces; j++)
    {
        SLOT* pComVtable = m_rgpIPtr[j];

        if (pComVtable != 0)
        {
            ComMethodTable* pHeader = (ComMethodTable*)pComVtable-1;
            pHeader->Release(); // release the vtable
        }

#ifdef _DEBUG
        m_rgpIPtr[j] = (SLOT *)(size_t)INVALID_POINTER_CD;
#endif
    }

    if (m_pClassComMT)
        m_pClassComMT->Release();

    if (m_pBasicComMT)
        m_pBasicComMT->Release();

    if (m_pIIDToInterfaceTemplateCache)
        delete m_pIIDToInterfaceTemplateCache;

    delete[] (BYTE*)this;
}


LONG ComCallWrapperTemplate::AddRef()
{
    WRAPPER_NO_CONTRACT;

    return InterlockedIncrement(&m_cbRefCount);
}

LONG ComCallWrapperTemplate::Release()
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(m_cbRefCount > 0);
    }
    CONTRACTL_END;

    // use a different var here becuase cleanup will delete the object
    // so can no longer make member refs
    LONG cbRef = InterlockedDecrement(&m_cbRefCount);
    if (cbRef == 0)
        Cleanup();

    return cbRef;
}

ComMethodTable* ComCallWrapperTemplate::GetClassComMT()
{
    CONTRACT (ComMethodTable*)
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(SupportsIClassX());
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END;

    // First check the cache
    if (m_pClassComMT)
        RETURN m_pClassComMT;

    MethodTable *pMT = m_thClass.GetMethodTable();

    // We haven't set it up yet, generate one.
    ComMethodTable* pClassComMT = CreateComMethodTableForClass(pMT);
    pClassComMT->AddRef();

    // Cache it.
    if (InterlockedCompareExchangeT(&m_pClassComMT, pClassComMT, NULL) != NULL)
    {
        pClassComMT->Release();
    }

    RETURN m_pClassComMT;
}

ComMethodTable* ComCallWrapperTemplate::GetComMTForItf(MethodTable *pItfMT)
{
    CONTRACT (ComMethodTable*)
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(CheckPointer(pItfMT));
        PRECONDITION(pItfMT->IsInterface());
        POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
    }
    CONTRACT_END;

    // Look through all the implemented interfaces to see if the specified
    // one is present yet.
    for (UINT iItf = 0; iItf < m_cbInterfaces; iItf++)
    {
        ComMethodTable* pItfComMT = (ComMethodTable *)m_rgpIPtr[iItf] - 1;
        if (pItfComMT && (pItfComMT->m_pMT == pItfMT))
            RETURN pItfComMT;
    }

    // The class does not implement the specified interface.
    RETURN NULL;
}

ComMethodTable* ComCallWrapperTemplate::GetBasicComMT()
{
    CONTRACT (ComMethodTable*)
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END;

    RETURN m_pBasicComMT;
}


ULONG ComCallWrapperTemplate::GetNumInterfaces()
{
    LIMITED_METHOD_CONTRACT;
    return m_cbInterfaces;
}

SLOT* ComCallWrapperTemplate::GetVTableSlot(ULONG index)
{
    CONTRACT (SLOT*)
    {
        WRAPPER(THROWS);
        WRAPPER(GC_TRIGGERS);
        MODE_ANY;
        PRECONDITION(index >= 0 && index < m_cbInterfaces);
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END;

    RETURN m_rgpIPtr[index];
}

// Determines whether the template is for a type that cannot be safely marshalled to
// an out of proc COM client
BOOL ComCallWrapperTemplate::IsSafeTypeForMarshalling()
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    }
    CONTRACTL_END;

    if (m_flags & enum_IsSafeTypeForMarshalling)
    {
        return TRUE;
    }

    BOOL isSafe = TRUE;
    PTR_MethodTable pMt = this->GetClassType().GetMethodTable();
    EX_TRY
    {
        // Do casting checks so that we handle derived types as well. The base blocked types are:
        // System.Reflection.Assembly, System.Reflection.MemberInfo, System.Reflection.Module,
        // System.Reflection.MethodBody, and System.Reflection.ParameterInfo.
        // Some interesting derived types that get blocked as a result are:
        // System.Type, System.Reflection.TypeInfo, System.Reflection.MethodInfo, and System.Reflection.FieldInfo
        if (pMt->CanCastToClass(CoreLibBinder::GetClass(CLASS__ASSEMBLYBASE)) ||
        pMt->CanCastToClass(CoreLibBinder::GetClass(CLASS__MEMBER)) ||
        pMt->CanCastToClass(CoreLibBinder::GetClass(CLASS__MODULEBASE)) ||
        pMt->CanCastToClass(CoreLibBinder::GetClass(CLASS__RUNTIME_METHOD_BODY)) ||
        pMt->CanCastToClass(CoreLibBinder::GetClass(CLASS__PARAMETER)))
        {
            isSafe = FALSE;
        }
    }
    EX_CATCH
    {
        isSafe = FALSE;
    }
    EX_END_CATCH(SwallowAllExceptions);

    if (isSafe)
    {
        FastInterlockOr(&m_flags, enum_IsSafeTypeForMarshalling);
    }

    return isSafe;
}

//--------------------------------------------------------------------------
// Checks to see if the parent of the current class interface is visible to COM.
// Throws an InvalidOperationException if not.
//--------------------------------------------------------------------------
void ComCallWrapperTemplate::CheckParentComVisibility(BOOL fForIDispatch)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;


    // Throw an exception to report the error.
    if (!CheckParentComVisibilityNoThrow(fForIDispatch))
    {
        ComCallWrapperTemplate *invisParent = FindInvisibleParent();
        _ASSERTE(invisParent != NULL);

        SString thisType;
        SString invisParentType;
        TypeString::AppendType(thisType, m_thClass);
        TypeString::AppendType(invisParentType, invisParent->m_thClass);
        COMPlusThrow(kInvalidOperationException, IDS_EE_COM_INVISIBLE_PARENT, thisType.GetUnicode(), invisParentType.GetUnicode());
    }
}

BOOL ComCallWrapperTemplate::CheckParentComVisibilityNoThrow(BOOL fForIDispatch)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;


    // If the parent is visible to COM then everything is ok.
    if (!HasInvisibleParent())
        return TRUE;

    return FALSE;
}

DefaultInterfaceType ComCallWrapperTemplate::GetDefaultInterface(MethodTable **ppDefaultItf)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    if ((m_flags & enum_DefaultInterfaceTypeComputed) == 0)
    {
        // we have not computed the default interface yet
        TypeHandle th;
        DefaultInterfaceType defItfType = GetDefaultInterfaceForClassInternal(m_thClass, &th);

        _ASSERTE(th.IsNull() || !th.IsTypeDesc());
        m_pDefaultItf = th.AsMethodTable();

        FastInterlockOr(&m_flags, enum_DefaultInterfaceTypeComputed | (DWORD)defItfType);
    }

    *ppDefaultItf = m_pDefaultItf;
    return (DefaultInterfaceType)(m_flags & enum_DefaultInterfaceTypeMask);
}

//--------------------------------------------------------------------------
// Creates a ComMethodTable for a class's IClassX.
//--------------------------------------------------------------------------
ComMethodTable* ComCallWrapperTemplate::CreateComMethodTableForClass(MethodTable *pClassMT)
{
    CONTRACT (ComMethodTable*)
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM());
        PRECONDITION(CheckPointer(pClassMT));
        PRECONDITION(!pClassMT->IsInterface());
        PRECONDITION(!pClassMT->GetComPlusParentMethodTable() || pClassMT->GetComPlusParentMethodTable()->GetComCallWrapperTemplate());
        PRECONDITION(SupportsIClassX());
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END;

    unsigned cbNewPublicFields = 0;
    unsigned cbNewPublicMethods = 0;
    MethodTable* pComPlusParentClass = pClassMT->GetComPlusParentMethodTable();
    MethodTable* pParentClass = pClassMT->GetParentMethodTable();
    MethodTable* pCurrParentClass = pComPlusParentClass;
    MethodTable* pCurrMT = pClassMT;
    InteropMethodTableData* pCurrParentInteropMT = NULL;
    InteropMethodTableData* pCurrInteropMT = NULL;
    CorClassIfaceAttr ClassItfType = pClassMT->GetComClassInterfaceType();
    ComMethodTable *pParentComMT = NULL;
    unsigned cbTotalParentFields = 0;
    unsigned cbNumParentVirtualMethods = 0;
    unsigned cbParentComMTSlots = 0;
    unsigned i;
    const unsigned cbExtraSlots = ComMethodTable::GetNumExtraSlots(ifDual);
    CQuickEEClassPtrs apClassesToProcess;
    int cClassesToProcess = 0;

    // If the specified class has a parent then retrieve information on him.
    // This makes sure we always have the space for parent slots
    if (pComPlusParentClass)
    {
        ComCallWrapperTemplate *pComPlusParentTemplate = pComPlusParentClass->GetComCallWrapperTemplate();
        _ASSERTE(pComPlusParentTemplate);
        pParentComMT = pComPlusParentTemplate->GetClassComMT();
        cbParentComMTSlots = pParentComMT->GetNumSlots();
    }

    // We only set up the members of the class interface if the doesn't have any generic instantiations
    // in it's hierarchy.
    if (!pClassMT->HasGenericClassInstantiationInHierarchy())
    {
        LOG((LF_INTEROP, LL_INFO1000, "CreateComMethodTableForClass %s\n", pClassMT->GetClass()->GetDebugClassName()));
        LOG((LF_INTEROP, LL_INFO1000, "parent class: %s\n", (pComPlusParentClass) ? pParentComMT->GetMethodTable()->GetClass()->GetDebugClassName() : 0));


        // Create an array of all the classes for which we need to compute the added members.
        do
        {
            apClassesToProcess.ReSizeThrows(cClassesToProcess + 2);
            apClassesToProcess[cClassesToProcess++] = pCurrMT;
            pCurrMT = pCurrMT->GetParentMethodTable();
        }
        while (pCurrMT != pComPlusParentClass);
        apClassesToProcess[cClassesToProcess++] = pCurrMT;

        // Compute the number of methods and fields that were added between our parent
        // COM+ class and the current class. This includes methods on COM classes
        // between the current class and its parent COM+ class.
        for (cClassesToProcess -= 2; cClassesToProcess >= 0; cClassesToProcess--)
        {
            // Retrieve the current class and the current parent class.
            pCurrMT = apClassesToProcess[cClassesToProcess];
            pCurrInteropMT = pCurrMT->GetComInteropData();
            _ASSERTE(pCurrInteropMT);

            pCurrParentClass = apClassesToProcess[cClassesToProcess + 1];

            // Retrieve the number of fields and vtable methods on the parent class.
            if (pCurrParentClass)
            {
                cbTotalParentFields = pCurrParentClass->GetNumInstanceFields();
                pCurrParentInteropMT = pCurrParentClass->GetComInteropData();
                _ASSERTE(pCurrParentInteropMT);
                cbNumParentVirtualMethods = pCurrParentInteropMT->cVTable;
            }

            // Compute the number of methods that were private but made public on this class.
            for (i = 0; i < cbNumParentVirtualMethods; i++)
            {
                MethodDesc* pMD = NULL;
                InteropMethodTableSlotData *pCurrInteropMD = NULL;
                pCurrInteropMD = &pCurrInteropMT->pVTable[i];
                pMD = pCurrInteropMD->pMD;

                MethodDesc* pParentMD = NULL;
                InteropMethodTableSlotData *pCurrParentInteropMD = NULL;
                pCurrParentInteropMD = &pCurrParentInteropMT->pVTable[i];
                pParentMD = pCurrParentInteropMD->pMD;

                if (pMD &&
                    !(pCurrInteropMD ? IsDuplicateClassItfMD(pCurrInteropMD, i) : IsDuplicateClassItfMD(pMD, i)) &&
                    IsOverloadedComVisibleMember(pMD, pParentMD))
                {
                    cbNewPublicMethods++;
                }
            }

            // Compute the number of public methods that were added.
            unsigned cbNumVirtualMethods = 0;
            cbNumVirtualMethods = pCurrInteropMT->cVTable;

            for (i = cbNumParentVirtualMethods; i < cbNumVirtualMethods; i++)
            {
                MethodDesc* pMD = NULL;
                InteropMethodTableSlotData *pCurrInteropMD = NULL;
                pCurrInteropMD = &pCurrInteropMT->pVTable[i];
                pMD = pCurrInteropMD->pMD;

                if (pMD &&
                        !(pCurrInteropMD ? IsDuplicateClassItfMD(pCurrInteropMD, i) : IsDuplicateClassItfMD(pMD, i)) &&
                    IsNewComVisibleMember(pMD))
                {
                    cbNewPublicMethods++;
                }
            }

            // Add the non virtual methods introduced on the current class.
            MethodTable::MethodIterator it(pCurrMT);
            for (; it.IsValid(); it.Next())
            {
                if (!it.IsVirtual())
                {
                    MethodDesc* pMD = it.GetMethodDesc();
                        if (pMD && !IsDuplicateClassItfMD(pMD, it.GetSlotNumber()) && IsNewComVisibleMember(pMD) &&
                        !pMD->IsStatic() && !pMD->IsCtor() &&
                        (!pCurrMT->IsValueType() || (ClassItfType != clsIfAutoDual && IsStrictlyUnboxed(pMD))))
                    {
                        cbNewPublicMethods++;
                    }
                }
            }

            // Compute the number of new public fields this class introduces.
            // <TODO>check this approximation </TODO>
            ApproxFieldDescIterator fdIterator(pCurrMT, ApproxFieldDescIterator::INSTANCE_FIELDS);
            FieldDesc* pFD;

            while ((pFD = fdIterator.Next()) != NULL)
            {
                if (IsMemberVisibleFromCom(pCurrMT, pFD->GetMemberDef(), mdTokenNil))
                    cbNewPublicFields++;
            }
        }
    }


    // Alloc space for the class method table, includes getter and setter
    // for public fields
    S_UINT32 cbNewSlots = S_UINT32(cbNewPublicFields) * S_UINT32(2) + S_UINT32(cbNewPublicMethods);
    S_UINT32 cbTotalSlots = S_UINT32(cbParentComMTSlots) + cbNewSlots;

    LOG((LF_INTEROP, LL_INFO1000, "cbExtraSlots:         %d\n", cbExtraSlots));
    LOG((LF_INTEROP, LL_INFO1000, "cbParentComMTSlots:   %d\n", cbParentComMTSlots));
    LOG((LF_INTEROP, LL_INFO1000, "cbNewSlots:           %d\n", cbNewSlots.IsOverflow() ? 0 : cbNewSlots.Value()));
    LOG((LF_INTEROP, LL_INFO1000, "  cbNewPublicFields:  %d\n", cbNewPublicFields));
    LOG((LF_INTEROP, LL_INFO1000, "  cbNewPublicMethods: %d\n", cbNewPublicMethods));
    LOG((LF_INTEROP, LL_INFO1000, "cbTotalSlots:         %d\n", cbTotalSlots.IsOverflow() ? 0 : cbTotalSlots.Value()));

    // Alloc COM vtable & method descs
    S_UINT32 cbVtable  = (cbTotalSlots + S_UINT32(cbExtraSlots)) * S_UINT32(sizeof(SLOT));
    S_UINT32 cbToAlloc = S_UINT32(sizeof(ComMethodTable)) + cbVtable;

    if (cbToAlloc.IsOverflow())
        ThrowHR(COR_E_OVERFLOW);

    NewExecutableHolder<ComMethodTable> pComMT = (ComMethodTable*) new (executable) BYTE[cbToAlloc.Value()];

    _ASSERTE(!cbNewSlots.IsOverflow() && !cbTotalSlots.IsOverflow() && !cbVtable.IsOverflow());

    ExecutableWriterHolder<ComMethodTable> comMTWriterHolder(pComMT, cbToAlloc.Value());
    ComMethodTable* pComMTRW = comMTWriterHolder.GetRW();

    // set up the header
    pComMTRW->m_ptReserved = (SLOT)(size_t)0xDEADC0FF;          // reserved
    pComMTRW->m_pMT  = pClassMT; // pointer to the class method table
    pComMTRW->m_cbRefCount = 0;
    pComMTRW->m_pMDescr = NULL;
    pComMTRW->m_pITypeInfo = NULL;
    pComMTRW->m_pDispatchInfo = NULL;
    pComMTRW->m_cbSlots = cbTotalSlots.Value(); // number of slots not counting IDisp methods.
    pComMTRW->m_IID = GUID_NULL;


    // Set the flags.
    pComMTRW->m_Flags = enum_ClassVtableMask | ClassItfType;

    // Determine if the interface is visible from COM.
    if (IsTypeVisibleFromCom(TypeHandle(pComMT->m_pMT)))
        pComMTRW->m_Flags |= enum_ComVisible;

#if _DEBUG
    {
        // In debug set all the vtable slots to 0xDEADCA11.
        SLOT *pComVTable = (SLOT*)(pComMTRW + 1);
        for (unsigned iComSlots = 0; iComSlots < cbTotalSlots.Value() + cbExtraSlots; iComSlots++)
            *(pComVTable + iComSlots) = (SLOT)(size_t)0xDEADCA11;
    }
#endif

    LOG((LF_INTEROP, LL_INFO1000, "---------- end of CreateComMethodTableForClass %s -----------\n", pClassMT->GetClass()->GetDebugClassName()));

    pComMT.SuppressRelease();
    RETURN pComMT;
}

//--------------------------------------------------------------------------
// Creates a ComMethodTable for a an interface.
//--------------------------------------------------------------------------
ComMethodTable* ComCallWrapperTemplate::CreateComMethodTableForInterface(MethodTable* pInterfaceMT)
{
    CONTRACT (ComMethodTable*)
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM());
        PRECONDITION(CheckPointer(pInterfaceMT));
        PRECONDITION(pInterfaceMT->IsInterface());
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END;

    MethodTable *pItfClass = pInterfaceMT;
    CorIfaceAttr ItfType = pInterfaceMT->GetComInterfaceType();
    ULONG cbExtraSlots = ComMethodTable::GetNumExtraSlots(ItfType);

    LOG((LF_INTEROP, LL_INFO1000, "CreateComMethodTableForInterface %s\n", pItfClass->GetDebugClassName()));

    // @todo get slots off the methodtable
    unsigned cbSlots = pInterfaceMT->GetNumVirtuals();
    unsigned cbComSlots = pInterfaceMT->IsSparseForCOMInterop() ? pInterfaceMT->GetClass()->GetSparseCOMInteropVTableMap()->GetNumVTableSlots() : cbSlots;

    LOG((LF_INTEROP, LL_INFO1000, "cbExtraSlots = %d\n", cbExtraSlots));
    LOG((LF_INTEROP, LL_INFO1000, "cbComSlots   = %d\n", cbComSlots));
    LOG((LF_INTEROP, LL_INFO1000, "cbSlots      = %d\n", cbSlots));

    S_UINT32 cbVtable    = (S_UINT32(cbComSlots) + S_UINT32(cbExtraSlots)) * S_UINT32(sizeof(SLOT));
    S_UINT32 cbMethDescs = S_UINT32(cbSlots) * S_UINT32((COMMETHOD_PREPAD + sizeof(ComCallMethodDesc)));
    S_UINT32 cbToAlloc   = S_UINT32(sizeof(ComMethodTable)) + cbVtable + cbMethDescs;

    if (cbToAlloc.IsOverflow())
        ThrowHR(COR_E_OVERFLOW);

    NewExecutableHolder<ComMethodTable> pComMT = (ComMethodTable*) new (executable) BYTE[cbToAlloc.Value()];

    _ASSERTE(!cbVtable.IsOverflow() && !cbMethDescs.IsOverflow());

    ExecutableWriterHolder<ComMethodTable> comMTWriterHolder(pComMT, cbToAlloc.Value());
    ComMethodTable* pComMTRW = comMTWriterHolder.GetRW();

    // set up the header
    pComMTRW->m_ptReserved = (SLOT)(size_t)0xDEADC0FF;          // reserved
    pComMTRW->m_pMT  = pInterfaceMT; // pointer to the interface's method table
    pComMTRW->m_cbSlots = cbComSlots; // number of slots not counting IUnk
    pComMTRW->m_cbRefCount = 0;
    pComMTRW->m_pMDescr = NULL;
    pComMTRW->m_pITypeInfo = NULL;
    pComMTRW->m_pDispatchInfo = NULL;

    // Set the flags.
    pComMTRW->m_Flags = ItfType;

    // Set the IID of the interface.
    pInterfaceMT->GetGuid(&pComMTRW->m_IID, TRUE);
    pComMTRW->m_Flags |= enum_GuidGenerated;

    // Determine if the interface is visible from COM.
    if (IsTypeVisibleFromCom(TypeHandle(pComMT->m_pMT)))
        pComMTRW->m_Flags |= enum_ComVisible;

    // Determine if the interface is a COM imported class interface.
    if (pItfClass->GetClass()->IsComClassInterface())
        pComMTRW->m_Flags |= enum_ComClassItf;

#ifdef _DEBUG
    {
        // In debug set all the vtable slots to 0xDEADCA11.
        SLOT *pComVTable = (SLOT*)(pComMTRW + 1);
        for (unsigned iComSlots = 0; iComSlots < cbComSlots + cbExtraSlots; iComSlots++)
            *(pComVTable + iComSlots) = (SLOT)(size_t)0xDEADCA11;
    }
#endif

    LOG((LF_INTEROP, LL_INFO1000, "---------- end of CreateComMethodTableForInterface %s -----------\n", pItfClass->GetDebugClassName()));

    pComMT.SuppressRelease();
    RETURN pComMT;
}

ComMethodTable* ComCallWrapperTemplate::CreateComMethodTableForBasic(MethodTable* pMT)
{
    CONTRACT (ComMethodTable*)
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM());
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END;

    const unsigned cbExtraSlots = ComMethodTable::GetNumExtraSlots(ifDispatch);
    CorClassIfaceAttr ClassItfType = pMT->GetComClassInterfaceType();

    LOG((LF_INTEROP, LL_INFO1000, "CreateComMethodTableForBasic %s\n", pMT->GetDebugClassName()));

    unsigned cbVtable    = cbExtraSlots * sizeof(SLOT);
    unsigned cbToAlloc   = sizeof(ComMethodTable) + cbVtable;

    NewExecutableHolder<ComMethodTable> pComMT = (ComMethodTable*) new (executable) BYTE[cbToAlloc];
    ExecutableWriterHolder<ComMethodTable> comMTWriterHolder(pComMT, cbToAlloc);
    ComMethodTable* pComMTRW = comMTWriterHolder.GetRW();

    // set up the header
    pComMTRW->m_ptReserved = (SLOT)(size_t)0xDEADC0FF;
    pComMTRW->m_pMT  = pMT;
    pComMTRW->m_cbSlots = 0;  // number of slots not counting IUnk
    pComMTRW->m_cbRefCount = 0;
    pComMTRW->m_pMDescr = NULL;
    pComMTRW->m_pITypeInfo = NULL;
    pComMTRW->m_pDispatchInfo = NULL;

    // Initialize the flags.
    pComMTRW->m_Flags =  enum_IsBasic;
    pComMTRW->m_Flags |= enum_ClassVtableMask | ClassItfType;

    // Set the IID of the interface.
    pComMTRW->m_IID = IID_IUnknown;
    pComMTRW->m_Flags |= enum_GuidGenerated;

    // Determine if the interface is visible from COM.
    if (IsTypeVisibleFromCom(TypeHandle(pComMT->m_pMT)))
        pComMTRW->m_Flags |= enum_ComVisible;

    // Determine if the interface is a COM imported class interface.
    if (pMT->GetClass()->IsComClassInterface())
        pComMTRW->m_Flags |= enum_ComClassItf;

#ifdef _DEBUG_0xDEADCA11
    {
        // In debug set all the vtable slots to 0xDEADCA11.
        SLOT *pComVTable = (SLOT*)(pComMTRW + 1);
        for (unsigned iComSlots = 0; iComSlots < DEBUG_AssertSlots + cbExtraSlots; iComSlots++)
            *(pComVTable + iComSlots) = (SLOT)(size_t)0xDEADCA11;
    }
#endif

    LOG((LF_INTEROP, LL_INFO1000, "---------- end of CreateComMethodTableForBasic %s -----------\n", pMT->GetDebugClassName()));

    pComMT.SuppressRelease();
    RETURN pComMT;
}

//--------------------------------------------------------------------------
// Creates a ComMethodTable for an interface and stores it in the m_rgpIPtr array.
//--------------------------------------------------------------------------
ComMethodTable *ComCallWrapperTemplate::InitializeForInterface(MethodTable *pParentMT, MethodTable *pItfMT, DWORD dwIndex)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    ComMethodTable *pItfComMT = NULL;
    if (m_pParent != NULL)
    {
        pItfComMT = m_pParent->GetComMTForItf(pItfMT);
        if (pItfComMT != NULL)
        {
            // if the parent COM MT is not a trivial aggregate, simple MethodTable slot check is enough
            if (!m_thClass.GetMethodTable()->ImplementsInterfaceWithSameSlotsAsParent(pItfMT, pParentMT))
            {
                // the interface is implemented by parent but this class reimplemented
                // its method(s) so we will need to build a new COM vtable for it
                pItfComMT = NULL;
            }
        }
    }

    if (pItfComMT == NULL)
    {
        // we couldn't use parent's vtable so we create a new one
        pItfComMT = CreateComMethodTableForInterface(pItfMT);
    }

    m_rgpIPtr[dwIndex] = (SLOT*)(pItfComMT + 1);
    pItfComMT->AddRef();

    // update pItfMT in case code:CreateComMethodTableForInterface decided to redirect the interface
    pItfMT = pItfComMT->GetMethodTable();
    if (pItfMT == CoreLibBinder::GetExistingClass(CLASS__ICUSTOM_QUERYINTERFACE))
    {
        m_flags |= enum_ImplementsICustomQueryInterface;
    }
    else if (InlineIsEqualGUID(pItfComMT->GetIID(), IID_IMarshal))
    {
        // detect IMarshal so we can handle IAgileObject in a backward compatible way
        m_flags |= enum_ImplementsIMarshal;
    }

    return pItfComMT;
}


//--------------------------------------------------------------------------
// ComCallWrapper* ComCallWrapper::CreateTemplate(TypeHandle thClass)
//  create a template wrapper, which is cached in the class
//  used for initializing other wrappers for instances of the class
//--------------------------------------------------------------------------
ComCallWrapperTemplate* ComCallWrapperTemplate::CreateTemplate(TypeHandle thClass)
{
    CONTRACT (ComCallWrapperTemplate*)
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM());
        PRECONDITION(!thClass.IsNull());
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END;

    GCX_PREEMP();

    if (!thClass.IsTypeDesc())
    {
        // Canonicalize the class type because we are going to stick the template pointer to EEClass.
        thClass = thClass.GetCanonicalMethodTable();
    }
    MethodTable *pMT = thClass.GetMethodTable();

    MethodTable *pParentMT = pMT->GetComPlusParentMethodTable();
    ComCallWrapperTemplate *pParentTemplate = NULL;
    unsigned iItf = 0;

    // Create the parent's template if it has not been created yet.
    if (pParentMT)
    {
        pParentTemplate = pParentMT->GetComCallWrapperTemplate();
        if (!pParentTemplate)
            pParentTemplate = CreateTemplate(pParentMT);
    }

    // Preload the policy for this interface
    CCWInterfaceMapIterator it(thClass);

    // Num interfaces in the template.
    unsigned numInterfaces = it.GetCount();

    // Check to see if another thread has already set up the template.
    {
        // Move this inside the scope so it is destroyed before its memory is.
        ComCallWrapperTemplateHolder pTemplate = NULL;

        pTemplate = thClass.GetComCallWrapperTemplate();

        if (pTemplate)
        {
            pTemplate.SuppressRelease();
            RETURN pTemplate;
        }

        // Allocate the template.
        pTemplate = (ComCallWrapperTemplate*)new BYTE[sizeof(ComCallWrapperTemplate) + numInterfaces * sizeof(SLOT)];

        // Store the information required by the template.
        //  Also, eagerly set vars to NULL, in case we are interrupted during construction
        //  and try to destruct the template.
        memset(pTemplate->m_rgpIPtr, 0, numInterfaces * sizeof(SLOT));
        pTemplate->m_thClass = thClass;
        pTemplate->m_cbInterfaces = numInterfaces;
        pTemplate->m_pParent = pParentTemplate;
        pTemplate->m_cbRefCount = 1;
        pTemplate->m_pClassComMT = NULL;        // Defer setting this up.
        pTemplate->m_pBasicComMT = NULL;
        pTemplate->m_pDefaultItf = NULL;
        pTemplate->m_pICustomQueryInterfaceGetInterfaceMD = NULL;
        pTemplate->m_pIIDToInterfaceTemplateCache = NULL;
        pTemplate->m_flags = 0;

        // Determine the COM visibility of classes in our hierarchy.
        pTemplate->DetermineComVisibility();

        // Eagerly create the basic CMT.
        pTemplate->m_pBasicComMT = pTemplate->CreateComMethodTableForBasic(pMT);
        pTemplate->m_pBasicComMT->AddRef();

        if (ClassSupportsIClassX(pMT))
        {
            // we will allow building IClassX for the class
            pTemplate->m_flags |= enum_SupportsIClassX;
        }

        if (IsOleAutDispImplRequiredForClass(pMT))
        {
            // Determine what IDispatch implementation this class should use
            pTemplate->m_flags |= enum_UseOleAutDispatchImpl;
        }

        // Eagerly create the interface CMTs.
        // when iterate the interfaces implemented by the methodtable, we can check whether
        // the interface supports ICustomQueryInterface.
        CoreLibBinder::GetClass(CLASS__ICUSTOM_QUERYINTERFACE);

        it.Reset();
        while (it.Next())
        {
            MethodTable *pItfMT = it.GetInterface();
            ComMethodTable *pItfComMT = pTemplate->InitializeForInterface(pParentMT, pItfMT, it.GetIndex());
        }

        // Cache the template in class.
        if (!thClass.SetComCallWrapperTemplate(pTemplate))
        {
            // another thread beat us to it
            pTemplate = thClass.GetComCallWrapperTemplate();
            _ASSERTE(pTemplate != NULL);

            pTemplate.SuppressRelease();
            RETURN pTemplate;
        }
        pTemplate.SuppressRelease();

#ifdef PROFILING_SUPPORTED
        // Notify profiler of the CCW, so it can avoid double-counting.
        if (pTemplate->SupportsIClassX())
        {
            BEGIN_PROFILER_CALLBACK(CORProfilerTrackCCW());
            // When under the profiler, we'll eagerly generate the IClassX CMT.
            pTemplate->GetClassComMT();

            IID IClassXIID = GUID_NULL;
            SLOT *pComVtable = (SLOT *)(pTemplate->m_pClassComMT + 1);

            // If the class is visible from COM, then give out the IClassX IID.
            if (pTemplate->m_pClassComMT->IsComVisible())
                GenerateClassItfGuid(thClass, &IClassXIID);

#if defined(_DEBUG)
            WCHAR rIID[40]; // {00000000-0000-0000-0000-000000000000}
            GuidToLPWSTR(IClassXIID, rIID, lengthof(rIID));
            SString ssName;
            thClass.GetName(ssName);
            LOG((LF_CORPROF, LL_INFO100, "COMClassicVTableCreated Class:%ls, IID:%ls, vTbl:%#08x\n",
                 ssName.GetUnicode(), rIID, pComVtable));
#else
            LOG((LF_CORPROF, LL_INFO100, "COMClassicVTableCreated TypeHandle:%#x, IID:{%08x-...}, vTbl:%#08x\n",
                 thClass.AsPtr(), IClassXIID.Data1, pComVtable));
#endif
            (&g_profControlBlock)->COMClassicVTableCreated(
                (ClassID) thClass.AsPtr(), IClassXIID, pComVtable,
                pTemplate->m_pClassComMT->m_cbSlots +
                    ComMethodTable::GetNumExtraSlots(pTemplate->m_pClassComMT->GetInterfaceType()));
            END_PROFILER_CALLBACK();
        }
#endif // PROFILING_SUPPORTED
        RETURN pTemplate;
    }
}

//--------------------------------------------------------------------------
// Creates a new Template for just one interface.
//--------------------------------------------------------------------------
ComCallWrapperTemplate *ComCallWrapperTemplate::CreateTemplateForInterface(MethodTable *pItfMT)
{
    CONTRACT (ComCallWrapperTemplate*)
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM());
        PRECONDITION(CheckPointer(pItfMT));
        PRECONDITION(pItfMT->IsInterface());
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END;

    GCX_PREEMP();

    // Num interfaces in the template.
    unsigned numInterfaces = 1;

    // Allocate the template.
    ComCallWrapperTemplateHolder pTemplate = pItfMT->GetComCallWrapperTemplate();
    if (pTemplate)
    {
        pTemplate.SuppressRelease();
        RETURN pTemplate;
    }

    pTemplate = (ComCallWrapperTemplate *)new BYTE[sizeof(ComCallWrapperTemplate) + numInterfaces * sizeof(SLOT)];

    // Store the information required by the template.
    //  Also, eagerly set vars to NULL, in case we are interrupted during construction
    //  and try to destruct the template.
    memset(pTemplate->m_rgpIPtr, 0, numInterfaces * sizeof(SLOT));
    pTemplate->m_thClass = TypeHandle(pItfMT);
    pTemplate->m_cbInterfaces = numInterfaces;
    pTemplate->m_pParent = NULL;
    pTemplate->m_cbRefCount = 1;
    pTemplate->m_pClassComMT = NULL;
    pTemplate->m_pBasicComMT = NULL;
    pTemplate->m_pDefaultItf = pItfMT;
    pTemplate->m_pICustomQueryInterfaceGetInterfaceMD = NULL;
    pTemplate->m_pIIDToInterfaceTemplateCache = NULL;
    pTemplate->m_flags = enum_RepresentsVariantInterface;

    // Initialize the one ComMethodTable
    ComMethodTable *pItfComMT;

    pItfComMT = pTemplate->InitializeForInterface(NULL, pItfMT, 0);

    // Cache the template on the interface.
    if (!pItfMT->SetComCallWrapperTemplate(pTemplate))
    {
        // another thread beat us to it
        pTemplate = pItfMT->GetComCallWrapperTemplate();
        _ASSERTE(pTemplate != NULL);
    }

    pTemplate.SuppressRelease();
    RETURN pTemplate;
}

void ComCallWrapperTemplate::DetermineComVisibility()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    m_flags &= (~enum_InvisibleParent);

    if (m_pParent == NULL)
        return;

    // Check if the parent has an invisible parent
    // or if the parent itself is invisible.
    if (m_pParent->HasInvisibleParent()
        || !IsTypeVisibleFromCom(m_pParent->m_thClass))
    {
        _ASSERTE(NULL != FindInvisibleParent());
        m_flags |= enum_InvisibleParent;
    }
}

ComCallWrapperTemplate* ComCallWrapperTemplate::FindInvisibleParent()
{
    ComCallWrapperTemplate* invisParentMaybe = m_pParent;

    // Walk up the CCW parent classes and try to find
    // if one is invisible to COM.
    while (invisParentMaybe != NULL)
    {
        // If our parent is invisible, return it.
        if (!IsTypeVisibleFromCom(invisParentMaybe->m_thClass))
            return invisParentMaybe;

        invisParentMaybe = invisParentMaybe->m_pParent;
    }

    // All classes in hierarchy are COM visible
    return NULL;
}

//--------------------------------------------------------------------------
// ComCallWrapperTemplate* ComCallWrapperTemplate::GetTemplate(TypeHandle thClass)
// look for a template in the method table, if not create one
//--------------------------------------------------------------------------
ComCallWrapperTemplate* ComCallWrapperTemplate::GetTemplate(TypeHandle thType)
{
    CONTRACT (ComCallWrapperTemplate*)
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END;


    // Check to see if the specified class already has a template set up.
    ComCallWrapperTemplate* pTemplate = thType.GetComCallWrapperTemplate();
    if (pTemplate)
        RETURN pTemplate;

    // Create the template and return it. CreateTemplate will take care of synchronization.
    if (thType.IsInterface())
    {
        RETURN CreateTemplateForInterface(thType.AsMethodTable());
    }
    else
    {
        RETURN CreateTemplate(thType);
    }
}

//--------------------------------------------------------------------------
// ComMethodTable *ComCallWrapperTemplate::SetupComMethodTableForClass(MethodTable *pMT)
// Sets up the wrapper template for the speficied class and sets up a COM
// method table for the IClassX interface of the specified class. If the
// bLayOutComMT flag is set then if the IClassX COM method table has not
// been laid out yet then it will be.
//--------------------------------------------------------------------------
ComMethodTable *ComCallWrapperTemplate::SetupComMethodTableForClass(MethodTable *pMT, BOOL bLayOutComMT)
{
    CONTRACT (ComMethodTable*)
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(!pMT->IsInterface());
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END;

    // Retrieve the COM call wrapper template for the class.
    ComCallWrapperTemplate *pTemplate = GetTemplate(pMT);

    // Retrieve the IClassX COM method table.
    ComMethodTable *pIClassXComMT = pTemplate->GetClassComMT();
    _ASSERTE(pIClassXComMT);

    // Lay out the IClassX COM method table if it hasn't been laid out yet and
    // the bLayOutComMT flag is set.
    if (!pIClassXComMT->IsLayoutComplete() && bLayOutComMT)
    {
        pIClassXComMT->LayOutClassMethodTable();
        _ASSERTE(pIClassXComMT->IsLayoutComplete());
    }

    RETURN pIClassXComMT;
}


MethodDesc * ComCallWrapperTemplate::GetICustomQueryInterfaceGetInterfaceMD()
{
    CONTRACT (MethodDesc*)
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(m_flags & enum_ImplementsICustomQueryInterface);
    }
    CONTRACT_END;

    if (m_pICustomQueryInterfaceGetInterfaceMD == NULL)
        m_pICustomQueryInterfaceGetInterfaceMD = m_thClass.GetMethodTable()->GetMethodDescForInterfaceMethod(
           CoreLibBinder::GetMethod(METHOD__ICUSTOM_QUERYINTERFACE__GET_INTERFACE),
           TRUE /* throwOnConflict */);
    RETURN m_pICustomQueryInterfaceGetInterfaceMD;
}

ComCallWrapperTemplate::IIDToInterfaceTemplateCache *ComCallWrapperTemplate::GetOrCreateIIDToInterfaceTemplateCache()
{
    CONTRACT (IIDToInterfaceTemplateCache *)
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END;

    IIDToInterfaceTemplateCache *pCache = m_pIIDToInterfaceTemplateCache.Load();
    if (pCache == NULL)
    {
        pCache = new IIDToInterfaceTemplateCache();

        IIDToInterfaceTemplateCache *pOldCache = InterlockedCompareExchangeT(&m_pIIDToInterfaceTemplateCache, pCache, NULL);
        if (pOldCache != NULL)
        {
            delete pCache;
            RETURN pOldCache;
        }
    }
    RETURN pCache;
}


//--------------------------------------------------------------------------
//  Module* ComCallMethodDesc::GetModule()
//  Get Module
//--------------------------------------------------------------------------
Module* ComCallMethodDesc::GetModule()
{
    CONTRACT (Module*)
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION( IsFieldCall() ? (m_pFD != NULL) : (m_pMD != NULL) );
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END;

    MethodTable* pClass = (IsFieldCall()) ? m_pFD->GetEnclosingMethodTable() : m_pMD->GetMethodTable();
    _ASSERTE(pClass != NULL);

    RETURN pClass->GetModule();
}
