/**
 * \file
 */

#ifndef __MONO_NETWORK_INTERFACES_H__
#define __MONO_NETWORK_INTERFACES_H__
/*
 * Utility functions to access network information.
 */

#include <glib.h>
#include <mono/utils/mono-compiler.h>

/* never remove or reorder these enums values: they are used in corlib/System */

typedef enum {
	MONO_NETWORK_BYTESREC,
	MONO_NETWORK_BYTESSENT,
	MONO_NETWORK_BYTESTOTAL
} MonoNetworkData;

typedef enum {
	MONO_NETWORK_ERROR_NONE, /* no error happened */
	MONO_NETWORK_ERROR_NOT_FOUND, /* adapter name invalid */
	MONO_NETWORK_ERROR_OTHER
} MonoNetworkError;

gpointer *mono_networkinterface_list (int *size);
gint64    mono_network_get_data (char* name, MonoNetworkData data, MonoNetworkError *error);

#endif /* __MONO_NETWORK_INTERFACES_H__ */

