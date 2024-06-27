// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

/*
Demonstrates the use of SavePerfDataFile to save session data to a perf file.
*/

#include <tracepoint/TracepointSession.h>
#include <stdio.h>
#include <string.h>
#include <errno.h>
#include <time.h>

#include <unistd.h>

using namespace std::string_view_literals;
using namespace tracepoint_control;
using namespace tracepoint_decode;

int
main(int argc, char* argv[])
{
    int error = 0;
    TracepointTimestampRange writtenRange;

    if (argc < 3 ||
        (0 != strcmp(argv[1], "0") && 0 != strcmp(argv[1], "1")))
    {
        fprintf(stderr,
            "Usage: save-session [0|1] outFileName systemName:eventName ...\n"
            "0 = circular, 1 = realtime\n");
        return 1;
    }

    auto const mode = 0 == strcmp(argv[1], "0")
        ? TracepointSessionMode::Circular
        : TracepointSessionMode::RealTime;

    TracepointCache cache;
    TracepointSession session(cache, mode, 0); // 0 should round up to a 1-page buffer.

    fprintf(stderr, "Session: BC=%u BS=%x RT=%u MODE=%u\n",
        session.BufferCount(), session.BufferSize(), session.IsRealtime(), (unsigned)session.Mode());

    fprintf(stderr, "\n");

    for (int argi = 3; argi < argc; argi += 1)
    {
        TracepointName name(argv[argi]);
        error = cache.AddFromSystem(name);
        if (error != ENOENT || name.SystemName != UserEventsSystemName ||
            !name.IsValidEventHeader())
        {
            fprintf(stderr, "AddFromSystem(%s) = %u\n", argv[argi], error);
        }
        else
        {
            // User-specified EventHeader event is not registered.
            // Pre-register it and try to collect it anyway.
            error = cache.PreregisterEventHeaderTracepoint(name);
            fprintf(stderr, "PreregisterEventHeaderTracepoint(%s) = %u\n", argv[argi], error);
        }
    }

    fprintf(stderr, "\n");

    unsigned enabled = 0;
    for (int argi = 3; argi < argc; argi += 1)
    {
        error = session.EnableTracepoint(TracepointName(argv[argi]));
        fprintf(stderr, "EnableTracepoint(%s) = %u\n", argv[argi], error);
        enabled += error == 0;
    }

    if (enabled == 0)
    {
        return error;
    }

    for (unsigned i = 0;; i += 1)
    {
        printf("\nPress enter to iterate, x + enter to exit...\n");
        char ch = (char)getchar();
        if (ch == 'x' || ch == 'X')
        {
            break;
        }

        while (ch != '\n')
        {
            ch = (char)getchar();
        }

        char outFileName[256];
        snprintf(outFileName, sizeof(outFileName), "%s.%u", argv[2], i);

        // CodeQL [SM01937] This is a sample/tool. Using externally-supplied path is intended behavior.
        error = session.SavePerfDataFile(
            outFileName,
            TracepointSavePerfDataFileOptions()
            .TimestampFilter(writtenRange.Last) // For circular, filter out old events.
            .TimestampWrittenRange(&writtenRange));
        printf("SavePerfDataFile(%s) = %u\n", outFileName, error);
    }

    return 0;
}
