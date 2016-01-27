// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ===========================================================================
//  File: ImpTlb.CPP
// 

//

//      ---------------------------------------------------------------
//      Who     When        What
//      ---------------------------------------------------------------
//      WGE     970906      Created
//
// ===========================================================================
#include "stdafx.h"

#include "imptlb.h"
#include <posterror.h>
#include <strongname.h>
#include <nsutilpriv.h>

#include "..\compiler\regmeta.h"
#include "..\compiler\importhelper.h"
#include "tlbutils.h"                   // For GenerateMangledTypeName().
#include <tlbimpexp.h>
#include "sstring.h"
#include "strsafe.h"

#include <metahost.h>

// Pointer to the activated CLR interface provided by the shim.
extern ICLRRuntimeInfo *g_pCLRRuntime;

#ifdef wcsncmp 
 #undef wcsncmp
#endif
#ifdef wcsncpy
 #undef wcsncpy
#endif

// deprecated: use the secureCrt replacements
// _CRTIMP int __cdecl wcsncmp(const wchar_t *, const wchar_t *, size_t);
// _CRTIMP wchar_t * __cdecl wcsncpy(wchar_t *, const wchar_t *, size_t);

#define S_CONVERSION_LOSS _HRESULT_TYPEDEF_(3) // Non-error code meaning a conversion lost information.

#define ADD_ITF_MEMBERS_TO_CLASS        // Define to add interface members to the CoClass.
#define ITF_MEMBER_RESOLUTION_NAMEONLY  // Define to ignore signatures when looking for collisions (ie, when defined
                                        //  void Foo(int) and void Foo(String) collide).

// defines controlling ctor of non-creatable objects.
#define NONCREATABLE_CTOR_VISIBILITY mdAssem  // Define to a visibility flag.

#define MAX_CLASSNAME_SIZE 1024

#ifndef lengthof
#define lengthof(rg)    (sizeof(rg)/sizeof(rg[0]))
#endif

#ifndef IfNullGo
#define IfNullGo(x) do {if (!(x)) IfFailGo(E_OUTOFMEMORY);} while (0)
#endif

#define BUILD_CUSTOM_ATTRIBUTE(type,bytes)  {*reinterpret_cast<UNALIGNED type*>(__pca) = bytes; __pca += sizeof(type); _ASSERTE(__pca-__ca <= sizeof(__ca));}
#define INIT_CUSTOM_ATTRIBUTE(n)            {_ASSERTE((n) <= (sizeof(__ca)-sizeof(SHORT)));__pca = __ca; BUILD_CUSTOM_ATTRIBUTE(USHORT,1);}
#define SIZEOF_CUSTOM_ATTRIBUTE()           ((ULONG) (__pca - __ca))
#define PTROF_CUSTOM_ATTRIBUTE()            (&__ca[0])
#define DECLARE_CUSTOM_ATTRIBUTE(n)         BYTE __ca[(n)+sizeof(SHORT)*2], *__pca;__pca=__ca; INIT_CUSTOM_ATTRIBUTE(n);
#define APPEND_STRING_TO_CUSTOM_ATTRIBUTE(str) {int l = (int)strlen(str); __pca=(BYTE*)CPackedLen::PutLength(__pca,l);memcpy(__pca,str,l);__pca+=l;}
#define FINISH_CUSTOM_ATTRIBUTE()           {BUILD_CUSTOM_ATTRIBUTE(short,0);}

#define DECLARE_DYNLEN_CUSTOM_ATTRIBUTE(n)          CQuickArray<BYTE> __tmpCAArray; IfFailGo(__tmpCAArray.ReSizeNoThrow(n + sizeof(SHORT)*2)); BYTE *__ca, *__pca; __ca = __tmpCAArray.Ptr(); __pca=__ca; BUILD_CUSTOM_ATTRIBUTE(USHORT,1);
#define BUILD_DYNLEN_CUSTOM_ATTRIBUTE(type,bytes)   {*reinterpret_cast<UNALIGNED type*>(__pca) = bytes; __pca += sizeof(type); _ASSERTE(__pca-__ca <= (int)__tmpCAArray.Size());}
#define FINISH_DYNLEN_CUSTOM_ATTRIBUTE()            {BUILD_DYNLEN_CUSTOM_ATTRIBUTE(short,0);}

#define APPEND_WIDE_STRING_TO_CUSTOM_ATTRIBUTE(str)                                             \
{                                                                                               \
    CQuickArray<char> __tmpStr;                                                                 \
    int __cStr = WszWideCharToMultiByte(CP_ACP, 0, str, -1, 0, 0, NULL, NULL);                  \
    IfFailGo(__tmpStr.ReSizeNoThrow(__cStr));                                                   \
    __cStr = WszWideCharToMultiByte(CP_ACP, 0, str, -1, __tmpStr.Ptr(), __cStr, NULL, NULL);    \
    __pca=(BYTE*)CPackedLen::PutLength(__pca,__cStr);                                           \
    memcpy(__pca,__tmpStr.Ptr(),__cStr);                                                        \
    __pca+=__cStr;                                                                              \
}

// The maximum number of bytes the encoding of a DWORD can take.
#define DWORD_MAX_CB 4

// The maximum number of bytes the encoding of a DWORD can take.
#define STRING_OVERHEAD_MAX_CB 4

// Use the unused variant types m_knowntypes for common types.
#define VT_SLOT_FOR_GUID         VT_EMPTY
#define VT_SLOT_FOR_IENUMERABLE  VT_NULL
#define VT_SLOT_FOR_MULTICASTDEL VT_I2
#define VT_SLOT_FOR_TYPE         VT_I4
#define VT_SLOT_FOR_STRINGBUF    VT_I8

static LPCWSTR szObject                             = W("System.Object");
static LPCWSTR szValueType                          = W("System.ValueType");
static LPCWSTR szEnum                               = W("System.Enum");

static LPCWSTR TLB_CLASSLIB_ARRAY                   = {W("System.Array")};
static LPCWSTR TLB_CLASSLIB_DATE                    = {W("System.DateTime")};
static LPCWSTR TLB_CLASSLIB_DECIMAL                 = {W("System.Decimal")};
static LPCWSTR TLB_CLASSLIB_VARIANT                 = {W("System.Variant")};
static LPCWSTR TLB_CLASSLIB_GUID                    = {W("System.Guid")};
static LPCWSTR TLB_CLASSLIB_IENUMERABLE             = {W("System.Collections.IEnumerable")};
static LPCWSTR TLB_CLASSLIB_MULTICASTDELEGATE       = {W("System.MulticastDelegate")};
static LPCWSTR TLB_CLASSLIB_TYPE                    = {W("System.Type")};
static LPCWSTR TLB_CLASSLIB_STRINGBUFFER            = {W("System.Text.StringBuilder")};

static LPCWSTR COM_STDOLE2                          = {W("StdOle")};
static LPCWSTR COM_GUID                             = {W("GUID")};

static const LPCWSTR        PROP_DECORATION_GET     = {W("get_")};
static const LPCWSTR        PROP_DECORATION_SET     = {W("set_")};
static const LPCWSTR        PROP_DECORATION_LET     = {W("let_")};
static const int            PROP_DECORATION_LEN     = 4;

static const LPCWSTR        DLL_EXTENSION           = {W(".dll")};
static const int            DLL_EXTENSION_LEN       = 4;
static const LPCWSTR        EXE_EXTENSION           = {W(".exe")};
static const int            EXE_EXTENSION_LEN       = 4;

static LPCWSTR const        OBJECT_INITIALIZER_NAME = {W(".ctor")};
static const int            OBJECT_INITIALIZER_FLAGS = mdPublic | mdSpecialName;
static const int            OBJECT_INITIALIZER_IMPL_FLAGS = miNative | miRuntime | miInternalCall;
static const int            NONCREATABLE_OBJECT_INITIALIZER_FLAGS = NONCREATABLE_CTOR_VISIBILITY | mdSpecialName;

static const COR_SIGNATURE  OBJECT_INITIALIZER_SIG[3] = { 
    (IMAGE_CEE_CS_CALLCONV_DEFAULT | IMAGE_CEE_CS_CALLCONV_HASTHIS), 0, ELEMENT_TYPE_VOID };

static const int    DEFAULT_INTERFACE_FUNC_FLAGS    = mdPublic | mdVirtual | mdAbstract | mdHideBySig | mdNewSlot;
static const int    DEFAULT_PROPERTY_FUNC_FLAGS     = mdPublic | mdVirtual | mdAbstract | mdHideBySig | mdSpecialName | mdNewSlot;
static const int    DEFAULT_CONST_FIELD_FLAGS       = fdPublic | fdStatic | fdLiteral;
static const int    DEFAULT_RECORD_FIELD_FLAGS      = fdPublic;
static const int    DELEGATE_INVOKE_FUNC_FLAGS      = mdPublic | mdVirtual;

static const int    DEFAULT_ITF_FUNC_IMPL_FLAGS     = miNative | miRuntime | miInternalCall;

static const WCHAR         VTBL_GAP_FUNCTION[]      = {W("_VtblGap")};
static const int           VTBL_GAP_FUNCTION_FLAGS  = mdPublic | mdSpecialName;
static const int           VTBL_GAP_FUNC_IMPL_FLAGS = miRuntime;
static const COR_SIGNATURE VTBL_GAP_SIGNATURE[]     = {IMAGE_CEE_CS_CALLCONV_DEFAULT, 0, ELEMENT_TYPE_VOID};
static const LPCWSTR       VTBL_GAP_FORMAT_1        = {W("%ls%d")};
static const LPCWSTR       VTBL_GAP_FORMAT_N        = {W("%ls%d_%d")};

static const LPCWSTR       ENUM_TYPE_NAME           = {COR_ENUM_FIELD_NAME_W};
static const DWORD         ENUM_TYPE_FLAGS          = fdPublic;
static const COR_SIGNATURE ENUM_TYPE_SIGNATURE[]    = {IMAGE_CEE_CS_CALLCONV_FIELD, ELEMENT_TYPE_I4};
static const DWORD         ENUM_TYPE_SIGNATURE_SIZE = lengthof(ENUM_TYPE_SIGNATURE);

static const LPCWSTR       DYNAMIC_NAMESPACE_NAME   = {W("DynamicModule")};

static const LPCWSTR       UNSAFE_ITF_PREFIX        = {W("Unsafe.")};

static const LPCWSTR       GET_ENUMERATOR_MEMBER_NAME = {W("GetEnumerator")};

static const WCHAR         CLASS_SUFFIX[]              = {W("Class")};
static const DWORD         CLASS_SUFFIX_LENGTH       = lengthof(CLASS_SUFFIX);
static const WCHAR         EVENT_ITF_SUFFIX[]          = {W("_Event")};
static const DWORD         EVENT_ITF_SUFFIX_LENGTH   = lengthof(EVENT_ITF_SUFFIX);
static const WCHAR         EVENT_PROVIDER_SUFFIX[]     = {W("_EventProvider")};
static const DWORD         EVENT_PROVIDER_SUFFIX_LENGTH = lengthof(EVENT_ITF_SUFFIX);
static const WCHAR         EVENT_HANDLER_SUFFIX[]      = {W("EventHandler")};
static const DWORD         EVENT_HANDLER_SUFFIX_LENGTH = lengthof(EVENT_HANDLER_SUFFIX);

static const WCHAR         EVENT_ADD_METH_PREFIX[]          = {W("add_")};
static const DWORD         EVENT_ADD_METH_PREFIX_LENGTH   = lengthof(EVENT_ADD_METH_PREFIX);
static const WCHAR         EVENT_REM_METH_PREFIX[]          = {W("remove_")};
static const DWORD         EVENT_REM_METH_PREFIX_LENGTH   = lengthof(EVENT_REM_METH_PREFIX);

static const WCHAR         DELEGATE_INVOKE_METH_NAME[]      = {W("Invoke")};
static const DWORD         DELEGATE_INVOKE_METH_NAME_LENGTH = lengthof(EVENT_ADD_METH_PREFIX);

// {C013B386-CC3E-4b6d-9B67-A3AE97274BBE}
static const GUID FREE_STATUS_GUID = 
{ 0xc013b386, 0xcc3e, 0x4b6d, { 0x9b, 0x67, 0xa3, 0xae, 0x97, 0x27, 0x4b, 0xbe } };

// {C013B387-CC3E-4b6d-9B67-A3AE97274BBE}
static const GUID DELETED_STATUS_GUID = 
{ 0xc013b387, 0xcc3e, 0x4b6d, { 0x9b, 0x67, 0xa3, 0xae, 0x97, 0x27, 0x4b, 0xbe } };

// {C013B388-CC3E-4b6d-9B67-A3AE97274BBE}
static const GUID USED_STATUS_GUID = 
{ 0xc013b388, 0xcc3e, 0x4b6d, { 0x9b, 0x67, 0xa3, 0xae, 0x97, 0x27, 0x4b, 0xbe } };

static const GUID IID_IEnumerable = 
{ 0x496b0abe, 0xcdee, 0x11d3, { 0x88, 0xe8, 0x00, 0x90, 0x27, 0x54, 0xc4, 0x3a } };


 #define STRUCTLAYOUT tdSequentialLayout
// ULONG_MAX is a flag meaning "don't convert".
static const ULONG rdwTypeFlags[] = {
    tdPublic | tdSealed,                                // TKIND_ENUM       = 0,
    tdPublic | tdSealed | tdBeforeFieldInit | STRUCTLAYOUT, // TKIND_RECORD    = TKIND_ENUM + 1,    
    tdPublic | tdAbstract,                              // TKIND_MODULE     = TKIND_RECORD + 1,
    tdPublic | tdInterface | tdAbstract | tdImport,     // TKIND_INTERFACE  = TKIND_MODULE + 1,
    tdPublic | tdInterface | tdAbstract | tdImport,     // TKIND_DISPATCH   = TKIND_INTERFACE + 1,
    tdPublic | tdImport,                                // TKIND_COCLASS    = TKIND_DISPATCH + 1,
    tdPublic | tdImport,                                // TKIND_ALIAS      = TKIND_COCLASS + 1,
    tdPublic | tdSealed | tdExplicitLayout,             // TKIND_UNION     = TKIND_ALIAS + 1,
    ULONG_MAX,                                          // TKIND_MAX        = TKIND_UNION + 1
};
static const LPCWSTR g_szTypekind[] = {
    W("Enum         "),
    W("Record       "),
    W("Module       "),
    W("Interface    "),
    W("Dispinterface"),
    W("Coclass      "),
    W("Alias        "),
    W("Union        "),
};

#define NATIVE_TYPE_NONE ((CorNativeType)(NATIVE_TYPE_MAX+1))

#define NON_CONVERTED_PARAMS_FLAGS (PARAMFLAG_FRETVAL|PARAMFLAG_FLCID)


//*****************************************************************************
// External declarations.
//*****************************************************************************
extern mdAssemblyRef DefineAssemblyRefForImportedTypeLib(
    void        *pAssembly,             // Assembly importing the typelib.
    void        *pvModule,              // Module importing the typelib.
    IUnknown    *pIMeta,                // IMetaData* from import module.
    IUnknown    *pIUnk,                 // IUnknown to referenced Assembly.
    BSTR        *pwzNamespace,          // The namespace of the resolved assembly.
    BSTR        *pwzAsmName,            // The name of the resolved assembly.
    Assembly    **AssemblyRef);         // The resolved assembly.

extern mdAssemblyRef DefineAssemblyRefForExportedAssembly(
    LPCWSTR     szFullName,             // Assembly full name.
    IUnknown    *pIMeta);               // Metadata emit interface.

static HRESULT _UnpackVariantToConstantBlob(VARIANT *pvar, BYTE *pcvType, void **pvValue, __int64 *pd);
static INT64 _DoubleDateToTicks(const double d);
static HRESULT TryGetFuncDesc(ITypeInfo *pITI, int i, FUNCDESC **ppFunc);

//*****************************************************************************
// Class factory.
//*****************************************************************************
CImportTlb* CImportTlb::CreateImporter(
    LPCWSTR     szLibrary, 
    ITypeLib    *pitlb, 
    BOOL        bGenerateTCEAdapters, 
    BOOL        bUnsafeInterfaces,
    BOOL        bSafeArrayAsSystemArray,
    BOOL        bTransformDispRetVals,
    BOOL        bPreventClassMembers,
    BOOL        bSerializableValueClasses)
{
    return new (nothrow) CImportTlb(szLibrary, pitlb, bGenerateTCEAdapters, bUnsafeInterfaces, bSafeArrayAsSystemArray, bTransformDispRetVals, bPreventClassMembers, bSerializableValueClasses);
} // CImportTlb* CImportTlb::CreateImporter()

//*****************************************************************************
// Default constructor.
//*****************************************************************************
CImportTlb::CImportTlb()
 :  m_szLibrary(NULL),
    m_pITLB(NULL),
    m_bGenerateTCEAdapters(false),
    m_bSafeArrayAsSystemArray(false),
    m_bTransformDispRetVals(false),
    m_bPreventClassMembers(false),
    m_bSerializableValueClasses(false),
    m_pEmit(NULL),
    m_pImport(NULL),
    m_pITI(NULL),
    m_pOrigITI(NULL),
    m_psAttr(NULL),
    m_arSystem(mdAssemblyRefNil),
    m_Notify(NULL),
    m_trValueType(0),
    m_trEnum(0),
    m_bUnsafeInterfaces(FALSE),
    m_tkSuppressCheckAttr(mdTokenNil),
    m_tdHasDefault(0),
    m_szName(NULL),
    m_szMember(NULL),
    m_wzNamespace(NULL),
    m_tkInterface(0),
    m_szInterface(NULL),
    m_pMemberNames(NULL),
    m_cMemberProps(0),
    m_ImplIface(eImplIfaceNone)
{
    // Clear the known types array.  The values will be lazily initialized.
    memset(m_tkKnownTypes, 0, sizeof(m_tkKnownTypes));
    memset(m_tkAttr, 0, sizeof(m_tkAttr));
} // CImportTlb::CImportTlb()

//*****************************************************************************
// Complex constructor.
//*****************************************************************************
CImportTlb::CImportTlb(
    LPCWSTR     szLibrary,              // Name of library being imported.   
    ITypeLib    *pitlb,                 // The type library to import from.  
    BOOL        bGenerateTCEAdapters,   // A flag indicating if the TCE adapters are being generated.
    BOOL        bUnsafeInterfaces,      // A flag indicating that runtime security checks should be disabled
    BOOL        bSafeArrayAsSystemArray,// A flag indicating whether to import SAFEARRAY's as System.Array's.
    BOOL        bTransformDispRetVals,   // A flag indicating if we should do [out,retval] transformation on disp only itfs.
    BOOL        bPreventClassMembers,   // A flag indicating if we should add members to CoClasses.
    BOOL        bSerializableValueClasses) // A flag indicating if we should mark value classes serializable.
 :  m_szLibrary(szLibrary),
    m_pITLB(pitlb),
    m_bGenerateTCEAdapters(bGenerateTCEAdapters),
    m_bUnsafeInterfaces(bUnsafeInterfaces),
    m_bSafeArrayAsSystemArray(bSafeArrayAsSystemArray),
    m_bTransformDispRetVals(bTransformDispRetVals),
    m_bPreventClassMembers(bPreventClassMembers),
    m_bSerializableValueClasses(bSerializableValueClasses),
    m_pEmit(0),
    m_pImport(0),
    m_pITI(0),
    m_pOrigITI(0),
    m_psAttr(0),
    m_arSystem(mdAssemblyRefNil),
    m_Notify(0),
    m_trValueType(0),
    m_trEnum(0),
    m_tkSuppressCheckAttr(mdTokenNil),
    m_tdHasDefault(0),
    m_szName(0),
    m_szMember(0),
    m_wzNamespace(0),
    m_tkInterface(0),
    m_szInterface(0),
    m_pMemberNames(0),
    m_cMemberProps(0),
    m_ImplIface(eImplIfaceNone)
{
    if (pitlb)
        pitlb->AddRef();

    // Clear the known types array.  The values will be lazily initialized.
    memset(m_tkKnownTypes, 0, sizeof(m_tkKnownTypes));
    memset(m_tkAttr, 0, sizeof(m_tkAttr));
    
#if defined(TLB_STATS)
    m_bStats = QueryPerformanceFrequency(&m_freqVal);
#endif
} // CImportTlb::CImportTlb()

//*****************************************************************************
// Destructor.
//*****************************************************************************
CImportTlb::~CImportTlb()
{
    if (m_pEmit)
        m_pEmit->Release();
    if (m_pImport)
        m_pImport->Release();
    if (m_pITLB)
        m_pITLB->Release();
    if (m_Notify)
        m_Notify->Release();

    if (m_wzNamespace)
        ::SysFreeString(m_wzNamespace);
} // CImportTlb::~CImportTlb()


//*****************************************************************************
// Allow the user to specify a namespace to be used in the conversion.
//*****************************************************************************
HRESULT CImportTlb::SetNamespace(
    WCHAR const *pNamespace)
{
    HRESULT     hr=S_OK;                // A result.
    
    IfNullGo(m_wzNamespace=::SysAllocString(pNamespace));
    
ErrExit:
    
    return hr;
} // HRESULT CImportTlb::SetNamespace()

//*****************************************************************************
// Allow the user to specify a notification object to be used in the conversion.
//*****************************************************************************
HRESULT CImportTlb::SetNotification(
    ITypeLibImporterNotifySink *pNotify)
{
    _ASSERTE(m_Notify == 0);
    m_Notify = pNotify;
    pNotify->AddRef();

    return S_OK;
} // HRESULT CImportTlb::SetNotification()

//*****************************************************************************
// Allow the user to specify the MetaData scope to be used in the conversion.
//*****************************************************************************
HRESULT CImportTlb::SetMetaData(
    IUnknown    *pIUnk)
{
    HRESULT     hr;
    _ASSERTE(m_pEmit == 0);
    IfFailGo(pIUnk->QueryInterface(IID_IMetaDataEmit2, (void**)&m_pEmit));
ErrExit:
    return hr;    
} // HRESULT CImportTlb::SetMetaData()

//*****************************************************************************
// Import a TypeLibrary into a CompLib.
//*****************************************************************************
HRESULT CImportTlb::Import()
{
#ifndef DACCESS_COMPILE
    HRESULT     hr;                     // A result.
    mdModule    md;                     // Module token.
    VARIANT     vt = {0};               // For setting options.
    ITypeLib2   *pITLB2 = 0;            // To get custom attributes.
    IMetaDataDispenserEx *pDisp = 0;    // To create export scope.
    TLIBATTR    *psAttr=0;              // The library's attributes.
    BSTR        szLibraryName = 0;      // The library's name.
    LPCWSTR     wzFile;                 // The filename of the typelib (no path).
    LPCWSTR     wzSource;               // Source of the typelib, for CA.
    
    _ASSERTE(m_Notify);

    // Quick sanity check.
    if (!m_pITLB)
        return (E_INVALIDARG);

    // Check to see if the type library implements ITypeLib2.
    if (m_pITLB->QueryInterface(IID_ITypeLib2, (void **)&pITLB2) != S_OK)
        pITLB2 = 0;

    // If custom attribute for namespace exists, use it.
    if (pITLB2)
    {
        VARIANT vt;
        VariantInit(&vt);
        if (pITLB2->GetCustData(GUID_ManagedName, &vt) == S_OK)
        {   
            if (V_VT(&vt) == VT_BSTR)
            {
                // If there already was a namespace set, release it.
                if (m_wzNamespace)
                    SysFreeString(m_wzNamespace);
            
                // If the namespace ends with .dll then remove the extension.
                LPWSTR pDest = wcsstr(vt.bstrVal, DLL_EXTENSION);
                if (pDest && (pDest[DLL_EXTENSION_LEN] == 0 || pDest[DLL_EXTENSION_LEN] == ' '))
                    *pDest = 0;

                if (!pDest)
                {
                    // If the namespace ends with .exe then remove the extension.
                    pDest = wcsstr(vt.bstrVal, EXE_EXTENSION);
                    if (pDest && (pDest[EXE_EXTENSION_LEN] == 0 || pDest[EXE_EXTENSION_LEN] == ' '))
                        *pDest = 0;
                }

                if (pDest)
                {
                    // We removed the extension so re-allocate a string of the new length.
                    m_wzNamespace = SysAllocString(vt.bstrVal);
                    SysFreeString(vt.bstrVal);
                    IfNullGo(m_wzNamespace);
                }
                else
                {
                    // There was no extension to remove so we can use the string returned
                    // by GetCustData().
                    m_wzNamespace = vt.bstrVal;
                }        
            }
            else
            {
                VariantClear(&vt);
            }
        }
    }

    // Use the namespace name if we don't know the filename.
    if (!m_szLibrary)
        m_szLibrary = m_wzNamespace;
    
    // If the typelib was exported from COM+ to begin with, don't import it.
    if (pITLB2)
    {
        ::VariantInit(&vt);
        hr = pITLB2->GetCustData(GUID_ExportedFromComPlus, &vt);
        if (vt.vt != VT_EMPTY)
        {
            if (0)
            {
                // com emulates option is ON
            }
            else
            {
                IfFailGo(PostError(TLBX_E_CIRCULAR_IMPORT, m_szLibrary));
            }
        }
    }

    _ASSERTE(m_pEmit);
    IfFailGo(m_pEmit->QueryInterface(IID_IMetaDataImport2, (void **)&m_pImport));

    // Initialize the reserved names map.
    IfFailGo(m_ReservedNames.Init());

    // Initialize the default interface to class interface map for the TLB being imported.
    IfFailGo(m_DefItfToClassItfMap.Init(m_pITLB, m_wzNamespace));

    // Create the Object classref record and AssemblyRef for mscorlib.dll.
    IfFailGo(_DefineSysRefs());    
    
    // Create the library record.
    IfFailGo(_NewLibraryObject());

    // Note that this was imported.
    IfFailGo(m_pITLB->GetLibAttr(&psAttr));
    if (SUCCEEDED(::QueryPathOfRegTypeLib(psAttr->guid, psAttr->wMajorVerNum, psAttr->wMinorVerNum,  psAttr->lcid, &szLibraryName)))
        wzSource = szLibraryName;
    else
        wzSource = m_szLibrary;

    // We can't base the decision on SYSKIND.  For example, we can have a SYS_WIN64 tlb loaded as 32-bit with 4-byte aligned pointers.
    m_cbVtableSlot = 0;

    IfFailGo(m_pImport->GetModuleFromScope(&md));
    // Skip the path or drive info 
    wzFile = wcsrchr(wzSource, W('\\'));
    if (wzFile == 0)
    {   // That's odd, should have been a fully qualified path.  Just use an empty string.
        wzFile = W("");
    }
    else
    {   // skip leading backslash
        wzFile++;
    }

    // Convert the typelib.
    IfFailGo(ConvertTypeLib());

ErrExit:
    if (psAttr)
        m_pITLB->ReleaseTLibAttr(psAttr);
    if (szLibraryName)
        ::SysFreeString(szLibraryName);
    if (pITLB2)
        pITLB2->Release();
    if (pDisp)
        pDisp->Release();

    return (hr);
#else
    DacNotImpl();
    return E_NOTIMPL;
#endif // #ifndef DACCESS_COMPILE
} // HRESULT CImportTlb::Import()
    
//*****************************************************************************
// Create the Complib to represent the TypeLib.
//*****************************************************************************
HRESULT CImportTlb::_NewLibraryObject()
{
    HRESULT             hr;                     // A result.
    TLIBATTR *          psAttr=0;               // The library's attributes.
    BSTR                szLibraryName=0;        // The library's name.
    CQuickArray<WCHAR>  rScopeName;             // The name of the scope.

    // Information about the library.
    IfFailGo(m_pITLB->GetLibAttr(&psAttr));
    IfFailGo(m_pITLB->GetDocumentation(MEMBERID_NIL, &szLibraryName, 0, 0, 0));

    // Create the scope name by using the typelib name and adding .dll.
    IfFailGo(rScopeName.ReSizeNoThrow(SysStringLen(szLibraryName) + 5 * sizeof(WCHAR)));
    StringCchPrintf(rScopeName.Ptr(), rScopeName.Size(), W("%s.dll"), szLibraryName);

    IfFailGo(m_pEmit->SetModuleProps(rScopeName.Ptr()));

ErrExit:
    if (psAttr)
        m_pITLB->ReleaseTLibAttr(psAttr);

    if (szLibraryName)
        ::SysFreeString(szLibraryName);

    return (hr);
} // HRESULT CImportTlb::_NewLibraryObject()

//*****************************************************************************
// Define an assembly ref for mscorlib, typeref for Object.
//*****************************************************************************
HRESULT CImportTlb::_DefineSysRefs()
{
    HRESULT     hr;                     // A result.
    WCHAR       szPath[_MAX_PATH];
    WCHAR       szDrive[_MAX_DRIVE];
    WCHAR       szDir[_MAX_PATH];
    DWORD       dwLen;                  // Length of system directory name.
    IMetaDataDispenserEx *pDisp = 0;    // To import mscorlib.
    IMetaDataAssemblyImport *pAImp = 0; // To read mscorlib assembly.
    IMetaDataAssemblyEmit *pAEmit = 0;  // To create mscorlib assembly ref.
    ASSEMBLYMETADATA amd = {0};         // Assembly metadata.
    mdToken     tk;                     // A token.
    const void  *pvPublicKey;           // Public key.
    ULONG       cbPublicKey;            // Length of public key.
    BYTE        *pbToken=0;             // Compressed token for public key.
    ULONG       cbToken;                // Length of token.
    ULONG       ulHashAlg;              // Hash algorithm.
    DWORD       dwFlags;                // Assembly flags.

    // Get the dispenser.
    IfFailGo(g_pCLRRuntime->GetInterface(
        CLSID_CorMetaDataDispenser, 
        IID_IMetaDataDispenserEx, 
        (void **)&pDisp));

    // Get the name of mscorlib.
    //@todo: define, function, etc., instead of hard coded "mscorlib"
    dwLen = lengthof(szPath) - 13; // allow space for "mscorlib" ".dll" "\0"
    IfFailGo(pDisp->GetCORSystemDirectory(szPath, dwLen, &dwLen));
    SplitPath(szPath, szDrive, _MAX_DRIVE, szDir, _MAX_PATH, 0, 0, 0, 0);
    MakePath(szPath, szDrive, szDir, W("mscorlib"), W(".dll"));
    
    // Open the scope, get the details.
    IfFailGo(pDisp->OpenScope(szPath, 0, IID_IMetaDataAssemblyImport, (IUnknown**)&pAImp));
    IfFailGo(pAImp->GetAssemblyFromScope(&tk));
    IfFailGo(pAImp->GetAssemblyProps(tk, &pvPublicKey,&cbPublicKey, &ulHashAlg, 
        szPath,lengthof(szPath),&dwLen, &amd, &dwFlags));
    
    if (!StrongNameTokenFromPublicKey((BYTE*)(pvPublicKey),cbPublicKey, &pbToken,&cbToken))
    {
        hr = StrongNameErrorInfo();
        goto ErrExit;
    }
    dwFlags &= ~afPublicKey;
    
    // Define the assembly ref.
    IfFailGo(m_pEmit->QueryInterface(IID_IMetaDataAssemblyEmit, (void**)&pAEmit));
    IfFailGo(pAEmit->DefineAssemblyRef(pbToken,cbToken, szPath, &amd,0,0,dwFlags, &m_arSystem));
    
    IfFailGo(m_TRMap.DefineTypeRef(m_pEmit, m_arSystem, szObject, &m_trObject));
    
    m_tkKnownTypes[VT_DISPATCH] = m_trObject;
    m_tkKnownTypes[VT_UNKNOWN] = m_trObject;
    m_tkKnownTypes[VT_VARIANT] = m_trObject;
    
ErrExit:
    if (pbToken)
        StrongNameFreeBuffer(pbToken);
    if (pDisp)
        pDisp->Release();
    if (pAEmit)
        pAEmit->Release();
    if (pAImp)
        pAImp->Release();

    return hr;
} // HRESULT CImportTlb::_DefineSysRefs()

//*****************************************************************************
// Lazily get the token for a CustomAttribute.
//*****************************************************************************
HRESULT CImportTlb::GetAttrType(
    int         attr,                   // The attribute for which the type is desired.
    mdToken     *pTk)                   // Put the type here.
{
    HRESULT     hr = S_OK;              // A result.
    mdTypeRef   tr;                     // An intermediate typeref.
    DWORD           dwSigSize;          // The size of the sig for special sigs.
    DWORD           dwMaxSigSize;       // The max size of the special sig.
    COR_SIGNATURE   *pSig;              // Pointer to the start of the sig,
    COR_SIGNATURE   *pCurr;             // Current sig pointer.
    mdTypeRef       trType;             // The typeref for System.Type.

    _ASSERTE(attr >= 0);
    _ASSERTE(attr < ATTR_COUNT);

    //@todo: globally define these names.
#define INTEROP_ATTRIBUTE(x) static COR_SIGNATURE x##_SIG[] = INTEROP_##x##_SIG;
#define INTEROP_ATTRIBUTE_SPECIAL(x)
    INTEROP_ATTRIBUTES();
#undef INTEROP_ATTRIBUTE
#undef INTEROP_ATTRIBUTE_SPECIAL
#define INTEROP_ATTRIBUTE(x) \
    case ATTR_##x: \
        IfFailGo(m_pEmit->DefineTypeRefByName(m_arSystem, INTEROP_##x##_TYPE_W, &tr)); \
        IfFailGo(m_pEmit->DefineMemberRef(tr, W(".ctor"), x##_SIG, lengthof(x##_SIG), &m_tkAttr[attr])); \
        break;
#define INTEROP_ATTRIBUTE_SPECIAL(x)

    if (IsNilToken(m_tkAttr[attr]))
    {
        switch (attr)
        {
            INTEROP_ATTRIBUTES();

            case ATTR_COMEVENTINTERFACE:
            {
                // Retrieve token for System.Type.
                IfFailGo(GetKnownTypeToken(VT_SLOT_FOR_TYPE, &trType));

                // Build the sig.
                dwMaxSigSize = 5 + sizeof(mdTypeRef) * 2;
                pSig = (COR_SIGNATURE*)_alloca(dwMaxSigSize);
                pCurr = pSig;
                *pCurr++ = IMAGE_CEE_CS_CALLCONV_DEFAULT_HASTHIS;
                *pCurr++ = 2;
                *pCurr++ = ELEMENT_TYPE_VOID;
                *pCurr++ = ELEMENT_TYPE_CLASS;
                pCurr += CorSigCompressToken(trType, pCurr);
                *pCurr++ = ELEMENT_TYPE_CLASS;
                pCurr += CorSigCompressToken(trType, pCurr);
                dwSigSize = (DWORD)(pCurr - pSig);
                _ASSERTE(dwSigSize <= dwMaxSigSize);

                // Declare the typeref and the member ref for the CA.
                IfFailGo(m_pEmit->DefineTypeRefByName(m_arSystem, INTEROP_COMEVENTINTERFACE_TYPE_W, &tr)); \
                IfFailGo(m_pEmit->DefineMemberRef(tr, W(".ctor"), pSig, dwSigSize, &m_tkAttr[attr])); \
                break;
            }

            case ATTR_COCLASS:
            {
                // Retrieve token for System.Type.
                IfFailGo(GetKnownTypeToken(VT_SLOT_FOR_TYPE, &trType));

                // Build the sig.
                dwMaxSigSize = 4 + sizeof(mdTypeRef);
                pSig = (COR_SIGNATURE*)_alloca(dwMaxSigSize);
                pCurr = pSig;
                *pCurr++ = IMAGE_CEE_CS_CALLCONV_DEFAULT_HASTHIS;
                *pCurr++ = 1;
                *pCurr++ = ELEMENT_TYPE_VOID;
                *pCurr++ = ELEMENT_TYPE_CLASS;
                pCurr += CorSigCompressToken(trType, pCurr);
                dwSigSize = (DWORD)(pCurr - pSig);
                _ASSERTE(dwSigSize <= dwMaxSigSize);

                // Declare the typeref and the member ref for the CA.
                IfFailGo(m_pEmit->DefineTypeRefByName(m_arSystem, INTEROP_COCLASS_TYPE_W, &tr)); \
                IfFailGo(m_pEmit->DefineMemberRef(tr, W(".ctor"), pSig, dwSigSize, &m_tkAttr[attr])); \
                break;
            }
        }
    }
#undef INTEROP_ATTRIBUTE
#undef INTEROP_ATTRIBUTE_SPECIAL

    *pTk = m_tkAttr[attr];
ErrExit:
    return hr;  
} // HRESULT CImportTlb::GetAttrType()

//*****************************************************************************
// Create the TypeDefs.
//*****************************************************************************
HRESULT 
CImportTlb::ConvertTypeLib()
{
    HRESULT hr;
    int     cTi;    // Count of TypeInfos.
    int     i;      // Loop control.
    
    // How many TypeInfos?
    IfFailGo(cTi = m_pITLB->GetTypeInfoCount());
    
    // Iterate over them.
    for (i = 0; i < cTi; ++i)
    {
        // Get the TypeInfo.
        hr = m_pITLB->GetTypeInfo(i, &m_pITI);
        if (SUCCEEDED(hr))
        {
            // Save up the original TypeInfo (may be later alias-resolved).
            _ASSERTE(m_pOrigITI == NULL);
            m_pOrigITI = m_pITI;
            m_pOrigITI->AddRef();
            
            // Retrieve the attributes of the type info.
            IfFailGo(m_pITI->GetTypeAttr(&m_psAttr));
            
            // Convert the TypeInfo.
            hr = ConvertTypeInfo();
            if (FAILED(hr))
            {
                if (hr == CEE_E_CVTRES_NOT_FOUND || hr == TLBX_I_RESOLVEREFFAILED)
                {   // Reflection emit is broken, no need to try to continue.
                    goto ErrExit;
                }
                
                BSTR szTypeInfoName = NULL;
                hr = m_pITI->GetDocumentation(MEMBERID_NIL, &szTypeInfoName, 0, 0, 0);
                if (SUCCEEDED(hr))
                {
                    ReportEvent(NOTIF_CONVERTWARNING, TLBX_E_INVALID_TYPEINFO, szTypeInfoName);
                }
                else
                {
                    ReportEvent(NOTIF_CONVERTWARNING, TLBX_E_INVALID_TYPEINFO_UNNAMED, i);
                }
                if (szTypeInfoName != NULL)
                    ::SysFreeString(szTypeInfoName);
#if defined(_DEBUG)                
                if (CLRConfig::GetConfigValue(CLRConfig::INTERNAL_MD_TlbImp_BreakOnErr))
                    _ASSERTE(!"Invalid type");
#endif                
            }

            // Release for next TypeInfo.
            m_pOrigITI->Release();
            m_pOrigITI = NULL;
      
            m_pITI->ReleaseTypeAttr(m_psAttr);
            m_psAttr = NULL;
            m_pITI->Release();
            m_pITI = NULL;
        }
    }
    
ErrExit:
    if (m_pOrigITI != NULL)
    {
        m_pOrigITI->Release();
        m_pOrigITI = NULL;
    }
    
    if (m_psAttr != NULL)
    {
        m_pITI->ReleaseTypeAttr(m_psAttr);
        m_psAttr = NULL;
    }
    if (m_pITI != NULL)
    {
        m_pITI->Release();
        m_pITI = NULL;
    }
    return hr;
} // CImportTlb::ConvertTypeLib

//*****************************************************************************
// Convert a single ITypeInfo into the scope.
//*****************************************************************************
HRESULT CImportTlb::ConvertTypeInfo()   // S_OK or error.
{
    HRESULT     hr;                     // A result.
    BSTR        bstrManagedName=0;      // Managed name (or part thereof).
    CQuickArray<WCHAR> qbClassName;     // The name of the class.
    ULONG       ulFlags;                // TypeDef flags.
    WORD        wTypeInfoFlags;         // TypeInfo flags.  Alias flags, if an alias.
    mdToken     tkAttr;                 // Attribute type for flags.
    TYPEKIND    tkindAlias;             // TYPEKIND of an aliased TypeInfo.
    GUID        guid;                   // GUID of the typeinfo.
    BOOL        bConversionLoss=false;  // If true, info was lost converting sigs.
    mdToken     tkParent;               // Parent of the typedef.
    mdToken     td;                     // For looking up a TypeDef.
    ITypeInfo2  *pITI2=0;               // For getting custom value.
    
#if defined(TLB_STATS)
    WCHAR       rcStats[16];            // Buffer for stats.
    LARGE_INTEGER __startVal;
    QueryPerformanceCounter(&__startVal); 
#endif
    
    m_tdTypeDef = mdTypeDefNil;
    
    // Get some information about the TypeInfo.
    IfFailGo(m_pITI->GetDocumentation(MEMBERID_NIL, &m_szName, 0, 0, 0));
    
#if defined(_DEBUG)                
    LPWSTR strShouldBreakOnTypeName = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_MD_TlbImp_BreakOnTypeImport);
    if ((NULL != strShouldBreakOnTypeName) && (wcsncmp(strShouldBreakOnTypeName, m_szName, MAX_CLASSNAME_LENGTH) == 0))
        _ASSERTE(!"MD_TlbImp_BreakOnTypeImport");
#endif                

    // Assume that we will be able to convert the typeinfo.
    guid = m_psAttr->guid;
    wTypeInfoFlags = m_psAttr->wTypeFlags;
    
    // If this typeinfo is an alias, see what it is an alias for.  If for a built-in
    //  type, we will just skip it.  If for a user-defined type, we will duplicate
    //  that definition under this alias' name and guid.
    if (m_psAttr->typekind == TKIND_ALIAS)
    {
        hr = _ResolveTypeDescAliasTypeKind(m_pITI, &m_psAttr->tdescAlias, &tkindAlias);
        IfFailGo(hr);
        if (hr == S_OK)
        {
            TYPEDESC tdesc = m_psAttr->tdescAlias;
            m_pITI->ReleaseTypeAttr(m_psAttr);
            m_pITI->Release();

            IfFailGo(_ResolveTypeDescAlias(m_pOrigITI, &tdesc, &m_pITI, &m_psAttr, &guid));
            // Now m_pOrigITI refers to the alias whereas m_pITI is the TypeInfo of the aliased type.

            // We should no longer have an alias.
            _ASSERTE(m_psAttr->typekind == tkindAlias);
            _ASSERTE(tkindAlias != TKIND_ALIAS);

            ulFlags = rdwTypeFlags[tkindAlias];
        }
        else
            ulFlags = ULONG_MAX;
    }
    else
    {
        ulFlags = rdwTypeFlags[m_psAttr->typekind];
    }

    // Figure out the name.

    // If the type info is for a CoClass, we need to decorate the name.
    if (m_psAttr->typekind == TKIND_COCLASS)
    {
        // Generate a mangled name for the component.
        IfFailGo(GetManagedNameForCoClass(m_pOrigITI, qbClassName));
        m_szMngName = qbClassName.Ptr();
    }   
    else 
    {
        IfFailGo(GetManagedNameForTypeInfo(m_pOrigITI, m_wzNamespace, NULL, &bstrManagedName));
        m_szMngName = bstrManagedName;
    }
    
    if (m_psAttr->typekind == TKIND_INTERFACE ||
        (m_psAttr->typekind == TKIND_DISPATCH && m_psAttr->wTypeFlags & TYPEFLAG_FDUAL))
    {
        // If the interface is not derived from IUnknown, or not an interface, we can't convert it.
        if (IsIUnknownDerived(m_pITI, m_psAttr) != S_OK)
        {
            ReportEvent(NOTIF_CONVERTWARNING, TLBX_E_NOTIUNKNOWN, m_szName);
            ulFlags = ULONG_MAX;
        }
        // If the interface is not derived from IDispatch, but claims to be [dual], give a warning but convert it.
        if ((m_psAttr->wTypeFlags & TYPEFLAG_FDUAL) && IsIDispatchDerived(m_pITI, m_psAttr) != S_OK)
        {
            ReportEvent(NOTIF_CONVERTWARNING, TLBX_W_DUAL_NOT_DISPATCH, m_szName);
        }
    }
    else
    if (m_psAttr->typekind == TKIND_MODULE)
    {   // If module has no vars, skip it.  We currently don't import module functions.
        if (m_psAttr->cVars == 0)
            ulFlags = ULONG_MAX;
    }
    
    // If something we can convert...
    if (ulFlags != ULONG_MAX)
    {   
        // Interfaces derive from nil...
        if (IsTdInterface(ulFlags))
            tkParent = mdTypeDefNil;
        else  // ... enums from Enum, ...
        if (m_psAttr->typekind == TKIND_ENUM)
        {
            if (IsNilToken(m_trEnum))
                IfFailGo(m_TRMap.DefineTypeRef(m_pEmit, m_arSystem, szEnum, &m_trEnum));
            tkParent = m_trEnum;
        }
        else // ... structs from ValueType, ...
        if (m_psAttr->typekind == TKIND_RECORD || m_psAttr->typekind == TKIND_UNION)
        {
            if (IsNilToken(m_trValueType))
                IfFailGo(m_TRMap.DefineTypeRef(m_pEmit, m_arSystem, szValueType, &m_trValueType));
            tkParent = m_trValueType;
        }
        else // ... and classes derive from Object.
            tkParent = m_trObject;

        // The typelib importer generates metadata into an empty ReflectionEmit scope.  Because
        //  RE manages type names itself, duplicate checking is turned off.  Because of user-defined
        //  names (via CUSTOM), it is possible for the user to declare a duplicate.  So,
        //  before adding the new type, check for duplicates.
        hr = m_pImport->FindTypeDefByName(m_szMngName, mdTypeDefNil, &td);
        if (hr != CLDB_E_RECORD_NOTFOUND)
        {
            ReportEvent(NOTIF_CONVERTWARNING, TLBX_E_DUPLICATE_TYPE_NAME, m_szMngName);
            IfFailGo(TLBX_E_DUPLICATE_TYPE_NAME);
        }

        // Create the typedef.
        IfFailGo(m_pEmit->DefineTypeDef(m_szMngName, ulFlags, tkParent, 0, &m_tdTypeDef));
        IfFailGo(_AddGuidCa(m_tdTypeDef, guid));

        // Save the typeinfo flags.
        if (wTypeInfoFlags)
        {
            IfFailGo(GetAttrType(ATTR_TYPELIBTYPE, &tkAttr));
            DECLARE_CUSTOM_ATTRIBUTE(sizeof(WORD));
            BUILD_CUSTOM_ATTRIBUTE(WORD, wTypeInfoFlags);
            FINISH_CUSTOM_ATTRIBUTE();
            IfFailGo(m_pEmit->DefineCustomAttribute(m_tdTypeDef, tkAttr, PTROF_CUSTOM_ATTRIBUTE(), SIZEOF_CUSTOM_ATTRIBUTE(),0));
        }

        // Mark unsafe interfaces (suppressed security runtime checks).
        if (m_bUnsafeInterfaces)
        {
            if (m_tkSuppressCheckAttr == mdTokenNil)
            {
                mdTypeRef       tr;
                COR_SIGNATURE   rSig[] = {IMAGE_CEE_CS_CALLCONV_DEFAULT_HASTHIS, 0, ELEMENT_TYPE_VOID};
                IfFailGo(m_pEmit->DefineTypeRefByName(m_arSystem, COR_SUPPRESS_UNMANAGED_CODE_CHECK_ATTRIBUTE, &tr));
                IfFailGo(m_pEmit->DefineMemberRef(tr, COR_CTOR_METHOD_NAME_W, rSig, lengthof(rSig), &m_tkSuppressCheckAttr));
            }

            DECLARE_CUSTOM_ATTRIBUTE(0);
            FINISH_CUSTOM_ATTRIBUTE();
            IfFailGo(m_pEmit->DefineCustomAttribute(m_tdTypeDef, m_tkSuppressCheckAttr, PTROF_CUSTOM_ATTRIBUTE(), SIZEOF_CUSTOM_ATTRIBUTE(), 0));
        }

        // Fill in the details depending on the type of the TypeInfo.
        switch (m_psAttr->typekind)
        {
        case TKIND_ENUM:
            hr = ConvEnum(m_pITI, m_psAttr);
            break;
            
        case TKIND_RECORD:
            hr = ConvRecord(m_pITI, m_psAttr, FALSE);
            break;
            
        case TKIND_UNION:
            hr = ConvRecord(m_pITI, m_psAttr, TRUE);
            break;
            
        case TKIND_MODULE:
            hr = ConvModule(m_pITI, m_psAttr);
            break;
            
        case TKIND_INTERFACE:
            hr = ConvIface(m_pITI, m_psAttr);
            break;
            
        case TKIND_DISPATCH:
            hr = ConvDispatch(m_pITI, m_psAttr);
            break;
            
        case TKIND_COCLASS:
            hr = ConvCoclass(m_pITI, m_psAttr);
            break;
            
        case TKIND_ALIAS:
            _ASSERTE(!"Alias should have been resolved!");
            break;
            
        default:
            _ASSERTE(!"Unexpected TYPEKIND");
            break;
        }
        if (FAILED(hr))
            goto ErrExit;
        
        if (hr == S_CONVERSION_LOSS)
        {
            bConversionLoss = true;
            IfFailGo(GetAttrType(ATTR_COMCONVERSIONLOSS, &tkAttr));
            DECLARE_CUSTOM_ATTRIBUTE(0);
            FINISH_CUSTOM_ATTRIBUTE();
            IfFailGo(m_pEmit->DefineCustomAttribute(m_tdTypeDef, tkAttr, PTROF_CUSTOM_ATTRIBUTE(),SIZEOF_CUSTOM_ATTRIBUTE(),0));
        }

    }
    
    if (bConversionLoss)
        hr = S_CONVERSION_LOSS;
    else
        hr = S_OK;
        
#if defined(TLB_STATS)
    LARGE_INTEGER __stopVal;
    QueryPerformanceCounter(&__stopVal);
    DWORD __delta;
    __delta = (DWORD)(__stopVal.QuadPart - __startVal.QuadPart);
    StringCchPrintf(rcStats, COUNTOF(rcStats), W("  %.2f"), 
            ((float)__delta*1000)/(float)m_freqVal.QuadPart);
#endif
    
    // Report that this type has been converted.
    ReportEvent(NOTIF_TYPECONVERTED, TLBX_I_TYPEINFO_IMPORTED, m_szName);
    
ErrExit:
    if (pITI2)
        pITI2->Release();
    if (m_szName)
        ::SysFreeString(m_szName), m_szName = 0;
    if (bstrManagedName)
        ::SysFreeString(bstrManagedName);
    return (hr);
} // HRESULT CImportTlb::ConvertTypeInfo()


//*****************************************************************************
// Determine if the type explicitly implements IEnumerable.
//*****************************************************************************
HRESULT CImportTlb::ExplicitlyImplementsIEnumerable(
    ITypeInfo   *pITI,                  // ITypeInfo* to check for IEnumerable.
    TYPEATTR    *psAttr,                // TYPEATTR of TypeInfo.
    BOOL        fLookupPartner)         // Flag indicating if we should look at the partner itf.
{
    HREFTYPE    href;                   // HREFTYPE of an implemented interface.
    ITypeInfo   *pItiIface=0;           // ITypeInfo for an interface.
    TYPEATTR    *psAttrIface=0;         // TYPEATTR for an interface.
    BOOL        fFoundImpl = FALSE;
    int         i = 0;
    HRESULT     hr = S_OK;
    ITypeInfo*  pITISelf2 = NULL;
    TYPEATTR    psAttrSelf2;
    int         ImplFlags = 0;
    
    // Look through each of the implemented/inherited interfaces
    for (i=0; i<psAttr->cImplTypes && !fFoundImpl; ++i)
    {
        // Get an interface
        IfFailGo(pITI->GetRefTypeOfImplType(i, &href));
        IfFailGo(pITI->GetRefTypeInfo(href, &pItiIface));
        IfFailGo(pItiIface->GetTypeAttr(&psAttrIface));
        IfFailGo(pITI->GetImplTypeFlags(i, &ImplFlags));

        if (!(ImplFlags & IMPLTYPEFLAG_FSOURCE))
        {
            hr = ExplicitlyImplementsIEnumerable(pItiIface, psAttrIface, TRUE);
            if (hr == S_OK)
                fFoundImpl = TRUE;
            
            // Check this interface for the IEnumerable.
            if (psAttrIface->guid == IID_IEnumerable)
                fFoundImpl = TRUE;
        }

        pItiIface->ReleaseTypeAttr(psAttrIface);
        psAttrIface = 0;
        pItiIface->Release();
        pItiIface = 0;
    }

    if ( fLookupPartner && (pITI->GetRefTypeOfImplType(-1, &href) == S_OK) )
    {
        IfFailGo(pITI->GetRefTypeInfo(href, &pItiIface));
        IfFailGo(pItiIface->GetTypeAttr(&psAttrIface));

        hr = ExplicitlyImplementsIEnumerable(pItiIface, psAttrIface, FALSE);
        if (hr == S_OK)
            fFoundImpl = TRUE;
        
        // Check this interface for the IEnumerable.
        if (psAttrIface->guid == IID_IEnumerable)
            fFoundImpl = TRUE;   
    }


ErrExit:
    if (psAttrIface)
        pItiIface->ReleaseTypeAttr(psAttrIface);
    if (pItiIface)
        pItiIface->Release();
    
    return (fFoundImpl) ? S_OK : S_FALSE;
}


//*****************************************************************************
// Convert the details for a coclass.
//*****************************************************************************
#ifdef _PREFAST_
#pragma warning(push)
#pragma warning(disable:21000) // Suppress PREFast warning about overly large function
#endif
HRESULT CImportTlb::ConvCoclass(        // S_OK or error.
    ITypeInfo   *pITI,                  // ITypeInfo* to convert.
    TYPEATTR    *psAttr)                // TYPEATTR of TypeInfo.
{
    BOOL        fHadDefaultItf = FALSE;
    HRESULT     hr;                     // A result.
    int         i;                      // Loop control.
    HREFTYPE    href;                   // HREFTYPE of an implemented interface.
    ITypeInfo   *pItiIface=0;           // ITypeInfo for an interface.
    TYPEATTR    *psAttrIface=0;         // TYPEATTR for an interface.
    int         ImplFlags;              // ImplType flags.
    mdToken     tkIface;                // Token for an interface.
    CQuickArray<mdToken> rImpls;        // Array of implemented interfaces.
    CQuickArray<mdToken> rEvents;       // Array of implemented event interfaces.
    CQuickArray<mdToken> rTmpImpls;     // Temporary array of impls.
    CQuickArray<ITypeInfo*> rImplTypes; // Array of implemented ITypeInfo*s.
    CQuickArray<ITypeInfo*> rSrcTypes;  // Array of source ITypeInfo*s.
    int         ixSrc;                  // Index into rSrcTypes for source interfaces.
    int         ixImpl;                 // Index into rImpls for implemented interface.
    int         ixTmpImpl;              // Index into rTmpImpls.
    mdToken     mdCtor;                 // Dummy token for the object initializer.
    mdToken     tkAttr;                 // Token for custom attribute type.
    mdToken     token;                  // Dummy token for typeref.
    BOOL        fInheritsIEnum = FALSE;
    
#ifdef _DEBUG
    int         bImplIEnumerable=0;     // If true, the class implements IEnumerable.
#endif

    // Size the rImpls and rSrcs arrays large enough for impls, events, the IEnumerable itf and two ending nulls.
    IfFailGo(rImpls.ReSizeNoThrow(psAttr->cImplTypes+2));
    memset(rImpls.Ptr(), 0, (psAttr->cImplTypes+2)*sizeof(mdToken));
    IfFailGo(rEvents.ReSizeNoThrow(psAttr->cImplTypes+1));
    memset(rEvents.Ptr(), 0, (psAttr->cImplTypes+1)*sizeof(mdToken));
    IfFailGo(rTmpImpls.ReSizeNoThrow(psAttr->cImplTypes+3));
    memset(rTmpImpls.Ptr(), 0, (psAttr->cImplTypes+3)*sizeof(mdToken));    
    IfFailGo(rImplTypes.ReSizeNoThrow(psAttr->cImplTypes+2));
    memset(rImplTypes.Ptr(), 0, (psAttr->cImplTypes+2)*sizeof(ITypeInfo*));
    IfFailGo(rSrcTypes.ReSizeNoThrow(psAttr->cImplTypes+1));
    memset(rSrcTypes.Ptr(), 0, (psAttr->cImplTypes+1)*sizeof(ITypeInfo*));
    ixImpl = -1;
    ixSrc = -1;
    ixTmpImpl = -1;

    if (ExplicitlyImplementsIEnumerable(pITI, psAttr) == S_OK)
        fInheritsIEnum = TRUE;

    // Build the list of implemented and event interfaces.
    // The EE cares about implemented interfaces, so we convert them to actual
    //  tokens and add them to the typedef.  VB cares about event interfaces,
    //  but we are going to add a list of typeref names as a custom attribute.
    //  We can't build the list as we go along, because the default may not
    //  be the first event source.  So, we store tokens for the implemented
    //  interfaces, but ITypeInfo*s for the event sources.
    for (i=0; i<psAttr->cImplTypes; ++i)
    {
        IfFailGo(pITI->GetRefTypeOfImplType(i, &href));
        IfFailGo(pITI->GetRefTypeInfo(href, &pItiIface));
        IfFailGo(pItiIface->GetTypeAttr(&psAttrIface));
        IfFailGo(pITI->GetImplTypeFlags(i, &ImplFlags));
        
        // If the interface is derived from IUnknown, or not an interface, we can't use it as an interface.
        // Don't add explicit IUnknown or IDispatch.
        if ((IsIUnknownDerived(pItiIface, psAttrIface) != S_OK && psAttrIface->typekind != TKIND_DISPATCH) ||
            psAttrIface->guid == IID_IDispatch ||
            psAttrIface->guid == IID_IUnknown)
        {
            pItiIface->ReleaseTypeAttr(psAttrIface);
            psAttrIface = 0;
            pItiIface->Release();
            pItiIface = 0;
            continue;
        }     

        // Add the event to the impls list or the events list.
        if (ImplFlags & IMPLTYPEFLAG_FSOURCE)
        {
            // Get the token for the event interface.
            IfFailGo(_GetTokenForEventItf(pItiIface, &tkIface));

            // If we've already marked this CoClass as implementing this source interface, don't do so again.
            for (int iCheck=0; iCheck <= ixSrc; iCheck++)
            {
                if (rEvents[iCheck] == tkIface)
                    goto LoopEnd;
            }

            // Add the source interface to the list of source interfaces.
            ++ixSrc;

            // If this is explicitly the default source interface...
            if (ImplFlags & IMPLTYPEFLAG_FDEFAULT)
            {   
                // Put the def source ITypeInfo at the head of the list of source
                // ITypeInfo's.
                for (int ix = ixSrc; ix > 0; --ix)
                {
                    rSrcTypes[ix] = rSrcTypes[ix-1];
                    rEvents[ix] = rEvents[ix-1];
                }
                rEvents[0] = tkIface;
                rSrcTypes[0] = pItiIface;
            }
            else
            {
                rEvents[ixSrc] = tkIface;
                rSrcTypes[ixSrc] = pItiIface;
            }
        }
        else
        {   
            // Get the token for the interface.
            IfFailGo(_GetTokenForTypeInfo(pItiIface, FALSE, &tkIface));

            // If we've already marked this CoClass as implementing this interface, don't do so again.
            for (int iCheck=0; iCheck <= ixImpl; iCheck++)
            {
                if (rImpls[iCheck] == tkIface)
                    goto LoopEnd;
            }
    
            // Add the implemented interface to the list of implemented interfaces.
            ++ixImpl;

            // If this is explicitly the default interface...
            if (ImplFlags & IMPLTYPEFLAG_FDEFAULT)
            {   
                fHadDefaultItf = TRUE;
                // Put the new interface at the start of the list.
                for (int ix=ixImpl; ix > 0; --ix)
                {
                    rImpls[ix] = rImpls[ix-1];
                    rImplTypes[ix] = rImplTypes[ix-1];
                }
                rImpls[0] = tkIface;
                rImplTypes[0] = pItiIface;
            }
            else
            {
                rImpls[ixImpl] = tkIface;
                rImplTypes[ixImpl] = pItiIface;
            }
        }

LoopEnd:
        pItiIface->ReleaseTypeAttr(psAttrIface);
        psAttrIface = 0;
        pItiIface = 0;  // Pointer now owned by array.
    }

    // Create an interface that will represent the class.
    IfFailGo(_CreateClassInterface(pITI, rImplTypes[0], rImpls[0], rEvents[0], &tkIface));

    // Create a temporary array of interface tokens.
    if (fHadDefaultItf)
    {
        // default interface should be the first interface
        rTmpImpls[++ixTmpImpl] = rImpls[0];
        rTmpImpls[++ixTmpImpl] = tkIface;
    }
    else
    {        
        rTmpImpls[++ixTmpImpl] = tkIface;
        if (ixImpl >= 0)
            rTmpImpls[++ixTmpImpl] = rImpls[0];
    }
    if (ixSrc >= 0)
        rTmpImpls[++ixTmpImpl] = rEvents[0];
    if (ixImpl >= 0)
    {
        memcpy(&rTmpImpls[ixTmpImpl + 1], &rImpls[1], ixImpl * sizeof(mdTypeRef));
        ixTmpImpl += ixImpl;
    }
    if (ixSrc >= 0)
    {
        memcpy(&rTmpImpls[ixTmpImpl + 1], &rEvents[1], ixSrc * sizeof(mdTypeRef));
        ixTmpImpl += ixSrc;
    }

    // Check to see if the default interface has a member with a DISPID of DISPID_NEWENUM.
    BOOL fIEnumFound = FALSE;
    if (ixImpl >= 0)
    {
        // The ITypeInfo for the default interface had better be set.
        _ASSERTE(rImplTypes[0]);
        
        if ( (!fInheritsIEnum) && (HasNewEnumMember(rImplTypes[0]) == S_OK) )
        {
            IfFailGo(GetKnownTypeToken(VT_SLOT_FOR_IENUMERABLE, &tkIface));
            rTmpImpls[++ixTmpImpl] = tkIface;
            fIEnumFound = TRUE;
        }
    }

    // Else Check to see if the IEnumerable Custom Value exists on the CoClass.
    if (!fIEnumFound)
    {
        BOOL CVExists = FALSE;
        _ForceIEnumerableCVExists(pITI, &CVExists);
        if (CVExists && !fInheritsIEnum)
        {
            IfFailGo(GetKnownTypeToken(VT_SLOT_FOR_IENUMERABLE, &tkIface));
            rTmpImpls[++ixTmpImpl] = tkIface;
            fIEnumFound = TRUE;
        }
    }

    // Add the implemented interfaces and event interfaces to the TypeDef.
    IfFailGo(m_pEmit->SetTypeDefProps(m_tdTypeDef, ULONG_MAX/*Classflags*/, 
        ULONG_MAX, (mdToken*)rTmpImpls.Ptr()));

    // Create an initializer for the class.  
    ULONG ulFlags;
    if (psAttr->wTypeFlags & TYPEFLAG_FCANCREATE)
        ulFlags = OBJECT_INITIALIZER_FLAGS;
    else
        ulFlags = NONCREATABLE_OBJECT_INITIALIZER_FLAGS;
    {
        IfFailGo(m_pEmit->DefineMethod(m_tdTypeDef, OBJECT_INITIALIZER_NAME, ulFlags,
            OBJECT_INITIALIZER_SIG, sizeof(OBJECT_INITIALIZER_SIG), 0/*rva*/, OBJECT_INITIALIZER_IMPL_FLAGS/*flags*/, &mdCtor));
    }
    
    // Set ClassInterfaceType.None on the generated class.
    DECLARE_CUSTOM_ATTRIBUTE(sizeof(short));
    BUILD_CUSTOM_ATTRIBUTE(short, clsIfNone);
    IfFailGo(GetAttrType(ATTR_CLASSINTERFACE, &tkAttr));
    FINISH_CUSTOM_ATTRIBUTE();
    IfFailGo(m_pEmit->DefineCustomAttribute(m_tdTypeDef, tkAttr, PTROF_CUSTOM_ATTRIBUTE(), SIZEOF_CUSTOM_ATTRIBUTE(), 0));


    if (!m_bPreventClassMembers)
    {
        // Iterate over the implemented interfaces, and add the members to the coclass.
        m_ImplIface = eImplIfaceDefault;
        for (i=0; i<=ixImpl; ++i)
        {   
            _ASSERTE(rImplTypes[i]);

            // Interface info.
            m_tkInterface = rImpls[i];
            pItiIface = rImplTypes[i];
            rImplTypes[i] = 0; // ownership transferred.
            
            // Get interface name for decoration.
            if (m_szInterface) 
                ::SysFreeString(m_szInterface), m_szInterface = 0;
            IfFailGo(pItiIface->GetDocumentation(MEMBERID_NIL, &m_szInterface, 0,0,0));
            
            // Add the interface members to the coclass.
            IfFailGo(pItiIface->GetTypeAttr(&psAttrIface));
            switch (psAttrIface->typekind)
            {
            case TKIND_DISPATCH:
                hr = ConvDispatch(pItiIface, psAttrIface, false);
                break;
            case TKIND_INTERFACE:
                hr = ConvIface(pItiIface, psAttrIface, false);
                break;
            default:
                hr = S_OK;
                _ASSERTE(!"Unexpected typekind for implemented interface");
            }
            pItiIface->ReleaseTypeAttr(psAttrIface);
            psAttrIface = 0;
            IfFailGo(hr);
            m_ImplIface = eImplIface;
            rImplTypes[i] = pItiIface;
            pItiIface = 0; // ownership transferred back.
        }

        // Add the methods of the event interfaces to the class.
        for (i=0; i<=ixSrc; ++i)
            IfFailGo(_AddSrcItfMembersToClass(rEvents[i]));
    }
    
    // If there are source interfaces, add a custom value for that.
    if (ixSrc >= 0)
    {
        CQuickArray<char> rEvents;  // Output buffer.
        int cbCur;              // Current location in output buffer.
        int cbReq;              // Size of an individual piece.
        CQuickArray<WCHAR> rEvent;

        // Save 6 bytes at the beginning of the buffer for the custom attribute prolog and
        //  the string length.  The string length may require 1, 2, or 4 bytes to express.
        cbCur = 6;

        // For each event interface...
        for (int ix=0; ix <= ixSrc; ++ix)
        {
            pItiIface = rSrcTypes[ix];
            rSrcTypes[ix] = 0;

            // Get the typeref name for the interface.
            for(;;)
            {
                int cchReq;
                IfFailGo(_GetTokenForTypeInfo(pItiIface, FALSE, &token, rEvent.Ptr(), (int)rEvent.MaxSize(), &cchReq, TRUE));
                if (cchReq <= (int)rEvent.MaxSize())
                    break;
                IfFailGo(rEvent.ReSizeNoThrow(cchReq));
            }

            // Append to the buffer.  See how much space is required, get it.
            cbReq = WszWideCharToMultiByte(CP_UTF8,0, rEvent.Ptr(),-1, 0,0, 0,0);
            
            // make sure we have enough space for the extra terminating 0 and for the 00 00 suffix
            size_t cbNewSize;
            if (!ClrSafeInt<size_t>::addition(cbCur, cbReq, cbNewSize) ||
                !ClrSafeInt<size_t>::addition(cbNewSize, 3, cbNewSize))
            {
                IfFailGo(COR_E_OVERFLOW);
            }
            if (cbNewSize > rEvents.MaxSize())
            {
                IfFailGo(rEvents.ReSizeNoThrow(cbNewSize));
            }
            // Do the conversion.
            WszWideCharToMultiByte(CP_UTF8,0, rEvent.Ptr(),-1, rEvents.Ptr()+cbCur,cbReq, 0,0);
            cbCur += cbReq;
            pItiIface->Release();
        }
        pItiIface = 0;

        // Add an extra terminating 0.
        *(rEvents.Ptr()+cbCur) = 0;
        ++cbCur;

        // Now build the custom attribute.
        int iLen = cbCur - 6;
        char *pBytes = rEvents.Ptr();

        // Length may be encoded with less the 4 bytes.
        int lenPad = 4 - CPackedLen::Size(iLen);
        _ASSERTE(lenPad >= 0);

        pBytes += lenPad;
        cbCur -= lenPad;

        // Prologue.
        pBytes[0] = 0x01;
        pBytes[1] = 0x00;

        CPackedLen::PutLength(pBytes + 2, iLen);

        // Zero named properties/fields.
        pBytes[cbCur + 0] = 0x00;
        pBytes[cbCur + 1] = 0x00;
        cbCur += 2;

        // Finally, store it.
        IfFailGo(GetAttrType(ATTR_COMSOURCEINTERFACES, &tkAttr));
        IfFailGo(m_pEmit->DefineCustomAttribute(m_tdTypeDef, tkAttr, pBytes, cbCur, 0));
    }

ErrExit:
    if (psAttrIface)
        pItiIface->ReleaseTypeAttr(psAttrIface);
    if (pItiIface)
        pItiIface->Release();
    // Clean up any left-over ITypeInfo*.
    for (ULONG ix=0; ix < rImplTypes.Size(); ++ix)
        if (rImplTypes[ix])
           (rImplTypes[ix])->Release();
    for (ULONG ix=0; ix < rSrcTypes.Size(); ++ix)
        if (rSrcTypes[ix])
           (rSrcTypes[ix])->Release();
    m_tkInterface = 0;
    if (m_szInterface)
        ::SysFreeString(m_szInterface), m_szInterface = 0;
    m_ImplIface = eImplIfaceNone;
    return (hr);
} // HRESULT CImportTlb::ConvCoclass()
#ifdef _PREFAST_
#pragma warning(pop)
#endif

//*****************************************************************************
// Convert an enum to a class with fields that have default values.
//*****************************************************************************
HRESULT CImportTlb::ConvEnum(           // S_OK or error.
    ITypeInfo   *pITI,                  // ITypeInfo* to convert.
    TYPEATTR    *psAttr)                // TYPEATTR of TypeInfo.
{
    HRESULT     hr;                     // A result.
    int         i;                      // Loop control.
    VARDESC     *psVar=0;               // VARDESC for a member.
    mdFieldDef  mdField;                // The FieldDef for the enum's type.

    // Create the field definition for the enum type.  Always import as an __int32.
    IfFailGo(m_pEmit->DefineField(m_tdTypeDef, ENUM_TYPE_NAME, ENUM_TYPE_FLAGS, ENUM_TYPE_SIGNATURE,ENUM_TYPE_SIGNATURE_SIZE, 
        0,0, -1, &mdField));

    // Iterate over the vars.
    for (i=0; i<psAttr->cVars; ++i)
    {
        // Get variable information.
        IfFailGo(pITI->GetVarDesc(i, &psVar));
        // Do the conversion.
        IfFailGo(_ConvConstant(pITI, psVar, true/*enum member*/));
        // Release for next var.
        pITI->ReleaseVarDesc(psVar);
        psVar = 0;
    }

    hr = S_OK;

ErrExit:
    if (psVar)
        pITI->ReleaseVarDesc(psVar);
    return (hr);
} // HRESULT CImportTlb::ConvEnum()

//*****************************************************************************
// Convert a record to a class with fields.
//*****************************************************************************
HRESULT CImportTlb::ConvRecord(         // S_OK or error.
    ITypeInfo   *pITI,                  // ITypeInfo* to convert.
    TYPEATTR    *psAttr,                // TYPEATTR of TypeInfo.
    BOOL        bUnion)                 // Convert as a union?
{
    HRESULT     hr=S_OK;                // A result.
    int         i;                      // Loop control.
    VARDESC     *psVar=0;               // VARDESC for a member.
    mdFieldDef  mdField;                // Token for a given field.
    CQuickArray<COR_FIELD_OFFSET> rLayout; // Array for layout information.
    BOOL        bConversionLoss=false;  // If true, some attributes were lost on conversion.

    // Unions with embedded Object Types can't really be converted.  Just reserve correct size.
    if (bUnion && (HasObjectFields(pITI, psAttr) == S_OK))
    {
        IfFailGo(m_pEmit->SetClassLayout(m_tdTypeDef, psAttr->cbAlignment, 0, psAttr->cbSizeInstance));
        goto ErrExit;
    }
    
    // Prepare for layout info.
    IfFailGo(rLayout.ReSizeNoThrow(psAttr->cVars+1));

    // Iterate over the vars.
    for (i=0; i<psAttr->cVars; ++i)
    {
        // Get variable information.
        IfFailGo(pITI->GetVarDesc(i, &psVar));
        // Do the conversion.
        IfFailGo(_ConvField(pITI, psVar, &mdField, bUnion));
        if (hr == S_CONVERSION_LOSS)
            bConversionLoss = true;
        // Save the layout info.
        rLayout[i].ridOfField = mdField;
        rLayout[i].ulOffset = psVar->oInst;
        // Release for next var.
        pITI->ReleaseVarDesc(psVar);
        psVar = 0;
    }

    // If it is a union, Save the layout information.
    if (bUnion)
    {
        rLayout[psAttr->cVars].ridOfField = mdFieldDefNil;
        IfFailGo(m_pEmit->SetClassLayout(m_tdTypeDef, psAttr->cbAlignment, rLayout.Ptr(), -1));
    }
    else // Not a union.  Preserve the alignment.
        IfFailGo(m_pEmit->SetClassLayout(m_tdTypeDef, psAttr->cbAlignment, 0, -1));

    // If we are marking these as serializable - do so now.
    if (m_bSerializableValueClasses)
    {
        mdToken tkAttr;
        IfFailGo(GetAttrType(ATTR_SERIALIZABLE, &tkAttr));
        DECLARE_CUSTOM_ATTRIBUTE(0);
        FINISH_CUSTOM_ATTRIBUTE();
        IfFailGo(m_pEmit->DefineCustomAttribute(m_tdTypeDef, tkAttr, PTROF_CUSTOM_ATTRIBUTE(),SIZEOF_CUSTOM_ATTRIBUTE(),0));
    }

    if (bConversionLoss)
        hr = S_CONVERSION_LOSS;

ErrExit:
    if (psVar)
        pITI->ReleaseVarDesc(psVar);
    return (hr);
} // HRESULT CImportTlb::ConvRecord()

//*****************************************************************************
// Convert an module to a class with fields that have default values.
//  @FUTURE: convert methods as PInvoke methods.
//*****************************************************************************
HRESULT CImportTlb::ConvModule(         // S_OK or error.
    ITypeInfo   *pITI,                  // ITypeInfo* to convert.
    TYPEATTR    *psAttr)                // TYPEATTR of TypeInfo.
{
    HRESULT     hr;                     // A result.
    int         i;                      // Loop control.
    VARDESC     *psVar=0;               // VARDESC for a member.

    // Iterate over the vars.
    for (i=0; i<psAttr->cVars; ++i)
    {
        // Get variable information.
        IfFailGo(pITI->GetVarDesc(i, &psVar));
        // Do the conversion.
        IfFailGo(_ConvConstant(pITI, psVar));
        // Release for next var.
        pITI->ReleaseVarDesc(psVar);
        psVar = 0;
    }

    hr = S_OK;

ErrExit:
    if (psVar)
        pITI->ReleaseVarDesc(psVar);
    return (hr);
} // HRESULT CImportTlb::ConvModule()

//*****************************************************************************
// Convert metadata for an interface.
//*****************************************************************************
HRESULT CImportTlb::ConvIface(          // S_OK or error.
    ITypeInfo   *pITI,                  // ITypeInfo* to convert.
    TYPEATTR    *psAttr,                // TYPEATTR of TypeInfo.
    BOOL        bVtblGapFuncs)          // Vtable gap functions?
{
    HRESULT     hr;                     // A result.
    ITypeInfo   *pITIBase=0;            // ITypeInfo* of base interface.
    TYPEATTR    *psAttrBase=0;          // TYPEATTR of base interface.
    ITypeInfo   *pITISelf2=0;           // ITypeInfo* of partner.
    TYPEATTR    *psAttrSelf2=0;         // TYPEATTR of partner.
    mdToken     tkImpls[3]={0,0,0};     // Token of implemented interfaces.
    int         ixImpls = 0;            // Index of current implemented interface.
    HREFTYPE    href;                   // href of base interface.
    mdToken     tkIface;                // Token for an interface.
    BOOL        fInheritsIEnum = FALSE;

    // If there is a partner interface, prefer it.
    if (pITI->GetRefTypeOfImplType(-1, &href) == S_OK)
    {
        IfFailGo(pITI->GetRefTypeInfo(href, &pITISelf2));
        IfFailGo(pITISelf2->GetTypeAttr(&psAttrSelf2));
    }

    // Base interface?
    if (psAttr->cImplTypes == 1)
    {
        IfFailGo(pITI->GetRefTypeOfImplType(0, &href));
        IfFailGo(pITI->GetRefTypeInfo(href, &pITIBase));
        IfFailGo(pITIBase->GetTypeAttr(&psAttrBase));

        // If this interface extends something other than IDispatch or IUnknown, record that
        //  fact as an "implemented interface".
        if (psAttrBase->guid != IID_IDispatch && psAttrBase->guid != IID_IUnknown)
        {
            // Get Token of the base interface.
            IfFailGo(_GetTokenForTypeInfo(pITIBase, FALSE, &tkImpls[ixImpls++]));
        }
        else
        {   // Maybe we're "funky"...
            if (pITISelf2)
            {
                pITIBase->ReleaseTypeAttr(psAttrBase);
                pITIBase->Release();
                pITIBase = 0;
                psAttrBase = 0;

                if (psAttrSelf2->cImplTypes == 1)
                {
                    IfFailGo(pITISelf2->GetRefTypeOfImplType(0, &href));
                    IfFailGo(pITISelf2->GetRefTypeInfo(href, &pITIBase));
                    IfFailGo(pITIBase->GetTypeAttr(&psAttrBase));

                    if (psAttrBase->guid != IID_IDispatch && psAttrBase->guid != IID_IUnknown)
                    {
                        // Get Token of the base interface.
                        IfFailGo(_GetTokenForTypeInfo(pITIBase, FALSE, &tkImpls[ixImpls++]));
                    }
                }
                else
                {
                    BSTR szTypeInfoName;
                    pITISelf2->GetDocumentation(MEMBERID_NIL, &szTypeInfoName, 0, 0, 0);
                    ReportEvent(NOTIF_CONVERTWARNING, TLBX_E_INVALID_TYPEINFO, szTypeInfoName);
                    SysFreeString(szTypeInfoName);
                    
                    IfFailGo(TLBX_E_INVALID_TYPEINFO);
                }
            }
        }

        pITIBase->ReleaseTypeAttr(psAttrBase);
        psAttrBase = 0;
        pITIBase->Release();
        pITIBase = 0;
    }

    if (ExplicitlyImplementsIEnumerable(pITI, psAttr) == S_OK)
        fInheritsIEnum = TRUE;
    
    // If this interface has a NewEnum member then have it implement IEnumerable.
    if ( (!fInheritsIEnum) && (HasNewEnumMember(pITI) == S_OK) )
    {
        IfFailGo(GetKnownTypeToken(VT_SLOT_FOR_IENUMERABLE, &tkIface));
        tkImpls[ixImpls++] = tkIface;
    }

    // If not processing an implemented interface, add additional interface properties. 
    if (m_ImplIface == eImplIfaceNone)
    {
        // Set base interface as an implemented interface.
        if (tkImpls[0])
            IfFailGo(m_pEmit->SetTypeDefProps(m_tdTypeDef, ULONG_MAX/*flags*/, ULONG_MAX/*extends*/, tkImpls));

        // If the interface is not derived from IDispatch mark it as IUnknown based.
        if (IsIDispatchDerived(pITI, psAttr) == S_FALSE)
        {
            mdMemberRef mr;
            // Note that this is a vtable, but not IDispatch derived.
            // Custom attribute buffer.
            DECLARE_CUSTOM_ATTRIBUTE(sizeof(short));
            // Set up the attribute.
            BUILD_CUSTOM_ATTRIBUTE(short, ifVtable);
            // Store the attribute
            IfFailGo(GetAttrType(ATTR_INTERFACETYPE, &mr));
            FINISH_CUSTOM_ATTRIBUTE();
            IfFailGo(m_pEmit->DefineCustomAttribute(m_tdTypeDef, mr, PTROF_CUSTOM_ATTRIBUTE(), SIZEOF_CUSTOM_ATTRIBUTE(), 0));
        }
    }

    // Convert the members on the interface (and base interfaces).
    // If this interface had a "funky partner", base the conversion on that.
    if (pITISelf2)
        IfFailGo(_ConvIfaceMembers(pITISelf2, psAttrSelf2, bVtblGapFuncs, psAttr->wTypeFlags & TYPEFLAG_FDUAL, fInheritsIEnum));
    else
        IfFailGo(_ConvIfaceMembers(pITI, psAttr, bVtblGapFuncs, psAttr->wTypeFlags & TYPEFLAG_FDUAL, fInheritsIEnum));

ErrExit:
    if (psAttrSelf2)
        pITISelf2->ReleaseTypeAttr(psAttrSelf2);
    if (pITISelf2)
        pITISelf2->Release();
    if (psAttrBase)
        pITIBase->ReleaseTypeAttr(psAttrBase);
    if (pITIBase)
        pITIBase->Release();
    return (hr);
} // HRESULT CImportTlb::ConvIface()

//*****************************************************************************
// Convert the metadata for a dispinterface.  Try to convert as a normal
//  interface.
//*****************************************************************************
HRESULT CImportTlb::ConvDispatch(       // S_OK or error.
    ITypeInfo   *pITI,                  // ITypeInfo* to convert.
    TYPEATTR    *psAttr,                // TYPEATTR of TypeInfo.
    BOOL        bVtblGapFuncs)          // Vtable gap functions for interface implementations?
{
    HRESULT     hr;                     // A result.
    HREFTYPE    href;                   // Base interface href.
    ITypeInfo   *pITIBase=0;            // Base interface ITypeInfo.
    TYPEATTR    *psAttrBase=0;          // TYPEATTR of base interface.
    mdMemberRef mr;                     // MemberRef for custom value.
    DWORD       attr[2] = {0x00010001, 0x00000002};
    BYTE        bIface = ifDispatch;    // Custom value means "dispinterface"
    BOOL        fInheritsIEnum = FALSE;
   
    // If this is a dual interface, treat it like a normal interface.
    if ((psAttr->wTypeFlags & TYPEFLAG_FDUAL))
    {
        hr = ConvIface(pITI, psAttr, bVtblGapFuncs);
        goto ErrExit;
    }

    if (ExplicitlyImplementsIEnumerable(pITI, psAttr) == S_OK)
        fInheritsIEnum = TRUE;

    // If there is a vtable view of this interface (funky dispinterface).
    //  @FUTURE: what would be really nice here would be an alias mechanism, so that we could
    //   just point this dispinterface to that other interface, in those situations that it
    //   is dual.  OTOH, that is probably pretty rare, because if that other interface 
    //   were dual, why would the dispinterface even be needed?
    if (pITI->GetRefTypeOfImplType(-1, &href) == S_OK)
    {
        IfFailGo(pITI->GetRefTypeInfo(href, &pITIBase));
        IfFailGo(pITIBase->GetTypeAttr(&psAttrBase));
        IfFailGo(_ConvIfaceMembers(pITIBase, psAttrBase, bVtblGapFuncs, TRUE, fInheritsIEnum));
        pITIBase->ReleaseTypeAttr(psAttrBase);
        psAttrBase = 0;
        pITIBase->Release();
        pITIBase = 0;
        goto ErrExit;
    }

    // If not processing an implemented interface, mark the interface type.
    if (m_ImplIface == eImplIfaceNone)
    {
        // If this interface has a NewEnum member then have it implement IEnumerable.        
        if ((S_OK == HasNewEnumMember(pITI)) && !fInheritsIEnum)
        {
            mdToken     tkImpl[2] = {0,0};
            IfFailGo(GetKnownTypeToken(VT_SLOT_FOR_IENUMERABLE, &tkImpl[0]));
            IfFailGo(m_pEmit->SetTypeDefProps(m_tdTypeDef, ULONG_MAX, ULONG_MAX, tkImpl));
        }

        // Note that this is a dispinterface.
        DECLARE_CUSTOM_ATTRIBUTE(sizeof(short));
        // Set up the attribute.
        BUILD_CUSTOM_ATTRIBUTE(short, ifDispatch);
        // Store the attribute
        IfFailGo(GetAttrType(ATTR_INTERFACETYPE, &mr));
        FINISH_CUSTOM_ATTRIBUTE();
        IfFailGo(m_pEmit->DefineCustomAttribute(m_tdTypeDef, mr, PTROF_CUSTOM_ATTRIBUTE(), SIZEOF_CUSTOM_ATTRIBUTE(), 0));
    }

    IfFailGo(_ConvDispatchMembers(pITI, psAttr, fInheritsIEnum));

ErrExit:
    if (psAttrBase)
        pITIBase->ReleaseTypeAttr(psAttrBase);
    if (pITIBase)
        pITIBase->Release();
    return (hr);
} // HRESULT CImportTlb::ConvDispatch()

//*****************************************************************************
// Determine if an interface is derived from IUnknown.
//*****************************************************************************
HRESULT CImportTlb::IsIUnknownDerived(
    ITypeInfo   *pITI,                  // The containing ITypeInfo.
    TYPEATTR    *psAttr)                // The ITypeInfo's TYPEATTR
{
    HRESULT     hr=S_OK;                // A result.

    HREFTYPE    href;                   // Base interface href.
    ITypeInfo   *pITIBase=0;            // Base interface ITypeInfo.
    TYPEATTR    *psAttrBase=0;          // TYPEATTR of base interface.

    // This should never be called on CoClasses.
    _ASSERTE(psAttr->typekind != TKIND_COCLASS);

    // If IDispatch or IUnknown, we've recursed far enough.
    if (IsEqualGUID(psAttr->guid, IID_IUnknown) || IsEqualGUID(psAttr->guid, IID_IDispatch))
    {
        goto ErrExit;
    }

    // Handle base interface.
    if (psAttr->cImplTypes == 1)
    {
        IfFailGo(pITI->GetRefTypeOfImplType(0, &href));
        IfFailGo(pITI->GetRefTypeInfo(href, &pITIBase));
        IfFailGo(pITIBase->GetTypeAttr(&psAttrBase));

        // IUnknow derived if base interface is.
        hr = IsIUnknownDerived(pITIBase, psAttrBase);
        pITIBase->ReleaseTypeAttr(psAttrBase);
        psAttrBase = 0;
        pITIBase->Release();
        pITIBase = 0;
    }
    else
    {   // No base interface, not IUnknown, not IDispatch.  Not very COM-ish, so don't try to handle.
        hr = S_FALSE;
    }

ErrExit:
    if (psAttrBase)
        pITIBase->ReleaseTypeAttr(psAttrBase);
    if (pITIBase)
        pITIBase->Release();
    return (hr);
} // HRESULT CImportTlb::IsIUnknownDerived()

//*****************************************************************************
// Determine if an interface is derived from IDispatch.  Note that a pure
//  dispinterface doesn't derive from IDispatch.
//*****************************************************************************
HRESULT CImportTlb::IsIDispatchDerived(
    ITypeInfo   *pITI,                  // The containing ITypeInfo.
    TYPEATTR    *psAttr)                // The ITypeInfo's TYPEATTR
{
    HRESULT     hr=S_OK;                // A result.

    HREFTYPE    href;                   // Base interface href.
    ITypeInfo   *pITIBase=0;            // Base interface ITypeInfo.
    TYPEATTR    *psAttrBase=0;          // TYPEATTR of base interface.

    // If IDispatch, we've recursed far enough.
    if (IsEqualGUID(psAttr->guid, IID_IDispatch))
    {
        goto ErrExit;
    }

    if (psAttr->typekind == TKIND_DISPATCH)
    {
        IfFailGo(pITI->GetRefTypeOfImplType(-1, &href));
        IfFailGo(pITI->GetRefTypeInfo(href, &pITIBase));
        IfFailGo(pITIBase->GetTypeAttr(&psAttrBase));

        // IDispatch derived if base interface is.
        hr = IsIDispatchDerived(pITIBase, psAttrBase);
        pITIBase->ReleaseTypeAttr(psAttrBase);
        psAttrBase = 0;
        pITIBase->Release();
        pITIBase = 0;
        
        goto ErrExit;
    }
    
    // Handle base interface.
    if (psAttr->cImplTypes == 1)
    {
        IfFailGo(pITI->GetRefTypeOfImplType(0, &href));
        IfFailGo(pITI->GetRefTypeInfo(href, &pITIBase));
        IfFailGo(pITIBase->GetTypeAttr(&psAttrBase));

        // IDispatch derived if base interface is.
        hr = IsIDispatchDerived(pITIBase, psAttrBase);
        pITIBase->ReleaseTypeAttr(psAttrBase);
        psAttrBase = 0;
        pITIBase->Release();
        pITIBase = 0;
    }
    else
    {   // No base interface, not IDispatch.  Done.
        hr = S_FALSE;
    }

ErrExit:
    if (psAttrBase)
        pITIBase->ReleaseTypeAttr(psAttrBase);
    if (pITIBase)
        pITIBase->Release();
    return (hr);
} // HRESULT CImportTlb::IsIDispatchDerived()

//*****************************************************************************
// Determine if an interface has a member with a DISPID of DISPID_NEWENUM.
//*****************************************************************************
HRESULT CImportTlb::HasNewEnumMember(   // S_OK if has NewEnum, S_FALSE otherwise.
    ITypeInfo   *pItfTI)                // The interface in question.
{
    HRESULT     hr = S_OK;              // A result.
    BOOL        bHasNewEnumMember=FALSE;// If true, has a NewEnum
    TYPEATTR    *pAttr = NULL;          // A TypeInfo's typeattr
    FUNCDESC    *pFuncDesc = NULL;      // A Function's FuncDesc
    VARDESC     *pVarDesc = NULL;       // A properties VarDesc
    int         i;                      // Loop control.
    ITypeInfo   *pITISelf2=0;           // Partner interface.
    HREFTYPE    href;                   // HREF of partner.
    WCHAR       IEnumCA[] = W("{CD2BC5C9-F452-4326-B714-F9C539D4DA58}");


    // If there is a partner interface, prefer it.
    if (pItfTI->GetRefTypeOfImplType(-1, &href) == S_OK)
    {
        IfFailGo(pItfTI->GetRefTypeInfo(href, &pITISelf2));
        pItfTI = pITISelf2;
    }

    // Retrieve the attributes of the interface.
    IfFailGo(pItfTI->GetTypeAttr(&pAttr));   

    if ((pAttr->typekind == TKIND_DISPATCH) || ((pAttr->typekind == TKIND_INTERFACE) && (IsIDispatchDerived(pItfTI, pAttr) == S_OK)))
    {
        // Check to see if the ForceIEnumerable custom value exists on the type
        _ForceIEnumerableCVExists(pItfTI, &bHasNewEnumMember);

        // Check to see if the interface has a function with a DISPID of DISPID_NEWENUM.
        for (i = 0; i < pAttr->cFuncs; i++)
        {
            IfFailGo(TryGetFuncDesc(pItfTI, i, &pFuncDesc)); 
            
            if (FuncIsNewEnum(pItfTI, pFuncDesc, i) == S_OK)
            {
                // Throw a warning if we find more than one func with DISPID_NEWENUM.
                if (bHasNewEnumMember == TRUE)
                {
                    BSTR ObjectName;
                    pItfTI->GetDocumentation(-1, &ObjectName, NULL, NULL, NULL);
                    ReportEvent(NOTIF_CONVERTWARNING, TLBX_E_INVALID_TYPEINFO, ObjectName);
                    SysFreeString(ObjectName);
                }

                // The interface has a function with a DISPID of DISPID_NEWENUM.
                bHasNewEnumMember = TRUE;
                break;
            }
            
            pItfTI->ReleaseFuncDesc(pFuncDesc);
            pFuncDesc = NULL;
        }

        // Check to see if the interface as a property with a DISPID of DISPID_NEWENUM.
        for (i = 0; i < pAttr->cVars; i++)
        {
            IfFailGo(pItfTI->GetVarDesc(i, &pVarDesc));

            if (PropertyIsNewEnum(pItfTI, pVarDesc, i) == S_OK)
            {
                // Throw a warning if we find more than one func with DISPID_NEWENUM.
                if (bHasNewEnumMember == TRUE)
                {
                    BSTR ObjectName;
                    pItfTI->GetDocumentation(-1, &ObjectName, NULL, NULL, NULL);
                    ReportEvent(NOTIF_CONVERTWARNING, TLBX_E_INVALID_TYPEINFO, ObjectName);
                    SysFreeString(ObjectName);
                }

                // The interface has a property with a DISPID of DISPID_NEWENUM.
                bHasNewEnumMember = TRUE;
                break;
            }
            
            pItfTI->ReleaseVarDesc(pVarDesc);
            pVarDesc = NULL;
        }
    }
    else
    {
        // Check to see if the ForceIEnumerable custom value exists on the type
        //  If it does, spit out a warning.
        _ForceIEnumerableCVExists(pItfTI, &bHasNewEnumMember);

        if (bHasNewEnumMember)
        {
            // Invalid custom attribute on the iface.
            BSTR CustomValue = SysAllocString((const WCHAR*)&IEnumCA[0]);
            BSTR ObjectName;
            pItfTI->GetDocumentation(-1, &ObjectName, NULL, NULL, NULL);
            
            ReportEvent(NOTIF_CONVERTWARNING, TLBX_W_IENUM_CA_ON_IUNK, CustomValue, ObjectName);

            SysFreeString(CustomValue);
            SysFreeString(ObjectName);

            bHasNewEnumMember = FALSE;
        }
    }

    hr = bHasNewEnumMember ? S_OK : S_FALSE;

ErrExit:
    if (pAttr)
        pItfTI->ReleaseTypeAttr(pAttr);
    if (pFuncDesc)
        pItfTI->ReleaseFuncDesc(pFuncDesc);
    if (pVarDesc)
        pItfTI->ReleaseVarDesc(pVarDesc);
    if (pITISelf2)
        pITISelf2->Release();
    return hr;
} // HRESULT CImportTlb::HasNewEnumMember(ITypeInfo *pItfTI)

//*****************************************************************************
// Determine if a given function is a valid NewEnum member.
//*****************************************************************************
HRESULT CImportTlb::FuncIsNewEnum(      // S_OK if the function is the NewEnum member S_FALSE otherwise.
    ITypeInfo *pITI,                    // The ITypeInfo that contains the function.                                    
    FUNCDESC *pFuncDesc,                // The function in question.
    DWORD index)                        // The function index
{

    HRESULT         hr = S_OK;
    BOOL            bIsValidNewEnum = FALSE;
    TYPEDESC*       pType = NULL;
    TYPEATTR*       pAttr = NULL;
    ITypeInfo*      pITIUD = NULL; 
    long            lDispSet = 0;
    
    _GetDispIDCA(pITI, index, &lDispSet, TRUE);

    if ((pFuncDesc->memid == DISPID_NEWENUM) || (lDispSet == DISPID_NEWENUM))
    {
        if (pFuncDesc->funckind == FUNC_DISPATCH)
        {
            if ((pFuncDesc->invkind == INVOKE_PROPERTYGET) || (pFuncDesc->invkind == INVOKE_FUNC))
            {
                if (pFuncDesc->cParams == 0)
                {
                    pType = &pFuncDesc->elemdescFunc.tdesc;
                }
                else if ((m_bTransformDispRetVals) && (pFuncDesc->cParams == 1) && (pFuncDesc->lprgelemdescParam[0].paramdesc.wParamFlags & PARAMFLAG_FRETVAL))
                {
                    pType = pFuncDesc->lprgelemdescParam[0].tdesc.lptdesc;
                }
            }
        }
        else if (pFuncDesc->funckind == FUNC_PUREVIRTUAL)
        {
            if ((pFuncDesc->cParams == 1) &&
                ((pFuncDesc->invkind == INVOKE_PROPERTYGET) || (pFuncDesc->invkind == INVOKE_FUNC)) &&
                (pFuncDesc->lprgelemdescParam[0].paramdesc.wParamFlags & PARAMFLAG_FRETVAL) &&
                (pFuncDesc->lprgelemdescParam[0].tdesc.vt == VT_PTR))
            {
                pType = pFuncDesc->lprgelemdescParam[0].tdesc.lptdesc;
            }
        }

        if (pType)
        {
            if (pType->vt == VT_UNKNOWN || pType->vt == VT_DISPATCH)
            {
                // The member returns an IUnknown * or an IDispatch * which is valid.
                bIsValidNewEnum = TRUE;
            }
            else if (pType->vt == VT_PTR)
            {
                pType =  pType->lptdesc;
                if (pType->vt == VT_USERDEFINED)
                {
                    IfFailGo(pITI->GetRefTypeInfo(pType->hreftype, &pITIUD));
                    IfFailGo(pITIUD->GetTypeAttr(&pAttr));
                    if (IsEqualGUID(pAttr->guid, IID_IEnumVARIANT) || 
                        IsEqualGUID(pAttr->guid, IID_IUnknown) ||
                        IsEqualGUID(pAttr->guid, IID_IDispatch))
                    {
                        // The member returns a valid interface type for a NewEnum member.
                        bIsValidNewEnum = TRUE;
                    }
                }
            }
        }
    }        

ErrExit:
    if (pAttr)
        pITIUD->ReleaseTypeAttr(pAttr);
    if (pITIUD)
        pITIUD->Release();
    
    if (FAILED(hr))
        return hr;
    else 
        return bIsValidNewEnum ? S_OK : S_FALSE;
} // HRESULT CImportTlb::FuncIsNewEnum(FUNCDESC *pFuncDesc)

//*****************************************************************************
// Determine if a given function is a valid NewEnum member.
//*****************************************************************************
HRESULT CImportTlb::PropertyIsNewEnum(    // S_OK if the function is the NewEnum member S_FALSE otherwise.
    ITypeInfo *pITI,                      // The ITypeInfo that contains the property.
    VARDESC *pVarDesc,                    // The function in question.
    DWORD index)                          // The property index.
{
    HRESULT         hr = S_OK;
    BOOL            bIsValidNewEnum = FALSE;
    TYPEDESC*       pType = NULL;
    TYPEATTR*       pAttr = NULL;
    ITypeInfo*      pITIUD = NULL; 
    long            lDispSet = 0;

    _GetDispIDCA(pITI, index, &lDispSet, FALSE);
  
    if ( ((pVarDesc->memid == DISPID_NEWENUM) || (lDispSet == DISPID_NEWENUM)) && 
        (pVarDesc->elemdescVar.paramdesc.wParamFlags & PARAMFLAG_FRETVAL) &&
        (pVarDesc->wVarFlags & VARFLAG_FREADONLY))
    {
        pType = &pVarDesc->elemdescVar.tdesc;
        if (pType->vt == VT_UNKNOWN || pType->vt == VT_DISPATCH)
        {
            // The member returns an IUnknown * or an IDispatch * which is valid.
            bIsValidNewEnum = TRUE;
        }
        else if (pType->vt == VT_PTR)
        {
            pType =  pType->lptdesc;
            if (pType->vt == VT_USERDEFINED)
            {
                IfFailGo(pITI->GetRefTypeInfo(pType->hreftype, &pITIUD));
                IfFailGo(pITIUD->GetTypeAttr(&pAttr));
                if (IsEqualGUID(pAttr->guid, IID_IEnumVARIANT) || 
                    IsEqualGUID(pAttr->guid, IID_IUnknown) ||
                    IsEqualGUID(pAttr->guid, IID_IDispatch))
                {
                    // The member returns a valid interface type for a NewEnum member.
                    bIsValidNewEnum = TRUE;
                }
            }
        }
    }

ErrExit:
    if (pAttr)
        pITIUD->ReleaseTypeAttr(pAttr);
    if (pITIUD)
        pITIUD->Release();

    if (FAILED(hr))
        return hr;
    else 
        return bIsValidNewEnum ? S_OK : S_FALSE;
} // HRESULT CImportTlb::FuncIsNewEnum(FUNCDESC *pFuncDesc)

//*****************************************************************************
// Determine is a TypeInfo has any object fields.
//*****************************************************************************
HRESULT CImportTlb::HasObjectFields(    // S_OK, S_FALSE, or error.
    ITypeInfo   *pITI,                  // The TypeInfo in question.
    TYPEATTR    *psAttr)                // Attributes of the typeinfo.
{
    HRESULT     hr;                     // A result.
    
    int         i;                      // Loop control.
    VARDESC     *psVar=0;               // VARDESC for a member.

    // Iterate over the vars.
    for (i=0; i<psAttr->cVars; ++i)
    {
        // Get variable information.
        IfFailGo(pITI->GetVarDesc(i, &psVar));
        
        // See if it is an object type.
        IfFailGo(IsObjectType(pITI, &psVar->elemdescVar.tdesc));
        // If result is S_FALSE, not an Object; keep looking.
        if (hr == S_OK)
            goto ErrExit;
        
        // Release for next var.
        pITI->ReleaseVarDesc(psVar);
        psVar = 0;
    }

    hr = S_FALSE;    
    
ErrExit:
    if (psVar)
        pITI->ReleaseVarDesc(psVar);
    return hr;    
} // HRESULT CImportTlb::HasObjectFields()

//*****************************************************************************
// Is a given type an Object type?
//*****************************************************************************
HRESULT CImportTlb::IsObjectType(       // S_OK, S_FALSE, or error.
    ITypeInfo   *pITI,                  // The TypeInfo in question.
    const TYPEDESC *pType)              // The type.
{
    HRESULT     hr;                     // A result.
    TYPEDESC    tdTemp;                 // Copy of TYPEDESC, for R/W.
    ITypeInfo   *pITIAlias=0;           // Typeinfo of the aliased type.
    TYPEATTR    *psAttrAlias=0;         // TYPEATTR of the aliased typeinfo.
    int         bObjectField=false;     // The question to be answered.
    int         iByRef=0;               // Indirection.

    // Strip off leading VT_PTR and VT_BYREF
    while (pType->vt == VT_PTR)
        pType = pType->lptdesc, ++iByRef;
    if (pType->vt & VT_BYREF)
    {
        tdTemp = *pType;
        tdTemp.vt &= ~VT_BYREF;
        pType = &tdTemp;
        ++iByRef;
    }

    // Determine if the field is/has object type.
    switch (pType->vt)
    { 
    case VT_PTR:
        _ASSERTE(!"Should not have VT_PTR here");
        break;

    // These are object types.
    case VT_BSTR:
    case VT_DISPATCH:
    case VT_VARIANT:
    case VT_UNKNOWN:
    case VT_SAFEARRAY:
    case VT_LPSTR:
    case VT_LPWSTR:
        bObjectField = true;
        break;

    // A user-defined may or may not be/contain Object type.
    case VT_USERDEFINED:
        // User defined type.  Get the TypeInfo.
        IfFailGo(pITI->GetRefTypeInfo(pType->hreftype, &pITIAlias));
        IfFailGo(pITIAlias->GetTypeAttr(&psAttrAlias));

        // Some user defined class.  Is it a value class, or a VOS class?
        switch (psAttrAlias->typekind)
        {
        // Alias -- Is the aliased thing an Object type?
        case TKIND_ALIAS:
            hr = IsObjectType(pITIAlias, &psAttrAlias->tdescAlias);
            goto ErrExit;
        // Record/Enum/Union -- Does it contain an Object type?
        case TKIND_RECORD:
        case TKIND_ENUM:
        case TKIND_UNION:
            // Byref/Ptrto record is Object.  Contained record might be.
            if (iByRef)
                bObjectField = true;
            else
            {
                hr = HasObjectFields(pITIAlias, psAttrAlias);
                goto ErrExit;
            }
            break;
        // Class/Interface -- An Object Type.
        case TKIND_INTERFACE:
        case TKIND_DISPATCH:
        case TKIND_COCLASS:
            bObjectField = true;
            break;
        default:
            //case TKIND_MODULE: -- can't pass one of these as a parameter.
            _ASSERTE(!"Unexpected typekind for user defined type");
            bObjectField = true;
        } // switch (psAttrAlias->typekind)
        break;

    case VT_CY:
    case VT_DATE:
    case VT_DECIMAL:
        // Pointer to the value type is an object.  Contained one isn't.
        if (iByRef)
            bObjectField = true;
        else
            bObjectField = false;
        break;

    // A fixed array is an Object type.
    case VT_CARRAY:
        bObjectField = true;
        break;

    // Other types I4, etc., are not Object types.
    default:
        bObjectField = false;
        break;
    } // switch (vt=pType->vt)


    hr = bObjectField ? S_OK : S_FALSE;

ErrExit:
    if (psAttrAlias)
        pITIAlias->ReleaseTypeAttr(psAttrAlias);
    if (pITIAlias)
        pITIAlias->Release();

    return hr;
} // HRESULT CImportTlb::IsObjectType()

//*****************************************************************************
// Convert the functions on an interface.  Convert the functions on the
//  base interface first, because in COM Classic, parent's functions are also
//  in the derived interface's vtable.
//*****************************************************************************
HRESULT CImportTlb::_ConvIfaceMembers(
    ITypeInfo   *pITI,                  // The containing ITypeInfo.
    TYPEATTR    *psAttr,                // The ITypeInfo's TYPEATTR
    BOOL        bVtblGapFuncs,          // Add functions for vtblGaps?
    BOOL        bAddDispIds,            // Add DispIds to the member?
    BOOL        bInheritsIEnum)         // Inherits from IEnumerable.
{
    HRESULT     hr=S_OK;                // A result.
    int         i;                      // Loop control.
    FUNCDESC    *psFunc=0;              // FUNCDESC for a member.

    HREFTYPE    href;                   // Base interface href.
    ITypeInfo   *pITIBase=0;            // Base interface ITypeInfo.
    TYPEATTR    *psAttrBase=0;          // TYPEATTR of base interface.
    BOOL        bConversionLoss=false;  // If true, some attributes were lost on conversion.

    _ASSERTE( (psAttr->typekind == TKIND_INTERFACE) || (psAttr->typekind == TKIND_DISPATCH) );

    // If IDispatch or IUnknown, we've recursed far enough.
    if (IsEqualGUID(psAttr->guid, IID_IUnknown) || IsEqualGUID(psAttr->guid, IID_IDispatch))
    {
        if (m_cbVtableSlot == 0)
        {
            m_cbVtableSlot = psAttr->cbSizeInstance;
        }
        m_Slot = (psAttr->cbSizeVft / m_cbVtableSlot);
        goto ErrExit;
    }

    // Handle base interface.
    if (psAttr->cImplTypes == 1)
    {
        IfFailGo(pITI->GetRefTypeOfImplType(0, &href));
        IfFailGo(pITI->GetRefTypeInfo(href, &pITIBase));
        IfFailGo(pITIBase->GetTypeAttr(&psAttrBase));

        IfFailGo(_ConvIfaceMembers(pITIBase, psAttrBase, bVtblGapFuncs, bAddDispIds, bInheritsIEnum));
        
        pITIBase->ReleaseTypeAttr(psAttrBase);
        psAttrBase = 0;
        pITIBase->Release();
        pITIBase = 0;
    }
    else
    {   // No base interface, not IUnknown, not IDispatch.  We shouldn't be here.
        m_Slot = 0;
        if (m_cbVtableSlot == 0)
        {
            m_cbVtableSlot = psAttr->cbSizeInstance;
        }
        _ASSERTE(!"Interface does not derive from IUnknown.");
    }

    // Loop over functions.
    IfFailGo(_FindFirstUserMethod(pITI, psAttr, &i));
    IfFailGo(BuildMemberList(pITI, i, psAttr->cFuncs, bInheritsIEnum));

    BOOL bAllowIEnum = !bInheritsIEnum;
    for (i=0; i<(int)m_MemberList.Size(); ++i)
    {
        // Convert the function.
        IfFailGo(_ConvFunction(pITI, &m_MemberList[i], bVtblGapFuncs, bAddDispIds, FALSE, &bAllowIEnum));
        if (hr == S_CONVERSION_LOSS)
            bConversionLoss = true;
    }

    // Add the property info.
    IfFailGo(_ConvPropertiesForFunctions(pITI, psAttr));
    
    if (bConversionLoss)
        hr = S_CONVERSION_LOSS;

ErrExit:
    // Release FuncDescs.
    FreeMemberList(pITI);

    if (psAttrBase)
        pITIBase->ReleaseTypeAttr(psAttrBase);
    if (pITIBase)
        pITIBase->Release();
    if (psFunc)
        pITI->ReleaseFuncDesc(psFunc);
    return (hr);
} // HRESULT CImportTlb::_ConvIfaceMembers()

//*****************************************************************************
// Convert the functions on a source interface to add_ and remove_ method.  
// Convert the functions on the base interface first, because in COM Classic, 
// parent's functions are also in the derived interface's vtable.
//*****************************************************************************
HRESULT CImportTlb::_ConvSrcIfaceMembers(
    ITypeInfo   *pITI,                  // The containing ITypeInfo.
    TYPEATTR    *psAttr,                // The ITypeInfo's TYPEATTR
    BOOL        fInheritsIEnum)
{
    HRESULT     hr=S_OK;                // A result.
    int         i;                      // Loop control.
    FUNCDESC    *psFunc=0;              // FUNCDESC for a member.
    HREFTYPE    href;                   // Base interface href.
    ITypeInfo   *pITIBase=0;            // Base interface ITypeInfo.
    TYPEATTR    *psAttrBase=0;          // TYPEATTR of base interface.
    BOOL        bConversionLoss=false;  // If true, some attributes were lost on conversion.

    _ASSERTE( (psAttr->typekind == TKIND_INTERFACE) || (psAttr->typekind == TKIND_DISPATCH) );

    // If IDispatch or IUnknown, we've recursed far enough.
    if (IsEqualGUID(psAttr->guid, IID_IUnknown) || IsEqualGUID(psAttr->guid, IID_IDispatch))
    {
        if (m_cbVtableSlot == 0)
        {
            m_cbVtableSlot = psAttr->cbSizeInstance;
        }
        m_Slot = (psAttr->cbSizeVft / m_cbVtableSlot);
        goto ErrExit;
    }

    // Handle base interface.
    if (psAttr->cImplTypes == 1)
    {
        IfFailGo(pITI->GetRefTypeOfImplType(0, &href));
        IfFailGo(pITI->GetRefTypeInfo(href, &pITIBase));
        IfFailGo(pITIBase->GetTypeAttr(&psAttrBase));

        IfFailGo(_ConvSrcIfaceMembers(pITIBase, psAttrBase, fInheritsIEnum));
        pITIBase->ReleaseTypeAttr(psAttrBase);
        psAttrBase = 0;
        pITIBase->Release();
        pITIBase = 0;
    }
    else
    {   // No base interface, not IUnknown, not IDispatch.  We shouldn't be here.
        m_Slot = 0;
        if (m_cbVtableSlot == 0)
        {
            m_cbVtableSlot = psAttr->cbSizeInstance;
        }
        _ASSERTE(!"Interface does not derive from IUnknown.");
    }

    // Loop over functions.
    IfFailGo(_FindFirstUserMethod(pITI, psAttr, &i));
    IfFailGo(BuildMemberList(pITI, i, psAttr->cFuncs, fInheritsIEnum));

    // If we have any properties, we want to skip them.  Should we add gaps?
    if (m_cMemberProps != 0)
    {
        ReportEvent(NOTIF_CONVERTWARNING, TLBX_W_NO_PROPS_IN_EVENTS, m_szName);
        bConversionLoss = true;
    }

    for (i = m_cMemberProps; i<(int)m_MemberList.Size(); ++i)
    {
        // Convert the function.
        IfFailGo(_GenerateEvent(pITI, &m_MemberList[i], fInheritsIEnum));
        if (hr == S_CONVERSION_LOSS)
            bConversionLoss = true;
    }
    
    if (bConversionLoss)
        hr = S_CONVERSION_LOSS;

ErrExit:
    // Release FuncDescs.
    FreeMemberList(pITI);

    if (psAttrBase)
        pITIBase->ReleaseTypeAttr(psAttrBase);
    if (pITIBase)
        pITIBase->Release();
    if (psFunc)
        pITI->ReleaseFuncDesc(psFunc);
    return (hr);
} // HRESULT CImportTlb::_ConvIfaceMembers()

//*****************************************************************************
// Add the property definitions for property functions.
//*****************************************************************************
HRESULT CImportTlb::_ConvPropertiesForFunctions(
    ITypeInfo   *pITI,                  // ITypeInfo* being converted.
    TYPEATTR    *psAttr)                // TypeAttr for the typeinfo.
{
    HRESULT     hr=S_OK;                // A result.
    int         ix;                     // Loop control.
    int         ix2;                    // More loop control.
    mdProperty  pd;                     // A property token.
    USHORT      ms;                     // Some method's semantics.
    mdToken     tk;                     // A method's token.
    mdMethodDef mdFuncs[6] ={0};        // Array of setter, getter, other.
    FUNCDESC    *psF=0;                 // FUNCDESC of Get, Put, or PutRef.
    TYPEDESC    *pProperty;             // TYPEDESC of property type.
    BOOL        bPropRetval;            // Is the property type a [retval]?
    ULONG       ixValue;                // Index of the value parameter for putters.
    int         ixVarArg;               // Index of vararg param, if any.
    CQuickBytes qbComSig;               // new signature 
    BYTE        *pbSig;                 // Pointer into the signature.
    ULONG       sigFlags;               // Signature handling flags.
    ULONG       cbTotal;                // Size of the signature.
    ULONG       cb;                     // Size of a signature element.
    LPWSTR      pszName;                // Possibly decorated name of property.
    CQuickArray<WCHAR> qbName;          // Buffer for name decoration.
    int         iSrcParam;              // Param count, as looping through params.
    int         cDestParams;            // Count of destination params.
    CQuickArray<BYTE> qbDummyNativeTypeBuf; // A dummy native type array.
    ULONG       iNativeOfs=0;           // Current offset in native type buffer.
    BOOL        bNewEnumMember=FALSE;   // Is this a NewEnum property?
    BOOL        bConversionLoss=FALSE;  // Was some type not fully converted?    
    int         cFound;                 // Functions found matching a given property.
    
    // Using semantics as an index, so be sure array is big enough.
    _ASSERTE(lengthof(mdFuncs) > msOther);
    
    for (ix=m_cMemberProps; ix<(int)m_MemberList.Size(); ++ix)
    {   // See if this one needs to be processed.
        if (m_MemberList[ix].m_mdFunc == 0)
            continue;
        
        MemberInfo *pMember = &m_MemberList[ix];
        pMember->GetFuncInfo(tk, ms);
        
        // Get the name.
        if (m_szMember)
            ::SysFreeString(m_szMember), m_szMember = 0;
        IfFailGo(pITI->GetDocumentation(pMember->m_psFunc->memid, &m_szMember, 0,0,0));
        
        // Found one.  Put in the right slot.
        _ASSERTE(ms == msGetter || ms == msSetter || ms==msOther);
        mdFuncs[msSetter] = mdFuncs[msGetter] = mdFuncs[msOther] = 0;
        mdFuncs[ms] = tk;
        pMember->m_mdFunc = 0;
        
        // Look for related functions.
        cFound = 1;
        for (ix2=ix+1; ix2<(int)m_MemberList.Size(); ++ix2)
        {
            MemberInfo *pMember2 = &m_MemberList[ix2];
            if (pMember2->m_mdFunc != 0 && pMember2->m_psFunc->memid == pMember->m_psFunc->memid)
            {   // Found a related function.
                pMember2->GetFuncInfo(tk, ms);
                _ASSERTE(ms == msGetter || ms == msSetter || ms==msOther);
                _ASSERTE(mdFuncs[ms] == 0);
                mdFuncs[ms] = tk;
                pMember2->m_mdFunc = 0;
                // If have found all three, don't bother looking for more.
                if (++cFound == 3)
                    break;
            }
        }
        
        // Build the signature for the property.
        hr = _GetFunctionPropertyInfo(pMember->m_psFunc, &ms, &psF, &pProperty, &bPropRetval, TRUE, m_szMember);
        
        // The function really should have a property associated with it, to get here.  Check anyway.
        _ASSERTE(pProperty);
        if (!pProperty)
            continue;

        // Some sort of property accessor.
        IfFailGo(qbComSig.ReSizeNoThrow(CB_MAX_ELEMENT_TYPE + 1));
        pbSig = (BYTE *)qbComSig.Ptr();
        cbTotal = cb = CorSigCompressData((ULONG)IMAGE_CEE_CS_CALLCONV_PROPERTY, pbSig);
        // Count of parameters.
        
        // If this is a getter, see if there is a retval.
        if (psF->invkind == INVOKE_PROPERTYGET)
        {   // Examine each param, and count all except the [retval].
            for (cDestParams=iSrcParam=0; iSrcParam<psF->cParams; ++iSrcParam)
            {
                if ((psF->lprgelemdescParam[iSrcParam].paramdesc.wParamFlags & NON_CONVERTED_PARAMS_FLAGS) == 0)
                    ++cDestParams;
            }
            // There is no new value param for getters.
            ixValue = -1;
        }
        else
        {   
            // This is a putter, so 1 param is new value, others are indices (or lcid).
            for (cDestParams=iSrcParam=0; iSrcParam<psF->cParams-1; ++iSrcParam)
            {
                if ((psF->lprgelemdescParam[iSrcParam].paramdesc.wParamFlags & NON_CONVERTED_PARAMS_FLAGS) == 0)
                    ++cDestParams;
            }            
            // The last parameter is the new value.
            ixValue = psF->cParams - 1;
        }

        //-------------------------------------------------------------------------
        // See if there is a vararg param.
        ixVarArg = psF->cParams + 1;
        if (psF->cParamsOpt == -1)
        {
            // If this is a PROPERTYPUT or PROPERTYPUTREF, skip the last non-retval parameter (it
            //  is the new value to be set).
            BOOL bPropVal = (psF->invkind & (INVOKE_PROPERTYPUT | INVOKE_PROPERTYPUTREF)) ? TRUE : FALSE;
            // Find the vararg param.
            for (iSrcParam=psF->cParams-1; iSrcParam>=0; --iSrcParam)
            {
                // The count of optional params does not include any lcid params, nor does
                //  it include the return value, so skip those.
                if ((psF->lprgelemdescParam[iSrcParam].paramdesc.wParamFlags & (PARAMFLAG_FRETVAL|PARAMFLAG_FLCID)) != 0)
                    continue;
                // If haven't yet seen the property value, this param is it, so skip it, too.
                if (bPropVal)
                {
                    bPropVal = FALSE;
                    continue;
                }
                ixVarArg = iSrcParam;
                break;
            } // for (iSrcParam=cParams-1...
        }
        
        // Put in the count of index parameters.
        _ASSERTE(cDestParams >= 0);
        cb = CorSigCompressData(cDestParams, &pbSig[cbTotal]);
        cbTotal += cb;

        // Create the signature for the property type.
        sigFlags = SIG_ELEM | (bPropRetval ? SIG_RET : (SigFlags)0);
        IfFailGo(_ConvSignature(pITI, pProperty, sigFlags, qbComSig, cbTotal, &cbTotal, qbDummyNativeTypeBuf, 0, &iNativeOfs, bNewEnumMember));
        if (hr == S_CONVERSION_LOSS)
            bConversionLoss = true;

        // Fill in the "index" part of the property's signature.
        for (iSrcParam=0; iSrcParam<psF->cParams; ++iSrcParam)
        {
            if (psF->lprgelemdescParam[iSrcParam].paramdesc.wParamFlags & NON_CONVERTED_PARAMS_FLAGS)
                continue;
            if (iSrcParam == static_cast<int>(ixValue))
                continue;
            sigFlags = SIG_FUNC | SIG_USE_BYREF;
            if (iSrcParam == ixVarArg)
                sigFlags |= SIG_VARARG;
            IfFailGo(_ConvSignature(pITI, &psF->lprgelemdescParam[iSrcParam].tdesc, sigFlags, qbComSig, cbTotal, &cbTotal, qbDummyNativeTypeBuf, 0, &iNativeOfs, bNewEnumMember));
            if (hr == S_CONVERSION_LOSS)
                bConversionLoss = true;
        }

        // Get the property name.  Add interface name and make unique, if needed.
        // m_szInterface should be non-null if processing an implemented interface; should be null otherwise.
        _ASSERTE(m_ImplIface == eImplIfaceNone || m_szInterface != 0);
        IfFailGo(qbName.ReSizeNoThrow(wcslen(m_szMember)+2));
        wcscpy_s(qbName.Ptr(), wcslen(m_szMember)+2, m_szMember); 
        IfFailGo(GenerateUniqueMemberName(qbName, (PCCOR_SIGNATURE)qbComSig.Ptr(), cbTotal, m_szInterface, mdtProperty));
        pszName = qbName.Ptr();

        // Define the property.
        IfFailGo(m_pEmit->DefineProperty(m_tdTypeDef, pszName, 0/*dwFlags*/, 
                        (PCCOR_SIGNATURE) qbComSig.Ptr(), cbTotal, 0, 0, -1, 
                        mdFuncs[msSetter], mdFuncs[msGetter], &mdFuncs[msOther], 
                        &pd));

        // Handle dispids for non-implemented interfaces, and for default interface
        if (m_ImplIface != eImplIface)
        {
            // Set the dispid CA on the property.
            long lDispSet = 1;
            _SetDispIDCA(pITI, pMember->m_iMember, psF->memid, pd, TRUE, &lDispSet, TRUE);

            // If this property is default property, add a custom attribute to the class.
            if (lDispSet == DISPID_VALUE)
                IfFailGo(_AddDefaultMemberCa(m_tdTypeDef, m_szMember));
        }
        
        // Add the alias information if the type is an alias.
        IfFailGo(_HandleAliasInfo(pITI, pProperty, pd));
    }
    
    if (bConversionLoss)
        hr = S_CONVERSION_LOSS;

ErrExit:    
    if (m_szMember)
        ::SysFreeString(m_szMember), m_szMember=0;
    
    return hr;
} // HRESULT CImportTlb::_ConvPropertiesForFunctions()

//*****************************************************************************
// Convert the vars and functions of a dispinterface.  Vars actually turn
//  into a getter and possibly a setter.
//*****************************************************************************
HRESULT CImportTlb::_ConvDispatchMembers(
    ITypeInfo   *pITI,                  // ITypeInfo* to convert.
    TYPEATTR    *psAttr,                // TypeAttr of ITypeInfo.
    BOOL        fInheritsIEnum)
{
    HRESULT     hr;                     // A result.
    int         i;                      // Loop control.
    BOOL        bConversionLoss=FALSE;  // If true, some attributes were lost on conversion.

    IfFailGo(_FindFirstUserMethod(pITI, psAttr, &i));
    IfFailGo(BuildMemberList(pITI, i, psAttr->cFuncs, fInheritsIEnum));
    
    // Dispatch members really have no slot.
    m_Slot = 0;

    // Loop over properties.
    for (i=0; i<m_cMemberProps; ++i)
    {
        IfFailGo(_ConvProperty(pITI, &m_MemberList[i]));
    }

    // Loop over functions.
    BOOL bAllowIEnum = !fInheritsIEnum;
    for (; i<(int)m_MemberList.Size(); ++i)
    {
        // Get variable information.
        IfFailGo(_ConvFunction(pITI, &m_MemberList[i], FALSE, TRUE, FALSE, &bAllowIEnum));
        if (hr == S_CONVERSION_LOSS)
            bConversionLoss = TRUE;
    }

    // Add the property info.
    IfFailGo(_ConvPropertiesForFunctions(pITI, psAttr));
    
    if (bConversionLoss)
        hr = S_CONVERSION_LOSS;

ErrExit:
    // Free the func descs.
    FreeMemberList(pITI);

    return (hr);
} // HRESULT CImportTlb::_ConvDispatchMembers()

//*****************************************************************************
// Examine the functions on an interface, and skip the first 3 or first 7
//  if the functions are IUnknown or IDispatch members.
//*****************************************************************************
HRESULT CImportTlb::_FindFirstUserMethod(
    ITypeInfo   *pITI,                  // The Typedef to examine.
    TYPEATTR    *psAttr,                // TYPEATTR for the typedef.
    int         *pIx)                   // Put index of first user function here.
{
    HRESULT     hr = S_OK;              // A result.
    int         i;                      // Loop control.
    FUNCDESC    *psFunc=0;              // FUNCDESC for a member.
    BSTR        szName=0;               // A function's name.

    // Note:  this is a terrible workaround, but in some situations the methods from IUnknown / IDispatch will
    //  show up as though native dispatch functions.
    i = 0;
    if (psAttr->cFuncs >= 3)
    {
        IfFailGo(TryGetFuncDesc(pITI, i, &psFunc));
        if (psFunc->memid == 0x60000000 &&
            psFunc->elemdescFunc.tdesc.vt == VT_VOID &&
            psFunc->cParams == 2 &&
            psFunc->lprgelemdescParam[0].tdesc.vt == VT_PTR && // -> VT_USERDEFINED
            psFunc->lprgelemdescParam[1].tdesc.vt == VT_PTR && // -> VT_PTR -> VT_VOID
            SUCCEEDED(pITI->GetDocumentation(psFunc->memid, &szName, 0,0,0)) &&
            (wcscmp(szName, W("QueryInterface")) == 0) )
                i = 3;
        pITI->ReleaseFuncDesc(psFunc);
        psFunc=0;
        if (szName)
            ::SysFreeString(szName);
        szName = 0;
        if (psAttr->cFuncs >= 7)
        {
            IfFailGo(TryGetFuncDesc(pITI, i, &psFunc));
            if (psFunc->memid == 0x60010000 &&
                psFunc->elemdescFunc.tdesc.vt == VT_VOID &&
                psFunc->cParams == 1 &&
                psFunc->lprgelemdescParam[0].tdesc.vt == VT_PTR && // -> VT_UINT
                SUCCEEDED(pITI->GetDocumentation(psFunc->memid, &szName, 0,0,0)) &&
                (wcscmp(szName, W("GetTypeInfoCount")) == 0) )
                    i = 7;
            pITI->ReleaseFuncDesc(psFunc);
            psFunc=0;
            if (szName)
                ::SysFreeString(szName);
            szName = 0;
        }
    }

    *pIx = i;

ErrExit:
    if (psFunc)
        pITI->ReleaseFuncDesc(psFunc);
    if (szName)
        ::SysFreeString(szName);
    return (hr);
} // HRESULT CImportTlb::_FindFirstUserMethod()

//*****************************************************************************
// Given a FUNCDESC that is has INVOKE_PROPERTY* decoration, determine
//  the role of the function, and the property signature type.
//*****************************************************************************
HRESULT CImportTlb::_GetFunctionPropertyInfo(
    FUNCDESC    *psFunc,                // Function for which to get info.
    USHORT      *pSemantics,            // Put appropriate semantics here.
    FUNCDESC    **ppSig,                // Put FUNCDESC for signature here.
    TYPEDESC    **ppProperty,           // Put TYPEDESC for return here.
    BOOL        *pbRetval,              // If true, the type is [retval]
    BOOL        fUseLastParam,          // If true, default to the last parameter as the return value
    BSTR        strName)                // Name of the property
{
    FUNCDESC    *psTmp;                 // FUNCDESC for some method.
    FUNCDESC    *psGet=0;               // FUNCDESC for Get method defining a property.
    FUNCDESC    *psPut=0;               // FUNCDESC for Put method defining a property.
    FUNCDESC    *psPutRef=0;            // FUNCDESC for PutRef method defining a property.
    FUNCDESC    *psF;                   // A FUNCDESC.
    TYPEDESC    *pReturn=0;             // The FUNCDESC's return type.
    int         cFound=0;               // Count of functions found.
    int         i;                      // Loop control.
    HRESULT     hr = S_OK;

    if (psFunc->invkind & INVOKE_PROPERTYGET)
    {   // A "Get", so return type is property type.
        *ppSig = psFunc;
        *pSemantics = msGetter;
    }
    else
    {   
        _ASSERTE(psFunc->invkind & (INVOKE_PROPERTYPUT | INVOKE_PROPERTYPUTREF));
        // Search for the "best" method from which to grab the signature.  We prefer the Get(),
        //  Followed by the Put(), followed by the PutRef()
        // Also look for Put() and PutRef(), so we can 
        for (int iFunc=0; iFunc<(int)m_MemberList.Size() && cFound<3; ++iFunc)
        {
            // Get a FUNCDESC from the list.
            psTmp = m_MemberList[iFunc].m_psFunc;

            // Is it for the same func?
            if (psTmp->memid != psFunc->memid)
                continue;

            // Is it the Get()?  If so, it is the one we want.
            if (psTmp->invkind & INVOKE_PROPERTYGET)
            {
                psGet = psTmp;
                ++cFound;
                continue;
            }

            // Is it the Put()?  Use it if we don't find a Get().
            if (psTmp->invkind & INVOKE_PROPERTYPUT)
            {
                psPut = psTmp;
                ++cFound;
                continue;
            }

            // Is it the PutRef()?  Keep track of it.
            if (psTmp->invkind & INVOKE_PROPERTYPUTREF)
            {
                psPutRef = psTmp;
                ++cFound;
            }
        }
        // Get the best FUNCDESC for the signature.
        *ppSig = psGet ? psGet : (psPut ? psPut : psFunc);

        // Determine whether this is a the "Set" or "VB specific Let" function.
        if (psFunc->invkind & INVOKE_PROPERTYPUTREF)
        {   // This function is the PROPERTYPUTREF.  Make it the setter.  If
            //  there is also a PROPERTYPUT, it will be the "letter".
            *pSemantics = msSetter;
        }
        else
        {   // We are looking at the PROPERTYPUT function (the "Let" function in native VB6.).
            
            // If there is also a PROPERTYPUTREF, make this the "VB Specific Let" function.
            if (psPutRef)
            {   // A PPROPERTYPUTREF also exists, so make this the "Let" function.
                *pSemantics = msOther;
            }
            else
            {   // There is no PROPERTYPUTREF, so make this the setter.
                *pSemantics = msSetter;
            }
        }
    }

    // Occasionally there is a property with no discernable type.  In that case, lose the 
    //  property on conversion.

    // Determine the type of the property, based on the "best" accessor.
    psF = *ppSig;
    *pbRetval = FALSE;
    if (psF->invkind & INVOKE_PROPERTYGET)
    {   // look for [retval].
        for (i=psF->cParams-1; i>=0; --i)
        {
            if (psF->lprgelemdescParam[i].paramdesc.wParamFlags & PARAMFLAG_FRETVAL)
            {   // will consume a level of indirection (later).
                *pbRetval = TRUE;
                pReturn = &psF->lprgelemdescParam[i].tdesc;
                break;
            }
        }
        // If no [retval], check return type.
        if (!pReturn && psF->elemdescFunc.tdesc.vt != VT_VOID && psF->elemdescFunc.tdesc.vt != VT_HRESULT)
            pReturn = &psF->elemdescFunc.tdesc;

        if (fUseLastParam)
        {
            // We may have stripped the [retval] if this is a disp-only interface.  Just use the last parameter.
            if (!pReturn && (psF->cParams > 0))
                pReturn = &psF->lprgelemdescParam[psF->cParams-1].tdesc;
        }
        else
        {
            // If there is no type, don't try to set the getter.
            if (pReturn && pReturn->vt == VT_VOID)
                pReturn = NULL;
        }

        if (!pReturn)
        {
            ReportEvent(NOTIF_CONVERTWARNING, TLBX_E_PROPGET_WITHOUT_RETURN, strName, m_szMngName);
        }
    }
    else
    {   // Find lastmost param that isn't [retval].  (Should be the last param, but it is 
        //  possible to write an IDL with a PROPERTYPUT that has a [retval].
        for (i=psF->cParams-1; i>=0; --i)
        {
            if ((psF->lprgelemdescParam[psF->cParams-1].paramdesc.wParamFlags & PARAMFLAG_FRETVAL) == 0)
            {
                {   // First, and possibly only, param.
                    pReturn = &psF->lprgelemdescParam[i].tdesc;
                    break;
                }
            }
        }
    }

//ErrExit:
    if (pReturn == 0)
        *pSemantics = 0;
    *ppProperty = pReturn;

    return hr;
} // HRESULT CImportTlb::_GetFunctionPropertyInfo()

//*****************************************************************************
// Convert a function description to metadata entries.
//  
// This can be rather involved.  If the function is a INVOKE_PROPERTY*,
//  determine if it will be converted as a COM+ property, and if so, which
//  of up to three functions will be selected to provide the property 
//  signature.
// The function return type is found by scaning the parameters looking for 
//  [retval]s.
//*****************************************************************************
#ifdef _PREFAST_
#pragma warning(push)
#pragma warning(disable:21000) // Suppress PREFast warning about overly large function
#endif
HRESULT CImportTlb::_ConvFunction(
    ITypeInfo   *pITI,                  // Containing TypeInfo.
    MemberInfo  *pMember,               // iNFO for the function.
    BOOL        bVtblGapFuncs,          // Add functions for vtblGaps?
    BOOL        bAddDispIds,            // Add DispIds to the member?
    BOOL        bDelegateInvokeMeth,    // Convert function for a delegate invoke
    BOOL*       bAllowIEnum)            // Allowed to change this function to GetEnumerator
{
    HRESULT     hr;                     // A result.
    int         iSrcParam;              // Param count, as looping through params.
    int         iDestParam;             // Param count, as looping through params.
    int         cDestParams;            // Count of destination params.
    int         ixOpt;                  // Index of first param that is optional due to cParamsOpt.
    int         ixVarArg;               // Index of vararg param, if any.
    mdMethodDef mdFunc;                 // Token of new member.
    BSTR        szTypeName=0;           // Name of the type.
    DWORD       dwFlags=0;              // Member flags.
    DWORD       dwImplFlags=0;          // The impl flags.
    WCHAR       *pszName=0;             // Possibly decorated name of member.
    CQuickArray<WCHAR> qbName;          // Buffer for decorated name.
    TYPEDESC    *pReturn=0;             // Return type.
    int         bRetval=false;          // Is the return result a [retval] parameter?
    int         ixRetval;               // Which param is the [retval]?
    TYPEDESC    *pReturnRetval=0;       // Return type from [retval] (incl. indirection).
    WORD        wRetFlags=0;            // Return type flags.
    ULONG       offset=0;               // Offset of function
    BSTR        *rszParamNames=0;       // Parameter names.
    UINT        iNames;                 // Count of actual names.
    CQuickBytes qbComSig;               // new signature 
    BYTE        *pbSig;                 // Pointer into the signature.
    ULONG       sigFlags;               // Signature handling flags.
    CQuickArray<BYTE> qbNativeBuf;      // Native type buffer.
    CQuickArray<BYTE> qbDummyNativeTypeBuf; // A dummy native type array.
    CQuickArray<ULONG> qbNativeOfs;     // Offset of native type for each param.
    CQuickArray<ULONG> qbNativeLen;     // Length of native type for each param.
    ULONG       iNativeOfs=0;           // Current offset in native type buffer.
    ULONG       iNewNativeOfs=0;        // New offset in native type buffer.
    ULONG       cb;                     // Size of an element.
    ULONG       cbTotal = 0;            // Size of the signature.
    int         bOleCall=false;         // Is the implementation OLE style?(HRESULT or IDispatch)
    USHORT      msSemantics=0;          // Property's methodsemantics.
    WCHAR       szSpecial[40];          // To build name of special function.
    mdToken     tkAttr;                 // Token for custom attribute type.
    BOOL        bConversionLoss=false;  // If true, some attributes were lost on conversion.
    enum {ParamRetval=-1, ParamNone=-2};
    int         iParamError=ParamNone;  // Index of param with conversion error.
    BOOL        bNewEnumMember = FALSE; // A flag indicating if the member is the NewEnum member.
    int         iLCIDParam = -1;        // Index of the LCID parameter.
    FUNCDESC    *psFunc = pMember->m_psFunc;

    // If we might insert vtable gaps, then we'd better have initialized the vtable slot size.
    _ASSERTE(!bVtblGapFuncs || (m_cbVtableSlot != 0));

    // Retrieve the member name from the member info.
    IfNullGo(m_szMember = SysAllocString(bDelegateInvokeMeth ? DELEGATE_INVOKE_METH_NAME : pMember->m_pName));

#ifdef _DEBUG
    LPWSTR funcName = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_TlbImpShouldBreakOnConvFunction);
    if (funcName)
    {
        if (wcscmp(funcName, pMember->m_pName) == 0)
            _ASSERTE(!"TlbImpBreakOnConvFunction");
        
        delete [] funcName;
    }
#endif //_DEBUG

    // Determine if the member is the new enum member.
    if ((*bAllowIEnum))
    {
        bNewEnumMember = FuncIsNewEnum(pITI, psFunc, pMember->m_iMember) == S_OK;
        
        // Once a method is converted in this interface, don't convert any more.
        if (bNewEnumMember)
            *bAllowIEnum = FALSE;
    }
    

    // We should NEVER have a new enum member when we are dealing with a delegate invoke meth.
    if (bNewEnumMember && bDelegateInvokeMeth)
    {
        // Get the real name of the method
        BSTR szTypeInfoName = NULL;
        BSTR szMemberName = NULL;
        hr = m_pITI->GetDocumentation(MEMBERID_NIL, &szTypeInfoName, 0, 0, 0);
        if (FAILED(hr))
            szTypeInfoName = SysAllocString(W("???"));
                       
        ReportEvent(NOTIF_CONVERTWARNING, TLBX_E_EVENT_WITH_NEWENUM, szTypeInfoName);

        SysFreeString(szMemberName);
        SysFreeString(szTypeInfoName);
        
        IfFailGo(TLBX_E_EVENT_WITH_NEWENUM);
    }

    // If there is a gap in the vtable, emit a special function.
    if (bVtblGapFuncs)
    {
        if ((psFunc->oVft / m_cbVtableSlot) != m_Slot)
        {
            ULONG n = psFunc->oVft / m_cbVtableSlot;
            // Make sure slot numbers are monotonically increasing.
            if (n < m_Slot)
            {
                IfFailGo(pITI->GetDocumentation(MEMBERID_NIL, &szTypeName, 0, 0, 0));
                IfFailGo(PostError(TLBX_E_BAD_VTABLE, m_szMember, szTypeName, m_szLibrary));
            }

            n -= m_Slot;
            if (n == 1)
                _snwprintf_s(szSpecial, lengthof(szSpecial), lengthof(szSpecial) - 1, VTBL_GAP_FORMAT_1, VTBL_GAP_FUNCTION, m_Slot);
            else
                _snwprintf_s(szSpecial, lengthof(szSpecial), lengthof(szSpecial) - 1, VTBL_GAP_FORMAT_N, VTBL_GAP_FUNCTION, m_Slot, n);
            IfFailGo(m_pEmit->DefineMethod(m_tdTypeDef, szSpecial, VTBL_GAP_FUNCTION_FLAGS, VTBL_GAP_SIGNATURE,sizeof(VTBL_GAP_SIGNATURE),
                0/* rva*/, VTBL_GAP_FUNC_IMPL_FLAGS, &mdFunc));
            m_Slot += n;
        }
        // What we will expect next time.
        ++m_Slot;
    }

    //-------------------------------------------------------------------------
    // Determine the return type.
    // If this is an hresult function, prepare to munge return, params.
    if (psFunc->elemdescFunc.tdesc.vt == VT_HRESULT)
    {
        bOleCall = true;
    }
    else
    {
        if ((psFunc->elemdescFunc.tdesc.vt != VT_VOID) && (psFunc->elemdescFunc.tdesc.vt != VT_HRESULT))
            pReturn = &psFunc->elemdescFunc.tdesc;
    }

    // Look for [RETVAL].
    for (iSrcParam=0; iSrcParam<psFunc->cParams; ++iSrcParam)
    {
        if (psFunc->lprgelemdescParam[iSrcParam].paramdesc.wParamFlags & PARAMFLAG_FRETVAL)
        {   
            // If already have a return, or a DISPATCH function, error.
            if (pReturn != 0)
            {   // Unexpected return found.
                ReportEvent(NOTIF_CONVERTWARNING, TLBX_E_AMBIGUOUS_RETURN, m_szName, m_szMember);
                IfFailGo(TLBX_E_AMBIGUOUS_RETURN);
            }
            else
            {   // Found a return type.
                wRetFlags = psFunc->lprgelemdescParam[iSrcParam].paramdesc.wParamFlags;
                pReturn = &psFunc->lprgelemdescParam[iSrcParam].tdesc;
                bRetval = true;
                ixRetval = iSrcParam;
            }
            break;
        }
    }
    
    // Check to see if there is an LCID parameter.
    for (iSrcParam=0;iSrcParam<psFunc->cParams;iSrcParam++)
    {
        if (psFunc->lprgelemdescParam[iSrcParam].paramdesc.wParamFlags & PARAMFLAG_FLCID)
        {
            if (iLCIDParam != -1)
                IfFailGo(PostError(TLBX_E_MULTIPLE_LCIDS, m_szName, m_szMember));
            iLCIDParam = iSrcParam;
        }
    }

    //-------------------------------------------------------------------------
    // Size buffers to accomodate parameters.
    // Resize the native type length array.
    IfFailGo(qbNativeBuf.ReSizeNoThrow(1));
    IfFailGo(qbNativeLen.ReSizeNoThrow(psFunc->cParams + 1));
    IfFailGo(qbNativeOfs.ReSizeNoThrow(psFunc->cParams + 1));
    memset(qbNativeLen.Ptr(), 0, (psFunc->cParams + 1)*sizeof(int));
    memset(qbNativeOfs.Ptr(), 0, (psFunc->cParams + 1)*sizeof(int));

    // resize to make room for calling convention and count of argument
    IfFailGo(qbComSig.ReSizeNoThrow(CB_MAX_ELEMENT_TYPE + 1));
    pbSig = (BYTE *)qbComSig.Ptr();

    //-------------------------------------------------------------------------
    // Determine which params need to be marked optional, by virtue of cParamsOpt count.
    if (psFunc->cParamsOpt == 0)
        ixVarArg = ixOpt = psFunc->cParams + 1;
    else
    {
        if (psFunc->cParamsOpt == -1)
        {   // Varargs.
            ixVarArg = ixOpt = psFunc->cParams + 1;
            // If this is a PROPERTYPUT or PROPERTYPUTREF, skip the last non-retval parameter (it
            //  is the new value to be set).
            BOOL bPropVal = (psFunc->invkind & (INVOKE_PROPERTYPUT | INVOKE_PROPERTYPUTREF)) ? TRUE : FALSE;
            // Find the vararg param.
            for (iSrcParam=psFunc->cParams-1; iSrcParam>=0; --iSrcParam)
            {
                // The count of optional params does not include any lcid params, nor does
                //  it include the return value, so skip those.
                if ((psFunc->lprgelemdescParam[iSrcParam].paramdesc.wParamFlags & (PARAMFLAG_FRETVAL|PARAMFLAG_FLCID)) != 0)
                    continue;
                // If haven't yet seen the property value, this param is it, so skip it, too.
                if (bPropVal)
                {
                    bPropVal = FALSE;
                    continue;
                }
                ixVarArg = iSrcParam;
                break;
            } // for (iSrcParam=cParams-1...
        }
        else
        {   // ixOpt will be index of first optional parameter.
            short cOpt = psFunc->cParamsOpt;
            ixOpt = 0;
            ixVarArg = psFunc->cParams + 1;
            for (iSrcParam=psFunc->cParams-1; iSrcParam>=0; --iSrcParam)
            {
                // The count of optional params does not include any lcid params, nor does
                //  it include the return value, so skip those.
                if ((psFunc->lprgelemdescParam[iSrcParam].paramdesc.wParamFlags & (PARAMFLAG_FRETVAL|PARAMFLAG_FLCID)) == 0)
                {   
                    if (--cOpt == 0)
                    {
                        ixOpt = iSrcParam;
                        break;
                    }
                }
            } // for (iSrcParam=cParams-1...
        }
    }


    //-------------------------------------------------------------------------
    // Get the parameter names.
    rszParamNames = reinterpret_cast<BSTR*>(_alloca((psFunc->cParams+1) * sizeof(BSTR*)));

    // Get list of names.
    IfFailGo(pITI->GetNames(psFunc->memid, rszParamNames, psFunc->cParams+1, &iNames));

    // zero name pointer for non-named params.
    for (iSrcParam=iNames; iSrcParam<=psFunc->cParams; ++iSrcParam)
        rszParamNames[iSrcParam] = 0;

    //-------------------------------------------------------------------------
    // Convert the calling convention, param count, and return type.
    cDestParams = psFunc->cParams;
    if (bRetval)
        --cDestParams;
    if (iLCIDParam != -1)
        --cDestParams;

    if (pReturn)
    {   
        // Param count
        cbTotal = cb = CorSigCompressData((ULONG)IMAGE_CEE_CS_CALLCONV_DEFAULT | IMAGE_CEE_CS_CALLCONV_HASTHIS, pbSig);
        cb = CorSigCompressData(cDestParams, &(pbSig[cbTotal]));
        cbTotal += cb;
        // Return type or [retval].
        if (bRetval)
            sigFlags = (SigFlags)(wRetFlags & SIG_FLAGS_MASK) | SIG_FUNC, iParamError=ixRetval;
        else
            sigFlags = SIG_FUNC, iParamError=ParamRetval;
        IfFailGo(_ConvSignature(pITI, pReturn, sigFlags, qbComSig, cbTotal, &cbTotal, qbNativeBuf, iNativeOfs, &iNewNativeOfs, bNewEnumMember));
        qbNativeLen[0] = iNewNativeOfs - iNativeOfs;
        qbNativeOfs[0] = iNativeOfs;
        iNativeOfs = iNewNativeOfs;
        if (hr == S_CONVERSION_LOSS)
            bConversionLoss = true;
    }
    else
    {   // No return value
        cbTotal = cb = CorSigCompressData((ULONG)IMAGE_CEE_CS_CALLCONV_DEFAULT | IMAGE_CEE_CS_CALLCONV_HASTHIS, pbSig);
        cb = CorSigCompressData(cDestParams, &(pbSig[cbTotal]));
        cbTotal += cb;
        cb = CorSigCompressData(ELEMENT_TYPE_VOID, &pbSig[cbTotal]);
        cbTotal += cb;
    }

    //-------------------------------------------------------------------------
    // Translate each parameter.
    for (iSrcParam=0, iDestParam=0; iSrcParam<psFunc->cParams; ++iSrcParam)
    {
        if (!(psFunc->lprgelemdescParam[iSrcParam].paramdesc.wParamFlags & NON_CONVERTED_PARAMS_FLAGS))
        {
            sigFlags = (SigFlags)(psFunc->lprgelemdescParam[iSrcParam].paramdesc.wParamFlags & SIG_FLAGS_MASK) | SIG_FUNC | SIG_USE_BYREF;
            if (iSrcParam == ixVarArg)
                sigFlags |= SIG_VARARG;
            iParamError = iSrcParam;
            IfFailGo(_ConvSignature(pITI, &psFunc->lprgelemdescParam[iSrcParam].tdesc, sigFlags, qbComSig, cbTotal, &cbTotal, qbNativeBuf, iNativeOfs, &iNewNativeOfs, bNewEnumMember));
            qbNativeLen[iDestParam+1] = iNewNativeOfs - iNativeOfs;
            qbNativeOfs[iDestParam+1] = iNativeOfs;
            iNativeOfs = iNewNativeOfs;
            iDestParam++;
            if (hr == S_CONVERSION_LOSS)
                bConversionLoss = true;
        }
    }
    iParamError = ParamNone;

    //-------------------------------------------------------------------------
    // Get the previously decorated name.  Add interface name and make unique.
    if (bDelegateInvokeMeth)
    {
        pszName = (WCHAR*)DELEGATE_INVOKE_METH_NAME;
    }
    else
    {
        // m_szInterface should be non-null if processing an implemented interface; should be null otherwise.
        _ASSERTE(m_ImplIface == eImplIfaceNone || m_szInterface != 0);
        IfFailGo(qbName.ReSizeNoThrow(wcslen(pMember->m_pName)+2));
        wcscpy_s(qbName.Ptr(), wcslen(pMember->m_pName)+2, pMember->m_pName); 
        IfFailGo(GenerateUniqueMemberName(qbName, (PCCOR_SIGNATURE)qbComSig.Ptr(), cbTotal, m_szInterface, mdtMethodDef));
        pszName = qbName.Ptr();
    }

    // Determine the function's semantics, flags and impl flags.
    if (!bDelegateInvokeMeth)
    {
    msSemantics = pMember->m_msSemantics;
        dwImplFlags = DEFAULT_ITF_FUNC_IMPL_FLAGS;
    dwFlags = msSemantics ? DEFAULT_PROPERTY_FUNC_FLAGS : DEFAULT_INTERFACE_FUNC_FLAGS;
    // If processing an implemented interface, remove the abstract bit.  Methods on classes are not abstract.
    if (m_ImplIface != eImplIfaceNone)
        dwFlags &= ~mdAbstract;
    }
    else
    {
        msSemantics = 0;
        dwImplFlags = miRuntime;
        dwFlags = DELEGATE_INVOKE_FUNC_FLAGS;
    }

    //-------------------------------------------------------------------------
    // Create the function definition in the metadata.
    IfFailGo(m_pEmit->DefineMethod(m_tdTypeDef, pszName, dwFlags, (PCCOR_SIGNATURE) qbComSig.Ptr(),cbTotal, 
        0 /* rva*/, dwImplFlags | (bOleCall ? 0 : miPreserveSig), &mdFunc));

    // If the method is part of a property, save info to set up the property.
    if (msSemantics)
        pMember->SetFuncInfo(mdFunc, msSemantics);
    
    // Handle dispids for non-implemented interfaces, and for default interface
    if (m_ImplIface != eImplIface)
    {
        // Add the DispIds if the flag is set.
        long lDispSet = 1;
        _SetDispIDCA(pITI, pMember->m_iMember, psFunc->memid, mdFunc, bAddDispIds, &lDispSet, TRUE);

        // If this method is the default, and not a property accessor, add a custom attribute to the class.
        if (lDispSet == DISPID_VALUE && msSemantics == 0)
            IfFailGo(_AddDefaultMemberCa(m_tdTypeDef, m_szMember));
    }
    
    DECLARE_CUSTOM_ATTRIBUTE(sizeof(int));
    
    // If this method has an LCID then set the LCIDConversion attribute.
    if (iLCIDParam != -1)
    {
        // Dispid for the function.
        BUILD_CUSTOM_ATTRIBUTE(int, iLCIDParam);
        IfFailGo(GetAttrType(ATTR_LCIDCONVERSION, &tkAttr));
        FINISH_CUSTOM_ATTRIBUTE();
        IfFailGo(m_pEmit->DefineCustomAttribute(mdFunc, tkAttr, PTROF_CUSTOM_ATTRIBUTE(), SIZEOF_CUSTOM_ATTRIBUTE(), 0));
    }

    // Save the func flags for anyone that needs typelib's flags.
    if (psFunc->wFuncFlags)
    {
        IfFailGo(GetAttrType(ATTR_TYPELIBFUNC, &tkAttr));
        INIT_CUSTOM_ATTRIBUTE(sizeof(WORD));
        BUILD_CUSTOM_ATTRIBUTE(WORD, psFunc->wFuncFlags);
        FINISH_CUSTOM_ATTRIBUTE();
        IfFailGo(m_pEmit->DefineCustomAttribute(mdFunc, tkAttr, PTROF_CUSTOM_ATTRIBUTE(), SIZEOF_CUSTOM_ATTRIBUTE(),0));
    }

    //-------------------------------------------------------------------------
    // Convert the param info for the return type.
    if (pReturn)
    {   // store return value parameter as sequence 0
        if (bRetval)
        {
            hr = _IsAlias(pITI, &psFunc->lprgelemdescParam[ixRetval].tdesc);
            IfFailGo(hr);
            if (qbNativeLen[0] || hr == S_OK)
            {
                IfFailGo(_ConvParam(pITI, mdFunc, 0, &psFunc->lprgelemdescParam[ixRetval], ParamNormal, 0 /*rszParamNames[ixRetval+1]*/, 
                    &qbNativeBuf[qbNativeOfs[0]], qbNativeLen[0]));
            }
        }
        else
        {
            hr = _IsAlias(pITI, &psFunc->elemdescFunc.tdesc);
            IfFailGo(hr);
            if (qbNativeLen[0] || hr == S_OK)
            {
                IfFailGo(_ConvParam(pITI, mdFunc, 0, &psFunc->elemdescFunc, ParamNormal, 0, 
                    &qbNativeBuf[qbNativeOfs[0]], qbNativeLen[0]));
            }
        }
    }

    //-------------------------------------------------------------------------
    // Convert parameter info (flags, native type, default value).
    for (iSrcParam=iDestParam=0; iSrcParam<psFunc->cParams; ++iSrcParam)
    {
        if ((psFunc->lprgelemdescParam[iSrcParam].paramdesc.wParamFlags & NON_CONVERTED_PARAMS_FLAGS) == 0)
        {
            ParamOpts opt = ParamNormal;
            if (iSrcParam >= ixOpt)
                opt = ParamOptional;
            else
            if (iSrcParam == ixVarArg)
                opt = ParamVarArg;
            iDestParam++;
            IfFailGo(_ConvParam(pITI, mdFunc, iDestParam, &psFunc->lprgelemdescParam[iSrcParam], opt, rszParamNames[iSrcParam + 1], 
                &qbNativeBuf[qbNativeOfs[iDestParam]], qbNativeLen[iDestParam]));
        }
    }

    
    //-------------------------------------------------------------------------
    // If processing an implemented interface, set up MethodImpls.
    if (m_ImplIface != eImplIfaceNone)
    {  
        // Define a memberref on the implemented interface.
        mdToken mrItfMember;
        IfFailGo(m_pEmit->DefineMemberRef(m_tkInterface, pMember->m_pName, (PCCOR_SIGNATURE) qbComSig.Ptr(),cbTotal, &mrItfMember));

        // Define a method impl.
        IfFailGo(m_pEmit->DefineMethodImpl(m_tdTypeDef, mdFunc, mrItfMember));
    }

    if (bConversionLoss)
    {
        hr = S_CONVERSION_LOSS;
        ReportEvent(NOTIF_CONVERTWARNING, TLBX_I_UNCONVERTABLE_ARGS, m_szName, m_szMember);
    }

ErrExit:
    // Special case for typeload load failures -- they're very hard to diagnose.
    if (hr == TYPE_E_CANTLOADLIBRARY)
    {
        if (iParamError >= 0 && iParamError < psFunc->cParams && rszParamNames[iParamError+1])
            ReportEvent(NOTIF_CONVERTWARNING, TLBX_E_PARAM_ERROR_NAMED, m_szName, rszParamNames[iParamError+1], m_szMember);
        else
            ReportEvent(NOTIF_CONVERTWARNING, TLBX_E_PARAM_ERROR_UNNAMED, m_szName, iParamError, m_szMember);
    }
    if (rszParamNames)
    {
        for (iSrcParam=0; iSrcParam<=psFunc->cParams; ++iSrcParam)
            if (rszParamNames[iSrcParam])
                ::SysFreeString(rszParamNames[iSrcParam]);
    }
    if (m_szMember)
        ::SysFreeString(m_szMember), m_szMember=0;
    if (szTypeName)
        ::SysFreeString(szTypeName);
    
    return (hr);
} // HRESULT CImportTlb::_ConvFunction()
#ifdef _PREFAST_
#pragma warning(pop)
#endif


HRESULT CImportTlb::_SetHiddenCA(mdTypeDef token)
{
    mdToken tkAttr;
    HRESULT hr = S_OK;
    
    DECLARE_CUSTOM_ATTRIBUTE(sizeof(short));
    BUILD_CUSTOM_ATTRIBUTE(short, TYPEFLAG_FHIDDEN);
    IfFailGo(GetAttrType(ATTR_TYPELIBTYPE, &tkAttr));
    FINISH_CUSTOM_ATTRIBUTE();
    m_pEmit->DefineCustomAttribute(token, tkAttr, PTROF_CUSTOM_ATTRIBUTE(), SIZEOF_CUSTOM_ATTRIBUTE(), 0);
    
ErrExit:
    return S_OK;    
}

HRESULT CImportTlb::_ForceIEnumerableCVExists(ITypeInfo* pITI, BOOL* CVExists)
{
    ITypeInfo2  *pITI2 = 0;
    *CVExists = FALSE;
    HRESULT hr = S_OK;

    pITI->QueryInterface(IID_ITypeInfo2, reinterpret_cast<void**>(&pITI2));

    if (pITI2)
    {
        VARIANT vCustomData;
        VariantInit(&vCustomData);

        IfFailGo(pITI2->GetCustData(GUID_ForceIEnumerable, &vCustomData));

        if (V_VT(&vCustomData) != VT_EMPTY)
            *CVExists = TRUE;
            
        VariantClear(&vCustomData);       
    }

ErrExit:
    if (pITI2)
        pITI2->Release();
        
    return S_OK;
}


HRESULT CImportTlb::_GetDispIDCA(
    ITypeInfo* pITI,
    int iMember,
    long* lDispSet,
    BOOL bFunc
    )
{
    ITypeInfo2  *pITI2=0;               // For getting custom value.
    HRESULT hr = S_OK;
    long lDispId = DISPID_UNKNOWN;
    
    // Get the ITypeInfo2 interface if possible
    pITI->QueryInterface(IID_ITypeInfo2, reinterpret_cast<void**>(&pITI2));

    if (pITI2)
    {
        VARIANT vCustomData;
        VariantInit(&vCustomData);

        if (bFunc)
            IfFailGo(pITI2->GetFuncCustData(iMember, GUID_DispIdOverride, &vCustomData));
        else
            IfFailGo(pITI2->GetVarCustData(iMember, GUID_DispIdOverride, &vCustomData));

        if ((V_VT(&vCustomData) == VT_I2) || (V_VT(&vCustomData) == VT_I4))
        {
            hr = VariantChangeType(&vCustomData, &vCustomData, 0, VT_I4);
            if (hr == S_OK)
                lDispId = vCustomData.lVal;
        }

        VariantClear(&vCustomData);
    }

ErrExit:
    if (lDispSet != NULL)
        *lDispSet = lDispId;

    if (pITI2)
        pITI2->Release();
        
    return S_OK;
}



HRESULT CImportTlb::_SetDispIDCA(
    ITypeInfo* pITI,
    int iMember,
    long lDispId,
    mdToken func,
    BOOL fAlwaysAdd,
    long* lDispSet,
    BOOL bFunc
    )
{
    WCHAR DispIDCA[] = W("{CD2BC5C9-F452-4326-B714-F9C539D4DA58}");
    ITypeInfo2  *pITI2=0;               // For getting custom value.
    HRESULT hr = S_OK;
    
    // Get the ITypeInfo2 interface if possible
    pITI->QueryInterface(IID_ITypeInfo2, reinterpret_cast<void**>(&pITI2));

    if (pITI2)
    {
        VARIANT vCustomData;
        VariantInit(&vCustomData);

        if (bFunc)
            IfFailGo(pITI2->GetFuncCustData(iMember, GUID_DispIdOverride, &vCustomData));
        else
            IfFailGo(pITI2->GetVarCustData(iMember, GUID_DispIdOverride, &vCustomData));

        if ((V_VT(&vCustomData) == VT_I2) || (V_VT(&vCustomData) == VT_I4))
        {
            hr = VariantChangeType(&vCustomData, &vCustomData, 0, VT_I4);
            if (hr == S_OK)
            {
                lDispId = vCustomData.lVal;
                fAlwaysAdd = true;
            }
        }
        else if (V_VT(&vCustomData) != VT_EMPTY)
        {
            // Invalid Variant type on the data - spit out a warning.
            BSTR CustomValue = SysAllocString((const WCHAR*)&DispIDCA[0]);
            BSTR ObjectName;
            IfFailGo(pITI2->GetDocumentation(iMember+1, &ObjectName, NULL, NULL, NULL));
            
            ReportEvent(NOTIF_CONVERTWARNING, TLBX_W_NON_INTEGRAL_CA_TYPE, CustomValue, ObjectName);

            SysFreeString(CustomValue);
            SysFreeString(ObjectName);
        }

        VariantClear(&vCustomData);
    }

    // Set the dispid CA on the property.
    if (fAlwaysAdd)
    {
        mdToken tkAttr;
        DECLARE_CUSTOM_ATTRIBUTE(sizeof(DISPID));
        BUILD_CUSTOM_ATTRIBUTE(DISPID, lDispId);
        IfFailGo(GetAttrType(ATTR_DISPID, &tkAttr));
        FINISH_CUSTOM_ATTRIBUTE();
        IfFailGo(m_pEmit->DefineCustomAttribute(func, tkAttr, PTROF_CUSTOM_ATTRIBUTE(), SIZEOF_CUSTOM_ATTRIBUTE(), 0));
    }
    
ErrExit:
    if (lDispSet != NULL)
    {
        *lDispSet = lDispId;
    }

    if (pITI2)
        pITI2->Release();

    return S_OK;
}


HRESULT CImportTlb::_CheckForPropertyCustomAttributes(ITypeInfo* pITI, int index, INVOKEKIND* ikind)
{
    HRESULT     hr;
    VARIANT     vCustomData;
    ITypeInfo2* pITI2       = 0;
    BOOL        found       = FALSE;

    VariantInit(&vCustomData);

    // Get the ITypeInfo2 interface if possible
    pITI->QueryInterface(IID_ITypeInfo2, reinterpret_cast<void**>(&pITI2));
    if (pITI2)
    {
        // First, check for PropGet
        hr = pITI2->GetFuncCustData(index, GUID_PropGetCA, &vCustomData);
        IfFailGo(hr);
        if (V_VT(&vCustomData) != VT_EMPTY)
        {
            *ikind = INVOKE_PROPERTYGET;
            found = TRUE;
            goto ErrExit;
        }

        // Second, check for PropPut
        VariantClear(&vCustomData);
        hr = pITI2->GetFuncCustData(index, GUID_PropPutCA, &vCustomData);
        IfFailGo(hr);
        if (V_VT(&vCustomData) != VT_EMPTY)
        {
            *ikind = INVOKE_PROPERTYPUT;
            found = TRUE;
            goto ErrExit;
        }
    }

ErrExit:
    VariantClear(&vCustomData);

    if (pITI2)
        pITI2->Release();

    if (found)
        return S_OK;

    return S_FALSE;
}

//*****************************************************************************
// Generate an event with an add and remove method 
//*****************************************************************************
HRESULT CImportTlb::_GenerateEvent(
    ITypeInfo   *pITI,                  // Containing TypeInfo.
    MemberInfo  *pMember,               // Info for the function
    BOOL        fInheritsIEnum)
{
    HRESULT             hr = S_OK;      // A result.
    mdMethodDef         mdAdd;          // Token of add_XXX method.
    mdMethodDef         mdRemove;       // Token of remove_XXX method.
    CQuickArray<WCHAR>  qbName;         // Buffer for decorated name.
    CQuickArray<BYTE>   qbSig;          // The signature.
    ULONG               cb;             // Size of an element.
    ULONG               cbTotal = 0;    // Size of the signature.
    mdTypeDef           tdDelegate;     // The delegate type def.
    mdEvent             tkEvent;        // The token for the event.

    // If this method is a property method, then skip the event.
    // Also look at the property semantic - it might be we couldn't import this as a property
    //  and fell back to a method.
    if ((pMember->m_psFunc->invkind != INVOKE_FUNC) && (pMember->m_msSemantics != 0))
    {
        ReportEvent(NOTIF_CONVERTWARNING, TLBX_W_NO_PROPS_IN_EVENTS, m_szName);
        return S_CONVERSION_LOSS;
    }

    // Generate the delegate.
    IfFailGo(_GenerateEventDelegate(pITI, pMember, &tdDelegate, fInheritsIEnum));

    // Generate the sig for the add and remove methods.
    IfFailGo(qbSig.ReSizeNoThrow(CB_MAX_ELEMENT_TYPE * 2 + 1));
    cbTotal = CorSigCompressData((ULONG)IMAGE_CEE_CS_CALLCONV_DEFAULT | IMAGE_CEE_CS_CALLCONV_HASTHIS, qbSig.Ptr());
    cb = CorSigCompressData(1, &(qbSig[cbTotal]));
    cbTotal += cb;
    cb = CorSigCompressData(ELEMENT_TYPE_VOID, &qbSig[cbTotal]);
    cbTotal += cb;
    cb = CorSigCompressData(ELEMENT_TYPE_CLASS, &qbSig[cbTotal]);
    cbTotal += cb;
    cb = CorSigCompressToken(tdDelegate, &qbSig[cbTotal]);
    cbTotal += cb;

    // Generate the add method.
    IfFailGo(qbName.ReSizeNoThrow(EVENT_ADD_METH_PREFIX_LENGTH + wcslen(pMember->m_pName) + 1));
    StringCchPrintf(qbName.Ptr(), qbName.Size(), W("%s%s"), EVENT_ADD_METH_PREFIX, pMember->m_pName);   
    IfFailGo(m_pEmit->DefineMethod(m_tdTypeDef, qbName.Ptr(), DEFAULT_INTERFACE_FUNC_FLAGS, 
        qbSig.Ptr(), cbTotal, 0 /* rva*/, DEFAULT_ITF_FUNC_IMPL_FLAGS, &mdAdd));

    // Generate the remove method.
    IfFailGo(qbName.ReSizeNoThrow(EVENT_REM_METH_PREFIX_LENGTH + wcslen(pMember->m_pName) + 1));
    StringCchPrintf(qbName.Ptr(), qbName.Size(), W("%s%s"), EVENT_REM_METH_PREFIX, pMember->m_pName);   
    IfFailGo(m_pEmit->DefineMethod(m_tdTypeDef, qbName.Ptr(), DEFAULT_INTERFACE_FUNC_FLAGS, 
        qbSig.Ptr(), cbTotal, 0 /* rva*/, DEFAULT_ITF_FUNC_IMPL_FLAGS, &mdRemove));

    // Define the event itself.
    IfFailGo(m_pEmit->DefineEvent(m_tdTypeDef, pMember->m_pName, 0, tdDelegate, 
        mdAdd, mdRemove, mdTokenNil, NULL, &tkEvent));

ErrExit:

    return (hr);
} // HRESULT CImportTlb::_GenerateEvent()

//*****************************************************************************
// Generate an add and remove method 
//*****************************************************************************
HRESULT CImportTlb::_GenerateEventDelegate(
    ITypeInfo   *pITI,                  // Containing TypeInfo.
    MemberInfo  *pMember,               // Info for the source interface func
    mdTypeDef   *ptd,                   // The output typedef.
    BOOL        fInheritsIEnum)
{
    HRESULT             hr = S_OK;                  // A result.
    BSTR                bstrSrcItfName = NULL;      // The name of the source interface.
    CQuickArray<WCHAR>  qbEventHandlerName;         // The name of the event handler.
    BSTR                szOldName = NULL;           // The old value m_tdTypeDef.
    mdTypeDef           tdOldTypeDef = NULL;        // The old value m_szName.
    CQuickArray<BYTE>   qbSig;                      // The signature.
    ULONG               cb;                         // Size of an element.
    ULONG               cbTotal = 0;                // Total size of signature.
    mdMethodDef         mdFunc;                     // The defined function.
    mdTypeRef           trMulticastDelegate;        // The type ref for System.MulticastDelegate.
    mdToken             tkAttr;                     // Custom attribute type.

    // Store the old values of the ITypeInfo name and of the current type def.
    szOldName = m_szName;
    tdOldTypeDef = m_tdTypeDef;
    m_szName = NULL;

    // Retrieve the full name of the source interface.
    IfFailGo(GetManagedNameForTypeInfo(pITI, m_wzNamespace, NULL, &bstrSrcItfName));

    // Generate a unique name for the event handler which will be of the form:
    //     <SrcItfName>_<MethodName>_EventHandler<PotentialSuffix>
    IfFailGo(qbEventHandlerName.ReSizeNoThrow(wcslen(bstrSrcItfName) + wcslen(pMember->m_pName) + EVENT_HANDLER_SUFFIX_LENGTH + 6));
    StringCchPrintf(qbEventHandlerName.Ptr(), qbEventHandlerName.Size(), W("%s_%s%s"), bstrSrcItfName, pMember->m_pName, EVENT_HANDLER_SUFFIX);
    IfFailGo(GenerateUniqueTypeName(qbEventHandlerName));

    // Set the information on the current type.
    IfNullGo(m_szName = SysAllocString(qbEventHandlerName.Ptr()));

    // Retrieve the parent type ref.
    IfFailGo(GetKnownTypeToken(VT_SLOT_FOR_MULTICASTDEL, &trMulticastDelegate));

    // Create the typedef for the event interface.
    IfFailGo(m_pEmit->DefineTypeDef(m_szName, tdPublic | tdSealed, trMulticastDelegate, NULL, &m_tdTypeDef));

     // Hide the interface from Object Browsers (EventHandler)
     _SetHiddenCA(m_tdTypeDef);

    // Make the interface ComVisible(false).
    {
        DECLARE_CUSTOM_ATTRIBUTE(sizeof(BYTE));
        BUILD_CUSTOM_ATTRIBUTE(BYTE, FALSE);
        IfFailGo(GetAttrType(ATTR_COMVISIBLE, &tkAttr));
        FINISH_CUSTOM_ATTRIBUTE();
        IfFailGo(m_pEmit->DefineCustomAttribute(m_tdTypeDef, tkAttr, PTROF_CUSTOM_ATTRIBUTE(), SIZEOF_CUSTOM_ATTRIBUTE(), 0));
    }

    // Generate the sig for the constructor.
    IfFailGo(qbSig.ReSizeNoThrow(CB_MAX_ELEMENT_TYPE * 2 + 1));
    cbTotal = CorSigCompressData((ULONG)IMAGE_CEE_CS_CALLCONV_DEFAULT | IMAGE_CEE_CS_CALLCONV_HASTHIS, qbSig.Ptr());
    cb = CorSigCompressData(2, &(qbSig[cbTotal]));
    cbTotal += cb;
    cb = CorSigCompressData(ELEMENT_TYPE_VOID, &qbSig[cbTotal]);
    cbTotal += cb;
    cb = CorSigCompressData(ELEMENT_TYPE_OBJECT, &qbSig[cbTotal]);
    cbTotal += cb;
    cb = CorSigCompressData(ELEMENT_TYPE_U, &qbSig[cbTotal]);
    cbTotal += cb;

    // Generate the constructor.
    IfFailGo(m_pEmit->DefineMethod(m_tdTypeDef, OBJECT_INITIALIZER_NAME, OBJECT_INITIALIZER_FLAGS, 
        qbSig.Ptr(), cbTotal, 0 /* rva*/, miRuntime, &mdFunc));
    
    // Generate the invoke method.
    BOOL bAllowIEnum = !fInheritsIEnum;
    IfFailGo(_ConvFunction(pITI, pMember, FALSE, FALSE, TRUE, &bAllowIEnum));

    // Set the output typedef.
    *ptd = m_tdTypeDef;

ErrExit:
    if (m_szName)
        ::SysFreeString(m_szName);
    if (m_szMember)
        ::SysFreeString(m_szMember); m_szMember=0;
    if (bstrSrcItfName)
        ::SysFreeString(bstrSrcItfName);

    // Restore the initial values for the ITypeInfo name and the type def.
    m_szName = szOldName;
    m_tdTypeDef = tdOldTypeDef;

    return (hr);
} // HRESULT CImportTlb::_GenerateEventDelegate()

//*****************************************************************************
//*****************************************************************************
struct MDTOKENHASH : HASHLINK
{
    mdToken tkKey;
    mdToken tkData;
}; // struct MDTOKENHASH : HASHLINK

class CTokenHash : public CChainedHash<MDTOKENHASH>
{
public:
    virtual bool InUse(MDTOKENHASH *pItem)
    { return (pItem->tkKey != NULL); }

    virtual void SetFree(MDTOKENHASH *pItem)
    { 
        pItem->tkKey = NULL; 
        pItem->tkKey = NULL; 
    }

    virtual ULONG Hash(const void *pData)
    { 
        // Do case-insensitive hash
        return (ULONG)(ULONG_PTR)pData; 
    }

    virtual int Cmp(const void *pData, void *pItem){
        return (mdToken)(ULONG_PTR)pData != reinterpret_cast<MDTOKENHASH*>(pItem)->tkKey;
    }
}; // CTokenHash : public CChainedHash<MDTOKENHASH>

//*****************************************************************************
// Copy methods and events from a source interface to a class that sources the
//  given interface.
//*****************************************************************************
HRESULT CImportTlb::_AddSrcItfMembersToClass(   // S_OK or error.
    mdTypeRef   trSrcItf)                       // Typeref of the source interface.
{
    HRESULT             hr=S_OK;                // A result.
    ULONG               i;                      // Generic counter.
    HCORENUM            MemberEnum = NULL;      // The enum of members.
    ULONG               cMembers = 0;           // Temp count of members.
    mdTypeDef           tdSrcItf;               // A type def to the interface.
    mdEvent             tkItfEvent;             // The token of the interface event.
    mdEvent             tkClassEvent;           // The token of the class event.
    mdToken             tkEventType;            // The event type.
    mdMethodDef         mdItfMethod;            // The method def of the interface method.
    mdMethodDef         mdAddMethod;            // The add method.
    mdMethodDef         mdRemoveMethod;         // The remove method.
    mdMethodDef         mdFireMethod;           // The fire method.
    mdMethodDef         mdClassMethod;          // The method def of the class method.
    CQuickArray<mdMethodDef>  qbOtherMethods;   // The other methods for the property.
    ULONG               cchOtherMethods;        // The cound of other methods.
    CQuickArray<WCHAR>  qbMemberName;           // Name of the member.
    CQuickArray<WCHAR>  qbEventItfFullName;     // Full name of the event interface.
    CQuickArray<WCHAR>  qbEventItfName;         // Name of the event interface.
    ULONG               cchName;                // Length of a name, in wide chars.
    ULONG               ItfMemberAttr;          // The attributes of the interface member.
    ULONG               ItfMemberImplFlags;     // The impl flags of the interface member.               
    PCCOR_SIGNATURE     ItfMemberSig;           // The signature of the interface member.
    ULONG               ItfMemberSigSize;       // The size of the member signature.
    mdMemberRef         mrItfMember;            // A member ref to the interface member def.
    BSTR                bstrSrcItfName = NULL;  // The name of the CoClass.
    mdAssemblyRef       ar;                     // The assembly ref.
    CTokenHash          ItfMDToClassMDMap;      // The interface MD to class MD map.
    MDTOKENHASH *       pItem;                  // An item in the token hashtable.

    // Retrieve the name of the event interface.
    do {
        IfFailGo(m_pImport->GetTypeRefProps(
            trSrcItf, 
            &ar, 
            qbEventItfFullName.Ptr(), 
            (ULONG)qbEventItfFullName.MaxSize(), 
            &cchName));
        if (hr == CLDB_S_TRUNCATION)
        {
            IfFailGo(qbEventItfFullName.ReSizeNoThrow(cchName));
            continue;
        }
        break;
    } while (1);
    IfFailGo(qbEventItfName.ReSizeNoThrow(cchName));
    ns::SplitPath(qbEventItfFullName.Ptr(), NULL, 0, qbEventItfName.Ptr(), (int)qbEventItfName.Size());

    // Resolve the typeref to a typedef.
    IfFailGo(m_pImport->FindTypeDefByName(qbEventItfFullName.Ptr(), mdTokenNil, &tdSrcItf));

    // Define methods and method impl's for all the methods in the interface.
    while ((hr = m_pImport->EnumMethods(&MemberEnum, tdSrcItf, &mdItfMethod, 1, &cMembers)) == S_OK)
    {
        // Retrieve the method properties.
        do {
            IfFailGo(m_pImport->GetMethodProps(
                mdItfMethod,
                NULL,
                qbMemberName.Ptr(),
                (ULONG)qbMemberName.MaxSize(),
                &cchName,
                &ItfMemberAttr,
                &ItfMemberSig,
                &ItfMemberSigSize,
                NULL,
                &ItfMemberImplFlags));
            if (hr == CLDB_S_TRUNCATION)
            {
                IfFailGo(qbMemberName.ReSizeNoThrow(cchName));
                continue;
            }
            break;
        } while (1);

        // Define a member ref on the class to the interface member def.
        IfFailGo(m_pEmit->DefineMemberRef(trSrcItf, qbMemberName.Ptr(), ItfMemberSig, ItfMemberSigSize, &mrItfMember));

        // Generate a unique name for the class member.
        IfFailGo(GenerateUniqueMemberName(qbMemberName, NULL, 0, qbEventItfName.Ptr(), mdtMethodDef));

        // Define a member on the class.
        IfFailGo(m_pEmit->DefineMethod(m_tdTypeDef, qbMemberName.Ptr(), ItfMemberAttr & ~mdAbstract,
            ItfMemberSig, ItfMemberSigSize, 0/*rva*/, ItfMemberImplFlags, &mdClassMethod));

        // Define a method impl.
        IfFailGo(m_pEmit->DefineMethodImpl(m_tdTypeDef, mdClassMethod, mrItfMember));

        // Add the interface member to the map.
        if ((pItem = ItfMDToClassMDMap.Add((const void *)(ULONG_PTR)mdItfMethod)) == NULL)
            IfFailGo(E_FAIL);
        PREFIX_ASSUME(pItem != NULL);
        pItem->tkKey = mdItfMethod;
        pItem->tkData = mdClassMethod;
    }
    IfFailGo(hr);

    m_pImport->CloseEnum(MemberEnum);
    MemberEnum = NULL;

    // Define all the events in the interface on the class.
    while ((hr = m_pImport->EnumEvents(&MemberEnum, tdSrcItf, &tkItfEvent, 1, &cMembers)) == S_OK)
    {
        // Retrieve the properties of the property.
        do {
            IfFailGo(m_pImport->GetEventProps(
                tkItfEvent,
                NULL,               
                qbMemberName.Ptr(),    
                (ULONG)qbMemberName.MaxSize(),
                &cchName,         
                &ItfMemberAttr,   
                &tkEventType,    
                &mdAddMethod,
                &mdRemoveMethod,
                &mdFireMethod,
                qbOtherMethods.Ptr(),
                (ULONG)qbOtherMethods.MaxSize(),
                &cchOtherMethods));
            if (hr == CLDB_S_TRUNCATION)
            {
                IfFailGo(qbMemberName.ReSizeNoThrow(cchName));
                IfFailGo(qbOtherMethods.ReSizeNoThrow(cchOtherMethods));
                continue;
            }
            break;
        } while (1);

        // NULL terminate the array of other methods.
        IfFailGo(qbOtherMethods.ReSizeNoThrow(cchOtherMethods + 1));
        qbOtherMethods[cchOtherMethods] = NULL;

        // Replace all the interface method def's with the equivalent class method def's.
        if (!IsNilToken(mdAddMethod))
        {
            pItem = ItfMDToClassMDMap.Find((const void *)(ULONG_PTR)mdAddMethod);
            _ASSERTE(pItem);
            mdAddMethod = pItem->tkData;
        }
        if (!IsNilToken(mdRemoveMethod))
        {
            pItem = ItfMDToClassMDMap.Find((const void *)(ULONG_PTR)mdRemoveMethod);
            _ASSERTE(pItem);
            mdRemoveMethod = pItem->tkData;
        }
        _ASSERTE(IsNilToken(mdFireMethod));
        _ASSERTE(cchOtherMethods == 0);

        // Generate a unique name for the event.
        IfFailGo(GenerateUniqueMemberName(qbMemberName, NULL, 0, qbEventItfName.Ptr(), mdtEvent));

        // Define property on the class.
        IfFailGo(m_pEmit->DefineEvent(m_tdTypeDef, qbMemberName.Ptr(), ItfMemberAttr,
            tkEventType, mdAddMethod, mdRemoveMethod, mdFireMethod, qbOtherMethods.Ptr(), &tkClassEvent));
    }
    IfFailGo(hr);   

    m_pImport->CloseEnum(MemberEnum);
    MemberEnum = NULL;

ErrExit:
    if (MemberEnum)
        m_pImport->CloseEnum(MemberEnum);
    if (bstrSrcItfName)
        ::SysFreeString(bstrSrcItfName);

    return hr;

#undef ITF_MEMBER_SIG
#undef ITF_MEMBER_SIG_SIZE
} // HRESULT CImportTlb::_AddSrcItfMembersToClass()

//*****************************************************************************
// Compare the two signatures ignoring the return type. If the signatures
// match then TRUE will be returned, FALSE will be returned otherwise.
// This method assumes the two signatures are in the same scope.
//*****************************************************************************
HRESULT CImportTlb::CompareSigsIgnoringRetType(
    PCCOR_SIGNATURE pbSig1,           // The 1st method signature.
    ULONG           cbSig1,           // Size of the 1st method signature.
    PCCOR_SIGNATURE pbSig2,           // The 2nd method signature.
    ULONG           cbSig2)           // Size of the 2nd method signature.
{
    HRESULT             hr = S_OK;
    PCCOR_SIGNATURE     pbSig1Start;  
    PCCOR_SIGNATURE     pbSig2Start;  
    ULONG               Sig1ParamCount;
    ULONG               Sig2ParamCount;
    ULONG               cbSig1RetType;
    ULONG               cbSig2RetType;

    // Save the start of the signatures.
    pbSig1Start = pbSig1;  
    pbSig2Start = pbSig2;  

    // Skip the calling conventions.
    CorSigUncompressData(pbSig1);
    CorSigUncompressData(pbSig2);

    // Compare the param count.
    Sig1ParamCount = CorSigUncompressData(pbSig1);
    Sig2ParamCount = CorSigUncompressData(pbSig2);
    if (Sig1ParamCount != Sig2ParamCount)
        return S_FALSE;

    // Skip the return type.
    cbSig1RetType = cbSig1 - (ULONG)(pbSig1 - pbSig1Start);
    IfFailGo(_CountBytesOfOneArg(pbSig1, &cbSig1RetType));
    pbSig1 += cbSig1RetType;
    cbSig2RetType = cbSig2 - (ULONG)(pbSig2 - pbSig2Start);
    IfFailGo(_CountBytesOfOneArg(pbSig2, &cbSig2RetType));
    pbSig2 += cbSig2RetType;

    // Update the remaining sig sizes.
    cbSig1 -= (ULONG) (pbSig1 - pbSig1Start);
    cbSig2 -= (ULONG) (pbSig2 - pbSig2Start);

    // If the remaining sig sizes are different then the sigs don't match.
    if (cbSig1 != cbSig2)
        return S_FALSE;

    // Compare the rest of the sigs using memcmp.
    if (memcmp(pbSig1, pbSig2, cbSig1) != 0)
        return S_FALSE;

    // The parameters match.
    return S_OK;

ErrExit:
    // An error occurred.
    return hr;
} // HRESULT CImportTlb::CompareSigsIgnoringRetType()

//*****************************************************************************
// Look up a method in the emit scope. This lookup method does not take the
// return type into account when comparing using a sig. So 2 methods with 
// the same name, same parameters and a different return type will be 
// considered the same.
//*****************************************************************************
HRESULT CImportTlb::FindMethod(         // S_OK or CLDB_E_RECORD_NOTFOUND, or error.
    mdTypeDef   td,                     // The method typedef.
    LPCWSTR     szName,                 // The method name.
    PCCOR_SIGNATURE pbReqSig,              // The method signature.
    ULONG       cbReqSig,               // Size of the method signature.
    mdMethodDef *pmb)                   // Put the method here.
{
    HRESULT             hr = S_OK;          // A result.
    PCCOR_SIGNATURE     pbFoundSig = NULL;  
    ULONG               cbFoundSig = 0;
    ULONG               MethodAttr;             
    ULONG               MethodImplFlags;     
    mdMethodDef         md;
    CQuickArray<WCHAR>  qbMethodName;
    HCORENUM            MethodEnum = NULL;
    ULONG               cMethods = 0;
    ULONG               cchName;
    BOOL                bMethodFound = FALSE;

    // Go through all the methods on the class looking for one with the
    // same name and same parameters.
    while ((hr = m_pImport->EnumMethods(&MethodEnum, td, &md, 1, &cMethods)) == S_OK)
    {
        // Retrieve the method properties.
        do {
            IfFailGo(m_pImport->GetMethodProps(
                md,
                NULL,
                qbMethodName.Ptr(),
                (ULONG)qbMethodName.MaxSize(),
                &cchName,
                &MethodAttr,
                &pbFoundSig,
                &cbFoundSig,
                NULL,
                &MethodImplFlags));
            if (hr == CLDB_S_TRUNCATION)
            {
                IfFailGo(qbMethodName.ReSizeNoThrow(cchName));
                continue;
            }
            break;
        } while (1);

        // Compare the name of the method.
        if (wcscmp(szName, qbMethodName.Ptr()) != 0)
            continue;

        // If the signature of the requested method is specified, then compare
        // the signature against the signature of the found method ignoring
        // the return type.
        if (pbReqSig)
        {
            IfFailGo(hr = CompareSigsIgnoringRetType(pbReqSig, cbReqSig, pbFoundSig, cbFoundSig));
            if (hr == S_FALSE)
                continue;           
        }

    // We have found the member.
        bMethodFound = TRUE;
        break;
    }
    IfFailGo(hr);

ErrExit:
    if (MethodEnum)
        m_pImport->CloseEnum(MethodEnum);

    return bMethodFound ? S_OK : CLDB_E_RECORD_NOTFOUND;
}

//*****************************************************************************
// Look up a property in the emit scope.
//*****************************************************************************
HRESULT CImportTlb::FindProperty(      // S_OK or CLDB_E_RECORD_NOTFOUND, or error.
    mdTypeDef   td,                     // The property typedef.
    LPCWSTR     szName,                 // The property name.
    PCCOR_SIGNATURE pbSig,                 // The property signature.
    ULONG       cbSig,                  // Size of the property signature.
    mdProperty  *ppr)                   // Put the property here.
{
    HRESULT     hr;                     // A result.
    RegMeta     *pRegMeta = (RegMeta*)(m_pEmit);
    LPUTF8      szNameAnsi;

    if (szName == NULL)
    {
        return CLDB_E_RECORD_NOTFOUND;
    }
    UTF8STR(szName, szNameAnsi);

    hr = ImportHelper::FindProperty(
             &(pRegMeta->GetMiniStgdb()->m_MiniMd), 
             m_tdTypeDef, 
             szNameAnsi, 
             pbSig, 
             cbSig, 
             ppr);
    return hr;
} // HRESULT CImportTlb::FindProperty()

//*****************************************************************************
// Look up a event in the emit scope.
//*****************************************************************************
HRESULT CImportTlb::FindEvent(          // S_OK or CLDB_E_RECORD_NOTFOUND, or error.
    mdTypeDef   td,                     // The event typedef.
    LPCWSTR     szName,                 // The event name.
    mdEvent     *pev)                   // Put the event here.
{
    HRESULT     hr;                     // A result.
    RegMeta     *pRegMeta = (RegMeta*)(m_pEmit);
    LPUTF8      szNameAnsi;

    if (szName == NULL)
    {
        return CLDB_E_RECORD_NOTFOUND;
    }
    UTF8STR(szName, szNameAnsi);

    hr = ImportHelper::FindEvent(
             &(pRegMeta->GetMiniStgdb()->m_MiniMd), 
             m_tdTypeDef, 
             szNameAnsi, 
             pev);
    return hr;
} // HRESULT CImportTlb::FindEvent()

//*****************************************************************************
// Checks to see if the specified TYPEDESC is an alias.
//*****************************************************************************
HRESULT CImportTlb::_IsAlias(
    ITypeInfo   *pITI,                  // The ITypeInfo containing the TYPEDESC.
    TYPEDESC    *pTypeDesc)             // The token of the param, field, etc.
{
    HRESULT     hr = S_FALSE;           // A result.
    ITypeInfo   *pTypeITI=0;            // The ITypeInfo of the type.
    ITypeLib    *pTypeTLB=0;            // The TLB that contains the type.
    TYPEATTR    *psTypeAttr=0;          // TYPEATTR of the type.

    // Drill down to the actual type that is pointed to.
    while (pTypeDesc->vt == VT_PTR)
        pTypeDesc = pTypeDesc->lptdesc;

    // If the parameter is an alias then we need to add a custom attribute to the 
    // parameter that describes the alias.
    if (pTypeDesc->vt == VT_USERDEFINED)
    {
        IfFailGo(pITI->GetRefTypeInfo(pTypeDesc->hreftype, &pTypeITI));
        IfFailGo(pTypeITI->GetTypeAttr(&psTypeAttr));
        if (psTypeAttr->typekind == TKIND_ALIAS)
        {
            hr = S_OK;
        }
    }

ErrExit:
    if (psTypeAttr)
        pTypeITI->ReleaseTypeAttr(psTypeAttr);
    if (pTypeITI)
        pTypeITI->Release();
    return hr;
} // HRESULT CImportTlb::_IsAlias()

//*****************************************************************************
// Add alias information if the TYPEDESC represents an alias.
//*****************************************************************************
HRESULT CImportTlb::_HandleAliasInfo(
    ITypeInfo   *pITI,                  // The ITypeInfo containing the TYPEDESC.
    TYPEDESC    *pTypeDesc,             // The TYPEDESC.
    mdToken     tk)                     // The token of the param, field, etc.
{
    HRESULT     hr = S_OK;              // A result.
    ITypeInfo   *pTypeITI=0;            // The ITypeInfo of the type.
    ITypeLib    *pTypeTLB=0;            // The TLB that contains the type.
    TYPEATTR    *psTypeAttr=0;          // TYPEATTR of the type.
    BSTR        bstrAliasTypeName=0;    // The name of the alias type.
    BSTR        bstrAliasTypeLibName=0; // The name of the typelib that contains the alias type.

    // Drill down to the actual type that is pointed to.
    while (pTypeDesc->vt == VT_PTR)
        pTypeDesc = pTypeDesc->lptdesc;

    // If the parameter is an alias then we need to add a custom attribute to the 
    // parameter that describes the alias.
    if (pTypeDesc->vt == VT_USERDEFINED)
    {
        IfFailGo(pITI->GetRefTypeInfo(pTypeDesc->hreftype, &pTypeITI));
        IfFailGo(pTypeITI->GetTypeAttr(&psTypeAttr));
        if (psTypeAttr->typekind == TKIND_ALIAS)
        {
            // Retrieve the name of the alias type.
            IfFailGo(pTypeITI->GetContainingTypeLib(&pTypeTLB, NULL));
            IfFailGo(GetNamespaceOfRefTlb(pTypeTLB, &bstrAliasTypeLibName, NULL));
            IfFailGo(GetManagedNameForTypeInfo(pTypeITI, bstrAliasTypeLibName, NULL, &bstrAliasTypeName));

            // Add the ComAliasName CA to the parameter.
            _AddStringCa(ATTR_COMALIASNAME, tk, bstrAliasTypeName);
        }
    }

ErrExit:
    if (psTypeAttr)
        pTypeITI->ReleaseTypeAttr(psTypeAttr);
    if (pTypeITI)
        pTypeITI->Release();
    if (pTypeTLB)
        pTypeTLB->Release();
    if (bstrAliasTypeLibName)
        ::SysFreeString(bstrAliasTypeLibName);   
    if (bstrAliasTypeName)
        ::SysFreeString(bstrAliasTypeName);
    return hr;
} // HRESULT CImportTlb::_HandleAliasInfo()

//*****************************************************************************
// Convert one of a function's parameters.
//*****************************************************************************
HRESULT CImportTlb::_ConvParam(
    ITypeInfo   *pITI,                  // Containing TypeInfo.
    mdMethodDef mdFunc,                 // Owning member.
    int         iSequence,              // Parameter sequence.
    const ELEMDESC *pdesc,              // Param flags, default value.
    ParamOpts   paramOpts,              // Is param normal, optional, or vararg?
    LPCWSTR     szName,                 // Name of the parameter.
    BYTE        *pbNative,              // Native type info, if any.
    ULONG       cbNative)               // Size of native type info.
{
    HRESULT     hr;                     // A result.
    mdParamDef  pdParam;                // Token of the parameter.
    DWORD       dwFlags;                // Param flags.
    USHORT      Sequence = static_cast<USHORT>(iSequence);
    BYTE        cvType  = ELEMENT_TYPE_VOID; // ELEMENT_TYPE_* flag for constant value
    void        *pcvValue=0;            // constant value blob
    __int64     d;                      // For cases where value is a date.
    int         bDecimal=0;             // If true, constant is a decimal.
    mdToken     tkAttr;                 // For custom attribute token.
    DECIMAL     decVal;                 // Decimal constant value.

    // Compute the flags.  Only make sense on non-return params.
    dwFlags = 0;
    if (iSequence > 0)
    {
        if (pdesc->paramdesc.wParamFlags & PARAMFLAG_FIN)
            dwFlags |= pdIn;
        if (pdesc->paramdesc.wParamFlags & PARAMFLAG_FOUT)
            dwFlags |= pdOut;
        if (pdesc->paramdesc.wParamFlags & PARAMFLAG_FOPT)
            dwFlags |= pdOptional;
        if (paramOpts == ParamOptional)
            dwFlags |= pdOptional;
    }

    // Get any default values.  Return type, param with iSequence==0, has no default.
    if (pdesc->paramdesc.wParamFlags & PARAMFLAG_FHASDEFAULT && iSequence != 0)
    {
        switch (pdesc->paramdesc.pparamdescex->varDefaultValue.vt)
        {
        case VT_CY:
        case VT_DECIMAL:
        case VT_DATE:
        case VT_UNKNOWN:
        case VT_DISPATCH:
            break;
        default:
            // This workaround is because a typelib can store anything that can convert to VT_I4 with a value of 0
            //  for the default value of an interface pointer.  But, a VT_I2(0) confuses the consumers
            //  of the managed wrapper dll.  So, if it is an interface on the unmanaged side, make
            //  the constant value an ET_CLASS.
            if (cbNative > 0 && (*pbNative == NATIVE_TYPE_INTF ||
                                 *pbNative == NATIVE_TYPE_IUNKNOWN ||
                                 *pbNative == NATIVE_TYPE_IDISPATCH))
        {
                cvType = ELEMENT_TYPE_CLASS;
                pcvValue = 0;
        }
        else
            IfFailGo( _UnpackVariantToConstantBlob(&pdesc->paramdesc.pparamdescex->varDefaultValue, &cvType, &pcvValue, &d) );
        }
    }

    // Create the param definition.
    IfFailGo(m_pEmit->DefineParam(mdFunc, iSequence, szName, dwFlags, cvType, pcvValue, -1, &pdParam));

    // Add the native type if it there is any.
    if (cbNative > 0)
        IfFailGo(m_pEmit->SetFieldMarshal(pdParam, (PCCOR_SIGNATURE) pbNative, cbNative));

    if (pdesc->paramdesc.wParamFlags & PARAMFLAG_FHASDEFAULT && iSequence != 0)
    {
        switch (pdesc->paramdesc.pparamdescex->varDefaultValue.vt)
        {
        case VT_CY:
            IfFailGo(VarDecFromCy(pdesc->paramdesc.pparamdescex->varDefaultValue.cyVal, &decVal));
            IfFailGo(DecimalCanonicalize(&decVal));
            goto StoreDecimal;
        case VT_DECIMAL:
            // If there is a decimal constant value, set it as a custom attribute.
            {
            decVal = pdesc->paramdesc.pparamdescex->varDefaultValue.decVal;
        StoreDecimal:
            DECLARE_CUSTOM_ATTRIBUTE(sizeof(BYTE)+sizeof(BYTE)+sizeof(UINT)+sizeof(UINT)+sizeof(UINT));
            BUILD_CUSTOM_ATTRIBUTE(BYTE, decVal.scale);
            BUILD_CUSTOM_ATTRIBUTE(BYTE, decVal.sign);
            BUILD_CUSTOM_ATTRIBUTE(UINT, decVal.Hi32);
            BUILD_CUSTOM_ATTRIBUTE(UINT, decVal.Mid32);
            BUILD_CUSTOM_ATTRIBUTE(UINT, decVal.Lo32);
            IfFailGo(GetAttrType(ATTR_DECIMALVALUE, &tkAttr));
            FINISH_CUSTOM_ATTRIBUTE();
            IfFailGo(m_pEmit->DefineCustomAttribute(pdParam, tkAttr, PTROF_CUSTOM_ATTRIBUTE(), SIZEOF_CUSTOM_ATTRIBUTE(),0));
            }
            break;
        case VT_DATE:
            {
            DECLARE_CUSTOM_ATTRIBUTE(sizeof(__int64));
            __int64 date = _DoubleDateToTicks(pdesc->paramdesc.pparamdescex->varDefaultValue.date);
            BUILD_CUSTOM_ATTRIBUTE(__int64, date);
            IfFailGo(GetAttrType(ATTR_DATETIMEVALUE, &tkAttr));
            FINISH_CUSTOM_ATTRIBUTE();
            IfFailGo(m_pEmit->DefineCustomAttribute(pdParam, tkAttr, PTROF_CUSTOM_ATTRIBUTE(), SIZEOF_CUSTOM_ATTRIBUTE(),0));
            }
            break;
        case VT_UNKNOWN:
            {
            DECLARE_CUSTOM_ATTRIBUTE(0);
            IfFailGo(GetAttrType(ATTR_IUNKNOWNVALUE, &tkAttr));
            FINISH_CUSTOM_ATTRIBUTE();
            IfFailGo(m_pEmit->DefineCustomAttribute(pdParam, tkAttr, PTROF_CUSTOM_ATTRIBUTE(), SIZEOF_CUSTOM_ATTRIBUTE(),0));
            }
            break;
        case VT_DISPATCH:
            {
            DECLARE_CUSTOM_ATTRIBUTE(0);
            IfFailGo(GetAttrType(ATTR_IDISPATCHVALUE, &tkAttr));
            FINISH_CUSTOM_ATTRIBUTE();
            IfFailGo(m_pEmit->DefineCustomAttribute(pdParam, tkAttr, PTROF_CUSTOM_ATTRIBUTE(), SIZEOF_CUSTOM_ATTRIBUTE(),0));
            }
            break;
        default:
            break;
        }
    }

    // Add the alias information if the param is an alias.
    IfFailGo(_HandleAliasInfo(pITI, (TYPEDESC*)&pdesc->tdesc, pdParam));
    
    // If a vararg param, set the custom attribute.
    if (paramOpts == ParamVarArg)
    {
        mdToken     tkAttribute;
        DECLARE_CUSTOM_ATTRIBUTE(0);
        FINISH_CUSTOM_ATTRIBUTE();
        IfFailGo(GetAttrType(ATTR_PARAMARRAY, &tkAttribute));
        IfFailGo(m_pEmit->DefineCustomAttribute(pdParam, tkAttribute, PTROF_CUSTOM_ATTRIBUTE(), SIZEOF_CUSTOM_ATTRIBUTE(), 0));
    }

ErrExit:
    return hr;
} // HRESULT CImportTlb::_ConvParam()

//*****************************************************************************
// Convert a constant into a field with a default value.
//*****************************************************************************
HRESULT CImportTlb::_ConvConstant(
    ITypeInfo   *pITI,                  // Containing TypeInfo.
    VARDESC     *psVar,                 // VARDESC for the property.
    BOOL        bEnumMember)            // If true, type is containing class.
{
    HRESULT     hr;                     // A result.
    mdFieldDef  mdField;                // Token of the new field.
    DWORD       dwFlags;                // Member flags.
    CQuickBytes qbComSig;               // The COM+ Signature of the field.
    ULONG       cb, cbTotal;
    BYTE        cvType = ELEMENT_TYPE_VOID; // E_T_Type for constant value
    void        *pcvValue;              // Pointer to constant value data.
    mdToken     tkAttr;                 // Type for custom attribute.
    __int64     d;                      // For cases where value is a date.
    BOOL        bConversionLoss=false;  // If true, some attributes were lost on conversion.
    BYTE        *pbSig;                 // Pointer to signature bytes.
    CQuickArray<BYTE> qbNativeBuf;      // Native type buffer.
    ULONG       cbNative = 0;           // Size of native type.
    int         bDecimal = 0;           // If the value is a decimal.
    DECIMAL     decVal;                 // Decimal constant value.

    // Information about the member.
    IfFailGo(pITI->GetDocumentation(psVar->memid, &m_szMember, 0,0,0));

    // resize to make room for calling convention and count of argument
    IfFailGo(qbComSig.ReSizeNoThrow(CB_MAX_ELEMENT_TYPE * 4));
    pbSig = (BYTE *)qbComSig.Ptr();

    // Compute properties.
    dwFlags = DEFAULT_CONST_FIELD_FLAGS;

    // Build the signature.
    cbTotal = cb = CorSigCompressData((ULONG)IMAGE_CEE_CS_CALLCONV_FIELD, pbSig);
    if (bEnumMember)
    {
        cb = CorSigCompressData(ELEMENT_TYPE_VALUETYPE, &pbSig[cbTotal]);
        cbTotal += cb;
        cb = CorSigCompressToken(m_tdTypeDef, reinterpret_cast<ULONG*>(&pbSig[cbTotal]));
        cbTotal += cb;
    }
    else
    {
        // Use the conversion function to get the signature.
        ULONG cbSave = cbTotal;
        IfFailGo(_ConvSignature(pITI, &psVar->elemdescVar.tdesc, SIG_FLAGS_NONE, qbComSig, cbTotal, &cbTotal, qbNativeBuf, 0, &cbNative, FALSE));
        if (hr == S_CONVERSION_LOSS)
            bConversionLoss = true;
        
        if (psVar->elemdescVar.tdesc.vt == VT_DATE)
        {
            // But for dates, convert it as float -- DateTime is reported as R4 in a typelib!
            cbTotal = cbSave;
            cb = CorSigCompressData(ELEMENT_TYPE_R4, &pbSig[cbTotal]);
            cbTotal += cb;
        }
    }
    
    // Get the default value.
    switch (psVar->lpvarValue->vt)
    {
    case VT_CY:
    case VT_DECIMAL:
    case VT_DATE:
    case VT_UNKNOWN:
    case VT_DISPATCH:
        break;
    default:
        // This workaround is because a typelib can store anything that can convert to VT_I4 with a value of 0
        //  for the default value of an interface pointer.  But, a VT_I2(0) confuses the consumers
        //  of the managed wrapper dll.  So, if it is an interface on the unmanaged side, make
        //  the constant value an ET_CLASS.
        BYTE *pbNative = NULL;
        pbNative = qbNativeBuf.Ptr();
        if (cbNative > 0 && (*pbNative == NATIVE_TYPE_INTF ||
                             *pbNative == NATIVE_TYPE_IUNKNOWN ||
                             *pbNative == NATIVE_TYPE_IDISPATCH))
        {
            cvType = ELEMENT_TYPE_CLASS;
            pcvValue = 0;
        }
        else
            IfFailGo( _UnpackVariantToConstantBlob(psVar->lpvarValue, &cvType, &pcvValue, &d) );
    }

    // Create the field definition.
    IfFailGo(m_pEmit->DefineField(m_tdTypeDef, m_szMember, dwFlags, (PCCOR_SIGNATURE)pbSig, cbTotal, 
        cvType, pcvValue, -1, &mdField));

    switch (psVar->lpvarValue->vt)
    {
    case VT_CY:
        IfFailGo(VarDecFromCy(psVar->lpvarValue->cyVal, &decVal));
        IfFailGo(DecimalCanonicalize(&decVal));
        goto StoreDecimal;
    case VT_DECIMAL:
        // If there is a decimal constant value, set it as a custom attribute.
        {
        decVal = psVar->lpvarValue->decVal;
    StoreDecimal:
        DECLARE_CUSTOM_ATTRIBUTE(sizeof(BYTE)+sizeof(BYTE)+sizeof(UINT)+sizeof(UINT)+sizeof(UINT));
        BUILD_CUSTOM_ATTRIBUTE(BYTE, decVal.scale);
        BUILD_CUSTOM_ATTRIBUTE(BYTE, decVal.sign);
        BUILD_CUSTOM_ATTRIBUTE(UINT, decVal.Hi32);
        BUILD_CUSTOM_ATTRIBUTE(UINT, decVal.Mid32);
        BUILD_CUSTOM_ATTRIBUTE(UINT, decVal.Lo32);
        IfFailGo(GetAttrType(ATTR_DECIMALVALUE, &tkAttr));
        FINISH_CUSTOM_ATTRIBUTE();
        IfFailGo(m_pEmit->DefineCustomAttribute(mdField, tkAttr, PTROF_CUSTOM_ATTRIBUTE(), SIZEOF_CUSTOM_ATTRIBUTE(),0));
        }
        break;
    case VT_DATE:
        {
        DECLARE_CUSTOM_ATTRIBUTE(sizeof(__int64));
        __int64 date = _DoubleDateToTicks(psVar->lpvarValue->date);
        BUILD_CUSTOM_ATTRIBUTE(__int64, date);
        IfFailGo(GetAttrType(ATTR_DATETIMEVALUE, &tkAttr));
        FINISH_CUSTOM_ATTRIBUTE();
        IfFailGo(m_pEmit->DefineCustomAttribute(mdField, tkAttr, PTROF_CUSTOM_ATTRIBUTE(), SIZEOF_CUSTOM_ATTRIBUTE(),0));
        }
        break;
    case VT_UNKNOWN:
        {
        DECLARE_CUSTOM_ATTRIBUTE(0);
        IfFailGo(GetAttrType(ATTR_IUNKNOWNVALUE, &tkAttr));
        FINISH_CUSTOM_ATTRIBUTE();
        IfFailGo(m_pEmit->DefineCustomAttribute(mdField, tkAttr, PTROF_CUSTOM_ATTRIBUTE(), SIZEOF_CUSTOM_ATTRIBUTE(),0));
        }
        break;
    case VT_DISPATCH:
        {
        DECLARE_CUSTOM_ATTRIBUTE(0);
        IfFailGo(GetAttrType(ATTR_IDISPATCHVALUE, &tkAttr));
        FINISH_CUSTOM_ATTRIBUTE();
        IfFailGo(m_pEmit->DefineCustomAttribute(mdField, tkAttr, PTROF_CUSTOM_ATTRIBUTE(), SIZEOF_CUSTOM_ATTRIBUTE(),0));
        }
        break;
    default:
        break;
    }

    // Save the field flags.
    if (psVar->wVarFlags)
    {
        IfFailGo(GetAttrType(ATTR_TYPELIBVAR, &tkAttr));
        DECLARE_CUSTOM_ATTRIBUTE(sizeof(WORD));
        BUILD_CUSTOM_ATTRIBUTE(WORD, psVar->wVarFlags);
        FINISH_CUSTOM_ATTRIBUTE();
        IfFailGo(m_pEmit->DefineCustomAttribute(mdField, tkAttr, PTROF_CUSTOM_ATTRIBUTE(), SIZEOF_CUSTOM_ATTRIBUTE(),0));
    }

    // Set up the native description, if any.
    if (cbNative > 0)
        IfFailGo(m_pEmit->SetFieldMarshal(mdField, (PCCOR_SIGNATURE) qbNativeBuf.Ptr(), cbNative));

    // Add the alias information if the type is an alias.
    IfFailGo(_HandleAliasInfo(pITI, &psVar->elemdescVar.tdesc, mdField));
    
    if (bConversionLoss)
    {
        hr = S_CONVERSION_LOSS;
        ReportEvent(NOTIF_CONVERTWARNING, TLBX_I_UNCONVERTABLE_FIELD, m_szName, m_szMember);
    }

ErrExit:
    if (m_szMember)
        ::SysFreeString(m_szMember), m_szMember=0;
    return (hr);
} // HRESULT CImportTlb::_ConvConstant()

//*****************************************************************************
// Convert a (record) field into a member.
//*****************************************************************************
HRESULT CImportTlb::_ConvField(
    ITypeInfo   *pITI,                  // Containing TypeInfo.
    VARDESC     *psVar,                 // VARDESC for the property.
    mdFieldDef  *pmdField,              // Put field token here.
    BOOL        bUnion)                 // Convert as a union?
{
    HRESULT     hr;                     // A result.
    DWORD       dwFlags;                // Member flags.
    CQuickBytes qbComSig;               // The COM+ Signature of the field.
    ULONG       cb, cbTotal;            // Size of a sig element, signature.
    BYTE        *pbSig;                 // Pointer to signature bytes.
    CQuickArray<BYTE> qbNativeBuf;      // Native type buffer.
    ULONG       cbNative;               // Size of native type.
    mdToken     tkAttr;                 // CustomAttribute type.
    BOOL        bConversionLoss=false;  // If true, some attributes were lost on conversion.

    // Information about the member.
    IfFailGo(pITI->GetDocumentation(psVar->memid, &m_szMember, 0,0,0));

    // Compute properties.
    dwFlags = DEFAULT_RECORD_FIELD_FLAGS;

    // resize to make room for calling convention and count of argument
    IfFailGo(qbComSig.ReSizeNoThrow(CB_MAX_ELEMENT_TYPE * 2));
    pbSig = (BYTE *)qbComSig.Ptr();

    // Build the signature.
    cbTotal = cb = CorSigCompressData((ULONG)IMAGE_CEE_CS_CALLCONV_FIELD, pbSig);
    IfFailGo(_ConvSignature(pITI, &psVar->elemdescVar.tdesc, SIG_FIELD, qbComSig, cbTotal, &cbTotal, qbNativeBuf, 0, &cbNative, FALSE));
    if (hr == S_CONVERSION_LOSS)
        bConversionLoss = true;

    // Create the field definition.
    IfFailGo(m_pEmit->DefineField(m_tdTypeDef, m_szMember, dwFlags, (PCCOR_SIGNATURE) qbComSig.Ptr(),cbTotal, 
        ELEMENT_TYPE_VOID, NULL, -1, pmdField));

    // Save the field flags.
    if (psVar->wVarFlags)
    {
        IfFailGo(GetAttrType(ATTR_TYPELIBVAR, &tkAttr));
        DECLARE_CUSTOM_ATTRIBUTE(sizeof(WORD));
        BUILD_CUSTOM_ATTRIBUTE(WORD, psVar->wVarFlags);
        FINISH_CUSTOM_ATTRIBUTE();
        IfFailGo(m_pEmit->DefineCustomAttribute(*pmdField, tkAttr, PTROF_CUSTOM_ATTRIBUTE(), SIZEOF_CUSTOM_ATTRIBUTE(),0));
    }

    if (bConversionLoss)
    {
        IfFailGo(GetAttrType(ATTR_COMCONVERSIONLOSS, &tkAttr));
        DECLARE_CUSTOM_ATTRIBUTE(0);
        FINISH_CUSTOM_ATTRIBUTE();
        IfFailGo(m_pEmit->DefineCustomAttribute(*pmdField, tkAttr, PTROF_CUSTOM_ATTRIBUTE(),SIZEOF_CUSTOM_ATTRIBUTE(),0));
    }

    // Set up the native description, if any.
    if (cbNative > 0)
        IfFailGo(m_pEmit->SetFieldMarshal(*pmdField, (PCCOR_SIGNATURE) qbNativeBuf.Ptr(), cbNative));

    // Add the alias information if the type is an alias.
    IfFailGo(_HandleAliasInfo(pITI, &psVar->elemdescVar.tdesc, *pmdField));
    
    if (bConversionLoss)
    {
        hr = S_CONVERSION_LOSS;
        ReportEvent(NOTIF_CONVERTWARNING, TLBX_I_UNCONVERTABLE_FIELD, m_szName, m_szMember);
    }

ErrExit:
    if (m_szMember)
        ::SysFreeString(m_szMember), m_szMember=0;
    return (hr);
} // HRESULT CImportTlb::_ConvField()

//*****************************************************************************
// Convert a dispatch property into a pair of get/set functions.
//*****************************************************************************
HRESULT CImportTlb::_ConvProperty(
    ITypeInfo   *pITI,                  // Containing TypeInfo.
    MemberInfo  *pMember)               // VARDESC for the property.
{
    HRESULT     hr;                     // A result.
    mdMethodDef mdFuncGet;              // A get function.
    mdMethodDef mdFuncSet;              // A set function.
    mdProperty  pdProperty;             // Property on the two functions.
    DWORD       dwFlags;                // Function flags.
    WCHAR       *pszName=0;             // Decorated name of member.
    CQuickArray<WCHAR> qbName;          // Buffer for decorated name.
    CQuickBytes qbComSig;               // com signature buffer
    ULONG       cb;                     // Size of an element.
    ULONG       cbTotal = 0;            // Total size of signature.
    BYTE        *pbSig;                 // Pointer to signature buffer.
    BOOL        bConversionLoss=false;  // If true, some attributes were lost on conversion.
    CQuickArray<BYTE> qbNativeBuf;      // Native type buffer.
    ULONG       iNativeOfs=0;           // Current offset in native type buffer.
    VARDESC     *psVar = pMember->m_psVar;

    // Check to see if the property is the NewEnum member.
    if (PropertyIsNewEnum(pITI, psVar, pMember->m_iMember) == S_OK)
        return _ConvNewEnumProperty(pITI, psVar, pMember);

    // Get the name.
    IfFailGo(pITI->GetDocumentation(psVar->memid, &m_szMember, 0,0,0));

    // Create the get signature.
    IfFailGo(qbComSig.ReSizeNoThrow(CB_MAX_ELEMENT_TYPE * 2));
    pbSig = reinterpret_cast<BYTE*>(qbComSig.Ptr());
    cbTotal = cb = CorSigCompressData((ULONG)IMAGE_CEE_CS_CALLCONV_DEFAULT | IMAGE_CEE_CS_CALLCONV_HASTHIS, pbSig);
    // Getter takes zero parameters.
    cb = CorSigCompressData(0, &(pbSig[cb]));
    cbTotal += cb;
    // Getter returns the property type.
    IfFailGo(_ConvSignature(pITI, &psVar->elemdescVar.tdesc, SIG_ELEM, qbComSig, cbTotal, &cbTotal, qbNativeBuf, 0, &iNativeOfs, FALSE));
    if (hr == S_CONVERSION_LOSS)
        bConversionLoss = true;

    // Getter properties.
    dwFlags = DEFAULT_PROPERTY_FUNC_FLAGS;
    // If processing an implemented interface, remove the abstract bit.  Methods on classes are not abstract.
    if (m_ImplIface != eImplIfaceNone)
        dwFlags &= ~mdAbstract;

    // Get the previously decorated name.  Add interface name and make unique.
    // m_szInterface should be non-null if processing an implemented interface; should be null otherwise.
    _ASSERTE(m_ImplIface == eImplIfaceNone || m_szInterface != 0);
    IfFailGo(qbName.ReSizeNoThrow(wcslen(pMember->m_pName)+2));
    wcscpy_s(qbName.Ptr(), wcslen(pMember->m_pName)+2, pMember->m_pName); 
    IfFailGo(GenerateUniqueMemberName(qbName, (PCCOR_SIGNATURE)qbComSig.Ptr(), cbTotal, m_szInterface, mdtMethodDef));
    pszName = qbName.Ptr();

    // Create the get Accessor.
    IfFailGo(m_pEmit->DefineMethod(m_tdTypeDef, pszName, dwFlags, (PCCOR_SIGNATURE) qbComSig.Ptr(), cbTotal, 
        0/*RVA*/, DEFAULT_ITF_FUNC_IMPL_FLAGS, &mdFuncGet));

    // Handle dispids for non-implemented interfaces, and for default interface
    if (m_ImplIface != eImplIface)
    {
        // Set the Dispid CA.
        _SetDispIDCA(pITI, pMember->m_iMember, psVar->memid, mdFuncGet, TRUE, NULL, FALSE);
    }
    
    // If processing an implemented interface, set up MethodImpls.
    if (m_ImplIface != eImplIfaceNone)
    {
        // Define a memberref on the implemented interface.
        mdToken mrItfMember;
        IfFailGo(m_pEmit->DefineMemberRef(m_tkInterface, pMember->m_pName, (PCCOR_SIGNATURE) qbComSig.Ptr(),cbTotal, &mrItfMember));

        // Define a method impl.
        IfFailGo(m_pEmit->DefineMethodImpl(m_tdTypeDef, mdFuncGet, mrItfMember));
    }

    // If not a read-only var, create the setter.
    if ((psVar->wVarFlags & VARFLAG_FREADONLY) == 0)
    {
        // Create the setter signature.
        IfFailGo(qbComSig.ReSizeNoThrow(CB_MAX_ELEMENT_TYPE * 3));
        pbSig = reinterpret_cast<BYTE*>(qbComSig.Ptr());
        cbTotal = cb = CorSigCompressData((ULONG)IMAGE_CEE_CS_CALLCONV_DEFAULT | IMAGE_CEE_CS_CALLCONV_HASTHIS, pbSig);
        // Setter takes one parameter.
        cb = CorSigCompressData(1, &(pbSig[cb]));
        cbTotal += cb;
        // Setter returns nothing.
        cb = CorSigCompressData(ELEMENT_TYPE_VOID, &pbSig[cbTotal]);
        cbTotal += cb;
        // Setter takes the property type.
        IfFailGo(_ConvSignature(pITI, &psVar->elemdescVar.tdesc, SIG_ELEM, qbComSig, cbTotal, &cbTotal, qbNativeBuf, 0, &iNativeOfs, FALSE));
        if (hr == S_CONVERSION_LOSS)
            bConversionLoss = true;

        // Setter properties.
        dwFlags = DEFAULT_PROPERTY_FUNC_FLAGS;
        // If processing an implemented interface, remove the abstract bit.  Methods on classes are not abstract.
        if (m_ImplIface != eImplIfaceNone)
            dwFlags &= ~mdAbstract;

        // Get the previously decorated name.  Add interface name and make unique.
        // m_szInterface should be non-null if processing an implemented interface; should be null otherwise.
        _ASSERTE(m_ImplIface == eImplIfaceNone || m_szInterface != 0);
        IfFailGo(qbName.ReSizeNoThrow(wcslen(pMember->m_pName2)+2));
        wcscpy_s(qbName.Ptr(), wcslen(pMember->m_pName2)+2, pMember->m_pName2); 
        IfFailGo(GenerateUniqueMemberName(qbName, (PCCOR_SIGNATURE)qbComSig.Ptr(), cbTotal, m_szInterface, mdtMethodDef));
        pszName = qbName.Ptr();

        // Create the setter Accessor.
        IfFailGo(m_pEmit->DefineMethod(m_tdTypeDef, pszName, dwFlags, (PCCOR_SIGNATURE) qbComSig.Ptr(),cbTotal, 
            0/*RVA*/, DEFAULT_ITF_FUNC_IMPL_FLAGS, &mdFuncSet));

        // Handle dispids for non-implemented interfaces, and for default interface
        if (m_ImplIface != eImplIface)
        {
        // Set the Dispid CA.
            _SetDispIDCA(pITI, pMember->m_iMember, psVar->memid, mdFuncSet, TRUE, NULL, FALSE);
    }
        
        // If processing an implemented interface, set up MethodImpls.
        if (m_ImplIface != eImplIfaceNone)
        {
            // Define a memberref on the implemented interface.
            mdToken mrItfMember;
            IfFailGo(m_pEmit->DefineMemberRef(m_tkInterface, pMember->m_pName2, (PCCOR_SIGNATURE) qbComSig.Ptr(),cbTotal, &mrItfMember));

            // Define a method impl.
            IfFailGo(m_pEmit->DefineMethodImpl(m_tdTypeDef, mdFuncSet, mrItfMember));
        }
    }
    else
    {   // read-only, setter method is nil.
        mdFuncSet = mdMethodDefNil;
    }

    // Create the property signature: 'type', or <fieldcallconv><type>
    cbTotal = cb = CorSigCompressData((ULONG)IMAGE_CEE_CS_CALLCONV_PROPERTY, pbSig);
    cb = CorSigCompressData(0, &(pbSig[cb]));
    cbTotal += cb;
    // Property is just the property type.
    IfFailGo(_ConvSignature(pITI, &psVar->elemdescVar.tdesc, SIG_ELEM, qbComSig, cbTotal, &cbTotal, qbNativeBuf, 0, &iNativeOfs, FALSE));
    if (hr == S_CONVERSION_LOSS)
        bConversionLoss = true;

    // Get the property name.  Add interface name and make unique, if needed.
    // m_szInterface should be non-null if processing an implemented interface; should be null otherwise.
    _ASSERTE(m_ImplIface == eImplIfaceNone || m_szInterface != 0);
    IfFailGo(qbName.ReSizeNoThrow(wcslen(m_szMember)+2));
    wcscpy_s(qbName.Ptr(), wcslen(m_szMember)+2, m_szMember); 
    IfFailGo(GenerateUniqueMemberName(qbName, (PCCOR_SIGNATURE)qbComSig.Ptr(), cbTotal, m_szInterface, mdtProperty));
    pszName = qbName.Ptr();

    // Set up the Property on the two methods.
    IfFailGo(m_pEmit->DefineProperty(m_tdTypeDef, pszName, 0/*dwFlags*/, (PCCOR_SIGNATURE) qbComSig.Ptr(),cbTotal, ELEMENT_TYPE_VOID, NULL/*default*/, -1,
        mdFuncSet, mdFuncGet, NULL, &pdProperty));

    // Handle dispids for non-implemented interfaces, and for default interface
    if (m_ImplIface != eImplIface)
    {
        // Set the Dispid CA on the property.
        long lDispSet = 1;
        _SetDispIDCA(pITI, pMember->m_iMember, psVar->memid, pdProperty, TRUE, &lDispSet, FALSE);

    // If this property is default property, add a custom attribute to the class.
        if (lDispSet == DISPID_VALUE)
        IfFailGo(_AddDefaultMemberCa(m_tdTypeDef, m_szMember));
    }

    // Add the alias information if the type is an alias.
    IfFailGo(_HandleAliasInfo(pITI, &psVar->elemdescVar.tdesc, pdProperty));

    if (bConversionLoss)
    {
        hr = S_CONVERSION_LOSS;
        ReportEvent(NOTIF_CONVERTWARNING, TLBX_I_UNCONVERTABLE_ARGS, m_szName, m_szMember);
    }

ErrExit:
    if (m_szMember)
        ::SysFreeString(m_szMember), m_szMember=0;
    return (hr);
} // HRESULT CImportTlb::_ConvProperty()

//*****************************************************************************
// Convert the NewEnum dispatch property into the GetEnumerator method.
//*****************************************************************************
HRESULT CImportTlb::_ConvNewEnumProperty(
    ITypeInfo   *pITI,                  // Containing TypeInfo.
    VARDESC     *psVar,                 // VARDESC for the property.
    MemberInfo  *pMember)
{
    HRESULT     hr;                     // A result.
    mdMethodDef mdGetEnum;              // The GetEnumerator method.
    CQuickBytes qbComSig;               // com signature buffer
    ULONG       cb;                     // Size of an element.
    ULONG       cbTotal = 0;            // Total size of signature.
    BYTE        *pbSig;                 // Pointer to signature buffer.
    BOOL        bConversionLoss=false;  // If true, some attributes were lost on conversion.
    CQuickArray<BYTE> qbNativeBuf;      // Native type buffer.
    ULONG       iNativeOfs=0;           // Current offset in native type buffer.

    // Get the name.
    IfFailGo(pITI->GetDocumentation(psVar->memid, &m_szMember, 0,0,0));

    // Create the GetEnumerator signature.
    IfFailGo(qbComSig.ReSizeNoThrow(CB_MAX_ELEMENT_TYPE * 2));
    pbSig = reinterpret_cast<BYTE*>(qbComSig.Ptr());
    cbTotal = cb = CorSigCompressData((ULONG)IMAGE_CEE_CS_CALLCONV_DEFAULT | IMAGE_CEE_CS_CALLCONV_HASTHIS, pbSig);

    // GetEnumerator takes zero parameters.
    cb = CorSigCompressData(0, &(pbSig[cb]));
    cbTotal += cb;

    // Getter returns the property type.
    IfFailGo(_ConvSignature(pITI, &psVar->elemdescVar.tdesc, SIG_ELEM, qbComSig, cbTotal, &cbTotal, qbNativeBuf, 0, &iNativeOfs, TRUE));
    if (hr == S_CONVERSION_LOSS)
        bConversionLoss = true;

    // Create the GetEnumerator method.
    IfFailGo(m_pEmit->DefineMethod(m_tdTypeDef, GET_ENUMERATOR_MEMBER_NAME, DEFAULT_INTERFACE_FUNC_FLAGS, (PCCOR_SIGNATURE) qbComSig.Ptr(), cbTotal, 
        0/*RVA*/, DEFAULT_ITF_FUNC_IMPL_FLAGS, &mdGetEnum));

    // Set the Dispid CA.
    _SetDispIDCA(pITI, pMember->m_iMember, psVar->memid, mdGetEnum, TRUE, NULL, FALSE);

    // Add the alias information if the type is an alias.
    IfFailGo(_HandleAliasInfo(pITI, &psVar->elemdescVar.tdesc, mdGetEnum));

    if (bConversionLoss)
    {
        hr = S_CONVERSION_LOSS;
        ReportEvent(NOTIF_CONVERTWARNING, TLBX_I_UNCONVERTABLE_ARGS, m_szName, m_szMember);
    }

ErrExit:
    if (m_szMember)
        ::SysFreeString(m_szMember), m_szMember=0;
    return (hr);
} // HRESULT CImportTlb::_ConvNewEnumProperty()

//*****************************************************************************
// Given an ITypeLib*, come up with a namespace name.  Use the typelib name
//  unless there is one specified via custom attribute.
//
// NOTE:  This returns the member variable m_wzNamespace if the typelib
//  is the importing typelib.  That must not be freed!
//*****************************************************************************
HRESULT CImportTlb::GetNamespaceOfRefTlb(
    ITypeLib    *pITLB,                 // TypeLib for which to get namespace name.
    BSTR        *pwzNamespace,          // Put the name here.
    CImpTlbDefItfToClassItfMap **ppDefItfToClassItfMap) // Put def itf to class itf map here.
{
    mdAssemblyRef arDummy;
    BSTR          wzAsmName = NULL;
    HRESULT       hr = S_OK;
        
    // If already resolved, just return assembly ref.
    if (!m_LibRefs.Find(pITLB, &arDummy, pwzNamespace, &wzAsmName, NULL, ppDefItfToClassItfMap))
    {
        // Add a reference to the typelib.
        IfFailGo(_AddTlbRef(pITLB, &arDummy, pwzNamespace, &wzAsmName, ppDefItfToClassItfMap));
    }

ErrExit:
    if (wzAsmName)
        ::SysFreeString(wzAsmName);

    return hr;
} // HRESULT CImportTlb::GetNamespaceOfRefTlb()

//*****************************************************************************
// Given a TYPEDESC, resolve the USERDEFINED to the TYPEKIND.
//*****************************************************************************
HRESULT CImportTlb::_ResolveTypeDescAliasTypeKind(
    ITypeInfo   *pITIAlias,             // The typeinfo containing the typedesc.
    TYPEDESC    *ptdesc,                // The typedesc.
    TYPEKIND    *ptkind)                // Put the aliased typekind.
{
    HRESULT     hr;                     // A result.
    ITypeInfo   *pTIResolved=0;     // The resolved ITypeInfo.
    TYPEATTR    *psResolved=0;      // The resolved TypeInfo's TYPEATTR

    if (ptdesc->vt != VT_USERDEFINED)
    {
        *ptkind = TKIND_MAX;
        return S_FALSE;
    }

    hr = _ResolveTypeDescAlias(pITIAlias, ptdesc, &pTIResolved, &psResolved);
    if (hr == S_OK)
        *ptkind = psResolved->typekind;
    else
        *ptkind = TKIND_MAX;

    if (psResolved)
        pTIResolved->ReleaseTypeAttr(psResolved);
    if (pTIResolved)
        pTIResolved->Release();

    return hr;
} // HRESULT CImportTlb::_ResolveTypeDescAliasTypeKind()

//*****************************************************************************
// Given a TYPEDESC in a TypeInfo, eliminate aliases (get to the aliased
//  type).
//*****************************************************************************
HRESULT CImportTlb::_ResolveTypeDescAlias(
    ITypeInfo   *pITIAlias,             // The typeinfo containing the typedesc.
    const TYPEDESC  *ptdesc,            // The typedesc.
    ITypeInfo   **ppTIResolved,         // Put the aliased ITypeInfo here.
    TYPEATTR    **ppsAttrResolved,      // Put the ITypeInfo's TYPEATTR here.
    GUID        *pGuid)                 // Caller may want aliased object's guid.
{
    HRESULT     hr;                     // A result.
    ITypeInfo   *pITI=0;                // Referenced typeinfo.
    TYPEATTR    *psAttr=0;              // TYPEATTR of referenced typeinfo.

    // If the TDESC isn't a USERDEFINED, it is already resolved.
    if (ptdesc->vt != VT_USERDEFINED)
    {
        *ppTIResolved = pITIAlias;
        pITIAlias->AddRef();
        // Need to addref the [out] psAttr.  Only way to do it:
        IfFailGo(pITIAlias->GetTypeAttr(ppsAttrResolved));
        hr = S_FALSE;
        goto ErrExit;
    }

    // The TYPEDESC is a USERDEFINED.  Get the TypeInfo.
    IfFailGo(pITIAlias->GetRefTypeInfo(ptdesc->hreftype, &pITI));
    IfFailGo(pITI->GetTypeAttr(&psAttr));

    // If the caller needs the aliased object's guid, get it now.
    if (pGuid && *pGuid == GUID_NULL && psAttr->guid != GUID_NULL)
        *pGuid = psAttr->guid;

    // If the userdefined typeinfo is not itself an alias, then it is what the alias aliases.
    //  Also, if the userdefined typeinfo is an alias to a builtin type, then the builtin
    //  type is what the alias aliases.
    if (psAttr->typekind != TKIND_ALIAS || psAttr->tdescAlias.vt != VT_USERDEFINED)
    {
        *ppsAttrResolved = psAttr;
        *ppTIResolved = pITI;
        if (psAttr->typekind == TKIND_ALIAS)
            hr = S_FALSE;
        psAttr = 0;
        pITI = 0;
        goto ErrExit;
    }

    // The userdefined type was itself an alias to a userdefined type.  Alias to what?
    hr = _ResolveTypeDescAlias(pITI, &psAttr->tdescAlias, ppTIResolved, ppsAttrResolved, pGuid);

ErrExit:
    if (psAttr)
        pITI->ReleaseTypeAttr(psAttr);
    if (pITI)
        pITI->Release();
    return hr;
} // HRESULT CImportTlb::_ResolveTypeDescAlias()

//*****************************************************************************
// Create the TypeInfo records (AKA classes, AKA critters).
//*****************************************************************************
HRESULT CImportTlb::GetKnownTypeToken(
    VARTYPE     vt,                     // The type for which the token is desired.
    mdTypeRef   *ptr)                   // Put the token here.
{
    HRESULT     hr = S_OK;                  // A result.

    _ASSERTE(
        (vt >= VT_CY && vt <= VT_DECIMAL) || (vt == VT_SAFEARRAY) || (vt == VT_SLOT_FOR_GUID) ||
        (vt == VT_SLOT_FOR_IENUMERABLE) || (vt == VT_SLOT_FOR_MULTICASTDEL) || (vt == VT_SLOT_FOR_TYPE) ||
        (vt == VT_SLOT_FOR_STRINGBUF));

    // If it has already been added, just return it.
    if (m_tkKnownTypes[vt])
    {
        *ptr = m_tkKnownTypes[vt];
        goto ErrExit;
    }

    // Not yet created, so create the typeref now.
    switch (vt)
    {
    //=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
    // WARNING:  the VT_EMPTY slot is used for System.GUID!!        
    case VT_SLOT_FOR_GUID:
        _ASSERTE(VT_SLOT_FOR_GUID == VT_EMPTY);
        IfFailGo(m_TRMap.DefineTypeRef(         
            m_pEmit,                        // The emit scope.
            m_arSystem,                     // The system assemblyref.
            TLB_CLASSLIB_GUID,              // URL of the TypeDef, wide chars.
            &m_tkKnownTypes[VT_SLOT_FOR_GUID]));    // Put mdTypeRef here
        break;

    // WARNING:  the VT_NULL slot is used for System.Collections.IEnumerable!!        
    case VT_SLOT_FOR_IENUMERABLE:
        _ASSERTE(VT_SLOT_FOR_IENUMERABLE == VT_NULL);
        IfFailGo(m_TRMap.DefineTypeRef(         
            m_pEmit,                        // The emit scope.
            m_arSystem,                     // The system assemblyref.
            TLB_CLASSLIB_IENUMERABLE,       // URL of the TypeDef, wide chars.
            &m_tkKnownTypes[VT_SLOT_FOR_IENUMERABLE]));    // Put mdTypeRef here
        break;

    // WARNING:  the VT_I2 slot is used for System.MulticastDelegate!!        
    case VT_SLOT_FOR_MULTICASTDEL:
        _ASSERTE(VT_SLOT_FOR_MULTICASTDEL == VT_I2);
        IfFailGo(m_TRMap.DefineTypeRef(         
            m_pEmit,                        // The emit scope.
            m_arSystem,                     // The system assemblyref.
            TLB_CLASSLIB_MULTICASTDELEGATE, // URL of the TypeDef, wide chars.
            &m_tkKnownTypes[VT_SLOT_FOR_MULTICASTDEL]));    // Put mdTypeRef here
        break;

    // WARNING:  the VT_I4 slot is used for System.Type!!        
    case VT_SLOT_FOR_TYPE:
        _ASSERTE(VT_SLOT_FOR_TYPE == VT_I4);
        IfFailGo(m_TRMap.DefineTypeRef(         
            m_pEmit,                        // The emit scope.
            m_arSystem,                     // The system assemblyref.
            TLB_CLASSLIB_TYPE,              // URL of the TypeDef, wide chars.
            &m_tkKnownTypes[VT_SLOT_FOR_TYPE]));    // Put mdTypeRef here
        break;

    // WARNING:  the VT_I8 slot is used for System.Text.StringBuilder!!
    case VT_SLOT_FOR_STRINGBUF:
        _ASSERTE(VT_SLOT_FOR_STRINGBUF == VT_I8);
        IfFailGo(m_TRMap.DefineTypeRef(         
            m_pEmit,                        // The emit scope.
            m_arSystem,                     // The system assemblyref.
            TLB_CLASSLIB_STRINGBUFFER,      // URL of the TypeDef, wide chars.
            &m_tkKnownTypes[VT_SLOT_FOR_STRINGBUF]));    // Put mdTypeRef here
        break;

    case VT_CY:
        IfFailGo(m_TRMap.DefineTypeRef(         
            m_pEmit,                        // The emit scope.
            m_arSystem,                     // The system assemblyref.
            TLB_CLASSLIB_DECIMAL,           // URL of the TypeDef, wide chars.
            &m_tkKnownTypes[VT_CY]));       // Put mdTypeRef here
        break;
        
    case VT_DATE:
        IfFailGo(m_TRMap.DefineTypeRef(         
            m_pEmit,                        // The emit scope.
            m_arSystem,                     // The system assemblyref.
            TLB_CLASSLIB_DATE,              // URL of the TypeDef, wide chars.
            &m_tkKnownTypes[VT_DATE]));     // Put mdTypeRef here
        break;

    case VT_DECIMAL:
        IfFailGo(m_TRMap.DefineTypeRef(         
            m_pEmit,                        // The emit scope.
            m_arSystem,                     // The system assemblyref.
            TLB_CLASSLIB_DECIMAL,           // URL of the TypeDef, wide chars.
            &m_tkKnownTypes[VT_DECIMAL]));  // Put mdTypeRef here
        break;

    case VT_SAFEARRAY:
        IfFailGo(m_TRMap.DefineTypeRef(         
            m_pEmit,                        // The emit scope.
            m_arSystem,                     // The system assemblyref.
            TLB_CLASSLIB_ARRAY,             // URL of the TypeDef, wide chars.
            &m_tkKnownTypes[VT_SAFEARRAY]));  // Put mdTypeRef here
        break;
            
    default:
        _ASSERTE(!"Unknown type in GetKnownTypes");
        IfFailGo(E_INVALIDARG);
    }

    _ASSERTE(!IsNilToken(m_tkKnownTypes[vt]));
    *ptr = m_tkKnownTypes[vt];
    
ErrExit:
    return hr;
} // HRESULT CImportTlb::GetKnownTypeToken()


//*****************************************************************************
// Given an ITypeInfo for a coclass, return an ITypeInfo for the default
//  interface.  This is either the explicitly marked default, or the first
//  non-source interface.
//*****************************************************************************
HRESULT CImportTlb::GetDefaultInterface(    // Error, S_OK or S_FALSE.
    ITypeInfo *pCoClassTI,                  // The TypeInfo of the coclass.
    ITypeInfo **pDefaultItfTI)              // The returned default interface.
{
    HRESULT     hr;                 // A result
    HREFTYPE    href;               // HREFTYPE of an implemented interface.
    INT         ImplFlags;          // ImplType flags.
    ITypeInfo   *pITI=NULL;         // ITypeInfo for an interface.
    TYPEATTR    *pCoClassTypeAttr;  // The type attributes of the coclass.
    int         NumInterfaces;      // The number of interfaces on the coclass.
    int         i;                  // A counter.

    // Initialize the default interface to NULL.
    *pDefaultItfTI = NULL;

    // Retrieve the number of interfaces the coclass has
    IfFailGo(pCoClassTI->GetTypeAttr(&pCoClassTypeAttr));
    NumInterfaces = pCoClassTypeAttr->cImplTypes;
    pCoClassTI->ReleaseTypeAttr(pCoClassTypeAttr);

    for (i=0; i < NumInterfaces; i++)
    {
        IfFailGo(pCoClassTI->GetImplTypeFlags(i, &ImplFlags));

        if ((ImplFlags & (IMPLTYPEFLAG_FSOURCE | IMPLTYPEFLAG_FDEFAULT)) == IMPLTYPEFLAG_FDEFAULT)
        {
            // We have found a default interface.
            if (*pDefaultItfTI)
                (*pDefaultItfTI)->Release();

            IfFailGo(pCoClassTI->GetRefTypeOfImplType(i, &href));
            IfFailGo(pCoClassTI->GetRefTypeInfo(href, pDefaultItfTI));
            break;
        } 
        else if (!(ImplFlags & IMPLTYPEFLAG_FSOURCE) && !(*pDefaultItfTI))
        {
            // If this is the first normal interface we encounter then we need to 
            // hang on to it in case we don't find any default interfaces. If that
            // happens then this is the one that will be returned.
            IfFailGo(pCoClassTI->GetRefTypeOfImplType(i, &href));
            IfFailGo(pCoClassTI->GetRefTypeInfo(href, pDefaultItfTI));
        }       
    }

    // Return either S_OK or S_FALSE depending on if we have found a default interface.
    if (*pDefaultItfTI)
        return S_OK;
    else
        return S_FALSE;

ErrExit:
    if (pITI)
        pITI->Release();

    return hr;
} // HRESULT CImportTlb::GetDefaultInterface()

//*****************************************************************************
// Given a TypeInfo, return a TypeDef/TypeRef token.
//*****************************************************************************
HRESULT CImportTlb::_GetTokenForTypeInfo(
    ITypeInfo   *pITI,                  // ITypeInfo for which to get token.
    BOOL        bConvDefItfToClassItf,  // If TRUE, convert the def itf to its class itf.
    mdToken     *pToken,                // Put the token here.
    __out_ecount (chTypeRef) __out_opt LPWSTR pszTypeRef, // Optional, put the name here.
    int         chTypeRef,              // Size of the name buffer in characters.
    int         *pchTypeRef,            // Optional, put size of name here.
    BOOL        bAsmQualifiedName)      // Assembly qualified name or not?
{
    HRESULT     hr;                     // A result.
    ITypeLib    *pITLB=0;               // Containing typelib.
    BSTR        bstrNamespace=0;        // Namespace of the type.
    BSTR        bstrFullName=0;         // Fully qualified name of type.
    BSTR        bstrTempName=0;         // Temp name.
    BSTR        bstrAsmName=0;          // Assembly name.
    LPCWSTR     strTypeName=0;          // The type name.
    mdAssemblyRef ar;                   // The typelib's assembly ref.
    TYPEATTR*   psAttr = 0;             // The TYPEATTR for the type info.
    CImpTlbDefItfToClassItfMap *pDefItfToClassItfMap; // The default interface to class interface map.

    // Get the library.
    IfFailGo(pITI->GetContainingTypeLib(&pITLB, 0));
   
    // Resolve the external reference.
    IfFailGo(_AddTlbRef(pITLB, &ar, &bstrNamespace, &bstrAsmName, &pDefItfToClassItfMap));

    // If are converting default interfaces to class interfaces, then check
    // to see if we need to do the convertion for the current ITypeInfo.
    if (bConvDefItfToClassItf)
    {
        // Retrieve the TYPEATTR.
        IfFailGo(pITI->GetTypeAttr(&psAttr));

        // If we are dealing with an interface, then check to see if there
        // is a class interface we should use.
        if (psAttr->typekind == TKIND_INTERFACE || psAttr->typekind == TKIND_DISPATCH)
        {
            strTypeName = pDefItfToClassItfMap->GetClassItfName(psAttr->guid);
        }
    }

    // If we haven't found a class interface, then use the current interface.
    if (!strTypeName)
    {
    // Get the name of the typeinfo.
    IfFailGo(GetManagedNameForTypeInfo(pITI, bstrNamespace, NULL, &bstrFullName));
        strTypeName = bstrFullName;
    }
    
    // Give name back to caller, if desired.
    if (pszTypeRef)
        wcsncpy_s(pszTypeRef, chTypeRef, strTypeName, chTypeRef-1);
    if (pchTypeRef)
        *pchTypeRef = (int)(wcslen(pszTypeRef) + 1);

    // Define the TypeRef (will return any existing typeref).
    IfFailGo(m_TRMap.DefineTypeRef(m_pEmit, ar, strTypeName, pToken));

    // If the caller desires an assembly qualified name, then provide it.
    if (bAsmQualifiedName)
    {
        int cchAsmQualifiedName = SysStringLen(bstrFullName) + SysStringLen(bstrAsmName) + 2;
        IfNullGo(bstrTempName = ::SysAllocStringLen(0, cchAsmQualifiedName));
        ns::MakeAssemblyQualifiedName(bstrTempName, cchAsmQualifiedName + 1, bstrFullName, SysStringLen(bstrFullName), bstrAsmName, SysStringLen(bstrAsmName));
        SysFreeString(bstrFullName);
        bstrFullName = bstrTempName;
    }

    // Give name back to caller, if desired.
    if (pszTypeRef)
        wcsncpy_s(pszTypeRef, chTypeRef, bstrFullName, chTypeRef-1);
    if (pchTypeRef)
        *pchTypeRef = (int)(wcslen(pszTypeRef) + 1);

ErrExit:
    if (bstrNamespace)
        ::SysFreeString(bstrNamespace);
    if (bstrFullName)
        ::SysFreeString(bstrFullName);
    if (bstrAsmName)
        ::SysFreeString(bstrAsmName);
    if (pITLB)
        pITLB->Release();
    if (psAttr)
        pITI->ReleaseTypeAttr(psAttr);

    return (hr);
} // HRESULT CImportTlb::_GetTokenForTypeInfo()

//*****************************************************************************
// Given a TypeInfo for a source interface, creates a new event interface
// if none exists or returns an existing one.
//*****************************************************************************
HRESULT CImportTlb::_GetTokenForEventItf(ITypeInfo *pSrcItfITI, mdTypeRef *ptr)
{
#ifndef DACCESS_COMPILE
    HRESULT             hr = S_OK;                  // A result.   
    ImpTlbEventInfo*    pEventInfo;                 // The event information.
    BSTR                bstrSrcItfName = NULL;      // The name of the CoClass.
    CQuickArray<WCHAR>  qbEventItfName;             // The name of the event interface.
    CQuickArray<WCHAR>  qbEventProviderName;        // The name of the event provider.
    mdToken             tkAttr;                     // Custom attribute type.
    BSTR                szOldName = NULL;           // The old value m_tdTypeDef.
    mdTypeDef           tdOldTypeDef = NULL;        // The old value m_szName.
    TYPEATTR*           psAttr = 0;                 // The TYPEATTR for the source interface.
    mdTypeRef           trEventItf;                 // A type ref to the event interface.
    ITypeLib*           pTypeTLB;                   // The typelib containing this interface.
    mdAssemblyRef       ar;                         // Dummy AssmRef.
    BSTR                wzNamespace=0;              // Namespace of the event interface assembly.
    BSTR                wzAsmName=0;                // Assembly name of the event interface assembly.
    Assembly*           SrcItfAssembly=0;           // The Source Event Interface assembly.
    CQuickArray<WCHAR>  qbSrcItfName;               // The name of the source interface.
    CImpTlbDefItfToClassItfMap *pDefItfToClassItfMap;   // The default interface to class interface map.
    BOOL                fInheritsIEnum = FALSE;
       
    // Retrieve the namespace of the typelib containing this source interface.
    IfFailGo(pSrcItfITI->GetContainingTypeLib(&pTypeTLB, NULL));

    // Resolve the external reference.
    IfFailGo(_AddTlbRef(pTypeTLB, &ar, &wzNamespace, &wzAsmName, &pDefItfToClassItfMap));

    // Get the assembly + namespace the source interface resides in.  
    //  May return all NULL - indicating the importing assembly.
    m_LibRefs.Find(pTypeTLB, &ar, &wzNamespace, &wzAsmName, &SrcItfAssembly, NULL);
    if (SrcItfAssembly == NULL)
        SrcItfAssembly = m_pAssembly;

    // Retrieve the full name of the source interface.
    if (wzNamespace)
        IfFailGo(GetManagedNameForTypeInfo(pSrcItfITI, (WCHAR*)wzNamespace, NULL, &bstrSrcItfName));
    else
        IfFailGo(GetManagedNameForTypeInfo(pSrcItfITI, m_wzNamespace, NULL, &bstrSrcItfName));          

    // Start by looking up the event information for the source itf type info.
    pEventInfo = m_EventInfoMap.FindEventInfo(bstrSrcItfName);
    if (pEventInfo)
    {
        SysFreeString(bstrSrcItfName);
        *ptr = pEventInfo->trEventItf;
        return S_OK;
    }

    // Store the old values of the ITypeInfo name and of the current type def.
    szOldName = m_szName;
    tdOldTypeDef = m_tdTypeDef;
    m_szName = NULL;

    // Get some information about the TypeInfo.
    IfFailGo(pSrcItfITI->GetDocumentation(MEMBERID_NIL, &m_szName, 0, 0, 0));
    IfFailGo(pSrcItfITI->GetTypeAttr(&psAttr));

    if (ExplicitlyImplementsIEnumerable(pSrcItfITI, psAttr) == S_OK)
        fInheritsIEnum = TRUE;

    // Generate a unique name for the event interface which will be of the form:
        //     <ImportingAssemblyNamespace>.<SrcItfName>_Event<PotentialSuffix>

    // Strip the namespace
    IfFailGo(qbSrcItfName.ReSizeNoThrow(wcslen(bstrSrcItfName) + 2));
    ns::SplitPath((WCHAR*)bstrSrcItfName, NULL, 0, qbSrcItfName.Ptr(), (int)wcslen(bstrSrcItfName) + 1);

    // Add the namespace of the importing typelib and the event suffix
    IfFailGo(qbEventItfName.ReSizeNoThrow(qbSrcItfName.Size() + wcslen(m_wzNamespace) + EVENT_ITF_SUFFIX_LENGTH + 7));
    StringCchPrintf(qbEventItfName.Ptr(), qbEventItfName.Size(), W("%s.%s%s"), m_wzNamespace, qbSrcItfName.Ptr(), EVENT_ITF_SUFFIX);
        IfFailGo(GenerateUniqueTypeName(qbEventItfName));

    // Generate a unique name for the event provider which will be of the form:
    //     <ImportingAssemblyNamespace>.<SrcItfName>_EventProvider<PotentialSuffix>

    // Add the namespace of the imporing typelib and the event suffix
    IfFailGo(qbEventProviderName.ReSizeNoThrow(qbSrcItfName.Size() + wcslen(m_wzNamespace) + EVENT_PROVIDER_SUFFIX_LENGTH + 7));
    StringCchPrintf(qbEventProviderName.Ptr(), qbEventProviderName.Size(), W("%s.%s%s"), m_wzNamespace, qbSrcItfName.Ptr(), EVENT_PROVIDER_SUFFIX);
        IfFailGo(GenerateUniqueTypeName(qbEventProviderName));

    // Add the event provider as a reserved name.
    m_ReservedNames.AddReservedName(qbEventProviderName.Ptr());

    // Create the typedef for the event interface.
    IfFailGo(m_pEmit->DefineTypeDef(qbEventItfName.Ptr(), tdPublic | tdInterface | tdAbstract, mdTypeDefNil, NULL, &m_tdTypeDef));

    // Hide the event interface from the VB object browser (_Event)
    _SetHiddenCA(m_tdTypeDef);

    // Make the interface ComVisible(false).
    {
        DECLARE_CUSTOM_ATTRIBUTE(sizeof(BYTE));
        BUILD_CUSTOM_ATTRIBUTE(BYTE, FALSE);
        IfFailGo(GetAttrType(ATTR_COMVISIBLE, &tkAttr));
        FINISH_CUSTOM_ATTRIBUTE();
        IfFailGo(m_pEmit->DefineCustomAttribute(m_tdTypeDef, tkAttr, PTROF_CUSTOM_ATTRIBUTE(), SIZEOF_CUSTOM_ATTRIBUTE(), 0));
    }

    // Set the ComEventInterface CA on the interface.
    {
        CQuickBytes asmQualifiedSrcItfName;
        if (!ns::MakeAssemblyQualifiedName(asmQualifiedSrcItfName, bstrSrcItfName, wzAsmName))
            IfFailGo(E_OUTOFMEMORY);
        DECLARE_DYNLEN_CUSTOM_ATTRIBUTE(wcslen((WCHAR*)asmQualifiedSrcItfName.Ptr()) + 5 + wcslen(qbEventProviderName.Ptr()) + 5);
        APPEND_WIDE_STRING_TO_CUSTOM_ATTRIBUTE((WCHAR*)asmQualifiedSrcItfName.Ptr());
        APPEND_WIDE_STRING_TO_CUSTOM_ATTRIBUTE(qbEventProviderName.Ptr());
        IfFailGo(GetAttrType(ATTR_COMEVENTINTERFACE, &tkAttr));
        FINISH_DYNLEN_CUSTOM_ATTRIBUTE();
        IfFailGo(m_pEmit->DefineCustomAttribute(m_tdTypeDef, tkAttr, PTROF_CUSTOM_ATTRIBUTE(), SIZEOF_CUSTOM_ATTRIBUTE(), 0));
    }

    // Add the add_XXX and remove_XXX methods to the event interface.
    IfFailGo(_ConvSrcIfaceMembers(pSrcItfITI, psAttr, fInheritsIEnum));

    // Define a typeref for the event interface.
    IfFailGo(m_pEmit->DefineTypeRefByName(TokenFromRid(1, mdtModule), qbEventItfName.Ptr(), &trEventItf));
    
    // Add the event info to the map.
    IfFailGo(m_EventInfoMap.AddEventInfo(bstrSrcItfName, trEventItf, qbEventItfName.Ptr(), qbEventProviderName.Ptr(), SrcItfAssembly));

    // Set the out type ref.
    *ptr = trEventItf;

ErrExit:
    if (bstrSrcItfName)
        ::SysFreeString(bstrSrcItfName);
    if (m_szName)
        ::SysFreeString(m_szName);
    if (psAttr)
        pSrcItfITI->ReleaseTypeAttr(psAttr);
    if (pTypeTLB)
        pTypeTLB->Release();

    // Restore the initial values for the ITypeInfo name and the type def.
    m_szName = szOldName;
    m_tdTypeDef = tdOldTypeDef;

    return (hr);
#else
    DacNotImpl();
    return E_NOTIMPL;
#endif // #ifndef DACCESS_COMPILE
} // HRESULT CImportTlb::_GetTokenForEventItf()

//*****************************************************************************
// Creates an interface with the same name as the class and which implements
// the default interface and the default event interface.
//*****************************************************************************
HRESULT CImportTlb::_CreateClassInterface(ITypeInfo *pCoClassITI, ITypeInfo *pDefItfITI, mdTypeRef trDefItf, mdTypeRef rtDefEvItf, mdToken *ptr)
{
    HRESULT     hr = S_OK;              // A result.
    CQuickArray<mdToken> rImpls;        // Array of implemented interfaces.
    int         ixImpl = -1;            // Index into rImpls for implemented interface.
    mdTypeDef   tdTypeDef;              // The class interface typedef.
    BSTR        bstrFullName = NULL;    // The name of the CoClass.
    TYPEATTR    *psAttrIface=0;         // TYPEATTR for an interface.
    CQuickArray<WCHAR> qbClassName;     // The name of the class.

    IfFailGo(rImpls.ReSizeNoThrow(3));
    memset(rImpls.Ptr(), 0, 3 * sizeof(mdToken));
    if (trDefItf)
        rImpls[++ixImpl] = trDefItf;
    if (rtDefEvItf)
        rImpls[++ixImpl] = rtDefEvItf;

    // Retrieve the TypeAttr for the interface.
    if (pDefItfITI)
        IfFailGo(pDefItfITI->GetTypeAttr(&psAttrIface));

    // Retrieve the name of the CoClass (use the original name if this is an alias).
    IfFailGo(GetManagedNameForTypeInfo(m_pOrigITI, m_wzNamespace, NULL, &bstrFullName));

    // Create the typedef.
    IfFailGo(m_pEmit->DefineTypeDef(bstrFullName, rdwTypeFlags[TKIND_INTERFACE], mdTypeDefNil, 0, &tdTypeDef));

    // Set the IID to the IID of the default interface.
    IfFailGo(_AddGuidCa(tdTypeDef, psAttrIface ? psAttrIface->guid : GUID_NULL));

    // Add the CoClass CA to the interface.
    _AddStringCa(ATTR_COCLASS, tdTypeDef, m_szMngName);

    // Add the implemented interfaces and event interfaces to the TypeDef.
    IfFailGo(m_pEmit->SetTypeDefProps(tdTypeDef, ULONG_MAX/*Classflags*/, 
        ULONG_MAX, (mdToken*)rImpls.Ptr()));

    // Set the out type def.
    *ptr = tdTypeDef;

ErrExit:
    if (bstrFullName)
        ::SysFreeString(bstrFullName);
    if (psAttrIface)
        pDefItfITI->ReleaseTypeAttr(psAttrIface);

    return (hr);
} // HRESULT CImportTlb::_CreateClassInterface()

//*****************************************************************************
// Creates an interface with the same name as the class and which implements
// the default interface and the default event interface.
//*****************************************************************************
HRESULT CImportTlb::GetManagedNameForCoClass(ITypeInfo *pITI, CQuickArray<WCHAR> &qbClassName)
{ 
    HRESULT     hr = S_OK;              // A result.
    BSTR        bstrFullName=0;         // Fully qualified name of type.

    // Retrieve the name of the CoClass.
    IfFailGo(GetManagedNameForTypeInfo(pITI, m_wzNamespace, NULL, &bstrFullName));

    // Resize the class name to accomodate the Class and potential suffix.
    IfFailGo(qbClassName.ReSizeNoThrow(wcslen(bstrFullName) + CLASS_SUFFIX_LENGTH + 6));

    // Set the class name to the CoClass name suffixed with Class.
    StringCchPrintf(qbClassName.Ptr(), qbClassName.Size(), W("%s%s"), bstrFullName, CLASS_SUFFIX);

    // Generate a unique name for the class.
    IfFailGo(GenerateUniqueTypeName(qbClassName));

ErrExit:
    if (bstrFullName)
        ::SysFreeString(bstrFullName);

    return (hr);
} // HRESULT CImportTlb::GetManagedNameForCoClass()

//*****************************************************************************
// Creates an interface with the same name as the class and which implements
// the default interface and the default event interface.
//*****************************************************************************
HRESULT CImportTlb::GenerateUniqueTypeName(CQuickArray<WCHAR> &qbTypeName)
{ 
    HRESULT     hr = S_OK;              // A result.
    WCHAR       *pSuffix=0;             // Location for suffix.
    size_t      cchSuffix;
    WCHAR       *pName=0;               // The name without the namespace.
    int         iSuffix=2;              // Starting value for suffix.
    mdToken     td;                     // For looking up a TypeDef.
    BSTR        szTypeInfoName=0;       // Name of a typeinfo.
    ITypeInfo   *pITI=0;                // A typeinfo.

    // Resize the class name to accomodate the Class and potential suffix.
    IfFailGo(qbTypeName.ReSizeNoThrow(wcslen(qbTypeName.Ptr()) + 6));

    // Set the suffix pointer.
    pSuffix = qbTypeName.Ptr() + wcslen(qbTypeName.Ptr());
    cchSuffix = qbTypeName.Size() - wcslen(qbTypeName.Ptr());

    // Set the name pointer.
    WCHAR* pTemp = ns::FindSep(qbTypeName.Ptr());
    if (pTemp == NULL)
        pName = qbTypeName.Ptr();
    else
        pName = pTemp + 1;

    // Attempt to find a class name that is not in use.
    for (;;)
    {
        // First check to see if the type name is in use in the metadata we 
        // have emitted so far.
        hr = m_pImport->FindTypeDefByName(qbTypeName.Ptr(), mdTypeDefNil, &td);
        if (hr == CLDB_E_RECORD_NOTFOUND)
        {
            // It is not in use in the metadata but we still need to check the
            // typelib because the type might not have been emitted yet.
            USHORT cReq = 4;
            USHORT cFound = cReq;
            BOOL bTypeInTlb = FALSE;
            CQuickArray<ITypeInfo *> qbTI;
            CQuickArray<MEMBERID> qbMemId;
            
            // Retrieve all the instances of the name in the typelib.
            do
            {
                // Double the number of requested names.
                cReq *= 2;

                // Resize the array's to accomodate the resquested names.
                IfFailGo(qbTI.ReSizeNoThrow(cReq));
                IfFailGo(qbMemId.ReSizeNoThrow(cReq));

                // Request the names.
                cFound = cReq;
                IfFailGo(m_pITLB->FindName(pName, 0, qbTI.Ptr(), qbMemId.Ptr(), &cFound));

                // Release all the ITypeInfo's. 
                for (int i = 0; i < cFound; i++)
                    qbTI[i]->Release();
            }
            while (cReq == cFound);

            // Check to see if one of the instances of the name is for a type.
            for (int i = 0; i < cFound; i++)
            {
                if (qbMemId[i] == MEMBERID_NIL)
                {
                    bTypeInTlb = TRUE;
                    break;
                }
            }

            // If the type name exists in the typelib, but we didn't find it as a type,
            //  we still need to do a deeper check, due to how FindName() works.
            if (!bTypeInTlb && cFound > 0)
            {
                int                     cTi;             // Count of TypeInfos.
                int                     i;               // Loop control.

                //@todo: this iterates over every typeinfo every time!  We could cache
                // the names, and skip the types already converted.  However, this should
                // be pretty rare.

                // How many TypeInfos?
                IfFailGo(cTi = m_pITLB->GetTypeInfoCount());

                // Iterate over them.
                for (i=0; i<cTi; ++i)
                {
                    // Get the TypeInfo, and its name.
                    IfFailGo(m_pITLB->GetTypeInfo(i, &pITI));
                    IfFailGo(pITI->GetDocumentation(MEMBERID_NIL, &szTypeInfoName, 0, 0, 0));
                    if (wcscmp(pName, szTypeInfoName) == 0)
                    {
                        bTypeInTlb = TRUE;
                        break;
                    }

                    // Release for next TypeInfo.
                    ::SysFreeString(szTypeInfoName);
                    szTypeInfoName = 0;
                    pITI->Release();
                    pITI = 0;
                }
            }

            // The type name is not in the typelib and not in the metadata then we still
            // need to check to see if is a reserved name.
            if (!bTypeInTlb)
            {
                if (!m_ReservedNames.IsReservedName(qbTypeName.Ptr()))
                {
                    // The name is not a reserved name so we can use it.
                    break;
                }
            }
        }
        IfFailGo(hr);

        // Append the new suffix to the class name.
        StringCchPrintf(pSuffix, cchSuffix, W("_%i"), iSuffix++);
    }

ErrExit:
    if (szTypeInfoName)
        ::SysFreeString(szTypeInfoName);
    if (pITI)
        pITI->Release();
    return (hr);
} // HRESULT CImportTlb::GenerateUniqueTypeName()

//*****************************************************************************
// Generate a unique member name based on the interface member name.
//*****************************************************************************
HRESULT CImportTlb::GenerateUniqueMemberName(// S_OK or error
    CQuickArray<WCHAR> &qbMemberName,       // Original name of member.
    PCCOR_SIGNATURE pSig,                   // Signature of the member.
    ULONG       cSig,                       // Length of the signature.
    LPCWSTR     szPrefix,                   // Possible prefix for decoration.
    mdToken     type)                       // Is it a property? (Not a method?)
{
    HRESULT     hr;                         // A result.
    mdToken     tkMember;                   // Dummy location for token.
    WCHAR       *pSuffix=0;                 // Location for suffix.
    size_t      cchSuffix = 0;
    int         iSuffix=2;                  // Starting value for suffix.

    // Try to find a member name that is not already in use.
    for (;;)
    {   // See if this is (finally) a unique member or property.
        switch (type)
        {
        case mdtProperty:
            hr = FindProperty(m_tdTypeDef, qbMemberName.Ptr(), 0, 0, &tkMember);
            // If name is OK as property, check that there is no method or 
            // property with the name.
            if (hr == CLDB_E_RECORD_NOTFOUND)
                hr = FindMethod(m_tdTypeDef, qbMemberName.Ptr(), 0,0, &tkMember);
            if (hr == CLDB_E_RECORD_NOTFOUND)
                hr = FindEvent(m_tdTypeDef, qbMemberName.Ptr(),  &tkMember);
            break;
        case mdtMethodDef:
            hr = FindMethod(m_tdTypeDef, qbMemberName.Ptr(), pSig, cSig, &tkMember);
            // If name is OK as method, check that there is no property or 
            // event with the name.
            if (hr == CLDB_E_RECORD_NOTFOUND)
                hr = FindProperty(m_tdTypeDef, qbMemberName.Ptr(), 0,0, &tkMember);
            if (hr == CLDB_E_RECORD_NOTFOUND)
                hr = FindEvent(m_tdTypeDef, qbMemberName.Ptr(),  &tkMember);
            break;
        case mdtEvent:
            hr = FindEvent(m_tdTypeDef, qbMemberName.Ptr(),  &tkMember);
            // If name is OK as event, check that there is no property or 
            // method with the name.
            if (hr == CLDB_E_RECORD_NOTFOUND)
                hr = FindProperty(m_tdTypeDef, qbMemberName.Ptr(), 0,0, &tkMember);
            if (hr == CLDB_E_RECORD_NOTFOUND)
                hr = FindMethod(m_tdTypeDef, qbMemberName.Ptr(), 0,0, &tkMember);
            break;
        default:
            // Unexpected type.  Make noise, but let it pass.
            _ASSERTE(!"Unexpected token type in GenerateUniqueMemberName");
            hr = CLDB_E_RECORD_NOTFOUND;
        }

        // If name was not found, it is unique.
        if (hr == CLDB_E_RECORD_NOTFOUND)
        {
            hr = S_OK;
            goto ErrExit;
        }
        // Test for failure.
        IfFailGo(hr);
        
        // Make a test decoration.
        if (szPrefix)
        {
            size_t iLenPrefix, iLenName;
            iLenPrefix = wcslen(szPrefix);  
            iLenName = wcslen(qbMemberName.Ptr());
            IfFailGo(qbMemberName.ReSizeNoThrow(iLenName + iLenPrefix + 2));
            // Shift by prefix length, plus '_'.  Note use of overlap-safe move.
            memmove(&qbMemberName[iLenPrefix+1], &qbMemberName[0], (iLenName+1)*sizeof(WCHAR));
            wcscpy_s(qbMemberName.Ptr(), iLenPrefix + 1, szPrefix);
            qbMemberName[iLenPrefix] = W('_');
            szPrefix = 0;
            // Try again with prefix before trying a suffix.
            continue;
        }
        if (!pSuffix)
        {
            IfFailGo(qbMemberName.ReSizeNoThrow(wcslen(qbMemberName.Ptr()) + 6));
            pSuffix = qbMemberName.Ptr() + wcslen(qbMemberName.Ptr());
            cchSuffix = qbMemberName.Size() - wcslen(qbMemberName.Ptr());
        }
        StringCchPrintf(pSuffix, cchSuffix, W("_%i"), iSuffix++);
    } 

ErrExit:
    return hr;
} // HRESULT CImportTlb::GenerateUniqueMemberName()

//*****************************************************************************
// Convert a TYPEDESC to a COM+ signature.
//
// Conversion rules:
//  integral types are converted as-is.
//  strings to strings, with native type decoration.
//  VT_UNKNOWN, VT_DISPATCH as ref class (ie, Object)
//  VT_PTR -> VT_USERDEFINED interface as Object
//  VT_USERDEFINED record as value type.
//
// With SIG_FUNC:
//  PTR to valuetype depends on other flags:
//   [IN] or [RETVAL] valuetype + NATIVE_TYPE_LPSTRUCT
//   [OUT] or [IN, OUT] byref valuetype 
//  PTR to integral type:
//   [IN] @todo: see atti
//   [OUT] [IN, OUT] byref type
//   [RETVAL] type
//  PTR to object
//   [IN] @todo: see atti
//   [OUT] [IN, OUT] byref object
//   [RETVAL] object
// 
// With SIG_FIELD:
//  PTR to integral type adds ELEMENT_TYPE_PTR.
//
// Conversion proceeds in three steps.
//  1) Parse the COM type info.  Accumulate VT_PTR and VT_BYREF into a count
//     of indirections.  Follow TKIND_ALIAS to determine the ultimate aliased
//     type, and for non-user-defined types, convert that ultimate type.
//     Collect array sizes and udt names.  Determine element type and native
//     type.
//  2) Normalize to COM+ types.  Determine if there is conversion loss.
//  3) Emit the COM+ signature.  Recurse to handle array types.  Add native
//     type info if there is any.
//*****************************************************************************
#ifdef _PREFAST_
#pragma warning(push)
#pragma warning(disable:21000) // Suppress PREFast warning about overly large function
#endif
HRESULT CImportTlb::_ConvSignature(     // S_OK, S_CONVERSION_LOSS, or error.
    ITypeInfo   *pITI,                  // [IN] The typeinfo containing the TYPEDESC.
    const TYPEDESC *pType,              // [IN] The TYPEDESC to convert.
    ULONG       Flags,                  // [IN] Flags describing the TYPEDESC.
    CQuickBytes &qbSigBuf,              // [IN, OUT] A CQuickBytes containing the signature.
    ULONG       cbSig,                  // [IN] Where to start building the signature.
    ULONG       *pcbSig,                // [OUT] Where the signature ends (ix of first byte past; where to start next).
    CQuickArray<BYTE> &qbNativeTypeBuf, // [IN, OUT] A CQuickBytes containing the native type.
    ULONG       cbNativeType,           // [IN] Where to start building the native type.
    ULONG       *pcbNativeType,         // [OUT] Where the native type ends (ix of first byte past; where to start next).
    BOOL        bNewEnumMember,         // [IN] A flag indicating if the member is the NewEnum member.
    int         iByRef)                 // [IN] ByRef count of caller (for recursive calls).
{
    HRESULT     hr=S_OK;                // A result.
    TYPEDESC    tdTemp;                 // Copy of TYPEDESC, for R/W.
    VARTYPE     vt;                     // The typelib signature element.
    int         bByRef=false;           // If true, convert first pointer as "ELEMENT_TYPE_BYREF".
    COR_SIGNATURE et=0;                 // The COM+ signature element.
    mdToken     tk=0;                   // Token from some COM+ signature element.
    ULONG       nt=NATIVE_TYPE_NONE;    // Native type decoration.
    ITypeInfo   *pITIAlias=0;           // Typeinfo of the aliased type.
    TYPEATTR    *psAttrAlias=0;         // TYPEATTR of the aliased typeinfo.
    ITypeInfo   *pITIUD=0;              // TypeInfo of an aliased UserDefined type.
    ITypeLib    *pITLBUD=0;             // TypeLib of an aliased UserDefined type.
    BSTR        bstrNamespace=0;        // Namespace name.
    BSTR        bstrName=0;             // UserDefined name.
    int         bConversionLoss=false;  // If true, the conversion was lossy.
    BYTE        *pbSig;                 // Byte pointer for easy pointer math.
    ULONG       cb;                     // Size of a signature element.
    ULONG       cElems=0;               // Count of elements in an array.
    int         i;                      // Loop control.
    TYPEATTR    *psAttr = 0;            // The TYPEATTR for the user defined type being converted.
    const StdConvertibleItfInfo *pConvertionInfo = 0; // The standard convertible interface information.
    CQuickArray<BYTE> qbNestedNativeType;// A native type buffer used for array sig convertion.
    ULONG       iNestedNativeTypeOfs=0;  // A native type offset.
    ULONG       nested=NATIVE_TYPE_NONE; // A nested native type.

    // VT_ to ELEMENT_TYPE_ translation table.
    struct VtSig 
    {
        CorElementType  et;
        CorNativeType   nt;
        short           flags;
    };

    // The VARIANT_TYPE to sig mapping table. 
    static const VtSig
    _VtInfo[MAX_TLB_VT] =
    {   
        // Relies on {0} initializing the entire sub-structure to 0.
        {ELEMENT_TYPE_MAX,      NATIVE_TYPE_NONE, 0},       //    VT_EMPTY        = 0
        {ELEMENT_TYPE_MAX,      NATIVE_TYPE_NONE, 0},       //    VT_NULL         = 1
        {ELEMENT_TYPE_I2,       NATIVE_TYPE_NONE, 0},       //    VT_I2           = 2
        {ELEMENT_TYPE_I4,       NATIVE_TYPE_NONE, 0},       //    VT_I4           = 3
        {ELEMENT_TYPE_R4,       NATIVE_TYPE_NONE, 0},       //    VT_R4           = 4
        {ELEMENT_TYPE_R8,       NATIVE_TYPE_NONE, 0},       //    VT_R8           = 5
        {ELEMENT_TYPE_VALUETYPE,NATIVE_TYPE_CURRENCY, 0},   //    VT_CY           = 6
        {ELEMENT_TYPE_VALUETYPE,NATIVE_TYPE_NONE, 0},       //    VT_DATE         = 7
        {ELEMENT_TYPE_STRING,   NATIVE_TYPE_BSTR, 0},       //    VT_BSTR         = 8
        {ELEMENT_TYPE_OBJECT,   NATIVE_TYPE_IDISPATCH, 0},  //    VT_DISPATCH     = 9
        {ELEMENT_TYPE_I4,       NATIVE_TYPE_ERROR, 0},      //    VT_ERROR        = 10 scode
        {ELEMENT_TYPE_BOOLEAN,  NATIVE_TYPE_NONE, 0},       //    VT_BOOL         = 11
        {ELEMENT_TYPE_OBJECT,   NATIVE_TYPE_STRUCT, 0},     //    VT_VARIANT      = 12
        {ELEMENT_TYPE_OBJECT,   NATIVE_TYPE_IUNKNOWN, 0},   //    VT_UNKNOWN      = 13
        {ELEMENT_TYPE_VALUETYPE,NATIVE_TYPE_NONE, 0},       //    VT_DECIMAL      = 14
        {ELEMENT_TYPE_MAX,      NATIVE_TYPE_NONE, 0},       //                    = 15
        {ELEMENT_TYPE_I1,       NATIVE_TYPE_NONE, 0},       //    VT_I1           = 16
        {ELEMENT_TYPE_U1,       NATIVE_TYPE_NONE, 0},       //    VT_UI1          = 17
        {ELEMENT_TYPE_U2,       NATIVE_TYPE_NONE, 0},       //    VT_UI2          = 18
        {ELEMENT_TYPE_U4,       NATIVE_TYPE_NONE, 0},       //    VT_UI4          = 19
        {ELEMENT_TYPE_I8,       NATIVE_TYPE_NONE, 0},       //    VT_I8           = 20
        {ELEMENT_TYPE_U8,       NATIVE_TYPE_NONE, 0},       //    VT_UI8          = 21
        
    // it would be nice to convert these as I and U, with NT_I4 and NT_U4, but that doesn't work.
        {ELEMENT_TYPE_I4,       NATIVE_TYPE_NONE, 0},       //    VT_INT          = 22     INT is I4 on win32
        {ELEMENT_TYPE_U4,       NATIVE_TYPE_NONE, 0},       //    VT_UINT         = 23     UINT is UI4 on win32

        {ELEMENT_TYPE_VOID,     NATIVE_TYPE_NONE, 0},       //    VT_VOID         = 24
    
        {ELEMENT_TYPE_I4,       NATIVE_TYPE_ERROR, 0},      //    VT_HRESULT      = 25
        {ELEMENT_TYPE_MAX,      NATIVE_TYPE_NONE, 0},       //    VT_PTR          = 26
        {ELEMENT_TYPE_MAX,      NATIVE_TYPE_NONE, 0},       //    VT_SAFEARRAY    = 27
        {ELEMENT_TYPE_SZARRAY,  NATIVE_TYPE_FIXEDARRAY, 0}, //    VT_CARRAY       = 28
        {ELEMENT_TYPE_MAX,      NATIVE_TYPE_NONE, 0},       //    VT_USERDEFINED  = 29
        {ELEMENT_TYPE_STRING,   NATIVE_TYPE_LPSTR, 0},      //    VT_LPSTR        = 30
        {ELEMENT_TYPE_STRING,   NATIVE_TYPE_LPWSTR, 0},     //    VT_LPWSTR       = 31
    };

    _ASSERTE(pType && pcbSig &&  pcbNativeType);

    //-------------------------------------------------------------------------
    // Parse COM signature

    // Strip off leading VT_PTR and VT_BYREF
    while (pType->vt == VT_PTR)
        pType = pType->lptdesc, ++iByRef;
    if (pType->vt & VT_BYREF)
    {
        tdTemp = *pType;
        tdTemp.vt &= ~VT_BYREF;
        ++iByRef;
        pType = &tdTemp;
    }

    // Determine the element type, and possibly the token and/or native type.
    switch (vt=pType->vt)
    { 
    case VT_PTR:
        _ASSERTE(!"Should not have VT_PTR here");
        break;

    // These are all known types (plus GUID).
    case VT_CY:
    case VT_DATE:
    case VT_DECIMAL:
        IfFailGo(GetKnownTypeToken(vt, &tk));
        et = _VtInfo[vt].et;
        nt = _VtInfo[vt].nt;
        break;

    case VT_SAFEARRAY:
        if (m_bSafeArrayAsSystemArray && !IsSigVarArg(Flags))
        {
            IfFailGo(GetKnownTypeToken(vt, &tk));
            et = ELEMENT_TYPE_CLASS;
            nt = NATIVE_TYPE_SAFEARRAY;
        }
        else
        {
            IfFailGo(GetKnownTypeToken(vt, &tk));
            et = ELEMENT_TYPE_SZARRAY;
            nt = NATIVE_TYPE_SAFEARRAY;
        }
        break;

    case VT_USERDEFINED:
        // Resolve the alias to the ultimate aliased type.
        IfFailGo(_ResolveTypeDescAlias(pITI, pType, &pITIAlias, &psAttrAlias));

        // If the aliased type was built-in, convert that built-in type.
        if (psAttrAlias->typekind == TKIND_ALIAS)
        {   // Recurse to follow the alias chain.
            _ASSERTE(psAttrAlias->tdescAlias.vt != VT_USERDEFINED);
            hr = _ConvSignature(pITIAlias, &psAttrAlias->tdescAlias, Flags, qbSigBuf, cbSig, pcbSig, qbNativeTypeBuf, cbNativeType, pcbNativeType, bNewEnumMember, iByRef);
            goto ErrExit;
        }

        // If the type is a coclass then we need to retrieve the default interface and
        //  substitute it for the coclass.  Look up on the resolved alias, because it is
        //  that class that has a default interface.
        if (psAttrAlias->typekind == TKIND_COCLASS)
        {
            ITypeInfo *pDefaultItf = NULL;
            hr = GetDefaultInterface(pITIAlias, &pDefaultItf);
            if ((hr != S_OK) || !pDefaultItf)
            {
                hr = E_UNEXPECTED;
                goto ErrExit;
            }

            pITIUD = pDefaultItf;
        }
        else
        {   // USERDEFINED class/interface/record/union/enum.  Retrieve the type 
            //  info for the user defined type.  Note: use the TKIND_ALIAS typeinfo 
            //  itself for this conversion (not the aliased type) to preserve 
            //  names, lib locations, etc.
            IfFailGo(pITI->GetRefTypeInfo(pType->hreftype, &pITIUD));
        }

        // pITIUD points to the typeinfo for which we'll create a signature.
        IfFailGo(pITIUD->GetDocumentation(MEMBERID_NIL, &bstrName, 0,0,0));
        IfFailGo(pITIUD->GetContainingTypeLib(&pITLBUD, 0));
        IfFailGo(pITIUD->GetTypeAttr(&psAttr));
        IfFailGo(GetNamespaceNameForTypeLib(pITLBUD, &bstrNamespace));

        // If the "User Defined Type" is GUID in StdOle2, convert to M.R.GUID
        if (SString::_wcsicmp(bstrNamespace, COM_STDOLE2) == 0 && wcscmp(bstrName, COM_GUID) == 0)
        {   // Classlib valuetype GUID.
            et = ELEMENT_TYPE_VALUETYPE;
            IfFailGo(GetKnownTypeToken(VT_SLOT_FOR_GUID, &tk));
        }
        else
        {   // Some user defined class.  Is it a value class, or a VOS class?
            tk = 0;
            switch (psAttrAlias->typekind)
            {
            case TKIND_RECORD:
            case TKIND_ENUM:
            case TKIND_UNION:
                et = ELEMENT_TYPE_VALUETYPE;
                break;
            case TKIND_INTERFACE:
            case TKIND_DISPATCH:
            case TKIND_COCLASS:
                // A pointer to a user defined type of interface/dispatch/coclass 
                //  is a straight COM+ object (the ref is implicit), so eliminate 
                //  one byref count for those.
                // Somehow, there are typelibs written with ([out, retval] IFoo *pOut);
                if (iByRef <= 0)
                {   
                    // convert to an int.
                    bConversionLoss = true;
                    tk = 0;
                    et = ELEMENT_TYPE_I;
                    nt = NATIVE_TYPE_NONE;
                    iByRef = 0;
                    break;
                }
                else
                {
                    --iByRef;

                    // Check for references to Stdole2.IUnknown or Stdole2.IDispatch.
                    if (psAttr->guid == IID_IUnknown)
                    {
                        vt = VT_UNKNOWN;
                        goto IsReallyUnknown;
                    }
                    else if (psAttr->guid == IID_IDispatch)
                    {
                        vt = VT_DISPATCH;
                        goto IsReallyUnknown;
                    }
                    
                    // Check to see if this user defined type is one of the standard ones
                    // we generate custom marshalers for.
                    pConvertionInfo = GetConvertionInfoFromNativeIID(psAttr->guid);
                    if (pConvertionInfo)
                    {
                        // Convert the UTF8 string to unicode.
                        int MngTypeNameStrLen = (int)(strlen(pConvertionInfo->m_strMngTypeName) + 1);
                        WCHAR *strFullyQualifiedMngTypeName = (WCHAR *)_alloca(MngTypeNameStrLen * sizeof(WCHAR));
                        int ret = WszMultiByteToWideChar(CP_UTF8, 0, pConvertionInfo->m_strMngTypeName, MngTypeNameStrLen, strFullyQualifiedMngTypeName, MngTypeNameStrLen);
                        _ASSERTE(ret != 0);
                        if (!ret)
                            IfFailGo(HRESULT_FROM_GetLastError());

                        // Create a TypeRef to the marshaller.
                        IfFailGo(m_TRMap.DefineTypeRef(m_pEmit, m_arSystem, strFullyQualifiedMngTypeName, &tk));

                        // The type is a standard interface that we need to convert.
                        et = ELEMENT_TYPE_CLASS;
                        nt = NATIVE_TYPE_CUSTOMMARSHALER;
                        break;
                    }
                }
                et = ELEMENT_TYPE_CLASS;
                nt = NATIVE_TYPE_INTF;
                break;
            default:
                //case TKIND_MODULE: -- can't pass one of these as a parameter.
                //case TKIND_ALIAS: -- should already be resolved.
                _ASSERTE(!"Unexpected typekind for user defined type");
                et = ELEMENT_TYPE_END;
            } // switch (psAttrAlias->typekind)
        }
        break;

    IsReallyUnknown:
    case VT_UNKNOWN:
    case VT_DISPATCH:
        // If the NewEnum member, retrieve the custom marshaler information for IEnumVARIANT.
        if (bNewEnumMember && (pConvertionInfo=GetConvertionInfoFromNativeIID(IID_IEnumVARIANT)))
        {
            // Convert the UTF8 string to unicode.
            int MngTypeNameStrLen = (int)(strlen(pConvertionInfo->m_strMngTypeName) + 1);
            WCHAR *strFullyQualifiedMngTypeName = (WCHAR *)_alloca(MngTypeNameStrLen * sizeof(WCHAR));
            int ret = WszMultiByteToWideChar(CP_UTF8, 0, pConvertionInfo->m_strMngTypeName, MngTypeNameStrLen, strFullyQualifiedMngTypeName, MngTypeNameStrLen);
            _ASSERTE(ret != 0);
            if (!ret)
                IfFailGo(HRESULT_FROM_GetLastError());

            // Create a TypeRef to the marshaller.
            IfFailGo(m_TRMap.DefineTypeRef(m_pEmit, m_arSystem, strFullyQualifiedMngTypeName, &tk));

            // The type is a standard interface that we need to convert.
            et = ELEMENT_TYPE_CLASS;
            nt = NATIVE_TYPE_CUSTOMMARSHALER;
        }
        else
        {
            et = _VtInfo[vt].et;
            nt = _VtInfo[vt].nt;
        }
        break;

    case VT_CARRAY:
        // Determine the count of elements.
        for (cElems=1, i=0; i<pType->lpadesc->cDims; ++i)
            cElems *= pType->lpadesc->rgbounds[i].cElements;

        // Set the native type based on weither we are dealing with a field or a method sig.
        if (IsSigField(Flags))
        {
            nt = NATIVE_TYPE_FIXEDARRAY;
        }
        else
        {
            nt = NATIVE_TYPE_ARRAY;
        }

        // Set the element type.
        et = _VtInfo[vt].et;
        break;

    case VT_BOOL:
        // Special case for VARIANT_BOOL: If a field of a struct or union, convert
        //  as ET_I2.
        if (IsSigField(Flags))
            vt = VT_I2;
        // Fall through to default case.

    default:
        if (vt > VT_LPWSTR)
        {
            ReportEvent(NOTIF_CONVERTWARNING, TLBX_E_BAD_VT_TYPE, vt, m_szName, m_szMember);
            IfFailGo(PostError(TLBX_E_BAD_VT_TYPE, vt, m_szName, m_szMember));
        }
        _ASSERTE(vt <= VT_LPWSTR && _VtInfo[vt].et != ELEMENT_TYPE_MAX);
#ifdef _PREFAST_
#pragma warning(push)
#pragma warning(disable:26000) // "Disable PREFast/espX warning about buffer overflow"
#endif
        et = _VtInfo[vt].et;
        nt = _VtInfo[vt].nt;
#ifdef _PREFAST_
#pragma warning(pop)
#endif
        break;
    } // switch (vt=pType->vt)

    //-------------------------------------------------------------------------
    // Normalize to COM+ types.

    // At this point the type, flags, and pointer nesting are known.  Is this a legal combination?
    //  If not, what is the appropriate "simplifing assumption"?

    if (et == ELEMENT_TYPE_VOID)
    {
        if (IsSigField(Flags))
        {   // A void as a field.  No byref.
            iByRef = 0;
        }
        else
        {   
            // Param or return type.  "void *" -> ET_I, "void **", "void ***",... -> ET_BYREF ET_I
            if (iByRef > 1)
                iByRef = 1;
            else
            if (iByRef == 1)
                iByRef = 0;
        }
        tk = 0;
        et = ELEMENT_TYPE_I;
        nt = NATIVE_TYPE_NONE;
    }

    if (et == ELEMENT_TYPE_STRING && iByRef == 0 && !IsSigField(Flags) && IsSigOut(Flags))
    {
        // This is an [out] or [in, out] string parameter without indirections.
        if (vt == VT_BSTR)
        {
            // [in, out] System.String does not make much sense. Managed strings are
            // immutable and we do not have BSTR <-> StringBuilder marshaling support.
            // Convert them to IntPtr.
            bConversionLoss = true;
            tk = 0;
            et = ELEMENT_TYPE_I;
            nt = NATIVE_TYPE_NONE;
        }
        else
        {
            _ASSERTE(vt == VT_LPSTR || vt == VT_LPWSTR);

            // [in, out] C-strings and wide strings have a lossless conversion to StringBuilder.
            IfFailGo(GetKnownTypeToken(VT_SLOT_FOR_STRINGBUF, &tk));
            et = ELEMENT_TYPE_CLASS;

            // nt already has the right value
            _ASSERTE(nt == (vt == VT_LPSTR ? NATIVE_TYPE_LPSTR : NATIVE_TYPE_LPWSTR));
        }
    }

    if (iByRef)
    {
        if (et == ELEMENT_TYPE_VALUETYPE && iByRef >= 2)
        {
            bConversionLoss = true;
            tk = 0;
            et = ELEMENT_TYPE_I;
            nt = NATIVE_TYPE_NONE;
            iByRef = 0;
        }
        else
        {
            switch (Flags & SIG_TYPE_MASK)
            {
            case SIG_FIELD:
                // If ptr to valuetype or class type, we can't handle it.
                if (et == ELEMENT_TYPE_END || 
                    et == ELEMENT_TYPE_CLASS || 
                    et == ELEMENT_TYPE_OBJECT || 
                    et == ELEMENT_TYPE_VALUETYPE)
                {
                    bConversionLoss = true;
                    tk = 0;
                    et = ELEMENT_TYPE_I;
                    nt = NATIVE_TYPE_NONE;
                    iByRef = 0;
                }
                break;
            case SIG_FUNC:
                // Pointer to value type?
                if (et == ELEMENT_TYPE_VALUETYPE)
                {   
                    // For [retval], eat one level of indirection; otherwise turn one into BYREF
                    if (IsSigOutRet(Flags))
                    {   // [out, retval], so reduce one level of indirection.
                        --iByRef;
                    }
                    else
                    {   // Favor BYREF over NATIVE_TYPE_LPSTRUCT
                        if (IsSigUseByref(Flags))
                        {
                            bByRef = true;
                            --iByRef;
                        }
                        if (iByRef > 0)
                        {
                            nt = NATIVE_TYPE_LPSTRUCT;
                            --iByRef;
                        }
                    }
                }
                else // Pointer to Object or base type.
                {   
                    if (IsSigRet(Flags))
                    {   // [retval] so consume one indirection.
                        _ASSERTE(iByRef > 0);
                        --iByRef;
                    }
                    if (iByRef > 0 && IsSigUseByref(Flags))
                    {
                        bByRef = true;
                        --iByRef;
                    }
                }
                break;
            case SIG_ELEM:
                // This case comes up when a property type is from a [retval].
                if (IsSigRet(Flags))
                {
                    if (iByRef > 0)
                        --iByRef;
                }
                break;
            }
        }
    } // if (iByRef)

    //-------------------------------------------------------------------------
    // We don't want any ET_PTR, so if there are any byref counts left, bail.
    if (iByRef)
    {
        bConversionLoss = true;
        tk = 0;
        et = ELEMENT_TYPE_I;
        nt = NATIVE_TYPE_NONE;
        iByRef = 0;
        bByRef = false;
    }
    
    //-------------------------------------------------------------------------
    // Build COM+ signature.

    // Type has been analyzed, and possibly modified.  Emit the COM+ signature.
    _ASSERTE(et != ELEMENT_TYPE_MAX);
    _ASSERTE(et != ELEMENT_TYPE_END);

    // If it is a pointer to something, emit that now.
    if (bByRef || iByRef)
    {
        // Size the array to hold the elements.
        IfFailGo(qbSigBuf.ReSizeNoThrow(cbSig + CB_MAX_ELEMENT_TYPE * (iByRef+(bByRef?1:0))));
        pbSig = reinterpret_cast<BYTE*>(qbSigBuf.Ptr());

        // Put in any leading "BYREF"
        if (bByRef)
        {
            pbSig = reinterpret_cast<BYTE*>(qbSigBuf.Ptr());
            cb = CorSigCompressData(ELEMENT_TYPE_BYREF, &pbSig[cbSig]);
            cbSig += cb;
        }

        // Put in the "PTR"s.
        while (iByRef-- > 0)
        {
            cb = CorSigCompressData(ELEMENT_TYPE_PTR, &pbSig[cbSig]);
            cbSig += cb;
        }
    }

    // Emit the type.
    IfFailGo(qbSigBuf.ReSizeNoThrow(cbSig + CB_MAX_ELEMENT_TYPE));
    pbSig = reinterpret_cast<BYTE*>(qbSigBuf.Ptr());
    cb = CorSigCompressData(et, &pbSig[cbSig]);
    cbSig += cb;

    // Add the class type, the array information, etc.
    switch (et)
    {
    case ELEMENT_TYPE_CLASS:
    case ELEMENT_TYPE_VALUETYPE:
        // Size the array to hold the token.
        IfFailGo(qbSigBuf.ReSizeNoThrow(cbSig + CB_MAX_ELEMENT_TYPE));
        pbSig = reinterpret_cast<BYTE*>(qbSigBuf.Ptr());

        // If the token hasn't been resolved yet, do that now.
        if (tk == 0)
        {
            _ASSERTE(pITIUD);
            IfFailGo(_GetTokenForTypeInfo(pITIUD, TRUE, &tk));
        }
        cb = CorSigCompressToken(tk, reinterpret_cast<ULONG*>(&pbSig[cbSig]));
        cbSig += cb;
        break;

    case ELEMENT_TYPE_SZARRAY:
        // map to SZARRAY <subtype>
        IfFailGo(qbSigBuf.ReSizeNoThrow(cbSig + CB_MAX_ELEMENT_TYPE));
        pbSig = reinterpret_cast<BYTE*>(qbSigBuf.Ptr());
        // Recurse on the type.
        IfFailGo(_ConvSignature(pITI, &pType->lpadesc->tdescElem, SIG_ELEM, qbSigBuf, cbSig, &cbSig, qbNestedNativeType, 0, &iNestedNativeTypeOfs, bNewEnumMember));
        if (hr == S_CONVERSION_LOSS)
            bConversionLoss = true;
        break;

    case VT_DISPATCH:       
    case VT_UNKNOWN:
    default:
        _ASSERTE(tk == 0);
        // et, nt assigned above.
        break;
    } // switch (et)

    // Do any native type info.
    if (nt != NATIVE_TYPE_NONE) 
    {
        if (iNestedNativeTypeOfs > 0)
            CorSigUncompressData(reinterpret_cast<PCCOR_SIGNATURE>(qbNestedNativeType.Ptr()), &nested);
        
        if (nt == NATIVE_TYPE_FIXEDARRAY)
        {
            IfFailGo(qbNativeTypeBuf.ReSizeNoThrow(cbNativeType + NATIVE_TYPE_MAX_CB * 2 + DWORD_MAX_CB));
            cbNativeType += CorSigCompressData(nt, &qbNativeTypeBuf[cbNativeType]);
            cbNativeType += CorSigCompressData(cElems, &qbNativeTypeBuf[cbNativeType]);
            if (nested == NATIVE_TYPE_BSTR || nested == NATIVE_TYPE_LPWSTR || nested == NATIVE_TYPE_LPSTR)
            {   // Use the nested type.
                cbNativeType += CorSigCompressData(nested, &qbNativeTypeBuf[cbNativeType]);
            }
            else
            {   // Use a default sub type.
                cbNativeType += CorSigCompressData(NATIVE_TYPE_MAX, &qbNativeTypeBuf[cbNativeType]);
            }            
        }
        else if (nt == NATIVE_TYPE_ARRAY)
        {
            IfFailGo(qbNativeTypeBuf.ReSizeNoThrow(cbNativeType + NATIVE_TYPE_MAX_CB * 2 + DWORD_MAX_CB * 2));
            cbNativeType += CorSigCompressData(nt, &qbNativeTypeBuf[cbNativeType]);
            if (nested == NATIVE_TYPE_BSTR || nested == NATIVE_TYPE_LPWSTR || nested == NATIVE_TYPE_LPSTR)
            {   // Use the nested type.
                cbNativeType += CorSigCompressData(nested, &qbNativeTypeBuf[cbNativeType]);
            }
            else
            {   // Use a default sub type.
                cbNativeType += CorSigCompressData(NATIVE_TYPE_MAX, &qbNativeTypeBuf[cbNativeType]);
            }
            // Use zero for param index.
            cbNativeType += CorSigCompressData(0, &qbNativeTypeBuf[cbNativeType]);
            // Use count from typelib for elem count.
            cbNativeType += CorSigCompressData(cElems, &qbNativeTypeBuf[cbNativeType]);
        }
        else if (nt == NATIVE_TYPE_SAFEARRAY)
        {
            BOOL bPtrArray = FALSE;
            CQuickArray<WCHAR> rTemp;
            CQuickArray<char> rTypeName;
            LPUTF8 strTypeName = "";
            TYPEDESC *pTypeDesc = &pType->lpadesc->tdescElem;
            VARTYPE ArrayElemVT = pTypeDesc->vt;

            if (ArrayElemVT == VT_PTR)
            {
                bPtrArray = TRUE;
                pTypeDesc = pType->lpadesc->tdescElem.lptdesc;
                ArrayElemVT = pTypeDesc->vt;
                if ((ArrayElemVT != VT_USERDEFINED) && (ArrayElemVT != VT_VOID))
                {
                    // We do not support deep marshalling pointers.
                    ArrayElemVT = VT_INT;
                    bConversionLoss = TRUE;
                }
            }

            // If we are dealing with a safe array of user defined types and if we 
            // are importing safe array's as System.Array then add the SafeArrayUserDefSubType.
            if (ArrayElemVT == VT_USERDEFINED)
            {
                // Resolve the alias to the ultimate aliased type.
                IfFailGo(_ResolveTypeDescAlias(pITI, pTypeDesc, &pITIAlias, &psAttrAlias));

                // If the type is a coclass then we need to retrieve the default interface and
                //  substitute it for the coclass.  Look up on the resolved alias, because it is
                //  that class that has a default interface.
                if (psAttrAlias->typekind == TKIND_COCLASS)
                {
                    ITypeInfo *pDefaultItf = NULL;
                    hr = GetDefaultInterface(pITIAlias, &pDefaultItf);
                    if ((hr != S_OK) || !pDefaultItf)
                    {
                        hr = E_UNEXPECTED;
                        goto ErrExit;
                    }

                    pITIUD = pDefaultItf;
                }
                else
                {   // USERDEFINED interface/record/union/enum.  Retrieve the type 
                    //  info for the user defined type.  Note: use the TKIND_ALIAS typeinfo 
                    //  itself for this conversion (not the aliased type) to preserve 
                    //  names, lib locations, etc.
                    IfFailGo(pITI->GetRefTypeInfo(pTypeDesc->hreftype, &pITIUD));
                }

                // pITIUD points to the typeinfo for which we'll create a signature.
                IfFailGo(pITIUD->GetTypeAttr(&psAttr));

                // Get the typeref name for the type.
                for(;;)
                {
                    int cchReq;
                    mdToken tkDummy;
                    IfFailGo(_GetTokenForTypeInfo(pITIUD, TRUE, &tkDummy, rTemp.Ptr(), (int)rTemp.MaxSize(), &cchReq, TRUE));
                    if (cchReq <= (int)rTemp.MaxSize())
                        break;
                    IfFailGo(rTemp.ReSizeNoThrow(cchReq));
                }

                // Convert the type name to UTF8.
                ULONG cbReq = WszWideCharToMultiByte(CP_UTF8, 0, rTemp.Ptr(), -1, 0, 0, 0, 0);
                IfFailGo(rTypeName.ReSizeNoThrow(cbReq + 1));
                WszWideCharToMultiByte(CP_UTF8, 0, rTemp.Ptr(), -1, rTypeName.Ptr(), cbReq, 0, 0);

                // Determine the safe array element VT.
                switch (psAttrAlias->typekind)
                {
                    case TKIND_RECORD:
                    case TKIND_ENUM:
                    case TKIND_UNION:
                        if (bPtrArray)
                        {
                            ArrayElemVT = VT_INT;
                            bConversionLoss = TRUE;
                        }
                        else
                        {
                            ArrayElemVT = psAttrAlias->typekind == TKIND_ENUM ? VT_I4 : VT_RECORD;
                            strTypeName = rTypeName.Ptr();
                        }
                        break;

                    case TKIND_INTERFACE:
                    case TKIND_DISPATCH:
                    case TKIND_COCLASS:
                        if (!bPtrArray)
                        {
                            ArrayElemVT = VT_INT;
                            bConversionLoss = TRUE;
                        }
                        else
                        {
                            if (IsIDispatchDerived(pITIUD, psAttr) == S_FALSE)
                                ArrayElemVT = VT_UNKNOWN;
                            else
                                ArrayElemVT = VT_DISPATCH;
                            strTypeName = rTypeName.Ptr();
                        }
                        break;
                }

                // If we are not converting the SAFEARRAY to a System.Array, then
                // we don't need to encode the name of the user defined type.
                if (!m_bSafeArrayAsSystemArray)
                    strTypeName = "";
            }

            // Make sure the native type buffer is large enough.
            ULONG TypeNameStringLen = (ULONG)strlen(strTypeName);
            IfFailGo(qbNativeTypeBuf.ReSizeNoThrow(cbNativeType + NATIVE_TYPE_MAX_CB * 2 + DWORD_MAX_CB + TypeNameStringLen + STRING_OVERHEAD_MAX_CB));

            // Add the native type to the native type info.
            cbNativeType += CorSigCompressData(nt, &qbNativeTypeBuf[cbNativeType]);

            // Add the VARTYPE of the array.
            cbNativeType += CorSigCompressData(ArrayElemVT, &qbNativeTypeBuf[cbNativeType]);

            // Add the type name to the native type info.
            BYTE *pNativeType = (BYTE*)CPackedLen::PutLength(&qbNativeTypeBuf[cbNativeType], TypeNameStringLen);
            cbNativeType += (ULONG)(pNativeType - &qbNativeTypeBuf[cbNativeType]);
            memcpy(&qbNativeTypeBuf[cbNativeType], strTypeName, TypeNameStringLen);
            cbNativeType += TypeNameStringLen;
        }
        else if (nt == NATIVE_TYPE_CUSTOMMARSHALER)
        {
            // Calculate the length of each string and then the total length of the native type info.
            ULONG MarshalerTypeNameStringLen = (ULONG)strlen(pConvertionInfo->m_strCustomMarshalerTypeName);
            ULONG CookieStringLen = (ULONG)strlen(pConvertionInfo->m_strCookie);
            ULONG TotalNativeTypeLen = MarshalerTypeNameStringLen + CookieStringLen;
            BYTE *pNativeType = 0;

            // Make sure the native type buffer is large enough.
            IfFailGo(qbNativeTypeBuf.ReSizeNoThrow(cbNativeType + NATIVE_TYPE_MAX_CB + TotalNativeTypeLen + STRING_OVERHEAD_MAX_CB * 4));

            // Add the native type to the native type info.
            cbNativeType += CorSigCompressData(nt, &qbNativeTypeBuf[cbNativeType]);

            // Add an empty string for the typelib guid.
            pNativeType = (BYTE*)CPackedLen::PutLength(&qbNativeTypeBuf[cbNativeType], 0);
            cbNativeType += (ULONG)(pNativeType - &qbNativeTypeBuf[cbNativeType]);

            // Add an empty string for the unmanaged type name.
            pNativeType = (BYTE*)CPackedLen::PutLength(&qbNativeTypeBuf[cbNativeType], 0);
            cbNativeType += (ULONG)(pNativeType - &qbNativeTypeBuf[cbNativeType]);

            // Add the name of the custom marshaler to the native type info.
            pNativeType = (BYTE*)CPackedLen::PutLength(&qbNativeTypeBuf[cbNativeType], MarshalerTypeNameStringLen);
            cbNativeType += (ULONG)(pNativeType - &qbNativeTypeBuf[cbNativeType]);
            memcpy(&qbNativeTypeBuf[cbNativeType], pConvertionInfo->m_strCustomMarshalerTypeName, MarshalerTypeNameStringLen);
            cbNativeType += MarshalerTypeNameStringLen;

            // Add the cookie to the native type info.
            pNativeType = (BYTE*)CPackedLen::PutLength(&qbNativeTypeBuf[cbNativeType], CookieStringLen);
            cbNativeType += (ULONG)(pNativeType - &qbNativeTypeBuf[cbNativeType]);
            memcpy(&qbNativeTypeBuf[cbNativeType], pConvertionInfo->m_strCookie, CookieStringLen);
            cbNativeType += CookieStringLen;
        }
        else
        {
            IfFailGo(qbNativeTypeBuf.ReSizeNoThrow(cbNativeType + NATIVE_TYPE_MAX_CB + 1));
            cbNativeType += CorSigCompressData(nt, &qbNativeTypeBuf[cbNativeType]);
        }
    }

    // Return the size of the native type to the caller.
    *pcbNativeType = cbNativeType;

    // Return size to caller.
    *pcbSig = cbSig;

    // If there was a conversion loss, change the return code.
    if (bConversionLoss)
        hr = S_CONVERSION_LOSS;

ErrExit:
    if (bstrNamespace)
        ::SysFreeString(bstrNamespace);
    if (bstrName)
        ::SysFreeString(bstrName);
    if(psAttrAlias)
        pITIAlias->ReleaseTypeAttr(psAttrAlias);
    if (pITIAlias)
        pITIAlias->Release();
    if (psAttr)
        pITIUD->ReleaseTypeAttr(psAttr);
    if (pITIUD)
        pITIUD->Release();
    if (pITLBUD)
        pITLBUD->Release();

    return hr;
} // HRESULT CImportTlb::_ConvSignature()
#ifdef _PREFAST_
#pragma warning(pop)
#endif

//*****************************************************************************
// Build a sorted list of functions to convert.  (Sort by vtable offset.)
//*****************************************************************************
HRESULT CImportTlb::BuildMemberList(
    ITypeInfo   *pITI,                  // TypeInfo with functions.
    int         iStart,                 // First function to take.
    int         iEnd,                   // Last function to take.
    BOOL        bInheritsIEnum)         // Inherits from IEnumerable.
{
    HRESULT     hr;                     // A result.
    int         bNeedSort = false;      // If true, need to sort the array.
    int         ix = 0;                 // Loop counter.
    int         oVftPrev = -1;          // To see if oVft is increasing.
    TYPEATTR    *psAttr = 0;            // TypeAttr for pITI.
    FUNCDESC    *psFunc;                // A FUNCDESC.
    LPWSTR      pszName;                // Working pointer for name.
    BSTR        bstrName=0;             // Name from typelib.
    ITypeInfo2  *pITI2=0;               // To get custom attributes.
    VARIANT     vt;                     // Variant type.
    BOOL        bFunctionToGetter;      // Did a given getter come from a managed function?
    
    ::VariantInit(&vt);
    
    IfFailGo(pITI->GetTypeAttr(&psAttr));
    pITI->QueryInterface(IID_ITypeInfo2, reinterpret_cast<void**>(&pITI2));
    
    // Get the vars.
    IfFailGo(m_MemberList.ReSizeNoThrow(psAttr->cVars + iEnd - iStart));
    memset(m_MemberList.Ptr(), 0, m_MemberList.Size()*sizeof(MemberInfo));
    for (ix=0; ix<psAttr->cVars; ++ix)
    {
        IfFailGo(pITI->GetVarDesc(ix, &(m_MemberList[ix].m_psVar)));
        m_MemberList[ix].m_iMember = ix;
    }
    m_cMemberProps = psAttr->cVars;
                    
    // Get the funcs.
    for (; iStart<iEnd; ++iStart, ++ix)
    {
        IfFailGo(TryGetFuncDesc(pITI, iStart, &(m_MemberList[ix].m_psFunc)));
        psFunc = m_MemberList[ix].m_psFunc;
        if (psFunc->oVft < oVftPrev)
            bNeedSort = true;
        oVftPrev = psFunc->oVft;
        m_MemberList[ix].m_iMember = iStart;
    }

    if (bNeedSort)
    {
        class Sorter : public CQuickSort<MemberInfo> 
        {
            typedef CImportTlb::MemberInfo MemberInfo;
        public:
            Sorter(MemberInfo *p, int n) : CQuickSort<MemberInfo>(p,n) {}
            virtual int Compare(MemberInfo *p1, MemberInfo *p2)
            { 
                if (p1->m_psFunc->oVft < p2->m_psFunc->oVft)
                    return -1;
                if (p1->m_psFunc->oVft == p2->m_psFunc->oVft)
                    return 0;
                return 1;
            }
        };
        Sorter sorter(m_MemberList.Ptr()+m_cMemberProps, (int)m_MemberList.Size()-m_cMemberProps);
        sorter.Sort();
        // Check for duplicates.
        oVftPrev = -1;
        for (ix=m_cMemberProps; ix<(int)m_MemberList.Size(); ++ix)
        {
            if (m_MemberList[ix].m_psFunc->oVft == oVftPrev)
            {
                hr = TLBX_E_BAD_VTABLE;
                break;
            }
            oVftPrev = m_MemberList[ix].m_psFunc->oVft;
        }
    }

    // Build the list of unique names.
    m_pMemberNames = new (nothrow) CWCHARPool;
    IfNullGo(m_pMemberNames);
    
    // Property names.  No possibility of collisions.
    for (ix=0; ix<m_cMemberProps; ++ix)
    {
        IfFailGo(pITI->GetDocumentation(m_MemberList[ix].m_psVar->memid, &bstrName, 0,0,0));
        IfNullGo(pszName = m_pMemberNames->Alloc((ULONG)wcslen(bstrName)+PROP_DECORATION_LEN+1));
        wcscpy_s(pszName, wcslen(bstrName)+PROP_DECORATION_LEN+1, PROP_DECORATION_GET);
        wcscat_s(pszName, wcslen(bstrName)+PROP_DECORATION_LEN+1, bstrName);
        m_MemberList[ix].m_pName = pszName;
        if ((m_MemberList[ix].m_psVar->wVarFlags & VARFLAG_FREADONLY) == 0)
        {
            IfNullGo(pszName = m_pMemberNames->Alloc((ULONG)wcslen(bstrName)+PROP_DECORATION_LEN+1));
            wcscpy_s(pszName, wcslen(bstrName)+PROP_DECORATION_LEN+1, PROP_DECORATION_SET);
            wcscat_s(pszName, wcslen(bstrName)+PROP_DECORATION_LEN+1, bstrName);
            m_MemberList[ix].m_pName2 = pszName;
        }
        ::SysFreeString(bstrName);
        bstrName = 0;
    }
    
    // Function names.  Because of get_/set_ decoration, collisions are possible.
    for (ix=m_cMemberProps; ix<(int)m_MemberList.Size(); ++ix)
    {
        int bNewEnumMember = FALSE;

        // Build a name based on invkind.
        psFunc = m_MemberList[ix].m_psFunc;

        // Unless we are doing the [out, retval] transformation for disp only interfaces,
        // we need to clear the [retval] flag.
        if (!m_bTransformDispRetVals)
        {
            if (psFunc->funckind == FUNC_DISPATCH)
            {   // If [RETVAL] is set, clear it.
                for (int i=0; i<psFunc->cParams; ++i)
                    if ((psFunc->lprgelemdescParam[i].paramdesc.wParamFlags & PARAMFLAG_FRETVAL) != 0)
                        psFunc->lprgelemdescParam[i].paramdesc.wParamFlags &= ~PARAMFLAG_FRETVAL;
            }
        }

        BOOL bExplicitManagedName = FALSE;
        if ( (!bNewEnumMember) && (!bInheritsIEnum) && (FuncIsNewEnum(pITI, psFunc, m_MemberList[ix].m_iMember) == S_OK) )
        {    
            // The member is the new enum member so set its name to GetEnumerator.
            IfNullGo(bstrName = SysAllocString(GET_ENUMERATOR_MEMBER_NAME));
            bNewEnumMember = TRUE;
            
            // To prevent additional methods from implementing the NewEnum method, we mark the interface
            bInheritsIEnum = TRUE;
        }
        else
        {
            // If the managed name custom value is set for this member, then use it.
            if (pITI2)
            {
                hr = pITI2->GetFuncCustData(m_MemberList[ix].m_iMember, GUID_ManagedName, &vt);
                if (hr == S_OK && vt.vt == VT_BSTR)
                {
                    IfNullGo(bstrName = SysAllocString(vt.bstrVal));
                    bExplicitManagedName = TRUE;
                }
                ::VariantClear(&vt);
            }

            if (!bstrName)
                IfFailGo(pITI->GetDocumentation(psFunc->memid, &bstrName, 0,0,0));
        }

        // If this is a property getter, see if it was originally a function.
        bFunctionToGetter = FALSE;
        if (psFunc->invkind == INVOKE_PROPERTYGET && pITI2)
        {
            hr = pITI2->GetFuncCustData(m_MemberList[ix].m_iMember, GUID_Function2Getter, &vt);
            if (hr == S_OK && vt.vt == VT_I4 && vt.lVal == 1)
                bFunctionToGetter = TRUE;
            ::VariantClear(&vt);
        }


        // Check for the propget and propset custom attributes if this not already a property.
        if ( (psFunc->invkind & (INVOKE_PROPERTYGET | INVOKE_PROPERTYPUT | INVOKE_PROPERTYPUTREF)) == 0 )
        {
            INVOKEKIND ikind;
            if (S_OK == _CheckForPropertyCustomAttributes(pITI, m_MemberList[ix].m_iMember, &ikind))
                psFunc->invkind = ikind;
        }        


        // If this is a property accessor, but not the 'new enum member', and not 
        //  originally from a managed function (that was exported as a getter),
        //  decorate the name appropriately. If the managed name was set explicitly by
        //  the Guid_ManagedName attribute, then don't try an decorate it.
        ULONG nChars = 0;
        if (!bExplicitManagedName && (psFunc->invkind & (INVOKE_PROPERTYGET | INVOKE_PROPERTYPUT | INVOKE_PROPERTYPUTREF) && !bNewEnumMember && !bFunctionToGetter))
        {
            nChars = (ULONG)wcslen(bstrName)+PROP_DECORATION_LEN+1;
            IfNullGo(pszName = m_pMemberNames->Alloc(nChars));

            USHORT      msSemantics=0;          // Property's methodsemantics.
            FUNCDESC    *psF;                   // FUNCDESC of Get, Put, or PutRef.
            TYPEDESC    *pProperty;             // TYPEDESC of property type.
            BOOL        bPropRetval;            // Is the property type a [retval]?
            IfFailGo(_GetFunctionPropertyInfo(psFunc, &msSemantics, &psF, &pProperty, &bPropRetval, FALSE, bstrName));
            
            m_MemberList[ix].m_msSemantics = msSemantics;
            switch(msSemantics)
            {
            case msGetter:
                wcscpy_s(pszName, nChars, PROP_DECORATION_GET);
                break;
            case msSetter:
                wcscpy_s(pszName, nChars, PROP_DECORATION_SET);
                break;
            case msOther:
                wcscpy_s(pszName, nChars, PROP_DECORATION_LET);
                break;
            default:
                _ASSERTE(msSemantics == 0);
                *pszName = 0;
                break;
            }
            wcscat_s(pszName, nChars, bstrName);
        }
        else
        {
            nChars = (ULONG)wcslen(bstrName)+1;
            IfNullGo(pszName = m_pMemberNames->Alloc(nChars));
            wcscpy_s(pszName, nChars, bstrName);
        }
        
        // Check for name collision, restore original name if collision occurs.
        for (int index=0; index<ix; index++)
        {
            if ( (m_MemberList[index].m_pName) && (wcscmp(pszName, m_MemberList[index].m_pName) == 0) )
            {
                wcscpy_s(pszName, nChars, bstrName);
                m_MemberList[ix].m_msSemantics = 0;
            }
        }

        // Save the unique name.
        m_MemberList[ix].m_pName = pszName;
        ::SysFreeString(bstrName);
        bstrName = 0;
    }
    
ErrExit:
    if (pITI2)
        pITI2->Release();
    if (psAttr)
        pITI->ReleaseTypeAttr(psAttr);
    if (bstrName)
        ::SysFreeString(bstrName);
    ::VariantClear(&vt);
    return hr;
} // HRESULT CImportTlb::BuildMemberList()

//*****************************************************************************
// Free the list built in BuildMemberList().
//*****************************************************************************
HRESULT CImportTlb::FreeMemberList(
    ITypeInfo   *pITI)                  // TypeInfo with functions.
{
    int         ix;                     // Loop control.
    for (ix=0; ix<m_cMemberProps; ++ix)
        pITI->ReleaseVarDesc(m_MemberList[ix].m_psVar);
    m_cMemberProps = 0;
    for (; ix<(int)m_MemberList.Size(); ++ix)
        pITI->ReleaseFuncDesc(m_MemberList[ix].m_psFunc);
    m_MemberList.Shrink(0);
    if (m_pMemberNames)
    {
        delete m_pMemberNames;
        m_pMemberNames = 0;
    }
    return S_OK;
} // HRESULT CImportTlb::FreeMemberList()

//*****************************************************************************
// Set a GUID CustomAttribute on an object.
//*****************************************************************************
HRESULT CImportTlb::_AddGuidCa(         // S_OK or error.
    mdToken     tkObj,                  // Object to be attributed.
    REFGUID     guid)                   // The GUID.
{
    HRESULT     hr;                     // A result.
    mdMemberRef mr;                     // MemberRef for GUID CA.
    WCHAR       wzGuid[40];             // Buffer for Guid, Unicode.
    CHAR        szGuid[40];             // Buffer for Guid, Ansi.
    DECLARE_CUSTOM_ATTRIBUTE(40);
        
    // If GUID_NULL, don't store it.
    if (guid == GUID_NULL)
        return S_OK;
    
    // Get the GUID as a string.
    // ----+----1----+----2----+----3----+----4
    // {12345678-1234-1234-1234-123456789012}
    GuidToLPWSTR(guid, wzGuid, lengthof(wzGuid));
    _ASSERTE(wzGuid[37] == W('}'));
    wzGuid[37] = W('\0');
    WszWideCharToMultiByte(CP_UTF8, 0, wzGuid+1,-1, szGuid,sizeof(szGuid), 0,0);
    
    // Put it in the Custom Attribute.
    APPEND_STRING_TO_CUSTOM_ATTRIBUTE(szGuid);
    
    // Store the attribute
    IfFailGo(GetAttrType(ATTR_GUID, &mr));
    FINISH_CUSTOM_ATTRIBUTE();
    IfFailGo(m_pEmit->DefineCustomAttribute(tkObj, mr, PTROF_CUSTOM_ATTRIBUTE(), SIZEOF_CUSTOM_ATTRIBUTE(), 0));
    
ErrExit:
    return hr;    
} // HRESULT CImportTlb::_AddGuidCa()
    
//*****************************************************************************
// Add a default member as a custom attribute.
//*****************************************************************************
HRESULT CImportTlb::_AddDefaultMemberCa(// S_OK or error.
    mdToken     tkObj,                  // TypeDef with default member.
    LPCWSTR     wzName)                 // Name of the default member.
{   
    // Only set once per typedef.
    if (tkObj == m_tdHasDefault)
        return S_OK;
    m_tdHasDefault = tkObj;
    
    return _AddStringCa(ATTR_DEFAULTMEMBER, tkObj, wzName);
} // HRESULT CImportTlb::_AddDefaultMemberCa()
    
//*****************************************************************************
// Add a string custom attribute of the given type to the token.
//*****************************************************************************
HRESULT CImportTlb::_AddStringCa(       // S_OK or error.
    int         attr,                   // The type of the CA.
    mdToken     tk,                     // Token to add the CA to.
    LPCWSTR     wzString)               // String to put in the CA.
{
    HRESULT     hr = S_OK;                     // A result.
    mdMemberRef mr;                     // MemberRef for DefaultMember CA.
    BYTE        *pca;                   // Pointer to custom attribute.
    BYTE        *ca;                    // Pointer to custom attribute.
    int         wzLen;                  // Length of wide string.
    int         len;                    // Length of the string.
    CQuickArray<BYTE> buf;

    if (wzString == NULL)
    {
        hr = E_INVALIDARG;
        goto ErrExit;
    }
        
    // Prolog, up to 4 bytes length, string, epilog
    wzLen = (int)wcslen(wzString);
    len = WszWideCharToMultiByte(CP_UTF8,0, wzString, wzLen, 0,0, 0,0);
    IfFailGo(buf.ReSizeNoThrow(2 + 4 + len + 2));
    ca = pca = buf.Ptr();
    
    // Add prolog.
    *reinterpret_cast<UNALIGNED USHORT*>(pca) = 1;
    pca += sizeof(USHORT);
    
    // Add length.
    pca = reinterpret_cast<BYTE*>(CPackedLen::PutLength(pca, len));
    
    // Add string.
    WszWideCharToMultiByte(CP_UTF8,0, wzString, wzLen, reinterpret_cast<char*>(pca), len, 0, 0);
    pca += len;
    
    // Add epilog.
    *reinterpret_cast<UNALIGNED USHORT*>(pca) = 0;
    pca += sizeof(USHORT);
    
    // Store the attribute
    IfFailGo(GetAttrType(attr, &mr));
    IfFailGo(m_pEmit->DefineCustomAttribute(tk, mr, ca, (ULONG)(pca-ca), 0));
    
ErrExit:
    return hr;    
} // HRESULT CImportTlb::_AddStringCa()

//*****************************************************************************
// Add a referenced typelib to the list of referenced typelibs.  Check if
//  it is "this" typelib first.
//*****************************************************************************
HRESULT CImportTlb::_AddTlbRef(         // S_OK or error.
    ITypeLib        *pITLB,             // The referenced typelib.
    mdAssemblyRef   *par,               // The AssemblyRef in this module.
    BSTR            *pwzNamespace,      // The namespace contained in the resolved assembly.
    BSTR            *pwzAsmName,        // The name of the resolved assembly.
    CImpTlbDefItfToClassItfMap **ppDefItfToClassItfMap) // The default interface to class interface map.
{
    HRESULT          hr = S_OK;                       // A result.
    IUnknown         *pIUnk=0;                        // IUnknown for external assembly.
    mdAssemblyRef    ar=0;                            // Assembly ref in the module containing the typeref.
    ITypeLib2        *pITLB2=0;                       // To get custom attributes.
    VARIANT          vt;                              // Variant type.
    Assembly*        ResolvedAssembly=0;              // The resolved assembly.
    CImpTlbDefItfToClassItfMap *pDefItfToClassItfMap; // Temp def itf to class itf map.

    // Validate the arguments.
    _ASSERTE(pITLB && par && pwzNamespace && pwzAsmName);
    
    // Initialize the out parameters to NULL.
    *par = mdTokenNil;
    *pwzNamespace = NULL;
    *pwzAsmName = NULL;
    if (ppDefItfToClassItfMap)
        *ppDefItfToClassItfMap = NULL;
    
    ::VariantInit(&vt);
    
    // If not the importing typelib, add it to the list.
    if (pITLB == m_pITLB)
    {   // Not an external assembly.
        //*par = mdAssemblyRefNil;
        *par = TokenFromRid(1, mdtModule);
        IfNullGo(*pwzNamespace = SysAllocStringLen(m_wzNamespace, SysStringLen(m_wzNamespace)));
        *pwzAsmName = NULL;
        if (ppDefItfToClassItfMap)
            *ppDefItfToClassItfMap = &m_DefItfToClassItfMap;
        return S_OK;
    }

    // If already resolved, just return assembly ref.
    if (m_LibRefs.Find(pITLB, par, pwzNamespace, pwzAsmName, NULL, ppDefItfToClassItfMap))
        return S_OK;

    // See if the typelib was exported, in which case it already has assembly ref information.
    if (pITLB->QueryInterface(IID_ITypeLib2, reinterpret_cast<void**>(&pITLB2)) == S_OK)
    {
        hr = pITLB2->GetCustData(GUID_ExportedFromComPlus, &vt);
        if (vt.vt == VT_BSTR)
        {
            // Use the CA data to get a reference.
            //CQuickArray<BYTE> rBuf;
            //int iLen;
            // The buffer should have been converted with CP_ACP, and should convert back directly.
            //IfFailGo(rBuf.ReSizeNoThrow(iLen=::SysStringLen(vt.bstrVal)));
            //if (iLen=WszWideCharToMultiByte(CP_ACP,0, vt.bstrVal,iLen, (char*)rBuf.Ptr(),iLen, 0,0))
            {
                // Define the assembly ref for the exported assembly.
                //ar = DefineAssemblyRefForExportedAssembly(rBuf.Ptr(),(DWORD)rBuf.Size(), m_pEmit);
                ar = DefineAssemblyRefForExportedAssembly(vt.bstrVal, m_pEmit);

                // Retrieve the namespace from the typelib.
                IfFailGo(GetNamespaceNameForTypeLib(pITLB, pwzNamespace));

                // Set the assembly name.
                IfNullGo(*pwzAsmName = SysAllocStringLen(vt.bstrVal, SysStringLen(vt.bstrVal)));
            }
        }
    }    
    
    // If it wasn't directly converted to a reference, callback to the resolver.
    if (IsNilToken(ar))
    {
        // Get the assembly for that typelib.
        if (FAILED(m_Notify->ResolveRef(pITLB, &pIUnk)))
            IfFailGo(TLBX_I_RESOLVEREFFAILED);

        // If a NULL assembly was returned, then stop converting the type but 
        // continue the import.
        if (pIUnk == NULL)
            IfFailGo(TLBX_E_INVALID_TYPEINFO);

        // Create an assembly ref in local assembly for referenced assembly.
        ar = DefineAssemblyRefForImportedTypeLib(m_pAssembly, m_pModule, m_pEmit, pIUnk, pwzNamespace, pwzAsmName, &ResolvedAssembly);
    }
    
    // Make sure the ref was resolved before adding to cache.
    if (IsNilToken(ar))
        IfFailGo(TLBX_I_RESOLVEREFFAILED);
    
    // Add the TLB to the list of references.
    IfFailGo(m_LibRefs.Add(pITLB, this, ar, *pwzNamespace, *pwzAsmName, ResolvedAssembly, &pDefItfToClassItfMap));

    // Set the output parameters.
    *par = ar;
    if (ppDefItfToClassItfMap)
        *ppDefItfToClassItfMap = pDefItfToClassItfMap;

ErrExit:
    if (FAILED(hr))
    {
        if (*pwzNamespace)
        {
            SysFreeString(*pwzNamespace);
            *pwzNamespace = NULL;
        }
        if (*pwzAsmName)
        {
            SysFreeString(*pwzAsmName);
            *pwzAsmName = NULL;
        }
    }
    if (pIUnk)
        pIUnk->Release();
    if (pITLB2)
        pITLB2->Release();
    VariantClear(&vt);

    return hr;
} // HRESULT CImportTlb::_AddTlbRef()

//*****************************************************************************
// Error reporting helper.
//*****************************************************************************
HRESULT CImportTlb::ReportEvent(        // Returns the original HR.
    int         ev,                     // The event kind.
    int         hrRpt,                  // HR.
    ...)                                // Variable args.
{
    HRESULT     hr;                     // A result.
    va_list     marker;                 // User text.
    BSTR        bstrBuf=0;              // BSTR for bufferrr.
    BSTR        bstrMsg=0;              // BSTR for message.
    const int   iSize = 1024;           // Message size;
    
    // We need a BSTR anyway for the call to ReportEvent, so just allocate a
    //  big one for the buffer.
    IfNullGo(bstrBuf = ::SysAllocStringLen(0, iSize));
    
    // Format the message.
    va_start(marker, hrRpt);
    hr = FormatRuntimeErrorVa(bstrBuf, iSize, hrRpt, marker);
    va_end(marker);
    
    // Display it.
    IfNullGo(bstrMsg = ::SysAllocString(bstrBuf));
    m_Notify->ReportEvent(static_cast<ImporterEventKind>(ev), hrRpt, bstrMsg);
    
ErrExit:    
    // Clean up.
    if (bstrBuf)
        ::SysFreeString(bstrBuf);
    if (bstrMsg)
        ::SysFreeString(bstrMsg);
    return hrRpt;
} // HRESULT CImportTlb::ReportEvent()

//*****************************************************************************
// Helper function to perform the shared functions of creating a TypeRef.
//*****************************************************************************
HRESULT CImpTlbTypeRef::DefineTypeRef(  // S_OK or error.
    IMetaDataEmit *pEmit,               // Emit interface.
    mdAssemblyRef ar,                   // The system assemblyref.
    const LPCWSTR szURL,                // URL of the TypeDef, wide chars.
    mdTypeRef   *ptr)                   // Put mdTypeRef here
{
    HRESULT     hr = S_OK;              // A result.
    LPCWSTR     szLookup;               // The name to look up.
    mdToken     tkNester;               // Token of enclosing class.
    
    // If the name contains a '+', this is a nested type.  The first part becomes
    //  the resolution scope for the part after the '+'.
    szLookup = wcsrchr(szURL, NESTED_SEPARATOR_WCHAR);
    if (szLookup)
    {
        CQuickArray<WCHAR> qbName;
        IfFailGo(qbName.ReSizeNoThrow(szLookup - szURL + 1));
        wcsncpy_s(qbName.Ptr(), (szLookup - szURL + 1), szURL, szLookup - szURL);
        IfFailGo(DefineTypeRef(pEmit, ar, qbName.Ptr(), &tkNester));
        ar = tkNester;
        ++szLookup;
    }
    else
        szLookup = szURL;

    // Look for the item in the map.
    CImpTlbTypeRef::TokenOfTypeRefHashKey sSearch, *pMapped;

    sSearch.tkResolutionScope = ar;
    sSearch.szName = szLookup;
    pMapped = m_Map.Find(&sSearch);

    if (pMapped)
    {
        *ptr = pMapped->tr;
        goto ErrExit;
    }

    // Wasn't found, create a new one and add to the map.
    hr = pEmit->DefineTypeRefByName(ar, szLookup, ptr);
    if (SUCCEEDED(hr))
    {
        sSearch.tr = *ptr;
        pMapped = m_Map.Add(&sSearch);
        IfNullGo(pMapped);
    }

ErrExit:
    return (hr);
} // HRESULT CImpTlbTypeRef::DefineTypeRef()

//*****************************************************************************
// Free the held typelibs in the list of imported typelibs.
//*****************************************************************************
CImpTlbLibRef::~CImpTlbLibRef()
{
    for (ULONG i = 0; i < Size(); i++)
    {
        SysFreeString(operator[](i).szNameSpace);
        delete operator[](i).pDefItfToClassItfMap;
    }
} // CImpTlbLibRef::~CImpTlbLibRef()

//*****************************************************************************
// Add a new typelib reference to the list.
//*****************************************************************************
HRESULT CImpTlbLibRef::Add(
    ITypeLib    *pITLB,
    CImportTlb  *pImporter,
    mdAssemblyRef ar,
    BSTR wzNamespace,
    BSTR wzAsmName,
    Assembly* assm,
    CImpTlbDefItfToClassItfMap **ppMap)
{
    HRESULT     hr = S_OK;              // A result.
    TLIBATTR    *pAttr=0;               // A typelib attribute.
    ULONG       i;                      // Index.
    CTlbRef     *pTlbRef=0;             // A pointer to the TlbRef struct.
    CImpTlbDefItfToClassItfMap *pDefItfToClassItfMap = NULL;  //  ptr to the default interface to class interface map.
    
    // Validate the arguments.
    _ASSERTE(wzNamespace);
    _ASSERTE(wzAsmName);

    IfFailGo(pITLB->GetLibAttr(&pAttr));
    
#if defined(_DEBUG)
    for (i=0; i<Size(); ++i)
    {
        if (operator[](i).guid == pAttr->guid)
        {
            _ASSERTE(!"External TypeLib already referenced");
            goto ErrExit;
        }
    }
#else
    i  = (ULONG)Size();
#endif    

    // Allocate and initialize the default interface to class interface map.
    pDefItfToClassItfMap = new (nothrow) CImpTlbDefItfToClassItfMap();
    IfNullGo(pDefItfToClassItfMap);
    IfFailGo(pDefItfToClassItfMap->Init(pITLB, wzNamespace));

    // Attemp to resize the array.
    IfFailGo(ReSizeNoThrow(i+1));
    pTlbRef = &operator[](i);
    pTlbRef->guid = pAttr->guid;
    pTlbRef->ar = ar;
    IfNullGo(pTlbRef->szNameSpace = SysAllocString(wzNamespace));
    IfNullGo(pTlbRef->szAsmName = SysAllocString(wzAsmName));
    pTlbRef->pDefItfToClassItfMap = pDefItfToClassItfMap;
    pTlbRef->Asm = assm;
    
ErrExit:
    if (pAttr)
        pITLB->ReleaseTLibAttr(pAttr);
    if (FAILED(hr))
    {
        if (pTlbRef && pTlbRef->szNameSpace)
            SysFreeString(pTlbRef->szNameSpace);
        if (pTlbRef && pTlbRef->szAsmName)
            SysFreeString(pTlbRef->szAsmName);
        delete pDefItfToClassItfMap;
    }
    else
    {
        *ppMap = pDefItfToClassItfMap;
    }

    return hr;
} // void CImpTlbLibRef::Add()

//*****************************************************************************
// Find an existing typelib reference.
//*****************************************************************************
int CImpTlbLibRef::Find(
    ITypeLib    *pITLB,
    mdAssemblyRef *par,
    BSTR *pwzNamespace,
    BSTR *pwzAsmName,
    Assembly** assm,
    CImpTlbDefItfToClassItfMap **ppDefItfToClassItfMap)
{
    HRESULT     hr;                     // A result.
    TLIBATTR    *pAttr=0;               // A typelib attribute.
    int         rslt = FALSE;           // Return result.
    ULONG       i;                      // Loop control.
    
    _ASSERTE(pwzNamespace);
    _ASSERTE(pwzAsmName);

    // Initalize the out parameters to NULL.
    *pwzNamespace = NULL;
    *pwzAsmName = NULL;

    if (assm) 
        *assm = NULL;

    IfFailGo(pITLB->GetLibAttr(&pAttr));
    
    for (i=0; i<Size(); ++i)
    {
        if (operator[](i).guid == pAttr->guid)
        {
            *par = operator[](i).ar;
            IfNullGo(*pwzNamespace = SysAllocString(operator[](i).szNameSpace));
            IfNullGo(*pwzAsmName = SysAllocString(operator[](i).szAsmName));
            if (ppDefItfToClassItfMap)
                *ppDefItfToClassItfMap = operator[](i).pDefItfToClassItfMap;
            if (assm)
                *assm = operator[](i).Asm;
            rslt = TRUE;
            goto ErrExit;
        }
    }
    
ErrExit:
    if (FAILED(hr))
    {
        if (*pwzNamespace)
            SysFreeString(*pwzNamespace);
        if (*pwzAsmName)
            SysFreeString(*pwzAsmName);
    }
    if (pAttr)
        pITLB->ReleaseTLibAttr(pAttr);
    return rslt;
} // void CImpTlbLibRef::Find()

//*****************************************************************************
// unpack variant to an ELEMENT_TYPE_* plus a blob value
// If VT_BOOL, it is a two-byte value.
//*****************************************************************************
HRESULT _UnpackVariantToConstantBlob(VARIANT *pvar, BYTE *pcvType, void **pvValue, __int64 *pd)
{
    HRESULT     hr = NOERROR;

    switch (pvar->vt)
    {
    case VT_BOOL:
        *pcvType = ELEMENT_TYPE_BOOLEAN;
        *((VARIANT_BOOL **)pvValue) = &(pvar->boolVal);
        break;
    case VT_I1:
        *pcvType = ELEMENT_TYPE_I1;
        *((CHAR **)pvValue) = &(pvar->cVal);
        break;
    case VT_UI1:
        *pcvType = ELEMENT_TYPE_U1;
        *((BYTE **)pvValue) = &(pvar->bVal);
        break;
    case VT_I2:
        *pcvType = ELEMENT_TYPE_I2;
        *((SHORT **)pvValue) = &(pvar->iVal);
        break;
    case VT_UI2:
        *pcvType = ELEMENT_TYPE_U2;
        *((USHORT **)pvValue) = &(pvar->uiVal);
        break;
    case VT_I4:
    case VT_INT:
        *pcvType = ELEMENT_TYPE_I4;
        *((LONG **)pvValue) = &(pvar->lVal);
        break;
    case VT_UI4:
    case VT_UINT:
        *pcvType = ELEMENT_TYPE_U4;
        *((ULONG **)pvValue) = &(pvar->ulVal);
        break;
    case VT_R4:
        *pcvType = ELEMENT_TYPE_R4;
        *((float **)pvValue) = &(pvar->fltVal);
        break;      
    case VT_I8:
        *pcvType = ELEMENT_TYPE_I8;
        *((LONGLONG **)pvValue) = &(pvar->cyVal.int64);
        break;
    case VT_R8:
        *pcvType = ELEMENT_TYPE_R8;
        *((double **)pvValue) = &(pvar->dblVal);
        break;
    case VT_BSTR:
        *pcvType = ELEMENT_TYPE_STRING;
        *((BSTR *)pvValue) = pvar->bstrVal;     
        break;

    case VT_DATE:
        *pcvType = ELEMENT_TYPE_I8;
        *pd = _DoubleDateToTicks(pvar->date);
        *((LONGLONG **)pvValue) = pd;
        break;
    case VT_UNKNOWN:
    case VT_DISPATCH:
        *pcvType = ELEMENT_TYPE_CLASS;
        _ASSERTE(pvar->punkVal == NULL);
        *((IUnknown ***)pvValue) = &(pvar->punkVal);        
        break;
    default:
        _ASSERTE(!"Not a valid type to specify default value!");
        IfFailGo( META_E_BAD_INPUT_PARAMETER );
        break;
    }
ErrExit:
    return hr;
} // HRESULT _UnpackVariantToConstantBlob()

//*****************************************************************************
// Stolen from classlib.
//*****************************************************************************
INT64 _DoubleDateToTicks(const double d)
{
    const INT64 MillisPerSecond = 1000;
    const INT64 MillisPerDay = MillisPerSecond * 60 * 60 * 24;
    const INT64 TicksPerMillisecond = 10000;
    const INT64 TicksPerSecond = TicksPerMillisecond * 1000;
    const INT64 TicksPerMinute = TicksPerSecond * 60;
    const INT64 TicksPerHour = TicksPerMinute * 60;
    const INT64 TicksPerDay = TicksPerHour * 24;
    const int DaysPer4Years = 365 * 4 + 1;
    const int DaysPer100Years = DaysPer4Years * 25 - 1;
    const int DaysPer400Years = DaysPer100Years * 4 + 1;
    const int DaysTo1899 = DaysPer400Years * 4 + DaysPer100Years * 3 - 367;
    const INT64 DoubleDateOffset = DaysTo1899 * TicksPerDay;
    const int DaysTo10000 = DaysPer400Years * 25 - 366;
    const INT64 MaxMillis = DaysTo10000 * MillisPerDay;

    INT64 millis = (INT64)(d * MillisPerDay + (d >= 0? 0.5: -0.5));
    if (millis < 0) millis -= (millis % MillisPerDay) * 2;
    millis += DoubleDateOffset / TicksPerMillisecond;
    if (millis < 0 || millis >= MaxMillis) {
        return 0;
    }
    return millis * TicksPerMillisecond;
} // INT64 _DoubleDateToTicks()


//*****************************************************************************
// Wrapper for GetFuncDesc to catch errors.
//*****************************************************************************
static HRESULT TryGetFuncDesc(          // S_OK or error.
    ITypeInfo   *pITI,                  // ITypeInfo with function.
    int         i,                      // Function index.
    FUNCDESC    **ppFunc)               // Put FUNCDESC here.
{
    HRESULT     hr;                     // A return code.
    __try
    {
        hr = pITI->GetFuncDesc(i, ppFunc);
    }
    __except(1)
    {
        hr = PostError(TLBX_E_TLB_EXCEPTION, _exception_code());
    }
    
    return hr;
} // static HRESULT TryGetFuncDesc()

//*****************************************************************************
// Implementation of a hashed ResolutionScope+Name to TypeRef map.
//*****************************************************************************
void CImpTlbTypeRef::CTokenOfTypeRefHash::Clear()
{
#if defined(_DEBUG)
    // printf("Name to TypeRef cache: %d buckets, %d used, %d collisions\n", Buckets(), Count(), Collisions());
#endif
    CClosedHash<class TokenOfTypeRefHashKey>::Clear();
} // void CImpTlbTypeRef::CTokenOfTypeRefHash::Clear()

unsigned int CImpTlbTypeRef::CTokenOfTypeRefHash::Hash(const TokenOfTypeRefHashKey *pData)
{
    // Starting value for hash.
    ULONG   hash = 5381;
    
    // Hash in the resolution scope token.
    const BYTE *pbData = reinterpret_cast<const BYTE *>(&pData->tkResolutionScope);
    int iSize = 4;
    while (--iSize >= 0)
    {
        hash = ((hash << 5) + hash) ^ *pbData;
        ++pbData;
    }

    // Hash in the typeref name.
    LPCWSTR szStr = pData->szName;
    int     c;
    while ((c = *szStr) != 0)
    {
        hash = ((hash << 5) + hash) ^ c;
        ++szStr;
    }

    return hash;
} // unsigned int CImpTlbTypeRef::CTokenOfTypeRefHash::Hash()

unsigned int CImpTlbTypeRef::CTokenOfTypeRefHash::Compare(const TokenOfTypeRefHashKey *p1, TokenOfTypeRefHashKey *p2)
{
    // Resolution scopes are fast to compare.
    if (p1->tkResolutionScope < p2->tkResolutionScope)
        return -1;
    if (p1->tkResolutionScope > p2->tkResolutionScope)
        return 1;
    // But if they are the same, compare the names.
    return wcscmp(p1->szName, p2->szName);
} // unsigned int CImpTlbTypeRef::CTokenOfTypeRefHash::Compare()

CImpTlbTypeRef::CTokenOfTypeRefHash::ELEMENTSTATUS CImpTlbTypeRef::CTokenOfTypeRefHash::Status(TokenOfTypeRefHashKey *p)
{
    if (p->tkResolutionScope == static_cast<mdToken>(FREE))
        return (FREE);
    if (p->tkResolutionScope == static_cast<mdToken>(DELETED))
        return (DELETED);
    return (USED);
} // CImpTlbTypeRef::CTokenOfTypeRefHash::ELEMENTSTATUS CImpTlbTypeRef::CTokenOfTypeRefHash::Status()

void CImpTlbTypeRef::CTokenOfTypeRefHash::SetStatus(TokenOfTypeRefHashKey *p, ELEMENTSTATUS s)
{
    p->tkResolutionScope = static_cast<mdToken>(s);
} // void CImpTlbTypeRef::CTokenOfTypeRefHash::SetStatus()

void *CImpTlbTypeRef::CTokenOfTypeRefHash::GetKey(TokenOfTypeRefHashKey *p)
{
    return p;
} // void *CImpTlbTypeRef::CTokenOfTypeRefHash::GetKey()

CImpTlbTypeRef::TokenOfTypeRefHashKey* CImpTlbTypeRef::CTokenOfTypeRefHash::Add(const TokenOfTypeRefHashKey *pData)
{
    LPWSTR pName;
    const void *pvData = pData;
    TokenOfTypeRefHashKey *pNew = Super::Add(const_cast<void*>(pvData));
    if (pNew == 0)
        return 0;
    pNew->szName = pName = m_Names.Alloc((ULONG)wcslen(pData->szName)+1);
    if (pNew->szName == 0)
        return 0;
    wcscpy_s(pName, wcslen(pData->szName)+1, pData->szName);
    pNew->tkResolutionScope = pData->tkResolutionScope;
    pNew->tr = pData->tr;

    return pNew;
} // TokenOfTypeRefHashKey* CImpTlbTypeRef::CTokenOfTypeRefHash::Add()

//*****************************************************************************
// Implementation of a hashed ITypeInfo * source interface to event information
// map.
//*****************************************************************************
HRESULT CImpTlbEventInfoMap::AddEventInfo(LPCWSTR szSrcItfName, mdTypeRef trEventItf, LPCWSTR szEventItfName, LPCWSTR szEventProviderName, Assembly* SrcItfAssembly)
{
    ImpTlbEventInfo sNew;
    sNew.szSrcItfName = szSrcItfName;
    sNew.trEventItf = trEventItf;
    sNew.szEventItfName = szEventItfName;
    sNew.szEventProviderName = szEventProviderName;
    sNew.SrcItfAssembly = SrcItfAssembly;
    return Add(&sNew) != NULL ? S_OK : E_OUTOFMEMORY;
} // BOOL CImpTlbEventInfoMap::AddEventInfo()

ImpTlbEventInfo *CImpTlbEventInfoMap::FindEventInfo(LPCWSTR szSrcItfName)
{
    ImpTlbEventInfo sSearch, *pMapped;
    sSearch.szSrcItfName = szSrcItfName;
    pMapped = Find(&sSearch);
    return pMapped;
} // ImpTlbEventInfo *CImpTlbEventInfoMap::FindEventInfo()

HRESULT CImpTlbEventInfoMap::GetEventInfoList(CQuickArray<ImpTlbEventInfo*> &qbEvInfoList)
{
    HRESULT hr = S_OK;
    int cCurrEvInfo = 0;

    // Resise the event info list.
    IfFailGo(qbEvInfoList.ReSizeNoThrow(Count()));

    // Retrieve the first event info.
    ImpTlbEventInfo *pEvInfo = GetFirst();

    // Add all the event info's to the list.
    while (pEvInfo)
    {
        qbEvInfoList[cCurrEvInfo++] = pEvInfo;
        pEvInfo = GetNext(pEvInfo);
    }

ErrExit:
    return hr;    
} // HRESULT CImpTlbEventInfoMap::GetEventInfoList()

unsigned int CImpTlbEventInfoMap::Hash(const ImpTlbEventInfo *pData)
{
    // Starting value for hash.
    ULONG   hash = 5381;
    
    // Hash in the source interface name.
    LPCWSTR szStr = pData->szSrcItfName;
    int     c;
    while ((c = *szStr) != 0)
    {
        hash = ((hash << 5) + hash) ^ c;
        ++szStr;
    }

    return hash;
} // unsigned int CImpTlbEventInfoMap::Hash()

unsigned int CImpTlbEventInfoMap::Compare(const ImpTlbEventInfo *p1, ImpTlbEventInfo *p2)
{
    // Compare the source interface names.
    return wcscmp(p1->szSrcItfName, p2->szSrcItfName);
} // unsigned int CImpTlbEventInfoMap::Compare()

CImpTlbEventInfoMap::ELEMENTSTATUS CImpTlbEventInfoMap::Status(ImpTlbEventInfo *p)
{
    if (p->szSrcItfName == reinterpret_cast<LPCWSTR>(FREE))
        return (FREE);
    if (p->szSrcItfName == reinterpret_cast<LPCWSTR>(DELETED))
        return (DELETED);
    return (USED);
} // CImpTlbEventInfoMap::ELEMENTSTATUS CImpTlbEventInfoMap::Status()

void CImpTlbEventInfoMap::SetStatus(ImpTlbEventInfo *p, ELEMENTSTATUS s)
{
    p->szSrcItfName = reinterpret_cast<LPCWSTR>(s);
} // void CImpTlbEventInfoMap::SetStatus()

void *CImpTlbEventInfoMap::GetKey(ImpTlbEventInfo *p)
{
    return p;
} // void *CImpTlbEventInfoMap::GetKey()

ImpTlbEventInfo* CImpTlbEventInfoMap::Add(const ImpTlbEventInfo *pData)
{
    // Add the new entry to the map.
    const void *pvData = pData;
    ImpTlbEventInfo *pNew = Super::Add(const_cast<void*>(pvData));
    if (pNew == 0)
        return 0;

    // Copy the source interface name.
    pNew->szSrcItfName = m_Names.Alloc((ULONG)wcslen(pData->szSrcItfName)+1);
    if (pNew->szSrcItfName == 0)
        return 0;
    wcscpy_s((LPWSTR)pNew->szSrcItfName, wcslen(pData->szSrcItfName)+1, pData->szSrcItfName);

    // Copy the event interface type def.
    pNew->trEventItf = pData->trEventItf;

    // Copy the event interface name.
    pNew->szEventItfName = m_Names.Alloc((ULONG)wcslen(pData->szEventItfName)+1);
    if (pNew->szEventItfName == 0)
        return 0;
    wcscpy_s((LPWSTR)pNew->szEventItfName, wcslen(pData->szEventItfName)+1, pData->szEventItfName);

    // Copy the event provider name.
    pNew->szEventProviderName = m_Names.Alloc((ULONG)wcslen(pData->szEventProviderName)+1);
    if (pNew->szEventProviderName == 0)
        return 0;
    wcscpy_s((LPWSTR)pNew->szEventProviderName, wcslen(pData->szEventProviderName)+1, pData->szEventProviderName);

    // Copy the Source Interface Assembly pointer
    pNew->SrcItfAssembly = pData->SrcItfAssembly;
    
    // Return the new entry.
    return pNew;
} // ImpTlbEventInfo* CImpTlbEventInfoMap::Add()

CImpTlbDefItfToClassItfMap::CImpTlbDefItfToClassItfMap() 
: CClosedHash<class ImpTlbClassItfInfo>(101) 
, m_bstrNameSpace(NULL) 
{
}

CImpTlbDefItfToClassItfMap::~CImpTlbDefItfToClassItfMap() 
{ 
    Clear(); 
    if (m_bstrNameSpace)
    {
        ::SysFreeString(m_bstrNameSpace);
        m_bstrNameSpace = NULL;
    }
}

HRESULT CImpTlbDefItfToClassItfMap::Init(ITypeLib *pTlb, BSTR bstrNameSpace)
{
    HRESULT                 hr;                     // A result.
    int                     cTi;                    // Count of TypeInfos.
    int                     i;                      // Loop control.
    TYPEATTR                *psAttr=0;              // TYPEATTR for the ITypeInfo.
    TYPEATTR                *psDefItfAttr=0;        // TYPEATTR for the default interface.
    ITypeInfo               *pITI=0;                // The ITypeInfo.
    ITypeInfo               *pDefItfITI=0;          // The ITypeInfo for the default interface.

    // Save the namespace.
    IfNullGo(m_bstrNameSpace = SysAllocString(bstrNameSpace));

    // How many TypeInfos?
    IfFailGo(cTi = pTlb->GetTypeInfoCount());

    // Iterate over them.
    for (i = 0; i < cTi; ++i)
    {
        // Get the TypeInfo.
        hr = pTlb->GetTypeInfo(i, &pITI);
        if (SUCCEEDED(hr))
        {
            // Retrieve the attributes of the type info.
            IfFailGo(pITI->GetTypeAttr(&psAttr));

            // If we are dealing with a CoClass, then set up the default interface to 
            // class interface mapping.
            if (psAttr->typekind == TKIND_COCLASS)
                IfFailGo(AddCoClassInterfaces(pITI, psAttr));

            // Release for next TypeInfo.
            if (psAttr)
            {
                pITI->ReleaseTypeAttr(psAttr);
                psAttr = 0;
            }
            if (pITI)
            {
                pITI->Release();
                pITI = 0;
            }
        }
    }

ErrExit:
    if (psAttr)
        pITI->ReleaseTypeAttr(psAttr);
    if (pITI)
        pITI->Release();

    return (hr);
}

HRESULT CImpTlbDefItfToClassItfMap::AddCoClassInterfaces(ITypeInfo *pCoClassITI, TYPEATTR *pCoClassTypeAttr)
{
    HRESULT     hr;                 // A result
    HREFTYPE    href;               // HREFTYPE of an implemented interface.
    INT         ImplFlags;          // ImplType flags.
    int         NumInterfaces;      // The number of interfaces on the coclass.
    int         i;                  // A counter.
    ITypeInfo   *pItfITI=0;         // The ITypeInfo for the current interface.
    ITypeInfo   *pBaseItfITI=0;     // The ITypeInfo for the base interface.
    TYPEATTR    *psItfAttr=0;       // TYPEATTR for the interface.
    BSTR        bstrClassItfName=0; // The name of the class interface.

    // Retrieve the name of the CoClass.
    IfFailGo(GetManagedNameForTypeInfo(pCoClassITI, m_bstrNameSpace, NULL, &bstrClassItfName));

    // Retrieve the default interface for the CoClass.
    IfFailGo(CImportTlb::GetDefaultInterface(pCoClassITI, &pItfITI));

    // If there is a default interface, then add it to the map.
    if (hr == S_OK)
    {
        // Retrieve the attributes of the default interface type info.
        IfFailGo(pItfITI->GetTypeAttr(&psItfAttr));

        // If there already is a CoClass that implements this 
        // interface then we do not want to do the mapping.
        ImpTlbClassItfInfo sSearch, *pMapped;
        sSearch.ItfIID = psItfAttr->guid;
        pMapped = Find(&sSearch);
        if (pMapped)
        {
            // There already is a CoClass that implements the interface so 
            // we set the class itf name to NULL to indicate not to do the def 
            // itf to class itf convertion for this interface.
            pMapped->szClassItfName = NULL;
        }
        else
        {
            // Unless the default interface is IUnknown or IDispatch, add the 
            // def itf to class itf entry to the map.       
            if (psItfAttr->guid != IID_IUnknown && psItfAttr->guid != IID_IDispatch)
            {
                ImpTlbClassItfInfo sNew;
                sNew.ItfIID = psItfAttr->guid;
                sNew.szClassItfName = bstrClassItfName;
                IfNullGo(Add(&sNew)); 
            }
        }

        // Release for next interface.
        pItfITI->ReleaseTypeAttr(psItfAttr);
        psItfAttr = 0;  
        pItfITI->Release();
        pItfITI = 0;
    }

    // Retrieve the number of interfaces the coclass has
    NumInterfaces = pCoClassTypeAttr->cImplTypes;

    // Go through all the interfaces and add them to the map.
    for (i=0; i < NumInterfaces; i++)
    {
        // Get the impl flags.
        IfFailGo(pCoClassITI->GetImplTypeFlags(i, &ImplFlags));

        // If this is an implemented interface.
        if (!(ImplFlags & IMPLTYPEFLAG_FSOURCE))
        {
            IfFailGo(pCoClassITI->GetRefTypeOfImplType(i, &href));
            IfFailGo(pCoClassITI->GetRefTypeInfo(href, &pItfITI));

            do
            {
                // Retrieve the attributes of the interface type info.
                IfFailGo(pItfITI->GetTypeAttr(&psItfAttr));

                // If there already is a CoClass that implements this 
                // interface then we do not want to do the mapping.
                ImpTlbClassItfInfo sSearch, *pMapped;
                sSearch.ItfIID = psItfAttr->guid;
                pMapped = Find(&sSearch);
                if (pMapped)
                {
                    // There already is a CoClass that implements the interface. If that
                    // CoClass is not the current one, then we we set the class itf name 
                    // to NULL to indicate not to do the def itf to class itf convertion 
                    // for this interface.
                    if (pMapped->szClassItfName && wcscmp(pMapped->szClassItfName, bstrClassItfName) != 0)
                        pMapped->szClassItfName = NULL;
                }
                else
                {
                    // Add an entry with a NULL name to prevent future substitutions.
                    ImpTlbClassItfInfo sNew;
                    sNew.ItfIID = psItfAttr->guid;
                    sNew.szClassItfName = NULL;
                    IfNullGo(Add(&sNew)); 
                }

                // If there is a base interface, then handle it also.
                if (psItfAttr->cImplTypes == 1)
                {
                    IfFailGo(pItfITI->GetRefTypeOfImplType(0, &href));
                    IfFailGo(pItfITI->GetRefTypeInfo(href, &pBaseItfITI));                       
                }

                // Release for next interface.
                if (psItfAttr)
                {
                    pItfITI->ReleaseTypeAttr(psItfAttr);
                    psItfAttr = 0;
                }
                if (pItfITI)
                {
                    pItfITI->Release();
                    pItfITI = 0;
                }

                // Set the current interface to the base interface.
                pItfITI = pBaseItfITI;
                pBaseItfITI = 0;
            }
            while(pItfITI);
        }       
    }

ErrExit:
    if (psItfAttr)
        pItfITI->ReleaseTypeAttr(psItfAttr);
    if (pItfITI)
        pItfITI->Release();
    if (bstrClassItfName)
        ::SysFreeString(bstrClassItfName);

    return hr;
}

LPCWSTR CImpTlbDefItfToClassItfMap::GetClassItfName(IID &rItfIID)
{
    ImpTlbClassItfInfo sSearch, *pMapped;
    sSearch.ItfIID = rItfIID;
    pMapped = Find(&sSearch);
    return pMapped ? pMapped->szClassItfName : NULL;
}

unsigned int CImpTlbDefItfToClassItfMap::Hash(const ImpTlbClassItfInfo *pData)
{
    // Starting value for hash.
    ULONG   hash = 5381;
    
    // Hash in the IID.
    const BYTE *pbData = reinterpret_cast<const BYTE *>(&pData->ItfIID);
    int iSize = sizeof(IID);
    while (--iSize >= 0)
    {
        hash = ((hash << 5) + hash) ^ *pbData;
        ++pbData;
    }

    return hash;
} // unsigned int CImpTlbDefItfToClassItfMap::Hash()

unsigned int CImpTlbDefItfToClassItfMap::Compare(const ImpTlbClassItfInfo *p1, ImpTlbClassItfInfo *p2)
{
    // Compare the IID's.
    return memcmp(&p1->ItfIID, &p2->ItfIID, sizeof(IID));
} // unsigned int CImpTlbEventInfoMap::Compare()

CImpTlbDefItfToClassItfMap::ELEMENTSTATUS CImpTlbDefItfToClassItfMap::Status(ImpTlbClassItfInfo *p)
{
    if (IsEqualGUID(p->ItfIID, FREE_STATUS_GUID))
    {
        return (FREE);
    }
    else if (IsEqualGUID(p->ItfIID, DELETED_STATUS_GUID))
    {
        return (DELETED);
    }
    return (USED);
} // CImpTlbDefItfToClassItfMap::ELEMENTSTATUS CImpTlbEventInfoMap::Status()

void CImpTlbDefItfToClassItfMap::SetStatus(ImpTlbClassItfInfo *p, ELEMENTSTATUS s)
{
    if (s == FREE)
    {
        p->ItfIID = FREE_STATUS_GUID;
    }
    else if (s == DELETED)
    {
        p->ItfIID = DELETED_STATUS_GUID;
    }
    else
    {
        _ASSERTE(!"Invalid status!");
    }
} // void CImpTlbDefItfToClassItfMap::SetStatus()

void *CImpTlbDefItfToClassItfMap::GetKey(ImpTlbClassItfInfo *p)
{
    return p;
} // void *CImpTlbDefItfToClassItfMap::GetKey()

ImpTlbClassItfInfo* CImpTlbDefItfToClassItfMap::Add(const ImpTlbClassItfInfo *pData)
{
    // Add the new entry to the map.
    const void *pvData = pData;
    ImpTlbClassItfInfo *pNew = Super::Add(const_cast<void*>(pvData));
    if (pNew == 0)
        return 0;

    // Copy the IID.
    pNew->ItfIID = pData->ItfIID;

    // Copy the class interface name.
    if (pData->szClassItfName)
    {
        pNew->szClassItfName = m_Names.Alloc((ULONG)wcslen(pData->szClassItfName)+1);
        if (pNew->szClassItfName == 0)
            return 0;
        wcscpy_s((LPWSTR)pNew->szClassItfName, wcslen(pData->szClassItfName)+1, pData->szClassItfName);
    }
    else
    {
        pNew->szClassItfName = NULL;
    }

    // Return the new entry.
    return pNew;
} // ImpTlbEventInfo* CImpTlbEventInfoMap::Add()

// EOF =======================================================================
