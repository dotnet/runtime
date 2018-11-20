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

void ZapVersionResource::Save(ZapWriter * pZapWriter)
{
    // Resources are binary-sorted tree structure. Since we are saving just one resource
    // the binary structure is degenerated link list. By convention, Windows uses three levels
    // for resources: Type, Name, Language.

    // See vctools\link\doc\pecoff.doc for detailed documentation of PE format.

    VersionResourceHeader header;

    ZeroMemory(&header, sizeof(header));

    header.TypeDir.NumberOfIdEntries = 1;
    header.TypeEntry.Id = (USHORT)((ULONG_PTR)RT_VERSION);
    header.TypeEntry.OffsetToDirectory = offsetof(VersionResourceHeader, NameDir);
    header.TypeEntry.DataIsDirectory = 1;

    header.NameDir.NumberOfIdEntries = 1;
    header.NameEntry.Id = 1;
    header.NameEntry.OffsetToDirectory = offsetof(VersionResourceHeader, LangDir);
    header.NameEntry.DataIsDirectory = 1;

    header.LangDir.NumberOfIdEntries = 1;
    header.LangEntry.OffsetToDirectory = offsetof(VersionResourceHeader, DataEntry);

    header.DataEntry.OffsetToData = m_pVersionData->GetRVA();
    header.DataEntry.Size = m_pVersionData->GetSize();

    pZapWriter->Write(&header, sizeof(header));
}

void ZapImage::CopyWin32VersionResource()
{
#ifndef FEATURE_PAL
    // Copy the version resource over so it is easy to see in the dumps where the ngened module came from
    COUNT_T cbResourceData;
    PVOID pResourceData = m_ModuleDecoder.GetWin32Resource(MAKEINTRESOURCE(1), RT_VERSION, &cbResourceData);

    if (!pResourceData || !cbResourceData)
        return;

    ZapBlob * pVersionData = new (GetHeap()) ZapBlobPtr(pResourceData, cbResourceData);

    ZapVersionResource * pVersionResource = new (GetHeap()) ZapVersionResource(pVersionData);

    m_pWin32ResourceSection->Place(pVersionResource);
    m_pWin32ResourceSection->Place(pVersionData);

    SetDirectoryEntry(IMAGE_DIRECTORY_ENTRY_RESOURCE, m_pWin32ResourceSection);
#endif
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
