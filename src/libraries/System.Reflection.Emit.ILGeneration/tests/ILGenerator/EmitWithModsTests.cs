// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Reflection.Emit.Tests
{
    public sealed class EmitWithMods
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
            MethodInfo methodToCall = typeof(IHelper).GetMethod("PassThrough");

            var dynamicMethod = new DynamicMethod("CallingPassThrough",
                MethodAttributes.Public | MethodAttributes.Static,
                CallingConventions.Standard,
                typeof(int),
                new[]
                {
                  typeof(IHelper),
                  typeof(int),
                },
                typeof(EmitWithMods),
                true);

            ILGenerator il = dynamicMethod.GetILGenerator();

            LocalBuilder copy = il.DeclareLocal(typeof(int), false);

            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Stloc, (ushort)copy.LocalIndex);

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldloca, (ushort)copy.LocalIndex);

            il.EmitCall(OpCodes.Callvirt, methodToCall, null);

            il.Emit(OpCodes.Ret);

            var func = (Func<IHelper, int, int>)dynamicMethod.CreateDelegate(typeof(Func<IHelper, int, int>));

            var helperInstance = new Helper();

            int sum = func(helperInstance, 888);

            Assert.Equal(888, sum);
        }
    }

}
