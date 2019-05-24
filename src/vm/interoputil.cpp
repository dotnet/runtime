// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


#include "common.h"
#include "vars.hpp"
#include "excep.h"
#include "interoputil.h"
#include "cachelinealloc.h"
#include "comutilnative.h"
#include "field.h"
#include "guidfromname.h"
#include "eeconfig.h"
#include "mlinfo.h"
#include "comdelegate.h"
#include "appdomain.hpp"
#include "prettyprintsig.h"
#include "util.hpp"
#include "interopconverter.h"
#include "wrappers.h"
#include "invokeutil.h"
#include "comcallablewrapper.h"
#include "../md/compiler/custattr.h"
#include "siginfo.hpp"
#include "eemessagebox.h"
#include "finalizerthread.h"

#ifdef FEATURE_COMINTEROP
#include "cominterfacemarshaler.h"
#include <roerrorapi.h>
#endif

#ifdef FEATURE_COMINTEROP_APARTMENT_SUPPORT
#include "olecontexthelpers.h"
#endif // FEATURE_COMINTEROP_APARTMENT_SUPPORT

#ifdef FEATURE_COMINTEROP
#include "dispex.h"
#include "runtimecallablewrapper.h"
#include "comtoclrcall.h"
#include "clrtocomcall.h"
#include "comcache.h"
#include "commtmemberinfomap.h"
#include "olevariant.h"
#include "stdinterfaces.h"
#include "notifyexternals.h"
#include "typeparse.h"
#include "..\md\winmd\inc\adapter.h"
#include "winrttypenameconverter.h"
#include "interoputil.inl"
#include "typestring.h"

#ifndef __ILanguageExceptionErrorInfo_INTERFACE_DEFINED__
#define __ILanguageExceptionErrorInfo_INTERFACE_DEFINED__
    EXTERN_C const IID IID_ILanguageExceptionErrorInfo;

    MIDL_INTERFACE("04a2dbf3-df83-116c-0946-0812abf6e07d")
    ILanguageExceptionErrorInfo : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetLanguageException( 
            /* [out] */ __RPC__deref_out_opt IUnknown **languageException) = 0;
        
    };
#endif // !__ILanguageExceptionErrorInfo_INTERFACE_DEFINED__

#define STANDARD_DISPID_PREFIX              W("[DISPID")
#define STANDARD_DISPID_PREFIX_LENGTH       7
#define GET_ENUMERATOR_METHOD_NAME          W("GetEnumerator")

// Note: All of the methods below must appear in the order in which the interfaces are defined in IL.
// WinRT -> CLR adapters
static const BinderMethodID s_stubsIterableToEnumerable[] =
{
    METHOD__ITERABLE_TO_ENUMERABLE_ADAPTER__GET_ENUMERATOR_STUB
};
static const BinderMethodID s_stubsVectorToList[] =
{
    METHOD__VECTOR_TO_LIST_ADAPTER__INDEXER_GET,
    METHOD__VECTOR_TO_LIST_ADAPTER__INDEXER_SET,
    METHOD__VECTOR_TO_LIST_ADAPTER__INDEX_OF,
    METHOD__VECTOR_TO_LIST_ADAPTER__INSERT,
    METHOD__VECTOR_TO_LIST_ADAPTER__REMOVE_AT
};
static const BinderMethodID s_stubsVectorToCollection[] =
{
    METHOD__VECTOR_TO_COLLECTION_ADAPTER__COUNT,
    METHOD__VECTOR_TO_COLLECTION_ADAPTER__IS_READ_ONLY,
    METHOD__VECTOR_TO_COLLECTION_ADAPTER__ADD,
    METHOD__VECTOR_TO_COLLECTION_ADAPTER__CLEAR,
    METHOD__VECTOR_TO_COLLECTION_ADAPTER__CONTAINS,
    METHOD__VECTOR_TO_COLLECTION_ADAPTER__COPY_TO,
    METHOD__VECTOR_TO_COLLECTION_ADAPTER__REMOVE
};
static const BinderMethodID s_stubsMapToDictionary[] =
{
    METHOD__MAP_TO_DICTIONARY_ADAPTER__INDEXER_GET,
    METHOD__MAP_TO_DICTIONARY_ADAPTER__INDEXER_SET,
    METHOD__MAP_TO_DICTIONARY_ADAPTER__KEYS,
    METHOD__MAP_TO_DICTIONARY_ADAPTER__VALUES,
    METHOD__MAP_TO_DICTIONARY_ADAPTER__CONTAINS_KEY,
    METHOD__MAP_TO_DICTIONARY_ADAPTER__ADD,
    METHOD__MAP_TO_DICTIONARY_ADAPTER__REMOVE,
    METHOD__MAP_TO_DICTIONARY_ADAPTER__TRY_GET_VALUE
};
static const BinderMethodID s_stubsMapToCollection[] =
{
    METHOD__MAP_TO_COLLECTION_ADAPTER__COUNT,
    METHOD__MAP_TO_COLLECTION_ADAPTER__IS_READ_ONLY,
    METHOD__MAP_TO_COLLECTION_ADAPTER__ADD,
    METHOD__MAP_TO_COLLECTION_ADAPTER__CLEAR,
    METHOD__MAP_TO_COLLECTION_ADAPTER__CONTAINS,
    METHOD__MAP_TO_COLLECTION_ADAPTER__COPY_TO,
    METHOD__MAP_TO_COLLECTION_ADAPTER__REMOVE
};
static const BinderMethodID s_stubsIVectorViewToIReadOnlyCollection[] =
{
    METHOD__IVECTORVIEW_TO_IREADONLYCOLLECTION_ADAPTER__COUNT,
};
static const BinderMethodID s_stubsIVectorViewToIReadOnlyList[] =
{
    METHOD__IVECTORVIEW_TO_IREADONLYLIST_ADAPTER__INDEXER_GET,
};
static const BinderMethodID s_stubsIMapViewToIReadOnlyCollection[] =
{
    METHOD__IMAPVIEW_TO_IREADONLYCOLLECTION_ADAPTER__COUNT,
};
static const BinderMethodID s_stubsIMapViewToIReadOnlyDictionary[] =
{
    METHOD__IMAPVIEW_TO_IREADONLYDICTIONARY_ADAPTER__CONTAINSKEY,
    METHOD__IMAPVIEW_TO_IREADONLYDICTIONARY_ADAPTER__TRYGETVALUE,
    METHOD__IMAPVIEW_TO_IREADONLYDICTIONARY_ADAPTER__INDEXER_GET,
    METHOD__IMAPVIEW_TO_IREADONLYDICTIONARY_ADAPTER__KEYS,
    METHOD__IMAPVIEW_TO_IREADONLYDICTIONARY_ADAPTER__VALUES
};
static const BinderMethodID s_stubsBindableIterableToEnumerable[] =
{
    METHOD__BINDABLEITERABLE_TO_ENUMERABLE_ADAPTER__GET_ENUMERATOR_STUB
};
static const BinderMethodID s_stubsBindableVectorToList[] =
{
    METHOD__BINDABLEVECTOR_TO_LIST_ADAPTER__INDEXER_GET,
    METHOD__BINDABLEVECTOR_TO_LIST_ADAPTER__INDEXER_SET,
    METHOD__BINDABLEVECTOR_TO_LIST_ADAPTER__ADD,
    METHOD__BINDABLEVECTOR_TO_LIST_ADAPTER__CONTAINS,
    METHOD__BINDABLEVECTOR_TO_LIST_ADAPTER__CLEAR,
    METHOD__BINDABLEVECTOR_TO_LIST_ADAPTER__IS_READ_ONLY,
    METHOD__BINDABLEVECTOR_TO_LIST_ADAPTER__IS_FIXED_SIZE,
    METHOD__BINDABLEVECTOR_TO_LIST_ADAPTER__INDEX_OF,
    METHOD__BINDABLEVECTOR_TO_LIST_ADAPTER__INSERT,
    METHOD__BINDABLEVECTOR_TO_LIST_ADAPTER__REMOVE,
    METHOD__BINDABLEVECTOR_TO_LIST_ADAPTER__REMOVE_AT
};
static const BinderMethodID s_stubsBindableVectorToCollection[] =
{
    METHOD__BINDABLEVECTOR_TO_COLLECTION_ADAPTER__COPY_TO,
    METHOD__BINDABLEVECTOR_TO_COLLECTION_ADAPTER__COUNT,
    METHOD__BINDABLEVECTOR_TO_COLLECTION_ADAPTER__SYNC_ROOT,
    METHOD__BINDABLEVECTOR_TO_COLLECTION_ADAPTER__IS_SYNCHRONIZED
};
static const BinderMethodID s_stubsNotifyCollectionChangedToManaged[] =
{
    (BinderMethodID)0, // add_CollectionChanged
    (BinderMethodID)1, // remove_CollectionChanged
};
static const BinderMethodID s_stubsNotifyPropertyChangedToManaged[] =
{
    (BinderMethodID)0, // add_PropertyChanged
    (BinderMethodID)1, // remove_PropertyChanged
};
static const BinderMethodID s_stubsICommandToManaged[] =
{
    (BinderMethodID)0, // add_CanExecuteChanged
    (BinderMethodID)1, // remove_CanExecuteChanged
    (BinderMethodID)2, // CanExecute
    (BinderMethodID)3, // Execute
};

static const BinderMethodID s_stubsClosableToDisposable[] =
{
    METHOD__ICLOSABLE_TO_IDISPOSABLE_ADAPTER__DISPOSE
};

// CLR -> WinRT adapters
static const BinderMethodID s_stubsEnumerableToIterable[] =
{
    METHOD__ENUMERABLE_TO_ITERABLE_ADAPTER__FIRST_STUB
};
static const BinderMethodID s_stubsListToVector[] =
{
    METHOD__LIST_TO_VECTOR_ADAPTER__GET_AT,
    METHOD__LIST_TO_VECTOR_ADAPTER__SIZE,
    METHOD__LIST_TO_VECTOR_ADAPTER__GET_VIEW,
    METHOD__LIST_TO_VECTOR_ADAPTER__INDEX_OF,
    METHOD__LIST_TO_VECTOR_ADAPTER__SET_AT,
    METHOD__LIST_TO_VECTOR_ADAPTER__INSERT_AT,
    METHOD__LIST_TO_VECTOR_ADAPTER__REMOVE_AT,
    METHOD__LIST_TO_VECTOR_ADAPTER__APPEND,
    METHOD__LIST_TO_VECTOR_ADAPTER__REMOVE_AT_END,
    METHOD__LIST_TO_VECTOR_ADAPTER__CLEAR,
    METHOD__LIST_TO_VECTOR_ADAPTER__GET_MANY,
    METHOD__LIST_TO_VECTOR_ADAPTER__REPLACE_ALL,
};
static const BinderMethodID s_stubsDictionaryToMap[] =
{
    METHOD__DICTIONARY_TO_MAP_ADAPTER__LOOKUP,
    METHOD__DICTIONARY_TO_MAP_ADAPTER__SIZE,
    METHOD__DICTIONARY_TO_MAP_ADAPTER__HAS_KEY,
    METHOD__DICTIONARY_TO_MAP_ADAPTER__GET_VIEW,
    METHOD__DICTIONARY_TO_MAP_ADAPTER__INSERT,
    METHOD__DICTIONARY_TO_MAP_ADAPTER__REMOVE,
    METHOD__DICTIONARY_TO_MAP_ADAPTER__CLEAR,
};
static const BinderMethodID s_stubsIReadOnlyListToIVectorView[] =
{
    METHOD__IREADONLYLIST_TO_IVECTORVIEW_ADAPTER__GETAT,
    METHOD__IREADONLYLIST_TO_IVECTORVIEW_ADAPTER__SIZE,
    METHOD__IREADONLYLIST_TO_IVECTORVIEW_ADAPTER__INDEXOF,
    METHOD__IREADONLYLIST_TO_IVECTORVIEW_ADAPTER__GETMANY,
};
static const BinderMethodID s_stubsIReadOnlyDictionaryToIMapView[] =
{
    METHOD__IREADONLYDICTIONARY_TO_IMAPVIEW_ADAPTER__LOOKUP,
    METHOD__IREADONLYDICTIONARY_TO_IMAPVIEW_ADAPTER__SIZE,
    METHOD__IREADONLYDICTIONARY_TO_IMAPVIEW_ADAPTER__HASKEY,
    METHOD__IREADONLYDICTIONARY_TO_IMAPVIEW_ADAPTER__SPLIT,
};
static const BinderMethodID s_stubsEnumerableToBindableIterable[] =
{
    METHOD__ENUMERABLE_TO_BINDABLEITERABLE_ADAPTER__FIRST_STUB
};
static const BinderMethodID s_stubsListToBindableVector[] =
{
    METHOD__LIST_TO_BINDABLEVECTOR_ADAPTER__GET_AT,
    METHOD__LIST_TO_BINDABLEVECTOR_ADAPTER__SIZE,
    METHOD__LIST_TO_BINDABLEVECTOR_ADAPTER__GET_VIEW,
    METHOD__LIST_TO_BINDABLEVECTOR_ADAPTER__INDEX_OF,
    METHOD__LIST_TO_BINDABLEVECTOR_ADAPTER__SET_AT,
    METHOD__LIST_TO_BINDABLEVECTOR_ADAPTER__INSERT_AT,
    METHOD__LIST_TO_BINDABLEVECTOR_ADAPTER__REMOVE_AT,
    METHOD__LIST_TO_BINDABLEVECTOR_ADAPTER__APPEND,
    METHOD__LIST_TO_BINDABLEVECTOR_ADAPTER__REMOVE_AT_END,
    METHOD__LIST_TO_BINDABLEVECTOR_ADAPTER__CLEAR
};
static const BinderMethodID s_stubsNotifyCollectionChangedToWinRT[] =
{
    (BinderMethodID)0, // add_CollectionChanged
    (BinderMethodID)1, // remove_CollectionChanged
};
static const BinderMethodID s_stubsNotifyPropertyChangedToWinRT[] =
{
    (BinderMethodID)0, // add_PropertyChanged
    (BinderMethodID)1, // remove_PropertyChanged
};
static const BinderMethodID s_stubsICommandToWinRT[] =
{
    (BinderMethodID)0, // add_CanExecuteChanged
    (BinderMethodID)1, // remove_CanExecuteChanged
    (BinderMethodID)2, // CanExecute
    (BinderMethodID)3, // Execute
};


static const LPCUTF8 s_stubNamesNotifyCollectionChanged[] =
{
    "add_CollectionChanged",    // 0
    "remove_CollectionChanged", // 1
};

static const LPCUTF8 s_stubNamesNotifyPropertyChanged[] =
{
    "add_PropertyChanged",    // 0
    "remove_PropertyChanged", // 1
};

static const LPCUTF8 s_stubNamesICommand[] =
{
    "add_CanExecuteChanged",    // 0
    "remove_CanExecuteChanged", // 1
    "CanExecute",               // 2
    "Execute",                  // 3
};

static const BinderMethodID s_stubsDisposableToClosable[] =
{
    METHOD__IDISPOSABLE_TO_ICLOSABLE_ADAPTER__CLOSE
};

DEFINE_ASM_QUAL_TYPE_NAME(NCCWINRT_ASM_QUAL_TYPE_NAME, g_INotifyCollectionChanged_WinRTName, g_SystemRuntimeWindowsRuntimeAsmName, VER_ASSEMBLYVERSION_STR, g_ECMAKeyToken);
DEFINE_ASM_QUAL_TYPE_NAME(NCCMA_ASM_QUAL_TYPE_NAME, g_NotifyCollectionChangedToManagedAdapterName, g_SystemRuntimeWindowsRuntimeAsmName, VER_ASSEMBLYVERSION_STR, g_ECMAKeyToken);
DEFINE_ASM_QUAL_TYPE_NAME(NCCWA_ASM_QUAL_TYPE_NAME, g_NotifyCollectionChangedToWinRTAdapterName, g_SystemRuntimeWindowsRuntimeAsmName, VER_ASSEMBLYVERSION_STR, g_ECMAKeyToken);
DEFINE_ASM_QUAL_TYPE_NAME(NPCWINRT_ASM_QUAL_TYPE_NAME, g_INotifyPropertyChanged_WinRTName, g_SystemRuntimeWindowsRuntimeAsmName, VER_ASSEMBLYVERSION_STR, g_ECMAKeyToken);
DEFINE_ASM_QUAL_TYPE_NAME(NPCMA_ASM_QUAL_TYPE_NAME, g_NotifyPropertyChangedToManagedAdapterName, g_SystemRuntimeWindowsRuntimeAsmName, VER_ASSEMBLYVERSION_STR, g_ECMAKeyToken);
DEFINE_ASM_QUAL_TYPE_NAME(NPCWA_ASM_QUAL_TYPE_NAME, g_NotifyPropertyChangedToWinRTAdapterName, g_SystemRuntimeWindowsRuntimeAsmName, VER_ASSEMBLYVERSION_STR, g_ECMAKeyToken);
DEFINE_ASM_QUAL_TYPE_NAME(CMDWINRT_ASM_QUAL_TYPE_NAME, g_ICommand_WinRTName, g_SystemRuntimeWindowsRuntimeAsmName, VER_ASSEMBLYVERSION_STR, g_ECMAKeyToken);
DEFINE_ASM_QUAL_TYPE_NAME(CMDMA_ASM_QUAL_TYPE_NAME, g_ICommandToManagedAdapterName, g_SystemRuntimeWindowsRuntimeAsmName, VER_ASSEMBLYVERSION_STR, g_ECMAKeyToken);
DEFINE_ASM_QUAL_TYPE_NAME(CMDWA_ASM_QUAL_TYPE_NAME, g_ICommandToWinRTAdapterName, g_SystemRuntimeWindowsRuntimeAsmName, VER_ASSEMBLYVERSION_STR, g_ECMAKeyToken);
DEFINE_ASM_QUAL_TYPE_NAME(NCCEHWINRT_ASM_QUAL_TYPE_NAME, g_NotifyCollectionChangedEventHandler_WinRT, g_SystemRuntimeWindowsRuntimeAsmName, VER_ASSEMBLYVERSION_STR, g_ECMAKeyToken);
DEFINE_ASM_QUAL_TYPE_NAME(PCEHWINRT_ASM_QUAL_TYPE_NAME, g_PropertyChangedEventHandler_WinRT_Name, g_SystemRuntimeWindowsRuntimeAsmName, VER_ASSEMBLYVERSION_STR, g_ECMAKeyToken);

const WinRTInterfaceRedirector::NonMscorlibRedirectedInterfaceInfo WinRTInterfaceRedirector::s_rNonMscorlibInterfaceInfos[3] =
{
    {
        NCCWINRT_ASM_QUAL_TYPE_NAME,
        NCCMA_ASM_QUAL_TYPE_NAME,
        NCCWA_ASM_QUAL_TYPE_NAME,
        s_stubNamesNotifyCollectionChanged 
    },
    {
        NPCWINRT_ASM_QUAL_TYPE_NAME,
        NPCMA_ASM_QUAL_TYPE_NAME,
        NPCWA_ASM_QUAL_TYPE_NAME,
        s_stubNamesNotifyPropertyChanged 
    },
    {
        CMDWINRT_ASM_QUAL_TYPE_NAME,
        CMDMA_ASM_QUAL_TYPE_NAME,
        CMDWA_ASM_QUAL_TYPE_NAME,
        s_stubNamesICommand 
    },
};

#define SYSTEMDLL__INOTIFYCOLLECTIONCHANGED ((BinderClassID)(WinRTInterfaceRedirector::NON_MSCORLIB_MARKER | 0))
#define SYSTEMDLL__INOTIFYPROPERTYCHANGED   ((BinderClassID)(WinRTInterfaceRedirector::NON_MSCORLIB_MARKER | 1))
#define SYSTEMDLL__ICOMMAND                 ((BinderClassID)(WinRTInterfaceRedirector::NON_MSCORLIB_MARKER | 2))

const WinRTInterfaceRedirector::RedirectedInterfaceStubInfo WinRTInterfaceRedirector::s_rInterfaceStubInfos[2 * s_NumRedirectedInterfaces] =
{
    { CLASS__IITERABLE,                    _countof(s_stubsIterableToEnumerable),             s_stubsIterableToEnumerable,             _countof(s_stubsEnumerableToIterable),           s_stubsEnumerableToIterable           },
    { CLASS__IVECTOR,                      _countof(s_stubsVectorToList),                     s_stubsVectorToList,                     _countof(s_stubsListToVector),                   s_stubsListToVector                   },
    { CLASS__IMAP,                         _countof(s_stubsMapToDictionary),                  s_stubsMapToDictionary,                  _countof(s_stubsDictionaryToMap),                s_stubsDictionaryToMap                },
    { CLASS__IVECTORVIEW,                  _countof(s_stubsIVectorViewToIReadOnlyList),       s_stubsIVectorViewToIReadOnlyList,       _countof(s_stubsIReadOnlyListToIVectorView),     s_stubsIReadOnlyListToIVectorView     },
    { CLASS__IMAPVIEW,                     _countof(s_stubsIMapViewToIReadOnlyDictionary),    s_stubsIMapViewToIReadOnlyDictionary,    _countof(s_stubsIReadOnlyDictionaryToIMapView),  s_stubsIReadOnlyDictionaryToIMapView  },
    { CLASS__IBINDABLEITERABLE,            _countof(s_stubsBindableIterableToEnumerable),     s_stubsBindableIterableToEnumerable,     _countof(s_stubsEnumerableToBindableIterable),   s_stubsEnumerableToBindableIterable   },
    { CLASS__IBINDABLEVECTOR,              _countof(s_stubsBindableVectorToList),             s_stubsBindableVectorToList,             _countof(s_stubsListToBindableVector),           s_stubsListToBindableVector           },
    { SYSTEMDLL__INOTIFYCOLLECTIONCHANGED, _countof(s_stubsNotifyCollectionChangedToManaged), s_stubsNotifyCollectionChangedToManaged, _countof(s_stubsNotifyCollectionChangedToWinRT), s_stubsNotifyCollectionChangedToWinRT },
    { SYSTEMDLL__INOTIFYPROPERTYCHANGED,   _countof(s_stubsNotifyPropertyChangedToManaged),   s_stubsNotifyPropertyChangedToManaged,   _countof(s_stubsNotifyPropertyChangedToWinRT),   s_stubsNotifyPropertyChangedToWinRT   },
    { SYSTEMDLL__ICOMMAND,                 _countof(s_stubsICommandToManaged),                s_stubsICommandToManaged,                _countof(s_stubsICommandToWinRT),                s_stubsICommandToWinRT                },
    { CLASS__ICLOSABLE,                    _countof(s_stubsClosableToDisposable),             s_stubsClosableToDisposable,             _countof(s_stubsClosableToDisposable),           s_stubsDisposableToClosable           },

    // ICollection/ICollection<> stubs:
    { (BinderClassID)0,                    0,                                                 NULL,                                    0,                                               NULL                                  },
    { CLASS__IVECTOR,                      _countof(s_stubsVectorToCollection),               s_stubsVectorToCollection,               0,                                               NULL                                  },
    { CLASS__IMAP,                         _countof(s_stubsMapToCollection),                  s_stubsMapToCollection,                  0,                                               NULL                                  },
    { CLASS__IVECTORVIEW,                  _countof(s_stubsIVectorViewToIReadOnlyCollection), s_stubsIVectorViewToIReadOnlyCollection, 0,                                               NULL                                  },
    { CLASS__IMAPVIEW,                     _countof(s_stubsIMapViewToIReadOnlyCollection),    s_stubsIMapViewToIReadOnlyCollection,    0,                                               NULL                                  },
    { (BinderClassID)0,                    0,                                                 NULL,                                    0,                                               NULL                                  },
    { CLASS__IBINDABLEVECTOR,              _countof(s_stubsBindableVectorToCollection),       s_stubsBindableVectorToCollection,       0,                                               NULL                                  },
    { (BinderClassID)0,                    0,                                                 NULL,                                    0,                                               NULL                                  },
    { (BinderClassID)0,                    0,                                                 NULL,                                    0,                                               NULL                                  },
    { (BinderClassID)0,                    0,                                                 NULL,                                    0,                                               NULL                                  },
    { (BinderClassID)0,                    0,                                                 NULL,                                    0,                                               NULL                                  },
};

#ifdef _DEBUG
    VOID IntializeInteropLogging();
#endif 

struct ByrefArgumentInfo
{
    BOOL        m_bByref;
    VARIANT     m_Val;
};

// Flag indicating if COM support has been initialized.
BOOL    g_fComStarted = FALSE;

#ifdef FEATURE_COMINTEROP_UNMANAGED_ACTIVATION
void AllocateComClassObject(ComClassFactory* pComClsFac, OBJECTREF* pComObj);
#endif // FEATURE_COMINTEROP_UNMANAGED_ACTIVATION

#endif // FEATURE_COMINTEROP


#ifndef CROSSGEN_COMPILE
//------------------------------------------------------------------
// setup error info for exception object
//
#ifdef FEATURE_COMINTEROP
HRESULT SetupErrorInfo(OBJECTREF pThrownObject, ComCallMethodDesc *pCMD)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pCMD));
    }
    CONTRACTL_END;

    return SetupErrorInfo(pThrownObject, pCMD->IsWinRTCall());
}

typedef BOOL (*pfnRoOriginateLanguageException)(HRESULT error,
                                                HSTRING message,
                                                IUnknown* languageException);
typedef HRESULT (*pfnGetRestrictedErrorInfo)(IRestrictedErrorInfo ** ppRestrictedErrorInfo);
typedef HRESULT (*pfnSetRestrictedErrorInfo)(IRestrictedErrorInfo * pRestrictedErrorInfo);

pfnRoOriginateLanguageException g_pfnRoOriginateLanguageException = nullptr;
pfnGetRestrictedErrorInfo g_pfnGetRestrictedErrorInfo = nullptr;
pfnSetRestrictedErrorInfo g_pfnSetRestrictedErrorInfo = nullptr;

Volatile<bool> g_bCheckedWinRTErrorDllPresent = false;

//--------------------------------------------------------------------------------
// Attempts to load WinRT error API functions from the appropriate system library,
// and populates g_pfnRoOriginateLanguageException, g_pfnGetRestrictedErrorInfo,
// and g_pfnSetRestrictedErrorInfo.
//
// This is shared logic for loading the WinRT error libraries that should not be
// called directly.
void LoadProcAddressForWinRTErrorAPIs_Internal()
{
    WRAPPER_NO_CONTRACT;

    GCX_PREEMP();

    if (!g_bCheckedWinRTErrorDllPresent) 
    {
        HMODULE hModWinRTError11Dll =  WszLoadLibraryEx(W("api-ms-win-core-winrt-error-l1-1-1.dll"), NULL, LOAD_LIBRARY_SEARCH_SYSTEM32);

        // We never release the library since we can only do it at AppDomain shutdown and there is no good way to release it then.
        if (hModWinRTError11Dll)
        {
            g_pfnRoOriginateLanguageException = (pfnRoOriginateLanguageException)GetProcAddress(hModWinRTError11Dll, "RoOriginateLanguageException");
            g_pfnSetRestrictedErrorInfo = (pfnSetRestrictedErrorInfo)GetProcAddress(hModWinRTError11Dll, "SetRestrictedErrorInfo");
            g_pfnGetRestrictedErrorInfo = (pfnGetRestrictedErrorInfo)GetProcAddress(hModWinRTError11Dll, "GetRestrictedErrorInfo");
        }
        else
        {
            // Downlevel versions of WinRT that do not have the language-projected exceptions will still have
            // APIs for IRestrictedErrorInfo, so we should still try to load those.
            HMODULE hModWinRTError10Dll = WszLoadLibraryEx(L"api-ms-win-core-winrt-error-l1-1-0.dll", NULL, LOAD_LIBRARY_SEARCH_SYSTEM32);

            if (hModWinRTError10Dll)
            {
                g_pfnSetRestrictedErrorInfo = (pfnSetRestrictedErrorInfo)GetProcAddress(hModWinRTError10Dll, "SetRestrictedErrorInfo");
                g_pfnGetRestrictedErrorInfo = (pfnGetRestrictedErrorInfo)GetProcAddress(hModWinRTError10Dll, "GetRestrictedErrorInfo");
            }
        }

        g_bCheckedWinRTErrorDllPresent = true;
    }
}

//--------------------------------------------------------------------------------
// Attempts to load the IRestrictedErrorInfo APIs into the function pointers
// g_pfnGetRestrictedErrorInfo and g_pfnSetRestrictedErrorInfo. This is used for
// WinRT scenarios where we don't care about support for language-projected exception
// support. Returns S_OK if both of these functions could be loaded, and E_FAIL
// otherwise.
HRESULT LoadProcAddressForRestrictedErrorInfoAPIs()
{
    WRAPPER_NO_CONTRACT;

    LoadProcAddressForWinRTErrorAPIs_Internal();

    if (g_pfnSetRestrictedErrorInfo != NULL && g_pfnGetRestrictedErrorInfo != NULL)
        return S_OK;
    else
        return E_FAIL;
}

//--------------------------------------------------------------------------------
// Attempts to load the RoOriginateLanguageException API for language-projected
// exceptions into the function pointer g_pfnRoOriginateLanguageException. Returns
// S_OK if this function could be loaded, and E_FAIL otherwise.
HRESULT LoadProcAddressForRoOriginateLanguageExceptionAPI()
{
    WRAPPER_NO_CONTRACT;

    LoadProcAddressForWinRTErrorAPIs_Internal();

    if (g_pfnRoOriginateLanguageException != NULL)
        return S_OK;
    else
        return E_FAIL;
}

//--------------------------------------------------------------------------------
// GetRestrictedErrorInfo helper, enables and disables GC during call-outs
HRESULT SafeGetRestrictedErrorInfo(IRestrictedErrorInfo **ppIRestrictedErrInfo)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(ppIRestrictedErrInfo));
    }
    CONTRACTL_END;

    *ppIRestrictedErrInfo = NULL;
    HRESULT hr = S_OK;

    if(SUCCEEDED(LoadProcAddressForRestrictedErrorInfoAPIs()))
    {
        GCX_PREEMP();
        
        EX_TRY
        {
            hr = (*g_pfnGetRestrictedErrorInfo)(ppIRestrictedErrInfo);
        }
        EX_CATCH
        {
            hr = E_OUTOFMEMORY;
        }
        EX_END_CATCH(SwallowAllExceptions);
    }

    return hr;
}

