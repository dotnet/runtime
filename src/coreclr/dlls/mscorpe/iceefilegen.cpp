// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ===========================================================================
//  File: CEEGEN.CPP
// ===========================================================================
#include "stdafx.h"
#include "iceefilegen.h"
#include "ceefilegenwriter.h"

EXTERN_C HRESULT __stdcall CreateICeeFileGen(ICeeFileGen** pCeeFileGen)
{
    if (!pCeeFileGen)
        return E_POINTER;

    ICeeFileGen *gen = new (nothrow) ICeeFileGen();
    IfNullRet(gen);

    *pCeeFileGen = gen;
    return S_OK;
}

EXTERN_C HRESULT __stdcall DestroyICeeFileGen(ICeeFileGen** pCeeFileGen)
{
    if (!pCeeFileGen)
        return E_POINTER;
    delete *pCeeFileGen;
    *pCeeFileGen = NULL;
    return S_OK;
}

HRESULT ICeeFileGen::CreateCeeFile (HCEEFILE *ceeFile)
{
    return CreateCeeFileEx(ceeFile, ICEE_CREATE_FILE_PURE_IL);
}

HRESULT ICeeFileGen::CreateCeeFileEx (HCEEFILE *ceeFile, DWORD createFlags)
{
    return CreateCeeFileEx2(ceeFile, createFlags, NULL);
}

//
// Seed file is used as the base file. The new file data will be "appended" to the seed file
//

HRESULT ICeeFileGen::CreateCeeFileEx2 (HCEEFILE *ceeFile, DWORD createFlags, LPCWSTR seedFileName)
{
    if (!ceeFile)
        return E_POINTER;

    CeeFileGenWriter *gen = NULL;
    HRESULT hr;
    IfFailRet(CeeFileGenWriter::CreateNewInstanceEx(NULL, gen, createFlags, seedFileName));
    TESTANDRETURN(gen != NULL, E_OUTOFMEMORY);
    *ceeFile = gen;

    return S_OK;
}

HRESULT ICeeFileGen::CreateCeeFileFromICeeGen(ICeeGenInternal *pICeeGen, HCEEFILE *ceeFile, DWORD createFlags)
{
    if (!ceeFile)
        return E_POINTER;
    CCeeGen *genFrom = reinterpret_cast<CCeeGen*>(pICeeGen);
    CeeFileGenWriter *gen = NULL;
    HRESULT hr = CeeFileGenWriter::CreateNewInstance(genFrom, gen, createFlags);
    if (FAILED(hr))
        return hr;
    TESTANDRETURN(gen != NULL, E_OUTOFMEMORY);
    *ceeFile = gen;
    return S_OK;
}

HRESULT ICeeFileGen::DestroyCeeFile(HCEEFILE *ceeFile)
{
    if (!ceeFile)
        return E_POINTER;
    if (!*ceeFile)
        return E_POINTER;

    CeeFileGenWriter **gen = reinterpret_cast<CeeFileGenWriter**>(ceeFile);
    (*gen)->Cleanup();
    delete *gen;
    *ceeFile = NULL;
    return S_OK;
}

//

HRESULT ICeeFileGen::GetRdataSection (HCEEFILE ceeFile, HCEESECTION *section)
{
    TESTANDRETURNPOINTER(section);
    TESTANDRETURNARG(ceeFile != 0);

    CeeFileGenWriter *gen = reinterpret_cast<CeeFileGenWriter*>(ceeFile);
    *section = &gen->getStringSection();
    return S_OK;
}

HRESULT ICeeFileGen::GetIlSection (HCEEFILE ceeFile, HCEESECTION *section)
{
    TESTANDRETURNPOINTER(section);
    TESTANDRETURNARG(ceeFile != 0);

    CeeFileGenWriter *gen = reinterpret_cast<CeeFileGenWriter*>(ceeFile);
    *section = &gen->getIlSection();
    return S_OK;
}


