#include <config.h>
#include <glib.h>
#include <unistd.h>

#include <mono/io-layer/io-layer.h>

/* We're digging into handle internals here... */
#include <mono/io-layer/handles-private.h>
#include <mono/io-layer/wapi-private.h>

#define HDRSIZE sizeof(struct _WapiScratchHeader)

static guchar *printable (guchar *data, guint32 datalen)
{
	static guchar buf[32];
	guint32 i;
	
	if(datalen>=32) {
		datalen=31;
	}

	for(i=0; i<datalen; i++) {
		if(g_ascii_isprint (data[i])) {
			buf[i]=data[i];
		} else {
			buf[i]='.';
		}
	}
	buf[i]='\0';

	return(buf);
}

int main (int argc, char **argv)
{
	guint32 idx=0;
	struct _WapiScratchHeader *hdr;
	gboolean success;
	
	_wapi_shared_data=g_new0 (struct _WapiHandleShared_list *, 1);
	_wapi_shared_scratch=g_new0 (struct _WapiHandleScratch, 1);
	
	success=_wapi_shm_attach (&_wapi_shared_data[0], &_wapi_shared_scratch);
	if(success==FALSE) {
		g_error ("Failed to attach shared memory!");
		exit (-1);
	}

	hdr=(struct _WapiScratchHeader *)&_wapi_shared_scratch->scratch_data;
	if(hdr->flags==0 && hdr->length==0) {
		g_print ("Scratch space unused\n");
		exit (0);
	}
	
	while(idx < _wapi_shared_scratch->data_len) {
		hdr=(struct _WapiScratchHeader *)&_wapi_shared_scratch->scratch_data[idx];
		if(hdr->flags & WAPI_SHM_SCRATCH_FREE) {
			g_print ("Free block at %6d (index %6d), length %6d\n",
				 idx, idx+HDRSIZE, hdr->length);
		} else {
			guchar *data=&_wapi_shared_scratch->scratch_data[idx+HDRSIZE];
			
			g_print ("Used block at %6d (index %6d), length %6d, [%s]\n",
				 idx, idx+HDRSIZE, hdr->length,
				 printable (data, hdr->length));
		}

		idx+=(hdr->length+HDRSIZE);
	}
	
	exit (0);
}
