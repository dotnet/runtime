/**
 * \file
 * ConsoleDriver internal calls for Unix systems.
 *
 * Author:
 *	Gonzalo Paniagua Javier (gonzalo@ximian.com)
 *
 * Copyright (C) 2005-2009 Novell, Inc. (http://www.novell.com)
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */
#include <config.h>
#include <glib.h>
#include <stdio.h>
#include <string.h>
#include <fcntl.h>
#include <errno.h>
#include <signal.h>
#ifdef HAVE_SYS_SELECT_H
#    include <sys/select.h>
#endif
#ifdef HAVE_SYS_TIME_H
#    include <sys/time.h>
#endif
#include <sys/types.h>
#ifdef HAVE_UNISTD_H
#    include <unistd.h>
#endif
#include <mono/metadata/appdomain.h>
#include <mono/metadata/object-internals.h>
#include <mono/metadata/class-internals.h>
#include <mono/metadata/domain-internals.h>
#include <mono/metadata/gc-internals.h>
#include <mono/metadata/metadata.h>
#include <mono/metadata/threadpool.h>
#include <mono/utils/mono-signal-handler.h>
#include <mono/utils/mono-proclib.h>
#include <mono/utils/w32api.h>
#include <mono/utils/mono-errno.h>
#include <mono/metadata/console-io.h>
#include <mono/metadata/exception.h>
#include "icall-decl.h"

#ifndef ENABLE_NETCORE

/* On solaris, curses.h must come before both termios.h and term.h */
#ifdef HAVE_CURSES_H
#    include <curses.h>
#endif
#ifdef HAVE_TERMIOS_H
#    include <termios.h>
#endif
#ifdef HAVE_TERM_H
#    include <term.h>
#endif

/* Needed for FIONREAD under solaris */
#ifdef HAVE_SYS_FILIO_H
#    include <sys/filio.h>
#endif
#ifdef HAVE_SYS_IOCTL_H
#    include <sys/ioctl.h>
#endif

static gboolean setup_finished;
static gboolean atexit_called;

/* The string used to return the terminal to its previous state */
static gchar *teardown_str;

/* The string used to set the terminal into keypad xmit mode after SIGCONT is received */
static gchar *keypad_xmit_str;

#ifdef HAVE_TERMIOS_H
/* This is the last state used by Mono, used after a CONT signal is received */
static struct termios mono_attr;
#endif

/* static void console_restore_signal_handlers (void); */
static void console_set_signal_handlers (void);

static GENERATE_TRY_GET_CLASS_WITH_CACHE (console, "System", "Console");

void
mono_console_init (void)
{
	int fd;

	/* Make sure the standard file descriptors are opened */
	fd = open ("/dev/null", O_RDWR);
	while (fd >= 0 && fd < 3) {
		fd = open ("/dev/null", O_RDWR);
	}
	close (fd);
}

static struct termios initial_attr;

MonoBoolean
ves_icall_System_ConsoleDriver_Isatty (HANDLE handle, MonoError* error)
{
	return isatty (GPOINTER_TO_INT (handle));
}

static MonoBoolean
set_property (gint property, gboolean value)
{
	struct termios attr;
	gboolean callset = FALSE;
	gboolean check;
	
	if (tcgetattr (STDIN_FILENO, &attr) == -1)
		return FALSE;

	check = (attr.c_lflag & property) != 0;
	if ((value || check) && !(value && check)) {
		callset = TRUE;
		if (value)
			attr.c_lflag |= property;
		else
			attr.c_lflag &= ~property;
	}

	if (!callset)
		return TRUE;

	if (tcsetattr (STDIN_FILENO, TCSANOW, &attr) == -1)
		return FALSE;

	mono_attr = attr;
	return TRUE;
}

MonoBoolean
ves_icall_System_ConsoleDriver_SetEcho (MonoBoolean want_echo, MonoError* error)
{
	return set_property (ECHO, want_echo);
}

MonoBoolean
ves_icall_System_ConsoleDriver_SetBreak (MonoBoolean want_break, MonoError* error)
{
	return set_property (IGNBRK, !want_break);
}

