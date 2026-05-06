// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __GcEnum_h__
#define __GcEnum_h__

#include "forward_declarations.h"

struct REGDISPLAY;
class ICodeManager;
enum GCRefKind : unsigned char;

void EnumGcRef(PTR_OBJECTREF pRef, GCRefKind kind, ScanFunc* pfnEnumCallback, ScanContext* pvCallbackData);
void EnumGcRefConservatively(PTR_OBJECTREF pRef, ScanFunc* pfnEnumCallback, ScanContext* pvCallbackData);

void EnumGcRefs(ICodeManager * pCodeManager,
                 MethodInfo * pMethodInfo,
                 PTR_VOID safePointAddress,
                 REGDISPLAY * pRegisterSet,
                 ScanFunc* pfnEnumCallback,
                 ScanContext* pvCallbackData,
                 bool   isActiveStackFrame);

void EnumGcRefsInRegionConservatively(PTR_OBJECTREF pLowerBound,
                                         PTR_OBJECTREF pUpperBound,
                                         ScanFunc* pfnEnumCallback,
                                         ScanContext* pvCallbackData);

#endif // __GcEnum_h__
