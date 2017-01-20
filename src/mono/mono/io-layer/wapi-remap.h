/*
 * wapi-remap.h: io-layer symbol remapping support
 *
 * (C) 2014 Xamarin, Inc.
 */

#ifndef __WAPI_REMAP_H__
#define __WAPI_REMAP_H__

/*
 * The windows function names used by the io-layer can collide with symbols in system and 3rd party libs, esp. on osx/ios. So remap them to
 * wapi_<funcname>.
 */

#define GetLastError wapi_GetLastError
#define SetLastError wapi_SetLastError
#define CloseHandle wapi_CloseHandle 

#endif /* __WAPI_REMAP_H__ */
