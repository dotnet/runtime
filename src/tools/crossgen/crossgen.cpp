// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

// Return values from wmain() in case of error
enum ReturnValues
{
    FAILURE_RESULT = 1,
    CLR_INIT_ERROR = -2,
    ASSEMBLY_NOT_FOUND = -3,
    INVALID_ARGUMENTS = -4
};

#define NumItems(s) (sizeof(s) / sizeof(s[0]))

STDAPI CreatePDBWorker(LPCWSTR pwzAssemblyPath, LPCWSTR pwzPlatformAssembliesPaths, LPCWSTR pwzTrustedPlatformAssemblies, LPCWSTR pwzPlatformResourceRoots, LPCWSTR pwzAppPaths, LPCWSTR pwzAppNiPaths, LPCWSTR pwzPdbPath, BOOL fGeneratePDBLinesInfo, LPCWSTR pwzManagedPdbSearchPath, LPCWSTR pwzPlatformWinmdPaths, LPCWSTR pwzDiasymreaderPath);
STDAPI NGenWorker(LPCWSTR pwzFilename, DWORD dwFlags, LPCWSTR pwzPlatformAssembliesPaths, LPCWSTR pwzTrustedPlatformAssemblies, LPCWSTR pwzPlatformResourceRoots, LPCWSTR pwzAppPaths, LPCWSTR pwzOutputFilename=NULL, LPCWSTR pwzPlatformWinmdPaths=NULL, ICorSvcLogger *pLogger = NULL, LPCWSTR pwszCLRJITPath = nullptr);
void SetSvcLogger(ICorSvcLogger *pCorSvcLogger);
void SetMscorlibPath(LPCWSTR wzSystemDirectory);

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
    vfwprintf(stdout, szFormat, args);
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
    Output(W("Microsoft (R) CoreCLR Native Image "));
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
       W("    /silent              - Do not display completion message\n")
       W("    /verbose             - Display verbose information\n")
       W("    @response.rsp        - Process command line arguments from specified\n")
       W("                           response file\n")
       W("    /in <file>           - Specifies input filename (optional)\n")
       W("    /out <file>          - Specifies output filename (optional)\n")
       W("    /r <file>            - Specifies a trusted platform assembly reference\n")
       W("                         - Cannot be used with /p\n")
       W("    /p <path[") PATH_SEPARATOR_STR_W W("path]>     - List of paths containing target platform assemblies\n")
       // If /p, we will use it to build the TPA list and thus,
       // TPA list cannot be explicitly specified.
       W("                         - Cannot be used with /r\n")

       W("    /Platform_Resource_Roots <path[") PATH_SEPARATOR_STR_W W("path]>\n")
       W("                         - List of paths containing localized assembly directories\n")
       W("    /App_Paths <path[") PATH_SEPARATOR_STR_W W("path]>\n")
       W("                         - List of paths containing user-application assemblies and resources\n")
#ifndef NO_NGENPDB
       W("    /App_Ni_Paths <path[") PATH_SEPARATOR_STR_W W("path]>\n")
       W("                         - List of paths containing user-application native images\n")
       W("                         - Must be used with /CreatePDB switch\n")
#endif // NO_NGENPDB
       
#ifdef FEATURE_COMINTEROP
       W("    /Platform_Winmd_Paths <path[") PATH_SEPARATOR_STR_W W("path]>\n")
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
#if !defined(FEATURE_MERGE_JIT_AND_ENGINE)
       W("    /JITPath <path>\n")
       W("                         - Specifies the absolute file path to JIT compiler to be used.\n")
#endif // !defined(FEATURE_MERGE_JIT_AND_ENGINE)
#ifdef FEATURE_READYTORUN_COMPILER
       W("    /ReadyToRun          - Generate images resilient to the runtime and\n")
       W("                           dependency versions\n")
       W("    /LargeVersionBubble  - Generate image with a version bubble including all\n")
       W("                           input assemblies\n")

#endif
#ifdef FEATURE_WINMD_RESILIENT
       W(" WinMD Parameters\n")
       W("    /WinMDResilient - Generate images resilient to WinMD dependency changes.\n")
#endif
       W(" Size on Disk Parameters\n")
       W("    /NoMetaData     - Do not copy metadata and IL into native image.\n")
