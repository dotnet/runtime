// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace Microsoft.DotNet.CoreSetup.Test;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class PlatformSpecificTheoryAttribute : TheoryAttribute
{
    public PlatformSpecificTheoryAttribute(TestPlatforms platforms)
    {
        if (!PlatformSpecificFactAttribute.TestPlatformApplies(platforms))
        {
            base.Skip = "Test only runs on platform(s): " + platforms;
        }
    }
}