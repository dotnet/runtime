#if defined(_MSC_VER) && defined(_DEBUG)
#include "ep-tests-debug.h"
#endif

#include <eventpipe/ep.h>
#include <eglib/test/test.h>

#ifdef _CRTDBG_MAP_ALLOC
static _CrtMemState eventpipe_memory_start_snapshot;
static _CrtMemState eventpipe_memory_end_snapshot;
static _CrtMemState eventpipe_memory_diff_snapshot;
#endif

static RESULT
test_rt_setup (void)
{
#ifdef _CRTDBG_MAP_ALLOC
	_CrtMemCheckpoint (&eventpipe_memory_start_snapshot);
#endif
	return NULL;
}

static RESULT
test_rt_perf_frequency (void)
{
	return (ep_perf_frequency_query () > 0) ? NULL : FAILED ("Frequency to low");
}

static RESULT
test_rt_perf_timestamp (void)
{
	RESULT result = NULL;
	uint32_t test_location = 0;
	int64_t frequency = 0;
	double elapsed_time_ms = 0;

	ep_timestamp_t start = ep_perf_timestamp_get ();
	g_usleep (10 * 1000);
	ep_timestamp_t stop = ep_perf_timestamp_get ();

	test_location = 1;

	ep_raise_error_if_nok (stop > start);

	test_location = 2;

	frequency = ep_perf_frequency_query ();
	ep_raise_error_if_nok (frequency > 0);

	test_location = 3;

	elapsed_time_ms = ((double)(stop - start) / (double)frequency) * 1000;

	ep_raise_error_if_nok (elapsed_time_ms > 0);

	test_location = 4;

	ep_raise_error_if_nok (elapsed_time_ms > 10);

ep_on_exit:
	return result;

ep_on_error:
	if (!result)
		result = FAILED ("Failed at test location=%i", test_location);
	ep_exit_error_handler ();
}

static RESULT
test_rt_system_time (void)
{
	RESULT result = NULL;
	uint32_t test_location = 0;
	EventPipeSystemTime time1;
	EventPipeSystemTime time2;
	bool time_diff = false;

	ep_system_time_get (&time1);

	ep_raise_error_if_nok (ep_system_time_get_year (&time1) > 1600 && ep_system_time_get_year (&time1) < 30828);
	test_location = 1;

	ep_raise_error_if_nok (ep_system_time_get_month (&time1) > 0 && ep_system_time_get_month (&time1) < 13);
	test_location = 2;

	ep_raise_error_if_nok (ep_system_time_get_day (&time1) > 0 && ep_system_time_get_day (&time1) < 32);
	test_location = 3;

	ep_raise_error_if_nok (ep_system_time_get_day_of_week (&time1) >= 0 && ep_system_time_get_day_of_week (&time1) < 7);
	test_location = 4;

	ep_raise_error_if_nok (ep_system_time_get_hour (&time1) >= 0 && ep_system_time_get_hour (&time1) < 24);
	test_location = 5;

	ep_raise_error_if_nok (ep_system_time_get_minute (&time1) >= 0 && ep_system_time_get_minute (&time1) < 60);
	test_location = 6;

	ep_raise_error_if_nok (ep_system_time_get_second (&time1) >= 0 && ep_system_time_get_second (&time1) < 60);
	test_location = 7;

	ep_raise_error_if_nok (ep_system_time_get_milliseconds (&time1) >= 0 && ep_system_time_get_milliseconds (&time1) < 1000);
	test_location = 8;

	g_usleep (1000 * 1000);

	ep_system_time_get (&time2);

	time_diff |= ep_system_time_get_year (&time1) != ep_system_time_get_year (&time2);
	time_diff |= ep_system_time_get_month (&time1) != ep_system_time_get_month (&time2);
	time_diff |= ep_system_time_get_day (&time1) != ep_system_time_get_day (&time2);
	time_diff |= ep_system_time_get_day_of_week (&time1) != ep_system_time_get_day_of_week (&time2);
	time_diff |= ep_system_time_get_hour (&time1) != ep_system_time_get_hour (&time2);
	time_diff |= ep_system_time_get_minute (&time1) != ep_system_time_get_minute (&time2);
	time_diff |= ep_system_time_get_second (&time1) != ep_system_time_get_second (&time2);
	time_diff |= ep_system_time_get_milliseconds (&time1) != ep_system_time_get_milliseconds (&time2);

	ep_raise_error_if_nok (time_diff == true);

ep_on_exit:
	return result;

ep_on_error:
	if (!result)
		result = FAILED ("Failed at test location=%i", test_location);
	ep_exit_error_handler ();
}

static RESULT
test_rt_system_timestamp (void)
{
	RESULT result = NULL;
	uint32_t test_location = 0;

	ep_system_timestamp_t start = ep_system_timestamp_get ();
	g_usleep (10 * 1000);
	ep_system_timestamp_t stop = ep_system_timestamp_get ();

	test_location = 1;

	ep_raise_error_if_nok (stop > start);

ep_on_exit:
	return result;

ep_on_error:
	if (!result)
		result = FAILED ("Failed at test location=%i", test_location);
	ep_exit_error_handler ();
}

static RESULT
test_rt_teardown (void)
{
#ifdef _CRTDBG_MAP_ALLOC
	_CrtMemCheckpoint (&eventpipe_memory_end_snapshot);
	if ( _CrtMemDifference( &eventpipe_memory_diff_snapshot, &eventpipe_memory_start_snapshot, &eventpipe_memory_end_snapshot) ) {
		_CrtMemDumpStatistics( &eventpipe_memory_diff_snapshot );
		return FAILED ("Memory leak detected!");
	}
#endif
	return NULL;
}

static Test ep_rt_tests [] = {
	{"test_rt_setup", test_rt_setup},
	{"test_rt_perf_frequency", test_rt_perf_frequency},
	{"test_rt_perf_timestamp", test_rt_perf_timestamp},
	{"test_rt_system_time", test_rt_system_time},
	{"test_rt_system_timestamp", test_rt_system_timestamp},
	{"test_rt_teardown", test_rt_teardown},
	{NULL, NULL}
};

DEFINE_TEST_GROUP_INIT(ep_rt_tests_init, ep_rt_tests)
