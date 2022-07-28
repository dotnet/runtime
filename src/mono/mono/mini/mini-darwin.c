/**
 * \file
 * Darwin/MacOS support for Mono.
 *
 * Authors:
 *   Mono Team (mono-list@lists.ximian.com)
 *
 * Copyright 2001-2003 Ximian, Inc.
 * Copyright 2003-2011 Novell, Inc (http://www.novell.com)
 * Copyright 2011 Xamarin, Inc (http://www.xamarin.com)
 *
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */
#include <config.h>
#include <signal.h>
#ifdef HAVE_ALLOCA_H
#include <alloca.h>
#endif
#ifdef HAVE_UNISTD_H
#include <unistd.h>
#endif
#include <math.h>
#ifdef HAVE_SYS_TIME_H
#include <sys/time.h>
#endif

#include <mono/metadata/assembly.h>
#include <mono/metadata/loader.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/class.h>
#include <mono/metadata/object.h>
#include <mono/metadata/tokentype.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/threads.h>
#include <mono/metadata/appdomain.h>
#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/profiler-private.h>
#include <mono/metadata/mono-config.h>
#include <mono/metadata/environment.h>
#include <mono/metadata/mono-debug.h>
#include <mono/metadata/threads-types.h>
#include <mono/metadata/verify.h>
#include <mono/metadata/mempool-internals.h>
#include <mono/metadata/gc-internals.h>
#include <mono/utils/mono-math.h>
#include <mono/utils/mono-compiler.h>
#include <mono/utils/mono-logger-internals.h>
#include <mono/utils/mono-mmap.h>
#include <mono/utils/dtrace.h>

#include "mini.h"
#include "mini-runtime.h"
#include <string.h>
#include <ctype.h>
#include "trace.h"

#include "jit-icalls.h"

/* MacOS includes */
#include <mach/mach.h>
#include <mach/mach_error.h>
#include <mach/exception.h>
#include <mach/task.h>
#include <pthread.h>
#include <AvailabilityMacros.h>

void
mono_runtime_install_handlers (void)
{
	mono_runtime_posix_install_handlers ();

#if !defined (HOST_WATCHOS) && !defined (HOST_TVOS)
	/* LLDB installs task-wide Mach exception handlers. XNU dispatches Mach
	 * exceptions first to any registered "activation" handler and then to
	 * any registered task handler before dispatching the exception to a
	 * host-wide Mach exception handler that does translation to POSIX
	 * signals. This makes it impossible to use LLDB with an
	 * implicit-null-check-enabled Mono; continuing execution after LLDB
	 * traps an EXC_BAD_ACCESS will result in LLDB's EXC_BAD_ACCESS handler
	 * being invoked again. This also interferes with the translation of
	 * SIGFPEs to .NET-level ArithmeticExceptions. Work around this here by
	 * installing a no-op task-wide Mach exception handler for
	 * EXC_BAD_ACCESS and EXC_ARITHMETIC.
	 */
	kern_return_t kr = task_set_exception_ports (
		mach_task_self (),
		EXC_MASK_BAD_ACCESS | EXC_MASK_ARITHMETIC, /* SIGSEGV, SIGFPE */
		MACH_PORT_NULL,
		EXCEPTION_STATE_IDENTITY,
		MACHINE_THREAD_STATE);
	if (kr != KERN_SUCCESS)
		g_warning ("mono_runtime_install_handlers: task_set_exception_ports failed");
#endif
}

gboolean
mono_thread_state_init_from_handle (MonoThreadUnwindState *tctx, MonoThreadInfo *info, void *sigctx)
{
	kern_return_t ret;
	mach_msg_type_number_t num_state, num_fpstate;
	thread_state_t state, fpstate;

	g_assert (info);
	/*Zero enough state to make sure the caller doesn't confuse itself*/
	tctx->valid = FALSE;
	tctx->unwind_data [MONO_UNWIND_DATA_DOMAIN] = NULL;
	tctx->unwind_data [MONO_UNWIND_DATA_LMF] = NULL;
	tctx->unwind_data [MONO_UNWIND_DATA_JIT_TLS] = NULL;

	state = (thread_state_t) alloca (mono_mach_arch_get_thread_state_size ());
	fpstate = (thread_state_t) alloca (mono_mach_arch_get_thread_fpstate_size ());

	do {
		ret = mono_mach_arch_get_thread_states (info->native_handle, state, &num_state, fpstate, &num_fpstate);
	} while (ret == KERN_ABORTED);
	if (ret != KERN_SUCCESS)
		return FALSE;

	mono_mach_arch_thread_states_to_mono_context (state, fpstate, &tctx->ctx);

	/* mono_set_jit_tls () sets this */
	void *jit_tls = mono_thread_info_tls_get (info, TLS_KEY_JIT_TLS);
	/* SET_APPDOMAIN () sets this */
	void *domain = mono_thread_info_tls_get (info, TLS_KEY_DOMAIN);

	/*Thread already started to cleanup, can no longer capture unwind state*/
	if (!jit_tls || !domain)
		return FALSE;

	/*
	 * The current LMF address is kept in a separate TLS variable, and its hard to read its value without
	 * arch-specific code. But the address of the TLS variable is stored in another TLS variable which
	 * can be accessed through MonoThreadInfo.
	 */
	/* mono_set_lmf_addr () sets this */
	MonoLMF *lmf = NULL;
	MonoLMF **addr = (MonoLMF**)mono_thread_info_tls_get (info, TLS_KEY_LMF_ADDR);
	if (addr)
		lmf = *addr;

	tctx->unwind_data [MONO_UNWIND_DATA_DOMAIN] = domain;
	tctx->unwind_data [MONO_UNWIND_DATA_JIT_TLS] = jit_tls;
	tctx->unwind_data [MONO_UNWIND_DATA_LMF] = lmf;
	tctx->valid = TRUE;

	return TRUE;
}
