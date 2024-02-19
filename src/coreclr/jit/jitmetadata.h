// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

class Compiler;

enum class JitMetadataName
{
#define JITMETADATA(name, type, flags) name,
#include "jitmetadatalist.h"
};

class JitMetadata
{
public:
    static const char* getName(JitMetadataName name);
    static void report(Compiler* comp, JitMetadataName name, const void* data);
};

class JitMetrics
{
public:
#define JITMETADATAINFO(name, type, flags)
#define JITMETADATAMETRIC(name, type, flags) type name = 0;
#include "jitmetadatalist.h"

    void report(Compiler* comp);
    void dump(Compiler* comp);
};
