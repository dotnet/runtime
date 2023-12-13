// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics;
using Xunit;

namespace IntelHardwareIntrinsicTest._Avx512F
{
    public partial class Program
    {
        [Fact]
        public static unsafe void ConvertToInt32EmbeddedRounding_Float()
        {
            int testResult = 1;
            int answerTable_ToNegativeInfinity = -1;
            int answerTable_ToPositiveInfinity  = 0;
            int answerTable_ToZero = 0;
            if (Avx512F.IsSupported)
            {
                Vector128<float> inputVec = Vector128.Create(-0.45f, -0.45f, -0.45f, -0.45f);
                int res = Avx512F.ConvertToInt32(inputVec, FloatRoundingMode.ToNegativeInfinity);

                if (res != answerTable_ToNegativeInfinity)
                {
                    Console.WriteLine("Avx512 ConvertToInt32 Embedded rounding failed on float with ToNegativeInfinity:");
                    Console.Write(res);
                    Console.WriteLine();
                    Assert.Fail("");
                }

                res = Avx512F.ConvertToInt32(inputVec, FloatRoundingMode.ToPositiveInfinity);

                if (res != answerTable_ToPositiveInfinity)
                {
                    Console.WriteLine("Avx512 ConvertToInt32 Embedded rounding failed on float with ToPositiveInfinity:");
                    Console.Write(res);
                    Console.WriteLine();
                    Assert.Fail("");
                }

                res = Avx512F.ConvertToInt32(inputVec, FloatRoundingMode.ToZero);

                if (res != answerTable_ToZero)
                {
                    Console.WriteLine("Avx512 ConvertToInt32 Embedded rounding failed on float with ToZero:");
                    Console.Write(res);
                    Console.WriteLine();
                    Assert.Fail("");
                }
            }
            Assert.Equal(1, testResult);
        }
    }
}
