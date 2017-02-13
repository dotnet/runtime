// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// --------------------------------------------------------------------------------
// PEFingerprint.h
// 

// --------------------------------------------------------------------------------

#ifndef PEFINGERPRINT_H_
#define PEFINGERPRINT_H_




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
