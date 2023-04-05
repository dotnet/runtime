// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __DN_RT_MONO_H__
#define __DN_RT_MONO_H__

#include <dn-rt.h>

DN_NORETURN void
dn_rt_mono_failfast_msgv (const char* fmt, va_list ap);

DN_NORETURN void
dn_rt_mono_failfast_nomsg (const char* file, int line);

DN_NORETURN static inline void
dn_rt_failfast_msgv(const char* fmt, va_list ap)
{
	dn_rt_mono_failfast_msgv (fmt, ap);
}

DN_NORETURN static inline void
dn_rt_failfast_nomsg(const char* fmt, int line)
{
	dn_rt_mono_failfast_nomsg(fmt, line);
}

#endif /* __DN_RT_MONO_H__ */
