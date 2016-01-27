// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
** Header:  COMTypeLibConverter.cpp
**
**
** Purpose: Implementation of the native methods used by the 
**          typelib converter.
**
** 
===========================================================*/

#include "common.h"

#include "comtypelibconverter.h"
#include "runtimecallablewrapper.h"
#include "assembly.hpp"
#include "debugmacros.h"
#include <tlbimpexp.h>
#include "..\md\inc\imptlb.h"
#include <tlbutils.h>
#include "posterror.h"

BOOL            COMTypeLibConverter::m_bInitialized = FALSE;

void  COMTypeLibConverter::TypeLibImporterWrapper(
    ITypeLib    *pITLB,                 // Typelib to import.
    LPCWSTR     szFname,                // Name of the typelib, if known.
    LPCWSTR     szNamespace,            // Optional namespace override.
    IMetaDataEmit *pEmit,               // Metadata scope to which to emit.
    Assembly    *pAssembly,             // Assembly containing the imported module.
    Module      *pModule,               // Module we are emitting into.
    ITypeLibImporterNotifySink *pNotify,// Callback interface.
    TlbImporterFlags flags,             // Importer flags.
    CImportTlb  **ppImporter)           // The importer.
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(pITLB));
        PRECONDITION(CheckPointer(szFname, NULL_OK));
        PRECONDITION(CheckPointer(szNamespace, NULL_OK));
        PRECONDITION(CheckPointer(pEmit));
        PRECONDITION(CheckPointer(pAssembly));
        PRECONDITION(CheckPointer(pModule));
        PRECONDITION(CheckPointer(pNotify));
        PRECONDITION(CheckPointer(ppImporter));
    }
    CONTRACTL_END;
   
    HRESULT     hr;
    
    // Retrieve flag indicating whether runtime or linktime interface
    // security checks are required.
    BOOL bUnsafeInterfaces = (BOOL)(flags & TlbImporter_UnsafeInterfaces);

    // Determine if we import SAFEARRAY's as System.Array's.
    BOOL bSafeArrayAsSysArray = (BOOL)(flags & TlbImporter_SafeArrayAsSystemArray);

    // Determine if we are doing the [out,retval] transformation on disp only interfaces.
    BOOL bTransformDispRetVals = (BOOL)(flags & TlbImporter_TransformDispRetVals);

    // Determine if we are adding members to classes.
    BOOL bPreventClassMembers = (BOOL)(flags & TlbImporter_PreventClassMembers);

    // Determine if we are marking value classes as serializable
    BOOL bSerializableValueClasses = (BOOL)(flags & TlbImporter_SerializableValueClasses);
    
    // Create and initialize a TypeLib importer.
    NewPreempHolder<CImportTlb> pImporter = CImportTlb::CreateImporter(szFname, pITLB, true, bUnsafeInterfaces, bSafeArrayAsSysArray, bTransformDispRetVals, bPreventClassMembers, bSerializableValueClasses);
    if (!pImporter)
        COMPlusThrowOM();

    // If a namespace is specified, use it.
    if (szNamespace)
        pImporter->SetNamespace(szNamespace);

    // Set the various pointers.
    hr = pImporter->SetMetaData(pEmit);
    _ASSERTE(SUCCEEDED(hr) && "Couldn't get IMetaDataEmit* from Module");
    if (FAILED(hr))
        COMPlusThrowArgumentNull(W("pEmit"));
    
    pImporter->SetNotification(pNotify);
    pImporter->SetAssembly(pAssembly);
    pImporter->SetModule(pModule);

    // Do the conversion.
    hr = pImporter->Import();
    if (SUCCEEDED(hr))
    {
        *ppImporter = pImporter;
        pImporter.SuppressRelease();
    }
    else
    {
        COMPlusThrowHR(hr, kGetErrorInfo);
    }
} // HRESULT COMTypeLibConverter::TypeLibImporterWrapper()


