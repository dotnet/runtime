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

#define _WAPI_SHM_SCRATCH_SIZE 409600

extern gpointer _wapi_shm_attach (gboolean daemon, gboolean *success,
				  int *shm_id);
extern void _wapi_shm_destroy (void);

#endif /* _WAPI_SHARED_H_ */
