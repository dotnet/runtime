// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class ILGeneratorEmit1
    {
        [Fact]
        public void PosTest1()
        {
            ModuleBuilder module = Helpers.DynamicModule();
            TypeBuilder type = module.DefineType("C1", TypeAttributes.Public);
            MethodBuilder method1 = type.DefineMethod("meth1", MethodAttributes.Public, typeof(int), new Type[0]);

            int expectedRet = 1;

            // Generate code for the method that we are going to use as MethodInfo in ILGenerator.Emit()
            ILGenerator ilgen = method1.GetILGenerator();
            ilgen.Emit(OpCodes.Ldc_I4, expectedRet);
            ilgen.Emit(OpCodes.Ret);

            // Create the type where this method is in
            Type createdType1 = type.CreateType();
            MethodInfo createdMethod1 = createdType1.GetMethod("meth1");

            TypeBuilder type2 = module.DefineType("C2", TypeAttributes.Public);
            MethodBuilder method2 = type2.DefineMethod("meth2", MethodAttributes.Public | MethodAttributes.Static, typeof(int), new Type[0]);

            // Generate code for the method which will be invoking the first method
            ILGenerator ilgen2 = method2.GetILGenerator();
            ilgen2.Emit(OpCodes.Newobj, createdType1.GetConstructor(new Type[0]));
            ilgen2.Emit(OpCodes.Call, createdMethod1);
            ilgen2.Emit(OpCodes.Ret);

            // Create the type whose method will be invoking the MethodInfo method
            Type createdType2 = type2.CreateType();
            MethodInfo createdMethod2 = createdType2.GetMethod("meth2");

            // meth2 should invoke meth1 which should return value from meth1
            Assert.Equal(expectedRet, createdMethod2.Invoke(null, null));
        }

        [Fact]
        public void PosTest2()
        {
            ModuleBuilder module = Helpers.DynamicModule();
            TypeBuilder type1 = module.DefineType("C1", TypeAttributes.Public);
            MethodBuilder method1 = type1.DefineMethod("meth1", MethodAttributes.Public, typeof(int), new Type[0]);
            int expectedRet = 12;

            // Generate code for the method that we are going to use as MethodInfo in ILGenerator.Emit()
            ILGenerator ilGenerator1 = method1.GetILGenerator();
            ilGenerator1.Emit(OpCodes.Ldc_I4, expectedRet);
            ilGenerator1.Emit(OpCodes.Ret);

            // Create the type where this method is in
            Type createdType1 = type1.CreateType();
            MethodInfo createdMethod1 = createdType1.GetMethod("meth1");

            TypeBuilder type2 = module.DefineType("C2", TypeAttributes.Public);
            MethodBuilder method2 = type2.DefineMethod("meth2", MethodAttributes.Public, typeof(int), new Type[0]);

            // Generate code for the method which will be invoking the first method
            ILGenerator ilGenerator2 = method2.GetILGenerator();
            ilGenerator2.Emit(OpCodes.Newobj, createdType1.GetConstructor(new Type[0]));
            ilGenerator2.Emit(OpCodes.Callvirt, createdMethod1);
            ilGenerator2.Emit(OpCodes.Ret);

            // Create the type whose method will be invoking the MethodInfo method
            Type createdType2 = type2.CreateType();
            MethodInfo createdMethod2 = createdType2.GetMethod("meth2");

            // meth2 should invoke meth1 which should return value 'methodRet'
            Assert.Equal(expectedRet, createdMethod2.Invoke(Activator.CreateInstance(createdType2), null));
        }

        [Fact]
        public void PosTest3()
        {
            ModuleBuilder modbuild = Helpers.DynamicModule();

            TypeBuilder type1 = modbuild.DefineType("C1", TypeAttributes.Public);
            MethodBuilder method1 = type1.DefineMethod("meth1", MethodAttributes.Public, typeof(int), new Type[0]);
            method1.DefineGenericParameters("T");

            int expectedRet = 101;

            // Generate code for the method that we are going to use as MethodInfo in ILGenerator.Emit()
            ILGenerator ilGenerator = method1.GetILGenerator();
            ilGenerator.Emit(OpCodes.Ldc_I4, expectedRet);
            ilGenerator.Emit(OpCodes.Ret);

            // Create the type where this method is in
            Type createdType1 = type1.CreateType();
            MethodInfo createdMethod1 = createdType1.GetMethod("meth1");
            MethodInfo genericMethod = createdMethod1.MakeGenericMethod(typeof(int));

            TypeBuilder type2 = modbuild.DefineType("C2", TypeAttributes.Public);
            MethodBuilder method2 = type2.DefineMethod("meth2", MethodAttributes.Public | MethodAttributes.Static, typeof(int), new Type[0]);

            // Generate code for the method which will be invoking the first method
            ILGenerator ilGenerator2 = method2.GetILGenerator();
            ilGenerator2.Emit(OpCodes.Newobj, createdType1.GetConstructor(new Type[0]));
            ilGenerator2.Emit(OpCodes.Callvirt, genericMethod);
            ilGenerator2.Emit(OpCodes.Ret);

            // Create the type whose method will be invoking the MethodInfo method
            Type createdType2 = type2.CreateType();
            MethodInfo createdMethod2 = createdType2.GetMethod("meth2");

            // meth2 should invoke meth1 which should return value 'methodRet'
            Assert.Equal(expectedRet, createdMethod2.Invoke(null, null));
        }

        [Fact]
        public void PosTest4()
        {
            ModuleBuilder module = Helpers.DynamicModule();

            TypeBuilder type1 = module.DefineType("C1", TypeAttributes.Public);
            type1.DefineGenericParameters(new string[] { "T" });

            MethodBuilder method1 = type1.DefineMethod("meth1", MethodAttributes.Public, typeof(long), new Type[0]);

            long expectedRet = 500000;
            // Generate code for the method that we are going to use as MethodInfo in ILGenerator.Emit()
            ILGenerator ilGenerator1 = method1.GetILGenerator();
            ilGenerator1.Emit(OpCodes.Ldc_I8, expectedRet);
            ilGenerator1.Emit(OpCodes.Ret);

            // Create the type where this method is in
            Type createdType1 = type1.CreateType();
            Type genericType = createdType1.MakeGenericType(typeof(int));
            MethodInfo genericMethod = genericType.GetMethod("meth1");

            TypeBuilder type2 = module.DefineType("C2", TypeAttributes.Public);
            MethodBuilder method2 = type2.DefineMethod("meth2", MethodAttributes.Public | MethodAttributes.Static, typeof(long), new Type[0]);

            // Generate code for the method which will be invoking the first method
            ILGenerator ilGenerator2 = method2.GetILGenerator();
            ilGenerator2.Emit(OpCodes.Newobj, genericType.GetConstructor(new Type[0]));
            ilGenerator2.Emit(OpCodes.Callvirt, genericMethod);
            ilGenerator2.Emit(OpCodes.Ret);

            // Create the type whose method will be invoking the MethodInfo method
            Type createdType2 = type2.CreateType();
            MethodInfo createdMethod2 = createdType2.GetMethod("meth2");

            // meth2 should invoke meth1 which should return value 'methodRet'
            Assert.Equal(expectedRet, createdMethod2.Invoke(null, null));
        }

        [Fact]
        public void PosTest5()
        {
            ModuleBuilder module = Helpers.DynamicModule();

            TypeBuilder type1 = module.DefineType("C1", TypeAttributes.Public);
            type1.DefineGenericParameters("T");

            MethodBuilder method1 = type1.DefineMethod("meth1", MethodAttributes.Public, typeof(int), new Type[0]);
            method1.DefineGenericParameters("U");

            int expectedRet = 1;
            // Generate code for the method that we are going to use as MethodInfo in ILGenerator.Emit()
            ILGenerator ilGenerator1 = method1.GetILGenerator();
            ilGenerator1.Emit(OpCodes.Ldc_I4, expectedRet);
            ilGenerator1.Emit(OpCodes.Ret);

            // create the type where this method is in
            Type createdType1 = type1.CreateType();
            Type genericType = createdType1.MakeGenericType(typeof(int));
            MethodInfo createdMethod1 = genericType.GetMethod("meth1");
            MethodInfo genericMethod = createdMethod1.MakeGenericMethod(typeof(string));

            TypeBuilder type2 = module.DefineType("C2", TypeAttributes.Public);
            MethodBuilder method2 = type2.DefineMethod("meth2", MethodAttributes.Public | MethodAttributes.Static, typeof(int), new Type[0]);

            // Generate code for the method which will be invoking the first method
            ILGenerator ilGenerator2 = method2.GetILGenerator();
            ilGenerator2.Emit(OpCodes.Newobj, genericType.GetConstructor(new Type[0]));
            ilGenerator2.Emit(OpCodes.Callvirt, genericMethod);
            ilGenerator2.Emit(OpCodes.Ret);

            // Create the type whose method will be invoking the MethodInfo method
            Type createdType2 = type2.CreateType();
            MethodInfo createdMethod2 = createdType2.GetMethod("meth2");

            // meth2 should invoke meth1 which should return value 'methodRet'
            Assert.Equal(expectedRet, createdMethod2.Invoke(null, null));
        }

        [Fact]
        public void Emit_OpCodes_MethodInfo_NullMethod_ThrowsArgumentNullException()
        {
            ModuleBuilder module = Helpers.DynamicModule();
            MethodBuilder method = module.DefineGlobalMethod("Method", MethodAttributes.Public | MethodAttributes.Static, typeof(Type), new Type[0]);

            ILGenerator ilGenerator = method.GetILGenerator();
            AssertExtensions.Throws<ArgumentNullException>("meth", () => ilGenerator.Emit(OpCodes.Call, (MethodInfo)null));
        }
    }
}
