#include <config.h>

#ifdef ENABLE_PERFTRACING
#include "ep-rt-config.h"
#if !defined(EP_INCLUDE_SOURCE_FILES) || defined(EP_FORCE_INCLUDE_SOURCE_FILES)

#include "ep.h"

/*
 * Forward declares of all static functions.
 */

static
void
config_compute_keyword_and_level_lock_held (
	const EventPipeConfiguration *config,
	const EventPipeProvider *provider,
	int64_t *keyword_for_all_sessions,
	EventPipeEventLevel *level_for_all_sessions);

static
bool
config_register_provider_lock_held (
	EventPipeConfiguration *config,
	EventPipeProvider *provider,
	EventPipeProviderCallbackDataQueue *provider_callback_data_queue);

static
bool
config_unregister_provider_lock_held (
	EventPipeConfiguration *config,
	EventPipeProvider *provider);

/*
 * EventPipeConfiguration.
 */

static
void
config_compute_keyword_and_level_lock_held (
	const EventPipeConfiguration *config,
	const EventPipeProvider *provider,
	int64_t *keyword_for_all_sessions,
	EventPipeEventLevel *level_for_all_sessions)
{
	ep_rt_config_requires_lock_held ();
	EP_ASSERT (provider != NULL);
	EP_ASSERT (keyword_for_all_sessions != NULL);
	EP_ASSERT (level_for_all_sessions != NULL);

	*keyword_for_all_sessions = 0;
	*level_for_all_sessions = EP_EVENT_LEVEL_LOG_ALWAYS;

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

	ep_rt_config_requires_lock_held ();
	return;
}

static
bool
config_register_provider_lock_held (
	EventPipeConfiguration *config,
	EventPipeProvider *provider,
	EventPipeProviderCallbackDataQueue *provider_callback_data_queue)
{
	ep_rt_config_requires_lock_held ();
	EP_ASSERT (config != NULL);
	EP_ASSERT (provider != NULL);

	// See if we've already registered this provider.
	EventPipeProvider *existing_provider = ep_config_get_provider_lock_held (config, ep_provider_get_provider_name (provider));
	if (existing_provider)
		return false;

	// The provider has not been registered, so register it.
	ep_rt_provider_list_append (ep_config_get_provider_list_ref (config), provider);

	int64_t keyword_for_all_sessions;
	EventPipeEventLevel level_for_all_sessions;
	config_compute_keyword_and_level_lock_held (config, provider, &keyword_for_all_sessions, &level_for_all_sessions);

	for (int i = 0; i < EP_MAX_NUMBER_OF_SESSIONS; i++) {
		// Entering EventPipe lock gave us a barrier, we don't need more of them.
		EventPipeSession *session = ep_volatile_load_session_without_barrier (i);
		if (session) {
			EventPipeSessionProviderList *providers = ep_session_get_providers (session);
			EP_ASSERT (providers != NULL);

			EventPipeSessionProvider *session_provider = ep_rt_session_provider_list_find_by_name (ep_session_provider_list_get_providers_cref (providers), ep_provider_get_provider_name (provider));
			if (session_provider) {
				EventPipeProviderCallbackData provider_callback_data;
				ep_provider_set_config_lock_held (
					provider,
					keyword_for_all_sessions,
					level_for_all_sessions,
					((uint64_t)1 << ep_session_get_index (session)),
					ep_session_provider_get_keywords (session_provider),
					ep_session_provider_get_logging_level (session_provider),
					ep_session_provider_get_filter_data (session_provider),
					&provider_callback_data);
				ep_provider_callback_data_queue_enqueue (provider_callback_data_queue, &provider_callback_data);
			}
		}
	}

	ep_rt_config_requires_lock_held ();
	return true;
}

static
bool
config_unregister_provider_lock_held (
	EventPipeConfiguration *config,
	EventPipeProvider *provider)
{
	ep_rt_config_requires_lock_held ();
	EP_ASSERT (config != NULL);

	EventPipeProvider *existing_provider = NULL;
	ep_rt_provider_list_t *provider_list = ep_config_get_provider_list_ref (config);

	// The provider list should be non-NULL, but can be NULL on shutdown.
	if (!ep_rt_provider_list_is_empty (provider_list)) {
		// If we found the provider, remove it.
		if (ep_rt_provider_list_find (provider_list, provider, &existing_provider))
			ep_rt_provider_list_remove (provider_list, existing_provider);
	}

	ep_rt_config_requires_lock_held ();
	return (existing_provider != NULL);
}

