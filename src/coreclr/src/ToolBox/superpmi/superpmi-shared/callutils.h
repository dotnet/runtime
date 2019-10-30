//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
//----------------------------------------------------------
// CallUtils.h - Utility code for analyzing and working with managed calls
//----------------------------------------------------------
#ifndef _CallUtils
#define _CallUtils

#include "methodcontext.h"

enum CallType
{
    CallType_UserFunction = 0,
    CallType_Helper,
    CallType_Unknown = -1
};

class CallUtils
{
public:
    static CallType GetRecordedCallSiteInfo(MethodContext*            mc,
                                            CompileResult*            cr,
                                            unsigned int              callInstrOffset,
                                            /*out*/ CORINFO_SIG_INFO* outSigInfo,
                                            /*out*/ char**            outCallTargetSymbol);
    static CallType GetDirectCallSiteInfo(MethodContext*            mc,
                                          void*                     callTarget,
                                          /*out*/ CORINFO_SIG_INFO* outSigInfo,
                                          /*out*/ char**            outCallTargetSymbol);
    static bool HasRetBuffArg(MethodContext* mc, CORINFO_SIG_INFO args);
    static CorInfoHelpFunc GetHelperNum(CORINFO_METHOD_HANDLE method);
    static bool IsNativeMethod(CORINFO_METHOD_HANDLE method);
    static CORINFO_METHOD_HANDLE GetMethodHandleForNative(CORINFO_METHOD_HANDLE method);
    static const char* GetMethodName(MethodContext* mc, CORINFO_METHOD_HANDLE method, const char** classNamePtr);
    static const char* GetMethodFullName(MethodContext* mc, CORINFO_METHOD_HANDLE hnd, CORINFO_SIG_INFO sig);
};

#endif