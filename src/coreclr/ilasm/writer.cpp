// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// writer.cpp
//

#include "ilasmpch.h"

#include "assembler.h"

#include "ceefilegenwriter.h"

#ifndef _MSC_VER
//cloned definition from ntimage.h that is removed for non MSVC builds
typedef VOID
(NTAPI *PIMAGE_TLS_CALLBACK) (
    PVOID DllHandle,
    ULONG Reason,
    PVOID Reserved
    );
#endif //_MSC_VER


HRESULT Assembler::InitMetaData()
{
    HRESULT             hr = E_FAIL;

    if(m_fInitialisedMetaData) return S_OK;

    if(bClock) bClock->cMDInitBegin = GetTickCount();

    hr = MetaDataGetDispenser(CLSID_CorMetaDataDispenser,
        IID_IMetaDataDispenserEx2, (void **)&m_pDisp);
    if (FAILED(hr))
        goto exit;

    hr = m_pDisp->DefineScope(CLSID_CorMetaDataRuntime, 0, IID_IMetaDataEmit3,
                        (IUnknown **)&m_pEmitter);
    if (FAILED(hr))
        goto exit;

    m_pManifest->SetEmitter(m_pEmitter);
    if(FAILED(hr = m_pEmitter->QueryInterface(IID_IMetaDataImport2, (void**)&m_pImporter)))
        goto exit;

    if (m_fGeneratePDB)
    {
        m_pPortablePdbWriter = new PortablePdbWriter();
        if (FAILED(hr = m_pPortablePdbWriter->Init(m_pDisp))) goto exit;
    }

    //m_Parser = new AsmParse(m_pEmitter);
    m_fInitialisedMetaData = TRUE;

    hr = S_OK;

exit:
    if(bClock) bClock->cMDInitEnd = GetTickCount();
    return hr;
}
/*********************************************************************************/
/* if we have any Thread local store data, make the TLS directory record for it */

HRESULT Assembler::CreateTLSDirectory() {

    ULONG tlsEnd;
    HRESULT hr;
    if (FAILED(hr=m_pCeeFileGen->GetSectionDataLen(m_pTLSSection, &tlsEnd))) return(hr);

    if (tlsEnd == 0)        // No TLS data, we are done
        return(S_OK);

        // place to put the TLS directory
    HCEESECTION tlsDirSec = m_pGlobalDataSection;

    if(m_dwCeeFileFlags & ICEE_CREATE_FILE_PE32)
    {
        DWORD sizeofptr = (DWORD)sizeof(DWORD);
        DWORD sizeofdir = (DWORD)sizeof(IMAGE_TLS_DIRECTORY32);
        DWORD offsetofStartAddressOfRawData  = (DWORD)offsetof(IMAGE_TLS_DIRECTORY32, StartAddressOfRawData);
        DWORD offsetofEndAddressOfRawData    = (DWORD)offsetof(IMAGE_TLS_DIRECTORY32, EndAddressOfRawData);
        DWORD offsetofAddressOfIndex         = (DWORD)offsetof(IMAGE_TLS_DIRECTORY32, AddressOfIndex);
        DWORD offsetofAddressOfCallBacks     = (DWORD)offsetof(IMAGE_TLS_DIRECTORY32, AddressOfCallBacks);

            // Get memory for for the TLS directory block,as well as a spot for callback chain
        IMAGE_TLS_DIRECTORY32* tlsDir;
        if(FAILED(hr=m_pCeeFileGen->GetSectionBlock(tlsDirSec, sizeofdir + sizeofptr, sizeofptr, (void**) &tlsDir))) return(hr);
        DWORD* callBackChain = (DWORD*) &tlsDir[1];
        *callBackChain = 0;

            // Find out where the tls directory will end up
        ULONG tlsDirOffset;
        if(FAILED(hr=m_pCeeFileGen->GetSectionDataLen(tlsDirSec, &tlsDirOffset))) return(hr);
        tlsDirOffset -= (sizeofdir + sizeofptr);

            // Set the start of the TLS data (offset 0 of hte TLS section)
        tlsDir->StartAddressOfRawData = 0;
        if(FAILED(hr=m_pCeeFileGen->AddSectionReloc(tlsDirSec, tlsDirOffset + offsetofStartAddressOfRawData, m_pTLSSection, srRelocHighLow))) return(hr);

            // Set the end of the TLS data
        tlsDir->EndAddressOfRawData = VALPTR(tlsEnd);
        if(FAILED(hr=m_pCeeFileGen->AddSectionReloc(tlsDirSec, tlsDirOffset + offsetofEndAddressOfRawData, m_pTLSSection, srRelocHighLow))) return(hr);

            // Allocate space for the OS to put the TLS index for this PE file (needs to be Read/Write?)
        DWORD* tlsIndex;
        if(FAILED(hr=m_pCeeFileGen->GetSectionBlock(m_pGlobalDataSection, sizeof(DWORD), sizeof(DWORD), (void**) &tlsIndex))) return(hr);
        *tlsIndex = 0xCCCCCCCC;     // Does't really matter, the OS will fill it in

            // Find out where tlsIndex index is
        ULONG tlsIndexOffset;
        if(FAILED(hr=m_pCeeFileGen->GetSectionDataLen(tlsDirSec, &tlsIndexOffset))) return(hr);
        tlsIndexOffset -= sizeof(DWORD);

            // Set the address of the TLS index
        tlsDir->AddressOfIndex = VALPTR(tlsIndexOffset);
        if(FAILED(hr=m_pCeeFileGen->AddSectionReloc(tlsDirSec, tlsDirOffset + offsetofAddressOfIndex, m_pGlobalDataSection, srRelocHighLow))) return(hr);

            // Set addres of callbacks chain
        tlsDir->AddressOfCallBacks = VALPTR((DWORD)(DWORD_PTR)(PIMAGE_TLS_CALLBACK*)(size_t)(tlsDirOffset + sizeofdir));
        if(FAILED(hr=m_pCeeFileGen->AddSectionReloc(tlsDirSec, tlsDirOffset + offsetofAddressOfCallBacks, tlsDirSec, srRelocHighLow))) return(hr);

            // Set the other fields.
        tlsDir->SizeOfZeroFill = 0;
        tlsDir->Characteristics = 0;

        hr=m_pCeeFileGen->SetDirectoryEntry (m_pCeeFile, tlsDirSec, IMAGE_DIRECTORY_ENTRY_TLS,
            sizeofdir, tlsDirOffset);

        if (m_dwCeeFileFlags & ICEE_CREATE_MACHINE_I386)
            COR_SET_32BIT_REQUIRED(m_dwComImageFlags);
    }
    else
    {
        DWORD sizeofptr = (DWORD)sizeof(__int64);
        DWORD sizeofdir = (DWORD)sizeof(IMAGE_TLS_DIRECTORY64);
        DWORD offsetofStartAddressOfRawData  = (DWORD)offsetof(IMAGE_TLS_DIRECTORY64, StartAddressOfRawData);
        DWORD offsetofEndAddressOfRawData    = (DWORD)offsetof(IMAGE_TLS_DIRECTORY64, EndAddressOfRawData);
        DWORD offsetofAddressOfIndex         = (DWORD)offsetof(IMAGE_TLS_DIRECTORY64, AddressOfIndex);
        DWORD offsetofAddressOfCallBacks     = (DWORD)offsetof(IMAGE_TLS_DIRECTORY64, AddressOfCallBacks);

            // Get memory for for the TLS directory block,as well as a spot for callback chain
        IMAGE_TLS_DIRECTORY64* tlsDir;
        if(FAILED(hr=m_pCeeFileGen->GetSectionBlock(tlsDirSec, sizeofdir + sizeofptr, sizeofptr, (void**) &tlsDir))) return(hr);
        __int64* callBackChain = (__int64*) &tlsDir[1];
        *callBackChain = 0;

            // Find out where the tls directory will end up
        ULONG tlsDirOffset;
        if(FAILED(hr=m_pCeeFileGen->GetSectionDataLen(tlsDirSec, &tlsDirOffset))) return(hr);
        tlsDirOffset -= (sizeofdir + sizeofptr);

            // Set the start of the TLS data (offset 0 of hte TLS section)
        tlsDir->StartAddressOfRawData = 0;
        if(FAILED(hr=m_pCeeFileGen->AddSectionReloc(tlsDirSec, tlsDirOffset + offsetofStartAddressOfRawData, m_pTLSSection, srRelocHighLow))) return(hr);

            // Set the end of the TLS data
        tlsDir->EndAddressOfRawData = VALPTR(tlsEnd);
        if(FAILED(hr=m_pCeeFileGen->AddSectionReloc(tlsDirSec, tlsDirOffset + offsetofEndAddressOfRawData, m_pTLSSection, srRelocHighLow))) return(hr);

            // Allocate space for the OS to put the TLS index for this PE file (needs to be Read/Write?)
        DWORD* tlsIndex;
        if(FAILED(hr=m_pCeeFileGen->GetSectionBlock(m_pGlobalDataSection, sizeof(DWORD), sizeof(DWORD), (void**) &tlsIndex))) return(hr);
        *tlsIndex = 0xCCCCCCCC;     // Does't really matter, the OS will fill it in

            // Find out where tlsIndex index is
        ULONG tlsIndexOffset;
        if(FAILED(hr=m_pCeeFileGen->GetSectionDataLen(tlsDirSec, &tlsIndexOffset))) return(hr);
        tlsIndexOffset -= sizeof(DWORD);

            // Set the address of the TLS index
        tlsDir->AddressOfIndex = VALPTR(tlsIndexOffset);
        if(FAILED(hr=m_pCeeFileGen->AddSectionReloc(tlsDirSec, tlsDirOffset + offsetofAddressOfIndex, m_pGlobalDataSection, srRelocHighLow))) return(hr);

            // Set address of callbacks chain
        tlsDir->AddressOfCallBacks = VALPTR((DWORD)(DWORD_PTR)(PIMAGE_TLS_CALLBACK*)(size_t)(tlsDirOffset + sizeofdir));
        if(FAILED(hr=m_pCeeFileGen->AddSectionReloc(tlsDirSec, tlsDirOffset + offsetofAddressOfCallBacks, tlsDirSec, srRelocHighLow))) return(hr);

            // Set the other fields.
        tlsDir->SizeOfZeroFill = 0;
        tlsDir->Characteristics = 0;

        hr=m_pCeeFileGen->SetDirectoryEntry (m_pCeeFile, tlsDirSec, IMAGE_DIRECTORY_ENTRY_TLS,
            sizeofdir, tlsDirOffset);
    }

    if(m_dwCeeFileFlags & ICEE_CREATE_FILE_STRIP_RELOCS)
    {
        report->error("Base relocations are emitted, while /STRIPRELOC option has been specified");
    }
    m_dwComImageFlags &= ~COMIMAGE_FLAGS_ILONLY;

    return(hr);
}