void COMTypeLibConverter::ConvertAssemblyToTypeLibInternal(OBJECTREF* ppAssembly, 
                                                                STRINGREF* ppTypeLibName, 
                                                                DWORD Flags, 
                                                                OBJECTREF* ppNotifySink,
                                                                OBJECTREF* pRetObj)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION (IsProtectedByGCFrame (ppAssembly));
        PRECONDITION (IsProtectedByGCFrame (ppTypeLibName));
        PRECONDITION (IsProtectedByGCFrame (ppNotifySink));
        PRECONDITION (IsProtectedByGCFrame (pRetObj));
        INJECT_FAULT(COMPlusThrowOM());
    }
    CONTRACTL_END;
    
    Assembly    *pAssembly=0;           // Assembly to export.

    NewArrayHolder<WCHAR>                       szTypeLibName=0;  // The name for the typelib.
    SafeComHolder<ITypeLib>                     pTLB=0;     // The new typelib.
    SafeComHolder<ITypeLibExporterNotifySink>   pINotify=0; // Callback parameter.

    // Make sure the COMTypeLibConverter has been initialized.
    if (!m_bInitialized)
        Init();

    // Validate flags
    if ((Flags & ~TlbExporter_ValidFlags) != 0)
        COMPlusThrowArgumentOutOfRange(W("flags"), W("Argument_InvalidFlag"));

    // Retrieve the callback.
    if (*ppNotifySink == NULL)
        COMPlusThrowArgumentNull(W("notifySink"));
    
    pINotify = (ITypeLibExporterNotifySink*)GetComIPFromObjectRef(ppNotifySink, MscorlibBinder::GetClass(CLASS__ITYPE_LIB_EXPORTER_NOTIFY_SINK));
    if (!pINotify)
        COMPlusThrow(kArgumentException, W("Arg_NoImporterCallback"));
        
    // If a name was specified then copy it to a temporary string.
    if (*ppTypeLibName != NULL)
    {
        int TypeLibNameLen = (*ppTypeLibName)->GetStringLength();
        szTypeLibName = new WCHAR[TypeLibNameLen + 1];
        memcpyNoGCRefs(szTypeLibName, (*ppTypeLibName)->GetBuffer(), TypeLibNameLen * sizeof(WCHAR));
        szTypeLibName[TypeLibNameLen] = 0;
    }

    // Retrieve the assembly from the AssemblyBuilder argument.
    if (*ppAssembly == NULL)
        COMPlusThrowNonLocalized(kArgumentNullException, W("assembly"));

    pAssembly = ((ASSEMBLYREF)*ppAssembly)->GetAssembly();
    _ASSERTE(pAssembly);

    if (IsAfContentType_WindowsRuntime(pAssembly->GetFlags()))
        COMPlusThrow(kArgumentException, W("Argument_AssemblyWinMD"));

    {
        GCX_PREEMP();
        ExportTypeLibFromLoadedAssembly(pAssembly, szTypeLibName, &pTLB, pINotify, Flags);
    }

    // Make sure we got a typelib back.
    _ASSERTE(pTLB);

    // Convert the ITypeLib interface pointer to a COM+ object.
    GetObjectRefFromComIP(pRetObj, pTLB, NULL);
}

