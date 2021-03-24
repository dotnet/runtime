// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

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

class Program
{
    static int Main()
    {
        new Concrete<Component>();

        return 100;
    }
}
