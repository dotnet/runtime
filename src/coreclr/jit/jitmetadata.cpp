// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "jitpch.h"
#include "jitmetadata.h"

#ifdef DEBUG

const char* JitMetadata::getName(JitMetadataName name)
{
    switch (name)
    {
#define JITMETADATA(name, type, flags)                                                                                 \
    case JitMetadataName::name:                                                                                        \
        return #name;
#include "jitmetadatalist.h"

        default:
            unreached();
    }
}

void JitMetadata::report(Compiler* comp, JitMetadataName name, const void* data)
{
    comp->info.compCompHnd->reportMetadata(getName(name), data);
}

template <typename T>
static void reportValue(Compiler* comp, const char* key, T value)
{
    comp->info.compCompHnd->reportMetadata(key, &value);
}

void JitMetrics::report(Compiler* comp)
{
#define JITMETADATAINFO(name, type, flags)
#define JITMETADATAMETRIC(name, type, flags) reportValue(comp, #name, name);
#include "jitmetadatalist.h"
}

static void printMetric(double value)
{
    printf("%f", value);
}

static void printMetric(int value)
{
    printf("%d", value);
}

static void printMetric(int64_t value)
{
    printf("%lld", value);
}

void JitMetrics::dump(Compiler* comp)
{
    int nameMaxWidth = 0;
#define JITMETADATAINFO(name, type, flags)
#define JITMETADATAMETRIC(name, type, flags) nameMaxWidth = max(nameMaxWidth, (int)strlen(#name));
#include "jitmetadatalist.h"

#define JITMETADATAINFO(name, type, flags)
#define JITMETADATAMETRIC(name, type, flags)                                                                           \
    printf("%-*s: ", nameMaxWidth + 5, #name);                                                                         \
    printMetric(name);                                                                                                 \
    printf("\n");
#include "jitmetadatalist.h"
}

#endif
