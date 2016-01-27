// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


/*
 *
 * Purpose: Provide IValidate implementation.
 *          IValidate is used to validate PE stub, Metadata and IL.
 *
 */

#include "common.h"

#include "corerror.h"
#include "vererror.h"
#include "ivalidator.h"
#include "securityattributes.h"
#include "corhost.h"
#include "verifier.hpp"
#include "pedecoder.h"
#include "comcallablewrapper.h"
#include "../dlls/mscorrc/resource.h"
#include "posterror.h"
#include "comcallablewrapper.h"
#include "eeconfig.h"
#include "corhost.h"
#include "security.h"
#include "appdomain.inl"

typedef void (*VerifyErrorHandler)(void* pThis, HRESULT hrError, struct VerErrorStruct* pError);

// Declare global variables
#define DECLARE_DATA
#include "veropcodes.hpp"
#undef DECLARE_DATA

class CValidator
{
public:
    CValidator(IVEHandler *veh) : m_veh(veh) 
    {
        LIMITED_METHOD_CONTRACT;
    }
    HRESULT VerifyAllMethodsForClass(Module *pModule, mdTypeDef cl, ValidateWorkerArgs* pArgs);
    HRESULT VerifyAllGlobalFunctions(Module *pModule, ValidateWorkerArgs* pArgs);
    HRESULT VerifyAssembly(Assembly *pAssembly, ValidateWorkerArgs* pArgs);
    HRESULT VerifyModule(Module* pModule, ValidateWorkerArgs* pArgs);
    HRESULT ReportError(HRESULT hr, ValidateWorkerArgs* pArgs, mdToken tok=0);
    HRESULT VerifyMethod(COR_ILMETHOD_DECODER* pILHeader, IVEHandler* pVEHandler, WORD wFlags, ValidateWorkerArgs* pArgs);
    HRESULT VerifyExportedType(
        Module *             pModule, 
        mdToken              tkExportedType, 
        ValidateWorkerArgs * pArgs);
    void HandleError(HRESULT hrError, struct VerErrorStruct* pError);

private:
    IVEHandler *m_veh;
    ValidateWorkerArgs* m_pArgs;
};  // class CValidator

HRESULT CValidator::ReportError(HRESULT hr, ValidateWorkerArgs* pArgs, mdToken tok /* = 0 */)
{
    CONTRACTL {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
    } CONTRACTL_END;

    if (m_veh == NULL)
        return hr;

    HRESULT hr2 = E_FAIL;
    BEGIN_SO_INTOLERANT_CODE_NOTHROW(GetThread(), return COR_E_STACKOVERFLOW);
    VEContext vec;

    memset(&vec, 0, sizeof(VEContext));

    if (tok != 0)
    {
        vec.flags = VER_ERR_TOKEN;
        vec.Token = tok;
    }

    hr2 =  Verifier::ReportError(m_veh, hr, &vec, pArgs);
    END_SO_INTOLERANT_CODE;
    return hr2;
} // CValidator::ReportError

// Separate method since EX_TRY uses _alloca and is in a loop below.
COR_ILMETHOD* GetILHeader(MethodDesc *pMD)
{
    STANDARD_VM_CONTRACT;

    COR_ILMETHOD *pILHeader = NULL;

    EX_TRY
    {
        pILHeader = pMD->GetILHeader();
    }
    EX_CATCH
    {
    }
    EX_END_CATCH(SwallowAllExceptions);

    return pILHeader;
}

