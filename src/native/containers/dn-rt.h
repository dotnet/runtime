// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __DN_RT_H__
#define __DN_RT_H__

#include <stdarg.h>

#include <dn-compiler.h>

DN_NORETURN static void
dn_rt_failfast_msgv(const char* fmt, va_list ap);

DN_NORETURN static void
dn_rt_failfast_nomsg(const char* file, int line);

#if defined(FEATUERE_CORECLR)
// TODO: add CoreCLR runtime impl
#elif defined(FEATURE_NATIVEAOT)
// TODO: add NativeAOT runtime impl
#else
// Mono
#include <dn-rt-mono.h>
#endif

#endif /* __DN_RT_H__ */
