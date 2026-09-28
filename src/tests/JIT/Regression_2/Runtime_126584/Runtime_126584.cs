// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection.Emit;
using TestLibrary;
using Xunit;

// A struct large enough that initializing it reaches offsets >= 256.
public struct Big_126584
{
    public long A0, A1, A2, A3, A4, A5, A6, A7, A8, A9;
    public long A10, A11, A12, A13, A14, A15, A16, A17, A18, A19;
    public long A20, A21, A22, A23, A24, A25, A26, A27, A28, A29;
    public long A30, A31, A32, A33, A34, A35, A36, A37, A38, A39;
}

public class Runtime_126584
{
    // The emitter can only encode byte-sized offsets for locals numbered >= 32768. A struct
    // block-init of such a local reaching offset >= 256 must not be folded into the stack
    // addressing form, which the emitter cannot encode. Reflection.Emit is used to build a
    // method with enough locals to push the struct past that boundary without a huge source file.
    [ConditionalFact(typeof(Utilities), nameof(Utilities.IsReflectionEmitSupported))]
    public static void TestEntryPoint()
    {
        var method = new DynamicMethod("M0", typeof(long), new[] { typeof(bool) }, typeof(Runtime_126584).Module);
        ILGenerator il = method.GetILGenerator();

        const int DummyLocalCount = 33000;
        for (int i = 0; i < DummyLocalCount; i++)
        {
            il.DeclareLocal(typeof(int));
        }
        LocalBuilder big = il.DeclareLocal(typeof(Big_126584));

        // Initialize the struct behind a branch so it isn't hoisted into the prolog.
        Label skip = il.DefineLabel();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Brfalse, skip);
        il.Emit(OpCodes.Ldloca, big);
        il.Emit(OpCodes.Initobj, typeof(Big_126584));
        il.MarkLabel(skip);

        // Read a field past offset 255 so the access survives.
        il.Emit(OpCodes.Ldloca, big);
        il.Emit(OpCodes.Ldfld, typeof(Big_126584).GetField(nameof(Big_126584.A39)));
        il.Emit(OpCodes.Ret);

        var func = (Func<bool, long>)method.CreateDelegate(typeof(Func<bool, long>));
        Assert.Equal(0, func(true));
    }
}
