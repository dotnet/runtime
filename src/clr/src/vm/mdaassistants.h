// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


#ifndef _MDA_ASSISTANTS_
#define _MDA_ASSISTANTS_
  
#include "common.h"
#include "mda.h"
#include "mlinfo.h"
#include <dbginterface.h>

/*

//-----------------------------------------------------------------------------
// How to add a new MDA:
//-----------------------------------------------------------------------------

1) add a new class that derives from MdaAssistant to src\vm\mdaAssistants.h
- the new class should have some function to report the error (we'll call it ReportXYZ()).  
The function is not virtual, and so can take any needed parameters and will be called explicitly wherever you want to fire the MDA.

2) Add the new implementation to src\vm\mdaAssistants.cpp
See the other report functions for an example (eg, MdaLoaderLock::ReportViolation)

3) The MDA contains a user-description string. This must be localized, and so it comes from a resource file.
    - add a new resource ID to src\dlls\mscorrc\Resource.h (eg, MDARC_REPORT_AV_ON_COM_RELEASE)

4) add the actual text to src\dlls\mscorrc\Mscorrc.rc. 
    - add a #define MDARC_XYZ_MSG string. This is a printf format string and may contain parameters.
    - add an entry into the MDARC stringtable like "MDARC_XYZ_MSG MDAARC_XYZ"

5) In order to get an instance of the MDA:
    Use a construct like:
        MdaFatalExecutionEngineError * pMDA = MDA_GET_ASSISTANT(FatalExecutionEngineError);
            
    The macro parameter is the MDA class name minus the "MDA" prefix.
    This may return null if the MDA is not available.

6) Update mdaAssistantSchemas.inl

7) Add it to any appropriate groups in mdaGroups.inl. Please be sure to follow each groups policy.

8) Write a test for it.
*/

#ifdef MDA_SUPPORTED 

// Until Mda offers first class support for managed code we'll just make targetd ecalls.
class MdaManagedSupport
{
public:
    static FCDECL0(void, MemberInfoCacheCreation);
    static FCDECL0(void, DateTimeInvalidLocalFormat);
    static FCDECL1(void, ReportStreamWriterBufferedDataLost, StringObject * pString);
    static FCDECL0(FC_BOOL_RET, IsStreamWriterBufferedDataLostEnabled);
    static FCDECL0(FC_BOOL_RET, IsStreamWriterBufferedDataLostCaptureAllocatedCallStack);
    static FCDECL0(FC_BOOL_RET, IsInvalidGCHandleCookieProbeEnabled);
    static FCDECL1(void, FireInvalidGCHandleCookieProbe, LPVOID cookie);
    static FCDECL1(void, ReportErrorSafeHandleRelease, ExceptionObject * pException);
};

// MDA classes do not derive from MdaAssistant in the type system, but, rather, use this macro to
// ensure that their layout is identical to what it would be had they derived from MdaAssistant.  
// This allows them to be "aggregates", which C++ will allow to be initialized at compile time. 
// This means that we must explicitly coerce from a derived type to the "base" type as needed.
//
// Note that the layout is asserted to be correct at compile time via the MDA_DEFINE_ASSISTANT
// macro.
#define MDA_ASSISTANT_BASE_MEMBERS                          \
    MdaAssistant* AsMdaAssistant()                          \
    {                                                       \
        LIMITED_METHOD_CONTRACT;                            \
        return (MdaAssistant*)this;                         \
    }                                                       \
    void Enable()                                           \
    {                                                       \
        LIMITED_METHOD_CONTRACT;                            \
        ManagedDebuggingAssistants::Enable(                 \
            m_assistantDeclDef, this->AsMdaAssistant());    \
    }                                                       \
    MdaElemDeclDef m_assistantDeclDef;                      \
    MdaElemDeclDef m_assistantMsgDeclDef;                   \
    bool m_bSuppressDialog                                  \


//
// MdaFramework
// 
class MdaFramework
{
public:
    void Initialize(MdaXmlElement* pXmlInput);     
    void DumpDiagnostics();    

