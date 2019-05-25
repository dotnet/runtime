// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "stdafx.h"                     // Standard header.
#include <utilcode.h>                   // Utility helpers.
#include <corerror.h>
#include "ndpversion.h"

#include "../dlls/mscorrc/resource.h"
#ifdef FEATURE_PAL
#include "resourcestring.h"
#define NATIVE_STRING_RESOURCE_NAME mscorrc_debug
__attribute__((visibility("default"))) DECLARE_NATIVE_STRING_RESOURCE_TABLE(NATIVE_STRING_RESOURCE_NAME);
#endif
#include "sstring.h"
#include "stringarraylist.h"

#include <stdlib.h>

#ifdef USE_FORMATMESSAGE_WRAPPER
// we implement the wrapper for FormatMessageW. 
// Need access to the original 
#undef WszFormatMessage
#define WszFormatMessage ::FormatMessageW
#endif

#define MAX_VERSION_STRING 30

// External prototypes.
extern HINSTANCE GetModuleInst();

#ifndef FEATURE_PAL

//*****************************************************************************
// Get the MUI ID, on downlevel platforms where MUI is not supported it
// returns the default system ID.

typedef LANGID (WINAPI *PFNGETUSERDEFAULTUILANGUAGE)(void);  // kernel32!GetUserDefaultUILanguage

int GetMUILanguageID(LocaleIDValue* pResult)
{
    CONTRACTL
    {
        GC_NOTRIGGER;
        NOTHROW;
#ifdef      MODE_PREEMPTIVE
        MODE_PREEMPTIVE;
#endif
    }
    CONTRACTL_END;

    _ASSERTE(sizeof(LocaleID)/sizeof(WCHAR) >=LOCALE_NAME_MAX_LENGTH);
    return ::GetSystemDefaultLocaleName(*pResult, LOCALE_NAME_MAX_LENGTH);
}

static void BuildMUIDirectory(int langid, __out SString* pResult)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        PRECONDITION(CheckPointer(pResult));
    }
    CONTRACTL_END;
    
    pResult->Printf(W("MUI\\%04x\\"), langid);
}

void GetMUILanguageName(__out SString* pResult)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        PRECONDITION(CheckPointer(pResult));
    }
    CONTRACTL_END;

    LocaleIDValue langid;
    GetMUILanguageID(&langid);

    int lcid = ::LocaleNameToLCID(langid,0);
    return BuildMUIDirectory(lcid, pResult);
}
 
void GetMUIParentLanguageName(SString* pResult)
{
    WRAPPER_NO_CONTRACT;
    int langid = 1033;

    BuildMUIDirectory(langid, pResult);
}
#ifndef DACCESS_COMPILE
HRESULT GetMUILanguageNames(__inout StringArrayList* pCultureNames)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        PRECONDITION(CheckPointer(pCultureNames));
    } 
    CONTRACTL_END;

    HRESULT hr=S_OK;
    EX_TRY
    {
        SString result;
        GetMUILanguageName(&result);

        if(!result.IsEmpty())
        {
            pCultureNames->Append(result);
        }
        
        GetMUIParentLanguageName(&result);

        _ASSERTE(!result.IsEmpty());
        pCultureNames->Append(result);
        pCultureNames->Append(SString::Empty());
    }
    EX_CATCH_HRESULT(hr)
    return hr;
    
}
#endif // DACCESS_COMPILE

#endif // !FEATURE_PAL

BOOL CCompRC::s_bIsMscoree = FALSE;

//*****************************************************************************
// Do the mapping from an langId to an hinstance node
//*****************************************************************************
HRESOURCEDLL CCompRC::LookupNode(LocaleID langId, BOOL &fMissing)
{
    LIMITED_METHOD_CONTRACT;

    if (m_pHash == NULL) return NULL;

// Linear search
    int i;
    for(i = 0; i < m_nHashSize; i ++) {
        if (m_pHash[i].IsSet() && m_pHash[i].HasID(langId)) {
            return m_pHash[i].GetLibraryHandle();
        }
        if (m_pHash[i].IsMissing() && m_pHash[i].HasID(langId))
        {
            fMissing = TRUE;
            return NULL;
        }
    }

    return NULL;
}

