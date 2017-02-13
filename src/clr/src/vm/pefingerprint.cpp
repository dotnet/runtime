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

    return;
}


