// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace System.Reflection.Emit.ILGeneration.Tests
{
    public class SetCustomAttributeTests
    {
        [Fact]
        public void TestSetCustomAttribute1()
        {
            string name = "Assembly1";
            AssemblyName asmname = new AssemblyName();
            asmname.Name = name;

            AssemblyBuilder asmbuild = AssemblyBuilder.DefineDynamicAssembly(asmname, AssemblyBuilderAccess.Run);
            ModuleBuilder modbuild = TestLibrary.Utilities.GetModuleBuilder(asmbuild, "Module1");

            TypeBuilder tpbuild = modbuild.DefineType("C1");
            MethodBuilder methbuild = tpbuild.DefineMethod("method1", MethodAttributes.Public | MethodAttributes.Static, typeof(void), new Type[] { typeof(int) });
            ParameterBuilder parambuild = methbuild.DefineParameter(1, ParameterAttributes.HasDefault, "testParam");
            ILGenerator ilgen = methbuild.GetILGenerator();
            ilgen.Emit(OpCodes.Ret);

            Type attrType = typeof(MBMyAttribute3);
            ConstructorInfo ci = attrType.GetConstructors()[0];
            FieldInfo fi = attrType.GetField("Field12345");


            CustomAttributeBuilder cab = new CustomAttributeBuilder(ci,
                                                                    new object[] { 4 },
                                                                    new FieldInfo[] { fi },
                                                                    new object[] { "hello" });
            parambuild.SetCustomAttribute(cab);
            Type tp = tpbuild.CreateTypeInfo().AsType();
            MethodInfo md = tp.GetMethod("method1");
            ParameterInfo pi = md.GetParameters()[0];
            // VERIFY
            object[] attribs = pi.GetCustomAttributes(false).Select(a => (object)a).ToArray();

            Assert.Equal(1, attribs.Length);
            MBMyAttribute3 obj = (MBMyAttribute3)attribs[0];

            Assert.Equal("hello", obj.Field12345);
            Assert.Equal(4, obj.m_ctorType2);
        }

        [Fact]
        public void TestSetCustomAttribute2()
        {
            string name = "Assembly1";
            AssemblyName asmname = new AssemblyName();
            asmname.Name = name;

            AssemblyBuilder asmbuild = AssemblyBuilder.DefineDynamicAssembly(asmname, AssemblyBuilderAccess.Run);
            ModuleBuilder modbuild = TestLibrary.Utilities.GetModuleBuilder(asmbuild, "Module1");

            TypeBuilder tpbuild = modbuild.DefineType("C1");
            MethodBuilder methbuild = tpbuild.DefineMethod("method1", MethodAttributes.Public | MethodAttributes.Static, typeof(void), new Type[] { typeof(int) });
            ParameterBuilder parambuild = methbuild.DefineParameter(1, ParameterAttributes.HasDefault, "testParam");
            ILGenerator ilgen = methbuild.GetILGenerator();
            ilgen.Emit(OpCodes.Ret);

            parambuild.SetCustomAttribute(typeof(MBMyAttribute3).GetConstructor(new Type[] { typeof(bool)}), new byte[] { 01,00,01,00,00});
            Type tp = tpbuild.CreateTypeInfo().AsType();
            MethodInfo md = tp.GetMethod("method1");
            ParameterInfo pi = md.GetParameters()[0];
            // VERIFY
            object[] attribs = pi.GetCustomAttributes(false).Select(a => (object)a).ToArray();

            Assert.Equal(1, attribs.Length);
            MBMyAttribute3 obj = (MBMyAttribute3)attribs[0];

            Assert.True(obj.booleanValue);
        }

        [Fact]
        public void TestExceptionsThrownOnNull()
        {
            string name = "Assembly1";
            AssemblyName asmname = new AssemblyName();
            asmname.Name = name;

            AssemblyBuilder asmbuild = AssemblyBuilder.DefineDynamicAssembly(asmname, AssemblyBuilderAccess.Run);
            ModuleBuilder modbuild = TestLibrary.Utilities.GetModuleBuilder(asmbuild, "Module1");

            TypeBuilder tpbuild = modbuild.DefineType("C1");
            MethodBuilder methbuild = tpbuild.DefineMethod("method1", MethodAttributes.Public | MethodAttributes.Static, typeof(void), new Type[] { typeof(int) });
            ParameterBuilder parambuild = methbuild.DefineParameter(1, ParameterAttributes.HasDefault, "testParam");
            ILGenerator ilgen = methbuild.GetILGenerator();
            ilgen.Emit(OpCodes.Ret);

            Assert.Throws<ArgumentNullException>(() => parambuild.SetCustomAttribute(null, new byte[] { }));
            Assert.Throws<ArgumentNullException>(() => parambuild.SetCustomAttribute(typeof(MBMyAttribute3).GetConstructor(new Type[] { typeof(bool) }), null));
            Assert.Throws<ArgumentNullException>(() => parambuild.SetCustomAttribute(null));
        }
    }

    public class MBMyAttribute3 : Attribute
    {
        public MBMyAttribute3(int mc)
        {
            m_ctorType2 = mc;
        }

        public MBMyAttribute3(bool b)
        {
            booleanValue = b;
        }

        public bool booleanValue;
        public string Field12345;
        public int m_ctorType2;
    }
}
