#ifndef _WAPI_IO_PRIVATE_H_
#define _WAPI_IO_PRIVATE_H_

#include <config.h>
#include <glib.h>
#include <glob.h>

extern struct _WapiHandleOps _wapi_file_ops;
extern struct _WapiHandleOps _wapi_console_ops;
extern struct _WapiHandleOps _wapi_find_ops;

/* Currently used for both FILE and CONSOLE handle types.  This may
 * have to change in future.
 */
struct _WapiHandle_file
{
	guint32 filename;
	guint32 security_attributes;
	guint32 fileaccess;
	guint32 sharemode;
	guint32 attrs;
};

struct _WapiHandlePrivate_file
{
	int fd;
};

struct _WapiHandle_find
{
	glob_t glob;
	size_t count;
};

struct _WapiHandlePrivate_find
{
	int dummy;
};


#endif /* _WAPI_IO_PRIVATE_H_ */