// This method checks whether the given IErrorInfo is actually a managed CLR object.
BOOL IsManagedObject(IUnknown *pIUnknown)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM());
        PRECONDITION(CheckPointer(pIUnknown));
    }
    CONTRACTL_END;

    //Check based on IUnknown slots, i.e. we'll see whether the IP maps to a CCW.
    if (MapIUnknownToWrapper(pIUnknown) != NULL)
    {
        // We found an existing CCW hence this is a managed exception.
        return TRUE;
    }
    return FALSE;
}

// This method returns the IErrorInfo associated with the IRestrictedErrorInfo.
// Return Value - a. IErrorInfo which corresponds to a managed exception object, where *bHasNonCLRLanguageErrorObject = FALSE
//                       b. IErrorInfo corresponding to a non-CLR exception object , where *bHasNonCLRLanguageErrorObject = TRUE
//                       c. NULL in case the current hr value is different from the one associated with IRestrictedErrorInfo.
IErrorInfo *GetCorrepondingErrorInfo_WinRT(HRESULT hr, IRestrictedErrorInfo *pResErrInfo, BOOL* bHasNonCLRLanguageErrorObject)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM());
        PRECONDITION(CheckPointer(pResErrInfo));
    }
    CONTRACTL_END;

    *bHasNonCLRLanguageErrorObject = FALSE;
    // This function must run in preemptive GC mode.
    {
        GCX_PREEMP();
        HRESULT hrLocal = S_OK;

        SafeComHolderPreemp<ILanguageExceptionErrorInfo> pLangException;

        // 1. Check whether the given IRestrictedErrorInfo supports ILanguageExceptionErrorInfo
        // 2. If so, retrieve the language specific IInspectable by calling GetLanguageException.
        // 3. Check whether the IInspectable is CLR specific.
        // 4. If so, return the IInspectable as it is also the IErrorInfo.
        // 5. If not, check whether the HResult returned by the API is same as the one stored in IRestrictedErrorInfo.
        // 6. If so simply QI for IErrorInfo 
        // 7. If QI succeeds return IErrorInfo else return NULL.

        hrLocal = SafeQueryInterfacePreemp(pResErrInfo, IID_ILanguageExceptionErrorInfo, (IUnknown **) &pLangException);
        LogInteropQI(pResErrInfo, IID_ILanguageExceptionErrorInfo, hr, "ILanguageExceptionErrorInfo");
        if (SUCCEEDED(hrLocal))
        {
            IUnknown* pUnk;
            if(pLangException != NULL && SUCCEEDED(pLangException->GetLanguageException((IUnknown**) &pUnk)) && pUnk != NULL)
            {
                if(IsManagedObject(pUnk))
                {
                    // Since this represent a managed CCW, this is our exception object and will always be an IErrorInfo.
                    // Hence type casting to IErrorInfo is safe.
                    return (IErrorInfo*)pUnk;
                }
                else
                {
                    // pUnk represents an exception object of a different language.
                    // We simply need to store that the exception object represents a non-CLR exception and can release the actual exception object.
                    SafeReleasePreemp(pUnk);
                    *bHasNonCLRLanguageErrorObject = TRUE;
                }
            }
        }
        if(SUCCEEDED(GetRestrictedErrorDetails(pResErrInfo, NULL, NULL, &hrLocal, NULL)))
        {
            if(hr == hrLocal)
            {
                IErrorInfo *pErrInfo = NULL ;
                hrLocal = SafeQueryInterfacePreemp(pResErrInfo, IID_IErrorInfo, (IUnknown **) &pErrInfo);
                LogInteropQI(pResErrInfo, IID_IErrorInfo, hrLocal, "IErrorInfo");
                if(SUCCEEDED(hrLocal))
                {
                    return pErrInfo;
                }
            }
        }
    }

    return NULL;
}

HRESULT GetRestrictedErrorDetails(IRestrictedErrorInfo *pRestrictedErrorInfo, BSTR *perrorDescription, BSTR *pErrorRestrictedDescription, HRESULT *pHr, BSTR *pErrorCapabilitySid)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pRestrictedErrorInfo));
    }
    CONTRACTL_END;

    GCX_PREEMP();

    BSTR errDesc;
    BSTR errResDesc;
    BSTR errCapSid;
    HRESULT hrLocal;

    if(SUCCEEDED(pRestrictedErrorInfo->GetErrorDetails(&errDesc, &hrLocal, &errResDesc, &errCapSid)))
    {
        if(perrorDescription)
            *perrorDescription = errDesc;
        else
            ::SysFreeString(errDesc);

        if(pErrorRestrictedDescription)
            *pErrorRestrictedDescription = errResDesc;
        else
            ::SysFreeString(errResDesc);

        if(pErrorCapabilitySid)
            *pErrorCapabilitySid = errCapSid;
        else
            ::SysFreeString(errCapSid);
        if(pHr)
            *pHr = hrLocal;

        return S_OK;
    }

    return E_FAIL;
}

// HRESULT for CLR created IErrorInfo pointers are accessible
// from the enclosing simple wrapper
// This is in-proc only.
HRESULT GetHRFromCLRErrorInfo(IErrorInfo* pErr)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pErr));
        PRECONDITION(IsInProcCCWTearOff(pErr));
        PRECONDITION(IsSimpleTearOff(pErr));
    }
    CONTRACTL_END;

    SimpleComCallWrapper* pSimpleWrap = SimpleComCallWrapper::GetWrapperFromIP(pErr);
    return pSimpleWrap->IErrorInfo_hr();
}
#endif // FEATURE_COMINTEROP

HRESULT SetupErrorInfo(OBJECTREF pThrownObject, BOOL bIsWinRTScenario /* = FALSE */)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    HRESULT hr = E_FAIL;

#ifdef FEATURE_COMINTEROP
    Exception* pException = NULL;
#endif

    GCPROTECT_BEGIN(pThrownObject)
    {
        EX_TRY
        {
            // Calls to COM up ahead.
            hr = EnsureComStartedNoThrow();
            if (SUCCEEDED(hr) && pThrownObject != NULL)
            {
#ifdef _DEBUG            
                EX_TRY
                {
                    StackSString message;
                    GetExceptionMessage(pThrownObject, message);

                    if (g_pConfig->ShouldExposeExceptionsInCOMToConsole())
                    {
                        PrintToStdOutW(W(".NET exception in COM\n"));
                        if (!message.IsEmpty()) 
                            PrintToStdOutW(message.GetUnicode());
                        else
                            PrintToStdOutW(W("No exception info available"));
                    }

                    if (g_pConfig->ShouldExposeExceptionsInCOMToMsgBox())
                    {
                        GCX_PREEMP();
                        if (!message.IsEmpty()) 
                            EEMessageBoxNonLocalizedDebugOnly((LPWSTR)message.GetUnicode(), W(".NET exception in COM"), MB_ICONSTOP | MB_OK);
                        else
                            EEMessageBoxNonLocalizedDebugOnly(W("No exception information available"), W(".NET exception in COM"),MB_ICONSTOP | MB_OK);
                    }
                }
                EX_CATCH
                {
                }
                EX_END_CATCH (SwallowAllExceptions);
#endif

#ifdef FEATURE_COMINTEROP
                IErrorInfo* pErr = NULL;
                EX_TRY
                {
                    // This handles a special case for a newer subset of WinRT scenarios, starting in Windows
                    // 8.1, where we have support for language-projected extensions. In this case, we can use
                    // the thrown object to set up a projected IErrorInfo that we'll send back to native code.
                    //
                    // In all other scenarios (including WinRT prior to Windows 8.1), we just use the legacy
                    // IErrorInfo COM APIs.
                    if (bIsWinRTScenario &&
                        SUCCEEDED(LoadProcAddressForRestrictedErrorInfoAPIs()) &&
                        SUCCEEDED(LoadProcAddressForRoOriginateLanguageExceptionAPI()))
                    {
                        // In case of WinRT we check whether we have an already existing uncaught language exception.
                        // If so we simply SetRestrictedErrorInfo on that ensuring that the other language can catch that exception
                        // and we do not RoOriginateError from our side.
                        IRestrictedErrorInfo *pRestrictedErrorInfo = GetRestrictedErrorInfoFromErrorObject(pThrownObject);
                        if(pRestrictedErrorInfo != NULL)
                        {
                            (*g_pfnSetRestrictedErrorInfo)(pRestrictedErrorInfo);
                            GetRestrictedErrorDetails(pRestrictedErrorInfo, NULL, NULL, &hr, NULL);
                        }
                        else
                        {
                            // If there is no existing language exception we save the errorInfo on the current thread by storing the following information
                            // 1. HResult
                            // 2. ErrorMsg
                            // 3. The managed exception object, which can be later retrieved if needed.

                            pErr = (IErrorInfo *)GetComIPFromObjectRef(&pThrownObject, IID_IErrorInfo);

                            StackSString message;
                            HRESULT errorHr;
                            HSTRING errorMsgString;

                            GetExceptionMessage(pThrownObject, message);
                            errorHr = GetHRFromThrowable(pThrownObject);

                            if(FAILED(WindowsCreateString(message.GetUnicode(), message.GetCount(), &errorMsgString)))
                                errorMsgString = NULL;

                            //
                            // WinRT change to convert ObjectDisposedException into RO_E_CLOSED
                            // if we are calling into a WinRT managed object
                            //
                            if (errorHr == COR_E_OBJECTDISPOSED)
                                errorHr = RO_E_CLOSED;

                            // Set the managed exception 
                            {
                                GCX_PREEMP(); 
                                // This Windows API call will store the pErr as the LanguageException and
                                // construct an IRestrictedErrorInfo from the errorHr and errorMsgString
                                // which can then be later retrieved using GetRestrictedErrorInfo.
                                (*g_pfnRoOriginateLanguageException)(errorHr, errorMsgString, pErr);
                            }
                        }
                    }
                    else
                    {
                        // set the error info object for the exception that was thrown.
                        pErr = (IErrorInfo *)GetComIPFromObjectRef(&pThrownObject, IID_IErrorInfo);
                        {
                            GCX_PREEMP();
                            SetErrorInfo(0, pErr);
                        }
                    }

                    // Release the pErr in case it exists.
                    if (pErr)
                    {
                        hr = GetHRFromCLRErrorInfo(pErr);
                        ULONG cbRef = SafeRelease(pErr);
                        LogInteropRelease(pErr, cbRef, "IErrorInfo");
                    }
                }
                EX_CATCH
                {
                    hr = GET_EXCEPTION()->GetHR();
                }
                EX_END_CATCH(SwallowAllExceptions);
                //
                // WinRT change to convert ObjectDisposedException into RO_E_CLOSED
                // if we are calling into a WinRT managed object
                //
                if (hr == COR_E_OBJECTDISPOSED && bIsWinRTScenario)
                    hr = RO_E_CLOSED;
#endif // FEATURE_COMINTEROP
            }
        }
        EX_CATCH
        {
            if (SUCCEEDED(hr))
                hr = E_FAIL;
        }
        EX_END_CATCH(SwallowAllExceptions);
    }
    GCPROTECT_END();
    return hr;
}

//-------------------------------------------------------------------
 // Used to populate ExceptionData with COM data
//-------------------------------------------------------------------
void FillExceptionData(
    _Inout_ ExceptionData* pedata,
    _In_ IErrorInfo* pErrInfo,
    _In_opt_ IRestrictedErrorInfo* pRestrictedErrorInfo)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pedata));
    }
    CONTRACTL_END;
    
    if (pErrInfo != NULL)
    {
        Thread* pThread = GetThread();
        if (pThread != NULL)
        {
            GCX_PREEMP();
            
            pErrInfo->GetSource (&pedata->bstrSource);
            pErrInfo->GetDescription (&pedata->bstrDescription);
            pErrInfo->GetHelpFile (&pedata->bstrHelpFile);
            pErrInfo->GetHelpContext (&pedata->dwHelpContext );
            pErrInfo->GetGUID(&pedata->guid);

#ifdef FEATURE_COMINTEROP
            HRESULT hr = S_OK;
            if(pRestrictedErrorInfo == NULL)
            {
                hr = SafeQueryInterfacePreemp(pErrInfo, IID_IRestrictedErrorInfo, (IUnknown **) &pRestrictedErrorInfo);
                LogInteropQI(pErrInfo, IID_IRestrictedErrorInfo, hr, "IRestrictedErrorInfo");
            }

            if (SUCCEEDED(hr) && pRestrictedErrorInfo != NULL)
            {
                // Keep a AddRef-ed IRestrictedErrorInfo*
                pedata->pRestrictedErrorInfo = pRestrictedErrorInfo;

                // Retrieve restricted error information
                BSTR bstrDescription = NULL;
                HRESULT hrError;
                if (SUCCEEDED(GetRestrictedErrorDetails(pRestrictedErrorInfo, &bstrDescription, &pedata->bstrRestrictedError, &hrError, &pedata->bstrCapabilitySid)))
                {
                    if (bstrDescription != NULL)
                    {
                        ::SysFreeString(pedata->bstrDescription);
                        pedata->bstrDescription = bstrDescription;
                    }

                    _ASSERTE(hrError == pedata->hr);
                }

                // Retrieve reference string and ignore error
                pRestrictedErrorInfo->GetReference(&pedata->bstrReference);
            }
#endif
            ULONG cbRef = SafeRelease(pErrInfo); // release the IErrorInfo interface pointer
            LogInteropRelease(pErrInfo, cbRef, "IErrorInfo");
        }
    }
}
#endif // CROSSGEN_COMPILE

//---------------------------------------------------------------------------
// If pImport has the DefaultDllImportSearchPathsAttribute, 
// set the value of the attribute in pDlImportSearchPathFlags and return true.
BOOL GetDefaultDllImportSearchPathsAttributeValue(Module *pModule, mdToken token, DWORD * pDllImportSearchPathFlags)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(CheckPointer(pModule));
    }
    CONTRACTL_END;

    BYTE* pData = NULL;
    LONG cData = 0;

    HRESULT hr = pModule->GetCustomAttribute(token,
                                            WellKnownAttribute::DefaultDllImportSearchPaths,
                                            (const VOID **)(&pData),
                                            (ULONG *)&cData);

    IfFailThrow(hr);
    if(cData == 0 )
    {
        return FALSE;
    }

    CustomAttributeParser ca(pData, cData);
    CaArg args[1];
    args[0].InitEnum(SERIALIZATION_TYPE_U4, (ULONG)0);

    ParseKnownCaArgs(ca, args, lengthof(args));
    *pDllImportSearchPathFlags = args[0].val.u4;
    return TRUE;
}


//---------------------------------------------------------------------------
// Returns the index of the LCID parameter if one exists and -1 otherwise.
int GetLCIDParameterIndex(MethodDesc *pMD)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(CheckPointer(pMD));
    }
    CONTRACTL_END;
    
    int             iLCIDParam = -1;
    HRESULT         hr;
    const BYTE *    pVal;
    ULONG           cbVal;

    if (!pMD->GetMethodTable()->IsProjectedFromWinRT()) //  ignore LCIDConversionAttribute on WinRT methods
    {
        // Check to see if the method has the LCIDConversionAttribute.
        hr = pMD->GetCustomAttribute(WellKnownAttribute::LCIDConversion, (const void**)&pVal, &cbVal);
        if (hr == S_OK)
        {
            CustomAttributeParser caLCID(pVal, cbVal);
            CaArg args[1];
            args[0].Init(SERIALIZATION_TYPE_I4, 0);
            IfFailGo(ParseKnownCaArgs(caLCID, args, lengthof(args)));
            iLCIDParam = args[0].val.i4;
        }
    }

ErrExit:
    return iLCIDParam;
}

#ifndef CROSSGEN_COMPILE
//---------------------------------------------------------------------------
// Transforms an LCID into a CultureInfo.
void GetCultureInfoForLCID(LCID lcid, OBJECTREF *pCultureObj)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        INJECT_FAULT(COMPlusThrowOM());
        PRECONDITION(CheckPointer(pCultureObj));
    }
    CONTRACTL_END;

    OBJECTREF CultureObj = NULL;
    GCPROTECT_BEGIN(CultureObj)
    {
        // Allocate a CultureInfo with the specified LCID.
        CultureObj = AllocateObject(MscorlibBinder::GetClass(CLASS__CULTURE_INFO));

        MethodDescCallSite cultureInfoCtor(METHOD__CULTURE_INFO__INT_CTOR, &CultureObj);

        // Call the CultureInfo(int culture) constructor.
        ARG_SLOT pNewArgs[] = {
            ObjToArgSlot(CultureObj),
            (ARG_SLOT)lcid
        };
        cultureInfoCtor.Call(pNewArgs);

        // Set the returned culture object.
        *pCultureObj = CultureObj;
    }
    GCPROTECT_END();
}

#endif // CROSSGEN_COMPILE

//---------------------------------------------------------------------------
// This method determines if a member is visible from COM.
BOOL IsMemberVisibleFromCom(MethodTable *pDeclaringMT, mdToken tk, mdMethodDef mdAssociate)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(CheckPointer(pDeclaringMT));
    }
    CONTRACTL_END;
    
    HRESULT                 hr;
    const BYTE *            pVal;
    ULONG                   cbVal;
    DWORD                   dwFlags;

    IMDInternalImport *pInternalImport = pDeclaringMT->GetMDImport();
    Module *pModule = pDeclaringMT->GetModule();

    // Check to see if the member is public.
    switch (TypeFromToken(tk))
    {
        case mdtFieldDef:
            _ASSERTE(IsNilToken(mdAssociate));
            if (FAILED(pInternalImport->GetFieldDefProps(tk, &dwFlags)))
            {
                return FALSE;
            }
            if (!IsFdPublic(dwFlags))
                return FALSE;
            break;

        case mdtMethodDef:
            _ASSERTE(IsNilToken(mdAssociate));
            if (FAILED(pInternalImport->GetMethodDefProps(tk, &dwFlags)))
            {
                return FALSE;
            }
            if (!IsMdPublic(dwFlags))
            {
                return FALSE;
            }
            {
                // Generic Methods are not visible from COM
                MDEnumHolder hEnumTyPars(pInternalImport);
                if (FAILED(pInternalImport->EnumInit(mdtGenericParam, tk, &hEnumTyPars)))
                    return FALSE;

                if (pInternalImport->EnumGetCount(&hEnumTyPars) != 0)
                    return FALSE;
            }
            break;

        case mdtProperty:
            _ASSERTE(!IsNilToken(mdAssociate));
            if (FAILED(pInternalImport->GetMethodDefProps(mdAssociate, &dwFlags)))
            {
                return FALSE;
            }
            if (!IsMdPublic(dwFlags))
                return FALSE;
            
            if (!pDeclaringMT->IsProjectedFromWinRT() && !pDeclaringMT->IsExportedToWinRT() && !pDeclaringMT->IsWinRTObjectType())
            {
                // Check to see if the associate has the ComVisible attribute set (non-WinRT members only).
                hr = pModule->GetCustomAttribute(mdAssociate, WellKnownAttribute::ComVisible, (const void**)&pVal, &cbVal);
                if (hr == S_OK)
                {
                    CustomAttributeParser cap(pVal, cbVal);
                    if (FAILED(cap.SkipProlog()))
                        return FALSE;

                    UINT8 u1;
                    if (FAILED(cap.GetU1(&u1)))
                        return FALSE;

                    return (BOOL)u1;
                }
            }
            break;

        default:
            _ASSERTE(!"The type of the specified member is not handled by IsMemberVisibleFromCom");
            break;
    }

    if (!pDeclaringMT->IsProjectedFromWinRT() && !pDeclaringMT->IsExportedToWinRT() && !pDeclaringMT->IsWinRTObjectType())
    {
        // Check to see if the member has the ComVisible attribute set (non-WinRT members only).
        hr = pModule->GetCustomAttribute(tk, WellKnownAttribute::ComVisible, (const void**)&pVal, &cbVal);
        if (hr == S_OK)
        {
            CustomAttributeParser cap(pVal, cbVal);
            if (FAILED(cap.SkipProlog()))
                return FALSE;

            UINT8 u1;
            if (FAILED(cap.GetU1(&u1)))
                return FALSE;

            return (BOOL)u1;
        }
    }

    // The member is visible.
    return TRUE;
}


ULONG GetStringizedMethodDef(MethodTable *pDeclaringMT, mdToken tkMb, CQuickArray<BYTE> &rDef, ULONG cbCur)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(CheckPointer(pDeclaringMT));
    }
    CONTRACTL_END;
    
    IMDInternalImport *pMDImport = pDeclaringMT->GetMDImport();
    CQuickBytes     rSig;
    MDEnumHolder    ePm(pMDImport);         // For enumerating  params.
    mdParamDef      tkPm;                   // A param token.
    DWORD           dwFlags;                // Param flags.
    USHORT          usSeq;                  // Sequence of a parameter.
    ULONG           cPm;                    // Count of params.
    PCCOR_SIGNATURE pSig;
    ULONG           cbSig;

    // Don't count invisible members.
    if (!IsMemberVisibleFromCom(pDeclaringMT, tkMb, mdMethodDefNil))
        return cbCur;
    
    // accumulate the signatures.
    IfFailThrow(pMDImport->GetSigOfMethodDef(tkMb, &cbSig, &pSig));
    IfFailThrow(::PrettyPrintSigInternalLegacy(pSig, cbSig, "", &rSig, pMDImport));
    
    // Get the parameter flags.
    IfFailThrow(pMDImport->EnumInit(mdtParamDef, tkMb, &ePm));
    cPm = pMDImport->EnumGetCount(&ePm);
    
    // Resize for sig and params.  Just use 1 byte of param.
    rDef.ReSizeThrows(cbCur + rSig.Size() + cPm);
    memcpy(rDef.Ptr() + cbCur, rSig.Ptr(), rSig.Size());
    cbCur += (ULONG)(rSig.Size()-1);
    
    // Enumerate through the params and get the flags.
    while (pMDImport->EnumNext(&ePm, &tkPm))
    {
        LPCSTR szParamName_Ignore;
        IfFailThrow(pMDImport->GetParamDefProps(tkPm, &usSeq, &dwFlags, &szParamName_Ignore));
        if (usSeq == 0)     // Skip return type flags.
            continue;
        rDef[cbCur++] = (BYTE)dwFlags;
    }

    // Return the number of bytes.
    return cbCur;
} // void GetStringizedMethodDef()


ULONG GetStringizedFieldDef(MethodTable *pDeclaringMT, mdToken tkMb, CQuickArray<BYTE> &rDef, ULONG cbCur)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(CheckPointer(pDeclaringMT));
    }
    CONTRACTL_END;

    CQuickBytes         rSig;
    PCCOR_SIGNATURE     pSig;
    ULONG               cbSig;

    // Don't count invisible members.
    if (!IsMemberVisibleFromCom(pDeclaringMT, tkMb, mdMethodDefNil))
        return cbCur;
    
    IMDInternalImport *pMDImport = pDeclaringMT->GetMDImport();

    // accumulate the signatures.
    IfFailThrow(pMDImport->GetSigOfFieldDef(tkMb, &cbSig, &pSig));
    IfFailThrow(::PrettyPrintSigInternalLegacy(pSig, cbSig, "", &rSig, pMDImport));
    rDef.ReSizeThrows(cbCur + rSig.Size());
    memcpy(rDef.Ptr() + cbCur, rSig.Ptr(), rSig.Size());
    cbCur += (ULONG)(rSig.Size()-1);

    // Return the number of bytes.
    return cbCur;
} // void GetStringizedFieldDef()

//--------------------------------------------------------------------------------
// This method generates a stringized version of an interface that contains the
// name of the interface along with the signature of all the methods.
SIZE_T GetStringizedItfDef(TypeHandle InterfaceType, CQuickArray<BYTE> &rDef)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    MethodTable* pIntfMT = InterfaceType.GetMethodTable();
    PREFIX_ASSUME(pIntfMT != NULL);

    IMDInternalImport* pMDImport = pIntfMT->GetMDImport();
    PREFIX_ASSUME(pMDImport != NULL);
    
    LPCWSTR             szName;                 
    ULONG               cchName;
    MDEnumHolder        eMb(pMDImport);                         // For enumerating methods and fields.
    mdToken             tkMb;                                   // A method or field token.
    SIZE_T              cbCur;

    // Make sure the specified type is an interface with a valid token.
    _ASSERTE(!IsNilToken(pIntfMT->GetCl()) && pIntfMT->IsInterface());

    // Get the name of the class.
    DefineFullyQualifiedNameForClassW();
    szName = GetFullyQualifiedNameForClassNestedAwareW(pIntfMT);

    cchName = (ULONG)wcslen(szName);

    // Start with the interface name.
    cbCur = cchName * sizeof(WCHAR);
    rDef.ReSizeThrows(cbCur + sizeof(WCHAR));
    wcscpy_s(reinterpret_cast<LPWSTR>(rDef.Ptr()), rDef.Size()/sizeof(WCHAR), szName);

    // Enumerate the methods...
    IfFailThrow(pMDImport->EnumInit(mdtMethodDef, pIntfMT->GetCl(), &eMb));
    while(pMDImport->EnumNext(&eMb, &tkMb))
    {   // accumulate the signatures.
        cbCur = GetStringizedMethodDef(pIntfMT, tkMb, rDef, (ULONG)cbCur);
    }
    pMDImport->EnumClose(&eMb);

    // Enumerate the fields...
    IfFailThrow(pMDImport->EnumInit(mdtFieldDef, pIntfMT->GetCl(), &eMb));
    while(pMDImport->EnumNext(&eMb, &tkMb))
    {   // accumulate the signatures.
        cbCur = GetStringizedFieldDef(pIntfMT, tkMb, rDef, (ULONG)cbCur);
    }

    // Return the number of bytes.
    return cbCur;
} // ULONG GetStringizedItfDef()

//--------------------------------------------------------------------------------
// Helper to get the stringized form of typelib guid.
HRESULT GetStringizedTypeLibGuidForAssembly(Assembly *pAssembly, CQuickArray<BYTE> &rDef, ULONG cbCur, ULONG *pcbFetched)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS; 
        MODE_ANY;
        PRECONDITION(CheckPointer(pAssembly));
        PRECONDITION(CheckPointer(pcbFetched));        
    }
    CONTRACTL_END;

    HRESULT     hr = S_OK;              // A result.
    LPCUTF8     pszName = NULL;         // Library name in UTF8.
    ULONG       cbName;                 // Length of name, UTF8 characters.
    LPWSTR      pName;                  // Pointer to library name.
    ULONG       cchName;                // Length of name, wide chars.
    LPWSTR      pch=0;                  // Pointer into lib name.
    const void  *pSN=NULL;              // Pointer to public key.
    DWORD       cbSN=0;                 // Size of public key.
    USHORT      usMajorVersion;         // The major version number.
    USHORT      usMinorVersion;         // The minor version number.
    USHORT      usBuildNumber;          // The build number.
    USHORT      usRevisionNumber;       // The revision number.
    const BYTE  *pbData = NULL;         // Pointer to a custom attribute data.
    ULONG       cbData = 0;             // Size of custom attribute data.
    static char szTypeLibKeyName[] = {"TypeLib"};
 
    // Get the name, and determine its length.
    pszName = pAssembly->GetSimpleName();
    cbName=(ULONG)strlen(pszName);
    cchName = WszMultiByteToWideChar(CP_ACP,0, pszName,cbName+1, 0,0);
    
    // See if there is a public key.
    EX_TRY
    {
        pSN = pAssembly->GetPublicKey(&cbSN);
    }
    EX_CATCH
    {
        IfFailGo(COR_E_BADIMAGEFORMAT);
    }
    EX_END_CATCH(RethrowTerminalExceptions)
    

#ifdef FEATURE_COMINTEROP
    if (pAssembly->IsWinMD())
    {
        // ignore classic COM interop CA on .winmd
        hr = S_FALSE;
    }
    else
    {
        // If the ComCompatibleVersionAttribute is set, then use the version
        // number in the attribute when generating the GUID.
        IfFailGo(pAssembly->GetCustomAttribute(TokenFromRid(1, mdtAssembly), WellKnownAttribute::ComCompatibleVersion, (const void**)&pbData, &cbData));
    }

    if (hr == S_OK && cbData >= (2 + 4 * sizeof(INT32)))
    {
        CustomAttributeParser cap(pbData, cbData);
        IfFailRet(cap.SkipProlog());

        // Retrieve the major and minor version from the attribute.
        UINT32 u4;

        IfFailRet(cap.GetU4(&u4));
        usMajorVersion = GET_VERSION_USHORT_FROM_INT(u4);
        IfFailRet(cap.GetU4(&u4));
        usMinorVersion = GET_VERSION_USHORT_FROM_INT(u4);
        IfFailRet(cap.GetU4(&u4));
        usBuildNumber = GET_VERSION_USHORT_FROM_INT(u4);
        IfFailRet(cap.GetU4(&u4));
        usRevisionNumber = GET_VERSION_USHORT_FROM_INT(u4);
    }
    else
#endif // FEATURE_COMINTEROP
    {
        pAssembly->GetVersion(&usMajorVersion, &usMinorVersion, &usBuildNumber, &usRevisionNumber);
    }

    // Get the version information.
    struct  versioninfo
    {
        USHORT      usMajorVersion;         // Major Version.   
        USHORT      usMinorVersion;         // Minor Version.
        USHORT      usBuildNumber;          // Build Number.
        USHORT      usRevisionNumber;       // Revision Number.
    } ver;

    // <REVISIT_TODO> An issue here is that usMajor is used twice and usMinor not at all.
    //  We're not fixing that because everyone has a major version, so all the
    //  generated guids would change, which is breaking.  To compensate, if 
    //  the minor is non-zero, we add it separately, below.</REVISIT_TODO>
    ver.usMajorVersion = usMajorVersion;
    ver.usMinorVersion =  usMajorVersion;  // Don't fix this line!
    ver.usBuildNumber =  usBuildNumber;
    ver.usRevisionNumber =  usRevisionNumber;
    
    // Resize the output buffer.
    IfFailGo(rDef.ReSizeNoThrow(cbCur + cchName*sizeof(WCHAR) + sizeof(szTypeLibKeyName)-1 + cbSN + sizeof(ver)+sizeof(USHORT)));
                                                                                                          
    // Put it all together.  Name first.
    WszMultiByteToWideChar(CP_ACP,0, pszName,cbName+1, (LPWSTR)(&rDef[cbCur]),cchName);
    pName = (LPWSTR)(&rDef[cbCur]);
    for (pch=pName; *pch; ++pch)
        if (*pch == '.' || *pch == ' ')
            *pch = '_';
    else
        if (iswupper(*pch))
            *pch = towlower(*pch);
    cbCur += (cchName-1)*sizeof(WCHAR);
    memcpy(&rDef[cbCur], szTypeLibKeyName, sizeof(szTypeLibKeyName)-1);
    cbCur += sizeof(szTypeLibKeyName)-1;
        
    // Version.
    memcpy(&rDef[cbCur], &ver, sizeof(ver));
    cbCur += sizeof(ver);

    // If minor version is non-zero, add it to the hash.  It should have been in the ver struct,
    //  but due to a bug, it was omitted there, and fixing it "right" would have been
    //  breaking.  So if it isn't zero, add it; if it is zero, don't add it.  Any
    //  possible value of minor thus generates a different guid, and a value of 0 still generates
    //  the guid that the original, buggy, code generated.
    if (usMinorVersion != 0)
    {
        SET_UNALIGNED_16(&rDef[cbCur], usMinorVersion);
        cbCur += sizeof(USHORT);
    }

    // Public key.
    memcpy(&rDef[cbCur], pSN, cbSN);
    cbCur += cbSN;

    if (pcbFetched)
        *pcbFetched = cbCur;

