// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//===========================================================================
// File: TlbExport.CPP
//

//
// Notes: Create a TypeLib from COM+ metadata.
//---------------------------------------------------------------------------

#include "common.h"

#include "comcallablewrapper.h"
#include "field.h"
#include "dllimport.h"
#include "fieldmarshaler.h"
#include "eeconfig.h"
#include "comdelegate.h"
#include <nsutilpriv.h>
#include <tlbimpexp.h>
#include <mlang.h>
#include "tlbexport.h"
#include "commtmemberinfomap.h"
#include <corerror.h>
#include "posterror.h"
#include "typeparse.h"

#if defined(VALUE_MASK)
#undef VALUE_MASK
#endif

#include <guidfromname.h>
#include <stgpool.h>
#include <siginfo.hpp>
#include <typestring.h>
#include "perfcounters.h"
#include "comtypelibconverter.h"
#include "caparser.h"

// Define to export an empty dispinterface for an AutoDispatch IClassX
#define EMPTY_DISPINTERFACE_ICLASSX
#ifndef S_USEIUNKNOWN
#define S_USEIUNKNOWN (HRESULT)2
#endif

#if defined(_DEBUG) && defined(_TRACE)
#define TRACE printf
#else
#define TRACE NullFn
inline void NullFn(const char *pf,...) {}
#endif

#if defined(_DEBUG)
#define IfFailReport(expr) \
    do { if(FAILED(hr = (expr))) { DebBreakHr(hr); ReportError(hr); } } while (0)    
#else
#define IfFailReport(expr) \
    do { if(FAILED(hr = (expr))) { ReportError(hr); } } while (0)    
#endif

//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// This value determines whether, by default, we add the TYPEFLAG_FPROXY bit 
//  to exported interfaces.  If the value is true, Automation proxy is the 
//  default, and we do not set the bit.  If the value is false, no Automation
//  proxy is the default and we DO set the bit.
#define DEFAULT_AUTOMATION_PROXY_VALUE TRUE
//-----------------------------------------------------------------------------                                     

//*****************************************************************************
// Constants.
//*****************************************************************************
static const WCHAR szRetVal[] = W("pRetVal");
static const WCHAR szTypeLibExt[] = W(".TLB");

static const WCHAR szTypeLibKeyName[] = W("TypeLib");
static const WCHAR szClsidKeyName[] = W("CLSID");

static const WCHAR szIClassX[] = W("_%ls");
static const int cbIClassX = 1;
static const WCHAR cIClassX = W('_');

static const WCHAR szAlias[] = W("_MIDL_COMPAT_%ls");
static const int cbAlias = lengthof(szAlias) - 1;
static const WCHAR szParamName[] = W("p%d");

static const WCHAR szGuidName[]         = W("GUID");

static const CHAR szObjectClass[]       = "Object";
static const CHAR szArrayClass[]        = "Array";
static const CHAR szDateTimeClass[]     = "DateTime";
static const CHAR szDecimalClass[]      = "Decimal";
static const CHAR szGuidClass[]         = "Guid";
static const CHAR szStringClass[]       = g_StringName;
static const CHAR szStringBufferClass[] = g_StringBufferName;
static const CHAR szIEnumeratorClass[]  = "IEnumerator";
static const CHAR szColor[]             = "Color";

static const char szRuntime[]       = {"System."};
static const size_t cbRuntime       = (lengthof(szRuntime)-1);

static const char szText[]          = {"System.Text."};
static const size_t cbText          = (lengthof(szText)-1);

static const char szCollections[]   = {"System.Collections."};
static const size_t cbCollections   = (lengthof(szCollections)-1);

static const char szDrawing[]       = {"System.Drawing."};
static const size_t cbDrawing       = (lengthof(szDrawing)-1);

// The length of the following string(w/o the terminator): "HKEY_CLASSES_ROOT\\CLSID\\{00000000-0000-0000-0000-000000000000}".
static const int cCOMCLSIDRegKeyLength = 62;

// The length of the following string(w/o the terminator): "{00000000-0000-0000-0000-000000000000}".
static const int cCLSIDStrLength = 38;

// {17093CC8-9BD2-11cf-AA4F-304BF89C0001}
static const GUID GUID_TRANS_SUPPORTED     = {0x17093CC8,0x9BD2,0x11cf,{0xAA,0x4F,0x30,0x4B,0xF8,0x9C,0x00,0x01}};

// {00020430-0000-0000-C000-000000000046}
static const GUID LIBID_STDOLE2 = { 0x00020430, 0x0000, 0x0000, { 0xc0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x46 } };

// {66504301-BE0F-101A-8BBB-00AA00300CAB}
static const GUID GUID_OleColor = { 0x66504301, 0xBE0F, 0x101A, { 0x8B, 0xBB, 0x00, 0xAA, 0x00, 0x30, 0x0C, 0xAB } };

// LIBID mscoree
static const GUID LIBID_MSCOREE = {0x5477469e,0x83b1,0x11d2,{0x8b,0x49,0x00,0xa0,0xc9,0xb7,0xc9,0xc4}};

static const char XXX_DESCRIPTION_TYPE[] = {"System.ComponentModel.DescriptionAttribute"};
static const char XXX_ASSEMBLY_DESCRIPTION_TYPE[] = {"System.Reflection.AssemblyDescriptionAttribute"};

//*****************************************************************************
// Table to map COM+ calling conventions to TypeLib calling conventions.
//*****************************************************************************
CALLCONV Clr2TlbCallConv[] =
{
    CC_STDCALL,         //  IMAGE_CEE_CS_CALLCONV_DEFAULT   = 0x0,  
    CC_CDECL,           //  IMAGE_CEE_CS_CALLCONV_C         = 0x1,  
    CC_STDCALL,         //  IMAGE_CEE_CS_CALLCONV_STDCALL   = 0x2,  
    CC_STDCALL,         //  IMAGE_CEE_CS_CALLCONV_THISCALL  = 0x3,  
    CC_FASTCALL,        //  IMAGE_CEE_CS_CALLCONV_FASTCALL  = 0x4,  
    CC_CDECL,           //  IMAGE_CEE_CS_CALLCONV_VARARG    = 0x5,  
    CC_MAX              //  IMAGE_CEE_CS_CALLCONV_FIELD     = 0x6,  
                        //  IMAGE_CEE_CS_CALLCONV_MAX       = 0x7   
};



// Forward declarations.
extern HRESULT _FillVariant(MDDefaultValue *pMDDefaultValue, VARIANT *pvar); 
extern HRESULT _FillMDDefaultValue(BYTE bType, void const *pValue, MDDefaultValue *pMDDefaultValue);

//*****************************************************************************
// Stolen from classlib.
//*****************************************************************************
double _TicksToDoubleDate(const __int64 ticks)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;
    
    const INT64     MillisPerSecond     = 1000;
    const INT64     MillisPerDay        = MillisPerSecond * 60 * 60 * 24;
    const INT64     TicksPerMillisecond = 10000;
    const INT64     TicksPerSecond      = TicksPerMillisecond * 1000;
    const INT64     TicksPerMinute      = TicksPerSecond * 60;
    const INT64     TicksPerHour        = TicksPerMinute * 60;
    const INT64     TicksPerDay         = TicksPerHour * 24;
    const int       DaysPer4Years       = 365 * 4 + 1;
    const int       DaysPer100Years     = DaysPer4Years * 25 - 1;
    const int       DaysPer400Years     = DaysPer100Years * 4 + 1;
    const int       DaysTo1899          = DaysPer400Years * 4 + DaysPer100Years * 3 - 367;
    const INT64     DoubleDateOffset    = DaysTo1899 * TicksPerDay;
    const int       DaysTo10000         = DaysPer400Years * 25 - 366;
    const INT64     MaxMillis           = DaysTo10000 * MillisPerDay;
    const int       DaysPerYear         = 365; // non-leap year
    const INT64     OADateMinAsTicks    = (DaysPer100Years - DaysPerYear) * TicksPerDay;

    // Returns OleAut's zero'ed date ticks.
    if (ticks == 0)
         return 0.0;
         
    if (ticks < OADateMinAsTicks)
         return 0.0;

     // Currently, our max date == OA's max date (12/31/9999), so we don't 
     // need an overflow check in that direction.
     __int64 millis = (ticks  - DoubleDateOffset) / TicksPerMillisecond;
     if (millis < 0) 
     {
         __int64 frac = millis % MillisPerDay;
         if (frac != 0) millis -= (MillisPerDay + frac) * 2;
     }
     
     return (double)millis / MillisPerDay;
} // double _TicksToDoubleDate()


//*****************************************************************************
// Get the name of a typelib or typeinfo, add it to error text.
//*****************************************************************************
void PostTypeLibError(
    IUnknown    *pUnk,                  // An interface on the typeinfo.
    HRESULT     hrT,                    // The TypeInfo error.
    HRESULT     hrX)                    // The Exporter error.
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(pUnk));
    }
    CONTRACTL_END;

    HRESULT     hr;                     // A result.
    WCHAR       rcErr[1024];            // Buffer for error message.

    SafeComHolder<ITypeInfo> pITI=0;             // The ITypeInfo * on the typeinfo.
    SafeComHolder<ITypeLib> pITLB=0;             // The ITypeLib *.
    BSTRHolder               name=0;             // The name of the TypeInfo.

    // Try to get a name.
    hr = SafeQueryInterface(pUnk, IID_ITypeInfo, (IUnknown**)&pITI);
    if (SUCCEEDED(hr))
    {
        IfFailThrow(pITI->GetDocumentation(MEMBERID_NIL, &name, 0,0,0));
    }
    else
    {
        hr = SafeQueryInterface(pUnk, IID_ITypeLib, (IUnknown**)&pITLB);
        if (SUCCEEDED(hr))
            IfFailThrow(pITLB->GetDocumentation(MEMBERID_NIL, &name, 0,0,0));
    }

    if (name == NULL)
    {
        name = SysAllocString(W("???"));
        if (name == NULL)
            COMPlusThrowHR(E_OUTOFMEMORY);
    }

    // Format the typelib error.
    FormatRuntimeError(rcErr, lengthof(rcErr), hrT);

    SString strHRHex;
    strHRHex.Printf("%.8x", hrX);

    COMPlusThrowHR(hrX, hrX, strHRHex, name, rcErr);
} // void PostTypeLibError()




void ExportTypeLibFromLoadedAssembly(
    Assembly    *pAssembly,             // The assembly.
    LPCWSTR     szTlb,                  // The typelib name.
    ITypeLib    **ppTlb,                // If not null, also return ITypeLib here.
    ITypeLibExporterNotifySink *pINotify,// Notification callback.
    int         flags)                  // Export flags.
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(pAssembly));
        PRECONDITION(CheckPointer(szTlb, NULL_OK));
        PRECONDITION(CheckPointer(ppTlb));
        PRECONDITION(CheckPointer(pINotify, NULL_OK));
    }
    CONTRACTL_END;

    HRESULT     hr = S_OK;

    TypeLibExporter exporter;           // Exporter object.
    LPCWSTR     szModule=0;             // Module filename.
    StackSString ssDrive;
    StackSString ssDir;
    StackSString ssFile;
    size_t      cchDrive;
    size_t      cchDir;
    size_t      cchFile;
    CQuickWSTR  rcTlb;     // Buffer for the tlb filename.
    int         bDynamic=0;             // If true, dynamic module.
    Module      *pModule;               // The Assembly's SecurityModule.
    
    pModule = pAssembly->GetManifestModule();
    _ASSERTE(pModule);

    // Retrieve the module filename.
    szModule = pModule->GetPath();   
    PREFIX_ASSUME(szModule != NULL);
    
    // Make sure the assembly has not been imported from COM.
    if (pAssembly->IsImportedFromTypeLib())
        COMPlusThrowHR(TLBX_E_CIRCULAR_EXPORT, (UINT)TLBX_E_CIRCULAR_EXPORT, W(""), szModule, NULL);

    // If the module is dynamic then it will not have a file name.  We
    //  assign a dummy name for typelib name (if the scope does not have
    //  a name), but won't create a typelib on disk.
    if (*szModule == 0)
    {
        bDynamic = TRUE;
        szModule = W("Dynamic");
    }

    // Create the typelib name, if none provided.  Don't create one for Dynamic modules.
    if (!szTlb || !*szTlb)
    {
        if (bDynamic)
            szTlb = W("");
        else
        {
            SplitPath(szModule, &ssDrive, &ssDir, &ssFile, nullptr);
            MakePath(rcTlb, ssDrive.GetUnicode(), ssDir.GetUnicode(), ssFile.GetUnicode(), szTypeLibExt);
            szTlb = rcTlb.Ptr();
        }
    }

    // Do the conversion.  
    exporter.Convert(pAssembly, szTlb, pINotify, flags);

    // Get a copy of the ITypeLib*
    IfFailThrow(exporter.GetTypeLib(IID_ITypeLib, (IUnknown**)ppTlb));
} // void ExportTypeLibFromLoadedAssemblyInternal()


HRESULT ExportTypeLibFromLoadedAssemblyNoThrow(
    Assembly    *pAssembly,             // The assembly.
    LPCWSTR     szTlb,                  // The typelib name.
    ITypeLib    **ppTlb,                // If not null, also return ITypeLib here.
    ITypeLibExporterNotifySink *pINotify,// Notification callback.
    int         flags)                  // Export flags.
{
    CONTRACTL
    {
        DISABLED(NOTHROW);
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;

    EX_TRY
    {
        ExportTypeLibFromLoadedAssembly(pAssembly, 
                                                szTlb,
                                                ppTlb,
                                                pINotify,
                                                flags);
    }
    EX_CATCH_HRESULT(hr);

    return hr;
}

//*****************************************************************************
// Default notification class.
//*****************************************************************************
class CDefaultNotify : public ITypeLibExporterNotifySink
{
public:
    virtual HRESULT __stdcall ReportEvent(
        ImporterEventKind EventKind,        // Type of event.
        long        EventCode,              // HR of event.
        BSTR        EventMsg)               // Text message for event.
    {
        LIMITED_METHOD_CONTRACT;
        return S_OK;
    } // virtual HRESULT __stdcall ReportEvent()
    
    //-------------------------------------------------------------------------
    virtual HRESULT __stdcall ResolveRef(
        IUnknown    *Asm, 
        IUnknown    **pRetVal) 
    {
        CONTRACTL
        {
            DISABLED(NOTHROW);
            GC_TRIGGERS;
            MODE_PREEMPTIVE;
            SO_TOLERANT;
            PRECONDITION(CheckPointer(Asm));
            PRECONDITION(CheckPointer(pRetVal));
        }
        CONTRACTL_END;
        
        HRESULT     hr = S_OK;              // A result.
        Assembly    *pAssembly=0;           // The referenced Assembly.
        ITypeLib    *pTLB=0;                // The created TypeLib.
        MethodTable *pAssemblyClass = NULL; //@todo -- get this.
        LPVOID      RetObj = NULL;          // The object to return.

        BEGIN_EXTERNAL_ENTRYPOINT(&hr)
        {
            {
                GCX_COOP_THREAD_EXISTS(GET_THREAD());
                // Get the Referenced Assembly from the IUnknown.
                ASSEMBLYREF asmRef = NULL;
                GCPROTECT_BEGIN(asmRef);
                GetObjectRefFromComIP((OBJECTREF*)&asmRef, Asm, pAssemblyClass);
                pAssembly = asmRef->GetAssembly();
                GCPROTECT_END();
            }
            
            // Default resolution provides no notification, flags are 0.
            ExportTypeLibFromLoadedAssembly(pAssembly, 0, &pTLB, 0 /*pINotify*/, 0 /* flags*/);
        }
        END_EXTERNAL_ENTRYPOINT;

        *pRetVal = pTLB;
        
        return hr;
    } // virtual HRESULT __stdcall ResolveRef()
    
    //-------------------------------------------------------------------------
    virtual HRESULT STDMETHODCALLTYPE QueryInterface(// S_OK or E_NOINTERFACE
        REFIID      riid,                   // Desired interface.
        void        **ppvObject)            // Put interface pointer here.
    {
        CONTRACTL
        {
            NOTHROW;
            GC_TRIGGERS;
            MODE_PREEMPTIVE;
            SO_TOLERANT;
            PRECONDITION(CheckPointer(ppvObject));
        }
        CONTRACTL_END;

        *ppvObject = 0;
        if (riid == IID_IUnknown || riid == IID_ITypeLibExporterNotifySink)
        {
            *ppvObject = this;
            return S_OK;
        }
        return E_NOINTERFACE;
    } // virtual HRESULT QueryInterface()
    
    //-------------------------------------------------------------------------
    virtual ULONG STDMETHODCALLTYPE AddRef(void) 
    {
        LIMITED_METHOD_CONTRACT;
        return 1;
    } // virtual ULONG STDMETHODCALLTYPE AddRef()
    
    //-------------------------------------------------------------------------
    virtual ULONG STDMETHODCALLTYPE Release(void) 
    {
        LIMITED_METHOD_CONTRACT;
        return 1;
    } // virtual ULONG STDMETHODCALLTYPE Release()
};

static CDefaultNotify g_Notify;

//*****************************************************************************
// CTOR/DTOR.  
//*****************************************************************************
TypeLibExporter::TypeLibExporter()
 :  m_pICreateTLB(0), 
    m_pIUnknown(0), 
    m_pIDispatch(0),
    m_pGuid(0),
    m_hIUnknown(-1)
{
    LIMITED_METHOD_CONTRACT;

#if defined(_DEBUG)
    static int i;
    ++i;    // So a breakpoint can be set.
#endif
} // TypeLibExporter::TypeLibExporter()

TypeLibExporter::~TypeLibExporter()
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
    }
    CONTRACTL_END;
    
    ReleaseResources();
} // TypeLibExporter::~TypeLibExporter()

//*****************************************************************************
// Get an interface pointer from the ICreateTypeLib interface.
//*****************************************************************************
HRESULT TypeLibExporter::GetTypeLib(
    REFGUID     iid,
    IUnknown    **ppITypeLib)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(ppITypeLib));
    }
    CONTRACTL_END;

    return SafeQueryInterface(m_pICreateTLB, iid, (IUnknown**)ppITypeLib);
} // HRESULT TypeLibExporter::GetTypeLib()

//*****************************************************************************
// LayOut a TypeLib.  Call LayOut on all ICreateTypeInfo2s first.
//*****************************************************************************
void TypeLibExporter::LayOut()       // S_OK or error.
{
    STANDARD_VM_CONTRACT;

    HRESULT     hr = S_OK;              // A result.
    int         cTypes;                 // Count of exported types.
    int         ix;                     // Loop control.
    CExportedTypesInfo *pData;          // For iterating the entries.

    cTypes = m_Exports.Count();
    
    // Call LayOut on all ICreateTypeInfo2*s.
    for (ix=0; ix<cTypes; ++ix)
    {
        pData = m_Exports[ix];
        if (pData->pCTI && FAILED(hr = pData->pCTI->LayOut()))
            PostTypeLibError(pData->pCTI, hr, TLBX_E_LAYOUT_ERROR);
    }
    
    for (ix=0; ix<cTypes; ++ix)
    {
        pData = m_Exports[ix];
        if (pData->pCTIClassItf && FAILED(hr = pData->pCTIClassItf->LayOut()))
            PostTypeLibError(pData->pCTIClassItf, hr, TLBX_E_LAYOUT_ERROR);
    }
    
    // Repeat for injected types.
    cTypes = m_InjectedExports.Count();
    for (ix=0; ix<cTypes; ++ix)
    {
        pData = m_InjectedExports[ix];
        if (pData->pCTI && FAILED(hr = pData->pCTI->LayOut()))
            PostTypeLibError(pData->pCTI, hr, TLBX_E_LAYOUT_ERROR);
    }
    
    for (ix=0; ix<cTypes; ++ix)
    {
        pData = m_InjectedExports[ix];
        if (pData->pCTIClassItf && FAILED(hr = pData->pCTIClassItf->LayOut()))
            PostTypeLibError(pData->pCTIClassItf, hr, TLBX_E_LAYOUT_ERROR);
    }
} // HRESULT TypeLibExporter::LayOut()

//*****************************************************************************
// Release all pointers.
//*****************************************************************************
void TypeLibExporter::ReleaseResources()
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    // Release the ITypeInfo* pointers.
    m_Exports.Clear();
    m_InjectedExports.Clear();

    // Clean up the created TLB.
    SafeRelease(m_pICreateTLB);
    m_pICreateTLB = 0;

    // Clean up the ITypeInfo*s for well-known interfaces.
    SafeRelease(m_pIUnknown);
    m_pIUnknown = 0;

    SafeRelease(m_pIDispatch);
    m_pIDispatch = 0;

    SafeRelease(m_pGuid);
    m_pGuid = 0;
} // void TypeLibExporter::ReleaseResources()

//*****************************************************************************
// Enumerate the Types in a Module, add to the list.
//*****************************************************************************
void TypeLibExporter::AddModuleTypes(
    Module     *pModule)                // The module to convert.
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(pModule));
    }
    CONTRACTL_END;

    HRESULT     hr;
    ULONG       cTD;                    // Count of typedefs.
    mdTypeDef   td;                     // A TypeDef.
    MethodTable     *pClass;            // A MethodTable for a TypeDef.
    ULONG       ix;                     // Loop control.
    CExportedTypesInfo *pExported;      // For adding classes to the exported types cache.
    CExportedTypesInfo sExported;       // For adding classes to the exported types cache.
    

    // Convert all the types visible to COM.
    // Get an enumerator on TypeDefs in the scope.
    HENUMInternalHolder eTD(pModule->GetMDImport());
    eTD.EnumTypeDefInit();
    cTD = pModule->GetMDImport()->EnumTypeDefGetCount(&eTD);

    // Add all the classes to the hash.
    for (ix=0; ix<cTD; ++ix)
    {
        ZeroHolder  zhType = &m_ErrorContext.m_pScope;              // Clear error reporting info.
        
        // Get the TypeDef.
        if (!pModule->GetMDImport()->EnumTypeDefNext(&eTD, &td))
            IfFailReport(E_UNEXPECTED);
        
        IMDInternalImport* pInternalImport = pModule->GetMDImport();

        // Error reporting info.
        m_ErrorContext.m_tkType = td;
        m_ErrorContext.m_pScope = pModule->GetMDImport();

        // Get the class, perform the step.
        pClass = LoadClass(pModule, td);
        
        // Enumerate the formal type parameters
        HENUMInternal   hEnumGenericPars;
        hr = pInternalImport->EnumInit(mdtGenericParam, td, &hEnumGenericPars);
        if (SUCCEEDED(hr))
        {
            DWORD numGenericArgs = pInternalImport->EnumGetCount(&hEnumGenericPars);            
            // skip generic classes
            if( numGenericArgs  > 0 )
            {
                // We'll only warn if the type is marked ComVisible.
                if (SpecialIsGenericTypeVisibleFromCom(TypeHandle(pClass)))
                    ReportWarning(TLBX_I_GENERIC_TYPE, TLBX_I_GENERIC_TYPE);
                
                continue; 
            }
        }           
    
        // If the flag to not ignore non COM visible types in name decoration is set, then 
        // add the ComVisible(false) types to our list of exported types by skipping this check.
        if ((m_flags & TlbExporter_OldNames) == 0)
        {            
            // If the type isn't visible from COM, don't add it to the list of exports.
            if (!IsTypeVisibleFromCom(TypeHandle(pClass)))
                continue;
        }

        // See if this class is already in the list.
        sExported.pClass = pClass;
        pExported = m_Exports.Find(&sExported);
        if (pExported != 0)
            continue;
        
        // New class, add to list.
        pExported = m_Exports.Add(&sExported);
        if (!pExported)
            IfFailReport(E_OUTOFMEMORY);
        
        // Prefix can't tell that IfFailReport will actually throw an exception if pExported is NULL so
        // let's tell it explicitly that if we reach this point pExported will not be NULL.
        PREFIX_ASSUME(pExported != NULL);        
        pExported->pClass = pClass;
        pExported->pCTI = 0;
        pExported->pCTIClassItf = 0;
    }
} // HRESULT TypeLibExporter::AddModuleTypes()

//*****************************************************************************
// Enumerate the Modules in an assembly, add the types to the list.
//*****************************************************************************
void TypeLibExporter::AddAssemblyTypes(
    Assembly    *pAssembly)              // The assembly to convert.
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(pAssembly));
    }
    CONTRACTL_END;

    Module      *pManifestModule;       // A module in the assembly.
    mdFile      mf;                     // A file token.

    if (pAssembly->GetManifestImport())
    {
        // Enumerator over the modules of the assembly.
        HENUMInternalHolder phEnum(pAssembly->GetManifestImport());
        phEnum.EnumInit(mdtFile, mdTokenNil);

        // Get the module for the assembly.
        pManifestModule = pAssembly->GetManifestModule();
        AddModuleTypes(pManifestModule);
        
        while (pAssembly->GetManifestImport()->EnumNext(&phEnum, &mf))
        {
            DomainFile *pDomainFile = pAssembly->GetManifestModule()->LoadModule(GetAppDomain(), mf, FALSE);

            if (pDomainFile != NULL && !pDomainFile->GetFile()->IsResource())
                AddModuleTypes(pDomainFile->GetModule());
        }
    }
} // HRESULT TypeLibExporter::AddAssemblyTypes()
    
//*****************************************************************************
// Convert COM+ metadata to a typelib.
//*****************************************************************************
void TypeLibExporter::Convert(
    Assembly    *pAssembly,             // The Assembly to convert
    LPCWSTR     szTlbName,              // Name of resulting TLB
    ITypeLibExporterNotifySink *pNotify,// Notification callback.
    int         flags)                  // Conversion flags
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(pAssembly));
        PRECONDITION(CheckPointer(szTlbName));
        PRECONDITION(CheckPointer(pNotify, NULL_OK));
    }
    CONTRACTL_END;

    HRESULT     hr = S_OK;              // A result.
    ULONG       i;                      // Loop control.
    SString     sName;                  // Library name.
    GUID        guid;                   // Library guid.
    VARIANT     vt = {0};               // Variant for ExportedFromComPlus.
    CQuickArray<WCHAR> qLocale;         // Wide string for locale.
    LCID        lcid;                   // LCID for typelib, default 0.
    
    // Set PerfCounters
    COUNTER_ONLY(GetPerfCounters().m_Interop.cTLBExports++);

    SafeComHolder<IMultiLanguage> pIML=0;     // For locale->lcid conversion.
    SafeComHolder<ITypeLib> pITLB=0;          // TypeLib for IUnknown, IDispatch.
    BSTRHolder              szTIName=0;       // Name of a TypeInfo.
    BSTRHolder              szDescription=0;  // Assembly Description.

    // Error reporting information.
    m_ErrorContext.m_szAssembly = pAssembly->GetSimpleName();
    
    m_flags = flags;
    
    // Set the callback.
    m_pNotify = pNotify ? pNotify : &g_Notify;

    // If we haven't set 32-bit or 64-bit export yet, set it now with defaults.
    UpdateBitness(pAssembly);

    // Check the bitness of the assembly against our output bitness
    IfFailReport(CheckBitness(pAssembly));
    
    // Get some well known TypeInfos.
    GCX_PREEMP();
    
    BSTR wzPath;// = SysAllocStringLen(NULL, _MAX_PATH);
    IfFailReport(QueryPathOfRegTypeLib(LIBID_STDOLE2, -1, -1, 0, &wzPath));

    if (IsExportingAs64Bit())
    {
        hr = LoadTypeLibEx(wzPath, (REGKIND)(REGKIND_NONE | LOAD_TLB_AS_64BIT), &pITLB);       
    }
    else
    {
        hr = LoadTypeLibEx(wzPath, (REGKIND)(REGKIND_NONE | LOAD_TLB_AS_32BIT), &pITLB);
    }

    // If we failed to load StdOle2.tlb, then we're probably on a downlevel platform (< XP)
    //  so we'll just load whatever we have...note that at this point, cross-compile is not an option.
    if (FAILED(hr))
    {
        IfFailReport(LoadRegTypeLib(LIBID_STDOLE2, -1, -1, 0, &pITLB));
    }
    
    IfFailReport(pITLB->GetTypeInfoOfGuid(IID_IUnknown, &m_pIUnknown));
    IfFailReport(pITLB->GetTypeInfoOfGuid(IID_IDispatch, &m_pIDispatch));
    
    // Look for GUID (which unfortunately has no GUID).
    for (i=0; i<pITLB->GetTypeInfoCount() && !m_pGuid; ++i)
    {
        IfFailReport(pITLB->GetDocumentation(i, &szTIName, 0, 0, 0));
        if (SString::_wcsicmp(szTIName, szGuidName) == 0)
            IfFailReport(pITLB->GetTypeInfo(i, &m_pGuid));
    }

    // Create the output typelib.

    // Win2K: passing in too long a filename triggers a nasty buffer overrun bug
    // when the SaveAll() method is called. We'll avoid triggering this here.
    // 
    if (wcslen(szTlbName) > MAX_PATH_FNAME)
        IfFailReport(HRESULT_FROM_WIN32(ERROR_FILENAME_EXCED_RANGE));

    // Reverting to old behavior here until we can fix up the vtable offsets as well.
    // Set the SYSKIND based on the 64bit/32bit switches  
    if (IsExportingAs64Bit())
    {
        IfFailReport(CreateTypeLib2(SYS_WIN64, szTlbName, &m_pICreateTLB));
    }
    else
    {
        IfFailReport(CreateTypeLib2(SYS_WIN32, szTlbName, &m_pICreateTLB));
    }

    // Set the typelib GUID.
    IfFailReport(GetTypeLibGuidForAssembly(pAssembly, &guid));
    IfFailReport(m_pICreateTLB->SetGuid(guid));

    // Retrieve the type library's version number.
    USHORT usMaj, usMin;
    IfFailReport(GetTypeLibVersionForAssembly(pAssembly, &usMaj, &usMin));

    // Set the TLB's version number.
    IfFailReport(m_pICreateTLB->SetVersion(usMaj, usMin));

    // Set the LCID.  If no locale, set to 0, otherwise typelib defaults to 409.
    lcid = 0;
    LPCUTF8 pLocale = pAssembly->GetLocale();
    if (pLocale && *pLocale)
    {
        // Have to build a BSTR, not just a unicode string (i.e. allocate a
        // DWORD of length information at a negative offset from the string
        // start).        
        _ASSERTE((sizeof(WCHAR) * 2) == sizeof(DWORD));
        hr = qLocale.ReSizeNoThrow(sizeof(DWORD));
        if (SUCCEEDED(hr))
            hr = Utf2Quick(pLocale, qLocale, 2);
        if (SUCCEEDED(hr))
        {
            *(DWORD*)qLocale.Ptr() = (DWORD)wcslen(&qLocale.Ptr()[2]);
            hr = ::CoCreateInstance(CLSID_CMultiLanguage, NULL, CLSCTX_INPROC_SERVER, IID_IMultiLanguage, (void**)&pIML);
        }
        if (SUCCEEDED(hr))
            pIML->GetLcidFromRfc1766(&lcid, (BSTR)&qLocale.Ptr()[2]);
    }
    HRESULT hr2 = m_pICreateTLB->SetLcid(lcid);
    if (hr2 == TYPE_E_UNKNOWNLCID)
    {
        ReportWarning(TYPE_E_UNKNOWNLCID, TYPE_E_UNKNOWNLCID);
        hr2 = m_pICreateTLB->SetLcid(0);
    }
    IfFailReport(hr2);

    // Get the list of types in the assembly.
    AddAssemblyTypes(pAssembly);
    m_Exports.InitArray();

    // Get the assembly value for AutomationProxy.
    m_bAutomationProxy = DEFAULT_AUTOMATION_PROXY_VALUE;
    GetAutomationProxyAttribute(pAssembly->GetManifestImport(), TokenFromRid(1, mdtAssembly), &m_bAutomationProxy);

    // Pre load any caller-specified names into the typelib namespace.
    PreLoadNames();

    // Convert all the types.
    ConvertAllTypeDefs();

    // Set library level properties.
    sName.AppendUTF8(pAssembly->GetSimpleName());
    
    // Make it a legal typelib name.
    SString replaceChar = SL(W("_"));

    SString::Iterator iter = sName.Begin();
    while (sName.Find(iter, W(".")))
        sName.Replace(iter, 1, replaceChar);

    iter = sName.Begin();
    while (sName.Find(iter, W(" ")))
        sName.Replace(iter, 1, replaceChar);
    
    IfFailReport(m_pICreateTLB->SetName((LPOLESTR)sName.GetUnicode()));

    // If the assembly has a description CA, set that as the library Doc string.
    if (GetStringCustomAttribute(pAssembly->GetManifestImport(), XXX_ASSEMBLY_DESCRIPTION_TYPE, TokenFromRid(mdtAssembly, 1), (BSTR &)szDescription))
        m_pICreateTLB->SetDocString((LPWSTR)szDescription);

    // Mark this typelib as exported.
    LPCWSTR pszFullName;
    {
        //@todo:  exceptions?
        StackSString name;
        pAssembly->GetDisplayName(name);
        pszFullName = name.GetUnicode();

        vt.vt = VT_BSTR;
        vt.bstrVal = SysAllocString(pszFullName);
        if (vt.bstrVal == NULL)
            IfFailReport(E_OUTOFMEMORY);
    }
    
    //WszMultiByteToWideChar(CP_ACP,0, (char*)rBuf.Ptr(), (DWORD)rBuf.Size(), vt.bstrVal, (DWORD)rBuf.Size());
    IfFailReport(m_pICreateTLB->SetCustData(GUID_ExportedFromComPlus, &vt));
     
    // Lay out the TypeInfos.
    LayOut();

    if(vt.bstrVal)
    {
        SysFreeString(vt.bstrVal);
        vt.bstrVal = NULL;
    }
    
} // HRESULT TypeLibExporter::Convert()