HRESULT CValidator::VerifyAllMethodsForClass(Module *pModule, mdTypeDef cl, ValidateWorkerArgs* pArgs)
{
    STANDARD_VM_CONTRACT;

    HRESULT hr = S_OK;
    MethodTable *pMT = NULL;
     
    // In the case of COR_GLOBAL_PARENT_TOKEN (i.e. global functions), it is guaranteed
    // that the module has a method table or our caller will have skipped this step.
    TypeHandle th;
    {
        // <REVISIT>
        // Although there's no assert to disable here, we need to improve OOM reliability here. We are ignoring the HRESULT from the loader here.
        // That could cause an OOM failure to be disguised as something else. OOM's
        // need to be handled or propagated up to the caller.
        // </REVISIT>
        CONTRACT_VIOLATION(0);
        
        EX_TRY {
            th = ClassLoader::LoadTypeDefOrRefThrowing(pModule, cl,
                                             ClassLoader::ReturnNullIfNotFound, 
                                             ClassLoader::PermitUninstDefOrRef);
        }
        EX_CATCH_HRESULT(hr);

        if (FAILED(hr)) {
            if ((hr==COR_E_TYPELOAD) || (hr==VER_E_TYPELOAD)) {
                hr = ReportError(hr, pArgs,cl);
            } else {
                hr = ReportError(hr, pArgs);
            }
            goto Exit;
        }
    }

    pMT = th.GetMethodTable();
    if (pMT == NULL)
    {
        hr = ReportError(VER_E_TYPELOAD, pArgs, cl);
        goto Exit;
    }

    g_fVerifierOff = false;

    {
        // Verify all methods in class - excluding inherited methods
        MethodTable::MethodIterator it(pMT);
        for (; it.IsValid(); it.Next())
        {
            pArgs->pMethodDesc = it.GetMethodDesc();

            bool fVerifyTransparentMethod = true;
            if (pArgs->fTransparentMethodsOnly)
            {
                MethodSecurityDescriptor msd(pArgs->pMethodDesc);
                fVerifyTransparentMethod = !msd.IsCritical();
            }

            if (pArgs->pMethodDesc && 
                pArgs->pMethodDesc->GetMethodTable() == pMT &&
                pArgs->pMethodDesc->IsIL() && 
                !pArgs->pMethodDesc->IsAbstract() && 
                !pArgs->pMethodDesc->IsUnboxingStub() &&
                fVerifyTransparentMethod)
            {
                COR_ILMETHOD* pILHeader = GetILHeader(pArgs->pMethodDesc);

                if (pILHeader != NULL)
                {
                    COR_ILMETHOD_DECODER::DecoderStatus status;
                    COR_ILMETHOD_DECODER ILHeader(pILHeader, 
                                                  pArgs->pMethodDesc->GetMDImport(), &status); 

                    if (status == COR_ILMETHOD_DECODER::SUCCESS)
                    {
                        hr = VerifyMethod(&ILHeader, m_veh, VER_FORCE_VERIFY, pArgs);
                        if (hr == VER_E_INTERNAL) // this probably means peverify.dll was missing
                        {
                            goto Exit;
                        }
                    }
                    else if (status == COR_ILMETHOD_DECODER::VERIFICATION_ERROR)
                    {
                        hr = COR_E_VERIFICATION;
                    }
                    else if (status == COR_ILMETHOD_DECODER::FORMAT_ERROR)
                    {
                        hr = COR_E_BADIMAGEFORMAT;
                    }
                    else
                    {
                        _ASSERTE(!"Unhandled status from COR_ILMETHOD_DECODER");
                    }
                }
                else
                {
                    hr = COR_E_BADIMAGEFORMAT;
                }

                if (FAILED(hr))
                    hr = ReportError(hr, pArgs);

                if (FAILED(hr))
                    goto Exit;
            }
            // We should ideally have an API to yield to the host,
            // but this is not critical for Whidbey.
            if (CLRTaskHosted())
                ClrSleepEx(0, FALSE);
        }
    }

Exit:
    pArgs->pMethodDesc = NULL;
    return hr;
} // CValidator::VerifyAllMethodsForClass

