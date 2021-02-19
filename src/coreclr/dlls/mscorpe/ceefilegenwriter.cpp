// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// Derived class from CCeeGen which handles writing out
// the exe. All references to PEWriter pulled out of CCeeGen,
// and moved here
//
//

#include "stdafx.h"

#include <string.h>
#include <limits.h>

#include "corerror.h"
#include <posterror.h>
#include <shlwapi.h>

// The following block contains a template for the default entry point stubs of a COM+
// IL only program.  One can emit these stubs (with some fix-ups) and make
// the code supplied the entry point value for the image.  The fix-ups will
// in turn cause mscoree.dll to be loaded and the correct entry point to be
// called.
//
// Note: Although these stubs contain x86 specific code, they are used
// for all platforms


//*****************************************************************************
// This stub is designed for a x86 Windows application.  It will call the
// _CorExeMain function in mscoree.dll.  This entry point will in turn load
// and run the IL program.
//
//    jump _CorExeMain();
//
// The code jumps to the imported function _CorExeMain using the iat.
// The address in the template is address of the iat entry which is
// fixed up by the loader when the image is paged in.
//*****************************************************************************

const BYTE ExeMainX86Template[] =
{
	// Jump through IAT to _CorExeMain
	0xFF, 0x25,				// jmp [iat:_CorDllMain entry]
		0x00, 0x00, 0x00, 0x00,		//   address to replace

};

#define ExeMainX86TemplateSize		sizeof(ExeMainX86Template)
#define CorExeMainX86IATOffset		2

//*****************************************************************************
// This stub is designed for a x86 Windows application.  It will call the
// _CorDllMain function in mscoree.dll with with the base entry point for
// the loaded DLL.  This entry point will in turn load and run the IL program.
//
//    jump _CorDllMain
//
// The code jumps to the imported function _CorExeMain using the iat.
// The address in the template is address of the iat entry which is
// fixed up by the loader when the image is paged in.
//*****************************************************************************

const BYTE DllMainX86Template[] =
{
	// Jump through IAT to CorDllMain
	0xFF, 0x25,				// jmp [iat:_CorDllMain entry]
		0x00, 0x00, 0x00, 0x00,		//   address to replace
};

#define DllMainX86TemplateSize		sizeof(DllMainX86Template)
#define CorDllMainX86IATOffset		2

//*****************************************************************************
// This stub is designed for a AMD64 Windows application.  It will call the
// _CorExeMain function in mscoree.dll.  This entry point will in turn load
// and run the IL program.
//
//    mov rax, _CorExeMain();
//    jmp [rax]
//
// The code jumps to the imported function _CorExeMain using the iat.
// The address in the template is address of the iat entry which is
// fixed up by the loader when the image is paged in.
//*****************************************************************************

const BYTE ExeMainAMD64Template[] =
{
	// Jump through IAT to _CorExeMain
	0x48, 0xA1,				// rex.w rex.b mov rax,[following address]
    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,//address of iat:_CorExeMain entry
    0xFF, 0xE0              // jmp [rax]
};

#define ExeMainAMD64TemplateSize		sizeof(ExeMainAMD64Template)
#define CorExeMainAMD64IATOffset		2

//*****************************************************************************
// This stub is designed for a AMD64 Windows application.  It will call the
// _CorDllMain function in mscoree.dll with with the base entry point for
// the loaded DLL.  This entry point will in turn load and run the IL program.
//
//    mov rax, _CorDllMain();
//    jmp [rax]
//
// The code jumps to the imported function _CorDllMain using the iat.
// The address in the template is address of the iat entry which is
// fixed up by the loader when the image is paged in.
//*****************************************************************************

const BYTE DllMainAMD64Template[] =
{
	// Jump through IAT to CorDllMain
	0x48, 0xA1,				// rex.w rex.b mov rax,[following address]
    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,//address of iat:_CorDllMain entry
    0xFF, 0xE0              // jmp [rax]
};

#define DllMainAMD64TemplateSize		sizeof(DllMainAMD64Template)
#define CorDllMainAMD64IATOffset		2

//*****************************************************************************
// This stub is designed for an ia64 Windows application.  It will call the
// _CorExeMain function in mscoree.dll.  This entry point will in turn load
// and run the IL program.
//
//    jump _CorExeMain();
//
// The code jumps to the imported function _CorExeMain using the iat.
// We set the value of gp to point at the iat table entry for _CorExeMain
//*****************************************************************************

const BYTE ExeMainIA64Template[] =
{
    // ld8    r9  = [gp]    ;;
    // ld8    r10 = [r9],8
    // nop.i                ;;
    // ld8    gp  = [r9]
    // mov    b6  = r10
    // br.cond.sptk.few  b6
    //
    0x0B, 0x48, 0x00, 0x02, 0x18, 0x10, 0xA0, 0x40,
    0x24, 0x30, 0x28, 0x00, 0x00, 0x00, 0x04, 0x00,
    0x10, 0x08, 0x00, 0x12, 0x18, 0x10, 0x60, 0x50,
    0x04, 0x80, 0x03, 0x00, 0x60, 0x00, 0x80, 0x00
};

#define ExeMainIA64TemplateSize		sizeof(ExeMainIA64Template)

//*****************************************************************************
// This stub is designed for an ia64 Windows application.  It will call the
// _CorDllMain function in mscoree.dll with with the base entry point for
// the loaded DLL.  This entry point will in turn load and run the IL program.
//
//    jump _CorDllMain
//
// The code jumps to the imported function _CorExeMain using the iat.
// We set the value of gp to point at the iat table entry for _CorExeMain
//*****************************************************************************

const BYTE DllMainIA64Template[] =
{
    // ld8    r9  = [gp]    ;;
    // ld8    r10 = [r9],8
    // nop.i                ;;
    // ld8    gp  = [r9]
    // mov    b6  = r10
    // br.cond.sptk.few  b6
    //
    0x0B, 0x48, 0x00, 0x02, 0x18, 0x10, 0xA0, 0x40,
    0x24, 0x30, 0x28, 0x00, 0x00, 0x00, 0x04, 0x00,
    0x10, 0x08, 0x00, 0x12, 0x18, 0x10, 0x60, 0x50,
    0x04, 0x80, 0x03, 0x00, 0x60, 0x00, 0x80, 0x00
};

#define DllMainIA64TemplateSize		sizeof(DllMainIA64Template)

#ifdef EMIT_FIXUPS

// Emitted PEFIXUP structure looks like this
struct PEFIXUP
{
   WORD  wType;
   WORD  wSpare;
   DWORD rva;
   DWORD rvaTarget;
};

// Following structure is used to store the reloc information which
// will be used at UpdateFixups time to get the final data from the section
// bytes to update the fixup information.
//
struct DBG_FIXUP
{
   WORD  wType;
   WORD  wSpare;

   union
   {
      DWORD rva;
      unsigned offset;
   };

   union
   {
      DWORD rvaTarget;
      CeeSection * sectionSource;
   };
};

enum
{
   IMAGE_REL_I386_DIR24NB          = 0x0081,   // 24-bit base relative
   IMAGE_REL_I386_FILEPOS          = 0x0082,   // 32-bit file relative
                                               // all other relocation types are
                                               // in winnt.h, for some reason
                                               // this one is missing
   IMAGE_REL_I386_DIR30NB          = 0x0083,   // 30-bit base relative
};

#endif // EMIT_FIXUPS

// Get the Symbol entry given the head and a 0-based index
inline IMAGE_SYMBOL* GetSymbolEntry(IMAGE_SYMBOL* pHead, SIZE_T idx)
{
    return (IMAGE_SYMBOL*) (((BYTE*) pHead) + IMAGE_SIZEOF_SYMBOL * idx);
}

//*****************************************************************************
// To get a new instance, call CreateNewInstance() or CreateNewInstanceEx() instead of new
//*****************************************************************************

HRESULT CeeFileGenWriter::CreateNewInstance(CCeeGen *pCeeFileGenFrom,
                                            CeeFileGenWriter* & pGenWriter,
                                            DWORD createFlags)
{
    return CreateNewInstanceEx(pCeeFileGenFrom, pGenWriter, createFlags);
}

//
// Seed file is used as the base file. The new file data will be "appended" to the seed file
//

HRESULT CeeFileGenWriter::CreateNewInstanceEx(CCeeGen *pCeeFileGenFrom,
                                              CeeFileGenWriter* & pGenWriter,
                                              DWORD createFlags,
                                              LPCWSTR seedFileName)
{
    HRESULT hr = S_OK;
    ULONG preallocatedOffset = 0;
    NewHolder<PEWriter> pPEWriter(NULL);
    NewHolder<CeeFileGenWriter> pPrivateGenWriter;
    CeeSection *corHeaderSection = NULL;

    pPrivateGenWriter = new (nothrow) CeeFileGenWriter;
    if (pPrivateGenWriter == NULL)
        IfFailGo(E_OUTOFMEMORY);

    pPEWriter = new (nothrow) PEWriter;
    if (pPEWriter == NULL)
        IfFailGo(E_OUTOFMEMORY);

    //workaround
    //What's really the correct thing to be doing here?
    //HRESULT hr = pPEWriter->Init(pCeeFileGenFrom ? pCeeFileGenFrom->getPESectionMan() : NULL);
    hr = pPEWriter->Init(NULL, createFlags, seedFileName);
    IfFailGo(hr);

    //Create the general PEWriter.
    pPrivateGenWriter->m_peSectionMan = pPEWriter;
    hr = pPrivateGenWriter->Init(); // base class member to finish init
    IfFailGo(hr);

    if (!seedFileName) // Use base file's preferred base (if present)
    {
        if (pPEWriter->isPE32())
        {
            pPrivateGenWriter->setImageBase((DWORD) CEE_IMAGE_BASE_32);   // use same default as linker
        }
        else
        {
            pPrivateGenWriter->setImageBase64((ULONGLONG) CEE_IMAGE_BASE_64); // use same default as linker
        }
    }

    pPrivateGenWriter->setSubsystem(IMAGE_SUBSYSTEM_WINDOWS_CUI, CEE_IMAGE_SUBSYSTEM_MAJOR_VERSION, CEE_IMAGE_SUBSYSTEM_MINOR_VERSION);

    if (pPEWriter->createCorMainStub())
    {
        hr = pPrivateGenWriter->allocateIAT(); // so the IAT goes out first
        IfFailGo(hr);
    }

    hr = pPrivateGenWriter->allocateCorHeader();   // get COR header near front
    IfFailGo(hr);

#if 0 // Need to add this if we want to propagate the old COM+ header
    if (seedFileName)
    {
        memcpy(m_corHeader, baseFileDecoder->ntHeaders32()->corHeader, sizeof(IMAGE_COR20_HEADER));
    }
#endif

    //If we were passed a CCeeGen at the beginning, copy it's data now.
    if (pCeeFileGenFrom) {
        pCeeFileGenFrom->cloneInstance((CCeeGen*)pPrivateGenWriter);
    }

    hr = pPrivateGenWriter->getSectionCreate(".text0", sdExecute, &corHeaderSection);
    IfFailGo(hr);
    preallocatedOffset = corHeaderSection->dataLen();


    // set il RVA to be after the preallocated sections
    pPEWriter->setIlRva(preallocatedOffset);

#ifdef EMIT_FIXUPS
    if (createFlags & ICEE_CREATE_FILE_EMIT_FIXUPS)
    {
        pPrivateGenWriter->setEmitFixups();
    }
#endif

    pPEWriter.SuppressRelease();
    pPrivateGenWriter.SuppressRelease();
    pGenWriter = pPrivateGenWriter;

ErrExit:
    return hr;
} // HRESULT CeeFileGenWriter::CreateNewInstance()

