/*
 * security.h:  Security
 *
 * Author:
 *	Sebastien Pouliot  <sebastien@ximian.com>
 *
 * (C) 2004 Novell (http://www.novell.com)
 */

#ifndef _WAPI_SECURITY_H_
#define _WAPI_SECURITY_H_

#include <glib.h>

extern gboolean ImpersonateLoggedOnUser (gpointer handle);
extern gboolean RevertToSelf (void);

#endif /* _WAPI_SECURITY_H_ */
