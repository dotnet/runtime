//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

//
// TO DO: we currently use raw printf() for output. Maybe we need to pick up something like ngen's Output() handling
// to handle multiple code pages, etc, better.

#include <stdio.h>
#include <fcntl.h>
#include <io.h>

#include <windows.h>
#include <fxver.h>
#include <mscorsvc.h>

#include "palclr.h"

#include <sstring.h>
#include "ex.h"

#include "coregen.h"
#include "consoleargs.h"

#define SEPARATOR_CHAR_W W('\\')
#define SEPARATOR_STRING_W W("\\")

// Return values from wmain() in case of error
enum ReturnValues
{
    FAILURE_RESULT = 1,
    CLR_INIT_ERROR = -2,
    ASSEMBLY_NOT_FOUND = -3,
    INVALID_ARGUMENTS = -4
};

#define NumItems(s) (sizeof(s) / sizeof(s[0]))

STDAPI CreatePDBWorker(LPCWSTR pwzAssemblyPath, LPCWSTR pwzPlatformAssembliesPaths, LPCWSTR pwzTrustedPlatformAssemblies, LPCWSTR pwzPlatformResourceRoots, LPCWSTR pwzAppPaths, LPCWSTR pwzAppNiPaths, LPCWSTR pwzPdbPath, BOOL fGeneratePDBLinesInfo, LPCWSTR pwzManagedPdbSearchPath, LPCWSTR pwzPlatformWinmdPaths);
STDAPI NGenWorker(LPCWSTR pwzFilename, DWORD dwFlags, LPCWSTR pwzPlatformAssembliesPaths, LPCWSTR pwzTrustedPlatformAssemblies, LPCWSTR pwzPlatformResourceRoots, LPCWSTR pwzAppPaths, LPCWSTR pwzOutputFilename=NULL, LPCWSTR pwzPlatformWinmdPaths=NULL, ICorSvcLogger *pLogger = NULL);
void SetSvcLogger(ICorSvcLogger *pCorSvcLogger);
#ifdef FEATURE_CORECLR
void SetMscorlibPath(LPCWCHAR wzSystemDirectory);
#endif

/* --------------------------------------------------------------------------- *    
 * Console stuff
 * --------------------------------------------------------------------------- */

void Output(LPCWSTR str)
{
    wprintf(W("%s"), str);
}

void Outputf(LPCWSTR szFormat, ...)
{
    va_list args;
    va_start(args, szFormat);
    vwprintf(szFormat, args);
    va_end(args);
}

void OutputErr(LPCWSTR str)
{
    fwprintf(stderr, W("%s"), str);
}

void OutputErrf(LPCWSTR szFormat, ...)
{
    va_list args;
    va_start(args, szFormat);
    vfwprintf(stderr, szFormat, args);
    va_end(args);
}

void ErrorHR(HRESULT hr)
{
    OutputErrf(W("Error: failed to initialize CoreCLR: 0x%08x\n"), hr);
}

void ErrorWin32(DWORD err)
{
    ErrorHR(HRESULT_FROM_WIN32(err));
}

// Some error messages are useless to callers, so make them generic, except in debug builds, where we want as much
// information as possible.

#ifdef _DEBUG
#define ERROR_HR(msg,hr)        Outputf(msg, hr)
#define ERROR_WIN32(msg,err)    Outputf(msg, err)
#else // _DEBUG
#define ERROR_HR(msg,hr)        ErrorHR(hr)
#define ERROR_WIN32(msg,err)    ErrorWin32(err)
#endif // _DEBUG


void PrintLogoHelper()
{
#ifdef FEATURE_CORECLR
    Output(W("Microsoft (R) CoreCLR Native Image "));
#else
    Output(W("Microsoft (R) CLR Native Image "));
#endif
#ifdef MDIL
    Output(W("/ MDIL "));
#endif
    Outputf(W("Generator - Version %S\n"), VER_FILEVERSION_STR);
    Outputf(W("%S\n"), VER_LEGALCOPYRIGHT_LOGO_STR);
    Output(W("\n"));
}

