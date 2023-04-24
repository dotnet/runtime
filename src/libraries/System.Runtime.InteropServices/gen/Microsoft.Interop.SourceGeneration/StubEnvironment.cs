// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

using Microsoft.CodeAnalysis;

namespace Microsoft.Interop
{
    public sealed record StubEnvironment(
        Compilation Compilation,
        TargetFramework TargetFramework,
        Version TargetFrameworkVersion,
        bool ModuleSkipLocalsInit);
}
