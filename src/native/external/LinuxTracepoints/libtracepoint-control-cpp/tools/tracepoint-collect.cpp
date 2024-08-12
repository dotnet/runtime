// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

/*
Simple tool for collecting tracepoints into perf.data files.
*/

#include <tracepoint/TracepointSession.h>
#include <tracepoint/TracepointSpec.h>
#include <tracepoint/PerfDataFileWriter.h>
#include <assert.h>
#include <stdarg.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <errno.h>
#include <unistd.h>

#include <vector>

#define PROGRAM_NAME "tracepoint-collect"
#define EXIT_SIGNALS      SIGTERM, SIGINT
#define EXIT_SIGNALS_STR "SIGTERM or SIGINT"

static constexpr int ExitSigs[] = { EXIT_SIGNALS };
static constexpr unsigned ExitSigsCount = sizeof(ExitSigs) / sizeof(ExitSigs[0]);

static char const* const UsageCommon = R"(
Usage: )" PROGRAM_NAME R"( [options...] TracepointSpecs...
)";

// Usage error: stderr += UsageCommon + UsageShort.
static char const* const UsageShort = R"(
Try ")" PROGRAM_NAME R"( --help" for more information.
)";

// -h or --help: stdout += UsageCommon + UsageLong.
static char const* const UsageLong = R"(
Collects tracepoint events and saves them to a perf.data file. Collection
runs until )" EXIT_SIGNALS_STR R"( is received.

Requires privileges, typically the CAP_PERFMON capability plus read access to
/sys/kernel/tracing. Pre-registration of a tracepoint requires write access to
/sys/kernel/tracing/user_events_data.

Options:

-b, --buffersize <size>
                    Set the size of each buffer, in kilobytes. There will be
                    one buffer per CPU. The default is 128, max is 2GB.

-c, --circular      Use circular trace mode. Events will be collected in
                    circular buffers (new events overwrite old) until the
                    signal is received, at which point the output file will be
                    created, buffer contents will be written to the file, and
                    the tool will exit.

-C, --realtime      Use realtime trace mode (default). File will be created
                    immediately and events will be written to the file as they
                    are received until the signal is received, at which point
                    the tool will finalize the file and exit.

-i, --input <file>  Read additional TracepointSpecs from <file>. Each line in
                    the file is treated as a TracepointSpec. Empty lines and
                    lines starting with '#' are ignored.

-o, --output <file> Set the output filename. The default is "./perf.data".

-w, --wakeup <size> Set the wakeup watermark size for realtime trace mode, in
                    kilobytes. The default is 2. The tool will wait for a
                    buffer to have at least this much data before waking to
                    flush the buffer to the output file.

-v, --verbose       Show diagnostic output.

-h, --help          Show this help message and exit.

A TracepointSpec is one of the following:

* If the tracepoint is a normal (non-EventHeader) user_event that may not
  already exist, use the full user_event definition,
  "SystemName:EventName Fields...". If the tracepoint does not already exist,
  it will be pre-registered so that it can be added to the trace session. For
  example:

      user_events:MyEvent u32 MyField1; struct MyStruct2 MyField2 20

  " Fields..." is required. For an event with no fields, use " ;", e.g.

      user_events:MySimpleEvent ;

  See https://docs.kernel.org/trace/user_events.html#command-format for
  details on the user_events definition syntax.

* If the tracepoint is an EventHeader user_event that may not already exist,
  use the EventHeader identity, "SystemName:ProviderName_Suffix". If the
  tracepoint does not already exist, it will be pre-registered so that it can
  be added to the trace session. For example:

      user_events:MyProvider_L5K1

* If the tracepoint is known to already be registered (e.g. a kernel event),
  use the tracepoint identity with a leading colon, ":SystemName:EventName".
  If the tracepoint does not already exist, it will not be added to the trace
  session. For example:

      :sched:sched_switch

In all cases, you may omit "SystemName:" if it is "user_events:", e.g.

    MyEvent u32 MyField1;
    MyProvider_L5K1Gmygroup
    :MyUserEventThatAlreadyExists

For TracepointSpecs provided on the command line, use quotation marks to
ensure correct handling of spaces and semicolons in each TracepointSpec, e.g.

    "user_events:MyEvent u32 MyField1; struct MyStruct2 MyField2 20"
)";

