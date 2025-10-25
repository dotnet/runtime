// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using Xunit;
namespace b40221
{
    using System;

    public struct AA
    {
        static void Func()
        {
            int a = 0;
            while (a == 1)
                throw new Exception();
        }
        [OuterLoop]
        [Fact]
        public static int TestEntryPoint()
        {
            try
            {
                Func();
            }
            catch (Exception) { return -1; }
            return 100;
        }
    }
}
