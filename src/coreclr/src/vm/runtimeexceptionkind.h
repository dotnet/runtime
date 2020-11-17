// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// RuntimeExceptionKind.h
//

#ifndef __runtimeexceptionkind_h__
#define __runtimeexceptionkind_h__

//==========================================================================
// Identifies commonly-used exception classes for COMPlusThrowable().
//==========================================================================
enum RuntimeExceptionKind {
#define DEFINE_EXCEPTION(ns, reKind, bHRformessage, ...) k##reKind,
#include "rexcep.h"
kLastException
};

#endif  // __runtimeexceptionkind_h__