// static
void COMTypeLibConverter::LoadType(
    Module * pModule,
    mdTypeDef cl,
    TlbImporterFlags Flags)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        INJECT_FAULT(COMPlusThrowOM());
    }
    CONTRACTL_END;

    OBJECTREF pThrowable = NULL;
    
    GCPROTECT_BEGIN(pThrowable)
    {    
        EX_TRY
        {
            // Load the EE class that represents the type, so that
            // the TypeDefToMethodTable rid map contains this entry
            // (They were going to be loaded, anyway, to generate comtypes)
            TypeHandle typeHnd;
            typeHnd = ClassLoader::LoadTypeDefThrowing(pModule, cl, 
                                                       ClassLoader::ThrowIfNotFound, 
                                                       ClassLoader::PermitUninstDefOrRef);
        }
        EX_CATCH
        {
            pThrowable = GET_THROWABLE();
        }
        EX_END_CATCH(SwallowAllExceptions);

        if (pThrowable != NULL)
        {
            // Only spit out a special message if PreventClassMembers is set.
            if ((Flags & TlbImporter_PreventClassMembers) == 0)
            {
                struct _gc
                {
                    OBJECTREF pInnerException;
                    OBJECTREF pThrowable;
                    STRINGREF pMsg;
                } gc;
                
                gc.pInnerException = NULL;
                gc.pThrowable = NULL;
                gc.pMsg = NULL;
                
                GCPROTECT_BEGIN(gc);
                {
                    MethodTable* pMT = MscorlibBinder::GetException(kSystemException);

                    gc.pThrowable = AllocateObject(pMT);
                    gc.pInnerException = pThrowable;
                    ResMgrGetString(W("Arg_ImporterLoadFailure"), &gc.pMsg);                        

                    MethodDescCallSite exceptionCtor(METHOD__SYSTEM_EXCEPTION__STR_EX_CTOR, &gc.pThrowable);

                    ARG_SLOT args[] = { ObjToArgSlot(gc.pThrowable),
                                        ObjToArgSlot(gc.pMsg),
                                        ObjToArgSlot(gc.pInnerException) };

                    exceptionCtor.Call(args);

                    COMPlusThrow(gc.pThrowable);
                }
                GCPROTECT_END();
            }
            
            COMPlusThrow(pThrowable);
        }
    }
    GCPROTECT_END();
}

