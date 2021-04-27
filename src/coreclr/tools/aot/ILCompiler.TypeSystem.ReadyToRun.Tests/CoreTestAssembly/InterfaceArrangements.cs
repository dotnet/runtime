// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

namespace InterfaceArrangements
{
    interface I1 { }

    interface I2 : I1 { }
    
    interface IGen1<T> { }

    class NoInterfaces { }

    class OneInterface : I1 { }

    class Base<T> : IGen1<T>, I1 { }

    class Mid<U,V> : Base<U>, IGen1<V> { }

    class DerivedFromMid : Mid<string, string>, IGen1<string> { }

    interface IFoo<out U>
    {
        void IMethod();
    }

    class Foo : IFoo<string>, IFoo<int>
    {
        public virtual void IMethod() { }
    }

    class DerivedFromFoo : Foo, IFoo<string>, IFoo<int>
    {
        void IFoo<string>.IMethod() { }
    }

    class SuperDerivedFromFoo : DerivedFromFoo, IFoo<string>, IFoo<int>
    {
        void IFoo<int>.IMethod() { }
    }
}