void PrintUsageHelper()
{
    // Always print the logo when we print the usage, even if they've specified /nologo and we've parsed that already.
    PrintLogoHelper();

    Output(
       W("Usage: crossgen [args] <assembly name>\n")
       W("\n")
       W("    /? or /help          - Display this screen\n")
       W("    /nologo              - Prevents displaying the logo\n")
       W("    @response.rsp        - Process command line arguments from specified\n")
       W("                           response file\n")
       W("    /partialtrust        - Assembly will be run in a partial trust domain.\n")
       W("    /in <file>           - Specifies input filename (optional)\n")
#ifdef MDIL
       W("    /out <file>          - Specifies output filename (optional with native images,\n")
       W("                           required with MDIL)\n")
#else
       W("    /out <file>          - Specifies output filename (optional)\n")
#endif
#ifdef FEATURE_CORECLR
       W("    /Trusted_Platform_Assemblies <path[;path]>\n")
       W("                         - List of assemblies treated as trusted platform\n")
       W("                         - Cannot be used with Platform_Assemblies_Paths\n")
       W("    /Platform_Resource_Roots <path[;path]>\n")
       W("                         - List of paths containing localized assembly directories\n")
       W("    /App_Paths <path>    - List of paths containing user-application assemblies and resources\n")
#ifndef NO_NGENPDB
       W("    /App_Ni_Paths <path[;path]>\n")
       W("                         - List of paths containing user-application native images\n")
       W("                         - Must be used with /CreatePDB switch\n")
#endif // NO_NGENPDB
#endif // FEATURE_CORECLR

       W("    /Platform_Assemblies_Paths\n")
       W("                         - List of paths containing target platform assemblies\n")
#ifdef FEATURE_CORECLR
       // If Platform_Assemblies_Paths, we will use it to build the TPA list and thus,
       // TPA list cannot be explicitly specified.
       W("                         - Cannot be used with Trusted_Platform_Assemblies\n")
#endif // FEATURE_CORECLR
       
#ifdef FEATURE_COMINTEROP
       W("    /Platform_Winmd_Paths\n")
       W("                         - List of paths containing target platform WinMDs used\n")
       W("                           for emulating RoResolveNamespace\n")
#endif
       W("    /MissingDependenciesOK\n")
       W("                         - Specifies that crossgen should attempt not to fail\n")
       W("                           if a dependency is missing.\n")
#if 0
       W("    /Tuning              - Generate an instrumented image to collect\n")
       W("                           scenario traces, which can be used with ibcmerge.exe\n")
#endif
#ifdef FEATURE_READYTORUN_COMPILER
       W("    /ReadyToRun          - Generate images resilient to the runtime and\n")
       W("                           dependency versions\n")
#endif
#ifdef FEATURE_LEGACYNETCF
       W(" Compatability Modes\n")
       W("    /PreWP8App           - Set the Windows Phone 8 \"Quirks\" mode, namely AppDomainCompatSwitch=\n")
       W("                           WindowsPhone_3.7.0.0 or WindowsPhone_3.8.0.0.\n")
#endif
#ifdef MDIL
       W(" MDIL Generation Parameters\n")
       W("    /mdil           - Generate MDIL rather than native code. Requires presence of /out switch.\n")
       W("    /nomdil         - create MDIL image with no MDIL code or CTL data structures, use to force\n")
       W("                      fall back to JIT\n")
       W("    /EmbedMDIL      - Embed a previously created mdil data in IL image into native image.\n")
       W("    /fxmdil         - Generate framework assembly MDIL images containing minimal MDIL\n")
#endif // MDIL
#ifdef FEATURE_WINMD_RESILIENT
       W(" WinMD Parameters\n")
       W("    /WinMDResilient - Generate images resilient to WinMD dependency changes.\n")
#endif
#ifdef FEATURE_CORECLR
       W(" Size on Disk Parameters\n")
       W("    /NoMetaData     - Do not copy metadata and IL into native image.\n")
#ifndef NO_NGENPDB
       W(" Debugging Parameters\n")
       W("    /CreatePDB <Dir to store PDB> [/lines [<search path for managed PDB>] ]\n")
       W("        When specifying /CreatePDB, the native image should be created\n")
       W("        first, and <assembly name> should be the path to the NI.")
#endif // NO_NGENPDB
#endif // FEATURE_CORECLR
       );
}

class CrossgenLogger : public ICorSvcLogger
{
    STDMETHODIMP_(ULONG)    AddRef()  {return E_NOTIMPL;}
    STDMETHODIMP_(ULONG)    Release() {return E_NOTIMPL;}
    STDMETHODIMP            QueryInterface(REFIID riid,void ** ppv)
    {
        if (ppv==0) 
            return E_POINTER;
        
        *ppv = NULL;
        
        if (IsEqualIID(riid, IID_ICorSvcLogger) || IsEqualIID(riid, IID_IUnknown))
        {
            *ppv = this;
            return S_OK;
        }
        else
        {
            return E_NOINTERFACE;
        }
    }

