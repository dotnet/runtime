// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;

namespace System.Reflection.Emit.ILGeneration.Tests
{
    public class ExceptionEmitTests
    {
        [Fact]
        public void TestExceptionEmitCalls()
        {
            AssemblyName myAsmName = new AssemblyName("AdderExceptionAsm");
            AssemblyBuilder myAsmBldr = AssemblyBuilder.DefineDynamicAssembly(myAsmName, AssemblyBuilderAccess.Run);

            ModuleBuilder myModBldr = TestLibrary.Utilities.GetModuleBuilder(myAsmBldr, "Module1");
            TypeBuilder myTypeBldr = myModBldr.DefineType("Adder");

            MethodBuilder methodBuilder = myTypeBldr.DefineMethod("DoThrow",
                               MethodAttributes.Public |
                               MethodAttributes.Static,
                               typeof(int), new Type[] { typeof(bool) });

            ILGenerator ilgen = methodBuilder.GetILGenerator();
            Type overflow = typeof(OverflowException);

            LocalBuilder tmp1 = ilgen.DeclareLocal(typeof(int));
            Label dontThrow = ilgen.DefineLabel();

            // Begin the try block.
            Label exBlock = ilgen.BeginExceptionBlock();

            ilgen.Emit(OpCodes.Ldarg_0);
            ilgen.Emit(OpCodes.Brfalse_S, dontThrow);
            // Throw the exception now on the stack.
            ilgen.ThrowException(overflow);
            ilgen.MarkLabel(dontThrow);
            
            // Start the catch block for OverflowException. 
            ilgen.BeginCatchBlock(overflow);

            // Since our function has to return an integer value, we'll load -1 onto 
            // the stack to indicate an error, and store it in local variable tmp1. 
            //
            ilgen.Emit(OpCodes.Ldc_I4_M1);
            ilgen.Emit(OpCodes.Stloc_S, tmp1);

            // End the exception handling block.

            ilgen.EndExceptionBlock();

            // Return
            ilgen.Emit(OpCodes.Ldloc_S, tmp1);
            ilgen.Emit(OpCodes.Ret);

            Type createdType = myTypeBldr.CreateTypeInfo().AsType();
            MethodInfo md = createdType.GetMethod("DoThrow");
            
            // Throw
            int ret = (int)md.Invoke(null, new object[] { true });
            Assert.Equal(-1, ret);

            // Don't Throw
            ret = (int)md.Invoke(null, new object[] { false });
            Assert.Equal(0, ret);
        }
    }
}