HRESULT Assembler::CreateDebugDirectory()
{
    HRESULT hr = S_OK;
    HCEESECTION sec = m_pILSection;
    BYTE *de;
    ULONG deOffset;

    // Only emit this if we're also emitting debug info.
    if (!m_fGeneratePDB)
        return S_OK;

    IMAGE_DEBUG_DIRECTORY  debugDirIDD;
    struct Param
    {
    DWORD                  debugDirDataSize;
        BYTE              *debugDirData;
    } param;
    param.debugDirData = NULL;

    // get module ID
    DWORD rsds = 0x53445352;
    DWORD pdbAge = 0x1;
    DWORD len = sizeof(rsds) + sizeof(GUID) + sizeof(pdbAge) + (DWORD)strlen(m_szPdbFileName) + 1;
    BYTE* dbgDirData = new BYTE[len];

    DWORD offset = 0;
    memcpy_s(dbgDirData + offset, len, &rsds, sizeof(rsds));                            // RSDS
    offset += sizeof(rsds);
    memcpy_s(dbgDirData + offset, len, m_pPortablePdbWriter->GetGuid(), sizeof(GUID)); // PDB GUID
    offset += sizeof(GUID);
    memcpy_s(dbgDirData + offset, len, &pdbAge, sizeof(pdbAge));                        // PDB AGE
    offset += sizeof(pdbAge);
    memcpy_s(dbgDirData + offset, len, m_szPdbFileName, strlen(m_szPdbFileName) + 1);   // PDB PATH

    debugDirIDD.Characteristics = 0;
    debugDirIDD.TimeDateStamp = m_pPortablePdbWriter->GetTimestamp();
    debugDirIDD.MajorVersion = 0x100;
    debugDirIDD.MinorVersion = 0x504d;
    debugDirIDD.Type = IMAGE_DEBUG_TYPE_CODEVIEW;
    debugDirIDD.SizeOfData = len;
    debugDirIDD.AddressOfRawData = 0; // will be updated bellow
    debugDirIDD.PointerToRawData = 0; // will be updated bellow

    param.debugDirDataSize = len;

    // Make some room for the data.
    PAL_TRY(Param*, pParam, &param) {
        pParam->debugDirData = new BYTE[pParam->debugDirDataSize];
    } PAL_EXCEPT(EXCEPTION_EXECUTE_HANDLER) {
        hr = E_FAIL;
    } PAL_ENDTRY

    if (FAILED(hr)) return hr;

    param.debugDirData = dbgDirData;

    // Grab memory in the section for our stuff.
    // Note that UpdateResource doesn't work correctly if the debug directory is
    // in the data section.  So instead we put it in the text section (same as
    // cs compiler).
    if (FAILED(hr = m_pCeeFileGen->GetSectionBlock(sec,
                                                   sizeof(debugDirIDD) +
                                                   param.debugDirDataSize,
                                                   4,
                                                   (void**) &de)))
        goto ErrExit;

    // Where did we get that memory?
    if (FAILED(hr = m_pCeeFileGen->GetSectionDataLen(sec,
                                                     &deOffset)))
        goto ErrExit;

    deOffset -= (sizeof(debugDirIDD) + param.debugDirDataSize);

    // Setup a reloc so that the address of the raw
    // data is setup correctly.
    debugDirIDD.PointerToRawData = VAL32(deOffset + sizeof(debugDirIDD));

    if (FAILED(hr = m_pCeeFileGen->AddSectionReloc(
                                          sec,
                                          deOffset +
                                          offsetof(IMAGE_DEBUG_DIRECTORY,
                                                   PointerToRawData),
                                          sec, srRelocFilePos)))
        goto ErrExit;

    debugDirIDD.AddressOfRawData = VAL32(deOffset + sizeof(debugDirIDD));

    if (FAILED(hr = m_pCeeFileGen->AddSectionReloc(
                                          sec,
                                          deOffset +
                                          offsetof(IMAGE_DEBUG_DIRECTORY,
                                                   AddressOfRawData),
                                          sec, srRelocAbsolute)))
        goto ErrExit;
    // Emit the directory entry.
    if (FAILED(hr = m_pCeeFileGen->SetDirectoryEntry(m_pCeeFile,
                                                     sec,
                                                     IMAGE_DIRECTORY_ENTRY_DEBUG,
                                                     sizeof(debugDirIDD),
                                                     deOffset)))
        goto ErrExit;

    // Copy the debug directory into the section.
    memcpy(de, &debugDirIDD, sizeof(debugDirIDD));
    memcpy(de + sizeof(debugDirIDD), param.debugDirData,
           param.debugDirDataSize);

    if (param.debugDirData)
    {
        delete [] param.debugDirData;
    }
    return S_OK;

ErrExit:
    if (param.debugDirData)
    {
        delete [] param.debugDirData;
    }
    return hr;
}
//#ifdef EXPORT_DIR_ENABLED
HRESULT Assembler::CreateExportDirectory()
{
    HRESULT hr = S_OK;
    DWORD   Nentries = m_EATList.COUNT();
    if(Nentries == 0) return S_OK;

    IMAGE_EXPORT_DIRECTORY  exportDirIDD;
    DWORD                   exportDirDataSize;
    EATEntry               *pEATE;
    unsigned                i, L, ordBase = 0xFFFFFFFF, Ldllname;
    // get the DLL name from output file name
    char*                   pszDllName;
    Ldllname = (unsigned)wcslen(m_wzOutputFileName)*3+3;
    NewArrayHolder<char>    szOutputFileName(new char[Ldllname]);
    memset(szOutputFileName,0,wcslen(m_wzOutputFileName)*3+3);
    WszWideCharToMultiByte(CP_ACP,0,m_wzOutputFileName,-1,szOutputFileName,Ldllname,NULL,NULL);
    pszDllName = strrchr(szOutputFileName,DIRECTORY_SEPARATOR_CHAR_A);
#ifdef TARGET_WINDOWS
    if(pszDllName == NULL) pszDllName = strrchr(szOutputFileName,':');
#endif
    if(pszDllName == NULL) pszDllName = szOutputFileName;
    Ldllname = (unsigned)strlen(pszDllName)+1;

    // Allocate buffer for tables
    for(i = 0, L=0; i < Nentries; i++) L += 1+(unsigned)strlen(m_EATList.PEEK(i)->szAlias);
    exportDirDataSize = Nentries*5*sizeof(WORD) + L + Ldllname;
    NewArrayHolder<BYTE> exportDirData(new BYTE[exportDirDataSize]);
    memset(exportDirData,0,exportDirDataSize);

    // Export address table
    DWORD*  pEAT = (DWORD*)(BYTE*)exportDirData;
    // Name pointer table
    DWORD*  pNPT = pEAT + Nentries;
    // Ordinal table
    WORD*   pOT = (WORD*)(pNPT + Nentries);
    // Export name table
    char*   pENT = (char*)(pOT + Nentries);
    // DLL name
    char*   pDLLName = pENT + L;

    // sort the names/ordinals
    NewArrayHolder<char*> pAlias(new char*[Nentries]);
    for(i = 0; i < Nentries; i++)
    {
        pEATE = m_EATList.PEEK(i);
        pOT[i] = (WORD)pEATE->dwOrdinal;
        if(pOT[i] < ordBase) ordBase = pOT[i];
        pAlias[i] = pEATE->szAlias;
    }
    bool swapped = true;
    unsigned j;
    char*    pch;
    while(swapped)
    {
        swapped = false;
        for(i=1; i < Nentries; i++)
        {
            if(strcmp(pAlias[i-1],pAlias[i]) > 0)
            {
                swapped = true;
                pch = pAlias[i-1];
                pAlias[i-1] = pAlias[i];
                pAlias[i] = pch;
                j = pOT[i-1];
                pOT[i-1] = pOT[i];
                pOT[i] = j;
            }
        }
    }
    // normalize ordinals
    for(i = 0; i < Nentries; i++) pOT[i] -= ordBase;
    // fill the export address table
#ifdef _PREFAST_
#pragma warning(push)
#pragma warning(disable:22008) // "Suppress PREfast warnings about integer overflow"
#endif
    for(i = 0; i < Nentries; i++)
    {
        pEATE = m_EATList.PEEK(i);
        pEAT[pEATE->dwOrdinal - ordBase] = pEATE->dwStubRVA;
    }
#ifdef _PREFAST_
#pragma warning(pop)
#endif
    // fill the export names table
    unsigned l;
    for(i = 0, j = 0; i < Nentries; i++)
    {
        pNPT[i] = j; // relative offset in the table
        l = (unsigned)strlen(pAlias[i])+1;
        memcpy(&pENT[j],pAlias[i],l);
        j+=l;
    }
    _ASSERTE(j==L);
    // fill the DLL name
    memcpy(pDLLName,pszDllName,Ldllname);

    // Data blob is ready pending Name Pointer Table values offsetting

    memset(&exportDirIDD,0,sizeof(IMAGE_EXPORT_DIRECTORY));
    // Grab the timestamp of the PE file.
    DWORD fileTimeStamp;
    if (FAILED(hr = m_pCeeFileGen->GetFileTimeStamp(m_pCeeFile,&fileTimeStamp))) return hr;
    // Fill in the directory entry.
    // Characteristics, MajorVersion and MinorVersion play no role and stay 0
    exportDirIDD.TimeDateStamp = VAL32(fileTimeStamp);
    exportDirIDD.Name = VAL32(exportDirDataSize - Ldllname); // to be offset later
    exportDirIDD.Base = VAL32(ordBase);
    exportDirIDD.NumberOfFunctions = VAL32(Nentries);
    exportDirIDD.NumberOfNames = VAL32(Nentries);
    exportDirIDD.AddressOfFunctions = 0;    // to be offset later
    exportDirIDD.AddressOfNames = VAL32(Nentries*sizeof(DWORD));   // to be offset later
    exportDirIDD.AddressOfNameOrdinals = VAL32(Nentries*sizeof(DWORD)*2);  // to be offset later

    // Grab memory in the section for our stuff.
    HCEESECTION sec = m_pGlobalDataSection;
    BYTE *de;
    if (FAILED(hr = m_pCeeFileGen->GetSectionBlock(sec,
                                                   sizeof(IMAGE_EXPORT_DIRECTORY) + exportDirDataSize,
                                                   4,
                                                   (void**) &de))) return hr;
    // Where did we get that memory?
    ULONG deOffset, deDataOffset;
    if (FAILED(hr = m_pCeeFileGen->GetSectionDataLen(sec, &deDataOffset))) return hr;

    deDataOffset -= exportDirDataSize;
    deOffset = deDataOffset - sizeof(IMAGE_EXPORT_DIRECTORY);

    // Add offsets and set up relocs for header entries
    exportDirIDD.Name = VAL32(VAL32(exportDirIDD.Name) + deDataOffset);
    if (FAILED(hr = m_pCeeFileGen->AddSectionReloc(sec,deOffset + offsetof(IMAGE_EXPORT_DIRECTORY,Name),
                                          sec, srRelocAbsolute))) return hr;
    exportDirIDD.AddressOfFunctions = VAL32(VAL32(exportDirIDD.AddressOfFunctions) + deDataOffset);
    if (FAILED(hr = m_pCeeFileGen->AddSectionReloc(sec,deOffset + offsetof(IMAGE_EXPORT_DIRECTORY,AddressOfFunctions),
                                          sec, srRelocAbsolute))) return hr;
    exportDirIDD.AddressOfNames = VAL32(VAL32(exportDirIDD.AddressOfNames) + deDataOffset);
    if (FAILED(hr = m_pCeeFileGen->AddSectionReloc(sec,deOffset + offsetof(IMAGE_EXPORT_DIRECTORY,AddressOfNames),
                                          sec, srRelocAbsolute))) return hr;
    exportDirIDD.AddressOfNameOrdinals = VAL32(VAL32(exportDirIDD.AddressOfNameOrdinals) + deDataOffset);
    if (FAILED(hr = m_pCeeFileGen->AddSectionReloc(sec,deOffset + offsetof(IMAGE_EXPORT_DIRECTORY,AddressOfNameOrdinals),
                                          sec, srRelocAbsolute))) return hr;

    // Add offsets and set up relocs for Name Pointer Table
    j = deDataOffset + Nentries*5*sizeof(WORD); // EA, NP and O Tables come first
    for(i = 0; i < Nentries; i++)
    {
        pNPT[i] += j;
        if (FAILED(hr = m_pCeeFileGen->AddSectionReloc(sec,exportDirIDD.AddressOfNames+i*sizeof(DWORD),
            sec, srRelocAbsolute))) return hr;
    }


    // Emit the directory entry.
    if (FAILED(hr = m_pCeeFileGen->SetDirectoryEntry(m_pCeeFile, sec, IMAGE_DIRECTORY_ENTRY_EXPORT,
                                                     sizeof(IMAGE_EXPORT_DIRECTORY), deOffset)))  return hr;

    // Copy the debug directory into the section.
    memcpy(de, &exportDirIDD, sizeof(IMAGE_EXPORT_DIRECTORY));
    memcpy(de + sizeof(IMAGE_EXPORT_DIRECTORY), exportDirData, exportDirDataSize);
    return S_OK;
}

