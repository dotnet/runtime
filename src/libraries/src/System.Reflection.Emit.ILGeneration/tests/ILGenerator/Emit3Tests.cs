// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Reflection.Emit.Tests
{
    public struct EmitStruct3 { }

    public class ILGeneratorEmit3
    {
        [Fact]
        public void PosTest1()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            MethodBuilder method = type.DefineMethod("meth1", MethodAttributes.Public | MethodAttributes.Static, typeof(bool), new Type[0]);

            ILGenerator ilGenerator = method.GetILGenerator();

            LocalBuilder lb0 = ilGenerator.DeclareLocal(typeof(int));
            LocalBuilder lb1 = ilGenerator.DeclareLocal(typeof(byte));
            LocalBuilder lb2 = ilGenerator.DeclareLocal(typeof(double));
            LocalBuilder lb3 = ilGenerator.DeclareLocal(typeof(bool));
            LocalBuilder lb4 = ilGenerator.DeclareLocal(typeof(bool));

            Label label1 = ilGenerator.DefineLabel();
            Label label2 = ilGenerator.DefineLabel();
            Label label3 = ilGenerator.DefineLabel();
            Label label4 = ilGenerator.DefineLabel();

            // emit the locals and check that we get correct values stored 
            ilGenerator.Emit(OpCodes.Ldc_I4, 5);
            ilGenerator.Emit(OpCodes.Stloc, lb0);
            ilGenerator.Emit(OpCodes.Ldloc, lb0);
            ilGenerator.Emit(OpCodes.Ldc_I4, 5);
            ilGenerator.Emit(OpCodes.Ceq);
            ilGenerator.Emit(OpCodes.Stloc, lb4);
            ilGenerator.Emit(OpCodes.Ldloc, lb4);
            ilGenerator.Emit(OpCodes.Brtrue_S, label1);

            ilGenerator.Emit(OpCodes.Ldc_I4, 0);
            ilGenerator.Emit(OpCodes.Stloc, lb3);
            ilGenerator.Emit(OpCodes.Br_S, label4);

            ilGenerator.MarkLabel(label1);
            ilGenerator.Emit(OpCodes.Ldc_I4, 1);
            ilGenerator.Emit(OpCodes.Stloc, lb1);
            ilGenerator.Emit(OpCodes.Ldloc, lb1);
            ilGenerator.Emit(OpCodes.Ldc_I4, 1);
            ilGenerator.Emit(OpCodes.Ceq);
            ilGenerator.Emit(OpCodes.Stloc, lb4);
            ilGenerator.Emit(OpCodes.Ldloc, lb4);
            ilGenerator.Emit(OpCodes.Brtrue_S, label2);

            ilGenerator.Emit(OpCodes.Ldc_I4, 0);
            ilGenerator.Emit(OpCodes.Stloc, lb3);
            ilGenerator.Emit(OpCodes.Br_S, label4);

            ilGenerator.MarkLabel(label2);
            ilGenerator.Emit(OpCodes.Ldc_R8, 2.5);
            ilGenerator.Emit(OpCodes.Stloc, lb2);
            ilGenerator.Emit(OpCodes.Ldloc, lb2);
            ilGenerator.Emit(OpCodes.Ldc_R8, 2.5);
            ilGenerator.Emit(OpCodes.Ceq);
            ilGenerator.Emit(OpCodes.Stloc, lb4);
            ilGenerator.Emit(OpCodes.Ldloc, lb4);
            ilGenerator.Emit(OpCodes.Brtrue_S, label3);

            ilGenerator.Emit(OpCodes.Ldc_I4, 0);
            ilGenerator.Emit(OpCodes.Stloc, lb3);
            ilGenerator.Emit(OpCodes.Br_S, label4);

            // Should return true if all checks were correct
            ilGenerator.MarkLabel(label3);
            ilGenerator.Emit(OpCodes.Ldc_I4, 1);
            ilGenerator.Emit(OpCodes.Stloc, lb3);
            ilGenerator.Emit(OpCodes.Br_S, label4);

            ilGenerator.MarkLabel(label4);
            ilGenerator.Emit(OpCodes.Ldloc, lb3);
            ilGenerator.Emit(OpCodes.Ret);

            // Create the type where this method is in
            Type createdType = type.CreateTypeInfo().AsType();
            MethodInfo createdMethod = createdType.GetMethod("meth1");

            Assert.True((bool)createdMethod.Invoke(null, null));
        }

        [Fact]
        public void PosTest2()
        {
            TypeBuilder tb = Helpers.DynamicType(TypeAttributes.Public);
            MethodBuilder method = tb.DefineMethod("meth1", MethodAttributes.Public | MethodAttributes.Static, typeof(bool), new Type[0]);

            ILGenerator ilGenerator = method.GetILGenerator();

            LocalBuilder lb0 = ilGenerator.DeclareLocal(typeof(EmitStruct3));

            // Emit the locals
            ilGenerator.Emit(OpCodes.Ldloca, lb0);
            ilGenerator.Emit(OpCodes.Initobj, typeof(EmitStruct3));

            ilGenerator.Emit(OpCodes.Ldc_I4, 1);
            ilGenerator.Emit(OpCodes.Ret);

            // Create the type where this method is in
            Type createdType = tb.CreateTypeInfo().AsType();
            MethodInfo createdMethod = createdType.GetMethod("meth1");

            Assert.True((bool)createdMethod.Invoke(null, null));
        }

        [Fact]
        public void PosTest3()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            MethodBuilder method = type.DefineMethod("meth1", MethodAttributes.Public | MethodAttributes.Static, typeof(bool), new Type[0]);

            ILGenerator ilGenerator = method.GetILGenerator();

            // Emit locals
            LocalBuilder local = ilGenerator.DeclareLocal(typeof(int));
            ilGenerator.Emit(OpCodes.Nop, local);
            ilGenerator.Emit(OpCodes.Ldc_I4, 1);
            ilGenerator.Emit(OpCodes.Ret);

            // create the type where this method is in
            Type createdType = type.CreateTypeInfo().AsType();
            MethodInfo createdMethod = createdType.GetMethod("meth1");

            Assert.True((bool)createdMethod.Invoke(null, null));
        }

        [Fact]
        public void PosTest4()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            MethodBuilder method = type.DefineMethod("meth1", MethodAttributes.Public | MethodAttributes.Static, typeof(bool), new Type[0]);

            ILGenerator ilGenerator = method.GetILGenerator();

            LocalBuilder lb0 = ilGenerator.DeclareLocal(typeof(int));
            LocalBuilder lb1 = ilGenerator.DeclareLocal(typeof(byte));
            LocalBuilder lb2 = ilGenerator.DeclareLocal(typeof(double));
            LocalBuilder lb3 = ilGenerator.DeclareLocal(typeof(bool));
            LocalBuilder lb4 = ilGenerator.DeclareLocal(typeof(bool));

            Label label1 = ilGenerator.DefineLabel();
            Label label2 = ilGenerator.DefineLabel();
            Label label3 = ilGenerator.DefineLabel();
            Label label4 = ilGenerator.DefineLabel();

            // Emit the locals using Stloc_0, Stloc_1, Stloc_2, Stloc_3, Stloc_S, 
            // Ldloc_0, Ldloc_1, Ldloc_2, Ldloc_3, Ldloc_S,

            ilGenerator.Emit(OpCodes.Ldc_I4, 5);
            ilGenerator.Emit(OpCodes.Stloc_0, lb0);
            ilGenerator.Emit(OpCodes.Ldloc_0, lb0);
            ilGenerator.Emit(OpCodes.Ldc_I4, 5);
            ilGenerator.Emit(OpCodes.Ceq);
            ilGenerator.Emit(OpCodes.Stloc_S, lb4);
            ilGenerator.Emit(OpCodes.Ldloc_S, lb4);
            ilGenerator.Emit(OpCodes.Brtrue_S, label1);

            ilGenerator.Emit(OpCodes.Ldc_I4, 0);
            ilGenerator.Emit(OpCodes.Stloc_3, lb3);
            ilGenerator.Emit(OpCodes.Br_S, label4);

            ilGenerator.MarkLabel(label1);
            ilGenerator.Emit(OpCodes.Ldc_I4, 1);
            ilGenerator.Emit(OpCodes.Stloc_1, lb1);
            ilGenerator.Emit(OpCodes.Ldloc_1, lb1);
            ilGenerator.Emit(OpCodes.Ldc_I4, 1);
            ilGenerator.Emit(OpCodes.Ceq);
            ilGenerator.Emit(OpCodes.Stloc_S, lb4);
            ilGenerator.Emit(OpCodes.Ldloc_S, lb4);
            ilGenerator.Emit(OpCodes.Brtrue_S, label2);

            ilGenerator.Emit(OpCodes.Ldc_I4, 0);
            ilGenerator.Emit(OpCodes.Stloc_3, lb3);
            ilGenerator.Emit(OpCodes.Br_S, label4);

            ilGenerator.MarkLabel(label2);
            ilGenerator.Emit(OpCodes.Ldc_R8, 2.5);
            ilGenerator.Emit(OpCodes.Stloc_2, lb2);
            ilGenerator.Emit(OpCodes.Ldloc_2, lb2);
            ilGenerator.Emit(OpCodes.Ldc_R8, 2.5);
            ilGenerator.Emit(OpCodes.Ceq);
            ilGenerator.Emit(OpCodes.Stloc_S, lb4);
            ilGenerator.Emit(OpCodes.Ldloc_S, lb4);
            ilGenerator.Emit(OpCodes.Brtrue_S, label3);

            ilGenerator.Emit(OpCodes.Ldc_I4, 0);
            ilGenerator.Emit(OpCodes.Stloc_3, lb3);
            ilGenerator.Emit(OpCodes.Br_S, label4);

            ilGenerator.MarkLabel(label3);
            ilGenerator.Emit(OpCodes.Ldc_I4, 1);
            ilGenerator.Emit(OpCodes.Stloc_3, lb3);
            ilGenerator.Emit(OpCodes.Br_S, label4);

            ilGenerator.MarkLabel(label4);
            ilGenerator.Emit(OpCodes.Ldloc_3, lb3);
            ilGenerator.Emit(OpCodes.Ret);

            // Create the type where this method is in
            Type createdType = type.CreateTypeInfo().AsType();
            MethodInfo createdMethod = createdType.GetMethod("meth1");

            Assert.True((bool)createdMethod.Invoke(null, null));
        }

        [Fact]
        public void Emit_OpCodes_LocalBuilder_NullLocal_ThrowsArgumentNullException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.NotPublic);
            MethodBuilder method = type.DefineMethod("Method", MethodAttributes.Public | MethodAttributes.Static);
            ILGenerator generator = method.GetILGenerator();

            AssertExtensions.Throws<ArgumentNullException>("local", () => generator.Emit(OpCodes.Ldloc_0, (LocalBuilder)null));
        }

        [Fact]
        public void Emit_OpCodes_LocalBuilder_LocalFromDifferentMethod_ThrowsArgumentException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.NotPublic);
            MethodBuilder method1 = type.DefineMethod("Method1", MethodAttributes.Public | MethodAttributes.Static);
            MethodBuilder method2 = type.DefineMethod("Method2", MethodAttributes.Public | MethodAttributes.Static);
            ILGenerator generator = method1.GetILGenerator();
            LocalBuilder local = method2.GetILGenerator().DeclareLocal(typeof(string));

            AssertExtensions.Throws<ArgumentException>("local", () => generator.Emit(OpCodes.Ldloc_0, local));
        }

        [Fact]
        public void Emit_OpCodes_LocalBuilder_TooManyLocals_ThrowsInvalidOperationException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.NotPublic);
            MethodBuilder method = type.DefineMethod("NegTest3_Method", MethodAttributes.Public | MethodAttributes.Static);
            ILGenerator generator = method.GetILGenerator();
            for (int i = 0; i <= byte.MaxValue; ++i)
            {
                generator.DeclareLocal(typeof(string));
            }
            LocalBuilder arg = generator.DeclareLocal(typeof(string));

            Assert.Throws<InvalidOperationException>(() => generator.Emit(OpCodes.Ldloc_S, arg));
        }
    }
}
