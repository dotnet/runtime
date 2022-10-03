#include "ep-rt-config.h"

#ifdef ENABLE_PERFTRACING
#if !defined(EP_INCLUDE_SOURCE_FILES) || defined(EP_FORCE_INCLUDE_SOURCE_FILES)

#define EP_IMPL_CONFIG_GETTER_SETTER
#include "ep.h"
#include "ep-config.h"
#include "ep-config-internals.h"
#include "ep-event.h"
#include "ep-provider.h"
#include "ep-provider-internals.h"
#include "ep-session.h"
#include "ep-rt.h"

EventPipeConfiguration _ep_config_instance = { { 0 }, 0 };

/*
 * Forward declares of all static functions.
 */

// _Requires_lock_held (config)
static
void
config_compute_keyword_and_level (
	const EventPipeConfiguration *config,
	const EventPipeProvider *provider,
	int64_t *keyword_for_all_sessions,
	EventPipeEventLevel *level_for_all_sessions);

// _Requires_lock_held (config)
static
bool
config_register_provider (
	EventPipeConfiguration *config,
	EventPipeProvider *provider,
	EventPipeProviderCallbackDataQueue *provider_callback_data_queue);

// _Requires_lock_held (config)
static
bool
config_unregister_provider (
	EventPipeConfiguration *config,
	EventPipeProvider *provider);

/*
 * EventPipeConfiguration.
 */

static
void
config_compute_keyword_and_level (
	const EventPipeConfiguration *config,
	const EventPipeProvider *provider,
	int64_t *keyword_for_all_sessions,
	EventPipeEventLevel *level_for_all_sessions)
{
	EP_ASSERT (provider != NULL);
	EP_ASSERT (keyword_for_all_sessions != NULL);
	EP_ASSERT (level_for_all_sessions != NULL);

	ep_requires_lock_held ();

	*keyword_for_all_sessions = 0;
	*level_for_all_sessions = EP_EVENT_LEVEL_LOGALWAYS;

	for (int i = 0; i < EP_MAX_NUMBER_OF_SESSIONS; i++) {
		// Entering EventPipe lock gave us a barrier, we don't need more of them.
		EventPipeSession *session = ep_volatile_load_session_without_barrier (i);
		if (session) {
			EventPipeSessionProviderList *providers = ep_session_get_providers (session);
			EP_ASSERT (providers != NULL);

			EventPipeSessionProvider *session_provider = ep_rt_session_provider_list_find_by_name (ep_session_provider_list_get_providers_cref (providers), ep_provider_get_provider_name (provider));
			if (session_provider) {
				*keyword_for_all_sessions = *keyword_for_all_sessions | ep_session_provider_get_keywords (session_provider);
				*level_for_all_sessions = (ep_session_provider_get_logging_level (session_provider) > *level_for_all_sessions) ? ep_session_provider_get_logging_level (session_provider) : *level_for_all_sessions;
			}
		}
	}

	ep_requires_lock_held ();
	return;
}

static
bool
config_register_provider (
	EventPipeConfiguration *config,
	EventPipeProvider *provider,
	EventPipeProviderCallbackDataQueue *provider_callback_data_queue)
{
	EP_ASSERT (config != NULL);
	EP_ASSERT (provider != NULL);

	ep_requires_lock_held ();

	// The provider has not been registered, so register it.
	if (!ep_rt_provider_list_append (&config->provider_list, provider))
		return false;

	int64_t keyword_for_all_sessions;
	EventPipeEventLevel level_for_all_sessions;
	config_compute_keyword_and_level (config, provider, &keyword_for_all_sessions, &level_for_all_sessions);

	for (int i = 0; i < EP_MAX_NUMBER_OF_SESSIONS; i++) {
		// Entering EventPipe lock gave us a barrier, we don't need more of them.
		EventPipeSession *session = ep_volatile_load_session_without_barrier (i);
		if (session) {
			EventPipeSessionProviderList *providers = ep_session_get_providers (session);
			EP_ASSERT (providers != NULL);

			EventPipeSessionProvider *session_provider = ep_rt_session_provider_list_find_by_name (ep_session_provider_list_get_providers_cref (providers), ep_provider_get_provider_name (provider));
			if (session_provider) {
				EventPipeProviderCallbackData provider_callback_data;
				memset (&provider_callback_data, 0, sizeof (provider_callback_data));
				provider_set_config (
					provider,
					keyword_for_all_sessions,
					level_for_all_sessions,
					((uint64_t)1 << ep_session_get_index (session)),
					ep_session_provider_get_keywords (session_provider),
					ep_session_provider_get_logging_level (session_provider),
					ep_session_provider_get_filter_data (session_provider),
					&provider_callback_data);
				if (provider_callback_data_queue)
					ep_provider_callback_data_queue_enqueue (provider_callback_data_queue, &provider_callback_data);
				ep_provider_callback_data_fini (&provider_callback_data);
			}
		}
	}

	ep_requires_lock_held ();
	return true;
}

