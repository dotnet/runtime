/*
 * file-io-windows.c: Windows File IO internal calls.
 *
 * Copyright 2016 Microsoft
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */
#include <config.h>
#include <glib.h>

#if defined(HOST_WIN32)
#include <winsock2.h>
#include <windows.h>
#include "mono/metadata/file-io-windows-internals.h"

gunichar2
ves_icall_System_IO_MonoIO_get_VolumeSeparatorChar ()
{
	return (gunichar2) ':';	/* colon */
}

gunichar2
ves_icall_System_IO_MonoIO_get_DirectorySeparatorChar ()
{
	return (gunichar2) '\\';	/* backslash */
}

gunichar2
ves_icall_System_IO_MonoIO_get_AltDirectorySeparatorChar ()
{
	return (gunichar2) '/';	/* forward slash */
}

gunichar2
ves_icall_System_IO_MonoIO_get_PathSeparator ()
{
	return (gunichar2) ';';	/* semicolon */
}

void ves_icall_System_IO_MonoIO_DumpHandles (void)
{
	return;
}
#endif /* HOST_WIN32 */