CeeFileGenWriter::CeeFileGenWriter() // ctor is protected
{
    m_outputFileName = NULL;
    m_resourceFileName = NULL;
    m_dllSwitch = false;
    m_objSwitch = false;
    m_libraryName = NULL;
    m_libraryGuid = GUID_NULL;

    m_entryPoint = 0;
    m_comImageFlags = COMIMAGE_FLAGS_ILONLY;    // ceegen PEs don't have native code
    m_iatOffset = 0;
    m_dllCount = 0;

    m_dwMacroDefinitionSize = 0;
    m_dwMacroDefinitionRVA = NULL;

    m_dwManifestSize = 0;
    m_dwManifestRVA = NULL;

    m_dwStrongNameSize = 0;
    m_dwStrongNameRVA = NULL;

    m_dwVTableSize = 0;
    m_dwVTableRVA = NULL;

    m_iDataDlls = NULL;

    m_linked = false;
    m_fixed = false;

#ifdef EMIT_FIXUPS

    m_fEmitFixups = false;
    m_fFixupsUpdated = false;
    m_sectionFixups = NULL;
    m_pDebugDir = NULL;

#endif

} // CeeFileGenWriter::CeeFileGenWriter()

//*****************************************************************************
// Cleanup
//*****************************************************************************
HRESULT CeeFileGenWriter::Cleanup() // virtual
{
    ((PEWriter *)m_peSectionMan)->Cleanup();  // call derived cleanup
    delete m_peSectionMan;
    m_peSectionMan = NULL; // so base class won't delete

    delete[] m_outputFileName;
    delete[] m_resourceFileName;

    if (m_iDataDlls) {
        for (int i=0; i < m_dllCount; i++) {
            if (m_iDataDlls[i].m_methodName)
                delete[] m_iDataDlls[i].m_methodName;
        }
        delete[] m_iDataDlls;
    }

    return CCeeGen::Cleanup();
} // HRESULT CeeFileGenWriter::Cleanup()

HRESULT CeeFileGenWriter::EmitMacroDefinitions(void *pData, DWORD cData)
{
    // OBSOLETE
    m_dwMacroDefinitionSize = 0;

    return S_OK;
} // HRESULT CeeFileGenWriter::EmitMacroDefinitions()

HRESULT CeeFileGenWriter::link()
{
    HRESULT hr = checkForErrors();
    if (! SUCCEEDED(hr))
        return hr;

#ifdef EMIT_FIXUPS

    // The fixups describe each relocation.  Each fixup contains the relocation's
    // type, source RVA, and target RVA.  Since the reloc target can be filled
    // in after the relocation creation, the fixup target RVA discovery needs to
    // be deferred.
    // At this point all bytes should be filled in, ensuring that the final
    // target information is available.
    // UpdateFixups is called at this point to discover the final relocation target info.
    //
    hr = UpdateFixups();
    if (! SUCCEEDED(hr))
        return hr;

#endif

    // Don't set this if SetManifestEntry was not called - zapper sets the
    // resource directory explicitly
    if (m_dwManifestSize != 0)
    {
        m_corHeader->Resources.VirtualAddress = VAL32(m_dwManifestRVA);
        m_corHeader->Resources.Size = VAL32(m_dwManifestSize);
    }

    if (m_dwStrongNameSize != 0)
    {
        m_corHeader->StrongNameSignature.VirtualAddress = VAL32(m_dwStrongNameRVA);
        m_corHeader->StrongNameSignature.Size = VAL32(m_dwStrongNameSize);
    }

    if (m_dwVTableSize != 0)
    {
        m_corHeader->VTableFixups.VirtualAddress = VAL32(m_dwVTableRVA);
        m_corHeader->VTableFixups.Size = VAL32(m_dwVTableSize);
    }

    unsigned characteristicsMask = IMAGE_FILE_EXECUTABLE_IMAGE;

    if (getPEWriter().isPE32())
        characteristicsMask |= IMAGE_FILE_32BIT_MACHINE;
    if (!getPEWriter().isPE32())
        characteristicsMask |= IMAGE_FILE_LARGE_ADDRESS_AWARE;

    getPEWriter().setCharacteristics(characteristicsMask);

    m_corHeader->cb = VAL32(sizeof(IMAGE_COR20_HEADER));
    m_corHeader->MajorRuntimeVersion = VAL16(COR_VERSION_MAJOR);
    m_corHeader->MinorRuntimeVersion = VAL16(COR_VERSION_MINOR);
    if (m_dllSwitch)
        getPEWriter().setCharacteristics(IMAGE_FILE_DLL);
    if (m_objSwitch)
        getPEWriter().clearCharacteristics(IMAGE_FILE_DLL | IMAGE_FILE_EXECUTABLE_IMAGE);
    m_corHeader->Flags = VAL32(m_comImageFlags);
    IMAGE_COR20_HEADER_FIELD(*m_corHeader, EntryPointToken) = VAL32(m_entryPoint);
    _ASSERTE(TypeFromToken(m_entryPoint) == mdtMethodDef || m_entryPoint == mdTokenNil ||
             TypeFromToken(m_entryPoint) == mdtFile);
    setDirectoryEntry(getCorHeaderSection(), IMAGE_DIRECTORY_ENTRY_COMHEADER, sizeof(IMAGE_COR20_HEADER), m_corHeaderOffset);

    if ((m_comImageFlags & COMIMAGE_FLAGS_IL_LIBRARY) == 0
        && !m_linked && !m_objSwitch)
    {
        hr = emitExeMain();
        if (FAILED(hr))
            return hr;
#ifndef TARGET_UNIX
        hr = emitResourceSection();
        if (FAILED(hr))
            return hr;
#endif
    }

    m_linked = true;

    IfFailRet(getPEWriter().link());

    return S_OK;
} // HRESULT CeeFileGenWriter::link()


HRESULT CeeFileGenWriter::fixup()
{
    HRESULT hr;

    m_fixed = true;

    if (!m_linked)
        IfFailRet(link());

    CeeGenTokenMapper *pMapper = getTokenMapper();

    // Apply token remaps if there are any.
    if (! m_fTokenMapSupported && pMapper != NULL) {
        IMetaDataImport *pImport;
        hr = pMapper->GetMetaData(&pImport);
        _ASSERTE(SUCCEEDED(hr));
        hr = MapTokens(pMapper, pImport);
        pImport->Release();

    }

    // remap the entry point if entry point token has been moved
    if (pMapper != NULL && !m_objSwitch)
    {
        mdToken tk = m_entryPoint;
        pMapper->HasTokenMoved(tk, tk);
        IMAGE_COR20_HEADER_FIELD(*m_corHeader, EntryPointToken) = VAL32(tk);
    }

    IfFailRet(getPEWriter().fixup(pMapper));

    return S_OK;
} // HRESULT CeeFileGenWriter::fixup()

HRESULT CeeFileGenWriter::generateImage(void **ppImage)
{
    HRESULT hr = S_OK;
    LPCWSTR outputFileName = NULL;

#ifndef TARGET_UNIX
    HANDLE hThreadToken = NULL;
    // Impersonation is only supported on Win2k and above.
    if (!OpenThreadToken(GetCurrentThread(), TOKEN_READ | TOKEN_IMPERSONATE, TRUE, &hThreadToken))
    {
        if (GetLastError() != ERROR_NO_TOKEN)
        {
            _ASSERTE(!"Failed to get thread token!");
            return HRESULT_FROM_GetLastError();
        }
    }

    if (hThreadToken != NULL)
    {
        if (!RevertToSelf())
        {
            _ASSERTE(!"Failed to revert impersonation!");
            CloseHandle(hThreadToken);
            return HRESULT_FROM_GetLastError();
        }
    }
#endif // !TARGET_UNIX

    if (!m_fixed)
        IfFailGo(fixup());

    outputFileName = m_outputFileName;

    if (! outputFileName && ppImage == NULL) {
        if (m_comImageFlags & COMIMAGE_FLAGS_IL_LIBRARY)
            outputFileName = W("output.ill");
        else if (m_dllSwitch)
            outputFileName = W("output.dll");
        else if (m_objSwitch)
            outputFileName = W("output.exe");
        else
            outputFileName = W("output.obj");
    }

    // output file name and ppImage are mutually exclusive
    _ASSERTE((NULL == outputFileName && ppImage != NULL) || (outputFileName != NULL && NULL == ppImage));

    if (outputFileName != NULL)
        IfFailGo(getPEWriter().write(outputFileName));
    else
        IfFailGo(getPEWriter().write(ppImage));

ErrExit:
#ifndef TARGET_UNIX
    if (hThreadToken != NULL)
    {
        BOOL success = SetThreadToken(NULL, hThreadToken);
        CloseHandle(hThreadToken);

        if (!success)
        {
            _ASSERTE(!"Failed to reimpersonate!");
            hr = HRESULT_FROM_GetLastError();
        }
    }
#endif // !TARGET_UNIX
    return hr;
} // HRESULT CeeFileGenWriter::generateImage()

