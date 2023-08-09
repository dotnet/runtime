// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using Xunit;
namespace DefaultNamespace
{
    using System;

    public class DD
    {
        public static int zero = 0;
        [Fact]
        public static int TestEntryPoint()
        {
            try
            {
                int x = 100 / DD.zero;
            }
            catch (DivideByZeroException)
            {
                return 100;
            }
            return 1;
        }
    }
}
