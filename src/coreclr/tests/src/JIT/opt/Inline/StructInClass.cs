// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Text;

namespace StructInClass
{
    class StructInClass
    {
    }
    class TestClass
    {

        public struct TheStruct
        {
            public string fieldinStruct;
        }

        static void StructTaker_Inline(TheStruct Struct)
        {
            Struct.fieldinStruct = "xyz";
        }

        static int Main(string[] args)
        {
            try
            {
                TestClass newobj = new TestClass();

                TheStruct s = new TheStruct();

                StructTaker_Inline(s);

                return 100;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return 666;

            }
        }
    }
}
