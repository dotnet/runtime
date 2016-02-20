// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


#include "common.h"
#ifdef MDA_SUPPORTED
#include "mda.h"
#include "mdaassistants.h"
#include "field.h"
#include "dllimport.h"
#ifdef FEATURE_COMINTEROP
#include "runtimecallablewrapper.h"
#include "comcallablewrapper.h"
#include "comcache.h"
#include "comtoclrcall.h"
#include "mlinfo.h"
#endif
#include "sigformat.h"
#include "fieldmarshaler.h"
#include "dllimportcallback.h"
#include "dbginterface.h"
#include "finalizerthread.h"

#ifdef FEATURE_COMINTEROP_APARTMENT_SUPPORT
#include "olecontexthelpers.h"
#endif // FEATURE_COMINTEROP_APARTMENT_SUPPORT

#ifdef MDA_SUPPORTED
////
//// Mda Assistants
////


// Why is ANYTHING in here marked SO_TOLERANT?? Presumably some of them are called from managed code????


//
// MdaFramework
// 
void MdaFramework::DumpDiagnostics()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        SO_NOT_MAINLINE;
    }
    CONTRACTL_END;
    
    ManagedDebuggingAssistants* pMda = g_mdaStaticHeap.m_pMda;

#ifdef _DEBUG
    if (m_dumpSchemaSchema)
    {
        MdaXmlElement* pXmlSchemaSchema = pMda->m_pSchemaSchema->ToXml(pMda->m_pMdaXmlIndustry);
//        MdaXmlMessage::SendMessage(pXmlSchemaSchema, TRUE, pMda->m_pSchemaSchema);   
    }

    if (m_dumpAssistantSchema)
    {
        MdaXmlElement* pXmlAssistantMsgSchema = pMda->m_pAssistantMsgSchema->ToXml(pMda->m_pMdaXmlIndustry);
//        MdaXmlMessage::SendMessage(pXmlAssistantMsgSchema, TRUE, pMda->m_pSchemaSchema);   
    }

    if (m_dumpAssistantMsgSchema)
    {
        MdaXmlElement* pXmlAssistantSchema = pMda->m_pAssistantSchema->ToXml(pMda->m_pMdaXmlIndustry);
//        MdaXmlMessage::SendMessage(pXmlAssistantSchema, TRUE, pMda->m_pSchemaSchema);   
    }
#endif
}

#ifdef _DEBUG
extern BOOL g_bMdaDisableAsserts;
#endif

void MdaFramework::Initialize(MdaXmlElement* pXmlInput)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        SO_INTOLERANT;
    }
    CONTRACTL_END;

#ifdef _DEBUG
    ManagedDebuggingAssistants* pMda = g_mdaStaticHeap.m_pMda;
    g_bMdaDisableAsserts = pXmlInput->GetAttributeValueAsBool(MdaAttrDecl(DisableAsserts));
    MdaXmlElement* pXmlDiagnostics = pXmlInput->GetChild(MdaElemDecl(Diagnostics));
            
    if (pXmlDiagnostics)
    {
        m_dumpSchemaSchema = pXmlDiagnostics->GetAttributeValueAsBool(MdaAttrDecl(DumpSchemaSchema), FALSE);
        m_dumpAssistantSchema = pXmlDiagnostics->GetAttributeValueAsBool(MdaAttrDecl(DumpAssistantSchema), FALSE);
        m_dumpAssistantMsgSchema = pXmlDiagnostics->GetAttributeValueAsBool(MdaAttrDecl(DumpAssistantMsgSchema), FALSE);
    }
#endif
}

//
// MdaGcUnmanagedToManaged
//
void MdaGcUnmanagedToManaged::TriggerGC()
{
    WRAPPER_NO_CONTRACT;

    TriggerGCForMDAInternal();
}


//
// MdaGcManagedToUnmanaged
//
void MdaGcManagedToUnmanaged::TriggerGC()
{
    WRAPPER_NO_CONTRACT;

    TriggerGCForMDAInternal();
}

void TriggerGCForMDAInternal()
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        SO_TOLERANT;
    }
    CONTRACTL_END;

    BEGIN_SO_INTOLERANT_CODE_NOTHROW(GetThread(), return);

    EX_TRY
    {
        GCHeap::GetGCHeap()->GarbageCollect();

#ifdef FEATURE_SYNCHRONIZATIONCONTEXT_WAIT
        //
        // It is very dangerous to wait for finalizer thread here if we are inside a wait 
        // operation, as the wait operation might call into interop which calls this MDA
        // and call into FinalizerThreadWait. In this case, we might run into infinite recursion: 
        //   SynchronizationContext.Wait -> P/Invoke -> WaitForPendingFinalizer ->
        //   SynchronizationContext.Wait ....
        // 
        // So, if we are inside a SyncContext.Wait, don't call out to FinalizerThreadWait
        //
        if (!GetThread()->HasThreadStateNC(Thread::TSNC_InsideSyncContextWait))
#endif // FEATURE_SYNCHRONIZATIONCONTEXT_WAIT            
            // It is possible that user code run as part of finalization will wait for this thread.
            // To avoid deadlocks, we limit the wait time to 10 seconds (an arbitrary number).
            FinalizerThread::FinalizerThreadWait(10 * 1000);
    }
    EX_CATCH
    {
        // Caller cannot take exceptions.
    }
    EX_END_CATCH(SwallowAllExceptions);
    
    END_SO_INTOLERANT_CODE;
}

//
// MdaCallbackOnCollectedDelegate
//
/*
MdaCallbackOnCollectedDelegate::~MdaCallbackOnCollectedDelegate()
{
    WRAPPER_NO_CONTRACT;
    
    if (m_pList && m_size)
    {
        for (int i=0; i < m_size; i++)
            ReplaceEntry(i, NULL);

        delete[] m_pList;
    }
}
*/

void MdaCallbackOnCollectedDelegate::ReportViolation(MethodDesc* pMD)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        SO_INTOLERANT;
    }
    CONTRACTL_END;

    MdaXmlElement* pXml;
    MdaXmlMessage msg(this->AsMdaAssistant(), TRUE, &pXml);
    MdaXmlElement* pDelegate = pXml->AddChild(MdaElemDecl(Delegate));
    StackSString delegateName;

    if(pMD)
    {
        AsMdaAssistant()->OutputMethodDesc(pMD, pDelegate);
        AsMdaAssistant()->ToString(delegateName, pMD);
    }

    msg.SendMessagef(MDARC_CALLBACK_ON_COLLECTED_DELEGATE, delegateName.GetUnicode());
}

void MdaCallbackOnCollectedDelegate::AddToList(UMEntryThunk* pEntryThunk)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        SO_INTOLERANT;
        PRECONDITION(CheckPointer(pEntryThunk));
    }
    CONTRACTL_END;

    // Get an index to use.
    ULONG oldIndex = m_iIndex;
    ULONG newIndex = oldIndex + 1;
    if (newIndex >= (ULONG)m_size)
        newIndex = 0;
    
    while ((ULONG)FastInterlockCompareExchange((LONG*)&m_iIndex, newIndex, oldIndex) != oldIndex)
    {
        oldIndex = m_iIndex;
        newIndex = oldIndex + 1;
        if (newIndex >= (ULONG)m_size)
            newIndex = 0;
    }

    // We successfully incremented the index and can use the oldIndex value as our entry.
    ReplaceEntry(oldIndex, pEntryThunk);
}

void MdaCallbackOnCollectedDelegate::ReplaceEntry(int index, UMEntryThunk* pET)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        SO_INTOLERANT;
        PRECONDITION((index >= 0) && (index < m_size));
        PRECONDITION(CheckPointer(m_pList));
    }
    CONTRACTL_END;
    
    if ((m_pList) && (m_size > index) && (index >= 0))
    {
        UMEntryThunk* pETTemp = m_pList[index];
        while (FastInterlockCompareExchangePointer((LPVOID*)&m_pList[index], (LPVOID)pET, (LPVOID)pETTemp) != (LPVOID)pETTemp)
        {
            pETTemp = m_pList[index];
        }
        
        if (NULL != pETTemp)
        {
            pETTemp->Terminate();
        }
    }
}

#ifdef FEATURE_COMINTEROP   

