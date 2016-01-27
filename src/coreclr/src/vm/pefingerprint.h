// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// --------------------------------------------------------------------------------
// PEFingerprint.h
// 

// --------------------------------------------------------------------------------

#ifndef PEFINGERPRINT_H_
#define PEFINGERPRINT_H_


#ifdef FEATURE_FUSION

#include "corcompile.h"

class PEImage;

//==================================================================================
// This is the implementation of IILFingerprint object maintained by PEImage objects.
// IILFingerprint is described in detail in IILFingerprint.h
//==================================================================================
class PEFingerprint : public IILFingerprint
{
  public:
  //----------------------------------------------------------------
  // IILFingerprint methods
  //----------------------------------------------------------------
    STDMETHOD_(ULONG, AddRef)();
    STDMETHOD_(ULONG, Release)();
    STDMETHOD_(BOOL, CommitAndCompare)(ILFingerprintTag componentType, LPCVOID data);
    STDMETHOD_(BOOL, CommitAndCompareMulti)(UINT numComponents, const ILFingerprintComponent *pComponents);
    STDMETHOD_(void, LockAndLoadIL)();

  //----------------------------------------------------------------
  // Non-interface public methods.
  //----------------------------------------------------------------
  public:
    static PEFingerprint* PEFingerprint::CreatePEFingerprint(PEImage *owner);
    virtual ~PEFingerprint();

  private:
    PEFingerprint(PEImage *owner);

  //----------------------------------------------------------------
  // Private methods.
  //----------------------------------------------------------------
  private:

    BOOL IsComponentCommitted(ILFingerprintTag tag)
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(tag < ILFTagCount);
        return 0 != (m_commitMask & (1 << tag));
    }

    void SetComponentCommitted(ILFingerprintTag tag)
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(tag < ILFTagCount);
        m_commitMask |= (1 << tag);
    }

    LPVOID TagDataStart(ILFingerprintTag tag)
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(tag < ILFTagCount);
        return (LPVOID)(((LPBYTE)this) + s_offsets[tag]);
    }

    DWORD TagDataSize(ILFingerprintTag tag)
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(tag < ILFTagCount);
        return s_sizes[tag];
    }


  //----------------------------------------------------------------
  // Private instance data
  //----------------------------------------------------------------
  private:
    Crst                            *m_pcrst;                              // Synchronizes updates to fingerprint
    PEImage                         *m_peimage;                            // Backpointer to PEImage (for ref-counting purposes, the PEImage and PEFingerprint have the same identity)
    DWORD                            m_commitMask;                         // Bitmask to indicate which components have been committed ( fCommitted =  (m_commitMask & (1 << tag)) )
    FILETIME                         m_timeStamp;                          // Component: File system lastwrite Timestamp
    DWORD                            m_size;                               // Component: File size
    GUID                             m_mvid;                               // Component: Mvid

    BOOL                             m_alreadyLoaded;                      // Turns repeated attempts to LockAndLoadIL() into NOP's
    HRESULT                          m_priorLockAndLoadFailure;            // If LockAndLoadIL() failed the first time, return the same failure on subsequent attempts.

  //----------------------------------------------------------------
  // Private static data
  //----------------------------------------------------------------
  private:
    const static size_t              s_offsets[ILFTagCount];               // static: Maps tags to offsets within PEFingerprint
    const static DWORD               s_sizes[ILFTagCount];                 // static: Maps tag to expected data size
};

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
class PEFingerprintVerificationHolder
{
  public:
    PEFingerprintVerificationHolder(PEImage *owner);

  private:
    FileHandleHolder m_fileHandle;
};


#endif //PEFINGERPRINT
