// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//*****************************************************************************
// File: module.cpp
//

//
//*****************************************************************************
#include "stdafx.h"
#include "winbase.h"

#include "metadataexports.h"

#include "winbase.h"
#include "corpriv.h"
#include "corsym.h"

#include "pedecoder.h"
#include "stgpool.h"

//---------------------------------------------------------------------------------------
// Update an existing metadata importer with a buffer
//
// Arguments:
//     pUnk - IUnknoown of importer to update.
//     pData - local buffer containing new metadata
//     cbData - size of buffer in bytes.
//     dwReOpenFlags - metadata flags to pass for reopening.
//
// Returns:
//     S_OK on success. Else failure.
//
// Notes:
//    This will call code:MDReOpenMetaDataWithMemoryEx from the metadata engine.
STDAPI ReOpenMetaDataWithMemoryEx(
    void        *pUnk,
    LPCVOID     pData,
    ULONG       cbData,
    DWORD       dwReOpenFlags)
{
    HRESULT hr = MDReOpenMetaDataWithMemoryEx(pUnk,pData, cbData, dwReOpenFlags);
    return hr;
}

//---------------------------------------------------------------------------------------
// Initialize a new CordbModule around a Module in the target.
//
// Arguments:
//    pProcess - process that this module lives in
//    vmDomainFile - CLR cookie for module.
CordbModule::CordbModule(
    CordbProcess *     pProcess,
    VMPTR_Module        vmModule,
    VMPTR_DomainFile    vmDomainFile)
: CordbBase(pProcess, vmDomainFile.IsNull() ? VmPtrToCookie(vmModule) : VmPtrToCookie(vmDomainFile), enumCordbModule),
    m_pAssembly(0),
    m_pAppDomain(0),
    m_classes(11),
    m_functions(101),
    m_vmDomainFile(vmDomainFile),
    m_vmModule(vmModule),
    m_EnCCount(0),
    m_fForceMetaDataSerialize(FALSE),
    m_nativeCodeTable(101)
{
    _ASSERTE(pProcess->GetProcessLock()->HasLock());

    _ASSERTE(!vmModule.IsNull());

    m_nLoadEventContinueCounter = 0;
#ifdef _DEBUG
    m_classes.DebugSetRSLock(pProcess->GetProcessLock());
    m_functions.DebugSetRSLock(pProcess->GetProcessLock());
#endif

    // Fill out properties via DAC.
    ModuleInfo modInfo;
    pProcess->GetDAC()->GetModuleData(vmModule, &modInfo); // throws

    m_PEBuffer.Init(modInfo.pPEBaseAddress, modInfo.nPESize);

    m_fDynamic  = modInfo.fIsDynamic;
    m_fInMemory = modInfo.fInMemory;
    m_vmPEFile = modInfo.vmPEFile;

    if (!vmDomainFile.IsNull())
    {
        DomainFileInfo dfInfo;

        pProcess->GetDAC()->GetDomainFileData(vmDomainFile, &dfInfo); // throws

        m_pAppDomain = pProcess->LookupOrCreateAppDomain(dfInfo.vmAppDomain);
        m_pAssembly  = m_pAppDomain->LookupOrCreateAssembly(dfInfo.vmDomainAssembly);
    }
    else
    {
        // Not yet implemented
        m_pAppDomain = pProcess->GetSharedAppDomain();
        m_pAssembly = m_pAppDomain->LookupOrCreateAssembly(modInfo.vmAssembly);
    }
#ifdef _DEBUG
    m_nativeCodeTable.DebugSetRSLock(GetProcess()->GetProcessLock());
#endif

    // MetaData is initialized lazily (via code:CordbModule::GetMetaDataImporter).
    // Getting the metadata may be very expensive (especially if we go through the metadata locator, which
    // invokes back to the data-target), so don't do it until asked.
    // m_pIMImport, m_pInternalMetaDataImport are smart pointers that already initialize to NULL.
}


#ifdef _DEBUG
//---------------------------------------------------------------------------------------
// Callback helper for code:CordbModule::DbgAssertModuleDeleted
//
// Arguments
//    vmDomainFile - domain file in the enumeration
//    pUserData - pointer to the CordbModule that we just got an exit event for.
//
void DbgAssertModuleDeletedCallback(VMPTR_DomainFile vmDomainFile, void * pUserData)
{
    CordbModule * pThis = reinterpret_cast<CordbModule *>(pUserData);
    INTERNAL_DAC_CALLBACK(pThis->GetProcess());

    if (!pThis->m_vmDomainFile.IsNull())
    {
        VMPTR_DomainFile vmDomainFileDeleted = pThis->m_vmDomainFile;

        CONSISTENCY_CHECK_MSGF((vmDomainFileDeleted != vmDomainFile),
            ("A Module Unload event was sent for a module, but it still shows up in the enumeration.\n vmDomainFileDeleted=%p\n",
            VmPtrToCookie(vmDomainFileDeleted)));
    }
}

//---------------------------------------------------------------------------------------
// Assert that a module is no longer discoverable via enumeration.
//
// Notes:
//   See code:IDacDbiInterface#Enumeration for rules that we're asserting.
//   This is a debug only method. It's conceptually similar to
//   code:CordbProcess::DbgAssertAppDomainDeleted.
//
void CordbModule::DbgAssertModuleDeleted()
{
    GetProcess()->GetDAC()->EnumerateModulesInAssembly(
        m_pAssembly->GetDomainAssemblyPtr(),
        DbgAssertModuleDeletedCallback,
        this);
}
#endif // _DEBUG

CordbModule::~CordbModule()
{
    // We should have been explicitly neutered before our internal ref went to 0.
    _ASSERTE(IsNeutered());

    _ASSERTE(m_pIMImport == NULL);
}

// Neutered by CordbAppDomain
void CordbModule::Neuter()
{
    // m_pAppDomain, m_pAssembly assigned w/o AddRef()
    m_classes.NeuterAndClear(GetProcess()->GetProcessLock());
    m_functions.NeuterAndClear(GetProcess()->GetProcessLock());

    m_nativeCodeTable.NeuterAndClear(GetProcess()->GetProcessLock());
    m_pClass.Clear();

    // This is very important because it also releases the metadata's potential file locks.
    m_pInternalMetaDataImport.Clear();
    m_pIMImport.Clear();

    CordbBase::Neuter();
}

//
// Creates an IStream based off the memory described by the TargetBuffer.
//
// Arguments:
//   pProcess - process that buffer is valid in.
//   buffer - memory range in target
//   ppStream - out parameter to receive the new stream. *ppStream == NULL on input.
//      caller owns the new object and must call Release.
//
// Returns:
//    Throws on error.
//    Common errors include if memory is missing in the target.
//
// Notes:
//   This will copy the memory over from the TargetBuffer, and then create a new IStream
//   object around it.
//
void GetStreamFromTargetBuffer(CordbProcess * pProcess, TargetBuffer buffer, IStream ** ppStream)
{
    CONTRACTL
    {
        THROWS;
    }
    CONTRACTL_END;

    _ASSERTE(ppStream != NULL);
    _ASSERTE(*ppStream == NULL);

    int cbSize = buffer.cbSize;
    NewArrayHolder<BYTE> localBuffer(new BYTE[cbSize]);

    pProcess->SafeReadBuffer(buffer, localBuffer);

    HRESULT hr = E_FAIL;
    hr = CInMemoryStream::CreateStreamOnMemoryCopy(localBuffer, cbSize, ppStream);
    IfFailThrow(hr);
    _ASSERTE(*ppStream != NULL);
}

//
// Helper API to get in-memory symbols from the target into a host stream object.
//
// Arguments:
//   ppStream - out parameter to receive the new stream. *ppStream == NULL on input.
//      caller owns the new object and must call Release.
//
// Returns:
//   kSymbolFormatNone if no PDB stream is present. This is a common case for
//     file-based modules, and also for dynamic modules that just aren't tracking
//     debug information.
//   The format of the symbols stored into ppStream. This is common:
//      - Ref.Emit modules if the debuggee generated debug symbols,
//      - in-memory modules (such as Load(Byte[], Byte[])
//      - hosted modules.
//   Throws on error
//
IDacDbiInterface::SymbolFormat CordbModule::GetInMemorySymbolStream(IStream ** ppStream)
{
    // @dbgtodo : add a PUBLIC_REENTRANT_API_ENTRY_FOR_SHIM contract
    // This function is mainly called internally in dbi, and also by the shim to emulate the
    // UpdateModuleSymbols callback on attach.

    CONTRACTL
    {
        THROWS;
    }
    CONTRACTL_END;

    _ASSERTE(ppStream != NULL);
    _ASSERTE(*ppStream == NULL);
    *ppStream = NULL;

    TargetBuffer bufferPdb;
    IDacDbiInterface::SymbolFormat symFormat;
    GetProcess()->GetDAC()->GetSymbolsBuffer(m_vmModule, &bufferPdb, &symFormat);
    if (bufferPdb.IsEmpty())
    {
        // No in-memory PDB. Common case.
        _ASSERTE(symFormat == IDacDbiInterface::kSymbolFormatNone);
        return IDacDbiInterface::kSymbolFormatNone;
    }
    else
    {
        _ASSERTE(symFormat != IDacDbiInterface::kSymbolFormatNone);
        GetStreamFromTargetBuffer(GetProcess(), bufferPdb, ppStream);
        return symFormat;
    }
}

//---------------------------------------------------------------------------------------
// Accessor for PE file.
//
// Returns:
//    VMPTR_PEFile for this module. Should always be non-null
//
// Notes:
//    A main usage of this is to find the proper internal MetaData importer.
//    DACized code needs to map from PEFile --> IMDInternalImport.
//
VMPTR_PEFile CordbModule::GetPEFile()
{
    return m_vmPEFile;
}

//---------------------------------------------------------------------------------------
//
// Top-level getter for the public metadata importer for this module
//
// Returns:
//     metadata importer.
//     Never returns NULL. Will throw some hr (likely CORDBG_E_MISSING_METADATA) instead.
//
// Notes:
//     This will lazily create the metadata, possibly invoking back into the data-target.
IMetaDataImport * CordbModule::GetMetaDataImporter()
{
    CONTRACTL
    {
        THROWS;
    }
    CONTRACTL_END;


    // If we already have it, then we're done.
    // This is critical to do at the top of this function to avoid potential recursion.
    if (m_pIMImport != NULL)
    {
        return m_pIMImport;
    }

    // Lazily initialize


    // Fetch metadata from target
    LOG((LF_CORDB,LL_INFO1000, "CM::GMI Lazy init refreshing metadata\n"));

    ALLOW_DATATARGET_MISSING_MEMORY(
        RefreshMetaData();
    );

    // If lookup failed from the Module & target memory, try the metadata locator interface
    // from debugger, if we have one.
    if (m_pIMImport == NULL)
    {
        bool isILMetaDataForNGENImage;  // Not currently used for anything.

        // The process's LookupMetaData will ping the debugger's ICorDebugMetaDataLocator iface.
        CordbProcess * pProcess = GetProcess();
        RSLockHolder processLockHolder(pProcess->GetProcessLock());
        m_pInternalMetaDataImport.Clear();

        // Do not call code:CordbProcess::LookupMetaData from this function.  It will try to load
        // through the CordbModule again which will end up back here, and on failure you'll fill the stack.
        // Since we've already done everything possible from the Module anyhow, just call the
        // stuff that talks to the debugger.
        // Don't do anything with the ptr returned here, since it's really m_pInternalMetaDataImport.
        pProcess->LookupMetaDataFromDebugger(m_vmPEFile, isILMetaDataForNGENImage, this);
    }

    // If we still can't get it, throw.
    if (m_pIMImport == NULL)
    {
        ThrowHR(CORDBG_E_MISSING_METADATA);
    }

    return m_pIMImport;
}

// Refresh the metadata cache if a profiler added new rows.
//
// Arguments:
//    token - token that we want to ensure is in the metadata cache.
//
// Notes:
//    In profiler case, this may be referred to new rows and we may need to update the metadata
//    This only supports StandAloneSigs.
//
void CordbModule::UpdateMetaDataCacheIfNeeded(mdToken token)
{
    CONTRACTL
    {
        THROWS;
    }
    CONTRACTL_END;

    LOG((LF_CORDB,LL_INFO10000, "CM::UMCIN token=0x%x\n", token));

    // If we aren't trying to keep parity with our legacy profiler metadata update behavior
    // then we should avoid this temporary update mechanism entirely
    if(GetProcess()->GetWriteableMetadataUpdateMode() != LegacyCompatPolicy)
    {
        return;
    }

    //
    // 1) Check if in-range? Compare against tables, etc.
    //
    if(CheckIfTokenInMetaData(token))
    {
        LOG((LF_CORDB,LL_INFO10000, "CM::UMCIN token was present\n"));
        return;
    }

    //
    // 2) Copy over new MetaData. From now on we assume that the profiler is
    //    modifying module metadata and that we need to serialize in process
    //    at each refresh
    //
    LOG((LF_CORDB,LL_INFO10000, "CM::UMCIN token was not present, refreshing\n"));
    m_fForceMetaDataSerialize = TRUE;
    RefreshMetaData();

    // If we are dump debugging, we may still not have it. Nothing to be done.
}

// Returns TRUE if the token is present, FALSE if not.
BOOL CordbModule::CheckIfTokenInMetaData(mdToken token)
{
    CONTRACTL
    {
        THROWS;
    }
    CONTRACTL_END;
    LOG((LF_CORDB,LL_INFO10000, "CM::CITIM token=0x%x\n", token));
    _ASSERTE(TypeFromToken(token) == mdtSignature);
    RSExtSmartPtr<IMetaDataTables> pTable;

    HRESULT hr = GetMetaDataImporter()->QueryInterface(IID_IMetaDataTables, (void**) &pTable);

    _ASSERTE(SUCCEEDED(hr));
    if (FAILED(hr))
    {
        ThrowHR(hr);
    }

    ULONG cbRowsAvailable; // number of rows in the table

    hr = pTable->GetTableInfo(
        mdtSignature >> 24,                      // [IN] Which table.
        NULL,                    // [OUT] Size of a row, bytes.
        &cbRowsAvailable,                    // [OUT] Number of rows.
        NULL,                    // [OUT] Number of columns in each row.
        NULL,                     // [OUT] Key column, or -1 if none.
        NULL);          // [OUT] Name of the table.

    _ASSERTE(SUCCEEDED(hr));
    if (FAILED(hr))
    {
        ThrowHR(hr);
    }


    // Rows start counting with number 1.
    ULONG rowRequested = RidFromToken(token);
    LOG((LF_CORDB,LL_INFO10000, "CM::UMCIN requested=0x%x available=0x%x\n", rowRequested, cbRowsAvailable));
    return (rowRequested <= cbRowsAvailable);
}

// This helper class ensures the remote serailzied buffer gets deleted in the RefreshMetaData
// function below
class CleanupRemoteBuffer
{
public:
    CordbProcess* pProcess;
    CordbModule* pModule;
    TargetBuffer bufferMetaData;
    BOOL fDoCleanup;

    CleanupRemoteBuffer() :
    fDoCleanup(FALSE) { }

    ~CleanupRemoteBuffer()
    {
        if(fDoCleanup)
        {
            //
            // Send 2nd event to free buffer.
            //
            DebuggerIPCEvent event;
            pProcess->InitIPCEvent(&event,
                DB_IPCE_RESOLVE_UPDATE_METADATA_2,
                true,
                pModule->GetAppDomain()->GetADToken());

            event.MetadataUpdateRequest.pMetadataStart = CORDB_ADDRESS_TO_PTR(bufferMetaData.pAddress);

            // Note: two-way event here...
            IfFailThrow(pProcess->SendIPCEvent(&event, sizeof(DebuggerIPCEvent)));
            _ASSERTE(event.type == DB_IPCE_RESOLVE_UPDATE_METADATA_2_RESULT);
        }
    }

};

// Called to refetch metadata. This occurs when a dynamic module grows or the profiler
// has edited the metadata
void CordbModule::RefreshMetaData()
{
    CONTRACTL
    {
        THROWS;
    }
    CONTRACTL_END;

    LOG((LF_CORDB,LL_INFO1000, "CM::RM\n"));

    // There are several different ways we can get the metadata
    // 1) [Most common] Module is loaded into VM and never changed. The importer
    //    will be constructed refering to the file on disk. This is a significant
    //    working set win because the VM and debugger share the image. If there is
    //    an error reading by file we can fall back to case #2 for these modules
    // 2) Most modules have a buffer in target memory that represents their
    //    metadata. We copy that data over the RS and construct an in-memory
    //    importer on top of it.
    // 3) The only modules that don't have a suitable buffer (case #2) are those
    //    modified in memory via the profiling API (or ENC). A message can be sent from
    //    the debugger to the debuggee instructing it to allocate a buffer and
    //    serialize the metadata into it. Then we copy that data to the RS and
    //    construct an in-memory importer on top of it.
    //    We don't need to send this message in the ENC case because the debugger
    //    has the same changes applied as the debuggee.
    // 4) Case #3 won't work when dump debugging because we can't send IPC events.
    //    Instead we can locate chunks of the metadata pointed to in the implementation
    //    details of a remote MDInternalRW object, marshal that memory over to the
    //    debugger process, and then put a metadata reader on top of it.
    //    In time this DAC'ized metadata could be used in almost any scenario,
    //    although its probably worth keeping the file mapping technique in case
    //    #1 around for its performance wins.

    CordbProcess * pProcess = GetProcess();
    TargetBuffer bufferMetaData;
    CleanupRemoteBuffer cleanup; // this local has a destructor to do some finally work


    // check for scenarios we might want to handle with case #4
    if (GetProcess()->GetShim() == NULL &&
        GetProcess()->GetWriteableMetadataUpdateMode() == AlwaysShowUpdates &&
        !m_fDynamic)
    {
        //None of the above requirements are particularly hard to change in the future as needed...
        // a) dump-debugging mode - If we do this on a process that can move forward we need a mechanism to determine
        //                          when to refetch the metadata.
        // b) AlwaysShowUpdates - this is purely a risk mitigation choice, there aren't any known back-compat issues
        //                        using DAC'ized metadata. If you want back-compat with the in-proc debugging behavior
        //                        you need to figure out how to ReOpen the same public MD interface with new data.
        // c) !m_fDynamic       - A risk mitigation choice. Initial testing suggests it would work fine.


        // So far we've only got a reader for in-memory-writable metadata (MDInternalRW implementation)
        // We could make a reader for MDInternalRO, but no need yet. This also ensures we don't encroach into common
        // scenario where we can map a file on disk.
        TADDR remoteMDInternalRWAddr = NULL;
        GetProcess()->GetDAC()->GetPEFileMDInternalRW(m_vmPEFile, &remoteMDInternalRWAddr);
        if (remoteMDInternalRWAddr != NULL)
        {
            // we should only be doing this once to initialize, we don't support reopen with this technique
            _ASSERTE(m_pIMImport == NULL);
            ULONG32 mdStructuresVersion;
            HRESULT hr = GetProcess()->GetDAC()->GetMDStructuresVersion(&mdStructuresVersion);
            IfFailThrow(hr);
            ULONG32 mdStructuresDefines;
            hr = GetProcess()->GetDAC()->GetDefinesBitField(&mdStructuresDefines);
            IfFailThrow(hr);
            IMetaDataDispenserCustom* pDispCustom = NULL;
            hr = GetProcess()->GetDispenser()->QueryInterface(IID_IMetaDataDispenserCustom, (void**)&pDispCustom);
            IfFailThrow(hr);
            IMDCustomDataSource* pDataSource = NULL;
            hr = CreateRemoteMDInternalRWSource(remoteMDInternalRWAddr, GetProcess()->GetDataTarget(), mdStructuresDefines, mdStructuresVersion, &pDataSource);
            IfFailThrow(hr);
            IMetaDataImport* pImport = NULL;
            hr = pDispCustom->OpenScopeOnCustomDataSource(pDataSource, 0, IID_IMetaDataImport, (IUnknown**)&m_pIMImport);
            IfFailThrow(hr);
            UpdateInternalMetaData();
            return;
        }
    }

    if(!m_fForceMetaDataSerialize) // case 1 and 2
    {
        LOG((LF_CORDB,LL_INFO10000, "CM::RM !m_fForceMetaDataSerialize case\n"));
        GetProcess()->GetDAC()->GetMetadata(m_vmModule, &bufferMetaData); // throws
    }
    else if (GetProcess()->GetShim() == NULL) // case 3 won't work on a dump so don't try
    {
        return;
    }
    else // case 3 on a live process
    {
        LOG((LF_CORDB,LL_INFO10000, "CM::RM m_fForceMetaDataSerialize case\n"));
        //
        // Send 1 event to get metadata. This allocates a buffer
        //
        DebuggerIPCEvent event;
        pProcess->InitIPCEvent(&event,
            DB_IPCE_RESOLVE_UPDATE_METADATA_1,
            true,
            GetAppDomain()->GetADToken());

        event.MetadataUpdateRequest.vmModule = m_vmModule;

        // Note: two-way event here...
        IfFailThrow(pProcess->SendIPCEvent(&event, sizeof(DebuggerIPCEvent)));

        _ASSERTE(event.type == DB_IPCE_RESOLVE_UPDATE_METADATA_1_RESULT);

        //
        // Update it on the RS
        //
        bufferMetaData.Init(PTR_TO_CORDB_ADDRESS(event.MetadataUpdateRequest.pMetadataStart), (ULONG) event.MetadataUpdateRequest.nMetadataSize);

        // init the cleanup object to ensure the buffer gets destroyed later
        cleanup.bufferMetaData = bufferMetaData;
        cleanup.pProcess = pProcess;
        cleanup.pModule = this;
        cleanup.fDoCleanup = TRUE;
    }

    InitMetaData(bufferMetaData, IsFileMetaDataValid()); // throws
}

// Determines whether the on-disk metadata for this module is usable as the
// current metadata
BOOL CordbModule::IsFileMetaDataValid()
{
    bool fOpenFromFile = true;

    // Dynamic, In-memory, modules must be OpenScopeOnMemory.
    // For modules that require the metadata to be serialized in memory, we must also OpenScopeOnMemory
    // For Enc, we'll can use OpenScope(onFile) and it will get converted to Memory when we get an emitter.
    // We're called from before the ModuleLoad callback, so EnC status hasn't been set yet, so
    // EnC will be false.
    if (m_fDynamic || m_fInMemory || m_fForceMetaDataSerialize)
    {
        LOG((LF_CORDB,LL_INFO10000, "CM::IFMV: m_fDynamic=0x%x m_fInMemory=0x%x m_fForceMetaDataSerialize=0x%x\n",
            m_fDynamic, m_fInMemory, m_fForceMetaDataSerialize));
        fOpenFromFile = false;
    }

#ifdef _DEBUG
    // Reg key override to force us to use Open-by-memory. This can let us run perf tests to
    // compare the Open-by-mem vs. Open-by-file.
    static DWORD openFromFile = 99;
    if (openFromFile == 99)
        openFromFile = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_DbgNoOpenMDByFile);

    if (openFromFile)
    {
        LOG((LF_CORDB,LL_INFO10000, "CM::IFMV: INTERNAL_DbgNoOpenMDByFile is set\n"));
        fOpenFromFile = false;
    }
#endif

    LOG((LF_CORDB,LL_INFO10000, "CM::IFMV: returns 0x%x\n", fOpenFromFile));
    return fOpenFromFile;
}

