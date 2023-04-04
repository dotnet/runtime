// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
namespace Def
{
    using System;

    public class jitassert2
    {
        [Fact]
        public static int TestEntryPoint()
        {
            int i = -1;
            Object[] array = new Object[3];
            try
            {
                Object o = array[i];
                Console.WriteLine("Should have thrown!");
                return 1;
            }
            catch (System.Exception)
            {
                Console.WriteLine("Yup");
                return 100;
            }
        }
    }
}

