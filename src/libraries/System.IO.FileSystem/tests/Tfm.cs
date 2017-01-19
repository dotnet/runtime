// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;


public static class Tfm
{
    public const TargetFrameworkMonikers BelowNet462 = TargetFrameworkMonikers.Net45 | TargetFrameworkMonikers.Net451 | TargetFrameworkMonikers.Net452 | TargetFrameworkMonikers.Net46 | TargetFrameworkMonikers.Net461;
    public const TargetFrameworkMonikers BelowNet47 = BelowNet462 | TargetFrameworkMonikers.Net462 | TargetFrameworkMonikers.Net463;
    public const TargetFrameworkMonikers Core50 = TargetFrameworkMonikers.Netcore50 | TargetFrameworkMonikers.Netcore50aot;
}