static const BYTE ExportStubAMD64Template[] =
{
	// Jump through VTFixup table
	0x48, 0xA1,				// rex.w rex.b mov rax,[following address]
    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,//address of VTFixup slot
    0xFF, 0xE0              // jmp [rax]
};
static const BYTE ExportStubX86Template[] =
{
	// Jump through VTFixup table
	0xFF, 0x25,				// jmp [following address]
    0x00, 0x00, 0x00, 0x00  //address of VTFixup slot
};
static const WORD ExportStubARMTemplate[] =
{
	// Jump through VTFixup table
    0xf8df, 0xf000,         // ldr pc, [pc, #0]
    0x0000, 0x0000          //address of VTFixup slot
};

DWORD   Assembler::EmitExportStub(DWORD dwVTFSlotRVA)
{
    DWORD EXPORT_STUB_SIZE = (DWORD)(sizeof(WORD)+sizeof(DWORD));
    DWORD OFFSET_OF_ADDR = (DWORD)sizeof(WORD);
    DWORD STUB_ALIGNMENT = 16;
    BYTE* STUB_TEMPLATE = NULL;
    DWORD PEFileOffset;
    BYTE* outBuff;
    DWORD*  pdwVTFSlotRVA;

    if(m_dwCeeFileFlags & ICEE_CREATE_MACHINE_AMD64)
    {
        STUB_TEMPLATE = (BYTE*)&ExportStubAMD64Template[0];
        EXPORT_STUB_SIZE = sizeof(ExportStubAMD64Template);
        OFFSET_OF_ADDR = 2;
        STUB_ALIGNMENT = 4;
    }
    else if(m_dwCeeFileFlags & ICEE_CREATE_MACHINE_I386)
    {
        STUB_TEMPLATE = (BYTE*)&ExportStubX86Template[0];
        EXPORT_STUB_SIZE = sizeof(ExportStubX86Template);
        OFFSET_OF_ADDR = 2;
    }
    else if(m_dwCeeFileFlags & ICEE_CREATE_MACHINE_ARM)
    {
        STUB_TEMPLATE = (BYTE*)&ExportStubARMTemplate[0];
        EXPORT_STUB_SIZE = sizeof(ExportStubARMTemplate);
        OFFSET_OF_ADDR = 4;
        STUB_ALIGNMENT = 4;
    }
    else
    {
        report->error("Unmanaged exports are not implemented for unknown platform");
        return NULL;
    }
    // Addr must be aligned, not the stub!
    if (FAILED(m_pCeeFileGen->GetSectionDataLen (m_pILSection, &PEFileOffset))) return 0;
    if((PEFileOffset + OFFSET_OF_ADDR)&(STUB_ALIGNMENT-1))
    {
        ULONG L = STUB_ALIGNMENT - ((PEFileOffset + OFFSET_OF_ADDR)&(STUB_ALIGNMENT-1));
        if (FAILED(m_pCeeFileGen->GetSectionBlock (m_pILSection, L, 1, (void **) &outBuff))) return 0;
        memset(outBuff,0,L);
    }

    if (FAILED(m_pCeeFileGen->GetSectionBlock (m_pILSection, EXPORT_STUB_SIZE, 1, (void **) &outBuff))) return 0;
    memcpy(outBuff,STUB_TEMPLATE,EXPORT_STUB_SIZE);
    pdwVTFSlotRVA = (DWORD*)(&outBuff[OFFSET_OF_ADDR]);
    *pdwVTFSlotRVA = VAL32(dwVTFSlotRVA);

    // The offset where we start, (not where the alignment bytes start!)
    if (FAILED(m_pCeeFileGen->GetSectionDataLen (m_pILSection, &PEFileOffset))) return 0;

    PEFileOffset -= EXPORT_STUB_SIZE;
    _ASSERTE(((PEFileOffset + OFFSET_OF_ADDR)&(STUB_ALIGNMENT-1))==0);
    m_pCeeFileGen->AddSectionReloc(m_pILSection, PEFileOffset+OFFSET_OF_ADDR,m_pGlobalDataSection, srRelocHighLow);

    if(m_dwCeeFileFlags & ICEE_CREATE_FILE_STRIP_RELOCS)
    {
        report->error("Base relocations are emitted, while /STRIPRELOC option has been specified");
    }
    m_pCeeFileGen->GetMethodRVA(m_pCeeFile, PEFileOffset,&PEFileOffset);
    return PEFileOffset;
}
//#endif