gint32
ves_icall_System_ConsoleDriver_InternalKeyAvailable (gint32 timeout, MonoError* error)
{
	fd_set rfds;
	struct timeval tv;
	struct timeval *tvptr;
	div_t divvy;
	int ret, nbytes;

	do {
		FD_ZERO (&rfds);
		FD_SET (STDIN_FILENO, &rfds);
		if (timeout >= 0) {
			divvy = div (timeout, 1000);
			tv.tv_sec = divvy.quot;
			tv.tv_usec = divvy.rem;
			tvptr = &tv;
		} else {
			tvptr = NULL;
		}
		ret = select (STDIN_FILENO + 1, &rfds, NULL, NULL, tvptr);
	} while (ret == -1 && errno == EINTR);

	if (ret > 0) {
		nbytes = 0;
		ret = ioctl (STDIN_FILENO, FIONREAD, &nbytes);
		if (ret >= 0)
			ret = nbytes;
	}

	return (ret > 0) ? ret : 0;
}

static gint32 cols_and_lines;

#ifdef TIOCGWINSZ
static int
terminal_get_dimensions (void)
{
	struct winsize ws;
	int ret;
	int save_errno = errno;
	
	if (ioctl (STDIN_FILENO, TIOCGWINSZ, &ws) == 0){
		ret = (ws.ws_col << 16) | ws.ws_row;
		mono_set_errno (save_errno);
		return ret;
	} 
	return -1;
}
#else
static int
terminal_get_dimensions (void)
{
	return -1;
}
#endif

static void
tty_teardown (void)
{
	int unused G_GNUC_UNUSED;

	if (!setup_finished)
		return;

	if (teardown_str != NULL) {
		unused = write (STDOUT_FILENO, teardown_str, strlen (teardown_str));
		g_free (teardown_str);
		teardown_str = NULL;
	}

	tcflush (STDIN_FILENO, TCIFLUSH);
	tcsetattr (STDIN_FILENO, TCSANOW, &initial_attr);
	set_property (ECHO, TRUE);
	setup_finished = FALSE;
}

static void
do_console_cancel_event (void)
{
	static MonoMethod *System_Console_DoConsoleCancelEventBackground_method = (MonoMethod*)(intptr_t)-1;
	ERROR_DECL (error);

	if (mono_class_try_get_console_class () == NULL)
		return;

	if (System_Console_DoConsoleCancelEventBackground_method == (gpointer)(intptr_t)-1) {
		System_Console_DoConsoleCancelEventBackground_method = mono_class_get_method_from_name_checked (mono_class_try_get_console_class (), "DoConsoleCancelEventInBackground", 0, 0, error);
		mono_error_assert_ok (error);
	}
	if (System_Console_DoConsoleCancelEventBackground_method == NULL)
		return;

	mono_runtime_invoke_checked (System_Console_DoConsoleCancelEventBackground_method, NULL, NULL, error);
	mono_error_assert_ok (error);
}

static int need_cancel = FALSE;
/* this is executed from the finalizer thread */
void
mono_console_handle_async_ops (void)
{
	if (need_cancel) {
		need_cancel = FALSE;
		do_console_cancel_event ();
	}
}

static gboolean in_sigint;

MONO_SIG_HANDLER_FUNC (static, sigint_handler)
{
	int save_errno;

	if (in_sigint)
		return;

	in_sigint = TRUE;
	save_errno = errno;
	need_cancel = TRUE;
	mono_gc_finalize_notify ();
	mono_set_errno (save_errno);
	in_sigint = FALSE;
}

static struct sigaction save_sigcont, save_sigwinch;

#if HAVE_SIGACTION
static struct sigaction save_sigint;
#endif