    MDA_ASSISTANT_BASE_MEMBERS;
    BOOL m_disableAsserts;
    BOOL m_dumpSchemaSchema;
    BOOL m_dumpAssistantSchema;
    BOOL m_dumpAssistantMsgSchema;
    BOOL m_dumpMachineConfig;
    BOOL m_dumpAppConfig;   
};


//
// MdaJitCompilationStart
// 
class MdaJitCompilationStart
{
public:
    void Initialize(MdaXmlElement* pXmlInput); 
    void NowCompiling(MethodDesc* pMethodDesc);

    MDA_ASSISTANT_BASE_MEMBERS;
    MdaQuery::CompiledQueries* m_pMethodFilter;
    BOOL m_bBreak;
};


//
// MdaLoadFromContext
//
class MdaLoadFromContext
{
public:
    void Initialize(MdaXmlElement* pXmlInput) { LIMITED_METHOD_CONTRACT; } 
    void NowLoading(IAssembly** ppIAssembly, StackCrawlMark *pCallerStackMark);

    MDA_ASSISTANT_BASE_MEMBERS;
};


// MdaBindingFailure
//
class MdaBindingFailure
{
public:
    void Initialize(MdaXmlElement* pXmlInput) { LIMITED_METHOD_CONTRACT; }
    void BindFailed(AssemblySpec *pSpec, OBJECTREF *pExceptionObj);

    MDA_ASSISTANT_BASE_MEMBERS;
};


//
// MdaReflection
// 
class MdaMemberInfoCacheCreation
{
public:
    void Initialize(MdaXmlElement* pXmlInput) { WRAPPER_NO_CONTRACT; }
    void MemberInfoCacheCreation();

    MDA_ASSISTANT_BASE_MEMBERS;
};


//
// MdaPInvokeLog
//
class MdaPInvokeLog
{
public:
    void Initialize(MdaXmlElement* pXmlInput) { LIMITED_METHOD_CONTRACT; m_pXmlInput = pXmlInput; }
    BOOL Filter(SString& sszDllName);
    void LogPInvoke(NDirectMethodDesc* pMd, HINSTANCE hMod);

    MDA_ASSISTANT_BASE_MEMBERS;
    MdaXmlElement* m_pXmlInput;
};


//
// MdaOverlappedFreeError
//
class MdaOverlappedFreeError
{
public:
    void Initialize(MdaXmlElement* pXmlInput) { LIMITED_METHOD_CONTRACT; }
    void ReportError(LPVOID pOverlapped);

    MDA_ASSISTANT_BASE_MEMBERS;
};

//
// MdaInvalidOverlappedToPinvoke
//
class MdaInvalidOverlappedToPinvoke
{
public:
    void Initialize(MdaXmlElement* pXmlInput);

    BOOL ShouldHook(MethodDesc *pMD);
    
    // Called when setting up the pinvoke target
    LPVOID Register(HINSTANCE hmod,LPVOID pvTarget);

    // Logs the MDA error if the overlapped pointer isn't in the gc heap
    LPVOID CheckOverlappedPointer(UINT index,LPVOID pOverlapped);

    struct pinvoke_entry
    {    
        LPCWSTR m_moduleName;
        LPCWSTR m_functionName;
        LPVOID m_mdaFunction;
        LPVOID m_realFunction;
        HINSTANCE m_hmod;    

        void Init(LPCWSTR moduleName, LPCWSTR functionName, LPVOID mdaFunction)
        {
            WRAPPER_NO_CONTRACT;
            m_moduleName = moduleName;
            m_functionName = functionName;
            m_mdaFunction = mdaFunction;
            m_realFunction = NULL;
            m_hmod = NULL;
        }
    };
    BOOL InitializeModuleFunctions(HINSTANCE hmod);

    MDA_ASSISTANT_BASE_MEMBERS;
    pinvoke_entry *m_entries;
    UINT m_entryCount;
    BOOL m_bJustMyCode;
};