//*****************************************************************************
// Add a new node to the map and return it.
//*****************************************************************************
const int MAP_STARTSIZE = 7;
const int MAP_GROWSIZE = 5;

HRESULT CCompRC::AddMapNode(LocaleID langId, HRESOURCEDLL hInst, BOOL fMissing)
{
    CONTRACTL
    {
        GC_NOTRIGGER;
        NOTHROW;
        INJECT_FAULT(return E_OUTOFMEMORY;);
    }
    CONTRACTL_END;

    
    if (m_pHash == NULL) {
        m_pHash = new (nothrow)CCulturedHInstance[MAP_STARTSIZE];        
        if (m_pHash==NULL)
            return E_OUTOFMEMORY;
        m_nHashSize = MAP_STARTSIZE;
    }

// For now, place in first open slot
    int i;
    for(i = 0; i < m_nHashSize; i ++) {
        if (!m_pHash[i].IsSet() && !m_pHash[i].IsMissing()) {
            if (fMissing)
            {
                m_pHash[i].SetMissing(langId);
            }
            else
            {
                m_pHash[i].Set(langId,hInst);
            }
            
            return S_OK;
        }
    }

// Out of space, regrow
    CCulturedHInstance * pNewHash = new (nothrow)CCulturedHInstance[m_nHashSize + MAP_GROWSIZE];
    if (pNewHash)
    {
        memcpy(pNewHash, m_pHash, sizeof(CCulturedHInstance) * m_nHashSize);
        delete [] m_pHash;
        m_pHash = pNewHash;
        if (fMissing)
        {
            m_pHash[m_nHashSize].SetMissing(langId);
        }
        else
        {
            m_pHash[m_nHashSize].Set(langId,hInst);
        }
        m_nHashSize += MAP_GROWSIZE;
    }
    else
        return E_OUTOFMEMORY;
    return S_OK;
}

//*****************************************************************************
// Initialize
//*****************************************************************************
LPCWSTR CCompRC::m_pDefaultResource = W("mscorrc.debug.dll");
LPCWSTR CCompRC::m_pFallbackResource= W("mscorrc.dll");

#ifdef FEATURE_PAL
LPCSTR CCompRC::m_pDefaultResourceDomain = "mscorrc.debug";
LPCSTR CCompRC::m_pFallbackResourceDomain = "mscorrc";
#endif // FEATURE_PAL