using namespace std::string_view_literals;
using namespace tracepoint_control;
using namespace tracepoint_decode;

class Tracepoint
{
    std::vector<char> storage;

public:

    TracepointSpec spec;

    Tracepoint(Tracepoint&&) = default;
    Tracepoint& operator=(Tracepoint&&) = default;

    explicit
    Tracepoint(std::string_view line)
    {
        auto const trimmedLine = TracepointSpec::Trim(line);

        if (!trimmedLine.empty())
        {
            storage.assign(trimmedLine.begin(), trimmedLine.end());
        }

        spec = TracepointSpec({ storage.data(), storage.size() });
    }
};

struct Options
{
    char const* output = "./perf.data";
    bool verbose = false;
};

// fprintf(stderr, "PROGRAM_NAME: " + format, args...).
static void
PrintStderr(const char* format, ...)
{
    va_list args;
    va_start(args, format);
    fputs(PROGRAM_NAME ": ", stderr);
    vfprintf(stderr, format, args);
    va_end(args);
}

// if (condition) fprintf(stderr, "PROGRAM_NAME: " + format, args...).
static void
PrintStderrIf(bool condition, const char* format, ...)
{
    if (condition)
    {
        va_list args;
        va_start(args, format);
        fputs(PROGRAM_NAME ": ", stderr);
        vfprintf(stderr, format, args);
        va_end(args);
    }
}

