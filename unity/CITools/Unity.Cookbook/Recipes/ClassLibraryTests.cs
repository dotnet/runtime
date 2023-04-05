// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Unity.Cookbook.Platforms;

namespace Unity.Cookbook.Recipes;

public class ClassLibraryTests : BaseTestRecipe
{
    public ClassLibraryTests(DesktopPlatformSet platformSet)
        : base(platformSet)
    {
    }

    protected override string DisplayName => "Class Library";

    protected override string BuildScriptTestArgumentValue => "classlibs";
}
