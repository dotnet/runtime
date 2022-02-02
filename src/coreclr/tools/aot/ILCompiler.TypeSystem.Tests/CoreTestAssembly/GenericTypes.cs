// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace GenericTypes
{
    /// <summary>
    /// Generic class to be used for testing.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class GenericClass<T>
    {
        /// <summary>
        /// Purpose is to manipulate a method involving a generic parameter in its return type.
        /// </summary>
        public abstract T Foo();
        /// <summary>
        /// Purpose is to manipulate a method involving a generic parameter in its parameter list.
        /// </summary>
        public void Bar(T a)
        {
        }

        ~GenericClass()
        { }
    }

    public class DerivedGenericClass<T> : GenericClass<T>
    {
        public override sealed T Foo()
        {
            return default(T);
        }
    }
    /// <summary>
    /// Generic class with multiple parameters to be used for testing.
    /// </summary>
    public class TwoParamGenericClass<T,U>
    {
        /// <summary>
        /// Purpose is to allow testing of the properties of non-generic methods on generic types
        /// </summary>
        public void NonGenericFunction()
        {
        }

        /// <summary>
        /// Purpose is to allow testing of the properties of generic methods on generic types
        /// </summary>
        public void GenericFunction<K, V>()
        {
        }
    }

    /// <summary>
    /// Non-generic type which has a generic method in it
    /// </summary>
    public class NonGenericClass
    {
        /// <summary>
        /// Purpose is to allow testing the properties of generic methods on nongeneric types
        /// </summary>
        /// <typeparam name="K"></typeparam>
        /// <typeparam name="V"></typeparam>
        public void GenericFunction<K, V>()
        {
        }
    }

    /// <summary>
    /// Generic structure with 3 fields all defined by type parameters
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct GenStruct<A,B,C>
    {
        A _a;
        B _b;
        C _c;
    }

    public class GenClass<A>
    {
#pragma warning disable 169
        A _a;
#pragma warning restore 169
    }

    public class GenDerivedClass<A,B> : GenClass<A>
    {
#pragma warning disable 169
        B _b;
#pragma warning restore 169
    }
}
