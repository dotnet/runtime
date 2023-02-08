// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using Xunit;
namespace Test
{
    using System;

    public class AA
    {
        public static Array Method1()
        {
            Array[] arr = new Array[1];
            try
            {
                return arr[0];
            }
            finally
            {
                throw new Exception();
            }
            return arr[0];
        }
        [Fact]
        public static int TestEntryPoint()
        {
            try
            {
                Method1();
            }
            catch (Exception)
            {
                return 100;
            }
            return 101;
        }
    }
}
