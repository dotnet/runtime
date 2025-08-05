// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

/*
Sample tool that demonstrates reading a perf.data file with PerfDataFile and
writing a perf.data file with PerfDataFileWriter.

This is not intended to be useful (except perhaps for testing purposes). It is
intended to show how PerfDataFile can be used to take the perf.data file
apart and how PerfDataFileWriter can put it back together.

Note that the output file is not expected to be exactly the same as the input:

- Output is always a normal-mode file even if input was a pipe-mode file.
- Output file may store headers in a different order.
- Output file may use more/less padding.
- If the input file is semantically inconsistent, the output file may not
  precisely match the input (the inconsistent data might be lost). For
  example, there are usually two (or more) copies of each attr, one in a
  v1 format and another in a v2 format. The rewrite process will typically
  ignore the v1 copy of the data if a v2 copy is available, so if the v1 copy
  is semantically different from the v2 copy, that detail might be lost during
  rewrite.
*/

#include <tracepoint/PerfDataFile.h>
#include <tracepoint/PerfDataFileWriter.h>
#include <tracepoint/PerfEventAbi.h>
#include <tracepoint/PerfEventInfo.h>
#include <tracepoint/PerfEventMetadata.h>

#include <string>
#include <unordered_set>
#include <vector>

#ifdef _WIN32
#define strerror_r(errnum, buf, buflen) (strerror_s(buf, buflen, errnum), buf)
#define UNLINK(filename) _unlink(filename)
#else
#include <unistd.h>
#define UNLINK(filename) unlink(filename)
#endif // _WIN32

using namespace tracepoint_decode;

static void
WriteErrorMessage(char const* filename, int error, char const* context)
{
    if (error == ENOMEM)
    {
        throw std::bad_alloc();
    }

    char errorBuf[80];
    fprintf(stderr, "%s: error %u : %s (%s).\n",
        filename,
        error,
        context,
        strerror_r(error, errorBuf, sizeof(errorBuf)));
}

static void
WriteWarningMessage(char const* filename, int error, char const* context)
{
    if (error == ENOMEM)
    {
        throw std::bad_alloc();
    }

    char errorBuf[80];
    fprintf(stderr, "%s: warning %u : %s (%s).\n",
        filename,
        error,
        context,
        strerror_r(error, errorBuf, sizeof(errorBuf)));
}

static void
MergeEventDesc(
    PerfDataFileWriter& output,
    char const* outputPath,
    std::unordered_set<uint64_t>& sampleIdsUsed,
    std::vector<uint64_t>& sampleIdsBuffer,
    PerfEventDesc const& desc)
{
    sampleIdsBuffer.clear();
    for (uint32_t iId = 0; iId != desc.ids_count; iId += 1)
    {
        auto const id = desc.ids[iId];
        if (sampleIdsUsed.insert(desc.ids[iId]).second)
        {
            sampleIdsBuffer.push_back(id);
        }
    }

    if (!sampleIdsBuffer.empty())
    {
        PerfEventDesc newDesc = desc;
        newDesc.ids = sampleIdsBuffer.data();
        newDesc.ids_count = static_cast<uint32_t>(sampleIdsBuffer.size());
        auto err = output.AddEventDesc(newDesc);
        if (err != 0)
        {
            WriteWarningMessage(outputPath, err, "output.AddEventDesc failed, metadata incomplete");
        }
    }
}

