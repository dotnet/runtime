// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace ConsoleApplication1
{
    internal struct TheStruct
    {
        public string fieldinStruct;
    }

    public class TestStruct
    {
        private static void StructTaker_Inline(TheStruct s)
        {
            s.fieldinStruct = "xyz";
        }

        [Fact]
        public static int TestEntryPoint()
        {
            TheStruct testStruct = new TheStruct();

            testStruct.fieldinStruct = "change_xyz";

            StructTaker_Inline(testStruct);

            System.Console.WriteLine("Struct field = {0}", testStruct.fieldinStruct);

            return 100;
        }
    }
}