HRESULT CCompRC::Init(LPCWSTR pResourceFile, BOOL bUseFallback)
{
    CONTRACTL
    {
        GC_NOTRIGGER;
        NOTHROW;
        INJECT_FAULT(return E_OUTOFMEMORY;);
    }
    CONTRACTL_END;

	// This function is called during Watson process.  We need to make sure
	// that this function is restartable.
	// 
    // Make sure to NEVER null out the function callbacks in the Init
    // function. They get set for the "Default CCompRC" during EEStartup
    // and we want to make sure we don't wipe them out.

    m_bUseFallback = bUseFallback;
    
    if (m_pResourceFile == NULL)
    {
        if(pResourceFile)
        {
            NewArrayHolder<WCHAR> pwszResourceFile(NULL);
    
            DWORD lgth = (DWORD) wcslen(pResourceFile) + 1;
            pwszResourceFile = new(nothrow) WCHAR[lgth];
            if (pwszResourceFile)
            {
                wcscpy_s(pwszResourceFile, lgth, pResourceFile);
                LPCWSTR pFile = pwszResourceFile.Extract();
                if (InterlockedCompareExchangeT(&m_pResourceFile, pFile, NULL) != NULL)
                {
                    delete [] pFile;
                }
            }
        }
    else
        InterlockedCompareExchangeT(&m_pResourceFile, m_pDefaultResource, NULL);
    }
    
    if (m_pResourceFile == NULL)
    {
        return E_OUTOFMEMORY;
    }

#ifdef FEATURE_PAL

    if (m_pResourceFile == m_pDefaultResource)
    {
        m_pResourceDomain = m_pDefaultResourceDomain;
    }
    else if (m_pResourceFile == m_pFallbackResource)
    {
        m_pResourceDomain = m_pFallbackResourceDomain;
    }
    else
    {
        _ASSERTE(!"Unsupported resource file");
    }

#ifndef CROSSGEN_COMPILE
    // PAL_BindResources requires that libcoreclr.so has been loaded,
    // and thus can'be be called by crossgen.
    if (!PAL_BindResources(m_pResourceDomain))
    {
        // The function can fail only due to OOM
        return E_OUTOFMEMORY;
    }
#endif

#endif // FEATURE_PAL

    if (m_csMap == NULL)
    {
    // NOTE: there are times when the debugger's helper thread is asked to do a favor for another thread in the
    // process. Typically, this favor involves putting up a dialog for the user. Putting up a dialog usually ends
    // up involving the CCompRC code since (of course) the strings in the dialog are in a resource file. Thus, the
    // debugger's helper thread will attempt to acquire this CRST. This is okay, since the helper thread only does
    // these favors for other threads when there is no debugger attached. Thus, there are no deadlock hazards with
    // this lock, and its safe for the helper thread to take, so this CRST is marked with CRST_DEBUGGER_THREAD.
        CRITSEC_COOKIE csMap = ClrCreateCriticalSection(CrstCCompRC,
                                       (CrstFlags)(CRST_UNSAFE_ANYMODE | CRST_DEBUGGER_THREAD | CRST_TAKEN_DURING_SHUTDOWN));

        if (csMap)
        {
            if (InterlockedCompareExchangeT(&m_csMap, csMap, NULL) != NULL)
            {
                ClrDeleteCriticalSection(csMap);
            }
        }
    }

    if (m_csMap == NULL)
        return E_OUTOFMEMORY;

    return S_OK;
}

void CCompRC::SetResourceCultureCallbacks(
        FPGETTHREADUICULTURENAMES fpGetThreadUICultureNames,
        FPGETTHREADUICULTUREID fpGetThreadUICultureId)
{
    LIMITED_METHOD_CONTRACT;

    m_fpGetThreadUICultureNames = fpGetThreadUICultureNames;
    m_fpGetThreadUICultureId = fpGetThreadUICultureId;
}

void CCompRC::GetResourceCultureCallbacks(
        FPGETTHREADUICULTURENAMES* fpGetThreadUICultureNames,
        FPGETTHREADUICULTUREID* fpGetThreadUICultureId)
{
    LIMITED_METHOD_CONTRACT;

    if(fpGetThreadUICultureNames)
        *fpGetThreadUICultureNames=m_fpGetThreadUICultureNames;

    if(fpGetThreadUICultureId)
        *fpGetThreadUICultureId=m_fpGetThreadUICultureId;
}

void CCompRC::Destroy()
{
    CONTRACTL
    {
        GC_NOTRIGGER;
        NOTHROW;
#ifdef      MODE_PREEMPTIVE
        MODE_PREEMPTIVE;
#endif
    }
    CONTRACTL_END;

    // Free all resource libraries

    //*****************************************************************************
    // Free the loaded library if we ever loaded it and only if we are not on
    // Win 95 which has a known bug with DLL unloading (it randomly unloads a
    // dll on shut down, not necessarily the one you asked for).  This is done
    // only in debug mode to make coverage runs accurate.
    //*****************************************************************************

#if defined(_DEBUG)
    if (m_Primary.GetLibraryHandle()) {
        ::FreeLibrary(m_Primary.GetLibraryHandle());
    }

    if (m_pHash != NULL) {
        int i;
        for(i = 0; i < m_nHashSize; i ++) {
            if (m_pHash[i].GetLibraryHandle() != NULL) {
                ::FreeLibrary(m_pHash[i].GetLibraryHandle());
                break;
            }
        }
    }
#endif

    // destroy map structure
    if(m_pResourceFile != m_pDefaultResource)
        delete [] m_pResourceFile;
    m_pResourceFile = NULL;

    if(m_csMap) {
        ClrDeleteCriticalSection(m_csMap);
        ZeroMemory(&(m_csMap), sizeof(CRITSEC_COOKIE));
    }

    if(m_pHash != NULL) {
        delete [] m_pHash;
        m_pHash = NULL;
    }
}