HRESULT ICeeFileGen::GetSectionCreate (HCEEFILE ceeFile, const char *name, DWORD flags,
                                                        HCEESECTION *section)
{
    TESTANDRETURNPOINTER(section);
    TESTANDRETURNARG(ceeFile != 0);
    TESTANDRETURNPOINTER(name);

    CeeFileGenWriter *gen = reinterpret_cast<CeeFileGenWriter*>(ceeFile);
    CeeSection **ceeSection = reinterpret_cast<CeeSection**>(section);

    HRESULT hr = gen->getSectionCreate(name, flags, ceeSection);

    return hr;
}

HRESULT ICeeFileGen::SetDirectoryEntry(HCEEFILE ceeFile, HCEESECTION section, ULONG num, ULONG size, ULONG offset)
{
    TESTANDRETURNPOINTER(ceeFile);
    TESTANDRETURNPOINTER(section);

    CeeFileGenWriter *gen = reinterpret_cast<CeeFileGenWriter*>(ceeFile);
    CeeSection &sec = *(reinterpret_cast<CeeSection*>(section));
    return(gen->setDirectoryEntry(sec, num, size, offset));
}

HRESULT ICeeFileGen::GetSectionDataLen (HCEESECTION section, ULONG *dataLen)
{
    TESTANDRETURNPOINTER(section);
    TESTANDRETURNPOINTER(dataLen);

    CeeSection *sec = reinterpret_cast<CeeSection*>(section);
    *dataLen = sec->dataLen();
    return S_OK;
}

HRESULT ICeeFileGen::GetSectionRVA (HCEESECTION section, ULONG *rva)
{
    TESTANDRETURNPOINTER(section);
    TESTANDRETURNPOINTER(rva);

    CeeSection *sec = reinterpret_cast<CeeSection*>(section);
    *rva = sec->getBaseRVA();
    return S_OK;
}

HRESULT ICeeFileGen::GetSectionBlock (HCEESECTION section, ULONG len,
                            ULONG align, void **ppBytes)
{
    TESTANDRETURNPOINTER(section);
    TESTANDRETURNPOINTER(ppBytes);

    CeeSection *sec = reinterpret_cast<CeeSection*>(section);
    void *bytes = sec->getBlock(len, align);
    TESTANDRETURN(bytes != NULL, E_OUTOFMEMORY);
    *ppBytes = bytes;

    return S_OK;
}

HRESULT ICeeFileGen::AddSectionReloc (HCEESECTION section, ULONG offset, HCEESECTION relativeTo, CeeSectionRelocType relocType)
{
    TESTANDRETURNPOINTER(section);

    CeeSection *sec = reinterpret_cast<CeeSection*>(section);
    CeeSection *relSec = reinterpret_cast<CeeSection*>(relativeTo);

    if (relSec)
    {
        return(sec->addSectReloc(offset, *relSec, relocType));
    }
    else
    {
        return(sec->addBaseReloc(offset, relocType));
    }
}

HRESULT ICeeFileGen::SetOutputFileName (HCEEFILE ceeFile, _In_ LPWSTR outputFileName)
{
    TESTANDRETURNPOINTER(ceeFile);
    TESTANDRETURNPOINTER(outputFileName);

    CeeFileGenWriter *gen = reinterpret_cast<CeeFileGenWriter*>(ceeFile);
    return(gen->setOutputFileName(outputFileName));
}

__success(return == S_OK) HRESULT ICeeFileGen::GetOutputFileName (HCEEFILE ceeFile, _Out_ LPWSTR *outputFileName)
{
    TESTANDRETURNPOINTER(ceeFile);
    TESTANDRETURNPOINTER(outputFileName);

    CeeFileGenWriter *gen = reinterpret_cast<CeeFileGenWriter*>(ceeFile);
    TESTANDRETURNPOINTER(outputFileName);
    *outputFileName = gen->getOutputFileName();
    return S_OK;
}


HRESULT ICeeFileGen::SetResourceFileName (HCEEFILE ceeFile, _In_ LPWSTR resourceFileName)
{
    TESTANDRETURNPOINTER(ceeFile);
    TESTANDRETURNPOINTER(resourceFileName);

    CeeFileGenWriter *gen = reinterpret_cast<CeeFileGenWriter*>(ceeFile);
    return(gen->setResourceFileName(resourceFileName));
}