//---------------------------------------------------------------------------------------
// 
void 
MethodDescAndCorILMethodDecoderToCorInfoMethodInfo(
    MethodDesc *           ftn, 
    COR_ILMETHOD_DECODER * ILHeader, 
    CORINFO_METHOD_INFO *  pMethodInfo)
{
    STANDARD_VM_CONTRACT;

    pMethodInfo->ftn = CORINFO_METHOD_HANDLE(ftn);
    pMethodInfo->scope = CORINFO_MODULE_HANDLE(ftn->GetModule());
    pMethodInfo->ILCode = const_cast<BYTE*>(ILHeader->Code);
    pMethodInfo->ILCodeSize = ILHeader->GetCodeSize();
    pMethodInfo->maxStack = ILHeader->GetMaxStack();
    pMethodInfo->EHcount = ILHeader->EHCount();
    pMethodInfo->options =
        (CorInfoOptions)
        (((ILHeader->GetFlags() & CorILMethod_InitLocals) ? CORINFO_OPT_INIT_LOCALS : 0) |
         (ftn->AcquiresInstMethodTableFromThis() ? CORINFO_GENERICS_CTXT_FROM_THIS : 0) |
         (ftn->RequiresInstMethodTableArg() ? CORINFO_GENERICS_CTXT_FROM_METHODTABLE : 0) |
         (ftn->RequiresInstMethodDescArg() ? CORINFO_GENERICS_CTXT_FROM_METHODDESC : 0));

    PCCOR_SIGNATURE pSigToConvert;
    DWORD           cbSigToConvert;
    ftn->GetSig(&pSigToConvert, &cbSigToConvert);
    CONSISTENCY_CHECK(NULL != pSigToConvert);
    // fetch the method signature
    CEEInfo::ConvToJitSig(
        pSigToConvert, 
        cbSigToConvert, 
        pMethodInfo->scope, 
        mdTokenNil, 
        &pMethodInfo->args, 
        ftn, 
        false);

    //@GENERICS:
    // Shared generic methods and shared methods on generic structs take an extra argument representing their instantiation
    if (ftn->RequiresInstArg())
        pMethodInfo->args.callConv = (CorInfoCallConv) (pMethodInfo->args.callConv | CORINFO_CALLCONV_PARAMTYPE);

    // method attributes and signature are consistant
    _ASSERTE(!!ftn->IsStatic() == ((pMethodInfo->args.callConv & CORINFO_CALLCONV_HASTHIS) == 0));

    // And its local variables
    CEEInfo::ConvToJitSig(
        ILHeader->LocalVarSig, 
        ILHeader->cbLocalVarSig, 
        pMethodInfo->scope, 
        mdTokenNil, 
        &pMethodInfo->locals, 
        ftn, 
        true);
} // MethodDescAndCorILMethodDecoderToCorInfoMethodInfo

//---------------------------------------------------------------------------------------
// 
void PEVerifyErrorHandler(void* pThis, HRESULT hrError, struct VerErrorStruct* pError)
{
    WRAPPER_NO_CONTRACT;
    STATIC_CONTRACT_SO_TOLERANT;
    ((CValidator*)pThis)->HandleError(hrError, pError);
}

void CValidator::HandleError(HRESULT hrError, struct VerErrorStruct* pError)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        SO_TOLERANT;
    }
    CONTRACTL_END;

    BEGIN_SO_INTOLERANT_CODE(GetThread());
    _ASSERTE(sizeof(VEContext) == sizeof(struct VerErrorStruct));
    Verifier::ReportError(m_veh, hrError, (VEContext*)pError, m_pArgs);
    END_SO_INTOLERANT_CODE;
}
typedef void (__stdcall* VerifyFunc)(ICorJitInfo* pJitInfo, CORINFO_METHOD_INFO* pMethodInfo, VerifyErrorHandler pErrorHandler, void* pThis);
static void VerifyMethodHelper(VerifyFunc pVerFunc, CEEJitInfo* pJI, CORINFO_METHOD_INFO* pMethodInfo, void* pThis)
{
    // Helper method to allow us to use SO_TOLERANT_CODE macro
    STATIC_CONTRACT_SO_INTOLERANT;
    WRAPPER_NO_CONTRACT;

    BEGIN_SO_TOLERANT_CODE(GetThread());
    // Verify the method
    pVerFunc(pJI, pMethodInfo, PEVerifyErrorHandler, pThis);
    END_SO_TOLERANT_CODE;
    
}

static Volatile<VerifyFunc> g_pVerFunc = NULL;