HRESULT Assembler::GetCAName(mdToken tkCA, __out LPWSTR *ppszName)
{
    HRESULT hr = S_OK;
    DWORD cchName;
    LPWSTR name;

    *ppszName = NULL;

    if (TypeFromToken(tkCA) == mdtMemberRef)
    {
        mdToken parent;
        if (FAILED(hr = m_pImporter->GetMemberRefProps( tkCA, &parent, NULL, 0, NULL, NULL, NULL)))
            return hr;
        tkCA = parent;
    }
    else if (TypeFromToken(tkCA) == mdtMethodDef)
    {
        mdToken parent;
        if (FAILED(hr = m_pImporter->GetMemberProps( tkCA, &parent, NULL, 0, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL)))
            return hr;
        tkCA = parent;
    }

    if (TypeFromToken(tkCA) == mdtTypeRef)
    {
        // A TypeRef
        if (FAILED(hr = m_pImporter->GetTypeRefProps(tkCA, NULL, NULL, 0, &cchName)))
            return hr;
        if ((name = new WCHAR[cchName + 1]) == NULL)
            return E_OUTOFMEMORY;
        hr = m_pImporter->GetTypeRefProps(tkCA, NULL, name, cchName, &cchName);
    }
    else
    {
        hr = m_pImporter->GetTypeDefProps(tkCA, NULL, 0, &cchName, NULL, NULL);
        if (hr != S_OK)
            return hr;
        if ((name = new WCHAR[cchName + 1]) == NULL)
            return E_OUTOFMEMORY;
        hr = m_pImporter->GetTypeDefProps(tkCA, name, cchName, &cchName, NULL, NULL);
    }
    if (SUCCEEDED(hr))
        *ppszName = name;
    else
        delete [] name;
    return hr;
}

BYTE HexToByte (CHAR wc)
{
    if (!iswxdigit(wc)) return (BYTE) 0xff;
    if (iswdigit(wc)) return (BYTE) (wc - L'0');
    if (iswupper(wc)) return (BYTE) (wc - L'A' + 10);
    return (BYTE) (wc - L'a' + 10);
}


BOOL Assembler::EmitFieldsMethods(Class* pClass)
{
    unsigned n;
    BOOL ret = TRUE;
    // emit all field definition metadata tokens
    if((n = pClass->m_FieldDList.COUNT()))
    {
        FieldDescriptor*    pFD;
        if(m_fReportProgress) printf("Fields: %d;\t",n);
        for(int j=0; (pFD = pClass->m_FieldDList.PEEK(j)); j++) // can't use POP here: we'll need field list for props
        {
            if(!EmitField(pFD))
            {
                if(!OnErrGo) return FALSE;
                ret = FALSE;
            }
            pFD->m_fNew = FALSE;
        }
    }
    // Fields are emitted; emit the class layout
    {
        COR_FIELD_OFFSET *pOffsets = NULL;
        ULONG ul = pClass->m_ulPack;
        ULONG N = pClass->m_dwNumFieldsWithOffset;

        EmitSecurityInfo(pClass->m_cl,
                         pClass->m_pPermissions,
                         pClass->m_pPermissionSets);
        pClass->m_pPermissions = NULL;
        pClass->m_pPermissionSets = NULL;
        if((pClass->m_ulSize != 0xFFFFFFFF)||(ul != 0)||(N != 0))
        {
            if(IsTdAutoLayout(pClass->m_Attr)) report->warn("Layout specified for auto-layout class\n");
            if((ul > 128)||((ul & (ul-1)) !=0 ))
                report->error("Invalid packing parameter (%d), must be 1,2,4,8...128\n",pClass->m_ulPack);
            if(N)
            {
                pOffsets = new COR_FIELD_OFFSET[N+1];
                ULONG i,j=0;
                FieldDescriptor *pFD;
                for(i=0; (pFD = pClass->m_FieldDList.PEEK(i)); i++)
                {
                    if(pFD->m_ulOffset != 0xFFFFFFFF)
                    {
                        pOffsets[j].ridOfField = RidFromToken(pFD->m_fdFieldTok);
                        pOffsets[j].ulOffset = pFD->m_ulOffset;
                        j++;
                    }
                }
                _ASSERTE(j == N);
                pOffsets[j].ridOfField = mdFieldDefNil;
            }
            m_pEmitter->SetClassLayout   (
                        pClass->m_cl,       // [IN] typedef
                        ul,                     // [IN] packing size specified as 1, 2, 4, 8, or 16
                        pOffsets,               // [IN] array of layout specification
                        pClass->m_ulSize); // [IN] size of the class
            if(pOffsets) delete [] pOffsets;
        }
    }
    // emit all method definition metadata tokens
    if((n = pClass->m_MethodList.COUNT()))
    {
        Method* pMethod;

        if(m_fReportProgress) printf("Methods: %d;\t",n);
        for(int i=0; (pMethod = pClass->m_MethodList.PEEK(i));i++)
        {
            if(!EmitMethod(pMethod))
            {
                if(!OnErrGo) return FALSE;
                ret = FALSE;
            }
            pMethod->m_fNew = FALSE;
        }
    }
    if(m_fReportProgress) printf("\n");
    return ret;
}

