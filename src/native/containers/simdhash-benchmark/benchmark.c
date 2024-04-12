#include <stdint.h>
#include <inttypes.h>
#include <stdio.h>
#include <assert.h>
#include <sys/time.h>
#include <strings.h>

#include "../dn-vector.h"
#include "../dn-simdhash.h"
#include "../dn-simdhash-utils.h"
#include "../dn-simdhash-specializations.h"

#include "measurement.h"
#include "all-measurements.h"

dn_simdhash_string_ptr_t *all_measurements;

#undef MEASUREMENT
#define MEASUREMENT(name, data_type, setup, teardown, body) \
    measurement_info DN_SIMDHASH_GLUE(name, _measurement_info) = { \
        #name, \
        setup, \
        DN_SIMDHASH_GLUE(measurement_, name), \
        teardown \
    };

#include "all-measurements.h"

void
dn_simdhash_assert_fail (const char *file, int line, const char *condition) {
    fprintf(stderr, "simdhash assertion failed at %s:%i:\n%s\n", file, line, condition);
    fflush(stderr);
    abort();
}

int64_t get_100ns_ticks () {
    struct timeval tv;
    gettimeofday(&tv, NULL);
    return ((int64_t)tv.tv_sec * 1000000 + tv.tv_usec) * 10;
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
        if (strcasecmp(name, args->argv[i]) == 0) {
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
        warmup_iterations = 1000,
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

    int64_t run_elapsed_average = run_elapsed_total / steps / necessary_iterations;

    args->result = 0;
    printf(
        "%" PRId64 " step(s): avg %" PRId64 " min %" PRId64 " max %" PRId64 "\n",
        steps, run_elapsed_average, run_elapsed_min / necessary_iterations, run_elapsed_max / necessary_iterations
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
