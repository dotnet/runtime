// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using Xunit;
namespace Test
{
    using System;

    public class AA
    {
        [Fact]
        public static int TestEntryPoint()
        {
            int L = 1;
            try
            {
                return 100;
            }
            catch (Exception)
            {
                while (L * L < 0) { };
            }
            while (L * L < 0) { };
            return -1;
        }
    }
}
