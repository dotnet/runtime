// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using RecipeEngine.Api.Platforms;
using RecipeEngine.Platforms;
using RecipeEngine.Platforms.Loaders;
using RecipeEngine.Platforms.Models;

namespace Unity.Cookbook.Platforms;

public class DesktopPlatformSet : PlatformSetBase<SystemType>
{
    public Platform Windows { get; }
    public Platform Ubuntu { get; }
    public Platform MacOS { get; }


    public DesktopPlatformSet(IPlatformLoader loader)
        : base("platforms.json", loader)
    {
        Windows = new Platform(Platforms[SystemType.Windows].Agent, SystemType.Windows);
        Ubuntu = new Platform(Platforms[SystemType.Ubuntu].Agent, SystemType.Ubuntu);
        MacOS = new Platform(Platforms[SystemType.MacOS].Agent, SystemType.MacOS);
    }
}