void TypeLibExporter::UpdateBitness(Assembly* pAssembly)
{
    WRAPPER_NO_CONTRACT;
    
    // If one has already been set, just return.
    if ((TlbExportAs64Bit(m_flags)) || (TlbExportAs32Bit(m_flags)))
        return;

    // If we are exporting a dynamic assembly, just go with the machine type we're running on.
    if (pAssembly->IsDynamic())
    {
#ifdef _WIN64
        m_flags |= TlbExporter_ExportAs64Bit;
#else
        m_flags |= TlbExporter_ExportAs32Bit;
#endif
        return;
    }

    // Get the assembly info
    PEFile* pPEFile = pAssembly->GetDomainAssembly()->GetFile();
    _ASSERTE(pPEFile);

    DWORD PEKind, MachineKind;
    pPEFile->GetPEKindAndMachine(&PEKind, &MachineKind);

    // Based on the assembly flags, determine a bitness to export with.
    // Algorithm base copied from ComputeProcArchFlags() in bcl\system\reflection\assembly.cs
    if ((PEKind & pe32Plus) == pe32Plus)
    {
        switch (MachineKind)
        {
            case IMAGE_FILE_MACHINE_IA64:
            case IMAGE_FILE_MACHINE_AMD64:
                m_flags |= TlbExporter_ExportAs64Bit;
                break;

            case IMAGE_FILE_MACHINE_I386:
                if ((PEKind & peILonly) == peILonly)
                {
#ifdef _WIN64
                    m_flags |= TlbExporter_ExportAs64Bit;
#else
                    m_flags |= TlbExporter_ExportAs32Bit;
#endif
                }
                else
                {
                    _ASSERTE(!"Invalid MachineKind / PEKind pair on the assembly!");
                }
                break;

            default:
                _ASSERTE(!"Unknown MachineKind!");
        }
    }
    else if (MachineKind == IMAGE_FILE_MACHINE_I386)
    {
        if ((PEKind & pe32BitRequired) == pe32BitRequired)
        {
            m_flags |= TlbExporter_ExportAs32Bit;
        }
        else if ((PEKind & peILonly) == peILonly)
        {
#ifdef _WIN64
            m_flags |= TlbExporter_ExportAs64Bit;
#else
            m_flags |= TlbExporter_ExportAs32Bit;
#endif
        }
        else
        {
            m_flags |= TlbExporter_ExportAs32Bit;
        }
    }
    else if (MachineKind == IMAGE_FILE_MACHINE_ARMNT)
    {
        m_flags |= TlbExporter_ExportAs32Bit;
    }
    else
    {
#ifdef _WIN64
        m_flags |= TlbExporter_ExportAs64Bit;
#else
        m_flags |= TlbExporter_ExportAs32Bit;
#endif
    }
}


// Find out if our assembly / bitness combination is valid.
HRESULT TypeLibExporter::CheckBitness(Assembly* pAssembly)
{
    WRAPPER_NO_CONTRACT;

    if (pAssembly->IsDynamic())
        return S_OK;

    PEFile* pPEFile = pAssembly->GetDomainAssembly()->GetFile();
    if (pPEFile == NULL)
        return TLBX_E_BITNESS_MISMATCH;

    DWORD PEKind, MachineKind;
    pPEFile->GetPEKindAndMachine(&PEKind, &MachineKind);

    // Neutral assembly?
    if ((PEKind & peILonly) == peILonly)
        return S_OK;

    if (IsExportingAs64Bit())
    {
        if ((MachineKind == IMAGE_FILE_MACHINE_IA64) || (MachineKind == IMAGE_FILE_MACHINE_AMD64))
            return S_OK;
    }
    else
    {
        if ((MachineKind == IMAGE_FILE_MACHINE_I386) || (MachineKind == IMAGE_FILE_MACHINE_ARMNT))
            return S_OK;
    }

    return TLBX_E_BITNESS_MISMATCH;
}


//*****************************************************************************
//*****************************************************************************
void TypeLibExporter::PreLoadNames()
{
    STANDARD_VM_CONTRACT;

    SafeComHolder<ITypeLibExporterNameProvider>  pINames = 0;
    HRESULT     hr = S_OK;              // A result.
    SafeArrayHolder pNames = 0;         // Names provided by caller.
    VARTYPE     vt;                     // Type of data.
    int        lBound, uBound, ix;     // Loop control.
    BSTR        name;

    // Look for names provider, but don't require it.
    hr = SafeQueryInterface(m_pNotify, IID_ITypeLibExporterNameProvider, (IUnknown**)&pINames);
    if (FAILED(hr))
        return;

    // There is a provider, so get the list of names.
    IfFailReport(pINames->GetNames(&pNames));

    // Better have a single dimension array of strings.
    if (pNames == 0)
        IfFailReport(TLBX_E_BAD_NAMES);
    
    if (SafeArrayGetDim(pNames) != 1)
        IfFailReport(TLBX_E_BAD_NAMES);
    
    IfFailReport(SafeArrayGetVartype(pNames, &vt));
    if (vt != VT_BSTR)
        IfFailReport(TLBX_E_BAD_NAMES);

    // Get names bounds.
    IfFailReport(SafeArrayGetLBound(pNames, 1, (LONG*)&lBound));
    IfFailReport(SafeArrayGetUBound(pNames, 1, (LONG*)&uBound));

    // Enumerate the names.
    for (ix=lBound; ix<=uBound; ++ix)
    {
        IfFailReport(SafeArrayGetElement(pNames, (LONG*)&ix, (void*)&name));
        m_pICreateTLB->SetName(name);
    }
}

//*****************************************************************************
//*****************************************************************************
void TypeLibExporter::FormatErrorContextString(
    CErrorContext *pContext,            // The context to format.
    SString       *pOut)                // Buffer to format into.
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pContext));
        PRECONDITION(CheckPointer(pOut));
    }
    CONTRACTL_END;
    
    HRESULT  hr;
    SString *pBuf;
    SString  ssInternal;
    
    // Nested contexts?
    if (pContext->m_prev == 0)
    {   // No, just convert into caller's buffer.
        pBuf = pOut;
    }
    else
    {   // Yes, convert locally, then concatenate.
        pBuf = &ssInternal;
    }
    
    // More?
    if (pContext->m_pScope)
    {   
        // Check whether type is nested (which requires more formatting).
        DWORD dwFlags;
        IfFailReport(pContext->m_pScope->GetTypeDefProps(pContext->m_tkType, &dwFlags, 0));
        
        if (IsTdNested(dwFlags))
        {
            TypeNameBuilder tnb(pBuf, TypeNameBuilder::ParseStateNAME);
            TypeString::AppendNestedTypeDef(tnb, pContext->m_pScope, pContext->m_tkType);
        }
        else
            TypeString::AppendTypeDef(*pBuf, pContext->m_pScope, pContext->m_tkType);

        // Member?
        if (pContext->m_szMember)
        {
            pBuf->Append(NAMESPACE_SEPARATOR_WSTR);
            
            pBuf->AppendUTF8(pContext->m_szMember);

            // Param?
            if (pContext->m_szParam)
            {
                pBuf->Append(W("("));
                pBuf->AppendUTF8(pContext->m_szParam);
                pBuf->Append(W(")"));
            }
            else if (pContext->m_ixParam > -1)
            {
                pBuf->AppendPrintf(W("(#%d)"), pContext->m_ixParam);
            }
        } // member

        pBuf->Append(ASSEMBLY_SEPARATOR_WSTR);
    } // Type name
    
    pBuf->AppendUTF8(pContext->m_szAssembly);
    
    // If there is a nested context, put it all together.
    if (pContext->m_prev)
    {
        // Format the context this one was nested inside.
        SString ssOuter;
        FormatErrorContextString(pContext->m_prev, &ssOuter);

        // Put them together with text.
        LPWSTR pUnicodeBuffer = pOut->OpenUnicodeBuffer(1024);
        FormatRuntimeError(pUnicodeBuffer, 1024, TLBX_E_CTX_NESTED, pBuf->GetUnicode(), ssOuter.GetUnicode());
        pOut->CloseBuffer((COUNT_T)wcslen(pUnicodeBuffer));
    }
} // HRESULT TypeLibExporter::FormatErrorContextString()

//*****************************************************************************
//*****************************************************************************
void TypeLibExporter::FormatErrorContextString(
    SString    *pBuf)                   // Buffer to format into.
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pBuf));
    }
    CONTRACTL_END;

    FormatErrorContextString(&m_ErrorContext, pBuf);
} // HRESULT TypeLibExporter::FormatErrorContextString()

//*****************************************************************************
// Event reporting helper.
//*****************************************************************************
void TypeLibExporter::ReportError(HRESULT hrRpt)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    WCHAR      rcErr[1024];
    SString    ssName;
    SafeComHolder<IErrorInfo> pErrorInfo;
    BSTRHolder bstrDescription = NULL;

    // Format the error message.
    if (SafeGetErrorInfo(&pErrorInfo) != S_OK)
        pErrorInfo = NULL;

    // If we retrieved and IErrorInfo then retrieve the description.
    if (pErrorInfo)
    {
        if (FAILED(pErrorInfo->GetDescription(&bstrDescription)))
            bstrDescription = NULL;
    }

    if (bstrDescription)
    {
        // Use the description as the error message.
        wcsncpy_s(rcErr, COUNTOF(rcErr), bstrDescription, _TRUNCATE);
    }
    else
    {
        // Format the error message.
        FormatRuntimeError(rcErr, lengthof(rcErr), hrRpt);
    }

    // Format the context.
    FormatErrorContextString(&ssName);

    // Post the error to the errorinfo object.
    VMPostError(TLBX_E_ERROR_MESSAGE, ssName.GetUnicode(), rcErr);

    // Throw the exception, including context info.
    COMPlusThrowHR(TLBX_E_ERROR_MESSAGE, kGetErrorInfo);
} // void TypeLibExporter::ReportError()

//*****************************************************************************
// Event reporting helper.
//*****************************************************************************
void TypeLibExporter::ReportEvent(   // Returns the original HR.
    int         ev,                  // The event kind.
    int         hr,                  // HR.
    ...)                             // Variable args.
{
    STANDARD_VM_CONTRACT;

    WCHAR       rcMsg[1024];            // Buffer for message.
    va_list     marker;                 // User text.
    BSTRHolder  bstrMsg=0;              // BSTR for message.
    
    // Format the message.
    va_start(marker, hr);
    hr = FormatRuntimeErrorVa(rcMsg, lengthof(rcMsg), hr, marker);
    va_end(marker);
    
    // Convert to a BSTR.
    bstrMsg = SysAllocString(rcMsg);
    
    // Display it, and clean up.
    if (bstrMsg != NULL)
        m_pNotify->ReportEvent(static_cast<ImporterEventKind>(ev), hr, bstrMsg);

} // HRESULT CImportTlb::ReportEvent()

//*****************************************************************************
// Warning reporting helper.
//*****************************************************************************
void TypeLibExporter::ReportWarning( // Original error code.
    HRESULT hrReturn,                   // HR to return.
    HRESULT hrRpt,                      // Error code.
    ...)                                // Args to message.
{
    STANDARD_VM_CONTRACT;

    WCHAR       rcErr[1024];            // Buffer for error message.
    SString     ssName;                 // Buffer for context.
    va_list     marker;                 // User text.
    BSTRHolder  bstrMsg=0;              // BSTR for message.
    BSTRHolder  bstrBuf=0;              // Buffer for message.
    UINT        iLen;                   // Length of allocated buffer.
    
    // Format the message.
    va_start(marker, hrRpt);
    FormatRuntimeErrorVa(rcErr, lengthof(rcErr), hrRpt, marker);
    va_end(marker);
    
    // Format the context.
    FormatErrorContextString(&ssName);
                        
    // Put them together.
    iLen = (UINT)(wcslen(rcErr) + ssName.GetCount() + 200);
    bstrBuf = SysAllocStringLen(0, iLen);
    
    if (bstrBuf != NULL)
    {
        FormatRuntimeError(bstrBuf, iLen, TLBX_W_WARNING_MESSAGE, ssName.GetUnicode(), rcErr);
        
        // Have to copy to another BSTR, because the runtime will also print the trash after the 
        //  terminating nul.
        bstrMsg = SysAllocString(bstrBuf);
        
        if (bstrMsg != NULL)
            m_pNotify->ReportEvent(NOTIF_CONVERTWARNING, hrRpt, bstrMsg);
    }

} // void TypeLibExporter::ReportWarning()
    
// Throws exceptions encountered during type exportation.
// Wrapped with ThrowHRWithContext.
void TypeLibExporter::InternalThrowHRWithContext(HRESULT hrRpt, ...)
{
    STANDARD_VM_CONTRACT;

    WCHAR   rcErr[2048];
    SString ssName;
    va_list marker;  

    // Format the error message.
    va_start(marker, hrRpt);
    FormatRuntimeErrorVa(rcErr, lengthof(rcErr), hrRpt, marker);
    va_end(marker);

    // Format the context.
    FormatErrorContextString(&ssName);

    // Post the error to the errorinfo object.
    VMPostError(TLBX_E_ERROR_MESSAGE, ssName.GetUnicode(), rcErr);

    // Throw the exception, including context info.
    COMPlusThrowHR(TLBX_E_ERROR_MESSAGE, kGetErrorInfo);
} // void TypeLibExporter::InternalThrowHRWithContext()

//*****************************************************************************
// Post a class load error on failure.
//*****************************************************************************
void TypeLibExporter::PostClassLoadError(
    LPCUTF8     pszName,                // Name of the class.
    SString&    message)                // Exception message of class load failure.
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(pszName));
    }
    CONTRACTL_END;

    // See if we got anything back.
    if (!message.IsEmpty())
        InternalThrowHRWithContext(TLBX_E_CLASS_LOAD_EXCEPTION, pszName, message.GetUnicode());
    else
        InternalThrowHRWithContext(TLBX_E_CANT_LOAD_CLASS, pszName);
} // HRESULT TypeLibExporter::PostClassLoadError()

//*****************************************************************************
// Determine the type, if any, of auto-interface for a class.
//  May be none, dispatch, or dual.
//*****************************************************************************
void TypeLibExporter::ClassHasIClassX(  
    MethodTable           *pClass,                // The class.
    CorClassIfaceAttr *pClassItfType)         // None, dual, dispatch
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(pClass));
        PRECONDITION(!pClass->IsInterface());
        PRECONDITION(CheckPointer(pClassItfType));
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;
    ComMethodTable *pClassComMT = NULL;

    *pClassItfType = clsIfNone;

    // If the class is a COM import or if it isn't COM visible, then from the 
    // exporter's perspective, it doens't have an IClassX.
    if (!pClass->IsComImport())
    {
        ComCallWrapperTemplate *pTemplate = ComCallWrapperTemplate::GetTemplate(pClass);
        if (pTemplate->SupportsIClassX())
        {
            pClassComMT = ComCallWrapperTemplate::SetupComMethodTableForClass(pClass, FALSE);
            _ASSERTE(pClassComMT);

            if (pClassComMT->IsComVisible())
                *pClassItfType = pClassComMT->GetClassInterfaceType();
        }
    }
} // HRESULT TypeLibExporter::ClassHasIClassX()

//*****************************************************************************
// Load a class by token, post an error on failure.
//*****************************************************************************
MethodTable * TypeLibExporter::LoadClass(
    Module      *pModule,               // Module with Loader to use to load the class.
    mdToken     tk)                     // The token to load.
{
    CONTRACT(MethodTable *)
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(pModule));
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END;

    // Get the MethodTable for the token.        
    TypeHandle th;
    SString exceptionMessage;

    EX_TRY
    {            
        th = ClassLoader::LoadTypeDefOrRefThrowing(pModule, tk,
                                                  ClassLoader::ThrowIfNotFound, 
                                                  ClassLoader::PermitUninstDefOrRef);
    }
    EX_CATCH
    {
        GET_EXCEPTION()->GetMessage(exceptionMessage);
    }
    EX_END_CATCH(SwallowAllExceptions);

    if (th.IsNull())
    {
        // Format a hopefully useful error message.
        LPCUTF8 pNS, pName;
        SString sName;
            
        if (TypeFromToken(tk) == mdtTypeDef)
        {
            if (FAILED(pModule->GetMDImport()->GetNameOfTypeDef(tk, &pName, &pNS)))
            {
                pName = pNS = "Invalid TypeDef record";
            }
        }
        else
        {
            _ASSERTE(TypeFromToken(tk) == mdtTypeRef);
            if (FAILED(pModule->GetMDImport()->GetNameOfTypeRef(tk, &pNS, &pName)))
            {
                pNS = pName = "Invalid TypeRef record";
            }
        }

        if (pNS && *pNS)
        {
            sName.AppendUTF8(pNS);
            sName.AppendUTF8(NAMESPACE_SEPARATOR_STR);
        }
            
        sName.AppendUTF8(pName);
            
        StackScratchBuffer scratch;
        PostClassLoadError(sName.GetUTF8(scratch), exceptionMessage);
    }

    RETURN (th.AsMethodTable());
    
} // void TypeLibExporter::LoadClass()

//*****************************************************************************
// Load a class by name, post an error on failure.
//*****************************************************************************
TypeHandle TypeLibExporter::LoadClass(
    Module      *pModule,               // Module with Loader to use to load the class.
    LPCUTF8     pszName)                // Name of class to load.
{
    CONTRACT(TypeHandle)
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(pModule));
        PRECONDITION(CheckPointer(pszName));
        POSTCONDITION(!RETVAL.IsNull());
    }
    CONTRACT_END;

    TypeHandle th;
    SString exceptionMessage;

    EX_TRY
    {            
        th = TypeName::GetTypeUsingCASearchRules(pszName, pModule->GetAssembly());
        _ASSERTE(!th.IsNull());
    }
    EX_CATCH
    {
        GET_EXCEPTION()->GetMessage(exceptionMessage);
    }
    EX_END_CATCH(SwallowAllExceptions);

    if (th.IsNull())
    {
        PostClassLoadError(pszName, exceptionMessage);
    }

    RETURN th;

} // void TypeLibExporter::LoadClass()


//*****************************************************************************
// Enumerate the TypeDefs and convert them to TypeInfos.
//*****************************************************************************
void TypeLibExporter::ConvertAllTypeDefs()
{
    STANDARD_VM_CONTRACT;

    HRESULT     hr = S_OK;              // A result.
    CExportedTypesInfo *pData;          // For iterating the entries.
    int         cTypes;                 // Count of types.
    int         ix;                     // Loop control.
    
    LPCSTR pName1, pNS1;                // Names of a type.
    LPCSTR pName2, pNS2;                // Names of another type.
    MethodTable     *pc1;                   // A Type.
    MethodTable     *pc2;                   // Another type.
    CQuickArray<BYTE> bNamespace;       // Array of flags for namespace decoration.
        
    cTypes = m_Exports.Count();

    // If there are no types in the assembly, then we are done.
    if (cTypes <= 0)
        return;
    
    // Order by name, then look for duplicates.
    m_Exports.SortByName();                    
    
    // Resize the array for namespace flags now, but use the ICreateTypeInfo*, so that
    //  the flags will be sorted.
    bNamespace.ReSizeThrows(cTypes);
    
    // Get names of first type.
    pc1 = m_Exports[0]->pClass;
    IfFailReport(pc1->GetMDImport()->GetNameOfTypeDef(pc1->GetCl(), &pName1, &pNS1));
    
    // Iterate through the types, looking for duplicate type names.
    for (ix=0; ix<cTypes-1; ++ix)
    {
        // Get the Type pointers and the types' names.
        pc2 = m_Exports[ix+1]->pClass;
        IfFailReport(pc2->GetMDImport()->GetNameOfTypeDef(pc2->GetCl(), &pName2, &pNS2));
        
        // If the types match (case insensitive). mark both types for namespace
        //  decoration.  
        if (stricmpUTF8(pName1, pName2) == 0)
        {
            m_Exports[ix]->pCTI = reinterpret_cast<ICreateTypeInfo2*>(1);
            m_Exports[ix+1]->pCTI = reinterpret_cast<ICreateTypeInfo2*>(1);
        }
        else
        {   // Didn't match, so advance "class 1" pointer.
            pc1 = pc2;
            pName1 = pName2;
            pNS1 = pNS2;
        }
    }
    
    // Put into token order for actual creation.
    m_Exports.SortByToken();
    
    // Fill the flag array, from the ICreateTypeInfo* pointers.
    memset(bNamespace.Ptr(), 0, bNamespace.Size()*sizeof(BYTE));
    for (ix=0; ix<cTypes; ++ix)
    {
        if (m_Exports[ix]->pCTI)
            bNamespace[ix] = 1, m_Exports[ix]->pCTI = 0;
    }
    
    // Pass 1.  Create the TypeInfos.
    // There are four steps in the process:
    //  a) Creates the TypeInfos for the types themselves.  When a duplicate
    //     is encountered, skip the type until later, so that we don't create
    //     a decorated name that will conflict with a subsequent non-decorated
    //     name.  We want to preserve a type's given name as much as possible.
    //  b) Create the TypeInfos for the types that were duplicates in step a.
    //     Perform decoration of the names as necessary to eliminate duplicates.
    //  c) Create the TypeInfos for the IClassXs.  When there is a duplicate,
    //     skip, as in step a.
    //  d) Create the remaining TypeInfos for IClassXs.  Perform decoration of 
    //     the names as necessary to eliminate duplicates.
    
    // Step a, Create the TypeInfos for the TypeDefs, no decoration.
    for (ix=0; ix<cTypes; ++ix)
    {
        int     bAutoProxy = m_bAutomationProxy;
        pData = m_Exports[ix];
        pData->tkind = TKindFromClass(pData->pClass);
        GetAutomationProxyAttribute(pData->pClass->GetMDImport(), pData->pClass->GetCl(), &bAutoProxy);
        pData->bAutoProxy = (bAutoProxy != 0);
        
        CreateITypeInfo(pData, (bNamespace[ix]!=0), false);
    }
    // Step b, Create the TypeInfos for the TypeDefs, decoration as needed.
    for (ix=0; ix<cTypes; ++ix)
    {
        pData = m_Exports[ix];
        if (pData->pCTI == 0)
            CreateITypeInfo(pData, (bNamespace[ix]!=0), true);
    }
    
    // Step c, Create the TypeInfos for the IClassX interfaces.  No decoration.
    for (ix=0; ix<cTypes; ++ix)
    {
        pData = m_Exports[ix];
        CreateIClassXITypeInfo(pData, (bNamespace[ix]!=0), false);
    }
    // Step d, Create the TypeInfos for the IClassX interfaces.  Decoration as required.
    for (ix=0; ix<cTypes; ++ix)
    {
        pData = m_Exports[ix];
        if (pData->pCTIClassItf == 0)
            CreateIClassXITypeInfo(pData, (bNamespace[ix]!=0), true);
    }
    
    // Pass 2, add the ImplTypes to the CoClasses.
    for (ix=0; ix<cTypes; ++ix)
    {
        pData = m_Exports[ix];
        ConvertImplTypes(pData);
    }
    
    // Pass 3, fill in the TypeInfo details...
    for (ix=0; ix<cTypes; ++ix)
    {
        pData = m_Exports[ix];
        ConvertDetails(pData);
    }
    
    hr = S_OK;
} // void TypeLibExporter::ConvertAllTypeDefs()

//*****************************************************************************
// Convert one TypeDef.  Useful for one-off TypeDefs in other scopes where 
//  that other scope's typelib doesn't contain a TypeInfo.  This happens
//  for the event information with imported typelibs.
//*****************************************************************************
HRESULT TypeLibExporter::ConvertOneTypeDef(
    MethodTable     *pClass)                // The one class to convert.
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(pClass));
    }
    CONTRACTL_END;

    HRESULT     hr = S_OK;              // A result.
    ICreateTypeInfo2 *pCTI=0;           // The TypeInfo to create.
    ICreateTypeInfo2 *pDefault=0;       // A possible IClassX TypeInfo.
    CErrorContext SavedContext;         // Previous error context.
    CExportedTypesInfo *pExported;      // For adding classes to the exported types cache.
    CExportedTypesInfo sExported;       // For adding classes to the exported types cache.

    // Save error reporting context.
    SavedContext = m_ErrorContext;
    m_ErrorContext.m_szAssembly  = pClass->GetAssembly()->GetSimpleName();
    m_ErrorContext.m_tkType      = mdTypeDefNil;
    m_ErrorContext.m_pScope      = 0;
    m_ErrorContext.m_szMember    = 0;
    m_ErrorContext.m_szParam     = 0;
    m_ErrorContext.m_ixParam     = -1;
    m_ErrorContext.m_prev = &SavedContext;
    
    // See if this class is already in the list.
    sExported.pClass = pClass;
    pExported = m_InjectedExports.Find(&sExported);
    if (pExported == 0)
    {
        // Get the AutoProxy value for an isolated class.
        int     bAutoProxy = DEFAULT_AUTOMATION_PROXY_VALUE;
        if (FALSE == GetAutomationProxyAttribute(pClass->GetMDImport(), pClass->GetCl(), &bAutoProxy))
            GetAutomationProxyAttribute(pClass->GetAssembly()->GetManifestImport(), TokenFromRid(1, mdtAssembly), &bAutoProxy);

        // New class, add to list.
        if (NULL == (pExported = m_InjectedExports.Add(&sExported)))
            IfFailReport(E_OUTOFMEMORY);
        m_InjectedExports.UpdateArray();
        
        // Prefix can't tell that IfFailReport will actually throw an exception if pExported is NULL so
        // let's tell it explicitly that if we reach this point pExported will not be NULL.
        PREFIX_ASSUME(pExported != NULL);        
        pExported->pClass = pClass;
        pExported->pCTI = 0;
        pExported->pCTIClassItf = 0;
        pExported->tkind = TKindFromClass(pClass);
        pExported->bAutoProxy = (bAutoProxy != 0);

        // Step 1, Create the TypeInfos for the TypeDefs.
        CreateITypeInfo(pExported);
    
        // Step 1a, Create the TypeInfos for the IClassX interfaces.
        CreateIClassXITypeInfo(pExported);
    
        // Step 2, add the ImplTypes to the CoClasses.
        ConvertImplTypes(pExported);
    
        // Step 3, fill in the TypeInfo details...
        ConvertDetails(pExported);
    }
    
    // Restore error reporting context.
    m_ErrorContext = SavedContext;
    
    return (hr);
} // HRESULT TypeLibExporter::ConvertOneTypeDef()