__success(return == S_OK)
HRESULT ICeeFileGen::GetResourceFileName (HCEEFILE ceeFile, _Out_ LPWSTR *resourceFileName)
{
    TESTANDRETURNPOINTER(ceeFile);
    TESTANDRETURNPOINTER(resourceFileName);

    CeeFileGenWriter *gen = reinterpret_cast<CeeFileGenWriter*>(ceeFile);
    TESTANDRETURNPOINTER(resourceFileName);
    *resourceFileName = gen->getResourceFileName();
    return S_OK;
}


HRESULT ICeeFileGen::SetImageBase(HCEEFILE ceeFile, size_t imageBase)
{
    TESTANDRETURNPOINTER(ceeFile);

    CeeFileGenWriter *gen = reinterpret_cast<CeeFileGenWriter*>(ceeFile);
    gen->setImageBase(imageBase);
    return S_OK;
}

HRESULT ICeeFileGen::SetImageBase64(HCEEFILE ceeFile, ULONGLONG imageBase)
{
    TESTANDRETURNPOINTER(ceeFile);

    CeeFileGenWriter *gen = reinterpret_cast<CeeFileGenWriter*>(ceeFile);
    gen->setImageBase64(imageBase);
    return S_OK;
}

HRESULT ICeeFileGen::SetFileAlignment(HCEEFILE ceeFile, ULONG fileAlignment)
{
    TESTANDRETURNPOINTER(ceeFile);

    CeeFileGenWriter *gen = reinterpret_cast<CeeFileGenWriter*>(ceeFile);
    gen->setFileAlignment(fileAlignment);
    return S_OK;
}

HRESULT ICeeFileGen::SetSubsystem(HCEEFILE ceeFile, DWORD subsystem, DWORD major, DWORD minor)
{
    TESTANDRETURNPOINTER(ceeFile);

    CeeFileGenWriter *gen = reinterpret_cast<CeeFileGenWriter*>(ceeFile);
    gen->setSubsystem(subsystem, major, minor);
    return S_OK;
}

HRESULT ICeeFileGen::GetIMapTokenIface(HCEEFILE ceeFile, IMetaDataEmit *emitter, IUnknown **pIMapToken)
{
    _ASSERTE(!"This is an obsolete function!");
    return E_NOTIMPL;
}

HRESULT ICeeFileGen::GetMethodRVA(HCEEFILE ceeFile, ULONG codeOffset, ULONG *codeRVA)
{
    TESTANDRETURNARG(ceeFile != 0);
    TESTANDRETURNPOINTER(codeRVA);
    CeeFileGenWriter *gen = reinterpret_cast<CeeFileGenWriter*>(ceeFile);
    gen->getMethodRVA(codeOffset, codeRVA);
    return S_OK;
}

HRESULT ICeeFileGen::EmitString(HCEEFILE ceeFile, _In_ LPWSTR strValue, ULONG *strRef)
{
    TESTANDRETURNPOINTER(ceeFile);

    CeeFileGenWriter *gen = reinterpret_cast<CeeFileGenWriter*>(ceeFile);
    return(gen->getStringSection().getEmittedStringRef(strValue, strRef));
}

HRESULT ICeeFileGen::LinkCeeFile (HCEEFILE ceeFile)
{
    TESTANDRETURNPOINTER(ceeFile);

    CeeFileGenWriter *gen = reinterpret_cast<CeeFileGenWriter*>(ceeFile);
    return gen->link();
}

HRESULT ICeeFileGen::GetHeaderInfo (HCEEFILE ceeFile, PIMAGE_NT_HEADERS *ppNtHeaders, PIMAGE_SECTION_HEADER *ppSections, ULONG *pNumSections)
{
    TESTANDRETURNPOINTER(ceeFile);

    CeeFileGenWriter *gen = reinterpret_cast<CeeFileGenWriter*>(ceeFile);
    gen->getPEWriter().getHeaderInfo(ppNtHeaders, ppSections, pNumSections);
    return S_OK;
}