#ifndef NO_NGENPDB
       W(" Debugging Parameters\n")
       W("    /CreatePDB <Dir to store PDB> [/lines [<search path for managed PDB>] ]\n")
       W("        When specifying /CreatePDB, the native image should be created\n")
       W("        first, and <assembly name> should be the path to the NI.\n")
       W("    /DiasymreaderPath <Path to diasymreader.dll>\n")
       W("        - Specifies the absolute file path to diasymreader.dll to be used.\n")
#elif defined(FEATURE_PERFMAP)
       W(" Debugging Parameters\n")
       W("    /CreatePerfMap <Dir to store perf map>\n")
       W("        When specifying /CreatePerfMap, the native image should be created\n")
       W("        first, and <assembly name> should be the path to the NI.\n")
#endif
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
bool StringEndsWith(LPCWSTR pwzString, LPCWSTR pwzCandidate)
{
    size_t stringLength = wcslen(pwzString);
    size_t candidateLength = wcslen(pwzCandidate);

    if (candidateLength > stringLength || stringLength == 0 || candidateLength == 0)
    {
        return false;
    }

    LPCWSTR pwzStringEnd = pwzString + stringLength - candidateLength;

    return !_wcsicmp(pwzStringEnd, pwzCandidate);
}

//
// When using the Phone binding model (TrustedPlatformAssemblies), automatically
// detect which path CoreLib.[ni.]dll lies in.
//
bool ComputeMscorlibPathFromTrustedPlatformAssemblies(SString& pwzMscorlibPath, LPCWSTR pwzTrustedPlatformAssemblies)
{
    LPWSTR wszTrustedPathCopy = new WCHAR[wcslen(pwzTrustedPlatformAssemblies) + 1];
    wcscpy_s(wszTrustedPathCopy, wcslen(pwzTrustedPlatformAssemblies) + 1, pwzTrustedPlatformAssemblies);
    wchar_t *context;
    LPWSTR wszSingleTrustedPath = wcstok_s(wszTrustedPathCopy, PATH_SEPARATOR_STR_W, &context);
    
    while (wszSingleTrustedPath != NULL)
    {
        size_t pathLength = wcslen(wszSingleTrustedPath);
        // Strip off enclosing quotes, if present
        if (wszSingleTrustedPath[0] == W('\"') && wszSingleTrustedPath[pathLength-1] == W('\"'))
        {
            wszSingleTrustedPath[pathLength-1] = '\0';
            wszSingleTrustedPath++;
        }

        if (StringEndsWith(wszSingleTrustedPath, DIRECTORY_SEPARATOR_STR_W CoreLibName_IL_W) ||
            StringEndsWith(wszSingleTrustedPath, DIRECTORY_SEPARATOR_STR_W CoreLibName_NI_W))
        {
            pwzMscorlibPath.Set(wszSingleTrustedPath);
            SString::Iterator pwzSeparator = pwzMscorlibPath.End();
            bool retval = true;
            
            if (!SUCCEEDED(CopySystemDirectory(pwzMscorlibPath, pwzMscorlibPath)))
            {
                retval = false;
            }

            delete [] wszTrustedPathCopy;
            return retval;
        }
        
        wszSingleTrustedPath = wcstok_s(NULL, PATH_SEPARATOR_STR_W, &context);
    }
    delete [] wszTrustedPathCopy;

    return false;
}

