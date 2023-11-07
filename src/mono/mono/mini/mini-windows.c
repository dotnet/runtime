/**
 * \file
 * POSIX signal handling support for Mono.
 *
 * Authors:
 *   Mono Team (mono-list@lists.ximian.com)
 *
 * Copyright 2001-2003 Ximian, Inc.
 * Copyright 2003-2008 Ximian, Inc.
 *
 * See LICENSE for licensing information.
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */
#include <config.h>
#include <signal.h>
#include <math.h>
#include <conio.h>
#include <assert.h>

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
#include <mono/metadata/gc-internals.h>
#include <mono/metadata/threads-types.h>
#include <mono/metadata/verify.h>
#include <mono/metadata/mempool-internals.h>
#include <mono/utils/mono-math.h>
#include <mono/utils/mono-compiler.h>
#include <mono/utils/mono-counters.h>
#include <mono/utils/mono-logger-internals.h>
#include <mono/utils/mono-mmap.h>
#include <mono/utils/dtrace.h>
#include <mono/utils/mono-context.h>
#include <mono/utils/w32subset.h>

#include "mini.h"
#include "mini-runtime.h"
#include "mini-windows.h"
#include <string.h>
#include <ctype.h>
#include "trace.h"

#include "jit-icalls.h"

#define MONO_HANDLER_DELIMITER ','

#define MONO_HANDLER_ATEXIT_WAIT_KEYPRESS "atexit-waitkeypress"
#define MONO_HANDLER_ATEXIT_WAIT_KEYPRESS_LEN STRING_LENGTH(MONO_HANDLER_ATEXIT_WAIT_KEYPRESS)

// Typedefs used to setup handler table.
typedef void (*handler)(void);

typedef struct {
	const char * cmd;
	const int cmd_len;
	handler handler;
} HandlerItem;

#if HAVE_API_SUPPORT_WIN32_CONSOLE
/**
* atexit_wait_keypress:
*
* This function is installed as an atexit function making sure that the console is not terminated before the end user has a chance to read the result.
* This can be handy in debug scenarios (running from within the debugger) since an exit of the process will close the console window
* without giving the end user a chance to look at the output before closed.
*/
static void
atexit_wait_keypress (void)
{

	fflush (stdin);

	printf ("Press any key to continue . . . ");
	fflush (stdout);

	_getch ();

	return;
}

/**
* install_atexit_wait_keypress:
*
* This function installs the wait keypress exit handler.
*/
static void
install_atexit_wait_keypress (void)
{
	atexit (atexit_wait_keypress);
	return;
}

#else

/**
* install_atexit_wait_keypress:
*
* Not supported on WINAPI family.
*/
static void
install_atexit_wait_keypress (void)
{
	return;
}

#endif /* HAVE_API_SUPPORT_WIN32_CONSOLE */

// Table describing handlers that can be installed at process startup. Adding a new handler can be done by adding a new item to the table together with an install handler function.
const HandlerItem g_handler_items[] = { { MONO_HANDLER_ATEXIT_WAIT_KEYPRESS, MONO_HANDLER_ATEXIT_WAIT_KEYPRESS_LEN, install_atexit_wait_keypress },
					{ NULL, 0, NULL } };

/**
 * get_handler_arg_len:
 * @handlers: Get length of next handler.
 *
 * This function calculates the length of next handler included in argument.
 *
 * Returns: The length of next handler, if available.
 */
static size_t
get_next_handler_arg_len (const char *handlers)
{
	assert (handlers != NULL);

	size_t current_len = 0;
	const char *handler = strchr (handlers, MONO_HANDLER_DELIMITER);
	if (handler != NULL) {
		// Get length of next handler arg.
		current_len = (handler - handlers);
	} else {
		// Consume rest as length of next handler arg.
		current_len = strlen (handlers);
	}

	return current_len;
}

/**
 * install_custom_handler:
 * @handlers: Handlers included in --handler argument, example "atexit-waitkeypress,someothercmd,yetanothercmd".
 * @handler_arg_len: Output, length of consumed handler.
 *
 * This function installs the next handler included in @handlers parameter.
 *
 * Returns: TRUE on successful install, FALSE on failure or unrecognized handler.
 */
static gboolean
install_custom_handler (const char *handlers, size_t *handler_arg_len)
{
	gboolean result = FALSE;

	assert (handlers != NULL);
	assert (handler_arg_len);

	*handler_arg_len = get_next_handler_arg_len (handlers);
	for (int current_item = 0; current_item < G_N_ELEMENTS (g_handler_items); ++current_item) {
		const HandlerItem * handler_item = &g_handler_items [current_item];

		if (handler_item->cmd == NULL)
			continue;

		if (*handler_arg_len == handler_item->cmd_len && strncmp (handlers, handler_item->cmd, *handler_arg_len) == 0) {
			assert (handler_item->handler != NULL);
			handler_item->handler ();
			result = TRUE;
			break;
		}
	}
	return result;
}