ErrExit:
    return hr;
}

void SafeRelease_OnException(IUnknown* pUnk, RCW* pRCW)
{
    CONTRACTL
    {
        NOTHROW;
        MODE_ANY;
    }
    CONTRACTL_END;

#ifndef CROSSGEN_COMPILE
#ifdef FEATURE_COMINTEROP
    LogInterop(W("An exception occurred during release"));
    LogInteropLeak(pUnk);
#endif // FEATURE_COMINTEROP
#endif // CROSSGEN_COMPILE
}

#include <optsmallperfcritical.h>
//--------------------------------------------------------------------------------
// Release helper, must be called in preemptive mode.  Only use this variant if
// you already know you're in preemptive mode for other reasons.  
ULONG SafeReleasePreemp(IUnknown * pUnk, RCW * pRCW)
{
    CONTRACTL {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(pUnk, NULL_OK));
    } CONTRACTL_END;

    if (pUnk == NULL)
        return 0;

    ULONG res = 0;
    Thread * const pThread = GetThreadNULLOk();

    // Message pump could happen, so arbitrary managed code could run.
    CONTRACT_VIOLATION(ThrowsViolation | FaultViolation);

    bool fException = false;
    
    SCAN_EHMARKER();
    PAL_CPP_TRY
    {
        SCAN_EHMARKER_TRY();
        // This is a holder to tell the contract system that we're catching all exceptions.
        CLR_TRY_MARKER();

        // Its very possible that the punk has gone bad before we could release it. This is a common application
        // error. We may AV trying to call Release, and that AV will show up as an AV in mscorwks, so we'll take
        // down the Runtime. Mark that an AV is alright, and handled, in this scope using this holder.
        AVInRuntimeImplOkayHolder AVOkay(pThread);

        res = pUnk->Release();

        SCAN_EHMARKER_END_TRY();
    }
    PAL_CPP_CATCH_ALL
    {
        SCAN_EHMARKER_CATCH();
#if defined(STACK_GUARDS_DEBUG)
        // Catching and just swallowing an exception means we need to tell
        // the SO code that it should go back to normal operation, as it
        // currently thinks that the exception is still on the fly.
        pThread->GetCurrentStackGuard()->RestoreCurrentGuard();
#endif
        fException = true;
        SCAN_EHMARKER_END_CATCH();
    }
    PAL_CPP_ENDTRY;

    if (fException)
    {
        SafeRelease_OnException(pUnk, pRCW);
    }

    return res;
}

//--------------------------------------------------------------------------------
// Release helper, enables and disables GC during call-outs
ULONG SafeRelease(IUnknown* pUnk, RCW* pRCW)
{
    CONTRACTL {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pUnk, NULL_OK));
    } CONTRACTL_END;

    if (pUnk == NULL)
        return 0;

    ULONG res = 0;
    Thread * const pThread = GetThreadNULLOk();
    GCX_PREEMP_NO_DTOR_HAVE_THREAD(pThread);

    // Message pump could happen, so arbitrary managed code could run.
    CONTRACT_VIOLATION(ThrowsViolation | FaultViolation);

    bool fException = false;
    
    SCAN_EHMARKER();
    PAL_CPP_TRY
    {
        SCAN_EHMARKER_TRY();
        // This is a holder to tell the contract system that we're catching all exceptions.
        CLR_TRY_MARKER();

        // Its very possible that the punk has gone bad before we could release it. This is a common application
        // error. We may AV trying to call Release, and that AV will show up as an AV in mscorwks, so we'll take
        // down the Runtime. Mark that an AV is alright, and handled, in this scope using this holder.
        AVInRuntimeImplOkayHolder AVOkay(pThread);

        res = pUnk->Release();

        SCAN_EHMARKER_END_TRY();
    }
    PAL_CPP_CATCH_ALL
    {
        SCAN_EHMARKER_CATCH();
#if defined(STACK_GUARDS_DEBUG)
        // Catching and just swallowing an exception means we need to tell
        // the SO code that it should go back to normal operation, as it
        // currently thinks that the exception is still on the fly.
        pThread->GetCurrentStackGuard()->RestoreCurrentGuard();
#endif
        fException = true;
        SCAN_EHMARKER_END_CATCH();
    }
    PAL_CPP_ENDTRY;

    if (fException)
    {
        SafeRelease_OnException(pUnk, pRCW);
    }

    GCX_PREEMP_NO_DTOR_END();

    return res;
}

#include <optdefault.h>

//--------------------------------------------------------------------------------
// Determines if a COM object can be cast to the specified type.
BOOL CanCastComObject(OBJECTREF obj, MethodTable * pTargetMT)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;
    
    if (!obj)
        return TRUE;

    if (pTargetMT->IsInterface())
    {
        return Object::SupportsInterface(obj, pTargetMT);
    }
    else
    {
        return obj->GetMethodTable()->CanCastToClass(pTargetMT);
    }
}

// Returns TRUE iff the argument represents the "__ComObject" type or
// any type derived from it (i.e. typelib-imported RCWs).
BOOL IsComWrapperClass(TypeHandle type)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;
    
    MethodTable* pMT = type.GetMethodTable();
    if (pMT == NULL)
        return FALSE;
        
    return pMT->IsComObjectType();
}

// Returns TRUE iff the argument represents the "__ComObject" type.
BOOL IsComObjectClass(TypeHandle type)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

#ifdef FEATURE_COMINTEROP
    if (!type.IsTypeDesc())
    {
        MethodTable *pMT = type.AsMethodTable();

        if (pMT->IsComObjectType())
        {
            // May be __ComObject or typed RCW. __ComObject must have already been loaded
            // if we see an MT marked like this so calling the *NoInit method is sufficient.

            return pMT == g_pBaseCOMObject;
        }
    }
#endif

    return FALSE;
}

VOID
ReadBestFitCustomAttribute(MethodDesc* pMD, BOOL* BestFit, BOOL* ThrowOnUnmappableChar)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;
    
    ReadBestFitCustomAttribute(pMD->GetModule(),
        pMD->GetMethodTable()->GetCl(),
        BestFit, ThrowOnUnmappableChar);
}

VOID
ReadBestFitCustomAttribute(Module* pModule, mdTypeDef cl, BOOL* BestFit, BOOL* ThrowOnUnmappableChar)
{
    // Set the attributes to their defaults, just to be safe.
    *BestFit = TRUE;
    *ThrowOnUnmappableChar = FALSE;

    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(CheckPointer(pModule));
    }
    CONTRACTL_END;
    
    HRESULT     hr;
    BYTE*       pData;
    ULONG       cbCount; 

    // A well-formed BestFitMapping attribute will have at least 5 bytes
    // 1,2 for the prolog (should be 0x1, 0x0)
    // 3 for the BestFitMapping bool
    // 4,5 for the number of named parameters (will be 0 if ThrowOnUnmappableChar doesn't exist)
    // 6 - 29 for the description of ThrowOnUnmappableChar
    // 30 for the ThrowOnUnmappableChar bool

    // Try the assembly first
    hr = pModule->GetCustomAttribute(TokenFromRid(1, mdtAssembly), WellKnownAttribute::BestFitMapping, (const VOID**)(&pData), &cbCount);
    if ((hr == S_OK) && (pData) && (cbCount > 4) && (pData[0] == 1) && (pData[1] == 0))
    {
        _ASSERTE((cbCount == 5) || (cbCount == 30));
        
        // index to 2 to skip prolog
        *BestFit = pData[2] != 0;

        // If this parameter exists,
        if (cbCount == 30)
            // index to end of data to skip description of named argument
            *ThrowOnUnmappableChar = pData[29] != 0;
    }

    // Now try the interface/class/struct
    if (IsNilToken(cl))
        return;
    hr = pModule->GetCustomAttribute(cl, WellKnownAttribute::BestFitMapping, (const VOID**)(&pData), &cbCount);
    if ((hr == S_OK) && (pData) && (cbCount > 4) && (pData[0] == 1) && (pData[1] == 0))
    {
        _ASSERTE((cbCount == 5) || (cbCount == 30));

        // index to 2 to skip prolog    
        *BestFit = pData[2] != 0;
        
        // If this parameter exists,
        if (cbCount == 30)
            // index to end of data to skip description of named argument
            *ThrowOnUnmappableChar = pData[29] != 0;
    }
}


int InternalWideToAnsi(__in_ecount(iNumWideChars) LPCWSTR szWideString, int iNumWideChars, __out_ecount_opt(cbAnsiBufferSize) LPSTR szAnsiString, int cbAnsiBufferSize, BOOL fBestFit, BOOL fThrowOnUnmappableChar)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;


    if ((szWideString == 0) || (iNumWideChars == 0) || (szAnsiString == 0) || (cbAnsiBufferSize == 0))
        return 0;

    DWORD flags = 0;
    int retval;

    if (fBestFit == FALSE)
        flags = WC_NO_BEST_FIT_CHARS;

    if (fThrowOnUnmappableChar)
    {
        BOOL DefaultCharUsed = FALSE;
        retval = WszWideCharToMultiByte(CP_ACP,
                                    flags,
                                    szWideString,
                                    iNumWideChars,
                                    szAnsiString,
                                    cbAnsiBufferSize,
                                    NULL,
                                    &DefaultCharUsed);
        DWORD lastError = GetLastError();

        if (retval == 0)
        {
            INSTALL_UNWIND_AND_CONTINUE_HANDLER; 
            COMPlusThrowHR(HRESULT_FROM_WIN32(lastError));
            UNINSTALL_UNWIND_AND_CONTINUE_HANDLER; 
        }

        if (DefaultCharUsed)
        {
            struct HelperThrow
            {
                static void Throw()
                {
                    COMPlusThrow( kArgumentException, IDS_EE_MARSHAL_UNMAPPABLE_CHAR );
                }
            };
            
            ENCLOSE_IN_EXCEPTION_HANDLER( HelperThrow::Throw );
        }

    }
    else
    {
        retval = WszWideCharToMultiByte(CP_ACP,
                                    flags,
                                    szWideString,
                                    iNumWideChars,
                                    szAnsiString,
                                    cbAnsiBufferSize,
                                    NULL,
                                    NULL);
        DWORD lastError = GetLastError();

        if (retval == 0)
        {
            INSTALL_UNWIND_AND_CONTINUE_HANDLER; 
            COMPlusThrowHR(HRESULT_FROM_WIN32(lastError));
            UNINSTALL_UNWIND_AND_CONTINUE_HANDLER; 
        }
    }

    return retval;
}

namespace
{
    HRESULT TryParseClassInterfaceAttribute(
        _In_ Module *pModule,
        _In_ mdToken tkObj,
        _Out_ CorClassIfaceAttr *val)
    {
        CONTRACTL
        {
            NOTHROW;
            GC_TRIGGERS;
            MODE_ANY;
            PRECONDITION(CheckPointer(pModule));
            PRECONDITION(CheckPointer(val));
        }
        CONTRACTL_END

        const BYTE *pVal = nullptr;
        ULONG cbVal = 0;
        HRESULT hr = pModule->GetCustomAttribute(tkObj, WellKnownAttribute::ClassInterface, (const void**)&pVal, &cbVal);
        if (hr != S_OK)
        {
            *val = clsIfNone;
            return S_FALSE;
        }

        CustomAttributeParser cap(pVal, cbVal);
        if (FAILED(cap.ValidateProlog()))
            return COR_E_BADIMAGEFORMAT;

        U1 u1;
        if (FAILED(cap.GetU1(&u1)))
            return COR_E_BADIMAGEFORMAT;

        *val = (CorClassIfaceAttr)(u1);
        _ASSERTE(*val < clsIfLast);

        return S_OK;
    }
}

//---------------------------------------------------------
// Read the ClassInterfaceType custom attribute info from 
// both assembly level and class level
//---------------------------------------------------------
CorClassIfaceAttr ReadClassInterfaceTypeCustomAttribute(TypeHandle type)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(!type.IsInterface());
    }
    CONTRACTL_END

    // Ignore classic COM interop CA on WinRT types
    if (!type.GetMethodTable()->IsWinRTObjectType() && !type.GetMethodTable()->IsExportedToWinRT())
    {
        CorClassIfaceAttr attrValueMaybe;

        // First look for the class interface attribute at the class level.
        HRESULT hr = TryParseClassInterfaceAttribute(type.GetModule(), type.GetCl(), &attrValueMaybe);
        if (FAILED(hr))
            ThrowHR(hr, BFA_BAD_CLASS_INT_CA_FORMAT);

        if (hr == S_FALSE)
        {
            // Check the class interface attribute at the assembly level.
            Assembly *pAssembly = type.GetAssembly();
            hr = TryParseClassInterfaceAttribute(pAssembly->GetManifestModule(), pAssembly->GetManifestToken(), &attrValueMaybe);
            if (FAILED(hr))
                ThrowHR(hr, BFA_BAD_CLASS_INT_CA_FORMAT);
        }

        if (hr == S_OK)
            return attrValueMaybe;
    }

    return DEFAULT_CLASS_INTERFACE_TYPE;
}

//--------------------------------------------------------------------------------
// GetErrorInfo helper, enables and disables GC during call-outs
HRESULT SafeGetErrorInfo(IErrorInfo **ppIErrInfo)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(ppIErrInfo));   
    }
    CONTRACTL_END;

    *ppIErrInfo = NULL;

#ifdef FEATURE_COMINTEROP
    GCX_PREEMP();

    HRESULT hr = S_OK;
    EX_TRY
    {
        hr = GetErrorInfo(0, ppIErrInfo);
    }
    EX_CATCH
    {
        hr = E_OUTOFMEMORY;
    }
    EX_END_CATCH(SwallowAllExceptions);
    
    return hr;
#else // FEATURE_COMINTEROP
    // Indicate no error object
    return S_FALSE;
#endif
}


#include <optsmallperfcritical.h>
//--------------------------------------------------------------------------------
// QI helper, enables and disables GC during call-outs
HRESULT SafeQueryInterface(IUnknown* pUnk, REFIID riid, IUnknown** pResUnk)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_TRIGGERS;
    STATIC_CONTRACT_MODE_ANY;
    _ASSERTE(pUnk);
    _ASSERTE(pResUnk);

    Thread * const pThread = GetThreadNULLOk();

    *pResUnk = NULL;
    HRESULT hr = E_FAIL;

    GCX_PREEMP_NO_DTOR_HAVE_THREAD(pThread);

    BEGIN_CONTRACT_VIOLATION(ThrowsViolation); // message pump could happen, so arbitrary managed code could run

    struct Param { HRESULT * const hr; IUnknown** const pUnk; REFIID riid; IUnknown*** const pResUnk; } param = { &hr, &pUnk, riid, &pResUnk };
#define PAL_TRY_ARG(argName) (*(pParam->argName))
#define PAL_TRY_REFARG(argName) (pParam->argName)
    PAL_TRY(Param * const, pParam, &param)
    {
        PAL_TRY_ARG(hr) = PAL_TRY_ARG(pUnk)->QueryInterface(PAL_TRY_REFARG(riid), (void**) PAL_TRY_ARG(pResUnk));
    }
    PAL_EXCEPT(EXCEPTION_EXECUTE_HANDLER)
    {
#if defined(STACK_GUARDS_DEBUG)
        // Catching and just swallowing an exception means we need to tell
        // the SO code that it should go back to normal operation, as it
        // currently thinks that the exception is still on the fly.
        GetThread()->GetCurrentStackGuard()->RestoreCurrentGuard();
#endif
    }
    PAL_ENDTRY;
#undef PAL_TRY_ARG
#undef PAL_TRY_REFARG

    END_CONTRACT_VIOLATION;

    LOG((LF_INTEROP, LL_EVERYTHING, hr == S_OK ? "QI Succeeded\n" : "QI Failed\n")); 

    // Ensure if the QI returned ok that it actually set a pointer.
    if (hr == S_OK)
    {
        if (*pResUnk == NULL)
            hr = E_NOINTERFACE;
    }

    GCX_PREEMP_NO_DTOR_END();

    return hr;
}


//--------------------------------------------------------------------------------
// QI helper, must be called in preemptive mode.  Faster than the MODE_ANY version 
// because it doesn't need to toggle the mode.  Use this version only if you already
// know that you're in preemptive mode for other reasons.
HRESULT SafeQueryInterfacePreemp(IUnknown* pUnk, REFIID riid, IUnknown** pResUnk)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_TRIGGERS;
    STATIC_CONTRACT_MODE_PREEMPTIVE;
    _ASSERTE(pUnk);
    _ASSERTE(pResUnk);

    Thread * const pThread = GetThreadNULLOk();

    *pResUnk = NULL;
    HRESULT hr = E_FAIL;

    BEGIN_CONTRACT_VIOLATION(ThrowsViolation); // message pump could happen, so arbitrary managed code could run

    struct Param { HRESULT * const hr; IUnknown** const pUnk; REFIID riid; IUnknown*** const pResUnk; } param = { &hr, &pUnk, riid, &pResUnk };
#define PAL_TRY_ARG(argName) (*(pParam->argName))
#define PAL_TRY_REFARG(argName) (pParam->argName)
    PAL_TRY(Param * const, pParam, &param)
    {
        PAL_TRY_ARG(hr) = PAL_TRY_ARG(pUnk)->QueryInterface(PAL_TRY_REFARG(riid), (void**) PAL_TRY_ARG(pResUnk));
    }
    PAL_EXCEPT(EXCEPTION_EXECUTE_HANDLER)
    {
#if defined(STACK_GUARDS_DEBUG)
        // Catching and just swallowing an exception means we need to tell
        // the SO code that it should go back to normal operation, as it
        // currently thinks that the exception is still on the fly.
        GetThread()->GetCurrentStackGuard()->RestoreCurrentGuard();
#endif
    }
    PAL_ENDTRY;
#undef PAL_TRY_ARG
#undef PAL_TRY_REFARG

    END_CONTRACT_VIOLATION;

    LOG((LF_INTEROP, LL_EVERYTHING, hr == S_OK ? "QI Succeeded\n" : "QI Failed\n")); 

    // Ensure if the QI returned ok that it actually set a pointer.
    if (hr == S_OK)
    {
        if (*pResUnk == NULL)
            hr = E_NOINTERFACE;
    }

    return hr;
}
#include <optdefault.h>

#ifdef FEATURE_COMINTEROP

#ifndef CROSSGEN_COMPILE

//--------------------------------------------------------------------------------
// Cleanup helpers
//--------------------------------------------------------------------------------
void MinorCleanupSyncBlockComData(InteropSyncBlockInfo* pInteropInfo)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION( GCHeapUtilities::IsGCInProgress() || ( (g_fEEShutDown & ShutDown_SyncBlock) && g_fProcessDetach ) );
    }
    CONTRACTL_END;

    // No need to notify the thread that the RCW is in use here.
    // This is a privileged function called during GC or shutdown.
    RCW* pRCW = pInteropInfo->GetRawRCW();
    if (pRCW)
        pRCW->MinorCleanup();
}

void CleanupSyncBlockComData(InteropSyncBlockInfo* pInteropInfo)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;
    
    if ((g_fEEShutDown & ShutDown_SyncBlock) && g_fProcessDetach )
        MinorCleanupSyncBlockComData(pInteropInfo);

#ifdef FEATURE_COMINTEROP_UNMANAGED_ACTIVATION
    ComClassFactory* pComClassFactory = pInteropInfo->GetComClassFactory();
    if (pComClassFactory)
    {
        delete pComClassFactory;
        pInteropInfo->SetComClassFactory(NULL);
    }
#endif // FEATURE_COMINTEROP_UNMANAGED_ACTIVATION

    // No need to notify the thread that the RCW is in use here.
    // This is only called during finalization of a __ComObject so no one
    // else could have a reference to this object.
    RCW* pRCW = pInteropInfo->GetRawRCW();
    if (pRCW)
    {
        pInteropInfo->SetRawRCW(NULL);
        pRCW->Cleanup();
    }

    ComCallWrapper* pCCW = pInteropInfo->GetCCW();
    if (pCCW)
    {
        pInteropInfo->SetCCW(NULL);
        pCCW->Cleanup();
    }
}

void ReleaseRCWsInCachesNoThrow(LPVOID pCtxCookie)
{
    CONTRACTL
    {
        DISABLED(NOTHROW);
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pCtxCookie, NULL_OK));
    }
    CONTRACTL_END;

    EX_TRY
    {
        ReleaseRCWsInCaches(pCtxCookie);
    }
    EX_CATCH
    {
    }
    EX_END_CATCH(SwallowAllExceptions);
}

//--------------------------------------------------------------------------------
//  Helper to release all of the RCWs in the specified context across all caches.
//  If pCtxCookie is NULL, release all RCWs
void ReleaseRCWsInCaches(LPVOID pCtxCookie)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pCtxCookie, NULL_OK));
    }
    CONTRACTL_END;
    
    // Go through all the app domains and for each one release all the 
    // RCW's that live in the current context.
    AppDomainIterator i(TRUE);
    while (i.Next())
        i.GetDomain()->ReleaseRCWs(pCtxCookie);

    if (!g_fEEShutDown)
    {
        GCX_COOP();            

        // If the finalizer thread has sync blocks to clean up or if it is in the process
        // of cleaning up the sync blocks, we need to wait for it to finish.
        if (FinalizerThread::GetFinalizerThread()->RequireSyncBlockCleanup() || SyncBlockCache::GetSyncBlockCache()->IsSyncBlockCleanupInProgress())
            FinalizerThread::FinalizerThreadWait();

        // If more sync blocks were added while the finalizer thread was calling the finalizers
        // or while it was transitioning into a context to clean up the IP's, we need to wake
        // it up again to have it clean up the newly added sync blocks.
        if (FinalizerThread::GetFinalizerThread()->RequireSyncBlockCleanup() || SyncBlockCache::GetSyncBlockCache()->IsSyncBlockCleanupInProgress())
            FinalizerThread::FinalizerThreadWait();
    }
}

//--------------------------------------------------------------------------------
// Marshalling Helpers
//--------------------------------------------------------------------------------


// Convert an IUnknown to CCW, returns NULL if the pUnk is not on
// a managed tear-off (OR) if the pUnk is to a managed tear-off that
// has been aggregated
ComCallWrapper* GetCCWFromIUnknown(IUnknown* pUnk, BOOL bEnableCustomization)
{
    CONTRACT (ComCallWrapper*)
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pUnk));
        POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
    }
    CONTRACT_END;
    
    ComCallWrapper* pWrap = MapIUnknownToWrapper(pUnk);
    if (pWrap != NULL)
    {
        // check if this wrapper is aggregated
        if (pWrap->GetOuter() != NULL)
        {
            pWrap = NULL;
        }
    }
    
    RETURN pWrap;
}

HRESULT LoadRegTypeLib(_In_ REFGUID guid,
                       _In_ unsigned short wVerMajor,
                       _In_ unsigned short wVerMinor,
                       _Outptr_ ITypeLib **pptlib)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    *pptlib = NULL;

    GCX_PREEMP();

    BSTRHolder wzPath;
    HRESULT hr = S_OK;

    EX_TRY
    {
        hr = QueryPathOfRegTypeLib(guid, wVerMajor, wVerMinor, LOCALE_USER_DEFAULT, &wzPath);
        if (SUCCEEDED(hr))
        {
#ifdef _WIN64
            REGKIND rk = (REGKIND)(REGKIND_NONE | LOAD_TLB_AS_64BIT);
#else
            REGKIND rk = (REGKIND)(REGKIND_NONE | LOAD_TLB_AS_32BIT);
#endif // _WIN64
            hr = LoadTypeLibEx(wzPath, rk, pptlib);
        }
    }
    EX_CATCH
    {
        hr = GET_EXCEPTION()->GetHR();
    }
    EX_END_CATCH(SwallowAllExceptions);

    return hr;
}

VOID EnsureComStarted(BOOL fCoInitCurrentThread)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(GetThread() || !fCoInitCurrentThread);
        PRECONDITION(g_fEEStarted);
    }
    CONTRACTL_END;

    if (g_fComStarted == FALSE)
    {
        FinalizerThread::GetFinalizerThread()->SetRequiresCoInitialize();

        // Attempt to set the thread's apartment model (to MTA by default). May not
        // succeed (if someone beat us to the punch). That doesn't matter (since
        // COM+ objects are now apartment agile), we only care that a CoInitializeEx
        // has been performed on this thread by us.
        if (fCoInitCurrentThread)
            GetThread()->SetApartment(Thread::AS_InMTA, FALSE);

        // set the finalizer event
        FinalizerThread::EnableFinalization();

        g_fComStarted = TRUE;
    }
}

HRESULT EnsureComStartedNoThrow(BOOL fCoInitCurrentThread)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(g_fEEStarted);
        PRECONDITION(GetThread() != NULL);      // Should always be inside BEGIN_EXTERNAL_ENTRYPOINT
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;

    if (!g_fComStarted)
    {
        GCX_COOP();
        EX_TRY
        {
            EnsureComStarted(fCoInitCurrentThread);
        }
        EX_CATCH_HRESULT(hr);
    }

    return hr;
}

//--------------------------------------------------------------------------------
// BOOL ExtendsComImport(MethodTable* pMT);
// check if the class is OR extends a COM Imported class
BOOL ExtendsComImport(MethodTable* pMT)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(CheckPointer(pMT));
    }
    CONTRACTL_END;
    
    while (pMT != NULL && !pMT->IsComImport())
    {
        pMT = pMT->GetParentMethodTable();
    }
    return pMT != NULL;
}

#ifdef FEATURE_CLASSIC_COMINTEROP
//--------------------------------------------------------------------------------
// Gets the CLSID from the specified Prog ID.

HRESULT GetCLSIDFromProgID(__in_z WCHAR *strProgId, GUID *pGuid)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    }
    CONTRACTL_END;
    
    HRESULT     hr = S_OK;

#ifdef FEATURE_CORESYSTEM
    return CLSIDFromProgID(strProgId, pGuid);
#else
    return CLSIDFromProgIDEx(strProgId, pGuid);
#endif
}
#endif // FEATURE_CLASSIC_COMINTEROP

#include <optsmallperfcritical.h>
//--------------------------------------------------------------------------------
// AddRef helper, enables and disables GC during call-outs
ULONG SafeAddRef(IUnknown* pUnk)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;
    
    ULONG res = ~0;
    if (pUnk == NULL)
        return res;

    GCX_PREEMP_NO_DTOR();

    // @TODO: Consider special-casing this when we know it's one of ours so
    //        that we can avoid having to 'leave' and then 'enter'.

    CONTRACT_VIOLATION(ThrowsViolation); // arbitrary managed code could run

    res = pUnk->AddRef();

    GCX_PREEMP_NO_DTOR_END();

    return res;
}

//--------------------------------------------------------------------------------
// AddRef helper, must be called in preemptive mode.  Only use this variant if
// you already know you're in preemptive mode for other reasons.  
ULONG SafeAddRefPreemp(IUnknown* pUnk)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    }
    CONTRACTL_END;
    
    ULONG res = ~0;
    if (pUnk == NULL)
        return res;

    // @TODO: Consider special-casing this when we know it's one of ours so
    //        that we can avoid having to 'leave' and then 'enter'.

    CONTRACT_VIOLATION(ThrowsViolation); // arbitrary managed code could run

    res = pUnk->AddRef();

    return res;
}
#include <optdefault.h>

//--------------------------------------------------------------------------------
// Ole RPC seems to return an inconsistent SafeArray for arrays created with
// SafeArrayVector(VT_BSTR). OleAut's SafeArrayGetVartype() doesn't notice
// the inconsistency and returns a valid-seeming (but wrong vartype.)
// Our version is more discriminating. This should only be used for
// marshaling scenarios where we can assume unmanaged code permissions
// (and hence are already in a position of trusting unmanaged data.)

HRESULT ClrSafeArrayGetVartype(_In_ SAFEARRAY *psa, _Out_ VARTYPE *pvt)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(CheckPointer(psa));
        PRECONDITION(CheckPointer(pvt));
    }
    CONTRACTL_END;

    if (pvt == NULL || psa == NULL)
    {
       // This is the HRESULT returned by OLEAUT if either of the args are null.
       return E_INVALIDARG;
    }
    
    USHORT fFeatures = psa->fFeatures;
    USHORT hardwiredType = (fFeatures & (FADF_BSTR|FADF_UNKNOWN|FADF_DISPATCH|FADF_VARIANT));
    
    if (hardwiredType == FADF_BSTR && psa->cbElements == sizeof(BSTR))
    {
        *pvt = VT_BSTR;
        return S_OK;
    }
    else if (hardwiredType == FADF_UNKNOWN && psa->cbElements == sizeof(IUnknown*))
    {
        *pvt = VT_UNKNOWN;
        return S_OK;
    }
    else if (hardwiredType == FADF_DISPATCH && psa->cbElements == sizeof(IDispatch*))
    {
        *pvt = VT_DISPATCH;
        return S_OK;
    }
    else if (hardwiredType == FADF_VARIANT && psa->cbElements == sizeof(VARIANT))
    {
        *pvt = VT_VARIANT;
        return S_OK;
    }
    else
    {
        _ASSERTE(GetModuleHandleA("oleaut32.dll") != NULL);
        // We have got a SAFEARRAY.  Oleaut32.dll should have been loaded.
        CONTRACT_VIOLATION(ThrowsViolation);
        return ::SafeArrayGetVartype(psa, pvt);
    }
}

