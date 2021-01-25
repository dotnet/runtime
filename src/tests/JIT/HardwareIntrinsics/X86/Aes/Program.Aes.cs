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
                ["Decrypt.Byte"] = DecryptByte,
                ["DecryptLast.Byte"] = DecryptLastByte,
                ["Encrypt.Byte"] = EncryptByte,
                ["EncryptLast.Byte"] = EncryptLastByte,
                ["InverseMixColumns.Byte"] = InverseMixColumnsByte,
                ["KeygenAssist.Byte.5"] = KeygenAssistByte5,
            };
        }
    }
}
