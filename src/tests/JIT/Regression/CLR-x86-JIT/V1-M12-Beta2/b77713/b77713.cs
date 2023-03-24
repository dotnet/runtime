// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using Xunit;
namespace Test
{
    using System;

    public class BB
    {
        internal static void Main1()
        {
            bool local2 = false;
            try
            {
                if (local2)
                    return;
            }
            finally
            {
                throw new Exception();
            }
        }
        [Fact]
        public static int TestEntryPoint()
        {
            try
            {
                Main1();
            }
            catch (Exception)
            {
                return 100;
            }
            return 101;
        }
    }
}