HRESULT CeeFileGenWriter::setOutputFileName(__in LPWSTR fileName)
{
    if (m_outputFileName)
        delete[] m_outputFileName;
    size_t len = wcslen(fileName) + 1;
    m_outputFileName = (LPWSTR)new (nothrow) WCHAR[len];
    TESTANDRETURN(m_outputFileName!=NULL, E_OUTOFMEMORY);
    wcscpy_s(m_outputFileName, len, fileName);
    return S_OK;
} // HRESULT CeeFileGenWriter::setOutputFileName()

HRESULT CeeFileGenWriter::setResourceFileName(__in LPWSTR fileName)
{
    if (m_resourceFileName)
        delete[] m_resourceFileName;
    size_t len = wcslen(fileName) + 1;
    m_resourceFileName = (LPWSTR)new (nothrow) WCHAR[len];
    TESTANDRETURN(m_resourceFileName!=NULL, E_OUTOFMEMORY);
    wcscpy_s(m_resourceFileName, len, fileName);
    return S_OK;
} // HRESULT CeeFileGenWriter::setResourceFileName()

HRESULT CeeFileGenWriter::setLibraryName(__in LPWSTR libraryName)
{
    if (m_libraryName)
        delete[] m_libraryName;
    size_t len = wcslen(libraryName) + 1;
    m_libraryName = (LPWSTR)new (nothrow) WCHAR[len];
    TESTANDRETURN(m_libraryName != NULL, E_OUTOFMEMORY);
    wcscpy_s(m_libraryName, len, libraryName);
    return S_OK;
} // HRESULT CeeFileGenWriter::setLibraryName()

HRESULT CeeFileGenWriter::setLibraryGuid(__in LPWSTR libraryGuid)
{
    return IIDFromString(libraryGuid, &m_libraryGuid);
} // HRESULT CeeFileGenWriter::setLibraryGuid()

HRESULT CeeFileGenWriter::emitLibraryName(IMetaDataEmit *emitter)
{
    HRESULT hr;
    IfFailRet(emitter->SetModuleProps(m_libraryName));

    // Set the GUID as a custom attribute, if it is not NULL_GUID.
    if (m_libraryGuid != GUID_NULL)
    {
        static COR_SIGNATURE _SIG[] = INTEROP_GUID_SIG;
        mdTypeRef tr;
        mdMemberRef mr;
        WCHAR wzGuid[40];
        BYTE  rgCA[50];
        IfFailRet(emitter->DefineTypeRefByName(mdTypeRefNil, INTEROP_GUID_TYPE_W, &tr));
        IfFailRet(emitter->DefineMemberRef(tr, W(".ctor"), _SIG, sizeof(_SIG), &mr));
        StringFromGUID2(m_libraryGuid, wzGuid, lengthof(wzGuid));
        memset(rgCA, 0, sizeof(rgCA));
        // Tag is 0x0001
        rgCA[0] = 1;
        // Length of GUID string is 36 characters.
        rgCA[2] = 0x24;
        // Convert 36 characters, skipping opening {, into 3rd byte of buffer.
        WszWideCharToMultiByte(CP_ACP,0, wzGuid+1,36, reinterpret_cast<char*>(&rgCA[3]),36, 0,0);
        hr = emitter->DefineCustomAttribute(1,mr,rgCA,41,0);
    }
    return (hr);
} // HRESULT CeeFileGenWriter::emitLibraryName()

HRESULT CeeFileGenWriter::setImageBase(size_t imageBase)
{
    _ASSERTE(getPEWriter().isPE32());
        getPEWriter().setImageBase32((DWORD)imageBase);
    return S_OK;
} // HRESULT CeeFileGenWriter::setImageBase()

HRESULT CeeFileGenWriter::setImageBase64(ULONGLONG imageBase)
{
    _ASSERTE(!getPEWriter().isPE32());
    getPEWriter().setImageBase64(imageBase);
    return S_OK;
} // HRESULT CeeFileGenWriter::setImageBase64()

HRESULT CeeFileGenWriter::setFileAlignment(ULONG fileAlignment)
{
    getPEWriter().setFileAlignment(fileAlignment);
    return S_OK;
} // HRESULT CeeFileGenWriter::setFileAlignment()

HRESULT CeeFileGenWriter::setSubsystem(DWORD subsystem, DWORD major, DWORD minor)
{
    getPEWriter().setSubsystem(subsystem, major, minor);
    return S_OK;
} // HRESULT CeeFileGenWriter::setSubsystem()

HRESULT CeeFileGenWriter::checkForErrors()
{
    if (TypeFromToken(m_entryPoint) == mdtMethodDef) {
        if (m_dllSwitch) {
            //current spec would need to check the binary sig of the entry point method
        }
        return S_OK;
    }
    return S_OK;
} // HRESULT CeeFileGenWriter::checkForErrors()

HRESULT CeeFileGenWriter::getMethodRVA(ULONG codeOffset, ULONG *codeRVA)
{
    _ASSERTE(codeRVA);
    *codeRVA = getPEWriter().getIlRva() + codeOffset;
    return S_OK;
} // HRESULT CeeFileGenWriter::getMethodRVA()

HRESULT CeeFileGenWriter::setDirectoryEntry(CeeSection &section, ULONG entry, ULONG size, ULONG offset)
{
    return getPEWriter().setDirectoryEntry((PEWriterSection*)(&section.getImpl()), entry, size, offset);
} // HRESULT CeeFileGenWriter::setDirectoryEntry()

HRESULT CeeFileGenWriter::getFileTimeStamp(DWORD *pTimeStamp)
{
    return getPEWriter().getFileTimeStamp(pTimeStamp);
} // HRESULT CeeFileGenWriter::getFileTimeStamp()

HRESULT CeeFileGenWriter::setAddrReloc(UCHAR *instrAddr, DWORD value)
{
    *(DWORD *)instrAddr = VAL32(value);
    return S_OK;
} // HRESULT CeeFileGenWriter::setAddrReloc()

HRESULT CeeFileGenWriter::addAddrReloc(CeeSection &thisSection, UCHAR *instrAddr, DWORD offset, CeeSection *targetSection)
{
    if (!targetSection) {
        thisSection.addBaseReloc(offset, srRelocHighLow);
    } else {
        thisSection.addSectReloc(offset, *targetSection, srRelocHighLow);
    }
    return S_OK;
} // HRESULT CeeFileGenWriter::addAddrReloc()

// create CorExeMain and import directory into .text and the .iat into .data
//
// The structure of the import directory information is as follows, but it is not contiguous in
// section. All the r/o data goes into the .text section and the iat array (which the loader
// updates with the imported addresses) goes into the .data section because WINCE needs it to be writable.
//
//    struct IData {
//      // one for each DLL, terminating in NULL
//      IMAGE_IMPORT_DESCRIPTOR iid[];
//      // import lookup table: a set of entries for the methods of each DLL,
//      // terminating each set with NULL
//      IMAGE_THUNK_DATA32/64 ilt[];
//      // hint/name table: an set of entries for each method of each DLL wiht
//      // no terminating entry
//      struct {
//          WORD Hint;
//          // null terminated string
//          BYTE Name[];
//      } ibn;      // Hint/name table
//      // import address table: a set of entries for the methods of each DLL,
//      // terminating each set with NULL
//      IMAGE_THUNK_DATA32/64 iat[];
//      // one for each DLL, null terminated strings
//      BYTE DllName[];
//  };
//

// IAT must be first in its section, so have code here to allocate it up front
// prior to knowing other info such as if dll or not. This won't work if have > 1
// function imported, but we'll burn that bridge when we get to it.
HRESULT CeeFileGenWriter::allocateIAT()
{
    m_dllCount = 1;
    m_iDataDlls = new (nothrow) IDataDllInfo[m_dllCount];
    if (m_iDataDlls == NULL) {
        return E_OUTOFMEMORY;
    }
    memset(m_iDataDlls, '\0', m_dllCount * sizeof(IDataDllInfo));
    m_iDataDlls[0].m_name = "mscoree.dll";
    m_iDataDlls[0].m_numMethods = 1;
    m_iDataDlls[0].m_methodName = new (nothrow) const char*[m_iDataDlls[0].m_numMethods];
    if (! m_iDataDlls[0].m_methodName) {
        return E_OUTOFMEMORY;
    }
    m_iDataDlls[0].m_methodName[0] = NULL;

    int iDataSizeIAT = 0;

    for (int i=0; i < m_dllCount; i++) {
        m_iDataDlls[i].m_iatOffset = iDataSizeIAT;
        iDataSizeIAT += (m_iDataDlls[i].m_numMethods + 1)
                      * (getPEWriter().isPE32() ? sizeof(IMAGE_THUNK_DATA32)
                                                : sizeof(IMAGE_THUNK_DATA64));
    }

    HRESULT hr = getSectionCreate(".text0", sdExecute, &m_iDataSectionIAT);
    TESTANDRETURNHR(hr);
    m_iDataOffsetIAT = m_iDataSectionIAT->dataLen();
    _ASSERTE(m_iDataOffsetIAT == 0);
    m_iDataIAT = m_iDataSectionIAT->getBlock(iDataSizeIAT);
    if (! m_iDataIAT) {
        return E_OUTOFMEMORY;
    }
    memset(m_iDataIAT, '\0', iDataSizeIAT);

    // Don't set the IAT directory entry yet, since we may not actually end up doing
    // an emitExeMain.

    return S_OK;
} // HRESULT CeeFileGenWriter::allocateIAT()

