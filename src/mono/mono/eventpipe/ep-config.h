#ifndef __EVENTPIPE_CONFIGURATION_H__
#define __EVENTPIPE_CONFIGURATION_H__

#include <config.h>

#ifdef ENABLE_PERFTRACING
#include "ep-rt-config.h"
#include "ep-types.h"
#include "ep-event-instance.h"

#undef EP_IMPL_GETTER_SETTER
#ifdef EP_IMPL_CONFIG_GETTER_SETTER
#define EP_IMPL_GETTER_SETTER
#endif
#include "ep-getter-setter.h"

/*
 * EventPipeConfiguration.
 */

#if defined(EP_INLINE_GETTER_SETTER) || defined(EP_IMPL_CONFIG_GETTER_SETTER)
struct _EventPipeConfiguration {
#else
struct _EventPipeConfiguration_Internal {
#endif
	ep_rt_provider_list_t provider_list;
	EventPipeProvider *config_provider;
	EventPipeEvent *metadata_event;
	ep_char8_t *config_provider_name;
};

#if !defined(EP_INLINE_GETTER_SETTER) && !defined(EP_IMPL_CONFIG_GETTER_SETTER)
struct _EventPipeConfiguration {
	uint8_t _internal [sizeof (struct _EventPipeConfiguration_Internal)];
};
#endif

static
inline
const ep_char8_t *
ep_config_get_default_provider_name_utf8 (void)
{
	return "Microsoft-DotNETCore-EventPipeConfiguration";
}

static
inline
const ep_char8_t *
ep_config_get_public_provider_name_utf8 (void)
{
	return "Microsoft-Windows-DotNETRuntime";
}

static
inline
const ep_char8_t *
ep_config_get_rundown_provider_name_utf8 (void)
{
	return "Microsoft-Windows-DotNETRuntimeRundown";
}

static
inline
EventPipeConfiguration *
ep_config_get (void)
{
	// Singelton.
	extern EventPipeConfiguration _ep_config_instance;
	return &_ep_config_instance;
}

EventPipeConfiguration *
ep_config_init (EventPipeConfiguration *config);

void
ep_config_shutdown (EventPipeConfiguration *config);

EventPipeProvider *
ep_config_create_provider (
	EventPipeConfiguration *config,
	const ep_char8_t *provider_name,
	EventPipeCallback callback_func,
	EventPipeCallbackDataFree callback_data_free_func,
	void *callback_data,
	EventPipeProviderCallbackDataQueue *provider_callback_data_queue);

void
ep_config_delete_provider (
	EventPipeConfiguration *config,
	EventPipeProvider *provider);

void
ep_config_enable (
	EventPipeConfiguration *config,
	const EventPipeSession *session,
	EventPipeProviderCallbackDataQueue *provider_callback_data_queue);

void
ep_config_disable (
	EventPipeConfiguration *config,
	const EventPipeSession *session,
	EventPipeProviderCallbackDataQueue *provider_callback_data_queue);

EventPipeEventMetadataEvent *
ep_config_build_event_metadata_event (
	EventPipeConfiguration *config,
	const EventPipeEventInstance *source_instance,
	uint32_t metadata_id);

void
ep_config_delete_deferred_providers (EventPipeConfiguration *config);

/*
 * EventPipeEventMetadataEvent.
 */

#if defined(EP_INLINE_GETTER_SETTER) || defined(EP_IMPL_CONFIG_GETTER_SETTER)
struct _EventPipeEventMetadataEvent {
#else
struct _EventPipeEventMetadataEvent_Internal {
#endif
	EventPipeEventInstance event_instance;
	uint8_t *payload_buffer;
	uint32_t  payload_buffer_len;
};

#if !defined(EP_INLINE_GETTER_SETTER) && !defined(EP_IMPL_CONFIG_GETTER_SETTER)
struct _EventPipeEventMetadataEvent {
	uint8_t _internal [sizeof (struct _EventPipeEventMetadataEvent_Internal)];
};
#endif

EventPipeEventMetadataEvent *
ep_event_metdata_event_alloc (
	EventPipeEvent *ep_event,
	uint32_t proc_num,
	uint64_t thread_id,
	uint8_t *data,
	uint32_t data_len,
	const uint8_t *activity_id,
	const uint8_t *related_activity_id);

void
ep_event_metdata_event_free (EventPipeEventMetadataEvent *metadata_event);

#endif /* ENABLE_PERFTRACING */
#endif /** __EVENTPIPE_CONFIGURATION_H__ **/
