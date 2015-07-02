// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using System.Reflection.Emit;
using TestLibrary;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class TypeBuilderDefineEvent
    {
        [Fact]
        public void TestDefineEvent()
        {
            AssemblyName myAsmName =
                new AssemblyName("TypeBuilderGetFieldTest");
            AssemblyBuilder myAssembly = AssemblyBuilder.DefineDynamicAssembly(
                myAsmName, AssemblyBuilderAccess.Run);
            ModuleBuilder myModule = TestLibrary.Utilities.GetModuleBuilder(myAssembly, "Module1");

            TypeBuilder myType = myModule.DefineType("Sample",
                TypeAttributes.Class | TypeAttributes.Public);

            string[] typeParamNames = { "T" };
            GenericTypeParameterBuilder[] typeParams =
                myType.DefineGenericParameters(typeParamNames);

            EventBuilder eb = myType.DefineEvent("TestEvent", EventAttributes.None, typeof(int));
            MethodBuilder addOnMethod = myType.DefineMethod("addOnMethod", MethodAttributes.Public);
            ILGenerator ilGen = addOnMethod.GetILGenerator();
            ilGen.Emit(OpCodes.Ret);
            MethodBuilder removeOnMethod = myType.DefineMethod("removeOnMethod", MethodAttributes.Public);
            ilGen = removeOnMethod.GetILGenerator();
            ilGen.Emit(OpCodes.Ret);
            eb.SetAddOnMethod(addOnMethod);
            eb.SetRemoveOnMethod(removeOnMethod);

            Type t = myType.CreateTypeInfo().AsType();

            EventInfo ei = t.GetEvent("TestEvent", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            Assert.NotNull(ei);
        }

        [Fact]
        public void TestThrowsExceptionForCreateTypeCalled()
        {
            AssemblyName myAsmName =
                new AssemblyName("TypeBuilderGetFieldTest");
            AssemblyBuilder myAssembly = AssemblyBuilder.DefineDynamicAssembly(
                myAsmName, AssemblyBuilderAccess.Run);
            ModuleBuilder myModule = TestLibrary.Utilities.GetModuleBuilder(myAssembly, "Module1");

            TypeBuilder myType = myModule.DefineType("Sample",
                TypeAttributes.Class | TypeAttributes.Public);

            string[] typeParamNames = { "T" };
            GenericTypeParameterBuilder[] typeParams =
                myType.DefineGenericParameters(typeParamNames);

            myType.DefineEvent("TestEvent", EventAttributes.None, typeof(int));
            Type t = myType.CreateTypeInfo().AsType();
            Assert.Throws<InvalidOperationException>(() => { myType.DefineEvent("TestEvent2", EventAttributes.None, typeof(int)); });
        }

        [Fact]
        public void TestThrowsExceptionForNullName() { GeneralNegativeTest(null, EventAttributes.None, typeof(int), typeof(ArgumentNullException)); }

        [Fact]
        public void TestThrowsExceptionForNullEventType() { GeneralNegativeTest("TestEvent", EventAttributes.None, null, typeof(ArgumentNullException)); }

        [Fact]
        public void TestThrowsExceptionForEmptyName() { GeneralNegativeTest("", EventAttributes.None, null, typeof(ArgumentException)); }

        [Fact]
        public void TestThrowsExceptionForNullTerminatedName() { GeneralNegativeTest("\0Testing", EventAttributes.None, null, typeof(ArgumentException)); }

        public void GeneralNegativeTest(string name, EventAttributes attrs, Type eventType, Type expected)
        {
            AssemblyName myAsmName =
                new AssemblyName("TypeBuilderGetFieldTest");
            AssemblyBuilder myAssembly = AssemblyBuilder.DefineDynamicAssembly(
                myAsmName, AssemblyBuilderAccess.Run);
            ModuleBuilder myModule = TestLibrary.Utilities.GetModuleBuilder(myAssembly, "Module1");


            TypeBuilder myType = myModule.DefineType("Sample",
                TypeAttributes.Class | TypeAttributes.Public);

            string[] typeParamNames = { "T" };
            GenericTypeParameterBuilder[] typeParams =
                myType.DefineGenericParameters(typeParamNames);

            Action test = () =>
            {
                myType.DefineEvent(name, attrs, eventType);
                Type t = myType.CreateTypeInfo().AsType();
            };

            Assert.Throws(expected, test);
        }
    }
}
