/**
 * \file
 */

#include "w32process.h"
#include "w32process-unix-internals.h"

#ifdef USE_OSX_BACKEND

#include <errno.h>
#include <unistd.h>
#include <sys/time.h>
#include <sys/proc.h>
#include <sys/sysctl.h>
#include <sys/utsname.h>
#include <mach-o/dyld.h>
#include <mach-o/getsect.h>

/* sys/resource.h (for rusage) is required when using osx 10.3 (but not 10.4) */
#ifdef __APPLE__
#include <TargetConditionals.h>
#include <sys/resource.h>
#ifdef HAVE_LIBPROC_H
/* proc_name */
#include <libproc.h>
#endif
#endif

#include "utils/mono-logger-internals.h"

gchar*
mono_w32process_get_name (pid_t pid)
{
	gchar *ret = NULL;

#if defined (__mono_ppc__) || !defined (TARGET_OSX)
	size_t size;
	struct kinfo_proc *pi;
	gint mib[] = { CTL_KERN, KERN_PROC, KERN_PROC_PID, pid };

	if (sysctl(mib, 4, NULL, &size, NULL, 0) < 0)
		return(ret);

	if ((pi = g_malloc (size)) == NULL)
		return(ret);

	if (sysctl (mib, 4, pi, &size, NULL, 0) < 0) {
		if (errno == ENOMEM) {
			g_free (pi);
			mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_PROCESS, "%s: Didn't allocate enough memory for kproc info", __func__);
		}
		return(ret);
	}

	if (strlen (pi->kp_proc.p_comm) > 0)
		ret = g_strdup (pi->kp_proc.p_comm);

	g_free (pi);
#else
	gchar buf[256];
	gint res;

	/* No proc name on OSX < 10.5 nor ppc nor iOS */
	memset (buf, '\0', sizeof(buf));
	res = proc_name (pid, buf, sizeof(buf));
	if (res == 0) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_PROCESS, "%s: proc_name failed, error (%d) \"%s\"", __func__, errno, g_strerror (errno));
		return NULL;
	}

	// Fixes proc_name triming values to 15 characters #32539
	if (strlen (buf) >= MAXCOMLEN - 1) {
		gchar path_buf [PROC_PIDPATHINFO_MAXSIZE];
		gchar *name_buf;
		gint path_len;

		memset (path_buf, '\0', sizeof(path_buf));
		path_len = proc_pidpath (pid, path_buf, sizeof(path_buf));

		if (path_len > 0 && path_len < sizeof(path_buf)) {
			name_buf = path_buf + path_len;
			for(;name_buf > path_buf; name_buf--) {
				if (name_buf [0] == '/') {
					name_buf++;
					break;
				}
			}

			if (memcmp (buf, name_buf, MAXCOMLEN - 1) == 0)
				ret = g_strdup (name_buf);
		}
	}

	if (ret == NULL && strlen (buf) > 0)
		ret = g_strdup (buf);
#endif

	return ret;
}

gchar*
mono_w32process_get_path (pid_t pid)
{
#if defined(__mono_ppc__) || !defined(TARGET_OSX)
	return mono_w32process_get_name (pid);
#else
	gchar buf [PROC_PIDPATHINFO_MAXSIZE];
	gint res;

	res = proc_pidpath (pid, buf, sizeof (buf));
	if (res <= 0)
		return NULL;
	if (buf [0] == '\0')
		return NULL;
	return g_strdup (buf);
#endif
}

GSList*
mono_w32process_get_modules (pid_t pid)
{
	GSList *ret = NULL;
	MonoW32ProcessModule *mod;
	guint32 count;
	int i = 0;

	if (pid != getpid ())
		return NULL;

	count = _dyld_image_count ();
	for (i = 0; i < count; i++) {
#if SIZEOF_VOID_P == 8
		const struct mach_header_64 *hdr;
		const struct section_64 *sec;
#else
		const struct mach_header *hdr;
		const struct section *sec;
#endif
		const char *name;

		name = _dyld_get_image_name (i);
#if SIZEOF_VOID_P == 8
		hdr = (const struct mach_header_64*)_dyld_get_image_header (i);
		sec = getsectbynamefromheader_64 (hdr, SEG_DATA, SECT_DATA);
#else
		hdr = _dyld_get_image_header (i);
		sec = getsectbynamefromheader (hdr, SEG_DATA, SECT_DATA);
#endif

		/* Some dynlibs do not have data sections on osx (#533893) */
		if (sec == 0)
			continue;

		mod = g_new0 (MonoW32ProcessModule, 1);
		mod->address_start = GINT_TO_POINTER (sec->addr);
		mod->address_end = GINT_TO_POINTER (sec->addr+sec->size);
		mod->perms = g_strdup ("r--p");
		mod->address_offset = 0;
		mod->device = makedev (0, 0);
		mod->inode = i;
		mod->filename = g_strdup (name);

		if (g_slist_find_custom (ret, mod, mono_w32process_module_equals) == NULL) {
			ret = g_slist_prepend (ret, mod);
		} else {
			mono_w32process_module_free (mod);
		}
	}

	return g_slist_reverse (ret);
}

#endif
