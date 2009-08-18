/*
 * security.c:  Security
 *
 * Author:
 *	Sebastien Pouliot  <sebastien@ximian.com>
 *
 * (C) 2004 Novell (http://www.novell.com)
 */

#include <config.h>
#include <mono/io-layer/io-layer.h>

#ifdef HAVE_PWD_H
#include <pwd.h>
#endif

#include <string.h>
#include <sys/types.h>
#include <sys/stat.h>
#include <unistd.h>


/* Disclaimers */

#if defined(__GNUC__)
#ifndef HAVE_GETRESUID
	#warning getresuid not supported. WindowsImpersonationContext wont work
#endif
#ifndef HAVE_SETRESUID
	#warning setresuid not supported. WindowsImpersonationContext wont work
#endif
#endif


gboolean 
ImpersonateLoggedOnUser (gpointer handle)
{
	uid_t token = (uid_t) GPOINTER_TO_INT (handle);
#ifdef HAVE_SETRESUID
	if (setresuid (-1, token, getuid ()) < 0)
		return FALSE;
#endif
	return (geteuid () == token);
}


gboolean RevertToSelf (void)
{
#ifdef HAVE_GETRESUID
	uid_t ruid, euid;
#endif
	uid_t suid = -1;

#ifdef HAVE_GETRESUID
	if (getresuid (&ruid, &euid, &suid) < 0)
		return FALSE;
#endif
#ifdef HAVE_SETRESUID
	if (setresuid (-1, suid, -1) < 0)
		return FALSE;
#else
	return TRUE;
#endif
	return (geteuid () == suid);
}

