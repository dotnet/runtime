// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.Build.Framework;
using NuGet.RuntimeModel;
using NuGet.Versioning;

namespace Microsoft.DotNet.Build.Tasks;

public sealed class GenerateRuntimeDependencies : BuildTask
{
    [Required]
    public ITaskItem[] RuntimeIdentifiers { get; set; }

    [Required]
    public string PackageId { get; set; }

    [Required]
    public string PackageVersion { get; set; }

    [Required]
    public string OutputPath { get; set; }

    public override bool Execute()
    {
        if (RuntimeIdentifiers.Length == 0)
        {
            Log.LogError("At least one runtime identifier is required.");
            return false;
        }

        var runtimes = new List<RuntimeDescription>();
        var versionRange = new VersionRange(NuGetVersion.Parse(PackageVersion));
        foreach (ITaskItem rid in RuntimeIdentifiers)
        {
            var dependency = new RuntimePackageDependency($"runtime.{rid.ItemSpec}.{PackageId}", versionRange);
            var dependencySet = new RuntimeDependencySet(PackageId, new[] { dependency });
            runtimes.Add(new RuntimeDescription(rid.ItemSpec, new[] { dependencySet }));
        }

        JsonRuntimeFormat.WriteRuntimeGraph(OutputPath, new RuntimeGraph(runtimes));
        return true;
    }
}
