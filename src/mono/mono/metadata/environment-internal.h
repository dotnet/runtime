/**
 * \file
 * System.Environment support internal calls
 *
 * Authors:
 *	Dick Porter (dick@ximian.com)
 *	Sebastien Pouliot (sebastien@ximian.com)
 *	Jay Krell (jaykrell@microsoft.com)
 *
 * Copyright 2002-2003 Ximian, Inc (http://www.ximian.com)
 * Copyright 2004-2009 Novell, Inc (http://www.novell.com)
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#ifndef _MONO_METADATA_ENVIRONMENT_INTERNAL_H_
#define _MONO_METADATA_ENVIRONMENT_INTERNAL_H_

#include <mono/metadata/icalls.h>

ICALL_EXPORT
MonoStringHandle
ves_icall_System_Environment_GetOSVersionString (MonoError *error);

#endif /* _MONO_METADATA_ENVIRONMENT_INTERNAL_H_ */