static
bool
config_unregister_provider (
	EventPipeConfiguration *config,
	EventPipeProvider *provider)
{
	EP_ASSERT (config != NULL);

	ep_requires_lock_held ();

	EventPipeProvider *existing_provider = NULL;
	ep_rt_provider_list_t *provider_list = &config->provider_list;

	// The provider list should be non-NULL, but can be NULL on shutdown.
	if (!ep_rt_provider_list_is_empty (provider_list)) {
		// If we found the provider, remove it.
		if (ep_rt_provider_list_find (provider_list, provider, &existing_provider))
			ep_rt_provider_list_remove (provider_list, existing_provider);
	}

	ep_requires_lock_held ();
	return (existing_provider != NULL);
}

EventPipeConfiguration *
ep_config_init (EventPipeConfiguration *config)
{
	EP_ASSERT (config != NULL);

	ep_requires_lock_not_held ();

	EventPipeProviderCallbackDataQueue callback_data_queue;
	EventPipeProviderCallbackData provider_callback_data;
	EventPipeProviderCallbackDataQueue *provider_callback_data_queue = ep_provider_callback_data_queue_init (&callback_data_queue);

	ep_rt_provider_list_alloc (&config->provider_list);
	ep_raise_error_if_nok (ep_rt_provider_list_is_valid (&config->provider_list));

	EP_LOCK_ENTER (section1)
		config->config_provider = provider_create_register (ep_config_get_default_provider_name_utf8 (), NULL, NULL, NULL, provider_callback_data_queue);
	EP_LOCK_EXIT (section1)

	ep_raise_error_if_nok (config->config_provider != NULL);

	while (ep_provider_callback_data_queue_try_dequeue (provider_callback_data_queue, &provider_callback_data)) {
		ep_rt_prepare_provider_invoke_callback (&provider_callback_data);
		provider_invoke_callback (&provider_callback_data);
		ep_provider_callback_data_fini (&provider_callback_data);
	}

	// Create the metadata event.
	config->metadata_event = ep_provider_add_event (
		config->config_provider,
		0, /* event_id */
		0, /* keywords */
		0, /* event_version */
		EP_EVENT_LEVEL_LOGALWAYS,
		false, /* need_stack */
		NULL, /* metadata */
		0); /* metadata_len */
	ep_raise_error_if_nok (config->metadata_event != NULL);

ep_on_exit:
	ep_provider_callback_data_queue_fini (provider_callback_data_queue);
	ep_requires_lock_not_held ();
	return config;

ep_on_error:
	ep_config_shutdown (config);
	config = NULL;
	ep_exit_error_handler ();
}

void
ep_config_shutdown (EventPipeConfiguration *config)
{
	EP_ASSERT (config != NULL);

	ep_requires_lock_not_held ();

	ep_event_free (config->metadata_event);
	config->metadata_event = NULL;

	ep_delete_provider (config->config_provider);
	config->config_provider = NULL;

	// Take the lock before manipulating the list.
	EP_LOCK_ENTER (section1)
		// We don't delete provider itself because it can be in-use
		ep_rt_provider_list_free (&config->provider_list, NULL);
	EP_LOCK_EXIT (section1)

ep_on_exit:
	ep_requires_lock_not_held ();
	return;

ep_on_error:
	ep_exit_error_handler ();
}

