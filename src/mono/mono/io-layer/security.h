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

G_BEGIN_DECLS

extern gboolean ImpersonateLoggedOnUser (gpointer handle);
extern gboolean RevertToSelf (void);

G_END_DECLS

#endif /* _WAPI_SECURITY_H_ */
