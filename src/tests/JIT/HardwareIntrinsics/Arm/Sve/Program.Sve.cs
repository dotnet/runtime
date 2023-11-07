// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/******************************************************************************
 * This file is auto-generated from a template file by the GenerateTests.csx  *
 * script in tests\src\JIT\HardwareIntrinsics.Arm\Shared. In order to make    *
 * changes, please update the corresponding template and run according to the *
 * directions listed in the file.                                             *
 ******************************************************************************/

using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Numerics;
using Xunit;

// TODO-SVE: This file should be replaced with a .template test.

namespace JIT.HardwareIntrinsics.Arm._AdvSimd
{
    public static partial class Program
    {
        [Fact]
        public static void SveTest()
        {
            var test = new SveTest__SveTest();
            test.Succeeded = true;

            if (test.IsSupported)
            {
                test.RunBasicScenario_LoadVector();
            }
            else
            {
                Console.WriteLine("SVE is not Supported.");
            }

            if (!test.Succeeded)
            {
                throw new Exception("One or more scenarios did not complete as expected.");
            }
        }
    }

    public sealed unsafe class SveTest__SveTest
    {
        public bool IsSupported => Sve.IsSupported;

        public bool Succeeded { get; set; }

        private static unsafe void* Align(byte* buffer, ulong expectedAlignment)
        {
            return (void*)(((ulong)buffer + expectedAlignment - 1) & ~(expectedAlignment - 1));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public Vector<int> do_LoadVectorNonFaulting(Vector<int> mask, int* address)
        {
            return Sve.LoadVectorNonFaulting(mask, address);
        }

        public void RunBasicScenario_LoadVector()
        {
            Vector<int> mask = Vector<int>.One;

            int elemsInVector = 4;
            int OpElementCount = elemsInVector * 2;
            int[] inArray1 = new int[OpElementCount];
            for (var i = 0; i < OpElementCount; i++) { inArray1[i] = i+1; }

            GCHandle inHandle1;
            inHandle1 = GCHandle.Alloc(inArray1, GCHandleType.Pinned);
            int* inArray1Ptr = (int*)Align((byte*)(inHandle1.AddrOfPinnedObject().ToPointer()), 128);

            Vector<int> outVector1 = do_LoadVectorNonFaulting(mask, inArray1Ptr);

            // TODO-SVE: There is no register allocation for predicate registers.
            // Instead, the jit will allocate a Z register for mask, but codegen will use the equivalent
            // register number as a predicate. But that predicate register will have an invalid value
            // (probably zero) and load the wrong vector elements.
            for (var i = 0; i < elemsInVector; i++)
            {
                if (inArray1[i] != outVector1[i])
                {
                    Console.WriteLine("{0} {1} != {2}", i, inArray1[i], outVector1[i]);
                    Succeeded = false;
                }
                Console.WriteLine(outVector1[i]);
            }

            Console.WriteLine("Done");
        }
    }
}
