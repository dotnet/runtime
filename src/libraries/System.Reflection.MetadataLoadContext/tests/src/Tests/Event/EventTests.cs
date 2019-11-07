// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using SampleMetadata;
using Xunit;

namespace System.Reflection.Tests
{
    public static partial class EventTests
    {
        [Fact]
        public static void EventTest1()
        {
            Type t = typeof(DerivedFromEventHolder1<int>).Project();
            Type rt = t;
            Type dt = t.BaseType;

            EventInfo e = t.GetEvent("MyEvent");
            string es = e.ToString();
            Assert.Equal(typeof(Action<>).Project().MakeGenericType(typeof(int).Project()), e.EventHandlerType);
            Assert.Equal(t.Module, e.Module);

            Assert.Equal(dt, e.DeclaringType);
            Assert.Equal(rt, e.ReflectedType);

            MethodInfo adder = e.AddMethod;
            Assert.Equal("add_MyEvent", adder.Name);
            Assert.Equal(dt, adder.DeclaringType);
            Assert.Equal(rt, adder.ReflectedType);

            MethodInfo remover = e.RemoveMethod;
            Assert.Equal("remove_MyEvent", remover.Name);
            Assert.Equal(dt, remover.DeclaringType);
            Assert.Equal(rt, remover.ReflectedType);

            Assert.Null(e.RaiseMethod);

            Assert.Equal(0, e.GetOtherMethods(nonPublic: true).Length);
            Assert.Equal(0, e.GetOtherMethods(nonPublic: false).Length);
        }
    }
}
