/**
 * \file
 */

#include "w32process.h"
#include "w32process-unix-internals.h"

#ifdef USE_DEFAULT_BACKEND

#include <unistd.h>

#ifdef HOST_SOLARIS
/* procfs.h cannot be included if this define is set, but it seems to work fine if it is undefined */
#if _FILE_OFFSET_BITS == 64
#undef _FILE_OFFSET_BITS
#include <procfs.h>
#define _FILE_OFFSET_BITS 64
#else
#include <procfs.h>
#endif
#endif

/* makedev() macro */
#ifdef MAJOR_IN_MKDEV
#include <sys/mkdev.h>
#elif defined MAJOR_IN_SYSMACROS
#include <sys/sysmacros.h>
#endif

#include "utils/mono-logger-internals.h"

#ifndef MAXPATHLEN
#define MAXPATHLEN 242
#endif

gchar*
mono_w32process_get_name (pid_t pid)
{
	FILE *fp;
	gchar *filename;
	gchar buf[256];
	gchar *ret = NULL;

#if defined(HOST_SOLARIS)
	filename = g_strdup_printf ("/proc/%d/psinfo", pid);
	if ((fp = fopen (filename, "r")) != NULL) {
		struct psinfo info;
		int nread;

		nread = fread (&info, sizeof (info), 1, fp);
		if (nread == 1) {
			ret = g_strdup (info.pr_fname);
		}

		fclose (fp);
	}
	g_free (filename);
#else
	memset (buf, '\0', sizeof(buf));
	filename = g_strdup_printf ("/proc/%d/exe", pid);
	if (readlink (filename, buf, 255) > 0) {
		ret = g_strdup (buf);
	}
	g_free (filename);

	if (ret != NULL) {
		return(ret);
	}

	filename = g_strdup_printf ("/proc/%d/cmdline", pid);
	if ((fp = fopen (filename, "r")) != NULL) {
		if (fgets (buf, 256, fp) != NULL) {
			ret = g_strdup (buf);
		}

		fclose (fp);
	}
	g_free (filename);

	if (ret != NULL) {
		return(ret);
	}

	filename = g_strdup_printf ("/proc/%d/stat", pid);
	if ((fp = fopen (filename, "r")) != NULL) {
		if (fgets (buf, 256, fp) != NULL) {
			char *start, *end;

			start = strchr (buf, '(');
			if (start != NULL) {
				end = strchr (start + 1, ')');

				if (end != NULL) {
					ret = g_strndup (start + 1,
							 end - start - 1);
				}
			}
		}

		fclose (fp);
	}
	g_free (filename);
#endif

	return ret;
}

gchar*
mono_w32process_get_path (pid_t pid)
{
	return mono_w32process_get_name (pid);
}

static FILE *
open_process_map (int pid, const char *mode)
{
	gint i;
	const gchar *proc_path[] = {
		"/proc/%d/maps", /* GNU/Linux */
		"/proc/%d/map",  /* FreeBSD */
		NULL
	};

	for (i = 0; proc_path [i]; i++) {
		gchar *filename;
		FILE *fp;

		filename = g_strdup_printf (proc_path[i], pid);
		fp = fopen (filename, mode);
		g_free (filename);

		if (fp)
			return fp;
	}

	return NULL;
}


GSList*
mono_w32process_get_modules (pid_t pid)
{
	GSList *ret = NULL;
	FILE *fp;
	MonoW32ProcessModule *mod;
	gchar buf[MAXPATHLEN + 1], *p, *endp;
	gchar *start_start, *end_start, *prot_start, *offset_start;
	gchar *maj_dev_start, *min_dev_start, *inode_start, prot_buf[5];
	gpointer address_start, address_end, address_offset;
	guint32 maj_dev, min_dev;
	guint64 inode;
	guint64 device;

	fp = open_process_map (pid, "r");
	if (!fp) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_PROCESS, "%s: Can't open process map file for pid %d", __func__, pid);
		return NULL;
	}

	while (fgets (buf, sizeof(buf), fp)) {
		p = buf;
		while (g_ascii_isspace (*p)) ++p;
		start_start = p;
		if (!g_ascii_isxdigit (*start_start)) {
			continue;
		}
		address_start = (gpointer)strtoul (start_start, &endp, 16);
		p = endp;
		if (*p != '-') {
			continue;
		}

		++p;
		end_start = p;
		if (!g_ascii_isxdigit (*end_start)) {
			continue;
		}
		address_end = (gpointer)strtoul (end_start, &endp, 16);
		p = endp;
		if (!g_ascii_isspace (*p)) {
			continue;
		}

		while (g_ascii_isspace (*p)) ++p;
		prot_start = p;
		if (*prot_start != 'r' && *prot_start != '-') {
			continue;
		}
		memcpy (prot_buf, prot_start, 4);
		prot_buf[4] = '\0';
		while (!g_ascii_isspace (*p)) ++p;

		while (g_ascii_isspace (*p)) ++p;
		offset_start = p;
		if (!g_ascii_isxdigit (*offset_start)) {
			continue;
		}
		address_offset = (gpointer)strtoul (offset_start, &endp, 16);
		p = endp;
		if (!g_ascii_isspace (*p)) {
			continue;
		}

		while(g_ascii_isspace (*p)) ++p;
		maj_dev_start = p;
		if (!g_ascii_isxdigit (*maj_dev_start)) {
			continue;
		}
		maj_dev = strtoul (maj_dev_start, &endp, 16);
		p = endp;
		if (*p != ':') {
			continue;
		}

		++p;
		min_dev_start = p;
		if (!g_ascii_isxdigit (*min_dev_start)) {
			continue;
		}
		min_dev = strtoul (min_dev_start, &endp, 16);
		p = endp;
		if (!g_ascii_isspace (*p)) {
			continue;
		}

		while (g_ascii_isspace (*p)) ++p;
		inode_start = p;
		if (!g_ascii_isxdigit (*inode_start)) {
			continue;
		}
		inode = (guint64)strtol (inode_start, &endp, 10);
		p = endp;
		if (!g_ascii_isspace (*p)) {
			continue;
		}

		device = makedev ((int)maj_dev, (int)min_dev);
		if ((device == 0) && (inode == 0)) {
			continue;
		}

		while(g_ascii_isspace (*p)) ++p;
		/* p now points to the filename */

		mod = g_new0 (MonoW32ProcessModule, 1);
		mod->address_start = address_start;
		mod->address_end = address_end;
		mod->perms = g_strdup (prot_buf);
		mod->address_offset = address_offset;
		mod->device = device;
		mod->inode = inode;
		mod->filename = g_strdup (g_strstrip (p));

		if (g_slist_find_custom (ret, mod, mono_w32process_module_equals) == NULL) {
			ret = g_slist_prepend (ret, mod);
		} else {
			mono_w32process_module_free (mod);
		}
	}

	ret = g_slist_reverse (ret);

	fclose (fp);

	return(ret);
}

#endif