HRESULT CValidator::VerifyMethod(COR_ILMETHOD_DECODER* pILHeader, IVEHandler* pVEHandler, WORD wFlags, ValidateWorkerArgs* pArgs)
{
    STANDARD_VM_CONTRACT;

    HRESULT hr = S_OK;
    EX_TRY
    {
        // Find the DLL entrypoint
        m_pArgs = pArgs;
        if (g_pVerFunc.Load() == NULL)
        {
            HINSTANCE hJit64 = NULL;
            if (SUCCEEDED(g_pCLRRuntime->LoadLibrary(W("peverify.dll"), &hJit64)))
            {
                typedef void (__stdcall* psxsPeVerifyStartup) (CoreClrCallbacks);
                psxsPeVerifyStartup sxsPeVerifyStartup = (psxsPeVerifyStartup) GetProcAddress(hJit64, "sxsPeVerifyStartup");

                if(sxsPeVerifyStartup)
                {
                    CoreClrCallbacks cccallbacks = GetClrCallbacks();
                    (*sxsPeVerifyStartup) (cccallbacks);
                    g_pVerFunc = (VerifyFunc)GetProcAddress(hJit64, "VerifyMethod");
                }
            }
        }

        if(!g_pVerFunc)
        {
            _ASSERTE(!"Failed to load peverify.dll or find VerifyMethod proc address");
            hr = VER_E_INTERNAL;
        }
        else
        {
            Thread *pThread = GetThread();
            if (pThread->IsAbortRequested())
            {
                pThread->HandleThreadAbort();
            }
            // Prepare the args
            MethodDesc* ftn = pArgs->pMethodDesc;
            CEEJitInfo ji(pArgs->pMethodDesc, pILHeader, NULL, true /* verify only */);
            CORINFO_METHOD_INFO methodInfo;
            MethodDescAndCorILMethodDecoderToCorInfoMethodInfo(ftn, pILHeader, &methodInfo);

            // Verify the method
            VerifyMethodHelper(g_pVerFunc, &ji, &methodInfo, this);
        }
    }
    EX_CATCH
    {
        // Catch and report any errors that peverify.dll lets fall through (ideally that should never happen)
        hr = GET_EXCEPTION()->GetHR();
        hr = ReportError(hr, pArgs);
    }
    EX_END_CATCH(RethrowTerminalExceptions)

    return hr;
} // CValidator::VerifyMethod

// Helper function to verify the global functions
HRESULT CValidator::VerifyAllGlobalFunctions(Module *pModule, ValidateWorkerArgs* pArgs)
{
    STANDARD_VM_CONTRACT;

    HRESULT hr = S_OK;
    // Is there anything worth verifying?
    if (pModule->GetGlobalMethodTable())
        hr =  VerifyAllMethodsForClass(pModule, COR_GLOBAL_PARENT_TOKEN, pArgs);
    return hr;
} // CValidator::VerifyAllGlobalFunctions

HRESULT CValidator::VerifyModule(Module* pModule, ValidateWorkerArgs* pArgs)
{
    STANDARD_VM_CONTRACT;
    
    // Get a count of all the classdefs and enumerate them.
    HRESULT             hr = S_OK;
    IMDInternalImport * pMDI = NULL;
    
    if (pModule == NULL)
    {
        IfFailGo(VER_E_BAD_MD);
    }
    
    pMDI = pModule->GetMDImport();
    if (pMDI == NULL)
    {
        IfFailGo(VER_E_BAD_MD);
    }
        
    // First verify all global functions - if there are any
    IfFailGoto(
        VerifyAllGlobalFunctions(pModule, pArgs), 
        ErrExit_SkipReportError);

    {
        HENUMTypeDefInternalHolder hTypeDefEnum(pMDI);

        IfFailGo(hTypeDefEnum.EnumTypeDefInitNoThrow());  

        // Verify all TypeDefs
        mdTypeDef tkTypeDef;
        while (pMDI->EnumTypeDefNext(&hTypeDefEnum, &tkTypeDef))
        {
            IfFailGoto(
                VerifyAllMethodsForClass(pModule, tkTypeDef, pArgs), 
                ErrExit_SkipReportError);
        }
    }

    {
        HENUMInternalHolder hExportedTypeEnum(pMDI);

        IfFailGo(hExportedTypeEnum.EnumInitNoThrow(
            mdtExportedType, 
            mdTokenNil));

        // Verify all ExportedTypes
        mdToken tkExportedType;
        while (pMDI->EnumNext(&hExportedTypeEnum, &tkExportedType))
        {
            IfFailGoto(
                VerifyExportedType(pModule, tkExportedType, pArgs), 
                ErrExit_SkipReportError);
        }
    }
    
ErrExit:
    if (FAILED(hr))
    {
        hr = ReportError(hr, pArgs);
    }

ErrExit_SkipReportError:    
    return hr;
} // CValidator::VerifyModule

