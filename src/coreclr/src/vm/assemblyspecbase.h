//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
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

#ifndef FEATURE_FUSION
#include "coreclr/corebindresult.h"
#include "coreclr/corebindresult.inl"
#include "../binder/inc/assembly.hpp"
#endif // FEATURE_FUSION

#include "baseassemblyspec.h"
#include "baseassemblyspec.inl"

#endif // __ASSEMBLY_SPEC_BASE_H__
