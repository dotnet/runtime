// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace BuildDriver;

public class GlobalConfig
{
    public required string Architecture;
    public required string Configuration;
    public required Verbosity VerbosityLevel;
}