//--------------------------------------------------------------------------------
// // safe VariantChangeType
// Release helper, enables and disables GC during call-outs
HRESULT SafeVariantChangeType(_Inout_ VARIANT* pVarRes, _In_ VARIANT* pVarSrc,
                              unsigned short wFlags, VARTYPE vt)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pVarRes));
        PRECONDITION(CheckPointer(pVarSrc));        
    }
    CONTRACTL_END;
    
    HRESULT hr = S_OK;
    if (pVarRes)
    {
        GCX_PREEMP();
        EX_TRY
        {
            hr = VariantChangeType(pVarRes, pVarSrc, wFlags, vt);
        }
        EX_CATCH
        {
            hr = GET_EXCEPTION()->GetHR();
        }
        EX_END_CATCH(SwallowAllExceptions);
    }

    return hr;
}

//--------------------------------------------------------------------------------
HRESULT SafeVariantChangeTypeEx(_Inout_ VARIANT* pVarRes, _In_ VARIANT* pVarSrc,
                          LCID lcid, unsigned short wFlags, VARTYPE vt)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pVarRes));
        PRECONDITION(CheckPointer(pVarSrc));     
    }
    CONTRACTL_END;
    
    GCX_PREEMP();
    _ASSERTE(GetModuleHandleA("oleaut32.dll") != NULL);
    CONTRACT_VIOLATION(ThrowsViolation);

    HRESULT hr = VariantChangeTypeEx (pVarRes, pVarSrc,lcid,wFlags,vt);
    
    return hr;
}

//--------------------------------------------------------------------------------
void SafeVariantInit(VARIANT* pVar)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pVar));
    }
    CONTRACTL_END;

    // From the oa sources
    V_VT(pVar) = VT_EMPTY;
}

//--------------------------------------------------------------------------------
// void SafeReleaseStream(IStream *pStream)
void SafeReleaseStream(IStream *pStream)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pStream));   
    }
    CONTRACTL_END;

    GCX_PREEMP();

    {
        HRESULT hr = CoReleaseMarshalData(pStream);
    
#ifdef _DEBUG          
        wchar_t      logStr[200];
        swprintf_s(logStr, NumItems(logStr), W("Object gone: CoReleaseMarshalData returned %x, file %S, line %d\n"), hr, __FILE__, __LINE__);
        LogInterop(logStr);
        if (hr != S_OK)
        {
            // Reset the stream to the begining
            LARGE_INTEGER li;
            LISet32(li, 0);
            ULARGE_INTEGER li2;
            pStream->Seek(li, STREAM_SEEK_SET, &li2);
            hr = CoReleaseMarshalData(pStream);
            swprintf_s(logStr, NumItems(logStr), W("Object gone: CoReleaseMarshalData returned %x, file %S, line %d\n"), hr, __FILE__, __LINE__);
            LogInterop(logStr);
        }
#endif
    }

    ULONG cbRef = SafeReleasePreemp(pStream);
    LogInteropRelease(pStream, cbRef, "Release marshal Stream");
}

//---------------------------------------------------------------------------
//  is the iid represent an IClassX for this class
BOOL IsIClassX(MethodTable *pMT, REFIID riid, ComMethodTable **ppComMT)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pMT));
        PRECONDITION(CheckPointer(ppComMT));
    }
    CONTRACTL_END;

    // Walk up the hierarchy starting at the specified method table and compare
    // the IID's of the IClassX's against the specified IID.
    while (pMT != NULL)
    {
        ComCallWrapperTemplate *pTemplate = ComCallWrapperTemplate::GetTemplate(pMT);
        if (pTemplate->SupportsIClassX())
        {
            ComMethodTable *pComMT =
                ComCallWrapperTemplate::SetupComMethodTableForClass(pMT, FALSE);
            _ASSERTE(pComMT);

            if (IsEqualIID(riid, pComMT->GetIID()))
            {
                *ppComMT = pComMT;
                return TRUE;
            }
        }

        pMT = pMT->GetComPlusParentMethodTable();
    }

    return FALSE;
}

#endif //#ifndef CROSSGEN_COMPILE


//---------------------------------------------------------------------------
// Returns TRUE if we support IClassX (the auto-generated class interface)
// for the given class.
BOOL ClassSupportsIClassX(MethodTable *pMT)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    // WinRT delegates use IClassX
    if (pMT->IsWinRTDelegate())
        return TRUE;

    if (pMT->IsWinRTObjectType() || pMT->IsExportedToWinRT())
    {
        // Other than that WinRT does not need IClassX so the goal is to return FALSE for
        // anything that is guaranteed to not be a legacy classic COM interop scenario
        return FALSE;
    }

    // If the class is decorated with an explicit ClassInterfaceAttribute, we're going to say yes.
    if (S_OK == pMT->GetCustomAttribute(WellKnownAttribute::ClassInterface, NULL, NULL))
        return TRUE;

    MethodTable::InterfaceMapIterator it = pMT->IterateInterfaceMap();
    while (it.Next())
    {
        MethodTable *pItfMT = it.GetInterfaceInfo()->GetApproxMethodTable(pMT->GetLoaderModule());
        if (pItfMT->IsProjectedFromWinRT())
            return FALSE;
    }

    return TRUE;
}


#ifndef CROSSGEN_COMPILE

#ifdef FEATURE_COMINTEROP_UNMANAGED_ACTIVATION
//---------------------------------------------------------------------------
// OBJECTREF AllocateComObject_ForManaged(MethodTable* pMT)
OBJECTREF AllocateComObject_ForManaged(MethodTable* pMT)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pMT));
        PRECONDITION(pMT->IsComObjectType());
        PRECONDITION(!pMT->IsProjectedFromWinRT());
    }
    CONTRACTL_END;

    // Calls to COM up ahead.
    HRESULT hr = S_OK;
    EnsureComStarted();

    ComClassFactory *pComClsFac = (ComClassFactory *)GetComClassFactory(pMT);
    return pComClsFac->CreateInstance(pMT, TRUE);
}
#endif // FEATURE_COMINTEROP_UNMANAGED_ACTIVATION

#ifdef FEATURE_CLASSIC_COMINTEROP

//---------------------------------------------------------------------------
//  get/load type for a given clsid
MethodTable* GetTypeForCLSID(REFCLSID rclsid, BOOL* pfAssemblyInReg)
{
    CONTRACT (MethodTable*)
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
    }
    CONTRACT_END;
   
    AppDomain* pDomain = GetAppDomain();
    _ASSERTE(pDomain);

    // check to see if we have this class cached
    MethodTable *pMT= pDomain->LookupClass(rclsid);
    if (pMT == NULL)
    {
        pMT = pDomain->LoadCOMClass(rclsid, FALSE, pfAssemblyInReg);        
        if (pMT != NULL)
            pDomain->InsertClassForCLSID(pMT, TRUE);            
    }
    RETURN pMT;
}


//---------------------------------------------------------------------------
//  get/load a value class for a given guid
MethodTable* GetValueTypeForGUID(REFCLSID guid)
{
    CONTRACT (MethodTable*)
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
    }
    CONTRACT_END;

    AppDomain* pDomain = GetAppDomain();
    _ASSERTE(pDomain);

    // Check to see if we have this value class cached
    MethodTable *pMT = pDomain->LookupClass(guid);
    if (pMT == NULL)
        pMT = pDomain->LoadCOMClass(guid, TRUE, NULL);        

    if (pMT)
    {
        // Make sure the class is a value class.
        if (!pMT->IsValueType())
        {
            DefineFullyQualifiedNameForClassW();
            COMPlusThrow(kArgumentException, IDS_EE_GUID_REPRESENTS_NON_VC,
                         GetFullyQualifiedNameForClassNestedAwareW(pMT));
        }

        // Insert the type in our map from CLSID to method table.
        pDomain->InsertClassForCLSID(pMT, TRUE);            
    }

    RETURN pMT;
}

#endif // FEATURE_CLASSIC_COMINTEROP

#endif //#ifndef CROSSGEN_COMPILE


//---------------------------------------------------------------------------
// This method returns the default interface for the class.
DefaultInterfaceType GetDefaultInterfaceForClassInternal(TypeHandle hndClass, TypeHandle *pHndDefClass)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM());
        PRECONDITION(!hndClass.IsNull());
        PRECONDITION(CheckPointer(pHndDefClass));
        PRECONDITION(!hndClass.GetMethodTable()->IsInterface());
    }
    CONTRACTL_END;

    // Set ppDefComMT to NULL before we start.
    *pHndDefClass = TypeHandle();

    HRESULT      hr       = S_FALSE;
    MethodTable* pClassMT = hndClass.GetMethodTable();
    const void*         pvData;
    ULONG               cbData;
    CorClassIfaceAttr   ClassItfType;
    BOOL                bComVisible;

    PREFIX_ASSUME(pClassMT != NULL);

    if (pClassMT->IsWinRTObjectType() || pClassMT->IsExportedToWinRT())
    {
        // there's no point executing the rest of the function for WinRT classes
        return DefaultInterfaceType_IUnknown;
    }

    if (pClassMT->IsComImport())
    {
        ClassItfType = clsIfNone;
        bComVisible = TRUE;
    }
    else
    {
        ClassItfType = pClassMT->GetComClassInterfaceType();
        bComVisible = IsTypeVisibleFromCom(hndClass);
    }

    // If the class is not COM visible, then its default interface is IUnknown.
    if (!bComVisible)
        return DefaultInterfaceType_IUnknown;
    
    // Start by checking for the ComDefaultInterface attribute.
    hr = pClassMT->GetCustomAttribute(WellKnownAttribute::ComDefaultInterface, &pvData, &cbData);
    IfFailThrow(hr);
    if (hr == S_OK && cbData > 2)
    {
        TypeHandle DefItfType;
        AppDomain *pCurrDomain = SystemDomain::GetCurrentDomain();

        CustomAttributeParser cap(pvData, cbData);
        IfFailThrow(cap.SkipProlog());

        LPCUTF8 szStr;
        ULONG   cbStr;
        IfFailThrow(cap.GetNonNullString(&szStr, &cbStr));
        
        // Allocate a new buffer that will contain the name of the default COM interface.
        StackSString defItf(SString::Utf8, szStr, cbStr);

        // Load the default COM interface specified in the CA.
        {
            GCX_COOP();
            
            DefItfType = TypeName::GetTypeUsingCASearchRules(defItf.GetUnicode(), pClassMT->GetAssembly());

            // If the type handle isn't a named type, then throw an exception using
            // the name of the type obtained from pCurrInterfaces.
            if (!DefItfType.GetMethodTable())
            {
                // This should only occur for TypeDesc's.
                StackSString ssClassName;
                DefineFullyQualifiedNameForClassW()
                COMPlusThrow(kTypeLoadException, IDS_EE_INVALIDCOMDEFITF,
                             GetFullyQualifiedNameForClassW(pClassMT),
                             defItf.GetUnicode());
            }

            // Otherwise, if the type is not an interface thrown an exception using the actual
            // name of the type.
            if (!DefItfType.IsInterface())
            {
                StackSString ssClassName;
                StackSString ssInvalidItfName;
                pClassMT->_GetFullyQualifiedNameForClass(ssClassName);
                DefItfType.GetMethodTable()->_GetFullyQualifiedNameForClass(ssInvalidItfName);
                COMPlusThrow(kTypeLoadException, IDS_EE_INVALIDCOMDEFITF,
                             ssClassName.GetUnicode(), ssInvalidItfName.GetUnicode());
            }

            // Make sure the class implements the interface.
            if (!pClassMT->CanCastToNonVariantInterface(DefItfType.GetMethodTable()))
            {
                StackSString ssClassName;
                StackSString ssInvalidItfName;
                pClassMT->_GetFullyQualifiedNameForClass(ssClassName);
                DefItfType.GetMethodTable()->_GetFullyQualifiedNameForClass(ssInvalidItfName);
                COMPlusThrow(kTypeLoadException, IDS_EE_COMDEFITFNOTSUPPORTED,
                             ssClassName.GetUnicode(), ssInvalidItfName.GetUnicode());
            }
        }

        // The default interface is valid so return it.
        *pHndDefClass = DefItfType;
        return DefaultInterfaceType_Explicit;
    }

    // If the class's interface type is AutoDispatch or AutoDual then return either the 
    // IClassX for the current class or IDispatch.
    if (ClassItfType != clsIfNone)
    {
        *pHndDefClass = hndClass;
        return ClassItfType == clsIfAutoDisp ? DefaultInterfaceType_AutoDispatch : DefaultInterfaceType_AutoDual;
    }

    // The class interface is set to NONE for this level of the hierarchy. So we need to check
    // to see if this class implements an interface.

    // Search for the first COM visible implemented interface. We start with the most
    // derived class and work our way up the hierarchy.    
    for (MethodTable *pParentMT = pClassMT->GetParentMethodTable(); pParentMT; pParentMT = pParentMT->GetParentMethodTable())
    {
        MethodTable::InterfaceMapIterator it = pClassMT->IterateInterfaceMap();
        while (it.Next())
        {
            MethodTable *pItfMT = it.GetInterfaceInfo()->GetApproxMethodTable(pClassMT->GetLoaderModule());
        
            // Skip generic interfaces. Classic COM interop does not support these and we don't
            // use the result of this function in WinRT scenarios. WinRT parameter marshaling
            // doesn't come here at all because the default interface is always specified using
            // the DefaultAttribute. Field marshaling does come here but WinRT does not support
            // fields of reference types other than string.
            if (!pItfMT->HasInstantiation())
            {
                // If the interface is visible from COM and not implemented by our parent, 
                // then use it as the default.
                if (IsTypeVisibleFromCom(TypeHandle(pItfMT)) && !pParentMT->ImplementsInterface(pItfMT))
                {
                    *pHndDefClass = TypeHandle(pItfMT);
                    return DefaultInterfaceType_Explicit;
                }
            }
        }
    }

    // If the class is a COM import with no interfaces, then its default interface will
    // be IUnknown.
    if (pClassMT->IsComImport())
        return DefaultInterfaceType_IUnknown;

    // If we have a managed parent class then return its default interface.
    MethodTable *pParentClass = pClassMT->GetComPlusParentMethodTable();
    if (pParentClass)
        return GetDefaultInterfaceForClassWrapper(TypeHandle(pParentClass), pHndDefClass);

    // Check to see if the class is an extensible RCW.
    if (pClassMT->IsComObjectType())
        return DefaultInterfaceType_BaseComClass;

    // The class has no interfaces and is marked as ClassInterfaceType.None.
    return DefaultInterfaceType_IUnknown;
}


DefaultInterfaceType GetDefaultInterfaceForClassWrapper(TypeHandle hndClass, TypeHandle *pHndDefClass)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(!hndClass.IsNull());
    }
    CONTRACTL_END;

#ifndef CROSSGEN_COMPILE
    if (!hndClass.IsTypeDesc())
    {
        ComCallWrapperTemplate *pTemplate = hndClass.AsMethodTable()->GetComCallWrapperTemplate();
        if (pTemplate != NULL)
        {
            // if CCW template is available, use its cache
            MethodTable *pDefaultItf;
            DefaultInterfaceType itfType = pTemplate->GetDefaultInterface(&pDefaultItf);

            *pHndDefClass = TypeHandle(pDefaultItf);
            return itfType;
        }
    }
#endif // CROSSGEN_COMPILE

    return GetDefaultInterfaceForClassInternal(hndClass, pHndDefClass);
}


#ifndef CROSSGEN_COMPILE

HRESULT TryGetDefaultInterfaceForClass(TypeHandle hndClass, TypeHandle *pHndDefClass, DefaultInterfaceType *pDefItfType)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(!hndClass.IsNull());
        PRECONDITION(CheckPointer(pHndDefClass));
        PRECONDITION(CheckPointer(pDefItfType));        
    }
    CONTRACTL_END;

    GCX_COOP();
    
    HRESULT hr = S_OK;
    OBJECTREF pThrowable = NULL;
    
    GCPROTECT_BEGIN(pThrowable)
    {
        EX_TRY
        {
            *pDefItfType = GetDefaultInterfaceForClassWrapper(hndClass, pHndDefClass);
        }
        EX_CATCH
        {
            pThrowable = GET_THROWABLE();
        }
        EX_END_CATCH(SwallowAllExceptions);

        if (pThrowable != NULL)
            hr = SetupErrorInfo(pThrowable);
    }
    GCPROTECT_END();
    return hr;
}

// Returns the default interface for a class if it's an explicit interface or the AutoDual
// class interface. Sets *pbDispatch otherwise. This is the logic used by array marshaling
// in code:OleVariant::MarshalInterfaceArrayComToOleHelper.
MethodTable *GetDefaultInterfaceMTForClass(MethodTable *pMT, BOOL *pbDispatch)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pMT));
        PRECONDITION(!pMT->IsInterface());
        PRECONDITION(CheckPointer(pbDispatch));
    }
    CONTRACTL_END;

    TypeHandle hndDefItfClass;
    DefaultInterfaceType DefItfType = GetDefaultInterfaceForClassWrapper(TypeHandle(pMT), &hndDefItfClass);

    switch (DefItfType)
    {
        case DefaultInterfaceType_Explicit:
        case DefaultInterfaceType_AutoDual:
        {
            return hndDefItfClass.GetMethodTable();
        }

        case DefaultInterfaceType_IUnknown:
        case DefaultInterfaceType_BaseComClass:
        {
            *pbDispatch = FALSE;
            return NULL;
        }

        case DefaultInterfaceType_AutoDispatch:
        {
            *pbDispatch = TRUE;
            return NULL;
        }

        default:
        {
            _ASSERTE(!"Invalid default interface type!");
            return NULL;
        }
    }
}

//---------------------------------------------------------------------------
// This method retrieves the list of source interfaces for a given class.
void GetComSourceInterfacesForClass(MethodTable *pMT, CQuickArray<MethodTable *> &rItfList)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM());
        PRECONDITION(CheckPointer(pMT));
    }
    CONTRACTL_END;

    HRESULT             hr          = S_OK;
    const void*         pvData;
    ULONG               cbData;
    CQuickArray<CHAR>   qbCurrInterfaces;

    GCX_COOP();

    // Reset the size of the interface list to 0.
    rItfList.Shrink(0);

    if (pMT->IsWinRTObjectType() || pMT->IsExportedToWinRT())
    {
        // classic COM eventing is not supported in WinRT
        return;
    }

    // Starting at the specified class MT retrieve the COM source interfaces 
    // of all the striped of the hierarchy.
    for (; pMT != NULL; pMT = pMT->GetParentMethodTable())
    {
        // See if there is any [source] interface at this level of the hierarchy.
        hr = pMT->GetCustomAttribute(WellKnownAttribute::ComSourceInterfaces, &pvData, &cbData);
        IfFailThrow(hr);
        if (hr == S_OK && cbData > 2)
        {
            AppDomain *pCurrDomain = SystemDomain::GetCurrentDomain();

            CustomAttributeParser cap(pvData, cbData);
            IfFailThrow(cap.SkipProlog());

            while (cap.BytesLeft() != 0)
            {
                // Uncompress the current string of source interfaces.
                BYTE const *pbStr;
                ULONG       cbStr;
                IfFailThrow(cap.GetData(&pbStr, &cbStr));
        
                // Allocate a new buffer that will contain the current list of source interfaces.
                qbCurrInterfaces.ReSizeThrows(cbStr + 1);
                LPUTF8 strCurrInterfaces = qbCurrInterfaces.Ptr();
                memcpyNoGCRefs(strCurrInterfaces, pbStr, cbStr);
                strCurrInterfaces[cbStr] = 0;
                LPUTF8 pCurrInterfaces = strCurrInterfaces;
                LPUTF8 pCurrInterfacesEnd = pCurrInterfaces + cbStr + 1;

                while (pCurrInterfaces < pCurrInterfacesEnd && *pCurrInterfaces != 0)
                {
                    // Load the COM source interface specified in the CA.
                    TypeHandle ItfType;
                    ItfType = TypeName::GetTypeUsingCASearchRules(pCurrInterfaces, pMT->GetAssembly());

                    // If the type handle isn't a named type, then throw an exception using
                    // the name of the type obtained from pCurrInterfaces.
                    if (!ItfType.GetMethodTable())
                    {
                        // This should only occur for TypeDesc's.
                        StackSString ssInvalidItfName(SString::Utf8, pCurrInterfaces);
                        DefineFullyQualifiedNameForClassW()
                        COMPlusThrow(kTypeLoadException, IDS_EE_INVALIDCOMSOURCEITF,
                                     GetFullyQualifiedNameForClassW(pMT),
                                     ssInvalidItfName.GetUnicode());
                    }

                    // Otherwise, if the type is not an interface thrown an exception using the actual
                    // name of the type.
                    if (!ItfType.IsInterface())
                    {
                        StackSString ssClassName;
                        StackSString ssInvalidItfName;
                        pMT->_GetFullyQualifiedNameForClass(ssClassName);
                        ItfType.GetMethodTable()->_GetFullyQualifiedNameForClass(ssInvalidItfName);
                        COMPlusThrow(kTypeLoadException, IDS_EE_INVALIDCOMSOURCEITF,
                                     ssClassName.GetUnicode(), ssInvalidItfName.GetUnicode());
                    }

                    // Ensure the source interface is not generic.
                    if (ItfType.HasInstantiation())
                    {
                        StackSString ssClassName;
                        StackSString ssInvalidItfName;
                        pMT->_GetFullyQualifiedNameForClass(ssClassName);
                        ItfType.GetMethodTable()->_GetFullyQualifiedNameForClass(ssInvalidItfName);
                        COMPlusThrow(kTypeLoadException, IDS_EE_INVALIDCOMSOURCEITF,
                                     ssClassName.GetUnicode(), ssInvalidItfName.GetUnicode());
                    }


                    // Retrieve the IID of the COM source interface.
                    IID ItfIID;
                    ItfType.GetMethodTable()->GetGuid(&ItfIID, TRUE);                

                    // Go through the list of source interfaces and check to see if the new one is a duplicate.
                    // It can be a duplicate either if it is the same interface or if it has the same IID.
                    BOOL bItfInList = FALSE;
                    for (UINT i = 0; i < rItfList.Size(); i++)
                    {
                        if (rItfList[i] == ItfType.GetMethodTable())
                        {
                            bItfInList = TRUE;
                            break;
                        }

                        IID ItfIID2;
                        rItfList[i]->GetGuid(&ItfIID2, TRUE);
                        if (IsEqualIID(ItfIID, ItfIID2))
                        {
                            bItfInList = TRUE;
                            break;
                        }
                    }

                    // If the COM source interface is not in the list then add it.
                    if (!bItfInList)
                    {
                        size_t OldSize = rItfList.Size();
                        rItfList.ReSizeThrows(OldSize + 1);
                        rItfList[OldSize] = ItfType.GetMethodTable();
                    }

                    // Process the next COM source interfaces in the CA.
                    pCurrInterfaces += strlen(pCurrInterfaces) + 1;
                }
            }
        }
    }
}


//--------------------------------------------------------------------------------
// These methods convert a native IEnumVARIANT to a managed IEnumerator.
OBJECTREF ConvertEnumVariantToMngEnum(IEnumVARIANT *pNativeEnum)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;
    
    OBJECTREF MngEnum = NULL;
    OBJECTREF EnumeratorToEnumVariantMarshaler = NULL;
    GCPROTECT_BEGIN(EnumeratorToEnumVariantMarshaler)
    {
        // Retrieve the custom marshaler and the MD to use to convert the IEnumVARIANT.
        StdMngIEnumerator *pStdMngIEnumInfo = SystemDomain::GetCurrentDomain()->GetMngStdInterfacesInfo()->GetStdMngIEnumerator();
        MethodDesc *pEnumNativeToManagedMD = pStdMngIEnumInfo->GetCustomMarshalerMD(CustomMarshalerMethods_MarshalNativeToManaged);
        EnumeratorToEnumVariantMarshaler = pStdMngIEnumInfo->GetCustomMarshaler();
        MethodDescCallSite enumNativeToManaged(pEnumNativeToManagedMD, &EnumeratorToEnumVariantMarshaler);

        // Prepare the arguments that will be passed to MarshalNativeToManaged.
        ARG_SLOT MarshalNativeToManagedArgs[] = {
            ObjToArgSlot(EnumeratorToEnumVariantMarshaler),
            (ARG_SLOT)pNativeEnum
        };

        // Retrieve the managed view for the current native interface pointer.
        MngEnum = enumNativeToManaged.Call_RetOBJECTREF(MarshalNativeToManagedArgs);
    }
    GCPROTECT_END();

    return MngEnum;
}

//--------------------------------------------------------------------------------
// This method converts an OLE_COLOR to a System.Color.
void ConvertOleColorToSystemColor(OLE_COLOR SrcOleColor, SYSTEMCOLOR *pDestSysColor)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    // Retrieve the method desc to use for the current AD.
    MethodDesc *pOleColorToSystemColorMD = 
        GetAppDomain()->GetLoaderAllocator()->GetMarshalingData()->GetOleColorMarshalingInfo()->GetOleColorToSystemColorMD();

    MethodDescCallSite oleColorToSystemColor(pOleColorToSystemColorMD);

    _ASSERTE(pOleColorToSystemColorMD->HasRetBuffArg());

    ARG_SLOT Args[] = 
    {
        PtrToArgSlot(pDestSysColor),
        PtrToArgSlot(SrcOleColor)
    };

    oleColorToSystemColor.Call(Args);
}

//--------------------------------------------------------------------------------
// This method converts a System.Color to an OLE_COLOR.
OLE_COLOR ConvertSystemColorToOleColor(OBJECTREF *pSrcObj)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;
    
    // Retrieve the method desc to use for the current AD.
    MethodDesc *pSystemColorToOleColorMD = 
        GetAppDomain()->GetLoaderAllocator()->GetMarshalingData()->GetOleColorMarshalingInfo()->GetSystemColorToOleColorMD();
    MethodDescCallSite systemColorToOleColor(pSystemColorToOleColorMD);

    // Set up the args and call the method.
    SYSTEMCOLOR *pSrcSysColor = (SYSTEMCOLOR *)(*pSrcObj)->UnBox();
    return systemColorToOleColor.CallWithValueTypes_RetOleColor((const ARG_SLOT *)&pSrcSysColor);
}

//--------------------------------------------------------------------------------
// This method generates a stringized version of a class interface that contains 
// the signatures of all the methods and fields.
ULONG GetStringizedClassItfDef(TypeHandle InterfaceType, CQuickArray<BYTE> &rDef)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM());
        PRECONDITION(!InterfaceType.IsNull());
    }
    CONTRACTL_END;

    LPCWSTR             szName;                 
    ULONG               cchName;
    MethodTable*        pIntfMT = InterfaceType.GetMethodTable();
    PREFIX_ASSUME(pIntfMT != NULL);
    
    MethodTable*        pDeclaringMT = NULL;
    DWORD               nSlots;                 // Slots on the pseudo interface.
    mdToken             tkMb;                   // A method or field token.
    ULONG               cbCur;
    HRESULT             hr = S_OK;
    ULONG               i;

    // Should be an actual class.
    _ASSERTE(!pIntfMT->IsInterface());

    // See what sort of IClassX this class gets.
    TypeHandle thDefItf;
    BOOL bGenerateMethods = FALSE;
    DefaultInterfaceType DefItfType = GetDefaultInterfaceForClassWrapper(TypeHandle(pIntfMT), &thDefItf);
    
    // The results apply to this class if the thDefItf is this class itself, not a parent class.
    // A side effect is that [ComVisible(false)] types' guids are generated without members.
    if (thDefItf.GetMethodTable() == pIntfMT && DefItfType == DefaultInterfaceType_AutoDual)
        bGenerateMethods = TRUE;

    // Get the name of the class.
    DefineFullyQualifiedNameForClassW();
    szName = GetFullyQualifiedNameForClassNestedAwareW(pIntfMT);
    cchName = (ULONG)wcslen(szName);

    // Start with the interface name.
    cbCur = cchName * sizeof(WCHAR);
    rDef.ReSizeThrows(cbCur + sizeof(WCHAR));
    wcscpy_s(reinterpret_cast<LPWSTR>(rDef.Ptr()), rDef.Size()/sizeof(WCHAR), szName);

    if (bGenerateMethods)
    {
        ComMTMemberInfoMap MemberMap(pIntfMT); // The map of members.

        // Retrieve the method properties.
        MemberMap.Init(sizeof(void*));

        CQuickArray<ComMTMethodProps> &rProps = MemberMap.GetMethods();
        nSlots = (DWORD)rProps.Size();

        // Now add the methods to the TypeInfo.
        for (i=0; i<nSlots; ++i)
        {
            ComMTMethodProps *pProps = &rProps[i];
            if (pProps->bMemberVisible)
            {
                if (pProps->semantic < FieldSemanticOffset)
                {
                    pDeclaringMT = pProps->pMeth->GetMethodTable();
                    tkMb = pProps->pMeth->GetMemberDef();
                    cbCur = GetStringizedMethodDef(pDeclaringMT, tkMb, rDef, cbCur);
                }
                else
                {
                    ComCallMethodDesc   *pFieldMeth;    // A MethodDesc for a field call.
                    FieldDesc   *pField;                // A FieldDesc.
                    pFieldMeth = reinterpret_cast<ComCallMethodDesc*>(pProps->pMeth);
                    pField = pFieldMeth->GetFieldDesc();
                    pDeclaringMT = pField->GetApproxEnclosingMethodTable();
                    tkMb = pField->GetMemberDef();
                    cbCur = GetStringizedFieldDef(pDeclaringMT, tkMb, rDef, cbCur);
                }
            }
        }
    }
    
    // Return the number of bytes.
    return cbCur;
} // ULONG GetStringizedClassItfDef()

//--------------------------------------------------------------------------------
// Helper to get the GUID of a class interface.
void GenerateClassItfGuid(TypeHandle InterfaceType, GUID *pGuid)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM());
        PRECONDITION(!InterfaceType.IsNull());
        PRECONDITION(CheckPointer(pGuid));
    }
    CONTRACTL_END;

    LPWSTR      szName;                 // Name to turn to a guid.
    ULONG       cchName;                // Length of the name (possibly after decoration).
    CQuickArray<BYTE> rName;            // Buffer to accumulate signatures.
    ULONG       cbCur;                  // Current offset.
    HRESULT     hr = S_OK;              // A result.

    cbCur = GetStringizedClassItfDef(InterfaceType, rName);
    
    // Pad up to a whole WCHAR.
    if (cbCur % sizeof(WCHAR))
    {
        int cbDelta = sizeof(WCHAR) - (cbCur % sizeof(WCHAR));
        rName.ReSizeThrows(cbCur + cbDelta);
        memset(rName.Ptr() + cbCur, 0, cbDelta);
        cbCur += cbDelta;
    }

    // Point to the new buffer.
    cchName = cbCur / sizeof(WCHAR);
    szName = reinterpret_cast<LPWSTR>(rName.Ptr());

    // Generate guid from name.
    CorGuidFromNameW(pGuid, szName, cchName);
} // void GenerateClassItfGuid()