//*****************************************************************************
// Create the ITypeInfo for a type.  Well, sort of.  This function will create
//  the first of possibly two typeinfos for the type.  If the type is a class
//  we will create a COCLASS typeinfo now, and an INTERFACE typeinfo later,
//  which typeinfo will be the default interface for the coclass.  If this
//  typeinfo needs to be aliased, we will create the ALIAS now (with the 
//  real name) and the aliased typeinfo later, with the real attributes, but
//  with a mangled name. 
//*****************************************************************************
void TypeLibExporter::CreateITypeInfo(
    CExportedTypesInfo *pData,          // Conversion data.
    bool        bNamespace,             // If true, use namespace + name
    bool        bResolveDup)            // If true, decorate name to resolve dups.
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(pData));
    }
    CONTRACTL_END;

    HRESULT     hr = S_OK;              // A result.
    LPCUTF8     pName;                  // Name in UTF8.
    LPCUTF8     pNS;                    // Namespace in UTF8.
    SString     sName;                  // Name of the TypeDef.
    TYPEKIND    tkind;                  // The TYPEKIND of a TypeDef.
    GUID        clsid;                  // A TypeDef's clsid.
    DWORD       dwFlags;                // A TypeDef's flags.
    int         iSuffix = 0;            // Counter for suffix.
    mdTypeDef   td;                     // Token for the class.

    VariantHolder vt;                                 // For defining custom attribute.   
    SafeComHolder<ICreateTypeInfo> pCTITemp=0;        // For creating a typeinfo.
    SafeComHolder<ICreateTypeInfo2> pCTI2=0;          // For creating the typeinfo.
    SafeComHolder<ITypeLib> pITLB=0;                  // For dup IID reporting.   
    SafeComHolder<ITypeInfo> pITIDup=0;               // For dup IID reporting.
    BSTRHolder  bstrDup=0;                            // For dup IID reporting.
    BSTRHolder  bstrDescr=0;                          // For description.
    ZeroHolder  zhType = &m_ErrorContext.m_pScope;    // Clear error reporting info.
    CorClassIfaceAttr classItfType = clsIfNone;       // For class interface type.
    TypeHandle  thClass = TypeHandle(pData->pClass);  // TypeHandle representing the class.

    DefineFullyQualifiedNameForClassW();

    // Get the TypeDef and some info about it.
    td = pData->pClass->GetCl();
    IfFailReport(pData->pClass->GetMDImport()->GetTypeDefProps(td, &dwFlags, 0));
    tkind = pData->tkind;
    
    // Error reporting info.
    m_ErrorContext.m_tkType = td;
    m_ErrorContext.m_pScope = pData->pClass->GetMDImport();
    
    pData->pCTI = 0;
    pData->pCTIClassItf = 0;

    // If it is ComImport or WindowsRuntimeImport, do not export it.
    if (IsTdImport(dwFlags) || pData->pClass->IsProjectedFromWinRT())
        return;

    // Check to see if the type is supposed to be visible from COM. If it
    // is not then we go to the next type.
    if (!IsTypeVisibleFromCom(TypeHandle(pData->pClass)))
        return;

    // Get the GUID for the class.  Will generate from name if no defined GUID,
    //  will also use signatures if interface.
    pData->pClass->GetGuid(&clsid, TRUE);

    // Get the name.
    IfFailReport(pData->pClass->GetMDImport()->GetNameOfTypeDef(td, &pName, &pNS));
    
    // Warn about exporting AutoLayout valueclasses
    if ( (pData->pClass->IsValueType()) && (!pData->pClass->IsEnum()) && (IsTdAutoLayout(pData->pClass->GetAttrClass())))
        ReportWarning(TLBX_W_EXPORTING_AUTO_LAYOUT, TLBX_W_EXPORTING_AUTO_LAYOUT, pName);

    // Warn about exporting generic classes.
    if (pData->pClass->GetNumGenericArgs() != 0)
        ReportWarning(TLBX_I_GENERIC_TYPE, TLBX_I_GENERIC_TYPE);

    // Classes that derive from generic classes can be COM visible, however we don't
    // expose a class interface for them. Give a warning to the user about this.
    if (pData->pClass->HasGenericClassInstantiationInHierarchy())
    {
        if (!pData->pClass->IsComImport() && IsTypeVisibleFromCom(thClass))
        {
            // Note that we can't call ClassHasIClassX here since it would return
            // classIfNone if the type has generic parents in it's hierarchy.
            if (ReadClassInterfaceTypeCustomAttribute(thClass) != clsIfNone)
                ReportWarning(TLBX_I_GENERIC_BASE_TYPE, TLBX_I_GENERIC_BASE_TYPE);        
        }
    }
        
    // Warn about exporting reference types as structs.
    if ((pData->tkind == TKIND_RECORD || pData->tkind == TKIND_UNION) && !pData->pClass->IsValueType())
        ReportWarning(TLBX_I_REF_TYPE_AS_STRUCT, TLBX_I_REF_TYPE_AS_STRUCT);

    // workaround for microsoft.wfc.interop.dll -- skip their IDispatch.
    if (clsid == IID_IDispatch || clsid == IID_IUnknown)
    {
        ReportEvent(NOTIF_CONVERTWARNING, TLBX_S_NOSTDINTERFACE, pName);
        return;
    }

    if (bNamespace)
    {
        sName.MakeFullNamespacePath(SString(SString::Utf8, pNS), SString(SString::Utf8, pName));

        SString replaceChar = SL(W("_"));

        SString::Iterator iter = sName.Begin();
        while (sName.Find(iter, W(".")))
            sName.Replace(iter, 1, replaceChar);
    }
    else
    {   // Convert name to wide chars.
        sName.AppendUTF8(pName);
    }

    // Create the typeinfo for this typedef.
    for (;;)
    {
        // Attempt to create the TypeDef.
        hr = m_pICreateTLB->CreateTypeInfo((LPOLESTR)sName.GetUnicode(), tkind, &pCTITemp);
        
        // If a name conflict, decorate, otherwise, done.
        if (hr != TYPE_E_NAMECONFLICT)
            break;
            
        if (!bResolveDup)
        {
            hr = S_FALSE;
            return;
        }

        if (iSuffix == 0)
        {           
            iSuffix = 2;
        }
        else
        {
            sName.Delete(sName.End()-=2, 2);            
        }

        SString sDup;
        sDup.Printf(szDuplicateDecoration, iSuffix++);

        sName.Append(sDup);
    }
    
    IfFailReport(hr);
    IfFailReport(SafeQueryInterface(pCTITemp, IID_ICreateTypeInfo2, (IUnknown**)&pCTI2));
    
    // Set the guid.
    _ASSERTE(clsid != GUID_NULL);
    hr = pCTI2->SetGuid(clsid);
    if (FAILED(hr))
    {
        if (hr == TYPE_E_DUPLICATEID)
        {
            IfFailReport(SafeQueryInterface(m_pICreateTLB, IID_ITypeLib, (IUnknown**)&pITLB));
            IfFailReport(pITLB->GetTypeInfoOfGuid(clsid, &pITIDup));
            IfFailReport(pITIDup->GetDocumentation(MEMBERID_NIL, &bstrDup, 0,0,0));
            InternalThrowHRWithContext(TLBX_E_DUPLICATE_IID, sName.GetUnicode(), (BSTR)bstrDup);
        }
        return;
    }
    TRACE("TypeInfo %x: %ls, {%08x-%04x-%04x-%04x-%02x%02x%02x%02x}\n", pCTI2, sName, 
        clsid.Data1, clsid.Data2, clsid.Data3, clsid.Data4[0]<<8|clsid.Data4[1], clsid.Data4[2], clsid.Data4[3], clsid.Data4[4], clsid.Data4[5]); 

    IfFailReport(pCTI2->SetVersion(1, 0));

    // Record the fully qualified type name in a custom attribute.
    // If the TypelibImportClassAttribute exists, use that instead.
    SString sName2;
    hr = GetTypeLibImportClassName(pData->pClass, sName2);
    if (hr == S_OK)
    {      
        V_BSTR(&vt) = ::SysAllocString(sName2.GetUnicode());
        if (V_BSTR(&vt) == NULL)
            IfFailReport(E_OUTOFMEMORY);

        V_VT(&vt) = VT_BSTR;
    }
    else
    {
        // Default to the real name.
        LPCWSTR pszName = GetFullyQualifiedNameForClassNestedAwareW(pData->pClass);

        V_BSTR(&vt) = ::SysAllocString(pszName);
        if (V_BSTR(&vt) == NULL)
            IfFailReport(E_OUTOFMEMORY);

        V_VT(&vt) = VT_BSTR;
    }
    
    IfFailReport(pCTI2->SetCustData(GUID_ManagedName, &vt));

    // If the class is decorated with a description, apply it to the typelib.
    if (GetDescriptionString(pData->pClass, td, (BSTR &)bstrDescr))
        IfFailReport(pCTI2->SetDocString(bstrDescr));
    
    // Transfer ownership of the pointer.
    pData->pCTI = pCTI2;
    pCTI2.SuppressRelease();
    pCTI2 = 0;
} // void TypeLibExporter::CreateITypeInfo()

HRESULT TypeLibExporter::GetTypeLibImportClassName(
    MethodTable*pClass,
    SString&    szName)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;
    _ASSERTE(NULL != pClass);

    HRESULT hr = S_OK;

    // Check for the presence of the TypelibImportClassAttribute.
    const char*       pvData;           // Pointer to a custom attribute data.
    ULONG             cbData;           // Size of custom attribute data.

    hr = pClass->GetMDImport()->GetCustomAttributeByName(pClass->GetCl(),
                                                         INTEROP_TYPELIBIMPORTCLASS_TYPE,
                                                         reinterpret_cast<const void**>(&pvData),
                                                         &cbData);

    if (hr == S_OK && cbData > 5 && pvData[0] == 1 && pvData[1] == 0)
    {       
        CustomAttributeParser cap(pvData, cbData);
        VERIFY(SUCCEEDED(cap.ValidateProlog())); // Validated above, just ensure consistency.

        LPCUTF8 szString;
        ULONG   cbString;
        if (SUCCEEDED(cap.GetNonNullString(&szString, &cbString)))
        {
            // Set the string and null terminate it.
            szName.SetUTF8(szString, cbString);
            szName.AppendASCII("\0");

            // We successfully retrieved the string.
            return S_OK;
        }
    }

    return S_FALSE;
}



//*****************************************************************************
// See if an object has a Description, and get it as a BSTR.
//*****************************************************************************
BOOL TypeLibExporter::GetDescriptionString(
    MethodTable     *pClass,                // Class containing the token.
    mdToken     tk,                     // Token of the object.
    BSTR        &bstrDescr)             // Put description here.
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(pClass));
    }
    CONTRACTL_END;

    // Check for a description custom attribute.
    return GetStringCustomAttribute(pClass->GetMDImport(), XXX_DESCRIPTION_TYPE, tk, bstrDescr);

} // HRESULT TypeLibExporter::GetDescriptionString()

//*****************************************************************************
// See if an object has a custom attribute, and get it as a BSTR.
//*****************************************************************************
BOOL TypeLibExporter::GetStringCustomAttribute(
    IMDInternalImport *pImport, 
    LPCSTR     szName, 
    mdToken     tk, 
    BSTR        &bstrDescr)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(pImport));
        PRECONDITION(CheckPointer(szName));
    }
    CONTRACTL_END;

    HRESULT     hr;                     // A result.
    const void  *pvData;                // Pointer to a custom attribute data.
    ULONG       cbData;                 // Size of custom attribute data.
    
    // Look for the desired custom attribute.
    IfFailReport(pImport->GetCustomAttributeByName(tk, szName,  &pvData,&cbData));
    if (hr == S_OK && cbData > 2)
    {
        CustomAttributeParser cap(pvData, cbData);
        IfFailReport(cap.SkipProlog());

        LPCUTF8 szString;
        ULONG   cbString;
        IfFailReport(cap.GetString(&szString, &cbString));

        bstrDescr = SysAllocStringLen(0, cbString); // allocates cbString+1 characters (appends '\0')
        if (bstrDescr == NULL)
            IfFailReport(E_OUTOFMEMORY);

        if (cbString > 0)
        {
            ULONG cch = WszMultiByteToWideChar(CP_UTF8, 0, szString, cbString, bstrDescr, cbString);
            bstrDescr[cch] = W('\0');
        }

        return TRUE;
    }
    
    return FALSE;
} // HRESULT GetStringCustomAttribute()

//*****************************************************************************
// Get the value for AutomationProxy for an object.  Return the default
//  if there is no attribute.
//*****************************************************************************
BOOL TypeLibExporter::GetAutomationProxyAttribute(
    IMDInternalImport *pImport, 
    mdToken     tk, 
    int         *bValue)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(pImport));
        PRECONDITION(CheckPointer(bValue));
    }
    CONTRACTL_END;

    HRESULT     hr;                     // A result.
    const void  *pvData;                // Pointer to a custom attribute data.
    ULONG       cbData;                 // Size of custom attribute data.

    IfFailReport(pImport->GetCustomAttributeByName(tk, INTEROP_AUTOPROXY_TYPE, &pvData, &cbData));
    if (hr == S_OK && cbData > 2)
    {
        CustomAttributeParser cap(pvData, cbData);
        if (FAILED(cap.SkipProlog()))
            return FALSE;

        UINT8 u1;
        if (FAILED(cap.GetU1(&u1)))
            return FALSE;

        *bValue = u1 != 0;
    }

    if (hr == S_OK)
        return TRUE;

    return FALSE;
} // void TypeLibExporter::GetAutomationProxyAttribute()

//*****************************************************************************
// Create the IClassX ITypeInfo.
//*****************************************************************************
void TypeLibExporter::CreateIClassXITypeInfo(
    CExportedTypesInfo *pData,          // Conversion data.
    bool        bNamespace,             // If true, use namespace + name
    bool        bResolveDup)            // If true, decorate name to resolve dups.
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(pData));
    }
    CONTRACTL_END;
    
    HRESULT     hr = S_OK;              // A result.
    LPCUTF8     pName;                  // Name in UTF8.
    LPCUTF8     pNS;                    // Namespace in UTF8.
    SString     sName;                  // Name of the TypeDef.
    SString     sNameTypeInfo;          // Name of the IClassX.
    TYPEKIND    tkind;                  // The TYPEKIND of a TypeDef.
    GUID        clsid;                  // A TypeDef's clsid.
    DWORD       dwFlags;                // A TypeDef's flags.
    LPWSTR      pSuffix;                // Pointer into the name.
    int         iSuffix = 0;            // Counter for suffix.
    GUID        guid = {0};             // A default interface's IID.
    HREFTYPE    href;                   // href of base interface of IClassX.
    mdTypeDef   td;                     // Token for the class.
    CorClassIfaceAttr classItfType = clsIfNone; // For class interface type.
    
    VariantHolder vt;                           // For defining custom attribute.
    SafeComHolder<ICreateTypeInfo> pCTITemp=0;  // For creating a typeinfo.
    SafeComHolder<ITypeInfo>       pITemp=0;    // An ITypeInfo to get a name.
    SafeComHolder<ITypeLib>        pITLB=0;     // For dup IID reporting.
    SafeComHolder<ITypeInfo>       pITIDup=0;   // For dup IID reporting.
    SafeComHolder<ICreateTypeInfo2> pCTI2=0;    // For creating the typeinfo.
    BSTRHolder                     bstrName=0;     // An ITypeInfo's name.
    BSTRHolder                     bstrDescr=0; // For description.
    BSTRHolder                     bstrDup=0;   // For dup IID reporting.
    ZeroHolder  zhType = &m_ErrorContext.m_pScope;              // Clear error reporting info.
    
    MethodTable* pClassOuter = pData->pClass;
    
    DefineFullyQualifiedNameForClassW();
        
    // Get the TypeDef and some info about it.
    td = pData->pClass->GetCl();
    IfFailReport(pData->pClass->GetMDImport()->GetTypeDefProps(td, &dwFlags, 0));
    tkind = pData->tkind;
    
    // Error reporting info.
    m_ErrorContext.m_tkType = td;
    m_ErrorContext.m_pScope = pData->pClass->GetMDImport();
    
    // A CoClass needs an IClassX, and an alias kind needs an alias.
    if (tkind != TKIND_COCLASS)
        return;
    
    // Check to see if the type is supposed to be visible from COM. If it
    // is not then we go to the next type.
    if (!IsTypeVisibleFromCom(TypeHandle(pClassOuter)))
        return;
    
    // Imported types don't need an IClassX.
    if (IsTdImport(dwFlags))
        return;
    
    // Check to see if we need to set up an IClassX for the class.
    ClassHasIClassX(pData->pClass, &classItfType);
    if (classItfType == clsIfNone)
        return;
    
    // Get full name from metadata.
    IfFailReport(pData->pClass->GetMDImport()->GetNameOfTypeDef(td, &pName, &pNS));
    
    // Get the GUID for the class.  Used to generate IClassX guid.
    pData->pClass->GetGuid(&clsid, TRUE);

    // Get the name of the class.  Use the ITypeInfo if there is one, except don't 
    //  use the typeinfo for types which are Aliased.
    if (pData->pCTI)
    {
        IfFailReport(SafeQueryInterface(pData->pCTI, IID_ITypeInfo, (IUnknown**)&pITemp));
        IfFailReport(pITemp->GetDocumentation(MEMBERID_NIL, &bstrName, 0,0,0));
        sName.Append(bstrName);
    }
    else
    {
        sName.AppendUTF8(pName);
    }

    // Create the typeinfo name for the IClassX
    sNameTypeInfo.Set(cIClassX);
    sNameTypeInfo.Append(sName);
    
    tkind = TKIND_INTERFACE;
    pSuffix = 0;
    for (;;)
    {
        // Try to create the TypeInfo.
        hr = m_pICreateTLB->CreateTypeInfo((LPOLESTR)sNameTypeInfo.GetUnicode(), tkind, &pCTITemp);
        
        // If a name conflict, decorate, otherwise, done.
        if (hr != TYPE_E_NAMECONFLICT)
            break;
            
        if (!bResolveDup)
        {
            hr = S_FALSE;
            return;
        }
                
        if (iSuffix == 0)
        {           
            iSuffix = 2;
        }
        else
        {
            sNameTypeInfo.Delete(sNameTypeInfo.End()-=2, 2);            
        }

        SString sDup;
        sDup.Printf(szDuplicateDecoration, iSuffix++);

        sNameTypeInfo.Append(sDup);
    }
    
    IfFailReport(hr);
    IfFailReport(SafeQueryInterface(pCTITemp, IID_ICreateTypeInfo2, (IUnknown**)&pCTI2));
    
    // Generate the "IClassX" UUID and set it.
    GenerateClassItfGuid(TypeHandle(pData->pClass), &guid);
    hr = pCTI2->SetGuid(guid);
    if (FAILED(hr))
    {
        if (hr == TYPE_E_DUPLICATEID)
        {
            IfFailReport(SafeQueryInterface(m_pICreateTLB, IID_ITypeLib, (IUnknown**)&pITLB));
            IfFailReport(pITLB->GetTypeInfoOfGuid(guid, &pITIDup));
            IfFailReport(pITIDup->GetDocumentation(MEMBERID_NIL, &bstrDup, 0,0,0));
            InternalThrowHRWithContext(TLBX_E_DUPLICATE_IID, sNameTypeInfo.GetUnicode(), (BSTR)bstrDup);
        }
        return;
    }

    // Adding methods may cause an href to this typeinfo, which will cause it to be layed out.
    //  Set the inheritance, so that nesting will be correct when that layout happens.
    // Add IDispatch as impltype 0.
    GetRefTypeInfo(pCTI2, m_pIDispatch, &href);
    IfFailReport(pCTI2->AddImplType(0, href));

    // Record the fully qualified type name in a custom attribute.
    LPCWSTR szName = GetFullyQualifiedNameForClassNestedAwareW(pData->pClass);
    V_VT(&vt) = VT_BSTR;
    V_BSTR(&vt) = SysAllocString(szName);
    if (V_BSTR(&vt) == NULL)
        IfFailReport(E_OUTOFMEMORY);

    IfFailReport(pCTI2->SetCustData(GUID_ManagedName, &vt));

    TRACE("IClassX  %x: %ls, {%08x-%04x-%04x-%04x-%02x%02x%02x%02x}\n", pCTI2, sName, 
        guid.Data1, guid.Data2, guid.Data3, guid.Data4[0]<<8|guid.Data4[1], guid.Data4[2], guid.Data4[3], guid.Data4[4], guid.Data4[5]); 

    // If the class is decorated with a description, apply it to the typelib.
    if(GetDescriptionString(pData->pClass, td, (BSTR &)bstrDescr))
        IfFailReport(pCTI2->SetDocString(bstrDescr));
    
    // Transfer ownership of the pointer.
    _ASSERTE(pData->pCTIClassItf == 0);
    pData->pCTIClassItf = pCTI2;
    pCTI2.SuppressRelease();
    pCTI2 = 0;
} // HRESULT TypeLibExporter::CreateIClassXITypeInfo()

//*****************************************************************************
// Add the impltypes to an ITypeInfo.
//*****************************************************************************
void TypeLibExporter::ConvertImplTypes(
    CExportedTypesInfo *pData)          // Conversion data.
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(pData));
    }
    CONTRACTL_END;
    
    HRESULT     hr = S_OK;              // A result.
    DWORD       dwFlags;                // A TypeDef's flags.
    mdTypeDef   td;                     // Token for the class.
    ZeroHolder  zhType = &m_ErrorContext.m_pScope;              // Clear error reporting info.
    
    // Get the TypeDef and some info about it.
    td = pData->pClass->GetCl();
    IfFailReport(pData->pClass->GetMDImport()->GetTypeDefProps(td, &dwFlags, 0));
    
    // Error reporting info.
    m_ErrorContext.m_tkType = td;
    m_ErrorContext.m_pScope = pData->pClass->GetMDImport();
    
    // If there is no ITypeInfo, skip it.
    if (pData->pCTI == 0)
        return;
    
    // Check to see if the type is supposed to be visible from COM. If it
    // is not then we go to the next type.
    if (!IsTypeVisibleFromCom(TypeHandle(pData->pClass)))
        return;
    
    // Add the ImplTypes to the CoClass.
    switch (pData->tkind)
    {
    case TKIND_INTERFACE:
    case TKIND_DISPATCH:
        // Add the base type to the interface.
            ConvertInterfaceImplTypes(pData->pCTI, pData->pClass);
        break;
            
    case TKIND_RECORD:
    case TKIND_UNION:
    case TKIND_ENUM:
        // Nothing to do at this step.
        break;
            
    case TKIND_COCLASS:
        // Add the ImplTypes to the CoClass.
            ConvertClassImplTypes(pData->pCTI, pData->pCTIClassItf, pData->pClass);
        break;
            
    default:
        _ASSERTE(!"Unknown TYPEKIND");
            IfFailReport(E_INVALIDARG);
        break;
    }
} // HRESULT TypeLibExporter::ConvertImplTypes()

//*****************************************************************************
// Convert the details (members) of an ITypeInfo.
//*****************************************************************************
void TypeLibExporter::ConvertDetails(
    CExportedTypesInfo *pData)          // Conversion data.
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(pData));
    }
    CONTRACTL_END;
    
    HRESULT     hr = S_OK;              // A result.
    DWORD       dwFlags;                // A TypeDef's flags.
    mdTypeDef   td;                     // Token for the class.
    ZeroHolder  zhType = &m_ErrorContext.m_pScope;              // Clear error reporting info.
    
    // Get the TypeDef and some info about it.
    td = pData->pClass->GetCl();
    IfFailReport(pData->pClass->GetMDImport()->GetTypeDefProps(td, &dwFlags, 0));
    
    // Error reporting info.
    m_ErrorContext.m_tkType = td;
    m_ErrorContext.m_pScope = pData->pClass->GetMDImport();
    
    // If there is no TypeInfo, skip it, but for CoClass need to populate IClassX.
    if (pData->pCTI == 0 && pData->tkind != TKIND_COCLASS)
        return;
    
    // Check to see if the type is supposed to be visible from COM. If it
    // is not then we go to the next type.
    if (!IsTypeVisibleFromCom(TypeHandle(pData->pClass)))
        return;
    
    // Fill in the rest of the typeinfo for this typedef.
    switch (pData->tkind)
    {
    case TKIND_INTERFACE:
    case TKIND_DISPATCH:
            ConvertInterfaceDetails(pData->pCTI, pData->pClass, pData->bAutoProxy);
        break;
            
    case TKIND_RECORD:
    case TKIND_UNION:
            ConvertRecord(pData);
        break;
            
    case TKIND_ENUM:
            ConvertEnum(pData->pCTI, pData->pClass);
        break;
            
    case TKIND_COCLASS:
        // Populate the methods on the IClassX interface.
            ConvertClassDetails(pData->pCTI, pData->pCTIClassItf, pData->pClass, pData->bAutoProxy);
        break;
            
    default:
        _ASSERTE(!"Unknown TYPEKIND");
            IfFailReport(E_INVALIDARG);
        break;
    } // Switch (tkind)

    // Report that this type has been converted.
    SString ssType;
    if (IsTdNested(dwFlags))
    {
        TypeNameBuilder tnb(&ssType, TypeNameBuilder::ParseStateNAME);
        TypeString::AppendNestedTypeDef(tnb, m_ErrorContext.m_pScope, m_ErrorContext.m_tkType);
    }
    else
        TypeString::AppendTypeDef(ssType, m_ErrorContext.m_pScope, m_ErrorContext.m_tkType);
    ReportEvent(NOTIF_TYPECONVERTED, TLBX_I_TYPE_EXPORTED, ssType.GetUnicode());
} // void TypeLibExporter::ConvertDetails()
    
//*****************************************************************************
// Add the ImplTypes to the TypeInfo.
//*****************************************************************************
void TypeLibExporter::ConvertInterfaceImplTypes(
    ICreateTypeInfo2 *pThisTypeInfo,    // The typeinfo being created.
    MethodTable     *pClass)                // MethodTable for the TypeInfo.
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(pThisTypeInfo));
        PRECONDITION(CheckPointer(pClass));
    }
    CONTRACTL_END;

    HRESULT     hr = S_OK;
    ULONG       ulIface;                // Is this interface [dual]?
    HREFTYPE    href;                   // href of base interface.

    // IDispatch or IUnknown derived?
    IfFailReport(pClass->GetMDImport()->GetIfaceTypeOfTypeDef(pClass->GetCl(), &ulIface));

    // Parent interface.
    if (IsDispatchBasedItf((CorIfaceAttr)ulIface))
    {
        // Get the HREFTYPE for IDispatch.
        GetRefTypeInfo(pThisTypeInfo, m_pIDispatch, &href);
    }
    else
    {
        // Get the HREFTYPE for IUnknown.
        GetRefTypeInfo(pThisTypeInfo, m_pIUnknown, &href);
    }

    // Add the HREF as an interface.
    IfFailReport(pThisTypeInfo->AddImplType(0, href));
} // void TypeLibExporter::ConvertInterfaceImplTypes()


//*****************************************************************************
// Create the TypeInfo for an interface by iterating over functions.
//*****************************************************************************
void TypeLibExporter::ConvertInterfaceDetails (
    ICreateTypeInfo2 *pThisTypeInfo,    // The typeinfo being created.
    MethodTable     *pMT,                // MethodTable for the TypeInfo.
    int         bAutoProxy)             // If true, oleaut32 is the interface's marshaller.
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(pThisTypeInfo));
        PRECONDITION(CheckPointer(pMT));
    }
    CONTRACTL_END;
    
    HRESULT     hr = S_OK;
    ULONG       ulIface;                // Is this interface [dual]?
    DWORD       dwTIFlags=0;            // TypeLib flags.
    int         cVisibleMembers = 0;    // The count of methods that are visible to COM.

    // Retrieve the map of members.
    ComMTMemberInfoMap MemberMap(pMT);

    // IDispatch or IUnknown derived?
    IfFailReport(pMT->GetMDImport()->GetIfaceTypeOfTypeDef(pMT->GetCl(), &ulIface));
    
    if (IsDispatchBasedItf((CorIfaceAttr)ulIface))
    {
        // IDispatch derived.
        dwTIFlags |= TYPEFLAG_FDISPATCHABLE;
        
        if (ulIface == ifDual)
            dwTIFlags |= TYPEFLAG_FDUAL | TYPEFLAG_FOLEAUTOMATION;
        else
            _ASSERTE(ulIface == ifDispatch);
    }
    else
    {
        // IUnknown derived.
        dwTIFlags |= TYPEFLAG_FOLEAUTOMATION;
    }
    
    if (!bAutoProxy)
        dwTIFlags |= TYPEFLAG_FPROXY;

    // Set appropriate flags.
    IfFailReport(pThisTypeInfo->SetTypeFlags(dwTIFlags));

    // Retrieve the method properties.
    size_t sizeOfPtr = IsExportingAs64Bit() ? 8 : 4;
    
    MemberMap.Init(sizeOfPtr);
    if (MemberMap.HadDuplicateDispIds())
        ReportWarning(TLBX_I_DUPLICATE_DISPID, TLBX_I_DUPLICATE_DISPID);

    // We need a scope to bypass the inialization skipped by goto ErrExit 
    // compiler error.
    {
        CQuickArray<ComMTMethodProps> &rProps = MemberMap.GetMethods();

        // Now add the methods to the TypeInfo.
        MethodTable::MethodIterator it(pMT);
        for (; it.IsValid(); it.Next())
        {
            if (it.IsVirtual())
            {
            // Only convert the method if it is visible from COM.
                if (rProps[it.GetSlotNumber()].bMemberVisible)
            {
                    if (ConvertMethod(pThisTypeInfo, &rProps[it.GetSlotNumber()], cVisibleMembers, ulIface))
                    cVisibleMembers++;
                }
            }
        }
    }
} // void TypeLibExporter::ConvertInterfaceDetails()

