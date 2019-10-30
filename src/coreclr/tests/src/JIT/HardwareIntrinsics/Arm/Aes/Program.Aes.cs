// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace JIT.HardwareIntrinsics.Arm
{
    public static partial class Program
    {
        static Program()
        {
            TestList = new Dictionary<string, Action>() {
                ["Decrypt.Vector128.Byte"] = Decrypt_Vector128_Byte,
                ["Encrypt.Vector128.Byte"] = Encrypt_Vector128_Byte,
                ["InverseMixColumns.Vector128.Byte"] = InverseMixColumns_Vector128_Byte,
                ["MixColumns.Vector128.Byte"] = MixColumns_Vector128_Byte,
            };
        }
    }
}
