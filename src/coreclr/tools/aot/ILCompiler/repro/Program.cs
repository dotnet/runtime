// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;

class Program
{
    [method: DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(DerivedAttributeWithGetter))]
    [method: DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(ClassWithDerivedAttr))]
    static void Main()
    {
        DerivedAttributeWithGetter derivedAttributeWithGetter = new DerivedAttributeWithGetter();
        Console.WriteLine("Hello world " + derivedAttributeWithGetter.P);
        object[] attributes = typeof(ClassWithDerivedAttr).GetCustomAttributes(true);

        if (attributes.Length > 0 && attributes[0] is DerivedAttributeWithGetter attr)
        {
            Console.WriteLine($"attribute found with value {attr.P}");
        }
        else
        {
            Console.WriteLine("No Attributes I guess" + derivedAttributeWithGetter.P);
        }
    }
}

public class BaseAttributeWithGetterSetter : Attribute
{
    protected int _p;

    public virtual int P
    {
        get => _p;
        set
        {
            _p = value;
        }
    }
}

public class DerivedAttributeWithGetter : BaseAttributeWithGetterSetter
{
    public override int P
    {
        get => _p;
    }
}

[DerivedAttributeWithGetter(P = 2)]
public class ClassWithDerivedAttr
{ }
