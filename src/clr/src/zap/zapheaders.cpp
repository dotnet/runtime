// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// ZapHeaders.cpp
//

//
// Zapping of headers (IMAGE_COR20_HEADER, CORCOMPILE_HEADER, etc.)
// 
// ======================================================================================

#include "common.h"

#include "zapheaders.h"

#include "zapcode.h"
#include "zaprelocs.h"
#include "zapmetadata.h"
#include "zapimport.h"

#include <clr_std/vector>
#include <clr_std/algorithm>

//
// IMAGE_COR20_HEADER
//

void ZapImage::SaveCorHeader()
{
    IMAGE_COR20_HEADER corHeader;

    ZeroMemory(&corHeader, sizeof(corHeader));

    corHeader.cb = VAL32(sizeof(IMAGE_COR20_HEADER));
    corHeader.MajorRuntimeVersion = VAL16(COR_VERSION_MAJOR);
    corHeader.MinorRuntimeVersion = VAL16(COR_VERSION_MINOR);
    corHeader.Flags = VAL32(COMIMAGE_FLAGS_IL_LIBRARY);

#ifdef _TARGET_X86_
    if (IsReadyToRunCompilation())
    {
        // Mark the ready-to-run image as x86-specific
        corHeader.Flags |= VAL32(COMIMAGE_FLAGS_32BITREQUIRED);
    }
#endif

    if (m_ModuleDecoder.HasManagedEntryPoint())
        corHeader.EntryPointToken = VAL32(m_ModuleDecoder.GetEntryPointToken());

    SetDirectoryData(&corHeader.ManagedNativeHeader, m_pNativeHeader);
    SetDirectoryData(&corHeader.Resources, m_pResources);
    SetDirectoryData(&corHeader.MetaData, m_pILMetaData);

    Write(&corHeader, sizeof(corHeader));
}

//
// CORCOMPILE_HEADER
//
void ZapImage::SaveNativeHeader()
{
    CORCOMPILE_HEADER nativeHeader;

    ZeroMemory(&nativeHeader, sizeof(nativeHeader));

    nativeHeader.Signature = CORCOMPILE_SIGNATURE;
    nativeHeader.MajorVersion = CORCOMPILE_MAJOR_VERSION;
    nativeHeader.MinorVersion = CORCOMPILE_MINOR_VERSION;

    //
    // Fill in data in native image header
    //

    nativeHeader.ImageBase = (TADDR) GetNativeBaseAddress();

    if (m_ModuleDecoder.HasDirectoryEntry(IMAGE_DIRECTORY_ENTRY_SECURITY))
        nativeHeader.Flags |= CORCOMPILE_HEADER_HAS_SECURITY_DIRECTORY;
            
    nativeHeader.COR20Flags = m_ModuleDecoder.GetCorHeader()->Flags;

#ifdef CROSSGEN_COMPILE
    if (CLRConfig::GetConfigValue(CLRConfig::INTERNAL_CrossGenAssumeInputSigned))
    {
        // Bug fix #814972
        // In buildlabs NI images are produced before binaries are strongname signed or authenticode signed.
        // which results in crossgen'ed NI different on the user box vs. the one from build lab.
        // Setting source IL authenticode signed and strong name bit for crossgen'ed images should make both the images identical.             
        nativeHeader.Flags |= CORCOMPILE_HEADER_HAS_SECURITY_DIRECTORY;
        
        if (m_ModuleDecoder.GetCorHeader()->StrongNameSignature.Size != 0)
            nativeHeader.COR20Flags |= COMIMAGE_FLAGS_STRONGNAMESIGNED;
    }
#endif

    if (m_ModuleDecoder.HasReadyToRunHeader())
    {
        // Pretend that ready-to-run images are IL-only
        nativeHeader.COR20Flags |= COMIMAGE_FLAGS_ILONLY;

        // Pretend that ready-to-run images do not have a native header
        nativeHeader.COR20Flags &= ~COMIMAGE_FLAGS_IL_LIBRARY;

        // Remember whether the source IL image had ReadyToRun header
        nativeHeader.Flags |= CORCOMPILE_HEADER_IS_READY_TO_RUN;
    }

    if (m_fHaveProfileData)
        nativeHeader.Flags |= CORCOMPILE_HEADER_IS_IBC_OPTIMIZED;

    DWORD dwPEKind, dwMachine;
    m_ModuleDecoder.GetPEKindAndMachine(&dwPEKind, &dwMachine);
    nativeHeader.PEKind = dwPEKind;
    nativeHeader.Machine = (WORD)dwMachine;

    nativeHeader.Characteristics = m_ModuleDecoder.GetCharacteristics();


    SetDirectoryData(&nativeHeader.EEInfoTable, m_pEEInfoTable);
    SetDirectoryData(&nativeHeader.HelperTable, m_pHelperTableSection);
    SetDirectoryData(&nativeHeader.ImportSections, m_pImportSectionsTable);
    SetDirectoryData(&nativeHeader.StubsData, m_pStubsSection);
    SetDirectoryData(&nativeHeader.VersionInfo, m_pVersionInfo);
    SetDirectoryData(&nativeHeader.Dependencies, m_pDependencies);
    SetDirectoryData(&nativeHeader.DebugMap, m_pDebugInfoTable);
    SetDirectoryData(&nativeHeader.VirtualSectionsTable, m_pVirtualSectionsTable);
    SetDirectoryData(&nativeHeader.ModuleImage, m_pPreloadSections[CORCOMPILE_SECTION_MODULE]);
    SetDirectoryData(&nativeHeader.CodeManagerTable, m_pCodeManagerEntry);
    SetDirectoryData(&nativeHeader.ProfileDataList, m_pInstrumentSection);
    SetDirectoryData(&nativeHeader.ManifestMetaData, m_pAssemblyMetaData);

    Write(&nativeHeader, sizeof(nativeHeader));
}

