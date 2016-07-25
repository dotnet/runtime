// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class ExceptionEmitTests
    {
        [Fact]
        public void TestExceptionEmitCalls()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.NotPublic);

            MethodBuilder methodBuilder = type.DefineMethod("DoThrow", MethodAttributes.Public | MethodAttributes.Static, typeof(int), new Type[] { typeof(bool) });

            ILGenerator ilGenerator = methodBuilder.GetILGenerator();
            Type overflow = typeof(OverflowException);

            LocalBuilder tmp1 = ilGenerator.DeclareLocal(typeof(int));
            Label dontThrow = ilGenerator.DefineLabel();

            // Begin the try block.
            Label exBlock = ilGenerator.BeginExceptionBlock();

            ilGenerator.Emit(OpCodes.Ldarg_0);
            ilGenerator.Emit(OpCodes.Brfalse_S, dontThrow);
            // Throw the exception now on the stack.
            ilGenerator.ThrowException(overflow);
            ilGenerator.MarkLabel(dontThrow);
            
            // Start the catch block for OverflowException. 
            ilGenerator.BeginCatchBlock(overflow);

            // Since our function has to return an integer value, we'll load -1 onto 
            // the stack to indicate an error, and store it in local variable tmp1.
            ilGenerator.Emit(OpCodes.Ldc_I4_M1);
            ilGenerator.Emit(OpCodes.Stloc_S, tmp1);

            // End the exception handling block.
            ilGenerator.EndExceptionBlock();

            // Return
            ilGenerator.Emit(OpCodes.Ldloc_S, tmp1);
            ilGenerator.Emit(OpCodes.Ret);

            Type createdType = type.CreateTypeInfo().AsType();
            MethodInfo createdMethod = createdType.GetMethod("DoThrow");
            
            Assert.Equal(-1, createdMethod.Invoke(null, new object[] { true })); // Throws
            Assert.Equal(0, createdMethod.Invoke(null, new object[] { false })); // Doesn't throw
        }
    }
}