HRESULT CValidator::VerifyAssembly(Assembly *pAssembly, ValidateWorkerArgs* pArgs)
{
    STANDARD_VM_CONTRACT;

    HRESULT hr;

    _ASSERTE(pAssembly->GetManifestImport());

    // Verify the module containing the manifest. There is no
    // FileRefence so will no show up in the list.
    hr = VerifyModule(pAssembly->GetManifestModule(), pArgs);
    if (FAILED(hr))
        goto Exit;

    {
        IMDInternalImport* pManifestImport = pAssembly->GetManifestImport();

        HENUMInternalHolder hEnum(pManifestImport);

        mdToken mdFile;
        hr = hEnum.EnumInitNoThrow(mdtFile, mdTokenNil);
        if (FAILED(hr)) 
        {
            hr = ReportError(hr, pArgs);
            goto Exit;
        }

        while(pManifestImport->EnumNext(&hEnum, &mdFile)) 
        {
            DomainFile* pModule = pAssembly->GetManifestModule()->LoadModule(GetAppDomain(), mdFile, FALSE);

            if (pModule != NULL)
            {
                hr = VerifyModule(pModule->GetModule(), pArgs);
                if (FAILED(hr)) 
                    goto Exit;
            }
        }
    }

Exit:
    return hr;
} // CValidator::VerifyAssembly

HRESULT 
CValidator::VerifyExportedType(
    Module *             pModule, 
    mdToken              tkExportedType, 
    ValidateWorkerArgs * pArgs)
{
    STANDARD_VM_CONTRACT;
    
    HRESULT    hr;
    TypeHandle th;
    NameHandle nameHandle(pModule, tkExportedType);
    
    LPCSTR szNamespace;
    LPCSTR szName;
    IfFailGo(pModule->GetMDImport()->GetExportedTypeProps(
        tkExportedType, 
        &szNamespace, 
        &szName, 
        NULL,   // tkImplementation
        NULL,   // tkTypeDefId
        NULL)); // dwExportedTypeFlags
    
    nameHandle.SetName(szNamespace, szName);
    
    EX_TRY
    {
        th = pModule->GetClassLoader()->LoadTypeHandleThrowing(
            &nameHandle, 
            CLASS_LOADED, 
            pModule);
        hr = S_OK;
    }
    EX_CATCH
    {
        hr = GET_EXCEPTION()->GetHR();
    }
    EX_END_CATCH(SwallowAllExceptions);
    
    IfFailGo(hr);
    if (th.GetMethodTable() == NULL)
    {
        IfFailGo(VER_E_TYPELOAD);
    }
    
ErrExit:
    if (FAILED(hr))
    {
        hr = ReportError(hr, pArgs, tkExportedType);
    }
    
    return hr;
} // CValidator::VerifyExportedType