//
// CORCOMPILE_CODE_MANAGER_ENTRY
//
void ZapImage::SaveCodeManagerEntry()
{
    CORCOMPILE_CODE_MANAGER_ENTRY codeManagerEntry;

    ZeroMemory(&codeManagerEntry, sizeof(codeManagerEntry));

    SetDirectoryData(&codeManagerEntry.HotCode, m_pHotCodeSection);
    SetDirectoryData(&codeManagerEntry.Code, m_pCodeSection);
    SetDirectoryData(&codeManagerEntry.ColdCode, m_pColdCodeSection);

    SetDirectoryData(&codeManagerEntry.ROData, m_pReadOnlyDataSection);

    //
    //Initialize additional sections for diagnostics
    //

    codeManagerEntry.HotIBCMethodOffset = (m_iIBCMethod < m_iUntrainedMethod && m_iIBCMethod < m_MethodCompilationOrder.GetCount()) ?
        (m_MethodCompilationOrder[m_iIBCMethod]->GetCode()->GetRVA() - m_pHotCodeSection->GetRVA()) : m_pHotCodeSection->GetSize();

    codeManagerEntry.HotGenericsMethodOffset = (m_iGenericsMethod < m_iUntrainedMethod && m_iGenericsMethod < m_MethodCompilationOrder.GetCount()) ?
        (m_MethodCompilationOrder[m_iGenericsMethod]->GetCode()->GetRVA() - m_pHotCodeSection->GetRVA()) : m_pHotCodeSection->GetSize();

    COUNT_T i;
    for (i = m_iUntrainedMethod; i < m_MethodCompilationOrder.GetCount(); i++)
    {
        ZapNode * pColdCode = m_MethodCompilationOrder[i]->GetColdCode();
        if (pColdCode != NULL)
        {
            codeManagerEntry.ColdUntrainedMethodOffset = pColdCode->GetRVA() - m_pColdCodeSection->GetRVA();
            break;
        }
    }
    if (i == m_MethodCompilationOrder.GetCount())
        codeManagerEntry.ColdUntrainedMethodOffset = m_pColdCodeSection->GetSize();


    if (m_stats)
    {
#define ACCUM_SIZE(dest, src) if( src != NULL ) dest+= src->GetSize()
        // this is probably supposed to mean Hot+Unprofiled
        ACCUM_SIZE(m_stats->m_totalHotCodeSize, m_pHotCodeSection);
        ACCUM_SIZE(m_stats->m_totalUnprofiledCodeSize, m_pCodeSection);
        ACCUM_SIZE(m_stats->m_totalColdCodeSize, m_pColdCodeSection);
        ACCUM_SIZE(m_stats->m_totalCodeSizeInProfiledMethods, m_pHotCodeSection);
#undef ACCUM_SIZE
        m_stats->m_totalColdCodeSizeInProfiledMethods = codeManagerEntry.ColdUntrainedMethodOffset;
    }

    Write(&codeManagerEntry, sizeof(codeManagerEntry));
}