HRESULT CeeFileGenWriter::emitExeMain()
{
    if (m_dllCount == 0)
        return S_OK;

    // Note: code later on in this method assumes that mscoree.dll is at
    // index m_iDataDlls[0], with CorDllMain or CorExeMain at method[0]

    _ASSERTE(getPEWriter().createCorMainStub());

    if (m_dllSwitch) {
        m_iDataDlls[0].m_methodName[0] = "_CorDllMain";
    } else {
        m_iDataDlls[0].m_methodName[0] = "_CorExeMain";
    }

    // IMAGE_IMPORT_DESCRIPTOR on PE/PE+ must be 4-byte or 8-byte aligned
    int align     = (getPEWriter().isPE32()) ? 4 : 8;
    int curOffset = getTextSection().dataLen();

    int diff = ((curOffset + align -1) & ~(align-1)) - curOffset;
    if (diff)
    {
        char* pDiff = getTextSection().getBlock(diff);
        if (NULL==pDiff) return E_OUTOFMEMORY;
        memset(pDiff,0,diff);
    }

    int iDataSizeRO = (m_dllCount + 1) * sizeof(IMAGE_IMPORT_DESCRIPTOR);
    CeeSection &iDataSectionRO = getTextSection();
    int iDataOffsetRO = iDataSectionRO.dataLen();
    int iDataSizeIAT = 0;
    int i;
    for (i=0; i < m_dllCount; i++) {
        m_iDataDlls[i].m_iltOffset = iDataSizeRO + iDataSizeIAT;
        iDataSizeIAT += (m_iDataDlls[i].m_numMethods + 1)
                      * (getPEWriter().isPE32() ? sizeof(IMAGE_THUNK_DATA32)
                                                : sizeof(IMAGE_THUNK_DATA64));
    }

    iDataSizeRO += iDataSizeIAT;

    for (i=0; i < m_dllCount; i++) {
        int delta = (iDataSizeRO + iDataOffsetRO) % 16;
        // make sure is on a 16-byte offset
        if (delta != 0)
            iDataSizeRO += (16 - delta);
        _ASSERTE((iDataSizeRO + iDataOffsetRO) % 16 == 0);
        m_iDataDlls[i].m_ibnOffset = iDataSizeRO;
        for (int j=0; j < m_iDataDlls[i].m_numMethods; j++) {
            int nameLen = (int)(strlen(m_iDataDlls[i].m_methodName[j]) + 1);
            iDataSizeRO += sizeof(WORD) + nameLen + nameLen%2;
        }
    }
    for (i=0; i < m_dllCount; i++) {
        m_iDataDlls[i].m_nameOffset = iDataSizeRO;
        iDataSizeRO += (int)(strlen(m_iDataDlls[i].m_name) + 2);
    }

    char *iDataRO = iDataSectionRO.getBlock(iDataSizeRO);

    if (!iDataRO) return E_OUTOFMEMORY;

    memset(iDataRO, '\0', iDataSizeRO);

    setDirectoryEntry(iDataSectionRO, IMAGE_DIRECTORY_ENTRY_IMPORT, iDataSizeRO, iDataOffsetRO);

    IMAGE_IMPORT_DESCRIPTOR *iid = (IMAGE_IMPORT_DESCRIPTOR *)iDataRO;
    for (i=0; i < m_dllCount; i++) {

        // fill in the import descriptors for each DLL
        IMAGE_IMPORT_DESC_FIELD(iid[i], OriginalFirstThunk) = VAL32((ULONG)(m_iDataDlls[i].m_iltOffset + iDataOffsetRO));
        iid[i].Name = VAL32(m_iDataDlls[i].m_nameOffset + iDataOffsetRO);
        iid[i].FirstThunk = VAL32((ULONG)(m_iDataDlls[i].m_iatOffset + m_iDataOffsetIAT));

        iDataSectionRO.addSectReloc(
            (unsigned)(iDataOffsetRO + (char *)(&IMAGE_IMPORT_DESC_FIELD(iid[i], OriginalFirstThunk)) - iDataRO), iDataSectionRO, srRelocAbsolute);
        iDataSectionRO.addSectReloc(
            (unsigned)(iDataOffsetRO + (char *)(&iid[i].Name) - iDataRO), iDataSectionRO, srRelocAbsolute);
        iDataSectionRO.addSectReloc(
            (unsigned)(iDataOffsetRO + (char *)(&iid[i].FirstThunk) - iDataRO), *m_iDataSectionIAT, srRelocAbsolute);

        if (getPEWriter().isPE32())
        {
        // now fill in the import lookup table for each DLL
            IMAGE_THUNK_DATA32 *ilt = (IMAGE_THUNK_DATA32*) (iDataRO + m_iDataDlls[i].m_iltOffset);
            IMAGE_THUNK_DATA32 *iat = (IMAGE_THUNK_DATA32*) (m_iDataIAT + m_iDataDlls[i].m_iatOffset);

            int ibnOffset = m_iDataDlls[i].m_ibnOffset;
            for (int j=0; j < m_iDataDlls[i].m_numMethods; j++)
            {
                ilt[j].u1.AddressOfData = VAL32((ULONG)(ibnOffset + iDataOffsetRO));
                iat[j].u1.AddressOfData = VAL32((ULONG)(ibnOffset + iDataOffsetRO));

                iDataSectionRO.addSectReloc( (unsigned)(iDataOffsetRO + (char *)(&ilt[j].u1.AddressOfData) - iDataRO),
                                                iDataSectionRO, srRelocAbsolute);
                m_iDataSectionIAT->addSectReloc( (unsigned)(m_iDataOffsetIAT + (char *)(&iat[j].u1.AddressOfData) - m_iDataIAT),
                                                    iDataSectionRO, srRelocAbsolute);
                int nameLen = (int)(strlen(m_iDataDlls[i].m_methodName[j]) + 1);
                memcpy(iDataRO + ibnOffset + offsetof(IMAGE_IMPORT_BY_NAME, Name),
                                        m_iDataDlls[i].m_methodName[j], nameLen);
                ibnOffset += sizeof(WORD) + nameLen + nameLen%2;
            }
        }
        else
        {
            // now fill in the import lookup table for each DLL
            IMAGE_THUNK_DATA64 *ilt = (IMAGE_THUNK_DATA64*) (iDataRO + m_iDataDlls[i].m_iltOffset);
            IMAGE_THUNK_DATA64 *iat = (IMAGE_THUNK_DATA64*) (m_iDataIAT + m_iDataDlls[i].m_iatOffset);

            int ibnOffset = m_iDataDlls[i].m_ibnOffset;
            for (int j=0; j < m_iDataDlls[i].m_numMethods; j++)
            {
                ilt[j].u1.AddressOfData = VAL64((ULONG)(ibnOffset + iDataOffsetRO));
                iat[j].u1.AddressOfData = VAL64((ULONG)(ibnOffset + iDataOffsetRO));

                iDataSectionRO.addSectReloc( (unsigned)(iDataOffsetRO + (char *)(&ilt[j].u1.AddressOfData) - iDataRO),
                                             iDataSectionRO, srRelocAbsolute);
                m_iDataSectionIAT->addSectReloc( (unsigned)(m_iDataOffsetIAT + (char *)(&iat[j].u1.AddressOfData) - m_iDataIAT),
                                                 iDataSectionRO, srRelocAbsolute);
                int nameLen = (int)(strlen(m_iDataDlls[i].m_methodName[j]) + 1);
                memcpy(iDataRO + ibnOffset + offsetof(IMAGE_IMPORT_BY_NAME, Name),
                       m_iDataDlls[i].m_methodName[j], nameLen);
                ibnOffset += sizeof(WORD) + nameLen + nameLen%2;
            }
        }

        // now fill in the import lookup table for each DLL
        strcpy_s(iDataRO + m_iDataDlls[i].m_nameOffset,
                 iDataSizeRO - m_iDataDlls[i].m_nameOffset,
                 m_iDataDlls[i].m_name);

    } // end of for loop i < m_dllCount


    if (getPEWriter().isI386())
    {
        // Put the entry point code into the PE file
        unsigned entryPointOffset = getTextSection().dataLen();
        int iatOffset = (int) (entryPointOffset + (m_dllSwitch ? CorDllMainX86IATOffset : CorExeMainX86IATOffset));
        align = 4;    // x86 fixups must be 4-byte aligned

        // The IAT offset must be aligned because fixup is applied to it.
        diff = ((iatOffset + align -1) & ~(align-1)) - iatOffset;
        if (diff)
        {
            char* pDiff = getTextSection().getBlock(diff);
            if(NULL==pDiff) return E_OUTOFMEMORY;
            memset(pDiff,0,diff);
            entryPointOffset += diff;
        }
        _ASSERTE((getTextSection().dataLen() + (m_dllSwitch ? CorDllMainX86IATOffset : CorExeMainX86IATOffset)) % align == 0);

        getPEWriter().setEntryPointTextOffset(entryPointOffset);
        if (m_dllSwitch)
        {
            UCHAR *dllMainBuf = (UCHAR*)getTextSection().getBlock(sizeof(DllMainX86Template));
            if(dllMainBuf==NULL) return E_OUTOFMEMORY;
            memcpy(dllMainBuf, DllMainX86Template, sizeof(DllMainX86Template));
            //mscoree.dll
            setAddrReloc(dllMainBuf+CorDllMainX86IATOffset, m_iDataDlls[0].m_iatOffset + m_iDataOffsetIAT);
            addAddrReloc(getTextSection(), dllMainBuf, entryPointOffset+CorDllMainX86IATOffset, m_iDataSectionIAT);
        }
        else
        {
            UCHAR *exeMainBuf = (UCHAR*)getTextSection().getBlock(sizeof(ExeMainX86Template));
            if(exeMainBuf==NULL) return E_OUTOFMEMORY;
            memcpy(exeMainBuf, ExeMainX86Template, sizeof(ExeMainX86Template));
            //mscoree.dll
            setAddrReloc(exeMainBuf+CorExeMainX86IATOffset, m_iDataDlls[0].m_iatOffset + m_iDataOffsetIAT);
            addAddrReloc(getTextSection(), exeMainBuf, entryPointOffset+CorExeMainX86IATOffset, m_iDataSectionIAT);
        }
    }
    else if (getPEWriter().isAMD64())
    {
        // Put the entry point code into the PE file
        unsigned entryPointOffset = getTextSection().dataLen();
        int iatOffset = (int) (entryPointOffset + (m_dllSwitch ? CorDllMainAMD64IATOffset : CorExeMainAMD64IATOffset));
        align = 16;    // AMD64 fixups must be 8-byte aligned

        // The IAT offset must be aligned because fixup is applied to it.
        diff = ((iatOffset + align -1) & ~(align-1)) - iatOffset;
        if (diff)
        {
            char* pDiff = getTextSection().getBlock(diff);
            if(NULL==pDiff) return E_OUTOFMEMORY;
            memset(pDiff,0,diff);
            entryPointOffset += diff;
        }
        _ASSERTE((getTextSection().dataLen() + (m_dllSwitch ? CorDllMainAMD64IATOffset : CorExeMainAMD64IATOffset)) % align == 0);

        getPEWriter().setEntryPointTextOffset(entryPointOffset);
        if (m_dllSwitch)
        {
            UCHAR *dllMainBuf = (UCHAR*)getTextSection().getBlock(sizeof(DllMainAMD64Template));
            if(dllMainBuf==NULL) return E_OUTOFMEMORY;
            memcpy(dllMainBuf, DllMainAMD64Template, sizeof(DllMainAMD64Template));
            //mscoree.dll
            setAddrReloc(dllMainBuf+CorDllMainAMD64IATOffset, m_iDataDlls[0].m_iatOffset + m_iDataOffsetIAT);
            addAddrReloc(getTextSection(), dllMainBuf, entryPointOffset+CorDllMainAMD64IATOffset, m_iDataSectionIAT);
        }
        else
        {
            UCHAR *exeMainBuf = (UCHAR*)getTextSection().getBlock(sizeof(ExeMainAMD64Template));
            if(exeMainBuf==NULL) return E_OUTOFMEMORY;
            memcpy(exeMainBuf, ExeMainAMD64Template, sizeof(ExeMainAMD64Template));
            //mscoree.dll
            setAddrReloc(exeMainBuf+CorExeMainAMD64IATOffset, m_iDataDlls[0].m_iatOffset + m_iDataOffsetIAT);
            addAddrReloc(getTextSection(), exeMainBuf, entryPointOffset+CorExeMainAMD64IATOffset, m_iDataSectionIAT);
        }
    }
    else if (getPEWriter().isIA64())
    {
        // Must have a PE+ PE64 file
        //_ASSERTE(!getPEWriter().isPE32());

        // Put the entry point code into the PE+ file
        curOffset = getTextSection().dataLen();
        align = 16;       // instructions on ia64 must be 16-byte aligned

        // The entry point address be aligned
        diff = ((curOffset + align -1) & ~(align-1)) - curOffset;
        if (diff)
        {
            char* pDiff = getTextSection().getBlock(diff);
            if(NULL==pDiff) return E_OUTOFMEMORY;
            memset(pDiff,0,diff);
        }

        unsigned entryPointOffset = getTextSection().dataLen();

        if (m_dllSwitch)
        {
            UCHAR *dllMainBuf = (UCHAR*)getTextSection().getBlock(sizeof(DllMainIA64Template));
            if (dllMainBuf==NULL) return E_OUTOFMEMORY;
            memcpy(dllMainBuf, DllMainIA64Template, sizeof(DllMainIA64Template));
        }
        else
        {
            UCHAR *exeMainBuf = (UCHAR*)getTextSection().getBlock(sizeof(ExeMainIA64Template));
            if (exeMainBuf==NULL) return E_OUTOFMEMORY;
            memcpy(exeMainBuf, ExeMainIA64Template, sizeof(ExeMainIA64Template));
        }

        // Put the entry point function pointer into the PE file
        unsigned entryPlabelOffset = getTextSection().dataLen();
        getPEWriter().setEntryPointTextOffset(entryPlabelOffset);

        UCHAR * entryPtr = (UCHAR*)getTextSection().getBlock(sizeof(ULONGLONG));
        UCHAR * gpPtr    = (UCHAR*)getTextSection().getBlock(sizeof(ULONGLONG));

        memset(entryPtr,0,sizeof(ULONGLONG));
        memset(gpPtr,0,sizeof(ULONGLONG));

        setAddrReloc(entryPtr, entryPointOffset);
        addAddrReloc(getTextSection(), entryPtr, entryPlabelOffset, &getTextSection());

        setAddrReloc(gpPtr, m_iDataDlls[0].m_iatOffset + m_iDataOffsetIAT);
        addAddrReloc(getTextSection(), gpPtr, entryPlabelOffset+8, m_iDataSectionIAT);
    }
    else
    {
        _ASSERTE(!"Unknown target machine");
    }

    // Now set our IAT entry since we're using the IAT
    setDirectoryEntry(*m_iDataSectionIAT, IMAGE_DIRECTORY_ENTRY_IAT, iDataSizeIAT, m_iDataOffsetIAT);

    return S_OK;
} // HRESULT CeeFileGenWriter::emitExeMain()

