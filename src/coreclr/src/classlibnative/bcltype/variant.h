// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// File: Variant.h
//

//
// Purpose: Headers for the Variant class.
//

//

#ifndef _VARIANT_H_
#define _VARIANT_H_

#ifndef FEATURE_COMINTEROP
#error FEATURE_COMINTEROP is required for this file
#endif // FEATURE_COMINTEROP

#include <cor.h>
#include "fcall.h"
#include "olevariant.h"

class COMVariant
{
    friend class OleVariant;

public:
    //
    // Helper Routines
    //

    static FCDECL2(void, SetFieldsObject, VariantData* vThisRef, Object* vVal);
    static FCDECL1(Object*, BoxEnum, VariantData* var);

private:
    // GetCVTypeFromClass
    // This method will return the CVTypes from the Variant instance
    static CVTypes GetCVTypeFromClass(TypeHandle th);
    static int GetEnumFlags(TypeHandle th);
};

#endif // _VARIANT_H_

