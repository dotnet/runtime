// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"
#include <bundle.h>
#include <sstring.h>

/*static*/
bool AssemblyProbeExtension::IsEnabled()
{
    LIMITED_METHOD_CONTRACT;

    return Bundle::AppIsBundle();
}

/*static*/
ProbeExtensionResult AssemblyProbeExtension::Probe(const SString& path, bool pathIsBundleRelative)
{
    STANDARD_VM_CONTRACT;

    if (Bundle::AppIsBundle())
    {
        BundleFileLocation bundleLocation = Bundle::AppBundle->Probe(path, pathIsBundleRelative);
        if (bundleLocation.IsValid())
        {
            return ProbeExtensionResult::Bundle(bundleLocation);
        }
    }

    return ProbeExtensionResult::Invalid();
}
