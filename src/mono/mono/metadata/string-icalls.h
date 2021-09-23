/**
 * \file
 */

#ifndef _MONO_CLI_STRING_ICALLS_H_
#define _MONO_CLI_STRING_ICALLS_H_

/*
 * string-icalls.h: String internal calls for the corlib
 *
 * Author:
 *   Patrik Torstensson (patrik.torstensson@labs2.com)
 *
 * (C) 2001 Ximian, Inc.
 */

#include <glib.h>
#include <mono/metadata/class.h>
#include <mono/metadata/object.h>
#include "mono/utils/mono-compiler.h"
#include <mono/metadata/icalls.h>

ICALL_EXPORT
void
ves_icall_System_String_ctor_RedirectToCreateString (void);

#endif /* _MONO_CLI_STRING_ICALLS_H_ */
