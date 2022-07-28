// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;

namespace JIT.HardwareIntrinsics.Arm
{
    class Program
    {
        const int Pass = 100;
        const int Fail = 0;

        static unsafe int Main(string[] args)
        {
            int testResult = ArmBase.IsSupported ? Pass : Fail;

            try
            {
                ArmBase.Yield();
            }
            catch (Exception e)
            {
                testResult = (ArmBase.IsSupported || (e is not PlatformNotSupportedException)) ? Fail : Pass;
            }

            return testResult;
        }
    }
}