//*****************************************************************************
// Export a Record to a TypeLib.
//*****************************************************************************
void TypeLibExporter::ConvertRecordBaseClass(
    CExportedTypesInfo *pData,          // Conversion data.
    MethodTable     *pSubMT,             // The base class.
    ULONG       &ixVar)                 // Variable index in the typelib.
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(pData));
        PRECONDITION(CheckPointer(pSubMT));
    }
    CONTRACTL_END;
    
    // The typeinfo being created.
    ICreateTypeInfo2 *pThisTypeInfo = pData->pCTI;

    HRESULT     hr = S_OK;              // A result.
    mdFieldDef  fd;                     // A Field def.
    ULONG       iFD;                    // Loop control.
    ULONG       cFD;                    // Count of total MemberDefs.
    DWORD       dwFlags;                // Field flags.
    LPCUTF8     szName;                 // Name in UTF8.
    LPCUTF8     szNamespace;            // A Namespace in UTF8.
    SString     sName;                  // Name

    // To enum fields.
    HENUMInternalHolder eFDi(pSubMT->GetMDImport());

    // If there is no class here, or if the class is Object, don't add members.
    if (pSubMT == 0 ||
        pSubMT == g_pObjectClass) 
        return;

    // If this class has a base class, export those members first.
    ConvertRecordBaseClass(pData, pSubMT->GetParentMethodTable(), ixVar);

    // Build the member name prefix.
    IfFailReport(pSubMT->GetMDImport()->GetNameOfTypeDef(pSubMT->GetCl(), &szName, &szNamespace));
    
    sName.SetUTF8(szName);
    sName.Append(W("_"));
        
    // Get an enumerator for the MemberDefs in the TypeDef.
    eFDi.EnumInit(mdtFieldDef, pSubMT->GetCl());
    cFD = pSubMT->GetMDImport()->EnumGetCount(&eFDi);

    SString sNameMember;
    // For each MemberDef...
    for (iFD=0; iFD<cFD; ++iFD)
    {
        // Get the next field.
        if (!pSubMT->GetMDImport()->EnumNext(&eFDi, &fd))
        {
            IfFailReport(E_UNEXPECTED);
        }
        
        IfFailReport(pSubMT->GetMDImport()->GetFieldDefProps(fd, &dwFlags));
        
        // Only non-static fields.
        if (!IsFdStatic(dwFlags))
        {
            IfFailReport(pSubMT->GetMDImport()->GetNameOfFieldDef(fd, &szName));
            
            sNameMember.Set(sName);
            sNameMember.AppendUTF8(szName);
            if (ConvertVariable(pThisTypeInfo, pSubMT, fd, sNameMember, ixVar))
                ixVar++;
        }
    }
} // void TypeLibExporter::ConvertRecordBaseClass()

void TypeLibExporter::ConvertRecord(
    CExportedTypesInfo *pData)          // Conversion data.
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(pData));
    }
    CONTRACTL_END;
    
    ICreateTypeInfo2 *pThisTypeInfo=pData->pCTI;     // The typeinfo being created.
    MethodTable     *pMT=pData->pClass;               // MethodTable for the TypeInfo.

    HRESULT     hr = S_OK;              // A result.
    mdFieldDef  fd;                     // A Field def.
    ULONG       iFD;                    // Loop control.
    ULONG       ixVar=0;                // Index of current var converted.
    ULONG       cFD;                    // Count of total MemberDefs.
    DWORD       dwFlags;                // Field flags.
    DWORD       dwPack;                 // Class pack size.
    mdToken     tkExtends;              // A class's parent.
    LPCUTF8     szName;                 // Name in UTF8.
    SString     sName;                  // Name.

    // To enum fields.
    HENUMInternalHolder eFDi(pMT->GetMDImport());

    // If the type is a struct, but it has explicit layout, don't export the members, 
    //  because we can't export them accurately (unless they're really sequential).
    if (pData->tkind == TKIND_RECORD)
    {
        IfFailReport(pMT->GetMDImport()->GetTypeDefProps(pMT->GetCl(), &dwFlags, &tkExtends));
        
        if (IsTdExplicitLayout(dwFlags))
        {
            ReportWarning(S_OK, TLBX_I_NONSEQUENTIALSTRUCT);
            return;
        }
    }

    // Set the packing size, if there is one.
    dwPack = 0;
    if (FAILED(pMT->GetMDImport()->GetClassPackSize(pMT->GetCl(), &dwPack)))
    {
        dwPack = 0;
    }
    if (dwPack == 0)
    {
        dwPack = DEFAULT_PACKING_SIZE;
    }
    
    IfFailReport(pThisTypeInfo->SetAlignment((USHORT)dwPack));

    // Haven't seen any non-public members yet.
    m_bWarnedOfNonPublic = FALSE;

    // If this class has a base class, export those members first.
    ConvertRecordBaseClass(pData, pMT->GetParentMethodTable(), ixVar);

    // Get an enumerator for the MemberDefs in the TypeDef.
    eFDi.EnumInit(mdtFieldDef, pMT->GetCl());
    cFD = pMT->GetMDImport()->EnumGetCount(&eFDi);

    // For each MemberDef...
    for (iFD=0; iFD<cFD; ++iFD)
    {
        // Get the next field.
        if (!pMT->GetMDImport()->EnumNext(&eFDi, &fd))
        {
            IfFailReport(E_UNEXPECTED);
        }
        
        IfFailReport(pMT->GetMDImport()->GetFieldDefProps(fd, &dwFlags));
        
        // Skip static fields.
        if (IsFdStatic(dwFlags) == 0)
        {
            IfFailReport(pMT->GetMDImport()->GetNameOfFieldDef(fd, &szName));
            
            sName.SetUTF8(szName);
            if (ConvertVariable(pThisTypeInfo, pMT, fd, sName, ixVar))
                ixVar++;
        }
    }
} // HRESULT TypeLibExporter::ConvertRecord()

//*****************************************************************************
// Export an Enum to a typelib.
//*****************************************************************************
void TypeLibExporter::ConvertEnum(
    ICreateTypeInfo2 *pThisTypeInfo,    // The typeinfo being created.
    MethodTable     *pMT)                // MethodTable for the TypeInfo.
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(pThisTypeInfo));
        PRECONDITION(CheckPointer(pMT));
    }
    CONTRACTL_END;

    HRESULT     hr = S_OK;              // A result.
    mdFieldDef  fd;                     // A Field def.
    DWORD       dwTIFlags=0;            // TypeLib flags.
    ULONG       dwFlags;                // A field's flags.
    ULONG       iFD;                    // Loop control.
    ULONG       cFD;                    // Count of total MemberDefs.
    ULONG       iVar=0;                 // Count of vars actually converted.
    LPCUTF8     szName;                 // Name in UTF8.
    SString     sName;                  // Name.
    SafeComHolder<ITypeInfo> pThisTI=0; // TypeInfo for this ICreateITypeInfo.
    BSTRHolder        szThisTypeInfo=0; // Name of this ITypeInfo.

    IMDInternalImport* pImport = pMT->GetMDImport();

    // To enum fields.    
    HENUMInternalHolder eFDi(pImport);
   
    // Explicitly set the flags.
    IfFailReport(pThisTypeInfo->SetTypeFlags(dwTIFlags));

    // Get an enumerator for the MemberDefs in the TypeDef.
    eFDi.EnumInit(mdtFieldDef, pMT->GetCl());
    cFD = pImport->EnumGetCount(&eFDi);

    // Build the member name prefix.  If generating an enum, get the real name from the default interface.
    IfFailReport(SafeQueryInterface(pThisTypeInfo, IID_ITypeInfo, (IUnknown**)&pThisTI));
    IfFailReport(pThisTI->GetDocumentation(MEMBERID_NIL, &szThisTypeInfo, 0,0,0));

    sName.Set(szThisTypeInfo);
    sName.Append(W("_"));

    SString sNameMember;
    // For each MemberDef...
    for (iFD=0; iFD<cFD; ++iFD)
    {
        // Get the next field.
        if (!pImport->EnumNext(&eFDi, &fd))
        {
            IfFailReport(E_UNEXPECTED);
        }
        
        // Only convert static fields.
        IfFailReport(pImport->GetFieldDefProps(fd, &dwFlags));
        
        if (IsFdStatic(dwFlags) == 0)
        {
            continue;
        }
        
        // Skip ComVisible(false) members
        if (!IsMemberVisibleFromCom(pMT, fd, mdTokenNil))
        {
            continue;
        }

        sNameMember.Set(sName);
        IfFailReport(pImport->GetNameOfFieldDef(fd, &szName));
        
        sNameMember.AppendUTF8(szName);
        
        if (ConvertEnumMember(pThisTypeInfo, pMT, fd, sNameMember, iVar))
        {
            iVar++;
        }
    }
} // void TypeLibExporter::ConvertEnum()

//*****************************************************************************
// Does a class have a default ctor?
//*****************************************************************************
BOOL TypeLibExporter::HasDefaultCtor(
    MethodTable     *pMT)                // The class in question.
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(pMT));
    }
    CONTRACTL_END;

    HRESULT     hr;                     // A result.
    mdMethodDef md;                     // A method of the type.
    DWORD       dwFlags;                // Method's flags.
    ULONG       cMD;                    // Count of returned tokens.
    ULONG       iMD;                    // Loop control.
    PCCOR_SIGNATURE pSig;               // The signature.
    ULONG       ixSig;                  // Index into signature.
    ULONG       cbSig;                  // Size of the signature.
    ULONG       callconv;               // Method's calling convention.
    ULONG       cParams;                // Method's count of parameters.
    BOOL        rslt=FALSE;             // Was one found?
    LPCUTF8     pName;                  // Method name.

    IMDInternalImport* pImport = pMT->GetMDImport();
    
    // To enum methods.
    HENUMInternalHolder eMDi(pImport);

    // Get an enumerator for the MemberDefs in the TypeDef.
    eMDi.EnumInit(mdtMethodDef, pMT->GetCl());
    cMD = pImport->EnumGetCount(&eMDi);

    // For each MemberDef...
    for (iMD=0; iMD<cMD; ++iMD)
    {
        // Get the next field.
        if (!pImport->EnumNext(&eMDi, &md))
        {
            IfFailReport(E_UNEXPECTED);
        }
        
        // Is the name special?  Is the method public?
        IfFailReport(pImport->GetMethodDefProps(md, &dwFlags));
        
        if (!IsMdRTSpecialName(dwFlags) || !IsMdPublic(dwFlags))
            continue;
        
        // Yes, is the name a ctor?
        IfFailReport(pImport->GetNameOfMethodDef(md, &pName));
        
        if (!IsMdInstanceInitializer(dwFlags, pName))
            continue;
        
        // It is a ctor.  Is it a default ctor?
        IfFailReport(pImport->GetSigOfMethodDef(md, &cbSig, &pSig));
        
        // Skip the calling convention, and get the param count.
        ixSig = CorSigUncompressData(pSig, &callconv);
        CorSigUncompressData(&pSig[ixSig], &cParams);
        
        // Default ctor has zero params.
        if (cParams == 0)
        {
            rslt = TRUE;
            break;
        }
    }

    return rslt;
} // BOOL TypeLibExporter::HasDefaultCtor()

//*****************************************************************************
// Export a class to a TypeLib.
//*****************************************************************************
void TypeLibExporter::ConvertClassImplTypes(
    ICreateTypeInfo2 *pThisTypeInfo,    // The typeinfo being created.
    ICreateTypeInfo2 *pClassItfTypeInfo,// The ICLassX for the TypeInfo.
    MethodTable     *pMT)                // MethodTable for the TypeInfo.
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(pThisTypeInfo, NULL_OK));
        PRECONDITION(CheckPointer(pClassItfTypeInfo, NULL_OK));
        PRECONDITION(CheckPointer(pMT));
    }
    CONTRACTL_END;
    
    HRESULT     hr = S_OK;
    HREFTYPE    href;                   // HREF to a TypeInfo.
    DWORD       dwFlags;                // Metadata flags.
    int         flags=0;                // Flags for the interface impl or CoClass.
    UINT        iImpl=0;                // Current Impl index.
    MethodTable     *pIDefault = 0;         // Default interface, if any.
    MethodTable *pDefItfMT = 0;         // Default interface method table, if any.
    CQuickArray<MethodTable *> SrcItfList; // List of event sources.
    CorClassIfaceAttr classItfType = clsIfNone; // For class interface type.
    DefaultInterfaceType DefItfType;
    TypeHandle hndDefItfClass;

    SafeComHolder<ITypeInfo> pTI=0;                 // TypeInfo for default dispinterface.
    SafeComHolder<ICreateTypeInfo2> pCTI2 = NULL;   // The ICreateTypeInfo2 interface used to define custom data.

    // We should never be converting the class impl types of COM imported CoClasses.
    _ASSERTE(!pMT->IsComImport());
    
    if (pThisTypeInfo)
    {   
        IfFailReport(pMT->GetMDImport()->GetTypeDefProps(pMT->GetCl(), &dwFlags, 0));
        
        // If abstract class, or no default ctor, don't make it creatable.
        if (!IsTdAbstract(dwFlags) && HasDefaultCtor(pMT))
            flags |= TYPEFLAG_FCANCREATE;
        
        // PreDeclid as appropriate.
        IfFailReport(pThisTypeInfo->SetTypeFlags(flags));
    }    

    // Retrieve the MethodTable that represents the default interface.
    DefItfType = GetDefaultInterfaceForClassWrapper(TypeHandle(pMT), &hndDefItfClass);

    // Remember the MethodTable of the default interface.
    pIDefault = hndDefItfClass.GetMethodTable();

    // For some classes we synthesize an IClassX.  We don't do that for 
    // configured class, classes imported from COM, 
    // or for classes with an explicit default interface.
    if (pClassItfTypeInfo)
    {   
        // Set the interface as the default for the class.
        IfFailReport(SafeQueryInterface(pClassItfTypeInfo, IID_ITypeInfo, (IUnknown**)&pTI));
        GetRefTypeInfo(pThisTypeInfo, pTI, &href);
        IfFailReport(pThisTypeInfo->AddImplType(iImpl, href));

        // If the class interface is the default interface, mark it as such.
        if (pMT == pIDefault)
            IfFailReport(pThisTypeInfo->SetImplTypeFlags(iImpl, IMPLTYPEFLAG_FDEFAULT));

        // Increment the impl count.
        ++iImpl;
    }

    // Go up the class hierarchy and add the IClassX's of the parent classes 
    // as interfaces implemented by the COM component.
    MethodTable *pParentClass = pMT->GetComPlusParentMethodTable();
    while (pParentClass)
    {
        // If the parent class has an IClassX interface then add it.
        ClassHasIClassX(pParentClass, &classItfType);
        if (classItfType == clsIfAutoDual)
        {
            hr = EEClassToHref(pThisTypeInfo, pParentClass, FALSE, &href);

            // If not IUnknown, add the HREF as an interface.
            if (hr != S_USEIUNKNOWN)
            {
                IfFailReport(pThisTypeInfo->AddImplType(iImpl, href));
                if (pParentClass == pIDefault)
                    IfFailReport(pThisTypeInfo->SetImplTypeFlags(iImpl, IMPLTYPEFLAG_FDEFAULT));

                ++iImpl;
            }
        }

        // Process the next class up the hierarchy.
        pParentClass = pParentClass->GetComPlusParentMethodTable();
    }

    ComCallWrapperTemplate *pClassTemplate = ComCallWrapperTemplate::GetTemplate(TypeHandle(pMT));
    MethodTable::InterfaceMapIterator it = pMT->IterateInterfaceMap();
    while (it.Next())
    {
        flags = 0;
        
        // Get the MethodTable for an implemented interface.
        MethodTable *pIClass = it.GetInterface();
        
        // Retrieve the ComMethodTable for the interface.
        ComMethodTable *pItfComMT = pClassTemplate->GetComMTForItf(pIClass);

        // If the interface is visible from COM, add it.
        if (IsTypeVisibleFromCom(TypeHandle(pIClass)) && !pItfComMT->IsComClassItf())
        {
#if defined(_DEBUG)
            TRACE("Class %s implements %s\n", pMT->GetDebugClassName(), pIClass->GetDebugClassName());
#endif
            // Get an href for the managed class.
            hr = EEClassToHref(pThisTypeInfo, pIClass, FALSE, &href);
            
            // If not IUnknown, add the HREF as an interface.
            if (hr != S_USEIUNKNOWN)
            {
                if (pIClass == pIDefault)
                    flags |= IMPLTYPEFLAG_FDEFAULT;

                IfFailReport(pThisTypeInfo->AddImplType(iImpl, href));
                IfFailReport(pThisTypeInfo->SetImplTypeFlags(iImpl, flags));
                ++iImpl;
            }
        }
        else if (!IsTypeVisibleFromCom(TypeHandle(pIClass)) && (pIClass == pIDefault))
        {
            // Report a warning if the default interface is not COM visible
            ReportWarning(TLBX_W_DEFAULT_INTF_NOT_VISIBLE, TLBX_W_DEFAULT_INTF_NOT_VISIBLE);
        }
    }
    
    // Retrieve the list of COM source interfaces for the managed class.
    GetComSourceInterfacesForClass(pMT, SrcItfList);
        
    // Add all the source interfaces to the CoClass.
    flags = IMPLTYPEFLAG_FSOURCE | IMPLTYPEFLAG_FDEFAULT;
    for (UINT i = 0; i < SrcItfList.Size(); i++)
    {
        hr = EEClassToHref(pThisTypeInfo, SrcItfList[i], FALSE, &href);

        // If not IUnknown, add the HREF as an interface.
        if (hr != S_USEIUNKNOWN)
        {
            IfFailReport(pThisTypeInfo->AddImplType(iImpl, href));
            IfFailReport(pThisTypeInfo->SetImplTypeFlags(iImpl, flags));
            ++iImpl;
            flags = IMPLTYPEFLAG_FSOURCE;
        }
    }
} // void TypeLibExporter::ConvertClassImplTypes()
        
//*****************************************************************************
// Export a class to a TypeLib.
//*****************************************************************************
void TypeLibExporter::ConvertClassDetails(
    ICreateTypeInfo2 *pThisTypeInfo,    // The typeinfo being created.
    ICreateTypeInfo2 *pDefaultTypeInfo, // The ICLassX for the TypeInfo.
    MethodTable     *pMT,                // MethodTable for the TypeInfo.
    int         bAutoProxy)             // If true, oleaut32 is the proxy.
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(pThisTypeInfo, NULL_OK));
        PRECONDITION(CheckPointer(pDefaultTypeInfo, NULL_OK));
        PRECONDITION(CheckPointer(pMT));
    }
    CONTRACTL_END;

    HRESULT             hr = S_OK;
    CorClassIfaceAttr   classItfType = clsIfNone; 
    
    ClassHasIClassX(pMT, &classItfType);
    if (classItfType == clsIfAutoDual)
    {
        // Set up the IClassX interface.
        ConvertIClassX(pDefaultTypeInfo, pMT, bAutoProxy);
    }
    else if (pDefaultTypeInfo)
    {
        DWORD dwTIFlags = TYPEFLAG_FDUAL | TYPEFLAG_FOLEAUTOMATION | TYPEFLAG_FDISPATCHABLE | TYPEFLAG_FHIDDEN;
        if (!bAutoProxy)
            dwTIFlags |= TYPEFLAG_FPROXY;
        IfFailReport(pDefaultTypeInfo->SetTypeFlags(dwTIFlags));
    }
} // void TypeLibExporter::ConvertClassDetails()

//*****************************************************************************
// Create the DispInterface for the vtable that describes an entire class.
//*****************************************************************************
void TypeLibExporter::ConvertIClassX(
    ICreateTypeInfo2 *pThisTypeInfo,     // The TypeInfo for the IClassX.
    MethodTable     *pMT,                // The MethodTable object for the class.
    int         bAutoProxy)             // If true, oleaut32 is the proxy.
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(pThisTypeInfo));
        PRECONDITION(CheckPointer(pMT));
    }
    CONTRACTL_END;

    HRESULT     hr = S_OK;              // A result.
    DWORD       dwTIFlags=0;            // TypeLib flags.
    DWORD       nSlots;                 // Number of vtable slots.
    UINT        i;                      // Loop control.
    int         cVisibleMembers = 0;    // The count of methods that are visible to COM.
    ComMTMemberInfoMap MemberMap(pMT); // The map of members.

    // Should be an actual class.
    _ASSERTE(!pMT->IsInterface());

    // Retrieve the method properties.
    size_t sizeOfPtr = IsExportingAs64Bit() ? 8 : 4;
    
    MemberMap.Init(sizeOfPtr);
    if (MemberMap.HadDuplicateDispIds())
        ReportWarning(TLBX_I_DUPLICATE_DISPID, TLBX_I_DUPLICATE_DISPID);

    // We need a scope to bypass the inialization skipped by goto ErrExit 
    // compiler error.
    {
        CQuickArray<ComMTMethodProps> &rProps = MemberMap.GetMethods();
        nSlots = (DWORD)rProps.Size();

        dwTIFlags |= TYPEFLAG_FDUAL | TYPEFLAG_FOLEAUTOMATION | TYPEFLAG_FDISPATCHABLE | TYPEFLAG_FHIDDEN | TYPEFLAG_FNONEXTENSIBLE;
        if (!bAutoProxy)
            dwTIFlags |= TYPEFLAG_FPROXY;
        IfFailReport(pThisTypeInfo->SetTypeFlags(dwTIFlags));

        // Assign slot numbers.
        for (i=0; i<nSlots; ++i)
            rProps[i].oVft = (short)((7 + i) * sizeOfPtr);

        // Now add the methods to the TypeInfo.
        for (i=0; i<nSlots; ++i)
        {
            TRACE("[%d] %10ls pMeth:%08x, prop:%d, semantic:%d, dispid:0x%x, oVft:%d\n", i, rProps[i].pName, rProps[i].pMeth, 
                    rProps[i].property, rProps[i].semantic, rProps[i].dispid, rProps[i].oVft);
            if (rProps[i].bMemberVisible)
            {
                if (rProps[i].semantic < FieldSemanticOffset)
                {
                    if (ConvertMethod(pThisTypeInfo, &rProps[i], cVisibleMembers, ifDual))
                        cVisibleMembers++;
                }
                else
                {
                    if (ConvertFieldAsMethod(pThisTypeInfo, &rProps[i], cVisibleMembers))
                        cVisibleMembers++;
                }
            }
        }
    }
} // void TypeLibExporter::ConvertIClassX()


