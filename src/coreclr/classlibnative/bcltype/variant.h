// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
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
public:
    static FCDECL1(CLR_BOOL, IsSystemDrawingColor, MethodTable* valMT);
};

extern "C" uint32_t QCALLTYPE Variant_ConvertSystemColorToOleColor(QCall::ObjectHandleOnStack obj);

#endif // _VARIANT_H_