#ifndef TARGET_UNIX

// This function reads a resource file and emits it into the generated PE file.
// 1. We can only link resources in obj format. Must convert from .res to .obj
// with CvtRes.exe. See https://github.com/dotnet/runtime/issues/11412.
// 2. Must touch up all COFF relocs from .rsrc$01 (resource header) to .rsrc$02
// (resource raw data)
HRESULT CeeFileGenWriter::emitResourceSection()
{
    if (m_resourceFileName == NULL)
        return S_OK;

    const WCHAR* szResFileName = m_resourceFileName;

    // read the resource file and spit it out in the .rsrc section

    HANDLE hFile = INVALID_HANDLE_VALUE;
    HANDLE hMap = NULL;
    IMAGE_FILE_HEADER *hMod = NULL;

    HRESULT hr = S_OK;

    struct Param
    {
        HANDLE hFile;
        HANDLE hMap;
        IMAGE_FILE_HEADER *hMod;
        const WCHAR* szResFileName;
        CeeFileGenWriter *genWriter;
        HRESULT hr;
    } param;

    param.hFile = hFile;
    param.hMap = hMap;
    param.hMod = hMod;
    param.szResFileName = szResFileName;
    param.genWriter = this;
    param.hr = S_OK;

    PAL_TRY(Param *, pParam, &param)
    {
        SIZE_T cbFileSize;
        const BYTE *pbStartOfMappedMem;
        IMAGE_SECTION_HEADER *rsrc[2] = { NULL, NULL };
        S_SIZE_T cbTotalSizeOfRawData;

        char *data = NULL;
        SIZE_T cReloc = 0;
        IMAGE_RELOCATION *pReloc = NULL;
        SIZE_T cSymbol = 0;
        IMAGE_SYMBOL *pSymbolTable = NULL;

        // create a mapped view of the .res file
        pParam->hFile = WszCreateFile(pParam->szResFileName, GENERIC_READ, FILE_SHARE_READ, NULL, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, NULL);
        if (pParam->hFile == INVALID_HANDLE_VALUE)
        {
            //dbprintf("Resource file %S not found\n", szResFileName);
            pParam->hr = HRESULT_FROM_WIN32(ERROR_RESOURCE_DATA_NOT_FOUND);
            goto lDone;
        }

        // Grab the file size for verification checks.
        {
            DWORD dwFileSizeHigh;
            DWORD dwFileSize = SafeGetFileSize(pParam->hFile, &dwFileSizeHigh);
            if (dwFileSize == (DWORD)(-1))
            {
                pParam->hr = HRESULT_FROM_GetLastError();
                goto lDone;
            }

            // Since we intend to memory map this file, the size of the file can not need 64 bits to represent!
            if (dwFileSizeHigh != 0)
            {
                pParam->hr = HRESULT_FROM_WIN32(ERROR_RESOURCE_DATA_NOT_FOUND);
                goto lDone;
            }

            cbFileSize = static_cast<SIZE_T>(dwFileSize);
        }

        pParam->hMap = WszCreateFileMapping(pParam->hFile, 0, PAGE_READONLY, 0, 0, NULL);

        if (pParam->hMap == NULL)
        {
            //dbprintf("Invalid .res file: %S\n", szResFileName);
            pParam->hr = HRESULT_FROM_GetLastError();
            goto lDone;
        }

        pbStartOfMappedMem = reinterpret_cast<const BYTE *>(MapViewOfFile(pParam->hMap, FILE_MAP_READ, 0, 0, 0));

        // test failure conditions
        if (pbStartOfMappedMem == NULL)
        {
            //dbprintf("Invalid .res file: %S:Can't get header\n", szResFileName);
            pParam->hr = HRESULT_FROM_GetLastError();
            goto lDone;
        }

        // Check that the file contains an IMAGE_FILE_HEADER structure.
        if (IMAGE_SIZEOF_FILE_HEADER > cbFileSize)
        {
            pParam->hr = HRESULT_FROM_WIN32(ERROR_RESOURCE_DATA_NOT_FOUND);
            goto lDone;
        }

        pParam->hMod = (IMAGE_FILE_HEADER*)pbStartOfMappedMem;

        if (VAL16(pParam->hMod->SizeOfOptionalHeader) != 0)
        {
            //dbprintf("Invalid .res file: %S:Illegal optional header\n", szResFileName);
            pParam->hr = HRESULT_FROM_WIN32(ERROR_RESOURCE_DATA_NOT_FOUND); // GetLastError() = 0 since API worked.
            goto lDone;
        }

        // Scan all section headers and grab .rsrc$01 and .rsrc$02
        {
            // First section is directly after header
            SIZE_T cSections = static_cast<SIZE_T>(VAL16(pParam->hMod->NumberOfSections));
            SIZE_T cbStartOfSections = IMAGE_SIZEOF_FILE_HEADER;
            S_SIZE_T cbEndOfSections(S_SIZE_T(cbStartOfSections) +
                                     (S_SIZE_T(cSections) * S_SIZE_T(IMAGE_SIZEOF_SECTION_HEADER)));

            // Check that all sections are within the bounds of the mapped file.
            if (cbEndOfSections.IsOverflow() ||
                cbEndOfSections.Value() > cbFileSize)
            {
                pParam->hr = HRESULT_FROM_WIN32(ERROR_RESOURCE_DATA_NOT_FOUND);
                goto lDone;
            }

            {
                IMAGE_SECTION_HEADER *pSection =
                    (IMAGE_SECTION_HEADER *)(pbStartOfMappedMem + cbStartOfSections);
                IMAGE_SECTION_HEADER *pSectionEnd = pSection + cSections;

                for (; pSection < pSectionEnd; pSection++)
                {
                    if (strcmp(".rsrc$01", (char *)pSection->Name) == 0)
                    {
                        rsrc[0] = pSection;
                    }
                    else if (strcmp(".rsrc$02", (char *)pSection->Name) == 0)
                    {
                        rsrc[1] = pSection;
                    }
                }
            }
        }

        // If we don't have both resources, fail.
        if (!rsrc[0] || !rsrc[1])
        {
            //dbprintf("Invalid .res file: %S: Missing sections .rsrc$01 or .rsrc$02\n", szResFileName);
            pParam->hr = HRESULT_FROM_WIN32(ERROR_RESOURCE_DATA_NOT_FOUND);
            goto lDone;
        }

        // Verify the resource data starts and sizes
        {
            cbTotalSizeOfRawData = S_SIZE_T(0);

            for (int i = 0; i < 2; i++)
            {
                S_SIZE_T cbStartOfResourceData(static_cast<SIZE_T>(VAL32(rsrc[i]->PointerToRawData)));
                S_SIZE_T cbSizeOfResourceData(static_cast<SIZE_T>(VAL32(rsrc[i]->SizeOfRawData)));
                S_SIZE_T cbEndOfResourceData(cbStartOfResourceData + cbSizeOfResourceData);

                if (cbEndOfResourceData.IsOverflow() ||
                    cbEndOfResourceData.Value() > cbFileSize)
                {
                    pParam->hr = HRESULT_FROM_WIN32(ERROR_RESOURCE_DATA_NOT_FOUND);
                    goto lDone;
                }

                cbTotalSizeOfRawData += cbSizeOfResourceData;
            }

            // Check that the total raw data doesn't overflow.
            if (cbTotalSizeOfRawData.IsOverflow() ||
                cbTotalSizeOfRawData.Value() > cbFileSize)
            {
                pParam->hr = HRESULT_FROM_WIN32(ERROR_RESOURCE_DATA_NOT_FOUND);
                goto lDone;
            }
        }

        PESection *rsrcSection;
        pParam->hr = pParam->genWriter->getPEWriter().getSectionCreate(".rsrc", sdReadOnly, &rsrcSection);
        if (FAILED(pParam->hr)) goto lDone;

        rsrcSection->directoryEntry(IMAGE_DIRECTORY_ENTRY_RESOURCE);
        data = rsrcSection->getBlock(static_cast<unsigned>(cbTotalSizeOfRawData.Value()), 8);
        if(data == NULL)
        {
            pParam->hr = E_OUTOFMEMORY;
            goto lDone;
        }

        // Copy resource header
        memcpy(data, (char *)pParam->hMod + VAL32(rsrc[0]->PointerToRawData), VAL32(rsrc[0]->SizeOfRawData));

        // Map all the relocs in .rsrc$01 using the reloc and symbol tables in the COFF object.,
        cReloc        = 0;         // Total number of relocs
        pReloc        = NULL;      // Reloc table start

        cSymbol       = 0;         // Total number of symbols
        pSymbolTable  = NULL;      // Symbol table start

        {
            // Check that the relocations and symbols lie within the resource
            cReloc = VAL16(rsrc[0]->NumberOfRelocations);
            SIZE_T cbStartOfRelocations = static_cast<SIZE_T>(VAL32(rsrc[0]->PointerToRelocations));
            S_SIZE_T cbEndOfRelocations(S_SIZE_T(cbStartOfRelocations) +
                                        (S_SIZE_T(cReloc) * S_SIZE_T(sizeof(IMAGE_RELOCATION))));


            // Verify the number of symbols fit into the resource.
            cSymbol = static_cast<SIZE_T>(VAL32(pParam->hMod->NumberOfSymbols));
            SIZE_T cbStartOfSymbolTable = static_cast<SIZE_T>(VAL32(pParam->hMod->PointerToSymbolTable));
            S_SIZE_T cbEndOfSymbolTable(S_SIZE_T(cbStartOfSymbolTable) +
                                        (S_SIZE_T(cSymbol) * S_SIZE_T(IMAGE_SIZEOF_SYMBOL)));

            if (cbEndOfRelocations.IsOverflow() ||
                cbEndOfRelocations.Value() > cbFileSize ||
                cbEndOfSymbolTable.IsOverflow() ||
                cbEndOfSymbolTable.Value() > cbFileSize)
            {
                pParam->hr = HRESULT_FROM_WIN32(ERROR_RESOURCE_DATA_NOT_FOUND);
                goto lDone;
            }

            pReloc = (IMAGE_RELOCATION *)(pbStartOfMappedMem + cbStartOfRelocations);
            pSymbolTable = (IMAGE_SYMBOL *)(pbStartOfMappedMem + cbStartOfSymbolTable);
        }

        _ASSERTE(pReloc != NULL && pSymbolTable != NULL);

        for(SIZE_T iReloc = 0; iReloc < cReloc; iReloc++, pReloc++)
        {
            // Ensure this is a valid reloc
            {
                S_SIZE_T cbRelocEnd = S_SIZE_T(VAL32(pReloc->VirtualAddress)) + S_SIZE_T(sizeof(DWORD));
                if (cbRelocEnd.IsOverflow() ||
                    cbRelocEnd.Value() > static_cast<SIZE_T>(VAL32(rsrc[0]->SizeOfRawData)))
                {
                    pParam->hr = HRESULT_FROM_WIN32(ERROR_RESOURCE_DATA_NOT_FOUND);
                    goto lDone;
                }
            }

            // index into symbol table, provides address into $02
            DWORD iSymbol = VAL32(pReloc->SymbolTableIndex);

            // Make sure the index is in range
            if (iSymbol >= cSymbol)
            {
                pParam->hr = HRESULT_FROM_WIN32(ERROR_RESOURCE_DATA_NOT_FOUND);
                goto lDone;
            }

            IMAGE_SYMBOL* pSymbolEntry = GetSymbolEntry(pSymbolTable, iSymbol);

            // Ensure the symbol entry is valid for a resource.
            if ((pSymbolEntry->StorageClass != IMAGE_SYM_CLASS_STATIC) ||
                (VAL16(pSymbolEntry->Type) != IMAGE_SYM_TYPE_NULL) ||
                (VAL16(pSymbolEntry->SectionNumber) != 3)) // 3rd section is .rsrc$02
            {
                //dbprintf("Invalid .res file: %S:Illegal symbol entry\n", szResFileName);
                pParam->hr = HRESULT_FROM_WIN32(ERROR_RESOURCE_DATA_NOT_FOUND);
                goto lDone;
            }

            // Ensure that RVA is valid address (inside rsrc[1])
            if (VAL32(pSymbolEntry->Value) >= VAL32(rsrc[1]->SizeOfRawData))
            {
                //dbprintf("Invalid .res file: %S:Illegal rva into .rsrc$02\n", szResFileName);
                pParam->hr = HRESULT_FROM_WIN32(ERROR_RESOURCE_DATA_NOT_FOUND);
                goto lDone;
            }

            DWORD dwOffsetInRsrc2 = VAL32(pSymbolEntry->Value) + VAL32(rsrc[0]->SizeOfRawData);

            // Create reloc
            *(DWORD*)(data + VAL32(pReloc->VirtualAddress)) = VAL32(dwOffsetInRsrc2);
            rsrcSection->addSectReloc(pReloc->VirtualAddress, rsrcSection, srRelocAbsolute);
        }

        // Copy $02 (resource raw) data
        memcpy(data+VAL32(rsrc[0]->SizeOfRawData),
            (char *)pParam->hMod + VAL32(rsrc[1]->PointerToRawData),
            VAL32(rsrc[1]->SizeOfRawData));

lDone: ;
    }
    PAL_EXCEPT(EXCEPTION_EXECUTE_HANDLER)
    {
        //dbprintf("Exception occured manipulating .res file %S\n", szResFileName);
        param.hr = HRESULT_FROM_WIN32(ERROR_RESOURCE_DATA_NOT_FOUND);
    }
    PAL_ENDTRY

    hMod = param.hMod;
    hFile = param.hFile;
    szResFileName = param.szResFileName;
    hr = param.hr;

    if (hMod != NULL)
        UnmapViewOfFile(hMod);
    if (hMap != NULL)
        CloseHandle(hMap);
    if (hFile != INVALID_HANDLE_VALUE)
        CloseHandle(hFile);

    return hr;
} // HRESULT CeeFileGenWriter::emitResourceSection()
#endif // !TARGET_UNIX

