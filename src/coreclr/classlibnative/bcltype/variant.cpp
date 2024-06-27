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
#include "vars.hpp"
#include "variant.h"

extern "C" uint32_t QCALLTYPE Variant_ConvertSystemColorToOleColor(QCall::ObjectHandleOnStack obj)
{
    QCALL_CONTRACT;

    uint32_t ret = 0;

    BEGIN_QCALL;

    GCX_COOP();
    OBJECTREF srcObj = obj.Get();

    GCPROTECT_BEGIN(srcObj);
    ret = ConvertSystemColorToOleColor(&srcObj);
    GCPROTECT_END();

    END_QCALL;

    return ret;
}

extern "C" void QCALLTYPE Variant_ConvertOleColorToSystemColor(QCall::ObjectHandleOnStack objRet, uint32_t oleColor, MethodTable* pMT)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    GCX_COOP();
    SYSTEMCOLOR systemColor{};
    ConvertOleColorToSystemColor(oleColor, &systemColor);
    objRet.Set(pMT->Box(&systemColor));

    END_QCALL;
}

extern "C" void QCALLTYPE Variant_ConvertValueTypeToRecord(QCall::ObjectHandleOnStack obj, VARIANT * pOle)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;
    GCX_COOP();

    OBJECTREF objRef = obj.Get();
    GCPROTECT_BEGIN(objRef);
    V_VT(pOle) = VT_RECORD;
    OleVariant::ConvertValueClassToVariant(&objRef, pOle);
    GCPROTECT_END();

    END_QCALL;
}
#endif // FEATURE_COMINTEROP
