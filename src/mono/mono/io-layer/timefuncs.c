#include <config.h>
#include <glib.h>
#include <sys/time.h>
#include <stdlib.h>

#include "mono/io-layer/wapi.h"

gboolean QueryPerformanceCounter(WapiLargeInteger *count G_GNUC_UNUSED)
{
	return(FALSE);
}

gboolean QueryPerformanceFrequency(WapiLargeInteger *freq G_GNUC_UNUSED)
{
	return(FALSE);
}

