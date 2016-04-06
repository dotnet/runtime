/*
 * mini-exceptions-native-unwinder.c: libcorkscrew-based native unwinder
 *
 * Authors:
 *   Alex Rønne Petersen (alexrp@xamarin.com)
 *
 * Copyright 2015 Xamarin, Inc (http://www.xamarin.com)
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */
#include <config.h>

#include <mono/utils/mono-logger-internals.h>

/*
 * Attempt to handle native SIGSEGVs with libunwind or libcorkscrew.
 */

#ifdef HAVE_SIGNAL_H
#include <signal.h>
#endif

#include <mono/utils/mono-signal-handler.h>
#include "mini.h"

#if defined (PLATFORM_ANDROID)

#include <signal.h>
#include <sys/types.h>
#include <mono/utils/mono-dl.h>

#define UNW_LOCAL_ONLY
#undef _U /* ctype.h apparently defines this and it screws up the libunwind headers. */
#include "../../external/android-libunwind/include/libunwind.h"
#define _U 0x01

#define FUNC_NAME_LENGTH 512
#define FRAMES_TO_UNWIND 256

/* Expand the SYM argument. */
#define LOAD_SYM(DL, ERR, SYM, VAR) _LOAD_SYM(DL, ERR, SYM, VAR)
#define _LOAD_SYM(DL, ERR, SYM, VAR) \
	do { \
		if ((ERR = mono_dl_symbol (DL, #SYM, (void **) &VAR))) { \
			mono_dl_close (DL); \
			return ERR; \
		} \
	} while (0)

typedef int (*unw_init_local_t) (unw_cursor_t *, unw_context_t *);
typedef int (*unw_get_reg_t) (unw_cursor_t *, int, unw_word_t *);
typedef int (*unw_get_proc_name_t) (unw_cursor_t *, char *, size_t, unw_word_t *);
typedef int (*unw_step_t) (unw_cursor_t *);

static char *
mono_extension_handle_native_sigsegv_libunwind (void *ctx, MONO_SIG_HANDLER_INFO_TYPE *info)
{
	char *dl_err;
	int unw_err;

	unw_init_local_t unw_init_local_fn;
	unw_get_reg_t unw_get_reg_fn;
	unw_get_proc_name_t unw_get_proc_name_fn;
	unw_step_t unw_step_fn;

	unw_cursor_t cursor;

	size_t frames = 0;

	MonoDl *dl = mono_dl_open ("libunwind.so", MONO_DL_LAZY, &dl_err);

	if (!dl)
		return dl_err;

	LOAD_SYM (dl, dl_err, UNW_OBJ (init_local), unw_init_local_fn);
	LOAD_SYM (dl, dl_err, UNW_OBJ (get_reg), unw_get_reg_fn);
	LOAD_SYM (dl, dl_err, UNW_OBJ (get_proc_name), unw_get_proc_name_fn);
	LOAD_SYM (dl, dl_err, UNW_OBJ (step), unw_step_fn);

	if ((unw_err = unw_init_local_fn (&cursor, ctx))) {
		mono_dl_close (dl);

		return g_strdup_printf ("unw_init_local () returned %d", unw_err);
	}

	do {
		int reg_err;

		unw_word_t ip, off;
		char name [FUNC_NAME_LENGTH];

		if ((reg_err = unw_get_reg_fn (&cursor, UNW_REG_IP, &ip))) {
			mono_runtime_printf_err ("unw_get_reg (UNW_REG_IP) returned %d", reg_err);
			break;
		}

		reg_err = unw_get_proc_name_fn (&cursor, name, FUNC_NAME_LENGTH, &off);

		if (reg_err == -UNW_ENOINFO)
			strcpy (name, "???");

		mono_runtime_printf_err (" at %s+%zu [0x%zx]", name, off, ip);

		unw_err = unw_step_fn (&cursor);
		frames++;
	} while (unw_err > 0 && frames < FRAMES_TO_UNWIND);

	if (unw_err < 0)
		mono_runtime_printf_err ("unw_step () returned %d", unw_err);

	mono_dl_close (dl);

	return NULL;
}

/*
 * This code is based on the AOSP header system/core/include/corkscrew/backtrace.h.
 *
 * This is copied here because libcorkscrew is not a stable library and the header (and
 * other headers that it depends on) will eventually go away.
 *
 * We can probably remove this one day when libunwind becomes the norm.
 */

typedef struct {
	uintptr_t absolute_pc;
	uintptr_t stack_top;
	size_t stack_size;
} backtrace_frame_t;

typedef struct {
	uintptr_t relative_pc;
	uintptr_t relative_symbol_addr;
	char *map_name;
	char *symbol_name;
	char *demangled_name;
} backtrace_symbol_t;

typedef void (*get_backtrace_symbols_t) (const backtrace_frame_t *backtrace, size_t frames, backtrace_symbol_t *backtrace_symbols);
typedef void (*free_backtrace_symbols_t) (backtrace_symbol_t *backtrace_symbols, size_t frames);

enum {
	MAX_BACKTRACE_LINE_LENGTH = 800,
};

/* Internals that we're exploiting to work in a signal handler. Only works on ARM/x86. */

typedef struct map_info_t map_info_t;

typedef ssize_t (*unwind_backtrace_signal_arch_t) (siginfo_t *si, void *sc, const map_info_t *lst, backtrace_frame_t *bt, size_t ignore_depth, size_t max_depth);
typedef map_info_t *(*acquire_my_map_info_list_t) (void);
typedef void (*release_my_map_info_list_t) (map_info_t *milist);

static char *
mono_extension_handle_native_sigsegv_libcorkscrew (void *ctx, MONO_SIG_HANDLER_INFO_TYPE *info)
{
#if defined (__arm__) || defined (__i386__)
	char *dl_err;

	get_backtrace_symbols_t get_backtrace_symbols;
	free_backtrace_symbols_t free_backtrace_symbols;
	unwind_backtrace_signal_arch_t unwind_backtrace_signal_arch;
	acquire_my_map_info_list_t acquire_my_map_info_list;
	release_my_map_info_list_t release_my_map_info_list;

	backtrace_frame_t frames [FRAMES_TO_UNWIND];
	backtrace_symbol_t symbols [FRAMES_TO_UNWIND];

	map_info_t *map_info;
	ssize_t frames_unwound;
	size_t i;

	MonoDl *dl = mono_dl_open ("libcorkscrew.so", MONO_DL_LAZY, &dl_err);

	if (!dl)
		return dl_err;

	LOAD_SYM (dl, dl_err, get_backtrace_symbols, get_backtrace_symbols);
	LOAD_SYM (dl, dl_err, free_backtrace_symbols, free_backtrace_symbols);
	LOAD_SYM (dl, dl_err, unwind_backtrace_signal_arch, unwind_backtrace_signal_arch);
	LOAD_SYM (dl, dl_err, acquire_my_map_info_list, acquire_my_map_info_list);
	LOAD_SYM (dl, dl_err, release_my_map_info_list, release_my_map_info_list);

	map_info = acquire_my_map_info_list ();
	frames_unwound = unwind_backtrace_signal_arch (info, ctx, map_info, frames, 0, FRAMES_TO_UNWIND);
	release_my_map_info_list (map_info);

	if (frames_unwound == -1) {
		mono_dl_close (dl);

		return g_strdup ("unwind_backtrace_signal_arch () returned -1");
	}

	get_backtrace_symbols (frames, frames_unwound, symbols);

	for (i = 0; i < frames_unwound; i++) {
		backtrace_frame_t *frame = frames + i;
		backtrace_symbol_t *symbol = symbols + i;

		const char *name = symbol->demangled_name ? symbol->demangled_name : (symbol->symbol_name ? symbol->symbol_name : "???");
		uintptr_t off = symbol->relative_pc - symbol->relative_symbol_addr;
		uintptr_t ip = frame->absolute_pc;

		mono_runtime_printf_err ("  at %s+%zu [0x%zx]", name, off, ip);
	}

	free_backtrace_symbols (symbols, frames_unwound);

	mono_dl_close (dl);

	return NULL;
#else
	return g_strdup ("libcorkscrew is only supported on 32-bit ARM/x86");
#endif
}

void
mono_exception_native_unwind (void *ctx, MONO_SIG_HANDLER_INFO_TYPE *info)
{
	char *unwind_err, *corkscrew_err;

	mono_runtime_printf_err ("\nAttempting native Android stacktrace:\n");

	unwind_err = mono_extension_handle_native_sigsegv_libunwind (ctx, info);

	if (unwind_err) {
		corkscrew_err = mono_extension_handle_native_sigsegv_libcorkscrew (ctx, info);

		if (corkscrew_err) {
			mono_runtime_printf_err ("\tCould not unwind with `libunwind.so`: %s", unwind_err);
			mono_runtime_printf_err ("\tCould not unwind with `libcorkscrew.so`: %s", corkscrew_err);
			mono_runtime_printf_err ("\n\tNo options left to get a native stacktrace :-(");

			g_free (corkscrew_err);
		}

		g_free (unwind_err);
	}
}

#else

void
mono_exception_native_unwind (void *ctx, MONO_SIG_HANDLER_INFO_TYPE *info)
{
}

#endif