#ifdef _TARGET_X86_    
//
// PInvokeStackImbalance
//
struct StackImbalanceCookie
{
    enum
    {
        // combined with the unmanaged calling convention (code:pmCallConvMask) in
        // code:m_callConv if the unmanaged target has a floating point return value
        HAS_FP_RETURN_VALUE = 0x80000000
    };

    // Filled in by stub generated by code:NDirectMethodDesc.GenerateStubForMDA or
    // code:COMDelegate::GenerateStubForMDA:
    MethodDesc   *m_pMD;            // dispatching MD (P/Invoke or delegate's Invoke)
    LPVOID        m_pTarget;        // target address
    DWORD         m_dwStackArgSize; // number of arg bytes pushed on stack
    DWORD         m_callConv;       // unmanaged calling convention, highest bit indicates FP return

    // Pre-call ESP as recorded by PInvokeStackImbalanceHelper:
    DWORD         m_dwSavedEsp;
};

class MdaPInvokeStackImbalance
{
public:
    void Initialize(MdaXmlElement* pXmlInput) { LIMITED_METHOD_CONTRACT; }
    void CheckStack(StackImbalanceCookie *pSICookie, DWORD dwPostESP);

    MDA_ASSISTANT_BASE_MEMBERS;
};
#endif


//
// DllMainReturnsFalse
//
class MdaDllMainReturnsFalse
{
public:
    void Initialize(MdaXmlElement* pXmlInput) { LIMITED_METHOD_CONTRACT; }
    void ReportError();

    MDA_ASSISTANT_BASE_MEMBERS;
};



//
// MdaModuloObjectHashcode 
//
class MdaModuloObjectHashcode
{
public:
    void Initialize(MdaXmlElement* pXmlInput)   
    { 
        CONTRACTL
        {
            THROWS;
            GC_NOTRIGGER;
            MODE_ANY;
        }
        CONTRACTL_END;
        
        m_modulus = pXmlInput->GetAttribute(MdaAttrDecl(Modulus))->GetValueAsInt32();
        if (m_modulus <= 0)
            m_modulus = 1;
    }

    INT32 GetModulo() { LIMITED_METHOD_CONTRACT; _ASSERTE(m_modulus > 0); return m_modulus; }

    MDA_ASSISTANT_BASE_MEMBERS;
    INT32 m_modulus;
};


//
// MdaGCUnmanagedToManaged
//
class MdaGcUnmanagedToManaged
{
public:
    void Initialize(MdaXmlElement* pXmlInput) { LIMITED_METHOD_CONTRACT; }
    void TriggerGC(); // calls to GC.Collect & GC.WaitForPendingFinalizers are also generated to IL stubs

    MDA_ASSISTANT_BASE_MEMBERS;
};


//
// MdaGCManagedToUnmanaged
//
class MdaGcManagedToUnmanaged
{
public:
    void Initialize(MdaXmlElement* pXmlInput) { LIMITED_METHOD_CONTRACT; }
    void TriggerGC(); // calls to GC.Collect & GC.WaitForPendingFinalizers are also generated to IL stubs

    MDA_ASSISTANT_BASE_MEMBERS;
};


//
// MdaLoaderLock
//
class MdaLoaderLock
{
public:
    void Initialize(MdaXmlElement* pXmlInput) { LIMITED_METHOD_CONTRACT; }
    void ReportViolation(HINSTANCE hInst);

    MDA_ASSISTANT_BASE_MEMBERS;
};


//
// MdaReentrancy
//
class MdaReentrancy
{
public:
    void Initialize(MdaXmlElement* pXmlInput) { LIMITED_METHOD_CONTRACT; }
    void ReportViolation();

    MDA_ASSISTANT_BASE_MEMBERS;
};


//
// MdaAsynchronousThreadAbort
//
class MdaAsynchronousThreadAbort
{
public:
    void Initialize(MdaXmlElement* pXmlInput) { LIMITED_METHOD_CONTRACT; }
    void ReportViolation(Thread *pCallingThread, Thread *pAbortedThread);

    MDA_ASSISTANT_BASE_MEMBERS;
};