//---------------------------------------------------------------------------------------
// Accessor for Internal MetaData importer. This is lazily initialized.
//
// Returns:
//     Internal MetaDataImporter, which can be handed off to DAC. Not AddRef().
//     Should be non-null. Throws on error.
//
// Notes:
//     An internal metadata importer is used extensively by DAC-ized code (And Edit-and-continue).
//     This should not be handed out through ICorDebug.
IMDInternalImport * CordbModule::GetInternalMD()
{
    if (m_pInternalMetaDataImport == NULL)
    {
        UpdateInternalMetaData(); // throws
    }
    return m_pInternalMetaDataImport;
}

//---------------------------------------------------------------------------------------
// The one-stop top-level initialization function the metadata (both public and private) for this module.
//
// Arguments:
//    buffer - valid buffer into target containing the metadata.
//    useFileMappingOptimization - if true this allows us to attempt just opening the importer
//                                 by using the metadata in the module on disk. if false or
//                                 if the attempt fails we open the metadata import on memory in
//                                 target buffer
//
// Notes:
//    This will initialize both the internal and public metadata from the buffer in the target.
//    Only called as a helper from RefreshMetaData()
//
//    This may throw (eg, target buffer is missing).
//
void CordbModule::InitMetaData(TargetBuffer buffer, BOOL allowFileMappingOptimization)
{
    CONTRACTL
    {
        THROWS;
    }
    CONTRACTL_END;

    LOG((LF_CORDB,LL_INFO100000, "CM::IM: initing with remote buffer 0x%p length 0x%x\n",
        CORDB_ADDRESS_TO_PTR(buffer.pAddress), buffer.cbSize));

    // clear all the metadata
    m_pInternalMetaDataImport.Clear();

    if (m_pIMImport == NULL)
    {
        // The optimization we're going for here is that the OS will use the same physical memory to
        // back multiple ReadOnly opens of the same file.  Thus since we expect the target process in
        // live debugging, or the debugger in dump debugging, has already opened the file we would
        // like to not create a local buffer and spend time copying in metadata from the target when
        // the OS will happily do address lookup magic against the same physical memory for everyone.


        // Try getting the data from the file if allowed, and fall back to using the buffer
        // if required
        HRESULT hr = S_OK;
        if (allowFileMappingOptimization)
        {
            hr = InitPublicMetaDataFromFile();
            if(FAILED(hr))
            {
                LOG((LF_CORDB,LL_INFO1000000, "CM::IPM: File mapping failed with hr=0x%x\n", hr));
            }
        }

        if(!allowFileMappingOptimization || FAILED(hr))
        {
            // This is where the expensive copy of all metadata content from target memory
            // that we would like to try and avoid happens.
            InitPublicMetaData(buffer);
        }
    }
    else
    {
        // We've already handed out an Import object, and so we can't create a new pointer instance.
        // Instead, we update the existing instance with new data.
        UpdatePublicMetaDataFromRemote(buffer);
    }

    // if we haven't set it by this point UpdateInternalMetaData below is going to get us
    // in an infinite loop of refreshing public metadata
    _ASSERTE(m_pIMImport != NULL);

    // Now that public metadata has changed, force internal metadata to update too.
    // Public and internal metadata expose different access interfaces to the same underlying storage.
    UpdateInternalMetaData();
}

//---------------------------------------------------------------------------------------
// Updates the Internal MetaData object from the public importer. Lazily fetch public importer if needed.
//
// Assumptions:
//     Caller has cleared Internal metadata before even updating public metadata.
//     This way, if the caller fails halfway through updating the public metadata, we don't have
//     stale internal MetaData.
void CordbModule::UpdateInternalMetaData()
{
    CONTRACTL
    {
        THROWS;
    }
    CONTRACTL_END;

    // Caller should have already cleared it.
    _ASSERTE(m_pInternalMetaDataImport == NULL);

    // Get the importer. If it's currently null, this will go fetch it.
    IMetaDataImport * pImport = GetMetaDataImporter(); // throws

    // If both the public and the private interfaces are NULL on entry to this function, the call above will
    // recursively call this function.  This can happen if the caller calls GetInternalMD() directly
    // instead of InitMetaData().  In this case, the above function call will have initialized the internal
    // interface as well, so we need to check for it here.

    if (m_pInternalMetaDataImport == NULL)
    {
        HRESULT hr = GetMDInternalInterfaceFromPublic(
            pImport,
            IID_IMDInternalImport,
            reinterpret_cast<void**> (&m_pInternalMetaDataImport));

        if (m_pInternalMetaDataImport == NULL)
        {
            ThrowHR(hr);
        }
    }

    _ASSERTE(m_pInternalMetaDataImport != NULL);
}

// Initialize the public metadata.
//
// The debuggee already has a copy of the metadata in its process.
// If we OpenScope on file as read-only, the OS file-system will share our metadata with the
// copy in the debuggee. This can be a major perf win. FX metadata can be over 8 MB+.
// OpenScopeOnMemory can't be shared b/c we allocate a buffer.
HRESULT CordbModule::InitPublicMetaDataFromFile()
{
    INTERNAL_API_ENTRY(this->GetProcess());

    // @dbgtodo  metadata - In v3, we can't assume we have the same path namespace as the target (i.e. it could be
    // a dump or remote), so we can't just try and open the file.  Instead we have to rely on interfaces
    // on the datatarget to map the metadata here.  Note that this must also work for minidumps where the
    // metadata isn't necessarily in the dump image.

    // Get filename. There are 2 filenames to choose from:
    // - ngen (if applicable).
    // - non-ngen (aka "normal").
    // By loading metadata out of the same OS file as loaded into the debuggee space, the OS can share those pages.
    const WCHAR * szFullPathName = NULL;
    bool fDebuggerLoadingNgen = false;
    bool fDebuggeeLoadedNgen = false;
    szFullPathName = GetNGenImagePath();

    if(szFullPathName != NULL)
    {
        fDebuggeeLoadedNgen = true;
        fDebuggerLoadingNgen = true;

#ifndef TARGET_UNIX
        // NGEN images are large and we shouldn't load them if they won't be shared, therefore fail the NGEN mapping and
        // fallback to IL image if the debugger doesn't have the image loaded already.
        // Its possible that the debugger would still load the NGEN image sometime in the future and we will miss a sharing
        // opportunity. Its an acceptable loss from an imperfect heuristic.
        if (NULL == WszGetModuleHandle(szFullPathName))
#endif
        {
            szFullPathName = NULL;
            fDebuggerLoadingNgen = false;
        }

    }

    // If we don't have or decided not to load the NGEN image, check to see if IL image is available
    if (!fDebuggerLoadingNgen)
    {
        szFullPathName = GetModulePath();
    }

    // If we are doing live debugging we shouldn't use metadata from an IL image because it doesn't match closely enough.
    // In particular the RVAs for IL code headers are different between the two images which will cause all IL code and
    // local var signature lookups to fail. With further work we could compensate for the RVAs by computing
    // the image layout differences and adjusting the returned RVAs, but there may be other differences that need to be accounted
    // for as well. If we did go that route we should do a binary diff across a variety of NGEN/IL image metadata blobs to
    // get a concrete understanding of the format differences.
    //
    // This check should really be 'Are we OK with only getting the functionality level of mini-dump debugging?' but since we
    // don't know the debugger's intent we guess whether or not we are doing dump debugging by checking if we are shimmed. Once
    // the shim supports live debugging we should probably just stop automatically falling back to IL image and let the debugger
    // decide via the ICorDebugMetadataLocator interface.
    if(fDebuggeeLoadedNgen && !fDebuggerLoadingNgen && GetProcess()->GetShim()!=NULL)
    {
        // The IL image might be there, but we shouldn't use it for live debugging
        return CORDBG_E_MISSING_METADATA;
    }


    // @dbgtodo  metadata  - This is really a CreateFile() call which we can't do. We must offload this to
    // the data target for the dump-debugging scenarios.
    //
    // We're opening it as "read". If we QI for an IEmit interface (which we need for EnC),
    // then the metadata engine will convert it to a "write" underneath us.
    // We want "read" so that we can let the OS share the pages.
    DWORD dwOpenFlags = 0;

    // If we know we're never going to need to write (i.e. never do EnC), then we should indicate
    // that to metadata by telling it this interface will always be read-only.  By passing read-only,
    // the metadata library will then also share the VM space for the image when the same image is
    // opened multiple times for multiple AppDomains.
    // We don't currently have a way to tell absolutely whether this module will support EnC, but we
    // know that NGen modules NEVER support EnC, and NGen is the common case that eats up a lot of VM.
    // So we'll use the heuristic of opening the metadata for all ngen images as read-only.  Ideally
    // we'd go even further here (perhaps even changing metadata to map only the region of the file it
    // needs).
    if (fDebuggerLoadingNgen)
    {
        dwOpenFlags = ofReadOnly | ofTrustedImage;
    }

    // This is the only place we ever validate that the file matches, because we're potentially
    // loading the file from disk ourselves.  We're doing this without giving the debugger a chance
    // to do anything.  We should never load a file that isn't an exact match.
    return InitPublicMetaDataFromFile(szFullPathName, dwOpenFlags, true);
}

// We should only ever validate we have the correct file if it's a file we found ourselves.
// We allow the debugger to choose their own policy with regard to using metadata from the IL image
// when debugging an NI, or even intentionally using mismatched metadata if they like.
HRESULT CordbModule::InitPublicMetaDataFromFile(const WCHAR * pszFullPathName,
                                                DWORD dwOpenFlags,
                                                bool validateFileInfo)
{
#ifdef HOST_UNIX
    // UNIXTODO: Some intricate details of file mapping don't work on Linux as on Windows.
    // We have to revisit this and try to fix it for POSIX system.
    return E_FAIL;
#else
    if (validateFileInfo)
    {
        // Check that we've got the right file to target.
        // There's nothing to prevent some other file being copied in for live, and with
        // dump debugging there's nothing to say that we're not on another machine where a different
        // file is at the same path.
        // If we can't validate we have a hold of the correct file, we should not open it.
        // We will fall back on asking the debugger to get us the correct file, or copying
        // target memory back to the debugger.
        DWORD dwImageTimeStamp = 0;
        DWORD dwImageSize = 0;
        bool isNGEN = false; // unused
        StringCopyHolder filePath;


        _ASSERTE(!m_vmPEFile.IsNull());
        // MetaData lookup favors the NGEN image, which is what we want here.
        if (!this->GetProcess()->GetDAC()->GetMetaDataFileInfoFromPEFile(m_vmPEFile,
                                                                         dwImageTimeStamp,
                                                                         dwImageSize,
                                                                         isNGEN,
                                                                         &filePath))
        {
            LOG((LF_CORDB,LL_WARNING, "CM::IM: Couldn't get metadata info for file \"%s\"\n", pszFullPathName));
            return CORDBG_E_MISSING_METADATA;
        }

        // If the timestamp and size don't match, then this is the wrong file!
        // Map the file and check them.
        HandleHolder hMDFile = WszCreateFile(pszFullPathName,
                                              GENERIC_READ,
                                              FILE_SHARE_READ,
                                              NULL,                 // default security descriptor
                                              OPEN_EXISTING,
                                              FILE_ATTRIBUTE_NORMAL,
                                              NULL);

        if (hMDFile == INVALID_HANDLE_VALUE)
        {
            LOG((LF_CORDB,LL_WARNING, "CM::IM: Couldn't open file \"%s\" (GLE=%x)\n", pszFullPathName, GetLastError()));
            return CORDBG_E_MISSING_METADATA;
        }

        DWORD dwFileHigh = 0;
        DWORD dwFileLow = GetFileSize(hMDFile, &dwFileHigh);
        if (dwFileLow == INVALID_FILE_SIZE)
        {
            LOG((LF_CORDB,LL_WARNING, "CM::IM: File \"%s\" had invalid size.\n", pszFullPathName));
            return CORDBG_E_MISSING_METADATA;
        }

        _ASSERTE(dwFileHigh == 0);

        HandleHolder hMap = WszCreateFileMapping(hMDFile, NULL, PAGE_READONLY, dwFileHigh, dwFileLow, NULL);
        if (hMap == NULL)
        {
            LOG((LF_CORDB,LL_WARNING, "CM::IM: Couldn't create mapping of file \"%s\" (GLE=%x)\n", pszFullPathName, GetLastError()));
            return CORDBG_E_MISSING_METADATA;
        }

        MapViewHolder hMapView = MapViewOfFile(hMap, FILE_MAP_READ, 0, 0, 0);
        if (hMapView == NULL)
        {
            LOG((LF_CORDB,LL_WARNING, "CM::IM: Couldn't map view of file \"%s\" (GLE=%x)\n", pszFullPathName, GetLastError()));
            return CORDBG_E_MISSING_METADATA;
        }

        // Mapped as flat file, have PEDecoder go find what we want.
        PEDecoder pedecoder(hMapView, (COUNT_T)dwFileLow);

        if (!pedecoder.HasNTHeaders())
        {
            LOG((LF_CORDB,LL_WARNING, "CM::IM: \"%s\" did not have PE headers!\n", pszFullPathName));
            return CORDBG_E_MISSING_METADATA;
        }

        if ((dwImageSize != pedecoder.GetVirtualSize()) ||
            (dwImageTimeStamp != pedecoder.GetTimeDateStamp()))
        {
            LOG((LF_CORDB,LL_WARNING, "CM::IM: Validation of \"%s\" failed.  "
                "Expected size=%x, Expected timestamp=%x, Actual size=%x, Actual timestamp=%x\n",
                pszFullPathName,
                pedecoder.GetVirtualSize(),
                pedecoder.GetTimeDateStamp(),
                dwImageSize,
                dwImageTimeStamp));
            return CORDBG_E_MISSING_METADATA;
        }

        // All checks passed, go ahead and load this file for real.
    }

    // Get metadata Dispenser.
    IMetaDataDispenserEx * pDisp = GetProcess()->GetDispenser();

    HRESULT hr = pDisp->OpenScope(pszFullPathName, dwOpenFlags, IID_IMetaDataImport, (IUnknown**)&m_pIMImport);
    _ASSERTE(SUCCEEDED(hr) == (m_pIMImport != NULL));

    if (FAILED(hr))
    {
        // This should never happen in normal scenarios.  It could happen if someone has renamed
        // the assembly after it was opened by the debugee process, but this should be rare enough
        // that we don't mind taking the perf. hit and loading from memory.
        // @dbgtodo  metadata  - would this happen in the shadow-copy scenario?
        LOG((LF_CORDB,LL_WARNING, "CM::IM: Couldn't open metadata in file \"%s\" (hr=%x)\n", pszFullPathName, hr));
    }

    return hr;
#endif // TARGET_UNIX
}

//---------------------------------------------------------------------------------------
// Initialize the public metadata.
//
// Arguments:
//    buffer - valid buffer into target containing the metadata.
//
// Assumptions:
//    This is an internal function which should only be called once to initialize the
//    metadata. Future attempts to re-initialize (in dynamic cases) should call code:CordbModule::UpdatePublicMetaDataFromRemote
//    After the public metadata is initialized, initialize private metadata via code:CordbModule::UpdateInternalMetaData
//
void CordbModule::InitPublicMetaData(TargetBuffer buffer)
{
    CONTRACTL
    {
        THROWS;
    }
    CONTRACTL_END;

    INTERNAL_API_ENTRY(this->GetProcess());
    LOG((LF_CORDB,LL_INFO100000, "CM::IPM: initing with remote buffer 0x%p length 0x%x\n",
        CORDB_ADDRESS_TO_PTR(buffer.pAddress), buffer.cbSize));
    ULONG nMetaDataSize = buffer.cbSize;

    if (nMetaDataSize == 0)
    {
        // We should always have metadata, and if we don't, we want to know.
        // @dbgtodo  metadata - we know metadata from dynamic modules doesn't work in V3
        // (non-shim) cases yet.
        // But our caller should already have handled that case.
        SIMPLIFYING_ASSUMPTION(!"Error: missing the metadata");
        return;
    }

    HRESULT hr = S_OK;

    // Get metadata Dispenser.
    IMetaDataDispenserEx * pDisp = GetProcess()->GetDispenser();

    // copy it over from the remote process

    CoTaskMemHolder<VOID> pMetaDataCopy;
    CopyRemoteMetaData(buffer, pMetaDataCopy.GetAddr());


    //
    // Setup our metadata import object, m_pIMImport
    //

    // Save the old mode for restoration
    VARIANT valueOld;
    hr = pDisp->GetOption(MetaDataSetUpdate, &valueOld);
    SIMPLIFYING_ASSUMPTION(!FAILED(hr));

    // Set R/W mode so that we can update the metadata when
    // we do EnC operations.
    VARIANT valueRW;
    V_VT(&valueRW) = VT_UI4;
    V_I4(&valueRW) = MDUpdateFull;
    hr = pDisp->SetOption(MetaDataSetUpdate, &valueRW);
    SIMPLIFYING_ASSUMPTION(!FAILED(hr));

    hr = pDisp->OpenScopeOnMemory(pMetaDataCopy,
                                  nMetaDataSize,
                                  ofTakeOwnership,
                                  IID_IMetaDataImport,
                                  reinterpret_cast<IUnknown**>( &m_pIMImport ));

    // MetaData has taken ownership -don't free the memory
    pMetaDataCopy.SuppressRelease();

    // Immediately restore the old setting.
    HRESULT hrRestore = pDisp->SetOption(MetaDataSetUpdate, &valueOld);
    SIMPLIFYING_ASSUMPTION(!FAILED(hrRestore));

    // Throw on errors.
    IfFailThrow(hr);
    IfFailThrow(hrRestore);

    // Done!
}

//---------------------------------------------------------------------------------------
// Update public MetaData by copying it from the target and updating our IMetaDataImport object.
//
// Arguments:
//    buffer - buffer into target space containing metadata blob
//
// Notes:
//     Useful for additional class-loads into a dynamic module. A new class means new metadata
//     and so we need to update the RS metadata to stay in sync with the left-side.
//
//     This will call code:CordbModule::CopyRemoteMetaData to copy the remote buffer locally, and then
//     it can OpenScopeOnMemory().
//
void CordbModule::UpdatePublicMetaDataFromRemote(TargetBuffer bufferRemoteMetaData)
{
    CONTRACTL
    {
        // @dbgtodo  metadata  - think about the error semantics here. These fails during dispatching an event; so
        // address this during event pipeline.
        THROWS;
    }
    CONTRACTL_END;

    if (bufferRemoteMetaData.IsEmpty())
    {
        ThrowHR(E_INVALIDARG);
    }

    INTERNAL_API_ENTRY(this->GetProcess()); //
    LOG((LF_CORDB,LL_INFO100000, "CM::UPMFR: updating with remote buffer 0x%p length 0x%x\n",
        CORDB_ADDRESS_TO_PTR(bufferRemoteMetaData.pAddress), bufferRemoteMetaData.cbSize));
    // We're re-initializing existing metadata.
    _ASSERTE(m_pIMImport != NULL);


    HRESULT hr = S_OK;

    ULONG dwMetaDataSize = bufferRemoteMetaData.cbSize;

    // First copy it from the remote process
    CoTaskMemHolder<VOID> pLocalMetaDataPtr;
    CopyRemoteMetaData(bufferRemoteMetaData, pLocalMetaDataPtr.GetAddr());

    IMetaDataDispenserEx *  pDisp = GetProcess()->GetDispenser();
    _ASSERTE(pDisp != NULL); // throws on error.

    LOG((LF_CORDB,LL_INFO100000, "CM::RI: converting to new metadata\n"));

    // now verify that the metadata is valid by opening a temporary scope on the memory
    {
        ReleaseHolder<IMetaDataImport> pIMImport;
        hr = pDisp->OpenScopeOnMemory(pLocalMetaDataPtr,
                                  dwMetaDataSize,
                                  0,
                                  IID_IMetaDataImport,
                                  (IUnknown**)&pIMImport);
        IfFailThrow(hr);
    }

    // We reopen on an existing instance, not create a new instance.
    _ASSERTE(m_pIMImport != NULL); //

    // Now tell our current IMetaDataImport object to re-initialize by swapping in the new memory block.
    // This allows us to keep manipulating metadata objects on other threads without crashing.
    // This will also invalidate an existing associated Internal MetaData.
    hr = ReOpenMetaDataWithMemoryEx(m_pIMImport, pLocalMetaDataPtr, dwMetaDataSize, ofTakeOwnership );
    IfFailThrow(hr);

    // Success.  MetaData now owns the metadata memory
    pLocalMetaDataPtr.SuppressRelease();
}

//---------------------------------------------------------------------------------------
// Copy metadata memory from the remote process into a newly allocated local buffer.
//
// Arguments:
//    pRemoteMetaDataPtr - pointer to remote buffer
//    dwMetaDataSize - size of buffer.
//    pLocalBuffer - holder to get local buffer.
//
// Returns:
//    pLocalBuffer may be allocated.
//    Throws on error (pLocalBuffer may contain garbage).
//    Else if successful, pLocalBuffer contains local copy of metadata.
//
// Notes:
//    This can copy metadata out for the dynamic case or the normal case.
//    Uses an allocator (CoTaskMemHolder) that lets us hand off the memory to the metadata.
void CordbModule::CopyRemoteMetaData(
    TargetBuffer buffer,
    CoTaskMemHolder<VOID> * pLocalBuffer)
{
    CONTRACTL
    {
        THROWS;
    }
    CONTRACTL_END;

    _ASSERTE(pLocalBuffer != NULL);
    _ASSERTE(!buffer.IsEmpty());

    // Allocate space for the local copy of the metadata
    // No need to zero out the memory since we'll fill it all here.
    LPVOID pRawBuffer = CoTaskMemAlloc(buffer.cbSize);
    if (pRawBuffer == NULL)
    {
        ThrowOutOfMemory();
    }

    pLocalBuffer->Assign(pRawBuffer);



    // Copy the metadata from the left side
    GetProcess()->SafeReadBuffer(buffer, (BYTE *)pRawBuffer);

    return;
}

HRESULT CordbModule::QueryInterface(REFIID id, void **pInterface)
{
    if (id == IID_ICorDebugModule)
    {
        *pInterface = static_cast<ICorDebugModule*>(this);
    }
    else if (id == IID_ICorDebugModule2)
    {
        *pInterface = static_cast<ICorDebugModule2*>(this);
    }
    else if (id == IID_ICorDebugModule3)
    {
        *pInterface = static_cast<ICorDebugModule3*>(this);
    }
    else if (id == IID_ICorDebugModule4)
    {
        *pInterface = static_cast<ICorDebugModule4*>(this);
    }
    else if (id == IID_IUnknown)
    {
        *pInterface = static_cast<IUnknown*>(static_cast<ICorDebugModule*>(this));
    }
    else
    {
        *pInterface = NULL;
        return E_NOINTERFACE;
    }

    ExternalAddRef();
    return S_OK;
}

HRESULT CordbModule::GetProcess(ICorDebugProcess **ppProcess)
{
    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT(ppProcess, ICorDebugProcess **);

    *ppProcess = static_cast<ICorDebugProcess*> (GetProcess());
    GetProcess()->ExternalAddRef();

    return S_OK;
}

HRESULT CordbModule::GetBaseAddress(CORDB_ADDRESS *pAddress)
{
    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT(pAddress, CORDB_ADDRESS *);

    *pAddress = m_PEBuffer.pAddress;
    return S_OK;
}

HRESULT CordbModule::GetAssembly(ICorDebugAssembly **ppAssembly)
{
    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT(ppAssembly, ICorDebugAssembly **);

    *ppAssembly = static_cast<ICorDebugAssembly *> (m_pAssembly);
    if (m_pAssembly != NULL)
    {
        m_pAssembly->ExternalAddRef();
    }

    return S_OK;
}

