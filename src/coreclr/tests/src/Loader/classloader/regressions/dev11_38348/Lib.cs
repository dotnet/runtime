// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

public class Base<T>
{
}

public class Child<T> : Base<T>
{
}

public class VarType
{
    static public void foo<T>()
    {
        Console.WriteLine(typeof(T).ToString());
    }
}
