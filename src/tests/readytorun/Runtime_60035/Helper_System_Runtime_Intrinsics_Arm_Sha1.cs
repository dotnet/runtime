// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace Runtime_60035
{
    public static class Helper_System_Runtime_Intrinsics_Arm_Sha1
    {
        public static bool IsSupported
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            get { return System.Runtime.Intrinsics.Arm.Sha1.IsSupported; }
        }
    }

    public static class Helper_System_Runtime_Intrinsics_Arm_Sha1_Arm64
    {
        public static bool IsSupported
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            get { return System.Runtime.Intrinsics.Arm.Sha1.Arm64.IsSupported; }
        }
    }
}