static void
PushFrontDef(Options const& o, std::vector<Tracepoint>& tracepoints, Tracepoint&& tp)
{
    auto const& spec = tp.spec;
    auto const trimmed = spec.Trimmed;
    switch (tp.spec.Kind)
    {
    case TracepointSpecKind::Empty:
        break;
    case TracepointSpecKind::Identifier:
        PrintStderrIf(o.verbose, "verbose: identifier \"%.*s:%.*s\".\n",
            (unsigned)spec.SystemName.size(), spec.SystemName.data(),
            (unsigned)spec.EventName.size(), spec.EventName.data());
        tracepoints.push_back(std::move(tp));
        break;
    case TracepointSpecKind::Definition:
        if (spec.SystemName != UserEventsSystemName)
        {
            PrintStderr("error: definition system name \"%.*s\" must be 'user_events' (from \"%.*s\").\n",
                (unsigned)spec.SystemName.size(), spec.SystemName.data(),
                (unsigned)trimmed.size(), trimmed.data());
        }
        else
        {
            PrintStderrIf(o.verbose, "verbose: definition \"%.*s:%.*s%s%.*s%s%.*s\"\n",
                (unsigned)spec.SystemName.size(), spec.SystemName.data(),
                (unsigned)spec.EventName.size(), spec.EventName.data(),
                spec.Flags.empty() ? "" : ":",
                (unsigned)spec.Flags.size(), spec.Flags.data(),
                spec.Fields.empty() ? "" : " ",
                (unsigned)spec.Fields.size(), spec.Fields.data());
            tracepoints.push_back(std::move(tp));
        }
        break;
    case TracepointSpecKind::EventHeaderDefinition:
        if (spec.SystemName != UserEventsSystemName)
        {
            PrintStderr("error: eventheader system name \"%.*s\" must be 'user_events' (from \"%.*s\").\n",
                (unsigned)spec.SystemName.size(), spec.SystemName.data(),
                (unsigned)trimmed.size(), trimmed.data());
        }
        else
        {
            PrintStderrIf(o.verbose, "verbose: eventheader \"%.*s:%.*s%s%.*s\".\n",
                (unsigned)spec.SystemName.size(), spec.SystemName.data(),
                (unsigned)spec.EventName.size(), spec.EventName.data(),
                spec.Flags.empty() ? "" : ":",
                (unsigned)spec.Flags.size(), spec.Flags.data());
            tracepoints.push_back(std::move(tp));
        }
        break;
    case TracepointSpecKind::ErrorIdentifierCannotHaveFields:
        PrintStderr("error: identifier cannot have fields (from \"%.*s\").\n",
            (unsigned)trimmed.size(), trimmed.data());
        break;
    case TracepointSpecKind::ErrorIdentifierCannotHaveFlags:
        PrintStderr("error: identifier cannot have flags (from \"%.*s\").\n",
            (unsigned)trimmed.size(), trimmed.data());
        break;
    case TracepointSpecKind::ErrorDefinitionCannotHaveColonAfterFlags:
        PrintStderr("error: definition cannot have colon after flags (from \"%.*s\").\n",
            (unsigned)trimmed.size(), trimmed.data());
        break;
    case TracepointSpecKind::ErrorIdentifierEventNameEmpty:
        PrintStderr("error: identifier event name is empty (from \"%.*s\").\n",
            (unsigned)trimmed.size(), trimmed.data());
        break;
    case TracepointSpecKind::ErrorDefinitionEventNameEmpty:
        PrintStderr("error: definition event name is empty (from \"%.*s\").\n",
            (unsigned)trimmed.size(), trimmed.data());
        break;
    case TracepointSpecKind::ErrorIdentifierEventNameInvalid:
        PrintStderr("error: identifier event name \"%.*s\" is invalid (from \"%.*s\").\n",
            (unsigned)spec.EventName.size(), spec.EventName.data(),
            (unsigned)trimmed.size(), trimmed.data());
        break;
    case TracepointSpecKind::ErrorDefinitionEventNameInvalid:
        PrintStderr("error: definition event name \"%.*s\" is invalid (from \"%.*s\").\n",
            (unsigned)spec.EventName.size(), spec.EventName.data(),
            (unsigned)trimmed.size(), trimmed.data());
        break;
    case TracepointSpecKind::ErrorEventHeaderDefinitionEventNameInvalid:
        PrintStderr("error: eventheader event name \"%.*s\" is invalid (from \"%.*s\").\n",
            (unsigned)spec.EventName.size(), spec.EventName.data(),
            (unsigned)trimmed.size(), trimmed.data());
        PrintStderr("(error) If this was meant to be the name of an existing non-eventheader event, add a leading ':'.\n");
        PrintStderr("(error) If this was meant to be the definition of a non-eventheader event, a Fields... section must be provided.\n");
        PrintStderr("(error) If a non-eventheader event has no fields, use \" ;\" for Fields..., e.g. \"MyEvent ;\".\n");
        break;
    case TracepointSpecKind::ErrorIdentifierSystemNameEmpty:
        PrintStderr("error: identifier system name is empty (from \"%.*s\").\n",
            (unsigned)trimmed.size(), trimmed.data());
        break;
    case TracepointSpecKind::ErrorDefinitionSystemNameEmpty:
        PrintStderr("error: definition system name is empty (from \"%.*s\").\n",
            (unsigned)trimmed.size(), trimmed.data());
        break;
    case TracepointSpecKind::ErrorIdentifierSystemNameInvalid:
        PrintStderr("error: identifier system name \"%.*s\" is invalid (from \"%.*s\").\n",
            (unsigned)spec.SystemName.size(), spec.SystemName.data(),
            (unsigned)trimmed.size(), trimmed.data());
        break;
    case TracepointSpecKind::ErrorDefinitionSystemNameInvalid:
        PrintStderr("error: definition system name \"%.*s\" is invalid (from \"%.*s\").\n",
            (unsigned)spec.SystemName.size(), spec.SystemName.data(),
            (unsigned)trimmed.size(), trimmed.data());
        break;
    }
}

static bool
PushFrontDefsFromFile(Options const& o, std::vector<Tracepoint>& tracepoints, char const* filename)
{
    // CodeQL [SM01937] This is a sample/tool. Using externally-supplied path is intended behavior.
    FILE* file = fopen(filename, "r");
    if (file == nullptr)
    {
        PrintStderr("error: cannot open file \"%s\", error %u.\n",
            filename, errno);
        return false;
    }

    std::vector<char> line;

    char buf[128];
    while (fgets(buf, sizeof(buf), file))
    {
        line.insert(line.end(), buf, buf + strlen(buf));
        if (line.back() == '\n')
        {
            PushFrontDef(o, tracepoints, Tracepoint({ line.data(), line.size() }));
            line.clear();
        }
    }

    auto const error = errno;

    bool const ok = 0 == ferror(file);
    fclose(file);

    if (!ok)
    {
        fprintf(stderr, "error: failed reading file \"%s\", error %u.\n",
            filename, error);
    }
    else
    {
        // Flush last line.
        PushFrontDef(o, tracepoints, Tracepoint({ line.data(), line.size() }));
    }

    return ok;
}

