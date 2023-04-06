// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __DN_RT_H__
#define __DN_RT_H__

#include <stdarg.h>

#include "dn-compiler.h"

DN_NORETURN static void
dn_rt_failfast_msgv(const char* fmt, va_list ap);

DN_NORETURN static void
dn_rt_failfast_nomsg(const char* file, int line);

#if defined(FEATURE_CORECLR)
#include "pal.h"
// TODO: add CoreCLR runtime impl
DN_NORETURN static inline void
dn_rt_failfast_msgv(const char* fmt, va_list ap)
{
	RaiseFailFastException(nullptr, nullptr, 0);
}

DN_NORETURN static inline void
dn_rt_failfast_nomsg(const char* file, int line)
{
	RaiseFailFastException(nullptr, nullptr, 0);
}

#elif defined(FEATURE_NATIVEAOT)
#include "common.h"
#include "gcenv.h"
#include "CommonTypes.h"
#include "CommonMacros.h"
#include "PalRedhawkCommon.h"
#include "PalRedhawk.h"
#include "rhassert.h"
// TODO: add NativeAOT runtime impl
DN_NORETURN static inline void
dn_rt_failfast_msgv(const char* fmt, va_list ap)
{
	RhFailFast();
	UNREACHABLE();
}

DN_NORETURN static inline void
dn_rt_failfast_nomsg(const char* file, int line)
{
	RhFailFast();
	UNREACHABLE();
}
#else
// Mono
#include "dn-rt-mono.h"
#endif

#endif /* __DN_RT_H__ */
