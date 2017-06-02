//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//


// 
// A simple in-app shim for UWP app activation
//

#include "windows.h"

// Function forwarder to DllGetActivationFactory in UWPHost
// App model requires the inproc server for WinRt components to be in the package which
// installs the component itself. UWPHost is not app-local hence can't be declared as 
// inproc server. Instead UWPShim will be the inproc server which merely forwards 
// the request to UWPHost
#pragma comment(linker, "/export:DllGetActivationFactory=uwphost.DllGetActivationFactory")

extern HRESULT ExecuteAssembly(_In_z_ wchar_t *entryPointAssemblyFileName, int argc, LPCWSTR* argv, DWORD *exitCode);

#ifndef IfFailRet
#define IfFailRet(EXPR) \
do { errno_t x = (EXPR); if(FAILED(x)) { return (x); } } while (0)
#endif

int __cdecl wmain(const int argc, const wchar_t* argv[])
{

    DWORD exitCode = -1;
    if (argc < 2)
    {
        // Invalid number of arguments
        return -1;
    }
    
    // This module is merely a shim to figure out what the actual EntryPoint assembly is and call the Host with 
    // that information. The EntryPoint would be found based on the following assumptions
    //
    // 1) Current module lives under the "CoreRuntime" subfolder of the AppX package installation folder.
    // 2) It has the same name as the EntryPoint assembly that will reside in the parent folder (i.e. the AppX package installation folder).
    
    const wchar_t* pActivationModulePath = argv[0];

    const wchar_t *pLastSlash = wcsrchr(pActivationModulePath, L'\\');
    if (pLastSlash == NULL)
    {
        return -1;
    }
    
    wchar_t entryPointAssemblyFileName[MAX_PATH];
    IfFailRet(wcsncpy_s(entryPointAssemblyFileName, MAX_PATH, pActivationModulePath, pLastSlash-pActivationModulePath));
    IfFailRet(wcscat_s(entryPointAssemblyFileName, MAX_PATH, L"\\entrypoint"));
    IfFailRet(wcscat_s(entryPointAssemblyFileName, MAX_PATH, pLastSlash));
    
    auto success = ExecuteAssembly(entryPointAssemblyFileName, argc-1, &(argv[1]), &exitCode);

    return exitCode;
}

