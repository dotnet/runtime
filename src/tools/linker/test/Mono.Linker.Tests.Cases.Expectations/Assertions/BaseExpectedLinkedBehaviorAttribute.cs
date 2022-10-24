// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;

namespace Mono.Linker.Tests.Cases.Expectations.Assertions
{
    /// <summary>
    /// Base attribute for attributes that mark up the expected behavior of the linker on a member
    /// </summary>
    [Conditional("INCLUDE_EXPECTATIONS")]
    public abstract class BaseExpectedLinkedBehaviorAttribute : Attribute
    {
    }
}
