/**
 * \file
 */

#include "w32process.h"
#include "w32process-unix-internals.h"

#ifdef USE_HAIKU_BACKEND

/* KernelKit.h doesn't include the right headers? */
#include <os/kernel/image.h>

gchar*
mono_w32process_get_name (pid_t pid)
{
	image_info imageInfo;
	int32 cookie = 0;

	if (get_next_image_info ((team_id) pid, &cookie, &imageInfo) != B_OK)
		return NULL;

	return g_strdup (imageInfo.name);
}

gchar*
mono_w32process_get_path (pid_t pid)
{
	return mono_w32process_get_name (pid);
}


#else

MONO_EMPTY_SOURCE_FILE (w32process_unix_haiku);

#endif
