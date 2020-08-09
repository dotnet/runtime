// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class ILGeneratorEmitMethodInfo
    {
        public interface IWithIn<T>
        {
            void Method(in RuntimeMethodHandle arg);
        }

        public sealed class WithIn : IWithIn<int>
        {
            public void Method(in RuntimeMethodHandle arg)
            {
                
            }
        }

        [Fact]
        public void EmitMethodInfo()
        {
            var testInstance = new WithIn();
            var methodType = typeof(IWithIn<int>);
            var method = methodType.GetMethod("Method");

            ModuleBuilder moduleBuilder = Helpers.DynamicModule(); ;
            var typeBuilder = moduleBuilder.DefineType("DynamicType", TypeAttributes.Public);

            var methodBuilder = typeBuilder.DefineMethod("Call", MethodAttributes.Public | MethodAttributes.Static, null, new Type[] { typeof(IWithIn<int>) });

            var ilBuilder = methodBuilder.GetILGenerator();
            ilBuilder.Emit(OpCodes.Ldarg_0);
            ilBuilder.Emit(OpCodes.Ldtoken, method);
            ilBuilder.Emit(OpCodes.Callvirt, method);
            ilBuilder.Emit(OpCodes.Ret);

            var type = typeBuilder.CreateType();
            var genMethod = type.GetMethod("Call");
            genMethod.Invoke(null, new object[] { testInstance });

            var il = genMethod.GetMethodBody().GetILAsByteArray();

            var ilMethodMetadataToken = BitConverter.ToInt32(il, 2);
            var resolvedMethod = type.Module.ResolveMethod(ilMethodMetadataToken);
            Assert.Equal(method, resolvedMethod);
            ilMethodMetadataToken = BitConverter.ToInt32(il, 7);
            resolvedMethod = type.Module.ResolveMethod(ilMethodMetadataToken);
            Assert.Equal(method, resolvedMethod);
        }
    }
}
