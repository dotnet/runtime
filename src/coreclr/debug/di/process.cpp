// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//*****************************************************************************
// File: process.cpp
//
//*****************************************************************************

#include "stdafx.h"
#include "primitives.h"
#include "safewrap.h"

#include "check.h"

#ifndef SM_REMOTESESSION
#define SM_REMOTESESSION 0x1000
#endif

#include "corpriv.h"
#include "corexcep.h"
#include "../../dlls/mscorrc/resource.h"
#include <limits.h>

#include <sstring.h>

// @dbgtodo shim: process has some private hooks into the shim.
#include "shimpriv.h"

#include "metadataexports.h"
#include "readonlydatatargetfacade.h"
#include "metahost.h"

// Keep this around for retail debugging. It's very very useful because
// it's global state that we can always find, regardless of how many locals the compiler
// optimizes away ;)
struct RSDebuggingInfo;
extern RSDebuggingInfo * g_pRSDebuggingInfo;

//---------------------------------------------------------------------------------------
//
// OpenVirtualProcessImpl method called by the shim to get an ICorDebugProcess4 instance
//
// Arguments:
//    clrInstanceId - target pointer identifying which CLR in the Target to debug.
//    pDataTarget - data target abstraction.
//    hDacModule - the handle of the appropriate DAC dll for this runtime
//    riid - interface ID to query for.
//    ppProcessOut - new object for target, interface ID matches riid.
//    ppFlagsOut - currently only has 1 bit to indicate whether or not this runtime
//                 instance will send a managed event after attach
//
// Return Value:
//    S_OK on success. Else failure
//
// Assumptions:
//
// Notes:
//    The outgoing process object can be cleaned up by calling Detach (which
//    will reset the Attach bit.)
//    @dbgtodo attach-bit: need to determine fate of attach bit.
//
//---------------------------------------------------------------------------------------
STDAPI DLLEXPORT OpenVirtualProcessImpl(
    ULONG64 clrInstanceId,
    IUnknown * pDataTarget,
    HMODULE hDacModule,
    CLR_DEBUGGING_VERSION * pMaxDebuggerSupportedVersion,
    REFIID riid,
    IUnknown ** ppInstance,
    CLR_DEBUGGING_PROCESS_FLAGS* pFlagsOut)
{
    HRESULT hr = S_OK;
    RSExtSmartPtr<CordbProcess> pProcess;
    PUBLIC_API_ENTRY(NULL);
    EX_TRY
    {

        if ( (pDataTarget == NULL) || (clrInstanceId == 0) || (pMaxDebuggerSupportedVersion == NULL) ||
            ((pFlagsOut == NULL) && (ppInstance == NULL))
            )
        {
            ThrowHR(E_INVALIDARG);
        }

        // We consider the top 8 bits of the struct version to be the only part that represents
        // a breaking change.  This gives us some freedom in the future to have the debugger
        // opt into getting more data.
        const WORD kMajorMask = 0xff00;
        const WORD kMaxStructMajor = 0;
        if ((pMaxDebuggerSupportedVersion->wStructVersion & kMajorMask) > kMaxStructMajor)
        {
            // Don't know how to interpret the version structure
            ThrowHR(CORDBG_E_UNSUPPORTED_VERSION_STRUCT);
        }

        // This process object is intended to be used for the V3 pipeline, and so
        // much of the process from V2 is not being used. For example,
        // - there is no ShimProcess object
        // - there is no w32et thread (all threads are effectively an event thread)
        // - the stop state is 'live', which corresponds to CordbProcess not knowing what
        // its stop state really is (because that is now controlled by the shim).
        ProcessDescriptor pd = ProcessDescriptor::CreateUninitialized();
        IfFailThrow(CordbProcess::OpenVirtualProcess(
            clrInstanceId,
            pDataTarget,  // takes a reference
            hDacModule,
            NULL, // Cordb
            &pd, // 0 for V3 cases (pShim == NULL).
            NULL, // no Shim in V3 cases
            &pProcess));

        // CordbProcess::OpenVirtualProcess already did the external addref to pProcess.
        // Since pProcess is a smart ptr, it will external release in this function.
        // Living reference will be the one from the QI.

        // get the managed debug event pending flag
        if(pFlagsOut != NULL)
        {
            hr = pProcess->GetAttachStateFlags(pFlagsOut);
            if(FAILED(hr))
            {
                ThrowHR(hr);
            }
        }

        //
        // Check to make sure the debugger supports debugging this version
        // Note that it's important that we still store the flags (above) in this case
        //
        if (!CordbProcess::IsCompatibleWith(pMaxDebuggerSupportedVersion->wMajor))
        {
            // Not compatible - don't keep the process instance, and return this specific error-code
            ThrowHR(CORDBG_E_UNSUPPORTED_FORWARD_COMPAT);
        }

        //
        // Now Query for the requested interface
        //
        if(ppInstance != NULL)
        {
            IfFailThrow(pProcess->QueryInterface(riid, reinterpret_cast<void**> (ppInstance)));
        }

        // if you have to add code here that could fail make sure ppInstance gets released and NULL'ed at exit
    }
    EX_CATCH_HRESULT(hr);

    if((FAILED(hr) || ppInstance == NULL) && pProcess != NULL)
    {
        // The process has a strong reference to itself which is only released by neutering it.
        // Since we aren't handing out the ref then we need to clean it up
        _ASSERTE(ppInstance == NULL || *ppInstance == NULL);
        pProcess->Neuter();
    }
    return hr;
};

//---------------------------------------------------------------------------------------
//
// OpenVirtualProcessImpl2 method called by the dbgshim to get an ICorDebugProcess4 instance
//
// Arguments:
//    clrInstanceId - target pointer identifying which CLR in the Target to debug.
//    pDataTarget - data target abstraction.
//    pDacModulePath - the module path of the appropriate DAC dll for this runtime
//    riid - interface ID to query for.
//    ppProcessOut - new object for target, interface ID matches riid.
//    ppFlagsOut - currently only has 1 bit to indicate whether or not this runtime
//                 instance will send a managed event after attach
//
// Return Value:
//    S_OK on success. Else failure
//---------------------------------------------------------------------------------------
STDAPI DLLEXPORT OpenVirtualProcessImpl2(
    ULONG64 clrInstanceId,
    IUnknown * pDataTarget,
    LPCWSTR pDacModulePath,
    CLR_DEBUGGING_VERSION * pMaxDebuggerSupportedVersion,
    REFIID riid,
    IUnknown ** ppInstance,
    CLR_DEBUGGING_PROCESS_FLAGS* pFlagsOut)
{
    HMODULE hDac = LoadLibraryW(pDacModulePath);
    if (hDac == NULL)
    {
        return HRESULT_FROM_WIN32(GetLastError());
    }
    return OpenVirtualProcessImpl(clrInstanceId, pDataTarget, hDac, pMaxDebuggerSupportedVersion, riid, ppInstance, pFlagsOut);
}

//---------------------------------------------------------------------------------------
// DEPRECATED - use OpenVirtualProcessImpl
// OpenVirtualProcess method used by the shim in CLR v4 Beta1
// We'd like a beta1 shim/VS to still be able to open dumps using a CLR v4 Beta2+ mscordbi.dll,
// so we'll leave this in place (at least until after Beta2 is in wide use).
//---------------------------------------------------------------------------------------
STDAPI DLLEXPORT OpenVirtualProcess2(
    ULONG64 clrInstanceId,
    IUnknown * pDataTarget,
    HMODULE hDacModule,
    REFIID riid,
    IUnknown ** ppInstance,
    CLR_DEBUGGING_PROCESS_FLAGS* pFlagsOut)
{
    CLR_DEBUGGING_VERSION maxVersion = {0};
    maxVersion.wMajor = 4;
    return OpenVirtualProcessImpl(clrInstanceId, pDataTarget, hDacModule, &maxVersion, riid, ppInstance, pFlagsOut);
}

//---------------------------------------------------------------------------------------
// DEPRECATED - use OpenVirtualProcessImpl
// Public OpenVirtualProcess method to get an ICorDebugProcess4 instance
// Used directly in CLR v4 pre Beta1 - can probably be safely removed now
//---------------------------------------------------------------------------------------
STDAPI DLLEXPORT OpenVirtualProcess(
    ULONG64 clrInstanceId,
    IUnknown * pDataTarget,
    REFIID riid,
    IUnknown ** ppInstance)
{
    return OpenVirtualProcess2(clrInstanceId, pDataTarget, NULL, riid, ppInstance, NULL);
};

//-----------------------------------------------------------------------------
// Most Hresults to Unrecoverable error indicate an internal error
// in the Right-Side.
// However, a few are legal (eg, "could actually happen in a retail scenario and
// not indicate an issue in mscorbi"). Track that here.
//-----------------------------------------------------------------------------

bool IsLegalFatalError(HRESULT hr)
{
    return
        (hr == CORDBG_E_INCOMPATIBLE_PROTOCOL) ||
        (hr == CORDBG_E_CANNOT_DEBUG_FIBER_PROCESS) ||
        (hr == CORDBG_E_INCOMPATIBLE_PLATFORMS) ||
        (hr == CORDBG_E_MISMATCHED_CORWKS_AND_DACWKS_DLLS) ||
        // This should only happen in the case of a security attack on us.
        (hr == E_ACCESSDENIED) ||
        (hr == E_FAIL);
}

//-----------------------------------------------------------------------------
// Safe wait. Use this anytime we're waiting on:
// - an event signaled by the helper thread.
// - something signaled by a thread that holds the process lock.
// Note that we must preserve GetLastError() semantics.
//-----------------------------------------------------------------------------
inline DWORD SafeWaitForSingleObject(CordbProcess * p, HANDLE h, DWORD dwTimeout)
{
    // Can't hold process lock while blocking
    _ASSERTE(!p->ThreadHoldsProcessLock());

    return ::WaitForSingleObject(h, dwTimeout);
}

#define CORDB_WAIT_TIMEOUT 360000 // milliseconds

//---------------------------------------------------------------------------------------
//
// Get the timeout value used in waits.
//
// Return Value:
//    Number of milliseconds to waite or possible INFINITE (-1).
//
//
// Notes:
//    Uses registry values for fine tuning.
//

// static
static inline DWORD CordbGetWaitTimeout()
{
#ifdef _DEBUG
    // 0 = Wait forever
    // 1 = Wait for CORDB_WAIT_TIMEOUT
    // n = Wait for n milliseconds
    static ConfigDWORD cordbWaitTimeout;
    DWORD dwTimeoutVal = cordbWaitTimeout.val(CLRConfig::INTERNAL_DbgWaitTimeout);
    if (dwTimeoutVal == 0)
        return DWORD(-1);
    else if (dwTimeoutVal != 1)
        return dwTimeoutVal;
    else
#endif
    {
        return CORDB_WAIT_TIMEOUT;
    }
}

//----------------------------------------------------------------------------
// Implementation of IDacDbiInterface::IMetaDataLookup.
// lookup Internal Metadata Importer keyed by PEAssembly
// isILMetaDataForNGENImage is true iff the IMDInternalImport returned represents a pointer to
// metadata from an IL image when the module was an ngen'ed image.
IMDInternalImport * CordbProcess::LookupMetaData(VMPTR_PEAssembly vmPEAssembly, bool &isILMetaDataForNGENImage)
{
    INTERNAL_DAC_CALLBACK(this);

    HASHFIND hashFindAppDomain;
    HASHFIND hashFindModule;
    IMDInternalImport * pMDII = NULL;
    isILMetaDataForNGENImage = false;

    // Check to see if one of the cached modules has the metadata we need
    // If not we will do a more exhaustive search below
    for (CordbAppDomain * pAppDomain = m_appDomains.FindFirst(&hashFindAppDomain);
         pAppDomain != NULL;
         pAppDomain = m_appDomains.FindNext(&hashFindAppDomain))
    {
        for (CordbModule * pModule = pAppDomain->m_modules.FindFirst(&hashFindModule);
             pModule != NULL;
             pModule = pAppDomain->m_modules.FindNext(&hashFindModule))
        {
            if (pModule->GetPEFile() == vmPEAssembly)
            {
                pMDII = NULL;
                ALLOW_DATATARGET_MISSING_MEMORY(
                    pMDII = pModule->GetInternalMD();
                );
                if(pMDII != NULL)
                    return pMDII;
            }
        }
    }

    // Cache didn't have it... time to search harder
    PrepopulateAppDomainsOrThrow();

    // There may be perf issues here. The DAC may make a lot of metadata requests, and so
    // this may be an area for potential perf optimizations if we find things running slow.

    // enumerate through all Modules
    for (CordbAppDomain * pAppDomain = m_appDomains.FindFirst(&hashFindAppDomain);
         pAppDomain != NULL;
         pAppDomain = m_appDomains.FindNext(&hashFindAppDomain))
    {
        pAppDomain->PrepopulateModules();

        for (CordbModule * pModule = pAppDomain->m_modules.FindFirst(&hashFindModule);
             pModule != NULL;
             pModule = pAppDomain->m_modules.FindNext(&hashFindModule))
        {
            if (pModule->GetPEFile() == vmPEAssembly)
            {
                pMDII = NULL;
                ALLOW_DATATARGET_MISSING_MEMORY(
                    pMDII = pModule->GetInternalMD();
                );

                if ( pMDII == NULL)
                {
                    // If we couldn't get metadata from the CordbModule, then we need to ask the
                    // debugger if it can find the metadata elsewhere.
                    // If this was live debugging, we should have just gotten the memory contents.
                    // Thus this code is for dump debugging, when you don't have the metadata in the dump.
                    pMDII = LookupMetaDataFromDebugger(vmPEAssembly, isILMetaDataForNGENImage, pModule);
                }
                return pMDII;
            }
        }
    }

    return NULL;
}


IMDInternalImport * CordbProcess::LookupMetaDataFromDebugger(
    VMPTR_PEAssembly vmPEAssembly,
    bool &isILMetaDataForNGENImage,
    CordbModule * pModule)
{
    DWORD dwImageTimeStamp = 0;
    DWORD dwImageSize = 0;
    bool isNGEN = false;
    StringCopyHolder filePath;
    IMDInternalImport * pMDII = NULL;

    // First, see if the debugger can locate the exact metadata we want.
    if (this->GetDAC()->GetMetaDataFileInfoFromPEFile(vmPEAssembly, dwImageTimeStamp, dwImageSize, isNGEN, &filePath))
    {
        _ASSERTE(filePath.IsSet());

        // Since we track modules by their IL images, that presents a little bit of oddness here.  The correct
        // thing to do is preferentially load the NI content.
        // We don't discriminate between timestamps & sizes becuase CLRv4 deterministic NGEN guarantees that the
        // IL image and NGEN image have the same timestamp and size.  Should that guarantee change, this code
        // will be horribly broken.

        // If we happen to have an NI file path, use it instead.
        const WCHAR * pwszFilePath = pModule->GetNGenImagePath();
        if (pwszFilePath)
        {
            // Force the issue, regardless of the older codepath's opinion.
            isNGEN = true;
        }
        else
        {
            pwszFilePath = (WCHAR *)filePath;
        }

        ALLOW_DATATARGET_MISSING_MEMORY(
            pMDII = LookupMetaDataFromDebuggerForSingleFile(pModule, pwszFilePath, dwImageTimeStamp, dwImageSize);
        );

        // If it's an ngen'ed image and the debugger couldn't find it, we can use the metadata from
        // the corresponding IL image if the debugger can locate it.
        filePath.Clear();
        if ((pMDII == NULL) &&
            (isNGEN) &&
            (this->GetDAC()->GetILImageInfoFromNgenPEFile(vmPEAssembly, dwImageTimeStamp, dwImageSize, &filePath)))
        {
            _ASSERTE(filePath.IsSet());

            WCHAR *mutableFilePath = (WCHAR *)filePath;

            size_t pathLen = wcslen(mutableFilePath);

            const WCHAR *nidll = W(".ni.dll");
            const WCHAR *niexe = W(".ni.exe");
            const size_t dllLen = wcslen(nidll);  // used for ni.exe as well

            if (pathLen > dllLen && _wcsicmp(mutableFilePath+pathLen-dllLen, nidll) == 0)
            {
                wcscpy_s(mutableFilePath+pathLen-dllLen, dllLen, W(".dll"));
            }
            else if (pathLen > dllLen && _wcsicmp(mutableFilePath+pathLen-dllLen, niexe) == 0)
            {
                wcscpy_s(mutableFilePath+pathLen-dllLen, dllLen, W(".exe"));
            }

            ALLOW_DATATARGET_MISSING_MEMORY(
                pMDII = LookupMetaDataFromDebuggerForSingleFile(pModule, mutableFilePath, dwImageTimeStamp, dwImageSize);
            );

            if (pMDII != NULL)
            {
                isILMetaDataForNGENImage = true;
            }
        }
    }
    return pMDII;
}

// We do not know if the image being sent to us is an IL image or ngen image.
// CordbProcess::LookupMetaDataFromDebugger() has this knowledge when it looks up the file to hand off
// to this function.
// DacDbiInterfaceImpl::GetMDImport() has this knowledge in the isNGEN flag.
// The CLR v2 code that windbg used made a distinction whether the metadata came from
// the exact binary or not (i.e. were we getting metadata from the IL image and using
// it against the ngen image?) but that information was never used and so not brought forward.
// It would probably be more interesting generally to track whether the debugger gives us back
// a file that bears some relationship to the file we asked for, which would catch the NI/IL case
// as well.
IMDInternalImport * CordbProcess::LookupMetaDataFromDebuggerForSingleFile(
    CordbModule * pModule,
    LPCWSTR pwszFilePath,
    DWORD dwTimeStamp,
    DWORD dwSize)
{
    INTERNAL_DAC_CALLBACK(this);

    ULONG32 cchLocalImagePath = MAX_LONGPATH;
    ULONG32 cchLocalImagePathRequired;
    NewArrayHolder<WCHAR> pwszLocalFilePath = NULL;
    IMDInternalImport * pMDII = NULL;

    const HRESULT E_NSF_BUFFER = HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER);
    HRESULT hr = E_NSF_BUFFER;
    for(unsigned i=0; i<2 && hr == E_NSF_BUFFER; i++)
    {
        if (pwszLocalFilePath != NULL)
            pwszLocalFilePath.Release();

        if (NULL == (pwszLocalFilePath = new (nothrow) WCHAR[cchLocalImagePath+1]))
            ThrowHR(E_OUTOFMEMORY);

        cchLocalImagePathRequired = 0;

        hr = m_pMetaDataLocator->GetMetaData(pwszFilePath,
                                             dwTimeStamp,
                                             dwSize,
                                             cchLocalImagePath,
                                             &cchLocalImagePathRequired,
                                             pwszLocalFilePath);

        pwszLocalFilePath[cchLocalImagePath] = W('\0');
        cchLocalImagePath = cchLocalImagePathRequired;
    }

    if (SUCCEEDED(hr))
    {
        hr = pModule->InitPublicMetaDataFromFile(pwszLocalFilePath, ofReadOnly, false);
        if (SUCCEEDED(hr))
        {
            // While we're successfully returning a metadata reader, remember that there's
            // absolutely no guarantee this metadata is an exact match for the vmPEAssembly.
            // The debugger could literally send us back a path to any managed file with
            // metadata content that is readable and we'll 'succeed'.
            // For now, this is by-design.  A debugger should be allowed to decide if it wants
            // to take a risk by returning 'mostly matching' metadata to see if debugging is
            // possible in the absence of a true match.
            pMDII = pModule->GetInternalMD();
        }
    }

    return pMDII;
}


//---------------------------------------------------------------------------------------
//
// Implement IDacDbiInterface::IAllocator::Alloc
// Expected to throws on error.
//
// Arguments:
//    lenBytes - size of the byte array to allocate
//
// Return Value:
//    Return the newly allocated byte array, or throw on OOM
//
// Notes:
//    Since this function is a callback from DAC, it must not take the process lock.
//    If it does, we may deadlock between the DD lock and the process lock.
//    If we really need to take the process lock for whatever reason, we must take it in the DBI functions
//    which call the DAC API that ends up calling this function.
//    See code:InternalDacCallbackHolder for more information.
//

void * CordbProcess::Alloc(SIZE_T lenBytes)
{
    return new BYTE[lenBytes]; // throws
}

//---------------------------------------------------------------------------------------
//
// Implements IDacDbiInterface::IAllocator::Free
//
// Arguments:
//    p - pointer to the memory to be released
//
// Notes:
//    Since this function is a callback from DAC, it must not take the process lock.
//    If it does, we may deadlock between the DD lock and the process lock.
//    If we really need to take the process lock for whatever reason, we must take it in the DBI functions
//    which call the DAC API that ends up calling this function.
//    See code:InternalDacCallbackHolder for more information.
//

void CordbProcess::Free(void * p)
{
    // This shouldn't throw.
    delete [] ((BYTE *) p);
}


//---------------------------------------------------------------------------------------
//
// #DBIVersionChecking
//
// There are a few checks we need to do to make sure we are using the matching DBI and DAC for a particular
// version of the runtime.
//
// 1. Runtime vs. DBI
//     - Desktop
//         This is done by making sure that the CorDebugInterfaceVersion passed to code:CreateCordbObject is
//         compatible with the version of the DBI.
//
//     - Windows CoreCLR
//         This is done by dbgshim.dll.  It checks whether the runtime DLL and the DBI DLL have the same
//         product version.  See CreateDebuggingInterfaceForVersion() in dbgshim.cpp.
//
//     - Remote transport (Mac CoreCLR + CoreSystem CoreCLR)
//         Since there is no dbgshim.dll for a remote CoreCLR, we have to do this check in some other place.
//         We do this in code:CordbProcess::CreateDacDbiInterface, by calling
//         code:DacDbiInterfaceImpl::CheckDbiVersion right after we have created the DDMarshal.
//         The IDacDbiInterface implementation on remote device checks the product version of the device
//         coreclr by:
//             mac - looking at the Info.plist file in the CoreCLR bundle.
//             CoreSystem - this check is skipped at the moment, but should be implemented if we release it
//
//         The one twist here is that the DBI needs to communicate with the IDacDbiInterface
//         implementation on the device BEFORE it can verify the product versions.  This means that we need to
//         have one IDacDbiInterface API which is consistent across all versions of the IDacDbiInterface.
//         This puts two constraints on CheckDbiVersion():
//
//             1.  It has to be the first API on the IDacDbiInterface.
//             - Otherwise, a wrong version of the DBI may end up calling a different API on the
//               IDacDbiInterface and getting random results. (Really what matters is that it is
//               protocol message id 0, at present the source code position implies the message id)
//
//             2.  Its parameters cannot change.
//             - Otherwise, we may run into random errors when we marshal/unmarshal the arguments for the
//               call to CheckDbiVersion().  Debugging will still fail, but we won't get the
//               version mismatch error. (Again, the protocol is what ultimately matters)
//             - To mitigate the impact of this constraint, we use the code:DbiVersion structure.
//               In addition to the DBI version, it also contains a format number (in case we decide to
//               check something else in the future), a breaking change number so that we can force
//               breaking changes between a DBI and a DAC, and space reserved for future use.
//
// 2. DBI vs. DAC
//     - Desktop and Windows CoreCLR (old architecture)
//          No verification is done. There is a transitive implication that if DBI matches runtime and DAC matches
//          runtime then DBI matches DAC. Technically because the DBI only matches runtime on major version number
//          runtime and DAC could be from different builds. However because we service all three binaries together
//          and DBI always loads the DAC that is sitting in the same directory DAC and DBI generally get tight
//          version coupling. A user with admin privileges could put different builds together and no version check
//          would ever fail though.
//
//      - Desktop and Windows CoreCLR (new architecture)
//          No verification is done. Similar to above its implied that if DBI matches runtime and runtime matches
//          DAC then DBI matches DAC. The only difference is that here both the DBI and DAC are provided by the
//          debugger. We provide timestamp and filesize for both binaries which are relatively strongly bound hints,
//          but there is no enforcement on the returned binaries beyond the runtime compat checking.
//
//      - Remote transport (Mac CoreCLR and CoreSystem CoreCLR)
//          Because the transport exists between DBI and DAC it becomes much more important to do a versioning check
//
//          Mac - currently does a tightly bound version check between DBI and the runtime (CheckDbiVersion() above),
//             which transitively gives a tightly bound check to DAC. In same function there is also a check that is
//             logically a DAC DBI protocol check, verifying that the m_dwProtocolBreakingChangeCounter of DbiVersion
//             matches. However this check should be weaker than the build version check and doesn't add anything here.
//
//          CoreSystem - currently skips the tightly bound version check to make internal deployment and usage easier.
//             We want to use old desktop side debugger components to target newer CoreCLR builds, only forcing a desktop
//             upgrade when the protocol actually does change. To do this we use two checks:
//             1. The breaking change counter in CheckDbiVersion() whenever a dev knows they are breaking back
//                compat and wants to be explicit about it. This is the same as mac above.
//             2. During the auto-generation of the DDMarshal classes we take an MD5 hash of IDacDbiInterface source
//                code and embed it in two DDMarshal functions, one which runs locally and one that runs remotely.
//                If both DBI and DAC were built from the same source then the local and remote hashes will match. If the
//                hashes don't match then we assume there has been a been a breaking change in the protocol. Note
//                this hash could have both false-positives and false-negatives. False positives could occur when
//                IDacDbiInterface is changed in a trivial way, such as changing a comment. False negatives could
//                occur when the semantics of the protocol are changed even though the interface is not. Another
//                case would be changing the DDMarshal proxy generation code. In addition to the hashes we also
//                embed timestamps when the auto-generated code was produced. However this isn't used for version
//                matching, only as a hint to indicate which of two mismatched versions is newer.
//
//
// 3. Runtime vs. DAC
//     - Desktop, Windows CoreCLR, CoreSystem CoreCLR
//         In both cases we check this by matching the timestamp in the debug directory of the runtime image
//         and the timestamp we store in the DAC table when we generate the DAC dll.  This is done in
//         code:ClrDataAccess::VerifyDlls.
//
//     - Mac CoreCLR
//         On Mac, we don't have a timestamp in the runtime image.  Instead, we rely on checking the 16-byte
//         UUID in the image.  This UUID is used to check whether a symbol file matches the image, so
//         conceptually it's the same as the timestamp we use on Windows.  This is also done in
//         code:ClrDataAccess::VerifyDlls.
//
//---------------------------------------------------------------------------------------
//
// Instantiates a DacDbi Interface object in a live-debugging scenario that matches
// the current instance of mscorwks in this process.
//
// Return Value:
//    Returns on success. Else throws.
//
// Assumptions:
//    Client will code:CordbProcess::FreeDac when its done with the DacDbi interface.
//    Caller has initialized clrInstanceId.
//
// Notes:
//    This looks for the DAC next to this current DBI. This assumes that Dac and Dbi are both on
//    the local file system. That assumption will break in zero-copy deployment scenarios.
//
//---------------------------------------------------------------------------------------
void
CordbProcess::CreateDacDbiInterface()
{
    _ASSERTE(m_pDACDataTarget != NULL);
    _ASSERTE(m_pDacPrimitives == NULL); // don't double-init

    // Caller has already determined which CLR in the target is being debugged.
    _ASSERTE(m_clrInstanceId != 0);

    m_pDacPrimitives = NULL;

    HRESULT hrStatus = S_OK;

    // Non-marshalling path for live local dac.
    // in the new arch we can get the module from OpenVirtualProcess2 but in the shim case
    // and the deprecated OpenVirtualProcess case we must assume it comes from DAC in the
    // same directory as DBI
    if (m_hDacModule == NULL)
    {
        m_hDacModule.Assign(ShimProcess::GetDacModule(m_cordb->GetDacModulePath()));
    }

    //
    // Get the access interface, passing our callback interfaces (data target, allocator and metadata lookup)
    //

    IDacDbiInterface::IAllocator * pAllocator = this;
    IDacDbiInterface::IMetaDataLookup * pMetaDataLookup = this;


    typedef HRESULT (STDAPICALLTYPE * PFN_DacDbiInterfaceInstance)(
        ICorDebugDataTarget *,
        CORDB_ADDRESS,
        IDacDbiInterface::IAllocator *,
        IDacDbiInterface::IMetaDataLookup *,
        IDacDbiInterface **);

    IDacDbiInterface* pInterfacePtr = NULL;
    PFN_DacDbiInterfaceInstance pfnEntry = (PFN_DacDbiInterfaceInstance)GetProcAddress(m_hDacModule, "DacDbiInterfaceInstance");
    if (!pfnEntry)
    {
        ThrowLastError();
    }

    hrStatus = pfnEntry(m_pDACDataTarget, m_clrInstanceId, pAllocator, pMetaDataLookup, &pInterfacePtr);
    IfFailThrow(hrStatus);

    // We now have a resource, pInterfacePtr, that needs to be freed.
    m_pDacPrimitives = pInterfacePtr;

    // Setup DAC target consistency checking based on what we're using for DBI
    m_pDacPrimitives->DacSetTargetConsistencyChecks( m_fAssertOnTargetInconsistency );
}

//---------------------------------------------------------------------------------------
//
// Is the DAC/DBI interface initialized?
//
// Return Value:
//    TRUE iff init.
//
// Notes:
//    The RS will try to initialize DD as soon as it detects the runtime as loaded.
//    If the DD interface has not initialized, then it very likely the runtime has not
//    been loaded into the target.
//
BOOL CordbProcess::IsDacInitialized()
{
    return m_pDacPrimitives != NULL;
}

//---------------------------------------------------------------------------------------
//
// Get the DAC interface.
//
// Return Value:
//    the Dac/Dbi interface pointer to the process.
//    Never returns NULL.
//
// Assumptions:
//    Caller is responsible for ensuring Data-Target is safe to access (eg, not
//    currently running).
//    Caller is responsible for ensuring DAC-cache is flushed. Call code:CordbProcess::ForceDacFlush
//    as needed.
//
//---------------------------------------------------------------------------------------
IDacDbiInterface * CordbProcess::GetDAC()
{
    // Since the DD primitives may throw, easiest way to model that is to make this throw.
    CONTRACTL
    {
        THROWS;
    }
    CONTRACTL_END;

    // We should always have the DAC/DBI interface.
    _ASSERTE(m_pDacPrimitives != NULL);
    return m_pDacPrimitives;
}

//---------------------------------------------------------------------------------------
// Get the Data-Target
//
// Returns:
//     pointer to the data-target. Should be non-null.
//     Lifetime of the pointer is until this process object is neutered.
//
ICorDebugDataTarget * CordbProcess::GetDataTarget()
{
    return m_pDACDataTarget;
}

//---------------------------------------------------------------------------------------
// Create a CordbProcess object around an existing OS process.
//
// Arguments:
//     pDataTarget - abstracts access to the debuggee.
//     clrInstanceId - identifies the CLR instance within the debuggee. (This is the
//         base address of mscorwks)
//     pCordb - Pointer to the implementation of the owning Cordb object implementing the
//         owning ICD interface.
//         This should go away - we can get the functionality from the pShim.
//         If this is null, then pShim must be null too.
//     processID - OS process ID of target process. 0 if pShim == NULL.
//     pShim - shim counter part object. This allows hooks back for v2 compat. This will
//         go away once we no longer support V2 backwards compat.
//         This must be non-null for any V2 paths (including non-DAC-ized code).
//         If this is null, then we're in a V3 path.
//     ppProcess - out parameter for new process object. This gets addreffed.
//
// Return Value:
//     S_OK on success, and *ppProcess set to newly created debuggee object. Else error.
//
// Notes:
//    @dbgtodo - , shim: Cordb, and pShim will all eventually go away.
//
//---------------------------------------------------------------------------------------

// static
HRESULT CordbProcess::OpenVirtualProcess(
    ULONG64 clrInstanceId,
    IUnknown * pDataTarget,
    HMODULE hDacModule,
    Cordb* pCordb,
    const ProcessDescriptor * pProcessDescriptor,
    ShimProcess * pShim,
    CordbProcess ** ppProcess)
{
    _ASSERTE(pDataTarget != NULL);

    // In DEBUG builds, verify that we do actually have an ICorDebugDataTarget (i.e. that
    // someone hasn't messed up the COM interop marshalling, etc.).
#ifdef _DEBUG
    {
        IUnknown * pTempDt;
        HRESULT hrQi = pDataTarget->QueryInterface(IID_ICorDebugDataTarget, (void**)&pTempDt);
        _ASSERTE_MSG(SUCCEEDED(hrQi), "OpenVirtualProcess was passed something that isn't actually an ICorDebugDataTarget");
        pTempDt->Release();
    }
#endif

    // If we're emulating V2, then both pCordb and pShim are non-NULL.
    // If we're doing a real V3 path, then they're both NULL.
    // Either way, they should have the same null-status.
    _ASSERTE((pCordb == NULL) == (pShim == NULL));

    // If we're doing real V3, then we must have a real instance ID
    _ASSERTE(!((pShim == NULL) && (clrInstanceId == 0)));

    *ppProcess = NULL;

    HRESULT hr = S_OK;
    RSUnsafeExternalSmartPtr<CordbProcess> pProcess;
    pProcess.Assign(new (nothrow) CordbProcess(clrInstanceId, pDataTarget, hDacModule, pCordb, pProcessDescriptor, pShim));

    if (pProcess == NULL)
    {
        return E_OUTOFMEMORY;
    }

    ICorDebugProcess * pThis = pProcess;
    (void)pThis; //prevent "unused variable" error from GCC

    // CordbProcess::Init may need shim hooks, so connect Shim now.
    // This will bump reference count.
    if (pShim != NULL)
    {
        pShim->SetProcess(pProcess);

        _ASSERTE(pShim->GetProcess() == pThis);
        _ASSERTE(pShim->GetWin32EventThread() != NULL);
    }

    hr = pProcess->Init();

    if (SUCCEEDED(hr))
    {
        *ppProcess = pProcess;
        pProcess->ExternalAddRef();
    }
    else
    {
        // handle failure path
        pProcess->CleanupHalfBakedLeftSide();

        if (pShim != NULL)
        {
            // Shim still needs to be disposed to clean up other resources.
            pShim->SetProcess(NULL);
        }

        // In failure case, pProcess's dtor will do the final release.
    }


    return hr;
}

//---------------------------------------------------------------------------------------
// CordbProcess constructor
//
// Arguments:
//     pDataTarget - Pointer to an implementation of ICorDebugDataTarget
//         (or ICorDebugMutableDataTarget), which virtualizes access to the process.
//     clrInstanceId - representation of the CLR to debug in the process.  Must be specified
//         (non-zero) if pShim is NULL.  If 0, use the first CLR that we see.
//     pCordb - Pointer to the implementation of the owning Cordb object implementing the
//         owning ICD interface.
//     pW32 - Pointer to the Win32 event thread to use when processing events for this
//         process.
//     dwProcessID - For V3, 0.
//         Else for shim codepaths, the processID of the process this object will represent.
//     pShim - Pointer to the shim for handling V2 debuggers on the V3 architecture.
//
//---------------------------------------------------------------------------------------

CordbProcess::CordbProcess(ULONG64 clrInstanceId,
                           IUnknown * pDataTarget,
                           HMODULE hDacModule,
                           Cordb * pCordb,
                           const ProcessDescriptor * pProcessDescriptor,
                           ShimProcess * pShim)
  : CordbBase(NULL, pProcessDescriptor->m_Pid, enumCordbProcess),
    m_fDoDelayedManagedAttached(false),
    m_cordb(pCordb),
    m_handle(NULL),
    m_processDescriptor(*pProcessDescriptor),
    m_detached(false),
    m_uninitializedStop(false),
    m_exiting(false),
    m_terminated(false),
    m_unrecoverableError(false),
    m_specialDeferment(false),
    m_helperThreadDead(false),
    m_loaderBPReceived(false),
    m_cOutstandingEvals(0),
    m_cOutstandingHandles(0),
    m_clrInstanceId(clrInstanceId),
    m_stopCount(0),
    m_synchronized(false),
    m_syncCompleteReceived(false),
    m_pShim(pShim),
    m_userThreads(11),
    m_oddSync(false),
#ifdef FEATURE_INTEROP_DEBUGGING
    m_unmanagedThreads(11),
#endif
    m_appDomains(11),
    m_sharedAppDomain(0),
    m_steppers(11),
    m_continueCounter(1),
    m_flushCounter(0),
    m_leftSideEventAvailable(NULL),
    m_leftSideEventRead(NULL),
#if defined(FEATURE_INTEROP_DEBUGGING)
    m_leftSideUnmanagedWaitEvent(NULL),
#endif // FEATURE_INTEROP_DEBUGGING
    m_initialized(false),
    m_stopRequested(false),
    m_stopWaitEvent(NULL),
#ifdef FEATURE_INTEROP_DEBUGGING
    m_cFirstChanceHijackedThreads(0),
    m_unmanagedEventQueue(NULL),
    m_lastQueuedUnmanagedEvent(NULL),
    m_lastQueuedOOBEvent(NULL),
    m_outOfBandEventQueue(NULL),
    m_lastDispatchedIBEvent(NULL),
    m_dispatchingUnmanagedEvent(false),
    m_dispatchingOOBEvent(false),
    m_doRealContinueAfterOOBBlock(false),
    m_state(0),
#endif // FEATURE_INTEROP_DEBUGGING
    m_helperThreadId(0),
    m_pPatchTable(NULL),
    m_cPatch(0),
    m_rgData(NULL),
    m_rgNextPatch(NULL),
    m_rgUncommittedOpcode(NULL),
    m_minPatchAddr(MAX_ADDRESS),
    m_maxPatchAddr(MIN_ADDRESS),
    m_iFirstPatch(0),
    m_hHelperThread(NULL),
    m_dispatchedEvent(DB_IPCE_DEBUGGER_INVALID),
    m_pDefaultAppDomain(NULL),
    m_hDacModule(hDacModule),
    m_pDacPrimitives(NULL),
    m_pEventChannel(NULL),
    m_fAssertOnTargetInconsistency(false),
    m_runtimeOffsetsInitialized(false),
    m_writableMetadataUpdateMode(LegacyCompatPolicy)
{
    _ASSERTE((m_id == 0) == (pShim == NULL));

    HRESULT hr = pDataTarget->QueryInterface(IID_ICorDebugDataTarget, reinterpret_cast<void **>(&m_pDACDataTarget));
    IfFailThrow(hr);

#ifdef FEATURE_INTEROP_DEBUGGING
    m_DbgSupport.m_DebugEventQueueIdx = 0;
    m_DbgSupport.m_TotalNativeEvents = 0;
    m_DbgSupport.m_TotalIB = 0;
    m_DbgSupport.m_TotalOOB = 0;
    m_DbgSupport.m_TotalCLR = 0;
#endif // FEATURE_INTEROP_DEBUGGING

    g_pRSDebuggingInfo->m_MRUprocess = this;

    // This is a strong reference to ourselves.
    // This is cleared in code:CordbProcess::Neuter
    m_pProcess.Assign(this);

#ifdef _DEBUG
    // On Debug builds, we'll ASSERT by default whenever the target appears to be corrupt or
    // otherwise inconsistent (both in DAC and DBI).  But we also need the ability to
    // explicitly test corrupt targets.
    // Tests should set COMPlus_DbgIgnoreInconsistentTarget=1 to suppress these asserts
    // Note that this controls two things:
    //     1) DAC behavior - see code:IDacDbiInterface::DacSetTargetConsistencyChecks
    //     2) RS-only consistency asserts - see code:CordbProcess::TargetConsistencyCheck
    if( !CLRConfig::GetConfigValue(CLRConfig::INTERNAL_DbgDisableTargetConsistencyAsserts) )
    {
        m_fAssertOnTargetInconsistency = true;
    }
#endif
}

/*
    A list of which resources owned by this object are accounted for.

    UNKNOWN
        Cordb*                      m_cordb;
        CordbHashTable              m_unmanagedThreads; // Released in CordbProcess but not removed from hash
        DebuggerIPCEvent*           m_lastQueuedEvent;

        // CordbUnmannagedEvent is a struct which is not derrived from CordbBase.
        // It contains a CordbUnmannagedThread which may need to be released.
        CordbUnmanagedEvent         *m_unmanagedEventQueue;
        CordbUnmanagedEvent         *m_lastQueuedUnmanagedEvent;
        CordbUnmanagedEvent         *m_outOfBandEventQueue;
        CordbUnmanagedEvent         *m_lastQueuedOOBEvent;

        BYTE*                       m_pPatchTable;
        BYTE                        *m_rgData;
        void                        *m_pbRemoteBuf;

   RESOLVED
        // Nutered
        CordbHashTable        m_userThreads;
        CordbHashTable        m_appDomains;

        // Cleaned up in ExitProcess
        DebuggerIPCEvent*     m_queuedEventList;

        CordbHashTable        m_steppers; // Closed in ~CordbProcess

        // Closed in CloseIPCEventHandles called from ~CordbProcess
        HANDLE                m_leftSideEventAvailable;
        HANDLE                m_leftSideEventRead;

        // Closed in ~CordbProcess
        HANDLE                m_handle;
        HANDLE                m_leftSideUnmanagedWaitEvent;
        HANDLE                m_stopWaitEvent;

        // Deleted in ~CordbProcess
        CRITICAL_SECTION      m_processMutex;

*/


CordbProcess::~CordbProcess()
{
    LOG((LF_CORDB, LL_INFO1000, "CP::~CP: deleting process 0x%08x\n", this));

    DTOR_ENTRY(this);

    _ASSERTE(IsNeutered());

    _ASSERTE(m_cordb == NULL);

    // We shouldn't still be in Cordb's list of processes. Unfortunately, our root Cordb object
    // may have already been deleted b/c we're at the mercy of ref-counting, so we can't check.

	_ASSERTE(m_sharedAppDomain == NULL);

    m_processMutex.Destroy();
    m_StopGoLock.Destroy();

    // These handles were cleared in neuter
    _ASSERTE(m_handle == NULL);
#if defined(FEATURE_INTEROP_DEBUGGING)
    _ASSERTE(m_leftSideUnmanagedWaitEvent == NULL);
#endif // FEATURE_INTEROP_DEBUGGING
    _ASSERTE(m_stopWaitEvent == NULL);

    // Set this to mark that we really did cleanup.
}

//-----------------------------------------------------------------------------
// Static build helper.
// This will create a process under the pCordb root, and add it to the list.
// We don't return the process - caller gets the pid and looks it up under
// the Cordb object.
//
// Arguments:
//     pCordb - Pointer to the implementation of the owning Cordb object implementing the
//         owning ICD interface.
//     szProgramName - Name of the program to execute.
//     szProgramArgs - Command line arguments for the process.
//     lpProcessAttributes - OS-specific attributes for process creation.
//     lpThreadAttributes - OS-specific attributes for thread creation.
//     fInheritFlags - OS-specific flag for child process inheritance.
//     dwCreationFlags - OS-specific creation flags.
//     lpEnvironment - OS-specific environmental strings.
//     szCurrentDirectory - OS-specific string for directory to run in.
//     lpStartupInfo - OS-specific info on startup.
//     lpProcessInformation - OS-specific process information buffer.
//     corDebugFlags - What type of process to create, currently always managed.
//-----------------------------------------------------------------------------
HRESULT ShimProcess::CreateProcess(
      Cordb * pCordb,
      ICorDebugRemoteTarget * pRemoteTarget,
      LPCWSTR szProgramName,
      _In_z_ LPWSTR  szProgramArgs,
      LPSECURITY_ATTRIBUTES lpProcessAttributes,
      LPSECURITY_ATTRIBUTES lpThreadAttributes,
      BOOL fInheritHandles,
      DWORD dwCreationFlags,
      PVOID lpEnvironment,
      LPCWSTR szCurrentDirectory,
      LPSTARTUPINFOW lpStartupInfo,
      LPPROCESS_INFORMATION lpProcessInformation,
      CorDebugCreateProcessFlags corDebugFlags
)
{
    _ASSERTE(pCordb != NULL);

#if defined(FEATURE_DBGIPC_TRANSPORT_DI)
    // The transport cannot deal with creating a suspended process (it needs the debugger to start up and
    // listen for connections).
    _ASSERTE((dwCreationFlags & CREATE_SUSPENDED) == 0);
#endif // FEATURE_DBGIPC_TRANSPORT_DI

    HRESULT hr = S_OK;

    RSExtSmartPtr<ShimProcess> pShim;
    EX_TRY
    {
        pShim.Assign(new ShimProcess());

        // Indicate that this process was started under the debugger as opposed to attaching later.
        pShim->m_attached = false;

        hr = pShim->CreateAndStartWin32ET(pCordb);
        IfFailThrow(hr);

        // Call out to newly created Win32-event Thread to create the process.
        // If this succeeds, new CordbProcess will add a ref to the ShimProcess
        hr = pShim->GetWin32EventThread()->SendCreateProcessEvent(pShim->GetMachineInfo(),
                                                                  szProgramName,
                                                                  szProgramArgs,
                                                                  lpProcessAttributes,
                                                                  lpThreadAttributes,
                                                                  fInheritHandles,
                                                                  dwCreationFlags,
                                                                  lpEnvironment,
                                                                  szCurrentDirectory,
                                                                  lpStartupInfo,
                                                                  lpProcessInformation,
                                                                  corDebugFlags);
        IfFailThrow(hr);
    }
    EX_CATCH_HRESULT(hr);

    // If this succeeds, then process takes ownership of thread. Else we need to kill it.
    if (FAILED(hr))
    {
        if (pShim != NULL)
        {
            pShim->Dispose();
        }
    }
    // Always release our ref to ShimProcess. If the Process was created, then it takes a reference.

    return hr;
}

//-----------------------------------------------------------------------------
// Static build helper for the attach case.
// On success, this will add the process to the pCordb list, and then
// callers can look it up there by pid.
//
// Arguments:
//     pCordb - root under which this all lives
//     dwProcessID - OS process ID to attach to
//     fWin32Attach - are we interop debugging?
//-----------------------------------------------------------------------------
HRESULT ShimProcess::DebugActiveProcess(
    Cordb * pCordb,
    ICorDebugRemoteTarget * pRemoteTarget,
    const ProcessDescriptor * pProcessDescriptor,
    BOOL fWin32Attach
)
{
    _ASSERTE(pCordb != NULL);

    HRESULT hr = S_OK;

    RSExtSmartPtr<ShimProcess> pShim;

    EX_TRY
    {
        pShim.Assign(new ShimProcess());

        // Indicate that this process was attached to, asopposed to being started under the debugger.
        pShim->m_attached = true;

        hr = pShim->CreateAndStartWin32ET(pCordb);
        IfFailThrow(hr);

        // If this succeeds, new CordbProcess will add a ref to the ShimProcess
        hr = pShim->GetWin32EventThread()->SendDebugActiveProcessEvent(pShim->GetMachineInfo(),
                                                                       pProcessDescriptor,
                                                                       fWin32Attach == TRUE,
                                                                       NULL);
        IfFailThrow(hr);

        _ASSERTE(SUCCEEDED(hr));

#if !defined(FEATURE_DBGIPC_TRANSPORT_DI)
        // Don't do this when we are remote debugging since we won't be getting the loader breakpoint.
        // We don't support JIT attach in remote debugging scenarios anyway.
        //
        // When doing jit attach for pure managed debugging we allow the native attach event to be signaled
        // after DebugActiveProcess completes which means we must wait here long enough to have set the debuggee
        // bit indicating managed attach is coming.
        // However in interop debugging we can't do that because there are debug events which come before the
        // loader breakpoint (which is how far we need to get to set the debuggee bit). If we blocked
        // DebugActiveProcess there then the debug events would be referring to an ICorDebugProcess that hasn't
        // yet been returned to the caller of DebugActiveProcess. Instead, for interop debugging we force the
        // native debugger to wait until it gets the loader breakpoint to set the event. Note we can't converge
        // on that solution for the pure managed case because there is no loader breakpoint event. Hence pure
        // managed and interop debugging each require their own solution
        //
        // See bugs Dev10 600873 and 595322 for examples of what happens if we wait in interop or don't wait
        // in pure managed respectively
        //
        // Long term this should all go away because we won't need to set a managed attach pending bit because
        // there shouldn't be any IPC events involved in managed attach. There might not even be a notion of
        // being 'managed attached'
        if(!pShim->m_fIsInteropDebugging)
        {
            DWORD  dwHandles = 2;
            HANDLE arrHandles[2];

            arrHandles[0] = pShim->m_terminatingEvent;
            arrHandles[1] = pShim->m_markAttachPendingEvent;

            // Wait for the completion of marking pending attach bit or debugger detaching
            WaitForMultipleObjectsEx(dwHandles, arrHandles, FALSE, INFINITE, FALSE);
        }
#endif //!FEATURE_DBGIPC_TRANSPORT_DI
    }
    EX_CATCH_HRESULT(hr);

    // If this succeeds, then process takes ownership of thread. Else we need to kill it.
    if (FAILED(hr))
    {
        if (pShim!= NULL)
        {
            pShim->Dispose();
        }
    }

    // Always release our ref to ShimProcess. If the Process was created, then it takes a reference.

    return hr;
}

//-----------------------------------------------------------------------------
// Neuter all of all children, but not the actual process object.
//
// Assumptions:
//   This clears Right-side state. Assumptions about left-side state are either:
//   1. We're in a shutdown scenario, where all left-side state is already
//   freed.
//   2. Caller already verified there are no left-side resources (eg, by calling
//   code:CordbProcess::IsReadyForDetach)
//   3. Caller did code:CordbProcess::NeuterLeftSideResources first
//   to clean up left-side resources.
//
// Notes:
//   This could be called multiple times (code:CordbProcess::FlushAll), so
//   be sure to null out any potential dangling pointers. State may be rebuilt
//   up after each time.
void CordbProcess::NeuterChildren()
{
    _ASSERTE(GetProcessLock()->HasLock());

    // Frees left-side resources. See assumptions above.
    m_LeftSideResourceCleanupList.NeuterAndClear(this);


    m_EvalTable.Clear();


    // Sweep neuter lists.
    m_ExitNeuterList.NeuterAndClear(this);
    m_ContinueNeuterList.NeuterAndClear(this);

    m_userThreads.NeuterAndClear(GetProcessLock());

    m_pDefaultAppDomain = NULL;

    // Frees per-appdomain left-side resources. See assumptions above.
    m_appDomains.NeuterAndClear(GetProcessLock());
    if (m_sharedAppDomain != NULL)
    {
        m_sharedAppDomain->Neuter();
        m_sharedAppDomain->InternalRelease();
        m_sharedAppDomain = NULL;
    }

    m_steppers.NeuterAndClear(GetProcessLock());

#ifdef FEATURE_INTEROP_DEBUGGING
    if (m_lastDispatchedIBEvent != NULL)
    {
        m_lastDispatchedIBEvent->m_owner->InternalRelease();
        m_lastDispatchedIBEvent = NULL;
    }

    m_unmanagedThreads.NeuterAndClear(GetProcessLock());
#endif // FEATURE_INTEROP_DEBUGGING

    // Explicitly keep the Win32EventThread alive so that we can use it in the window
    // between NeuterChildren + Neuter.
}

//-----------------------------------------------------------------------------
// Neuter
//
// When the process dies, remove all the resources associated with this object.
//
// Notes:
//   Once we neuter ourself, we can no longer send IPC events. So this is useful
//   on detach. This will be called on FlushAll (which has Whidbey detach
//   semantics)
//-----------------------------------------------------------------------------
void CordbProcess::Neuter()
{
    // Process's Neuter is at the top of the neuter tree. So we take the process-lock
    // here and then all child items (appdomains, modules, etc) will assert
    // that they hold the lock.
    _ASSERTE(!this->ThreadHoldsProcessLock());

    // Take the process lock.
    RSLockHolder lockHolder(GetProcessLock());


    NeuterChildren();

    // Release the metadata interfaces
    m_pMetaDispenser.Clear();


    if (m_hHelperThread != NULL)
    {
        CloseHandle(m_hHelperThread);
        m_hHelperThread = NULL;
    }

    {
        lockHolder.Release();
        {
            // We may still hold the Stop-Go lock.
            // @dbgtodo - left-side resources / shutdown, shim: Currently
            // the shim shutdown is too interwoven with CordbProcess to split
            // it out from the locks. Must fully hoist the W32ET and make
            // it safely outside the RS, and outside the protection of RS
            // locks.
            PUBLIC_API_UNSAFE_ENTRY_FOR_SHIM(this);

            // Now that all of our children are neutered, it should be safe to kill the W32ET.
            // Shutdown the shim, and this will also shutdown the W32ET.
            // Do this outside of the process-lock so that we can shutdown the
            // W23ET.
            if (m_pShim != NULL)
            {
                m_pShim->Dispose();
                m_pShim.Clear();
            }
        }

        lockHolder.Acquire();
    }

    // Unload DAC, and then release our final data target references
    FreeDac();
    m_pDACDataTarget.Clear();
    m_pMutableDataTarget.Clear();
    m_pMetaDataLocator.Clear();

    if (m_pEventChannel != NULL)
    {
        m_pEventChannel->Delete();
        m_pEventChannel = NULL;
    }

    // Need process lock to clear the patch table
    ClearPatchTable();

    CordbProcess::CloseIPCHandles();

    CordbBase::Neuter();

    m_cordb.Clear();

    // Need to release this reference to ourselves. Other leaf objects may still hold
    // strong references back to this CordbProcess object.
    _ASSERTE(m_pProcess == this);
    m_pProcess.Clear();
}

// Wrapper to return metadata dispenser.
//
// Notes:
//    Does not adjust reference count of dispenser.
//    Dispenser is destroyed in code:CordbProcess::Neuter
//    Dispenser is non-null.
IMetaDataDispenserEx * CordbProcess::GetDispenser()
{
    _ASSERTE(m_pMetaDispenser != NULL);
    return m_pMetaDispenser;
}


void CordbProcess::CloseIPCHandles()
{
    INTERNAL_API_ENTRY(this);

    // Close off Right Side's handles.
    if (m_leftSideEventAvailable != NULL)
    {
        CloseHandle(m_leftSideEventAvailable);
        m_leftSideEventAvailable = NULL;
    }

    if (m_leftSideEventRead != NULL)
    {
        CloseHandle(m_leftSideEventRead);
        m_leftSideEventRead = NULL;
    }

    if (m_handle != NULL)
    {
        // @dbgtodo  - We should probably add asserts to all calls to CloseHandles(), but this has been
        // a particularly problematic spot in the past for Mac debugging.
        BOOL fSuccess = CloseHandle(m_handle);
        (void)fSuccess; //prevent "unused variable" error from GCC
        _ASSERTE(fSuccess);

        m_handle = NULL;
    }

#if defined(FEATURE_INTEROP_DEBUGGING)
    if (m_leftSideUnmanagedWaitEvent != NULL)
    {
        CloseHandle(m_leftSideUnmanagedWaitEvent);
        m_leftSideUnmanagedWaitEvent = NULL;
    }
#endif // FEATURE_INTEROP_DEBUGGING

    if (m_stopWaitEvent != NULL)
    {
        CloseHandle(m_stopWaitEvent);
        m_stopWaitEvent = NULL;
    }
}


//-----------------------------------------------------------------------------
// Create new OS Thread for the Win32 Event Thread (the thread used in interop-debugging to sniff
// native debug events). This is 1:1 w/ a CordbProcess object.
// This will then be used to actually create the CordbProcess object.
// The process object will then take ownership of the thread.
//
// Arguments:
//     pCordb - the root object that the process lives under
//
// Return values:
//     S_OK on success.
//-----------------------------------------------------------------------------
HRESULT ShimProcess::CreateAndStartWin32ET(Cordb * pCordb)
{

    //
    // Create the win32 event listening thread
    //
    CordbWin32EventThread * pWin32EventThread = new (nothrow) CordbWin32EventThread(pCordb, this);

    HRESULT hr = S_OK;

    if (pWin32EventThread != NULL)
    {
        hr = pWin32EventThread->Init();

        if (SUCCEEDED(hr))
        {
            hr = pWin32EventThread->Start();
        }

        if (FAILED(hr))
        {
            delete pWin32EventThread;
            pWin32EventThread = NULL;
        }
    }
    else
    {
        hr = E_OUTOFMEMORY;
    }

    m_pWin32EventThread = pWin32EventThread;
    return ErrWrapper(hr);
}


//---------------------------------------------------------------------------------------
//
// Try to initialize the DAC. Called in scenarios where it may fail.
//
// Return Value:
//    TRUE  - DAC is initialized.
//    FALSE  - Not initialized, but can try again later. Common case if
//          target has not yet loaded the runtime.
//    Throws exception - fatal.
//
// Assumptions:
//    Target is stopped by OS, so we can safely inspect it without it moving on us.
//
// Notes:
//    This can be called eagerly to sniff if the LS is initialized.
//
//---------------------------------------------------------------------------------------
BOOL CordbProcess::TryInitializeDac()
{
    CONTRACTL
    {
        THROWS;
    }
    CONTRACTL_END;

    // Target is stopped by OS, so we can safely inspect it without it moving on us.

    // We want to avoid exceptions in the normal case, so we do some pre-checks
    // to detect failure without relying on exceptions.
    // Can't initialize DAC until mscorwks is loaded. So that's a sanity test.
    HRESULT hr = EnsureClrInstanceIdSet();
    if (FAILED(hr))
    {
        return FALSE;
    }

    // By this point, we know which CLR in the target to debug. That means there is a CLR
    // in the target, and it's safe to initialize DAC.
    _ASSERTE(m_clrInstanceId != 0);

    // Now expect it to succeed
    InitializeDac();
    return TRUE;
}

//---------------------------------------------------------------------------------------
//
// Load & Init DAC, expecting to succeed.
//
// Return Value:
//    Throws on failure.
//
// Assumptions:
//    Caller invokes this at a point where they can expect it to succeed.
//    This is called early in the startup path because DAC is needed for accessing
//    data in the target.
//
// Notes:
//    This needs to succeed, and should always succeed (baring a bad installation)
//    so we assert on failure paths.
//    This may be called mutliple times.
//
//---------------------------------------------------------------------------------------
void CordbProcess::InitializeDac()
{
    CONTRACTL
    {
        THROWS;
    }
    CONTRACTL_END;
    INTERNAL_API_ENTRY(this);

    // For Mac debugginger, m_hDacModule is not used, and it will always be NULL.  To check whether DAC has
    // been initialized, we need to check something else, namely m_pDacPrimitives.
    if (m_pDacPrimitives == NULL)
    {
        LOG((LF_CORDB, LL_INFO1000, "About to load DAC\n"));
        CreateDacDbiInterface(); // throws
    }
    else
    {
        LOG((LF_CORDB, LL_INFO1000, "Dac already loaded, 0x%p\n", (HMODULE)m_hDacModule));
    }

    // Always flush dac.
    ForceDacFlush();
}

//---------------------------------------------------------------------------------------
//
// Free DAC resources
//
// Notes:
//    This should clean up state such that code:CordbProcess::InitializeDac could be called again.
//
//---------------------------------------------------------------------------------------
void CordbProcess::FreeDac()
{
    CONTRACTL
    {
        NOTHROW; // backout code.
    }
    CONTRACTL_END;

    if (m_pDacPrimitives != NULL)
    {
        m_pDacPrimitives->Destroy();
        m_pDacPrimitives = NULL;
    }

    if (m_hDacModule != NULL)
    {
        LOG((LF_CORDB, LL_INFO1000, "Unloading DAC\n"));
        m_hDacModule.Clear();
    }
}

IEventChannel * CordbProcess::GetEventChannel()
{
    _ASSERTE(m_pEventChannel != NULL);
    return m_pEventChannel;
}

//---------------------------------------------------------------------------------------
// Mark that the process is being interop-debugged.
//
// Notes:
//   @dbgtodo shim: this should eventually move into the shim or go away.
//   It's only to support V2 legacy interop-debugging.
//   Called after code:CordbProcess::Init if we want to enable interop debugging.
//   This allows us to separate out Interop-debugging flags from the core initialization,
//   and paves the way for us to eventually remove it.
//
//   Since we're always on the naitve-pipeline, the Enabling interop debugging just changes
//   how the native debug events are being handled. So this must be called after Init, but
//   before any events are actually handled.
//   This mus be calle on the win32 event thread to guarantee that it's called before WFDE.
void CordbProcess::EnableInteropDebugging()
{
    CONTRACTL
    {
        THROWS;
        PRECONDITION(m_pShim != NULL);
    }
    CONTRACTL_END;

    // Must be on W32ET to guarantee that we're called after Init yet before WFDE (which
    // are both called on the W32et).
    _ASSERTE(IsWin32EventThread());
#ifdef FEATURE_INTEROP_DEBUGGING

    m_state |= PS_WIN32_ATTACHED;
    if (GetDCB() != NULL)
    {
        GetDCB()->m_rightSideIsWin32Debugger = true;
        UpdateLeftSideDCBField(&(GetDCB()->m_rightSideIsWin32Debugger), sizeof(GetDCB()->m_rightSideIsWin32Debugger));
    }

    // Tell the Shim we're interop-debugging.
    m_pShim->SetIsInteropDebugging(true);
#else
    ThrowHR(CORDBG_E_INTEROP_NOT_SUPPORTED);
#endif
}

//---------------------------------------------------------------------------------------
//
// Init -- create any objects that the process object needs to operate.
//
// Arguments:
//
// Return Value:
//    S_OK on success
//
// Assumptions:
//    Called on Win32 Event Thread, after OS debugging pipeline is established but
//    before WaitForDebugEvent / ContinueDebugEvent. This means the target is stopped.
//
// Notes:
//    To enable interop-debugging, call code:CordbProcess::EnableInteropDebugging
//---------------------------------------------------------------------------------------
HRESULT CordbProcess::Init()
{
    INTERNAL_API_ENTRY(this);

    HRESULT hr = S_OK;
    BOOL fIsLSStarted = FALSE; // see meaning below.

    FAIL_IF_NEUTERED(this);


    EX_TRY
    {
        m_processMutex.Init("Process Lock", RSLock::cLockReentrant, RSLock::LL_PROCESS_LOCK);
        m_StopGoLock.Init("Stop-Go Lock", RSLock::cLockReentrant, RSLock::LL_STOP_GO_LOCK);

#ifdef _DEBUG
        m_appDomains.DebugSetRSLock(GetProcessLock());
        m_userThreads.DebugSetRSLock(GetProcessLock());
#ifdef FEATURE_INTEROP_DEBUGGING
        m_unmanagedThreads.DebugSetRSLock(GetProcessLock());
#endif
        m_steppers.DebugSetRSLock(GetProcessLock());
#endif

        // See if the data target is mutable, and cache the mutable interface if it is
        // We must initialize this before we try to use the data target to access the memory in the target process.
        m_pMutableDataTarget.Clear();            // if we were called already, release
        hr = m_pDACDataTarget->QueryInterface(IID_ICorDebugMutableDataTarget, (void**)&m_pMutableDataTarget);
        if (!SUCCEEDED(hr))
        {
            // The data target doesn't support mutation.  We'll fail any requests that require mutation.
            m_pMutableDataTarget.Assign(new ReadOnlyDataTargetFacade());
        }

        m_pMetaDataLocator.Clear();
        hr = m_pDACDataTarget->QueryInterface(IID_ICorDebugMetaDataLocator, reinterpret_cast<void **>(&m_pMetaDataLocator));

        // Get the metadata dispenser.
        hr = InternalCreateMetaDataDispenser(IID_IMetaDataDispenserEx, (void **)&m_pMetaDispenser);

        // We statically link in the dispenser. We expect it to succeed, except for OOM, which
        // debugger doesn't yet handle.
        SIMPLIFYING_ASSUMPTION_SUCCEEDED(hr);
        IfFailThrow(hr);

        _ASSERTE(m_pMetaDispenser != NULL);

        // In order to allow users to call the metadata reader from multiple threads we need to set
        // a flag on the dispenser to create threadsafe readers. This is done best-effort but
        // really shouldn't ever fail. See issue 696511.
        VARIANT optionValue;
        VariantInit(&optionValue);
        V_VT(&optionValue) = VT_UI4;
        V_UI4(&optionValue) = MDThreadSafetyOn;
        m_pMetaDispenser->SetOption(MetaDataThreadSafetyOptions, &optionValue);

        //
        // Setup internal events.
        // @dbgtodo shim: these events should eventually be in the shim.
        //


        // Managed debugging is built on the native-pipeline, and that will detect against double-attaches.

        // @dbgtodo shim: In V2, LSEA + LSER were used by the LS's helper thread. Now with the V3 pipeline,
        // that helper-thread uses native-debug events. The W32ET gets those events and then uses LSEA, LSER to
        // signal existing RS infrastructure. Eventually get rid of LSEA, LSER completely.
        //

        m_leftSideEventAvailable = WszCreateEvent(NULL, FALSE, FALSE, NULL);
        if (m_leftSideEventAvailable == NULL)
        {
            ThrowLastError();
        }

        m_leftSideEventRead = WszCreateEvent(NULL, FALSE, FALSE, NULL);
        if (m_leftSideEventRead == NULL)
        {
            ThrowLastError();
        }

        m_stopWaitEvent = WszCreateEvent(NULL, TRUE, FALSE, NULL);
        if (m_stopWaitEvent == NULL)
        {
            ThrowLastError();
        }

        if (m_pShim != NULL)
        {
            // Get a handle to the debuggee.
            // This is not needed in the V3 pipeline because we don't assume we have a live, local, process.
            m_handle = GetShim()->GetNativePipeline()->GetProcessHandle();

            if (m_handle == NULL)
            {
                ThrowLastError();
            }
        }

        // The LS startup goes through the following phases:
        // 1) mscorwks not yet loaded (eg, any unmanaged app)
        // 2) mscorwks loaded (DAC can now be used)
        // 3) IPC Block created at OS level
        // 4) IPC block data initialized (so we can read meainingful data from it)
        // 5) LS marks that it's initialized (queryable by a DAC primitive) (may not be atomic)
        // 6) LS fires a "Startup" exception (sniffed by WFDE).
        //
        // LS is currently stopped by OS debugging, so it's doesn't shift phases.
        // From the RS's perspective:
        // - after phase 5 is an attach
        // - before phase 6 is a launch.
        // This means there's an overlap: if we catch it at phase 5, we'll just get
        // an extra Startup exception from phase 6, which is safe. This overlap is good
        // because it means there's no bad window to do an attach in.

        // fIsLSStarted means before phase 6 (eg, RS should expect a startup exception)

        // Determines if the LS is started.

        {
            BOOL fReady = TryInitializeDac();

            if (fReady)
            {
                // Invoke DAC primitive.
                _ASSERTE(m_pDacPrimitives != NULL);
                fIsLSStarted = m_pDacPrimitives->IsLeftSideInitialized();
            }
            else
            {
                _ASSERTE(m_pDacPrimitives == NULL);

                // DAC is not yet loaded, so we're at least before phase 2, which is before phase 6.
                // So leave fIsLSStarted = false. We'll get a startup exception later.
                _ASSERTE(!fIsLSStarted);
            }
        }


        if (fIsLSStarted)
        {
            // Left-side has started up. This is common for Attach cases when managed-code is already running.

            if (m_pShim != NULL)
            {
                FinishInitializeIPCChannelWorker(); // throws

                // At this point, the control block is complete and all four
                // events are available and valid for the remote process.

                // Request that the process object send an Attach IPC event.
                // This is only used in an attach case.
                // @dbgtodo sync: this flag can go away once the
                // shim can use real sync APIs.
                m_fDoDelayedManagedAttached = true;
            }
            else
            {
                // In the V3 pipeline case, if we have the DD-interface, then the runtime is loaded
                // and we consider it initialized.
                if (IsDacInitialized())
                {
                    m_initialized = true;
                }
            }
        }
        else
        {
            // LS is not started yet. This would be common for "Launch" cases.
            // We will get a Startup Exception notification when it does start.
        }
    }
    EX_CATCH_HRESULT(hr);

    if (FAILED(hr))
    {
        CleanupHalfBakedLeftSide();
    }

    return hr;
}


COM_METHOD CordbProcess::CanCommitChanges(ULONG cSnapshots,
                ICorDebugEditAndContinueSnapshot *pSnapshots[],
                ICorDebugErrorInfoEnum **pError)
{
    return E_NOTIMPL;
}

COM_METHOD CordbProcess::CommitChanges(ULONG cSnapshots,
    ICorDebugEditAndContinueSnapshot *pSnapshots[],
    ICorDebugErrorInfoEnum **pError)
{
    return E_NOTIMPL;
}


//
// Terminating -- places the process into the terminated state. This should
// also get any blocking process functions unblocked so they'll return
// a failure code.
//
void CordbProcess::Terminating(BOOL fDetach)
{
    INTERNAL_API_ENTRY(this);

    LOG((LF_CORDB, LL_INFO1000,"CP::T: Terminating process 0x%x detach=%d\n", m_id, fDetach));
    m_terminated = true;

    m_cordb->ProcessStateChanged();

    // Set events that may be blocking stuff.
    // But don't set RSER unless we actually read the event. We don't block on RSER
    // since that wait also checks the leftside's process handle.
    SetEvent(m_leftSideEventRead);
    SetEvent(m_leftSideEventAvailable);
    SetEvent(m_stopWaitEvent);

    if (m_pShim != NULL)
        m_pShim->SetTerminatingEvent();

    if (fDetach && (m_pEventChannel != NULL))
    {
        m_pEventChannel->Detach();
    }
}


// Wrapper to give shim access to code:CordbProcess::QueueManagedAttachIfNeededWorker
void CordbProcess::QueueManagedAttachIfNeeded()
{
    PUBLIC_API_ENTRY_FOR_SHIM(this);
    QueueManagedAttachIfNeededWorker();
}

//---------------------------------------------------------------------------------------
// Hook from Shim to request a managed attach IPC event
//
// Notes:
//   Called by shim after the loader-breakpoint is handled.
//   @dbgtodo sync: ths should go away once the shim can initiate
//   a sync
void CordbProcess::QueueManagedAttachIfNeededWorker()
{
    HRESULT hrQueue = S_OK;

    // m_fDoDelayedManagedAttached ensures that we only send an Attach event if the LS is actually present.
    if (m_fDoDelayedManagedAttached && GetShim()->GetAttached())
    {
        RSLockHolder lockHolder(&this->m_processMutex);
        GetDAC()->MarkDebuggerAttachPending();

        hrQueue = this->QueueManagedAttach();
    }

    if (m_pShim != NULL)
        m_pShim->SetMarkAttachPendingEvent();

    IfFailThrow(hrQueue);
}

//---------------------------------------------------------------------------------------
//
// QueueManagedAttach
//
// Send a managed attach. This is asynchronous and will return immediately.
//
// Return Value:
//    S_OK on success
//
//---------------------------------------------------------------------------------------
HRESULT CordbProcess::QueueManagedAttach()
{
    INTERNAL_API_ENTRY(this);

    _ASSERTE(ThreadHoldsProcessLock());

    _ASSERTE(m_fDoDelayedManagedAttached);
    m_fDoDelayedManagedAttached = false;

    _ASSERTE(IsDacInitialized());

    // We don't know what Queue it.
    SendAttachProcessWorkItem * pItem = new (nothrow) SendAttachProcessWorkItem(this);

    if (pItem == NULL)
    {
        return E_OUTOFMEMORY;
    }

    this->m_cordb->m_rcEventThread->QueueAsyncWorkItem(pItem);

    return S_OK;
}

// However, we still want to synchronize.
// @dbgtodo sync: when we hoist attaching, we can send an DB_IPCE_ASYNC_BREAK event instead or Attach
// (for V2 semantics, we still need to synchronize the process)?
void SendAttachProcessWorkItem::Do()
{
    HRESULT hr;

    // This is being processed on the RCET, where it's safe to take the Stop-Go lock.
    RSLockHolder ch(this->GetProcess()->GetStopGoLock());

    DebuggerIPCEvent *event = (DebuggerIPCEvent*) _alloca(CorDBIPC_BUFFER_SIZE);

    // This just acts like an async-break, which will kick off things.
    // This will not induce any faked attach events from the VM (like it did in V2).
    // The Left-side will still slip forward allowing the async-break to happen, so
    // we may get normal debug events in addition to the sync-complete.
    //
    // 1. In the common attach case, we should just get a sync-complete.
    // 2. In Jit-attach cases, the LS is sending an event, and so we'll get that event and then the sync-complete.
    GetProcess()->InitAsyncIPCEvent(event, DB_IPCE_ATTACHING, VMPTR_AppDomain::NullPtr());

    // This should result in a sync-complete from the Left-side, which will be raised as an exception
    // that the debugger passes into Filter and then internally goes through code:CordbProcess::TriageSyncComplete
    // and that triggers code:CordbRCEventThread::FlushQueuedEvents to be called on the RCET.
    // We already pre-queued a fake CreateProcess event.

    // The left-side will also mark itself as attached in response to this event.
    // We explicitly don't mark it as attached from the right-side because we want to let the left-side
    // synchronize first (to stop all running threads) before marking the debugger as attached.
    LOG((LF_CORDB, LL_INFO1000, "[%x] CP::S: sending attach.\n", GetCurrentThreadId()));

    hr = GetProcess()->SendIPCEvent(event, CorDBIPC_BUFFER_SIZE);

    LOG((LF_CORDB, LL_INFO1000, "[%x] CP::S: sent attach.\n", GetCurrentThreadId()));
}

//---------------------------------------------------------------------------------------
// Try to lookup a cached thread object
//
// Arguments:
//     vmThread - vm identifier for thread.
//
// Returns:
//     Thread object if cached; null if not yet cached.
//
// Notes:
//     This does not create the thread object if it's not cached. Caching is unpredictable,
//     and so this may appear to randomly return NULL.
//     Callers should prefer code:CordbProcess::LookupOrCreateThread unless they expicitly
//     want to check RS state.
CordbThread * CordbProcess::TryLookupThread(VMPTR_Thread vmThread)
{
    return m_userThreads.GetBase(VmPtrToCookie(vmThread));
}

//---------------------------------------------------------------------------------------
// Lookup (or create) a CordbThread object by the given volatile OS id. Returns null if not a manged thread
//
// Arguments:
//      dwThreadId - os thread id that a managed thread may be using.
//
// Returns:
//      Thread instance if there is currently a managed thread scheduled to run on dwThreadId.
//      NULL if this tid is not a valid Managed thread. (This is considered a common case)
//      Throws on error.
//
// Notes:
//      OS Thread ID is not fiber-safe, so this is a dangerous function to call.
//      Avoid this as much as possible. Prefer using VMPTR_Thread and
//      code:CordbProcess::LookupOrCreateThread instead of OS thread IDs.
//      See code:CordbThread::GetID for details.
CordbThread * CordbProcess::TryLookupOrCreateThreadByVolatileOSId(DWORD dwThreadId)
{
    PrepopulateThreadsOrThrow();
    return TryLookupThreadByVolatileOSId(dwThreadId);
}

//---------------------------------------------------------------------------------------
// Lookup a cached CordbThread object by the tid. Returns null if not in the cache (which
// includes unmanged thread)
//
// Arguments:
//      dwThreadId - os thread id that a managed thread may be using.
//
// Returns:
//      Thread instance if there is currently a managed thread scheduled to run on dwThreadId.
//      NULL if this tid is not a valid Managed thread. (This is considered a common case)
//      Throws on error.
//
// Notes:
//   Avoids this method:
//   * OS Thread ID is not fiber-safe, so this is a dangerous function to call.
//   * This is juts a Lookup, not LookupOrCreate, so it should only be used by methods
//    that care about the RS state (instead of just LS state).
//   Prefer using VMPTR_Thread and code:CordbProcess::LookupOrCreateThread
//
CordbThread * CordbProcess::TryLookupThreadByVolatileOSId(DWORD dwThreadId)
{
    HASHFIND find;
    for (CordbThread * pThread = m_userThreads.FindFirst(&find);
         pThread != NULL;
         pThread =  m_userThreads.FindNext(&find))
    {
        _ASSERTE(pThread != NULL);

        // Get the OS tid. This returns 0 if the thread is switched out.
        DWORD dwThreadId2 = GetDAC()->TryGetVolatileOSThreadID(pThread->m_vmThreadToken);
        if (dwThreadId2 == dwThreadId)
        {
            return pThread;
        }
    }

    // This OS thread ID does not match any managed thread id.
    return NULL;
}

//---------------------------------------------------------------------------------------
// Preferred CordbThread lookup routine.
//
// Arguments:
//     vmThread - LS thread to lookup. Must be non-null.
//
// Returns:
//     CordbThread instance for given vmThread. May return a previously cached
//     instance or create a new instance. Never returns NULL.
//     Throw on error.
CordbThread * CordbProcess::LookupOrCreateThread(VMPTR_Thread vmThread)
{
    _ASSERTE(!vmThread.IsNull());

    // Return if we have an existing instance.
    CordbThread * pReturn = TryLookupThread(vmThread);
    if (pReturn != NULL)
    {
        return pReturn;
    }

    RSInitHolder<CordbThread> pThread(new CordbThread(this, vmThread)); // throws
    pReturn = pThread.TransferOwnershipToHash(&m_userThreads);

    return pReturn;
}




HRESULT CordbProcess::QueryInterface(REFIID id, void **pInterface)
{
    if (id == IID_ICorDebugProcess)
    {
        *pInterface = static_cast<ICorDebugProcess*>(this);
    }
    else if (id == IID_ICorDebugController)
    {
        *pInterface = static_cast<ICorDebugController*>(static_cast<ICorDebugProcess*>(this));
    }
    else if (id == IID_ICorDebugProcess2)

    {
        *pInterface = static_cast<ICorDebugProcess2*>(this);
    }
    else if (id == IID_ICorDebugProcess3)
    {
        *pInterface = static_cast<ICorDebugProcess3*>(this);
    }
    else if (id == IID_ICorDebugProcess4)
    {
        *pInterface = static_cast<ICorDebugProcess4*>(this);
    }
    else if (id == IID_ICorDebugProcess5)
    {
        *pInterface = static_cast<ICorDebugProcess5*>(this);
    }
    else if (id == IID_ICorDebugProcess7)
    {
        *pInterface = static_cast<ICorDebugProcess7*>(this);
    }
    else if (id == IID_ICorDebugProcess8)
    {
        *pInterface = static_cast<ICorDebugProcess8*>(this);
    }
    else if (id == IID_ICorDebugProcess11)
    {
        *pInterface = static_cast<ICorDebugProcess11*>(this);
    }
    else if (id == IID_IUnknown)
    {
        *pInterface = static_cast<IUnknown*>(static_cast<ICorDebugProcess*>(this));
    }

    else
    {
        *pInterface = NULL;
        return E_NOINTERFACE;
    }

    ExternalAddRef();
    return S_OK;
}




// Public implementation of ICorDebugProcess4::ProcessStateChanged
HRESULT CordbProcess::ProcessStateChanged(CorDebugStateChange eChange)
{
    HRESULT hr = S_OK;
    PUBLIC_API_BEGIN(this)
    {
        switch(eChange)
        {
        case PROCESS_RUNNING:
            FlushProcessRunning();
            break;

        case FLUSH_ALL:
            FlushAll();
            break;

        default:
            ThrowHR(E_INVALIDARG);

        }
    }
    PUBLIC_API_END(hr);
    return hr;
}


HRESULT CordbProcess::EnumerateHeap(ICorDebugHeapEnum **ppObjects)
{
    if (!ppObjects)
        return E_POINTER;

    HRESULT hr = S_OK;
    PUBLIC_API_ENTRY(this);
    ATT_REQUIRE_STOPPED_MAY_FAIL(this);

    EX_TRY
    {
        if (m_pDacPrimitives->AreGCStructuresValid())
        {
            CordbHeapEnum *pHeapEnum = new CordbHeapEnum(this);
            GetContinueNeuterList()->Add(this, pHeapEnum);
            hr = pHeapEnum->QueryInterface(__uuidof(ICorDebugHeapEnum), (void**)ppObjects);
        }
        else
        {
            hr = CORDBG_E_GC_STRUCTURES_INVALID;
        }
    }
    EX_CATCH_HRESULT(hr);

    return hr;
}

HRESULT CordbProcess::GetGCHeapInformation(COR_HEAPINFO *pHeapInfo)
{
    if (!pHeapInfo)
        return E_INVALIDARG;

    HRESULT hr = S_OK;
    PUBLIC_API_ENTRY(this);
    ATT_REQUIRE_STOPPED_MAY_FAIL(this);

    EX_TRY
    {
        GetDAC()->GetGCHeapInformation(pHeapInfo);
    }
    EX_CATCH_HRESULT(hr);

    return hr;
}

HRESULT CordbProcess::EnumerateHeapRegions(ICorDebugHeapSegmentEnum **ppRegions)
{
    if (!ppRegions)
        return E_INVALIDARG;

    HRESULT hr = S_OK;
    PUBLIC_API_ENTRY(this);
    ATT_REQUIRE_STOPPED_MAY_FAIL(this);

    EX_TRY
    {
        DacDbiArrayList<COR_SEGMENT> segments;
        hr = GetDAC()->GetHeapSegments(&segments);

        if (SUCCEEDED(hr))
        {
            if (!segments.IsEmpty())
            {
                CordbHeapSegmentEnumerator *segEnum = new CordbHeapSegmentEnumerator(this, &segments[0], (DWORD)segments.Count());
                GetContinueNeuterList()->Add(this, segEnum);
                hr = segEnum->QueryInterface(__uuidof(ICorDebugHeapSegmentEnum), (void**)ppRegions);
            }
            else
            {
                hr = E_OUTOFMEMORY;
            }
        }
    }
    EX_CATCH_HRESULT(hr);

    return hr;
}

HRESULT CordbProcess::GetObject(CORDB_ADDRESS addr, ICorDebugObjectValue **ppObject)
{
    return this->GetObjectInternal(addr, nullptr, ppObject);
}

HRESULT CordbProcess::GetObjectInternal(CORDB_ADDRESS addr, CordbAppDomain* pAppDomainOverride, ICorDebugObjectValue **pObject)
{
    HRESULT hr = S_OK;

    PUBLIC_REENTRANT_API_ENTRY(this);
    ATT_REQUIRE_STOPPED_MAY_FAIL(this);

    EX_TRY
    {
        if (!m_pDacPrimitives->IsValidObject(addr))
        {
            hr = CORDBG_E_CORRUPT_OBJECT;
        }
        else if (pObject == NULL)
        {
            hr = E_INVALIDARG;
        }
        else
        {
            RSLockHolder ch(GetProcess()->GetStopGoLock());
            RSLockHolder procLock(this->GetProcess()->GetProcessLock());

            CordbAppDomain *cdbAppDomain = NULL;
            CordbType *pType = NULL;
            hr = GetTypeForObject(addr, pAppDomainOverride, &pType, &cdbAppDomain);

            if (SUCCEEDED(hr))
            {
                _ASSERTE(pType != NULL);
                _ASSERTE(cdbAppDomain != NULL);

                DebuggerIPCE_ObjectData objData;
                m_pDacPrimitives->GetBasicObjectInfo(addr, ELEMENT_TYPE_CLASS, cdbAppDomain->GetADToken(), &objData);

                NewHolder<CordbObjectValue> pNewObjectValue(new CordbObjectValue(cdbAppDomain, pType, TargetBuffer(addr, (ULONG)objData.objSize), &objData));
                hr = pNewObjectValue->Init();

                if (SUCCEEDED(hr))
                {
                    hr = pNewObjectValue->QueryInterface(__uuidof(ICorDebugObjectValue), (void**)pObject);
                    if (SUCCEEDED(hr))
                        pNewObjectValue.SuppressRelease();
                }
            }
        }
    }
    EX_CATCH_HRESULT(hr);

    return hr;
}


HRESULT CordbProcess::EnumerateGCReferences(BOOL enumerateWeakReferences, ICorDebugGCReferenceEnum **ppEnum)
{
    if (!ppEnum)
        return E_POINTER;

    HRESULT hr = S_OK;
    PUBLIC_API_ENTRY(this);
    ATT_REQUIRE_STOPPED_MAY_FAIL(this);

    EX_TRY
    {
        CordbRefEnum *pRefEnum = new CordbRefEnum(this, enumerateWeakReferences);
        GetContinueNeuterList()->Add(this, pRefEnum);
        hr = pRefEnum->QueryInterface(IID_ICorDebugGCReferenceEnum, (void**)ppEnum);
    }
    EX_CATCH_HRESULT(hr);
    return hr;
}

HRESULT CordbProcess::EnumerateHandles(CorGCReferenceType types, ICorDebugGCReferenceEnum **ppEnum)
{
    if (!ppEnum)
        return E_POINTER;

    HRESULT hr = S_OK;
    PUBLIC_API_ENTRY(this);
    ATT_REQUIRE_STOPPED_MAY_FAIL(this);

    EX_TRY
    {
        CordbRefEnum *pRefEnum = new CordbRefEnum(this, types);
        GetContinueNeuterList()->Add(this, pRefEnum);
        hr = pRefEnum->QueryInterface(IID_ICorDebugGCReferenceEnum, (void**)ppEnum);
    }
    EX_CATCH_HRESULT(hr);

    return hr;
}

HRESULT CordbProcess::EnableNGENPolicy(CorDebugNGENPolicy ePolicy)
{
    return E_NOTIMPL;
}


HRESULT CordbProcess::GetTypeID(CORDB_ADDRESS obj, COR_TYPEID *pId)
{
    if (pId == NULL)
        return E_POINTER;

    HRESULT hr = S_OK;
    PUBLIC_API_ENTRY(this);
    ATT_REQUIRE_STOPPED_MAY_FAIL(this);

    EX_TRY
    {
        hr = GetProcess()->GetDAC()->GetTypeID(obj, pId);
    }
    EX_CATCH_HRESULT(hr);

    return hr;
}

HRESULT CordbProcess::GetTypeForTypeID(COR_TYPEID id, ICorDebugType **ppType)
{
    if (ppType == NULL)
        return E_POINTER;

    HRESULT hr = S_OK;

    PUBLIC_API_ENTRY(this);
    RSLockHolder stopGoLock(this->GetProcess()->GetStopGoLock());
    RSLockHolder procLock(this->GetProcess()->GetProcessLock());

    EX_TRY
    {
        DebuggerIPCE_ExpandedTypeData data;
        GetDAC()->GetObjectExpandedTypeInfoFromID(AllBoxed, VMPTR_AppDomain::NullPtr(), id, &data);

        CordbType *type = 0;
        hr = CordbType::TypeDataToType(GetSharedAppDomain(), &data, &type);

        if (SUCCEEDED(hr))
            hr = type->QueryInterface(IID_ICorDebugType, (void**)ppType);
    }
    EX_CATCH_HRESULT(hr);

    return hr;
}


COM_METHOD CordbProcess::GetArrayLayout(COR_TYPEID id, COR_ARRAY_LAYOUT *pLayout)
{
    if (pLayout == NULL)
        return E_POINTER;

    HRESULT hr = S_OK;
    PUBLIC_API_BEGIN(this);

    hr = GetProcess()->GetDAC()->GetArrayLayout(id, pLayout);

    PUBLIC_API_END(hr);
    return hr;
}

COM_METHOD CordbProcess::GetTypeLayout(COR_TYPEID id, COR_TYPE_LAYOUT *pLayout)
{
    if (pLayout == NULL)
        return E_POINTER;

    HRESULT hr = S_OK;
    PUBLIC_API_BEGIN(this);

    hr = GetProcess()->GetDAC()->GetTypeLayout(id, pLayout);

    PUBLIC_API_END(hr);
    return hr;
}

COM_METHOD CordbProcess::GetTypeFields(COR_TYPEID id, ULONG32 celt, COR_FIELD fields[], ULONG32 *pceltNeeded)
{
    HRESULT hr = S_OK;
    PUBLIC_API_BEGIN(this);

    hr = GetProcess()->GetDAC()->GetObjectFields(id, celt, fields, pceltNeeded);

    PUBLIC_API_END(hr);
    return hr;
}

COM_METHOD CordbProcess::SetWriteableMetadataUpdateMode(WriteableMetadataUpdateMode flags)
{
    HRESULT hr = S_OK;
    PUBLIC_API_BEGIN(this);

    if(flags != LegacyCompatPolicy &&
       flags != AlwaysShowUpdates)
    {
        hr = E_INVALIDARG;
    }
    else if(m_pShim != NULL)
    {
        if(flags != LegacyCompatPolicy)
        {
            hr = CORDBG_E_UNSUPPORTED;
        }
    }

    if(SUCCEEDED(hr))
    {
        m_writableMetadataUpdateMode = flags;
    }

    PUBLIC_API_END(hr);
    return hr;
}

COM_METHOD CordbProcess::EnableExceptionCallbacksOutsideOfMyCode(BOOL enableExceptionsOutsideOfJMC)
{
    HRESULT hr = S_OK;
    PUBLIC_API_BEGIN(this);

    hr = GetProcess()->GetDAC()->SetSendExceptionsOutsideOfJMC(enableExceptionsOutsideOfJMC);

    PUBLIC_API_END(hr);
    return hr;
}

COM_METHOD CordbProcess::EnableGCNotificationEvents(BOOL fEnable)
{
    HRESULT hr = S_OK;
    PUBLIC_API_BEGIN(this)
    {
        hr = this->m_pDacPrimitives->EnableGCNotificationEvents(fEnable);
    }
    PUBLIC_API_END(hr);
    return hr;
}

//-----------------------------------------------------------
// ICorDebugProcess11
//-----------------------------------------------------------
COM_METHOD CordbProcess::EnumerateLoaderHeapMemoryRegions(ICorDebugMemoryRangeEnum **ppRanges)
{
    VALIDATE_POINTER_TO_OBJECT(ppRanges, ICorDebugMemoryRangeEnum **);
    FAIL_IF_NEUTERED(this);

    HRESULT hr = S_OK;

    PUBLIC_API_BEGIN(this);
    {
        DacDbiArrayList<COR_MEMORY_RANGE> heapRanges;

        hr = GetDAC()->GetLoaderHeapMemoryRanges(&heapRanges);

        if (SUCCEEDED(hr))
        {
            RSInitHolder<CordbMemoryRangeEnumerator> heapSegmentEnumerator(
                new CordbMemoryRangeEnumerator(this, &heapRanges[0], (DWORD)heapRanges.Count()));

            GetContinueNeuterList()->Add(this, heapSegmentEnumerator);
            heapSegmentEnumerator.TransferOwnershipExternal(ppRanges);
        }
    }
    PUBLIC_API_END(hr);
    return hr;
}

HRESULT CordbProcess::GetTypeForObject(CORDB_ADDRESS addr, CordbAppDomain* pAppDomainOverride, CordbType **ppType, CordbAppDomain **pAppDomain)
{
    VMPTR_AppDomain appDomain;
    VMPTR_Module mod;
    VMPTR_DomainAssembly domainAssembly;

    HRESULT hr = E_FAIL;
    if (GetDAC()->GetAppDomainForObject(addr, &appDomain, &mod, &domainAssembly))
    {
        if (pAppDomainOverride)
        {
            appDomain = pAppDomainOverride->GetADToken();
        }
        CordbAppDomain *cdbAppDomain = appDomain.IsNull() ? GetSharedAppDomain() : LookupOrCreateAppDomain(appDomain);

        _ASSERTE(cdbAppDomain);

        DebuggerIPCE_ExpandedTypeData data;
        GetDAC()->GetObjectExpandedTypeInfo(AllBoxed, appDomain, addr, &data);

        CordbType *type = 0;
        hr = CordbType::TypeDataToType(cdbAppDomain, &data, &type);

        if (SUCCEEDED(hr))
        {
            *ppType = type;
            if (pAppDomain)
                *pAppDomain = cdbAppDomain;
        }
    }

    return hr;
}


// ******************************************
// CordbRefEnum
// ******************************************
CordbRefEnum::CordbRefEnum(CordbProcess *proc, BOOL walkWeakRefs)
    : CordbBase(proc, 0, enumCordbHeap), mRefHandle(0), mEnumStacksFQ(TRUE),
      mHandleMask((UINT32)(walkWeakRefs ? CorHandleAll : CorHandleStrongOnly))
{
}

CordbRefEnum::CordbRefEnum(CordbProcess *proc, CorGCReferenceType types)
    : CordbBase(proc, 0, enumCordbHeap), mRefHandle(0), mEnumStacksFQ(FALSE),
      mHandleMask((UINT32)types)
{
}

void CordbRefEnum::Neuter()
{
    EX_TRY
    {
        if (mRefHandle)
        {
            GetProcess()->GetDAC()->DeleteRefWalk(mRefHandle);
            mRefHandle = 0;
        }
    }
    EX_CATCH
    {
        _ASSERTE(!"Hit an error freeing a ref walk.");
    }
    EX_END_CATCH(SwallowAllExceptions)

    CordbBase::Neuter();
}

HRESULT CordbRefEnum::QueryInterface(REFIID riid, void **ppInterface)
{
    if (ppInterface == NULL)
        return E_INVALIDARG;

    if (riid == IID_ICorDebugGCReferenceEnum)
    {
        *ppInterface = static_cast<ICorDebugGCReferenceEnum*>(this);
    }
    else if (riid == IID_IUnknown)
    {
        *ppInterface = static_cast<IUnknown*>(static_cast<ICorDebugGCReferenceEnum*>(this));
    }
    else
    {
        *ppInterface = NULL;
        return E_NOINTERFACE;
    }

    ExternalAddRef();
    return S_OK;
}

HRESULT CordbRefEnum::Skip(ULONG celt)
{
    return E_NOTIMPL;
}

HRESULT CordbRefEnum::Reset()
{
    PUBLIC_API_ENTRY(this);
    HRESULT hr = S_OK;
    EX_TRY
    {
        if (mRefHandle)
        {
            GetProcess()->GetDAC()->DeleteRefWalk(mRefHandle);
            mRefHandle = 0;
        }
    }
    EX_CATCH_HRESULT(hr);

    return hr;
}

HRESULT CordbRefEnum::Clone(ICorDebugEnum **ppEnum)
{
    return E_NOTIMPL;
}

HRESULT CordbRefEnum::GetCount(ULONG *pcelt)
{
    return E_NOTIMPL;
}


//

HRESULT CordbRefEnum::Next(ULONG celt, COR_GC_REFERENCE refs[], ULONG *pceltFetched)
{
    if (refs == NULL || pceltFetched == NULL)
        return E_POINTER;

    CordbProcess *process = GetProcess();
    HRESULT hr = S_OK;

    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    ATT_REQUIRE_STOPPED_MAY_FAIL(process);

    RSLockHolder procLockHolder(process->GetProcessLock());

    EX_TRY
    {
        if (!mRefHandle)
            hr = process->GetDAC()->CreateRefWalk(&mRefHandle, mEnumStacksFQ, mEnumStacksFQ, mHandleMask);

        if (SUCCEEDED(hr))
        {
            DacGcReference dacRefs[32];
            ULONG toFetch = ARRAY_SIZE(dacRefs);
            ULONG total = 0;

            for (ULONG c = 0; SUCCEEDED(hr) && c < (celt/ARRAY_SIZE(dacRefs) + 1); ++c)
            {
                // Fetch 32 references at a time, the last time, only fetch the remainder (that is, if
                // the user didn't fetch a multiple of 32).
                if (c == celt/ARRAY_SIZE(dacRefs))
                    toFetch = celt % ARRAY_SIZE(dacRefs);

                ULONG fetched = 0;
                hr = process->GetDAC()->WalkRefs(mRefHandle, toFetch, dacRefs, &fetched);

                if (SUCCEEDED(hr))
                {
                    for (ULONG i = 0; i < fetched; ++i)
                    {
                        CordbAppDomain *pDomain = process->LookupOrCreateAppDomain(dacRefs[i].vmDomain);

                        ICorDebugAppDomain *pAppDomain = NULL;
                        ICorDebugValue *pOutObject = NULL;
                        if (dacRefs[i].pObject & 1)
                        {
                            dacRefs[i].pObject &= ~1;
                            ICorDebugObjectValue *pObjValue = NULL;

                            hr = process->GetObject(dacRefs[i].pObject, &pObjValue);

                            if (SUCCEEDED(hr))
                            {
                                hr = pObjValue->QueryInterface(IID_ICorDebugValue, (void**)&pOutObject);
                                pObjValue->Release();
                            }
                        }
                        else
                        {
                            ICorDebugReferenceValue *tmpValue = NULL;
                            IfFailThrow(CordbReferenceValue::BuildFromGCHandle(pDomain,
                                                                   dacRefs[i].objHnd,
                                                                   &tmpValue));

                            if (SUCCEEDED(hr))
                            {
                                hr = tmpValue->QueryInterface(IID_ICorDebugValue, (void**)&pOutObject);
                                tmpValue->Release();
                            }
                        }

                        if (SUCCEEDED(hr) && pDomain)
                        {
                            hr = pDomain->QueryInterface(IID_ICorDebugAppDomain, (void**)&pAppDomain);
                        }

                        if (FAILED(hr))
                            break;

                        refs[total].Domain = pAppDomain;
                        refs[total].Location = pOutObject;
                        refs[total].Type = (CorGCReferenceType)dacRefs[i].dwType;
                        refs[total].ExtraData = dacRefs[i].i64ExtraData;

                        total++;
                    }
                }
            }

            *pceltFetched = total;
        }
    }
    EX_CATCH_HRESULT(hr);

    return hr;
}


// ******************************************
// CordbHeapEnum
// ******************************************
CordbHeapEnum::CordbHeapEnum(CordbProcess *proc)
    : CordbBase(proc, 0, enumCordbHeap), mHeapHandle(0)
{
}

HRESULT CordbHeapEnum::QueryInterface(REFIID riid, void **ppInterface)
{
    if (ppInterface == NULL)
        return E_INVALIDARG;

    if (riid == IID_ICorDebugHeapEnum)
    {
        *ppInterface = static_cast<ICorDebugHeapEnum*>(this);
    }
    else if (riid == IID_IUnknown)
    {
        *ppInterface = static_cast<IUnknown*>(static_cast<ICorDebugHeapEnum*>(this));
    }
    else
    {
        *ppInterface = NULL;
        return E_NOINTERFACE;
    }

    ExternalAddRef();
    return S_OK;
}

HRESULT CordbHeapEnum::Skip(ULONG celt)
{
    return E_NOTIMPL;
}

HRESULT CordbHeapEnum::Reset()
{
    Clear();
    return S_OK;
}

void CordbHeapEnum::Clear()
{
    EX_TRY
    {
        if (mHeapHandle)
        {
            GetProcess()->GetDAC()->DeleteHeapWalk(mHeapHandle);
            mHeapHandle = 0;
        }
    }
    EX_CATCH
    {
        _ASSERTE(!"Hit an error freeing the heap walk.");
    }
    EX_END_CATCH(SwallowAllExceptions)
}

HRESULT CordbHeapEnum::Clone(ICorDebugEnum **ppEnum)
{
    return E_NOTIMPL;
}

HRESULT CordbHeapEnum::GetCount(ULONG *pcelt)
{
    return E_NOTIMPL;
}

HRESULT CordbHeapEnum::Next(ULONG celt, COR_HEAPOBJECT objects[], ULONG *pceltFetched)
{
    HRESULT hr = S_OK;
    PUBLIC_API_ENTRY(this);
    RSLockHolder stopGoLock(this->GetProcess()->GetStopGoLock());
    RSLockHolder procLock(this->GetProcess()->GetProcessLock());
    ULONG fetched = 0;

    EX_TRY
    {
        if (mHeapHandle == 0)
        {
            hr = GetProcess()->GetDAC()->CreateHeapWalk(&mHeapHandle);
        }

        if (SUCCEEDED(hr))
        {
            hr = GetProcess()->GetDAC()->WalkHeap(mHeapHandle, celt, objects, &fetched);
            _ASSERTE(fetched <= celt);
        }

        if (SUCCEEDED(hr))
        {
            // Return S_FALSE if we've reached the end of the enum.
            if (fetched < celt)
                hr = S_FALSE;
        }
    }
    EX_CATCH_HRESULT(hr);

    // Set the fetched parameter to reflect the number of elements (if any)
    // that were successfully saved to "objects"
    if (pceltFetched)
        *pceltFetched = fetched;

    return hr;
}

//---------------------------------------------------------------------------------------
// Flush state for when the process starts running.
//
// Notes:
//   Helper for code:CordbProcess::ProcessStateChanged.
//   Since ICD Arrowhead does not own the eventing pipeline, it needs the debugger to
//   notifying it of when the process is running again.  This is like the counterpart
//   to code:CordbProcess::Filter
void CordbProcess::FlushProcessRunning()
{
    _ASSERTE(GetProcessLock()->HasLock());

    // Update the continue counter.
    m_continueCounter++;

    // Safely dispose anything that should be neutered on continue.
    MarkAllThreadsDirty();
    ForceDacFlush();
}

//---------------------------------------------------------------------------------------
// Flush all cached state and bring us back to "cold startup"
//
// Notes:
//   Helper for code:CordbProcess::ProcessStateChanged.
//   This is used if the data-target changes underneath us in a way that is
//   not consistent with the process running forward. For example, if for
//   a time-travel debugger, the data-target may flow "backwards" in time.
//
void CordbProcess::FlushAll()
{
    CONTRACTL
    {
        THROWS;
    }
    CONTRACTL_END;

    HRESULT hr;
    _ASSERTE(GetProcessLock()->HasLock());

    //
    // First, determine if it's safe to Flush
    //

    hr = IsReadyForDetach();
    IfFailThrow(hr);

    // Check for outstanding CordbHandle values.
    if (OutstandingHandles())
    {
        ThrowHR(CORDBG_E_DETACH_FAILED_OUTSTANDING_TARGET_RESOURCES);
    }

    // FlushAll is a superset of FlushProcessRunning.
    // This will also ensure we clear the DAC cache.
    FlushProcessRunning();

    // If we detach before the CLR is loaded into the debuggee, then we can no-op a lot of work.
    // We sure can't be sending IPC events to the LS before it exists.
    NeuterChildren();
}

//---------------------------------------------------------------------------------------
//
// Detach the Debugger from the LS process.
//
//
// Return Value:
//    S_OK on successful detach. Else error.
//
// Assumptions:
//    Target is stopped.
//
// Notes:
//    Once we're detached, the LS can resume running and exit.
//    So it's possible to get an ExitProcess callback in the middle of the Detach phase. If that happens,
//    we must return CORDBG_E_PROCESS_TERMINATED and pretend that the exit happened before we tried to detach.
//    Else if we detach successfully, return S_OK.
//
//    @dbgtodo attach-bit: need to figure out semantics of Detach
//    in V3, especially w.r.t to an attach bit.
//---------------------------------------------------------------------------------------
HRESULT CordbProcess::Detach()
{
    PUBLIC_API_ENTRY(this);

    FAIL_IF_NEUTERED(this);

    if (IsInteropDebugging())
    {
        return CORDBG_E_INTEROP_NOT_SUPPORTED;
    }


    HRESULT hr = S_OK;
    // A very important note: we require that the process is synchronized before doing a detach. This ensures
    // that no events are on their way from the Left Side. We also require that the user has drained the
    // managed event queue, but there is currently no way to really enforce that here.
    // @todo-  why can't we enforce that the managed event Q is drained?
    ATT_REQUIRE_SYNCED_OR_NONINIT_MAY_FAIL(this);


    hr = IsReadyForDetach();
    if (FAILED(hr))
    {
        // Avoid neutering. Gives client a chance to fix detach issue and retry.
        return hr;
    }

    // Since the detach may resume the LS and allow it to exit, which may invoke the EP callback
    // which may destroy this process object, be sure to protect us w/ an extra AddRef/Release
    RSSmartPtr<CordbProcess> pRef(this);



    LOG((LF_CORDB, LL_INFO1000, "CP::Detach - beginning\n"));
    if (m_pShim == NULL) // This API is moved off to the shim
    {

        // This is still invasive.
        // Ignore failures. This will fail for a non-invasive target.
        if (IsDacInitialized())
        {
            HRESULT hrIgnore = S_OK;
            EX_TRY
            {
                GetDAC()->MarkDebuggerAttached(FALSE);
            }
            EX_CATCH_HRESULT(hrIgnore);
        }
    }
    else
    {
        EX_TRY
        {
            DetachShim();
        }
        EX_CATCH_HRESULT(hr);
    }

    // Either way, neuter everything.
    this->Neuter();

    // Implicit release on pRef
    LOG((LF_CORDB, LL_INFO1000, "CP::Detach - returning w/ hr=0x%x\n", hr));
    return hr;
}

// Free up key left-side resources
//
// Called on detach
// This does key neutering of objects that hold left-side resources and require
// preemptively freeing the resources.
// After this, code:CordbProcess::Neuter should only affect right-side state.
void CordbProcess::NeuterChildrenLeftSideResources()
{
    _ASSERTE(GetStopGoLock()->HasLock());

    _ASSERTE(!GetProcessLock()->HasLock());
    RSLockHolder lockHolder(GetProcessLock());


    // Need process-lock to operate on hashtable, but can't yet Neuter under process-lock,
    // so we have to copy the contents to an auxilary list which we can then traverse outside the lock.
    RSPtrArray<CordbAppDomain> listAppDomains;
    m_appDomains.CopyToArray(&listAppDomains);



    // Must not hold process lock so that we can be safe to send IPC events
    // to cleanup left-side resources.
    lockHolder.Release();
    _ASSERTE(!GetProcessLock()->HasLock());

    // Frees left-side resources. This may send IPC events.
    // This will make normal neutering a nop.
    m_LeftSideResourceCleanupList.NeuterLeftSideResourcesAndClear(this);

    for(unsigned int idx = 0; idx < listAppDomains.Length(); idx++)
    {
        CordbAppDomain * pAppDomain = listAppDomains[idx];

        // CordbHandleValue is in the appdomain exit list, and that needs
        // to send an IPC event to cleanup and release the handle from
        // the GCs handle table.
        pAppDomain->GetSweepableExitNeuterList()->NeuterLeftSideResourcesAndClear(this);
    }
    listAppDomains.Clear();

}

//---------------------------------------------------------------------------------------
// Detach the Debugger from the LS process for the V2 case
//
// Assumptions:
//      This will NeuterChildren(), caller will do the real Neuter()
//      Caller has already ensured that detach is safe.
//
//   @dbgtodo attach-bit: this should be moved into the shim; need
//   to figure out semantics for freeing left-side resources (especially GC
//   handles) on detach.
void CordbProcess::DetachShim()
{

    HASHFIND hashFind;
    HRESULT hr = S_OK;

    // If we detach before the CLR is loaded into the debuggee, then we can no-op a lot of work.
    // We sure can't be sending IPC events to the LS before it exists.
    if (m_initialized)
    {
        // The managed event queue is not necessarily drained. Cordbg could call detach between any callback.
        // While the process is still stopped, neuter all of our children.
        // This will make our Neuter() a nop and saves the W32ET from having to do dangerous work.
        this->NeuterChildrenLeftSideResources();
        {
            RSLockHolder lockHolder(GetProcessLock());
            this->NeuterChildren();
        }

        // Go ahead and detach from the entire process now. This is like sending a "Continue".
        DebuggerIPCEvent * pIPCEvent = (DebuggerIPCEvent *) _alloca(CorDBIPC_BUFFER_SIZE);
        InitIPCEvent(pIPCEvent, DB_IPCE_DETACH_FROM_PROCESS, true, VMPTR_AppDomain::NullPtr());

        hr = m_cordb->SendIPCEvent(this, pIPCEvent, CorDBIPC_BUFFER_SIZE);
        hr = WORST_HR(hr, pIPCEvent->hr);
        IfFailThrow(hr);
    }
    else
    {
        // @dbgtodo attach-bit: push this up, once detach IPC event is hoisted.
        RSLockHolder lockHolder(GetProcessLock());

        // Shouldn't have any appdomains.
        (void)hashFind; //prevent "unused variable" error from GCC
        _ASSERTE(m_appDomains.FindFirst(&hashFind) == NULL);
    }

    LOG((LF_CORDB, LL_INFO10000, "CP::Detach - got reply from LS\n"));

    // It's possible that the LS may exit after they reply to our detach_from_process, but
    // before we update our internal state that they're detached. So still have to check
    // failure codes here.
    hr = this->m_pShim->GetWin32EventThread()->SendDetachProcessEvent(this);


    // Since we're auto-continuing when we detach, we should set the stop count back to zero.
    // This (along w/ m_detached) prevents anyone from calling Continue on this process
    // after this call returns.
    m_stopCount = 0;

    if (hr != CORDBG_E_PROCESS_TERMINATED)
    {
        // Remember that we've detached from this process object. This will prevent any further operations on
        // this process, just in case... :)
        // If LS exited, then don't set this flag because it overrides m_terminated when reporting errors;
        // and we want to provide a consistent story about whether we detached or whether the LS exited.
        m_detached = true;
    }
    IfFailThrow(hr);


    // Now that all complicated cleanup is done, caller can do a final neuter.
    // This will implicitly stop our Win32 event thread as well.
}

// Delete all events from the queue without dispatching. This is useful in shutdown.
// An event that is currently dispatching is not on the queue.
void CordbProcess::DeleteQueuedEvents()
{
    INTERNAL_API_ENTRY(this);
    // We must have the process lock to ensure that no one is trying to add an event
    _ASSERTE(!ThreadHoldsProcessLock());

    if (m_pShim != NULL)
    {
        PUBLIC_CALLBACK_IN_THIS_SCOPE0_NO_LOCK(this);

        // DeleteAll() is part of the shim, and it will change external ref counts, so must really
        // be marked as outside the RS.
        m_pShim->GetManagedEventQueue()->DeleteAll();
    }
}

//---------------------------------------------------------------------------------------
//
// Track that we're about to dispatch a managed event.
//
// Arguments:
//      event - event being dispatched
//
// Assumptions:
//    This is used to support code:CordbProcess::AreDispatchingEvent
//    This is always called on the same thread as code:CordbProcess::FinishEventDispatch
void CordbProcess::StartEventDispatch(DebuggerIPCEventType event)
{
    LIMITED_METHOD_CONTRACT;

    _ASSERTE(m_dispatchedEvent == DB_IPCE_DEBUGGER_INVALID);
    _ASSERTE(event != DB_IPCE_DEBUGGER_INVALID);
    m_dispatchedEvent = event;
}

//---------------------------------------------------------------------------------------
//
// Track that we're done dispatching a managed event.
//
//
// Assumptions:
//    This is always called on the same thread as code:CordbProcess::StartEventDispatch
//
// Notes:
//   @dbgtodo shim: eventually this goes into the shim when we hoist Continue
void CordbProcess::FinishEventDispatch()
{
    LIMITED_METHOD_CONTRACT;

    _ASSERTE(m_dispatchedEvent != DB_IPCE_DEBUGGER_INVALID);
    m_dispatchedEvent = DB_IPCE_DEBUGGER_INVALID;
}

//---------------------------------------------------------------------------------------
//
// Are we in the middle of dispatching an event?
//
// Notes:
//   This is used by code::CordbProcess::ContinueInternal. Continue logic takes
//   a shortcut if the continue is called on the dispatch thread.
//   It doesn't matter which event is being dispatch; only that we're on the dispatch thread.
//   @dbgtodo shim: eventually this goes into the shim when we hoist Continue
bool CordbProcess::AreDispatchingEvent()
{
    LIMITED_METHOD_CONTRACT;

    return m_dispatchedEvent != DB_IPCE_DEBUGGER_INVALID;
}





// Terminate the app. We'll still dispatch an ExitProcess callback, so the app
// must wait for that before calling Cordb::Terminate.
// If this fails, the client can always call the OS's TerminateProcess command
// to rudely kill the debuggee.
HRESULT CordbProcess::Terminate(unsigned int exitCode)
{
    PUBLIC_API_ENTRY(this);

    LOG((LF_CORDB, LL_INFO1000, "CP::Terminate: with exitcode %u\n", exitCode));
    FAIL_IF_NEUTERED(this);


    // @dbgtodo shutdown: eventually, all of Terminate() will be in the Shim.
    // Free all the remaining events. Since this will call into the shim, do this outside of any locks.
    // (ATT_ takes locks).
    DeleteQueuedEvents();

    ATT_REQUIRE_SYNCED_OR_NONINIT_MAY_FAIL(this);

    // When we terminate the process, it's handle will become signaled and
    // Win32 Event Thread will leap into action and call CordbWin32EventThread::ExitProcess
    // Unfortunately, that may destroy this object if the ExitProcess callback
    // decides to call Release() on the process.


    // Indicate that the process is exiting so that (among other things) we don't try and
    // send messages to the left side while it's being deleted.
    Lock();

    // In case we're continuing from the loader bp, we don't want to try and kick off an attach. :)
    m_fDoDelayedManagedAttached = false;
    m_exiting = true;



    // We'd like to just take a lock around everything here, but that may deadlock us
    // since W32ET will wait on the lock, and Continue may wait on W32ET.
    // So we just do an extra AddRef/Release to make sure we're still around.
    // @todo - could we move this smartptr up so that it's well-nested w/ the lock?
    RSSmartPtr<CordbProcess> pRef(this);

    Unlock();


    // At any point after this call, the w32 ET may run the ExitProcess code which will race w/ the continue call.
    // This call only posts a request that the process terminate and does not guarantee the process actually
    // terminates. In particular, the process can not exit until any outstanding IO requests are done (on cancelled).
    // It also can not exit if we have an outstanding not-continued native-debug event.
    // Fortunately, the interesting work in terminate is done in ExitProcessWorkItem::Do, which can take the Stop-Go lock.
    // Since we're currently holding the stop-go lock, that means we at least get some serialization.
    //
    // Note that on Windows, the process isn't really terminated until we receive the EXIT_PROCESS_DEBUG_EVENT.
    // Before then, we can still still access the debuggee's address space.  On the other, for Mac debugging,
    // the process can die any time after this call, and so we can no longer call into the DAC.
    GetShim()->GetNativePipeline()->TerminateProcess(exitCode);

    // We just call Continue() so that the debugger doesn't have to. (It's arguably odd
    // to call Continue() after Terminate).
    // We're stopped & Synced.
    // For interop-debugging this is very important because the Terminate may not really kill the process
    // until after we continue from the current native debug event.
    ContinueInternal(FALSE);

    // Implicit release on pRef here (since it's going out of scope)...
    // After this release, this object may be destroyed. So don't use any member functions
    // (including Locks) after here.


    return S_OK;
}

// This can be called at any time, even if we're in an unrecoverable error state.
HRESULT CordbProcess::GetID(DWORD *pdwProcessId)
{
    PUBLIC_REENTRANT_API_ENTRY(this);
    OK_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT(pdwProcessId, DWORD *);

    HRESULT hr = S_OK;
    EX_TRY
    {
        // This shouldn't be used in V3 paths. Normally, we can enforce that by checking against
        // m_pShim. However, this API can be called after being neutered, in which case m_pShim is cleared.
        // So check against 0 instead.
        if (m_id == 0)
        {
            *pdwProcessId = 0;
            ThrowHR(E_NOTIMPL);
        }
        *pdwProcessId = GetProcessDescriptor()->m_Pid;
    }
    EX_CATCH_HRESULT(hr);
    return hr;
}

// Helper to get process descriptor internally. We know we'll always succeed.
// This is more convient for internal callers since they can just use it as an expression
// without having to check HRESULTS.
const ProcessDescriptor* CordbProcess::GetProcessDescriptor()
{
    // This shouldn't be used in V3 paths, in which case it's set to 0. Only the shim should be
    // calling this. Assert to catch anybody else.
    _ASSERTE(m_processDescriptor.IsInitialized());

    return &m_processDescriptor;
}


HRESULT CordbProcess::GetHandle(HANDLE *phProcessHandle)
{
    PUBLIC_REENTRANT_API_ENTRY(this);
    FAIL_IF_NEUTERED(this); // Once we neuter the process, we close our OS handle to it.
    VALIDATE_POINTER_TO_OBJECT(phProcessHandle, HANDLE *);

    if (m_pShim == NULL)
    {
        _ASSERTE(!"CordbProcess::GetHandle() should be not be called on the new architecture");
        *phProcessHandle = NULL;
        return E_NOTIMPL;
    }
    else
    {
        *phProcessHandle = m_handle;
        return S_OK;
    }
}

HRESULT CordbProcess::IsRunning(BOOL *pbRunning)
{
    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT(pbRunning, BOOL*);

    *pbRunning = !GetSynchronized();

    return S_OK;
}

HRESULT CordbProcess::EnableSynchronization(BOOL bEnableSynchronization)
{
    /* !!! */
    PUBLIC_API_ENTRY(this);
    return E_NOTIMPL;
}

HRESULT CordbProcess::Stop(DWORD dwTimeout)
{
    PUBLIC_API_ENTRY(this);
    CORDBRequireProcessStateOK(this);

    HRESULT hr = StopInternal(dwTimeout, VMPTR_AppDomain::NullPtr());

    return ErrWrapper(hr);
}

HRESULT CordbProcess::StopInternal(DWORD dwTimeout, VMPTR_AppDomain pAppDomainToken)
{
    LOG((LF_CORDB, LL_INFO1000, "CP::S: stopping process 0x%x(%d) with timeout %d\n", m_id, m_id,  dwTimeout));

    INTERNAL_API_ENTRY(this);

    // Stop + Continue are executed under the Stop-Go lock. This makes them atomic.
    // We'll toggle the process-lock (b/c we communicate w/ the W32et, so just the process-lock is
    // not sufficient to make this atomic).
    // It's ok to take this lock before checking if the CordbProcess has been neutered because
    // the lock is destroyed in the dtor after neutering.
    RSLockHolder ch(&m_StopGoLock);

    // Check if this CordbProcess has been neutered under the SG lock.
    // Otherwise it's possible to race with Detach() and Terminate().
    FAIL_IF_NEUTERED(this);
    CORDBFailIfOnWin32EventThread(this);

    if (m_pShim == NULL) // Stop/Go is moved off to the shim
    {
        return E_NOTIMPL;
    }


    DebuggerIPCEvent* event;
    HRESULT hr = S_OK;

    STRESS_LOG2(LF_CORDB, LL_INFO1000, "CP::SI, timeout=%d, this=%p\n", dwTimeout, this);

    // Stop() is a syncronous (blocking) operation. Furthermore, we have no way to cancel the async-break request.
    // Thus if we returned early on a timeout, then we'll be in a random state b/c the LS may get stopped at any
    // later spot.
    // One solution just require the param is INFINITE until we fix this and E_INVALIDARG if it's not.
    // But that could be a breaking change (what if a debugger passes in a really large value that's effectively
    // INFINITE).
    // So we'll just ignore it and always treat it as infinite.
    dwTimeout = INFINITE;

    // Do the checks on the process state under the SG lock.  This ensures that another thread cannot come in
    // after we do the checks and take the lock before we do.  For example, Detach() can race with Stop() such
    // that:
    //      1. Thread A calls CordbProcess::Detach() and takes the stop-go lock
    //      2. Thread B calls CordbProcess::Stop(), passes all the checks, and then blocks on the stop-go lock
    //      3. Thread A finishes the detach, invalides the process state, cleans all the resources, and then
    //         releases the stop-go lock
    //      4. Thread B gets the lock, but everything has changed
    CORDBRequireProcessStateOK(this);

    Lock();

    ASSERT_SINGLE_THREAD_ONLY(HoldsLock(&m_StopGoLock));

    // Don't need to stop if the process hasn't even executed any managed code yet.
    if (!m_initialized)
    {
        LOG((LF_CORDB, LL_INFO1000, "CP::S: process isn't initialized yet.\n"));

        // Mark the process as synchronized so no events will be dispatched until the thing is continued.
        SetSynchronized(true);

        // Remember uninitialized stop...
        m_uninitializedStop = true;

#ifdef FEATURE_INTEROP_DEBUGGING
        // If we're Win32 attached, then suspend all the unmanaged threads in the process.
        // We may or may not be stopped at a native debug event.
        if (IsInteropDebugging())
        {
            SuspendUnmanagedThreads();
        }
#endif // FEATURE_INTEROP_DEBUGGING

        // Get the RC Event Thread to stop listening to the process.
        m_cordb->ProcessStateChanged();

        hr = S_OK;
        goto Exit;
    }

    // Don't need to stop if the process is already synchronized.
    // @todo - Issue 129917. It's possible that we'll get a call to Stop when the LS is already stopped.
    // Sending an AsyncBreak would deadlock here (b/c the LS will ignore the frivilous request,
    // and thus never send a SyncComplete, and thus our Waiting on the SyncComplete will deadlock).
    // We avoid this case by checking m_syncCompleteReceived (which should roughly correspond to
    // the LS's m_stopped variable).
    // One window this can happen is after a Continue() pings the RCET but before the RCET actually sweeps + flushes.

    if (GetSynchronized() || GetSyncCompleteRecv())
    {
        LOG((LF_CORDB, LL_INFO1000, "CP::S: process was already synchronized. m_syncCompleteReceived=%d\n", GetSyncCompleteRecv()));

        if (GetSyncCompleteRecv())
        {
            // We must be in that window alluded to above (while the RCET is sweeping). Re-ping the RCET.
            SetSynchronized(true);
            m_cordb->ProcessStateChanged();
        }
        hr = S_OK;
        goto Exit;
    }

    STRESS_LOG0(LF_CORDB, LL_INFO1000, "CP::S: process not sync'd, requesting stop.\n");

    m_stopRequested = true;

    // We don't want to dispatch any Win32 debug events while we're trying to stop.
    // Setting m_specialDeferment=true means that any debug event we get will be queued and not dispatched.
    // We do this to avoid a nested call to Continue.
    // These defered events will get dispatched when somebody calls continue (and since they're calling
    // stop now, they must call continue eventually).
    // Note that if we got a Win32 debug event between when we took the Stop-Go lock above and now,
    // that even may have been dispatched. We're ok because SSFW32Stop will hijack that event and continue it,
    // and then all future events will be queued.
    m_specialDeferment = true;
    Unlock();

    BOOL asyncBreakSent;

    // We need to ensure that the helper thread is alive.
    hr = this->StartSyncFromWin32Stop(&asyncBreakSent);
    if (FAILED(hr))
    {
        return hr;
    }


    if (asyncBreakSent)
    {
        hr = S_OK;
        Lock();

        m_stopRequested = false;

        goto Exit;
    }

    // Send the async break event to the RC.
    event = (DebuggerIPCEvent*) _alloca(CorDBIPC_BUFFER_SIZE);
    InitIPCEvent(event, DB_IPCE_ASYNC_BREAK, false, pAppDomainToken);

    STRESS_LOG1(LF_CORDB, LL_INFO1000, "CP::S: sending async stop to appd 0x%x.\n", VmPtrToCookie(pAppDomainToken));

    hr = m_cordb->SendIPCEvent(this, event, CorDBIPC_BUFFER_SIZE);
    hr = WORST_HR(hr, event->hr);
    if (FAILED(hr))
    {
        // We don't hold the lock so just return immediately. Don't adjust stop-count.
        _ASSERTE(!ThreadHoldsProcessLock());
        return hr;
    }

    LOG((LF_CORDB, LL_INFO1000, "CP::S: sent async stop to appd 0x%x.\n", VmPtrToCookie(pAppDomainToken)));

    // Wait for the sync complete message to come in. Note: when the sync complete message arrives to the RCEventThread,
    // it will mark the process as synchronized and _not_ dispatch any events. Instead, it will set m_stopWaitEvent
    // which will let this function return. If the user wants to process any queued events, they will need to call
    // Continue.
    STRESS_LOG0(LF_CORDB, LL_INFO1000, "CP::S: waiting for event.\n");

    DWORD ret;
    ret = SafeWaitForSingleObject(this, m_stopWaitEvent, dwTimeout);

    STRESS_LOG1(LF_CORDB, LL_INFO1000, "CP::S: got event, %d.\n", ret);

    if (m_terminated)
    {
        return CORDBG_E_PROCESS_TERMINATED;
    }

    if (ret == WAIT_OBJECT_0)
    {
        LOG((LF_CORDB, LL_INFO1000, "CP::S: process stopped.\n"));

        m_stopRequested = false;
        m_cordb->ProcessStateChanged();

        hr = S_OK;
        Lock();
        goto Exit;
    }
    else if (ret == WAIT_TIMEOUT)
    {
        hr = ErrWrapper(CORDBG_E_TIMEOUT);
    }
    else
        hr = HRESULT_FROM_GetLastError();

    // We came out of the wait, but we weren't signaled because a sync complete event came in. Re-check the process and
    // remove the stop requested flag.
    Lock();
    m_stopRequested = false;

    if (GetSynchronized())
    {
        LOG((LF_CORDB, LL_INFO1000, "CP::S: process stopped.\n"));

        m_cordb->ProcessStateChanged();

        hr = S_OK;
    }

Exit:
    _ASSERTE(ThreadHoldsProcessLock());

    // Stop queuing any Win32 Debug events. We should be synchronized now.
    m_specialDeferment = false;

    if (SUCCEEDED(hr))
    {
        IncStopCount();
    }

    STRESS_LOG2(LF_CORDB, LL_INFO1000, "CP::S: returning from Stop, hr=0x%08x, m_stopCount=%d.\n", hr, GetStopCount());

    Unlock();

    return hr;
}

//---------------------------------------------------------------------------------------
// Clear all RS state on all CordbThread objects.
//
// Notes:
//   This clears all the thread-related state that the RS may have cached,
//   such as locals, frames, etc.
//   This would be called if the debugger is resuming execution.
void CordbProcess::MarkAllThreadsDirty()
{
    INTERNAL_API_ENTRY(this);
    _ASSERTE(ThreadHoldsProcessLock());

    CordbThread * pThread;
    HASHFIND find;

    // We don't need to prepopulate here (to collect LS state) because we're just updating RS state.
    for (pThread =  m_userThreads.FindFirst(&find);
         pThread != NULL;
         pThread =  m_userThreads.FindNext(&find))
    {
        _ASSERTE(pThread != NULL);
        pThread->MarkStackFramesDirty();
    }

    ClearPatchTable();
}

HRESULT CordbProcess::Continue(BOOL fIsOutOfBand)
{
    PUBLIC_API_ENTRY(this);

    if (m_pShim == NULL) // This API is moved off to the shim
    {
        // bias towards failing with CORDBG_E_NUETERED.
        FAIL_IF_NEUTERED(this);
        return E_NOTIMPL;
    }

    HRESULT hr;

    if (fIsOutOfBand)
    {
#ifdef FEATURE_INTEROP_DEBUGGING
        hr = ContinueOOB();
#else
        hr = E_INVALIDARG;
#endif // FEATURE_INTEROP_DEBUGGING
    }
    else
    {
        hr = ContinueInternal(fIsOutOfBand);
    }

    return hr;
}

#ifdef FEATURE_INTEROP_DEBUGGING
//---------------------------------------------------------------------------------------
//
// ContinueOOB
//
// Continue the Win32 event as an out-of-band event.
//
// Return Value:
//    S_OK on successful continue. Else error.
//
//---------------------------------------------------------------------------------------
HRESULT CordbProcess::ContinueOOB()
{
    INTERNAL_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);

    HRESULT hr = S_OK;

    // If we're continuing from an out-of-band unmanaged event, then just go
    // ahead and get the Win32 event thread to continue the process. No other
    // work needs to be done (i.e., don't need to send a managed continue message
    // or dispatch any events) because any processing done due to the out-of-band
    // message can't alter the synchronized state of the process.

    Lock();
    _ASSERTE(m_outOfBandEventQueue != NULL);

    // Are we calling this from the unmanaged callback?
    if (m_dispatchingOOBEvent)
    {
        STRESS_LOG0(LF_CORDB, LL_INFO1000, "CP::CI: continue while dispatching unmanaged out-of-band event.\n");
        // We don't know what thread we're on here.

        // Tell the Win32 event thread to continue when it returns from handling its unmanaged callback.
        m_dispatchingOOBEvent = false;

        Unlock();
    }
    else
    {
        STRESS_LOG0(LF_CORDB, LL_INFO1000, "CP::CI: continue outside of dispatching.\n");

        // If we're not dispatching this, then they shouldn't be on the win32 event thread.
        _ASSERTE(!this->IsWin32EventThread());

        Unlock();

        // Send an event to the Win32 event thread to do the continue. This is an out-of-band continue.
        hr = this->m_pShim->GetWin32EventThread()->SendUnmanagedContinue(this, cOobUMContinue);
    }

    return hr;


}
#endif // FEATURE_INTEROP_DEBUGGING

//---------------------------------------------------------------------------------------
//
// ContinueInternal
//
// Continue the Win32 event.
//
// Return Value:
//    S_OK on success. Else error.
//
//---------------------------------------------------------------------------------------
HRESULT CordbProcess::ContinueInternal(BOOL fIsOutOfBand)
{
    INTERNAL_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);

    // Continue has an ATT similar to ATT_REQUIRE_STOPPED_MAY_FAIL, but w/ some subtle differences.
    // - if we're stopped at a native DE, but not synchronized, we don't want to sync.
    // - We may get Debug events (especially native ones) at weird times, and thus we have to continue
    // at weird times.

    // External APIs should not have the process lock.
    _ASSERTE(!ThreadHoldsProcessLock());
    _ASSERTE(m_pShim != NULL);

    // OutOfBand should use ContinueOOB
    _ASSERTE(!fIsOutOfBand);

    // Since Continue is process-wide, just use a null appdomain pointer.
    VMPTR_AppDomain pAppDomainToken = VMPTR_AppDomain::NullPtr();

    HRESULT hr = S_OK;

    if (m_unrecoverableError)
    {
        return CORDBHRFromProcessState(this, NULL);
    }


    // We can't call ContinueInternal for an inband event on the win32 event thread.
    // This is an issue in the CLR (or an API design decision, depending on your perspective).
    // Continue() may send an IPC event and we can't do that on the win32 event thread.

    CORDBFailIfOnWin32EventThread(this);

    STRESS_LOG1(LF_CORDB, LL_INFO1000, "CP::CI: continuing IB,  this=0x%X\n", this);

    // Stop + Continue are executed under the Stop-Go lock. This makes them atomic.
    // We'll toggle the process-lock (b/c we communicate w/ the W32et, so that's not sufficient).
    RSLockHolder rsLockHolder(&m_StopGoLock);

    // Check for other failures (do these after we have the SG lock).
    if (m_terminated)
    {
        return CORDBG_E_PROCESS_TERMINATED;
    }
    if (m_detached)
    {
        return CORDBG_E_PROCESS_DETACHED;
    }

    Lock();

    ASSERT_SINGLE_THREAD_ONLY(HoldsLock(&m_StopGoLock));
    _ASSERTE(fIsOutOfBand == FALSE);

    // If we've got multiple Stop calls, we need a Continue for each one. So, if the stop count > 1, just go ahead and
    // return without doing anything. Note: this is only for in-band or managed events. OOB events are still handled as
    // normal above.
    _ASSERTE(GetStopCount() > 0);

    if (GetStopCount() == 0)
    {
        Unlock();
        _ASSERTE(!"Superflous Continue. ICorDebugProcess.Continue() called too many times");
        return CORDBG_E_SUPERFLOUS_CONTINUE;
    }

    DecStopCount();

    // We give managed events priority over unmanaged events. That way, the entire queued managed state can drain before
    // we let any other unmanaged events through.

    // Every stop or event must be matched by a corresponding Continue. m_stopCount counts outstanding stopping events
    // along with calls to Stop. If the count is high at this point, we simply return. This ensures that even if someone
    // calls Stop just as they're receiving an event that they can call Continue for that Stop and for that event
    // without problems.
    if (GetStopCount() > 0)
    {
        STRESS_LOG1(LF_CORDB, LL_INFO1000, "CP::CI: m_stopCount=%d, Continue just returning S_OK...\n", GetStopCount());

        Unlock();
        return S_OK;
    }

    // We're no longer stopped, so reset the m_stopWaitEvent.
    ResetEvent(m_stopWaitEvent);

    // If we're continuing from an uninitialized stop, then we don't need to do much at all. No event need be sent to
    // the Left Side (duh, it isn't even there yet.) We just need to get the RC Event Thread to start listening to the
    // process again, and resume any unmanaged threads if necessary.
    if (m_uninitializedStop)
    {
        STRESS_LOG0(LF_CORDB, LL_INFO1000, "CP::CI: continuing from uninitialized stop.\n");

        // No longer synchronized (it was a partial sync in the first place.)
        SetSynchronized(false);
        MarkAllThreadsDirty();

        // No longer in an uninitialized stop.
        m_uninitializedStop = false;

        // Notify the RC Event Thread.
        m_cordb->ProcessStateChanged();

        Unlock();

#ifdef FEATURE_INTEROP_DEBUGGING
        // We may or may not have a native debug event queued here.
        // If Cordbg called Stop() from a native debug event (to get the process Synchronized), then
        // we'll have a native debug event, and we need to continue it.
        // If Cordbg called Stop() to do an AsyncBreak, then there's no native-debug event.

        // If we're Win32 attached, resume all the unmanaged threads.
        if (IsInteropDebugging())
        {
            if(m_lastDispatchedIBEvent != NULL)
            {
                m_lastDispatchedIBEvent->SetState(CUES_UserContinued);
            }

            // Send to the Win32 event thread to do the unmanaged continue for us.
            // If we're at a debug event, this will continue it.
            // Else it will degenerate into ResumeUnmanagedThreads();
            this->m_pShim->GetWin32EventThread()->SendUnmanagedContinue(this, cRealUMContinue);
        }
#endif // FEATURE_INTEROP_DEBUGGING


        return S_OK;
    }

    // If there are more managed events, get them dispatched now.
    if (!m_pShim->GetManagedEventQueue()->IsEmpty() && GetSynchronized())
    {
        STRESS_LOG0(LF_CORDB, LL_INFO1000, "CP::CI: managed event queued.\n");

        // Mark that we're not synchronized anymore.
        SetSynchronized(false);

        // If the callback queue is not empty, then the LS is not actually continuing, and so our cached
        // state is still valid.

        // If we're in the middle of dispatching a managed event, then simply return. This indicates to HandleRCEvent
        // that the user called Continue and HandleRCEvent will dispatch the next queued event. But if Continue was
        // called outside the managed callback, all we have to do is tell the RC event thread that something about the
        // process has changed and it will dispatch the next managed event.
        if (!AreDispatchingEvent())
        {
            STRESS_LOG0(LF_CORDB, LL_INFO1000, "CP::CI: continuing while not dispatching managed event.\n");

            m_cordb->ProcessStateChanged();
        }

        Unlock();
        return S_OK;
    }

    // Neuter if we have an outstanding object.
    // Only do this if we're really continuining the debuggee. So don't do this if our stop-count is high b/c we
    // shouldn't neuter until we're done w/ the current event. And don't do this until we drain the current callback queue.
    // Note that we can't hold the process lock while we do this b/c Neutering may send IPC events.
    // However, we're still under the StopGo lock b/c that may help us serialize things.

    // Sweep neuter list. This will catch anything that's marked as 'safe to neuter'. This includes
    // all objects added to the 'neuter-on-Continue'.
    // Only do this if we're synced- we don't want to do this if we're continuing from a Native Debug event.
    if (GetSynchronized())
    {
        // Need process-lock to operate on hashtable, but can't yet Neuter under process-lock,
        // so we have to copy the contents to an auxilary list which we can then traverse outside the lock.
        RSPtrArray<CordbAppDomain> listAppDomains;
        HRESULT hrCopy = S_OK;
        EX_TRY // @dbgtodo cleanup: push this up
        {
            m_appDomains.CopyToArray(&listAppDomains);
        }
        EX_CATCH_HRESULT(hrCopy);
        SetUnrecoverableIfFailed(GetProcess(), hrCopy);

        m_ContinueNeuterList.NeuterAndClear(this);

        // @dbgtodo left-side resources: eventually (once
        // NeuterLeftSideResources is process-lock safe), do this all under the
        // lock. Can't hold process lock b/c neutering left-side resources
        // may send events.
        Unlock();

        // This may send IPC events.
        // This will make normal neutering a nop.
        // This will toggle the process lock.
        m_LeftSideResourceCleanupList.SweepNeuterLeftSideResources(this);


        // Many objects (especially CordbValue, FuncEval) don't have clear lifetime semantics and
        // so they must be put into an exit-neuter list (Process/AppDomain) for worst-case scenarios.
        // These objects are likely released early, and so we sweep them aggressively on each Continue (kind of like a mini-GC).
        //
        // One drawback is that there may be a lot of useless sweeping if the debugger creates a lot of
        // objects that it holds onto. Consider instead of sweeping, have the object explicitly post itself
        // to a list that's guaranteed to be cleared. This would let us avoid sweeping not-yet-ready objects.
        // This will toggle the process lock
        m_ExitNeuterList.SweepAllNeuterAtWillObjects(this);


        for(unsigned int idx = 0; idx < listAppDomains.Length(); idx++)
        {
            CordbAppDomain * pAppDomain = listAppDomains[idx];

            // CordbHandleValue is in the appdomain exit list, and that needs
            // to send an IPC event to cleanup and release the handle from
            // the GCs handle table.
            // This will toggle the process lock.
            pAppDomain->GetSweepableExitNeuterList()->SweepNeuterLeftSideResources(this);
        }
        listAppDomains.Clear();

        Lock();
    }


    // At this point, if the managed event queue is empty, m_synchronized may still be true if we had previously
    // synchronized.

#ifdef FEATURE_INTEROP_DEBUGGING
    // Next, check for unmanaged events that may be queued. If there are some queued, then we need to get the Win32
    // event thread to go ahead and dispatch the next one. If there aren't any queued, then we can just fall through and
    // send the continue message to the left side. This works even if we have an outstanding ownership request, because
    // until that answer is received, its just like the event hasn't happened yet.
    //
    // If we're terminated, then we've already continued from the last win32 event and so don't continue.
    // @todo - or we could ensure the PS_SOME_THREADS_SUSPENDED | PS_HIJACKS_IN_PLACE are removed.
    // Either way, we're just protecting against exit-process at strange times.
    bool fDoWin32Continue = !m_terminated && ((m_state & (PS_WIN32_STOPPED | PS_SOME_THREADS_SUSPENDED | PS_HIJACKS_IN_PLACE)) != 0);

    // We need to store this before marking the event user continued below
    BOOL fHasUserUncontinuedEvents = HasUserUncontinuedNativeEvents();

    if(m_lastDispatchedIBEvent != NULL)
    {
        m_lastDispatchedIBEvent->SetState(CUES_UserContinued);
    }

    if (fHasUserUncontinuedEvents)
    {
        // ExitProcess is the last debug event we'll get. The Process Handle is not signaled until
        // after we continue from ExitProcess. m_terminated is only set once we know the process is signaled.
        // (This isn't 100% true for the detach case, but since you can't do interop detach, we don't care)
        //_ASSERTE(!m_terminated);

        STRESS_LOG1(LF_CORDB, LL_INFO1000, "CP::CI: there are queued uncontinued events. m_dispatchingUnmanagedEvent = %d\n", m_dispatchingUnmanagedEvent);

        // Are we being called while in the unmanaged event callback?
        if (m_dispatchingUnmanagedEvent)
        {
            LOG((LF_CORDB, LL_INFO1000, "CP::CI: continue while dispatching.\n"));
            // The Win32ET could have made a cross-thread call to Continue while dispatching,
            // so we don't know if this is the win32 ET.

            // Tell the Win32 thread to continue when it returns from handling its unmanaged callback.
            m_dispatchingUnmanagedEvent = false;

            // If there are no more unmanaged events, then we fall through and continue the process for real. Otherwise,
            // we can simply return.
            if (HasUndispatchedNativeEvents())
            {
                STRESS_LOG0(LF_CORDB, LL_INFO1000, "CP::CI: more unmanaged events need dispatching.\n");

                // Note: if we tried to access the Left Side while stopped but couldn't, then m_oddSync will be true. We
                // need to reset it to false since we're continuing now.
                m_oddSync = false;

                Unlock();
                return S_OK;
            }
            else
            {
                // Also, if there are no more unmanaged events, then when DispatchUnmanagedInBandEvent sees that
                // m_dispatchingUnmanagedEvent is false, it will continue the process. So we set doWin32Continue to
                // false here so that we don't try to double continue the process below.
                STRESS_LOG0(LF_CORDB, LL_INFO1000, "CP::CI: no more unmanaged events to dispatch.\n");

                fDoWin32Continue = false;
            }
        }
        else
        {
            // after the DebugEvent callback returned the continue still had no been issued. Then later
            // on another thread the user called back to continue the event, which gets us to right here
            LOG((LF_CORDB, LL_INFO1000, "CP::CI: continue outside of dispatching.\n"));

            // This should be the common place to Dispatch an IB event that was hijacked for sync.

            // If we're not dispatching, this better not be the win32 event thread.
            _ASSERTE(!IsWin32EventThread());

            // If the event at the head of the queue is really the last event, or if the event at the head of the queue
            // hasn't been dispatched yet, then we simply fall through and continue the process for real. However, if
            // its not the last event, we send to the Win32 event thread and get it to continue, then we return.
            if (HasUndispatchedNativeEvents())
            {
                STRESS_LOG0(LF_CORDB, LL_INFO1000, "CP::CI: more unmanaged events need dispatching.\n");

                // Note: if we tried to access the Left Side while stopped but couldn't, then m_oddSync will be true. We
                // need to reset it to false since we're continuing now.
                m_oddSync = false;

                Unlock();

                hr = this->m_pShim->GetWin32EventThread()->SendUnmanagedContinue(this, cRealUMContinue);

                return hr;
            }
        }
    }
#endif // FEATURE_INTEROP_DEBUGGING

    // Both the managed and unmanaged event queues are now empty. Go
    // ahead and continue the process for real.
    LOG((LF_CORDB, LL_INFO1000, "CP::CI: headed for true continue.\n"));

    // We need to check these while under the lock, but action must be
    // taked outside of the lock.
    bool fIsExiting = m_exiting;
    bool fWasSynchronized = GetSynchronized();

    // Mark that we're no longer synchronized.
    if (fWasSynchronized)
    {
        LOG((LF_CORDB, LL_INFO1000, "CP::CI: process was synchronized.\n"));

        SetSynchronized(false);
        SetSyncCompleteRecv(false);

        // we're no longer in a callback, so set flags to indicate that we've finished.
        GetShim()->NotifyOnContinue();

        // Flush will update state, including continue counter and marking
        // frames dirty.
        this->FlushProcessRunning();


        // Tell the RC event thread that something about this process has changed.
        m_cordb->ProcessStateChanged();
    }

    m_continueCounter++;

    // If m_oddSync is set, then out last synchronization was due to us syncing the process because we were Win32
    // stopped. Therefore, while we do need to do most of the work to continue the process below, we don't actually have
    // to send the managed continue event. Setting wasSynchronized to false here helps us do that.
    if (m_oddSync)
    {
        fWasSynchronized = false;
        m_oddSync = false;
    }

#ifdef FEATURE_INTEROP_DEBUGGING
    // We must ensure that all managed threads are suspended here. We're about to let all managed threads run free via
    // the managed continue message to the Left Side. If we don't suspend the managed threads, then they may start
    // slipping forward even if we receive an in-band unmanaged event. We have to hijack in-band unmanaged events while
    // getting the managed continue message over to the Left Side to keep the process running free. Otherwise, the
    // SendIPCEvent will hang below. But in doing so, we could let managed threads slip to far. So we ensure they're all
    // suspended here.
    //
    // Note: we only do this suspension if the helper thread hasn't died yet. If the helper thread has died, then we
    // know that we're loosing the Runtime. No more managed code is going to run, so we don't bother trying to prevent
    // managed threads from slipping via the call below.
    //
    // Note: we just remember here, under the lock, so we can unlock then wait for the syncing thread to free the
    // debugger lock. Otherwise, we may block here and prevent someone from continuing from an OOB event, which also
    // prevents the syncing thread from releasing the debugger lock like we want it to.
    bool fNeedSuspend = fWasSynchronized && fDoWin32Continue && !m_helperThreadDead;

    // If we receive a new in-band event once we unlock, we need to know to hijack it and keep going while we're still
    // trying to send the managed continue event to the process.
    if (fWasSynchronized && fDoWin32Continue && !fIsExiting)
    {
        m_specialDeferment = true;
    }

    if (fNeedSuspend)
    {
        // @todo - what does this actually accomplish? We already suspended everything when we first synced.

        // Any thread that may hold a lock blocking the helper is
        // inside of a can't stop region, and thus we won't suspend it.
        SuspendUnmanagedThreads();
    }
#endif // FEATURE_INTEROP_DEBUGGING

    Unlock();

    // Although we've released the Process-lock, we still have the Stop-Go lock.
    _ASSERTE(m_StopGoLock.HasLock());

    // If we're processing an ExitProcess managed event, then we don't want to really continue the process, so just fall
    // thru.  Note: we did let the unmanaged continue go through above for this case.
    if (fIsExiting)
    {
        LOG((LF_CORDB, LL_INFO1000, "CP::CI: continuing from exit case.\n"));
    }
    else if (fWasSynchronized)
    {
        LOG((LF_CORDB, LL_INFO1000, "CP::CI: Sending continue to AppD:0x%x.\n", VmPtrToCookie(pAppDomainToken)));
#ifdef FEATURE_INTEROP_DEBUGGING
        STRESS_LOG2(LF_CORDB, LL_INFO1000, "Continue flags:special=%d, dowin32=%d\n", m_specialDeferment, fDoWin32Continue);
#endif
        // Send to the RC to continue the process.
        DebuggerIPCEvent * pEvent = (DebuggerIPCEvent *) _alloca(CorDBIPC_BUFFER_SIZE);

        InitIPCEvent(pEvent, DB_IPCE_CONTINUE, false, pAppDomainToken);

        hr = m_cordb->SendIPCEvent(this, pEvent, CorDBIPC_BUFFER_SIZE);

        // It is possible that we continue and then the process immediately exits before the helper
        // thread is finished continuing and can report success back to us. That's arguably a success
        // case sinceu the process did indeed continue, but since we didn't get the acknowledgement,
        // we can't be sure it's success. So we call it S_FALSE instead of S_OK.
        // @todo - how do we handle other failure here?
        if (hr == CORDBG_E_PROCESS_TERMINATED)
        {
            hr = S_FALSE;
        }
        _ASSERTE(SUCCEEDED(pEvent->hr));

        LOG((LF_CORDB, LL_INFO1000, "CP::CI: Continue sent to AppD:0x%x.\n", VmPtrToCookie(pAppDomainToken)));
    }

#ifdef FEATURE_INTEROP_DEBUGGING
    // If we're win32 attached to the Left side, then we need to win32 continue the process too (unless, of course, it's
    // already been done above.)
    //
    // Note: we do this here because we want to get the Left Side to receive and ack our continue message above if we
    // were sync'd. If we were sync'd, then by definition the process (and the helper thread) is running anyway, so all
    // this continue is going to do is to let the threads that have been suspended go.
    if (fDoWin32Continue)
    {
#ifdef _DEBUG
        {
            // A little pause here extends the special deferment region and thus causes native-debug
            // events to get hijacked. This test some wildly different corner case paths.
            // See VSWhidbey bugs 131905, 168971
            static DWORD dwRace = -1;
            if (dwRace == -1)
                dwRace = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_DbgRace);

            if ((dwRace & 1) == 1)
            {
                Sleep(30);
            }
        }
#endif

        STRESS_LOG0(LF_CORDB, LL_INFO1000, "CP::CI: sending unmanaged continue.\n");

        // Send to the Win32 event thread to do the unmanaged continue for us.
        hr = this->m_pShim->GetWin32EventThread()->SendUnmanagedContinue(this, cRealUMContinue);
    }
#endif // FEATURE_INTEROP_DEBUGGING

    STRESS_LOG0(LF_CORDB, LL_INFO1000, "CP::CI: continue done, returning.\n");

    return hr;
}

HRESULT CordbProcess::HasQueuedCallbacks(ICorDebugThread *pThread,
                                         BOOL *pbQueued)
{
    PUBLIC_REENTRANT_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT_OR_NULL(pThread,ICorDebugThread *);
    VALIDATE_POINTER_TO_OBJECT(pbQueued,BOOL *);

    // Shim owns the event queue
    if (m_pShim != NULL)
    {
        PUBLIC_CALLBACK_IN_THIS_SCOPE0_NO_LOCK(this); // Calling to shim, leaving RS.
        *pbQueued = m_pShim->GetManagedEventQueue()->HasQueuedCallbacks(pThread);
        return S_OK;
    }
    return E_NOTIMPL; // Not implemented in V3.
}

//
// A small helper function to convert a CordbBreakpoint to an ICorDebugBreakpoint based on its type.
//
static ICorDebugBreakpoint *CordbBreakpointToInterface(CordbBreakpoint * pBreakpoint)
{
    _ASSERTE(pBreakpoint != NULL);

    //
    // I really dislike this. We've got three subclasses of CordbBreakpoint, but we store them all into the same hash
    // (m_breakpoints), so when we get one out of the hash, we don't really know what type it is. But we need to know
    // what type it is because we need to cast it to the proper interface before passing it out. I.e., when we create a
    // function breakpoint, we return the breakpoint casted to an ICorDebugFunctionBreakpoint. But if we grab that same
    // breakpoint out of the hash as a CordbBreakpoint and pass it out as an ICorDebugBreakpoint, then that's a
    // different pointer, and its wrong. So I've added the type to the breakpoint so we can cast properly here. I'd love
    // to do this a different way, though...
    //
    // -- Mon Dec 14 21:06:46 1998
    //
    switch(pBreakpoint->GetBPType())
    {
    case CBT_FUNCTION:
        return static_cast<ICorDebugFunctionBreakpoint *>(static_cast<CordbFunctionBreakpoint *> (pBreakpoint));
        break;

    case CBT_MODULE:
        return static_cast<ICorDebugModuleBreakpoint*>(static_cast<CordbModuleBreakpoint *> (pBreakpoint));
        break;

    case CBT_VALUE:
        return static_cast<ICorDebugValueBreakpoint *>(static_cast<CordbValueBreakpoint *> (pBreakpoint));
        break;

    default:
        _ASSERTE(!"Invalid breakpoint type!");
    }

    return NULL;
}


// Callback data for code:CordbProcess::GetAssembliesInLoadOrder
class ShimAssemblyCallbackData
{
public:
    // Ctor to initialize callback data
    //
    // Arguments:
    //   pAppDomain - appdomain that the assemblies are in.
    //   pAssemblies - preallocated array of smart pointers to hold assemblies
    //   countAssemblies - size of pAssemblies in elements.
    ShimAssemblyCallbackData(
        CordbAppDomain * pAppDomain,
        RSExtSmartPtr<ICorDebugAssembly>* pAssemblies,
        ULONG countAssemblies)
    {
        _ASSERTE(pAppDomain != NULL);
        _ASSERTE(pAssemblies != NULL);

        m_pProcess = pAppDomain->GetProcess();
        m_pAppDomain = pAppDomain;
        m_pAssemblies = pAssemblies;
        m_countElements = countAssemblies;
        m_index = 0;

        // Just to be safe, clear them all out
        for(ULONG i = 0; i < countAssemblies; i++)
        {
            pAssemblies[i].Clear();
        }
    }

    // Dtor
    //
    // Notes:
    //   This can assert end-of-enumeration invariants.
    ~ShimAssemblyCallbackData()
    {
        // Ensure that we went through all assemblies.
        _ASSERTE(m_index == m_countElements);
    }

    // Callback invoked from DAC enumeration.
    //
    // arguments:
    //    vmDomainAssembly - VMPTR for assembly
    //    pData - a 'this' pointer
    //
    static void Callback(VMPTR_DomainAssembly vmDomainAssembly, void * pData)
    {
        ShimAssemblyCallbackData * pThis = static_cast<ShimAssemblyCallbackData *> (pData);
        INTERNAL_DAC_CALLBACK(pThis->m_pProcess);

        CordbAssembly * pAssembly = pThis->m_pAppDomain->LookupOrCreateAssembly(vmDomainAssembly);

        pThis->SetAndMoveNext(pAssembly);
    }

    // Set the current index in the table and increment the cursor.
    //
    // Arguments:
    //    pAssembly - assembly from DAC enumerator
    void SetAndMoveNext(CordbAssembly * pAssembly)
    {
        _ASSERTE(pAssembly != NULL);

        if (m_index >= m_countElements)
        {
            // Enumerating the assemblies in the target should be fixed since
            // the target is not running.
            // We should never get here unless the target is unstable.
            // The caller (the shim) pre-allocated the table of assemblies.
            m_pProcess->TargetConsistencyCheck(!"Target changed assembly count");
            return;
        }

        m_pAssemblies[m_index].Assign(pAssembly);
        m_index++;
    }

protected:
    CordbProcess * m_pProcess;
    CordbAppDomain * m_pAppDomain;
    RSExtSmartPtr<ICorDebugAssembly>* m_pAssemblies;
    ULONG m_countElements;
    ULONG m_index;
};

//---------------------------------------------------------------------------------------
// Shim Helper to enumerate the assemblies in the load-order
//
// Arguments:
//    pAppdomain - non-null appdomain to enumerate assemblies.
//    pAssemblies - caller pre-allocated array to hold assemblies
//    countAssemblies - size of the array.
//
// Notes:
//    Caller preallocated array (likely from ICorDebugAssemblyEnum::GetCount),
//    and now this function fills in the assemblies in the order they were
//    loaded.
//
//    The target should be stable, such that the number of assemblies in the
//    target is stable, and therefore countAssemblies as determined by the
//    shim via ICorDebugAssemblyEnum::GetCount should match the number of
//    assemblies enumerated here.
//
//    Called by code:ShimProcess::QueueFakeAttachEvents.
//    This provides the assemblies in load-order. In contrast,
//    ICorDebugAppDomain::EnumerateAssemblies is a random order. The shim needs
//    load-order to match Whidbey semantics for dispatching fake load-assembly
//    callbacks on attach. The debugger then uses the order
//    in its module display window.
//
void CordbProcess::GetAssembliesInLoadOrder(
    ICorDebugAppDomain * pAppDomain,
    RSExtSmartPtr<ICorDebugAssembly>* pAssemblies,
    ULONG countAssemblies)
{
    PUBLIC_API_ENTRY_FOR_SHIM(this);
    RSLockHolder lockHolder(GetProcessLock());

    _ASSERTE(GetShim() != NULL);

    CordbAppDomain * pAppDomainInternal = static_cast<CordbAppDomain *> (pAppDomain);

    ShimAssemblyCallbackData data(pAppDomainInternal, pAssemblies, countAssemblies);

    // Enumerate through and fill out pAssemblies table.
    GetDAC()->EnumerateAssembliesInAppDomain(
        pAppDomainInternal->GetADToken(),
        ShimAssemblyCallbackData::Callback,
        &data); // user data

    // pAssemblies array has now been updated.
}

// Callback data for code:CordbProcess::GetModulesInLoadOrder
class ShimModuleCallbackData
{
public:
    // Ctor to initialize callback data
    //
    // Arguments:
    //   pAssembly - assembly that the Modules are in.
    //   pModules - preallocated array of smart pointers to hold Modules
    //   countModules - size of pModules in elements.
    ShimModuleCallbackData(
        CordbAssembly * pAssembly,
        RSExtSmartPtr<ICorDebugModule>* pModules,
        ULONG countModules)
    {
        _ASSERTE(pAssembly != NULL);
        _ASSERTE(pModules != NULL);

        m_pProcess = pAssembly->GetAppDomain()->GetProcess();
        m_pAssembly = pAssembly;
        m_pModules = pModules;
        m_countElements = countModules;
        m_index = 0;

        // Just to be safe, clear them all out
        for(ULONG i = 0; i < countModules; i++)
        {
            pModules[i].Clear();
        }
    }

    // Dtor
    //
    // Notes:
    //   This can assert end-of-enumeration invariants.
    ~ShimModuleCallbackData()
    {
        // Ensure that we went through all Modules.
        _ASSERTE(m_index == m_countElements);
    }

    // Callback invoked from DAC enumeration.
    //
    // arguments:
    //    vmDomainAssembly - VMPTR for Module
    //    pData - a 'this' pointer
    //
    static void Callback(VMPTR_DomainAssembly vmDomainAssembly, void * pData)
    {
        ShimModuleCallbackData * pThis = static_cast<ShimModuleCallbackData *> (pData);
        INTERNAL_DAC_CALLBACK(pThis->m_pProcess);

        CordbModule * pModule = pThis->m_pAssembly->GetAppDomain()->LookupOrCreateModule(vmDomainAssembly);

        pThis->SetAndMoveNext(pModule);
    }

    // Set the current index in the table and increment the cursor.
    //
    // Arguments:
    //    pModule - Module from DAC enumerator
    void SetAndMoveNext(CordbModule * pModule)
    {
        _ASSERTE(pModule != NULL);

        if (m_index >= m_countElements)
        {
            // Enumerating the Modules in the target should be fixed since
            // the target is not running.
            // We should never get here unless the target is unstable.
            // The caller (the shim) pre-allocated the table of Modules.
            m_pProcess->TargetConsistencyCheck(!"Target changed Module count");
            return;
        }

        m_pModules[m_index].Assign(pModule);
        m_index++;
    }

protected:
    CordbProcess * m_pProcess;
    CordbAssembly * m_pAssembly;
    RSExtSmartPtr<ICorDebugModule>* m_pModules;
    ULONG m_countElements;
    ULONG m_index;
};

//---------------------------------------------------------------------------------------
// Shim Helper to enumerate the Modules in the load-order
//
// Arguments:
//    pAppdomain - non-null appdomain to enumerate Modules.
//    pModules - caller pre-allocated array to hold Modules
//    countModules - size of the array.
//
// Notes:
//    Caller preallocated array (likely from ICorDebugModuleEnum::GetCount),
//    and now this function fills in the Modules in the order they were
//    loaded.
//
//    The target should be stable, such that the number of Modules in the
//    target is stable, and therefore countModules as determined by the
//    shim via ICorDebugModuleEnum::GetCount should match the number of
//    Modules enumerated here.
//
//    Called by code:ShimProcess::QueueFakeAssemblyAndModuleEvent.
//    This provides the Modules in load-order. In contrast,
//    ICorDebugAssembly::EnumerateModules is a random order. The shim needs
//    load-order to match Whidbey semantics for dispatching fake load-Module
//    callbacks on attach. The most important thing is that the manifest module
//    gets a LodModule callback before any secondary modules.  For dynamic
//    modules, this is necessary for operations on the secondary module
//    that rely on manifest metadata (eg. GetSimpleName).
//
//    @dbgtodo : This is almost identical to GetAssembliesInLoadOrder, and
//    (together wih the CallbackData classes) seems a HUGE amount of code and
//    complexity for such a simple thing.  We also have extra code to order
//    AppDomains and Threads.  We should try and rip all of this extra complexity
//    out, and replace it with better data structures for storing these items.
//    Eg., if we used std::map, we could have efficient lookups and ordered
//    enumerations.  However, we do need to be careful about exposing new invariants
//    through ICorDebug that customers may depend on, which could place a long-term
//    compatibility burden on us.  We could have a simple generic data structure
//    (eg. built on std::hash_map and std::list) which provided efficient look-up
//    and both in-order and random enumeration.
//
void CordbProcess::GetModulesInLoadOrder(
    ICorDebugAssembly * pAssembly,
    RSExtSmartPtr<ICorDebugModule>* pModules,
    ULONG countModules)
{
    PUBLIC_API_ENTRY_FOR_SHIM(this);
    RSLockHolder lockHolder(GetProcessLock());

    _ASSERTE(GetShim() != NULL);

    CordbAssembly * pAssemblyInternal = static_cast<CordbAssembly *> (pAssembly);

    ShimModuleCallbackData data(pAssemblyInternal, pModules, countModules);

    // Enumerate through and fill out pModules table.
    GetDAC()->EnumerateModulesInAssembly(
        pAssemblyInternal->GetDomainAssemblyPtr(),
        ShimModuleCallbackData::Callback,
        &data); // user data

    // pModules array has now been updated.
}


//---------------------------------------------------------------------------------------
// Callback to count the number of enumerations in a process.
//
// Arguments:
//     id - the connection id.
//     pName - name of the connection
//     pUserData - an EnumerateConnectionsData
//
// Notes:
//    Helper function for code:CordbProcess::QueueFakeConnectionEvents
//
// static
void CordbProcess::CountConnectionsCallback(DWORD id, LPCWSTR pName, void * pUserData)
{
}

//---------------------------------------------------------------------------------------
// Callback to enumerate all the connections in a process.
//
// Arguments:
//     id - the connection id.
//     pName - name of the connection
//     pUserData - an EnumerateConnectionsData
//
// Notes:
//    Helper function for code:CordbProcess::QueueFakeConnectionEvents
//
// static
void CordbProcess::EnumerateConnectionsCallback(DWORD id, LPCWSTR pName, void * pUserData)
{
}

//---------------------------------------------------------------------------------------
// Callback from Shim to queue fake Connection events on attach.
//
// Notes:
//    See code:ShimProcess::QueueFakeAttachEvents
void CordbProcess::QueueFakeConnectionEvents()
{
    PUBLIC_API_ENTRY_FOR_SHIM(this);

}

//
// DispatchRCEvent -- dispatches a previously queued IPC event received
// from the runtime controller. This represents the last amount of processing
// the DI gets to do on an event before giving it to the user.
//
void CordbProcess::DispatchRCEvent()
{
    INTERNAL_API_ENTRY(this);

    CONTRACTL
    {
        // This is happening on the RCET thread, so there's no place to propagate an error back up.
        NOTHROW;
    }
    CONTRACTL_END;

    _ASSERTE(m_pShim != NULL); // V2 case

    //
    // Note: the current thread should have the process locked when it
    // enters this method.
    //
    _ASSERTE(ThreadHoldsProcessLock());

    // Create/Launch paths already ensured that we had a callback.
    _ASSERTE(m_cordb != NULL);
    _ASSERTE(m_cordb->m_managedCallback != NULL);
    _ASSERTE(m_cordb->m_managedCallback2 != NULL);
    _ASSERTE(m_cordb->m_managedCallback3 != NULL);
    _ASSERTE(m_cordb->m_managedCallback4 != NULL);


    // Bump up the stop count. Either we'll dispatch a managed event,
    // or the logic below will decide not to dispatch one and call
    // Continue itself. Either way, the stop count needs to go up by
    // one...
    _ASSERTE(this->GetSyncCompleteRecv());
    SetSynchronized(true);
    IncStopCount();

    // As soon as we call Unlock(), we might get neutered and lose our reference to
    // the shim.  Grab it now for use later.
    RSExtSmartPtr<ShimProcess> pShim(m_pShim);

    Unlock();

    _ASSERTE(!ThreadHoldsProcessLock());


    // We want to stay synced until after the callbacks return. This is b/c we're on the RCET,
    // and we may deadlock if we send IPC events on the RCET if we're not synced (see SendIPCEvent for details).
    // So here, stopcount=1. The StopContinueHolder bumps it up to 2.
    // - If Cordbg calls continue in the callback, that bumps it back down to 1, but doesn't actually continue.
    //   The holder dtor then bumps it down to 0, doing the real continue.
    // - If Cordbg doesn't call continue in the callback, then stopcount stays at 2, holder dtor drops it down to 1,
    //   and then the holder was just a nop.
    // This gives us delayed continues w/ no extra state flags.


    // The debugger may call Detach() immediately after it returns from the callback, but before this thread returns
    // from this function.  Thus after we execute the callbacks, it's possible the CordbProcess object has been neutered.

    // Since we're already sycned, the Stop from the holder here is practically a nop that just bumps up a count.
    // Create an extra scope for the StopContinueHolder.
    {
        StopContinueHolder h;
        HRESULT hr = h.Init(this);
        if (FAILED(hr))
        {
            CORDBSetUnrecoverableError(this, hr, 0);
        }

        HRESULT hrCallback = S_OK;
        // It's possible a ICorDebugProcess::Detach() may have occurred by now.
        {
            // @dbgtodo shim: eventually the entire RCET should be considered outside the RS.
            PUBLIC_CALLBACK_IN_THIS_SCOPE0_NO_LOCK(this);


            // Snag the first event off the queue.
            // Holder will call Delete, which will invoke virtual Dtor that will release ICD objects.
            // Since these are external refs, we want to do it while "outside" the RS.
            NewHolder<ManagedEvent> pEvent(pShim->DequeueManagedEvent());

            // Normally pEvent shouldn't be NULL, since this method is called when the queue is not empty.
            // But due to a race between CordbProcess::Terminate(), CordbWin32EventThread::ExitProcess() and this method
            // it is totally possible that the queue has already been cleaned up and we can't expect that event is always available.
            if (pEvent != NULL)
            {
                // Since we need to access a member (m_cordb), protect this block with a
                // lock and a check for Neutering (in case process detach has just
                // occurred).  We'll release the lock around the dispatch later on.
                RSLockHolder lockHolder(GetProcessLock());
                if (!IsNeutered())
                {
#ifdef _DEBUG
                    // On a debug build, keep track of the last IPC event we dispatched.
                    m_pDBGLastIPCEventType = pEvent->GetDebugCookie();
#endif

                    ManagedEvent::DispatchArgs args(m_cordb->m_managedCallback, m_cordb->m_managedCallback2, m_cordb->m_managedCallback3, m_cordb->m_managedCallback4);

                    {
                        // Release lock around the dispatch of the event
                        RSInverseLockHolder inverseLockHolder(GetProcessLock());

                        EX_TRY
                        {
                            // This dispatches almost directly into the user's callbacks.
                            // It does not update any RS state.
                            hrCallback = pEvent->Dispatch(args);
                        }
                        EX_CATCH_HRESULT(hrCallback);
                    }
                }
            }

        } // we're now back inside the RS

        if (hrCallback == E_NOTIMPL)
        {
            ContinueInternal(FALSE);
        }


    } // forces Continue to be called

    Lock();

};

#ifdef _DEBUG
//---------------------------------------------------------------------------------------
// Debug-only callback to ensure that an appdomain is not available after the ExitAppDomain event.
//
// Arguments:
//    vmAppDomain - appdomain from enumeration
//    pUserData - pointer to a DbgAssertAppDomainDeletedData which contains the VMAppDomain that was just deleted.
// notes:
//    see code:CordbProcess::DbgAssertAppDomainDeleted for details.
void CordbProcess::DbgAssertAppDomainDeletedCallback(VMPTR_AppDomain vmAppDomain, void * pUserData)
{
    DbgAssertAppDomainDeletedData * pCallbackData = reinterpret_cast<DbgAssertAppDomainDeletedData *>(pUserData);
    INTERNAL_DAC_CALLBACK(pCallbackData->m_pThis);

    VMPTR_AppDomain vmAppDomainDeleted = pCallbackData->m_vmAppDomainDeleted;
    CONSISTENCY_CHECK_MSGF((vmAppDomain != vmAppDomainDeleted),
        ("An ExitAppDomain event was sent for appdomain, but it still shows up in the enumeration.\n vmAppDomain=%p\n",
        VmPtrToCookie(vmAppDomainDeleted)));
}

//---------------------------------------------------------------------------------------
// Debug-only helper to Assert that VMPTR is actually removed.
//
// Arguments:
//    vmAppDomainDeleted - vmptr of appdomain that we just got exit event for.
//       This should not be discoverable from the RS.
//
// Notes:
//   See code:IDacDbiInterface#Enumeration for rules that we're asserting.
//   Once the exit appdomain event is dispatched, the appdomain should not be discoverable by the RS.
//   Else the RS may use the AppDomain* after it's deleted.
//   This asserts that the AppDomain* is not discoverable.
//
//   Since this is a debug-only function, it should have no side-effects.
void CordbProcess::DbgAssertAppDomainDeleted(VMPTR_AppDomain vmAppDomainDeleted)
{
    DbgAssertAppDomainDeletedData callbackData;
    callbackData.m_pThis = this;
    callbackData.m_vmAppDomainDeleted = vmAppDomainDeleted;

    GetDAC()->EnumerateAppDomains(
        CordbProcess::DbgAssertAppDomainDeletedCallback,
        &callbackData);
}

#endif  // _DEBUG

//---------------------------------------------------------------------------------------
// Update state and potentially Dispatch a single event.
//
// Arguments:
//    pEvent - non-null pointer to debug event.
//    pCallback1 - callback object to dispatch on (for V1 callbacks)
//    pCallback2 - 2nd callback object to dispatch on (for new V2 callbacks)
//    pCallback3 - 3rd callback object to dispatch on (for new V4 callbacks)
//
//
// Returns:
//    Nothing. Throws on error.
//
// Notes:
//    Generally, this will dispatch exactly 1 callback. It may dispatch 0 callbacks if there is an error
//    or in other corner cases (documented within the dispatch code below).
//    Errors could occur because:
//    - the event is corrupted (exceptional case)
//    - the RS is corrupted / OOM (exceptional case)
//    Exception errors here will propagate back to the Filter() call, and there's not really anything
//    a debugger can do about an error here (perhaps report it to the user).
//    Errors must leave IcorDebug in a consistent state.
//
//    This is dispatched directly on the Win32Event Thread in response to calling Filter.
//    Therefore, this can't send any IPC events (Not an issue once everything is DAC-ized).
//    A V2 shim can provide a proxy calllack that takes these events and queues them and
//    does the real dispatch to the user to emulate V2 semantics.
//
#ifdef _PREFAST_
#pragma warning(push)
#pragma warning(disable:21000) // Suppress PREFast warning about overly large function
#endif
void CordbProcess::RawDispatchEvent(
    DebuggerIPCEvent *          pEvent,
    RSLockHolder *              pLockHolder,
    ICorDebugManagedCallback *  pCallback1,
    ICorDebugManagedCallback2 * pCallback2,
    ICorDebugManagedCallback3 * pCallback3,
    ICorDebugManagedCallback4 * pCallback4)
{
    CONTRACTL
    {
        THROWS;
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;
    // We start off with the lock, and we'll toggle it.
    _ASSERTE(ThreadHoldsProcessLock());


    //
    // Call StartEventDispatch to true to guard against calls to Continue()
    // from within the user's callback. We need Continue() to behave a little
    // bit differently in such a case.
    //
    // Also note that Win32EventThread::ExitProcess will take the lock and free all
    // events in the queue. (the current event is already off the queue, so
    // it will be ok). But we can't do the EP callback in the middle of this dispatch
    // so if this flag is set, EP will wait on the miscWaitEvent (which will
    // get set in FlushQueuedEvents when we return from here) and let us finish here.
    //
    StartEventDispatch(pEvent->type);

    // Keep strong references to these objects in case a callback deletes them from underneath us.
    RSSmartPtr<CordbAppDomain> pAppDomain;
    CordbThread * pThread = NULL;


    // Get thread that this event is on. In attach scenarios, this may be the first time ICorDebug has seen this thread.
    if (!pEvent->vmThread.IsNull())
    {
        pThread = LookupOrCreateThread(pEvent->vmThread);
    }

    if (!pEvent->vmAppDomain.IsNull())
    {
        pAppDomain.Assign(LookupOrCreateAppDomain(pEvent->vmAppDomain));
    }

    DWORD dwVolatileThreadId = 0;
    if (pThread != NULL)
    {
        dwVolatileThreadId = pThread->GetUniqueId();
    }


    //
    // Update the app domain that this thread lives in.
    //
    if ((pThread != NULL) && (pAppDomain != NULL))
    {
        // It shouldn't be possible for us to see an exited AppDomain here
        _ASSERTE( !pAppDomain->IsNeutered() );

         pThread->m_pAppDomain = pAppDomain;
    }

    _ASSERTE(pEvent != NULL);
    _ASSERTE(pCallback1 != NULL);
    _ASSERTE(pCallback2 != NULL);
    _ASSERTE(pCallback3 != NULL);
    _ASSERTE(pCallback4 != NULL);

    STRESS_LOG1(LF_CORDB, LL_EVERYTHING, "Pre-Dispatch IPC event: %s\n", IPCENames::GetName(pEvent->type));

    switch (pEvent->type & DB_IPCE_TYPE_MASK)
    {
    case DB_IPCE_CREATE_PROCESS:
        {
            PUBLIC_CALLBACK_IN_THIS_SCOPE(this, pLockHolder, pEvent);
            pCallback1->CreateProcess(static_cast<ICorDebugProcess*> (this));
        }
        break;

    case DB_IPCE_BREAKPOINT:
        {
            _ASSERTE(pThread != NULL);
            _ASSERTE(pAppDomain != NULL);

            // Find the breakpoint object on this side.
            CordbBreakpoint *pBreakpoint = NULL;

            // We've found cases out in the wild where we get this event on a thread we don't recognize.
            // We're not sure how this happens. Add a runtime check to protect ourselves to avoid the
            // an AV. We still assert because this should not be happening.
            // It likely means theres some issue where we failed to send a CreateThread notification.
            TargetConsistencyCheck(pThread != NULL);
            pBreakpoint = pAppDomain->m_breakpoints.GetBase(LsPtrToCookie(pEvent->BreakpointData.breakpointToken));

            if (pBreakpoint != NULL)
            {
                ICorDebugBreakpoint * pIBreakpoint = CordbBreakpointToInterface(pBreakpoint);
                _ASSERTE(pIBreakpoint != NULL);

                {
                    PUBLIC_CALLBACK_IN_THIS_SCOPE2(this, pLockHolder, pEvent, "thread=0x%p, bp=0x%p", pThread, pBreakpoint);
                    pCallback1->Breakpoint(pAppDomain, pThread, pIBreakpoint);
                }
            }
        }
        break;

    case DB_IPCE_BEFORE_GARBAGE_COLLECTION:
        {
            {
                PUBLIC_CALLBACK_IN_THIS_SCOPE(this, pLockHolder, pEvent);
                pCallback4->BeforeGarbageCollection(static_cast<ICorDebugProcess*>(this));
            }
            break;
        }

    case DB_IPCE_AFTER_GARBAGE_COLLECTION:
        {
            {
                PUBLIC_CALLBACK_IN_THIS_SCOPE(this, pLockHolder, pEvent);
                pCallback4->AfterGarbageCollection(static_cast<ICorDebugProcess*>(this));
            }
            break;
        }
#ifdef FEATURE_DATABREAKPOINT
    case DB_IPCE_DATA_BREAKPOINT:
        {
            _ASSERTE(pThread != NULL);

            {
                PUBLIC_CALLBACK_IN_THIS_SCOPE(this, pLockHolder, pEvent);
                pCallback4->DataBreakpoint(static_cast<ICorDebugProcess*>(this), pThread, reinterpret_cast<BYTE*>(&(pEvent->DataBreakpointData.context)), sizeof(CONTEXT));
            }
            break;
        }
        break;
#endif
    case DB_IPCE_USER_BREAKPOINT:
        {
            STRESS_LOG1(LF_CORDB, LL_INFO1000, "[%x] RCET::DRCE: user breakpoint.\n",
                 GetCurrentThreadId());

            _ASSERTE(pThread != NULL);
            _ASSERTE(pAppDomain != NULL);
            _ASSERTE(pThread->m_pAppDomain != NULL);

            {
                PUBLIC_CALLBACK_IN_THIS_SCOPE(this, pLockHolder, pEvent);
                pCallback1->Break(pThread->m_pAppDomain, pThread);
            }

        }
        break;

    case DB_IPCE_STEP_COMPLETE:
        {
            STRESS_LOG1(LF_CORDB, LL_INFO1000, "[%x] RCET::DRCE: step complete.\n",
                 GetCurrentThreadId());

            PREFIX_ASSUME(pThread != NULL);

            CordbStepper * pStepper = m_steppers.GetBase(LsPtrToCookie(pEvent->StepData.stepperToken));

            // It's possible the stepper is NULL if:
            // - event X & step-complete are both in the queue
            // - during dispatch for event X, Cordbg cancels the stepper (thus removing it from m_steppers)
            // - the Step-Complete still stays in the queue, and so we're here, but out stepper's been removed.
            // (This could happen for breakpoints too)
            // Don't dispatch a callback if the stepper is NULL.
            if (pStepper != NULL)
            {
                RSSmartPtr<CordbStepper> pRef(pStepper);
                pStepper->m_active = false;
                m_steppers.RemoveBase((ULONG_PTR)pStepper->m_id);

                {
                    _ASSERTE(pThread->m_pAppDomain != NULL);
                    PUBLIC_CALLBACK_IN_THIS_SCOPE2(this, pLockHolder, pEvent, "thrad=0x%p, stepper=0x%p", pThread, pStepper);
                    pCallback1->StepComplete(pThread->m_pAppDomain, pThread, pStepper, pEvent->StepData.reason);
                }

                // implicit Release on pRef
            }
        }
        break;

    case DB_IPCE_EXCEPTION:
        {
            STRESS_LOG1(LF_CORDB, LL_INFO1000, "[%x] RCET::DRCE: exception.\n",
                 GetCurrentThreadId());

            _ASSERTE(pAppDomain != NULL);

            // For some exceptions very early in startup (eg, TypeLoad), this may have occurred before we
            // even executed jitted code on the thread. We may have not received a CreateThread yet.
            // In V2, we detected this and sent a LogMessage on a random thread.
            // In V3, we lazily create the CordbThread objects (possibly before the CreateThread event),
            // and so we know we should have one.
            _ASSERTE(pThread != NULL);

            pThread->SetExInfo(pEvent->Exception.vmExceptionHandle);

            _ASSERTE(pThread->m_pAppDomain != NULL);

            {
                PUBLIC_CALLBACK_IN_THIS_SCOPE(this, pLockHolder, pEvent);
                pCallback1->Exception(pThread->m_pAppDomain, pThread, !pEvent->Exception.firstChance);
            }

        }
        break;

    case DB_IPCE_SYNC_COMPLETE:
        _ASSERTE(!"Should have never queued a sync complete pEvent.");
        break;

    case DB_IPCE_THREAD_ATTACH:
        {
            STRESS_LOG1(LF_CORDB, LL_INFO100, "RCET::DRCE: thread attach : ID=%x.\n", dwVolatileThreadId);

            TargetConsistencyCheck(pThread != NULL);
            {
                PUBLIC_CALLBACK_IN_THIS_SCOPE1(this, pLockHolder, pEvent, "thread=0x%p", pThread);
                pCallback1->CreateThread(pAppDomain, pThread);
            }
        }
        break;

    case DB_IPCE_THREAD_DETACH:
        {
            STRESS_LOG2(LF_CORDB, LL_INFO100, "[%x] RCET::HRCE: thread detach : ID=%x \n",
                 GetCurrentThreadId(), dwVolatileThreadId);

            // If the runtime thread never entered managed code, there
            // won't be a CordbThread, and CreateThread was never
            // called, so don't bother calling ExitThread.
            if (pThread != NULL)
            {
                AddToNeuterOnContinueList(pThread);

                RSSmartPtr<CordbThread>    pRefThread(pThread);

                _ASSERTE(pAppDomain != NULL);

                // A thread is reported as dead before we get the exit event.
                // See code:IDacDbiInterface#IsThreadMarkedDead for the invariant being asserted here.
                TargetConsistencyCheck(pThread->IsThreadDead());

                // Enforce the enumeration invariants (see code:IDacDbiInterface#Enumeration)that the thread is not discoverable.
                INDEBUG(pThread->DbgAssertThreadDeleted());

                // Remove the thread from the hash. If we've removed it from the hash, we really should
                // neuter it ... but that causes test failures.
                // We'll neuter it in continue.
                m_userThreads.RemoveBase(VmPtrToCookie(pThread->m_vmThreadToken));


                LOG((LF_CORDB, LL_INFO1000, "[%x] RCET::HRCE: sending thread detach.\n", GetCurrentThreadId()));

                {
                    PUBLIC_CALLBACK_IN_THIS_SCOPE(this, pLockHolder, pEvent);
                    pCallback1->ExitThread(pAppDomain, pThread);
                }

                // Implicit release on thread & pAppDomain
            }
        }
        break;

    case DB_IPCE_METADATA_UPDATE:
        {
            CordbModule * pModule = pAppDomain->LookupOrCreateModule(pEvent->MetadataUpdateData.vmDomainAssembly);
            pModule->RefreshMetaData();
        }
        break;

    case DB_IPCE_LOAD_MODULE:
        {
            _ASSERTE (pAppDomain != NULL);
            CordbModule * pModule = pAppDomain->LookupOrCreateModule(pEvent->LoadModuleData.vmDomainAssembly);

            {
                pModule->SetLoadEventContinueMarker();

                PUBLIC_CALLBACK_IN_THIS_SCOPE(this, pLockHolder, pEvent);
                pCallback1->LoadModule(pAppDomain, pModule);
            }

        }
        break;

    case DB_IPCE_CREATE_CONNECTION:
        {
            STRESS_LOG1(LF_CORDB, LL_INFO100,
                "RCET::HRCE: Connection change %d \n",
                pEvent->CreateConnection.connectionId);

            // pass back the connection id and the connection name.
            PUBLIC_CALLBACK_IN_THIS_SCOPE(this, pLockHolder, pEvent);
            pCallback2->CreateConnection(
                this,
                pEvent->CreateConnection.connectionId,
                const_cast<WCHAR*> (pEvent->CreateConnection.wzConnectionName.GetString()));
        }
        break;

    case DB_IPCE_DESTROY_CONNECTION:
        {
            STRESS_LOG1(LF_CORDB, LL_INFO100,
                 "RCET::HRCE: Connection destroyed %d \n",
                 pEvent->ConnectionChange.connectionId);
            PUBLIC_CALLBACK_IN_THIS_SCOPE(this, pLockHolder, pEvent);
            pCallback2->DestroyConnection(this, pEvent->ConnectionChange.connectionId);
        }
        break;

    case DB_IPCE_CHANGE_CONNECTION:
        {
            STRESS_LOG1(LF_CORDB, LL_INFO100,
                 "RCET::HRCE: Connection changed %d \n",
                 pEvent->ConnectionChange.connectionId);

            PUBLIC_CALLBACK_IN_THIS_SCOPE(this, pLockHolder, pEvent);
            pCallback2->ChangeConnection(this, pEvent->ConnectionChange.connectionId);
        }
        break;

    case DB_IPCE_UNLOAD_MODULE:
        {
            STRESS_LOG3(LF_CORDB, LL_INFO100, "RCET::HRCE: unload module on thread %#x Mod:0x%x AD:0x%08x\n",
                 dwVolatileThreadId,
                 VmPtrToCookie(pEvent->UnloadModuleData.vmDomainAssembly),
                 VmPtrToCookie(pEvent->vmAppDomain));

            PREFIX_ASSUME (pAppDomain != NULL);

            CordbModule *module = pAppDomain->LookupOrCreateModule(pEvent->UnloadModuleData.vmDomainAssembly);

            if (module == NULL)
            {
                LOG((LF_CORDB, LL_INFO100, "Already unloaded Module - continue()ing!" ));
                break;
            }
            _ASSERTE(module != NULL);
            INDEBUG(module->DbgAssertModuleDeleted());

            // The appdomain we're unloading in must be the appdomain we were loaded in. Otherwise, we've got mismatched
            // module and appdomain pointers. Bugs 65943 & 81728.
            _ASSERTE(pAppDomain == module->GetAppDomain());

            // Ensure the module gets neutered once we call continue.
            AddToNeuterOnContinueList(module); // throws
            {
                PUBLIC_CALLBACK_IN_THIS_SCOPE(this, pLockHolder, pEvent);
                pCallback1->UnloadModule(pAppDomain, module);
            }

            pAppDomain->m_modules.RemoveBase(VmPtrToCookie(pEvent->UnloadModuleData.vmDomainAssembly));
        }
        break;

    case DB_IPCE_LOAD_CLASS:
        {
            CordbClass *pClass = NULL;

            LOG((LF_CORDB, LL_INFO10000,
                 "RCET::HRCE: load class on thread %#x Tok:0x%08x Mod:0x%08x Asm:0x%08x AD:0x%08x\n",
                 dwVolatileThreadId,
                 pEvent->LoadClass.classMetadataToken,
                 VmPtrToCookie(pEvent->LoadClass.vmDomainAssembly),
                 LsPtrToCookie(pEvent->LoadClass.classDebuggerAssemblyToken),
                 VmPtrToCookie(pEvent->vmAppDomain)));

            _ASSERTE (pAppDomain != NULL);

            CordbModule* pModule = pAppDomain->LookupOrCreateModule(pEvent->LoadClass.vmDomainAssembly);
            if (pModule == NULL)
            {
                LOG((LF_CORDB, LL_INFO100, "Load Class on not-loaded Module - continue()ing!" ));
                break;
            }
            _ASSERTE(pModule != NULL);

            BOOL fDynamic = pModule->IsDynamic();

            // If this is a class load in a dynamic module, the metadata has become invalid.
            if (fDynamic)
            {
                pModule->RefreshMetaData();
            }

            hr = pModule->LookupOrCreateClass(pEvent->LoadClass.classMetadataToken, &pClass);
            _ASSERTE(SUCCEEDED(hr) == (pClass != NULL));
            IfFailThrow(hr);

            // Prevent class load from being sent twice.
            // @dbgtodo - Microsoft, cordbclass: this is legacy. Can this really happen? Investigate as we dac-ize CordbClass.
            if (pClass->LoadEventSent())
            {
                // Dynamic modules are dynamic at the module level -
                // you can't add a new version of a class once the module
                // is baked.
                // EnC adds completely new classes.
                // There shouldn't be any other way to send multiple
                // ClassLoad events.
                // Except that there are race conditions between loading
                // an appdomain, and loading a class, so if we get the extra
                // class load, we should ignore it.
                break; //out of the switch statement
            }
            pClass->SetLoadEventSent(TRUE);


            if (pClass != NULL)
            {
                PUBLIC_CALLBACK_IN_THIS_SCOPE(this, pLockHolder, pEvent);
                pCallback1->LoadClass(pAppDomain, pClass);
            }
        }
        break;

    case DB_IPCE_UNLOAD_CLASS:
        {
            LOG((LF_CORDB, LL_INFO10000,
                 "RCET::HRCE: unload class on thread %#x Tok:0x%08x Mod:0x%08x AD:0x%08x\n",
                 dwVolatileThreadId,
                 pEvent->UnloadClass.classMetadataToken,
                 VmPtrToCookie(pEvent->UnloadClass.vmDomainAssembly),
                 VmPtrToCookie(pEvent->vmAppDomain)));

            // get the appdomain object
            _ASSERTE (pAppDomain != NULL);

            CordbModule *pModule = pAppDomain->LookupOrCreateModule(pEvent->UnloadClass.vmDomainAssembly);
            if (pModule == NULL)
            {
                LOG((LF_CORDB, LL_INFO100, "Unload Class on not-loaded Module - continue()ing!" ));
                break;
            }
            _ASSERTE(pModule != NULL);

            CordbClass *pClass = pModule->LookupClass(pEvent->UnloadClass.classMetadataToken);

            if (pClass != NULL && !pClass->HasBeenUnloaded())
            {
                pClass->SetHasBeenUnloaded(true);

                PUBLIC_CALLBACK_IN_THIS_SCOPE(this, pLockHolder, pEvent);
                pCallback1->UnloadClass(pAppDomain, pClass);
            }
        }
        break;

    case DB_IPCE_FIRST_LOG_MESSAGE:
        {
            _ASSERTE(pThread != NULL);
            _ASSERTE(pAppDomain != NULL);

            const WCHAR * pszContent = pEvent->FirstLogMessage.szContent.GetString();
            {
                PUBLIC_CALLBACK_IN_THIS_SCOPE(this, pLockHolder, pEvent);
                pCallback1->LogMessage(
                   pAppDomain,
                   pThread,
                   pEvent->FirstLogMessage.iLevel,
                   const_cast<WCHAR*> (pEvent->FirstLogMessage.szCategory.GetString()),
                   const_cast<WCHAR*> (pszContent));
            }
        }
        break;

    case DB_IPCE_LOGSWITCH_SET_MESSAGE:
        {

            LOG((LF_CORDB, LL_INFO10000,
                "[%x] RCET::DRCE: Log Switch Setting Message.\n",
                 GetCurrentThreadId()));

            _ASSERTE(pThread != NULL);

            const WCHAR *pstrLogSwitchName = pEvent->LogSwitchSettingMessage.szSwitchName.GetString();
            const WCHAR *pstrParentName = pEvent->LogSwitchSettingMessage.szParentSwitchName.GetString();

            // from the thread object get the appdomain object
            _ASSERTE(pAppDomain == pThread->m_pAppDomain);
            _ASSERTE (pAppDomain != NULL);

            {
                PUBLIC_CALLBACK_IN_THIS_SCOPE(this, pLockHolder, pEvent);
                pCallback1->LogSwitch(
                    pAppDomain,
                    pThread,
                    pEvent->LogSwitchSettingMessage.iLevel,
                    pEvent->LogSwitchSettingMessage.iReason,
                    const_cast<WCHAR*> (pstrLogSwitchName),
                    const_cast<WCHAR*> (pstrParentName));

            }
        }

        break;
    case DB_IPCE_CUSTOM_NOTIFICATION:
        {
            _ASSERTE(pThread != NULL);
            _ASSERTE(pAppDomain != NULL);


            // determine first whether custom notifications for this type are enabled -- if not
            // we just return without doing anything.
            CordbClass * pNotificationClass = LookupClass(pAppDomain,
                                                          pEvent->CustomNotification.vmDomainAssembly,
                                                          pEvent->CustomNotification.classToken);

            // if the class is NULL, that means the debugger never enabled notifications for it. Otherwise,
            // the CordbClass instance would already have been created when the notifications were
            // enabled.
            if ((pNotificationClass != NULL) && pNotificationClass->CustomNotificationsEnabled())

            {
                PUBLIC_CALLBACK_IN_THIS_SCOPE(this, pLockHolder, pEvent);
                pCallback3->CustomNotification(pThread, pAppDomain);
            }
        }

        break;

    case DB_IPCE_CREATE_APP_DOMAIN:
        {
            STRESS_LOG2(LF_CORDB, LL_INFO100,
                 "RCET::HRCE: create appdomain on thread %#x AD:0x%08x \n",
                 dwVolatileThreadId,
                 VmPtrToCookie(pEvent->vmAppDomain));


            // Enumerate may have prepopulated the appdomain, so check if it already exists.
            // Either way, still send the CreateEvent. (We don't want to skip the Create event
            // just because the debugger did an enumerate)
            // We remove AppDomains from the hash as soon as they are exited.
            pAppDomain.Assign(LookupOrCreateAppDomain(pEvent->AppDomainData.vmAppDomain));
            _ASSERTE(pAppDomain != NULL); // throws on failure

            {
                PUBLIC_CALLBACK_IN_THIS_SCOPE(this, pLockHolder, pEvent);
                hr = pCallback1->CreateAppDomain(this, pAppDomain);
            }
        }


        break;

    case DB_IPCE_EXIT_APP_DOMAIN:
        {
            STRESS_LOG2(LF_CORDB, LL_INFO100, "RCET::HRCE: exit appdomain on thread %#x AD:0x%08x \n",
                 dwVolatileThreadId,
                 VmPtrToCookie(pEvent->vmAppDomain));

            // In debug-only builds, assert that the appdomain is indeed deleted and not discoverable.
            INDEBUG(DbgAssertAppDomainDeleted(pEvent->vmAppDomain));

            // If we get an ExitAD message for which we have no AppDomain, then ignore it.
            // This can happen if an AD gets torn down very early (before the LS AD is to the
            // point that it can be published).
            // This could also happen if we attach a debugger right before the Exit event is sent.
            // In this case, the debuggee is no longer publishing the appdomain.
            if (pAppDomain == NULL)
            {
                break;
            }
            _ASSERTE (pAppDomain != NULL);

            // See if this is the default AppDomain exiting.  This should only happen very late in
            // the shutdown cycle, and so we shouldn't do anything significant with m_pDefaultDomain==NULL.
            // We should try and remove m_pDefaultDomain entirely since we can't count on it always existing.
            if (pAppDomain == m_pDefaultAppDomain)
            {
                m_pDefaultAppDomain = NULL;
            }

            // Update any threads which were last seen in this AppDomain.  We don't
            // get any notification when a thread leaves an AppDomain, so our idea
            // of what AppDomain the thread is in may be out of date.
            UpdateThreadsForAdUnload( pAppDomain );

            // This will still maintain weak references so we could call Continue.
            AddToNeuterOnContinueList(pAppDomain);

            {
                PUBLIC_CALLBACK_IN_THIS_SCOPE(this, pLockHolder, pEvent);
                hr = pCallback1->ExitAppDomain(this, pAppDomain);
            }

            // @dbgtodo appdomain: This should occur before the callback.
            // Even after ExitAppDomain, the outside world will want to continue calling
            // Continue (and thus they may need to call CordbAppDomain::GetProcess(), which Neutering
            // would clear). Thus we can't neuter yet.

            // Remove this app domain. This means any attempt to lookup the AppDomain
            // will fail (which we do at the top of this method).  Since any threads (incorrectly) referring
            // to this AppDomain have been moved to the default AppDomain, no one should be
            // interested in looking this AppDomain up anymore.
            m_appDomains.RemoveBase(VmPtrToCookie(pEvent->vmAppDomain));
        }

        break;

    case DB_IPCE_LOAD_ASSEMBLY:
        {
            LOG((LF_CORDB, LL_INFO100,
                "RCET::HRCE: load assembly on thread %#x Asm:0x%08x AD:0x%08x \n",
                dwVolatileThreadId,
                VmPtrToCookie(pEvent->AssemblyData.vmDomainAssembly),
                VmPtrToCookie(pEvent->vmAppDomain)));

            _ASSERTE (pAppDomain != NULL);

            // Determine if this Assembly is cached.
            CordbAssembly * pAssembly = pAppDomain->LookupOrCreateAssembly(pEvent->AssemblyData.vmDomainAssembly);
            _ASSERTE(pAssembly != NULL); // throws on error

            // If created, or have, an Assembly, notify callback.
            {
                PUBLIC_CALLBACK_IN_THIS_SCOPE(this, pLockHolder, pEvent);
                hr = pCallback1->LoadAssembly(pAppDomain, pAssembly);
            }
        }

        break;

    case DB_IPCE_UNLOAD_ASSEMBLY:
        {
            LOG((LF_CORDB, LL_INFO100, "RCET::DRCE: unload assembly on thread %#x Asm:0x%x AD:0x%x\n",
                 dwVolatileThreadId,
                 VmPtrToCookie(pEvent->AssemblyData.vmDomainAssembly),
                 VmPtrToCookie(pEvent->vmAppDomain)));

            _ASSERTE (pAppDomain != NULL);

            CordbAssembly * pAssembly = pAppDomain->LookupOrCreateAssembly(pEvent->AssemblyData.vmDomainAssembly);

            if (pAssembly == NULL)
            {
                // No assembly. This could happen if we attach right before an unload event is sent.
                return;
            }
           _ASSERTE(pAssembly != NULL);
           INDEBUG(pAssembly->DbgAssertAssemblyDeleted());

            // Ensure the assembly gets neutered when we call continue.
            AddToNeuterOnContinueList(pAssembly); // throws

            {
                PUBLIC_CALLBACK_IN_THIS_SCOPE(this, pLockHolder, pEvent);
                hr = pCallback1->UnloadAssembly(pAppDomain, pAssembly);
            }

            pAppDomain->RemoveAssemblyFromCache(pEvent->AssemblyData.vmDomainAssembly);
        }

        break;

    case DB_IPCE_FUNC_EVAL_COMPLETE:
        {
            LOG((LF_CORDB, LL_INFO1000, "RCET::DRCE: func eval complete.\n"));

            CordbEval *pEval = NULL;
            {
                pEval = pEvent->FuncEvalComplete.funcEvalKey.UnWrapAndRemove(this);
                if (pEval  == NULL)
                {
                    _ASSERTE(!"Bogus FuncEval handle in IPC block.");
                    // Bogus handle in IPC block.
                    break;
                }
            }
            _ASSERTE(pEval != NULL);

            _ASSERTE(pThread != NULL);
            _ASSERTE(pAppDomain != NULL);

            CONSISTENCY_CHECK_MSGF(pEval->m_DbgAppDomainStarted == pAppDomain,
                ("AppDomain changed from Func-Eval. Eval=%p, Started=%p, Now=%p\n",
                pEval, pEval->m_DbgAppDomainStarted, (void*) pAppDomain));

            // Hold the data about the result in the CordbEval for later.
            pEval->m_complete       = true;
            pEval->m_successful     = !!pEvent->FuncEvalComplete.successful;
            pEval->m_aborted        = !!pEvent->FuncEvalComplete.aborted;
            pEval->m_resultAddr     = pEvent->FuncEvalComplete.resultAddr;
            pEval->m_vmObjectHandle = pEvent->FuncEvalComplete.vmObjectHandle;
            pEval->m_resultType     = pEvent->FuncEvalComplete.resultType;
            pEval->m_resultAppDomainToken = pEvent->FuncEvalComplete.vmAppDomain;

            CordbAppDomain *pResultAppDomain = LookupOrCreateAppDomain(pEvent->FuncEvalComplete.vmAppDomain);

            _ASSERTE(OutstandingEvalCount() > 0);
            DecrementOutstandingEvalCount();

            CONSISTENCY_CHECK_MSGF(pEval->m_DbgAppDomainStarted == pAppDomain,
                ("AppDomain changed from Func-Eval. Eval=%p, Started=%p, Now=%p\n",
                pEval, pEval->m_DbgAppDomainStarted, (void*) pAppDomain));

            // If we did this func eval with this thread stopped at an exception, then we need to pretend as if we
            // really didn't continue from the exception, since, of course, we really didn't on the Left Side.
            if (pEval->IsEvalDuringException())
            {
                pThread->SetExInfo(pEval->m_vmThreadOldExceptionHandle);
            }

            bool fEvalCompleted = pEval->m_successful || pEval->m_aborted;

            // If a CallFunction() is aborted, the LHS may not complete the abort
            // immediately and hence we cant do a SendCleanup() at that point. Also,
            // the debugger may (incorrectly) release the CordbEval before this
            // DB_IPCE_FUNC_EVAL_COMPLETE event is received. Hence, we maintain an
            // extra ref-count to determine when this can be done.
            // Note that this can cause a two-way DB_IPCE_FUNC_EVAL_CLEANUP event
            // to be sent. Hence, it has to be done before the Continue (see issue 102745).


            // Note that if the debugger has already (incorrectly) released the CordbEval,
            // pEval will be pointing to garbage and should not be used by the debugger.
            if (fEvalCompleted)
            {
                PUBLIC_CALLBACK_IN_THIS_SCOPE2(this, pLockHolder, pEvent, "thread=0x%p, eval=0x%p. (Complete)", pThread, pEval);
                pCallback1->EvalComplete(pResultAppDomain, pThread, pEval);
            }
            else
            {
                PUBLIC_CALLBACK_IN_THIS_SCOPE2(this, pLockHolder, pEvent, "pThread=0x%p, eval=0x%p. (Exception)", pThread, pEval);
                pCallback1->EvalException(pResultAppDomain, pThread, pEval);
            }

            // This release may send an DB_IPCE_FUNC_EVAL_CLEANUP IPC event. That's ok b/c
            // we're still synced even if if Continue was called inside the callback.
            // That's because the StopContinueHolder bumped up the stopcount.
            // Corresponding AddRef() in CallFunction().
            // @todo - this is leaked if we don't get an EvalComplete event (eg, process exits with
            // in middle of func-eval).
            pEval->Release();
        }
        break;


    case DB_IPCE_NAME_CHANGE:
        {
            LOG((LF_CORDB, LL_INFO1000, "RCET::HRCE: Name Change %d  0x%p\n",
                 dwVolatileThreadId,
                 VmPtrToCookie(pEvent->NameChange.vmAppDomain)));

            pThread = NULL;
            pAppDomain.Clear();
            if (pEvent->NameChange.eventType == THREAD_NAME_CHANGE)
            {
                // Lookup the CordbThread that matches this runtime thread.
                if (!pEvent->NameChange.vmThread.IsNull())
                {
                    pThread = LookupOrCreateThread(pEvent->NameChange.vmThread);
                }
            }
            else
            {
                _ASSERTE (pEvent->NameChange.eventType == APP_DOMAIN_NAME_CHANGE);
                pAppDomain.Assign(LookupOrCreateAppDomain(pEvent->NameChange.vmAppDomain));
                if (pAppDomain)
                {
                    pAppDomain->InvalidateName();
                }
            }

            if (pThread || pAppDomain)
            {
                PUBLIC_CALLBACK_IN_THIS_SCOPE(this, pLockHolder, pEvent);
                pCallback1->NameChange(pAppDomain, pThread);
            }
        }

        break;

    case DB_IPCE_UPDATE_MODULE_SYMS:
        {
            RSExtSmartPtr<IStream> pStream;

            // Find the app domain the module lives in.
            _ASSERTE (pAppDomain != NULL);

            // Find the Right Side module for this module.
            CordbModule * pModule = pAppDomain->LookupOrCreateModule(pEvent->UpdateModuleSymsData.vmDomainAssembly);
            _ASSERTE(pModule != NULL);

            // This is a legacy event notification for updated PDBs.
            // Creates a new IStream object. Ownership is handed off via callback.
            IDacDbiInterface::SymbolFormat symFormat = pModule->GetInMemorySymbolStream(&pStream);

            // We shouldn't get this event if there aren't PDB symbols waiting.  Specifically we don't want
            // to incur the cost of copying over ILDB symbols here without the debugger asking for them.
            // Eventually we may remove this callback as well and always rely on explicit requests.
            _ASSERTE(symFormat == IDacDbiInterface::kSymbolFormatPDB);

            if (symFormat == IDacDbiInterface::kSymbolFormatPDB)
            {
                PUBLIC_CALLBACK_IN_THIS_SCOPE(this, pLockHolder, pEvent);

                _ASSERTE(pStream != NULL); // Shouldn't send the event if we don't have a stream.

                pCallback1->UpdateModuleSymbols(pAppDomain, pModule, pStream);
            }

        }
        break;

    case DB_IPCE_MDA_NOTIFICATION:
        {
            RSInitHolder<CordbMDA> pMDA(new CordbMDA(this, &pEvent->MDANotification)); // throws

            // Ctor leaves both internal + ext Ref at 0, adding to neuter list bumps int-ref up to 1.
            // Neutering will dump it back down to zero.
            this->AddToNeuterOnExitList(pMDA);

            // We bump up and down the external ref so that even if the callback doesn't touch the refs,
            // our Ext-Release here will still cause a 1->0 ext-ref transition, which will get it
            // swept on the neuter list.
            RSExtSmartPtr<ICorDebugMDA> pExternalMDARef;
            pMDA.TransferOwnershipExternal(&pExternalMDARef);
            {
                PUBLIC_CALLBACK_IN_THIS_SCOPE(this, pLockHolder, pEvent);

                pCallback2->MDANotification(
                    this,
                    pThread, // may be null
                    pExternalMDARef);

                // pExternalMDARef's dtor will do an external release,
                // which is very significant because it may be the one that does the 1->0 ext ref transition,
                // which may mean cause the "NeuterAtWill" bit to get flipped on this CordbMDA object.
                // Since this is an external release, do it in the PUBLIC_CALLBACK scope.
                pExternalMDARef.Clear();
            }

            break;
        }

    case DB_IPCE_CONTROL_C_EVENT:
        {
            hr = S_FALSE;

            {
                PUBLIC_CALLBACK_IN_THIS_SCOPE(this, pLockHolder, pEvent);
                hr = pCallback1->ControlCTrap((ICorDebugProcess*) this);
            }
        }
        break;

        // EnC Remap opportunity
        case DB_IPCE_ENC_REMAP:
        {
            LOG((LF_CORDB, LL_INFO1000, "[%x] RCET::DRCE: EnC Remap!.\n",
                 GetCurrentThreadId()));

            _ASSERTE(NULL != pAppDomain);

            CordbModule * pModule = pAppDomain->LookupOrCreateModule(pEvent->EnCRemap.vmDomainAssembly);
            PREFIX_ASSUME(pModule != NULL);

            CordbFunction * pCurFunction    = NULL;
            CordbFunction * pResumeFunction = NULL;

            // lookup the version of the function that we are mapping from
            // this is the one that is currently running
            pCurFunction = pModule->LookupOrCreateFunction(
                pEvent->EnCRemap.funcMetadataToken, pEvent->EnCRemap.currentVersionNumber);

            // lookup the version of the function that we are mapping to
            // it will always be the most recent
            pResumeFunction = pModule->LookupOrCreateFunction(
                    pEvent->EnCRemap.funcMetadataToken, pEvent->EnCRemap.resumeVersionNumber);

            _ASSERTE(pCurFunction->GetEnCVersionNumber() < pResumeFunction->GetEnCVersionNumber());

            RSSmartPtr<CordbFunction> pRefCurFunction(pCurFunction);
            RSSmartPtr<CordbFunction> pRefResumeFunction(pResumeFunction);

            // Verify we're not about to overwrite an outstanding remap IP
            // This should only be set while a remap opportunity is being handled,
            // and cleared (by CordbThread::MarkStackFramesDirty) on Continue.
            // We want to be absolutely sure we don't accidentally keep a stale pointer
            // around because it would point to arbitrary stack space in the CLR potentially
            // leading to stack corruption.
            _ASSERTE( pThread->m_EnCRemapFunctionIP == NULL );

            // Stash the address of the remap IP buffer.  This indicates that calling
            // RemapFunction is valid and provides a communications channel between the RS
            // and LS for the remap IL offset.
            pThread->m_EnCRemapFunctionIP = pEvent->EnCRemap.resumeILOffset;

            {
                PUBLIC_CALLBACK_IN_THIS_SCOPE(this, pLockHolder, pEvent);
                pCallback2->FunctionRemapOpportunity(
                    pAppDomain,
                    pThread,
                    pCurFunction,
                    pResumeFunction,
                    (ULONG32)pEvent->EnCRemap.currentILOffset);
            }

            // Implicit release on pCurFunction and pResumeFunction.
        }
        break;

        // EnC Remap complete
        case DB_IPCE_ENC_REMAP_COMPLETE:
        {
            LOG((LF_CORDB, LL_INFO1000, "[%x] RCET::DRCE: EnC Remap Complete!.\n",
                 GetCurrentThreadId()));

            _ASSERTE(NULL != pAppDomain);

            CordbModule* pModule = pAppDomain->LookupOrCreateModule(pEvent->EnCRemap.vmDomainAssembly);
            PREFIX_ASSUME(pModule != NULL);

            // Find the function we're remapping to, which must be the latest version
            CordbFunction *pRemapFunction=
                pModule->LookupFunctionLatestVersion(pEvent->EnCRemapComplete.funcMetadataToken);
            PREFIX_ASSUME(pRemapFunction != NULL);

            // Dispatch the FunctionRemapComplete callback
            RSSmartPtr<CordbFunction> pRef(pRemapFunction);
            {
                PUBLIC_CALLBACK_IN_THIS_SCOPE(this, pLockHolder, pEvent);
                pCallback2->FunctionRemapComplete(pAppDomain, pThread, pRemapFunction);
            }
            // Implicit release on pRemapFunction via holder
        }
        break;

        case DB_IPCE_BREAKPOINT_SET_ERROR:
        {
            LOG((LF_CORDB, LL_INFO1000, "RCET::DRCE: breakpoint set error.\n"));

            RSSmartPtr<CordbBreakpoint> pRef;

            _ASSERTE(pThread != NULL);
            _ASSERTE(pAppDomain != NULL);

            // Find the breakpoint object on this side.
            CordbBreakpoint * pBreakpoint = NULL;


            if (pThread == NULL)
            {
                // We've found cases out in the wild where we get this event on a thread we don't recognize.
                // We're not sure how this happens. Add a runtime check to protect ourselves to avoid the
                // an AV. We still assert because this should not be happening.
                // It likely means theres some issue where we failed to send a CreateThread notification.
                STRESS_LOG1(LF_CORDB, LL_INFO1000, "BreakpointSetError on unrecognized thread. %p\n", pBreakpoint);

                _ASSERTE(!"Missing thread on bp set error");
                break;
            }

            pBreakpoint = pAppDomain->m_breakpoints.GetBase(LsPtrToCookie(pEvent->BreakpointSetErrorData.breakpointToken));

            if (pBreakpoint != NULL)
            {
                ICorDebugBreakpoint * pIBreakpoint = CordbBreakpointToInterface(pBreakpoint);
                _ASSERTE(pIBreakpoint != NULL);
            {
                    PUBLIC_CALLBACK_IN_THIS_SCOPE2(this, pLockHolder, pEvent, "thread=0x%p, bp=0x%p", pThread, pBreakpoint);
                    pCallback1->BreakpointSetError(pAppDomain, pThread, pIBreakpoint, 0);
            }
            }
            // Implicit release on pRef.
        }
        break;


    case DB_IPCE_EXCEPTION_CALLBACK2:
        {
            STRESS_LOG4(LF_CORDB, LL_INFO100,
                "RCET::DRCE: Exception2 0x%p 0x%X 0x%X 0x%X\n",
                 pEvent->ExceptionCallback2.framePointer.GetSPValue(),
                 pEvent->ExceptionCallback2.nOffset,
                 pEvent->ExceptionCallback2.eventType,
                 pEvent->ExceptionCallback2.dwFlags
                 );

            if (pThread == NULL)
            {
                // We've got an exception on a thread we don't know about.  This could be a thread that
                // has never run any managed code, so let's just ignore the exception.  We should have
                // already sent a log message about this situation for the EXCEPTION callback above.
                _ASSERTE( pEvent->ExceptionCallback2.eventType == DEBUG_EXCEPTION_UNHANDLED );
                break;
            }

            pThread->SetExInfo(pEvent->ExceptionCallback2.vmExceptionHandle);

            //
            // Send all the information back to the debugger.
            //
            RSSmartPtr<CordbFrame> pFrame;

            FramePointer fp = pEvent->ExceptionCallback2.framePointer;
            if (fp != LEAF_MOST_FRAME)
            {
                // The interface forces us to to pass a FramePointer via an ICorDebugFrame.
                // However, we can't get a real ICDFrame without a stackwalk, and we don't
                // want to do a stackwalk now. so pass a netuered proxy frame. The shim
                // can map this to a real frame.
                // See comments at CordbPlaceHolderFrame class for details.
                pFrame.Assign(new CordbPlaceholderFrame(this, fp));
            }

            CorDebugExceptionCallbackType type = pEvent->ExceptionCallback2.eventType;
            {
                PUBLIC_CALLBACK_IN_THIS_SCOPE3(this, pLockHolder, pEvent, "pThread=0x%p, frame=%p, type=%d", pThread, (ICorDebugFrame*) pFrame, type);
                hr = pCallback2->Exception(
                    pThread->m_pAppDomain,
                    pThread,
                    pFrame,
                    (ULONG32)(pEvent->ExceptionCallback2.nOffset),
                    type,
                    pEvent->ExceptionCallback2.dwFlags);
            }
        }
        break;

    case DB_IPCE_EXCEPTION_UNWIND:
        {
            STRESS_LOG2(LF_CORDB, LL_INFO100,
                "RCET::DRCE: Exception Unwind 0x%X 0x%X\n",
                 pEvent->ExceptionCallback2.eventType,
                 pEvent->ExceptionCallback2.dwFlags
                 );

            if (pThread == NULL)
            {
                // We've got an exception on a thread we don't know about.  This probably should never
                // happen (if it's unwinding, then we expect a managed frame on the stack, and so we should
                // know about the thread), but if it does fall back to ignoring the exception.
                _ASSERTE( !"Got unwind event for unknown exception" );
                break;
            }

            //
            // Send all the information back to the debugger.
            //
            {
                PUBLIC_CALLBACK_IN_THIS_SCOPE1(this, pLockHolder, pEvent, "pThread=0x%p", pThread);
                hr = pCallback2->ExceptionUnwind(
                    pThread->m_pAppDomain,
                    pThread,
                    pEvent->ExceptionUnwind.eventType,
                    pEvent->ExceptionUnwind.dwFlags);
            }
        }
        break;


    case DB_IPCE_INTERCEPT_EXCEPTION_COMPLETE:
        {
            STRESS_LOG0(LF_CORDB, LL_INFO100, "RCET::DRCE: Exception Interception Complete.\n");

            if (pThread == NULL)
            {
                // We've got an exception on a thread we don't know about.  This probably should never
                // happen (if it's unwinding, then we expect a managed frame on the stack, and so we should
                // know about the thread), but if it does fall back to ignoring the exception.
                _ASSERTE( !"Got complete event for unknown exception" );
                break;
            }

            //
            // Tell the debugger that the exception has been intercepted.  This is similar to the
            // notification we give when we start unwinding for a non-intercepted exception, except that the
            // interception has been completed at this point, which means that we are conceptually at the end
            // of the second pass.
            //
            {
                PUBLIC_CALLBACK_IN_THIS_SCOPE1(this, pLockHolder, pEvent, "pThread=0x%p", pThread);
                hr = pCallback2->ExceptionUnwind(
                    pThread->m_pAppDomain,
                    pThread,
                    DEBUG_EXCEPTION_INTERCEPTED,
                    0);
            }
        }
        break;
#ifdef TEST_DATA_CONSISTENCY
    case DB_IPCE_TEST_CRST:
        {
            EX_TRY
            {
                // the left side has signaled that we should test whether pEvent->TestCrstData.vmCrst is held
                GetDAC()->TestCrst(pEvent->TestCrstData.vmCrst);
            }
            EX_CATCH_HRESULT(hr);

            if (pEvent->TestCrstData.fOkToTake)
            {
                _ASSERTE(hr == S_OK);
                if (hr != S_OK)
                {
                    // we want to catch this in retail builds too
                    ThrowHR(E_FAIL);
                }
            }
            else // the lock was already held
            {
                // see if we threw because the lock was held
                _ASSERTE(hr == CORDBG_E_PROCESS_NOT_SYNCHRONIZED);
                if (hr != CORDBG_E_PROCESS_NOT_SYNCHRONIZED)
                {
                    // we want to catch this in retail builds too
                    ThrowHR(E_FAIL);
                }
            }

        }
        break;

    case DB_IPCE_TEST_RWLOCK:
        {
            EX_TRY
            {
                // the left side has signaled that we should test whether pEvent->TestRWLockData.vmRWLock is held
                GetDAC()->TestRWLock(pEvent->TestRWLockData.vmRWLock);
            }
            EX_CATCH_HRESULT(hr);

            if (pEvent->TestRWLockData.fOkToTake)
            {
                _ASSERTE(hr == S_OK);
                if (hr != S_OK)
                {
                    // we want to catch this in retail builds too
                    ThrowHR(E_FAIL);
                }
            }
            else // the lock was already held
            {
                // see if we threw because the lock was held
                _ASSERTE(hr == CORDBG_E_PROCESS_NOT_SYNCHRONIZED);
                if (hr != CORDBG_E_PROCESS_NOT_SYNCHRONIZED)
                {
                    // we want to catch this in retail builds too
                    ThrowHR(E_FAIL);
                }
            }
        }
        break;
#endif

    default:
        _ASSERTE(!"Unknown event");
        LOG((LF_CORDB, LL_INFO1000,
             "[%x] RCET::HRCE: Unknown event: 0x%08x\n",
             GetCurrentThreadId(), pEvent->type));
    }


    FinishEventDispatch();
}
#ifdef _PREFAST_
#pragma warning(pop)
#endif

//---------------------------------------------------------------------------------------
// Callback for prepopulating threads.
//
// Arguments:
//    vmThread - thread as part of the eunmeration.
//    pUserData - data supplied with callback. It's a CordbProcess* object.
//

// static
void CordbProcess::ThreadEnumerationCallback(VMPTR_Thread vmThread, void * pUserData)
{
    CordbProcess * pThis = reinterpret_cast<CordbProcess *> (pUserData);
    INTERNAL_DAC_CALLBACK(pThis);

    STRESS_LOG0(LF_CORDB, LL_INFO1000, "ThreadEnumerationCallback()\n");

    // Do lookup / lazy-create.
    pThis->LookupOrCreateThread(vmThread);
}

//---------------------------------------------------------------------------------------
// Fully build up the CordbThread cache to match VM state.
void CordbProcess::PrepopulateThreadsOrThrow()
{
    RSLockHolder lockHolder(GetProcessLock());
    if (IsDacInitialized())
    {
        STRESS_LOG0(LF_CORDB, LL_INFO1000, "PrepopulateThreadsOrThrow()\n");
        GetDAC()->EnumerateThreads(ThreadEnumerationCallback, this);
    }
}

//---------------------------------------------------------------------------------------
// Create a Thread enumerator
//
// Arguments:
//     pOwnerObj - object (a CordbProcess or CordbThread) that will own the enumerator.
//     pOwnerList - the neuter list that the enumerator will live on
//     pHolder - an outparameter for the enumerator to be initialized.
//
void CordbProcess::BuildThreadEnum(CordbBase * pOwnerObj, NeuterList * pOwnerList, RSInitHolder<CordbHashTableEnum> * pHolder)
{
    CordbHashTableEnum::BuildOrThrow(
        pOwnerObj,
        pOwnerList,
        &m_userThreads,
        IID_ICorDebugThreadEnum,
        pHolder);
}

// Public implementation of ICorDebugProcess::EnumerateThreads
HRESULT CordbProcess::EnumerateThreads(ICorDebugThreadEnum **ppThreads)
{
    HRESULT hr = S_OK;
    PUBLIC_API_BEGIN(this);
    {
        if (m_detached)
        {
            // #Detach_Check:
            //
            // FUTURE: Consider adding this IF block to the PUBLIC_API macros so that
            // typical public APIs fail quickly if we're trying to do a detach.  For
            // now, I'm hand-adding this check only to the few problematic APIs that get
            // called while queuing the fake attach events.  In these cases, it is not
            // enough to check if CordbProcess::IsNeutered(), as the detaching thread
            // may have begun the detaching and neutering process, but not be
            // finished--in which case m_detached is true, but
            // CordbProcess::IsNeutered() is still false.
            ThrowHR(CORDBG_E_PROCESS_DETACHED);
        }

        ValidateOrThrow(ppThreads);

        RSInitHolder<CordbHashTableEnum> pEnum;
        InternalEnumerateThreads(pEnum.GetAddr());

        pEnum.TransferOwnershipExternal(ppThreads);
    }
    PUBLIC_API_END(hr);
    return hr;
}

// Internal implementation of EnumerateThreads
VOID CordbProcess::InternalEnumerateThreads(RSInitHolder<CordbHashTableEnum> *ppThreads)
{
    INTERNAL_API_ENTRY(this);
    // Needs to prepopulate
    PrepopulateThreadsOrThrow();
    BuildThreadEnum(this, this->GetContinueNeuterList(), ppThreads);
}

// Implementation of ICorDebugProcess::GetThread
HRESULT CordbProcess::GetThread(DWORD dwThreadId, ICorDebugThread **ppThread)
{
    PUBLIC_API_ENTRY(this);
    VALIDATE_POINTER_TO_OBJECT(ppThread, ICorDebugThread **);

    // No good pre-existing ATT_* contract for this.
    // Because for legacy, we have to allow this on the win32 event thread.
    *ppThread = NULL;

    HRESULT hr = S_OK;
    EX_TRY
    {
        RSLockHolder lockHolder(GetProcessLock());
        if (m_detached)
        {
            // See code:CordbProcess::EnumerateThreads#Detach_Check
            ThrowHR(CORDBG_E_PROCESS_DETACHED);
        }
        CordbThread * pThread = TryLookupOrCreateThreadByVolatileOSId(dwThreadId);
        if (pThread == NULL)
        {
            // This is a common case because we may be looking up an unmanaged thread.
            hr = E_INVALIDARG;
        }
        else
        {
            *ppThread = static_cast<ICorDebugThread*> (pThread);
            pThread->ExternalAddRef();
        }
    }
    EX_CATCH_HRESULT(hr);

    LOG((LF_CORDB, LL_INFO10000, "CP::GT returns id=0x%x hr=0x%x ppThread=0x%p",
             dwThreadId, hr, *ppThread));
    return hr;
}

HRESULT CordbProcess::ThreadForFiberCookie(DWORD fiberCookie,
                                           ICorDebugThread **ppThread)
{
    return E_NOTIMPL;
}

HRESULT CordbProcess::GetHelperThreadID(DWORD *pThreadID)
{
    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);

    _ASSERTE(m_pShim != NULL);
    if (pThreadID == NULL)
    {
        return (E_INVALIDARG);
    }

    HRESULT hr = S_OK;
    // Return the ID of the current helper thread. There may be no thread in the process, or there may be a true helper
    // thread.
    if ((m_helperThreadId != 0) && !m_helperThreadDead)
    {
        *pThreadID = m_helperThreadId;
    }
    else if ((GetDCB() != NULL) && (GetDCB()->m_helperThreadId != 0))
    {
        EX_TRY
        {
            // be sure we have the latest information
            UpdateRightSideDCB();
            *pThreadID = GetDCB()->m_helperThreadId;
        }
        EX_CATCH_HRESULT(hr);

    }
    else
    {
        *pThreadID = 0;
    }

    return hr;
}

//---------------------------------------------------------------------------------------
//
// Sends IPC event to set all the managed threads, except for the one given, to the given state
//
// Arguments:
//     state - The state to set the threads to.
//     pExceptThread - The thread to not set.  This is usually the thread that is currently
//         sending an IPC event to the RS, and should be excluded.
//
// Return Value:
//     Typical HRESULT semantics, nothing abnormal.
//
HRESULT CordbProcess::SetAllThreadsDebugState(CorDebugThreadState state,
                                              ICorDebugThread * pExceptThread)
{
    PUBLIC_REENTRANT_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT_OR_NULL(pExceptThread, ICorDebugThread *);
    ATT_REQUIRE_STOPPED_MAY_FAIL(this);

    if (GetShim() == NULL)
    {
        return E_NOTIMPL;
    }
    CordbThread * pCordbExceptThread = static_cast<CordbThread *> (pExceptThread);

    LOG((LF_CORDB, LL_INFO1000, "CP::SATDS: except thread=0x%08x 0x%x\n",
         pExceptThread,
         (pCordbExceptThread != NULL) ? pCordbExceptThread->m_id : 0));

    // Send one event to the Left Side to twiddle each thread's state.
    DebuggerIPCEvent event;

    InitIPCEvent(&event, DB_IPCE_SET_ALL_DEBUG_STATE, true, VMPTR_AppDomain::NullPtr());

    event.SetAllDebugState.vmThreadToken = ((pCordbExceptThread != NULL) ?
                                            pCordbExceptThread->m_vmThreadToken : VMPTR_Thread::NullPtr());

    event.SetAllDebugState.debugState = state;

    HRESULT hr = SendIPCEvent(&event, sizeof(DebuggerIPCEvent));

    hr = WORST_HR(hr, event.hr);

    // If that worked, then loop over all the threads on this side and set their states.
    if (SUCCEEDED(hr))
    {
        RSLockHolder lockHolder(GetProcessLock());
        HASHFIND hashFind;
        CordbThread * pThread;

        // We don't need to prepopulate here (to collect LS state) because we're just updating RS state.
        for (pThread = m_userThreads.FindFirst(&hashFind);
              pThread != NULL;
              pThread = m_userThreads.FindNext(&hashFind))
        {
            if (pThread != pCordbExceptThread)
            {
                pThread->m_debugState = state;
            }
        }
    }

    return hr;
}


HRESULT CordbProcess::EnumerateObjects(ICorDebugObjectEnum **ppObjects)
{
    /* !!! */
    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT(ppObjects, ICorDebugObjectEnum **);

    return E_NOTIMPL;
}

//---------------------------------------------------------------------------------------
//
// Determines if the target address is a "CLR transition stub".
//
// Arguments:
//     address - The address of an instruction to check in the target address space.
//     pfTransitionStub - Space to store the result, TRUE if the address belongs to a
//         transition stub, FALSE if not.  Only valid if this method returns a success code.
//
// Return Value:
//     Typical HRESULT semantics, nothing abnormal.
//
//---------------------------------------------------------------------------------------
HRESULT CordbProcess::IsTransitionStub(CORDB_ADDRESS address, BOOL *pfTransitionStub)
{
    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT(pfTransitionStub, BOOL *);

    // Default to FALSE
    *pfTransitionStub = FALSE;

    if (this->m_helperThreadDead)
    {
        return S_OK;
    }

    // If we're not initialized, then it can't be a stub...
    if (!m_initialized)
    {
        return S_OK;
    }

    ATT_REQUIRE_STOPPED_MAY_FAIL(this);

    HRESULT hr = S_OK;
    EX_TRY
    {
        DebuggerIPCEvent eventData;

        InitIPCEvent(&eventData, DB_IPCE_IS_TRANSITION_STUB, true, VMPTR_AppDomain::NullPtr());

        eventData.IsTransitionStub.address = CORDB_ADDRESS_TO_PTR(address);

        hr = SendIPCEvent(&eventData, sizeof(eventData));
        hr = WORST_HR(hr, eventData.hr);
        IfFailThrow(hr);

        _ASSERTE(eventData.type == DB_IPCE_IS_TRANSITION_STUB_RESULT);

        *pfTransitionStub = eventData.IsTransitionStubResult.isStub;
        LOG((LF_CORDB, LL_INFO1000, "CP::ITS: addr=0x%p result=%d\n", address, *pfTransitionStub));
        // @todo - beware that IsTransitionStub has a very important sideeffect - it synchronizes the runtime!
        // This for example covers an OS bug where SetThreadContext may silently fail if we're not synchronized.
        // (See IMDArocess::SetThreadContext for details on that bug).
        // If we ever stop using IPC events here and only use DAC; we need to be aware of that.

        // Check against DAC primitives
        {
            BOOL fIsStub2 = GetDAC()->IsTransitionStub(address);
            (void)fIsStub2; //prevent "unused variable" error from GCC
            CONSISTENCY_CHECK_MSGF(*pfTransitionStub == fIsStub2, ("IsStub2 failed, DAC2:%d, IPC:%d, addr:0x%p", (int) fIsStub2, (int) *pfTransitionStub, CORDB_ADDRESS_TO_PTR(address)));

        }
    }
    EX_CATCH_HRESULT(hr);
    if(FAILED(hr))
    {
        LOG((LF_CORDB, LL_INFO1000, "CP::ITS: FAILED hr=0x%x\n", hr));
    }
    return hr;
}


HRESULT CordbProcess::SetStopState(DWORD threadID, CorDebugThreadState state)
{
    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    return E_NOTIMPL;
}

HRESULT CordbProcess::IsOSSuspended(DWORD threadID, BOOL *pbSuspended)
{
    PUBLIC_API_ENTRY(this);
    // Gotta have a place for the result!
    if (!pbSuspended)
        return E_INVALIDARG;

    FAIL_IF_NEUTERED(this);

#ifdef FEATURE_INTEROP_DEBUGGING
    RSLockHolder lockHolder(GetProcessLock());

    // Have we seen this thread?
    CordbUnmanagedThread *ut = GetUnmanagedThread(threadID);

    // If we have, and if we've suspended it, then say so.
    if (ut && ut->IsSuspended())
    {
        *pbSuspended = TRUE;
    }
    else
    {
        *pbSuspended = FALSE;
    }
#else
    // Not interop-debugging, we never OS suspend.
    *pbSuspended = FALSE;
#endif
    return S_OK;
}

//
// This routine reads a thread context from the process being debugged, taking into account the fact that the context
// record may be a different size than the one we compiled with. On systems < NT5, then OS doesn't usually allocate
// space for the extended registers. However, the CONTEXT struct that we compile with does have this space.
//
HRESULT CordbProcess::SafeReadThreadContext(LSPTR_CONTEXT pContext, DT_CONTEXT * pCtx)
{
    HRESULT hr = S_OK;

    INTERNAL_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);

    EX_TRY
    {

        void *pRemoteContext = pContext.UnsafeGet();
        TargetBuffer tbFull(pRemoteContext, sizeof(DT_CONTEXT));

        // The context may have 2 parts:
        // 1. Base register, which are always present.
        // 2. Optional extended registers, which are only present if CONTEXT_EXTENDED_REGISTERS is set
        //    in the flags.

        // At a minimum we have room for a whole context up to the extended registers.
    #if defined(DT_CONTEXT_EXTENDED_REGISTERS)
        ULONG32 minContextSize = offsetof(DT_CONTEXT, ExtendedRegisters);
    #else
        ULONG32 minContextSize = sizeof(DT_CONTEXT);
    #endif

        // Read the minimum part.
        TargetBuffer tbMin = tbFull.SubBuffer(0, minContextSize);
        SafeReadBuffer(tbMin, (BYTE*) pCtx);

    #if defined(DT_CONTEXT_EXTENDED_REGISTERS)
        void *pCurExtReg = (void*)((UINT_PTR)pCtx + minContextSize);
        TargetBuffer tbExtended = tbFull.SubBuffer(minContextSize);

        // Now, read the extended registers if the context contains them. If the context does not have extended registers,
        // just set them to zero.
        if (SUCCEEDED(hr) && (pCtx->ContextFlags & CONTEXT_EXTENDED_REGISTERS) == CONTEXT_EXTENDED_REGISTERS)
        {
            SafeReadBuffer(tbExtended, (BYTE*) pCurExtReg);
        }
        else
        {
            memset(pCurExtReg, 0, tbExtended.cbSize);
        }
    #endif

    }
    EX_CATCH_HRESULT(hr);
    return hr;
}

//
// This routine writes a thread context to the process being debugged, taking into account the fact that the context
// record may be a different size than the one we compiled with. On systems < NT5, then OS doesn't usually allocate
// space for the extended registers. However, the CONTEXT struct that we compile with does have this space.
//
HRESULT CordbProcess::SafeWriteThreadContext(LSPTR_CONTEXT pContext, const DT_CONTEXT * pCtx)
{
    INTERNAL_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);

    HRESULT hr = S_OK;
    DWORD sizeToWrite = sizeof(DT_CONTEXT);

    BYTE * pRemoteContext = (BYTE*) pContext.UnsafeGet();
    BYTE * pCtxSource = (BYTE*) pCtx;


#if defined(DT_CONTEXT_EXTENDED_REGISTERS)
    // If our context has extended registers, then write the whole thing. Otherwise, just write the minimum part.
    if ((pCtx->ContextFlags & DT_CONTEXT_EXTENDED_REGISTERS) != DT_CONTEXT_EXTENDED_REGISTERS)
    {
        sizeToWrite = offsetof(DT_CONTEXT, ExtendedRegisters);
    }
#endif

// 64 bit windows puts space for the first 6 stack parameters in the CONTEXT structure so that
// kernel to usermode transitions don't have to allocate a CONTEXT and do a separate sub rsp
// to allocate stack spill space for the arguments. This means that writing to P1Home - P6Home
// will overwrite the arguments of some function higher on the stack, very bad. Conceptually you
// can think of these members as not being part of the context, ie they don't represent something
// which gets saved or restored on context switches. They are just space we shouldn't overwrite.
// See issue 630276 for more details.
#if defined TARGET_AMD64
    pRemoteContext += offsetof(CONTEXT, ContextFlags); // immediately follows the 6 parameters P1-P6
    pCtxSource += offsetof(CONTEXT, ContextFlags);
    sizeToWrite -= offsetof(CONTEXT, ContextFlags);
#endif

    EX_TRY
    {
        // Write the context.
        TargetBuffer tb(pRemoteContext, sizeToWrite);
        SafeWriteBuffer(tb, (const BYTE*) pCtxSource);
    }
    EX_CATCH_HRESULT(hr);

    return hr;
}


HRESULT CordbProcess::GetThreadContext(DWORD threadID, ULONG32 contextSize, BYTE context[])
{
    PUBLIC_REENTRANT_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    LOG((LF_CORDB, LL_INFO10000, "CP::GTC: thread=0x%x\n", threadID));

    DT_CONTEXT * pContext;

    if (contextSize != sizeof(DT_CONTEXT))
    {
        LOG((LF_CORDB, LL_INFO10000, "CP::GTC: thread=0x%x, context size is invalid.\n", threadID));
        return E_INVALIDARG;
    }

    pContext = reinterpret_cast<DT_CONTEXT *>(context);

    VALIDATE_POINTER_TO_OBJECT_ARRAY(context, BYTE, contextSize, true, true);

    if (this->IsInteropDebugging())
    {
#ifdef FEATURE_INTEROP_DEBUGGING
        RSLockHolder lockHolder(GetProcessLock());

        // Find the unmanaged thread
        CordbUnmanagedThread *ut = GetUnmanagedThread(threadID);

        if (ut == NULL)
        {
            LOG((LF_CORDB, LL_INFO10000, "CP::GTC: thread=0x%x, thread id is invalid.\n", threadID));

            return E_INVALIDARG;
        }

        return ut->GetThreadContext((DT_CONTEXT*)context);
#else
        return E_NOTIMPL;
#endif
    }
    else
    {
        RSLockHolder ch(GetProcess()->GetStopGoLock());
        RSLockHolder lockHolder(GetProcessLock());

        HRESULT hr = S_OK;
        EX_TRY
        {
            CordbThread* thread = this->TryLookupThreadByVolatileOSId(threadID);
            if (thread == NULL)
            {
                LOG((LF_CORDB, LL_INFO10000, "CP::GTC: thread=0x%x, thread id is invalid.\n", threadID));

                hr = E_INVALIDARG;
            }
            else
            {
                DT_CONTEXT* managedContext;
                hr = thread->GetManagedContext(&managedContext);
                *pContext = *managedContext;
            }
        }
        EX_CATCH_HRESULT(hr)
        return hr;
    }
}

// Public implementation of ICorDebugProcess::SetThreadContext.
// @dbgtodo interop-debugging: this should go away in V3. Use the data-target instead. This is
// interop-debugging aware (and cooperates with hijacks)
HRESULT CordbProcess::SetThreadContext(DWORD threadID, ULONG32 contextSize, BYTE context[])
{
    PUBLIC_REENTRANT_API_ENTRY(this);

    HRESULT hr = S_OK;

    // @todo -  could we look at the context flags and return E_INVALIDARG if they're bad?
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT_ARRAY(context, BYTE, contextSize, true, true);

    if (contextSize != sizeof(DT_CONTEXT))
    {
        LOG((LF_CORDB, LL_INFO10000, "CP::STC: thread=0x%x, context size is invalid.\n", threadID));
        return E_INVALIDARG;
    }

    DT_CONTEXT* pContext = (DT_CONTEXT*)context;

    if (this->IsInteropDebugging())
    {
#ifdef FEATURE_INTEROP_DEBUGGING
        RSLockHolder lockHolder(GetProcessLock());

        CordbUnmanagedThread *ut = NULL;

        // Find the unmanaged thread
        ut = GetUnmanagedThread(threadID);

        if (ut == NULL)
        {
            LOG((LF_CORDB, LL_INFO10000, "CP::STC: thread=0x%x, thread is invalid.\n", threadID));
            return E_INVALIDARG;
        }

        hr = ut->SetThreadContext(pContext);

        // Update the register set for the leaf-unmanaged chain so that it's consistent w/ the context.
        // We may not necessarily be synchronized, and so these frames may be stale. Even so, no harm done.
        if (SUCCEEDED(hr))
        {
            // @dbgtodo stackwalk: this should all disappear with V3 stackwalker and getting rid of SetThreadContext.
            EX_TRY
            {
                // Find the managed thread.  Returns NULL if thread is not managed.
                // If we don't have a thread prveiously cached, then there's no state to update.
                CordbThread * pThread = TryLookupThreadByVolatileOSId(threadID);

                if (pThread != NULL)
                {
                    // In V2, we used to update the CONTEXT of the leaf chain if the chain is an unmanaged chain.
                    // In Arrowhead, we just force a cleanup of the stackwalk cache.  This is a more correct
                    // thing to do anyway, since the CONTEXT being set could be anything.
                    pThread->CleanupStack();
                }
            }
            EX_CATCH_HRESULT(hr);
        }
#else
        return E_NOTIMPL;
#endif
    }
    else
    {
        RSLockHolder ch(GetProcess()->GetStopGoLock());
        RSLockHolder lockHolder(GetProcessLock());

        EX_TRY
        {
            CordbThread* thread = this->TryLookupThreadByVolatileOSId(threadID);
            if (thread == NULL)
            {
                LOG((LF_CORDB, LL_INFO10000, "CP::GTC: thread=0x%x, thread id is invalid.\n", threadID));

                hr = E_INVALIDARG;
            }

            hr = thread->SetManagedContext(pContext);
        }
        EX_CATCH
        {
            hr = E_FAIL;
        }
        EX_END_CATCH(SwallowAllExceptions)


    }
    return hr;
}


// @dbgtodo  ICDProcess - When we DACize this function, we should use code:DacReplacePatches
HRESULT CordbProcess::ReadMemory(CORDB_ADDRESS address,
                                 DWORD size,
                                 BYTE buffer[],
                                 SIZE_T *read)
{
    PUBLIC_REENTRANT_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);

    // A read of 0 bytes is okay.
    if (size == 0)
        return S_OK;

    VALIDATE_POINTER_TO_OBJECT_ARRAY(buffer, BYTE, size, true, true);
    VALIDATE_POINTER_TO_OBJECT(buffer, SIZE_T *);

    if (address == NULL)
        return E_INVALIDARG;

    // If no read parameter is supplied, we ignore it. This matches the semantics of kernel32!ReadProcessMemory.
    SIZE_T dummyRead;
    if (read == NULL)
    {
        read = &dummyRead;
    }
    *read = 0;

    HRESULT hr = S_OK;

    CORDBRequireProcessStateOK(this);

    // Grab the memory we want to read
    // Note that this will return success on a partial read
    ULONG32 cbRead;
    hr = GetDataTarget()->ReadVirtual(address, buffer, size, &cbRead);
    if (FAILED(hr))
    {
        hr = CORDBG_E_READVIRTUAL_FAILURE;
        goto LExit;
    }

    // Read at least one byte
    *read = (SIZE_T) cbRead;

    // There seem to be strange cases where ReadProcessMemory will return a seemingly negative number into *read, which
    // is an unsigned value. So we check the sanity of *read by ensuring that its no bigger than the size we tried to
    // read.
    if ((*read > 0) && (*read <= size))
    {
        LOG((LF_CORDB, LL_INFO100000, "CP::RM: read %d bytes from 0x%08x, first byte is 0x%x\n",
             *read, (DWORD)address, buffer[0]));

        if (m_initialized)
        {
            RSLockHolder ch(&this->m_processMutex);

            // If m_pPatchTable is NULL, then it's been cleaned out b/c of a Continue for the left side.  Get the table
            // again. Only do this, of course, if the managed state of the process is initialized.
            if (m_pPatchTable == NULL)
            {
                hr = RefreshPatchTable(address, *read, buffer);
            }
            else
            {
                // The previously fetched table is still good, so run through it & see if any patches are applicable
                hr = AdjustBuffer(address, *read, buffer, NULL, AB_READ);
            }
        }
    }

LExit:
    if (FAILED(hr))
    {
        RSLockHolder ch(&this->m_processMutex);
        ClearPatchTable();
    }
    else if (*read < size)
    {
        // Unlike the DT api, our API is supposed to return an error on partial read
        hr = HRESULT_FROM_WIN32(ERROR_PARTIAL_COPY);
    }
    return hr;
}

// Update patches & buffer to make the left-side's usage of patches transparent
// to our client.  Behavior depends on AB_MODE:
// AB_READ:
// - use the RS patch table structure to replace patch opcodes in buffer.
// AB_WRITE:
// - update the RS patch table structure w/ new replace-opcode values
// if we've written over them. And put the int3 back in for write-memory.
//
// Note: If we're writing memory over top of a patch, then it must be JITted or stub code.
// Writing over JITed or Stub code can be dangerous since the CLR may not expect it
// (eg. JIT data structures about the code layout may be incorrect), but in certain
// narrow cases it may be safe (eg. replacing a constant).  VS says they wouldn't expect
// this to work, but we'll keep the support in for legacy reasons.
//
// address, size - describe buffer in LS memory
// buffer - local copy of buffer that will be read/written from/to LS.
// bufferCopy - for writeprocessmemory, copy of original buffer (w/o injected patches)
// pbUpdatePatchTable - flag if patchtable got dirty and needs to be updated.
HRESULT CordbProcess::AdjustBuffer( CORDB_ADDRESS address,
                                    SIZE_T size,
                                    BYTE buffer[],
                                    BYTE **bufferCopy,
                                    AB_MODE mode,
                                    BOOL *pbUpdatePatchTable)
{
    INTERNAL_API_ENTRY(this);

    _ASSERTE(m_initialized);
    _ASSERTE(this->ThreadHoldsProcessLock());

    if (    address == NULL
         || size == NULL
         || buffer == NULL
         || (mode != AB_READ && mode != AB_WRITE) )
        return E_INVALIDARG;

    if (pbUpdatePatchTable != NULL )
        *pbUpdatePatchTable = FALSE;

    // If we don't have a patch table loaded, then return S_OK since there are no patches to adjust
    if (m_pPatchTable == NULL)
        return S_OK;

    //is the requested memory completely out-of-range?
    if ((m_minPatchAddr > (address + (size - 1))) ||
        (m_maxPatchAddr < address))
    {
        return S_OK;
    }

    // Without runtime offsets, we can't adjust - this should only ever happen on dumps, where there's
    // no W32ET to get the offsets, and so they stay zeroed
    if (!m_runtimeOffsetsInitialized)
        return S_OK;

    LOG((LF_CORDB,LL_INFO10000, "CordbProcess::AdjustBuffer at addr 0x%p\n", address));

    if (mode == AB_WRITE)
    {
        // We don't want to mess up the original copy of the buffer, so
        // for right now, just copy it wholesale.
        (*bufferCopy) = new (nothrow) BYTE[size];
        if (NULL == (*bufferCopy))
            return E_OUTOFMEMORY;

        memmove((*bufferCopy), buffer, size);
    }

    ULONG iNextFree = m_iFirstPatch;
    while( iNextFree != DPT_TERMINATING_INDEX )
    {
        BYTE *DebuggerControllerPatch = m_pPatchTable + m_runtimeOffsets.m_cbPatch*iNextFree;
        PRD_TYPE opcode = *(PRD_TYPE *)(DebuggerControllerPatch + m_runtimeOffsets.m_offOpcode);
        CORDB_ADDRESS patchAddress = PTR_TO_CORDB_ADDRESS(*(BYTE**)(DebuggerControllerPatch + m_runtimeOffsets.m_offAddr));

        if (IsPatchInRequestedRange(address, size, patchAddress))
        {
            if (mode == AB_READ)
            {
                CORDbgSetInstructionEx(buffer, address, patchAddress, opcode, size);
            }
            else if (mode == AB_WRITE)
            {
                _ASSERTE( pbUpdatePatchTable != NULL );
                _ASSERTE( bufferCopy != NULL );

                //There can be multiple patches at the same address: we don't want 2nd+ patches to get the
                // break opcode, so we read from the unmodified copy.
                m_rgUncommittedOpcode[iNextFree] =
                    CORDbgGetInstructionEx(*bufferCopy, address, patchAddress, opcode, size);

                //put the breakpoint into the memory itself
                CORDbgInsertBreakpointEx(buffer, address, patchAddress, opcode, size);

                *pbUpdatePatchTable = TRUE;
            }
            else
                _ASSERTE( !"CordbProcess::AdjustBuffergiven non(Read|Write) mode!" );
        }

        iNextFree = m_rgNextPatch[iNextFree];
    }

    // If we created a copy of the buffer but didn't modify it, then free it now.
    if( ( mode == AB_WRITE ) && ( !*pbUpdatePatchTable ) )
    {
        delete [] *bufferCopy;
        *bufferCopy = NULL;
    }

    return S_OK;
}


void CordbProcess::CommitBufferAdjustments( CORDB_ADDRESS start,
                                            CORDB_ADDRESS end )
{
    INTERNAL_API_ENTRY(this);

    _ASSERTE(m_initialized);
    _ASSERTE(this->ThreadHoldsProcessLock());
    _ASSERTE(m_runtimeOffsetsInitialized);

    ULONG iPatch = m_iFirstPatch;
    while( iPatch != DPT_TERMINATING_INDEX )
    {
        BYTE *DebuggerControllerPatch = m_pPatchTable +
            m_runtimeOffsets.m_cbPatch*iPatch;

        BYTE *patchAddress = *(BYTE**)(DebuggerControllerPatch + m_runtimeOffsets.m_offAddr);

        if (IsPatchInRequestedRange(start, (SIZE_T)(end - start), PTR_TO_CORDB_ADDRESS(patchAddress)) &&
            !PRDIsBreakInst(&(m_rgUncommittedOpcode[iPatch])))
        {
            //copy this back to the copy of the patch table
            *(PRD_TYPE *)(DebuggerControllerPatch + m_runtimeOffsets.m_offOpcode) =
                m_rgUncommittedOpcode[iPatch];
        }

        iPatch = m_rgNextPatch[iPatch];
    }
}

void CordbProcess::ClearBufferAdjustments( )
{
    INTERNAL_API_ENTRY(this);
    _ASSERTE(this->ThreadHoldsProcessLock());

    ULONG iPatch = m_iFirstPatch;
    while( iPatch != DPT_TERMINATING_INDEX )
    {
        InitializePRDToBreakInst(&(m_rgUncommittedOpcode[iPatch]));
        iPatch = m_rgNextPatch[iPatch];
    }
}

void CordbProcess::ClearPatchTable(void )
{
    INTERNAL_API_ENTRY(this);
    _ASSERTE(this->ThreadHoldsProcessLock());

    if (m_pPatchTable != NULL )
    {
        delete [] m_pPatchTable;
        m_pPatchTable = NULL;

        delete [] m_rgNextPatch;
        m_rgNextPatch = NULL;

        delete [] m_rgUncommittedOpcode;
        m_rgUncommittedOpcode = NULL;

        m_iFirstPatch = DPT_TERMINATING_INDEX;
        m_minPatchAddr = MAX_ADDRESS;
        m_maxPatchAddr = MIN_ADDRESS;
        m_rgData = NULL;
        m_cPatch = 0;
    }
}

HRESULT CordbProcess::RefreshPatchTable(CORDB_ADDRESS address, SIZE_T size, BYTE buffer[])
{
    CONTRACTL
    {
        NOTHROW;
    }
    CONTRACTL_END;

    INTERNAL_API_ENTRY(this);
    _ASSERTE(m_initialized);
    _ASSERTE(this->ThreadHoldsProcessLock());

    HRESULT hr = S_OK;
    BYTE *rgb = NULL;

    // All of m_runtimeOffsets will be zeroed out if there's been no call to code:CordbProcess::GetRuntimeOffsets.
    // Thus for things to work, we'd have to have a live target that went and got the real values.
    // For dumps, things are still all zeroed out because we don't have any events sent to the W32ET, don't
    // have a live process to investigate, etc.
    if (!m_runtimeOffsetsInitialized)
        return S_OK;

    _ASSERTE( m_runtimeOffsets.m_cbOpcode == sizeof(PRD_TYPE) );

    CORDBRequireProcessStateOK(this);

    if (m_pPatchTable == NULL )
    {
        // First, check to be sure the patch table is valid on the Left Side. If its not, then we won't read it.
        BOOL fPatchTableValid = FALSE;

        hr = SafeReadStruct(PTR_TO_CORDB_ADDRESS(m_runtimeOffsets.m_pPatchTableValid), &fPatchTableValid);
        if (FAILED(hr) || !fPatchTableValid)
        {
            LOG((LF_CORDB, LL_INFO10000, "Wont refresh patch table because its not valid now.\n"));
            return S_OK;
        }

        SIZE_T offStart = 0;
        SIZE_T offEnd = 0;
        UINT cbTableSlice = 0;

        // Grab the patch table info
        offStart = min(m_runtimeOffsets.m_offRgData, m_runtimeOffsets.m_offCData);
        offEnd   = max(m_runtimeOffsets.m_offRgData, m_runtimeOffsets.m_offCData) + sizeof(SIZE_T);
        cbTableSlice = (UINT)(offEnd - offStart);

        if (cbTableSlice == 0)
        {
            LOG((LF_CORDB, LL_INFO10000, "Wont refresh patch table because its not valid now.\n"));
            return S_OK;
        }

        EX_TRY
        {
            rgb = new BYTE[cbTableSlice]; // throws

            TargetBuffer tbSlice((BYTE*)m_runtimeOffsets.m_pPatches + offStart, cbTableSlice);
            this->SafeReadBuffer(tbSlice, rgb); // Throws;

            // Note that rgData is a pointer in the left side address space
            m_rgData = *(BYTE**)(rgb + m_runtimeOffsets.m_offRgData - offStart);
            m_cPatch = *(ULONG*)(rgb + m_runtimeOffsets.m_offCData - offStart);

            // Grab the patch table
            UINT cbPatchTable = (UINT)(m_cPatch * m_runtimeOffsets.m_cbPatch);

            if (cbPatchTable == 0)
            {
                LOG((LF_CORDB, LL_INFO10000, "Wont refresh patch table because its not valid now.\n"));
                _ASSERTE(hr == S_OK);
                goto LExit; // can't return since we're in a Try/Catch
            }

            // Throwing news
            m_pPatchTable = new BYTE[ cbPatchTable ];
            m_rgNextPatch = new ULONG[m_cPatch];
            m_rgUncommittedOpcode = new PRD_TYPE[m_cPatch];

            TargetBuffer tb(m_rgData, cbPatchTable);
            this->SafeReadBuffer(tb, m_pPatchTable); // Throws

            //As we go through the patch table we do a number of things:
            //
            // 1. collect min,max address seen for quick fail check
            //
            // 2. Link all valid entries into a linked list, the first entry of which is m_iFirstPatch
            //
            // 3. Initialize m_rgUncommittedOpcode, so that we can undo local patch table changes if WriteMemory can't write
            // atomically.
            //
            // 4. If the patch is in the memory we grabbed, unapply it.

            ULONG iDebuggerControllerPatchPrev = DPT_TERMINATING_INDEX;

            m_minPatchAddr = MAX_ADDRESS;
            m_maxPatchAddr = MIN_ADDRESS;
            m_iFirstPatch = DPT_TERMINATING_INDEX;

            for (ULONG iPatch = 0; iPatch < m_cPatch;iPatch++)
            {
                // <REVISIT_TODO>@todo port: we're making assumptions about the size of opcodes,address pointers, etc</REVISIT_TODO>
                BYTE *DebuggerControllerPatch = m_pPatchTable + m_runtimeOffsets.m_cbPatch * iPatch;
                PRD_TYPE opcode = *(PRD_TYPE*)(DebuggerControllerPatch + m_runtimeOffsets.m_offOpcode);
                CORDB_ADDRESS patchAddress = PTR_TO_CORDB_ADDRESS(*(BYTE**)(DebuggerControllerPatch + m_runtimeOffsets.m_offAddr));

                // A non-zero opcode indicates to us that this patch is valid.
                if (!PRDIsEmpty(opcode))
                {
                    _ASSERTE( patchAddress != 0 );

                    // (1), above
                    // Note that GetPatchEndAddr() returns the address immediately AFTER the patch,
                    // so we have to subtract 1 from it below.
                    if (m_minPatchAddr > patchAddress )
                        m_minPatchAddr = patchAddress;
                    if (m_maxPatchAddr < patchAddress )
                        m_maxPatchAddr = GetPatchEndAddr(patchAddress) - 1;

                    // (2), above
                    if ( m_iFirstPatch == DPT_TERMINATING_INDEX)
                    {
                        m_iFirstPatch = iPatch;
                        _ASSERTE( iPatch != DPT_TERMINATING_INDEX);
                    }

                    if (iDebuggerControllerPatchPrev != DPT_TERMINATING_INDEX)
                    {
                        m_rgNextPatch[iDebuggerControllerPatchPrev] = iPatch;
                    }

                    iDebuggerControllerPatchPrev = iPatch;

                    // (3), above
                    InitializePRDToBreakInst(&(m_rgUncommittedOpcode[iPatch]));

                    // (4), above
                    if (IsPatchInRequestedRange(address, size, patchAddress))
                    {
                        _ASSERTE( buffer != NULL );
                        _ASSERTE( size != NULL );


                        //unapply the patch here.
                        CORDbgSetInstructionEx(buffer, address, patchAddress, opcode, size);
                    }

                }
            }

            if (iDebuggerControllerPatchPrev != DPT_TERMINATING_INDEX)
            {
                m_rgNextPatch[iDebuggerControllerPatchPrev] = DPT_TERMINATING_INDEX;
            }
        }
LExit:
    ;
        EX_CATCH_HRESULT(hr);
    }


    if (rgb != NULL )
    {
        delete [] rgb;
    }

    if (FAILED( hr ) )
    {
        ClearPatchTable();
    }

    return hr;
}

//---------------------------------------------------------------------------------------
//
// Given an address, see if there is a patch in the patch table that matches it and return
// if its an unmanaged patch or not.
//
// Arguments:
//     address - The address of an instruction to check in the target address space.
//     pfPatchFound - Space to store the result, TRUE if the address belongs to a
//         patch, FALSE if not.  Only valid if this method returns a success code.
//     pfPatchIsUnmanaged - Space to store the result, TRUE if the address is a patch
//         and the patch is unmanaged, FALSE if not.  Only valid if this method returns a
//         success code.
//
// Return Value:
//     Typical HRESULT semantics, nothing abnormal.
//
// Note: this method is pretty in-efficient. It refreshes the patch table, then scans it.
//     Refreshing the patch table involves a scan, too, so this method could be folded
//     with that.
//
//---------------------------------------------------------------------------------------
HRESULT CordbProcess::FindPatchByAddress(CORDB_ADDRESS address, bool *pfPatchFound, bool *pfPatchIsUnmanaged)
{
    INTERNAL_API_ENTRY(this);
    _ASSERTE(ThreadHoldsProcessLock());
    _ASSERTE((pfPatchFound != NULL) && (pfPatchIsUnmanaged != NULL));
    _ASSERTE(m_runtimeOffsetsInitialized);
    FAIL_IF_NEUTERED(this);

    *pfPatchFound = false;
    *pfPatchIsUnmanaged = false;

    // First things first. If the process isn't initialized, then there can be no patch table, so we know the breakpoint
    // doesn't belong to the Runtime.
    if (!m_initialized)
    {
        return S_OK;
    }

    // This method is called from the main loop of the win32 event thread in response to a first chance breakpoint event
    // that we know is not a flare. The process has been runnning, and it may have invalidated the patch table, so we'll
    // flush it here before refreshing it to make sure we've got the right thing.
    //
    // Note: we really should have the Left Side mark the patch table dirty to help optimize this.
    ClearPatchTable();

    // Refresh the patch table.
    HRESULT hr = RefreshPatchTable();

    if (FAILED(hr))
    {
        LOG((LF_CORDB, LL_INFO1000, "CP::FPBA: failed to refresh the patch table\n"));
        return hr;
    }

    // If there is no patch table yet, then we know there is no patch at the given address, so return S_OK with
    // *patchFound = false.
    if (m_pPatchTable == NULL)
    {
        LOG((LF_CORDB, LL_INFO1000, "CP::FPBA: no patch table\n"));
        return S_OK;
    }

    // Scan the patch table for a matching patch.
    for (ULONG iNextPatch = m_iFirstPatch; iNextPatch != DPT_TERMINATING_INDEX; iNextPatch = m_rgNextPatch[iNextPatch])
    {
        BYTE *patch = m_pPatchTable + (m_runtimeOffsets.m_cbPatch * iNextPatch);
        BYTE *patchAddress = *(BYTE**)(patch + m_runtimeOffsets.m_offAddr);
        DWORD traceType = *(DWORD*)(patch + m_runtimeOffsets.m_offTraceType);

        if (address == PTR_TO_CORDB_ADDRESS(patchAddress))
        {
            *pfPatchFound = true;

            if (traceType == m_runtimeOffsets.m_traceTypeUnmanaged)
            {
                *pfPatchIsUnmanaged = true;

#if defined(_DEBUG)
                HRESULT hrDac = S_OK;
                EX_TRY
                {
                    // We should be able to double check w/ DAC that this really is outside of the runtime.
                    IDacDbiInterface::AddressType addrType = GetDAC()->GetAddressType(address);
                    CONSISTENCY_CHECK_MSGF(addrType == IDacDbiInterface::kAddressUnrecognized, ("Bad address type = %d", addrType));
                }
                EX_CATCH_HRESULT(hrDac);
                CONSISTENCY_CHECK_MSGF(SUCCEEDED(hrDac), ("DAC::GetAddressType failed, hr=0x%08x", hrDac));
#endif
            }

            break;
        }
    }

    // If we didn't find a patch, its actually still possible that this breakpoint exception belongs to us. There are
    // races with very large numbers of threads entering the Runtime through the same managed function. We will have
    // multiple threads adding and removing ref counts to an int 3 in the code stream. Sometimes, this count will go to
    // zero and the int 3 will be removed, then it will come back up and the int 3 will be replaced. The in-process
    // logic takes pains to ensure that such cases are handled properly, therefore we need to perform the same check
    // here to make the correct decision. Basically, the check is to see if there is indeed an int 3 at the exception
    // address. If there is _not_ an int 3 there, then we've hit this race. We will lie and say a managed patch was
    // found to cover this case. This is tracking the logic in DebuggerController::ScanForTriggers, where we call
    // IsPatched.
    if (*pfPatchFound == false)
    {
        // Read one instruction from the faulting address...
#if defined(TARGET_ARM) || defined(TARGET_ARM64)
        PRD_TYPE TrapCheck = 0;
#else
        BYTE TrapCheck = 0;
#endif

        HRESULT hr2 = SafeReadStruct(address, &TrapCheck);

        if (SUCCEEDED(hr2) && (TrapCheck != CORDbg_BREAK_INSTRUCTION))
        {
            LOG((LF_CORDB, LL_INFO1000, "CP::FPBA: patchFound=true based on odd missing int 3 case.\n"));

            *pfPatchFound = true;
        }
    }

    LOG((LF_CORDB, LL_INFO1000, "CP::FPBA: patchFound=%d, patchIsUnmanaged=%d\n", *pfPatchFound, *pfPatchIsUnmanaged));

    return S_OK;
}

HRESULT CordbProcess::WriteMemory(CORDB_ADDRESS address, DWORD size,
                                  BYTE buffer[], SIZE_T *written)
{
    PUBLIC_REENTRANT_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    CORDBRequireProcessStateOK(this);
    _ASSERTE(m_runtimeOffsetsInitialized);


    if (size == 0 || address == NULL)
        return E_INVALIDARG;

    VALIDATE_POINTER_TO_OBJECT_ARRAY(buffer, BYTE, size, true, true);
    VALIDATE_POINTER_TO_OBJECT(written, SIZE_T *);


#if defined(_DEBUG) && defined(FEATURE_INTEROP_DEBUGGING)
    // Shouldn't be using this to write int3. Use UM BP API instead.
    // This is technically legal (what if the '0xcc' is data or something), so we can't fail in retail.
    // But we can add this debug-only check to help VS migrate to the new API.
    static ConfigDWORD configCheckInt3;
    DWORD fCheckInt3 = configCheckInt3.val(CLRConfig::INTERNAL_DbgCheckInt3);
    if (fCheckInt3)
    {
#if defined(TARGET_X86) || defined(TARGET_AMD64)
        if (size == 1 && buffer[0] == 0xCC)
        {
            CONSISTENCY_CHECK_MSGF(false,
                ("You're using ICorDebugProcess::WriteMemory() to write an 'int3' (1 byte 0xCC) at address 0x%p.\n"
                "If you're trying to set a breakpoint, you should be using ICorDebugProcess::SetUnmanagedBreakpoint() instead.\n"
                "(This assert is only enabled under the COM+ knob DbgCheckInt3.)\n",
                CORDB_ADDRESS_TO_PTR(address)));
        }
#endif // TARGET_X86 || TARGET_AMD64

        // check if we're replaced an opcode.
        if (size == 1)
        {
            RSLockHolder ch(&this->m_processMutex);

            NativePatch * p = GetNativePatch(CORDB_ADDRESS_TO_PTR(address));
            if (p != NULL)
            {
            CONSISTENCY_CHECK_MSGF(false,
                ("You're using ICorDebugProcess::WriteMemory() to write an 'opcode (0x%x)' at address 0x%p.\n"
                "There's already a native patch at that address from ICorDebugProcess::SetUnmanagedBreakpoint().\n"
                "If you're trying to remove the breakpoint, use ICDProcess::ClearUnmanagedBreakpoint() instead.\n"
                "(This assert is only enabled under the COM+ knob DbgCheckInt3.)\n",
                (DWORD) (buffer[0]), CORDB_ADDRESS_TO_PTR(address)));
            }
        }
    }
#endif // _DEBUG && FEATURE_INTEROP_DEBUGGING


    *written = 0;

    HRESULT hr = S_OK;
    HRESULT hrSaved = hr; // this will hold the 'real' hresult in case of a
                          // partially completed operation
    HRESULT hrPartialCopy = HRESULT_FROM_WIN32(ERROR_PARTIAL_COPY);

    BOOL bUpdateOriginalPatchTable = FALSE;
    BYTE *bufferCopy = NULL;

    // Only update the patch table if the managed state of the process
    // is initialized.
    if (m_initialized)
    {
        RSLockHolder ch(&this->m_processMutex);

        if (m_pPatchTable == NULL )
        {
            if (!SUCCEEDED( hr = RefreshPatchTable() ) )
            {
                goto LExit;
            }
        }

        if ( !SUCCEEDED( hr = AdjustBuffer( address,
                                            size,
                                            buffer,
                                            &bufferCopy,
                                            AB_WRITE,
                                            &bUpdateOriginalPatchTable)))
        {
            goto LExit;
        }
    }

    //conveniently enough, SafeWriteBuffer will throw if it can't complete the entire operation
    EX_TRY
    {
        TargetBuffer tb(address, size);
        SafeWriteBuffer(tb, buffer); // throws
        *written = tb.cbSize; // DT's Write does everything or fails.
    }
    EX_CATCH_HRESULT(hr);

    if (FAILED(hr))
    {
        if(hr != hrPartialCopy)
            goto LExit;
        else
            hrSaved = hr;
    }


    LOG((LF_CORDB, LL_INFO100000, "CP::WM: wrote %d bytes at 0x%08x, first byte is 0x%x\n",
         *written, (DWORD)address, buffer[0]));

    if (bUpdateOriginalPatchTable == TRUE )
    {
        {
            RSLockHolder ch(&this->m_processMutex);

            //don't tweak patch table for stuff that isn't written to LeftSide
            CommitBufferAdjustments(address, address + *written);
        }

        // The only way this should be able to fail is if
        //someone else fiddles with the memory protections on the
        //left side while it's frozen
        EX_TRY
        {
            TargetBuffer tb(m_rgData, (ULONG) (m_cPatch*m_runtimeOffsets.m_cbPatch));
            SafeWriteBuffer(tb, m_pPatchTable);
        }
        EX_CATCH_HRESULT(hr);
        SIMPLIFYING_ASSUMPTION_SUCCEEDED(hr);
    }

    // Since we may have
    // overwritten anything (objects, code, etc), we should mark
    // everything as needing to be re-cached.
    m_continueCounter++;

 LExit:
    if (m_initialized)
    {
        RSLockHolder ch(&this->m_processMutex);
        ClearBufferAdjustments( );
    }

    //we messed up our local copy, so get a clean copy the next time
    //we need it
    if (bUpdateOriginalPatchTable==TRUE)
    {
        if (bufferCopy != NULL)
        {
            memmove(buffer, bufferCopy, size);
            delete [] bufferCopy;
        }
    }

    if (FAILED( hr ))
    {
        //we messed up our local copy, so get a clean copy the next time
        //we need it
        if (bUpdateOriginalPatchTable==TRUE)
        {
            RSLockHolder ch(&this->m_processMutex);
            ClearPatchTable();
        }
    }
    else if( FAILED(hrSaved) )
    {
        hr = hrSaved;
    }

    return hr;
}

HRESULT CordbProcess::ClearCurrentException(DWORD threadID)
{
#ifndef FEATURE_INTEROP_DEBUGGING
    return E_INVALIDARG;
#else
    PUBLIC_API_ENTRY(this);

    RSLockHolder lockHolder(GetProcessLock());

    // There's something wrong if you're calling this an there are no queued unmanaged events.
    if ((m_unmanagedEventQueue == NULL) && (m_outOfBandEventQueue == NULL))
        return E_INVALIDARG;

    // Grab the unmanaged thread object.
    CordbUnmanagedThread *pUThread = GetUnmanagedThread(threadID);

    if (pUThread == NULL)
        return E_INVALIDARG;

    LOG((LF_CORDB, LL_INFO1000, "CP::CCE: tid=0x%x\n", threadID));

    // We clear both the IB and OOB event.
    if (pUThread->HasIBEvent() && !pUThread->IBEvent()->IsEventUserContinued())
    {
        pUThread->IBEvent()->SetState(CUES_ExceptionCleared);
    }

    if (pUThread->HasOOBEvent())
    {
        // must decide exception status _before_ we continue the event.
        _ASSERTE(!pUThread->OOBEvent()->IsEventContinuedUnhijacked());
        pUThread->OOBEvent()->SetState(CUES_ExceptionCleared);
    }

    // If the thread is hijacked, then set the thread's debugger word to 0 to indicate to it that the
    // exception has been cleared.
    if (pUThread->IsGenericHijacked())
    {
        HRESULT hr = pUThread->SetEEDebuggerWord(0);
        _ASSERTE(SUCCEEDED(hr));
    }

    return S_OK;
#endif // FEATURE_INTEROP_DEBUGGING
}

#ifdef FEATURE_INTEROP_DEBUGGING
CordbUnmanagedThread *CordbProcess::HandleUnmanagedCreateThread(DWORD dwThreadId, HANDLE hThread, void *lpThreadLocalBase)
{
    INTERNAL_API_ENTRY(this);
    CordbUnmanagedThread *ut = new (nothrow) CordbUnmanagedThread(this, dwThreadId, hThread, lpThreadLocalBase);

    if (ut != NULL)
    {
        HRESULT hr = m_unmanagedThreads.AddBase(ut); // InternalAddRef, release on EXIT_THREAD events.

        if (!SUCCEEDED(hr))
        {
            delete ut;
            ut = NULL;

            LOG((LF_CORDB, LL_INFO10000, "Failed adding unmanaged thread to process!\n"));
            CORDBSetUnrecoverableError(this, hr, 0);
        }
    }
    else
    {
        LOG((LF_CORDB, LL_INFO10000, "New CordbThread failed!\n"));
        CORDBSetUnrecoverableError(this, E_OUTOFMEMORY, 0);
    }

    return ut;
}
#endif // FEATURE_INTEROP_DEBUGGING


//-----------------------------------------------------------------------------
// Initializes the DAC
// Arguments: none--initializes the DAC for this CordbProcess instance
// Note: Throws on error
//-----------------------------------------------------------------------------
void CordbProcess::InitDac()
{
    // Go-Go DAC power!!
    HRESULT hr = S_OK;
    EX_TRY
    {
        InitializeDac();
    }
    EX_CATCH_HRESULT(hr);

    // We Need DAC to debug for both Managed & Interop.
    if (FAILED(hr))
    {
        // We assert here b/c we're trying to be friendly. Most likely, the cause is either:
        // - a bad installation
        // - a CLR dev built mscorwks but didn't build DAC.
        SIMPLIFYING_ASSUMPTION_MSGF(false, ("Failed to load DAC while for debugging. hr=0x%08x", hr));
        ThrowHR(hr);
    }
} //CordbProcess::InitDac

// Update the entire RS copy of the debugger control block by reading the LS copy. The RS copy is treated as
// a throw-away temporary buffer, rather than a true cache. That is, we make no assumptions about the
// validity of the information over time. Thus, before using any of the values, we need to update it. We
// update everything for simplicity; any perf hit we take by doing this instead of updating the individual
// fields we want at any given point isn't significant, particularly if we are updating multiple fields.

// Arguments:
//     none, but reads process memory from the LS debugger control block
// Return Value: none (copies from LS DCB to RS buffer GetDCB())
// Note: throws if SafeReadBuffer fails
void CordbProcess::UpdateRightSideDCB()
{
    IfFailThrow(m_pEventChannel->UpdateRightSideDCB());
} // CordbProcess::UpdateRightSideDCB

// Update a single field with a value stored in the RS copy of the DCB. We can't update the entire LS DCB
// because in some cases, the LS and RS are simultaneously initializing the DCB. If we initialize a field on
// the RS and write back the whole thing, we may overwrite something the LS has initialized in the interim.

// Arguments:
//     input: rsFieldAddr - the address of the field in the RS copy of the DCB that we want to write back to
//                          the LS DCB. We use this to compute the offset of the field from the beginning of the
//                          DCB and then add this offset to the starting address of the LS DCB to get the LS
//                          address of the field we are updating
//            size        - the size of the field we're updating.
// Return value: none
// Note: throws if SafeWriteBuffer fails
void CordbProcess::UpdateLeftSideDCBField(void * rsFieldAddr, SIZE_T size)
{
    IfFailThrow(m_pEventChannel->UpdateLeftSideDCBField(rsFieldAddr, size));
} // CordbProcess::UpdateRightSideDCB


//-----------------------------------------------------------------------------
// Gets the remote address of the event block for the Target and verifies that it's valid.
// We use this address when we need to read from or write to the debugger control block.
// Also allocates the RS buffer used for temporary storage for information from the DCB and
// copies the LS DCB into the RS buffer.
// Arguments:
//     output: pfBlockExists - true iff the LS DCB has been successfully allocated.  Note that
//             we need this information even if the function throws, so we can't simply send it back
//             as a return value.
// Return value:
//     None, but allocates GetDCB() on success. If the LS DCB has not
//     been successfully initialized or if this throws, GetDCB() will be NULL.
//
// Notes:
//     Throws on error
//
//-----------------------------------------------------------------------------
void CordbProcess::GetEventBlock(BOOL * pfBlockExists)
{
    if (GetDCB() == NULL) // we only need to do this once
    {
        _ASSERTE(m_pShim != NULL);
        _ASSERTE(ThreadHoldsProcessLock());

        // This will Initialize the DAC/DBI interface.
        BOOL fDacReady = TryInitializeDac();

        if (fDacReady)
        {
            // Ensure that we have a DAC interface.
            _ASSERTE(m_pDacPrimitives != NULL);

            // This is not technically necessary for Mac debugging.  The event channel doesn't rely on
            // knowing the target address of the DCB on the LS.
            CORDB_ADDRESS pLeftSideDCB = NULL;
            pLeftSideDCB = (GetDAC()->GetDebuggerControlBlockAddress());
            if (pLeftSideDCB == NULL)
            {
                *pfBlockExists = false;
                ThrowHR(CORDBG_E_DEBUGGING_NOT_POSSIBLE);
            }

            IfFailThrow(NewEventChannelForThisPlatform(pLeftSideDCB,
                                                       m_pMutableDataTarget,
                                                       GetProcessDescriptor(),
                                                       m_pShim->GetMachineInfo(),
                                                       &m_pEventChannel));
            _ASSERTE(m_pEventChannel != NULL);

            // copy information from left side DCB
            UpdateRightSideDCB();

            // Verify that the control block is valid.
            // This  will throw on error.
            VerifyControlBlock();

            *pfBlockExists = true;
        }
        else
        {
            // we can't initialize the DAC, so we can't get the block
            *pfBlockExists = false;
        }
    }
    else // we got the block before
    {
        *pfBlockExists = true;
    }

} // CordbProcess::GetEventBlock()


//
// Verify that the version info in the control block matches what we expect. The minimum supported protocol from the
// Left Side must be greater or equal to the minimum required protocol of the Right Side. Note: its the Left Side's job
// to conform to whatever protocol the Right Side requires, so long as minimum is supported.
//
void CordbProcess::VerifyControlBlock()
{
    INTERNAL_API_ENTRY(this);
    _ASSERTE(m_pShim != NULL);

    if (GetDCB()->m_DCBSize == 0)
    {
        // the LS is still initializing the DCB
        ThrowHR(CORDBG_E_DEBUGGING_NOT_POSSIBLE);
    }

    // Fill in the protocol numbers for the Right Side and update the LS DCB.
    GetDCB()->m_rightSideProtocolCurrent = CorDB_RightSideProtocolCurrent;
    UpdateLeftSideDCBField(&(GetDCB()->m_rightSideProtocolCurrent), sizeof(GetDCB()->m_rightSideProtocolCurrent));

    GetDCB()->m_rightSideProtocolMinSupported = CorDB_RightSideProtocolMinSupported;
    UpdateLeftSideDCBField(&(GetDCB()->m_rightSideProtocolMinSupported),
                           sizeof(GetDCB()->m_rightSideProtocolMinSupported));

    // Dbi and Wks have a more flexible versioning allowed, as described by the Debugger
    // Version Protocol String in DEBUGGER_PROTOCOL_STRING in DbgIpcEvents.h. This allows different build
    // numbers, but the other protocol numbers should still match.

    // These assertions verify that the debug manager is behaving correctly.
    // An assertion failure here means that the runtime version of the debuggee is different from the runtime version of
    // the debugger is capable of debugging.

    // The Debug Manager should properly match LS & RS, and thus guarantee that this assert should never fire.
    // But just in case the installation is corrupted, we'll check it.
    if (GetDCB()->m_DCBSize != sizeof(DebuggerIPCControlBlock))
    {
        CONSISTENCY_CHECK_MSGF(false, ("DCB in LS is %d bytes, in RS is %d bytes. Version mismatch!!\n",
                               GetDCB()->m_DCBSize, sizeof(DebuggerIPCControlBlock)));
        ThrowHR(CORDBG_E_INCOMPATIBLE_PROTOCOL);
    }

    // The Left Side has to support at least our minimum required protocol.
    if (GetDCB()->m_leftSideProtocolCurrent < GetDCB()->m_rightSideProtocolMinSupported)
    {
        _ASSERTE(GetDCB()->m_leftSideProtocolCurrent >= GetDCB()->m_rightSideProtocolMinSupported);
        ThrowHR(CORDBG_E_INCOMPATIBLE_PROTOCOL);
    }

    // The Left Side has to be able to emulate at least our minimum required protocol.
    if (GetDCB()->m_leftSideProtocolMinSupported > GetDCB()->m_rightSideProtocolCurrent)
    {
        _ASSERTE(GetDCB()->m_leftSideProtocolMinSupported <= GetDCB()->m_rightSideProtocolCurrent);
        ThrowHR(CORDBG_E_INCOMPATIBLE_PROTOCOL);
    }

#ifdef _DEBUG
    char buf[MAX_LONGPATH];
    DWORD len = GetEnvironmentVariableA("CORDBG_NotCompatibleTest", buf, sizeof(buf));
    _ASSERTE(len < sizeof(buf));

    if (len > 0)
        ThrowHR(CORDBG_E_INCOMPATIBLE_PROTOCOL);
#endif

    if (GetDCB()->m_bHostingInFiber)
    {
        ThrowHR(CORDBG_E_CANNOT_DEBUG_FIBER_PROCESS);
    }

    _ASSERTE(!GetDCB()->m_rightSideShouldCreateHelperThread);
} // CordbProcess::VerifyControlBlock

//-----------------------------------------------------------------------------
// This is the CordbProcess objects chance to inspect the DCB and initialize stuff
//
// Return Value:
//     Typical HRESULT return values, nothing abnormal.
//     If succeeded, then the block exists and is valid.
//
//-----------------------------------------------------------------------------
HRESULT CordbProcess::GetRuntimeOffsets()
{
    INTERNAL_API_ENTRY(this);

    _ASSERTE(m_pShim != NULL);
    UpdateRightSideDCB();

    // Can't get a handle to the helper thread if the target is remote.

    // If we got this far w/o failing, then we should be able to get the helper thread handle.
    // RS will handle not having the helper-thread handle, so we just make a best effort here.
    DWORD dwHelperTid = GetDCB()->m_realHelperThreadId;
    _ASSERTE(dwHelperTid != 0);


    {
#if TARGET_UNIX
        m_hHelperThread = NULL; //RS is supposed to be able to live without a helper thread handle.
#else
        m_hHelperThread = OpenThread(SYNCHRONIZE, FALSE, dwHelperTid);
        CONSISTENCY_CHECK_MSGF(m_hHelperThread != NULL, ("Failed to get helper-thread handle. tid=0x%x\n", dwHelperTid));
#endif
    }

    // get the remote address of the runtime offsets structure and read the structure itself
    HRESULT hrRead = SafeReadStruct(PTR_TO_CORDB_ADDRESS(GetDCB()->m_pRuntimeOffsets), &m_runtimeOffsets);

    if (FAILED(hrRead))
    {
        return hrRead;
    }

    LOG((LF_CORDB, LL_INFO10000, "CP::GRO: got runtime offsets: \n"));

#ifdef FEATURE_INTEROP_DEBUGGING
    LOG((LF_CORDB, LL_INFO10000, "    m_genericHijackFuncAddr=          0x%p\n",
         m_runtimeOffsets.m_genericHijackFuncAddr));
    LOG((LF_CORDB, LL_INFO10000, "    m_signalHijackStartedBPAddr=      0x%p\n",
         m_runtimeOffsets.m_signalHijackStartedBPAddr));
    LOG((LF_CORDB, LL_INFO10000, "    m_excepNotForRuntimeBPAddr=       0x%p\n",
         m_runtimeOffsets.m_excepNotForRuntimeBPAddr));
    LOG((LF_CORDB, LL_INFO10000, "    m_notifyRSOfSyncCompleteBPAddr=   0x%p\n",
         m_runtimeOffsets.m_notifyRSOfSyncCompleteBPAddr));
    LOG((LF_CORDB, LL_INFO10000, "    m_debuggerWordTLSIndex=           0x%08x\n",
         m_runtimeOffsets.m_debuggerWordTLSIndex));
#endif // FEATURE_INTEROP_DEBUGGING

    LOG((LF_CORDB, LL_INFO10000, "    m_TLSIndex=                       0x%08x\n",
         m_runtimeOffsets.m_TLSIndex));
    LOG((LF_CORDB, LL_INFO10000, "    m_EEThreadStateOffset=            0x%08x\n",
         m_runtimeOffsets.m_EEThreadStateOffset));
    LOG((LF_CORDB, LL_INFO10000, "    m_EEThreadStateNCOffset=          0x%08x\n",
         m_runtimeOffsets.m_EEThreadStateNCOffset));
    LOG((LF_CORDB, LL_INFO10000, "    m_EEThreadPGCDisabledOffset=      0x%08x\n",
         m_runtimeOffsets.m_EEThreadPGCDisabledOffset));
    LOG((LF_CORDB, LL_INFO10000, "    m_EEThreadPGCDisabledValue=       0x%08x\n",
         m_runtimeOffsets.m_EEThreadPGCDisabledValue));
    LOG((LF_CORDB, LL_INFO10000, "    m_EEThreadFrameOffset=            0x%08x\n",
         m_runtimeOffsets.m_EEThreadFrameOffset));
    LOG((LF_CORDB, LL_INFO10000, "    m_EEThreadMaxNeededSize=          0x%08x\n",
         m_runtimeOffsets.m_EEThreadMaxNeededSize));
    LOG((LF_CORDB, LL_INFO10000, "    m_EEThreadSteppingStateMask=      0x%08x\n",
         m_runtimeOffsets.m_EEThreadSteppingStateMask));
    LOG((LF_CORDB, LL_INFO10000, "    m_EEMaxFrameValue=                0x%08x\n",
         m_runtimeOffsets.m_EEMaxFrameValue));
    LOG((LF_CORDB, LL_INFO10000, "    m_EEThreadDebuggerFilterContextOffset= 0x%08x\n",
         m_runtimeOffsets.m_EEThreadDebuggerFilterContextOffset));
    LOG((LF_CORDB, LL_INFO10000, "    m_EEFrameNextOffset=              0x%08x\n",
         m_runtimeOffsets.m_EEFrameNextOffset));
    LOG((LF_CORDB, LL_INFO10000, "    m_EEIsManagedExceptionStateMask=  0x%08x\n",
         m_runtimeOffsets.m_EEIsManagedExceptionStateMask));
    LOG((LF_CORDB, LL_INFO10000, "    m_pPatches=                       0x%08x\n",
         m_runtimeOffsets.m_pPatches));
    LOG((LF_CORDB, LL_INFO10000, "    m_offRgData=                      0x%08x\n",
         m_runtimeOffsets.m_offRgData));
    LOG((LF_CORDB, LL_INFO10000, "    m_offCData=                       0x%08x\n",
         m_runtimeOffsets.m_offCData));
    LOG((LF_CORDB, LL_INFO10000, "    m_cbPatch=                        0x%08x\n",
         m_runtimeOffsets.m_cbPatch));
    LOG((LF_CORDB, LL_INFO10000, "    m_offAddr=                        0x%08x\n",
         m_runtimeOffsets.m_offAddr));
    LOG((LF_CORDB, LL_INFO10000, "    m_offOpcode=                      0x%08x\n",
         m_runtimeOffsets.m_offOpcode));
    LOG((LF_CORDB, LL_INFO10000, "    m_cbOpcode=                       0x%08x\n",
         m_runtimeOffsets.m_cbOpcode));
    LOG((LF_CORDB, LL_INFO10000, "    m_offTraceType=                   0x%08x\n",
         m_runtimeOffsets.m_offTraceType));
    LOG((LF_CORDB, LL_INFO10000, "    m_traceTypeUnmanaged=             0x%08x\n",
         m_runtimeOffsets.m_traceTypeUnmanaged));

#ifdef FEATURE_INTEROP_DEBUGGING
    // Flares are only used for interop debugging.

    // Do check that the flares are all at unique offsets.
    // Since this is determined at link-time, we need a run-time check (an
    // assert isn't good enough, since this would only happen in a super
    // optimized / bbt run).
    {
        const void * flares[] = {
            m_runtimeOffsets.m_signalHijackStartedBPAddr,
            m_runtimeOffsets.m_excepForRuntimeHandoffStartBPAddr,
            m_runtimeOffsets.m_excepForRuntimeHandoffCompleteBPAddr,
            m_runtimeOffsets.m_signalHijackCompleteBPAddr,
            m_runtimeOffsets.m_excepNotForRuntimeBPAddr,
            m_runtimeOffsets.m_notifyRSOfSyncCompleteBPAddr,
        };

        const int NumFlares = ARRAY_SIZE(flares);

        // Ensure that all of the flares are unique.
        for(int i = 0; i < NumFlares; i++)
        {
            for(int j = i+1; j < NumFlares; j++)
            {
                if (flares[i] == flares[j])
                {
                    // If we ever fail here, that means the LS build is busted.

                    // This assert is useful if we drop a checked RS onto a retail
                    // LS (that's legal).
                    _ASSERTE(!"LS has matching Flares.");
                    LOG((LF_CORDB, LL_ALWAYS, "Failing because of matching flares.\n"));
                    return CORDBG_E_INCOMPATIBLE_PROTOCOL;
                }
            }
        }
    }

#endif  // FEATURE_INTEROP_DEBUGGING
    m_runtimeOffsetsInitialized = true;
    return S_OK;
}

#ifdef FEATURE_INTEROP_DEBUGGING

//-----------------------------------------------------------------------------
// Resume hijacked threads.
//-----------------------------------------------------------------------------
void CordbProcess::ResumeHijackedThreads()
{
    INTERNAL_API_ENTRY(this);
    _ASSERTE(m_pShim != NULL);
    _ASSERTE(ThreadHoldsProcessLock());

    LOG((LF_CORDB, LL_INFO10000, "CP::RHT: entered\n"));
    if (this->m_state & (CordbProcess::PS_SOME_THREADS_SUSPENDED | CordbProcess::PS_HIJACKS_IN_PLACE))
    {
        // On XP, This will also resume the threads suspended for Sync.
        this->ResumeUnmanagedThreads();
    }

    // Hijacks send their ownership flares and then wait on this event. By setting this
    // we let the hijacks run free.
    if (this->m_leftSideUnmanagedWaitEvent != NULL)
    {
        SetEvent(this->m_leftSideUnmanagedWaitEvent);
    }
    else
    {
        // Only reason we expect to not have this event is if the CLR hasn't been loaded yet.
        // In that case, we won't hijack, so nobody's listening for this event either.
        _ASSERTE(!m_initialized);
    }
}

//-----------------------------------------------------------------------------
// For debugging support, record the win32 events.
// Note that although this is for debugging, we want it in retail because we'll
// be debugging retail most of the time :(
// pEvent - the win32 debug event we just received
// pUThread - our unmanaged thread object for the event. We could look it up
//           from pEvent->dwThreadId, but passed in for perf reasons.
//-----------------------------------------------------------------------------
void CordbProcess::DebugRecordWin32Event(const DEBUG_EVENT * pEvent, CordbUnmanagedThread * pUThread)
{
    _ASSERTE(ThreadHoldsProcessLock());

    // Although we could look up the Unmanaged thread, it's faster to have it just passed in.
    // So here we do a consistency check.
    _ASSERTE(pUThread != NULL);
    _ASSERTE(pUThread->m_id == pEvent->dwThreadId);

    m_DbgSupport.m_TotalNativeEvents++; // bump up the counter.

    MiniDebugEvent * pMiniEvent = &m_DbgSupport.m_DebugEventQueue[m_DbgSupport.m_DebugEventQueueIdx];
    pMiniEvent->code        = (BYTE) pEvent->dwDebugEventCode;
    pMiniEvent->pUThread    = pUThread;

    DWORD tid = pEvent->dwThreadId;

    // Record debug-event specific data.
    switch(pEvent->dwDebugEventCode)
    {
    case LOAD_DLL_DEBUG_EVENT:
        pMiniEvent->u.ModuleData.pBaseAddress = pEvent->u.LoadDll.lpBaseOfDll;
        STRESS_LOG2(LF_CORDB, LL_INFO1000, "Win32 Debug Event received: tid=0x%8x, Load Dll. Addr=%p\n",
            tid,
            pEvent->u.LoadDll.lpBaseOfDll);
        break;
    case UNLOAD_DLL_DEBUG_EVENT:
        pMiniEvent->u.ModuleData.pBaseAddress = pEvent->u.UnloadDll.lpBaseOfDll;
        STRESS_LOG2(LF_CORDB, LL_INFO1000, "Win32 Debug Event received: tid=0x%8x, Unload Dll. Addr=%p\n",
            tid,
            pEvent->u.UnloadDll.lpBaseOfDll);
        break;
    case EXCEPTION_DEBUG_EVENT:
        pMiniEvent->u.ExceptionData.pAddress = pEvent->u.Exception.ExceptionRecord.ExceptionAddress;
        pMiniEvent->u.ExceptionData.dwCode   = pEvent->u.Exception.ExceptionRecord.ExceptionCode;

        STRESS_LOG3(LF_CORDB, LL_INFO1000, "Win32 Debug Event received: tid=%8x, (1) Exception. Code=0x%08x, Addr=%p\n",
            tid,
            pMiniEvent->u.ExceptionData.dwCode,
            pMiniEvent->u.ExceptionData.pAddress
        );
        break;
    default:
        STRESS_LOG2(LF_CORDB, LL_INFO1000, "Win32 Debug Event received tid=%8x, %d\n", tid, pEvent->dwDebugEventCode);
        break;
    }


    // Go to the next entry in the queue.
    m_DbgSupport.m_DebugEventQueueIdx = (m_DbgSupport.m_DebugEventQueueIdx + 1) % DEBUG_EVENTQUEUE_SIZE;
}

void CordbProcess::QueueUnmanagedEvent(CordbUnmanagedThread *pUThread, const DEBUG_EVENT *pEvent)
{
    INTERNAL_API_ENTRY(this);
    _ASSERTE(ThreadHoldsProcessLock());
    _ASSERTE(m_pShim != NULL);

    LOG((LF_CORDB, LL_INFO10000, "CP::QUE: queued unmanaged event %d for thread 0x%x\n",
         pEvent->dwDebugEventCode, pUThread->m_id));


    _ASSERTE(pEvent->dwDebugEventCode == EXCEPTION_DEBUG_EVENT);

    // Copy the event into the given thread
    CordbUnmanagedEvent *ue;

    // Use the primary IB event slot unless this is the special stack overflow event case.
    if (!pUThread->HasSpecialStackOverflowCase())
        ue = pUThread->IBEvent();
    else
        ue = pUThread->IBEvent2();

    if(pUThread->HasIBEvent() && !pUThread->HasSpecialStackOverflowCase())
    {
        // Any event being replaced should at least have been continued outside of the hijack
        // We don't track whether or not we expect the exception to retrigger but if we are replacing
        // the event then it did not.
        _ASSERTE(ue->IsEventContinuedUnhijacked());
        LOG((LF_CORDB, LL_INFO10000, "CP::QUE: A previously seen event is being discarded 0x%x 0x%p\n",
         ue->m_currentDebugEvent.u.Exception.ExceptionRecord.ExceptionCode,
         ue->m_currentDebugEvent.u.Exception.ExceptionRecord.ExceptionAddress));
        DequeueUnmanagedEvent(ue->m_owner);
    }

    memcpy(&(ue->m_currentDebugEvent), pEvent, sizeof(DEBUG_EVENT));
    ue->m_state = CUES_IsIBEvent;
    ue->m_next = NULL;

    // Enqueue the event.
    pUThread->SetState(CUTS_HasIBEvent);

    if (m_unmanagedEventQueue == NULL)
        m_unmanagedEventQueue = ue;
    else
        m_lastQueuedUnmanagedEvent->m_next = ue;

    m_lastQueuedUnmanagedEvent = ue;
}

void CordbProcess::DequeueUnmanagedEvent(CordbUnmanagedThread *ut)
{
    INTERNAL_API_ENTRY(this);

    _ASSERTE(m_unmanagedEventQueue != NULL);
    _ASSERTE(ut->HasIBEvent() || ut->HasSpecialStackOverflowCase());
    _ASSERTE(ThreadHoldsProcessLock());


    CordbUnmanagedEvent *ue;

    if (ut->HasIBEvent())
        ue = ut->IBEvent();
    else
    {
        ue = ut->IBEvent2();

        // Since we're dequeuing the special stack overflow event, we're no longer in the special stack overflow case.
        ut->ClearState(CUTS_HasSpecialStackOverflowCase);
    }

    DWORD ec = ue->m_currentDebugEvent.dwDebugEventCode;
    LOG((LF_CORDB, LL_INFO10000, "CP::DUE: dequeue unmanaged event %d for thread 0x%x\n", ec, ut->m_id));

    _ASSERTE(ec == EXCEPTION_DEBUG_EVENT);

    CordbUnmanagedEvent **tmp = &m_unmanagedEventQueue;
    CordbUnmanagedEvent **prev = NULL;

    // Note: this supports out-of-order dequeing of unmanaged events. This is necessary because we queue events even if
    // we're not clear on the ownership question. When we get the answer, and if the event belongs to the Runtime, we go
    // ahead and yank the event out of the queue, wherever it may be.
    while (*tmp && *tmp != ue)
    {
        prev = tmp;
        tmp = &((*tmp)->m_next);
    }

    _ASSERTE(*tmp == ue);

    *tmp = (*tmp)->m_next;

    if (m_unmanagedEventQueue == NULL)
        m_lastQueuedUnmanagedEvent = NULL;
    else if (m_lastQueuedUnmanagedEvent == ue)
    {
        _ASSERTE(prev != NULL);
        m_lastQueuedUnmanagedEvent = *prev;
    }

    ut->ClearState(CUTS_HasIBEvent);

}

void CordbProcess::QueueOOBUnmanagedEvent(CordbUnmanagedThread *pUThread, const DEBUG_EVENT * pEvent)
{
    INTERNAL_API_ENTRY(this);
    _ASSERTE(ThreadHoldsProcessLock());
    _ASSERTE(!pUThread->HasOOBEvent());
    _ASSERTE(IsWin32EventThread());
    _ASSERTE(m_pShim != NULL);

    LOG((LF_CORDB, LL_INFO10000, "CP::QUE: queued OOB unmanaged event %d for thread 0x%x\n",
         pEvent->dwDebugEventCode, pUThread->m_id));

    // Copy the event into the given thread
    CordbUnmanagedEvent *ue = pUThread->OOBEvent();
    memcpy(&(ue->m_currentDebugEvent), pEvent, sizeof(DEBUG_EVENT));
    ue->m_state = CUES_None;
    ue->m_next = NULL;

    // Enqueue the event.
    pUThread->SetState(CUTS_HasOOBEvent);

    if (m_outOfBandEventQueue == NULL)
        m_outOfBandEventQueue = ue;
    else
        m_lastQueuedOOBEvent->m_next = ue;

    m_lastQueuedOOBEvent = ue;
}

void CordbProcess::DequeueOOBUnmanagedEvent(CordbUnmanagedThread *ut)
{
    INTERNAL_API_ENTRY(this);
    _ASSERTE(m_outOfBandEventQueue != NULL);
    _ASSERTE(ut->HasOOBEvent());
    _ASSERTE(ThreadHoldsProcessLock());

    CordbUnmanagedEvent *ue = ut->OOBEvent();
    DWORD ec = ue->m_currentDebugEvent.dwDebugEventCode;

    LOG((LF_CORDB, LL_INFO10000, "CP::DUE: dequeue OOB unmanaged event %d for thread 0x%x\n", ec, ut->m_id));

    CordbUnmanagedEvent **tmp = &m_outOfBandEventQueue;
    CordbUnmanagedEvent **prev = NULL;

    // Note: this supports out-of-order dequeing of unmanaged events. This is necessary because we queue events even if
    // we're not clear on the ownership question. When we get the answer, and if the event belongs to the Runtime, we go
    // ahead and yank the event out of the queue, wherever it may be.
    while (*tmp && *tmp != ue)
    {
        prev = tmp;
        tmp = &((*tmp)->m_next);
    }

    _ASSERTE(*tmp == ue);

    *tmp = (*tmp)->m_next;

    if (m_outOfBandEventQueue == NULL)
        m_lastQueuedOOBEvent = NULL;
    else if (m_lastQueuedOOBEvent == ue)
    {
        _ASSERTE(prev != NULL);
        m_lastQueuedOOBEvent = *prev;
    }

    ut->ClearState(CUTS_HasOOBEvent);
}

HRESULT CordbProcess::SuspendUnmanagedThreads()
{
    INTERNAL_API_ENTRY(this);

    _ASSERTE(ThreadHoldsProcessLock());

    // Iterate over all unmanaged threads...
    CordbUnmanagedThread* ut;
    HASHFIND find;

    for (ut =  m_unmanagedThreads.FindFirst(&find); ut != NULL; ut =  m_unmanagedThreads.FindNext(&find))
    {

        // Don't suspend any thread in a can't stop region. This includes cooperative mode threads & preemptive
        // threads that haven't pushed a NativeTransitionFrame. The ultimate problem here is that a thread
        // in this state is effectively inside the runtime, and thus may take a lock  that blocks the helper thread.
        // IsCan'tStop also includes the helper thread & hijacked threads - which we shouldn't suspend anyways.

        // Only suspend those unmanaged threads that aren't already suspended by us and that aren't already hijacked by
        // us.

        if (!ut->IsSuspended() &&
            !ut->IsDeleted() &&
            !ut->IsCantStop() &&
            !ut->IsBlockingForSync()
        )
        {
            LOG((LF_CORDB, LL_INFO1000, "CP::SUT: suspending unmanaged thread 0x%x, handle 0x%x\n", ut->m_id, ut->m_handle));

            DWORD succ = SuspendThread(ut->m_handle);

            if (succ == 0xFFFFFFFF)
            {
                // This is okay... the thread may be dying after an ExitThread event.
                LOG((LF_CORDB, LL_INFO1000, "CP::SUT: failed to suspend thread 0x%x\n", ut->m_id));
            }
            else
            {
                m_state |= PS_SOME_THREADS_SUSPENDED;

                ut->SetState(CUTS_Suspended);
            }
        }
    }

    return S_OK;
}

HRESULT CordbProcess::ResumeUnmanagedThreads()
{
    INTERNAL_API_ENTRY(this);
    _ASSERTE(ThreadHoldsProcessLock());
    FAIL_IF_NEUTERED(this);

    // Iterate over all unmanaged threads...
    CordbUnmanagedThread* ut;
    HASHFIND find;

    for (ut =  m_unmanagedThreads.FindFirst(&find); ut != NULL; ut =  m_unmanagedThreads.FindNext(&find))
    {
        // Only resume those unmanaged threads that were suspended by us.
        if (ut->IsSuspended())
        {
            LOG((LF_CORDB, LL_INFO1000, "CP::RUT: resuming unmanaged thread 0x%x\n", ut->m_id));

            DWORD succ = ResumeThread(ut->m_handle);

            if (succ == 0xFFFFFFFF)
            {
                LOG((LF_CORDB, LL_INFO1000, "CP::RUT: failed to resume thread 0x%x\n", ut->m_id));
            }
            else
                ut->ClearState(CUTS_Suspended);
        }
    }

    m_state &= ~PS_SOME_THREADS_SUSPENDED;

    return S_OK;
}

//-----------------------------------------------------------------------------
// DispatchUnmanagedInBandEvent
//
// Handler for Win32 events already known to be Unmanaged and in-band.
//-----------------------------------------------------------------------------
void CordbProcess::DispatchUnmanagedInBandEvent()
{
    INTERNAL_API_ENTRY(this);
    _ASSERTE(ThreadHoldsProcessLock());

    // There should be no queued OOB events!!! If there are, then we have a breakdown in our protocol, since all OOB
    // events should be dispatched before attempting to really continue from any in-band event.
    _ASSERTE(m_outOfBandEventQueue == NULL);
    _ASSERTE(m_cordb != NULL);
    _ASSERTE(m_cordb->m_unmanagedCallback != NULL);
    _ASSERTE(!m_dispatchingUnmanagedEvent);

    CordbUnmanagedThread * pUnmanagedThread = NULL;
    CordbUnmanagedEvent * pUnmanagedEvent = m_unmanagedEventQueue;

    while (true)
    {
        // get the next queued event that isn't dispatched yet
        while(pUnmanagedEvent != NULL && pUnmanagedEvent->IsDispatched())
        {
            pUnmanagedEvent = pUnmanagedEvent->m_next;
        }

        if(pUnmanagedEvent == NULL)
            break;

        // Get the thread for this event
        _ASSERTE(pUnmanagedThread == NULL);
        pUnmanagedThread = pUnmanagedEvent->m_owner;
        _ASSERTE(pUnmanagedThread != NULL);

        // We better not have dispatched it yet!
        _ASSERTE(!pUnmanagedEvent->IsDispatched());

        // We shouldn't be dispatching IB events on a thread that has exited.
        // Though it's possible that the thread may exit *after* the IB event has been dispatched
        // if it gets hijacked.
        _ASSERTE(!pUnmanagedThread->IsDeleted());

        // Make sure we keep the thread alive while we're playing with it.
        pUnmanagedThread->InternalAddRef();

        LOG((LF_CORDB, LL_INFO10000, "CP::DUE: dispatching unmanaged event %d for thread 0x%x\n",
             pUnmanagedEvent->m_currentDebugEvent.dwDebugEventCode, pUnmanagedThread->m_id));

        m_dispatchingUnmanagedEvent = true;

        // Add/Remove a reference which is scoped to the time that m_lastDispatchedIBEvent
        // is set to pUnmanagedEvent (it is an interior pointer)
        // see DevDiv issue 818301 for more details
        if(m_lastDispatchedIBEvent != NULL)
        {
            m_lastDispatchedIBEvent->m_owner->InternalRelease();
            m_lastDispatchedIBEvent = NULL;
        }
        pUnmanagedThread->InternalAddRef();
        m_lastDispatchedIBEvent = pUnmanagedEvent;
        pUnmanagedEvent->SetState(CUES_Dispatched);

        IncStopCount();

        Unlock();

        {
            // Interface is semantically const, but does not include const in signature.
            DEBUG_EVENT * pEvent = const_cast<DEBUG_EVENT *> (&(pUnmanagedEvent->m_currentDebugEvent));
            PUBLIC_WIN32_CALLBACK_IN_THIS_SCOPE(this,pEvent, FALSE);
            m_cordb->m_unmanagedCallback->DebugEvent(pEvent, FALSE);
        }

        Lock();

        // Calling IMDA::Continue() will set m_dispatchingUnmanagedEvent = false.
        // So if Continue() was called && we have more events, we'll loop and dispatch more events.
        // Else we'll break out of the while loop.
        if(m_dispatchingUnmanagedEvent)
            break;

        // Continue was called in the dispatch callback, but that continue path just
        // clears the dispatch flag and returns. The continue right here is the logical
        // completion of the user's continue request
        // Note it is sometimes the case that these events have already been continued because
        // they had defered dispatching. At the time of deferal they were immediately continued.
        // If the event is already continued then this continue becomes a no-op.
        m_pShim->GetWin32EventThread()->DoDbgContinue(this, pUnmanagedEvent);

        // Release our reference to the unmanaged thread that we dispatched
        // This event should have been continued long ago...
        _ASSERTE(!pUnmanagedThread->IBEvent()->IsEventWaitingForContinue());
        pUnmanagedThread->InternalRelease();
        pUnmanagedThread = NULL;
    }

    m_dispatchingUnmanagedEvent = false;

    // Release our reference to the last thread that we dispatched now...
    if(pUnmanagedThread)
    {
        pUnmanagedThread->InternalRelease();
        pUnmanagedThread = NULL;
    }
}

//-----------------------------------------------------------------------------
// DispatchUnmanagedOOBEvent
//
// Handler for Win32 events already known to be Unmanaged and out-of-band.
//-----------------------------------------------------------------------------
void CordbProcess::DispatchUnmanagedOOBEvent()
{
    INTERNAL_API_ENTRY(this);
    _ASSERTE(ThreadHoldsProcessLock());
    _ASSERTE(IsWin32EventThread());

    // There should be OOB events queued...
    _ASSERTE(m_outOfBandEventQueue != NULL);
    _ASSERTE(m_cordb->m_unmanagedCallback != NULL);

    do
    {
        // Get the first event in the OOB Queue...
        CordbUnmanagedEvent * pUnmanagedEvent = m_outOfBandEventQueue;
        CordbUnmanagedThread * pUnmanagedThread = pUnmanagedEvent->m_owner;

        // Make sure we keep the thread alive while we're playing with it.
        RSSmartPtr<CordbUnmanagedThread> pRef(pUnmanagedThread);

        LOG((LF_CORDB, LL_INFO10000, "[%x] CP::DUE: dispatching OOB unmanaged event %d for thread 0x%x\n",
             GetCurrentThreadId(), pUnmanagedEvent->m_currentDebugEvent.dwDebugEventCode, pUnmanagedThread->m_id));

        m_dispatchingOOBEvent = true;
        pUnmanagedEvent->SetState(CUES_Dispatched);
        Unlock();

        {
            // Interface is semantically const, but does not include const in signature.
            DEBUG_EVENT * pEvent = const_cast<DEBUG_EVENT *> (&(pUnmanagedEvent->m_currentDebugEvent));
            PUBLIC_WIN32_CALLBACK_IN_THIS_SCOPE(this, pEvent, TRUE);
            m_cordb->m_unmanagedCallback->DebugEvent(pEvent, TRUE);
        }

        Lock();

        // If they called Continue from the callback, then continue the OOB event right now before dispatching the next
        // one.
        if (!m_dispatchingOOBEvent)
        {
            DequeueOOBUnmanagedEvent(pUnmanagedThread);

            // Should not have continued from this debug event yet.
            _ASSERTE(pUnmanagedEvent->IsEventWaitingForContinue());

            // Do a little extra work if that was an OOB exception event...
            HRESULT hr = pUnmanagedEvent->m_owner->FixupAfterOOBException(pUnmanagedEvent);
            _ASSERTE(SUCCEEDED(hr));

            // Go ahead and continue now...
            this->m_pShim->GetWin32EventThread()->DoDbgContinue(this, pUnmanagedEvent);
        }

        // Implicit release of pUnmanagedThread via pRef
    }
    while (!m_dispatchingOOBEvent && (m_outOfBandEventQueue != NULL));

    m_dispatchingOOBEvent = false;

    LOG((LF_CORDB, LL_INFO10000, "CP::DUE: done dispatching OOB events. Queue=0x%08x\n", m_outOfBandEventQueue));
}
#endif // FEATURE_INTEROP_DEBUGGING

//-----------------------------------------------------------------------------
// StartSyncFromWin32Stop
//
// Get the process from a Fozen state or a Live state to a Synchronized State.
// Note that Process Exit is considered to be synchronized.
// This is a nop if we're not Interop Debugging.
// If this function succeeds, we're in a synchronized state.
//
// Arguments:
//    pfAsyncBreakSent - returns if this method sent an async-break or not.
//
// Return value:
//    typical HRESULT return values, nothing sinister here.
//-----------------------------------------------------------------------------
HRESULT CordbProcess::StartSyncFromWin32Stop(BOOL * pfAsyncBreakSent)
{
    INTERNAL_API_ENTRY(this);
    if (m_pShim == NULL) // This API is moved off to the shim
    {
        return E_NOTIMPL;
    }

    HRESULT hr = S_OK;

    // Caller should have taken the stop-go lock. This prevents us from racing w/ a continue.
    _ASSERTE(m_StopGoLock.HasLock());

    // Process should be init before we try to sync it.
    _ASSERTE(this->m_initialized);

    // If nobody's listening for an AsyncBreak, and we're not stopped, then our caller
    // doesn't know if we're sending an AsyncBreak or not; and thus we may not continue.
    // Failing this assert means that we're stopping but we don't think we're going to get a continue
    // down the road, and thus we're headed for a deadlock.
    _ASSERTE((pfAsyncBreakSent != NULL) || (m_stopCount > 0));

    if (pfAsyncBreakSent)
    {
        *pfAsyncBreakSent = FALSE;
    }

#ifdef FEATURE_INTEROP_DEBUGGING

    // If we're win32 stopped (but not out-of-band win32 stopped), or if we're running free on the Left Side but we're
    // just not synchronized (and we're win32 attached), then go ahead and do an internal continue and send an async
    // break event to get the Left Side sync'd up.
    //
    // The process can be running free as far as Win32 events are concerned, but still not synchronized as far as the
    // Runtime is concerned. This can happen in a lot of cases where we end up with the Runtime not sync'd but with the
    // process running free due to hijacking, etc...
    if (((m_state & CordbProcess::PS_WIN32_STOPPED) && (m_outOfBandEventQueue == NULL)) ||
        (!GetSynchronized() && IsInteropDebugging()))
    {
        Lock();

        if (((m_state & CordbProcess::PS_WIN32_STOPPED) && (m_outOfBandEventQueue == NULL)) ||
            (!GetSynchronized() && IsInteropDebugging()))
        {
            // This can't be the win32 ET b/c we need that thread to be alive and pumping win32 DE so that
            // our Async Break can get across.
            // So nobody should ever be calling this on the w32 ET. But they could, since we do trickle in from
            // outside APIs. So we need a retail check.
            if (IsWin32EventThread())
            {
                _ASSERTE(!"Don't call this API on the W32 Event Thread");

                Unlock();
                return ErrWrapper(CORDBG_E_CANT_CALL_ON_THIS_THREAD);
            }

            STRESS_LOG1(LF_CORDB, LL_INFO1000, "[%x] CP::SSFW32S: sending internal continue\n", GetCurrentThreadId());

            // Can't do this on the win32 event thread.
            _ASSERTE(!this->IsWin32EventThread());

            // If the helper thread is already dead, then we just return as if we sync'd the process.
            if (m_helperThreadDead)
            {
                if (pfAsyncBreakSent)
                {
                    *pfAsyncBreakSent = TRUE;
                }

                // Mark the process as synchronized so no events will be dispatched until the thing is
                // continued. However, the marking here is not a usual marking for synchronized. It has special
                // semantics when we're interop debugging. We use m_oddSync to remember this so that we can take special
                // action in Continue().
                SetSynchronized(true);
                m_oddSync = true;

                // Get the RC Event Thread to stop listening to the process.
                m_cordb->ProcessStateChanged();

                Unlock();

                return S_OK;
            }

            m_stopRequested = true;

            // See ::Stop for why we defer this. The delayed events will be dispatched when some one calls continue.
            // And we know they'll call continue b/c (stopCount > 0) || (our caller knows we're sending an AsyncBreak).
            m_specialDeferment = true;

            Unlock();

            // If the process gets synchronized between the Unlock() and here, then SendUnmanagedContinue() will end up
            // not doing anything at all since a) it holds the process lock when working and b) it gates everything on
            // if the process is sync'd or not. This is exactly what we want.
            hr = this->m_pShim->GetWin32EventThread()->SendUnmanagedContinue(this, cInternalUMContinue);

            LOG((LF_CORDB, LL_INFO1000, "[%x] CP::SSFW32S: internal continue returned\n", GetCurrentThreadId()));

            // Send an async break to the left side now that its running.
            DebuggerIPCEvent * pEvent = (DebuggerIPCEvent *) _alloca(CorDBIPC_BUFFER_SIZE);
            InitIPCEvent(pEvent, DB_IPCE_ASYNC_BREAK, false, VMPTR_AppDomain::NullPtr());

            LOG((LF_CORDB, LL_INFO1000, "[%x] CP::SSFW32S: sending async stop\n", GetCurrentThreadId()));

            // If the process gets synchronized between the Unlock() and here, then this message will do nothing (Left
            // Side catches it) and we'll never get a response, and it won't hurt anything.
            hr = m_cordb->SendIPCEvent(this, pEvent, CorDBIPC_BUFFER_SIZE);
            // @Todo- how do we handle a failure here?

            // If the send returns with the helper thread being dead, then we know we don't need to wait for the process
            // to sync.
            if (!m_helperThreadDead)
            {
                STRESS_LOG1(LF_CORDB, LL_INFO1000, "[%x] CP::SSFW32S: sent async stop, waiting for event\n", GetCurrentThreadId());

                // If we got synchronized between the Unlock() and here its okay since m_stopWaitEvent is still high
                // from the last sync.
                DWORD dwWaitResult = SafeWaitForSingleObject(this, m_stopWaitEvent, INFINITE);

                STRESS_LOG2(LF_CORDB, LL_INFO1000, "[%x] CP::SSFW32S: got event, %d\n", GetCurrentThreadId(), dwWaitResult);

                _ASSERTE(dwWaitResult == WAIT_OBJECT_0);
            }

            Lock();

            m_specialDeferment = false;

            if (pfAsyncBreakSent)
            {
                *pfAsyncBreakSent = TRUE;
            }

            // If the helper thread died while we were trying to send an event to it, then we just do the same odd sync
            // logic we do above.
            if (m_helperThreadDead)
            {
                SetSynchronized(true);
                m_oddSync = true;
                hr = S_OK;
            }

            m_stopRequested = false;
            m_cordb->ProcessStateChanged();
        }

        Unlock();
    }
#endif // FEATURE_INTEROP_DEBUGGING

    return hr;
}

// Check if the left side has exited. If so, get the right-side
// into shutdown mode. Only use this to avert us from going into
// an unrecoverable error.
bool CordbProcess::CheckIfLSExited()
{
// Check by waiting on the handle with no timeout.
    if (WaitForSingleObject(m_handle, 0) == WAIT_OBJECT_0)
    {
        Lock();
        m_terminated = true;
        m_exiting = true;
        Unlock();
    }

    LOG((LF_CORDB, LL_INFO10, "CP::IsLSExited() returning '%s'\n",
        m_exiting ? "true" : "false"));

    return m_exiting;
}

// Call this if something really bad happened and we can't do
// anything meaningful with the CordbProcess.
void CordbProcess::UnrecoverableError(HRESULT errorHR,
                                      unsigned int errorCode,
                                      const char *errorFile,
                                      unsigned int errorLine)
{
    LOG((LF_CORDB, LL_INFO10, "[%x] CP::UE: unrecoverable error 0x%08x "
         "(%d) %s:%d\n",
         GetCurrentThreadId(),
         errorHR, errorCode, errorFile, errorLine));

    // We definitely want to know about any of these.
    STRESS_LOG3(LF_CORDB, LL_EVERYTHING, "Unrecoverable Error:0x%08x, File=%s, line=%d\n", errorHR, errorFile, errorLine);

    // It's possible for an unrecoverable error to occur if the user detaches the
    // debugger while inside CordbProcess::DispatchRCEvent() (as that function deliberately
    // calls Unlock() while calling into the Shim).  Detect such cases here & bail before we
    // try to access invalid fields on this CordbProcess.
    //
    // Normally, we'd need to take the cordb process lock around the IsNeutered check
    // (and the code that follows).  And perhaps this is a good thing to do in the
    // future.  But for now we're not for two reasons:
    //
    // 1) It's scary.  We're in UnrecoverableError() for gosh sake.  I don't know all
    // the possible bad states we can be in to get here.  Will taking the process lock
    // have ordering issues?  Will the process lock even be valid to take here (or might
    // we AV)?  Since this is error handling, we should probably be as light as we can
    // not to cause more errors.
    //
    // 2) It's unnecessary.  For the Watson dump I investigated that caused this fix in
    // the first place, we already detached before entering UnrecoverableError()
    // (indeed, the only reason we're in UnrecoverableError is that we already detached
    // and that caused a prior API to fail).  Thus, there's no timing issue (in that
    // case, anyway), wrt to entering UnrecoverableError() and detaching / neutering.
    if (IsNeutered())
        return;

#ifdef _DEBUG
    // Ping our error trapping logic
    HRESULT hrDummy;
    hrDummy = ErrWrapper(errorHR);
#endif

    if (m_pShim == NULL)
    {
        // @dbgtodo - , shim: Once everything is hoisted, we can remove
        // this code.
        // In the v3 case, we should never get an unrecoverable error. Instead, the HR should be propagated
        // and returned at the top-level public API.
        _ASSERTE(!"Unrecoverable error dispatched in V3 case.");
    }

    CONSISTENCY_CHECK_MSGF(IsLegalFatalError(errorHR), ("Unrecoverable internal error: hr=0x%08x!", errorHR));

    if (!IsLegalFatalError(errorHR) || (errorHR != CORDBG_E_CANNOT_DEBUG_FIBER_PROCESS))
    {
        // This will throw everything into a Zombie state. The ATT_ macros will check this and fail immediately.
        m_unrecoverableError = true;

        //
        // Mark the process as no longer synchronized.
        //
        Lock();
        SetSynchronized(false);
        IncStopCount();
        Unlock();
    }

    // Set the error flags in the process so that if parts of it are
    // still alive, it will realize that its in this mode and do the
    // right thing.
    if (GetDCB() != NULL)
    {
        GetDCB()->m_errorHR = errorHR;
        GetDCB()->m_errorCode = errorCode;
        EX_TRY
        {
            UpdateLeftSideDCBField(&(GetDCB()->m_errorHR), sizeof(GetDCB()->m_errorHR));
            UpdateLeftSideDCBField(&(GetDCB()->m_errorCode), sizeof(GetDCB()->m_errorCode));
        }
        EX_CATCH
        {
            _ASSERTE(!"Writing process memory failed, perhaps due to an unexpected disconnection from the target.");
        }
        EX_END_CATCH(SwallowAllExceptions);
    }

    //
    // Let the user know that we've hit an unrecoverable error.
    //
    if (m_cordb->m_managedCallback)
    {
        // We are about to send DebuggerError call back. The state of RS is undefined.
        // So we use the special Public Callback. We may be holding locks and stuff.
        // We may also be deeply nested within the RS.
        PUBLIC_CALLBACK_IN_THIS_SCOPE_DEBUGGERERROR(this);
        m_cordb->m_managedCallback->DebuggerError((ICorDebugProcess*) this,
                                                  errorHR,
                                                  errorCode);
    }
}


HRESULT CordbProcess::CheckForUnrecoverableError()
{
    HRESULT hr = S_OK;

    if (GetDCB() != NULL)
    {
        // be sure we have the latest information
        UpdateRightSideDCB();

        if (GetDCB()->m_errorHR != S_OK)
        {
            UnrecoverableError(GetDCB()->m_errorHR,
                               GetDCB()->m_errorCode,
                           __FILE__, __LINE__);

            hr = GetDCB()->m_errorHR;
        }
    }

    return hr;
}


/*
 * EnableLogMessages enables/disables sending of log messages to the
 * debugger for logging.
 */
HRESULT CordbProcess::EnableLogMessages(BOOL fOnOff)
{
    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    ATT_REQUIRE_STOPPED_MAY_FAIL(this);
    HRESULT hr = S_OK;

    DebuggerIPCEvent *event = (DebuggerIPCEvent*) _alloca(CorDBIPC_BUFFER_SIZE);
    InitIPCEvent(event, DB_IPCE_ENABLE_LOG_MESSAGES, false, VMPTR_AppDomain::NullPtr());
    event->LogSwitchSettingMessage.iLevel = (int)fOnOff;

    hr = m_cordb->SendIPCEvent(this, event, CorDBIPC_BUFFER_SIZE);
    hr = WORST_HR(hr, event->hr);

    LOG((LF_CORDB, LL_INFO10000, "[%x] CP::EnableLogMessages: EnableLogMessages=%d sent.\n",
         GetCurrentThreadId(), fOnOff));

    return hr;
}

/*
 * ModifyLogSwitch modifies the specified switch's severity level.
 */
COM_METHOD CordbProcess::ModifyLogSwitch(_In_z_ WCHAR *pLogSwitchName, LONG lLevel)
{
    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    ATT_REQUIRE_STOPPED_MAY_FAIL(this);

    HRESULT hr = S_OK;

    _ASSERTE (pLogSwitchName != NULL);

    DebuggerIPCEvent *event = (DebuggerIPCEvent*) _alloca(CorDBIPC_BUFFER_SIZE);
    InitIPCEvent(event, DB_IPCE_MODIFY_LOGSWITCH, false, VMPTR_AppDomain::NullPtr());
    event->LogSwitchSettingMessage.iLevel = lLevel;
    event->LogSwitchSettingMessage.szSwitchName.SetStringTruncate(pLogSwitchName);

    hr = m_cordb->SendIPCEvent(this, event, CorDBIPC_BUFFER_SIZE);
    hr = WORST_HR(hr, event->hr);

    LOG((LF_CORDB, LL_INFO10000, "[%x] CP::ModifyLogSwitch: ModifyLogSwitch sent.\n",
         GetCurrentThreadId()));

    return hr;
}

//-----------------------------------------------------------------------------
// Writes a buffer from the target and performs checks similar to SafeWriteStruct
//
// Arguments:
//    tb - TargetBuffer which represents the target memory we want to write to
//    pLocalBuffer - local pointer into source buffer
//    cbSize - the size of local buffer
//
// Exceptions
//    On error throws the result of WriteVirtual unless a short write is performed,
//    in which case throws ERROR_PARTIAL_COPY
//
void CordbProcess::SafeWriteBuffer(TargetBuffer tb,
                                   const BYTE * pLocalBuffer)
{
    _ASSERTE(m_pMutableDataTarget != NULL);
    HRESULT hr = m_pMutableDataTarget->WriteVirtual(tb.pAddress,
        pLocalBuffer,
        tb.cbSize);
    IfFailThrow(hr);
}

//-----------------------------------------------------------------------------
// Reads a buffer from the target and performs checks similar to SafeWriteStruct
//
// Arguments:
//    tb - TargetBuffer which represents the target memory to read from
//    pLocalBuffer - local pointer into source buffer
//    cbSize - the size of the remote buffer
//    throwOnError - determines whether the function throws exceptions or returns HRESULTs
//                   in failure cases
//
// Exceptions:
//    If throwOnError is TRUE
//      On error always throws the special CORDBG_E_READVIRTUAL_FAILURE, unless a short write is performed
//      in which case throws ERROR_PARTIAL_COPY
//   If throwOnError is FALSE
//      No exceptions are thrown, and instead the same error codes are returned as HRESULTs
//
HRESULT CordbProcess::SafeReadBuffer(TargetBuffer tb, BYTE * pLocalBuffer, BOOL throwOnError)
{
    ULONG32 cbRead;
    HRESULT hr = m_pDACDataTarget->ReadVirtual(tb.pAddress,
        pLocalBuffer,
        tb.cbSize,
        &cbRead);

    if (FAILED(hr))
    {
        if (throwOnError)
            ThrowHR(CORDBG_E_READVIRTUAL_FAILURE);
        else
            return CORDBG_E_READVIRTUAL_FAILURE;
    }

    if (cbRead != tb.cbSize)
    {
        if (throwOnError)
            ThrowWin32(ERROR_PARTIAL_COPY);
        else
            return HRESULT_FROM_WIN32(ERROR_PARTIAL_COPY);
    }
    return S_OK;
}


//---------------------------------------------------------------------------------------
// Lookup or create an appdomain.
//
// Arguments:
//     vmAppDomain - CLR appdomain to lookup
//
// Returns:
//     Instance of CordbAppDomain for the given appdomain. This is a cached instance.
//     If the CordbAppDomain does not yet exist, it will be created and added to the cache.
//     Never returns NULL. Throw on error.
CordbAppDomain * CordbProcess::LookupOrCreateAppDomain(VMPTR_AppDomain vmAppDomain)
{
    CordbAppDomain * pAppDomain = m_appDomains.GetBase(VmPtrToCookie(vmAppDomain));
    if (pAppDomain != NULL)
    {
        return pAppDomain;
    }
    return CacheAppDomain(vmAppDomain);
}

CordbAppDomain * CordbProcess::GetSharedAppDomain()
{
    if (m_sharedAppDomain == NULL)
    {
        CordbAppDomain *pAD = new CordbAppDomain(this, VMPTR_AppDomain::NullPtr());
        if (InterlockedCompareExchangeT<CordbAppDomain*>(&m_sharedAppDomain, pAD, NULL) != NULL)
        {
            delete pAD;
        }
		m_sharedAppDomain->InternalAddRef();
    }

    return m_sharedAppDomain;
}

//---------------------------------------------------------------------------------------
//
// Add a new appdomain to the cache.
//
// Arguments:
//      vmAppDomain - appdomain to add.
//
// Return Value:
//    Pointer to newly created appdomain, which should be the normal case.
//    Throws on failure. Never returns null.
//
// Assumptions:
//    Caller ensure the appdomain is not already cached.
//    Caller should have stop-go lock, which provides thread-safety.
//
// Notes:
//    This sets unrecoverable error on failure.
//
//---------------------------------------------------------------------------------------
CordbAppDomain * CordbProcess::CacheAppDomain(VMPTR_AppDomain vmAppDomain)
{
    INTERNAL_API_ENTRY(GetProcess());

    _ASSERTE(GetProcessLock()->HasLock());

    RSInitHolder<CordbAppDomain> pAppDomain;
    pAppDomain.Assign(new CordbAppDomain(this, vmAppDomain));  // throws

    // Add to the hash. This will addref the pAppDomain.
    // Caller ensures we're not already cached.
    // The cache will take ownership.
    m_appDomains.AddBaseOrThrow(pAppDomain);

    // If this assert fires, then it likely means the target is corrupted.
    TargetConsistencyCheck(m_pDefaultAppDomain == NULL);
    m_pDefaultAppDomain = pAppDomain;

    CordbAppDomain * pReturn = pAppDomain;
    pAppDomain.ClearAndMarkDontNeuter();

    _ASSERTE(pReturn != NULL);
    return pReturn;
}

//---------------------------------------------------------------------------------------
//
// Callback for Appdomain enumeration.
//
// Arguments:
//      vmAppDomain - new appdomain to add to enumeration
//      pUserData - data passed with callback (a 'this' ptr for CordbProcess)
//
//
// Assumptions:
//    Invoked as callback from code:CordbProcess::PrepopulateAppDomains
//
//
//---------------------------------------------------------------------------------------

// static
void CordbProcess::AppDomainEnumerationCallback(VMPTR_AppDomain vmAppDomain, void * pUserData)
{
    CONTRACTL
    {
        THROWS;
    }
    CONTRACTL_END;

    CordbProcess * pProcess = static_cast<CordbProcess *> (pUserData);
    INTERNAL_DAC_CALLBACK(pProcess);

    pProcess->LookupOrCreateAppDomain(vmAppDomain);
}

//---------------------------------------------------------------------------------------
//
// Traverse appdomains in the target and build up our list.
//
// Arguments:
//
// Return Value:
//    returns on success.
//    Throws on error. AppDomain cache may be partially populated.
//
// Assumptions:
//    This is an non-invasive inspection operation called when the debuggee is stopped.
//
// Notes:
//    This can be called multiple times. If the list is non-empty, it will nop.
//---------------------------------------------------------------------------------------
void CordbProcess::PrepopulateAppDomainsOrThrow()
{
    CONTRACTL
    {
        THROWS;
    }
    CONTRACTL_END;

    INTERNAL_API_ENTRY(this);

    if (!IsDacInitialized())
    {
        return;
    }

    // DD-primitive  that invokes a callback.  This may throw.
    GetDAC()->EnumerateAppDomains(
        CordbProcess::AppDomainEnumerationCallback,
        this);
}

//---------------------------------------------------------------------------------------
//
// EnumerateAppDomains enumerates all app domains in the process.
//
// Arguments:
//      ppAppDomains - get appdomain enumerator
//
// Return Value:
//    S_OK on success.
//
// Assumptions:
//
//
// Notes:
//    This operation is non-invasive target.
//
//---------------------------------------------------------------------------------------
HRESULT CordbProcess::EnumerateAppDomains(ICorDebugAppDomainEnum **ppAppDomains)
{
    HRESULT hr = S_OK;
    PUBLIC_API_BEGIN(this);
    {
        ValidateOrThrow(ppAppDomains);

        // Ensure list is populated.
        PrepopulateAppDomainsOrThrow();

        RSInitHolder<CordbHashTableEnum> pEnum;
        CordbHashTableEnum::BuildOrThrow(
            this,
            GetContinueNeuterList(),
            &m_appDomains,
            IID_ICorDebugAppDomainEnum,
            pEnum.GetAddr());

        *ppAppDomains = static_cast<ICorDebugAppDomainEnum*> (pEnum);
        pEnum->ExternalAddRef();

        pEnum.ClearAndMarkDontNeuter();
    }
    PUBLIC_API_END(hr);
    return hr;
}

/*
 * GetObject returns the runtime process object.
 * Note: This method is not yet implemented.
 */
HRESULT CordbProcess::GetObject(ICorDebugValue **ppObject)
{
    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT(ppObject, ICorDebugObjectValue **);

    return E_NOTIMPL;
}


//---------------------------------------------------------------------------------------
//
// Given a taskid, finding the corresponding thread. The function can fail if we do not
// find any thread with the given taskid
//
// Arguments:
//     taskId - The task ID to look for.
//     ppThread - OUT: Space for storing the thread corresponding to the taskId given.
//
// Return Value:
//     Typical HRESULT semantics, nothing abnormal.
//
HRESULT CordbProcess::GetThreadForTaskID(TASKID taskId, ICorDebugThread2 ** ppThread)
{
    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());

    HRESULT hr = S_OK;

    EX_TRY
    {
        RSLockHolder lockHolder(GetProcessLock());

        if (ppThread == NULL)
        {
            ThrowHR(E_INVALIDARG);
        }

        // On initialization, the task ID of every thread is INVALID_TASK_ID, unless a host is present and
        // the host calls IClrTask::SetTaskIdentifier().  So we need to explicitly check for INVALID_TASK_ID
        // here and return NULL if necessary.  We return S_FALSE because that's the return value for the case
        // where we can't find a thread for the specified task ID.
        if (taskId == INVALID_TASK_ID)
        {
            *ppThread = NULL;
            hr = S_FALSE;
        }
        else
        {
            PrepopulateThreadsOrThrow();

            // now find the ICorDebugThread corresponding to it
            CordbThread * pThread;
            HASHFIND hashFind;


            for (pThread  = m_userThreads.FindFirst(&hashFind);
                 pThread != NULL;
                 pThread  = m_userThreads.FindNext(&hashFind))
            {
                if (pThread->GetTaskID() == taskId)
                {
                    break;
                }
            }

            if (pThread == NULL)
            {
                *ppThread = NULL;
                hr = S_FALSE;
            }
            else
            {
                *ppThread = pThread;
                pThread->ExternalAddRef();
            }
        }
    }
    EX_CATCH_HRESULT(hr);
    return hr;
}   // CordbProcess::GetThreadForTaskid

HRESULT
CordbProcess::GetVersion(COR_VERSION* pVersion)
{
    if (NULL == pVersion)
    {
        return E_INVALIDARG;
    }

    //
    // Because we require a matching version of mscordbi.dll to debug a certain version of the runtime,
    // we can just use constants found in this particular mscordbi.dll to determine the version of the left side.
    pVersion->dwMajor = RuntimeProductMajorVersion;
    pVersion->dwMinor = RuntimeProductMinorVersion;
    pVersion->dwBuild = RuntimeProductPatchVersion;
    pVersion->dwSubBuild = 0;

    return S_OK;
}

#ifdef FEATURE_INTEROP_DEBUGGING
//-----------------------------------------------------------------------------
// Search for a native patch given the address. Return null if not found.
// Since we return an address, this is only valid until the table is disturbed.
//-----------------------------------------------------------------------------
NativePatch * CordbProcess::GetNativePatch(const void * pAddress)
{
    _ASSERTE(ThreadHoldsProcessLock());

    int cTotal = m_NativePatchList.Count();
    NativePatch * pTable = m_NativePatchList.Table();
    if (pTable == NULL)
    {
        return NULL;
    }

    for(int i = 0; i  < cTotal; i++)
    {
        if (pTable[i].pAddress == pAddress)
        {
            return &pTable[i];
        }
    }
    return NULL;
}

//-----------------------------------------------------------------------------
// Is there an break-opcode (int3 on x86) at the address in the debuggee?
//-----------------------------------------------------------------------------
bool CordbProcess::IsBreakOpcodeAtAddress(const void * address)
{
    // There should have been an int3 there already. Since we already put it in there,
    // we should be able to safely read it out.
#if defined(TARGET_ARM) || defined(TARGET_ARM64)
    PRD_TYPE opcodeTest = 0;
#elif defined(TARGET_AMD64) || defined(TARGET_X86)
    BYTE opcodeTest = 0;
#else
    PORTABILITY_ASSERT("NYI: Architecture specific opcode type to read");
#endif

    HRESULT hr = SafeReadStruct(PTR_TO_CORDB_ADDRESS(address), &opcodeTest);
    SIMPLIFYING_ASSUMPTION_SUCCEEDED(hr);

    return (opcodeTest == CORDbg_BREAK_INSTRUCTION);
}
#endif // FEATURE_INTEROP_DEBUGGING

//-----------------------------------------------------------------------------
// CordbProcess::SetUnmanagedBreakpoint
// Called by a native debugger to add breakpoints during Interop.
// address - remote address into the debuggee
// bufsize, buffer[] - initial size & buffer for the opcode that we're replacing.
// buflen - size of the buffer that we write to.
//-----------------------------------------------------------------------------
HRESULT
CordbProcess::SetUnmanagedBreakpoint(CORDB_ADDRESS address, ULONG32 bufsize, BYTE buffer[], ULONG32 * bufLen)
{
    LOG((LF_CORDB, LL_INFO100, "CP::SetUnBP: pProcess=%x, address=%p.\n", this, CORDB_ADDRESS_TO_PTR(address)));
#ifndef FEATURE_INTEROP_DEBUGGING
    return E_NOTIMPL;
#else
    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    FAIL_IF_MANAGED_ONLY(this);
    _ASSERTE(!ThreadHoldsProcessLock());
    Lock();
    HRESULT hr = SetUnmanagedBreakpointInternal(address, bufsize, buffer, bufLen);
    Unlock();
    return hr;
#endif
}

//-----------------------------------------------------------------------------
// CordbProcess::SetUnmanagedBreakpointInternal
// The worker behind SetUnmanagedBreakpoint, this function can set both public
// breakpoints used by the debugger and internal breakpoints used for utility
// purposes in interop debugging.
// address - remote address into the debuggee
// bufsize, buffer[] - initial size & buffer for the opcode that we're replacing.
// buflen - size of the buffer that we write to.
//-----------------------------------------------------------------------------
HRESULT
CordbProcess::SetUnmanagedBreakpointInternal(CORDB_ADDRESS address, ULONG32 bufsize, BYTE buffer[], ULONG32 * bufLen)
{
    LOG((LF_CORDB, LL_INFO100, "CP::SetUnBPI: pProcess=%x, address=%p.\n", this, CORDB_ADDRESS_TO_PTR(address)));
#ifndef FEATURE_INTEROP_DEBUGGING
    return E_NOTIMPL;
#else

    INTERNAL_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    FAIL_IF_MANAGED_ONLY(this);
    _ASSERTE(ThreadHoldsProcessLock());

    HRESULT hr = S_OK;

    NativePatch * p = NULL;
#if defined(TARGET_X86) || defined(TARGET_AMD64)
    const BYTE patch = CORDbg_BREAK_INSTRUCTION;
    BYTE opcode;
#elif defined(TARGET_ARM64)
    const PRD_TYPE patch = CORDbg_BREAK_INSTRUCTION;
    PRD_TYPE opcode;
#else
    PORTABILITY_ASSERT("NYI: CordbProcess::SetUnmanagedBreakpoint, interop debugging NYI on this platform");
    hr = E_NOTIMPL;
    goto ErrExit;
#endif

    // Make sure args are good
    if ((buffer == NULL) || (bufsize < sizeof(patch)) || (bufLen == NULL))
    {
        hr = E_INVALIDARG;
        goto ErrExit;
    }

    // Fail if there's already a patch at this address.
    if (GetNativePatch(CORDB_ADDRESS_TO_PTR(address)) != NULL)
    {
        hr = CORDBG_E_NATIVE_PATCH_ALREADY_AT_ADDR;
        goto ErrExit;
    }

    // Preallocate this now so that if are oom, we can fail before we get half-way through.
    p = m_NativePatchList.Append();
    if (p == NULL)
    {
        hr = E_OUTOFMEMORY;
        goto ErrExit;
    }


    // Read out opcode. 1 byte on x86

    hr = ApplyRemotePatch(this, CORDB_ADDRESS_TO_PTR(address), &p->opcode);
    if (FAILED(hr))
        goto ErrExit;

    // It's all successful, so now update our out-params & internal bookkeaping.
#if defined(TARGET_X86) || defined(TARGET_AMD64)
    opcode = (BYTE)p->opcode;
    buffer[0] = opcode;
#elif defined(TARGET_ARM64)
    opcode = p->opcode;
    memcpy_s(buffer, bufsize, &opcode, sizeof(opcode));
#else
    PORTABILITY_ASSERT("NYI: CordbProcess::SetUnmanagedBreakpoint, interop debugging NYI on this platform");
#endif
    *bufLen = sizeof(opcode);

    p->pAddress = CORDB_ADDRESS_TO_PTR(address);
    p->opcode = opcode;

    _ASSERTE(SUCCEEDED(hr));

ErrExit:
    // If we failed, then free the patch
    if (FAILED(hr) && (p != NULL))
    {
        m_NativePatchList.Delete(*p);
    }

    return hr;

#endif // FEATURE_INTEROP_DEBUGGING
}


//-----------------------------------------------------------------------------
// CordbProcess::ClearUnmanagedBreakpoint
// Called by a native debugger to remove breakpoints during Interop.
// The patch is deleted even if the function fails.
//-----------------------------------------------------------------------------
HRESULT
CordbProcess::ClearUnmanagedBreakpoint(CORDB_ADDRESS address)
{
    LOG((LF_CORDB, LL_INFO100, "CP::ClearUnBP: pProcess=%x, address=%p.\n", this, CORDB_ADDRESS_TO_PTR(address)));
#ifndef FEATURE_INTEROP_DEBUGGING
    return E_NOTIMPL;
#else
    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    FAIL_IF_MANAGED_ONLY(this);

    _ASSERTE(!ThreadHoldsProcessLock());

    HRESULT hr = S_OK;
    PRD_TYPE opcode;

    Lock();

    // Make sure this is a valid patch.
    int cTotal = m_NativePatchList.Count();
    NativePatch * pTable = m_NativePatchList.Table();
    if (pTable == NULL)
    {
        hr = CORDBG_E_NO_NATIVE_PATCH_AT_ADDR;
        goto ErrExit;
    }

    int i;
    for(i = 0; i  < cTotal; i++)
    {
        if (pTable[i].pAddress == CORDB_ADDRESS_TO_PTR(address))
            break;
    }

    if (i >= cTotal)
    {
        hr = CORDBG_E_NO_NATIVE_PATCH_AT_ADDR;
        goto ErrExit;
    }

    // Found it! Remove it from our table. Note that this may shuffle table contents
    // around, so don't keep pointers into the table.
    opcode = pTable[i].opcode;

    m_NativePatchList.Delete(pTable[i]);
    _ASSERTE(m_NativePatchList.Count() == cTotal - 1);

    // Now remove the patch.



    // Just call through to Write ProcessMemory
    hr = RemoveRemotePatch(this, CORDB_ADDRESS_TO_PTR(address), opcode);
    if (FAILED(hr))
        goto ErrExit;


    // Our internal bookeaping was already updated to remove the patch, so now we're done.
    // If we had a failure, we should have already bailed.
    _ASSERTE(SUCCEEDED(hr));

ErrExit:
    Unlock();
    return hr;
#endif // FEATURE_INTEROP_DEBUGGING
}


//------------------------------------------------------------------------------------
// StopCount, Sync, SyncReceived form our stop-status. This status is super-critical
// to most hangs, so we stress log it.
//------------------------------------------------------------------------------------
void CordbProcess::SetSynchronized(bool fSynch)
{
    _ASSERTE(ThreadHoldsProcessLock() || !"Must have process lock to toggle SyncStatus");
    STRESS_LOG1(LF_CORDB, LL_INFO1000, "CP:: set sync=%d\n", fSynch);
    m_synchronized = fSynch;
}

bool CordbProcess::GetSynchronized()
{
    // This can be accessed whether we're Locked or not. This means that the result
    // may change underneath us.
    return m_synchronized;
}

void CordbProcess::IncStopCount()
{
    _ASSERTE(ThreadHoldsProcessLock());
    m_stopCount++;
    STRESS_LOG1(LF_CORDB, LL_INFO1000, "CP:: Inc StopCount=%d\n", m_stopCount);
}
void CordbProcess::DecStopCount()
{
    // We can inc w/ just the process lock (b/c we can dispatch events from the W32ET)
    // But decrementing (eg, Continue), requires the stop-go lock.
    // This if an operation takes the SG lock, it ensures we don't continue from underneath it.
    ASSERT_SINGLE_THREAD_ONLY(HoldsLock(&m_StopGoLock));
    _ASSERTE(ThreadHoldsProcessLock());

    m_stopCount--;
    STRESS_LOG1(LF_CORDB, LL_INFO1000, "CP:: Dec StopCount=%d\n", m_stopCount);
}

// Just gets whether we're stopped or not (m_stopped > 0).
// You only need the StopGo lock for this.
bool CordbProcess::IsStopped()
{
    // We don't require the process-lock, just the SG-lock.
    // Holding the SG lock prevents another thread from continuing underneath you.
    // (see DecStopCount()).
    // But you could still be running free, and have another thread stop-underneath you.
    // Thus IsStopped() leans towards returning false.
    ASSERT_SINGLE_THREAD_ONLY(HoldsLock(&m_StopGoLock));

    return (m_stopCount > 0);
}

int CordbProcess::GetStopCount()
{
    _ASSERTE(ThreadHoldsProcessLock());
    return m_stopCount;
}

bool CordbProcess::GetSyncCompleteRecv()
{
    _ASSERTE(ThreadHoldsProcessLock());
    return m_syncCompleteReceived;
}

void CordbProcess::SetSyncCompleteRecv(bool fSyncRecv)
{
    _ASSERTE(ThreadHoldsProcessLock());
    STRESS_LOG1(LF_CORDB, LL_INFO1000, "CP:: set syncRecv=%d\n", fSyncRecv);
    m_syncCompleteReceived = fSyncRecv;
}

// This can be used if we ever need the RS to emulate old behavior of previous versions.
// This can not be used in QIs to deny queries for new interfaces.
// QIs must be consistent across the lifetime of an object. Say CordbThread used this in a QI
// do deny returning a ICorDebugThread2 interface when emulating v1.1. Once that Thread is neutered,
// it no longer has a pointer to the process, and it no longer knows if it should be denying
// the v2.0 query. An object's QI can't start returning new interfaces onces its neutered.
bool CordbProcess::SupportsVersion(CorDebugInterfaceVersion featureVersion)
{
    _ASSERTE(featureVersion == CorDebugVersion_2_0);
    return true;
}


//---------------------------------------------------------------------------------------
// Add an object to the process's Left-Side resource cleanup list
//
// Arguments:
//    pObject - non-null object to be added
//
// Notes:
//    This list tracks objects with process-scope that hold left-side
//    resources (like func-eval).
//    See code:CordbAppDomain::GetSweepableExitNeuterList for per-appdomain
//    objects with left-side resources.
void CordbProcess::AddToLeftSideResourceCleanupList(CordbBase * pObject)
{
    INTERNAL_API_ENTRY(this);
    _ASSERTE(pObject != NULL);

    m_LeftSideResourceCleanupList.Add(this, pObject);
}

// This list will get actively swept (looking for objects w/ external ref = 0) between continues.
void CordbProcess::AddToNeuterOnExitList(CordbBase *pObject)
{
    INTERNAL_API_ENTRY(this);
    _ASSERTE(pObject != NULL);

    HRESULT hr = S_OK;
    EX_TRY
    {
        this->m_ExitNeuterList.Add(this, pObject);
    }
    EX_CATCH_HRESULT(hr);
    SetUnrecoverableIfFailed(GetProcess(), hr);
}

// Mark that this object should be neutered the next time we Continue the process.
void CordbProcess::AddToNeuterOnContinueList(CordbBase *pObject)
{
    INTERNAL_API_ENTRY(this);
    _ASSERTE(pObject != NULL);

    m_ContinueNeuterList.Add(this, pObject); // throws
}


/* ------------------------------------------------------------------------- *
 * Runtime Controller Event Thread class
 * ------------------------------------------------------------------------- */

//
// Constructor
//
CordbRCEventThread::CordbRCEventThread(Cordb* cordb)
{
    _ASSERTE(cordb != NULL);

    m_cordb.Assign(cordb);
    m_thread = NULL;
    m_threadId = 0;
    m_run = TRUE;
    m_threadControlEvent = NULL;
    m_processStateChanged = FALSE;

    g_pRSDebuggingInfo->m_RCET = this;
}


//
// Destructor. Cleans up all of the open handles and such.
// This expects that the thread has been stopped and has terminated
// before being called.
//
CordbRCEventThread::~CordbRCEventThread()
{
    if (m_threadControlEvent != NULL)
        CloseHandle(m_threadControlEvent);

    if (m_thread != NULL)
        CloseHandle(m_thread);

    g_pRSDebuggingInfo->m_RCET = NULL;
}

//
// Init sets up all the objects that the thread will need to run.
//
HRESULT CordbRCEventThread::Init()
{
    if (m_cordb == NULL)
        return E_INVALIDARG;

    m_threadControlEvent = WszCreateEvent(NULL, FALSE, FALSE, NULL);

    if (m_threadControlEvent == NULL)
        return HRESULT_FROM_GetLastError();

    return S_OK;
}


#if defined(FEATURE_INTEROP_DEBUGGING)
//
// Helper to duplicate a handle or thorw
//
// Arguments:
//     pLocalHandle - handle to duplicate into the remote process
//     pRemoteHandle - RemoteHandle structure in IPC block to hold the remote handle.
// Return value:
//     None. Throws on error.
//
void CordbProcess::DuplicateHandleToLocalProcess(HANDLE * pLocalHandle, RemoteHANDLE * pRemoteHandle)
{
    _ASSERTE(m_pShim != NULL);

    // Dup RSEA and RSER into this process if we don't already have them.
    // On Launch, we don't have them yet, but on attach we do.
    if (*pLocalHandle == NULL)
    {
        BOOL fSuccess = pRemoteHandle->DuplicateToLocalProcess(m_handle, pLocalHandle);
        if (!fSuccess)
        {
            ThrowLastError();
        }
    }

}
#endif // FEATURE_INTEROP_DEBUGGING

// Public entry wrapper for code:CordbProcess::FinishInitializeIPCChannelWorker
void CordbProcess::FinishInitializeIPCChannel()
{
    // This is called directly from a shim callback.
    PUBLIC_API_ENTRY_FOR_SHIM(this);
    FinishInitializeIPCChannelWorker();
}

//
// Initialize the IPC channel. After this, IPC events can flow in both ways.
//
// Return value:
//     Returns S_OK on success.
//
// Notes:
//     This will dispatch an UnrecoverableError callback if it fails.
//     This will also initialize key state in the CordbProcess object.
//
// @dbgtodo remove helper-thread: this should eventually go away once we get rid of IPC events.
//
void CordbProcess::FinishInitializeIPCChannelWorker()
{
    CONTRACTL
    {
        THROWS;
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;
    _ASSERTE(m_pShim != NULL);

    RSLockHolder lockHolder(&this->m_processMutex);

    // If it's already initialized, then nothing left to do.
    // this protects us if this function is called multiple times.
    if (m_initialized)
    {
        _ASSERTE(GetDCB() != NULL);
        return;
    }

    EX_TRY
    {
        LOG((LF_CORDB, LL_INFO1000, "[%x] RCET::HFRCE: first event..., process %p\n", GetCurrentThreadId(), this));

        BOOL fBlockExists;
        GetEventBlock(&fBlockExists); // throws on error

        LOG((LF_CORDB, LL_EVERYTHING, "Size of CdbP is %d\n", sizeof(CordbProcess)));

        m_pEventChannel->Init(m_handle);

#if defined(FEATURE_INTEROP_DEBUGGING)
        DuplicateHandleToLocalProcess(&m_leftSideUnmanagedWaitEvent, &GetDCB()->m_leftSideUnmanagedWaitEvent);
#endif // FEATURE_INTEROP_DEBUGGING

        // Read the Runtime Offsets struct out of the debuggee.
        hr = GetRuntimeOffsets();
        IfFailThrow(hr);

        // we need to be careful here. The LS will have a thread running free that may be initializing
        // fields of the DCB (specifically it may be setting up the helper thread), so we need to make sure
        // we don't overwrite any fields that the LS is writing. We need to be sure we only write to RS
        // status fields.
        m_initialized = true;
        GetDCB()->m_rightSideIsWin32Debugger = IsInteropDebugging();
        UpdateLeftSideDCBField(&(GetDCB()->m_rightSideIsWin32Debugger), sizeof(GetDCB()->m_rightSideIsWin32Debugger));

        LOG((LF_CORDB, LL_INFO1000, "[%x] RCET::HFRCE: ...went fine\n", GetCurrentThreadId()));
        _ASSERTE(SUCCEEDED(hr));

    } EX_CATCH_HRESULT(hr);
    if (SUCCEEDED(hr))
    {
        return;
    }

    // We only land here on failure cases.
    // We must have jumped to this label. Maybe we didn't set HR, so check now.
    STRESS_LOG1(LF_CORDB, LL_INFO1000, "HFCR: FAILED hr=0x%08x\n", hr);

    CloseIPCHandles();

    // Rethrow
    ThrowHR(hr);
}


//---------------------------------------------------------------------------------------
// Marshals over a string buffer in a managed event
//
// Arguments:
//    pTarget - data-target for read the buffer from the LeftSide.
//
// Throws on error
void Ls_Rs_BaseBuffer::CopyLSDataToRSWorker(ICorDebugDataTarget * pTarget)
{
    //
    const DWORD cbCacheSize = m_cbSize;

    // SHOULD not happen for more than once in well-behaved case.
    if (m_pbRS != NULL)
    {
        SIMPLIFYING_ASSUMPTION(!"m_pbRS is non-null; is this a corrupted event?");
        ThrowHR(E_INVALIDARG);
    }

    NewArrayHolder<BYTE> pData(new BYTE[cbCacheSize]);

    ULONG32 cbRead;
    HRESULT hrRead = pTarget->ReadVirtual(PTR_TO_CORDB_ADDRESS(m_pbLS), pData, cbCacheSize , &cbRead);

    if(FAILED(hrRead))
    {
        hrRead = CORDBG_E_READVIRTUAL_FAILURE;
    }

    if (SUCCEEDED(hrRead) && (cbCacheSize != cbRead))
    {
        hrRead = HRESULT_FROM_WIN32(ERROR_PARTIAL_COPY);
    }
    IfFailThrow(hrRead);

    // Now do Transfer
    m_pbRS = pData;
    pData.SuppressRelease();
}

//---------------------------------------------------------------------------------------
// Marshals over a Byte buffer in a managed event
//
// Arguments:
//    pTarget - data-target for read the buffer from the LeftSide.
//
// Throws on error
void Ls_Rs_ByteBuffer::CopyLSDataToRS(ICorDebugDataTarget * pTarget)
{
    CopyLSDataToRSWorker(pTarget);
}

//---------------------------------------------------------------------------------------
// Marshals over a string buffer in a managed event
//
// Arguments:
//    pTarget - data-target for read the buffer from the LeftSide.
//
// Throws on error
void Ls_Rs_StringBuffer::CopyLSDataToRS(ICorDebugDataTarget * pTarget)
{
    CopyLSDataToRSWorker(pTarget);

    // Ensure we're a valid, well-formed string.
    // @dbgtodo - this should only happen in corrupted scenarios. Perhaps a better HR here?
    // - null terminated.
    // - no embedded nulls.

    const WCHAR * pString = GetString();
    SIZE_T dwExpectedLenWithNull = m_cbSize / sizeof(WCHAR);

    // Should at least have 1 character for the null-terminator.
    if (dwExpectedLenWithNull == 0)
    {
        ThrowHR(CORDBG_E_TARGET_INCONSISTENT);
    }

    // Ensure that there's a null where we expect it to be.
    if (pString[dwExpectedLenWithNull-1] != 0)
    {
        ThrowHR(CORDBG_E_TARGET_INCONSISTENT);
    }

    // Now we know it's safe to call wcslen. The buffer is local, so we know the pages are there.
    // And we know there's a null capping the max length of the string.
    SIZE_T dwActualLenWithNull = wcslen(pString) + 1;
    if (dwActualLenWithNull != dwExpectedLenWithNull)
    {
        ThrowHR(CORDBG_E_TARGET_INCONSISTENT);
    }
}

//---------------------------------------------------------------------------------------
// Marshals the arguments in a managed-debug event.
//
// Arguments:
//    pManagedEvent - (IN/OUT) debug event to marshal. Events are not usable in the host process
//       until they are marshalled. This will marshal the event in-place, and may convert
//       some target addresses to host addresses.
//
// Return Value:
//    S_OK on success. Else Error.
//
// Assumptions:
//    Target is currently stopped and inspectable.
//    After the event is marshalled, it has resources that must be cleaned up
//    by calling code:DeleteIPCEventHelper.
//
// Notes:
//     Call a Copy function (CopyManagedEventFromTarget, CopyRCEventFromIPCBlock)to
//     get the event to marshal.
//     This will marshal args from the target into the host.
//     The debug event is fixed size. But since the debuggee is stopped, this can copy
//     arbitrary-length buffers out of of the debuggee.
//
//     This could be rolled into code:CordbProcess::RawDispatchEvent
//---------------------------------------------------------------------------------------
void CordbProcess::MarshalManagedEvent(DebuggerIPCEvent * pManagedEvent)
{
    CONTRACTL
    {
        THROWS;

        // Event has already been copied, now we do some quick Marshalling.
        // Thsi should be a private local copy, and not the one in the IPC block or Target.
        PRECONDITION(CheckPointer(pManagedEvent));
    }
    CONTRACTL_END;

    IfFailThrow(pManagedEvent->hr);

    // This may throw part way through marshalling. But that's ok because
    // code:DeleteIPCEventHelper can cleanup a partially-marshalled event.

    // Do a pre-processing on the event
    switch (pManagedEvent->type & DB_IPCE_TYPE_MASK)
    {
        case DB_IPCE_MDA_NOTIFICATION:
        {
            pManagedEvent->MDANotification.szName.CopyLSDataToRS(this->m_pDACDataTarget);
            pManagedEvent->MDANotification.szDescription.CopyLSDataToRS(this->m_pDACDataTarget);
            pManagedEvent->MDANotification.szXml.CopyLSDataToRS(this->m_pDACDataTarget);
            break;
        }

        case DB_IPCE_FIRST_LOG_MESSAGE:
        {
            pManagedEvent->FirstLogMessage.szContent.CopyLSDataToRS(this->m_pDACDataTarget);
            break;
        }

        default:
            break;
    }


}


//---------------------------------------------------------------------------------------
// Copy a managed debug event from the target process into this local process
//
// Arguments:
//    pRecord - native-debug event serving as the envelope for the managed event.
//    pLocalManagedEvent - (dst) required local buffer to hold managed event.
//
// Return Value:
//    * True if the event belongs to this runtime. This is very useful when multiple CLRs are
//    loaded into the target and all sending events wit the same exception code.
//    * False if this does not belong to this instance of ICorDebug. (perhaps it's an event
//    intended for another instance of the CLR in the target, or some rogue user code happening
//    to use our exception code).
//    In either case, the event can still be cleaned up via code:DeleteIPCEventHelper.
//
//    Throws on error. In the error case, the contents of pLocalManagedEvent are undefined.
//    They may have been partially copied from the target. The local managed event does not own
//    any resources until it's marshalled, so the buffer can be ignored if this function fails.
//
// Assumptions:
//
// Notes:
//    The events are sent form the target via code:Debugger::SendRawEvent
//    This just does a raw Byte copy, but does not do any Marshalling.
//    This should always succeed in the well-behaved case. However, A bad debuggee can
//    always send a poor-formed debug event.
//    We don't distinguish between a badly formed event and an event that's not ours.
//    The event still needs to be Marshaled before being used. (see code:CordbProcess::MarshalManagedEvent)
//
//---------------------------------------------------------------------------------------
#if defined(_MSC_VER) && defined(TARGET_ARM)
// This is a temporary workaround for an ARM specific MS C++ compiler bug (internal LKG build 18.1).
// Branch < if (ptrRemoteManagedEvent == NULL) > was always taken and the function always returned false.
// TODO: It should be removed once the bug is fixed.
#pragma optimize("", off)
#endif
bool CordbProcess::CopyManagedEventFromTarget(
    const EXCEPTION_RECORD * pRecord,
    DebuggerIPCEvent * pLocalManagedEvent)
{
    _ASSERTE(pRecord != NULL);
    _ASSERTE(pLocalManagedEvent != NULL);

    // Initialize the event enough such backout code can call code:DeleteIPCEventHelper.
    pLocalManagedEvent->type = DB_IPCE_DEBUGGER_INVALID;

    // Ensure we have a CLR instance ID by now.  Either we had one already, or we're in
    // V2 mode and this is the startup event, and so we'll set it now.
    HRESULT hr = EnsureClrInstanceIdSet();
    IfFailThrow(hr);
    _ASSERTE(m_clrInstanceId != 0);

    // Determine if the event is really a debug event, and for our instance.
    CORDB_ADDRESS ptrRemoteManagedEvent = IsEventDebuggerNotification(pRecord, m_clrInstanceId);

    if (ptrRemoteManagedEvent == NULL)
    {
        return false;
    }

    // What we are doing on Windows here is dangerous.  Any buffer for IPC events must be at least
    // CorDBIPC_BUFFER_SIZE big, but here we are only copying sizeof(DebuggerIPCEvent).  Fortunately, the
    // only case where an IPC event is bigger than sizeof(DebuggerIPCEvent) is for the second category
    // described in the comment for code:IEventChannel.  In this case, we are just transferring the IPC
    // event from the native pipeline to the event channel, and the event channel will read it directly from
    // the send buffer on the LS.  See code:CordbRCEventThread::WaitForIPCEventFromProcess.
#if !defined(FEATURE_DBGIPC_TRANSPORT_DI)
    hr = SafeReadStruct(ptrRemoteManagedEvent, pLocalManagedEvent);
#else
    // For Mac remote debugging the address returned above is actually a local address.
    // Also, we need to copy the entire buffer because once a debug event is read from the debugger
    // transport, it won't be available afterwards.
    memcpy(reinterpret_cast<BYTE *>(pLocalManagedEvent),
           CORDB_ADDRESS_TO_PTR(ptrRemoteManagedEvent),
           CorDBIPC_BUFFER_SIZE);
    hr = S_OK;
#endif
    SIMPLIFYING_ASSUMPTION(SUCCEEDED(hr));
    IfFailThrow(hr);

    return true;
}
#if defined(_MSC_VER) && defined(TARGET_ARM)
#pragma optimize("", on)
#endif

//---------------------------------------------------------------------------------------
// EnsureClrInstanceIdSet - Ensure we have a CLR Instance ID to debug
//
// In Arrowhead scenarios, the debugger is required to pass a valid CLR instance ID
// to us in OpenVirtualProcess.  In V2 scenarios, for compatibility, we'll allow a
// CordbProcess object to exist for a process that doesn't yet have the CLR loaded.
// In this case the CLR instance ID will start off as 0, but be filled in when we see the
// startup exception indicating the CLR has been loaded.
//
// If we don't already have an instance ID, this function sets it to the only CLR in the
// target process.  This requires that a CLR be loaded in the target process.
//
// Return Value:
//    S_OK - if m_clrInstanceId was already set, or is now set to a valid CLR instance ID
//    an error HRESULT - if m_clrInstanceId was 0, and cannot be set to a valid value
//                       (i.e. because we cannot find a CLR in the target process).
//
//    Note that we need to probe for this on attach, and it's common to attach before the
//    CLR has been loaded, so we avoid using exceptions for this common case.
//
HRESULT CordbProcess::EnsureClrInstanceIdSet()
{
    // If we didn't expect a specific CLR, then attempt to attach to any.
    if (m_clrInstanceId == 0)
    {
        // The only case in which we were allowed to request the "default" CLR instance
        // ID is when we're running in V2 mode.  In V3, the client is required to pass
        // a non-zero value to OpenVirtualProcess. Since V2 is no longer supported we
        // no longer attempt to find it.
        if(m_cordb->GetTargetCLR() != 0)
        {
            m_clrInstanceId = PTR_TO_CORDB_ADDRESS(m_cordb->GetTargetCLR());
            return S_OK;
        }

        // In V3, the client is required to pass a non-zero value to OpenVirtualProcess.
        // In V2 mode we should be setting target CLR up front but return an error
        // if we haven't.
        _ASSERTE(m_pShim != NULL);
        return E_UNEXPECTED;

    }

    // We've (now) got a valid CLR instance id
    return S_OK;
}

//---------------------------------------------------------------------------------------
// // Copy event from IPC block into local.
//
// Arguments:
//    pLocalManagedEvent - required local buffer to hold managed event.
//
// Return Value:
//    None. Always succeeds.
//
// Assumptions:
//    The IPC block has already been opened and filled in with an event.
//
// Notes:
//    This is copying from a shared-memory block, which is treated as local memory.
//    This just does a raw Byte copy, but does not do any Marshalling.
//    This does no validation on the event.
//    The event still needs to be Marshaled before being used. (see code:CordbProcess::MarshalManagedEvent)
//
//---------------------------------------------------------------------------------------
void inline CordbProcess::CopyRCEventFromIPCBlock(DebuggerIPCEvent * pLocalManagedEvent)
{
    _ASSERTE(pLocalManagedEvent != NULL);

    IfFailThrow(m_pEventChannel->GetEventFromLeftSide(pLocalManagedEvent));
}

// Return true if this is the RCEvent thread, else false.
bool CordbRCEventThread::IsRCEventThread()
{
    return (m_threadId == GetCurrentThreadId());
}

//---------------------------------------------------------------------------------------
// Runtime assert, throws CORDBG_E_TARGET_INCONSISTENT if the expression is not true.
//
// Arguments:
//     fExpression - assert parameter. If true, this function is a nop. If false,
//             this will throw a CORDBG_E_TARGET_INCONSISTENT error.
//
// Notes:
//     Use this for runtime checks to validate assumptions about the data-target.
//     IcorDebug can't trust that data from the debugee is consistent (perhaps it's
//     corrupted).
void CordbProcess::TargetConsistencyCheck(bool fExpression)
{
    if (!fExpression)
    {
        STRESS_LOG0(LF_CORDB, LL_INFO10000, "Target consistency check failed");

        // When debugging possibly corrupt targets, this failure may be expected.  For debugging purposes,
        // assert if we're not expecting any target inconsistencies.
        CONSISTENCY_CHECK_MSG( !m_fAssertOnTargetInconsistency, "Target consistency check failed unexpectedly");

        ThrowHR(CORDBG_E_TARGET_INCONSISTENT);
    }
}

//
// SendIPCEvent -- send an IPC event to the runtime controller. All this
// really does is copy the event into the process's send buffer and sets
// the RSEA then waits on RSER.
//
// Note: when sending a two-way event (replyRequired = true), the
// eventSize must be large enough for both the event sent and the
// result event.
//
// Returns whether the event was sent successfully. This is different than event->eventHr.
//
HRESULT CordbRCEventThread::SendIPCEvent(CordbProcess* process,
                                         DebuggerIPCEvent* event,
                                         SIZE_T eventSize)
{

    _ASSERTE(process != NULL);
    _ASSERTE(event != NULL);
    _ASSERTE(process->GetShim() != NULL);

#ifdef _DEBUG
    // We need to be synchronized whenever we're sending an IPC Event.
    // This may require our callers' using a Stop-Continue holder.
    // Attach + AsyncBreak are the only (obvious) exceptions.
    // For continue, we set Sync-Status to false before sending, so we exclude that too.
    // Everybody else should only be sending events when synced. We should never ever ever
    // send an event from a CorbXYZ dtor (b/c that would be called at any random time). Instead,
    // use a NeuterList.
    switch (event->type)
    {
        case DB_IPCE_ATTACHING:
        case DB_IPCE_ASYNC_BREAK:
        case DB_IPCE_CONTINUE:
            break;

        default:
            CONSISTENCY_CHECK_MSGF(process->GetSynchronized(), ("Must by synced while sending IPC event: %s (0x%x)",
                IPCENames::GetName(event->type), event->type));
    }
#endif


    LOG((LF_CORDB, LL_EVERYTHING, "SendIPCEvent in CordbRCEventThread called\n"));

    // For simplicity sake, we have the following conservative invariants when sending IPC events:
    // - Always hold the Stop-Go lock.
    // - never on the W32ET.
    // - Never hold the Process-lock (this allows the w32et to take that lock to pump)

    // Must have the stop-go lock to send an IPC event.
    CONSISTENCY_CHECK_MSGF(process->GetStopGoLock()->HasLock(), ("Must have stop-go lock to send event. proc=%p, event=%s",
        process, IPCENames::GetName(event->type)));

    // The w32 ET will need to take the process lock. So if we're holding it here, then we'll
    // deadlock (since W32 ET is blocked on lock, which we would hold; and we're blocked on W32 ET
    // to keep pumping.
    _ASSERTE(!process->ThreadHoldsProcessLock() || !"Can't hold P-lock while sending blocking IPC event");


    // Can't be on the w32 ET, or we can't be pumping.
    // Although we can trickle in here from public APIs, our caller should have validated
    // that we weren't on the w32et, so the assert here is justified. But just in case there's something we missed,
    // we have a runtime check (as a final backstop against a deadlock).
    _ASSERTE(!process->IsWin32EventThread());
    CORDBFailIfOnWin32EventThread(process);


    // If this is an async event, then we expect it to be sent while the process is locked.
    if (event->asyncSend)
    {
        // This may be on the w32et, so we can't hold the stop-go lock.
        _ASSERTE(event->type == DB_IPCE_ATTACHING); // only async event should be attaching.
    }


    // This will catch us if we've detached or exited.
    // Note if we exited, then we should have been neutered and so shouldn't even be sending an IPC event,
    // but just in case, we'll check.
    CORDBRequireProcessStateOK(process);


#ifdef _DEBUG
    // We should never send an Async Break on the RCET. This will deadlock.
    // - if we're on the RCET, we should be stopped, and thus Stop() should just bump up a stop count,
    //   and not actually send an AsyncBreak.
    // - Delayed-Continues help enforce this.
    // This is a special case of the deadlock check below.
    if (IsRCEventThread())
    {
        _ASSERTE(event->type != DB_IPCE_ASYNC_BREAK);
    }
#endif

#ifdef _DEBUG
    // This assert protects us against a deadlock.
    // 1) (RCET) blocked on (This function): If we're on the RCET, then the RCET is blocked until we return (duh).
    // 2) (LS) blocked on (RCET): If the LS is not synchronized, then it may be sending an event to the RCET, and thus blocked on the RCET.
    // 3) (Helper thread) blocked on (LS): That LS thread may be holding a lock that the helper thread needs, thus blocking the helper thread.
    // 4) (This function) blocked on (Helper Thread): We block until the helper thread can process our IPC event.
    //     #4 is not true for async events.
    //
    // If we hit this assert, it means we may get the deadlock above and we're calling SendIPCEvent at a time we shouldn't.
    // Note this race is as old as dirt.
    if (IsRCEventThread() && !event->asyncSend)
    {
        // Note that w/ Continue & Attach, GetSynchronized() has a different meaning and the race above won't happen.
        BOOL fPossibleDeadlock = process->GetSynchronized() || (event->type == DB_IPCE_CONTINUE) || (event->type == DB_IPCE_ATTACHING);
        CONSISTENCY_CHECK_MSGF(fPossibleDeadlock, ("Possible deadlock while sending: '%s'\n", IPCENames::GetName(event->type)));
    }
#endif



    // Cache this process into the MRU so that we can find it if we're debugging in retail.
    g_pRSDebuggingInfo->m_MRUprocess = process;

    HRESULT hr = S_OK;
    HRESULT hrEvent = S_OK;
    _ASSERTE(event != NULL);

    // NOTE: the eventSize parameter is only so you can specify an event size that is SMALLER than the process send
    // buffer size!!
    if (eventSize > CorDBIPC_BUFFER_SIZE)
        return E_INVALIDARG;

    STRESS_LOG4(LF_CORDB, LL_INFO1000, "CRCET::SIPCE: sending %s to AD 0x%x, proc 0x%x(%d)\n",
         IPCENames::GetName(event->type), VmPtrToCookie(event->vmAppDomain), process->m_id, process->m_id);

    // For 2-way events, this check is unnecessary (since we already check for LS exit)
    // But for async events, we need this.
    // So just check it up here and make everyone's life easier.
    if (process->m_terminated)
    {
        STRESS_LOG0(LF_CORDB, LL_INFO10000, "CRCET::SIPCE: LS already terminated, shortcut exiting\n");
        return CORDBG_E_PROCESS_TERMINATED;
    }

    // If the helper thread has died, we can't send an IPC event (and it's never coming back either).
    // Although we do wait on the thread's handle, there are strange windows where the thread's handle
    // is not yet signaled even though we've continued from the exit-thread event for the helper.
    if (process->m_helperThreadDead)
    {
        STRESS_LOG0(LF_CORDB, LL_INFO10000, "CRCET::SIPCE: Helper-thread dead, shortcut exiting\n");
        return CORDBG_E_PROCESS_TERMINATED;
    }

    BOOL fUnrecoverableError = TRUE;
    EX_TRY
    {
        hr = process->GetEventChannel()->SendEventToLeftSide(event, eventSize);
        fUnrecoverableError = FALSE;
    }
    EX_CATCH_HRESULT(hr);


    // If we're sending a Continue() event, then after this, the LS may run free.
    // If this is the last managed event before the LS exits, (which is the case
    // if we're responding to either an Exit-Thread or if we respond to a Detach)
    // the LS may exit at anytime from here on, so we need to be careful.


    if (fUnrecoverableError)
    {
        _ASSERTE(FAILED(hr));
        CORDBSetUnrecoverableError(process, hr, 0);
    }
    else
    {
        // Get a handle to the target process - this call always succeeds
        HANDLE hLSProcess = NULL;
        process->GetHandle(&hLSProcess);

        // We take locks to ensure that the CordbProcess object is still alive,
        // even if the OS process exited.
        _ASSERTE(hLSProcess != NULL);

        // Check if Sending the IPC event failed
        if (FAILED(hr))
        {
            // The failure to send an event may be due to the target process terminating
            // (especially, but not exclusively, in the case of async events).
            // There is a race here - we can't rely on any check above SendEventToLeftSide
            // to tell us whether the process has exited yet.
            // Check for that case and return an accurate hresult.
            DWORD ret = WaitForSingleObject(hLSProcess, 0);
            if (ret == WAIT_OBJECT_0)
            {
                return CORDBG_E_PROCESS_TERMINATED;
            }

            // Some other failure sending the IPC event - just return it.
            return hr;
        }

        STRESS_LOG0(LF_CORDB, LL_INFO1000, "CRCET::SIPCE: sent...\n");

        // If this is an async send, then don't wait for the left side to acknowledge that its read the event.
        _ASSERTE(!event->asyncSend || !event->replyRequired);

        if (process->GetEventChannel()->NeedToWaitForAck(event))
        {
            STRESS_LOG0(LF_CORDB, LL_INFO1000,"CRCET::SIPCE: waiting for left side to read event. (on RSER)\n");

            DWORD ret;

            // Wait for either a reply (common case) or the left side to go away.
            // We can't detach while waiting for a reply (because detach needs to send events).
            // All of the outcomes from this wait are completely disjoint.
            // It's possible for the LS to reply and then exit normally (Thread_Detach, Process_Detach)
            // and so ExitProcess may have been called, but it doesn't matter.

            enum {
                ID_RSER = WAIT_OBJECT_0,
                ID_LSPROCESS,
                ID_HELPERTHREAD,
            };

            // Only wait on the helper thread for cases where the process is stopped (and thus we don't expect it do exit on us).
            // If the process is running and we lose our helper thread, it ought to be during shutdown and we ough to
            // follow up with an exit.
            // This includes when we've dispatch Native events, and it includes the AsyncBreak sent to get us from a
            // win32 frozen state to a synchronized state).
            HANDLE hHelperThread = NULL;
            if (process->IsStopped())
            {
                hHelperThread = process->GetHelperThreadHandle();
            }


            // Note that in case of a tie (multiple handles signaled), WaitForMultipleObjects gives
            // priority to the handle earlier in the array.
            HANDLE waitSet[] = { process->GetEventChannel()->GetRightSideEventAckHandle(), hLSProcess, hHelperThread};
            DWORD cWaitSet = ARRAY_SIZE(waitSet);
            if (hHelperThread == NULL)
            {
                cWaitSet--;
            }

            do
            {
                ret = WaitForMultipleObjectsEx(cWaitSet, waitSet, FALSE, CordbGetWaitTimeout(), FALSE);
                // If we timeout because we're waiting for an uncontinued OOB event, we need to just keep waiting.
            } while ((ret == WAIT_TIMEOUT) && process->IsWaitingForOOBEvent());

            switch(ret)
            {
            case ID_RSER:
                // Normal reply from LS.
                // This is set iff the LS replied to our event. The LS may have exited since it replied
                // but we don't care. We still have the reply and we'll pass it on.
                STRESS_LOG0(LF_CORDB, LL_INFO1000, "CRCET::SIPCE: left side read the event.\n");

                // If this was a two-way event, then the result is already ready for us. Simply copy the result back
                // over the original event that was sent. Otherwise, the left side has simply read the event and is
                // processing it...
                if (event->replyRequired)
                {
                    process->GetEventChannel()->GetReplyFromLeftSide(event, eventSize);
                    hrEvent = event->hr;
                }
                break;

            case ID_LSPROCESS:
                // Left side exited on us.
                // ExitProcess may or may not have been called here (since it's on a different thread).
                STRESS_LOG0(LF_CORDB, LL_INFO1000, "CRCET::SIPCE: left side exiting while RS was waiting for reply.\n");
                hr = CORDBG_E_PROCESS_TERMINATED;
                break;

            case ID_HELPERTHREAD:
                // We can only send most IPC events while the LS is synchronized. We shouldn't lose our helper thread
                // when synced under any sort of normal conditions.
                // This won't fire if the process already exited, because LSPROCESS gets higher priority in the wait
                // (since it was placed earlier).
                // Thus the only "legitimate" window where this could happen would be in a shutdown scenario after
                // the helper is dead but before the process has died. We shouldn't be synced in that scenario,
                // so we shouldn't be sending IPC events during it.
                STRESS_LOG0(LF_CORDB, LL_INFO1000, "CRCET::SIPCE: lost helper thread.\n");


                // Assert because we want to know if we ever actually hit this in any detectable scenario.
                // However, shutdown can occur in preemptive mode. Thus if the RS does an AsyncBreak late
                // enough, then the LS will appear to be stopped but may still shutdown.
                // Since the debuggee can exit asynchronously at any time (eg, suppose somebody forcefully
                // kills it with taskman), this doesn't introduce a new case.
                // That aside, it would be great to be able to assert this:
                //_ASSERTE(!"Potential deadlock - Randomly Lost helper thread");

                // We'll piggy back this on the terminated case.
                hr = CORDBG_E_PROCESS_TERMINATED;
                break;

            default:
                {
                    // If we timed out/failed, check the left side to see if it is in the unrecoverable error mode. If it is,
                    // return the HR from the left side that caused the error.  Otherwise, return that we timed out and that
                    // we don't really know why.
                    HRESULT realHR = (ret == WAIT_FAILED) ? HRESULT_FROM_GetLastError() : ErrWrapper(CORDBG_E_TIMEOUT);

                    hr = process->CheckForUnrecoverableError();

                    if (hr == S_OK)
                    {
                        CORDBSetUnrecoverableError(process, realHR, 0);
                        hr = realHR;
                    }

                    STRESS_LOG1(LF_CORDB, LL_INFO1000, "CRCET::SIPCE: left side timeout/fail while RS waiting for reply. hr = 0x%08x\n", hr);
                }
                break;
            }

            // If the LS picked up RSEA, it will be reset (since it's an auto event).
            // But in the case that the wait failed or  that the LS exited, we need to explicitly reset RSEA
            if (hr != S_OK)
            {
                process->GetEventChannel()->ClearEventForLeftSide();
            }

            // Done waiting for reply.

        }
    }

    process->ForceDacFlush();

    // The hr and hrEvent are 2 very different things.
    // hr tells us whether the event was sent successfully.
    // hrEvent tells us how the LS responded to it.
    // if FAILED(hr), then hrEvent is useless b/c the LS never got it.
    // But if SUCCEEDED(hr), then hrEvent may still have failed and that could be
    // valuable information.

    return hr;
}

//---------------------------------------------------------------------------------------
// FlushQueuedEvents flushes a process's event queue.
//
// Arguments:
//    pProcess - non-null process object whose queue will be drained
//
// Notes:
//    @dbgtodo shim: this should be part of the shim.
//    This dispatches events that are queued up. The queue is populated by
//    the shim's proxy callback (see code:ShimProxyCallback). This will dispatch events
//    to the 'real' callback supplied by the debugger. This will dispatch events
//    as long as the debugger keeps calling continue.
//
//    This requires that the process lock be held, although it will toggle the lock.
void CordbRCEventThread::FlushQueuedEvents(CordbProcess* process)
{
    CONTRACTL
    {
        NOTHROW; // This is happening on the RCET thread, so there's no place to propagate an error back up.
    }
    CONTRACTL_END;

    STRESS_LOG0(LF_CORDB,LL_INFO10000, "CRCET::FQE: Beginning to flush queue\n");

    _ASSERTE(process->GetShim() != NULL);

    // We should only call this is we already have queued events
    _ASSERTE(!process->GetShim()->GetManagedEventQueue()->IsEmpty());

    //
    // Dispatch queued events so long as they keep calling Continue()
    // before returning from their callback. If they call Continue(),
    // process->m_synchronized will be false again and we know to
    // loop around and dispatch the next event.
    //
    _ASSERTE(process->ThreadHoldsProcessLock());


    // Give shim a chance to queue any faked attach events.  Grab a pointer to the
    // ShimProcess now, while we still hold the process lock.  Once we release the lock,
    // GetShim() may not work.
    RSExtSmartPtr<ShimProcess> pShim(process->GetShim());

    // Release lock before we call out to shim to Queue fake events.
    {
        RSInverseLockHolder inverseLockHolder(process->GetProcessLock());
        {
            PUBLIC_CALLBACK_IN_THIS_SCOPE0_NO_LOCK(pProcess);

            // Because we've released the lock, at any point from here forward the
            // CorDbProcess may suddenly get neutered if the user detaches the debugger.

            pShim->QueueFakeAttachEventsIfNeeded(false);
        }
    }

    // Now that we're holding the process lock again, we can safely check whether
    // process has become neutered
    if (process->IsNeutered())
    {
        return;
    }

    {

        // Main dispatch loop here. DispatchRCEvent will take events out of the
        // queue and invoke callbacks
        do
        {
            // DispatchRCEvent will mark the process as stopped before dispatching.
            process->DispatchRCEvent();

            LOG((LF_CORDB,LL_INFO10000, "CRCET::FQE: Finished w/ "
                 "DispatchRCEvent\n"));
        }
        while (process->GetSyncCompleteRecv() &&
               (process->GetSynchronized() == false) &&
               (process->GetShim() != NULL) && // may have lost Shim if we detached while dispatch
               (!process->GetShim()->GetManagedEventQueue()->IsEmpty()) &&
               (process->m_unrecoverableError == false));
    }

    //
    // If they returned from a callback without calling Continue() then
    // the process is still synchronized, so let the rc event thread
    // know that it need to update its process list and remove the
    // process's event.
    //
    if (process->GetSynchronized())
    {
        ProcessStateChanged();
    }

    LOG((LF_CORDB,LL_INFO10000, "CRCET::FQE: finished\n"));
}

//---------------------------------------------------------------------------------------
// Preliminary Handle an Notification event from the target. This may queue the event,
// but does not actually dispatch the event.
//
// Arguments:
//    pManagedEvent - local managed-event. On success, this function assumes ownership of the
//        event and will delete its memory. Assumed that caller allocated via 'new'.
//    pCallback - callback obecjt to dispatch events on.
//
// Return Value:
//    None. Throws on error. On error, caller still owns the pManagedEvent and must free it.
//
// Assumptions:
//    This should be called once a notification event is received from the target.
//
// Notes:
//    HandleRCEvent -- handle an IPC event received from the runtime controller.
//    This will update ICorDebug state and immediately dispatch the event.
//
//---------------------------------------------------------------------------------------
void CordbProcess::HandleRCEvent(
    DebuggerIPCEvent *         pManagedEvent,
    RSLockHolder *             pLockHolder,
    ICorDebugManagedCallback * pCallback)
{
    CONTRACTL
    {
        THROWS;
        PRECONDITION(CheckPointer(pManagedEvent));
        PRECONDITION(CheckPointer(pCallback));
        PRECONDITION(ThreadHoldsProcessLock());
    }
    CONTRACTL_END;

    if (!this->IsSafeToSendEvents() || this->m_exiting)
    {
        return;
    }

    // Marshals over some standard data from event.
    MarshalManagedEvent(pManagedEvent);

    STRESS_LOG4(LF_CORDB, LL_INFO1000, "RCET::TP: Got %s for AD 0x%x, proc 0x%x(%d)\n",
        IPCENames::GetName(pManagedEvent->type), VmPtrToCookie(pManagedEvent->vmAppDomain), this->m_id, this->m_id);

    RSExtSmartPtr<ICorDebugManagedCallback2> pCallback2;
    pCallback->QueryInterface(IID_ICorDebugManagedCallback2, reinterpret_cast<void **> (&pCallback2));

    RSExtSmartPtr<ICorDebugManagedCallback3> pCallback3;
    pCallback->QueryInterface(IID_ICorDebugManagedCallback3, reinterpret_cast<void **> (&pCallback3));

    RSExtSmartPtr<ICorDebugManagedCallback4> pCallback4;
    pCallback->QueryInterface(IID_ICorDebugManagedCallback4, reinterpret_cast<void **> (&pCallback4));

    // Dispatch directly. May not necessarily dispatch an event.
    // Toggles the lock to dispatch callbacks.
    RawDispatchEvent(pManagedEvent, pLockHolder, pCallback, pCallback2, pCallback3, pCallback4);
}

//
// ProcessStateChanged -- tell the rc event thread that the ICorDebug's
// process list has changed by setting its flag and thread control event.
// This will cause the rc event thread to update its set of handles to wait
// on.
//
void CordbRCEventThread::ProcessStateChanged()
{
    m_cordb->LockProcessList();
    STRESS_LOG0(LF_CORDB, LL_INFO100000, "CRCET::ProcessStateChanged\n");
    m_processStateChanged = TRUE;
    SetEvent(m_threadControlEvent);
    m_cordb->UnlockProcessList();
}


//---------------------------------------------------------------------------------------
// Primary loop of the Runtime Controller event thread.  This routine loops during the
// debug session taking IPC events from the IPC block and calling out to process them.
//
// Arguments:
//    None.
//
// Return Value:
//    None.
//
// Notes:
//    @dbgtodo shim: eventually hoist the entire RCET into the shim.
//---------------------------------------------------------------------------------------
void CordbRCEventThread::ThreadProc()
{
    HANDLE         waitSet[MAXIMUM_WAIT_OBJECTS];
    CordbProcess * rgProcessSet[MAXIMUM_WAIT_OBJECTS];
    unsigned int   waitCount;

#ifdef _DEBUG
    memset(&rgProcessSet, NULL, MAXIMUM_WAIT_OBJECTS * sizeof(CordbProcess *));
    memset(&waitSet, NULL, MAXIMUM_WAIT_OBJECTS * sizeof(HANDLE));
#endif


    // First event to wait on is always the thread control event.
    waitSet[0] = m_threadControlEvent;
    rgProcessSet[0] = NULL;
    waitCount = 1;

    while (m_run)
    {
        DWORD dwStatus = WaitForMultipleObjectsEx(waitCount, waitSet, FALSE, 2000, FALSE);

        if (dwStatus == WAIT_FAILED)
        {
            STRESS_LOG1(LF_CORDB, LL_INFO10000, "CordbRCEventThread::ThreadProc WaitFor"
                        "MultipleObjects failed: 0x%x\n", GetLastError());
        }
#ifdef _DEBUG
        else if ((dwStatus >= WAIT_OBJECT_0) && (dwStatus < WAIT_OBJECT_0 + waitCount) && m_run)
        {
            // Got an event. Figure out which process it came from.
            unsigned int procNumber = dwStatus - WAIT_OBJECT_0;

            if (procNumber != 0)
            {
                // @dbgtodo shim: rip all of this out. Leave the assert in for now to verify that we're not accidentally
                // going down this codepath. Once we rip this out, we can also simplify some of the code below.
                // Notification events  (including Sync-complete) should be coming from Win32 event thread via
                // V3 pipeline.
                _ASSERTE(!"Shouldn't be here");

            }
        }
#endif

        // Empty any queued work items.
        DrainWorkerQueue();

        // Check a flag to see if we need to update our list of processes to wait on.
        if (m_processStateChanged)
        {
            STRESS_LOG0(LF_CORDB, LL_INFO1000, "RCET::TP: refreshing process list.\n");

            unsigned int i;

            //
            // free the old wait list
            //
            for (i = 1; i < waitCount; i++)
            {
                rgProcessSet[i]->InternalRelease();
            }

            // Pass 1: iterate the hash of all processes and collect the unsynchronized ones into the wait list.
            // Note that Stop / Continue can still be called on a different thread while we're doing this.
            m_cordb->LockProcessList();
            m_processStateChanged = FALSE;

            waitCount = 1;

            CordbSafeHashTable<CordbProcess> * pHashTable = m_cordb->GetProcessList();
            HASHFIND hashFind;
            CordbProcess * pProcess;

            for (pProcess =  pHashTable->FindFirst(&hashFind); pProcess != NULL; pProcess = pHashTable->FindNext(&hashFind))
            {
                _ASSERTE(waitCount < MAXIMUM_WAIT_OBJECTS);

                if( waitCount >= MAXIMUM_WAIT_OBJECTS )
                {
                    break;
                }

                // Only listen to unsynchronized processes. Processes that are synchronized will not send events without
                // being asked by us first, so there is no need to async listen to them.
                //
                // Note: if a process is not synchronized then there is no way for it to transition to the synchronized
                // state without this thread receiving an event and taking action. So there is no need to lock the
                // per-process mutex when checking the process's synchronized flag here.
                if (!pProcess->GetSynchronized() && pProcess->IsSafeToSendEvents())
                {
                    STRESS_LOG2(LF_CORDB, LL_INFO1000, "RCET::TP: listening to process 0x%x(%d)\n",
                                pProcess->m_id, pProcess->m_id);

                    waitSet[waitCount] = pProcess->m_leftSideEventAvailable;
                    rgProcessSet[waitCount] = pProcess;
                    rgProcessSet[waitCount]->InternalAddRef();
                    waitCount++;
                }
            }

            m_cordb->UnlockProcessList();

            // Pass 2: for each process that we placed in the wait list, determine if there are any existing queued
            // events that need to be flushed.

            // Start i at 1 to skip the control event...
            i = 1;

            while(i < waitCount)
            {
                pProcess = rgProcessSet[i];

                // Take the process lock so we can check the queue safely
                pProcess->Lock();

                // Now that we've just locked the processes, we can safely inspect it and dispatch events.
                // The process may have changed since when we first added it to the process list in Pass 1,
                // so we can't make any assumptions about whether it's sync, live, or exiting.

                // Flush the queue if necessary. Note, we only do this if we've actually received a SyncComplete message
                // from this process. If we haven't received a SyncComplete yet, then we don't attempt to drain any
                // queued events yet. They'll be drained when the SyncComplete event is actually received.
                if (pProcess->GetSyncCompleteRecv() &&
                    (pProcess->GetShim() != NULL) &&
                    !pProcess->GetSynchronized())
                {
                    if (pProcess->GetShim()->GetManagedEventQueue()->IsEmpty())
                    {
                        // Effectively what we are doing here is to continue everything without actually
                        // handling an event.  We can get here if the event raised by the LS is a duplicate
                        // creation event, which the shim discards without adding it to the event queue.
                        // See code:ShimProcess::IsDuplicateCreationEvent.
                        //
                        // To continue, we need to increment the stop count first.  Also, we can't call
                        // Continue() while holding the process lock.
                        pProcess->SetSynchronized(true);
                        pProcess->IncStopCount();
                        pProcess->Unlock();
                        pProcess->ContinueInternal(FALSE);
                        pProcess->Lock();
                    }
                    else
                    {
                        // This may toggle the process-lock
                        FlushQueuedEvents(pProcess);
                    }
                }

                // Flushing could have left the process synchronized...
                // Common case is if the callback didn't call Continue().
                if (pProcess->GetSynchronized())
                {
                    // remove the process from the wait list by moving all the other processes down one.
                    if ((i + 1) < waitCount)
                    {
                        memcpy(&rgProcessSet[i], &(rgProcessSet[i+1]), sizeof(rgProcessSet[0]) * (waitCount - i - 1));
                        memcpy(&waitSet[i], &waitSet[i+1], sizeof(waitSet[0]) * (waitCount - i - 1));
                    }

                    // drop the count of processes to wait on
                    waitCount--;

                    pProcess->Unlock();

                    // make sure to release the reference we added when the process was added to the wait list.
                    pProcess->InternalRelease();

                    // We don't have to increment i because we've copied the next element into
                    // the current value at i.
                }
                else
                {
                    // Even after flushing, its still not syncd, so leave it in the wait list.
                    pProcess->Unlock();

                    // Increment i normally.
                    i++;
                }
            }
        } // end ProcessStateChanged
    }  // while (m_run)

#ifdef _DEBUG_IMPL
    // We intentionally return while leaking some CordbProcess objects inside
    // rgProcessSet, in some cases (e.g., I've seen this happen when detaching from a
    // debuggee almost immediately after attaching to it). In the future, we should
    // really consider not leaking these anymore. However, I'm unsure how safe it is to just
    // go and InternalRelease() those guys, as above we intentionally DON'T release them when
    // they're not synchronized. So for now, to make debug builds happy, exclude those
    // references when we run CheckMemLeaks() later on. In our next side-by-side release,
    // consider actually doing InternalRelease() on the remaining CordbProcesses on
    // retail, and then we can remove the following loop.
    for (UINT i=1; i < waitCount; i++)
    {
        InterlockedDecrement(&Cordb::s_DbgMemTotalOutstandingInternalRefs);
    }
#endif //_DEBUG_IMPL
}


//
// This is the thread's real thread proc. It simply calls to the
// thread proc on the given object.
//
/*static*/
DWORD WINAPI CordbRCEventThread::ThreadProc(LPVOID parameter)
{
    CordbRCEventThread * pThread = (CordbRCEventThread *) parameter;

    INTERNAL_THREAD_ENTRY(pThread);
    pThread->ThreadProc();
    return 0;
}

template<typename T>
InterlockedStack<T>::InterlockedStack()
{
    m_pHead = NULL;
}

template<typename T>
InterlockedStack<T>::~InterlockedStack()
{
    // This is an arbitrary choice. We expect the stacks be drained.
    _ASSERTE(m_pHead == NULL);
}

// Thread safe pushes + pops.
// Many threads can push simultaneously.
// Only 1 thread can pop.
template<typename T>
void InterlockedStack<T>::Push(T * pItem)
{
    // InterlockedCompareExchangePointer(&dest, ex, comp).
    // Really behaves like:
    //     val = *dest;
    //     if (*dest == comp) { *dest = ex; }
    //     return val;
    //
    // We can do a thread-safe assign { comp = dest; dest = ex } via:
    //     do { comp = dest } while (ICExPtr(&dest, ex, comp) != comp));


    do
    {
        pItem->m_next = m_pHead;
    }
    while(InterlockedCompareExchangeT(&m_pHead, pItem, pItem->m_next) != pItem->m_next);
}

// Returns NULL on empty,
// else returns the head of the list.
template<typename T>
T * InterlockedStack<T>::Pop()
{
    if (m_pHead == NULL)
    {
        return NULL;
    }

    // This allows 1 thread to Pop() and race against N threads doing a Push().
    T * pItem = NULL;
    do
    {
        pItem = m_pHead;
    } while(InterlockedCompareExchangeT(&m_pHead, pItem->m_next, pItem) != pItem);

    return pItem;
}


// RCET will take ownership of this item and delete it.
// This can be done w/o taking any locks (thus it can be called from any lock context)
// This may race w/ the RCET draining the queue.
void CordbRCEventThread::QueueAsyncWorkItem(RCETWorkItem * pItem)
{
    // @todo -
    // Non-blocking insert into queue.

    _ASSERTE(pItem != NULL);

    m_WorkerStack.Push(pItem);

    // Ping the RCET so that it drains the queue.
    SetEvent(m_threadControlEvent);
}

// Execute & delete all workitems in the queue.
// This can be done w/o taking any locks. (though individual items may take locks).
void CordbRCEventThread::DrainWorkerQueue()
{
    _ASSERTE(IsRCEventThread());

    while(true)
    {
        RCETWorkItem* pCur = m_WorkerStack.Pop();
        if (pCur == NULL)
        {
            break;
        }

        pCur->Do();
        delete pCur;
    }
}


//---------------------------------------------------------------------------------------
// Wait for an reply from the debuggee.
//
// Arguments:
//    pProcess - process for debuggee.
//    pAppDomain - not used.
//    pEvent - caller-allocated event to be filled out.
//             This is expected to be at least as big as CorDBIPC_BUFFER_SIZE.
//
// Return Value:
//    S_OK on success. else failure.
//
// Assumptions:
//    Caller allocates
//
// Notes:
//   WaitForIPCEventFromProcess waits for an event from just the specified
//   process. This should only be called when the process is in a synchronized
//   state, which ensures that the RCEventThread isn't listening to the
//   process's event, too, which would get confusing.
//
//   @dbgtodo - this function should eventually be obsolete once everything
//   is using DAC calls instead of helper-thread.
//
//---------------------------------------------------------------------------------------
HRESULT CordbRCEventThread::WaitForIPCEventFromProcess(CordbProcess * pProcess,
                                                       CordbAppDomain * pAppDomain,
                                                       DebuggerIPCEvent * pEvent)
{
    CORDBRequireProcessStateOKAndSync(pProcess, pAppDomain);

    DWORD dwStatus;
    HRESULT hr = S_OK;

    do
    {
        dwStatus = SafeWaitForSingleObject(pProcess,
                                           pProcess->m_leftSideEventAvailable,
                                           CordbGetWaitTimeout());

        if (pProcess->m_terminated)
        {
            return CORDBG_E_PROCESS_TERMINATED;
        }
        // If we timeout because we're waiting for an uncontinued OOB event, we need to just keep waiting.
    } while ((dwStatus == WAIT_TIMEOUT) && pProcess->IsWaitingForOOBEvent());




    if (dwStatus == WAIT_OBJECT_0)
    {
        pProcess->CopyRCEventFromIPCBlock(pEvent);

        EX_TRY
        {
            pProcess->MarshalManagedEvent(pEvent);

            STRESS_LOG4(LF_CORDB, LL_INFO1000, "CRCET::SIPCE: Got %s for AD 0x%x, proc 0x%x(%d)\n",
                        IPCENames::GetName(pEvent->type),
                        VmPtrToCookie(pEvent->vmAppDomain),
                        pProcess->m_id,
                        pProcess->m_id);

        }
        EX_CATCH_HRESULT(hr)

        SetEvent(pProcess->m_leftSideEventRead);

        return hr;
    }
    else if (dwStatus == WAIT_TIMEOUT)
    {
        //
        // If we timed out, check the left side to see if it is in the
        // unrecoverable error mode. If it is, return the HR from the
        // left side that caused the error. Otherwise, return that we timed
        // out and that we don't really know why.
        //
        HRESULT realHR = ErrWrapper(CORDBG_E_TIMEOUT);

        hr = pProcess->CheckForUnrecoverableError();

        if (hr == S_OK)
        {
            CORDBSetUnrecoverableError(pProcess, realHR, 0);
            return realHR;
        }
        else
            return hr;
    }
    else
    {
        _ASSERTE(dwStatus == WAIT_FAILED);

        hr = HRESULT_FROM_GetLastError();

        CORDBSetUnrecoverableError(pProcess, hr, 0);

        return hr;
    }
}


//
// Start actually creates and starts the thread.
//
HRESULT CordbRCEventThread::Start()
{
    if (m_threadControlEvent == NULL)
    {
        return E_INVALIDARG;
    }

    m_thread = CreateThread(NULL,
                            0,
                            &CordbRCEventThread::ThreadProc,
                            (LPVOID) this,
                            0,
                            &m_threadId);

    if (m_thread == NULL)
    {
        return HRESULT_FROM_GetLastError();
    }

    return S_OK;
}


//
// Stop causes the thread to stop receiving events and exit. It
// waits for it to exit before returning.
//
HRESULT CordbRCEventThread::Stop()
{
    if (m_thread != NULL)
    {
        LOG((LF_CORDB, LL_INFO100000, "CRCET::Stop\n"));

        m_run = FALSE;

        SetEvent(m_threadControlEvent);

        DWORD ret = WaitForSingleObject(m_thread, INFINITE);

        if (ret != WAIT_OBJECT_0)
        {
            return HRESULT_FROM_GetLastError();
        }
    }

    m_cordb.Clear();

    return S_OK;
}


/* ------------------------------------------------------------------------- *
 * Win32 Event Thread class
 * ------------------------------------------------------------------------- */

enum
{
    W32ETA_NONE              = 0,
    W32ETA_CREATE_PROCESS    = 1,
    W32ETA_ATTACH_PROCESS    = 2,
    W32ETA_CONTINUE          = 3,
    W32ETA_DETACH            = 4
};



//---------------------------------------------------------------------------------------
// Constructor
//
// Arguments:
//    pCordb - Pointer to the owning cordb object for this event thread.
//    pShim - Pointer to the shim for supporting V2 debuggers on V3 architecture.
//
//---------------------------------------------------------------------------------------
CordbWin32EventThread::CordbWin32EventThread(
    Cordb * pCordb,
    ShimProcess * pShim
    ) :
    m_thread(NULL), m_threadControlEvent(NULL),
    m_actionTakenEvent(NULL), m_run(TRUE),
    m_action(W32ETA_NONE)
{
    m_cordb.Assign(pCordb);
    _ASSERTE(pCordb != NULL);

    m_pShim = pShim;

    m_pNativePipeline = NULL;
}


//
// Destructor. Cleans up all of the open handles and such.
// This expects that the thread has been stopped and has terminated
// before being called.
//
CordbWin32EventThread::~CordbWin32EventThread()
{
    if (m_thread != NULL)
        CloseHandle(m_thread);

    if (m_threadControlEvent != NULL)
        CloseHandle(m_threadControlEvent);

    if (m_actionTakenEvent != NULL)
        CloseHandle(m_actionTakenEvent);

    if (m_pNativePipeline != NULL)
    {
        m_pNativePipeline->Delete();
        m_pNativePipeline = NULL;
    }

    m_sendToWin32EventThreadMutex.Destroy();
}


//
// Init sets up all the objects that the thread will need to run.
//
HRESULT CordbWin32EventThread::Init()
{
    if (m_cordb == NULL)
        return E_INVALIDARG;

    m_sendToWin32EventThreadMutex.Init("Win32-Send lock", RSLock::cLockFlat, RSLock::LL_WIN32_SEND_LOCK);

    m_threadControlEvent = WszCreateEvent(NULL, FALSE, FALSE, NULL);
    if (m_threadControlEvent == NULL)
        return HRESULT_FROM_GetLastError();

    m_actionTakenEvent = WszCreateEvent(NULL, FALSE, FALSE, NULL);
    if (m_actionTakenEvent == NULL)
        return HRESULT_FROM_GetLastError();

    m_pNativePipeline = NewPipelineWithDebugChecks();
    if (m_pNativePipeline == NULL)
    {
        return E_OUTOFMEMORY;
    }

    return S_OK;
}

//
// Main function of the Win32 Event Thread
//
void CordbWin32EventThread::ThreadProc()
{
#if defined(RSCONTRACTS)
    DbgRSThread::GetThread()->SetThreadType(DbgRSThread::cW32ET);

    // The win32 ET conceptually holds a lock (all threads do).
    DbgRSThread::GetThread()->TakeVirtualLock(RSLock::LL_WIN32_EVENT_THREAD);
#endif

    // In V2, the debuggee decides what to do if the debugger rudely exits / detaches. (This is
    // handled by host policy). With the OS native-debuggging pipeline, the debugger by default
    // kills the debuggee if it exits. To emulate V2 behavior, we need to override that default.
    BOOL fOk = m_pNativePipeline->DebugSetProcessKillOnExit(FALSE);
    (void)fOk; //prevent "unused variable" error from GCC
    _ASSERTE(fOk);


    // Run the top-level event loop.
    Win32EventLoop();

#if defined(RSCONTRACTS)
    // The win32 ET conceptually holds a lock (all threads do).
    DbgRSThread::GetThread()->ReleaseVirtualLock(RSLock::LL_WIN32_EVENT_THREAD);
#endif
}

// Define a holder that calls code:DeleteIPCEventHelper
using DeleteIPCEventHolder = SpecializedWrapper<DebuggerIPCEvent, DeleteIPCEventHelper>;

//---------------------------------------------------------------------------------------
//
// Helper to clean up IPCEvent before deleting it.
// This must be called after an event is marshalled via code:CordbProcess::MarshalManagedEvent
//
// Arguments:
//     pManagedEvent - managed event to delete.
//
// Notes:
//     This can delete a partially marshalled event.
//
void DeleteIPCEventHelper(DebuggerIPCEvent *pManagedEvent)
{
    CONTRACTL
    {
        // This is backout code that shouldn't need to throw.
        NOTHROW;
    }
    CONTRACTL_END;
    if (pManagedEvent == NULL)
    {
        return;
    }
    switch (pManagedEvent->type & DB_IPCE_TYPE_MASK)
    {
        // so far only this event need to cleanup.
        case DB_IPCE_MDA_NOTIFICATION:
            pManagedEvent->MDANotification.szName.CleanUp();
            pManagedEvent->MDANotification.szDescription.CleanUp();
            pManagedEvent->MDANotification.szXml.CleanUp();
            break;

        case DB_IPCE_FIRST_LOG_MESSAGE:
            pManagedEvent->FirstLogMessage.szContent.CleanUp();
            break;

        default:
            break;
    }
    delete [] (BYTE *)pManagedEvent;
}

//---------------------------------------------------------------------------------------
// Handle a CLR specific notification event.
//
// Arguments:
//    pManagedEvent - non-null pointer to a managed event.
//    pLockHolder - hold to process lock that gets toggled if this dispatches an event.
//    pCallback - callback to dispatch potential managed events.
//
// Return Value:
//    Throws on error.
//
// Assumptions:
//    Target is stopped. Record was already determined to be a CLR event.
//
// Notes:
//    This is called after caller does WaitForDebugEvent.
//    Any exception this Filter does not recognize is treated as kNotClr.
//    Currently, this includes both managed-exceptions and unmanaged ones.
//    For interop-debugging, the interop logic will handle all kNotClr and triage if
//    it's really a non-CLR exception.
//
//---------------------------------------------------------------------------------------
void CordbProcess::FilterClrNotification(
    DebuggerIPCEvent * pManagedEvent,
    RSLockHolder * pLockHolder,
    ICorDebugManagedCallback * pCallback)
{
    CONTRACTL
    {
        THROWS;
        PRECONDITION(CheckPointer(pManagedEvent));
        PRECONDITION(CheckPointer(pCallback));
        PRECONDITION(ThreadHoldsProcessLock());
    }
    CONTRACTL_END;

    // There are 3 types of events from the LS:
    // 1) Replies (eg, corresponding to WaitForIPCEvent)
    //       we need to set LSEA/wait on LSER.
    // 2) Sync-Complete (kind of like a special notification)
    //       Ping the helper
    // 3) Notifications (eg, Module-load):
    //       these are dispatched immediately.
    // 4) Left-side Startup event


    // IF we're synced, then we must be getting a "Reply".
    bool fReply = this->GetSynchronized();

    LOG((LF_CORDB, LL_INFO10000, "CP::FCN - Received event %s; fReply: %d\n",
         IPCENames::GetName(pManagedEvent->type),
         fReply));

    if (fReply)
    {
        //
        _ASSERTE(m_pShim != NULL);
        //
        // Case 1: Reply
        //

        pLockHolder->Release();
        _ASSERTE(!ThreadHoldsProcessLock());

        // Save the IPC event and wake up the thread which is waiting for it from the LS.
        GetEventChannel()->SaveEventFromLeftSide(pManagedEvent);
        SetEvent(this->m_leftSideEventAvailable);

        // Some other thread called code:CordbRCEventThread::WaitForIPCEventFromProcess, and
        // that will respond here and set the event.

        DWORD dwResult = WaitForSingleObject(this->m_leftSideEventRead, CordbGetWaitTimeout());
        pLockHolder->Acquire();
        if (dwResult != WAIT_OBJECT_0)
        {
            // The wait failed.  This is probably WAIT_TIMEOUT which suggests a deadlock/assert on
            // the RCEventThread.
            CONSISTENCY_CHECK_MSGF(false, ("WaitForSingleObject failed: %d", dwResult));
            ThrowHR(CORDBG_E_TIMEOUT);
        }
    }
    else
    {
        if (pManagedEvent->type == DB_IPCE_LEFTSIDE_STARTUP)
        {
            //
            // Case 4: Left-side startup event. We'll mark that we're attached from oop.
            //

            // Now that LS is started, we should definitely be able to instantiate DAC.
            InitializeDac();

            // @dbgtodo 'attach-bit': we don't want the debugger automatically invading the process.
            GetDAC()->MarkDebuggerAttached(TRUE);
        }
        else if (pManagedEvent->type == DB_IPCE_SYNC_COMPLETE)
        {
            // Since V3 doesn't request syncs, it shouldn't get sync-complete.
            // @dbgtodo sync: this changes when V3 can explicitly request an AsyncBreak.
            _ASSERTE(m_pShim != NULL);

            //
            // Case 2: Sync Complete
            //

            HandleSyncCompleteReceived();
        }
        else
        {
            //
            // Case 3: Notification. This will dispatch the event immediately.
            //

            // Toggles the process-lock if it dispatches callbacks.
            HandleRCEvent(pManagedEvent, pLockHolder, pCallback);

        } // end Notification
    }
}



//
// If the thread has an unhandled managed exception, hijack it.
//
// Arguments:
//     dwThreadId - OS Thread id.
//
// Returns:
//     True if hijacked; false if not.
//
// Notes:
//     This is called from shim to emulate being synchronized at an unhandled
//     exception.
//     Other ICorDebug operations could calls this (eg, func-eval at 2nd chance).
BOOL CordbProcess::HijackThreadForUnhandledExceptionIfNeeded(DWORD dwThreadId)
{
    PUBLIC_API_ENTRY(this); // from Shim

    BOOL fHijacked = FALSE;
    HRESULT hr = S_OK;
    EX_TRY
    {
        RSLockHolder lockHolder(GetProcessLock());

        // OS will not execute the Unhandled Exception Filter under native debugger, so
        // we need to hijack the thread to get it to execute the UEF, which will then do
        // work for unhandled managed exceptions.
        CordbThread * pThread = TryLookupOrCreateThreadByVolatileOSId(dwThreadId);
        if (pThread != NULL)
        {
            // If the thread has a managed exception, then we should have a pThread object.

            if (pThread->HasUnhandledNativeException())
            {
                _ASSERTE(pThread->IsThreadExceptionManaged()); // should have been marked earlier

                pThread->HijackForUnhandledException();
                fHijacked = TRUE;
            }
        }
    }
    EX_CATCH_HRESULT(hr);
    SIMPLIFYING_ASSUMPTION(SUCCEEDED(hr));

    return fHijacked;
}

//---------------------------------------------------------------------------------------
// Validate the given exception record or throw.
//
// Arguments:
//    pRawRecord - non-null raw bytes of the exception
//    countBytes - number of bytes in pRawRecord buffer.
//    format - format of pRawRecord
//
// Returns:
//    A type-safe exception record from the raw buffer.
//
// Notes:
//   This is a helper for code:CordbProcess::Filter.
//   This can do consistency checks on the incoming parameters such as:
//    * verify countBytes matches the expected size for the given format.
//    * verify the format is supported.
//
//   If we let a given ICD understand multiple formats (eg, have x86 understand both Exr32 and
//    Exr64), this would be the spot to allow the conversion.
//
const EXCEPTION_RECORD * CordbProcess::ValidateExceptionRecord(
        const BYTE pRawRecord[],
        DWORD countBytes,
        CorDebugRecordFormat format)
{
    ValidateOrThrow(pRawRecord);

    //
    // Check format against expected platform.
    //

    // @dbgtodo - , cross-plat: Once we do cross-plat, these should be based off target-architecture not host's.
#if defined(HOST_64BIT)
    if (format != FORMAT_WINDOWS_EXCEPTIONRECORD64)
    {
        ThrowHR(E_INVALIDARG);
    }
#else
    if (format != FORMAT_WINDOWS_EXCEPTIONRECORD32)
    {
        ThrowHR(E_INVALIDARG);
    }
#endif

    // @dbgtodo cross-plat: once we do cross-plat, need to use correct EXCEPTION_RECORD variant.
    if (countBytes != sizeof(EXCEPTION_RECORD))
    {
        ThrowHR(E_INVALIDARG);
    }


    const EXCEPTION_RECORD * pRecord = reinterpret_cast<const EXCEPTION_RECORD *> (pRawRecord);

    return pRecord;
};

// Return value: S_OK or indication that no more room exists for enabled types
HRESULT CordbProcess::SetEnableCustomNotification(ICorDebugClass * pClass, BOOL fEnable)
{
    HRESULT hr = S_OK;
    PUBLIC_API_BEGIN(this); // takes the lock

    ValidateOrThrow(pClass);

    ((CordbClass *)pClass)->SetCustomNotifications(fEnable);

    PUBLIC_API_END(hr);
    return hr;
} // CordbProcess::SetEnableCustomNotification

//---------------------------------------------------------------------------------------
// Public implementation of ICDProcess4::Filter
//
// Arguments:
//    pRawRecord - non-null raw bytes of the exception
//    countBytes - number of bytes in pRawRecord buffer.
//    format - format of pRawRecord
//    dwFlags - flags providing auxiliary info for exception record.
//    dwThreadId - thread that exception occurred on.
//    pCallback - callback to dispatch potential managed events on.
//    pContinueStatus - Continuation status for exception. This dictates what
//         to pass to kernel32!ContinueDebugEvent().
//
// Return Value:
//    S_OK on success.
//
// Assumptions:
//    Target is stopped.
//
// Notes:
//    The exception could be anything, including:
//    - a CLR notification,
//    - a random managed exception (both from managed code or the runtime),
//    - a non-CLR exception
//
//    This is cross-platform. The {pRawRecord, countBytes, format} describe events
//    on an arbitrary target architecture. On windows, this will be an EXCEPTION_RECORD.
//
HRESULT CordbProcess::Filter(
        const BYTE pRawRecord[],
        DWORD countBytes,
        CorDebugRecordFormat format,
        DWORD dwFlags,
        DWORD dwThreadId,
        ICorDebugManagedCallback * pCallback,
        DWORD * pContinueStatus
)
{
    HRESULT hr = S_OK;
    PUBLIC_API_BEGIN(this); // takes the lock
    {
        //
        // Validate parameters
        //

        // If we don't care about the continue status, we leave it untouched.
        ValidateOrThrow(pContinueStatus);
        ValidateOrThrow(pCallback);

        const EXCEPTION_RECORD * pRecord = ValidateExceptionRecord(pRawRecord, countBytes, format);

        DWORD dwFirstChance = (dwFlags & IS_FIRST_CHANCE);

        //
        // Deal with 2nd-chance exceptions. Don't actually hijack now (that's too invasive),
        // but mark that we have the exception in case a future operation (eg, func-eval) needs to hijack.
        //
        if (!dwFirstChance)
        {
            CordbThread * pThread = TryLookupOrCreateThreadByVolatileOSId(dwThreadId);

            // If we don't have a managed-thread object, then it certainly can't have a throwable.
            // It's possible this is still an exception from the native portion of the runtime,
            // but that's ok, we'll just treat it as a native exception.
            // This could be expensive, don't want to do it often... (definitely not on every Filter).


            // OS will not execute the Unhandled Exception Filter under native debugger, so
            // we need to hijack the thread to get it to execute the UEF, which will then do
            // work for unhandled managed exceptions.
            if ((pThread != NULL) && pThread->IsThreadExceptionManaged())
            {
                // Copy exception record for future use in case we decide to hijack.
                pThread->SetUnhandledNativeException(pRecord);
            }
            // we don't care about 2nd-chance exceptions, unless we decide to hijack it later.
        }

        //
        // Deal with CLR notifications
        //
        else if (pRecord->ExceptionCode == CLRDBG_NOTIFICATION_EXCEPTION_CODE) // Special event code
        {
            //
            // This may not be for us, or we may not have a managed thread object:
            // 1. Anybody can raise an exception with this exception code, so can't assume this belongs to us yet.
            // 2. Notifications may come on unmanaged threads if they're coming from MDAs or CLR internal events
            //    fired before the thread is created.
            //
            BYTE * pManagedEventBuffer = new BYTE[CorDBIPC_BUFFER_SIZE];
            DeleteIPCEventHolder pManagedEvent(reinterpret_cast<DebuggerIPCEvent *>(pManagedEventBuffer));

            bool fOwner = CopyManagedEventFromTarget(pRecord, pManagedEvent);
            if (fOwner)
            {
                // This toggles the lock if it dispatches callbacks
                FilterClrNotification(pManagedEvent, GET_PUBLIC_LOCK_HOLDER(), pCallback);

                // Cancel any notification events from target. These are just supposed to notify ICD and not
                // actually be real exceptions in the target.
                // Canceling here also prevents a VectoredExceptionHandler in the target from picking
                // up exceptions for the CLR.
                *pContinueStatus = DBG_CONTINUE;
            }

            // holder will invoke DeleteIPCEventHelper(pManagedEvent).
        }

    }
    PUBLIC_API_END(hr);
    // we may not find the correct mscordacwks so fail gracefully
    _ASSERTE(SUCCEEDED(hr) || (hr != HRESULT_FROM_WIN32(ERROR_MOD_NOT_FOUND)));

    return hr;
}

//---------------------------------------------------------------------------------------
// Wrapper to invoke ICorDebugMutableDataTarget::ContinueStatusChanged
//
// Arguments:
//   dwContinueStatus - new continue status
//
// Returns:
//   None. Throw on error.
//
// Notes:
//   Initial continue status is returned from code:CordbProcess::Filter.
//   Some operations (mainly hijacking on a 2nd-chance exception), may need to
//   override that continue status.
//   ICorDebug operations invoke a callback on the data-target to notify the debugger
//   of a change in status. Debugger may fail the request.
//
void CordbProcess::ContinueStatusChanged(DWORD dwThreadId, CORDB_CONTINUE_STATUS dwContinueStatus)
{
    HRESULT hr = m_pMutableDataTarget->ContinueStatusChanged(dwThreadId, dwContinueStatus);
    IfFailThrow(hr);
}

//---------------------------------------------------------------------------------------
// Request a synchronization to occur after a debug event is dispatched.
//
// Note:
//    This is called in response to a managed debug event, and so we know that we have
//    a worker thread in the process (the one that just sent the event!)
//    This can not be called asynchronously.
//---------------------------------------------------------------------------------------
void CordbProcess::RequestSyncAtEvent()
{
    GetDAC()->RequestSyncAtEvent();
}

//---------------------------------------------------------------------------------------
//
// Primary loop of the Win32 debug event thread.
//
//
// Arguments:
//    None.
//
// Return Value:
//    None.
//
// Notes:
//    This is it, you've found it, the main guy.  This function loops as long as the
//    debugger is around calling the OS WaitForDebugEvent() API.  It takes the OS Debug
//    Event and filters it thru the right-side, continuing the process if not recognized.
//
// @dbgtodo shim: this will become part of the shim.
//---------------------------------------------------------------------------------------
void CordbWin32EventThread::Win32EventLoop()
{
    // This must be called from the win32 event thread.
    _ASSERTE(IsWin32EventThread());

    LOG((LF_CORDB, LL_INFO1000, "W32ET::W32EL: entered win32 event loop\n"));

    // Allow the timeout for WFDE to be adjustable. Default to 25 ms based off perf numbers (see issue VSWhidbey 132368).
    DWORD dwWFDETimeout = CLRConfig::GetConfigValue(CLRConfig::UNSUPPORTED_DbgWFDETimeout);

    while (m_run)
    {
        BOOL fEventAvailable = FALSE;

        // We should not have any locks right now.


        // Have to wait on 2 sources:
        // WaitForMultipleObjects - ping for messages (create, attach, Continue, detach) and also
        //    process exits in the managed-only case.
        // Native Debug Events - This is a huge perf hit so we want to avoid it whenever we can.
        //    Only wait on these if we're interop debugging and if the process is not frozen.
        //    A frozen process can't send any debug events, so don't bother looking for them.


        unsigned int cWaitCount = 1;

        HANDLE rghWaitSet[2];

        rghWaitSet[0] = m_threadControlEvent;

        DWORD dwWaitTimeout = INFINITE;
        DEBUG_EVENT event = {};
        if (m_pProcess != NULL)
        {
            // Process is always built on Native debugging pipeline, so it needs to always be prepared to call WFDE
            // As an optimization, if the target is stopped, then we can avoid calling WFDE.
            {
#ifndef FEATURE_INTEROP_DEBUGGING
                // Managed-only, never win32 stopped, so always check for an event.
                dwWaitTimeout = 0;
                fEventAvailable = m_pNativePipeline->WaitForDebugEvent(&event, dwWFDETimeout, m_pProcess);
#else
                // Wait for a Win32 debug event from any processes that we may be attached to as the Win32 debugger.
                const bool fIsWin32Stopped = (m_pProcess->m_state & CordbProcess::PS_WIN32_STOPPED) != 0;
                const bool fSkipWFDE = fIsWin32Stopped;


                const bool fIsInteropDebugging = m_pProcess->IsInteropDebugging();
                (void)fIsInteropDebugging; //prevent "unused variable" error from GCC

                // Assert checks
                _ASSERTE(fIsInteropDebugging == m_pShim->IsInteropDebugging());

                if (!fSkipWFDE)
                {
                    dwWaitTimeout = 0;
                    fEventAvailable = m_pNativePipeline->WaitForDebugEvent(&event, dwWFDETimeout, m_pProcess);
                }
                else
                {
                    // If we're managed-only debugging, then the process should always be running,
                    // which means we always need to be calling WFDE to pump potential debug events.
                    // If we're interop-debugging, then the process can be stopped at a native-debug event,
                    // in which case we don't have to call WFDE until we resume it again.
                    // So we can only skip the WFDE when we're interop-debugging.
                    _ASSERTE(fIsInteropDebugging);
                }
#endif // FEATURE_INTEROP_DEBUGGING
            }


        } // end m_pProcess != NULL

#if defined(FEATURE_INTEROP_DEBUGGING)
        // While interop-debugging, the process may get killed rudely underneath us, even if we haven't
        // continued the last debug event. In such cases, The process object will get signalled normally.
        // If we didn't just get a native-exitProcess event, then listen on the process handle for exit.
        // (this includes all managed-only debugging)
        // It's very important to establish this before we go into the WaitForMutlipleObjects below
        // because the debuggee may exit while we're sitting in that loop (waiting for the debugger to call Continue).
        bool fDidNotJustGetExitProcessEvent = !fEventAvailable || (event.dwDebugEventCode != EXIT_PROCESS_DEBUG_EVENT);
#else
        // In non-interop scenarios, we'll never get any native debug events, let alone an ExitProcess native event.
        bool fDidNotJustGetExitProcessEvent = true;
#endif // FEATURE_INTEROP_DEBUGGING


        // The m_pProcess handle will get nulled out after we process the ExitProcess event, and
        // that will ensure that we only wait for an Exit event once.
        if ((m_pProcess != NULL) && fDidNotJustGetExitProcessEvent)
        {
            rghWaitSet[1] = m_pProcess->UnsafeGetProcessHandle();
            cWaitCount = 2;
        }

        // See if any process that we aren't attached to as the Win32 debugger have exited. (Note: this is a
        // polling action if we are also waiting for Win32 debugger events. We're also looking at the thread
        // control event here, too, to see if we're supposed to do something, like attach.
        DWORD dwStatus = WaitForMultipleObjectsEx(cWaitCount, rghWaitSet, FALSE, dwWaitTimeout, FALSE);

        _ASSERTE((dwStatus == WAIT_TIMEOUT) || (dwStatus < cWaitCount));

        if (!m_run)
        {
            _ASSERTE(m_action == W32ETA_NONE);
            break;
        }

        LOG((LF_CORDB, LL_INFO100000, "W32ET::W32EL - got event , ret=%d, has w32 dbg event=%d\n",
             dwStatus, fEventAvailable));

        // If we haven't timed out, or if it wasn't the thread control event
        // that was set, then a process has
        // exited...
        if ((dwStatus != WAIT_TIMEOUT) && (dwStatus != WAIT_OBJECT_0))
        {
            // Grab the process that exited.
            _ASSERTE((dwStatus - WAIT_OBJECT_0) == 1);
            ExitProcess(false); // not detach
            fEventAvailable = false;
        }
        // Should we create a process?
        else if (m_action == W32ETA_CREATE_PROCESS)
        {
            CreateProcess();
        }
        // Should we attach to a process?
        else if (m_action == W32ETA_ATTACH_PROCESS)
        {
            AttachProcess();
        }
        // Should we detach from a process?
        else if (m_action == W32ETA_DETACH)
        {
            ExitProcess(true); // detach case

            // Once we detach, we don't need to continue any outstanding event.
            // So act like we never got the event.
            fEventAvailable = false;
            PREFIX_ASSUME(m_pProcess == NULL); // W32 cleared process pointer
        }

#ifdef FEATURE_INTEROP_DEBUGGING
        // Should we continue the process?
        else if (m_action == W32ETA_CONTINUE)
        {
            HandleUnmanagedContinue();
        }
#endif // FEATURE_INTEROP_DEBUGGING

        // We don't need to sweep the FCH threads since we never hijack a thread in cooperative mode.


        // Only process an event if one is available.
        if (!fEventAvailable)
        {
            continue;
        }

        // The only ref we have is the one in the ProcessList hash;
        // If we dispatch an ExitProcess event, we may even lose that.
        // But since the CordbProcess is our parent object, we know it won't go away until
        // it neuters us, so we can safely proceed.
        // Find the process this event is for.
        PREFIX_ASSUME(m_pProcess != NULL);
        _ASSERTE(m_pProcess->m_id == GetProcessId(&event)); // should only get events for our proc
        g_pRSDebuggingInfo->m_MRUprocess = m_pProcess;

        // Must flush the dac cache since we were just running.
        m_pProcess->ForceDacFlush();

        // So we've filtered out CLR events.
        // Let the shim handle the remaining events. This will call back into Filter() if appropriate.
        // This will also ensure the debug event gets continued.
        HRESULT hrShim = S_OK;
        {
            PUBLIC_CALLBACK_IN_THIS_SCOPE0_NO_LOCK(NULL);
            hrShim = m_pShim->HandleWin32DebugEvent(&event);
        }
        // Any errors from the shim (eg. failure to load DAC) are unrecoverable
        SetUnrecoverableIfFailed(m_pProcess, hrShim);

    } // loop

    LOG((LF_CORDB, LL_INFO1000, "W32ET::W32EL: exiting event loop\n"));

    return;
}

//---------------------------------------------------------------------------------------
//
// Returns if the current thread is the win32 thread.
//
// Return Value:
//    true iff this is the win32 event thread.
//
//---------------------------------------------------------------------------------------
bool CordbProcess::IsWin32EventThread()
{
    _ASSERTE((m_pShim != NULL) || !"Don't check win32 event thread in V3 cases");
    return m_pShim->IsWin32EventThread();
}

//---------------------------------------------------------------------------------------
// Call when the sync complete event is received and can be processed.
//
// Notes:
//    This is called when the RS gets the sync-complete from the LS and can process it.
//
//    This has a somewhat elaborate contract to fill between Interop-debugging, Async-Break, draining the
//    managed event-queue, and coordinating with the dispatch thread (RCET).
//
//    @dbgtodo - this should eventually get hoisted into the shim.
void CordbProcess::HandleSyncCompleteReceived()
{
    _ASSERTE(ThreadHoldsProcessLock());

    this->SetSyncCompleteRecv(true);

    // If some thread is waiting for the process to sync, notify that it can go now.
    if (this->m_stopRequested)
    {
        this->SetSynchronized(true);
        SetEvent(this->m_stopWaitEvent);
    }
    else
    {
        // Note: we set the m_stopWaitEvent all the time and leave it high while we're stopped. This
        // must be done after we've checked m_stopRequested.
        SetEvent(this->m_stopWaitEvent);

        // Otherwise, simply mark that the state of the process has changed and let the
        // managed event dispatch logic take over.
        //
        // Note: process->m_synchronized remains false, which indicates to the RC event
        // thread that it can dispatch the next managed event.
        m_cordb->ProcessStateChanged();
    }
}


#ifdef FEATURE_INTEROP_DEBUGGING

//---------------------------------------------------------------------------------------
//
// Get (create if needed) the unmanaged thread for an unmanaged debug event.
//
// Arguments:
//    event - native debug event.
//
// Return Value:
//    Unmanaged thread corresponding to the native debug event.
//
//
// Notes:
//    Thread may be newly allocated, or may be existing. CordbProcess holds
//    list of all CordbUnmanagedThreads, and will handle freeing memory.
//
//---------------------------------------------------------------------------------------
CordbUnmanagedThread * CordbProcess::GetUnmanagedThreadFromEvent(const DEBUG_EVENT * pEvent)
{
    _ASSERTE(ThreadHoldsProcessLock());
    HRESULT hr;

    CordbUnmanagedThread * pUnmanagedThread = NULL;

    // Remember newly created threads.
    if (pEvent->dwDebugEventCode == CREATE_PROCESS_DEBUG_EVENT)
    {
        // We absolutely should have an unmanaged callback by this point.
        // That means that the client debugger should have called ICorDebug::SetUnmanagedHandler by now.
        // However, we can't actually enforce that (see comment  in ICorDebug::SetUnmanagedHandler for details),
        // so we do a runtime check to check this.
        // This is an extremely gross API misuse and an issue in the client if the callback is not set yet.
        // Without the unmanaged callback, we absolutely can't do interop-debugging. We assert (checked builds) and
        // dispatch unrecoverable error (retail builds) to avoid an AV.


        if (this->m_cordb->m_unmanagedCallback == NULL)
        {
            CONSISTENCY_CHECK_MSGF((this->m_cordb->m_unmanagedCallback != NULL),
                ("GROSS API misuse!!\nNo unmanaged callback set by the time we've received CreateProcess debug event for proces 0x%x.\n",
                pEvent->dwProcessId));

            CORDBSetUnrecoverableError(this, CORDBG_E_INTEROP_NOT_SUPPORTED, 0);

            // Returning NULL will tell caller not to dispatch event to client. We have no callback object to dispatch upon.
            return NULL;
        }

        pUnmanagedThread = this->HandleUnmanagedCreateThread(pEvent->dwThreadId,
                                                             pEvent->u.CreateProcessInfo.hThread,
                                                             pEvent->u.CreateProcessInfo.lpThreadLocalBase);

        // Managed-attach won't start until after Cordbg continues from the loader-bp.
    }
    else if (pEvent->dwDebugEventCode == CREATE_THREAD_DEBUG_EVENT)
    {
        pUnmanagedThread = this->HandleUnmanagedCreateThread(pEvent->dwThreadId,
                                                             pEvent->u.CreateThread.hThread,
                                                             pEvent->u.CreateThread.lpThreadLocalBase);

        BOOL fBlockExists = FALSE;
        hr = S_OK;
        EX_TRY
        {
            // See if we have the debugger control block yet...

            this->GetEventBlock(&fBlockExists);

            // If we have the debugger control block, and if that control block has the address of the thread proc for
            // the helper thread, then we're initialized enough on the Left Side to recgonize the helper thread based on
            // its thread proc's address.
            if (this->GetDCB() != NULL)
            {
                // get the latest LS DCB information
                UpdateRightSideDCB();
                if ((this->GetDCB()->m_helperThreadStartAddr != NULL) && (pUnmanagedThread != NULL))
                {
                    void * pStartAddr = pEvent->u.CreateThread.lpStartAddress;

                    if (pStartAddr == this->GetDCB()->m_helperThreadStartAddr)
                    {
                        // Remember the ID of the helper thread.
                        this->m_helperThreadId = pEvent->dwThreadId;

                        LOG((LF_CORDB, LL_INFO1000, "W32ET::W32EL: Left Side Helper Thread is 0x%x\n", pEvent->dwThreadId));
                    }
                }
            }
        }
        EX_CATCH_HRESULT(hr)
        {
            if (fBlockExists && FAILED(hr))
            {
                _ASSERTE(IsLegalFatalError(hr));
                // Send up the DebuggerError event
                this->UnrecoverableError(hr, 0, NULL, 0);

                // Kill the process.
                // RS will pump events until we LS process exits.
                TerminateProcess(this->m_handle, hr);

                return pUnmanagedThread;
            }
        }
    }
    else
    {
        // Find the unmanaged thread that this event is for.
        pUnmanagedThread = this->GetUnmanagedThread(pEvent->dwThreadId);
    }

    return pUnmanagedThread;
}

//---------------------------------------------------------------------------------------
//
// Handle a native-debug event representing a managed sync-complete event.
//
//
// Return Value:
//    Reaction telling caller how to respond to the native-debug event.
//
// Assumptions:
//    Called within the Triage process after receiving a native-debug event.
//
//---------------------------------------------------------------------------------------
Reaction CordbProcess::TriageSyncComplete()
{
    _ASSERTE(ThreadHoldsProcessLock());

    STRESS_LOG0(LF_CORDB, LL_INFO1000, "CP::TSC: received 'sync complete' flare.\n");

    _ASSERTE(IsInteropDebugging());

    // Note: we really don't need to be suspending Runtime threads that we know have tripped
    // here. If we ever end up with a nice, quick way to know that about each unmanaged thread, then
    // we should put that to good use here.
    this->SuspendUnmanagedThreads();

    this->HandleSyncCompleteReceived();

    // Let the process run free.
    return REACTION(cIgnore);

    // At this point, all managed threads are stopped at safe places and all unmanaged
    // threads are either suspended or hijacked. All stopped managed threads are also hard
    // suspended (due to the call to SuspendUnmanagedThreads above) except for the thread
    // that sent the sync complete flare.

    // We've handled this exception, so skip all further processing.
    UNREACHABLE();
}

//-----------------------------------------------------------------------------
// Triage a breakpoint (non-flare) on a "normal" thread.
//-----------------------------------------------------------------------------
Reaction CordbProcess::TriageBreakpoint(CordbUnmanagedThread * pUnmanagedThread, const DEBUG_EVENT * pEvent)
{
    _ASSERTE(ThreadHoldsProcessLock());

    HRESULT hr = S_OK;

    DWORD dwExCode = pEvent->u.Exception.ExceptionRecord.ExceptionCode;
    const void * pExAddress = pEvent->u.Exception.ExceptionRecord.ExceptionAddress;

    _ASSERTE(dwExCode == STATUS_BREAKPOINT);

    // There are three cases here:
    //
    // 1. The breakpoint definetly belongs to the Runtime. (I.e., a BP in our patch table that
    // is in managed code.) In this case, we continue the process with
    // DBG_EXCEPTION_NOT_HANDLED, which lets the in-process exception logic kick in as if we
    // weren't here.
    //
    // 2. The breakpoint is definetly not ours. (I.e., a BP that is not in our patch table.) We
    // pass these up as regular exception events, doing the can't stop check as usual.
    //
    // 3. We're not sure. (I.e., a BP in our patch table, but set in unmangaed code.) In this
    // case, we hijack as usual, also with can't stop check as usual.

    bool fPatchFound = false;
    bool fPatchIsUnmanaged = false;

    hr = this->FindPatchByAddress(PTR_TO_CORDB_ADDRESS(pExAddress),
                                  &fPatchFound,
                                  &fPatchIsUnmanaged);

    if (SUCCEEDED(hr))
    {
        if (fPatchFound)
        {
#ifdef _DEBUG
            // What if managed & native patch the same address? That could happen on a step out M --> U.
            {
                NativePatch * pNativePatch = GetNativePatch(pExAddress);
                SIMPLIFYING_ASSUMPTION_MSGF(pNativePatch == NULL, ("Have Managed & native patch at 0x%p", pExAddress));
            }
#endif

            // BP could be ours... if its unmanaged, then we still need to hijack, so fall
            // through to that logic. Otherwise, its ours.
            if (!fPatchIsUnmanaged)
            {
                LOG((LF_CORDB, LL_INFO1000, "W32ET::W32EL: breakpoint exception "
                     "belongs to runtime due to patch table match.\n"));

                return REACTION(cCLR);
            }
            else
            {
                LOG((LF_CORDB, LL_INFO1000, "W32ET::W32EL: breakpoint exception "
                     "matched in patch table, but its unmanaged so might hijack anyway.\n"));

                // If we're in cooperative mode, then we must have a inproc handler, and don't need to hijack
                // One way this can happen is the patch placed for a func-eval complete is hit in coop-mode.
                if (pUnmanagedThread->GetEEPGCDisabled())
                {
                    LOG((LF_CORDB, LL_INFO10000, "Already in coop-mode, don't need to hijack\n"));
                    return REACTION(cCLR);
                }
                else
                {
                    return REACTION(cBreakpointRequiringHijack);
                }
            }

            UNREACHABLE();
        }
        else // Patch not found
        {
            // If we're here, then we have a BP that's not in the managed patch table, and not
            // in the native patch list. This should be rare. Perhaps an int3 / DebugBreak() / Assert in
            // the native code stream.
            // Anyway, we don't know about this patch so we can't skip it. The only thing we can do
            // is chuck it up to Cordbg and hope they can help us. Note that this is the same case
            // we were in w. V1.

            // BP doesn't belong to CLR ... so dispatch it to Cordbg as either make it IB or OOB.
            // @todo - make the runtime 1 giant Can't stop region.
            bool fCantStop = pUnmanagedThread->IsCantStop();

#ifdef _DEBUG
            // We rarely expect a raw int3 here. Add a debug check that will assert.
            // Tests that know they don't have raw int3 can enable this regkey to get
            // extra coverage.
            static DWORD s_fBreakOnRawInt3 = -1;

            if (s_fBreakOnRawInt3 == -1)
                s_fBreakOnRawInt3 = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_DbgBreakOnRawInt3);

            if (s_fBreakOnRawInt3)
            {
                CONSISTENCY_CHECK_MSGF(false, ("Unexpected Raw int3 at:%p on tid 0x%x (%d). CantStop=%d."
                    "This assert is used by specific tests to get extra checks."
                    "For normal cases it's ignorable and is enabled by setting DbgBreakOnRawInt3==1.",
                    pExAddress, pEvent->dwThreadId, pEvent->dwThreadId, fCantStop));
            }
#endif

            if (fCantStop)
            {
                // If we're in a can't stop region, then its OOB no matter what at this point.
                return REACTION(cOOB);
            }
            else
            {
                // PGC must be enabled if we're going to stop for an IB event.
                bool PGCDisabled = pUnmanagedThread->GetEEPGCDisabled();
                _ASSERTE(!PGCDisabled);

                // Bp is definitely not ours, and PGC is not disabled, so in-band exception.
                LOG((LF_CORDB, LL_INFO1000, "W32ET::W32EL: breakpoint exception "
                     "does not belong to the runtime due to failed patch table match.\n"));

                return REACTION(cInband);
            }

            UNREACHABLE();
        }

        UNREACHABLE();
    }
    else
    {
        // Patch table lookup failed? Only on OOM or if ReadProcessMemory fails...
        _ASSERTE(!"Patch table lookup failed!");
        CORDBSetUnrecoverableError(this, hr, 0);
        return REACTION(cOOB);
    }

    UNREACHABLE();
}

//---------------------------------------------------------------------------------------
//
// Triage a "normal" 1st chance exception on a "normal" thread.
// Not hijacked, not the helper thread, not a flare, etc.. This is the common
// case for a native exception from native code.
//
// Arguments:
//     pUnmanagedThread - Pointer to the CordbUnmanagedThread object that we want to hijack.
//     pEvent - Pointer to the debug event which contains the exception code and address.
//
// Return Value:
//     The Reaction tells if the event is in-band, out-of-band, CLR specific or ignorable.
//
//---------------------------------------------------------------------------------------
Reaction CordbProcess::Triage1stChanceNonSpecial(CordbUnmanagedThread * pUnmanagedThread, const DEBUG_EVENT * pEvent)
{
    _ASSERTE(ThreadHoldsProcessLock());
    CONTRACTL
    {
        THROWS;
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;

    DWORD dwExCode = pEvent->u.Exception.ExceptionRecord.ExceptionCode;
    const void * pExAddress = pEvent->u.Exception.ExceptionRecord.ExceptionAddress;

    // This had better not be a flare. If it is, that means we have some race that unmarked
    // the hijacks.
    _ASSERTE(!ExceptionIsFlare(dwExCode, pExAddress));

    // Any first chance exception could belong to the Runtime, so long as the Runtime has actually been
    // initialized. Here we'll setup a first-chance hijack for this thread so that it can give us the
    // true answer that we need.

    // But none of those exceptions could possibly be ours unless we have a managed thread to go with
    // this unmanaged thread. A non-NULL EEThreadPtr tells us that there is indeed a managed thread for
    // this unmanaged thread, even if the Right Side hasn't received a managed ThreadCreate message yet.
    REMOTE_PTR pEEThread;
    hr = pUnmanagedThread->GetEEThreadPtr(&pEEThread);
    _ASSERTE(SUCCEEDED(hr));

    if (pEEThread == NULL)
    {
        // No managed thread, so it can't possibly belong to the runtime!
        // But it may still be in a can't-stop region (think some goofy shutdown case).
        if (pUnmanagedThread->IsCantStop())
        {
            return REACTION(cOOB);
        }
        else
        {
            return REACTION(cInband);
        }
    }



    // We have to be careful here. A Runtime thread may be in a place where we cannot let an
    // unmanaged exception stop it. For instance, an unmanaged user breakpoint set on
    // WaitForSingleObject will prevent Runtime threads from sending events to the Right Side. So at
    // various points below, we check to see if this Runtime thread is in a place were we can't let
    // it stop, and if so then we jump over to the out-of-band dispatch logic and treat this
    // exception as out-of-band. The debugger is supposed to continue from the out-of-band event
    // properly and help us avoid this problem altogether.

    // Grab a few flags from the thread's state...
    bool fThreadStepping = false;
    bool fSpecialManagedException = false;

    pUnmanagedThread->GetEEState(&fThreadStepping, &fSpecialManagedException);

    // If we've got a single step exception, and if the Left Side has indicated that it was
    // stepping the thread, then the exception is ours.
    if (dwExCode == STATUS_SINGLE_STEP)
    {
        if (fThreadStepping)
        {
            // Yup, its the Left Side that was stepping the thread...
            STRESS_LOG0(LF_CORDB, LL_INFO1000, "W32ET::W32EL: single step exception belongs to the runtime.\n");

            return REACTION(cCLR);
        }

        // Any single step that is triggered when the thread's state doesn't indicate that
        // we were stepping the thread automatically gets passed out as an unmanged event.
        STRESS_LOG0(LF_CORDB, LL_INFO1000, "W32ET::W32EL: single step exception "
             "does not belong to the runtime.\n");

        if (pUnmanagedThread->IsCantStop())
        {
            return REACTION(cOOB);
        }
        else
        {
            return REACTION(cInband);
        }

        UNREACHABLE();
    }

#ifdef CorDB_Short_Circuit_First_Chance_Ownership
    // If the runtime indicates that this is a special exception being thrown within the runtime,
    // then its ours no matter what.
    else if (fSpecialManagedException)
    {
        STRESS_LOG0(LF_CORDB, LL_INFO1000, "W32ET::W32EL: exception belongs to the runtime due to "
             "special managed exception marking.\n");

        return REACTION(cCLR);
    }
    else if ((dwExCode == EXCEPTION_COMPLUS) || (dwExCode == EXCEPTION_HIJACK))
    {
        STRESS_LOG0(LF_CORDB, LL_INFO1000,
             "W32ET::W32EL: exception belongs to Runtime due to match on built in exception code\n");

        return REACTION(cCLR);
    }
    else if (dwExCode == EXCEPTION_MSVC)
    {
        // The runtime may use C++ exceptions internally. We can still report these
        // to the debugger as long as we're outside of a can't-stop region.
        if (pUnmanagedThread->IsCantStop())
        {
            return REACTION(cCLR);
        }
        else
        {
            return REACTION(cInband);
        }
    }
    else if (dwExCode == STATUS_BREAKPOINT)
    {
        return TriageBreakpoint(pUnmanagedThread, pEvent);
    }// end BP case
#endif

    // It's not a breakpoint or single-step. Now it just comes down to the address from where
    // the exception is coming from. If it's managed, we give it back to the CLR. If it's
    // from native, then we dispatch to Cordbg.
    // We can use DAC to figure this out from Out-of-process.
    _ASSERTE(dwExCode != STATUS_BREAKPOINT); // BP were already handled.


    // Use DAC to decide if it's ours or not w/o going inproc.
    CORDB_ADDRESS address = PTR_TO_CORDB_ADDRESS(pExAddress);

    IDacDbiInterface::AddressType addrType;

    addrType = GetDAC()->GetAddressType(address);
    bool fIsCorCode =((addrType == IDacDbiInterface::kAddressManagedMethod) ||
                      (addrType == IDacDbiInterface::kAddressRuntimeManagedCode) ||
                      (addrType == IDacDbiInterface::kAddressRuntimeUnmanagedCode));

    STRESS_LOG2(LF_CORDB, LL_INFO1000, "W32ET::W32EL: IsCorCode(0x%I64p)=%d\n", address, fIsCorCode);


    if (fIsCorCode)
    {
        return REACTION(cCLR);
    }
    else
    {
        if (pUnmanagedThread->IsCantStop())
        {
            return REACTION(cOOB);
        }
        else
        {
            return REACTION(cInband);
        }
    }

    UNREACHABLE();
}

//---------------------------------------------------------------------------------------
//
// Triage a 1st-chance exception when the CLR is initialized.
//
// Arguments:
//    pUnmanagedThread - thread that the event has occurred on.
//    pEvent - native debug event for the exception that occurred that this is triaging.
//
// Return Value:
//    Reaction for how to handle this event.
//
// Assumptions:
//    Called when receiving a debug event when the process is stopped.
//
// Notes:
//    A 1st-chance event has a wide spectrum of possibility including:
//    - It may be unmanaged or managed.
//    - Or it may be an execution control exception for managed-exceution
//    - thread skipping an OOB event.
//
//---------------------------------------------------------------------------------------
Reaction CordbProcess::TriageExcep1stChanceAndInit(CordbUnmanagedThread * pUnmanagedThread,
                                                   const DEBUG_EVENT * pEvent)
{
    _ASSERTE(ThreadHoldsProcessLock());
    _ASSERTE(m_runtimeOffsetsInitialized);

    NativePatch * pNativePatch = NULL;
    DebuggerIPCRuntimeOffsets * pIPCRuntimeOffsets = &(this->m_runtimeOffsets);

    DWORD dwExCode = pEvent->u.Exception.ExceptionRecord.ExceptionCode;
    const void * pExAddress = pEvent->u.Exception.ExceptionRecord.ExceptionAddress;

    LOG((LF_CORDB, LL_INFO1000, "CP::TE1stCAI: Enter\n"));

#ifdef _DEBUG
    // Some Interop bugs involve threads that land at a bad IP. Since we're interop-debugging, we can't
    // attach a debugger to the LS. So we have some debug mode where we enable the SS flag and thus
    // produce a trace of where a thread is going.
    if (pUnmanagedThread->IsDEBUGTrace() && (dwExCode == STATUS_SINGLE_STEP))
    {
        pUnmanagedThread->ClearState(CUTS_DEBUG_SingleStep);
        LOG((LF_CORDB, LL_INFO10000, "DEBUG TRACE, thread %4x at IP: 0x%p\n", pUnmanagedThread->m_id, pExAddress));

        // Clear the exception and pretend this never happened.
        return REACTION(cIgnore);
    }
#endif

    // If we were stepping for exception retrigger and got the single step and it should be hidden then just ignore it.
    // Anything that isn't cInbandExceptionRetrigger will cause the debug event to be dequeued, stepping turned off, and
    // it will count as not retriggering
    // TODO: I don't think the IsSSFlagNeeded() check is needed here though it doesn't break anything
    if (pUnmanagedThread->IsSSFlagNeeded() && pUnmanagedThread->IsSSFlagHidden() && (dwExCode == STATUS_SINGLE_STEP))
    {
        LOG((LF_CORDB, LL_INFO10000, "CP::TE1stCAI: ignoring hidden single step\n"));
        return REACTION(cIgnore);
    }

    // Is this a breakpoint indicating that the Left Side is now synchronized?
    if ((dwExCode == STATUS_BREAKPOINT) &&
        (pExAddress == pIPCRuntimeOffsets->m_notifyRSOfSyncCompleteBPAddr))
    {
        return TriageSyncComplete();
    }
    else if ((dwExCode == STATUS_BREAKPOINT) &&
             (pExAddress == pIPCRuntimeOffsets->m_excepForRuntimeHandoffCompleteBPAddr))
    {
        _ASSERTE(!"This should be unused now");

        // This notification means that a thread that had been first-chance hijacked is now
        // finally leaving the hijack.
        STRESS_LOG0(LF_CORDB, LL_INFO1000, "CP::TE1stCAI: received 'first chance hijack handoff complete' flare.\n");

        // Let the process run.
        return REACTION(cIgnore);
    }
    else if ((dwExCode == STATUS_BREAKPOINT) &&
             (pExAddress == pIPCRuntimeOffsets->m_signalHijackCompleteBPAddr))
    {
        STRESS_LOG0(LF_CORDB, LL_INFO1000, "CP::TE1stCAI: received 'hijack complete' flare.\n");
        return REACTION(cInbandHijackComplete);
    }
    else if ((dwExCode == STATUS_BREAKPOINT) &&
             (pExAddress == m_runtimeOffsets.m_signalHijackStartedBPAddr))
    {
        STRESS_LOG0(LF_CORDB, LL_INFO1000, "CP::TE1stCAI: received 'hijack started' flare.\n");
        return REACTION(cFirstChanceHijackStarted);
    }
    else if ((dwExCode == STATUS_BREAKPOINT) && ((pNativePatch = GetNativePatch(pExAddress)) != NULL) )
    {
        // We hit a native BP placed by Cordbg.  This could happen on any thread (including helper)
        bool fCantStop = pUnmanagedThread->IsCantStop();

        // REVISIT_TODO: if the user also set a breakpoint here then we should dispatch to the debugger
        // and rely on the debugger to get us past this. Should be a rare case though.
        if (fCantStop)
        {
            // Need to skip it completely; never dispatch.
            pUnmanagedThread->SetupForSkipBreakpoint(pNativePatch);

            // Debuggee will single step over the patch, and fire a SS exception.
            // We'll then call FixupForSkipBreakpoint, and continue the process.
            return REACTION(cIgnore);
        }
        else
        {
            // Native patch in native code. A very common scenario.
            // Dispatch as an IB event to Cordbg.
            STRESS_LOG1(LF_CORDB, LL_INFO10000, "Native patch in native code (at %p), dispatching as IB event.\n", pExAddress);
            return REACTION(cInband);
        }

        UNREACHABLE();
    }

    else if ((dwExCode == STATUS_BREAKPOINT) && !IsBreakOpcodeAtAddress(pExAddress))
    {
        // If we got an int3 exception, but there's not actually an int3 at the address, then just reset the IP
        // to the address. This can happen if the int 3 is cleared after the thread has dispatched it (in which case
        // WFDE will pick it up) but before we realize it's one of ours.
        STRESS_LOG2(LF_CORDB, LL_INFO1000, "CP::TE1stCAI: Phantom Int3: Tid=0x%x, addr=%p\n", pEvent->dwThreadId, pExAddress);

        DT_CONTEXT context;

        context.ContextFlags = DT_CONTEXT_FULL;

        BOOL fSuccess = DbiGetThreadContext(pUnmanagedThread->m_handle, &context);

        _ASSERTE(fSuccess);

        if (fSuccess)
        {
            // Backup IP to point to the instruction we need to execute. Continuing from a breakpoint exception
            // continues execution at the instruction after the breakpoint, but we need to continue where the
            // breakpoint was.
            CORDbgSetIP(&context, (LPVOID) pExAddress);

            fSuccess = DbiSetThreadContext(pUnmanagedThread->m_handle, &context);
            _ASSERTE(fSuccess);
        }

        return REACTION(cIgnore);
    }
    else if (pUnmanagedThread->IsSkippingNativePatch())
    {
        // If we Single-Step over an exception, then the OS never gives us the single-step event.
        // Thus if we're skipping a native patch, we don't care what exception event we got.
        LOG((LF_CORDB, LL_INFO100000, "Done skipping native patch. Ex=0x%x\n, IsSS=%d",
             dwExCode,
             (dwExCode == STATUS_SINGLE_STEP)));

        // This is the 2nd half of skipping a native patch.
        // This could happen on any thread (including helper)
        // We've already removed the opcode and now we just finished a single-step over it.
        // So put the patch back in, and continue the process.
        pUnmanagedThread->FixupForSkipBreakpoint();

        return REACTION(cIgnore);
    }
    else if (this->IsHelperThreadWorked(pUnmanagedThread->GetOSTid()))
    {
        // We should never ever get a single-step event from the helper thread.
        CONSISTENCY_CHECK_MSGF(dwExCode != STATUS_SINGLE_STEP, (
                "Single-Step exception on helper thread (tid=0x%x/%d) in debuggee process (pid=0x%x/%d).\n"
                "For more information, attach a debuggee non-invasively to the LS to get the callstack.\n",
                pUnmanagedThread->m_id,
                pUnmanagedThread->m_id,
                this->m_id,
                this->m_id));

        // We ignore any first chance exceptions from the helper thread. There are lots of places
        // on the left side where we attempt to dereference bad object refs and such that will be
        // handled by exception handlers already in place.
        //
        // Note: we check this after checking for the sync complete notification, since that can
        // come from the helper thread.
        //
        // Note: we do let single step and breakpoint exceptions go through to the debugger for processing.
        if ((dwExCode != STATUS_BREAKPOINT) && (dwExCode != STATUS_SINGLE_STEP))
        {
            return REACTION(cCLR);
        }
        else
        {
            // Since the helper thread is part of the "can't stop" region, we should have already
            // skipped any BPs on it.
            // However, any Assert on the helper thread will hit this case.
            CONSISTENCY_CHECK_MSGF((dwExCode != STATUS_BREAKPOINT), (
                "Assert on helper thread (tid=0x%x/%d) in debuggee process (pid=0x%x/%d).\n"
                "For more information, attach a debuggee non-invasively to the LS to get the callstack.\n",
                pUnmanagedThread->m_id,
                pUnmanagedThread->m_id,
                this->m_id,
                this->m_id));

            // These breakpoint and single step exceptions have to be dispatched to the debugger as
            // out-of-band events. This tells the debugger that they must continue from these events
            // immediately, and that no interaction with the Left Side is allowed until they do so. This
            // makes sense, since these events are on the helper thread.
            return REACTION(cOOB);
        }
        UNREACHABLE();
    }
    else if (pUnmanagedThread->IsFirstChanceHijacked() && this->ExceptionIsFlare(dwExCode, pExAddress))
    {
        _ASSERTE(!"This should be unused now");
    }
    else if (pUnmanagedThread->IsGenericHijacked())
    {
        if (this->ExceptionIsFlare(dwExCode, pExAddress))
        {
            STRESS_LOG0(LF_CORDB, LL_INFO1000, "CP::TE1stCAI: fixing up from generic hijack.\n");

            _ASSERTE(dwExCode == STATUS_BREAKPOINT);

            // Fixup the thread from the generic hijack.
            pUnmanagedThread->FixupFromGenericHijack();

            // We force continue from this flare, since its only purpose was to notify us that we had to
            // fixup the thread from a generic hijack.
            return REACTION(cIgnore);
        }
        else
        {
            // We might reach here due to the stack overflow issue, due to target
            // memory corruption, or even due to an exception thrown during hijacking

            BOOL bStackOverflow = FALSE;

            if (dwExCode == STATUS_ACCESS_VIOLATION || dwExCode == STATUS_STACK_OVERFLOW)
            {
                CORDB_ADDRESS stackLimit;
                CORDB_ADDRESS stackBase;
                if (pUnmanagedThread->GetStackRange(&stackBase, &stackLimit))
                {
                    TADDR addr = pEvent->u.Exception.ExceptionRecord.ExceptionInformation[1];
                    if (stackLimit <= addr && addr < stackBase)
                        bStackOverflow = TRUE;
                }
                else
                {
                    // to limit the impact of the change we'll consider failure to retrieve the stack
                    // bounds as stack overflow as well
                    bStackOverflow = TRUE;
                }
            }

            if (!bStackOverflow)
            {
                // generic hijack means we're in CantStop, so return cOOB
                return REACTION(cOOB);
            }

            // If generichijacked and its not a flare, and the address referenced is on the stack then we've
            // got our special stack overflow case. Take off generic hijacked, mark that the helper thread
            // is dead, throw this event on the floor, and pop anyone in SendIPCEvent out of their wait.
            pUnmanagedThread->ClearState(CUTS_GenericHijacked);

            this->m_helperThreadDead = true;

            // This only works on Windows, not on Mac.  We don't support interop-debugging on Mac anyway.
            SetEvent(m_pEventChannel->GetRightSideEventAckHandle());

            // Note: we remember that this was a second chance event from one of the special stack overflow
            // cases with CUES_ExceptionUnclearable. This tells us to force the process to terminate when we
            // continue from the event. Since for some odd reason the OS decides to re-raise this exception
            // (first chance then second chance) infinitely.

            _ASSERTE(pUnmanagedThread->HasIBEvent());

            pUnmanagedThread->IBEvent()->SetState(CUES_ExceptionUnclearable);

            //newEvent = false;
            return REACTION(cInband_NotNewEvent);
        }
    }
    else
    {
	    LOG((LF_CORDB, LL_INFO1000, "CP::TE1stCAI: Triage1stChanceNonSpecial\n"));

        Reaction r(REACTION(cOOB));
        HRESULT hrCheck = S_OK;;
        EX_TRY
        {
            r = Triage1stChanceNonSpecial(pUnmanagedThread, pEvent);
        }
        EX_CATCH_HRESULT(hrCheck);
        SIMPLIFYING_ASSUMPTION(SUCCEEDED(hrCheck));
        SetUnrecoverableIfFailed(this, hrCheck);

        return r;

    }

    // At this point, any first-chance exceptions that could be special have been handled. Any
    // first-chance exception that we're still processing at this point is destined to be
    // dispatched as an unmanaged event.
    UNREACHABLE();
}


//---------------------------------------------------------------------------------------
//
// Triage a 2nd-chance exception when the CLR is initialized.
//
// Arguments:
//    pUnmanagedThread - thread that the event has occurred on.
//    pEvent - native debug event for the exception that occurred that this is triaging.
//
// Return Value:
//    Reaction for how to handle this event.
//
// Assumptions:
//    Called when receiving a debug event when the process is stopped.
//
// Notes:
//    We already hijacked 2nd-chance managed exceptions, so this is just handling
//    some V2 Interop corner cases.
//    @dbgtodo interop: this should eventually completely go away with the V3 design.
//
//---------------------------------------------------------------------------------------
Reaction CordbProcess::TriageExcep2ndChanceAndInit(CordbUnmanagedThread * pUnmanagedThread, const DEBUG_EVENT * pEvent)
{
    _ASSERTE(ThreadHoldsProcessLock());

    DWORD dwExCode = pEvent->u.Exception.ExceptionRecord.ExceptionCode;

#ifdef _DEBUG
    // For debugging, add an extra knob that let us break on any 2nd chance exceptions.
    // Most tests don't throw 2nd-chance, so we could have this enabled most of the time and
    // catch bogus 2nd chance exceptions
    static DWORD dwNo2ndChance = -1;

    if (dwNo2ndChance == -1)
    {
        dwNo2ndChance = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_DbgNo2ndChance);
    }

    if (dwNo2ndChance)
    {
        CONSISTENCY_CHECK_MSGF(false, ("2nd chance exception occurred on LS thread=0x%x, code=0x%08x, address=0x%p\n"
            "This assert is firing b/c you explicitly requested it by having the 'DbgNo2ndChance' knob enabled.\n"
            "Disable it to avoid asserts on 2nd chance.",
            pUnmanagedThread->m_id,
            dwExCode,
            pEvent->u.Exception.ExceptionRecord.ExceptionAddress));
    }
#endif


    // Second chance exception, Runtime initialized. It could belong to the Runtime, so we'll check. If it
    // does, then we'll hijack the thread. Otherwise, well just fall through and let it get
    // dispatched. Note: we do this so that the CLR's unhandled exception logic gets a chance to run even
    // though we've got a win32 debugger attached. But the unhandled exception logic never touches
    // breakpoint or single step exceptions, so we ignore those here, too.

    // There are strange cases with stack overflow exceptions. If a nieve application catches a stack
    // overflow exception and handles it, without resetting the guard page, then the app will get an AV when
    // it overflows the stack a second time. We will get the first chance AV, but when we continue from it the
    // OS won't run any SEH handlers, so our FCH won't actually work. Instead, we'll get the AV back on
    // second chance right away, and we'll end up right here.
    if (this->IsSpecialStackOverflowCase(pUnmanagedThread, pEvent))
    {
        // IsSpecialStackOverflowCase will queue the event for us, so its no longer a "new event". Setting
        // newEvent = false here basically prevents us from playing with the event anymore and we fall down
        // to the dispatch logic below, which will get our already queued first chance AV dispatched for
        // this thread.
        //newEvent = false;
        return REACTION(cInband_NotNewEvent);
    }
    else if (this->IsHelperThreadWorked(pUnmanagedThread->GetOSTid()))
    {
        // A second chance exception from the helper thread. This is pretty bad... we just force continue
        // from them and hope for the best.
        return REACTION(cCLR);
    }

    if(pUnmanagedThread->IsCantStop())
    {
        return REACTION(cOOB);
    }
    else
    {
        return REACTION(cInband);
    }
}


//---------------------------------------------------------------------------------------
//
// Triage a win32 Debug event to get a reaction
//
// Arguments:
//    pUnmanagedThread - thread that the event has occurred on.
//    pEvent - native debug event for the exception that occurred that this is triaging.
//
// Return Value:
//    Reaction for how to handle this event.
//
// Assumptions:
//    Called when receiving a debug event when the process is stopped.
//
// Notes:
//    This is the main triage routine for Win32 debug events, this delegates to the
//    1st and 2nd chance routines above appropriately.
//
//---------------------------------------------------------------------------------------
Reaction CordbProcess::TriageWin32DebugEvent(CordbUnmanagedThread * pUnmanagedThread, const DEBUG_EVENT * pEvent)
{
    _ASSERTE(ThreadHoldsProcessLock());

    // Lots of special cases for exception events. The vast majority of hybrid debugging work that takes
    // place is in response to exception events. The work below will consider certain exception events
    // special cases and rather than letting them be queued and dispatched, they will be handled right
    // here.
    if (pEvent->dwDebugEventCode == EXCEPTION_DEBUG_EVENT)
    {
        STRESS_LOG4(LF_CORDB, LL_INFO1000, "CP::TW32DE: unmanaged exception on "
             "tid 0x%x, code 0x%08x, addr 0x%08x, chance %d\n",
             pEvent->dwThreadId,
             pEvent->u.Exception.ExceptionRecord.ExceptionCode,
             pEvent->u.Exception.ExceptionRecord.ExceptionAddress,
             2-pEvent->u.Exception.dwFirstChance);

#ifdef LOGGING
        if (pEvent->u.Exception.ExceptionRecord.ExceptionCode == STATUS_ACCESS_VIOLATION)
        {
            LOG((LF_CORDB, LL_INFO1000, "\t<%s> address 0x%08x\n",
                 pEvent->u.Exception.ExceptionRecord.ExceptionInformation[0] ? "write to" : "read from",
                 pEvent->u.Exception.ExceptionRecord.ExceptionInformation[1]));
        }
#endif

        // Mark the loader bp for kicks. We won't start managed attach until native attach is finished.
        if (!this->m_loaderBPReceived)
        {
            // If its a first chance breakpoint, and its the first one, then its the loader breakpoint.
            if (pEvent->u.Exception.dwFirstChance &&
                (pEvent->u.Exception.ExceptionRecord.ExceptionCode == STATUS_BREAKPOINT))
            {
                LOG((LF_CORDB, LL_INFO1000, "CP::TW32DE: loader breakpoint received.\n"));

                // Remember that we've received the loader BP event.
                this->m_loaderBPReceived = true;

                // We never hijack the loader BP anymore (CLR 2.0+).
                // This is b/c w/ interop-attach, we don't start the managed-attach until _after_ Cordbg
                // continues from the loader-bp.
            }
        } // end of loader bp.

        // This event might be the retriggering of an event we already saw but previously had to hijack
        if(pUnmanagedThread->HasIBEvent())
        {
            const EXCEPTION_RECORD* pRecord1 = &(pEvent->u.Exception.ExceptionRecord);
            const EXCEPTION_RECORD* pRecord2 = &(pUnmanagedThread->IBEvent()->m_currentDebugEvent.u.Exception.ExceptionRecord);
            if(pRecord1->ExceptionCode == pRecord2->ExceptionCode &&
                pRecord1->ExceptionFlags == pRecord2->ExceptionFlags &&
                pRecord1->ExceptionAddress == pRecord2->ExceptionAddress)
            {
                STRESS_LOG0(LF_CORDB, LL_INFO1000, "CP::TW32DE: event is continuation of previously hijacked event.\n");
                // if we continued from the hijack then we should have already dispatched this event
                _ASSERTE(pUnmanagedThread->IBEvent()->IsDispatched());
                return REACTION(cInbandExceptionRetrigger);
            }
        }

        // We only care about exception events if they are first chance events and if the Runtime is
        // initialized within the process. Otherwise, we don't do anything special with them.
        if (pEvent->u.Exception.dwFirstChance && this->m_initialized)
        {
            return TriageExcep1stChanceAndInit(pUnmanagedThread, pEvent);
        }
        else if (!pEvent->u.Exception.dwFirstChance && this->m_initialized)
        {
            return TriageExcep2ndChanceAndInit(pUnmanagedThread, pEvent);
        }
        else
        {
            // An exception event, but the Runtime hasn't been initialize. I.e., its an exception event
            // that we will never try to hijack.
            return REACTION(cInband);
        }

        UNREACHABLE();
    }
    else
    // OOB
    {
        return REACTION(cOOB);
    }

}

//---------------------------------------------------------------------------------------
//
// Top-level handler for a win32 debug event during Interop-debugging.
//
// Arguments:
//    event - native debug event to handle.
//
// Assumptions:
//    The process just got a native debug event via WaitForDebugEvent
//
// Notes:
//    The function will Triage the exception and then handle it based on the
//    appropriate reaction (see: code:Reaction).
//
// @dbgtodo interop: this should all go into the shim.
//---------------------------------------------------------------------------------------
void CordbProcess::HandleDebugEventForInteropDebugging(const DEBUG_EVENT * pEvent)
{
    PUBLIC_API_ENTRY_FOR_SHIM(this);
    _ASSERTE(IsInteropDebugging() || !"Only do this in real interop handling path");


    STRESS_LOG3(LF_CORDB, LL_INFO1000, "W32ET::W32EL: got unmanaged event %d on thread 0x%x, proc 0x%x\n",
         pEvent->dwDebugEventCode, pEvent->dwThreadId, pEvent->dwProcessId);

    // Get the Lock.
    _ASSERTE(!this->ThreadHoldsProcessLock());

    RSSmartPtr<CordbProcess> pRef(this); // make sure we're alive...

    RSLockHolder processLockHolder(&this->m_processMutex);

    // If we get a new Win32 Debug event, then we need to flush any cached oop data structures.
    // This includes refreshing DAC and our patch table.
    ForceDacFlush();
    ClearPatchTable();

#ifdef _DEBUG
    // We want to detect if we've deadlocked. Unfortunately, w/ interop debugging, there can be a lot of
    // deadtime since we need to wait for a debug event. Thus the CPU usage may appear to be at 0%, but
    // we're not deadlocked b/c we're still receiving debug events.
    // So ping every X debug events.
    static int s_cCount = 0;
    static int s_iPingLevel = -1;
    if (s_iPingLevel == -1)
    {
        s_iPingLevel = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_DbgPingInterop);
    }
    if (s_iPingLevel != 0)
    {
        s_cCount++;
        if (s_cCount >= s_iPingLevel)
        {
            s_cCount = 0;
            ::Beep(1000,100);

            // Refresh so we can adjust ping level midstream.
            s_iPingLevel = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_DbgPingInterop);
        }
    }
#endif

    bool fNewEvent = true;

    // Mark the process as stopped.
    this->m_state |= CordbProcess::PS_WIN32_STOPPED;

    CordbUnmanagedThread * pUnmanagedThread = GetUnmanagedThreadFromEvent(pEvent);

    // In retail, if there is no unmanaged thread then we just continue and loop back around. UnrecoverableError has
    // already been set in this case. Note: there is an issue in the Win32 debugging API that can cause duplicate
    // ExitThread events. We therefore must handle not finding an unmanaged thread gracefully.

    _ASSERTE((pUnmanagedThread != NULL) || (pEvent->dwDebugEventCode == EXIT_THREAD_DEBUG_EVENT));

    if (pUnmanagedThread == NULL)
    {
        // Note: we use ContinueDebugEvent directly here since our continue is very simple and all of our other
        // continue mechanisms rely on having an UnmanagedThread object to play with ;)
        STRESS_LOG2(LF_CORDB, LL_INFO1000, "W32ET::W32EL: Continuing without thread on tid 0x%x, code=0x%x\n",
                    pEvent->dwThreadId,
                    pEvent->dwDebugEventCode);

        this->m_state &= ~CordbProcess::PS_WIN32_STOPPED;

        BOOL fOk = ContinueDebugEvent(pEvent->dwProcessId, pEvent->dwThreadId, DBG_EXCEPTION_NOT_HANDLED);

        _ASSERTE(fOk || !"ContinueDebugEvent failed when he have no thread. Debuggee is likely hung");

        return;
    }

    // There's an innate race such that we can get a Debug Event even after we've suspended a thread.
    // This can happen if the thread has already dispatched the debug event but we haven't called WFDE to pick it up
    // yet. This is sufficiently goofy that we want to stress log it.
    if (pUnmanagedThread->IsSuspended())
    {
        STRESS_LOG1(LF_CORDB, LL_INFO1000, "W32ET::W32EL: Thread 0x%x is suspended\n", pEvent->dwThreadId);
    }

    // For debugging races in retail, we'll keep a rolling queue of win32 debug events.
    this->DebugRecordWin32Event(pEvent, pUnmanagedThread);


    // Check to see if shutdown of the in-proc debugging services has begun. If it has, then we know we'll no longer
    // be running any managed code, and we know that we can stop hijacking threads. We remember this by setting
    // m_initialized to false, thus preventing most things from happening elsewhere.
    // Don't even bother checking the DCB fields until it's been verified (m_initialized == true)
    if (this->m_initialized && (this->GetDCB() != NULL))
    {
        UpdateRightSideDCB();
        if (this->GetDCB()->m_shutdownBegun)
        {
            STRESS_LOG0(LF_CORDB, LL_INFO1000, "W32ET::W32EL: shutdown begun...\n");
            this->m_initialized = false;
        }
    }

#ifdef _DEBUG
    //Verify that GetThreadContext agrees with the exception address
    if (pEvent->dwDebugEventCode == EXCEPTION_DEBUG_EVENT)
    {
        DT_CONTEXT tempDebugContext;
        tempDebugContext.ContextFlags = DT_CONTEXT_FULL;
        DbiGetThreadContext(pUnmanagedThread->m_handle, &tempDebugContext);
        CordbUnmanagedThread::LogContext(&tempDebugContext);
#if defined(TARGET_X86) || defined(TARGET_AMD64)
        const ULONG_PTR breakpointOpcodeSize = 1;
#elif defined(TARGET_ARM64)
        const ULONG_PTR breakpointOpcodeSize = 4;
#else
        const ULONG_PTR breakpointOpcodeSize = 1;
        PORTABILITY_ASSERT("NYI: Breakpoint size offset for this platform");
#endif
        _ASSERTE(CORDbgGetIP(&tempDebugContext) == pEvent->u.Exception.ExceptionRecord.ExceptionAddress ||
            (DWORD)(size_t)CORDbgGetIP(&tempDebugContext) == ((DWORD)(size_t)pEvent->u.Exception.ExceptionRecord.ExceptionAddress)+breakpointOpcodeSize);
    }
#endif

    // This call will decide what to do w/ the win32 event we just got. It does a lot of work.
    Reaction reaction = TriageWin32DebugEvent(pUnmanagedThread, pEvent);


    // Stress-log the reaction.
#ifdef _DEBUG
    STRESS_LOG3(LF_CORDB, LL_INFO1000, "Reaction: %d (%s), line=%d\n",
                reaction.GetType(),
                reaction.GetReactionName(),
                reaction.GetLine());
#else
    STRESS_LOG1(LF_CORDB, LL_INFO1000, "Reaction: %d\n", reaction.GetType());
#endif

    // Make sure the lock wasn't accidentally released.
    _ASSERTE(ThreadHoldsProcessLock());
    CordbWin32EventThread * pW32EventThread = this->m_pShim->GetWin32EventThread();
    _ASSERTE(pW32EventThread != NULL);

    // if we were waiting for a retriggered exception but received any other event then turn
    // off the single stepping and dequeue the IB event. Right now we only use the SS flag internally
    // for stepping during possible retrigger.
    if(reaction.GetType() != Reaction::cInbandExceptionRetrigger && pUnmanagedThread->IsSSFlagNeeded())
    {
        _ASSERTE(pUnmanagedThread->HasIBEvent());
        CordbUnmanagedEvent* pUnmanagedEvent = pUnmanagedThread->IBEvent();
        _ASSERTE(pUnmanagedEvent->IsIBEvent());
        _ASSERTE(pUnmanagedEvent->IsEventContinuedUnhijacked());
        _ASSERTE(pUnmanagedEvent->IsDispatched());
        LOG((LF_CORDB, LL_INFO100000, "CP::HDEFID: IB event did not retrigger ue=0x%p\n", pUnmanagedEvent));

        DequeueUnmanagedEvent(pUnmanagedThread);
        pUnmanagedThread->EndStepping();
    }

    switch(reaction.GetType())
    {
    // Common for flares.
    case Reaction::cIgnore:

        // Shouldn't be suspending in the first place with outstanding flares.
        _ASSERTE(!pUnmanagedThread->IsSuspended());

        pW32EventThread->ForceDbgContinue(this, pUnmanagedThread, DBG_CONTINUE, false);
        goto LDone;

    case Reaction::cCLR:
        // Don't care if thread is suspended here. We'll just let the thread continue whatever it's doing.

        this->m_DbgSupport.m_TotalCLR++;

        // If this is for the CLR, then we just continue unhandled and know that the CLR has
        // a handler inplace to deal w/ this exception.
        pW32EventThread->ForceDbgContinue(this, pUnmanagedThread, DBG_EXCEPTION_NOT_HANDLED, false);
        goto LDone;


    case Reaction::cInband_NotNewEvent:
        fNewEvent = false;

        // fall through to Inband case...

    case Reaction::cInband:
    {
        this->m_DbgSupport.m_TotalIB++;

        // Hijack in-band events (exception events, exit threads) if there is already an event at the head
        // of the queue or if the process is currently synchronized. Of course, we only do this if the
        // process is initialized.
        //
        // Note: we also hijack these left over in-band events if we're actively trying to send the
        // managed continue message to the Left Side. This is controlled by m_specialDeferment below.

        // Only exceptions can be IB events - everything else is OOB.
        _ASSERTE(pEvent->dwDebugEventCode == EXCEPTION_DEBUG_EVENT);

        // CLR internal exceptions should be sent back to the CLR and never treated as inband events.
        // If this assert fires, the event was triaged wrong.
        CONSISTENCY_CHECK_MSGF((pEvent->u.Exception.ExceptionRecord.ExceptionCode != EXCEPTION_COMPLUS),
            ("Attempting to dispatch a CLR internal exception as an Inband event. Reaction line=%d\n",
             reaction.GetLine()));


        _ASSERTE(!pUnmanagedThread->IsCantStop());

        // We need to decide whether or not to dispatch this event immediately
        // We defer it to enforce that we only dispatch 1 IB event at a time (managed events are
        // considered IB here).
        // This means if:
        // 1) there's already an outstanding unmanaged inband event (an event the user has not continued from)
        // 2) If the process is synchronized (since that means we've already dispatched a managed event).
        // 3) If we've received a SyncComplete event, but aren't yet Sync.  This will almost always be the same as
        //    whether we're synced, but has a distict quality. It's always set by the w32 event thread in Interop,
        //    and so it's guaranteed to be serialized against this check here (also on the w32et).
        // 4) Special deferment - This covers the region where we're sending a Stop/Continue IPC event across.
        //    We defer it here to keep the Helper thread alive so that it can handle these IPC events.
        // Queued events will be dispatched when continue is called.
        BOOL fHasUserUncontinuedNativeEvents = HasUserUncontinuedNativeEvents();
        bool fDeferInbandEvent = (fHasUserUncontinuedNativeEvents ||
                                  GetSynchronized() ||
                                  GetSyncCompleteRecv() ||
                                  m_specialDeferment);

        // If we've got a new event, queue it.
        if (fNewEvent)
        {
            this->QueueUnmanagedEvent(pUnmanagedThread, pEvent);
        }

        if (fNewEvent && this->m_initialized && fDeferInbandEvent)
        {
            STRESS_LOG4(LF_CORDB, LL_INFO1000, "W32ET::W32EL: Needed to defer dispatching event: %d %d %d %d\n",
                 fHasUserUncontinuedNativeEvents,
                 GetSynchronized(),
                 GetSyncCompleteRecv(),
                 m_specialDeferment);

            // this continues the IB debug event into the hijack
            // the process is now running again
            pW32EventThread->DoDbgContinue(this, pUnmanagedThread->IBEvent());

            // Since we've hijacked this event, we don't need to do any further processing.
            goto LDone;
        }
        else
        {
            // No need to defer the dispatch, do it now
            this->DispatchUnmanagedInBandEvent();

            goto LDone;
        }
        UNREACHABLE();
    }

    case Reaction::cFirstChanceHijackStarted:
    {
        // determine the logical event we are handling, if any
        CordbUnmanagedEvent* pUnmanagedEvent = NULL;
        if(pUnmanagedThread->HasIBEvent())
        {
            pUnmanagedEvent = pUnmanagedThread->IBEvent();
        }
        LOG((LF_CORDB, LL_INFO100000, "W32ET::W32EL: IB hijack starting, ue=0x%p\n", pUnmanagedEvent));

        // fetch the LS memory set up for this hijack
        REMOTE_PTR pDebuggerWord = NULL;
        DebuggerIPCFirstChanceData fcd;
        pUnmanagedThread->GetEEDebuggerWord(&pDebuggerWord);
        SafeReadStruct(PTR_TO_CORDB_ADDRESS(pDebuggerWord), &fcd);

        LOG((LF_CORDB, LL_INFO100000, "W32ET::W32EL: old fcd DebugCounter=0x%x\n", fcd.debugCounter));

        // determine what action the LS should take
        if(pUnmanagedThread->IsBlockingForSync())
        {
            // there should be an event we hijacked in this case
            _ASSERTE(pUnmanagedEvent != NULL);

            // block that event
            LOG((LF_CORDB, LL_INFO100000, "W32ET::W32EL: blocking\n"));
            fcd.action = HIJACK_ACTION_WAIT;
            fcd.debugCounter = 0x2;
            SafeWriteStruct(PTR_TO_CORDB_ADDRESS(pDebuggerWord), &fcd);
        }
        else
        {
            // we don't need to block. We want the vectored handler to just exit
            // as if it wasn't there
            _ASSERTE(fcd.action == HIJACK_ACTION_EXIT_UNHANDLED);
            LOG((LF_CORDB, LL_INFO100000, "W32ET::W32EL: not blocking\n"));
        }

        LOG((LF_CORDB, LL_INFO100000, "W32ET::W32EL: continuing from flare\n"));
        pW32EventThread->ForceDbgContinue(this, pUnmanagedThread, DBG_CONTINUE, false);
        goto LDone;
    }

    case Reaction::cInbandHijackComplete:
    {
        // We now execute the hijack worker even when not actually hijacked
        // so can't assert this
        //_ASSERTE(pUnmanagedThread->IsFirstChanceHijacked());

        // we should not be stepping at the end of hijacks
        _ASSERTE(!pUnmanagedThread->IsSSFlagHidden());
        _ASSERTE(!pUnmanagedThread->IsSSFlagNeeded());

        // if we were hijacked then clean up
        if(pUnmanagedThread->IsFirstChanceHijacked())
        {
            LOG((LF_CORDB, LL_INFO100000, "W32ET::W32EL: hijack complete will restore context...\n"));
            DT_CONTEXT tempContext = { 0 };
            tempContext.ContextFlags = DT_CONTEXT_FULL;
            HRESULT hr = pUnmanagedThread->GetThreadContext(&tempContext);
            _ASSERTE(SUCCEEDED(hr));

            // The sync hijack returns normally but the m2uHandoff hijack needs to have the IP
            // deliberately restored
            if(!pUnmanagedThread->IsBlockingForSync())
            {
                // restore the context to the current un-hijacked context
                BOOL succ = DbiSetThreadContext(pUnmanagedThread->m_handle, &tempContext);
                _ASSERTE(succ);

                // Because hijacks don't return normally they might have pushed handlers without poping them
                // back off. To take care of that we explicitly restore the old SEH chain.
    #ifdef TARGET_X86
                hr = pUnmanagedThread->RestoreLeafSeh();
                _ASSERTE(SUCCEEDED(hr));
    #endif
            }
            else
            {
                _ASSERTE(pUnmanagedThread->HasIBEvent());
                CordbUnmanagedEvent* pUnmanagedEvent = pUnmanagedThread->IBEvent();
                LOG((LF_CORDB, LL_INFO100000, "W32ET::W32EL: IB hijack completing, continuing unhijacked ue=0x%p\n", pUnmanagedEvent));
                _ASSERTE(pUnmanagedEvent->IsEventContinuedHijacked());
                _ASSERTE(pUnmanagedEvent->IsDispatched());
                _ASSERTE(pUnmanagedEvent->IsEventUserContinued());
                _ASSERTE(!pUnmanagedEvent->IsEventContinuedUnhijacked());
                pUnmanagedEvent->SetState(CUES_EventContinuedUnhijacked);

                // fetch the LS memory set up for this hijack
                REMOTE_PTR pDebuggerWord = NULL;
                DebuggerIPCFirstChanceData fcd;
                pUnmanagedThread->GetEEDebuggerWord(&pDebuggerWord);
                SafeReadStruct(PTR_TO_CORDB_ADDRESS(pDebuggerWord), &fcd);

                LOG((LF_CORDB, LL_INFO10000, "W32ET::W32EL: pDebuggerWord is 0x%p\n", pDebuggerWord));

                //set the correct continuation action based upon the user's selection
                if(pUnmanagedEvent->IsExceptionCleared())
                {
                    LOG((LF_CORDB, LL_INFO10000, "W32ET::W32EL: exception cleared\n"));
                    fcd.action = HIJACK_ACTION_EXIT_HANDLED;
                }
                else
                {
                    LOG((LF_CORDB, LL_INFO10000, "W32ET::W32EL: exception not cleared\n"));
                    fcd.action = HIJACK_ACTION_EXIT_UNHANDLED;
                }

                //
                // LS context is restored here so that execution continues from next instruction that caused the hijack.
                // We shouldn't always restore the LS context though.
                // Consider the following case where this can cause issues:
                // Debuggee process hits an exception and calls KERNELBASE!RaiseException, debugger gets the notification and
                // prepares for first-chance hijack. Debugger(DBI) saves the current thread context (see SetupFirstChanceHijackForSync) which is restored
                // later below (see SafeWriteThreadContext call) when the process is in VEH (CLRVectoredExceptionHandlerShim->FirstChanceSuspendHijackWorker).
                // The thread context that got saved(by SetupFirstChanceHijackForSync) was for when the thread was executing RaiseException and when
                // this context gets restored in VEH, the thread resumes after the exception handler with a context that is not same as one with which
                // it entered. This inconsistency can lead to bad execution code-paths or even a debuggee crash.
                //
                // Example case where we should definitely update the LS context:
                // After a DbgBreakPoint call, IP gets updated to point to the instruction after int 3 and this is the context saved by debugger.
                // The IP in context passed to VEH still points to int 3 though and if we don't update the LS context in VEH, the breakpoint
                // instruction will get executed again.
                //
                // Here's a list of cases when we update the LS context:
                // * we know that context was explicitly updated during this hijack, OR
                // * if single-stepping flag was set on it originally, OR
                // * if this was a breakpoint event
                // Note that above list is a heuristic and it is possible that we need to add more such cases in future.
                //
                BOOL isBreakPointEvent = (pUnmanagedEvent->m_currentDebugEvent.dwDebugEventCode == EXCEPTION_DEBUG_EVENT &&
                    pUnmanagedEvent->m_currentDebugEvent.u.Exception.ExceptionRecord.ExceptionCode == STATUS_BREAKPOINT);
                if (pUnmanagedThread->IsContextSet() || IsSSFlagEnabled(&tempContext) || isBreakPointEvent)
                {
                    _ASSERTE(fcd.pLeftSideContext != NULL);
                    LOG((LF_CORDB, LL_INFO10000, "W32ET::W32EL: updating LS context at 0x%p\n", fcd.pLeftSideContext));
                    // write the new context over the old one on the LS
                    SafeWriteThreadContext(fcd.pLeftSideContext, &tempContext);
                }

                // Write the new Fcd data to the LS
                fcd.debugCounter = 0x1;
                SafeWriteStruct(PTR_TO_CORDB_ADDRESS(pDebuggerWord), &fcd);

                fcd.debugCounter = 0;
                SafeReadStruct(PTR_TO_CORDB_ADDRESS(pDebuggerWord), &fcd);
                _ASSERTE(fcd.debugCounter == 1);

                DequeueUnmanagedEvent(pUnmanagedThread);
            }

            _ASSERTE(m_cFirstChanceHijackedThreads > 0);
            m_cFirstChanceHijackedThreads--;
            if(m_cFirstChanceHijackedThreads == 0)
            {
                m_state &= ~PS_HIJACKS_IN_PLACE;
            }

            pUnmanagedThread->ClearState(CUTS_FirstChanceHijacked);
            pUnmanagedThread->ClearState(CUTS_BlockingForSync);

            // if the user set the context it either was already applied (m2uHandoff hijack)
            // or is about to be applied when the hijack returns (sync hijack).
            // There may still a small window where it won't appear accurate that
            // we just have to live with
            pUnmanagedThread->ClearState(CUTS_HasContextSet);
        }

        pW32EventThread->ForceDbgContinue(this, pUnmanagedThread, DBG_CONTINUE, false);

        // We've handled this event. Skip further processing.
        goto LDone;
    }

    case Reaction::cBreakpointRequiringHijack:
    {
        HRESULT hr = pUnmanagedThread->SetupFirstChanceHijack(EHijackReason::kM2UHandoff, &(pEvent->u.Exception.ExceptionRecord));
        _ASSERTE(SUCCEEDED(hr));
        pW32EventThread->ForceDbgContinue(this, pUnmanagedThread, DBG_CONTINUE, false);
        goto LDone;
    }

    case Reaction::cInbandExceptionRetrigger:
    {
        // this should be unused now
        _ASSERTE(FALSE);
        _ASSERTE(pUnmanagedThread->HasIBEvent());
        CordbUnmanagedEvent* pUnmanagedEvent = pUnmanagedThread->IBEvent();
        _ASSERTE(pUnmanagedEvent->IsIBEvent());
        _ASSERTE(pUnmanagedEvent->IsEventContinuedUnhijacked());
        _ASSERTE(pUnmanagedEvent->IsDispatched());
        LOG((LF_CORDB, LL_INFO100000, "W32ET::W32EL: IB event completing, continuing ue=0x%p\n", pUnmanagedEvent));

        DequeueUnmanagedEvent(pUnmanagedThread);
        // If this event came from RaiseException then flush the context to ensure we won't use it until we re-enter
        if(pUnmanagedEvent->m_owner->IsRaiseExceptionHijacked())
        {
            pUnmanagedEvent->m_owner->RestoreFromRaiseExceptionHijack();
            pUnmanagedEvent->m_owner->ClearRaiseExceptionEntryContext();
        }
        else // otherwise we should have been stepping
        {
            pUnmanagedThread->EndStepping();
        }
        pW32EventThread->ForceDbgContinue(this, pUnmanagedThread,
            pUnmanagedEvent->IsExceptionCleared() ? DBG_CONTINUE : DBG_EXCEPTION_NOT_HANDLED, false);

        // We've handled this event. Skip further processing.
        goto LDone;
    }

    case Reaction::cOOB:
    {
        // Don't care if this thread claimed to be suspended or not. Dispatch event anyways. After all,
        // OOB events can come at *any* time.

        // This thread may be suspended. We don't care.
        this->m_DbgSupport.m_TotalOOB++;

        // Not an  inband event. This includes ALL non-exception events (including EXIT_THREAD) as
        // well as any exception that can't be hijacked (ex, an exception on the helper thread).

        // If this is an exit thread or exit process event, then we need to mark the unmanaged thread as
        // exited for later.
        if ((pEvent->dwDebugEventCode == EXIT_PROCESS_DEBUG_EVENT) ||
            (pEvent->dwDebugEventCode == EXIT_THREAD_DEBUG_EVENT))
        {
            pUnmanagedThread->SetState(CUTS_Deleted);
        }

        // If we get an exit process or exit thread event on the helper thread, then we know we're loosing
        // the Left Side, so go ahead and remember that the helper thread has died.
        if (this->IsHelperThreadWorked(pUnmanagedThread->GetOSTid()))
        {
            if ((pEvent->dwDebugEventCode == EXIT_PROCESS_DEBUG_EVENT) ||
                (pEvent->dwDebugEventCode == EXIT_THREAD_DEBUG_EVENT))
            {
                this->m_helperThreadDead = true;
            }
        }

        // Queue the current out-of-band event.
        this->QueueOOBUnmanagedEvent(pUnmanagedThread, pEvent);

        // Go ahead and dispatch the event if its the first one.
        if (this->m_outOfBandEventQueue == pUnmanagedThread->OOBEvent())
        {
            // Set this to true to indicate to Continue() that we're in the unamnaged callback.
            CordbUnmanagedEvent * pUnmanagedEvent = pUnmanagedThread->OOBEvent();

            this->m_dispatchingOOBEvent = true;

            pUnmanagedEvent->SetState(CUES_Dispatched);

            this->Unlock();

            // Handler should have been registered by now.
            _ASSERTE(this->m_cordb->m_unmanagedCallback != NULL);

            // Call the callback with fIsOutOfBand = TRUE.
            {
                PUBLIC_WIN32_CALLBACK_IN_THIS_SCOPE(this, pEvent, TRUE);
                this->m_cordb->m_unmanagedCallback->DebugEvent(const_cast<DEBUG_EVENT*> (pEvent), TRUE);
            }

            this->Lock();

            // If m_dispatchingOOBEvent is false, that means that the user called Continue() from within
            // the callback. We know that we can go ahead and continue the process now.
            if (this->m_dispatchingOOBEvent == false)
            {
                // Note: this call will dispatch more OOB events if necessary.
                pW32EventThread->UnmanagedContinue(this, cOobUMContinue);
            }
            else
            {
                // We're not dispatching anymore, so set this back to false.
                this->m_dispatchingOOBEvent = false;
            }
        }

        // We've handled this event. Skip further processing.
        goto LDone;
    }
    } // end Switch on Reaction

    UNREACHABLE();

LDone:
    // Process Lock implicitly released by holder.

    STRESS_LOG0(LF_CORDB, LL_INFO1000, "W32ET::W32EL: done processing event.\n");

    return;
}

//
// Returns true if the exception is a flare from the left side, false otherwise.
//
bool CordbProcess::ExceptionIsFlare(DWORD exceptionCode, const void *exceptionAddress)
{
    _ASSERTE(m_runtimeOffsetsInitialized);

    // Can't have a flare if the left side isn't initialized
    if (m_initialized)
    {
        DebuggerIPCRuntimeOffsets *pRO = &m_runtimeOffsets;

        // All flares are breakpoints...
        if (exceptionCode == STATUS_BREAKPOINT)
        {
            // Does the breakpoint address match a flare address?
            if ((exceptionAddress == pRO->m_signalHijackStartedBPAddr) ||
                (exceptionAddress == pRO->m_excepForRuntimeHandoffStartBPAddr) ||
                (exceptionAddress == pRO->m_excepForRuntimeHandoffCompleteBPAddr) ||
                (exceptionAddress == pRO->m_signalHijackCompleteBPAddr) ||
                (exceptionAddress == pRO->m_excepNotForRuntimeBPAddr) ||
                (exceptionAddress == pRO->m_notifyRSOfSyncCompleteBPAddr))
                return true;
        }
    }

    return false;
}
#endif // FEATURE_INTEROP_DEBUGGING

// Allocate a buffer in the target and copy data into it.
//
// Arguments:
//    pDomain - an appdomain associated with the allocation request.
//    bufferSize - size of the buffer in bytes
//    bufferFrom - local buffer of data (bufferSize bytes) to copy data from.
//    ppRes - address into target of allocated buffer
//
// Returns:
//    S_OK on success, else error.
HRESULT CordbProcess::GetAndWriteRemoteBuffer(CordbAppDomain *pDomain, unsigned int bufferSize, const void *bufferFrom, void **ppRes)
{
    _ASSERTE(ppRes != NULL);
    *ppRes = NULL;

    HRESULT hr = S_OK;

    EX_TRY
    {
        TargetBuffer tbTarget = GetRemoteBuffer(bufferSize); // throws
        SafeWriteBuffer(tbTarget, (const BYTE*) bufferFrom); // throws

        // Succeeded.
        *ppRes = CORDB_ADDRESS_TO_PTR(tbTarget.pAddress);
    }
    EX_CATCH_HRESULT(hr);
    return hr;
}

#ifdef FEATURE_INTEROP_DEBUGGING

//
// Checks to see if the given second chance exception event actually signifies the death of the process due to a second
// stack overflow special case.
//
// There are strange cases with stack overflow exceptions. If a nieve application catches a stack overflow exception and
// handles it, without resetting the guard page, then the app will get an AV when it overflows the stack a second time. We
// will get the first chance AV, but when we continue from it the OS won't run any SEH handlers, so our FCH won't
// actually work. Instead, we'll get the AV back on second chance right away.
//
bool CordbProcess::IsSpecialStackOverflowCase(CordbUnmanagedThread *pUThread, const DEBUG_EVENT *pEvent)
{
    _ASSERTE(pEvent->dwDebugEventCode == EXCEPTION_DEBUG_EVENT);
    _ASSERTE(pEvent->u.Exception.dwFirstChance == 0);

    // If this is not an AV, it can't be our special case.
    if (pEvent->u.Exception.ExceptionRecord.ExceptionCode != STATUS_ACCESS_VIOLATION)
        return false;

    // If the thread isn't already first chance hijacked, it can't be our special case.
    if (!pUThread->IsFirstChanceHijacked())
        return false;

    // The first chance hijack didn't take, so we're not FCH anymore and we're not waiting for an answer
    // anymore... Note: by leaving this thread completely unhijacked, we'll report its true context, which is correct.
    pUThread->ClearState(CUTS_FirstChanceHijacked);

    // The process is techincally dead as a door nail here, so we'll mark that the helper thread is dead so our managed
    // API bails nicely.
    m_helperThreadDead = true;

    // Remember we're in our special case.
    pUThread->SetState(CUTS_HasSpecialStackOverflowCase);

    // Now, remember the second chance AV event in the second IB event slot for this thread and add it to the end of the
    // IB event queue.
    QueueUnmanagedEvent(pUThread, pEvent);

    // Note: returning true will ensure that the queued first chance AV for this thread is dispatched.
    return true;
}

//-----------------------------------------------------------------------------
// Longhorn broke ContinueDebugEvent.
// In previous OS releases, DBG_CONTINUE would continue a  non-continuable exception.
// In longhorn, we need to pass the DBG_FORCE_CONTINUE flag to do that.
// Note that all CLR exceptions are non-continuable.
// Now instead of DBG_CONTINUE, we need to pass DBG_FORCE_CONTINUE.
//-----------------------------------------------------------------------------

// Currently we don't have headers for the longhorn winnt.h. So we need to privately declare
// this here. We have a check such that if we do get headers, the value won't change underneath us.
#define MY_DBG_FORCE_CONTINUE               ((DWORD   )0x00010003L)
#ifndef DBG_FORCE_CONTINUE
#define DBG_FORCE_CONTINUE MY_DBG_FORCE_CONTINUE
#else
static_assert_no_msg(DBG_FORCE_CONTINUE == MY_DBG_FORCE_CONTINUE);
#endif

DWORD GetDbgContinueFlag()
{
    // Currently, default to not using the new DBG_FORCE_CONTINUE flag.
    static ConfigDWORD fNoFlagKey;
    bool fNoFlag = fNoFlagKey.val(CLRConfig::UNSUPPORTED_DbgNoForceContinue) != 0;


    if (!fNoFlag)
    {
        return DBG_FORCE_CONTINUE;
    }
    else
    {
        return DBG_CONTINUE;
    }
}


// Some Interop bugs involve threads that land at a bad IP. Since we're interop-debugging, we can't
// attach a debugger to the LS. So we have some debug mode where we enable the SS flag and thus
// produce a trace of where a thread is going.
#ifdef _DEBUG
void EnableDebugTrace(CordbUnmanagedThread *ut)
{
    // To enable, attach w/ a debugger and either set fTrace==true, or setip.
    static bool fTrace = false;
    if (!fTrace)
        return;

    // Give us a nop so that we can setip in the optimized case.
#ifdef TARGET_X86
    __asm {
        nop
    }
#endif

    fTrace = true;
    CordbProcess *pProcess = ut->GetProcess();

    // Get the context
    HRESULT hr = S_OK;
    DT_CONTEXT context;
    context.ContextFlags = DT_CONTEXT_FULL;


    hr = pProcess->GetThreadContext((DWORD) ut->m_id, sizeof(context), (BYTE*)&context);
    if (FAILED(hr))
        return;

    // If the flag is already set, then don't set it again - that will just get confusing.
    if (IsSSFlagEnabled(&context))
    {
        return;
    }
    _ASSERTE(CORDbgGetIP(&context) != 0);
    SetSSFlag(&context);

    // If SS flag not set, enable it. And remeber that it's us so we know how to handle
    // it when we get the debug event.
    hr = pProcess->SetThreadContext((DWORD)ut->m_id, sizeof(context), (BYTE*)&context);
    ut->SetState(CUTS_DEBUG_SingleStep);
}
#endif // _DEBUG

//-----------------------------------------------------------------------------
// DoDbgContinue
//
// Continues from a specific Win32 DEBUG_EVENT.
//
// Arguments:
//    pProcess - The process to continue.
//    pUnmanagedEvent - The event to continue.
//
//-----------------------------------------------------------------------------
void CordbWin32EventThread::DoDbgContinue(CordbProcess *pProcess,
                                          CordbUnmanagedEvent *pUnmanagedEvent)
{
    _ASSERTE(pProcess->ThreadHoldsProcessLock());
    _ASSERTE(IsWin32EventThread());
    _ASSERTE(pUnmanagedEvent != NULL);
    _ASSERTE(!pUnmanagedEvent->IsEventContinuedUnhijacked());

    STRESS_LOG3(LF_CORDB, LL_INFO1000,
         "W32ET::DDC: continue with ue=0x%p, thread=0x%p, tid=0x%x\n",
         pUnmanagedEvent,
         pUnmanagedEvent->m_owner,
         pUnmanagedEvent->m_owner->m_id);

#ifdef _DEBUG
    EnableDebugTrace(pUnmanagedEvent->m_owner);
#endif


    if (pUnmanagedEvent->IsEventContinuedHijacked())
    {
        LOG((LF_CORDB, LL_INFO100000, "W32ET::DDC: Skiping DoDbgContinue because event was already"
            " continued hijacked, ue=0x%p\n", pUnmanagedEvent));
        return;
    }

    BOOL threadIsHijacked = (pUnmanagedEvent->m_owner->IsFirstChanceHijacked() ||
                             pUnmanagedEvent->m_owner->IsGenericHijacked());

    BOOL eventIsIB = (pUnmanagedEvent->m_owner->HasIBEvent() &&
                      pUnmanagedEvent->m_owner->IBEvent() == pUnmanagedEvent);

    _ASSERTE((DWORD) pProcess->m_id == pUnmanagedEvent->m_currentDebugEvent.dwProcessId);
    _ASSERTE(pProcess->m_state & CordbProcess::PS_WIN32_STOPPED);

    DWORD dwContType;
    if(eventIsIB)
    {
        // 3 cases here...
        // event was already hijacked
        if(threadIsHijacked)
        {
            LOG((LF_CORDB, LL_INFO100000, "W32ET::DDC: Continuing IB, already hijacked, ue=0x%p\n", pUnmanagedEvent));
            pUnmanagedEvent->SetState(CUES_EventContinuedHijacked);
            dwContType = !pUnmanagedEvent->m_owner->IsBlockingForSync() ? GetDbgContinueFlag() : DBG_EXCEPTION_NOT_HANDLED;
        }
        // event was not hijacked but has been dispatched
        else if(!threadIsHijacked && pUnmanagedEvent->IsDispatched())
        {
            LOG((LF_CORDB, LL_INFO100000, "W32ET::DDC: Continuing IB, not hijacked, ue=0x%p\n", pUnmanagedEvent));
            _ASSERTE(pUnmanagedEvent->IsDispatched());
            _ASSERTE(pUnmanagedEvent->IsEventUserContinued());
            _ASSERTE(!pUnmanagedEvent->IsEventContinuedUnhijacked());
            pUnmanagedEvent->SetState(CUES_EventContinuedUnhijacked);
            dwContType = pUnmanagedEvent->IsExceptionCleared() ? GetDbgContinueFlag() : DBG_EXCEPTION_NOT_HANDLED;

            // The event was never hijacked and so will never need to retrigger, get rid
            // of it right now. If it had been hijacked then we would dequeue it either after the
            // hijack complete flare or one instruction after that when it has had a chance to retrigger
            pProcess->DequeueUnmanagedEvent(pUnmanagedEvent->m_owner);
        }
        // event was not hijacked nor dispatched
        else // if(!threadIsHijacked && !pUnmanagedEvent->IsDispatched())
        {
            LOG((LF_CORDB, LL_INFO100000, "W32ET::DDC: Continuing IB, now hijacked, ue=0x%p\n", pUnmanagedEvent));
            HRESULT hr = pProcess->HijackIBEvent(pUnmanagedEvent);
            _ASSERTE(SUCCEEDED(hr));
            pUnmanagedEvent->SetState(CUES_EventContinuedHijacked);
            dwContType = !pUnmanagedEvent->m_owner->IsBlockingForSync() ? GetDbgContinueFlag() : DBG_EXCEPTION_NOT_HANDLED;
        }
    }
    else
    {
        LOG((LF_CORDB, LL_INFO100000, "W32ET::DDC: Continuing OB, ue=0x%p\n", pUnmanagedEvent));
        // we might actually be hijacked here, but if we are it should be for a previous IB event
        // we just mark all OB events as continued unhijacked
        pUnmanagedEvent->SetState(CUES_EventContinuedUnhijacked);
        dwContType = pUnmanagedEvent->IsExceptionCleared() ? GetDbgContinueFlag() : DBG_EXCEPTION_NOT_HANDLED;
    }

    // If the exception is marked as unclearable, then make sure the continue type is correct and force the process
    // to terminate.
    if (pUnmanagedEvent->IsExceptionUnclearable())
    {
        TerminateProcess(pProcess->UnsafeGetProcessHandle(), pUnmanagedEvent->m_currentDebugEvent.u.Exception.ExceptionRecord.ExceptionCode);
        dwContType = DBG_EXCEPTION_NOT_HANDLED;
    }

    // If we're continuing from the loader-bp, then send the managed attach here.
    // (Note this will only be set if the runtime was loaded when we first tried to attach).
    // We assume that the loader-bp is the 1st BP exception. This is naive,
    // since it's not 100% accurate (someone could CreateThread w/ a threadproc of DebugBreak).
    // But it's the best we can do.
    // Note that it's critical we do this BEFORE continuing the process.  If this is mixed-mode, we've already
    // told VS about this breakpoint, and so it's set the attach-complete event.  As soon as we continue this debug
    // event the process can start moving again, so the CLR needs to know to wait for a managed attach.
    DWORD dwEventCode = pUnmanagedEvent->m_currentDebugEvent.dwDebugEventCode;
    if (dwEventCode == EXCEPTION_DEBUG_EVENT)
    {
        EXCEPTION_DEBUG_INFO * pDebugInfo = &pUnmanagedEvent->m_currentDebugEvent.u.Exception;
        if (pDebugInfo->dwFirstChance && pDebugInfo->ExceptionRecord.ExceptionCode == STATUS_BREAKPOINT)
        {
            HRESULT hrIgnore = S_OK;
            EX_TRY
            {
                LOG((LF_CORDB, LL_INFO1000, "W32ET::DDC: Continuing from LdrBp, doing managed attach.\n"));
                pProcess->QueueManagedAttachIfNeededWorker();
            }
            EX_CATCH_HRESULT(hrIgnore);
            SIMPLIFYING_ASSUMPTION(SUCCEEDED(hrIgnore));
        }
    }

    STRESS_LOG4(LF_CORDB, LL_INFO1000,
        "W32ET::DDC: calling ContinueDebugEvent(0x%x, 0x%x, 0x%x), process state=0x%x\n",
        pProcess->m_id, pUnmanagedEvent->m_owner->m_id, dwContType, pProcess->m_state);

    // Actually continue the debug event
    pProcess->m_state &= ~CordbProcess::PS_WIN32_STOPPED;
    BOOL fSuccess = m_pNativePipeline->ContinueDebugEvent((DWORD)pProcess->m_id, (DWORD)pUnmanagedEvent->m_owner->m_id, dwContType);

    // ContinueDebugEvent may 'fail' if we force kill the debuggee while stopped at the exit-process event.
    if (!fSuccess && (dwEventCode != EXIT_PROCESS_DEBUG_EVENT))
    {
        _ASSERTE(!"ContinueDebugEvent failed!");
        CORDBSetUnrecoverableError(pProcess, HRESULT_FROM_GetLastError(), 0);
        STRESS_LOG1(LF_CORDB, LL_INFO1000, "W32ET::DDC: Last error after ContinueDebugEvent is %d\n", GetLastError());
    }

    // If this thread is marked for deletion (exit thread or exit process event on it), then we need to delete the
    // unmanaged thread object.
    if ((dwEventCode == EXIT_PROCESS_DEBUG_EVENT) || (dwEventCode == EXIT_THREAD_DEBUG_EVENT))
    {
        CordbUnmanagedThread * pUnmanagedThread = pUnmanagedEvent->m_owner;
        _ASSERTE(pUnmanagedThread->IsDeleted());


        // Thread may have a hijacked inband event on it. Thus it's actually running free from the OS perspective,
        // and fair game to be terminated. In that case, we need to auto-dequeue the event.
        // This will just prevent the RS from making the underlying call to ContinueDebugEvent on this thread
        // for the inband event. Since we've already lost the thread, that's actually exactly what we want.
        if (pUnmanagedThread->HasIBEvent())
        {
            pProcess->DequeueUnmanagedEvent(pUnmanagedThread);
        }

        STRESS_LOG1(LF_CORDB, LL_INFO1000, "Removing thread 0x%x (%d) from process list\n", pUnmanagedThread->m_id);
        pProcess->m_unmanagedThreads.RemoveBase((ULONG_PTR)pUnmanagedThread->m_id);
    }


    // If we just continued from an exit process event, then its time to do the exit processing.
    if (dwEventCode == EXIT_PROCESS_DEBUG_EVENT)
    {
        pProcess->Unlock();
        ExitProcess(false); // not detach case
        pProcess->Lock();
    }

}

//---------------------------------------------------------------------------------------
//
// ForceDbgContinue continues from the last Win32 DEBUG_EVENT on the given thread, no matter what it was.
//
// Arguments:
//      pProcess - process object to continue
//      pUnmanagedThread - unmanaged thread object (maybe null if we're doing a raw cotninue)
//      contType - continuation status (DBG_CONTINUE or DBG_EXCEPTION_NOT_HANDLED)
//      fContinueProcess - do we resume hijacks?
//
void CordbWin32EventThread::ForceDbgContinue(CordbProcess *pProcess, CordbUnmanagedThread *pUnmanagedThread, DWORD contType,
                                             bool fContinueProcess)
{
    _ASSERTE(pProcess->ThreadHoldsProcessLock());
    _ASSERTE(pUnmanagedThread != NULL);
    STRESS_LOG4(LF_CORDB, LL_INFO1000,
         "W32ET::FDC: force continue with 0x%x (%s), contProcess=%d, tid=0x%x\n",
         contType,
         (contType == DBG_CONTINUE) ? "DBG_CONTINUE" : "DBG_EXCEPTION_NOT_HANDLED",
         fContinueProcess,
         pUnmanagedThread->m_id);

    if (fContinueProcess)
    {
        pProcess->ResumeHijackedThreads();
    }

    if (contType == DBG_CONTINUE)
    {
        contType = GetDbgContinueFlag();
    }

    _ASSERTE(pProcess->m_state & CordbProcess::PS_WIN32_STOPPED);

    // Remove the Win32 stopped flag so long as the OOB event queue is empty. We're forcing a continue here, so by
    // definition this should be the case...
    _ASSERTE(pProcess->m_outOfBandEventQueue == NULL);

    pProcess->m_state &= ~CordbProcess::PS_WIN32_STOPPED;

    STRESS_LOG4(LF_CORDB, LL_INFO1000, "W32ET::FDC: calling ContinueDebugEvent(0x%x, 0x%x, 0x%x), process state=0x%x\n",
         pProcess->m_id, pUnmanagedThread->m_id, contType, pProcess->m_state);


    #ifdef _DEBUG
    EnableDebugTrace(pUnmanagedThread);
    #endif
    BOOL ret = m_pNativePipeline->ContinueDebugEvent((DWORD)pProcess->m_id, (DWORD)pUnmanagedThread->m_id, contType);

    if (!ret)
    {
        // This could in theory fail from Process exit, but that really would only be on the DoDbgContinue path.
         _ASSERTE(!"ContinueDebugEvent failed #2!");
        STRESS_LOG1(LF_CORDB, LL_INFO1000, "W32ET::DDC: Last error after ContinueDebugEvent is %d\n", GetLastError());
    }
}
#endif // FEATURE_INTEROP_DEBUGGING

//
// This is the thread's real thread proc. It simply calls to the
// thread proc on the given object.
//
/*static*/ DWORD WINAPI CordbWin32EventThread::ThreadProc(LPVOID parameter)
{
    CordbWin32EventThread* t = (CordbWin32EventThread*) parameter;
    INTERNAL_THREAD_ENTRY(t);
    t->ThreadProc();
    return 0;
}


//
// Send a CreateProcess event to the Win32 thread to have it create us
// a new process.
//
HRESULT CordbWin32EventThread::SendCreateProcessEvent(
                                  MachineInfo machineInfo,
                                  LPCWSTR programName,
                                  _In_z_ LPWSTR  programArgs,
                                  LPSECURITY_ATTRIBUTES lpProcessAttributes,
                                  LPSECURITY_ATTRIBUTES lpThreadAttributes,
                                  BOOL bInheritHandles,
                                  DWORD dwCreationFlags,
                                  PVOID lpEnvironment,
                                  LPCWSTR lpCurrentDirectory,
                                  LPSTARTUPINFOW lpStartupInfo,
                                  LPPROCESS_INFORMATION lpProcessInformation,
                                  CorDebugCreateProcessFlags corDebugFlags)
{
    HRESULT hr = S_OK;

    LockSendToWin32EventThreadMutex();
    LOG((LF_CORDB, LL_EVERYTHING, "CordbWin32EventThread::SCPE Called\n"));
    m_actionData.createData.machineInfo = machineInfo;
    m_actionData.createData.programName = programName;
    m_actionData.createData.programArgs = programArgs;
    m_actionData.createData.lpProcessAttributes = lpProcessAttributes;
    m_actionData.createData.lpThreadAttributes = lpThreadAttributes;
    m_actionData.createData.bInheritHandles = bInheritHandles;
    m_actionData.createData.dwCreationFlags = dwCreationFlags;
    m_actionData.createData.lpEnvironment = lpEnvironment;
    m_actionData.createData.lpCurrentDirectory = lpCurrentDirectory;
    m_actionData.createData.lpStartupInfo = lpStartupInfo;
    m_actionData.createData.lpProcessInformation = lpProcessInformation;
    m_actionData.createData.corDebugFlags = corDebugFlags;

    // m_action is set last so that the win32 event thread can inspect
    // it and take action without actually having to take any
    // locks. The lock around this here is simply to prevent multiple
    // threads from making requests at the same time.
    m_action = W32ETA_CREATE_PROCESS;

    BOOL succ = SetEvent(m_threadControlEvent);

    if (succ)
    {
      DWORD ret = WaitForSingleObject(m_actionTakenEvent, INFINITE);

        LOG((LF_CORDB, LL_EVERYTHING, "Process Handle is: %x, m_threadControlEvent is %x\n",
             (UINT_PTR)m_actionData.createData.lpProcessInformation->hProcess, (UINT_PTR)m_threadControlEvent));

        if (ret == WAIT_OBJECT_0)
            hr = m_actionResult;
        else
            hr = HRESULT_FROM_GetLastError();
    }
    else
        hr = HRESULT_FROM_GetLastError();

    UnlockSendToWin32EventThreadMutex();

    return hr;
}


//---------------------------------------------------------------------------------------
//
// Create a process
//
// Assumptions:
//    This occurs on the win32 event thread. It is invokved via
//    a message sent from code:CordbWin32EventThread::SendCreateProcessEvent
//
// Notes:
//    Create a new process. This is called in the context of the Win32
//    event thread to ensure that if we're Win32 debugging the process
//    that the same thread that waits for debugging events will be the
//    thread that creates the process.
//
//---------------------------------------------------------------------------------------
void CordbWin32EventThread::CreateProcess()
{
    m_action = W32ETA_NONE;
    HRESULT hr = S_OK;

    DWORD dwCreationFlags = m_actionData.createData.dwCreationFlags;

    // If the creation flags has DEBUG_PROCESS in them, then we're
    // Win32 debugging this process. Otherwise, we have to create
    // suspended to give us time to setup up our side of the IPC
    // channel.
    BOOL fInteropDebugging   =
#if defined(FEATURE_INTEROP_DEBUGGING)
        (dwCreationFlags & (DEBUG_PROCESS | DEBUG_ONLY_THIS_PROCESS));
#else
        false; // Interop not supported.
#endif

    // Have Win32 create the process...
    hr = m_pNativePipeline->CreateProcessUnderDebugger(
                                      m_actionData.createData.machineInfo,
                                      m_actionData.createData.programName,
                                      m_actionData.createData.programArgs,
                                      m_actionData.createData.lpProcessAttributes,
                                      m_actionData.createData.lpThreadAttributes,
                                      m_actionData.createData.bInheritHandles,
                                      dwCreationFlags,
                                      m_actionData.createData.lpEnvironment,
                                      m_actionData.createData.lpCurrentDirectory,
                                      m_actionData.createData.lpStartupInfo,
                                      m_actionData.createData.lpProcessInformation);

    if (SUCCEEDED(hr))
    {
        // Process ID is filled in after process is successfully created.
        DWORD dwProcessId = m_actionData.createData.lpProcessInformation->dwProcessId;
        ProcessDescriptor pd = ProcessDescriptor::FromPid(dwProcessId);

        RSUnsafeExternalSmartPtr<CordbProcess> pProcess;
        hr = m_pShim->InitializeDataTarget(&pd);

        if (SUCCEEDED(hr))
        {
            // To emulate V2 semantics, we pass 0 for the clrInstanceID into
            // OpenVirtualProcess. This will then connect to the first CLR
            // loaded.
            const ULONG64 cFirstClrLoaded = 0;
            hr = CordbProcess::OpenVirtualProcess(cFirstClrLoaded, m_pShim->GetDataTarget(), NULL, m_cordb, &pd, m_pShim, &pProcess);
        }

        // Shouldn't happen on a create, only an attach
        _ASSERTE(hr != CORDBG_E_DEBUGGER_ALREADY_ATTACHED);

        // Remember the process in the global list of processes.
        if (SUCCEEDED(hr))
        {
            EX_TRY
            {
                // Mark if we're interop-debugging
                if (fInteropDebugging)
                {
                    pProcess->EnableInteropDebugging();
                }

                m_cordb->AddProcess(pProcess); // will take ref if it succeeds
            }
            EX_CATCH_HRESULT(hr);
        }

        // If we're Win32 attached to this process, then increment the
        // proper count, otherwise add this process to the wait set
        // and resume the process's main thread.
        if (SUCCEEDED(hr))
        {
            _ASSERTE(m_pProcess == NULL);
            m_pProcess.Assign(pProcess);
        }
    }


    //
    // Signal the hr to the caller.
    //
    m_actionResult = hr;
    SetEvent(m_actionTakenEvent);
}


//
// Send a DebugActiveProcess event to the Win32 thread to have it attach to
// a new process.
//
HRESULT CordbWin32EventThread::SendDebugActiveProcessEvent(
                                                  MachineInfo machineInfo,
                                                  const ProcessDescriptor *pProcessDescriptor,
                                                  bool fWin32Attach,
                                                  CordbProcess *pProcess)
{
    HRESULT hr = S_OK;

    LockSendToWin32EventThreadMutex();

    m_actionData.attachData.machineInfo = machineInfo;
    m_actionData.attachData.processDescriptor = *pProcessDescriptor;
#if !defined(FEATURE_DBGIPC_TRANSPORT_DI)
    m_actionData.attachData.fWin32Attach = fWin32Attach;
#endif
    m_actionData.attachData.pProcess = pProcess;

    // m_action is set last so that the win32 event thread can inspect
    // it and take action without actually having to take any
    // locks. The lock around this here is simply to prevent multiple
    // threads from making requests at the same time.
    m_action = W32ETA_ATTACH_PROCESS;

    BOOL succ = SetEvent(m_threadControlEvent);

    if (succ)
    {
        DWORD ret = WaitForSingleObject(m_actionTakenEvent, INFINITE);

        if (ret == WAIT_OBJECT_0)
            hr = m_actionResult;
        else
            hr = HRESULT_FROM_GetLastError();
    }
    else
        hr = HRESULT_FROM_GetLastError();

    UnlockSendToWin32EventThreadMutex();

    return hr;
}

//-----------------------------------------------------------------------------
// Is the given thread id a helper thread (real or worker?)
//-----------------------------------------------------------------------------
bool CordbProcess::IsHelperThreadWorked(DWORD tid)
{
    // Check against the id gained by sniffing Thread-Create events.
    if (tid == this->m_helperThreadId)
    {
        return true;
    }

    // Now check for potential datate in the IPC block. If not there,
    // then we know it can't be the helper.
    DebuggerIPCControlBlock * pDCB = this->GetDCB();

    if (pDCB == NULL)
    {
        return false;
    }

    // get the latest information from the LS DCB
    UpdateRightSideDCB();
    return
        (tid == pDCB->m_realHelperThreadId) ||
        (tid == pDCB->m_temporaryHelperThreadId);

}

//---------------------------------------------------------------------------------------
//
// Cleans up the Left Side's DCB after a failed attach attempt.
//
// Assumptions:
//    Called when the left-site failed initialization
//
// Notes:
//    This can be called multiple times.
//---------------------------------------------------------------------------------------
void CordbProcess::CleanupHalfBakedLeftSide()
{
    if (GetDCB() != NULL)
    {
        EX_TRY
        {
            GetDCB()->m_rightSideIsWin32Debugger = false;
            UpdateLeftSideDCBField(&(GetDCB()->m_rightSideIsWin32Debugger), sizeof(GetDCB()->m_rightSideIsWin32Debugger));

            if (m_pEventChannel != NULL)
            {
                m_pEventChannel->Delete();
                m_pEventChannel = NULL;
            }
        }
        EX_CATCH
        {
            _ASSERTE(!"Writing process memory failed, perhaps due to an unexpected disconnection from the target.");
        }
        EX_END_CATCH(SwallowAllExceptions);
    }

    // Close and null out the various handles and events, including our process handle m_handle.
    CloseIPCHandles();

    m_cordb.Clear();

    // This process object is Dead-On-Arrival, so it doesn't really have anything to neuter.
    // But for safekeeping, we'll mark it as neutered.
    UnsafeNeuterDeadObject();
}


//---------------------------------------------------------------------------------------
//
// Attach to an existing process.
//
//
// Assumptions:
//    Called on W32Event Thread, in response to event sent by
//    code:CordbWin32EventThread::SendDebugActiveProcessEvent
//
// Notes:
//    Attach to a process. This is called in the context of the Win32
//    event thread to ensure that if we're Win32 debugging the process
//    that the same thread that waits for debugging events will be the
//    thread that attaches the process.
//
//    @dbgtodo shim: this will be part of the shim
//---------------------------------------------------------------------------------------
void CordbWin32EventThread::AttachProcess()
{
    _ASSERTE(IsWin32EventThread());

    RSUnsafeExternalSmartPtr<CordbProcess> pProcess;

    m_action = W32ETA_NONE;

    HRESULT hr = S_OK;

    ProcessDescriptor processDescriptor = m_actionData.attachData.processDescriptor;
    bool fNativeAttachSucceeded = false;

    // Always do OS attach to the target.
    // By this point, the pid should be valid (because OpenProcess above), pending some race where the process just exited.
    // The OS will enforce that only 1 debugger is attached.
    // Common failure paths here would be: access denied, double-attach
    {
        hr = m_pNativePipeline->DebugActiveProcess(m_actionData.attachData.machineInfo,
                                                   processDescriptor);
        if (FAILED(hr))
        {
            goto LExit;
        }
        fNativeAttachSucceeded = true;
    }


    hr = m_pShim->InitializeDataTarget(&processDescriptor);
    if (FAILED(hr))
    {
        goto LExit;
    }

    // To emulate V2 semantics, we pass 0 for the clrInstanceID into
    // OpenVirtualProcess. This will then connect to the first CLR
    // loaded.
    {
        const ULONG64 cFirstClrLoaded = 0;
        hr = CordbProcess::OpenVirtualProcess(cFirstClrLoaded, m_pShim->GetDataTarget(), NULL, m_cordb, &processDescriptor, m_pShim, &pProcess);
        if (FAILED(hr))
        {
            goto LExit;
        }
    }

    // Remember the process in the global list of processes.
    // The caller back in code:Cordb::DebugActiveProcess will then get this by fetching it from the list.

    EX_TRY
    {
        // Don't allow attach if any metadata/IL updates have been applied
        if (pProcess->GetDAC()->MetadataUpdatesApplied())
        {
            hr = CORDBG_E_ASSEMBLY_UPDATES_APPLIED;
            goto LExit;
        }

        // Mark interop-debugging
        if (m_actionData.attachData.IsInteropDebugging())
        {
            pProcess->EnableInteropDebugging(); // Throwing
        }

        m_cordb->AddProcess(pProcess); // will take ref if it succeeds


        // Queue fake Attach event for CreateProcess
        {
            PUBLIC_CALLBACK_IN_THIS_SCOPE0_NO_LOCK(pProcess);
            m_pShim->BeginQueueFakeAttachEvents();
        }
    }
    EX_CATCH_HRESULT(hr);
    if (FAILED(hr))
    {
        goto LExit;
    }

    _ASSERTE(m_pProcess == NULL);
    m_pProcess.Assign(pProcess);
    pProcess.Clear();     // ownership transferred to m_pProcess

    // Should have succeeded if we got to this point.
    _ASSERTE(SUCCEEDED(hr));


LExit:
    if (FAILED(hr))
    {
        // If we succeed to do a native-attach, but then failed elsewhere, try to native-detach.
        if (fNativeAttachSucceeded)
        {
            m_pNativePipeline->DebugActiveProcessStop(processDescriptor.m_Pid);
        }

        if (pProcess != NULL)
        {
            // Safe to call this even if the process wasn't added.
            m_cordb->RemoveProcess(pProcess);
            pProcess->CleanupHalfBakedLeftSide();
            pProcess.Clear();
        }
        m_pProcess.Clear();
    }

    //
    // Signal the hr to the caller.
    //
    m_actionResult = hr;
    SetEvent(m_actionTakenEvent);
}


// Note that the actual 'DetachProcess' method is really ExitProcess with CW32ET_UNKNOWN_PROCESS_SLOT ==
// processSlot
HRESULT CordbWin32EventThread::SendDetachProcessEvent(CordbProcess *pProcess)
{
    LOG((LF_CORDB, LL_INFO1000, "W32ET::SDPE\n"));
    HRESULT hr = S_OK;

    LockSendToWin32EventThreadMutex();

    m_actionData.detachData.pProcess = pProcess;

    // m_action is set last so that the win32 event thread can inspect it and take action without actually
    // having to take any locks. The lock around this here is simply to prevent multiple threads from making
    // requests at the same time.
    m_action = W32ETA_DETACH;

    BOOL succ = SetEvent(m_threadControlEvent);

    if (succ)
    {
        DWORD ret = WaitForSingleObject(m_actionTakenEvent, INFINITE);

        if (ret == WAIT_OBJECT_0)
            hr = m_actionResult;
        else
            hr = HRESULT_FROM_GetLastError();
    }
    else
        hr = HRESULT_FROM_GetLastError();

    UnlockSendToWin32EventThreadMutex();

    return hr;
}

#ifdef FEATURE_INTEROP_DEBUGGING
//
// Send a UnmanagedContinue event to the Win32 thread to have it
// continue from an unmanged debug event.
//
HRESULT CordbWin32EventThread::SendUnmanagedContinue(CordbProcess *pProcess,
                                                     EUMContinueType eContType)
{
    HRESULT hr = S_OK;

    // If this were being called on the win32 EventThread, we'd deadlock.
    _ASSERTE(!IsWin32EventThread());

    // This can't hold the process lock, b/c we're making a cross-thread call,
    // and our target will need the process lock.
    _ASSERTE(!pProcess->ThreadHoldsProcessLock());

    LockSendToWin32EventThreadMutex();

    m_actionData.continueData.process = pProcess;
    m_actionData.continueData.eContType = eContType;

    // m_action is set last so that the win32 event thread can inspect
    // it and take action without actually having to take any
    // locks. The lock around this here is simply to prevent multiple
    // threads from making requests at the same time.
    m_action = W32ETA_CONTINUE;

    BOOL succ = SetEvent(m_threadControlEvent);

    if (succ)
    {
        DWORD ret = WaitForSingleObject(m_actionTakenEvent, INFINITE);

        if (ret == WAIT_OBJECT_0)
            hr = m_actionResult;
        else
            hr = HRESULT_FROM_GetLastError();
    }
    else
        hr = HRESULT_FROM_GetLastError();

    UnlockSendToWin32EventThreadMutex();

    return hr;
}


//
// Handle unmanaged continue. Continue an unmanaged debug
// event. Deferes to UnmanagedContinue. This is called in the context
// of the Win32 event thread to ensure that if we're Win32 debugging
// the process that the same thread that waits for debugging events
// will be the thread that continues the process.
//
void CordbWin32EventThread::HandleUnmanagedContinue()
{
    _ASSERTE(IsWin32EventThread());

    m_action = W32ETA_NONE;
    HRESULT hr = S_OK;

    // Continue the process
    CordbProcess *pProcess = m_actionData.continueData.process;

    // If we lost the process object, we must have exited.
    if (m_pProcess != NULL)
    {
        _ASSERTE(m_pProcess != NULL);
        _ASSERTE(pProcess == m_pProcess);

        _ASSERTE(!pProcess->ThreadHoldsProcessLock());

        RSSmartPtr<CordbProcess> proc(pProcess);
        RSLockHolder ch(&pProcess->m_processMutex);

        hr = UnmanagedContinue(pProcess, m_actionData.continueData.eContType);
    }

    // Signal the hr to the caller.
    m_actionResult = hr;
    SetEvent(m_actionTakenEvent);
}

//
// Continue an unmanaged debug event. This is called in the context of the Win32 Event thread to ensure that the same
// thread that waits for debug events will be the thread that continues the process.
//
HRESULT CordbWin32EventThread::UnmanagedContinue(CordbProcess *pProcess,
                                                 EUMContinueType eContType)
{
    _ASSERTE(pProcess->ThreadHoldsProcessLock());
    _ASSERTE(IsWin32EventThread());
    _ASSERTE(m_pShim != NULL);

    HRESULT hr = S_OK;

    STRESS_LOG1(LF_CORDB, LL_INFO1000, "UM Continue. type=%d\n", eContType);

    if (eContType == cOobUMContinue)
    {
        _ASSERTE(pProcess->m_outOfBandEventQueue != NULL);

        // Dequeue the OOB event.
        CordbUnmanagedEvent *ue = pProcess->m_outOfBandEventQueue;
        CordbUnmanagedThread *ut = ue->m_owner;
        pProcess->DequeueOOBUnmanagedEvent(ut);

        // Do a little extra work if that was an OOB exception event...
        hr = ue->m_owner->FixupAfterOOBException(ue);
        _ASSERTE(SUCCEEDED(hr));

        // Continue from the event.
        DoDbgContinue(pProcess, ue);

        // If there are more queued OOB events, dispatch them now.
        if (pProcess->m_outOfBandEventQueue != NULL)
            pProcess->DispatchUnmanagedOOBEvent();

        // Note: if we previously skipped letting the entire process go on an IB continue due to a blocking OOB event,
        // and if the OOB event queue is now empty, then go ahead and let the process continue now...
        if ((pProcess->m_doRealContinueAfterOOBBlock == true) &&
            (pProcess->m_outOfBandEventQueue == NULL))
            goto doRealContinue;
    }
    else if (eContType == cInternalUMContinue)
    {
        // We're trying to get into a synced state which means we need the process running (potentially
        // with some threads hijacked) in order to have the helper thread do the sync.
        LOG((LF_CORDB, LL_INFO1000, "W32ET::UC: internal continue.\n"));

        if (!pProcess->GetSynchronized())
        {
            LOG((LF_CORDB, LL_INFO1000, "W32ET::UC: internal continue, !sync'd.\n"));
            pProcess->ResumeUnmanagedThreads();

            // the event we may need to hijack and continue;
            CordbUnmanagedEvent* pEvent = pProcess->m_lastQueuedUnmanagedEvent;

            // It is possible to be stopped at either an IB or an OOB event here. We only want to
            // continue from an IB event here though
            if(pProcess->m_state & CordbProcess::PS_WIN32_STOPPED && pEvent != NULL &&
                pEvent->IsEventWaitingForContinue())
            {
                LOG((LF_CORDB, LL_INFO1000, "W32ET::UC: internal continue, frozen on IB event.\n"));

                // There should be a uncontinued IB event at the head of the queue
                _ASSERTE(pEvent->IsIBEvent());
                _ASSERTE(!pEvent->IsEventContinuedUnhijacked());
                _ASSERTE(!pEvent->IsEventContinuedHijacked());

                // Ensure that the event is hijacked now (it may not have been before) so that the
                // thread does not slip forward during the sync process. After that we can safely continue
                // it.
                pProcess->HijackIBEvent(pEvent);
                m_pShim->GetWin32EventThread()->DoDbgContinue(pProcess, pEvent);
            }
        }

        LOG((LF_CORDB, LL_INFO1000, "W32ET::UC: internal continue, done.\n"));
    }
    else
    {
        // If we're here, then we know 100% for sure that we've successfully gotten the managed continue event to the
        // Left Side, so we can stop force hijacking left over in-band events now. Note: if we had hijacked any such
        // events, they'll be dispatched below since they're properly queued.
        pProcess->m_specialDeferment = false;

        // We don't actually do any work if there is an outstanding out-of-band event. When we do continue from the
        // out-of-band event, we'll do this work, too.
        if (pProcess->m_outOfBandEventQueue != NULL)
        {
            LOG((LF_CORDB, LL_INFO1000, "W32ET::UC: ignoring real continue due to block by out-of-band event(s).\n"));

            _ASSERTE(pProcess->m_doRealContinueAfterOOBBlock == false);
            pProcess->m_doRealContinueAfterOOBBlock = true;
        }
        else
        {
doRealContinue:
            // This is either the Frozen -> Running transition or a
            // Synced -> Running transition
            _ASSERTE(pProcess->m_outOfBandEventQueue == NULL);


            pProcess->m_doRealContinueAfterOOBBlock = false;

            LOG((LF_CORDB, LL_INFO1000, "W32ET::UC: continuing the process.\n"));
            // Dispatch any more queued in-band events, or if there are none then just continue the process.
            //
            // Note: don't dispatch more events if we've already sent up the ExitProcess event... those events are just
            // lost.
            if ((pProcess->HasUndispatchedNativeEvents()) && (pProcess->m_exiting == false))
            {
                pProcess->DispatchUnmanagedInBandEvent();
            }
            else
            {
                // If the unmanaged event queue is empty now, and the process is synchronized, and there are queued
                // managed events, then go ahead and get more managed events dispatched.
                //
                // Note: don't dispatch more events if we've already sent up the ExitProcess event... those events are
                // just lost.
                if (pProcess->GetSynchronized() && (!m_pShim->GetManagedEventQueue()->IsEmpty()) && (pProcess->m_exiting == false))
                {
                    if(pProcess->m_state & CordbProcess::PS_WIN32_STOPPED)
                    {
                        DoDbgContinue(pProcess, pProcess->m_lastDispatchedIBEvent);

                        // This if should not be necessary, I am just being extra careful because this
                        // fix is going in late - see issue 818301
                        _ASSERTE(pProcess->m_lastDispatchedIBEvent != NULL);
                        if(pProcess->m_lastDispatchedIBEvent != NULL)
                        {
                            pProcess->m_lastDispatchedIBEvent->m_owner->InternalRelease();
                        pProcess->m_lastDispatchedIBEvent = NULL;
                    }
                    }

                    // Now, get more managed events dispatched.
                    pProcess->SetSynchronized(false);
                    pProcess->MarkAllThreadsDirty();
                    m_cordb->ProcessStateChanged();
                }
                else
                {
                    // free all the hijacked threads that hit native debug events
                    pProcess->ResumeHijackedThreads();

                    // after continuing the here the process should be running completely
                    // free... no hijacks, no suspended threads, and of course not frozen
                    if(pProcess->m_state & CordbProcess::PS_WIN32_STOPPED)
                    {
                        DoDbgContinue(pProcess, pProcess->m_lastDispatchedIBEvent);
                        // This if should not be necessary, I am just being extra careful because this
                        // fix is going in late - see issue 818301
                        _ASSERTE(pProcess->m_lastDispatchedIBEvent != NULL);
                        if(pProcess->m_lastDispatchedIBEvent != NULL)
                        {
                            pProcess->m_lastDispatchedIBEvent->m_owner->InternalRelease();
                        pProcess->m_lastDispatchedIBEvent = NULL;
                    }
                }
            }
            }

            // Implicit Release on UT
        }
    }

    return hr;
}
#endif // FEATURE_INTEROP_DEBUGGING

void ExitProcessWorkItem::Do()
{
    STRESS_LOG1(LF_CORDB, LL_INFO1000, "ExitProcessWorkItem proc=%p\n", GetProcess());

    // This is being called on the RCET.
    // That's the thread that dispatches managed events. Since it's calling us now, we know
    // it can't be dispatching a managed event, and so we don't need to both waiting for it

    {
        // Get the SG lock here to coordinate against any other continues.
        RSLockHolder ch(GetProcess()->GetStopGoLock());
        RSLockHolder ch2(&(GetProcess()->m_processMutex));

        LOG((LF_CORDB, LL_INFO1000,"W32ET::EP: ExitProcess callback\n"));

        // We're synchronized now, so mark the process as such.
        GetProcess()->SetSynchronized(true);
        GetProcess()->IncStopCount();

        // By the time we release the SG + Process locks here, the process object has been
        // marked as exiting + terminated (by the w32et which queued us). Future attempts to
        // continue should fail, and thus we should remain synchronized.
    }


    //  Just to be safe, neuter any children before the exit process callback.
    {
        RSLockHolder ch(GetProcess()->GetProcessLock());

        // Release the process.
        GetProcess()->NeuterChildren();
    }

    RSSmartPtr<Cordb> pCordb(NULL);

    // There is a race condition here where the debuggee process is killed while we are processing a process
    // detach.  We queue the process exit event for the Win32 event thread before queueing the process detach
    // event.  By the time this function is executed, we may have neutered the CordbProcess already as a
    // result of code:CordbProcess::Detach.  Detect that case here under the SG lock.
    {
        RSLockHolder ch(GetProcess()->GetStopGoLock());
        if (!GetProcess()->IsNeutered())
        {
            _ASSERTE(GetProcess()->m_cordb != NULL);
            pCordb.Assign(GetProcess()->m_cordb);
        }
    }

    // Move this into Shim?

    // Invoke the ExitProcess callback. This is very important since the a shell
    // may rely on it for proper shutdown and may hang if they don't get it.
    // We don't expect Cordbg to continue from this (we're certainly not going to wait for it).
    if ((pCordb != NULL) && (pCordb->m_managedCallback != NULL))
    {
        PUBLIC_CALLBACK_IN_THIS_SCOPE0_NO_LOCK(GetProcess());
        pCordb->m_managedCallback->ExitProcess(GetProcess());
    }

    // This CordbProcess object now has no reservations against a client calling ICorDebug::Terminate.
    // That call may race against the CordbProcess::Neuter below, but since we already neutered the children,
    // that neuter call will not do anything interesting that will conflict with Terminate.

    LOG((LF_CORDB, LL_INFO1000,"W32ET::EP: returned from ExitProcess callback\n"));

    {
        RSLockHolder ch(GetProcess()->GetStopGoLock());

        // Release the process.
        GetProcess()->Neuter();
    }

    // Our dtor will release the Process object.
    // This may be the final release on the process.
}


//---------------------------------------------------------------------------------------
//
// Handles process exiting and detach cases
//
// Arguments:
//    fDetach - true if detaching, false if process is exiting.
//
// Return Value:
//    The type of the next argument in the signature,
//    normalized.
//
// Assumptions:
//    On exit, the process has already exited and we detected this by either an EXIT_PROCESS
//    native debug event, or by waiting on the process handle.
//    On detach, the process is stil live.
//
// Notes:
//    ExitProcess is called when a process exits or detaches.
//    This does our final cleanup and removes the process from our wait sets.
//    We're either here because we're detaching (fDetach == TRUE), or because the process has really exited,
//    and we're doing shutdown logic.
//
//---------------------------------------------------------------------------------------
void CordbWin32EventThread::ExitProcess(bool fDetach)
{
    INTERNAL_API_ENTRY(this);

    // Consider the following when you're modifying this function:
    // - The OS can kill the debuggee at any time.
    // - ExitProcess can race with detach.

    LOG((LF_CORDB, LL_INFO1000,"W32ET::EP: begin ExitProcess, detach=%d\n", fDetach));


    // For the Mac remote debugging transport, DebugActiveProcessStop() is a nop.  The transport will be
    // shut down later when we neuter the CordbProcess.
#if !defined(FEATURE_DBGIPC_TRANSPORT_DI)
    // @dbgtodo shim: this is a primitive workaround for interop-detach
    // Eventually, the Debugger owns the detach pipeline, so this won't be necessary.
    if (fDetach && (m_pProcess != NULL))
    {
        HRESULT hr = m_pNativePipeline->DebugActiveProcessStop(m_pProcess->GetProcessDescriptor()->m_Pid);

        // We don't expect detach to fail (we check earlier for common conditions that
        // may cause it to fail)
        SIMPLIFYING_ASSUMPTION(SUCCEEDED(hr));
        if( FAILED(hr) )
        {
            m_actionResult = hr;
            SetEvent(m_actionTakenEvent);
            return;
        }
    }
#endif // !FEATURE_DBGIPC_TRANSPORT_DI


    // We don't really care if we're on the Win32 thread or not here. We just want to be sure that
    // the LS Exit case and the Detach case both occur on the same thread. This makes it much easier
    // to assert that if we exit while detaching, EP is only called once.
    // If we ever decide to make the RCET listen on the LS process handle for EP(exit), then we should also
    // make the EP(detach) handled on the RCET (via DoFavor() ).
    _ASSERTE(IsWin32EventThread());

    // So either the Exit case or Detach case must happen first.
    // 1) If Detach first, then LS process is removed from wait set and so EP(Exit) will never happen
    //    because we check wait set after returning from EP(Detach).
    // 2) If Exit is first, m_pProcess gets set=NULL. EP(detach) will still get called, so explicitly check that.
    if (fDetach && ((m_pProcess == NULL) || m_pProcess->m_terminated))
    {
        // m_terminated is only set after the LS exits.
        // So the only way (fDetach && m_terminated) is true is if the LS exited while detaching. In that case
        // we already called EP(exit) and we don't want to call it again for EP(detach). So return here.
        LOG((LF_CORDB, LL_INFO1000,"W32ET::EP: In EP(detach), but EP(exit) already called. Early failure\n"));

        m_actionResult = CORDBG_E_PROCESS_TERMINATED;
        SetEvent(m_actionTakenEvent);

        return;
    }

    // We null m_pProcess at the end here, so
    // Only way we could get here w/ null process is if we're called twice. We can only be called
    // by detach or exit. Can't detach twice, can't exit twice, so must have been one of each.
    // If exit is first, we got removed from the wait set, so 2nd call must be detach and we'd catch
    // that above. If detach is first, we'd get removed from the wait set and so exit would never happen.
    _ASSERTE(m_pProcess != NULL);
    _ASSERTE(!m_pProcess->ThreadHoldsProcessLock());



    // Mark the process teminated. After this, the RCET will never call FlushQueuedEvents. It will
    // ignore all events it receives (including a SyncComplete) and the RCET also does not listen
    // to terminated processes (so ProcessStateChange() won't cause a FQE either).
    m_pProcess->Terminating(fDetach);

    // Take care of the race where the process exits right after the user calls Continue() from the last
    // managed event but before the handler has actually returned.
    //
    // Also, To get through this lock means that either:
    // 1. FlushQueuedEvents is not currently executing and no one will call FQE.
    // 2. FQE is exiting but is in the middle of a callback (so AreDispatchingEvent = true)
    //
    m_pProcess->Lock();

    m_pProcess->m_exiting = true;

    if (fDetach)
    {
        m_pProcess->SetSynchronized(false);
    }

    // If we are exiting, we *must* dispatch the ExitProcess callback, but we will delete all the events
    // in the queue and not bother dispatching anything else. If (and only if) we are currently dispatching
    // an event, then we will wait while that event is finished before invoking ExitProcess.
    // (Note that a dispatched event has already been removed from the queue)

    // Remove the process from the global list of processes.
    m_cordb->RemoveProcess(m_pProcess);

    if (fDetach)
    {
        // Signal the hr to the caller.
        LOG((LF_CORDB, LL_INFO1000,"W32ET::EP: Detach: send result back!\n"));

        m_actionResult = S_OK;
        SetEvent(m_actionTakenEvent);
    }

    m_pProcess->Unlock();

    // Delete all queued events
    m_pProcess->DeleteQueuedEvents();


    // If we're detaching, then the Detach already neutered everybody, so nothing here.
    // If we're exiting, then we still need to neuter things, but we can't do that on this thread,
    // so we queue it. We also need to dispatch an exit process callback. We'll queue that onto the RCET
    // and dispatch it inband w/the other callbacks.
    if (!fDetach)
    {
#ifdef TARGET_UNIX
        // Cleanup the transport pipe and semaphore files that might be left by the target (LS) process.
        m_pNativePipeline->CleanupTargetProcess();
#endif
        ExitProcessWorkItem * pItem = new (nothrow) ExitProcessWorkItem(m_pProcess);
        if (pItem != NULL)
        {
            m_cordb->m_rcEventThread->QueueAsyncWorkItem(pItem);
        }
    }

    // This will remove the process from our wait lists (so that we don't send multiple ExitProcess events).
    m_pProcess.Clear();
}


//
// Start actually creates and starts the thread.
//
HRESULT CordbWin32EventThread::Start()
{
    HRESULT hr = S_OK;
    if (m_threadControlEvent == NULL)
        return E_INVALIDARG;

    // Create the thread suspended to make sure that m_threadId is set
    // before CordbWin32EventThread::ThreadProc runs
    // Stack size = 0x80000 = 512KB
    m_thread = CreateThread(NULL, 0x80000, &CordbWin32EventThread::ThreadProc,
                            (LPVOID) this, CREATE_SUSPENDED | STACK_SIZE_PARAM_IS_A_RESERVATION, &m_threadId);

    if (m_thread == NULL)
        return HRESULT_FROM_GetLastError();

    DWORD succ = ResumeThread(m_thread);
    if (succ == (DWORD)-1)
        return HRESULT_FROM_GetLastError();
    return hr;
}


//
// Stop causes the thread to stop receiving events and exit. It
// waits for it to exit before returning.
//
HRESULT CordbWin32EventThread::Stop()
{
    HRESULT hr = S_OK;

    // m_pProcess may be NULL from CordbWin32EventThread::ExitProcess

    // Can't block on W32ET while holding the process-lock since the W32ET may need that to exit.
    // But since m_pProcess may be null, we can't enforce that.

    if (m_thread != NULL)
    {
        LockSendToWin32EventThreadMutex();
        m_action = W32ETA_NONE;
        m_run = FALSE;

        SetEvent(m_threadControlEvent);
        UnlockSendToWin32EventThreadMutex();

        DWORD ret = WaitForSingleObject(m_thread, INFINITE);

        if (ret != WAIT_OBJECT_0)
            hr = HRESULT_FROM_GetLastError();
    }

    m_pProcess.Clear();
    m_cordb.Clear();

    return hr;
}








// Allocate a buffer of cbBuffer bytes in the target.
//
// Arguments:
//     cbBuffer - count of bytes for the buffer.
//
// Returns:
//     a TargetBuffer describing the new memory region in the target.
//     Throws on error.
TargetBuffer CordbProcess::GetRemoteBuffer(ULONG cbBuffer)
{
    INTERNAL_SYNC_API_ENTRY(this); //

    // Create and initialize the event as synchronous
    DebuggerIPCEvent event;
    InitIPCEvent(&event,
                 DB_IPCE_GET_BUFFER,
                 true,
                 VMPTR_AppDomain::NullPtr());

    // Indicate the buffer size wanted
    event.GetBuffer.bufSize = cbBuffer;

    // Make the request, which is synchronous
    HRESULT hr = SendIPCEvent(&event, sizeof(event));
    IfFailThrow(hr);
    _ASSERTE(event.type == DB_IPCE_GET_BUFFER_RESULT);

    IfFailThrow(event.GetBufferResult.hr);

    // The request succeeded. Return the newly allocated range.
    return TargetBuffer(event.GetBufferResult.pBuffer, cbBuffer);
}

/*
 * This will release a previously allocated left side buffer.
 */
HRESULT CordbProcess::ReleaseRemoteBuffer(void **ppBuffer)
{
    INTERNAL_SYNC_API_ENTRY(this); //

    _ASSERTE(m_pShim != NULL);

    // Create and initialize the event as synchronous
    DebuggerIPCEvent event;
    InitIPCEvent(&event,
                 DB_IPCE_RELEASE_BUFFER,
                 true,
                 VMPTR_AppDomain::NullPtr());

    // Indicate the buffer to release
    event.ReleaseBuffer.pBuffer = (*ppBuffer);

    // Make the request, which is synchronous
    HRESULT hr = SendIPCEvent(&event, sizeof(event));
    TESTANDRETURNHR(hr);

    (*ppBuffer) = NULL;

    // Indicate success
    return event.ReleaseBufferResult.hr;
}

HRESULT CordbProcess::SetDesiredNGENCompilerFlags(DWORD dwFlags)
{
    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);

    return CORDBG_E_NGEN_NOT_SUPPORTED;

}

HRESULT CordbProcess::GetDesiredNGENCompilerFlags(DWORD *pdwFlags )
{
    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT(pdwFlags, DWORD*);
    *pdwFlags = 0;

    CordbProcess *pProcess = GetProcess();
    ATT_REQUIRE_STOPPED_MAY_FAIL(pProcess);
    HRESULT  hr = S_OK;
    EX_TRY
    {
        hr = pProcess->GetDAC()->GetNGENCompilerFlags(pdwFlags);
    }
    EX_CATCH_HRESULT(hr);
    return hr;
}

//-----------------------------------------------------------------------------
// Get an ICorDebugReference Value for the GC handle.
// handle - raw bits for the GC handle.
// pOutHandle
//-----------------------------------------------------------------------------
HRESULT CordbProcess::GetReferenceValueFromGCHandle(
    UINT_PTR gcHandle,
    ICorDebugReferenceValue **pOutValue)
{
    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    ATT_REQUIRE_STOPPED_MAY_FAIL(this);
    VALIDATE_POINTER_TO_OBJECT(pOutValue, ICorDebugReferenceValue*);

    *pOutValue = NULL;
    HRESULT hr = S_OK;

    EX_TRY
    {
        if (gcHandle == NULL)
        {
            ThrowHR(CORDBG_E_BAD_REFERENCE_VALUE);
        }

        IDacDbiInterface* pDAC = GetProcess()->GetDAC();
        VMPTR_OBJECTHANDLE vmObjHandle = pDAC->GetVmObjectHandle(gcHandle);
        if(!pDAC->IsVmObjectHandleValid(vmObjHandle))
        {
            ThrowHR(CORDBG_E_BAD_REFERENCE_VALUE);
        }
        ULONG appDomainId = pDAC->GetAppDomainIdFromVmObjectHandle(vmObjHandle);
        VMPTR_AppDomain vmAppDomain = pDAC->GetAppDomainFromId(appDomainId);

        RSLockHolder lockHolder(GetProcessLock());
        CordbAppDomain * pAppDomain = LookupOrCreateAppDomain(vmAppDomain);
        lockHolder.Release();

        // Now that we finally have the AppDomain, we can go ahead and get a ReferenceValue
        // from the ObjectHandle.
        hr = CordbReferenceValue::BuildFromGCHandle(pAppDomain, vmObjHandle, pOutValue);
        _ASSERTE(SUCCEEDED(hr) == (*pOutValue != NULL));
        IfFailThrow(hr);
    }
    EX_CATCH_HRESULT(hr);
    return hr;
}

//-----------------------------------------------------------------------------
// Return count of outstanding GC handles held by CordbHandleValue objects
LONG CordbProcess::OutstandingHandles()
{
    return m_cOutstandingHandles;
}

//-----------------------------------------------------------------------------
// Increment the outstanding handle count for code:CordbProcess::OutstandingHandles
// This is the inverse of code:CordbProcess::DecrementOutstandingHandles
void CordbProcess::IncrementOutstandingHandles()
{
    _ASSERTE(ThreadHoldsProcessLock());
    m_cOutstandingHandles++;
}

//-----------------------------------------------------------------------------
// Decrement the outstanding handle count for code:CordbProcess::OutstandingHandles
// This is the inverse of code:CordbProcess::IncrementOutstandingHandles
void CordbProcess::DecrementOutstandingHandles()
{
    _ASSERTE(ThreadHoldsProcessLock());
    m_cOutstandingHandles--;
}


/*
 * IsReadyForDetach
 *
 * This method encapsulates all logic for deciding if it is ok for a debugger to
 * detach from the process at this time.
 *
 * Parameters: None.
 *
 * Returns: S_OK if it is ok to detach, else a specific HRESULT describing why it
 *   is not ok to detach.
 *
 */
HRESULT CordbProcess::IsReadyForDetach()
{
    INTERNAL_API_ENTRY(this);

    // Always safe to detach in V3 case.
    if (m_pShim == NULL)
    {
        return S_OK;
    }

    // If not initialized yet, then there are no detach liabilities.
    if (!m_initialized)
    {
        return S_OK;
    }

    RSLockHolder lockHolder(&this->m_processMutex);

    //
    // If there are any outstanding func-evals then fail the detach.
    //
    if (OutstandingEvalCount() != 0)
    {
        return CORDBG_E_DETACH_FAILED_OUTSTANDING_EVALS;
    }

    // V2 didn't check outstanding handles (code:CordbProcess::OutstandingHandles)
    // because it could automatically clean those up on detach.

    //
    // If there are any outstanding steppers then fail the detach.
    //
    if (m_steppers.IsInitialized() && (m_steppers.GetCount() > 0))
    {
        return CORDBG_E_DETACH_FAILED_OUTSTANDING_STEPPERS;
    }

    //
    // If there are any outstanding breakpoints then fail the detach.
    //
    HASHFIND foundAppDomain;
    CordbAppDomain *pAppDomain = m_appDomains.FindFirst(&foundAppDomain);

    while (pAppDomain != NULL)
    {
        if (pAppDomain->m_breakpoints.IsInitialized() && (pAppDomain->m_breakpoints.GetCount() > 0))
        {
            return CORDBG_E_DETACH_FAILED_OUTSTANDING_BREAKPOINTS;
        }

        // Check for any outstanding EnC modules.
        HASHFIND foundModule;
        CordbModule * pModule = pAppDomain->m_modules.FindFirst(&foundModule);
        while (pModule != NULL)
        {
            if (pModule->m_EnCCount > 0)
            {
                return CORDBG_E_DETACH_FAILED_ON_ENC;
            }
            pModule = pAppDomain->m_modules.FindNext(&foundModule);
        }


        pAppDomain = m_appDomains.FindNext(&foundAppDomain);
    }

    return S_OK;
}


/*
 * Look for any thread which was last seen in the specified AppDomain.
 * The CordbAppDomain object is about to be neutered due to an AD Unload
 * So the thread must no longer be considered to be in that domain.
 * Note that this is a workaround due to the existence of the (possibly incorrect)
 * cached AppDomain value.  Ideally we would remove the cached value entirely
 * and there would be no need for this.
 *
 * @dbgtodo: , appdomain: We should remove CordbThread::m_pAppDomain in the V3 architecture.
 * If we need the thread's current domain, we should get it accurately with DAC.
 */
void CordbProcess::UpdateThreadsForAdUnload(CordbAppDomain * pAppDomain)
{
    INTERNAL_API_ENTRY(this);

    // If we're doing an AD unload then we should have already seen the ATTACH
    // notification for the default domain.
    //_ASSERTE( m_pDefaultAppDomain != NULL );
    // @dbgtodo appdomain: fix Default domain invariants with DAC-izing Appdomain work.

    RSLockHolder lockHolder(GetProcessLock());

    CordbThread* t;
    HASHFIND find;

    // We don't need to prepopulate here (to collect LS state) because we're just updating RS state.
    for (t =  m_userThreads.FindFirst(&find);
         t != NULL;
         t =  m_userThreads.FindNext(&find))
    {
        if( t->GetAppDomain() == pAppDomain )
        {
            // This thread cannot actually be in this AppDomain anymore (since it's being
            // unloaded).  Reset it to point to the default AppDomain
            t->m_pAppDomain = m_pDefaultAppDomain;
        }
    }
}

// CordbProcess::LookupClass
// Looks up a previously constructed CordbClass instance without creating. May return NULL if the
// CordbClass instance doesn't exist.
// Argument: (in) vmDomainAssembly - pointer to the domain assembly for the module
//           (in) mdTypeDef    - metadata token for the class
// Return value: pointer to a previously created CordbClass instance or NULL in none exists
CordbClass * CordbProcess::LookupClass(ICorDebugAppDomain * pAppDomain, VMPTR_DomainAssembly vmDomainAssembly, mdTypeDef classToken)
{
    _ASSERTE(ThreadHoldsProcessLock());

    if (pAppDomain != NULL)
    {
        CordbModule * pModule = ((CordbAppDomain *)pAppDomain)->m_modules.GetBase(VmPtrToCookie(vmDomainAssembly));
        if (pModule != NULL)
        {
            return pModule->LookupClass(classToken);
        }
    }
    return NULL;
} // CordbProcess::LookupClass

//---------------------------------------------------------------------------------------
// Look for a specific module in the process.
//
// Arguments:
//    vmDomainAssembly - non-null module to lookup
//
// Returns:
//    a CordbModule object for the given cookie. Object may be from the cache, or created
//    lazily.
//    Never returns null.  Throws on error.
//
// Notes:
//    A VMPTR_DomainAssembly has appdomain affinity, but is ultimately scoped to a process.
//    So if we get a raw VMPTR_DomainAssembly (eg, from the stackwalker or from some other
//    lookup function), then we need to do a process wide lookup since we don't know which
//    appdomain it's in. If you know the appdomain, you can use code:CordbAppDomain::LookupOrCreateModule.
//
CordbModule * CordbProcess::LookupOrCreateModule(VMPTR_DomainAssembly vmDomainAssembly)
{
    INTERNAL_API_ENTRY(this);

    RSLockHolder lockHolder(GetProcess()->GetProcessLock());
    _ASSERTE(!vmDomainAssembly.IsNull());

    DomainAssemblyInfo data;
    GetDAC()->GetDomainAssemblyData(vmDomainAssembly, &data); // throws

    CordbAppDomain * pAppDomain = LookupOrCreateAppDomain(data.vmAppDomain);
    return pAppDomain->LookupOrCreateModule(vmDomainAssembly);
}

//---------------------------------------------------------------------------------------
// Determine if the process has any in-band queued events which have not been dispatched
//
// Returns:
//    TRUE iff there are undispatched IB events
//
#ifdef FEATURE_INTEROP_DEBUGGING
BOOL CordbProcess::HasUndispatchedNativeEvents()
{
    INTERNAL_API_ENTRY(this);

    CordbUnmanagedEvent* pEvent = m_unmanagedEventQueue;
    while(pEvent != NULL && pEvent->IsDispatched())
    {
        pEvent = pEvent->m_next;
    }

    return pEvent != NULL;
}
#endif

//---------------------------------------------------------------------------------------
// Determine if the process has any in-band queued events which have not been user continued
//
// Returns:
//    TRUE iff there are user uncontinued IB events
//
#ifdef FEATURE_INTEROP_DEBUGGING
BOOL CordbProcess::HasUserUncontinuedNativeEvents()
{
    INTERNAL_API_ENTRY(this);

    CordbUnmanagedEvent* pEvent = m_unmanagedEventQueue;
    while(pEvent != NULL && pEvent->IsEventUserContinued())
    {
        pEvent = pEvent->m_next;
    }

    return pEvent != NULL;
}
#endif

//---------------------------------------------------------------------------------------
// Hijack the thread which had this event. If the thread is already hijacked this method
// has no effect.
//
// Arguments:
//    pUnmanagedEvent - the debug event which requires us to hijack
//
// Returns:
//    S_OK on success, failing HRESULT if the hijack could not be set up
//
#ifdef FEATURE_INTEROP_DEBUGGING
HRESULT CordbProcess::HijackIBEvent(CordbUnmanagedEvent * pUnmanagedEvent)
{
    // Can't hijack after the event has already been continued hijacked
    _ASSERTE(!pUnmanagedEvent->IsEventContinuedHijacked());
    // Can only hijack IB events
    _ASSERTE(pUnmanagedEvent->IsIBEvent());

    // If we already hijacked the event then there is nothing left to do
    if(pUnmanagedEvent->m_owner->IsFirstChanceHijacked() ||
        pUnmanagedEvent->m_owner->IsGenericHijacked())
    {
        return S_OK;
    }

    ResetEvent(this->m_leftSideUnmanagedWaitEvent);
    if (pUnmanagedEvent->m_currentDebugEvent.u.Exception.dwFirstChance)
    {
        HRESULT hr = pUnmanagedEvent->m_owner->SetupFirstChanceHijackForSync();
        SIMPLIFYING_ASSUMPTION(SUCCEEDED(hr));
        return hr;
    }
    else // Second chance exceptions must be generic hijacked.
    {
        HRESULT hr = pUnmanagedEvent->m_owner->SetupGenericHijack(pUnmanagedEvent->m_currentDebugEvent.dwDebugEventCode, &pUnmanagedEvent->m_currentDebugEvent.u.Exception.ExceptionRecord);
        SIMPLIFYING_ASSUMPTION(SUCCEEDED(hr));
        return hr;
    }
}
#endif

// Sets a bitfield reflecting the managed debugging state at the time of
// the jit attach.
HRESULT CordbProcess::GetAttachStateFlags(CLR_DEBUGGING_PROCESS_FLAGS *pFlags)
{
    HRESULT hr = S_OK;
    PUBLIC_REENTRANT_API_BEGIN(this)
    {
        if(pFlags == NULL)
            hr = E_POINTER;
        else
            *pFlags = GetDAC()->GetAttachStateFlags();
    }
    PUBLIC_API_END(hr);

    return hr;
}

// Determine if this version of ICorDebug is compatibile with the ICorDebug in the specified major CLR version
bool CordbProcess::IsCompatibleWith(DWORD clrMajorVersion)
{
    // The debugger versioning policy is that debuggers generally need to opt-in to supporting major new
    // versions of the CLR.  Often new versions of the CLR violate some invariant that previous debuggers assume
    // (eg. hot/cold splitting in Whidbey, multiple CLRs in a process in CLR v4), and neither VS or the CLR
    // teams generally want the support burden of forward compatibility.

    //
    // If this assert is firing for you, its probably because the major version
    // number of the clr.dll has changed. This assert is here to remind you to do a bit of other
    // work you may not have realized you needed to do so that our versioning works smoothly
    // for debugging. You probably want to contact the CLR DST team if you are a
    // non-debugger person hitting this. DON'T JUST DELETE THIS ASSERT!!!
    //
    // 1) You should ensure new versions of all ICorDebug users in DevDiv (VS Debugger, MDbg, etc.)
    //    are using a creation path that explicitly specifies that they support this new major
    //    version of the CLR.
    // 2) You should file an issue to track blocking earlier debuggers from targeting this
    //    version of the CLR (i.e. update requiredVersion to the new CLR major
    //    version).  To enable a smooth internal transition, this often isn't done until absolutely
    //    necessary (sometimes as late as Beta2).
    // 3) You can consider updating the CLR_ID guid used by the shim to recognize a CLR, but only
    //    if it's important to completely hide newer CLRs from the shim.  The expectation now
    //    is that we won't need to do this (i.e. we'd like VS to give a nice error message about
    //    needed a newer version of the debugger, rather than just acting as if a process has no CLR).
    // 4) Update this assert so that it no longer fires for your new CLR version or any of
    //    the previous versions, but don't delete the assert...
    //    the next CLR version after yours will probably need the same reminder

    _ASSERTE_MSG(clrMajorVersion <= 4,
        "Found major CLR version greater than 4 in mscordbi.dll from CLRv4 - contact CLRDST");

    // This knob lets us enable forward compatibility for internal scenarios, and also simulate new
    // versions of the runtime for testing the failure user-experience in a version of the debugger
    // before it is shipped.
    // We don't want to risk customers getting this, so for RTM builds this must be CHK-only.
    // To aid in internal transition, we may temporarily enable this in RET builds, but when
    // doing so must file a bug to track making it CHK only again before RTM.
    // For example, Dev10 Beta2 shipped with this knob, but it was made CHK-only at the start of RC.
    // In theory we might have a point release someday where we break debugger compat, but
    // it seems unlikely and since this knob is unsupported anyway we can always extend it
    // then (support reading a string value, etc.).  So for now we just map the number
    // to the major CLR version number.
    DWORD requiredVersion = 0;
#ifdef _DEBUG
    requiredVersion = CLRConfig::GetConfigValue(CLRConfig::UNSUPPORTED_Debugging_RequiredVersion);
#endif

    // If unset (the only supported configuration), then we require a debugger designed for CLRv4
    //  for desktop, where we do not allow forward compat.
    // For SL, we allow forward compat.  Right now, that means SLv2+ debugger requests can be
    //  honored for SLv4.
    if (requiredVersion <= 0)
    {
        requiredVersion = 2;
    }

    // Compare the version we were created for against the minimum required
    return (clrMajorVersion >= requiredVersion);
}

bool CordbProcess::IsThreadSuspendedOrHijacked(ICorDebugThread * pICorDebugThread)
{
    // An RS lock can be held while this is called. Specifically,
    // CordbThread::EnumerateChains may be on the stack, and it uses
    // ATT_REQUIRE_STOPPED_MAY_FAIL, which holds the CordbProcess::m_StopGoLock lock for
    // its entire duration. As a result, this needs to be considered a reentrant API. See
    // comments above code:PrivateShimCallbackHolder for more info.
    PUBLIC_REENTRANT_API_ENTRY_FOR_SHIM(this);

    CordbThread * pCordbThread = static_cast<CordbThread *> (pICorDebugThread);
    return GetDAC()->IsThreadSuspendedOrHijacked(pCordbThread->m_vmThreadToken);
}

void CordbProcess::HandleControlCTrapResult(HRESULT result)
{
    RSLockHolder ch(GetStopGoLock());

    DebuggerIPCEvent eventControlCResult;

    InitIPCEvent(&eventControlCResult,
        DB_IPCE_CONTROL_C_EVENT_RESULT,
        false,
        VMPTR_AppDomain::NullPtr());

    // Indicate whether the debugger has handled the event.
    eventControlCResult.hr = result;

    // Send the reply to the LS.
    SendIPCEvent(&eventControlCResult, sizeof(eventControlCResult));
}
