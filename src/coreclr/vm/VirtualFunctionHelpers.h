// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
** Header: VirtualFunctionHelpers.h
**
**
===========================================================*/

#ifndef _VIRTUALFUNCTIONHELPERS_H
#define _VIRTUALFUNCTIONHELPERS_H

#include "qcall.h"
#include "corinfo.h"

extern "C" void * QCALLTYPE JIT_ResolveVirtualFunctionPointer(QCall::ObjectHandleOnStack obj, CORINFO_CLASS_HANDLE classHnd, CORINFO_METHOD_HANDLE methodHnd);

#endif //_VIRTUALFUNCTIONHELPERS_H
