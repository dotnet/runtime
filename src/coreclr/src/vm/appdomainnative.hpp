// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.



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
    static FCDECL3(Object*, CreateDynamicAssembly, AssemblyNameBaseObject* assemblyNameUNSAFE, StackCrawlMark* stackMark, INT32 access);
    static FCDECL0(Object*, GetLoadedAssemblies);
    static FCDECL1(Object*, GetOrInternString, StringObject* pStringUNSAFE);
    static FCDECL1(Object*, IsStringInterned, StringObject* pString);

#ifdef FEATURE_APPX
    static INT32 QCALLTYPE IsAppXProcess();
#endif
};

#endif