    HRESULT STDMETHODCALLTYPE Log(        
            /*[in] */CorSvcLogLevel logLevel,
            /*[in] */BSTR message
        )
    {
        if (logLevel == LogLevel_Error)
            OutputErr(message);
        else
            Output(message);
        return S_OK;
    }
};

CrossgenLogger                g_CrossgenLogger;

//
// Tests whether szArg, the currently indexed argv matches the specified parameter name, szTestParamName.
// Specify szTestParamName without a switch.  This method handles testing for - and / switches.
//
bool MatchParameter(LPCWSTR szArg, LPCWSTR szTestParamName)
{
    if (wcslen(szArg) == 0)
    {
        return false;
    }

    if (szArg[0] != W('/') && szArg[0] != W('-'))
    {
        return false;
    }

    return !_wcsicmp(szArg + 1, szTestParamName) || !_wcsicmp(szArg + 1, szTestParamName);
}

//
// Returns true if pwzString ends with the string in pwzCandidate
// Ignores case
//
bool StringEndsWith(LPWSTR pwzString, LPWSTR pwzCandidate)
{
    size_t stringLength = wcslen(pwzString);
    size_t candidateLength = wcslen(pwzCandidate);

    if (candidateLength > stringLength || stringLength == 0 || candidateLength == 0)
    {
        return false;
    }

    LPWSTR pwzStringEnd = pwzString + stringLength - candidateLength;

    return !_wcsicmp(pwzStringEnd, pwzCandidate);
}

#ifdef FEATURE_CORECLR
//
// When using the Phone binding model (TrustedPlatformAssemblies), automatically
// detect which path mscorlib.[ni.]dll lies in.
//
bool ComputeMscorlibPathFromTrustedPlatformAssemblies(LPWSTR pwzMscorlibPath, DWORD cbMscorlibPath, LPCWSTR pwzTrustedPlatformAssemblies)
{
    LPWSTR wszTrustedPathCopy = new WCHAR[wcslen(pwzTrustedPlatformAssemblies) + 1];
    wcscpy_s(wszTrustedPathCopy, wcslen(pwzTrustedPlatformAssemblies) + 1, pwzTrustedPlatformAssemblies);
    LPWSTR wszSingleTrustedPath = wcstok(wszTrustedPathCopy, W(";"));
    
    while (wszSingleTrustedPath != NULL)
    {
        size_t pathLength = wcslen(wszSingleTrustedPath);
        // Strip off enclosing quotes, if present
        if (wszSingleTrustedPath[0] == W('\"') && wszSingleTrustedPath[pathLength-1] == W('\"'))
        {
            wszSingleTrustedPath[pathLength-1] = '\0';
            wszSingleTrustedPath++;
        }

        if (StringEndsWith(wszSingleTrustedPath, W("\\mscorlib.dll")) ||
            StringEndsWith(wszSingleTrustedPath, W("\\mscorlib.ni.dll")))
        {
            wcscpy_s(pwzMscorlibPath, cbMscorlibPath, wszSingleTrustedPath);
            
            LPWSTR pwzSeparator = wcsrchr(pwzMscorlibPath, W('\\'));
            if (pwzSeparator == NULL)
            {
                delete [] wszTrustedPathCopy;
                return false;
            }
            pwzSeparator[1] = W('\0'); // after '\'

            delete [] wszTrustedPathCopy;
            return true;
        }
        
        wszSingleTrustedPath = wcstok(NULL, W(";"));
    }
    delete [] wszTrustedPathCopy;

    return false;
}

