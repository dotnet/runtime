// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class TypeBuilderDefineConstructor1
    {
        private const string DynamicAssemblyName = "TestDynamicAssembly";
        private const string DynamicModuleName = "TestDynamicModule";
        private const string DynamicTypeName = "TestDynamicType";
        private const string DynamicFieldName = "m_testDynamicFieldA";
        private const string DynamicMethodName = "TestDynamicMethodA";

        public TypeBuilder RetriveTestTypeBuilder(TypeAttributes typeAtt)
        {
            AssemblyName asmName = new AssemblyName();
            asmName.Name = DynamicAssemblyName;
            AssemblyBuilder asmBuilder = AssemblyBuilder.DefineDynamicAssembly(asmName, AssemblyBuilderAccess.Run);
            ModuleBuilder modBuilder = TestLibrary.Utilities.GetModuleBuilder(asmBuilder, DynamicModuleName);
            TypeBuilder typeBuilder = modBuilder.DefineType(DynamicTypeName, typeAtt);
            return typeBuilder;
        }

        [Fact]
        public void PosTest1()
        {
            ExePosTest(TypeAttributes.Public | TypeAttributes.Class, MethodAttributes.Public, CallingConventions.HasThis, BindingFlags.Public | BindingFlags.Instance);
        }

        [Fact]
        public void PosTest2()
        {
            ExePosTest(TypeAttributes.Class | TypeAttributes.Public, MethodAttributes.Static, new Type[0], CallingConventions.Standard, BindingFlags.Static | BindingFlags.NonPublic);
        }

        [Fact]
        public void PosTest3()
        {
            ExePosTest(TypeAttributes.Class | TypeAttributes.Public, MethodAttributes.Family, CallingConventions.HasThis, BindingFlags.Instance | BindingFlags.NonPublic);
        }

        [Fact]
        public void PosTest4()
        {
            ExePosTest(TypeAttributes.Class | TypeAttributes.Public, MethodAttributes.Assembly, CallingConventions.HasThis, BindingFlags.Instance | BindingFlags.NonPublic);
        }

        [Fact]
        public void PosTest5()
        {
            ExePosTest(TypeAttributes.Class | TypeAttributes.Public, MethodAttributes.Private, CallingConventions.HasThis, BindingFlags.Instance | BindingFlags.NonPublic);
        }

        [Fact]
        public void PosTest6()
        {
            ExePosTest(TypeAttributes.Class | TypeAttributes.Public, MethodAttributes.PrivateScope, CallingConventions.HasThis, BindingFlags.Instance | BindingFlags.NonPublic);
        }

        [Fact]
        public void PosTest7()
        {
            ExePosTest(TypeAttributes.Class | TypeAttributes.Public, MethodAttributes.FamORAssem, CallingConventions.HasThis, BindingFlags.Instance | BindingFlags.NonPublic);
        }

        [Fact]
        public void PosTest8()
        {
            ExePosTest(TypeAttributes.Class | TypeAttributes.Public, MethodAttributes.FamANDAssem, CallingConventions.HasThis, BindingFlags.Instance | BindingFlags.NonPublic);
        }

        [Fact]
        public void PosTest9()
        {
            ExePosTest(TypeAttributes.Class | TypeAttributes.Public, MethodAttributes.Final | MethodAttributes.Public, CallingConventions.HasThis, BindingFlags.Instance | BindingFlags.Public);
        }

        [Fact]
        public void PosTest10()
        {
            ExePosTest(TypeAttributes.Class | TypeAttributes.Public, MethodAttributes.Final | MethodAttributes.Family, CallingConventions.HasThis, BindingFlags.Instance | BindingFlags.NonPublic);
        }

        [Fact]
        public void PosTest11()
        {
            ExePosTest(TypeAttributes.Class | TypeAttributes.Public, MethodAttributes.SpecialName | MethodAttributes.Family, CallingConventions.HasThis, BindingFlags.Instance | BindingFlags.NonPublic);
        }

        [Fact]
        public void PosTest12()
        {
            ExePosTest(TypeAttributes.Class | TypeAttributes.Public, MethodAttributes.UnmanagedExport | MethodAttributes.Family, CallingConventions.HasThis, BindingFlags.Instance | BindingFlags.NonPublic);
        }

        [Fact]
        public void PosTest13()
        {
            ExePosTest(TypeAttributes.Class | TypeAttributes.Public, MethodAttributes.RTSpecialName | MethodAttributes.Family, CallingConventions.HasThis, BindingFlags.Instance | BindingFlags.NonPublic);
        }

        [Fact]
        public void PosTest14()
        {
            ExePosTest(TypeAttributes.Class | TypeAttributes.Public, MethodAttributes.HideBySig | MethodAttributes.Family, CallingConventions.HasThis, BindingFlags.Instance | BindingFlags.NonPublic);
        }

        private void ExePosTest(TypeAttributes typeAttr, MethodAttributes ctorAttr, CallingConventions ctorCallingConv, BindingFlags ctorBindingAttr)
        {
            ExePosTest(typeAttr, ctorAttr, new Type[] { typeof(int), typeof(int) }, ctorCallingConv, ctorBindingAttr);
        }

        private void ExePosTest(TypeAttributes typeAttr, MethodAttributes ctorAttr, Type[] ctorParams, CallingConventions ctorCallingConv, BindingFlags ctorBindingAttr)
        {
            TypeBuilder testTypeBuilder;
            FieldBuilder fieldBuilderA, fieldBuilderB;
            ConstructorBuilder ctorBuilder;
            ILGenerator ctorIL;
            ConstructorInfo actualCtor;

            testTypeBuilder = RetriveTestTypeBuilder(typeAttr);

            fieldBuilderA = testTypeBuilder.DefineField(DynamicFieldName,
                                                                       typeof(int),
                                                                       FieldAttributes.Private);

            fieldBuilderB = testTypeBuilder.DefineField(DynamicFieldName,
                                                                       typeof(int),
                                                                       FieldAttributes.Private);

            ctorBuilder = testTypeBuilder.DefineConstructor(ctorAttr,
                                                                               ctorCallingConv,
                                                                               ctorParams);
            ctorIL = ctorBuilder.GetILGenerator();

            if (0 != ctorParams.Length)
            {
                //Calling base class constructor
                ctorIL.Emit(OpCodes.Ldarg_0);
                ctorIL.Emit(OpCodes.Call, typeof(object).GetConstructor(new Type[0]));

                ctorIL.Emit(OpCodes.Ldarg_0);
                ctorIL.Emit(OpCodes.Ldarg_1);
                ctorIL.Emit(OpCodes.Stfld, fieldBuilderA);

                ctorIL.Emit(OpCodes.Ldarg_0);
                ctorIL.Emit(OpCodes.Ldarg_2);
                ctorIL.Emit(OpCodes.Stfld, fieldBuilderB);
            }

            ctorIL.Emit(OpCodes.Ret);

            Type testType = testTypeBuilder.CreateTypeInfo().AsType();
            actualCtor = testType.GetConstructors(ctorBindingAttr).FirstOrDefault();
            Assert.NotNull(actualCtor);
        }


        [Fact]
        public void NegTest1()
        {
            TypeBuilder testTypeBuilder;

            testTypeBuilder = RetriveTestTypeBuilder(TypeAttributes.Public);
            testTypeBuilder.CreateTypeInfo().AsType();
            Assert.Throws<InvalidOperationException>(() =>
            {
                testTypeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.HasThis, new Type[] { typeof(int) });
            });
        }

        [Fact]
        public void NegTest2()
        {
            TypeBuilder testTypeBuilder;

            testTypeBuilder = RetriveTestTypeBuilder(TypeAttributes.Public |
                                                                             TypeAttributes.Interface |
                                                                             TypeAttributes.Abstract);
            testTypeBuilder.CreateTypeInfo().AsType();
            Assert.Throws<InvalidOperationException>(() =>
            {
                testTypeBuilder.DefineConstructor(MethodAttributes.Public,
                       CallingConventions.HasThis,
                       new Type[] { typeof(int) });
            });
        }
    }
}