MONO_SIG_HANDLER_FUNC (static, sigcont_handler)
{
	int unused G_GNUC_UNUSED;
	// Ignore error, there is not much we can do in the sigcont handler.
	tcsetattr (STDIN_FILENO, TCSANOW, &mono_attr);

	if (keypad_xmit_str != NULL)
		unused = write (STDOUT_FILENO, keypad_xmit_str, strlen (keypad_xmit_str));

	// Call previous handler
	if (save_sigcont.sa_sigaction != NULL &&
	    save_sigcont.sa_sigaction != (void *)SIG_DFL &&
	    save_sigcont.sa_sigaction != (void *)SIG_IGN)
		(*save_sigcont.sa_sigaction) (MONO_SIG_HANDLER_PARAMS);
}

MONO_SIG_HANDLER_FUNC (static, sigwinch_handler)
{
	int dims = terminal_get_dimensions ();
	if (dims != -1)
		cols_and_lines = dims;
	
	// Call previous handler
	if (save_sigwinch.sa_sigaction != NULL &&
	    save_sigwinch.sa_sigaction != (void *)SIG_DFL &&
	    save_sigwinch.sa_sigaction != (void *)SIG_IGN)
		(*save_sigwinch.sa_sigaction) (MONO_SIG_HANDLER_PARAMS);
}

/*
 * console_set_signal_handlers:
 *
 * Installs various signals handlers for the use of the console, as
 * follows:
 *
 * SIGCONT: this is received after the application has resumed execution
 * if it was suspended with Control-Z before.   This signal handler needs
 * to resend the terminal sequence to send keyboard in keypad mode (this
 * is the difference between getting a cuu1 code or a kcuu1 code for up-arrow
 * for example
 *
 * SIGINT: invokes the System.Console.DoConsoleCancelEvent method using
 * a thread from the thread pool which notifies all registered cancel_event
 * listeners.
 *
 * SIGWINCH: is used to track changes to the console window when a GUI
 * terminal is resized.    It sets an internal variable that is checked
 * by System.Console when the Terminfo driver has been activated.
 */
static void
console_set_signal_handlers ()
{
#if defined(HAVE_SIGACTION)
	struct sigaction sigcont, sigint, sigwinch;

	memset (&sigcont, 0, sizeof (struct sigaction));
	memset (&sigint, 0, sizeof (struct sigaction));
	memset (&sigwinch, 0, sizeof (struct sigaction));
	
	// Continuing
	sigcont.sa_handler = (void (*)(int)) sigcont_handler;
	sigcont.sa_flags = SA_RESTART;
	sigemptyset (&sigcont.sa_mask);
	sigaction (SIGCONT, &sigcont, &save_sigcont);
	
	// Interrupt handler
	sigint.sa_handler = (void (*)(int)) sigint_handler;
	sigint.sa_flags = SA_RESTART;
	sigemptyset (&sigint.sa_mask);
	sigaction (SIGINT, &sigint, &save_sigint);

	// Window size changed
	sigwinch.sa_handler = (void (*)(int)) sigwinch_handler;
	sigwinch.sa_flags = SA_RESTART;
	sigemptyset (&sigwinch.sa_mask);
	sigaction (SIGWINCH, &sigwinch, &save_sigwinch);
#endif
}

#if currently_unuused
//
// Currently unused, should we ever call the restore handler?
// Perhaps before calling into Process.Start?
//
void
console_restore_signal_handlers ()
{
	sigaction (SIGCONT, &save_sigcont, NULL);
	sigaction (SIGINT, &save_sigint, NULL);
	sigaction (SIGWINCH, &save_sigwinch, NULL);
}
#endif

