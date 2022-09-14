// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Pipelines;

public static class CiFiltering
{
    // These fields can be used to restrict which build legs run in your PR.
    //
    // Set this to only dis/allow platforms that contain given substrings.
    // E.g. add "linux" if you only want to filter platforms containing "linux" in their name.
    // Then "dotnet build" this project and commit the YAML.
    //
    // First allowed platforms are filtered, then disallowed filter is applied.
    //
    // Example "Run all linux non-arm legs":
    //   - allowed  = [ "linux" ]
    //   - disallowed = [ "arm" ]
    public static readonly List<string> AllowedPlatforms = new() { };
    public static readonly List<string> DisallowedPlatforms = new() { };
}