// Given a path terminated with "\\" and a search mask, this function will add
// the enumerated files, corresponding to the search mask, from the path into
// the refTPAList.
void PopulateTPAList(SString path, LPCWSTR pwszMask, SString &refTPAList, bool fCreatePDB)
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
            
            // No NIs are supported when creating NI images (other than NI of System.Private.CoreLib.dll).
            if (!fCreatePDB)
            {
                // Only CoreLib's ni.dll should be in the TPAList for the compilation of non-mscorlib assemblies.
                if (StringEndsWith((LPWSTR)pwszFilename, W(".ni.dll")))
                {
                    fAddFileToTPAList = false;
                }
            }
            
            if (fAddFileToTPAList)
            {
                if (fAddDelimiter)
                {
                    // Add the path delimiter if we already have entries in the TPAList
                    refTPAList.Append(PATH_SEPARATOR_CHAR_W);
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
void ComputeTPAListFromPlatformAssembliesPath(LPCWSTR pwzPlatformAssembliesPaths, SString &refTPAList, bool fCreatePDB)
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
            BOOL found = ssPlatformAssembliesPath.Find(itr, PATH_SEPARATOR_CHAR_W);
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
                if (qualifiedPath[len-1]!=DIRECTORY_SEPARATOR_CHAR_W)
                {
                    qualifiedPath.Append(DIRECTORY_SEPARATOR_CHAR_W);
                }

                // Enumerate the EXE/DLL modules within this path and add them to the TPAList
                EX_TRY
                {
                    PopulateTPAList(qualifiedPath, W("*.exe"), refTPAList, fCreatePDB);
                    PopulateTPAList(qualifiedPath, W("*.dll"), refTPAList, fCreatePDB);
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

extern HMODULE g_hThisInst;

int _cdecl wmain(int argc, __in_ecount(argc) WCHAR **argv)
{
#ifndef FEATURE_PAL
    g_hThisInst = WszGetModuleHandle(NULL);
#endif

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
    StackSString wzDirectoryToStorePDB;
    bool fCreatePDB = false;
    bool fGeneratePDBLinesInfo = false;
    LPWSTR pwzSearchPathForManagedPDB = NULL;
    LPCWSTR pwzOutputFilename = NULL;
    LPCWSTR pwzPublicKeys = nullptr;
    bool fLargeVersionBubbleSwitch = false;

#if !defined(FEATURE_MERGE_JIT_AND_ENGINE)
    LPCWSTR pwszCLRJITPath = nullptr;
#endif // !defined(FEATURE_MERGE_JIT_AND_ENGINE)

    LPCWSTR pwzDiasymreaderPath = nullptr;

    HRESULT hr;

#ifndef PLATFORM_UNIX
    // This is required to properly display Unicode characters
    _setmode(_fileno(stdout), _O_U8TEXT);
#endif

    // Skip this executable path
    argv++;
    argc--;

    ConsoleArgs consoleArgs;
    int argc2;
    LPWSTR *argv2;

    SString ssTrustedPlatformAssemblies;

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

    // By default, Crossgen will generate readytorun images unless /FragileNonVersionable switch is specified
    dwFlags |= NGENWORKER_FLAGS_READYTORUN;

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
        else if (MatchParameter(*argv, W("silent")))
        {
            dwFlags |= NGENWORKER_FLAGS_SILENT;
        }
        else if (MatchParameter(*argv, W("verbose")))
        {
            dwFlags |= NGENWORKER_FLAGS_VERBOSE;
        }
        else if (MatchParameter(*argv, W("Tuning")))
        {
            dwFlags |= NGENWORKER_FLAGS_TUNING;
        }
        else if (MatchParameter(*argv, W("MissingDependenciesOK")))
        {
            dwFlags |= NGENWORKER_FLAGS_MISSINGDEPENDENCIESOK;
        }
#if !defined(FEATURE_MERGE_JIT_AND_ENGINE)
        else if (MatchParameter(*argv, W("JITPath")) && (argc > 1))
        {
            pwszCLRJITPath = argv[1];
            
            // skip JIT Path
            argv++;
            argc--;
        }
#endif // !defined(FEATURE_MERGE_JIT_AND_ENGINE)
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
        else if (MatchParameter(*argv, W("FragileNonVersionable")))
        {
            dwFlags &= ~NGENWORKER_FLAGS_READYTORUN;
        }
        else if (MatchParameter(*argv, W("LargeVersionBubble")))
        {
            dwFlags |= NGENWORKER_FLAGS_LARGEVERSIONBUBBLE;
            fLargeVersionBubbleSwitch = true;
        }
#endif
        else if (MatchParameter(*argv, W("NoMetaData")))
        {
            dwFlags |= NGENWORKER_FLAGS_NO_METADATA;
        }
        else if (MatchParameter(*argv, W("out")))
        {
            if (pwzOutputFilename != NULL)
            {
                OutputErr(W("Cannot specify multiple output files.\n"));
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
                OutputErr(W("Cannot specify multiple input files.\n"));
                exit(INVALID_ARGUMENTS);
            }
            pwzFilename = argv[1];
            argv++;
            argc--;
        }
        else if (MatchParameter(*argv, W("r")) && (argc > 1))
        {
            if (!ssTrustedPlatformAssemblies.IsEmpty())
            {
                // Add the path delimiter if we already have entries in the TPAList
                ssTrustedPlatformAssemblies.Append(PATH_SEPARATOR_CHAR_W);
            }
            ssTrustedPlatformAssemblies.Append(argv[1]);

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
        // Note: Leaving "Platform_Assemblies_Paths" for backwards compatibility reasons.
        else if ((MatchParameter(*argv, W("Platform_Assemblies_Paths")) || MatchParameter(*argv, W("p"))) && (argc > 1))
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
#ifndef NO_NGENPDB
        else if (MatchParameter(*argv, W("CreatePDB")) && (argc > 1))
        {
            // syntax: /CreatePDB <directory to store PDB> [/lines  [<search path for managed PDB>] ]
            
            // Parse: /CreatePDB
            fCreatePDB = true;
            argv++;
            argc--;

            // Clear any extra flags - using /CreatePDB fails if any of these are set.
            dwFlags = dwFlags & ~NGENWORKER_FLAGS_READYTORUN;

            // Parse: <directory to store PDB>
            wzDirectoryToStorePDB.Set(argv[0]);
            argv++;
            argc--;

            // Ensure output dir ends in a backslash, or else diasymreader has issues
            if (wzDirectoryToStorePDB[wzDirectoryToStorePDB.GetCount()-1] != DIRECTORY_SEPARATOR_CHAR_W)
            {
                wzDirectoryToStorePDB.Append(DIRECTORY_SEPARATOR_STR_W);
            }

            if (argc == 0)
            {
                OutputErr(W("The /CreatePDB switch requires <directory to store PDB> and <assembly name>.\n"));
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
                    OutputErr(W("The /CreatePDB switch requires <directory to store PDB> and <assembly name>.\n"));
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
        else if (MatchParameter(*argv, W("DiasymreaderPath")) && (argc > 1))
        {
            pwzDiasymreaderPath = argv[1];

            // skip diasymreader Path
            argv++;
            argc--;
        }
#endif // NO_NGENPDB
#ifdef FEATURE_PERFMAP
        else if (MatchParameter(*argv, W("CreatePerfMap")) && (argc > 1))
        {
            // syntax: /CreatePerfMap <directory to store perfmap>

            // Parse: /CreatePerfMap
            // NOTE: We use the same underlying PDB logic.
            fCreatePDB = true;
            argv++;
            argc--;

            // Clear the /ready to run flag - /CreatePerfmap does not work with any other flags.
            dwFlags = dwFlags & ~NGENWORKER_FLAGS_READYTORUN;

            // Parse: <directory to store PDB>
            wzDirectoryToStorePDB.Set(argv[0]);
            argv++;
            argc--;

            // Ensure output dir ends in a backslash
            if (wzDirectoryToStorePDB[wcslen(wzDirectoryToStorePDB)-1] != DIRECTORY_SEPARATOR_CHAR_W)
            {
                wzDirectoryToStorePDB.Append(DIRECTORY_SEPARATOR_STR_W);
            }

            if (argc == 0)
            {
                OutputErr(W("The /CreatePerfMap switch requires <directory to store perfmap> and <assembly name>.\n"));
                exit(FAILURE_RESULT);
            }

            // Undo last arg iteration, since we do it for all cases at the bottom of
            // the loop
            argv--;
            argc++;
        }
#endif // FEATURE_PERFMAP
        else
        {
            if (argc == 1)
            {
#if !defined(PLATFORM_UNIX)
                // When not running on Mac or Linux, which can have forward-slash pathnames, we know
                // a command switch here means an invalid argument.
                if (*argv[0] == W('-') || *argv[0] == W('/'))
                {
                    OutputErrf(W("Invalid parameter: %s\n"), *argv);
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
                    OutputErr(W("Cannot use /In and specify an input file as the last argument.\n"));
                    exit(INVALID_ARGUMENTS);
                }
                
                pwzFilename = *argv;
                break;
            }
            else
            {
                OutputErrf(W("Invalid parameter: %s\n"), *argv);
                exit(INVALID_ARGUMENTS);
            }
        }

        argv++;
        argc--;
    }

    if (pwzFilename == NULL)
    {
        OutputErr(W("You must specify an assembly to compile\n"));
        exit(INVALID_ARGUMENTS);
    }

    if (fCreatePDB && (dwFlags != 0))
    {
        OutputErr(W("The /CreatePDB switch cannot be used with other switches, except /lines and the various path switches.\n"));
        exit(FAILURE_RESULT);
    }

    if (pwzAppNiPaths != nullptr && !fCreatePDB)
    {
        OutputErr(W("The /App_Ni_Paths switch can only be used with the /CreatePDB switch.\n"));
        exit(FAILURE_RESULT);
    }

#if !defined(FEATURE_MERGE_JIT_AND_ENGINE)
    if (pwszCLRJITPath != nullptr && fCreatePDB)
    {
        OutputErr(W("The /JITPath switch can not be used with the /CreatePDB switch.\n"));
        exit(FAILURE_RESULT);
    }
#endif // !defined(FEATURE_MERGE_JIT_AND_ENGINE)

#if !defined(NO_NGENPDB)
    if (pwzDiasymreaderPath != nullptr && !fCreatePDB)
    {
        OutputErr(W("The /DiasymreaderPath switch can only be used with the /CreatePDB switch.\n"));
        exit(FAILURE_RESULT);
    }
#endif // !defined(NO_NGENPDB)

    if (!ssTrustedPlatformAssemblies.IsEmpty())
    {
        pwzTrustedPlatformAssemblies = (WCHAR *)ssTrustedPlatformAssemblies.GetUnicode();
    }

    if ((pwzTrustedPlatformAssemblies != nullptr) && (pwzPlatformAssembliesPaths != nullptr))
    {
        OutputErr(W("The /r and /p switches cannot be both specified.\n"));
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
            OutputErr(W("The /NoMetaData switch can only be used with Windows.winmd.\n"));
            exit(FAILURE_RESULT);
        }
    }
    
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

    PathString wzTrustedPathRoot;

    SString ssTPAList;  

    if (fCreatePDB)
    {
        // While creating PDB, assembly binder gives preference to files in TPA.
        // This can create difficulties if the input file is not in TPA.
        // To avoid this issue, put the input file as the first item in TPA.
        ssTPAList.Append(pwzFilename);
    }

    if(pwzPlatformAssembliesPaths != nullptr)
    {
        // /p command line switch has been specified.
        _ASSERTE(pwzTrustedPlatformAssemblies == nullptr);
        
        // Formulate the TPAList from /p
        ComputeTPAListFromPlatformAssembliesPath(pwzPlatformAssembliesPaths, ssTPAList, fCreatePDB);
        pwzTrustedPlatformAssemblies = (WCHAR *)ssTPAList.GetUnicode();
        pwzPlatformAssembliesPaths = NULL;
    }

    if (pwzTrustedPlatformAssemblies != nullptr)
    {
        if (ComputeMscorlibPathFromTrustedPlatformAssemblies(wzTrustedPathRoot, pwzTrustedPlatformAssemblies))
        {
            pwzPlatformAssembliesPaths = wzTrustedPathRoot.GetUnicode();
            SetMscorlibPath(pwzPlatformAssembliesPaths);
        }
    }

    if (pwzPlatformAssembliesPaths == NULL)
    {
        if (!WszGetModuleFileName(NULL, wzTrustedPathRoot))
        {
            ERROR_WIN32(W("Error: GetModuleFileName failed (%d)\n"), GetLastError());
            exit(CLR_INIT_ERROR);
        }
        
        if (SUCCEEDED(CopySystemDirectory(wzTrustedPathRoot, wzTrustedPathRoot)))
        {
            pwzPlatformAssembliesPaths = wzTrustedPathRoot.GetUnicode();
        }
        else
        {
            ERROR_HR(W("Error: wcsrchr returned NULL; GetModuleFileName must have given us something bad\n"), E_UNEXPECTED);
            exit(CLR_INIT_ERROR);
        }
        
        
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
            pwzPlatformWinmdPaths,
            pwzDiasymreaderPath);
        
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
#if !defined(FEATURE_MERGE_JIT_AND_ENGINE)
        ,
        NULL, // ICorSvcLogger
        pwszCLRJITPath   
#endif // !defined(FEATURE_MERGE_JIT_AND_ENGINE)
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

#ifdef PLATFORM_UNIX
int main(int argc, char *argv[])
{
    if (0 != PAL_Initialize(argc, argv))
    {
        return FAILURE_RESULT;
    }

    wchar_t **wargv = new wchar_t*[argc];
    for (int i = 0; i < argc; i++)
    {
        size_t len = strlen(argv[i]) + 1;
        wargv[i] = new wchar_t[len];
        WszMultiByteToWideChar(CP_ACP, 0, argv[i], -1, wargv[i], len);
    }

    int ret = wmain(argc, wargv);

    for (int i = 0; i < argc; i++)
    {
        delete[] wargv[i];
    }
    delete[] wargv;

    return ret;
}
#endif // PLATFORM_UNIX
