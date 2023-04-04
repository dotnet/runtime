// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __DN_RT_H__
#define __DN_RT_H__

#include <stdarg.h>

#include <dn-compiler.h>

DN_NORETURN static void
dn_rt_failfast_msgv(const char* fmt, va_list ap);

DN_NORETURN static void
dn_rt_failfast_msg(const char* file, int line);

#endif /* __DN_RT_H__ */
