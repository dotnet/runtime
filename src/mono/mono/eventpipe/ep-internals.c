#include <config.h>

#ifdef ENABLE_PERFTRACING
#include "ep-rt-config.h"

#ifndef EP_INLINE_GETTER_SETTER
#define EP_IMPL_GETTER_SETTER
#define EP_DEFINE_GETTER(instance_type, instance_type_name, return_type, instance_field_name) \
	return_type ep_ ## instance_type_name ## _get_ ## instance_field_name (const instance_type instance) { return instance-> instance_field_name; } \
	size_t ep_ ## instance_type_name ## _sizeof_ ## instance_field_name (const instance_type instance) { return sizeof (instance-> instance_field_name); }
#define EP_DEFINE_GETTER_REF(instance_type, instance_type_name, return_type, instance_field_name) \
	return_type ep_ ## instance_type_name ## _get_ ## instance_field_name ## _ref (instance_type instance) { return &(instance-> instance_field_name); } \
	const return_type ep_ ## instance_type_name ## _get_ ## instance_field_name ## _cref (const instance_type instance) { return &(instance-> instance_field_name); }
#define EP_DEFINE_GETTER_ARRAY_REF(instance_type, instance_type_name, return_type, const_return_type, instance_field_name, instance_field) \
	return_type ep_ ## instance_type_name ## _get_ ## instance_field_name ## _ref (instance_type instance) { return &(instance-> instance_field); } \
	const_return_type ep_ ## instance_type_name ## _get_ ## instance_field_name ## _cref (const instance_type instance) { return &(instance-> instance_field); }
#define EP_DEFINE_SETTER(instance_type, instance_type_name, instance_field_type, instance_field_name) \
	void ep_ ## instance_type_name ## _set_ ## instance_field_name (instance_type instance, instance_field_type instance_field_name) { instance-> instance_field_name = instance_field_name; }
#endif

#include "ep.h"

// Option to include all internal source files into ep-internals.c.
#ifdef EP_INCLUDE_SOURCE_FILES
#define EP_FORCE_INCLUDE_SOURCE_FILES
#include "ep-block-internals.c"
#include "ep-buffer-manager-internals.c"
#include "ep-config-internals.c"
#include "ep-event-internals.c"
#include "ep-event-instance-internals.c"
#include "ep-event-payload-internals.c"
#include "ep-event-source-internals.c"
#include "ep-file-internals.c"
#include "ep-metadata-generator-internals.c"
#include "ep-provider-internals.c"
#include "ep-session-internals.c"
#include "ep-session-provider-internals.c"
#include "ep-stream-internals.c"
#include "ep-thread-internals.c"
#endif

/*
 * EventFilterDescriptor.
 */

EventFilterDescriptor *
ep_event_filter_desc_alloc (
	uint64_t ptr,
	uint32_t size,
	uint32_t type)
{
	EventFilterDescriptor *instance = ep_rt_object_alloc (EventFilterDescriptor);
	ep_raise_error_if_nok (ep_event_filter_desc_init (instance, ptr, size, type) != NULL);

ep_on_exit:
	return instance;

ep_on_error:
	ep_event_filter_desc_free (instance);

	instance = NULL;
	ep_exit_error_handler ();
}

EventFilterDescriptor *
ep_event_filter_desc_init (
	EventFilterDescriptor *event_filter_desc,
	uint64_t ptr,
	uint32_t size,
	uint32_t type)
{
	EP_ASSERT (event_filter_desc != NULL);

	event_filter_desc->ptr = ptr;
	event_filter_desc->size = size;
	event_filter_desc->type = type;

	return event_filter_desc;
}

void
ep_event_filter_desc_fini (EventFilterDescriptor * filter_desc)
{
	;
}

void
ep_event_filter_desc_free (EventFilterDescriptor * filter_desc)
{
	ep_return_void_if_nok (filter_desc != NULL);

	ep_event_filter_desc_fini (filter_desc);
	ep_rt_object_free (filter_desc);
}

/*
 * EventPipeProviderCallbackDataQueue.
 */

EventPipeProviderCallbackDataQueue *
ep_provider_callback_data_queue_init (EventPipeProviderCallbackDataQueue *provider_callback_data_queue)
{
	ep_return_null_if_nok (provider_callback_data_queue != NULL);
	ep_rt_provider_callback_data_queue_alloc (&provider_callback_data_queue->queue);
	return provider_callback_data_queue;
}

