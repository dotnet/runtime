// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using NiceIO;

namespace BuildDriver;

static class Artifacts
{
    public static NPath ConsolidateArtifacts(GlobalConfig gConfig)
    {
        CopyUnityGCTo(gConfig,
            Utils.RuntimeArtifactDirectory(gConfig).Combine("native"),
            Utils.UnityTestHostDotNetAppDirectory(gConfig));

        CopyUnityEmbedHostToArtifacts(gConfig);

        Paths.RepoRoot.Combine("LICENSE.md").Copy(Utils.RuntimeArtifactDirectory(gConfig));

        return Utils.RuntimeArtifactDirectory(gConfig);
    }

    public static void CopyUnityEmbedHostToArtifacts(GlobalConfig gConfig)
    {
        CopyUnityEmbedHostTo(gConfig,
            Utils.RuntimeArtifactDirectory(gConfig).Combine("lib", Utils.UnityEmbedHostTfmDirectoryName(gConfig)),
            Utils.UnityTestHostDotNetAppDirectory(gConfig));
    }

    static void CopyUnityGCTo(GlobalConfig gConfig, params NPath[] destinations)
    {
        foreach (var dest in destinations)
            Paths.UnityGC.Combine(gConfig.Configuration, Paths.UnityGCFileName).Copy(dest);
    }

    static void CopyUnityEmbedHostTo(GlobalConfig gConfig, params NPath[] destinations)
    {
        var files = Paths.UnityEmbedHost.Combine("bin", gConfig.Configuration, Utils.UnityEmbedHostTfmDirectoryName(gConfig))
            .Files("unity-embed-host.*")
            .ToArray();
        foreach (var dest in destinations)
            files.Copy(dest);
    }
}
