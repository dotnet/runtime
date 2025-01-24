// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using NiceIO;

namespace BuildDriver;

static class Artifacts
{
    // Clean up the artifacts dir and rearrange things into a structure that makes sense for us
    public static NPath ConsolidateArtifacts(GlobalConfig gConfig)
    {
        Paths.RepoRoot.Combine("LICENSE.TXT").Copy(Utils.RuntimeArtifactDirectory(gConfig).Combine("LICENSE.md"));

        // This directory contains windows specific native libraries for reading/writing pdbs that we do not need.
        Utils.RuntimeArtifactDirectory(gConfig).Combine("native", "runtimes").DeleteIfExists();

        //Get rid of the tfm directory to save on code churn on our side when we upgrade. We won't be supporting more than one.
        NPath lib = Utils.RuntimeArtifactDirectory(gConfig).Combine("lib");
        NPath tfm = lib.Directories().First();
        tfm.MoveFiles(lib, true);
        tfm.Delete();

        // Move System.Private.CoreLib from native to lib. This is where it lives when .Net is installed on a system
        Utils.RuntimeArtifactDirectory(gConfig).Combine("native").MoveFiles(lib, false, path => path.FileName.StartsWith("System.Private.CoreLib"));

        return Utils.RuntimeArtifactDirectory(gConfig);
    }

}
