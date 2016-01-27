// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// --------------------------------------------------------------------------------
// PEFingerprint.cpp
// 

//
//
// Dev11 note on timing of torn state detection:
//
//   This implementation of PEFingerprint contains a known flaw: The MVID/SNHash/TPBand
//   torn state test only occurs after the file is already opened and made available
//   for the runtime to use. In fact, we don't do it until someone asks for a Commit
//   on the fingerprint.
//
//   This is clearly a perversion of the design: however, it was not feasible
//   to do the check beforehand within the current codebase without incurring
//   severe performance costs or major code surgery.
//
//   For Dev11, however, we accept this because of two things:
//
//     - GAC assemblies are installed through gacutil.exe which always timestamps
//       the assembly based on the time of install. Thus, timestamp collisions
//       inside the GAC should not happen unless someone manually tampers with the GAC.
//       Since we do verify the timestamp and lock the file before opening it,
//       it is not a problem that the actual mvid/snhash check happens later than it should.
// --------------------------------------------------------------------------------



#include "common.h"
#include "pefile.h"
#include "pefingerprint.h"

#ifdef FEATURE_FUSION

static VOID ThrowTornState(LPCWSTR path);
static void FetchILTimestampAndSize(LPCWSTR path, FILETIME *pTimestamp, DWORD *pSize, HANDLE hFileHandleIfOpen = INVALID_HANDLE_VALUE);


const size_t PEFingerprint::s_offsets[] =
{
    offsetof(PEFingerprint, m_timeStamp),
    offsetof(PEFingerprint, m_size),
    offsetof(PEFingerprint, m_mvid),
};

const DWORD PEFingerprint::s_sizes[] =
{
    sizeof(((PEFingerprint *)NULL)->m_timeStamp),
    sizeof(((PEFingerprint *)NULL)->m_size),
    sizeof(((PEFingerprint *)NULL)->m_mvid),
};



//---------------------------------------------------------------
// Ctor
//---------------------------------------------------------------
PEFingerprint::PEFingerprint(PEImage *owner) :
   m_pcrst(NULL)
  ,m_peimage(owner)
  ,m_commitMask(0)
  ,m_alreadyLoaded(FALSE)
  ,m_priorLockAndLoadFailure(S_OK)
{

    LIMITED_METHOD_CONTRACT;

    _ASSERTE(owner);

    memset(&m_timeStamp, 0xcc, sizeof(m_timeStamp));
    memset(&m_size, 0xcc, sizeof(m_size));
    memset(&m_mvid, 0xcc, sizeof(m_mvid));

    return;
}


//---------------------------------------------------------------
// PEFingerprint factory
//---------------------------------------------------------------
/*static*/ PEFingerprint *PEFingerprint::CreatePEFingerprint(PEImage *owner)
{
    CONTRACTL
    {
        THROWS; 
        GC_TRIGGERS;
        MODE_ANY;
        SO_INTOLERANT;
        INJECT_FAULT(COMPlusThrowOM();); 
    }
    CONTRACTL_END

    NewHolder<PEFingerprint> pPEFingerprint = new PEFingerprint(owner);
    pPEFingerprint->m_pcrst = new Crst(CrstLeafLock);

    //---------------------------------------------------------------
    // Since obtaining the timestamp is cheap and doesn't need to open the
    // file, go ahead and get it now and commit into the fingerprint.
    //
    // @review: Would it be better to lock the file right now to
    // prevent overwriter for the life of the fingerprint?
    //---------------------------------------------------------------
    LPCWSTR path = pPEFingerprint->m_peimage->GetPath();
    _ASSERTE(path);

    FILETIME lastWriteTime;
    DWORD size;
    FetchILTimestampAndSize(path, &lastWriteTime, &size);

    ILFingerprintComponent components[] =
    {
        { ILFTagTimestamp, &lastWriteTime },
        { ILFTagSize, &size },
    };
    BOOL success = pPEFingerprint->CommitAndCompareMulti(COUNTOF(components), components);
    _ASSERTE(success);  // No way this commit can fail - we own the only pointer!
    return pPEFingerprint.Extract();
}



