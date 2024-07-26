// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

/*
Tests that any implementation of the tracepoint interface should pass.
Does not check whether the events ever get enabled or go anywhere.
*/

#include <tracepoint/tracepoint.h>
#include <stdio.h>
#include <stdarg.h>

static bool s_any_errors = false;

static void
check_errno(unsigned line, int err, char const* op)
{
    if (err != 0)
    {
        s_any_errors = true;
        fprintf(stderr, "tracepoint-utest.cpp(%u) : warning : errno %d from %s\n",
            line, err, op);
    }
}

#define CHECK(op) check_errno(__LINE__, (op), #op)

static void
verify_cond(unsigned line, bool condition, char const* format, ...)
{
    if (!condition)
    {
        s_any_errors = true;

        va_list args;
        va_start(args, format);
        fprintf(stderr, "tracepoint-utest.cpp(%u) : error : ", line);
        vfprintf(stderr, format, args);
        fprintf(stderr, "\n");
        va_end(args);
    }
}

static void
verify_provider(unsigned line, tracepoint_provider_state const& p, bool providerIsOpen)
{
    if (!providerIsOpen)
    {
        verify_cond(line, -1 == p.data_file,
            "Closed provider data_file: expected -1, actual %d", p.data_file);
    }
}

static void
verify_tp_disconnected(unsigned line, tracepoint_state const& e)
{
    iovec emptyVec = {};
    tracepoint_write(&e, 1, &emptyVec);

    verify_cond(line, 0 == e.status_word,
        "Disconnected event status_word: expected 0, actual %u", e.status_word);
    verify_cond(line, -1 == e.write_index,
        "Disconnected event write_index: expected -1, actual %d", e.write_index);
    verify_cond(line, nullptr == e.provider_state,
        "Disconnected event provider_state: expected NULL, actual %p", e.provider_state);
    verify_cond(line, nullptr == e.tracepoint_list_link.next,
        "Disconnected event next: expected NULL, actual %p", e.tracepoint_list_link.next);
    verify_cond(line, nullptr == e.tracepoint_list_link.prev,
        "Disconnected event prev: expected NULL, actual %p", e.tracepoint_list_link.prev);
}

static void
verify_tp_closed(unsigned line, tracepoint_state const& e, tracepoint_provider_state const& p)
{
    iovec emptyVec = {};
    tracepoint_write(&e, 1, &emptyVec);

    verify_cond(line, &p == e.provider_state,
        "Closed event provider_state: expected %p, actual %p", &p, e.provider_state);
    auto enabled = TRACEPOINT_ENABLED(&e);
    verify_cond(line, 0 == enabled,
        "Closed event TRACEPOINT_ENABLED: expected 0, actual %u", enabled);
}

static void
verify_tp_open(unsigned line, tracepoint_state const& e, tracepoint_provider_state const& p)
{
    iovec emptyVec = {};
    tracepoint_write(&e, 1, &emptyVec);

    verify_cond(line, &p == e.provider_state,
        "Open event provider_state: expected %p, actual %p", &p, e.provider_state);
    (void)TRACEPOINT_ENABLED(&e); // Verify dereferencable.
}

static void
connect_and_verify(unsigned line, tracepoint_provider_state& p, bool providerIsOpen)
{
    tracepoint_state e0 = TRACEPOINT_STATE_INIT;
    tracepoint_state e1 = TRACEPOINT_STATE_INIT;

    verify_provider(line, p, providerIsOpen);
    verify_tp_disconnected(__LINE__, e0);
    verify_tp_disconnected(__LINE__, e1);

    CHECK(tracepoint_connect(&e0, &p, "e0 "));
    CHECK(tracepoint_connect(&e1, &p, "e1 "));

    verify_provider(line, p, providerIsOpen);
    if (!providerIsOpen)
    {
        verify_tp_closed(line, e0, p);
        verify_tp_closed(line, e1, p);
    }
    else
    {
        verify_tp_open(line, e0, p);
        verify_tp_open(line, e1, p);
    }

    tracepoint_close_provider(&p);

    verify_provider(line, p, false);
    verify_tp_disconnected(line, e0);
    verify_tp_disconnected(line, e1);

    CHECK(tracepoint_connect(&e0, nullptr, "e0 "));
    CHECK(tracepoint_connect(&e1, nullptr, "e1 "));

    verify_provider(line, p, false);
    verify_tp_disconnected(line, e0);
    verify_tp_disconnected(line, e1);

    CHECK(tracepoint_connect(&e0, &p, "e0 "));
    CHECK(tracepoint_connect(&e1, &p, "e1 "));

    verify_provider(line, p, false);
    verify_tp_closed(line, e0, p);
    verify_tp_closed(line, e1, p);

    CHECK(tracepoint_open_provider(&p));

    verify_provider(line, p, true);
    verify_tp_disconnected(line, e0);
    verify_tp_disconnected(line, e1);

    CHECK(tracepoint_connect(&e0, &p, "e0 "));
    CHECK(tracepoint_connect(&e1, &p, "e1 "));

    verify_provider(line, p, true);
    verify_tp_open(line, e0, p);
    verify_tp_open(line, e1, p);

    tracepoint_close_provider(&p);

    verify_provider(line, p, false);
    verify_tp_disconnected(line, e0);
    verify_tp_disconnected(line, e1);
}