// Public implementation of ICorDebugModule::GetName,
// wrapper around code:GetNameWorker (which throws).
HRESULT CordbModule::GetName(ULONG32 cchName, ULONG32 *pcchName, __out_ecount_part_opt(cchName, *pcchName) WCHAR szName[])
{
    HRESULT hr = S_OK;
    PUBLIC_API_BEGIN(this)
    {
        EX_TRY
        {
            hr = GetNameWorker(cchName, pcchName, szName);
        }
        EX_CATCH_HRESULT(hr);

        // GetNameWorker can use metadata.  If it fails due to missing metadata, or if we fail to find expected
        // target memory (dump debugging) then we should fall back to getting the file name without metadata.
        if ((hr == CORDBG_E_MISSING_METADATA) ||
            (hr == CORDBG_E_READVIRTUAL_FAILURE) ||
            (hr == HRESULT_FROM_WIN32(ERROR_PARTIAL_COPY)))
        {
            DWORD dwImageTimeStamp = 0; // unused
            DWORD dwImageSize = 0;      // unused
            bool isNGEN = false;
            StringCopyHolder filePath;

            _ASSERTE(!m_vmPEFile.IsNull());
            if (this->GetProcess()->GetDAC()->GetMetaDataFileInfoFromPEFile(m_vmPEFile,
                                                                             dwImageTimeStamp,
                                                                             dwImageSize,
                                                                             isNGEN,
                                                                             &filePath))
            {
                _ASSERTE(filePath.IsSet());

                // Unfortunately, metadata lookup preferentially takes the ngen image - so in this case,
                //  we need to go back and get the IL image's name instead.
                if ((isNGEN) &&
                    (this->GetProcess()->GetDAC()->GetILImageInfoFromNgenPEFile(m_vmPEFile,
                                                                                dwImageTimeStamp,
                                                                                dwImageSize,
                                                                                &filePath)))
                {
                    _ASSERTE(filePath.IsSet());
                }

                hr = CopyOutString(filePath, cchName, pcchName, szName);
            }
        }
    }
    PUBLIC_API_END(hr);

    return hr;
}

//---------------------------------------------------------------------------------------
// Gets the module pretty name (may be filename or faked up name)
//
// Arguments:
//   cchName - count of characters in the szName buffer on input.
//   *pcchName - Optional Out parameter, which gets set to the fully requested size
//          (not just how many characters are written).
//   szName - buffer to get name.
//
// Returns:
//   S_OK on success.
//   S_FALSE if we fabricate the name.
//   Return failing HR (on common errors) or Throw on exceptional errors.
//
// Note:
//    Filename isn't necessarily the same as the module name in the metadata.
//
HRESULT CordbModule::GetNameWorker(ULONG32 cchName, ULONG32 *pcchName, __out_ecount_part_opt(cchName, *pcchName) WCHAR szName[])
{
    CONTRACTL
    {
        THROWS;
    }
    CONTRACTL_END;
    HRESULT hr = S_OK;
    const WCHAR * szTempName = NULL;

    ALLOW_DATATARGET_MISSING_MEMORY(
        szTempName = GetModulePath();
    );

#if defined(FEATURE_DBGIPC_TRANSPORT_DI)
    // To support VS when debugging remotely we act like the Compact Framework and return the assembly name
    // when asked for the name of an in-memory module.
    if (szTempName == NULL)
    {
        IMetaDataAssemblyImport *pAssemblyImport = NULL;
        if (SUCCEEDED(hr = GetMetaDataImporter()->QueryInterface(IID_IMetaDataAssemblyImport, (void**)&pAssemblyImport)))
        {
            mdAssembly mda = TokenFromRid(1, mdtAssembly);
            hr = pAssemblyImport->GetAssemblyProps(mda,          // [IN] The Assembly for which to get the properties.
                                                   NULL,         // [OUT] Pointer to the Originator blob.
                                                   NULL,         // [OUT] Count of bytes in the Originator Blob.
                                                   NULL,         // [OUT] Hash Algorithm.
                                                   szName,       // [OUT] Buffer to fill with name.
                                                   cchName,      // [IN] Size of buffer in wide chars.
                                                   (ULONG*)pcchName, // [OUT] Actual # of wide chars in name.
                                                   NULL,         // [OUT] Assembly MetaData.
                                                   NULL);        // [OUT] Flags.

            pAssemblyImport->Release();

            return hr;
        }

        // reset hr
        hr = S_OK;
    }


#endif // FEATURE_DBGIPC_TRANSPORT_DI


    EX_TRY_ALLOW_DATATARGET_MISSING_MEMORY
    {
        StringCopyHolder buffer;
        // If the module has no file name, then we'll fabricate a fake name
        if (!szTempName)
        {
            // On MiniDumpNormal, if the debugger can't find the module then there's no way we will
            // find metadata.
            hr = HRESULT_FROM_WIN32(ERROR_PARTIAL_COPY);

            // Tempting to use the metadata-scope name, but that's a regression from Whidbey. For manifest modules,
            // the metadata scope name is not initialized with the string the user supplied to create the
            // dynamic assembly. So we call into the runtime to use CLR heuristics to get a more accurate name.
            m_pProcess->GetDAC()->GetModuleSimpleName(m_vmModule, &buffer);
            _ASSERTE(buffer.IsSet());
            szTempName = buffer;
            // Note that we considered returning S_FALSE for fabricated names like this, but that's a breaking
            // change from Whidbey that is known to trigger bugs in vS.  If a debugger wants to differentiate
            // real path names from fake simple names, we'll just have to add a new API with the right semantics.
        }

        hr = CopyOutString(szTempName, cchName, pcchName, szName);
    }
    EX_END_CATCH_ALLOW_DATATARGET_MISSING_MEMORY

    return hr;
}

//---------------------------------------------------------------------------------------
// Gets actual name of loaded module. (no faked names)
//
// Returns:
//    string for full path to module name. This is a file that can be opened.
//    NULL if name is not available (such as in some dynamic module cases)
//    Throws if failed accessing target
//
// Notes:
//    We avoid using the method name "GetModuleFileName" because winbase.h #defines that
//    token (along with many others) to have an A or W suffix.
const WCHAR * CordbModule::GetModulePath()
{
    // Lazily initialize.  Module filenames cannot change, and so once
    // we've retrieved this successfully, it's stored for good.
    if (!m_strModulePath.IsSet())
    {
        IDacDbiInterface * pDac = m_pProcess->GetDAC(); // throws
        pDac->GetModulePath(m_vmModule, &m_strModulePath); // throws
        _ASSERTE(m_strModulePath.IsSet());
    }

    if (m_strModulePath.IsEmpty())
    {
        return NULL;    // module has no filename
    }
    return m_strModulePath;
}

//---------------------------------------------------------------------------------------
// Get and caches ngen image path.
//
// Returns:
//    Null-terminated string to ngen image path.
//    NULL if there is no ngen filename (eg, file is not ngenned).
//    Throws on error (such as inability to read the path from the target).
//
// Notes:
//    This can be used to get the path to find metadata. For ngenned images,
//    the IL (and associated metadata) may not be loaded, so we may want to get the
//    metadata out of the ngen image.
const WCHAR * CordbModule::GetNGenImagePath()
{
    HRESULT hr = S_OK;
    EX_TRY
    {
        // Lazily initialize.  Module filenames cannot change, and so once
        // we've retrieved this successfully, it's stored for good.
        if (!m_strNGenImagePath.IsSet())
        {
            IDacDbiInterface * pDac = m_pProcess->GetDAC(); // throws
            BOOL fNonEmpty = pDac->GetModuleNGenPath(m_vmModule, &m_strNGenImagePath); // throws
            (void)fNonEmpty; //prevent "unused variable" error from GCC
            _ASSERTE(m_strNGenImagePath.IsSet() && (m_strNGenImagePath.IsEmpty() == !fNonEmpty));
        }
    }
    EX_CATCH_HRESULT(hr);

    if (FAILED(hr) ||
        m_strNGenImagePath == NULL ||
        m_strNGenImagePath.IsEmpty())
    {
        return NULL;    // module has no ngen filename
    }
    return m_strNGenImagePath;
}

// Implementation of ICorDebugModule::EnableJITDebugging
// See also code:CordbModule::SetJITCompilerFlags
HRESULT CordbModule::EnableJITDebugging(BOOL bTrackJITInfo, BOOL bAllowJitOpts)
{
    // Leftside will enforce that this is a valid time to change jit flags.
    // V1.0 behavior allowed setting these in the middle of a module's lifetime, which meant
    // that different methods throughout the module may have been jitted differently.
    // Since V2, this has to be set when the module is first loaded, before anything is jitted.

    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);

    DWORD dwFlags = CORDEBUG_JIT_DEFAULT;

    // Since V2, bTrackJITInfo is the default and cannot be turned off.
    if (!bAllowJitOpts)
    {
        dwFlags |= CORDEBUG_JIT_DISABLE_OPTIMIZATION;
    }
    return SetJITCompilerFlags(dwFlags);
}

HRESULT CordbModule::EnableClassLoadCallbacks(BOOL bClassLoadCallbacks)
{
    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    ATT_ALLOW_LIVE_DO_STOPGO(GetProcess());

    // You must receive ClassLoad callbacks for dynamic modules so that we can keep the metadata up-to-date on the Right
    // Side. Therefore, we refuse to turn them off for all dynamic modules (they were forced on when the module was
    // loaded on the Left Side.)
    if (m_fDynamic && !bClassLoadCallbacks)
        return E_INVALIDARG;

    if (m_vmDomainFile.IsNull())
        return E_UNEXPECTED;

    // Send a Set Class Load Flag event to the left side. There is no need to wait for a response, and this can be
    // called whether or not the process is synchronized.
    CordbProcess *pProcess = GetProcess();

    DebuggerIPCEvent event;
    pProcess->InitIPCEvent(&event,
                           DB_IPCE_SET_CLASS_LOAD_FLAG,
                           false,
                           (GetAppDomain()->GetADToken()));
    event.SetClassLoad.vmDomainFile = this->m_vmDomainFile;
    event.SetClassLoad.flag = (bClassLoadCallbacks == TRUE);

    HRESULT hr = pProcess->m_cordb->SendIPCEvent(pProcess, &event,
                                                 sizeof(DebuggerIPCEvent));
    hr = WORST_HR(hr, event.hr);
    return hr;
}

//-----------------------------------------------------------------------------
// Public implementation of ICorDebugModule::GetFunctionFromToken
// Get the CordbFunction matches this token / module pair.
// Each time a function is Enc-ed, it gets its own CordbFunction object.
// This will return the latest EnC version of the function for this Module,Token pair.
HRESULT CordbModule::GetFunctionFromToken(mdMethodDef token,
                                          ICorDebugFunction **ppFunction)
{
    // This is not reentrant. DBI should call code:CordbModule::LookupOrCreateFunctionLatestVersion instead.
    PUBLIC_API_ENTRY(this);
    ATT_ALLOW_LIVE_DO_STOPGO(GetProcess()); // @todo - can this be RequiredStop?


    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT(ppFunction, ICorDebugFunction **);

    HRESULT hr = S_OK;
    EX_TRY
    {
        RSLockHolder lockHolder(GetProcess()->GetProcessLock());

        // Check token is valid.
        if ((token == mdMethodDefNil) ||
            (TypeFromToken(token) != mdtMethodDef) ||
            (!GetMetaDataImporter()->IsValidToken(token)))
        {
            ThrowHR(E_INVALIDARG);
        }

        CordbFunction * pFunction = LookupOrCreateFunctionLatestVersion(token);

        *ppFunction = static_cast<ICorDebugFunction*> (pFunction);
        pFunction->ExternalAddRef();

    }
    EX_CATCH_HRESULT(hr);
    return hr;
}

HRESULT CordbModule::GetFunctionFromRVA(CORDB_ADDRESS rva,
                                        ICorDebugFunction **ppFunction)
{
    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT(ppFunction, ICorDebugFunction **);

    return E_NOTIMPL;
}

HRESULT CordbModule::LookupClassByToken(mdTypeDef token,
                                        CordbClass **ppClass)
{
    INTERNAL_API_ENTRY(this->GetProcess()); //
    FAIL_IF_NEUTERED(this);

    HRESULT hr = S_OK;
    EX_TRY // @dbgtodo  exceptions - push this up
    {
        *ppClass = NULL;

        if ((token == mdTypeDefNil) || (TypeFromToken(token) != mdtTypeDef))
        {
            ThrowHR(E_INVALIDARG);
        }

        RSLockHolder lockHolder(GetProcess()->GetProcessLock()); // @dbgtodo  synchronization - Push this up

        CordbClass *pClass = m_classes.GetBase(token);
        if (pClass == NULL)
        {
            // Validate the token.
            if (!GetMetaDataImporter()->IsValidToken(token))
            {
                ThrowHR(E_INVALIDARG);
            }

            RSInitHolder<CordbClass> pClassInit(new CordbClass(this, token));
            pClass = pClassInit.TransferOwnershipToHash(&m_classes);
        }

        *ppClass = pClass;

    }
    EX_CATCH_HRESULT(hr);
    return hr;
}

HRESULT CordbModule::GetClassFromToken(mdTypeDef token,
                                       ICorDebugClass **ppClass)
{
    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    ATT_ALLOW_LIVE_DO_STOPGO(this->GetProcess()); // @todo - could this be RequiredStopped?
    VALIDATE_POINTER_TO_OBJECT(ppClass, ICorDebugClass **);

    HRESULT hr = S_OK;
    EX_TRY
    {
        CordbClass *pClass = NULL;
        *ppClass = NULL;

        // Validate the token.
        if (!GetMetaDataImporter()->IsValidToken(token))
        {
            ThrowHR(E_INVALIDARG);
        }

        hr = LookupClassByToken(token, &pClass);
        IfFailThrow(hr);

        *ppClass = static_cast<ICorDebugClass*> (pClass);
        pClass->ExternalAddRef();
    }
    EX_CATCH_HRESULT(hr);
    return hr;
}

HRESULT CordbModule::CreateBreakpoint(ICorDebugModuleBreakpoint **ppBreakpoint)
{
    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT(ppBreakpoint, ICorDebugModuleBreakpoint **);

    return E_NOTIMPL;
}

//
// Return the token for the Module table entry for this object.  The token
// may then be passed to the meta data import api's.
//
HRESULT CordbModule::GetToken(mdModule *pToken)
{
    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT(pToken, mdModule *);

    HRESULT hr = S_OK;
    EX_TRY
    {
        hr = GetMetaDataImporter()->GetModuleFromScope(pToken);
        IfFailThrow(hr);
    }
    EX_CATCH_HRESULT(hr);
    return hr;
}


// public implementation for ICorDebugModule::GetMetaDataInterface
// Return a meta data interface pointer that can be used to examine the
// meta data for this module.
HRESULT CordbModule::GetMetaDataInterface(REFIID riid, IUnknown **ppObj)
{
    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT(ppObj, IUnknown **);

    HRESULT hr = S_OK;
    EX_TRY
    {
    // QI the importer that we already have and return the result.
        hr = GetMetaDataImporter()->QueryInterface(riid, (void**)ppObj);
        IfFailThrow(hr);
    }
    EX_CATCH_HRESULT(hr);

    return hr;
}

//-----------------------------------------------------------------------------
// LookupFunctionLatestVersion finds the latest cached version of an existing CordbFunction
// in the given module. If the function doesn't exist, it returns NULL.
//
// Arguments:
//     funcMetaDataToken - methoddef token for function to lookup
//
//
// Notes:
//     If no CordbFunction instance was cached, then this returns NULL.
//     use code:CordbModule::LookupOrCreateFunctionLatestVersion to do a lookup that will
//     populate the cache if needed.
CordbFunction* CordbModule::LookupFunctionLatestVersion(mdMethodDef funcMetaDataToken)
{
    INTERNAL_API_ENTRY(this);
    return m_functions.GetBase(funcMetaDataToken);
}


//-----------------------------------------------------------------------------
// Lookup (or create) the CordbFunction for the latest EnC version.
//
// Arguments:
//     funcMetaDataToken - methoddef token for function to lookup
//
// Returns:
//     CordbFunction instance for that token. This will create an instance if needed, and so never returns null.
//     Throws on critical error.
//
// Notes:
//     This creates the latest EnC version. Use code:CordbModule::LookupOrCreateFunction to do an
//     enc-version aware function lookup.
//
CordbFunction* CordbModule::LookupOrCreateFunctionLatestVersion(mdMethodDef funcMetaDataToken)
{
    INTERNAL_API_ENTRY(this);
    CordbFunction * pFunction = m_functions.GetBase(funcMetaDataToken);
    if (pFunction != NULL)
    {
        return pFunction;
    }

    // EnC adds each version to the hash. So if the hash lookup fails, then it must not be an EnC case,
    // and so we can use the default version number.
    return CreateFunction(funcMetaDataToken, CorDB_DEFAULT_ENC_FUNCTION_VERSION);
}

//-----------------------------------------------------------------------------
// LookupOrCreateFunction finds an existing version of CordbFunction in the given module.
// If the function doesn't exist, it creates it.
//
// The outgoing function is not yet fully inititalized. For eg, the Class field is not set.
// However, ICorDebugFunction::GetClass() will check that and lazily initialize the field.
//
// Throws on error.
//
CordbFunction * CordbModule::LookupOrCreateFunction(mdMethodDef funcMetaDataToken, SIZE_T enCVersion)
{
    INTERNAL_API_ENTRY(this);

    _ASSERTE(GetProcess()->ThreadHoldsProcessLock());

    CordbFunction * pFunction = m_functions.GetBase(funcMetaDataToken);

    // special case non-existance as need to add to the hash table too
    if (pFunction == NULL)
    {
        // EnC adds each version to the hash. So if the hash lookup fails,
        // then it must not be an EnC case.
        return CreateFunction(funcMetaDataToken, enCVersion);
    }

    // linked list sorted with most recent version at front. Version numbers correspond
    // to actual edit count against the module, so version numbers not necessarily contiguous.
    // Any valid EnC version must already exist as we would have created it on the ApplyChanges
    for (CordbFunction *pf=pFunction; pf != NULL; pf = pf->GetPrevVersion())
    {
        if (pf->GetEnCVersionNumber() == enCVersion)
        {
            return pf;
        }
    }

    _ASSERTE(!"Couldn't find EnC version of function\n");
    ThrowHR(E_FAIL);
}

HRESULT CordbModule::IsDynamic(BOOL *pDynamic)
{
    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT(pDynamic, BOOL *);

    (*pDynamic) = m_fDynamic;

    return S_OK;
}

BOOL CordbModule::IsDynamic()
{
    return m_fDynamic;
}


HRESULT CordbModule::IsInMemory(BOOL *pInMemory)
{
    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT(pInMemory, BOOL *);

    (*pInMemory) = m_fInMemory;

    return S_OK;
}

HRESULT CordbModule::GetGlobalVariableValue(mdFieldDef fieldDef,
                                            ICorDebugValue **ppValue)
{
    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT(ppValue, ICorDebugValue **);
    ATT_REQUIRE_STOPPED_MAY_FAIL(this->GetProcess());

    HRESULT hr = S_OK;
    EX_TRY
    {

        if (m_pClass == NULL)
        {
            CordbClass * pGlobalClass = NULL;
            hr = LookupClassByToken(COR_GLOBAL_PARENT_TOKEN, &pGlobalClass);
            IfFailThrow(hr);

            m_pClass.Assign(pGlobalClass);
            _ASSERTE(m_pClass != NULL);
        }

        hr = m_pClass->GetStaticFieldValue(fieldDef, NULL, ppValue);
        IfFailThrow(hr);
    }
    EX_CATCH_HRESULT(hr);
    return hr;
}



//
// CreateFunction creates a new function from the given information and
// adds it to the module.
//
CordbFunction * CordbModule::CreateFunction(mdMethodDef funcMetaDataToken, SIZE_T enCVersion)
{
    INTERNAL_API_ENTRY(this);

    // In EnC cases, the token may not yet be valid. We may be caching the CordbFunction
    // for a token for an added method before the metadata is updated on the RS.
    // We rely that our caller has done token validation.

    // Create a new CordbFunction object or throw.
    RSInitHolder<CordbFunction> pFunction(new CordbFunction(this, funcMetaDataToken, enCVersion)); // throws
    CordbFunction * pCopy = pFunction.TransferOwnershipToHash(&m_functions);
    return pCopy;
}

#ifdef EnC_SUPPORTED
//---------------------------------------------------------------------------------------
//
// Creates a new CordbFunction object to represent this new version of a function and
// updates the module's function collection to mark this as the latest version.
//
// Arguments:
//    funcMetaDataToken - the functions methodDef token in this module
//    enCVerison        - The new version number of this function
//    ppFunction        - Output param for the new instance - optional
//
// Assumptions:
//    Assumes the specified version of this function doesn't already exist (i.e. enCVersion
//    is newer than all existing versions).
//
HRESULT CordbModule::UpdateFunction(mdMethodDef funcMetaDataToken,
                                    SIZE_T enCVersion,
                                    CordbFunction** ppFunction)
{
    INTERNAL_API_ENTRY(this);
    if (ppFunction)
        *ppFunction = NULL;

    _ASSERTE(funcMetaDataToken);

    RSLockHolder lockHolder(GetProcess()->GetProcessLock());

    // pOldVersion is the 2nd newest version
    CordbFunction* pOldVersion = LookupFunctionLatestVersion(funcMetaDataToken);

    // if don't have an old version, then create a default versioned one as will most likely
    // go looking for it later and easier to put it in now than have code to insert it later.
    if (!pOldVersion)
    {
        LOG((LF_ENC, LL_INFO10000, "CM::UF: adding %8.8x with version %d\n", funcMetaDataToken, enCVersion));
        HRESULT hr = S_OK;
        EX_TRY
        {
            pOldVersion = CreateFunction(funcMetaDataToken, CorDB_DEFAULT_ENC_FUNCTION_VERSION);
        }
        EX_CATCH_HRESULT(hr);
        if (FAILED(hr))
        {
            return hr;
        }
    }

    // This method should not be called for versions that already exist
    _ASSERTE( enCVersion > pOldVersion->GetEnCVersionNumber());

    LOG((LF_ENC, LL_INFO10000, "CM::UF: updating %8.8x with version %d\n", funcMetaDataToken, enCVersion));
    // Create a new function object.
    CordbFunction * pNewVersion = new (nothrow) CordbFunction(this, funcMetaDataToken, enCVersion);

    if (pNewVersion == NULL)
        return E_OUTOFMEMORY;

    // Chain the 2nd most recent version onto this instance (this will internal addref).
    pNewVersion->SetPrevVersion(pOldVersion);

    // Add the function to the Module's hash of all functions.
    HRESULT hr = m_functions.SwapBase(pOldVersion, pNewVersion);

    if (FAILED(hr))
    {
        delete pNewVersion;
        return hr;
    }

    // Do cleanup for function which is no longer the latest version
    pNewVersion->GetPrevVersion()->MakeOld();

    if (ppFunction)
        *ppFunction = pNewVersion;

    return hr;
}
#endif // EnC_SUPPORTED


HRESULT CordbModule::LookupOrCreateClass(mdTypeDef classMetaDataToken,CordbClass** ppClass)
{
    INTERNAL_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);

    RSLockHolder lockHolder(GetProcess()->GetProcessLock()); // @dbgtodo  exceptions synchronization-
                                                               // Push this lock up, convert to exceptions.

    HRESULT hr = S_OK;
    *ppClass = LookupClass(classMetaDataToken);
    if (*ppClass == NULL)
    {
        hr = CreateClass(classMetaDataToken,ppClass);
        if (!SUCCEEDED(hr))
        {
            return hr;
        }
        _ASSERTE(*ppClass != NULL);
    }
    return hr;
}

