// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Reflection;
using System.Reflection.Emit;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class EvBMyAttribute2 : Attribute
    {
    }

    public class EventBuilderSetCustomAttribute2
    {
        public delegate void TestEventHandler(object sender, object arg);

        private TypeBuilder TypeBuilder
        {
            get
            {
                if (null == _typeBuilder)
                {
                    AssemblyBuilder assembly = AssemblyBuilder.DefineDynamicAssembly(
                        new AssemblyName("EventBuilderAddOtherMethod_Assembly"), AssemblyBuilderAccess.Run);
                    ModuleBuilder module = TestLibrary.Utilities.GetModuleBuilder(assembly, "EventBuilderAddOtherMethod_Module");
                    _typeBuilder = module.DefineType("EventBuilderAddOtherMethod_Type", TypeAttributes.Abstract);
                }

                return _typeBuilder;
            }
        }

        private TypeBuilder _typeBuilder;

        [Fact]
        public void TestSetCustomAttribute()
        {
            EventBuilder ev = TypeBuilder.DefineEvent("Event_PosTest1", EventAttributes.None, typeof(TestEventHandler));
            ConstructorInfo con = typeof(EvBMyAttribute2).GetConstructor(new Type[] { });
            CustomAttributeBuilder attribute = new CustomAttributeBuilder(con, new object[] { });

            ev.SetCustomAttribute(attribute);
        }

        [Fact]
        public void TestThrowsExceptionOnNullBuilder()
        {
            EventBuilder ev = TypeBuilder.DefineEvent("Event_NegTest1", EventAttributes.None, typeof(TestEventHandler));
            Assert.Throws<ArgumentNullException>(() => { ev.SetCustomAttribute(null); });
        }

        [Fact]
        public void TestThrowsExceptionOnCreateTypeCalled()
        {
            EventBuilder ev = TypeBuilder.DefineEvent("Event_NegTest2", EventAttributes.None, typeof(TestEventHandler));
            ConstructorInfo con = typeof(EvBMyAttribute2).GetConstructor(new Type[] { });
            CustomAttributeBuilder attribute = new CustomAttributeBuilder(con, new object[] { });
            TypeBuilder.CreateTypeInfo().AsType();

            Assert.Throws<InvalidOperationException>(() => { ev.SetCustomAttribute(attribute); });
        }
    }
}
