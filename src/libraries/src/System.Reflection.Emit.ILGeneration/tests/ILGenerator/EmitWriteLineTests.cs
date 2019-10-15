// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class ILGeneratorEmitWriteLine
    {
        [Fact]
        public void EmitWriteLineTests()
        {
            ModuleBuilder module = Helpers.DynamicModule();
            TypeBuilder type1 = module.DefineType("C1", TypeAttributes.Public);
            MethodBuilder method1 = type1.DefineMethod("meth1", MethodAttributes.Public, typeof(int), new Type[0]);
            FieldBuilder field = type1.DefineField("field1", typeof(int), FieldAttributes.Public | FieldAttributes.Static);

            int expectedRet = 1;

            // Generate code for the method that we are going to use as MethodInfo in ILGenerator.Emit()
            ILGenerator ilGenerator1 = method1.GetILGenerator();
            ilGenerator1.Emit(OpCodes.Ldc_I4, expectedRet);
            ilGenerator1.Emit(OpCodes.Ret);

            // Create the type where this method is in
            Type createdType1 = type1.CreateTypeInfo().AsType();
            FieldInfo createdField = createdType1.GetField("field1");

            TypeBuilder type2 = module.DefineType("C2", TypeAttributes.Public);
            MethodBuilder method2 = type2.DefineMethod("meth2", MethodAttributes.Public | MethodAttributes.Static, typeof(int), new Type[0]);

            // Generate code for the method which will be invoking the first method
            ILGenerator ilGenerator2 = method2.GetILGenerator();
            LocalBuilder local = ilGenerator2.DeclareLocal(typeof(bool));
            ilGenerator2.EmitWriteLine(createdField);
            ilGenerator2.EmitWriteLine("emitWriteLine");
            ilGenerator2.EmitWriteLine(local);
            ilGenerator2.Emit(OpCodes.Ldc_I4_1);
            ilGenerator2.Emit(OpCodes.Ret);

            // Create the type whose method will be invoking the MethodInfo method
            Type createdType2 = type2.CreateTypeInfo().AsType();
            MethodInfo createdMethod = createdType2.GetMethod("meth2");

            // meth2 should invoke meth1 which should return value from meth1
            Assert.Equal(expectedRet, createdMethod.Invoke(null, null));
        }
    }
}
