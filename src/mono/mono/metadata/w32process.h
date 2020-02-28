/**
 * \file
 * System.Diagnostics.Process support
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *
 * (C) 2002 Ximian, Inc.
 */

#ifndef _MONO_METADATA_W32PROCESS_H_
#define _MONO_METADATA_W32PROCESS_H_

#include <config.h>
#include <glib.h>

#if HAVE_SYS_TYPES_H
#include <sys/types.h>
#endif

#include <mono/metadata/object.h>
#include "object-internals.h"
#include "marshal.h"

typedef enum {
	MONO_W32PROCESS_PRIORITY_CLASS_NORMAL       = 0x0020,
	MONO_W32PROCESS_PRIORITY_CLASS_IDLE         = 0x0040,
	MONO_W32PROCESS_PRIORITY_CLASS_HIGH         = 0x0080,
	MONO_W32PROCESS_PRIORITY_CLASS_REALTIME     = 0x0100,
	MONO_W32PROCESS_PRIORITY_CLASS_BELOW_NORMAL = 0x4000,
	MONO_W32PROCESS_PRIORITY_CLASS_ABOVE_NORMAL = 0x8000,
} MonoW32ProcessPriorityClass;

typedef struct 
{
	gpointer process_handle;
	guint32 pid; /* Contains mono_w32error_get_last () on failure */
	MonoArray *env_variables;
	MonoString *username;
	MonoString *domain;
	mono_bstr password; /* BSTR from SecureString in 2.0 profile */
	MonoBoolean load_user_profile;
} MonoW32ProcessInfo;

typedef struct
{
	MonoObject object;
	MonoString *filename;
	MonoString *arguments;
	MonoString *working_directory;
	MonoString *verb;
	guint32 window_style;
	MonoBoolean error_dialog;
	gpointer error_dialog_parent_handle;
	MonoBoolean use_shell_execute;

	MonoString *unused_username;
	MonoString *unused_domain;
	MonoObject *unused_password; /* SecureString in 2.0 profile, dummy in 1.x */
	MonoString *unused_password_in_clear_text;
	MonoBoolean unused_load_user_profile;
	MonoBoolean unused_redirect_standard_input;
	MonoBoolean unused_redirect_standard_output;
	MonoBoolean unused_redirect_standard_error;
	MonoObject *unused_encoding_stdout;
	MonoObject *unused_encoding_stderr;

	MonoBoolean create_no_window;

	MonoObject *unused_weak_parent_process;
	MonoObject *unused_envVars;

} MonoW32ProcessStartInfo;

TYPED_HANDLE_DECL (MonoW32ProcessStartInfo);

typedef struct _MonoCreateProcessCoop {
	gunichar2 *filename;
	gunichar2 *arguments;
	gunichar2 *working_directory;
	gunichar2 *verb;
	gunichar2 *username;
	gunichar2 *domain;
	struct {
		MonoStringHandle filename;
		MonoStringHandle arguments;
		MonoStringHandle working_directory;
		MonoStringHandle verb;
		MonoStringHandle username;
		MonoStringHandle domain;
	} coophandle;
	struct {
		MonoGCHandle filename;
		MonoGCHandle arguments;
		MonoGCHandle working_directory;
		MonoGCHandle verb;
		MonoGCHandle username;
		MonoGCHandle domain;
	} gchandle;
	struct {
		gsize filename;
		gsize arguments;
		gsize working_directory;
		gsize verb;
		gsize username;
		gsize domain;
	} length;
} MonoCreateProcessCoop;

void
mono_createprocess_coop_init (MonoCreateProcessCoop *coop, MonoW32ProcessStartInfoHandle proc_start_info, MonoW32ProcessInfo *process_info);

void
mono_createprocess_coop_cleanup (MonoCreateProcessCoop *coop);

void
mono_w32process_init (void);

void
mono_w32process_cleanup (void);

void
mono_w32process_signal_finished (void);

#ifndef HOST_WIN32

void
mono_w32process_set_cli_launcher (gchar *path);

gchar*
mono_w32process_get_path (pid_t pid);

#endif
#endif /* _MONO_METADATA_W32PROCESS_H_ */
