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

#else

MONO_EMPTY_SOURCE_FILE (w32process_unix_default);

#endif
