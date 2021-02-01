// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace VirtualFunctionOverride
{
    interface IIFaceWithGenericMethod
    {
        void GenMethod<T>();
    }

    class HasMethodInterfaceOverrideOfGenericMethod : IIFaceWithGenericMethod
    {
        void IIFaceWithGenericMethod.GenMethod<T>() { }
    }

    class SimpleGeneric<T>
    {
        public override string ToString()
        {
            return null;
        }
    }

    class BaseGenericWithOverload<T>
    {
        public virtual void MyMethod(string s) { }
        public virtual void MyMethod(T s) { }
    }

    class DerivedGenericWithOverload<U> : BaseGenericWithOverload<U>
    {
        public override void MyMethod(string s) { }
        public override void MyMethod(U s) { }
    }

    class ClassWithFinalizer
    {
        ~ClassWithFinalizer()
        {

        }
    }
}