void
mono_runtime_install_handlers (void)
{
#ifndef MONO_CROSS_COMPILE
	win32_seh_init();
	win32_seh_set_handler(SIGFPE, mono_sigfpe_signal_handler);
	win32_seh_set_handler(SIGILL, mono_crashing_signal_handler);
	win32_seh_set_handler(SIGSEGV, mono_sigsegv_signal_handler);
	if (mini_debug_options.handle_sigint)
		win32_seh_set_handler(SIGINT, mono_sigint_signal_handler);
#endif
}

gboolean
mono_runtime_install_custom_handlers (const char *handlers)
{
	gboolean result = FALSE;

	assert (handlers != NULL);
	while (*handlers != '\0') {
		size_t handler_arg_len = 0;

		result = install_custom_handler (handlers, &handler_arg_len);
		handlers += handler_arg_len;

		if (*handlers == MONO_HANDLER_DELIMITER)
			handlers++;
		if (!result)
			break;
	}

	return result;
}

void
mono_runtime_install_custom_handlers_usage (void)
{
	fprintf (stdout,
		 "Custom Handlers:\n"
		 "   --handlers=HANDLERS            Enable handler support, HANDLERS is a comma\n"
		 "                                  separated list of available handlers to install.\n"
		 "\n"
#if HAVE_API_SUPPORT_WIN32_CONSOLE
		 "HANDLERS is composed of:\n"
		 "    atexit-waitkeypress           Install an atexit handler waiting for a keypress\n"
		 "                                  before exiting process.\n");
#else
		 "No handlers supported on current platform.\n");
#endif /* HAVE_API_SUPPORT_WIN32_CONSOLE */
}

void
mono_init_native_crash_info (void)
{
	return;
}

/* mono_chain_signal:
 *
 *   Call the original signal handler for the signal given by the arguments, which
 * should be the same as for a signal handler. Returns TRUE if the original handler
 * was called, false otherwise.
 */
gboolean
MONO_SIG_HANDLER_SIGNATURE (mono_chain_signal)
{
	/* Set to FALSE to indicate that vectored exception handling should continue to look for handler */
	MONO_SIG_HANDLER_GET_INFO ()->handled = FALSE;
	return TRUE;
}

#if !HAVE_EXTERN_DEFINED_NATIVE_CRASH_HANDLER
#ifndef MONO_CROSS_COMPILE
void
mono_dump_native_crash_info (const char *signal, MonoContext *mctx, MONO_SIG_HANDLER_INFO_TYPE *info)
{
	//TBD
}

void
mono_post_native_crash_handler (const char *signal, MonoContext *mctx, MONO_SIG_HANDLER_INFO_TYPE *info, gboolean crash_chaining)
{
	if (!crash_chaining)
		abort ();
}
#endif /* !MONO_CROSS_COMPILE */
#endif /* !HAVE_EXTERN_DEFINED_NATIVE_CRASH_HANDLER */

#if HAVE_API_SUPPORT_WIN32_TIMERS
#include <mmsystem.h>
static MMRESULT g_timer_event = 0;
static HANDLE g_timer_main_thread = INVALID_HANDLE_VALUE;

static VOID
thread_timer_expired (HANDLE thread)
{
	CONTEXT context;

	context.ContextFlags = CONTEXT_CONTROL;
	if (GetThreadContext (thread, &context)) {
		guchar *ip;

#ifdef _ARM64_
		ip = (guchar *) context.Pc;
#elif _WIN64
		ip = (guchar *) context.Rip;
#else
		ip = (guchar *) context.Eip;
#endif

		MONO_PROFILER_RAISE (sample_hit, (ip, &context));
	}
}

static VOID CALLBACK
timer_event_proc (UINT uID, UINT uMsg, DWORD_PTR dwUser, DWORD_PTR dw1, DWORD_PTR dw2)
{
	thread_timer_expired ((HANDLE)dwUser);
}

static VOID
start_profiler_timer_event (void)
{
	g_return_if_fail (g_timer_main_thread == INVALID_HANDLE_VALUE && g_timer_event == 0);

	TIMECAPS timecaps;

	if (timeGetDevCaps (&timecaps, sizeof (timecaps)) != TIMERR_NOERROR)
		return;

	g_timer_main_thread = OpenThread (READ_CONTROL | THREAD_GET_CONTEXT, FALSE, GetCurrentThreadId ());
	if (g_timer_main_thread == NULL)
		return;

	if (timeBeginPeriod (1) != TIMERR_NOERROR)
		return;

	g_timer_event = timeSetEvent (1, 0, (LPTIMECALLBACK)timer_event_proc, (DWORD_PTR)g_timer_main_thread, TIME_PERIODIC | TIME_KILL_SYNCHRONOUS);
	if (g_timer_event == 0) {
		timeEndPeriod (1);
		return;
	}
}