EventPipeProvider *
ep_config_create_provider (
	EventPipeConfiguration *config,
	const ep_char8_t *provider_name,
	EventPipeCallback callback_func,
	EventPipeCallbackDataFree callback_data_free_func,
	void *callback_data,
	EventPipeProviderCallbackDataQueue *provider_callback_data_queue)
{
	EP_ASSERT (config != NULL);
	EP_ASSERT (provider_name != NULL);

	ep_requires_lock_not_held ();

	EventPipeProvider *provider = NULL;
	EP_LOCK_ENTER (section1)
		provider = config_create_provider (config, provider_name, callback_func, callback_data_free_func, callback_data, provider_callback_data_queue);
		ep_raise_error_if_nok_holding_lock (provider != NULL, section1);
	EP_LOCK_EXIT (section1)

ep_on_exit:
	ep_requires_lock_not_held ();
	return provider;

ep_on_error:
	ep_config_delete_provider (config, provider);
	provider = NULL;
	ep_exit_error_handler ();
}

void
ep_config_delete_provider (
	EventPipeConfiguration *config,
	EventPipeProvider *provider)
{
	EP_ASSERT (config != NULL);

	ep_requires_lock_not_held ();

	ep_return_void_if_nok (provider != NULL);

	EP_LOCK_ENTER (section1)
		config_delete_provider (config, provider);
	EP_LOCK_EXIT (section1)

ep_on_exit:
	ep_requires_lock_not_held ();
	return;

ep_on_error:
	ep_exit_error_handler ();
}

void
ep_config_enable (
	EventPipeConfiguration *config,
	const EventPipeSession *session,
	EventPipeProviderCallbackDataQueue *provider_callback_data_queue)
{
	EP_ASSERT (config != NULL);
	EP_ASSERT (session != NULL);

	ep_requires_lock_not_held ();

	EP_LOCK_ENTER (section1)
		config_enable_disable (config, session, provider_callback_data_queue, true);
	EP_LOCK_EXIT (section1)

ep_on_exit:
	ep_requires_lock_not_held ();
	return;

ep_on_error:
	ep_exit_error_handler ();
}

void
ep_config_disable (
	EventPipeConfiguration *config,
	const EventPipeSession *session,
	EventPipeProviderCallbackDataQueue *provider_callback_data_queue)
{
	EP_ASSERT (config != NULL);
	EP_ASSERT (session != NULL);

	ep_requires_lock_not_held ();

	EP_LOCK_ENTER (section1)
		config_enable_disable (config, session, provider_callback_data_queue, false);
	EP_LOCK_EXIT (section1)

ep_on_exit:
	ep_requires_lock_not_held ();
	return;

ep_on_error:
	ep_exit_error_handler ();
}

EventPipeEventMetadataEvent *
ep_config_build_event_metadata_event (
	EventPipeConfiguration *config,
	const EventPipeEventInstance *source_instance,
	uint32_t metadata_id)
{
	EP_ASSERT (config != NULL);
	EP_ASSERT (source_instance != NULL);

	// The payload of the event should contain:
	// - Metadata ID
	// - GUID ProviderID.
	// - Optional event description payload.

	EventPipeEventMetadataEvent *instance = NULL;
	uint8_t *instance_payload = NULL;

	// Calculate the size of the event.
	EventPipeEvent *source_event = ep_event_instance_get_ep_event (source_instance);
	EventPipeProvider *provider = ep_event_get_provider (source_event);
	const ep_char16_t *provider_name_utf16 = ep_provider_get_provider_name_utf16 (provider);
	const uint8_t *payload_data = ep_event_get_metadata (source_event);
	uint32_t payload_data_len = ep_event_get_metadata_len (source_event);
	uint32_t provider_name_len = (uint32_t)((ep_rt_utf16_string_len (provider_name_utf16) + 1) * sizeof (ep_char16_t));
	uint32_t instance_payload_size = sizeof (metadata_id) + provider_name_len + payload_data_len;

	// Allocate the payload.
	instance_payload = ep_rt_byte_array_alloc (instance_payload_size);
	ep_raise_error_if_nok (instance_payload != NULL);

	// Fill the buffer with the payload.
	uint8_t *current;
	current = instance_payload;

	ep_write_buffer_uint32_t (&current, metadata_id);

	ep_write_buffer_string_utf16_t (&current, provider_name_utf16, provider_name_len);

	// Write the incoming payload data.
	memcpy(current, payload_data, payload_data_len);

	// Construct the metadata event instance.
	instance = ep_event_metadata_event_alloc (
		config->metadata_event,
		ep_rt_current_processor_get_number (),
		ep_rt_thread_id_t_to_uint64_t (ep_rt_current_thread_get_id ()),
		instance_payload,
		instance_payload_size,
		NULL /* pActivityId */,
		NULL /* pRelatedActivityId */);

	ep_raise_error_if_nok (instance != NULL);
	instance_payload = NULL;

	EP_ASSERT (!ep_event_get_need_stack (config->metadata_event));

	// Set the timestamp to match the source event, because the metadata event
	// will be emitted right before the source event.
	ep_event_instance_set_timestamp ((EventPipeEventInstance *)instance, ep_event_instance_get_timestamp (source_instance));

ep_on_exit:
	return instance;

ep_on_error:
	ep_rt_byte_array_free (instance_payload);
	instance = NULL;
	ep_exit_error_handler ();
}

