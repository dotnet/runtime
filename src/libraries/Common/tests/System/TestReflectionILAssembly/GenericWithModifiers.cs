// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

public class GenericWithModifiers
{
    // The 'const' modifier is not available in C#, so we generate IL with "IsConst" modopt.
    public void MyMethodWithGeneric(Tuple<int, bool /*modopt(IsConst)*/> t) { }

}