//
// MdaAsynchronousThreadAbort
//
class MdaDangerousThreadingAPI
{
public:
    void Initialize(MdaXmlElement* pXmlInput) { LIMITED_METHOD_CONTRACT; }
    void ReportViolation(__in_z WCHAR *apiName);

    MDA_ASSISTANT_BASE_MEMBERS;
};


//
// MdaReportAvOnComRelease
//
class MdaReportAvOnComRelease
{
public:
    void Initialize(MdaXmlElement* pXmlInput)
    { 
        CONTRACTL
        {
            THROWS;
            GC_NOTRIGGER;
            MODE_ANY;
        }
        CONTRACTL_END;
        m_allowAv = pXmlInput->GetAttribute(MdaAttrDecl(AllowAv))->GetValueAsBool();
    }

    void ReportHandledException(RCW* pRCW);

    BOOL AllowAV() { LIMITED_METHOD_CONTRACT; return m_allowAv; }

    MDA_ASSISTANT_BASE_MEMBERS;
    BOOL m_allowAv;
};



//
// MdaFatalExecutionEngineError
// 
class MdaFatalExecutionEngineError
{
public:
    void Initialize(MdaXmlElement* pXmlInput)
    {
        WRAPPER_NO_CONTRACT;
    }

    // Report a FatalExecutionEngine error. 
    // It is assumed to be on the current thread.
    void ReportFEEE(TADDR addrOfError, HRESULT hrError);

    MDA_ASSISTANT_BASE_MEMBERS;
};

//
// MdaCallbackOnCollectedDelegate
//
class MdaCallbackOnCollectedDelegate
{
public:
    void Initialize(MdaXmlElement* pXmlInput)
    {
        CONTRACTL
        {
            THROWS;
            GC_NOTRIGGER;
            MODE_ANY;
        }
        CONTRACTL_END;

        m_size = pXmlInput->GetAttribute(MdaAttrDecl(ListSize))->GetValueAsInt32();
        if (m_size < 50)
            m_size = 1000;

        if (m_size > 2000)
            m_size = 1000;

        m_pList = new UMEntryThunk*[m_size];
        memset(m_pList, 0, sizeof(UMEntryThunk*) * m_size);
    }

    void ReportViolation(MethodDesc* pMD);
    void AddToList(UMEntryThunk* pEntryThunk);

private:
    void ReplaceEntry(int index, UMEntryThunk* pET);
    
public:
    MDA_ASSISTANT_BASE_MEMBERS;
    UMEntryThunk**      m_pList;
    int                 m_iIndex;
    int                 m_size;
};

//
// InvalidMemberDeclaration
//
class MdaInvalidMemberDeclaration
{
public:
    void Initialize(MdaXmlElement* pXmlInput) { LIMITED_METHOD_CONTRACT; }
    
#ifdef FEATURE_COMINTEROP
    void ReportViolation(ComCallMethodDesc *pCMD, OBJECTREF *pExceptionObj);
#endif //FEATURE_COMINTEROP

    MDA_ASSISTANT_BASE_MEMBERS;
};
    

//
// MdaExceptionSwallowedOnCallFromCom
//
class MdaExceptionSwallowedOnCallFromCom
{
public:
    void Initialize(MdaXmlElement* pXmlInput) { LIMITED_METHOD_CONTRACT; }
    void ReportViolation(MethodDesc *pMD, OBJECTREF *pExceptionObj);

    MDA_ASSISTANT_BASE_MEMBERS;
};


//
// MdaInvalidVariant
//
class MdaInvalidVariant
{
public:
    void Initialize(MdaXmlElement* pXmlInput) { LIMITED_METHOD_CONTRACT; }
    void ReportViolation();

    MDA_ASSISTANT_BASE_MEMBERS;
};


//
// MdaInvalidApartmentStateChange
//
class MdaInvalidApartmentStateChange
{
public:
    void Initialize(MdaXmlElement* pXmlInput) { LIMITED_METHOD_CONTRACT; }
    void ReportViolation(Thread* pThread, Thread::ApartmentState state, BOOL fAlreadySet);

    MDA_ASSISTANT_BASE_MEMBERS;
};



