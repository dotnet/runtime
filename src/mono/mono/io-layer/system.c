#include <config.h>
#include <glib.h>
#include <sys/time.h>
#include <stdlib.h>
#include <unistd.h>

#include "mono/io-layer/wapi.h"

void GetSystemInfo(WapiSystemInfo *info)
{
	info->dwPageSize=getpagesize();

	/* Fill in the rest of this junk. Maybe with libgtop */
	info->dwNumberOfProcessors=1;
}