HRESULT Assembler::ResolveLocalMemberRefs()
{
    unsigned ulTotal=0, ulDefs=0, ulRefs=0, ulUnres=0;
    MemberRefDList* pList[2] = {&m_LocalMethodRefDList,&m_LocalFieldRefDList};

    if(pList[0]->COUNT() + pList[1]->COUNT())
    {
        MemberRefDescriptor*    pMRD;
        mdToken         tkMemberDef = 0;
        int i,j,k;
        Class   *pSearch;

        if(m_fReportProgress) printf("Resolving local member refs: ");
        for(k=0; k<2; k++)
        {
            for(i=0; (pMRD = pList[k]->PEEK(i)) != NULL; i++)
            {
                if(pMRD->m_tkResolved) continue;

                tkMemberDef = 0;
                Method* pListMD;
                char*           pMRD_szName = pMRD->m_szName;
                DWORD           pMRD_dwName = pMRD->m_dwName;
                ULONG           pMRD_dwCSig = (pMRD->m_pSigBinStr ? pMRD->m_pSigBinStr->length() : 0);
                PCOR_SIGNATURE  pMRD_pSig = (PCOR_SIGNATURE)(pMRD->m_pSigBinStr ? pMRD->m_pSigBinStr->ptr() : NULL);
                CQuickBytes     qbSig;

                ulTotal++;

                pSearch = NULL;
                if(pMRD->m_tdClass == mdTokenNil)
                    pSearch = m_lstClass.PEEK(0);
                else if((TypeFromToken(pMRD->m_tdClass) != mdtTypeDef)
                    ||((pSearch = m_lstClass.PEEK(RidFromToken(pMRD->m_tdClass)-1)) == NULL))
                {
                    report->msg("Error: bad parent 0x%08X of local member ref '%s'\n",
                        pMRD->m_tdClass,pMRD->m_szName);
                }
                if(pSearch)
                {
                    // MemberRef may reference a method or a field
                    if(k==0) //methods
                    {
                        if((*pMRD_pSig & IMAGE_CEE_CS_CALLCONV_MASK)==IMAGE_CEE_CS_CALLCONV_VARARG)
                        {
                            ULONG L;
                            qbSig.Shrink(0);
                            _GetFixedSigOfVarArg(pMRD_pSig,pMRD_dwCSig,&qbSig,&L);
                            pMRD_pSig = (PCOR_SIGNATURE)(qbSig.Ptr());
                            pMRD_dwCSig = L;
                        }
                        for(j=0; (pListMD = pSearch->m_MethodList.PEEK(j)) != NULL; j++)
                        {
                            if(pListMD->m_dwName != pMRD_dwName) continue;
                            if(strcmp(pListMD->m_szName,pMRD_szName)) continue;
                            if(pListMD->m_dwMethodCSig  != pMRD_dwCSig)  continue;
                            if(memcmp(pListMD->m_pMethodSig,pMRD_pSig,pMRD_dwCSig)) continue;
                            tkMemberDef = pListMD->m_Tok;
                            ulDefs++;
                            break;
                        }
                        if(tkMemberDef && ((*pMRD_pSig & IMAGE_CEE_CS_CALLCONV_MASK)==IMAGE_CEE_CS_CALLCONV_VARARG))
                        {
                            WszMultiByteToWideChar(g_uCodePage,0,pMRD_szName,-1,wzUniBuf,dwUniBuf);

                            if(IsMdPrivateScope(pListMD->m_Attr))
                            {
                                WCHAR* p = wcsstr(wzUniBuf,W("$PST06"));
                                if(p) *p = 0;
                            }

                            m_pEmitter->DefineMemberRef(tkMemberDef, wzUniBuf,
                                                             pMRD->m_pSigBinStr->ptr(),
                                                             pMRD->m_pSigBinStr->length(),
                                                             &tkMemberDef);
                            ulDefs--;
                            ulRefs++;
                        }
                    }
                    else   // fields
                    {
                        FieldDescriptor* pListFD;
                        for(j=0; (pListFD = pSearch->m_FieldDList.PEEK(j)) != NULL; j++)
                        {
                            if(pListFD->m_dwName != pMRD_dwName) continue;
                            if(strcmp(pListFD->m_szName,pMRD_szName)) continue;
                            if(pListFD->m_pbsSig)
                            {
                                if(pListFD->m_pbsSig->length()  != pMRD_dwCSig)  continue;
                                if(memcmp(pListFD->m_pbsSig->ptr(),pMRD_pSig,pMRD_dwCSig)) continue;
                            }
                            else if(pMRD_dwCSig) continue;
                            tkMemberDef = pListFD->m_fdFieldTok;
                            ulDefs++;
                            break;
                        }
                    }
                }
                if(tkMemberDef==0)
                { // could not resolve ref to def, make new ref and leave it this way
                    if((pSearch = pMRD->m_pClass) != NULL)
                    {
                        mdToken tkRef = MakeTypeRef(1,pSearch->m_szFQN);

                        if(RidFromToken(tkRef))
                        {
                            WszMultiByteToWideChar(g_uCodePage,0,pMRD_szName,-1,wzUniBuf,dwUniBuf);

                            m_pEmitter->DefineMemberRef(tkRef, wzUniBuf, pMRD_pSig,
                                pMRD_dwCSig, &tkMemberDef);
                            ulRefs++;
                        }
                        else
                        {
                            report->msg("Error: unresolved member ref '%s' of class 0x%08X\n",pMRD->m_szName,pMRD->m_tdClass);
                            ulUnres++;
                        }
                    }
                    else
                    {
                        report->msg("Error: unresolved global member ref '%s'\n",pMRD->m_szName);
                        ulUnres++;
                    }
                }
                pMRD->m_tkResolved = tkMemberDef;
            }
        }
        for(i=0; (pMRD = m_MethodSpecList.PEEK(i)) != NULL; i++)
        {
            if(pMRD->m_tkResolved) continue;
            tkMemberDef = pMRD->m_tdClass;
            if(TypeFromToken(tkMemberDef)==0x99000000)
            {
                tkMemberDef = m_LocalMethodRefDList.PEEK(RidFromToken(tkMemberDef)-1)->m_tkResolved;
                if((TypeFromToken(tkMemberDef)==mdtMethodDef)||(TypeFromToken(tkMemberDef)==mdtMemberRef))
                {
                    ULONG           pMRD_dwCSig = (pMRD->m_pSigBinStr ? pMRD->m_pSigBinStr->length() : 0);
                    PCOR_SIGNATURE  pMRD_pSig = (PCOR_SIGNATURE)(pMRD->m_pSigBinStr ? pMRD->m_pSigBinStr->ptr() : NULL);
                    HRESULT hr = m_pEmitter->DefineMethodSpec(tkMemberDef, pMRD_pSig, pMRD_dwCSig, &(pMRD->m_tkResolved));
                    if(FAILED(hr))
                        report->error("Unable to define method instantiation");
                }
            }
            if(RidFromToken(pMRD->m_tkResolved)) ulDefs++;
            else ulUnres++;
        }
        if(m_fReportProgress) printf("%d -> %d defs, %d refs, %d unresolved\n",ulTotal,ulDefs,ulRefs,ulUnres);
    }
    return (ulUnres ? E_FAIL : S_OK);
}

HRESULT Assembler::DoLocalMemberRefFixups()
{
    MemberRefDList* pList;
    unsigned    Nlmr = m_LocalMethodRefDList.COUNT() + m_LocalFieldRefDList.COUNT(),
                Nlmrf = m_LocalMemberRefFixupList.COUNT();
    HRESULT     hr = S_OK;
    if(Nlmr)
    {
        MemberRefDescriptor* pMRD;
        LocalMemberRefFixup* pMRF;
        int i;
        for(i = 0; (pMRF = m_LocalMemberRefFixupList.PEEK(i)) != NULL; i++)
        {
            switch(TypeFromToken(pMRF->tk))
            {
                case 0x99000000: pList = &m_LocalMethodRefDList; break;
                case 0x98000000: pList = &m_LocalFieldRefDList; break;
                case 0x9A000000: pList = &m_MethodSpecList; break;
                default: pList = NULL; break;
            }
            if(pList)
            {
                if((pMRD = pList->PEEK(RidFromToken(pMRF->tk)-1)) != NULL)
                    SET_UNALIGNED_VAL32((void *)(pMRF->offset), pMRD->m_tkResolved);
                else
                {
                    report->msg("Error: bad local member ref token 0x%08X in LMR fixup\n",pMRF->tk);
                    hr = E_FAIL;
                }
            }
            pMRF->m_fNew = FALSE;
        }
    }
    else if(Nlmrf)
    {
        report->msg("Error: %d local member ref fixups, no local member refs\n",Nlmrf);
        hr = E_FAIL;
    }
    return hr;
}
void Assembler::EmitUnresolvedCustomAttributes()
{
    CustomDescr *pCD;
    while((pCD = m_CustomDescrList.POP()) != NULL)
    {
        pCD->tkType = ResolveLocalMemberRef(pCD->tkType);
        pCD->tkOwner = ResolveLocalMemberRef(pCD->tkOwner);
        // Look for the class'es interfaceimpl if this CA is one of those
        if (pCD->tkInterfacePair)
            pCD->tkOwner = GetInterfaceImpl(pCD->tkOwner, pCD->tkInterfacePair);
        DefineCV(new CustomDescr(pCD->tkOwner,pCD->tkType,pCD->pBlob));
    }
}

BOOL Assembler::EmitEventsProps(Class* pClass)
{
    unsigned n;
    BOOL ret = TRUE;
    // emit all event definition metadata tokens
    if((n = pClass->m_EventDList.COUNT()))
    {
        if(m_fReportProgress) printf("Events: %d;\t",n);
        EventDescriptor* pED;
        for(int j=0; (pED = pClass->m_EventDList.PEEK(j)); j++) // can't use POP here: we'll need event list for props
        {
            if(!EmitEvent(pED))
            {
                if(!OnErrGo) return FALSE;
                ret = FALSE;
            }
            pED->m_fNew = FALSE;
        }
    }
    // emit all property definition metadata tokens
    if((n = pClass->m_PropDList.COUNT()))
    {
        if(m_fReportProgress) printf("Props: %d;\t",n);
        PropDescriptor* pPD;

        for(int j=0; (pPD = pClass->m_PropDList.PEEK(j)); j++)
        {
            if(!EmitProp(pPD))
            {
                if(!OnErrGo) return FALSE;
                ret = FALSE;
            }
            pPD->m_fNew = FALSE;
        }
    }
    if(m_fReportProgress) printf("\n");
    return ret;
}

