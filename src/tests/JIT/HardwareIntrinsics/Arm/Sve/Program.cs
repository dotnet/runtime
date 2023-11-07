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

namespace JIT.HardwareIntrinsics.Arm._AdvSimd
{
    public static partial class Program
    {
        [Fact]
        public static void SveTest()
        {
            var test = new SveTest__SveTest();

            if (test.IsSupported)
            {
                test.RunBasicScenario_Count16();

                // Validates basic functionality works, using Unsafe.Read
                test.RunBasicScenario_SveUzp1();
            }

            if (!test.Succeeded)
            {
                throw new Exception("One or more scenarios did not complete as expected.");
            }
        }
    }

    public sealed unsafe class SveTest__SveTest
    {
        public bool IsSupported => AdvSimd.IsSupported;

        public bool Succeeded { get; set; }


        [MethodImpl(MethodImplOptions.NoInlining)]
        public ulong Count16()
        {
            return Sve.Count16BitElements();
        }


        [MethodImpl(MethodImplOptions.NoInlining)]
        public Vector<int> unzip(Vector<int> left, Vector<int> right)
        {
            return Sve.UnzipEven(left, right);
        }


        public void RunBasicScenario_Count16()
        {
            Count16();
            Console.WriteLine("Done");
            Succeeded = true;
        }

        public void RunBasicScenario_SveUzp1()
        {
            Vector<int> left = Vector<int>.One;
            Vector<int> right = Vector<int>.One;
            unzip(left, right);
            Console.WriteLine("Done");
            Succeeded = true;
        }
    }
}
