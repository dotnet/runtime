// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using Xunit;

namespace JIT.HardwareIntrinsics.Arm._AdvSimd
{
    public static partial class Program
    {
        static Program()
        {
            JIT.HardwareIntrinsics.Arm.Program.PrintSupportedIsa();
        }

        [ConditionalTheory(typeof(AdvSimd.Arm64), nameof(AdvSimd.Arm64.IsSupported))]
        [InlineData(3f)]
        [InlineData(-2f)]
        public static void MultiplyBySelectedScalar_CreateScalarUnsafe(float scalar)
        {
            Assert.Equal(Vector64.Create(scalar, 2 * scalar), MultiplyCreatedScalar(Vector64.Create(1f, 2f), scalar));
            Assert.Equal(Vector128.Create(scalar, 2 * scalar, 3 * scalar, 4 * scalar), MultiplyCreatedScalar(Vector128.Create(1f, 2f, 3f, 4f), scalar));
            Assert.Equal(Vector128.Create((double)scalar, 2d * scalar), MultiplyCreatedScalar(Vector128.Create(1d, 2d), scalar));
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
        private static Vector64<float> MultiplyCreatedScalar(Vector64<float> value, float scalar) =>
            AdvSimd.MultiplyBySelectedScalar(value, Vector64.CreateScalarUnsafe(scalar), 0);

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
        private static Vector128<float> MultiplyCreatedScalar(Vector128<float> value, float scalar) =>
            AdvSimd.MultiplyBySelectedScalar(value, Vector128.CreateScalarUnsafe(scalar), 0);

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
        private static Vector128<double> MultiplyCreatedScalar(Vector128<double> value, double scalar) =>
            AdvSimd.Arm64.MultiplyBySelectedScalar(value, Vector128.CreateScalarUnsafe(scalar), 0);
    }
}