EventPipeConfiguration *
ep_config_init (EventPipeConfiguration *config)
{
	ep_rt_config_requires_lock_not_held ();

	ep_return_false_if_nok (config != NULL);

	ep_config_set_config_provider (config, ep_create_provider (ep_config_get_default_provider_name_utf8 (), NULL, NULL));
	ep_raise_error_if_nok (ep_config_get_config_provider (config) != NULL);

	// Create the metadata event.
	ep_config_set_metadata_event (config, ep_provider_add_event (
		ep_config_get_config_provider (config),
		0, /* event_id */
		0, /* keywords */
		0, /* event_version */
		EP_EVENT_LEVEL_LOG_ALWAYS,
		false, /* need_stack */
		NULL, /* meatadata */
		0));  /* metadata_len */
	ep_raise_error_if_nok (ep_config_get_metadata_event (config) != NULL);

ep_on_exit:
	ep_rt_config_requires_lock_not_held ();
	return config;

ep_on_error:
	ep_config_shutdown (config);

	config = NULL;
	ep_exit_error_handler ();
}

void
ep_config_shutdown (EventPipeConfiguration *config)
{
	ep_rt_config_requires_lock_not_held ();

	ep_event_free (ep_config_get_metadata_event (config));
	ep_config_set_metadata_event (config, NULL);

	ep_delete_provider (ep_config_get_config_provider (config));
	ep_config_set_config_provider (config, NULL);

	// Take the lock before manipulating the list.
	EP_CONFIG_LOCK_ENTER
		// We don't delete provider itself because it can be in-use
		ep_rt_provider_list_free (ep_config_get_provider_list_ref (config), NULL);
	EP_CONFIG_LOCK_EXIT

ep_on_exit:
	ep_rt_config_requires_lock_not_held ();
	return;

ep_on_error:
	ep_exit_error_handler ();
}

