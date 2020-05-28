/**
 * \file
 * System.Native PAL internal calls
 * Adapter code between the Mono runtime and the CoreFX Platform Abstraction Layer (PAL)
 * Copyright 2018 Microsoft
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
*/

#if !defined (HOST_WATCHOS) && !defined (HOST_TVOS) /* These platforms don't support async suspend and do not need this code for now */

#include <config.h>
#include <glib.h>
#include "mono/utils/mono-threads-api.h"
#include "mono/utils/atomic.h"
#include "mono/metadata/icall-internals.h"
#include "pal-icalls.h"


/*
 * mono_pal_init:
 *
 *	Initializes Mono's usage of the PAL (probably just by registering the necessary internal calls).
 *	This is called only from managed code, by any Interop.* classes that need to use the code here.
 *	The function may be called multiple times.
 *
 */
void
mono_pal_init (void)
{
	volatile static gboolean module_initialized = FALSE;
	if (mono_atomic_cas_i32 (&module_initialized, TRUE, FALSE) == FALSE) {
		mono_add_internal_call_with_flags ("Interop/Sys::Read", ves_icall_Interop_Sys_Read, TRUE);

#if defined(__APPLE__)
		mono_add_internal_call_with_flags ("Interop/RunLoop::CFRunLoopRun", ves_icall_Interop_RunLoop_CFRunLoopRun, TRUE);
#endif
	}

}

gint32
ves_icall_Interop_Sys_Read (intptr_t fd, gchar* buffer, gint32 count)
{
	gint32 result;
	MONO_ENTER_GC_SAFE;
	result = SystemNative_Read (fd, buffer, count);
	mono_marshal_set_last_error ();
	MONO_EXIT_GC_SAFE;
	return result;
}

#if defined(__APPLE__)

#include <CoreFoundation/CFRunLoop.h>

static void
interrupt_CFRunLoop (gpointer data)
{
	g_assert (data);
	CFRunLoopStop ((CFRunLoopRef)data);
}

void
ves_icall_Interop_RunLoop_CFRunLoopRun (void)
{
	gpointer runloop_ref = CFRunLoopGetCurrent ();
	gboolean interrupted;
	mono_thread_info_install_interrupt (interrupt_CFRunLoop, runloop_ref, &interrupted);

	if (interrupted)
		return;

	MONO_ENTER_GC_SAFE;
	CFRunLoopRun ();
	MONO_EXIT_GC_SAFE;

	mono_thread_info_uninstall_interrupt (&interrupted);
}

#endif

#endif /* !defined (HOST_WATCHOS) && !defined (HOST_TVOS) */