//*****************************************************************************
// Initialization is done lazily, for backwards compatibility "mscorrc.dll"
// is consider the default location for all strings that use CCompRC. 
// An instance value for CCompRC can be created to load resources from a different
// resource dll.
//*****************************************************************************
LONG    CCompRC::m_dwDefaultInitialized = 0;
CCompRC CCompRC::m_DefaultResourceDll;

CCompRC* CCompRC::GetDefaultResourceDll()
{
    CONTRACTL
    {
        GC_NOTRIGGER;
        NOTHROW;
#ifdef      MODE_PREEMPTIVE
        MODE_PREEMPTIVE;
#endif
    }
    CONTRACTL_END;
    
    if (m_dwDefaultInitialized)
        return &m_DefaultResourceDll;

    if(FAILED(m_DefaultResourceDll.Init(NULL, TRUE)))
    {
        return NULL;
    }
    m_dwDefaultInitialized = 1;
    
    return &m_DefaultResourceDll;
}

LONG    CCompRC::m_dwFallbackInitialized = 0;
CCompRC CCompRC::m_FallbackResourceDll;

CCompRC* CCompRC::GetFallbackResourceDll()
{
    CONTRACTL
    {
        GC_NOTRIGGER;
        NOTHROW;
#ifdef      MODE_PREEMPTIVE
        MODE_PREEMPTIVE;
#endif
    }
    CONTRACTL_END;
    
    if (m_dwFallbackInitialized)
        return &m_FallbackResourceDll;

    if(FAILED(m_FallbackResourceDll.Init(m_pFallbackResource, FALSE)))
    {
        return NULL;
    }
    m_dwFallbackInitialized = 1;
    
    return &m_FallbackResourceDll;
}



//*****************************************************************************
//*****************************************************************************