HRESULT Assembler::AllocateStrongNameSignature()
{
    HRESULT             hr = S_OK;
    HCEESECTION         hSection;
    DWORD               dwDataLength;
    DWORD               dwDataOffset;
    DWORD               dwDataRVA;
    VOID               *pvBuffer;
    AsmManStrongName   *pSN = &m_pManifest->m_sStrongName;

    // pSN->m_cbPublicKey is the length of the m_pbPublicKey
    dwDataLength = ((int)pSN->m_cbPublicKey < 128 + 32) ? 128 : (int)pSN->m_cbPublicKey - 32;

    // Grab memory in the section for our stuff.
    if (FAILED(hr = m_pCeeFileGen->GetIlSection(m_pCeeFile,
                                                &hSection)))
    {
        return hr;
    }

    if (FAILED(hr = m_pCeeFileGen->GetSectionBlock(hSection,
                                                   dwDataLength,
                                                   4,
                                                   &pvBuffer)))
    {
        return hr;
    }

    // Where did we get that memory?
    if (FAILED(hr = m_pCeeFileGen->GetSectionDataLen(hSection,
                                                     &dwDataOffset)))
    {
        return hr;
    }

    dwDataOffset -= dwDataLength;

    // Convert to an RVA.
    if (FAILED(hr = m_pCeeFileGen->GetMethodRVA(m_pCeeFile,
                                                dwDataOffset,
                                                &dwDataRVA)))
    {
        return hr;
    }

    // Emit the directory entry.
    if (FAILED(hr = m_pCeeFileGen->SetStrongNameEntry(m_pCeeFile,
                                                      dwDataLength,
                                                      dwDataRVA)))
    {
        return hr;
    }

    return S_OK;
}