//
// LookupClass finds an existing CordbClass in the given module.
// If the class doesn't exist, it returns NULL.
//
CordbClass* CordbModule::LookupClass(mdTypeDef classMetaDataToken)
{
    INTERNAL_API_ENTRY(this);
    _ASSERTE(GetProcess()->ThreadHoldsProcessLock());
    return m_classes.GetBase(classMetaDataToken);
}

//
// CreateClass creates a new class from the given information and
// adds it to the module.
//
HRESULT CordbModule::CreateClass(mdTypeDef classMetaDataToken,
                                 CordbClass** ppClass)
{
    INTERNAL_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);

    _ASSERTE(GetProcess()->ThreadHoldsProcessLock());

    CordbClass* pClass = new (nothrow) CordbClass(this, classMetaDataToken);

    if (pClass == NULL)
        return E_OUTOFMEMORY;

    HRESULT hr = m_classes.AddBase(pClass);

    if (SUCCEEDED(hr))
    {
        *ppClass = pClass;
        if (classMetaDataToken == COR_GLOBAL_PARENT_TOKEN)
        {
            _ASSERTE( m_pClass == NULL ); //redundant create
            m_pClass.Assign(pClass);
        }
    }
    else
    {
        delete pClass;
    }

    return hr;
}


// Resolve a type-ref from this module to a CordbClass
//
// Arguments:
//    token - a Type Ref in this module's scope.
//    ppClass - out parameter to get the class we resolve to.
//
// Returns:
//    S_OK on success.
//    CORDBG_E_CLASS_NOT_LOADED is the TypeRef is not yet resolved because the type it will refer
//    to is not yet loaded.
//
// Notes:
//    In general, a TypeRef refers to a type in another module. (Although as a corner case, it could
//    refer to this module too). This resolves a TypeRef within the current module's scope to a
//    (TypeDef, metadata scope), which is in turn encapsulated as a CordbClass.
//
//    A TypeRef has a resolution scope (ModuleRef or AssemblyRef) and string name for the type
//    within that scope. Resolving means:
//    1. Determining the actual metadata scope loaded for the resolution scope.
//        See also code:CordbModule::ResolveAssemblyInternal
//        If the resolved module hasn't been loaded yet, the resolution will fail.
//    2. Doing a string lookup of the TypeRef's name within that resolved scope to find the TypeDef.
//    3. Returning the (resolved scope, TypeDef) pair.
//
HRESULT CordbModule::ResolveTypeRef(mdTypeRef token, CordbClass **ppClass)
{
    FAIL_IF_NEUTERED(this);
    INTERNAL_SYNC_API_ENTRY(GetProcess()); //

    CordbProcess * pProcess = GetProcess();

    _ASSERTE((pProcess->GetShim() == NULL) || pProcess->GetSynchronized());


    if ((token == mdTypeRefNil) || (TypeFromToken(token) != mdtTypeRef))
    {
        return E_INVALIDARG;
    }

    if (m_vmDomainFile.IsNull() || m_pAppDomain == NULL)
    {
        return E_UNEXPECTED;
    }

    HRESULT         hr = S_OK;
    *ppClass = NULL;
    EX_TRY
    {
        TypeRefData inData = {m_vmDomainFile, token};
        TypeRefData outData;

        {
            RSLockHolder lockHolder(pProcess->GetProcessLock());
            pProcess->GetDAC()->ResolveTypeReference(&inData, &outData);
        }

        CordbModule * pModule = m_pAppDomain->LookupOrCreateModule(outData.vmDomainFile);
        IfFailThrow(pModule->LookupClassByToken(outData.typeToken, ppClass));
    }
    EX_CATCH_HRESULT(hr);

    return hr;

} // CordbModule::ResolveTypeRef

// Resolve a type ref or def to a CordbClass
//
// Arguments:
//    token - a mdTypeDef or mdTypeRef in this module's scope to be resolved
//    ppClass - out parameter to get the CordbClass for this type
//
// Notes:
//    See code:CordbModule::ResolveTypeRef for more details.
HRESULT CordbModule::ResolveTypeRefOrDef(mdToken token, CordbClass **ppClass)
{
    FAIL_IF_NEUTERED(this);
    INTERNAL_SYNC_API_ENTRY(this->GetProcess()); //

    if ((token == mdTypeRefNil) ||
        (TypeFromToken(token) != mdtTypeRef && TypeFromToken(token) != mdtTypeDef))
        return E_INVALIDARG;

    if (TypeFromToken(token)==mdtTypeRef)
    {
        // It's a type-ref. That means the type is defined in another module.
        // That other module is determined at runtime by Fusion / Loader policy. So we need to
        // ultimately ask the runtime which module was actually loaded.
        return ( ResolveTypeRef(token, ppClass) );
    }
    else
    {
        // It's a type-def. This is the easy case because the type is defined in this same module.
        return ( LookupClassByToken(token, ppClass) );
    }

}

//
// GetSize returns the size of the module.
//
HRESULT CordbModule::GetSize(ULONG32 *pcBytes)
{
    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT(pcBytes, ULONG32 *);

    *pcBytes = m_PEBuffer.cbSize;

    return S_OK;
}

CordbAssembly *CordbModule::GetCordbAssembly()
{
    INTERNAL_API_ENTRY(this);
    return m_pAssembly;
}


// This is legacy from the aborted V1 EnC attempt - not used in V2 EnC support
HRESULT CordbModule::GetEditAndContinueSnapshot(
    ICorDebugEditAndContinueSnapshot **ppEditAndContinueSnapshot)
{
    return E_NOTIMPL;
}


//---------------------------------------------------------------------------------------
//
// Requests that an edit be applied to the module for edit and continue and updates
// the right-side state and metadata.
//
// Arguments:
//    cbMetaData - number of bytes in pbMetaData
//    pbMetaData - a delta metadata blob describing the metadata edits to be made
//    cbIL       - number of bytes in pbIL
//    pbIL       - a new method body stream containing all of the method body information
//                 (IL, EH info, etc) for edited and added methods.
//
// Return Value:
//    S_OK on success, various errors on failure
//
// Notes:
//
//
//    This applies the same changes to the RS's copy of the metadata that the left-side will apply to
//    it's copy of the metadata. see code:EditAndContinueModule::ApplyEditAndContinue
//
HRESULT CordbModule::ApplyChanges(ULONG  cbMetaData,
                                  BYTE   pbMetaData[],
                                  ULONG  cbIL,
                                  BYTE   pbIL[])
{
    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());

#ifdef FEATURE_ENC_SUPPORTED
    // We enable EnC back in code:CordbModule::SetJITCompilerFlags.
    // If EnC isn't enabled, then we'll fail in the LS when we try to ApplyChanges.
    // We'd expect a well-behaved debugger to never actually land here.


    LOG((LF_CORDB,LL_INFO10000, "CP::AC: applying changes"));

    VALIDATE_POINTER_TO_OBJECT_ARRAY(pbMetaData,
                                   BYTE,
                                   cbMetaData,
                                   true,
                                   true);
    VALIDATE_POINTER_TO_OBJECT_ARRAY(pbIL,
                                   BYTE,
                                   cbIL,
                                   true,
                                   true);

    HRESULT hr;
    RSExtSmartPtr<IUnknown> pUnk;
    RSExtSmartPtr<IMDInternalImport> pMDImport;
    RSExtSmartPtr<IMDInternalImport> pMDImport2;

    //
    // Edit was successful - update the right-side state to reflect the edit
    //

    ++m_EnCCount;

    // apply the changes to our copy of the metadata

    _ASSERTE(m_pIMImport != NULL); // must have metadata at this point in EnC
    IfFailGo(m_pIMImport->QueryInterface(IID_IUnknown, (void**)&pUnk));

    IfFailGo(GetMDInternalInterfaceFromPublic(pUnk, IID_IMDInternalImport,
                                                    (void **)&pMDImport));

    // The left-side will call this same method on its copy of the metadata.
    hr = pMDImport->ApplyEditAndContinue(pbMetaData, cbMetaData, &pMDImport2);
    if (pMDImport2 != NULL)
    {
        // ApplyEditAndContinue() expects IMDInternalImport**, but we give it RSExtSmartPtr<IMDInternalImport>
        // Silent cast of RSExtSmartPtr to IMDInternalImport* leads to assignment of a raw pointer
        // without calling AddRef(), thus we need to do it manually.

        // @todo -  ApplyEditAndContinue should probably AddRef the out parameter.
        pMDImport2->AddRef();
    }
    IfFailGo(hr);


    // We're about to get a new importer object, so release the old one.
    m_pIMImport.Clear();
    IfFailGo(GetMDPublicInterfaceFromInternal(pMDImport2, IID_IMetaDataImport, (void **)&m_pIMImport));
    // set the new RVA value

    // Send the delta over to the debugee and request that it apply the edit
    IfFailGo( ApplyChangesInternal(cbMetaData, pbMetaData, cbIL, pbIL) );

    EX_TRY
    {

        m_pInternalMetaDataImport.Clear();
        UpdateInternalMetaData();
    }
    EX_CATCH_HRESULT(hr);
    _ASSERTE(SUCCEEDED(hr));

ErrExit:
    // MetaData interface pointers will be automatically released via SmartPtr dtors.

    // @todo : prevent further execution of program
    return hr;
#else
    return E_NOTIMPL;
#endif
}




//---------------------------------------------------------------------------------------
//
// Requests that an edit be applied to the module for edit and continue and updates
// some right-side state, but does not update our copy of the metadata.
//
// Arguments:
//    cbMetaData - number of bytes in pbMetaData
//    pbMetaData - a delta metadata blob describing the metadata edits to be made
//    cbIL       - number of bytes in pbIL
//    pbIL       - a new method body stream containing all of the method body information
//                 (IL, EH info, etc) for edited and added methods.
//
// Return Value:
//    S_OK on success, various errors on failure
//
HRESULT CordbModule::ApplyChangesInternal(ULONG  cbMetaData,
                                          BYTE   pbMetaData[],
                                          ULONG  cbIL,
                                          BYTE   pbIL[])
{
    CONTRACTL
    {
        NOTHROW;
    }
    CONTRACTL_END;

    LOG((LF_ENC,LL_INFO100, "CordbProcess::ApplyChangesInternal\n"));

    FAIL_IF_NEUTERED(this);
    INTERNAL_SYNC_API_ENTRY(this->GetProcess()); //

    if (m_vmDomainFile.IsNull())
        return E_UNEXPECTED;

#ifdef FEATURE_ENC_SUPPORTED
    HRESULT hr;

    void * pRemoteBuf = NULL;

    EX_TRY
    {

        // Create and initialize the event as synchronous
        // We'll be sending a NULL appdomain pointer since the individual modules
        // will contains pointers to their respective A.D.s
        DebuggerIPCEvent event;
        GetProcess()->InitIPCEvent(&event, DB_IPCE_APPLY_CHANGES, false, VMPTR_AppDomain::NullPtr());

        event.ApplyChanges.vmDomainFile = this->m_vmDomainFile;

        // Have the left-side create a buffer for us to store the delta into
        ULONG cbSize = cbMetaData+cbIL;
        TargetBuffer tbFull = GetProcess()->GetRemoteBuffer(cbSize);
        pRemoteBuf = CORDB_ADDRESS_TO_PTR(tbFull.pAddress);

        TargetBuffer tbMetaData = tbFull.SubBuffer(0, cbMetaData); // 1st half
        TargetBuffer tbIL = tbFull.SubBuffer(cbMetaData); // 2nd half

        // Copy the delta metadata over to the debugee

        GetProcess()->SafeWriteBuffer(tbMetaData, pbMetaData); // throws
        GetProcess()->SafeWriteBuffer(tbIL, pbIL); // throws

        // Send a synchronous event requesting the debugee apply the edit
        event.ApplyChanges.pDeltaMetadata = tbMetaData.pAddress;
        event.ApplyChanges.cbDeltaMetadata = tbMetaData.cbSize;
        event.ApplyChanges.pDeltaIL = tbIL.pAddress;
        event.ApplyChanges.cbDeltaIL = tbIL.cbSize;

        LOG((LF_ENC,LL_INFO100, "CordbProcess::ApplyChangesInternal sending event\n"));
        hr = GetProcess()->SendIPCEvent(&event, sizeof(event));
        hr = WORST_HR(hr, event.hr);
        IfFailThrow(hr);

        // Allocate space for the return event.
        // We always copy over the whole buffer size which is bigger than sizeof(DebuggerIPCEvent)
        // This seems ugly, in this case we know the exact size of the event we want to read
        // why copy over all the extra data?
        DebuggerIPCEvent *retEvent = (DebuggerIPCEvent *) _alloca(CorDBIPC_BUFFER_SIZE);

        {
            //
            // Wait for events to return from the RC. We expect zero or more add field,
            // add function or update function events and one completion event.
            //
            while (TRUE)
            {
                hr = GetProcess()->m_cordb->WaitForIPCEventFromProcess(GetProcess(),
                                                                       GetAppDomain(),
                                                                       retEvent);
                IfFailThrow(hr);

                if (retEvent->type == DB_IPCE_APPLY_CHANGES_RESULT)
                {
                    // Done receiving update events
                    hr = retEvent->ApplyChangesResult.hr;
                    LOG((LF_CORDB, LL_INFO1000, "[%x] RCET::DRCE: EnC apply changes result %8.8x.\n", hr));
                    break;
                }

                _ASSERTE(retEvent->type == DB_IPCE_ENC_UPDATE_FUNCTION ||
                                  retEvent->type == DB_IPCE_ENC_ADD_FUNCTION ||
                                  retEvent->type == DB_IPCE_ENC_ADD_FIELD);
                LOG((LF_CORDB, LL_INFO1000, "[%x] RCET::DRCE: EnC %s %8.8x to version %d.\n",
                        GetCurrentThreadId(),
                        retEvent->type == DB_IPCE_ENC_UPDATE_FUNCTION ? "Update function" :
                        retEvent->type == DB_IPCE_ENC_ADD_FUNCTION ? "Add function" : "Add field",
                        retEvent->EnCUpdate.memberMetadataToken, retEvent->EnCUpdate.newVersionNumber));

                CordbAppDomain *pAppDomain = GetAppDomain();
                _ASSERTE(NULL != pAppDomain);
                CordbModule* pModule = NULL;


                pModule = pAppDomain->LookupOrCreateModule(retEvent->EnCUpdate.vmDomainFile); // throws
                _ASSERTE(pModule != NULL);

                // update to the newest version

                if (retEvent->type == DB_IPCE_ENC_UPDATE_FUNCTION ||
                     retEvent->type == DB_IPCE_ENC_ADD_FUNCTION)
                {
                    // Update the function collection to reflect this edit
                    hr = pModule->UpdateFunction(retEvent->EnCUpdate.memberMetadataToken, retEvent->EnCUpdate.newVersionNumber, NULL);

                }
                // mark the class and relevant type as old so we update it next time we try to query it
                if (retEvent->type == DB_IPCE_ENC_ADD_FUNCTION ||
                     retEvent->type == DB_IPCE_ENC_ADD_FIELD)
                {
                    RSLockHolder lockHolder(GetProcess()->GetProcessLock()); // @dbgtodo  synchronization -  push this up
                    CordbClass* pClass = pModule->LookupClass(retEvent->EnCUpdate.classMetadataToken);
                    // if don't find class, that is fine because it hasn't been loaded yet so doesn't
                    // need to be updated
                    if (pClass)
                    {
                        pClass->MakeOld();
                    }
                }
            }
        }

        LOG((LF_ENC,LL_INFO100, "CordbProcess::ApplyChangesInternal complete.\n"));
    }
    EX_CATCH_HRESULT(hr);

    // process may have gone away by the time we get here so don't assume is there.
    CordbProcess *pProcess = GetProcess();
    if (pProcess)
    {
        HRESULT hr2 = pProcess->ReleaseRemoteBuffer(&pRemoteBuf);
        TESTANDRETURNHR(hr2);
    }
    return hr;
#else // FEATURE_ENC_SUPPORTED
    return E_NOTIMPL;
#endif // FEATURE_ENC_SUPPORTED

}

// Set the JMC status for the entire module.
// All methods specified in others[] will have jmc status !fIsUserCode
// All other methods will have jmc status fIsUserCode.
HRESULT CordbModule::SetJMCStatus(
        BOOL fIsUserCode,
        ULONG32 cOthers,
        mdToken others[])
{
    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());

    if (m_vmDomainFile.IsNull())
        return E_UNEXPECTED;

    // @todo -allow the other parameters. These are functions that have default status
    // opposite of fIsUserCode.
    if (cOthers != 0)
    {
        _ASSERTE(!"not yet impl for cOthers != 0");
        return E_NOTIMPL;
    }

    // Send event to the LS.
    CordbProcess* pProcess = this->GetProcess();
    _ASSERTE(pProcess != NULL);


    // Tell the LS that this module is/is not user code
    DebuggerIPCEvent event;
    pProcess->InitIPCEvent(&event, DB_IPCE_SET_MODULE_JMC_STATUS, true, this->GetAppDomain()->GetADToken());
    event.SetJMCFunctionStatus.vmDomainFile = m_vmDomainFile;
    event.SetJMCFunctionStatus.dwStatus = fIsUserCode;


    // Note: two-way event here...
    HRESULT hr = pProcess->m_cordb->SendIPCEvent(pProcess, &event, sizeof(DebuggerIPCEvent));

    // Stop now if we can't even send the event.
    if (!SUCCEEDED(hr))
    {
        LOG((LF_CORDB, LL_INFO10, "CordbModule::SetJMCStatus failed  0x%08x...\n", hr));

        return hr;
    }

    _ASSERTE(event.type == DB_IPCE_SET_MODULE_JMC_STATUS_RESULT);

    LOG((LF_CORDB, LL_INFO10, "returning from CordbModule::SetJMCStatus 0x%08x...\n", hr));

    return event.hr;
}


//
// Resolve an assembly given an AssemblyRef token. Note that
// this will not trigger the loading of assembly. If assembly is not yet loaded,
// this will return an CORDBG_E_CANNOT_RESOLVE_ASSEMBLY error
//
HRESULT CordbModule::ResolveAssembly(mdToken tkAssemblyRef,
                                    ICorDebugAssembly **ppAssembly)
{
    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    ATT_REQUIRE_STOPPED_MAY_FAIL(this->GetProcess());

    if(ppAssembly)
    {
        *ppAssembly = NULL;
    }

    HRESULT hr = S_OK;
    EX_TRY
    {
        CordbAssembly *pCordbAsm = ResolveAssemblyInternal(tkAssemblyRef);
        if (pCordbAsm == NULL)
        {
            // Don't throw here. It's a common-case failure path and not exceptional.
            hr = CORDBG_E_CANNOT_RESOLVE_ASSEMBLY;
        }
        else if(ppAssembly)
        {
            _ASSERTE(pCordbAsm != NULL);
            *ppAssembly = pCordbAsm;
            pCordbAsm->ExternalAddRef();
        }
    }
    EX_CATCH_HRESULT(hr);
    return hr;
}

//---------------------------------------------------------------------------------------
// Worker to resolve an assembly ref.
//
// Arguments:
//     tkAssemblyRef - token of assembly ref to resolve
//
// Returns:
//     Assembly that this token resolves to.
//     NULL if it's a valid token but the assembly has not yet been resolved.
//      (This is a non-exceptional error case).
//
// Notes:
//     MetaData has tokens to represent a reference to another assembly.
//     But Loader/Fusion policy ultimately decides which specific assembly is actually loaded
//     for that token.
//     This does the lookup of actual assembly and reports back to the debugger.

CordbAssembly * CordbModule::ResolveAssemblyInternal(mdToken tkAssemblyRef)
{
    INTERNAL_SYNC_API_ENTRY(GetProcess()); //

    if (TypeFromToken(tkAssemblyRef) != mdtAssemblyRef || tkAssemblyRef == mdAssemblyRefNil)
    {
        // Not a valid token
        ThrowHR(E_INVALIDARG);
    }

    CordbAssembly *    pAssembly = NULL;

    if (!m_vmDomainFile.IsNull())
    {
        // Get DAC to do the real work to resolve the assembly
        VMPTR_DomainAssembly vmDomainAssembly = GetProcess()->GetDAC()->ResolveAssembly(m_vmDomainFile, tkAssemblyRef);

        // now find the ICorDebugAssembly corresponding to it
        if (!vmDomainAssembly.IsNull() && m_pAppDomain != NULL)
        {
            RSLockHolder lockHolder(GetProcess()->GetProcessLock());
            // Don't throw here because if the lookup fails, we want to throw CORDBG_E_CANNOT_RESOLVE_ASSEMBLY.
            pAssembly = m_pAppDomain->LookupOrCreateAssembly(vmDomainAssembly);
        }
    }

    return pAssembly;
}

//
// CreateReaderForInMemorySymbols - create an ISymUnmanagedReader object for symbols
// which are loaded into memory in the CLR.  See interface definition in cordebug.idl for
// details.
//
HRESULT CordbModule::CreateReaderForInMemorySymbols(REFIID riid, void** ppObj)
{
    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);

    CordbProcess *pProcess = GetProcess();
    ATT_REQUIRE_STOPPED_MAY_FAIL(pProcess);

    HRESULT hr = S_OK;
    EX_TRY
    {
        // Get the symbol memory in a stream to give to the reader.
        ReleaseHolder<IStream> pStream;
        IDacDbiInterface::SymbolFormat symFormat = GetInMemorySymbolStream(&pStream);

        // First create the symbol binder corresponding to the format of the stream
        ReleaseHolder<ISymUnmanagedBinder> pBinder;
        if (symFormat == IDacDbiInterface::kSymbolFormatPDB)
        {
#ifndef TARGET_UNIX
            // PDB format - use diasymreader.dll with COM activation
            InlineSString<_MAX_PATH> ssBuf;
            IfFailThrow(GetClrModuleDirectory(ssBuf));
            IfFailThrow(FakeCoCreateInstanceEx(CLSID_CorSymBinder_SxS,
                                               ssBuf.GetUnicode(),
                                               IID_ISymUnmanagedBinder,
                                               (void**)&pBinder,
                                               NULL));
#else
            IfFailThrow(FakeCoCreateInstance(CLSID_CorSymBinder_SxS,
                                             IID_ISymUnmanagedBinder,
                                             (void**)&pBinder));
#endif
        }
        else
        {
            // No in-memory symbols, return the appropriate error
            _ASSERTE(symFormat == IDacDbiInterface::kSymbolFormatNone);
            if (m_fDynamic || m_fInMemory)
            {
                // This is indeed an in-memory or dynamic module, we just don't have any symbols for it.
                // This means the application didn't supply any, or they are not yet available.  Symbols
                // first become available at LoadClass time for dynamic modules and UpdateModuleSymbols
                // time for non-dynamic in-memory modules.
                ThrowHR(CORDBG_E_SYMBOLS_NOT_AVAILABLE);
            }

            // This module is on disk - the debugger should use it's normal symbol-loading logic.
            ThrowHR(CORDBG_E_MODULE_LOADED_FROM_DISK);
        }

        // In the attach or dump case, if we attach or take the dump after we have defined a dynamic module, we may
        // have already set the symbol format to "PDB" by the time we call CreateReaderForInMemorySymbols during initialization
        // for loaded modules. (In the launch case, we do this initialization when the module is actually loaded, and before we
        // set the symbol format.) When we call CreateReaderForInMemorySymbols, we can't assume the initialization was already
        // performed or specifically, that we already have m_pIMImport initialized. We can't call into diasymreader with a NULL
        // pointer as the value for m_pIMImport, so we need to check that here.
        if (m_pIMImport == NULL)
        {
            ThrowHR(CORDBG_E_SYMBOLS_NOT_AVAILABLE);
        }

        // Now create the symbol reader from the data
        ReleaseHolder<ISymUnmanagedReader> pReader;
        IfFailThrow(pBinder->GetReaderFromStream(m_pIMImport, pStream, &pReader));

        // Attempt to return the interface requested
        // Note that this does an AddRef for our return value ppObj, so we don't suppress the release
        // of the pReader holder.
        IfFailThrow(pReader->QueryInterface(riid, ppObj));
    }
    EX_CATCH_HRESULT(hr);
    return hr;
}

