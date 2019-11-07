// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class ILGeneratorDeclareLocal
    {
        public static Type[] TestData => new Type[]
        {
            typeof(int),
            typeof(object),
            typeof(TestClassLocal1),
            typeof(TestStructLocal1),
            typeof(TestDelegateLocal1),
            typeof(TestEnumLocal1),
            typeof(TestExceptionLocal1),
            typeof(void)
        };

        [Fact]
        public void Basic()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.NotPublic);
            MethodBuilder method = type.DefineMethod("TestMethod", MethodAttributes.Public | MethodAttributes.Static);
            ILGenerator generator = method.GetILGenerator();
            VerifyDeclareLocal(generator);
        }

        [Fact]
        public void BeginScope()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.NotPublic);
            MethodBuilder method = type.DefineMethod("TestMethod", MethodAttributes.Public | MethodAttributes.Static);
            ILGenerator generator = method.GetILGenerator();

            generator.BeginScope();
            VerifyDeclareLocal(generator);
        }

        [Fact]
        public void BeginExceptionBlock()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.NotPublic);
            MethodBuilder method = type.DefineMethod("TestMethod", MethodAttributes.Public | MethodAttributes.Static);
            ILGenerator generator = method.GetILGenerator();

            generator.BeginExceptionBlock();
            VerifyDeclareLocal(generator);
        }

        [Fact]
        public void BeginCatchBlock()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.NotPublic);
            MethodBuilder method = type.DefineMethod("PosTest4_Method", MethodAttributes.Public | MethodAttributes.Static);
            ILGenerator generator = method.GetILGenerator();

            generator.BeginExceptionBlock();
            generator.BeginCatchBlock(typeof(TestExceptionLocal1));
            VerifyDeclareLocal(generator);
        }

        [Fact]
        public void BeginFinallyBlock()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.NotPublic);
            MethodBuilder method = type.DefineMethod("PosTest5_Method", MethodAttributes.Public | MethodAttributes.Static);
            ILGenerator generator = method.GetILGenerator();

            generator.BeginExceptionBlock();
            generator.BeginFinallyBlock();
            VerifyDeclareLocal(generator);
        }

        [Fact]
        public void AbstractPublicMethod()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Abstract | TypeAttributes.Public);
            MethodBuilder method = type.DefineMethod("PosTest6_Method", MethodAttributes.Abstract | MethodAttributes.Public);
            ILGenerator generator = method.GetILGenerator();
            VerifyDeclareLocal(generator);
        }

        [Fact]
        public void DeclareLocal_NullLocalType_ThrowsArgumentNullException()
        {
            ModuleBuilder module = Helpers.DynamicModule();
            MethodBuilder method = module.DefineGlobalMethod("Method", MethodAttributes.Public | MethodAttributes.Static, typeof(Type), new Type[0]);
            ILGenerator ilGenerator = method.GetILGenerator();
            AssertExtensions.Throws<ArgumentNullException>("localType", () => ilGenerator.DeclareLocal(null));
            AssertExtensions.Throws<ArgumentNullException>("localType", () => ilGenerator.DeclareLocal(null, false));
        }

        [Fact]
        public void DeclareLocal_TypeCreated_ThrowsInvalidOperationException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.NotPublic);
            MethodBuilder method = type.DefineMethod("Method", MethodAttributes.Public | MethodAttributes.Static, typeof(Type), new Type[0]);
            ILGenerator ilGenerator = method.GetILGenerator();
            ilGenerator.Emit(OpCodes.Ret);

            type.CreateTypeInfo();
            Assert.Throws<InvalidOperationException>(() => ilGenerator.DeclareLocal(typeof(int)));
        }

        [Fact]
        public void DeclareLocal_GlobalFunctionsCreated_ThrowsInvalidOperationException()
        {
            ModuleBuilder module = Helpers.DynamicModule();
            MethodBuilder method = module.DefineGlobalMethod("method1", MethodAttributes.Public | MethodAttributes.Static, typeof(Type), new Type[0]);
            ILGenerator ilGenerator = method.GetILGenerator();
            ilGenerator.Emit(OpCodes.Ret);

            module.CreateGlobalFunctions();
            Assert.Throws<InvalidOperationException>(() => ilGenerator.DeclareLocal(typeof(int)));
        }

        private void VerifyDeclareLocal(ILGenerator generator)
        {
            for (int i = 0; i < TestData.Length; i++)
            {
                LocalBuilder local = generator.DeclareLocal(TestData[i]);
                Assert.Equal(TestData[i], local.LocalType);
                Assert.Equal(i, local.LocalIndex);
                Assert.False(local.IsPinned);
            }
        }

        public class TestClassLocal1 { }
        public struct TestStructLocal1 { }
        public delegate void TestDelegateLocal1(TestStructLocal1 ts);
        public enum TestEnumLocal1 { DEFAULT }
        public class TestExceptionLocal1 : Exception { }
    }
}
