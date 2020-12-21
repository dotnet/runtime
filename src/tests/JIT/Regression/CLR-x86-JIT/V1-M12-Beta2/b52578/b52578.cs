// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
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
