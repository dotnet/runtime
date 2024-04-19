// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdint.h>
#include <inttypes.h>
#include <stdio.h>
#include <assert.h>
#include <time.h>
#include <string.h>

#ifdef _MSC_VER
#include <windows.h>
#else
#include <sys/time.h>
#include <strings.h>
char *strcasestr(const char *haystack, const char *needle);
#endif

#include "../dn-vector.h"
#include "../dn-simdhash.h"
#include "../dn-simdhash-utils.h"
#include "../dn-simdhash-specializations.h"

#include "measurement.h"

dn_simdhash_string_ptr_t *all_measurements;

#undef MEASUREMENT
#define MEASUREMENT(name, data_type, setup, teardown, body) \
    extern measurement_info DN_SIMDHASH_GLUE(name, _measurement_info);

// Suppress actual codegen
#define MEASUREMENTS_IMPLEMENTATION 0

#include "all-measurements.h"

void
dn_simdhash_assert_fail (const char *file, int line, const char *condition) {
    fprintf(stderr, "simdhash assertion failed at %s:%i:\n%s\n", file, line, condition);
    fflush(stderr);
    abort();
}

#define MTICKS_PER_SEC (10 * 1000 * 1000)

int64_t get_100ns_ticks () {
#ifdef _MSC_VER
	static LARGE_INTEGER freq;
	static UINT64 start_time;
	UINT64 cur_time;
	LARGE_INTEGER value;

	if (!freq.QuadPart) {
		QueryPerformanceFrequency(&freq);
		QueryPerformanceCounter(&value);
		start_time = value.QuadPart;
	}
	QueryPerformanceCounter(&value);
	cur_time = value.QuadPart;
	return (int64_t)((cur_time - start_time) * (double)MTICKS_PER_SEC / freq.QuadPart);
#else
    struct timespec ts;
    // FIXME: Use clock_monotonic for wall time instead? I think process time is what we want
#ifdef __wasm
    dn_simdhash_assert(clock_gettime(CLOCK_MONOTONIC, &ts) == 0);
#else
    dn_simdhash_assert(clock_gettime(CLOCK_PROCESS_CPUTIME_ID, &ts) == 0);
#endif
    return ((int64_t)ts.tv_sec * MTICKS_PER_SEC + ts.tv_nsec / 100);
#endif
}

void init_measurements () {
    if (!all_measurements)
        all_measurements = dn_simdhash_string_ptr_new (0, NULL);

    #undef MEASUREMENT
    #define MEASUREMENT(name, data_type, setup, teardown, body) \
        dn_simdhash_string_ptr_try_add(all_measurements, #name, &DN_SIMDHASH_GLUE(name, _measurement_info));

    #include "all-measurements.h"
}

int64_t run_measurement (int iteration_count, setup_func setup, measurement_func measurement, measurement_func teardown) {
    void *data = NULL;
    if (setup)
        data = setup();

    int64_t started = get_100ns_ticks();
    for (int i = 0; i < iteration_count; i++)
        measurement(data);
    int64_t ended = get_100ns_ticks();

    if (teardown)
        teardown(data);

    return ended - started;
}

typedef struct {
    int argc;
    char **argv;
    int result;
} main_args;

void foreach_measurement (const char *name, void *_info, void *_args) {
    measurement_info *info = _info;
    main_args *args = _args;

    uint8_t match = args->argc <= 1;
    for (int i = 1; i < args->argc; i++) {
#ifdef _MSC_VER
        if (strstr(name, args->argv[i])) {
#else
        if (strcasestr(name, args->argv[i])) {
#endif
            match = 1;
            break;
        }
    }

    if (!match)
        return;

    printf("%s: ", name);
    fflush(stdout);

    run_measurement(100, info->setup, info->func, info->teardown);

    int64_t overhead = run_measurement(1, info->setup, info->func, info->teardown);

    int64_t warmup_duration = 20000000,
        target_step_duration = 10000000,
        target_duration = warmup_duration * 10,
        warmup_iterations = 500,
        warmup_until = get_100ns_ticks() + warmup_duration,
        warmup_elapsed_total = 0,
        warmup_count = 0;

    do {
        warmup_elapsed_total += run_measurement(warmup_iterations, info->setup, info->func, info->teardown) - overhead;
        warmup_count++;
    } while (get_100ns_ticks() < warmup_until);

    int64_t average_warmup_duration = warmup_elapsed_total / warmup_count,
        necessary_iterations = (target_step_duration * warmup_iterations / average_warmup_duration),
        steps = 0,
        run_elapsed_total = 0,
        run_elapsed_min = INT64_MAX,
        run_elapsed_max = INT64_MIN,
        run_until = get_100ns_ticks() + target_duration;

    if (necessary_iterations < 16)
        necessary_iterations = 16;
    // HACK: Reduce minor variation in iteration count
    necessary_iterations = next_power_of_two((uint32_t)necessary_iterations);

    printf(
        "Warmed %" PRId64 " time(s). Running %" PRId64 " iterations... ",
        warmup_count, necessary_iterations
    );
    fflush(stdout);

    do {
        int64_t step_duration = run_measurement(necessary_iterations, info->setup, info->func, info->teardown) - overhead;
        run_elapsed_total += step_duration;
        if (step_duration < run_elapsed_min)
            run_elapsed_min = step_duration;
        if (step_duration > run_elapsed_max)
            run_elapsed_max = step_duration;
        steps++;
    } while (get_100ns_ticks() < run_until);

    double run_elapsed_average = (double)(run_elapsed_total) / steps / necessary_iterations / 100.0;

    args->result = 0;
    printf(
        "%" PRId64 " step(s): avg %.3fns min %.3fns max %.3fns\n",
        steps,
        run_elapsed_average,
        (double)run_elapsed_min / necessary_iterations / 100.0,
        (double)run_elapsed_max / necessary_iterations / 100.0
    );
    fflush(stdout);
}

int main (int argc, char* argv[]) {
    init_measurements();

    main_args args = {
        argc, argv, 1
    };
    dn_simdhash_string_ptr_foreach(all_measurements, foreach_measurement, &args);

    fflush(stdout);
    fflush(stderr);

    switch (args.result) {
        case 0:
            break;
        case 1:
            // no benchmarks run
            fprintf(stderr, "No benchmarks run. List of all benchmarks follows:\n");
            break;
        default:
            fprintf(stderr, "Unknown failure!\n");
            break;
    }

    return args.result;
}
