// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using Xunit;

namespace JitTest.HFA
{
    public class TestCase
    {
        [DllImport("test2", EntryPoint = "GetInt32Const")]
        public static extern int GetInt32Const();

        [DllImport("test2", EntryPoint = "GetInt64Const")]
        public static extern long GetInt64Const();

        [DllImport("test2", EntryPoint = "GetFloatConst")]
        public static extern float GetFloatConst();

        [DllImport("test2", EntryPoint = "GetDoubleConst")]
        public static extern double GetDoubleConst();

        [Fact]
        public static int TestEntryPoint()
        {
            System.Console.WriteLine("Int32 Const = " + GetInt32Const());
            System.Console.WriteLine("Int64 Const = " + GetInt64Const());
            System.Console.WriteLine("Float Const = " + GetFloatConst());
            System.Console.WriteLine("Double Const = " + GetDoubleConst());

            if (GetInt32Const() != 7)
            {
                System.Console.WriteLine("FAILED: GetInt32Const()!=7");
                System.Console.WriteLine("GetInt32Const() is {0}", GetInt32Const());
                return 1;
            }
            if (GetInt64Const() != 7)
            {
                System.Console.WriteLine("FAILED: GetInt64Const()!=7");
                System.Console.WriteLine("GetInt64Const() is {0}", GetInt64Const());
                return 1;
            }
            if ((GetFloatConst() - 7.777777) > 0.5)
            {
                System.Console.WriteLine("FAILED: (GetFloatConst()-7.777777)>0.5");
                System.Console.WriteLine("GetFloatConst() is {0}", GetFloatConst());
                return 1;
            }
            if ((GetDoubleConst() - 7.777777) > 0.5)
            {
                System.Console.WriteLine("FAILED: (GetDoubleConst()-7.777777)>0.5");
                System.Console.WriteLine("GetDoubleConst() is {0}", GetDoubleConst());
                return 1;
            }
            return 100;
        }
    }
}