static void
set_control_chars (gchar *control_chars, const guchar *cc)
{
	/* The index into the array comes from corlib/System/ControlCharacters.cs */
#ifdef VINTR
	control_chars [0] = cc [VINTR];
#endif
#ifdef VQUIT
	control_chars [1] = cc [VQUIT];
#endif
#ifdef VERASE
	control_chars [2] = cc [VERASE];
#endif
#ifdef VKILL
	control_chars [3] = cc [VKILL];
#endif
#ifdef VEOF
	control_chars [4] = cc [VEOF];
#endif
#ifdef VTIME
	control_chars [5] = cc [VTIME];
#endif
#ifdef VMIN
	control_chars [6] = cc [VMIN];
#endif
#ifdef VSWTC
	control_chars [7] = cc [VSWTC];
#endif
#ifdef VSTART
	control_chars [8] = cc [VSTART];
#endif
#ifdef VSTOP
	control_chars [9] = cc [VSTOP];
#endif
#ifdef VSUSP
	control_chars [10] = cc [VSUSP];
#endif
#ifdef VEOL
	control_chars [11] = cc [VEOL];
#endif
#ifdef VREPRINT
	control_chars [12] = cc [VREPRINT];
#endif
#ifdef VDISCARD
	control_chars [13] = cc [VDISCARD];
#endif
#ifdef VWERASE
	control_chars [14] = cc [VWERASE];
#endif
#ifdef VLNEXT
	control_chars [15] = cc [VLNEXT];
#endif
#ifdef VEOL2
	control_chars [16] = cc [VEOL2];
#endif
}

MonoBoolean
ves_icall_System_ConsoleDriver_TtySetup (MonoStringHandle keypad, MonoStringHandle teardown, MonoArrayHandleOut control_chars, int **size, MonoError* error)
{
	// FIXME Lock around the globals?

	int dims;

	dims = terminal_get_dimensions ();
	if (dims == -1){
		int cols = 0, rows = 0;
				      
		char *str = g_getenv ("COLUMNS");
		if (str != NULL) {
			cols = atoi (str);
			g_free (str);
		}
		str = g_getenv ("LINES");
		if (str != NULL) {
			rows = atoi (str);
			g_free (str);
		}

		if (cols != 0 && rows != 0)
			cols_and_lines = (cols << 16) | rows;
		else
			cols_and_lines = -1;
	} else {
		cols_and_lines = dims;
	}
	
	*size = &cols_and_lines;

	/* 17 is the number of entries set in set_control_chars() above.
	 * NCCS is the total size, but, by now, we only care about those 17 values*/
	MonoArrayHandle control_chars_arr = mono_array_new_handle (mono_domain_get (), mono_defaults.byte_class, 17, error);
	return_val_if_nok (error, FALSE);

	MONO_HANDLE_ASSIGN (control_chars, control_chars_arr);
	if (tcgetattr (STDIN_FILENO, &initial_attr) == -1)
		return FALSE;

	mono_attr = initial_attr;
	mono_attr.c_lflag &= ~(ICANON);
	mono_attr.c_iflag &= ~(IXON|IXOFF);
	mono_attr.c_cc [VMIN] = 1;
	mono_attr.c_cc [VTIME] = 0;
#ifdef VDSUSP
	/* Disable C-y being used as a suspend character on OSX */
	mono_attr.c_cc [VDSUSP] = 255;
#endif
	gint ret;
	do {
		MONO_ENTER_GC_SAFE;
		ret = tcsetattr (STDIN_FILENO, TCSANOW, &mono_attr);
		MONO_EXIT_GC_SAFE;
	} while (ret == -1 && errno == EINTR);

	if (ret == -1)
		return FALSE;

	MonoGCHandle h;
	set_control_chars (MONO_ARRAY_HANDLE_PIN (control_chars_arr, gchar, 0, &h), mono_attr.c_cc);
	mono_gchandle_free_internal (h);
	/* If initialized from another appdomain... */
	if (setup_finished)
		return TRUE;

	keypad_xmit_str = NULL;
	if (!MONO_HANDLE_IS_NULL (keypad)) {
		keypad_xmit_str = mono_string_handle_to_utf8 (keypad, error);
		return_val_if_nok (error, FALSE);
	}
	
	console_set_signal_handlers ();
	setup_finished = TRUE;
	if (!atexit_called) {
		if (!MONO_HANDLE_IS_NULL (teardown)) {
			teardown_str = mono_string_handle_to_utf8 (teardown, error);
			return_val_if_nok (error, FALSE);
		}

		mono_atexit (tty_teardown);
	}

	return TRUE;
}

#else /* ENABLE_NETCORE */

void
mono_console_init (void)
{
}

void
mono_console_handle_async_ops (void)
{
}

#endif
