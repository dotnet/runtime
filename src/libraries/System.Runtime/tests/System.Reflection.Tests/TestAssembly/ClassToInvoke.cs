// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System.Reflection.TestAssembly
{
    public sealed class ClassToInvoke
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Assembly CallMe_AggressiveInlining()
        {
            Assembly asm = CallMeActual();

            // Avoid tailcall optimization by ensuring CallMe_Actual() is not the last method invoked.
            CallMe_AvoidTailcall();

            return asm;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Assembly CallMeActual()
        {
            return Assembly.GetCallingAssembly();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static int CallMe_AvoidTailcall() => 42;
    }
}
