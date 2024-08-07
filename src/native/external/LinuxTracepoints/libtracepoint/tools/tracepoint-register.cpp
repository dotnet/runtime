// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#include <tracepoint/tracepoint.h>
#include <assert.h>
#include <stdarg.h>
#include <stdio.h>
#include <string.h>
#include <signal.h>

#include <string>
#include <string_view>
#include <forward_list>

using namespace std::string_view_literals;

// From eventheader/eventheader.h:
#define EVENTHEADER_COMMAND_TYPES "u8 eventheader_flags; u8 version; u16 id; u16 tag; u8 opcode; u8 level"

static constexpr std::string_view EventHeaderCommandTypes = EVENTHEADER_COMMAND_TYPES;

enum {
    // Maximum length of a Tracepoint name "ProviderName_Attributes", including nul termination.
    EVENTHEADER_NAME_MAX = 256,

    // Maximum length needed for a DIAG_IOCSREG command "ProviderName_Attributes CommandTypes".
    EVENTHEADER_COMMAND_MAX = EVENTHEADER_NAME_MAX + 1 + EventHeaderCommandTypes.size(), // +1 for space.
};

// From tracepoint.c:
extern "C" int
tracepoint_connect2(
    tracepoint_state* tp_state,
    tracepoint_provider_state* provider_state,
    char const* tp_name_args,
    unsigned flags);

// From uapi/linux/perf_event.h:
enum user_reg_flag {
    USER_EVENT_REG_PERSIST = 1U << 0,
};

enum WaitSetting : unsigned char {
    WaitUnspecified,
    WaitNo,
    WaitYes,
};

struct TracepointInfo
{
    std::string command;
    tracepoint_state state;

    explicit
    TracepointInfo(std::string&& _command)
        : command(std::move(_command))
        , state(TRACEPOINT_STATE_INIT)
    {
        return;
    }
};

struct Options
{
    bool verbose = false;
};

#define PROGRAM_NAME "tracepoint-register"
#define EXIT_SIGNALS      SIGTERM, SIGINT
#define EXIT_SIGNALS_STR "SIGTERM, SIGINT"

static char const* const UsageCommon = R"(
Usage: )" PROGRAM_NAME R"( [options...] UserEventDefinitions...
)";

// Usage error: stderr += UsageCommon + UsageShort.
static char const* const UsageShort = R"(
Try ")" PROGRAM_NAME R"( --help" for more information.
)";

// -h or --help: stdout += UsageCommon + UsageLong.
static char const* const UsageLong = R"(
Pre-registers user_events tracepoints so you can start recording (e.g. with
the Linux "perf" tool) before starting the program that generates the events.

Requires write access to /sys/kernel/tracing/user_events_data. The -p option
requires the CAP_PERFMON capability.

Options:

-i, --input <file>  Read additional UserEventDefinitions from <file>. Each
                    line in the file is treated as a UserEventDefinition.
                    Empty lines and lines starting with '#' are ignored.