//
// Version Resource
//

// Needed for RT_VERSION.
#define MAKEINTRESOURCE(v) MAKEINTRESOURCEW(v)

void ZapWin32ResourceDirectory::Save(ZapWriter * pZapWriter)
{
    //
    // The IMAGE_RESOURCE_DIRECTORY resource data structure is followed by a number of IMAGE_RESOURCE_DIRECTORY_ENTRY entries, which can either 
    // point to other resource directories (RVAs to other ZapWin32ResourceDirectory nodes), or point to actual resource data (RVAs to a number 
    // of IMAGE_RESOURCE_DATA_ENTRY entries that immediately follow the IMAGE_RESOURCE_DIRECTORY_ENTRY entries).
    //

    //
    // Sorting for resources is done in the following way accoring to the PE format specifications:
    //   1) First, all the IMAGE_RESOURCE_DIRECTORY_ENTRY entries where the ID is a name string, sorted by names
    //   2) Second, all the IMAGE_RESOURCE_DIRECTORY_ENTRY entries with non-string IDs, sorted by IDs.
    //
    struct ResourceSorter
    {
        bool operator() (DataOrSubDirectoryEntry& a, DataOrSubDirectoryEntry& b)
        {
            if (a.m_nameOrIdIsString && !b.m_nameOrIdIsString)
                return true;
            if (!a.m_nameOrIdIsString && b.m_nameOrIdIsString)
                return false;
            if (a.m_nameOrIdIsString)
                return wcscmp(((ZapWin32ResourceString*)(a.m_pNameOrId))->GetString(), ((ZapWin32ResourceString*)(b.m_pNameOrId))->GetString()) < 0;
            else
                return a.m_pNameOrId < b.m_pNameOrId;
        }
    } resourceSorter;
    std::sort(m_entries.begin(), m_entries.end(), resourceSorter);


    IMAGE_RESOURCE_DIRECTORY directory;
    ZeroMemory(&directory, sizeof(IMAGE_RESOURCE_DIRECTORY));

    for (auto& entry : m_entries)
    {
        if (entry.m_nameOrIdIsString)
            directory.NumberOfNamedEntries++;
        else
            directory.NumberOfIdEntries++;
    }
    pZapWriter->Write(&directory, sizeof(IMAGE_RESOURCE_DIRECTORY));

    // Offsets are based from the begining of the resources blob (see PE format documentation)
    DWORD dataEntryRVA = this->GetRVA() - m_pWin32ResourceSection->GetRVA()
        + sizeof(IMAGE_RESOURCE_DIRECTORY) + 
        (DWORD)m_entries.size() * sizeof(IMAGE_RESOURCE_DIRECTORY_ENTRY);

    for (auto& entry : m_entries)
    {
        IMAGE_RESOURCE_DIRECTORY_ENTRY dirEntry;
        ZeroMemory(&dirEntry, sizeof(IMAGE_RESOURCE_DIRECTORY_ENTRY));

        if (entry.m_nameOrIdIsString)
        {
            // Offsets are based from the begining of the resources blob (see PE format documentation)
            dirEntry.NameOffset = ((ZapWin32ResourceString*)(entry.m_pNameOrId))->GetRVA() - m_pWin32ResourceSection->GetRVA();
            dirEntry.NameIsString = true;
        }
        else
        {
            _ASSERT(IS_INTRESOURCE(entry.m_pNameOrId));
            dirEntry.Id = (WORD)((ULONG_PTR)entry.m_pNameOrId & 0xffff);
        }

        if (entry.m_dataIsSubDirectory)
        {
            // Offsets are based from the begining of the resources blob (see PE format documentation)
            dirEntry.OffsetToDirectory = entry.m_pDataOrSubDirectory->GetRVA() - m_pWin32ResourceSection->GetRVA();
            dirEntry.DataIsDirectory = true;
        }
        else
        {
            dirEntry.OffsetToData = dataEntryRVA;
            dataEntryRVA += sizeof(IMAGE_RESOURCE_DATA_ENTRY);
        }

        pZapWriter->Write(&dirEntry, sizeof(IMAGE_RESOURCE_DIRECTORY_ENTRY));
    }

    for (auto& entry : m_entries)
    {
        if (entry.m_dataIsSubDirectory)
            continue;

        IMAGE_RESOURCE_DATA_ENTRY dataEntry;
        ZeroMemory(&dataEntry, sizeof(IMAGE_RESOURCE_DATA_ENTRY));

        dataEntry.OffsetToData = entry.m_pDataOrSubDirectory->GetRVA();
        dataEntry.Size = entry.m_pDataOrSubDirectory->GetSize();

        pZapWriter->Write(&dataEntry, sizeof(IMAGE_RESOURCE_DATA_ENTRY));
    }
}

