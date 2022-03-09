// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace VirtualStaticInterfaceMethods
{
    interface ISimple
    {
        static abstract int WhichMethod();
    }

    class Simple : ISimple
    {
        public static int WhichMethod() => throw null;
    }

    interface IVariant<in T>
    {
        static abstract string WhichMethod(T param);
    }

    class SimpleVariant : IVariant<Base>
    {
        public static string WhichMethod(Base b) => throw null;
    }

    class SimpleVariantTwice : IVariant<Base>, IVariant<Mid>
    {
        public static string WhichMethod(Base b) => throw null;
        public static string WhichMethod(Mid b) => throw null;
    }

    class VariantWithInheritanceBase : IVariant<Mid>
    {
        public static string WhichMethod(Mid b) => throw null;
    }

    class VariantWithInheritanceDerived : VariantWithInheritanceBase, IVariant<Base>
    {
        public static string WhichMethod(Base b) => throw null;
    }

    class GenericVariantWithInheritanceBase<T> : IVariant<T>
    {
        public static string WhichMethod(T b) => throw null;
    }

    class GenericVariantWithInheritanceDerived<T> : GenericVariantWithInheritanceBase<T>, IVariant<T>
    {
        public static new string WhichMethod(T b) => throw null;
    }

    class GenericVariantWithHiddenBase : IVariant<Mid>
    {
        public static string WhichMethod(Mid b) => throw null;
    }

    class GenericVariantWithHiddenDerived<T> : GenericVariantWithHiddenBase, IVariant<T>
    {
        public static string WhichMethod(T b) => throw null;
    }

    class Base { }
    class Mid : Base { }
    class Derived : Mid { }
}