//*****************************************************************************
// Export a Method's metadata to a typelib.
//*****************************************************************************
#ifdef _PREFAST_
#pragma warning(push)
#pragma warning(disable:21000) // Suppress PREFast warning about overly large function
#endif
BOOL TypeLibExporter::ConvertMethod(
    ICreateTypeInfo2 *pCTI,             // ICreateTypeInfo2 to get the method.
    ComMTMethodProps *pProps,           // Some properties of the method.
    ULONG       iMD,                    // Index of the member
    ULONG       ulIface)                // Is this interface : IUnknown, [dual], or DISPINTERFACE?
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(pCTI));
        PRECONDITION(CheckPointer(pProps));
    }
    CONTRACTL_END;

    HRESULT     hr = S_OK;              // A result.
    HRESULT     hrSignature = S_OK;     // A failure HR;
    LPCUTF8     pszName;                // Name in UTF8.
    SString     sName;                  // Holds name.
    ULONG       dwImplFlags;            // The function's impl flags.
    PCCOR_SIGNATURE pbSig;              // Pointer to Cor signature.
    ULONG       cbSig;                  // Size of Cor signature.
    ULONG       ixSig;                  // Index into signature.
    ULONG       cbElem;                 // Size of an element in the signature.
    ULONG       callconv;               // A member's calling convention.
    ULONG       ret;                    // The return type.
    ULONG       elem;                   // A signature element.
    TYPEDESC    *pRetVal=0;             // Return type's TYPEDESC.
    ULONG       cSrcParams;             // Count of source params.
    ULONG       cDestParams = 0;        // Count of dest parameters.
    USHORT      iSrcParam;              // Loop control, over params.
    USHORT      iDestParam;             // Loop control, over params.
    USHORT      iLCIDParam;             // The index of the LCID param.
    ULONG       dwParamFlags;           // A parameter's flags.
    CDescPool   sPool;                  // Pool of memory in which to build funcdesc.
    CDescPool   sVariants;              // Pool of variants for default values.
    PARAMDESCEX *pParamDesc;            // Pointer to one param default value.
    int         bHrMunge=true;          // Munge return type to HRESULT?
    CQuickArray<BSTR> rNames;           // Array of names to function and parameters.
    ULONG       cNames=0;               // Count of function and parameter names.
    FUNCDESC    *pfunc = NULL;          // A funcdesc.
    MethodDesc  *pMeth;                 // A MethodDesc.
    IMDInternalImport *pInternalImport; // Internal interface containing the method.
    MDDefaultValue defaultValue;        // place holder for default value
    PCCOR_SIGNATURE pvNativeType;       // native parameter type
    ULONG           cbNativeType = 0;   // native parameter type length
    MethodTable     *pMT;                // Class containing the method.
    int         bHasOptorDefault=false; // If true, the method has optional params or default values -- no vararg
    const void  *pvData;                // Pointer to a custom attribute.
    ULONG       cbData;                 // Size of custom attribute.
    BOOL        bByRef;                 // Is a parameter byref?
    BSTRHolder        bstrDescr=0;             // Description of the method.
    VariantHolder vtManagedName;        // Variant used to set the managed name of the member.
    
    ZeroHolder  zhParam = &m_ErrorContext.m_szParam;    // Clear error reporting info.
    ZeroHolder  zhMember = &m_ErrorContext.m_szMember;  // Clear error reporting info.

    // Get info about the method.
    pMeth = pProps->pMeth;
    pMeth->GetSig(&pbSig, &cbSig);
    pInternalImport = pMeth->GetMDImport();
    pMT = pMeth->GetMethodTable();
    IfFailReport(pInternalImport->GetMethodImplProps(pMeth->GetMemberDef(), 0, &dwImplFlags));
    
    // Error reporting info.
    IfFailReport(pInternalImport->GetNameOfMethodDef(pMeth->GetMemberDef(), &m_ErrorContext.m_szMember));
    
    // Allocate one variant.
    pParamDesc = reinterpret_cast<PARAMDESCEX*>(sVariants.AllocZero(sizeof(PARAMDESCEX)));
    if(NULL == pParamDesc)
        IfFailReport(E_OUTOFMEMORY);

    // Prepare to parse signature and build the FUNCDESC.
    pfunc = reinterpret_cast<FUNCDESC*>(sPool.AllocZero(sizeof(FUNCDESC)));
    if (pfunc == NULL)
        IfFailReport(E_OUTOFMEMORY);
    
    ixSig = 0;

    // Get the calling convention.
    ixSig += CorSigUncompressData(&pbSig[ixSig], &callconv);
    _ASSERTE((callconv & IMAGE_CEE_CS_CALLCONV_MASK) != IMAGE_CEE_CS_CALLCONV_FIELD);
    pfunc->callconv = Clr2TlbCallConv[callconv & IMAGE_CEE_CS_CALLCONV_MASK];

    // vtable offset.
    pfunc->oVft = pProps->oVft;

    // Get the argument count.  Allow for an extra in case of [retval].
    ixSig += CorSigUncompressData(&pbSig[ixSig], &cSrcParams);
    cDestParams = cSrcParams;
    rNames.ReSizeThrows(cDestParams+3);
    memset(rNames.Ptr(), 0, (cDestParams+3) * sizeof(BSTR));

    // Set some method properties.
    pfunc->memid = pProps->dispid;
    if (pfunc->memid == -11111) //@todo: fix for msvbalib.dll
        pfunc->memid = -1;
    pfunc->funckind = FUNC_PUREVIRTUAL;

    // Set the invkind based on whether the function is an accessor.
    if (pProps->semantic == 0)
        pfunc->invkind = INVOKE_FUNC;
    else if (pProps->semantic == msGetter)
        pfunc->invkind = INVOKE_PROPERTYGET;
    else if (pProps->semantic == msSetter)
        pfunc->invkind = INVOKE_PROPERTYPUTREF;
    else if (pProps->semantic == msOther)
        pfunc->invkind = INVOKE_PROPERTYPUT;
    else
        pfunc->invkind = INVOKE_FUNC; // non-accessor property function.

    rNames[0] = pProps->pName;
    cNames = 1;
    
    // Convert return type to elemdesc.  If we are doing HRESULT munging, we need to 
    //  examine the return type, and if it is not VOID, create an additional final 
    //  parameter as a pointer to the type.

    // Get the return type.  
    cbElem = CorSigUncompressData(&pbSig[ixSig], &ret);

    // Error reporting info.
    m_ErrorContext.m_ixParam = 0;
    
    // Get native type of return if available
    mdParamDef pdParam;
    pvNativeType = NULL;
    hr = pInternalImport->FindParamOfMethod(pMeth->GetMemberDef(), 0, &pdParam);
    if (hr == S_OK)
    {
        hr = pInternalImport->GetFieldMarshal(pdParam, &pvNativeType, &cbNativeType);
        if (hr != CLDB_E_RECORD_NOTFOUND)
        {
            IfFailReport(hr);
        }
    }
    
    // Determine if we need to do HRESULT munging.
    bHrMunge = !IsMiPreserveSig(dwImplFlags);
    
    // Reset some properties for DISPINTERFACES.
    if (ulIface == ifDispatch)
    {
        pfunc->callconv = CC_STDCALL;
        pfunc->funckind = FUNC_DISPATCH;
        
        // Never munge a dispinterface.
        bHrMunge = false;
    }
    
    if (bHrMunge)
    {
        // Munge the return type into a new last param, set return type to HRESULT.
        pfunc->elemdescFunc.tdesc.vt = VT_HRESULT;
        
        // Does the function actually return anything?
        if (ret == ELEMENT_TYPE_VOID)
        {
            // Skip over the return value, no [retval].
            pRetVal = 0;
            ixSig += cbElem;
        }
        else
        {
            // Allocate a TYPEDESC to be pointed to, convert type into it.
            pRetVal = reinterpret_cast<TYPEDESC*>(sPool.AllocZero(sizeof(TYPEDESC)));       
            if (pRetVal == NULL)
                IfFailReport(E_OUTOFMEMORY);
            
            hr = CorSigToTypeDesc(pCTI, pMT, &pbSig[ixSig], pvNativeType, cbNativeType, &cbElem, pRetVal, &sPool, TRUE);
            if (FAILED(hr))
                return FALSE;
            
            ixSig += cbElem;

            ++cDestParams;
            // It is pretty weird for a property putter to return something, but apparenly legal.
            //_ASSERTE(pfunc->invkind != INVOKE_PROPERTYPUT && pfunc->invkind != INVOKE_PROPERTYPUTREF);

            // Todo:  When the C compiler tries to import a typelib with a C 
            // array return type (even if it's a retval), 
            // it generates a wrapper method with a signature like "int [] foo()", 
            // which isn't valid C, so it barfs.  So, we'll change the return type 
            // to a pointer by hand.
            if (pRetVal->vt == VT_CARRAY)
            {
                pRetVal->vt = VT_PTR;
                pRetVal->lptdesc = &pRetVal->lpadesc->tdescElem;
            }
        }
    }
    else
    {
        // No munging, convert return type.
        pRetVal = 0;
        hr = CorSigToTypeDesc(pCTI, pMT, &pbSig[ixSig], pvNativeType, cbNativeType, &cbElem, &pfunc->elemdescFunc.tdesc, &sPool, TRUE);
        if (FAILED(hr))
            return FALSE;
        
        ixSig += cbElem;
    }

    // Error reporting info.
    m_ErrorContext.m_ixParam = -1;
    
    // Check to see if there is an LCIDConversion attribute on the method.
    iLCIDParam = (USHORT)GetLCIDParameterIndex(pMeth);
    if (iLCIDParam != (USHORT)-1)
    {
        BOOL bValidLCID = TRUE;

        // Make sure the parameter index is valid.
        if (iLCIDParam > cSrcParams)
        {
            ReportWarning(TLBX_E_BAD_SIGNATURE, TLBX_E_INVALIDLCIDPARAM);
            bValidLCID = FALSE;
        }

        // LCID's are not allowed on pure dispatch interfaces.
        if (ulIface == ifDispatch)
        {
            ReportWarning(TLBX_E_BAD_SIGNATURE, TLBX_E_LCIDONDISPONLYITF);
            bValidLCID = FALSE;
        }

        if (bValidLCID)
        {
            // Take the LCID parameter into account in the exported method.
            ++cDestParams;
        }
        else
        {
            // The LCID is invalid so we will ignore it.
            iLCIDParam = -1;
        }
    }

    // for each parameter
    pfunc->lprgelemdescParam = reinterpret_cast<ELEMDESC*>(sPool.AllocZero(cDestParams * sizeof(ELEMDESC)));
    if (pfunc->lprgelemdescParam == NULL)
        IfFailReport(E_OUTOFMEMORY);
    
    // Holds the allocated strings so we can deallocate on function exit.
    //  Only need +1 as we don't clean up the first and last names (function name and retval)
    NewArrayHolder<BSTRHolder> namesHolder = new BSTRHolder[cDestParams+1];
    
    // Variant array used to hold default value data
    NewArrayHolder<VariantPtrHolder> vtDefaultValues = new VariantPtrHolder[cDestParams];

    pfunc->cParams = static_cast<short>(cDestParams);
    for (iSrcParam=1, iDestParam=0; iDestParam<cDestParams; ++iSrcParam, ++iDestParam)
    {   
        // Check to see if we need to insert the LCID param before the current param.
        if (iLCIDParam == iDestParam)
        {
            // Set the flags and the type of the parameter.
            pfunc->lprgelemdescParam[iDestParam].paramdesc.wParamFlags = PARAMFLAG_FIN | PARAMFLAG_FLCID;
            pfunc->lprgelemdescParam[iDestParam].tdesc.vt = VT_I4;

            // Generate a parameter name.
            sName.Printf(szParamName, iDestParam + 1);

            rNames[iDestParam + 1] = SysAllocString(sName.GetUnicode());
            if (rNames[iDestParam + 1] == NULL)
                IfFailReport(E_OUTOFMEMORY);

            namesHolder[iDestParam+1] = rNames[iDestParam + 1];
            
            ++cNames;

            // Increment the current destination parameter.
            ++iDestParam;
        }

        // If we are past the end of the source parameters then we are done.
        if (iSrcParam > cSrcParams)
            break;

        // Get additional parameter metadata.
        dwParamFlags = 0;
        sName.Clear();

        // Error reporting info.
        m_ErrorContext.m_ixParam = iSrcParam;
        
        // See if there is a ParamDef for this param.
        hr = pInternalImport->FindParamOfMethod(pMeth->GetMemberDef(), iSrcParam, &pdParam);

        pvNativeType = NULL;
        if (hr == S_OK)
        {   
            // Get info about the param.        
            IfFailReport(pInternalImport->GetParamDefProps(pdParam, &iSrcParam, &dwParamFlags, &pszName));
            
            // Error reporting info.
            m_ErrorContext.m_szParam = pszName;
            
            // Turn off reserved (internal use) bits.
            dwParamFlags &= ~pdReservedMask;

            // Convert name from UTF8 to unicode.
            sName.SetUTF8(pszName);

            // Param default value, if any.
            IfFailReport(pInternalImport->GetDefaultValue(pdParam, &defaultValue));
            IfFailReport(_FillVariant(&defaultValue, &pParamDesc->varDefaultValue));

            // If no default value, check for decimal custom attribute.
            if (pParamDesc->varDefaultValue.vt == VT_EMPTY)
            {
                IfFailReport(pMT->GetMDImport()->GetCustomAttributeByName(pdParam, INTEROP_DECIMALVALUE_TYPE,  &pvData,&cbData));
                if (hr == S_OK && cbData >= (2 + sizeof(BYTE)+sizeof(BYTE)+sizeof(UINT)+sizeof(UINT)+sizeof(UINT)))
                {
                    const BYTE *pbData = (const BYTE *)pvData;
                    pParamDesc->varDefaultValue.vt = VT_DECIMAL;
                    pParamDesc->varDefaultValue.decVal.scale = *(BYTE*)(pbData+2);
                    pParamDesc->varDefaultValue.decVal.sign= *(BYTE*)(pbData+3);
                    pParamDesc->varDefaultValue.decVal.Hi32= GET_UNALIGNED_32(pbData+4);
                    pParamDesc->varDefaultValue.decVal.Mid32= GET_UNALIGNED_32(pbData+8);
                    pParamDesc->varDefaultValue.decVal.Lo32= GET_UNALIGNED_32(pbData+12);
                }
            }
            // If still no default value, check for date time custom attribute.
            if (pParamDesc->varDefaultValue.vt == VT_EMPTY)
            {
                IfFailReport(pMT->GetMDImport()->GetCustomAttributeByName(pdParam, INTEROP_DATETIMEVALUE_TYPE,  &pvData,&cbData));
                if (hr == S_OK && cbData >= (2 + sizeof(__int64)))
                {
                    const BYTE *pbData = (const BYTE *)pvData;
                    pParamDesc->varDefaultValue.vt = VT_DATE;
                    pParamDesc->varDefaultValue.date = _TicksToDoubleDate(GET_UNALIGNED_64(pbData+2));
                }
            }
            // If still no default value, check for IDispatch custom attribute.
            if (pParamDesc->varDefaultValue.vt == VT_EMPTY)
            {
                IfFailReport(pMT->GetMDImport()->GetCustomAttributeByName(pdParam, INTEROP_IDISPATCHVALUE_TYPE,  &pvData,&cbData));
                if (hr == S_OK)
                {
                    pParamDesc->varDefaultValue.vt = VT_DISPATCH;
                    pParamDesc->varDefaultValue.pdispVal = 0;
                }
            }
            // If still no default value, check for IUnknown custom attribute.
            if (pParamDesc->varDefaultValue.vt == VT_EMPTY)
            {
                IfFailReport(pMT->GetMDImport()->GetCustomAttributeByName(pdParam, INTEROP_IUNKNOWNVALUE_TYPE,  &pvData,&cbData));
                if (hr == S_OK)
                {
                    pParamDesc->varDefaultValue.vt = VT_UNKNOWN;
                    pParamDesc->varDefaultValue.punkVal = 0;
                }
            }
            
            if (pParamDesc->varDefaultValue.vt != VT_EMPTY)
            {
                // Copy the variant into the holder object so we release on function exit.
                vtDefaultValues[iDestParam] = (VARIANT*)&pParamDesc->varDefaultValue;
                
                pfunc->lprgelemdescParam[iDestParam].paramdesc.pparamdescex = pParamDesc;
                dwParamFlags |= PARAMFLAG_FHASDEFAULT;

                // Allocate another paramdesc.
                pParamDesc = reinterpret_cast<PARAMDESCEX*>(sVariants.AllocZero(sizeof(PARAMDESCEX)));
                if (pParamDesc == NULL)
                    IfFailReport(E_OUTOFMEMORY);
                
                bHasOptorDefault = true;
            }

            // native marshal type, if any.
            hr = pInternalImport->GetFieldMarshal(pdParam, &pvNativeType, &cbNativeType);
            if (hr != CLDB_E_RECORD_NOTFOUND)
            {
                IfFailReport(hr);
            }
            
            // Remember if there are optional params.
            if (dwParamFlags & PARAMFLAG_FOPT)
                bHasOptorDefault = true;
        }
        else
        {
            pdParam = 0, m_ErrorContext.m_szParam = 0;
        }
        
        // Do we need a name for this parameter?
        if ((pfunc->invkind & (INVOKE_PROPERTYPUT | INVOKE_PROPERTYPUTREF)) == 0 ||
            iSrcParam < cSrcParams)
        {
            // Yes, so make one up if we don't have one.
            if (sName.GetCount() == 0) 
            {
                sName.Printf(szParamName, iDestParam + 1);
            }

            rNames[iDestParam + 1] = SysAllocString(sName.GetUnicode());
            if (rNames[iDestParam + 1] == NULL)
                IfFailReport(E_OUTOFMEMORY);

            namesHolder[iDestParam+1] = rNames[iDestParam + 1];
            
            ++cNames;
        }

        // Save the element type.
        CorSigUncompressData(&pbSig[ixSig], &elem);
        
        // Convert the param info to elemdesc.
        bByRef = FALSE;
        hr = CorSigToTypeDesc(pCTI, pMT, &pbSig[ixSig], pvNativeType, cbNativeType, &cbElem,
                            &pfunc->lprgelemdescParam[iDestParam].tdesc, &sPool, TRUE, &bByRef);
        if (FAILED(hr))
            return FALSE;
        
        ixSig += cbElem;

        // If there is no [in,out], set one, based on the parameter.
        if ((dwParamFlags & (PARAMFLAG_FOUT | PARAMFLAG_FIN)) == 0)
        {
            // If param is by reference, make in/out
            if (bByRef)
                dwParamFlags |= PARAMFLAG_FIN | PARAMFLAG_FOUT;
            else
                dwParamFlags |= PARAMFLAG_FIN;
        }

        // If this is the last param, and it an array of objects, and has a ParamArrayAttribute,
        //  the function is varargs.
        if ((iSrcParam == cSrcParams) && !IsNilToken(pdParam) && !bHasOptorDefault) 
        {
            if (pfunc->lprgelemdescParam[iDestParam].tdesc.vt == VT_SAFEARRAY &&
                pfunc->lprgelemdescParam[iDestParam].tdesc.lpadesc->tdescElem.vt == VT_VARIANT)
            {
                if (pInternalImport->GetCustomAttributeByName(pdParam, INTEROP_PARAMARRAY_TYPE, 0,0) == S_OK)
                    pfunc->cParamsOpt = -1;
            }
        }
        
        pfunc->lprgelemdescParam[iDestParam].paramdesc.wParamFlags = static_cast<USHORT>(dwParamFlags);
    }

    // Is there a [retval]?
    if (pRetVal)
    {
        // Error reporting info.
        m_ErrorContext.m_ixParam = 0;
        m_ErrorContext.m_szParam = 0;
        
        _ASSERTE(bHrMunge);
        _ASSERTE(cDestParams > cSrcParams);
        pfunc->lprgelemdescParam[cDestParams-1].tdesc.vt = VT_PTR;
        pfunc->lprgelemdescParam[cDestParams-1].tdesc.lptdesc = pRetVal;
        pfunc->lprgelemdescParam[cDestParams-1].paramdesc.wParamFlags = PARAMFLAG_FOUT | PARAMFLAG_FRETVAL;

        // no need to allocate a new string for this.  rather use the constant szRetVal
        rNames[cDestParams] = (LPWSTR)szRetVal;

        ++cNames;
    }

    // Error reporting info.
    m_ErrorContext.m_ixParam = -1;
    
    // Was there a signature error?  If so, exit now that all sigs have been reported.
    IfFailReport(hrSignature);
    
    IfFailReport(pCTI->AddFuncDesc(iMD, pfunc));

    IfFailReport(pCTI->SetFuncAndParamNames(iMD, rNames.Ptr(), cNames));

    if (pProps->bFunction2Getter)
    {
        VARIANT vtOne;
        vtOne.vt = VT_I4;
        vtOne.lVal = 1;
        IfFailReport(pCTI->SetFuncCustData(iMD, GUID_Function2Getter, &vtOne));
    }

    // If the managed name of the method is different from the unmanaged name, then
    // we need to capture the managed name in a custom value. We only apply this
    // attribute for methods since properties cannot be overloaded.
    if (pProps->semantic == 0)
    {
        sName.SetUTF8(pMeth->GetName());
        if (sName.Compare(SString(pProps->pName)) != 0)
        {
            V_VT(&vtManagedName) = VT_BSTR;

            if (NULL == (V_BSTR(&vtManagedName) = SysAllocString(sName.GetUnicode())))
                IfFailReport(E_OUTOFMEMORY);

            IfFailReport(pCTI->SetFuncCustData(iMD, GUID_ManagedName, &vtManagedName));
        }
    }

    // Check for a description.
    if(GetDescriptionString(pMT, pMeth->GetMemberDef(), (BSTR &)bstrDescr))
        IfFailReport(pCTI->SetFuncDocString(iMD, bstrDescr));
    

    // Error reporting info.
    m_ErrorContext.m_szMember = 0;
    m_ErrorContext.m_szParam = 0;
    m_ErrorContext.m_ixParam = -1;

    return TRUE;
} // void TypeLibExporter::ConvertMethod()
#ifdef _PREFAST_
#pragma warning(pop)
#endif
    
//*****************************************************************************
// Export a Field as getter/setter method's to a typelib.
//*****************************************************************************
BOOL TypeLibExporter::ConvertFieldAsMethod(
    ICreateTypeInfo2 *pCTI,             // ICreateTypeInfo2 to get the method.
    ComMTMethodProps *pProps,           // Some properties of the method.
    ULONG       iMD)                    // Index of the member
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(pCTI));
        PRECONDITION(CheckPointer(pProps));
    }
    CONTRACTL_END;

    HRESULT     hr = S_OK;              // A result.
    PCCOR_SIGNATURE pbSig;              // Pointer to Cor signature.
    ULONG       cbSig;                  // Size of Cor signature.
    ULONG       ixSig;                  // Index into signature.
    ULONG       cbElem;                 // Size of an element in the signature.

    ULONG       callconv;               // A member's calling convention.
    TYPEDESC    *pType;                 // TYPEDESC for the field type.
    CDescPool   sPool;                  // Pool of memory in which to build funcdesc.
    BSTR        rNames[2];              // Array of names to function and parameters.
    ULONG       cNames;                 // Count of function and parameter names.
    FUNCDESC    *pfunc;                 // A funcdesc.
    ComCallMethodDesc   *pFieldMeth;    // A MethodDesc for a field call.
    FieldDesc   *pField;                // A FieldDesc.
    IMDInternalImport *pInternalImport; // Internal interface containing the field.
    PCCOR_SIGNATURE pvNativeType;       // native field type
    ULONG           cbNativeType;       // native field type length
    MethodTable     *pMT;                // Class containing the field.
    BSTRHolder  bstrDescr=0;            // Description of the method.

    // Get info about the method.
    pFieldMeth = reinterpret_cast<ComCallMethodDesc*>(pProps->pMeth);
    pField = pFieldMeth->GetFieldDesc();
    pField->GetSig(&pbSig, &cbSig);
    pInternalImport = pField->GetMDImport();
    pMT = pField->GetEnclosingMethodTable();

    // Error reporting info.
    IfFailReport(pMT->GetMDImport()->GetNameOfFieldDef(pField->GetMemberDef(), &m_ErrorContext.m_szMember));
    
    // Prepare to parse signature and build the FUNCDESC.
    pfunc = reinterpret_cast<FUNCDESC*>(sPool.AllocZero(sizeof(FUNCDESC)));
    if (NULL == pfunc)
        IfFailReport(E_OUTOFMEMORY);
    ixSig = 0;

    // Get the calling convention.
    ixSig += CorSigUncompressData(&pbSig[ixSig], &callconv);
    _ASSERTE(callconv == IMAGE_CEE_CS_CALLCONV_FIELD);
    pfunc->callconv = CC_STDCALL;

    // vtable offset.
    pfunc->oVft = pProps->oVft;

    // Set some method properties.
    pfunc->memid = pProps->dispid;
    pfunc->funckind = FUNC_PUREVIRTUAL;

    // Set the invkind based on whether the function is an accessor.
    if ((pProps->semantic - FieldSemanticOffset) == msGetter)
        pfunc->invkind = INVOKE_PROPERTYGET;
    else if ((pProps->semantic - FieldSemanticOffset) == msSetter)
    {
        if (IsVbRefType(&pbSig[ixSig], pInternalImport))
            pfunc->invkind = INVOKE_PROPERTYPUTREF;
        else
            pfunc->invkind = INVOKE_PROPERTYPUT;
    }
    else
        _ASSERTE(!"Incorrect semantic in ConvertFieldAsMethod");

    // Name of the function.
    rNames[0] = pProps->pName;
    cNames = 1;

    // Return type is HRESULT.
    pfunc->elemdescFunc.tdesc.vt = VT_HRESULT;

    // Set up the one and only parameter.
    pfunc->lprgelemdescParam = reinterpret_cast<ELEMDESC*>(sPool.AllocZero(sizeof(ELEMDESC)));
    if (NULL == pfunc->lprgelemdescParam)
        IfFailReport(E_OUTOFMEMORY);
    pfunc->cParams = 1;

    // Do we need a name for the parameter?  If PROPERTYGET, we do.
    if (pfunc->invkind == INVOKE_PROPERTYGET)
    {
        // Yes, so make one up.
        rNames[1] = (WCHAR*)szRetVal;
        ++cNames;
    }

    // If Getter, convert param as ptr, otherwise convert directly.
    if (pfunc->invkind == INVOKE_PROPERTYGET)
    {
        pType = reinterpret_cast<TYPEDESC*>(sPool.AllocZero(sizeof(TYPEDESC)));
        if (NULL == pType)
            IfFailReport(E_OUTOFMEMORY);

        pfunc->lprgelemdescParam[0].tdesc.vt = VT_PTR;
        pfunc->lprgelemdescParam[0].tdesc.lptdesc = pType;
        pfunc->lprgelemdescParam[0].paramdesc.wParamFlags = PARAMFLAG_FOUT | PARAMFLAG_FRETVAL;
    }
    else
    {
        pType = &pfunc->lprgelemdescParam[0].tdesc;
        pfunc->lprgelemdescParam[0].paramdesc.wParamFlags = PARAMFLAG_FIN;
    }

    // Get native field type
    pvNativeType = NULL;
    hr = pInternalImport->GetFieldMarshal(
        pField->GetMemberDef(), 
        &pvNativeType, 
        &cbNativeType);
    if (hr != CLDB_E_RECORD_NOTFOUND)
    {
        IfFailReport(hr);
    }
    
    // Convert the field type to elemdesc.
    hr = CorSigToTypeDesc(pCTI, pMT, &pbSig[ixSig], pvNativeType, cbNativeType, &cbElem, pType, &sPool, TRUE);
    if (FAILED(hr))
        return FALSE;

    ixSig += cbElem;

    // It is unfortunate that we can not handle this better.  Fortunately
    //  this should be very rare.
    // This is a weird case - if we're getting a CARRAY, we cannot add
    // a VT_PTR in the sig, as it will cause the C getter to return an
    // array, which is bad.  So we omit the extra pointer, which at least
    // makes the compiler happy.
    if (pfunc->invkind == INVOKE_PROPERTYGET
        && pType->vt == VT_CARRAY)
    {
        pfunc->lprgelemdescParam[0].tdesc.vt = pType->vt;
        pfunc->lprgelemdescParam[0].tdesc.lptdesc = pType->lptdesc;
    }

    // A property put of an object should be a propertyputref
    if (pfunc->invkind == INVOKE_PROPERTYPUT &&
        (pType->vt == VT_UNKNOWN || pType->vt == VT_DISPATCH))
    {
        pfunc->invkind = INVOKE_PROPERTYPUTREF;
    }
    
    IfFailReport(pCTI->AddFuncDesc(iMD, pfunc));

    IfFailReport(pCTI->SetFuncAndParamNames(iMD, rNames, cNames));

    // Check for a description.
    if(GetDescriptionString(pMT, pField->GetMemberDef(), (BSTR &)bstrDescr))
        IfFailReport(pCTI->SetFuncDocString(iMD, bstrDescr));
    
    // Error reporting info.
    m_ErrorContext.m_szMember = 0;

    return TRUE;
} // void TypeLibExporter::ConvertFieldAsMethod()

//*****************************************************************************
// Export a variable's metadata to a typelib.
//*****************************************************************************
BOOL TypeLibExporter::ConvertVariable(
    ICreateTypeInfo2 *pCTI,             // ICreateTypeInfo2 to get the variable.
    MethodTable     *pMT,                // The class containing the variable.
    mdFieldDef  md,                     // The member definition.
    SString&    sName,                  // Name of the member.
    ULONG       iMD)                    // Index of the member
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(pCTI));
        PRECONDITION(CheckPointer(pMT));
    }
    CONTRACTL_END;

    HRESULT     hr = S_OK;              // A result.
    PCCOR_SIGNATURE pbSig;              // Pointer to Cor signature.
    ULONG       cbSig;                  // Size of Cor signature.
    ULONG       ixSig;                  // Index into signature.
    ULONG       cbElem;                 // Size of an element in the signature.
    DWORD       dwFlags;                // A member's flags.
    ULONG       callconv;               // A member's calling convention.
    MDDefaultValue defaultValue;        // default value
    ULONG       dispid=DISPID_UNKNOWN;  // The variable's dispid.
    CDescPool   sPool;                  // Pool of memory in which to build vardesc.
    VARDESC     *pvar;                  // A vardesc.
    PCCOR_SIGNATURE pvNativeType;       // native field type
    ULONG           cbNativeType;       // native field type length
    const void  *pvData;                // Pointer to a custom attribute.
    ULONG       cbData;                 // Size of custom attribute.
    LPWSTR      pSuffix;                // Pointer into the name.
    int         iSuffix = 0;            // Counter for suffix.
    BSTRHolder  bstrDescr=0;            // Description of the method.

    VARIANT       vtTemp;
    VariantPtrHolder vtVariant = &vtTemp;

    SafeVariantInit(vtVariant);

    // Error reporting info.
    IfFailReport(pMT->GetMDImport()->GetNameOfFieldDef(md, &m_ErrorContext.m_szMember));
    
    // Get info about the field.
    IfFailReport(pMT->GetMDImport()->GetDispIdOfMemberDef(md, &dispid));
    IfFailReport(pMT->GetMDImport()->GetFieldDefProps(md, &dwFlags));
    if (IsFdHasDefault(dwFlags))
    {
        IfFailReport(pMT->GetMDImport()->GetDefaultValue(md, &defaultValue));
        IfFailReport( _FillVariant(&defaultValue, vtVariant) ); 
    }

    // If exporting a non-public member of a struct, warn the user.
    if (!IsFdPublic(dwFlags) && !m_bWarnedOfNonPublic)
    {
        m_bWarnedOfNonPublic = TRUE;
        ReportWarning(TLBX_E_NONPUBLIC_FIELD, TLBX_E_NONPUBLIC_FIELD);
    }

    IfFailReport(pMT->GetMDImport()->GetSigOfFieldDef(md, &cbSig, &pbSig));
    
    // Prepare to parse signature and build the VARDESC.
    pvar = reinterpret_cast<VARDESC*>(sPool.AllocZero(sizeof(VARDESC)));
    if(pvar == NULL)
        IfFailReport(E_OUTOFMEMORY);
    ixSig = 0;

    // Get the calling convention.
    ixSig += CorSigUncompressData(&pbSig[ixSig], &callconv);
    _ASSERTE(callconv == IMAGE_CEE_CS_CALLCONV_FIELD);

    // Get native field type
    pvNativeType = NULL;
    hr = pMT->GetMDImport()->GetFieldMarshal(md, &pvNativeType, &cbNativeType);
    if (hr != CLDB_E_RECORD_NOTFOUND)
    {
        IfFailReport(hr);
    }
    
    // Convert the type to elemdesc.
    hr = CorSigToTypeDesc(pCTI, pMT, &pbSig[ixSig], pvNativeType, cbNativeType, &cbElem, &pvar->elemdescVar.tdesc, &sPool, FALSE);
    if (FAILED(hr))
        return FALSE;

    ixSig += cbElem;

    pvar->wVarFlags = 0;
    pvar->varkind = VAR_PERINSTANCE;
    pvar->memid = dispid;

    // Constant value.
    if (vtVariant->vt != VT_EMPTY)
        pvar->lpvarValue = vtVariant;
    else
    {
        IfFailReport(pMT->GetMDImport()->GetCustomAttributeByName(md, INTEROP_DECIMALVALUE_TYPE,  &pvData,&cbData));
        if (hr == S_OK && cbData >= (2 + sizeof(BYTE)+sizeof(BYTE)+sizeof(UINT)+sizeof(UINT)+sizeof(UINT)))
        {
            const BYTE *pbData = (const BYTE *)pvData;
            vtVariant->vt = VT_DECIMAL;
            vtVariant->decVal.scale = *(BYTE*)(pbData+2);
            vtVariant->decVal.sign= *(BYTE*)(pbData+3);
            vtVariant->decVal.Hi32= GET_UNALIGNED_32(pbData+4);
            vtVariant->decVal.Mid32= GET_UNALIGNED_32(pbData+8);
            vtVariant->decVal.Lo32= GET_UNALIGNED_32(pbData+12);
            pvar->lpvarValue = vtVariant;
        }
        // If still no default value, check for date time custom attribute.
        if (vtVariant->vt == VT_EMPTY)
        {
            IfFailReport(pMT->GetMDImport()->GetCustomAttributeByName(md, INTEROP_DATETIMEVALUE_TYPE,  &pvData,&cbData));
            if (hr == S_OK && cbData >= (2 + sizeof(__int64)))
            {
                const BYTE *pbData = (const BYTE *)pvData;
                vtVariant->vt = VT_DATE;
                vtVariant->date = _TicksToDoubleDate(GET_UNALIGNED_64(pbData+2));
            }
        }
        // If still no default value, check for IDispatch custom attribute.
        if (vtVariant->vt == VT_EMPTY)
        {
            IfFailReport(pMT->GetMDImport()->GetCustomAttributeByName(md, INTEROP_IDISPATCHVALUE_TYPE,  &pvData,&cbData));
            if (hr == S_OK)
            {
                vtVariant->vt = VT_DISPATCH;
                vtVariant->pdispVal = 0;
            }
        }
        // If still no default value, check for IUnknown custom attribute.
        if (vtVariant->vt == VT_EMPTY)
        {
            IfFailReport(pMT->GetMDImport()->GetCustomAttributeByName(md, INTEROP_IUNKNOWNVALUE_TYPE,  &pvData,&cbData));
            if (hr == S_OK)
            {
                vtVariant->vt = VT_UNKNOWN;
                vtVariant->punkVal = 0;
            }
        }
    }

    IfFailReport(pCTI->AddVarDesc(iMD, pvar));
    
    // Set the name for the member; decorate if necessary.
    pSuffix = 0;
    for (;;)
    {
        // Attempt to set the name.
        hr = pCTI->SetVarName(iMD, (LPOLESTR)sName.GetUnicode());
        
        // If a name conflict, decorate, otherwise, done.
        if (hr != TYPE_E_AMBIGUOUSNAME)
            break;

        if (iSuffix == 0)
        {
            iSuffix = 2;
        }
        else
        {
            sName.Delete(sName.End()-=2, 2);
        }

        SString sDup;
        sDup.Printf(szDuplicateDecoration, iSuffix++);
        
        sName.Append(sDup);
    }
    IfFailReport(hr);

    // Check for a description.
    if(GetDescriptionString(pMT, md, (BSTR &)bstrDescr))
        IfFailReport(pCTI->SetVarDocString(iMD, bstrDescr));
    
    // Error reporting info.
    m_ErrorContext.m_szMember = 0;

    return TRUE;
} // HRESULT TypeLibExporter::ConvertVariable()

