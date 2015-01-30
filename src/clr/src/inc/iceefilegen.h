//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

/*****************************************************************************
 **                                                                         **
 ** ICeeFileGen.h - code generator interface.                               **
 **                                                                         **
 ** This interface provides functionality to create a CLR PE executable.    **
 ** This will typically be used by compilers to generate their compiled     **
 ** output executable.                                                      **
 **                                                                         **
 ** The implemenation lives in mscorpe.dll                                  **
 **                                                                         **
 *****************************************************************************/

/*
  This is how this is typically used:
  
  // Step #1 ... Get CLR hosting API:
  #include <mscoree.h>
  #include <metahost.h>

  ICLRMetaHost * pMetaHost;
  CLRCreateInstance(CLSID_CLRMetaHost, IID_ICLRMetaHost, &pMetaHost); // defined in mscoree.h
  
  ICLRRuntimeInfo * pCLRRuntimeInfo;
  pMetaHost->GetRuntime(wszClrVersion, IID_ICLRRuntimeInfo, &pCLRRuntimeInfo);
  
  // Step #2 ... Load mscorpe.dll and get its entrypoints
  HMODULE hModule;
  pCLRRuntimeInfo->LoadLibrary(L"mscorpe.dll", &hModule);
  
  PFN_CreateICeeFileGen pfnCreateICeeFileGen = (PFN_CreateICeeFileGen)::GetProcAddress("CreateICeeFileGen"); // Windows API
  PFN_DestroyICeeFileGen pfnDestroyICeeFileGen = (PFN_DestroyICeeFileGen)::GetProcAddress("DestroyICeeFileGen"); // Windows API
  
  // Step #3 ... Use mscorpe.dll APIs
  pfnCreateICeeFileGen(...);    // Get a ICeeFileGen
  
  CreateCeeFile(...);           // Get a HCEEFILE (called for every output file needed)
  SetOutputFileName(...);       // Set the name for the output file
  pEmit = IMetaDataEmit object; // Get a metadata emitter
  GetSectionBlock(...);, AddSectionReloc(...); ... // Get blocks, write non-metadata information, and add necessary relocation
  EmitMetaDataEx(pEmit);        // Write out the metadata
  GenerateCeeFile(...);         // Write out the file. Implicitly calls LinkCeeFile and FixupCeeFile
  
  pfnDestroyICeeFileGen(...);   // Release the ICeeFileGen object
*/


#ifndef _ICEEFILEGEN_H_
#define _ICEEFILEGEN_H_

#include <ole2.h>
#include "cor.h"

class ICeeFileGen;

typedef void *HCEEFILE;

typedef HRESULT (__stdcall * PFN_CreateICeeFileGen)(ICeeFileGen ** ceeFileGen);  // call this to instantiate an ICeeFileGen interface
typedef HRESULT (__stdcall * PFN_DestroyICeeFileGen)(ICeeFileGen ** ceeFileGen); // call this to delete an ICeeFileGen

#define ICEE_CREATE_FILE_PE32	       0x00000001  // Create a PE  (32-bit)
#define ICEE_CREATE_FILE_PE64	       0x00000002  // Create a PE+ (64-bit) 
#define ICEE_CREATE_FILE_CORMAIN_STUB  0x00000004  // add a mscoree!_Cor___Main call stub 
#define ICEE_CREATE_FILE_STRIP_RELOCS  0x00000008  // strip the .reloc section
#define ICEE_CREATE_FILE_EMIT_FIXUPS   0x00000010  // emit fixups for use by Vulcan

#define ICEE_CREATE_MACHINE_MASK       0x0000FF00  // space for up to 256 machine targets
#define ICEE_CREATE_MACHINE_ILLEGAL    0x00000000  // An illegal machine name
#define ICEE_CREATE_MACHINE_I386       0x00000100  // Create a IMAGE_FILE_MACHINE_I386 
#define ICEE_CREATE_MACHINE_IA64       0x00000200  // Create a IMAGE_FILE_MACHINE_IA64
#define ICEE_CREATE_MACHINE_AMD64      0x00000400  // Create a IMAGE_FILE_MACHINE_AMD64
#define ICEE_CREATE_MACHINE_ARM        0x00000800  // Create a IMAGE_FILE_MACHINE_ARMNT

    // Pass this to CreateCeeFileEx to create a pure IL Exe or DLL
#define ICEE_CREATE_FILE_PURE_IL  ICEE_CREATE_FILE_PE32         | \
                                  ICEE_CREATE_FILE_CORMAIN_STUB | \
                                  ICEE_CREATE_MACHINE_I386

class ICeeFileGen {
  public:
    virtual HRESULT CreateCeeFile(HCEEFILE *ceeFile); // call this to instantiate a file handle

