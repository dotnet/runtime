//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
//*****************************************************************************
// File: InProcDac.h
// 

//
//*****************************************************************************

#ifndef _INPROCDAC_H
#define _INPROCDAC_H

#if defined(FEATURE_DBGIPC_TRANSPORT_VM)
#include "dacdbiinterface.h"
#include "cordebug.h"
#include "xcordebug.h"

#ifndef DACCESS_COMPILE
#include "ddunpack.h"
#endif

class IDacDbiMarshalStub;
class ReadBuffer;
class WriteBuffer;

// 
// InProcDac is a helper class used by the Debugger class to make DAC and
// the IDacDbiInterface available from within process.
// This is done on the Macintosh because we don't have OS support for our
// normal out-of-process access (eg. VM read as non-root user).
// 
// Note that we don't ever actually use this in DACCESS_COMPILE builds - it's
// implementation is compiled into just mscorwks, but the callbacks (data target
// and IMetaDataLookup) are called from mscordacwks.  We need the declaration
// visible in DACCESS_COMPILE builds because a field of this type is contained
// by-value in the Debugger class, and so we need the correct size for field
// layout.
// 
class InProcDac 
	: private IDacDbiInterface::IMetaDataLookup,
	  private IDacDbiInterface::IAllocator
{
public:
    InProcDac() DAC_EMPTY();
    ~InProcDac() DAC_EMPTY();

    void Initialize();
    void Cleanup();

    // This takes a marshalled version of a DD interface request
    HRESULT DoRequest(ReadBuffer * pSend, WriteBuffer * pResult);

private:

    // IMetaDataLookup methods
    virtual IMDInternalImport * LookupMetaData(VMPTR_PEFile addressPEFile, bool &isILMetaDataForNGENImage);

    // 
    // IAllocator interfaces
    // 
    virtual void * Alloc(SIZE_T lenBytes) DAC_EMPTY_RET(NULL);

    virtual void Free(void * p) DAC_EMPTY();

   class InProcDataTarget :
        public ICorDebugMutableDataTarget
    {
    public:
        InProcDataTarget();
        virtual ~InProcDataTarget();

        // IUnknown.
        virtual HRESULT STDMETHODCALLTYPE QueryInterface(
            REFIID riid,
            void** ppInterface);

        virtual ULONG STDMETHODCALLTYPE AddRef();

        virtual ULONG STDMETHODCALLTYPE Release();

        // ICorDebugMutableDataTarget.
        virtual HRESULT STDMETHODCALLTYPE GetPlatform( 
            CorDebugPlatform *pPlatform);

        virtual HRESULT STDMETHODCALLTYPE ReadVirtual( 
            CORDB_ADDRESS address,
            PBYTE pBuffer,
            ULONG32 request,
            ULONG32 *pcbRead);

        virtual HRESULT STDMETHODCALLTYPE WriteVirtual( 
            CORDB_ADDRESS address,
            const BYTE * pBuffer,
            ULONG32 request);

        virtual HRESULT STDMETHODCALLTYPE GetThreadContext(
            DWORD dwThreadID,
            ULONG32 contextFlags,
            ULONG32 contextSize,
            PBYTE context);

        virtual HRESULT STDMETHODCALLTYPE SetThreadContext(
            DWORD dwThreadID,
            ULONG32 contextSize,
            const BYTE * context);

        virtual HRESULT STDMETHODCALLTYPE ContinueStatusChanged(
            DWORD dwThreadId,
            CORDB_CONTINUE_STATUS continueStatus);

    private:
        LONG m_ref;                         // Reference count.
    };



private:
    // 
    // InProcDac Fields
    // 
    ReleaseHolder<InProcDataTarget>     m_pDataTarget;
    HModuleHolder                       m_hDacModule;
#ifndef DACCESS_COMPILE
    IDacDbiInterface *                  m_pDacDbi;
    DDUnpack *                          m_pUnpacker;
#else
    VOID *                              m_pDacDbi;
    VOID *                              m_pUnpacker;
#endif
};


#ifdef DACCESS_COMPILE
// This method is a funny case for DAC and DacCop.  InProcDac isn't used in DACCESS_COMPILE builds at all
// (inprocdac.cpp isn't compiled in DAC builds), but we need the declaration since an instance
// of it is contained by-value in the Debugger class (need to know the right size so field layout
// matches the target).  The LookupMetadata function is called from DAC, and so DacCop searches
// for all implementations of it in mscordacwks.dll and find this one (the real one is either in 
// mscordbi.dll or coreclr which DacCop doesn't analyze).  We need an implementation of virtual
// methods for the DACCESS_COMPILE build, but rather than use the usual DAC_EMPTY macros we'll
// use this explicit implementation here to avoid a DacCop violation.
inline IMDInternalImport * InProcDac::LookupMetaData(VMPTR_PEFile addressPEFile, bool &isILMetaDataForNGENImage)
{
    SUPPORTS_DAC;   // not really - but we should never be called
    _ASSERTE_MSG(false, "This implementation should never be called in DAC builds");
    DacError(E_UNEXPECTED);
    return NULL;
}
#endif  // DACCESS_COMPILE



#endif // FEATURE_DBGIPC_TRANSPORT_VM

#endif //_INPROCDAC_H