void
mono_runtime_setup_stat_profiler (void)
{
	start_profiler_timer_event ();
	return;
}
#elif !HAVE_EXTERN_DEFINED_WIN32_TIMERS
void
mono_runtime_setup_stat_profiler (void)
{
	g_unsupported_api ("timeGetDevCaps, timeBeginPeriod, timeEndPeriod, timeSetEvent, timeKillEvent");
	SetLastError (ERROR_NOT_SUPPORTED);
	return;
}
#endif /* HAVE_API_SUPPORT_WIN32_TIMERS */

#if HAVE_API_SUPPORT_WIN32_OPEN_THREAD
gboolean
mono_setup_thread_context(DWORD thread_id, MonoContext *mono_context)
{
	HANDLE handle;
#if defined(MONO_HAVE_SIMD_REG_AVX) && HAVE_API_SUPPORT_WIN32_CONTEXT_XSTATE
	BYTE context_buffer [2048];
	DWORD context_buffer_len = G_N_ELEMENTS (context_buffer);
	PCONTEXT context = NULL;
	BOOL success = InitializeContext (context_buffer, CONTEXT_INTEGER | CONTEXT_FLOATING_POINT | CONTEXT_CONTROL | CONTEXT_XSTATE, &context, &context_buffer_len);
	success &= SetXStateFeaturesMask (context, XSTATE_MASK_AVX);
	g_assert (success == TRUE);
#else
	CONTEXT context_buffer;
	PCONTEXT context = &context_buffer;
	context->ContextFlags = CONTEXT_INTEGER | CONTEXT_FLOATING_POINT | CONTEXT_CONTROL;
#endif

	g_assert (thread_id != GetCurrentThreadId ());

	handle = OpenThread (THREAD_ALL_ACCESS, FALSE, thread_id);
	g_assert (handle);

	if (!GetThreadContext (handle, context)) {
		CloseHandle (handle);
		return FALSE;
	}

	memset (mono_context, 0, sizeof (MonoContext));
	mono_sigctx_to_monoctx (context, mono_context);

	CloseHandle (handle);
	return TRUE;
}
#elif !HAVE_EXTERN_DEFINED_WIN32_OPEN_THREAD
gboolean
mono_setup_thread_context (DWORD thread_id, MonoContext *mono_context)
{
	g_unsupported_api ("OpenThread");
	SetLastError (ERROR_NOT_SUPPORTED);
	return FALSE;
}
#endif /* HAVE_API_SUPPORT_WIN32_OPEN_THREAD */

gboolean
mono_thread_state_init_from_handle (MonoThreadUnwindState *tctx, MonoThreadInfo *info, void *sigctx)
{
	tctx->valid = FALSE;
	tctx->unwind_data [MONO_UNWIND_DATA_DOMAIN] = NULL;
	tctx->unwind_data [MONO_UNWIND_DATA_LMF] = NULL;
	tctx->unwind_data [MONO_UNWIND_DATA_JIT_TLS] = NULL;

	if (sigctx == NULL) {
		DWORD id = mono_thread_info_get_tid (info);
		mono_setup_thread_context (id, &tctx->ctx);
	} else {
#ifdef ENABLE_CHECKED_BUILD
		g_assert (((CONTEXT *)sigctx)->ContextFlags & CONTEXT_INTEGER);
		g_assert (((CONTEXT *)sigctx)->ContextFlags & CONTEXT_CONTROL);
		g_assert (((CONTEXT *)sigctx)->ContextFlags & CONTEXT_FLOATING_POINT);
#if defined(MONO_HAVE_SIMD_REG_AVX) && HAVE_API_SUPPORT_WIN32_CONTEXT_XSTATE
		DWORD64 features = 0;
		g_assert (((CONTEXT *)sigctx)->ContextFlags & CONTEXT_XSTATE);
		g_assert (GetXStateFeaturesMask (((CONTEXT *)sigctx), &features) == TRUE);
		g_assert ((features & XSTATE_MASK_LEGACY_SSE) != 0);
		g_assert ((features & XSTATE_MASK_AVX) != 0);
#endif
#endif
		mono_sigctx_to_monoctx (sigctx, &tctx->ctx);
	}

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

BOOL
mono_win32_runtime_tls_callback (HMODULE module_handle, DWORD reason, LPVOID reserved, MonoWin32TLSCallbackType callback_type)
{
	if (!mono_win32_handle_tls_callback_type (callback_type))
		return TRUE;

	if (!mono_gc_dllmain (module_handle, reason, reserved))
		return FALSE;

	switch (reason)
	{
	case DLL_PROCESS_ATTACH:
		mono_install_runtime_load (mini_init);
		break;
	case DLL_PROCESS_DETACH:
		break;
	case DLL_THREAD_DETACH:
		mono_thread_info_detach ();
		break;

	}
	return TRUE;
}
