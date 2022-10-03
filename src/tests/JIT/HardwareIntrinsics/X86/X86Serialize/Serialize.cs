// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace IntelHardwareIntrinsicTest
{
    class Program
    {
        const int Pass = 100;
        const int Fail = 0;

        static unsafe int Main(string[] args)
        {
            int testResult = X86Serialize.IsSupported ? Pass : Fail;

            try
            {
                X86Serialize.Serialize();
            }
            catch (Exception e)
            {
                testResult = (X86Serialize.IsSupported || (e is not PlatformNotSupportedException)) ? Fail : Pass;
            }

            return testResult;
        }
    }
}
