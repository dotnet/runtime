// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using RecipeEngine.Api.Platforms;

namespace Unity.Cookbook;

public class JobConfiguration
{
    public required SystemType SystemType;
    public required Architecture Architecture;
    public required Configuration Configuration;

    public bool ExcludeFromPublishing;
}
