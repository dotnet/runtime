// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#include <eventheader/EventFormatter.h>

extern "C" {
#include <tracefs/tracefs.h>
}

#include <errno.h>
#include <stdio.h>

struct CallbackContext
{
    std::string Value;
    EventEnumerator Enumerator;
    EventFormatter Formatter;
};

static int
IterateCallback(tep_event* ev, tep_record* rec, int cpu, void* pContext)
{
    auto& ctx = *static_cast<CallbackContext*>(pContext);
    if (ev->format.nr_fields < 1 ||
        0 != strcmp(ev->format.fields[0].name, "eventheader_flags"))
    {
        fprintf(stdout, "- No eventheader_flags: \"%s\"\n", ev->name);
    }
    else
    {
        auto const flagsOffset = ev->format.fields[0].offset;
        auto const pData = static_cast<uint8_t const*>(rec->data) + flagsOffset;
        auto const cData = rec->size - flagsOffset;

        if (!ctx.Enumerator.StartEvent(ev->name, pData, cData))
        {
            fprintf(stdout, "- StartEvent error %u: \"%s\"\n", ctx.Enumerator.LastError(), ev->name);
        }
        else
        {
            ctx.Value.clear();
            int err = ctx.Formatter.AppendEventAsJsonAndMoveToEnd(ctx.Value, ctx.Enumerator);
            if (err != 0)
            {
                fprintf(stdout, "- AppendEvent error %u: \"%s\"\n", err, ev->name);
            }
            else
            {
                fprintf(stdout, "%s,\n", ctx.Value.c_str());
            }
        }
    }

    return 0;
}

int main()
{
    int err;
    CallbackContext callbackContext;
    auto tep = tracefs_local_events(nullptr);
    if (!tep)
    {
        err = errno;
        char errBuf[80];
        strerror_r(err, errBuf, sizeof(errBuf));
        printf("tracefs_local_events: errno=%u %s\n", err, errBuf);
        goto Done;
    }

    int parsingFailures;
    if (tracefs_fill_local_events(nullptr, tep, &parsingFailures))
    {
        err = errno;
        char errBuf[80];
        strerror_r(err, errBuf, sizeof(errBuf));
        printf("tracefs_fill_local_events: errno=%u %s\n", err, errBuf);
        goto Done;
    }

    printf("tracefs_fill_local_events parsing failures: %u\n", parsingFailures);

    if (tracefs_iterate_raw_events(tep, nullptr, nullptr, 0, &IterateCallback, &callbackContext))
    {
        err = errno;
        char errBuf[80];
        strerror_r(err, errBuf, sizeof(errBuf));
        printf("tracefs_iterate_raw_events: errno=%u %s\n", err, errBuf);
        goto Done;
    }

    printf("Done.\n");

Done:

    if (tep)
    {
        tep_free(tep);
    }

    return err;
}
