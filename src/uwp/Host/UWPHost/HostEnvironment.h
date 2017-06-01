//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//


//
// CoreCLR Host that is used in activating CoreCLR in 
// UWP apps' F5/development time scenarios
//

#ifndef __HOSTENVIRONMENT_H__
#define __HOSTENVIRONMENT_H__

#include <stdio.h>
#include "mscoree.h"
#include "appmodel.h"
#ifdef _DEBUG
#include "assert.h"
#define _ASSERTE assert
#endif

// Some useful macros
#ifndef IfFailGoto
#define IfFailGoto(EXPR, LABEL) \
do { hr = (EXPR); if(FAILED(hr)) { goto LABEL; } } while (0)
#endif

#ifndef IfFailRet
#define IfFailRet(EXPR) \
do { hr = (EXPR); if(FAILED(hr)) { return (hr); } } while (0)
#endif

#ifndef IfFailWin32Ret
#define IfFailWin32Ret(EXPR) \
do { hr = (EXPR); if(hr != ERROR_SUCCESS) { hr = HRESULT_FROM_WIN32(hr); return hr;} } while (0)
#endif

#ifndef IfFailWin32Goto
#define IfFailWin32Goto(EXPR, LABEL) \
do { hr = (EXPR); if(hr != ERROR_SUCCESS) { hr = HRESULT_FROM_WIN32(hr);  goto LABEL; } } while (0)
#endif

#ifndef IfFalseWin32Goto
#define IfFalseWin32Goto(EXPR1, EXPR2, LABEL) \
do { if (TRUE != (EXPR1)) { IfFailWin32Goto(EXPR2, LABEL); } } while (0)
#endif

#ifndef IfFalseGoto
#define IfFalseGoto(EXPR1, EXPR2, LABEL) \
do { if (TRUE != (EXPR1)) { IfFailGoto(EXPR2, LABEL); } } while (0)
#endif

#ifndef IfFailGo
#define IfFailGo(EXPR) IfFailGoto(EXPR, ErrExit)
#endif

#ifndef IfFailWin32Go
#define IfFailWin32Go(EXPR) IfFailWin32Goto(EXPR, ErrExit)
#endif

#ifndef IfFalseWin32Go
#define IfFalseWin32Go(EXPR1, EXPR2) IfFalseWin32Goto(EXPR1, EXPR2, ErrExit)
#endif

#ifndef IfFalseGo
#define IfFalseGo(EXPR1, EXPR2) IfFalseGoto(EXPR1, EXPR2, ErrExit)
#endif
// ----


// The name of the CoreCLR native runtime DLL.
static wchar_t *coreCLRDll = L"CoreCLR.dll";
#ifdef _DEBUG
static wchar_t *coreRuntimePackageFullNamePrefix = L"Microsoft.NET.CoreRuntime";
#endif

// Dynamically expanding string buffer to hold TPA list
class StringBuffer {
    wchar_t* m_buffer;
    size_t m_capacity;
    size_t m_length;

    StringBuffer(const StringBuffer&);
    StringBuffer& operator =(const StringBuffer&);

public:
    StringBuffer() : m_capacity(0), m_buffer(0), m_length(0) {
    }

    ~StringBuffer() {
        delete[] m_buffer;
    }

    const wchar_t* CStr() const {
        return m_buffer;
    }

    void Append(const wchar_t* str, size_t strLen) {
        if (!m_buffer) {
            m_buffer = new wchar_t[4096];
            m_capacity = 4096;
        }
        if (m_length + strLen + 1 > m_capacity) {
            size_t newCapacity = m_capacity * 2;
            wchar_t* newBuffer = new wchar_t[newCapacity];
            wcsncpy_s(newBuffer, newCapacity, m_buffer, m_length);
            delete[] m_buffer;
            m_buffer = newBuffer;
            m_capacity = newCapacity;
        }
        wcsncpy_s(m_buffer + m_length, m_capacity - m_length, str, strLen);
        m_length += strLen;
    }
};


class HostEnvironment
{

private:


    StringBuffer m_tpaList;

    StringBuffer m_appPaths;
    wchar_t** m_dependentPackagesRootPaths;
    UINT32 m_dependentPackagesCount;

    ICLRRuntimeHost2* m_CLRRuntimeHost;

    HMODULE m_coreCLRModule;

    bool m_fIsAppXPackage;
    
    // AppX package path
    wchar_t * m_packageRoot;
    
    wchar_t *m_currentPackageFullName;
    wchar_t  m_coreCLRInstallDirectory[MAX_PATH];
                
    // Attempts to load CoreCLR.dll 
    // On success pins the dll and returns the HMODULE.
    // On failure returns nullptr.
    HRESULT TryLoadCoreCLR();

    void InitializeTPAList(_In_reads_(tpaEntriesCount) wchar_t** tpaEntries, int tpaEntriesCount);
public:    

    HRESULT Initialize();

    bool CanDebugUWPApps()
    {
        // Combine with development mode flag
        return m_fIsAppXPackage /* && m_fIsInDevelopmentMode */;
    }
    
    bool IsCoreCLRLoaded()
    {
        return (m_coreCLRModule != NULL);
    }
    
    PWSTR GetPackageRoot()
    {
        return m_packageRoot;
    }
    
    wchar_t* GetCoreCLRInstallPath()
    {
        return m_coreCLRInstallDirectory;
    }
    

    HostEnvironment();

    ~HostEnvironment();

    // Returns the semicolon-separated list of paths to runtime dlls that are considered trusted.
    // On first call, scans the coreclr directory for dlls and adds them all to the list.
    const wchar_t * GetTpaList() 
    {
        if (!m_tpaList.CStr()) 
        {
            wchar_t *tpaEntries[] = {L"System.Private.CoreLib.ni.dll", L"mscorlib.ni.dll"};
            InitializeTPAList(tpaEntries, _countof(tpaEntries));
        }

        return m_tpaList.CStr();
    }

    const wchar_t * GetAppPaths() 
    {
        if (!m_appPaths.CStr())
        {
            m_appPaths.Append(m_packageRoot, wcslen(m_packageRoot));
            m_appPaths.Append(L"\\entrypoint;", wcslen(L"\\entrypoint;"));
            m_appPaths.Append(m_packageRoot, wcslen(m_packageRoot));
            m_appPaths.Append(L";",1);
            for (UINT32 i=0;i<m_dependentPackagesCount;i++)
            {
                m_appPaths.Append(m_dependentPackagesRootPaths[i], wcslen(m_dependentPackagesRootPaths[i]));
                m_appPaths.Append(L";",1);
            }
        }
        return m_appPaths.CStr();
    }

    ICLRRuntimeHost2* GetCLRRuntimeHost(HRESULT &hResult);
};
    



#endif // __HOSTENVIRONMENT_H__
