// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Xunit;
using TestLibrary;

public abstract class AComponent { }
public class Component : AComponent { }

public abstract class Abstract
{
    public abstract IReadOnlyList<AComponent> New { get; }
}

public sealed class Concrete<T> : Abstract
    where T : AComponent
{
    public override IReadOnlyList<T> New => throw null;
}

public class Program
{
    [ActiveIssue("needs triage", typeof(PlatformDetection), nameof(PlatformDetection.IsSimulator))]
    [Fact]
    public static void TestEntryPoint()
    {
        new Concrete<Component>();
    }
}
