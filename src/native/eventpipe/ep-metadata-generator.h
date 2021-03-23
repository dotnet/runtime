#ifndef __EVENTPIPE_METADATA_GENERATOR_H__
#define __EVENTPIPE_METADATA_GENERATOR_H__

#include "ep-rt-config.h"

#ifdef ENABLE_PERFTRACING
#include "ep-types.h"

#undef EP_IMPL_GETTER_SETTER
#ifdef EP_IMPL_METADATA_GENERATOR_GETTER_SETTER
#define EP_IMPL_GETTER_SETTER
#endif
#include "ep-getter-setter.h"

/*
 * EventPipeMetadataGenerator.
 */

// Generates metadata for an event emitted by the EventPipe.
uint8_t *
ep_metadata_generator_generate_event_metadata (
	uint32_t event_id,
	const ep_char16_t *event_name,
	uint64_t keywords,
	uint32_t version,
	EventPipeEventLevel level,
	uint8_t opcode,
	EventPipeParameterDesc *params,
	uint32_t params_len,
	size_t *metadata_len);

/*
 * EventPipeParameterDesc.
 */

// Contains the metadata associated with an EventPipe event parameter.
// NOTE, needs to match layout of COR_PRF_EVENTPIPE_PARAM_DESC.
#if defined(EP_INLINE_GETTER_SETTER) || defined(EP_IMPL_METADATA_GENERATOR_GETTER_SETTER)
struct _EventPipeParameterDesc {
#else
struct _EventPipeParameterDesc_Internal {
#endif
	EventPipeParameterType type;
	// Only used for array types to indicate what type the array elements are
	EventPipeParameterType element_type;
	const ep_char16_t *name;
};

#if !defined(EP_INLINE_GETTER_SETTER) && !defined(EP_IMPL_METADATA_GENERATOR_GETTER_SETTER)
struct _EventPipeParameterDesc {
	uint8_t _internal [sizeof (struct _EventPipeParameterDesc_Internal)];
};
#endif

EventPipeParameterDesc *
ep_parameter_desc_init (
	EventPipeParameterDesc *parameter_desc,
	EventPipeParameterType type,
	const ep_char16_t *name);

void
ep_parameter_desc_fini (EventPipeParameterDesc *parameter_desc);

#endif /* ENABLE_PERFTRACING */
#endif /* __EVENTPIPE_METADATA_GENERATOR_H__ */