void MdaInvalidMemberDeclaration::ReportViolation(ComCallMethodDesc *pCMD, OBJECTREF *pExceptionObj)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        SO_INTOLERANT;
    }
    CONTRACTL_END;

    EX_TRY
    {
        MdaXmlElement* pXml;
        MdaXmlMessage msg(this->AsMdaAssistant(), TRUE, &pXml);

        TypeHandle th;
        StackSString strMemberName;
        StackSString strTypeName;
        StackSString strMessage;

        GetExceptionMessage(*pExceptionObj, strMessage);

        if (pCMD->IsFieldCall())
        {
            FieldDesc *pFD = pCMD->GetFieldDesc();

            th = pFD->GetFieldTypeHandleThrowing();
            strMemberName.SetUTF8(pFD->GetName());
            AsMdaAssistant()->OutputFieldDesc(pFD, pXml->AddChild(MdaElemDecl(Field)));
        }
        else
        {
            MethodDesc *pMD = pCMD->GetCallMethodDesc();

            th = TypeHandle(pMD->GetMethodTable());
            strMemberName.SetUTF8(pMD->GetName());
            AsMdaAssistant()->OutputMethodDesc(pMD, pXml->AddChild(MdaElemDecl(Method)));        
        }

        th.GetName(strTypeName);

        AsMdaAssistant()->OutputTypeHandle(th, pXml->AddChild(MdaElemDecl(Type)));
        AsMdaAssistant()->OutputException(pExceptionObj, pXml->AddChild(MdaElemDecl(Exception)));
            
        msg.SendMessagef(MDARC_INVALID_MEMBER_DECLARATION, 
            strMemberName.GetUnicode(), strTypeName.GetUnicode(), strMessage.GetUnicode());    
    }
    EX_CATCH
    {
        // Caller cannot take exceptions.
    }
    EX_END_CATCH(SwallowAllExceptions);
}

#endif //FEATURE_COMINTEROP


//
// MdaExceptionSwallowedOnCallFromCom
//
void MdaExceptionSwallowedOnCallFromCom::ReportViolation(MethodDesc *pMD, OBJECTREF *pExceptionObj)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        SO_INTOLERANT;
    }
    CONTRACTL_END;

    EX_TRY
    {
        StackSString strMessage;
        StackSString strTypeName;
        StackSString strMemberName;
            
        MdaXmlElement* pXml;
        MdaXmlMessage msg(this->AsMdaAssistant(), TRUE, &pXml);

        TypeHandle th = TypeHandle(pMD->GetMethodTable());

        GetExceptionMessage(*pExceptionObj, strMessage);
        th.GetName(strTypeName);
        strMemberName.SetUTF8(pMD->GetName());

        AsMdaAssistant()->OutputMethodDesc(pMD, pXml->AddChild(MdaElemDecl(Method)));        
        AsMdaAssistant()->OutputTypeHandle(th, pXml->AddChild(MdaElemDecl(Type)));        
        AsMdaAssistant()->OutputException(pExceptionObj, pXml->AddChild(MdaElemDecl(Exception)));
            
        msg.SendMessagef(MDARC_EXCEPTION_SWALLOWED_COM_TO_CLR, 
            strMemberName.GetUnicode(), strTypeName.GetUnicode(), strMessage.GetUnicode());    
    }
    EX_CATCH
    {
        // Caller cannot take exceptions.
    }
    EX_END_CATCH(SwallowAllExceptions);
}
   
    
//
// MdaInvalidVariant
//
void MdaInvalidVariant::ReportViolation()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        SO_INTOLERANT;
    }
    CONTRACTL_END;

    MdaXmlElement* pXml;
    MdaXmlMessage msg(this->AsMdaAssistant(), TRUE, &pXml);

    msg.SendMessagef(MDARC_INVALID_VARIANT);
}


//
// MdaInvalidIUnknown
//
void MdaInvalidIUnknown::ReportViolation()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        SO_INTOLERANT;
    }
    CONTRACTL_END;

    MdaXmlElement* pXml;
    MdaXmlMessage msg(this->AsMdaAssistant(), TRUE, &pXml);

    msg.SendMessagef(MDARC_INVALID_IUNKNOWN);
}


//
// MdaContextSwitchDeadlock
//
void MdaContextSwitchDeadlock::ReportDeadlock(LPVOID Origin, LPVOID Destination)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        SO_INTOLERANT;
    }
    CONTRACTL_END;

    if (g_fEEShutDown == 0)
    {
        EX_TRY
        {
            MdaXmlElement* pXml;
            MdaXmlMessage msg(this->AsMdaAssistant(), TRUE, &pXml);

            msg.SendMessagef(MDARC_CONTEXT_SWITCH_DEADLOCK, Origin, Destination);
        }
        EX_CATCH
        {
            // Caller cannot take exceptions.
        }
        EX_END_CATCH(SwallowAllExceptions);
    }
}


//
// MdaRaceOnRCWCleanup
//
void MdaRaceOnRCWCleanup::ReportViolation()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        SO_INTOLERANT;
    }
    CONTRACTL_END;

    MdaXmlElement* pXml;
    MdaXmlMessage msg(this->AsMdaAssistant(), TRUE, &pXml);

    msg.SendMessagef(MDARC_RCW_CLEANUP_RACE);
}


//
// MdaFailedQI
//
void MdaFailedQI::ReportAdditionalInfo(HRESULT hr, RCW* pRCW, GUID iid, MethodTable* pMT)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        SO_INTOLERANT;
    }
    CONTRACTL_END;

    MdaXmlElement* pXml;
    MdaXmlMessage msg(this->AsMdaAssistant(), TRUE, &pXml);
    TypeHandle th(pMT);

    SafeComHolder<IUnknown> pInnerUnk = pRCW->GetIUnknown(); 

    // We are interested in the case where the QI fails because of wrong context.
    if (!pRCW->IsFreeThreaded() && GetCurrentCtxCookie() != pRCW->GetWrapperCtxCookie())
    {
        // Try to change context and perform the QI in the new context again.
        MdaFailedQIAssistantCallbackData    data;
       
        data.pWrapper   = pRCW;
        data.iid        = iid;

        pRCW->EnterContext(MdaFailedQIAssistantCallback, &data);
        if (data.fSuccess)
        {
            // QI succeeds in the other context, i.e. the original QI fails because of wrong context.
            pXml = AsMdaAssistant()->OutputTypeHandle(th, pXml->AddChild(MdaElemDecl(Type)));

            // Stringize IID
            WCHAR strNativeItfIID[39];
            StringFromGUID2(iid, strNativeItfIID, sizeof(strNativeItfIID) / sizeof(WCHAR));

            // Map HRESULT to a message
            StackSString sszHR2Description;
            GetHRMsg(hr, sszHR2Description);

            // Format the HRESULT as a string
            StackSString sszHR2Hex;
            sszHR2Hex.Printf("%.8x", hr);

            StackSString sszTypeName;
            th.GetName(sszTypeName);
            
            msg.SendMessagef(MDARC_FAILED_QI, sszTypeName.GetUnicode(), strNativeItfIID, sszHR2Hex.GetUnicode(), sszHR2Description.GetUnicode());
        }
    }
    else if (hr == E_NOINTERFACE)
    {

        // BUG: You'd have to check the registry to ensure that the proxy stub it's not actually there as opposed to the 
        // COM object QI simply returning a failure code.
    
    /*
        // Check if pInnerUnk is actually pointing to a proxy, i.e. that it is pointing to an address
        // within the loaded ole32.dll image.  Note that WszGetModuleHandle does not increment the 
        // ref count.
        HINSTANCE hModOle32 = WszGetModuleHandle(OLE32DLL);
        if (hModOle32 && IsIPInModule(hModOle32, (BYTE *)(*(BYTE **)(IUnknown*)pInnerUnk)))
        {
            pXml = AsMdaAssistant()->OutputTypeHandle(th, pXml->AddChild(MdaElemDecl(Type)));

            WCHAR strGuid[40];
            GuidToLPWSTR(iid, strGuid, 40);
            msg.SendMessagef(MDARC_FAILED_QI, strGuid);
        }
   */
    }
}

HRESULT MdaFailedQIAssistantCallback(LPVOID pData)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        SO_TOLERANT;
        PRECONDITION(CheckPointer(pData));
    }
    CONTRACTL_END;
    
    HRESULT                 hr = E_FAIL;

    BEGIN_EXTERNAL_ENTRYPOINT(&hr)
    {
        SafeComHolder<IUnknown> pDummyUnk;
        
        MdaFailedQIAssistantCallbackData    *pCallbackData = (MdaFailedQIAssistantCallbackData *)pData;
        
        // Initialize the fSuccess flag to false until we know for a fact the QI will succeed.
        pCallbackData->fSuccess = FALSE;
        
        // QI for the requested interface.
        hr = pCallbackData->pWrapper->SafeQueryInterfaceRemoteAware(pCallbackData->iid, &pDummyUnk);
        
        // If the QI call succeded then set the fSuccess flag to true.
        if (SUCCEEDED(hr))
            pCallbackData->fSuccess = TRUE;
    }
    END_EXTERNAL_ENTRYPOINT;
    
    return S_OK;        // Need to return S_OK so that the assert in CtxEntry::EnterContext() won't fire.
}