HRESULT ICeeFileGen::GenerateCeeFile (HCEEFILE ceeFile)
{
    TESTANDRETURNPOINTER(ceeFile);

    CeeFileGenWriter *gen = reinterpret_cast<CeeFileGenWriter*>(ceeFile);
    return gen->generateImage(NULL);     // NULL means don't write in-memory buffer, uses outputFileName
}

HRESULT ICeeFileGen::SetEntryPoint(HCEEFILE ceeFile, mdMethodDef method)
{
    TESTANDRETURNPOINTER(ceeFile);

    CeeFileGenWriter *gen = reinterpret_cast<CeeFileGenWriter*>(ceeFile);
    return gen->setEntryPoint(method);
}

HRESULT ICeeFileGen::GetEntryPoint(HCEEFILE ceeFile, mdMethodDef *method)
{
    TESTANDRETURNPOINTER(ceeFile);

    CeeFileGenWriter *gen = reinterpret_cast<CeeFileGenWriter*>(ceeFile);
    TESTANDRETURNPOINTER(method);
    *method = gen->getEntryPoint();
    return S_OK;
}


HRESULT ICeeFileGen::SetComImageFlags (HCEEFILE ceeFile, DWORD mask)
{
    TESTANDRETURNPOINTER(ceeFile);

    CeeFileGenWriter *gen = reinterpret_cast<CeeFileGenWriter*>(ceeFile);
    return gen->setComImageFlags(mask);
}

HRESULT ICeeFileGen::ClearComImageFlags (HCEEFILE ceeFile, DWORD mask)
{
    TESTANDRETURNPOINTER(ceeFile);

    CeeFileGenWriter *gen = reinterpret_cast<CeeFileGenWriter*>(ceeFile);
    return gen->clearComImageFlags(mask);
}

HRESULT ICeeFileGen::GetComImageFlags (HCEEFILE ceeFile, DWORD *mask)
{
    TESTANDRETURNPOINTER(ceeFile);

    CeeFileGenWriter *gen = reinterpret_cast<CeeFileGenWriter*>(ceeFile);
    TESTANDRETURNPOINTER(mask);
    *mask = gen->getComImageFlags();
    return S_OK;
}


HRESULT ICeeFileGen::SetDllSwitch (HCEEFILE ceeFile, BOOL dllSwitch)
{
    TESTANDRETURNPOINTER(ceeFile);

    CeeFileGenWriter *gen = reinterpret_cast<CeeFileGenWriter*>(ceeFile);
    return(gen->setDllSwitch(dllSwitch==TRUE));
}

HRESULT ICeeFileGen::GetDllSwitch (HCEEFILE ceeFile, BOOL *dllSwitch)
{
    TESTANDRETURNPOINTER(ceeFile);

    CeeFileGenWriter *gen = reinterpret_cast<CeeFileGenWriter*>(ceeFile);
    TESTANDRETURNPOINTER(dllSwitch);
    *dllSwitch = gen->getDllSwitch();
    return S_OK;
}

HRESULT ICeeFileGen::EmitMetaDataEx (HCEEFILE ceeFile, IMetaDataEmit *emitter)
{
    TESTANDRETURNPOINTER(ceeFile);
    TESTANDRETURNPOINTER(emitter);

    CeeFileGenWriter *gen = reinterpret_cast<CeeFileGenWriter*>(ceeFile);
    return(gen->emitMetaData(emitter));
}

HRESULT ICeeFileGen::EmitMetaDataAt (HCEEFILE ceeFile, IMetaDataEmit *emitter, HCEESECTION section, DWORD offset, BYTE* buffer, unsigned buffLen)
{
    TESTANDRETURNPOINTER(ceeFile);
    TESTANDRETURNPOINTER(emitter);

    CeeFileGenWriter *gen = reinterpret_cast<CeeFileGenWriter*>(ceeFile);
    CeeSection* sec = reinterpret_cast<CeeSection*>(section);

    return(gen->emitMetaData(emitter, sec, offset, buffer, buffLen));
}