HRESULT CCompRC::GetLibrary(LocaleID langId, HRESOURCEDLL* phInst)
{
    CONTRACTL
    {
        GC_NOTRIGGER;
        NOTHROW;
#ifdef      MODE_PREEMPTIVE
        MODE_PREEMPTIVE;
#endif
        PRECONDITION(phInst != NULL);
    }
    CONTRACTL_END;

    HRESULT     hr = E_FAIL;
    HRESOURCEDLL    hInst = 0;
#ifndef DACCESS_COMPILE    
    HRESOURCEDLL    hLibInst = 0; //Holds early library instance
    BOOL        fLibAlreadyOpen = FALSE; //Determine if we can close the opened library.
#endif

    // Try to match the primary entry, or else use the primary if we don't care.
    if (m_Primary.IsSet())
    {
        if (langId == UICULTUREID_DONTCARE || m_Primary.HasID(langId))
        {
            hInst = m_Primary.GetLibraryHandle();
            hr = S_OK;
        }
    }
    else if(m_Primary.IsMissing())
    {
        // If primary is missing then the hash will not have anything either
        hr = HRESULT_FROM_WIN32(ERROR_FILE_NOT_FOUND);
    }
#ifndef DACCESS_COMPILE    
    // If this is the first visit, we must set the primary entry
    else
    {
        // Don't immediately return if LoadLibrary fails so we can indicate the file was missing
        hr = LoadLibrary(&hLibInst);
        // If it's a transient failure, don't cache the failure
        if (FAILED(hr) && Exception::IsTransient(hr))
        {
            return hr;
        }
        
        CRITSEC_Holder csh (m_csMap);
        // As we expected
        if (!m_Primary.IsSet() && !m_Primary.IsMissing())
        {
            hInst  = hLibInst;
            if (SUCCEEDED(hr))
            {
                m_Primary.Set(langId,hLibInst);
            }
            else
            {
                m_Primary.SetMissing(langId);
            }
        }
        
        // Someone got into this critical section before us and set the primary already
        else if (m_Primary.HasID(langId))
        {
            hInst = m_Primary.GetLibraryHandle();
            fLibAlreadyOpen = TRUE;
        }
        
        // If neither case is true, someone got into this critical section before us and
        //  set the primary to other than the language we want...
        else
        {
            fLibAlreadyOpen = TRUE;
        }
        
        IfFailRet(hr);
        
        if (fLibAlreadyOpen)
        {
            FreeLibrary(hLibInst);
            fLibAlreadyOpen = FALSE;
        }
    }
#endif

    // If we enter here, we know that the primary is set to something other than the
    // language we want - multiple languages use the hash table
    if (hInst == NULL && !m_Primary.IsMissing())
    {
        // See if the resource exists in the hash table
        {
            CRITSEC_Holder csh(m_csMap);
            BOOL fMissing = FALSE;
            hInst = LookupNode(langId, fMissing);
            if (fMissing == TRUE)
            {
                hr = HRESULT_FROM_WIN32(ERROR_FILE_NOT_FOUND);
                goto Exit;
            }
        }

#ifndef DACCESS_COMPILE    
        // If we didn't find it, we have to load the library and insert it into the hash
        if (hInst == NULL) 
        {
            hr = LoadLibrary(&hLibInst);
            // If it's a transient failure, don't cache the failure
            if (FAILED(hr) && Exception::IsTransient(hr))
            {
                return hr;
            }
            {
                CRITSEC_Holder csh (m_csMap);
                
                // Double check - someone may have entered this section before us
                BOOL fMissing = FALSE;
                hInst = LookupNode(langId, fMissing);
                if (hInst == NULL && !fMissing)
                {
                    if (SUCCEEDED(hr))
                    {
                        hInst = hLibInst;
                        hr = AddMapNode(langId, hInst);
                    } else
                    {
                        HRESULT hrLoadLibrary = hr;
                        hr = AddMapNode(langId, hInst, TRUE /* fMissing */);
                        if (SUCCEEDED(hr))
                        {
                            hr = hrLoadLibrary;
                        }
                    }
                }
                else
                {
                    fLibAlreadyOpen = TRUE;
                }
            }

            if (fLibAlreadyOpen || FAILED(hr))
            {
                FreeLibrary(hLibInst);
            }
        }

        // We found the node, so set hr to be a success.
        else 
        {
            hr = S_OK;
        }
#endif // DACCESS_COMPILE    
    }
Exit:
    *phInst = hInst;
    return hr;
}

//*****************************************************************************
// Load the string 
// We load the localized libraries and cache the handle for future use.
// Mutliple threads may call this, so the cache structure is thread safe.
//*****************************************************************************
HRESULT CCompRC::LoadString(ResourceCategory eCategory, UINT iResourceID, __out_ecount(iMax) LPWSTR szBuffer, int iMax,  int *pcwchUsed)
{
    WRAPPER_NO_CONTRACT;
    LocaleIDValue langIdValue;
    LocaleID langId;
    // Must resolve current thread's langId to a dll.   
    if(m_fpGetThreadUICultureId) {
        int ret = (*m_fpGetThreadUICultureId)(&langIdValue);
        
        // Callback can't return 0, since that indicates empty.
        // To indicate empty, callback should return UICULTUREID_DONTCARE
        _ASSERTE(ret != 0);

        if (ret == 0)
            return E_UNEXPECTED;
        langId=langIdValue;
        
    }
    else {
        langId = UICULTUREID_DONTCARE;
    }
    

    return LoadString(eCategory, langId, iResourceID, szBuffer, iMax, pcwchUsed);
}

