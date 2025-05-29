// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef HAVE_ASSEMBLY_PROBE_EXTENSIONS_H
#define HAVE_ASSEMBLY_PROBE_EXTENSIONS_H

#include <sstring.h>
#include "bundle.h"

class ProbeExtensionResult
{
public:
    enum class Type : int32_t
    {
        Invalid,
        Bundle,
        External,
    };

    Type Type;
    union
    {
        BundleFileLocation BundleLocation;
        struct
        {
            void* Data;
            int64_t Size;
        } ExternalData;
    };

    ProbeExtensionResult()
        : Type{Type::Invalid}
    { }

    static ProbeExtensionResult Bundle(BundleFileLocation location)
    {
        return ProbeExtensionResult(location);
    }

    static ProbeExtensionResult External(void* data, int64_t size)
    {
        return ProbeExtensionResult(data, size);
    }

    static ProbeExtensionResult Invalid() { LIMITED_METHOD_CONTRACT; return ProbeExtensionResult(); }

    bool IsValid() const { return Type != Type::Invalid; }

private:
    ProbeExtensionResult(BundleFileLocation location)
        : Type{Type::Bundle}
        , BundleLocation{location}
    { }

    ProbeExtensionResult(void* data, int64_t size)
        : Type{Type::External}
        , ExternalData{data, size}
    { }
};

class AssemblyProbeExtension
{
public:
    static bool IsEnabled();
    static ProbeExtensionResult Probe(const SString& path, bool pathIsBundleRelative = false);
};

#endif // HAVE_ASSEMBLY_PROBE_EXTENSIONS_H
