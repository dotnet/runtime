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

#ifdef _AIX
/* like solaris, just different */
#include <sys/procfs.h>
/* fallback for procfs-less i */
#include <procinfo.h>
#include <sys/types.h>
#endif

/* makedev() macro */
#ifdef MAJOR_IN_MKDEV
#include <sys/mkdev.h>
#elif defined MAJOR_IN_SYSMACROS
#include <sys/sysmacros.h>
#endif

#include "utils/mono-logger-internals.h"
#include "icall-decl.h"

#ifndef MAXPATHLEN
#define MAXPATHLEN 242
#endif

/* XXX: why don't we just use proclib? */
gchar*
mono_w32process_get_name (pid_t pid)
{
	FILE *fp;
	gchar *filename;
	gchar buf[256];
	gchar *ret = NULL;

#if defined(HOST_SOLARIS) || (defined(_AIX) && !defined(__PASE__))
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
#elif defined(__PASE__)
	/* AIX has a procfs, but it's not available on i */
	struct procentry64 proc;
	pid_t newpid;

	newpid = pid;
	if (getprocs64(&proc, sizeof (proc), NULL, NULL, &newpid, 1) == 1) {
		ret = g_strdup (proc.pi_comm);
	}
#else
	memset (buf, '\0', sizeof(buf));
	filename = g_strdup_printf ("/proc/%d/exe", pid);
#if defined(HAVE_READLINK)
	if (readlink (filename, buf, 255) > 0) {
		ret = g_strdup (buf);
	}
#endif
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
#if defined(_AIX)
	/* due to procfs, this won't work on i */
	GSList *ret = NULL;
	FILE *fp;
	MonoW32ProcessModule *mod;
	struct prmap module;
	int i;
	fpos64_t curpos;

	char pidpath[32]; /* "/proc/<uint64_t max>/map" plus null, rounded */
	char libpath[MAXPATHLEN + 1];
	char membername[MAXPATHLEN + 1];
	char combinedname[(MAXPATHLEN * 2) + 3]; /* lib, member, (), and nul */

	sprintf (pidpath, "/proc/%d/map", pid);
	if ((fp = fopen(pidpath, "r"))) {
		while (fread (&module, sizeof (module), 1, fp) == 1
			/* proc(4) declares such a struct to be the array terminator */
			&& (module.pr_size != 0 && module.pr_mflags != 0)
			&& (module.pr_mflags & MA_READ)) {

			fgetpos64 (fp, &curpos); /* save our position */
			fseeko (fp, module.pr_pathoff, SEEK_SET);
			while ((libpath[i++] = fgetc (fp)));
			i = 0;
			while ((membername[i++] = fgetc (fp)));
			i = 0;
			fsetpos64 (fp, &curpos); /* back to normal */

			mod = g_new0 (MonoW32ProcessModule, 1);
			mod->address_start = module.pr_vaddr;
			mod->address_end = module.pr_vaddr + module.pr_size;
			mod->address_offset = (void*)module.pr_off;
			mod->perms = g_strdup ("r--p"); /* XXX? */

			/* AIX has what appears to be device, channel and inode information,
			 * but it's in a string. Try parsing it.
			 *
			 * XXX: I believe it's fstype.devno.chano.inode, but I'm uncertain
			 * as to how that maps out, so I only fill in the inode (like BSD)
			 */
			sscanf (module.pr_mapname, "%*[^.].%*lu.%*u.%lu", &(mod->inode));

			if (membername[0]) {
				snprintf(combinedname, MAXPATHLEN, "%s(%s)", libpath, membername); 
				mod->filename = g_strdup (combinedname);
			} else {
				mod->filename = g_strdup (libpath);
			}

			if (g_slist_find_custom (ret, mod, mono_w32process_module_equals) == NULL) {
				ret = g_slist_prepend (ret, mod);
			} else {
				mono_w32process_module_free (mod);
			}
		}
	} else {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_PROCESS, "%s: Can't open process map file for pid %d", __func__, pid);
		return NULL;
	}

	if (ret)
		ret = g_slist_reverse (ret);

	fclose (fp);

	return(ret);
#else
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
#if defined(MAJOR_IN_MKDEV) || defined(MAJOR_IN_SYSMACROS)
		device = makedev ((int)maj_dev, (int)min_dev);
#else
		(void)maj_dev;
		(void)min_dev;
		device = 0;
#endif
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
#endif
}

void
mono_w32process_platform_init_once (void)
{
}

#else

MONO_EMPTY_SOURCE_FILE (w32process_unix_default);

#endif
