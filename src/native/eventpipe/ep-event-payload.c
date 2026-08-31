#include "ep-rt-config.h"

#ifdef ENABLE_PERFTRACING
#if !defined(EP_INCLUDE_SOURCE_FILES) || defined(EP_FORCE_INCLUDE_SOURCE_FILES)

#define EP_IMPL_EVENT_PAYLOAD_GETTER_SETTER
#include "ep-event-payload.h"
#include "ep-rt.h"

/*
 * EventData.
 */

EventData *
ep_event_data_alloc (
	uint64_t ptr,
	uint32_t size,
	uint32_t reserved)
{
	EventData *instance = ep_rt_object_alloc (EventData);
	ep_raise_error_if_nok (instance != NULL);
	ep_raise_error_if_nok (ep_event_data_init (instance, ptr, size,reserved) != NULL);

ep_on_exit:
	return instance;

ep_on_error:
	ep_event_data_free (instance);
	instance = NULL;
	ep_exit_error_handler ();
}

EventData *
ep_event_data_init (
	EventData *event_data,
	uint64_t ptr,
	uint32_t size,
	uint32_t reserved)
{
	EP_ASSERT (event_data != NULL);

	event_data->ptr = ptr;
	event_data->size = size;
	event_data->reserved = reserved;

	return event_data;
}

void
ep_event_data_fini (EventData *event_data)
{
	;
}

void
ep_event_data_free (EventData *event_data)
{
	ep_return_void_if_nok (event_data != NULL);

	ep_event_data_fini (event_data);
	ep_rt_object_free (event_data);
}

/*
 * EventPipeEventPayload.
 */

EventPipeEventPayload *
ep_event_payload_init (
	EventPipeEventPayload *event_payload,
	uint8_t *data,
	uint32_t len)
{
	EP_ASSERT (event_payload != NULL);

	event_payload->data = data;
	event_payload->event_data = NULL;
	event_payload->event_data_len = 0;
	event_payload->size = len;
	event_payload->allocated_data = false;

	return event_payload;
}

EventPipeEventPayload *
ep_event_payload_init_2 (
	EventPipeEventPayload *event_payload,
	EventData *event_data,
	uint32_t event_data_len)
{
	EP_ASSERT (event_payload != NULL);

	event_payload->data = NULL;
	event_payload->event_data = event_data;
	event_payload->event_data_len = event_data_len;
	event_payload->allocated_data = false;

	size_t tmp_size = 0;
	for (uint32_t i = 0; i < event_data_len; ++i) {
		tmp_size += ep_event_data_get_size (&event_data [i]);
		if (tmp_size < ep_event_data_get_size (&event_data [i])) {
			tmp_size = (size_t)UINT32_MAX + 1;
			break;
		}
	}

	if (tmp_size > UINT32_MAX) {
		// If there is an overflow, drop the data and create an empty payload
		event_payload->event_data = NULL;
		event_payload->event_data_len = 0;
		event_payload->size = 0;
	} else {
		event_payload->size = (uint32_t)tmp_size;
	}

	return event_payload;
}

void
ep_event_payload_fini (EventPipeEventPayload *event_payload)
{
	ep_return_void_if_nok (event_payload != NULL);

	if (event_payload->allocated_data && event_payload->data) {
		ep_rt_byte_array_free (event_payload->data);
		event_payload->data = NULL;
	}
}

void
ep_event_payload_copy_data (
	EventPipeEventPayload *event_payload,
	uint8_t *dst)
{
	EP_ASSERT (event_payload != NULL);
	EP_ASSERT (dst != NULL);

	if (event_payload->size > 0) {
		if (ep_event_payload_is_flattened (event_payload)) {
			memcpy (dst, event_payload->data, event_payload->size);
		} else if (event_payload->event_data != NULL) {
			uint32_t offset = 0;
			EventData *event_data = event_payload->event_data;
			for (uint32_t i = 0; i < event_payload->event_data_len; ++i) {
				EP_ASSERT ((offset + ep_event_data_get_size (&event_data[i])) <= event_payload->size);
				memcpy (dst + offset, (uint8_t *)(uintptr_t)ep_event_data_get_ptr (&event_data[i]), ep_event_data_get_size (&event_data[i]));
				offset += ep_event_data_get_size (&event_data[i]);
			}
		}
	}
}

void
ep_event_payload_flatten (EventPipeEventPayload *event_payload)
{
	EP_ASSERT (event_payload != NULL);

	if (event_payload->size > 0) {
		if (!ep_event_payload_is_flattened (event_payload)) {
			uint8_t * tmp_data = ep_rt_byte_array_alloc (event_payload->size);
			if (tmp_data) {
				event_payload->allocated_data = true;
				ep_event_payload_copy_data (event_payload, tmp_data);
				event_payload->data = tmp_data;
			}
		}
	}
}

uint8_t *
ep_event_payload_get_flat_data (EventPipeEventPayload *event_payload)
{
	EP_ASSERT (event_payload != NULL);

	if (!ep_event_payload_is_flattened (event_payload))
		ep_event_payload_flatten (event_payload);

	return event_payload->data;
}


#endif /* !defined(EP_INCLUDE_SOURCE_FILES) || defined(EP_FORCE_INCLUDE_SOURCE_FILES) */
#endif /* ENABLE_PERFTRACING */

#if !defined(ENABLE_PERFTRACING) || (defined(EP_INCLUDE_SOURCE_FILES) && !defined(EP_FORCE_INCLUDE_SOURCE_FILES))
extern const char quiet_linker_empty_file_warning_eventpipe_event_payload;
const char quiet_linker_empty_file_warning_eventpipe_event_payload = 0;
#endif