HRESULT CCompRC::LoadString(ResourceCategory eCategory, LocaleID langId, UINT iResourceID, __out_ecount(iMax) LPWSTR szBuffer, int iMax, int *pcwchUsed)
{
    CONTRACTL
    {
        GC_NOTRIGGER;
        NOTHROW;
#ifdef      MODE_PREEMPTIVE
        MODE_PREEMPTIVE;
#endif
    }
    CONTRACTL_END;

#ifndef FEATURE_PAL
    HRESULT         hr;
    HRESOURCEDLL    hInst = 0; //instance of cultured resource dll
    int length;

    hr = GetLibrary(langId, &hInst);

    if (SUCCEEDED(hr))
    {
        // Now that we have the proper dll handle, load the string
        _ASSERTE(hInst != NULL);

        length = ::WszLoadString(hInst, iResourceID, szBuffer, iMax);
        if(length > 0) 
        {
            if(pcwchUsed) 
            {
                *pcwchUsed = length;
            }
            return (S_OK);
        }
        if(GetLastError()==ERROR_SUCCESS)
            hr=HRESULT_FROM_WIN32(ERROR_NOT_FOUND);
        else
            hr=HRESULT_FROM_GetLastError();
    }


    // Failed to load string
    if ( hr != E_OUTOFMEMORY && ShouldUseFallback())
    {
        CCompRC* pFallback=CCompRC::GetFallbackResourceDll();
        if (pFallback)
        {
            //should not fall back to itself
            _ASSERTE(pFallback != this);

            // check existence in the fallback Dll 

            hr = pFallback->LoadString(Optional, langId, iResourceID,szBuffer, iMax, pcwchUsed);

            if(SUCCEEDED(hr))
                return hr;
        }
        switch (eCategory)
        {
            case Optional:
                hr = E_FAIL;
                break;
            case  DesktopCLR:
                hr = E_FAIL;
                break;
            case Debugging:
            case Error:
                // get stub message
                {
                    
                   if (pFallback)
                   {
                        
                        StackSString ssErrorFormat;
                        if (eCategory == Error)
                        {
                            hr=ssErrorFormat.LoadResourceAndReturnHR(pFallback,  CCompRC::Required, IDS_EE_LINK_FOR_ERROR_MESSAGES);
                        }
                        else
                        {
                            _ASSERTE(eCategory == Debugging);
                            hr=ssErrorFormat.LoadResourceAndReturnHR(pFallback,  CCompRC::Required, IDS_EE_LINK_FOR_DEBUGGING_MESSAGES);
                        }
                        
                        if (SUCCEEDED(hr))
                        {
                            StackSString sFormattedMessage;
                            int iErrorCode = HR_FOR_URT_MSG(iResourceID);

                            hr = S_OK;
                            
                            DWORD_PTR args[] = {(DWORD_PTR)VER_FILEVERSION_STR_L, iResourceID, iErrorCode};
                            
                            length = WszFormatMessage(FORMAT_MESSAGE_FROM_STRING | FORMAT_MESSAGE_ARGUMENT_ARRAY ,
                                                        (LPCWSTR)ssErrorFormat, 0, 0,
                                                        szBuffer,iMax,(va_list*)args);

                            if (length == 0 && GetLastError() == ERROR_INSUFFICIENT_BUFFER)
                            {
                                // The buffer wasn't big enough for the message. Tell the caller this.
                                // 
                                // Clear the buffer, just in case.
                                if (szBuffer && iMax)
                                    *szBuffer = W('\0');

                                length = iMax;
                                hr=HRESULT_FROM_GetLastError();
                            }

                            if(length > 0) 
                            {
                                if(pcwchUsed) 
                                {
                                    *pcwchUsed = length;
                                }
                                return hr;
                            }
                            
                            // Format mesage failed
                            hr=HRESULT_FROM_GetLastError();
                                    
                        }
                    }
                    else // if (pFallback)
                    {
                        _ASSERTE(FAILED(hr));
                    }
                }
                // if we got here then we couldn't get the fallback message
                // the fallback message is required so just falling through into "Required"
                
            case Required:

                if ( hr != E_OUTOFMEMORY)
                {
                    // Shouldn't be any reason for this condition but the case where
                    // the resource dll is missing, code used the wrong ID or developer didn't 
                    // update the resource DLL.
                    _ASSERTE(!"Missing mscorrc.dll or mscorrc.debug.dll?");
                    hr = HRESULT_FROM_GetLastError();
                }
                break;
            default:
                {
                    _ASSERTE(!"Invalid eCategory");
                }
        }
    }

    // Return an empty string to save the people with a bad error handling
    if (szBuffer && iMax)
        *szBuffer = W('\0');

    return hr;
#else // !FEATURE_PAL
    return LoadNativeStringResource(NATIVE_STRING_RESOURCE_TABLE(NATIVE_STRING_RESOURCE_NAME), iResourceID,
      szBuffer, iMax, pcwchUsed);
#endif // !FEATURE_PAL
}

