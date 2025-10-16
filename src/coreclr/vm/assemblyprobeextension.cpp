// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"
#include <bundle.h>
#include <hostinformation.h>
#include <sstring.h>

/*static*/
bool AssemblyProbeExtension::IsEnabled()
{
    LIMITED_METHOD_CONTRACT;

    return Bundle::AppIsBundle() || HostInformation::HasExternalProbe();
}

/*static*/
ProbeExtensionResult AssemblyProbeExtension::Probe(const SString& path, bool pathIsBundleRelative)
{
    STANDARD_VM_CONTRACT;

    if (!Bundle::AppIsBundle() && !HostInformation::HasExternalProbe())
        return ProbeExtensionResult::Invalid();

    if (Bundle::AppIsBundle())
    {
        BundleFileLocation bundleLocation = Bundle::AppBundle->Probe(path, pathIsBundleRelative);
        if (bundleLocation.IsValid())
        {
            return ProbeExtensionResult::Bundle(bundleLocation);
        }
    }

    void* data;
    int64_t size;
    if (HostInformation::ExternalAssemblyProbe(path, &data, &size))
    {
        return ProbeExtensionResult::External(data, size);
    }

    return ProbeExtensionResult::Invalid();
}
