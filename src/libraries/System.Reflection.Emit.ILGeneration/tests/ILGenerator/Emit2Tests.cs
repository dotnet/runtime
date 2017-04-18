// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class ILGeneratorEmit2
    {
        [Fact]
        public void PosTest1()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.NotPublic);
            MethodBuilder method = type.DefineMethod("Method", MethodAttributes.Public | MethodAttributes.Static);
            ILGenerator generator = method.GetILGenerator();
            LocalBuilder arg = generator.DeclareLocal(typeof(int));

            generator.Emit(OpCodes.Ldarg_0, arg);
            generator.Emit(OpCodes.Nop, arg);

            // Try emit opcode which takes multiple args
            generator.Emit(OpCodes.Add, arg);
        }

        [Fact]
        public void PosTest2()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.NotPublic);
            MethodBuilder method = type.DefineMethod("Method", MethodAttributes.Public | MethodAttributes.Static);
            ILGenerator generator = method.GetILGenerator();
            LocalBuilder arg = generator.DeclareLocal(typeof(ILGeneratorEmit2));

            generator.Emit(OpCodes.Ldnull, arg);

            generator.BeginScope();

            generator.Emit(OpCodes.Ldarg_0, arg);
            generator.Emit(OpCodes.Nop, arg);

            // Try emit opcode which takes multiple args
            generator.Emit(OpCodes.Add, arg);
        }

        [Fact]
        public void PosTest3()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.NotPublic);
            MethodBuilder method = type.DefineMethod("Method", MethodAttributes.Public | MethodAttributes.Static);
            ILGenerator generator = method.GetILGenerator();
            LocalBuilder arg = generator.DeclareLocal(typeof(object));

            generator.BeginExceptionBlock();

            generator.Emit(OpCodes.Ldnull, arg);
            generator.Emit(OpCodes.Ldarg_0, arg);
            generator.Emit(OpCodes.Nop, arg);

            // Try emit opcode which takes multiple args
            generator.Emit(OpCodes.Add, arg);
        }

        [Fact]
        public void PosTest4()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.NotPublic);
            MethodBuilder method = type.DefineMethod("Method", MethodAttributes.Public | MethodAttributes.Static);
            ILGenerator generator = method.GetILGenerator();
            LocalBuilder arg = generator.DeclareLocal(typeof(int));

            generator.BeginExceptionBlock();
            generator.Emit(OpCodes.Throw, arg);
            generator.BeginCatchBlock(typeof(Exception));

            generator.Emit(OpCodes.Ldnull, arg);
            generator.Emit(OpCodes.Ldarg_0, arg);
            generator.Emit(OpCodes.Nop, arg);

            // Try emit opcode which takes multiple args
            generator.Emit(OpCodes.Add, arg);
        }

        [Fact]
        public void PosTest5()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.NotPublic);
            MethodBuilder method = type.DefineMethod("Method", MethodAttributes.Public | MethodAttributes.Static);
            ILGenerator generator = method.GetILGenerator();
            LocalBuilder arg = generator.DeclareLocal(typeof(int));

            generator.BeginExceptionBlock();
            generator.Emit(OpCodes.Throw, arg);
            generator.BeginFinallyBlock();

            generator.Emit(OpCodes.Ldnull, arg);
            generator.Emit(OpCodes.Ldarg_0, arg);
            generator.Emit(OpCodes.Nop, arg);

            // Try emit opcode which takes multiple args
            generator.Emit(OpCodes.Add, arg);
        }

        [Fact]
        public void PosTest6()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.NotPublic);
            MethodBuilder method = type.DefineMethod("Method", MethodAttributes.Public | MethodAttributes.Static);
            ILGenerator generator = method.GetILGenerator();
            LocalBuilder arg = generator.DeclareLocal(typeof(int));

            generator.BeginExceptionBlock();
            generator.Emit(OpCodes.Throw, arg);
            generator.BeginFaultBlock();

            generator.Emit(OpCodes.Ldnull, arg);
            generator.Emit(OpCodes.Ldarg_0, arg);
            generator.Emit(OpCodes.Nop, arg);

            // Try emit opcode which takes multiple args
            generator.Emit(OpCodes.Add, arg);
        }

        [Fact]
        public void PosTest7()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.NotPublic);
            MethodBuilder method = type.DefineMethod("Method", MethodAttributes.Public | MethodAttributes.Static);
            ILGenerator generator = method.GetILGenerator();
            LocalBuilder arg = generator.DeclareLocal(typeof(int));

            generator.Emit(OpCodes.Ldarg_0, arg);
            generator.Emit(OpCodes.Nop, arg);

            // Try emit opcode which takes multiple args
            generator.Emit(OpCodes.Add, arg);
        }
        
        [Fact]
        public void Emit_OpCodes_LocalBuilder_NullLocal_ThrowsArgumentNullException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.NotPublic);
            MethodBuilder method = type.DefineMethod("Method", MethodAttributes.Public | MethodAttributes.Static);
            ILGenerator generator = method.GetILGenerator();

            AssertExtensions.Throws<ArgumentNullException>("local", () => generator.Emit(OpCodes.Ldarg_0, (LocalBuilder)null));
        }

        [Fact]
        public void Emit_OpCodes_LocalBuilder_LocalFromDifferentMethod_ThrowsArgumentException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.NotPublic);
            MethodBuilder method1 = type.DefineMethod("Method1", MethodAttributes.Public | MethodAttributes.Static);
            MethodBuilder method2 = type.DefineMethod("Method2", MethodAttributes.Public | MethodAttributes.Static);
            ILGenerator generator = method1.GetILGenerator();
            LocalBuilder local = method2.GetILGenerator().DeclareLocal(typeof(int));

            AssertExtensions.Throws<ArgumentException>("local", () => generator.Emit(OpCodes.Ldarg_0, local));
        }

        [Fact]
        public void Emit_OpCodes_LocalBuilder_TooManyLocals_ThrowsInvalidOperationException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.NotPublic);
            MethodBuilder method = type.DefineMethod("NegTest3_Method", MethodAttributes.Public | MethodAttributes.Static);
            ILGenerator generator = method.GetILGenerator();
            for (int i = 0; i <= byte.MaxValue; ++i)
            {
                generator.DeclareLocal(typeof(int));
            }
            LocalBuilder arg = generator.DeclareLocal(typeof(int));

            Assert.Throws<InvalidOperationException>(() => generator.Emit(OpCodes.Br_S, arg));
        }
    }
}
