// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;

namespace ConsoleApplication1
{
    internal struct TheStruct
    {
        public string fieldinStruct;
    }

    internal class TestStruct
    {
        private static void StructTaker_Inline(TheStruct s)
        {
            s.fieldinStruct = "xyz";
        }

        private static int Main()
        {
            TheStruct testStruct = new TheStruct();

            testStruct.fieldinStruct = "change_xyz";

            StructTaker_Inline(testStruct);

            System.Console.WriteLine("Struct field = {0}", testStruct.fieldinStruct);

            return 100;
        }
    }
}
