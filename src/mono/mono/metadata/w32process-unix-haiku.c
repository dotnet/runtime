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

GSList*
mono_w32process_get_modules (pid_t pid)
{
	GSList *ret = NULL;
	MonoW32ProcessModule *mod;
	gint32 cookie = 0;
	image_info imageInfo;

	while (get_next_image_info (B_CURRENT_TEAM, &cookie, &imageInfo) == B_OK) {
		mod = g_new0 (MonoW32ProcessModule, 1);
		mod->device = imageInfo.device;
		mod->inode = imageInfo.node;
		mod->filename = g_strdup (imageInfo.name);
		mod->address_start = MIN (imageInfo.text, imageInfo.data);
		mod->address_end = MAX ((uint8_t*)imageInfo.text + imageInfo.text_size,
			(uint8_t*)imageInfo.data + imageInfo.data_size);
		mod->perms = g_strdup ("r--p");
		mod->address_offset = 0;

		if (g_slist_find_custom (ret, mod, mono_w32process_module_equals) == NULL) {
			ret = g_slist_prepend (ret, mod);
		} else {
			mono_w32process_module_free (mod);
		}
	}

	return g_slist_reverse (ret);
}

#endif
