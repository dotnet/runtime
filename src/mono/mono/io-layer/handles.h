/*
 * handles.h:  Generic operations on handles
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *
 * (C) 2002 Ximian, Inc.
 */

#ifndef _WAPI_HANDLES_H_
#define _WAPI_HANDLES_H_

#define INVALID_HANDLE_VALUE (gpointer)-1

G_BEGIN_DECLS

extern gboolean CloseHandle (gpointer handle);
extern gboolean DuplicateHandle (gpointer srcprocess, gpointer src, gpointer targetprocess, gpointer *target, guint32 access, gboolean inherit, guint32 options);

/* Another kludge alert! Visible non-w32 API is broken! */
extern void _wapi_cleanup (void);

G_END_DECLS

#endif /* _WAPI_HANDLES_H_ */
