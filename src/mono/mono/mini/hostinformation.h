// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __MONO_MINI_HOSTINFORMATION_H__
#define __MONO_MINI_HOSTINFORMATION_H__

#include <stdbool.h>

#include <glib.h>
#include <mono/utils/mono-compiler.h>

#include <corehost/host_runtime_contract.h>

void
mono_host_information_set_contract (const struct host_runtime_contract *contract);

gboolean
mono_host_information_get_assembly_names (const char * const **names, size_t *count);

gboolean
mono_host_information_resolve_assembly_to_path (
	const char *simple_name,
	const char **directory,
	const char **file_name);

#endif /* __MONO_MINI_HOSTINFORMATION_H__ */
