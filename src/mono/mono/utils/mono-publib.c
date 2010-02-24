#include "config.h"
#include <mono/utils/mono-publib.h>
#include <glib.h>

void
mono_free (void *ptr)
{
	g_free (ptr);
}