#ifdef _PREFAST_
#pragma warning(push)
#pragma warning(disable:21000) // Suppress PREFast warning about overly large function
#endif
HRESULT Assembler::CreatePEFile(__in __nullterminated WCHAR *pwzOutputFilename)
{
    HRESULT             hr;
    DWORD               mresourceSize = 0;
    BYTE*               mresourceData = NULL;
    WCHAR*              wzScopeName = NULL;

    if(bClock) bClock->cMDEmitBegin = GetTickCount();
    if(m_fReportProgress) printf("Creating PE file\n");
    if (!m_pEmitter)
    {
        printf("Error: Cannot create a PE file with no metadata\n");
        return E_FAIL;
    }
    if(!(m_fDLL || m_fEntryPointPresent))
    {
        printf("Error: No entry point declared for executable\n");
        if(!OnErrGo) return E_FAIL;
    }

    if(bClock) bClock->cMDEmit1 = GetTickCount();

    // Allocate space for a strong name signature if we're delay or full
    // signing the assembly.
    if (m_pManifest->m_sStrongName.m_pbPublicKey)
    {
        if (FAILED(hr = AllocateStrongNameSignature()))
        {
            goto exit;
        }

        // Public-sign by default
        m_dwComImageFlags |= COMIMAGE_FLAGS_STRONGNAMESIGNED;
    }

    if(bClock) bClock->cMDEmit2 = GetTickCount();

    if(m_VTFList.COUNT()==0)
    {
        Method* pMD;
        Class* pClass;
        unsigned N=0, OrdBase=0xFFFFFFFF, i, j;
        for(i=0; (pClass = m_lstClass.PEEK(i)) != NULL; i++)
        {
            for(j = 0; (pMD = pClass->m_MethodList.PEEK(j)) != NULL; j++)
            {
                if(pMD->m_dwExportOrdinal != 0xFFFFFFFF)
                {
                    N++;
                    if(pMD->m_dwExportOrdinal < OrdBase) OrdBase = pMD->m_dwExportOrdinal;
                }
            }
        }
        if(N)
        {
            for(i=0; (pClass = m_lstClass.PEEK(i)) != NULL; i++)
            {
                for(j = 0; (pMD = pClass->m_MethodList.PEEK(j)) != NULL; j++)
                {
                    if(pMD->m_wVTSlot >= 0x8000)
                    {
                        pMD->m_wVTSlot -= 0x8000 + OrdBase - 1;
                    }
                }
            }
            SetDataSection();
            char* sz = new char[20];
            strcpy_s(sz,20,"VTF_EAT_internal");
            EmitDataLabel(sz);
            sz = new char[20];
            strcpy_s(sz,20,"VTF_EAT_internal");
            if(m_dwCeeFileFlags & ICEE_CREATE_FILE_PE64)
            {
                ULONGLONG *pdw = new ULONGLONG[N];
                for(i=0; i<N; i++) pdw[i] = UI64(0xdeadbeefdeadbeef);
                EmitData(pdw,sizeof(ULONGLONG)*N);
                m_VTFList.PUSH(new VTFEntry((USHORT)N,COR_VTABLE_64BIT|COR_VTABLE_FROM_UNMANAGED,sz));
                delete [] pdw;
            }
            else
            {
                unsigned *pdw = new unsigned[N];
                for(i=0; i<N; i++) pdw[i] = 0xdeadbeef;
                EmitData(pdw,sizeof(unsigned)*N);
                m_VTFList.PUSH(new VTFEntry((USHORT)N,COR_VTABLE_32BIT|COR_VTABLE_FROM_UNMANAGED,sz));
                delete [] pdw;
            }
        }
    }
    wzScopeName=&wzUniBuf[0];
    if(m_szScopeName[0]) // default: scope name = output file name
    {
        WszMultiByteToWideChar(g_uCodePage,0,m_szScopeName,-1,wzScopeName,MAX_SCOPE_LENGTH);
    }
    else
    {
        WCHAR* pwc;
        if ((pwc = wcsrchr(m_wzOutputFileName, DIRECTORY_SEPARATOR_CHAR_A)) != NULL) pwc++;
#ifdef TARGET_WINDOWS
        else if ((pwc = wcsrchr(m_wzOutputFileName, ':')) != NULL) pwc++;
#endif
        else pwc = m_wzOutputFileName;

        wcsncpy_s(wzScopeName, MAX_SCOPE_LENGTH, pwc, _TRUNCATE);
    }
    hr = m_pEmitter->SetModuleProps(wzScopeName);

    if (FAILED(hr))
        goto exit;

    EmitImports();
    if(m_pManifest)
    {
        hr = S_OK;
        if(m_pManifest->m_pAsmEmitter==NULL)
            hr=m_pEmitter->QueryInterface(IID_IMetaDataAssemblyEmit, (void**) &(m_pManifest->m_pAsmEmitter));

        if(SUCCEEDED(hr))
        {
            m_pManifest->EmitAssemblyRefs();
        }
    }
    // Emit classes, class members and globals:
    {
        Class *pSearch;
        int i;
        BOOL    bIsUndefClass = FALSE;
        if(m_fReportProgress)   printf("\nEmitting classes:\n");
        for (i=1; (pSearch = m_lstClass.PEEK(i)); i++)   // 0 is <Module>
        {
            if(m_fReportProgress)
                printf("Class %d:\t%s\n",i,pSearch->m_szFQN);

            if(pSearch->m_bIsMaster)
            {
                report->msg("Error: Reference to undefined class '%s'\n",pSearch->m_szFQN);
                bIsUndefClass = TRUE;
            }
            if(!EmitClass(pSearch))
            {
                if(!OnErrGo) return E_FAIL;
            }
            pSearch->m_fNew = FALSE;
        }
        if(bIsUndefClass && !OnErrGo) return E_FAIL;

        if(m_fReportProgress)   printf("\nEmitting fields and methods:\n");
        for (i=0; (pSearch = m_lstClass.PEEK(i)) != NULL; i++)
        {
            if(m_fReportProgress)
            {
                if(i == 0)  printf("Global \t");
                else        printf("Class %d\t",i);
            }
            if(!EmitFieldsMethods(pSearch))
            {
                if(!OnErrGo) return E_FAIL;
            }
        }
    }

    // All ref'ed items def'ed in this file are emitted, resolve member refs to member defs:
    if(bClock) bClock->cRef2DefBegin = GetTickCount();
    hr = ResolveLocalMemberRefs();
    if(bClock) bClock->cRef2DefEnd = GetTickCount();
    if(FAILED(hr) &&(!OnErrGo)) goto exit;

    // Local member refs resolved, emit events, props and method impls
    {
        Class *pSearch;
        int i;

        if(m_fReportProgress)   printf("\nEmitting events and properties:\n");
        for (i=0; (pSearch = m_lstClass.PEEK(i)); i++)
        {
            if(m_fReportProgress)
            {
                if(i == 0)  printf("Global \t");
                else        printf("Class %d\t",i);
            }
            if(!EmitEventsProps(pSearch))
            {
                if(!OnErrGo) return E_FAIL;
            }
            pSearch->m_fNewMembers = FALSE;
        }
    }
    if(bClock) bClock->cMDEmit3 = GetTickCount();
    if(m_MethodImplDList.COUNT())
    {
        if(m_fReportProgress) report->msg("Method Implementations (total): %d\n",m_MethodImplDList.COUNT());
        if(!EmitMethodImpls())
        {
            if(!OnErrGo) return E_FAIL;
        }
    }
    // Emit the rest of the metadata
    if(bClock) bClock->cMDEmit4 = GetTickCount();
    hr = S_OK;
    if(m_pManifest)
    {
        if (FAILED(hr = m_pManifest->EmitManifest())) goto exit;
    }
    ResolveLocalMemberRefs(); // in case CAs added some
    EmitUnresolvedCustomAttributes();
    // Emit typedefs as special TypeSpecs
    {
#define ELEMENT_TYPE_TYPEDEF (ELEMENT_TYPE_MAX+1)
        TypeDefDescr* pTDD;
        unsigned __int8* pb;
        unsigned namesize;
        while((pTDD = m_TypeDefDList.POP()))
        {
            BinStr* pbs = new BinStr();
            if(pbs)
            {
                namesize = 1 + (unsigned)strlen(pTDD->m_szName);
                pb = pbs->getBuff(namesize + 1 + sizeof(mdToken));
                *pb = ELEMENT_TYPE_TYPEDEF;
                memcpy(++pb,pTDD->m_szName,namesize);
                pTDD->m_tkTypeSpec = ResolveLocalMemberRef(pTDD->m_tkTypeSpec);
                SET_UNALIGNED_VAL32(pb+namesize, pTDD->m_tkTypeSpec);
                if(TypeFromToken(pTDD->m_tkTypeSpec)==mdtCustomAttribute)
                {
                    CustomDescr* pCA = pTDD->m_pCA;
                    pbs->appendInt32(VAL32(pCA->tkType));
                    pbs->appendInt32(VAL32(pCA->tkOwner));
                    if(pCA->pBlob) pbs->append(pCA->pBlob);
                }
                ResolveTypeSpec(pbs);
                delete pbs;
            }
            delete pTDD;
        }
    }
    if(bClock) bClock->cMDEmitEnd = GetTickCount();

    hr = DoLocalMemberRefFixups();
    if(FAILED(hr) &&(!OnErrGo)) goto exit;
    // Local member refs resolved and fixed up in BinStr method bodies. Emit the bodies.
    {
        Class* pClass;
        Method* pMethod;
        for (int i=0; (pClass = m_lstClass.PEEK(i)); i++)
        {
            for(int j=0; (pMethod = pClass->m_MethodList.PEEK(j)); j++)
            {
                if(!EmitMethodBody(pMethod,NULL))
                {
                    report->msg("Error: failed to emit body of '%s'\n",pMethod->m_szName);
                    hr = E_FAIL;
                    if(!OnErrGo) goto exit;
                }
                pMethod->m_fNewBody = FALSE;
            }
        }
        //while(MethodBody* pMB = m_MethodBodyList.POP()) delete pMB;
    }

    if (DoGlobalFixups() == FALSE)
        return E_FAIL;

    if(m_wzResourceFile)
    {
#ifdef TARGET_UNIX
        report->msg("Warning: The Win32 resource file '%S' is ignored and not emitted on xPlatform.\n", m_wzResourceFile);
#else
        if (FAILED(hr=m_pCeeFileGen->SetResourceFileName(m_pCeeFile, m_wzResourceFile)))
        {
            report->msg("Warning: failed to set Win32 resource file name '%S', hr=0x%8.8X\n         The Win32 resource is not emitted.\n",
                        m_wzResourceFile, hr);
        }
#endif
    }

    if (FAILED(hr=CreateTLSDirectory())) goto exit;

    if (FAILED(hr=CreateDebugDirectory())) goto exit;

    if (FAILED(hr=m_pCeeFileGen->SetOutputFileName(m_pCeeFile, pwzOutputFilename))) goto exit;

        // Reserve a buffer for the meta-data
    DWORD metaDataSize;
    if (FAILED(hr=m_pEmitter->GetSaveSize(cssAccurate, &metaDataSize))) goto exit;
    BYTE* metaData;
    if (FAILED(hr=m_pCeeFileGen->GetSectionBlock(m_pILSection, metaDataSize, sizeof(DWORD), (void**) &metaData))) goto exit;
    ULONG metaDataOffset;
    if (FAILED(hr=m_pCeeFileGen->GetSectionDataLen(m_pILSection, &metaDataOffset))) goto exit;
    metaDataOffset -= metaDataSize;
    // set managed resource entry, if any
    if(m_pManifest && m_pManifest->m_dwMResSizeTotal)
    {
        mresourceSize = m_pManifest->m_dwMResSizeTotal;

        if (FAILED(hr=m_pCeeFileGen->GetSectionBlock(m_pILSection, mresourceSize,
                                            sizeof(DWORD), (void**) &mresourceData))) goto exit;
        if (FAILED(hr=m_pCeeFileGen->SetManifestEntry(m_pCeeFile, mresourceSize, 0))) goto exit;
    }
    if(m_VTFList.COUNT())
    {
        GlobalLabel *pGlobalLabel;
        VTFEntry*   pVTFEntry;

        if(m_pVTable) delete m_pVTable; // can't have both; list takes precedence
        m_pVTable = new BinStr();
        hr = S_OK;
        for(WORD k=0; (pVTFEntry = m_VTFList.POP()); k++)
        {
            if((pGlobalLabel = FindGlobalLabel(pVTFEntry->m_szLabel)))
            {
                Method* pMD;
                Class* pClass;
                m_pVTable->appendInt32(VAL32(pGlobalLabel->m_GlobalOffset));
                m_pVTable->appendInt16(VAL16(pVTFEntry->m_wCount));
                m_pVTable->appendInt16(VAL16(pVTFEntry->m_wType));
                for(int i=0; (pClass = m_lstClass.PEEK(i)); i++)
                {
                    for(WORD j = 0; (pMD = pClass->m_MethodList.PEEK(j)); j++)
                    {
                        if(pMD->m_wVTEntry == k+1)
                        {
                            char*   ptr;
                            if(SUCCEEDED(hr = m_pCeeFileGen->ComputeSectionPointer(m_pGlobalDataSection,pGlobalLabel->m_GlobalOffset,&ptr)))
                            {
                                DWORD dwDelta;
                                if((pVTFEntry->m_wType & COR_VTABLE_32BIT))
                                {
                                    dwDelta = (pMD->m_wVTSlot-1)*(DWORD)sizeof(DWORD);
                                    ptr += dwDelta;
                                    DWORD* mptr = (DWORD*)ptr;
                                    *mptr = (DWORD)(pMD->m_Tok);
                                }
                                else
                                {
                                    dwDelta = (pMD->m_wVTSlot-1)*(DWORD)sizeof(ULONGLONG);
                                    ptr += dwDelta;
                                    ULONGLONG* mptr = (ULONGLONG*)ptr;
                                    *mptr = (ULONGLONG)(pMD->m_Tok);
                                }
                                if(pMD->m_dwExportOrdinal != 0xFFFFFFFF)
                                {
                                    EATEntry*   pEATE = new EATEntry;
                                    pEATE->dwOrdinal = pMD->m_dwExportOrdinal;
                                    pEATE->szAlias = pMD->m_szExportAlias ? pMD->m_szExportAlias : pMD->m_szName;
                                    pEATE->dwStubRVA = EmitExportStub(pGlobalLabel->m_GlobalOffset+dwDelta);
                                    m_EATList.PUSH(pEATE);
                                }
                            }
                            else
                                report->msg("Error: Failed to get pointer to label '%s' inVTable fixup\n",pVTFEntry->m_szLabel);
                        }
                    }
                }
            }
            else
            {
                report->msg("Error: Unresolved label '%s' in VTable fixup\n",pVTFEntry->m_szLabel);
                hr = E_FAIL;
            }
            delete pVTFEntry;
        }
        if(FAILED(hr)) goto exit;
    }
    if(m_pVTable)
    {
        ULONG i, N = m_pVTable->length()/sizeof(DWORD);
        ULONG ulVTableOffset;
        m_pCeeFileGen->GetSectionDataLen (m_pILSection, &ulVTableOffset);
        // SetVTableEntry will align VTable on DWORD
        ulVTableOffset = (ulVTableOffset + (ULONG)sizeof(DWORD) - 1) & ~((ULONG)sizeof(DWORD) - 1);
        if (FAILED(hr=m_pCeeFileGen->SetVTableEntry64(m_pCeeFile, m_pVTable->length(),(void*)(m_pVTable->ptr())))) goto exit; // @WARNING: casting down from pointer-size to DWORD
        for(i = 0; i < N; i+=2)
        {
            m_pCeeFileGen->AddSectionReloc(m_pILSection,
                                            ulVTableOffset+(i*sizeof(DWORD)),
                                            m_pGlobalDataSection,
                                            srRelocAbsolute);
        }
    }
    if(m_EATList.COUNT())
    {
        if(FAILED(CreateExportDirectory())) goto exit;
        m_dwComImageFlags &= ~COMIMAGE_FLAGS_ILONLY;
        if (m_dwCeeFileFlags & ICEE_CREATE_MACHINE_I386)
            COR_SET_32BIT_REQUIRED(m_dwComImageFlags);
    }

    if (m_dwCeeFileFlags & ICEE_CREATE_MACHINE_ARM || m_fAppContainer)
    {
        // For AppContainer and ARM, you must have a minimum subsystem version of 6.02
        m_wSSVersionMajor = (m_wSSVersionMajor < 6) ? 6 : m_wSSVersionMajor;
        m_wSSVersionMinor = (m_wSSVersionMinor < 2 && m_wSSVersionMajor <= 6) ? 2 : m_wSSVersionMinor;
    }

    // Default the subsystem, instead the user doesn't set it to GUI or CUI
    if (m_dwSubsystem == (DWORD)-1)
        // The default for ILAsm previously was CUI, so that should be the default behavior...
        m_dwSubsystem = IMAGE_SUBSYSTEM_WINDOWS_CUI;

    if (FAILED(hr=m_pCeeFileGen->SetSubsystem(m_pCeeFile, m_dwSubsystem, m_wSSVersionMajor, m_wSSVersionMinor))) goto exit;

    if (FAILED(hr=m_pCeeFileGen->ClearComImageFlags(m_pCeeFile, COMIMAGE_FLAGS_ILONLY))) goto exit;
    if (FAILED(hr=m_pCeeFileGen->SetComImageFlags(m_pCeeFile, m_dwComImageFlags))) goto exit;

    if(m_dwFileAlignment)
    {
        if(FAILED(hr=m_pCeeFileGen->SetFileAlignment(m_pCeeFile, m_dwFileAlignment))) goto exit;
    }
    if(m_stBaseAddress)
    {

        if(m_dwCeeFileFlags & ICEE_CREATE_FILE_PE64)
        {
            if(FAILED(hr=m_pCeeFileGen->SetImageBase64(m_pCeeFile, m_stBaseAddress))) goto exit;
        }
        else
        {
            if(FAILED(hr=m_pCeeFileGen->SetImageBase(m_pCeeFile, (size_t)m_stBaseAddress))) goto exit;
        }
    }
    if(m_stSizeOfStackReserve || m_fAppContainer || m_fHighEntropyVA)
    {
        PIMAGE_NT_HEADERS   pNT;
        PIMAGE_SECTION_HEADER   pSect;
        ULONG   ulNumSect;
        if(FAILED(hr=m_pCeeFileGen->GetHeaderInfo(m_pCeeFile,&pNT,&pSect,&ulNumSect))) goto exit;
        if(m_dwCeeFileFlags & ICEE_CREATE_FILE_PE64)
        {
            PIMAGE_OPTIONAL_HEADER64 pOpt = (PIMAGE_OPTIONAL_HEADER64)(&pNT->OptionalHeader);
            if (m_stSizeOfStackReserve)
                pOpt->SizeOfStackReserve = VAL64(m_stSizeOfStackReserve);
            if (m_fAppContainer)
                pOpt->DllCharacteristics |= IMAGE_DLLCHARACTERISTICS_APPCONTAINER;
            if (m_fHighEntropyVA)
                pOpt->DllCharacteristics |= IMAGE_DLLCHARACTERISTICS_HIGH_ENTROPY_VA;
        }
        else
        {
            PIMAGE_OPTIONAL_HEADER32 pOpt = (PIMAGE_OPTIONAL_HEADER32)(&pNT->OptionalHeader);
            if (m_stSizeOfStackReserve)
                pOpt->SizeOfStackReserve = (DWORD)VAL32(m_stSizeOfStackReserve);
            if (m_fAppContainer)
                pOpt->DllCharacteristics |= IMAGE_DLLCHARACTERISTICS_APPCONTAINER;
            if (m_fHighEntropyVA)
                pOpt->DllCharacteristics |= IMAGE_DLLCHARACTERISTICS_HIGH_ENTROPY_VA;
        }
    }
        //Compute all the RVAs
    if (FAILED(hr=m_pCeeFileGen->LinkCeeFile(m_pCeeFile))) goto exit;

        // Fix up any fields that have RVA associated with them
    if (m_fHaveFieldsWithRvas) {
        hr = S_OK;
        ULONG dataSectionRVA;
        if (FAILED(hr=m_pCeeFileGen->GetSectionRVA(m_pGlobalDataSection, &dataSectionRVA))) goto exit;

        ULONG tlsSectionRVA;
        if (FAILED(hr=m_pCeeFileGen->GetSectionRVA(m_pTLSSection, &tlsSectionRVA))) goto exit;

        ULONG ilSectionRVA;
        if (FAILED(hr=m_pCeeFileGen->GetSectionRVA(m_pILSection, &ilSectionRVA))) goto exit;

        FieldDescriptor* pListFD;
        Class* pClass;
        for(int i=0; (pClass = m_lstClass.PEEK(i)); i++)
        {
            for(int j=0; (pListFD = pClass->m_FieldDList.PEEK(j)); j++)
            {
                if (pListFD->m_rvaLabel != 0)
                {
                    DWORD rva;
                    if(*(pListFD->m_rvaLabel)=='@')
                    {
                        rva = (DWORD)atoi(pListFD->m_rvaLabel + 1);
                    }
                    else
                    {
                        GlobalLabel *pLabel = FindGlobalLabel(pListFD->m_rvaLabel);
                        if (pLabel == 0)
                        {
                            report->msg("Error:Could not find label '%s' for the field '%s'\n", pListFD->m_rvaLabel, pListFD->m_szName);
                            hr = E_FAIL;
                            continue;
                        }

                        rva = pLabel->m_GlobalOffset;
                        if (pLabel->m_Section == m_pTLSSection)
                            rva += tlsSectionRVA;
                        else if (pLabel->m_Section == m_pILSection)
                            rva += ilSectionRVA;
                        else {
                            _ASSERTE(pLabel->m_Section == m_pGlobalDataSection);
                            rva += dataSectionRVA;
                        }
                    }
                    if (FAILED(m_pEmitter->SetFieldRVA(pListFD->m_fdFieldTok, rva))) goto exit;
                }
            }
        }
        if (FAILED(hr)) goto exit;
    }

    if(bClock) bClock->cFilegenBegin = GetTickCount();
    // actually output the meta-data
    if (FAILED(hr=m_pCeeFileGen->EmitMetaDataAt(m_pCeeFile, m_pEmitter, m_pILSection, metaDataOffset, metaData, metaDataSize))) goto exit;

    if((m_wMSVmajor < 0xFF)&&(m_wMSVminor < 0xFF))
    {
        STORAGESIGNATURE *pSSig = (STORAGESIGNATURE *)metaData;
        BYTE* pb = metaData;
        pb += 3*sizeof(DWORD)+2*sizeof(WORD)+VAL32(pSSig->iVersionString);
        pb = (BYTE*)(((size_t)pb + 3) & ~3);
        PSTORAGEHEADER pSHdr = (PSTORAGEHEADER)pb;
        PSTORAGESTREAM pStr = (PSTORAGESTREAM)(pSHdr+1);
        for(short iStr = 1; iStr <= VAL16(pSHdr->iStreams); iStr++)
        {
            if((strcmp(pStr->rcName,"#-")==0)||(strcmp(pStr->rcName,"#~")==0))
            {
                pb = metaData + VAL32(pStr->iOffset); // start of the stream header
                pb += sizeof(DWORD); // skip Reserved
                *pb = VAL16(m_wMSVmajor)&0xFF;
                *(pb+1) = VAL16(m_wMSVminor)&0xFF;
                break;
            }
            pb = (BYTE*)pStr;
            pb += 2*sizeof(DWORD)+strlen(pStr->rcName)+1;
            pb = (BYTE*)(((size_t)pb + 3) & ~3);
            pStr = (PSTORAGESTREAM)pb;
        }
    }

    if(m_fTolerateDupMethods) // means that there are /ENC files
    {
        if(m_pbsMD) delete m_pbsMD;
        m_pbsMD = new BinStr();
        memcpy(m_pbsMD->getBuff(metaDataSize),metaData,metaDataSize);
    }
    // actually output the resources
    if(mresourceSize && mresourceData)
    {
        size_t i, N = m_pManifest->m_dwMResNum, sizeread, L;
        BYTE    *ptr = (BYTE*)mresourceData;
        BOOL    mrfail = FALSE;
        FILE*   pFile = NULL;
        char sz[2048];
        for(i=0; i < N; i++)
        {
            m_pManifest->m_fMResNew[i] = FALSE;
            memset(sz,0,2048);
            WszWideCharToMultiByte(CP_ACP,0,m_pManifest->m_wzMResName[i],-1,sz,2047,NULL,NULL);
            L = m_pManifest->m_dwMResSize[i];
            sizeread = 0;
            memcpy(ptr,&L,sizeof(DWORD));
            ptr += sizeof(DWORD);
            if(fopen_s(&pFile,sz,"rb") == 0)
            {
                sizeread = fread((void *)ptr,1,L,pFile);
                fclose(pFile);
                ptr += sizeread;
            }
            else
            {
                report->msg("Error: failed to open mgd resource file '%ls'\n",m_pManifest->m_wzMResName[i]);
                mrfail = TRUE;
            }
            if(sizeread < L)
            {
                report->msg("Error: failed to read expected %d bytes from mgd resource file '%ls'\n",L,m_pManifest->m_wzMResName[i]);
                mrfail = TRUE;
                L -= sizeread;
                memset(ptr,0,L);
                ptr += L;
            }
        }
        if(mrfail)
        {
            hr = E_FAIL;
            goto exit;
        }
    }

    hr = S_OK;

exit:
    return hr;
}
#ifdef _PREFAST_
#pragma warning(pop)
#endif
