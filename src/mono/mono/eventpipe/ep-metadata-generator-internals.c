#include <config.h>

#ifdef ENABLE_PERFTRACING
#include "ep-rt-config.h"
#if !defined(EP_INCLUDE_SOURCE_FILES) || defined(EP_FORCE_INCLUDE_SOURCE_FILES)

#define EP_IMPL_GETTER_SETTER
#include "ep.h"

/*
 * EventPipeParameterDesc.
 */

EventPipeParameterDesc *
ep_parameter_desc_init (
	EventPipeParameterDesc *parameter_desc,
	EventPipeParameterType type,
	const ep_char16_t *name)
{
	EP_ASSERT (parameter_desc != NULL);

	parameter_desc->type = type;
	parameter_desc->name = name;

	return parameter_desc;
}

void
ep_parameter_desc_fini (EventPipeParameterDesc *parameter_desc)
{
	;
}

#endif /* !defined(EP_INCLUDE_SOURCE_FILES) || defined(EP_FORCE_INCLUDE_SOURCE_FILES) */
#endif /* ENABLE_PERFTRACING */

#ifndef EP_INCLUDE_SOURCE_FILES
extern const char quiet_linker_empty_file_warning_eventpipe_metadata_generator_internals;
const char quiet_linker_empty_file_warning_eventpipe_metadata_generator_internals = 0;
#endif