//---------------------------------------------------------------
// Dtor
//---------------------------------------------------------------
PEFingerprint::~PEFingerprint()
{
    LIMITED_METHOD_CONTRACT;
    delete m_pcrst;
    return;
}

//---------------------------------------------------------------
// AddRef
//---------------------------------------------------------------
ULONG PEFingerprint::AddRef()
{
    LIMITED_METHOD_CONTRACT;
    return m_peimage->AddRef();
}

//---------------------------------------------------------------
// Release
//---------------------------------------------------------------
ULONG PEFingerprint::Release()
{
    LIMITED_METHOD_CONTRACT;
    STATIC_CONTRACT_GC_TRIGGERS;
    STATIC_CONTRACT_CAN_TAKE_LOCK;
    return m_peimage->Release();
}

//---------------------------------------------------------------------------------------------
// Convenience fcn: equivalent to calling CommitAndCompareMulti() with one component.
//---------------------------------------------------------------------------------------------
BOOL PEFingerprint::CommitAndCompare(ILFingerprintTag componentType, LPCVOID data)
{
    CONTRACTL
    {
        THROWS; 
        GC_TRIGGERS;
        MODE_ANY;
        SO_INTOLERANT;
        INJECT_FAULT(COMPlusThrowOM();); 
    }
    CONTRACTL_END

    ILFingerprintComponent c = {componentType, data};
    return CommitAndCompareMulti(1, &c);
}


 //---------------------------------------------------------------------------------------------
 // CommitAndCompareMulti(): Atomically commits one or more fingerprint components into
 // the fingerprint. Once a component is committed, its value can never change.
 //
 // An attempt to commit a component succeeds only if the component was not already committed
 // or the prior value maches the new one exactly.
 //
 // Calling CommitAndCompare() multiple times is not equivalent to calling CommitAndCompareMulti().
 // CommitAndCompareMulti() is atomic - either all the commits happen or none of them do.
 //
 // Returns:
 //    TRUE:  All passed components committed successful.
 //    FALSE: At leat one component failed to commit successfully.
 //---------------------------------------------------------------------------------------------
BOOL PEFingerprint::CommitAndCompareMulti(UINT numComponents, const ILFingerprintComponent *pComponents)
{
    CONTRACTL
    {
        THROWS; 
        GC_TRIGGERS;
        MODE_ANY;
        SO_INTOLERANT;
        INJECT_FAULT(COMPlusThrowOM();); 
    }
    CONTRACTL_END

    //------------------------------------------------------------------------------
    // See "Dev11 note on timing of torn state detection". This step should not be
    // here but this is how we "verify" the MVID/SNHash on IL open. We wait until
    // the first time someone attempts a commit on an opened file to do the check.
    // The caller will think we did the check at file open time, even though we
    // actually left a window of vulnerability.
    //------------------------------------------------------------------------------
    if (!m_alreadyLoaded)
    {
        PEImageHolder pOpenedILimage;
        m_peimage->Clone(MDInternalImport_OnlyLookInCache,&pOpenedILimage);

        if(pOpenedILimage != NULL && pOpenedILimage->IsOpened())
        {

            for (UINT j = 0; j < numComponents; j++)
            {
                // Don't open if we're just checking timestamp (forecloses possible reentrancy problems
                // due to timestamp commits occurring within PEImage itself.)
                ILFingerprintTag tag = pComponents[j]._tag;
                if (tag == ILFTagMvid)
                {
                    this->LockAndLoadIL();
                    break;
                }

            }
        }
    }

    //------------------------------------------------------------------------------
    // Inside the crit section, make sure all the components can successfully commit
    // before commitng any of them.
    //------------------------------------------------------------------------------
    CrstHolder ch(m_pcrst);
    UINT i;
    for (i = 0; i < numComponents; i++)
    {
        ILFingerprintTag tag = pComponents[i]._tag;
        if (IsComponentCommitted(tag))
        {
            if (0 != memcmp(pComponents[i]._data, TagDataStart(tag), TagDataSize(tag)))
                return FALSE;
        }
    }
    for (i = 0; i < numComponents; i++)
    {
        ILFingerprintTag tag = pComponents[i]._tag;
        if (!IsComponentCommitted(tag))
        {
            memcpy(TagDataStart(tag), pComponents[i]._data, TagDataSize(tag));
            SetComponentCommitted(tag);
        }
    }

    return TRUE;
}



