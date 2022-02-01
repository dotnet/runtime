/**
 * \file
 */

#include "config.h"
#include "utils/mono-networkinterfaces.h"

#include <stdlib.h>
#include <stdio.h>
#include <string.h>

/* FIXME: bsds untested */

/**
 * mono_networkinterface_list:
 * \param size a pointer to a location where the size of the returned array is stored
 * \returns an array of names for the interfaces currently on the system.
 * The size of the array is stored in \p size.
 */
gpointer*
mono_networkinterface_list (int *size)
{
	int i = 0, count = 0;
	void **nilist = NULL;
	char buf [512];
	FILE *f;
	char name [256];

	f = fopen ("/proc/net/dev", "r");
	if (!f) 
		return NULL;

	if (!fgets (buf, sizeof (buf) / sizeof (char), f))
		goto out;

	if (!fgets (buf, sizeof (buf) / sizeof (char), f))
		goto out;

	while (fgets (buf, sizeof (buf), f) != NULL) {
		char *ptr;
		buf [sizeof(buf) - 1] = 0;
		if ((ptr = strchr (buf, ':')) == NULL || (*ptr++ = 0, sscanf (buf, "%s", name) != 1))
			goto out;

		if (i >= count) {
			if (!count)
				count = 16;
			else
				count *= 2;
		}

		nilist = (void **) g_realloc (nilist, count * sizeof (void*));
		nilist [i++] = g_strdup (name);
	}

 out:
	if (f) fclose(f);
	if (size)
		*size = i;

	if (!nilist)
		nilist = (void **) g_malloc (sizeof (void*));
	nilist [i] = NULL;
	return nilist;
}

/**
 * mono_network_get_data:
 * \param name name of the interface
 * \param data description of data to return
 * \return a data item of a network adapter like bytes sent per sec, etc
 * according to the \p data argumet.
 */
gint64
mono_network_get_data (char* name, MonoNetworkData data, MonoNetworkError *error)
{
	gint64 val = 0;
	char buf [512];
	char cname [256];
	FILE *f;

	unsigned long rx_bytes, rx_packets, rx_errs, rx_drops,
		rx_fifo, rx_frame, tx_bytes, tx_packets, tx_errs, tx_drops,
		tx_fifo, tx_colls, tx_carrier, rx_multi;

	*error = MONO_NETWORK_ERROR_OTHER;

	f = fopen ("/proc/net/dev", "r");
	if (!f) 
		return -1;

	if (!fgets (buf, sizeof (buf) / sizeof (char), f))
		goto out;

	if (!fgets (buf, sizeof (buf) / sizeof (char), f))
		goto out;

	while (fgets (buf, sizeof (buf), f) != NULL) {

		char *ptr;
		buf [sizeof (buf) - 1] = 0;
		if ((ptr = strchr (buf, ':')) == NULL ||
				(*ptr++ = 0, sscanf (buf, "%250s", cname) != 1))
			goto out;

		if (strcmp (name, cname) != 0) continue;

		if (sscanf (ptr, "%ld%ld%ld%ld%ld%ld%ld%*d%ld%ld%ld%ld%ld%ld%ld",
							 &rx_bytes, &rx_packets, &rx_errs, &rx_drops,
							 &rx_fifo, &rx_frame, &rx_multi,
							 &tx_bytes, &tx_packets, &tx_errs, &tx_drops,
							 &tx_fifo, &tx_colls, &tx_carrier) != 14) 
			goto out;

		switch (data) {
		case MONO_NETWORK_BYTESSENT:
			val = tx_bytes;
			*error = MONO_NETWORK_ERROR_NONE;
			goto out;
		case MONO_NETWORK_BYTESREC:
			val = rx_bytes;
			*error = MONO_NETWORK_ERROR_NONE;
			goto out;
		case MONO_NETWORK_BYTESTOTAL:
			val = rx_bytes + tx_bytes;
			*error = MONO_NETWORK_ERROR_NONE;
			goto out;
		}
	}

 out:
	if (f) fclose (f);
	return val;
}

