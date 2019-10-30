// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// This file is the interface to NGen (*N*ative code *G*eneration),
// which compiles IL modules to machine code ahead-of-time.
// This avoids the need for JIT compiling the code at program startup.
//


#ifndef _NGEN_H_
#define _NGEN_H_

#include "mscorsvc.h"

// Log size default of 10 MB.
#define DEFAULT_SERVICE_LOG_SIZE (10 * 1024 * 1024)
// NGEN.log is smaller 1MB
#define DEFAULT_MACHINE_WIDE_LOG_SIZE (1 * 1024 * 1024)
// User specific log is yet smaller 200KB
#define DEFAULT_USER_WIDE_LOG_SIZE (200 * 1024)
// Log size default of 100 KB. This should be big enough to hold some log info from roughly 100 
// ngen events. (Roughly 200KB of space including secondary log file.)
#define DEFAULT_APPLOCAL_WIDE_LOG_SIZE (100*1024)

#define NGEN_LOG_HEADER_TEXT W("To learn about increasing the verbosity of the NGen log files please see http://go.microsoft.com/fwlink/?linkid=210113\r\n")

// supported debug info types
enum DebugType
{
    DT_NIL,
    DT_PDB
};

//
// IMPORTANT * IMPORTANT * IMPORTANT * IMPORTANT * IMPORTANT * IMPORTANT
//
// This structure cannot have any fields removed from it!!!
//
// IMPORTANT * IMPORTANT * IMPORTANT * IMPORTANT * IMPORTANT * IMPORTANT
//
// NGening a module invokes the runtime against which the module was built.
// Thus this structure needs to be backwards-compatible.
//
// If additional options need to be added to this structure, 
// add them to the end of the structure and make sure you update
// logic throughout the runtime to look at a different size in the dwSize
// field. This is how we'll 'version' this structure.
// 
// If you are adding a code-generation flag (like debug or prof), use
// fInstrument as a template (but be sure to add your new flag as the
// last element in the struct). 

typedef struct _NGenOptions
{
    DWORD       dwSize;         // Size of the structure. Used to version the structure
    
    // V1
    
    bool        fDebug;         // Generate debuggable code and debug information
    bool        fDebugOpt;      // Generate debugging information, but optimized code
    bool        fProf;          // Generate instrumented code for profiling (call graphs)
    
    bool        fSilent;        // Dont spew text output
    LPCWSTR     lpszExecutableFileName; // Name of the module to ngen

    // V2 (Whidbey)

    bool        fInstrument;    // Generate instrumented code for basic-block profiling

    // No longer supported
    bool        fWholeProgram;  // Do cross function optimizations (whole program)

    // No longer supported
    bool        fProfInfo;      // Embed working set profiling data into the image

    LPCWSTR     lpszRepositoryDir;      // Directory for repository of native images
    RepositoryFlags repositoryFlags;

    // No longer supported
    DebugType   dtRequested;    // the requested debug type
    LPCWSTR     lpszDebugDir;   // the name of the output debug dir for the above

    // No longer supported
    bool        fNoInstall;     // Creates stand alone ngen-images that can be installed in the NIC later

    // No longer supported
    bool        fEmitFixups;    // Support for Vulcan
    bool        fFatHeaders;

    // Diagnostic flags
    bool        fVerbose;       // print verbose descriptions of native images
    unsigned    uStats;         // image stats mask

#define LAST_WHIDBEY_NGENOPTION uStats

    // V4
    bool        fNgenLastRetry; // Ngen has previously failed and this is the last retry

    // V4.5
    bool        fAutoNGen;      // This is an automatically generated NGen request

    // Blue
    bool        fRepositoryOnly;// Install from repository only, no real NGen

} NGenOptions;

// Function pointer types that we use to dynamically bind to the appropriate runtime version
extern "C" typedef HRESULT STDAPICALLTYPE
    NGenCreateZapperAPI(
        HANDLE* hZapper, 
        NGenOptions *options);
typedef NGenCreateZapperAPI *PNGenCreateZapper;

extern "C" typedef HRESULT STDAPICALLTYPE
    NGenTryEnumerateFusionCacheAPI(
        HANDLE hZapper,
        LPCWSTR assemblyName,
        bool fPrint,
        bool fDelete);
typedef NGenTryEnumerateFusionCacheAPI *PNGenTryEnumerateFusionCache;

// The return type should really be HRESULT.
// However, it is BOOL for backwards-compatibility
extern "C" typedef BOOL STDAPICALLTYPE
    NGenCompileAPI(
        HANDLE hZapper,
        LPCWSTR path);
typedef NGenCompileAPI *PNGenCompile;

extern "C" typedef HRESULT STDAPICALLTYPE
    NGenFreeZapperAPI(
        HANDLE hZapper);
typedef NGenFreeZapperAPI *PNGenFreeZapper;

class ILocalServerLifetime
{
public:
    virtual void AddRefServerProcess()  = 0;
    virtual void ReleaseServerProcess() = 0;
};

struct ICorSvcLogger;

extern "C" typedef HRESULT STDAPICALLTYPE
    NGenCreateNGenWorkerAPI(
        ICorSvcWorker **pCorSvcWorker,
        ILocalServerLifetime *pLocalServerLifetime,
        ICorSvcLogger *pCorSvcLogger
        );
typedef NGenCreateNGenWorkerAPI *PNGenCreateNGenWorker;

#endif