//
// MdaDisconnectedContext
//
void MdaDisconnectedContext::ReportViolationDisconnected(LPVOID context, HRESULT hr)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        SO_INTOLERANT;
    }
    CONTRACTL_END;

    if (g_fEEShutDown == 0)
    {
        MdaXmlElement* pXml;
        MdaXmlMessage msg(this->AsMdaAssistant(), TRUE, &pXml);

        StackSString strHRMsg;
        GetHRMsg(hr, strHRMsg);
        
        msg.SendMessagef(MDARC_DISCONNECTED_CONTEXT_1, context, strHRMsg.GetUnicode());
    }
}

void MdaDisconnectedContext::ReportViolationCleanup(LPVOID context1, LPVOID context2, HRESULT hr)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        SO_INTOLERANT;
    }
    CONTRACTL_END;

    if (g_fEEShutDown == 0)
    {
        EX_TRY
        {
            MdaXmlElement* pXml;
            MdaXmlMessage msg(this->AsMdaAssistant(), TRUE, &pXml);

            StackSString strHRMsg;
            GetHRMsg(hr, strHRMsg);

            msg.SendMessagef(MDARC_DISCONNECTED_CONTEXT_2, context1, strHRMsg.GetUnicode(), context2);        
        }
        EX_CATCH
        {
            // Caller cannot take exceptions.
        }
        EX_END_CATCH(SwallowAllExceptions)
    }
}


//
// MdaInvalidApartmentStateChange
//
void MdaInvalidApartmentStateChange::ReportViolation(Thread* pThread, Thread::ApartmentState newstate, BOOL fAlreadySet)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        SO_INTOLERANT;
    }
    CONTRACTL_END;
    
    EX_TRY
    {
        MdaXmlElement* pXml;
        MdaXmlMessage msg(this->AsMdaAssistant(), TRUE, &pXml);

        AsMdaAssistant()->OutputThread(pThread, pXml->AddChild(MdaElemDecl(Thread)));

        if (fAlreadySet)
        {
            if (newstate == Thread::AS_InSTA)
            {
                msg.SendMessagef(MDARC_INVALID_APT_STATE_CHANGE_SET, W("STA"), W("MTA"));
            }
            else
            {
                msg.SendMessagef(MDARC_INVALID_APT_STATE_CHANGE_SET, W("MTA"), W("STA"));
            }
        }
        else
        {
            if (newstate == Thread::AS_InSTA)
            {
                msg.SendMessagef(MDARC_INVALID_APT_STATE_CHANGE_NOTSET, W("STA"), W("MTA"));
            }
            else
            {
                msg.SendMessagef(MDARC_INVALID_APT_STATE_CHANGE_NOTSET, W("MTA"), W("STA"));
            }
        }
    }
    EX_CATCH
    {
        // Caller cannot take exceptions.
    }
    EX_END_CATCH(SwallowAllExceptions);
}
    
//
// MdaDllMainReturnsFalse
//
void MdaDllMainReturnsFalse::ReportError()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        SO_TOLERANT;
    }
    CONTRACTL_END;

    BEGIN_SO_INTOLERANT_CODE_NOTHROW(GetThread(), return);

    MdaXmlElement* pXml;
    MdaXmlMessage msg(this->AsMdaAssistant(), TRUE, &pXml);

    msg.SendMessagef(MDARC_DLLMAIN_RETURNS_FALSE);

    END_SO_INTOLERANT_CODE;
}

//
// MdaOverlappedFreeError
//
void MdaOverlappedFreeError::ReportError(LPVOID pOverlapped)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        SO_INTOLERANT;
    }
    CONTRACTL_END;

    MdaXmlElement* pXml;
    MdaXmlMessage msg(this->AsMdaAssistant(), TRUE, &pXml);

    msg.SendMessagef(MDARC_INVALID_OVERLAPPED_FREE,
            pOverlapped);
}

//
// MdaInvalidOverlappedToPinvoke
//

// NOTE: the overlapped pointer needs to be named "overlapped". 
// It is embedded in the (Args) and (ArgsUsed) sections.


#define CREATE_WRAPPER_FUNCTION(DllName, Return, Flags, Name, Args, ArgsUsed)                        \
    DllName##_##Name,
enum {
#include "invalidoverlappedwrappers.h"
};
#undef CREATE_WRAPPER_FUNCTION

#define CREATE_WRAPPER_FUNCTION(DllName, Return, Flags, Name, Args, ArgsUsed)                               \
Return Flags Mda_##Name Args                                                                                \
{                                                                                                           \
    CONTRACTL                                                                                               \
    {                                                                                                       \
        THROWS;                                                                                             \
        GC_TRIGGERS;                                                                                        \
        SO_TOLERANT;                                                                                        \
        MODE_ANY;                                                                                           \
    }                                                                                                       \
    CONTRACTL_END;                                                                                          \
    Return (Flags *old##Name) Args;                                                                         \
    MdaInvalidOverlappedToPinvoke *pOverlapCheck = MDA_GET_ASSISTANT(InvalidOverlappedToPinvoke);           \
    _ASSERTE(pOverlapCheck);                                                                                \
    *(PVOID *)&(old##Name) = pOverlapCheck->CheckOverlappedPointer(DllName##_##Name, (LPVOID) overlapped);  \
    return old##Name ArgsUsed;                                                                              \
}
#include "invalidoverlappedwrappers.h"
#undef CREATE_WRAPPER_FUNCTION

#define CREATE_WRAPPER_FUNCTION(DllName, Return, Flags, Name, Args, ArgsUsed)    \
    { L#DllName W(".DLL"), L#Name, Mda_##Name, NULL, NULL },
static MdaInvalidOverlappedToPinvoke::pinvoke_entry PInvokeTable[] = {
#include "invalidoverlappedwrappers.h"
};
#undef CREATE_WRAPPER_FUNCTION

void MdaInvalidOverlappedToPinvoke::Initialize(MdaXmlElement* pXmlInput)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

// TODO: CONTRACT_VIOLATION(SOToleranceViolation);

    m_entries = PInvokeTable;
    m_entryCount = sizeof(PInvokeTable) / sizeof(pinvoke_entry);
    m_bJustMyCode = pXmlInput->GetAttributeValueAsBool(MdaAttrDecl(JustMyCode));    
}

BOOL MdaInvalidOverlappedToPinvoke::InitializeModuleFunctions(HINSTANCE hmod)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        SO_TOLERANT;
    }
    CONTRACTL_END;

    // For every entry where m_moduleName matches moduleName, fill in m_hmod with hmod
    // and fill in the m_realFunction pointer.

    BOOL bFoundSomething = FALSE;

    BEGIN_SO_INTOLERANT_CODE_NOTHROW(GetThread(), return FALSE);

    SString moduleNameFullPath, moduleName;
    ClrGetModuleFileNameNoThrow(hmod,moduleNameFullPath);
    // Strip any path info
    SString::CIterator iM = moduleNameFullPath.End();
    if (moduleNameFullPath.FindBack(iM, W('\\')))
    {
        iM++;
        moduleName.Set(moduleNameFullPath, iM, moduleNameFullPath.End());
    }

    for (UINT i=0; i<m_entryCount; i++)
    {
        if (SString::_wcsicmp(m_entries[i].m_moduleName, moduleName.GetUnicode()) == 0)
        {
            if (m_entries[i].m_hmod == NULL)
            {
                SString moduleNameForLookup(m_entries[i].m_functionName);
                StackScratchBuffer ansiVersion;                
                m_entries[i].m_realFunction = GetProcAddress(hmod, moduleNameForLookup.GetANSI(ansiVersion));
                m_entries[i].m_hmod = hmod;                
            }
            bFoundSomething = TRUE;
        }
    }

    END_SO_INTOLERANT_CODE;

    return bFoundSomething;
}

LPVOID MdaInvalidOverlappedToPinvoke::CheckOverlappedPointer(UINT index, LPVOID pOverlapped)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        SO_TOLERANT;
    }
    CONTRACTL_END;

    MdaInvalidOverlappedToPinvoke::pinvoke_entry *pEntry = m_entries + index;

    // pEntry should always be non-NULL, because we got the address of pvMdaFunction
    // from the entries table in the first place.
    _ASSERTE(pEntry);
    if (pEntry == NULL)
    {
        return NULL;
    }

    // Is the overlapped pointer in the gc heap?
    if (pOverlapped != NULL)
    {
        // If a stack overflow occurs, we would just want to continue and
        // return the function pointer as expected.
        BEGIN_SO_INTOLERANT_CODE_NOTHROW(GetThread(), return pEntry->m_realFunction);

        BOOL fHeapPointer;

        {
            GCX_COOP();
            GCHeap *pHeap = GCHeap::GetGCHeap();
            fHeapPointer = pHeap->IsHeapPointer(pOverlapped);
        }

        if (!fHeapPointer)
        {
            // Output a message
            MdaXmlElement* pXml;
            MdaXmlMessage msg(this->AsMdaAssistant(), TRUE, &pXml);

            msg.SendMessagef(MDARC_INVALID_OVERLAPPED_TO_PINVOKE, 
                pOverlapped, 
                pEntry->m_functionName,
                pEntry->m_moduleName);
        }

        END_SO_INTOLERANT_CODE;
    }
    
    return pEntry->m_realFunction;
}

