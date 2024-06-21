// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#include <tracepoint/PerfEventInfo.h>
#include <tracepoint/PerfDataFile.h>
#include <tracepoint/PerfEventAbi.h>
#include <eventheader/EventFormatter.h>

#include <string.h>
#include <map>

#ifdef _WIN32
#define strerror_r(errnum, buf, buflen) (strerror_s(buf, buflen, errnum), buf)
#endif // _WIN32

using namespace eventheader_decode;
using namespace tracepoint_decode;

static bool
FlushEvents(std::multimap<uint64_t, std::string>& events, bool comma) noexcept
{
    for (auto const& pair : events)
    {
        fputs(comma ? ",\n " : "\n ", stdout);
        comma = true;
        fputs(pair.second.c_str(), stdout);
    }

    events.clear();
    return comma;
}

int main(int argc, char* argv[])
{
    int err;
    if (argc <= 1)
    {
        fprintf(stderr, "\nUsage: %s [perf.data] ... (use - for stdin)\n", argv[0]);
        err = 1;
        goto Done;
    }

    try
    {
        std::multimap<uint64_t, std::string> events;
        EventFormatter formatter;
        PerfDataFile file;
        bool comma = false;

        for (int argi = 1; argi < argc; argi += 1)
        {
            bool const isStdin =
                0 == strcmp(argv[argi], "") ||
                0 == strcmp(argv[argi], "-") ||
                0 == strcmp(argv[argi], "--");
            char const* const filename = isStdin ? "stdin" : argv[argi];
            fprintf(stdout, "%s\n\"%s\": [",
                comma ? "," : "",
                filename);
            comma = false;

            // CodeQL [SM01937] This is non shipping sample code that is not intended to be secure. Users should be able
            // to specify the output file path.
            err = isStdin ? file.OpenStdin() : file.Open(filename);
            if (err != 0)
            {
                char errBuf[80];
                fprintf(stderr, "\n- Open(\"%s\") error %d: \"%s\"\n",
                    filename,
                    err,
                    strerror_r(err, errBuf, sizeof(errBuf)));
            }
            else for (;;)
            {
                perf_event_header const* pHeader;
                err = file.ReadEvent(&pHeader);
                if (!pHeader)
                {
                    if (err)
                    {
                        fprintf(stderr, "\n- ReadEvent error %d.\n", err);
                    }
                    break;
                }

                if (pHeader->type != PERF_RECORD_SAMPLE)
                {
                    if (pHeader->type == PERF_RECORD_FINISHED_ROUND)
                    {
                        comma = FlushEvents(events, comma);
                    }

                    continue; // Only interested in sample events for now.
                }

                PerfSampleEventInfo sampleEventInfo;
                err = file.GetSampleEventInfo(pHeader, &sampleEventInfo);
                if (err)
                {
                    fprintf(stderr, "\n- GetSampleEventInfo error %d.\n", err);
                    continue;
                }

                // Events are returned out-of-order and need to be sorted. Use a map to
                // put them into timestamp order. Flush the map at the end of each round.
                auto it = events.emplace(
                    (sampleEventInfo.SampleType() & PERF_SAMPLE_TIME) ? sampleEventInfo.time : 0u,
                    std::string());
                err = formatter.AppendSampleAsJson(
                    it->second,
                    sampleEventInfo,
                    file.FileBigEndian(),
                    static_cast<EventFormatterJsonFlags>(
                        EventFormatterJsonFlags_Space |
                        EventFormatterJsonFlags_FieldTag));
                if (err)
                {
                    fprintf(stderr, "\n- Format error %d.\n", err);
                }
            }

            comma = FlushEvents(events, comma);

            fputs(" ]", stdout);
            comma = true;
        }

        fprintf(stdout, "\n");
        err = 0;
    }
    catch (std::exception const& ex)
    {
        fprintf(stderr, "\nException: %s\n", ex.what());
        err = 1;
    }

Done:

    return err;
}
