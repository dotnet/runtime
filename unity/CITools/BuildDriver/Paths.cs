// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using NiceIO;

namespace BuildDriver;

public class Paths
{
    public static NPath RepoRoot => typeof(Paths).Assembly.Location.ToNPath().RecursiveParents
        .First(dir => dir.Combine(".yamato").DirectoryExists());

    public static NPath UnityRoot => RepoRoot.Combine("unity");
    public static NPath UnityGC => UnityRoot.Combine("unitygc");

    public static NPath UnityEmbedApiTests => UnityRoot.Combine("embed_api_tests");

    public static NPath UnityEmbedHost => UnityRoot.Combine("unity-embed-host");

    public static NPath Artifacts => RepoRoot.Combine("artifacts");
}