void ZapImage::CopyWin32Resources()
{
#ifdef FEATURE_PREJIT
    if (!IsReadyToRunCompilation())
    {
        // When compiling a fragile NGEN image, in order to avoid the risk of regression, only copy the RT_VERSION resource over so it 
        // is easy to see in the dumps where the ngened module came from. For R2R, we copy all resources (new behavior).
        COUNT_T cbResourceData;
        PVOID pResourceData = m_ModuleDecoder.GetWin32Resource(MAKEINTRESOURCE(1), RT_VERSION, &cbResourceData);

        if (!pResourceData || !cbResourceData)
            return;

        ZapBlob * pVersionData = new (GetHeap()) ZapBlobPtr(pResourceData, cbResourceData);

        ZapWin32ResourceDirectory* pTypeDirectory = new (GetHeap()) ZapWin32ResourceDirectory(m_pWin32ResourceSection);
        ZapWin32ResourceDirectory* pNameDirectory = new (GetHeap()) ZapWin32ResourceDirectory(m_pWin32ResourceSection);
        ZapWin32ResourceDirectory* pLanguageDirectory = new (GetHeap()) ZapWin32ResourceDirectory(m_pWin32ResourceSection);

        pTypeDirectory->AddEntry(RT_VERSION, false, pNameDirectory, true);
        pNameDirectory->AddEntry(MAKEINTRESOURCE(1), false, pLanguageDirectory, true);
        pLanguageDirectory->AddEntry(MAKEINTRESOURCE(0), false, pVersionData, false);

        pTypeDirectory->PlaceNodeAndDependencies(m_pWin32ResourceSection);

        m_pWin32ResourceSection->Place(pVersionData);

        SetDirectoryEntry(IMAGE_DIRECTORY_ENTRY_RESOURCE, m_pWin32ResourceSection);

        return;
    }
#endif

    class ResourceEnumerationCallback
    {
        ZapImage* m_pZapImage;
        PEDecoder* m_pModuleDecoder;

        std::vector<ZapNode*> m_dataEntries;
        std::vector<ZapBlob*> m_stringEntries;

        ZapWin32ResourceDirectory* m_pRootDirectory;
        ZapWin32ResourceDirectory* m_pCurrentTypesDirectory;
        ZapWin32ResourceDirectory* m_pCurrentNamesDirectory;

        bool AddResource(LPCWSTR lpszResourceName, LPCWSTR lpszResourceType, DWORD langID, BYTE* pResourceData, COUNT_T cbResourceData)
        {
            ZapBlob* pDataBlob = new (m_pZapImage->GetHeap()) ZapBlobPtr(pResourceData, cbResourceData);
            m_dataEntries.push_back(pDataBlob);

            m_pCurrentNamesDirectory->AddEntry((PVOID)(ULONG_PTR)langID, false, pDataBlob, false);

            return true;
        }

        ZapWin32ResourceDirectory* CreateResourceSubDirectory(ZapWin32ResourceDirectory* pRootDir, LPCWSTR pNameOrId)
        {
            bool nameIsString = !IS_INTRESOURCE(pNameOrId);

            PVOID pIdOrNameZapNode = (PVOID)pNameOrId;
            if (nameIsString)
            {
                pIdOrNameZapNode = new (m_pZapImage->GetHeap()) ZapWin32ResourceString(pNameOrId);
                m_stringEntries.push_back((ZapBlob*)pIdOrNameZapNode);
            }

            ZapWin32ResourceDirectory* pResult = new (m_pZapImage->GetHeap()) ZapWin32ResourceDirectory(m_pZapImage->m_pWin32ResourceSection);
            pRootDir->AddEntry(pIdOrNameZapNode, nameIsString, pResult, true);

            return pResult;
        }

    public:
        ResourceEnumerationCallback(PEDecoder* pModuleDecoder, ZapImage* pZapImage)
            : m_pZapImage(pZapImage), m_pModuleDecoder(pModuleDecoder)
        {
            m_pRootDirectory = new (pZapImage->GetHeap()) ZapWin32ResourceDirectory(pZapImage->m_pWin32ResourceSection);
            m_pCurrentTypesDirectory = m_pCurrentNamesDirectory = NULL;
        }

        static bool EnumResourcesCallback(LPCWSTR lpszResourceName, LPCWSTR lpszResourceType, DWORD langID, BYTE* data, COUNT_T cbData, void *context)
        {
            ResourceEnumerationCallback* pCallback = (ResourceEnumerationCallback*)context;
            // Third level in the enumeration: resources by langid for each name/type. 

            // Note that this callback is not equivalent to the Windows enumeration apis as this api provides the resource data
            // itself, and the resources are guaranteed to be present directly in the associated binary. This does not exactly
            // match the Windows api, but it is exactly what we want when copying all resource data.

            return pCallback->AddResource(lpszResourceName, lpszResourceType, langID, data, cbData);
        }

        static bool EnumResourceNamesCallback(LPCWSTR lpszResourceName, LPCWSTR lpszResourceType, void *context)
        {
            // Second level in the enumeration: resources by names for each resource type

            ResourceEnumerationCallback* pCallback = (ResourceEnumerationCallback*)context;
            pCallback->m_pCurrentNamesDirectory = pCallback->CreateResourceSubDirectory(pCallback->m_pCurrentTypesDirectory, lpszResourceName);
            
            return pCallback->m_pModuleDecoder->EnumerateWin32Resources(lpszResourceName, lpszResourceType, ResourceEnumerationCallback::EnumResourcesCallback, context);
        }

        static bool EnumResourceTypesCallback(LPCWSTR lpszType, void *context)
        {
            // First level in the enumeration: resources by types

            // Skip IBC resources
            if (!IS_INTRESOURCE(lpszType) && (wcscmp(lpszType, W("IBC")) == 0))
                return true;

            ResourceEnumerationCallback* pCallback = (ResourceEnumerationCallback*)context;
            pCallback->m_pCurrentTypesDirectory = pCallback->CreateResourceSubDirectory(pCallback->m_pRootDirectory, lpszType);

            return pCallback->m_pModuleDecoder->EnumerateWin32ResourceNames(lpszType, ResourceEnumerationCallback::EnumResourceNamesCallback, context);
        }

        void PlaceResourceNodes(ZapVirtualSection* pWin32ResourceSection)
        {
            m_pRootDirectory->PlaceNodeAndDependencies(pWin32ResourceSection);

            //
            // These strings are stored together after the last Resource Directory entry and before the first Resource Data entry. This 
            // minimizes the impact of these variable-length strings on the alignment of the fixed-size directory entries
            //
            for (auto& entry : m_stringEntries)
                pWin32ResourceSection->Place(entry);

            for (auto& entry : m_dataEntries)
                pWin32ResourceSection->Place(entry);
        }
    };

    ResourceEnumerationCallback callbacks(&m_ModuleDecoder, this);

    HMODULE hModule = (HMODULE)dac_cast<TADDR>(m_ModuleDecoder.GetBase());

    //
    // Resources are binary-sorted tree structure. By convention, Windows uses three levels
    // for resources: Type, Name, Language. To reduces the overall complexity, we'll copy and store resources in the
    // "neutral" language only.
    //

    if (!m_ModuleDecoder.EnumerateWin32ResourceTypes(ResourceEnumerationCallback::EnumResourceTypesCallback, &callbacks))
    {
        ThrowHR(E_FAIL);
    }
    else
    {
        callbacks.PlaceResourceNodes(m_pWin32ResourceSection);

        SetDirectoryEntry(IMAGE_DIRECTORY_ENTRY_RESOURCE, m_pWin32ResourceSection);
    }
}
#undef MAKEINTRESOURCE


