/**
 * \file
 * Console IO internal calls
 *
 * Author:
 *	Gonzalo Paniagua Javier (gonzalo@ximian.com)
 *
 * Copyright (c) 2005 Novell, Inc. (http://www.novell.com)
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#ifndef _MONO_METADATA_CONSOLEIO_H
#define _MONO_METADATA_CONSOLEIO_H

#include <config.h>
#include <glib.h>

#include <mono/metadata/object.h>
#include <mono/utils/mono-compiler.h>
#include <mono/metadata/icalls.h>

void mono_console_init (void);
void mono_console_handle_async_ops (void);

#endif /* _MONO_METADATA_CONSOLEIO_H */
