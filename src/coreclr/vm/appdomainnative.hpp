// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.



/*============================================================
**
** Header:  AppDomainNative.hpp
**
** Purpose: Implements native methods for AppDomains
**
**
===========================================================*/
#ifndef _APPDOMAINNATIVE_H
#define _APPDOMAINNATIVE_H

#include "qcall.h"

class AppDomainNative
{
public:
    static void QCALLTYPE CreateDynamicAssembly(QCall::ObjectHandleOnStack assemblyName, QCall::StackCrawlMarkHandle stackMark, INT32 access, QCall::ObjectHandleOnStack assemblyLoadContext, QCall::ObjectHandleOnStack retAssembly);
    static FCDECL0(Object*, GetLoadedAssemblies);
    static FCDECL1(Object*, GetOrInternString, StringObject* pStringUNSAFE);
    static FCDECL1(Object*, IsStringInterned, StringObject* pString);
};

#endif
