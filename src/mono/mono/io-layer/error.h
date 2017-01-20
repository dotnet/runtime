/*
 * error.h:  Error reporting
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *
 * (C) 2002 Ximian, Inc.
 */

#ifndef _WAPI_ERROR_H_
#define _WAPI_ERROR_H_

typedef enum {
	ERROR_SUCCESS              = 0,
	ERROR_FILE_NOT_FOUND       = 2,
	ERROR_PATH_NOT_FOUND       = 3,
	ERROR_TOO_MANY_OPEN_FILES  = 4,
	ERROR_ACCESS_DENIED        = 5,
	ERROR_INVALID_HANDLE       = 6,
	ERROR_NOT_ENOUGH_MEMORY    = 8,
	ERROR_BAD_FORMAT           = 11,
	ERROR_INVALID_ACCESS       = 12,
	ERROR_INVALID_DATA         = 13,
	ERROR_OUTOFMEMORY          = 14,
	ERROR_NOT_SAME_DEVICE      = 17,
	ERROR_NO_MORE_FILES        = 18,
	ERROR_BAD_LENGTH           = 24,
	ERROR_SEEK                 = 25,
	ERROR_WRITE_FAULT          = 29,
	ERROR_GEN_FAILURE          = 31,
	ERROR_SHARING_VIOLATION    = 32,
	ERROR_LOCK_VIOLATION       = 33,
	ERROR_HANDLE_DISK_FULL     = 39,
	ERROR_NOT_SUPPORTED        = 50,
	ERROR_FILE_EXISTS          = 80,
	ERROR_CANNOT_MAKE          = 82,
	ERROR_INVALID_PARAMETER    = 87,
	ERROR_INVALID_NAME         = 123,
	ERROR_PROC_NOT_FOUND       = 127,
	ERROR_DIR_NOT_EMPTY        = 145,
	ERROR_ALREADY_EXISTS       = 183,
	ERROR_BAD_EXE_FORMAT       = 193,
	ERROR_FILENAME_EXCED_RANGE = 206,
	ERROR_DIRECTORY            = 267,
	ERROR_IO_PENDING           = 997,
	ERROR_ENCRYPTION_FAILED    = 6000,
	WSAEINTR                   = 10004,
	WSAEBADF                   = 10009,
	WSAEACCES                  = 10013,
	WSAEFAULT                  = 10014,
	WSAEINVAL                  = 10022,
	WSAEMFILE                  = 10024,
	WSAEWOULDBLOCK             = 10035,
	WSAEINPROGRESS             = 10036,
	WSAEALREADY                = 10037,
	WSAENOTSOCK                = 10038,
	WSAEDESTADDRREQ            = 10039,
	WSAEMSGSIZE                = 10040,
	WSAENOPROTOOPT             = 10042,
	WSAEPROTONOSUPPORT         = 10043,
	WSAESOCKTNOSUPPORT         = 10044,
	WSAEOPNOTSUPP              = 10045,
	WSAEAFNOSUPPORT            = 10047,
	WSAEADDRINUSE              = 10048,
	WSAEADDRNOTAVAIL           = 10049,
	WSAENETDOWN                = 10050,
	WSAENETUNREACH             = 10051,
	WSAECONNRESET              = 10054,
	WSAENOBUFS                 = 10055,
	WSAEISCONN                 = 10056,
	WSAENOTCONN                = 10057,
	WSAESHUTDOWN               = 10058,
	WSAETIMEDOUT               = 10060,
	WSAECONNREFUSED            = 10061,
	WSAEHOSTDOWN               = 10064,
	WSAEHOSTUNREACH            = 10065,
	WSASYSCALLFAILURE          = 10107,
} WapiError;

G_BEGIN_DECLS

guint32 GetLastError (void);
void SetLastError (guint32 code);
gint _wapi_get_win32_file_error (gint err);

G_END_DECLS

#endif /* _WAPI_ERROR_H_ */
