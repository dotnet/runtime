/**
 * \file
 */

#ifndef _MONO_METADATA_W32ERROR_H_
#define _MONO_METADATA_W32ERROR_H_

#include <config.h>
#include <glib.h>

#if !defined(HOST_WIN32)

#define ERROR_SUCCESS              0
#define ERROR_FILE_NOT_FOUND       2
#define ERROR_PATH_NOT_FOUND       3
#define ERROR_TOO_MANY_OPEN_FILES  4
#define ERROR_ACCESS_DENIED        5
#define ERROR_INVALID_HANDLE       6
#define ERROR_NOT_ENOUGH_MEMORY    8
#define ERROR_BAD_FORMAT           11
#define ERROR_INVALID_ACCESS       12
#define ERROR_INVALID_DATA         13
#define ERROR_OUTOFMEMORY          14
#define ERROR_NOT_SAME_DEVICE      17
#define ERROR_NO_MORE_FILES        18
#define ERROR_BAD_LENGTH           24
#define ERROR_SEEK                 25
#define ERROR_WRITE_FAULT          29
#define ERROR_GEN_FAILURE          31
#define ERROR_SHARING_VIOLATION    32
#define ERROR_LOCK_VIOLATION       33
#define ERROR_HANDLE_DISK_FULL     39
#define ERROR_NOT_SUPPORTED        50
#define ERROR_DEV_NOT_EXIST        55
#define ERROR_FILE_EXISTS          80
#define ERROR_CANNOT_MAKE          82
#define ERROR_INVALID_PARAMETER    87
#define ERROR_INVALID_NAME         123
#define ERROR_PROC_NOT_FOUND       127
#define ERROR_DIR_NOT_EMPTY        145
#define ERROR_ALREADY_EXISTS       183
#define ERROR_BAD_EXE_FORMAT       193
#define ERROR_FILENAME_EXCED_RANGE 206
#define ERROR_DIRECTORY            267
#define ERROR_IO_PENDING           997
#define ERROR_CANT_RESOLVE_FILENAME 1921
#define ERROR_ENCRYPTION_FAILED    6000
#define WSAEINTR                   10004
#define WSAEBADF                   10009
#define WSAEACCES                  10013
#define WSAEFAULT                  10014
#define WSAEINVAL                  10022
#define WSAEMFILE                  10024
#define WSAEWOULDBLOCK             10035
#define WSAEINPROGRESS             10036
#define WSAEALREADY                10037
#define WSAENOTSOCK                10038
#define WSAEDESTADDRREQ            10039
#define WSAEMSGSIZE                10040
#define WSAEPROTOTYPE              10041
#define WSAENOPROTOOPT             10042
#define WSAEPROTONOSUPPORT         10043
#define WSAESOCKTNOSUPPORT         10044
#define WSAEOPNOTSUPP              10045
#define WSAEAFNOSUPPORT            10047
#define WSAEADDRINUSE              10048
#define WSAEADDRNOTAVAIL           10049
#define WSAENETDOWN                10050
#define WSAENETUNREACH             10051
#define WSAECONNRESET              10054
#define WSAENOBUFS                 10055
#define WSAEISCONN                 10056
#define WSAENOTCONN                10057
#define WSAESHUTDOWN               10058
#define WSAETIMEDOUT               10060
#define WSAECONNREFUSED            10061
#define WSAEHOSTDOWN               10064
#define WSAEHOSTUNREACH            10065
#define WSASYSCALLFAILURE          10107
#define WSAENXIO                   100001

#endif

guint32
mono_w32error_get_last (void);

void
mono_w32error_set_last (guint32 error);

guint32
mono_w32error_unix_to_win32 (guint32 error);

#endif /* _MONO_METADATA_W32ERROR_H_ */
