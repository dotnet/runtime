/**
 * \file
 */

#include "w32process.h"
#include "w32process-unix-internals.h"

#ifdef USE_BSD_BACKEND

#include <errno.h>
#include <signal.h>
#include <sys/proc.h>
#include <sys/sysctl.h>
#if !defined(__OpenBSD__)
#include <sys/utsname.h>
#endif
#if defined(__FreeBSD__)
#include <sys/user.h> /* struct kinfo_proc */
#endif

#include <link.h>
#include "utils/mono-logger-internals.h"
#include "icall-decl.h"

gchar*
mono_w32process_get_name (pid_t pid)
{
	gint mib [6];
	gsize size;
	struct kinfo_proc *pi;
	gchar *ret = NULL;

#if defined(__FreeBSD__)
	mib [0] = CTL_KERN;
	mib [1] = KERN_PROC;
	mib [2] = KERN_PROC_PID;
	mib [3] = pid;
	if (sysctl(mib, 4, NULL, &size, NULL, 0) < 0) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_PROCESS, "%s: sysctl() failed: %d", __func__, errno);
		return NULL;
	}

	if ((pi = g_malloc (size)) == NULL)
		return NULL;

	if (sysctl (mib, 4, pi, &size, NULL, 0) < 0) {
		if (errno == ENOMEM) {
			g_free (pi);
			mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_PROCESS, "%s: Didn't allocate enough memory for kproc info", __func__);
		}
		return NULL;
	}

	ret = strlen (pi->ki_comm) > 0 ? g_strdup (pi->ki_comm) : NULL;

	g_free (pi);
#elif defined(__OpenBSD__)
	mib [0] = CTL_KERN;
	mib [1] = KERN_PROC;
	mib [2] = KERN_PROC_PID;
	mib [3] = pid;
	mib [4] = sizeof(struct kinfo_proc);
	mib [5] = 0;

retry:
	if (sysctl(mib, 6, NULL, &size, NULL, 0) < 0) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_PROCESS, "%s: sysctl() failed: %d", __func__, errno);
		return NULL;
	}

	if ((pi = g_malloc (size)) == NULL)
		return NULL;

	mib[5] = (int)(size / sizeof(struct kinfo_proc));

	if ((sysctl (mib, 6, pi, &size, NULL, 0) < 0) ||
		(size != sizeof (struct kinfo_proc))) {
		if (errno == ENOMEM) {
			g_free (pi);
			goto retry;
		}
		return NULL;
	}

	ret = strlen (pi->p_comm) > 0 ? g_strdup (pi->p_comm) : NULL;

	g_free (pi);
#endif

	return ret;
}

gchar*
mono_w32process_get_path (pid_t pid)
{
#if defined (__OpenBSD__)
	// No KERN_PROC_PATHNAME on OpenBSD
	return mono_w32process_get_name (pid);
#else
	gsize path_len = PATH_MAX + 1;
	gchar path [PATH_MAX + 1];
	gint mib [4];
	mib [0] = CTL_KERN;
#if defined (__NetBSD__)
	mib [1] = KERN_PROC_ARGS;
	mib [2] = pid;
	mib [3] = KERN_PROC_PATHNAME;
#else // FreeBSD
	mib [1] = KERN_PROC;
	mib [2] = KERN_PROC_PATHNAME;
	mib [3] = pid;
#endif
	if (sysctl (mib, 4, path, &path_len, NULL, 0) < 0) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_PROCESS, "%s: sysctl() failed: %d", __func__, errno);
		return NULL;
	} else {
		return g_strdup (path);
	}
#endif
}

static gint
mono_w32process_get_modules_callback (struct dl_phdr_info *info, gsize size, gpointer ptr)
{
	if (size < offsetof (struct dl_phdr_info, dlpi_phnum) + sizeof (info->dlpi_phnum))
		return (-1);

	struct dl_phdr_info *cpy = g_calloc (1, sizeof(struct dl_phdr_info));
	if (!cpy)
		return (-1);

	memcpy(cpy, info, sizeof(*info));

	g_ptr_array_add ((GPtrArray *)ptr, cpy);

	return (0);
}

GSList*
mono_w32process_get_modules (pid_t pid)
{
	GSList *ret = NULL;
	MonoW32ProcessModule *mod;
	GPtrArray *dlarray = g_ptr_array_new();
	gint i;

	if (dl_iterate_phdr (mono_w32process_get_modules_callback, dlarray) < 0)
		return NULL;

	for (i = 0; i < dlarray->len; i++) {
		struct dl_phdr_info *info = g_ptr_array_index (dlarray, i);

		mod = g_new0 (MonoW32ProcessModule, 1);
		mod->address_start = (gpointer)(info->dlpi_addr + info->dlpi_phdr[0].p_vaddr);
		mod->address_end = (gpointer)(info->dlpi_addr + info->dlpi_phdr[info->dlpi_phnum - 1].p_vaddr);
		mod->perms = g_strdup ("r--p");
		mod->address_offset = 0;
		mod->inode = i;
		mod->filename = g_strdup (info->dlpi_name);

		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_PROCESS, "%s: inode=%d, filename=%s, address_start=%p, address_end=%p",
			__func__, mod->inode, mod->filename, mod->address_start, mod->address_end);

		g_free (info);

		if (g_slist_find_custom (ret, mod, mono_w32process_module_equals) == NULL) {
			ret = g_slist_prepend (ret, mod);
		} else {
			mono_w32process_module_free (mod);
		}
	}

	g_ptr_array_free (dlarray, TRUE);

	return g_slist_reverse (ret);
}

void
mono_w32process_platform_init_once (void)
{
}

#else

MONO_EMPTY_SOURCE_FILE (w32process_unix_bsd);

#endif
