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
#include "olevariant.h"

extern "C" uint32_t QCALLTYPE Variant_ConvertSystemColorToOleColor(QCall::ObjectHandleOnStack obj);
extern "C" void QCALLTYPE Variant_ConvertOleColorToSystemColor(QCall::ObjectHandleOnStack objRet, uint32_t oleColor, MethodTable* pMT);
extern "C" void QCALLTYPE Variant_ConvertValueTypeToRecord(QCall::ObjectHandleOnStack obj, VARIANT* pOle);

#endif // _VARIANT_H_

