// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <config.h>
#include <glib.h>
#include <dn-rt-mono.h>

void
dn_rt_mono_failfast_msgv (const char *format, va_list ap)
{
	g_logv (G_LOG_DOMAIN, G_LOG_LEVEL_ERROR, format, ap);
	g_assert_not_reached ();
}

void
dn_rt_mono_failfast_nomsg (const char *file, int line)
{
	mono_assertion_message_disabled (file, line);
}
