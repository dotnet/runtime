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
            bool b = true;
            do
            {
                try
                {
                    b = true;
                    do
                    {
                        while (b)
                            return 100;
                    } while (b);
                }
                catch (Exception) { }
                do
                {
                    long local4 = 32L;
                    do
                    {
                    } while (checked(38L >= local4));
                } while (b);
            } while (b);
            return -1;
        }
    }
}