void COMTypeLibConverter::ConvertTypeLibToMetadataInternal(OBJECTREF* ppTypeLib, 
                                                           OBJECTREF* ppAsmBldr, 
                                                           OBJECTREF* ppModBldr, 
                                                           STRINGREF* ppNamespace, 
                                                           TlbImporterFlags Flags, 
                                                           OBJECTREF* ppNotifySink, 
                                                           OBJECTREF* pEventItfInfoList)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        INJECT_FAULT(COMPlusThrowOM());
        PRECONDITION(IsProtectedByGCFrame (ppTypeLib));
        PRECONDITION(IsProtectedByGCFrame (ppAsmBldr));
        PRECONDITION(IsProtectedByGCFrame (ppModBldr));
        PRECONDITION(IsProtectedByGCFrame (ppNamespace));
        PRECONDITION(IsProtectedByGCFrame (ppNotifySink));
        PRECONDITION(IsProtectedByGCFrame (pEventItfInfoList));
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;

    Module      *pModule = NULL;               // ModuleBuilder parameter.
    Assembly    *pAssembly = NULL;      // AssemblyBuilder parameter.
    REFLECTMODULEBASEREF pReflect = NULL;      // ReflectModule passed as param.
    int         cTypeDefs;              // Count of imported TypeDefs.
    int         i;                      // Loop control.
    mdTypeDef   cl;                     // An imported TypeDef.

    NewArrayHolder<WCHAR>        szNamespace = NULL;            // The namespace to put the type in.
    NewPreempHolder<CImportTlb>  pImporter = NULL;              // The importer used to import the typelib.
    SafeComHolder<ITypeLib>      pTLB = NULL;                   // TypeLib parameter.
    SafeComHolder<ITypeLibImporterNotifySink> pINotify = NULL;  // Callback parameter.

    // Make sure the COMTypeLibConverter has been initialized.
    if (!m_bInitialized)
        Init();

    // Validate the flags.
    if ((Flags & ~TlbImporter_ValidFlags) != 0)
        COMPlusThrowArgumentOutOfRange(W("flags"), W("Argument_InvalidFlag"));
    
    // Retrieve the callback.
    MethodTable * pSinkMT = MscorlibBinder::GetClass(CLASS__ITYPE_LIB_IMPORTER_NOTIFY_SINK);
    pINotify = (ITypeLibImporterNotifySink*)GetComIPFromObjectRef(ppNotifySink, pSinkMT);
    if (!pINotify)
        COMPlusThrow(kArgumentException, W("Arg_NoImporterCallback"));
        
    pReflect = (REFLECTMODULEBASEREF) *ppModBldr;
    _ASSERTE(pReflect);
    
   
    pModule = pReflect->GetModule();
    _ASSERTE(pModule);

    // Suppress capturing while we dispatch events. This is a performance optimization to avoid
    // re-serializing metadata between each type. Instead, we suppress serialization while we bake all
    // the types and then re-enable it at the end (when this holder goes out of scope).
    _ASSERTE(pModule->IsReflection());
    ReflectionModule::SuppressMetadataCaptureHolder holderCapture(pModule->GetReflectionModule());
        

    // Retrieve the assembly from the AssemblyBuilder argument.
    pAssembly = ((ASSEMBLYREF)*ppAsmBldr)->GetAssembly();
    _ASSERTE(pAssembly);

    // Retrieve a pointer to the ITypeLib interface.
    pTLB = (ITypeLib*)GetComIPFromObjectRef(ppTypeLib, IID_ITypeLib);
    if (!pTLB)
        COMPlusThrow(kArgumentException, W("Arg_NoITypeLib"));
    
    // If a namespace was specified then copy it to a temporary string.
    if (*ppNamespace != NULL)
    {
        int NamespaceLen = (*ppNamespace)->GetStringLength();
        szNamespace = new WCHAR[NamespaceLen + 1];
        memcpyNoGCRefs(szNamespace, (*ppNamespace)->GetBuffer(), NamespaceLen * sizeof(WCHAR));
        szNamespace[NamespaceLen] = 0;
    }

    // Switch to preemptive GC before we call out to COM.
    {
        GCX_PREEMP();
        
        // Have to wrap the CImportTlb object in a call, because it has a destructor.
        TypeLibImporterWrapper(pTLB, NULL /*filename*/, szNamespace,
                               pModule->GetEmitter(), pAssembly, pModule, pINotify,
                               Flags, &pImporter);
    }

    // Enumerate the types imported from the typelib, and add them to the assembly's available type table.
    IMDInternalImport* pInternalImport = pModule->GetMDImport();
    HENUMTypeDefInternalHolder hEnum(pInternalImport);
        
    hEnum.EnumTypeDefInit();
    cTypeDefs = pInternalImport->EnumTypeDefGetCount(&hEnum);

    for (i=0; i<cTypeDefs; ++i)
    {
        BOOL success = pInternalImport->EnumTypeDefNext(&hEnum, &cl);
        _ASSERTE(success);
            
        pAssembly->AddType(pModule, cl);
    }

    // Allocate an empty array
    CreateItfInfoList(pEventItfInfoList);

#ifdef _DEBUG
    if (!g_pConfig->TlbImpSkipLoading())
    {
#endif // _DEBUG
        pInternalImport->EnumReset(&hEnum);
        for (i=0; i<cTypeDefs; ++i)
        {
            BOOL success = pInternalImport->EnumTypeDefNext(&hEnum, &cl);
            _ASSERTE(success);

            LoadType(pModule, cl, Flags);
        }

        // Retrieve the event interface list.
        GetEventItfInfoList(pImporter, pAssembly, pEventItfInfoList);
#ifdef _DEBUG
    }
#endif // _DEBUG
}

void COMTypeLibConverter::CreateItfInfoList(OBJECTREF* pEventItfInfoList)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        INJECT_FAULT(COMPlusThrowOM());
        PRECONDITION(IsProtectedByGCFrame (pEventItfInfoList));
    }
    CONTRACTL_END;
    
    // Allocate the array list that will contain the event sources.
    SetObjectReference(pEventItfInfoList, 
                       AllocateObject(MscorlibBinder::GetClass(CLASS__ARRAY_LIST)),
                       SystemDomain::GetCurrentDomain());

    MethodDescCallSite ctor(METHOD__ARRAY_LIST__CTOR, pEventItfInfoList);

    // Call the ArrayList constructor.
    ARG_SLOT CtorArgs[] =
    { 
        ObjToArgSlot(*pEventItfInfoList)
    };
    ctor.Call(CtorArgs);
}

