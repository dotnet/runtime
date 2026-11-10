// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace GvmVariantInterface
{
    class GvmVariantBase
    {
    }

    class GvmVariantDerived : GvmVariantBase
    {
    }

    interface IInVariantGvm<in T>
    {
        string Func<U>(T t);
    }

    interface IOutVariantGvm<out T>
    {
        string Func<U>();
    }

    class ClassWithVariantGvms : IInVariantGvm<object>, IInVariantGvm<GvmVariantBase>, IOutVariantGvm<GvmVariantDerived>, IOutVariantGvm<GvmVariantBase>
    {
        string IInVariantGvm<object>.Func<U>(object t) => "CallOnObject";
        string IInVariantGvm<GvmVariantBase>.Func<U>(GvmVariantBase t) => "CallOnBase";
        string IOutVariantGvm<GvmVariantDerived>.Func<U>() => "CallOnDerived";
        string IOutVariantGvm<GvmVariantBase>.Func<U>() => "CallOnBase";
    }
}
