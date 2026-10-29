// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "ecall-table.h"
#include "ecall-descriptor.h"

#define FCFuncElement(name, impl) extern "C" const ECFunc ECALL_SYMBOL;
#define FCFuncElementSig(name, sig, impl) FCFuncElement(name, impl)
#define FCDynamic(name, id) FCFuncElement(name, nullptr)
#define FCDynamicSig(name, sig, id) FCFuncElement(name, nullptr)
#include "ecalllist.h"

#define FCFuncStart(name) static const ECFunc* const name[] = {
#define FCFuncEnd() nullptr };
#define FCFuncElement(name, impl) &ECALL_SYMBOL,
#define FCFuncElementSig(name, sig, impl) FCFuncElement(name, impl)
#define FCDynamic(name, id) FCFuncElement(name, nullptr)
#define FCDynamicSig(name, sig, id) FCFuncElement(name, nullptr)
#include "ecalllist.h"

extern "C"
{
    const ECClass c_rgECClasses[] = {
#define FCClassElement(name, ns, funcs) { name, ns, funcs },
#include "ecalllist.h"
    };

    const int c_nECClasses = sizeof(c_rgECClasses) / sizeof(c_rgECClasses[0]);
}
