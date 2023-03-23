// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace GCTest_gcarr_cs
{
    public class Test
    {
        private int _magic = 0x12345678;
        internal virtual void CheckValid()
        {
            if (_magic != 0x12345678)
                throw new Exception();
        }

        [Fact]
        public static int TestEntryPoint()
        {
            Test[] arr = new Test[97];
            for (int i = 0; i < 97; i++)
                arr[i] = new Test();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            for (int i = 0; i < 97; i++)
                arr[i].CheckValid();
            arr = null;
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            Console.WriteLine("Test passed.");
            return 100;
        }
    }
}
