#ifndef _WAPI_MISC_PRIVATE_H_
#define _WAPI_MISC_PRIVATE_H_

#include <glib.h>
#include <sys/time.h>

extern void _wapi_calc_timeout(struct timespec *timeout, guint32 ms);

#endif /* _WAPI_MISC_PRIVATE_H_ */
