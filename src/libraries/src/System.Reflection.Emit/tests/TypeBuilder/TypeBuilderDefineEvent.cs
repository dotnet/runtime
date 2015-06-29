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
        public void PosTest1()
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
        public void NegTest1()
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
        public void NegTest2() { GeneralNegativeTest(null, EventAttributes.None, typeof(int), typeof(ArgumentNullException)); }

        [Fact]
        public void NegTest3() { GeneralNegativeTest("TestEvent", EventAttributes.None, null, typeof(ArgumentNullException)); }

        [Fact]
        public void NegTest4() { GeneralNegativeTest("", EventAttributes.None, null, typeof(ArgumentException)); }

        [Fact]
        public void NegTest5() { GeneralNegativeTest("\0Testing", EventAttributes.None, null, typeof(ArgumentException)); }

        [Fact]
        public void NegTest6() { GeneralNegativeTest("Testing", (EventAttributes)(-1), typeof(int), null); }

        [Fact]
        public void NegTest7() { GeneralNegativeTest("Testing", (EventAttributes)(0x1000), typeof(int), null); }


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

            if (expected != null)
            {
                Action test = () =>
                {
                    myType.DefineEvent(name, attrs, eventType);
                    Type t = myType.CreateTypeInfo().AsType();
                };

                Assert.Throws(expected, test);
            }
            else
            {
                Type t1 = myType.CreateTypeInfo().AsType();
            }
        }
    }
}