//*****************************************************************************
// Export a variable's metadata to a typelib.
//*****************************************************************************
BOOL TypeLibExporter::ConvertEnumMember(
    ICreateTypeInfo2 *pCTI,              // ICreateTypeInfo2 to get the variable.
    MethodTable     *pMT,                // The Class containing the member.
    mdFieldDef  md,                     // The member definition.
    SString&    sName,                  // Name of the member.
    ULONG       iMD)                    // Index of the member
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(pCTI));
        PRECONDITION(CheckPointer(pMT));
    }
    CONTRACTL_END;

    HRESULT     hr = S_OK;              // A result.
    LPCUTF8     pName, pNS;             // To format name.
    DWORD       dwFlags;                // A member's flags.
    VARIANT     vtVariant;              // A Variant.
    MDDefaultValue defaultValue;        // default value
    ULONG       dispid=DISPID_UNKNOWN;  // The variable's dispid.
    CDescPool   sPool;                  // Pool of memory in which to build vardesc.
    VARDESC     *pvar;                  // A vardesc.
    BSTRHolder  bstrDescr=0;            // Description of the method.

    vtVariant.vt = VT_EMPTY;

    // Error reporting info.
    IfFailReport(pMT->GetMDImport()->GetNameOfFieldDef(md, &m_ErrorContext.m_szMember));
    
    // Get info about the field.
    IfFailReport(pMT->GetMDImport()->GetDispIdOfMemberDef(md, &dispid));
    IfFailReport(pMT->GetMDImport()->GetFieldDefProps(md, &dwFlags));
    
    // We do not need to handle decimal's here since enum's can only be integral types.
    IfFailReport(pMT->GetMDImport()->GetDefaultValue(md, &defaultValue));

    // Prepare to parse signature and build the VARDESC.
    pvar = reinterpret_cast<VARDESC*>(sPool.AllocZero(sizeof(VARDESC)));
    if (NULL == pvar)
        IfFailReport(E_OUTOFMEMORY);

    IfFailReport( _FillVariant(&defaultValue, &vtVariant) ); 

    // Don't care what the metadata says the type is -- the type is I4 in the typelib.
    pvar->elemdescVar.tdesc.vt = VT_I4;

    pvar->wVarFlags = 0;
    pvar->varkind = VAR_CONST;
    pvar->memid = dispid;

    // Constant value.
    if (vtVariant.vt != VT_EMPTY)
    {
        pvar->lpvarValue = &vtVariant;
        
        // If this is an I8 or UI8, do the conversion manually, because some 
        //  systems' oleaut32 don't support 64-bit integers.
        if (vtVariant.vt == VT_I8)
        {  
            // If withing range of 32-bit signed number, OK.
            if (vtVariant.llVal <= LONG_MAX && vtVariant.llVal >= LONG_MIN)
                vtVariant.vt = VT_I4, hr = S_OK;
            else
                hr = E_FAIL;
        }
        else if (vtVariant.vt == VT_UI8)
        {
            // If withing range of 32-bit unsigned number, OK.
            if (vtVariant.ullVal <= ULONG_MAX)
                vtVariant.vt = VT_UI4, hr = S_OK;
            else
                hr = E_FAIL;
        }
        else
        {
            hr = SafeVariantChangeTypeEx(&vtVariant, &vtVariant, 0, 0, VT_I4);
        }
        
        if (FAILED(hr))
        {
            if (FAILED(pMT->GetMDImport()->GetNameOfTypeDef(pMT->GetCl(), &pName, &pNS)))
            {
                pName = pNS = "Invalid TypeDef record";
            }
            ReportWarning(TLBX_W_ENUM_VALUE_TOOBIG, TLBX_W_ENUM_VALUE_TOOBIG, pName, sName.GetUnicode());
            return FALSE;
        }
    }
    else
    {   // No value assigned, use 0.
        pvar->lpvarValue = &vtVariant;
        vtVariant.vt = VT_I4;
        vtVariant.lVal = 0;
    }

    IfFailReport(pCTI->AddVarDesc(iMD, pvar));
    IfFailReport(pCTI->SetVarName(iMD, (LPOLESTR)sName.GetUnicode()));

    // Check for a description.
    if(GetDescriptionString(pMT, md, (BSTR &)bstrDescr))
        IfFailReport(pCTI->SetVarDocString(iMD, bstrDescr));
    
    // Error reporting info.
    m_ErrorContext.m_szMember = 0;

    return TRUE;
} // void TypeLibExporter::ConvertEnumMember()

//*****************************************************************************
// Given a COM+ signature of a field or property, determine if it should
//  be a PROPERTYPUT or PROPERTYPUTREF.
//*****************************************************************************
BOOL TypeLibExporter::IsVbRefType(
    PCCOR_SIGNATURE pbSig,
    IMDInternalImport *pInternalImport)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(pInternalImport));
    }
    CONTRACTL_END;

    ULONG       elem=0;                 // An element from a COM+ signature.
    ULONG       cbElem=0;

    cbElem = CorSigUncompressData(pbSig, &elem);
    if (elem == ELEMENT_TYPE_PTR || elem == ELEMENT_TYPE_BYREF)
    {
        return IsVbRefType(&pbSig[cbElem], pInternalImport);
    }
    else
    {
        switch (elem)
        {
            // For documentation -- arrays are NOT ref types here.
            //case ELEMENT_TYPE_SDARRAY:
            //case ELEMENT_TYPE_ARRAY:
            //case ELEMENT_TYPE_SZARRAY:
            // Look for variant.
            case ELEMENT_TYPE_VALUETYPE:
                return FALSE;

            case ELEMENT_TYPE_CLASS:
                return TRUE;
                
            case ELEMENT_TYPE_OBJECT:
                return FALSE;

            default:
                break;
        }
    }

    return FALSE;
} // BOOL TypeLibExporter::IsVbRefType()

BOOL TypeLibExporter::IsExportingAs64Bit()
{  
    LIMITED_METHOD_CONTRACT;
    if (TlbExportAs64Bit(m_flags))
    {
         return TRUE;
    }
    else if (TlbExportAs32Bit(m_flags))
    {
        return FALSE;
    }
    else
    {
#ifdef _WIN64
        return TRUE;
#else
        return FALSE;
#endif
    }
} // BOOL TypeLibExporter::IsExportingAs64Bit()

void TypeLibExporter::ArrayToTypeDesc(ICreateTypeInfo2 *pCTI, CDescPool *ppool, ArrayMarshalInfo *pArrayMarshalInfo, TYPEDESC *ptdesc)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(pCTI));
        PRECONDITION(CheckPointer(ppool));
        PRECONDITION(CheckPointer(pArrayMarshalInfo));
        PRECONDITION(CheckPointer(ptdesc));
    }
    CONTRACTL_END;

    HRESULT hr = E_FAIL;
    VARTYPE vtElement = pArrayMarshalInfo->GetElementVT();
    TypeHandle thElement = pArrayMarshalInfo->GetElementTypeHandle();

    if (vtElement == VT_RECORD)
    {
        // We are dealing with an array of embedded structures.
        ptdesc->vt = VT_USERDEFINED;                                
        EEClassToHref(pCTI, thElement.GetMethodTable(), FALSE, &ptdesc->hreftype);
    }
    else if ((vtElement == VT_UNKNOWN || vtElement == VT_DISPATCH) && !thElement.IsObjectType())
    {
        if (!thElement.IsValueType() && !pArrayMarshalInfo->IsSafeArraySubTypeExplicitlySpecified())
        {
            // We are dealing with an array of user defined interfaces.
            ptdesc->vt = VT_PTR;
            ptdesc->lptdesc = reinterpret_cast<TYPEDESC*>(ppool->AllocZero(sizeof(TYPEDESC)));
            if (ptdesc->lptdesc == NULL)
                IfFailReport(E_OUTOFMEMORY);
                    
            ptdesc->lptdesc->vt = VT_USERDEFINED;
            EEClassToHref(pCTI, thElement.GetMethodTable(), FALSE, &ptdesc->lptdesc->hreftype);        
        }
        else
        {
            // The user specified that the array of value classes be converted to an 
            // array of IUnknown or IDispatch pointers. 
            ptdesc->vt = vtElement;
        }
    }
    else if (pArrayMarshalInfo->IsPtr())
    {
        ptdesc->vt = VT_PTR;
        ptdesc->lptdesc = reinterpret_cast<TYPEDESC*>(ppool->AllocZero(sizeof(TYPEDESC)));
        if (ptdesc->lptdesc == NULL)
            IfFailReport(E_OUTOFMEMORY);

        ptdesc->lptdesc->vt = vtElement;
    }
    else
    {
        // We are dealing with an array of primitive types.
        ptdesc->vt = vtElement;
    }
}
// HRESULT ArrayToTypeDesc(ArrayMarshalInfo *pArrayMarshalInfo, TYPEDESC *pElementTypeDesc)

VARTYPE TypeLibExporter::GetVtForIntPtr()
{
    WRAPPER_NO_CONTRACT;

    return static_cast<VARTYPE>(IsExportingAs64Bit() ? VT_I8 : VT_I4);
} // VARTYPE TypeLibExporter::GetVtForIntPtr()

VARTYPE TypeLibExporter::GetVtForUIntPtr()
{
    WRAPPER_NO_CONTRACT;

    return static_cast<VARTYPE>(IsExportingAs64Bit() ? VT_UI8 : VT_UI4);
} // VARTYPE TypeLibExporter::GetVtForUIntPtr()

/*
BOOL TypeLibExporter::ValidateSafeArrayElemVT(VARTYPE vt)
{
    switch(vt)
    {
        case VT_I2:
        case VT_I4:
        case VT_R4:
        case VT_R8:
        case VT_CY:
        case VT_DATE:
        case VT_BSTR:
        case VT_DISPATCH:
        case VT_ERROR:
        case VT_BOOL:
        case VT_VARIANT:
        case VT_UNKNOWN:
        case VT_DECIMAL:
        case VT_RECORD:
        case VT_I1:
        case VT_UI1:
        case VT_UI2:
        case VT_UI4:
        case VT_INT:
        case VT_UINT:
            return TRUE;

        default:
            return FALSE;
    }
}
*/

//*****************************************************************************
// Read a COM+ signature element and create a TYPEDESC that corresponds 
//  to it.
//*****************************************************************************
#ifdef _PREFAST_
#pragma warning(push)
#pragma warning(disable:21000) // Suppress PREFast warning about overly large function
#endif
HRESULT TypeLibExporter::CorSigToTypeDesc(
    ICreateTypeInfo2 *pCTI,              // Typeinfo being created.
    MethodTable     *pMT,                // MethodTable with the token.
    PCCOR_SIGNATURE pbSig,              // Pointer to the Cor Signature.
    PCCOR_SIGNATURE pbNativeSig,        // Pointer to the native sig, if any
    ULONG       cbNativeSig,            // Count of bytes in native sig.
    ULONG       *pcbElem,               // Put # bytes consumed here.
    TYPEDESC    *ptdesc,                // Build the typedesc here.
    CDescPool   *ppool,                 // Pool for additional storage as required.
    BOOL        bMethodSig,             // TRUE if the sig is for a method, FALSE for a field.
    BOOL        *pbByRef)               // If not null, and the type is byref, set to true.
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(pCTI));
        PRECONDITION(CheckPointer(pMT));
        PRECONDITION(CheckPointer(pcbElem));
        PRECONDITION(CheckPointer(ptdesc));
        PRECONDITION(CheckPointer(ppool));
        PRECONDITION(CheckPointer(pbByRef, NULL_OK));
    }
    CONTRACTL_END;

    HRESULT     hr=S_OK;
    ULONG       elem = 0;               // The element type.
    ULONG       cbElem = 0;             // Bytes in the element.
    ULONG       cb;                     // Bytes in a sub-element.
    ULONG       cbNativeElem = 0;       // # of bytes parsed off of native type.
    ULONG       nativeElem = 0;         // The native element type
    ULONG       nativeCount;            // The native element size
    mdToken     tkTypeRef;              // Token for a TypeRef/TypeDef
    SString     sName;                  // Buffer to build a name from NS/Name.
    LPCUTF8     pclsname;               // Class name for ELEMENT_TYPE_CLASS.
    HREFTYPE    hRef = 0;               // HREF to some type.
    IMDInternalImport *pInternalImport; // Internal interface containing the signature.
    Module*     pModule = NULL;         // Module containing the signature.
    int         i;                      // Loop control.
    SigTypeContext emptyTypeContext;    // an empty type context is sufficient: all methods should be non-generic
    ULONG       dwTypeFlags = 0;        // The type flags.
    BOOL        fAnsi = FALSE;          // Is the structure marked as CharSet=Ansi.
    BOOL        fIsStringBuilder = FALSE;
    LPCUTF8     pNS;


    pInternalImport = pMT->GetMDImport();
    pModule = pMT->GetModule();

    // Just be sure the count is zero if the pointer is.
    if (pbNativeSig == NULL)
        cbNativeSig = 0;

    // Grab the native marshaling type.
    if (cbNativeSig > 0)
    {
        cbNativeElem = CorSigUncompressData(pbNativeSig, &nativeElem);
        pbNativeSig += cbNativeElem;
        cbNativeSig -= cbNativeElem;

        // AsAny makes no sense for COM Interop.  Ignore it.
        if (nativeElem == NATIVE_TYPE_ASANY)
        {
            ReportWarning(TLBX_E_BAD_SIGNATURE, TLBX_E_ASANY);
            nativeElem = 0;
        }
    }

    // If we are dealing with a struct, determine if it is marked as CharSet=Ansi.
    if (!bMethodSig)
    {
        // Make sure one of Auto, Ansi or Unicode is specified.
        if (!IsTdAnsiClass(dwTypeFlags) && !IsTdAutoClass(dwTypeFlags) && !IsTdUnicodeClass(dwTypeFlags))
        {
            _ASSERTE(!"Bad stringformat value in wrapper class.");
            ReportWarning(TLBX_E_BAD_SIGNATURE, E_FAIL);  // bad metadata
            hr = TLBX_E_BAD_SIGNATURE;
            goto ExitFunc;
        }
        
        if (FAILED(pInternalImport->GetTypeDefProps(pMT->GetCl(), &dwTypeFlags, NULL)))
        {
            ReportWarning(TLBX_E_BAD_SIGNATURE, E_FAIL);
            hr = TLBX_E_BAD_SIGNATURE;
            goto ExitFunc;
        }
        fAnsi = IsTdAnsiClass(dwTypeFlags);
    }
    
    // Get the element type.
TryAgain:
    cbElem += CorSigUncompressData(pbSig+cbElem, &elem);

    // Handle the custom marshaler native type separately.
    if (elem != ELEMENT_TYPE_BYREF && nativeElem == NATIVE_TYPE_CUSTOMMARSHALER)
    {
        switch(elem)
        {
            case ELEMENT_TYPE_VAR:
            case ELEMENT_TYPE_CLASS:
            case ELEMENT_TYPE_OBJECT:
                // @TODO(DM): Ask the custom marshaler for the ITypeInfo to use for the unmanaged type.
                ptdesc->vt = VT_UNKNOWN;
                break;

            case ELEMENT_TYPE_STRING:
            case ELEMENT_TYPE_SZARRAY:
            case ELEMENT_TYPE_ARRAY:
                ptdesc->vt = GetVtForIntPtr();
                break;

            default:
                ReportWarning(TLBX_E_BAD_SIGNATURE, TLBX_E_BAD_NATIVETYPE);
                return(TLBX_E_BAD_SIGNATURE);
            break;
        }

        // Eat the rest of the signature.  The extra -1's are to account
        // for the byte parsed off above.
        SigPointer p(&pbSig[cbElem-1]);
        IfFailThrow(p.SkipExactlyOne());
        cbElem += (ULONG)(p.GetPtr() - &pbSig[cbElem]);  // Note I didn't use -1 here.
        goto ExitFunc;
    }

