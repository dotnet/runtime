// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: Variant.cpp
//

//
// Purpose: Native Implementation of the Variant Class
//

//

#include "common.h"

#ifdef FEATURE_COMINTEROP

#include "object.h"
#include "excep.h"
#include "frames.h"
#include "vars.hpp"
#include "variant.h"
#include "string.h"

FCIMPL1(CLR_BOOL, COMVariant::IsSystemDrawingColor, MethodTable* valMT)
{
    FCALL_CONTRACT;

    return IsTypeRefOrDef(g_ColorClassName, valMT->GetModule(), valMT->GetCl());
}
FCIMPLEND

extern "C" uint32_t QCALLTYPE Variant_ConvertSystemColorToOleColor(QCall::ObjectHandleOnStack obj)
{
    QCALL_CONTRACT;

    uint32_t ret = 0;

    BEGIN_QCALL;

    GCX_COOP();
    OBJECTREF srcObj = obj.Get();
    ret = ConvertSystemColorToOleColor(&srcObj);

    END_QCALL;

    return ret;
}

#endif // FEATURE_COMINTEROP
