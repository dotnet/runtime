// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using RecipeEngine.Api.Platforms;

namespace Unity.Cookbook;

public class JobConfiguration
{
    public required SystemType SystemType;
    public required Architecture Architecture;
    public required Configuration Configuration;

    /// <summary>
    /// Excludes configuration from automatic publishing of artifacts to stevedore.
    /// E.g. if we want to do postprocessing of configuration artifacts in another job.
    /// </summary>
    public bool ExcludeFromPublishing;

    /// <summary>
    /// Excludes configuration from being tested as a part of "All CoreCLR Tests" job.
    /// </summary>
    public bool ExcludeFromTesting;
}
