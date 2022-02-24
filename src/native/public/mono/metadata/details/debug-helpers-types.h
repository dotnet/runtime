// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
#ifndef _MONO_DEBUG_HELPERS_TYPES_H
#define _MONO_DEBUG_HELPERS_TYPES_H

#include <mono/metadata/details/class-types.h>

MONO_BEGIN_DECLS

typedef struct MonoDisHelper MonoDisHelper;

typedef char* (*MonoDisIndenter) (MonoDisHelper *dh, MonoMethod *method, uint32_t ip_offset);
typedef char* (*MonoDisTokener)  (MonoDisHelper *dh, MonoMethod *method, uint32_t token);

struct MonoDisHelper {
	const char *newline;
	const char *label_format;
	const char *label_target;
	MonoDisIndenter indenter;
	MonoDisTokener  tokener;
	void* user_data;
};

typedef struct MonoMethodDesc MonoMethodDesc;

MONO_END_DECLS

#endif /* _MONO_DEBUG_HELPERS_TYPES_H */