// We want to hook the functions where it is in the user's code only, unless
// the attribute JustMyCode is set to false. In that case, we want all 
// occurances.
BOOL MdaInvalidOverlappedToPinvoke::ShouldHook(MethodDesc *pMD)
{
    LIMITED_METHOD_CONTRACT;
    return (m_bJustMyCode ? IsJustMyCode(pMD) : TRUE);
}

LPVOID MdaInvalidOverlappedToPinvoke::Register(HINSTANCE hmod,LPVOID pvTarget)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        SO_TOLERANT;
    }
    CONTRACTL_END;

    // Quick lookup - do we have a matching target?
    // walk our entries, looking for a match.
    BOOL bNullModules = FALSE;
    BOOL bSeenThisModule = FALSE;
    
    for (UINT i=0; i<m_entryCount; i++)
    {
        MdaInvalidOverlappedToPinvoke::pinvoke_entry *pEntry = m_entries + i;
        if (pvTarget == pEntry->m_realFunction)
        {
            return pEntry->m_mdaFunction;
        }
        
        bNullModules |= (pEntry->m_hmod == NULL);
        bSeenThisModule |= (pEntry->m_hmod == hmod);
    }
    
    // if we have some NULL targets, do we have a matching hmod?
    // if so, 
    if (bNullModules && !bSeenThisModule)
    {
        if (InitializeModuleFunctions(hmod))
        {
            // Search once more
            for (UINT i=0; i<m_entryCount; i++)
            {
                pinvoke_entry *pEntry = m_entries + i;
                if (pvTarget == pEntry->m_realFunction)
                {
                    return pEntry->m_mdaFunction;
                }                
            }
        }
    }

    return NULL;
}

//
// MdaPInvokeLog
//
BOOL MdaPInvokeLog::Filter(SString& sszDllName)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        SO_INTOLERANT;
    }
    CONTRACTL_END;

    MdaXmlElement* pXmlFilter = m_pXmlInput->GetChild(MdaElemDecl(Filter));  
    if (!pXmlFilter)
        return TRUE;

    BOOL bFound = FALSE;
    for (COUNT_T i = 0; i < pXmlFilter->GetChildren().GetCount(); i ++)
    {
        if (pXmlFilter->GetChildren()[i]->GetAttribute(MdaAttrDecl(DllName))->GetValueAsCSString()->EqualsCaseInsensitive(sszDllName))
        {
            bFound = TRUE;
            break;
        }
    }

   return bFound;
}

void MdaPInvokeLog::LogPInvoke(NDirectMethodDesc* pMD, HINSTANCE hMod)
{   
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        SO_INTOLERANT;
    }
    CONTRACTL_END;

    EX_TRY
    {
        StackSString sszEntryPoint;
        sszEntryPoint.SetUTF8(pMD->GetEntrypointName());

        PathString szDllFullName ;
        WCHAR szDrive[_MAX_PATH] = {0};
        WCHAR szPath[_MAX_PATH] = {0};
        WCHAR szFileName[_MAX_PATH] = {0};
        WCHAR szExt[_MAX_PATH] = {0};
        WszGetModuleFileName(hMod, szDllFullName);      
        SplitPath(szDllFullName, szDrive, _MAX_PATH, szPath, _MAX_PATH, szFileName, _MAX_PATH, szExt, _MAX_PATH);

        StackSString sszDllName;
        sszDllName.Append(szFileName);
        if (szExt)
            sszDllName.Append(szExt);

        if (Filter(sszDllName))
        {
            MdaXmlElement* pXml;
            MdaXmlMessage msg(this->AsMdaAssistant(), FALSE, &pXml);
            
            MdaXmlElement* pMethod = pXml->AddChild(MdaElemDecl(Method));
            AsMdaAssistant()->OutputMethodDesc(pMD, pMethod);

            MdaXmlElement* pDllImport = pXml->AddChild(MdaElemDecl(DllImport));
            pDllImport->AddAttributeSz(MdaAttrDecl(DllName), szDllFullName);
            pDllImport->AddAttributeSz(MdaAttrDecl(EntryPoint), sszEntryPoint.GetUnicode());

            msg.SendMessage();    
        }
    }
    EX_CATCH
    {
        // Caller cannot take exceptions.
    }
    EX_END_CATCH(SwallowAllExceptions);
}


#ifdef _TARGET_X86_    
//
// MdaPInvokeStackImbalance
//
void MdaPInvokeStackImbalance::CheckStack(StackImbalanceCookie *pSICookie, DWORD dwPostEsp)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        SO_TOLERANT;
    }
    CONTRACTL_END;

    DWORD dwEspAfterPushedArgs = pSICookie->m_dwSavedEsp;
    DWORD dwEspBeforePushedArgs = dwEspAfterPushedArgs + pSICookie->m_dwStackArgSize;
    BOOL bStackImbalance = false;

    // Note: We use relaxed rules here depending on the NetFx40_PInvokeStackResilience compat switch in order to mimic 2.0 behavior.
    switch (pSICookie->m_callConv & pmCallConvMask)
    {
        // Caller cleans stack
        case pmCallConvCdecl:
            if (dwPostEsp != dwEspAfterPushedArgs)
            {
                if (dwPostEsp != dwEspBeforePushedArgs)
                {
                    bStackImbalance = true;
                }
                else
                {
                    // If NetFx40_PInvokeStackResilience is on, ignore the case where we see that the callee cleaned the stack.
                    BOOL fPreV4Method = pSICookie->m_pMD->GetModule()->IsPreV4Assembly();
                    if (!g_pConfig->PInvokeRestoreEsp(fPreV4Method))
                        bStackImbalance = true;
                }
            }
            break;

        // Callee cleans stack
        case pmCallConvThiscall:
        case pmCallConvWinapi:
        case pmCallConvStdcall:
            if (dwPostEsp != dwEspBeforePushedArgs)
            {
                if (dwPostEsp != dwEspAfterPushedArgs)
                {
                    bStackImbalance = true;
                }
                else
                {
                    // If NetFx40_PInvokeStackResilience is on, ignore the case where we see that the callee did not clean the stack
                    BOOL fPreV4Method = pSICookie->m_pMD->GetModule()->IsPreV4Assembly();
                    if (!g_pConfig->PInvokeRestoreEsp(fPreV4Method))
                        bStackImbalance = true;
                }
            }
            break;

        // Unsupported calling convention
        case pmCallConvFastcall:
        default:
            _ASSERTE(!"Unsupported calling convention");
            break;
    }
        
    if (!bStackImbalance)
        return;

    BEGIN_SO_INTOLERANT_CODE(GetThread());

    MdaXmlElement* pXml;
    MdaXmlMessage msg(this->AsMdaAssistant(), TRUE, &pXml);
    MdaXmlElement* pMethod = pXml->AddChild(MdaElemDecl(Method));
    AsMdaAssistant()->OutputMethodDesc(pSICookie->m_pMD, pMethod);
    
    StackSString sszMethodName;
    msg.SendMessagef(MDARC_PINVOKE_SIGNATURE_MISMATCH, AsMdaAssistant()->ToString(sszMethodName, pSICookie->m_pMD).GetUnicode());

    END_SO_INTOLERANT_CODE;
}
#endif

    
//
// MdaJitCompilationStart
//
void MdaJitCompilationStart::Initialize(MdaXmlElement* pXmlInput)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        SO_INTOLERANT;
    }
    CONTRACTL_END;

    m_bBreak = pXmlInput->GetAttributeValueAsBool(MdaAttrDecl(Break));
    MdaXmlElement* pXmlMethodFilter = pXmlInput->GetChild(MdaElemDecl(Methods));
    m_pMethodFilter = NULL;

    if (pXmlMethodFilter)
    {
        m_pMethodFilter = new MdaQuery::CompiledQueries();
        MdaQuery::Compile(pXmlMethodFilter, m_pMethodFilter);
    }
}

void MdaJitCompilationStart::NowCompiling(MethodDesc* pMD)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        SO_INTOLERANT;
    }
    CONTRACTL_END;

    if (m_pMethodFilter && !m_pMethodFilter->Test(pMD))
        return;

    MdaXmlElement* pXml;
    MdaXmlMessage msg(this->AsMdaAssistant(), m_bBreak, &pXml);
    
    MdaXmlElement* pMethod = pXml->AddChild(MdaElemDecl(Method));
    AsMdaAssistant()->OutputMethodDesc(pMD, pMethod);
    
    msg.SendMessage();
}