//
// Debug Directory
//

void ZapDebugDirectory::SaveOriginalDebugDirectoryEntry(ZapWriter *pZapWriter)
{
    if (m_ppDebugData != nullptr)
    {
        for (DWORD i = 0; i < m_nDebugDirectory; i++)
        {
            if (m_ppDebugData[i] != nullptr)
            {
                m_pDebugDirectory[i].SizeOfData = m_ppDebugData[i]->GetSize();
                m_pDebugDirectory[i].AddressOfRawData = m_ppDebugData[i]->GetRVA();

                // Compute the absolute file (seek) pointer. We need to reach to the matching physical section to do that.
                ZapPhysicalSection * pPhysicalSection = ZapImage::GetImage(pZapWriter)->m_pTextSection;

                DWORD dwOffset = m_ppDebugData[i]->GetRVA() - pPhysicalSection->GetRVA();
                _ASSERTE(dwOffset < pPhysicalSection->GetSize());

                m_pDebugDirectory[i].PointerToRawData = pPhysicalSection->GetFilePos() + dwOffset;
            }
            else
            {
                m_pDebugDirectory[i].SizeOfData = 0;
                m_pDebugDirectory[i].AddressOfRawData = 0;
                m_pDebugDirectory[i].PointerToRawData = 0;
            }
        }

        pZapWriter->Write(m_pDebugDirectory, sizeof(IMAGE_DEBUG_DIRECTORY) * m_nDebugDirectory);
    }
}

