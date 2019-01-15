/**
 * \file
 * Copyright 2016 Microsoft
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */
#ifndef __MONO_METADATA_MONO_SECURITY_WINDOWS_INTERNALS_H__
#define __MONO_METADATA_MONO_SECURITY_WINDOWS_INTERNALS_H__

#include <config.h>
#include <glib.h>

#ifdef HOST_WIN32
#include "mono/metadata/security.h"
#include "mono/metadata/object.h"
#include "mono/metadata/object-internals.h"
#include "mono/metadata/metadata.h"
#include "mono/metadata/metadata-internals.h"

gint32
mono_security_win_get_token_name (gpointer token, gunichar2 ** uniname, MonoError *error);

gboolean
mono_security_win_is_machine_protected (const gunichar2 *path, MonoError *error);

gboolean
mono_security_win_is_user_protected (const gunichar2 *path, MonoError *error);

gboolean
mono_security_win_protect_machine (const gunichar2 *path, MonoError *error);

gboolean
mono_security_win_protect_user (const gunichar2 *path, MonoError *error);

#endif /* HOST_WIN32 */

#endif /* __MONO_METADATA_MONO_SECURITY_WINDOWS_INTERNALS_H__ */