HRESULT ICeeFileGen::GetIMapTokenIfaceEx(HCEEFILE ceeFile, IMetaDataEmit *emitter, IUnknown **pIMapToken)
{
    TESTANDRETURNPOINTER(ceeFile);
    TESTANDRETURNPOINTER(pIMapToken);

    CeeFileGenWriter *gen = reinterpret_cast<CeeFileGenWriter*>(ceeFile);
    return gen->getMapTokenIface(pIMapToken);
}

HRESULT ICeeFileGen::AddNotificationHandler(HCEEFILE ceeFile,
                                            IUnknown *pHandler)
{
    TESTANDRETURNPOINTER(ceeFile);
    TESTANDRETURNPOINTER(pHandler);

    CeeFileGenWriter *gen = reinterpret_cast<CeeFileGenWriter*>(ceeFile);
    return gen->addNotificationHandler(pHandler);
}

HRESULT ICeeFileGen::SetManifestEntry(HCEEFILE ceeFile, ULONG size, ULONG offset)
{
    TESTANDRETURNPOINTER(ceeFile);

    CeeFileGenWriter *gen = reinterpret_cast<CeeFileGenWriter*>(ceeFile);
    return gen->setManifestEntry(size, offset);
}

HRESULT ICeeFileGen::SetStrongNameEntry(HCEEFILE ceeFile, ULONG size, ULONG offset)
{
    TESTANDRETURNPOINTER(ceeFile);

    CeeFileGenWriter *gen = reinterpret_cast<CeeFileGenWriter*>(ceeFile);
    return gen->setStrongNameEntry(size, offset);
}

HRESULT ICeeFileGen::ComputeSectionOffset(HCEESECTION section, _In_ char *ptr,
										  unsigned *offset)
{
    TESTANDRETURNPOINTER(section);

    CeeSection &sec = *(reinterpret_cast<CeeSection*>(section));

	*offset = sec.computeOffset(ptr);

	return S_OK;
}

__success(return == S_OK)
HRESULT ICeeFileGen::ComputeSectionPointer(HCEESECTION section, ULONG offset,
										  _Out_ char **ptr)
{
    TESTANDRETURNPOINTER(section);

    CeeSection &sec = *(reinterpret_cast<CeeSection*>(section));

	*ptr = sec.computePointer(offset);

	return S_OK;
}

HRESULT ICeeFileGen::ComputeOffset(HCEEFILE ceeFile, _In_ char *ptr,
								   HCEESECTION *pSection, unsigned *offset)
{
    TESTANDRETURNPOINTER(pSection);

    CeeFileGenWriter *gen = reinterpret_cast<CeeFileGenWriter*>(ceeFile);

	CeeSection *section;

	HRESULT hr = gen->computeOffset(ptr, &section, offset);

	if (SUCCEEDED(hr))
		*pSection = reinterpret_cast<HCEESECTION>(section);

	return hr;
}

HRESULT ICeeFileGen::GetCorHeader(HCEEFILE ceeFile,
								  IMAGE_COR20_HEADER **header)
{
    CeeFileGenWriter *gen = reinterpret_cast<CeeFileGenWriter*>(ceeFile);
	return gen->getCorHeader(header);
}

HRESULT ICeeFileGen::SetVTableEntry(HCEEFILE ceeFile, ULONG size, ULONG offset)
{
    TESTANDRETURNPOINTER(ceeFile);

    CeeFileGenWriter *gen = reinterpret_cast<CeeFileGenWriter*>(ceeFile);
    return gen->setVTableEntry(size, offset);
}

HRESULT ICeeFileGen::SetVTableEntry64(HCEEFILE ceeFile, ULONG size, void* ptr)
{
    TESTANDRETURNPOINTER(ceeFile);

    CeeFileGenWriter *gen = reinterpret_cast<CeeFileGenWriter*>(ceeFile);
    return gen->setVTableEntry64(size, ptr);
}

HRESULT ICeeFileGen::GetFileTimeStamp (HCEEFILE ceeFile, DWORD *pTimeStamp)
{
    TESTANDRETURNPOINTER(ceeFile);
    TESTANDRETURNPOINTER(pTimeStamp);

    CeeFileGenWriter *gen = reinterpret_cast<CeeFileGenWriter*>(ceeFile);
    return(gen->getFileTimeStamp(pTimeStamp));
}