void ZapDebugDirectory::SaveNGenDebugDirectoryEntry(ZapWriter *pZapWriter)
{
#if !defined(NO_NGENPDB)
    _ASSERTE(pZapWriter);

    IMAGE_DEBUG_DIRECTORY debugDirectory = {0};
    if (m_nDebugDirectory > 0)
    {
        memcpy(&debugDirectory, m_pDebugDirectory, sizeof(IMAGE_DEBUG_DIRECTORY));
    }
    debugDirectory.Type = IMAGE_DEBUG_TYPE_CODEVIEW;
    debugDirectory.SizeOfData = m_pNGenPdbDebugData->GetSize();
    debugDirectory.AddressOfRawData = m_pNGenPdbDebugData->GetRVA();
    // Make sure the "is portable pdb" indicator (MinorVersion == 0x504d) is clear
    // for the NGen debug directory entry since this debug directory is copied
    // from an existing entry which could be a portable pdb.
    debugDirectory.MinorVersion = 0;
    {
        ZapPhysicalSection *pPhysicalSection = ZapImage::GetImage(pZapWriter)->m_pTextSection;
        DWORD dwOffset = m_pNGenPdbDebugData->GetRVA() - pPhysicalSection->GetRVA();
        _ASSERTE(dwOffset < pPhysicalSection->GetSize());
        debugDirectory.PointerToRawData = pPhysicalSection->GetFilePos() + dwOffset;
    }
    pZapWriter->Write(&debugDirectory, sizeof(debugDirectory));
#endif // NO_NGENPDB
}

void ZapDebugDirectory::Save(ZapWriter * pZapWriter)
{
    _ASSERTE(pZapWriter);

    if (CLRConfig::GetConfigValue(CLRConfig::UNSUPPORTED_NGenEnableCreatePdb)) {
        SaveOriginalDebugDirectoryEntry(pZapWriter);
        SaveNGenDebugDirectoryEntry(pZapWriter);
    } else {
        SaveNGenDebugDirectoryEntry(pZapWriter);
        SaveOriginalDebugDirectoryEntry(pZapWriter);
    }
}

ZapPEExports::ZapPEExports(LPCWSTR dllPath) 
{
	m_dllFileName = wcsrchr(dllPath, DIRECTORY_SEPARATOR_CHAR_W);
	if (m_dllFileName != NULL)
		m_dllFileName++;
	else 
		m_dllFileName = dllPath;
}

DWORD ZapPEExports::GetSize()
{
	return DWORD(sizeof(IMAGE_EXPORT_DIRECTORY) + wcslen(m_dllFileName) * sizeof(BYTE) + 1);
}

