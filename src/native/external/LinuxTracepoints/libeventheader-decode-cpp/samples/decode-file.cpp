// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#include <eventheader/EventFormatter.h>

#include <assert.h>
#include <stdio.h>
#include <string.h>

#include <memory>
#include <string>
#include <vector>

#ifdef _WIN32
#include <share.h>
#define fopen(filename, mode) _fsopen(filename, mode, _SH_DENYWR)
#define strerror_r(errnum, buf, buflen) (strerror_s(buf, buflen, errnum), buf)
#define le32toh(x) x
#endif // _WIN32

using namespace eventheader_decode;

struct fcloseDelete
{
    void operator()(FILE* file) const noexcept
    {
        fclose(file);
    }
};

static bool
ReadFromFile(FILE* file, void* buffer, uint32_t size)
{
    size_t actual = fread(buffer, 1, size, file);
    if (size == actual)
    {
        return true;
    }

    int err = ferror(file);
    if (err)
    {
        char errBuf[80];
        printf("\n- fread error %u %s",
            err,
            strerror_r(err, errBuf, sizeof(errBuf)));
    }
    else if (actual != 0)
    {
        printf("\n- fread early eof (asked for %u, got %u)", size, static_cast<unsigned>(actual));
    }

    return false;
}

int main(int argc, char* argv[])
{
    int err;
    if (argc <= 1)
    {
        printf("\nUsage: %s [InterceptorSampleFileName1] ...\n", argv[0]);
        err = 1;
        goto Done;
    }

    try
    {
        std::vector<char> buffer(4096);
        std::string eventText;
        EventEnumerator enumerator;
        EventFormatter formatter;
        bool comma = false;

        for (int argi = 1; argi < argc; argi += 1)
        {
            char const* const filename = argv[argi];
            printf("%s\n\"%s\": [",
                comma ? "," : "",
                filename);
            comma = false;

            std::unique_ptr<FILE, fcloseDelete> file(fopen(filename, "rb"));
            if (!file)
            {
                err = errno;
                char errBuf[80];
                printf("\n- fopen(%s) error %u %s",
                    filename,
                    err,
                    strerror_r(err, errBuf, sizeof(errBuf)));
            }
            else for (;;)
            {
                uint32_t recordSize;
                if (!ReadFromFile(file.get(), &recordSize, sizeof(recordSize)))
                {
                    break;
                }

                recordSize = le32toh(recordSize);

                if (recordSize <= sizeof(recordSize))
                {
                    printf("\n- Unexpected recordSize %u", recordSize);
                    break;
                }

                recordSize -= sizeof(recordSize); // File's recordSize includes itself.

                if (buffer.size() < recordSize)
                {
                    buffer.reserve(recordSize);
                    buffer.resize(buffer.capacity());
                }

                if (!ReadFromFile(file.get(), buffer.data(), recordSize))
                {
                    break;
                }

                auto const nameSize = static_cast<uint32_t>(strnlen(buffer.data(), recordSize));
                if (nameSize == recordSize)
                {
                    printf("\n- TracepointName not nul-terminated.");
                    continue;
                }

                fputs(comma ? ",\n " : "\n ", stdout);
                comma = true;

                if (!enumerator.StartEvent(
                    buffer.data(), // tracepoint name
                    nameSize, // tracepoint name length
                    buffer.data() + nameSize + 1, // event data
                    recordSize - nameSize - 1))   // event data length
                {
                    printf("\n- StartEvent error %d.", enumerator.LastError());
                }
                else
                {
                    eventText.clear();
                    err = formatter.AppendEventAsJsonAndMoveToEnd(
                        eventText, enumerator, static_cast<EventFormatterJsonFlags>(
                            EventFormatterJsonFlags_Space |
                            EventFormatterJsonFlags_FieldTag));
                    if (err != 0)
                    {
                        printf("\n- AppendEvent error.");
                    }
                    else
                    {
                        fputs(eventText.c_str(), stdout);
                    }
                }
            }

            fputs(" ]", stdout);
            comma = true;
        }

        printf("\n");
        err = 0;
    }
    catch (std::exception const& ex)
    {
        printf("\nException: %s\n", ex.what());
        err = 1;
    }

Done:

    return err;
}
