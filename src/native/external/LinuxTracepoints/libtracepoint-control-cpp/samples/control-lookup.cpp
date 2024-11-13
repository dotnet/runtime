// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

/*
Demonstrates use of TracepointCache to look up tracefs metadata.
*/

#include <tracepoint/TracepointCache.h>
#include <stdio.h>

using namespace std::string_view_literals;
using namespace tracepoint_control;

int
main(int argc, char* argv[])
{
    TracepointCache cache;
    for (int argi = 1; argi < argc; argi += 1)
    {
        int error;
        auto const name = TracepointName(argv[argi]);

        error = cache.AddFromSystem(name);
        fprintf(stdout, "AddFromSystem(%.*s:%.*s)=%u\n",
            (unsigned)name.SystemName.size(), name.SystemName.data(),
            (unsigned)name.EventName.size(), name.EventName.data(),
            error);

        unsigned id = 0;
        if (auto const meta = cache.FindByName(name); meta)
        {
            fprintf(stdout, "- FindByName=%u\n", meta->Id());
            fprintf(stdout, "  Sys = %.*s\n", (unsigned)meta->SystemName().size(), meta->SystemName().data());
            fprintf(stdout, "  Name= %.*s\n", (unsigned)meta->Name().size(), meta->Name().data());
            fprintf(stdout, "  Fmt = %.*s\n", (unsigned)meta->PrintFmt().size(), meta->PrintFmt().data());
            fprintf(stdout, "  Flds= %u\n", (unsigned)meta->Fields().size());
            fprintf(stdout, "  Id  = %u\n", (unsigned)meta->Id());
            fprintf(stdout, "  CmnC= %u\n", (unsigned)meta->CommonFieldCount());
            fprintf(stdout, "  Kind= %u\n", (unsigned)meta->Kind());
            id = meta->Id();
        }

        if (auto const meta = cache.FindById(id); meta)
        {
            fprintf(stdout, "- FindById(%u)=%u\n", id, meta->Id());
        }
    }

    fprintf(stdout, "CommonTypeOffset=%d\n", cache.CommonTypeOffset());
    fprintf(stdout, "CommonTypeSize  =%u\n", cache.CommonTypeSize());

    return 0;
}