HRESULT CeeFileGenWriter::setManifestEntry(ULONG size, ULONG offset)
{
    if (offset)
        m_dwManifestRVA = offset;
    else {
        CeeSection TextSection = getTextSection();
        getMethodRVA(TextSection.dataLen() - size, &m_dwManifestRVA);
    }

    m_dwManifestSize = size;
    return S_OK;
} // HRESULT CeeFileGenWriter::setManifestEntry()

HRESULT CeeFileGenWriter::setStrongNameEntry(ULONG size, ULONG offset)
{
    m_dwStrongNameRVA = offset;
    m_dwStrongNameSize = size;
    return S_OK;
} // HRESULT CeeFileGenWriter::setStrongNameEntry()

HRESULT CeeFileGenWriter::setVTableEntry64(ULONG size, void* ptr)
{
    if (ptr && size)
    {
        void * pv;
        CeeSection TextSection = getTextSection();
        // make it DWORD-aligned
        ULONG L = TextSection.dataLen();
        if((L &= ((ULONG)sizeof(DWORD)-1)))
        {
            L = (ULONG)sizeof(DWORD) - L;
            if((pv = TextSection.getBlock(L)))
                memset(pv,0,L);
            else
                return E_OUTOFMEMORY;
        }
        getMethodRVA(TextSection.dataLen(), &m_dwVTableRVA);
        if((pv = TextSection.getBlock(size)))
        {
            memcpy(pv,ptr,size);
        }
        else
            return E_OUTOFMEMORY;
        m_dwVTableSize = size;
    }

    return S_OK;
} // HRESULT CeeFileGenWriter::setVTableEntry()

