// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;

public class C {
    public void F<T>() where T : class, new() { }
    public void G<T>() where T : struct { }
}
