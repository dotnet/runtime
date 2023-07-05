#include "ep-rt-config.h"

#ifdef ENABLE_PERFTRACING
#if !defined(EP_INCLUDE_SOURCE_FILES) || defined(EP_FORCE_INCLUDE_SOURCE_FILES)

#define EP_IMPL_EVENT_GETTER_SETTER
#include "ep-event.h"
#include "ep-metadata-generator.h"

/*
 * Forward declares of all static functions.
 */

static
void
event_build_minimum_metadata (
	EventPipeEvent *ep_event,
	uint8_t **metadata,
	uint32_t *metadata_len);

/*
 * EventPipeEvent.
 */

static
void
event_build_minimum_metadata (
	EventPipeEvent *ep_event,
	uint8_t **metadata,
	uint32_t *metadata_len)
{
	EP_ASSERT (ep_event != NULL);
	EP_ASSERT (metadata != NULL);
	EP_ASSERT (metadata_len != NULL);

	size_t output_len = 0;
	ep_char16_t empty_string [1] = { 0 };
	*metadata = ep_metadata_generator_generate_event_metadata (
		ep_event->event_id,
		empty_string,
		ep_event->keywords,
		ep_event->event_version,
		ep_event->level,
		0,
		NULL,
		0,
		&output_len);

	*metadata_len = (uint32_t)output_len;
}

EventPipeEvent *
ep_event_alloc (
	EventPipeProvider *provider,
	uint64_t keywords,
	uint32_t event_id,
	uint32_t event_version,
	EventPipeEventLevel level,
	bool need_stack,
	const uint8_t *metadata,
	uint32_t metadata_len)
{
	EP_ASSERT (provider != NULL);

	EventPipeEvent *instance = ep_rt_object_alloc (EventPipeEvent);
	ep_raise_error_if_nok (instance != NULL);

	instance->provider = provider;
	instance->keywords = keywords;
	instance->event_id = event_id;
	instance->event_version = event_version;
	instance->level = level;
	instance->need_stack = need_stack;
	instance->enabled_mask = 0;

	if (metadata != NULL) {
		instance->metadata = ep_rt_byte_array_alloc (metadata_len);
		ep_raise_error_if_nok (instance->metadata != NULL);

		memcpy (instance->metadata, metadata, metadata_len);
		instance->metadata_len = metadata_len;
	} else {
		// if metadata is not provided, we have to build the minimum version. It's required by the serialization contract.
		event_build_minimum_metadata (instance, &(instance->metadata), &(instance->metadata_len));
	}

ep_on_exit:
	return instance;

ep_on_error:
	ep_event_free (instance);
	instance = NULL;
	ep_exit_error_handler ();
}

void
ep_event_free (EventPipeEvent *ep_event)
{
	ep_return_void_if_nok (ep_event != NULL);

	ep_rt_byte_array_free (ep_event->metadata);
	ep_rt_object_free (ep_event);
}

#endif /* !defined(EP_INCLUDE_SOURCE_FILES) || defined(EP_FORCE_INCLUDE_SOURCE_FILES) */
#endif /* ENABLE_PERFTRACING */

#if !defined(ENABLE_PERFTRACING) || (defined(EP_INCLUDE_SOURCE_FILES) && !defined(EP_FORCE_INCLUDE_SOURCE_FILES))
extern const char quiet_linker_empty_file_warning_eventpipe_event_internals;
const char quiet_linker_empty_file_warning_eventpipe_event_internals = 0;
#endif
