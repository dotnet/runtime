// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

public class A<T>
{
    ~A()
    {
        System.Console.WriteLine("Class A finalizer called");
    }
}