/* ------------------------------------------------------------------------- *
 * Class class
 * ------------------------------------------------------------------------- */

//---------------------------------------------------------------------------------------
// Set the continue counter that marks when the module is in its Load event
//
// Notes:
//    Jit flags can only be changed in the real module Load event. We may
//    have multiple module load events on different threads coming at the
//    same time. So each module load tracks its continue counter.
//
//    This can be used by code:CordbModule::EnsureModuleIsInLoadCallback to
//    properly return CORDBG_E_MUST_BE_IN_LOAD_MODULE
void CordbModule::SetLoadEventContinueMarker()
{
    // Well behaved targets should only set this once.
    GetProcess()->TargetConsistencyCheck(m_nLoadEventContinueCounter == 0);

    m_nLoadEventContinueCounter = GetProcess()->m_continueCounter;
}

//---------------------------------------------------------------------------------------
// Return CORDBG_E_MUST_BE_IN_LOAD_MODULE if the module is not in the load module callback.
//
// Notes:
//   The comparison is done via continue counters. The counter of the load
//   event is cached via code:CordbModule::SetLoadEventContinueMarker.
//
//   This state is currently stored on the RS. Alternatively, it could likely be retreived from the LS state as
//   well. One disadvantage of the current model is that if we detach during the load-module callback and
//   then reattach, the RS state is flushed and we lose the fact that we can toggle the jit flags.
HRESULT CordbModule::EnsureModuleIsInLoadCallback()
{
    if (this->m_nLoadEventContinueCounter < GetProcess()->m_continueCounter)
    {
        return CORDBG_E_MUST_BE_IN_LOAD_MODULE;
    }
    else
    {
        return S_OK;
    }
}

// Implementation of ICorDebugModule2::SetJITCompilerFlags
// See also code:CordbModule::EnableJITDebugging
HRESULT CordbModule::SetJITCompilerFlags(DWORD dwFlags)
{
    PUBLIC_REENTRANT_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);

    CordbProcess *pProcess = GetProcess();

    ATT_REQUIRE_STOPPED_MAY_FAIL(pProcess);
    HRESULT hr = S_OK;

    EX_TRY
    {
        // can't have a subset of these, eg 0x101, so make sure we have an exact match
        if ((dwFlags != CORDEBUG_JIT_DEFAULT) &&
            (dwFlags != CORDEBUG_JIT_DISABLE_OPTIMIZATION) &&
            (dwFlags != CORDEBUG_JIT_ENABLE_ENC))
        {
            hr = E_INVALIDARG;
        }
        else
        {
            BOOL fAllowJitOpts = ((dwFlags & CORDEBUG_JIT_DISABLE_OPTIMIZATION) != CORDEBUG_JIT_DISABLE_OPTIMIZATION);
            BOOL fEnableEnC = ((dwFlags & CORDEBUG_JIT_ENABLE_ENC) == CORDEBUG_JIT_ENABLE_ENC);

            // Can only change jit flags when module is first loaded and before there's any jitted code.
            // This ensures all code in the module is jitted the same way.
            hr = EnsureModuleIsInLoadCallback();

            if (SUCCEEDED(hr))
            {
                // DD interface will check if it's a valid time to change the flags.
                hr = pProcess->GetDAC()->SetCompilerFlags(GetRuntimeDomainFile(), fAllowJitOpts, fEnableEnC);
            }
        }
    }
    EX_CATCH_HRESULT(hr);

    // emulate v2 hresults
    if (GetProcess()->GetShim() != NULL)
    {
        // Emulate Whidbey error hresults
        hr = GetProcess()->GetShim()->FilterSetJitFlagsHresult(hr);
    }
    return hr;

}

// Implementation of ICorDebugModule2::GetJitCompilerFlags
HRESULT CordbModule::GetJITCompilerFlags(DWORD *pdwFlags )
{
    PUBLIC_REENTRANT_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT(pdwFlags, DWORD*);
    *pdwFlags = CORDEBUG_JIT_DEFAULT;;

    CordbProcess *pProcess = GetProcess();


    ATT_REQUIRE_STOPPED_MAY_FAIL(pProcess);
    HRESULT hr = S_OK;

    EX_TRY
    {
        BOOL fAllowJitOpts;
        BOOL fEnableEnC;

        pProcess->GetDAC()->GetCompilerFlags (
            GetRuntimeDomainFile(),
            &fAllowJitOpts,
            &fEnableEnC);

        if (fEnableEnC)
        {
            *pdwFlags = CORDEBUG_JIT_ENABLE_ENC;
        }
        else if (! fAllowJitOpts)
        {
            *pdwFlags = CORDEBUG_JIT_DISABLE_OPTIMIZATION;
        }

    }
    EX_CATCH_HRESULT(hr);
    return hr;
}

HRESULT CordbModule::IsMappedLayout(BOOL *isMapped)
{
    PUBLIC_API_ENTRY(this);
    VALIDATE_POINTER_TO_OBJECT(isMapped, BOOL*);
    FAIL_IF_NEUTERED(this);

    HRESULT hr = S_OK;
    CordbProcess *pProcess = GetProcess();

    ATT_REQUIRE_STOPPED_MAY_FAIL(pProcess);

    EX_TRY
    {
        hr = pProcess->GetDAC()->IsModuleMapped(m_vmModule, isMapped);
    }
    EX_CATCH_HRESULT(hr);

    return hr;
}

/* ------------------------------------------------------------------------- *
 * CordbCode class
 * ------------------------------------------------------------------------- */
//-----------------------------------------------------------------------------
// CordbCode constructor
// Arguments:
//    Input:
//        pFunction  - CordbFunction instance for this function
//        encVersion - Edit and Continue version number for this code chunk
//        fIsIL      - indicates whether the instance is a CordbILCode (as
//                     opposed to a CordbNativeCode)
//        id         - This is the hashtable key for CordbCode objects
//                   - for native code, the code start address
//                   - for IL code, 0
//                   - for ReJit IL code, the remote pointer to the ReJitSharedInfo
//    Output:
//        fields of the CordbCode instance have been initialized
//-----------------------------------------------------------------------------

CordbCode::CordbCode(CordbFunction * pFunction, UINT_PTR id, SIZE_T encVersion, BOOL fIsIL)
  : CordbBase(pFunction->GetProcess(), id, enumCordbCode),
    m_fIsIL(fIsIL),
    m_nVersion(encVersion),
    m_rgbCode(NULL),
    m_continueCounterLastSync(0),
    m_pFunction(pFunction)
{
    _ASSERTE(pFunction != NULL);
    _ASSERTE(m_nVersion >= CorDB_DEFAULT_ENC_FUNCTION_VERSION);
} // CordbCode::CordbCode

//-----------------------------------------------------------------------------
// Destructor for CordbCode object
//-----------------------------------------------------------------------------
CordbCode::~CordbCode()
{
    _ASSERTE(IsNeutered());
}

//-----------------------------------------------------------------------------
// Neutered by CordbFunction
// See CordbBase::Neuter for neuter semantics.
//-----------------------------------------------------------------------------
void CordbCode::Neuter()
{
    m_pFunction = NULL;

    delete [] m_rgbCode;
    m_rgbCode = NULL;

    CordbBase::Neuter();
}

//-----------------------------------------------------------------------------
// Public method for IUnknown::QueryInterface.
// Has standard QI semantics.
//-----------------------------------------------------------------------------
HRESULT CordbCode::QueryInterface(REFIID id, void ** pInterface)
{
    if (id == IID_ICorDebugCode)
    {
        *pInterface = static_cast<ICorDebugCode*>(this);
    }
    else if (id == IID_IUnknown)
    {
        *pInterface = static_cast<IUnknown *>(static_cast<ICorDebugCode *>(this));
    }
    else
    {
        *pInterface = NULL;
        return E_NOINTERFACE;
    }

    ExternalAddRef();
    return S_OK;
}

//-----------------------------------------------------------------------------
// NOT IMPLEMENTED. Remap sequence points are entirely private to the LS,
// and ICorDebug will dispatch a RemapOpportunity callback to notify the
// debugger instead of letting the debugger query for the points.
//
// Returns: E_NOTIMPL
//-----------------------------------------------------------------------------
HRESULT CordbCode::GetEnCRemapSequencePoints(ULONG32 cMap, ULONG32 * pcMap, ULONG32 offsets[])
{
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT_OR_NULL(pcMap, ULONG32*);
    VALIDATE_POINTER_TO_OBJECT_ARRAY_OR_NULL(offsets, ULONG32*, cMap, true, true);

    //
    // Old EnC interface - deprecated
    //
    return E_NOTIMPL;
} // CordbCode::GetEnCRemapSequencePoints


//-----------------------------------------------------------------------------
// CordbCode::IsIL
// Public method to determine if this Code object represents IL or native code.
//
// Parameters:
//    pbIL - OUT: on return, set to True if IL code, else False.
//
// Returns:
//    S_OK on success.
//-----------------------------------------------------------------------------
HRESULT CordbCode::IsIL(BOOL *pbIL)
{
    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT(pbIL, BOOL *);

    *pbIL = IsIL();

    return S_OK;
}

//-----------------------------------------------------------------------------
// CordbCode::GetFunction
// Public method to get the Function object associated with this Code object.
// Function:Code = 1:1 for IL, and 1:n for Native. So there is always a single
// unique Function object to return.
//
// Parameters:
//   ppFunction - OUT: returns the Function object for this Code.
//
// Returns:
//   S_OK - on success.
//-----------------------------------------------------------------------------
HRESULT CordbCode::GetFunction(ICorDebugFunction **ppFunction)
{
    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT(ppFunction, ICorDebugFunction **);

    *ppFunction = static_cast<ICorDebugFunction*> (m_pFunction);
    m_pFunction->ExternalAddRef();

    return S_OK;
}

//-----------------------------------------------------------------------------
// CordbCode::GetSize
// Get the size of the code in bytes. If this is IL code, it will be bytes of IL.
// If this is native code, it will be bytes of native code.
//
// Parameters:
//   pcBytes - OUT: on return, set to the size of the code in bytes.
//
// Returns:
//   S_OK on success.
//-----------------------------------------------------------------------------
HRESULT CordbCode::GetSize(ULONG32 *pcBytes)
{
    PUBLIC_REENTRANT_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT(pcBytes, ULONG32 *);

    *pcBytes = GetSize();
    return S_OK;
}

//-----------------------------------------------------------------------------
// CordbCode::CreateBreakpoint
// public method to create a breakpoint in the code.
//
// Parameters:
//   offset - offset in bytes to set the breakpoint at. If this is a Native
//      code object (IsIl == false), then units are bytes of native code. If
//      this is an IL code object, then units are bytes of IL code.
//   ppBreakpoint- out-parameter to hold newly created breakpoint object.
//
// Return value:
//   S_OK iff *ppBreakpoint is set. Else some error.
//-----------------------------------------------------------------------------
HRESULT CordbCode::CreateBreakpoint(ULONG32 offset,
                                    ICorDebugFunctionBreakpoint **ppBreakpoint)
{
    PUBLIC_REENTRANT_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT(ppBreakpoint, ICorDebugFunctionBreakpoint **);

    HRESULT hr;
    ULONG32 size = GetSize();
    BOOL offsetIsIl = IsIL();
    LOG((LF_CORDB, LL_INFO10000, "CCode::CreateBreakpoint, offset=%d, size=%d, IsIl=%d, this=0x%p\n",
        offset, size, offsetIsIl, this));

    // Make sure the offset is within range of the method.
    // If we're native code, then both offset & total code size are bytes of native code,
    // else they're both bytes of IL.
    if (offset >= size)
    {
        return CORDBG_E_UNABLE_TO_SET_BREAKPOINT;
    }

    CordbFunctionBreakpoint *bp = new (nothrow) CordbFunctionBreakpoint(this, offset, offsetIsIl);

    if (bp == NULL)
        return E_OUTOFMEMORY;

    hr = bp->Activate(TRUE);
    if (SUCCEEDED(hr))
    {
        *ppBreakpoint = static_cast<ICorDebugFunctionBreakpoint*> (bp);
        bp->ExternalAddRef();
        return S_OK;
    }
    else
    {
        delete bp;
        return hr;
    }
}

//-----------------------------------------------------------------------------
// CordbCode::GetCode
// Public method to get the code-bytes for this Code object. For an IL-code
// object, this will be bytes of IL. For a native-code object, this will be
// bytes of native opcodes.
// The units of the offsets are the same as the units on the CordbCode object.
// (eg, IL offsets for an IL code object, and native offsets for a native code object)
// This will glue together hot + cold regions into a single blob.
//
// Units are also logical (aka linear) values, which
// Parameters:
//    startOffset - linear offset in Code to start copying from.
//    endOffset - linear offset in Code to end copying from. Total bytes copied would be (endOffset - startOffset)
//    cBufferAlloc - number of bytes in the buffer supplied by the buffer[] parameter.
//    buffer - caller allocated storage to copy bytes into.
//    pcBufferSize - required out-parameter, holds number of bytes copied into buffer.
//
// Returns:
//    S_OK if copy successful. Else error.
//-----------------------------------------------------------------------------
HRESULT CordbCode::GetCode(ULONG32 startOffset,
                           ULONG32 endOffset,
                           ULONG32 cBufferAlloc,
                           BYTE buffer[],
                           ULONG32 *pcBufferSize)
{
    PUBLIC_REENTRANT_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT_ARRAY(buffer, BYTE, cBufferAlloc, true, true);
    VALIDATE_POINTER_TO_OBJECT(pcBufferSize, ULONG32 *);

    LOG((LF_CORDB,LL_EVERYTHING, "CC::GC: for token:0x%x\n", m_pFunction->GetMetadataToken()));

    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());

    HRESULT hr = S_OK;
    *pcBufferSize = 0;

    // Check ranges.
    ULONG32 totalSize = GetSize();

    if (cBufferAlloc < endOffset - startOffset)
        endOffset = startOffset + cBufferAlloc;

    if (endOffset > totalSize)
        endOffset = totalSize;

    if (startOffset > totalSize)
        startOffset = totalSize;

    // Check the continue counter since WriteMemory bumps it up.
    if ((m_rgbCode == NULL) ||
        (m_continueCounterLastSync < GetProcess()->m_continueCounter))
    {
        ReadCodeBytes();
        m_continueCounterLastSync = GetProcess()->m_continueCounter;
    }

    // if we just got the code, we'll have to copy it over
    if (*pcBufferSize == 0 && m_rgbCode != NULL)
    {
        memcpy(buffer,
               m_rgbCode+startOffset,
               endOffset - startOffset);
        *pcBufferSize = endOffset - startOffset;
    }
    return hr;

} // CordbCode::GetCode

#include "dbgipcevents.h"

//-----------------------------------------------------------------------------
// CordbCode::GetVersionNumber
// Public method to get the EnC version number of the code.
//
// Parameters:
//    nVersion - OUT: on return, set to the version number.
//
// Returns:
//    S_OK on success.
//-----------------------------------------------------------------------------
HRESULT CordbCode::GetVersionNumber( ULONG32 *nVersion)
{
    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT(nVersion, ULONG32 *);

    LOG((LF_CORDB,LL_INFO10000,"R:CC:GVN:Returning 0x%x "
        "as version\n",m_nVersion));

    *nVersion = (ULONG32)m_nVersion;

#ifndef EnC_SUPPORTED
    _ASSERTE(*nVersion == 1);
#endif // EnC_SUPPORTED

    return S_OK;
}

// get the CordbFunction instance for this code object
CordbFunction * CordbCode::GetFunction()
{
    _ASSERTE(m_pFunction != NULL);
    return m_pFunction;
}

/* ------------------------------------------------------------------------- *
 * CordbILCode class
 * ------------------------------------------------------------------------- */

//-----------------------------------------------------------------------------
// CordbILCode ctor to make IL code.
// Arguments:
//    Input:
//        pFunction      - pointer to the CordbFunction instance for this function
//        codeRegionInfo - starting address and size in bytes of IL code blob
//        nVersion       - EnC version number for this IL code blob
//        localVarSigToken - LocalVarSig for this IL blob
//        id             - the key when using ILCode in a CordbHashTable
//    Output:
//        fields of this instance of CordbILCode have been initialized
//-----------------------------------------------------------------------------
CordbILCode::CordbILCode(CordbFunction * pFunction,
                         TargetBuffer    codeRegionInfo,
                         SIZE_T          nVersion,
                         mdSignature     localVarSigToken,
                         UINT_PTR        id)
  : CordbCode(pFunction, id, nVersion, TRUE),
#ifdef EnC_SUPPORTED
    m_fIsOld(FALSE),
#endif
    m_codeRegionInfo(codeRegionInfo),
    m_localVarSigToken(localVarSigToken)
{
} // CordbILCode::CordbILCode


#ifdef EnC_SUPPORTED
//-----------------------------------------------------------------------------
// CordbILCode::MakeOld
// Internal method to perform any cleanup necessary when a code blob is no longer
// the most current.
//-----------------------------------------------------------------------------
void CordbILCode::MakeOld()
{
    m_fIsOld = TRUE;
}
#endif

//-----------------------------------------------------------------------------
// CordbILCode::GetAddress
// Public method to get the Entry address for the code.  This is the address
// where the method first starts executing.
//
// Parameters:
//    pStart - out-parameter to hold start address.
//
// Returns:
//    S_OK if *pStart is properly updated.
//-----------------------------------------------------------------------------
HRESULT CordbILCode::GetAddress(CORDB_ADDRESS * pStart)
{
    PUBLIC_REENTRANT_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT(pStart, CORDB_ADDRESS *);


    _ASSERTE(this != NULL);
    _ASSERTE(this->GetFunction() != NULL);
    _ASSERTE(this->GetFunction()->GetModule() != NULL);
    _ASSERTE(this->GetFunction()->GetModule()->GetProcess() == GetProcess());

    *pStart = (m_codeRegionInfo.pAddress);

    return S_OK;
} // CordbILCode::GetAddress

//-----------------------------------------------------------------------------
// CordbILCode::ReadCodeBytes
// Reads the actual bytes of IL code into the data member m_rgbCode
// Arguments:
//    none (uses data members)
// Return value:
//    standard HRESULT values
//    also allocates and initializes m_rgbCode
// Notes: assumes that the caller has checked to ensure that m_rgbCode doesn't
//    hold valid data
//-----------------------------------------------------------------------------
HRESULT CordbILCode::ReadCodeBytes()
{
    HRESULT hr = S_OK;
    EX_TRY
    {
        // We have an address & size, so we'll just call ReadMemory.
        // This will conveniently strip out any patches too.
        CORDB_ADDRESS pStart = m_codeRegionInfo.pAddress;
        ULONG32 cbSize = (ULONG32) m_codeRegionInfo.cbSize;

        delete [] m_rgbCode;
        m_rgbCode = new BYTE[cbSize];    // throws

        SIZE_T cbRead;
        hr = GetProcess()->ReadMemory(pStart, cbSize, m_rgbCode, &cbRead);
        IfFailThrow(hr);

        SIMPLIFYING_ASSUMPTION(cbRead == cbSize);
    }
    EX_CATCH_HRESULT(hr);
    return hr;
} // CordbILCode::ReadCodeBytes

//-----------------------------------------------------------------------------
// CordbILCode::GetILToNativeMapping
// Public method (implements ICorDebugCode) to get the IL-->{ Native Start, Native End} mapping.
// Since 1 CordbILCode can map to multiple CordbNativeCode due to generics, we cannot reliably return the
// mapping information in all cases.  So we always fail with CORDBG_E_NON_NATIVE_FRAME.  The caller should
// call code:CordbNativeCode::GetILToNativeMapping instead.
//
// Parameters:
//    cMap - size of incoming map[] array (in elements).
//    pcMap - OUT: full size of IL-->Native map (in elements).
//    map - caller allocated array to be filled in.
//
// Returns:
//    CORDBG_E_NON_NATIVE_FRAME in all cases
//-----------------------------------------------------------------------------
HRESULT CordbILCode::GetILToNativeMapping(ULONG32                    cMap,
                                          ULONG32 *                  pcMap,
                                          COR_DEBUG_IL_TO_NATIVE_MAP map[])
{
    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT_OR_NULL(pcMap, ULONG32 *);
    VALIDATE_POINTER_TO_OBJECT_ARRAY_OR_NULL(map, COR_DEBUG_IL_TO_NATIVE_MAP *, cMap, true, true);

    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());

    return CORDBG_E_NON_NATIVE_FRAME;
} // CordbILCode::GetILToNativeMapping


/*
* CordbILCode::GetLocalVarSig
*
* Get the method's local variable metadata signature. This may be cached, but for dynamic modules we'll always
* read it from the metadata. This function also returns the count of local variables in the method.
*
* Parameters:
*    pLocalSigParser - OUT: the local variable signature for the method.
*    pLocalCount - OUT: the number of locals the method has.
*
* Returns:
*    HRESULT for success or failure.
*
*/
HRESULT CordbILCode::GetLocalVarSig(SigParser *pLocalSigParser,
    ULONG *pLocalVarCount)
{
    INTERNAL_SYNC_API_ENTRY(GetProcess());

    CONTRACTL  // @dbgtodo  exceptions - convert to throws...
    {
        NOTHROW;
    }
    CONTRACTL_END;

    FAIL_IF_NEUTERED(this);
    HRESULT hr = S_OK;

    // A function will not have a local var sig if it has no locals!
    if (m_localVarSigToken != mdSignatureNil)
    {
        PCCOR_SIGNATURE localSignature;
        ULONG size;
        uint32_t localCount;

        EX_TRY // // @dbgtodo  exceptions  - push this up
        {
            GetFunction()->GetModule()->UpdateMetaDataCacheIfNeeded(m_localVarSigToken);
            hr = GetFunction()->GetModule()->GetMetaDataImporter()->GetSigFromToken(m_localVarSigToken,
                &localSignature,
                &size);
        }
        EX_CATCH_HRESULT(hr);
        if (FAILED(hr))
        {
            LOG((LF_CORDB, LL_WARNING, "CICF::GLVS caught hr=0x%x\n", hr));
        }
        IfFailRet(hr);

        LOG((LF_CORDB, LL_INFO100000, "CIC::GLVS creating sig parser sig=0x%x size=0x%x\n", localSignature, size));
        SigParser sigParser = SigParser(localSignature, size);

        uint32_t data;

        IfFailRet(sigParser.GetCallingConvInfo(&data));

        _ASSERTE(data == IMAGE_CEE_CS_CALLCONV_LOCAL_SIG);

        // Snagg the count of locals in the sig.
        IfFailRet(sigParser.GetData(&localCount));
        LOG((LF_CORDB, LL_INFO100000, "CIC::GLVS localCount=0x%x\n", localCount));
        if (pLocalSigParser != NULL)
        {
            *pLocalSigParser = sigParser;
        }
        if (pLocalVarCount != NULL)
        {
            *pLocalVarCount = localCount;
        }
    }
    else
    {
        //
        // Signature is Nil, so fill in everything with NULLs and zeros
        //
        if (pLocalSigParser != NULL)
        {
            *pLocalSigParser = SigParser(NULL, 0);
        }

        if (pLocalVarCount != NULL)
        {
            *pLocalVarCount = 0;
        }
    }
    LOG((LF_CORDB, LL_INFO100000, "CIC::GLVS returning hr=0x%x\n", hr));
    return hr;
}

