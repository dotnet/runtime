// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Reflection.Emit.Tests
{
    public sealed class ILGeneratorEmit5
    {
        public interface IHelper
        {
            int PassThrough(in int value);
        }

        public sealed class Helper : IHelper
        {
            public int PassThrough(in int value) => value;
        }

        [Fact]
        public void TestEmitCallFunctionWithInArgumentFromDynamicMethod()
        {
            var methodToCall = typeof(IHelper).GetMethod("PassThrough");

            var dynamicMethod = new DynamicMethod("CallingPassThrough",
                MethodAttributes.Public | MethodAttributes.Static,
                CallingConventions.Standard,
                typeof(int),
                new[]
                {
                  typeof(IHelper),
                  typeof(int),
                },
                typeof(ILGeneratorEmit5),
                true);

            var il = dynamicMethod.GetILGenerator();

            var copy = il.DeclareLocal(typeof(int), false);

            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Stloc, (ushort)copy.LocalIndex);

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldloca, (ushort)copy.LocalIndex);

            il.EmitCall(OpCodes.Callvirt, methodToCall, null);

            il.Emit(OpCodes.Ret);

            var func = (Func<IHelper, int, int>)dynamicMethod.CreateDelegate(typeof(Func<IHelper, int, int>));

            var helperInstance = new Helper();

            var sum = func(helperInstance, 888);

            Assert.Equal(888, sum);
        }
    }

}
