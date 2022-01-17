// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace JIT.HardwareIntrinsics.X86
{
    public static partial class Program
    {
        static Program()
        {
            TestList = new Dictionary<string, Action>() {
                ["Sha1MessageSchedule1.Byte"] = Sha1MessageSchedule1Byte,
                ["Sha1MessageSchedule2.Byte"] = Sha1MessageSchedule2Byte,
                ["Sha1NextE.Byte"] = Sha1NextEByte,
                ["Sha1FourRounds.Byte"] = Sha1FourRoundsByte,
                ["Sha256MessageSchedule1.Byte"] = Sha256MessageSchedule1Byte,
                ["Sha256MessageSchedule2.Byte"] = Sha256MessageSchedule2Byte,
                ["Sha256TwoRounds.Byte"] = Sha256TwoRoundsByte,
            };
        }
    }
}