#ifndef DACCESS_COMPILE
HRESULT CCompRC::LoadMUILibrary(HRESOURCEDLL * pHInst)
{
    WRAPPER_NO_CONTRACT;
    _ASSERTE(pHInst != NULL);
    LocaleID langId;
    LocaleIDValue langIdValue;
    // Must resolve current thread's langId to a dll.   
    if(m_fpGetThreadUICultureId) {
        int ret = (*m_fpGetThreadUICultureId)(&langIdValue);
        
        // Callback can't return 0, since that indicates empty.
        // To indicate empty, callback should return UICULTUREID_DONTCARE
        _ASSERTE(ret != 0);
        langId=langIdValue;
    }
    else 
        langId = UICULTUREID_DONTCARE;

    HRESULT hr = GetLibrary(langId, pHInst);
    return hr;
}

HRESULT CCompRC::LoadResourceFile(HRESOURCEDLL * pHInst, LPCWSTR lpFileName)
{
#ifndef FEATURE_PAL
    DWORD dwLoadLibraryFlags;
    if(m_pResourceFile == m_pDefaultResource)
        dwLoadLibraryFlags = LOAD_LIBRARY_AS_DATAFILE;
    else
        dwLoadLibraryFlags = 0;

    if ((*pHInst = WszLoadLibraryEx(lpFileName, NULL, dwLoadLibraryFlags)) == NULL) {
        return HRESULT_FROM_GetLastError();
    }
#else // !FEATURE_PAL    
    PORTABILITY_ASSERT("UNIXTODO: Implement resource loading - use peimagedecoder?");
#endif // !FEATURE_PAL
    return S_OK;
}