static void
ArgSize(
    _In_z_ char const* flagName,
    unsigned maxValue,
    int argi,
    int argc,
    _In_reads_(argc) char* argv[],
    _Inout_ bool* usageError,
    _Inout_ unsigned* value)
{
    if (argi >= argc)
    {
        PrintStderr("error: missing value for flag %s.\n",
            flagName);
        *usageError = true;
    }
    else
    {
        auto const* const arg = argv[argi];
        auto argValue = strtoul(arg, nullptr, 0);
        if (argValue == 0)
        {
            PrintStderr("error: expected positive integer for flag %s \"%s\".\n",
                flagName, arg);
            *usageError = true;
        }
        else if (argValue > maxValue)
        {
            PrintStderr("error: value %lu too large (max %u) for flag %s \"%s\".\n",
                argValue, maxValue, flagName, arg);
            *usageError = true;
        }
        else
        {
            *value = static_cast<unsigned>(argValue);
        }
    }
}

static void
InitExitSigSet(sigset_t* exitSigSet)
{
    sigemptyset(exitSigSet);
    for (auto exitSig : ExitSigs)
    {
        sigaddset(exitSigSet, exitSig);
    }
}

static int SignalHandled = 0;

class SignalMask
{
    int m_initError;
    bool m_masked;
    unsigned short m_sigsInstalled;
    sigset_t m_oldSigSet;
    struct sigaction m_oldActs[ExitSigsCount];

public:

    SignalMask(SignalMask const&) = delete;
    SignalMask& operator=(SignalMask const&) = delete;

    ~SignalMask()
    {
        Restore();
    }

    SignalMask() noexcept
        : m_initError(0)
        , m_masked(false)
        , m_sigsInstalled(0)
    {
        struct sigaction newAct = {};
        newAct.sa_handler = [](int sig)
            {
                SignalHandled = sig;
            };
        InitExitSigSet(&newAct.sa_mask);

        if (sigprocmask(SIG_BLOCK, &newAct.sa_mask, &m_oldSigSet))
        {
            m_initError = errno;
            PrintStderr("error: sigprocmask error %u.\n",
                m_initError);
            if (m_initError == 0)
            {
                m_initError = EINTR;
            }
            return;
        }

        m_masked = true;

        for (; m_sigsInstalled != ExitSigsCount; m_sigsInstalled += 1)
        {
            if (sigaction(ExitSigs[m_sigsInstalled], &newAct, &m_oldActs[m_sigsInstalled]))
            {
                m_initError = errno;
                PrintStderr("error: sigaction error %u.\n",
                    m_initError);
                if (m_initError == 0)
                {
                    m_initError = EINTR;
                }
                return;
            }
        }
    }

    int
    InitError() const noexcept
    {
        return m_initError;
    }

    sigset_t const*
    OldSigSet() const noexcept
    {
        return &m_oldSigSet;
    }

    void
    Restore() noexcept
    {
        for (; m_sigsInstalled != 0; m_sigsInstalled -= 1)
        {
            sigaction(ExitSigs[m_sigsInstalled - 1], &m_oldActs[m_sigsInstalled - 1], nullptr);
        }

        if (m_masked)
        {
            m_masked = false;
            sigprocmask(SIG_SETMASK, &m_oldSigSet, nullptr);

            if (m_initError == 0)
            {
                fputc('\n', stderr);
            }
        }
    }
};