void ZapPEExports::Save(ZapWriter * pZapWriter)
{
	_ASSERTE(pZapWriter);

	IMAGE_EXPORT_DIRECTORY exports;
	ZeroMemory(&exports, sizeof(exports));

	exports.Name = pZapWriter->GetCurrentRVA() + sizeof(exports);

	// Write out exports header 
	pZapWriter->Write(&exports, sizeof(exports));

	// Write out string that exports.Name points at.  
	for (LPCWSTR ptr = m_dllFileName; ; ptr++)
	{
		pZapWriter->Write((PVOID) ptr, 1);
		if (*ptr == 0)
			break;
	}
}

// If the IL image has IMAGE_DIRECTORY_ENTRY_DEBUG with information about the PDB,
// copy that information over to the ngen image.
// This lets the debugger find out information about the PDB without loading
// the IL image.
// Note that we support using ngen images (from the GAC) without
// loading the ngen image. So not only is this a perf optimization for the debugger
// scenario, but it is also needed for dump-debugging where loading the IL
// is not an option.

void ZapImage::CopyDebugDirEntry()
{
    // Insert an NGEN PDB debug directory entry *before* the IL PDB debug directory entry
    // (if one exists), so that we don't break tools that look for an IL PDB.
    {
        // This entry is initially empty. It is filled in ZapImage::GenerateFile.
        RSDS rsds = {0};
        m_pNGenPdbDebugData = ZapBlob::NewBlob(static_cast<ZapWriter *>(this), &rsds, sizeof rsds);
    }

    // IL PDB entry: copy of the (first of possibly many) IMAGE_DEBUG_DIRECTORY entry 
    // in the IL image
    DWORD nDebugEntry = 0;
    PIMAGE_DEBUG_DIRECTORY pDebugDir = NULL;
    ZapNode **ppDebugData = NULL;

    if (m_ModuleDecoder.HasDirectoryEntry(IMAGE_DIRECTORY_ENTRY_DEBUG)) {
        COUNT_T debugEntrySize;
        TADDR pDebugEntry = m_ModuleDecoder.GetDirectoryEntryData(IMAGE_DIRECTORY_ENTRY_DEBUG, &debugEntrySize);

        if (debugEntrySize != 0) {

            if (debugEntrySize < sizeof(IMAGE_DEBUG_DIRECTORY) || 0 != (debugEntrySize % sizeof(IMAGE_DEBUG_DIRECTORY))) {
                m_zapper->Warning(W("IMAGE_DIRECTORY_ENTRY_DEBUG size (%d) should be a multiple of %d\n"),
                                  debugEntrySize, sizeof(IMAGE_DEBUG_DIRECTORY));
            } else {
                
                // Since pDebugEntry is an array of IMAGE_DEBUG_DIRECTORYs, debugEntrySize
                // should be a multiple of sizeof(IMAGE_DEBUG_DIRECTORY).
                _ASSERTE(0 == (debugEntrySize % sizeof(IMAGE_DEBUG_DIRECTORY)));
                
                nDebugEntry = DWORD(debugEntrySize / sizeof(IMAGE_DEBUG_DIRECTORY));
                pDebugDir = new (GetHeap()) IMAGE_DEBUG_DIRECTORY[nDebugEntry];
                memcpy(pDebugDir, (const void *)pDebugEntry, sizeof(IMAGE_DEBUG_DIRECTORY) * nDebugEntry);
                ppDebugData = new (GetHeap()) ZapNode*[nDebugEntry];
                memset(ppDebugData, 0, nDebugEntry * sizeof(ZapNode*));
                
                for (DWORD i = 0; i < nDebugEntry; i++)
                {
                    // Some compilers set PointerToRawData but not AddressOfRawData as they put the
                    // data at the end of the file in an unmapped part of the file

                    RVA rvaOfRawData = (pDebugDir[i].AddressOfRawData != NULL)
                        ? pDebugDir[i].AddressOfRawData : m_ModuleDecoder.OffsetToRva(pDebugDir[i].PointerToRawData);

                    ULONG cbDebugData = pDebugDir[i].SizeOfData;

                    if (cbDebugData != 0) {
                        if (!m_ModuleDecoder.CheckRva(rvaOfRawData, cbDebugData))
                            m_zapper->Warning(W("IMAGE_DIRECTORY_ENTRY_DEBUG points to bad data\n"));
                        else
                            ppDebugData[i] = new (GetHeap()) ZapBlobPtr((PVOID)m_ModuleDecoder.GetRvaData(rvaOfRawData), cbDebugData);
                    }
                }
            }
        }
    }

    ZapDebugDirectory * pDebugDirectory = new (GetHeap()) ZapDebugDirectory(m_pNGenPdbDebugData, 
                                                                            nDebugEntry,
                                                                            pDebugDir,
                                                                            ppDebugData);

    m_pDebugSection->Place(pDebugDirectory);
    m_pDebugSection->Place(m_pNGenPdbDebugData);
    if (ppDebugData)
    {
        for (DWORD i = 0; i < nDebugEntry; i++)
        {
            if (ppDebugData[i] != nullptr)
                m_pDebugSection->Place(ppDebugData[i]);
        }
    }

    SetDirectoryEntry(IMAGE_DIRECTORY_ENTRY_DEBUG, pDebugDirectory);
}