void
ep_config_delete_deferred_providers (EventPipeConfiguration *config)
{
	EP_ASSERT (config != NULL);

	ep_requires_lock_not_held ();

	EP_LOCK_ENTER (section1)
		config_delete_deferred_providers (config);
	EP_LOCK_EXIT (section1)

ep_on_exit:
	ep_requires_lock_not_held ();
	return;

ep_on_error:
	ep_exit_error_handler ();
}

EventPipeSessionProvider *
config_get_session_provider (
	const EventPipeConfiguration *config,
	const EventPipeSession *session,
	const EventPipeProvider *provider)
{
	EP_ASSERT (config != NULL);
	EP_ASSERT (session != NULL);

	ep_requires_lock_held ();

	EventPipeSessionProvider *existing_provider = ep_session_get_session_provider (session, provider);

	ep_requires_lock_held ();
	return existing_provider;
}

EventPipeProvider *
config_get_provider (
	EventPipeConfiguration *config,
	const ep_char8_t *name)
{
	EP_ASSERT (config != NULL);
	EP_ASSERT (name != NULL);

	ep_requires_lock_held ();

	// The provider list should be non-NULL, but can be NULL on shutdown.
	ep_return_null_if_nok (!ep_rt_provider_list_is_empty (&config->provider_list));
	EventPipeProvider *provider = ep_rt_provider_list_find_by_name (&config->provider_list, name);

	ep_requires_lock_held ();
	return provider;
}

EventPipeProvider *
config_create_provider (
	EventPipeConfiguration *config,
	const ep_char8_t *provider_name,
	EventPipeCallback callback_func,
	EventPipeCallbackDataFree callback_data_free_func,
	void *callback_data,
	EventPipeProviderCallbackDataQueue *provider_callback_data_queue)
{
	EP_ASSERT (config != NULL);
	EP_ASSERT (provider_name != NULL);

	ep_requires_lock_held ();

	EventPipeProvider *provider = ep_provider_alloc (config, provider_name, callback_func, callback_data_free_func, callback_data);
	ep_raise_error_if_nok (provider != NULL);

	config_register_provider (config, provider, provider_callback_data_queue);

ep_on_exit:
	ep_requires_lock_held ();
	return provider;

ep_on_error:
	config_delete_provider (config, provider);
	provider = NULL;
	ep_exit_error_handler ();
}

void
config_delete_provider (
	EventPipeConfiguration *config,
	EventPipeProvider *provider)
{
	EP_ASSERT (config != NULL);
	EP_ASSERT (provider != NULL);

	ep_requires_lock_held ();

	config_unregister_provider (config, provider);
	provider_free (provider);

	ep_requires_lock_held ();
	return;
}

void
config_delete_deferred_providers (EventPipeConfiguration *config)
{
	EP_ASSERT (config != NULL);

	ep_requires_lock_held ();

	// The provider list should be non-NULL, but can be NULL on shutdown.
	const ep_rt_provider_list_t *provider_list = &config->provider_list;
	if (!ep_rt_provider_list_is_empty (provider_list)) {
		ep_rt_provider_list_iterator_t iterator = ep_rt_provider_list_iterator_begin (provider_list);

		while (!ep_rt_provider_list_iterator_end (provider_list, &iterator)) {
			EventPipeProvider *provider = ep_rt_provider_list_iterator_value (&iterator);
			EP_ASSERT (provider != NULL);

			// Get next item before deleting current.
			ep_rt_provider_list_iterator_next (&iterator);
			if (ep_provider_get_delete_deferred (provider))
				config_delete_provider (config, provider);
		}
	}

	ep_requires_lock_held ();
	return;
}