static unsigned
EnableTracepoints(
    Options const& o,
    std::vector<Tracepoint> const& tracepoints,
    TracepointCache& cache,
    TracepointSession& session)
{
    unsigned enabledCount = 0;
    for (auto const& tp : tracepoints)
    {
        int error;
        if (tp.spec.Kind == TracepointSpecKind::Identifier)
        {
            error = cache.AddFromSystem(TracepointName(tp.spec.SystemName, tp.spec.EventName));
            switch (error)
            {
            default:
                PrintStderr("warning: Cannot load format for \"%.*s:%.*s\", error %u.\n",
                    (unsigned)tp.spec.SystemName.size(), tp.spec.SystemName.data(),
                    (unsigned)tp.spec.EventName.size(), tp.spec.EventName.data(),
                    error);
                continue;
            case 0:
                PrintStderrIf(o.verbose, "verbose: Loaded format for \"%.*s:%.*s\".\n",
                    (unsigned)tp.spec.SystemName.size(), tp.spec.SystemName.data(),
                    (unsigned)tp.spec.EventName.size(), tp.spec.EventName.data());
                break;
            case EEXIST:
                PrintStderrIf(o.verbose, "verbose: Format already loaded for \"%.*s:%.*s\".\n",
                    (unsigned)tp.spec.SystemName.size(), tp.spec.SystemName.data(),
                    (unsigned)tp.spec.EventName.size(), tp.spec.EventName.data());
                break;
            }
        }
        else
        {
            error = cache.PreregisterTracepointDefinition(tp.spec);
            switch (error)
            {
            default:
                PrintStderr("warning: Cannot pre-register \"%.*s:%.*s\", error %u.\n",
                    (unsigned)tp.spec.SystemName.size(), tp.spec.SystemName.data(),
                    (unsigned)tp.spec.EventName.size(), tp.spec.EventName.data(),
                    error);
                continue;
            case 0:
                PrintStderrIf(o.verbose, "verbose: Pre-registered \"%.*s:%.*s\".\n",
                    (unsigned)tp.spec.SystemName.size(), tp.spec.SystemName.data(),
                    (unsigned)tp.spec.EventName.size(), tp.spec.EventName.data());
                break;
            case EEXIST:
                PrintStderrIf(o.verbose, "verbose: Did not pre-register \"%.*s:%.*s\" (already cached).\n",
                    (unsigned)tp.spec.SystemName.size(), tp.spec.SystemName.data(),
                    (unsigned)tp.spec.EventName.size(), tp.spec.EventName.data());
                break;
            }
        }

        error = session.EnableTracepoint(TracepointName(tp.spec.SystemName, tp.spec.EventName));
        if (error != 0)
        {
            PrintStderr("warning: Cannot enable \"%.*s:%.*s\", error %u.\n",
                (unsigned)tp.spec.SystemName.size(), tp.spec.SystemName.data(),
                (unsigned)tp.spec.EventName.size(), tp.spec.EventName.data(),
                error);
        }
        else
        {
            enabledCount += 1;
            PrintStderrIf(o.verbose, "verbose: Enabled \"%.*s:%.*s\".\n",
                (unsigned)tp.spec.SystemName.size(), tp.spec.SystemName.data(),
                (unsigned)tp.spec.EventName.size(), tp.spec.EventName.data());
        }
    }

    return enabledCount;
}

static int
CollectCircular(Options const& o, TracepointSession& session)
{
    int error;
    int sig = 0;
    sigset_t exitSigSet;
    InitExitSigSet(&exitSigSet);

    // Scope for signalMask.
    PrintStderr("info: collecting until " EXIT_SIGNALS_STR ".\n");
    {
        SignalMask signalMask;
        error = signalMask.InitError();
        if (error != 0)
        {
            return error;
        }

        sigwait(&exitSigSet, &sig);
    }
    PrintStderr("info: stopping session (signal %u).\n",
        sig);

    error = session.SavePerfDataFile(o.output);
    if (error == 0)
    {
        PrintStderr("info: saved buffer contents to \"%s\".\n",
            o.output);
    }
    else
    {
        PrintStderr("error: failed saving to \"%s\", error %u.\n",
            o.output, error);
    }

    return error;
}