HRESULT TryGenerateClassItfGuid(TypeHandle InterfaceType, GUID *pGuid)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(!InterfaceType.IsNull());
        PRECONDITION(CheckPointer(pGuid));
    }
    CONTRACTL_END;

    GCX_COOP();

    HRESULT hr = S_OK;
    OBJECTREF pThrowable = NULL;

    GCPROTECT_BEGIN(pThrowable)
    {
        EX_TRY
        {
            GenerateClassItfGuid(InterfaceType, pGuid);
        }
        EX_CATCH
        {
            pThrowable = GET_THROWABLE();
        }
        EX_END_CATCH (SwallowAllExceptions);

        if (pThrowable != NULL)
            hr = SetupErrorInfo(pThrowable);
    }
    GCPROTECT_END();
    
    return hr;
}

//--------------------------------------------------------------------------------
// Helper to get the GUID of the typelib that is created from an assembly.
HRESULT GetTypeLibGuidForAssembly(Assembly *pAssembly, GUID *pGuid)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pAssembly));
        PRECONDITION(CheckPointer(pGuid));        
    }
    CONTRACTL_END;

    HRESULT     hr = S_OK;
    CQuickArray<BYTE> rName;            // String for guid.
    ULONG       cbData;                 // Size of the string in bytes.
 
    // Get GUID from Assembly, else from Manifest Module, else Generate from name.
    hr = pAssembly->GetManifestImport()->GetItemGuid(TokenFromRid(1, mdtAssembly), pGuid);

    if (*pGuid == GUID_NULL)
    {
        // Get the string.
        IfFailGo(GetStringizedTypeLibGuidForAssembly(pAssembly, rName, 0, &cbData));
        
        // Pad to a whole WCHAR.
        if (cbData % sizeof(WCHAR))
        {
            IfFailGo(rName.ReSizeNoThrow(cbData + sizeof(WCHAR)-(cbData%sizeof(WCHAR))));
            while (cbData % sizeof(WCHAR))
                rName[cbData++] = 0;
        }
    
        // Turn into guid
        CorGuidFromNameW(pGuid, (LPWSTR)rName.Ptr(), cbData/sizeof(WCHAR));
}

ErrExit:
    return hr;
} // HRESULT GetTypeLibGuidForAssembly()

//--------------------------------------------------------------------------------
// Helper to get the version of the typelib that is created from an assembly.
HRESULT GetTypeLibVersionForAssembly(
    _In_ Assembly *pAssembly,
    _Out_ USHORT *pMajorVersion,
    _Out_ USHORT *pMinorVersion)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pAssembly));
        PRECONDITION(CheckPointer(pMajorVersion));
        PRECONDITION(CheckPointer(pMinorVersion));
    }
    CONTRACTL_END;

    HRESULT hr;
    const BYTE *pbData = nullptr;
    ULONG cbData = 0;

    if (!pAssembly->IsWinMD())
    {
        // Check to see if the TypeLibVersionAttribute is set.
        IfFailRet(pAssembly->GetManifestImport()->GetCustomAttributeByName(TokenFromRid(1, mdtAssembly), INTEROP_TYPELIBVERSION_TYPE, (const void**)&pbData, &cbData));
    }

    // For attribute contents, see https://docs.microsoft.com/dotnet/api/system.runtime.interopservices.typelibversionattribute
    if (cbData >= (2 + 2 * sizeof(UINT32)))
    {
        CustomAttributeParser cap(pbData, cbData);
        IfFailRet(cap.SkipProlog());

        // Retrieve the major and minor version from the attribute.
        UINT32 u4;
        IfFailRet(cap.GetU4(&u4));
        *pMajorVersion = GET_VERSION_USHORT_FROM_INT(u4);
        IfFailRet(cap.GetU4(&u4));
        *pMinorVersion = GET_VERSION_USHORT_FROM_INT(u4);
    }
    else
    {
        // Use the assembly's major and minor version number.
        IfFailRet(pAssembly->GetVersion(pMajorVersion, pMinorVersion, nullptr, nullptr));
    }

    // Some system don't handle a typelib with a version of 0.0.
    // When that happens, change it to 1.0.
    if (*pMajorVersion == 0 && *pMinorVersion == 0)
        *pMajorVersion = 1;

    return S_OK;
} // HRESULT TypeLibExporter::GetTypeLibVersionFromAssembly()

#endif //CROSSGEN_COMPILE


//---------------------------------------------------------------------------
// This method determines if a member is visible from COM.
BOOL IsMethodVisibleFromCom(MethodDesc *pMD)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(CheckPointer(pMD));
    }
    CONTRACTL_END;

    HRESULT     hr = S_OK; 
    mdProperty  pd;        
    LPCUTF8     pPropName;
    ULONG       uSemantic;
    mdMethodDef md = pMD->GetMemberDef();
    
    // See if there is property information for this member.
    hr = pMD->GetModule()->GetPropertyInfoForMethodDef(md, &pd, &pPropName, &uSemantic);
    IfFailThrow(hr);

    if (hr == S_OK)
    {
        return IsMemberVisibleFromCom(pMD->GetMethodTable(), pd, md);
    }
    else
    {
        return IsMemberVisibleFromCom(pMD->GetMethodTable(), md, mdTokenNil);
    }
}

//---------------------------------------------------------------------------
// This method determines if a type is visible from COM or not based on
// its visibility. This version of the method works with a type handle.
// This version will ignore a type's generic attributes.
//
// This API should *never* be called directly!!!
static BOOL SpecialIsGenericTypeVisibleFromCom(TypeHandle hndType)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(!hndType.IsNull());
    }
    CONTRACTL_END;

    DWORD                   dwFlags;
    mdTypeDef               tdEnclosingType;
    HRESULT                 hr;
    const BYTE *            pVal;
    ULONG                   cbVal;
    MethodTable *           pMT = hndType.GetMethodTable();
    _ASSERTE(pMT);

    mdTypeDef               mdType = pMT->GetCl();
    IMDInternalImport *     pInternalImport = pMT->GetMDImport();
    Assembly *              pAssembly = pMT->GetAssembly();
    Module *                pModule = pMT->GetModule();

    // If the type is a COM imported interface then it is visible from COM.
    if (pMT->IsInterface() && pMT->IsComImport())
        return TRUE;

    // If the type is imported from WinRT (has the tdWindowsRuntime flag set), then it is visible from COM.
    if (pMT->IsProjectedFromWinRT())
        return TRUE;

    // If the type is an array, then it is not visible from COM.
    if (pMT->IsArray())
        return FALSE;

    // Retrieve the flags for the current type.
    tdEnclosingType = mdType;
    if (FAILED(pInternalImport->GetTypeDefProps(tdEnclosingType, &dwFlags, 0)))
    {
        return FALSE;
    }

    // Handle nested types.
    while (IsTdNestedPublic(dwFlags))
    {
        hr = pInternalImport->GetNestedClassProps(tdEnclosingType, &tdEnclosingType);
        if (FAILED(hr))
        {
            return FALSE;
        }

        // Retrieve the flags for the enclosing type.
        if (FAILED(pInternalImport->GetTypeDefProps(tdEnclosingType, &dwFlags, 0)))
        {
            return FALSE;
        }
    }

    // If the outermost type is not visible then the specified type is not visible.
    if (!IsTdPublic(dwFlags))
        return FALSE;

    // Check to see if the type has the ComVisible attribute set.
    hr = pModule->GetCustomAttribute(mdType, WellKnownAttribute::ComVisible, (const void**)&pVal, &cbVal);
    if (hr == S_OK)
    {
        CustomAttributeParser cap(pVal, cbVal);
        if (FAILED(cap.SkipProlog()))
            return FALSE;

        UINT8 u1;
        if (FAILED(cap.GetU1(&u1)))
            return FALSE;

        return (BOOL)u1;
    }

    // Check to see if the assembly has the ComVisible attribute set.
    hr = pModule->GetCustomAttribute(pAssembly->GetManifestToken(), WellKnownAttribute::ComVisible, (const void**)&pVal, &cbVal);
    if (hr == S_OK)
    {
        CustomAttributeParser cap(pVal, cbVal);
        if (FAILED(cap.SkipProlog()))
            return FALSE;

        UINT8 u1;
        if (FAILED(cap.GetU1(&u1)))
            return FALSE;

        return (BOOL)u1;
    }

    // The type is visible.
    return TRUE;
}

//---------------------------------------------------------------------------
// This method determines if a type is visible from COM or not based on
// its visibility. This version of the method works with a type handle.
BOOL IsTypeVisibleFromCom(TypeHandle hndType)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(!hndType.IsNull());        
    }
    CONTRACTL_END;    

    if (!hndType.SupportsGenericInterop(TypeHandle::Interop_NativeToManaged))
    {
        // If the type is a generic type, then it is not visible from COM.
        if (hndType.HasInstantiation() || hndType.IsGenericVariable())
            return FALSE;
    }

    return SpecialIsGenericTypeVisibleFromCom(hndType);
}

#ifdef FEATURE_PREJIT
//---------------------------------------------------------------------------
// Determines if a method is likely to be used for forward COM/WinRT interop.
BOOL MethodNeedsForwardComStub(MethodDesc *pMD, DataImage *pImage)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    MethodTable *pMT = pMD->GetMethodTable();
        
    if (pMT->HasInstantiation() && !pMT->SupportsGenericInterop(TypeHandle::Interop_ManagedToNative))
    {
        // method is declared on an unsupported generic type -> stub not needed
        return FALSE;
    }

    if (pMT->IsProjectedFromWinRT() && pMT->IsComImport() && pMD->IsPrivate())
    {
        // private WinRT method -> stub not needed
        return FALSE;
    }

    if (pMT->IsWinRTObjectType())
    {
        // WinRT runtime class -> stub needed
        return TRUE;
    }

    if (pMT->IsWinRTRedirectedInterface(TypeHandle::Interop_ManagedToNative))
    {
        if (!pMT->HasInstantiation())
        {
            // non-generic redirected interface -> stub needed
            return TRUE;
        }

        // Generating stubs for generic redirected interfaces into all assemblies would grow NetFX native images
        // by several per cent. See BCL\System\Internal.cs if you need to add specific instantiations in mscorlib.
        DWORD assemblyFlags = pImage->GetModule()->GetAssembly()->GetFlags();
        if (IsAfContentType_WindowsRuntime(assemblyFlags))
        {
            // generic redirected interface while NGENing a .winmd -> stub needed
            return TRUE;
        }
    }

    GUID guid;
    pMT->GetGuid(&guid, FALSE);

    if (guid != GUID_NULL)
    {
        // explicit GUID defined in metadata -> stub needed
        return TRUE;
    }

    return FALSE;
}

//---------------------------------------------------------------------------
// Determines if a method is visible from COM in a way that requires a marshaling
// stub, i.e. it allows early binding.
BOOL MethodNeedsReverseComStub(MethodDesc *pMD)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pMD));
    }
    CONTRACTL_END;

    BOOL fIsAllowedCtorOrStatic = FALSE;
    MethodTable *pMT = pMD->GetMethodTable();

    if (pMT->IsInterface())
    {
        if (!pMT->IsComImport() && !IsTypeVisibleFromCom(TypeHandle(pMT)))
            return FALSE;

        if (pMT->HasInstantiation() && !pMT->SupportsGenericInterop(TypeHandle::Interop_NativeToManaged))
            return FALSE;

        // declaring interface must be InterfaceIsIUnknown or InterfaceIsDual
        if (pMT->GetComInterfaceType() == ifDispatch)
            return FALSE;

        Assembly * pAssembly = pMT->GetAssembly();
        if (pAssembly->IsWinMD() && !pAssembly->IsManagedWinMD())
        {
            //
            // Internal interfaces defined in native winmds can only ever be implemented by native components.
            // Managed classes won't be able to implement the internal interfaces, and so the reverse COM stubs 
            // are not needed for them.
            //
            if (IsTdNotPublic(pMT->GetClass()->GetProtection()))
            {
                //
                // However, we do need CCWs for internal interfaces that define protected members of inheritable classes
                // (for example, Windows.UI.Xaml.Application implements both IApplication, which we don't need 
                // a CCW for and IApplicationOverrides, which we do need).
                //
                if (!pMT->GetWriteableData()->IsOverridingInterface())
                {
                    return FALSE;
                }
            }
        }
    }
    else
    {
        if (!IsTypeVisibleFromCom(TypeHandle(pMT)))
            return FALSE;

        if (pMT->IsDelegate())
        {
            // the 'Invoke' method of a WinRT delegate needs stub
            return ((pMT->IsProjectedFromWinRT() || WinRTTypeNameConverter::IsRedirectedType(pMT)) &&
                     pMD->HasSameMethodDefAs(COMDelegate::FindDelegateInvokeMethod(pMT)));
        }

        if (pMT->IsExportedToWinRT() && (pMD->IsCtor() || pMD->IsStatic()))
        {
            fIsAllowedCtorOrStatic = TRUE;
        }
        else
        {
            // declaring class must be AutoDual
            if (pMT->GetComClassInterfaceType() != clsIfAutoDual)
                return FALSE;
        }
    }

    // static methods and ctors are not exposed to COM except for WinRT
    if (!fIsAllowedCtorOrStatic && (pMD->IsCtor() || pMD->IsStatic()))
        return FALSE;

    // NGen can't compile stubs for var arg methods
    if (pMD->IsVarArg())
        return FALSE;

    return IsMethodVisibleFromCom(pMD);
}
#endif // FEATURE_PREJIT


#ifndef CROSSGEN_COMPILE

//--------------------------------------------------------------------------------
// Validate that the given target is valid for the specified type.
BOOL IsComTargetValidForType(REFLECTCLASSBASEREF* pRefClassObj, OBJECTREF* pTarget)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pRefClassObj));
        PRECONDITION(CheckPointer(pTarget));
    }
    CONTRACTL_END;
    
    MethodTable* pInvokedMT = (*pRefClassObj)->GetType().GetMethodTable();

    MethodTable* pTargetMT = (*pTarget)->GetMethodTable();
    _ASSERTE(pTargetMT);
    PREFIX_ASSUME(pInvokedMT != NULL);

    // If the target class and the invoke class are identical then the invoke is valid.
    if (pTargetMT == pInvokedMT)
        return TRUE;

    // We always allow calling InvokeMember on a __ComObject type regardless of the type
    // of the target object.
    if (IsComObjectClass((*pRefClassObj)->GetType()))
        return TRUE;

    // If the class that is being invoked on is an interface then check to see if the
    // target class supports that interface.
    if (pInvokedMT->IsInterface())
        return Object::SupportsInterface(*pTarget, pInvokedMT);

    // Check to see if the target class inherits from the invoked class.
    while (pTargetMT)
    {
        pTargetMT = pTargetMT->GetParentMethodTable();
        if (pTargetMT == pInvokedMT)
        {
            // The target class inherits from the invoked class.
            return TRUE;
        }
    }

    // There is no valid relationship between the invoked and the target classes.
    return FALSE;
}

DISPID ExtractStandardDispId(__in_z LPWSTR strStdDispIdMemberName)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    // Find the first character after the = in the standard DISPID member name.
    LPWSTR strDispId = wcsstr(&strStdDispIdMemberName[STANDARD_DISPID_PREFIX_LENGTH], W("=")) + 1;
    if (!strDispId)
        COMPlusThrow(kArgumentException, IDS_EE_INVALID_STD_DISPID_NAME);

    // Validate that the last character of the standard member name is a ].
    LPWSTR strClosingBracket = wcsstr(strDispId, W("]"));
    if (!strClosingBracket || (strClosingBracket[1] != 0))
        COMPlusThrow(kArgumentException, IDS_EE_INVALID_STD_DISPID_NAME);

    // Extract the number from the standard DISPID member name.
    return _wtoi(strDispId);
}

static HRESULT InvokeExHelper(
    IDispatchEx *       pDispEx,
    DISPID              MemberID,
    LCID                lcid,
    WORD                flags,
    DISPPARAMS *        pDispParams,
    VARIANT*            pVarResult,
    EXCEPINFO *         pExcepInfo,
                              IServiceProvider *pspCaller)
{
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_TRIGGERS;
    STATIC_CONTRACT_MODE_ANY;

    _ASSERTE(pDispEx != NULL);

    struct Param : CallOutFilterParam {
        HRESULT             hr;
        IDispatchEx *       pDispEx;
        DISPID              MemberID;
        LCID                lcid;
        WORD                flags;
        DISPPARAMS *        pDispParams;
        VARIANT*            pVarResult;
        EXCEPINFO *         pExcepInfo;
        IServiceProvider *  pspCaller;
    }; Param param;
    
    param.OneShot = TRUE; // Inherited from CallOutFilterParam
    param.hr = S_OK;
    param.pDispEx = pDispEx;
    param.MemberID = MemberID;
    param.lcid = lcid;
    param.flags = flags;
    param.pDispParams = pDispParams;
    param.pVarResult = pVarResult;
    param.pExcepInfo = pExcepInfo;
    param.pspCaller = pspCaller;
    
    PAL_TRY(Param *, pParam, &param)
    {
        pParam->hr = pParam->pDispEx->InvokeEx(pParam->MemberID,
                                               pParam->lcid,
                                               pParam->flags,
                                               pParam->pDispParams,
                                               pParam->pVarResult,
                                               pParam->pExcepInfo,
                                               pParam->pspCaller);
    }
    PAL_EXCEPT_FILTER(CallOutFilter)
    {
        _ASSERTE(!"CallOutFilter returned EXECUTE_HANDLER.");
    }
    PAL_ENDTRY;

    return param.hr;
}

static HRESULT InvokeHelper(
    IDispatch *     pDisp,
    DISPID          MemberID,
    REFIID          riid,
    LCID            lcid,
    WORD            flags,
    DISPPARAMS *    pDispParams,
    VARIANT*        pVarResult,
    EXCEPINFO *     pExcepInfo,
                            UINT *piArgErr)
{
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_TRIGGERS;
    STATIC_CONTRACT_MODE_ANY;

    _ASSERTE(pDisp != NULL);

    struct Param : CallOutFilterParam {
        HRESULT             hr;
        IDispatch *         pDisp;
        DISPID              MemberID;
        REFIID              riid;
        LCID                lcid;
        WORD                flags;
        DISPPARAMS *        pDispParams;
        VARIANT *           pVarResult;
        EXCEPINFO *         pExcepInfo;
        UINT *              piArgErr;

        Param(REFIID _riid) : riid(_riid) {}
    }; Param param(riid);

    param.OneShot = TRUE; // Inherited from CallOutFilterParam
    param.hr = S_OK;
    param.pDisp = pDisp;
    param.MemberID = MemberID;
    //param.riid = riid;
    param.lcid = lcid;
    param.flags = flags;
    param.pDispParams = pDispParams;
    param.pVarResult = pVarResult;
    param.pExcepInfo = pExcepInfo;
    param.piArgErr = piArgErr;

    PAL_TRY(Param *, pParam, &param)
    {
        pParam->hr = pParam->pDisp->Invoke(pParam->MemberID,
                                           pParam->riid,
                                           pParam->lcid,
                                           pParam->flags,
                                           pParam->pDispParams,
                                           pParam->pVarResult,
                                           pParam->pExcepInfo,
                                           pParam->piArgErr);
    }
    PAL_EXCEPT_FILTER(CallOutFilter)
    {
        _ASSERTE(!"CallOutFilter returned EXECUTE_HANDLER.");
    }
    PAL_ENDTRY;

    return param.hr;
}


void DispInvokeConvertObjectToVariant(OBJECTREF *pSrcObj, VARIANT *pDestVar, ByrefArgumentInfo *pByrefArgInfo)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pSrcObj));
        PRECONDITION(IsProtectedByGCFrame (pSrcObj));
        PRECONDITION(CheckPointer(pDestVar));        
        PRECONDITION(CheckPointer(pByrefArgInfo));
    }
    CONTRACTL_END;
   
    if (pByrefArgInfo->m_bByref)
    {
        if (*pSrcObj == NULL)
        {
            V_VT(pDestVar) = VT_VARIANT | VT_BYREF;
            pDestVar->pvarVal = &pByrefArgInfo->m_Val;
        }
        else if (MscorlibBinder::IsClass((*pSrcObj)->GetMethodTable(), CLASS__VARIANT_WRAPPER))
        {
            OBJECTREF WrappedObj = (*((VARIANTWRAPPEROBJECTREF*)pSrcObj))->GetWrappedObject();
            GCPROTECT_BEGIN(WrappedObj)
            {
                OleVariant::MarshalOleVariantForObject(&WrappedObj, &pByrefArgInfo->m_Val);
                V_VT(pDestVar) = VT_VARIANT | VT_BYREF;
                pDestVar->pvarVal = &pByrefArgInfo->m_Val;
            }
            GCPROTECT_END();
        }
        else
        {
            OleVariant::MarshalOleVariantForObject(pSrcObj, &pByrefArgInfo->m_Val);
            OleVariant::CreateByrefVariantForVariant(&pByrefArgInfo->m_Val, pDestVar);
        }
    }
    else
    {
        OleVariant::MarshalOleVariantForObject(pSrcObj, pDestVar);
    }
}

static void DoIUInvokeDispMethod(IDispatchEx* pDispEx, IDispatch* pDisp, DISPID MemberID, LCID lcid, 
                                 WORD flags, DISPPARAMS* pDispParams, VARIANT* pVarResult)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;
    
    UINT        iArgErr;
    EXCEPINFO   ExcepInfo;
    HRESULT     hr;

    memset(&ExcepInfo, 0, sizeof(EXCEPINFO));
   
    GCX_COOP();
    OBJECTREF pThrowable = NULL;
    GCPROTECT_BEGIN(pThrowable);
    {
        // Call the method
        EX_TRY 
        {
            {
            // We are about to make call's to COM so switch to preemptive GC.
            GCX_PREEMP();

                if (pDispEx)
                {
                    hr = InvokeExHelper(pDispEx, MemberID, lcid, flags, pDispParams,
                                        pVarResult, &ExcepInfo, NULL);
                }
                else
                {
                    hr = InvokeHelper(  pDisp, MemberID, IID_NULL, lcid, flags,
                                        pDispParams, pVarResult, &ExcepInfo, &iArgErr);
                }
            }

            // If the invoke call failed then throw an exception based on the EXCEPINFO.
            if (FAILED(hr))
            {
                if (hr == DISP_E_EXCEPTION)
                {
                    // This method will free the BSTR's in the EXCEPINFO.
                    COMPlusThrowHR(&ExcepInfo);
                }
                else
                {
                    COMPlusThrowHR(hr);
                }
            }
        } 
        EX_CATCH 
        {
            // If we get here we need to throw an TargetInvocationException
            pThrowable = GET_THROWABLE();
            _ASSERTE(pThrowable != NULL);
        }
        EX_END_CATCH(RethrowTerminalExceptions);

        if (pThrowable != NULL)
        {
            COMPlusThrow(InvokeUtil::CreateTargetExcept(&pThrowable));
        }
    }
    GCPROTECT_END();
}


FORCEINLINE void DispParamHolderRelease(VARIANT* value)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;
    
    if (value)
    {
       if (V_VT(value) & VT_BYREF)
       {
           VariantHolder TmpVar;
           OleVariant::ExtractContentsFromByrefVariant(value, &TmpVar);
       }
       
       SafeVariantClear(value);
    }
}

class DispParamHolder : public Wrapper<VARIANT*, DispParamHolderDoNothing, DispParamHolderRelease, NULL>
{
public:
    DispParamHolder(VARIANT* p = NULL)
        : Wrapper<VARIANT*, DispParamHolderDoNothing, DispParamHolderRelease, NULL>(p)
    {
        WRAPPER_NO_CONTRACT;
    }

    FORCEINLINE void operator=(VARIANT* p)
    {
        WRAPPER_NO_CONTRACT;
        Wrapper<VARIANT*, DispParamHolderDoNothing, DispParamHolderRelease, NULL>::operator=(p);
    }
};



