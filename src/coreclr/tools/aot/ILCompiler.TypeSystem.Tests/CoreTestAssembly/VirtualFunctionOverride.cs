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

    unsafe class FunctionPointerOverloadBase
    {
        // Do not reorder these, the test assumes this order
        public virtual Type Method(delegate* unmanaged[Cdecl]<void> p) => typeof(delegate* unmanaged[Cdecl]<void>);
        public virtual Type Method(delegate* unmanaged[Stdcall]<void> p) => typeof(delegate* unmanaged[Stdcall]<void>);
        public virtual Type Method(delegate* unmanaged[Stdcall, SuppressGCTransition]<void> p) => typeof(delegate* unmanaged[Stdcall, SuppressGCTransition]<void>);
        public virtual Type Method(delegate*<void> p) => typeof(delegate*<void>);
    }

    unsafe class FunctionPointerOverloadDerived : FunctionPointerOverloadBase
    {
        // Do not reorder these, the test assumes this order
        public override Type Method(delegate* unmanaged[Cdecl]<void> p) => null;
        public override Type Method(delegate* unmanaged[Stdcall]<void> p) => null;
        public override Type Method(delegate* unmanaged[Stdcall, SuppressGCTransition]<void> p) => null;
        public override Type Method(delegate*<void> p) => null;
    }
}