HRESULT CeeFileGenWriter::setVTableEntry(ULONG size, ULONG offset)
{
    return setVTableEntry64(size,(void*)(ULONG_PTR)offset);
} // HRESULT CeeFileGenWriter::setVTableEntry()

HRESULT CeeFileGenWriter::setEnCRvaBase(ULONG dataBase, ULONG rdataBase)
{
    setEnCMode();
    getPEWriter().setEnCRvaBase(dataBase, rdataBase);
    return S_OK;
} // HRESULT CeeFileGenWriter::setEnCRvaBase()

HRESULT CeeFileGenWriter::computeSectionOffset(CeeSection &section, __in char *ptr,
                                               unsigned *offset)
{
    *offset = section.computeOffset(ptr);

    return S_OK;
} // HRESULT CeeFileGenWriter::computeSectionOffset()

HRESULT CeeFileGenWriter::computeOffset(__in char *ptr,
                                        CeeSection **pSection, unsigned *offset)
{
    TESTANDRETURNPOINTER(pSection);

    CeeSection **s = m_sections;
    CeeSection **sEnd = s + m_numSections;
    while (s < sEnd)
    {
        if ((*s)->containsPointer(ptr))
        {
            *pSection = *s;
            *offset = (*s)->computeOffset(ptr);

            return S_OK;
        }
        s++;
    }

    return E_FAIL;
} // HRESULT CeeFileGenWriter::computeOffset()

HRESULT CeeFileGenWriter::getCorHeader(IMAGE_COR20_HEADER **ppHeader)
{
    *ppHeader = m_corHeader;
    return S_OK;
} // HRESULT CeeFileGenWriter::getCorHeader()


#ifdef EMIT_FIXUPS

HRESULT CeeFileGenWriter::InitFixupSection()
{
    if (!m_fEmitFixups)
    {
       return(E_UNEXPECTED);
    }

    HRESULT hr;

    hr = getSectionCreate(".fixups",
                           IMAGE_SCN_CNT_INITIALIZED_DATA  | IMAGE_SCN_MEM_READ,
                           &m_sectionFixups);
    if (SUCCEEDED(hr))
    {
       size_t cbDebugDir = sizeof(IMAGE_DEBUG_DIRECTORY);
       hr = GetSectionBlock(m_sectionFixups, (ULONG) cbDebugDir, 32, (void **) &m_pDebugDir);
       if (SUCCEEDED(hr))
       {
          memset(m_pDebugDir, 0, cbDebugDir);
          m_pDebugDir->Type = IMAGE_DEBUG_TYPE_FIXUP;
          m_fFixupsUpdated = false;

          return(S_OK);
       }
    }

    m_pDebugDir = NULL;
    m_sectionFixups = NULL;
    m_fEmitFixups = false;

    return(E_FAIL);

} // HRESULT CeeFileGenWriter::InitFixupSection()

HRESULT CeeFileGenWriter::addFixup(CeeSection& sectionSource, unsigned offset, CeeSectionRelocType relocType, CeeSection * psectionTarget, CeeSectionRelocExtra *extra)
{
   if (!m_fEmitFixups)
   {
      return(S_OK);
   }

   _ASSERTE(sizeof(DBG_FIXUP) == sizeof(PEFIXUP));
   _ASSERTE(m_fFixupsUpdated == false);

   DBG_FIXUP * pfixup;

   if (m_sectionFixups == NULL)
   {
      HRESULT hr = InitFixupSection();
      if (FAILED(hr))
      {
         return(hr);
      }

      // The fixup section begins with a IMAGE_DEBUG_DIRECTORY containing a
      // IMAGE_DEBUG_TYPE_FIXUP directory entry, which describes the array
      // of fixups which follows it.

      // The very first item of this array is aligned on a 32 bit boundary.
      // All other fixup entries follow unaligned.
      pfixup = (DBG_FIXUP *) m_sectionFixups->getBlock(sizeof(DBG_FIXUP), 32);
      TESTANDRETURN(pfixup != NULL, E_OUTOFMEMORY);

      // Initialize the IMAGE_DEBUG_TYPE_FIXUP entry relocations
#ifdef HOST_64BIT
      _ASSERTE(!"Base relocs are not yet implemented for 64-bit");
      m_pDebugDir->AddressOfRawData = 0; // @ToDo: srRelocAbsolutePtr can't take a 64-bit address
#else
      m_pDebugDir->AddressOfRawData = (size_t) pfixup;
      m_sectionFixups->addSectReloc(offsetof(IMAGE_DEBUG_DIRECTORY, AddressOfRawData), *m_sectionFixups, srRelocAbsolutePtr);
#endif

      m_pDebugDir->PointerToRawData = m_sectionFixups->computeOffset((char *) pfixup);

      m_sectionFixups->addSectReloc(offsetof(IMAGE_DEBUG_DIRECTORY, PointerToRawData), *m_sectionFixups, srRelocFilePos);

      unsigned offsetDir = m_sectionFixups->computeOffset((char *) m_pDebugDir);
      setDirectoryEntry(*m_sectionFixups, IMAGE_DIRECTORY_ENTRY_DEBUG, sizeof(IMAGE_DEBUG_DIRECTORY), offsetDir);

#ifdef TEST_EMIT_FIXUPS
      TestEmitFixups();
#endif
   }
   else
   {
      pfixup = (DBG_FIXUP *) m_sectionFixups->getBlock(sizeof(DBG_FIXUP), 1);
      TESTANDRETURN(pfixup != NULL, E_OUTOFMEMORY);
   }

   // Save off the relocation information for use later.  The relocation's
   // target information can be filled in later.
   // The relocation target info is not always immediately available, so it needs
   // to be extracted later, during the link phase.  For now the relocation info
   // is stored so the target can be extracted at link time in the UpdateFixups
   // function.
   //
   unsigned offsetFixup = m_sectionFixups->computeOffset((char *) pfixup);
   pfixup->wSpare = 0;
   pfixup->wType = relocType;
   _ASSERTE(pfixup->wType == relocType);
   pfixup->offset = offset;
   pfixup->sectionSource = &sectionSource;

   m_pDebugDir->SizeOfData += sizeof(DBG_FIXUP);

   // Add a relocation for the fixup's source RVA field, (no fixup on this reloc)
   m_sectionFixups->addSectReloc(offsetFixup + offsetof(DBG_FIXUP, rva), sectionSource, srRelocAbsolutePtr);

   // Add a relocation for the fixup's target RVA field.  Correct target extracted
   // later in UpdateFixups, (no fixup on this reloc)
   CeeSectionRelocType tgtRelocType;

   switch (relocType)
   {
      case srRelocMapToken:
         // not an RVA
         tgtRelocType = srRelocMapToken;
         break;

      case srRelocFilePos:
         tgtRelocType = srRelocFilePos;
         break;

      case srRelocHighAdj:
         tgtRelocType = srRelocHighAdj;
         break;

      default:
         tgtRelocType = (relocType & srRelocPtr) ? srRelocAbsolutePtr : srRelocAbsolute;
         break;
   }

   if (psectionTarget != NULL)
   {
      m_sectionFixups->addSectReloc(offsetFixup + offsetof(DBG_FIXUP, rvaTarget), *psectionTarget, tgtRelocType, extra);
   }
   else
   {
      m_sectionFixups->addBaseReloc(offsetFixup + offsetof(DBG_FIXUP, rvaTarget), tgtRelocType, extra);
   }

   return(S_OK);
} // HRESULT CeeFileGenWriter::addFixup()