//
// MdaLoadFromContext
//
void MdaLoadFromContext::NowLoading(IAssembly** ppIAssembly, StackCrawlMark *pCallerStackMark)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        SO_INTOLERANT;
    }
    CONTRACTL_END;

    if (ppIAssembly && *ppIAssembly) {

        // Send an MDA if this assembly was loaded in the LoadFrom context
        if ((*ppIAssembly)->GetFusionLoadContext() == LOADCTX_TYPE_LOADFROM) {
             // Apply MDA filtering
            if (g_pDebugInterface && pCallerStackMark && ManagedDebuggingAssistants::IsManagedDebuggerAttached()) {
                MethodDesc *pMethodDesc = NULL;
                {
                    GCX_COOP();
                    pMethodDesc = SystemDomain::GetCallersMethod(pCallerStackMark, NULL);
                }
                if (pMethodDesc && !g_pDebugInterface->IsJMCMethod(pMethodDesc->GetModule(), pMethodDesc->GetMemberDef()))
                    return;
            }

            MdaXmlElement *pXml;
            MdaXmlMessage msg(this->AsMdaAssistant(), TRUE, &pXml);

            MdaXmlElement *pXmlAssembly = pXml->AddChild(MdaElemDecl(AssemblyInfo));
            
            StackSString sszAssemblyName;
            StackSString sszCodeBase;
            SafeComHolder<IAssemblyName> pNameDef;

            if (FAILED((*ppIAssembly)->GetAssemblyNameDef(&pNameDef))) {
                return;
            }

            if ((!FusionBind::GetAssemblyNameStringProperty(pNameDef, ASM_NAME_NAME, sszAssemblyName)) ||
                (!FusionBind::GetAssemblyNameStringProperty(pNameDef, ASM_NAME_CODEBASE_URL, sszCodeBase))) {
                return;
            }
            
            pXmlAssembly->AddAttributeSz(MdaAttrDecl(DisplayName), sszAssemblyName.GetUnicode());
            pXmlAssembly->AddAttributeSz(MdaAttrDecl(CodeBase), sszCodeBase.GetUnicode());
                   
            msg.SendMessagef(MDARC_LOAD_FROM_CONTEXT, sszAssemblyName.GetUnicode(), sszCodeBase.GetUnicode());
        }
    }
}

const LPCWSTR ContextIdName[] =
{
    W("Load"),
    W("LoadFrom"),
    W("Anonymous")
};

//
// MdaBindingFailure
//
void MdaBindingFailure::BindFailed(AssemblySpec *pSpec, OBJECTREF *pExceptionObj)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        SO_INTOLERANT;
    }
    CONTRACTL_END;

    MdaXmlElement *pXml;
    MdaXmlMessage msg(this->AsMdaAssistant(), TRUE, &pXml);

    MdaXmlElement *pXmlAssembly = pXml->AddChild(MdaElemDecl(AssemblyInfo));

    DWORD dwAppDomainId;
    SString sszAssemblyName;
    SString sszCodeBase;
    SString sszMessage;
    int iBindingContext;
    HRESULT hr;

    // determine AppDomain ID
    AppDomain *appDomain = pSpec->GetAppDomain();
    if (appDomain) {     
        dwAppDomainId = appDomain->GetId().m_dwId;
    } else {
        dwAppDomainId = 0;
    }
    pXmlAssembly->AddAttributeInt(MdaAttrDecl(AppDomainId), dwAppDomainId);

    // determine Assembly display name
    LPCSTR assemblyName = pSpec->GetName();
    if (assemblyName && assemblyName[0]) {
        sszAssemblyName.SetASCII(assemblyName);
    }
    pXmlAssembly->AddAttributeSz(MdaAttrDecl(DisplayName), sszAssemblyName.GetUnicode());

    // determine Assembly code base
    if (pSpec->GetCodeBase() && pSpec->GetCodeBase()[0]) {
        sszCodeBase.Set(pSpec->GetCodeBase());
    }
    pXmlAssembly->AddAttributeSz(MdaAttrDecl(CodeBase), sszCodeBase.GetUnicode());

    // retrieve the exception message.
    GetExceptionMessage(*pExceptionObj, sszMessage);

    // determine failing HRESULT  
    hr = GetExceptionHResult(*pExceptionObj);
    pXmlAssembly->AddAttributeInt(MdaAttrDecl(HResult), hr);

    // determine binding context Assembly would have been loaded in (based on parent)
    IAssembly* pParentAssembly = pSpec->GetParentIAssembly();
    if (pParentAssembly) {
        iBindingContext = pParentAssembly->GetFusionLoadContext();
    } else {

        // if the parent hasn't been set but the code base has, it's in LoadFrom
        iBindingContext = LOADCTX_TYPE_LOADFROM;
    }
    pXmlAssembly->AddAttributeInt(MdaAttrDecl(BindingContextId), iBindingContext);

    // Make sure the binding context ID isn't larger then our ID to name lookup table.
    _ASSERTE(iBindingContext < COUNTOF(ContextIdName));
    
    if (sszAssemblyName.IsEmpty())
    {
        _ASSERTE(!sszCodeBase.IsEmpty());
        msg.SendMessagef(MDARC_BINDING_FAILURE_CODEBASE_ONLY, sszCodeBase.GetUnicode(), 
                         ContextIdName[iBindingContext], dwAppDomainId, sszMessage.GetUnicode());
    }
    else if (sszCodeBase.IsEmpty())
    {
        _ASSERTE(!sszAssemblyName.IsEmpty());
        msg.SendMessagef(MDARC_BINDING_FAILURE_DISPLAYNAME_ONLY, sszAssemblyName.GetUnicode(),  
                         ContextIdName[iBindingContext], dwAppDomainId, sszMessage.GetUnicode());
    }
    else 
    {
        msg.SendMessagef(MDARC_BINDING_FAILURE, sszAssemblyName.GetUnicode(), sszCodeBase.GetUnicode(), 
                         ContextIdName[iBindingContext], dwAppDomainId, sszMessage.GetUnicode());
    }
}


//
// MdaReflection
//
FCIMPL0(void, MdaManagedSupport::MemberInfoCacheCreation)
{
    FCALL_CONTRACT;

    HELPER_METHOD_FRAME_BEGIN_0();
    {
        MdaMemberInfoCacheCreation* pMda = MDA_GET_ASSISTANT(MemberInfoCacheCreation);
        if (pMda)
        {
            pMda->MemberInfoCacheCreation();
        }
    }
    HELPER_METHOD_FRAME_END();
}
FCIMPLEND

void MdaMemberInfoCacheCreation::MemberInfoCacheCreation()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        SO_INTOLERANT;
    }
    CONTRACTL_END;

    MdaXmlElement* pXml;
    MdaXmlMessage msg(this->AsMdaAssistant(), TRUE, &pXml);
    
    msg.SendMessage(MDARC_REFLECTION_PERFORMANCE_MEMBERINFOCACHECREATION);
}


FCIMPL0(FC_BOOL_RET, MdaManagedSupport::IsStreamWriterBufferedDataLostEnabled)
{
    FCALL_CONTRACT;

    // To see if it's enabled, allocate one then throw it away.
    MdaStreamWriterBufferedDataLost* pMda = MDA_GET_ASSISTANT(StreamWriterBufferedDataLost);
        
    FC_RETURN_BOOL(pMda != NULL);
}
FCIMPLEND

FCIMPL0(FC_BOOL_RET, MdaManagedSupport::IsStreamWriterBufferedDataLostCaptureAllocatedCallStack)
{
    FCALL_CONTRACT;

    // To see if it's enabled, allocate one then throw it away.
    MdaStreamWriterBufferedDataLost* pMda = MDA_GET_ASSISTANT(StreamWriterBufferedDataLost);
        
    FC_RETURN_BOOL((pMda != NULL) && (pMda->CaptureAllocatedCallStack()));
}
FCIMPLEND

FCIMPL1(void, MdaManagedSupport::ReportStreamWriterBufferedDataLost, StringObject * stringRef)
{
    FCALL_CONTRACT;

    STRINGREF str(stringRef);
    MdaStreamWriterBufferedDataLost* pMda = MDA_GET_ASSISTANT(StreamWriterBufferedDataLost);
    if (pMda)
    {
        HELPER_METHOD_FRAME_BEGIN_1(str);
        StackSString message(str->GetBuffer());
        pMda->ReportError(message);
        HELPER_METHOD_FRAME_END();
    }   
}
FCIMPLEND

FCIMPL0(FC_BOOL_RET, MdaManagedSupport::IsInvalidGCHandleCookieProbeEnabled)
{
    FCALL_CONTRACT;

    // To see if it's enabled, allocate one then throw it away.
    MdaInvalidGCHandleCookie* pMda = MDA_GET_ASSISTANT(InvalidGCHandleCookie);
        
    FC_RETURN_BOOL(pMda != NULL);
}
FCIMPLEND