DWORD ZapVirtualSectionsTable::GetSize()
{
    DWORD nSectionInfos = 0;

    COUNT_T nPhysicalSections = m_pImage->GetPhysicalSectionCount();
    for (COUNT_T iPhysicalSection = 0; iPhysicalSection < nPhysicalSections; iPhysicalSection++)
    {
        ZapPhysicalSection * pPhysicalSection = m_pImage->GetPhysicalSection(iPhysicalSection);

        DWORD dwPreviousType = 0;

        COUNT_T nVirtualSections = pPhysicalSection->GetVirtualSectionCount();
        for (COUNT_T iVirtualSection = 0; iVirtualSection < nVirtualSections; iVirtualSection++)
        {
            ZapVirtualSection * pVirtualSection = pPhysicalSection->GetVirtualSection(iVirtualSection);

            if (pVirtualSection->GetNodeCount() == 0)
                continue;

            DWORD dwSectionType = pVirtualSection->GetSectionType();
            if (dwSectionType == 0)
                continue;

            // Fold sections with the same type together
            if (dwPreviousType != dwSectionType)
            {
                dwPreviousType = dwSectionType;
                nSectionInfos++;
            }
        }
    }

    return nSectionInfos * sizeof(CORCOMPILE_VIRTUAL_SECTION_INFO);
}

void ZapVirtualSectionsTable::Save(ZapWriter * pZapWriter)
{
    COUNT_T nPhysicalSections = m_pImage->GetPhysicalSectionCount();
    for (COUNT_T iPhysicalSection = 0; iPhysicalSection < nPhysicalSections; iPhysicalSection++)
    {
        ZapPhysicalSection * pPhysicalSection = m_pImage->GetPhysicalSection(iPhysicalSection);

        CORCOMPILE_VIRTUAL_SECTION_INFO sectionInfo;

        sectionInfo.SectionType = 0;

        COUNT_T nVirtualSections = pPhysicalSection->GetVirtualSectionCount();
        for (COUNT_T iVirtualSection = 0; iVirtualSection < nVirtualSections; iVirtualSection++)
        {
            ZapVirtualSection * pVirtualSection = pPhysicalSection->GetVirtualSection(iVirtualSection);

            if (pVirtualSection->GetNodeCount() == 0)
                continue;

            DWORD dwSectionType = pVirtualSection->GetSectionType();
            if (dwSectionType == 0)
                continue;

            // Fold sections with the same type together
            if (sectionInfo.SectionType != dwSectionType)
            {
                if (sectionInfo.SectionType != 0)
                {
                    pZapWriter->Write(&sectionInfo, sizeof(sectionInfo));
                }

                sectionInfo.SectionType = dwSectionType;
                sectionInfo.VirtualAddress = pVirtualSection->GetRVA();
            }

            // Update section size
            sectionInfo.Size = (pVirtualSection->GetRVA() + pVirtualSection->GetSize()) - sectionInfo.VirtualAddress;
        }

        if (sectionInfo.SectionType != 0)
        {
            pZapWriter->Write(&sectionInfo, sizeof(sectionInfo));
        }
    }
}
