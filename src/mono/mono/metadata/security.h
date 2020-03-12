/**
 * \file
 * Security internal calls
 *
 * Author:
 *	Sebastien Pouliot  <sebastien@ximian.com>
 *
 * (C) 2004 Novell (http://www.novell.com)
 */


#ifndef _MONO_METADATA_SECURITY_H_
#define _MONO_METADATA_SECURITY_H_

#include <glib.h>
#include <mono/metadata/object.h>
#include <mono/metadata/object-internals.h>
#include <mono/utils/mono-compiler.h>
#include <mono/utils/mono-error.h>
#include <mono/utils/mono-publib.h>
#include <mono/metadata/icalls.h>
#include "reflection-internals.h"

/* System.Security.Principal.WindowsIdentity */
gpointer
mono_security_principal_windows_identity_get_current_token (MonoError *error);

#endif /* _MONO_METADATA_SECURITY_H_ */
