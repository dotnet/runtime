// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Unity.Cookbook.Platforms;

namespace Unity.Cookbook.Recipes;

public class EmbeddingTests : BaseTestRecipe
{
    public EmbeddingTests(DesktopPlatformSet platformSet)
    : base(platformSet)
    {
    }

    protected override string DisplayName => "Embedding API";

    protected override string BuildScriptTestArgumentValue => "embedding";
}
