/*
 * shared.h:  Shared memory handle, and daemon launching
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *
 * (C) 2002 Ximian, Inc.
 */

#ifndef _WAPI_SHARED_H_
#define _WAPI_SHARED_H_

#include <mono/io-layer/wapi-private.h>

struct _WapiScratchHeader 
{
	/* These two can be merged */
	guint32 flags;
	guint32 length;
};

enum {
	WAPI_SHM_SCRATCH_FREE=0x1,
};

typedef enum {
	WAPI_SHM_DATA,
	WAPI_SHM_SCRATCH
} _wapi_shm_t;

extern guchar *_wapi_shm_file (_wapi_shm_t type, guint32 segment);
extern gpointer _wapi_shm_file_map (_wapi_shm_t type, guint32 segment,
				    gboolean *created, off_t *size);
extern gpointer _wapi_shm_file_expand (gpointer mem, _wapi_shm_t type,
				       guint32 segment, guint32 old_len,
				       guint32 new_len);
extern gboolean _wapi_shm_attach (struct _WapiHandleShared_list **data,
				  struct _WapiHandleScratch **scratch);
extern void _wapi_shm_destroy (void);

#endif /* _WAPI_SHARED_H_ */