//--------------------------------------------------------------------------------
// InvokeDispMethod will convert a set of managed objects and call IDispatch.  The
// result will be returned as a CLR Variant pointed to by pRetVal.
void IUInvokeDispMethod(
    REFLECTCLASSBASEREF* pRefClassObj,
    OBJECTREF* pTarget,
    OBJECTREF* pName,
    DISPID *pMemberID,
    OBJECTREF* pArgs,
    OBJECTREF* pByrefModifiers,
    OBJECTREF* pNamedArgs,
    OBJECTREF* pRetVal,
    LCID lcid,
    WORD flags,
    BOOL bIgnoreReturn,
    BOOL bIgnoreCase)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pTarget));        
    }
    CONTRACTL_END;

    HRESULT             hr;
    UINT                i;
    UINT                iSrcArg;
    UINT                iDestArg;
    VARIANT             VarResult;
    UINT                cArgs               = 0;
    UINT                cNamedArgs          = 0;
    DISPPARAMS          DispParams          = {0};
    DISPID*             aDispID             = NULL;
    DISPID              MemberID            = 0;
    ByrefArgumentInfo*  aByrefArgInfos      = NULL;
    BOOL                bSomeArgsAreByref   = FALSE;
    SafeComHolder<IDispatch> pDisp          = NULL;
    SafeComHolder<IDispatchEx> pDispEx      = NULL;
    VariantPtrHolder    pVarResult          = NULL;
    NewArrayHolder<DispParamHolder> params  = NULL;

    //
    // Function initialization.
    //

    SafeVariantInit(&VarResult);


    // InteropUtil.h does not know about anything other than OBJECTREF so
    // convert the OBJECTREF's to their real type.

    STRINGREF* pStrName = (STRINGREF*) pName;
    PTRARRAYREF* pArrArgs = (PTRARRAYREF*) pArgs;
    PTRARRAYREF* pArrByrefModifiers = (PTRARRAYREF*) pByrefModifiers;
    PTRARRAYREF* pArrNamedArgs = (PTRARRAYREF*) pNamedArgs;
    MethodTable* pInvokedMT = (*pRefClassObj)->GetType().GetMethodTable();
    PREFIX_ASSUME(pInvokedMT != NULL);

    // Retrieve the total count of arguments.
    if (*pArrArgs != NULL)
        cArgs = (*pArrArgs)->GetNumComponents();

    // Retrieve the count of named arguments.
    if (*pArrNamedArgs != NULL)
        cNamedArgs = (*pArrNamedArgs)->GetNumComponents();

    // Validate that the target is valid for the specified type.
    if (!IsComTargetValidForType(pRefClassObj, pTarget))
        COMPlusThrow(kTargetException, W("RFLCT.Targ_ITargMismatch"));

    // If the invoked type is an interface, make sure it is IDispatch based.
    if (pInvokedMT->IsInterface())
    {
        CorIfaceAttr ifaceType = pInvokedMT->GetComInterfaceType();
        if (!IsDispatchBasedItf(ifaceType))
            COMPlusThrow(kTargetInvocationException, IDS_EE_INTERFACE_NOT_DISPATCH_BASED);
    }

    // Validate that the target is a COM object.
    _ASSERTE((*pTarget)->GetMethodTable()->IsComObjectType());

    //
    // Initialize the DISPPARAMS structure.
    //
    if (cArgs > 0)
    {
        UINT cPositionalArgs = cArgs - cNamedArgs;
       
        DispParams.cArgs = cArgs;
        DispParams.rgvarg = (VARIANTARG *)_alloca(cArgs * sizeof(VARIANTARG));
        params = new DispParamHolder[cArgs];

        // Initialize all the variants.
        GCX_PREEMP();
        for (i = 0; i < cArgs; i++)
        {
            SafeVariantInit(&DispParams.rgvarg[i]);
            params[i] = &DispParams.rgvarg[i];
        }
    }


    //
    // Retrieve the IDispatch interface that will be invoked on.
    //

    if (pInvokedMT->IsInterface())
    {
        // The invoked type is a dispatch or dual interface so we will make the
        // invocation on it.
        pDisp = (IDispatch *)ComObject::GetComIPFromRCWThrowing(pTarget, pInvokedMT);
    }
    else
    {
        // A class was passed in so we will make the invocation on the default
        // IDispatch for the COM component.

        RCWHolder pRCW(GetThread());
        RCWPROTECT_BEGIN(pRCW, *pTarget);

        // Retrieve the IDispath pointer from the wrapper.
        pDisp = (IDispatch*)pRCW->GetIDispatch();
        if (!pDisp)
            COMPlusThrow(kTargetInvocationException, IDS_EE_NO_IDISPATCH_ON_TARGET);

        // If we aren't ignoring case, then we need to try and QI for IDispatchEx to 
        // be able to use IDispatchEx::GetDispID() which has a flag to control case
        // sentisitivity.
        if (!bIgnoreCase && cNamedArgs == 0)
        {
            RCW_VTABLEPTR(pRCW);
            hr = SafeQueryInterface(pDisp, IID_IDispatchEx, (IUnknown**)&pDispEx);
            if (FAILED(hr))
                pDispEx = NULL;
        }

        RCWPROTECT_END(pRCW);
    }
    _ASSERTE((IUnknown*)pDisp != NULL);


    //
    // Prepare the DISPID's that will be passed to invoke.
    //

    if (pMemberID && (*pMemberID != DISPID_UNKNOWN) && (cNamedArgs == 0))
    {
        // The caller specified a member ID and we don't have any named arguments so
        // we can simply use the member ID the caller specified.
        MemberID = *pMemberID;
    }
    else
    {
        int strNameLength = (*pStrName)->GetStringLength();

        // Check if we are invoking on the default member.
        if (strNameLength == 0)
        {
            // Set the DISPID to 0 (default member).
            MemberID = 0;

            _ASSERTE(cNamedArgs == 0);
            if (cNamedArgs != 0)
                COMPlusThrow(kNotSupportedException,W("NotSupported_IDispInvokeDefaultMemberWithNamedArgs"));
        }
        else
        {
            //
            // Create an array of strings that will be passed to GetIDsOfNames().
            //

            UINT cNamesToConvert = cNamedArgs + 1;
            LPWSTR strTmpName = NULL;
            
            // Allocate the array of strings to convert, the array of pinned handles and the
            // array of converted DISPID's.
            size_t allocSize = cNamesToConvert * sizeof(LPWSTR);
            if (allocSize < cNamesToConvert)
                COMPlusThrowArgumentOutOfRange(W("namedParameters"), W("ArgumentOutOfRange_Capacity"));
            LPWSTR *aNamesToConvert = (LPWSTR *)_alloca(allocSize);
            
            allocSize = cNamesToConvert * sizeof(DISPID);
            if (allocSize < cNamesToConvert)
                COMPlusThrowArgumentOutOfRange(W("namedParameters"), W("ArgumentOutOfRange_Capacity"));
            aDispID = (DISPID *)_alloca(allocSize);

            // The first name to convert is the name of the method itself.
            aNamesToConvert[0] = (*pStrName)->GetBuffer();

            // Check to see if the name is for a standard DISPID.
            if (SString::_wcsnicmp(aNamesToConvert[0], STANDARD_DISPID_PREFIX, STANDARD_DISPID_PREFIX_LENGTH) == 0)
            {
                // The name is for a standard DISPID so extract it from the name.
                MemberID = ExtractStandardDispId(aNamesToConvert[0]);

                // Make sure there are no named arguments to convert.
                if (cNamedArgs > 0)
                {
                    STRINGREF *pNamedArgsData = (STRINGREF *)(*pArrNamedArgs)->GetDataPtr();

                    for (i = 0; i < cNamedArgs; i++)
                    {
                        // The first name to convert is the name of the method itself.
                        strTmpName = pNamedArgsData[i]->GetBuffer();

                        // Check to see if the name is for a standard DISPID.
                        if (SString::_wcsnicmp(strTmpName, STANDARD_DISPID_PREFIX, STANDARD_DISPID_PREFIX_LENGTH) != 0)
                            COMPlusThrow(kArgumentException, IDS_EE_NON_STD_NAME_WITH_STD_DISPID);

                        // The name is for a standard DISPID so extract it from the name.
                        aDispID[i + 1] = ExtractStandardDispId(strTmpName);
                    }
                }
            }
            else
            {
                BOOL fGotIt = FALSE;
                BOOL fIsNonGenericComObject = pInvokedMT->IsInterface() || (pInvokedMT != g_pBaseCOMObject && pInvokedMT->IsComObjectType());
                BOOL fUseCache = fIsNonGenericComObject && !(IUnknown*)pDispEx && strNameLength <= ReflectionMaxCachedNameLength && cNamedArgs == 0;
                DispIDCacheElement vDispIDElement;

                // If the object is not a generic COM object and the member meets the criteria to be
                // in the cache then look up the DISPID in the cache.
                if (fUseCache)
                {
                    vDispIDElement.pMT = pInvokedMT;
                    vDispIDElement.strNameLength = strNameLength;
                    vDispIDElement.lcid = lcid;
                    wcscpy_s(vDispIDElement.strName, COUNTOF(vDispIDElement.strName), aNamesToConvert[0]);

                    // Only look up if the cache has already been created.
                    DispIDCache* pDispIDCache = GetAppDomain()->GetRefDispIDCache();
                    fGotIt = pDispIDCache->GetFromCache (&vDispIDElement, MemberID);
                }

                if (!fGotIt)
                {
                    NewArrayHolder<PinningHandleHolder> ahndPinnedObjs = new PinningHandleHolder[cNamesToConvert];
                    ahndPinnedObjs[0] = GetAppDomain()->CreatePinningHandle((OBJECTREF)*pStrName);

                    // Copy the named arguments into the array of names to convert.
                    if (cNamedArgs > 0)
                    {
                        STRINGREF *pNamedArgsData = (STRINGREF *)(*pArrNamedArgs)->GetDataPtr();

                        for (i = 0; i < cNamedArgs; i++)
                        {
                            // Pin the string object and retrieve a pointer to its data.
                            ahndPinnedObjs[i + 1] = GetAppDomain()->CreatePinningHandle((OBJECTREF)pNamedArgsData[i]);
                            aNamesToConvert[i + 1] = pNamedArgsData[i]->GetBuffer();
                        }
                    }

                    //
                    // Call GetIDsOfNames to convert the names to DISPID's
                    //

                    {
                    // We are about to make call's to COM so switch to preemptive GC.
                    GCX_PREEMP();

                    if ((IUnknown*)pDispEx)
                    {
                        // We should only get here if we are doing a case sensitive lookup and
                        // we don't have any named arguments.
                        _ASSERTE(cNamedArgs == 0);
                        _ASSERTE(!bIgnoreCase);

                        // We managed to retrieve an IDispatchEx IP so we will use it to
                        // retrieve the DISPID.
                        BSTRHolder bstrTmpName = SysAllocString(aNamesToConvert[0]);
                        if (!bstrTmpName)
                            COMPlusThrowOM();

                        hr = pDispEx->GetDispID(bstrTmpName, fdexNameCaseSensitive, aDispID);
                    }
                    else
                    {
                        // Call GetIdsOfNames() to retrieve the DISPID's of the method and of the arguments.
                        hr = pDisp->GetIDsOfNames(
                                                    IID_NULL,
                                                    aNamesToConvert,
                                                    cNamesToConvert,
                                                    lcid,
                                                    aDispID
                                                );
                    }
                    }

                    if (FAILED(hr))
                    {
                        // Check to see if the user wants to invoke the new enum member.
                        if (cNamesToConvert == 1 && SString::_wcsicmp(aNamesToConvert[0], GET_ENUMERATOR_METHOD_NAME) == 0)
                        {
                            // Invoke the new enum member.
                            MemberID = DISPID_NEWENUM;
                        }
                        else
                        {
                            // The name is unknown.
                            COMPlusThrowHR(hr);
                        }
                    }
                    else
                    {
                        // The member ID is the first elements of the array we got back from GetIdsOfNames.
                        MemberID = aDispID[0];
                    }

                    // If the object is not a generic COM object and the member meets the criteria to be
                    // in the cache then insert the member in the cache.
                    if (fUseCache)
                    {
                        DispIDCache *pDispIDCache = GetAppDomain()->GetRefDispIDCache();
                        pDispIDCache->AddToCache (&vDispIDElement, MemberID);
                    }
                }
            }
        }

        // Store the member ID if the caller passed in a place to store it.
        if (pMemberID)
            *pMemberID = MemberID;
    }


    //
    // Fill in the DISPPARAMS structure.
    //

    if (cArgs > 0)
    {
        // Allocate the byref argument information.
        aByrefArgInfos = (ByrefArgumentInfo*)_alloca(cArgs * sizeof(ByrefArgumentInfo));
        memset(aByrefArgInfos, 0, cArgs * sizeof(ByrefArgumentInfo));

        // Set the byref bit on the arguments that have the byref modifier.
        if (*pArrByrefModifiers != NULL)
        {
            BYTE *aByrefModifiers = (BYTE*)(*pArrByrefModifiers)->GetDataPtr();
            for (i = 0; i < cArgs; i++)
            {
                if (aByrefModifiers[i])
                {
                    aByrefArgInfos[i].m_bByref = TRUE;
                    bSomeArgsAreByref = TRUE;
                }
            }
        }

        // We need to protect the temporary object that will be used to convert from
        // the managed objects to OLE variants.
        OBJECTREF TmpObj = NULL;
        GCPROTECT_BEGIN(TmpObj)
        {
            if (!(flags & (DISPATCH_PROPERTYPUT | DISPATCH_PROPERTYPUTREF)))
            {
                // For anything other than a put or a putref we just use the specified
                // named arguments.
                DispParams.cNamedArgs = cNamedArgs;
                DispParams.rgdispidNamedArgs = (cNamedArgs == 0) ? NULL : &aDispID[1];

                // Convert the named arguments from COM+ to OLE. These arguments are in the same order
                // on both sides.
                for (i = 0; i < cNamedArgs; i++)
                {
                    iSrcArg = i;
                    iDestArg = i;
                    TmpObj = ((OBJECTREF*)(*pArrArgs)->GetDataPtr())[iSrcArg];
                    DispInvokeConvertObjectToVariant(&TmpObj, &DispParams.rgvarg[iDestArg], &aByrefArgInfos[iSrcArg]); 
                }

                // Convert the unnamed arguments. These need to be presented in reverse order to IDispatch::Invoke().
                for (iSrcArg = cNamedArgs, iDestArg = cArgs - 1; iSrcArg < cArgs; iSrcArg++, iDestArg--)
                {
                    TmpObj = ((OBJECTREF*)(*pArrArgs)->GetDataPtr())[iSrcArg];
                    DispInvokeConvertObjectToVariant(&TmpObj, &DispParams.rgvarg[iDestArg], &aByrefArgInfos[iSrcArg]); 
                }
            }
            else
            {
                // If we are doing a property put then we need to set the DISPID of the
                // argument to DISP_PROPERTYPUT if there is at least one argument.
                DispParams.cNamedArgs = cNamedArgs + 1;
                DispParams.rgdispidNamedArgs = (DISPID*)_alloca((cNamedArgs + 1) * sizeof(DISPID));
                
                // Fill in the array of named arguments.
                DispParams.rgdispidNamedArgs[0] = DISPID_PROPERTYPUT;
                for (i = 1; i < cNamedArgs; i++)
                    DispParams.rgdispidNamedArgs[i] = aDispID[i];

                // The last argument from reflection becomes the first argument that must be passed to IDispatch.
                iSrcArg = cArgs - 1;
                iDestArg = 0;
                TmpObj = ((OBJECTREF*)(*pArrArgs)->GetDataPtr())[iSrcArg];
                DispInvokeConvertObjectToVariant(&TmpObj, &DispParams.rgvarg[iDestArg], &aByrefArgInfos[iSrcArg]); 

                // Convert the named arguments from COM+ to OLE. These arguments are in the same order
                // on both sides.
                for (i = 0; i < cNamedArgs; i++)
                {
                    iSrcArg = i;
                    iDestArg = i + 1;
                    TmpObj = ((OBJECTREF*)(*pArrArgs)->GetDataPtr())[iSrcArg];
                    DispInvokeConvertObjectToVariant(&TmpObj, &DispParams.rgvarg[iDestArg], &aByrefArgInfos[iSrcArg]); 
                }

                // Convert the unnamed arguments. These need to be presented in reverse order to IDispatch::Invoke().
                for (iSrcArg = cNamedArgs, iDestArg = cArgs - 1; iSrcArg < cArgs - 1; iSrcArg++, iDestArg--)
                {
                    TmpObj = ((OBJECTREF*)(*pArrArgs)->GetDataPtr())[iSrcArg];
                    DispInvokeConvertObjectToVariant(&TmpObj, &DispParams.rgvarg[iDestArg], &aByrefArgInfos[iSrcArg]); 
                }
            }
        }
        GCPROTECT_END();
    }
    else
    {
        // There are no arguments.
        DispParams.cArgs = cArgs;
        DispParams.cNamedArgs = 0;
        DispParams.rgdispidNamedArgs = NULL;
        DispParams.rgvarg = NULL;
    }

    // If we're calling on DISPID=-4, then pass both METHOD and PROPERTYGET
    if (MemberID == DISPID_NEWENUM)
    {
        _ASSERTE((flags & DISPATCH_METHOD) && "Expected DISPATCH_METHOD to be set.");
        flags |= DISPATCH_METHOD | DISPATCH_PROPERTYGET;
    }

    //
    // Call invoke on the target's IDispatch.
    //

    if (!bIgnoreReturn)
        pVarResult = &VarResult;

    DoIUInvokeDispMethod(pDispEx, pDisp, MemberID, lcid, flags, &DispParams, pVarResult);


    //
    // Return value handling and cleanup.
    //

    // Back propagate any byref args.
    if (bSomeArgsAreByref)
    {
        OBJECTREF TmpObj = NULL;
        GCPROTECT_BEGIN(TmpObj)
        {
            for (i = 0; i < cArgs; i++)
            {
                if (aByrefArgInfos[i].m_bByref)
                {
                    // Convert the variant back to an object.
                    OleVariant::MarshalObjectForOleVariant(&aByrefArgInfos[i].m_Val, &TmpObj);
                    (*pArrArgs)->SetAt(i, TmpObj);
                }      
            }
        }
        GCPROTECT_END();
    }

    if (!bIgnoreReturn)
    {
        if (MemberID == DISPID_NEWENUM)
        {
            //
            // Use a custom marshaler to convert the IEnumVARIANT to an IEnumerator.
            //

            // Start by making sure that the variant we got back contains an IP.
            if ((VarResult.vt != VT_UNKNOWN) || !VarResult.punkVal)
                COMPlusThrow(kInvalidCastException, IDS_EE_INVOKE_NEW_ENUM_INVALID_RETURN);

            // Have the custom marshaler do the conversion.
            *pRetVal = ConvertEnumVariantToMngEnum((IEnumVARIANT *)VarResult.punkVal);
        }
        else
        {
            // Convert the return variant to a COR variant.
            OleVariant::MarshalObjectForOleVariant(&VarResult, pRetVal);
        }
    }
}

#if defined(FEATURE_COMINTEROP_UNMANAGED_ACTIVATION) && defined(FEATURE_CLASSIC_COMINTEROP)

void GetComClassHelper(
    _Out_ OBJECTREF *pRef,
    _In_ EEClassFactoryInfoHashTable *pClassFactHash,
    _In_ ClassFactoryInfo *pClassFactInfo,
    _In_opt_ WCHAR *wszProgID)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(ThrowOutOfMemory());
        PRECONDITION(CheckPointer(pRef));
        PRECONDITION(CheckPointer(pClassFactHash));
        PRECONDITION(CheckPointer(pClassFactInfo));
        PRECONDITION(CheckPointer(wszProgID, NULL_OK));
    }
    CONTRACTL_END;

    OBJECTHANDLE hRef;
    AppDomain *pDomain = GetAppDomain();

    CrstHolder ch(pDomain->GetRefClassFactCrst());

    // Check again.
    if (pClassFactHash->GetValue(pClassFactInfo, (HashDatum *)&hRef))
    {
        *pRef = ObjectFromHandle(hRef);
    }
    else
    {
        //
        // There is no managed class for this CLSID
        // so we will create a ComClassFactory to
        // represent it.
        //

        NewHolder<ComClassFactory> pComClsFac = ComClassFactoryCreator::Create(pClassFactInfo->m_clsid);
        pComClsFac->SetManagedVersion();

        NewArrayHolder<WCHAR> wszRefProgID = NULL;
        if (wszProgID)
        {
            size_t len = wcslen(wszProgID)+1;
            wszRefProgID = new WCHAR[len];
            wcscpy_s(wszRefProgID, len, wszProgID);
        }

        NewArrayHolder<WCHAR> wszRefServer = NULL;
        if (pClassFactInfo->m_strServerName)
        {
            size_t len = wcslen(pClassFactInfo->m_strServerName)+1;
            wszRefServer = new WCHAR[len];
            wcscpy_s(wszRefServer, len, pClassFactInfo->m_strServerName);
        }

        pComClsFac->Init(wszRefProgID, wszRefServer, NULL);
        AllocateComClassObject(pComClsFac, pRef);

        // Insert to hash.
        hRef = pDomain->CreateHandle(*pRef);
        pClassFactHash->InsertValue(pClassFactInfo, (LPVOID)hRef);

        // Make sure the hash code is working.
        _ASSERTE (pClassFactHash->GetValue(pClassFactInfo, (HashDatum *)&hRef));

        wszRefProgID.SuppressRelease();
        wszRefServer.SuppressRelease();
        pComClsFac.SuppressRelease();
    }
}

//-------------------------------------------------------------
// returns a ComClass reflect class that wraps the IClassFactory
void GetComClassFromProgID(STRINGREF srefProgID, STRINGREF srefServer, OBJECTREF *pRef)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        INJECT_FAULT(COMPlusThrowOM());
        PRECONDITION(srefProgID != NULL);
        PRECONDITION(pRef != NULL);
    }
    CONTRACTL_END;

    NewArrayHolder<WCHAR>   wszProgID;
    NewArrayHolder<WCHAR>   wszServer;
    HRESULT                 hr          = S_OK;
    MethodTable*            pMT         = NULL;
    CLSID                   clsid       = {0};
    BOOL                    bServerIsLocal = (srefServer == NULL);

    //
    // Allocate strings for the ProgID and the server.
    //

    int len = srefProgID->GetStringLength();

    wszProgID = new WCHAR[len+1];

    if (len)
        memcpy(wszProgID, srefProgID->GetBuffer(), (len*2));
    wszProgID[len] = W('\0');

    if (srefServer != NULL)
    {
        len = srefServer->GetStringLength();

        wszServer = new WCHAR[len+1];

        if (len)
            memcpy(wszServer, srefServer->GetBuffer(), (len*2));
        wszServer[len] = W('\0');
    }


    //
    // Call GetCLSIDFromProgID() to convert the ProgID to a CLSID.
    //

    EnsureComStarted();
    
    {
        GCX_PREEMP();
        hr = GetCLSIDFromProgID(wszProgID, &clsid);
    }

    if (FAILED(hr))
        COMPlusThrowHR(hr);

    //
    // If no server name has been specified, see if we can find the well known 
    // managed class for this CLSID.
    //

    if (bServerIsLocal)
    {
        BOOL fAssemblyInReg = FALSE;
        // @TODO(DM): Do we really need to be this forgiving ? We should
        //            look into letting the type load exceptions percolate 
        //            up to the user instead of swallowing them and using __ComObject.
        EX_TRY
        {                
            pMT = GetTypeForCLSID(clsid, &fAssemblyInReg);
        }
        EX_CATCH
        {
        }
        EX_END_CATCH(RethrowTerminalExceptions)
    }
        
    if (pMT != NULL)
    {               
        //
        // There is a managed class for this ProgID.
        //

        *pRef = pMT->GetManagedClassObject();
    }
    else
    {
        // Check if we have in the hash.
        OBJECTHANDLE hRef;
        ClassFactoryInfo ClassFactInfo;
        ClassFactInfo.m_clsid = clsid;
        ClassFactInfo.m_strServerName = wszServer;
        EEClassFactoryInfoHashTable *pClassFactHash = GetAppDomain()->GetClassFactHash();
        
        if (pClassFactHash->GetValue(&ClassFactInfo, (HashDatum *)&hRef))
        {
            *pRef = ObjectFromHandle(hRef);
        }
        else
        {
            GetComClassHelper(pRef, pClassFactHash, &ClassFactInfo, wszProgID);
        }
    }

    // If we made it this far *pRef better be set.
    _ASSERTE(*pRef != NULL);
}

//-------------------------------------------------------------
// returns a ComClass reflect class that wraps the IClassFactory
void GetComClassFromCLSID(REFCLSID clsid, STRINGREF srefServer, OBJECTREF *pRef)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        INJECT_FAULT(COMPlusThrowOM());
        PRECONDITION(pRef != NULL);
    }
    CONTRACTL_END;

    NewArrayHolder<WCHAR>   wszServer;
    HRESULT                 hr              = S_OK;
    MethodTable*            pMT             = NULL;
    BOOL                    bServerIsLocal  = (srefServer == NULL);

    //
    // Allocate strings for the server.
    //

    if (srefServer != NULL)
    {
        int len = srefServer->GetStringLength();

        wszServer = new WCHAR[len+1];

        if (len)
            memcpy(wszServer, srefServer->GetBuffer(), (len*2));

        wszServer[len] = W('\0');
    }


    //
    // If no server name has been specified, see if we can find the well known 
    // managed class for this CLSID.
    //

    if (bServerIsLocal)
    {
        // @TODO(DM): Do we really need to be this forgiving ? We should
        //            look into letting the type load exceptions percolate 
        //            up to the user instead of swallowing them and using __ComObject.
        EX_TRY
        {
            pMT = GetTypeForCLSID(clsid);
        }
        EX_CATCH
        {
        }
        EX_END_CATCH(RethrowTerminalExceptions)
    }
              
    if (pMT != NULL)
    {               
        //
        // There is a managed class for this CLSID.
        //

        *pRef = pMT->GetManagedClassObject();
    }
    else
    {
        // Check if we have in the hash.
        OBJECTHANDLE hRef;
        ClassFactoryInfo ClassFactInfo;
        ClassFactInfo.m_clsid = clsid;
        ClassFactInfo.m_strServerName = wszServer;
        EEClassFactoryInfoHashTable *pClassFactHash = GetAppDomain()->GetClassFactHash();
        
        if (pClassFactHash->GetValue(&ClassFactInfo, (HashDatum*) &hRef))
        {
            *pRef = ObjectFromHandle(hRef);
        }
        else
        {
            GetComClassHelper(pRef, pClassFactHash, &ClassFactInfo, NULL);
        }
    }

    // If we made it this far *pRef better be set.
    _ASSERTE(*pRef != NULL);
}

#endif // FEATURE_COMINTEROP_UNMANAGED_ACTIVATION && FEATURE_CLASSIC_COMINTEROP

#endif //#ifndef CROSSGEN_COMPILE


#ifdef FEATURE_COMINTEROP_UNMANAGED_ACTIVATION
//-------------------------------------------------------------
// check if a ComClassFactory has been setup for this class
// if not set one up
ClassFactoryBase *GetComClassFactory(MethodTable* pClassMT)
{
    CONTRACT (ClassFactoryBase*)
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(ThrowOutOfMemory());
        PRECONDITION(CheckPointer(pClassMT));
        PRECONDITION(pClassMT->IsComObjectType() || pClassMT->IsExportedToWinRT());
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END;

    if (!pClassMT->IsExportedToWinRT())
    {
        // Work our way up the hierachy until we find the first COM import type.
        while (!pClassMT->IsComImport())
        {
            pClassMT = pClassMT->GetParentMethodTable();
            _ASSERTE(pClassMT != NULL);
            _ASSERTE(pClassMT->IsComObjectType());      
        }
    }

    // check if com data has been setup for this class
    ClassFactoryBase *pClsFac = pClassMT->GetComClassFactory();

    if (pClsFac == NULL)
    {
        //
        // Collectible types do not support WinRT interop
        //
        if (pClassMT->Collectible() && (pClassMT->IsExportedToWinRT() || pClassMT->IsProjectedFromWinRT()))
        {
            COMPlusThrow(kNotSupportedException, W("NotSupported_CollectibleWinRT"));
        }

        NewHolder<ClassFactoryBase> pNewFactory;

        if (pClassMT->IsExportedToWinRT())
        {
            WinRTManagedClassFactory *pWinRTMngClsFac = new WinRTManagedClassFactory(pClassMT);
            pNewFactory = pWinRTMngClsFac;

            pWinRTMngClsFac->Init();
        }
        else if (pClassMT->IsProjectedFromWinRT())
        {
            WinRTClassFactory *pWinRTClsFac = new WinRTClassFactory(pClassMT);
            pNewFactory = pWinRTClsFac;

            pWinRTClsFac->Init();
        }
        else
        {
            GUID guid;
            pClassMT->GetGuid(&guid, TRUE);

            ComClassFactory *pComClsFac = ComClassFactoryCreator::Create(guid);
                
            pNewFactory = pComClsFac;

            pComClsFac->Init(NULL, NULL, pClassMT);
        }

        // store the class factory in EE Class
        if (!pClassMT->SetComClassFactory(pNewFactory))
        {
            // another thread beat us to it
            pNewFactory = pClassMT->GetComClassFactory();
        }

        pClsFac = pNewFactory.Extract();
    }

    RETURN pClsFac;
}
#endif // FEATURE_COMINTEROP_UNMANAGED_ACTIVATION


//-------------------------------------------------------------------
// void InitializeComInterop()
// Called from EEStartup, to initialize com Interop specific data 
// structures.
//-------------------------------------------------------------------
void InitializeComInterop()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;
    
    InitializeSListHead(&RCW::s_RCWStandbyList);
    ComCall::Init();
#ifdef _TARGET_X86_
    ComPlusCall::Init();
#endif
#ifndef CROSSGEN_COMPILE
    CtxEntryCache::Init();
    ComCallWrapperTemplate::Init();
#ifdef _DEBUG
    IntializeInteropLogging();
#endif //_DEBUG
#endif //CROSSGEN_COMPILE
}

// Try to load a WinRT type.
TypeHandle LoadWinRTType(SString* ssTypeName, BOOL bThrowIfNotFound, ICLRPrivBinder* loadBinder /* =nullptr */)
{
    CONTRACT (TypeHandle)
    {
        MODE_ANY;
        THROWS;
        GC_TRIGGERS;
    }
    CONTRACT_END;

    TypeHandle typeHandle;

    SString ssAssemblyName(SString::Utf8Literal, "WindowsRuntimeAssemblyName, ContentType=WindowsRuntime");
    DomainAssembly *pAssembly = LoadDomainAssembly(&ssAssemblyName, nullptr, 
                                                   loadBinder, 
                                                   bThrowIfNotFound, ssTypeName);
    if (pAssembly != NULL)
    {
        typeHandle = TypeName::GetTypeFromAssembly(*ssTypeName, pAssembly->GetAssembly(), bThrowIfNotFound);
    }

    RETURN typeHandle;
}

// Makes a IRoSimpleMetaDataBuilder callback for a runtime class.
// static
HRESULT WinRTGuidGenerator::MetaDataLocator::LocateTypeWithDefaultInterface(MethodTable *pMT, LPCWSTR pszName, IRoSimpleMetaDataBuilder &metaDataDestination)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pMT));
        PRECONDITION(!pMT->IsInterface());
        PRECONDITION(CheckPointer(pszName));
    }
    CONTRACTL_END;

    MethodTable *pDefItfMT = pMT->GetDefaultWinRTInterface();
    if (pDefItfMT == NULL)
    {
        StackSString ss;
        TypeString::AppendType(ss, TypeHandle(pMT));
        COMPlusThrowHR(COR_E_BADIMAGEFORMAT, IDS_EE_WINRT_IID_NODEFAULTINTERFACE, ss.GetUnicode());
    }

    DefineFullyQualifiedNameForClassW();

    GUID iid;
    pDefItfMT->GetGuid(&iid, FALSE);

    if (pDefItfMT->HasInstantiation())
    {
        SArray<BYTE> namesBuf;
        PCWSTR *pNamePointers;
        COUNT_T cNames;
        PopulateNames(pDefItfMT, namesBuf, pNamePointers, cNames);

        // runtime class with generic default interface
        return metaDataDestination.SetRuntimeClassParameterizedDefault(
            pszName,
            cNames,
            pNamePointers);
    }
    else
    {
        LPCWSTR pszDefItfName = GetFullyQualifiedNameForClassW_WinRT(pDefItfMT);

        // runtime class with non-generic default interface
        return metaDataDestination.SetRuntimeClassSimpleDefault(
            pszName,
            pszDefItfName,
            &iid);
    }
}

// Makes a IRoSimpleMetaDataBuilder callback for a structure.
// static
HRESULT WinRTGuidGenerator::MetaDataLocator::LocateStructure(MethodTable *pMT, LPCWSTR pszName, IRoSimpleMetaDataBuilder &metaDataDestination)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pMT));
        PRECONDITION(CheckPointer(pszName));
    }
    CONTRACTL_END;

    SArray<BYTE> namesBuf;
    COUNT_T cNames = 0;

    ApproxFieldDescIterator fieldIterator(pMT, ApproxFieldDescIterator::INSTANCE_FIELDS);
    for (FieldDesc *pFD = fieldIterator.Next(); pFD != NULL; pFD = fieldIterator.Next())
    {
        TypeHandle th = pFD->GetApproxFieldTypeHandleThrowing();
        if (th.IsTypeDesc())
        {
            // WinRT structures should not have TypeDesc fields
            IfFailThrowBF(E_FAIL, BFA_BAD_SIGNATURE, pMT->GetModule());
        }

        PopulateNamesAppendTypeName(th.AsMethodTable(), namesBuf, cNames);
    }

    PCWSTR *pNamePointers;
    PopulateNamesAppendNamePointers(pMT, namesBuf, pNamePointers, cNames);

    return metaDataDestination.SetStruct(
        pszName,
        cNames,
        pNamePointers);
}


//
// Tables of information about the redirected types used to setup GUID marshaling
//

struct RedirectedEnumInfo
{
    LPCWSTR wszBackingField;
};

#define DEFINE_PROJECTED_TYPE(szWinRTNS, szWinRTName, szClrNS, szClrName, nClrAsmIdx, nContractAsmIdx, nWinRTIndex, nClrIndex, nWinMDTypeKind) \
    { nullptr },
