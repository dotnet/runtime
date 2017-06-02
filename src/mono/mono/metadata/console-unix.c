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

#include <mono/metadata/console-io.h>
#include <mono/metadata/exception.h>

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
ves_icall_System_ConsoleDriver_Isatty (HANDLE handle)
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
ves_icall_System_ConsoleDriver_SetEcho (MonoBoolean want_echo)
{
	
	return set_property (ECHO, want_echo);
}

MonoBoolean
ves_icall_System_ConsoleDriver_SetBreak (MonoBoolean want_break)
{
	return set_property (IGNBRK, !want_break);
}

gint32
ves_icall_System_ConsoleDriver_InternalKeyAvailable (gint32 timeout)
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
		errno = save_errno;
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
	static MonoClassField *cancel_handler_field;
	MonoError error;
	MonoDomain *domain = mono_domain_get ();
	MonoClass *klass;
	MonoDelegate *load_value;
	MonoMethod *method;
	MonoVTable *vtable;

	/* FIXME: this should likely iterate all the domains, instead */
	if (!domain->domain)
		return;

	klass = mono_class_try_load_from_name (mono_defaults.corlib, "System", "Console");
	if (klass == NULL)
		return;

	if (cancel_handler_field == NULL) {
		cancel_handler_field = mono_class_get_field_from_name (klass, "cancel_handler");
		g_assert (cancel_handler_field);
	}

	vtable = mono_class_vtable_full (domain, klass, &error);
	if (vtable == NULL || !is_ok (&error)) {
		mono_error_cleanup (&error);
		return;
	}
	mono_field_static_get_value_checked (vtable, cancel_handler_field, &load_value, &error);
	if (load_value == NULL || !is_ok (&error)) {
		mono_error_cleanup (&error);
		return;
	}

	klass = load_value->object.vtable->klass;
	method = mono_class_get_method_from_name (klass, "BeginInvoke", -1);
	g_assert (method != NULL);

	mono_threadpool_begin_invoke (domain, (MonoObject*) load_value, method, NULL, &error);
	if (!is_ok (&error)) {
		g_warning ("Couldn't invoke System.Console cancel handler due to %s", mono_error_get_message (&error));
		mono_error_cleanup (&error);
	}
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
	errno = save_errno;
	in_sigint = FALSE;
}

static struct sigaction save_sigcont, save_sigint, save_sigwinch;

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
set_control_chars (MonoArray *control_chars, const guchar *cc)
{
	/* The index into the array comes from corlib/System/ControlCharacters.cs */
#ifdef VINTR
	mono_array_set (control_chars, gchar, 0, cc [VINTR]);
#endif
#ifdef VQUIT
	mono_array_set (control_chars, gchar, 1, cc [VQUIT]);
#endif
#ifdef VERASE
	mono_array_set (control_chars, gchar, 2, cc [VERASE]);
#endif
#ifdef VKILL
	mono_array_set (control_chars, gchar, 3, cc [VKILL]);
#endif
#ifdef VEOF
	mono_array_set (control_chars, gchar, 4, cc [VEOF]);
#endif
#ifdef VTIME
	mono_array_set (control_chars, gchar, 5, cc [VTIME]);
#endif
#ifdef VMIN
	mono_array_set (control_chars, gchar, 6, cc [VMIN]);
#endif
#ifdef VSWTC
	mono_array_set (control_chars, gchar, 7, cc [VSWTC]);
#endif
#ifdef VSTART
	mono_array_set (control_chars, gchar, 8, cc [VSTART]);
#endif
#ifdef VSTOP
	mono_array_set (control_chars, gchar, 9, cc [VSTOP]);
#endif
#ifdef VSUSP
	mono_array_set (control_chars, gchar, 10, cc [VSUSP]);
#endif
#ifdef VEOL
	mono_array_set (control_chars, gchar, 11, cc [VEOL]);
#endif
#ifdef VREPRINT
	mono_array_set (control_chars, gchar, 12, cc [VREPRINT]);
#endif
#ifdef VDISCARD
	mono_array_set (control_chars, gchar, 13, cc [VDISCARD]);
#endif
#ifdef VWERASE
	mono_array_set (control_chars, gchar, 14, cc [VWERASE]);
#endif
#ifdef VLNEXT
	mono_array_set (control_chars, gchar, 15, cc [VLNEXT]);
#endif
#ifdef VEOL2
	mono_array_set (control_chars, gchar, 16, cc [VEOL2]);
#endif
}

MonoBoolean
ves_icall_System_ConsoleDriver_TtySetup (MonoString *keypad, MonoString *teardown, MonoArray **control_chars, int **size)
{
	MonoError error;

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
	MonoArray *control_chars_arr = mono_array_new_checked (mono_domain_get (), mono_defaults.byte_class, 17, &error);
	if (mono_error_set_pending_exception (&error))
		return FALSE;
	mono_gc_wbarrier_generic_store (control_chars, (MonoObject*) control_chars_arr);
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
	if (tcsetattr (STDIN_FILENO, TCSANOW, &mono_attr) == -1)
		return FALSE;

	set_control_chars (*control_chars, mono_attr.c_cc);
	/* If initialized from another appdomain... */
	if (setup_finished)
		return TRUE;

	keypad_xmit_str = NULL;
	if (keypad != NULL) {
		keypad_xmit_str = mono_string_to_utf8_checked (keypad, &error);
		if (mono_error_set_pending_exception (&error))
			return FALSE;
	}
	
	console_set_signal_handlers ();
	setup_finished = TRUE;
	if (!atexit_called) {
		if (teardown != NULL) {
			teardown_str = mono_string_to_utf8_checked (teardown, &error);
			if (mono_error_set_pending_exception (&error))
				return FALSE;
		}

		mono_atexit (tty_teardown);
	}

	return TRUE;
}