//---------------------------------------------------------------------------------------------
// LockAndLoadIL()
//
//   Forces the runtime to open the IL file and lock it against future overwrites. This
//   is bad for working set so this should be avoided.
//
//   Once opened and locked, this method extracts the actual fingerprint from the IL file
//   and attempts to commit it into the ILFingerprint. If successful, all future commits
//   will now be compared against this trusted data. If unsuccessful, this is a torn state
//   situation and LockAndLoadIL() throws the torn state exception.
//---------------------------------------------------------------------------------------------
void PEFingerprint::LockAndLoadIL()
{
    CONTRACTL
    {
        THROWS; 
        GC_TRIGGERS;
        MODE_ANY;
        SO_INTOLERANT;
        INJECT_FAULT(COMPlusThrowOM();); 
    }
    CONTRACTL_END

    //----------------------------------------------------------------------------------
    // If already loaded, return the prior result.
    //----------------------------------------------------------------------------------
    if (m_alreadyLoaded)
    {
        if (FAILED(m_priorLockAndLoadFailure))
        {
            ThrowHR(m_priorLockAndLoadFailure);
        }
        else
        {
            return;
        }
    }
    PEImageHolder pOpenedILimage;
    m_peimage->Clone(MDInternalImport_Default,&pOpenedILimage);
    HRESULT hr = S_OK;
    {
        GCX_PREEMP();
        IfFailThrow(m_peimage->TryOpenFile());
    }
    //----------------------------------------------------------------------------------
    // Force the file open (by requesting a metadata pointer to it.)
    //----------------------------------------------------------------------------------
    IMDInternalImport *pMDImport = NULL;
    EX_TRY
    {
        pMDImport = pOpenedILimage->GetMDImport();
        hr = S_OK;
    }
    EX_CATCH_HRESULT(hr);
    if (Exception::IsTransient(hr))
        ThrowHR(hr);
    if (FAILED(hr))
    {
        m_priorLockAndLoadFailure = hr;
        m_alreadyLoaded = TRUE;
        ThrowHR(hr);
    }

    m_alreadyLoaded = TRUE;

    //------------------------------------------------------------------------------
    // See "Dev11 note on timing of torn state detection". This step should not be
    // here as the "right" design is to extract the actual MVID before we officially
    // open the file. But since we don't do that in the current implementation, we do
    // it now.
    //------------------------------------------------------------------------------
    GUID mvid;
    pOpenedILimage->GetMVID(&mvid);

    BOOL success = this->CommitAndCompare(ILFTagMvid, &mvid);
    if (!success)
        ThrowTornState(m_peimage->GetPath());
}


//==================================================================================
// Helper for throwing a torn state exception.
//==================================================================================
static VOID ThrowTornState(LPCWSTR path)
{
    CONTRACTL
    {
        THROWS; 
        GC_TRIGGERS;
        MODE_ANY;
        SO_INTOLERANT;
        INJECT_FAULT(COMPlusThrowOM();); 
    }
    CONTRACTL_END

    COMPlusThrow(kFileLoadException, IDS_EE_TORNSTATE, path);
}

#endif // FEATURE_FUSION




