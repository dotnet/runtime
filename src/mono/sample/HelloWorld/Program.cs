// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics;
using System.Runtime.CompilerServices;

namespace HelloWorld
{
    internal class Program
    {

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static Vector64<uint> test_create(float v) {
            //var v1 = Vector128.Create((uint) v);
            var v2 = Vector64.Create((uint) v);

            //var result = Sha256.ScheduleUpdate1(v1, v1, v1);

            return v2;
        }

        public static void Main() {
            var result = test_create(15.5f);
            System.Console.WriteLine("hello" + result);
        }
    }
}