int
main(int argc, char* argv[])
{
    bool mainSuccessful = false;

    if (argc <= 1)
    {
        fprintf(stderr, "\nUsage: %s [perf.data] ... (will generate *.rewrite)\n",
            argv[0]);
    }
    else try
    {
        PerfDataFile input;
        PerfDataFileWriter output;
        std::string outputPathBuffer;
        std::vector<uint64_t> sampleIdsBuffer;
        std::unordered_set<uint64_t> sampleIdsUsed;

        for (int argi = 1; argi < argc; argi += 1)
        {
            int err;
            auto const inputPath = argv[argi];
            outputPathBuffer = inputPath;
            outputPathBuffer += ".rewrite";
            auto const outputPath = outputPathBuffer.c_str();

            // CodeQL [SM01937] This is a sample/tool. Using externally-supplied path is intended behavior.
            err = input.Open(inputPath);
            if (err != 0)
            {
                WriteErrorMessage(inputPath, err, "input.Open failed, skipping file");
                continue;
            }

            if (input.ByteReader().ByteSwapNeeded())
            {
                // PerfDataFileWriter only supports creating host-endian files, so we can't
                // easily rewrite a byte-swapped input file.
                err = ENOTSUP;
                WriteErrorMessage(inputPath, err, "input is byte-swapped, skipping file");
                continue;
            }

            // CodeQL [SM01937] This is a sample/tool. Using externally-supplied path is intended behavior.
            err = output.Create(outputPath);
            if (err != 0)
            {
                WriteErrorMessage(outputPath, err, "output.Create failed, skipping file");
                continue;
            }

            sampleIdsUsed.clear();
            for (;;)
            {
                perf_event_header const* pHeader;
                err = input.ReadEvent(&pHeader);
                if (!pHeader)
                {
                    if (err != 0)
                    {
                        WriteWarningMessage(inputPath, err, "input.Read failed, ignoring rest of input");
                    }
                    break;
                }

                uint32_t eventDataSize;
                switch (pHeader->type)
                {
                default:
                    eventDataSize = pHeader->size;
                    break;

                case PERF_RECORD_AUXTRACE:
                    // Special-case. Event content size != pHeader->size.
                    eventDataSize = input.EventDataSize(pHeader);
                    break;

                case PERF_RECORD_HEADER_ATTR:
                    // Pseudo-event, conflicts with AddEventDesc.
                    // PerfDataFile automatically merges data from this event into its own
                    // EventDesc table. We'll use AddEventDesc to generate the output file's
                    // attr headers based on the merged EventDesc table.
                    continue;

                case PERF_RECORD_HEADER_EVENT_TYPE:
                    // Pseudo-event, conflicts with AddEventDesc.
                    // PerfDataFile could automatically merge data from this event into its
                    // own EventDesc table, but that is not implemented because this event
                    // type is deprecated. Instead, we'll just ignore this event type.
                    continue;

                case PERF_RECORD_HEADER_TRACING_DATA:
                    // Pseudo-event, conflicts with SetTracingData.
                    // PerfDataFile automatically merges data from this event into its own
                    // metadata table. We'll use SetTracingData to generate the output file's
                    // metadata based on the metadata referenced by the input file's events.
                    continue;

                case PERF_RECORD_HEADER_BUILD_ID:
                case PERF_RECORD_HEADER_FEATURE:
                    // Pseudo-events, conflict with SetHeader.
                    // PerfDataFile automatically merges data from these events into its own
                    // header table. We'll use SetHeader to generate the output file's headers
                    // based on the merged header table.
                    continue;
                }

                if (pHeader->type == PERF_RECORD_SAMPLE)
                {
                    // Populate the output file's metadata from the event's metadata.
                    PerfSampleEventInfo info;
                    err = input.GetSampleEventInfo(pHeader, &info);
                    if (err != 0)
                    {
                        WriteWarningMessage(inputPath, err, "input.GetSampleEventInfo failed, metadata may be incomplete");
                    }
                    else if (info.event_desc->metadata)
                    {
                        auto const& desc = *info.event_desc;
                        err = output.AddTracepointEventDesc(desc);
                        if (err == 0)
                        {
                            // We don't need to AddEventDesc for the IDs covered by this event_desc.
                            for (auto i = 0u; i != desc.ids_count; i += 1)
                            {
                                sampleIdsUsed.insert(desc.ids[i]);
                            }
                        }
                        else if (err == EEXIST)
                        {
                            // Already added metadata for this event.
                        }
                        else
                        {
                            WriteWarningMessage(outputPath, err, "output.AddTracepointEventDesc failed, metadata may be incomplete");
                        }
                    }
                }

                err = output.WriteEventData(pHeader, eventDataSize);
                if (err != 0)
                {
                    WriteErrorMessage(outputPath, err, "output.Write failed");
                    goto CloseAndUnlinkOutput;
                }
            }

            // Populate the output file's EventDesc table from the input file's table.
            // Some of this was already done by AddTracepointEventDesc.
            // In addition, the input file's table usually has duplicate entries - one entry with
            // names and one entry without names. Therefore, MergeEventDesc will skip ids that are
            // already populated, and we merge all descriptors with names before merging any
            // descriptors that don't have names.
            for (size_t iDesc = 0; iDesc != input.EventDescCount(); iDesc += 1)
            {
                // First, merge data from descriptors that have names.
                auto const& desc = input.EventDesc(iDesc);
                if (desc.name[0] != '\0')
                {
                    MergeEventDesc(output, outputPath, sampleIdsUsed, sampleIdsBuffer, desc);
                }
            }
            for (size_t iDesc = 0; iDesc != input.EventDescCount(); iDesc += 1)
            {
                // Second, fill gaps (if any) using descriptors that don't have names.
                auto const& desc = input.EventDesc(iDesc);
                if (desc.name[0] == '\0')
                {
                    MergeEventDesc(output, outputPath, sampleIdsUsed, sampleIdsBuffer, desc);
                }
            }

            // Populate the output file's headers.
            for (auto i = PERF_HEADER_FIRST_FEATURE; i != PERF_HEADER_LAST_FEATURE; i = static_cast<PerfHeaderIndex>(i + 1))
            {
                switch (i)
                {
                case PERF_HEADER_TRACING_DATA:
                case PERF_HEADER_EVENT_DESC:
                    // Let the output file auto-populate these based on AddEventDesc and AddTracingData.
                    continue;
                default:
                    break;
                }

                auto header = input.Header(i);
                if (!header.empty())
                {
                    // Copy the input file's merged header into the output file.
                    err = output.SetHeader(i, header.data(), static_cast<unsigned>(header.size()));
                    if (err != 0)
                    {
                        WriteErrorMessage(outputPath, err, "output.SetHeader failed, closing");
                        goto CloseAndUnlinkOutput;
                    }
                }
            }

            err = output.SetTracingData(
                input.TracingDataLongSize(),
                input.TracingDataPageSize(),
                input.TracingDataHeaderPage(),
                input.TracingDataHeaderEvent(),
                input.TracingDataFtraces(),
                input.TracingDataFtraceCount(),
                input.TracingDataKallsyms(),
                input.TracingDataPrintk(),
                input.TracingDataSavedCmdLine());
            if (err != 0)
            {
                WriteErrorMessage(outputPath, err, "output.SetTracingData failed");
                goto CloseAndUnlinkOutput;
            }

            err = output.FinalizeAndClose();
            if (err != 0)
            {
                WriteErrorMessage(outputPath, err, "output.FinalizeAndClose failed");
                goto CloseAndUnlinkOutput;
            }

            fprintf(stdout, "\"%s\" --> \"%s\"\n", inputPath, outputPath);
            mainSuccessful = true; // One or more files completed.
            continue;

        CloseAndUnlinkOutput:

            output.CloseNoFinalize();
            UNLINK(outputPath);
        }
    }
    catch (std::exception const& ex)
    {
        fprintf(stderr, "\nException: %s\n", ex.what());
        mainSuccessful = false;
    }

    return mainSuccessful ? 0 : 1;
}
