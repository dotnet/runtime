// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Text;

namespace Inline_SideAffects
{
    class Inline_SideAffects
    {
        static int i = 0;
        static bool Foo_Inline()
        {
            i++;

            return true;
        }

        static bool Bar_Inline()
        {
            i += 3;
            return false;
        }
        static int Main(string[] args)
        {

            if ((Foo_Inline()) && (Bar_Inline()))
            {
                // Should not come there.
                goto Fail;
            }
            else
            {
                // It should come here.
                if (i != 4) goto succeeded;
            }

            i = 0;

            if ((Bar_Inline()) && (Foo_Inline()))
            {
                goto Fail;
            }
            else
            {
                // It should come here.
                if (i != 1) goto succeeded;
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
