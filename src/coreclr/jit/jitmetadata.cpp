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

#endif
