// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using System.Reflection.Emit;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class TypeBuilderDefineProperty1
    {
        private static Type[] s_emptyTypes = new Type[0];

        [Fact]
        public void PosTest1()
        {
            AssemblyName an = new AssemblyName();
            an.Name = "DynamicRandomAssembly";
            AssemblyBuilder ab = AssemblyBuilder.DefineDynamicAssembly(an, AssemblyBuilderAccess.Run);

            ModuleBuilder mb = TestLibrary.Utilities.GetModuleBuilder(ab, "Module1");

            TypeBuilder tb = mb.DefineType("DynamicRandomClass", TypeAttributes.Public);

            Type[] parameterTypes = { typeof(int), typeof(double) };

            PropertyBuilder pb = tb.DefineProperty(
                "propertyname",
                PropertyAttributes.None,
                typeof(int),
                parameterTypes);


            Assert.Equal("propertyname", pb.Name);
            Assert.Equal(PropertyAttributes.None, pb.Attributes);
            Assert.True(pb.PropertyType.Equals(typeof(int)));
        }

        public class TBBaseClass1
        {
            public int Property { get { return 10; } }
        }

        [Fact]
        public void PosTest2()
        {
            AssemblyName an = new AssemblyName();
            an.Name = "Assembly1";
            AssemblyBuilder ab = AssemblyBuilder.DefineDynamicAssembly(an, AssemblyBuilderAccess.Run);

            ModuleBuilder mb = TestLibrary.Utilities.GetModuleBuilder(ab, "Module1");

            TypeBuilder tb = mb.DefineType("DerivedClass", TypeAttributes.Public | TypeAttributes.AutoClass | TypeAttributes.AnsiClass | TypeAttributes.BeforeFieldInit, typeof(TBBaseClass1));

            PropertyBuilder pb = tb.DefineProperty("Property", PropertyAttributes.None, CallingConventions.HasThis | CallingConventions.Standard, typeof(int), s_emptyTypes);

            MethodAttributes methodAttr = MethodAttributes.SpecialName | MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.ReuseSlot;
            CallingConventions conventions = CallingConventions.Standard | CallingConventions.HasThis;

            MethodBuilder getP = tb.DefineMethod("get_Property", methodAttr, conventions, typeof(int), s_emptyTypes);
            ILGenerator il = getP.GetILGenerator();
            il.Emit(OpCodes.Ldc_I4, 5);
            il.Emit(OpCodes.Ret);
            pb.SetGetMethod(getP);

            Type type = tb.CreateTypeInfo().AsType();
            PropertyInfo pi = type.GetProperty("Property"); //it shouldn't throw AmbiguousMatchException
            object obj = Activator.CreateInstance(type);
            int retValue = (int)type.GetProperty("Property").GetGetMethod().Invoke(obj, null);
            Assert.Equal(5, retValue);
        }

        [Fact]
        public void NegTest1()
        {
            AssemblyName an = new AssemblyName();
            an.Name = "DynamicRandomAssembly";
            AssemblyBuilder ab = AssemblyBuilder.DefineDynamicAssembly(an, AssemblyBuilderAccess.Run);

            ModuleBuilder mb = TestLibrary.Utilities.GetModuleBuilder(ab, "Module1");

            TypeBuilder tb = mb.DefineType("DynamicRandomClass", TypeAttributes.Public);

            Type[] parameterTypes = { typeof(int), typeof(double) };

            Assert.Throws<ArgumentException>(() => { PropertyBuilder pb = tb.DefineProperty("", PropertyAttributes.None, typeof(int), parameterTypes); });
        }

        [Fact]
        public void NegTest2()
        {
            AssemblyName an = new AssemblyName();
            an.Name = "DynamicRandomAssembly";
            AssemblyBuilder ab = AssemblyBuilder.DefineDynamicAssembly(an, AssemblyBuilderAccess.Run);

            ModuleBuilder mb = TestLibrary.Utilities.GetModuleBuilder(ab, "Module1");

            TypeBuilder tb = mb.DefineType("DynamicRandomClass", TypeAttributes.Public);

            Type[] parameterTypes = { typeof(int), typeof(double) };

            string propertyname = null;

            Assert.Throws<ArgumentNullException>(() => { PropertyBuilder pb = tb.DefineProperty(propertyname, PropertyAttributes.None, typeof(int), parameterTypes); });
        }

        [Fact]
        public void NegTest3()
        {
            AssemblyName an = new AssemblyName();
            an.Name = "DynamicRandomAssembly";
            AssemblyBuilder ab = AssemblyBuilder.DefineDynamicAssembly(an, AssemblyBuilderAccess.Run);

            ModuleBuilder mb = TestLibrary.Utilities.GetModuleBuilder(ab, "Module1");

            TypeBuilder tb = mb.DefineType("DynamicRandomClass", TypeAttributes.Public);

            Type[] parameterTypes = { typeof(int), typeof(double) };

            Type type = tb.CreateTypeInfo().AsType();

            Assert.Throws<InvalidOperationException>(() => { PropertyBuilder pb = tb.DefineProperty("propertyname", PropertyAttributes.None, typeof(int), parameterTypes); });
        }
    }
}