#ifndef _WAPI_SHARED_H_
#define _WAPI_SHARED_H_

struct _WapiScratchHeader 
{
	/* These two can be merged */
	guint32 flags;
	guint32 length;
};

enum {
	WAPI_SHM_SCRATCH_FREE=0x1,
};

extern gpointer _wapi_shm_attach (guint32 *scratch_size);
extern void _wapi_shm_destroy (void);

extern guint32 _wapi_shm_scratch_store (guchar *storage, gconstpointer data,
					guint32 len);
extern guchar *_wapi_shm_scratch_lookup_as_string (guchar *storage,
						   guint32 idx);
extern void _wapi_shm_scratch_delete (guchar *storage, guint32 idx);

#endif /* _WAPI_SHARED_H_ */
