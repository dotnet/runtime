// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: OAVariant.h
//

//
// Purpose: Wrapper for Ole Automation compatible math ops.
// Calls through to OleAut.dll
//

//

#ifndef _OAVARIANT_H_
#define _OAVARIANT_H_

#ifndef FEATURE_COMINTEROP
#error FEATURE_COMINTEROP is required for this file
#endif // FEATURE_COMINTEROP

#include "variant.h"

class COMOAVariant
{
public:

    // Utility Functions
    // Conversion between COM+ variant type field & OleAut Variant enumeration
    // WinCE doesn't support Variants entirely.
    static VARENUM CVtoVT(const CVTypes cv);
    static CVTypes VTtoCV(const VARENUM vt);

    // Conversion between COM+ Variant & OleAut Variant.  ToOAVariant
    // returns true if the conversion process allocated an object (like a BSTR).
    static bool ToOAVariant(const VariantData * const var, VARIANT * oa);
    static void FromOAVariant(const VARIANT * const oa, VariantData * const& var);

    // Throw a specific exception for a failure, specified by a given HRESULT.
    static void OAFailed(const HRESULT hr);

    static FCDECL6(void, ChangeTypeEx, VariantData *result, VariantData *op, LCID lcid, void *targetType, int cvType, INT16 flags);
};


#endif  // _OAVARIANT_H_
