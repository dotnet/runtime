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
#include <dlfcn.h>

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
#include "icall-decl.h"

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

struct mono_dyld_image_info
{
	const void *header_addr;
	const void *data_section_start;
	const void *data_section_end;
	const char *name;
	guint64 order;
};

static guint64 dyld_order = 0;
static GHashTable *images;
static mono_mutex_t images_mutex;

static int
sort_modules_by_load_order (gconstpointer a, gconstpointer b)
{
	MonoW32ProcessModule *ma = (MonoW32ProcessModule *) a;
	MonoW32ProcessModule *mb = (MonoW32ProcessModule *) b;
	return ma->inode == mb->inode ? 0 : ma->inode < mb->inode ? -1 : 1;
}

GSList *
mono_w32process_get_modules (pid_t pid)
{
	GSList *ret = NULL;
	MONO_ENTER_GC_SAFE;
	if (pid != getpid ())
		goto done;

	GHashTableIter it;
	g_hash_table_iter_init (&it, images);

	gpointer val;

	mono_os_mutex_lock (&images_mutex);
	while (g_hash_table_iter_next (&it, NULL, &val)) {
		struct mono_dyld_image_info *info = (struct mono_dyld_image_info *) val;
		MonoW32ProcessModule *mod = g_new0 (MonoW32ProcessModule, 1);
		mod->address_start = GINT_TO_POINTER (info->data_section_start);
		mod->address_end = GINT_TO_POINTER (info->data_section_end);
		mod->perms = g_strdup ("r--p");
		mod->address_offset = 0;
		mod->device = 0;
		mod->inode = info->order;
		mod->filename = g_strdup (info->name);
		ret = g_slist_prepend (ret, mod);
	}
	mono_os_mutex_unlock (&images_mutex);
	ret = g_slist_sort (ret, &sort_modules_by_load_order);
done:
	MONO_EXIT_GC_SAFE;
	return ret;
}

static void
mono_dyld_image_info_free (void *info)
{
	struct mono_dyld_image_info *dinfo = (struct mono_dyld_image_info *) info;
	g_free ((void *) dinfo->name);
	g_free (dinfo);
}

static void
image_added (const struct mach_header *hdr32, intptr_t vmaddr_slide)
{
	#if SIZEOF_VOID_P == 8
	const struct mach_header_64 *hdr64 = (const struct mach_header_64 *)hdr32;
	const struct section_64 *sec = getsectbynamefromheader_64 (hdr64, SEG_DATA, SECT_DATA);
	#else
	const struct section *sec = getsectbynamefromheader (hdr32, SEG_DATA, SECT_DATA);
	#endif
	Dl_info dlinfo;
	if (!dladdr (hdr32, &dlinfo)) return;
	if (sec == NULL) return;

	mono_os_mutex_lock (&images_mutex);
	gpointer found = g_hash_table_lookup (images, (gpointer) hdr32);
	mono_os_mutex_unlock (&images_mutex);

	if (found == NULL) {
		struct mono_dyld_image_info *info = g_new0 (struct mono_dyld_image_info, 1);
		info->header_addr = hdr32;
		info->data_section_start = GINT_TO_POINTER (sec->addr);
		info->data_section_end = GINT_TO_POINTER (sec->addr + sec->size);
		info->name = g_strdup (dlinfo.dli_fname);
		info->order = dyld_order;
		++dyld_order;

		mono_os_mutex_lock (&images_mutex);
		g_hash_table_insert (images, (gpointer) hdr32, info);
		mono_os_mutex_unlock (&images_mutex);
	}
}

static void
image_removed (const struct mach_header *hdr32, intptr_t vmaddr_slide)
{
	mono_os_mutex_lock (&images_mutex);
	g_hash_table_remove (images, hdr32);
	mono_os_mutex_unlock (&images_mutex);
}

void
mono_w32process_platform_init_once (void)
{
	mono_os_mutex_init (&images_mutex);
	images = g_hash_table_new_full (NULL, NULL, NULL, &mono_dyld_image_info_free);

	/* Ensure that the functions used within the lock-protected region in
	 * mono_w32process_get_modules have been loaded, in case these symbols
	 * are lazily bound. g_new0 and g_strdup will be called by
	 * _dyld_register_func_for_add_image when it calls image_added with the
	 * current list of all loaded dynamic libraries
	 */
	GSList *dummy = g_slist_prepend (NULL, NULL);
	g_slist_free (dummy);
	GHashTableIter it;
	g_hash_table_iter_init (&it, images);
	g_hash_table_iter_next (&it, NULL, NULL);

	_dyld_register_func_for_add_image (&image_added);
	_dyld_register_func_for_remove_image (&image_removed);
}

#else

MONO_EMPTY_SOURCE_FILE (w32process_unix_osx);

#endif