FCIMPL1(void, MdaManagedSupport::FireInvalidGCHandleCookieProbe, LPVOID cookie)
{
    FCALL_CONTRACT;

    MdaInvalidGCHandleCookie* pMda = MDA_GET_ASSISTANT(InvalidGCHandleCookie);
    if (pMda)
    {
        HELPER_METHOD_FRAME_BEGIN_0();
        pMda->ReportError(cookie);
        HELPER_METHOD_FRAME_END();
    }
}
FCIMPLEND

FCIMPL1(void, MdaManagedSupport::ReportErrorSafeHandleRelease, ExceptionObject * exceptionRef)
{
    FCALL_CONTRACT;

    OBJECTREF exception(exceptionRef);
    MdaMarshalCleanupError* pMda = MDA_GET_ASSISTANT(MarshalCleanupError);
    if (pMda)
    {
        HELPER_METHOD_FRAME_BEGIN_1(exception);
        pMda->ReportErrorSafeHandleRelease(&exception);
        HELPER_METHOD_FRAME_END();
    }
}
FCIMPLEND

void MdaInvalidGCHandleCookie::ReportError(LPVOID cookie)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        SO_INTOLERANT;
    }
    CONTRACTL_END;

    MdaXmlElement* pXml;
    MdaXmlMessage msg(this->AsMdaAssistant(), TRUE, &pXml);

    msg.SendMessagef(MDARC_INVALID_GCHANDLE_COOKIE, cookie);
}

void MdaStreamWriterBufferedDataLost::ReportError(SString text)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        SO_INTOLERANT;
    }
    CONTRACTL_END;
    
    MdaXmlElement* pXml;
    MdaXmlMessage msg(this->AsMdaAssistant(), TRUE, &pXml);
    
    msg.SendMessage(text);
}


//
// MdaNotMarshalable
//
void MdaNotMarshalable::ReportViolation()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        SO_INTOLERANT;
    }
    CONTRACTL_END;

    MdaXmlElement* pXml;
    MdaXmlMessage msg(this->AsMdaAssistant(), TRUE, &pXml);

    msg.SendMessagef(MDARC_NOTMARSHALABLE);
}


//
// MdaMarshalCleanupError
//
void MdaMarshalCleanupError::ReportErrorThreadCulture(OBJECTREF *pExceptionObj)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        SO_INTOLERANT;
    }
    CONTRACTL_END;

    EX_TRY
    {
        MdaXmlElement* pXml;
        MdaXmlMessage msg(this->AsMdaAssistant(), TRUE, &pXml);

        // retrieve the exception message.
        SString sszMessage;
        GetExceptionMessage(*pExceptionObj, sszMessage);

        msg.SendMessagef(MDARC_MARSHALCLEANUPERROR_THREADCULTURE, sszMessage.GetUnicode());
    }
    EX_CATCH
    {
    }
    EX_END_CATCH(SwallowAllExceptions);
}

void MdaMarshalCleanupError::ReportErrorSafeHandleRelease(OBJECTREF *pExceptionObj)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        SO_INTOLERANT;
    }
    CONTRACTL_END;

    EX_TRY
    {
        MdaXmlElement* pXml;
        MdaXmlMessage msg(this->AsMdaAssistant(), TRUE, &pXml);

        // retrieve the exception message.
        SString sszMessage;
        GetExceptionMessage(*pExceptionObj, sszMessage);

        msg.SendMessagef(MDARC_MARSHALCLEANUPERROR_SAFEHANDLERELEASE, sszMessage.GetUnicode());
    }
    EX_CATCH
    {
    }
    EX_END_CATCH(SwallowAllExceptions);
}

void MdaMarshalCleanupError::ReportErrorSafeHandleProp(OBJECTREF *pExceptionObj)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        SO_INTOLERANT;
    }
    CONTRACTL_END;

    EX_TRY
    {
        MdaXmlElement* pXml;
        MdaXmlMessage msg(this->AsMdaAssistant(), TRUE, &pXml);

        // retrieve the exception message.
        SString sszMessage;
        GetExceptionMessage(*pExceptionObj, sszMessage);

        msg.SendMessagef(MDARC_MARSHALCLEANUPERROR_SAFEHANDLEPROP, sszMessage.GetUnicode());
    }
    EX_CATCH
    {
    }
    EX_END_CATCH(SwallowAllExceptions);
}

void MdaMarshalCleanupError::ReportErrorCustomMarshalerCleanup(TypeHandle typeCustomMarshaler, OBJECTREF *pExceptionObj)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        SO_INTOLERANT;
    }
    CONTRACTL_END;

    EX_TRY
    {
            
        MdaXmlElement* pXml;
        MdaXmlMessage msg(this->AsMdaAssistant(), TRUE, &pXml);

        // retrieve the exception message.
        SString sszMessage;
        GetExceptionMessage(*pExceptionObj, sszMessage);

        // Retrieve the type name.
        StackSString sszType;
        typeCustomMarshaler.GetName(sszType);

        msg.SendMessagef(MDARC_MARSHALCLEANUPERROR_CUSTOMCLEANUP, sszType.GetUnicode(), sszMessage.GetUnicode());
    }
    EX_CATCH
    {
    }
    EX_END_CATCH(SwallowAllExceptions);
}

//
// MdaMarshaling
//
void MdaMarshaling::Initialize(MdaXmlElement* pXmlInput)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        SO_INTOLERANT;
    }
    CONTRACTL_END;
    
    m_pMethodFilter = new MdaQuery::CompiledQueries();
    m_pFieldFilter = new MdaQuery::CompiledQueries();

    MdaXmlElement* pXmlMethodFilter = pXmlInput->GetChild(MdaElemDecl(MethodFilter));
    if (pXmlMethodFilter)
        MdaQuery::Compile(pXmlMethodFilter, m_pMethodFilter);

    MdaXmlElement* pXmlFieldFilter = pXmlInput->GetChild(MdaElemDecl(FieldFilter));
    if (pXmlFieldFilter)
        MdaQuery::Compile(pXmlFieldFilter, m_pFieldFilter);
}

void MdaMarshaling::ReportFieldMarshal(FieldMarshaler* pFM)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        SO_INTOLERANT;
        PRECONDITION(CheckPointer(pFM));
    }
    CONTRACTL_END;

    FieldDesc* pFD = pFM->GetFieldDesc();

    if (!pFD || !m_pFieldFilter->Test(pFD))
        return;

    MdaXmlElement* pXml;
    MdaXmlMessage msg(this->AsMdaAssistant(), FALSE, &pXml);
    
    MdaXmlElement* pField = pXml->AddChild(MdaElemDecl(MarshalingField));
    AsMdaAssistant()->OutputFieldDesc(pFD, pField);

    StackSString sszField;
    SString managed;
    SString unmanaged;
        
    GetManagedSideForField(managed, pFD);
    GetUnmanagedSideForField(unmanaged, pFM);

    msg.SendMessagef(MDARC_MARSHALING_FIELD, AsMdaAssistant()->ToString(sszField, pFD).GetUnicode(), managed.GetUnicode(), unmanaged.GetUnicode());
}


void MdaMarshaling::GetManagedSideForField(SString& strManagedMarshalType, FieldDesc* pFD)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        SO_INTOLERANT;
    }
    CONTRACTL_END;

    if (!CheckForPrimitiveType(pFD->GetFieldType(), strManagedMarshalType))
    {
        // The following workaround is added to avoid a recursion caused by calling GetTypeHandle on
        // the m_value field of the UIntPtr class.
        LPCUTF8 szNamespace, szClassName;
        IfFailThrow(pFD->GetMDImport()->GetNameOfTypeDef(pFD->GetApproxEnclosingMethodTable()->GetCl(), &szClassName, &szNamespace));
        
        if (strcmp(szNamespace, "System") == 0 && strcmp(szClassName, "UIntPtr") == 0)
        {
            static LPWSTR strRetVal = W("Void*");
            strManagedMarshalType.Set(strRetVal);
        }
        else
        {
            MetaSig fSig(pFD);
            fSig.NextArgNormalized();
            TypeHandle th = fSig.GetLastTypeHandleNT();
            if (th.IsNull())
            {
                static const WCHAR strErrorMsg[] = W("<error>");
                strManagedMarshalType.Set(strErrorMsg);
            }
            else
            {
                SigFormat sigFmt;
                sigFmt.AddType(th);
                UINT iManagedTypeLen = (UINT)strlen(sigFmt.GetCString()) + 1;

                WCHAR* buffer = strManagedMarshalType.OpenUnicodeBuffer(iManagedTypeLen);
                MultiByteToWideChar(CP_ACP, MB_PRECOMPOSED, sigFmt.GetCString(), -1, buffer, iManagedTypeLen);
                strManagedMarshalType.CloseBuffer();
            }
        }
    }
}