EventPipeProvider *
ep_config_create_provider (
	EventPipeConfiguration *config,
	const ep_char8_t *provider_name,
	EventPipeCallback callback_func,
	void *callback_data,
	EventPipeProviderCallbackDataQueue *provider_callback_data_queue)
{
	ep_rt_config_requires_lock_not_held ();

	ep_return_null_if_nok (config != NULL && provider_name != NULL);

	EventPipeProvider *provider = NULL;
	EP_CONFIG_LOCK_ENTER
		provider = ep_config_create_provider_lock_held (config, provider_name, callback_func, callback_data, provider_callback_data_queue);
		ep_raise_error_if_nok_holding_lock (provider != NULL);
	EP_CONFIG_LOCK_EXIT

ep_on_exit:
	ep_rt_config_requires_lock_not_held ();
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
	ep_rt_config_requires_lock_not_held ();

	ep_return_void_if_nok (config != NULL && provider != NULL);

	EP_CONFIG_LOCK_ENTER
		ep_config_delete_provider_lock_held (config, provider);
	EP_CONFIG_LOCK_EXIT

ep_on_exit:
	ep_rt_config_requires_lock_not_held ();
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
	ep_rt_config_requires_lock_not_held ();

	ep_return_void_if_nok (config != NULL && session != NULL);

	EP_CONFIG_LOCK_ENTER
		ep_config_enable_disable_lock_held (config, session, provider_callback_data_queue, true);
	EP_CONFIG_LOCK_EXIT

ep_on_exit:
	ep_rt_config_requires_lock_not_held ();
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
	ep_rt_config_requires_lock_not_held ();

	ep_return_void_if_nok (config != NULL && session != NULL);

	EP_CONFIG_LOCK_ENTER
		ep_config_enable_disable_lock_held (config, session, provider_callback_data_queue, false);
	EP_CONFIG_LOCK_EXIT

ep_on_exit:
	ep_rt_config_requires_lock_not_held ();
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
	ep_return_null_if_nok (config != NULL && source_instance != NULL);

	// The payload of the event should contain:
	// - Metadata ID
	// - GUID ProviderID.
	// - Optional event description payload.

	uint8_t *instance_payload = NULL;

	// Calculate the size of the event.
	EventPipeEvent *source_event = ep_event_instance_get_ep_event (source_instance);
	EventPipeProvider *provider = ep_event_get_provider (source_event);
	const ep_char16_t *provider_name_utf16 = ep_provider_get_provider_name_utf16 (provider);
	const uint8_t *payload_data = ep_event_instance_get_data (source_instance);
	uint32_t payload_data_len = ep_event_instance_get_data_len (source_instance);
	uint32_t provider_name_len = (ep_rt_utf16_string_len (provider_name_utf16) + 1) * sizeof (ep_char16_t);
	uint32_t instance_payload_size = sizeof (metadata_id) + provider_name_len + payload_data_len;
	
	// Allocate the payload.
	instance_payload = ep_rt_byte_array_alloc (instance_payload_size);
	ep_raise_error_if_nok (instance_payload != NULL);
	
	// Fill the buffer with the payload.
	uint8_t *current = instance_payload;

	memcpy(current, &metadata_id, sizeof(metadata_id));
	current += sizeof(metadata_id);

	memcpy(current, provider_name_utf16, provider_name_len);
	current += provider_name_len;

	// Write the incoming payload data.
	memcpy(current, payload_data, payload_data_len);

	// Construct the metadata event instance.
	EventPipeEventMetadataEvent *instance = ep_event_metdata_event_alloc (
		ep_config_get_metadata_event (config),
		ep_rt_current_processor_get_number (),
		ep_rt_current_thread_get_id (),
		instance_payload,
		instance_payload_size,
		NULL /* pActivityId */,
		NULL /* pRelatedActivityId */);

	ep_raise_error_if_nok (instance != NULL);
	instance_payload = NULL;

	EP_ASSERT (ep_event_get_need_stack (ep_config_get_metadata_event (config)) == false);

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
	ep_rt_config_requires_lock_not_held ();

	ep_return_void_if_nok (config != NULL);

	EP_CONFIG_LOCK_ENTER
		ep_config_delete_deferred_providers_lock_held (config);
	EP_CONFIG_LOCK_EXIT

ep_on_exit:
	ep_rt_config_requires_lock_not_held ();
	return;

ep_on_error:
	ep_exit_error_handler ();
}

EventPipeSessionProvider *
ep_config_get_session_provider_lock_held (
	const EventPipeConfiguration *config,
	const EventPipeSession *session,
	const EventPipeProvider *provider)
{
	ep_rt_config_requires_lock_held ();

	ep_return_null_if_nok (config != NULL && session != NULL);

	EventPipeSessionProvider *existing_provider = ep_session_get_session_provider_lock_held (session, provider);

	ep_rt_config_requires_lock_held ();
	return existing_provider;
}

EventPipeProvider *
ep_config_get_provider_lock_held (
	EventPipeConfiguration *config,
	const ep_char8_t *name)
{
	ep_rt_config_requires_lock_held ();

	ep_return_null_if_nok (config != NULL && name != NULL);

	// The provider list should be non-NULL, but can be NULL on shutdown.
	ep_return_null_if_nok (ep_rt_provider_list_is_empty (ep_config_get_provider_list_cref (config)) != true);
	EventPipeProvider *provider = ep_rt_provider_list_find_by_name (ep_config_get_provider_list_cref (config), name);

	ep_rt_config_requires_lock_held ();
	return provider;
}

EventPipeProvider *
ep_config_create_provider_lock_held (
	EventPipeConfiguration *config,
	const ep_char8_t *provider_name,
	EventPipeCallback callback_func,
	void *callback_data,
	EventPipeProviderCallbackDataQueue *provider_callback_data_queue)
{
	ep_rt_config_requires_lock_held ();

	ep_return_null_if_nok (config != NULL && provider_name != NULL);
	
	EventPipeProvider *provider = ep_provider_alloc (config, provider_name, callback_func, callback_data);
	ep_raise_error_if_nok (provider != NULL);
	ep_raise_error_if_nok (config_register_provider_lock_held (config, provider, provider_callback_data_queue) == true);

ep_on_exit:
	ep_rt_config_requires_lock_held ();
	return provider;

ep_on_error:
	ep_config_delete_provider_lock_held (config, provider);

	provider = NULL;
	ep_exit_error_handler ();
}

