#ifndef _WAPI_TYPES_H_
#define _WAPI_TYPES_H_

#include <glib.h>

typedef union 
{
	struct 
	{
		guint32 LowPart;
		gint32 HighPart;
	} u;
	
	guint64 QuadPart;
} WapiLargeInteger;

#endif /* _WAPI_TYPES_H_ */