//*****************************************************************************
//*****************************************************************************
void COMTypeLibConverter::GetEventItfInfoList(CImportTlb *pImporter, Assembly *pAssembly, OBJECTREF *pEventItfInfoList)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        INJECT_FAULT(COMPlusThrowOM());
        PRECONDITION(CheckPointer(pImporter));
        PRECONDITION(CheckPointer(pAssembly));
        PRECONDITION(IsProtectedByGCFrame (pEventItfInfoList));
    }
    CONTRACTL_END;

    UINT                            i;              
    CQuickArray<ImpTlbEventInfo*>   qbEvInfoList;

    // Retrieve the list of event interfaces.
    pImporter->GetEventInfoList(qbEvInfoList);

    // Iterate over TypeInfos.
    for (i = 0; i < qbEvInfoList.Size(); i++)
    {
        // Retrieve the Add method desc for the ArrayList.
        MethodDescCallSite addMeth(METHOD__ARRAY_LIST__ADD, pEventItfInfoList);

        // Retrieve the event interface info for the current CoClass.
        OBJECTREF EventItfInfoObj = GetEventItfInfo(pAssembly, qbEvInfoList[i]);
        _ASSERTE(EventItfInfoObj);

        // Add the event interface info to the list.
        ARG_SLOT AddArgs[] = { 
            ObjToArgSlot(*pEventItfInfoList),
            ObjToArgSlot(EventItfInfoObj)
        };
        addMeth.Call(AddArgs);
    }
} // LPVOID COMTypeLibConverter::GetTypeLibEventSourceList()

//*****************************************************************************
// Initialize the COMTypeLibConverter.
//*****************************************************************************
void COMTypeLibConverter::Init()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;

    // Ensure COM is started up.
    EnsureComStarted();

    // Set the initialized flag to TRUE.
    m_bInitialized = TRUE;
} // void COMTypeLibConverter::Init()

//*****************************************************************************
// Given an imported class in an assembly, generate a list of event sources.
//*****************************************************************************
OBJECTREF COMTypeLibConverter::GetEventItfInfo(Assembly *pAssembly, ImpTlbEventInfo *pImpTlbEventInfo)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        INJECT_FAULT(COMPlusThrowOM());
        PRECONDITION(CheckPointer(pAssembly));
        PRECONDITION(CheckPointer(pImpTlbEventInfo));
    }
    CONTRACTL_END;

    OBJECTREF   RetObj = NULL;

    struct _gc
    {
        OBJECTREF EventItfInfoObj;
        STRINGREF EventItfNameStrObj;
        STRINGREF SrcItfNameStrObj;
        STRINGREF EventProvNameStrObj;
        OBJECTREF AssemblyObj;
        OBJECTREF SrcItfAssemblyObj;
    } gc;
    ZeroMemory(&gc, sizeof(gc));

    GCPROTECT_BEGIN(gc)
    {
        // Create the EventSource object.
        gc.EventItfInfoObj = AllocateObject(MscorlibBinder::GetClass(CLASS__TCE_EVENT_ITF_INFO));
                            
        // Retrieve the assembly object.
        gc.AssemblyObj = pAssembly->GetExposedObject();

        // Retrieve the source interface assembly object (may be the same assembly).
        gc.SrcItfAssemblyObj = pImpTlbEventInfo->SrcItfAssembly->GetExposedObject();

        // Prepare the constructor arguments.
        gc.EventItfNameStrObj = StringObject::NewString(pImpTlbEventInfo->szEventItfName);       
        gc.SrcItfNameStrObj = StringObject::NewString(pImpTlbEventInfo->szSrcItfName);       
        gc.EventProvNameStrObj = StringObject::NewString(pImpTlbEventInfo->szEventProviderName);

        MethodDescCallSite ctor(METHOD__TCE_EVENT_ITF_INFO__CTOR, &gc.EventItfInfoObj);
        
        // Call the EventItfInfo constructor.
        ARG_SLOT CtorArgs[] = { 
            ObjToArgSlot(gc.EventItfInfoObj),
            ObjToArgSlot(gc.EventItfNameStrObj),
            ObjToArgSlot(gc.SrcItfNameStrObj),
            ObjToArgSlot(gc.EventProvNameStrObj),
            ObjToArgSlot(gc.AssemblyObj),
            ObjToArgSlot(gc.SrcItfAssemblyObj),
        };
        ctor.Call(CtorArgs);

        RetObj = gc.EventItfInfoObj;
    }
    GCPROTECT_END();

    return RetObj;
} // OBJECTREF COMTypeLibConverter::GetEventSourceInfo()

