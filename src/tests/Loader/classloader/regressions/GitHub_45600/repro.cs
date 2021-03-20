// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

public abstract class A { }

public class B : A { }

public abstract class C { }

public abstract class C<CTParam> : C where CTParam : A { }

public class D : C<B> { }

public abstract class E
{
    internal E() { }

    internal abstract Type NamedObjectType { get; }
}

public class E<ETParam> : E
    where ETParam : A
{
    private readonly F<C<ETParam>> components =
        new F<C<ETParam>>();

    internal override Type NamedObjectType => typeof(ETParam);

    public void Register<ERegMethodParam>()
        where ERegMethodParam : C<ETParam>, new()
    {
        components.Register<ERegMethodParam>();
    }
}

public class F<FTParam> where FTParam : class
{
    private readonly HashSet<Type> componentTypes = new HashSet<Type>();

    private readonly Dictionary<Type, Func<FTParam>> componentFactories =
        new Dictionary<Type, Func<FTParam>>();

    public void Register<FRegMethodParamHaha>()   // F<C<B>>.Register<D>
        where FRegMethodParamHaha : class, FTParam, new()
    {
    }
}

public class G
{
    private readonly Dictionary<Type, E> subcontainersByNamedObjectType =
        new Dictionary<Type, E>();

    private readonly Dictionary<Type, E> subcontainersByRegisteredType =
        new Dictionary<Type, E>();

    public E<ETParam> RegisterNamedObjectType<ETParam>()
    where ETParam : A
    {
        return RegisterSubcontainer(new E<ETParam>());
    }

    public GRegMethodParam RegisterSubcontainer<GRegMethodParam>(GRegMethodParam subcontainer)
        where GRegMethodParam : E
    {
        subcontainersByNamedObjectType.Add(subcontainer.NamedObjectType, subcontainer);
        subcontainersByRegisteredType.Add(typeof(GRegMethodParam), subcontainer);
        return subcontainer;
    }

    internal void Register<GRegMethodParam1, GRegMethodParam2>()
        where GRegMethodParam1 : A
        where GRegMethodParam2 : C<GRegMethodParam1>, new()
    {
        GetSubcontainerFor<GRegMethodParam1>().Register<GRegMethodParam2>();  // E<B>.Reg<D>
    }

    public E<GGetSebMethodParam> GetSubcontainerFor<GGetSebMethodParam>()
        where GGetSebMethodParam : A
    {
        return (E<GGetSebMethodParam>)GetSubcontainerFor(typeof(GGetSebMethodParam));
    }

    public E GetSubcontainerFor(Type baseNamedObjectType)
    {
        return subcontainersByNamedObjectType[baseNamedObjectType];
    }
}

class Program
{
    static int Main(string[] args)
    {
        var contaner = new G();
        contaner.RegisterNamedObjectType<B>();
        contaner.Register<B, D>();
        return 100;
    }
}
