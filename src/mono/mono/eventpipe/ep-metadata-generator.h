#ifndef __EVENTPIPE_METADATA_GENERATOR_H__
#define __EVENTPIPE_METADATA_GENERATOR_H__

#include <config.h>

#ifdef ENABLE_PERFTRACING
#include "ep-rt-config.h"
#include "ep-types.h"

/*
 * EventPipeMetadataGenerator.
 */

uint8_t *
ep_metadata_generator_generate_event_metadata (
	uint32_t event_id,
	const ep_char16_t *event_name,
	uint64_t keywords,
	uint32_t version,
	EventPipeEventLevel level,
	EventPipeParameterDesc *params,
	uint32_t params_len,
	size_t *metadata_len);

/*
 * EventPipeParameterDesc.
 */

#if defined(EP_INLINE_GETTER_SETTER) || defined(EP_IMPL_GETTER_SETTER)
struct _EventPipeParameterDesc {
#else
struct _EventPipeParameterDesc_Internal {
#endif
	EventPipeParameterType type;
	const ep_char16_t *name;
};

#if !defined(EP_INLINE_GETTER_SETTER) && !defined(EP_IMPL_GETTER_SETTER)
struct _EventPipeParameterDesc {
	uint8_t _internal [sizeof (struct _EventPipeParameterDesc_Internal)];
};
#endif

EP_DEFINE_GETTER(EventPipeParameterDesc *, parameter_desc, EventPipeParameterType, type)
EP_DEFINE_GETTER(EventPipeParameterDesc *, parameter_desc, const ep_char16_t *, name)

EventPipeParameterDesc *
ep_parameter_desc_init (
	EventPipeParameterDesc *parameter_desc,
	EventPipeParameterType type,
	const ep_char16_t *name);

void
ep_parameter_desc_fini (EventPipeParameterDesc *parameter_desc);

#endif /* ENABLE_PERFTRACING */
#endif /** __EVENTPIPE_METADATA_GENERATOR_H__ **/