    // <TODO>@FUTURE: remove this function. We no longer support mdScope.</TODO>
    virtual HRESULT EmitMetaData (HCEEFILE ceeFile, IMetaDataEmit *emitter, mdScope scope);
    virtual HRESULT EmitLibraryName (HCEEFILE ceeFile, IMetaDataEmit *emitter, mdScope scope);
    virtual HRESULT EmitMethod (); // <TODO>@FUTURE: remove</TODO>
    virtual HRESULT GetMethodRVA (HCEEFILE ceeFile, ULONG codeOffset, ULONG *codeRVA); 
    virtual HRESULT EmitSignature (); // <TODO>@FUTURE: remove</TODO>

    virtual HRESULT EmitString (HCEEFILE ceeFile,_In_ LPWSTR strValue, ULONG *strRef);
    virtual HRESULT GenerateCeeFile (HCEEFILE ceeFile);

    virtual HRESULT SetOutputFileName (HCEEFILE ceeFile, _In_ LPWSTR outputFileName);
    _Return_type_success_(return == S_OK)
    virtual HRESULT GetOutputFileName (HCEEFILE ceeFile, _Out_ LPWSTR *outputFileName);

    virtual HRESULT SetResourceFileName (HCEEFILE ceeFile, _In_ LPWSTR resourceFileName);

    _Return_type_success_(return == S_OK)
    virtual HRESULT GetResourceFileName (HCEEFILE ceeFile, _Out_ LPWSTR *resourceFileName);

    virtual HRESULT SetImageBase(HCEEFILE ceeFile, size_t imageBase);

    virtual HRESULT SetSubsystem(HCEEFILE ceeFile, DWORD subsystem, DWORD major, DWORD minor);

    virtual HRESULT SetEntryClassToken (); //<TODO>@FUTURE: remove</TODO>
    virtual HRESULT GetEntryClassToken (); //<TODO>@FUTURE: remove</TODO>

    virtual HRESULT SetEntryPointDescr (); //<TODO>@FUTURE: remove</TODO>
    virtual HRESULT GetEntryPointDescr (); //<TODO>@FUTURE: remove</TODO>

    virtual HRESULT SetEntryPointFlags (); //<TODO>@FUTURE: remove</TODO>
    virtual HRESULT GetEntryPointFlags (); //<TODO>@FUTURE: remove</TODO>

    virtual HRESULT SetDllSwitch (HCEEFILE ceeFile, BOOL dllSwitch);
    virtual HRESULT GetDllSwitch (HCEEFILE ceeFile, BOOL *dllSwitch);

    virtual HRESULT SetLibraryName (HCEEFILE ceeFile, _In_ LPWSTR LibraryName);
    _Return_type_success_( return == S_OK )
    virtual HRESULT GetLibraryName (HCEEFILE ceeFile, _Out_ LPWSTR *LibraryName);

    virtual HRESULT SetLibraryGuid (HCEEFILE ceeFile, _In_ LPWSTR LibraryGuid);

    virtual HRESULT DestroyCeeFile(HCEEFILE *ceeFile); // call this to delete a file handle

    virtual HRESULT GetSectionCreate (HCEEFILE ceeFile, const char *name, DWORD flags, HCEESECTION *section);
    virtual HRESULT GetIlSection (HCEEFILE ceeFile, HCEESECTION *section);
    virtual HRESULT GetRdataSection (HCEEFILE ceeFile, HCEESECTION *section);

    virtual HRESULT GetSectionDataLen (HCEESECTION section, ULONG *dataLen);
    virtual HRESULT GetSectionBlock (HCEESECTION section, ULONG len, ULONG align=1, void **ppBytes=0);
    virtual HRESULT TruncateSection (HCEESECTION section, ULONG len);
    virtual HRESULT AddSectionReloc (HCEESECTION section, ULONG offset, HCEESECTION relativeTo, CeeSectionRelocType relocType);

    // deprecated: use SetDirectoryEntry instead
    virtual HRESULT SetSectionDirectoryEntry (HCEESECTION section, ULONG num);

    virtual HRESULT CreateSig (); //<TODO>@FUTURE: Remove</TODO>
    virtual HRESULT AddSigArg (); //<TODO>@FUTURE: Remove</TODO>
    virtual HRESULT SetSigReturnType (); //<TODO>@FUTURE: Remove</TODO>
    virtual HRESULT SetSigCallingConvention (); //<TODO>@FUTURE: Remove</TODO>
    virtual HRESULT DeleteSig (); //<TODO>@FUTURE: Remove</TODO>

    virtual HRESULT SetEntryPoint (HCEEFILE ceeFile, mdMethodDef method);
    virtual HRESULT GetEntryPoint (HCEEFILE ceeFile, mdMethodDef *method);

    virtual HRESULT SetComImageFlags (HCEEFILE ceeFile, DWORD mask);
    virtual HRESULT GetComImageFlags (HCEEFILE ceeFile, DWORD *mask);

    // get IMapToken interface for tracking mapped tokens
    virtual HRESULT GetIMapTokenIface(HCEEFILE ceeFile, IMetaDataEmit *emitter, IUnknown **pIMapToken);
    virtual HRESULT SetDirectoryEntry (HCEEFILE ceeFile, HCEESECTION section, ULONG num, ULONG size, ULONG offset = 0);

