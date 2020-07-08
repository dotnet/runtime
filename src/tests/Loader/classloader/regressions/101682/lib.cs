// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

public class Foo<A>
{
    public virtual void FV<B>(B x) {
        Console.WriteLine("Foo<{0}>.FV<{1}>({2})", typeof(A), typeof(B), x);
    }
    public virtual void FV<B,C>(B x) {
        Console.WriteLine("Foo<{0}>.FV<{1},{2}>({3})", typeof(A), typeof(B), typeof(C), x);
    }
}
