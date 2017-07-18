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


// Alternative implementation to CommandLineToArgvW, which is not available to Store profile
// apps (including this UWP shim).
LPWSTR *SegmentCommandLine(LPCWSTR lpCmdLine, int *pNumArgs)
{
    *pNumArgs = 0;

    int nch = (int)wcslen(lpCmdLine);

    // Calculate the worst-case storage requirement. (One pointer for
    // each argument, plus storage for the arguments themselves.)
    int cbAlloc = (nch+1)*sizeof(LPWSTR) + sizeof(wchar_t)*(nch + 1);
    
    LPWSTR pAlloc = (LPWSTR)malloc(cbAlloc);
    if (!pAlloc)
        return NULL;

    LPWSTR *argv = (LPWSTR*) pAlloc;  // We store the argv pointers in the first halt
    LPWSTR  pdst = (LPWSTR)( ((BYTE*)pAlloc) + sizeof(LPWSTR)*(nch+1) ); // A running pointer to second half to store arguments
    LPCWSTR psrc = lpCmdLine;
    wchar_t   c;
    BOOL    inquote;
    BOOL    copychar;
    int     numslash;

    // First, parse the program name (argv[0]). Argv[0] is parsed under
    // special rules. Anything up to the first whitespace outside a quoted
    // subtring is accepted. Backslashes are treated as normal characters.
    argv[ (*pNumArgs)++ ] = pdst;
    inquote = FALSE;
    do {
        if (*psrc == L'"' )
        {
            inquote = !inquote;
            c = *psrc++;
            continue;
        }
        *pdst++ = *psrc;

        c = *psrc++;

    } while ( (c != L'\0' && (inquote || (c != L' ' && c != L'\t'))) );

    if ( c == L'\0' ) {
        psrc--;
    } else {
        *(pdst-1) = L'\0';
    }

    inquote = FALSE;



    /* loop on each argument */
    for(;;)
    {
        if ( *psrc )
        {
            while (*psrc == L' ' || *psrc == L'\t')
            {
                ++psrc;
            }
        }

        if (*psrc == L'\0')
            break;              /* end of args */

        /* scan an argument */
        argv[ (*pNumArgs)++ ] = pdst;

        /* loop through scanning one argument */
        for (;;)
        {
            copychar = 1;
            /* Rules: 2N backslashes + " ==> N backslashes and begin/end quote
               2N+1 backslashes + " ==> N backslashes + literal "
               N backslashes ==> N backslashes */
            numslash = 0;
            while (*psrc == L'\\')
            {
                /* count number of backslashes for use below */
                ++psrc;
                ++numslash;
            }
            if (*psrc == L'"')
            {
                /* if 2N backslashes before, start/end quote, otherwise
                   copy literally */
                if (numslash % 2 == 0)
                {
                    if (inquote && psrc[1] == L'"')
                    {
                        psrc++;    /* Double quote inside quoted string */
                    }
                    else
                    {
                        /* skip first quote char and copy second */
                        copychar = 0;       /* don't copy quote */
                        inquote = !inquote;
                    }
                }
                numslash /= 2;          /* divide numslash by two */
            }
    
            /* copy slashes */
            while (numslash--)
            {
                *pdst++ = L'\\';
            }
    
            /* if at end of arg, break loop */
            if (*psrc == L'\0' || (!inquote && (*psrc == L' ' || *psrc == L'\t')))
                break;
    
            /* copy character into argument */
            if (copychar)
            {
                *pdst++ = *psrc;
            }
            ++psrc;
        }

        /* null-terminate the argument */

        *pdst++ = L'\0';          /* terminate string */
    }

    /* We put one last argument in -- a null ptr */
    argv[ (*pNumArgs) ] = NULL;

    return argv;
}

int __cdecl wmain()
{
    DWORD exitCode = -1;
    int argc;
    LPCWSTR* wszArglist = const_cast<LPCWSTR*>(SegmentCommandLine(GetCommandLineW(), &argc));
    
    if (argc < 2)
    {
        // Invalid number of arguments
        return exitCode;
    }
    
    // This module is merely a shim to figure out what the actual EntryPoint assembly is and call the Host with 
    // that information. The EntryPoint would be found based on the following assumptions
    //
    // 1) Current module lives under the "CoreRuntime" subfolder of the AppX package installation folder.
    // 2) It has the same name as the EntryPoint assembly that will reside in the parent folder (i.e. the AppX package installation folder).
    
    const wchar_t* pActivationModulePath = wszArglist[0];

    const wchar_t *pLastSlash = wcsrchr(pActivationModulePath, L'\\');
    if (pLastSlash == NULL)
    {
        return exitCode;
    }
    
    wchar_t entryPointAssemblyFileName[MAX_PATH];
    IfFailRet(wcsncpy_s(entryPointAssemblyFileName, MAX_PATH, pActivationModulePath, pLastSlash-pActivationModulePath));
    IfFailRet(wcscat_s(entryPointAssemblyFileName, MAX_PATH, L"\\entrypoint"));
    IfFailRet(wcscat_s(entryPointAssemblyFileName, MAX_PATH, pLastSlash));
    
    auto success = ExecuteAssembly(entryPointAssemblyFileName, argc-1, &(wszArglist[1]), &exitCode);

    return exitCode;
}

