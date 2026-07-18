// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef HOST_WINDOWS
#include "../pal/inc/pal.h"
#include "../pal/inc/rt/ntimage.h"
#endif // HOST_WINDOWS
#include "common.h"

#include <stdint.h>
#include <stddef.h>

#include <sospriv.h>
#include "cdacplatformmetadata.hpp"
#include "interoplibinterface_comwrappers.h"
#include "comcallablewrapper.h"
#ifdef FEATURE_COMINTEROP
#include "runtimecallablewrapper.h"
#endif // FEATURE_COMINTEROP
#include "methodtable.h"
#include "threads.h"
#include "vars.hpp"
#include "exinfo.h"

#include "configure.h"

#ifdef FEATURE_INTERPRETER
#include "interpexec.h"
#endif // FEATURE_INTERPRETER

#include "virtualcallstub.h"
#include "../debug/ee/debugger.h"
#include "patchpointinfo.h"

#ifdef HAVE_GCCOVER
#include "gccover.h"
#endif // HAVE_GCCOVER

struct GlobalStubData;

template<>
struct cdac_data<GlobalStubData>
{
    inline static TADDR ThePreStub = GetEEFuncEntryPoint(::ThePreStub);
    inline static TADDR VarargPInvokeStub = GetEEFuncEntryPoint(::VarargPInvokeStub);
#if !defined(TARGET_X86) && !defined(TARGET_ARM64) && !defined(TARGET_LOONGARCH64) && !defined(TARGET_RISCV64)
    inline static TADDR VarargPInvokeStub_RetBuffArg = GetEEFuncEntryPoint(::VarargPInvokeStub_RetBuffArg);
#endif
    inline static TADDR GenericPInvokeCalliHelper = GetEEFuncEntryPoint(::GenericPInvokeCalliHelper);
#if defined(TARGET_X86) && !defined(UNIX_X86_ABI)
    inline static TADDR JIT_TailCall = GetEEFuncEntryPoint(::JIT_TailCall);
#endif
};