int main()
{
    tracepoint_provider_state p0 = TRACEPOINT_PROVIDER_STATE_INIT;
    tracepoint_provider_state p1 = TRACEPOINT_PROVIDER_STATE_INIT;
    tracepoint_provider_state p2 = TRACEPOINT_PROVIDER_STATE_INIT;

    // Verify fresh provider (sanity check)
    verify_provider(__LINE__, p0, false);

    // Verify fresh-closed provider
    tracepoint_close_provider(&p1);
    verify_provider(__LINE__, p1, false);

    // Verify open-closed provider
    CHECK(tracepoint_open_provider(&p2));
    verify_provider(__LINE__, p2, true);
    tracepoint_close_provider(&p2);
    verify_provider(__LINE__, p2, false);

    // Connect events to fresh provider.
    connect_and_verify(__LINE__, p0, false);

    // Connect events to closed provider.
    connect_and_verify(__LINE__, p1, false);

    // Connect events to open provider.
    CHECK(tracepoint_open_provider(&p2));
    connect_and_verify(__LINE__, p2, true);

    tracepoint_state e0 = TRACEPOINT_STATE_INIT;
    tracepoint_state e1 = TRACEPOINT_STATE_INIT;

    // Disconnected --> Disconnected
    CHECK(tracepoint_connect(&e0, nullptr, "e0 "));
    verify_tp_disconnected(__LINE__, e0);

    // Disconnected --> p0
    CHECK(tracepoint_connect(&e0, &p0, "e0 "));
    verify_provider(__LINE__, p0, false);
    verify_tp_closed(__LINE__, e0, p0);
    CHECK(tracepoint_connect(&e1, &p0, "e1 "));
    verify_provider(__LINE__, p0, false);
    verify_tp_closed(__LINE__, e0, p0);
    verify_tp_closed(__LINE__, e1, p0);

    // p0 --> p0
    CHECK(tracepoint_connect(&e0, &p0, "e0 "));
    verify_provider(__LINE__, p0, false);
    verify_tp_closed(__LINE__, e0, p0);
    verify_tp_closed(__LINE__, e1, p0);
    CHECK(tracepoint_connect(&e1, &p0, "e1 "));
    verify_provider(__LINE__, p0, false);
    verify_tp_closed(__LINE__, e0, p0);
    verify_tp_closed(__LINE__, e1, p0);

    // p0 --> p1
    CHECK(tracepoint_connect(&e0, &p1, "e0 "));
    verify_provider(__LINE__, p0, false);
    verify_provider(__LINE__, p1, false);
    verify_tp_closed(__LINE__, e0, p1);
    verify_tp_closed(__LINE__, e1, p0);
    CHECK(tracepoint_connect(&e1, &p1, "e1 "));
    verify_provider(__LINE__, p0, false);
    verify_provider(__LINE__, p1, false);
    verify_tp_closed(__LINE__, e0, p1);
    verify_tp_closed(__LINE__, e1, p1);

    // p1 --> p0
    CHECK(tracepoint_connect(&e0, &p0, "e0 "));
    verify_provider(__LINE__, p0, false);
    verify_provider(__LINE__, p1, false);
    verify_tp_closed(__LINE__, e0, p0);
    verify_tp_closed(__LINE__, e1, p1);
    CHECK(tracepoint_connect(&e1, &p0, "e1 "));
    verify_provider(__LINE__, p0, false);
    verify_provider(__LINE__, p1, false);
    verify_tp_closed(__LINE__, e0, p0);
    verify_tp_closed(__LINE__, e1, p0);

    // p0 --> Disconnected
    CHECK(tracepoint_connect(&e0, nullptr, "e0 "));
    verify_provider(__LINE__, p0, false);
    verify_tp_disconnected(__LINE__, e0);
    verify_tp_closed(__LINE__, e1, p0);
    CHECK(tracepoint_connect(&e1, nullptr, "e1 "));
    verify_provider(__LINE__, p0, false);
    verify_tp_disconnected(__LINE__, e0);
    verify_tp_disconnected(__LINE__, e1);

    fprintf(stderr, "%s\n", s_any_errors ? "FAIL" : "OK");
    return s_any_errors;
}
