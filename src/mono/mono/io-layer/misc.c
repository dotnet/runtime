#include <config.h>
#include <glib.h>
#include <sys/time.h>
#include <stdlib.h>

#include "misc-private.h"

void _wapi_calc_timeout(struct timespec *timeout, guint32 ms)
{
	struct timeval now;
	div_t divvy;
		
	divvy=div((int)ms, 1000);
	gettimeofday(&now, NULL);
		
	timeout->tv_sec=now.tv_sec+divvy.quot;
	timeout->tv_nsec=(now.tv_usec+divvy.rem)*1000;
}
