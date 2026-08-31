// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// 

using System;
using System.Reflection;
using System.Collections;
using System.Runtime.CompilerServices;

[assembly: SingleAttribute<int>()]
[assembly: SingleAttribute<bool>()]

[assembly: MultiAttribute<int>()]
[assembly: MultiAttribute<int>(1)]
[assembly: MultiAttribute<int>(Value = 2)]
[assembly: MultiAttribute<bool>()]
[assembly: MultiAttribute<bool>(true)]

[module: SingleAttribute<long>()]

[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Property, AllowMultiple = false)]
public class SingleAttribute<T> : Attribute
{

}

[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Property, AllowMultiple = true)]
public class MultiAttribute<T> : Attribute
{
    public T Value { get; set; }
    
    public MultiAttribute()
    {
    }
    
    public MultiAttribute(T value)
    {
        Value = value;
    }
}

public enum MyEnum
{
    Ctor,
    Property
}

[SingleAttribute<int>()]
[SingleAttribute<bool>()]
[MultiAttribute<int>()]
[MultiAttribute<int>(1)]
[MultiAttribute<int>(Value = 2)]
[MultiAttribute<bool>()]
[MultiAttribute<bool>(true)]
[MultiAttribute<bool>(Value = true)]
[MultiAttribute<bool?>()]
[MultiAttribute<string>("Ctor")]
[MultiAttribute<string>(Value = "Property")]
[MultiAttribute<Type>(typeof(Class))]
[MultiAttribute<Type>(Value = typeof(Class.Derive))]
[MultiAttribute<MyEnum>(MyEnum.Ctor)]
[MultiAttribute<MyEnum>(Value = MyEnum.Property)]
public class Class
{
    public class Derive : Class
    {

    }

    [SingleAttribute<int>()]
    [SingleAttribute<bool>()]
    [MultiAttribute<int>()]
    [MultiAttribute<int>(1)]
    [MultiAttribute<int>(Value = 2)]
    [MultiAttribute<bool>()]
    [MultiAttribute<bool>(true)]
    public int Property { get; set; }
}