//
// MdaFailedQI
//
HRESULT MdaFailedQIAssistantCallback(LPVOID pData);

typedef struct
{
    RCW*             pWrapper;
    IID              iid;
    BOOL             fSuccess;
} MdaFailedQIAssistantCallbackData;

#define OLE32DLL    W("ole32.dll")

class MdaFailedQI
{
public:
    void Initialize(MdaXmlElement* pXmlInput) { LIMITED_METHOD_CONTRACT; }
    void ReportAdditionalInfo(HRESULT hr, RCW* pRCW, GUID iid, MethodTable* pMT); 

    MDA_ASSISTANT_BASE_MEMBERS;
};


//
// MdaDisconnectedContext
//
class MdaDisconnectedContext
{
public:
    void Initialize(MdaXmlElement* pXmlInput) { LIMITED_METHOD_CONTRACT; }
    void ReportViolationDisconnected(LPVOID context, HRESULT hr);
    void ReportViolationCleanup(LPVOID context1, LPVOID context2, HRESULT hr);

    MDA_ASSISTANT_BASE_MEMBERS;
};


//
// MdaNotMarshalable
//
class MdaNotMarshalable
{
public:
    void Initialize(MdaXmlElement* pXmlInput) { LIMITED_METHOD_CONTRACT; }
    void ReportViolation();

    MDA_ASSISTANT_BASE_MEMBERS;
};



//
// MdaMarshalCleanupError
//
class MdaMarshalCleanupError
{
public:
    void Initialize(MdaXmlElement* pXmlInput) { LIMITED_METHOD_CONTRACT; }
    void ReportErrorThreadCulture(OBJECTREF *pExceptionObj);
    void ReportErrorSafeHandleRelease(OBJECTREF *pExceptionObj);
    void ReportErrorSafeHandleProp(OBJECTREF *pExceptionObj);
    void ReportErrorCustomMarshalerCleanup(TypeHandle typeCustomMarshaler, OBJECTREF *pExceptionObj);

    MDA_ASSISTANT_BASE_MEMBERS;
};


//
// MdaInvalidIUnknown
//
class MdaInvalidIUnknown
{
public:
    void Initialize(MdaXmlElement* pXmlInput) { LIMITED_METHOD_CONTRACT; }
    void ReportViolation();

    MDA_ASSISTANT_BASE_MEMBERS;
};


//
// MdaContextSwitchDeadlock
//
class MdaContextSwitchDeadlock
{
public:
    void Initialize(MdaXmlElement* pXmlInput) { LIMITED_METHOD_CONTRACT; }
    void ReportDeadlock(LPVOID Origin, LPVOID Destination);

    MDA_ASSISTANT_BASE_MEMBERS;
};


//
// MdaRaceOnRCWCleanup
//
class MdaRaceOnRCWCleanup
{
public:
    void Initialize(MdaXmlElement* pXmlInput) { LIMITED_METHOD_CONTRACT; }
    void ReportViolation();

    MDA_ASSISTANT_BASE_MEMBERS;
};

//
// MdaMarshaling
//
class MdaMarshaling
{
public:
    void Initialize(MdaXmlElement* pXmlInput);
    void ReportFieldMarshal(FieldMarshaler* pFM);
  
private:
    void GetManagedSideForMethod(SString& strManagedMarshalType, Module* pModule, SigPointer sig, CorElementType elemType);
    void GetUnmanagedSideForMethod(SString& strNativeMarshalType, MarshalInfo* mi, BOOL fSizeIsSpecified);
    void GetManagedSideForField(SString& strManagedMarshalType, FieldDesc* pFD);
    void GetUnmanagedSideForField(SString& strUnmanagedMarshalType, FieldMarshaler* pFM);
    BOOL CheckForPrimitiveType(CorElementType elemType, SString& strPrimitiveType);

public:
    MDA_ASSISTANT_BASE_MEMBERS;
    MdaQuery::CompiledQueries* m_pMethodFilter;
    MdaQuery::CompiledQueries* m_pFieldFilter;
};



