// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
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
            Type methodType = typeof(IWithIn<int>);
            MethodInfo method = methodType.GetMethod("Method");
            MethodInfo getMethodFromHandle = typeof(MethodBase).GetMethod("GetMethodFromHandle", new[] { typeof(RuntimeMethodHandle), typeof(RuntimeTypeHandle) });

            ModuleBuilder moduleBuilder = Helpers.DynamicModule();
            TypeBuilder typeBuilder = moduleBuilder.DefineType("DynamicType", TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.Class);

            MethodBuilder methodBuilder = typeBuilder.DefineMethod("Get", MethodAttributes.Public | MethodAttributes.Static, typeof(MethodBase), new Type[0]);
            ILGenerator ilBuilder = methodBuilder.GetILGenerator();
            ilBuilder.Emit(OpCodes.Ldtoken, method);
            ilBuilder.Emit(OpCodes.Ldtoken, methodType);
            ilBuilder.Emit(OpCodes.Call, getMethodFromHandle);
            ilBuilder.Emit(OpCodes.Ret);

            Type type = typeBuilder.CreateType();

            MethodInfo genMethod = type.GetMethod("Get");
            byte[] il = genMethod.GetMethodBody().GetILAsByteArray();

            int ilMethodMetadataToken = BinaryPrimitives.ReadInt32LittleEndian(new Span<byte>(il, 1, 4));
            MethodBase resolvedMethod = type.Module.ResolveMethod(ilMethodMetadataToken);
            Assert.Equal(method, resolvedMethod);
            var methodBase = (MethodBase)genMethod.Invoke(null, null);
            Assert.Equal(method, methodBase);
        }
    }
}
