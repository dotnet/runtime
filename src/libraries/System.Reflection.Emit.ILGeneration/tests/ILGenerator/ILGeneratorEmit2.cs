// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using System.Reflection.Emit;
using Xunit;

namespace System.Reflection.Emit.ILGeneration.Tests
{
    public class ILGeneratorEmit2
    {
        private const string c_ASSEMBLY_NAME = "ILGeneratorEmit10";
        private const string c_DEFAULT_MODULE_NAME = "DynamicModule";
        private const string c_DEFAULT_TYPE_NAME = "DynamicType";
        private const AssemblyBuilderAccess c_DEFAULT_ASSEMBLY_BUILDER_ACCESS = AssemblyBuilderAccess.Run;
        private const MethodAttributes c_DEFAULT_METHOD_ATTRIBUTE = MethodAttributes.Public | MethodAttributes.Static;

        private static TypeBuilder s_testTypeBuilder;

        private static TypeBuilder TestTypeBuilder
        {
            get
            {
                if (s_testTypeBuilder == null)
                {
                    AssemblyName name = new AssemblyName(c_ASSEMBLY_NAME);
                    AssemblyBuilder assembly = AssemblyBuilder.DefineDynamicAssembly(name, c_DEFAULT_ASSEMBLY_BUILDER_ACCESS);
                    ModuleBuilder module = TestLibrary.Utilities.GetModuleBuilder(assembly, c_ASSEMBLY_NAME);
                    s_testTypeBuilder = module.DefineType(c_DEFAULT_TYPE_NAME);
                }

                return s_testTypeBuilder;
            }
        }

        [Fact]
        public void PosTest1()
        {
            MethodBuilder method = TestTypeBuilder.DefineMethod("PosTest1_Method", c_DEFAULT_METHOD_ATTRIBUTE);
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
            MethodBuilder method = TestTypeBuilder.DefineMethod("PosTest2_Method", c_DEFAULT_METHOD_ATTRIBUTE);
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
            MethodBuilder method = TestTypeBuilder.DefineMethod("PosTest3_Method", c_DEFAULT_METHOD_ATTRIBUTE);
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
            MethodBuilder method = TestTypeBuilder.DefineMethod("PosTest4_Method", c_DEFAULT_METHOD_ATTRIBUTE);
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
            MethodBuilder method = TestTypeBuilder.DefineMethod("PosTest5_Method", c_DEFAULT_METHOD_ATTRIBUTE);
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
            MethodBuilder method = TestTypeBuilder.DefineMethod("PosTest6_Method", c_DEFAULT_METHOD_ATTRIBUTE);
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
            MethodBuilder method = TestTypeBuilder.DefineMethod("PosTest7_Method", c_DEFAULT_METHOD_ATTRIBUTE);
            ILGenerator generator = method.GetILGenerator();
            LocalBuilder arg = generator.DeclareLocal(typeof(int));

            generator.Emit(OpCodes.Ldarg_0, arg);
            generator.Emit(OpCodes.Nop, arg);

            // Try emit opcode which takes multiple args
            generator.Emit(OpCodes.Add, arg);
        }

        [Fact]
        public void NegTest1()
        {
            MethodBuilder method = TestTypeBuilder.DefineMethod("NegTest1_Method", c_DEFAULT_METHOD_ATTRIBUTE);
            ILGenerator generator = method.GetILGenerator();
            LocalBuilder arg = null;

            Assert.Throws<ArgumentNullException>(() => { generator.Emit(OpCodes.Ldarg_0, arg); });
        }

        [Fact]
        public void NegTest2()
        {
            MethodBuilder method1 = TestTypeBuilder.DefineMethod("NegTest2_Method1", c_DEFAULT_METHOD_ATTRIBUTE);
            MethodBuilder method2 = TestTypeBuilder.DefineMethod("NegTest2_Method2", c_DEFAULT_METHOD_ATTRIBUTE);
            ILGenerator generator = method1.GetILGenerator();
            LocalBuilder arg = method2.GetILGenerator().DeclareLocal(typeof(int));

            Assert.Throws<ArgumentException>(() => { generator.Emit(OpCodes.Ldarg_0, arg); });
        }

        [Fact]
        public void NegTest3()
        {
            MethodBuilder method = TestTypeBuilder.DefineMethod("NegTest3_Method", c_DEFAULT_METHOD_ATTRIBUTE);
            ILGenerator generator = method.GetILGenerator();
            for (int i = 0; i <= Byte.MaxValue; ++i)
            {
                generator.DeclareLocal(typeof(int));
            }
            LocalBuilder arg = generator.DeclareLocal(typeof(int));

            Assert.Throws<InvalidOperationException>(() => { generator.Emit(OpCodes.Br_S, arg); });
        }
    }
}