//
// InvalidFunctionPointerInDelegate
//
class MdaInvalidFunctionPointerInDelegate
{
public:
    void Initialize(MdaXmlElement* pXmlInput) {LIMITED_METHOD_CONTRACT; }
    void ReportViolation(LPVOID pFunc);    

    MDA_ASSISTANT_BASE_MEMBERS;
};


//
// DirtyCastAndCallOnInterface
//
class MdaDirtyCastAndCallOnInterface
{
public:
    void Initialize(MdaXmlElement* pXmlInput) {LIMITED_METHOD_CONTRACT; }
    void ReportViolation(IUnknown* pUnk);    

    MDA_ASSISTANT_BASE_MEMBERS;
};


//
// InvalidCERCall
//
class MdaInvalidCERCall
{
public:
    void Initialize(MdaXmlElement* pXmlInput) { LIMITED_METHOD_CONTRACT; }
    void ReportViolation(MethodDesc* pCallerMD, MethodDesc *pCalleeMD, DWORD dwOffset);

    MDA_ASSISTANT_BASE_MEMBERS;
};



//
// VirtualCERCall
//
class MdaVirtualCERCall
{
public:
    void Initialize(MdaXmlElement* pXmlInput) { LIMITED_METHOD_CONTRACT; }
    void ReportViolation(MethodDesc* pCallerMD, MethodDesc *pCalleeMD, DWORD dwOffset);

    MDA_ASSISTANT_BASE_MEMBERS;
};



//
// OpenGenericCERCall
//
class MdaOpenGenericCERCall
{
public:
    void Initialize(MdaXmlElement* pXmlInput) { LIMITED_METHOD_CONTRACT; }
    void ReportViolation(MethodDesc* pMD);

    MDA_ASSISTANT_BASE_MEMBERS;
};



//
// IllegalPrepareConstrainedRegion
//
class MdaIllegalPrepareConstrainedRegion
{
public:
    void Initialize(MdaXmlElement* pXmlInput) { LIMITED_METHOD_CONTRACT; }
    void ReportViolation(MethodDesc* pMD, DWORD dwOffset);

    MDA_ASSISTANT_BASE_MEMBERS;
};



//
// ReleaseHandleFailed
//
class MdaReleaseHandleFailed
{
public:
    void Initialize(MdaXmlElement* pXmlInput) { LIMITED_METHOD_CONTRACT; }
    void ReportViolation(TypeHandle th, LPVOID lpvHandle);

    MDA_ASSISTANT_BASE_MEMBERS;
};


//
// NonComVisibleBaseClass
//
class MdaNonComVisibleBaseClass
{
public:
    void Initialize(MdaXmlElement* pXmlInput) { LIMITED_METHOD_CONTRACT; }
#ifdef FEATURE_COMINTEROP   
    void ReportViolation(MethodTable *pMT, BOOL fForIDispatch);
#endif //FEATURE_COMINTEROP

    MDA_ASSISTANT_BASE_MEMBERS;
};


//
// InvalidGCHandleCookie
//
class MdaInvalidGCHandleCookie
{
public:
    void Initialize(MdaXmlElement* pXmlInput) { LIMITED_METHOD_CONTRACT; }
    void ReportError(LPVOID cookie);

    MDA_ASSISTANT_BASE_MEMBERS;
};
    
//
// MdaXmlValidator
//
class MdaXmlValidator
{
public:
    void Initialize(MdaXmlElement* pXmlInput) { LIMITED_METHOD_CONTRACT; }

    MDA_ASSISTANT_BASE_MEMBERS;
};


#ifdef _DEBUG
//
// MdaXmlValidationError 
//
class MdaXmlValidationError
{
public:    
    void Initialize(MdaXmlElement* pXml) { LIMITED_METHOD_CONTRACT; }

public:
    void ReportError(MdaSchema::ValidationResult* pValidationResult);

    MDA_ASSISTANT_BASE_MEMBERS;
};
#endif


