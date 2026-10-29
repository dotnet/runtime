// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <config.h>
#include <string.h>

#include "hostinformation.h"

static struct host_runtime_contract host_contract;

#define HOST_CONTRACT_HAS_FIELD(field) \
	(host_contract.size >= offsetof (struct host_runtime_contract, field) + sizeof (host_contract.field))

void
mono_host_information_set_contract (const struct host_runtime_contract *contract)
{
	g_assert (contract != NULL);
	g_assert (host_contract.size == 0);

	memcpy (&host_contract, contract, MIN (contract->size, sizeof (host_contract)));
}

gboolean
mono_host_information_get_assembly_names (const char * const **names, size_t *count)
{
	if (!HOST_CONTRACT_HAS_FIELD (resolve_assembly_to_path) ||
		host_contract.get_assembly_names == NULL ||
		host_contract.resolve_assembly_to_path == NULL)
		return FALSE;

	return host_contract.get_assembly_names (names, count, host_contract.context);
}

gboolean
mono_host_information_resolve_assembly_to_path (
	const char *simple_name,
	const char **directory,
	const char **file_name)
{
	if (directory == NULL || file_name == NULL)
		return FALSE;

	*directory = NULL;
	*file_name = NULL;
	if (!HOST_CONTRACT_HAS_FIELD (resolve_assembly_to_path) || host_contract.resolve_assembly_to_path == NULL)
		return FALSE;

	return host_contract.resolve_assembly_to_path (simple_name, directory, file_name, host_contract.context)
		&& *directory != NULL
		&& (*directory) [0] != '\0'
		&& *file_name != NULL
		&& (*file_name) [0] != '\0';
}