//*****************************************************************************
// Load the library for this thread's current language
// Called once per language. 
// Search order is: 
//  1. Dll in localized path (<dir passed>\<lang name (en-US format)>\mscorrc.dll)
//  2. Dll in localized (parent) path (<dir passed>\<lang name> (en format)\mscorrc.dll)
//  3. Dll in root path (<dir passed>\mscorrc.dll)
//  4. Dll in current path   (<current dir>\mscorrc.dll)
//*****************************************************************************
HRESULT CCompRC::LoadLibraryHelper(HRESOURCEDLL *pHInst,
                                   SString& rcPath)
{
    CONTRACTL
    {
        GC_NOTRIGGER;
        NOTHROW;
#ifdef      MODE_PREEMPTIVE
        MODE_PREEMPTIVE;
#endif
    }
    CONTRACTL_END;
    
    HRESULT     hr = E_FAIL;
    

    _ASSERTE(m_pResourceFile != NULL);

    // must initialize before calling SString::Empty()
    SString::Startup();

    // Try and get both the culture fallback sequence         

    StringArrayList cultureNames; 

    if (m_fpGetThreadUICultureNames) 
    {
        hr = (*m_fpGetThreadUICultureNames)(&cultureNames);
    }        
    else
    {
        EX_TRY
        {
            cultureNames.Append(SString::Empty());
        }
        EX_CATCH_HRESULT(hr);
    }

    if (hr == E_OUTOFMEMORY)
        return hr;
    EX_TRY
    {
        for (DWORD i=0; i< cultureNames.GetCount();i++)
        {
            SString& sLang = cultureNames[i];
       
            PathString rcPathName(rcPath);

            if (!rcPathName.EndsWith(W("\\")))
            {
                rcPathName.Append(W("\\"));
            }

            if (!sLang.IsEmpty())
            {
                rcPathName.Append(sLang);
                rcPathName.Append(W("\\"));
                rcPathName.Append(m_pResourceFile);
            }
            else
            {
                rcPathName.Append(m_pResourceFile);
            }

            // Feedback for debugging to eliminate unecessary loads.
            DEBUG_STMT(DbgWriteEx(W("Loading %s to load strings.\n"), rcPath.GetUnicode()));

            // Load the resource library as a data file, so that the OS doesn't have
            // to allocate it as code.  This only works so long as the file contains
            // only strings.
            hr = LoadResourceFile(pHInst, rcPathName);
            if (SUCCEEDED(hr))
                break;
            
        }   
    }
    EX_CATCH_HRESULT(hr);
    
    // Last ditch search effort in current directory
    if (FAILED(hr)) {
        hr = LoadResourceFile(pHInst, m_pResourceFile);
    }

    return hr;
}

// Two-stage approach:
// First try module directory, then try CORSystemDirectory for default resource
HRESULT CCompRC::LoadLibraryThrows(HRESOURCEDLL * pHInst)
{
    CONTRACTL
    {
        GC_NOTRIGGER;
        THROWS;
#ifdef      MODE_PREEMPTIVE
        MODE_PREEMPTIVE;
#endif
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;

    _ASSERTE(pHInst != NULL);

#ifdef CROSSGEN_COMPILE
    // The resources are embeded into the .exe itself for crossgen
    *pHInst = GetModuleInst();
#else
    PathString       rcPath;      // Path to resource DLL.

    // Try first in the same directory as this dll.

    VALIDATECORECLRCALLBACKS();

    
    hr = g_CoreClrCallbacks.m_pfnGetCORSystemDirectory(rcPath);
    if (FAILED(hr))
        return hr;

    hr = LoadLibraryHelper(pHInst, rcPath);


#endif // CROSSGEN_COMPILE

    return hr;
}
HRESULT CCompRC::LoadLibrary(HRESOURCEDLL * pHInst)
{
    CONTRACTL
    {
        GC_NOTRIGGER;
    NOTHROW;
#ifdef      MODE_PREEMPTIVE
    MODE_PREEMPTIVE;
#endif
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;
    EX_TRY
    {
        hr = LoadLibraryThrows(pHInst);
    }
    EX_CATCH_HRESULT(hr);

    return hr;
}
#endif // DACCESS_COMPILE



#ifdef USE_FORMATMESSAGE_WRAPPER
DWORD
PALAPI
CCompRC::FormatMessage(
           IN DWORD dwFlags,
           IN LPCVOID lpSource,
           IN DWORD dwMessageId,
           IN DWORD dwLanguageId,
           OUT LPWSTR lpBuffer,
           IN DWORD nSize,
           IN va_list *Arguments)
{
    STATIC_CONTRACT_NOTHROW;
    StackSString str;
    if (dwFlags & FORMAT_MESSAGE_FROM_SYSTEM)
    {
        dwFlags&=~FORMAT_MESSAGE_FROM_SYSTEM;
        dwFlags|=FORMAT_MESSAGE_FROM_STRING;
        str.LoadResourceAndReturnHR(NULL,CCompRC::Error,dwMessageId);
        lpSource=str.GetUnicode();
    }
    return WszFormatMessage(dwFlags,
                            lpSource,
                            dwMessageId,
                            dwLanguageId,
                            lpBuffer,
                            nSize,
                            Arguments);
}
#endif // USE_FORMATMESSAGE_WRAPPER