static void ValidateWorker(LPVOID /* ValidateWorker_Args */ ptr)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    } CONTRACTL_END;

    ValidateWorkerArgs *args = (ValidateWorkerArgs *) ptr;
    AppDomain *pDomain = GetThread()->GetDomain();
    
    StackSString ssFile(args->wszFileName);
    StackSString ssFileDir;
    StackSString ssDirectory;
    
    // Fill ssDirectory with just drive of the file (e.g. 'C:')
    SplitPath(ssFile, &ssDirectory, &ssFileDir, NULL, NULL);
    // Now apped directory from the file name (incl. leading and trailing '/' or '\')
    ssDirectory.Append(ssFileDir);
    
    {
        // Set up the domain to resolve all dependency assemblies for introspection
        struct _gc {
            OBJECTREF orAppDomain;
            STRINGREF refDirectory;
        } gc;
        ZeroMemory(&gc, sizeof(gc));

        GCPROTECT_BEGIN(gc);

        gc.orAppDomain = pDomain->GetExposedObject();
        if (!ssDirectory.IsEmpty())
        {
            gc.refDirectory = StringObject::NewString(ssDirectory);
        }
        
        MethodDescCallSite meth(METHOD__APP_DOMAIN__ENABLE_RESOLVE_ASSEMBLIES_FOR_INTROSPECTION, &gc.orAppDomain);
        ARG_SLOT args[2] = 
        {
            ObjToArgSlot(gc.orAppDomain), 
            ObjToArgSlot(gc.refDirectory)
        };
        meth.Call(args);
        
        GCPROTECT_END();
    }

    GCX_PREEMP();

    Assembly *pAssembly;
    if (args->wszFileName)
    {
        // Load the primary assembly for introspection
        AssemblySpec spec;
        spec.SetCodeBase(args->wszFileName);
        spec.SetIntrospectionOnly(TRUE);
        pAssembly = spec.LoadAssembly(FILE_LOADED);
    }
    else
    {
        // TODO: This is a workaround to get SQLCLR running.
        //       Our loader requires that a parent assembly is specified in order to load an
        //       assembly from byte array.  But here we do not know the parent.
        PEAssemblyHolder pFile(PEAssembly::OpenMemory(SystemDomain::System()->SystemFile(),
                                                      args->pe, args->size, TRUE));
        pAssembly = pDomain->LoadAssembly(NULL, pFile, FILE_LOADED);
    }

    // Verify the assembly
    args->hr = args->val->VerifyAssembly(pAssembly, args);
}


