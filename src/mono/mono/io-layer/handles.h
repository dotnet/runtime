#ifndef _WAPI_HANDLES_H_
#define _WAPI_HANDLES_H_

#define INVALID_HANDLE_VALUE (WapiHandle *)-1

typedef struct _WapiHandle WapiHandle;

extern gboolean CloseHandle(WapiHandle *handle);

#endif /* _WAPI_HANDLES_H_ */