#define DEFINE_PROJECTED_ENUM(szWinRTNamespace, szWinRTName, szClrNamespace, szClrName, nClrAssemblyIndex, nContractAsmIdx, WinRTRedirectedTypeIndex, ClrRedirectedTypeIndex, szBackingFieldSize) \
    { L ## szBackingFieldSize },

static const RedirectedEnumInfo g_redirectedEnumInfo[WinMDAdapter::RedirectedTypeIndex_Count] =
{
#include "winrtprojectedtypes.h"
};

#undef DEFINE_PROJECTED_TYPE
#undef DEFINE_PROJECTED_ENUM

struct RedirectedPInterfaceInfo
{
    DWORD cGenericParameters;
    const GUID IID;
};

#define DEFINE_PROJECTED_TYPE(szWinRTNS, szWinRTName, szClrNS, szClrName, nClrAsmIdx, nContractAsmIdx, nWinRTIndex, nClrIndex, nWinMDTypeKind) \
    { 0, { 0 } },
#define DEFINE_PROJECTED_INTERFACE(szWinRTNamespace, szWinRTName, szClrNamespace, szClrName, nClrAssemblyIndex, nContractAsmIdx, WinRTRedirectedTypeIndex, ClrRedirectedTypeIndex, PIID) \
    { 0, PIID },
#define DEFINE_PROJECTED_PINTERFACE(szWinRTNamespace, szWinRTName, szClrNamespace, szClrName, nClrAssemblyIndex, nContractAsmIdx, WinRTRedirectedTypeIndex, ClrRedirectedTypeIndex, GenericTypeParameterCount, PIID) \
    { GenericTypeParameterCount, PIID },

static const RedirectedPInterfaceInfo g_redirectedPInterfaceInfo[] =
{
#include "winrtprojectedtypes.h"
};

#undef DEFINE_PROJECTED_TYPE
#undef DEFINE_PROJECTED_INTERFACE
#undef DEFINE_PROJECTED_PINTERFACE

struct RedirectedPDelegateInfo
{
    const DWORD cGenericParameters;
    const GUID IID;
};

#define DEFINE_PROJECTED_TYPE(szWinRTNS, szWinRTName, szClrNS, szClrName, nClrAsmIdx, nContractAsmIdx, nWinRTIndex, nClrIndex, nWinMDTypeKind) \
    { 0, { 0 } },
#define DEFINE_PROJECTED_DELEGATE(szWinRTNamespace, szWinRTName, szClrNamespace, szClrName, nClrAssemblyIndex, nContractAsmIdx, WinRTRedirectedTypeIndex, ClrRedirectedTypeIndex, PIID) \
    { 0, PIID },
#define DEFINE_PROJECTED_PDELEGATE(szWinRTNamespace, szWinRTName, szClrNamespace, szClrName, nClrAssemblyIndex, nContractAsmIdx, WinRTRedirectedTypeIndex, ClrRedirectedTypeIndex, GenericTypeParameterCount, PIID) \
    { GenericTypeParameterCount, PIID },

static const RedirectedPDelegateInfo g_redirectedPDelegateInfo[] =
{
#include "winrtprojectedtypes.h"
};

#undef DEFINE_PROJECTED_TYPE
#undef DEFINE_PROJECTED_DELEGATE
#undef DEFINE_PROJECTED_PDELEGATE

struct RedirectedRuntimeclassInfo
{
    LPCWSTR wszDefaultIntefaceName;
    const GUID IID;
};

#define DEFINE_PROJECTED_TYPE(szWinRTNS, szWinRTName, szClrNS, szClrName, nClrAsmIdx, nContractAsmIdx, nWinRTIndex, nClrIndex, nWinMDTypeKind) \
    { nullptr, { 0 } },
#define DEFINE_PROJECTED_RUNTIMECLASS(szWinRTNamespace, szWinRTName, szClrNamespace, szClrName, nClrAssemblyIndex, nContractAsmIdx, WinRTRedirectedTypeIndex, ClrRedirectedTypeIndex, szDefaultInterfaceName, DefaultInterfaceIID) \
    { L ## szDefaultInterfaceName, DefaultInterfaceIID },

static RedirectedRuntimeclassInfo const g_redirectedRuntimeclassInfo[] =
{
#include "winrtprojectedtypes.h"
};
  
#undef DEFINE_PROJECTED_TYPE
#undef DEFINE_PROJECTED_RUNTIMECLASS

struct RedirectedStructInfo
{
    const DWORD   cFields;
    const LPCWSTR *pwzFields;
};

#define DEFINE_PROJECTED_TYPE(szWinRTNS, szWinRTName, szClrNS, szClrName, nClrAsmIdx, nContractAsmIdx, nWinRTIndex, nClrIndex, nWinMDTypeKind)
#define DEFINE_PROJECTED_STRUCT(szWinRTNamespace, szWinRTName, szClrNamespace, szClrName, nClrAssemblyIndex, nContractAsmIdx, WinRTRedirectedTypeIndex, ClrRedirectedTypeIndex, FieldSizes) \
static const LPCWSTR g_ ## WinRTRedirectedTypeIndex ## _Fields[] = { FieldSizes };
#define DEFINE_PROJECTED_JUPITER_STRUCT(szWinRTNamespace, szWinRTName, szClrNamespace, szClrName, nClrAssemblyIndex, nContractAsmIdx, WinRTRedirectedTypeIndex, ClrRedirectedTypeIndex, FieldSizes) \
static const LPCWSTR g_ ## WinRTRedirectedTypeIndex ## _Fields[] = { FieldSizes };
#include "winrtprojectedtypes.h"
#undef DEFINE_PROJECTED_TYPE
#undef DEFINE_PROJECTED_STRUCT
#undef DEFINE_PROJECTED_JUPITER_STRUCT

#define DEFINE_PROJECTED_TYPE(szWinRTNS, szWinRTName, szClrNS, szClrName, nClrAsmIdx, nContractAsmIdx, nWinRTIndex, nClrIndex, nWinMDTypeKind) \
    { 0, nullptr },
#define DEFINE_PROJECTED_STRUCT(szWinRTNamespace, szWinRTName, szClrNamespace, szClrName, nClrAssemblyIndex, nContractAsmIdx, WinRTRedirectedTypeIndex, ClrRedirectedTypeIndex, FieldSizes) \
    { COUNTOF(g_ ## WinRTRedirectedTypeIndex ## _Fields), g_ ## WinRTRedirectedTypeIndex ## _Fields },
#define DEFINE_PROJECTED_JUPITER_STRUCT(szWinRTNamespace, szWinRTName, szClrNamespace, szClrName, nClrAssemblyIndex, nContractAsmIdx, WinRTRedirectedTypeIndex, ClrRedirectedTypeIndex, FieldSizes) \
    { COUNTOF(g_ ## WinRTRedirectedTypeIndex ## _Fields), g_ ## WinRTRedirectedTypeIndex ## _Fields },

static const RedirectedStructInfo g_redirectedStructInfo[WinMDAdapter::RedirectedTypeIndex_Count] =
{
#include "winrtprojectedtypes.h"
};

#undef DEFINE_PROJECTED_TYPE
#undef DEFINE_PROJECTED_STRUCT
#undef DEFINE_PROJECTED_JUPITER_STRUCT
  
// Makes a IRoSimpleMetaDataBuilder callback for a redirected type or returns S_FALSE.
// static
HRESULT WinRTGuidGenerator::MetaDataLocator::LocateRedirectedType(
    MethodTable *              pMT, 
    IRoSimpleMetaDataBuilder & metaDataDestination)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pMT));
    }
    CONTRACTL_END;

    WinMDAdapter::RedirectedTypeIndex nRedirectedTypeIndex;
    if (!WinRTTypeNameConverter::ResolveRedirectedType(pMT, &nRedirectedTypeIndex))
    {
        // this is not a redirected type
        return S_FALSE;
    }
    
    WinMDAdapter::WinMDTypeKind typeKind;
    WinMDAdapter::GetRedirectedTypeInfo(nRedirectedTypeIndex, nullptr, nullptr, nullptr, nullptr, nullptr, &typeKind);
    switch (typeKind)
    {
    case WinMDAdapter::WinMDTypeKind_Attribute:
        {
            // not a runtime class -> throw
            StackSString ss;
            TypeString::AppendType(ss, TypeHandle(pMT));
            COMPlusThrowHR(COR_E_BADIMAGEFORMAT, IDS_EE_WINRT_IID_NODEFAULTINTERFACE, ss.GetUnicode());
        }

    case WinMDAdapter::WinMDTypeKind_Enum:
        return metaDataDestination.SetEnum(WinMDAdapter::GetRedirectedTypeFullWinRTName(nRedirectedTypeIndex),
                                           g_redirectedEnumInfo[nRedirectedTypeIndex].wszBackingField);

    case WinMDAdapter::WinMDTypeKind_Delegate:
        return metaDataDestination.SetDelegate(g_redirectedPDelegateInfo[nRedirectedTypeIndex].IID);

    case WinMDAdapter::WinMDTypeKind_PDelegate:
        return metaDataDestination.SetParameterizedDelegate(g_redirectedPDelegateInfo[nRedirectedTypeIndex].IID,
                                                            g_redirectedPDelegateInfo[nRedirectedTypeIndex].cGenericParameters);

    case WinMDAdapter::WinMDTypeKind_Interface:
        return metaDataDestination.SetWinRtInterface(g_redirectedPInterfaceInfo[nRedirectedTypeIndex].IID);

    case WinMDAdapter::WinMDTypeKind_PInterface:
        return metaDataDestination.SetParameterizedInterface(g_redirectedPInterfaceInfo[nRedirectedTypeIndex].IID,
                                                             g_redirectedPInterfaceInfo[nRedirectedTypeIndex].cGenericParameters);

    case WinMDAdapter::WinMDTypeKind_Runtimeclass:
        return metaDataDestination.SetRuntimeClassSimpleDefault(WinMDAdapter::GetRedirectedTypeFullWinRTName(nRedirectedTypeIndex),
                                                                g_redirectedRuntimeclassInfo[nRedirectedTypeIndex].wszDefaultIntefaceName,
                                                                &g_redirectedRuntimeclassInfo[nRedirectedTypeIndex].IID);
    
    case WinMDAdapter::WinMDTypeKind_Struct:
        return metaDataDestination.SetStruct(WinMDAdapter::GetRedirectedTypeFullWinRTName(nRedirectedTypeIndex),
                                             g_redirectedStructInfo[nRedirectedTypeIndex].cFields,
                                             g_redirectedStructInfo[nRedirectedTypeIndex].pwzFields);

    default:
        UNREACHABLE();
    }
}

HRESULT STDMETHODCALLTYPE WinRTGuidGenerator::MetaDataLocator::Locate(PCWSTR nameElement, IRoSimpleMetaDataBuilder &metaDataDestination) const
{
    CONTRACTL
    {
        THROWS; 
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(nameElement));
    }
    CONTRACTL_END;

    // All names that we feed to RoGetParameterizedTypeInstanceIID begin with 'T'
    // which is followed by the MethodTable address printed as a pointer (in hex).
    MethodTable *pMT;
    if (swscanf_s(nameElement, W("T%p"), (LPVOID *)&pMT) != 1)
    {
        // Except that it could be a field from a redirected structure
        if (wcscmp(nameElement, W("Windows.Foundation.TimeSpan")) == 0)
        {
            LPCWSTR pwszFields[] = { W("Int64") };
            return metaDataDestination.SetStruct(nameElement, COUNTOF(pwszFields), pwszFields);
        }
        else if (wcscmp(nameElement, W("Windows.UI.Xaml.DurationType")) == 0)
        {
            return metaDataDestination.SetEnum(nameElement, W("Int32"));
        }
        else if (wcscmp(nameElement, W("Windows.UI.Xaml.GridUnitType")) == 0)
        {
            return metaDataDestination.SetEnum(nameElement, W("Int32"));
        }
        else if (wcscmp(nameElement, W("Windows.UI.Xaml.Interop.TypeKind")) == 0)
        {
            return metaDataDestination.SetEnum(nameElement, W("Int32"));
        }
        else if (wcscmp(nameElement, W("Windows.UI.Xaml.Media.Animation.RepeatBehaviorType")) == 0)
        {
            return metaDataDestination.SetEnum(nameElement, W("Int32"));
        }
        else if (wcscmp(nameElement, W("Windows.Foundation.Numerics.Vector3")) == 0)
        {
            LPCWSTR pwszFields[] = { W("Single"), W("Single"), W("Single") };
            return metaDataDestination.SetStruct(nameElement, COUNTOF(pwszFields), pwszFields);
        }

        return E_FAIL;
    }

    // do a check for a redirected type first
    HRESULT hr = LocateRedirectedType(pMT, metaDataDestination);
    if (hr == S_OK || FAILED(hr))
    {
        // already handled by LocateRedirectedType
        return hr;
    }

    GUID iid;
    DefineFullyQualifiedNameForClassW();

    if (pMT->IsValueType())
    {
        if (pMT->IsEnum())
        {
            // enum
            StackSString ssBaseType;
            VERIFY(WinRTTypeNameConverter::AppendWinRTNameForPrimitiveType(MscorlibBinder::GetElementType(pMT->GetInternalCorElementType()), ssBaseType));

            return metaDataDestination.SetEnum(
                GetFullyQualifiedNameForClassW_WinRT(pMT),
                ssBaseType.GetUnicode());
        }
        else
        {
            // struct
            return LocateStructure(
                pMT,
                GetFullyQualifiedNameForClassW_WinRT(pMT),
                metaDataDestination);
        }
    }
    else
    {
        if (pMT->IsInterface())
        {
            pMT->GetGuid(&iid, FALSE);
            if (pMT->HasInstantiation())
            {
                // generic interface
                return metaDataDestination.SetParameterizedInterface(
                    iid,
                    pMT->GetNumGenericArgs());
            }
            else
            {
                // non-generic interface
                return metaDataDestination.SetWinRtInterface(iid);
            }
        }
        else
        {
            if (pMT->IsDelegate())
            {
                pMT->GetGuid(&iid, FALSE);
                if (pMT->HasInstantiation())
                {
                    // generic delegate
                    return metaDataDestination.SetParameterizedDelegate(
                        iid,
                        pMT->GetNumGenericArgs());
                }
                else
                {
                    // non-generic delegate
                    return metaDataDestination.SetDelegate(iid);
                }
            }
            else
            {
                // runtime class
                return LocateTypeWithDefaultInterface(
                    pMT,
                    GetFullyQualifiedNameForClassW_WinRT(pMT),
                    metaDataDestination);
            }
        }
    }
}


void WinRTGuidGenerator::PopulateNames(MethodTable *pMT, SArray<BYTE> &namesBuf, PCWSTR* &pszNames, COUNT_T &cNames)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pMT));
    }
    CONTRACTL_END;

    cNames = 0;

    // Fill namesBuf with a pile of strings.
    PopulateNamesAppendTypeName(pMT, namesBuf, cNames);
    PopulateNamesAppendNamePointers(pMT, namesBuf, pszNames, cNames);
}

void WinRTGuidGenerator::PopulateNamesAppendNamePointers(MethodTable *pMT, SArray<BYTE> &namesBuf, PCWSTR* &pszNames, COUNT_T cNames)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pMT));
    }
    CONTRACTL_END;

    // Get pointers to internal strings
    COUNT_T cbNamesOld = (COUNT_T)ALIGN_UP(namesBuf.GetCount(), sizeof(PWSTR)); // End of strings is not necessarily pointer aligned, align so that the follow on pointers are aligned.
    COUNT_T cbNamePointers = cNames * sizeof(PWSTR); // How much space do we need for the pointers to the names?
    COUNT_T cbNamesNew = cbNamesOld + cbNamePointers; // Total space.

    BYTE *pBuffer = namesBuf.OpenRawBuffer(cbNamesNew);

    // Scan through strings, and build list of pointers to them. This assumes that the strings are seperated by a single null character
    PWSTR pszName = (PWSTR)pBuffer;
    pszNames = (PCWSTR*)(pBuffer + cbNamesOld);
    for (COUNT_T i = 0; i < cNames; i++)
    {
        pszNames[i] = pszName;
        pszName += wcslen(pszName) + 1;
    }

    namesBuf.CloseRawBuffer(cbNamesNew);
}

void WinRTGuidGenerator::PopulateNamesAppendTypeName(MethodTable *pMT, SArray<BYTE> &namesBuf, COUNT_T &cNames)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pMT));
    }
    CONTRACTL_END;

    SmallStackSString name;

#ifdef _DEBUG
    pMT->CheckLoadLevel(CLASS_LOAD_EXACTPARENTS);
#endif // _DEBUG

    if (!WinRTTypeNameConverter::AppendWinRTNameForPrimitiveType(pMT, name))
    {
        if (pMT->HasInstantiation())
        {
            // get the typical instantiation
            TypeHandle typicalInst = ClassLoader::LoadTypeDefThrowing(pMT->GetModule(), 
                                                                      pMT->GetCl(),
                                                                      ClassLoader::ThrowIfNotFound,
                                                                      ClassLoader::PermitUninstDefOrRef
                                                                      , tdNoTypes
                                                                      , CLASS_LOAD_EXACTPARENTS
                                                                      );

            name.Printf(W("T%p"), typicalInst.AsPtr());
        }
        else
        {
            name.Printf(W("T%p"), (void *)pMT);
        }
    }

    COUNT_T cbNamesOld = namesBuf.GetCount();
    COUNT_T cbNewName = (COUNT_T)(name.GetCount() + 1) * 2;
    COUNT_T cbNamesNew = cbNamesOld + cbNewName;
    memcpy(namesBuf.OpenRawBuffer(cbNamesNew) + cbNamesOld, name.GetUnicode(), cbNewName);
    namesBuf.CloseRawBuffer(cbNamesNew);
    cNames++;

    if (pMT->HasInstantiation())
    {
        Instantiation inst = pMT->GetInstantiation();
        for (DWORD i = 0; i < inst.GetNumArgs(); i++)
        {
            PopulateNamesAppendTypeName(inst[i].GetMethodTable(), namesBuf, cNames);
        }
    }
}

// We need to be able to compute IIDs of generic interfaces for WinRT interop even on Win7 when we NGEN.
// Otherwise Framework assemblies that contain projected WinRT types would end up with different native
// images depending on the OS they were compiled on.
namespace ParamInstanceAPI_StaticallyLinked
{
    // make sure that paraminstanceapi.h can be dropped in without extensive modifications
    namespace std
    {
        static const NoThrow nothrow = ::nothrow;
    }

#pragma warning(push)
#pragma warning (disable: 4640)
    #include "paraminstanceapi.h"
#pragma warning(pop)
}

// Although the IRoMetaDataLocator and IRoSimpleMetaDataBuilder interfaces may currently be structurally
// equivalent, use proper wrappers instead of dirty casts. This will trigger compile errors if the two
// implementations diverge from each other in the future.
class MetaDataLocatorWrapper : public ParamInstanceAPI_StaticallyLinked::IRoMetaDataLocator
{
    class SimpleMetaDataBuilderWrapper : public ::IRoSimpleMetaDataBuilder
    {
        ParamInstanceAPI_StaticallyLinked::IRoSimpleMetaDataBuilder &m_destination;

    public:
        SimpleMetaDataBuilderWrapper(ParamInstanceAPI_StaticallyLinked::IRoSimpleMetaDataBuilder &destination)
            : m_destination(destination)
        { }

        STDMETHOD(SetWinRtInterface)(GUID iid)
        {  WRAPPER_NO_CONTRACT; return m_destination.SetWinRtInterface(iid); }
        
        STDMETHOD(SetDelegate)(GUID iid)
        {  WRAPPER_NO_CONTRACT; return m_destination.SetDelegate(iid); }

        STDMETHOD(SetInterfaceGroupSimpleDefault)(PCWSTR name, PCWSTR defaultInterfaceName, const GUID *defaultInterfaceIID)
        {  WRAPPER_NO_CONTRACT; return m_destination.SetInterfaceGroupSimpleDefault(name, defaultInterfaceName, defaultInterfaceIID); }

        STDMETHOD(SetInterfaceGroupParameterizedDefault)(PCWSTR name, UINT32 elementCount, PCWSTR *defaultInterfaceNameElements)
        {  WRAPPER_NO_CONTRACT; return m_destination.SetInterfaceGroupParameterizedDefault(name, elementCount, defaultInterfaceNameElements); }

        STDMETHOD(SetRuntimeClassSimpleDefault)(PCWSTR name, PCWSTR defaultInterfaceName, const GUID *defaultInterfaceIID)
        {  WRAPPER_NO_CONTRACT; return m_destination.SetRuntimeClassSimpleDefault(name, defaultInterfaceName, defaultInterfaceIID); }

        STDMETHOD(SetRuntimeClassParameterizedDefault)(PCWSTR name, UINT32 elementCount, const PCWSTR *defaultInterfaceNameElements)
        {  WRAPPER_NO_CONTRACT; return m_destination.SetRuntimeClassParameterizedDefault(name, elementCount, const_cast<PCWSTR *>(defaultInterfaceNameElements)); }

        STDMETHOD(SetStruct)(PCWSTR name, UINT32 numFields, const PCWSTR *fieldTypeNames)
        {  WRAPPER_NO_CONTRACT; return m_destination.SetStruct(name, numFields, const_cast<PCWSTR *>(fieldTypeNames)); }

        STDMETHOD(SetEnum)(PCWSTR name, PCWSTR baseType)
        {  WRAPPER_NO_CONTRACT; return m_destination.SetEnum(name, baseType); }

        STDMETHOD(SetParameterizedInterface)(GUID piid, UINT32 numArgs)
        {  WRAPPER_NO_CONTRACT; return m_destination.SetParameterizedInterface(piid, numArgs); }

        STDMETHOD(SetParameterizedDelegate)(GUID piid, UINT32 numArgs)
        {  WRAPPER_NO_CONTRACT; return m_destination.SetParameterizedDelegate(piid, numArgs); }
    };

    ::IRoMetaDataLocator &m_locator;

public:
    MetaDataLocatorWrapper(::IRoMetaDataLocator &locator)
        : m_locator(locator)
    { }

    STDMETHOD(Locate)(PCWSTR nameElement, ParamInstanceAPI_StaticallyLinked::IRoSimpleMetaDataBuilder &destination) const
    {
        CONTRACTL
        {
            THROWS; 
            GC_TRIGGERS;
            MODE_ANY;
        }
        CONTRACTL_END;

        SimpleMetaDataBuilderWrapper destinationWrapper(destination);
        return m_locator.Locate(nameElement, destinationWrapper);
    }
};


//--------------------------------------------------------------------------
// pGuid is filled with the constructed IID by the function.
// static
void WinRTGuidGenerator::ComputeGuidForGenericType(MethodTable *pMT, GUID *pGuid)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(pMT->SupportsGenericInterop(TypeHandle::Interop_NativeToManaged));
    }
    CONTRACTL_END;

    // throw a nice exception if the instantiation is not WinRT-legal
    if (!pMT->IsLegalNonArrayWinRTType())
    {
        StackSString ss;
        TypeString::AppendType(ss, TypeHandle(pMT));
        COMPlusThrowHR(COR_E_BADIMAGEFORMAT, IDS_EE_WINRT_IID_ILLEGALTYPE, ss.GetUnicode());
    }

    // create an array of name elements describing the type
    SArray<BYTE> namesBuf;
    PCWSTR *pNamePointers;
    COUNT_T cNames;
    PopulateNames(pMT, namesBuf, pNamePointers, cNames);

    // pass the array to the REX API
    MetaDataLocator metadataLocator;
#ifndef CROSSGEN_COMPILE
    if (WinRTSupported())
    {
        IfFailThrow(RoGetParameterizedTypeInstanceIID(
            cNames,
            pNamePointers,
            metadataLocator,
            pGuid,
            NULL));

#ifdef _DEBUG
        // assert that the two implementations computed the same Guid
        GUID pGuidForAssert;

        MetaDataLocatorWrapper metadataLocatorWrapper(metadataLocator);
        IfFailThrow(ParamInstanceAPI_StaticallyLinked::RoGetParameterizedTypeInstanceIID(
            cNames,
            pNamePointers,
            metadataLocatorWrapper,
            &pGuidForAssert,
            NULL));

        _ASSERTE_MSG(*pGuid == pGuidForAssert, "Guid computed by Win8 API does not match the one computed by statically linked RoGetParameterizedTypeInstanceIID");
#endif // _DEBUG
    }
    else
#endif //#ifndef CROSSGEN_COMPILE
    {
        // we should not be calling this on downlevel outside of NGEN
        _ASSERTE(GetAppDomain()->IsCompilationDomain());

        MetaDataLocatorWrapper metadataLocatorWrapper(metadataLocator);
        IfFailThrow(ParamInstanceAPI_StaticallyLinked::RoGetParameterizedTypeInstanceIID(
            cNames,
            pNamePointers,
            metadataLocatorWrapper,
            pGuid,
            NULL));
    }
}

// Returns MethodTable (typical instantiation) of the mscorlib copy of the specified redirected WinRT interface.
MethodTable *WinRTInterfaceRedirector::GetWinRTTypeForRedirectedInterfaceIndex(WinMDAdapter::RedirectedTypeIndex index)
{
    CONTRACT(MethodTable *)
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END;

    BinderClassID id = s_rInterfaceStubInfos[GetStubInfoIndex(index)].m_WinRTInterface;

    if ((id & NON_MSCORLIB_MARKER) == 0)
    {
        // the redirected interface lives in mscorlib
        RETURN MscorlibBinder::GetClass(id);
    }
    else
    {
        // the redirected interface lives in some other Framework assembly
        const NonMscorlibRedirectedInterfaceInfo *pInfo = &s_rNonMscorlibInterfaceInfos[id & ~NON_MSCORLIB_MARKER];
        SString assemblyQualifiedTypeName(SString::Utf8, pInfo->m_szWinRTInterfaceAssemblyQualifiedTypeName);

        RETURN TypeName::GetTypeFromAsmQualifiedName(assemblyQualifiedTypeName.GetUnicode()).GetMethodTable();
    }
}

//
MethodDesc *WinRTInterfaceRedirector::LoadMethodFromRedirectedAssembly(LPCUTF8 szAssemblyQualifiedTypeName, LPCUTF8 szMethodName)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    SString assemblyQualifiedTypeName(SString::Utf8, szAssemblyQualifiedTypeName);

    MethodTable *pMT = TypeName::GetTypeFromAsmQualifiedName(assemblyQualifiedTypeName.GetUnicode()).GetMethodTable();
    return MemberLoader::FindMethodByName(pMT, szMethodName);
}

#ifdef _DEBUG
void WinRTInterfaceRedirector::VerifyRedirectedInterfaceStubs()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    // Verify signatures of all stub methods by calling GetStubMethodForRedirectedInterface with all valid
    // combination of arguments.
    for (int i = 0; i < WinMDAdapter::RedirectedTypeIndex_Count; i++)
    {
        if (i == WinMDAdapter::RedirectedTypeIndex_System_Collections_Generic_IEnumerable ||
            i == WinMDAdapter::RedirectedTypeIndex_System_Collections_Generic_IList ||
            i == WinMDAdapter::RedirectedTypeIndex_System_Collections_Generic_IDictionary ||
            i == WinMDAdapter::RedirectedTypeIndex_System_Collections_Generic_IReadOnlyList ||
            i == WinMDAdapter::RedirectedTypeIndex_System_Collections_Generic_IReadOnlyDictionary ||
            i == WinMDAdapter::RedirectedTypeIndex_System_IDisposable)
        {
            int stubInfoIndex = GetStubInfoIndex((WinMDAdapter::RedirectedTypeIndex)i);

            // WinRT -> CLR
            for (int slot = 0; slot < s_rInterfaceStubInfos[stubInfoIndex].m_iCLRMethodCount; slot++)
            {
                GetStubMethodForRedirectedInterface((WinMDAdapter::RedirectedTypeIndex)i, slot, TypeHandle::Interop_ManagedToNative, FALSE);
            }
            if (i == WinMDAdapter::RedirectedTypeIndex_System_Collections_Generic_IList ||
                i == WinMDAdapter::RedirectedTypeIndex_System_Collections_Generic_IDictionary)
            {
                // WinRT -> CLR ICollection
                for (int slot = 0; slot < s_rInterfaceStubInfos[stubInfoIndex + s_NumRedirectedInterfaces].m_iCLRMethodCount; slot++)
                {
                    GetStubMethodForRedirectedInterface((WinMDAdapter::RedirectedTypeIndex)i, slot, TypeHandle::Interop_ManagedToNative, TRUE);
                }
            }

            // CLR -> WinRT
            for (int slot = 0; slot < s_rInterfaceStubInfos[stubInfoIndex].m_iWinRTMethodCount; slot++)
            {
                GetStubMethodForRedirectedInterface((WinMDAdapter::RedirectedTypeIndex)i, slot, TypeHandle::Interop_NativeToManaged, FALSE);
            }
        }
    }
}
#endif // _DEBUG

// Returns a MethodDesc to be used as an interop stub for the given redirected interface/slot/direction.
MethodDesc *WinRTInterfaceRedirector::GetStubMethodForRedirectedInterface(WinMDAdapter::RedirectedTypeIndex interfaceIndex,
                                                                          int slot,
                                                                          TypeHandle::InteropKind interopKind,
                                                                          BOOL fICollectionStub,
                                                                          Instantiation methodInst /*= Instantiation()*/)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(!(fICollectionStub && interopKind == TypeHandle::Interop_NativeToManaged));
    }
    CONTRACTL_END;

    int stubInfoIndex = GetStubInfoIndex(interfaceIndex);
    _ASSERTE(stubInfoIndex < s_NumRedirectedInterfaces);
    _ASSERTE(stubInfoIndex < _countof(s_rInterfaceStubInfos));

    const RedirectedInterfaceStubInfo *pStubInfo;
    pStubInfo = &s_rInterfaceStubInfos[fICollectionStub ? stubInfoIndex + s_NumRedirectedInterfaces : stubInfoIndex];

    BinderMethodID method;
    if (interopKind == TypeHandle::Interop_NativeToManaged)
    {
        _ASSERTE(slot < pStubInfo->m_iWinRTMethodCount);
        method = pStubInfo->m_rWinRTStubMethods[slot];
    }
    else
    {
        _ASSERTE(slot < pStubInfo->m_iCLRMethodCount);
        method = pStubInfo->m_rCLRStubMethods[slot];
    }

    MethodDesc *pMD;
    if ((pStubInfo->m_WinRTInterface & NON_MSCORLIB_MARKER) == 0)
    {
        if (!methodInst.IsEmpty() &&
            (method == METHOD__ITERABLE_TO_ENUMERABLE_ADAPTER__GET_ENUMERATOR_STUB ||
             method == METHOD__IVECTORVIEW_TO_IREADONLYLIST_ADAPTER__INDEXER_GET))
        {
            if (GetStructureBaseType(methodInst) != BaseType_None)
            {
                // This instantiation has ambiguous run-time behavior because it can be assigned by co-variance
                // from another instantiation in which the type argument is not an interface pointer in the WinRT
                // world. We have to use a special stub for these which performs a run-time check to see how to
                // marshal the argument.

                method = (method == METHOD__ITERABLE_TO_ENUMERABLE_ADAPTER__GET_ENUMERATOR_STUB) ?
                    METHOD__ITERABLE_TO_ENUMERABLE_ADAPTER__GET_ENUMERATOR_VARIANCE_STUB :
                    METHOD__IVECTORVIEW_TO_IREADONLYLIST_ADAPTER__INDEXER_GET_VARIANCE;
            }
        }

        pMD = MscorlibBinder::GetMethod(method);
    }
    else
    {
        // the stub method does not live in mscorlib
        const NonMscorlibRedirectedInterfaceInfo *pInfo = &s_rNonMscorlibInterfaceInfos[pStubInfo->m_WinRTInterface & ~NON_MSCORLIB_MARKER];

        pMD = LoadMethodFromRedirectedAssembly(
            (interopKind == TypeHandle::Interop_NativeToManaged) ? pInfo->m_szWinRTStubClassAssemblyQualifiedTypeName : pInfo->m_szCLRStubClassAssemblyQualifiedTypeName,
            pInfo->m_rszMethodNames[method]);
    }