HRESULT CeeFileGenWriter::UpdateFixups()
{
   // This method extracts the correct relocation target.  See addFixup method.

   if (!m_fEmitFixups || m_fFixupsUpdated)
   {
      return(S_OK);
   }
   m_fFixupsUpdated = true; // prevent UpdateFixups from being called again.

   size_t cfixups = m_pDebugDir->SizeOfData / sizeof(DBG_FIXUP);
   _ASSERT(m_pDebugDir->SizeOfData % sizeof(DBG_FIXUP) == 0);
   unsigned ibFixup = m_pDebugDir->PointerToRawData;

   for (size_t idx = 0; idx < cfixups; idx++, ibFixup += sizeof(DBG_FIXUP))
   {
      DBG_FIXUP * pfixup = (DBG_FIXUP *) m_sectionFixups->computePointer(ibFixup);
      CeeSection * sectionSource = pfixup->sectionSource;
      CeeSectionRelocType relocType = (CeeSectionRelocType) pfixup->wType;
      unsigned offset = pfixup->offset;

      // Get current data for replacing fixup contents
      const DWORD * pdw = (DWORD *) sectionSource->computePointer(offset);
      pfixup->rva = (DWORD) (UINT_PTR) pdw;
      pfixup->rvaTarget = *pdw;

      switch (relocType)
      {
#ifdef HOST_X86
      case srRelocAbsolute:
          // Emitted bytes: RVA, offset relative to image base
          // reloc src contains target offset relative to target section
          if ((*pdw & 0xFF000000) == 0)
          {
              pfixup->wType = IMAGE_REL_I386_DIR32NB;
          }
          else
          {
              // MethodDesc::Fixup function creates a 24 bit RVA, where the
              // high byte of the DWORD stores the flag value: METHOD_NEEDS_PRESTUB_RUN_FLAG.
              // work around it by converting the type to 24 bits here
              pfixup->wType = IMAGE_REL_I386_DIR24NB;
              pfixup->rvaTarget = *pdw & 0x00FFFFFF;
          }
          break;

      case srRelocAbsolutePtr:
          // Emitted bytes: RVA, offset relative to image base
          // reloc src contains target pointer
          pfixup->wType = IMAGE_REL_I386_DIR32NB;
          break;

      case srRelocHighLow:
          // Emitted bytes: full address of target
          // reloc src contains target offset relative to target section
          pfixup->wType = IMAGE_REL_I386_DIR32;
          break;

      case srRelocHighLowPtr:
          // Emitted bytes: full address of target
          // reloc src contains target pointer
          pfixup->wType = IMAGE_REL_I386_DIR32;
          break;

      case srRelocRelative:
          // Emitted bytes: value of reloc tgt - (reloc source + sizeof(DWORD))
          // reloc src contains offset relative to target section, minus sizeof(DWORD)
          // the reloc type for pFixup->rvaTarget is srRelocAbsolute
          // so contents of pFixup->rvaTarget need to be offset Target + sizeof(DWORD)
          // which is offset Target == Source contents + sizeof(DWORD) == *pdw + sizeof(DWORD)
          pfixup->wType = IMAGE_REL_I386_REL32;
          pfixup->rvaTarget = *pdw + sizeof(DWORD);
          break;

      case srRelocRelativePtr:
          // Emitted bytes: value of reloc tgt - (reloc source + sizeof(DWORD))
          // reloc src contains disp, disp = pTarget - (pSource + sizeof(DWORD))
          // the reloc type for pFixup->rvaTarget is srRelocAbsolutePtr
          // so contents of pFixup->rvaTarget need to be pTarget
          // which is pTarget == pSource + sizeof(DWORD) + disp == pdw + 4 + *pdw
          pfixup->wType = IMAGE_REL_I386_REL32;
          pfixup->rvaTarget = (int) (INT_PTR) pdw + sizeof(DWORD) + (int) *pdw;
          break;

      case srRelocMapToken:
          // Emitted bytes: contents of reloc source unchanged.
          // reloc src contains token value
          pfixup->wType = IMAGE_REL_I386_TOKEN;
          break;

#elif defined(HOST_AMD64)
          /*
          //
          // X86-64 relocations
          //
          IMAGE_REL_AMD64_ABSOLUTE        0x0000  // Reference is absolute, no relocation is necessary
          IMAGE_REL_AMD64_ADDR64          0x0001  // 64-bit address (VA).
          IMAGE_REL_AMD64_ADDR32          0x0002  // 32-bit address (VA).
          IMAGE_REL_AMD64_ADDR32NB        0x0003  // 32-bit address w/o image base (RVA).
          IMAGE_REL_AMD64_REL32           0x0004  // 32-bit relative address from byte following reloc
          IMAGE_REL_AMD64_REL32_1         0x0005  // 32-bit relative address from byte distance 1 from reloc
          IMAGE_REL_AMD64_REL32_2         0x0006  // 32-bit relative address from byte distance 2 from reloc
          IMAGE_REL_AMD64_REL32_3         0x0007  // 32-bit relative address from byte distance 3 from reloc
          IMAGE_REL_AMD64_REL32_4         0x0008  // 32-bit relative address from byte distance 4 from reloc
          IMAGE_REL_AMD64_REL32_5         0x0009  // 32-bit relative address from byte distance 5 from reloc
          IMAGE_REL_AMD64_SECTION         0x000A  // Section index
          IMAGE_REL_AMD64_SECREL          0x000B  // 32 bit offset from base of section containing target
          IMAGE_REL_AMD64_SECREL7         0x000C  // 7 bit unsigned offset from base of section containing target
          IMAGE_REL_AMD64_TOKEN           0x000D  // 32 bit metadata token
          IMAGE_REL_AMD64_SREL32          0x000E  // 32 bit signed span-dependent value emitted into object
          IMAGE_REL_AMD64_PAIR            0x000F
          IMAGE_REL_AMD64_SSPAN32         0x0010  // 32 bit signed span-dependent value applied at link time
          */
      case srRelocAbsolute:
          // Emitted bytes: RVA, offset relative to image base
          pfixup->wType = IMAGE_REL_AMD64_ADDR32NB;
          break;

      case srRelocAbsolutePtr:
          // Emitted bytes: RVA, offset relative to image base
          // reloc src contains target pointer
          pfixup->wType = IMAGE_REL_AMD64_ADDR32NB;
          break;

      case srRelocDir64Ptr:
          // Emitted bytes: full address of target
          // reloc src contains target pointer
          pfixup->wType = IMAGE_REL_IA64_DIR64;
          break;

      case srRelocMapToken:
          // Emitted bytes: contents of reloc source unchanged.
          // reloc src contains token value
          pfixup->wType = IMAGE_REL_AMD64_TOKEN;
          break;
#endif
      case srRelocFilePos:
          // Emitted bytes: offset relative to start of file, differs from RVA.
          pfixup->wType = IMAGE_REL_I386_FILEPOS;
          break;

      case srRelocAbsoluteTagged:
            pfixup->wType = IMAGE_REL_I386_DIR30NB;
            pfixup->rvaTarget = (*pdw & ~0x80000001) >> 1;
          break;

      case srRelocHighAdj:
          // Emitted bytes: 2 part relocation, with high part adjusted by constant.
          pfixup->wType = IMAGE_REL_BASED_HIGHADJ;
          break;

      default:
          _ASSERTE(!"Unknown relocation type");
          return(E_UNEXPECTED);
          break;
      }
   }

   return(S_OK);

} // HRESULT CeeFileGenWriter::UpdateFixups()


HRESULT CeeFileGenWriter::setEmitFixups()
{
   m_fEmitFixups = true;
   return(S_OK);

} // HRESULT CeeFileGenWriter::setEmitFixups()

#ifdef TEST_EMIT_FIXUPS

HRESULT CeeFileGenWriter::TestEmitFixups()
{
   HRESULT hr;
   // Test fixups

   CeeSection * testSection;
   hr = getSectionCreate(".test",
                          IMAGE_SCN_CNT_INITIALIZED_DATA  | IMAGE_SCN_MEM_READ,
                          &testSection);
   if (SUCCEEDED(hr))
   {
      struct FixupEntry
      {
         char sz[18];
         DWORD wTargets[8];
      };

      struct FixupTypes
      {
         char *               pszType;
         CeeSectionRelocType  relocType;
      };

      FixupTypes rgTypes[] =
      {
         { "srRelocAbsolute   ", srRelocAbsolute      },
         { "srRelocAbsolutePtr", srRelocAbsolutePtr   },
         { "srRelocHighLow    ", srRelocHighLow       },
         { "srRelocHighLowPtr ", srRelocHighLowPtr    },
      // { "srRelocRelative   ", srRelocRelative      },
      // { "srRelocRelativePtr", srRelocRelativePtr   },
         { "srRelocMapToken   ", srRelocMapToken      },
      // { "srRelocFilePos    ", srRelocFilePos       },
      // { "srRelocHighAdj    ", srRelocHighAdj       },
      };

      const size_t cFixups = sizeof(rgTypes) / sizeof(rgTypes[0]);

      DWORD * pdwTargets[20];

      // Target Blocks:

      for (size_t idx = 0; idx < cFixups; idx++)
      {
         hr = GetSectionBlock(testSection, sizeof(DWORD), 1, (void **) &pdwTargets[idx]);
         _ASSERTE(SUCCEEDED(hr));

         DWORD * pdw = pdwTargets[idx];
         *pdw = idx;
      }

      for (size_t idxType = 0; idxType < cFixups; idxType++)
      {
         // Fixup Entries
         FixupEntry * pEntry;
         hr = GetSectionBlock(testSection, sizeof(FixupEntry), 1, (void **) &pEntry);
         _ASSERTE(SUCCEEDED(hr));

         memset(pEntry, 0, sizeof(FixupEntry));
         strcpy_s(pEntry->sz, sizeof(pEntry->sz), rgTypes[idxType].pszType);

         size_t ibBlock = testSection->computeOffset((char *) pEntry);

         for (size_t idx = 0; idx < cFixups; idx++)
         {
            size_t ibFixup = ((size_t) &pEntry->wTargets[idx]) - (size_t) pEntry;

            switch (rgTypes[idxType].relocType)
            {
               case srRelocAbsolute:
                 pEntry->wTargets[idx] = idx * sizeof(DWORD);
                 break;

               case srRelocAbsolutePtr:
                 pEntry->wTargets[idx] = (DWORD) pdwTargets[idx];
                 break;

               case srRelocHighLow:
                 pEntry->wTargets[idx] = idx * sizeof(DWORD);
                 break;

               case srRelocHighLowPtr:
                 pEntry->wTargets[idx] = (DWORD) pdwTargets[idx];
                 break;

               case srRelocRelative:
                 pEntry->wTargets[idx] = idx;
                 break;

               case srRelocRelativePtr:
               {
                 size_t ibTgt = (size_t) pdwTargets[idx];
                 size_t ibSrc = ((size_t) &pEntry->wTargets[idx]) + sizeof(DWORD);
                 pEntry->wTargets[idx] = (DWORD)( ibTgt - ibSrc );
                 ibFixup += sizeof(DWORD); // offset needs to point at end of DWORD
                 break;
               }

               case srRelocHighAdj:
                 pEntry->wTargets[idx] = idx * sizeof(DWORD);
                 break;

               case srRelocMapToken:
                 pEntry->wTargets[idx] = idx * sizeof(DWORD);
                 break;

               case srRelocFilePos:
                 pEntry->wTargets[idx] = idx * sizeof(DWORD);
                 break;
            }

            addFixup(*testSection, ibBlock + ibFixup, rgTypes[idxType].relocType, testSection);
            testSection->addSectReloc(ibBlock + ibFixup, *testSection, rgTypes[idxType].relocType);
         }
      }
   }

   return(S_OK);
}
#endif // TEST_EMIT_FIXUPS
#endif // EMIT_FIXUPS