// Given a path terminated with "\\" and a search mask, this function will add
// the enumerated files, corresponding to the search mask, from the path into
// the refTPAList.
void PopulateTPAList(SString path, LPWSTR pwszMask, SString &refTPAList, bool fCompilingMscorlib, bool fCreatePDB)
{
    _ASSERTE(path.GetCount() > 0);
    ClrDirectoryEnumerator folderEnumerator(path.GetUnicode(), pwszMask);
    
    while (folderEnumerator.Next())
    {
        // Got a valid enumeration handle and the data about the first file.
        DWORD dwAttributes = folderEnumerator.GetFileAttributes();
        if ((!(dwAttributes & FILE_ATTRIBUTE_DIRECTORY)) && (!(dwAttributes & FILE_ATTRIBUTE_DEVICE)))
        {
            bool fAddDelimiter = (refTPAList.GetCount() > 0)?true:false;
            bool fAddFileToTPAList = true;
            LPCWSTR pwszFilename = folderEnumerator.GetFileName();
            if (fCompilingMscorlib)
            {
                // When compiling mscorlib.dll, no ".ni.dll" should be on the TPAList.
                if (StringEndsWith((LPWSTR)pwszFilename, W(".ni.dll")))
                {
                    fAddFileToTPAList = false;
                }
            }
            else
            {
                // When creating PDBs, we must ensure that .ni.dlls are in the TPAList
                if (!fCreatePDB)
                {
                    // Only mscorlib.ni.dll should be in the TPAList for the compilation of non-mscorlib assemblies.
                    if (StringEndsWith((LPWSTR)pwszFilename, W(".ni.dll")))
                    {
                        if (!StringEndsWith((LPWSTR)pwszFilename, W("mscorlib.ni.dll")))
                        {
                            fAddFileToTPAList = false;
                        }
                    }
                }
                
                // Ensure that mscorlib.dll is also not on the TPAlist for this case.                
                if (StringEndsWith((LPWSTR)pwszFilename, W("mscorlib.dll")))
                {
                    fAddFileToTPAList = false;
                }
            }
            
            if (fAddFileToTPAList)
            {
                if (fAddDelimiter)
                {
                    // Add the path delimiter if we already have entries in the TPAList
                    refTPAList.Append(W(";"));
                }
                // Add the path to the TPAList
                refTPAList.Append(path);
                refTPAList.Append(pwszFilename);
            } 
        }
    }
 }

// Given a semi-colon delimited set of absolute folder paths (pwzPlatformAssembliesPaths), this function
// will enumerate all EXE/DLL modules in those folders and add them to the TPAList buffer (refTPAList).
void ComputeTPAListFromPlatformAssembliesPath(LPCWSTR pwzPlatformAssembliesPaths, SString &refTPAList, bool fCompilingMscorlib, bool fCreatePDB)
{
    // We should have a valid pointer to the paths
    _ASSERTE(pwzPlatformAssembliesPaths != NULL);
    
    SString ssPlatformAssembliesPath(pwzPlatformAssembliesPaths);
    
    // Platform Assemblies Path List is semi-colon delimited
    if(ssPlatformAssembliesPath.GetCount() > 0)
    {
        SString::CIterator start = ssPlatformAssembliesPath.Begin();
        SString::CIterator itr = ssPlatformAssembliesPath.Begin();
        SString::CIterator end = ssPlatformAssembliesPath.End();
        SString qualifiedPath;

        while (itr != end)
        {
            start = itr;
            BOOL found = ssPlatformAssembliesPath.Find(itr, W(';'));
            if (!found)
            {
                itr = end;
            }

            SString qualifiedPath(ssPlatformAssembliesPath,start,itr);

            if (found)
            {
                itr++;
            }

            unsigned len = qualifiedPath.GetCount();

            if (len > 0)
            {
                if (qualifiedPath[len-1]!='\\')
                {
                    qualifiedPath.Append('\\');
                }

                // Enumerate the EXE/DLL modules within this path and add them to the TPAList
                EX_TRY
                {
                    PopulateTPAList(qualifiedPath, W("*.exe"), refTPAList, fCompilingMscorlib, fCreatePDB);
                    PopulateTPAList(qualifiedPath, W("*.dll"), refTPAList, fCompilingMscorlib, fCreatePDB);
                }
                EX_CATCH
                {
                    Outputf(W("Warning: Error enumerating files under %s.\n"), qualifiedPath.GetUnicode());
                }
                EX_END_CATCH(SwallowAllExceptions);
            }
        }
    }
}
#endif // FEATURE_CORECLR

extern HMODULE g_hThisInst;

