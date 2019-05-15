/**
 * \file
 * File System Watcher internal calls
 *
 * Authors:
 *	Gonzalo Paniagua Javier (gonzalo@ximian.com)
 *
 * Copyright 2004-2009 Novell, Inc (http://www.novell.com)
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#include <config.h>

#if !ENABLE_NETCORE

#ifdef HAVE_SYS_TYPES_H
#include <sys/types.h>
#endif
#ifdef HAVE_SYS_EVENT_H
#include <sys/event.h>
#endif
#ifdef HAVE_SYS_TIME_H
#include <sys/time.h>
#endif

#include <mono/metadata/appdomain.h>
#include <mono/metadata/exception.h>
#include <mono/metadata/filewatcher.h>
#include <mono/metadata/marshal.h>
#include <mono/utils/mono-dl.h>
#include <mono/utils/mono-io-portability.h>
#include <mono/metadata/w32error.h>

#ifdef HOST_WIN32

/*
 * TODO:
 * We use the managed watcher on windows, so the code inside this #if is never used
 */
gint
ves_icall_System_IO_FSW_SupportsFSW (void)
{
	return 1;
}

gboolean
ves_icall_System_IO_FAMW_InternalFAMNextEvent (gpointer conn,
					       MonoString **filename,
					       gint *code,
					       gint *reqnum)
{
	return FALSE;
}

#else

static int (*FAMNextEvent) (gpointer, gpointer);

gint
ves_icall_System_IO_FSW_SupportsFSW (void)
{
#if defined(__APPLE__)
	if (getenv ("MONO_DARWIN_USE_KQUEUE_FSW"))
		return 3; /* kqueue */
	else
		return 6; /* CoreFX */
#elif defined(HAVE_SYS_INOTIFY_H)
	return 6; /* CoreFX */
#elif HAVE_KQUEUE
	return 3; /* kqueue */
#else
	MonoDl *fam_module;
	int lib_used = 4; /* gamin */
	char *err;

	fam_module = mono_dl_open ("libgamin-1.so", MONO_DL_LAZY, NULL);
	if (fam_module == NULL) {
		lib_used = 2; /* FAM */
		fam_module = mono_dl_open ("libfam.so", MONO_DL_LAZY, NULL);
	}

	if (fam_module == NULL)
		return 0; /* DefaultWatcher */

	err = mono_dl_symbol (fam_module, "FAMNextEvent", (gpointer *) &FAMNextEvent);
	g_free (err);
	if (FAMNextEvent == NULL)
		return 0;

	return lib_used; /* DefaultWatcher */
#endif
}

/* Almost copied from fam.h. Weird, I know */
typedef struct {
	gint reqnum;
} FAMRequest;

typedef struct FAMEvent {
	gpointer fc;
	FAMRequest fr;
	gchar *hostname;
	gchar filename [PATH_MAX];
	gpointer userdata;
	gint code;
} FAMEvent;

gboolean
ves_icall_System_IO_FAMW_InternalFAMNextEvent (gpointer conn,
					       MonoString **filename,
					       gint *code,
					       gint *reqnum)
{
	ERROR_DECL (error);
	FAMEvent ev;

	if (FAMNextEvent (conn, &ev) == 1) {
		*filename = mono_string_new_checked (mono_domain_get (), ev.filename, error);
		*code = ev.code;
		*reqnum = ev.fr.reqnum;
		if (mono_error_set_pending_exception (error))
			return FALSE;
		return TRUE;
	}

	return FALSE;
}
#endif

#if HAVE_KQUEUE

static void
interrupt_kevent (gpointer data)
{
	int *kq_ptr = (int*)data;

	/* Interrupt the kevent () call by closing the fd */
	close (*kq_ptr);
	/* Signal to managed code that the fd is closed */
	*kq_ptr = -1;
}

/*
 * ves_icall_System_IO_KqueueMonitor_kevent_notimeout:
 *
 *   Call kevent (), while handling runtime interruptions.
 */
int
ves_icall_System_IO_KqueueMonitor_kevent_notimeout (int *kq_ptr, gpointer changelist, int nchanges, gpointer eventlist, int nevents)
{
	int res;
	gboolean interrupted;

	mono_thread_info_install_interrupt (interrupt_kevent, kq_ptr, &interrupted);
	if (interrupted) {
		close (*kq_ptr);
		*kq_ptr = -1;
		return -1;
	}

	MONO_ENTER_GC_SAFE;
	res = kevent (*kq_ptr, (const struct kevent*)changelist, nchanges, (struct kevent*)eventlist, nevents, NULL);
	MONO_EXIT_GC_SAFE;

	mono_thread_info_uninstall_interrupt (&interrupted);

	return res;
}

#else

int
ves_icall_System_IO_KqueueMonitor_kevent_notimeout (int *kq_ptr, gpointer changelist, int nchanges, gpointer eventlist, int nevents)
{
	g_assert_not_reached ();
	return -1;
}

#endif /* #if HAVE_KQUEUE */

#endif /* !ENABLE_NETCORE */