// This label is used to try again with a new element type, but without consuming more signature.
//  Usage is to set 'elem' to a new value, goto this label.
TryWithElemType:
    switch (elem)
    {
    case ELEMENT_TYPE_END:            // 0x0,
            ReportWarning(TLBX_E_BAD_SIGNATURE, TLBX_E_UNKNOWN_SIGNATURE);
            return(TLBX_E_BAD_SIGNATURE);
        break;
            
    case ELEMENT_TYPE_VOID:           // 0x1,
        ptdesc->vt = VT_VOID;  
        break;

    case ELEMENT_TYPE_BOOLEAN:        // 0x2,
        switch (nativeElem)
        {
        case 0:
            ptdesc->vt = static_cast<VARTYPE>(bMethodSig ? VT_BOOL : VT_I4);
            break;

        case NATIVE_TYPE_VARIANTBOOL:
            ptdesc->vt = VT_BOOL;
            break;

        case NATIVE_TYPE_BOOLEAN:
            ptdesc->vt = VT_I4;
            break;

        case NATIVE_TYPE_U1:
        case NATIVE_TYPE_I1:
            ptdesc->vt = VT_UI1;
            break;
                    
        default:
            DEBUG_STMT(DbgWriteEx(W("Bad Native COM attribute specified!\n")));
                    ReportWarning(TLBX_E_BAD_SIGNATURE, TLBX_E_BAD_NATIVETYPE);
                    return(TLBX_E_BAD_SIGNATURE);
        }   
        break;
            
    case ELEMENT_TYPE_CHAR:           // 0x3,
        if (nativeElem == 0)
        {
            if (!bMethodSig && IsTdAutoClass(dwTypeFlags))
            {
                // Types with a char set of auto and that would be represented differently
                // on different platforms are not allowed to be exported to COM.
                DefineFullyQualifiedNameForClassW();
                LPCWSTR szName = GetFullyQualifiedNameForClassW(pMT);
                _ASSERTE(szName);

                ReportWarning(TLBX_E_BAD_SIGNATURE, TLBX_E_AUTO_CS_NOT_ALLOWED, szName);
                hr = TLBX_E_BAD_SIGNATURE;
                goto ExitFunc;
            }

            ptdesc->vt = static_cast<VARTYPE>(fAnsi ? VT_UI1 : VT_UI2);
        }
        else
        {
            switch (nativeElem)
            {
            case 0:
            case NATIVE_TYPE_U2:
            case NATIVE_TYPE_I2:
                ptdesc->vt = VT_UI2;
                break;
                                
            case NATIVE_TYPE_U1:
            case NATIVE_TYPE_I1:
                ptdesc->vt = VT_UI1;
                break;
                                
            default:
                DEBUG_STMT(DbgWriteEx(W("Bad Native COM attribute specified!\n")));
                ReportWarning(TLBX_E_BAD_SIGNATURE, TLBX_E_BAD_NATIVETYPE);
                hr = TLBX_E_BAD_SIGNATURE;
                goto ExitFunc;
            }
        }
        break;

    case ELEMENT_TYPE_I1:             // 0x4,
        ptdesc->vt = VT_I1;
        break;
                
    case ELEMENT_TYPE_U1:             // 0x5,
        ptdesc->vt = VT_UI1;
        break;
                
    case ELEMENT_TYPE_I2:             // 0x6,
        ptdesc->vt = VT_I2;
        break;
                
    case ELEMENT_TYPE_U2:             // 0x7,
        ptdesc->vt = VT_UI2;
        break;
                
    case ELEMENT_TYPE_I4:             // 0x8,
        switch (nativeElem)
        {
        case 0:
        case NATIVE_TYPE_I4:
        case NATIVE_TYPE_U4: case NATIVE_TYPE_INTF: //@todo: Fix Microsoft.Win32.Interop.dll and remove this line.
            ptdesc->vt = VT_I4;
            break;
                    
        case NATIVE_TYPE_ERROR:
            ptdesc->vt = VT_HRESULT;
            break;
                            
        default:
            DEBUG_STMT(DbgWriteEx(W("Bad Native COM attribute specified!\n")));
            ReportWarning(TLBX_E_BAD_SIGNATURE, TLBX_E_BAD_NATIVETYPE);
            hr = TLBX_E_BAD_SIGNATURE;
            goto ExitFunc;
        }
        break;
            
    case ELEMENT_TYPE_U4:             // 0x9,
        switch (nativeElem)
        {
        case 0:
        case NATIVE_TYPE_U4:
            ptdesc->vt = VT_UI4;
            break;
                            
        case NATIVE_TYPE_ERROR:
            ptdesc->vt = VT_HRESULT;
            break;
                            
        default:
            DEBUG_STMT(DbgWriteEx(W("Bad Native COM attribute specified!\n")));
            ReportWarning(TLBX_E_BAD_SIGNATURE, TLBX_E_BAD_NATIVETYPE);
            hr = TLBX_E_BAD_SIGNATURE;
            goto ExitFunc;
        }
        break;
            
    case ELEMENT_TYPE_I8:             // 0xa,
        ptdesc->vt = VT_I8;
        break;
                
    case ELEMENT_TYPE_U8:             // 0xb,
        ptdesc->vt = VT_UI8;
        break;
                
    case ELEMENT_TYPE_R4:             // 0xc,
        ptdesc->vt = VT_R4;
        break;
                
    case ELEMENT_TYPE_R8:             // 0xd,
        ptdesc->vt = VT_R8;
        break;
                
    case ELEMENT_TYPE_OBJECT:
        goto IsObject;
            
    case ELEMENT_TYPE_STRING:         // 0xe,
    IsString:
        if (nativeElem == 0)
        {            
            if (bMethodSig)
            {
                ptdesc->vt = VT_BSTR;
            }
            else
            {
                if (IsTdAutoClass(dwTypeFlags))
                {
                    // Types with a char set of auto and that would be represented differently
                    // on different platforms are not allowed to be exported to COM.
                    DefineFullyQualifiedNameForClassW();
                    LPCWSTR szName = GetFullyQualifiedNameForClassW(pMT);
                    _ASSERTE(szName);

                    ReportWarning(TLBX_E_BAD_SIGNATURE, TLBX_E_AUTO_CS_NOT_ALLOWED, szName);
                    hr = TLBX_E_BAD_SIGNATURE;
                    goto ExitFunc;
                }

                ptdesc->vt = static_cast<VARTYPE>(fAnsi ? VT_LPSTR : VT_LPWSTR);
            }
        }
        else
        {
            switch (nativeElem)
            {
            case NATIVE_TYPE_BSTR:
                if (fIsStringBuilder)
                {
                    ReportWarning(TLBX_E_BAD_SIGNATURE, TLBX_E_BAD_NATIVETYPE);
                    hr = TLBX_E_BAD_SIGNATURE;
                    goto ExitFunc;
                }
                ptdesc->vt = VT_BSTR;
                break;
                        
            case NATIVE_TYPE_LPSTR:
                ptdesc->vt = VT_LPSTR;
                break;
                                
            case NATIVE_TYPE_LPWSTR:
                ptdesc->vt = VT_LPWSTR;
                break;
                                
            case NATIVE_TYPE_LPTSTR:
                {
                    // NATIVE_TYPE_LPTSTR is not allowed to be exported to COM.
                    DefineFullyQualifiedNameForClassW();
                    LPCWSTR szName = GetFullyQualifiedNameForClassW(pMT);
                    _ASSERTE(szName);
                    ReportWarning(TLBX_E_BAD_SIGNATURE, TLBX_E_LPTSTR_NOT_ALLOWED, szName);
                    hr = TLBX_E_BAD_SIGNATURE;
                    goto ExitFunc;
                }
            case NATIVE_TYPE_FIXEDSYSSTRING:
                // NATIVE_TYPE_FIXEDSYSSTRING is only allowed on fields.
                if (bMethodSig)
                {
                    ReportWarning(TLBX_E_BAD_SIGNATURE, TLBX_E_BAD_NATIVETYPE);
                    hr = TLBX_E_BAD_SIGNATURE;
                    goto ExitFunc;
                }

                // Retrieve the count of characters.
                if (cbNativeSig != 0)
                {
                    cb = CorSigUncompressData(pbNativeSig, &nativeCount);
                    pbNativeSig += cb;
                    cbNativeSig -= cb;
                }
                else
                {
                    nativeCount = 0;
                }

                // Fixed strings become embedded array's of characters.
                ptdesc->vt = VT_CARRAY;
                ptdesc->lpadesc = reinterpret_cast<ARRAYDESC*>(ppool->AllocZero(sizeof(ARRAYDESC)));
                if (ptdesc->lpadesc == NULL)
                    IfFailReport(E_OUTOFMEMORY);

                // Set the count of characters.
                ptdesc->lpadesc->cDims = 1;
                ptdesc->lpadesc->rgbounds[0].cElements = nativeCount;
                ptdesc->lpadesc->rgbounds[0].lLbound = 0;

                if (IsTdAutoClass(dwTypeFlags))
                {
                    // Types with a char set of auto and that would be represented differently
                    // on different platforms are not allowed to be exported to COM.
                    DefineFullyQualifiedNameForClassW();
                    LPCWSTR szName = GetFullyQualifiedNameForClassW(pMT);
                    _ASSERTE(szName);

                    ReportWarning(TLBX_E_BAD_SIGNATURE, TLBX_E_AUTO_CS_NOT_ALLOWED, szName);
                    hr = TLBX_E_BAD_SIGNATURE;
                    goto ExitFunc;
                }

                ptdesc->lpadesc->tdescElem.vt = static_cast<VARTYPE>(fAnsi ? VT_UI1 : VT_UI2);
                break;

            default:
                DEBUG_STMT(DbgWriteEx(W("Bad Native COM attribute specified!\n")));
                ReportWarning(TLBX_E_BAD_SIGNATURE, TLBX_E_BAD_NATIVETYPE);
                hr = TLBX_E_BAD_SIGNATURE;
                goto ExitFunc;
            }   
        }
        break;

    // every type above PTR will be simple type
    case ELEMENT_TYPE_PTR:            // 0xf,
    case ELEMENT_TYPE_BYREF:          // 0x10,
        // TYPEDESC is a pointer.
        ptdesc->vt = VT_PTR;
        if (pbByRef)
            *pbByRef = TRUE;
                
        // Pointer to what?
        ptdesc->lptdesc = reinterpret_cast<TYPEDESC*>(ppool->AllocZero(sizeof(TYPEDESC)));
        if (ptdesc->lptdesc == NULL)
            IfFailReport(E_OUTOFMEMORY);
            
        hr = CorSigToTypeDesc(pCTI, pMT, &pbSig[cbElem], pbNativeSig-cbNativeElem, 
                    cbNativeSig+cbNativeElem, &cb, ptdesc->lptdesc, ppool, bMethodSig);
        cbElem += cb;
            
        if (FAILED(hr))
            goto ExitFunc;
        
        break;

    case ELEMENT_TYPE_CLASS:          // 0x12,
    case ELEMENT_TYPE_VALUETYPE:
        // Get the TD/TR.
        cb = CorSigUncompressToken(&pbSig[cbElem], &tkTypeRef);
        cbElem += cb;
        
        if (TypeFromToken(tkTypeRef) == mdtTypeDef)
        {
            // Get the name of the TypeDef.
            if (FAILED(pInternalImport->GetNameOfTypeDef(tkTypeRef, &pclsname, &pNS)))
            {
                IfFailReport(COR_E_BADIMAGEFORMAT);
            }
        }
        else
        {
            // Get the name of the TypeRef.
            _ASSERTE(TypeFromToken(tkTypeRef) == mdtTypeRef);
            IfFailReport(pInternalImport->GetNameOfTypeRef(tkTypeRef, &pNS, &pclsname));
        }

        if (pNS)
        {
            sName.MakeFullNamespacePath(SString(SString::Utf8, pNS), SString(SString::Utf8, pclsname));
            StackScratchBuffer scratch;
            pclsname = sName.GetUTF8(scratch);
        }

        _ASSERTE(strlen(szRuntime) == cbRuntime);  // If you rename System, fix this invariant.
        _ASSERTE(strlen(szText) == cbText);  // If you rename System.Text, fix this invariant.

        // Is it System.something? 
        if (SString::_strnicmp(pclsname, szRuntime, cbRuntime) == 0)
        {   
            // Which one?
            LPCUTF8 pcls; pcls = pclsname + cbRuntime;
            if (stricmpUTF8(pcls, szStringClass) == 0)
            {
                goto IsString;
            }
            else if (stricmpUTF8(pcls, szDateTimeClass) == 0)
            {
                ptdesc->vt = VT_DATE;
                goto ExitFunc;
            }
            else if (stricmpUTF8(pcls, szDecimalClass) == 0)
            {
                switch (nativeElem)
                {
                    case NATIVE_TYPE_CURRENCY:
                        // Make this a currency.
                        ptdesc->vt = VT_CY;
                        break;
                                
                    case 0:
                        // Make this a decimal
                        ptdesc->vt = VT_DECIMAL;
                        break;
                                
                    default:
                        DEBUG_STMT(DbgWriteEx(W("Bad Native COM attribute specified!\n")));
                        ReportWarning(TLBX_E_BAD_SIGNATURE, TLBX_E_BAD_NATIVETYPE);
                        hr = TLBX_E_BAD_SIGNATURE;
                        goto ExitFunc;
                }
                goto ExitFunc;
            }
            else if (stricmpUTF8(pcls, szGuidClass) == 0)
            {
                switch (nativeElem)
                {
                    case NATIVE_TYPE_LPSTRUCT:
                        // Make this a pointer to . . .
                        ptdesc->vt = VT_PTR;
                        if (pbByRef)
                            *pbByRef = TRUE;
                                
                        ptdesc->lptdesc = reinterpret_cast<TYPEDESC*>(ppool->AllocZero(sizeof(TYPEDESC)));
                        if (ptdesc->lptdesc == NULL)
                            IfFailReport(E_OUTOFMEMORY);
                                
                        // . . . a user defined type for GUID
                        ptdesc->lptdesc->vt = VT_USERDEFINED;
                        GetRefTypeInfo(pCTI, m_pGuid, &ptdesc->lptdesc->hreftype);
                        break;
                                
                    case 0:
                    case NATIVE_TYPE_STRUCT:
                        // a user defined type for GUID
                        ptdesc->vt = VT_USERDEFINED;
                        GetRefTypeInfo(pCTI, m_pGuid, &ptdesc->hreftype);
                        break;
                                
                    default:
                        DEBUG_STMT(DbgWriteEx(W("Bad Native COM attribute specified!\n")));
                        ReportWarning(TLBX_E_BAD_SIGNATURE, TLBX_E_BAD_NATIVETYPE);
                        hr = TLBX_E_BAD_SIGNATURE;
                        goto ExitFunc;
                }
                goto ExitFunc;
            }
            else if (stricmpUTF8(pcls, szArrayClass) == 0)
            {
                // If no native type is specified then assume its a NATIVE_TYPE_INTF.
                if (nativeElem == 0)
                    nativeElem = NATIVE_TYPE_INTF;

                if (nativeElem == NATIVE_TYPE_SAFEARRAY)
                {
                    // Compat: If no safe array used def subtype was specified we will map it to a SAFEARRAY of VARIANTs.
                    ULONG vtElement = VT_VARIANT;
                    TypeHandle thElement = TypeHandle(g_pObjectClass);
                    
                    if (cbNativeSig > 0)
                    {
                        // Retrieve the safe array sub type.
                        cb = CorSigUncompressData(pbNativeSig, &vtElement);
                        pbNativeSig += cb;
                        cbNativeSig -= cb;    

                        // Get the type name if specified.
                        if (cbNativeSig > 0)
                        {
                            ULONG cbClass = 0;
                            
                            cb = CorSigUncompressData(pbNativeSig, &cbClass);
                            pbNativeSig += cb;
                            cbNativeSig -= cb;

                            if (cbClass > 0)
                            {
                                // Load the type. Use an SString for the string since we need to NULL terminate the string
                                // that comes from the metadata.
                                StackScratchBuffer utf8Name;
                                SString safeArrayUserDefTypeName(SString::Utf8, (LPUTF8)pbNativeSig, cbClass);
                                thElement = LoadClass(pMT->GetModule(), safeArrayUserDefTypeName.GetUTF8(utf8Name));
                            }
                        }                        
                    }
                    else
                    {
                        if (!bMethodSig)
                        {
                            // The field marshaller converts these to SAFEARRAYs of the type specified
                            // at runtime by the array. This isn't expressible in a type library 
                            // so provide a warning.
                            ReportWarning(TLBX_E_BAD_SIGNATURE, TLBX_W_BAD_SAFEARRAYFIELD_NO_ELEMENTVT);
                        }
                    }

                    ArrayMarshalInfo arrayMarshalInfo(IsExportingAs64Bit() ? amiExport64Bit : amiExport32Bit);
                    MarshalInfo::MarshalScenario ms = bMethodSig ? MarshalInfo::MARSHAL_SCENARIO_COMINTEROP : MarshalInfo::MARSHAL_SCENARIO_FIELD;
                    arrayMarshalInfo.InitForSafeArray(ms, thElement, (VARTYPE)vtElement, fAnsi);

                    if (!arrayMarshalInfo.IsValid())
                    {
                        ReportWarning(TLBX_E_BAD_SIGNATURE, arrayMarshalInfo.GetErrorResourceId());
                        hr = TLBX_E_BAD_SIGNATURE;
                        goto ExitFunc;
                    }
    
                    // TYPEDESC is an array.
                    ptdesc->vt = VT_SAFEARRAY;
                    ptdesc->lptdesc = reinterpret_cast<TYPEDESC*>(ppool->AllocZero(sizeof(TYPEDESC)));
                    if (ptdesc->lptdesc == NULL)
                        IfFailReport(E_OUTOFMEMORY);

                    ArrayToTypeDesc(pCTI, ppool, &arrayMarshalInfo, ptdesc->lptdesc);
                    
                    goto ExitFunc;
                }
                else if (nativeElem == NATIVE_TYPE_FIXEDARRAY)
                {               
                    // NATIVE_TYPE_FIXEDARRAY is only allowed on fields.
                    if (bMethodSig)
                    {
                        ReportWarning(TLBX_E_BAD_SIGNATURE, TLBX_E_BAD_NATIVETYPE);
                        hr = TLBX_E_BAD_SIGNATURE;
                        goto ExitFunc;
                    }
                    
                    // Retrieve the size of the fixed array. This is required.
                    if (cbNativeSig == 0)
                    {
                        ReportWarning(TLBX_E_BAD_SIGNATURE, IDS_EE_BADMARSHALFIELD_FIXEDARRAY_NOSIZE);
                        hr = TLBX_E_BAD_SIGNATURE;
                        goto ExitFunc;
                    }

                    cb = CorSigUncompressData(pbNativeSig, &nativeCount);
                    pbNativeSig += cb;
                    cbNativeSig -= cb;

                    // A size const of 0 isn't supported.
                    if (nativeCount == 0)
                    {
                        ReportWarning(TLBX_E_BAD_SIGNATURE, IDS_EE_BADMARSHALFIELD_FIXEDARRAY_ZEROSIZE);
                        hr = TLBX_E_BAD_SIGNATURE;
                        goto ExitFunc;
                    }

                    // Since these always export to arrays of BSTRs, we don't need to fetch the native type.

                    // Set the data
                    ptdesc->vt = VT_CARRAY;
                    ptdesc->lpadesc = NULL;
                    ptdesc->lpadesc = reinterpret_cast<ARRAYDESC*>(ppool->AllocZero(sizeof(ARRAYDESC)));
                    if (ptdesc->lpadesc == NULL)
                        IfFailReport(E_OUTOFMEMORY);

                    // Compat: FixedArrays of System.Arrays map to fixed arrays of BSTRs.
                    ptdesc->lpadesc->tdescElem.vt = VT_BSTR;
                    ptdesc->lpadesc->cDims = 1;
                    ptdesc->lpadesc->rgbounds->cElements = nativeCount;
                    ptdesc->lpadesc->rgbounds->lLbound = 0;

                    goto ExitFunc;
                }
                else if (nativeElem != NATIVE_TYPE_INTF)
                {
                    ReportWarning(TLBX_E_BAD_SIGNATURE, TLBX_E_BAD_NATIVETYPE);
                    hr = TLBX_E_BAD_SIGNATURE;
                    goto ExitFunc;
                }

                // If the native type is NATIVE_TYPE_INTF then we fall through and convert 
                // System.Array to its IClassX interface.
            }
            else if (stricmpUTF8(pcls, szObjectClass) == 0)
            {
    IsObject:
                // This next statement is to work around a "feature" that marshals an object inside
                //  a struct as an interface, instead of as a variant.  fieldmarshal metadata
                //  can override that.
                if (nativeElem == 0 && !bMethodSig)
                    nativeElem = NATIVE_TYPE_IUNKNOWN;

                switch (nativeElem)
                {
                    case NATIVE_TYPE_INTF:
                    case NATIVE_TYPE_IUNKNOWN:
                        // an IUnknown based interface.
                        ptdesc->vt = VT_UNKNOWN;
                        break;
                                        
                    case NATIVE_TYPE_IDISPATCH:
                        // an IDispatch based interface.
                        ptdesc->vt = VT_DISPATCH;
                        break;
                                
                    case 0:
                    case NATIVE_TYPE_STRUCT:
                        // a VARIANT
                        ptdesc->vt = VT_VARIANT;
                        break;
                                
                    default:
                        ReportWarning(TLBX_E_BAD_SIGNATURE, TLBX_E_BAD_NATIVETYPE);
                        hr = TLBX_E_BAD_SIGNATURE;
                        goto ExitFunc;
                }
                goto ExitFunc;
            }
        } // System
            
        if (SString::_strnicmp(pclsname, szText, cbText) == 0)
        {
            LPCUTF8 pcls; pcls = pclsname + cbText;
            if (stricmpUTF8(pcls, szStringBufferClass) == 0)
            {
                fIsStringBuilder = TRUE;
                        
                // If there is no fieldmarshal information, marshal as a LPWSTR
                if (nativeElem == 0)
                    nativeElem = NATIVE_TYPE_LPWSTR;
                        
                // Marshaller treats stringbuilders as [in, out] by default.
                if (pbByRef)
                    *pbByRef = TRUE;
                        
                goto IsString;
            }
        } // System.Text
            
        if (SString::_strnicmp(pclsname, szCollections, cbCollections) == 0)
        {
            LPCUTF8 pcls; pcls = pclsname + cbCollections;
            if (stricmpUTF8(pcls, szIEnumeratorClass) == 0)
            {
                StdOleTypeToHRef(pCTI, IID_IEnumVARIANT, &hRef);
                ptdesc->vt = VT_PTR;
                ptdesc->lptdesc = reinterpret_cast<TYPEDESC*>(ppool->AllocZero(sizeof(TYPEDESC)));
                if (ptdesc->lptdesc == NULL)
                    IfFailReport(E_OUTOFMEMORY);
                        
                ptdesc->lptdesc->vt = VT_USERDEFINED;
                ptdesc->lptdesc->hreftype = hRef;
                goto ExitFunc;
            }
        } // System.Collections
            
        if (SString::_strnicmp(pclsname, szDrawing, cbDrawing) == 0)
        {
            LPCUTF8 pcls; pcls = pclsname + cbDrawing;
            if (stricmpUTF8(pcls, szColor) == 0)
            {
                StdOleTypeToHRef(pCTI, GUID_OleColor, &hRef);
                ptdesc->vt = VT_USERDEFINED;
                ptdesc->hreftype = hRef;
                goto ExitFunc;
            }
        } // System.Drawing

        // It is not a built-in VT type, so build the typedesc.

        // Determine whether the type is a reference type (IUnknown derived) or a struct type.
        // Get the MethodTable for the referenced class.
        MethodTable     *pRefdClass;            // MethodTable object for referenced TypeDef.
        pRefdClass = LoadClass(pMT->GetModule(), tkTypeRef);

        // Is the type a ref type or a struct type.  Note that a ref type that has layout
        //  is exported as a TKIND_RECORD but is referenced as a **Foo, whereas a
        //  value type is also exported as a TKIND_RECORD but is referenced as a *Foo.
        if (elem == ELEMENT_TYPE_CLASS)
        {           
            // Check if it is a delegate (which can be marshaled as a function pointer).
            if (COMDelegate::IsDelegate(pRefdClass))
            {
                if (nativeElem == NATIVE_TYPE_FUNC)
                {
                    ptdesc->vt = GetVtForIntPtr();
                    goto ExitFunc;
                }
                else if (nativeElem != 0 && nativeElem != NATIVE_TYPE_INTF)
                {
                    ReportWarning(TLBX_E_BAD_SIGNATURE, TLBX_E_BAD_NATIVETYPE);
                    hr = TLBX_E_BAD_SIGNATURE;
                    goto ExitFunc;
                }
            }
            else if (TypeHandle(pRefdClass).CanCastTo(TypeHandle(MscorlibBinder::GetClass(CLASS__SAFE_HANDLE))))
            {
                ptdesc->vt = GetVtForIntPtr();
                goto ExitFunc;
            }
            else if (TypeHandle(pRefdClass).CanCastTo(TypeHandle(MscorlibBinder::GetClass(CLASS__CRITICAL_HANDLE))))
            {
                ptdesc->vt = GetVtForIntPtr();
                goto ExitFunc;
            }

            if (pRefdClass->HasLayout())
            {
                if (nativeElem == NATIVE_TYPE_INTF)
                {
                    // Classes with layout are exported as structs. Because of this, we can't export field or 
                    // parameters of these types marked with [MarshalAs(UnmanagedType.Interface)] as interface
                    // pointers of the actual type. The best we can do is make them IUnknown pointers and 
                    // provide a warning.
                    ReportWarning(TLBX_E_BAD_SIGNATURE, TLBX_W_LAYOUTCLASS_AS_INTERFACE);
                    ptdesc->vt = VT_UNKNOWN;
                    goto ExitFunc;                
                }
                else if (!bMethodSig)
                {
                    // Classes with layout inside structures must be either marked with [MarshalAs(UnmanagedType.Interface)],
                    // [MarshalAs(UnmanagedType.Struct)] or not have any MarshalAs information.
                    if ((nativeElem != 0) && (nativeElem != NATIVE_TYPE_STRUCT))
                    {
                        ReportWarning(TLBX_E_BAD_SIGNATURE, TLBX_E_BAD_NATIVETYPE);
                        hr = TLBX_E_BAD_SIGNATURE;
                        goto ExitFunc;                                                            
                    }

                    // These types are embedded structures so we can treat them as value classes.
                    goto IsStructWithLayout;
                }
                else
                {
                    // Classes with layout as parameters must be either marked with [MarshalAs(UnmanagedType.Interface)]
                    // [MarshalAs(UnmanagedType.LPStruct)] or not have any MarshalAs information.
                    if ((nativeElem != 0) && (nativeElem != NATIVE_TYPE_LPSTRUCT))
                    {
                        ReportWarning(TLBX_E_BAD_SIGNATURE, TLBX_E_BAD_NATIVETYPE);
                        hr = TLBX_E_BAD_SIGNATURE;
                        goto ExitFunc;                                                            
                    }                    
                }
            }

            // A reference to some non-system-defined/non delegate derived type.  Get the reference to the
            //  type, unless it is an imported COM type, in which case, we'll just use
            //  IUnknown.
            // If the type is not visible from COM then we return S_USEIUNKNOWN.
            if (!IsTypeVisibleFromCom(TypeHandle(pRefdClass)))
                hr = S_USEIUNKNOWN;
            else
                hr = EEClassToHref(pCTI, pRefdClass, TRUE, &hRef);
                
            if (hr == S_USEIUNKNOWN)
            {   
                // Not a known type, so use IUnknown
                ptdesc->vt = VT_UNKNOWN;
                goto ExitFunc;
            }
       
            // Not a known class, so make this a pointer to . . .
            ptdesc->vt = VT_PTR;
            ptdesc->lptdesc = reinterpret_cast<TYPEDESC*>(ppool->AllocZero(sizeof(TYPEDESC)));
            if (ptdesc->lptdesc == NULL)
                IfFailReport(E_OUTOFMEMORY);
                
            // . . . a user defined type . . .
            ptdesc->lptdesc->vt = VT_USERDEFINED;
            // . . . based on the token.
            ptdesc->lptdesc->hreftype = hRef;
        }
        else  // It's a value type.
        {   
IsStructWithLayout:    
            // If it is an enum, check the underlying type.  All COM enums are 32 bits,
            //  so if the .Net enum is not a 32 bit enum, convert to the underlying type
            //  instead of the enum type.
            if (pRefdClass->IsEnum())
            {
                // Get the element type of the underlying type.
                CorElementType et = pRefdClass->GetInternalCorElementType();
                // If it is not a 32-bit type or MarshalAs is specified, convert as the
                // underlying type.
                if ((et != ELEMENT_TYPE_I4 && et != ELEMENT_TYPE_U4) ||
                    (nativeElem != 0))
                {
                    elem = et;
                    goto TryWithElemType;
                }
                // Fall through to convert as the enum type.
            }
            else
            {
                // Value classes must be either marked with [MarshalAs(UnmanagedType.Struct)]
                // or not have any MarshalAs information.
                if ((nativeElem != 0) && (nativeElem != NATIVE_TYPE_STRUCT))
                {
                    ReportWarning(TLBX_E_BAD_SIGNATURE, TLBX_E_BAD_NATIVETYPE);
                    hr = TLBX_E_BAD_SIGNATURE;
                    goto ExitFunc;                                                            
                }
            }
                    
            // A reference to some non-system-defined type. Get the reference to the
            // type. Since this is a value class we must get a valid href. Otherwise
            // we fail the conversion.
            hr = TokenToHref(pCTI, pMT, tkTypeRef, FALSE, &hRef);
            if (hr == S_USEIUNKNOWN)
            {
                SString sClsName;
                sClsName.SetUTF8(pclsname);

                LPCWSTR szVCName = sClsName.GetUnicode();
                if (NAMESPACE_SEPARATOR_WCHAR == *szVCName)
                    szVCName++;

                ReportWarning(TLBX_E_BAD_SIGNATURE, TLBX_E_NONVISIBLEVALUECLASS, szVCName);
                hr = TLBX_E_BAD_SIGNATURE;
                goto ExitFunc;
            }

            // Value class is like other UserDefined types, except passed by value, ie
            //  on the stack, instead of by pointer.
            // . . . a user defined type . . .
            ptdesc->vt = VT_USERDEFINED;
            // . . . based on the token.
            ptdesc->hreftype = hRef;
        }
        break;

    case ELEMENT_TYPE_SZARRAY:          
    case ELEMENT_TYPE_ARRAY:
    {
        SigPointer sig(&pbSig[cbElem]);

        // Retrieve the type handle for the array elements.
        TypeHandle thElement = sig.GetTypeHandleThrowing(pModule, &emptyTypeContext);            
        _ASSERTE(!thElement.IsNull());        

        // Update the index into the managed signature array.
        IfFailThrow(sig.SkipExactlyOne());
        cbElem += static_cast<ULONG>(sig.GetPtr() - &pbSig[cbElem]);

        switch (nativeElem)
        {
        case 0:
        case NATIVE_TYPE_SAFEARRAY:
        {
            ULONG vtElement = VT_EMPTY;

            // Retrieve the safe array element type.
            if (cbNativeSig != 0)
            {
                cb = CorSigUncompressData(pbNativeSig, &vtElement);
                pbNativeSig += cb;
                cbNativeSig -= cb;
            }

            ArrayMarshalInfo arrayMarshalInfo(IsExportingAs64Bit() ? amiExport64Bit : amiExport32Bit);
            MarshalInfo::MarshalScenario ms = bMethodSig ? MarshalInfo::MARSHAL_SCENARIO_COMINTEROP : MarshalInfo::MARSHAL_SCENARIO_FIELD;
            arrayMarshalInfo.InitForSafeArray(ms, thElement, (VARTYPE)vtElement, fAnsi);

            if (!arrayMarshalInfo.IsValid())
            {
                ReportWarning(TLBX_E_BAD_SIGNATURE, arrayMarshalInfo.GetErrorResourceId());
                hr = TLBX_E_BAD_SIGNATURE;
                goto ExitFunc;
            }

            // TYPEDESC is an array.
            ptdesc->vt = VT_SAFEARRAY;
            ptdesc->lptdesc = reinterpret_cast<TYPEDESC*>(ppool->AllocZero(sizeof(TYPEDESC)));
            if (ptdesc->lptdesc == NULL)
                IfFailReport(E_OUTOFMEMORY);

            ArrayToTypeDesc(pCTI, ppool, &arrayMarshalInfo, ptdesc->lptdesc);
        }
        break;

        case NATIVE_TYPE_FIXEDARRAY:
        {
            ULONG ntElement = NATIVE_TYPE_DEFAULT;
            
            // NATIVE_TYPE_FIXEDARRAY is only allowed on fields.
            if (bMethodSig)
            {
                ReportWarning(TLBX_E_BAD_SIGNATURE, TLBX_E_BAD_NATIVETYPE);
                hr = TLBX_E_BAD_SIGNATURE;
                goto ExitFunc;
            }
           
            // Retrieve the size of the fixed array. This is required.
            if (cbNativeSig == 0)
            {
                ReportWarning(TLBX_E_BAD_SIGNATURE, IDS_EE_BADMARSHALFIELD_FIXEDARRAY_NOSIZE);
                hr = TLBX_E_BAD_SIGNATURE;
                goto ExitFunc;
            }

            cb = CorSigUncompressData(pbNativeSig, &nativeCount);
            pbNativeSig += cb;
            cbNativeSig -= cb;

            // A size const of 0 isn't supported.
            if (nativeCount == 0)
            {
                ReportWarning(TLBX_E_BAD_SIGNATURE, IDS_EE_BADMARSHALFIELD_FIXEDARRAY_ZEROSIZE);
                hr = TLBX_E_BAD_SIGNATURE;
                goto ExitFunc;
            }

            // Read the optional array sub type if specified. 
            if (cbNativeSig != 0)
            {
                cb = CorSigUncompressData(pbNativeSig, &ntElement);
                pbNativeSig += cb;
                cbNativeSig -= cb;
            }

            ArrayMarshalInfo arrayMarshalInfo(IsExportingAs64Bit() ? amiExport64Bit : amiExport32Bit);
            arrayMarshalInfo.InitForFixedArray(thElement, (CorNativeType)ntElement, fAnsi);

            if (!arrayMarshalInfo.IsValid())
            {
                ReportWarning(TLBX_E_BAD_SIGNATURE, arrayMarshalInfo.GetErrorResourceId());
                hr = TLBX_E_BAD_SIGNATURE;
                goto ExitFunc;
            }

            // Set the data
            ptdesc->vt = VT_CARRAY;
            ptdesc->lpadesc = reinterpret_cast<ARRAYDESC*>(ppool->AllocZero(sizeof(ARRAYDESC)));
            if (ptdesc->lpadesc == NULL)
                IfFailReport(E_OUTOFMEMORY);

            ArrayToTypeDesc(pCTI, ppool, &arrayMarshalInfo, &ptdesc->lpadesc->tdescElem);

            ptdesc->lpadesc->cDims = 1;
            ptdesc->lpadesc->rgbounds->cElements = nativeCount;
            ptdesc->lpadesc->rgbounds->lLbound = 0;
        }
        break;

        case NATIVE_TYPE_ARRAY:
        {
            ULONG ntElement = NATIVE_TYPE_DEFAULT;

            // NATIVE_TYPE_ARRAY is not allowed on fields.
            if (!bMethodSig)
            {
                ReportWarning(TLBX_E_BAD_SIGNATURE, TLBX_E_ARRAY_NEEDS_NT_FIXED);
                hr = TLBX_E_BAD_SIGNATURE;
                goto ExitFunc;
            }
            
            // Read the optional array sub type if specified. 
            if (cbNativeSig != 0)
            {
                cb = CorSigUncompressData(pbNativeSig, &ntElement);
                pbNativeSig += cb;
                cbNativeSig -= cb;
            }

            ArrayMarshalInfo arrayMarshalInfo(IsExportingAs64Bit() ? amiExport64Bit : amiExport32Bit);
            arrayMarshalInfo.InitForNativeArray(MarshalInfo::MARSHAL_SCENARIO_COMINTEROP, thElement, (CorNativeType)ntElement, fAnsi);

            if (!arrayMarshalInfo.IsValid())
            {
                ReportWarning(TLBX_E_BAD_SIGNATURE, arrayMarshalInfo.GetErrorResourceId());
                hr = TLBX_E_BAD_SIGNATURE;
                goto ExitFunc;
            }
    
            ptdesc->vt = VT_PTR;
            ptdesc->lptdesc = reinterpret_cast<TYPEDESC*>(ppool->AllocZero(sizeof(TYPEDESC)));
            if(ptdesc->lptdesc == NULL)
                IfFailReport(E_OUTOFMEMORY);

            ArrayToTypeDesc(pCTI, ppool, &arrayMarshalInfo, ptdesc->lptdesc);
        }
        break;

        default:
            ReportWarning(TLBX_E_BAD_SIGNATURE, TLBX_E_BAD_NATIVETYPE);
            hr = TLBX_E_BAD_SIGNATURE;
            goto ExitFunc;
        }

        // If we are dealing with an ELEMENT_TYPE_ARRAY, we need to eat the array description.
        if (elem == ELEMENT_TYPE_ARRAY)
        {
            // Eat the rank.
            cbElem += CorSigUncompressData(pbSig+cbElem, &elem);
                                                
            // Count of ubounds, ubounds.
            cbElem += CorSigUncompressData(pbSig+cbElem, &elem);
            for (i=elem; i>0; --i)
                cbElem += CorSigUncompressData(pbSig+cbElem, &elem);

            // Count of lbounds, lbounds.
            cbElem += CorSigUncompressData(pbSig+cbElem, &elem);
            for (i=elem; i>0; --i)
                cbElem += CorSigUncompressData(pbSig+cbElem, &elem);
        }

        break;
    }

    case ELEMENT_TYPE_TYPEDBYREF:       // 0x16
        ptdesc->vt = VT_VARIANT;
        break;

    //------------------------------------------
    // This really should be the commented out 
    //  block following.
    case ELEMENT_TYPE_I:              // 0x18,
        ptdesc->vt = GetVtForIntPtr();
        break;
                
    case ELEMENT_TYPE_U:              // 0x19,
        ptdesc->vt = GetVtForUIntPtr();
        break;

    case ELEMENT_TYPE_CMOD_REQD:        // 0x1F     // required C modifier : E_T_CMOD_REQD <mdTypeRef/mdTypeDef>
        ReportWarning(TLBX_E_BAD_SIGNATURE, TLBX_E_UNKNOWN_SIGNATURE);
        hr = TLBX_E_BAD_SIGNATURE;
        goto ExitFunc;

    case ELEMENT_TYPE_SENTINEL:
        goto TryAgain;

    case ELEMENT_TYPE_CMOD_OPT:         // 0x20     // optional C modifier : E_T_CMOD_OPT <mdTypeRef/mdTypeDef>
        cb = CorSigUncompressToken(&pbSig[cbElem], &tkTypeRef);
        cbElem += cb;
        goto TryAgain;

    case ELEMENT_TYPE_FNPTR:
        {
        ptdesc->vt = GetVtForIntPtr();

        // Eat the rest of the signature.
        SigPointer p(&pbSig[cbElem-1]);
        IfFailThrow(p.SkipExactlyOne());
        cbElem += (ULONG)(p.GetPtr() - &pbSig[cbElem]);  // Note I didn't use -1 here.
        break;
    }

    case ELEMENT_TYPE_GENERICINST: 
        ReportWarning(TLBX_E_BAD_SIGNATURE, TLBX_E_GENERICINST_SIGNATURE);
        hr = TLBX_E_BAD_SIGNATURE;
        goto ExitFunc;
        break;

    case ELEMENT_TYPE_VAR: 
    case ELEMENT_TYPE_MVAR: 
        ReportWarning(TLBX_E_BAD_SIGNATURE, TLBX_E_GENERICPAR_SIGNATURE);
        hr = TLBX_E_BAD_SIGNATURE;
        goto ExitFunc;
        break;

    default:
        ReportWarning(TLBX_E_BAD_SIGNATURE, TLBX_E_UNKNOWN_SIGNATURE);
        hr = TLBX_E_BAD_SIGNATURE;
        goto ExitFunc;
        break;
    }

ExitFunc:
        *pcbElem = cbElem;

    if (hr == S_USEIUNKNOWN)
        hr = S_OK;

    return hr;
} // TypeLibExporter::CorSigToTypeDesc
#ifdef _PREFAST_
#pragma warning(pop)
#endif

//*****************************************************************************
// Get an HREFTYPE for an ITypeInfo, in the context of a ICreateTypeInfo2.
//*****************************************************************************
HRESULT TypeLibExporter::TokenToHref(
    ICreateTypeInfo2 *pCTI,              // Typeinfo being created.
    MethodTable     *pMT,                // MethodTable with the token.
    mdToken     tk,                     // The TypeRef to resolve.
    BOOL        bWarnOnUsingIUnknown,   // A flag indicating if we should warn on substituting IUnknown.
    HREFTYPE    *pHref)                 // Put HREFTYPE here.
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(pCTI));
        PRECONDITION(CheckPointer(pMT));
        PRECONDITION(CheckPointer(pHref));
    }
    CONTRACTL_END;

    MethodTable     *pRefdClass;            // MethodTable object for referenced TypeDef.

    // Get the MethodTable for the referenced class, and see if it is being converted.
    pRefdClass = LoadClass(pMT->GetModule(), tk);

    // If the type is not visible from COM then we return S_USEIUNKNOWN.
    if (!IsTypeVisibleFromCom(TypeHandle(pRefdClass)))
        return S_USEIUNKNOWN;

    return EEClassToHref(pCTI, pRefdClass, bWarnOnUsingIUnknown, pHref);
} // HRESULT TypeLibExporter::TokenToHref()

//*****************************************************************************
// Call the resolver to export the typelib for an assembly.
//*****************************************************************************
void TypeLibExporter::ExportReferencedAssembly(
    Assembly    *pAssembly)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(pAssembly));
    }
    CONTRACTL_END;
    
    HRESULT     hr = S_OK;              // A result.
    ITypeLib    *pTLB = 0;              // Exported typelib.

    // Assembly as IP.
    SafeComHolder<IUnknown> pIAssembly = 0;
    
    {
        // Switch to cooperative to get an object ref.
        GCX_COOP();
        
        // Invoke the callback to resolve the reference.
        OBJECTREF orAssembly=0;
        GCPROTECT_BEGIN(orAssembly)
        {
            orAssembly = pAssembly->GetExposedObject();

            pIAssembly = GetComIPFromObjectRef(&orAssembly, MscorlibBinder::GetClass(CLASS__IASSEMBLY));
        }
        GCPROTECT_END();
    }
        
    IfFailReport(m_pNotify->ResolveRef((IUnknown*)pIAssembly, (IUnknown**)&pTLB));
    
    // If we got a typelib, store it on the assembly.
    if (pTLB)
        pAssembly->SetTypeLib(pTLB);
} // void TypeLibExporter::ExportReferencedAssembly()

//*****************************************************************************
// Determine if a class represents a well-known interface, and return that
//  interface (from its real typelib) if it does.
//*****************************************************************************
void TypeLibExporter::GetWellKnownInterface(
    MethodTable     *pMT,                // MethodTable to check.
    ITypeInfo   **ppTI)                 // Put ITypeInfo here, if found.
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(pMT));
        PRECONDITION(CheckPointer(ppTI));
    }
    CONTRACTL_END;

    HRESULT     hr;                     // A result.
    GUID        guid;                   // The MethodTable guid.
    WCHAR       wzGuid[40];             // Guid in string format.
    LONG        cbGuid;                 // Size of guid buffer.
    GUID        guidTlb;                // The typelib guid.
    DWORD       dwError;                // Note: HRESULT_FROM_WIN32 macro evaluates the argument 3x times

    
    HKEYHolder  hInterface;             // Registry key HKCR/Interface
    HKEYHolder  hGuid;                  // Registry key of .../{xxx...xxx}
    HKEYHolder  hTlb;                   // Registry key of .../TypeLib
        
    // The ITypeLib.
    SafeComHolder<ITypeLib> pTLB=0;

    // Get the GUID for the class.  Will generate from name if no defined GUID,
    //  will also use signatures if interface.
    pMT->GetGuid(&guid, TRUE);

    GuidToLPWSTR(guid, wzGuid, lengthof(wzGuid));

    // Look up that interface in the registry.
    dwError = WszRegOpenKeyEx(HKEY_CLASSES_ROOT, W("Interface"),0,KEY_READ, &hInterface);
    hr = HRESULT_FROM_WIN32(dwError);
    if (FAILED(hr))
        return;

    dwError = WszRegOpenKeyEx((HKEY)hInterface, wzGuid, 0, KEY_READ, &hGuid);
    hr = HRESULT_FROM_WIN32(dwError);
    if (FAILED(hr))
        return;

    dwError = WszRegOpenKeyEx((HKEY)hGuid, W("TypeLib"), 0, KEY_READ, &hTlb);
    hr = HRESULT_FROM_WIN32(dwError);
    if (FAILED(hr))
        return;
    
    cbGuid = sizeof(wzGuid);
    dwError = WszRegQueryValue((HKEY)hTlb, W(""), wzGuid, &cbGuid);
    hr = HRESULT_FROM_WIN32(dwError);
    if (FAILED(hr))
        return;
    
    CLSIDFromString(wzGuid, &guidTlb);

    // Retrieve the major and minor version number.
    USHORT wMajor;
    USHORT wMinor;
    Assembly *pAssembly = pMT->GetAssembly();

    hr = GetTypeLibVersionForAssembly(pAssembly,&wMajor, &wMinor);
    if (SUCCEEDED(hr))
    {
        hr = LoadRegTypeLib(guidTlb, wMajor, wMinor, 0, &pTLB);
    }
    if (FAILED(hr))
    {
        pAssembly->GetVersion(&wMajor, &wMinor, NULL, NULL);

        hr = LoadRegTypeLib(guidTlb, wMajor, wMinor, 0, &pTLB);
        if (FAILED(hr))
        {
            hr = LoadRegTypeLib(guidTlb, -1, -1, 0, &pTLB);
            if (FAILED(hr))
            {
                return;
            }
        }
    }
    

    hr = pTLB->GetTypeInfoOfGuid(guid, ppTI);
} // void TypeLibExporter::GetWellKnownInterface()
    