int _cdecl wmain(int argc, __in_ecount(argc) WCHAR **argv)
{
    g_hThisInst = WszGetModuleHandle(NULL);

    /////////////////////////////////////////////////////////////////////////
    //
    // Parse the arguments
    //
    bool fDisplayLogo = true;
    DWORD dwFlags = 0;
    LPCWSTR pwzFilename = NULL;
    LPCWSTR pwzPlatformResourceRoots = nullptr;
    LPCWSTR pwzTrustedPlatformAssemblies = nullptr;
    LPCWSTR pwzAppPaths = nullptr;
    LPCWSTR pwzAppNiPaths = nullptr;
    LPCWSTR pwzPlatformAssembliesPaths = nullptr;
    LPCWSTR pwzPlatformWinmdPaths = nullptr;
    WCHAR wzDirectoryToStorePDB[MAX_PATH] = W("\0");
    bool fCreatePDB = false;
    bool fGeneratePDBLinesInfo = false;
    LPWSTR pwzSearchPathForManagedPDB = NULL;
    LPCWSTR pwzOutputFilename = NULL;
    LPCWSTR pwzPublicKeys = nullptr;

    HRESULT hr;

    // This is required to properly display Unicode characters
    _setmode(_fileno(stdout), _O_U8TEXT);

    // Skip this executable path
    argv++;
    argc--;

    ConsoleArgs consoleArgs;
    int argc2;
    LPWSTR *argv2;

    if (argc == 0)
    {
        PrintUsageHelper();
        exit(INVALID_ARGUMENTS);
    }
    
    if (!consoleArgs.ExpandResponseFiles(argc, argv, &argc2, &argv2))
    {
        if (consoleArgs.ErrorMessage() != nullptr)
        {
            wprintf(consoleArgs.ErrorMessage());
            exit(FAILURE_RESULT);
        }
    }
    
    argc = argc2;
    argv = argv2;

    bool fCopySourceToOut = false;
    
    // By default, Crossgen will assume code-generation for fulltrust domains unless /PartialTrust switch is specified
    dwFlags |= NGENWORKER_FLAGS_FULLTRUSTDOMAIN;

    while (argc > 0)
    {
        if (MatchParameter(*argv, W("?"))
            || MatchParameter(*argv, W("help")))
        {
            PrintUsageHelper();
            exit(INVALID_ARGUMENTS);
        }
        else if (MatchParameter(*argv, W("nologo")))
        {
            fDisplayLogo = false;
        }
        else if (MatchParameter(*argv, W("Tuning")))
        {
            dwFlags |= NGENWORKER_FLAGS_TUNING;
        }
        else if (MatchParameter(*argv, W("MissingDependenciesOK")))
        {
            dwFlags |= NGENWORKER_FLAGS_MISSINGDEPENDENCIESOK;
        }
        else if (MatchParameter(*argv, W("PartialTrust")))
        {
            // Clear the /fulltrust flag
            dwFlags = dwFlags & ~NGENWORKER_FLAGS_FULLTRUSTDOMAIN;
        }
        else if (MatchParameter(*argv, W("FullTrust")))
        {
            // Keep the "/fulltrust" switch around but let it be no-nop. Without this, any usage of /fulltrust will result in crossgen command-line
            // parsing failure. Considering that scripts all over (CLR, Phone Build, etc) specify that switch, we let it be as opposed to going
            // and fixing all the scripts.
            //
            // We dont explicitly set the flag here again so that if "/PartialTrust" is specified, then it will successfully override the default
            // fulltrust behaviour.
        }
#ifdef FEATURE_LEGACYNETCF
        else if (MatchParameter(*argv, W("PreWP8App")))
        {
            dwFlags |= NGENWORKER_FLAGS_APPCOMPATWP8;
        }
#endif
#ifdef MDIL
        else if (MatchParameter(*argv, W("mdil")))
        {
            dwFlags |= NGENWORKER_FLAGS_CREATEMDIL;
        }
        else if (MatchParameter(*argv, W("fxmdil")))
        {
            dwFlags |= NGENWORKER_FLAGS_MINIMAL_MDIL | NGENWORKER_FLAGS_CREATEMDIL;
        }
        else if (MatchParameter(*argv, W("EmbedMDIL")))
        {
            dwFlags |= NGENWORKER_FLAGS_EMBEDMDIL;
        }
        else if (MatchParameter(*argv, W("NoMDIL")))
        {
            dwFlags |= NGENWORKER_FLAGS_NOMDIL;
        }
#else // !MDIL
        else if (MatchParameter(*argv, W("mdil")) || MatchParameter(*argv, W("fxmdil")) || MatchParameter(*argv, W("NoMDIL")))
        {
            // Copy the "in" file as the "out" file
            fCopySourceToOut = true;
        }
        else if (MatchParameter(*argv, W("EmbedMDIL")))
        {
            // Dont do anything - simply generate the NI
        }
#endif
#ifdef FEATURE_WINMD_RESILIENT
        else if (MatchParameter(*argv, W("WinMDResilient")))
        {
            dwFlags |= NGENWORKER_FLAGS_WINMD_RESILIENT;
        }
#endif
#ifdef FEATURE_READYTORUN_COMPILER
        else if (MatchParameter(*argv, W("ReadyToRun")))
        {
            dwFlags |= NGENWORKER_FLAGS_READYTORUN;
        }
#endif
#ifdef FEATURE_CORECLR
        else if (MatchParameter(*argv, W("NoMetaData")))
        {
            dwFlags |= NGENWORKER_FLAGS_NO_METADATA;
        }
#endif
        else if (MatchParameter(*argv, W("out")))
        {
            if (pwzOutputFilename != NULL)
            {
                Output(W("Cannot specify multiple output files.\n"));
                exit(INVALID_ARGUMENTS);
            }
            pwzOutputFilename = argv[1];
            argv++;
            argc--;
        }
        else if (MatchParameter(*argv, W("in")))
        {
            if (pwzFilename != NULL)
            {
                Output(W("Cannot specify multiple input files.\n"));
                exit(INVALID_ARGUMENTS);
            }
            pwzFilename = argv[1];
            argv++;
            argc--;
        }
#ifdef FEATURE_CORECLR
        else if (MatchParameter(*argv, W("Trusted_Platform_Assemblies")) && (argc > 1))
        {
            pwzTrustedPlatformAssemblies = argv[1];

            // skip path list
            argv++;
            argc--;
        }
        else if (MatchParameter(*argv, W("Platform_Resource_Roots")) && (argc > 1))
        {
            pwzPlatformResourceRoots = argv[1];

            // skip path list
            argv++;
            argc--;
        }
        else if (MatchParameter(*argv, W("App_Paths")) && (argc > 1))
        {
            pwzAppPaths = argv[1];

            // skip User app path
            argv++;
            argc--;
        }
#ifndef NO_NGENPDB
        else if (MatchParameter(*argv, W("App_Ni_Paths")) && (argc > 1))
        {
            pwzAppNiPaths = argv[1];

            // skip User app path
            argv++;
            argc--;
        }
#endif // NO_NGENPDB
#endif // FEATURE_CORECLR
        else if (MatchParameter(*argv, W("Platform_Assemblies_Paths")) && (argc > 1))
        {
            pwzPlatformAssembliesPaths = argv[1];
            
            // skip path list
            argv++;
            argc--;
        }
#ifdef FEATURE_COMINTEROP
        else if (MatchParameter(*argv, W("Platform_Winmd_Paths")) && (argc > 1))
        {
            pwzPlatformWinmdPaths = argv[1];

            // skip User app path
            argv++;
            argc--;
        }
#endif // FEATURE_COMINTEROP
#if defined(FEATURE_CORECLR) && !defined(NO_NGENPDB)
        else if (MatchParameter(*argv, W("CreatePDB")) && (argc > 1))
        {
            // syntax: /CreatePDB <directory to store PDB> [/lines  [<search path for managed PDB>] ]
            
            // Parse: /CreatePDB
            fCreatePDB = true;
            argv++;
            argc--;

            // Clear the /fulltrust flag - /CreatePDB does not work with any other flags.
            dwFlags = dwFlags & ~NGENWORKER_FLAGS_FULLTRUSTDOMAIN;

            // Parse: <directory to store PDB>
            if (wcscpy_s(
                wzDirectoryToStorePDB, 
                _countof(wzDirectoryToStorePDB), 
                argv[0]) != 0)
            {
                Output(W("Unable to parse output directory to store PDB"));
                exit(FAILURE_RESULT);
            }
            argv++;
            argc--;

            // Ensure output dir ends in a backslash, or else diasymreader has issues
            if (wzDirectoryToStorePDB[wcslen(wzDirectoryToStorePDB)-1] != SEPARATOR_CHAR_W)
            {
                if (wcscat_s(
                        wzDirectoryToStorePDB, 
                        _countof(wzDirectoryToStorePDB), 
                        SEPARATOR_STRING_W) != 0)
                {
                    Output(W("Unable to parse output directory to store PDB"));
                    exit(FAILURE_RESULT);
                }
            }

            if (argc == 0)
            {
                Output(W("The /CreatePDB switch requires <directory to store PDB> and <assembly name>.\n"));
                exit(FAILURE_RESULT);
            }

            // [/lines  [<search path for managed PDB>] ]
            if (MatchParameter(*argv, W("lines")) && (argc > 1))
            {
                // Parse: /lines
                fGeneratePDBLinesInfo = true;
                argv++;
                argc--;

                if (argc == 0)
                {
                    Output(W("The /CreatePDB switch requires <directory to store PDB> and <assembly name>.\n"));
                    exit(FAILURE_RESULT);
                }

                if (argc > 1)
                {
                    // Parse: <search path for managed PDB>
                    pwzSearchPathForManagedPDB = argv[0];
                    argv++;
                    argc--;
                }
            }

            // Undo last arg iteration, since we do it for all cases at the bottom of
            // the loop
            argv--;
            argc++;
        }
#endif // FEATURE_CORECLR && !NO_NGENPDB
        else
        {
            if (argc == 1)
            {
#if !defined(FEATURE_PAL)
                // When not running on Mac, which can have forward-slash pathnames, we know
                // a command switch here means an invalid argument.
                if (*argv[0] == W('-') || *argv[0] == W('/'))
                {
                    Outputf(W("Invalid parameter: %s\n"), *argv);
                    exit(INVALID_ARGUMENTS);
                }
#endif //!FEATURE_PAL
                // The last thing on the command line is an assembly name or path, and
                // because we got this far is not an argument like /nologo. Because this
                // code works on Mac, with forward-slash pathnames, we can't assume
                // anything with a forward slash is an argument. So we just always
                // assume the last thing on the command line must be an assembly name.

                if (pwzFilename != NULL)
                {
                    Output(W("Cannot use /In and specify an input file as the last argument.\n"));
                    exit(INVALID_ARGUMENTS);
                }
                
                pwzFilename = *argv;
                break;
            }
            else
            {
                Outputf(W("Invalid parameter: %s\n"), *argv);
                exit(INVALID_ARGUMENTS);
            }
        }

        argv++;
        argc--;
    }

    if (pwzFilename == NULL)
    {
        Output(W("You must specify an assembly to compile\n"));
        exit(INVALID_ARGUMENTS);
    }

#ifdef MDIL
    if (pwzOutputFilename == NULL)
    {
        if (dwFlags & NGENWORKER_FLAGS_CREATEMDIL)
        {
            Output(W("You must specify an output filename (/out <file>)\n"));
            exit(INVALID_ARGUMENTS);
        }
    }
    
    if ((dwFlags & NGENWORKER_FLAGS_EMBEDMDIL) && (dwFlags & NGENWORKER_FLAGS_CREATEMDIL))
    {
        Output(W("The /EmbedMDIL switch cannot be used with the /mdil or /createmdil switch.\n"));
        exit(INVALID_ARGUMENTS);
    }
    
    if ((dwFlags & NGENWORKER_FLAGS_NOMDIL) && !(dwFlags & NGENWORKER_FLAGS_CREATEMDIL))
    {
        Output(W("The /NoMDIL switch must be used with the /mdil or /createmdil switch.\n"));
        exit(INVALID_ARGUMENTS);
    }
#else // !MDIL
    if (fCopySourceToOut == true)
    {
        if (pwzOutputFilename == NULL)
        {
            Output(W("You must specify an output filename (/out <file>)\n"));
            exit(INVALID_ARGUMENTS);
        }
        if (CopyFileExW(pwzFilename, pwzOutputFilename, NULL, NULL, NULL, 0) == 0)
        {
            DWORD dwLastError = GetLastError();
            OutputErrf(W("Error: x86 copy failed for \"%s\" (0x%08x)\n"), pwzFilename, HRESULT_FROM_WIN32(dwLastError));
        }
        else
        {
            Outputf(W("[x86] %s generated successfully\n"),pwzOutputFilename);
        }
        
        return 0;
    }
#endif //MDIL

    if (fCreatePDB && (dwFlags != 0))
    {
        Output(W("The /CreatePDB switch cannot be used with other switches, except /lines and the various path switches.\n"));
        exit(FAILURE_RESULT);
    }

    if (pwzAppNiPaths != nullptr && !fCreatePDB)
    {
        Output(W("The /App_Ni_Paths switch can only be used with the /CreatePDB switch.\n"));
        exit(FAILURE_RESULT);
    }

#if defined(FEATURE_CORECLR)
    if ((pwzTrustedPlatformAssemblies != nullptr) && (pwzPlatformAssembliesPaths != nullptr))
    {
        Output(W("The /Trusted_Platform_Assemblies and /Platform_Assemblies_Paths switches cannot be both specified.\n"));
        exit(FAILURE_RESULT);
    }

    if ((dwFlags & NGENWORKER_FLAGS_NO_METADATA) != 0)
    {
        const size_t windowsDotWinmdLength = 13;    // Length of string "Windows.winmd"
        size_t filenameLength = wcslen(pwzFilename);
        bool isWindowsDotWinmd = true;
        if (filenameLength < windowsDotWinmdLength ||
            _wcsicmp(pwzFilename + filenameLength - windowsDotWinmdLength, W("windows.winmd")) != 0)
        {
            isWindowsDotWinmd = false;
        }
        else if (filenameLength > windowsDotWinmdLength)
        {
            WCHAR pathSeparator = pwzFilename[filenameLength - windowsDotWinmdLength - 1];
            if (pathSeparator != W('\\') && pathSeparator != W('/') && pathSeparator != W(':'))
            {
                isWindowsDotWinmd = false;
            }
        }
        if (!isWindowsDotWinmd)
        {
            Output(W("The /NoMetaData switch can only be used with Windows.winmd.\n"));
            exit(FAILURE_RESULT);
        }
    }
#endif // FEATURE_CORESYSTEM

#ifdef FEATURE_READYTORUN_COMPILER
    if (((dwFlags & NGENWORKER_FLAGS_TUNING) != 0) && ((dwFlags & NGENWORKER_FLAGS_READYTORUN) != 0))
    {
        Output(W("The /Tuning switch cannot be used with /ReadyToRun switch.\n"));
        exit(FAILURE_RESULT);
    }
#endif
    
    // All argument processing has happened by now. The only messages that should appear before here are errors
    // related to argument parsing, such as the Usage message. Afterwards, other messages can appear.

    /////////////////////////////////////////////////////////////////////////
    //
    // Start processing
    //

    if (fDisplayLogo)
    {
        PrintLogoHelper();
    }

    WCHAR wzTrustedPathRoot[MAX_PATH];

#ifdef FEATURE_CORECLR
    SString ssTPAList;  
    
    // Are we compiling mscorlib.dll? 
    bool fCompilingMscorlib = StringEndsWith((LPWSTR)pwzFilename, W("mscorlib.dll"));
    
    if(pwzPlatformAssembliesPaths != nullptr)
    {
        // Platform_Assemblies_Paths command line switch has been specified.
        _ASSERTE(pwzTrustedPlatformAssemblies == nullptr);
        
        // Formulate the TPAList from Platform_Assemblies_Paths
        ComputeTPAListFromPlatformAssembliesPath(pwzPlatformAssembliesPaths, ssTPAList, fCompilingMscorlib, fCreatePDB);
        pwzTrustedPlatformAssemblies = (WCHAR *)ssTPAList.GetUnicode();
        pwzPlatformAssembliesPaths = NULL;
    }

    if (pwzTrustedPlatformAssemblies != nullptr)
    {
        if (ComputeMscorlibPathFromTrustedPlatformAssemblies(wzTrustedPathRoot, MAX_PATH, pwzTrustedPlatformAssemblies))
        {
            pwzPlatformAssembliesPaths = wzTrustedPathRoot;
            SetMscorlibPath(pwzPlatformAssembliesPaths);
        }
    }
#endif // FEATURE_CORECLR

    if (pwzPlatformAssembliesPaths == NULL)
    {
        if (!WszGetModuleFileName(NULL, wzTrustedPathRoot, MAX_PATH))
        {
            ERROR_WIN32(W("Error: GetModuleFileName failed (%d)\n"), GetLastError());
            exit(CLR_INIT_ERROR);
        }

        wchar_t* pszSep = wcsrchr(wzTrustedPathRoot, SEPARATOR_CHAR_W);
        if (pszSep == NULL)
        {
            ERROR_HR(W("Error: wcsrchr returned NULL; GetModuleFileName must have given us something bad\n"), E_UNEXPECTED);
            exit(CLR_INIT_ERROR);
        }
        *pszSep = '\0';

        pwzPlatformAssembliesPaths = wzTrustedPathRoot;
    }

    // Initialize the logger
    SetSvcLogger(&g_CrossgenLogger);

    //Step - Compile the assembly

    if (fCreatePDB)
    {
        hr = CreatePDBWorker(
            pwzFilename, 
            pwzPlatformAssembliesPaths, 
            pwzTrustedPlatformAssemblies, 
            pwzPlatformResourceRoots, 
            pwzAppPaths, 
            pwzAppNiPaths,
            wzDirectoryToStorePDB, 
            fGeneratePDBLinesInfo, 
            pwzSearchPathForManagedPDB,
            pwzPlatformWinmdPaths);
        
    }
    else
    {
        hr = NGenWorker(pwzFilename, dwFlags,
         pwzPlatformAssembliesPaths,
         pwzTrustedPlatformAssemblies,
         pwzPlatformResourceRoots,
         pwzAppPaths,
         pwzOutputFilename,
         pwzPlatformWinmdPaths
         );
    }
    

    if (FAILED(hr))
    {
        if (hr == HRESULT_FROM_WIN32(ERROR_FILE_NOT_FOUND))
        {
            OutputErrf(W("Error: file \"%s\" or one of its dependencies was not found\n"), pwzFilename);
            exit(ASSEMBLY_NOT_FOUND);
        }
        else
        {
            OutputErrf(W("Error: compilation failed for \"%s\" (0x%08x)\n"), pwzFilename, hr);
            exit(hr);
        }
    }

    return 0;
}
