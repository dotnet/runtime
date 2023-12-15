// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Inline_SideAffects
{
    public class Inline_SideAffects
    {
        private static int s_i = 0;
        private static bool Foo_Inline()
        {
            s_i++;

            return true;
        }

        private static bool Bar_Inline()
        {
            s_i += 3;
            return false;
        }
        [Fact]
        public static int TestEntryPoint()
        {
            if ((Foo_Inline()) && (Bar_Inline()))
            {
                goto Fail;
            }
            else
            {
                if (s_i != 4) goto succeeded;
            }

            s_i = 0;

            if ((Bar_Inline()) && (Foo_Inline()))
            {
                goto Fail;
            }
            else
            {
                if (s_i != 1) goto succeeded;
            }

        succeeded:
            Console.WriteLine("Test Passed.");
            return 100;

        Fail:
            Console.WriteLine("Test Failed");
            return 101;
        }
    }
}