-p, --persist       Use the USER_EVENT_REG_PERSIST flag when registering each
                    tracepoint so that the tracepoints remain available after
                    )" PROGRAM_NAME R"( exits (requires CAP_PERFMON).

-w, --wait          Do not exit until signalled ()" EXIT_SIGNALS_STR R"().
                    Keeps tracepoints registered until exit. This is the
                    default if -p is not specified.

-W, --nowait        Exit immediately. This is the default if -p is specified.

-v, --verbose       Show diagnostic output.

-h, --help          Show this help message and exit.

A UserEventDefinition must be formatted as follows (see
https://docs.kernel.org/trace/user_events.html#command-format for details):

    EventName[:Flags] Fields...

Fields... is list of fields. Multiple fields are separated by "; ". If an
event has no fields, use ";" as the Fields... definition. Note that ";" is a
valid EventName character, so there must be whitespace between EventName and
the ";".

At present, no flags are defined.

UserEventDefinition examples:

- MyEvent1 ;
- MyEvent2 u32 MyField1
- MyEvent3:flag u32 MyField1; struct MyStruct2 MyField2 20

As a shortcut, an EventHeader tracepoint may be specified without specifying
the fields. A UserEventDefinition that contains no whitespace will be treated
as an EventHeader tracepoint and will be expanded to include the standard
EventHeader fields.

EventHeader UserEventDefinition examples:

- MyProvider_L2K1
- MyProvider_L5K3ffGmygroup
- MyProvider_L5K3ffGmygroup:flag
)";

static bool
AsciiIsLowercaseHex(char ch)
{
    return
        ('0' <= ch && ch <= '9') ||
        ('a' <= ch && ch <= 'f');
}

static bool
AsciiIsAlphanumeric(char ch)
{
    return
        ('0' <= ch && ch <= '9') ||
        ('A' <= ch && ch <= 'Z') ||
        ('a' <= ch && ch <= 'z');
}

static bool
AsciiIsSpace(char ch)
{
    return ch == ' ' || ('\t' <= ch && ch <= '\r');
}

// fprintf(stderr, format, args...).
static void
PrintStderr(const char* format, ...)
{
    va_list args;
    va_start(args, format);
    fputs(PROGRAM_NAME ": ", stderr);
    vfprintf(stderr, format, args);
    va_end(args);
}

// if (condition) fprintf(stderr, format, args...).
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
PushFrontDef(Options const& o, std::forward_list<TracepointInfo>& tracepoints, std::string_view line)
{
    // Trim trailing whitespace.
    size_t endPos = line.size();
    for (; endPos != 0; endPos -= 1)
    {
        auto const ch = line[endPos - 1];
        if (!AsciiIsSpace(ch))
        {
            break;
        }
    }

    // Trim leading whitespace.
    size_t startPos = 0;
    for (; startPos != endPos; startPos += 1)
    {
        if (!AsciiIsSpace(line[startPos]))
        {
            break;
        }
    }

    auto const trimmedDef = line.substr(startPos, endPos - startPos);
    if (trimmedDef.empty())
    {
        return;
    }
    else if (trimmedDef[0] == '#')
    {
        return;
    }

    std::string command;

    auto const firstInternalWhitespace = trimmedDef.find_first_of("\t\n\v\f\r "sv);
    if (std::string_view::npos != firstInternalWhitespace)
    {
        // Traditional tracepoint definition.
        // Trim trailing semicolons and whitespace, e.g. "EventName ;" or "EventName Field; ; ;".
        // EventName may contain semicolons, so "EventName; ; ;" must trim to "EventName;".
        size_t trimmedEnd;
        for (trimmedEnd = trimmedDef.size(); trimmedEnd != firstInternalWhitespace; trimmedEnd -= 1)
        {
            auto const ch = trimmedDef[trimmedEnd - 1];
            if (ch != ';' && !AsciiIsSpace(ch))
            {
                break;
            }
        }

        assert(trimmedEnd != 0);
        command = trimmedDef.substr(0, trimmedEnd);
    }
    else
    {
        // EventHeader tracepoint definition. Needs validation and expansion.

        // name = trimmedDef up to last ':'. If no ':', name = trimmedDef.
        auto const name = trimmedDef.substr(0, trimmedDef.rfind(':'));

        if (name.size() >= EVENTHEADER_NAME_MAX)
        {
            fprintf(stderr, "error: eventheader name \"%.*s\" is too long.\n",
                (unsigned)name.size(), name.data());
            return;
        }

        if (name.find(':') != std::string_view::npos)
        {
            fprintf(stderr, "error: eventheader name \"%.*s\" contains invalid char ':'.\n",
                (unsigned)name.size(), name.data());
            return;
        }

        auto pos = name.rfind('_');

        if (pos == std::string_view::npos ||
            name.size() < pos + 3 ||
            name[pos + 1] != 'L' ||
            !AsciiIsLowercaseHex(name[pos + 2]))
        {
            fprintf(stderr, "error: eventheader name \"%.*s\" is missing the required \"_L<level>\" suffix.\n",
                (unsigned)name.size(), name.data());
            return;
        }

        // Skip "_Lnnn"
        pos += 3;
        while (pos < name.size() && AsciiIsLowercaseHex(name[pos]))
        {
            pos += 1;
        }

        if (name.size() < pos + 2 ||
            name[pos] != 'K' ||
            !AsciiIsLowercaseHex(name[pos + 1]))
        {
            fprintf(stderr, "error: eventheader name \"%.*s\" is missing the required \"K<keyword>\" suffix.\n",
                (unsigned)name.size(), name.data());
            return;
        }

        // Skip "Knnn..."
        pos += 2;
        for (; pos < name.size(); pos += 1)
        {
            if (!AsciiIsAlphanumeric(name[pos]))
            {
                fprintf(stderr, "error: eventheader name \"%.*s\" contains non-alphanumeric characters in the \"_L<level>K<keyword>...\" suffix.\n",
                    (unsigned)name.size(), name.data());
                return;
            }
        }

        command.reserve(trimmedDef.size() + 1 + EventHeaderCommandTypes.size());
        command = trimmedDef;
        command += ' ';
        command += EventHeaderCommandTypes;
    }

    PrintStderrIf(o.verbose, "verbose: add \"%s\"\n",
        command.c_str());

    tracepoints.emplace_front(std::move(command));
}

static bool
PushFrontDefsFromFile(Options const& o, std::forward_list<TracepointInfo>& tracepoints, char const* filename)
{
    // CodeQL [SM01937] This is a sample/tool. Using externally-supplied path is intended behavior.
    FILE* file = fopen(filename, "r");
    if (file == nullptr)
    {
        fprintf(stderr, "error: failed to open file \"%s\".\n", filename);
        return false;
    }

    std::string line;

    char buf[128];
    while (fgets(buf, sizeof(buf), file))
    {
        line += buf;
        if (line.back() == '\n')
        {
            PushFrontDef(o, tracepoints, line);
            line.clear();
        }
    }

    bool const ok = 0 == ferror(file);
    fclose(file);

    if (!ok)
    {
        fprintf(stderr, "error: failed to read file \"%s\".\n", filename);
    }
    else
    {
        PushFrontDef(o, tracepoints, line);
    }

    return ok;
}

int
main(int argc, char* argv[])
{
    int error;
    tracepoint_provider_state providerState = TRACEPOINT_PROVIDER_STATE_INIT;

    try
    {
        std::forward_list<TracepointInfo> tracepoints;
        Options o;
        auto waitSetting = WaitUnspecified;
        bool persist = false;
        bool showHelp = false;
        bool usageError = false;

        for (int argi = 1; argi < argc; argi += 1)
        {
            char const* const arg = argv[argi];
            if (arg[0] != '-')
            {
                PushFrontDef(o, tracepoints, arg);
            }
            else if (arg[1] != '-')
            {
                auto const flags = &arg[1];
                for (unsigned flagsPos = 0; flags[flagsPos] != '\0'; flagsPos += 1)
                {
                    auto const flag = flags[flagsPos];
                    switch (flag)
                    {
                    case 'i':
                        argi += 1;
                        if (argi < argc)
                        {
                            PushFrontDefsFromFile(o, tracepoints, argv[argi]);
                        }
                        else
                        {
                            PrintStderr("error: missing filename for flag -i\n");
                            usageError = true;
                        }
                        break;
                    case 'p':
                        persist = true;
                        break;
                    case 'w':
                        waitSetting = WaitYes;
                        break;
                    case 'W':
                        waitSetting = WaitNo;
                        break;
                    case 'v':
                        o.verbose = true;
                        break;
                    case 'h':
                        showHelp = true;
                        break;
                    default:
                        PrintStderr("error: invalid flag -%c\n", flag);
                        usageError = true;
                        break;
                    }
                }
            }
            else
            {
                auto const flag = &arg[2];
                if (0 == strcmp(flag, "input"))
                {
                    argi += 1;
                    if (argi < argc)
                    {
                        PushFrontDefsFromFile(o, tracepoints, argv[argi]);
                    }
                    else
                    {
                        PrintStderr("error: missing filename for flag --input\n");
                        usageError = true;
                    }
                }
                else if (0 == strcmp(flag, "persist"))
                {
                    persist = true;
                }
                else if (0 == strcmp(flag, "wait"))
                {
                    waitSetting = WaitYes;
                }
                else if (0 == strcmp(flag, "nowait"))
                {
                    waitSetting = WaitNo;
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
                    PrintStderr("error: invalid flag --%s\n", flag);
                    usageError = true;
                }
            }
        }

        if (showHelp)
        {
            fputs(UsageCommon, stdout);
            fputs(UsageLong, stdout);
            error = EINVAL;
        }
        else if (usageError)
        {
            fputs(UsageCommon, stderr);
            fputs(UsageShort, stderr);
            error = EINVAL;
        }
        else if (tracepoints.empty())
        {
            PrintStderr("error: no tracepoints specified, exiting.\n");
            error = EINVAL;
        }
        else
        {
            error = tracepoint_open_provider(&providerState);
            if (0 != error)
            {
                PrintStderr("error: tracepoint_open_provider failed (%u).\n",
                    error);
            }
            else
            {
                tracepoints.reverse();
                for (auto& tracepoint : tracepoints)
                {
                    unsigned const flags = persist ? USER_EVENT_REG_PERSIST : 0;
                    int connectResult = tracepoint_connect2(&tracepoint.state, &providerState, tracepoint.command.c_str(), flags);
                    if (connectResult != 0)
                    {
                        PrintStderr("warning: tracepoint_connect failed (%u) for \"%s\".\n",
                            connectResult, tracepoint.command.c_str());
                    }
                }

                if (waitSetting == WaitYes ||
                    (waitSetting == WaitUnspecified && !persist))
                {
                    sigset_t exitSigSet;
                    sigemptyset(&exitSigSet);
                    static constexpr int ExitSigs[] = { EXIT_SIGNALS };
                    for (auto exitSig : ExitSigs)
                    {
                        sigaddset(&exitSigSet, exitSig);
                    }

                    PrintStderrIf(o.verbose, "verbose: waiting for { " EXIT_SIGNALS_STR " }.\n");

                    sigset_t oldSigSet;
                    if (sigprocmask(SIG_BLOCK, &exitSigSet, &oldSigSet))
                    {
                        PrintStderr("error: sigprocmask returned %u\n",
                            errno);
                    }
                    else
                    {
                        int sig = 0;
                        sigwait(&exitSigSet, &sig);
                        sigprocmask(SIG_SETMASK, &oldSigSet, nullptr);
                        PrintStderrIf(o.verbose, "verbose: signal %u.\n",
                            sig);
                    }
                }
            }

            tracepoint_close_provider(&providerState);
        }
    }
    catch (std::exception const& ex)
    {
        PrintStderr("fatal error: %s\n", ex.what());
        error = ENOMEM;
    }

    return error;
}
