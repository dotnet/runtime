// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace MockInterop01.Impl
{
    public class IFooImplExp : IFoo
    {
        InteropEnum IFoo.IFooReadOnlyProp { get { return InteropEnum.White; } }

        ComplexStruct IFoo.MethodForStruct(ref UnionStruct p1, out InteropDeleWithStructArray p2) { p2 = null; return new ComplexStruct(); }

        string IFoo.this[string p, IFoo p2]
        {
            get { return p; }
            set { }
        }

        InteropDeleWithStructArray _FooEvent;
        event InteropDeleWithStructArray IFoo.IFooEvent
        {
            add { _FooEvent += value; }
            remove { _FooEvent -= value; }
        }
    }

    public struct IBarImplImp : IBar
    {
        public object DoSomething(params string[] ary)
        { return null; }
        
        public object Register(ref object p)
        { 
            return p; 
        }
        public void UnRegister(object o)
        {
        }

        public void LibFunc(decimal p1, DateTime p2)
        {
        }
    }   
      
    public class EventImpl : IEventEvent
    {
        public event EventDele01 OnEvent01;
        public event EventDele02 OnEvent02;
        public event EventDele03 OnEvent03;

        public void Fire1(IFoo p)
        {
            if (OnEvent01 != null) OnEvent01(p);
        }
        public void Fire2(InteropEnum p)
        {
            if (OnEvent02 != null) OnEvent02(p);
        }
        public void Fire3(ComplexStruct p)
        {
            if (OnEvent03 != null) OnEvent03(p);
        }
    }
}
