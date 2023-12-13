// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using NiceIO;

namespace BuildDriver;

static class Artifacts
{
    public static NPath ConsolidateArtifacts(GlobalConfig gConfig)
    {
        Paths.RepoRoot.Combine("LICENSE.TXT").Copy(Utils.RuntimeArtifactDirectory(gConfig).Combine("LICENSE.md"));

        return Utils.RuntimeArtifactDirectory(gConfig);
    }

}
