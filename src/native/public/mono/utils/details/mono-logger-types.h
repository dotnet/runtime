// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
#ifndef _MONO_LOGGER_TYPES_H
#define _MONO_LOGGER_TYPES_H

#include <mono/utils/details/mono-publib-types.h>

MONO_BEGIN_DECLS

typedef void (*MonoPrintCallback) (const char *string, mono_bool is_stdout);
typedef void (*MonoLogCallback) (const char *log_domain, const char *log_level, const char *message, mono_bool fatal, void *user_data);

MONO_END_DECLS

#endif /* _MONO_LOGGER_TYPES_H */
