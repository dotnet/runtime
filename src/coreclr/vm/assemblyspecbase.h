// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ============================================================
//
// AssemblySpecBase.h
//


//
// Chooses the appropriate implementation to base AssemblySpec on
//
// ============================================================


#ifndef __ASSEMBLY_SPEC_BASE_H__
#define __ASSEMBLY_SPEC_BASE_H__

#include "../binder/inc/assemblyname.hpp"
#include "assemblybinder.h"

#include "../binder/inc/defaultassemblybinder.h"

#if !defined(DACCESS_COMPILE)
#include "../binder/inc/customassemblybinder.h"
#endif // !defined(DACCESS_COMPILE)

#include "baseassemblyspec.h"
#include "baseassemblyspec.inl"

#endif // __ASSEMBLY_SPEC_BASE_H__
