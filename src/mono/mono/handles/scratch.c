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
	/* Fix if this changes in shared.c */
	guint32 scratch_size=0;
	struct _WapiScratchHeader *hdr;
	
	_wapi_shared_data=_wapi_shm_attach (&scratch_size);
	
	_wapi_handle_shared_lock ();

	hdr=(struct _WapiScratchHeader *)&_wapi_shared_data->scratch_base[0];
	if(hdr->flags==0 && hdr->length==0) {
		g_print ("Scratch space unused\n");
		_wapi_handle_shared_unlock ();
		exit (0);
	}
	
	while(idx<scratch_size) {
		hdr=(struct _WapiScratchHeader *)&_wapi_shared_data->scratch_base[idx];
		if(hdr->flags & WAPI_SHM_SCRATCH_FREE) {
			g_print ("Free block at %6d (index %6d), length %6d\n",
				 idx, idx+HDRSIZE, hdr->length);
		} else {
			guchar *data=&_wapi_shared_data->scratch_base[idx+HDRSIZE];
			
			g_print ("Used block at %6d (index %6d), length %6d, [%s]\n",
				 idx, idx+HDRSIZE, hdr->length,
				 printable (data, hdr->length));
		}

		idx+=(hdr->length+HDRSIZE);
	}
	
	_wapi_handle_shared_unlock ();
	
	exit (0);
}
