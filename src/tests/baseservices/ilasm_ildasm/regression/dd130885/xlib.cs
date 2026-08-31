// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
public struct myDateTime : IEquatable<myDateTime>
{
    public UInt64 dateData;
    public override string ToString()
    {
         return "myDateTime";
    }
    public bool Equals(myDateTime d)
    {
         return dateData == d.dateData;
    }
    public void InstanceMethod()
    {
        Console.WriteLine("InstanceMethod");
    }
    public void GenericMethod<T>()
    {
        Console.WriteLine(typeof(T));
    }
}