//==================================================================================
// This holder must be wrapped around any code that opens an IL image.
// It will verify that the actual fingerprint doesn't conflict with the stored
// assumptions in the PEFingerprint. (If it does, the holder constructor throws
// a torn state exception.)
//
// It is a holder because it needs to keep a file handle open to prevent
// anyone from overwriting the IL after the check has been done. Once
// you've opened the "real" handle to the IL (i.e. LoadLibrary/CreateFile),
// you can safely destruct the holder.
//==================================================================================
PEFingerprintVerificationHolder::PEFingerprintVerificationHolder(PEImage *owner)
{
    CONTRACTL
    {
        THROWS; 
        GC_TRIGGERS;
        MODE_ANY;
        SO_INTOLERANT;
        INJECT_FAULT(COMPlusThrowOM();); 
    }
    CONTRACTL_END

#ifdef FEATURE_FUSION
    if (owner->IsTrustedNativeImage())
        return;   // Waste of cycles to check timestamps for NI images.


    LPCWSTR path = owner->GetPath();
    _ASSERTE(path);

    if (owner->IsOpened()) 
        return;   // Not the first layout to be opened - no need to repeat the work in that case.

    // First, lock the file and verify that the timestamp hasn't changed.
    TESTHOOKCALL(AboutToLockImage(path, IsCompilationProcess())); 
    m_fileHandle = WszCreateFile(path, GENERIC_READ, FILE_SHARE_READ, NULL, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, NULL);
    if (m_fileHandle == INVALID_HANDLE_VALUE)
    {
        // @review: If this call to open the file fails, it sounds a bit risky to fail the PE open altogether
        //          just to do a torn state check. Let the torn state detection bake a bit before we take this step.
        return;
    }

    FILETIME lastWriteTime;
    DWORD size;
    FetchILTimestampAndSize(path, &lastWriteTime, &size, m_fileHandle);
    ReleaseHolder<IILFingerprint> fingerPrint;
    ILFingerprintComponent components[] =
    {
        { ILFTagTimestamp, &lastWriteTime },
        { ILFTagSize, &size },
    };
    IfFailThrow(owner->GetILFingerprint(&fingerPrint));
    if (!fingerPrint->CommitAndCompareMulti(COUNTOF(components), components))
        ThrowTornState(path);


    // Now, verify that the MVID/SNHash/TPBand hasn't changed.
    // Oh wait, where that'd code go? See "Dev11 note on timing of torn state detection".
#endif // FEATURE_FUSION
    return;
}

#ifdef FEATURE_FUSION
#ifndef DACCESS_COMPILE
class CachingILFingerprintFactory : public IILFingerprintFactory
{
private:
    LONG m_refCount;
    Crst m_lock;

    // Hash Type ... NOTE! This is a case sensitive hash of a filename to an IL fingerprint.
    // This is acceptable as duplicates are not errors, and chosen as case insensitive hashes
    // are somewhat slower, and most hash lookups will actually match in case. If this is not
    // the case, converting to a case-insensitive hash should be trivial.
    typedef StringSHashWithCleanup< IILFingerprint, WCHAR > ILFingerprintHash;
    typedef StringHashElement< IILFingerprint, WCHAR > ILFingerprintHashElement;

    ILFingerprintHash m_hash;

    ~CachingILFingerprintFactory()
    {
    }

public:

    CachingILFingerprintFactory() : m_refCount(1), m_lock(CrstILFingerprintCache)
    {
        CONTRACTL
        {
            THROWS;
            GC_TRIGGERS;
        }
        CONTRACTL_END;
    }

    STDMETHOD_(ULONG, AddRef)()
    {
        CONTRACT(ULONG)
        {
            PRECONDITION(m_refCount>0 && m_refCount < COUNT_T_MAX);
            NOTHROW;
            GC_NOTRIGGER;
        }
        CONTRACT_END;

        RETURN (static_cast<ULONG>(FastInterlockIncrement(&m_refCount)));
    }

    STDMETHOD_(ULONG, Release)()
    {
        CONTRACTL
        {
            DESTRUCTOR_CHECK;
            NOTHROW;
            MODE_ANY;
            FORBID_FAULT;
        }
        CONTRACTL_END;

        ULONG result = 0;
        result=FastInterlockDecrement(&m_refCount);
        if (result == 0)
            delete this;

        return result;
    }