//*****************************************************************************
// Given the string persisted from a TypeLib export, recreate the assembly
//  reference.
//*****************************************************************************
mdAssemblyRef DefineAssemblyRefForExportedAssembly(
    LPCWSTR     pszFullName,            // Full name of the assembly.
    IUnknown    *pIMeta)                // Metadata emit interface.
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pszFullName));
        PRECONDITION(CheckPointer(pIMeta));
    }
    CONTRACTL_END;
    
    mdAssemblyRef ar=0;
    HRESULT     hr;                                 // A result.  
    AssemblySpec spec;                              // "Name" of assembly.
    CQuickArray<char> rBuf;
    int iLen;    
    SafeComHolder<IMetaDataAssemblyEmit> pMeta=0;   // Emit interface.
    
    iLen = WszWideCharToMultiByte(CP_ACP,0, pszFullName,-1, 0,0, 0,0);
    IfFailGo(rBuf.ReSizeNoThrow(iLen+1));
    WszWideCharToMultiByte(CP_ACP,0, pszFullName,-1, rBuf.Ptr(),iLen+1, 0,0);
     
    // Restore the AssemblySpec data.
    IfFailGo(spec.Init(rBuf.Ptr()));
    
    // Make sure we have the correct pointer type.
    IfFailGo(SafeQueryInterface(pIMeta, IID_IMetaDataAssemblyEmit, (IUnknown**)&pMeta));
    
    // Create the assemblyref token.
    IfFailGo(spec.EmitToken(pMeta, &ar));
        
ErrExit:
    return ar;
} // mdAssemblyRef DefineAssemblyRefForExportedAssembly()

