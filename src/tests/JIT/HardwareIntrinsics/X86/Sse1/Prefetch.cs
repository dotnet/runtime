// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics;
using Xunit;

namespace IntelHardwareIntrinsicTest._Sse1
{
    public partial class Program
    {
        [Fact]
        public static unsafe void Prefetch()
        {
            int testResult = Pass;

            if (Sse.IsSupported)
            {
                using (TestTable_SingleArray<float> floatTable = new TestTable_SingleArray<float>(new float[4] { 1, -5, 100, 3 }))
                {
                    try
                    {
                        Sse.Prefetch0(floatTable.inArrayPtr);
                    }
                    catch
                    {
                        testResult = Fail;
                    }

                    try
                    {
                        Sse.Prefetch1(floatTable.inArrayPtr);
                    }
                    catch
                    {
                        testResult = Fail;
                    }

                    try
                    {
                        Sse.Prefetch2(floatTable.inArrayPtr);
                    }
                    catch
                    {
                        testResult = Fail;
                    }

                    try
                    {
                        Sse.PrefetchNonTemporal(floatTable.inArrayPtr);
                    }
                    catch
                    {
                        testResult = Fail;
                    }
                }
            }

            Assert.Equal(Pass, testResult);
        }
    }
}
