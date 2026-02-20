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

extern "C" void QCALLTYPE String_Intern(QCall::StringHandleOnStack str);
extern "C" void QCALLTYPE String_IsInterned(QCall::StringHandleOnStack str);

extern "C" void QCALLTYPE AppDomain_CreateDynamicAssembly(QCall::ObjectHandleOnStack assemblyLoadContext, NativeAssemblyNameParts* pAssemblyName, INT32 hashAlgorithm, INT32 access, QCall::ObjectHandleOnStack retAssembly);

extern "C" void QCALLTYPE AssemblyNative_GetLoadedAssemblies(QCall::ObjectHandleOnStack retAssemblies);

#endif
