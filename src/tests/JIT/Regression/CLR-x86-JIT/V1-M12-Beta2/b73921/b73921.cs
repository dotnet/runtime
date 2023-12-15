// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using Xunit;
namespace Test
{
    using System;

    public class BB
    {
        public static bool TRUE() { throw new Exception(); }
        public static int Main1()
        {
            while (TRUE()) { }
            try
            {
                return 109;
            }
            catch (Exception) { }
            return 103;
        }
        [Fact]
        public static int TestEntryPoint()
        {
            try
            {
                Main1();
                return 101;
            }
            catch (Exception)
            {
                return 100;
            }
        }
    }
}
