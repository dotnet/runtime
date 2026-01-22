// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdio.h>
#include <stdlib.h>
#include <stdint.h>
#include <inttypes.h>

#include "../dn-ebr.h"
#include <minipal/time.h>

static void fatal_cb(const char *msg)
{
    fprintf(stderr, "FATAL: %s\n", msg);
    fflush(stderr);
    abort();
}

int main(int argc, char **argv)
{
    (void)argc; (void)argv;

    uint64_t iterations = 10000000ULL; // default 10M enter/exit pairs
    if (argc >= 2) {
        char *end = NULL;
        uint64_t val = strtoull(argv[1], &end, 10);
        if (end && *end == '\0' && val > 0) iterations = val;
    }

    dn_ebr_collector_t collector_storage;
    dn_ebr_collector_t *collector = dn_ebr_collector_init(&collector_storage, 1024 /*bytes*/, DN_DEFAULT_ALLOCATOR, fatal_cb);
    if (!collector) {
        fprintf(stderr, "Failed to init collector\n");
        return 2;
    }

    // Warm up to initialize TLS/thread state
    for (int i = 0; i < 10000; i++) {
        dn_ebr_enter_critical_region(collector);
        dn_ebr_exit_critical_region(collector);
    }

    int64_t freq = minipal_hires_tick_frequency();
    int64_t start = minipal_hires_ticks();

    for (uint64_t i = 0; i < iterations; i++) {
        dn_ebr_enter_critical_region(collector);
        dn_ebr_exit_critical_region(collector);
    }

    int64_t end = minipal_hires_ticks();

    double seconds = (double)(end - start) / (double)freq;
    double pairs_per_sec = (double)iterations / (seconds > 0.0 ? seconds : 1.0);
    double ns_per_pair = seconds * 1e9 / (double)iterations;

    printf("EBR enter/exit: iterations=%" PRIu64 ", seconds=%.6f, pairs/sec=%.0f, ns/pair=%.2f\n",
           iterations, seconds, pairs_per_sec, ns_per_pair);

    dn_ebr_collector_shutdown(collector);
    return 0;
}
