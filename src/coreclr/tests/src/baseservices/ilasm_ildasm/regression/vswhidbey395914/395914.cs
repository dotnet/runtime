// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

public class C {
    public void F<T>() where T : class, new() { }
    public void G<T>() where T : struct { }
}