static HRESULT ValidateHelper(
        IVEHandler        *veh,
        IUnknown          *pAppDomain,
        DWORD              ulAppDomainId,
        BOOL               UseId,
        unsigned long      ulFlags,
        unsigned long      ulMaxError,
        unsigned long      token,
        __in_z LPWSTR             fileName,
        BYTE               *pe,
        unsigned long      ulSize)
{
    CONTRACTL {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        SO_TOLERANT;
    } CONTRACTL_END;

    Thread  *pThread = GetThread();

    if (pe == NULL)
        return E_POINTER;

    HRESULT hr = S_OK;
    BEGIN_SO_INTOLERANT_CODE_NOTHROW(pThread, return COR_E_STACKOVERFLOW);
    ADID pDomain;
    ValidateWorkerArgs args;
    CValidator val(veh);
    AppDomainFromIDHolder ad;

    BOOL Chk = FALSE;
    BOOL UnloadDomain = FALSE;

    GCX_COOP();

    EX_TRY {
        PEDecoder pev(pe, (COUNT_T)ulSize);

        args.wszFileName = fileName;
        args.fVerbose = (ulFlags & VALIDATOR_EXTRA_VERBOSE) ? true : false;
        args.fShowSourceLines = (ulFlags & VALIDATOR_SHOW_SOURCE_LINES) ? true : false;
        args.fTransparentMethodsOnly = (ulFlags & VALIDATOR_TRANSPARENT_ONLY) ? true : false;
        args.val = &val;
        args.pe = pe;
        args.size = ulSize;

        if((ulFlags & VALIDATOR_NOCHECK_PEFORMAT) == 0)
        {
            // Verify the PE header / native stubs first
            // <REVISIT> This validation is not performed on non-manifest modules. </REVISIT>
            Chk = ((ulFlags & VALIDATOR_CHECK_ILONLY) != 0) ? (BOOL) pev.CheckILOnlyFormat() :
                                                              (BOOL) pev.CheckILFormat();
            if (!Chk)
            {
                hr = val.ReportError(VER_E_BAD_PE, &args);

                if (FAILED(hr))
                    goto End;
            }
        }
        if((ulFlags & VALIDATOR_CHECK_PEFORMAT_ONLY) != 0)
            goto End;

        if (fileName)
        {
            AppDomain* pAD = AppDomain::CreateDomainContext(fileName);
            UnloadDomain = TRUE;
            pAD->SetPassiveDomain();
            pDomain=pAD->GetId();
        }
        else if (UseId)
        {
            pDomain = (ADID)ulAppDomainId;
        }
        else
        {
            SystemDomain::LockHolder lh;
            ComCallWrapper* pWrap = GetCCWFromIUnknown(pAppDomain, FALSE);
            if (pWrap == NULL)
            {
                hr = COR_E_APPDOMAINUNLOADED;
                goto End;
            }
            pDomain = pWrap->GetDomainID();
        }

        if (FAILED(hr)) 
        {
            hr = val.ReportError(hr, &args);
            goto End;
        }

        ad.Assign(pDomain, TRUE);
        if (ad.IsUnloaded())
            COMPlusThrow(kAppDomainUnloadedException);
        if (ad->IsIllegalVerificationDomain())
            COMPlusThrow(kFileLoadException, IDS_LOADINTROSPECTION_DISALLOWED);
        ad->SetVerificationDomain();
        ad.Release();

        args.val = &val;

        // We need a file path here.  This is to do a fusion bind, and also
        // to make sure we can find any modules in the assembly.  We assume
        // that the path points to the same place the bytes came from, which is true
        // with PEVerify, but perhaps not with other clients.

        if (pDomain != pThread->GetDomain()->GetId())
        {
            pThread->DoADCallBack(
                pDomain, ValidateWorker, &args);
        }
        else
        {
            ValidateWorker(&args);
        }

        if (FAILED(args.hr))
            hr = val.ReportError(args.hr, &args);

        // Only Unload the domain if we created it.
        if (UnloadDomain)
            AppDomain::UnloadById(pDomain,TRUE);
End:;

    }
    EX_CATCH
    {
        hr = GET_EXCEPTION()->GetHR();
        hr = val.ReportError(hr, &args);
    }
    EX_END_CATCH(RethrowSOExceptions)

    END_SO_INTOLERANT_CODE;
    return hr;
}

void GetFormattingErrorMsg(__out_ecount(ulMaxLength) __out_z LPWSTR msg, unsigned int ulMaxLength)
{
    CONTRACTL {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(ulMaxLength >= 30);
    } CONTRACTL_END;

    EX_TRY
    {
        SString s;
        s.LoadResource(CCompRC::Debugging, IDS_VER_E_FORMATTING);
        wcsncpy_s(msg, ulMaxLength, s.GetUnicode(), _TRUNCATE);
    }
    EX_CATCH
    {
        wcscpy_s(msg, ulMaxLength, W("Error loading resource string"));
    }
    EX_END_CATCH(SwallowAllExceptions)
}

