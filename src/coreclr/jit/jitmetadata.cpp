// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "jitpch.h"
#include "jitmetadata.h"

#ifdef DEBUG

//------------------------------------------------------------------------
// JitMetadata::getName: Get the name corresponding to a JitMetadataName value,
// which can be used to report metadata back to the EE.
//
// Parameters:
//   name - The JitMetadataName enum entry
//
// Returns:
//   String representation of the name
//
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

//------------------------------------------------------------------------
// JitMetadata::report: Report metadata back to the EE.
//
// Parameters:
//   comp - Compiler instance
//   name - Enum entry specifying the metadata name
//   data - Pointer to the value to report back
//
void JitMetadata::report(Compiler* comp, JitMetadataName name, const void* data)
{
    comp->info.compCompHnd->reportMetadata(getName(name), data);
}

//------------------------------------------------------------------------
// reportValue: Report a specific value back to the EE.
//
// Parameters:
//   comp  - Compiler instance
//   key   - The key
//   value - Value to report back
//
template <typename T>
static void reportValue(Compiler* comp, const char* key, T value)
{
    comp->info.compCompHnd->reportMetadata(key, &value);
}

//------------------------------------------------------------------------
// JitMetrics::report: Report all metrics and their values back to the EE.
//
// Parameters:
//   comp - Compiler instance
//
void JitMetrics::report(Compiler* comp)
{
#define JITMETADATAINFO(name, type, flags)
#define JITMETADATAMETRIC(name, type, flags) reportValue(comp, #name, name);
#include "jitmetadatalist.h"
}

//------------------------------------------------------------------------
// printMetric: Print a double metric value to jitstdout.
//
// Parameters:
//   value - The value
//
static void printMetric(double value)
{
    printf("%f", value);
}

//------------------------------------------------------------------------
// printMetric: Print an int metric value to jitstdout.
//
// Parameters:
//   value - The value
//
static void printMetric(int value)
{
    printf("%d", value);
}

//------------------------------------------------------------------------
// printMetric: Print an int64_t metric value to jitstdout.
//
// Parameters:
//   value - The value
//
static void printMetric(int64_t value)
{
    printf("%lld", value);
}

//------------------------------------------------------------------------
// JitMetrics::dump: Print the values of all metrics to jitstdout.
//
void JitMetrics::dump()
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