    // Write out the metadata in "emitter" to the metadata section in "ceeFile"
    // Use EmitMetaDataAt() for more control
    virtual HRESULT EmitMetaDataEx (HCEEFILE ceeFile, IMetaDataEmit *emitter); 

    virtual HRESULT EmitLibraryNameEx (HCEEFILE ceeFile, IMetaDataEmit *emitter);
    virtual HRESULT GetIMapTokenIfaceEx(HCEEFILE ceeFile, IMetaDataEmit *emitter, IUnknown **pIMapToken);

    virtual HRESULT EmitMacroDefinitions(HCEEFILE ceeFile, void *pData, DWORD cData);
    virtual HRESULT CreateCeeFileFromICeeGen(
        ICeeGen *pFromICeeGen, HCEEFILE *ceeFile, DWORD createFlags = ICEE_CREATE_FILE_PURE_IL); // call this to instantiate a file handle

    virtual HRESULT SetManifestEntry(HCEEFILE ceeFile, ULONG size, ULONG offset);

    virtual HRESULT SetEnCRVABase(HCEEFILE ceeFile, ULONG dataBase, ULONG rdataBase);
    virtual HRESULT GenerateCeeMemoryImage (HCEEFILE ceeFile, void **ppImage);

    virtual HRESULT ComputeSectionOffset(HCEESECTION section, _In_ char *ptr,
                                         unsigned *offset);
    
    virtual HRESULT ComputeOffset(HCEEFILE file, _In_ char *ptr,
                                  HCEESECTION *pSection, unsigned *offset);
    
    virtual HRESULT GetCorHeader(HCEEFILE ceeFile, 
                                 IMAGE_COR20_HEADER **header);
    
    // Layout the sections and assign their starting addresses
    virtual HRESULT LinkCeeFile (HCEEFILE ceeFile);     

    // Apply relocations to any pointer data. Also generate PE base relocs
    virtual HRESULT FixupCeeFile (HCEEFILE ceeFile);

    // Base RVA assinged to the section. To be called only after LinkCeeFile()
    virtual HRESULT GetSectionRVA (HCEESECTION section, ULONG *rva);
    
    _Return_type_success_(return == S_OK)
    virtual HRESULT ComputeSectionPointer(HCEESECTION section, ULONG offset,
                                          _Out_ char **ptr);

    virtual HRESULT SetObjSwitch (HCEEFILE ceeFile, BOOL objSwitch);
    virtual HRESULT GetObjSwitch (HCEEFILE ceeFile, BOOL *objSwitch);
    virtual HRESULT SetVTableEntry(HCEEFILE ceeFile, ULONG size, ULONG offset);
    // See the end of interface for another overload of AetVTableEntry

    virtual HRESULT SetStrongNameEntry(HCEEFILE ceeFile, ULONG size, ULONG offset);

    // Emit the metadata from "emitter".
    // If 'section != 0, it will put the data in 'buffer'.  This
    // buffer is assumed to be in 'section' at 'offset' and of size 'buffLen'
    // (should use GetSaveSize to insure that buffer is big enough
    virtual HRESULT EmitMetaDataAt (HCEEFILE ceeFile, IMetaDataEmit *emitter, 
                                    HCEESECTION section, DWORD offset, 
                                    BYTE* buffer, unsigned buffLen);

    virtual HRESULT GetFileTimeStamp (HCEEFILE ceeFile, DWORD *pTimeStamp);

    // Add a notification handler. If it implements an interface that
    // the ICeeFileGen understands, S_OK is returned. Otherwise,
    // E_NOINTERFACE.
    virtual HRESULT AddNotificationHandler(HCEEFILE ceeFile,
                                           IUnknown *pHandler);

    virtual HRESULT SetFileAlignment(HCEEFILE ceeFile, ULONG fileAlignment);

    virtual HRESULT ClearComImageFlags (HCEEFILE ceeFile, DWORD mask);

    // call this to instantiate a PE+ (64-bit PE file)
    virtual HRESULT CreateCeeFileEx(HCEEFILE *ceeFile, ULONG createFlags);
    virtual HRESULT SetImageBase64(HCEEFILE ceeFile, ULONGLONG imageBase);

    virtual HRESULT GetHeaderInfo (HCEEFILE ceeFile, PIMAGE_NT_HEADERS *ppNtHeaders,
                                                     PIMAGE_SECTION_HEADER *ppSections,
                                                     ULONG *pNumSections);

    // Seed file is a base file which is copied over into the output file
    // Note that there are restrictions on the seed file (the sections
    // cannot be relocated), and that the copy is not complete as the new
    // headers overwrite the seed file headers.
    virtual HRESULT CreateCeeFileEx2(HCEEFILE *ceeFile, ULONG createFlags,
                                     LPCWSTR seedFileName = NULL);

    virtual HRESULT SetVTableEntry64(HCEEFILE ceeFile, ULONG size, void* ptr);
};

#endif