void MdaMarshaling::GetUnmanagedSideForField(SString& strUnmanagedMarshalType, FieldMarshaler* pFM)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
        SO_INTOLERANT;
    }
    CONTRACTL_END;

    NStructFieldTypeToString(pFM, strUnmanagedMarshalType);
}


void MdaMarshaling::GetManagedSideForMethod(SString& strManagedMarshalType, Module* pModule, SigPointer sig, CorElementType elemType)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        SO_INTOLERANT;
    }
    CONTRACTL_END;

    if (!CheckForPrimitiveType(elemType, strManagedMarshalType))
    {        
        // an empty type context is sufficient: all methods should be non-generic
        SigTypeContext emptyTypeContext;

        TypeHandle th = sig.GetTypeHandleNT(pModule, &emptyTypeContext);
        if (th.IsNull())
        {
            strManagedMarshalType.Set(W("<error>"));
        }
        else
        {
            SigFormat sigfmt;
            sigfmt.AddType(th);
            UINT iManagedMarshalTypeLength = MultiByteToWideChar( CP_ACP, MB_PRECOMPOSED, sigfmt.GetCString(), -1, NULL, 0);
                
            WCHAR* str = strManagedMarshalType.OpenUnicodeBuffer(iManagedMarshalTypeLength);
            MultiByteToWideChar( CP_ACP, MB_PRECOMPOSED, sigfmt.GetCString(), -1, str, iManagedMarshalTypeLength);
            strManagedMarshalType.CloseBuffer();
        }
    }  
}


void MdaMarshaling::GetUnmanagedSideForMethod(SString& strNativeMarshalType, MarshalInfo* mi, BOOL fSizeIsSpecified)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        SO_INTOLERANT;
    }
    CONTRACTL_END;
    
    mi->MarshalTypeToString(strNativeMarshalType, fSizeIsSpecified);    
}

BOOL MdaMarshaling::CheckForPrimitiveType(CorElementType elemType, SString& strPrimitiveType)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
        SO_INTOLERANT;
        INJECT_FAULT(COMPlusThrowOM());
    }
    CONTRACTL_END;
    
    LPWSTR  strRetVal;

    switch (elemType)
    {
        case ELEMENT_TYPE_VOID:
            strRetVal = W("Void");
            break;
        case ELEMENT_TYPE_BOOLEAN:
            strRetVal = W("Boolean");
            break;
        case ELEMENT_TYPE_I1:
            strRetVal = W("SByte");
            break;
        case ELEMENT_TYPE_U1:
            strRetVal = W("Byte");
            break;
        case ELEMENT_TYPE_I2:
            strRetVal = W("Int16");
            break;
        case ELEMENT_TYPE_U2:
            strRetVal = W("UInt16");
            break;
        case ELEMENT_TYPE_CHAR:
            strRetVal = W("Char");
            break;
        case ELEMENT_TYPE_I:
            strRetVal = W("IntPtr");
            break;
        case ELEMENT_TYPE_U:
            strRetVal = W("UIntPtr");
            break;
        case ELEMENT_TYPE_I4:
            strRetVal = W("Int32"); 
            break;
        case ELEMENT_TYPE_U4:       
            strRetVal = W("UInt32"); 
            break;
        case ELEMENT_TYPE_I8:       
            strRetVal = W("Int64"); 
            break;
        case ELEMENT_TYPE_U8:       
            strRetVal = W("UInt64"); 
            break;
        case ELEMENT_TYPE_R4:       
            strRetVal = W("Single"); 
            break;
        case ELEMENT_TYPE_R8:       
            strRetVal = W("Double"); 
            break;
        default:
            return FALSE;
    }

    strPrimitiveType.Set(strRetVal);
    return TRUE;
}

//
// MdaLoaderLock
//
void MdaLoaderLock::ReportViolation(HINSTANCE hInst)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        // Called from SO_TOLERANT CODE
        SO_TOLERANT;
    }
    CONTRACTL_END;

    BEGIN_SO_INTOLERANT_CODE_NOTHROW(GetThread(), return);

    EX_TRY
    {
        MdaXmlElement* pXml;
        MdaXmlMessage msg(this->AsMdaAssistant(), TRUE, &pXml);

        DWORD cName = 0;
        PathString szName;
        if (hInst)
        {
            cName = WszGetModuleFileName(hInst, szName);
        }

        if (cName)
        {
            msg.SendMessagef(MDARC_LOADER_LOCK_DLL, szName);
        }
        else
        {
            msg.SendMessagef(MDARC_LOADER_LOCK);
        }
    }
    EX_CATCH
    {
        // Caller cannot take exceptions.
    }
    EX_END_CATCH(SwallowAllExceptions);

    END_SO_INTOLERANT_CODE;
}


//
// MdaReentrancy
//
void MdaReentrancy::ReportViolation()
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        SO_TOLERANT;
    }
    CONTRACTL_END;

    BEGIN_SO_INTOLERANT_CODE_NOTHROW(GetThread(), return);

    EX_TRY
    {
        MdaXmlElement* pXml;
        MdaXmlMessage msg(this->AsMdaAssistant(), TRUE, &pXml);

        msg.SendMessagef(MDARC_REENTRANCY);
    }
    EX_CATCH
    {
        // Caller cannot take exceptions.
    }
    EX_END_CATCH(SwallowAllExceptions);

    END_SO_INTOLERANT_CODE;
}

//
// MdaAsynchronousThreadAbort
//
void MdaAsynchronousThreadAbort::ReportViolation(Thread *pCallingThread, Thread *pAbortedThread)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        SO_INTOLERANT;
    }
    CONTRACTL_END;

    EX_TRY
    {
        MdaXmlElement* pXml;
        MdaXmlMessage msg(this->AsMdaAssistant(), TRUE, &pXml);

        AsMdaAssistant()->OutputThread(pCallingThread, pXml->AddChild(MdaElemDecl(CallingThread)));
        AsMdaAssistant()->OutputThread(pAbortedThread, pXml->AddChild(MdaElemDecl(AbortedThread)));

        msg.SendMessagef(MDARC_ASYNCHRONOUS_THREADABORT, pCallingThread->GetOSThreadId(), pAbortedThread->GetOSThreadId());
    }
    EX_CATCH
    {
        // Caller cannot take exceptions.
    }
    EX_END_CATCH(SwallowAllExceptions);
}

    
//
// MdaAsynchronousThreadAbort
//
void MdaDangerousThreadingAPI::ReportViolation(__in_z WCHAR *apiName)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        SO_INTOLERANT;
    }
    CONTRACTL_END;

    MdaXmlElement* pXml;
    MdaXmlMessage msg(this->AsMdaAssistant(), TRUE, &pXml);

    msg.SendMessagef(MDARC_DANGEROUS_THREADINGAPI, apiName);
}

    
//
// MdaReportAvOnComRelease
//

void MdaReportAvOnComRelease::ReportHandledException(RCW* pRCW)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        SO_INTOLERANT;
    }
    CONTRACTL_END;

    EX_TRY
    {
        FAULT_NOT_FATAL();
    
        // TODO: comment this code...
        MdaXmlElement* pXml;
        MdaXmlMessage msg(this->AsMdaAssistant(), TRUE, &pXml);

        if (pRCW)
        {
            LPVOID vtablePtr = pRCW->GetVTablePtr();
            msg.SendMessagef(MDARC_REPORT_AV_ON_COM_RELEASE_WITH_VTABLE, vtablePtr);
        }
        else
        {
            msg.SendMessagef(MDARC_REPORT_AV_ON_COM_RELEASE);
        }
    }
    EX_CATCH
    {
        // Caller cannot take exceptions.
    }
    EX_END_CATCH(SwallowAllExceptions);
}

void MdaInvalidFunctionPointerInDelegate::ReportViolation(LPVOID pFunc)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        SO_INTOLERANT;
    }
    CONTRACTL_END;

    MdaXmlElement* pXml;
    MdaXmlMessage msg(this->AsMdaAssistant(), TRUE, &pXml);

    msg.SendMessagef(MDARC_INVALID_FUNCTION_PTR_IN_DELEGATE, pFunc);
}

//
// MdaDirtyCastAndCallOnInterface
//

void MdaDirtyCastAndCallOnInterface::ReportViolation(IUnknown* pUnk)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        SO_INTOLERANT;
    }
    CONTRACTL_END;

    MdaXmlElement* pXml;
    MdaXmlMessage msg(this->AsMdaAssistant(), TRUE, &pXml);

    msg.SendMessagef(MDARC_DIRTY_CAST_AND_CALL_ON_INTERFACE);
}

