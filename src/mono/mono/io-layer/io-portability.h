/*
 * io-portability.h:	Optional filename mangling to try to cope with
 *			badly-written non-portable windows apps
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *
 * Copyright (C) 2006 Novell, Inc.
 */

#ifndef _WAPI_IO_PORTABILITY_H_
#define _WAPI_IO_PORTABILITY_H_

#include <glib.h>
#include <sys/types.h>
#include <utime.h>
#include <sys/stat.h>
#include <unistd.h>

G_BEGIN_DECLS

extern int _wapi_open (const char *pathname, int flags, mode_t mode);
extern int _wapi_access (const char *pathname, int mode);
extern int _wapi_chmod (const char *pathname, mode_t mode);
extern int _wapi_utime (const char *filename, const struct utimbuf *buf);
extern int _wapi_unlink (const char *pathname);
extern int _wapi_rename (const char *oldpath, const char *newpath);
extern int _wapi_stat (const char *path, struct stat *buf);
extern int _wapi_lstat (const char *path, struct stat *buf);
extern int _wapi_mkdir (const char *pathname, mode_t mode);
extern int _wapi_rmdir (const char *pathname);
extern int _wapi_chdir (const char *path);
extern gchar *_wapi_basename (const gchar *filename);
extern gchar *_wapi_dirname (const gchar *filename);
extern GDir *_wapi_g_dir_open (const gchar *path, guint flags, GError **error);
extern gint _wapi_io_scandir (const gchar *dirname, const gchar *pattern,
			      gchar ***namelist);

G_END_DECLS

#endif /* _WAPI_IO_PORTABILITY_H_ */