    STDMETHOD(GetILFingerprintForPath)(
        LPCWSTR pwzPath, 
        IILFingerprint **ppFingerprint)
    {
        CONTRACTL
        {
            NOTHROW;
            GC_TRIGGERS;
        }
        CONTRACTL_END;
        HRESULT hr = S_OK;

        EX_TRY
        {
            CrstHolder ch(&m_lock);
            // Lookup in cache
            ILFingerprintHashElement *pCacheElement = m_hash.Lookup(pwzPath);

            // If that fails, run the parser, and populate the cache
            if (pCacheElement != NULL)
            {
                *ppFingerprint = clr::SafeAddRef(pCacheElement->Object);
            }
            else
            {
                // Create new assembly name object;
                ReleaseHolder<IILFingerprint> pFingerprint;
                NewArrayHolder<WCHAR> pwzPathCopy;
                IfFailThrow(RuntimeGetILFingerprintForPath(pwzPath, &pFingerprint));

                // Create hash element object
                NewHolder<ILFingerprintHashElement> pHashElem = new ILFingerprintHashElement();
                pwzPathCopy = DuplicateStringThrowing(pwzPath);
                pHashElem->String = pwzPathCopy;
                pHashElem->Object = pFingerprint;

                // Insert into hash table
                m_hash.Add(pHashElem);

                *ppFingerprint = clr::SafeAddRef(pFingerprint);

                // Prevent disastrous cleanup
                pwzPathCopy.SuppressRelease();
                pHashElem.SuppressRelease();
                pFingerprint.SuppressRelease();
            }
        }
        EX_CATCH_HRESULT(hr);

        return hr;
    }
};

HRESULT RuntimeCreateCachingILFingerprintFactory(IILFingerprintFactory **ppILFingerprintFactory)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
    }
    CONTRACTL_END;
    HRESULT hr = S_OK;

    EX_TRY
    {
        *ppILFingerprintFactory = new CachingILFingerprintFactory();
    }
    EX_CATCH_HRESULT(hr);

    return hr;
}

//-------------------------------------------------------------------------------------------------------------
// Common routine to fetch the IL file's timestamp and size. If the caller already has an open file handle, it should
// pass that as "hFileHandleIfOpen" to avoid the overhead of opening the file again.
//-------------------------------------------------------------------------------------------------------------
static void FetchILTimestampAndSize(LPCWSTR path, FILETIME *pTimestamp, DWORD *pSize, HANDLE hFileHandleIfOpen /* = INVALID_HANDLE_VALUE*/)
{
    CONTRACTL
    {
        THROWS; 
        GC_TRIGGERS;
        MODE_ANY;
        SO_INTOLERANT;
        INJECT_FAULT(COMPlusThrowOM();); 
    }
    CONTRACTL_END

    _ASSERTE(pTimestamp != NULL && pSize != NULL);

    if (hFileHandleIfOpen != INVALID_HANDLE_VALUE)
    {
        BY_HANDLE_FILE_INFORMATION info;
        if (!GetFileInformationByHandle(hFileHandleIfOpen, &info))
            ThrowLastError();
        *pTimestamp = info.ftLastWriteTime;
        *pSize = info.nFileSizeLow;
        return;
    }

    // For normal files, we can obtain the timestamp without opening the file - attempt to do so.
    WIN32_FILE_ATTRIBUTE_DATA wfd;
    if (!WszGetFileAttributesEx(path, GetFileExInfoStandard, &wfd))
        ThrowLastError();
    if (!(wfd.dwFileAttributes & FILE_ATTRIBUTE_REPARSE_POINT))
    {
        *pTimestamp = wfd.ftLastWriteTime;
        *pSize = wfd.nFileSizeLow;
        return;
    }

    // If we got here, the original path pointed to a symbolic or some other form of reparse point. In such cases, GetFileAttributesEx
    // may not return the same timestamp as GetFileInformationByHandle. (E.g. in the symbolic link case, GetFileAttributeEx returns
    // the symbolic link's timestamp rather than the target's timestamp.)
    //
    // Since this is the uncommon case, we can justify the perf hit of opening the file so we get the timestamp
    // on the actual target. 
    HandleHolder hFile(WszCreateFile(path, GENERIC_READ, FILE_SHARE_READ, NULL, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, NULL));
    if (hFile == INVALID_HANDLE_VALUE)
        ThrowLastError();
    BY_HANDLE_FILE_INFORMATION info;
    if (!GetFileInformationByHandle(hFile, &info))
        ThrowLastError();
    *pTimestamp = info.ftLastWriteTime;
    *pSize = info.nFileSizeLow;
    return;
}

#endif // !DACCESS_COMPILE
#endif // FEATURE_FUSION