//
// MdaFatalExecutionEngineError
//
void MdaFatalExecutionEngineError::ReportFEEE(TADDR addrOfError, HRESULT hrError)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        SO_INTOLERANT;
    }
    CONTRACTL_END;

    EX_TRY
    {
        MdaXmlElement* pXml;
        MdaXmlMessage msg(this->AsMdaAssistant(), TRUE, &pXml);

        DWORD tid = GetCurrentThreadId();

        msg.SendMessagef(MDARC_FATAL_EXECUTION_ENGINE_ERROR, addrOfError, tid, hrError);        
    }
    EX_CATCH
    {
        // Caller cannot take exceptions.
    }
    EX_END_CATCH(SwallowAllExceptions);
}


//
// MdaInvalidCERCall
//
void MdaInvalidCERCall::ReportViolation(MethodDesc* pCallerMD, MethodDesc *pCalleeMD, DWORD dwOffset)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        SO_INTOLERANT;
    }
    CONTRACTL_END;

    MdaXmlElement* pXml;
    MdaXmlMessage msg(this->AsMdaAssistant(), FALSE, &pXml);

    AsMdaAssistant()->OutputMethodDesc(pCalleeMD, pXml->AddChild(MdaElemDecl(Method)));
    AsMdaAssistant()->OutputCallsite(pCallerMD, dwOffset, pXml->AddChild(MdaElemDecl(Callsite)));

    StackSString sszCalleeMethodName(SString::Utf8, pCalleeMD->GetName());
    StackSString sszCallerMethodName(SString::Utf8, pCallerMD->GetName());
    msg.SendMessagef(MDARC_INVALID_CER_CALL, sszCallerMethodName.GetUnicode(), dwOffset, sszCalleeMethodName.GetUnicode());
}


//
// MdaVirtualCERCall
//
void MdaVirtualCERCall::ReportViolation(MethodDesc* pCallerMD, MethodDesc *pCalleeMD, DWORD dwOffset)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        SO_INTOLERANT;
    }
    CONTRACTL_END;

    MdaXmlElement* pXml;
    MdaXmlMessage msg(this->AsMdaAssistant(), FALSE, &pXml);

    AsMdaAssistant()->OutputMethodDesc(pCalleeMD, pXml->AddChild(MdaElemDecl(Method)));
    AsMdaAssistant()->OutputCallsite(pCallerMD, dwOffset, pXml->AddChild(MdaElemDecl(Callsite)));

    StackSString sszCalleeMethodName(SString::Utf8, pCalleeMD->GetName());
    StackSString sszCallerMethodName(SString::Utf8, pCallerMD->GetName());
    msg.SendMessagef(MDARC_VIRTUAL_CER_CALL, sszCallerMethodName.GetUnicode(), dwOffset, sszCalleeMethodName.GetUnicode());
}


//
// MdaOpenGenericCERCall
//
void MdaOpenGenericCERCall::ReportViolation(MethodDesc* pMD)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        SO_INTOLERANT;
    }
    CONTRACTL_END;

    MdaXmlElement* pXml;
    MdaXmlMessage msg(this->AsMdaAssistant(), FALSE, &pXml);

    AsMdaAssistant()->OutputMethodDesc(pMD, pXml->AddChild(MdaElemDecl(Method)));

    StackSString sszMethodName(SString::Utf8, pMD->GetName());
    msg.SendMessagef(MDARC_OPENGENERIC_CER_CALL, sszMethodName.GetUnicode());
}


//
// MdaIllegalPrepareConstrainedRegion
//
void MdaIllegalPrepareConstrainedRegion::ReportViolation(MethodDesc* pMD, DWORD dwOffset)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        SO_INTOLERANT;
    }
    CONTRACTL_END;

    MdaXmlElement* pXml;
    MdaXmlMessage msg(this->AsMdaAssistant(), FALSE, &pXml);

    AsMdaAssistant()->OutputCallsite(pMD, dwOffset, pXml->AddChild(MdaElemDecl(Callsite)));

    StackSString sszMethodName(SString::Utf8, pMD->GetName());
    msg.SendMessagef(MDARC_ILLEGAL_PCR, sszMethodName.GetUnicode(), dwOffset);
}


//
// MdaReleaseHandleFailed
//
void MdaReleaseHandleFailed::ReportViolation(TypeHandle th, LPVOID lpvHandle)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        SO_INTOLERANT;
    }
    CONTRACTL_END;

    MdaXmlElement* pXml;
    MdaXmlMessage msg(this->AsMdaAssistant(), TRUE, &pXml);

    AsMdaAssistant()->OutputTypeHandle(th, pXml->AddChild(MdaElemDecl(Type)));

    StackSString sszHandle;
    sszHandle.Printf(W("0x%p"), lpvHandle);
    pXml->AddChild(MdaElemDecl(Handle))->AddAttributeSz(MdaAttrDecl(Value), sszHandle.GetUnicode());

    StackSString sszType;
    th.GetName(sszType);
    msg.SendMessagef(MDARC_SAFEHANDLE_CRITICAL_FAILURE, sszType.GetUnicode(), lpvHandle);
}


#ifdef FEATURE_COMINTEROP   
//
// MdaReleaseHandleFailed
//
void MdaNonComVisibleBaseClass::ReportViolation(MethodTable *pMT, BOOL fForIDispatch)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        SO_INTOLERANT;
    }
    CONTRACTL_END;

    MdaXmlElement* pXml;
    MdaXmlMessage msg(this->AsMdaAssistant(), TRUE, &pXml);

    TypeHandle thDerived = TypeHandle(pMT);
    TypeHandle thBase = thDerived.GetParent();
    
    while (IsTypeVisibleFromCom(thBase))
        thBase = thBase.GetParent();

    // If we get there, one of the parents must be non COM visible.
    _ASSERTE(!thBase.IsNull());

    AsMdaAssistant()->OutputTypeHandle(thDerived, pXml->AddChild(MdaElemDecl(DerivedType)));
    AsMdaAssistant()->OutputTypeHandle(thBase, pXml->AddChild(MdaElemDecl(BaseType)));

    SString strDerivedClassName;
    SString strBaseClassName;

    thDerived.GetName(strDerivedClassName);
    thBase.GetName(strBaseClassName);


    msg.SendMessagef(fForIDispatch ? MDARC_NON_COMVISIBLE_BASE_CLASS_IDISPATCH : MDARC_NON_COMVISIBLE_BASE_CLASS_CLASSITF,
       strDerivedClassName.GetUnicode(), strBaseClassName.GetUnicode());
}
#endif //FEATURE_COMINTEROP


#ifdef _DEBUG
//
// MdaXmlValidationError
//
void MdaXmlValidationError::ReportError(MdaSchema::ValidationResult* pValidationResult)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        DEBUG_ONLY;
        SO_NOT_MAINLINE;
    }
    CONTRACTL_END;
    PRECONDITION(CheckPointer(pValidationResult->m_pViolatingElement));
    PRECONDITION(CheckPointer(pValidationResult->m_pViolatedElement));

    MdaXmlElement* pXml;
    MdaXmlMessage msg(this->AsMdaAssistant(), FALSE, &pXml);

    pXml->AddChild(MdaElemDecl(ViolatingXml))->AddChild(pValidationResult->m_pXmlRoot);
    pValidationResult->m_pSchema->ToXml(pXml->AddChild(MdaElemDecl(ViolatedXsd)));    
    
    msg.SendMessage(W("The following XML does not match its schema."));
}
#endif


//
// InvalidConfigFile
//
void MdaInvalidConfigFile::ReportError(MdaElemDeclDef configFile)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        DEBUG_ONLY;
        SO_NOT_MAINLINE;
    }
    CONTRACTL_END;

    MdaXmlElement* pXml;
    MdaXmlMessage report(this->AsMdaAssistant(), TRUE, &pXml);

    LPCWSTR szConfigFile = MdaSchema::GetElementName(configFile);
    pXml->AddAttributeSz(MdaAttrDecl(ConfigFile), szConfigFile);  
    
    report.SendMessagef(MDARC_INVALID_CONFIG_FILE, szConfigFile);
}

//
// MdaDateTimeInvalidLocalFormat
//
void MdaDateTimeInvalidLocalFormat::ReportError()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        DEBUG_ONLY;
        SO_NOT_MAINLINE;
    }
    CONTRACTL_END;

    MdaXmlElement* pXml;
    MdaXmlMessage msg(this->AsMdaAssistant(), TRUE, &pXml);

    msg.SendMessagef(MDARC_DATETIME_INVALID_LOCAL_FORMAT);
}

FCIMPL0(void, MdaManagedSupport::DateTimeInvalidLocalFormat)
{
    FCALL_CONTRACT;

    MdaDateTimeInvalidLocalFormat* pMda = MDA_GET_ASSISTANT(DateTimeInvalidLocalFormat);
    if (pMda)
    {
        HELPER_METHOD_FRAME_BEGIN_0();
        pMda->ReportError();
        HELPER_METHOD_FRAME_END();
    }
}
FCIMPLEND

#endif
#endif //MDA_SUPPORTED