//*****************************************************************************
// Get an HREFTYPE for an ITypeInfo, in the context of a ICreateTypeInfo2.
//*****************************************************************************
HRESULT TypeLibExporter::EEClassToHref( // S_OK or error.
    ICreateTypeInfo2 *pCTI,             // Typeinfo being created.
    MethodTable     *pClass,                // The MethodTable * to resolve.
    BOOL        bWarnOnUsingIUnknown,   // A flag indicating if we should warn on substituting IUnknown.
    HREFTYPE    *pHref)                 // Put HREFTYPE here.
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(pCTI));
        PRECONDITION(CheckPointer(pClass));
        PRECONDITION(CheckPointer(pHref));
    }
    CONTRACTL_END;
    
    HRESULT     hr=S_OK;                // A result.
    int         bUseIUnknown=false;     // Use IUnknown (if so, don't release pTI)?
    int         bUseIUnknownWarned=false; // If true, used IUnknown, but already issued a more specific warning.
    CExportedTypesInfo sExported;       // Cached ICreateTypeInfo pointers.
    CExportedTypesInfo *pExported;      // Pointer to found or new cached pointers.
    CHrefOfClassHashKey sLookup;        // Hash structure to lookup.
    CHrefOfClassHashKey *pFound;        // Found structure.
    bool        bImportedAssembly;      // The assembly containing pClass is imported.
    bool        bForceResolveCallback;  // Type library resolution should always be handled by caller first.

    // A different typeinfo; default for pTI.
    SafeComHolder<ITypeInfo> pTIDef=0;

    // A TypeInfo; maybe for TypeDef, maybe for TypeRef.
    SafeComHolder<ITypeInfo> pTI=0;


    // See if we already know this MethodTable' href.
    sLookup.pClass = pClass;
    if ((pFound=m_HrefOfClassHash.Find(&sLookup)) != NULL)
    {
        *pHref = pFound->href;
        if (*pHref == m_hIUnknown)
            return S_USEIUNKNOWN;
        return S_OK;
    }

    // See if the class is in the export list.
    sExported.pClass = pClass;
    pExported = m_Exports.Find(&sExported);

    // If not in the exported assembly, possibly it was injected?
    if (pExported == 0)
    {
        pExported = m_InjectedExports.Find(&sExported);
    }
    
    // Is there an export for this class?
    if (pExported)
    {   
        // Yes, For interfaces and value types (and enums), just use the typeinfo.
        if (pClass->IsValueType() || pClass->IsEnum() || pClass->HasLayout())
        {
            // No default interface, so use the class itself.     
            if (pExported->pCTI)
                IfFailReport(SafeQueryInterface(pExported->pCTI, IID_ITypeInfo, (IUnknown**)&pTI));
        }
        else
        if (!pClass->IsInterface())
        {   
            // If there is an explicit default interface, get the class for it.
            TypeHandle hndDefItfClass;
            DefaultInterfaceType DefItfType;
            DefItfType = GetDefaultInterfaceForClassWrapper(TypeHandle(pClass), &hndDefItfClass);
            switch (DefItfType)
            {
                case DefaultInterfaceType_Explicit:
                {
                    _ASSERTE(!hndDefItfClass.IsNull());

                    // Recurse to get the href for the default interface class.
                    hr = EEClassToHref(pCTI, hndDefItfClass.GetMethodTable(), bWarnOnUsingIUnknown, pHref);
                    // Done.  Note that the previous call will have cached the href for 
                    //  the default interface class.  As this function exits, it will
                    //  also cache the SAME href for this class.
                    goto ErrExit;
                }

                case DefaultInterfaceType_AutoDispatch:
                case DefaultInterfaceType_AutoDual:
                {
                    _ASSERTE(!hndDefItfClass.IsNull());

                    if (hndDefItfClass.GetMethodTable() != pClass)
                    {
                        // Recurse to get the href for the default interface class.
                        hr = EEClassToHref(pCTI, hndDefItfClass.GetMethodTable(), bWarnOnUsingIUnknown, pHref);
                        // Done.  Note that the previous call will have cached the href for 
                        //  the default interface class.  As this function exits, it will
                        //  also cache the SAME href for this class.
                        goto ErrExit;
                    }

                    // Return the class interface.
                    _ASSERTE(pExported->pCTIClassItf);
                    IfFailReport(SafeQueryInterface(pExported->pCTIClassItf, IID_ITypeInfo, (IUnknown**)&pTI));
                    break;
                }

                case DefaultInterfaceType_IUnknown:
                case DefaultInterfaceType_BaseComClass:
                {
                    pTI = m_pIUnknown;
                    bUseIUnknown=true;
                    SafeAddRef(pTI);
                    break;
                }

                default:
                {
                    _ASSERTE(!"Invalid default interface type!");
                    hr = E_FAIL;
                    break;
                }
            }
        }
        else
        {   // This is an interface, so use the typeinfo for the interface, if there is one.
            if (pExported->pCTI)
                IfFailReport(SafeQueryInterface(pExported->pCTI, IID_ITypeInfo, (IUnknown**)&pTI));
        }

        if ((IUnknown*)pTI == 0)
        {
            // This is a class from the module/assembly, yet it is not being exported.
            
            // Whatever happens, the result is OK.
            hr = S_OK;
            
            if (pClass->IsComImport())
            {
                // If it is an imported type, get an href to it.
                GetWellKnownInterface(pClass, &pTI);
            }
            
            // If still didn't get a TypeInfo, use IUnknown.
            if ((IUnknown*)pTI == 0)
            {
                pTI = m_pIUnknown;
                bUseIUnknown=true;
                SafeAddRef(pTI);
            }
        }
    }
    else
    {   // Not local.  Try to get from the class' module's typelib.
        // If the caller wants to get a chance to resolve type library references themselves (before we go probing the assembly),
        // we'll skip the next step and go directly to the notify sink callback.
        bForceResolveCallback = (m_flags & TlbExporter_CallerResolvedReferences) != 0;
        if (!bForceResolveCallback)
            hr = GetITypeInfoForEEClass(pClass, &pTI, false/* interface, not coclass */, false/* do not create */, m_flags);

        // If getting the typeinfo from the class itself failed, there are 
        //  several possibilities:
        //  - typelib didn't exist, and couldn't be created.
        //  - typelib did exist, but didn't contain the typeinfo.
        // We can create a local (to the exported typelib) copy of the 
        //  typeinfo, and get a reference to that.
        // However, we don't want to export the whole tree into this typelib,
        //  so we only create the typeinfo if the typelib existed  but the
        // typeinfo wasn't found and the assembly is not an imported assembly.
        bImportedAssembly = pClass->GetAssembly()->IsImportedFromTypeLib();

        if (bForceResolveCallback || (FAILED(hr) && hr != TYPE_E_ELEMENTNOTFOUND && !bImportedAssembly))
        {
            // Invoke the callback to resolve the reference.
            
            Assembly *pAssembly = pClass->GetAssembly();
            
            ExportReferencedAssembly(pAssembly);
            
            hr = GetITypeInfoForEEClass(pClass, &pTI, false/* interface, not coclass */, false/* do not create */, m_flags);
        }
        
        if (hr == TYPE_E_ELEMENTNOTFOUND)
        {   
            if (pClass->IsComImport())
            {
                // If it is an imported type, get an href to it.
                
                // Whatever happens, the result is OK.
                hr = S_OK;

                GetWellKnownInterface(pClass, &pTI);
                
                // If still didn't get a TypeInfo, use IUnknown.
                if ((IUnknown*)pTI == 0)
                {
                    pTI = m_pIUnknown;
                    bUseIUnknown=true;
                    SafeAddRef(pTI);
                }
            }
            else
            {
                // Convert the single typedef from the other scope.
                ConvertOneTypeDef(pClass);
                
                // Now that the type has been injected, recurse to let the default-interface code run.
                hr = EEClassToHref(pCTI, pClass, bWarnOnUsingIUnknown, pHref);
                
                // This class should already have been cached by the recursive call.  Don't want to add
                //  it again.
                goto ErrExit2;
            }
        }
        else if (FAILED(hr))
        {
            DefineFullyQualifiedNameForClassWOnStack();
            LPCWSTR szName = GetFullyQualifiedNameForClassNestedAwareW(pClass);
            if (hr == TLBX_W_LIBNOTREGISTERED)
            {
                // The imported typelib is not registered on this machine.  Give a warning, and substitute IUnknown.
                ReportEvent(NOTIF_CONVERTWARNING, hr, szName, (LPCWSTR) pClass->GetAssembly()->GetManifestModule()->GetPath());
                hr = S_OK;
                pTI = m_pIUnknown;
                bUseIUnknown = true;
                SafeAddRef(pTI);
                bUseIUnknownWarned = true;
            }
            else if (hr == TLBX_E_CANTLOADLIBRARY)
            {
                // The imported typelib is registered, but can't be loaded.  Corrupt?  Missing?
                InternalThrowHRWithContext(TLBX_E_CANTLOADLIBRARY, szName, (LPCWSTR) pClass->GetAssembly()->GetManifestModule()->GetPath());
            }
            IfFailReport(hr);
        }
    }

    // Make sure we could resolve the typeinfo.
    if (!(IUnknown*)pTI)
        IfFailReport(TYPE_E_ELEMENTNOTFOUND);

    // Assert that the containing typelib for pContainer is the typelib being created.
#if defined(_DEBUG)
    {
        SafeComHolder<ITypeInfo> pTI=0;
        SafeComHolder<ITypeLib> pTL=0;
        SafeComHolder<ITypeLib> pTLMe=0;
        UINT ix;
        SafeQueryInterface(pCTI, IID_ITypeInfo, (IUnknown**)&pTI);
        SafeQueryInterface(m_pICreateTLB, IID_ITypeLib, (IUnknown**)&pTLMe);
        pTI->GetContainingTypeLib(&pTL, &ix);
        _ASSERTE(pTL == pTLMe);
    }
#endif

    // If there is an ITypeInfo, convert to HREFTYPE.
    if ((IUnknown*)pTI)
    {
        if ((IUnknown*)pTI != m_pIUnknown)
        {
            // Resolve to default.
            if (pTIDef)
                hr = S_OK;  // Already have default.
            else
            {
                // TypeLib API has a issue (sort of by design):
                // Before a type (and its dependencies) is completely created (all members added), 
                // if you call Layout(), or anything that will lead to Layout(), such as ITypeInfo::GetTypeAttr
                // it will give the type an incorrect size. Ideally TypeLib API should fail in this case.
                // Anyway, we only need to avoid calling Layout() directly or indirectly until we have
                // completely created all types. 
                // In this case, we are calling ITypeInfo::GetTypeAttr() in the function below, which is only
                // needed for coclasses. Fortunately, coclass doesn't have a size problem, as it don't have any members
                // So, we skip calling GetDefaultInterfaceForCoclass unless the class is an coclass.
                if (TKindFromClass(pClass) == TKIND_COCLASS)
                    IfFailReport(GetDefaultInterfaceForCoclass(pTI, &pTIDef));
                else
                    hr = S_FALSE;
            }
            
            if (hr == S_OK)
                hr = pCTI->AddRefTypeInfo(pTIDef, pHref);
            else
                hr = pCTI->AddRefTypeInfo(pTI, pHref);
        }
        else
        {   // pTI == m_pIUnknown
            if (m_hIUnknown == -1)
                hr = pCTI->AddRefTypeInfo(pTI, &m_hIUnknown);
            *pHref = m_hIUnknown;
        }
    }
    
ErrExit:
    // If we got the href...
    if (hr == S_OK)
    {
        // Save for later use.
        if ( NULL == (pFound=m_HrefOfClassHash.Add(&sLookup)))
            IfFailReport(E_OUTOFMEMORY);
        
        pFound->pClass = pClass;
        pFound->href = *pHref;
    }

    // If substituting IUnknown, give a warning.
    if (hr == S_OK && bUseIUnknown && bWarnOnUsingIUnknown && !bUseIUnknownWarned)
    {
        DefineFullyQualifiedNameForClassWOnStack();
        LPCWSTR szName = GetFullyQualifiedNameForClassNestedAwareW(pClass);
        ReportWarning(S_OK, TLBX_I_USEIUNKNOWN, szName);
    }
    
ErrExit2:    
    if (hr == S_OK && bUseIUnknown)
        hr = S_USEIUNKNOWN;

    return hr;
} // HRESULT TypeLibExporter::EEClassToHref()

//*****************************************************************************
// Retrieve an HRef to the a type defined in StdOle.
//*****************************************************************************
void TypeLibExporter::StdOleTypeToHRef(ICreateTypeInfo2 *pCTI, REFGUID rGuid, HREFTYPE *pHref)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(pCTI));
        PRECONDITION(CheckPointer(pHref));
    }
    CONTRACTL_END;
    
    HRESULT hr = S_OK;
    SafeComHolder<ITypeLib> pITLB = NULL;
    SafeComHolder<ITypeInfo> pITI = NULL;
    MEMBERID MemID = 0;
    USHORT cFound = 0;

    IfFailReport(LoadRegTypeLib(LIBID_STDOLE2, -1, -1, 0, &pITLB));
    IfFailReport(pITLB->GetTypeInfoOfGuid(rGuid, &pITI));
    IfFailReport(pCTI->AddRefTypeInfo(pITI, pHref));
} // void TypeLibExporter::ColorToHRef()

//*****************************************************************************
// Given a TypeDef's flags, determine the proper TYPEKIND.
//*****************************************************************************
TYPEKIND TypeLibExporter::TKindFromClass(
    MethodTable     *pClass)                // MethodTable.
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(pClass));
    }
    CONTRACTL_END;
    
    HRESULT hr;
    ULONG ulIface = ifDual;       // Is this interface [dual], IUnknown, or DISPINTERFACE.
    
    if (pClass->IsInterface())
    {
        // IDispatch or IUnknown derived?
        IfFailReport(pClass->GetMDImport()->GetIfaceTypeOfTypeDef(pClass->GetCl(), &ulIface));
        if (ulIface == ifDispatch)
            return TKIND_DISPATCH;
        
        return TKIND_INTERFACE;
    }
    
    if (pClass->IsEnum())
        return TKIND_ENUM;

    if (pClass->IsValueType() || pClass->HasLayout())
    {
        TYPEKIND    tkResult=TKIND_RECORD;  // The resulting typekind.
        mdFieldDef  fd;                     // A Field def.
        ULONG       cFD;                    // Count of fields.
        ULONG       iFD=0;                  // Loop control.
        ULONG       ulOffset;               // Field offset.
        bool        bNonZero=false;         // Found any non-zero?
        MD_CLASS_LAYOUT sLayout;            // For enumerating layouts.

        // To enum fields.
        HENUMInternalHolder eFDi(pClass->GetMDImport());
        eFDi.EnumInit(mdtFieldDef, pClass->GetCl());
        
        // Get an enumerator for the FieldDefs in the TypeDef.  Only need the counts.
        cFD = pClass->GetMDImport()->EnumGetCount(&eFDi);

        // Get an enumerator for the class layout.
        IfFailReport(pClass->GetMDImport()->GetClassLayoutInit(pClass->GetCl(), &sLayout));

        // Enumerate the layout.
        while (pClass->GetMDImport()->GetClassLayoutNext(&sLayout, &fd, &ulOffset) == S_OK)
        {
            if (ulOffset != 0)
            {
                bNonZero = true;
                break;
            }
            ++iFD;
        }

        // If there were fields, all had layout, and all layouts are zero, call it a union.
        if (cFD > 0 && iFD == cFD && !bNonZero)
            tkResult = TKIND_UNION;

        return tkResult;
    }
    
    return TKIND_COCLASS;
} // TYPEKIND TypeLibExporter::TKindFromClass()

//*****************************************************************************
// Generate a HREFTYPE in the output TypeLib for a TypeInfo.
//*****************************************************************************
void TypeLibExporter::GetRefTypeInfo(
    ICreateTypeInfo2   *pContainer, 
    ITypeInfo   *pReferenced, 
    HREFTYPE    *pHref)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(pContainer));
        PRECONDITION(CheckPointer(pReferenced));
        PRECONDITION(CheckPointer(pHref));
    }
    CONTRACTL_END;

    HRESULT     hr;                     // A result.
    CHrefOfTIHashKey sLookup;               // Hash structure to lookup.
    CHrefOfTIHashKey *pFound;               // Found structure.

    // See if we already know this TypeInfo.
    sLookup.pITI = pReferenced;
    if ((pFound=m_HrefHash.Find(&sLookup)) != NULL)
    {
        *pHref = pFound->href;
        return;
    }

    // Assert that the containing typelib for pContainer is the typelib being created.
#if defined(_DEBUG)
    {
        SafeComHolder<ITypeInfo> pTI=0;
        SafeComHolder<ITypeLib> pTL=0;
        SafeComHolder<ITypeLib> pTLMe=0;
    UINT ix;
        
        SafeQueryInterface(pContainer, IID_ITypeInfo, (IUnknown**)&pTI);
        SafeQueryInterface(m_pICreateTLB, IID_ITypeLib, (IUnknown**)&pTLMe);
    pTI->GetContainingTypeLib(&pTL, &ix);
    _ASSERTE(pTL == pTLMe);
    }
#endif

    // Haven't seen it -- add the href.
    // NOTE: This code assumes that hreftypes are per-typelib.
    IfFailReport(pContainer->AddRefTypeInfo(pReferenced, pHref));

    // Save for later use.
    pFound=m_HrefHash.Add(&sLookup);
    if (pFound == NULL)
        IfFailReport(E_OUTOFMEMORY);
    
    // Prefix can't tell that IfFailReport will actually throw an exception if pFound is NULL so
    // let's tell it explicitly that if we reach this point pFound will not be NULL.
    PREFIX_ASSUME(pFound != NULL);        
    pFound->pITI = pReferenced;
    pFound->href = *pHref;
    pReferenced->AddRef();
} // HRESULT TypeLibExporter::GetRefTypeInfo()

//*****************************************************************************
// Implementation of a hashed ITypeInfo to HREFTYPE association.
//*****************************************************************************
void TypeLibExporter::CHrefOfTIHash::Clear()
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        SO_TOLERANT;
    }
    CONTRACTL_END;
    
    CHrefOfTIHashKey *p;
    for (p=GetFirst();  p;  p=GetNext(p))
    {
        SafeRelease(p->pITI);
    }
    
    CClosedHash<class CHrefOfTIHashKey>::Clear();
} // void TypeLibExporter::CHrefOfTIHash::Clear()

unsigned int TypeLibExporter::CHrefOfTIHash::Hash(const CHrefOfTIHashKey *pData)
{
    LIMITED_METHOD_CONTRACT;
    
#ifndef _WIN64
    // The pointers are at least 4-byte aligned, so ignore bottom two bits.
    return (unsigned int) (((size_t)(pData->pITI))>>2);
#else
    // @TODO IA64: Is this a good hashing mechanism on IA64?
    return (unsigned int) (((size_t)(pData->pITI))>>3);
#endif
} // unsigned long TypeLibExporter::CHrefOfTIHash::Hash()

unsigned int TypeLibExporter::CHrefOfTIHash::Compare(const CHrefOfTIHashKey *p1, CHrefOfTIHashKey *p2)
{
    LIMITED_METHOD_CONTRACT;
    
    if (p1->pITI == p2->pITI)
        return (0);
    return (1);
} // unsigned long TypeLibExporter::CHrefOfTIHash::Compare()

TypeLibExporter::CHrefOfTIHash::ELEMENTSTATUS TypeLibExporter::CHrefOfTIHash::Status(CHrefOfTIHashKey *p)
{
    LIMITED_METHOD_CONTRACT;
    if (p->pITI == reinterpret_cast<ITypeInfo*>(FREE))
        return (FREE);
    if (p->pITI == reinterpret_cast<ITypeInfo*>(DELETED))
        return (DELETED);
    return (USED);
} // TypeLibExporter::CHrefOfTIHash::ELEMENTSTATUS TypeLibExporter::CHrefOfTIHash::Status()

void TypeLibExporter::CHrefOfTIHash::SetStatus(CHrefOfTIHashKey *p, ELEMENTSTATUS s)
{
    LIMITED_METHOD_CONTRACT;
    
    p->pITI = reinterpret_cast<ITypeInfo*>(s);
} // void TypeLibExporter::CHrefOfTIHash::SetStatus()

void *TypeLibExporter::CHrefOfTIHash::GetKey(CHrefOfTIHashKey *p)
{
    LIMITED_METHOD_CONTRACT;
    
    return &p->pITI;
} // void *TypeLibExporter::CHrefOfTIHash::GetKey()


//*****************************************************************************
// Implementation of a hashed MethodTable* to HREFTYPE association.
//*****************************************************************************
void TypeLibExporter::CHrefOfClassHash::Clear()
{
    WRAPPER_NO_CONTRACT;
    CClosedHash<class CHrefOfClassHashKey>::Clear();
} // void TypeLibExporter::CHrefOfClassHash::Clear()

unsigned int TypeLibExporter::CHrefOfClassHash::Hash(const CHrefOfClassHashKey *pData)
{
    LIMITED_METHOD_CONTRACT;
    
#ifndef _WIN64
    // Tbe pointers are at least 4-byte aligned, so ignore bottom two bits.
    return (unsigned int) (((size_t)(pData->pClass))>>2);
#else
    // @TODO IA64: Is this a good hashing mechanism on IA64?
    return (unsigned int) (((size_t)(pData->pClass))>>3);
#endif
} // unsigned long TypeLibExporter::CHrefOfClassHash::Hash()

unsigned int TypeLibExporter::CHrefOfClassHash::Compare(const CHrefOfClassHashKey *p1, CHrefOfClassHashKey *p2)
{
    LIMITED_METHOD_CONTRACT;
    
    if (p1->pClass == p2->pClass)
        return (0);
    return (1);
} // unsigned long TypeLibExporter::CHrefOfClassHash::Compare()

TypeLibExporter::CHrefOfClassHash::ELEMENTSTATUS TypeLibExporter::CHrefOfClassHash::Status(CHrefOfClassHashKey *p)
{
    LIMITED_METHOD_CONTRACT;
    
    if (p->pClass == reinterpret_cast<MethodTable*>(FREE))
        return (FREE);
    if (p->pClass == reinterpret_cast<MethodTable*>(DELETED))
        return (DELETED);
    return (USED);
} // TypeLibExporter::CHrefOfClassHash::ELEMENTSTATUS TypeLibExporter::CHrefOfClassHash::Status()

void TypeLibExporter::CHrefOfClassHash::SetStatus(CHrefOfClassHashKey *p, ELEMENTSTATUS s)
{
    LIMITED_METHOD_CONTRACT;
    
    p->pClass = reinterpret_cast<MethodTable*>(s);
} // void TypeLibExporter::CHrefOfClassHash::SetStatus()

void *TypeLibExporter::CHrefOfClassHash::GetKey(CHrefOfClassHashKey *p)
{
    LIMITED_METHOD_CONTRACT;
    
    return &p->pClass;
} // void *TypeLibExporter::CHrefOfClassHash::GetKey()


//*****************************************************************************
// Implementation of a hashed MethodTable* to conversion information association.
//*****************************************************************************
void TypeLibExporter::CExportedTypesHash::Clear()
{
    WRAPPER_NO_CONTRACT;

    // Iterate over entries and free pointers.
    CExportedTypesInfo *pData;
    pData = GetFirst();
    while (pData)
    {
        SetStatus(pData, DELETED);
        pData = GetNext(pData);
    }

    CClosedHash<class CExportedTypesInfo>::Clear();
} // void TypeLibExporter::CExportedTypesHash::Clear()

unsigned int TypeLibExporter::CExportedTypesHash::Hash(const CExportedTypesInfo *pData)
{
    LIMITED_METHOD_CONTRACT;    
    
#ifndef _WIN64
    // Tbe pointers are at least 4-byte aligned, so ignore bottom two bits.
    return (unsigned int) (((size_t)(pData->pClass))>>2);
#else
    // @TODO IA64: Is this a good hashing mechanism on IA64?
    return (unsigned int) (((size_t)(pData->pClass))>>3);
#endif
} // unsigned long TypeLibExporter::CExportedTypesHash::Hash()

unsigned int TypeLibExporter::CExportedTypesHash::Compare(const CExportedTypesInfo *p1, CExportedTypesInfo *p2)
{
    LIMITED_METHOD_CONTRACT;
    
    if (p1->pClass == p2->pClass)
        return (0);
    return (1);
} // unsigned long TypeLibExporter::CExportedTypesHash::Compare()

TypeLibExporter::CExportedTypesHash::ELEMENTSTATUS TypeLibExporter::CExportedTypesHash::Status(CExportedTypesInfo *p)
{
    LIMITED_METHOD_CONTRACT;
    
    if (p->pClass == reinterpret_cast<MethodTable*>(FREE))
        return (FREE);
    if (p->pClass == reinterpret_cast<MethodTable*>(DELETED))
        return (DELETED);
    return (USED);
} // TypeLibExporter::CExportedTypesHash::ELEMENTSTATUS TypeLibExporter::CExportedTypesHash::Status()

void TypeLibExporter::CExportedTypesHash::SetStatus(CExportedTypesInfo *p, ELEMENTSTATUS s)
{
    WRAPPER_NO_CONTRACT;
    
    // If deleting a used entry, free the pointers.
    if (s == DELETED && Status(p) == USED)
    {
        if (p->pCTI) p->pCTI->Release(), p->pCTI=0;
        if (p->pCTIClassItf) p->pCTIClassItf->Release(), p->pCTIClassItf=0;
    }
    p->pClass = reinterpret_cast<MethodTable*>(s);
} // void TypeLibExporter::CExportedTypesHash::SetStatus()

void *TypeLibExporter::CExportedTypesHash::GetKey(CExportedTypesInfo *p)
{
    LIMITED_METHOD_CONTRACT;
    
    return &p->pClass;
} // void *TypeLibExporter::CExportedTypesHash::GetKey()

void TypeLibExporter::CExportedTypesHash::InitArray()
{
    STANDARD_VM_CONTRACT;
    
    // For iterating the entries.
    CExportedTypesInfo *pData = 0;
    
    // Make room for the data.
    m_iCount = 0;
    m_Array = new CExportedTypesInfo*[Base::Count()];
    
    // Fill the array.
    pData = GetFirst();
    while (pData)
    {
        m_Array[m_iCount++] = pData;
        pData = GetNext(pData);
    }
} // void TypeLibExporter::CExportedTypesHash::InitArray()

void TypeLibExporter::CExportedTypesHash::UpdateArray()
{
    STANDARD_VM_CONTRACT;
    
    // For iterating the entries.
    CExportedTypesInfo *pData = 0;

    // Clear the old data.
    if (m_Array)
        delete[] m_Array;
    
    // Make room for the data.
    m_iCount = 0;
    m_Array = new CExportedTypesInfo*[Base::Count()];
    
    // Fill the array.
    pData = GetFirst();
    while (pData)
    {
        m_Array[m_iCount++] = pData;
        pData = GetNext(pData);
    }
} // void TypeLibExporter::CExportedTypesHash::UpdateArray()

void TypeLibExporter::CExportedTypesHash::SortByName()
{
    WRAPPER_NO_CONTRACT;
    
    CSortByName sorter(m_Array, (int)m_iCount);
    sorter.Sort();
} // void TypeLibExporter::CExportedTypesHash::SortByName()

void TypeLibExporter::CExportedTypesHash::SortByToken()
{
    WRAPPER_NO_CONTRACT;
    
    CSortByToken sorter(m_Array, (int)m_iCount);
    sorter.Sort();
} // void TypeLibExporter::CExportedTypesHash::SortByToken()

int TypeLibExporter::CExportedTypesHash::CSortByToken::Compare(
    CExportedTypesInfo **p1,
    CExportedTypesInfo **p2)
{
    LIMITED_METHOD_CONTRACT;

    MethodTable *pC1 = (*p1)->pClass;
    MethodTable *pC2 = (*p2)->pClass;
    // Compare scopes.
    if (pC1->GetMDImport() < pC2->GetMDImport())
        return -1;
    if (pC1->GetMDImport() > pC2->GetMDImport())
        return 1;
    // Same scopes, compare tokens.
    if (pC1->GetTypeDefRid() < pC2->GetTypeDefRid())
        return -1;
    if (pC1->GetTypeDefRid() > pC2->GetTypeDefRid())
        return 1;
    // Hmmm.  Same class.
    return 0;
} // int TypeLibExporter::CExportedTypesHash::CSortByToken::Compare()

int TypeLibExporter::CExportedTypesHash::CSortByName::Compare(
    CExportedTypesInfo **p1,
    CExportedTypesInfo **p2)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(p1));
        PRECONDITION(CheckPointer(p2));
        PRECONDITION(CheckPointer(*p1));
        PRECONDITION(CheckPointer(*p2));
    }
    CONTRACTL_END;
    
    int iRslt;                          // A compare result.
    
    MethodTable *pC1 = (*p1)->pClass;
    MethodTable *pC2 = (*p2)->pClass;
    
    // Ignore scopes.  Need to see name collisions across scopes.
    // Same scopes, compare names.
    LPCSTR pName1, pNS1;
    LPCSTR pName2, pNS2;
    IfFailThrow(pC1->GetMDImport()->GetNameOfTypeDef(pC1->GetCl(), &pName1, &pNS1));
    IfFailThrow(pC2->GetMDImport()->GetNameOfTypeDef(pC2->GetCl(), &pName2, &pNS2));
    
    // Compare case-insensitive, because we want different capitalizations to sort together.
    SString sName1(SString::Utf8, pName1);
    SString sName2(SString::Utf8, pName2);

    iRslt = sName1.CompareCaseInsensitive(sName2);
    if (iRslt)
        return iRslt;
    
    // If names are spelled the same, ignoring capitalization, sort by namespace.
    //  We will attempt to use namespace for disambiguation.
    SString sNS1(SString::Utf8, pNS1);
    SString sNS2(SString::Utf8, pNS2);
    
    iRslt = sNS1.CompareCaseInsensitive(sNS2);
    return iRslt;
} // int TypeLibExporter::CExportedTypesHash::CSortByName::Compare()

