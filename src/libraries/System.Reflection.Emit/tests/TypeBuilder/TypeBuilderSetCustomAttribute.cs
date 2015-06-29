// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using TestLibrary;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Xunit;
using System.Linq;

namespace System.Reflection.Emit.Tests
{
    public class TBMyAttribute1 : Attribute
    {
        public TBMyAttribute1(int mc)
        {
            m_ctorType2 = mc;
        }

        public string Field12345;
        public int m_ctorType2;
    }

    public class TypeBuilderSetCustomAttribute
    {
        [Fact]
        public void PosTest1()
        {
            string name = "Assembly1";
            AssemblyName asmname = new AssemblyName();
            asmname.Name = name;

            AssemblyBuilder asmbuild = AssemblyBuilder.DefineDynamicAssembly(asmname, AssemblyBuilderAccess.Run);

            ModuleBuilder modbuild = TestLibrary.Utilities.GetModuleBuilder(asmbuild, "Module1");
            TypeBuilder tpbuild = modbuild.DefineType("C1");

            Type attrType = typeof(TBMyAttribute1);
            ConstructorInfo ci = attrType.GetConstructors()[0];
            FieldInfo fi = attrType.GetField("Field12345");


            CustomAttributeBuilder cab = new CustomAttributeBuilder(ci,
                                                                    new object[] { 4 },
                                                                    new FieldInfo[] { fi },
                                                                    new object[] { "hello" });
            tpbuild.SetCustomAttribute(cab);
            tpbuild.CreateTypeInfo().AsType();

            // VERIFY
            object[] attribs = tpbuild.GetCustomAttributes(false).Select(a => (object)a).ToArray();

            Assert.Equal(1, attribs.Length);
            TBMyAttribute1 obj = (TBMyAttribute1)attribs[0];

            Assert.Equal("hello", obj.Field12345);
            Assert.Equal(4, obj.m_ctorType2);
        }


        [Fact]
        public void NegTest1()
        {
            string name = "Assembly1";
            AssemblyName asmname = new AssemblyName();
            asmname.Name = name;
            AssemblyBuilder asmbuild = AssemblyBuilder.DefineDynamicAssembly(asmname, AssemblyBuilderAccess.Run);

            ModuleBuilder modbuild = TestLibrary.Utilities.GetModuleBuilder(asmbuild, "Module1");
            TypeBuilder tpbuild = modbuild.DefineType("C1", TypeAttributes.Public);

            Assert.Throws<ArgumentNullException>(() => { tpbuild.SetCustomAttribute(null); });
        }
    }
}
