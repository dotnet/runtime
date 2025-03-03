// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Intrinsics.Arm;

namespace JIT.HardwareIntrinsics.Arm
{
    public static partial class Program
    {
        public static void PrintSupportedIsa()
        {
            TestLibrary.TestFramework.LogInformation("Supported ISAs:");
            TestLibrary.TestFramework.LogInformation($"  AdvSimd:   {AdvSimd.IsSupported}");
            TestLibrary.TestFramework.LogInformation($"  Aes:       {Aes.IsSupported}");
            TestLibrary.TestFramework.LogInformation($"  ArmBase:   {ArmBase.IsSupported}");
            TestLibrary.TestFramework.LogInformation($"  Crc32:     {Crc32.IsSupported}");
            TestLibrary.TestFramework.LogInformation($"  Dp:        {Dp.IsSupported}");
            TestLibrary.TestFramework.LogInformation($"  Rdm:       {Rdm.IsSupported}");
            TestLibrary.TestFramework.LogInformation($"  Sha1:      {Sha1.IsSupported}");
            TestLibrary.TestFramework.LogInformation($"  Sha256:    {Sha256.IsSupported}");
            TestLibrary.TestFramework.LogInformation($"  Sve:       {Sve.IsSupported}");
            TestLibrary.TestFramework.LogInformation(string.Empty);
        }
    }
}