void
config_enable_disable (
	EventPipeConfiguration *config,
	const EventPipeSession *session,
	EventPipeProviderCallbackDataQueue *provider_callback_data_queue,
	bool enable)
{
	ep_requires_lock_held ();

	EP_ASSERT (config != NULL);
	EP_ASSERT (session != NULL);

	// The provider list should be non-NULL, but can be NULL on shutdown.
	const ep_rt_provider_list_t *provider_list = &config->provider_list;
	if (!ep_rt_provider_list_is_empty (provider_list)) {
		for (ep_rt_provider_list_iterator_t iterator = ep_rt_provider_list_iterator_begin (provider_list); !ep_rt_provider_list_iterator_end (provider_list, &iterator); ep_rt_provider_list_iterator_next (&iterator)) {
			EventPipeProvider *provider = ep_rt_provider_list_iterator_value (&iterator);
			if (provider) {
				// Enable/Disable the provider if it has been configured.
				EventPipeSessionProvider *session_provider = config_get_session_provider (config, session, provider);
				if (session_provider) {
					int64_t keyword_for_all_sessions;
					EventPipeEventLevel level_for_all_sessions;
					EventPipeProviderCallbackData provider_callback_data;
					memset (&provider_callback_data, 0, sizeof (provider_callback_data));
					config_compute_keyword_and_level (config, provider, &keyword_for_all_sessions, &level_for_all_sessions);
					if (enable) {
						provider_set_config (
							provider,
							keyword_for_all_sessions,
							level_for_all_sessions,
							ep_session_get_mask (session),
							ep_session_provider_get_keywords (session_provider),
							ep_session_provider_get_logging_level (session_provider),
							ep_session_provider_get_filter_data (session_provider),
							&provider_callback_data);
					} else {
						provider_unset_config (
							provider,
							keyword_for_all_sessions,
							level_for_all_sessions,
							ep_session_get_mask (session),
							ep_session_provider_get_keywords (session_provider),
							ep_session_provider_get_logging_level (session_provider),
							ep_session_provider_get_filter_data (session_provider),
							&provider_callback_data);
					}
					if (provider_callback_data_queue)
						ep_provider_callback_data_queue_enqueue (provider_callback_data_queue, &provider_callback_data);
					ep_provider_callback_data_fini (&provider_callback_data);
				}
			}
		}
	}

	ep_requires_lock_held ();
	return;
}

/*
 * EventPipeEventMetadataEvent.
 */

EventPipeEventMetadataEvent *
ep_event_metadata_event_alloc (
	EventPipeEvent *ep_event,
	uint32_t proc_num,
	uint64_t thread_id,
	uint8_t *data,
	uint32_t data_len,
	const uint8_t *activity_id,
	const uint8_t *related_activity_id)
{
	EventPipeEventMetadataEvent *instance = ep_rt_object_alloc (EventPipeEventMetadataEvent);
	ep_raise_error_if_nok (instance != NULL);

	ep_raise_error_if_nok (ep_event_instance_init (
		&instance->event_instance,
		ep_event,
		proc_num,
		thread_id,
		data,
		data_len,
		activity_id,
		related_activity_id) != NULL);

	instance->payload_buffer = data;
	instance->payload_buffer_len = data_len;

ep_on_exit:
	return instance;

ep_on_error:
	ep_event_metadata_event_free (instance);
	instance = NULL;
	ep_exit_error_handler ();
}

void
ep_event_metadata_event_free (EventPipeEventMetadataEvent *metadata_event)
{
	ep_return_void_if_nok (metadata_event != NULL);

	ep_event_instance_fini (&metadata_event->event_instance);
	ep_rt_byte_array_free (metadata_event->payload_buffer);
	ep_rt_object_free (metadata_event);
}

#endif /* !defined(EP_INCLUDE_SOURCE_FILES) || defined(EP_FORCE_INCLUDE_SOURCE_FILES) */
#endif /* ENABLE_PERFTRACING */

#if !defined(ENABLE_PERFTRACING) || (defined(EP_INCLUDE_SOURCE_FILES) && !defined(EP_FORCE_INCLUDE_SOURCE_FILES))
extern const char quiet_linker_empty_file_warning_eventpipe_configuration;
const char quiet_linker_empty_file_warning_eventpipe_configuration = 0;
#endif