//-----------------------------------------------------------------------------
// CordbILCode::GetLocalVariableType
// Internal method. Return the type of an IL local, specified by 0-based index.
//
// Parameters:
//   dwIndex - 0-based index for IL local number.
//   inst - instantiation information if this is a generic function. Eg,
//           if function is List<T>, inst describes T.
//   res - out parameter, yields to CordbType of the local.
//
// Return:
//   S_OK on success.
//
HRESULT CordbILCode::GetLocalVariableType(DWORD dwIndex,
    const Instantiation * pInst,
    CordbType ** ppResultType)
{
    ATT_ALLOW_LIVE_DO_STOPGO(GetProcess());
    LOG((LF_CORDB, LL_INFO10000, "CIC::GLVT dwIndex=0x%x pInst=0x%p\n", dwIndex, pInst));
    HRESULT hr = S_OK;

    EX_TRY
    {
        // Get the local variable signature.
        SigParser sigParser;
        ULONG cLocals;

        IfFailThrow(GetLocalVarSig(&sigParser, &cLocals));

        // Check the index.
        if (dwIndex >= cLocals)
        {
            ThrowHR(E_INVALIDARG);
        }

        // Run the signature and find the required argument.
        for (unsigned int i = 0; i < dwIndex; i++)
        {
            LOG((LF_CORDB, LL_INFO10000, "CIC::GLVT scanning index 0x%x\n", dwIndex));
            IfFailThrow(sigParser.SkipExactlyOne());
        }

        hr = CordbType::SigToType(GetFunction()->GetModule(), &sigParser, pInst, ppResultType);
        LOG((LF_CORDB, LL_INFO10000, "CIC::GLVT CT::SigToType returned hr=0x%x\n", hr));
        IfFailThrow(hr);

    } EX_CATCH_HRESULT(hr);
    return hr;
}

mdSignature CordbILCode::GetLocalVarSigToken()
{
    return m_localVarSigToken;
}

HRESULT CordbILCode::CreateNativeBreakpoint(ICorDebugFunctionBreakpoint **ppBreakpoint)
{
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT(ppBreakpoint, ICorDebugFunctionBreakpoint **);

    HRESULT hr;
    ULONG32 size = GetSize();
    LOG((LF_CORDB, LL_INFO10000, "CordbILCode::CreateNativeBreakpoint, size=%d, this=0x%p\n",
        size, this));

    ULONG32 offset = 0;
    CordbFunctionBreakpoint *bp = new (nothrow) CordbFunctionBreakpoint(this, offset, FALSE);

    if (bp == NULL)
    {
        return E_OUTOFMEMORY;
    }

    hr = bp->Activate(TRUE);
    if (SUCCEEDED(hr))
    {
        *ppBreakpoint = static_cast<ICorDebugFunctionBreakpoint*> (bp);
        bp->ExternalAddRef();
        return S_OK;
    }
    else
    {
        delete bp;
        return hr;
    }
}



CordbReJitILCode::CordbReJitILCode(CordbFunction *pFunction, SIZE_T encVersion, VMPTR_ILCodeVersionNode vmILCodeVersionNode) :
CordbILCode(pFunction, TargetBuffer(), encVersion, mdSignatureNil, VmPtrToCookie(vmILCodeVersionNode)),
m_cClauses(0),
m_cbLocalIL(0),
m_cILMap(0)
{
    _ASSERTE(!vmILCodeVersionNode.IsNull());
    DacSharedReJitInfo data = { 0 };
    IfFailThrow(GetProcess()->GetDAC()->GetILCodeVersionNodeData(vmILCodeVersionNode, &data));
    IfFailThrow(Init(&data));
}

//-----------------------------------------------------------------------------
// CordbReJitILCode::Init
//
// Returns:
//    S_OK if all fields are inited. Else error.
HRESULT CordbReJitILCode::Init(DacSharedReJitInfo* pSharedReJitInfo)
{
    HRESULT hr = S_OK;

    // Instrumented IL map
    if (pSharedReJitInfo->m_cInstrumentedMapEntries)
    {
        if (pSharedReJitInfo->m_cInstrumentedMapEntries > 100000)
            return CORDBG_E_TARGET_INCONSISTENT;
        m_cILMap = pSharedReJitInfo->m_cInstrumentedMapEntries;
        m_pILMap = new (nothrow)COR_IL_MAP[m_cILMap];
        TargetBuffer mapBuffer(pSharedReJitInfo->m_rgInstrumentedMapEntries, m_cILMap*sizeof(COR_IL_MAP));
        IfFailRet(GetProcess()->SafeReadBuffer(mapBuffer, (BYTE*)m_pILMap.GetValue(), FALSE /* bThrowOnError */));
    }

    // Read the method's IL header
    CORDB_ADDRESS pIlHeader = pSharedReJitInfo->m_pbIL;
    IMAGE_COR_ILMETHOD_FAT header = { 0 };
    bool headerMustBeTiny = false;
    ULONG32 headerSize = 0;
    hr = GetProcess()->SafeReadStruct(pIlHeader, &header);
    if (hr != S_OK)
    {
        // Its possible the header is tiny and there isn't enough memory to read a complete
        // FAT header
        headerMustBeTiny = true;
        IfFailRet(GetProcess()->SafeReadStruct(pIlHeader, (IMAGE_COR_ILMETHOD_TINY *)&header));
    }

    // Read the ILCodeSize and LocalVarSigTok from header
    ULONG32 ilCodeSize = 0;
    IMAGE_COR_ILMETHOD_TINY *pMethodTinyHeader = (IMAGE_COR_ILMETHOD_TINY *)&header;
    bool isTinyHeader = ((pMethodTinyHeader->Flags_CodeSize & (CorILMethod_FormatMask >> 1)) == CorILMethod_TinyFormat);
    if (isTinyHeader)
    {
        ilCodeSize = (((unsigned)pMethodTinyHeader->Flags_CodeSize) >> (CorILMethod_FormatShift - 1));
        headerSize = sizeof(IMAGE_COR_ILMETHOD_TINY);
        m_localVarSigToken = mdSignatureNil;
    }
    else if (headerMustBeTiny)
    {
        // header was not CorILMethod_TinyFormat
        // this is not possible, must be an error when reading from data target
        return CORDBG_E_READVIRTUAL_FAILURE;
    }
    else
    {
        ilCodeSize = header.CodeSize;
        headerSize = header.Size * 4;
        m_localVarSigToken = header.LocalVarSigTok;
    }
    if (ilCodeSize == 0 || ilCodeSize > 100000)
    {
        return CORDBG_E_TARGET_INCONSISTENT;
    }

    m_codeRegionInfo.Init(pIlHeader + headerSize, ilCodeSize);
    m_pLocalIL = new (nothrow) BYTE[ilCodeSize];
    if (m_pLocalIL == NULL)
        return E_OUTOFMEMORY;
    m_cbLocalIL = ilCodeSize;
    IfFailRet(GetProcess()->SafeReadBuffer(m_codeRegionInfo, m_pLocalIL, FALSE /*throwOnError*/));

    // Check if this il code has exception clauses
    if ((pMethodTinyHeader->Flags_CodeSize & CorILMethod_MoreSects) == 0)
    {
        return S_OK; // no EH, done initing
    }

    // EH section starts at the 4 byte aligned address after the code
    CORDB_ADDRESS ehClauseHeader = ((pIlHeader + headerSize + ilCodeSize - 1) & ~3) + 4;
    BYTE kind = 0;
    IfFailRet(GetProcess()->SafeReadStruct(ehClauseHeader, &kind));
    if ((kind & CorILMethod_Sect_KindMask) != CorILMethod_Sect_EHTable)
    {
        return S_OK;
    }
    if (kind & CorILMethod_Sect_FatFormat)
    {
        // Read the section header to see how many clauses there are
        IMAGE_COR_ILMETHOD_SECT_FAT sectionHeader = { 0 };
        IfFailRet(GetProcess()->SafeReadStruct(ehClauseHeader, &sectionHeader));
        m_cClauses = (sectionHeader.DataSize - 4) / sizeof(IMAGE_COR_ILMETHOD_SECT_EH_CLAUSE_FAT);
        if (m_cClauses > 10000) // sanity check the data before allocating
        {
            return CORDBG_E_TARGET_INCONSISTENT;
        }

        // Read in the clauses
        TargetBuffer buffer(ehClauseHeader + sizeof(IMAGE_COR_ILMETHOD_SECT_FAT), m_cClauses*sizeof(IMAGE_COR_ILMETHOD_SECT_EH_CLAUSE_FAT));
        NewArrayHolder<IMAGE_COR_ILMETHOD_SECT_EH_CLAUSE_FAT> pClauses = new (nothrow)IMAGE_COR_ILMETHOD_SECT_EH_CLAUSE_FAT[m_cClauses];
        if (pClauses == NULL)
            return E_OUTOFMEMORY;
        IfFailRet(GetProcess()->SafeReadBuffer(buffer, (BYTE*)pClauses.GetValue(), FALSE /*throwOnError*/));

        // convert clauses
        m_pClauses = new (nothrow)CorDebugEHClause[m_cClauses];
        if (m_pClauses == NULL)
            return E_OUTOFMEMORY;
        for (ULONG32 i = 0; i < m_cClauses; i++)
        {
            BOOL isFilter = ((pClauses[i].Flags & COR_ILEXCEPTION_CLAUSE_FILTER) != 0);
            m_pClauses[i].Flags = pClauses[i].Flags;
            m_pClauses[i].TryOffset = pClauses[i].TryOffset;
            m_pClauses[i].TryLength = pClauses[i].TryLength;
            m_pClauses[i].HandlerOffset = pClauses[i].HandlerOffset;
            m_pClauses[i].HandlerLength = pClauses[i].HandlerLength;
            // these two fields are a union in the image, but are seperate in the struct ICorDebug returns
            m_pClauses[i].ClassToken = isFilter ? 0 : pClauses[i].ClassToken;
            m_pClauses[i].FilterOffset = isFilter ? pClauses[i].FilterOffset : 0;
        }
    }
    else
    {
        // Read in the section header to see how many small clauses there are
        IMAGE_COR_ILMETHOD_SECT_SMALL sectionHeader = { 0 };
        IfFailRet(GetProcess()->SafeReadStruct(ehClauseHeader, &sectionHeader));
        ULONG32 m_cClauses = (sectionHeader.DataSize - 4) / sizeof(IMAGE_COR_ILMETHOD_SECT_SMALL);
        if (m_cClauses > 10000) // sanity check the data before allocating
        {
            return CORDBG_E_TARGET_INCONSISTENT;
        }

        // Read in the clauses
        TargetBuffer buffer(ehClauseHeader + sizeof(IMAGE_COR_ILMETHOD_SECT_SMALL), m_cClauses*sizeof(IMAGE_COR_ILMETHOD_SECT_EH_CLAUSE_SMALL));
        NewArrayHolder<IMAGE_COR_ILMETHOD_SECT_EH_CLAUSE_SMALL> pClauses = new (nothrow)IMAGE_COR_ILMETHOD_SECT_EH_CLAUSE_SMALL[m_cClauses];
        if (pClauses == NULL)
            return E_OUTOFMEMORY;
        IfFailRet(GetProcess()->SafeReadBuffer(buffer, (BYTE*)pClauses.GetValue(), FALSE /*throwOnError*/));

        // convert clauses
        m_pClauses = new (nothrow)CorDebugEHClause[m_cClauses];
        if (m_pClauses == NULL)
            return E_OUTOFMEMORY;
        for (ULONG32 i = 0; i < m_cClauses; i++)
        {
            BOOL isFilter = ((pClauses[i].Flags & COR_ILEXCEPTION_CLAUSE_FILTER) != 0);
            m_pClauses[i].Flags = pClauses[i].Flags;
            m_pClauses[i].TryOffset = pClauses[i].TryOffset;
            m_pClauses[i].TryLength = pClauses[i].TryLength;
            m_pClauses[i].HandlerOffset = pClauses[i].HandlerOffset;
            m_pClauses[i].HandlerLength = pClauses[i].HandlerLength;
            // these two fields are a union in the image, but are seperate in the struct ICorDebug returns
            m_pClauses[i].ClassToken = isFilter ? 0 : pClauses[i].ClassToken;
            m_pClauses[i].FilterOffset = isFilter ? pClauses[i].FilterOffset : 0;
        }
    }
    return S_OK;
}

#ifndef MIN
#define MIN(a,b) ((a) < (b) ? (a) : (b))
#endif

//-----------------------------------------------------------------------------
// CordbReJitILCode::GetEHClauses
// Public method to get the EH clauses for IL code
//
// Parameters:
//   cClauses - size of incoming clauses array (in elements).
//   pcClauses - OUT param: cClauses>0 -> the number of elements written to in the clauses array.
//                          cClauses=0 -> the number of EH clauses this IL code has
//   clauses - caller allocated storage to hold the EH clauses.
//
// Returns:
//    S_OK if successfully copied elements to clauses array.
HRESULT CordbReJitILCode::GetEHClauses(ULONG32 cClauses, ULONG32 * pcClauses, CorDebugEHClause clauses[])
{
    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT_OR_NULL(pcClauses, ULONG32 *);
    VALIDATE_POINTER_TO_OBJECT_ARRAY_OR_NULL(clauses, CorDebugEHClause *, cClauses, true, true);
    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());

    if (cClauses != 0 && clauses == NULL)
    {
        return E_INVALIDARG;
    }

    if (pcClauses != NULL)
    {
        if (cClauses == 0)
        {
            *pcClauses = m_cClauses;
        }
        else
        {
            *pcClauses = MIN(cClauses, m_cClauses);
        }
    }

    if (clauses != NULL)
    {
        memcpy_s(clauses, sizeof(CorDebugEHClause)*cClauses, m_pClauses, sizeof(CorDebugEHClause)*MIN(cClauses, m_cClauses));
    }
    return S_OK;
}

ULONG CordbReJitILCode::AddRef()
{
    return CordbCode::AddRef();
}
ULONG CordbReJitILCode::Release()
{
    return CordbCode::Release();
}

HRESULT CordbReJitILCode::QueryInterface(REFIID riid, void** ppInterface)
{
    if (riid == IID_ICorDebugILCode)
    {
        *ppInterface = static_cast<ICorDebugILCode*>(this);
    }
    else if (riid == IID_ICorDebugILCode2)
    {
        *ppInterface = static_cast<ICorDebugILCode2*>(this);
    }
    else
    {
        return CordbILCode::QueryInterface(riid, ppInterface);
    }

    AddRef();
    return S_OK;
}

HRESULT CordbReJitILCode::GetLocalVarSigToken(mdSignature *pmdSig)
{
    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT(pmdSig, mdSignature *);
    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());

    *pmdSig = m_localVarSigToken;
    return S_OK;
}

HRESULT CordbReJitILCode::GetInstrumentedILMap(ULONG32 cMap, ULONG32 *pcMap, COR_IL_MAP map[])
{
    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT_OR_NULL(pcClauses, ULONG32 *);
    VALIDATE_POINTER_TO_OBJECT_ARRAY_OR_NULL(map, COR_IL_MAP *, cMap, true, true);
    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());

    if (cMap != 0 && map == NULL)
    {
        return E_INVALIDARG;
    }

    if (pcMap != NULL)
    {
        if (cMap == 0)
        {
            *pcMap = m_cILMap;
        }
        else
        {
            *pcMap = MIN(cMap, m_cILMap);
        }
    }

    if (map != NULL)
    {
        memcpy_s(map, sizeof(COR_IL_MAP)*cMap, m_pILMap, sizeof(COR_IL_MAP)*MIN(cMap, m_cILMap));
    }
    return S_OK;
}

// FindNativeInfoInILVariableArray
// Linear search through an array of NativeVarInfos, to find the variable of index dwIndex, valid
// at the given ip. Returns CORDBG_E_IL_VAR_NOT_AVAILABLE if the variable isn't valid at the given ip.
// Arguments:
//     input:  dwIndex        - variable number
//             ip             - IP
//             nativeInfoList - list of instances of NativeVarInfo
//     output: ppNativeInfo   - the element of nativeInfoList that corresponds to the IP and variable number
//                              if we find such an element or NULL otherwise
// Return value: HRESULT: returns S_OK or CORDBG_E_IL_VAR_NOT_AVAILABLE if the variable isn't found
//
HRESULT FindNativeInfoInILVariableArray(DWORD                                               dwIndex,
                                        SIZE_T                                              ip,
                                        const DacDbiArrayList<ICorDebugInfo::NativeVarInfo> * nativeInfoList,
                                        const ICorDebugInfo::NativeVarInfo **                 ppNativeInfo)
{
    _ASSERTE(ppNativeInfo != NULL);
    *ppNativeInfo = NULL;

    // A few words about this search: it must be linear, and the
    // comparison of startOffset and endOffset to ip must be
    // <=/>. startOffset points to the first instruction that will
    // make the variable's home valid. endOffset points to the first
    // instruction at which the variable's home invalid.
    int lastGoodOne = -1;
    for (unsigned int i = 0; i < (unsigned)nativeInfoList->Count(); i++)
    {
        if ((*nativeInfoList)[i].varNumber == dwIndex)
        {
            if ( (lastGoodOne == -1) ||
                 ((*nativeInfoList)[lastGoodOne].startOffset < (*nativeInfoList)[i].startOffset) )
            {
                lastGoodOne = i;
            }

            if (((*nativeInfoList)[i].startOffset <= ip) &&
                ((*nativeInfoList)[i].endOffset > ip))
            {
                *ppNativeInfo = &((*nativeInfoList)[i]);

                return S_OK;
            }
        }
    }

    // workaround:
    //
    // We didn't find the variable. Was the endOffset of the last range for this variable
    // equal to the current IP? If so, go ahead and "lie" and report that as the
    // variable's home for now.
    //
    // Rationale:
    //
    // * See TODO comment in code:Compiler::siUpdate (jit\scopeinfo.cpp). In optimized
    //     code, the JIT can report var lifetimes as being one instruction too short.
    //     This workaround makes up for that.  Example code:
    //
    //         static void foo(int x)
    //         {
    //             int b = x; // Value of "x" would not be reported in optimized code without the workaround
    //             bar(ref b);
    //         }
    //
    // * Since this is the first instruction after the last range a variable was alive,
    //     we're essentially assuming that since that instruction hasn't been executed
    //     yet, and since there isn't a new home for the variable, that the last home is
    //     still good. This actually turns out to be true 99.9% of the time, so we'll go
    //     with it for now.
    // * We've been lying like this since 1999, so surely it's safe.
    if ((lastGoodOne > -1) && ((*nativeInfoList)[lastGoodOne].endOffset == ip))
    {
        *ppNativeInfo = &((*nativeInfoList)[lastGoodOne]);
        return S_OK;
    }

    return CORDBG_E_IL_VAR_NOT_AVAILABLE;
} // FindNativeInfoInILVariableArray


// * ------------------------------------------------------------------------- *
// * Variable Enum class
// * ------------------------------------------------------------------------- *
//-----------------------------------------------------------------------------
// CordbVariableHome constructor
// Arguments:
//    Input:
//        pCode          - CordbNativeCode instance containing this variable home
//        pNativeVarInfo - native location, lifetime, and index information for
//                         this variable
//        isLocal        - indicates whether the instance is a local variable,
//                         as opposed to an argument
//        index          - the argument or slot index
//    Output:
//        fields of the CordbVariableHome instance have been initialized
//-----------------------------------------------------------------------------
CordbVariableHome::CordbVariableHome(CordbNativeCode *pCode,
                                     const ICorDebugInfo::NativeVarInfo nativeVarInfo,
                                     BOOL isLocal,
                                     ULONG index) :
    CordbBase(pCode->GetModule()->GetProcess(), 0)
{
    _ASSERTE(pCode != NULL);

    m_pCode.Assign(pCode);
    m_nativeVarInfo = nativeVarInfo;
    m_isLocal = isLocal;
    m_index = index;
}

CordbVariableHome::~CordbVariableHome()
{
    _ASSERTE(this->IsNeutered());
}

void CordbVariableHome::Neuter()
{
    m_pCode.Clear();
    CordbBase::Neuter();
}

//-----------------------------------------------------------------------------
// Public method for IUnknown::QueryInterface.
// Has standard QI semantics.
//-----------------------------------------------------------------------------
HRESULT CordbVariableHome::QueryInterface(REFIID id, void **pInterface)
{
    if (id == IID_ICorDebugVariableHome)
    {
        *pInterface = static_cast<ICorDebugVariableHome *>(this);
    }
    else if (id == IID_IUnknown)
    {
        *pInterface = static_cast<IUnknown *>(static_cast<ICorDebugVariableHome *>(this));
    }
    else
    {
        *pInterface = NULL;
        return E_NOINTERFACE;
    }

    ExternalAddRef();
    return S_OK;
}

//-----------------------------------------------------------------------------
// CordbVariableHome::GetCode
// Public method to get the Code object containing this variable home.
//
// Parameters:
//   ppCode - OUT: returns the Code object for this variable home.
//
// Returns:
//   S_OK - on success.
//-----------------------------------------------------------------------------
HRESULT CordbVariableHome::GetCode(ICorDebugCode **ppCode)
{
    PUBLIC_REENTRANT_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT(ppCode, ICorDebugCode **);
    ATT_REQUIRE_STOPPED_MAY_FAIL(m_pCode->GetProcess());

    HRESULT hr = m_pCode->QueryInterface(IID_ICorDebugCode, (LPVOID*)ppCode);

    return hr;
}

//-----------------------------------------------------------------------------
// CordbVariableHome::GetSlotIndex
// Public method to get the slot index for this variable home.
//
// Parameters:
//   pSlotIndex - OUT: returns the managed slot-index of this variable home.
//
// Returns:
//   S_OK - on success
//   E_FAIL - if the variable is not a local variable, but an argument
//-----------------------------------------------------------------------------
HRESULT CordbVariableHome::GetSlotIndex(ULONG32 *pSlotIndex)
{
    PUBLIC_REENTRANT_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT(pSlotIndex, ULONG32 *);
    ATT_REQUIRE_STOPPED_MAY_FAIL(m_pCode->GetProcess());

    if (!m_isLocal)
    {
        return E_FAIL;
    }
    *pSlotIndex = m_index;
    return S_OK;
}

//-----------------------------------------------------------------------------
// CordbVariableHome::GetArgumentIndex
// Public method to get the slot index for this variable home.
//
// Parameters:
//   pSlotIndex - OUT: returns the managed argument-index of this variable home.
//
// Returns:
//   S_OK - on success
//   E_FAIL - if the variable is not an argument, but a local variable
//-----------------------------------------------------------------------------
HRESULT CordbVariableHome::GetArgumentIndex(ULONG32 *pArgumentIndex)
{
    PUBLIC_REENTRANT_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT(pArgumentIndex, ULONG32 *);
    ATT_REQUIRE_STOPPED_MAY_FAIL(m_pCode->GetProcess());

    if (m_isLocal)
    {
        return E_FAIL;
    }
    *pArgumentIndex = m_index;
    return S_OK;
}