void
ep_config_delete_provider_lock_held (
	EventPipeConfiguration *config,
	EventPipeProvider *provider)
{
	ep_rt_config_requires_lock_held ();

	ep_return_void_if_nok (config != NULL);

	config_unregister_provider_lock_held (config, provider);
	ep_provider_free (provider);

	ep_rt_config_requires_lock_held ();
	return;
}

void
ep_config_delete_deferred_providers_lock_held (EventPipeConfiguration *config)
{
	ep_rt_config_requires_lock_held ();

	ep_return_void_if_nok (config != NULL);

	// The provider list should be non-NULL, but can be NULL on shutdown.
	const ep_rt_provider_list_t *provider_list = ep_config_get_provider_list_ref (config);
	if (!ep_rt_provider_list_is_empty (provider_list)) {
		ep_rt_provider_list_iterator_t iterator;
		ep_rt_provider_list_iterator_begin (provider_list, &iterator);

		while (!ep_rt_provider_list_iterator_end (provider_list, &iterator)) {
			EventPipeProvider *provider = ep_rt_provider_list_iterator_value (&iterator);
			EP_ASSERT (provider != NULL);

			// Get next item before deleting current.
			ep_rt_provider_list_iterator_next (provider_list, &iterator);
			if (ep_provider_get_delete_deferred (provider))
				ep_config_delete_provider_lock_held (config, provider);
		}
	}

	ep_rt_config_requires_lock_held ();
	return;
}

void
ep_config_enable_disable_lock_held (
	EventPipeConfiguration *config,
	const EventPipeSession *session,
	EventPipeProviderCallbackDataQueue *provider_callback_data_queue,
	bool enable)
{
	ep_rt_config_requires_lock_held ();

	ep_return_void_if_nok (config != NULL && session != NULL);

	// The provider list should be non-NULL, but can be NULL on shutdown.
	const ep_rt_provider_list_t *provider_list = ep_config_get_provider_list_cref (config);
	if (!ep_rt_provider_list_is_empty (provider_list)) {
		ep_rt_provider_list_iterator_t iterator;
		for (ep_rt_provider_list_iterator_begin (provider_list, &iterator); !ep_rt_provider_list_iterator_end (provider_list, &iterator); ep_rt_provider_list_iterator_next (provider_list, &iterator)) {
			EventPipeProvider *provider = ep_rt_provider_list_iterator_value (&iterator);
			if (provider) {
				// Enable/Disable the provider if it has been configured.
				EventPipeSessionProvider *session_provider = ep_config_get_session_provider_lock_held (config, session, provider);
				if (session_provider) {
					int64_t keyword_for_all_sessions;
					EventPipeEventLevel level_for_all_sessions;
					EventPipeProviderCallbackData provider_callback_data;
					config_compute_keyword_and_level_lock_held (config, provider, &keyword_for_all_sessions, &level_for_all_sessions);
					if (enable) {
						ep_provider_set_config_lock_held (
							provider,
							keyword_for_all_sessions,
							level_for_all_sessions,
							ep_session_get_mask (session),
							ep_session_provider_get_keywords (session_provider),
							ep_session_provider_get_logging_level (session_provider),
							ep_session_provider_get_filter_data (session_provider),
							&provider_callback_data);
					} else {
						ep_provider_unset_config_lock_held (
							provider,
							keyword_for_all_sessions,
							level_for_all_sessions,
							ep_session_get_mask (session),
							ep_session_provider_get_keywords (session_provider),
							ep_session_provider_get_logging_level (session_provider),
							ep_session_provider_get_filter_data (session_provider),
							&provider_callback_data);
					}
					ep_provider_callback_data_queue_enqueue (provider_callback_data_queue, &provider_callback_data);
				}
			}
		}
	}

	ep_rt_config_requires_lock_held ();
	return;
}

#endif /* !defined(EP_INCLUDE_SOURCE_FILES) || defined(EP_FORCE_INCLUDE_SOURCE_FILES) */
#endif /* ENABLE_PERFTRACING */

#ifndef EP_INCLUDE_SOURCE_FILES
extern const char quiet_linker_empty_file_warning_eventpipe_configuration;
const char quiet_linker_empty_file_warning_eventpipe_configuration = 0;
#endif