static int
CollectRealtime(Options const& o, TracepointSession& session)
{
    int error;
    unsigned wakeupCount = 0;
    uint64_t eventBytes = 0;

    PerfDataFileWriter writer;
    error = writer.Create(o.output);
    if (error != 0)
    {
        PrintStderr("error: failed creating file \"%s\", error %u.\n",
            o.output, error);
        goto Done;
    }

    error = writer.WriteFinishedInit();
    if (error != 0)
    {
        PrintStderr("error: failed writing FinishedInit to \"%s\", error %u.\n",
            o.output, error);
        unlink(o.output); // Nothing useful in the file.
        goto Done;
    }

    {
        auto const writerSessionStartPos = writer.FilePos();
        auto writerRoundStartPos = writerSessionStartPos;
        TracepointTimestampRange writtenRange;

        assert(SignalHandled == 0);

        // Scope for signalMask.
        PrintStderr("info: created \"%s\", collecting until " EXIT_SIGNALS_STR ".\n",
            o.output);
        {
            SignalMask signalMask;
            error = signalMask.InitError();
            if (error != 0)
            {
                unlink(o.output); // Nothing useful in the file.
                goto Done;
            }

            while (SignalHandled == 0) // Not sure whether this can ever be false.
            {
                error = session.WaitForWakeup(nullptr, signalMask.OldSigSet());
                if (error != 0)
                {
                    signalMask.Restore();
                    if (error != EINTR)
                    {
                        PrintStderr("error: ppoll failed, error %u.\n",
                            error);
                    }
                    else
                    {
                        PrintStderrIf(o.verbose, "verbose: ppoll EINTR.\n");
                    }
                    break;
                }

                wakeupCount += 1;
                error = session.FlushToWriter(writer, &writtenRange);
                if (error != 0)
                {
                    signalMask.Restore();
                    PrintStderr("error: failed flushing \"%s\", error %u.\n",
                        o.output, error);
                    goto Finalize;
                }

                auto const writerRoundEndPos = writer.FilePos();
                eventBytes = writerRoundEndPos - writerSessionStartPos;
                PrintStderrIf(o.verbose, "verbose: flushed %lu bytes.\n",
                    static_cast<unsigned long>(writerRoundEndPos - writerRoundStartPos));
                if (writerRoundStartPos != writerRoundEndPos)
                {
                    error = writer.WriteFinishedRound();
                    if (error != 0)
                    {
                        signalMask.Restore();
                        PrintStderr("error: failed writing FinishedRound to \"%s\", error %u.\n",
                            o.output, error);
                        goto Finalize;
                    }

                    writerRoundStartPos = writer.FilePos();
                }
            }
        }
        PrintStderr("info: stopping session (signal %u).\n",
            SignalHandled);

        error = session.FlushToWriter(writer, &writtenRange);
        if (error != 0)
        {
            PrintStderr("error: failed flushing \"%s\", error %u.\n",
                o.output, error);
            goto Finalize;
        }

        auto const writerSessionEndPos = writer.FilePos();
        eventBytes = writerSessionEndPos - writerSessionStartPos;
        PrintStderrIf(o.verbose, "verbose: flushed %lu bytes.\n",
            static_cast<unsigned long>(writerSessionEndPos - writerRoundStartPos));

        error = session.SetWriterHeaders(writer, &writtenRange);
        if (error != 0)
        {
            PrintStderr("error: failed collecting system info for \"%s\", error %u.\n",
                o.output, error);
            goto Finalize;
        }
    }

Finalize:

    {
        auto newError = writer.FinalizeAndClose();
        if (newError == 0)
        {
            PrintStderr("info: woke %u times, wrote 0x%lX bytes to \"%s\".\n",
                wakeupCount, static_cast<unsigned long>(eventBytes), o.output);
        }
        else if (error == 0)
        {
            error = newError;
            PrintStderr("error: failed finalizing \"%s\", error %u.\n",
                o.output, error);
        }
    }

Done:

    return error;
}

