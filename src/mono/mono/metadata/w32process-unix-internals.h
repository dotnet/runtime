/**
 * \file
 */

#ifndef _MONO_METADATA_W32PROCESS_UNIX_INTERNALS_H_
#define _MONO_METADATA_W32PROCESS_UNIX_INTERNALS_H_

#include <config.h>
#include <glib.h>

/*
 * FOR EXCLUSIVE USE BY w32process-unix.c
 */

#if defined(HOST_DARWIN)
#define USE_OSX_BACKEND
#elif (defined(__OpenBSD__) || defined(__FreeBSD__)) && defined(HAVE_LINK_H)
#define USE_BSD_BACKEND
#elif defined(__HAIKU__)
#define USE_HAIKU_BACKEND
/* Define header for team_info */
#include <os/kernel/OS.h>
#else
#define USE_DEFAULT_BACKEND
#endif

gchar*
mono_w32process_get_name (pid_t pid);

#endif /* _MONO_METADATA_W32PROCESS_UNIX_INTERNALS_H_ */
