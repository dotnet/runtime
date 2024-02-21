// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

class Compiler;

class JitMetadata
{
public:
#define JITMETADATA(name, type, flags) static constexpr const char* name = #name;
#include "jitmetadatalist.h"

    static void report(Compiler* comp, const char* name, const void* data, size_t length);
};

class JitMetrics
{
public:
#define JITMETADATAINFO(name, type, flags)
#define JITMETADATAMETRIC(name, type, flags) type name = 0;
#include "jitmetadatalist.h"

    void report(Compiler* comp);
    void dump();
};
