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
    public interface IWithIn<T>
    {
        void Method(in int arg);
    }
    public class ILGeneratorEmitMethodInfo
    {
        [Fact]
        public void EmitMethodInfo()
        {
            var methodType = typeof(IWithIn<int>);
            var method = methodType.GetMethod("Method");
            var getMethodFromHandle = typeof(MethodBase).GetMethod("GetMethodFromHandle", new[] { typeof(RuntimeMethodHandle), typeof(RuntimeTypeHandle) });

            ModuleBuilder moduleBuilder = Helpers.DynamicModule();
            var typeBuilder = moduleBuilder.DefineType("DynamicType", TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.Class);

            var methodBuilder = typeBuilder.DefineMethod("Get", MethodAttributes.Public | MethodAttributes.Static, typeof(MethodBase), new Type[0]);
            var ilBuilder = methodBuilder.GetILGenerator();
            ilBuilder.Emit(OpCodes.Ldtoken, method);
            ilBuilder.Emit(OpCodes.Ldtoken, methodType);
            ilBuilder.Emit(OpCodes.Call, getMethodFromHandle);
            ilBuilder.Emit(OpCodes.Ret);

            var type = typeBuilder.CreateType();

            var genMethod = type.GetMethod("Get");
            var il = genMethod.GetMethodBody().GetILAsByteArray();

            var ilMethodMetadataToken = BitConverter.ToInt32(il, 1);
            var resolvedMethod = type.Module.ResolveMethod(ilMethodMetadataToken);
            Assert.Equal(method, resolvedMethod);
            var methodBase = (MethodBase)genMethod.Invoke(null, null);
            Assert.Equal(method, methodBase);
        }
    }
}
