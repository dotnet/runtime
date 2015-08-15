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

        public TypeBuilder RetrieveTestTypeBuilder(string typeName, TypeAttributes typeAtt)
        {
            AssemblyName asmName = new AssemblyName();
            asmName.Name = DynamicAssemblyName;
            AssemblyBuilder asmBuilder = AssemblyBuilder.DefineDynamicAssembly(asmName, AssemblyBuilderAccess.Run);

            ModuleBuilder modBuilder = TestLibrary.Utilities.GetModuleBuilder(asmBuilder, DynamicModuleName);

            TypeBuilder typeBuilder = modBuilder.DefineType(typeName, typeAtt);

            return typeBuilder;
        }

        public TypeBuilder RetrieveTestTypeBuilder(TypeAttributes typeAtt)
        {
            AssemblyName asmName = new AssemblyName();
            asmName.Name = DynamicAssemblyName;
            AssemblyBuilder asmBuilder = AssemblyBuilder.DefineDynamicAssembly(asmName, AssemblyBuilderAccess.Run);

            ModuleBuilder modBuilder = TestLibrary.Utilities.GetModuleBuilder(asmBuilder, DynamicModuleName);

            TypeBuilder typeBuilder = modBuilder.DefineType(DynamicTypeName, typeAtt);

            return typeBuilder;
        }

        [Fact]
        public void TestDefaultConstructor1()
        {
            ExePosTest(TypeAttributes.Public | TypeAttributes.Class, MethodAttributes.Public, BindingFlags.Public | BindingFlags.Instance);
        }

        [Fact]
        public void TestDefaultConstructor2()
        {
            ExePosTest(TypeAttributes.Class | TypeAttributes.Public, MethodAttributes.Static, BindingFlags.Static | BindingFlags.NonPublic);
        }

        [Fact]
        public void TestDefaultConstructor3()
        {
            ExePosTest(TypeAttributes.Class | TypeAttributes.Public, MethodAttributes.Family, BindingFlags.Instance | BindingFlags.NonPublic);
        }

        [Fact]
        public void TestDefaultConstructor4()
        {
            ExePosTest(TypeAttributes.Class | TypeAttributes.Public, MethodAttributes.Assembly, BindingFlags.Instance | BindingFlags.NonPublic);
        }

        [Fact]
        public void TestDefaultConstructor5()
        {
            ExePosTest(TypeAttributes.Class | TypeAttributes.Public, MethodAttributes.Private, BindingFlags.Instance | BindingFlags.NonPublic);
        }

        [Fact]
        public void TestDefaultConstructor6()
        {
            ExePosTest(TypeAttributes.Class | TypeAttributes.Public, MethodAttributes.PrivateScope, BindingFlags.Instance | BindingFlags.NonPublic);
        }

        [Fact]
        public void TestDefaultConstructor7()
        {
            ExePosTest(TypeAttributes.Class | TypeAttributes.Public, MethodAttributes.FamORAssem, BindingFlags.Instance | BindingFlags.NonPublic);
        }

        [Fact]
        public void TestDefaultConstructor8()
        {
            ExePosTest(TypeAttributes.Class | TypeAttributes.Public, MethodAttributes.FamANDAssem, BindingFlags.Instance | BindingFlags.NonPublic);
        }

        [Fact]
        public void TestDefaultConstructor9()
        {
            ExePosTest(TypeAttributes.Class | TypeAttributes.Public, MethodAttributes.Final | MethodAttributes.Public, BindingFlags.Instance | BindingFlags.Public);
        }

        [Fact]
        public void TestDefaultConstructor10()
        {
            ExePosTest(TypeAttributes.Class | TypeAttributes.Public, MethodAttributes.Final | MethodAttributes.Family, BindingFlags.Instance | BindingFlags.NonPublic);
        }

        [Fact]
        public void TestDefaultConstructor11()
        {
            ExePosTest(TypeAttributes.Class | TypeAttributes.Public, MethodAttributes.SpecialName | MethodAttributes.Family, BindingFlags.Instance | BindingFlags.NonPublic);
        }

        [Fact]
        public void TestDefaultConstructor12()
        {
            ExePosTest(TypeAttributes.Class | TypeAttributes.Public, MethodAttributes.UnmanagedExport | MethodAttributes.Family, BindingFlags.Instance | BindingFlags.NonPublic);
        }

        [Fact]
        public void TestDefaultConstructor13()
        {
            ExePosTest(TypeAttributes.Class | TypeAttributes.Public, MethodAttributes.RTSpecialName | MethodAttributes.Family, BindingFlags.Instance | BindingFlags.NonPublic);
        }

        [Fact]
        public void TestDefaultConstructor14()
        {
            ExePosTest(TypeAttributes.Class | TypeAttributes.Public, MethodAttributes.HideBySig | MethodAttributes.Family, BindingFlags.Instance | BindingFlags.NonPublic);
        }

        private void ExePosTest(TypeAttributes typeAttr, MethodAttributes ctorAttr, BindingFlags ctorBindingAttr)
        {
            TypeBuilder testTypeBuilder;
            ConstructorBuilder ctorBuilder;
            ConstructorInfo actualCtor;

            testTypeBuilder = RetrieveTestTypeBuilder(typeAttr);

            ctorBuilder = testTypeBuilder.DefineDefaultConstructor(ctorAttr);

            Type testType = testTypeBuilder.CreateTypeInfo().AsType();
            actualCtor = testType.GetConstructors(ctorBindingAttr).FirstOrDefault();

            Assert.NotNull(actualCtor);
        }


        [Fact]
        public void TestThrowsExceptionForCreatetypeCalled()
        {
            TypeBuilder testTypeBuilder;

            testTypeBuilder = RetrieveTestTypeBuilder(TypeAttributes.Public);
            testTypeBuilder.CreateTypeInfo().AsType();
            Assert.Throws<InvalidOperationException>(() => { testTypeBuilder.DefineDefaultConstructor(MethodAttributes.Public); });
        }

        [Fact]
        public void TestThrowsExceptionForInterface()
        {
            TypeBuilder testTypeBuilder;

            testTypeBuilder = RetrieveTestTypeBuilder("negtest2", TypeAttributes.Public |
                                                                             TypeAttributes.Interface |
                                                                             TypeAttributes.Abstract);

            Assert.Throws<InvalidOperationException>(() => { testTypeBuilder.DefineDefaultConstructor(MethodAttributes.Public); });
        }

        [Fact]
        public void TestThrowsExceptionForNoDefaultConstructor()
        {
            TypeBuilder baseTestTypeBuilder, testTypeBuilder;
            ConstructorBuilder baseCtorBuilder;
            FieldBuilder baseFieldBuilderA;

            baseTestTypeBuilder = RetrieveTestTypeBuilder(DynamicBaseTypeName,
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

            testTypeBuilder = RetrieveTestTypeBuilder(TypeAttributes.Public | TypeAttributes.Class);
            testTypeBuilder.SetParent(baseTestType);
            Assert.Throws<NotSupportedException>(() => { testTypeBuilder.DefineDefaultConstructor(MethodAttributes.Public); });
        }

        [Fact]
        public void TestThrowsExceptionForPrivateDefaultConstructor()
        {
            TypeBuilder baseTestTypeBuilder, testTypeBuilder;
            ConstructorBuilder baseCtorBuilder;

            baseTestTypeBuilder = RetrieveTestTypeBuilder(DynamicBaseTypeName,
                                                                             TypeAttributes.Public | TypeAttributes.Class);

            baseCtorBuilder = baseTestTypeBuilder.DefineConstructor(MethodAttributes.Private,
                                                                                             CallingConventions.HasThis,
                                                                                             new Type[] { typeof(int) });

            ILGenerator baseCtorIL = baseCtorBuilder.GetILGenerator();

            baseCtorIL.Emit(OpCodes.Ret);

            Type baseTestType = baseTestTypeBuilder.CreateTypeInfo().AsType();

            testTypeBuilder = RetrieveTestTypeBuilder(TypeAttributes.Public | TypeAttributes.Class);
            testTypeBuilder.SetParent(baseTestType);
            Assert.Throws<NotSupportedException>(() => { testTypeBuilder.DefineDefaultConstructor(MethodAttributes.Public); });
        }

        [Fact]
        public void TestThrowsExceptionForPrivateScopeDefaultConstructor()
        {
            TypeBuilder baseTestTypeBuilder, testTypeBuilder;
            ConstructorBuilder baseCtorBuilder;

            baseTestTypeBuilder = RetrieveTestTypeBuilder(DynamicBaseTypeName,
                                                                             TypeAttributes.Public | TypeAttributes.Class);

            baseCtorBuilder = baseTestTypeBuilder.DefineConstructor(MethodAttributes.PrivateScope,
                                                                                             CallingConventions.HasThis,
                                                                                             new Type[] { typeof(int) });

            ILGenerator baseCtorIL = baseCtorBuilder.GetILGenerator();

            baseCtorIL.Emit(OpCodes.Ret);

            Type baseTestType = baseTestTypeBuilder.CreateTypeInfo().AsType();

            testTypeBuilder = RetrieveTestTypeBuilder(TypeAttributes.Public | TypeAttributes.Class);
            testTypeBuilder.SetParent(baseTestType);
            Assert.Throws<NotSupportedException>(() => { testTypeBuilder.DefineDefaultConstructor(MethodAttributes.Public); });
        }
    }
}