//*****************************************************************************
// Public helper function used by typelib converter to create AssemblyRef
//  for a referenced typelib.
//*****************************************************************************
extern mdAssemblyRef DefineAssemblyRefForImportedTypeLib(
    void        *pvAssembly,            // Assembly importing the typelib.
    void        *pvModule,              // Module importing the typelib.
    IUnknown    *pIMeta,                // IMetaData* from import module.
    IUnknown    *pIUnk,                 // IUnknown to referenced Assembly.
    BSTR        *pwzNamespace,          // The namespace of the resolved assembly.
    BSTR        *pwzAsmName,            // The name of the resolved assembly.
    Assembly    **ppAssemblyRef)        // The resolved assembly.        
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pvAssembly));
        PRECONDITION(CheckPointer(pvModule));
        PRECONDITION(CheckPointer(pIMeta));
        PRECONDITION(CheckPointer(pIUnk));
        PRECONDITION(CheckPointer(pwzNamespace));
        PRECONDITION(CheckPointer(pwzAsmName));
        PRECONDITION(CheckPointer(ppAssemblyRef, NULL_OK));
    }
    CONTRACTL_END;
    
    // This is a workaround to allow an untyped param.  To really fix, move imptlb to this project,
    //  and out of the metadata project.  Once here, imptlb can just reference any of 
    //  the .h files in this project.
    Assembly*       pAssembly = reinterpret_cast<Assembly*>(pvAssembly);
    Module*         pTypeModule = reinterpret_cast<Module*>(pvModule);
    HRESULT         hr;
    Assembly*       pRefdAssembly           = NULL;
    IMetaDataEmit*  pEmitter                = NULL;
    MethodTable*    pAssemblyClass          = NULL;
    mdAssemblyRef   ar                      = mdAssemblyRefNil;
    Module*         pManifestModule         = NULL;
    mdTypeDef       td                      = 0;
    LPCSTR          szName                  = NULL;
    LPCSTR          szNamespace = NULL;
    CQuickBytes qb;
    WCHAR* wszBuff = (WCHAR*) qb.AllocThrows((MAX_CLASSNAME_LENGTH+1) * sizeof(WCHAR));
    SString         szRefdAssemblyName;
    IMDInternalImport*      pRefdMDImport = NULL;
    SafeComHolder<IMetaDataAssemblyEmit>  pAssemEmitter = NULL;

    GCX_COOP();

    // Initialize the output strings to NULL.
    *pwzNamespace = NULL;
    *pwzAsmName = NULL;
    BSTRHolder local_pwzNamespace = NULL;
    BSTRHolder local_pwzAsmName = NULL;

    // Get the Referenced Assembly object from the IUnknown.
    PREFIX_ASSUME(pIUnk != NULL);
    ASSEMBLYREF RefdAsmObj = NULL;
    GCPROTECT_BEGIN(RefdAsmObj);
    GetObjectRefFromComIP((OBJECTREF*)&RefdAsmObj, pIUnk, pAssemblyClass);
    PREFIX_ASSUME(RefdAsmObj != NULL);

    // Get the internal assembly from the assembly object.
    pRefdAssembly = RefdAsmObj->GetAssembly();
    GCPROTECT_END();
    PREFIX_ASSUME(pRefdAssembly != NULL);

    // Return the assembly if asked for
    if (ppAssemblyRef)
        *ppAssemblyRef = pRefdAssembly;

    // Get the manifest module for the importing and the referenced assembly.
    pManifestModule = pAssembly->GetManifestModule();  
        
    // Define the AssemblyRef in the global assembly.
    pEmitter = pManifestModule->GetEmitter();
    _ASSERTE(pEmitter);
    IfFailGo(SafeQueryInterface(pEmitter, IID_IMetaDataAssemblyEmit, (IUnknown**) &pAssemEmitter));
    ar = pAssembly->AddAssemblyRef(pRefdAssembly, pAssemEmitter);
    pAssemEmitter.Release();

    // Add the assembly ref token and the manifest module it is referring to the manifest module's rid map.
    pManifestModule->StoreAssemblyRef(ar, pRefdAssembly);

    // Add assembly ref in module manifest.
    IfFailGo(SafeQueryInterface(pIMeta, IID_IMetaDataAssemblyEmit, (IUnknown**) &pAssemEmitter));
    ar = pAssembly->AddAssemblyRef(pRefdAssembly, pAssemEmitter);    

    // Add the assembly ref token and the manifest module it is referring to the rid map of the module we are 
    // emiting into.
    pTypeModule->StoreAssemblyRef(ar, pRefdAssembly);
    
    // Retrieve the first typedef in the assembly.
    {
        ModuleIterator i = pRefdAssembly->IterateModules();
        Module *pRefdModule = NULL;
    
        while (i.Next())
        {
            pRefdModule = i.GetModule();
            pRefdMDImport = pRefdModule->GetMDImport();
            HENUMTypeDefInternalHolder hTDEnum(pRefdMDImport);

            IfFailGo(hTDEnum.EnumTypeDefInitNoThrow());

            if (pRefdMDImport->EnumTypeDefNext(&hTDEnum, &td) == true)
            {
                IfFailGo(pRefdMDImport->GetNameOfTypeDef(td, &szName, &szNamespace));
                break;
            }
        }
    }

    // DefineAssemblyRefForImportedTypeLib should never be called for assemblies that
    // do not contain any types so we better have found one.
    _ASSERTE(szNamespace);

    // Give the namespace back to the caller.
    WszMultiByteToWideChar(CP_UTF8,0, szNamespace, -1, wszBuff, MAX_CLASSNAME_LENGTH);
    local_pwzNamespace = SysAllocString(wszBuff);
    IfNullGo(local_pwzNamespace);

    // Give the assembly name back to the caller.
    pRefdAssembly->GetDisplayName(szRefdAssemblyName);
    local_pwzAsmName = SysAllocString(szRefdAssemblyName);
    IfNullGo(local_pwzAsmName);

