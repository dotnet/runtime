// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Test
{
    using System;
    class App
    {
        static void Func(ref Array param1) { }
        static void Main1()
        {
            Array arr = null;
            Func(ref ((Array[])arr)[0]);
        }
        static int Main()
        {
            try
            {
                Main1();
                return 1;
            }
            catch (NullReferenceException)
            {
                return 100;
            }
        }
    }
}