#ifdef _DEBUG
    // Verify that the signature of the stub method matches the corresponding interface method.
    MethodTable *pItfMT = NULL;
    Instantiation inst = pMD->GetMethodInstantiation();

    if (interopKind == TypeHandle::Interop_NativeToManaged)
    {
        // we are interested in the WinRT interface method
        pItfMT = GetWinRTTypeForRedirectedInterfaceIndex(interfaceIndex);
    }
    else
    {
        // we are interested in the CLR interface method
        if (fICollectionStub)
        {
            if (pMD->HasMethodInstantiation())
            {
                if (interfaceIndex == WinMDAdapter::RedirectedTypeIndex_Windows_Foundation_Collections_IVectorView ||
                    interfaceIndex == WinMDAdapter::RedirectedTypeIndex_Windows_Foundation_Collections_IMapView)
                    pItfMT = MscorlibBinder::GetExistingClass(CLASS__IREADONLYCOLLECTIONGENERIC);
                else
                    pItfMT = MscorlibBinder::GetExistingClass(CLASS__ICOLLECTIONGENERIC);

                if (interfaceIndex == WinMDAdapter::RedirectedTypeIndex_System_Collections_Generic_IDictionary ||
                    interfaceIndex == WinMDAdapter::RedirectedTypeIndex_System_Collections_Generic_IReadOnlyDictionary)
                {                
                    TypeHandle thKvPair = TypeHandle(MscorlibBinder::GetClass(CLASS__KEYVALUEPAIRGENERIC)).Instantiate(inst);
                    inst = Instantiation(&thKvPair, 1);
                }
            }
            else
            {
                pItfMT = MscorlibBinder::GetExistingClass(CLASS__ICOLLECTION);
            }
        }
        else
        {
            pItfMT = GetAppDomain()->GetRedirectedType(interfaceIndex);
        }
    }

    // get signature of the stub method
    PCCOR_SIGNATURE pSig1;
    DWORD cSig1;

    pMD->GetSig(&pSig1, &cSig1);
    SigTypeContext typeContext1;
    SigTypeContext::InitTypeContext(Instantiation(), pMD->GetMethodInstantiation(), &typeContext1);
    MetaSig sig1(pSig1, cSig1, pMD->GetModule(), &typeContext1);

    // get signature of the interface method
    PCCOR_SIGNATURE pSig2;
    DWORD cSig2;

    MethodDesc *pItfMD = pItfMT->GetMethodDescForSlot(slot);
    pItfMD->GetSig(&pSig2, &cSig2);
    SigTypeContext typeContext2;
    SigTypeContext::InitTypeContext(inst, Instantiation(), &typeContext2);
    MetaSig sig2(pSig2, cSig2, pItfMD->GetModule(), &typeContext2);

    _ASSERTE_MSG(MetaSig::CompareMethodSigs(sig1, sig2, FALSE), "Stub method signature does not match the corresponding interface method.");
#endif // _DEBUG

    if (!methodInst.IsEmpty())
    {
        _ASSERTE(pMD->HasMethodInstantiation());
        _ASSERTE(pMD->GetNumGenericMethodArgs() == methodInst.GetNumArgs());

        pMD = MethodDesc::FindOrCreateAssociatedMethodDesc(
            pMD,
            pMD->GetMethodTable(),
            FALSE,                 // forceBoxedEntryPoint
            methodInst,            // methodInst
            FALSE,                 // allowInstParam
            TRUE);                 // forceRemotableMethod
    }

    return pMD;
}

// static
MethodDesc *WinRTInterfaceRedirector::GetStubMethodForRedirectedInterfaceMethod(MethodDesc *pMD, TypeHandle::InteropKind interopKind)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    MethodTable *pMT = pMD->GetMethodTable();

    //
    // If we are calling into a class method instead of interface method,
    // convert it to first implemented interface method (we are always calling into the first
    // one - see code:ComPlusCall::PopulateComPlusCallMethodDesc for more details)
    //
    if (!pMT->IsInterface())
    {
        pMD = pMD->GetInterfaceMD();
        pMT = pMD->GetMethodTable();
    }    

    bool fICollectionStub = false;
    if (interopKind == TypeHandle::Interop_ManagedToNative)
    {
        MethodTable *pResolvedMT = RCW::ResolveICollectionInterface(pMT, TRUE /* fPreferIDictionary */, NULL);
        if (pResolvedMT != NULL)
        {
            fICollectionStub = true;
            pMT = pResolvedMT;
        }
    }

    WinMDAdapter::RedirectedTypeIndex index;
    if (WinRTInterfaceRedirector::ResolveRedirectedInterface(pMT, &index))
    {
        // make sure we return an exact MD that takes no extra instantiating arguments
        return WinRTInterfaceRedirector::GetStubMethodForRedirectedInterface(
            index,
            pMD->GetSlot(),
            interopKind,
            fICollectionStub,
            pMT->GetInstantiation());
    }

    return NULL;
}

// static
MethodTable *WinRTDelegateRedirector::GetWinRTTypeForRedirectedDelegateIndex(WinMDAdapter::RedirectedTypeIndex index)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    switch (index)
    {
    case WinMDAdapter::RedirectedTypeIndex_System_EventHandlerGeneric:
        return MscorlibBinder::GetClass(CLASS__WINDOWS_FOUNDATION_EVENTHANDLER);
        
    case WinMDAdapter::RedirectedTypeIndex_System_Collections_Specialized_NotifyCollectionChangedEventHandler:
    {
        SString assemblyQualifiedTypeName(SString::Utf8, NCCEHWINRT_ASM_QUAL_TYPE_NAME);
        return TypeName::GetTypeFromAsmQualifiedName(assemblyQualifiedTypeName.GetUnicode()).GetMethodTable();
    }

    case WinMDAdapter::RedirectedTypeIndex_System_ComponentModel_PropertyChangedEventHandler:
    {
        SString assemblyQualifiedTypeName(SString::Utf8, PCEHWINRT_ASM_QUAL_TYPE_NAME);
        return TypeName::GetTypeFromAsmQualifiedName(assemblyQualifiedTypeName.GetUnicode()).GetMethodTable();
    }

    default:
        UNREACHABLE();
    }
}

#ifndef CROSSGEN_COMPILE

#ifdef _DEBUG
//-------------------------------------------------------------------
// LOGGING APIS
//-------------------------------------------------------------------

static int g_TraceCount = 0;
static IUnknown* g_pTraceIUnknown = 0;

VOID IntializeInteropLogging()
{
    WRAPPER_NO_CONTRACT;
    
    g_pTraceIUnknown = g_pConfig->GetTraceIUnknown();
    g_TraceCount = g_pConfig->GetTraceWrapper();
}

VOID LogInterop(__in_z LPCSTR szMsg)
{
    LIMITED_METHOD_CONTRACT;
    LOG( (LF_INTEROP, LL_INFO10, "%s\n",szMsg) );
}

VOID LogInterop(__in_z LPCWSTR wszMsg)
{
    LIMITED_METHOD_CONTRACT;
    LOG( (LF_INTEROP, LL_INFO10, "%S\n", wszMsg) );
}

//-------------------------------------------------------------------
// VOID LogRCWCreate(RCW* pWrap, IUnknown* pUnk)
// log wrapper create
//-------------------------------------------------------------------
VOID LogRCWCreate(RCW* pWrap, IUnknown* pUnk)
{
    if (!LoggingOn(LF_INTEROP, LL_ALWAYS))  
        return;

    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;
        
    static int count = 0;
    LPVOID pCurrCtx = GetCurrentCtxCookie();

    // pre-increment the count, so it can never be zero
    count++;

    if (count == g_TraceCount)
    {
        g_pTraceIUnknown = pUnk;
    }

    if (g_pTraceIUnknown == 0 || g_pTraceIUnknown == pUnk)
    {
        LOG( (LF_INTEROP,
            LL_INFO10,
            "Create RCW: Wrapper %p #%d IUnknown:%p Context %p\n",
            pWrap, count,
            pUnk,
            pCurrCtx) );
    }
}

//-------------------------------------------------------------------
// VOID LogRCWMinorCleanup(RCW* pWrap)
// log wrapper minor cleanup
//-------------------------------------------------------------------
VOID LogRCWMinorCleanup(RCW* pWrap)
{
    if (!LoggingOn(LF_INTEROP, LL_ALWAYS))  
        return;

    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(CheckPointer(pWrap));
    }
    CONTRACTL_END;

    static int dest_count = 0;
    dest_count++;

    IUnknown *pUnk = pWrap->GetRawIUnknown_NoAddRef_NoThrow();

    if (g_pTraceIUnknown == 0 || g_pTraceIUnknown == pUnk)
    {
        LPVOID pCurrCtx = GetCurrentCtxCookie();
        LOG( (LF_INTEROP,
            LL_INFO10,
            "Minor Cleanup RCW: Wrapper %p #%d IUnknown %p Context: %p\n",
            pWrap, dest_count,
            pUnk,
            pCurrCtx) );
    }
}

//-------------------------------------------------------------------
// VOID LogRCWDestroy(RCW* pWrap, IUnknown* pUnk)
// log wrapper destroy
//-------------------------------------------------------------------
VOID LogRCWDestroy(RCW* pWrap)
{
    if (!LoggingOn(LF_INTEROP, LL_ALWAYS))  
        return;

    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(CheckPointer(pWrap));
    }
    CONTRACTL_END;
    
    static int dest_count = 0;
    dest_count++;

    IUnknown *pUnk = pWrap->GetRawIUnknown_NoAddRef_NoThrow();

    if (g_pTraceIUnknown == 0 || g_pTraceIUnknown == pUnk)
    {
        LPVOID pCurrCtx = GetCurrentCtxCookie();
        STRESS_LOG4(
            LF_INTEROP,
            LL_INFO10,
            "Destroy RCW: Wrapper %p #%d IUnknown %p Context: %p\n",
            pWrap, dest_count,
            pUnk,
            pCurrCtx);
    }
}

//-------------------------------------------------------------------
// VOID LogInteropLeak(IUnkEntry * pEntry)
//-------------------------------------------------------------------
VOID LogInteropLeak(IUnkEntry * pEntry)
{
    if (!LoggingOn(LF_INTEROP, LL_ALWAYS))  
        return;

    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(CheckPointer(pEntry));
    }
    CONTRACTL_END;

    IUnknown *pUnk = pEntry->GetRawIUnknown_NoAddRef_NoThrow();

    if (g_pTraceIUnknown == 0 || g_pTraceIUnknown == pUnk)
    {
        LOG( (LF_INTEROP,
            LL_INFO10,
            "IUnkEntry Leak: %p Context: %p\n",
            pUnk,
            pEntry->GetCtxCookie()) );
    }
}

//-------------------------------------------------------------------
//  VOID LogInteropLeak(IUnknown* pItf)
//-------------------------------------------------------------------
VOID LogInteropLeak(IUnknown* pItf)
{
    if (!LoggingOn(LF_INTEROP, LL_ALWAYS))  
        return;

    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    LPVOID              pCurrCtx    = NULL;

    if (g_pTraceIUnknown == 0 || g_pTraceIUnknown == pItf)
    {
        pCurrCtx = GetCurrentCtxCookie();
        LOG((LF_INTEROP,
            LL_EVERYTHING,
            "Leak: Itf = %p, CurrCtx = %p\n",
            pItf, pCurrCtx));
    }
}

//-------------------------------------------------------------------
// VOID LogInteropQI(IUnknown* pItf, REFIID iid, HRESULT hr, LPCSTR szMsg)
//-------------------------------------------------------------------
VOID LogInteropQI(IUnknown* pItf, REFIID iid, HRESULT hrArg, __in_z LPCSTR szMsg)
{
    if (!LoggingOn(LF_INTEROP, LL_ALWAYS))  
        return;

    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pItf));
    }
    CONTRACTL_END;

    LPVOID              pCurrCtx    = NULL;
    HRESULT             hr          = S_OK;
    SafeComHolder<IUnknown> pUnk        = NULL;
    int                 cch         = 0;
    WCHAR               wszIID[64];

    hr = SafeQueryInterface(pItf, IID_IUnknown, &pUnk);

    if (g_pTraceIUnknown == 0 || g_pTraceIUnknown == pUnk)
    {
        pCurrCtx = GetCurrentCtxCookie();

        cch = StringFromGUID2(iid, wszIID, sizeof(wszIID) / sizeof(WCHAR));
        _ASSERTE(cch > 0);

        if (SUCCEEDED(hrArg))
        {
            LOG((LF_INTEROP,
                LL_EVERYTHING,
                "Succeeded QI: Unk = %p, Itf = %p, CurrCtx = %p, IID = %S, Msg: %s\n",
                (IUnknown*)pUnk, pItf, pCurrCtx, wszIID, szMsg));
        }
        else
        {
            LOG((LF_INTEROP,
                LL_EVERYTHING,
                "Failed QI: Unk = %p, Itf = %p, CurrCtx = %p, IID = %S, HR = %p, Msg: %s\n",
                (IUnknown*)pUnk, pItf, pCurrCtx, wszIID, hrArg, szMsg));
        }
    }
}

//-------------------------------------------------------------------
//  VOID LogInteropAddRef(IUnknown* pItf, ULONG cbRef, LPCSTR szMsg)
//-------------------------------------------------------------------
VOID LogInteropAddRef(IUnknown* pItf, ULONG cbRef, __in_z LPCSTR szMsg)
{
    if (!LoggingOn(LF_INTEROP, LL_ALWAYS))  
        return;

    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pItf));
    }
    CONTRACTL_END;
    
    LPVOID              pCurrCtx    = NULL;
    HRESULT             hr          = S_OK;
    SafeComHolder<IUnknown> pUnk        = NULL;

    hr = SafeQueryInterface(pItf, IID_IUnknown, &pUnk);

    if (g_pTraceIUnknown == 0 || g_pTraceIUnknown == pUnk)
    {
        pCurrCtx = GetCurrentCtxCookie();
        LOG((LF_INTEROP,
            LL_EVERYTHING,
            "AddRef: Unk = %p, Itf = %p, CurrCtx = %p, RefCount = %d, Msg: %s\n",
            (IUnknown*)pUnk, pItf, pCurrCtx, cbRef, szMsg));
    }
}

//-------------------------------------------------------------------
//  VOID LogInteropRelease(IUnknown* pItf, ULONG cbRef, LPCSTR szMsg)
//-------------------------------------------------------------------
VOID LogInteropRelease(IUnknown* pItf, ULONG cbRef, __in_z LPCSTR szMsg)
{
    if (!LoggingOn(LF_INTEROP, LL_ALWAYS))  
        return;

    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(CheckPointer(pItf, NULL_OK));
    }
    CONTRACTL_END;
    
    LPVOID pCurrCtx = NULL;

    if (g_pTraceIUnknown == 0 || g_pTraceIUnknown == pItf)
    {
        pCurrCtx = GetCurrentCtxCookie();
        LOG((LF_INTEROP,
            LL_EVERYTHING,
            "Release: Itf = %p, CurrCtx = %p, RefCount = %d, Msg: %s\n",
            pItf, pCurrCtx, cbRef, szMsg));
    }
}

#endif // _DEBUG

IUnknown* MarshalObjectToInterface(OBJECTREF* ppObject, MethodTable* pItfMT, MethodTable* pClassMT, DWORD dwFlags)
{
    CONTRACTL
    {
        THROWS;
        MODE_COOPERATIVE;
        GC_TRIGGERS;
    }
    CONTRACTL_END;
    
    // When an interface method table is specified, fDispIntf must be consistent with the
    // interface type.
    BOOL bDispatch = (dwFlags & ItfMarshalInfo::ITF_MARSHAL_DISP_ITF);
    BOOL bInspectable = (dwFlags & ItfMarshalInfo::ITF_MARSHAL_INSP_ITF);
    BOOL bUseBasicItf = (dwFlags & ItfMarshalInfo::ITF_MARSHAL_USE_BASIC_ITF);
    
    _ASSERTE(!pItfMT || (!pItfMT->IsInterface() && bDispatch) ||
             (!!bDispatch == IsDispatchBasedItf(pItfMT->GetComInterfaceType())) ||
             (!!bInspectable == (pItfMT->GetComInterfaceType() == ifInspectable) || pItfMT->IsWinRTRedirectedInterface(TypeHandle::Interop_ManagedToNative)));

    if (pItfMT)
    {
        return GetComIPFromObjectRef(ppObject, pItfMT);
    }
    else if (!bUseBasicItf)
    {
        return GetComIPFromObjectRef(ppObject, pClassMT);
    }
    else
    {
        ComIpType ReqIpType = bDispatch ? ComIpType_Dispatch : (bInspectable ? ComIpType_Inspectable : ComIpType_Unknown);
        return GetComIPFromObjectRef(ppObject, ReqIpType, NULL);
    }
}

void UnmarshalObjectFromInterface(OBJECTREF *ppObjectDest, IUnknown **ppUnkSrc, MethodTable *pItfMT, MethodTable *pClassMT, DWORD dwFlags)
{
    CONTRACTL
    {
        THROWS;
        MODE_COOPERATIVE;
        GC_TRIGGERS;
        PRECONDITION(IsProtectedByGCFrame(ppObjectDest));
    }
    CONTRACTL_END;
    
    _ASSERTE(!pClassMT || !pClassMT->IsInterface());

    bool fIsInterface = (pItfMT != NULL && pItfMT->IsInterface());

    DWORD dwObjFromComIPFlags = ObjFromComIP::FromItfMarshalInfoFlags(dwFlags);
    GetObjectRefFromComIP(
        ppObjectDest,                  // Object
        ppUnkSrc,                      // Interface pointer
        pClassMT,                      // Class type
        fIsInterface ? pItfMT : NULL,  // Interface type - used to cache the incoming interface pointer
        dwObjFromComIPFlags            // Flags
        );
    
    // Make sure the interface is supported.
    _ASSERTE(!pItfMT || pItfMT->IsInterface() || pItfMT->GetComClassInterfaceType() != clsIfNone);

    if (fIsInterface)
    {
        if ((dwFlags & ItfMarshalInfo::ITF_MARSHAL_WINRT_SCENARIO) == 0)
        {
            // We only verify that the object supports the interface for non-WinRT scenarios because we
            // believe that the likelihood of improperly constructed programs is significantly lower
            // with WinRT and the Object::SupportsInterface check is very expensive.
            if (!Object::SupportsInterface(*ppObjectDest, pItfMT))
            {
                COMPlusThrowInvalidCastException(ppObjectDest, TypeHandle(pItfMT));
            }
        }
    }
}

#ifdef FEATURE_CLASSIC_COMINTEROP

//--------------------------------------------------------------------------------
//  Check if the pUnk implements IProvideClassInfo and try to figure
// out the class from there
MethodTable* GetClassFromIProvideClassInfo(IUnknown* pUnk)
{
    CONTRACT (MethodTable*)
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pUnk));
        POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
    }
    CONTRACT_END;
    
    MethodTable*                    pClassMT    = NULL;
    SafeComHolder<ITypeInfo>            pTypeInfo   = NULL;
    SafeComHolder<IProvideClassInfo>    pclsInfo    = NULL;

    // Use IProvideClassInfo to detect the appropriate class to use for wrapping
    HRESULT hr = SafeQueryInterface(pUnk, IID_IProvideClassInfo, (IUnknown **)&pclsInfo);
    LogInteropQI(pUnk, IID_IProvideClassInfo, hr, "GetClassFromIProvideClassInfo: QIing for IProvideClassinfo");
    if (hr == S_OK && pclsInfo)
    {
        hr = E_FAIL;                    

        // Make sure the class info is not our own 
        if (!IsSimpleTearOff(pclsInfo))
        {
            GCX_PREEMP();

            hr = pclsInfo->GetClassInfo(&pTypeInfo);
        }

        // If we succeded in retrieving the type information then keep going.
        TYPEATTRHolder ptattr(pTypeInfo);
        if (hr == S_OK && pTypeInfo)
        {
            {
            GCX_PREEMP();
            hr = pTypeInfo->GetTypeAttr(&ptattr);
            }
        
            // If we succeeded in retrieving the attributes and they represent
            // a CoClass, then look up the class from the CLSID.
            if (hr == S_OK && ptattr->typekind == TKIND_COCLASS)
            {
                GCX_ASSERT_COOP();
                pClassMT = GetTypeForCLSID(ptattr->guid);
            }
        }
    }

    RETURN pClassMT;
}

#endif // FEATURE_CLASSIC_COMINTEROP


enum IInspectableQueryResults {
    IInspectableQueryResults_SupportsIReference      = 0x1,
    IInspectableQueryResults_SupportsIReferenceArray = 0x2,
};

//--------------------------------------------------------------------------------
// Try to get the class from IInspectable. If *pfSupportsIInspectable is true, pUnk
// is assumed to be an IInspectable-derived interface. Otherwise, this function will
// QI for IInspectable and set *pfSupportsIInspectable accordingly.

TypeHandle GetClassFromIInspectable(IUnknown* pUnk, bool *pfSupportsIInspectable, bool *pfSupportsIReference, bool *pfSupportsIReferenceArray)
{
    CONTRACT (TypeHandle)
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pUnk));
        PRECONDITION(CheckPointer(pfSupportsIInspectable));
        PRECONDITION(CheckPointer(pfSupportsIReference));
        PRECONDITION(CheckPointer(pfSupportsIReferenceArray));
    }
    CONTRACT_END;
    
    *pfSupportsIReference = false;
    *pfSupportsIReferenceArray = false;
    
    HRESULT hr = S_OK;

    SafeComHolder<IInspectable> pInsp = NULL;
    if (*pfSupportsIInspectable)
    {
        // we know that pUnk is an IInspectable
        pInsp = static_cast<IInspectable *>(pUnk);
        pInsp.SuppressRelease();
    }
    else
    {
        hr = SafeQueryInterface(pUnk, IID_IInspectable, (IUnknown **)&pInsp);
        LogInteropQI(pUnk, IID_IInspectable, hr, "GetClassFromIInspectable: QIing for IInspectable");
        
        if (SUCCEEDED(hr))
        {
            *pfSupportsIInspectable = true;
        }
        else
        {
            RETURN TypeHandle();
        }
    }

    WinRtString winrtClassName;
    {
        GCX_PREEMP();
        if (FAILED(pInsp->GetRuntimeClassName(winrtClassName.Address())))
        {
            RETURN TypeHandle();
        }
    }

    // Early return if the class name is NULL
    if (winrtClassName == NULL)
        RETURN TypeHandle();
    
    // we have a class name
    UINT32 cchClassName;
    LPCWSTR pwszClassName = winrtClassName.GetRawBuffer(&cchClassName);
    SString ssClassName(SString::Literal, pwszClassName, cchClassName);

    
    // Check a cache to see if this has already been looked up.
    AppDomain *pDomain = GetAppDomain();
    UINT vCacheVersion = 0;
    BYTE bFlags;
    TypeHandle classTypeHandle = pDomain->LookupTypeByName(ssClassName, &vCacheVersion, &bFlags);

    if (!classTypeHandle.IsNull())
    {
        *pfSupportsIReference = ((bFlags & IInspectableQueryResults_SupportsIReference) != 0);
        *pfSupportsIReferenceArray = ((bFlags & IInspectableQueryResults_SupportsIReferenceArray) != 0);
    }
    else     
    {
        // use a copy of the original class name in case we peel off IReference/IReferenceArray below
        StackSString ssTmpClassName;

        // Check whether this is a value type, String, or T[] "boxed" in a IReference<T> or IReferenceArray<T>.
        if (ssClassName.BeginsWith(W("Windows.Foundation.IReference`1<")) && ssClassName.EndsWith(W(">")))
        {            
            ssTmpClassName.Set(ssClassName);
            ssTmpClassName.Delete(ssTmpClassName.Begin(), _countof(W("Windows.Foundation.IReference`1<")) - 1);
            ssTmpClassName.Delete(ssTmpClassName.End() - 1, 1);
            *pfSupportsIReference = true;
        }
        else if (ssClassName.BeginsWith(W("Windows.Foundation.IReferenceArray`1<")) && ssClassName.EndsWith(W(">")))
        {
            ssTmpClassName.Set(ssClassName);
            ssTmpClassName.Delete(ssTmpClassName.Begin(), _countof(W("Windows.Foundation.IReferenceArray`1<")) - 1);
            ssTmpClassName.Delete(ssTmpClassName.End() - 1, 1);
            *pfSupportsIReferenceArray = true;
        }

        EX_TRY
        {
            LPCWSTR pszWinRTTypeName = (ssTmpClassName.IsEmpty() ? ssClassName  : ssTmpClassName);
            classTypeHandle = WinRTTypeNameConverter::LoadManagedTypeForWinRTTypeName(pszWinRTTypeName, /* pLoadBinder */ nullptr, /*pbIsPrimitive = */ nullptr);
        }
        EX_CATCH
        {
        }
        EX_END_CATCH(RethrowTerminalExceptions)

        if (!classTypeHandle.IsNull())
        {
            // cache the (positive) result
            BYTE bFlags = 0;
            if (*pfSupportsIReference)
                bFlags |= IInspectableQueryResults_SupportsIReference;
            if (*pfSupportsIReferenceArray)
                bFlags |= IInspectableQueryResults_SupportsIReferenceArray;
            pDomain->CacheTypeByName(ssClassName, vCacheVersion, classTypeHandle, bFlags);
        }
    }
    
    RETURN classTypeHandle;
}


ABI::Windows::Foundation::IUriRuntimeClass *CreateWinRTUri(LPCWSTR wszUri, INT32 cchUri)
{
    STANDARD_VM_CONTRACT;

    UriMarshalingInfo* marshalingInfo = GetAppDomain()->GetLoaderAllocator()->GetMarshalingData()->GetUriMarshalingInfo();
        
    // Get the cached factory from the UriMarshalingInfo object of the current appdomain
    ABI::Windows::Foundation::IUriRuntimeClassFactory* pFactory = marshalingInfo->GetUriFactory();

    SafeComHolder<ABI::Windows::Foundation::IUriRuntimeClass> pIUriRC;
    HRESULT hrCreate = pFactory->CreateUri(WinRtStringRef(wszUri, cchUri), &pIUriRC);
    if (FAILED(hrCreate))
    {
        if (hrCreate == E_INVALIDARG)
        {
            COMPlusThrow(kArgumentException, IDS_EE_INVALIDARG_WINRT_INVALIDURI);
        }
        else
        {
            ThrowHR(hrCreate);
        }
    }

    return pIUriRC.Extract();
}

static void DECLSPEC_NORETURN ThrowTypeLoadExceptionWithInner(MethodTable *pClassMT, LPCWSTR pwzName, HRESULT hr, unsigned resID)
{
    CONTRACTL
    {
        THROWS;
        DISABLED(GC_NOTRIGGER);  // Must sanitize first pass handling to enable this
        MODE_ANY;
    }
    CONTRACTL_END;

    StackSString simpleName(SString::Utf8, pClassMT->GetAssembly()->GetSimpleName());

    EEMessageException ex(hr);
    EX_THROW_WITH_INNER(EETypeLoadException, (pwzName, simpleName.GetUnicode(), nullptr, resID), &ex);
}

//
// Creates activation factory and wraps it with a RCW
//
void GetNativeWinRTFactoryObject(MethodTable *pMT, Thread *pThread, MethodTable *pFactoryIntfMT, BOOL bNeedUniqueRCW, ICOMInterfaceMarshalerCallback *pCallback, OBJECTREF *prefFactory)
{
    CONTRACTL
    {
        THROWS;
        MODE_COOPERATIVE;
        GC_TRIGGERS;
        PRECONDITION(CheckPointer(pMT));
        PRECONDITION(CheckPointer(pThread));
        PRECONDITION(CheckPointer(pFactoryIntfMT, NULL_OK));
        PRECONDITION(CheckPointer(pCallback, NULL_OK));        
    }
    CONTRACTL_END;

    if (!WinRTSupported())
    {
        COMPlusThrow(kPlatformNotSupportedException, W("PlatformNotSupported_WinRT"));
    }

    HSTRING hName = GetComClassFactory(pMT)->AsWinRTClassFactory()->GetClassName();

    HRESULT hr;
    SafeComHolder<IInspectable> pFactory;        
    {            
        GCX_PREEMP();
        hr = clr::winrt::GetActivationFactory<IInspectable>(hName, &pFactory);
    }

    // There are a few particular failures that we'd like to map a specific exception type to
    //   - the factory interface is for a WinRT type which is not registered => TypeLoadException
    //   - the factory interface is not a factory for the WinRT type => ArgumentException
    if (hr == REGDB_E_CLASSNOTREG)
    {
        ThrowTypeLoadExceptionWithInner(pMT, WindowsGetStringRawBuffer(hName, nullptr), hr, IDS_EE_WINRT_TYPE_NOT_REGISTERED);
    }
    else if (hr == E_NOINTERFACE)
    {
        LPCWSTR wzTN = WindowsGetStringRawBuffer(hName, nullptr);
        if (pFactoryIntfMT)
        {
            InlineSString<DEFAULT_NONSTACK_CLASSNAME_SIZE> ssFactoryName;
            pFactoryIntfMT->_GetFullyQualifiedNameForClass(ssFactoryName);
            EEMessageException ex(hr);
            EX_THROW_WITH_INNER(EEMessageException, (kArgumentException, IDS_EE_WINRT_NOT_FACTORY_FOR_TYPE, ssFactoryName.GetUnicode(), wzTN), &ex);
        }
        else
        {
            EEMessageException ex(hr);
            EX_THROW_WITH_INNER(EEMessageException, (kArgumentException, IDS_EE_WINRT_INVALID_FACTORY_FOR_TYPE, wzTN), &ex);            
        }
    }
    else
    {
        IfFailThrow(hr);
    }
   
    DWORD flags =
        RCW::CF_SupportsIInspectable |          // Returns a WinRT RCW
        RCW::CF_DontResolveClass;               // Don't care about the exact type

    flags |= RCW::CF_DetectDCOMProxy;           // Attempt to detect that the factory is a DCOM proxy in order to suppress caching

    if (bNeedUniqueRCW)
        flags |= RCW::CF_NeedUniqueObject;      // Returns a unique RCW

    COMInterfaceMarshaler marshaler;
    marshaler.Init(
        pFactory, 
        g_pBaseCOMObject,                       // Always System.__ComObject
        pThread,
        flags
        );

    if (pCallback)
        marshaler.SetCallback(pCallback);
    
    // Find an existing RCW or create a new RCW
    *prefFactory = marshaler.FindOrCreateObjectRef(pFactory);

    return;
}                                

#endif //#ifndef CROSSGEN_COMPILE


#endif // FEATURE_COMINTEROP