ErrExit:
    if (FAILED(hr))
    {
        ar = mdAssemblyRefNil;
    }
    else
    {
        local_pwzNamespace.SuppressRelease();
        local_pwzAsmName.SuppressRelease();
        *pwzNamespace = local_pwzNamespace;
        *pwzAsmName = local_pwzAsmName;
    }
    
    return ar;
} // mdAssemblyRef DefineAssemblyRefForImportedTypeLib()



//*****************************************************************************
// A typelib exporter.
//*****************************************************************************
FCIMPL4(Object*, COMTypeLibConverter::ConvertAssemblyToTypeLib, Object* AssemblyUNSAFE, StringObject* TypeLibNameUNSAFE, DWORD Flags, Object* NotifySinkUNSAFE)
{
    FCALL_CONTRACT;

    OBJECTREF RetObj = NULL;
    struct _gc
    {
        OBJECTREF Assembly;
        STRINGREF TypeLibName;
        OBJECTREF NotifySink;
        OBJECTREF RetObj;
    } gc;
    
    gc.Assembly = (OBJECTREF) AssemblyUNSAFE;
    gc.TypeLibName = (STRINGREF) TypeLibNameUNSAFE;
    gc.NotifySink = (OBJECTREF) NotifySinkUNSAFE;
    gc.RetObj = NULL;
    
    HELPER_METHOD_FRAME_BEGIN_RET_PROTECT(gc);

    ConvertAssemblyToTypeLibInternal(&gc.Assembly, &gc.TypeLibName, Flags, &gc.NotifySink, &gc.RetObj);

    HELPER_METHOD_FRAME_END();
    return OBJECTREFToObject(gc.RetObj);
} // LPVOID COMTypeLibConverter::ConvertAssemblyToTypeLib()
FCIMPLEND

//*****************************************************************************
// Import a typelib as metadata.  Doesn't add TCE adapters.
//*****************************************************************************
FCIMPL7(void, COMTypeLibConverter::ConvertTypeLibToMetadata, Object* TypeLibUNSAFE, Object* AsmBldrUNSAFE, Object* ModBldrUNSAFE, StringObject* NamespaceUNSAFE, TlbImporterFlags Flags, Object* NotifySinkUNSAFE, OBJECTREF* pEventItfInfoList)
{
    FCALL_CONTRACT;

    struct _gc
    {
        OBJECTREF TypeLib;
        OBJECTREF AsmBldr;
        OBJECTREF ModBldr;
        STRINGREF Namespace;
        OBJECTREF NotifySink;
    } gc;
    
    gc.TypeLib = (OBJECTREF) TypeLibUNSAFE;
    gc.AsmBldr = (OBJECTREF) AsmBldrUNSAFE;
    gc.ModBldr = (OBJECTREF) ModBldrUNSAFE;
    gc.Namespace = (STRINGREF) NamespaceUNSAFE;
    gc.NotifySink = (OBJECTREF) NotifySinkUNSAFE;
    
    HELPER_METHOD_FRAME_BEGIN_PROTECT(gc);

    ASSUME_BYREF_FROM_JIT_STACK_BEGIN(pEventItfInfoList);
    ConvertTypeLibToMetadataInternal(&gc.TypeLib, &gc.AsmBldr, &gc.ModBldr, &gc.Namespace, Flags, &gc.NotifySink, pEventItfInfoList);
    ASSUME_BYREF_FROM_JIT_STACK_END();

    HELPER_METHOD_FRAME_END();
} // void COMTypeLibConverter::ConvertTypeLibToMetadata()
FCIMPLEND