static HRESULT FormatEventInfoHelper(
        HRESULT            hVECode,
        VEContext          Context,
         __out_ecount(ulMaxLength) __out_z LPWSTR msg,
        unsigned int      ulMaxLength,
        SAFEARRAY          *psa)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(ulMaxLength >= 30);
        SO_TOLERANT;
    } CONTRACTL_END;

    BEGIN_SO_INTOLERANT_CODE(GetThread());

    VerError err;
    memcpy(&err, &Context, sizeof(VerError));

    ValidateWorkerArgs argsDefault;
    ValidateWorkerArgs* pArgs = &argsDefault;

    // We passed a pointer to the ValidateWorkerArgs object through
    // the SAFEARRAY casted as a UINT because there was no room left in the
    // interface to pass information through it.
    {
        UINT dim;
        LONG l;
#ifdef _WIN64
        VARTYPE vt;
#endif // _WIN64
        VARIANT var;

        if(!psa) {
            goto lDone;
        }

        dim = SafeArrayGetDim(psa);            
        if (dim != 1) {
            _ASSERTE(!"There should be one element in the SafeArray");
            goto lDone;
        }

        if (FAILED(SafeArrayGetLBound(psa, 1, &l))) {
            _ASSERTE(false);
            goto lDone;
        }
        if (l != 0) {
            _ASSERTE(!"expected the lower bound to be zero");
            goto lDone;
        }

        if (FAILED(SafeArrayGetUBound(psa, 1, &l))) {
            _ASSERTE(false);
            goto lDone;
        }
        if (l != 0) {
            _ASSERTE(!"expected the upper bound to be zero");
            goto lDone;
        }
#ifdef _WIN64
        // This check fails on Win2K when it should pass
        SafeArrayGetVartype(psa, &vt);
        if(vt != VT_VARIANT) {
            _ASSERTE(!"expected the ElementType to be a VT_VARIANT");
            goto lDone;
        }
#endif // _WIN64
        l = 0;
        SafeArrayGetElement(psa, &l, &var);

#ifdef _WIN64
        if (V_VT(&var) != VT_UI8) { // We expect the VarType to be a VT_UI8 (VT_UI8 is not supported on Windows 2000)
            _ASSERTE(false);
            goto lDone;
        }

        pArgs = (ValidateWorkerArgs*)(size_t)V_UI8(&var);
#else
        // We don't check that the type is V_UINT here because that check fails on Win2K when it should pass
        pArgs = (ValidateWorkerArgs*)(size_t)V_UINT(&var);
#endif

    }
lDone: ;

    EX_TRY
    {
        Verifier::GetErrorMsg(hVECode, err, msg, ulMaxLength, pArgs);
    }
    EX_CATCH
    {
        GetFormattingErrorMsg(msg, ulMaxLength);
    }
    EX_END_CATCH(SwallowAllExceptions)

    END_SO_INTOLERANT_CODE;
    return S_OK;
}

HRESULT CorValidator::Validate(
        IVEHandler        *veh,
        IUnknown          *pAppDomain,
        unsigned long      ulFlags,
        unsigned long      ulMaxError,
        unsigned long      token,
        __in_z LPWSTR             fileName,
        BYTE               *pe,
        unsigned long      ulSize)
{
    WRAPPER_NO_CONTRACT;
    STATIC_CONTRACT_SO_TOLERANT;
    return ValidateHelper(veh, pAppDomain, 0, FALSE, ulFlags, ulMaxError,
                          token, fileName, pe, ulSize);
}

HRESULT CLRValidator::Validate(
        IVEHandler        *veh,
        unsigned long      ulAppDomainId,
        unsigned long      ulFlags,
        unsigned long      ulMaxError,
        unsigned long      token,
        __in_z LPWSTR             fileName,
        BYTE               *pe,
        unsigned long      ulSize)
{
    WRAPPER_NO_CONTRACT;
    STATIC_CONTRACT_SO_TOLERANT;
    return ValidateHelper(veh, NULL, ulAppDomainId, TRUE, ulFlags, ulMaxError,
                          token, fileName, pe, ulSize);
}

HRESULT CorValidator::FormatEventInfo(
        HRESULT            hVECode,
        VEContext          Context,
        __out_ecount(ulMaxLength) LPWSTR             msg,
        unsigned long      ulMaxLength,
        SAFEARRAY          *psa)
{
    WRAPPER_NO_CONTRACT;
    return FormatEventInfoHelper(hVECode, Context, msg, ulMaxLength, psa);
}

HRESULT CLRValidator::FormatEventInfo(
        HRESULT            hVECode,
        VEContext          Context,
        __out_ecount(ulMaxLength) LPWSTR             msg,
        unsigned long      ulMaxLength,
        SAFEARRAY          *psa)
{
    WRAPPER_NO_CONTRACT;
    STATIC_CONTRACT_SO_TOLERANT;
    return FormatEventInfoHelper(hVECode, Context, msg, ulMaxLength, psa);
}