//-----------------------------------------------------------------------------
// CordbVariableHome::GetLiveRange
// Public method to get the native range over which this variable is live.
//
// Parameters:
//   pStartOffset - OUT: returns the logical offset at which the variable is
//                  first live
//   pEndOffset   - OUT: returns the logical offset immediately after that at
//                  which the variable is last live
//
// Returns:
//   S_OK - on success
//-----------------------------------------------------------------------------
HRESULT CordbVariableHome::GetLiveRange(ULONG32 *pStartOffset,
                                        ULONG32 *pEndOffset)
{
    PUBLIC_REENTRANT_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT(pStartOffset, ULONG32 *);
    VALIDATE_POINTER_TO_OBJECT(pEndOffset, ULONG32 *);
    ATT_REQUIRE_STOPPED_MAY_FAIL(m_pCode->GetProcess());

    *pStartOffset = m_nativeVarInfo.startOffset;
    *pEndOffset = m_nativeVarInfo.endOffset;
    return S_OK;
}

//-----------------------------------------------------------------------------
// CordbVariableHome::GetLocationType
// Public method to get the type of native location for this variable home.
//
// Parameters:
//   pLocationType - OUT: the type of native location
//
// Returns:
//   S_OK - on success
//-----------------------------------------------------------------------------
HRESULT CordbVariableHome::GetLocationType(VariableLocationType *pLocationType)
{
    PUBLIC_REENTRANT_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT(pLocationType, VariableLocationType *);
    ATT_REQUIRE_STOPPED_MAY_FAIL(m_pCode->GetProcess());

    switch (m_nativeVarInfo.loc.vlType)
    {
    case ICorDebugInfo::VLT_REG:
        *pLocationType = VLT_REGISTER;
        break;
    case ICorDebugInfo::VLT_STK:
        *pLocationType = VLT_REGISTER_RELATIVE;
        break;
    default:
        *pLocationType = VLT_INVALID;
    }
    return S_OK;
}

//-----------------------------------------------------------------------------
// CordbVariableHome::GetRegister
// Public method to get the register or base register for this variable hom.
//
// Parameters:
//   pRegister - OUT: for VLT_REGISTER location types, gives the register.
//                    for VLT_REGISTER_RELATIVE location types, gives the base
//                    register.
//
// Returns:
//   S_OK - on success
//   E_FAIL - for VLT_INVALID location types
//-----------------------------------------------------------------------------
HRESULT CordbVariableHome::GetRegister(CorDebugRegister *pRegister)
{
    PUBLIC_REENTRANT_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT(pRegister, CorDebugRegister *);
    ATT_REQUIRE_STOPPED_MAY_FAIL(m_pCode->GetProcess());

    switch (m_nativeVarInfo.loc.vlType)
    {
    case ICorDebugInfo::VLT_REG:
        *pRegister = ConvertRegNumToCorDebugRegister(m_nativeVarInfo.loc.vlReg.vlrReg);
        break;
    case ICorDebugInfo::VLT_STK:
        *pRegister = ConvertRegNumToCorDebugRegister(m_nativeVarInfo.loc.vlStk.vlsBaseReg);
        break;
    default:
        return E_FAIL;
    }
    return S_OK;
}

//-----------------------------------------------------------------------------
// CordbVariableHome::GetOffset
// Public method to get the offset from the base register for this variable home.
//
// Parameters:
//   pOffset - OUT: gives the offset from the base register
//
// Returns:
//   S_OK - on success
//   E_FAIL - for location types other than VLT_REGISTER_RELATIVE
//-----------------------------------------------------------------------------
HRESULT CordbVariableHome::GetOffset(LONG *pOffset)
{
    PUBLIC_REENTRANT_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT(pOffset, LONG *);
    ATT_REQUIRE_STOPPED_MAY_FAIL(m_pCode->GetProcess());

    switch (m_nativeVarInfo.loc.vlType)
    {
    case ICorDebugInfo::VLT_STK:
        *pOffset = m_nativeVarInfo.loc.vlStk.vlsOffset;
        break;
    default:
        return E_FAIL;
    }
    return S_OK;
}


// * ------------------------------------------------------------------------- *
// * Native Code class
// * ------------------------------------------------------------------------- */


//-----------------------------------------------------------------------------
// CordbNativeCode ctor to make Native code.
// Arguments:
//    Input:
//        pFunction              - the function for which this is the native code object
//        pJitData               - the information about this code object retrieved from the DAC
//        fIsInstantiatedGeneric - indicates whether this code object is an instantiated
//                                 generic
//    Output:
//        fields of this instance of CordbNativeCode have been initialized
//-----------------------------------------------------------------------------
CordbNativeCode::CordbNativeCode(CordbFunction *                pFunction,
                                 const NativeCodeFunctionData * pJitData,
                                 BOOL                           fIsInstantiatedGeneric)
  : CordbCode(pFunction, (UINT_PTR)pJitData->m_rgCodeRegions[kHot].pAddress, pJitData->encVersion, FALSE),
    m_vmNativeCodeMethodDescToken(pJitData->vmNativeCodeMethodDescToken),
    m_fCodeAvailable(TRUE),
    m_fIsInstantiatedGeneric(fIsInstantiatedGeneric != FALSE)
{
    _ASSERTE(GetVersion() >= CorDB_DEFAULT_ENC_FUNCTION_VERSION);

    for (CodeBlobRegion region = kHot; region < MAX_REGIONS; ++region)
    {
        m_rgCodeRegions[region] = pJitData->m_rgCodeRegions[region];
    }
} //CordbNativeCode::CordbNativeCode

//-----------------------------------------------------------------------------
// Public method for IUnknown::QueryInterface.
// Has standard QI semantics.
//-----------------------------------------------------------------------------
HRESULT CordbNativeCode::QueryInterface(REFIID id, void ** pInterface)
{
    if (id == IID_ICorDebugCode)
    {
        *pInterface = static_cast<ICorDebugCode *>(this);
    }
    else if (id == IID_ICorDebugCode2)
    {
        *pInterface = static_cast<ICorDebugCode2 *>(this);
    }
    else if (id == IID_ICorDebugCode3)
    {
        *pInterface = static_cast<ICorDebugCode3 *>(this);
    }
    else if (id == IID_ICorDebugCode4)
    {
        *pInterface = static_cast<ICorDebugCode4 *>(this);
    }
    else if (id == IID_IUnknown)
    {
        *pInterface = static_cast<IUnknown *>(static_cast<ICorDebugCode *>(this));
    }
    else
    {
        *pInterface = NULL;
        return E_NOINTERFACE;
    }

    ExternalAddRef();
    return S_OK;
}

//-----------------------------------------------------------------------------
// CordbNativeCode::GetAddress
// Public method to get the Entry address for the code.  This is the address
// where the method first starts executing.
//
// Parameters:
//    pStart - out-parameter to hold start address.
//
// Returns:
//    S_OK if *pStart is properly updated.
//-----------------------------------------------------------------------------
HRESULT CordbNativeCode::GetAddress(CORDB_ADDRESS * pStart)
{
    PUBLIC_REENTRANT_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT(pStart, CORDB_ADDRESS *);


    _ASSERTE(this != NULL);
    _ASSERTE(this->GetFunction() != NULL);
    _ASSERTE(this->GetFunction()->GetModule() != NULL);
    _ASSERTE(this->GetFunction()->GetModule()->GetProcess() == GetProcess());

    // Since we don't do code-pitching, the address points directly to the code.
    *pStart = (m_rgCodeRegions[kHot].pAddress);

    if (*pStart == NULL)
    {
        return CORDBG_E_CODE_NOT_AVAILABLE;
    }
    return S_OK;
} // CordbNativeCode::GetAddress

//-----------------------------------------------------------------------------
// CordbNativeCode::ReadCodeBytes
// Reads the actual bytes of native code from both the hot and cold regions
// into the data member m_rgbCode
// Arguments:
//    none (uses data members)
// Return value:
//    standard HRESULT values
//    also allocates and initializes m_rgbCode
// Notes: assumes that the caller has checked to ensure that m_rgbCode doesn't
//    hold valid data
//-----------------------------------------------------------------------------
HRESULT CordbNativeCode::ReadCodeBytes()
{
    HRESULT hr = S_OK;

    EX_TRY
    {
        // We have an address & size, so we'll just call ReadMemory.
        // This will conveniently strip out any patches too.
        CORDB_ADDRESS pHotStart = m_rgCodeRegions[kHot].pAddress;
        CORDB_ADDRESS pColdStart = m_rgCodeRegions[kCold].pAddress;
        ULONG32 cbHotSize = (ULONG32) m_rgCodeRegions[kHot].cbSize;
        ULONG32 cbColdSize = GetColdSize();

        delete [] m_rgbCode;
        m_rgbCode = new BYTE[cbHotSize + cbColdSize];

        SIZE_T cbRead;
        hr = GetProcess()->ReadMemory(pHotStart, cbHotSize, m_rgbCode, &cbRead);
        IfFailThrow(hr);

        SIMPLIFYING_ASSUMPTION(cbRead == cbHotSize);

        if (HasColdRegion())
        {
            hr = GetProcess()->ReadMemory(pColdStart, cbColdSize, (BYTE *) m_rgbCode + cbHotSize, &cbRead);
            IfFailThrow(hr);

            SIMPLIFYING_ASSUMPTION(cbRead == cbColdSize);
        }
    }
    EX_CATCH_HRESULT(hr);
    return hr;

} // CordbNativeCode::ReadCodeBytes

//-----------------------------------------------------------------------------
// CordbNativeCode::GetColdSize
// Get the size of the cold regions in bytes.
//
// Parameters:
//   none--uses data member m_rgCodeRegions to compute total size.
//
// Returns:
//   the size of the code in bytes.
//-----------------------------------------------------------------------------
ULONG32 CordbNativeCode::GetColdSize()
{
    ULONG32 pcBytes = 0;
    for (CodeBlobRegion index = kCold; index < MAX_REGIONS; ++index)
    {
        pcBytes += m_rgCodeRegions[index].cbSize;
    }
    return pcBytes;
} // CordbNativeCode::GetColdSize

//-----------------------------------------------------------------------------
// CordbNativeCode::GetSize
// Get the size of the code in bytes.
//
// Parameters:
//   none--uses data member m_rgCodeRegions to compute total size.
//
// Returns:
//   the size of the code in bytes.
//-----------------------------------------------------------------------------
ULONG32 CordbNativeCode::GetSize()
{
    ULONG32 pcBytes = 0;
    for (CodeBlobRegion index = kHot; index < MAX_REGIONS; ++index)
    {
        pcBytes += m_rgCodeRegions[index].cbSize;
    }
    return pcBytes;
} // CordbNativeCode::GetSize

//-----------------------------------------------------------------------------
// CordbNativeCode::GetILToNativeMapping
// Public method (implements ICorDebugCode) to get the IL-->{ Native Start, Native End} mapping.
// This can only be retrieved for native code.
// This will copy as much of the map as can fit in the incoming buffer.
//
// Parameters:
//    cMap - size of incoming map[] array (in elements).
//    pcMap - OUT: full size of IL-->Native map (in elements).
//    map - caller allocated array to be filled in.
//
// Returns:
//    S_OK on successful copying.
//-----------------------------------------------------------------------------
HRESULT CordbNativeCode::GetILToNativeMapping(ULONG32                    cMap,
                                              ULONG32 *                  pcMap,
                                              COR_DEBUG_IL_TO_NATIVE_MAP map[])
{
    PUBLIC_REENTRANT_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT_OR_NULL(pcMap, ULONG32 *);
    VALIDATE_POINTER_TO_OBJECT_ARRAY_OR_NULL(map, COR_DEBUG_IL_TO_NATIVE_MAP *,cMap,true,true);

    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());

    HRESULT hr = S_OK;
    EX_TRY
    {
        LoadNativeInfo();

        SequencePoints * pSeqPts = GetSequencePoints();
        DebuggerILToNativeMap * rgMapInt = pSeqPts->GetMapAddr();
        ULONG32 cMapIntCount = pSeqPts->GetEntryCount();

        // If they gave us space to copy into...
        if (map != NULL)
        {
            // Only copy as much as either they gave us or we have to copy.
            ULONG32 cMapToCopy = min(cMap, cMapIntCount);

            // Remember that we need to translate between our internal DebuggerILToNativeMap and the external
            // COR_DEBUG_IL_TO_NATIVE_MAP!
            ULONG32 size = GetSize();
            ExportILToNativeMap(cMapToCopy, map, rgMapInt, size);
        }

        // return the full count of map entries
        if (pcMap)
        {
            *pcMap = cMapIntCount;
        }
    }
    EX_CATCH_HRESULT(hr);
    return hr;
} // CordbNativeCode::GetILToNativeMapping

//-----------------------------------------------------------------------------
// CordbNativeCode::GetCodeChunks
// Public method to get the code regions of code. If the code
// is broken into discontinuous regions (hot + cold), this lets a debugger
// find the number of regions, and (start,size) of each.
//
// Parameters:
//   cbufSize - size of incoming chunks array (in elements).
//   pcnumChunks - OUT param: the number of elements written to in the chunk array.//
//   chunks - caller allocated storage to hold the code chunks.
//
// Returns:
//    S_OK if successfully copied elements to Chunk array.
//-----------------------------------------------------------------------------
HRESULT CordbNativeCode::GetCodeChunks(
    ULONG32 cbufSize,
    ULONG32 * pcnumChunks,
    CodeChunkInfo chunks[]
)
{
    PUBLIC_API_ENTRY(this);

    if (pcnumChunks == NULL)
    {
        return E_INVALIDARG;
    }
    if ((chunks == NULL) != (cbufSize == 0))
    {
        return E_INVALIDARG;
    }

    // Current V2.0 implementation has at most 2 possible chunks right now (1 hot, and 1 cold).
    ULONG32 cActualChunks = HasColdRegion() ? 2 : 1;

    // If no buf size, then we're querying the total number of chunks.
    if (cbufSize == 0)
    {
        *pcnumChunks = cActualChunks;
        return S_OK;
    }

    // Else give them as many as they asked for.
    for (CodeBlobRegion index = kHot; (index < MAX_REGIONS) && ((int)cbufSize > index); ++index)
    {
        // Fill in the region information
        chunks[index].startAddr = m_rgCodeRegions[index].pAddress;
        chunks[index].length = (ULONG32) (m_rgCodeRegions[index].cbSize);
        *pcnumChunks = cbufSize;
    }

    return S_OK;
} // CordbNativeCode::GetCodeChunks

//-----------------------------------------------------------------------------
// CordbNativeCode::GetCompilerFlags
// Public entry point to get code flags for this Code object.
// Originally, ICDCode had this method implemented independently from the
// ICDModule method GetJitCompilerFlags. This was because it was considered that
// the flags would be per function, rather than per module.
// In addition, GetCompilerFlags did two different things depending on whether
// the code had a native image. It turned out that was the wrong thing to do
// .
//
// Parameters:
//    pdwFlags - OUT: code gen flags (see CorDebugJITCompilerFlags)
//
// Return value:
//    S_OK if pdwFlags is set properly.
//-----------------------------------------------------------------------------
HRESULT CordbNativeCode::GetCompilerFlags(DWORD * pdwFlags)
{
    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT(pdwFlags, DWORD *);
    *pdwFlags = 0;
    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());

    return GetFunction()->GetModule()->GetJITCompilerFlags(pdwFlags);

} // CordbNativeCode::GetCompilerFlags

//-----------------------------------------------------------------------------
// Given an IL local variable number and a native IP offset, return the
// location of the variable in jitted code.
//-----------------------------------------------------------------------------
HRESULT CordbNativeCode::ILVariableToNative(DWORD dwIndex,
                                            SIZE_T ip,
                                            const ICorDebugInfo::NativeVarInfo ** ppNativeInfo)
{
    _ASSERTE(m_nativeVarData.IsInitialized());

    return FindNativeInfoInILVariableArray(dwIndex,
                                           ip,
                                           m_nativeVarData.GetOffsetInfoList(),
                                           ppNativeInfo);
} // CordbNativeCode::ILVariableToNative


HRESULT CordbNativeCode::GetReturnValueLiveOffset(ULONG32 ILoffset, ULONG32 bufferSize, ULONG32 *pFetched, ULONG32 *pOffsets)
{
    HRESULT hr = S_OK;

    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);

    VALIDATE_POINTER_TO_OBJECT(pFetched, ULONG32 *);

    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());
    EX_TRY
    {
        hr = GetReturnValueLiveOffsetImpl(NULL, ILoffset, bufferSize, pFetched, pOffsets);
    }
    EX_CATCH_HRESULT(hr);
    return hr;
}

//-----------------------------------------------------------------------------
// CordbNativeCode::EnumerateVariableHomes
// Public method to get an enumeration of native variable homes. This may
// include multiple ICorDebugVariableHomes for the same slot or argument index
// if they have different homes at different points in the function.
//
// Parameters:
//   ppEnum - OUT: returns the enum of variable homes.
//
// Returns:
//   HRESULT for success or failure.
//-----------------------------------------------------------------------------
HRESULT CordbNativeCode::EnumerateVariableHomes(ICorDebugVariableHomeEnum **ppEnum)
{
    PUBLIC_REENTRANT_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT(ppEnum, ICorDebugVariableHomeEnum **);
    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());

    HRESULT hr = S_OK;

    // Get the argument count
    ULONG argCount = 0;
    CordbFunction *func = GetFunction();
    _ASSERTE(func != NULL);
    IfFailRet(func->GetSig(NULL, &argCount, NULL));

#ifdef _DEBUG
    // Get the number of locals
    ULONG localCount = 0;
    EX_TRY
    {
        GetFunction()->GetILCode()->GetLocalVarSig(NULL, &localCount);
    }
    EX_CATCH_HRESULT(hr);
    IfFailRet(hr);
#endif

    RSSmartPtr<CordbVariableHome> *rsHomes = NULL;

    EX_TRY
    {
        CordbProcess *pProcess = GetProcess();
        _ASSERTE(pProcess != NULL);

        const DacDbiArrayList<ICorDebugInfo::NativeVarInfo> *pOffsetInfoList = m_nativeVarData.GetOffsetInfoList();
        _ASSERTE(pOffsetInfoList != NULL);
        DWORD countHomes = 0;
        for (unsigned int i = 0; i < pOffsetInfoList->Count(); i++)
        {
            const ICorDebugInfo::NativeVarInfo *pNativeVarInfo = &((*pOffsetInfoList)[i]);
            _ASSERTE(pNativeVarInfo != NULL);

            // The variable information list can include variables
            // with special varNumbers representing, for instance, the
            // parameter types for generic methods. Here we are only
            // interested in local variables and arguments.
            if (pNativeVarInfo->varNumber < (DWORD)ICorDebugInfo::MAX_ILNUM)
            {
                countHomes++;
            }
        }
        rsHomes = new RSSmartPtr<CordbVariableHome>[countHomes];

        DWORD varHomeInd = 0;
        for (unsigned int i = 0; i < pOffsetInfoList->Count(); i++)
        {
            const ICorDebugInfo::NativeVarInfo *pNativeVarInfo = &((*pOffsetInfoList)[i]);

            // Again, only look for native var info representing local
            // variables and arguments.
            if (pNativeVarInfo->varNumber < (DWORD)ICorDebugInfo::MAX_ILNUM)
            {
                // determine whether this variable home represents and argument or local variable
                BOOL isLocal = ((ULONG)pNativeVarInfo->varNumber >= argCount);

                // determine the argument-index or slot-index of this variable home
                ULONG argOrSlotIndex;
                if (isLocal) {
                    argOrSlotIndex = pNativeVarInfo->varNumber - argCount;
                    _ASSERTE(argOrSlotIndex < localCount);
                } else {
                    argOrSlotIndex = pNativeVarInfo->varNumber;
                }

                RSInitHolder<CordbVariableHome> pCVH(new CordbVariableHome(this,
                                                                           (*pOffsetInfoList)[i],
                                                                           isLocal,
                                                                           argOrSlotIndex));
                pProcess->GetContinueNeuterList()->Add(pProcess, pCVH);
                _ASSERTE(varHomeInd < countHomes);
                rsHomes[varHomeInd].Assign(pCVH);
                pCVH.ClearAndMarkDontNeuter();
                varHomeInd++;
            }
        }

        RSInitHolder<CordbVariableHomeEnumerator> pCDVHE(
            new CordbVariableHomeEnumerator(GetProcess(), &rsHomes, countHomes));
        pProcess->GetContinueNeuterList()->Add(pProcess, pCDVHE);
        pCDVHE.TransferOwnershipExternal(ppEnum);
    }
    EX_CATCH_HRESULT(hr);

    return hr;
}