//
// MdaInvalidConfigFile 
//
class MdaInvalidConfigFile
{
public:    
    void Initialize(MdaXmlElement* pXml) { LIMITED_METHOD_CONTRACT; }

public:
    void ReportError(MdaElemDeclDef configFile);

    MDA_ASSISTANT_BASE_MEMBERS;
};

//
// MdaDateTimeInvalidLocalFormat
//
class MdaDateTimeInvalidLocalFormat
{
public:
    void Initialize(MdaXmlElement* pXmlInput) { LIMITED_METHOD_CONTRACT; }
    void ReportError();

    MDA_ASSISTANT_BASE_MEMBERS;
};

//
// MdaStreamWriterBufferedDataLost
//
class MdaStreamWriterBufferedDataLost
{
public:
    void Initialize(MdaXmlElement* pXmlInput) 
    { 
        CONTRACTL
        {
            THROWS;
            GC_NOTRIGGER;
            MODE_ANY;
        }
        CONTRACTL_END;
        m_captureAllocatedCallStack = pXmlInput->GetAttribute(MdaAttrDecl(CaptureAllocatedCallStack))->GetValueAsBool();
    }
    
    BOOL CaptureAllocatedCallStack() { LIMITED_METHOD_CONTRACT; return m_captureAllocatedCallStack; }

    void ReportError(SString text);

    MDA_ASSISTANT_BASE_MEMBERS;
    BOOL m_captureAllocatedCallStack;
};

class ValidateMdaAssistantLayout
{
    static_assert_no_msg(sizeof(MdaAssistant) == 3);
#define MDA_VALIDATE_MEMBER_LAYOUT
#include "mdaschema.inl"
#undef MDA_VALIDATE_MEMBER_LAYOUT
};

//
// MdaStaticHeap
//

typedef struct
{
    // This array is always live.  Checking whether an assistant is enabled is 
    // simply a fetch from this array.
    MdaAssistant*               m_assistants[MdaElemDef(AssistantMax)];

    // This pointer will point to the m_mda memory region, where the actual 
    // ManagedDebuggingAssistants instance lives.  It may be null if we no MDAs
    // were enabled.
    ManagedDebuggingAssistants* m_pMda;
    BYTE                        m_mda[sizeof(ManagedDebuggingAssistants)];

#define MDA_ASSISTANT_HEAP_RAW
#include "mdaschema.inl"
#undef  MDA_ASSISTANT_HEAP_RAW

    void DisableAll()
    {
        LIMITED_METHOD_CONTRACT;
        memset(&m_assistants, 0, sizeof(m_assistants));
    }
}
MdaStaticHeap;
typedef DPTR(MdaStaticHeap) PTR_MdaStaticHeap;
extern MdaStaticHeap g_mdaStaticHeap;


// static
FORCEINLINE void ManagedDebuggingAssistants::Enable(MdaElemDeclDef assistantDeclDef, MdaAssistant* pMda)
{
    g_mdaStaticHeap.m_assistants[assistantDeclDef] = pMda;
}

#ifndef DACCESS_COMPILE
FORCEINLINE MdaAssistant* ManagedDebuggingAssistants::GetAssistant(MdaElemDeclDef id)
{
    WRAPPER_NO_CONTRACT; 

    // If this assert fires, you should consider using GET_ASSISTANT_EX / TRIGGER_ASSISTANT_EX
    _ASSERTE((g_pDebugInterface == NULL) || !g_pDebugInterface->ThisIsHelperThread());

    return g_mdaStaticHeap.m_assistants[id];
}

FORCEINLINE MdaAssistant* ManagedDebuggingAssistants::GetAssistantEx(MdaElemDeclDef id)
{
    WRAPPER_NO_CONTRACT; 

    MdaAssistant* pMda = g_mdaStaticHeap.m_assistants[id];
    if ((pMda != NULL) && ((g_pDebugInterface == NULL) || !g_pDebugInterface->ThisIsHelperThread()))
        return pMda;

    return NULL;
}
#endif // DACCESS_COMPILE

void TriggerGCForMDAInternal();

#endif // MDA_SUPPORTED 
#endif // _MDA_ASSISTANTS_