int
main(int argc, char* argv[])
{
    int error;

    try
    {
        std::vector<Tracepoint> tracepoints;
        Options o;
        unsigned const buffersizeMax = 0x80000000 / 1024;
        unsigned buffersize = 128u;
        unsigned const wakeupMax = 0x80000000 / 1024;
        unsigned wakeup = 2u;
        bool realtime = true;
        bool showHelp = false;
        bool usageError = false;

        for (int argi = 1; argi < argc; argi += 1)
        {
            auto const* const arg = argv[argi];
            if (arg[0] != '-')
            {
                PushFrontDef(o, tracepoints, Tracepoint(arg));
            }
            else if (arg[1] != '-')
            {
                auto const flags = &arg[1];
                for (unsigned flagsPos = 0; flags[flagsPos] != '\0'; flagsPos += 1)
                {
                    auto const flag = flags[flagsPos];
                    switch (flag)
                    {
                    case 'b':
                        argi += 1;
                        ArgSize("-b", buffersizeMax, argi, argc, argv, &usageError, &buffersize);
                        break;
                    case 'c':
                        realtime = false;
                        break;
                    case 'C':
                        realtime = true;
                        break;
                    case 'i':
                        argi += 1;
                        if (argi < argc)
                        {
                            PushFrontDefsFromFile(o, tracepoints, argv[argi]);
                        }
                        else
                        {
                            PrintStderr("error: missing filename for flag -i.\n");
                            usageError = true;
                        }
                        break;
                    case 'o':
                        argi += 1;
                        if (argi < argc)
                        {
                            o.output = argv[argi];
                        }
                        else
                        {
                            PrintStderr("error: missing filename for flag -o.\n");
                            usageError = true;
                        }
                        break;
                    case 'w':
                        argi += 1;
                        ArgSize("-w", wakeupMax, argi, argc, argv, &usageError, &wakeup);
                        break;
                    case 'v':
                        o.verbose = true;
                        break;
                    case 'h':
                        showHelp = true;
                        break;
                    default:
                        PrintStderr("error: invalid flag -%c.\n",
                            flag);
                        usageError = true;
                        break;
                    }
                }
            }
            else
            {
                auto const flag = &arg[2];
                if (0 == strcmp(flag, "buffersize"))
                {
                    argi += 1;
                    ArgSize("--buffersize", buffersizeMax, argi, argc, argv, &usageError, &buffersize);
                }
                else if (0 == strcmp(flag, "circular"))
                {
                    realtime = false;
                }
                else if (0 == strcmp(flag, "realtime"))
                {
                    realtime = true;
                }
                else if (0 == strcmp(flag, "input"))
                {
                    argi += 1;
                    if (argi < argc)
                    {
                        PushFrontDefsFromFile(o, tracepoints, argv[argi]);
                    }
                    else
                    {
                        PrintStderr("error: missing filename for flag --input.\n");
                        usageError = true;
                    }
                }
                else if (0 == strcmp(flag, "output"))
                {
                    argi += 1;
                    if (argi < argc)
                    {
                        o.output = argv[argi];
                    }
                    else
                    {
                        PrintStderr("error: missing filename for flag --output.\n");
                        usageError = true;
                    }
                }
                else if (0 == strcmp(flag, "wakeup"))
                {
                    argi += 1;
                    ArgSize("--wakeup", wakeupMax, argi, argc, argv, &usageError, &wakeup);
                }
                else if (0 == strcmp(flag, "verbose"))
                {
                    o.verbose = true;
                }
                else if (0 == strcmp(flag, "help"))
                {
                    showHelp = true;
                }
                else
                {
                    PrintStderr("error: invalid flag \"--%s\".\n",
                        flag);
                    usageError = true;
                }
            }
        }

        if (showHelp || usageError)
        {
            fputs(UsageCommon, stdout);
            fputs(showHelp ? UsageLong : UsageShort, stdout);
            error = EINVAL;
            goto Done;
        }
        else if (tracepoints.empty())
        {
            PrintStderr("error: no tracepoints specified, exiting.\n");
            error = EINVAL;
            goto Done;
        }
        else if (realtime && wakeup >= buffersize)
        {
            PrintStderr("error: wakeup size %u must be less than buffersize %u.\n",
                wakeup, buffersize);
            error = EINVAL;
            goto Done;
        }

        auto const mode = realtime
            ? TracepointSessionMode::RealTime
            : TracepointSessionMode::Circular;
        TracepointCache cache;
        TracepointSession session(
            cache,
            TracepointSessionOptions(mode, buffersize * 1024)
            .WakeupWatermark(wakeup * 1024));

        unsigned const enabledCount = EnableTracepoints(o, tracepoints, cache, session);
        if (enabledCount == 0)
        {
            PrintStderr("error: No tracepoints enabled, exiting.\n");
            error = ENOENT;
            goto Done;
        }

        switch (mode)
        {
        case TracepointSessionMode::Circular:
            error = CollectCircular(o, session);
            break;
        case TracepointSessionMode::RealTime:
            error = CollectRealtime(o, session);
            break;
        default:
            assert(false);
            break;
        }
    }
    catch (std::exception const& ex)
    {
        PrintStderr("fatal error: %s.\n",
            ex.what());
        error = ENOMEM;
    }

Done:

    return error;
}
