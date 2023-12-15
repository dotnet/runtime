// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// This header contains the definition of an interface between the GC/HandleTable portions of the Redhawk
// codebase and the regular Redhawk code.

#ifndef __RedhawkGCInterface_h__
#define __RedhawkGCInterface_h__

#include "forward_declarations.h"


class MethodInfo;
struct REGDISPLAY;
enum GCRefKind : unsigned char;
class ICodeManager;


class RedhawkGCInterface
{
public:
    static void EnumGcRef(PTR_OBJECTREF pRef, GCRefKind kind, ScanFunc* pfnEnumCallback, ScanContext* pvCallbackData);
    static void EnumGcRefConservatively(PTR_OBJECTREF pRef, ScanFunc* pfnEnumCallback, ScanContext* pvCallbackData);

    static void EnumGcRefs(ICodeManager * pCodeManager,
                           MethodInfo * pMethodInfo,
                           PTR_VOID safePointAddress,
                           REGDISPLAY * pRegisterSet,
                           ScanFunc* pfnEnumCallback,
                           ScanContext* pvCallbackData,
                           bool   isActiveStackFrame);

    static void EnumGcRefsInRegionConservatively(PTR_OBJECTREF pLowerBound,
                                                 PTR_OBJECTREF pUpperBound,
                                                 ScanFunc* pfnEnumCallback,
                                                 ScanContext* pvCallbackData);

};

#endif // __RedhawkGCInterface_h__
