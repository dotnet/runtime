// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

/*
Demonstrates basic usage of the low-level tracepoint.h interface.

The tracepoint.h interface is not intended to be used directly. Developers
that want to add tracepoints to their code will likely prefer the high-level
API provided by <tracepoint/tracepoint-provider.h>.
*/

#include <tracepoint/tracepoint.h>
#include <stdio.h>

// A tracepoint_provider_state represents a connection to the tracing system. It
// is usually a global so that all code in the component can share the
// connection.
tracepoint_provider_state provider = TRACEPOINT_PROVIDER_STATE_INIT;

int main()
{
    int err;

    // A tracepoint_state represents a named user_events tracepoint.
    tracepoint_state unopened_tracepoint = TRACEPOINT_STATE_INIT;

    // The tracepoint is inert before it is opened.
    err = TRACEPOINT_ENABLED(&unopened_tracepoint);
    printf("TRACEPOINT_ENABLED with unopened tracepoint: %d\n", err); // Expect 0.
    err = tracepoint_write(&unopened_tracepoint, 1, &(struct iovec){}); // No-op.
    printf("tracepoint_write with unopened tracepoint: %d\n", err); // Expect EBADF.

    // The provider is inert before it is opened.
    tracepoint_close_provider(&provider); // No-op.
    err = tracepoint_connect(&unopened_tracepoint, &provider, "TracepointName"); // No-op.
    printf("tracepoint_connect with unopened provider: %d\n", err); // Expect 0.

    // Provider should be opened at component initialization.
    // Since tracing is not usually the core functionality of an application, and
    // since tracing with an unopened provider is a safe no-op, the error code
    // returned by tracepoint_open_provider will usually be ignored in retail
    // code. It is used mostly for debugging.
    err = tracepoint_open_provider(&provider);
    printf("tracepoint_open_provider: %d\n", err);

    // A tracepoint can be connected at any time but is typically connected at
    // component initialization.
    // Tracepoint has a name and a list of fieldname-fieldtype pairs.
    tracepoint_state simple_tracepoint = TRACEPOINT_STATE_INIT;
    err = tracepoint_connect(&simple_tracepoint, &provider, "simple_tracepoint u32 field1");
    printf("tracepoint_connect(simple_tracepoint): %d\n", err);

    tracepoint_state empty_tracepoint = TRACEPOINT_STATE_INIT;
    err = tracepoint_connect(&empty_tracepoint, &provider, "empty_tracepoint");
    printf("tracepoint_connect(empty_tracepoint): %d\n", err);

    tracepoint_state tp_data_loc = TRACEPOINT_STATE_INIT;
    err = tracepoint_connect(&tp_data_loc, &provider, "tp_data_loc u32 field1; __data_loc char[] field2; u32 field3;");
    printf("tracepoint_connect(tp_data_loc): %d\n", err);

    tracepoint_state tp_rel_loc = TRACEPOINT_STATE_INIT;
    err = tracepoint_connect(&tp_rel_loc, &provider, "tp_rel_loc u32 field1; __rel_loc char[] field2; u32 field3;");
    printf("tracepoint_connect(tp_rel_loc): %d\n", err);

    printf("\n");
    for (int iteration = 1;; iteration += 1)
    {
        printf("Writing tracepoints:\n");

        // Nothing bad happens if you call tracepoint_write when the tracepoint
        // is not enabled. We check TRACEPOINT_ENABLED as an optimization.
        printf("TRACEPOINT_ENABLED(&simple_tracepoint): %d\n",
            TRACEPOINT_ENABLED(&simple_tracepoint));
        if (TRACEPOINT_ENABLED(&simple_tracepoint))
        {
            struct iovec data_vecs[] = {
                {},                              // write_index will go here
                { &iteration, sizeof(iteration)} // u32 field1
            };
            err = tracepoint_write(&simple_tracepoint, 2, data_vecs);
            printf("tracepoint_write(simple_tracepoint): %d\n", err);
        }

        printf("TRACEPOINT_ENABLED(&empty_tracepoint): %d\n",
            TRACEPOINT_ENABLED(&empty_tracepoint));
        if (TRACEPOINT_ENABLED(&empty_tracepoint))
        {
            struct iovec data_vecs[] = {
                {},                              // write_index will go here
            };
            err = tracepoint_write(&empty_tracepoint, 1, data_vecs);
            printf("tracepoint_write(empty_tracepoint): %d\n", err);
        }

        printf("TRACEPOINT_ENABLED(&tp_data_loc): %d\n",
            TRACEPOINT_ENABLED(&tp_data_loc));
        if (TRACEPOINT_ENABLED(&tp_data_loc))
        {
            unsigned field1 = iteration + 100;
            unsigned field3 = iteration + 300;
            unsigned loc = 0xb0014; // Length 0x000b, abs_offset 0x0014.
            static char loc_data[] = "field2-str";
            struct iovec data_vecs[] = {
                {},                               // write_index will go here
                { &field1, sizeof(field1)},       // 0x08: u32 field1
                { &loc, sizeof(loc)},             // 0x0C: __rel_loc char[] field2
                { &field3, sizeof(field3)},       // 0x10: u32 field3
                { loc_data, sizeof(loc_data)},    // 0x14: char[] str
            };
            err = tracepoint_write(&tp_data_loc, sizeof(data_vecs) / sizeof(data_vecs[0]), data_vecs);
            printf("tracepoint_write(tp_data_loc): %d\n", err);
        }

        printf("TRACEPOINT_ENABLED(&tp_rel_loc): %d\n",
            TRACEPOINT_ENABLED(&tp_rel_loc));
        if (TRACEPOINT_ENABLED(&tp_rel_loc))
        {
            unsigned field1 = iteration + 100;
            unsigned field3 = iteration + 300;
            unsigned loc = 0xb0004; // Length 0x000b, rel_offset 0x0004.
            static char loc_data[] = "field2-str";
            struct iovec data_vecs[] = {
                {},                               // write_index will go here
                { &field1, sizeof(field1)},       // 0x08: u32 field1
                { &loc, sizeof(loc)},             // 0x0C: __rel_loc char[] field2
                { &field3, sizeof(field3)},       // 0x10: u32 field3
                { loc_data, sizeof(loc_data)},    // 0x14: char[] str
            };
            err = tracepoint_write(&tp_rel_loc, sizeof(data_vecs) / sizeof(data_vecs[0]), data_vecs);
            printf("tracepoint_write(tp_rel_loc): %d\n", err);
        }

        printf("Press enter to iterate, x + enter to exit...\n");
        char ch = (char)getchar();
        if (ch == 'x' || ch == 'X')
        {
            break;
        }

        while (ch != '\n')
        {
            ch = (char)getchar();
        }
    }

    // Provider should be closed at component cleanup (before any connected
    // tracepoint_state variables go out of scope).
    tracepoint_close_provider(&provider);

    return 0;
}
