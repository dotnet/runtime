// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using Xunit;
namespace Test
{
    using System;

    public class App
    {
        [Fact]
        public static int TestEntryPoint()
        {
            try
            {
                Int16 foo = 0;
                for (int i = 0; i < 5; i++)
                {
                    checked { foo += 32000; }
                    Console.WriteLine("foo=" + foo);
                }
            }
            catch (OverflowException) { return 100; }
            return 1;
        }
    }
}