int CordbNativeCode::GetCallInstructionLength(BYTE *ip, ULONG32 count)
{
#if defined(TARGET_ARM)
    if (Is32BitInstruction(*(WORD*)ip))
        return 4;
    else
        return 2;
#elif defined(TARGET_ARM64)
    return MAX_INSTRUCTION_LENGTH;
#elif defined(TARGET_X86)
    if (count < 2)
        return -1;

    // Skip instruction prefixes
    do
    {
        switch (*ip)
        {
            // Segment overrides
        case 0x26: // ES
        case 0x2E: // CS
        case 0x36: // SS
        case 0x3E: // DS
        case 0x64: // FS
        case 0x65: // GS

            // Size overrides
        case 0x66: // Operand-Size
        case 0x67: // Address-Size

            // Lock
        case 0xf0:

            // String REP prefixes
        case 0xf1:
        case 0xf2: // REPNE/REPNZ
        case 0xf3:
            ip++;
            count--;
            continue;

        default:
            break;
        }
    } while (0);

    // Read the opcode
    BYTE opcode = *ip++;
    if (opcode == 0xcc)
    {
        // todo:  Can we actually get this result?  Doesn't ICorDebug hand out un-patched assembly?
        _ASSERTE(!"Hit break opcode!");
        return -1;
    }

    // Analyze what we can of the opcode
    switch (opcode)
    {
    case 0xff:
    {
                 // Count may have been decremented by prefixes.
                 if (count < 2)
                     return -1;

                 BYTE modrm = *ip++;
                 BYTE mod = (modrm & 0xC0) >> 6;
                 BYTE reg = (modrm & 0x38) >> 3;
                 BYTE rm  = (modrm & 0x07);

                 int displace = -1;

                 if ((reg != 2) && (reg != 3) && (reg != 4) && (reg != 5))
                 {
                     //
                     // This is not a CALL or JMP instruction, return, unknown.
                     //
                     _ASSERTE(!"Unhandled opcode!");
                     return -1;
                 }


                 // Only try to decode registers if we actually have reg sets.
                 switch (mod)
                 {
                 case 0:
                 case 1:
                 case 2:

                     if (rm == 4)
                     {
                         if (count < 3)
                             return -1;

                         //
                         // Get values from the SIB byte
                         //
                         BYTE ss    = (*ip & 0xC0) >> 6;
                         BYTE index = (*ip & 0x38) >> 3;
                         BYTE base  = (*ip & 0x7);

                         //
                         // Finally add in the offset
                         //
                         if (mod == 0)
                         {
                             if (base == 5)
                                 displace = 7;
                             else
                                 displace = 3;
                         }
                         else if (mod == 1)
                         {
                             displace = 4;
                         }
                         else
                         {
                             displace = 7;
                         }
                     }
                     else
                     {
                         if (mod == 0)
                         {
                             if (rm == 5)
                                 displace = 6;
                             else
                                 displace = 2;
                         }
                         else if (mod == 1)
                         {
                             displace = 3;
                         }
                         else
                         {
                             displace = 6;
                         }
                     }
                     break;

                 case 3:
                 default:
                     displace = 2;
                     break;
                 }

                 return displace;
    }  // end of 0xFF case

    case 0xe8:
        return 5;


    default:
        break;
    }


    _ASSERTE(!"Unhandled opcode!");
    return -1;

#elif defined(TARGET_AMD64)
    BYTE rex = NULL;
    BYTE prefix = *ip;
    BOOL fContainsPrefix = FALSE;

    // Should not happen.
    if (prefix == 0xcc)
        return -1;

    // Skip instruction prefixes
    //@TODO by euzem:
    //This "loop" can't be really executed more than once so if CALL can really have more than one prefix we'll crash.
    //Some of these prefixes are not allowed for CALL instruction and we should treat them as invalid code.
    //It appears that this code was mostly copy/pasted from \NDP\clr\src\Debug\EE\amd64\amd64walker.cpp
    //with very minimum fixes.
    do
    {
        switch (prefix)
        {
            // Segment overrides
        case 0x26: // ES
        case 0x2E: // CS
        case 0x36: // SS
        case 0x3E: // DS
        case 0x64: // FS
        case 0x65: // GS

            // Size overrides
        case 0x66: // Operand-Size
        case 0x67: // Address-Size

            // Lock
        case 0xf0:

            // String REP prefixes
        case 0xf2: // REPNE/REPNZ
        case 0xf3:
            ip++;
            fContainsPrefix = TRUE;
            continue;

            // REX register extension prefixes
        case 0x40:
        case 0x41:
        case 0x42:
        case 0x43:
        case 0x44:
        case 0x45:
        case 0x46:
        case 0x47:
        case 0x48:
        case 0x49:
        case 0x4a:
        case 0x4b:
        case 0x4c:
        case 0x4d:
        case 0x4e:
        case 0x4f:
            // make sure to set rex to prefix, not *ip because *ip still represents the
            // codestream which has a 0xcc in it.
            rex = prefix;
            ip++;
            fContainsPrefix = TRUE;
            continue;

        default:
            break;
        }
    } while (0);

    // Read the opcode
    BYTE opcode = *ip++;

    // Should not happen.
    if (opcode == 0xcc)
        return -1;


    // Setup rex bits if needed
    BYTE rex_b = 0;
    BYTE rex_x = 0;
    BYTE rex_r = 0;

    if (rex != NULL)
    {
        rex_b = (rex & 0x1);       // high bit to modrm r/m field or SIB base field or OPCODE reg field    -- Hmm, when which?
        rex_x = (rex & 0x2) >> 1;  // high bit to sib index field
        rex_r = (rex & 0x4) >> 2;  // high bit to modrm reg field
    }

    // Analyze what we can of the opcode
    switch (opcode)
    {
    case 0xff:
    {
                 BYTE modrm = *ip++;

                 _ASSERT(modrm != NULL);

                 BYTE mod = (modrm & 0xC0) >> 6;
                 BYTE reg = (modrm & 0x38) >> 3;
                 BYTE rm  = (modrm & 0x07);

                 reg   |= (rex_r << 3);
                 rm    |= (rex_b << 3);

                 if ((reg < 2) || (reg > 5 && reg < 8) || (reg > 15)) {
                     // not a valid register for a CALL or BRANCH
                     _ASSERTE(!"Invalid opcode!");
                     return -1;
                 }

                 SHORT displace = -1;

                 // See: Tables A-15,16,17 in AMD Dev Manual 3 for information
                 //      about how the ModRM/SIB/REX bytes interact.

                 switch (mod)
                 {
                 case 0:
                 case 1:
                 case 2:
                     if ((rm & 0x07) == 4) // we have an SIB byte following
                     {
                         //
                         // Get values from the SIB byte
                         //
                         BYTE sib   = *ip;
                         _ASSERT(sib != NULL);

                         BYTE base  = (sib & 0x07);
                         base  |= (rex_b << 3);

                         ip++;

                         //
                         // Finally add in the offset
                         //
                         if (mod == 0)
                         {
                             if ((base & 0x07) == 5)
                                 displace = 7;
                             else
                                 displace = 3;
                         }
                         else if (mod == 1)
                         {
                             displace = 4;
                         }
                         else // mod == 2
                         {
                             displace = 7;
                         }
                     }
                     else
                     {
                         //
                         // Get the value we need from the register.
                         //

                         // Check for RIP-relative addressing mode.
                         if ((mod == 0) && ((rm & 0x07) == 5))
                         {
                             displace = 6;   // 1 byte opcode + 1 byte modrm + 4 byte displacement (signed)
                         }
                         else
                         {
                             if (mod == 0)
                                 displace = 2;
                             else if (mod == 1)
                                 displace = 3;
                             else // mod == 2
                                 displace = 6;
                         }
                     }

                     break;

                 case 3:
                 default:
                     displace = 2;
                 }

                 // Displace should be set by one of the cases above
                 if (displace == -1)
                 {
                     _ASSERTE(!"GetCallInstructionLength() encountered unexpected call instruction");
                     return -1;
                 }

                 // Account for the 1 byte prefix (REX or otherwise)
                 if (fContainsPrefix)
                     displace++;

                 // reg == 4 or 5 means that it is not a CALL, but JMP instruction
                 // so we will fall back to ASSERT after break
                 if ((reg != 4) && (reg != 5))
                     return displace;
                 break;
    }
    case 0xe8:
    {
                 //Near call with the target specified by a 32-bit relative displacement.
                 //[maybe 1 byte prefix] + [1 byte opcode E8h] + [4 bytes offset]
                 return 5 + (fContainsPrefix ? 1 : 0);
    }
    default:
        break;
    }

    _ASSERTE(!"Invalid opcode!");
    return -1;
#else
#error Platform not implemented
#endif
}

HRESULT CordbNativeCode::GetSigParserFromFunction(mdToken mdFunction, mdToken *pClass, SigParser &parser, SigParser &methodGenerics)
{
    // mdFunction may be a MemberRef, a MethodDef, or a MethodSpec.  We must handle all three cases.
    HRESULT hr = S_OK;
    IMetaDataImport* pImport = m_pFunction->GetModule()->GetMetaDataImporter();
    RSExtSmartPtr<IMetaDataImport2> pImport2;
    IfFailRet(pImport->QueryInterface(IID_IMetaDataImport2, (void**)&pImport2));

    if (TypeFromToken(mdFunction) == mdtMemberRef)
    {
        PCCOR_SIGNATURE sig = 0;
        ULONG sigSize = 0;
        IfFailRet(pImport->GetMemberRefProps(mdFunction, pClass, NULL, 0, 0, &sig, &sigSize));
        parser = SigParser(sig, sigSize);
    }
    else if (TypeFromToken(mdFunction) == mdtMethodDef)
    {
        PCCOR_SIGNATURE sig = 0;
        ULONG sigSize = 0;
        IfFailRet(pImport->GetMethodProps(mdFunction, pClass, NULL, 0, NULL, NULL, &sig, &sigSize, NULL, NULL));
        parser = SigParser(sig, sigSize);
    }
    else if (TypeFromToken(mdFunction) == mdtMethodSpec)
    {
        // For a method spec, we use GetMethodSpecProps to get the generic singature and the parent token
        // (which is a MethodDef token).  We'll recurse to get the other properties from the parent token.

        PCCOR_SIGNATURE sig = 0;
        ULONG sigSize = 0;
        mdToken parentToken = 0;
        IfFailRet(pImport2->GetMethodSpecProps(mdFunction, &parentToken, &sig, &sigSize));
        methodGenerics = SigParser(sig, sigSize);

        if (pClass)
            *pClass = parentToken;

        return GetSigParserFromFunction(parentToken, pClass, parser, methodGenerics);
    }
    else
    {
        // According to ECMA III.3.19, this can never happen.
        return E_UNEXPECTED;
    }

    return S_OK;
}

HRESULT CordbNativeCode::EnsureReturnValueAllowed(Instantiation *currentInstantiation, mdToken targetClass, SigParser &parser, SigParser &methodGenerics)
{
    HRESULT hr = S_OK;
    uint32_t genCount = 0;
    IfFailRet(SkipToReturn(parser, &genCount));

    return EnsureReturnValueAllowedWorker(currentInstantiation, targetClass, parser, methodGenerics, genCount);
}

HRESULT CordbNativeCode::EnsureReturnValueAllowedWorker(Instantiation *currentInstantiation, mdToken targetClass, SigParser &parser, SigParser &methodGenerics, ULONG genCount)
{
    // There are a few considerations here:
    // 1.  Generic instantiations.  This is a "Foo<T>", and we need to check if that "Foo"
    //      fits one of the categories we disallow (such as a struct).
    // 2.  Void return.
    // 3.  ValueType - Unsupported this release.
    // 4.  MVAR - Method generics.  We need to get the actual generic type and recursively
    //      check if we allow that.
    // 5.  VAR - Class generics.  We need to get the actual generic type and recurse.

    SigParser original(parser);
    HRESULT hr = S_OK;
    CorElementType returnType;
    IfFailRet(parser.GetElemType(&returnType));
    if (returnType == ELEMENT_TYPE_GENERICINST)
    {
        IfFailRet(parser.GetElemType(&returnType));

        if (returnType == ELEMENT_TYPE_CLASS)
            return S_OK;

        if (returnType != ELEMENT_TYPE_VALUETYPE)
            return META_E_BAD_SIGNATURE;

        if (currentInstantiation == NULL)
            return S_OK;  // We will check again when we have the instantiation.

        NewArrayHolder<CordbType*> types;
        Instantiation inst;
        IfFailRet(CordbJITILFrame::BuildInstantiationForCallsite(GetModule(), types, inst, currentInstantiation, targetClass, SigParser(methodGenerics)));

        CordbType *pType = 0;
        IfFailRet(CordbType::SigToType(GetModule(), &original, &inst, &pType));


        IfFailRet(pType->ReturnedByValue());
        if (hr == S_OK) // not S_FALSE
            return S_OK;

        return CORDBG_E_UNSUPPORTED;
    }

    if (returnType == ELEMENT_TYPE_VALUETYPE)
    {
        Instantiation inst;
        CordbType *pType = 0;
        IfFailRet(CordbType::SigToType(GetModule(), &original, &inst, &pType));

        IfFailRet(pType->ReturnedByValue());
        if (hr == S_OK) // not S_FALSE
            return S_OK;

        return CORDBG_E_UNSUPPORTED;
    }

    if (returnType == ELEMENT_TYPE_TYPEDBYREF)
        return CORDBG_E_UNSUPPORTED;

    if (returnType == ELEMENT_TYPE_VOID)
        return E_UNEXPECTED;

    if (returnType == ELEMENT_TYPE_MVAR)
    {
        // Get which generic parameter is referenced.
        uint32_t genParam = 0;
        IfFailRet(parser.GetData(&genParam));

        // Grab the calling convention of the method, ensure it's GENERICINST.
        uint32_t callingConv = 0;
        IfFailRet(methodGenerics.GetCallingConvInfo(&callingConv));
        if (callingConv != IMAGE_CEE_CS_CALLCONV_GENERICINST)
            return META_E_BAD_SIGNATURE;

        // Ensure sensible bounds.
        SigParser generics(methodGenerics);     // Make a copy since operations are destructive.
        uint32_t maxCount = 0;
        IfFailRet(generics.GetData(&maxCount));
        if (maxCount <= genParam || genParam > 1024)
            return META_E_BAD_SIGNATURE;

        // Walk to the parameter referenced.
        while (genParam--)
            IfFailRet(generics.SkipExactlyOne());

        // Now recurse with "generics" at the location to continue parsing.
        return EnsureReturnValueAllowedWorker(currentInstantiation, targetClass, generics, methodGenerics, genCount);
    }


    if (returnType == ELEMENT_TYPE_VAR)
    {
        // Get which type parameter is reference.
        uint32_t typeParam = 0;
        parser.GetData(&typeParam);

        // Ensure something reasonable.
        if (typeParam > 1024)
            return META_E_BAD_SIGNATURE;

        // Lookup the containing class's signature so we can get the referenced generic parameter.
        IMetaDataImport *pImport = m_pFunction->GetModule()->GetMetaDataImporter();
        PCCOR_SIGNATURE sig;
        ULONG countSig;
        IfFailRet(pImport->GetTypeSpecFromToken(targetClass, &sig, &countSig));

        // Enusre the type's typespec is GENERICINST.
        SigParser typeParser(sig, countSig);
        CorElementType et;
        IfFailRet(typeParser.GetElemType(&et));
        if (et != ELEMENT_TYPE_GENERICINST)
            return META_E_BAD_SIGNATURE;

        // Move to the correct location.
        IfFailRet(typeParser.GetElemType(&et));
        if (et != ELEMENT_TYPE_VALUETYPE && et != ELEMENT_TYPE_CLASS)
            return META_E_BAD_SIGNATURE;

        IfFailRet(typeParser.GetToken(NULL));

        uint32_t totalTypeCount = 0;
        IfFailRet(typeParser.GetData(&totalTypeCount));
        if (totalTypeCount < typeParam)
            return META_E_BAD_SIGNATURE;

        while (typeParam--)
            IfFailRet(typeParser.SkipExactlyOne());

        // This is a temporary workaround for an infinite recursion here.  ALL of this code will
        // go away when we allow struct return values, but in the mean time this avoids a corner
        // case in the type system we haven't solved yet.
        IfFailRet(typeParser.PeekElemType(&et));
        if (et == ELEMENT_TYPE_VAR)
            return E_FAIL;

        // Now that typeParser is at the location of the correct generic parameter, recurse.
        return EnsureReturnValueAllowedWorker(currentInstantiation, targetClass, typeParser, methodGenerics, genCount);
    }

    // Everything else supported
    return S_OK;
}

HRESULT CordbNativeCode::SkipToReturn(SigParser &parser, uint32_t *genCount)
{
    // Takes a method signature parser (at the beginning of a signature) and skips to the
    // return value.
    HRESULT hr = S_OK;

    // Skip calling convention
    uint32_t uCallConv;
    IfFailRet(parser.GetCallingConvInfo(&uCallConv));
    if ((uCallConv == IMAGE_CEE_CS_CALLCONV_FIELD) || (uCallConv == IMAGE_CEE_CS_CALLCONV_LOCAL_SIG))
        return META_E_BAD_SIGNATURE;

    // Skip type parameter count if function is generic
    if (uCallConv & IMAGE_CEE_CS_CALLCONV_GENERIC)
        IfFailRet(parser.GetData(genCount));

    // Skip argument count
    IfFailRet(parser.GetData(NULL));

    return S_OK;
}

HRESULT CordbNativeCode::GetCallSignature(ULONG32 ILoffset, mdToken *pClass, mdToken *pFunction, SigParser &parser, SigParser &generics)
{
    // check if specified IL offset is at a call instruction
    CordbILCode *pCode = this->m_pFunction->GetILCode();
    BYTE buffer[3];
    ULONG32 fetched = 0;
    HRESULT hr = pCode->GetCode(ILoffset, ILoffset+_countof(buffer), _countof(buffer), buffer, &fetched);

    if (FAILED(hr))
        return hr;
    else if (fetched != _countof(buffer))
        return CORDBG_E_INVALID_OPCODE;

    // tail.    - fe 14 (ECMA III.2.4)
    BYTE instruction = buffer[0];
    if (buffer[0] == 0xfe && buffer[1] == 0x14)
    {
        // tail call case.  We don't allow managed return values for tailcalls.
        return CORDBG_E_INVALID_OPCODE;
    }

    // call     - 28    (ECMA III.3.19)
    // callvirt - 6f    (ECMA III.4.2)
    if (instruction != 0x28 && instruction != 0x6f)
        return CORDBG_E_INVALID_OPCODE;

    // Now grab the MD token of the call
    mdToken mdFunction = 0;
    const ULONG32 offset = ILoffset + 1;
    hr = pCode->GetCode(offset, offset+sizeof(mdToken), sizeof(mdToken), (BYTE*)&mdFunction, &fetched);
    if (FAILED(hr) || fetched != sizeof(mdToken))
        return CORDBG_E_INVALID_OPCODE;

    if (pFunction)
        *pFunction = mdFunction;

    // Convert to a signature parser
    return GetSigParserFromFunction(mdFunction, pClass, parser, generics);
}

HRESULT CordbNativeCode::GetReturnValueLiveOffsetImpl(Instantiation *currentInstantiation, ULONG32 ILoffset, ULONG32 bufferSize, ULONG32 *pFetched, ULONG32 *pOffsets)
{
    if (pFetched == NULL)
        return E_INVALIDARG;

    HRESULT hr = S_OK;
    ULONG32 found = 0;

    // verify that the call target actually returns something we allow
    SigParser signature, generics;
    mdToken mdClass = 0;
    IfFailRet(GetCallSignature(ILoffset, &mdClass, NULL, signature, generics));
    IfFailRet(EnsureReturnValueAllowed(currentInstantiation, mdClass, signature, generics));

    // now find the native offset
    SequencePoints *pSP = GetSequencePoints();
    DebuggerILToNativeMap *pMap = pSP->GetCallsiteMapAddr();

    for (ULONG32 i = 0; i < pSP->GetCallsiteEntryCount() && pMap; ++i, pMap++)
    {
        if (pMap->ilOffset == ILoffset && (pMap->source & ICorDebugInfo::CALL_INSTRUCTION) == ICorDebugInfo::CALL_INSTRUCTION)
        {
            // if we have a buffer, fill it in.
            if (pOffsets && found < bufferSize)
            {
                // Fetch the actual assembly instructions
                BYTE nativeBuffer[8];

                ULONG32 fetched = 0;
                IfFailRet(GetCode(pMap->nativeStartOffset, pMap->nativeStartOffset+_countof(nativeBuffer), _countof(nativeBuffer), nativeBuffer, &fetched));

                int skipBytes = 0;

#if defined(TARGET_X86) && defined(FEATURE_CORESYSTEM)
                // Skip nop sleds on x86 coresystem.  The JIT adds these instructions as a security measure,
                // and incorrectly reports to us the wrong offset of the call instruction.
                const BYTE nop_opcode = 0x90;
                while (fetched && nativeBuffer[0] == nop_opcode)
                {
                    skipBytes++;

                    for (int j = 1; j < _countof(nativeBuffer) && nativeBuffer[j] == nop_opcode; ++j)
                        skipBytes++;

                    // We must have at least one skip byte since the outer while ensures it.  Thus we always need to reread
                    // the buffer at the end of this loop.
                    IfFailRet(GetCode(pMap->nativeStartOffset+skipBytes, pMap->nativeStartOffset+skipBytes+_countof(nativeBuffer), _countof(nativeBuffer), nativeBuffer, &fetched));
                }
#endif

                // Get the length of the call instruction.
                int offset = GetCallInstructionLength(nativeBuffer, fetched);
                if (offset == -1)
                    return E_UNEXPECTED; // Could not decode instruction, this should never happen.

                pOffsets[found] = pMap->nativeStartOffset + offset + skipBytes;
            }

            found++;
        }
    }

    if (pOffsets)
        *pFetched = found < bufferSize ? found : bufferSize;
    else
        *pFetched = found;

    if (found == 0)
        return E_FAIL;

    if (pOffsets && found > bufferSize)
        return S_FALSE;

    return S_OK;
}

//-----------------------------------------------------------------------------
// Creates a CordbNativeCode (if it's not already created) and adds it to the
// hash table of CordbNativeCode instances belonging to this module.
// Used by CordbFunction::InitNativeCodeInfo.
//
// Arguments:
//    Input:
//       methodToken - the methodDef token of the function this native code belongs to
//       methodDesc - the methodDesc for the jitted method
//       startAddress - the hot code startAddress for this method

// Return value:
//      found or created CordbNativeCode pointer
// Assumptions: methodToken is in the metadata for this module
//              methodDesc and startAddress should be consistent for
//              a jitted instance of methodToken's method
//-----------------------------------------------------------------------------
CordbNativeCode * CordbModule::LookupOrCreateNativeCode(mdMethodDef methodToken,
                                                        VMPTR_MethodDesc methodDesc,
                                                        CORDB_ADDRESS startAddress)
{
    INTERNAL_SYNC_API_ENTRY(GetProcess());
    _ASSERTE(startAddress != NULL);
    _ASSERTE(methodDesc != VMPTR_MethodDesc::NullPtr());

    CordbNativeCode * pNativeCode = NULL;
    NativeCodeFunctionData codeInfo;
    RSLockHolder lockHolder(GetProcess()->GetProcessLock());

    // see if we already have this--if not, we'll make an instance, otherwise we'll just return the one we have.
    pNativeCode = m_nativeCodeTable.GetBase((UINT_PTR) startAddress);

    if (pNativeCode == NULL)
    {
        GetProcess()->GetDAC()->GetNativeCodeInfoForAddr(methodDesc, startAddress, &codeInfo);

        // We didn't have an instance, so we'll build one and add it to the hash table
        LOG((LF_CORDB,
             LL_INFO10000,
             "R:CT::RSCreating code w/ ver:0x%x, md:0x%x, nativeStart=0x%08x, nativeSize=0x%08x\n",
             codeInfo.encVersion,
             VmPtrToCookie(codeInfo.vmNativeCodeMethodDescToken),
             codeInfo.m_rgCodeRegions[kHot].pAddress,
             codeInfo.m_rgCodeRegions[kHot].cbSize));

        // Lookup the function object that this code should be bound to
        CordbFunction* pFunction = CordbModule::LookupOrCreateFunction(methodToken, codeInfo.encVersion);
        _ASSERTE(pFunction != NULL);

        // There are bugs with the on-demand class load performed by CordbFunction in some cases. The old stack
        // tracing code avoided them by eagerly loading the parent class so I am following suit
        pFunction->InitParentClassOfFunction();

        // First, create a new CordbNativeCode instance--we'll need this to make the CordbJITInfo instance
        pNativeCode = new (nothrow)CordbNativeCode(pFunction, &codeInfo, codeInfo.isInstantiatedGeneric != 0);
        _ASSERTE(pNativeCode != NULL);

        m_nativeCodeTable.AddBaseOrThrow(pNativeCode);
    }

    return pNativeCode;
} // CordbNativeCode::LookupOrCreateFromJITData

// LoadNativeInfo loads from the left side any native variable info
// from the JIT.
//
void CordbNativeCode::LoadNativeInfo()
{
    THROW_IF_NEUTERED(this);
    INTERNAL_API_ENTRY(this->GetProcess());


    // If we've either never done this before (no info), or we have, but the version number has increased, we
    // should try and get a newer version of our JIT info.
    if(m_nativeVarData.IsInitialized())
    {
        return;
    }

    // You can't do this if the function is implemented as part of the Runtime.
    if (GetFunction()->IsNativeImpl() == CordbFunction::kNativeOnly)
    {
        ThrowHR(CORDBG_E_FUNCTION_NOT_IL);
    }
     CordbProcess *pProcess = GetProcess();
    // Get everything via the DAC
    if (m_fCodeAvailable)
    {
        RSLockHolder lockHolder(pProcess->GetProcessLock());
        pProcess->GetDAC()->GetNativeCodeSequencePointsAndVarInfo(GetVMNativeCodeMethodDescToken(),
                                                                  GetAddress(),
                                                                  m_fCodeAvailable,
                                                                  &m_nativeVarData,
                                                                  &m_sequencePoints);
    }

} // CordbNativeCode::LoadNativeInfo