void
ep_provider_callback_data_queue_fini (EventPipeProviderCallbackDataQueue *provider_callback_data_queue)
{
	ep_return_void_if_nok (provider_callback_data_queue != NULL);
	ep_rt_provider_callback_data_queue_free (&provider_callback_data_queue->queue);
}

/*
 * EventPipeProviderCallbackData.
 */

EventPipeProviderCallbackData *
ep_provider_callback_data_alloc (
	const ep_char8_t *filter_data,
	EventPipeCallback callback_function,
	void *callback_data,
	int64_t keywords,
	EventPipeEventLevel provider_level,
	bool enabled)
{
	EventPipeProviderCallbackData *instance = ep_rt_object_alloc (EventPipeProviderCallbackData);
	ep_raise_error_if_nok (instance != NULL);

	ep_raise_error_if_nok (ep_provider_callback_data_init (
		instance,
		filter_data,
		callback_function,
		callback_data,
		keywords,
		provider_level,
		enabled) != NULL);

ep_on_exit:
	return instance;

ep_on_error:
	instance = NULL;
	ep_exit_error_handler ();
}

EventPipeProviderCallbackData *
ep_provider_callback_data_alloc_copy (EventPipeProviderCallbackData *provider_callback_data_src)
{
	EventPipeProviderCallbackData *instance = ep_rt_object_alloc (EventPipeProviderCallbackData);
	ep_raise_error_if_nok (instance != NULL);

	if (provider_callback_data_src)
		*instance = *provider_callback_data_src;

ep_on_exit:
	return instance;

ep_on_error:
	instance = NULL;
	ep_exit_error_handler ();
}

EventPipeProviderCallbackData *
ep_provider_callback_data_init (
	EventPipeProviderCallbackData *provider_callback_data,
	const ep_char8_t *filter_data,
	EventPipeCallback callback_function,
	void *callback_data,
	int64_t keywords,
	EventPipeEventLevel provider_level,
	bool enabled)
{
	ep_return_null_if_nok (provider_callback_data != NULL);

	provider_callback_data->filter_data = filter_data;
	provider_callback_data->callback_function = callback_function;
	provider_callback_data->callback_data = callback_data;
	provider_callback_data->keywords = keywords;
	provider_callback_data->provider_level = provider_level;
	provider_callback_data->enabled = enabled;

	return provider_callback_data;
}

EventPipeProviderCallbackData *
ep_provider_callback_data_init_copy (
	EventPipeProviderCallbackData *provider_callback_data_dst,
	EventPipeProviderCallbackData *provider_callback_data_src)
{
	ep_return_null_if_nok (provider_callback_data_dst != NULL && provider_callback_data_src != NULL);
	*provider_callback_data_dst = *provider_callback_data_src;
	return provider_callback_data_dst;
}

void
ep_provider_callback_data_fini (EventPipeProviderCallbackData *provider_callback_data)
{
	;
}

void
ep_provider_callback_data_free (EventPipeProviderCallbackData *provider_callback_data)
{
	ep_return_void_if_nok (provider_callback_data != NULL);
	ep_rt_object_free (provider_callback_data);
}

/*
 * EventPipeProviderConfiguration.
 */

EventPipeProviderConfiguration *
ep_provider_config_init (
	EventPipeProviderConfiguration *provider_config,
	const ep_char8_t *provider_name,
	uint64_t keywords,
	EventPipeEventLevel logging_level,
	const ep_char8_t *filter_data)
{
	EP_ASSERT (provider_config != NULL);
	ep_return_null_if_nok (provider_name != NULL);

	provider_config->provider_name = provider_name;
	provider_config->keywords = keywords;
	provider_config->logging_level = logging_level;
	provider_config->filter_data = filter_data;

	return provider_config;
}

void
ep_provider_config_fini (EventPipeProviderConfiguration *provider_config)
{
	;
}

#endif /* ENABLE_PERFTRACING */

extern const char quiet_linker_empty_file_warning_eventpipe_internals;
const char quiet_linker_empty_file_warning_eventpipe_internals = 0;
