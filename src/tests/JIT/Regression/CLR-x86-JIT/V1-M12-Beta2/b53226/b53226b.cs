// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
namespace Test
{
    using System;

    public class App
    {
        private static int Main1()
        {
            bool b = false;
            TypedReference tr = __makeref(b);
            byte bb = __refvalue((b ? __makeref(b) : tr), byte);
            return 0;
        }
        [Fact]
        public static int TestEntryPoint()
        {
            try
            {
                return Main1();
            }
            catch (InvalidCastException)
            {
                return 100;
            }
        }
    }
}
