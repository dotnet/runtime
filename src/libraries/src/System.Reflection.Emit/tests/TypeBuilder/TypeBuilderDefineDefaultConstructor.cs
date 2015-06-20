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
    public class TypeBuilderDefineDefaultConstructor
    {
        private const string DynamicAssemblyName = "TestDynamicAssembly";
        private const string DynamicModuleName = "TestDynamicModule";
        private const string DynamicBaseTypeName = "BaseTestDynamicType";
        private const string DynamicTypeName = "TestDynamicType";
        private const string DynamicBaseFieldName = "m_baseTestDynamicFieldA";
        private const string DynamicMethodName = "TestDynamicMethodA";

        public TypeBuilder RetriveTestTypeBuilder(string typeName, TypeAttributes typeAtt)
        {
            AssemblyName asmName = new AssemblyName();
            asmName.Name = DynamicAssemblyName;
            AssemblyBuilder asmBuilder = AssemblyBuilder.DefineDynamicAssembly(asmName, AssemblyBuilderAccess.Run);

            ModuleBuilder modBuilder = TestLibrary.Utilities.GetModuleBuilder(asmBuilder, DynamicModuleName);

            TypeBuilder typeBuilder = modBuilder.DefineType(typeName, typeAtt);

            return typeBuilder;
        }

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
            ExePosTest(TypeAttributes.Public | TypeAttributes.Class, MethodAttributes.Public, BindingFlags.Public | BindingFlags.Instance);
        }

        [Fact]
        public void PosTest2()
        {
            ExePosTest(TypeAttributes.Class | TypeAttributes.Public, MethodAttributes.Static, BindingFlags.Static | BindingFlags.NonPublic);
        }

        [Fact]
        public void PosTest3()
        {
            ExePosTest(TypeAttributes.Class | TypeAttributes.Public, MethodAttributes.Family, BindingFlags.Instance | BindingFlags.NonPublic);
        }

        [Fact]
        public void PosTest4()
        {
            ExePosTest(TypeAttributes.Class | TypeAttributes.Public, MethodAttributes.Assembly, BindingFlags.Instance | BindingFlags.NonPublic);
        }

        [Fact]
        public void PosTest5()
        {
            ExePosTest(TypeAttributes.Class | TypeAttributes.Public, MethodAttributes.Private, BindingFlags.Instance | BindingFlags.NonPublic);
        }

        [Fact]
        public void PosTest6()
        {
            ExePosTest(TypeAttributes.Class | TypeAttributes.Public, MethodAttributes.PrivateScope, BindingFlags.Instance | BindingFlags.NonPublic);
        }

        [Fact]
        public void PosTest7()
        {
            ExePosTest(TypeAttributes.Class | TypeAttributes.Public, MethodAttributes.FamORAssem, BindingFlags.Instance | BindingFlags.NonPublic);
        }

        [Fact]
        public void PosTest8()
        {
            ExePosTest(TypeAttributes.Class | TypeAttributes.Public, MethodAttributes.FamANDAssem, BindingFlags.Instance | BindingFlags.NonPublic);
        }

        [Fact]
        public void PosTest9()
        {
            ExePosTest(TypeAttributes.Class | TypeAttributes.Public, MethodAttributes.Final | MethodAttributes.Public, BindingFlags.Instance | BindingFlags.Public);
        }

        [Fact]
        public void PosTest10()
        {
            ExePosTest(TypeAttributes.Class | TypeAttributes.Public, MethodAttributes.Final | MethodAttributes.Family, BindingFlags.Instance | BindingFlags.NonPublic);
        }

        [Fact]
        public void PosTest11()
        {
            ExePosTest(TypeAttributes.Class | TypeAttributes.Public, MethodAttributes.SpecialName | MethodAttributes.Family, BindingFlags.Instance | BindingFlags.NonPublic);
        }

        [Fact]
        public void PosTest12()
        {
            ExePosTest(TypeAttributes.Class | TypeAttributes.Public, MethodAttributes.UnmanagedExport | MethodAttributes.Family, BindingFlags.Instance | BindingFlags.NonPublic);
        }

        [Fact]
        public void PosTest13()
        {
            ExePosTest(TypeAttributes.Class | TypeAttributes.Public, MethodAttributes.RTSpecialName | MethodAttributes.Family, BindingFlags.Instance | BindingFlags.NonPublic);
        }

        [Fact]
        public void PosTest14()
        {
            ExePosTest(TypeAttributes.Class | TypeAttributes.Public, MethodAttributes.HideBySig | MethodAttributes.Family, BindingFlags.Instance | BindingFlags.NonPublic);
        }

        private void ExePosTest(TypeAttributes typeAttr, MethodAttributes ctorAttr, BindingFlags ctorBindingAttr)
        {
            TypeBuilder testTypeBuilder;
            ConstructorBuilder ctorBuilder;
            ConstructorInfo actualCtor;

            testTypeBuilder = RetriveTestTypeBuilder(typeAttr);

            ctorBuilder = testTypeBuilder.DefineDefaultConstructor(ctorAttr);

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
            Assert.Throws<InvalidOperationException>(() => { testTypeBuilder.DefineDefaultConstructor(MethodAttributes.Public); });
        }

        [Fact]
        public void NegTest2()
        {
            TypeBuilder testTypeBuilder;

            testTypeBuilder = RetriveTestTypeBuilder("negtest2", TypeAttributes.Public |
                                                                             TypeAttributes.Interface |
                                                                             TypeAttributes.Abstract);

            testTypeBuilder.CreateTypeInfo().AsType();
            Assert.Throws<InvalidOperationException>(() => { testTypeBuilder.DefineDefaultConstructor(MethodAttributes.Public); });
        }

        [Fact]
        public void NegTest3()
        {
            TypeBuilder baseTestTypeBuilder, testTypeBuilder;
            ConstructorBuilder baseCtorBuilder;
            FieldBuilder baseFieldBuilderA;

            baseTestTypeBuilder = RetriveTestTypeBuilder(DynamicBaseTypeName,
                                                                             TypeAttributes.Public | TypeAttributes.Class);

            baseFieldBuilderA = baseTestTypeBuilder.DefineField(DynamicBaseFieldName,
                                                                                      typeof(int),
                                                                                      FieldAttributes.Family);

            baseCtorBuilder = baseTestTypeBuilder.DefineConstructor(MethodAttributes.Public,
                                                                                             CallingConventions.HasThis,
                                                                                             new Type[] { typeof(int) });

            ILGenerator baseCtorIL = baseCtorBuilder.GetILGenerator();

            baseCtorIL.Emit(OpCodes.Ldarg_0);
            baseCtorIL.Emit(OpCodes.Call, typeof(object).GetConstructor(new Type[0]));

            baseCtorIL.Emit(OpCodes.Ldarg_0);
            baseCtorIL.Emit(OpCodes.Ldarg_1);
            baseCtorIL.Emit(OpCodes.Stfld, baseFieldBuilderA);

            baseCtorIL.Emit(OpCodes.Ret);

            Type baseTestType = baseTestTypeBuilder.CreateTypeInfo().AsType();

            testTypeBuilder = RetriveTestTypeBuilder(TypeAttributes.Public | TypeAttributes.Class);
            testTypeBuilder.SetParent(baseTestType);
            Assert.Throws<NotSupportedException>(() => { testTypeBuilder.DefineDefaultConstructor(MethodAttributes.Public); });
        }

        [Fact]
        public void NegTest4()
        {
            TypeBuilder baseTestTypeBuilder, testTypeBuilder;
            ConstructorBuilder baseCtorBuilder;

            baseTestTypeBuilder = RetriveTestTypeBuilder(DynamicBaseTypeName,
                                                                             TypeAttributes.Public | TypeAttributes.Class);

            baseCtorBuilder = baseTestTypeBuilder.DefineConstructor(MethodAttributes.Private,
                                                                                             CallingConventions.HasThis,
                                                                                             new Type[] { typeof(int) });

            ILGenerator baseCtorIL = baseCtorBuilder.GetILGenerator();

            baseCtorIL.Emit(OpCodes.Ret);

            Type baseTestType = baseTestTypeBuilder.CreateTypeInfo().AsType();

            testTypeBuilder = RetriveTestTypeBuilder(TypeAttributes.Public | TypeAttributes.Class);
            testTypeBuilder.SetParent(baseTestType);
            Assert.Throws<NotSupportedException>(() => { testTypeBuilder.DefineDefaultConstructor(MethodAttributes.Public); });
        }

        [Fact]
        public void NegTest5()
        {
            TypeBuilder baseTestTypeBuilder, testTypeBuilder;
            ConstructorBuilder baseCtorBuilder;

            baseTestTypeBuilder = RetriveTestTypeBuilder(DynamicBaseTypeName,
                                                                             TypeAttributes.Public | TypeAttributes.Class);

            baseCtorBuilder = baseTestTypeBuilder.DefineConstructor(MethodAttributes.PrivateScope,
                                                                                             CallingConventions.HasThis,
                                                                                             new Type[] { typeof(int) });

            ILGenerator baseCtorIL = baseCtorBuilder.GetILGenerator();

            baseCtorIL.Emit(OpCodes.Ret);

            Type baseTestType = baseTestTypeBuilder.CreateTypeInfo().AsType();

            testTypeBuilder = RetriveTestTypeBuilder(TypeAttributes.Public | TypeAttributes.Class);
            testTypeBuilder.SetParent(baseTestType);
            Assert.Throws<NotSupportedException>(() => { testTypeBuilder.DefineDefaultConstructor(MethodAttributes.Public); });
        }
    }
}