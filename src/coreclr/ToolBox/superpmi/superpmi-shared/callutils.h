// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
    static CorInfoHelpFunc GetHelperNum(CORINFO_METHOD_HANDLE method);
    static bool IsNativeMethod(CORINFO_METHOD_HANDLE method);
    static CORINFO_METHOD_HANDLE GetMethodHandleForNative(CORINFO_METHOD_HANDLE method);
    static const char* GetMethodName(MethodContext* mc, CORINFO_METHOD_HANDLE method, const char** classNamePtr);
    static const char* GetMethodFullName(MethodContext* mc, CORINFO_METHOD_HANDLE hnd, CORINFO_SIG_INFO sig, bool ignoreMethodName = false);
};

#endif
